using Godot;
using System.Collections.Generic;
using System.Linq;
using TheSignal.Core;

namespace TheSignal.Systems;

/// <summary>
/// D3: Base Building — upgrade hubs (Waystation/Grove/Freeport),
/// passive bonuses, companion housing, research tree.
/// </summary>
[GlobalClass]
public partial class BaseBuildingManager : Node
{
    public static BaseBuildingManager Instance { get; private set; }

    // Each hub has its own upgrade tree
    private Dictionary<string, HubState> _hubs = new();
    private Dictionary<string, int> _resources = new(); // resourceId -> amount

    // Research tree
    private Dictionary<string, ResearchNode> _researchTree = new();
    private List<string> _completedResearch = new();

    // Passive bonuses from upgrades
    private Dictionary<string, float> _passiveBonuses = new();

    public event Action<string, int> OnHubUpgraded; // hubId, newLevel
    public event Action<string> OnResearchCompleted;
    public event Action<string, int> OnResourceChanged;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        InitializeHubs();
        InitializeResearchTree();
    }

    private void InitializeHubs()
    {
        // Waystation (Purified-aligned hub)
        _hubs["waystation"] = new HubState
        {
            HubId = "waystation",
            DisplayName = "Waystation",
            Faction = FactionId.Purified,
            CurrentLevel = 1,
            MaxLevel = 5,
            UpgradeCosts = new Dictionary<int, Dictionary<string, int>>
            {
                [2] = new() { { "scrap", 100 }, { "purified_essence", 2 } },
                [3] = new() { { "scrap", 250 }, { "purified_essence", 5 }, { "tech_scrap", 10 } },
                [4] = new() { { "scrap", 500 }, { "purified_essence", 10 }, { "vital_essence", 5 } },
                [5] = new() { { "scrap", 1000 }, { "purified_essence", 20 }, { "resonance_crystal", 5 } }
            },
            Buildings = new Dictionary<string, BuildingState>
            {
                ["fabricator"] = new() { BuildingId = "fabricator", CurrentLevel = 1, MaxLevel = 3, Category = "crafting" },
                ["medbay"] = new() { BuildingId = "medbay", CurrentLevel = 1, MaxLevel = 3, Category = "recovery" },
                ["signal_beacon"] = new() { BuildingId = "signal_beacon", CurrentLevel = 1, MaxLevel = 3, Category = "signal" },
                ["armory"] = new() { BuildingId = "armory", CurrentLevel = 0, MaxLevel = 3, Category = "combat" },
                ["quarters"] = new() { BuildingId = "quarters", CurrentLevel = 1, MaxLevel = 3, Category = "housing" }
            }
        };

        // Grove (Rooted-aligned hub)
        _hubs["grove"] = new HubState
        {
            HubId = "grove",
            DisplayName = "Verdant Grove",
            Faction = FactionId.Rooted,
            CurrentLevel = 1,
            MaxLevel = 5,
            UpgradeCosts = new Dictionary<int, Dictionary<string, int>>
            {
                [2] = new() { { "scrap", 100 }, { "bio_gel", 5 } },
                [3] = new() { { "scrap", 250 }, { "bio_gel", 10 }, { "vital_essence", 3 } },
                [4] = new() { { "scrap", 500 }, { "vital_essence", 8 }, { "resonance_crystal", 3 } },
                [5] = new() { { "scrap", 1000 }, { "bio_gel", 20 }, { "resonance_crystal", 8 } }
            },
            Buildings = new Dictionary<string, BuildingState>
            {
                ["communion_pool"] = new() { BuildingId = "communion_pool", CurrentLevel = 1, MaxLevel = 3, Category = "mutation" },
                ["shrine"] = new() { BuildingId = "shrine", CurrentLevel = 1, MaxLevel = 3, Category = "purification" },
                ["root_network"] = new() { BuildingId = "root_network", CurrentLevel = 1, MaxLevel = 3, Category = "knowledge" },
                ["hatchery"] = new() { BuildingId = "hatchery", CurrentLevel = 0, MaxLevel = 3, Category = "companion" },
                ["quarters"] = new() { BuildingId = "quarters", CurrentLevel = 1, MaxLevel = 3, Category = "housing" }
            }
        };

        // Freeport (Scavenger-aligned hub)
        _hubs["freeport"] = new HubState
        {
            HubId = "freeport",
            DisplayName = "Scavenger Freeport",
            Faction = FactionId.Scavengers,
            CurrentLevel = 1,
            MaxLevel = 5,
            UpgradeCosts = new Dictionary<int, Dictionary<string, int>>
            {
                [2] = new() { { "scrap", 150 } },
                [3] = new() { { "scrap", 300 }, { "tech_scrap", 10 } },
                [4] = new() { { "scrap", 600 }, { "tech_scrap", 20 }, { "vital_essence", 5 } },
                [5] = new() { { "scrap", 1200 }, { "tech_scrap", 30 }, { "resonance_crystal", 5 } }
            },
            Buildings = new Dictionary<string, BuildingState>
            {
                ["market"] = new() { BuildingId = "market", CurrentLevel = 1, MaxLevel = 3, Category = "trade" },
                ["garage"] = new() { BuildingId = "garage", CurrentLevel = 1, MaxLevel = 3, Category = "travel" },
                ["workshop"] = new() { BuildingId = "workshop", CurrentLevel = 0, MaxLevel = 3, Category = "crafting" },
                ["gambling_den"] = new() { BuildingId = "gambling_den", CurrentLevel = 0, MaxLevel = 3, Category = "risk" },
                ["quarters"] = new() { BuildingId = "quarters", CurrentLevel = 1, MaxLevel = 3, Category = "housing" }
            }
        };
    }

    // ========== HUB UPGRADES ==========

    public bool CanUpgradeHub(string hubId)
    {
        if (!_hubs.TryGetValue(hubId, out var hub)) return false;
        if (hub.CurrentLevel >= hub.MaxLevel) return false;
        
        var costs = hub.UpgradeCosts[hub.CurrentLevel + 1];
        foreach (var cost in costs)
        {
            if (!HasResource(cost.Key, cost.Value)) return false;
        }
        return true;
    }

    public void UpgradeHub(string hubId)
    {
        if (!CanUpgradeHub(hubId)) return;

        var hub = _hubs[hubId];
        var costs = hub.UpgradeCosts[hub.CurrentLevel + 1];

        foreach (var cost in costs)
            SpendResource(cost.Key, cost.Value);

        hub.CurrentLevel++;
        RecalculatePassiveBonuses();
        OnHubUpgraded?.Invoke(hubId, hub.CurrentLevel);
        GD.Print($"[BaseBuilding] {hub.DisplayName} upgraded to Level {hub.CurrentLevel}");
    }

    // ========== BUILDING UPGRADES ==========

    public bool CanUpgradeBuilding(string hubId, string buildingId)
    {
        if (!_hubs.TryGetValue(hubId, out var hub)) return false;
        if (!hub.Buildings.TryGetValue(buildingId, out var building)) return false;
        if (building.CurrentLevel >= building.MaxLevel) return false;

        int cost = building.CurrentLevel * 50 + 100;
        return HasResource("scrap", cost);
    }

    public void UpgradeBuilding(string hubId, string buildingId)
    {
        if (!CanUpgradeBuilding(hubId, buildingId)) return;

        var building = _hubs[hubId].Buildings[buildingId];
        int cost = building.CurrentLevel * 50 + 100;
        SpendResource("scrap", cost);

        building.CurrentLevel++;
        RecalculatePassiveBonuses();
        GD.Print($"[BaseBuilding] {buildingId} upgraded to Level {building.CurrentLevel}");
    }

    // ========== PASSIVE BONUSES ==========

    private void RecalculatePassiveBonuses()
    {
        _passiveBonuses.Clear();

        foreach (var hub in _hubs.Values)
        {
            foreach (var building in hub.Buildings.Values)
            {
                if (building.CurrentLevel == 0) continue;
                ApplyBuildingBonus(hub.HubId, building);
            }
        }

        // Hub-level bonuses
        if (_hubs.TryGetValue("waystation", out var ws))
        {
            _passiveBonuses["crafting_speed"] = ws.CurrentLevel * 0.1f;
            _passiveBonuses["purified_rep_gain"] = ws.CurrentLevel * 0.05f;
        }
        if (_hubs.TryGetValue("grove", out var gr))
        {
            _passiveBonuses["mutation_efficiency"] = gr.CurrentLevel * 0.1f;
            _passiveBonuses["corruption_resistance"] = gr.CurrentLevel * 0.02f;
        }
        if (_hubs.TryGetValue("freeport", out var fp))
        {
            _passiveBonuses["trade_discount"] = fp.CurrentLevel * 0.05f;
            _passiveBonuses["fuel_efficiency"] = fp.CurrentLevel * 0.1f;
        }
    }

    private void ApplyBuildingBonus(string hubId, BuildingState building)
    {
        float level = building.CurrentLevel;
        switch (building.BuildingId)
        {
            case "fabricator":
                _passiveBonuses["craft_quality"] = level * 0.1f;
                _passiveBonuses["craft_cost_reduction"] = level * 0.05f;
                break;
            case "medbay":
                _passiveBonuses["heal_efficiency"] = level * 0.15f;
                _passiveBonuses["resurrection_chance"] = level * 0.05f;
                break;
            case "signal_beacon":
                _passiveBonuses["signal_range"] = level * 1f;
                _passiveBonuses["signal_points_gain"] = level * 0.1f;
                break;
            case "armory":
                _passiveBonuses["physical_damage"] = level * 0.05f;
                _passiveBonuses["armor_rating"] = level * 0.05f;
                break;
            case "communion_pool":
                _passiveBonuses["mutation_fragment_gain"] = level * 0.15f;
                _passiveBonuses["mutation_slot"] = level > 2 ? 1 : 0;
                break;
            case "shrine":
                _passiveBonuses["corruption_decay"] = level * 0.05f;
                break;
            case "root_network":
                _passiveBonuses["xp_gain"] = level * 0.05f;
                break;
            case "market":
                _passiveBonuses["buy_discount"] = level * 0.1f;
                _passiveBonuses["sell_bonus"] = level * 0.05f;
                break;
            case "garage":
                _passiveBonuses["fuel_capacity"] = level * 10f;
                _passiveBonuses["travel_speed"] = level * 0.1f;
                break;
            case "workshop":
                _passiveBonuses["salvage_yield"] = level * 0.15f;
                break;
            case "quarters":
                _passiveBonuses["companion_capacity"] = level * 1f;
                _passiveBonuses["companion_loyalty_gain"] = level * 0.05f;
                break;
        }
    }

    public float GetPassiveBonus(string bonusId)
    {
        return _passiveBonuses.GetValueOrDefault(bonusId, 0f);
    }

    // ========== RESEARCH TREE ==========

    private void InitializeResearchTree()
    {
        _researchTree["improved_crafting"] = new ResearchNode
        {
            ResearchId = "improved_crafting",
            DisplayName = "Improved Crafting",
            Description = "Unlock Tier 2 crafting recipes. Fabricator output +25%.",
            Cost = new Dictionary<string, int> { { "scrap", 200 }, { "tech_scrap", 5 } },
            Prerequisites = new List<string>(),
            Category = "technology",
            Duration = 60f, // 60 seconds
            Effect = "unlock_tier2_crafting"
        };

        _researchTree["signal_amplification"] = new ResearchNode
        {
            ResearchId = "signal_amplification",
            DisplayName = "Signal Amplification",
            Description = "Increase Signal range by 2 sectors. +1 Signal Point per level.",
            Cost = new Dictionary<string, int> { { "resonance_crystal", 3 }, { "scrap", 300 } },
            Prerequisites = new List<string> { "improved_crafting" },
            Category = "signal",
            Duration = 120f,
            Effect = "signal_range+2"
        };

        _researchTree["corruption_filter"] = new ResearchNode
        {
            ResearchId = "corruption_filter",
            DisplayName = "Corruption Filter",
            Description = "Reduce corruption gain from all sources by 25%.",
            Cost = new Dictionary<string, int> { { "bio_gel", 10 }, { "vital_essence", 3 } },
            Prerequisites = new List<string>(),
            Category = "biology",
            Duration = 90f,
            Effect = "corruption_reduction_25"
        };

        _researchTree["companion_synergy_mastery"] = new ResearchNode
        {
            ResearchId = "companion_synergy_mastery",
            DisplayName = "Synergy Mastery",
            Description = "Companion synergy abilities gain +50% effectiveness.",
            Cost = new Dictionary<string, int> { { "scrap", 500 }, { "vital_essence", 5 }, { "resonance_crystal", 3 } },
            Prerequisites = new List<string> { "signal_amplification" },
            Category = "social",
            Duration = 180f,
            Effect = "synergy_power_50"
        };

        _researchTree["nemesis_weakening"] = new ResearchNode
        {
            ResearchId = "nemesis_weakening",
            DisplayName = "Nemesis Analysis",
            Description = "Reveal all active Nemesis weaknesses. +25% damage vs Nemesis.",
            Cost = new Dictionary<string, int> { { "scrap", 400 }, { "tech_scrap", 15 }, { "data_fragment", 5 } },
            Prerequisites = new List<string> { "improved_crafting" },
            Category = "tactical",
            Duration = 150f,
            Effect = "reveal_nemesis_weaknesses"
        };

        _researchTree["automated_defenses"] = new ResearchNode
        {
            ResearchId = "automated_defenses",
            DisplayName = "Automated Defenses",
            Description = "Hub turrets auto-attack the first enemy each combat round.",
            Cost = new Dictionary<string, int> { { "scrap", 600 }, { "tech_scrap", 20 } },
            Prerequisites = new List<string> { "improved_crafting", "corruption_filter" },
            Category = "technology",
            Duration = 200f,
            Effect = "hub_turrets_auto"
        };

        _researchTree["mutation_refinement"] = new ResearchNode
        {
            ResearchId = "mutation_refinement",
            DisplayName = "Mutation Refinement",
            Description = "Reduce corruption cost of mutations by 40%. Unlock one bonus mutation slot.",
            Cost = new Dictionary<string, int> { { "bio_gel", 15 }, { "vital_essence", 8 }, { "resonance_crystal", 5 } },
            Prerequisites = new List<string> { "corruption_filter" },
            Category = "biology",
            Duration = 240f,
            Effect = "mutation_cost_reduction_40"
        };
    }

    public bool CanResearch(string researchId)
    {
        if (!_researchTree.TryGetValue(researchId, out var node)) return false;
        if (_completedResearch.Contains(researchId)) return false;
        if (_activeResearch == researchId) return false;

        foreach (var prereq in node.Prerequisites)
            if (!_completedResearch.Contains(prereq)) return false;

        foreach (var cost in node.Cost)
            if (!HasResource(cost.Key, cost.Value)) return false;

        return true;
    }

    private string _activeResearch = "";
    private float _researchProgress = 0f;
    private ResearchNode _currentResearchNode = null;

    public void StartResearch(string researchId)
    {
        if (!CanResearch(researchId)) return;
        
        var node = _researchTree[researchId];
        foreach (var cost in node.Cost)
            SpendResource(cost.Key, cost.Value);

        _activeResearch = researchId;
        _researchProgress = 0f;
        _currentResearchNode = node;
        GD.Print($"[BaseBuilding] Research started: {node.DisplayName} ({node.Duration}s)");
    }

    public override void _Process(double delta)
    {
        if (_activeResearch == "" || _currentResearchNode == null) return;

        _researchProgress += (float)delta;
        if (_researchProgress >= _currentResearchNode.Duration)
            CompleteActiveResearch();
    }

    private void CompleteActiveResearch()
    {
        if (_currentResearchNode == null) return;

        _completedResearch.Add(_activeResearch);
        ApplyResearchEffect(_currentResearchNode.Effect);
        OnResearchCompleted?.Invoke(_activeResearch);
        GD.Print($"[BaseBuilding] Research complete: {_currentResearchNode.DisplayName}");

        _activeResearch = "";
        _researchProgress = 0f;
        _currentResearchNode = null;
    }

    private void ApplyResearchEffect(string effect)
    {
        switch (effect)
        {
            case "unlock_tier2_crafting":
                _passiveBonuses["craft_tier_unlocked"] = 2f;
                break;
            case "signal_range+2":
                _passiveBonuses["signal_range"] = (_passiveBonuses.GetValueOrDefault("signal_range", 0f)) + 2f;
                break;
            case "corruption_reduction_25":
                _passiveBonuses["corruption_multiplier"] = 0.75f;
                break;
            case "synergy_power_50":
                _passiveBonuses["synergy_power_multiplier"] = 1.5f;
                break;
            case "reveal_nemesis_weaknesses":
                _passiveBonuses["nemesis_damage_multiplier"] = 1.25f;
                break;
            case "hub_turrets_auto":
                _passiveBonuses["hub_turret_damage"] = 5f;
                break;
            case "mutation_cost_reduction_40":
                _passiveBonuses["mutation_cost_multiplier"] = 0.6f;
                _passiveBonuses["bonus_mutation_slot"] = 1f;
                break;
        }
    }

    // ========== RESOURCES ==========

    public bool HasResource(string resourceId, int amount)
    {
        return _resources.GetValueOrDefault(resourceId, 0) >= amount;
    }

    public int GetResource(string resourceId)
    {
        return _resources.GetValueOrDefault(resourceId, 0);
    }

    public void AddResource(string resourceId, int amount)
    {
        if (!_resources.ContainsKey(resourceId))
            _resources[resourceId] = 0;
        _resources[resourceId] += amount;
        OnResourceChanged?.Invoke(resourceId, _resources[resourceId]);
    }

    public void SpendResource(string resourceId, int amount)
    {
        if (HasResource(resourceId, amount))
        {
            _resources[resourceId] -= amount;
            OnResourceChanged?.Invoke(resourceId, _resources[resourceId]);
        }
    }

    // ========== COMPANION HOUSING ==========

    public int GetCompanionCapacity()
    {
        return Mathf.RoundToInt(4 + GetPassiveBonus("companion_capacity"));
    }

    public bool CanHouseCompanion()
    {
        return GetCompanionCapacity() > 0;
    }

    // ========== QUERIES ==========

    public HubState GetHub(string hubId)
    {
        return _hubs.GetValueOrDefault(hubId);
    }

    public Dictionary<string, HubState> GetAllHubs() => _hubs;
    public List<string> GetCompletedResearch() => _completedResearch;
    public List<ResearchNode> GetAvailableResearch()
    {
        return _researchTree.Values
            .Where(n => !_completedResearch.Contains(n.ResearchId) && _activeResearch != n.ResearchId)
            .ToList();
    }

    public float GetResearchProgress() => _researchProgress;
    public string GetActiveResearch() => _activeResearch;
}

public class HubState
{
    public string HubId { get; set; }
    public string DisplayName { get; set; }
    public FactionId Faction { get; set; }
    public int CurrentLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 5;
    public Dictionary<int, Dictionary<string, int>> UpgradeCosts { get; set; } = new();
    public Dictionary<string, BuildingState> Buildings { get; set; } = new();
}

public class BuildingState
{
    public string BuildingId { get; set; }
    public int CurrentLevel { get; set; } = 0;
    public int MaxLevel { get; set; } = 3;
    public string Category { get; set; } = "";
}

public class ResearchNode
{
    public string ResearchId { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public Dictionary<string, int> Cost { get; set; } = new();
    public List<string> Prerequisites { get; set; } = new();
    public string Category { get; set; } = "";
    public float Duration { get; set; } = 60f;
    public string Effect { get; set; } = "";
}