using System;
using System.Collections.Generic;
using System.Linq;
using AVA.Vault.Core.Models;

namespace AVA.Vault.Core.Graph
{
    public class NoteGraph
    {
        public Dictionary<string, List<string>> Links { get; set; } = new();

        public void BuildGraph(IEnumerable<MarkdownNote> notes)
        {
            Links.Clear();

            foreach (var note in notes)
            {
                var title = note.Title?.Trim();
                if (string.IsNullOrWhiteSpace(title)) continue;

                AddNode(title);

                foreach (var link in note.Links.Distinct())
                {
                    var trimmed = link.Trim();
                    AddEdge(title, trimmed);
                }
            }
        }

        public void AddNode(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;

            id = id.Trim();
            if (!Links.ContainsKey(id))
                Links[id] = new List<string>();
        }

        public void AddEdge(string fromId, string toId)
        {
            if (string.IsNullOrWhiteSpace(fromId) || string.IsNullOrWhiteSpace(toId)) return;

            fromId = fromId.Trim();
            toId = toId.Trim();

            AddNode(fromId);
            AddNode(toId);

            if (!Links[fromId].Contains(toId))
                Links[fromId].Add(toId);
        }

        public List<string> GetLinkedNotes(string title)
        {
            return Links.TryGetValue(title.Trim(), out var linked) ? linked : new List<string>();
        }

        public bool AreNotesLinked(string sourceTitle, string targetTitle)
        {
            return Links.TryGetValue(sourceTitle.Trim(), out var linked) && linked.Contains(targetTitle.Trim());
        }

        public IEnumerable<string> AllNodes() => Links.Keys;

        public IEnumerable<(string From, string To)> AllEdges()
        {
            foreach (var from in Links.Keys)
            {
                foreach (var to in Links[from])
                {
                    yield return (from, to);
                }
            }
        }
    }
}
