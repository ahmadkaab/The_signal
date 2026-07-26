using Godot;
using System.Collections.Generic;
using System.Linq;
using TheSignal.Core;
using TheSignal.Data;

namespace TheSignal.Systems;

/// <summary>
/// D2: Nemesis System — enemies that survive remember the player, gain ranks,
/// hunt across zones, and drop unique loot.
/// </summary>
[GlobalClass]
public partial class NemesisSystem : Node
{
    public static NemesisSystem Instance { get; private set; }

    private List<NemesisData> _nemeses = new();
    private Dictionary<string, int> _killCounters = new(); // enemyId -> kills
    private Dictionary<string, List<string>> _grudges = new(); // enemyId -> zones hunted
    
    // Events
    public event Action<NemesisData> OnNemesisCreated;
    public event Action<NemesisData> OnNemesisRankUp;
    public event Action<NemesisData> OnNemesisDefeated;
    public event Action<NemesisData> OnNemesisEncounter;
    public event Action<string> OnNemesisAmbush; // zoneId

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    // ========== NEMESIS CREATION ==========

    public void RecordEnemyEscape(string enemyId, string zoneId, int playerLevel)
    {
        // An enemy escaped combat — create or update a nemesis
        var existing = _nemeses.FirstOrDefault(n => n.OriginalUnitId == enemyId);

        if (existing != null)
        {
            existing.Escapes++;
            existing.Rank = CalculateRank(existing.Escapes);
            existing.HuntZones.Add(zoneId);
            existing.LastEncounterZone = zoneId;
            
            OnNemesisRankUp?.Invoke(existing);
            GD.Print($"[Nemesis] {existing.Name} ranked up to {existing.Rank} after escape #{existing.Escapes}");
        }
        else
        {
            var nemesis = new NemesisData
            {
                NemesisId = $"NEMESIS_{enemyId}_{GD.Randi()}",
                OriginalUnitId = enemyId,
                Name = GenerateNemesisName(enemyId),
                Rank = 1,
                Escapes = 1,
                Kills = 0,
                SpawnZone = zoneId,
                LastEncounterZone = zoneId,
                LastEncounterLevel = playerLevel,
                HuntZones = new List<string> { zoneId },
                Personality = GeneratePersonality(),
                Wounds = new List<string>(),
                Strengths = new List<string> { GenerateStrength() },
                Weaknesses = new List<string> { GenerateWeakness() },
                UniqueLootTable = GenerateLootTable(playerLevel),
                IsAlive = true,
                EnragesOnLowHp = GD.Randf() > 0.5f,
                SummonsAllies = GD.Randf() > 0.7f,
                UsesEnvironment = GD.Randf() > 0.6f
            };

            _nemeses.Add(nemesis);
            OnNemesisCreated?.Invoke(nemesis);
            GD.Print($"[Nemesis] NEW: {nemesis.Name} (Rank {nemesis.Rank}) spawned in {zoneId}");
        }
    }

    public void RecordPlayerKill(string enemyId, string zoneId)
    {
        // Track kills — too many and a nemesis may form from sheer hatred
        if (!_killCounters.ContainsKey(enemyId))
            _killCounters[enemyId] = 0;
        
        _killCounters[enemyId]++;

        // If this unit type has been killed many times, a champion rises
        if (_killCounters[enemyId] >= 10 && !_nemeses.Any(n => n.OriginalUnitId == enemyId))
        {
            RecordEnemyEscape(enemyId, zoneId, 5);
            _killCounters[enemyId] = 0;
        }
    }

    // ========== NEMESIS ENCOUNTERS ==========

    public bool ShouldNemesisAppear(string zoneId, int playerLevel)
    {
        var activeNemeses = _nemeses.Where(n => n.IsAlive && n.HuntZones.Contains(zoneId)).ToList();
        if (activeNemeses.Count == 0) return false;

        // 20% base chance per active nemesis in zone
        float chance = activeNemeses.Count * 0.2f;
        return GD.Randf() < chance;
    }

