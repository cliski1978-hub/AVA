using System.Text.Json;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services;
using AVA.Vault.Core.Services.Data;


namespace AVA.Vault.Core.Adapters
{
    /// <summary>
    /// IVaultPersistenceProvider implementation for file system storage.
    /// All write operations return the same CfkApiResponse-derived types as DbVaultPersistenceProvider
    /// so the calling ViewModel never knows which provider handled the request.
    /// Succeeded/UserMessage are populated for every outcome — no exceptions escape to the VM.
    /// </summary>
    public class FileVaultPersistenceProvider : IVaultPersistenceProvider
    {
        private readonly VaultManager _vaultManager;
        private readonly VaultLogger _logger;
        private readonly IVaultIdService _ids;
        private readonly string _vaultsRoot;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented            = true,
            PropertyNamingPolicy     = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public FileVaultPersistenceProvider(VaultManager vaultManager, VaultLogger logger, IVaultIdService ids, string vaultsRoot)
        {
            _vaultManager = vaultManager;
            _logger       = logger;
            _ids          = ids;
            _vaultsRoot   = vaultsRoot;
        }

        // ── Vault ──────────────────────────────────────────────────────────────

        public Task<CreateVaultHeaderResponse> CreateVaultAsync(string name, string? vaultId = null, CancellationToken ct = default)
        {
            try
            {
                var vault = _vaultManager.CreateVault(name);
                if (!string.IsNullOrWhiteSpace(vaultId))
                    vault.Id = vaultId;

                var header = new VaultHeader
                {
                    ID          = vault.Id,
                    DisplayName = vault.Name,
                    CreatedAt   = vault.Created,
                    IsActive    = true
                };

                WriteJson(Path.Combine(_vaultsRoot, vault.Name, "vault.header.json"), header);
                _logger.Log(nameof(FileVaultPersistenceProvider), $"Created file vault [{vault.Id}] '{vault.Name}'");

                return Task.FromResult(new CreateVaultHeaderResponse
                {
                    VaultId     = header.ID,
                    Vault       = header,
                    UserMessage = "Vault created successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error creating file vault.", ex);
                return Task.FromResult(new CreateVaultHeaderResponse
                {
                    UserMessage = ex.Message
                });
            }
        }

        public Task<UpdateVaultHeaderResponse> RenameVaultAsync(string vaultId, string newName, CancellationToken ct = default)
        {
            try
            {
                var vaultPath  = GetVaultPath(vaultId);
                var headerPath = Path.Combine(vaultPath, "vault.header.json");

                if (!File.Exists(headerPath))
                    return Task.FromResult(new UpdateVaultHeaderResponse { Code = 400, UserMessage = $"Vault header not found for [{vaultId}]." });

                var header  = ReadJson<VaultHeader>(headerPath);
                var oldPath = Path.Combine(_vaultsRoot, header.DisplayName);
                var newPath = Path.Combine(_vaultsRoot, newName);

                header.DisplayName = newName;
                WriteJson(headerPath, header);

                if (Directory.Exists(oldPath) && oldPath != newPath)
                    Directory.Move(oldPath, newPath);

                _logger.Log(nameof(FileVaultPersistenceProvider), $"Renamed file vault [{vaultId}] to '{newName}'");

                return Task.FromResult(new UpdateVaultHeaderResponse
                {
                    VaultId     = vaultId,
                    Vault       = header,
                    UserMessage = "Vault renamed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error renaming file vault.", ex);
                return Task.FromResult(new UpdateVaultHeaderResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<DeleteVaultHeaderResponse> DeleteVaultAsync(string vaultId, CancellationToken ct = default)
        {
            try
            {
                var vaultPath = GetVaultPath(vaultId);
                if (Directory.Exists(vaultPath))
                    Directory.Delete(vaultPath, recursive: true);

                _logger.Log(nameof(FileVaultPersistenceProvider), $"Deleted file vault [{vaultId}]");

                return Task.FromResult(new DeleteVaultHeaderResponse
                {
                    Deleted     = true,
                    UserMessage = "Vault deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error deleting file vault.", ex);
                return Task.FromResult(new DeleteVaultHeaderResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<IEnumerable<VaultHeader>> ListVaultsAsync(CancellationToken ct = default)
        {
            var headers = _vaultManager.ListVaults();
            return Task.FromResult(headers);
        }

        // ── Project ────────────────────────────────────────────────────────────

        public Task<CreateVaultProjectResponse> CreateProjectAsync(string vaultId, string name, string? projectId = null, CancellationToken ct = default)
        {
            try
            {
                var resolvedProjectId = string.IsNullOrWhiteSpace(projectId) ? _ids.NewProjectId() : projectId;
                var projectDir        = GetProjectPath(vaultId, resolvedProjectId);
                Directory.CreateDirectory(projectDir);

                var project = new VaultProject
                {
                    ID         = resolvedProjectId,
                    VaultID    = vaultId,
                    Name       = name,
                    CreatedAt  = DateTime.UtcNow,
                    UpdatedAt  = DateTime.UtcNow,
                    IsExpanded = true
                };

                WriteJson(Path.Combine(projectDir, "project.header.json"), project);
                _logger.Log(nameof(FileVaultPersistenceProvider), $"Created file project [{resolvedProjectId}] '{name}' in vault {vaultId}");

                return Task.FromResult(new CreateVaultProjectResponse
                {
                    ProjectID   = project.ID,
                    UserMessage = "Project created successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error creating file project.", ex);
                return Task.FromResult(new CreateVaultProjectResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<UpdateVaultProjectResponse> RenameProjectAsync(string vaultId, string projectId, string newName, CancellationToken ct = default)
        {
            try
            {
                var headerPath = Path.Combine(GetProjectPath(vaultId, projectId), "project.header.json");

                if (!File.Exists(headerPath))
                    return Task.FromResult(new UpdateVaultProjectResponse { Code = 400, UserMessage = $"Project header not found [{projectId}]." });

                var project       = ReadJson<VaultProject>(headerPath);
                project.Name      = newName;
                project.UpdatedAt = DateTime.UtcNow;
                WriteJson(headerPath, project);

                return Task.FromResult(new UpdateVaultProjectResponse
                {
                    ProjectID   = projectId,
                    UserMessage = "Project renamed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error renaming file project.", ex);
                return Task.FromResult(new UpdateVaultProjectResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<DeleteVaultProjectResponse> DeleteProjectAsync(string vaultId, string projectId, CancellationToken ct = default)
        {
            try
            {
                var projectDir = GetProjectPath(vaultId, projectId);
                if (Directory.Exists(projectDir))
                    Directory.Delete(projectDir, recursive: true);

                return Task.FromResult(new DeleteVaultProjectResponse
                {
                    Deleted     = true,
                    UserMessage = "Project deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error deleting file project.", ex);
                return Task.FromResult(new DeleteVaultProjectResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<IEnumerable<VaultProject>> ListProjectsAsync(string vaultId, CancellationToken ct = default)
        {
            var projectsRoot = Path.Combine(GetVaultPath(vaultId), "projects");
            var results      = new List<VaultProject>();

            if (!Directory.Exists(projectsRoot)) return Task.FromResult<IEnumerable<VaultProject>>(results);

            foreach (var dir in Directory.GetDirectories(projectsRoot))
            {
                var headerPath = Path.Combine(dir, "project.header.json");
                if (File.Exists(headerPath))
                    results.Add(ReadJson<VaultProject>(headerPath));
            }

            return Task.FromResult<IEnumerable<VaultProject>>(results.OrderBy(p => p.CreatedAt));
        }

        // ── Session ────────────────────────────────────────────────────────────

        public Task<CreateVaultSessionResponse> CreateSessionAsync(string vaultId, string? projectId, string name, string? sessionId = null, CancellationToken ct = default)
        {
            try
            {
                var resolvedSessionId = string.IsNullOrWhiteSpace(sessionId) ? _ids.NewSessionId() : sessionId;
                var sessionDir        = GetSessionPath(vaultId, projectId, resolvedSessionId);
                Directory.CreateDirectory(sessionDir);

                var session = new VaultSession
                {
                    ID         = resolvedSessionId,
                    VaultID    = vaultId,
                    ProjectID  = projectId,
                    Name       = name,
                    CreatedAt  = DateTime.UtcNow,
                    LastActiveAt = DateTime.UtcNow,
                    AttachedModelIdsJson  = "[]",
                    BroadcastGroupIdsJson = "[]",
                    CanvasJson            = "{}"
                };

                WriteJson(Path.Combine(sessionDir, "session.header.json"), session);
                _logger.Log(nameof(FileVaultPersistenceProvider), $"Created file session [{resolvedSessionId}] '{name}'");

                return Task.FromResult(new CreateVaultSessionResponse
                {
                    SessionID   = session.ID,
                    Session     = session,
                    UserMessage = "Session created successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error creating file session.", ex);
                return Task.FromResult(new CreateVaultSessionResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<UpdateVaultSessionResponse> RenameSessionAsync(string vaultId, string sessionId, string newName, CancellationToken ct = default)
        {
            try
            {
                var session    = FindSession(vaultId, sessionId);
                var headerPath = GetSessionHeaderPath(vaultId, session.ProjectID, sessionId);

                session.Name         = newName;
                session.LastActiveAt = DateTime.UtcNow;
                WriteJson(headerPath, session);

                return Task.FromResult(new UpdateVaultSessionResponse
                {
                    SessionID   = sessionId,
                    Session     = session,
                    UserMessage = "Session renamed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error renaming file session.", ex);
                return Task.FromResult(new UpdateVaultSessionResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<UpdateVaultSessionResponse> UpdateSessionModelStateAsync(string vaultId, string sessionId, List<string> attachedModelIds, List<string> broadcastGroupIds, string? defaultModelId, CancellationToken ct = default)
        {
            try
            {
                var session               = FindSession(vaultId, sessionId);
                var headerPath            = GetSessionHeaderPath(vaultId, session.ProjectID, sessionId);
                session.AttachedModelIdsJson  = System.Text.Json.JsonSerializer.Serialize(attachedModelIds);
                session.BroadcastGroupIdsJson = System.Text.Json.JsonSerializer.Serialize(broadcastGroupIds);
                session.DefaultModelId    = defaultModelId;
                session.LastActiveAt      = DateTime.UtcNow;
                WriteJson(headerPath, session);

                return Task.FromResult(new UpdateVaultSessionResponse
                {
                    SessionID   = sessionId,
                    Session     = session,
                    UserMessage = "Session model state updated successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error updating session model state.", ex);
                return Task.FromResult(new UpdateVaultSessionResponse { Code = 500, UserMessage = ex.Message });
            }
        }

        public Task<DeleteVaultSessionResponse> DeleteSessionAsync(string vaultId, string sessionId, CancellationToken ct = default)
        {
            try
            {
                var session    = FindSession(vaultId, sessionId);
                var sessionDir = GetSessionPath(vaultId, session.ProjectID, sessionId);
                if (Directory.Exists(sessionDir))
                    Directory.Delete(sessionDir, recursive: true);

                return Task.FromResult(new DeleteVaultSessionResponse
                {
                    Deleted     = true,
                    UserMessage = "Session deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error deleting file session.", ex);
                return Task.FromResult(new DeleteVaultSessionResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<IEnumerable<VaultSession>> ListSessionsAsync(string vaultId, CancellationToken ct = default)
        {
            var results = new List<VaultSession>();
            results.AddRange(ReadSessionsFromDir(Path.Combine(GetVaultPath(vaultId), "sessions")));

            var projectsRoot = Path.Combine(GetVaultPath(vaultId), "projects");
            if (Directory.Exists(projectsRoot))
            {
                foreach (var projectDir in Directory.GetDirectories(projectsRoot))
                    results.AddRange(ReadSessionsFromDir(Path.Combine(projectDir, "sessions")));
            }

            return Task.FromResult<IEnumerable<VaultSession>>(results.OrderBy(s => s.CreatedAt));
        }

        // ── Note ──────────────────────────────────────────────────────────────

        public Task<CreateVaultNoteResponse> CreateNoteAsync(string vaultId, string? projectId, string title, string content, string? sessionId = null, CancellationToken ct = default)
        {
            try
            {
                var noteId   = _ids.NewNoteId();
                var notesDir = GetNotesPath(vaultId, projectId);
                Directory.CreateDirectory(notesDir);

                var note = new VaultNote
                {
                    ID        = noteId,
                    VaultID   = vaultId,
                    SessionID = sessionId,
                    Title     = title,
                    Content   = content,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsSynced  = false
                };

                File.WriteAllText(Path.Combine(notesDir, $"{noteId}.md"), content);
                WriteJson(Path.Combine(notesDir, $"{noteId}.meta.json"), note);
                _logger.Log(nameof(FileVaultPersistenceProvider), $"Created file note [{noteId}] '{title}'");

                return Task.FromResult(new CreateVaultNoteResponse
                {
                    Note        = note,
                    UserMessage = "Note created successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error creating file note.", ex);
                return Task.FromResult(new CreateVaultNoteResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<VaultNote?> GetNoteAsync(string vaultId, string noteId, CancellationToken ct = default)
        {
            var vaultPath = GetVaultPath(vaultId);
            var meta      = FindNoteMeta(vaultPath, noteId);
            return Task.FromResult(meta);
        }

        public Task<UpdateVaultNoteResponse> UpdateNoteAsync(string vaultId, string noteId, string? title, string? content, CancellationToken ct = default)
        {
            try
            {
                var vaultPath          = GetVaultPath(vaultId);
                var (metaPath, mdPath) = FindNoteFiles(vaultPath, noteId);

                if (metaPath == null)
                    return Task.FromResult(new UpdateVaultNoteResponse { Code = 400, UserMessage = $"Note [{noteId}] not found." });

                var note = ReadJson<VaultNote>(metaPath);
                if (title   != null) note.Title   = title;
                if (content != null)
                {
                    note.Content = content;
                    File.WriteAllText(mdPath!, content);
                }

                note.UpdatedAt = DateTime.UtcNow;
                WriteJson(metaPath, note);

                return Task.FromResult(new UpdateVaultNoteResponse
                {
                    UserMessage = "Note updated successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error updating file note.", ex);
                return Task.FromResult(new UpdateVaultNoteResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<DeleteVaultNoteResponse> DeleteNoteAsync(string vaultId, string noteId, CancellationToken ct = default)
        {
            try
            {
                var vaultPath          = GetVaultPath(vaultId);
                var (metaPath, mdPath) = FindNoteFiles(vaultPath, noteId);

                if (metaPath != null && File.Exists(metaPath)) File.Delete(metaPath);
                if (mdPath   != null && File.Exists(mdPath))   File.Delete(mdPath);

                return Task.FromResult(new DeleteVaultNoteResponse
                {
                    Deleted     = true,
                    UserMessage = "Note deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error deleting file note.", ex);
                return Task.FromResult(new DeleteVaultNoteResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<IEnumerable<VaultNote>> SearchNotesAsync(string vaultId, string? projectId = null, string? sessionId = null, string? keyword = null, string? tag = null, string sortBy = "Updated", bool sortDescending = true, DateTime? createdAfter = null, DateTime? createdBefore = null, DateTime? updatedAfter = null, DateTime? updatedBefore = null, CancellationToken ct = default)
        {
            var vaultPath = GetVaultPath(vaultId);
            var results   = new List<VaultNote>();

            // Include vault-level notes when no project filter
            if (string.IsNullOrWhiteSpace(projectId))
            {
                var vaultNotesDir = Path.Combine(vaultPath, "notes");
                if (Directory.Exists(vaultNotesDir))
                    ReadNotesFromDir(vaultNotesDir, results, sessionId, keyword, tag, createdAfter, createdBefore, updatedAfter, updatedBefore);
            }

            var projectsRoot = Path.Combine(vaultPath, "projects");
            if (!Directory.Exists(projectsRoot)) return Task.FromResult<IEnumerable<VaultNote>>(results);

            var projectDirs = string.IsNullOrWhiteSpace(projectId)
                ? Directory.GetDirectories(projectsRoot)
                : new[] { Path.Combine(projectsRoot, projectId) };

            foreach (var pDir in projectDirs)
                ReadNotesFromDir(Path.Combine(pDir, "notes"), results, sessionId, keyword, tag, createdAfter, createdBefore, updatedAfter, updatedBefore);

            IEnumerable<VaultNote> sorted = (sortBy, sortDescending) switch
            {
                ("Created",      true)  => results.OrderByDescending(n => n.CreatedAt),
                ("Created",      false) => results.OrderBy(n => n.CreatedAt),
                ("Alphabetical", true)  => results.OrderByDescending(n => n.Title),
                ("Alphabetical", false) => results.OrderBy(n => n.Title),
                (_,              true)  => results.OrderByDescending(n => n.UpdatedAt),
                (_,              false) => results.OrderBy(n => n.UpdatedAt),
            };

            return Task.FromResult(sorted);
        }

        // ── Tag ───────────────────────────────────────────────────────────────

        public Task<CreateVaultTagResponse> CreateTagAsync(string vaultId, string name, string? color = null, CancellationToken ct = default)
        {
            try
            {
                var tags     = ReadTags(vaultId);
                var existing = tags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                    return Task.FromResult(new CreateVaultTagResponse { Tag = existing, UserMessage = "Tag already exists." });

                var tag = new VaultTag
                {
                    ID        = _ids.NewId(),
                    ProjectID = vaultId,
                    Name      = name,
                    Color     = color,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                tags.Add(tag);
                WriteTags(vaultId, tags);
                _logger.Log(nameof(FileVaultPersistenceProvider), $"Created file tag [{tag.ID}] '{name}'");

                return Task.FromResult(new CreateVaultTagResponse { Tag = tag, UserMessage = "Tag created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error creating file tag.", ex);
                return Task.FromResult(new CreateVaultTagResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<UpdateVaultTagResponse> RenameTagAsync(string vaultId, string tagId, string newName, CancellationToken ct = default)
        {
            try
            {
                var tags = ReadTags(vaultId);
                var tag  = tags.FirstOrDefault(t => t.ID == tagId);

                if (tag == null)
                    return Task.FromResult(new UpdateVaultTagResponse { Code = 400, UserMessage = $"Tag [{tagId}] not found." });

                tag.Name      = newName;
                tag.UpdatedAt = DateTime.UtcNow;
                WriteTags(vaultId, tags);

                return Task.FromResult(new UpdateVaultTagResponse { UserMessage = "Tag renamed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error renaming file tag.", ex);
                return Task.FromResult(new UpdateVaultTagResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<DeleteVaultTagResponse> DeleteTagAsync(string vaultId, string tagId, CancellationToken ct = default)
        {
            try
            {
                var tags = ReadTags(vaultId);
                tags.RemoveAll(t => t.ID == tagId);
                WriteTags(vaultId, tags);

                return Task.FromResult(new DeleteVaultTagResponse { Deleted = true, UserMessage = "Tag deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error deleting file tag.", ex);
                return Task.FromResult(new DeleteVaultTagResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<IEnumerable<VaultTag>> ListTagsAsync(string vaultId, CancellationToken ct = default)
        {
            var tags = ReadTags(vaultId).OrderBy(t => t.Name);
            return Task.FromResult<IEnumerable<VaultTag>>(tags);
        }

        public Task<AssignTagToNoteResponse> AssignTagToNoteAsync(string vaultId, string noteId, string tagId, CancellationToken ct = default)
        {
            try
            {
                var vaultPath     = GetVaultPath(vaultId);
                var (metaPath, _) = FindNoteFiles(vaultPath, noteId);

                if (metaPath == null)
                    return Task.FromResult(new AssignTagToNoteResponse { Code = 400, UserMessage = $"Note [{noteId}] not found." });

                var note = ReadJson<VaultNote>(metaPath);
                note.VaultNoteVaultTags ??= new HashSet<VaultNoteVaultTag>();

                if (note.VaultNoteVaultTags.Any(jt => jt.TagID == tagId))
                    return Task.FromResult(new AssignTagToNoteResponse { UserMessage = "Tag already assigned." });

                var tags = ReadTags(vaultId);
                var tag  = tags.FirstOrDefault(t => t.ID == tagId);

                if (tag == null)
                    return Task.FromResult(new AssignTagToNoteResponse { Code = 400, UserMessage = $"Tag [{tagId}] not found in vault." });

                note.VaultNoteVaultTags.Add(new VaultNoteVaultTag
                {
                    ID = _ids.NewId(),
                    NoteID = note.ID,
                    TagID = tag.ID,
                    Tag = tag,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                note.UpdatedAt = DateTime.UtcNow;
                WriteJson(metaPath, note);

                return Task.FromResult(new AssignTagToNoteResponse { UserMessage = "Tag assigned successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error assigning tag to file note.", ex);
                return Task.FromResult(new AssignTagToNoteResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<RemoveTagFromNoteResponse> RemoveTagFromNoteAsync(string vaultId, string noteId, string tagId, CancellationToken ct = default)
        {
            try
            {
                var vaultPath     = GetVaultPath(vaultId);
                var (metaPath, _) = FindNoteFiles(vaultPath, noteId);

                if (metaPath == null)
                    return Task.FromResult(new RemoveTagFromNoteResponse { Code = 400, UserMessage = $"Note [{noteId}] not found." });

                var note    = ReadJson<VaultNote>(metaPath);
                var jtList = note.VaultNoteVaultTags?.ToList() ?? new List<VaultNoteVaultTag>();
                jtList.RemoveAll(jt => jt.TagID == tagId);
                note.VaultNoteVaultTags = new HashSet<VaultNoteVaultTag>(jtList);
                note.UpdatedAt = DateTime.UtcNow;
                WriteJson(metaPath, note);

                return Task.FromResult(new RemoveTagFromNoteResponse { UserMessage = "Tag removed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error removing tag from file note.", ex);
                return Task.FromResult(new RemoveTagFromNoteResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        // ── Link ──────────────────────────────────────────────────────────────

        public Task<CreateVaultRelationResponse> CreateRelationAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string? description = null, CancellationToken ct = default)
        {
            try
            {
                if (!VaultLinkRelationType.IsValid(relationType))
                    return Task.FromResult(new CreateVaultRelationResponse { Code = 400, UserMessage = $"Invalid RelationType '{relationType}'." });

                var relations = ReadRelations(vaultId);
                var existing = relations.FirstOrDefault(r =>
                    r.SourceNoteID == sourceNoteId &&
                    r.TargetNoteID == targetNoteId &&
                    r.RelationType == relationType);

                if (existing != null)
                    return Task.FromResult(new CreateVaultRelationResponse { Link = existing, UserMessage = "Link already exists." });

                var relation = new VaultNoteRelation
                {
                    ID           = Guid.NewGuid().ToString("N"),
                    SourceNoteID = sourceNoteId,
                    TargetNoteID = targetNoteId,
                    RelationType = relationType,
                    Description  = description ?? string.Empty,
                    Weight       = 1.0f,
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow
                };

                relations.Add(relation);
                WriteRelations(vaultId, relations);
                _logger.Log(nameof(FileVaultPersistenceProvider), $"Created file relation [{relation.ID}] {sourceNoteId} →[{relationType}]→ {targetNoteId}");

                return Task.FromResult(new CreateVaultRelationResponse { Link = relation, UserMessage = "Link created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error creating file link.", ex);
                return Task.FromResult(new CreateVaultRelationResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<DeleteVaultLinkResponse> DeleteRelationAsync(string vaultId, string linkId, CancellationToken ct = default)
        {
            try
            {
                var relations = ReadRelations(vaultId);
                relations.RemoveAll(r => r.ID == linkId);
                WriteRelations(vaultId, relations);

                return Task.FromResult(new DeleteVaultLinkResponse { Deleted = true, UserMessage = "Link deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(FileVaultPersistenceProvider), "Error deleting file link.", ex);
                return Task.FromResult(new DeleteVaultLinkResponse { Code = 400, UserMessage = ex.Message });
            }
        }

        public Task<IEnumerable<RelatedNoteResult>> GetRelatedNotesAsync(string vaultId, string noteId, string? relationType = null, CancellationToken ct = default)
        {
            var relations = ReadRelations(vaultId)
                          .Where(r => (r.SourceNoteID == noteId || r.TargetNoteID == noteId) &&
                                      (relationType == null || r.RelationType == relationType))
                          .ToList();

            var results = new List<RelatedNoteResult>();

            foreach (var relation in relations)
            {
                var isOutgoing  = relation.SourceNoteID == noteId;
                var relatedId   = isOutgoing ? relation.TargetNoteID : relation.SourceNoteID;
                var relatedNote = FindNoteMeta(GetVaultPath(vaultId), relatedId);

                results.Add(new RelatedNoteResult
                {
                    Note         = relatedNote,
                    LinkID       = relation.ID,
                    RelationType = relation.RelationType,
                    Direction    = isOutgoing ? LinkDirection.Outgoing : LinkDirection.Incoming,
                    Weight       = relation.Weight,
                    Description  = relation.Description
                });
            }

            return Task.FromResult<IEnumerable<RelatedNoteResult>>(results);
        }

        // ── Path helpers ──────────────────────────────────────────────────────

        private string GetVaultPath(string vaultId)
        {
            foreach (var dir in Directory.GetDirectories(_vaultsRoot))
            {
                var h = Path.Combine(dir, "vault.header.json");
                if (File.Exists(h))
                {
                    var header = ReadJson<VaultHeader>(h);
                    if (header.ID == vaultId) return dir;
                }
            }
            throw new DirectoryNotFoundException($"Vault directory not found for [{vaultId}]");
        }

        private string GetProjectPath(string vaultId, string projectId)
            => Path.Combine(GetVaultPath(vaultId), "projects", projectId);

        private string GetSessionPath(string vaultId, string? projectId, string sessionId)
        {
            var root = string.IsNullOrWhiteSpace(projectId)
                ? Path.Combine(GetVaultPath(vaultId), "sessions")
                : Path.Combine(GetProjectPath(vaultId, projectId), "sessions");
            return Path.Combine(root, sessionId);
        }

        private string GetSessionHeaderPath(string vaultId, string? projectId, string sessionId)
            => Path.Combine(GetSessionPath(vaultId, projectId, sessionId), "session.header.json");

        private string GetNotesPath(string vaultId, string? projectId)
            => string.IsNullOrWhiteSpace(projectId)
                ? Path.Combine(GetVaultPath(vaultId), "notes")
                : Path.Combine(GetProjectPath(vaultId, projectId), "notes");

        private VaultSession FindSession(string vaultId, string sessionId)
        {
            var sessions = ListSessionsAsync(vaultId).GetAwaiter().GetResult();
            var session  = sessions.FirstOrDefault(s => s.ID == sessionId);
            if (session == null) throw new FileNotFoundException($"Session [{sessionId}] not found in vault [{vaultId}]");
            return session;
        }

        private static IEnumerable<VaultSession> ReadSessionsFromDir(string dir)
        {
            if (!Directory.Exists(dir)) yield break;
            foreach (var sessionDir in Directory.GetDirectories(dir))
            {
                var h = Path.Combine(sessionDir, "session.header.json");
                if (File.Exists(h)) yield return ReadJson<VaultSession>(h);
            }
        }

        private static VaultNote? FindNoteMeta(string vaultPath, string noteId)
        {
            var (metaPath, _) = FindNoteFiles(vaultPath, noteId);
            return metaPath != null ? ReadJson<VaultNote>(metaPath) : null;
        }

        private static (string? metaPath, string? mdPath) FindNoteFiles(string vaultPath, string noteId)
        {
            // Check vault-level notes
            var vaultNotesDir = Path.Combine(vaultPath, "notes");
            var vaultMeta = Path.Combine(vaultNotesDir, $"{noteId}.meta.json");
            var vaultMd   = Path.Combine(vaultNotesDir, $"{noteId}.md");
            if (File.Exists(vaultMeta)) return (vaultMeta, vaultMd);

            // Check project-level notes
            var projectsRoot = Path.Combine(vaultPath, "projects");
            if (!Directory.Exists(projectsRoot)) return (null, null);

            foreach (var pDir in Directory.GetDirectories(projectsRoot))
            {
                var meta = Path.Combine(pDir, "notes", $"{noteId}.meta.json");
                var md   = Path.Combine(pDir, "notes", $"{noteId}.md");
                if (File.Exists(meta)) return (meta, md);
            }

            return (null, null);
        }

        private static void ReadNotesFromDir(string notesDir, List<VaultNote> results, string? sessionId, string? keyword, string? tag, DateTime? createdAfter, DateTime? createdBefore, DateTime? updatedAfter, DateTime? updatedBefore)
        {
            if (!Directory.Exists(notesDir)) return;

            foreach (var metaFile in Directory.GetFiles(notesDir, "*.meta.json"))
            {
                try
                {
                    var note = ReadJson<VaultNote>(metaFile);
                    if (sessionId     != null && note.SessionID != sessionId) continue;
                    if (keyword       != null && !NoteMatchesKeyword(note, keyword)) continue;
                    if (tag           != null && !(note.VaultNoteVaultTags?.Any(jt => jt.Tag.Name == tag) ?? false)) continue;
                    if (createdAfter  != null && note.CreatedAt < createdAfter)  continue;
                    if (createdBefore != null && note.CreatedAt > createdBefore) continue;
                    if (updatedAfter  != null && note.UpdatedAt < updatedAfter)  continue;
                    if (updatedBefore != null && note.UpdatedAt > updatedBefore) continue;
                    results.Add(note);
                }
                catch { /* skip corrupt meta */ }
            }
        }

        private static bool NoteMatchesKeyword(VaultNote note, string keyword)
        {
            var k = keyword.ToLowerInvariant();
            return (note.Title?.ToLowerInvariant().Contains(k) ?? false)
                || (note.Content?.ToLowerInvariant().Contains(k) ?? false);
        }

        // ── Tag file helpers ──────────────────────────────────────────────────

        private string GetTagsPath(string vaultId)
            => Path.Combine(GetVaultPath(vaultId), "tags.json");

        private List<VaultTag> ReadTags(string vaultId)
        {
            var path = GetTagsPath(vaultId);
            if (!File.Exists(path)) return new List<VaultTag>();
            return JsonSerializer.Deserialize<List<VaultTag>>(File.ReadAllText(path), JsonOptions)
                   ?? new List<VaultTag>();
        }

        private void WriteTags(string vaultId, List<VaultTag> tags)
            => File.WriteAllText(GetTagsPath(vaultId), JsonSerializer.Serialize(tags, JsonOptions));

        // ── Relation file helpers ─────────────────────────────────────────────

        private string GetRelationsPath(string vaultId)
            => Path.Combine(GetVaultPath(vaultId), "relations.json");

        private List<VaultNoteRelation> ReadRelations(string vaultId)
        {
            var path = GetRelationsPath(vaultId);
            if (!File.Exists(path)) return new List<VaultNoteRelation>();
            return JsonSerializer.Deserialize<List<VaultNoteRelation>>(File.ReadAllText(path), JsonOptions)
                   ?? new List<VaultNoteRelation>();
        }

        private void WriteRelations(string vaultId, List<VaultNoteRelation> relations)
            => File.WriteAllText(GetRelationsPath(vaultId), JsonSerializer.Serialize(relations, JsonOptions));

        // ── JSON helpers ──────────────────────────────────────────────────────

        private static T ReadJson<T>(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                   ?? throw new InvalidOperationException($"Failed to deserialize {path}");
        }

        private static void WriteJson<T>(string path, T obj)
            => File.WriteAllText(path, JsonSerializer.Serialize(obj, JsonOptions));
    }
}
