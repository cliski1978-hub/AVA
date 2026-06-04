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
    public sealed class VaultWorkflowFileRefsReadService : IVaultWorkflowFileRefsReadService
    {
        private readonly IVaultWorkflowFileRefQueryService _workflowFileRefQuery;
        private readonly IVaultFileRefQueryService _fileRefQuery;
        private readonly VaultLogger _logger;

        public VaultWorkflowFileRefsReadService(
            IVaultWorkflowFileRefQueryService workflowFileRefQuery,
            IVaultFileRefQueryService fileRefQuery,
            VaultLogger logger)
        {
            _workflowFileRefQuery = workflowFileRefQuery ?? throw new ArgumentNullException(nameof(workflowFileRefQuery));
            _fileRefQuery = fileRefQuery ?? throw new ArgumentNullException(nameof(fileRefQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultAttachedFilesResponse> GetFilesForWorkflowAsync(string workflowId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

            var links = await _workflowFileRefQuery.GetByWorkflowIdAsync(workflowId, ct);
            return await BuildResponseAsync(links, workflowId, "Workflow", null, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetRequiredFilesForWorkflowAsync(string workflowId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

            var links = await _workflowFileRefQuery.GetByWorkflowIdAsync(workflowId, ct);
            return await BuildResponseAsync(links.Where(l => l.IsRequired), workflowId, "Workflow", null, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetOptionalFilesForWorkflowAsync(string workflowId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

            var links = await _workflowFileRefQuery.GetByWorkflowIdAsync(workflowId, ct);
            return await BuildResponseAsync(links.Where(l => !l.IsRequired), workflowId, "Workflow", null, ct);
        }

        public async Task<VaultAttachedFileDto?> GetFileForWorkflowAsync(string workflowId, string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            var links = await _workflowFileRefQuery.GetByWorkflowIdAsync(workflowId, ct);
            var link = links.FirstOrDefault(l => l.FileRefID == fileRefId);
            if (link == null)
                return null;

            var fileRef = await _fileRefQuery.GetByIdAsync(fileRefId, ct);
            if (fileRef == null)
                return null;

            return MapToDto(link, fileRef, workflowId, "Workflow");
        }

        public async Task<int> CountFilesForWorkflowAsync(string workflowId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

            return await _workflowFileRefQuery.CountByWorkflowIdAsync(workflowId, ct);
        }

        private async Task<VaultAttachedFilesResponse> BuildResponseAsync(
            IEnumerable<VaultWorkflowFileRef> links,
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
            VaultWorkflowFileRef link,
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
