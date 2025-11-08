using System;

namespace CrowbaneArena
{
    /// <summary>
    /// Handles errors and exceptions in the mod.
    /// </summary>
    public static class ErrorHandler
    {
        public static void LogError(string error)
        {
            Console.WriteLine($"Error: {error}");
            // Placeholder for logging to file
        }

        public static void HandleException(Exception ex)
        {
            LogError(ex.Message);
        }
    }
}
