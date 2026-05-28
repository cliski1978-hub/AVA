using NUnit.Framework;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Services;
using System.IO;

namespace VaultTests
{
    public class IVaultManagerComplianceTests
    {
        private IVaultManager _manager;
        private string _tempPath;

        [SetUp]
        public void Setup()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempPath);
            _manager = new VaultManager(_tempPath);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_tempPath))
                Directory.Delete(_tempPath, true);
        }

        [Test]
        public void CanCreateAndAccessVaultViaInterface()
        {
            var vault = _manager.CreateVault("InterfaceVault");
            Assert.IsNotNull(_manager.GetVaultById(vault.Id));
        }

        [Test]
        public void CanListVaultMetadata()
        {
            _manager.CreateVault("IVM1");
            _manager.CreateVault("IVM2");
            var all = _manager.ListVaults();
            Assert.GreaterOrEqual(all.Count(), 2);
        }
    }
}
