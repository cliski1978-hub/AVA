using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using AVA.Vault.Core.Adapters;
using AVA.Vault.Core.Config;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services;

namespace VaultTests.Parity
{
    // ─────────────────────────────────────────────────────────────────────────
    // Abstract base — same 8 test bodies run against both providers.
    // Concrete fixtures below supply the provider under test.
    // ─────────────────────────────────────────────────────────────────────────
    public abstract class VaultProviderParityTests
    {
        protected IVaultPersistenceProvider Provider = null!;
        protected string VaultId = null!;
        protected string ProjectId = null!;

        protected abstract Task<IVaultPersistenceProvider> BuildProviderAsync();

        [SetUp]
        public async Task SetUp()
        {
            Provider  = await BuildProviderAsync();
            VaultId   = "test-vault-" + Guid.NewGuid().ToString("N")[..8];
            ProjectId = "test-project-" + Guid.NewGuid().ToString("N")[..8];

            // Each test gets a fresh vault and project via the provider itself
            await Provider.CreateVaultAsync("Test Vault", VaultId);
            await Provider.CreateProjectAsync(VaultId, "Test Project", ProjectId);
        }

        // ── 1. CreateNote ─────────────────────────────────────────────────────

        [Test]
        public async Task CreateNote_ReturnsNoteWithCorrectFields()
        {
            var note = await Provider.CreateNoteAsync(VaultId, ProjectId, "My Note", "Hello world");

            Assert.That(note, Is.Not.Null);
            Assert.That(note.ID,        Is.Not.Empty);
            Assert.That(note.VaultID,   Is.EqualTo(VaultId));
            Assert.That(note.Title,     Is.EqualTo("My Note"));
            Assert.That(note.Content,   Is.EqualTo("Hello world"));
            Assert.That(note.CreatedAt, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-5)));
        }

        // ── 2. UpdateNote ─────────────────────────────────────────────────────

        [Test]
        public async Task UpdateNote_PersistsNewTitleAndContent()
        {
            var note = await Provider.CreateNoteAsync(VaultId, ProjectId, "Original", "Old content");

            await Provider.UpdateNoteAsync(VaultId, note.ID, "Updated Title", "New content");

            var loaded = await Provider.GetNoteAsync(VaultId, note.ID);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Title,   Is.EqualTo("Updated Title"));
            Assert.That(loaded!.Content, Is.EqualTo("New content"));
        }

        // ── 3. DeleteNote ─────────────────────────────────────────────────────

        [Test]
        public async Task DeleteNote_NoteNoLongerRetrievable()
        {
            var note = await Provider.CreateNoteAsync(VaultId, ProjectId, "To Delete", "bye");

            await Provider.DeleteNoteAsync(VaultId, note.ID);

            var loaded = await Provider.GetNoteAsync(VaultId, note.ID);
            Assert.That(loaded, Is.Null);
        }

        // ── 4. SearchNotes ────────────────────────────────────────────────────

        [Test]
        public async Task SearchNotes_KeywordMatchesTitleAndContent()
        {
            await Provider.CreateNoteAsync(VaultId, ProjectId, "Alpha note", "contains banana");
            await Provider.CreateNoteAsync(VaultId, ProjectId, "Beta banana", "no match here");
            await Provider.CreateNoteAsync(VaultId, ProjectId, "Gamma note", "nothing relevant");

            var results = (await Provider.SearchNotesAsync(VaultId, keyword: "banana")).ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.Any(n => n.Title == "Alpha note"),  Is.True);
            Assert.That(results.Any(n => n.Title == "Beta banana"), Is.True);
        }

        [Test]
        public async Task SearchNotes_SortByAlphabeticalAscending()
        {
            await Provider.CreateNoteAsync(VaultId, ProjectId, "Zebra", "content");
            await Provider.CreateNoteAsync(VaultId, ProjectId, "Apple", "content");
            await Provider.CreateNoteAsync(VaultId, ProjectId, "Mango", "content");

            var results = (await Provider.SearchNotesAsync(
                VaultId, sortBy: "Alphabetical", sortDescending: false)).ToList();

            Assert.That(results[0].Title, Is.EqualTo("Apple"));
            Assert.That(results[1].Title, Is.EqualTo("Mango"));
            Assert.That(results[2].Title, Is.EqualTo("Zebra"));
        }

        // ── 5. CreateTag ──────────────────────────────────────────────────────

        [Test]
        public async Task CreateTag_TagAppearsInList()
        {
            var tag = await Provider.CreateTagAsync(VaultId, "important");

            var tags = (await Provider.ListTagsAsync(VaultId)).ToList();
            Assert.That(tags.Any(t => t.ID == tag.ID && t.Name == "important"), Is.True);
        }

        [Test]
        public async Task CreateTag_DuplicateNameReturnsExisting()
        {
            var first  = await Provider.CreateTagAsync(VaultId, "duplicate");
            var second = await Provider.CreateTagAsync(VaultId, "duplicate");

            Assert.That(second.ID, Is.EqualTo(first.ID));

            var tags = (await Provider.ListTagsAsync(VaultId)).ToList();
            Assert.That(tags.Count(t => t.Name == "duplicate"), Is.EqualTo(1));
        }

        // ── 6. AssignTag ──────────────────────────────────────────────────────

        [Test]
        public async Task AssignTag_TagAppearsOnNote()
        {
            var note = await Provider.CreateNoteAsync(VaultId, ProjectId, "Tagged Note", "body");
            var tag  = await Provider.CreateTagAsync(VaultId, "sprint");

            await Provider.AssignTagToNoteAsync(VaultId, note.ID, tag.ID);

            var results = (await Provider.SearchNotesAsync(VaultId, tag: "sprint")).ToList();
            Assert.That(results.Any(n => n.ID == note.ID), Is.True);
        }

        [Test]
        public async Task RemoveTag_TagNoLongerOnNote()
        {
            var note = await Provider.CreateNoteAsync(VaultId, ProjectId, "Note", "body");
            var tag  = await Provider.CreateTagAsync(VaultId, "temporary");

            await Provider.AssignTagToNoteAsync(VaultId, note.ID, tag.ID);
            await Provider.RemoveTagFromNoteAsync(VaultId, note.ID, tag.ID);

            var results = (await Provider.SearchNotesAsync(VaultId, tag: "temporary")).ToList();
            Assert.That(results.Any(n => n.ID == note.ID), Is.False);
        }

        // ── 7. CreateLink ─────────────────────────────────────────────────────

        [Test]
        public async Task CreateLink_LinkPersistedCorrectly()
        {
            var source = await Provider.CreateNoteAsync(VaultId, ProjectId, "Source", "src");
            var target = await Provider.CreateNoteAsync(VaultId, ProjectId, "Target", "tgt");

            var link = await Provider.CreateLinkAsync(
                VaultId, source.ID, target.ID, VaultLinkRelationType.References);

            Assert.That(link,            Is.Not.Null);
            Assert.That(link.ID,         Is.Not.Empty);
            Assert.That(link.SourceNoteID, Is.EqualTo(source.ID));
            Assert.That(link.TargetNoteID, Is.EqualTo(target.ID));
            Assert.That(link.RelationType, Is.EqualTo(VaultLinkRelationType.References));
        }

        [Test]
        public async Task CreateLink_DuplicateLinkReturnsExisting()
        {
            var source = await Provider.CreateNoteAsync(VaultId, ProjectId, "S", "s");
            var target = await Provider.CreateNoteAsync(VaultId, ProjectId, "T", "t");

            var first  = await Provider.CreateLinkAsync(VaultId, source.ID, target.ID, VaultLinkRelationType.RelatedTo);
            var second = await Provider.CreateLinkAsync(VaultId, source.ID, target.ID, VaultLinkRelationType.RelatedTo);

            Assert.That(second.ID, Is.EqualTo(first.ID));
        }

        // ── 8. GetRelatedNotes ────────────────────────────────────────────────

        [Test]
        public async Task GetRelatedNotes_ReturnsBothDirections()
        {
            var noteA = await Provider.CreateNoteAsync(VaultId, ProjectId, "A", "a");
            var noteB = await Provider.CreateNoteAsync(VaultId, ProjectId, "B", "b");
            var noteC = await Provider.CreateNoteAsync(VaultId, ProjectId, "C", "c");

            // A → B, C → A
            await Provider.CreateLinkAsync(VaultId, noteA.ID, noteB.ID, VaultLinkRelationType.ParentOf);
            await Provider.CreateLinkAsync(VaultId, noteC.ID, noteA.ID, VaultLinkRelationType.ChildOf);

            var related = (await Provider.GetRelatedNotesAsync(VaultId, noteA.NoteID)).ToList();

            Assert.That(related.Count, Is.EqualTo(2));
            Assert.That(related.Any(r => r.Note?.ID == noteB.NoteID), Is.True);
            Assert.That(related.Any(r => r.Note?.ID == noteC.NoteID), Is.True);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Concrete fixture: Database (EF Core InMemory)
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// DB provider parity tests run against the live AvaVault database.
    /// Requires:
    ///   1. AVA_TEST_DB environment variable set to the AvaVault connection string.
    ///   2. VaultDbImpl migration applied (dotnet ef database update --context VaultDbContext).
    ///
    /// Test data is isolated by unique VaultIds and cleaned up in TearDown.
    ///
    /// Example:
    ///   AVA_TEST_DB="Server=4D-C76\SQLEXPRESS;Database=AvaVault;Trusted_Connection=True;TrustServerCertificate=True;"
    /// </summary>
    [TestFixture]
    public class DbProviderParityTests : VaultProviderParityTests
    {
        private VaultDbContext? _db;
        private DbContextOptions<VaultDbContext>? _options;
        private const string EnvVar = "AVA_TEST_DB";

        protected override Task<IVaultPersistenceProvider> BuildProviderAsync()
        {
            var connStr = Environment.GetEnvironmentVariable(EnvVar);
            Assume.That(connStr, Is.Not.Null.And.Not.Empty,
                $"DB parity tests skipped — set {EnvVar} to the AvaVault connection string and apply VaultDbImpl migration first.");

            _options = new DbContextOptionsBuilder<VaultDbContext>()
                .UseSqlServer(connStr)
                .Options;

            _db = new VaultDbContext(_options);

            var factory = new TestDbContextFactory(_options);
            var logger  = new VaultLogger(new VaultInstanceConfig());
            var ids     = new VaultIdService();

            return Task.FromResult<IVaultPersistenceProvider>(
                new DbVaultPersistenceProvider(factory, logger, ids));
        }

        [TearDown]
        public async Task TearDown()
        {
            // Clean up test vaults — identified by our unique VaultId prefix
            if (_db != null && VaultId != null)
            {
                try
                {
                    var vault = _db.VaultHeaders.FirstOrDefault(v => v.ID == VaultId);
                    if (vault != null)
                    {
                        _db.VaultHeaders.Remove(vault);
                        await _db.SaveChangesAsync();
                    }
                }
                catch { /* best-effort cleanup */ }
                finally { _db.Dispose(); }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Concrete fixture: File system (temp directory)
    // ─────────────────────────────────────────────────────────────────────────
    [TestFixture]
    public class FileProviderParityTests : VaultProviderParityTests
    {
        private string _tempRoot = string.Empty;

        protected override Task<IVaultPersistenceProvider> BuildProviderAsync()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "AVA_ParityTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempRoot);

            var manager = new VaultManager(_tempRoot);
            var logger  = new VaultLogger(new VaultInstanceConfig());
            var ids     = new VaultIdService();

            return Task.FromResult<IVaultPersistenceProvider>(
                new FileVaultPersistenceProvider(manager, logger, ids, _tempRoot));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test helper: wraps a pre-built DbContext as an IDbContextFactory
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// VaultDbContext with SQL Server-specific column types normalised for SQLite.
    /// Used in tests only — production always targets SQL Server.
    /// </summary>
    internal class TestVaultDbContext : VaultDbContext
    {
        public TestVaultDbContext(DbContextOptions<VaultDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Replace SQL Server-specific column types with SQLite equivalents
            // using the fluent builder — direct prop mutation doesn't override
            // data annotations reliably in EF Core 9.
            foreach (var et in modelBuilder.Model.GetEntityTypes())
            {
                // Skip shared/junction types — modelBuilder.Entity() cannot target them
                if (et.HasSharedClrType) continue;

                var eb = modelBuilder.Entity(et.ClrType);
                foreach (var prop in et.GetProperties())
                {
                    var ct = prop.GetColumnType();
                    if (ct == null) continue;
                    var lower = ct.ToLowerInvariant();

                    if (lower.StartsWith("nvarchar") ||
                        lower is "datetime" or "datetime2")
                    {
                        eb.Property(prop.Name).HasColumnType("TEXT");
                    }
                }
            }
        }
    }

    internal class TestDbContextFactory : Microsoft.EntityFrameworkCore.IDbContextFactory<VaultDbContext>
    {
        private readonly DbContextOptions<VaultDbContext> _options;

        public TestDbContextFactory(DbContextOptions<VaultDbContext> options)
        {
            _options = options;
        }

        public VaultDbContext CreateDbContext() => new TestVaultDbContext(_options);

        public Task<VaultDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(CreateDbContext());
    }
}
