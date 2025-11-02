using ProjectM;
using VampireCommandFramework;

namespace CrowbaneArena.Commands.Converters
{
    public class ItemParameter
    {
        public PrefabGUID Value { get; set; }
        
        public static implicit operator ItemParameter(PrefabGUID guid) => new ItemParameter { Value = guid };
        public static implicit operator PrefabGUID(ItemParameter item) => item.Value;
    }
}