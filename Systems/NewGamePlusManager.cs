using Godot;
using System.Collections.Generic;
using System.Linq;
using TheSignal.Core;
using TheSignal.Core.Save;
using TheSignal.Data;

namespace TheSignal.Systems;

/// <summary>
/// D4: New Game+ / Prestige — carry over mutations/synergy/gear,
/// increased difficulty, exclusive content, lore unlocks.
/// </summary>
[GlobalClass]
public partial class NewGamePlusManager : Node
{
    public static NewGamePlusManager Instance { get; private set; }

    // NG+ state
    private int _prestigeLevel = 0;
    private bool _isNewGamePlus = false;
    private float _difficultyMultiplier = 1.0f;

    // Carry-over data
    private List<CarryOverItem> _carryOverItems = new();
    private List<string> _unlockedEndings = new();
    private Dictionary<string, bool> _loreUnlocks = new(); // loreId -> discovered

    // Exclusive content gates
    private Dictionary<string, int> _prestigeGates = new(); // contentId -> required prestige

    public event Action<int> OnPrestigeLevelChanged; // new prestige level
    public event Action<string> OnLoreUnlocked;
    public event Action<NewGamePlusData> OnNewGamePlusStarted;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        InitializePrestigeGates();
    }

    private void InitializePrestigeGates()
    {
        _prestigeGates["gauntlet_mode"] = 1;      // NG+1: Unlock Gauntlet Arena
        _prestigeGates["secret_ending"] = 2;       // NG+2: Unlock secret ending path
        _prestigeGates["lore_complete"] = 2;       // NG+2: All lore fragments discoverable
        _prestigeGates["boss_rush"] = 3;           // NG+3: Boss Rush mode
        _prestigeGates["legendary_gear"] = 3;      // NG+3: Legendary gear drop rate 100%
        _prestigeGates["true_ending"] = 4;         // NG+4: Unlock True Ending
        _prestigeGates["infinite_echo"] = 5;       // NG+5: 'Infinite Echo' artifact — all stats +50%
        _prestigeGates["commissioned_rank"] = 5;   // NG+5: Prestige title and unique cosmetic
        _prestigeGates["oracle_truth"] = 3;        // NG+3: Full ORACLE backstory unlocked
        _prestigeGates["walker_eternal"] = 10;     // NG+10: "Walker Eternal" achievement + ultra-rare title
    }

    // ========== STARTING NG+ ==========

    public NewGamePlusData PrepareNewGamePlus(SaveSlotData completedSave)
    {
        _isNewGamePlus = true;
        _prestigeLevel = (completedSave?.PrestigeLevel ?? 0) + 1;

        var data = new NewGamePlusData
        {
            PrestigeLevel = _prestigeLevel,
            DifficultyMultiplier = CalculateDifficulty(_prestigeLevel),
            CarryOverMutations = FilterCarryOver(completedSave?.ActiveMutations),
            CarryOverGear = FilterCarryOver(completedSave?.EquippedItems),
            CarryOverSynergyRanks = completedSave?.CompanionSynergyRanks ?? new Dictionary<string, int>(),
            CarryOverFactionRep = completedSave?.FactionReputation ?? new Dictionary<string, int>(),
            CarryOverResources = new Dictionary<string, int>
            {
                ["resonance_crystal"] = _prestigeLevel * 5,
                ["vital_essence"] = _prestigeLevel * 3,
                ["scrap"] = _prestigeLevel * 200
            },
            UnlockedEndings = _unlockedEndings,
            UnlockedContentIds = GetUnlockedContent(_prestigeLevel),
            LoreDiscovered = _loreUnlocks.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList()
        };

        OnNewGamePlusStarted?.Invoke(data);
        GD.Print($"[NG+] Prepared NG+{_prestigeLevel} (Difficulty: {_difficultyMultiplier:F2}x)");
        return data;
    }

    private float CalculateDifficulty(int prestigeLevel)
    {
        return prestigeLevel switch
        {
            0 => 1.0f,
            1 => 1.25f,
            2 => 1.50f,
            3 => 1.75f,
            4 => 2.0f,
            5 => 2.5f,
            >= 6 => 2.5f + (prestigeLevel - 5) * 0.5f,
            _ => 1.0f
        };
    }

    private List<string> FilterCarryOver(List<string> items)
    {
        if (items == null) return new List<string>();
        // Only carry over non-consumable, non-quest items
        return items.Where(item => !item.StartsWith("quest_") && !item.StartsWith("consumable_")).ToList();
    }

    private List<string> GetUnlockedContent(int prestige)
    {
        return _prestigeGates
            .Where(gate => prestige >= gate.Value)
            .Select(gate => gate.Key)
            .ToList();
    }

    // ========== DIFFICULTY MODIFIERS ==========

    public float GetEnemyHpMultiplier()
    {
        return _difficultyMultiplier;
    }

    public float GetEnemyDamageMultiplier()
    {
        return _difficultyMultiplier * 0.9f; // Slightly less punishing than HP
    }

    public int GetExtraEnemiesPerEncounter()
    {
        return _prestigeLevel / 2; // +1 extra enemy per 2 prestige levels
    }

    public float GetLootQuantityMultiplier()
    {
        return 1.0f + (_prestigeLevel * 0.2f); // +20% loot per prestige
    }

    public float GetLootQualityMultiplier()
    {
        return 1.0f + (_prestigeLevel * 0.1f); // +10% quality chance per prestige
    }

    public float GetXpMultiplier()
    {
        return 1.0f + (_prestigeLevel * 0.15f); // +15% XP per prestige
    }

    public int GetBonusStatPointsPerLevel()
    {
        return _prestigeLevel; // +1 stat point per level per prestige
    }

    public float GetScrapMultiplier()
    {
        return 1.0f + (_prestigeLevel * 0.2f);
    }

    public float GetCorruptionRateMultiplier()
    {
        return 1.0f + (_prestigeLevel * 0.1f); // Corruption builds faster
    }

    // ========== EXCLUSIVE CONTENT ==========

    public bool IsContentUnlocked(string contentId)
    {
        return _prestigeGates.TryGetValue(contentId, out int required) 
            && _prestigeLevel >= required;
    }

    public string[] GetExclusiveEncounters()
    {
        if (_prestigeLevel >= 3)
            return new[] { "boss_rush_champion", "elite_nemesis_pack", "resonance_avatar_trio" };
        if (_prestigeLevel >= 1)
            return new[] { "elite_patrol_omega", "corrupted_walker_shade" };
        return System.Array.Empty<string>();
    }

    public string[] GetExclusiveVendorItems()
    {
        if (_prestigeLevel >= 5)
            return new[] { "infinite_echo_artifact", "prestige_armor_set", "cosmetic_halo" };
        if (_prestigeLevel >= 2)
            return new[] { "legendary_weapon_core", "prestige_ring_of_power" };
        return new[] { "enhanced_mutation_vial" };
    }

    // ========== LORE UNLOCKS ==========

    public void UnlockLore(string loreId)
    {
        if (!_loreUnlocks.ContainsKey(loreId))
        {
            _loreUnlocks[loreId] = true;
            OnLoreUnlocked?.Invoke(loreId);
            GD.Print($"[NG+] Lore unlocked: {loreId}");
        }
    }

    public bool IsLoreUnlocked(string loreId)
    {
        return _loreUnlocks.GetValueOrDefault(loreId, false);
    }

    public string[] GetLoreFragments()
    {
        return new[]
        {
            "ORACLE was built in 2037 — 300 years before recorded history",
            "The Rust was not an accident. It was designed as a weapon.",
            "ORACLE's creator was the last pre-Rust president. She uploaded herself.",
            "The Signal is not a broadcast — it's a containment field.",
            "Sector 0 is not a city. It's a prison. ORACLE is the warden.",
            "The Cure and the Rust are the same code. Intent determines outcome.",
            "There were 12 original Walkers. You are the 13th.",
            "The Purified and Rooted were once one religion. The Schism created both.",
            "The Scavengers know the truth but profit from silence.",
            "A Walker before you reached Sector 0. They are still there."
        };
    }

    // ========== ENDING COMPLETION ==========

    public void RecordEnding(string endingId, string endingName)
    {
        if (!_unlockedEndings.Contains(endingId))
        {
            _unlockedEndings.Add(endingId);
            GD.Print($"[NG+] Ending recorded: {endingName} ({endingId})");

            // Accumulative lore rewards
            if (endingId.EndsWith("_truth"))
                UnlockLore("oracle_fragment_1");
            if (endingId.EndsWith("_sacrifice"))
                UnlockLore("walker_sacrifice_secret");
        }
    }

    public string[] GetUnlockedEndings() => _unlockedEndings.ToArray();
    public int GetPrestigeLevel() => _prestigeLevel;
    public bool IsActive() => _isNewGamePlus;

    // ========== PRESTIGE REWARDS ==========

    public string GetPrestigeTitle()
    {
        return _prestigeLevel switch
        {
            0 => "The Signal Seeker",
            1 => "Walker of Truth",
            2 => "The Unbroken Path",
            3 => "Architect of Fate",
            4 => "The Corrupted Dream",
            5 => "Commissioned Walker",
            >= 6 => $"Walker Eternal ({_prestigeLevel})",
            _ => "The Signal Seeker"
        };
    }

    public string GetPrestigeCosmetic()
    {
        return _prestigeLevel switch
        {
            1 => "resonance_shimmer_effect",
            2 => "corruption_aura",
            3 => "chrome_halo",
            4 => "oracle_light_crown",
            5 => "void_walker_cloak",
            _ => ""
        };
    }

    // ========== SAVE/LOAD ==========

    public Dictionary<string, object> SaveData()
    {
        return new Dictionary<string, object>
        {
            ["prestige_level"] = _prestigeLevel,
            ["endings"] = string.Join(",", _unlockedEndings),
            ["lore"] = string.Join(",", _loreUnlocks.Where(kvp => kvp.Value).Select(kvp => kvp.Key))
        };
    }

    public void LoadData(Dictionary<string, object> data)
    {
        if (data == null) return;
        
        _prestigeLevel = (int)(data.GetValueOrDefault("prestige_level", 0));
        _isNewGamePlus = _prestigeLevel > 0;
        _difficultyMultiplier = CalculateDifficulty(_prestigeLevel);

        if (data.TryGetValue("endings", out var endings) && endings is string endingsStr)
            _unlockedEndings = endingsStr.Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList();

        if (data.TryGetValue("lore", out var lore) && lore is string loreStr)
        {
            foreach (var id in loreStr.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                _loreUnlocks[id] = true;
        }
    }
}

public class NewGamePlusData
{
    public int PrestigeLevel { get; set; }
    public float DifficultyMultiplier { get; set; }
    public List<string> CarryOverMutations { get; set; } = new();
    public List<string> CarryOverGear { get; set; } = new();
    public Dictionary<string, int> CarryOverSynergyRanks { get; set; } = new();
    public Dictionary<string, int> CarryOverFactionRep { get; set; } = new();
    public Dictionary<string, int> CarryOverResources { get; set; } = new();
    public List<string> UnlockedEndings { get; set; } = new();
    public List<string> UnlockedContentIds { get; set; } = new();
    public List<string> LoreDiscovered { get; set; } = new();
}