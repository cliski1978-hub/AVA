using Microsoft.Extensions.DependencyInjection;
using AVA.Memory.Abstractions;
using AVA.Memory.Sql.Stores;

namespace AVA.Memory.Sql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers SQL-backed memory services.
        /// </summary>
        /// <param name="services">The DI service collection.</param>
        /// <param name="singleton">If true, registers as singletons (default: false / scoped).</param>
        public static IServiceCollection AddMemorySql(this IServiceCollection services, bool singleton = false)
        {
            if (singleton)
            {
                // 🟩 Singleton lifetimes for global memory architecture
                services.AddSingleton<IMemoryStore, SqlMemoryStore>();
                services.AddSingleton<IAssociationStore, SqlAssociationStore>();
                services.AddSingleton<IVectorIndex, SqlVectorIndex>();
            }
            else
            {
                // 🟦 Scoped lifetimes (per-request EF-style)
                services.AddScoped<IMemoryStore, SqlMemoryStore>();
                services.AddScoped<IAssociationStore, SqlAssociationStore>();
                services.AddScoped<IVectorIndex, SqlVectorIndex>();
            }

            return services;
        }
    }
}
