using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Navigation;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultNavigationReadService : IVaultNavigationReadService
    {
        private readonly IVaultHeaderQueryService _vaultHeaderQuery;
        private readonly IVaultProjectQueryService _projectQuery;
        private readonly IVaultHeaderNoteQueryService _headerNoteQuery;
        private readonly IVaultProjectNoteQueryService _projectNoteQuery;
        private readonly IVaultSessionQueryService _sessionQuery;
        private readonly IVaultWorkflowQueryService _workflowQuery;
        private readonly IVaultNoteQueryService _noteQuery;
        private readonly VaultLogger _logger;

        public VaultNavigationReadService(
            IVaultHeaderQueryService vaultHeaderQuery,
            IVaultProjectQueryService projectQuery,
            IVaultHeaderNoteQueryService headerNoteQuery,
            IVaultProjectNoteQueryService projectNoteQuery,
            IVaultSessionQueryService sessionQuery,
            IVaultWorkflowQueryService workflowQuery,
            IVaultNoteQueryService noteQuery,
            VaultLogger logger)
        {
            _vaultHeaderQuery = vaultHeaderQuery ?? throw new ArgumentNullException(nameof(vaultHeaderQuery));
            _projectQuery = projectQuery ?? throw new ArgumentNullException(nameof(projectQuery));
            _headerNoteQuery = headerNoteQuery ?? throw new ArgumentNullException(nameof(headerNoteQuery));
            _projectNoteQuery = projectNoteQuery ?? throw new ArgumentNullException(nameof(projectNoteQuery));
            _sessionQuery = sessionQuery ?? throw new ArgumentNullException(nameof(sessionQuery));
            _workflowQuery = workflowQuery ?? throw new ArgumentNullException(nameof(workflowQuery));
            _noteQuery = noteQuery ?? throw new ArgumentNullException(nameof(noteQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNavigationTreeDto> GetVaultNavigationTreeAsync(string vaultId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(vaultId))
                throw new ArgumentException("Vault ID is required.", nameof(vaultId));

            var vault = await _vaultHeaderQuery.GetByIdAsync(vaultId, ct);
            if (vault == null)
                return new VaultNavigationTreeDto();

            var vaultDto = await BuildVaultDtoAsync(vault, ct);
            return new VaultNavigationTreeDto { Vaults = new List<VaultNavigationVaultDto> { vaultDto } };
        }

        public async Task<VaultNavigationTreeDto> GetAllVaultNavigationTreesAsync(CancellationToken ct = default)
        {
            var vaults = await _vaultHeaderQuery.GetActiveAsync(ct);

            var vaultDtos = new List<VaultNavigationVaultDto>();
            foreach (var vault in vaults.OrderBy(v => v.SortOrder).ThenBy(v => v.DisplayName))
            {
                var vaultDto = await BuildVaultDtoAsync(vault, ct);
                vaultDtos.Add(vaultDto);
            }

            _logger.Log(nameof(VaultNavigationReadService), $"Built navigation tree with {vaults.Count} vaults");
            return new VaultNavigationTreeDto { Vaults = vaultDtos };
        }

        public async Task<VaultNavigationProjectDto> GetProjectNavigationBranchAsync(string projectId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("Project ID is required.", nameof(projectId));

            var project = await _projectQuery.GetByIdAsync(projectId, ct);
            if (project == null)
                return new VaultNavigationProjectDto();

            return await BuildProjectBranchAsync(project, ct);
        }

        private async Task<VaultNavigationVaultDto> BuildVaultDtoAsync(VaultHeader vault, CancellationToken ct)
        {
            var vaultDto = new VaultNavigationVaultDto
            {
                VaultID = vault.ID,
                DisplayName = vault.DisplayName,
                Description = vault.Description,
                IsActive = vault.IsActive,
                SortOrder = vault.SortOrder
            };

            var headerNotes = await _headerNoteQuery.GetByVaultIdAsync(vault.ID, ct);
            var vaultSessions = await _sessionQuery.GetByVaultIdAsync(vault.ID, ct);
            var vaultLevelSessions = vaultSessions
                .Where(s => string.IsNullOrEmpty(s.ProjectID))
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ToList();

            var vaultNoteIds = headerNotes.Select(hn => hn.NoteID).Distinct().ToList();
            var vaultNotes = await LoadNoteDictionaryAsync(vaultNoteIds, ct);

            vaultDto.NotesGroup = BuildNoteGroup(headerNotes, vaultNotes, "Vault", vault.ID);

            vaultDto.SessionsGroup = BuildSessionGroup(vaultLevelSessions, "Vault");

            var projects = await _projectQuery.GetByVaultIdAsync(vault.ID, ct);
            foreach (var project in projects.OrderBy(p => p.SortOrder).ThenBy(p => p.Name))
            {
                var projectDto = await BuildProjectBranchAsync(project, ct);
                vaultDto.Projects.Add(projectDto);
            }

            return vaultDto;
        }

        private async Task<VaultNavigationProjectDto> BuildProjectBranchAsync(VaultProject project, CancellationToken ct)
        {
            var projectNotes = await _projectNoteQuery.GetByProjectIdAsync(project.ID, ct);
            var projectSessions = await _sessionQuery.GetByProjectIdAsync(project.ID, ct);
            var workflows = await _workflowQuery.GetByProjectIdAsync(project.ID, ct);

            var projectNoteIds = projectNotes.Select(pn => pn.NoteID).Distinct().ToList();
            var projectNotesDict = await LoadNoteDictionaryAsync(projectNoteIds, ct);

            var dto = new VaultNavigationProjectDto
            {
                ProjectID = project.ID,
                VaultID = project.VaultID,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status,
                IsArchived = project.IsArchived,
                IsExpanded = project.IsExpanded,
                SortOrder = project.SortOrder,
                NotesGroup = BuildNoteGroup(projectNotes, projectNotesDict, "Project", project.ID),
                WorkflowsGroup = BuildWorkflowGroup(workflows),
                SessionsGroup = BuildSessionGroup(
                    projectSessions.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToList(),
                    "Project")
            };

            return dto;
        }

        private async Task<Dictionary<string, VaultNote>> LoadNoteDictionaryAsync(List<string> noteIds, CancellationToken ct)
        {
            if (noteIds.Count == 0)
                return new Dictionary<string, VaultNote>();

            var notes = await _noteQuery.GetByIdsAsync(noteIds, ct);
            return notes.ToDictionary(n => n.ID);
        }

        private static VaultNavigationGroupDto BuildNoteGroup<T>(
            IEnumerable<T> links,
            Dictionary<string, VaultNote> notes,
            string parentType,
            string parentId) where T : class
        {
            var items = new List<VaultNavigationItemDto>();

            foreach (var link in links)
            {
                var noteId = GetNoteId(link);
                if (noteId == null || !notes.TryGetValue(noteId, out var note))
                    continue;

                var sortOrder = GetLinkSortOrder(link);
                items.Add(new VaultNavigationItemDto
                {
                    ItemID = note.ID,
                    ItemType = "note",
                    DisplayName = note.Title,
                    Description = note.Summary,
                    ParentID = parentId,
                    ParentType = parentType,
                    SortOrder = sortOrder ?? note.SortOrder,
                    IsPinned = note.IsPinned,
                    IsTemplate = note.IsTemplate,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt
                });
            }

            items = items.OrderBy(i => i.SortOrder).ThenBy(i => i.DisplayName).ToList();

            return new VaultNavigationGroupDto
            {
                GroupType = "notes",
                DisplayName = "Notes",
                ColorKey = "Purple",
                ItemCount = items.Count,
                Items = items
            };
        }

        private static VaultNavigationGroupDto BuildSessionGroup(List<VaultSession> sessions, string parentType)
        {
            var items = sessions.Select(s => new VaultNavigationItemDto
            {
                ItemID = s.ID,
                ItemType = "session",
                DisplayName = s.Name,
                Description = s.Description,
                SortOrder = s.SortOrder,
                IsPinned = s.IsPinned,
                IsTemplate = s.IsTemplate,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return new VaultNavigationGroupDto
            {
                GroupType = "sessions",
                DisplayName = "Sessions",
                ColorKey = "GreenPurpleBlend",
                ItemCount = items.Count,
                Items = items
            };
        }

        private static VaultNavigationGroupDto BuildWorkflowGroup(List<VaultWorkflow> workflows)
        {
            var items = workflows
                .OrderBy(w => w.SortOrder)
                .ThenBy(w => w.Name)
                .Select(w => new VaultNavigationItemDto
                {
                    ItemID = w.ID,
                    ItemType = "workflow",
                    DisplayName = w.Name,
                    Description = w.Description,
                    SortOrder = w.SortOrder,
                    IsPinned = false,
                    IsTemplate = false,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt
                }).ToList();

            return new VaultNavigationGroupDto
            {
                GroupType = "workflows",
                DisplayName = "Workflows",
                ColorKey = "Green",
                ItemCount = items.Count,
                Items = items
            };
        }

        private static string? GetNoteId(object link) => link switch
        {
            VaultHeaderNote hn => hn.NoteID,
            VaultProjectNote pn => pn.NoteID,
            _ => null
        };

        private static int? GetLinkSortOrder(object link) => link switch
        {
            VaultHeaderNote hn => hn.SortOrder,
            VaultProjectNote pn => pn.SortOrder,
            _ => null
        };
    }
}
