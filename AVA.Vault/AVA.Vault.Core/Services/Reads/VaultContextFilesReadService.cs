using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Files;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultContextFilesReadService : IVaultContextFilesReadService
    {
        private readonly IVaultHeaderFileRefQueryService _headerFileRefQuery;
        private readonly IVaultProjectFileRefQueryService _projectFileRefQuery;
        private readonly IVaultSessionFileRefQueryService _sessionFileRefQuery;
        private readonly IVaultNoteFileRefQueryService _noteFileRefQuery;
        private readonly IVaultFileRefQueryService _fileRefQuery;
        private readonly IVaultWorkflowFileRefsReadService _workflowFileRefsRead;
        private readonly IVaultWorkflowNodeFileRefsReadService _nodeFileRefsRead;
        private readonly IVaultWorkflowLineFileRefsReadService _lineFileRefsRead;
        private readonly IVaultWorkflowLineStepFileRefsReadService _lineStepFileRefsRead;
        private readonly VaultLogger _logger;

        public VaultContextFilesReadService(
            IVaultHeaderFileRefQueryService headerFileRefQuery,
            IVaultProjectFileRefQueryService projectFileRefQuery,
            IVaultSessionFileRefQueryService sessionFileRefQuery,
            IVaultNoteFileRefQueryService noteFileRefQuery,
            IVaultFileRefQueryService fileRefQuery,
            IVaultWorkflowFileRefsReadService workflowFileRefsRead,
            IVaultWorkflowNodeFileRefsReadService nodeFileRefsRead,
            IVaultWorkflowLineFileRefsReadService lineFileRefsRead,
            IVaultWorkflowLineStepFileRefsReadService lineStepFileRefsRead,
            VaultLogger logger)
        {
            _headerFileRefQuery = headerFileRefQuery ?? throw new ArgumentNullException(nameof(headerFileRefQuery));
            _projectFileRefQuery = projectFileRefQuery ?? throw new ArgumentNullException(nameof(projectFileRefQuery));
            _sessionFileRefQuery = sessionFileRefQuery ?? throw new ArgumentNullException(nameof(sessionFileRefQuery));
            _noteFileRefQuery = noteFileRefQuery ?? throw new ArgumentNullException(nameof(noteFileRefQuery));
            _fileRefQuery = fileRefQuery ?? throw new ArgumentNullException(nameof(fileRefQuery));
            _workflowFileRefsRead = workflowFileRefsRead ?? throw new ArgumentNullException(nameof(workflowFileRefsRead));
            _nodeFileRefsRead = nodeFileRefsRead ?? throw new ArgumentNullException(nameof(nodeFileRefsRead));
            _lineFileRefsRead = lineFileRefsRead ?? throw new ArgumentNullException(nameof(lineFileRefsRead));
            _lineStepFileRefsRead = lineStepFileRefsRead ?? throw new ArgumentNullException(nameof(lineStepFileRefsRead));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultContextFilesDto> GetFilesForVaultAsync(string vaultId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(vaultId))
                throw new ArgumentException("Vault ID is required.", nameof(vaultId));

            var links = await _headerFileRefQuery.GetByVaultIdAsync(vaultId, ct);
            return await BuildContextResponseAsync(links, vaultId, "Vault", ct);
        }

        public async Task<VaultContextFilesDto> GetFilesForProjectAsync(string projectId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("Project ID is required.", nameof(projectId));

            var links = await _projectFileRefQuery.GetByProjectIdAsync(projectId, ct);
            return await BuildContextResponseAsync(links, projectId, "Project", ct);
        }

        public async Task<VaultContextFilesDto> GetFilesForSessionAsync(string sessionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID is required.", nameof(sessionId));

            var links = await _sessionFileRefQuery.GetBySessionIdAsync(sessionId, ct);
            return await BuildContextResponseAsync(links, sessionId, "Session", ct);
        }

        public async Task<VaultContextFilesDto> GetFilesForNoteAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var links = await _noteFileRefQuery.GetByNoteIdAsync(noteId, ct);
            return await BuildContextResponseAsync(links, noteId, "Note", ct);
        }

        public async Task<VaultContextFilesDto> GetFilesForWorkflowAsync(string workflowId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

            var filesResponse = await _workflowFileRefsRead.GetFilesForWorkflowAsync(workflowId, ct);
            return BuildFromResponse(filesResponse, workflowId, "Workflow");
        }

        public async Task<VaultContextFilesDto> GetFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var filesResponse = await _nodeFileRefsRead.GetFilesForWorkflowNodeAsync(workflowNodeId, ct);
            return BuildFromResponse(filesResponse, workflowNodeId, "WorkflowNode");
        }

        public async Task<VaultContextFilesDto> GetFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            var filesResponse = await _lineFileRefsRead.GetFilesForWorkflowLineAsync(workflowLineId, ct);
            return BuildFromResponse(filesResponse, workflowLineId, "WorkflowLine");
        }

        public async Task<VaultContextFilesDto> GetFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));

            var filesResponse = await _lineStepFileRefsRead.GetFilesForWorkflowLineStepAsync(workflowLineStepId, ct);
            return BuildFromResponse(filesResponse, workflowLineStepId, "WorkflowLineStep");
        }

        private async Task<VaultContextFilesDto> BuildContextResponseAsync<T>(
            List<T> links,
            string contextId,
            string contextType,
            CancellationToken ct) where T : class
        {
            var fileRefIds = links.Select(l => GetFileRefId(l)).Distinct().ToList();
            var fileRefs = fileRefIds.Count > 0
                ? (await _fileRefQuery.GetByIdsAsync(fileRefIds, ct)).ToDictionary(f => f.ID)
                : new Dictionary<string, VaultFileRef>();

            var dtos = new List<VaultAttachedFileDto>();
            int requiredCount = 0, optionalCount = 0;

            foreach (var link in links.OrderBy(l => GetSortOrder(l)))
            {
                var fileRefId = GetFileRefId(link);
                if (!fileRefs.TryGetValue(fileRefId, out var fileRef))
                    continue;

                dtos.Add(new VaultAttachedFileDto
                {
                    LinkID = GetLinkId(link),
                    FileRefID = fileRef.ID,
                    ParentID = contextId,
                    ParentType = contextType,
                    Name = fileRef.Name,
                    Path = fileRef.Path,
                    MimeType = fileRef.MimeType,
                    FileSizeBytes = fileRef.FileSizeBytes,
                    ContentHash = fileRef.ContentHash,
                    UsageRole = GetUsageRole(link),
                    Instructions = GetInstructions(link),
                    IsRequired = GetIsRequired(link),
                    SortOrder = GetSortOrder(link),
                    CreatedAt = fileRef.CreatedAt,
                    UpdatedAt = GetUpdatedAt(link)
                });

                if (GetIsRequired(link))
                    requiredCount++;
                else
                    optionalCount++;
            }

            return new VaultContextFilesDto
            {
                ContextID = contextId,
                ContextType = contextType,
                FileCount = dtos.Count,
                RequiredFileCount = requiredCount,
                OptionalFileCount = optionalCount,
                Files = new VaultAttachedFilesResponse
                {
                    ParentID = contextId,
                    ParentType = contextType,
                    TotalCount = dtos.Count,
                    RequiredCount = requiredCount,
                    OptionalCount = optionalCount,
                    Files = dtos
                }
            };
        }

        private static VaultContextFilesDto BuildFromResponse(
            VaultAttachedFilesResponse response,
            string contextId,
            string contextType)
        {
            return new VaultContextFilesDto
            {
                ContextID = contextId,
                ContextType = contextType,
                FileCount = response.TotalCount,
                RequiredFileCount = response.RequiredCount,
                OptionalFileCount = response.OptionalCount,
                Files = response
            };
        }

        private static string GetFileRefId<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.FileRefID,
            VaultProjectFileRef x => x.FileRefID,
            VaultSessionFileRef x => x.FileRefID,
            VaultNoteFileRef x => x.FileRefID,
            _ => string.Empty
        };

        private static string GetLinkId<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.ID,
            VaultProjectFileRef x => x.ID,
            VaultSessionFileRef x => x.ID,
            VaultNoteFileRef x => x.ID,
            _ => string.Empty
        };

        private static string GetUsageRole<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.UsageRole,
            VaultProjectFileRef x => x.UsageRole,
            VaultSessionFileRef x => x.UsageRole,
            VaultNoteFileRef x => x.UsageRole,
            _ => string.Empty
        };

        private static string? GetInstructions<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.Instructions,
            VaultProjectFileRef x => x.Instructions,
            VaultSessionFileRef x => x.Instructions,
            VaultNoteFileRef x => x.Instructions,
            _ => null
        };

        private static bool GetIsRequired<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.IsRequired,
            VaultProjectFileRef x => x.IsRequired,
            VaultSessionFileRef x => x.IsRequired,
            VaultNoteFileRef x => x.IsRequired,
            _ => false
        };

        private static int GetSortOrder<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.SortOrder,
            VaultProjectFileRef x => x.SortOrder,
            VaultSessionFileRef x => x.SortOrder,
            VaultNoteFileRef x => x.SortOrder,
            _ => 0
        };

        private static DateTime GetUpdatedAt<T>(T link) => link switch
        {
            VaultHeaderFileRef x => x.UpdatedAt,
            VaultProjectFileRef x => x.UpdatedAt,
            VaultSessionFileRef x => x.UpdatedAt,
            VaultNoteFileRef x => x.UpdatedAt,
            _ => DateTime.MinValue
        };
    }
}
