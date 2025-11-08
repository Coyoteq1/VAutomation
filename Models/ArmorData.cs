using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Models;

public class Armors
{
    public string Boots { get; set; } = string.Empty;
    public string Chest { get; set; } = string.Empty;
    public string Gloves { get; set; } = string.Empty;
    public string Legs { get; set; } = string.Empty;
    public string MagicSource { get; set; } = string.Empty;
    public string Head { get; set; } = string.Empty;
    public string Cloak { get; set; } = string.Empty;
    public string Bag { get; set; } = string.Empty;
}

public class JewelData
{
    public string SpellMod1 { get; set; } = string.Empty;
    public float SpellMod1Power { get; set; } = 1.0f;
    public string SpellMod2 { get; set; } = string.Empty;
    public float SpellMod2Power { get; set; } = 1.0f;
    public string SpellMod3 { get; set; } = string.Empty;
    public float SpellMod3Power { get; set; } = 1.0f;
    public string SpellMod4 { get; set; } = string.Empty;
    public float SpellMod4Power { get; set; } = 1.0f;
}
