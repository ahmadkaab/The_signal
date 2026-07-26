using Godot;
using System.Collections.Generic;
using TheSignal.Core.Save;

namespace TheSignal.Core;

public class Party
{
    public Godot.Collections.Array<string> ActiveCompanionIds { get; set; } = new();
    public Godot.Collections.Array<string> AvailableCompanionIds { get; set; } = new();
    public int MaxActiveSize { get; set; } = 4;
    
    public void Initialize() 
    { 
        ActiveCompanionIds.Clear();
        ActiveCompanionIds.Add("player");
    }
    
    public void AddActiveCompanion(string id, bool addToAvailable) 
    { 
        if (ActiveCompanionIds.Count < MaxActiveSize)
            ActiveCompanionIds.Add(id);
        if (addToAvailable && !AvailableCompanionIds.Contains(id))
            AvailableCompanionIds.Add(id);
    }
    
    public PartySaveData GetSaveData()
    {
        return new PartySaveData { ActiveCompanionIds = new List<string>(ActiveCompanionIds) };
    }
    
    public void LoadSaveData(PartySaveData data)
    {
        // Stub
    }
}
