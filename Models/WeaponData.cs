using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Models;

public class WeaponData
{
    public string Description { get; set; } = string.Empty;
    public List<string> Variants { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public string InfuseSpellMod { get; set; } = string.Empty;
    public string StatMod1 { get; set; } = string.Empty;
    public float StatMod1Power { get; set; } = 1.0f;
    public string StatMod2 { get; set; } = string.Empty;
    public float StatMod2Power { get; set; } = 1.0f;
    public string StatMod3 { get; set; } = string.Empty;
    public float StatMod3Power { get; set; } = 1.0f;
    public string StatMod4 { get; set; } = string.Empty;
    public float StatMod4Power { get; set; } = 1.0f;
    public string SpellMod1 { get; set; } = string.Empty;
    public string SpellMod2 { get; set; } = string.Empty;
}

public class BuildItemData
{
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; } = 1;
}
