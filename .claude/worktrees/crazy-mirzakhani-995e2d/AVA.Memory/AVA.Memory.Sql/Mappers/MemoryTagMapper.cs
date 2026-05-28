using AVA.Memory.Data.Entities;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Sql.Mappers
{
    public static class MemoryTagMapper
    {
        public static MemoryTagDto ToDto(this MemoryTag entity)
        {
            return new MemoryTagDto
            {
                ID = entity.ID,
                RecordID = entity.RecordID,
                Tag = entity.Tag,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,

                // ───────── Identity Fields ─────────
                PrimaryIdentityId = entity.PrimaryIdentityId,
                PrimaryIdentityHandle = entity.PrimaryIdentityHandle,
                PrimaryIdentityType = entity.PrimaryIdentityType,
                IdentityList = entity.IdentityList
            };
        }

        public static MemoryTag ToEntity(this MemoryTagDto dto)
        {
            return new MemoryTag
            {
                ID = dto.ID,
                RecordID = dto.RecordID,
                Tag = dto.Tag,
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
