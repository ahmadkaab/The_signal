using Godot;
using System.Collections.Generic;
using TheSignal.Core;

namespace TheSignal.Systems;

/// <summary>
/// C3: Faction War System — dynamic territory control, patrol spawns,
/// vendor stock shifts, reputation consequences.
/// </summary>
[GlobalClass]
public partial class FactionWarManager : Node
{
    public static FactionWarManager Instance { get; private set; }

    // Zone control state per faction
    private Dictionary<string, TerritoryState> _territoryStates = new();
    private Dictionary<FactionId, FactionState> _factionStates = new();
    
    // Patrol spawn pools
    private Dictionary<string, List<PatrolSpawn>> _patrolSpawns = new();
    
    // Vendor stock modifiers
    private Dictionary<string, VendorModifier> _vendorMods = new();

    // Events
    public event Action<string, FactionId> OnTerritoryChanged;
    public event Action<FactionId, int> OnReputationChanged;
    public event Action<string> OnFactionEventTriggered;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        InitializeFactions();
        InitializeTerritories();
    }

    private void InitializeFactions()
    {
        foreach (FactionId faction in new[] { FactionId.Purified, FactionId.Rooted, FactionId.Scavengers })
        {
            _factionStates[faction] = new FactionState
            {
                Faction = faction,
                Reputation = 0, // 0 = Neutral, -100 = Hostile, +100 = Allied
                InfluenceRadius = 1,
                MilitaryPower = 50,
                EconomyPower = 50,
                IsAtWar = new Dictionary<FactionId, bool>
                {
                    [FactionId.Purified] = faction == FactionId.Purified ? false : true,
                    [FactionId.Rooted] = faction == FactionId.Rooted ? false : true,
                    [FactionId.Scavengers] = faction == FactionId.Scavengers ? false : false // Scavs neutral
                }
            };
        }
    }

    private void InitializeTerritories()
    {
        // Default territory ownership
        SetTerritory("S09_GRAVEYARD", FactionId.Hostile, 0f); // Unsettled
        SetTerritory("S09_PURIFIED_CITADEL", FactionId.Purified, 1f);
        SetTerritory("S09_ROOTED_GROVE", FactionId.Rooted, 1f);
        SetTerritory("S09_SCAVENGER_FREEPORT", FactionId.Scavengers, 1f);
        
        SetTerritory("S08_ASH_PLAINS", FactionId.Hostile, 0.3f);
        SetTerritory("S08_SCORCHED_FOREST", FactionId.Hostile, 0.4f);
        SetTerritory("S08_SCRAP_MINES", FactionId.Scavengers, 0.7f);
        SetTerritory("S08_WASTELAND_FORT", FactionId.Purified, 0.8f);
        
        SetTerritory("S07_CRYSTAL_CAVERNS", FactionId.Hostile, 0.5f);
        SetTerritory("S07_CRYSTAL_SPIRES", FactionId.Rooted, 0.6f);
        SetTerritory("S07_RESONANCE_PIT", FactionId.Hostile, 0.2f);
        
        SetTerritory("S06_PURIFICATION_FORGE", FactionId.Purified, 0.9f);
        SetTerritory("S06_MUTATION_LAB", FactionId.Rooted, 0.8f);
        
        SetTerritory("S05_THE_BARRIER", FactionId.Hostile, 0.1f);
        SetTerritory("S05_SIGNAL_SPIRE", FactionId.Hostile, 0f);
    }

    public void SetTerritory(string zoneId, FactionId controllingFaction, float controlStrength)
    {
        _territoryStates[zoneId] = new TerritoryState
        {
            ZoneId = zoneId,
            ControllingFaction = controllingFaction,
            ControlStrength = Mathf.Clamp(controlStrength, 0f, 1f),
            LastChanged = GameManager.Instance?.TotalPlayTime ?? 0f
        };
    }

    // ========== REPUTATION ==========

    public int GetReputation(FactionId faction)
    {
        return _factionStates.TryGetValue(faction, out var state) ? state.Reputation : 0;
    }

    public string GetReputationLabel(FactionId faction)
    {
        int rep = GetReputation(faction);
        return rep switch
        {
            <= -80 => "Hated",
            <= -50 => "Hostile",
            <= -20 => "Unfriendly",
            < 20 => "Neutral",
            < 50 => "Friendly",
            < 80 => "Honored",
            _ => "Allied"
        };
    }

    public void AddReputation(FactionId faction, int delta)
    {
        if (!_factionStates.ContainsKey(faction)) return;
        
        var state = _factionStates[faction];
        int oldRep = state.Reputation;
        state.Reputation = Mathf.Clamp(state.Reputation + delta, -100, 100);
        
        GD.Print($"[Faction War] {faction} rep: {oldRep} -> {state.Reputation} ({GetReputationLabel(faction)})");
        OnReputationChanged?.Invoke(faction, state.Reputation);

        // Reputation consequences
        CheckReputationConsequences(faction, oldRep, state.Reputation);
    }

    private void CheckReputationConsequences(FactionId faction, int oldRep, int newRep)
    {
        // Crossed threshold changes
        if (oldRep >= 0 && newRep < 0)
            SetWarStatus(faction, true);
        else if (oldRep < 0 && newRep >= 0)
            SetWarStatus(faction, false);

        // Allied unlock
        if (oldRep < 80 && newRep >= 80)
            TriggerFactionEvent($"{faction}_allied");

        // Hostile unlock  
        if (oldRep > -80 && newRep <= -80)
            TriggerFactionEvent($"{faction}_hated");
    }

    // ========== TERRITORY CONTROL ==========

    public FactionId GetTerritoryControl(string zoneId)
    {
        return _territoryStates.TryGetValue(zoneId, out var state) 
            ? state.ControllingFaction 
            : FactionId.Unsettled;
    }

    public float GetControlStrength(string zoneId)
    {
        return _territoryStates.TryGetValue(zoneId, out var state) 
            ? state.ControlStrength 
            : 0f;
    }

    public void ShiftTerritoryControl(string zoneId, FactionId newOwner, float strength)
    {
        var oldOwner = GetTerritoryControl(zoneId);
        SetTerritory(zoneId, newOwner, strength);
        OnTerritoryChanged?.Invoke(zoneId, newOwner);

        // Update war score
        if (_factionStates.TryGetValue(newOwner, out var state))
            state.MilitaryPower += 5;

        GD.Print($"[Faction War] {zoneId}: {oldOwner} -> {newOwner} ({strength:P0})");
    }

    // ========== PATROL SPAWNS ==========

    public List<string> GetPatrolsForZone(string zoneId)
    {
        var owningFaction = GetTerritoryControl(zoneId);
        var results = new List<string>();

        // Base patrols based on zone control
        switch (owningFaction)
        {
            case FactionId.Purified:
                results.Add("purified_patrol");
                if (GetControlStrength(zoneId) > 0.7f)
                    results.Add("purified_elite_patrol");
                break;
            case FactionId.Rooted:
                results.Add("rooted_patrol");
                if (GetControlStrength(zoneId) > 0.7f)
                    results.Add("rooted_elite_patrol");
                break;
            case FactionId.Scavengers:
                results.Add("scavenger_patrol");
                break;
            default:
                results.Add("mutant_pack");
                if (GetControlStrength(zoneId) < 0.3f)
                    results.Add("rust_behemoth");
                break;
        }

        // Enemy factions may send incursion patrols into contested zones
        foreach (var kvp in _factionStates)
        {
            if (kvp.Key == owningFaction) continue;
            if (kvp.Value.IsAtWar.TryGetValue(owningFaction, out bool atWar) && atWar)
            {
                if (GD.Randf() < 0.3f) // 30% chance of incursion
                    results.Add($"{kvp.Key.ToString().ToLower()}_incursion");
            }
        }

        return results;
    }

    public float GetPatrolDensity(string zoneId)
    {
        float density = 0.3f; // base
        var control = GetControlStrength(zoneId);
        
        // High control = more patrols (security)
        density += control * 0.4f;
        
        // Contested zones get heavier patrols
        if (control > 0.3f && control < 0.7f)
            density += 0.3f;

        return Mathf.Clamp(density, 0.1f, 1.0f);
    }

    // ========== VENDOR STOCK ==========

    public VendorModifier GetVendorModifier(string vendorId)
    {
        return _vendorMods.TryGetValue(vendorId, out var mod) ? mod : new VendorModifier();
    }

    public void UpdateVendorStock(string zoneId)
    {
        var control = GetTerritoryControl(zoneId);
        
        foreach (var kvp in _vendorMods)
        {
            if (kvp.Value.ZoneId != zoneId) continue;
            kvp.Value.PriceMultiplier = GetReputation(control) switch
            {
                <= -50 => 2.5f,  // Hostile -> price gouging
                <= -20 => 1.5f,
                < 20 => 1.0f,    // Neutral -> base prices
                < 50 => 0.85f,   // Friendly -> discount
                < 80 => 0.7f,    // Honored
                _ => 0.5f        // Allied -> half price
            };
            kvp.Value.StockLevel = GetReputation(control) switch
            {
                <= -50 => 0.25f,
                <= -20 => 0.5f,
                < 20 => 1.0f,
                < 80 => 1.5f,
                _ => 2.0f
            };
        }
    }

    public void RegisterVendor(string vendorId, string zoneId, FactionId faction)
    {
        _vendorMods[vendorId] = new VendorModifier
        {
            VendorId = vendorId,
            ZoneId = zoneId,
            Faction = faction,
            PriceMultiplier = 1.0f,
            StockLevel = 1.0f
        };
    }

    // ========== FACTION EVENTS ==========

    public void TriggerFactionEvent(string eventId)
    {
        GD.Print($"[Faction War] Event triggered: {eventId}");
        OnFactionEventTriggered?.Invoke(eventId);

        switch (eventId)
        {
            case "purified_allied":
            case "rooted_allied":
            case "scavengers_allied":
                UnlockVendorStock(eventId.Replace("_allied", ""));
                break;
            case "purified_hated":
                TriggerPurifiedAssassins();
                break;
            case "rooted_hated":
                TriggerRootedSabotage();
                break;
            case "scavengers_hated":
                BlockScavengerTravel();
                break;
        }
    }

    private void SetWarStatus(FactionId faction, bool atWar)
    {
        if (!_factionStates.TryGetValue(faction, out var state)) return;
        state.IsAtWar[faction] = atWar;
        
        foreach (var other in new[] { FactionId.Purified, FactionId.Rooted, FactionId.Scavengers })
        {
            if (other != faction && _factionStates.TryGetValue(other, out var otherState))
                otherState.IsAtWar[faction] = atWar;
        }
    }

    private void UnlockVendorStock(string faction) 
    {
        GD.Print($"[Faction War] {faction} elite vendor stock unlocked!");
    }

    private void TriggerPurifiedAssassins()
    {
        GD.Print("[Faction War] Purified assassins dispatched. High-difficulty encounter incoming.");
    }

    private void TriggerRootedSabotage()
    {
        GD.Print("[Faction War] Rooted saboteurs damaging equipment. -1 fuel per travel until resolved.");
    }

    private void BlockScavengerTravel()
    {
        GD.Print("[Faction War] Scavenger Freeport closed. Cannot travel through S08_SCRAP_MINES.");
    }

    // ========== SAVE/LOAD ==========

    public Dictionary<string, object> SaveData()
    {
        var data = new Dictionary<string, object>();
        foreach (var kvp in _factionStates)
        {
            data[$"faction_{kvp.Key}"] = kvp.Value.Reputation;
        }
        return data;
    }

    public void LoadData(Dictionary<string, object> data)
    {
        if (data == null) return;
        foreach (var kvp in data)
        {
            if (kvp.Key.StartsWith("faction_") && kvp.Value is int rep)
            {
                if (Enum.TryParse<FactionId>(kvp.Key.Replace("faction_", ""), out var faction))
                {
                    if (_factionStates.TryGetValue(faction, out var state))
                        state.Reputation = rep;
                }
            }
        }
    }
}

public class TerritoryState
{
    public string ZoneId { get; set; }
    public FactionId ControllingFaction { get; set; } = FactionId.Unsettled;
    public float ControlStrength { get; set; } = 0.5f;
    public float LastChanged { get; set; }
}

public class FactionState
{
    public FactionId Faction { get; set; }
    public int Reputation { get; set; }
    public int InfluenceRadius { get; set; }
    public int MilitaryPower { get; set; }
    public int EconomyPower { get; set; }
    public Dictionary<FactionId, bool> IsAtWar { get; set; } = new();
}

public class PatrolSpawn
{
    public string EncounterId { get; set; }
    public string ZoneId { get; set; }
    public float Weight { get; set; } = 1f;
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 10;
    public bool IsBoss { get; set; } = false;
}

public class VendorModifier
{
    public string VendorId { get; set; }
    public string ZoneId { get; set; }
    public FactionId Faction { get; set; } = FactionId.Unsettled;
    public float PriceMultiplier { get; set; } = 1.0f;
    public float StockLevel { get; set; } = 1.0f;
}