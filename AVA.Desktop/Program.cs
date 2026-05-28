// ─────────────────────────────────────────────────────────────────────────────
//  AVA.App.Desktop — Program.cs
//  Shell: Photino.Blazor 4.x
//  Platforms: Windows, macOS, Ubuntu
// ─────────────────────────────────────────────────────────────────────────────

using System;
using Photino.Blazor;
using AVA.UI.Features.Canvas;
using AVA.UI.Features.Canvas.Actions;
using AVA.UI.Features.Canvas.Services;
using AVA.UI.Plugins;
using AVA.UI.Runtime;
using AVA.UI.State;
using AVA.UI.CORE.Services;
using AVA.UI.CORE.Interfaces;
using AVA.UI.CORE.Services.Network;
using Microsoft.Extensions.DependencyInjection;

// ── Memory SQL ────────────────────────────────────────────────────────────────
using AVA.Memory.Sql.Configuration;
using AVA.Memory.Sql.Extensions;
using AVA.Memory.Sql.Context;
using Microsoft.EntityFrameworkCore;

// ── Vault ─────────────────────────────────────────────────────────────────────
using AVA.Vault.Core;
using AVA.Vault.Core.Config;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Persistence;
using AVA.Vault.Core.Services;
using AVA.Vault.Core.Adapters;
using AVA.Vault.Core.Interfaces;
using AVA.Memory.Abstractions;
using AVA.UI.CORE.Services.Profiles;
using AVA.UI.Vault.Services;
using Microsoft.Extensions.Logging;

// ── Feature State Stores ──────────────────────────────────────────────────────
using AVA.UI.Features.Navigation.State;
using AVA.UI.Features.Reflection.State;
using AVA.UI.Features.Chat.State;
using AVA.UI.Features.Settings.State;

// ── Error State ───────────────────────────────────────────────────────────────
using AVA.UI.Errors;

// ── Session Storage / Chat Logs ───────────────────────────────────────────────
using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Services.Storage;
using AVA.UI.CORE.ChatContext.Services;
using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Policies;
using AVA.UI.CORE.ChatContext.Budgeting;
using AVA.UI.CORE.ChatContext.Builders;
using AVA.UI.CORE.ChatContext.Offload;
using AVA.UI.CORE.ChatContext.Monitoring;
using AVA.UI.CORE.ChatContext.Debugging;
using AVA.UI.CORE.ChatContext.Profiles;
using AVA.UI.CORE.ChatContext.Compression;
using AVA.UI.CORE.ChatContext.RM;
using AVA.UI.CORE.ChatContext.Memory;

// ── Feature ViewModels ────────────────────────────────────────────────────────
using AVA.UI.Features.Navigation.ViewModels;
using AVA.UI.Features.Chat.ViewModels;
using AVA.UI.Features.Shell.ViewModels;
using AVA.UI.Features.Reflection.ViewModels;
using AVA.UI.Features.Vault.ViewModels;
using AVA.UI.Features.ChatContext.ViewModels;

