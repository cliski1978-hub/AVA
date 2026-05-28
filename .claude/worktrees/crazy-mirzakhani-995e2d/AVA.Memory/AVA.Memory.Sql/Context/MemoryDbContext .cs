using AVA.Memory.Core.Models;
using AVA.Memory.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AVA.Memory.Sql.Context
{
    /// <summary>
    /// EF Core DbContext for AVA memory persistence.
    /// Stores MemoryRecords, AssociationEdges, Vectors, Tags, and Metadata.
    /// </summary>
    public class MemoryDbContext : DbContext
    {
        public MemoryDbContext(DbContextOptions<MemoryDbContext> options)
            : base(options)
        {
        }

        // Tables
        public DbSet<MemoryRecord> MemoryRecords { get; set; }
        public DbSet<AssociationEdge> AssociationEdges { get; set; }
        public DbSet<MemoryVector> MemoryVectors { get; set; }
        public DbSet<MemoryTag> MemoryTags { get; set; }
        public DbSet<MemoryMetadata> MemoryMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply entity type configs (keeps this clean)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MemoryDbContext).Assembly);
        }
    }
}
