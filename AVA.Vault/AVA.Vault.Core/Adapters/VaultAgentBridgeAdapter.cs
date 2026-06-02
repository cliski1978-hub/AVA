using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;
using CliskiCore.DbAPI.Interfaces;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core.Adapters
{
    /// <summary>
    /// Provides the integration bridge between AVA Agents and the Vault data layer.
    /// Handles context storage, retrieval, and linkage of agent-related artifacts.
    /// </summary>
    public sealed class VaultAgentBridgeAdapter : IAgentBridgeAdapter
    {
        private readonly IVaultDbContext _db;
        private readonly IContextLogger _log;
        private readonly VaultInstanceConfig _config;

        public VaultAgentBridgeAdapter(
            IVaultDbContext db,
            IContextLogger log,
            VaultInstanceConfig config)
        {
            _db = db;
            _log = log;
            _config = config;
        }

        // -------------------------------------------------------------
        // Core Bridge Methods
        // -------------------------------------------------------------

        public async Task<string> StoreAgentArtifactAsync(string agentId, string type, string content, Dictionary<string, string>? metadata = null)
        {
            try
            {
                var note = new VaultNote
                {
                    ID = Guid.NewGuid().ToString("N"),
                    VaultID = _config.VaultID,
                    Title = $"{type} Entry ({agentId})",
                    Content = content,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Metadata = new List<VaultMetadata>()
                };

                // add metadata entries
                if (metadata != null)
                {
                    foreach (var kv in metadata)
                        note.Metadata.Add(new VaultMetadata
                        {
                            ID = Guid.NewGuid().ToString("N"),
                            NoteID = note.ID,
                            Key = kv.Key,
                            Value = kv.Value
                        });
                }

                // add tag references
                if (metadata?.ContainsKey("tags") == true)
                {
                    var tags = metadata["tags"].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    note.VaultNoteVaultTags = tags.Select(t => new VaultNoteVaultTag
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        NoteID = note.ID,
                        TagID = Guid.NewGuid().ToString("N"),
                        Tag = new VaultTag
                        {
                            ID = Guid.NewGuid().ToString("N"),
                            ProjectID = _config.VaultID,
                            Name = t.Trim()
                        },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList();
                }

                _db.VaultNotes.Add(note);
                await _db.FlushAsync();

                _log.Log("VaultAgentBridge", $"Stored {type} for agent {agentId}: {note.ID}");
                return note.ID;
            }
            catch (Exception ex)
            {
                _log.LogError("VaultAgentBridge", $"Error storing {type} for agent {agentId}", ex);
                throw;
            }
        }

        public async Task<List<VaultNote>> QueryAgentArtifactsAsync(string agentId, string? type = null, string? tag = null)
        {
            try
            {
                var query = _db.VaultNotes
                    .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                    .Include(n => n.Metadata)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(x => x.Title.Contains(type));

                if (!string.IsNullOrEmpty(tag))
                    query = query.Where(x => x.VaultNoteVaultTags.Any(jt => jt.Tag.Name == tag));

                var results = await query.AsNoTracking().ToListAsync();

                _log.Log("VaultAgentBridge", $"Queried {results.Count} artifacts for agent {agentId}");
                return results;
            }
            catch (Exception ex)
            {
                _log.LogError("VaultAgentBridge", $"Error querying artifacts for agent {agentId}", ex);
                throw;
            }
        }

        public async Task<string> LinkAgentArtifactsAsync(string sourceNoteId, string targetNoteId, string relationType)
        {
            try
            {
                var relation = new VaultNoteRelation
                {
                    ID = Guid.NewGuid().ToString("N"),
                    SourceNoteID = sourceNoteId,
                    TargetNoteID = targetNoteId,
                    RelationType = relationType,
                    CreatedAt = DateTime.UtcNow
                };

                _db.VaultNoteRelations.Add(relation);
                await _db.FlushAsync();

                _log.Log("VaultAgentBridge", $"Linked notes {sourceNoteId} ? {targetNoteId} ({relationType})");
                return relation.ID;
            }
            catch (Exception ex)
            {
                _log.LogError("VaultAgentBridge", $"Error linking {sourceNoteId} ? {targetNoteId}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteAgentArtifactAsync(string noteId)
        {
            try
            {
                var note = await _db.VaultNotes
                    .Include(n => n.Metadata)
                    .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                    .FirstOrDefaultAsync(n => n.ID == noteId);

                if (note == null)
                    return false;

                _db.VaultNotes.Remove(note);
                await _db.FlushAsync();

                _log.Log("VaultAgentBridge", $"Deleted artifact {noteId}");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError("VaultAgentBridge", $"Error deleting artifact {noteId}", ex);
                throw;
            }
        }
    }
}
