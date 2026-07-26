using Godot;
using TheSignal.Core;

namespace TheSignal.Data;

[GlobalClass]
public partial class AbilityResource : Resource
{
    [Export] public string AbilityId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Texture2D Icon { get; set; }
    [Export] public AbilityType Type { get; set; }
    [Export] public DamageType DamageType { get; set; }

    [ExportGroup("Costs")]
    [Export] public int ApCost { get; set; } = 2;
    [Export] public int CooldownTurns { get; set; } = 0;
    [Export] public int ResonanceCost { get; set; } = 0;
    [Export] public int MaxCharges { get; set; } = 0;

    [ExportGroup("Targeting")]
    [Export] public AbilityTargetType TargetType { get; set; }
    [Export] public int Range { get; set; } = 3;
    [Export] public int Radius { get; set; } = 0;
    [Export] public bool RequiresLineOfSight { get; set; } = true;
    [Export] public bool CanTargetSelf { get; set; } = false;
    [Export] public bool CanTargetAllies { get; set; } = true;
    [Export] public bool CanTargetEnemies { get; set; } = true;
    [Export] public bool CanTargetEmptyTile { get; set; } = false;

    [ExportGroup("Effects")]
    [Export] public int BaseDamage { get; set; } = 0;
    [Export] public float StatScaling { get; set; } = 1.0f;
    [Export] public ScalingStat ScalingStat { get; set; } = ScalingStat.Might;
    public List<StatusEffectApplication> StatusEffects { get; set; } = new();
    public List<StatModifierEffect> StatModifiers { get; set; } = new();
    public List<PositionEffect> PositionEffects { get; set; } = new();
    public List<ResourceEffect> ResourceEffects { get; set; } = new();
    public List<SummonEffect> SummonEffects { get; set; } = new();
    public List<FieldEffect> FieldEffects { get; set; } = new();

    [ExportGroup("Animation & Audio")]
    [Export] public string AnimationTrigger { get; set; }
    [Export] public PackedScene VfxPrefab { get; set; }
    [Export] public AudioStream SoundEffect { get; set; }
    [Export] public float CastTime { get; set; } = 0f;

    [ExportGroup("Requirements")]
    [Export] public int MinLevel { get; set; } = 1;
    public List<string> RequiredSignalNodes { get; set; } = new();
    public List<string> RequiredMutations { get; set; } = new();
    [Export] public WeaponTags RequiredWeaponTags { get; set; } = WeaponTags.None;
}

[GlobalClass]
public partial class StatusEffectApplication : Resource
{
    [Export] public StatusEffectType EffectType { get; set; }
    [Export] public int Duration { get; set; } = 2;
    [Export] public int Stacks { get; set; } = 1;
    [Export] public int MaxStacks { get; set; } = 3;
    [Export] public float Chance { get; set; } = 1.0f;
    [Export] public bool OnHit { get; set; } = true;
    [Export] public bool OnCrit { get; set; } = false;
    [Export] public bool OnKill { get; set; } = false;
}

[GlobalClass]
public partial class StatModifierEffect : Resource
{
    [Export] public StatType Stat { get; set; }
    [Export] public float FlatBonus { get; set; } = 0f;
    [Export] public float PercentBonus { get; set; } = 0f;
    [Export] public int Duration { get; set; } = 2;
    [Export] public string SourceId { get; set; }
    [Export] public bool IsDebuff { get; set; } = false;
}

[GlobalClass]
public partial class PositionEffect : Resource
{
    [Export] public PositionEffectType Type { get; set; }
    [Export] public int Distance { get; set; } = 1;
    [Export] public bool IgnoreOccupied { get; set; } = false;
    
    // Runtime-only (not exported)
    public Vector2I TargetTile { get; set; }
    public string Caster { get; set; }
}

[GlobalClass]
public partial class ResourceEffect : Resource
{
    [Export] public ResourceType Resource { get; set; }
    [Export] public int Amount { get; set; }
    [Export] public bool IsCost { get; set; } = false;
    [Export] public bool IsRefund { get; set; } = false;
}

[GlobalClass]
public partial class SummonEffect : Resource
{
    [Export] public string UnitId { get; set; }
    [Export] public int Duration { get; set; } = 3;
    [Export] public int MaxSummons { get; set; } = 1;
    [Export] public bool InheritStats { get; set; } = false;
    [Export] public float StatMultiplier { get; set; } = 1.0f;
}

[GlobalClass]
public partial class FieldEffect : Resource
{
    [Export] public FieldType Type { get; set; }
    [Export] public int Radius { get; set; } = 2;
    [Export] public int Duration { get; set; } = 3;
    public List<StatusEffectApplication> EffectsPerTurn { get; set; } = new();
    public List<StatModifierEffect> StatModifiersPerTurn { get; set; } = new();
}

public enum AbilityType
{
    Attack,
    Skill,
    Ultimate,
    Movement,
    Utility,
    Gadget,
    Mutation,
    Combo
}

public enum ScalingStat
{
    Might,
    Agility,
    Constitution,
    Intelligence,
    Willpower,
    Resonance,
    WeaponDamage
}

public enum StatType
{
    MaxHp,
    CurrentHp,
    Armor,
    Evasion,
    Accuracy,
    CritChance,
    CritDamage,
    MeleeDamage,
    RangedDamage,
    TechDamage,
    ResonanceDamage,
    MoveRange,
    ApMax,
    ApCurrent,
    Initiative,
    MentalResist,
    PhysicalResist,
    MutationResist
}

public enum ResourceType
{
    ActionPoints,
    ResonanceFragments,
    SignalPoints,
    Health,
    Shield
}

public enum PositionEffectType
{
    Teleport,
    Push,
    Pull,
    Swap,
    Knockback,
    Slide
}

public enum FieldType
{
    Smoke,
    Fire,
    Poison,
    Electric,
    Resonance,
    NullField,
    Healing,
    Stasis
}