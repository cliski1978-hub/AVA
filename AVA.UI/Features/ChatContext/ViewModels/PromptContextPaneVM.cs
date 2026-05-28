using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Memory;
using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.ChatContext.Services;
using AVA.UI.CORE.ChatContext.Utilities;
using AVA.UI.CORE.Models.UI;
using AVA.UI.State;

namespace AVA.UI.Features.ChatContext.ViewModels;

/// <summary>
/// Coordinates UI state for the Prompt Context pane.
/// Owns payload category toggles, item-level overrides, and rebuild logic.
/// All filtering and selection logic delegates to ChatContext services.
/// </summary>
public class PromptContextPaneVM : IDisposable
{
    private readonly AppState _appState;
    private readonly IHistorySelectionPolicy _selectionPolicy;
    private readonly IModelContextPolicyResolver _policyResolver;
    private readonly ISessionChatHistoryService _chatHistory;
    private readonly IChatContextOffloadService _offloadService;
    private readonly IPromptAssemblyService _promptAssemblyService;
    private readonly IContextUsageMonitor _contextUsageMonitor;
    private readonly IPromptDebugFormatter _promptDebugFormatter;
    private readonly ISessionContextProfileService _sessionContextProfileService;
    private readonly IContextCompressionService _contextCompressionService;
    private readonly IRMContextAnalyzer _rmContextAnalyzer;
    private readonly IMemoryContextProvider _memoryContextProvider;

    private readonly HashSet<string> _forcedIncludeIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _forcedExcludeIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pinnedIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _selectedItemIds = new(StringComparer.OrdinalIgnoreCase);
    private List<SessionChatMessage> _cachedMessages = new();
    private List<PromptContextItem> _mutableItems = new();
    private ModelContextSettings? _baseModelSettings;

    /// <summary>
    /// Raised when pane state changes and Razor should refresh.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Gets the active session identifier for this prompt context preview.
    /// </summary>
    public string SessionId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the selected model identifier for this prompt context preview.
    /// </summary>
    public string ModelId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the draft prompt used when rebuilding context.
    /// </summary>
    public string CurrentPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets the selected model's default context policy.
    /// </summary>
    public ModelContextSettings? PolicySettings { get; private set; }

    /// <summary>
    /// Gets the current budget snapshot after policy and runtime overrides.
    /// </summary>
    public PromptBudgetState? BudgetState { get; private set; }

    /// <summary>
    /// Gets the current live context usage metrics.
    /// </summary>
    public ContextUsageMetrics? UsageMetrics { get; private set; }

    /// <summary>
    /// Gets the current prompt debug package for the inspector.
    /// </summary>
    public PromptDebugPackage? DebugPackage { get; private set; }

    /// <summary>
    /// Gets the current deterministic compression result, when compression is enabled.
    /// </summary>
    public CompressionResult? CurrentCompressionResult { get; private set; }

    /// <summary>
    /// Gets the current deterministic RM analysis result, when RM selection is enabled.
    /// </summary>
    public RMContextAnalysisResult? CurrentRMAnalysis { get; private set; }

    /// <summary>
    /// Gets the current memory retrieval result, when memory injection is enabled.
    /// </summary>
    public MemoryRetrievalResult? CurrentMemoryRetrieval { get; private set; }

    /// <summary>
    /// Gets the available deterministic session context profiles.
    /// </summary>
    public IReadOnlyList<SessionContextProfile> AvailableProfiles { get; private set; } = Array.Empty<SessionContextProfile>();

    /// <summary>
    /// Gets the selected session context profile type.
    /// </summary>
    public SessionContextProfileType SelectedProfileType { get; private set; } = SessionContextProfileType.Default;

    /// <summary>
    /// Gets the selected session context profile.
    /// </summary>
    public SessionContextProfile? SelectedProfile { get; private set; }

    /// <summary>
    /// Gets all prompt context items, including excluded items.
    /// </summary>
    public IReadOnlyList<PromptContextItem> Items => _mutableItems;

    /// <summary>
    /// Gets the selected context item identifiers for manual offload.
    /// </summary>
    public IReadOnlyList<string> SelectedItemIds => _selectedItemIds.ToList();

    /// <summary>
    /// Gets whether the pane is visible.
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Gets whether the pane is loading session history.
    /// </summary>
    public bool IsLoading { get; private set; }

    /// <summary>
    /// Gets whether selected messages are being written to an offload file.
    /// </summary>
    public bool IsOffloading { get; private set; }

    /// <summary>
    /// Gets whether the raw prompt inspector is visible.
    /// </summary>
    public bool IsPromptInspectorVisible { get; private set; }

