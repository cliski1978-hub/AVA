using System;
using System.Collections.Generic;
using System.Linq;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Contracts;

namespace AVA.UPS.Adapter.Validation
{
    /// <summary>
    /// Validates incoming UPS parameters against a method contract.
    /// Generates human-readable errors AND structured mismatch diagnostics.
    /// </summary>
    public class ContractValidator
    {
        private readonly Dictionary<string, UPSMethodContract> _contractLookup;

        // --------------------------------------------------------------------
        // Constructors
        // --------------------------------------------------------------------

        public ContractValidator(UPSContractFile contractFile)
        {
            _contractLookup = contractFile.Methods
                .ToDictionary(m => m.Name.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        public ContractValidator(List<UPSMethodContract> contracts)
        {
            _contractLookup = contracts
                .ToDictionary(m => m.Name.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        // --------------------------------------------------------------------
        // Main Validation Entry Point
        // --------------------------------------------------------------------

        public ContractValidationResult Validate(
            string methodName,
            List<UParam> parameters)
        {
            var result = new ContractValidationResult
            {
                ReceivedParameters = parameters
            };

            // Normalize method name
            methodName = methodName.Trim();

            // ----------------------------------------------------------------
            // 1. Ensure method exists
            // ----------------------------------------------------------------
            if (!_contractLookup.TryGetValue(methodName, out var contract))
            {
                result.Errors.Add($"No contract found for method '{methodName}'.");
                result.Mismatches.Add(new ContractMismatchDetail
                {
                    ParamKey = "",
                    Issue = "ContractNotFound",
                    ExpectedType = null,
                    ActualType = null
                });

                return result;
            }

            result.ExpectedContract = contract;

            // ----------------------------------------------------------------
            // 2. Normalize parameter keys
            // ----------------------------------------------------------------
            foreach (var p in parameters)
                p.Key = p.Key?.Trim() ?? string.Empty;

            // ----------------------------------------------------------------
            // 3. Validate required parameters + defaults
            // ----------------------------------------------------------------
            foreach (var expected in contract.Parameters)
            {
                var match = parameters.FirstOrDefault(p =>
                    p.Key.Equals(expected.Key, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    if (expected.Required && expected.Default == null)
                    {
                        // Missing required parameter
                        result.Errors.Add($"Missing required parameter: {expected.Key}");
                        result.Mismatches.Add(new ContractMismatchDetail
                        {
                            ParamKey = expected.Key,
                            ExpectedType = expected.Type,
                            ActualType = null,
                            Issue = "MissingRequired"
                        });
                    }

                    // Apply default if available
                    if (!expected.Required && expected.Default != null)
                    {
                        parameters.Add(new UParam
                        {
                            Key = expected.Key,
                            Type = expected.Type,
                            Value = expected.Default
                        });
                    }
                }
            }

            // ----------------------------------------------------------------
            // 4. Detect unexpected parameters
            // ----------------------------------------------------------------
            foreach (var param in parameters)
            {
                var expected = contract.Parameters.FirstOrDefault(p =>
                    p.Key.Equals(param.Key, StringComparison.OrdinalIgnoreCase));

                if (expected == null)
                {
                    result.Errors.Add($"Unexpected parameter: {param.Key}");
                    result.Mismatches.Add(new ContractMismatchDetail
                    {
                        ParamKey = param.Key,
                        ExpectedType = null,
                        ActualType = param.Type,
                        Issue = "UnexpectedParameter"
                    });
                }
            }

            // ----------------------------------------------------------------
            // 5. Type validation (semantic + structural)
            // ----------------------------------------------------------------
            foreach (var param in parameters)
            {
                var expected = contract.Parameters.FirstOrDefault(p =>
                    p.Key.Equals(param.Key, StringComparison.OrdinalIgnoreCase));

                if (expected == null)
                    continue;

                bool valid = UParamTypeChecker.IsTypeValid(param, expected.Type);

                if (!valid)
                {
                    result.Errors.Add(
                        $"Parameter '{param.Key}' type mismatch: expected '{expected.Type}'."
                    );

                    result.Mismatches.Add(new ContractMismatchDetail
                    {
                        ParamKey = param.Key,
                        ExpectedType = expected.Type,
                        ActualType = param.Type ?? param.Value?.GetType()?.Name,
                        Issue = "TypeMismatch"
                    });
                }
            }

            return result;
        }
    }
}
