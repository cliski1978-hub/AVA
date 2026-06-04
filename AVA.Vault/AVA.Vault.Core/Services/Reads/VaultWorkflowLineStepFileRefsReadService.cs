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
    public sealed class VaultWorkflowLineStepFileRefsReadService : IVaultWorkflowLineStepFileRefsReadService
    {
        private readonly IVaultWorkflowLineStepFileRefQueryService _lineStepFileRefQuery;
        private readonly IVaultFileRefQueryService _fileRefQuery;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineStepFileRefsReadService(
            IVaultWorkflowLineStepFileRefQueryService lineStepFileRefQuery,
            IVaultFileRefQueryService fileRefQuery,
            VaultLogger logger)
        {
            _lineStepFileRefQuery = lineStepFileRefQuery ?? throw new ArgumentNullException(nameof(lineStepFileRefQuery));
            _fileRefQuery = fileRefQuery ?? throw new ArgumentNullException(nameof(fileRefQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultAttachedFilesResponse> GetFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));

            var links = await _lineStepFileRefQuery.GetByWorkflowLineStepIdAsync(workflowLineStepId, ct);
            return await BuildResponseAsync(links, workflowLineStepId, "WorkflowLineStep", null, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetRequiredFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));

            var links = await _lineStepFileRefQuery.GetByWorkflowLineStepIdAsync(workflowLineStepId, ct);
            return await BuildResponseAsync(links.Where(l => l.IsRequired), workflowLineStepId, "WorkflowLineStep", null, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetOptionalFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));

            var links = await _lineStepFileRefQuery.GetByWorkflowLineStepIdAsync(workflowLineStepId, ct);
            return await BuildResponseAsync(links.Where(l => !l.IsRequired), workflowLineStepId, "WorkflowLineStep", null, ct);
        }

        public async Task<VaultAttachedFileDto?> GetFileForWorkflowLineStepAsync(string workflowLineStepId, string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            var links = await _lineStepFileRefQuery.GetByWorkflowLineStepIdAsync(workflowLineStepId, ct);
            var link = links.FirstOrDefault(l => l.FileRefID == fileRefId);
            if (link == null)
                return null;

            var fileRef = await _fileRefQuery.GetByIdAsync(fileRefId, ct);
            if (fileRef == null)
                return null;

            return MapToDto(link, fileRef, workflowLineStepId, "WorkflowLineStep");
        }

        public async Task<int> CountFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineStepId))
                throw new ArgumentException("WorkflowLineStep ID is required.", nameof(workflowLineStepId));

            return await _lineStepFileRefQuery.CountByWorkflowLineStepIdAsync(workflowLineStepId, ct);
        }

        private async Task<VaultAttachedFilesResponse> BuildResponseAsync(
            IEnumerable<VaultWorkflowLineStepFileRef> links,
            string parentId,
            string parentType,
            bool? isRequiredFilter,
            CancellationToken ct)
        {
            var linkList = links.ToList();
            var fileRefIds = linkList.Select(l => l.FileRefID).Distinct().ToList();
            var fileRefs = fileRefIds.Count > 0
                ? (await _fileRefQuery.GetByIdsAsync(fileRefIds, ct)).ToDictionary(f => f.ID)
                : new Dictionary<string, VaultFileRef>();

            var dtos = new List<VaultAttachedFileDto>();
            int requiredCount = 0, optionalCount = 0;

            foreach (var link in linkList.OrderBy(l => l.SortOrder))
            {
                if (!fileRefs.TryGetValue(link.FileRefID, out var fileRef))
                    continue;

                if (isRequiredFilter.HasValue && link.IsRequired != isRequiredFilter.Value)
                    continue;

                dtos.Add(MapToDto(link, fileRef, parentId, parentType));

                if (link.IsRequired)
                    requiredCount++;
                else
                    optionalCount++;
            }

            return new VaultAttachedFilesResponse
            {
                ParentID = parentId,
                ParentType = parentType,
                TotalCount = dtos.Count,
                RequiredCount = requiredCount,
                OptionalCount = optionalCount,
                Files = dtos
            };
        }

        private static VaultAttachedFileDto MapToDto(
            VaultWorkflowLineStepFileRef link,
            VaultFileRef fileRef,
            string parentId,
            string parentType)
        {
            return new VaultAttachedFileDto
            {
                LinkID = link.ID,
                FileRefID = fileRef.ID,
                ParentID = parentId,
                ParentType = parentType,
                Name = fileRef.Name,
                Path = fileRef.Path,
                MimeType = fileRef.MimeType,
                FileSizeBytes = fileRef.FileSizeBytes,
                ContentHash = fileRef.ContentHash,
                UsageRole = link.UsageRole,
                Instructions = link.Instructions,
                IsRequired = link.IsRequired,
                SortOrder = link.SortOrder,
                CreatedAt = fileRef.CreatedAt,
                UpdatedAt = link.UpdatedAt
            };
        }
    }
}
