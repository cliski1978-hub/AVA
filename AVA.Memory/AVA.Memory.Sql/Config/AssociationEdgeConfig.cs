using AVA.Memory.Core.Models;
using AVA.Memory.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AVA.Memory.Sql.Config
{
    public class AssociationEdgeConfig : IEntityTypeConfiguration<AssociationEdge>
    {
        public void Configure(EntityTypeBuilder<AssociationEdge> builder)
        {
            // Primary key
            builder.HasKey(e => e.ID);

            builder.Property(e => e.ID)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(e => e.Type)
                   .HasMaxLength(128);

            builder.Property(e => e.Weight).IsRequired();
            builder.Property(e => e.UpdatedAt).IsRequired();

            // Foreign key to FromRecord
            builder.HasOne(e => e.FromRecord)
                   .WithMany()
                   .HasForeignKey(e => e.FromRecordID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to ToRecord
            builder.HasOne(e => e.ToRecord)
                   .WithMany()
                   .HasForeignKey(e => e.ToRecordID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes for traversal
            builder.HasIndex(e => e.FromRecordID);
            builder.HasIndex(e => e.ToRecordID);
        }
    }
}
