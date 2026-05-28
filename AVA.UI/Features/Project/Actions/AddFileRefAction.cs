namespace AVA.UI.Features.Project.Actions;

/// <summary>
/// Adds a file reference to an active Project.
/// Replaces: AppState.AddProjectFileRefAsync(), AppState.AddPlaceholderFileToActiveProjectAsync()
/// Pipeline: SessionToolbarVM → AddFileRefAction → ProjectUiState + AvaSettingsService → Notify
/// </summary>
public class AddFileRefAction
{
    // TODO: Inject ProjectUiState, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId, string projectId, string path, string? name)
}
