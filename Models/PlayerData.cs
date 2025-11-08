using Unity.Entities;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Represents player data including user and character entity references.
    /// </summary>
    public class PlayerData
    {
        /// <summary>
        /// The user entity associated with this player.
        /// </summary>
        public Entity UserEntity { get; set; }

        /// <summary>
        /// The character entity controlled by this player.
        /// </summary>
        public Entity CharEntity { get; set; }

        /// <summary>
        /// The player's character name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The player's Steam ID.
        /// </summary>
        public ulong SteamId { get; set; }

        /// <summary>
        /// Indicates whether the player is currently connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerData"/> class.
        /// </summary>
        public PlayerData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerData"/> class.
        /// </summary>
        /// <param name="name">The player's character name.</param>
        /// <param name="steamId">The player's Steam ID.</param>
        /// <param name="isConnected">Whether the player is connected.</param>
        /// <param name="userEntity">The user entity.</param>
        /// <param name="characterEntity">The character entity.</param>
        public PlayerData(string name, ulong steamId, bool isConnected, Entity userEntity, Entity characterEntity)
        {
            Name = name;
            SteamId = steamId;
            IsConnected = isConnected;
            UserEntity = userEntity;
            CharEntity = characterEntity;
        }
    }
}
