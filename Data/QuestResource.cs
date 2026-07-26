using Godot;
using TheSignal.Core;
using TheSignal.Data;

namespace TheSignal.Systems;

// ResourceRegistry is defined in Systems/ResourceRegistry.cs

[GlobalClass]
public partial class QuestResource : Resource
{
    [Export] public string QuestId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public QuestType Type { get; set; }
    [Export] public int MinLevel { get; set; } = 1;
    [Export] public string StartStage { get; set; }
    [Export] public Godot.Collections.Dictionary<string, QuestStage> Stages { get; set; } = new();
    [Export] public QuestPrerequisites Prerequisites { get; set; }
    [Export] public bool IsRepeatable { get; set; } = false;
    [Export] public FactionId Faction { get; set; }
    [Export] public int FactionRepReward { get; set; } = 0;
    
    // Alias for code that expects Title
    [Export] public string Title { get => DisplayName; set => DisplayName = value; }
}

[GlobalClass]
public partial class QuestStage : Resource
{
    [Export] public string StageId { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Godot.Collections.Array<QuestObjective> Objectives { get; set; } = new();
    [Export] public string NextStage { get; set; }
    [Export] public QuestRewards Rewards { get; set; }
    [Export] public Godot.Collections.Dictionary StageVariables { get; set; } = new();
}

[GlobalClass]
public partial class QuestObjective : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string Description { get; set; }
    [Export] public QuestObjectiveType Type { get; set; }
    [Export] public string EventType { get; set; }
    [Export] public int RequiredCount { get; set; } = 1;
    [Export] public Godot.Collections.Dictionary Conditions { get; set; } = new();
    [Export] public bool IsOptional { get; set; } = false;
    [Export] public bool IsHidden { get; set; } = false;
}

[GlobalClass]
public partial class QuestRewards : Resource
{
    [Export] public int Xp { get; set; } = 0;
    [Export] public int SignalPoints { get; set; } = 0;
    [Export] public int ResonanceFragments { get; set; } = 0;
    [Export] public Godot.Collections.Array<RewardItem> Items { get; set; } = new();
    [Export] public Godot.Collections.Array<string> UnlockQuests { get; set; } = new();
    [Export] public Godot.Collections.Array<string> UnlockAbilities { get; set; } = new();
    [Export] public Godot.Collections.Dictionary FactionRep { get; set; } = new();
    
    // Used by EncounterGenerator
    public int Scrap { get; set; }
}

[GlobalClass]
public partial class RewardItem : Resource
{
    [Export] public string ItemId { get; set; }
    [Export] public int Count { get; set; } = 1;
    [Export] public bool IsChoice { get; set; } = false;
}

[GlobalClass]
public partial class QuestPrerequisites : Resource, System.Collections.IEnumerable
{
    [Export] public Godot.Collections.Array<string> RequiredQuestsCompleted { get; set; } = new();
    [Export] public Godot.Collections.Array<string> RequiredQuestsFailed { get; set; } = new();
    [Export] public int MinLevel { get; set; } = 1;
    [Export] public Godot.Collections.Dictionary MinFactionRep { get; set; } = new();
    [Export] public Godot.Collections.Array<string> RequiredFlags { get; set; } = new();
    [Export] public Godot.Collections.Array<string> ForbiddenFlags { get; set; } = new();
    
    public System.Collections.IEnumerator GetEnumerator()
    {
        return RequiredQuestsCompleted.GetEnumerator();
    }
}

public enum QuestType
{
    Main,
    Side,
    Companion,
    Faction,
    Exploration,
    Daily,
    Hidden
}

public enum QuestObjectiveType
{
    Kill,
    Collect,
    ReachLocation,
    TalkToNpc,
    InteractWithObject,
    Survive,
    Defend,
    Hack,
    Scan,
    Choice,
    Wait
}
