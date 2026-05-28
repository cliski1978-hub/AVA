using NUnit.Framework;
using System;
using System.Collections.Generic;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Services;
using AVA.Vault.Core.Logger;

namespace AVA.Vault.Core.Tests
{
    public class VaultFilterNotesTests
    {
        private VaultService _vault;
        private VaultProjectManager _manager;

        [SetUp]
        public void Setup()
        {
            _vault = new VaultService("TestVault");

            _vault.Notes.Add(new MarkdownNote
            {
                Id = "1",
                Title = "Note A",
                Content = "Alpha",
                Tags = new List<string> { "X", "Common" },
                Created = DateTime.UtcNow.AddDays(-2)
            });

            _vault.Notes.Add(new MarkdownNote
            {
                Id = "2",
                Title = "Note B",
                Content = "Beta",
                Tags = new List<string> { "Y", "Common" },
                Created = DateTime.UtcNow.AddDays(-1)
            });

            // ✅ Create a logger for this vault
            var logger = new VaultLogger(_vault.Config);
            _manager = new VaultProjectManager(_vault, logger);
        }

        [Test]
        public void Filter_ByTag_ReturnsCorrect()
        {
            var opts = new VaultQueryOptions { Tag = "X" };
            var results = _manager.FilterNotes(opts);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Note A", results[0].Title);
        }

        [Test]
        public void Filter_ByRequiredTags_ReturnsIntersection()
        {
            var opts = new VaultQueryOptions { RequiredTags = new List<string> { "Common", "Y" } };
            var results = _manager.FilterNotes(opts);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Note B", results[0].Title);
        }

        [Test]
        public void Filter_ByDateRange_Works()
        {
            var opts = new VaultQueryOptions
            {
                After = DateTime.UtcNow.AddDays(-3),
                Before = DateTime.UtcNow.AddDays(-1).AddHours(-1)
            };

            var results = _manager.FilterNotes(opts);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Note A", results[0].Title);
        }

        [Test]
        public void Filter_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _manager.FilterNotes(null));
        }
    }
}
