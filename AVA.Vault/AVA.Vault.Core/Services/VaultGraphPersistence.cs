using System;
using System.IO;
using System.Threading.Tasks;
using AVA.Vault.Core.Graph;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Utils;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Persists and reloads the NoteGraph associated with a vault.
    /// </summary>
    public sealed class VaultGraphPersistence
    {
        private readonly VaultLogger _logger;

        public VaultGraphPersistence(VaultLogger logger)
        {
            _logger = logger;
        }

        public async Task SaveGraphAsync(NoteGraph graph, string vaultPath)
        {
            try
            {
                Directory.CreateDirectory(vaultPath);
                var path = Path.Combine(vaultPath, "vault.graph.json");
                var json = VaultSerializer.ToJson(graph);
                await File.WriteAllTextAsync(path, json);
                _logger.Log(nameof(VaultGraphPersistence), "Vault graph persisted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(VaultGraphPersistence), "Failed to persist vault graph.", ex);
            }
        }

        public async Task<NoteGraph> LoadGraphAsync(string vaultPath)
        {
            try
            {
                var path = Path.Combine(vaultPath, "vault.graph.json");
                if (!File.Exists(path))
                    return new NoteGraph();

                var json = await File.ReadAllTextAsync(path);
                return VaultSerializer.FromJson<NoteGraph>(json) ?? new NoteGraph();
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(VaultGraphPersistence), "Failed to load vault graph.", ex);
                return new NoteGraph();
            }
        }
    }
}
