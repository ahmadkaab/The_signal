using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Systems;

namespace TheSignal.Systems;

public partial class SectorMapManager : Node
{
    public static SectorMapManager Instance { get; private set; }

    public Dictionary<string, ZoneState> ZoneStates { get; } = new();
    public List<SectorConnection> Connections { get; } = new();
    public Dictionary<string, ZoneResource> ZoneResources { get; } = new();

    public string CurrentZoneId { get; private set; }
    public int Fuel { get; set; } = 100;
    public int MaxFuel { get; private set; } = 100;
    public int Scrap { get; private set; } = 0;

    public event Action<string> OnZoneChanged;
    public event Action<string> OnZoneDiscovered;
    public event Action<string, float> OnCorruptionChanged;
    public event Action<int> OnFuelChanged;
    public event Action<int> OnScrapChanged;
    public event Action<string> OnZoneCleansed;
    public event Action<string> OnZoneCorrupted;

    public void Initialize()
    {
        LoadZoneResources();
        BuildConnections();
        InitializeZoneStates();
    }

    public override void _Ready()
    {
        Instance = this;
        LoadZoneResources();
        BuildConnections();
        InitializeZoneStates();
    }

    private void LoadZoneResources()
    {
        var files = DirAccess.GetFilesAt("res://Data/Zones/");
        foreach (var file in files)
        {
            if (file.EndsWith(".tres"))
            {
                var resource = GD.Load<ZoneResource>($"res://Data/Zones/{file}");
                if (resource != null)
                {
                    ZoneResources[resource.ZoneId] = resource;
                }
            }
        }
        GD.Print($"Loaded {ZoneResources.Count} zone resources");
    }

    private void BuildConnections()
    {
        Connections.Clear();
        foreach (var kvp in ZoneResources)
        {
            var zone = kvp.Value;
            foreach (var conn in zone.Connections)
            {
                var sectorConn = new SectorConnection
                {
                    FromZoneId = zone.ZoneId,
                    ToZoneId = conn.ToZoneId,
                    FuelCost = conn.FuelCost,
                    ScrapCost = conn.ScrapCost,
                    IsUnlocked = !conn.InitiallyLocked
                };
                Connections.Add(sectorConn);
            }
        }
    }

    private void InitializeZoneStates()
    {
        ZoneStates.Clear();
        foreach (var kvp in ZoneResources)
        {
            var zone = kvp.Value;
            var state = new ZoneState
            {
                ZoneId = zone.ZoneId,
                ZoneResource = zone,
                Discovered = zone.InitiallyDiscovered,
                CorruptionLevel = zone.BaseCorruptionLevel,
                Visited = false
            };

            if (zone.InitiallyDiscovered)
            {
                state.Discovered = true;
            }

            ZoneStates[zone.ZoneId] = state;
        }
    }

    public bool CanTravelTo(string fromZoneId, string toZoneId)
    {
        var conn = Connections.Find(c => c.FromZoneId == fromZoneId && c.ToZoneId == toZoneId);
        if (conn == null) return false;
        if (!conn.IsUnlocked) return false;
        if (Fuel < conn.FuelCost) return false;
        if (Scrap < conn.ScrapCost) return false;

        var toState = ZoneStates.GetValueOrDefault(toZoneId);
        if (toState == null) return false;

        // Check unlock requirements
        var toZone = ZoneResources.GetValueOrDefault(toZoneId);
        if (toZone == null) return false;

        foreach (var conn2 in toZone.Connections)
        {
            if (conn2.ToZoneId == fromZoneId)
            {
                if (conn2.InitiallyLocked)
                {
                    foreach (var flag in conn2.UnlockFlags)
                    {
                        if (!GameManager.Instance.WorldManager.GetFlag(flag))
                            return false;
                    }
                    foreach (var quest in conn2.UnlockQuests)
                    {
                        // Check quest completion
                    }
                    if (GameManager.Instance.Player.Level < conn2.MinLevel)
                        return false;
                }
            }
        }

        return true;
    }

