using System.Collections.Generic;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Contracts;

namespace AVA.UPS.Adapter.Validation
{
    /// <summary>
    /// Represents the full result of validating a UPS call against its contract.
    /// Includes human-readable errors AND structured mismatch diagnostics.
    /// </summary>
    public class ContractValidationResult
    {
        /// <summary>
        /// True when no errors or mismatches were detected.
        /// </summary>
        public bool IsValid => Errors.Count == 0 && Mismatches.Count == 0;

        /// <summary>
        /// Human-readable errors for quick debugging.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Structured mismatch entries for deep inspection and automated correction.
        /// </summary>
        public List<ContractMismatchDetail> Mismatches { get; set; } = new();

        /// <summary>
        /// The expected method contract.
        /// </summary>
        public UPSMethodContract? ExpectedContract { get; set; }

        /// <summary>
        /// The received UParams from the UPS envelope.
        /// Always non-null for safe reporting.
        /// </summary>
        public List<UParam> ReceivedParameters { get; set; } = new();

        /// <summary>
        /// Converts this validation result into a standardized UPS error payload.
        /// Includes both human-readable messages AND structured mismatch data.
        /// </summary>
        public List<UParam> ToUPS(string moduleName, string methodName)
        {
            var expected = ExpectedContract != null
                ? new
                {
                    method = ExpectedContract.Name,
                    parameters = ExpectedContract.Parameters
                }
                : null;

            var mismatchDetails = Mismatches.Count > 0 ? Mismatches : null;

            var received = new
            {
                parameters = ReceivedParameters,
                mismatches = mismatchDetails
            };

            return new List<UParam>
            {
                StandardUPSErrors.ContractError(
                    module: moduleName,
                    method: methodName,
                    errors: Errors,
                    expected: expected,
                    received: received
                )
            };
        }
    }

    /// <summary>
    /// Represents a single structured mismatch detected during contract validation.
    /// Enables precise diagnostics and future self-repair behaviors.
    /// </summary>
    public class ContractMismatchDetail
    {
        public string ParamKey { get; set; } = string.Empty;
        public string? ExpectedType { get; set; }
        public string? ActualType { get; set; }
        public string Issue { get; set; } = string.Empty;
    }
}
