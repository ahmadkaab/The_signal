using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TheSignal.Data;

namespace TheSignal.Platform;

/// <summary>
/// E1: Steamworks SDK integration — achievements, cloud saves,
/// leaderboards, DRM, friends list.
/// Requires Steamworks.NET or GodotSteam.
/// All calls are stubbed — uncomment SteamAPI calls when SDK is linked.
/// </summary>
[GlobalClass]
public partial class SteamIntegration : Node
{
    public static SteamIntegration Instance { get; private set; }

    // Steam state
    private bool _isInitialized = false;
    private ulong _steamId = 0;
    private string _steamName = "";
    private string _buildId = "";
    private bool _isOnSteamDeck = false;
    private bool _isOverlayEnabled = false;

    // Cached data
    private Dictionary<string, bool> _achievementCache = new();
    private Dictionary<string, int> _statCache = new();
    private Dictionary<string, long> _leaderboardScores = new();

    // Events
    public event Action OnSteamInitialized;
    public event Action<string> OnAchievementUnlocked;
    public event Action<string, int> OnStatChanged;
    public event Action<string, long> OnLeaderboardUpdated;
    public event Action<ulong, string> OnFriendJoined;
    public event Action OnOverlayToggled;
    public event Action<string> OnCloudSyncComplete;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    // ========== INITIALIZATION ==========

