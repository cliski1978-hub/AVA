using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CliskiCore.DbAPI.Interfaces;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core.Logger
{
    /// <summary>
    /// Standardized Vault logger compatible with CliskiCore.DbAPI.
    /// Routes logs to console, file, or any injected Microsoft ILogger.
    /// </summary>
    public sealed class VaultLogger : IContextLogger
    {
        private readonly ILogger<VaultLogger>? _logger;
        private readonly VaultInstanceConfig _config;
        private readonly string _logDirectory;
        private readonly ReaderWriterLockSlim _lock = new();

        public VaultLogger(VaultInstanceConfig config, ILogger<VaultLogger>? logger = null)
        {
            _config = config;
            _logger = logger;
            _logDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "logs",
                $"{config.VaultID}");

            Directory.CreateDirectory(_logDirectory);
        }

        // -------------------------------------------------------------
        // IContextLogger Implementation
        // -------------------------------------------------------------

        public void Log(string category, string message)
        {
            WriteLog("INFO", category, message);
        }

        public void LogError(string category, string message, Exception? ex = null)
        {
            WriteLog("ERROR", category, $"{message} {ex?.Message}");
        }

        public Task LogAsync(string category, string message, CancellationToken ct = default)
        {
            WriteLog("INFO", category, message);
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string category, string message, Exception? ex = null, CancellationToken ct = default)
        {
            WriteLog("ERROR", category, $"{message} {ex?.Message}");
            return Task.CompletedTask;
        }

        // -------------------------------------------------------------
        // Private Helpers
        // -------------------------------------------------------------

        private void WriteLog(string level, string category, string message)
        {
            string entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{category}] {message}";

            try
            {
                _lock.EnterWriteLock();

                string logFile = Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyyMMdd}.log");
                File.AppendAllText(logFile, entry + Environment.NewLine, Encoding.UTF8);

                // Forward to Microsoft logger if present
                if (_logger != null)
                {
                    if (level == "ERROR") _logger.LogError(entry);
                    else _logger.LogInformation(entry);
                }
                else
                {
                    Console.WriteLine(entry);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        // -------------------------------------------------------------
        // Convenience wrappers (for adapters and services)
        // -------------------------------------------------------------

        public void Info(string category, string message)
        {
            WriteLog("INFO", category, message);
        }

        public void Warn(string category, string message)
        {
            WriteLog("WARN", category, message);
        }

        public void Error(string category, string message, Exception? ex = null)
        {
            WriteLog("ERROR", category, $"{message} {ex?.Message}");
        }

    }
}
