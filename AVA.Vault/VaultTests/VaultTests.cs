using NUnit.Framework;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Services;
using System.Linq;
using AVA.Vault.Core.Parsing;

namespace VaultTests
{
    public class VaultTests
    {
        private VaultService _vault;

        [SetUp]
        public void Init()
        {
            _vault = new VaultService("Test Vault");
        }

        [Test]
        public void CanAddNoteAndFindByTitle()
        {
            var note = new MarkdownNote("Welcome", "Some #content with a [[Link]]");
            _vault.AddNote(note);

            var found = _vault.FindByTitle("Welcome");
            Assert.IsNotNull(found);
            Assert.AreEqual("Welcome", found.Title);
        }

        [Test]
        public void CanRemoveNote()
        {
            var note = new MarkdownNote("ToRemove", "...");
            _vault.AddNote(note);
            _vault.RemoveNote(note.Id);

            var found = _vault.FindByTitle("ToRemove");
            Assert.IsNull(found);
        }

        [Test]
        public void CanFindNotesByTag()
        {
            var parser = new NoteParser();
            var note = parser.Parse("# Tagged\nIncludes #hello");
            _vault.AddNote(note);

            var results = _vault.FindByTag("hello").ToList();
            Assert.IsTrue(results.Any());
            Assert.AreEqual("Tagged", results[0].Title);
        }


        [Test]
        public void GraphBuildsOnAdd()
        {
            var parser = new NoteParser();
            var note = parser.Parse("# A\nLinks to [[B]] and [[C]]");
            _vault.AddNote(note);

            var links = _vault.Graph.GetLinkedNotes("A");
            CollectionAssert.Contains(links, "B");
            CollectionAssert.Contains(links, "C");
        }

    }
}
