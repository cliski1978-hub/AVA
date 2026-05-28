// ─────────────────────────────────────────────────────────────────────────────
//  AVA.App.Desktop — Program.cs
//  Shell: Photino.Blazor
//  Platforms: Windows, macOS, Ubuntu (Linux)
//
//  Photino hosts a native OS webview window and serves the Blazor app
//  inside it. No browser required. Works natively on Ubuntu via WebKitGTK.
//
//  Install:
//      dotnet add package Photino.Blazor
//
//  Ubuntu prerequisites:
//      sudo apt install libwebkit2gtk-4.1-0
// ─────────────────────────────────────────────────────────────────────────────

using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using AVA.Core.Services;
using AVA.Core.Services.Network;
using AVA.Core.Interfaces;
using AVA.UI.State;

var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

// ── Register services ────────────────────────────────────────────────────────
appBuilder.Services.AddSingleton<AvaSettingsService>();
appBuilder.Services.AddSingleton<IEndpointClientService, EndpointClientService>();
appBuilder.Services.AddSingleton<AppState>();

// Root Blazor component
appBuilder.RootComponents.Add<AVA.UI.App>("app");

var app = appBuilder.Build();

// ── Window configuration ─────────────────────────────────────────────────────
app.MainWindow
    .SetTitle("AVA")
    .SetWidth(1280)
    .SetHeight(800)
    .SetResizable(true)
    .SetChromeless(false)
    .SetIconFile("wwwroot/icon.ico"); // add your icon here

AppDomain.CurrentDomain.UnhandledException += (sender, ex) =>
{
    Console.Error.WriteLine($"[AVA Unhandled] {ex.ExceptionObject}");
};

app.Run();
