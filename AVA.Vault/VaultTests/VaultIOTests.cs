using NUnit.Framework;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Services;
using System.IO;
using System.Linq;
using AVA.Vault.Core.Parsing;

namespace VaultTests
{
    public class VaultIOTests
    {
        private string _vaultPath;
        private VaultService _vault;

        [SetUp]
        public void Setup()
        {
            _vaultPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(_vaultPath, "notes"));

            _vault = new VaultService("DiskVault");
            _vault.AddNote(new MarkdownNote("NoteOne", "# NoteOne Content with #tag and [[Link]]"));
            _vault.AddNote(new MarkdownNote("NoteTwo", "# NoteTwo Content with [[NoteOne]]"));
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_vaultPath))
                Directory.Delete(_vaultPath, true);
        }

        [Test]
        public void CanSaveAndReloadNotes()
        {
            var parser = new NoteParser();

            var note1 = parser.Parse("# NoteOne\nContent with #tag and [[Link]]");
            var note2 = parser.Parse("# NoteTwo\nLinks to [[NoteOne]]");

            _vault.AddNote(note1);
            _vault.AddNote(note2);

            VaultIO.SaveNotesToDisk(_vault, _vaultPath);

            var loadedVault = new VaultService("Reloaded");
            VaultIO.LoadNotesFromDisk(loadedVault, _vaultPath);

            Assert.AreEqual(2, loadedVault.Notes.Count);
            Assert.IsTrue(loadedVault.Notes.Any(n => n.Title == "NoteOne"));
            Assert.IsTrue(loadedVault.Notes.Any(n => n.Title == "NoteTwo"));
        }

    }
}
