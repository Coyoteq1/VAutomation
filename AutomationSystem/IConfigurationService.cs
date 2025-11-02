using System;

namespace AutomationSystem
{
    /// <summary>
    /// Interface for configuration service functionality
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the current configuration
        /// </summary>
        T? GetConfig<T>() where T : class;

        /// <summary>
        /// Saves a configuration object
        /// </summary>
        /// <typeparam name="T">The type of configuration</typeparam>
        /// <param name="config">The configuration to save</param>
        void SaveConfig<T>(T config) where T : class;

        /// <summary>
        /// Reloads the configuration from disk
        /// </summary>
        void Reload();

        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        string ConfigPath { get; }

        /// <summary>
        /// Checks if configuration file exists
        /// </summary>
        bool ConfigExists { get; }

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        bool ValidateConfig();
    }
}