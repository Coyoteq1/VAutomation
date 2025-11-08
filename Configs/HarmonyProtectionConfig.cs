using System.Collections.Generic;
using Newtonsoft.Json;

namespace CrowbaneArena.Configs
{
    public class HarmonyProtectionConfig
    {
        public HarmonyProtectionSettings HarmonyProtection { get; set; }
        public VBloodHookSettings VBloodHookSettings { get; set; }
        public DebuggingSettings Debugging { get; set; }
    }

    public class HarmonyProtectionSettings
    {
        public bool Enabled { get; set; }
        public string LogLevel { get; set; }
        public bool SkipProblematicAssemblies { get; set; }
        public int MaxRetries { get; set; }
        public List<string> ProblematicAssemblies { get; set; }
        public List<string> ProblematicTypePatterns { get; set; }
        public bool EnableAssemblyInfoLogging { get; set; }
        public bool EnableDetailedErrorReporting { get; set; }
        public bool GracefulDegradation { get; set; }
    }

    public class VBloodHookSettings
    {
        public bool Enabled { get; set; }
        public bool FallbackOnFailure { get; set; }
        public int MaxTypeDiscoveryAttempts { get; set; }
        public bool LogSuccessfulPatches { get; set; }
        public bool LogFailedPatches { get; set; }
    }

    public class DebuggingSettings
    {
        public bool EnableReflectionDebugging { get; set; }
        public bool LogTypeLoadingTimes { get; set; }
        public bool VerboseAssemblyScanning { get; set; }
    }

    public static class HarmonyProtectionConfigHelper
    {
        public static HarmonyProtectionConfig Load(string configPath)
        {
            if (!System.IO.File.Exists(configPath))
                throw new System.IO.FileNotFoundException("Harmony protection config file not found", configPath);

            string json = System.IO.File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<HarmonyProtectionConfig>(json);
        }

        public static void Save(HarmonyProtectionConfig config, string configPath)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            System.IO.File.WriteAllText(configPath, json);
        }

        public static bool Validate(HarmonyProtectionConfig config)
        {
            // Basic validation
            if (config.HarmonyProtection == null || config.VBloodHookSettings == null || config.Debugging == null)
                return false;

            if (config.HarmonyProtection.MaxRetries < 0)
                return false;

            return true;
        }
    }
}
