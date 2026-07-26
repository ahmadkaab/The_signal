using Godot;
using TheSignal.Core;

namespace TheSignal.Data;

[GlobalClass]
public partial class ItemResource : Resource
{
    [Export] public string ItemId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public ItemType Type { get; set; }
    [Export] public ItemRarity Rarity { get; set; }
    [Export] public Texture2D Icon { get; set; }
    [Export] public int MaxStack { get; set; } = 1;
    [Export] public int Value { get; set; }
    [Export] public int Weight { get; set; }
    [Export] public bool IsUnique { get; set; }
    [Export] public bool IsQuestItem { get; set; }
    [Export] public bool IsCraftable { get; set; }
    public List<CraftingIngredient> CraftingCost { get; set; } = new();
    public List<ItemMod> ImplicitMods { get; set; } = new();
    [Export] public int ChipSlots { get; set; } = 0;
    public List<string> CompatibleChips { get; set; } = new();
}

[GlobalClass]
public partial class ItemMod : Resource
{
    [Export] public ModType Type { get; set; }
    [Export] public float Value { get; set; }
    [Export] public bool IsPercent { get; set; }
    [Export] public string Description { get; set; }
}

public enum ItemType
{
    WeaponMelee,
    WeaponRanged,
    ArmorHead,
    ArmorChest,
    ArmorLegs,
    ArmorHands,
    ArmorFeet,
    Implant,
    Chip,
    Consumable,
    CraftingMaterial,
    QuestItem,
    KeyItem,
    Gadget
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Unique
}

public enum ModType
{
    DamageFlat,
    DamagePercent,
    ArmorFlat,
    ArmorPercent,
    HpFlat,
    HpPercent,
    ApFlat,
    CritChance,
    CritDamage,
    Accuracy,
    Evasion,
    ResonancePower,
    SignalRange,
    CorruptionResist,
    MutationSlots
}

[GlobalClass]
public partial class CraftingIngredient : Resource
{
    [Export] public string ItemId { get; set; }
    [Export] public int Count { get; set; } = 1;
}

[GlobalClass]
public partial class WeaponResource : ItemResource
{
    [Export] public int BaseDamage { get; set; }
    [Export] public DamageType DamageType { get; set; }
    [Export] public int Range { get; set; } = 1;
    [Export] public int ApCost { get; set; } = 2;
    [Export] public float CritChance { get; set; } = 0.05f;
    [Export] public float CritDamage { get; set; } = 0.5f;
    [Export] public WeaponTags Tags { get; set; }
    public List<WeaponAbility> WeaponAbilities { get; set; } = new();
}

[GlobalClass]
public partial class ArmorResource : ItemResource
{
    [Export] public int BaseArmor { get; set; }
    [Export] public ArmorType ArmorType { get; set; }
    public List<ArmorSetBonus> SetBonuses { get; set; } = new();
    [Export] public bool AllowsStealth { get; set; } = true;
}

[GlobalClass]
public partial class ChipResource : ItemResource
{
    [Export] public ChipType ChipType { get; set; }
    [Export] public string EffectId { get; set; }
    [Export] public int Tier { get; set; } = 1;
    public List<string> CompatibleWith { get; set; } = new();
}

[GlobalClass]
public partial class ConsumableResource : ItemResource
{
    [Export] public ConsumableEffect Effect { get; set; }
    [Export] public int CooldownTurns { get; set; } = 0;
}

public enum ArmorType
{
    Light,
    Medium,
    Heavy,
    Shield
}

public enum ChipType
{
    Damage,
    Defense,
    Utility,
    Resonance,
    Hacking
}

public enum ConsumableEffect
{
    Heal,
    RestoreAp,
    Cleanse,
    Buff,
    Grenade,
    Scan
}

[Flags]
public enum WeaponTags
{
    None = 0,
    Light = 1,
    Heavy = 2,
    TwoHanded = 4,
    Concealable = 8,
    Silenced = 16,
    Explosive = 32,
    Energy = 64,
    Bio = 128
}

[GlobalClass]
public partial class ArmorSetBonus : Resource
{
    [Export] public string SetName { get; set; }
    [Export] public int PiecesRequired { get; set; }
    public List<ItemMod> Bonuses { get; set; } = new();
}

[GlobalClass]
public partial class WeaponAbility : Resource
{
    [Export] public string AbilityId { get; set; }
    [Export] public string Name { get; set; }
    [Export] public int ApCost { get; set; }
    [Export] public int Cooldown { get; set; }
}