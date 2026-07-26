using Godot;
using System.Collections.Generic;
using System.Linq;
using TheSignal.Core;
using TheSignal.Data;

namespace TheSignal.Systems;

/// <summary>
/// D1: Procedural Encounter Generator — weighted tables by zone/corruption/time,
/// elite/boss scaling, loot seeding.
/// </summary>
[GlobalClass]
public partial class EncounterGenerator : Node
{
    public static EncounterGenerator Instance { get; private set; }

    // Encounter templates by archetype
    private Dictionary<string, List<EncounterTemplate>> _encounterPool = new();
    
    // Seeded RNG for deterministic generation
    private ulong _seed;
    private System.Random _rng;

    // Difficulty scaling
    private const float ELITE_CHANCE = 0.15f;
    private const float BOSS_CHANCE = 0.05f;
    private const float VARIANT_CHANCE = 0.25f;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        _seed = (ulong)(GD.Randi() * uint.MaxValue + GD.Randi());
        _rng = new System.Random((int)_seed);
        InitializeEncounterPool();
    }

    private void InitializeEncounterPool()
    {
        // Rust Mutant archetype
        _encounterPool["rust_mutant"] = new List<EncounterTemplate>
        {
            new() { UnitId = "rust_mutant_scout", Weight = 40, MinLevel = 1, MaxLevel = 3, Tags = "melee,fast" },
            new() { UnitId = "rust_mutant_brute", Weight = 25, MinLevel = 2, MaxLevel = 5, Tags = "melee,heavy" },
            new() { UnitId = "rust_mutant_spitter", Weight = 20, MinLevel = 2, MaxLevel = 4, Tags = "ranged,poison" },
            new() { UnitId = "rust_mutant_pack_leader", Weight = 10, MinLevel = 3, MaxLevel = 6, Tags = "leader,buff" },
            new() { UnitId = "rust_mutant_behemoth", Weight = 5, MinLevel = 5, MaxLevel = 10, Tags = "boss,heavy" }
        };

        // Purified archetype
        _encounterPool["purified"] = new List<EncounterTemplate>
        {
            new() { UnitId = "purified_soldier", Weight = 35, MinLevel = 2, MaxLevel = 5, Tags = "ranged,armored" },
            new() { UnitId = "purified_elite", Weight = 25, MinLevel = 3, MaxLevel = 6, Tags = "melee,shield" },
            new() { UnitId = "purified_sniper", Weight = 15, MinLevel = 3, MaxLevel = 6, Tags = "ranged,piercing" },
            new() { UnitId = "purified_tech_priest", Weight = 15, MinLevel = 4, MaxLevel = 7, Tags = "support,heal" },
            new() { UnitId = "purified_paladin", Weight = 10, MinLevel = 5, MaxLevel = 9, Tags = "boss,holy" }
        };

        // Rooted archetype
        _encounterPool["rooted"] = new List<EncounterTemplate>
        {
            new() { UnitId = "rooted_watcher", Weight = 30, MinLevel = 2, MaxLevel = 5, Tags = "ranged,nature" },
            new() { UnitId = "rooted_vine_guardian", Weight = 25, MinLevel = 3, MaxLevel = 6, Tags = "melee,root" },
            new() { UnitId = "rooted_crystal_shaper", Weight = 20, MinLevel = 3, MaxLevel = 6, Tags = "ranged,crystal" },
            new() { UnitId = "rooted_elder", Weight = 15, MinLevel = 4, MaxLevel = 7, Tags = "support,buff" },
            new() { UnitId = "rooted_avatar", Weight = 10, MinLevel = 6, MaxLevel = 10, Tags = "boss,corruption" }
        };

        // Scavenger archetype
        _encounterPool["scavenger"] = new List<EncounterTemplate>
        {
            new() { UnitId = "scavenger_raider", Weight = 35, MinLevel = 1, MaxLevel = 4, Tags = "melee,fast" },
            new() { UnitId = "scavenger_gunner", Weight = 25, MinLevel = 2, MaxLevel = 5, Tags = "ranged,explosive" },
            new() { UnitId = "scavenger_junk_mech", Weight = 20, MinLevel = 3, MaxLevel = 6, Tags = "heavy,tech" },
            new() { UnitId = "scavenger_crew_leader", Weight = 15, MinLevel = 4, MaxLevel = 7, Tags = "leader,buff" },
            new() { UnitId = "scavenger_war_rig", Weight = 5, MinLevel = 6, MaxLevel = 10, Tags = "boss,vehicle" }
        };

        // Resonance (Signal creatures)
        _encounterPool["resonance"] = new List<EncounterTemplate>
        {
            new() { UnitId = "resonance_shade", Weight = 30, MinLevel = 4, MaxLevel = 8, Tags = "ranged,resonance" },
            new() { UnitId = "resonance_elemental", Weight = 25, MinLevel = 5, MaxLevel = 9, Tags = "boss,resonance" },
            new() { UnitId = "resonance_crystal", Weight = 25, MinLevel = 4, MaxLevel = 7, Tags = "melee,crystal" },
            new() { UnitId = "resonance_wraith", Weight = 15, MinLevel = 6, MaxLevel = 10, Tags = "fast,invisible" },
            new() { UnitId = "resonance_avatar", Weight = 5, MinLevel = 8, MaxLevel = 12, Tags = "boss,legendary" }
        };

        GD.Print($"[EncounterGen] Loaded {_encounterPool.Count} archetype pools");
    }

    // ========== ENCOUNTER GENERATION ==========

    public CombatEncounter GenerateEncounter(string zoneId, string archetype, int playerLevel, 
        float corruptionLevel, float timeOfDay, int partySize)
    {
        if (!_encounterPool.TryGetValue(archetype, out var templates))
        {
            GD.PrintErr($"[EncounterGen] Unknown archetype: {archetype}");
            return null;
        }

        // Filter templates by level range
        var validTemplates = templates.Where(t => 
            playerLevel >= t.MinLevel && playerLevel <= t.MaxLevel).ToList();

        if (validTemplates.Count == 0)
        {
            // Scale up nearest templates
            validTemplates = templates.OrderBy(t => 
                Mathf.Abs(t.MinLevel - playerLevel)).Take(3).ToList();
        }

        // Build encounter composition
        int enemyCount = CalculateEnemyCount(partySize, playerLevel);
        var encounter = new CombatEncounter
        {
            EncounterId = GenerateEncounterId(zoneId, archetype),
            EncounterName = GenerateEncounterName(archetype, corruptionLevel),
            MinLevel = playerLevel,
            MaxLevel = playerLevel + 2
        };

        // Select enemies based on weights
        var selectedUnits = new List<(UnitData, int, int)>(); // unit, position, delay
        
        for (int i = 0; i < enemyCount; i++)
        {
            var template = WeightedSelection(validTemplates);
            if (template == null) continue;

            // Apply scaling
            int scaledLevel = ScaleLevel(template.MinLevel, template.MaxLevel, playerLevel, corruptionLevel);
            bool isElite = _rng.NextDouble() < ELITE_CHANCE;
            bool isBoss = _rng.NextDouble() < BOSS_CHANCE;

            var unitData = CreateUnitData(template, scaledLevel, isElite, isBoss);
            
            // Distribute positions
            int position = GetSpawnPosition(i, enemyCount);
            int spawnDelay = isBoss ? 2 : (isElite ? 1 : 0);

            selectedUnits.Add((unitData, position, spawnDelay));
        }

        encounter.EnemyUnits = selectedUnits.Select(u => new TheSignal.Data.EnemySpawnInfo
        {
            UnitData = u.Item1,
            Position = u.Item2,
            SpawnDelay = u.Item3
        }).ToList();

        // Generate loot
        encounter.Rewards = GenerateLoot(playerLevel, corruptionLevel, selectedUnits.Any(u => false));

        return encounter;
    }

    private int CalculateEnemyCount(int partySize, int playerLevel)
    {
        int baseCount = partySize + 1; // Always outnumber
        int levelBonus = playerLevel / 3;
        return Mathf.Clamp(baseCount + levelBonus, 2, 8);
    }

    private int ScaleLevel(int minLevel, int maxLevel, int playerLevel, float corruption)
    {
        float corruptionBonus = corruption / 100f * 5f; // +0 to +5 levels
        int targetLevel = Mathf.RoundToInt(playerLevel + corruptionBonus);
        return Mathf.Clamp(targetLevel, minLevel, maxLevel + 2);
    }

    private EncounterTemplate WeightedSelection(List<EncounterTemplate> templates)
    {
        float totalWeight = templates.Sum(t => t.Weight);
        float roll = (float)_rng.NextDouble() * totalWeight;
        float cumulative = 0;

        foreach (var template in templates)
        {
            cumulative += template.Weight;
            if (roll <= cumulative) return template;
        }

        return templates[^1];
    }

    private UnitData CreateUnitData(EncounterTemplate template, int level, bool isElite, bool isBoss)
    {
        string unitId = template.UnitId;
        string rarity = "normal";

        if (isBoss)
        {
            unitId = $"boss_{template.UnitId}";
            rarity = "boss";
        }
        else if (isElite)
        {
            unitId = $"elite_{template.UnitId}";
            rarity = "elite";
        }

        return new UnitData
        {
            UnitId = unitId,
            DisplayName = $"{rarity} {template.UnitId.Replace("_", " ")}",
            Type = UnitType.Enemy
        };
    }

    private string GenerateEncounterId(string zoneId, string archetype)
    {
        return $"ENC_PROC_{zoneId}_{archetype}_{GD.Randi()}";
    }

    private string GenerateEncounterName(string archetype, float corruption)
    {
        string[] prefixes = { "Hostile", "Aggressive", "Vicious", "Corrupted", "Radiant" };
        int prefixIdx = _rng.Next(prefixes.Length);
        return $"{prefixes[prefixIdx]} {archetype.Replace("_", " ")}s";
    }

    private int GetSpawnPosition(int index, int total)
    {
        // Distribute evenly across a rough circle
        float angle = (float)index / total * Mathf.Tau;
        int x = Mathf.RoundToInt(Mathf.Cos(angle) * 4) + 10; // Center around column 10
        int y = Mathf.RoundToInt(Mathf.Sin(angle) * 3) + 7;  // Center around row 7
        return x * 100 + y; // Encode as 2-digit coords
    }

    // ========== LOOT GENERATION ==========

    public QuestRewards GenerateLoot(int playerLevel, float corruption, bool hasBoss)
    {
        var reward = new QuestRewards();

        // Scrap
        reward.Scrap = Mathf.RoundToInt(10 * playerLevel * (1 + corruption / 200f));

        // XP
        reward.Xp = Mathf.RoundToInt(50 * playerLevel * 1.5f);

        // Corruption-based Resonance fragments
        if (corruption > 20)
            reward.ResonanceFragments = Mathf.RoundToInt(corruption / 10f);

        // Boss bonus
        if (hasBoss)
        {
            reward.Scrap *= 3;
            reward.Xp *= 2;
            reward.Items = new Godot.Collections.Array<RewardItem>
            {
                new RewardItem { ItemId = "resonance_crystal", Count = 1 },
                new RewardItem { ItemId = "vital_essence", Count = 1 }
            };
        }

        // Random item drops
        if (_rng.NextDouble() < 0.3f)
        {
            string[] commonLoot = { "tech_scrap", "bio_gel", "rust_scrap" };
            var items = new List<string> { commonLoot[_rng.Next(commonLoot.Length)] };

            if (_rng.NextDouble() < 0.2f)
                items.Add("stimulant");
            if (_rng.NextDouble() < 0.05f)
                items.Add("purification_serum");

            reward.Items = new Godot.Collections.Array<RewardItem>();
            foreach (var itemId in items) reward.Items.Add(new RewardItem { ItemId = itemId, Count = 1 });
        }

        return reward;
    }

    // ========== CORRUPTION / TIME MODIFIERS ==========

    public float GetCorruptionModifier(float corruptionLevel)
    {
        // Higher corruption -> more dangerous encounters, better loot
        return 1.0f + (corruptionLevel / 100f);
    }

    public float GetTimeModifier(float timeOfDay)
    {
        // Night (0-6, 18-24) = more dangerous
        if (timeOfDay < 6 || timeOfDay > 18)
            return 1.3f;
        // Dawn/dusk = moderate
        if (timeOfDay < 8 || timeOfDay > 16)
            return 1.1f;
        // Day = standard
        return 1.0f;
    }

    // ========== ELITE/BOSS TEMPLATES ==========

    public CombatEncounter GenerateBossEncounter(string zoneId, int playerLevel, float corruption)
    {
        var bossEncounter = GenerateEncounter(zoneId, "resonance", playerLevel, corruption, 12f, 4);
        if (bossEncounter != null)
        {
            bossEncounter.EncounterName = $"[LEGENDARY] {bossEncounter.EncounterName}";
            bossEncounter.MinLevel = playerLevel + 2;
        }
        return bossEncounter;
    }

    public CombatEncounter GenerateElitePatrol(string zoneId, string archetype, int playerLevel)
    {
        var eliteEncounter = GenerateEncounter(zoneId, archetype, playerLevel, 30f, 12f, 3);
        if (eliteEncounter != null && eliteEncounter.EnemyUnits.Count > 0)
        {
            // Upgrade first enemy to elite
            var first = eliteEncounter.EnemyUnits[0];
            // stub: Rarity not on UnitData
        }
        return eliteEncounter;
    }

    public void SetSeed(ulong seed)
    {
        _seed = seed;
        _rng = new System.Random((int)seed);
        GD.Print($"[EncounterGen] RNG seeded: {seed}");
    }
}

public class EncounterTemplate
{
    public string UnitId { get; set; }
    public float Weight { get; set; } = 10;
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 10;
    public string Tags { get; set; } = "";
}