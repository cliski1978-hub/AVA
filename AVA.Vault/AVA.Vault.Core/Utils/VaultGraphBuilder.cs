using System.Collections.Generic;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Graph;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core.Utils
{
    /// <summary>
    /// Builds a note graph from Markdown notes, detecting inter-note references.
    /// </summary>
    public static class VaultGraphBuilder
    {
        // Maintain a shared VaultLogger for the static utility
        private static readonly VaultLogger _logger =
            new VaultLogger(new VaultInstanceConfig { VaultID = "system-graph" });

        public static NoteGraph BuildGraphFromNotes(IEnumerable<MarkdownNote> notes)
        {
            var graph = new NoteGraph();

            // Precompute links based on content-title referencing
            foreach (var source in notes)
            {
                foreach (var target in notes)
                {
                    if (source != target &&
                        !source.Links.Contains(target.Title) &&
                        source.Content.Contains(target.Title))
                    {
                        source.Links.Add(target.Title);
                        _logger.Log("VaultGraphBuilder", $"Linked '{source.Title}' to '{target.Title}' via content reference.");
                    }
                }
            }

            graph.BuildGraph(notes);
            return graph;
        }
    }
}
