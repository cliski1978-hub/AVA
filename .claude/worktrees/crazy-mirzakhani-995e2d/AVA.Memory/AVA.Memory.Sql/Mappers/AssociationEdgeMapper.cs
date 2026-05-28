using AVA.Memory.Data.Entities;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Sql.Mappers
{
    public static class AssociationEdgeMapper
    {
        public static AssociationEdgeDto ToDto(this AssociationEdge entity)
        {
            return new AssociationEdgeDto
            {
                ID = entity.ID,
                FromID = entity.FromRecordID,
                ToID = entity.ToRecordID,
                Type = entity.Type,
                Weight = entity.Weight,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,

                // ───────── Identity Fields ─────────
                PrimaryIdentityId = entity.PrimaryIdentityId,
                PrimaryIdentityHandle = entity.PrimaryIdentityHandle,
                PrimaryIdentityType = entity.PrimaryIdentityType,
                IdentityList = entity.IdentityList
            };
        }

        public static AssociationEdge ToEntity(this AssociationEdgeDto dto)
        {
            return new AssociationEdge
            {
                ID = dto.ID,
                FromRecordID = dto.FromID,
                ToRecordID = dto.ToID,
                Type = dto.Type,
                Weight = dto.Weight,
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
