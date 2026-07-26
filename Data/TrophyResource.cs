using Godot;

namespace TheSignal.Data;

[GlobalClass]
public partial class TrophyResource : Resource
{
    [Export] public string TrophyId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public TrophyRarity Rarity { get; set; } = TrophyRarity.Bronze;
    [Export] public bool IsHidden { get; set; } = false;
    [Export] public bool HasProgress { get; set; } = false;
    [Export] public int ProgressMax { get; set; } = 1;
    [Export] public int XboxGamerScore { get; set; } = 10;
}

public enum TrophyRarity
{
    Bronze,
    Silver,
    Gold,
    Platinum
}