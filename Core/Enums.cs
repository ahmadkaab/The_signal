namespace TheSignal.Core;

public enum SignalBranch
{
    Resonance,
    Biology,
    Technology,
    Stealth,
    Leadership
}

public enum MutationCategory
{
    Offense,
    Defense,
    Utility,
    Ultimate
}

public enum CorruptionTrigger
{
    OnHit,
    OnTurnStart,
    OnAbilityUse,
    Passive
}

public enum CorruptionType
{
    Stun,
    StatPenalty,
    Restriction,
    Visual
}

public enum QuestState
{
    Hidden,
    Available,
    Active,
    Complete,
    TurnedIn,
    Failed
}

public enum FactionId
{
    Purified,
    Rooted,
    Scavengers,
    SignalWalkers,
    Silent,
    Hostile,
    Unsettled
}

public enum GameState
{
    MainMenu,
    Exploration,
    Combat,
    Dialogue,
    Inventory,
    CharacterSheet,
    QuestLog,
    SectorMap,
    Paused
}

public enum CombatPhase
{
    Setup,
    PlayerTurn,
    EnemyTurn,
    AllyTurn,
    Victory,
    Defeat,
    Flee
}

public enum UnitType
{
    Player,
    Companion,
    Enemy,
    Neutral,
    Deployable
}

public enum DamageType
{
    Physical,
    Resonance,
    Fire,
    Poison,
    Shock,
    Psychic,
    True
}

public enum CoverType
{
    None,
    Half,
    Full
}

public enum AbilityTargetType
{
    Self,
    SingleEnemy,
    SingleAlly,
    SingleAny,
    AreaCircle,
    AreaCone,
    Line,
    Global
}

public enum StatusEffectType
{
    None,
    Bleed,
    Poison,
    Burn,
    Shock,
    Stun,
    Suppressed,
    Concealed,
    Harmonized,
    Adapted,
    Staggered,
    Regeneration,
    Shielded,
    Corroded,
    Fractured,
    Resonating,
    Phased,
    Rooted,
    Feared,
    Controlled,
    Steadfast,
    Marked,
    Sampled
}