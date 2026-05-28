using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Utils;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Provides cached and ranked search operations over Vault notes.
    /// Used by agents and UI layers for efficient query access.
    /// </summary>
    public sealed class VaultSearchService
    {
        private readonly VaultLogger _logger;
        private readonly Func<List<MarkdownNote>> _noteProvider;
        private readonly ConcurrentDictionary<string, List<MarkdownNote>> _cache = new();
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps = new();

        public VaultSearchService(VaultLogger logger, Func<List<MarkdownNote>> noteProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _noteProvider = noteProvider ?? throw new ArgumentNullException(nameof(noteProvider));
        }

        // -------------------------------------------------------------
        // Public Search
        // -------------------------------------------------------------

        public Task<List<MarkdownNote>> SearchAsync(VaultQueryOptions options, CancellationToken ct = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            string key = BuildCacheKey(options);
            if (TryGetCachedResult(key, out var cached))
                return Task.FromResult(cached);

            var notes = _noteProvider.Invoke();
            var filtered = VaultQueryHelper.ApplyFilters(notes, options);

            var ranked = RankResults(filtered, options.Keyword);

            _cache[key] = ranked;
            _cacheTimestamps[key] = DateTime.UtcNow;
            _logger.Log(nameof(VaultSearchService), $"Cached query: {key} ({ranked.Count} results)");

            return Task.FromResult(ranked);
        }

        // -------------------------------------------------------------
        // Ranking
        // -------------------------------------------------------------

        private static List<MarkdownNote> RankResults(IEnumerable<MarkdownNote> notes, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return notes.ToList();

            var term = keyword.ToLowerInvariant();
            return notes
                .Select(n => new
                {
                    Note = n,
                    Score =
                        (n.Title?.ToLowerInvariant().Contains(term) == true ? 3 : 0) +
                        (n.Content?.ToLowerInvariant().Split(term).Length - 1) +
                        (n.Tags?.Count(t => t.Equals(term, StringComparison.OrdinalIgnoreCase)) ?? 0)
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Note.Created)
                .Select(x => x.Note)
                .ToList();
        }

        // -------------------------------------------------------------
        // Cache Management
        // -------------------------------------------------------------

        private string BuildCacheKey(VaultQueryOptions options)
        {
            return $"{options.Tag}-{options.Keyword}-{options.Source}-{options.After}-{options.Before}-{string.Join(',', options.RequiredTags ?? new())}";
        }

        private bool TryGetCachedResult(string key, out List<MarkdownNote> notes)
        {
            notes = new List<MarkdownNote>();
            if (!_cache.TryGetValue(key, out var cached))
                return false;

            if (!_cacheTimestamps.TryGetValue(key, out var ts) || DateTime.UtcNow - ts > _cacheDuration)
            {
                _cache.TryRemove(key, out _);
                _cacheTimestamps.TryRemove(key, out _);
                return false;
            }

            notes = cached;
            return true;
        }

        public void ClearCache() => _cache.Clear();
    }
}
