using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AVA.Memory.Tests.Core.Utilities
{
    /// <summary>
    /// Provides helper methods for consistent stopwatch timing in tests.
    /// Simplifies duration measurement and async execution profiling.
    /// </summary>
    internal static class StopwatchHelper
    {
        /// <summary>
        /// Measures how long an action takes to execute and returns elapsed milliseconds.
        /// </summary>
        public static long Measure(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        /// <summary>
        /// Measures how long an asynchronous function takes to execute and returns elapsed milliseconds.
        /// </summary>
        public static async Task<long> MeasureAsync(Func<Task> asyncAction)
        {
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));

            var sw = Stopwatch.StartNew();
            await asyncAction();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        /// <summary>
        /// Executes an action and returns a tuple (result, durationMs).
        /// </summary>
        public static (T Result, long DurationMs) Measure<T>(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            var sw = Stopwatch.StartNew();
            var result = func();
            sw.Stop();
            return (result, sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Executes an async action and returns a tuple (result, durationMs).
        /// </summary>
        public static async Task<(T Result, long DurationMs)> MeasureAsync<T>(Func<Task<T>> asyncFunc)
        {
            if (asyncFunc == null)
                throw new ArgumentNullException(nameof(asyncFunc));

            var sw = Stopwatch.StartNew();
            var result = await asyncFunc();
            sw.Stop();
            return (result, sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Formats milliseconds to a human-readable time string.
        /// </summary>
        public static string Format(long durationMs)
        {
            if (durationMs < 1000)
                return $"{durationMs} ms";
            if (durationMs < 60_000)
                return $"{durationMs / 1000.0:F2} sec";
            return $"{TimeSpan.FromMilliseconds(durationMs):mm\\:ss\\.fff}";
        }
    }
}
