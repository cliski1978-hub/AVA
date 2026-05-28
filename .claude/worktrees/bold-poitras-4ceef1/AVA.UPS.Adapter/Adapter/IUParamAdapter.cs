using System.Collections.Generic;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Contracts;

namespace AVA.UPS.Adapter
{
    /// <summary>
    /// Converts between UPS UParams and strongly typed DTOs.
    /// Supports async operations, contract awareness, semantic type mapping,
    /// and strict/permissive transformation modes.
    /// </summary>
    public interface IUParamAdapter
    {
        // --------------------------------------------------------------------
        // 1. Basic Synchronous DTO Mapping
        // --------------------------------------------------------------------

        /// <summary>
        /// Converts a list of UPS parameters into a strongly typed DTO.
        /// Contract-independent (raw mapping).
        /// </summary>
        T Parse<T>(List<UParam> parameters) where T : new();

        /// <summary>
        /// Converts a strongly typed DTO into a set of UPS parameters.
        /// Contract-independent (raw mapping).
        /// </summary>
        List<UParam> Build<T>(T dto);

        // --------------------------------------------------------------------
        // 2. Contract-Aware Synchronous Mapping
        // --------------------------------------------------------------------

        /// <summary>
        /// Contract-aware DTO parsing with type enforcement and defaults.
        /// Populates fields according to UPSMethodContract semantics.
        /// </summary>
        T Parse<T>(List<UParam> parameters, UPSMethodContract contract) where T : new();

        /// <summary>
        /// Contract-aware DTO building that respects expected parameter keys,
        /// semantic types, and list-structured definitions.
        /// </summary>
        List<UParam> Build<T>(T dto, UPSMethodContract contract);

        // --------------------------------------------------------------------
        // 3. Asynchronous Versions (Future-proof for IO/Multi-agent)
        // --------------------------------------------------------------------

        /// <summary>
        /// Async version of Parse for scenarios where value resolution
        /// requires IO (identity lookup, vector DB, remote DTO transforms, etc.)
        /// </summary>
        Task<T> ParseAsync<T>(List<UParam> parameters) where T : new();

        /// <summary>
        /// Async version of Build supporting remote resolution, identity routing,
        /// and semantic enrichment.
        /// </summary>
        Task<List<UParam>> BuildAsync<T>(T dto);

        /// <summary>
        /// Contract-aware async parsing (full UPS lifecycle support).
        /// </summary>
        Task<T> ParseAsync<T>(List<UParam> parameters, UPSMethodContract contract) where T : new();

        /// <summary>
        /// Contract-aware async UPS parameter construction.
        /// </summary>
        Task<List<UParam>> BuildAsync<T>(T dto, UPSMethodContract contract);

        // --------------------------------------------------------------------
        // 4. Adapter Configuration Flags
        // --------------------------------------------------------------------

        /// <summary>
        /// StrictMode:
        /// true = DTO must match contract exactly (no missing fields, no extras).
        /// false = permissive mode, allowing partial DTOs and auto-coercions.
        /// </summary>
        bool StrictMode { get; set; }

        /// <summary>
        /// When true, identity fields (Id, Handle, Type) are automatically mapped
        /// from DTO annotations or naming conventions.
        /// </summary>
        bool AutoMapIdentity { get; set; }

        /// <summary>
        /// When true, embedding arrays and lists are flattened/normalized automatically.
        /// </summary>
        bool AutoNormalizeEmbeddings { get; set; }

        /// <summary>
        /// When true, DTO properties with nested objects will be converted into
        /// structured UParam blocks automatically.
        /// </summary>
        bool EnableNestedDTOs { get; set; }
    }
}
