using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Contracts;
using AVA.UPS.Adapter.Validation;

namespace AVA.UPS.Adapter.Dispatcher
{
    /// <summary>
    /// Executes UPS method calls inside the same process using reflection.
    /// Fully contract-aware, correlation-aware, and compatible with UPS v1.0+.
    /// </summary>
    public class ModuleDispatcher
    {
        private readonly object _instance;
        private readonly string _moduleName;
        private readonly Dictionary<string, MethodInfo> _methodLookup;
        private readonly ContractValidator _validator;

        public ModuleDispatcher(object instance, UPSContractFile contractFile)
        {
            _instance = instance;
            _moduleName = contractFile.Module;
            _validator = new ContractValidator(contractFile.Methods);
            _methodLookup = DiscoverMethods(instance);
        }

        // --------------------------------------------------------------------
        // Discover valid UPS methods on the instance
        // --------------------------------------------------------------------

        private Dictionary<string, MethodInfo> DiscoverMethods(object instance)
        {
            return instance
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(MethodSignatureIsValid)
                .ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);
        }

        private bool MethodSignatureIsValid(MethodInfo m)
        {
            // Core UPS v1.0+ signature: Task<List<UParam>>, List<UParam> first parameter
            var p = m.GetParameters();

            if (p.Length == 0)
                return false;

            if (!typeof(List<UParam>).IsAssignableFrom(p[0].ParameterType))
                return false;

            if (m.ReturnType == typeof(Task<List<UParam>>))
                return true;

            if (m.ReturnType == typeof(Task<UPSResponse>))
                return true;

            return false;
        }

        // --------------------------------------------------------------------
        // Invocation Pipeline
        // --------------------------------------------------------------------

        public async Task<DispatcherResult> InvokeAsync(
            string methodName,
            List<UParam> parameters,
            string? correlationId = null,
            CancellationToken token = default)
        {
            // STEP 1 — Contract Validation ------------------------------------
            var validation = _validator.Validate(methodName, parameters);

            if (!validation.IsValid)
            {
                return DispatcherResult.FromContractError(
                    validation,
                    _moduleName,
                    methodName,
                    correlationId
                );
            }

            // STEP 2 — Resolve method -----------------------------------------
            if (!_methodLookup.TryGetValue(methodName, out var method))
            {
                return DispatcherResult.FromError(
                    errorPayload: new List<UParam>
                    {
                        StandardUPSErrors.ContractError(
                            _moduleName,
                            methodName,
                            new List<string> {
                                $"Method '{methodName}' not found in module '{_moduleName}'."
                            })
                    },
                    errorType: "MissingMethod",
                    correlationId: correlationId
                );
            }

            // STEP 3 — Reflection Invoke --------------------------------------
            try
            {
                return await MethodInvoker.InvokeAsync(
                    method,
                    _instance,
                    parameters,
                    _moduleName,
                    methodName,
                    correlationId,
                    token
                );
            }
            catch (Exception ex)
            {
                return DispatcherResult.FromInvocationError(
                    _moduleName,
                    methodName,
                    ex.Message,
                    correlationId
                );
            }
        }
    }
}
