using NUnit.Framework;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Graph;
using AVA.Vault.Core.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace VaultTests
{
    public class NoteGraphTests
    {
        private NoteParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new NoteParser();
        }

        [Test]
        public void CanBuildGraphAndRetrieveLinks()
        {
            var notes = new List<MarkdownNote>
            {
               _parser.Parse("# Alpha\nSee [[Beta]] and [[Gamma]]"),
                _parser.Parse("# Beta\\nSee [[Gamma]]"),
                _parser.Parse("# Gamma")
            };

            var graph = new NoteGraph();
            graph.BuildGraph(notes);

            var alphaLinks = graph.GetLinkedNotes("Alpha");
            CollectionAssert.AreEquivalent(new[] { "Beta", "Gamma" }, alphaLinks);
        }

        [Test]
        public void ReportsEdgesCorrectly()
        {
            var notes = new List<MarkdownNote>
            {
                _parser.Parse("# Node1\n[[Node2]]"),
                _parser.Parse("# Node2\n[[Node3]]"),
                _parser.Parse("# Node3")
            };

            var graph = new NoteGraph();
            graph.BuildGraph(notes);

            var edges = graph.AllEdges().ToList();

            Assert.IsTrue(edges.Contains(("Node1", "Node2")), "Missing edge from Node1 to Node2");
            Assert.IsTrue(edges.Contains(("Node2", "Node3")), "Missing edge from Node2 to Node3");
        }


        [Test]
        public void HandlesEmptyNotesGracefully()
        {
            var graph = new NoteGraph();
            graph.BuildGraph(new List<MarkdownNote>());
            Assert.IsEmpty(graph.AllNodes());
        }
    }
}
