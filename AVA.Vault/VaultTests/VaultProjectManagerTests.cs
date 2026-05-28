using NUnit.Framework;
using System;
using System.Collections.Generic;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Services;
using AVA.Vault.Core.Logger;

namespace AVA.Vault.Core.Tests
{
    public class VaultProjectManagerTests
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
                Tags = new List<string> { "ProjectX" }
            });

            _vault.Notes.Add(new MarkdownNote
            {
                Id = "2",
                Title = "Note B",
                Content = "Beta",
                Tags = new List<string> { "ProjectX", "Research" }
            });

            // ✅ Create a logger for this vault
            var logger = new VaultLogger(_vault.Config);
            _manager = new VaultProjectManager(_vault, logger);
        }

        [Test]
        public void GetNotesForProject_ReturnsCorrectCount()
        {
            var notes = _manager.GetNotesForProject("ProjectX");
            Assert.AreEqual(2, notes.Count);
        }

        [Test]
        public void AddProjectTag_AddsNewTag()
        {
            var newNote = new MarkdownNote
            {
                Id = "3",
                Title = "Note C",
                Content = "Gamma",
                Tags = new List<string>()
            };

            _manager.AddProjectTag("ProjectZ", newNote);
            Assert.Contains("ProjectZ", newNote.Tags);
        }

        [Test]
        public void GetAllProjectTags_ReturnsDistinctSet()
        {
            var tags = _manager.GetAllProjectTags();
            Assert.Contains("ProjectX", tags);
            Assert.Contains("Research", tags);
        }

        [Test]
        public void NoteBelongsToProject_CorrectlyIdentifiesTags()
        {
            var note = _vault.Notes[0];
            Assert.IsTrue(_manager.NoteBelongsToProject(note, "ProjectX"));
            Assert.IsFalse(_manager.NoteBelongsToProject(note, "Nonexistent"));
        }
    }
}
