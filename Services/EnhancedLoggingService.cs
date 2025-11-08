using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BepInEx.Logging;
using Unity.Entities;
using CrowbaneArena.Models;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Comprehensive logging service that provides structured logging and performance tracking.
    /// Supplements the existing Plugin.Logger with additional capabilities.
    /// </summary>
    public static class EnhancedLoggingService
    {
        private static readonly Dictionary<string, DateTime> _operationTimers = new Dictionary<string, DateTime>();
        private static readonly object _lockObject = new object();
        private static string _logDirectory = "Logs";

        /// <summary>
        /// Initializes the enhanced logging service.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                EnsureLogDirectoryExists();
                LogSystemInfo();
                Plugin.Logger?.LogInfo("EnhancedLoggingService initialized successfully");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to initialize EnhancedLoggingService: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs performance metrics for an operation.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="duration">Duration of the operation in milliseconds</param>
        /// <param name="metadata">Optional metadata about the operation</param>
        public static void LogPerformance(string operationName, double duration, Dictionary<string, object> metadata = null)
        {
            try
            {
                var logEntry = new PerformanceLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    OperationName = operationName,
                    DurationMs = duration,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                var performanceLogPath = Path.Combine(_logDirectory, "performance.log");
                
                lock (_lockObject)
                {
                    var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = false });
                    File.AppendAllText(performanceLogPath, json + Environment.NewLine);
                }

                // Log to console if duration is significant
                if (duration > 100) // More than 100ms
                {
                    Plugin.Logger?.LogWarning($"Slow operation detected: {operationName} took {duration:F2}ms");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to log performance metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts timing an operation.
        /// </summary>
        /// <param name="operationName">Name of the operation to time</param>
        public static void StartTimer(string operationName)
        {
            lock (_lockObject)
            {
                _operationTimers[operationName] = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Stops timing an operation and logs the result.
        /// </summary>
        /// <param name="operationName">Name of the operation to stop timing</param>
        /// <param name="metadata">Optional metadata about the operation</param>
        public static void StopTimer(string operationName, Dictionary<string, object> metadata = null)
        {
            try
            {
                DateTime startTime;
                lock (_lockObject)
                {
                    if (!_operationTimers.TryGetValue(operationName, out startTime))
                    {
                        Plugin.Logger?.LogWarning($"No timer found for operation: {operationName}");
                        return;
                    }
                    _operationTimers.Remove(operationName);
                }

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                LogPerformance(operationName, duration, metadata);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to stop timer for {operationName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs entity validation results for debugging.
        /// </summary>
        /// <param name="entity">The entity being validated</param>
        /// <param name="validationResult">The validation result</param>
        /// <param name="context">Additional context for the validation</param>
        public static void LogEntityValidation(Entity entity, ValidationResult validationResult, string context = "")
        {
            try
            {
                var logLevel = validationResult.IsValid ? LogLevel.Info : LogLevel.Warning;
                var prefix = string.IsNullOrEmpty(context) ? "EntityValidation" : $"EntityValidation({context})";
                
                if (validationResult.IsValid)
                {
                    Plugin.Logger?.LogInfo($"{prefix}: Entity {entity} validated successfully");
                }
                else
                {
                    Plugin.Logger?.LogWarning($"{prefix}: Entity {entity} validation failed - {validationResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to log entity validation: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs arena state changes for debugging.
        /// </summary>
        /// <param name="playerName">Name of the player</param>
        /// <param name="action">Action being performed (enter/exit)</param>
        /// <param name="success">Whether the action was successful</param>
        /// <param name="error">Optional error message</param>
        public static void LogArenaStateChange(string playerName, string action, bool success, string error = null)
        {
            try
            {
                var message = $"Arena{action}: {playerName} - {(success ? "SUCCESS" : "FAILED")}";
                if (!string.IsNullOrEmpty(error))
                {
                    message += $" - Error: {error}";
                }

                var logLevel = success ? LogLevel.Info : LogLevel.Error;
                Plugin.Logger?.Log(logLevel, message);

                // Log to arena-specific file
                var arenaLogPath = Path.Combine(_logDirectory, "arena.log");
                var logEntry = new ArenaLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    PlayerName = playerName,
                    Action = action,
                    Success = success,
                    Error = error
                };

                lock (_lockObject)
                {
                    var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = false });
                    File.AppendAllText(arenaLogPath, json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to log arena state change: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs memory usage statistics.
        /// </summary>
        public static void LogMemoryUsage()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var memoryInfo = new
                {
                    WorkingSetMB = process.WorkingSet64 / (1024 * 1024),
                    PrivateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024),
                    VirtualMemoryMB = process.VirtualMemorySize64 / (1024 * 1024),
                    GCTotalMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024),
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                };

                Plugin.Logger?.LogInfo($"Memory Usage: WS={memoryInfo.WorkingSetMB}MB, Private={memoryInfo.PrivateMemoryMB}MB, GC={memoryInfo.GCTotalMemoryMB}MB");

                // Log to performance file
                var perfLogPath = Path.Combine(_logDirectory, "memory.log");
                var logEntry = new MemoryLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    WorkingSetMB = memoryInfo.WorkingSetMB,
                    PrivateMemoryMB = memoryInfo.PrivateMemoryMB,
                    VirtualMemoryMB = memoryInfo.VirtualMemoryMB,
                    GCTotalMemoryMB = memoryInfo.GCTotalMemoryMB,
                    Gen0Collections = memoryInfo.Gen0Collections,
                    Gen1Collections = memoryInfo.Gen1Collections,
                    Gen2Collections = memoryInfo.Gen2Collections
                };

                lock (_lockObject)
                {
                    var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = false });
                    File.AppendAllText(perfLogPath, json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to log memory usage: {ex.Message}");
            }
        }

        private static void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private static void LogSystemInfo()
        {
            try
            {
                var systemInfo = new
                {
                    OS = Environment.OSVersion.ToString(),
                    CLR = Environment.Version.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSetMB = Environment.WorkingSet / (1024 * 1024),
                    Is64BitOS = Environment.Is64BitOperatingSystem,
                    Is64BitProcess = Environment.Is64BitProcess
                };

                Plugin.Logger?.LogInfo($"System Info: {JsonSerializer.Serialize(systemInfo, new JsonSerializerOptions { WriteIndented = false })}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to log system info: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up old log files to prevent disk space issues.
        /// </summary>
        /// <param name="maxAgeDays">Maximum age of log files in days (default: 7)</param>
        public static void CleanupOldLogs(int maxAgeDays = 7)
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                    return;

                var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays);
                var logFiles = Directory.GetFiles(_logDirectory, "*.log");

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.LastWriteTimeUtc < cutoffDate)
                    {
                        try
                        {
                            fileInfo.Delete();
                            Plugin.Logger?.LogInfo($"Deleted old log file: {logFile}");
                        }
                        catch (Exception ex)
                        {
                            Plugin.Logger?.LogWarning($"Failed to delete old log file {logFile}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to cleanup old logs: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Log entry for performance metrics.
    /// </summary>
    public class PerformanceLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string OperationName { get; set; }
        public double DurationMs { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Log entry for arena state changes.
    /// </summary>
    public class ArenaLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string PlayerName { get; set; }
        public string Action { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Log entry for memory usage statistics.
    /// </summary>
    public class MemoryLogEntry
    {
        public DateTime Timestamp { get; set; }
        public long WorkingSetMB { get; set; }
        public long PrivateMemoryMB { get; set; }
        public long VirtualMemoryMB { get; set; }
        public long GCTotalMemoryMB { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
    }
}
