using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CliskiCore.DbAPI.Interfaces;
using CliskiCore.DbAPI.Adapters;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Adapters
{
    /// <summary>
    /// Adapts VaultDbContext to the CliskiCore.DbAPI standard interface.
    /// </summary>
    public class VaultDbContextAdapter
        : DbContextAdapterBase<VaultDbContext>, IVaultDbContext
    {
        private IDbContextTransaction? _transaction;

        public VaultDbContextAdapter(VaultDbContext context) : base(context) { }

        public DbSet<ActivityLogEntry> ActivityLog
        {
            get => _context.ActivityLog;
            set => _context.ActivityLog = value;
        }

        public DbSet<VaultHeader> VaultHeaders
        {
            get => _context.VaultHeaders;
            set => _context.VaultHeaders = value;
        }

        public DbSet<VaultProject> VaultProjects
        {
            get => _context.VaultProjects;
            set => _context.VaultProjects = value;
        }

        public DbSet<VaultSession> VaultSessions
        {
            get => _context.VaultSessions;
            set => _context.VaultSessions = value;
        }

        public DbSet<VaultFileRef> VaultFileRefs
        {
            get => _context.VaultFileRefs;
            set => _context.VaultFileRefs = value;
        }

        public DbSet<VaultNote> VaultNotes
        {
            get => _context.VaultNotes;
            set => _context.VaultNotes = value;
        }

        public DbSet<VaultTag> VaultTags
        {
            get => _context.VaultTags;
            set => _context.VaultTags = value;
        }

        public DbSet<VaultNoteRelation> VaultNoteRelations
        {
            get => _context.VaultNoteRelations;
            set => _context.VaultNoteRelations = value;
        }

        public DbSet<VaultGraph> VaultGraphs
        {
            get => _context.VaultGraphs;
            set => _context.VaultGraphs = value;
        }

        public DbSet<VaultMetadata> VaultMetadata
        {
            get => _context.VaultMetadata;
            set => _context.VaultMetadata = value;
        }
    }
}