    public void Initialize()
    {
        GD.Print("[Steam] Initializing Steamworks...");

        try
        {
            // Uncomment when Steamworks.NET is linked:
            // _isInitialized = SteamAPI.Init();
            _isInitialized = true; // Stub for development

            if (_isInitialized)
            {
                // _steamId = SteamUser.GetSteamID().m_SteamID;
                // _steamName = SteamFriends.GetPersonaName();
                // _buildId = SteamApps.GetAppBuildId().ToString();
                // _isOnSteamDeck = SteamUtils.IsSteamRunningOnSteamDeck();
                // _isOverlayEnabled = SteamUtils.IsOverlayEnabled();

                _steamId = 76561197960265728UL; // Stub
                _steamName = "DevWalker";
                _buildId = "12345";
                _isOnSteamDeck = false;
                _isOverlayEnabled = true;

                // Hook callbacks
                // _achievementUnlockCallback = Callback<AchievementUnlock_t>.Create(OnAchievementUnlock);
                // _userStatsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);

                RequestStats();
                OnSteamInitialized?.Invoke();
                GD.Print($"[Steam] Initialized: {_steamName} ({_steamId})");
            }
            else
            {
                GD.PrintErr("[Steam] Failed to initialize. Running in offline mode.");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[Steam] Init error: {e.Message}. Running in stub mode.");
            _isInitialized = true; // Stub mode — enable for dev builds
        }
    }

    public bool IsInitialized() => _isInitialized;
    public ulong GetSteamId() => _steamId;
    public string GetPlayerName() => _steamName;
    public bool IsSteamDeck() => _isOnSteamDeck;

    public void Shutdown()
    {
        if (_isInitialized)
        {
            // SteamAPI.Shutdown();
            GD.Print("[Steam] Shutdown");
        }
    }

    // ========== ACHIEVEMENTS ==========

    public void RequestStats()
    {
        if (!_isInitialized) return;
        // SteamUserStats.RequestCurrentStats();
        GD.Print("[Steam] Stats requested");
    }

    public void UnlockAchievement(string achievementId)
    {
        if (!_isInitialized) return;
        if (_achievementCache.TryGetValue(achievementId, out bool unlocked) && unlocked) return;

        // bool success = SteamUserStats.SetAchievement(achievementId);
        bool success = true; // Stub
        if (success)
        {
            _achievementCache[achievementId] = true;
            // SteamUserStats.StoreStats();
            OnAchievementUnlocked?.Invoke(achievementId);
            GD.Print($"[Steam] Achievement unlocked: {achievementId}");
        }
    }

    public bool IsAchievementUnlocked(string achievementId)
    {
        if (_achievementCache.TryGetValue(achievementId, out bool unlocked))
            return unlocked;

        // bool unlocked = false;
        // SteamUserStats.GetAchievement(achievementId, out unlocked);
        bool unlocked = false; // Stub
        _achievementCache[achievementId] = unlocked;
        return unlocked;
    }

    public void ClearAchievement(string achievementId)
    {
        if (!_isInitialized) return;
        // SteamUserStats.ClearAchievement(achievementId);
        // SteamUserStats.StoreStats();
        _achievementCache[achievementId] = false;
    }

    // ========== STATS ==========

    public void SetStat(string statId, int value)
    {
        if (!_isInitialized) return;
        // SteamUserStats.SetStat(statId, value);
        _statCache[statId] = value;
        OnStatChanged?.Invoke(statId, value);
    }

    public int GetStat(string statId)
    {
        if (_statCache.TryGetValue(statId, out int value)) return value;
        // int value = 0;
        // SteamUserStats.GetStat(statId, out value);
        return 0;
    }

    public void StoreStats()
    {
        if (!_isInitialized) return;
        // SteamUserStats.StoreStats();
    }

    // ========== LEADERBOARDS ==========

    public void UploadLeaderboardScore(string leaderboardId, int score)
    {
        if (!_isInitialized) return;

        // SteamUserStats.FindOrCreateLeaderboard(leaderboardId, ...);
        // SteamUserStats.UploadLeaderboardScore(leaderboard, ...);

        _leaderboardScores[leaderboardId] = score;
        OnLeaderboardUpdated?.Invoke(leaderboardId, score);
        GD.Print($"[Steam] Leaderboard {leaderboardId}: {score}");
    }

    public long GetLeaderboardScore(string leaderboardId)
    {
        return _leaderboardScores.GetValueOrDefault(leaderboardId, 0);
    }

    public string[] GetLeaderboardEntries(string leaderboardId, int count = 10)
    {
        // SteamUserStats.GetLeaderboardEntries(...);
        return new[] { $"[STUB] {_steamName}: {GetLeaderboardScore(leaderboardId)}" };
    }

    // ========== CLOUD SAVES ==========

    public bool IsCloudSaveAvailable()
    {
        // return SteamRemoteStorage.IsCloudEnabledForApp();
        return true; // Stub
    }

    public bool WriteCloudFile(string fileName, byte[] data)
    {
        if (!_isInitialized) return false;
        // return SteamRemoteStorage.FileWrite(fileName, data, data.Length);
        GD.Print($"[Steam] Cloud write: {fileName} ({data.Length} bytes)");
        return true;
    }

    public byte[] ReadCloudFile(string fileName)
    {
        if (!_isInitialized) return null;
        // int size = SteamRemoteStorage.GetFileSize(fileName);
        // byte[] data = new byte[size];
        // SteamRemoteStorage.FileRead(fileName, data, size);
        // return data;
        return null;
    }

    public void SyncCloud()
    {
        if (!_isInitialized) return;
        GD.Print("[Steam] Cloud sync triggered");
        OnCloudSyncComplete?.Invoke("ok");
    }

    // ========== DRM ==========

    public bool ValidateAppOwnership()
    {
        if (!_isInitialized) return true;
        // return SteamApps.IsSubscribedApp(GetAppId());
        return true; // Stub — always true in dev
    }

    public int GetAppId()
    {
        // return SteamUtils.GetAppID().m_AppId;
        return 480; // Stub: Spacewar test app
    }

    public string GetDRMStatus()
    {
        return _isInitialized ? "SteamDRM_Active" : "Offline_NoDRM";
    }

    // ========== FRIENDS / SOCIAL ==========

    public string[] GetFriends()
    {
        if (!_isInitialized) return Array.Empty<string>();

        // int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        // string[] friends = new string[count];
        // for (int i = 0; i < count; i++)
        // {
        //     CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
        //     friends[i] = SteamFriends.GetFriendPersonaName(friendId);
        // }
        // return friends;

        return new[] { "[STUB] Friend1", "[STUB] Friend2" };
    }

    public void OpenFriendInviteOverlay()
    {
        if (!_isInitialized) return;
        // SteamFriends.ActivateGameOverlayInviteDialog(SteamUser.GetSteamID());
        GD.Print("[Steam] Friend invite overlay opened");
    }

    public void OpenOverlay(string section = "community")
    {
        if (!_isInitialized) return;
        // SteamFriends.ActivateGameOverlay(section);
        GD.Print($"[Steam] Overlay: {section}");
        OnOverlayToggled?.Invoke();
    }

    // ========== REMOTE PLAY ==========

    public bool IsRemotePlayTogether()
    {
        // return SteamApps.IsRemotePlayTogether() || SteamApps.IsSteamInBigPictureMode();
        return false;
    }

    // ========== GAME SERVER ==========

    public void SetGameServer(string serverName, string gameDescription)
    {
        if (!_isInitialized) return;
        // SteamGameServer.SetServerName(serverName);
        // SteamGameServer.SetGameDescription(gameDescription);
        // SteamGameServer.LogOnAnonymous();
    }

    // ========== STEAM INPUT ==========

    public bool HasSteamInput()
    {
        if (!_isInitialized) return false;
        // return SteamInput.IsSteamInputEnabled();
        return _isOnSteamDeck;
    }

    public string[] GetInputDevices()
    {
        if (!_isInitialized) return Array.Empty<string>();
        // int count = SteamInput.GetConnectedControllers();
        // return count > 0 ? new[] { "Controller_0" } : Array.Empty<string>();
        return Array.Empty<string>();
    }

    // ========== ACHIEVEMENT DEFINITIONS (for reference) ==========

    public Dictionary<string, string> GetAllAchievementDefs()
    {
        return new Dictionary<string, string>
        {
            ["ACH_WELCOME"] = "The Signal Calls — Complete the prologue",
            ["ACH_FIRST_STEPS"] = "Growing Stronger — Reach level 5",
            ["ACH_KAEL"] = "Chrome Salvation — Recruit Kael-7",
            ["ACH_MARA"] = "Rooted in Green — Recruit Mara",
            ["ACH_VEX"] = "Cold Vengeance — Recruit Vex",
            ["ACH_ECHO"] = "First Echo — Recruit Echo",
            ["ACH_SLOANE"] = "Scrap Queen — Recruit Sloane",
            ["ACH_JINX"] = "Spark of Genius — Recruit Jinx",
            ["ACH_HOLLOW"] = "Embrace the Void — Recruit Hollow",
            ["ACH_ARIS"] = "Archivist's Secret — Recruit Aris",
            ["ACH_UNIT734"] = "Chrome Ancient — Recruit Unit 734",
            ["ACH_MESSENGER"] = "The First Word — Recruit the Messenger",
            ["ACH_COMPLETE_ACT_I"] = "The Long Walk — Complete Act I",
            ["ACH_ALL_COMPANIONS"] = "Walker of Friends — Recruit all companions",
            ["ACH_NEMESIS_1"] = "Nemesis Slayer — Defeat your first Nemesis",
            ["ACH_NEMESIS_10"] = "Unbroken — Defeat 10 Nemeses",
            ["ACH_PURIFIED_ALLIED"] = "Chrome Ally — Reach Allied with Purified",
            ["ACH_ROOTED_ALLIED"] = "Verdant Ally — Reach Allied with Rooted",
            ["ACH_SCAVENGER_ALLIED"] = "Junk Trust — Reach Allied with Scavengers",
            ["ACH_HUB_MAX"] = "Master Builder — Fully upgrade one hub",
            ["ACH_RESEARCH_ALL"] = "Brilliant Mind — Complete all research",
            ["ACH_NG_PLUS_1"] = "The Walker Returns — Start NG+",
            ["ACH_NG_PLUS_5"] = "Commissioned — Reach NG+5",
            ["ACH_NG_PLUS_10"] = "Walker Eternal — Reach NG+10",
            ["ACH_ENDING_TRUTH"] = "The Truth Beneath — Unlock the Truth ending",
            ["ACH_ENDING_SACRIFICE"] = "The Cost — Complete the Sacrifice ending",
            ["ACH_ENDING_ALL"] = "All Paths Walked — Unlock all endings",
            ["ACH_COOP"] = "Stronger Together — Complete a co-op mission",
            ["ACH_LEVEL_20"] = "Signal Ascendant — Reach level 20",
            ["ACH_LEVEL_50"] = "Walking Legend — Reach level 50",
            ["ACH_SCRAP_10000"] = "Scrap Lord — Accumulate 10,000 scrap",
            ["ACH_QUEST_100"] = "Side Hustler — Complete 100 side quests"
        };
    }

    // ========== STAT DEFINITIONS ==========

    public Dictionary<string, (string, int)> GetStatDefs()
    {
        return new Dictionary<string, (string, int)>
        {
            ["stat_kills"] = ("Total Enemies Killed", 0),
            ["stat_nemesis_kills"] = ("Nemeses Defeated", 0),
            ["stat_deaths"] = ("Times Defeated", 0),
            ["stat_resurrections"] = ("Times Resurrected", 0),
            ["stat_damage_dealt"] = ("Total Damage Dealt", 0),
            ["stat_damage_taken"] = ("Total Damage Taken", 0),
            ["stat_healing"] = ("Total HP Healed", 0),
            ["stat_crit_hits"] = ("Critical Hits Landed", 0),
            ["stat_overwatch_kills"] = ("Overwatch Kills", 0),
            ["stat_missions_won"] = ("Missions Won", 0),
            ["stat_missions_lost"] = ("Missions Lost", 0),
            ["stat_travel_distance"] = ("Distance Traveled", 0),
            ["stat_scrap_collected"] = ("Scrap Collected", 0),
            ["stat_items_crafted"] = ("Items Crafted", 0),
            ["stat_mutations_gained"] = ("Mutations Acquired", 0),
            ["stat_dialogues_completed"] = ("Dialogues Completed", 0),
            ["stat_playtime_seconds"] = ("Total Play Time", 0)
        };
    }
}