    public void TravelTo(string toZoneId)
    {
        if (CurrentZoneId == toZoneId) return;
        if (!CanTravelTo(CurrentZoneId, toZoneId)) return;

        var conn = Connections.Find(c => c.FromZoneId == CurrentZoneId && c.ToZoneId == toZoneId);
        if (conn == null) return;

        // Leave current zone
        if (!string.IsNullOrEmpty(CurrentZoneId))
        {
            LeaveZone(CurrentZoneId);
        }

        // Consume resources
        Fuel -= conn.FuelCost;
        Scrap -= conn.ScrapCost;
        OnFuelChanged?.Invoke(Fuel);
        OnScrapChanged?.Invoke(Scrap);

        // Enter new zone
        EnterZone(toZoneId);
    }

    private void LeaveZone(string zoneId)
    {
        if (ZoneStates.TryGetValue(zoneId, out var state))
        {
            // Apply corruption drift over time
            var zone = ZoneResources.GetValueOrDefault(zoneId);
            if (zone != null)
            {
                state.CorruptionLevel += zone.CorruptionDriftPerHour;
                state.CorruptionLevel = Mathf.Clamp(state.CorruptionLevel, -100, 100);
                OnCorruptionChanged?.Invoke(zoneId, state.CorruptionLevel);
            }
            state.Visited = true;
        }
    }

    private void EnterZone(string zoneId)
    {
        CurrentZoneId = zoneId;

        if (ZoneStates.TryGetValue(zoneId, out var state))
        {
            if (!state.Discovered)
            {
                DiscoverZone(zoneId);
            }
            else
            {
                state.Visited = true;
            }

            GameManager.Instance.WorldManager.EnterZone(zoneId);
            OnZoneChanged?.Invoke(zoneId);
        }
    }

    public void DiscoverZone(string zoneId)
    {
        if (ZoneStates.TryGetValue(zoneId, out var state))
        {
            state.Discovered = true;
            state.Visited = true;

            var zone = state.ZoneResource;
            if (zone != null)
            {
                GameManager.Instance.Player.GainXp(zone.FirstDiscoveryXp);

                // Grant discovery rewards
                foreach (var reward in zone.DiscoveryRewards)
                {
                    // Grant item/xp/etc
                }
            }

            OnZoneDiscovered?.Invoke(zoneId);
            GD.Print($"Zone discovered: {zone?.DisplayName ?? zoneId}");

            // Unlock connections
            UpdateConnectionUnlocks();
        }
    }

    public void UpdateConnectionUnlocks()
    {
        foreach (var conn in Connections)
        {
            if (conn.IsUnlocked) continue;

            var fromState = ZoneStates.GetValueOrDefault(conn.FromZoneId);
            var toState = ZoneStates.GetValueOrDefault(conn.ToZoneId);
            if (fromState == null || toState == null) continue;

            var fromZone = ZoneResources.GetValueOrDefault(conn.FromZoneId);
            if (fromZone == null) continue;

            foreach (var zc in fromZone.Connections)
            {
                if (zc.ToZoneId == conn.ToZoneId)
                {
                    bool canUnlock = true;
                    foreach (var flag in zc.UnlockFlags)
                    {
                        if (!GameManager.Instance.WorldManager.GetFlag(flag))
                            canUnlock = false;
                    }
                    if (GameManager.Instance.Player.Level < zc.MinLevel)
                        canUnlock = false;

                    if (canUnlock)
                    {
                        conn.IsUnlocked = true;
                        GD.Print($"Connection unlocked: {conn.FromZoneId} -> {conn.ToZoneId}");
                    }
                }
            }
        }
    }

    public void ModifyCorruption(string zoneId, float amount)
    {
        if (ZoneStates.TryGetValue(zoneId, out var state))
        {
            var zone = ZoneResources.GetValueOrDefault(zoneId);
            if (zone == null) return;

            if (amount > 0 && !zone.CanBeCorrupted) return;
            if (amount < 0 && !zone.CanBeCleansed) return;

            state.CorruptionLevel = Mathf.Clamp(state.CorruptionLevel + amount, -100, 100);
            OnCorruptionChanged?.Invoke(zoneId, state.CorruptionLevel);

            // Check for state transitions
            if (state.CorruptionLevel <= -50 && !state.IsCleansed)
            {
                state.IsCleansed = true;
                state.IsCorrupted = false;
                OnZoneCleansed?.Invoke(zoneId);
            }
            else if (state.CorruptionLevel >= 50 && !state.IsCorrupted)
            {
                state.IsCorrupted = true;
                state.IsCleansed = false;
                OnZoneCorrupted?.Invoke(zoneId);
            }
        }
    }

