using VampireCommandFramework;
using ProjectM;
using Unity.Entities;
using System;

namespace CrowbaneArena
{
    /// <summary>
    /// Custom converter for Arena-specific types
    /// </summary>
    public class ArenaEntityConverter : CommandArgumentConverter<Entity, ICommandContext>
    {
        public override Entity Parse(ICommandContext ctx, string input)
        {
            if (TryParse(ctx, input, out var result))
                return result;
            throw new ArgumentException($"Could not parse '{input}' as Entity");
        }

        public bool TryParse(ICommandContext ctx, string input, out Entity result)
        {
            result = Entity.Null;

            // Try to parse as GUID first
            if (Guid.TryParse(input, out var guid))
            {
                result = new Entity { Index = (int)guid.GetHashCode(), Version = 1 };
                return true;
            }

            // Try to find player by name
            var player = PlayerManager.GetPlayerByName(input);
            if (!player.Equals(Entity.Null))
            {
                result = player;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Converter for arena build presets
    /// </summary>
    public class BuildPresetConverter : CommandArgumentConverter<Build, ICommandContext>
    {
        public override Build Parse(ICommandContext ctx, string input)
        {
            if (TryParse(ctx, input, out var result))
                return result;
            throw new ArgumentException($"Could not parse '{input}' as Build");
        }

        public bool TryParse(ICommandContext ctx, string input, out Build result)
        {
            result = null;

            // Try to find build by name - placeholder implementation
            result = new Build { Name = input };
            return true;
        }
    }
}
