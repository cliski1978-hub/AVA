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
    public sealed class VaultWorkflowLineFileRefsReadService : IVaultWorkflowLineFileRefsReadService
    {
        private readonly IVaultWorkflowLineFileRefQueryService _lineFileRefQuery;
        private readonly IVaultFileRefQueryService _fileRefQuery;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineFileRefsReadService(
            IVaultWorkflowLineFileRefQueryService lineFileRefQuery,
            IVaultFileRefQueryService fileRefQuery,
            VaultLogger logger)
        {
            _lineFileRefQuery = lineFileRefQuery ?? throw new ArgumentNullException(nameof(lineFileRefQuery));
            _fileRefQuery = fileRefQuery ?? throw new ArgumentNullException(nameof(fileRefQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultAttachedFilesResponse> GetFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            var links = await _lineFileRefQuery.GetByWorkflowLineIdAsync(workflowLineId, ct);
            return await BuildResponseAsync(links, workflowLineId, "WorkflowLine", null, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetRequiredFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            var links = await _lineFileRefQuery.GetByWorkflowLineIdAsync(workflowLineId, ct);
            return await BuildResponseAsync(links.Where(l => l.IsRequired), workflowLineId, "WorkflowLine", null, ct);
        }

        public async Task<VaultAttachedFilesResponse> GetOptionalFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            var links = await _lineFileRefQuery.GetByWorkflowLineIdAsync(workflowLineId, ct);
            return await BuildResponseAsync(links.Where(l => !l.IsRequired), workflowLineId, "WorkflowLine", null, ct);
        }

        public async Task<VaultAttachedFileDto?> GetFileForWorkflowLineAsync(string workflowLineId, string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            var links = await _lineFileRefQuery.GetByWorkflowLineIdAsync(workflowLineId, ct);
            var link = links.FirstOrDefault(l => l.FileRefID == fileRefId);
            if (link == null)
                return null;

            var fileRef = await _fileRefQuery.GetByIdAsync(fileRefId, ct);
            if (fileRef == null)
                return null;

            return MapToDto(link, fileRef, workflowLineId, "WorkflowLine");
        }

        public async Task<int> CountFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(workflowLineId))
                throw new ArgumentException("WorkflowLine ID is required.", nameof(workflowLineId));

            return await _lineFileRefQuery.CountByWorkflowLineIdAsync(workflowLineId, ct);
        }

        private async Task<VaultAttachedFilesResponse> BuildResponseAsync(
            IEnumerable<VaultWorkflowLineFileRef> links,
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

            foreach (var link in linkList.OrderBy(l => l.FileOrder))
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
            VaultWorkflowLineFileRef link,
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
                SortOrder = link.FileOrder,
                CreatedAt = fileRef.CreatedAt,
                UpdatedAt = link.UpdatedAt
            };
        }
    }
}
