using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Models.Query;

namespace AVA.Vault.Core.Services.Queries
{
    /// <summary>
    /// Provides fast retrieval of chat-based VaultNotes, their metadata, and transcript summaries.
    /// Chat records are identified by having a tag named "chat" (case-insensitive).
    /// </summary>
    public sealed class VaultChatQueries
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultChatQueries(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // -------------------------------------------------------------
        // Primary Chat Retrieval
        // -------------------------------------------------------------

        /// <summary>
        /// Retrieves all chat notes in the Vault (optionally limited and filtered).
        /// </summary>
        public async Task<List<VaultNote>> GetAllChatsAsync(string? vaultId = null,int limit = 500, CancellationToken ct = default)
        {
            var query = _db.VaultNotes
                .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                .Include(n => n.Metadata)
                .Where(n => n.VaultNoteVaultTags.Any(jt => jt.Tag.Name.ToLower() == "chat"))
                .AsNoTracking()
                .OrderByDescending(n => n.UpdatedAt)
                .Take(limit);

            if (!string.IsNullOrEmpty(vaultId))
                query = query.Where(n => n.VaultID == vaultId).Take(limit);

            var results = await query.ToListAsync(ct);
            _logger.Log("VaultChatQueries", $"Retrieved {results.Count} chat records.");
            return results;
        }

        /// <summary>
        /// Retrieves chat notes for a specific date or date range.
        /// </summary>
        public async Task<List<VaultNote>> GetChatsByDateAsync(
            DateTime start,
            DateTime? end = null,
            string? vaultId = null,
            CancellationToken ct = default)
        {
            end ??= start.AddDays(1);

            var query = _db.VaultNotes
                .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                .Include(n => n.Metadata)
                .Where(n => n.VaultNoteVaultTags.Any(jt => jt.Tag.Name.ToLower() == "chat") &&
                            n.CreatedAt >= start && n.CreatedAt < end)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(vaultId))
                query = query.Where(n => n.VaultID == vaultId);

            var results = await query.OrderBy(n => n.CreatedAt).ToListAsync(ct);
            _logger.Log("VaultChatQueries", $"Retrieved {results.Count} chats between {start:u} and {end:u}");
            return results;
        }

        /// <summary>
        /// Retrieves all chat notes associated with a specific agent or participant.
        /// </summary>
        public async Task<List<VaultNote>> GetChatsByAgentAsync(
            string agentId,
            CancellationToken ct = default)
        {
            var results = await _db.VaultNotes
                .Include(n => n.Metadata)
                .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                .Where(n => n.VaultNoteVaultTags.Any(jt => jt.Tag.Name.ToLower() == "chat") &&
                            n.Metadata.Any(m => m.Key == "agent_id" && m.Value == agentId))
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(ct);

            _logger.Log("VaultChatQueries", $"Retrieved {results.Count} chats for agent {agentId}");
            return results;
        }

        // -------------------------------------------------------------
        // Transcript / Summary Retrieval
        // -------------------------------------------------------------

        /// <summary>
        /// Retrieves a single chat transcript by note ID.
        /// </summary>
        public async Task<VaultNote?> GetChatByIdAsync(string chatId, CancellationToken ct = default)
        {
            var note = await _db.VaultNotes
                .Include(n => n.Metadata)
                .Include(n => n.VaultNoteVaultTags).ThenInclude(jt => jt.Tag)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.ID == chatId && n.VaultNoteVaultTags.Any(jt => jt.Tag.Name.ToLower() == "chat"), ct);

            if (note == null)
                _logger.Log("VaultChatQueries", $"No chat found for ID {chatId}");
            else
                _logger.Log("VaultChatQueries", $"Loaded chat transcript {chatId}");

            return note;
        }

        /// <summary>
        /// Summarizes chat activity counts grouped by day.
        /// </summary>
        public async Task<List<ChatDailySummary>> GetChatActivitySummaryAsync(
            DateTime? since = null,
            CancellationToken ct = default)
        {
            since ??= DateTime.UtcNow.AddDays(-7);

            var summaries = await _db.VaultNotes
                .Where(n => n.VaultNoteVaultTags.Any(jt => jt.Tag.Name.ToLower() == "chat") &&
                            n.CreatedAt >= since.Value)
                .GroupBy(n => n.CreatedAt.Date)
                .Select(g => new ChatDailySummary
                {
                    Date = g.Key,
                    ChatCount = g.Count(),
                    Participants = g.SelectMany(n => n.Metadata
                        .Where(m => m.Key == "user_id" || m.Key == "agent_id")
                        .Select(m => m.Value))
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(s => s.Date)
                .ToListAsync(ct);

            _logger.Log("VaultChatQueries", $"Generated chat summary since {since:u}");
            return summaries;
        }

        // -------------------------------------------------------------
        // Raw SQL Path (High-Volume / Export)
        // -------------------------------------------------------------

        /// <summary>
        /// Retrieves chat records using direct SQL for maximum throughput (e.g., export operations).
        /// </summary>
        public async Task<List<VaultNote>> GetChatsRawSqlAsync(string vaultId, int limit = 1000, CancellationToken ct = default)
        {
            var sql = """
                SELECT n.*
                FROM VaultNotes n
                INNER JOIN VaultNoteVaultTags jt ON n.ID = jt.NoteID
                INNER JOIN VaultTags t ON jt.TagID = t.ID
                WHERE t.Name = 'chat' AND n.VaultID = {0}
                ORDER BY n.UpdatedAt DESC
                LIMIT {1}
            """;

            var results = await _db.VaultNotes
                .FromSqlRaw(sql, vaultId, limit)
                .AsNoTracking()
                .ToListAsync(ct);

            _logger.Log("VaultChatQueries", $"Retrieved {results.Count} chats via raw SQL path.");
            return results;
        }
    }

    // -------------------------------------------------------------
    // Support Model for Chat Summaries
    // -------------------------------------------------------------

    public sealed class ChatDailySummary
    {
        public DateTime Date { get; set; }
        public int ChatCount { get; set; }
        public int Participants { get; set; }
    }
}
