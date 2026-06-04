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
    public sealed class VaultWorkflowNodeFileRefsReadService : IVaultWorkflowNodeFileRefsReadService
    {
        private readonly IVaultWorkflowNodeFileRefQueryService _nodeFileRefQuery;
        private readonly IVaultFileRefQueryService _fileRefQuery;
        private readonly VaultLogger _logger;

        public VaultWorkflowNodeFileRefsReadService(
            IVaultWorkflowNodeFileRefQueryService nodeFileRefQuery,
            IVaultFileRefQueryService fileRefQuery,
            VaultLogger logger)
        {
            _nodeFileRefQuery = nodeFileRefQuery ?? throw new ArgumentNullException(nameof(nodeFileRefQuery));
            _fileRefQuery = fileRefQuery ?? throw new ArgumentNullException(nameof(fileRefQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultAttachedFilesResponse> GetFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var links = await _nodeFileRefQuery.GetByWorkflowNodeIdAsync(workflowNodeId, ct);
            return await BuildResponseAsync(links, workflowNodeId, "WorkflowNode", null, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetRequiredFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var links = await _nodeFileRefQuery.GetByWorkflowNodeIdAsync(workflowNodeId, ct);
            return await BuildResponseAsync(links.Where(l => l.IsRequired), workflowNodeId, "WorkflowNode", null, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetOptionalFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            var links = await _nodeFileRefQuery.GetByWorkflowNodeIdAsync(workflowNodeId, ct);
            return await BuildResponseAsync(links.Where(l => !l.IsRequired), workflowNodeId, "WorkflowNode", null, ct);
        }

        public async Task<VaultAttachedFileDto?> GetFileForWorkflowNodeAsync(string workflowNodeId, string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            var links = await _nodeFileRefQuery.GetByWorkflowNodeIdAsync(workflowNodeId, ct);
            var link = links.FirstOrDefault(l => l.FileRefID == fileRefId);
            if (link == null)
                return null;

            var fileRef = await _fileRefQuery.GetByIdAsync(fileRefId, ct);
            if (fileRef == null)
                return null;

            return MapToDto(link, fileRef, workflowNodeId, "WorkflowNode");
        }

        public async Task<int> CountFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowNodeId))
                throw new ArgumentException("WorkflowNode ID is required.", nameof(workflowNodeId));

            return await _nodeFileRefQuery.CountByWorkflowNodeIdAsync(workflowNodeId, ct);
        }

        private async Task<VaultAttachedFilesResponse> BuildResponseAsync(
            IEnumerable<VaultWorkflowNodeFileRef> links,
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
            VaultWorkflowNodeFileRef link,
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
