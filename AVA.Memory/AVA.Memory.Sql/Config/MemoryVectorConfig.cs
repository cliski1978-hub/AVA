using AVA.Memory.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AVA.Memory.Sql.Config
{
    /// <summary>
    /// EF Core configuration for MemoryVector entity.
    /// Stores embedding dimensions as (Index, Value) rows.
    /// </summary>
    public class MemoryVectorConfig : IEntityTypeConfiguration<MemoryVector>
    {
        public void Configure(EntityTypeBuilder<MemoryVector> builder)
        {
            builder.HasKey(v => v.ID);

            builder.Property(v => v.Index).IsRequired();
            builder.Property(v => v.Value).IsRequired();

            builder.HasIndex(v => new { v.RecordID, v.Index })
                   .IsUnique(); // one value per dimension
        }
    }
}
