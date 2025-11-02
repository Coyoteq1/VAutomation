using Unity.Entities;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Represents a player with user and character entities
    /// </summary>
    public class Player
    {
        public Entity User { get; set; } = Entity.Null;
        public Entity Character { get; set; } = Entity.Null;
    }
}