    public void Refuel(int amount)
    {
        Fuel = Mathf.Min(Fuel + amount, MaxFuel);
        OnFuelChanged?.Invoke(Fuel);
    }

    public void AddScrap(int amount)
    {
        Scrap += amount;
        OnScrapChanged?.Invoke(Scrap);
    }

    public ZoneState GetZoneState(string zoneId)
    {
        return ZoneStates.GetValueOrDefault(zoneId);
    }

    public SectorMapSaveData GetSaveData()
    {
        var zones = new Dictionary<string, ZoneStateData>();
        foreach (var kvp in ZoneStates)
        {
            zones[kvp.Key] = new ZoneStateData
            {
                Discovered = kvp.Value.Discovered,
                Cleared = kvp.Value.Cleared,
                CorruptionLevel = kvp.Value.CorruptionLevel,
                Visited = kvp.Value.Visited,
                CompletedEncounters = new List<string>(kvp.Value.CompletedEncounters),
                CompletedEvents = new List<string>(kvp.Value.CompletedEvents),
                EventCooldowns = new Dictionary<string, long>(kvp.Value.EventCooldowns)
            };
        }

        return new SectorMapSaveData
        {
            CurrentZoneId = CurrentZoneId,
            Fuel = Fuel,
            MaxFuel = MaxFuel,
            Scrap = Scrap,
            Zones = zones
        };
    }

    public void LoadSaveData(SectorMapSaveData data)
    {
        CurrentZoneId = data.CurrentZoneId;
        Fuel = data.Fuel;
        MaxFuel = data.MaxFuel;
        Scrap = data.Scrap;

        ZoneStates.Clear();
        foreach (var kvp in data.Zones)
        {
            var resource = ZoneResources.GetValueOrDefault(kvp.Key);
            ZoneStates[kvp.Key] = new ZoneState
            {
                ZoneId = kvp.Key,
                ZoneResource = resource,
                Discovered = kvp.Value.Discovered,
                Cleared = kvp.Value.Cleared,
                CorruptionLevel = kvp.Value.CorruptionLevel,
                Visited = kvp.Value.Visited,
                CompletedEncounters = new HashSet<string>(kvp.Value.CompletedEncounters),
                CompletedEvents = new HashSet<string>(kvp.Value.CompletedEvents),
                EventCooldowns = new Dictionary<string, long>(kvp.Value.EventCooldowns)
            };
        }

        BuildConnections();
        UpdateConnectionUnlocks();
    }
}

public class SectorConnection
{
    public string FromZoneId { get; set; }
    public string ToZoneId { get; set; }
    public int FuelCost { get; set; }
    public int ScrapCost { get; set; }
    public bool IsUnlocked { get; set; }
}

public class ZoneState
{
    public string ZoneId { get; set; }
    public ZoneResource ZoneResource { get; set; }
    public bool Discovered { get; set; }
    public bool Cleared { get; set; }
    public bool Visited { get; set; }
    public float CorruptionLevel { get; set; }
    public bool IsCleansed { get; set; }
    public bool IsCorrupted { get; set; }
    public HashSet<string> CompletedEncounters { get; set; } = new();
    public HashSet<string> CompletedEvents { get; set; } = new();
    public Dictionary<string, long> EventCooldowns { get; set; } = new();
    public bool HasActiveEncounter { get; set; }
    public bool HasActiveQuest { get; set; }
    public HashSet<string> CollectedItems { get; set; } = new();
}

public class SectorMapSaveData
{
    public string CurrentZoneId { get; set; }
    public int Fuel { get; set; }
    public int MaxFuel { get; set; }
    public int Scrap { get; set; }
    public Dictionary<string, ZoneStateData> Zones { get; set; } = new();
}

public class ZoneStateData
{
    public bool Discovered { get; set; }
    public bool Cleared { get; set; }
    public float CorruptionLevel { get; set; }
    public bool Visited { get; set; }
    public List<string> CompletedEncounters { get; set; } = new();
    public List<string> CompletedEvents { get; set; } = new();
    public Dictionary<string, long> EventCooldowns { get; set; } = new();
    public bool Cleansed { get; set; }
    public bool Corrupted { get; set; }
    public List<string> CollectedItems { get; set; } = new();
}

public delegate void ZoneEventDelegate(string zoneId);
public delegate void CorruptionDelegate(string zoneId, float level);
public delegate void ResourceDelegate(int value);