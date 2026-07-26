using Godot;
using System.Collections.Generic;
using TheSignal.Core;

namespace TheSignal.Data;

[GlobalClass]
public partial class UnitData : Resource
{
    [Export] public string UnitId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public UnitType Type { get; set; }
    [Export] public string EnemyId { get; set; }
    [Export] public string CompanionId { get; set; }

    [ExportGroup("Base Stats")]
    [Export] public int MaxHp { get; set; } = 100;
    [Export] public int CurrentHp { get; set; } = 100;
    [Export] public int MaxAp { get; set; } = 6;
    [Export] public int CurrentAp { get; set; } = 6;
    [Export] public int Armor { get; set; } = 0;
    [Export] public int Evasion { get; set; } = 0;
    [Export] public int Accuracy { get; set; } = 80;
    [Export] public int CritChance { get; set; } = 5;
    [Export] public int CritDamage { get; set; } = 50;
    [Export] public int Initiative { get; set; } = 10;
    [Export] public int MoveRange { get; set; } = 3;
    [Export] public int WeaponDamage { get; set; } = 5;
    [Export] public DamageType WeaponDamageType { get; set; } = DamageType.Physical;

    [ExportGroup("Resistances")]
    [Export] public float ResistPhysical { get; set; } = 0f;
    [Export] public float ResistResonance { get; set; } = 0f;
    [Export] public float ResistFire { get; set; } = 0f;
    [Export] public float ResistPoison { get; set; } = 0f;
    [Export] public float ResistShock { get; set; } = 0f;
    [Export] public float ResistPsychic { get; set; } = 0f;

    [ExportGroup("Abilities")]
    public List<string> AbilityIds { get; set; } = new();

    [ExportGroup("Position")]
    [Export] public Vector2I GridPosition { get; set; }

    [ExportGroup("Visual")]
    [Export] public PackedScene UnitScene { get; set; }
    [Export] public Color TeamColor { get; set; } = Colors.White;

    [ExportGroup("AI")]
    [Export] public string BehaviorTree { get; set; }
    [Export] public AIPersonality Personality { get; set; }
    [Export] public int AggroRange { get; set; } = 8;
    [Export] public int PreferredRange { get; set; } = 3;
}

[GlobalClass]
public partial class PlayerUnitData : UnitData
{
    public List<string> UnlockedSignalNodes { get; set; } = new();
    public List<string> EquippedMutations { get; set; } = new();
    [Export] public int Level { get; set; } = 1;
    [Export] public int CurrentXp { get; set; } = 0;
    [Export] public int SignalPoints { get; set; } = 0;
    [Export] public int ResonanceFragments { get; set; } = 0;
}

[GlobalClass]
public partial class CompanionUnitData : UnitData
{
    [Export] public string CompanionId { get; set; }
    [Export] public int Loyalty { get; set; } = 0;
    [Export] public int SynergyRank { get; set; } = 0;
    public List<string> UnlockedComboAbilities { get; set; } = new();
}

[GlobalClass]
public partial class EnemyUnitData : UnitData
{
    [Export] public int Level { get; set; } = 1;
    [Export] public int XpReward { get; set; } = 50;
    [Export] public int ScrapReward { get; set; } = 10;
    [Export] public int ResonanceFragmentReward { get; set; } = 0;
    public List<LootEntry> LootTable { get; set; } = new();
    [Export] public bool IsBoss { get; set; } = false;
}

[GlobalClass]
public partial class LootEntry : Resource
{
    [Export] public string ItemId { get; set; }
    [Export] public int MinCount { get; set; } = 1;
    [Export] public int MaxCount { get; set; } = 1;
    [Export] public float Weight { get; set; } = 1f;
    [Export] public int MinLevel { get; set; } = 1;
    [Export] public int MaxLevel { get; set; } = 99;
}