namespace AVA.App.Desktop;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var AppBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        // ── Core Services ──────────────────────────────────────────────────────
        AppBuilder.Services.AddLogging();
        AppBuilder.Services.AddSingleton<AvaSettingsService>();
        AppBuilder.Services.AddSingleton<VaultWorkspaceFileService>();
        AppBuilder.Services.AddSingleton<IEndpointClientService, EndpointClientService>();
        AppBuilder.Services.AddSingleton<DockLayoutService>();

        // ── ID Service ─────────────────────────────────────────────────────────
        // All ID generation routes through IAvaIdService — never Guid.NewGuid() directly.
        AppBuilder.Services.AddSingleton<IAvaIdService, AvaIdService>();

        // ── Session Storage ────────────────────────────────────────────────────
        // Sprint spec: AddScoped — correct for Blazor Server (per-circuit isolation).
        // Photino desktop has no circuits so singleton is equivalent here.
        // When AVA.App.Web is the active host, switch these to AddScoped.
        AppBuilder.Services.AddSingleton<ISessionStorageService, SessionStorageService>();
        AppBuilder.Services.AddSingleton<ISessionChatLogService, SessionChatLogService>();
        AppBuilder.Services.AddSingleton<ISessionChatHistoryService, SessionChatHistoryService>();
        AppBuilder.Services.AddSingleton<ISessionModelStateStore, SessionModelStateStore>();

        // ── ChatContext — Sprint 3.5 ───────────────────────────────────────────
        AppBuilder.Services.AddSingleton<ITokenEstimator, TokenEstimator>();
        AppBuilder.Services.AddSingleton<IContextBudgetCalculator, ContextBudgetCalculator>();
        AppBuilder.Services.AddSingleton<IContextWindowTracker, ContextWindowTracker>();
        AppBuilder.Services.AddSingleton<IModelContextPolicyResolver, ModelContextPolicyResolver>();
        AppBuilder.Services.AddSingleton<IHistorySelectionPolicy, DeterministicHistorySelectionPolicy>();
        AppBuilder.Services.AddSingleton<IPromptAssemblyService, PromptAssemblyService>();
        AppBuilder.Services.AddSingleton<IPromptContextBuilderService, PromptContextBuilderService>();
        AppBuilder.Services.AddSingleton<IChatContextOffloadService, ChatContextOffloadService>();
        AppBuilder.Services.AddSingleton<IContextUsageMonitor, ContextUsageMonitor>();
        AppBuilder.Services.AddSingleton<IPromptDebugFormatter, PromptDebugFormatter>();
        AppBuilder.Services.AddSingleton<ISessionContextProfileService, SessionContextProfileService>();
        AppBuilder.Services.AddSingleton<IContextCompressionService, ContextCompressionService>();
        AppBuilder.Services.AddSingleton<IRMContextAnalyzer, RMContextAnalyzer>();
        AppBuilder.Services.AddSingleton<IMemoryContextProvider, NullMemoryContextProvider>();

        // ── Error State ────────────────────────────────────────────────────────
        AppBuilder.Services.AddSingleton<ErrorState>();

        // ── Feature State Stores ───────────────────────────────────────────────
        AppBuilder.Services.AddSingleton<NavigationState>();
        AppBuilder.Services.AddSingleton<ReflectionState>();
        AppBuilder.Services.AddSingleton<ChatConversationState>();
        AppBuilder.Services.AddSingleton<SettingsState>();

        // ── Feature ViewModels (singleton — shared for app lifetime) ───────────
        AppBuilder.Services.AddSingleton<LeftNavVM>();
        AppBuilder.Services.AddSingleton<OutputPaneVM>();
        AppBuilder.Services.AddSingleton<MemoryLogPaneVM>();
        AppBuilder.Services.AddSingleton<ReflectionPaneVM>();
        AppBuilder.Services.AddSingleton<DockShellVM>();
        AppBuilder.Services.AddSingleton<ChatPaneVM>();
        AppBuilder.Services.AddSingleton<VaultSearchVM>();
        AppBuilder.Services.AddSingleton<VaultNoteEditorVM>();
        AppBuilder.Services.AddSingleton<PromptContextPaneVM>();
        AppBuilder.Services.AddSingleton<RightContextNavVM>();

        AppBuilder.Services.AddSingleton<AppState>(ServiceProvider =>
        {
            CanvasActions.ConfigureDocumentService(
                ServiceProvider.GetRequiredService<ICanvasDocumentService>());

            CanvasActions.ConfigurePersistenceService(
                ServiceProvider.GetRequiredService<ICanvasDocumentPersistenceService>());

            return new AppState(
                ServiceProvider.GetRequiredService<AvaSettingsService>(),
                ServiceProvider.GetRequiredService<VaultWorkspaceFileService>(),
                ServiceProvider.GetRequiredService<IVaultUiSyncService>(),
                ServiceProvider.GetRequiredService<ErrorState>(),
                ServiceProvider.GetRequiredService<ISessionStorageService>(),
                ServiceProvider.GetRequiredService<ISessionModelStateStore>(),
                ServiceProvider.GetRequiredService<IAvaIdService>(),
                ServiceProvider.GetRequiredService<LlmProfileService>(),
                ServiceProvider.GetRequiredService<NavigationState>(),
                ServiceProvider.GetRequiredService<ReflectionState>(),
                ServiceProvider.GetRequiredService<ChatConversationState>(),
                ServiceProvider.GetRequiredService<SettingsState>());
        });

        // ── Canvas Services ────────────────────────────────────────────────────
        AppBuilder.Services.AddSingleton<ICanvasDocumentService, CanvasDocumentService>();
        AppBuilder.Services.AddSingleton<ICanvasDocumentPersistenceService, CanvasDocumentPersistenceService>();
        AppBuilder.Services.AddSingleton<ICanvasInteractionService, CanvasInteractionService>();
        AppBuilder.Services.AddSingleton<ICanvasWindowService, CanvasWindowService>();

        AppBuilder.Services.AddSingleton<IAVAPluginRegistry>(_ =>
        {
            var Registry = new AVAPluginRegistry();
            Registry.RegisterPluginAsync(new CanvasPlugin()).GetAwaiter().GetResult();
            return Registry;
        });

        // ── Memory SQL ────────────────────────────────────────────────────────
        SqlMemoryConfigurator.Configure(opts =>
        {
            opts.ConnectionString = "Server=4D-C76\\SQLEXPRESS;Database=AvaMemory;Trusted_Connection=True;TrustServerCertificate=True;";
        });

        var MemoryOpts = SqlMemoryConfigurator.GetOptions();

        AppBuilder.Services.AddDbContextFactory<MemoryDbContext>(options =>
            options.UseSqlServer(MemoryOpts.ConnectionString));

        AppBuilder.Services.AddMemorySql(singleton: true);

        // ── Vault ─────────────────────────────────────────────────────────────
        var VaultConfig = new VaultInstanceConfig
        {
            VaultConnectionString = "Server=4D-C76\\SQLEXPRESS;Database=AvaVault;Trusted_Connection=True;TrustServerCertificate=True;",
            DisplayName = "AVA Vault",
            AutoMigrate = false,
            MockMode = false
        };

        AppBuilder.Services.AddVaultModule(VaultConfig);

        // ── Vault Persistence Providers ───────────────────────────────────────
        var VaultsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AVA", "Vaults");

        AppBuilder.Services.AddSingleton<VaultManager>(_ => new VaultManager(VaultsRoot));

        AppBuilder.Services.AddSingleton<DbVaultPersistenceProvider>();
        AppBuilder.Services.AddSingleton<FileVaultPersistenceProvider>(sp => new FileVaultPersistenceProvider(
            sp.GetRequiredService<VaultManager>(),
            sp.GetRequiredService<VaultLogger>(),
            sp.GetRequiredService<IVaultIdService>(),
            VaultsRoot));

        // ── Profile Persistence ──────────────────────────────────────────────
        AppBuilder.Services.AddSingleton<IProfilePersistenceProvider, DbProfilePersistenceProvider>();
        AppBuilder.Services.AddSingleton<LlmProfileService>();

        // ── Vault UI Bridge ───────────────────────────────────────────────────
        AppBuilder.Services.AddSingleton<IVaultUiSyncService>(sp => new VaultUiSyncService(
            sp.GetRequiredService<IDbContextFactory<VaultDbContext>>(),
            sp.GetRequiredService<DbVaultPersistenceProvider>(),
            sp.GetRequiredService<FileVaultPersistenceProvider>(),
            sp.GetRequiredService<IMemoryStore>(),
            sp.GetRequiredService<VaultLogger>(),
            sp.GetRequiredService<ILogger<VaultUiSyncService>>()));

        // ── Runtime Context ───────────────────────────────────────────────────
        // App-wide service access surface only; EF contexts remain short-lived
        // behind Vault/Memory services and are never exposed through this slot.
        AppBuilder.Services.AddSingleton<IIdentityRuntimeContext, NullIdentityRuntimeContext>();
        AppBuilder.Services.AddSingleton<IMemoryRuntimeContext, NullMemoryRuntimeContext>();
        AppBuilder.Services.AddSingleton<IResolutionMatrixContext, NullResolutionMatrixContext>();
        AppBuilder.Services.AddSingleton<IAvaRuntimeContext, AvaRuntimeContext>();

        // ── Root UI ────────────────────────────────────────────────────────────
        AppBuilder.RootComponents.Add<AVA.UI.App>("app");

        var App = AppBuilder.Build();

        // ── Window Config ──────────────────────────────────────────────────────
        App.MainWindow
            .SetTitle("AVA")
            .SetWidth(1280)
            .SetHeight(800)
            .SetResizable(true);

        AppDomain.CurrentDomain.UnhandledException += (Sender, Error) =>
        {
            App.MainWindow.ShowMessage("AVA — Fatal Error", Error.ExceptionObject.ToString());
        };

        App.Run();
    }
}
