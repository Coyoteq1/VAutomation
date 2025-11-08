using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Models;

public class LoadoutModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WeaponData Weapon { get; set; } = new();
    public Armors Armor { get; set; } = new();
    public Abilities Abilities { get; set; } = new();
    public PassiveSpells PassiveSpells { get; set; } = new();
    public string BloodType { get; set; } = string.Empty;
    public List<BuildItemData> Consumables { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public bool Default { get; set; } = false;
    public LoadoutSettings Settings { get; set; } = new();
    public BloodConfig Blood { get; set; } = new();
}

public class LoadoutSettings
{
    public bool ClearInventory { get; set; } = true;
}

public class BloodConfig
{
    public string PrimaryType { get; set; } = string.Empty;
    public string SecondaryType { get; set; } = string.Empty;
    public string StatFocus { get; set; } = string.Empty;
    public float PrimaryQuality { get; set; } = 100f;
    public float SecondaryQuality { get; set; } = 0f;
    public int SecondaryBuffIndex { get; set; } = 0;
    public bool GiveBloodPotion { get; set; } = true;
    public bool FillBloodPool { get; set; } = true;
}
