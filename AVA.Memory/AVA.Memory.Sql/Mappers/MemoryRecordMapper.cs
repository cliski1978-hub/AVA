using System.Linq;
using AVA.Memory.Data.Entities;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Sql.Mappers
{
    public static class MemoryRecordMapper
    {
        public static MemoryRecordDto ToDto(this MemoryRecord entity)
        {
            return new MemoryRecordDto
            {
                ID = entity.ID,
                Text = entity.Text,
                EpisodeId = entity.EpisodeId,
                ContextId = entity.ContextId,
                Salience = entity.Salience,
                Novelty = entity.Novelty,
                Frequency = entity.Frequency,
                DecayRate = entity.DecayRate,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                LastAccessedAt = entity.LastAccessedAt,
                Source = entity.Source,
                Vectors = entity.Vectors?.Select(v => v.ToDto()).ToList() ?? new(),
                Tags = entity.Tags?.Select(t => t.ToDto()).ToList() ?? new(),
                Metadata = entity.Metadata?.Select(m => m.ToDto()).ToList() ?? new(),
                OutgoingEdges = entity.OutgoingEdges?.Select(e => e.ToDto()).ToList() ?? new(),
                IncomingEdges = entity.IncomingEdges?.Select(e => e.ToDto()).ToList() ?? new(),

                // ───────── Identity Fields ─────────
                PrimaryIdentityId = entity.PrimaryIdentityId,
                PrimaryIdentityHandle = entity.PrimaryIdentityHandle,
                PrimaryIdentityType = entity.PrimaryIdentityType,
                IdentityList = entity.IdentityList
            };
        }

        public static MemoryRecord ToEntity(this MemoryRecordDto dto)
        {
            return new MemoryRecord
            {
                ID = dto.ID,
                Text = dto.Text,
                EpisodeId = dto.EpisodeId,
                ContextId = dto.ContextId,
                Salience = dto.Salience,
                Novelty = dto.Novelty,
                Frequency = dto.Frequency,
                DecayRate = dto.DecayRate,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                LastAccessedAt = dto.LastAccessedAt,
                Source = dto.Source,
                Vectors = dto.Vectors?.Select(v => v.ToEntity()).ToList() ?? new(),
                Tags = dto.Tags?.Select(t => t.ToEntity()).ToList() ?? new(),
                Metadata = dto.Metadata?.Select(m => m.ToEntity()).ToList() ?? new(),

                // ───────── Identity Fields ─────────
                PrimaryIdentityId = dto.PrimaryIdentityId,
                PrimaryIdentityHandle = dto.PrimaryIdentityHandle,
                PrimaryIdentityType = dto.PrimaryIdentityType,
                IdentityList = dto.IdentityList
            };
        }
    }
}