    public NemesisData GetAmbushingNemesis(string zoneId, int playerLevel)
    {
        var candidates = _nemeses
            .Where(n => n.IsAlive && n.HuntZones.Contains(zoneId))
            .OrderByDescending(n => n.Rank)
            .ToList();

        if (candidates.Count == 0) return null;

        var nemesis = candidates[GD.Randi() % candidates.Count];

        // Update tracking
        nemesis.LastEncounterZone = zoneId;
        nemesis.LastEncounterLevel = playerLevel;
        OnNemesisEncounter?.Invoke(nemesis);
        
        if (nemesis.HuntZones.Count > 1)
            OnNemesisAmbush?.Invoke(zoneId);

        return nemesis;
    }

    public void ResolveNemesisEncounter(string nemesisId, bool playerWon)
    {
        var nemesis = _nemeses.FirstOrDefault(n => n.NemesisId == nemesisId);
        if (nemesis == null) return;

        if (playerWon)
        {
            nemesis.IsAlive = false;
            nemesis.TimesDefeated++;
            OnNemesisDefeated?.Invoke(nemesis);
            GD.Print($"[Nemesis] {nemesis.Name} defeated! Dropping unique loot.");
            
            // Nemesis may return stronger
            if (GD.Randf() < 0.3f)
            {
                nemesis.IsAlive = true;
                nemesis.Rank++;
                nemesis.Strengths.Add(GenerateStrength());
                GD.Print($"[Nemesis] {nemesis.Name} returns! Rank {nemesis.Rank}");
            }
        }
        else
        {
            nemesis.Kills++;
            nemesis.Escapes++;
            nemesis.Rank = CalculateRank(nemesis.Escapes);
            GD.Print($"[Nemesis] {nemesis.Name} defeated you. Rank now {nemesis.Rank}");
        }
    }

    // ========== NEMESIS STATS ==========

    public int GetNemesisDamageBonus(NemesisData nemesis)
    {
        return nemesis.Rank * 3; // +3 damage per rank
    }

    public int GetNemesisArmorBonus(NemesisData nemesis)
    {
        return nemesis.Rank * 2; // +2 armor per rank
    }

    public int GetNemesisHpBonus(NemesisData nemesis)
    {
        return nemesis.Rank * 20; // +20 HP per rank
    }

    public float GetNemesisCritChance(NemesisData nemesis)
    {
        return 0.05f + (nemesis.Rank * 0.02f); // +2% crit per rank
    }

    // ========== GENERATION HELPERS ==========

    private string GenerateNemesisName(string enemyId)
    {
        string[] prefixes = { "The Unbroken", "Iron", "Deathless", "Rust-Forged", "Bloody", "Crimson", "Ashen", "Wrathful" };
        string[] suffixes = { "Reaper", "Huntress", "Marauder", "Tyrant", "Widowmaker", "Doom", "Oathkeeper", "Blight" };
        
        return $"{prefixes[GD.Randi() % prefixes.Length]} {suffixes[GD.Randi() % suffixes.Length]}";
    }

    private string[] _personalities = {
        "Vengeful — prioritizes the player over all other targets",
        "Tactical — uses cover and abilities intelligently",
        "Berserker — enrages below 50% HP, gains +50% damage",
        "Coward — flees at low HP, returns later stronger",
        "Stalker — appears randomly, attacks from behind",
        "Honorable — offers 1v1 duel, won't attack allies",
        "Sadistic — focuses downed allies, executes",
        "Adaptive — resists damage type used last encounter"
    };

    private string GeneratePersonality()
    {
        return _personalities[GD.Randi() % _personalities.Length];
    }

    private string[] _strengths = {
        "Corrupted Blade — attacks apply Bleed (2 stacks)",
        "Chrome Shell — 25% damage reduction from Ballistic",
        "Resonance Shield — reflects 15% of damage back",
        "Toxic Aura — poisons adjacent units each turn",
        "Quick Reflexes — +1 AP per turn",
        "Crystalline Armor — immune to Crits",
        "Signal Immunity — immune to Resonance damage",
        "Regeneration — heals 10% HP per turn"
    };

