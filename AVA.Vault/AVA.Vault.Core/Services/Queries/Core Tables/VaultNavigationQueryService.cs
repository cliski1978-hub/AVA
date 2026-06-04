using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Models.Query;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Queries
{
    public sealed class VaultNavigationQueryService : IVaultNavigationQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultNavigationQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNavigationResponse> GetNavigationTreeAsync(CancellationToken ct = default)
        {
            var activeVaults = await _db.VaultHeaders
                .AsNoTracking()
                .Where(h => h.IsActive)
                .ToListAsync(ct);

            var vaultGroups = new List<VaultNavGroup>();

            foreach (var vault in activeVaults)
            {
                var projects = await _db.VaultProjects
                    .AsNoTracking()
                    .Where(p => p.VaultID == vault.ID && !p.IsArchived)
                    .ToListAsync(ct);

                var vaultNotes = await _db.VaultNotes
                    .AsNoTracking()
                    .Where(n => n.VaultID == vault.ID && n.SessionID == null)
                    .ToListAsync(ct);

                var vaultSessions = await _db.VaultSessions
                    .AsNoTracking()
                    .Where(s => s.VaultID == vault.ID)
                    .ToListAsync(ct);

                var projectGroups = new List<VaultNavProjectGroup>();

                foreach (var project in projects)
                {
                    var projectNotes = await _db.Set<VaultProjectNote>()
                        .AsNoTracking()
                        .Include(pn => pn.Note)
                        .Where(pn => pn.ProjectID == project.ID)
                        .Select(pn => pn.Note)
                        .ToListAsync(ct);

                    var projectSessions = await _db.VaultSessions
                        .AsNoTracking()
                        .Where(s => s.ProjectID == project.ID)
                        .ToListAsync(ct);

                    var projectWorkflows = await _db.Set<VaultWorkflow>()
                        .AsNoTracking()
                        .Where(w => w.ProjectID == project.ID)
                        .ToListAsync(ct);

                    var projectGroup = new VaultNavProjectGroup
                    {
                        ProjectId = project.ID,
                        ProjectName = project.Name,
                        Notes = projectNotes.Select(n => new VaultNavItem
                        {
                            Id = n.ID,
                            Name = n.Title,
                            Type = "note"
                        }).ToList(),
                        Sessions = projectSessions.Select(s => new VaultNavItem
                        {
                            Id = s.ID,
                            Name = s.Name,
                            Type = "session"
                        }).ToList(),
                        Workflows = projectWorkflows.Select(w => new VaultNavItem
                        {
                            Id = w.ID,
                            Name = w.Name,
                            Type = "workflow"
                        }).ToList()
                    };

                    projectGroups.Add(projectGroup);
                }

                var vaultGroup = new VaultNavGroup
                {
                    VaultId = vault.ID,
                    VaultName = vault.DisplayName,
                    Projects = projectGroups,
                    Notes = vaultNotes.Select(n => new VaultNavItem
                    {
                        Id = n.ID,
                        Name = n.Title,
                        Type = "note"
                    }).ToList(),
                    Sessions = vaultSessions.Select(s => new VaultNavItem
                    {
                        Id = s.ID,
                        Name = s.Name,
                        Type = "session"
                    }).ToList()
                };

                vaultGroups.Add(vaultGroup);
            }

            _logger.Log(nameof(VaultNavigationQueryService), $"Built navigation tree with {activeVaults.Count} vaults");

            return new VaultNavigationResponse
            {
                Vaults = vaultGroups
            };
        }
    }
}
