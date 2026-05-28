# AVA — WPF → Cross-Platform Blazor Migration Plan

## Target Stack

| Layer | Technology | Platforms |
|---|---|---|
| Core logic | .NET 8 class library | All |
| Shared UI | Blazor Component Library | All |
| Desktop shell | Photino.NET | Windows, macOS, Ubuntu |
| Web shell | Blazor Server / WASM | Browser |

---

## Solution Structure

```
AVA.sln
│
├── AVA.Core/                        ← Ported from AVA.WPF.CORE (unchanged logic)
│   ├── Interfaces/
│   │   ├── IAvaCoreInterface.cs
│   │   ├── IAvaSettingsService.cs
│   │   ├── IEndpointClientService.cs
│   │   ├── ILogService.cs
│   │   ├── IMemoryTracer.cs
│   │   ├── IVaultService.cs
│   │   └── Vault/  (IVaultGraph, IVaultNote, IVaultTag, etc.)
│   ├── Models/
│   │   ├── Settings/  (AppSettings, LLMProfile, ConnectionProfiles...)
│   │   ├── Vault/     (VaultGraph, VaultNote, VaultTag, etc.)
│   │   ├── AvaResponse.cs
│   │   ├── OutputSegment.cs
│   │   └── Enums.cs
│   ├── Pipeline/
│   │   └── AvaInteractionPipeline.cs
│   ├── Services/
│   │   ├── AvaSettingsService.cs
│   │   ├── EndpointClientService.cs
│   │   ├── VaultService.cs
│   │   └── VaultManager.cs
│   └── Utilities/
│       ├── ResponseAnalyzer.cs
│       └── PromptFormatter.cs
│
├── AVA.UI.Components/               ← NEW: Shared Blazor component library
│   ├── Components/
│   │   ├── Chat/
│   │   │   ├── InputPane.razor
│   │   │   ├── OutputPane.razor
│   │   │   ├── OutputSegmentRenderer.razor
│   │   │   └── PromptPreviewPane.razor
│   │   ├── Memory/
│   │   │   └── MemoryLogPane.razor
│   │   ├── Reflection/
│   │   │   └── ReflectionPane.razor
│   │   ├── Settings/
│   │   │   ├── SettingsPane.razor
│   │   │   ├── LLMProfilesPanel.razor
│   │   │   ├── ConnectionProfilesPanel.razor
│   │   │   └── EditLLMProfileDialog.razor
│   │   ├── Vault/
│   │   │   ├── VaultPane.razor
│   │   │   ├── NotesGrid.razor
│   │   │   ├── TagsGrid.razor
│   │   │   ├── ProjectsGrid.razor
│   │   │   ├── LinksGrid.razor
│   │   │   └── TasksGrid.razor
│   │   └── Shared/
│   │       ├── StatusBar.razor
│   │       ├── FilteredGrid.razor
│   │       └── NavSidebar.razor
│   ├── Layout/
│   │   └── MainLayout.razor
│   ├── State/
│   │   └── AppState.cs             ← Replaces MainWinVM + nested VMs
│   └── wwwroot/
│       └── app.css
│
├── AVA.App.Desktop/                 ← Photino.NET shell
│   ├── Program.cs
│   └── AVA.App.Desktop.csproj
│
└── AVA.App.Web/                     ← Blazor Server shell
    ├── Program.cs
    ├── App.razor
    └── AVA.App.Web.csproj
```

---

## Migration Rules by File Category

### ✅ Zero Changes Required (copy as-is)
- All `Interfaces/` files
- All `Models/` files (Settings, Vault, AvaResponse, OutputSegment, Enums)
- `AvaInteractionPipeline.cs`
- `EndpointClientService.cs`
- `AvaSettingsService.cs`
- `VaultService.cs`
- `VaultManager.cs`
- `ResponseAnalyzer.cs`
- `PromptFormatter.cs`

### 🔄 Adapt (minor changes)
- `ViewModelBase` → becomes `AppState.cs` (uses `StateHasChanged` pattern)
- `RelayCommand` → keep for non-UI logic; UI uses `@onclick` directly
- `FilteredGridViewModel<T>` → becomes `FilteredGrid<T>.razor` generic component

### ❌ Replace (XAML → Razor)
- All `.xaml` views → `.razor` components (provided in this migration)
- All `*_xaml.cs` code-behinds → removed, logic absorbed into Razor `@code` blocks
- WPF Converters → Replaced with C# helper methods inline in Razor

### 🗑️ Remove (WPF-specific, not needed)
- `BooleanToBrushConverter.cs`
- `BooleanToVisibilityConverter.cs`
- `BoolToYesNoConverter.cs`
- `CoreModeToVisibilityConverter.cs`
- `InverseBooleanConverter.cs`
- `NullToVisibilityConverter.cs`
- `TagToBrushConverter.cs`
- `OutputSegmentTemplateSelector.cs`
- `PromptSegmentTemplateSelector.cs`
- `CoreLauncher.cs` (Windows-only process launcher)

---

## State Management Strategy

Replace the WPF VM tree with a single injectable `AppState` service:

```
WPF                          Blazor
─────────────────────────────────────
MainWinVM                 →  AppState (scoped service)
InputPaneVM               →  InputPane.razor @code block
OutputPaneVM              →  OutputPane.razor @code block
MemoryLogPaneVM           →  MemoryLogPane.razor @code block
ReflectionPaneVM          →  ReflectionPane.razor @code block
SettingsPaneVM            →  SettingsPane.razor @code block
VaultPaneViewModel        →  VaultPane.razor @code block
FilteredGridViewModel<T>  →  FilteredGrid<T>.razor generic
```

`AppState` holds shared cross-pane state:
- Active connection status
- Selected LLM profile
- Initialization state of the pipeline

---

## Key Behaviour Notes

1. **Late-wired InputVM** — In WPF, `InputVM` was null until `OnConnectionCompleted` fired.
   In Blazor, `InputPane` simply checks `AppState.IsConnected` and disables the send button until ready.

2. **ObservableCollection** — Replaced with `List<T>` + `StateHasChanged()` calls.

3. **OutputPane JSON parsing** — `OutputPaneVM.ReceiveResponse()` uses fragile Regex on JSON.
   Migration replaces this with proper `JsonDocument` deserialization (already partially done).

4. **Settings persistence** — `AvaSettingsService` uses `Environment.SpecialFolder.LocalApplicationData`
   which works on Windows, macOS, and Linux. No changes needed.

5. **Async void** — `SubmitPrompt` was `async void`. In Blazor event handlers `async void` is acceptable
   for top-level UI events, but exceptions should be caught and surfaced via AppState.

---

## csproj Templates

### AVA.Core.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### AVA.UI.Components.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\AVA.Core\AVA.Core.csproj" />
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### AVA.App.Desktop.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\AVA.UI.Components\AVA.UI.Components.csproj" />
    <PackageReference Include="Photino.Blazor" Version="3.1.0" />
  </ItemGroup>
</Project>
```

### AVA.App.Web.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\AVA.UI.Components\AVA.UI.Components.csproj" />
  </ItemGroup>
</Project>
```