    private string GenerateStrength()
    {
        return _strengths[GD.Randi() % _strengths.Length];
    }

    private string[] _weaknesses = {
        "Overconfidence — takes 10% more damage from flanking",
        "Corruption Instability — takes 20% more Resonance damage",
        "Slow — -1 AP per turn",
        "Fragile — -25% Max HP",
        "Predictable — player sees next move",
        "Light Sensitive — blinded by flash effects",
        "Old Wound — receives Bleed on first hit",
        "Code Fragment — can be hacked/stunned"
    };

    private string GenerateWeakness()
    {
        string weakness = _weaknesses[GD.Randi() % _weaknesses.Length];
        return weakness;
    }

    private int CalculateRank(int escapes)
    {
        return escapes switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            >= 5 => Mathf.Min(escapes, 10),
            _ => 1
        };
    }

    private Dictionary<string, int> GenerateLootTable(int playerLevel)
    {
        return new Dictionary<string, int>
        {
            ["resonance_crystal"] = 100,
            ["vital_essence"] = 60,
            ["purification_serum"] = 30,
            ["nemesis_unique"] = 15
        };
    }

    // ========== SAVE/LOAD ==========

    public List<NemesisData> GetAllNemeses() => _nemeses.Where(n => n.IsAlive).ToList();
    public int ActiveNemesisCount => _nemeses.Count(n => n.IsAlive);

    public Dictionary<string, object> SaveData()
    {
        var data = new Dictionary<string, object>();
        int i = 0;
        foreach (var nemesis in _nemeses)
        {
            data[$"nemesis_{i}_id"] = nemesis.NemesisId;
            data[$"nemesis_{i}_unit"] = nemesis.OriginalUnitId;
            data[$"nemesis_{i}_rank"] = nemesis.Rank;
            data[$"nemesis_{i}_escapes"] = nemesis.Escapes;
            data[$"nemesis_{i}_kills"] = nemesis.Kills;
            data[$"nemesis_{i}_zones"] = string.Join(",", nemesis.HuntZones);
            i++;
        }
        return data;
    }

    public void LoadData(Dictionary<string, object> data)
    {
        if (data == null) return;
        _nemeses.Clear();
        
        int i = 0;
        while (data.ContainsKey($"nemesis_{i}_id"))
        {
            _nemeses.Add(new NemesisData
            {
                NemesisId = data[$"nemesis_{i}_id"] as string,
                OriginalUnitId = data[$"nemesis_{i}_unit"] as string,
                Rank = (int)(data[$"nemesis_{i}_rank"] ?? 1),
                Escapes = (int)(data[$"nemesis_{i}_escapes"] ?? 0),
                Kills = (int)(data[$"nemesis_{i}_kills"] ?? 0),
                IsAlive = true,
                HuntZones = (data[$"nemesis_{i}_zones"] as string)?.Split(',').ToList() ?? new List<string>()
            });
            i++;
        }
    }
}

public class NemesisData
{
    public string NemesisId { get; set; }
    public string OriginalUnitId { get; set; }
    public string Name { get; set; }
    public int Rank { get; set; } = 1;
    public int Escapes { get; set; }
    public int Kills { get; set; }
    public int TimesDefeated { get; set; }
    public string SpawnZone { get; set; }
    public string LastEncounterZone { get; set; }
    public int LastEncounterLevel { get; set; }
    public List<string> HuntZones { get; set; } = new();
    public string Personality { get; set; }
    public List<string> Wounds { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public Dictionary<string, int> UniqueLootTable { get; set; } = new();
    public bool IsAlive { get; set; } = true;
    
    // AI behavior flags
    public bool EnragesOnLowHp { get; set; }
    public bool SummonsAllies { get; set; }
    public bool UsesEnvironment { get; set; }
}