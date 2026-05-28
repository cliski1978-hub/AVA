using NUnit.Framework;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Services;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Parsing;
using System.Linq;

namespace VaultTests
{
    public class IVaultComplianceTests
    {
        private IVault _vault;
        private NoteParser _parser;

        [SetUp]
        public void Setup()
        {
            _vault = new VaultService("Interface Vault");
            _parser = new NoteParser();
        }

        [Test]
        public void CanAddAndRetrieveNote()
        {
            var note = _parser.Parse("# My Note Title\nThis note includes #test and [[LinkToOther]]");
            _vault.AddNote(note);
            var found = _vault.FindByTitle("My Note Title");
            Assert.IsNotNull(found);
            Assert.AreEqual("My Note Title", found.Title);
        }

        [Test]
        public void CanSearchByTag()
        {
            var note = _parser.Parse("This note contains #searchable");
            _vault.AddNote(note);

            var results = _vault.FindByTag("searchable");
            Assert.IsTrue(results.Any());
        }
    }
}
