using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Tests.Core.TestDoubles
{
    /// <summary>
    /// In-memory mock implementation of IAssociationStore used for testing.
    /// </summary>
    internal class FakeAssociationStore : IAssociationStore
    {
        private readonly Dictionary<string, AssociationEdgeDto> _edges = new();

        public Task<string> UpsertAsync(AssociationEdgeDto edge, CancellationToken ct)
        {
            if (edge == null)
                throw new ArgumentNullException(nameof(edge));

            if (string.IsNullOrWhiteSpace(edge.ID))
                edge.ID = Guid.NewGuid().ToString("N");

            edge.UpdatedAt = DateTime.UtcNow;

            _edges[edge.ID] = edge;
            return Task.FromResult(edge.ID);
        }

        public Task<AssociationEdgeDto?> GetAsync(string id, CancellationToken ct)
        {
            _edges.TryGetValue(id, out var edge);
            return Task.FromResult<AssociationEdgeDto?>(edge);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken ct)
        {
            var removed = _edges.Remove(id);
            return Task.FromResult(removed);
        }

        public Task<(IReadOnlyList<AssociationEdgeDto> Items, int Total)> ListAsync(int skip, int take, CancellationToken ct)
        {
            var list = _edges.Values
                .Skip(skip)
                .Take(take)
                .ToList();

            var total = _edges.Count;
            return Task.FromResult<(IReadOnlyList<AssociationEdgeDto>, int)>((list, total));
        }

        public void Clear()
        {
            _edges.Clear();
        }
    }
}
