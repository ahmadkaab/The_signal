using Godot;
using System.Collections.Generic;
using TheSignal.Core;

namespace TheSignal.Data;

[GlobalClass]
public partial class ZoneResource : Resource
{
    [Export] public string ZoneId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Texture2D MapIcon { get; set; }
    [Export] public Texture2D BackgroundImage { get; set; }
    [Export] public ZoneType Type { get; set; }
    [Export] public int Sector { get; set; }
    [Export] public Vector2I MapPosition { get; set; }

    [ExportGroup("Discovery")]
    [Export] public int FirstDiscoveryXp { get; set; } = 100;
    [Export] public bool InitiallyDiscovered { get; set; } = false;
    public List<string> RequiredFlagsForDiscovery { get; set; } = new();
    public List<string> RequiredQuestsCompleted { get; set; } = new();

    [ExportGroup("Connections")]
    public List<ZoneConnection> Connections { get; set; } = new();

    [ExportGroup("Corruption")]
    [Export] public float BaseCorruptionLevel { get; set; } = 0f;
    [Export] public float CorruptionDriftPerHour { get; set; } = 0.1f;
    [Export] public float CleanseRate { get; set; } = 1f;
    [Export] public float CorruptRate { get; set; } = 1f;
    [Export] public bool CanBeCleansed { get; set; } = true;
    [Export] public bool CanBeCorrupted { get; set; } = true;

    [ExportGroup("Encounters")]
    public List<EncounterEntry> FixedEncounters { get; set; } = new();
    public List<EncounterTableEntry> RandomEncounterTables { get; set; } = new();
    [Export] public float RandomEncounterChancePerHour { get; set; } = 0.15f;
    [Export] public int MaxRandomEncountersPerVisit { get; set; } = 3;

    [ExportGroup("Events")]
    public List<ZoneEvent> Events { get; set; } = new();

    [ExportGroup("Rewards")]
    public List<ZoneReward> DiscoveryRewards { get; set; } = new();
    public List<ZoneReward> ClearRewards { get; set; } = new();

    [ExportGroup("Local Zone Scene")]
    [Export] public PackedScene LocalZoneScene { get; set; }
    public List<ZoneProp> Props { get; set; } = new();
    public List<ZoneNpc> Npcs { get; set; } = new();
    public List<ZoneInteractable> Interactables { get; set; } = new();
    [Export] public string AmbientMusic { get; set; }
    [Export] public Color AmbientLight { get; set; } = new Color(0.2f, 0.2f, 0.3f);
    [Export] public Godot.Environment EnvironmentOverride { get; set; }
}

[GlobalClass]
public partial class ZoneConnection : Resource
{
    [Export] public string ToZoneId { get; set; }
    [Export] public int FuelCost { get; set; } = 10;
    [Export] public int ScrapCost { get; set; } = 0;
    [Export] public TravelType TravelType { get; set; } = TravelType.Normal;
    [Export] public bool InitiallyLocked { get; set; } = false;
    public List<string> UnlockFlags { get; set; } = new();
    public List<string> UnlockQuests { get; set; } = new();
    [Export] public int MinLevel { get; set; } = 1;
    [Export] public FactionId RequiredFaction { get; set; } = FactionId.Purified;
    [Export] public int MinFactionRep { get; set; } = 0;
    [Export] public bool OneWay { get; set; } = false;
    [Export] public float DangerLevel { get; set; } = 1f;
}

[GlobalClass]
public partial class EncounterEntry : Resource
{
    [Export] public string EncounterId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Vector2I GridPosition { get; set; }
    public List<string> EnemyIds { get; set; } = new();
    [Export] public int EnemyCount { get; set; } = 3;
    [Export] public int MinLevel { get; set; } = 1;
    [Export] public int MaxLevel { get; set; } = 5;
    [Export] public bool OnceOnly { get; set; } = true;
    [Export] public bool Mandatory { get; set; } = false;
    public List<string> RequiredFlags { get; set; } = new();
    public List<EncounterReward> Rewards { get; set; } = new();
    [Export] public string PreCombatDialogue { get; set; }
    [Export] public string PostCombatDialogue { get; set; }
}

[GlobalClass]
public partial class EncounterTableEntry : Resource
{
    [Export] public string TableId { get; set; }
    [Export] public string DisplayName { get; set; }
    public List<EncounterEntry> PossibleEncounters { get; set; } = new();
    public List<float> Weights { get; set; } = new();
    [Export] public int MinLevel { get; set; } = 1;
    [Export] public int MaxLevel { get; set; } = 10;
    public Dictionary<string, float> ConditionModifiers { get; set; } = new(); // flag -> weight multiplier
}

[GlobalClass]
public partial class EncounterReward : Resource
{
    [Export] public string ItemId { get; set; }
    [Export] public int MinCount { get; set; } = 1;
    [Export] public int MaxCount { get; set; } = 1;
    [Export] public float Weight { get; set; } = 1f;
    [Export] public bool Guaranteed { get; set; } = false;
}

