using System;
using System.IO;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for logging arena events and usage tracking.
    /// Ensures the log directory exists and writes both to file and console.
    /// </summary>
    public class LoggingService
    {
        private readonly string logPath = "server/logs/usage_tracking.log";

        private void EnsureLogDirectory()
        {
            try
            {
                var dir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                // Fall back to console-only logging if we can't create the directory
                Console.WriteLine($"[LoggingService] Failed to ensure log directory: {ex.Message}");
            }
        }

        public void LogEvent(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";

            // Log to BepInEx logger
            Plugin.Logger?.Log(BepInEx.Logging.LogLevel.Info, logMessage);

            // Also log to file for persistent tracking
            try
            {
                EnsureLogDirectory();
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.Log(BepInEx.Logging.LogLevel.Warning, $"[LoggingService] File log failed: {ex.Message}");
            }
        }

        public void LogArenaEntry(object playerEntity)
        {
            LogEvent($"Player {playerEntity} entered arena.");
        }

        public void LogArenaExit(object playerEntity)
        {
            LogEvent($"Player {playerEntity} exited arena.");
        }

        public void LogWarning(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | WARNING | {message}";
            Plugin.Logger?.Log(BepInEx.Logging.LogLevel.Warning, logMessage);

            try
            {
                EnsureLogDirectory();
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.Log(BepInEx.Logging.LogLevel.Warning, $"[LoggingService] File log failed: {ex.Message}");
            }
        }

        public void LogError(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR | {message}";
            Plugin.Logger?.Log(BepInEx.Logging.LogLevel.Error, logMessage);

            try
            {
                EnsureLogDirectory();
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.Log(BepInEx.Logging.LogLevel.Warning, $"[LoggingService] File log failed: {ex.Message}");
            }
        }
    }
}
