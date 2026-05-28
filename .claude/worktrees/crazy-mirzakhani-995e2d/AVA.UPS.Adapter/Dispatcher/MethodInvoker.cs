using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Validation;
using AVA.UPS.Adapter.Dispatcher;

namespace AVA.UPS.Adapter.Dispatcher
{
    /// <summary>
    /// Reflection-based UPS method invoker supporting UPS v1.0+ signatures,
    /// diagnostics injection, correlation awareness, and structured error handling.
    /// </summary>
    public static class MethodInvoker
    {
        public static async Task<DispatcherResult> InvokeAsync(
            MethodInfo method,
            object instance,
            List<UParam> parameters,
            string moduleName,
            string methodName,
            string? correlationId = null,
            CancellationToken token = default)
        {
            var diagnostics = new Dictionary<string, object>
            {
                ["module"] = moduleName,
                ["method"] = methodName,
                ["correlationId"] = correlationId ?? "",
                ["invoker"] = "MethodInvoker",
                ["timestamp"] = DateTime.UtcNow.ToString("o")
            };

            try
            {
                object?[] args = BuildArguments(method, parameters, diagnostics, token);

                object? result;

                try
                {
                    result = method.Invoke(instance, args);
                }
                catch (TargetInvocationException ex)
                {
                    // unwrap actual exception
                    return DispatcherResult.FromInvocationError(
                        moduleName,
                        methodName,
                        ex.InnerException?.Message ?? ex.Message,
                        correlationId);
                }

                if (result == null)
                {
                    return DispatcherResult.FromError(
                        errorPayload: new List<UParam>(),
                        errorType: "InvocationReturnedNull",
                        correlationId: correlationId);
                }

                // Handle Task<List<UParam>>
                if (result is Task<List<UParam>> listTask)
                {
                    var payload = await listTask;
                    return DispatcherResult.FromSuccess(payload, correlationId, diagnostics);
                }

                // Handle Task<UPSResponse> (future-proof)
                if (result is Task<UPSResponse> responseTask)
                {
                    var response = await responseTask;
                    return new DispatcherResult
                    {
                        Success = response.Success,
                        Payload = response.Payload ?? new List<UParam>(),
                        CorrelationId = correlationId,
                        Diagnostics = diagnostics
                    };
                }

                // Unsupported return type
                return DispatcherResult.FromError(
                    errorPayload: StandardUPSErrors.ContractError(
                        moduleName,
                        methodName,
                        new List<string>
                        {
                            $"Method '{methodName}' returned unsupported type '{method.ReturnType}'."
                        }).Value as List<UParam> ?? new List<UParam>(),
                    errorType: "InvalidReturnType",
                    correlationId: correlationId);
            }
            catch (Exception ex)
            {
                return DispatcherResult.FromInvocationError(
                    moduleName,
                    methodName,
                    ex.Message,
                    correlationId);
            }
        }

        // --------------------------------------------------------------------
        // Build dynamic invocation parameter list
        // --------------------------------------------------------------------

        private static object?[] BuildArguments(
            MethodInfo method,
            List<UParam> parameters,
            Dictionary<string, object> diagnostics,
            CancellationToken token)
        {
            var methodParams = method.GetParameters();

            var args = new object?[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                var p = methodParams[i];

                if (p.ParameterType == typeof(List<UParam>))
                {
                    args[i] = parameters;
                    continue;
                }

                if (p.ParameterType == typeof(Dictionary<string, object>))
                {
                    args[i] = diagnostics;
                    continue;
                }

                if (p.ParameterType == typeof(CancellationToken))
                {
                    args[i] = token;
                    continue;
                }

                // Unknown param type
                throw new InvalidOperationException(
                    $"Method parameter '{p.Name}' has unsupported type '{p.ParameterType}'.");
            }

            return args;
        }
    }
}
