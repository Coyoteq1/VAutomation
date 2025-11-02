using System.Collections.Generic;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Represents a weapon model with its properties and aliases
    /// </summary>
    public class WeaponModel
    {
        /// <summary>
        /// The display name of the weapon
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The internal identifier for the weapon
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Alternative names/aliases for this weapon
        /// </summary>
        public List<string> Aliases { get; set; }

        /// <summary>
        /// Creates a new weapon model
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="identifier">Internal identifier</param>
        /// <param name="aliases">List of aliases</param>
        public WeaponModel(string name, string identifier, List<string> aliases)
        {
            Name = name;
            Identifier = identifier;
            Aliases = aliases ?? new List<string>();
        }
    }
}