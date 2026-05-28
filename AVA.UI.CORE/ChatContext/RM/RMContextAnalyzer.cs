using System.Globalization;
using System.Text.RegularExpressions;
using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.ChatContext.Utilities;

namespace AVA.UI.CORE.ChatContext.RM
{
    /// <summary>
    /// Performs deterministic RM-style context scoring and weighted prioritization.
    /// </summary>
    public class RMContextAnalyzer : IRMContextAnalyzer
    {
        private static readonly Regex TermRegex = new("[A-Za-z][A-Za-z0-9_\\-]{2,}", RegexOptions.Compiled);
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "that", "this", "from", "into", "then", "than",
            "have", "has", "had", "are", "was", "were", "will", "would", "could", "should",
            "about", "what", "when", "where", "which", "your", "you", "our", "can", "just",
            "all", "not", "but", "get", "got", "use", "using", "there", "their", "they"
        };

        /// <summary>
        /// Scores and prioritizes prompt context items for the current conversational state.
        /// </summary>
        public RMContextAnalysisResult Analyze(
            string currentPrompt,
            IReadOnlyList<PromptContextItem> items,
            ModelContextSettings modelSettings,
            PromptBudgetState budgetState)
        {
            currentPrompt ??= string.Empty;
            modelSettings ??= new ModelContextSettings();
            budgetState ??= new PromptBudgetState();

            var safeItems = (items ?? Array.Empty<PromptContextItem>()).Select(CopyItem).ToList();
            var promptTerms = ExtractTerms(currentPrompt);
            var recentTerms = ExtractRecentTerms(safeItems);
            var intent = ResolveIntent(currentPrompt, recentTerms);
            var scores = BuildScores(safeItems, promptTerms, recentTerms, intent);
            var scoreById = scores.ToDictionary(s => s.ItemId, StringComparer.OrdinalIgnoreCase);
            var prioritized = ApplyWeightedBudget(safeItems, scoreById, modelSettings, budgetState);

            for (var i = 0; i < scores.Count; i++)
            {
                var rank = i + 1;
                foreach (var item in prioritized.Where(p => string.Equals(p.ItemId, scores[i].ItemId, StringComparison.OrdinalIgnoreCase)))
                {
                    item.Metadata["RMFinalWeightedScore"] = RMScoreFormatter.FormatScore(scores[i].FinalWeightedScore);
                    item.Metadata["RMRank"] = rank.ToString(CultureInfo.InvariantCulture);
                    item.Metadata["RMReasons"] = RMScoreFormatter.FormatReasons(scores[i].Reasons);
                }
            }

            return new RMContextAnalysisResult
            {
                Scores = scores,
                PrioritizedItems = prioritized,
                AnalysisApplied = safeItems.Count > 0,
                Metadata = new Dictionary<string, string>
                {
                    ["Intent"] = intent,
                    ["PromptTermCount"] = promptTerms.Count.ToString(CultureInfo.InvariantCulture),
                    ["RecentThemeCount"] = recentTerms.Count.ToString(CultureInfo.InvariantCulture),
                    ["ScoreCount"] = scores.Count.ToString(CultureInfo.InvariantCulture)
                }
            };
        }

