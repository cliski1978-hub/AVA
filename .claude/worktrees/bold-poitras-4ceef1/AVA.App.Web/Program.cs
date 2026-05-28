// ─────────────────────────────────────────────────────────────────────────────
//  AVA.App.Web — Program.cs
//  Shell: Blazor Server
//
//  Runs AVA in the browser via Blazor Server (SignalR).
//  All component code runs on the server; the browser receives UI diffs.
//
//  Run:
//      dotnet run
//      Open http://localhost:5000
// ─────────────────────────────────────────────────────────────────────────────

using AVA.UI.CORE.Services;
using AVA.UI.CORE.Services.Network;
using AVA.UI.CORE.Interfaces;
using AVA.UI.State;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Server ─────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── AVA Services ──────────────────────────────────────────────────────────────
// Scoped so each browser session gets its own isolated state
builder.Services.AddScoped<AvaSettingsService>();
builder.Services.AddScoped<IEndpointClientService, EndpointClientService>();
builder.Services.AddScoped<DockLayoutService>();
builder.Services.AddScoped<AppState>();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<AVA.UI.App>()
   .AddInteractiveServerRenderMode();

app.Run();