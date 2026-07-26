using Godot;
using System.Collections.Generic;
using System.Linq;
using TheSignal.Core;
using TheSignal.Data;

namespace TheSignal.Systems;

/// <summary>
/// C5: Side Quest Board — 20 repeatable + 15 unique quests per zone.
/// Handles quest generation, assignment, tracking, and rewards.
/// </summary>
[GlobalClass]
public partial class QuestBoardManager : Node
{
    public static QuestBoardManager Instance { get; private set; }

    // Quest pools by zone
    private Dictionary<string, List<QuestResource>> _repeatablePool = new();
    private Dictionary<string, List<QuestResource>> _uniquePool = new();
    
    // Active quest tracking
    private List<string> _activeQuestIds = new();
    private Dictionary<string, int> _repeatableCompletions = new(); // questId -> completion count
    private Dictionary<string, float> _repeatableCooldowns = new(); // questId -> time until available

    // Available quests at each board
    private Dictionary<string, List<string>> _boardQuests = new(); // zoneId -> list of questIds

    public event Action<string> OnQuestAccepted;
    public event Action<string> OnQuestCompleted;
    public event Action<string> OnQuestBoardRefreshed;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        InitializeQuestPools();
    }

    private void InitializeQuestPools()
    {
        // Register all zones with quest boards
        string[] boardZones = {
            "S09_GRAVEYARD", "S09_PURIFIED_CITADEL", "S09_ROOTED_GROVE", "S09_SCAVENGER_FREEPORT",
            "S08_ASH_PLAINS", "S08_SCORCHED_FOREST", "S08_SCRAP_MINES", "S08_WASTELAND_FORT",
            "S07_CRYSTAL_CAVERNS", "S07_CRYSTAL_SPIRES", "S07_RESONANCE_PIT",
            "S06_PURIFICATION_FORGE", "S06_MUTATION_LAB",
            "S05_THE_BARRIER", "S05_SIGNAL_SPIRE"
        };

        foreach (string zone in boardZones)
        {
            _repeatablePool[zone] = new List<QuestResource>();
            _uniquePool[zone] = new List<QuestResource>();
            _boardQuests[zone] = new List<string>();
        }

        GD.Print($"[QuestBoard] Initialized pools for {boardZones.Length} zones with quest boards");
    }

    // ========== QUEST GENERATION ==========

    public List<QuestResource> GetAvailableQuests(string zoneId, int playerLevel, int factionRep)
    {
        var available = new List<QuestResource>();

        // Add repeatable quests (always available, with cooldown)
        if (_repeatablePool.TryGetValue(zoneId, out var repeatables))
        {
            foreach (var quest in repeatables)
            {
                if (IsQuestAvailable(quest, playerLevel, factionRep))
                {
                    available.Add(quest);
                }
            }
        }

        // Add unique quests (one-time only)
        if (_uniquePool.TryGetValue(zoneId, out var uniques))
        {
            foreach (var quest in uniques)
            {
                if (!_activeQuestIds.Contains(quest.QuestId) && 
                    !IsQuestCompleted(quest.QuestId) &&
                    IsQuestAvailable(quest, playerLevel, factionRep))
                {
                    available.Add(quest);
                }
            }
        }

        // Ensure at least 3 quests per board
        if (available.Count < 3)
        {
            available.AddRange(GenerateFallbackQuests(zoneId, playerLevel, 3 - available.Count));
        }

        return available;
    }

    private bool IsQuestAvailable(QuestResource quest, int playerLevel, int factionRep)
    {
        if (playerLevel < quest.MinLevel) return false;

        // Check rep requirements
        // foreach (var prereq in quest.Prerequisites)
        // {
        //     if (prereq.Contains("rep_"))
        //     {
        //         string[] parts = prereq.Split('_');
        //         if (parts.Length >= 3 && int.TryParse(parts[2], out int reqRep))
        //         {
        //             if (factionRep < reqRep) return false;
        //         }
        //     }
        // }

        // Check repeatable cooldown
        if (quest.IsRepeatable && _repeatableCooldowns.TryGetValue(quest.QuestId, out float cooldown))
        {
            if (cooldown > 0) return false;
        }

        return true;
    }

    private List<QuestResource> GenerateFallbackQuests(string zoneId, int playerLevel, int count)
    {
        var fallbacks = new List<QuestResource>();
        for (int i = 0; i < count; i++)
        {
            fallbacks.Add(new QuestResource
            {
                QuestId = $"FALLBACK_{zoneId}_{i}_{GD.Randi()}",
                Title = GetRandomQuestTitle(zoneId),
                Description = "A standard side objective in the area.",
                MinLevel = playerLevel,
                Type = QuestType.Side,
                IsRepeatable = true
            });
        }
        return fallbacks;
    }

    private string GetRandomQuestTitle(string zoneId)
    {
        string[] titles = {
            "Clear the Area", "Resource Collection", "Patrol Duty",
            "Scavenger Hunt", "Threat Neutralization", "Supply Run",
            "Intel Gathering", "Rescue Operation", "Secure Perimeter"
        };
        return titles[GD.Randi() % titles.Length];
    }

    // ========== QUEST TRACKING ==========

    public void AcceptQuest(string questId)
    {
        if (!_activeQuestIds.Contains(questId))
        {
            _activeQuestIds.Add(questId);
            OnQuestAccepted?.Invoke(questId);
            GD.Print($"[QuestBoard] Accepted quest: {questId}");
        }
    }

    public void CompleteQuest(string questId)
    {
        if (_activeQuestIds.Remove(questId))
        {
            OnQuestCompleted?.Invoke(questId);

            // Track repeatable completions
            _repeatableCompletions.TryGetValue(questId, out int count);
            _repeatableCompletions[questId] = count + 1;

            // Set cooldown for repeatable quests
            _repeatableCooldowns[questId] = 300f; // 5 minutes real-time

            GD.Print($"[QuestBoard] Completed quest: {questId}");
        }
    }

    public bool IsQuestCompleted(string questId)
    {
        return QuestManager.Instance?.CompletedQuests?.ContainsKey(questId) ?? false;
    }

    public void FailQuest(string questId)
    {
        _activeQuestIds.Remove(questId);
        GD.Print($"[QuestBoard] Failed quest: {questId}");
    }

    // ========== BOARD REFRESH ==========

    public void RefreshBoard(string zoneId)
    {
        if (_boardQuests.ContainsKey(zoneId))
        {
            // Reset repeatable cooldowns gradually
            _boardQuests[zoneId].Clear();
            OnQuestBoardRefreshed?.Invoke(zoneId);
        }
    }

    public void RefreshAllBoards()
    {
        foreach (var zoneId in _boardQuests.Keys)
        {
            RefreshBoard(zoneId);
        }
    }

    public override void _Process(double delta)
    {
        // Tick repeatable cooldowns
        float dt = (float)delta;
        var toRemove = new List<string>();
        foreach (var kvp in _repeatableCooldowns)
        {
            _repeatableCooldowns[kvp.Key] = kvp.Value - dt;
            if (_repeatableCooldowns[kvp.Key] <= 0)
                toRemove.Add(kvp.Key);
        }
        foreach (var key in toRemove)
        {
            _repeatableCooldowns.Remove(key);
        }
    }

    // ========== REGISTER QUESTS ==========

    public void RegisterRepeatableQuest(string zoneId, QuestResource quest)
    {
        if (!_repeatablePool.ContainsKey(zoneId))
            _repeatablePool[zoneId] = new List<QuestResource>();
        _repeatablePool[zoneId].Add(quest);
    }

    public void RegisterUniqueQuest(string zoneId, QuestResource quest)
    {
        if (!_uniquePool.ContainsKey(zoneId))
            _uniquePool[zoneId] = new List<QuestResource>();
        _uniquePool[zoneId].Add(quest);
    }

    // ========== REPEATABLE QUEST GENERATORS ==========

    public void GenerateRepeatableQuests()
    {
        // 20 repeatable quest templates across zones
        (string, string, string, string, int, int, string)[] repeatables = {
            ("Q_REP_GRAVEYARD_CLEAR", "S09_GRAVEYARD", "Clear Rust Mutants", "Neutralize 5 Rust Mutant packs in the Graveyard. They're disrupting salvage operations.", 1, 10, "scrap"),
            ("Q_REP_CITADEL_PATROL", "S09_PURIFIED_CITADEL", "Citadel Patrol", "Join a Purified patrol and secure the perimeter. Report any Rooted activity.", 2, 5, "purified_rep"),
            ("Q_REP_GROVE_HARVEST", "S09_ROOTED_GROVE", "Grove Harvest", "Collect Bio-Gel from the Crystal Caverns. The Grove's stores are running low.", 3, 15, "rooted_rep"),
            ("Q_REP_FREEPORT_TRADE", "S09_SCAVENGER_FREEPORT", "Trade Run", "Deliver supplies between Freeport and the Scrap Mines. Standard caravan route.", 2, 20, "scrap"),
            ("Q_REP_ASH_CLEAR", "S08_ASH_PLAINS", "Ash Clearing", "Cull the Ash Walker population. They're getting too close to the trade route.", 4, 15, "scrap"),
            ("Q_REP_FOREST_SCOUT", "S08_SCORCHED_FOREST", "Forest Recon", "Scout the Scorched Forest for Purified movement. Report back with intel.", 4, 10, "rooted_rep"),
            ("Q_REP_MINES_EXTRACT", "S08_SCRAP_MINES", "Deep Extraction", "Descend into the lower mines and extract tech fragments. Risk of collapse high.", 5, 25, "scrap"),
            ("Q_REP_FORT_DEFENSE", "S08_WASTELAND_FORT", "Fort Defense", "Defend the Wasteland Fort from an incoming Scavenger raid.", 5, 15, "purified_rep"),
            ("Q_REP_CAVERN_CRYSTAL", "S07_CRYSTAL_CAVERNS", "Crystal Harvest", "Mine Resonance Crystals from the cavern walls. The Purified pay well for these.", 6, 20, "scrap"),
            ("Q_REP_SPIRE_WATCH", "S07_CRYSTAL_SPIRES", "Spire Watch", "Keep watch at the Crystal Spires for Rooted patrols. Report any unusual activity.", 6, 10, "purified_rep"),
            ("Q_REP_PIT_HUNT", "S07_RESONANCE_PIT", "Pit Hunt", "Hunt Resonance Shades in the Pit. Their cores are valuable for mutation research.", 7, 25, "scrap"),
            ("Q_REP_FORGE_FUEL", "S06_PURIFICATION_FORGE", "Forge Fuel Run", "Collect fuel crystals from the Crystal Caverns for the Forge.", 8, 20, "purified_rep"),
            ("Q_REP_LAB_SPECIMEN", "S06_MUTATION_LAB", "Specimen Collection", "Collect living specimens from the Pit for the Lab's mutation research.", 8, 20, "rooted_rep"),
            ("Q_REP_BARRIER_STUDY", "S05_THE_BARRIER", "Barrier Study", "Take Resonance readings from the Barrier. ORACLE needs more data.", 9, 15, "oracle_trust"),
            ("Q_REP_SPIRE_SIGNAL", "S05_SIGNAL_SPIRE", "Signal Boost", "Amplify the Signal at the Spire. Each boost reveals more of Sector 0's map.", 10, 30, "signal_points"),
            ("Q_REP_SCRAP_COLLECT", "S08_ASH_PLAINS", "Scrap Collection", "Collect 50 units of scrap metal from the Ash Plains wreckage.", 3, 10, "scrap"),
            ("Q_REP_MUTANT_CULL", "S08_SCORCHED_FOREST", "Mutant Cull", "Reduce the mutant population in the Scorched Forest by 10 packs.", 5, 15, "scrap"),
            ("Q_REP_CAVERN_SALVAGE", "S07_CRYSTAL_CAVERNS", "Cavern Salvage", "Retrieve pre-Rust technology from deep within the Caverns.", 6, 20, "scrap"),
            ("Q_REP_LAB_CLEANSE", "S06_MUTATION_LAB", "Lab Cleanse", "Purify corrupted experiment pods in the Mutation Lab.", 8, 15, "purified_rep"),
            ("Q_REP_SPIRE_TRANSMIT", "S05_SIGNAL_SPIRE", "Spire Transmission", "Transmit a message through the Spire to allied factions.", 10, 20, "oracle_trust")
        };

        foreach (var (questId, zone, title, desc, minLvl, reward, rewardType) in repeatables)
        {
            RegisterRepeatableQuest(zone, new QuestResource
            {
                QuestId = questId,
                Title = title,
                Description = desc,
                MinLevel = minLvl,
                Type = QuestType.Side,
                IsRepeatable = true
            });
        }

        GD.Print($"[QuestBoard] Generated {repeatables.Length} repeatable quests");
    }

    public void GenerateUniqueQuests()
    {
        // 15 unique quests spread across zones
        (string questId, string zone, string title, string desc, int minLvl, int reward, string rewardType, string completion)[] uniques = {
            ("Q_UNIQUE_GHOST_SHIP", "S08_ASH_PLAINS", "Ghost Ship", "A pre-Rust freighter has emerged from the ash. Legend says it carries a working AI core. Investigate and recover the core.", 5, 30, "rare_weapon", "The freighter's data core reveals ORACLE's true age: older than recorded history."),
            ("Q_UNIQUE_BURIED_VAULT", "S08_SCRAP_MINES", "The Buried Vault", "Miners uncovered a sealed vault with Purified and Rooted symbols side by side. The key was lost centuries ago.", 5, 40, "artifact", "The vault contains a 'Unity Treaty' — proof the Purified and Rooted were once one faction."),
            ("Q_UNIQUE_CRYSTAL_QUEEN", "S07_CRYSTAL_CAVERNS", "Crystal Queen", "A massive crystalline entity has awakened in the deepest caverns. It's drawing all Resonance energy to itself.", 7, 50, "unique_ability", "Defeating the Queen grants 'Crystal Communion' — a passive that doubles Resonance gains."),
            ("Q_UNIQUE_FORGE_GHOSTS", "S06_PURIFICATION_FORGE", "Forge Ghosts", "Workers report seeing apparitions in the Forge — digital ghosts of Purified who died in a ritual accident.", 8, 45, "unique_armor", "The ghosts are 'backups' — digital copies of Purified minds. They reveal the faction's dark secret."),
            ("Q_UNIQUE_ELDER_BLOOM", "S06_MUTATION_LAB", "Elder Bloom", "A legendary flower that grants immunity to a single damage type. Only one bloom exists.", 8, 50, "permanent_buff", "Consuming the bloom grants permanent +20% Resistance to one damage type of your choice."),
            ("Q_UNIQUE_BARRIER_VOICE", "S05_THE_BARRIER", "The Voice in the Barrier", "Someone is trapped inside the Barrier. Their voice echoes through the crystal. Free them.", 9, 60, "companion_unlock", "Freeing the trapped Walker gives you a temporary ally for Act II."),
            ("Q_UNIQUE_ORACLE_FRAGMENT", "S05_SIGNAL_SPIRE", "Oracle Fragment", "A fragment of ORACLE's original code fell from the Spire. It contains restricted memories.", 10, 70, "lore_unlock", "The fragment reveals ORACLE was built to contain something. Not just broadcast the Cure — but imprison an entity."),
            ("Q_UNIQUE_DESERTER", "S09_PURIFIED_CITADEL", "The Deserter", "A Purified soldier fled the Citadel with classified data. Find them before the Chrome Guard does.", 2, 25, "intel", "The data reveals Purified plans to weaponize the Signal against the Rooted."),
            ("Q_UNIQUE_GROVE_SICKNESS", "S09_ROOTED_GROVE", "Grove Sickness", "The Heart-Tree is wilting. The Rooted blame the Purified. The truth is more disturbing.", 3, 25, "unique_item", "The sickness comes from deep underground — something is poisoning the roots across all sectors."),
            ("Q_UNIQUE_FREEPORT_COUP", "S09_SCAVENGER_FREEPORT", "Freeport Coup", "A rogue Scavenger faction is plotting a takeover. Choose: warn the leaders, join the rebels, or exploit the chaos.", 3, 30, "faction_choice", "Outcome changes Freeport leadership and faction standing permanently."),
            ("Q_UNIQUE_MESSENGER_QUEST", "S05_SIGNAL_SPIRE", "The First Word", "The Messenger reveals: ORACLE's first transmission contained a greeting. Who was it for?", 10, 80, "ending_unlock", "Unlocks the 'Diplomacy' ending option for Act II."),
            ("Q_UNIQUE_RUST_BEHEMOTH", "S08_SCORCHED_FOREST", "Rust Behemoth", "A walking fortress of Rust and scrap is flattening everything in its path. It must be stopped.", 6, 50, "rare_material", "The Behemoth's core is pure Purified Chrome — enough to craft a legendary weapon."),
            ("Q_UNIQUE_VEX_REDEMPTION", "S08_WASTELAND_FORT", "Vex's Redemption", "Vex's past catches up. A former Purified target survived and wants revenge. Help Vex end this.", 6, 40, "companion_upgrade", "Completing this permanently upgrades Vex's abilities and raises loyalty max to 100."),
            ("Q_UNIQUE_ECHO_ORIGIN", "S07_RESONANCE_PIT", "Echo's Origin", "Echo detects a signal from the deep Pit — another self-aware drone. Friend or threat?", 7, 45, "companion_upgrade", "Echo gains a new ability: 'Signal Duplicate' — create a copy of self for 2 turns."),
            ("Q_UNIQUE_PIT_ASCEND", "S07_RESONANCE_PIT", "Pit Ascension", "Reach the bottom of the Resonance Pit and survive the 'Resonance Trial' — a gauntlet of 5 waves.", 8, 60, "legendary_item", "Reward: 'The Echo' — a legendary pistol that deals bonus damage per active status effect.")
        };

        foreach (var (questId, zone, title, desc, minLvl, reward, rewardType, completion) in uniques)
        {
            RegisterUniqueQuest(zone, new QuestResource
            {
                QuestId = questId,
                Title = title,
                Description = desc,
                MinLevel = minLvl,
                Type = QuestType.Side,
                IsRepeatable = false
            });
        }

        GD.Print($"[QuestBoard] Generated {uniques.Length} unique quests");
    }
}