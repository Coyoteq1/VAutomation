using VampireCommandFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CrowbaneArena.Services
{
    public static class CommandRegistry
    {
        public static void RegisterAll(params Type[] commandTypes)
        {
            try
            {
                // VCF automatically discovers and registers commands with [Command] attributes
                // We just need to ensure the assembly is loaded
                foreach (var type in commandTypes)
                {
                    if (type.GetCustomAttribute<CommandGroupAttribute>() != null)
                    {
                        Plugin.Logger?.LogInfo($"Discovered command group: {type.Name}");
                    }
                }
                Plugin.Logger?.LogInfo("VCF command registration complete");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to register commands: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
                throw;
            }
        }

        public static void RegisterAll(IEnumerable<Type> commandTypes)
        {
            RegisterAll(commandTypes.ToArray());
        }

        public static void RegisterAllInAssembly(Assembly assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();
            var commandTypes = new List<Type>();

            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<CommandGroupAttribute>() != null)
                {
                    commandTypes.Add(type);
                }
            }

            RegisterAll(commandTypes);
        }
    }
}
