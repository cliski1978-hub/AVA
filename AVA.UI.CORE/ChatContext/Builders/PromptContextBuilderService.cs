using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Memory;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Builders
{
    /// <summary>
    /// High-level coordinator for deterministic prompt context building.
    /// Calls IHistorySelectionPolicy → IPromptAssemblyService → returns PromptContextPackage.
    /// No RM behavior — deterministic pipeline only.
    /// </summary>
    public class PromptContextBuilderService : IPromptContextBuilderService
    {
        private readonly IHistorySelectionPolicy _selectionPolicy;
        private readonly IPromptAssemblyService  _assemblyService;
        private readonly IMemoryContextProvider _memoryContextProvider;

        public PromptContextBuilderService(
            IHistorySelectionPolicy selectionPolicy,
            IPromptAssemblyService assemblyService,
            IMemoryContextProvider memoryContextProvider)
        {
            _selectionPolicy = selectionPolicy;
            _assemblyService = assemblyService;
            _memoryContextProvider = memoryContextProvider;
        }

        /// <inheritdoc />
        public PromptContextPackage BuildPromptContext(
            string sessionId,
            string modelId,
            string currentPrompt,
            IEnumerable<SessionChatMessage> chatHistory,
            ModelContextSettings modelSettings)
        {
            var selection = _selectionPolicy.SelectHistory(
                sessionId,
                modelId,
                currentPrompt,
                chatHistory,
                modelSettings);

            return _assemblyService.Assemble(
                sessionId,
                modelId,
                currentPrompt,
                selection,
                modelSettings);
        }

        /// <inheritdoc />
        public async Task<PromptContextPackage> BuildPromptContextAsync(
            string sessionId,
            string modelId,
            string currentPrompt,
            IEnumerable<SessionChatMessage> chatHistory,
            ModelContextSettings modelSettings,
            CancellationToken cancellationToken = default)
        {
            modelSettings ??= new ModelContextSettings();
            var selection = _selectionPolicy.SelectHistory(
                sessionId,
                modelId,
                currentPrompt,
                chatHistory,
                modelSettings);

            var memory = await _memoryContextProvider.RetrieveAsync(
                sessionId,
                modelId,
                currentPrompt,
                modelSettings,
                cancellationToken);

            var memoryItems = MemoryContextInjectionMapper.Map(memory);
            if (memoryItems.Count > 0)
            {
                selection.Items.AddRange(memoryItems);
                ApplyMemoryBudget(selection.Items, modelSettings);
            }

            var package = _assemblyService.Assemble(
                sessionId,
                modelId,
                currentPrompt,
                selection,
                modelSettings);

            package.Metadata["MemoryRetrievalApplied"] = memory.RetrievalApplied.ToString();
            package.Metadata["MemoryInjectionCount"] = memory.Items.Count.ToString();
            return package;
        }

        private static void ApplyMemoryBudget(IReadOnlyList<PromptContextItem> items, ModelContextSettings settings)
        {
            var available = Math.Max(0, settings.ContextWindow - settings.DefaultOutputReserve);
            var used = items.Where(i => i.IsIncluded).Sum(i => Math.Max(0, i.EstimatedTokens));
            if (used <= available)
                return;

            foreach (var item in items
                         .Where(IsMemoryInjectionItem)
                         .OrderBy(ReadMemoryRelevanceScore)
                         .ThenByDescending(i => Math.Max(0, i.EstimatedTokens)))
            {
                if (used <= available)
                    break;

                item.IsIncluded = false;
                item.SelectionStatus = PromptContextSelectionStatus.ExcludedByBudget;
                used -= Math.Max(0, item.EstimatedTokens);
            }
        }

        private static bool IsMemoryInjectionItem(PromptContextItem item) =>
            item.ItemType == PromptContextItemType.MemoryInjection &&
            item.Metadata.TryGetValue("MemoryInjection", out var value) &&
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

        private static double ReadMemoryRelevanceScore(PromptContextItem item)
        {
            return item.Metadata.TryGetValue("MemoryRelevanceScore", out var value) &&
                   double.TryParse(value, out var score)
                ? score
                : 0.0;
        }
    }
}
