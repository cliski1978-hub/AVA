using Microsoft.EntityFrameworkCore;
using CliskiCore.DbAPI.Interfaces;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Interfaces
{
    /// <summary>
    /// Domain-specific DbContext contract for the Vault module.
    /// Extends the CliskiCore.DbAPI IDbContext to expose all Vault entities.
    /// </summary>
    public interface IVaultDbContext : IDbContext
    {
        DbSet<ActivityLogEntry> ActivityLog { get; set; }
        DbSet<VaultHeader> VaultHeaders { get; set; }
        DbSet<VaultProject> VaultProjects { get; set; }
        DbSet<VaultSession> VaultSessions { get; set; }
        DbSet<VaultFileRef> VaultFileRefs { get; set; }
        DbSet<VaultNote> VaultNotes { get; set; }
        DbSet<VaultTag> VaultTags { get; set; }
        DbSet<VaultNoteRelation> VaultNoteRelations { get; set; }
        DbSet<VaultGraph> VaultGraphs { get; set; }
        DbSet<VaultMetadata> VaultMetadata { get; set; }
    }
}