    /// <summary>
    /// Gets or sets whether deterministic automatic compression is enabled.
    /// </summary>
    public bool EnableAutomaticCompression { get; set; }

    /// <summary>
    /// Gets or sets whether deterministic RM-aware context selection is enabled.
    /// </summary>
    public bool EnableRMSelection { get; set; }

    /// <summary>
    /// Gets or sets whether externally retrieved memory should be injected into prompt context.
    /// </summary>
    public bool EnableMemoryInjection { get; set; }

    /// <summary>
    /// Gets the last context build error message, if any.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the last successfully written chat offload path.
    /// </summary>
    public string? LastOffloadPath { get; private set; }

    /// <summary>
    /// Gets or sets whether rich full-history payload mode is enabled.
    /// </summary>
    public bool UseFullHistoryPayload { get; set; }

    /// <summary>
    /// Gets or sets whether conversation history is eligible for inclusion.
    /// </summary>
    public bool IncludeConversationHistory { get; set; }

    /// <summary>
    /// Gets or sets whether tool call and tool result records are eligible for inclusion.
    /// </summary>
    public bool IncludeToolCalls { get; set; }

    /// <summary>
    /// Gets or sets whether detailed tool metadata is eligible for inclusion.
    /// </summary>
    public bool IncludeToolMetadata { get; set; }

    /// <summary>
    /// Gets or sets whether general metadata is eligible for inclusion.
    /// </summary>
    public bool IncludeMetadata { get; set; }

    /// <summary>
    /// Gets whether payload toggles differ from the selected model defaults.
    /// </summary>
    public bool HasPayloadOverrides =>
        PolicySettings != null &&
        (UseFullHistoryPayload != PolicySettings.UseFullHistoryPayload ||
         IncludeConversationHistory != PolicySettings.IncludeConversationHistory ||
         IncludeToolCalls != PolicySettings.IncludeToolCalls ||
         IncludeToolMetadata != PolicySettings.IncludeToolMetadata ||
         IncludeMetadata != PolicySettings.IncludeMetadata);

    /// <summary>
    /// Gets whether any item-level runtime overrides are active.
    /// </summary>
    public bool HasItemOverrides =>
        _forcedIncludeIds.Count > 0 ||
        _forcedExcludeIds.Count > 0 ||
        _pinnedIds.Count > 0;

    /// <summary>
    /// Initializes a new prompt context pane view model.
    /// </summary>
    public PromptContextPaneVM(
        AppState appState,
        IHistorySelectionPolicy selectionPolicy,
        IModelContextPolicyResolver policyResolver,
        ISessionChatHistoryService chatHistory,
        IChatContextOffloadService offloadService,
        IPromptAssemblyService promptAssemblyService,
        IContextUsageMonitor contextUsageMonitor,
        IPromptDebugFormatter promptDebugFormatter,
        ISessionContextProfileService sessionContextProfileService,
        IContextCompressionService contextCompressionService,
        IRMContextAnalyzer rmContextAnalyzer,
        IMemoryContextProvider memoryContextProvider)
    {
        _appState = appState;
        _selectionPolicy = selectionPolicy;
        _policyResolver = policyResolver;
        _chatHistory = chatHistory;
        _offloadService = offloadService;
        _promptAssemblyService = promptAssemblyService;
        _contextUsageMonitor = contextUsageMonitor;
        _promptDebugFormatter = promptDebugFormatter;
        _sessionContextProfileService = sessionContextProfileService;
        _contextCompressionService = contextCompressionService;
        _rmContextAnalyzer = rmContextAnalyzer;
        _memoryContextProvider = memoryContextProvider;
        AvailableProfiles = _sessionContextProfileService.GetDefaultProfiles();
        SelectedProfile = _sessionContextProfileService.GetProfile(SelectedProfileType);
    }

    /// <summary>
    /// Opens the pane for the specified session, model, and draft prompt.
    /// </summary>
    public async Task OpenAsync(string sessionId, string modelId, string currentPrompt)
    {
        SessionId = sessionId;
        ModelId = modelId;
        CurrentPrompt = currentPrompt;
        IsVisible = true;

        ClearAllOverrides();
        await FullBuildAsync();
    }

    /// <summary>
    /// Opens the pane using the active workspace session and default model.
    /// </summary>
    public async Task OpenForCurrentSessionAsync(string currentPrompt)
    {
        var session = _appState.ActiveWorkspaceSession;
        var sessionId = session?.SessionId ?? string.Empty;
        var modelId = session?.DefaultModelId
            ?? session?.AttachedModelIds?.FirstOrDefault()
            ?? string.Empty;

        await OpenAsync(sessionId, modelId, currentPrompt);
    }

    /// <summary>
    /// Closes the prompt context pane.
    /// </summary>
    public void Close()
    {
        IsVisible = false;
        ErrorMessage = null;
        Notify();
    }

