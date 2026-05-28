using System;
using System.Collections.Generic;

namespace AVA.UPS.Adapter.Models
{
    /// <summary>
    /// Standardized envelope for UPS messages, including payload,
    /// routing metadata, identity information, and diagnostics.
    /// </summary>
    public class UPSMessageEnvelope
    {
        // ----------------------------------------------------------
        // Core Envelope Metadata
        // ----------------------------------------------------------

        /// <summary>
        /// Canonical UPS envelope identifier.
        /// MUST always be 'ID' to match UPS routing, registry, and persistence layers.
        /// </summary>
        public string ID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Optional correlation ID for request/response workflows.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Name of the calling module (e.g., "VaultService", "AgentCore", "MemoryBroker").
        /// </summary>
        public string Source { get; set; } = default!;

        /// <summary>
        /// Name of the target module (must match a key in UPSModuleRegistry).
        /// </summary>
        public string Target { get; set; } = default!;

        /// <summary>
        /// Name of the target module (must match a key in UPSModuleRegistry).
        /// </summary>
        public string TargetMethod { get; set; } = default!;

        // ----------------------------------------------------------
        // Identity Information (Primary + Structured List)
        // ----------------------------------------------------------

        /// <summary>
        /// The primary identity issuing this message.
        /// </summary>
        public string? IdentityIdPrimary { get; set; }

        /// <summary>
        /// The human-readable identity handle (username, service ID).
        /// </summary>
        public string? IdentityHandlePrimary { get; set; }

        /// <summary>
        /// Indicates identity type ("User", "Service", "Agent", etc.).
        /// </summary>
        public string? IdentityTypePrimary { get; set; }

        /// <summary>
        /// Extended/secondary identities (multi-tenant, multi-agent, delegates, etc.).
        /// </summary>
        public List<UPSIdentityDescriptor> IdentityList { get; set; } = new();


        // ----------------------------------------------------------
        // Payload
        // ----------------------------------------------------------

        /// <summary>
        /// The UPS payload (UParams) transmitted between modules.
        /// </summary>
        public List<UParam> Payload { get; set; } = new();


        // ----------------------------------------------------------
        // Timing & Diagnostics
        // ----------------------------------------------------------

        /// <summary>
        /// Timestamp for tracking message creation.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional envelope-level error info.
        /// </summary>
        public UPSMessageError? Error { get; set; }

        /// <summary>
        /// Optional destination for response envelopes.
        /// </summary>
        public string? ReplyTo { get; set; }
    }


    // --------------------------------------------------------------------
    // Supporting Model: Extended Identity Descriptor
    // --------------------------------------------------------------------
    public class UPSIdentityDescriptor
    {
        public string? IdentityId { get; set; }
        public string? Handle { get; set; }
        public string? Type { get; set; }
    }


    // --------------------------------------------------------------------
    // Supporting Model: Envelope Error
    // --------------------------------------------------------------------
    public class UPSMessageError
    {
        public string Code { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string? Details { get; set; }
    }
}
