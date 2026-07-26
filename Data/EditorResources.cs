using Godot;
using Godot.Collections;
using TheSignal.Core;

namespace TheSignal.Data;

[GlobalClass]
public partial class SignalNodeResource : Resource
{
    [Export] public string NodeId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public SignalBranch Branch { get; set; }
    [Export] public int Tier { get; set; }
    [Export] public int SpCost { get; set; }
    [Export] public Array<string> Prerequisites { get; set; } = new();
    [Export] public Array<SynergyLink> SynergyLinks { get; set; } = new();
    [Export] public bool IsActive { get; set; }
    [Export] public string AbilityId { get; set; }
    [Export] public Array<PassiveEffect> PassiveEffects { get; set; } = new();
    [Export] public Texture2D Icon { get; set; }
}

[GlobalClass]
public partial class SynergyLink : Resource
{
    [Export] public string RequiredNodeId { get; set; }
    [Export] public string GrantedEffect { get; set; }
    [Export] public string Description { get; set; }
}

[GlobalClass]
public partial class PassiveEffect : Resource
{
    [Export] public string Stat { get; set; }
    [Export] public float FlatBonus { get; set; }
    [Export] public float PercentBonus { get; set; }
    [Export] public int Duration { get; set; }
    [Export] public string SourceId { get; set; }
}

[GlobalClass]
public partial class MutationResource : Resource
{
    [Export] public string MutationId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public string CorruptionDescription { get; set; }
    [Export] public MutationCategory Category { get; set; }
    [Export] public Array<StatModifier> Benefits { get; set; } = new();
    [Export] public Array<CorruptionCost> CorruptionCosts { get; set; } = new();
    [Export] public Texture2D Icon { get; set; }
    [Export] public PackedScene VisualEffect { get; set; }
}

[GlobalClass]
public partial class StatModifier : Resource
{
    [Export] public string Stat { get; set; }
    [Export] public float FlatBonus { get; set; }
    [Export] public float PercentBonus { get; set; }
    [Export] public bool IsPercent { get; set; }
}

[GlobalClass]
public partial class CorruptionCost : Resource
{
    [Export] public CorruptionType Type { get; set; }
    [Export] public CorruptionTrigger Trigger { get; set; }
    [Export] public string Description { get; set; }
    [Export] public int Severity { get; set; }
}

[GlobalClass]
public partial class CompanionSynergyResource : Resource
{
    [Export] public string CompanionId { get; set; }
    [Export] public string CompanionName { get; set; }
    [Export] public Array<int> LoyaltyThresholds { get; set; } = new Array<int> { 100, 300, 600, 1000, 1500 };
    [Export] public Array<SynergyNode> Nodes { get; set; } = new();
    [Export] public string UnlockDialogue { get; set; }
}

[GlobalClass]
public partial class SynergyNode : Resource
{
    [Export] public string Description { get; set; }
    [Export] public Array<Dictionary> PlayerEffects { get; set; } = new();
    [Export] public Array<Dictionary> CompanionEffects { get; set; } = new();
    [Export] public string DualAbilityId { get; set; }
}

[GlobalClass]
public partial class ZoneEventResource : Resource
{
    [Export] public string EventId { get; set; }
    [Export] public string Title { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Texture2D EventImage { get; set; }
    [Export] public ZoneEventType Type { get; set; }
    [Export] public float Weight { get; set; } = 1f;
    [Export] public bool Repeatable { get; set; } = false;
    [Export] public int CooldownHours { get; set; } = 24;
    [Export] public Array<string> RequiredFlags { get; set; } = new();
    [Export] public Array<string> ForbiddenFlags { get; set; } = new();
    [Export] public Array<string> RequiredZonesCleared { get; set; } = new();
    [Export] public Array<ZoneEventChoice> Choices { get; set; } = new();
}

[GlobalClass]
public partial class ZoneEventChoice : Resource
{
    [Export] public string Text { get; set; }
    [Export] public string Description { get; set; }
    [Export] public string RequiredSkill { get; set; }
    [Export] public int RequiredSkillLevel { get; set; }
    [Export] public Array<string> RequiredFlags { get; set; } = new();
    [Export] public Array<string> ForbiddenFlags { get; set; } = new();
    [Export] public Array<EventOutcome> Outcomes { get; set; } = new();
    [Export] public float SuccessChance { get; set; } = 1f;
    [Export] public Array<EventOutcome> FailureOutcomes { get; set; } = new();
}

public enum ZoneEventType
{
    Narrative,
    Encounter,
    Discovery,
    Hazard,
    Opportunity,
    Faction,
    Companion
}

public enum ZoneEventOutcomeType
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