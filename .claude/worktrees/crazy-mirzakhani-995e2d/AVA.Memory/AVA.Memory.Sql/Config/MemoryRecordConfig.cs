using AVA.Memory.Core.Models;
using AVA.Memory.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AVA.Memory.Sql.Config
{
    /// <summary>
    /// EF Core configuration for MemoryRecord entity.
    /// Defines schema and relationships to normalized child entities.
    /// </summary>
    public class MemoryRecordConfig : IEntityTypeConfiguration<MemoryRecord>
    {
        public void Configure(EntityTypeBuilder<MemoryRecord> builder)
        {
            builder.HasKey(r => r.ID);

            builder.Property(r => r.ID)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(r => r.Text)
                   .HasMaxLength(4000);

            builder.Property(r => r.EpisodeId).HasMaxLength(64);
            builder.Property(r => r.ContextId).HasMaxLength(64);
            builder.Property(r => r.Source).HasMaxLength(256);

            builder.Property(r => r.Salience).IsRequired();
            builder.Property(r => r.Novelty).IsRequired();
            builder.Property(r => r.Frequency).IsRequired();
            builder.Property(r => r.DecayRate).IsRequired();

            builder.Property(r => r.CreatedAt).IsRequired();
            builder.Property(r => r.UpdatedAt).IsRequired();
            builder.Property(r => r.LastAccessedAt).IsRequired();

            // Relationships
            builder.HasMany(r => r.Vectors)
                   .WithOne(v => v.Record)
                   .HasForeignKey(v => v.RecordID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Tags)
                   .WithOne(t => t.Record)
                   .HasForeignKey(t => t.RecordID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Metadata)
                   .WithOne(m => m.Record)
                   .HasForeignKey(m => m.RecordID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.OutgoingEdges)
                   .WithOne(e => e.FromRecord)
                   .HasForeignKey(e => e.FromRecordID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.IncomingEdges)
                   .WithOne(e => e.ToRecord)
                   .HasForeignKey(e => e.ToRecordID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.CreatedAt);
            builder.HasIndex(r => r.UpdatedAt);
            builder.HasIndex(r => r.LastAccessedAt);
            builder.HasIndex(r => r.Salience);
        }
    }
}
