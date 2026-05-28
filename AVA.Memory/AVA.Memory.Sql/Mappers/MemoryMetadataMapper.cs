using AVA.Memory.Data.Entities;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Sql.Mappers
{
    public static class MemoryMetadataMapper
    {
        public static MemoryMetadataDto ToDto(this MemoryMetadata entity)
        {
            return new MemoryMetadataDto
            {
                ID = entity.ID,
                RecordID = entity.RecordID,
                Key = entity.Key,
                Value = entity.Value,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,

                // ───────── Identity Fields ─────────
                PrimaryIdentityId = entity.PrimaryIdentityId,
                PrimaryIdentityHandle = entity.PrimaryIdentityHandle,
                PrimaryIdentityType = entity.PrimaryIdentityType,
                IdentityList = entity.IdentityList
            };
        }

        public static MemoryMetadata ToEntity(this MemoryMetadataDto dto)
        {
            return new MemoryMetadata
            {
                ID = dto.ID,
                RecordID = dto.RecordID,
                Key = dto.Key,
                Value = dto.Value,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,

                // ───────── Identity Fields ─────────
                PrimaryIdentityId = dto.PrimaryIdentityId,
                PrimaryIdentityHandle = dto.PrimaryIdentityHandle,
                PrimaryIdentityType = dto.PrimaryIdentityType,
                IdentityList = dto.IdentityList
            };
        }
    }
}
