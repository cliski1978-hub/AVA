using System.Text.Json;
using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Services.Storage
{
    /// <summary>
    /// File-backed store for UI-owned session model-card state.
    /// </summary>
    public class SessionModelStateStore : ISessionModelStateStore
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly string RootFolder;
        private readonly string SessionsFolder;
        private readonly string StateFile;

        public SessionModelStateStore(string? sessionsPath = null)
        {
            if (string.IsNullOrWhiteSpace(sessionsPath))
            {
                RootFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AVA");
                SessionsFolder = Path.Combine(RootFolder, "sessions");
            }
            else
            {
                SessionsFolder = sessionsPath;
                RootFolder = SessionsFolder;
            }

            StateFile = Path.Combine(SessionsFolder, "session-model-state.json");
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SessionModelStateRecord>> LoadAsync(CancellationToken ct = default)
        {
            try
            {
                EnsureDirectory();

                if (!File.Exists(StateFile))
                    return Array.Empty<SessionModelStateRecord>();

                var json = await File.ReadAllTextAsync(StateFile, ct).ConfigureAwait(false);
                return JsonSerializer.Deserialize<List<SessionModelStateRecord>>(json, SerializerOptions)
                       ?? new List<SessionModelStateRecord>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVA] Failed to load session-model-state.json: {ex.Message}");
                return Array.Empty<SessionModelStateRecord>();
            }
        }

        /// <inheritdoc />
        public async Task SaveAsync(SessionModelStateRecord record, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(record);

            record.AttachedModelIds = Normalize(record.AttachedModelIds);
            record.BroadcastGroupIds = Normalize(record.BroadcastGroupIds)
                .Where(id => record.AttachedModelIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(record.DefaultModelId) &&
                !record.AttachedModelIds.Contains(record.DefaultModelId, StringComparer.OrdinalIgnoreCase))
            {
                record.DefaultModelId = record.AttachedModelIds.FirstOrDefault();
            }
            record.ModelBindings = NormalizeBindings(record.ModelBindings, record.AttachedModelIds, record.BroadcastGroupIds, record.DefaultModelId);

            var existing = (await LoadAsync(ct).ConfigureAwait(false)).ToList();
            existing.RemoveAll(item => Matches(item, record.VaultId, record.ProjectId, record.SessionId));

            record.UpdatedAt = DateTime.UtcNow;
            existing.Add(record);

            EnsureDirectory();
            var json = JsonSerializer.Serialize(existing, SerializerOptions);
            await File.WriteAllTextAsync(StateFile, json, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task ApplyToVaultsAsync(IEnumerable<VaultState> vaults, CancellationToken ct = default)
        {
            var records = await LoadAsync(ct).ConfigureAwait(false);

            foreach (var vault in vaults)
            {
                foreach (var session in vault.Sessions)
                {
                    ApplyRecord(records, vault.VaultId, null, session);
                }

                foreach (var project in vault.Projects)
                {
                    foreach (var session in project.Sessions)
                    {
                        ApplyRecord(records, vault.VaultId, project.ProjectId, session);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task ApplyToSessionAsync(string vaultId, string? projectId, SessionState session, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(vaultId);
            ArgumentNullException.ThrowIfNull(session);

            var records = await LoadAsync(ct).ConfigureAwait(false);
            ApplyRecord(records, vaultId, projectId, session);
        }

        private static void ApplyRecord(
            IEnumerable<SessionModelStateRecord> records,
            string vaultId,
            string? projectId,
            SessionState session)
        {
            // DB is the primary source. Only apply JSON if DB provided no model state.
            if (session.AttachedModelIds.Any()) return;

            var record = records.FirstOrDefault(item => Matches(item, vaultId, projectId, session.SessionId));
            if (record == null) return;

            // Only apply if the JSON record actually has model IDs to restore.
            var normalized = Normalize(record.AttachedModelIds);
            if (!normalized.Any()) return;

            session.AttachedModelIds = normalized;
            session.BroadcastGroupIds = Normalize(record.BroadcastGroupIds)
                .Where(id => session.AttachedModelIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                .ToList();
            session.DefaultModelId = string.IsNullOrWhiteSpace(record.DefaultModelId)
                ? session.AttachedModelIds.FirstOrDefault()
                : record.DefaultModelId;
            session.ModelBindings = NormalizeBindings(
                record.ModelBindings,
                session.AttachedModelIds,
                session.BroadcastGroupIds,
                session.DefaultModelId);
        }

        private static bool Matches(SessionModelStateRecord item, string vaultId, string? projectId, string sessionId)
            => item.VaultId.Equals(vaultId, StringComparison.OrdinalIgnoreCase)
               && string.Equals(item.ProjectId ?? string.Empty, projectId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               && item.SessionId.Equals(sessionId, StringComparison.OrdinalIgnoreCase);

        private static List<string> Normalize(IEnumerable<string>? ids)
            => (ids ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <inheritdoc />
        public async Task MirrorAsync(IEnumerable<VaultState> vaults, CancellationToken ct = default)
        {
            try
            {
                var records = new List<SessionModelStateRecord>();

                foreach (var vault in vaults)
                {
                    foreach (var session in vault.Sessions)
                        records.Add(BuildRecord(vault.VaultId, null, session));

                    foreach (var project in vault.Projects)
                        foreach (var session in project.Sessions)
                            records.Add(BuildRecord(vault.VaultId, project.ProjectId, session));
                }

                EnsureDirectory();
                var json = JsonSerializer.Serialize(records, SerializerOptions);
                await File.WriteAllTextAsync(StateFile, json, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVA] Failed to mirror session model state to JSON: {ex.Message}");
            }
        }

        private static SessionModelStateRecord BuildRecord(string vaultId, string? projectId, SessionState session)
            => new SessionModelStateRecord
            {
                VaultId          = vaultId,
                ProjectId        = projectId,
                SessionId        = session.SessionId,
                AttachedModelIds = Normalize(session.AttachedModelIds),
                BroadcastGroupIds = Normalize(session.BroadcastGroupIds),
                DefaultModelId   = session.DefaultModelId,
                ModelBindings = NormalizeBindings(
                    session.ModelBindings,
                    session.AttachedModelIds,
                    session.BroadcastGroupIds,
                    session.DefaultModelId),
                UpdatedAt        = DateTime.UtcNow
            };

        private static List<SessionModelBinding> NormalizeBindings(
            IEnumerable<SessionModelBinding>? bindings,
            IReadOnlyCollection<string> attachedIds,
            IReadOnlyCollection<string> broadcastIds,
            string? defaultModelId)
        {
            var result = new List<SessionModelBinding>();
            foreach (var modelId in attachedIds)
            {
                var binding = bindings?.FirstOrDefault(item =>
                    item.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase))
                    ?? new SessionModelBinding { ModelId = modelId };

                binding.ModelId = modelId;
                binding.IsDefault = string.Equals(modelId, defaultModelId, StringComparison.OrdinalIgnoreCase);
                binding.IsBroadcastEnabled = broadcastIds.Contains(modelId, StringComparer.OrdinalIgnoreCase);
                binding.RuntimeContextSettings ??= new RuntimeContextSettings();
                result.Add(binding);
            }

            return result;
        }

        private void EnsureDirectory(string? folder = null)
        {
            var target = string.IsNullOrWhiteSpace(folder) ? SessionsFolder : folder;
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }
        }

    }
}
