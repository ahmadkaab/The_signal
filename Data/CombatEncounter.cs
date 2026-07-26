using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Systems;

namespace TheSignal.Data;

[GlobalClass]
public partial class CombatEncounter : Resource
{
    [Export] public string EncounterId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public int Difficulty { get; set; } = 1;
    [Export] public int RecommendedLevel { get; set; } = 1;

    public List<Vector2I> PlayerSpawnPositions { get; set; } = new();
    public List<EnemySpawn> EnemySpawns { get; set; } = new();
    public List<CoverPlacement> CoverLayout { get; set; } = new();

    public List<EncounterObjective> Objectives { get; set; } = new();

    [Export] public string VictoryDialogue { get; set; }
    [Export] public string DefeatDialogue { get; set; }

    public List<string> SpecialRules { get; set; } = new();
    [Export] public int TimeLimitTurns { get; set; } = 0;
    [Export] public bool AllowFlee { get; set; } = true;
    [Export] public bool IsBossEncounter { get; set; } = false;

    [Export] public string EncounterName { get => DisplayName; set => DisplayName = value; }
    [Export] public int MinLevel { get; set; } = 1;
    [Export] public int MaxLevel { get; set; } = 10;
    public List<EnemySpawnInfo> EnemyUnits { get; set; } = new();
    [Export] public QuestRewards Rewards { get; set; }
}

public class EnemySpawnInfo
{
    public UnitData UnitData { get; set; }
    public int Position { get; set; }
    public int SpawnDelay { get; set; }
}

[GlobalClass]
public partial class EnemySpawn : Resource
{
    [Export] public string EnemyId { get; set; }
    [Export] public Vector2I GridPosition { get; set; }
    [Export] public int Level { get; set; } = 1;
    [Export] public bool IsElite { get; set; } = false;
    public List<string> BonusAbilities { get; set; } = new();
}

[GlobalClass]
public partial class CoverPlacement : Resource
{
    [Export] public Vector2I Coord { get; set; }
    [Export] public CoverType Type { get; set; } = CoverType.Half;
}

[GlobalClass]
public partial class EncounterObjective : Resource
{
    [Export] public ObjectiveType Type { get; set; }
    [Export] public string Description { get; set; }
    [Export] public string TargetId { get; set; }
    [Export] public int RequiredCount { get; set; } = 1;
    [Export] public bool IsOptional { get; set; } = false;
}

[GlobalClass]
public partial class EncounterRewards : Resource
{
    [Export] public int Xp { get; set; } = 0;
    [Export] public int SignalPoints { get; set; } = 0;
    [Export] public int ResonanceFragments { get; set; } = 0;
    [Export] public int Scrap { get; set; } = 0;
    public List<RewardItem> Items { get; set; } = new();
    public Dictionary<FactionId, int> FactionRep { get; set; } = new();
}

[GlobalClass]
public partial class RewardItem : Resource
{
    [Export] public string ItemId { get; set; }
    [Export] public int Count { get; set; } = 1;
}

public enum ObjectiveType
{
    DefeatAllEnemies,
    DefeatSpecificEnemy,
    ReachLocation,
    SurviveTurns,
    ProtectUnit,
    InteractWithObject,
    CaptureZone
}