        private static List<RMContextScore> BuildScores(
            IReadOnlyList<PromptContextItem> items,
            ISet<string> promptTerms,
            ISet<string> recentTerms,
            string intent)
        {
            var count = Math.Max(1, items.Count);
            return items
                .Select((item, index) => BuildScore(item, index, count, promptTerms, recentTerms, intent))
                .OrderByDescending(s => s.FinalWeightedScore)
                .ThenBy(s => s.ItemId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static RMContextScore BuildScore(
            PromptContextItem item,
            int index,
            int count,
            ISet<string> promptTerms,
            ISet<string> recentTerms,
            string intent)
        {
            var reasons = new List<RMSelectionReason>();
            var itemTerms = ExtractTerms(item.Content);
            foreach (var pair in item.Metadata ?? new Dictionary<string, string>())
            {
                foreach (var term in ExtractTerms(pair.Key + " " + pair.Value))
                    itemTerms.Add(term);
            }

            var relevance = promptTerms.Count == 0
                ? 0.0
                : Clamp((double)itemTerms.Intersect(promptTerms, StringComparer.OrdinalIgnoreCase).Count() / promptTerms.Count);
            AddReason(reasons, "PromptTerms", "Shares terms with the current prompt.", relevance);

            var continuity = recentTerms.Count == 0
                ? 0.0
                : Clamp((double)itemTerms.Intersect(recentTerms, StringComparer.OrdinalIgnoreCase).Count() / Math.Min(recentTerms.Count, 8));
            if (item.ItemType == PromptContextItemType.ChatMessage)
                continuity = Clamp(continuity + 0.15);
            if (item.ItemType == PromptContextItemType.MemoryInjection)
                continuity = Clamp(continuity + 0.25);
            AddReason(reasons, "Continuity", "Connects to active session themes.", continuity);

            var recency = Clamp((double)(index + 1) / count);
            AddReason(reasons, "Recency", "Appears near the current conversation edge.", recency);

            var intentAlignment = ScoreIntentAlignment(item, itemTerms, intent);
            AddReason(reasons, "Intent", $"Aligns with {intent} intent.", intentAlignment);

            var toolActivity = item.ItemType switch
            {
                PromptContextItemType.ToolCall => 0.9,
                PromptContextItemType.ToolResult => 0.85,
                _ when intent == "coding" || intent == "tool execution" => ContainsAny(itemTerms, ["build", "error", "file", "test", "tool", "command"]) ? 0.45 : 0.0,
                _ => 0.0
            };
            AddReason(reasons, "ToolActivity", "Supports recent tool or debugging activity.", toolActivity);

            var metadataWeight = item.ItemType == PromptContextItemType.Metadata ? 0.75 : 0.0;
            if (item.Metadata?.Count > 0)
                metadataWeight = Clamp(metadataWeight + 0.2);
            AddReason(reasons, "Metadata", "Carries routing, session, or execution metadata.", metadataWeight);

            if (item.IsPinned)
                AddReason(reasons, "Pinned", "Pinned items are protected from RM trimming.", 1.0);
            if (item.SelectionStatus == PromptContextSelectionStatus.ForcedByUser)
                AddReason(reasons, "ForcedByUser", "User-forced items are protected from RM trimming.", 1.0);
            if (item.ItemType == PromptContextItemType.CurrentPrompt)
                AddReason(reasons, "CurrentPrompt", "Current prompt remains required.", 1.0);

            var final = Clamp(
                relevance * 0.30 +
                continuity * 0.22 +
                recency * 0.18 +
                intentAlignment * 0.14 +
                toolActivity * 0.10 +
                metadataWeight * 0.06);

            if (item.IsPinned || item.SelectionStatus == PromptContextSelectionStatus.ForcedByUser || item.ItemType == PromptContextItemType.CurrentPrompt)
                final = 1.0;

            return new RMContextScore
            {
                ItemId = item.ItemId ?? string.Empty,
                RelevanceScore = relevance,
                ContinuityScore = continuity,
                RecencyScore = recency,
                IntentAlignmentScore = intentAlignment,
                ToolActivityScore = toolActivity,
                MetadataWeightScore = metadataWeight,
                FinalWeightedScore = final,
                Reasons = reasons.Where(r => r.Weight > 0).OrderByDescending(r => r.Weight).ToList()
            };
        }

        private static List<PromptContextItem> ApplyWeightedBudget(
            IReadOnlyList<PromptContextItem> items,
            IReadOnlyDictionary<string, RMContextScore> scores,
            ModelContextSettings modelSettings,
            PromptBudgetState budgetState)
        {
            var copies = items.Select(CopyItem).ToList();
            var protectedItems = copies.Where(IsProtected).ToList();
            var protectedTokens = protectedItems.Sum(i => Math.Max(0, i.EstimatedTokens));
            var contextWindow = Math.Max(0, modelSettings.ContextWindow);
            var reserve = Math.Max(0, modelSettings.DefaultOutputReserve);
            var budget = Math.Max(0, contextWindow - reserve - protectedTokens);
            var used = 0;

            var candidates = copies
                .Where(i => !IsProtected(i) && IsRmBudgetCandidate(i))
                .OrderByDescending(i => scores.TryGetValue(i.ItemId, out var score) ? score.FinalWeightedScore : 0.0)
                .ThenByDescending(i => i.IsIncluded)
                .ThenBy(i => i.ItemId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var selectedIds = new HashSet<string>(protectedItems.Select(i => i.ItemId), StringComparer.OrdinalIgnoreCase);
            foreach (var item in candidates)
            {
                var tokens = Math.Max(0, item.EstimatedTokens);
                if (used + tokens <= budget)
                {
                    used += tokens;
                    selectedIds.Add(item.ItemId);
                }
            }

            foreach (var item in copies)
            {
                if (IsProtected(item))
                    continue;

                if (!IsRmBudgetCandidate(item))
                    continue;

                if (selectedIds.Contains(item.ItemId))
                {
                    item.IsIncluded = true;
                    item.SelectionStatus = PromptContextSelectionStatus.Included;
                }
                else
                {
                    item.IsIncluded = false;
                    item.SelectionStatus = PromptContextSelectionStatus.ExcludedByBudget;
                }
            }

            return copies;
        }

        private static bool IsProtected(PromptContextItem item) =>
            item.ItemType == PromptContextItemType.CurrentPrompt ||
            item.IsPinned ||
            item.SelectionStatus == PromptContextSelectionStatus.ForcedByUser ||
            item.SelectionStatus == PromptContextSelectionStatus.Pinned;

        private static bool IsRmBudgetCandidate(PromptContextItem item) =>
            item.SelectionStatus != PromptContextSelectionStatus.ExcludedByPolicy &&
            item.SelectionStatus != PromptContextSelectionStatus.ExcludedByUser;

        private static double ScoreIntentAlignment(PromptContextItem item, ISet<string> terms, string intent)
        {
            if (intent == "coding")
                return item.ItemType is PromptContextItemType.ToolCall or PromptContextItemType.ToolResult
                    ? 0.9
                    : ContainsAny(terms, ["code", "class", "build", "test", "bug", "file", "compile", "error"]) ? 0.75 : 0.0;

            if (intent == "planning")
                return ContainsAny(terms, ["plan", "sprint", "goal", "step", "architecture", "design", "scope"]) ? 0.8 : 0.0;

            if (intent == "tool execution")
                return item.ItemType is PromptContextItemType.ToolCall or PromptContextItemType.ToolResult ? 1.0 : 0.0;

            if (intent == "memory discussion")
                return ContainsAny(terms, ["memory", "context", "vault", "session", "history", "recall"]) ? 0.8 : 0.0;

            return item.ItemType == PromptContextItemType.ChatMessage ? 0.45 : 0.0;
        }

        private static string ResolveIntent(string prompt, ISet<string> recentTerms)
        {
            var terms = ExtractTerms(prompt);
            foreach (var term in recentTerms)
                terms.Add(term);

            if (ContainsAny(terms, ["code", "class", "build", "test", "compile", "bug", "file", "sprint"]))
                return "coding";
            if (ContainsAny(terms, ["tool", "command", "run", "debug", "error"]))
                return "tool execution";
            if (ContainsAny(terms, ["memory", "context", "vault", "history", "recall"]))
                return "memory discussion";
            if (ContainsAny(terms, ["plan", "architecture", "design", "scope", "goal"]))
                return "planning";

            return "conversation";
        }

        private static HashSet<string> ExtractRecentTerms(IReadOnlyList<PromptContextItem> items)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items.Where(i => i.IsIncluded).TakeLast(8))
            {
                foreach (var term in ExtractTerms(item.Content))
                    set.Add(term);
            }

            return set;
        }

        private static HashSet<string> ExtractTerms(string? text)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text))
                return set;

            foreach (Match match in TermRegex.Matches(text))
            {
                var value = match.Value.Trim().ToLowerInvariant();
                if (!StopWords.Contains(value))
                    set.Add(value);
            }

            return set;
        }

        private static bool ContainsAny(ISet<string> terms, IEnumerable<string> values) =>
            values.Any(v => terms.Contains(v));

        private static void AddReason(List<RMSelectionReason> reasons, string code, string description, double weight)
        {
            if (weight <= 0)
                return;

            reasons.Add(new RMSelectionReason
            {
                Code = code,
                Description = description,
                Weight = Clamp(weight)
            });
        }

        private static double Clamp(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0.0;

            return Math.Max(0.0, Math.Min(1.0, value));
        }

        private static PromptContextItem CopyItem(PromptContextItem src) => new()
        {
            ItemId = src.ItemId ?? string.Empty,
            ItemType = src.ItemType,
            SourceId = src.SourceId ?? string.Empty,
            SourceLabel = src.SourceLabel ?? string.Empty,
            Content = src.Content ?? string.Empty,
            EstimatedTokens = Math.Max(0, src.EstimatedTokens),
            IsIncluded = src.IsIncluded,
            IsPinned = src.IsPinned,
            IsUserOverride = src.IsUserOverride,
            SelectionStatus = src.SelectionStatus,
            Metadata = new Dictionary<string, string>(src.Metadata ?? new Dictionary<string, string>())
        };
    }
}
