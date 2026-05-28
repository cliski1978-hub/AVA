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
using AVA.UI.State;
using AVA.UI.CORE.Services;
using AVA.UI.CORE.Interfaces;
using AVA.UI.CORE.Services.Network;
using Microsoft.Extensions.DependencyInjection;

namespace AVA.App.Desktop;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var AppBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        AppBuilder.Services.AddLogging();
        AppBuilder.Services.AddSingleton<AvaSettingsService>();
        AppBuilder.Services.AddSingleton<IEndpointClientService, EndpointClientService>();
        AppBuilder.Services.AddSingleton<DockLayoutService>();
        AppBuilder.Services.AddSingleton<AppState>(ServiceProvider =>
        {
            CanvasActions.ConfigureDocumentService(
                ServiceProvider.GetRequiredService<ICanvasDocumentService>());
            CanvasActions.ConfigurePersistenceService(
                ServiceProvider.GetRequiredService<ICanvasDocumentPersistenceService>());
            return new AppState(ServiceProvider.GetRequiredService<AvaSettingsService>(),
                                ServiceProvider.GetRequiredService<IEndpointClientService>());
        });
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

        AppBuilder.RootComponents.Add<AVA.UI.App>("app");

        var App = AppBuilder.Build();

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
