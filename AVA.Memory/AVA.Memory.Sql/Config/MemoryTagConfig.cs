using AVA.Memory.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AVA.Memory.Sql.Config
{
    /// <summary>
    /// EF Core configuration for MemoryTag entity.
    /// Stores tags linked to a MemoryRecord.
    /// </summary>
    public class MemoryTagConfig : IEntityTypeConfiguration<MemoryTag>
    {
        public void Configure(EntityTypeBuilder<MemoryTag> builder)
        {
            builder.HasKey(t => t.ID);

            builder.Property(t => t.Tag)
                   .IsRequired()
                   .HasMaxLength(128);

            builder.HasIndex(t => new { t.RecordID, t.Tag })
                   .IsUnique(); // prevent duplicate tags per record
        }
    }
}