[GlobalClass]
public partial class ZoneEvent : Resource
{
    [Export] public string EventId { get; set; }
    [Export] public string Title { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Texture2D EventImage { get; set; }
    [Export] public EventType Type { get; set; }
    [Export] public float Weight { get; set; } = 1f;
    [Export] public bool Repeatable { get; set; } = false;
    [Export] public int CooldownHours { get; set; } = 24;
    public List<string> RequiredFlags { get; set; } = new();
    public List<string> ForbiddenFlags { get; set; } = new();
    public List<string> RequiredZonesCleared { get; set; } = new();
    public List<EventChoice> Choices { get; set; } = new();
}

[GlobalClass]
public partial class EventChoice : Resource
{
    [Export] public string Text { get; set; }
    [Export] public string Description { get; set; }
    [Export] public string RequiredSkill { get; set; }
    [Export] public int RequiredSkillLevel { get; set; }
    public List<string> RequiredFlags { get; set; } = new();
    public List<string> ForbiddenFlags { get; set; } = new();
    public List<EventOutcome> Outcomes { get; set; } = new();
    [Export] public float SuccessChance { get; set; } = 1f;
    public List<EventOutcome> FailureOutcomes { get; set; } = new();
}

[GlobalClass]
public partial class EventOutcome : Resource
{
    [Export] public OutcomeType Type { get; set; }
    [Export] public string Value { get; set; } // ItemId, FlagName, FactionId, etc.
    [Export] public int Amount { get; set; }
    [Export] public float Chance { get; set; } = 1f;
    [Export] public string Description { get; set; }
}

[GlobalClass]
public partial class ZoneReward : Resource
{
    [Export] public string ItemId { get; set; }
    [Export] public int Count { get; set; } = 1;
    [Export] public int Xp { get; set; } = 0;
    [Export] public int SignalPoints { get; set; } = 0;
    [Export] public int ResonanceFragments { get; set; } = 0;
    [Export] public int Scrap { get; set; } = 0;
    public Dictionary<FactionId, int> FactionRep { get; set; } = new();
}

[GlobalClass]
public partial class ZoneProp : Resource
{
    [Export] public string PropId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public Vector3 Rotation { get; set; }
    [Export] public Vector3 Scale { get; set; } = Vector3.One;
    [Export] public bool IsInteractable { get; set; } = false;
    [Export] public string InteractableId { get; set; }
    [Export] public bool IsDestructible { get; set; } = false;
    [Export] public int Health { get; set; } = 10;
    public List<string> DestroyedFlags { get; set; } = new();
}

[GlobalClass]
public partial class ZoneNpc : Resource
{
    [Export] public string NpcId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public Vector3 Rotation { get; set; }
    public List<string> DialogueNodes { get; set; } = new();
    public List<string> QuestIds { get; set; } = new();
    [Export] public FactionId Faction { get; set; } = FactionId.SignalWalkers;
    [Export] public bool IsVendor { get; set; } = false;
    public List<string> VendorItems { get; set; } = new();
    [Export] public string Schedule { get; set; } // "day", "night", "always"
}

[GlobalClass]
public partial class ZoneInteractable : Resource
{
    [Export] public string InteractableId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public InteractableType Type { get; set; }
    [Export] public string LootTableId { get; set; }
    [Export] public string RequiredKeyItem { get; set; }
    [Export] public int RequiredSkillLevel { get; set; }
    [Export] public SkillType RequiredSkill { get; set; }
    [Export] public bool OnceOnly { get; set; } = true;
    public List<string> OnInteractFlags { get; set; } = new();
    [Export] public string DialogueNode { get; set; }
}

public enum ZoneType
{
    Wasteland,
    Ruins,
    Facility,
    Grove,
    Waystation,
    ScavengerCamp,
    PurifiedOutpost,
    RootedSanctuary,
    OracleRelay,
    SectorZero,
    Unique
}

public enum TravelType
{
    Normal,
    FastTravel,
    Underground,
    Airborne,
    Hazardous
}

public enum EventType
{
    Narrative,
    Encounter,
    Discovery,
    Hazard,
    Opportunity,
    Faction,
    Companion
}

public enum OutcomeType
{
    GainXp,
    GainSignalPoints,
    GainFragments,
    GainScrap,
    GainItem,
    GainFlag,
    RemoveFlag,
    ChangeFactionRep,
    ChangeCorruption,
    StartCombat,
    StartDialogue,
    UnlockZone,
    UnlockFastTravel,
    DamageHp,
    HealHp,
    AddStatusEffect,
    RemoveStatusEffect,
    SpawnNpc,
    RemoveNpc,
    ChangeZoneState
}

public enum InteractableType
{
    Container,
    Terminal,
    TerminalHack,
    MedicalStation,
    Fabricator,
    SignalBeacon,
    Shrine,
    CachedData,
    Anomaly,
    Door,
    Elevator
}

public enum SkillType
{
    Hacking,
    Engineering,
    Medicine,
    Biology,
    Resonance,
    Stealth,
    Persuasion,
    Intimidation,
    Perception,
    Survival
}