    /// <summary>
    /// Reloads chat history and rebuilds the current context preview.
    /// </summary>
    public async Task RebuildAsync()
    {
        await FullBuildAsync();
    }

    /// <summary>
    /// Sets whether rich full-history payload mode is enabled.
    /// </summary>
    public async Task SetUseFullHistoryPayloadAsync(bool value)
    {
        UseFullHistoryPayload = value;
        MarkCustomProfile();
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Sets whether conversation history is eligible for inclusion.
    /// </summary>
    public async Task SetIncludeConversationHistoryAsync(bool value)
    {
        IncludeConversationHistory = value;
        MarkCustomProfile();
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Sets whether tool call and tool result records are eligible for inclusion.
    /// </summary>
    public async Task SetIncludeToolCallsAsync(bool value)
    {
        IncludeToolCalls = value;
        MarkCustomProfile();
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Sets whether detailed tool metadata is eligible for inclusion.
    /// </summary>
    public async Task SetIncludeToolMetadataAsync(bool value)
    {
        IncludeToolMetadata = value;
        MarkCustomProfile();
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Sets whether general metadata is eligible for inclusion.
    /// </summary>
    public async Task SetIncludeMetadataAsync(bool value)
    {
        IncludeMetadata = value;
        MarkCustomProfile();
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Sets the session runtime history policy.
    /// </summary>
    public async Task SetHistoryPolicyAsync(HistoryPolicyType value)
    {
        if (PolicySettings != null)
            PolicySettings.HistoryPolicy = value;

        MarkCustomProfile();
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Sets the session runtime maximum history message count.
    /// </summary>
    public async Task SetMaxHistoryMessagesAsync(int? value)
    {
        if (PolicySettings != null)
            PolicySettings.MaxHistoryMessages = value;

        MarkCustomProfile();
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Sets whether manual history selection is allowed for this session/model binding.
    /// </summary>
    public async Task SetAllowManualHistorySelectionAsync(bool value)
    {
        if (PolicySettings != null)
            PolicySettings.AllowManualHistorySelection = value;

        MarkCustomProfile();
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Restores payload category toggles to the selected model defaults.
    /// </summary>
    public async Task ResetPayloadTogglesAsync()
    {
        if (PolicySettings != null)
            ApplyPayloadFromPolicy(PolicySettings);

        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Applies a deterministic session context profile and rebuilds the preview.
    /// </summary>
    public async Task ApplyProfileAsync(SessionContextProfileType profileType)
    {
        var profile = _sessionContextProfileService.GetProfile(profileType)
            ?? _sessionContextProfileService.GetProfile(SessionContextProfileType.Default)
            ?? new SessionContextProfile();

        _baseModelSettings = _policyResolver.Resolve(ModelId);
        PolicySettings = _sessionContextProfileService.ApplyProfile(_baseModelSettings, profile);
        SelectedProfileType = profile.ProfileType;
        SelectedProfile = profile;
        ApplyPayloadFromPolicy(PolicySettings);
        EnableAutomaticCompression = false;
        EnableMemoryInjection = false;
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Restores the default session context profile.
    /// </summary>
    public async Task ResetToDefaultProfileAsync()
    {
        await ApplyProfileAsync(SessionContextProfileType.Default);
    }

    /// <summary>
    /// Forces a context item into the current prompt preview.
    /// </summary>
    public async Task ForceIncludeAsync(string itemId)
    {
        _forcedExcludeIds.Remove(itemId);
        _forcedIncludeIds.Add(itemId);
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Forces a context item out of the current prompt preview.
    /// </summary>
    public async Task ForceExcludeAsync(string itemId)
    {
        _forcedIncludeIds.Remove(itemId);
        _forcedExcludeIds.Add(itemId);
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Toggles the pinned runtime override for a context item.
    /// </summary>
    public async Task TogglePinAsync(string itemId)
    {
        if (_pinnedIds.Contains(itemId))
            _pinnedIds.Remove(itemId);
        else
            _pinnedIds.Add(itemId);

        await QuickRebuildAsync();
    }

    /// <summary>
    /// Clears all runtime overrides for a context item.
    /// </summary>
    public async Task ClearItemOverrideAsync(string itemId)
    {
        _forcedIncludeIds.Remove(itemId);
        _forcedExcludeIds.Remove(itemId);
        _pinnedIds.Remove(itemId);
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Clears all runtime item overrides and rebuilds from deterministic policy.
    /// </summary>
    public async Task ClearItemOverridesAsync()
    {
        _forcedIncludeIds.Clear();
        _forcedExcludeIds.Clear();
        _pinnedIds.Clear();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Enables deterministic automatic compression and rebuilds the prompt package.
    /// </summary>
    public async Task ApplyCompressionAsync()
    {
        EnableAutomaticCompression = true;
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Disables deterministic automatic compression and rebuilds the prompt package.
    /// </summary>
    public async Task DisableCompressionAsync()
    {
        EnableAutomaticCompression = false;
        CurrentCompressionResult = null;
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Enables deterministic RM-aware context analysis and rebuilds the prompt package.
    /// </summary>
    public async Task ApplyRMAnalysisAsync()
    {
        EnableRMSelection = true;
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Disables deterministic RM-aware context analysis and rebuilds the prompt package.
    /// </summary>
    public async Task DisableRMAnalysisAsync()
    {
        EnableRMSelection = false;
        CurrentRMAnalysis = null;
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Refreshes memory injection from the configured memory context provider.
    /// </summary>
    public async Task RefreshMemoryInjectionAsync()
    {
        EnableMemoryInjection = true;
        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Toggles memory injection and rebuilds prompt context.
    /// </summary>
    public async Task ToggleMemoryInjectionAsync(bool enabled)
    {
        EnableMemoryInjection = enabled;
        if (!enabled)
            CurrentMemoryRetrieval = null;

        await PersistRuntimeContextSettingsAsync();
        await QuickRebuildAsync();
    }

    /// <summary>
    /// Toggles a context item selection for manual chat offload.
    /// </summary>
    public void ToggleItemSelection(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || string.Equals(itemId, "current-prompt", StringComparison.OrdinalIgnoreCase))
            return;

        if (!_selectedItemIds.Remove(itemId))
            _selectedItemIds.Add(itemId);

        ErrorMessage = null;
        Notify();
    }

    /// <summary>
    /// Selects all currently included session-backed context items.
    /// </summary>
    public void SelectAllIncluded()
    {
        foreach (var item in _mutableItems.Where(i => i.IsIncluded && IsSessionBackedItem(i)))
            _selectedItemIds.Add(item.ItemId);

        ErrorMessage = null;
        Notify();
    }

    /// <summary>
    /// Clears the current offload selection.
    /// </summary>
    public void ClearSelection()
    {
        _selectedItemIds.Clear();
        ErrorMessage = null;
        Notify();
    }

    /// <summary>
    /// Writes selected session messages to a structured chat offload file.
    /// </summary>
    public async Task OffloadSelectedAsync()
    {
        if (_selectedItemIds.Count == 0)
        {
            ErrorMessage = "No messages selected.";
            LastOffloadPath = null;
            Notify();
            return;
        }

        IsOffloading = true;
        ErrorMessage = null;
        LastOffloadPath = null;
        Notify();

        try
        {
            var selectedMessages = BuildSelectedOffloadMessages();
            if (selectedMessages.Count == 0)
            {
                ErrorMessage = "No session messages selected.";
                return;
            }

            var path = await _offloadService.OffloadAsync(
                SessionId,
                _appState.ActiveVaultId,
                _appState.ActiveProjectId,
                selectedMessages);

            if (string.IsNullOrWhiteSpace(path))
            {
                ErrorMessage = "No messages selected.";
                return;
            }

            LastOffloadPath = path;
            _selectedItemIds.Clear();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsOffloading = false;
            Notify();
        }
    }

    /// <summary>
    /// Opens the raw prompt inspector and builds the current debug package.
    /// </summary>
    public async Task OpenPromptInspectorAsync()
    {
        IsPromptInspectorVisible = true;
        await RefreshPromptInspectorAsync();
    }

    /// <summary>
    /// Closes the raw prompt inspector.
    /// </summary>
    public void ClosePromptInspector()
    {
        IsPromptInspectorVisible = false;
        Notify();
    }

    /// <summary>
    /// Rebuilds the raw prompt inspector package from current context state.
    /// </summary>
    public Task RefreshPromptInspectorAsync()
    {
        var effective = BuildEffectiveSettings();
        DebugPackage = BuildDebugPackage(effective);
        Notify();
        return Task.CompletedTask;
    }

    private async Task FullBuildAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Notify();

        try
        {
            var previousModelId = _baseModelSettings?.ModelId;
            _baseModelSettings = _policyResolver.Resolve(ModelId);
            if (previousModelId == null ||
                !string.Equals(previousModelId, _baseModelSettings.ModelId, StringComparison.OrdinalIgnoreCase) ||
                PolicySettings == null)
            {
                var runtime = _appState.GetRuntimeContextSettings(SessionId, ModelId);
                PolicySettings = ApplyRuntimeContextSettings(_baseModelSettings, runtime);
                SelectedProfileType = SessionContextProfileType.Custom;
                SelectedProfile = _sessionContextProfileService.GetProfile(SelectedProfileType);
                ApplyPayloadFromPolicy(PolicySettings);
                EnableAutomaticCompression = runtime.EnableAutomaticCompression;
                EnableMemoryInjection = runtime.EnableMemoryInjection;
            }

            var history = await _chatHistory.LoadHistoryAsync(
                SessionId,
                _appState.ActiveVaultId,
                _appState.ActiveProjectId);

            _cachedMessages = history?.Messages ?? new List<SessionChatMessage>();
            await RunSelectionAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _mutableItems = new List<PromptContextItem>();
            BudgetState = null;
            UsageMetrics = null;
            DebugPackage = null;
            CurrentCompressionResult = null;
            CurrentRMAnalysis = null;
            CurrentMemoryRetrieval = null;
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    private async Task QuickRebuildAsync()
    {
        await RunSelectionAsync();
        Notify();
    }

    private async Task RunSelectionAsync()
    {
        var effective = BuildEffectiveSettings();
        var result = _selectionPolicy.SelectHistory(
            SessionId,
            ModelId,
            CurrentPrompt,
            _cachedMessages,
            effective);

        _mutableItems = result.Items.Select(CopyItem).ToList();
        ApplyItemOverrides();
        PruneSelection();
        await ApplyMemoryInjectionAsync(effective);
        var uncompressedBudget = ComputeBudget(effective, _mutableItems);
        CurrentRMAnalysis = EnableRMSelection
            ? _rmContextAnalyzer.Analyze(CurrentPrompt, _mutableItems, effective, uncompressedBudget)
            : null;
        if (CurrentRMAnalysis?.AnalysisApplied == true)
        {
            _mutableItems = CurrentRMAnalysis.PrioritizedItems.Select(CopyItem).ToList();
            ApplyItemOverrides();
            PruneSelection();
        }

        uncompressedBudget = ComputeBudget(effective, _mutableItems);
        CurrentCompressionResult = EnableAutomaticCompression
            ? _contextCompressionService.Compress(_mutableItems, effective, uncompressedBudget)
            : null;
        BudgetState = ComputeBudget(effective, GetAssemblyItems());
        UsageMetrics = CalculateUsageMetrics(effective);
        if (IsPromptInspectorVisible)
            DebugPackage = BuildDebugPackage(effective);
    }

    private void ApplyItemOverrides()
    {
        foreach (var item in _mutableItems)
        {
            var isForcedInclude = _forcedIncludeIds.Contains(item.ItemId);
            var isForcedExclude = _forcedExcludeIds.Contains(item.ItemId);
            var isPinned = _pinnedIds.Contains(item.ItemId);

            if (isForcedInclude)
            {
                item.IsIncluded = true;
                item.IsUserOverride = true;
                item.SelectionStatus = PromptContextSelectionStatus.ForcedByUser;
            }
            else if (isForcedExclude)
            {
                item.IsIncluded = false;
                item.IsUserOverride = true;
                item.SelectionStatus = PromptContextSelectionStatus.ExcludedByUser;
            }

            if (isPinned)
            {
                item.IsIncluded = true;
                item.IsPinned = true;
                item.IsUserOverride = true;
                if (!isForcedInclude && !isForcedExclude)
                    item.SelectionStatus = PromptContextSelectionStatus.Pinned;
            }
        }
    }

    private PromptBudgetState ComputeBudget(ModelContextSettings s, IReadOnlyList<PromptContextItem> items)
    {
        var usedTokens = (items ?? Array.Empty<PromptContextItem>())
            .Where(i => i.IsIncluded)
            .Sum(i => Math.Max(0, i.EstimatedTokens));
        var contextWindow = Math.Max(0, s.ContextWindow);
        var outputReserve = Math.Max(0, s.DefaultOutputReserve);
        var reservedTotal = usedTokens + outputReserve;
        var remaining = contextWindow - reservedTotal;

        double usagePct = contextWindow > 0
            ? (double)reservedTotal / contextWindow * 100.0
            : reservedTotal > 0 ? 100.0 : 0.0;

        return new PromptBudgetState
        {
            ModelId = ModelId,
            ContextWindow = contextWindow,
            OutputReserve = outputReserve,
            UsedTokens = usedTokens,
            RemainingTokens = remaining,
            UsagePercent = usagePct,
            IsOverBudget = remaining < 0
        };
    }

    private ModelContextSettings BuildEffectiveSettings() => new()
    {
        ModelId = ModelId,
        ContextWindow = PolicySettings?.ContextWindow ?? 8192,
        DefaultOutputReserve = PolicySettings?.DefaultOutputReserve ?? 1024,
        HistoryPolicy = PolicySettings?.HistoryPolicy ?? HistoryPolicyType.RecentMessages,
        MaxHistoryMessages = PolicySettings?.MaxHistoryMessages,
        AllowManualHistorySelection = PolicySettings?.AllowManualHistorySelection ?? true,
        SupportsInternalMemory = PolicySettings?.SupportsInternalMemory ?? false,
        UseFullHistoryPayload = UseFullHistoryPayload,
        IncludeConversationHistory = IncludeConversationHistory,
        IncludeToolCalls = IncludeToolCalls,
        IncludeToolMetadata = IncludeToolMetadata,
        IncludeMetadata = IncludeMetadata
    };

    private static ModelContextSettings ApplyRuntimeContextSettings(
        ModelContextSettings modelSettings,
        RuntimeContextSettings runtime)
    {
        modelSettings ??= new ModelContextSettings();
        runtime ??= new RuntimeContextSettings();

        return new ModelContextSettings
        {
            ModelId = modelSettings.ModelId,
            ContextWindow = modelSettings.ContextWindow,
            DefaultOutputReserve = modelSettings.DefaultOutputReserve,
            SupportsInternalMemory = modelSettings.SupportsInternalMemory,
            HistoryPolicy = runtime.HistoryPolicy,
            UseFullHistoryPayload = runtime.UseFullHistoryPayload,
            IncludeConversationHistory = runtime.IncludeConversationHistory,
            IncludeToolCalls = runtime.IncludeToolCalls,
            IncludeToolMetadata = runtime.IncludeToolMetadata,
            IncludeMetadata = runtime.IncludeMetadata,
            MaxHistoryMessages = runtime.MaxHistoryMessages,
            AllowManualHistorySelection = runtime.AllowManualHistorySelection
        };
    }

    private RuntimeContextSettings BuildRuntimeContextSettings() => new()
    {
        HistoryPolicy = PolicySettings?.HistoryPolicy ?? HistoryPolicyType.RecentMessages,
        UseFullHistoryPayload = UseFullHistoryPayload,
        IncludeConversationHistory = IncludeConversationHistory,
        IncludeToolCalls = IncludeToolCalls,
        IncludeToolMetadata = IncludeToolMetadata,
        IncludeMetadata = IncludeMetadata,
        EnableAutomaticCompression = EnableAutomaticCompression,
        EnableMemoryInjection = EnableMemoryInjection,
        MaxHistoryMessages = PolicySettings?.MaxHistoryMessages,
        AllowManualHistorySelection = PolicySettings?.AllowManualHistorySelection ?? true
    };

    private Task PersistRuntimeContextSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId) || string.IsNullOrWhiteSpace(ModelId))
            return Task.CompletedTask;

        return _appState.UpdateRuntimeContextSettingsAsync(
            SessionId,
            ModelId,
            BuildRuntimeContextSettings());
    }

    private void ApplyPayloadFromPolicy(ModelContextSettings s)
    {
        UseFullHistoryPayload = s.UseFullHistoryPayload;
        IncludeConversationHistory = s.IncludeConversationHistory;
        IncludeToolCalls = s.IncludeToolCalls;
        IncludeToolMetadata = s.IncludeToolMetadata;
        IncludeMetadata = s.IncludeMetadata;
    }

    private void ClearAllOverrides()
    {
        _forcedIncludeIds.Clear();
        _forcedExcludeIds.Clear();
        _pinnedIds.Clear();
        _cachedMessages.Clear();
        _mutableItems.Clear();
        _baseModelSettings = null;
        PolicySettings = null;
        SelectedProfileType = SessionContextProfileType.Default;
        SelectedProfile = _sessionContextProfileService.GetProfile(SelectedProfileType);
        BudgetState = null;
        UsageMetrics = null;
        DebugPackage = null;
        CurrentCompressionResult = null;
        CurrentRMAnalysis = null;
        CurrentMemoryRetrieval = null;
        EnableAutomaticCompression = false;
        EnableRMSelection = false;
        EnableMemoryInjection = false;
        IsPromptInspectorVisible = false;
    }

    private ContextUsageMetrics CalculateUsageMetrics(ModelContextSettings effective)
    {
        var package = BuildPromptContextPackage(effective);
        return _contextUsageMonitor.Calculate(package, effective);
    }

    private PromptDebugPackage BuildDebugPackage(ModelContextSettings effective)
    {
        var package = BuildPromptContextPackage(effective);
        package.Metadata["ExcludedItemCount"] = _mutableItems.Count(i => !i.IsIncluded).ToString();
        return _promptDebugFormatter.BuildDebugPackage(package, effective);
    }

    private PromptContextPackage BuildPromptContextPackage(ModelContextSettings effective)
    {
        var selectionResult = new ContextSelectionResult
        {
            SessionId = SessionId,
            ModelId = ModelId,
            Items = GetAssemblyItems().Select(CopyItem).ToList(),
            BudgetState = BudgetState ?? new PromptBudgetState
            {
                ModelId = ModelId,
                ContextWindow = effective.ContextWindow,
                OutputReserve = effective.DefaultOutputReserve
            }
        };

        var package = _promptAssemblyService.Assemble(
            SessionId,
            ModelId,
            CurrentPrompt,
            selectionResult,
            effective);

        package.Metadata["ExcludedItemCount"] = _mutableItems.Count(i => !i.IsIncluded).ToString();
        package.Metadata["VisibleItemCount"] = _mutableItems.Count.ToString();
        package.Metadata["CompressionEnabled"] = EnableAutomaticCompression.ToString();
        package.Metadata["CompressionApplied"] = (CurrentCompressionResult?.CompressionApplied ?? false).ToString();
        package.Metadata["CompressionTokensSaved"] = (CurrentCompressionResult?.TokensSaved ?? 0).ToString();
        package.Metadata["CompressedBlockCount"] = (CurrentCompressionResult?.CompressedBlocks.Count ?? 0).ToString();
        package.Metadata["RMSelectionEnabled"] = EnableRMSelection.ToString();
        package.Metadata["RMAnalysisApplied"] = (CurrentRMAnalysis?.AnalysisApplied ?? false).ToString();
        package.Metadata["RMScoreCount"] = (CurrentRMAnalysis?.Scores.Count ?? 0).ToString();
        package.Metadata["MemoryInjectionEnabled"] = EnableMemoryInjection.ToString();
        package.Metadata["MemoryRetrievalApplied"] = (CurrentMemoryRetrieval?.RetrievalApplied ?? false).ToString();
        package.Metadata["MemoryInjectionCount"] = (CurrentMemoryRetrieval?.Items.Count ?? 0).ToString();
        return package;
    }

    private async Task ApplyMemoryInjectionAsync(ModelContextSettings effective)
    {
        CurrentMemoryRetrieval = null;
        if (!EnableMemoryInjection)
            return;

        CurrentMemoryRetrieval = await _memoryContextProvider.RetrieveAsync(
            SessionId,
            ModelId,
            CurrentPrompt,
            effective);

        var memoryItems = MemoryContextInjectionMapper.Map(CurrentMemoryRetrieval);
        if (memoryItems.Count == 0)
            return;

        _mutableItems.AddRange(memoryItems);
        ApplyMemoryBudget(effective);
    }

    private void ApplyMemoryBudget(ModelContextSettings effective)
    {
        var available = Math.Max(0, effective.ContextWindow - effective.DefaultOutputReserve);
        var used = _mutableItems
            .Where(i => i.IsIncluded)
            .Sum(i => Math.Max(0, i.EstimatedTokens));

        if (used <= available)
            return;

        foreach (var item in _mutableItems
                     .Where(IsMemoryInjectionItem)
                     .OrderBy(ReadMemoryRelevanceScore)
                     .ThenByDescending(i => Math.Max(0, i.EstimatedTokens)))
        {
            if (used <= available)
                break;

            if (item.IsPinned || item.SelectionStatus == PromptContextSelectionStatus.ForcedByUser)
                continue;

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

    private List<PromptContextItem> GetAssemblyItems()
    {
        if (!EnableAutomaticCompression ||
            CurrentCompressionResult == null ||
            !CurrentCompressionResult.CompressionApplied)
        {
            return _mutableItems.Select(CopyItem).ToList();
        }

        var items = CurrentCompressionResult.PreservedItems.Select(CopyItem).ToList();
        items.AddRange(CurrentCompressionResult.CompressedBlocks.Select(ToCompressedPromptItem));
        return items;
    }

    private static PromptContextItem ToCompressedPromptItem(CompressedContextBlock block)
    {
        var metadata = new Dictionary<string, string>(block.Metadata ?? new Dictionary<string, string>())
        {
            ["Compressed"] = "true",
            ["SourceMessageCount"] = block.OriginalMessageCount.ToString(),
            ["OriginalEstimatedTokens"] = block.OriginalEstimatedTokens.ToString(),
            ["CompressedEstimatedTokens"] = block.CompressedEstimatedTokens.ToString(),
            ["TokensSaved"] = Math.Max(0, block.OriginalEstimatedTokens - block.CompressedEstimatedTokens).ToString()
        };

        return new PromptContextItem
        {
            ItemId = block.BlockId,
            ItemType = PromptContextItemType.MemoryInjection,
            SourceId = block.BlockId,
            SourceLabel = "Compressed Summary",
            Content = CompressionFormatter.FormatBlock(block),
            EstimatedTokens = Math.Max(0, block.CompressedEstimatedTokens),
            IsIncluded = true,
            SelectionStatus = PromptContextSelectionStatus.Included,
            Metadata = metadata
        };
    }

    private List<SessionChatMessage> BuildSelectedOffloadMessages()
    {
        var itemsById = _mutableItems
            .Where(IsSessionBackedItem)
            .ToDictionary(i => i.ItemId, StringComparer.OrdinalIgnoreCase);

        return _cachedMessages
            .Where(m => _selectedItemIds.Contains(m.MessageId))
            .OrderBy(m => m.Timestamp)
            .Select(m => CopyMessageForOffload(m, itemsById, BuildOffloadMetadata()))
            .ToList();
    }

    private static SessionChatMessage CopyMessageForOffload(
        SessionChatMessage message,
        IReadOnlyDictionary<string, PromptContextItem> itemsById,
        IReadOnlyDictionary<string, string> offloadMetadata)
    {
        itemsById.TryGetValue(message.MessageId, out var item);
        var metadata = new Dictionary<string, string>(message.Metadata ?? new Dictionary<string, string>());
        foreach (var pair in offloadMetadata)
            metadata[pair.Key] = pair.Value;

        if (item != null)
        {
            metadata["SelectionStatus"] = item.SelectionStatus.ToString();
            metadata["WasIncludedInPromptPreview"] = item.IsIncluded.ToString();
            metadata["SourceLabel"] = item.SourceLabel;
        }

        return new SessionChatMessage
        {
            MessageId = message.MessageId ?? string.Empty,
            Role = message.Role,
            Timestamp = message.Timestamp,
            Content = message.Content ?? string.Empty,
            ModelId = message.ModelId,
            EstimatedTokens = item?.EstimatedTokens ?? message.EstimatedTokens,
            IsPinned = item?.IsPinned ?? message.IsPinned,
            IsExcluded = item?.SelectionStatus == PromptContextSelectionStatus.ExcludedByUser || message.IsExcluded,
            Metadata = metadata
        };
    }

    private Dictionary<string, string> BuildOffloadMetadata() => new()
    {
        ["Offload.SelectedModelId"] = ModelId,
        ["Offload.HistoryPolicy"] = (PolicySettings?.HistoryPolicy ?? HistoryPolicyType.RecentMessages).ToString(),
        ["Offload.ContextProfile"] = SelectedProfileType.ToString(),
        ["Offload.UseFullHistoryPayload"] = UseFullHistoryPayload.ToString(),
        ["Offload.IncludeConversationHistory"] = IncludeConversationHistory.ToString(),
        ["Offload.IncludeToolCalls"] = IncludeToolCalls.ToString(),
        ["Offload.IncludeToolMetadata"] = IncludeToolMetadata.ToString(),
        ["Offload.IncludeMetadata"] = IncludeMetadata.ToString()
    };

    private void PruneSelection()
    {
        var selectableIds = _mutableItems
            .Where(IsSessionBackedItem)
            .Select(i => i.ItemId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _selectedItemIds.RemoveWhere(id => !selectableIds.Contains(id));
    }

    private static bool IsSessionBackedItem(PromptContextItem item) =>
        !string.IsNullOrWhiteSpace(item.ItemId) &&
        !string.Equals(item.ItemId, "current-prompt", StringComparison.OrdinalIgnoreCase);

    private static PromptContextItem CopyItem(PromptContextItem src) => new()
    {
        ItemId = src.ItemId,
        ItemType = src.ItemType,
        SourceId = src.SourceId,
        SourceLabel = src.SourceLabel,
        Content = src.Content,
        EstimatedTokens = src.EstimatedTokens,
        IsIncluded = src.IsIncluded,
        IsPinned = src.IsPinned,
        IsUserOverride = src.IsUserOverride,
        SelectionStatus = src.SelectionStatus,
        Metadata = new Dictionary<string, string>(src.Metadata)
    };

    private void MarkCustomProfile()
    {
        if (SelectedProfileType == SessionContextProfileType.Custom)
            return;

        SelectedProfileType = SessionContextProfileType.Custom;
        SelectedProfile = _sessionContextProfileService.GetProfile(SessionContextProfileType.Custom)
            ?? new SessionContextProfile
            {
                ProfileId = "custom",
                Name = "Custom",
                Description = "Manual adjustments after selecting a profile.",
                ProfileType = SessionContextProfileType.Custom
            };
    }

    private void Notify() => OnChange?.Invoke();

    /// <summary>
    /// Releases resources held by the pane view model.
    /// </summary>
    public void Dispose()
    {
    }
}
