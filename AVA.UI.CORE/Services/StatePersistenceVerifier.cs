using System.Text.Json;
using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Services
{
    /// <summary>
    /// Result payload for state persistence verification.
    /// </summary>
    public sealed class StateVerificationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether verification succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the verification summary message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lightweight verification helper for state-model JSON round trips and settings persistence.
    /// </summary>
    public sealed class StatePersistenceVerifier
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        private readonly VaultWorkspaceFileService _vaultWorkspace;

        public StatePersistenceVerifier(VaultWorkspaceFileService vaultWorkspace)
        {
            _vaultWorkspace = vaultWorkspace;
        }

        public async Task<StateVerificationResult> VerifyAsync()
        {
            var originalVaults = CloneVaults(_vaultWorkspace.Vaults);

            try
            {
                var sample = BuildSampleVaultGraph();

                VerifyRoundTrip(sample);
                await _vaultWorkspace.SaveVaultsAsync(new List<VaultState> { sample });
                var reloaded = await _vaultWorkspace.LoadVaultsAsync();

                var ok = reloaded.Count == 1
                    && reloaded[0].Projects.Count == 1
                    && reloaded[0].Projects[0].Sessions.Count == 1
                    && reloaded[0].Projects[0].Sessions[0].Canvas.Cards.Count == 1;

                return new StateVerificationResult
                {
                    Success = ok,
                    Message = ok
                        ? "State models round-trip and vaults.json persistence verified."
                        : "Vault workspace reload did not match expected structure."
                };
            }
            finally
            {
                await _vaultWorkspace.SaveVaultsAsync(originalVaults);
            }
        }

        private static void VerifyRoundTrip(VaultState vault)
        {
            var json = JsonSerializer.Serialize(vault, JsonOptions);
            var restored = JsonSerializer.Deserialize<VaultState>(json, JsonOptions);

            if (restored == null
                || restored.Projects.Count != vault.Projects.Count
                || restored.Projects[0].Sessions.Count != vault.Projects[0].Sessions.Count
                || restored.Projects[0].Sessions[0].Canvas.Cards.Count !=
                   vault.Projects[0].Sessions[0].Canvas.Cards.Count)
            {
                throw new InvalidOperationException("JSON round-trip verification failed.");
            }
        }

        private static List<VaultState> CloneVaults(List<VaultState> vaults)
        {
            var json = JsonSerializer.Serialize(vaults, JsonOptions);
            return JsonSerializer.Deserialize<List<VaultState>>(json, JsonOptions) ?? new List<VaultState>();
        }

        private static VaultState BuildSampleVaultGraph()
        {
            var sessionId = Guid.NewGuid().ToString();
            var cardId = Guid.NewGuid().ToString();

            return new VaultState
            {
                Name = "Verifier Vault",
                IsExpanded = true,
                Projects = new List<ProjectState>
                {
                    new()
                    {
                        Name = "Verifier Project",
                        IsExpanded = true,
                        FileRefs = new List<FileRef>
                        {
                            new()
                            {
                                Path = @"C:\Temp\verifier.txt",
                                Name = "verifier.txt",
                                CreatedAt = DateTime.UtcNow
                            }
                        },
                        Sessions = new List<SessionState>
                        {
                            new()
                            {
                                SessionId = sessionId,
                                Name = "Verifier Session",
                                CreatedAt = DateTime.UtcNow,
                                LastActiveAt = DateTime.UtcNow,
                                AttachedModelIds = new List<string> { "model-1" },
                                BroadcastGroupIds = new List<string> { "model-1", "model-2" },
                                DefaultModelId = "model-1",
                                Canvas = new SessionCanvasState
                                {
                                    ActiveCardId = cardId,
                                    ShowGrid = true,
                                    SnapToGrid = true,
                                    SavedLayouts = new List<CanvasSnapshot>
                                    {
                                        new()
                                        {
                                            Name = "Default",
                                            CardPositions = new Dictionary<string, CardPosition>
                                            {
                                                [cardId] = new CardPosition
                                                {
                                                    X = 10,
                                                    Y = 20,
                                                    Width = 380,
                                                    Height = 420,
                                                    ZIndex = 2
                                                }
                                            },
                                            CreatedAt = DateTime.UtcNow
                                        }
                                    },
                                    Cards = new List<CardState>
                                    {
                                        new()
                                        {
                                            CardId = cardId,
                                            CardType = "SingleModel",
                                            Title = "Verifier Card",
                                            X = 10,
                                            Y = 20,
                                            Width = 380,
                                            Height = 420,
                                            ModelProfileIds = new List<string> { "model-1" },
                                            ActiveProfileId = "model-1"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
