using AVA.App.Web.Components;
using AVA.Memory.Abstractions;
using AVA.Memory.Sql.Context;
using AVA.Memory.Sql.Stores;
using AVA.UI.CORE.ChatContext.Budgeting;
using AVA.UI.CORE.ChatContext.Builders;
using AVA.UI.CORE.ChatContext.Compression;
using AVA.UI.CORE.ChatContext.Debugging;
using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Memory;
using AVA.UI.CORE.ChatContext.Monitoring;
using AVA.UI.CORE.ChatContext.Offload;
using AVA.UI.CORE.ChatContext.Policies;
using AVA.UI.CORE.ChatContext.Profiles;
using AVA.UI.CORE.ChatContext.RM;
using AVA.UI.CORE.ChatContext.Services;
using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Services;
using AVA.UI.CORE.Services.Profiles;
using AVA.UI.CORE.Services.Storage;
using AVA.UI.Errors;
using AVA.UI.Features.Chat.State;
using AVA.UI.Features.Chat.ViewModels;
using AVA.UI.Features.ChatContext.ViewModels;
using AVA.UI.Features.Navigation.State;
using AVA.UI.Features.Navigation.ViewModels;
using AVA.UI.Features.Canvas.ViewModels;
using AVA.UI.Features.Vault.ViewModels;
using AVA.UI.Features.Reflection.State;
using AVA.UI.Features.Reflection.ViewModels;
using AVA.UI.Features.Settings.Actions;
using AVA.UI.Features.Settings.State;
using AVA.UI.Features.Settings.ViewModels;
using AVA.UI.Features.Shell.ViewModels;
using AVA.UI.Runtime;
using AVA.UI.State;
using AVA.UI.Vault.Services;
using AVA.Vault.Core.Adapters;
using AVA.Vault.Core.Config;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Persistence;
using AVA.Vault.Core.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(o => o.DetailedErrors = true);

// â”€â”€ EF Core (in-memory for dev) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddDbContextFactory<VaultDbContext>(o => o.UseInMemoryDatabase("AVA_Vault"));
builder.Services.AddDbContextFactory<MemoryDbContext>(o => o.UseInMemoryDatabase("AVA_Memory"));

// â”€â”€ Base state & services â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddScoped<AvaSettingsService>();
builder.Services.AddScoped<VaultWorkspaceFileService>();
builder.Services.AddScoped<ErrorState>();
builder.Services.AddScoped<ISessionStorageService, SessionStorageService>();
builder.Services.AddScoped<ISessionModelStateStore, SessionModelStateStore>();
builder.Services.AddScoped<IAvaIdService, AvaIdService>();
builder.Services.AddScoped<NavigationState>();
builder.Services.AddScoped<ReflectionState>();
builder.Services.AddScoped<SettingsState>();
builder.Services.AddScoped<ISessionChatLogService, SessionChatLogService>();
builder.Services.AddScoped<ISessionChatHistoryService, SessionChatHistoryService>();
builder.Services.AddScoped<ChatConversationState>();

// â”€â”€ Vault â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddScoped<VaultInstanceConfig>();
builder.Services.AddScoped<VaultLogger>();
builder.Services.AddScoped<IVaultIdService, VaultIdService>();
builder.Services.AddScoped<VaultManager>();
builder.Services.AddScoped<IProfilePersistenceProvider, DbProfilePersistenceProvider>();
builder.Services.AddScoped<LlmProfileService>();
builder.Services.AddScoped<IMemoryStore, SqlMemoryStore>();
builder.Services.AddScoped<IVaultUiSyncService>(sp =>
{
    var dbFactory  = sp.GetRequiredService<IDbContextFactory<VaultDbContext>>();
    var logger     = sp.GetRequiredService<VaultLogger>();
    var ids        = sp.GetRequiredService<IVaultIdService>();
    var vaultMgr   = sp.GetRequiredService<VaultManager>();
    var memStore   = sp.GetRequiredService<IMemoryStore>();
    var genericLog = sp.GetRequiredService<ILogger<VaultUiSyncService>>();
    var dbProvider = new DbVaultPersistenceProvider(dbFactory, logger, ids);
    var fileProv   = new FileVaultPersistenceProvider(vaultMgr, logger, ids, "Vaults");
    return new VaultUiSyncService(dbFactory, dbProvider, fileProv, memStore, logger, genericLog);
});

// â”€â”€ Runtime contexts (null stubs) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddScoped<IIdentityRuntimeContext, NullIdentityRuntimeContext>();
builder.Services.AddScoped<IMemoryRuntimeContext, NullMemoryRuntimeContext>();
builder.Services.AddScoped<IResolutionMatrixContext, NullResolutionMatrixContext>();
builder.Services.AddScoped<IAvaRuntimeContext, AvaRuntimeContext>();

// â”€â”€ ViewModels â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddScoped<LeftNavVM>();
builder.Services.AddScoped<DockShellVM>();
builder.Services.AddScoped<VaultSearchVM>();
builder.Services.AddScoped<VaultNoteEditorVM>();

// â”€â”€ AppState â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddScoped<AppState>();

// â”€â”€ PromptContextPaneVM chain (singleton calculators, scoped VM) â”€â”€
builder.Services.AddSingleton<ITokenEstimator, TokenEstimator>();
builder.Services.AddSingleton<IContextBudgetCalculator, ContextBudgetCalculator>();
builder.Services.AddSingleton<IHistorySelectionPolicy, DeterministicHistorySelectionPolicy>();
builder.Services.AddSingleton<IChatContextOffloadService, ChatContextOffloadService>();
builder.Services.AddSingleton<IPromptAssemblyService, PromptAssemblyService>();
builder.Services.AddSingleton<IContextUsageMonitor, ContextUsageMonitor>();
builder.Services.AddSingleton<IPromptDebugFormatter, PromptDebugFormatter>();
builder.Services.AddSingleton<ISessionContextProfileService, SessionContextProfileService>();
builder.Services.AddSingleton<IContextCompressionService, ContextCompressionService>();
builder.Services.AddSingleton<IRMContextAnalyzer, RMContextAnalyzer>();
builder.Services.AddSingleton<IMemoryContextProvider, NullMemoryContextProvider>();
builder.Services.AddScoped<IModelContextPolicyResolver, ModelContextPolicyResolver>();
builder.Services.AddScoped<RightContextNavVM>();
builder.Services.AddScoped<PromptContextPaneVM>();

// â”€â”€ Additional ViewModels for AVA.UI pages â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddScoped<OutputPaneVM>();
builder.Services.AddScoped<MemoryLogPaneVM>();
builder.Services.AddScoped<ReflectionPaneVM>();
builder.Services.AddScoped<SessionCanvasVM>();
builder.Services.AddScoped<ConnectAction>();
builder.Services.AddScoped<TestEndpointAction>();
builder.Services.AddScoped<SettingsPaneVM>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


