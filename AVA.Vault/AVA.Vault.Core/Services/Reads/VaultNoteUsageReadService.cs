using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultNoteUsageReadService : IVaultNoteUsageReadService
    {
        private readonly IVaultHeaderNoteQueryService _headerNoteQuery;
        private readonly IVaultProjectNoteQueryService _projectNoteQuery;
        private readonly IVaultSessionNoteQueryService _sessionNoteQuery;
        private readonly IVaultWorkflowNoteQueryService _workflowNoteQuery;
        private readonly IVaultWorkflowNodeNoteQueryService _nodeNoteQuery;
        private readonly IVaultWorkflowLineNoteQueryService _lineNoteQuery;
        private readonly IVaultWorkflowLineStepNoteQueryService _lineStepNoteQuery;
        private readonly IVaultNoteFileRefQueryService _noteFileRefQuery;
        private readonly IVaultFileRefNoteQueryService _fileRefNoteQuery;
        private readonly IVaultNoteRelationQueryService _noteRelationQuery;
        private readonly IVaultNoteVaultTagQueryService _noteTagQuery;
        private readonly IVaultNoteQueryService _noteQuery;
        private readonly VaultLogger _logger;

        public VaultNoteUsageReadService(
            IVaultHeaderNoteQueryService headerNoteQuery,
            IVaultProjectNoteQueryService projectNoteQuery,
            IVaultSessionNoteQueryService sessionNoteQuery,
            IVaultWorkflowNoteQueryService workflowNoteQuery,
            IVaultWorkflowNodeNoteQueryService nodeNoteQuery,
            IVaultWorkflowLineNoteQueryService lineNoteQuery,
            IVaultWorkflowLineStepNoteQueryService lineStepNoteQuery,
            IVaultNoteFileRefQueryService noteFileRefQuery,
            IVaultFileRefNoteQueryService fileRefNoteQuery,
            IVaultNoteRelationQueryService noteRelationQuery,
            IVaultNoteVaultTagQueryService noteTagQuery,
            IVaultNoteQueryService noteQuery,
            VaultLogger logger)
        {
            _headerNoteQuery = headerNoteQuery ?? throw new ArgumentNullException(nameof(headerNoteQuery));
            _projectNoteQuery = projectNoteQuery ?? throw new ArgumentNullException(nameof(projectNoteQuery));
            _sessionNoteQuery = sessionNoteQuery ?? throw new ArgumentNullException(nameof(sessionNoteQuery));
            _workflowNoteQuery = workflowNoteQuery ?? throw new ArgumentNullException(nameof(workflowNoteQuery));
            _nodeNoteQuery = nodeNoteQuery ?? throw new ArgumentNullException(nameof(nodeNoteQuery));
            _lineNoteQuery = lineNoteQuery ?? throw new ArgumentNullException(nameof(lineNoteQuery));
            _lineStepNoteQuery = lineStepNoteQuery ?? throw new ArgumentNullException(nameof(lineStepNoteQuery));
            _noteFileRefQuery = noteFileRefQuery ?? throw new ArgumentNullException(nameof(noteFileRefQuery));
            _fileRefNoteQuery = fileRefNoteQuery ?? throw new ArgumentNullException(nameof(fileRefNoteQuery));
            _noteRelationQuery = noteRelationQuery ?? throw new ArgumentNullException(nameof(noteRelationQuery));
            _noteTagQuery = noteTagQuery ?? throw new ArgumentNullException(nameof(noteTagQuery));
            _noteQuery = noteQuery ?? throw new ArgumentNullException(nameof(noteQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNoteUsageDto> GetNoteUsageAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var note = await _noteQuery.GetByIdAsync(noteId, ct);

            var headerLinks = await _headerNoteQuery.GetByNoteIdAsync(noteId, ct);
            var projectLinks = await _projectNoteQuery.GetByNoteIdAsync(noteId, ct);
            var sessionLinks = await _sessionNoteQuery.GetByNoteIdAsync(noteId, ct);
            var workflowLinks = await _workflowNoteQuery.GetByNoteIdAsync(noteId, ct);
            var nodeLinks = await _nodeNoteQuery.GetByNoteIdAsync(noteId, ct);
            var lineLinks = await _lineNoteQuery.GetByNoteIdAsync(noteId, ct);
            var lineStepLinks = await _lineStepNoteQuery.GetByNoteIdAsync(noteId, ct);
            var noteFileRefLinks = await _noteFileRefQuery.GetByNoteIdAsync(noteId, ct);
            var fileRefNoteLinks = await _fileRefNoteQuery.GetByNoteIdAsync(noteId, ct);
            var relations = await _noteRelationQuery.GetAllByNoteIdAsync(noteId, ct);
            var tagLinks = await _noteTagQuery.GetByNoteIdAsync(noteId, ct);

            var locations = new List<VaultNoteUsageLocationDto>();

            AddLocations(locations, headerLinks, noteId, "Vault", l => l.VaultID);
            AddLocations(locations, projectLinks, noteId, "Project", l => l.ProjectID);
            AddLocations(locations, sessionLinks, noteId, "Session", l => l.SessionID);
            AddLocations(locations, workflowLinks, noteId, "Workflow", l => l.WorkflowID);
            AddLocations(locations, nodeLinks, noteId, "WorkflowNode", l => l.WorkflowNodeID);
            AddLocations(locations, lineLinks, noteId, "WorkflowLine", l => l.WorkflowLineID);
            AddLocations(locations, lineStepLinks, noteId, "WorkflowLineStep", l => l.WorkflowLineStepID);
            AddLocations(locations, noteFileRefLinks, noteId, "FileRef", l => l.FileRefID);
            AddLocations(locations, fileRefNoteLinks, noteId, "FileRef", l => l.FileRefID);
            AddRelationLocations(locations, relations, noteId);
            AddLocations(locations, tagLinks, noteId, "Tag", l => l.TagID);

            return new VaultNoteUsageDto
            {
                NoteID = noteId,
                Title = note?.Title ?? string.Empty,
                Exists = note != null,
                CanSafelyDelete = locations.Count == 0,
                UsageCount = locations.Count,
                VaultHeaderUsageCount = headerLinks.Count,
                ProjectUsageCount = projectLinks.Count,
                SessionUsageCount = sessionLinks.Count,
                WorkflowUsageCount = workflowLinks.Count,
                WorkflowNodeUsageCount = nodeLinks.Count,
                WorkflowLineUsageCount = lineLinks.Count,
                WorkflowLineStepUsageCount = lineStepLinks.Count,
                FileRefUsageCount = noteFileRefLinks.Count + fileRefNoteLinks.Count,
                NoteRelationUsageCount = relations.Count,
                TagUsageCount = tagLinks.Count,
                Locations = locations
            };
        }

        public async Task<bool> CanSafelyDeleteNoteAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var usage = await GetNoteUsageAsync(noteId, ct);
            return usage.CanSafelyDelete;
        }

        public async Task<VaultNoteUsageDto> GetNoteUsageLocationsAsync(string noteId, CancellationToken ct = default)
        {
            return await GetNoteUsageAsync(noteId, ct);
        }

        private static void AddLocations<T>(
            List<VaultNoteUsageLocationDto> locations,
            List<T> links,
            string noteId,
            string parentType,
            Func<T, string> parentIdSelector) where T : class
        {
            foreach (var link in links)
            {
                var sortOrder = GetSortOrder(link);
                locations.Add(new VaultNoteUsageLocationDto
                {
                    LinkID = GetLinkId(link),
                    NoteID = noteId,
                    ParentID = parentIdSelector(link),
                    ParentType = parentType,
                    UsageRole = GetUsageRole(link),
                    Instructions = GetInstructions(link),
                    IsRequired = GetIsRequired(link),
                    SortOrder = sortOrder,
                    CreatedAt = GetCreatedAt(link),
                    UpdatedAt = GetUpdatedAt(link)
                });
            }
        }

        private static void AddRelationLocations(
            List<VaultNoteUsageLocationDto> locations,
            List<VaultNoteRelation> relations,
            string noteId)
        {
            foreach (var rel in relations)
            {
                var otherId = rel.SourceNoteID == noteId ? rel.TargetNoteID : rel.SourceNoteID;
                locations.Add(new VaultNoteUsageLocationDto
                {
                    LinkID = rel.ID,
                    NoteID = noteId,
                    ParentID = otherId,
                    ParentType = "NoteRelation",
                    UsageRole = rel.RelationType,
                    Instructions = rel.Description,
                    IsRequired = false,
                    SortOrder = rel.SortOrder,
                    CreatedAt = rel.CreatedAt,
                    UpdatedAt = rel.UpdatedAt
                });
            }
        }

        private static string GetLinkId<T>(T link) => link switch
        {
            VaultHeaderNote x => x.ID,
            VaultProjectNote x => x.ID,
            VaultSessionNote x => x.ID,
            VaultWorkflowNote x => x.ID,
            VaultWorkflowNodeNote x => x.ID,
            VaultWorkflowLineNote x => x.ID,
            VaultWorkflowLineStepNote x => x.ID,
            VaultNoteFileRef x => x.ID,
            VaultFileRefNote x => x.ID,
            VaultNoteVaultTag x => x.ID,
            _ => string.Empty
        };

        private static string GetUsageRole<T>(T link) => link switch
        {
            VaultHeaderNote x => x.UsageRole,
            VaultProjectNote x => x.UsageRole,
            VaultSessionNote x => x.UsageRole,
            VaultWorkflowNote x => x.UsageRole,
            VaultWorkflowNodeNote x => x.UsageRole,
            VaultWorkflowLineNote x => x.UsageRole,
            VaultWorkflowLineStepNote x => x.UsageRole,
            VaultNoteFileRef x => x.UsageRole,
            VaultFileRefNote x => x.UsageRole,
            VaultNoteVaultTag x => string.Empty,
            _ => string.Empty
        };

        private static string? GetInstructions<T>(T link) => link switch
        {
            VaultHeaderNote x => x.Instructions,
            VaultProjectNote x => x.Instructions,
            VaultSessionNote x => x.Instructions,
            VaultWorkflowNote x => x.Instructions,
            VaultWorkflowNodeNote x => x.Instructions,
            VaultWorkflowLineNote x => x.Instructions,
            VaultWorkflowLineStepNote x => x.Instructions,
            VaultNoteFileRef x => x.Instructions,
            VaultFileRefNote x => x.Instructions,
            VaultNoteVaultTag x => null,
            _ => null
        };

        private static bool GetIsRequired<T>(T link) => link switch
        {
            VaultHeaderNote x => x.IsRequired,
            VaultProjectNote x => x.IsRequired,
            VaultSessionNote x => x.IsRequired,
            VaultWorkflowNote x => x.IsRequired,
            VaultWorkflowNodeNote x => x.IsRequired,
            VaultWorkflowLineNote x => x.IsRequired,
            VaultWorkflowLineStepNote x => x.IsRequired,
            VaultNoteFileRef x => x.IsRequired,
            VaultFileRefNote x => x.IsRequired,
            VaultNoteVaultTag x => false,
            _ => false
        };

        private static int GetSortOrder<T>(T link) => link switch
        {
            VaultHeaderNote x => x.SortOrder,
            VaultProjectNote x => x.SortOrder,
            VaultSessionNote x => x.SortOrder,
            VaultWorkflowNote x => x.SortOrder,
            VaultWorkflowNodeNote x => x.NoteOrder,
            VaultWorkflowLineNote x => x.SortOrder,
            VaultWorkflowLineStepNote x => x.SortOrder,
            VaultNoteFileRef x => x.SortOrder,
            VaultFileRefNote x => x.NoteOrder,
            VaultNoteVaultTag x => x.SortOrder,
            _ => 0
        };

        private static DateTime GetCreatedAt<T>(T link) => link switch
        {
            VaultHeaderNote x => x.CreatedAt,
            VaultProjectNote x => x.CreatedAt,
            VaultSessionNote x => x.CreatedAt,
            VaultWorkflowNote x => x.CreatedAt,
            VaultWorkflowNodeNote x => x.CreatedAt,
            VaultWorkflowLineNote x => x.CreatedAt,
            VaultWorkflowLineStepNote x => x.CreatedAt,
            VaultNoteFileRef x => x.CreatedAt,
            VaultFileRefNote x => x.CreatedAt,
            VaultNoteVaultTag x => x.CreatedAt,
            _ => DateTime.MinValue
        };

        private static DateTime GetUpdatedAt<T>(T link) => link switch
        {
            VaultHeaderNote x => x.UpdatedAt,
            VaultProjectNote x => x.UpdatedAt,
            VaultSessionNote x => x.UpdatedAt,
            VaultWorkflowNote x => x.UpdatedAt,
            VaultWorkflowNodeNote x => x.UpdatedAt,
            VaultWorkflowLineNote x => x.UpdatedAt,
            VaultWorkflowLineStepNote x => x.UpdatedAt,
            VaultNoteFileRef x => x.UpdatedAt,
            VaultFileRefNote x => x.UpdatedAt,
            VaultNoteVaultTag x => x.UpdatedAt,
            _ => DateTime.MinValue
        };
    }
}
