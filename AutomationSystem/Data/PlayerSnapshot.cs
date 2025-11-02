using Unity.Entities;

namespace CrowbaneArena.Data
{
    public class PlayerSnapshot
    {
        public Entity PlayerEntity { get; set; }
        public Entity CharacterEntity { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Mana { get; set; }
        public float MaxMana { get; set; }
        public float Stamina { get; set; }
        public float MaxStamina { get; set; }
        public System.Collections.Generic.List<Data.BuildItemData> Inventory { get; set; } = new();
        public System.Collections.Generic.List<Data.BuildItemData> Equipment { get; set; } = new();
        public bool IsValid { get; set; } = true;
        
        public PlayerSnapshot() { }
        
        public PlayerSnapshot(Entity playerEntity, Entity characterEntity, string playerName)
        {
            PlayerEntity = playerEntity;
            CharacterEntity = characterEntity;
            PlayerName = playerName;
            Health = 0f;
            MaxHealth = 0f;
            Mana = 0f;
            MaxMana = 0f;
            Stamina = 0f;
            MaxStamina = 0f;
        }
    }
}