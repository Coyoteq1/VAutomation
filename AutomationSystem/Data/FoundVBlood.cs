using ProjectM;
using Unity.Entities;

namespace CrowbaneArena.Data
{
    public class FoundVBlood
    {
        public PrefabGUID GUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public Entity Entity { get; set; }
        
        public FoundVBlood() { }
        
        public FoundVBlood(PrefabGUID guid, string name, Entity entity)
        {
            GUID = guid;
            Name = name;
            Entity = entity;
            IsAlive = false;
        }
    }
}