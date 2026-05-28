using AVA.Memory.Data.Entities;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Sql.Mappers
{
    public static class MemoryVectorMapper
    {
        public static MemoryVectorDto ToDto(this MemoryVector entity)
        {
            return new MemoryVectorDto
            {
                ID = entity.ID,
                RecordID = entity.RecordID,
                Index = entity.Index,
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

        public static MemoryVector ToEntity(this MemoryVectorDto dto)
        {
            return new MemoryVector
            {
                ID = dto.ID,
                RecordID = dto.RecordID,
                Index = dto.Index,
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
