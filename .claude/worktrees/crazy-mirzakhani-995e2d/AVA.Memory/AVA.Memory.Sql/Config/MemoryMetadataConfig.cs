using AVA.Memory.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AVA.Memory.Sql.Config
{
    /// <summary>
    /// EF Core configuration for MemoryMetadata entity.
    /// Stores key/value metadata linked to a MemoryRecord.
    /// </summary>
    public class MemoryMetadataConfig : IEntityTypeConfiguration<MemoryMetadata>
    {
        public void Configure(EntityTypeBuilder<MemoryMetadata> builder)
        {
            builder.HasKey(m => m.ID);

            builder.Property(m => m.Key)
                   .IsRequired()
                   .HasMaxLength(128);

            builder.Property(m => m.Value)
                   .HasMaxLength(2000);

            builder.HasIndex(m => new { m.RecordID, m.Key })
                   .IsUnique(); // one key per record
        }
    }
}
