using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Files;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultFileUsageReadService : IVaultFileUsageReadService
    {
        private readonly IVaultHeaderFileRefQueryService _headerFileRefQuery;
        private readonly IVaultProjectFileRefQueryService _projectFileRefQuery;
        private readonly IVaultSessionFileRefQueryService _sessionFileRefQuery;
        private readonly IVaultNoteFileRefQueryService _noteFileRefQuery;
        private readonly IVaultFileRefNoteQueryService _fileRefNoteQuery;
        private readonly IVaultWorkflowFileRefQueryService _workflowFileRefQuery;
        private readonly IVaultWorkflowNodeFileRefQueryService _nodeFileRefQuery;
        private readonly IVaultWorkflowLineFileRefQueryService _lineFileRefQuery;
        private readonly IVaultWorkflowLineStepFileRefQueryService _lineStepFileRefQuery;
        private readonly IVaultFileRefRelationQueryService _fileRefRelationQuery;
        private readonly IVaultFileRefQueryService _fileRefQuery;
        private readonly VaultLogger _logger;

        public VaultFileUsageReadService(
            IVaultHeaderFileRefQueryService headerFileRefQuery,
            IVaultProjectFileRefQueryService projectFileRefQuery,
            IVaultSessionFileRefQueryService sessionFileRefQuery,
            IVaultNoteFileRefQueryService noteFileRefQuery,
            IVaultFileRefNoteQueryService fileRefNoteQuery,
            IVaultWorkflowFileRefQueryService workflowFileRefQuery,
            IVaultWorkflowNodeFileRefQueryService nodeFileRefQuery,
            IVaultWorkflowLineFileRefQueryService lineFileRefQuery,
            IVaultWorkflowLineStepFileRefQueryService lineStepFileRefQuery,
            IVaultFileRefRelationQueryService fileRefRelationQuery,
            IVaultFileRefQueryService fileRefQuery,
            VaultLogger logger)
        {
            _headerFileRefQuery = headerFileRefQuery ?? throw new ArgumentNullException(nameof(headerFileRefQuery));
            _projectFileRefQuery = projectFileRefQuery ?? throw new ArgumentNullException(nameof(projectFileRefQuery));
            _sessionFileRefQuery = sessionFileRefQuery ?? throw new ArgumentNullException(nameof(sessionFileRefQuery));
            _noteFileRefQuery = noteFileRefQuery ?? throw new ArgumentNullException(nameof(noteFileRefQuery));
            _fileRefNoteQuery = fileRefNoteQuery ?? throw new ArgumentNullException(nameof(fileRefNoteQuery));
            _workflowFileRefQuery = workflowFileRefQuery ?? throw new ArgumentNullException(nameof(workflowFileRefQuery));
            _nodeFileRefQuery = nodeFileRefQuery ?? throw new ArgumentNullException(nameof(nodeFileRefQuery));
            _lineFileRefQuery = lineFileRefQuery ?? throw new ArgumentNullException(nameof(lineFileRefQuery));
            _lineStepFileRefQuery = lineStepFileRefQuery ?? throw new ArgumentNullException(nameof(lineStepFileRefQuery));
            _fileRefRelationQuery = fileRefRelationQuery ?? throw new ArgumentNullException(nameof(fileRefRelationQuery));
            _fileRefQuery = fileRefQuery ?? throw new ArgumentNullException(nameof(fileRefQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultFileUsageDto> GetFileUsageAsync(string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            var fileRef = await _fileRefQuery.GetByIdAsync(fileRefId, ct);

            var headerLinks = await _headerFileRefQuery.GetByFileRefIdAsync(fileRefId, ct);
            var projectLinks = await _projectFileRefQuery.GetByFileRefIdAsync(fileRefId, ct);
            var sessionLinks = await _sessionFileRefQuery.GetByFileRefIdAsync(fileRefId, ct);
            var noteLinks = await _noteFileRefQuery.GetByFileRefIdAsync(fileRefId, ct);
            var fileRefNoteLinks = await _fileRefNoteQuery.GetByFileRefIdAsync(fileRefId, ct);
            var workflowLinks = await _workflowFileRefQuery.GetByFileRefIdAsync(fileRefId, ct);
            var nodeLinks = await _nodeFileRefQuery.GetByFileRefIdAsync(fileRefId, ct);
            var lineLinks = await _lineFileRefQuery.GetByFileRefIdAsync(fileRefId, ct);
            var lineStepLinks = await _lineStepFileRefQuery.GetByFileRefIdAsync(fileRefId, ct);
            var relations = await _fileRefRelationQuery.GetAllByFileRefIdAsync(fileRefId, ct);

            var locations = new List<VaultFileUsageLocationDto>();

            AddLocations(locations, headerLinks, fileRefId, "Vault", l => l.VaultID);
            AddLocations(locations, projectLinks, fileRefId, "Project", l => l.ProjectID);
            AddLocations(locations, sessionLinks, fileRefId, "Session", l => l.SessionID);
            AddLocations(locations, noteLinks, fileRefId, "Note", l => l.NoteID);
            AddLocations(locations, fileRefNoteLinks, fileRefId, "FileRefNote", l => l.NoteID);
            AddLocations(locations, workflowLinks, fileRefId, "Workflow", l => l.WorkflowID);
            AddLocations(locations, nodeLinks, fileRefId, "WorkflowNode", l => l.WorkflowNodeID);
            AddLocations(locations, lineLinks, fileRefId, "WorkflowLine", l => l.WorkflowLineID);
            AddLocations(locations, lineStepLinks, fileRefId, "WorkflowLineStep", l => l.WorkflowLineStepID);
            AddRelationLocations(locations, relations, fileRefId);

            return new VaultFileUsageDto
            {
                FileRefID = fileRefId,
                Name = fileRef?.Name ?? string.Empty,
                Path = fileRef?.Path ?? string.Empty,
                Exists = fileRef != null,
                CanSafelyDelete = locations.Count == 0,
                UsageCount = locations.Count,
                VaultHeaderUsageCount = headerLinks.Count,
                ProjectUsageCount = projectLinks.Count,
                SessionUsageCount = sessionLinks.Count,
                NoteUsageCount = noteLinks.Count,
                FileRefNoteUsageCount = fileRefNoteLinks.Count,
                WorkflowUsageCount = workflowLinks.Count,
                WorkflowNodeUsageCount = nodeLinks.Count,
                WorkflowLineUsageCount = lineLinks.Count,
                WorkflowLineStepUsageCount = lineStepLinks.Count,
                FileRelationUsageCount = relations.Count,
                Locations = locations
            };
        }

        public async Task<bool> CanSafelyDeleteFileAsync(string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            var usage = await GetFileUsageAsync(fileRefId, ct);
            return usage.CanSafelyDelete;
        }

        public async Task<VaultFileUsageDto> GetFileUsageLocationsAsync(string fileRefId, CancellationToken ct = default)
        {
            return await GetFileUsageAsync(fileRefId, ct);
        }

        private static void AddLocations<T>(
            List<VaultFileUsageLocationDto> locations,
            List<T> links,
            string fileRefId,
            string parentType,
            Func<T, string> parentIdSelector) where T : class
        {
            foreach (var link in links)
            {
                var sortOrder = GetSortOrder(link);
                locations.Add(new VaultFileUsageLocationDto
                {
                    LinkID = GetLinkId(link),
                    FileRefID = fileRefId,
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
            List<VaultFileUsageLocationDto> locations,
            List<VaultFileRefRelation> relations,
            string fileRefId)
        {
            foreach (var rel in relations)
            {
                var otherId = rel.SourceFileRefID == fileRefId ? rel.TargetFileRefID : rel.SourceFileRefID;
                locations.Add(new VaultFileUsageLocationDto
                {
                    LinkID = rel.ID,
                    FileRefID = fileRefId,
                    ParentID = otherId,
                    ParentType = "FileRelation",
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
            VaultHeaderFileRef x => x.ID,
            VaultProjectFileRef x => x.ID,
            VaultSessionFileRef x => x.ID,
            VaultNoteFileRef x => x.ID,
            VaultFileRefNote x => x.ID,
            VaultWorkflowFileRef x => x.ID,
            VaultWorkflowNodeFileRef x => x.ID,
            VaultWorkflowLineFileRef x => x.ID,
            VaultWorkflowLineStepFileRef x => x.ID,
            _ => string.Empty
        };

        private static string GetUsageRole<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.UsageRole,
            VaultProjectFileRef x => x.UsageRole,
            VaultSessionFileRef x => x.UsageRole,
            VaultNoteFileRef x => x.UsageRole,
            VaultFileRefNote x => x.UsageRole,
            VaultWorkflowFileRef x => x.UsageRole,
            VaultWorkflowNodeFileRef x => x.UsageRole,
            VaultWorkflowLineFileRef x => x.UsageRole,
            VaultWorkflowLineStepFileRef x => x.UsageRole,
            _ => string.Empty
        };

        private static string? GetInstructions<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.Instructions,
            VaultProjectFileRef x => x.Instructions,
            VaultSessionFileRef x => x.Instructions,
            VaultNoteFileRef x => x.Instructions,
            VaultFileRefNote x => x.Instructions,
            VaultWorkflowFileRef x => x.Instructions,
            VaultWorkflowNodeFileRef x => x.Instructions,
            VaultWorkflowLineFileRef x => x.Instructions,
            VaultWorkflowLineStepFileRef x => x.Instructions,
            _ => null
        };

        private static bool GetIsRequired<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.IsRequired,
            VaultProjectFileRef x => x.IsRequired,
            VaultSessionFileRef x => x.IsRequired,
            VaultNoteFileRef x => x.IsRequired,
            VaultFileRefNote x => x.IsRequired,
            VaultWorkflowFileRef x => x.IsRequired,
            VaultWorkflowNodeFileRef x => x.IsRequired,
            VaultWorkflowLineFileRef x => x.IsRequired,
            VaultWorkflowLineStepFileRef x => x.IsRequired,
            _ => false
        };

        private static int GetSortOrder<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.SortOrder,
            VaultProjectFileRef x => x.SortOrder,
            VaultSessionFileRef x => x.SortOrder,
            VaultNoteFileRef x => x.SortOrder,
            VaultFileRefNote x => x.NoteOrder,
            VaultWorkflowFileRef x => x.SortOrder,
            VaultWorkflowNodeFileRef x => x.SortOrder,
            VaultWorkflowLineFileRef x => x.FileOrder,
            VaultWorkflowLineStepFileRef x => x.SortOrder,
            _ => 0
        };

        private static DateTime GetCreatedAt<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.CreatedAt,
            VaultProjectFileRef x => x.CreatedAt,
            VaultSessionFileRef x => x.CreatedAt,
            VaultNoteFileRef x => x.CreatedAt,
            VaultFileRefNote x => x.CreatedAt,
            VaultWorkflowFileRef x => x.CreatedAt,
            VaultWorkflowNodeFileRef x => x.CreatedAt,
            VaultWorkflowLineFileRef x => x.CreatedAt,
            VaultWorkflowLineStepFileRef x => x.CreatedAt,
            _ => DateTime.MinValue
        };

        private static DateTime GetUpdatedAt<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.UpdatedAt,
            VaultProjectFileRef x => x.UpdatedAt,
            VaultSessionFileRef x => x.UpdatedAt,
            VaultNoteFileRef x => x.UpdatedAt,
            VaultFileRefNote x => x.UpdatedAt,
            VaultWorkflowFileRef x => x.UpdatedAt,
            VaultWorkflowNodeFileRef x => x.UpdatedAt,
            VaultWorkflowLineFileRef x => x.UpdatedAt,
            VaultWorkflowLineStepFileRef x => x.UpdatedAt,
            _ => DateTime.MinValue
        };
    }
}
