using Godot;
using System.Collections.Generic;

namespace TheSignal.Core.Save;

public partial class SaveSlotData : Godot.RefCounted
{
    public string SlotName { get; set; }
    public int Version { get; set; } = 1;
    public System.DateTime SaveTime { get; set; }
    public int PlayTimeMinutes { get; set; }
    public int PlayerLevel { get; set; } = 1;
    public int PlayerResonance { get; set; }
    public string ZoneId { get; set; }
    public int QuestCount { get; set; }
    public int NGPlusLevel { get; set; }
    public string PlayerName { get; set; }
    public string ArchetypeId { get; set; }
    public int TotalPlayTimeSeconds { get; set; }
    
    // Save data payloads
    public Dictionary<string, object> PlayerData { get; set; } = new();
    public Dictionary<string, object> WorldData { get; set; } = new();
    public Dictionary<string, object> PartyData { get; set; } = new();
    public Dictionary<string, object> QuestData { get; set; } = new();

    // Phase 1 stubs
    public int PrestigeLevel { get; set; }
    public List<string> ActiveMutations { get; set; } = new();
    public List<string> EquippedItems { get; set; } = new();
    public Dictionary<string, int> CompanionSynergyRanks { get; set; } = new();
    public Dictionary<string, int> FactionReputation { get; set; } = new();
}
