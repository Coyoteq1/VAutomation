using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Models;

public interface IAbilityData
{
    string Name { get; set; }
}

public class AbilityData : IAbilityData
{
    public string Name { get; set; } = string.Empty;
    public JewelData Jewel { get; set; } = new();
}

public class Abilities
{
    public IAbilityData Travel { get; set; } = new AbilityData();
    public IAbilityData Ability1 { get; set; } = new AbilityData();
    public IAbilityData Ability2 { get; set; } = new AbilityData();
    public IAbilityData Ultimate { get; set; } = new AbilityData();
}

public class PassiveSpells
{
    public string PassiveSpell1 { get; set; } = string.Empty;
    public string PassiveSpell2 { get; set; } = string.Empty;
    public string PassiveSpell3 { get; set; } = string.Empty;
    public string PassiveSpell4 { get; set; } = string.Empty;
    public string PassiveSpell5 { get; set; } = string.Empty;
}
