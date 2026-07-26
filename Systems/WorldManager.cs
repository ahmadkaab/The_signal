using Godot;
using System;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Core.Save;

namespace TheSignal.Systems;

public partial class WorldManager : Node
{
    public string CurrentZoneId { get; private set; }
    public int CurrentSector { get; private set; } = 9;
    public int WorldSeed { get; private set; }
    public Dictionary<string, ZoneState> ZoneStates { get; } = new();
    public Dictionary<FactionId, int> FactionReputation { get; } = new();
    public Dictionary<string, bool> WorldFlags { get; } = new();

    public void Initialize(int seed = 0)
    {
        WorldSeed = seed == 0 ? (int)DateTime.UtcNow.Ticks : seed;
        GD.Print($"World initialized with seed: {WorldSeed}");

        // Initialize faction reputations
        foreach (FactionId faction in Enum.GetValues(typeof(FactionId)))
        {
            FactionReputation[faction] = 0;
        }

        // Initialize starting zone
        EnterZone("sector_9_graveyard");
    }

    public void EnterZone(string zoneId)
    {
        CurrentZoneId = zoneId;
        if (!ZoneStates.ContainsKey(zoneId))
        {
            ZoneStates[zoneId] = new ZoneState { ZoneId = zoneId };
        }
        GD.Print($"Entered zone: {zoneId}");
    }

    public void ModifyFactionRep(FactionId faction, int amount)
    {
        if (!FactionReputation.ContainsKey(faction))
            FactionReputation[faction] = 0;
        FactionReputation[faction] = Mathf.Clamp(FactionReputation[faction] + amount, -100, 100);
    }

    public int GetFactionRep(FactionId faction)
    {
        return FactionReputation.GetValueOrDefault(faction, 0);
    }

    public void SetFlag(string flag, bool value)
    {
        WorldFlags[flag] = value;
    }

    public bool GetFlag(string flag)
    {
        return WorldFlags.GetValueOrDefault(flag, false);
    }

    public ZoneState GetZoneState(string zoneId)
    {
        return ZoneStates.GetValueOrDefault(zoneId, new ZoneState { ZoneId = zoneId });
    }

    public void SetZoneCorruption(string zoneId, float level)
    {
        var state = GetZoneState(zoneId);
        state.CorruptionLevel = Mathf.Clamp(level, -100, 100);
        state.IsCleansed = level <= -50;
        state.IsCorrupted = level >= 50;
    }

    public void ReturnToLastSafeZone()
    {
        GD.Print("Returning to last safe zone...");
        // Default to the initial zone if no safe zone is recorded
        var safeZone = "sector_9_graveyard";
        if (!string.IsNullOrEmpty(CurrentZoneId))
        {
            EnterZone(CurrentZoneId);
        }
        else
        {
            EnterZone(safeZone);
        }
    }

    public WorldSaveData GetSaveData()
    {
        var zones = new Dictionary<string, ZoneSaveData>();
        foreach (var kvp in ZoneStates)
        {
            zones[kvp.Key] = new ZoneSaveData
            {
                Discovered = kvp.Value.Discovered,
                Cleared = kvp.Value.Cleared,
                CorruptionLevel = (int)kvp.Value.CorruptionLevel,
                CompletedEvents = new List<string>(kvp.Value.CompletedEvents),
                State = new Dictionary<string, object>
                {
                    ["IsCleansed"] = kvp.Value.IsCleansed,
                    ["IsCorrupted"] = kvp.Value.IsCorrupted
                }
            };
        }

        var factions = new Dictionary<string, int>();
        foreach (var kvp in FactionReputation)
        {
            factions[kvp.Key.ToString()] = kvp.Value;
        }

        return new WorldSaveData
        {
            Zones = zones,
            FactionReputation = factions,
            CurrentSector = CurrentSector
        };
    }

    public void LoadSaveData(WorldSaveData data)
    {
        CurrentSector = data.CurrentSector;
        ZoneStates.Clear();
        foreach (var kvp in data.Zones)
        {
            ZoneStates[kvp.Key] = new ZoneState
            {
                ZoneId = kvp.Key,
                CorruptionLevel = kvp.Value.CorruptionLevel,
                Cleared = kvp.Value.Cleared,
                Discovered = kvp.Value.Discovered,
                CompletedEncounters = new HashSet<string>(),
                CollectedItems = new HashSet<string>()
            };
        }

        FactionReputation.Clear();
        foreach (var kvp in data.FactionReputation)
        {
            if (Enum.TryParse<FactionId>(kvp.Key, out var faction))
                FactionReputation[faction] = kvp.Value;
        }
    }
}
