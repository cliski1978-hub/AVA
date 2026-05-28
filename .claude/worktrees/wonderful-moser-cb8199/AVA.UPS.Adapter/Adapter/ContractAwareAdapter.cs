using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Contracts;

namespace AVA.UPS.Adapter
{
    /// <summary>
    /// A contract-driven adapter that applies semantic UPS rules, defaults,
    /// domain type coercion, and strict/permissive mapping to and from DTOs.
    /// This class ensures DTO mappings fully align with the UPS contract.
    /// </summary>
    public class ContractAwareAdapter : BaseUParamAdapter
    {
        private readonly UPSMethodContract _contract;

        public ContractAwareAdapter(UPSMethodContract contract)
        {
            _contract = contract;
        }

        // --------------------------------------------------------------------
        // UPS → DTO
        // --------------------------------------------------------------------

        public override T Parse<T>(List<UParam> parameters)
        {
            var normalized = NormalizeIncoming(parameters);
            return base.Parse<T>(normalized, _contract);
        }

        public override Task<T> ParseAsync<T>(List<UParam> parameters)
            => Task.FromResult(Parse<T>(parameters));

        // --------------------------------------------------------------------
        // DTO → UPS
        // --------------------------------------------------------------------

        public override List<UParam> Build<T>(T dto)
        {
            var list = base.Build(dto, _contract);

            // Prune parameters not in contract
            list = list
                .Where(p => _contract.Parameters.Any(c =>
                    c.Key.Equals(p.Key, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Enforce contract ordering
            return OrderByContract(list);
        }

        public override Task<List<UParam>> BuildAsync<T>(T dto)
            => Task.FromResult(Build(dto));

        // --------------------------------------------------------------------
        // Incoming UPS parameter normalization
        // --------------------------------------------------------------------

        private List<UParam> NormalizeIncoming(List<UParam> parameters)
        {
            var normalized = new List<UParam>(parameters);

            foreach (var paramDef in _contract.Parameters)
            {
                bool exists = normalized.Any(p =>
                    p.Key.Equals(paramDef.Key, StringComparison.OrdinalIgnoreCase));

                // Inject default values when missing
                if (!exists && paramDef.Default != null)
                {
                    normalized.Add(new UParam
                    {
                        Key = paramDef.Key,
                        Type = paramDef.Type,
                        Value = paramDef.Default,
                        Meta = new Dictionary<string, object>
                        {
                            ["source"] = "contract-default"
                        }
                    });
                }
            }

            // Normalize semantic types according to contract
            foreach (var p in normalized)
            {
                var def = _contract.Parameters.FirstOrDefault(x =>
                    x.Key.Equals(p.Key, StringComparison.OrdinalIgnoreCase));

                if (def != null)
                {
                    // Override UParam.Type to contract.Type
                    p.Type = def.Type;
                }
            }

            return normalized;
        }

        // --------------------------------------------------------------------
        // Contract-based UParam ordering
        // --------------------------------------------------------------------

        private List<UParam> OrderByContract(List<UParam> list)
        {
            return list
                .OrderBy(p =>
                {
                    // Match contract order
                    for (int i = 0; i < _contract.Parameters.Count; i++)
                    {
                        if (_contract.Parameters[i].Key.Equals(p.Key,
                            StringComparison.OrdinalIgnoreCase))
                            return i;
                    }
                    return int.MaxValue;
                })
                .ToList();
        }
    }
}
