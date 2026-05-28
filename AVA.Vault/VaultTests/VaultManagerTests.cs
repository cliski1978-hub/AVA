using NUnit.Framework;
using AVA.Vault.Core.Services;
using System.IO;
using System.Linq;

namespace VaultTests
{
    public class VaultManagerTests
    {
        private string _testPath;
        private VaultManager _manager;

        [SetUp]
        public void Setup()
        {
            _testPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testPath);
            _manager = new VaultManager(_testPath);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
        }

        [Test]
        public void CanCreateAndRetrieveVault()
        {
            var vault = _manager.CreateVault("MyVault");
            var retrieved = _manager.GetVaultById(vault.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("MyVault", retrieved.Name);
        }

        [Test]
        public void CanListVaults()
        {
            _manager.CreateVault("Vault1");
            _manager.CreateVault("Vault2");
            var vaults = _manager.ListVaults().ToList();
            Assert.AreEqual(2, vaults.Count);
        }

        [Test]
        public void CanDeleteVault()
        {
            var vault = _manager.CreateVault("TempVault");
            _manager.DeleteVault(vault.Id);
            var listed = _manager.ListVaults().ToList();
            Assert.False(listed.Any(v => v.DisplayName == "TempVault"));
        }
    }
}
