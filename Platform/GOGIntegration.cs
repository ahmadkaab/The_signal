using Godot;
using System;
using System.Collections.Generic;

namespace TheSignal.Platform;

/// <summary>
/// E3: GOG Galaxy integration — achievements, cloud saves, Galaxy overlay,
/// multiplayer lobby support. Requires GalaxyCSharpGlue / GOG Galaxy SDK.
/// All calls stubbed for development.
/// </summary>
[GlobalClass]
public partial class GOGIntegration : Node
{
    public static GOGIntegration Instance { get; private set; }

    private bool _isInitialized = false;
    private string _galaxyId = "";
    private string _displayName = "";
    private bool _isOverlayAvailable = false;

    private Dictionary<string, bool> _achievements = new();
    private Dictionary<string, int> _stats = new();
    private Dictionary<string, long> _leaderboards = new();

    // Events
    public event Action OnGalaxyInitialized;
    public event Action<string> OnAchievementUnlocked;
    public event Action OnOverlayToggled;
    public event Action<string> OnCloudSyncComplete;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    // ========== INITIALIZATION ==========

    public void Initialize(string clientId = "YOUR_GOG_CLIENT_ID", string clientSecret = "YOUR_GOG_CLIENT_SECRET")
    {
        GD.Print("[GOG] Initializing Galaxy...");

        try
        {
            // When GalaxySDK is linked:
            // GalaxyInstance.Init(new InitParams { clientID = clientId, clientSecret = clientSecret });
            // var auth = GalaxyInstance.User();
            // auth.SignInGalaxy(...);

            _isInitialized = true;
            _galaxyId = "GOG_STUB_ID";
            _displayName = "DevWalker_GOG";
            _isOverlayAvailable = true;

            OnGalaxyInitialized?.Invoke();
            GD.Print($"[GOG] Initialized: {_displayName} ({_galaxyId})");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GOG] Init error: {e.Message}. Running stub.");
            _isInitialized = true;
        }
    }

    public bool IsInitialized() => _isInitialized;
    public string GetGalaxyId() => _galaxyId;
    public string GetDisplayName() => _displayName;

    // ========== AUTH ==========

    public void SignIn()
    {
        if (!_isInitialized) return;
        // GalaxyInstance.User().SignInGalaxy();
        GD.Print("[GOG] Signed in (stub)");
    }

    public void SignOut()
    {
        if (!_isInitialized) return;
        // GalaxyInstance.User().SignOut();
        GD.Print("[GOG] Signed out");
    }

    // ========== ACHIEVEMENTS ==========

    public void UnlockAchievement(string achievementId)
    {
        if (!_isInitialized) return;
        if (_achievements.TryGetValue(achievementId, out bool unlocked) && unlocked) return;

        // GalaxyInstance.Stats().SetAchievement(achievementId);
        // GalaxyInstance.Stats().StoreStatsAndAchievements();

        _achievements[achievementId] = true;
        OnAchievementUnlocked?.Invoke(achievementId);
        GD.Print($"[GOG] Achievement: {achievementId}");
    }

    public bool IsAchievementUnlocked(string achievementId)
    {
        return _achievements.GetValueOrDefault(achievementId, false);
    }

    public Dictionary<string, string> GetAchievementDefs()
    {
        // Would query GOG Galaxy API for achievement list
        return new Dictionary<string, string>
        {
            ["gog_ach_welcome"] = "The Signal Calls — Complete the awakening",
            ["gog_ach_act1"] = "The Long Walk — Complete Act I",
            ["gog_ach_allies"] = "Gather the Party — Recruit 6 companions",
            ["gog_ach_legend"] = "Walker Eternal — Reach level 30"
        };
    }

    // ========== STATS ==========

    public void SetStat(string statId, int value)
    {
        if (!_isInitialized) return;
        _stats[statId] = value;
        // GalaxyInstance.Stats().SetStatInt(statId, value);
    }

    public int GetStat(string statId)
    {
        if (_stats.TryGetValue(statId, out int value)) return value;
        return 0;
    }

    public void FlushStats()
    {
        if (!_isInitialized) return;
        // GalaxyInstance.Stats().StoreStatsAndAchievements();
        GD.Print("[GOG] Stats flushed");
    }

    // ========== LEADERBOARDS ==========

    public void SetLeaderboardScore(string leaderboardId, long score)
    {
        if (!_isInitialized) return;
        // GalaxyInstance.Stats().SetLeaderboardScore(leaderboardId, score);
        _leaderboards[leaderboardId] = score;
        GD.Print($"[GOG] Leaderboard {leaderboardId}: {score}");
    }

    public long GetLeaderboardScore(string leaderboardId)
    {
        return _leaderboards.GetValueOrDefault(leaderboardId, 0);
    }

    // ========== OVERLAY ==========

    public bool IsOverlayAvailable() => _isOverlayAvailable;

    public void OpenOverlay()
    {
        if (!_isInitialized || !_isOverlayAvailable) return;
        // GalaxyInstance.Utils().ShowOverlay();
        GD.Print("[GOG] Overlay opened (stub)");
        OnOverlayToggled?.Invoke();
    }

    public void OpenStoreOverlay()
    {
        if (!_isInitialized || !_isOverlayAvailable) return;
        // GalaxyInstance.Utils().ShowOverlayWithWebPage("https://www.gog.com");
        GD.Print("[GOG] Store overlay (stub)");
    }

    // ========== CLOUD SAVES ==========

    public bool IsCloudSaveAvailable()
    {
        // return GalaxyInstance.CloudStorage().IsStorageAvailable();
        return true;
    }

    public bool WriteCloudFile(string fileName, byte[] data)
    {
        if (!_isInitialized) return false;
        // GalaxyInstance.CloudStorage().PutFile(fileName, data, data.Length);
        GD.Print($"[GOG] Cloud save: {fileName} ({data.Length} bytes)");
        return true;
    }

    public byte[] ReadCloudFile(string fileName)
    {
        if (!_isInitialized) return null;
        // uint size = GalaxyInstance.CloudStorage().GetFileSize(fileName);
        // byte[] data = new byte[size];
        // GalaxyInstance.CloudStorage().GetFile(fileName, data, size);
        // return data;
        return null;
    }

    public void SyncCloud()
    {
        if (!_isInitialized) return;
        GD.Print("[GOG] Cloud sync completed");
        OnCloudSyncComplete?.Invoke("ok");
    }

    // ========== MULTIPLAYER LOBBY ==========

    public void CreateLobby(string lobbyName, int maxPlayers = 4)
    {
        if (!_isInitialized) return;

        // var lobby = GalaxyInstance.Matchmaking();
        // lobby.CreateLobby(lobbyName, maxPlayers, true, LobbyType.PUBLIC);

        GD.Print($"[GOG] Lobby created: {lobbyName} ({maxPlayers} players)");
    }

    public void LeaveLobby()
    {
        if (!_isInitialized) return;
        // GalaxyInstance.Matchmaking().LeaveLobby(lobbyId);
        GD.Print("[GOG] Left lobby");
    }

    public string[] GetLobbyList()
    {
        if (!_isInitialized) return Array.Empty<string>();
        // var lobby = GalaxyInstance.Matchmaking();
        // lobby.RequestLobbyList();
        return new[] { "[STUB] Lobby_1", "[STUB] Lobby_2" };
    }

    // ========== GOG CONFIG ==========

    public void SetConfig(string clientId, string clientSecret)
    {
        GD.Print($"[GOG] Config set: client={clientId.Substring(0, 8)}...");
    }

    // ========== SHUTDOWN ==========

    public void Shutdown()
    {
        if (_isInitialized)
        {
            // GalaxyInstance.Free();
            GD.Print("[GOG] Galaxy shutdown");
        }
    }
}