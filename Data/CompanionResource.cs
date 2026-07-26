using Godot;
using System.Collections.Generic;

namespace TheSignal.Data;

[GlobalClass]
public partial class CompanionResource : Resource
{
    [Export] public string CompanionId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public string Archetype { get; set; }
    [Export] public CompanionFaction Faction { get; set; }
    [Export] public string StartingZone { get; set; }
    [Export] public int StartingLevel { get; set; } = 1;
    [Export] public int BaseMight { get; set; } = 8;
    [Export] public int BaseAgility { get; set; } = 8;
    [Export] public int BaseConstitution { get; set; } = 8;
    [Export] public int BaseIntelligence { get; set; } = 8;
    [Export] public int BaseWillpower { get; set; } = 8;
    [Export] public int BaseResonance { get; set; } = 5;
    [Export] public string[] StartingAbilities { get; set; }
    [Export] public string StartingWeapon { get; set; }
    [Export] public string PersonalQuestId { get; set; }
    [Export] public string[] SynergyPartners { get; set; }
    [Export] public string DialogueStartNode { get; set; }
    [Export] public Texture2D Portrait { get; set; }
    [Export] public PackedScene UnitScene { get; set; }
}

public enum CompanionFaction
{
    Purified,
    Rooted,
    Scavenger,
    Neutral,
    Walker
}