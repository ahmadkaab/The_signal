using Godot;
using TheSignal.Core;

namespace TheSignal.Data;

[GlobalClass]
public partial class EnemyResource : Resource
{
    [Export] public string EnemyId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public EnemyArchetype Archetype { get; set; }
    [Export] public int Level { get; set; } = 1;
    [Export] public Texture2D Portrait { get; set; }
    [Export] public PackedScene UnitScene { get; set; }

    [ExportGroup("Base Stats")]
    [Export] public int BaseMight { get; set; } = 10;
    [Export] public int BaseAgility { get; set; } = 10;
    [Export] public int BaseConstitution { get; set; } = 10;
    [Export] public int BaseIntelligence { get; set; } = 10;
    [Export] public int BaseWillpower { get; set; } = 10;
    [Export] public int BaseResonance { get; set; } = 10;

    [ExportGroup("Derived Stats (Auto-calculated at runtime)")]
    [Export] public int BaseArmor { get; set; } = 0;
    [Export] public int BaseEvasion { get; set; } = 0;
    [Export] public float ResistPhysical { get; set; } = 0f;
    [Export] public float ResistResonance { get; set; } = 0f;
    [Export] public float ResistFire { get; set; } = 0f;
    [Export] public float ResistPoison { get; set; } = 0f;
    [Export] public float ResistShock { get; set; } = 0f;
    [Export] public float ResistPsychic { get; set; } = 0f;

    [ExportGroup("AI & Behavior")]
    [Export] public string BehaviorTree { get; set; }
    public List<string> AbilityIds { get; set; } = new();
    [Export] public AIPersonality Personality { get; set; }
    [Export] public int AggroRange { get; set; } = 8;
    [Export] public int PreferredRange { get; set; } = 3;

    [ExportGroup("Loot")]
    public List<LootEntry> LootTable { get; set; } = new();
    [Export] public int XpReward { get; set; } = 50;
    [Export] public int ScrapReward { get; set; } = 10;
    [Export] public int ResonanceFragmentReward { get; set; } = 0;
}

public enum EnemyArchetype
{
    Grunt,
    Elite,
    Champion,
    Boss,
    Turret,
    Drone,
    Beast,
    Mutant,
    Purified,
    Rooted,
    Scavenger,
    Oracle
}

public enum AIPersonality
{
    Aggressive,
    Defensive,
    Tactical,
    Berserker,
    Support,
    Sniper,
    Flanker,
    Opportunistic
}