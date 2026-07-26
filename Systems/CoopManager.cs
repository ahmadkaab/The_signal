using Godot;
using System.Collections.Generic;
using System.Linq;
using TheSignal.Core;
using TheSignal.Data;

namespace TheSignal.Systems;

/// <summary>
/// D5: Co-op / Multiplayer — drop-in/drop-out 2-player co-op,
/// shared progression, combat turn sync, host migration.
/// Uses Godot's ENet-based multiplayer API.
/// </summary>
[GlobalClass]
public partial class CoopManager : Node
{
    public static CoopManager Instance { get; private set; }

    // Connection state
    private ENetMultiplayerPeer _peer;
    private bool _isHost = false;
    private bool _isClient = false;
    private int _connectedPlayers = 0;
    private const int MAX_PLAYERS = 2;

    // Player roster
    private Dictionary<int, CoopPlayerInfo> _players = new(); // peer_id -> info
    private int _localPeerId = 1;
    private int _hostPeerId = 1;

    // Combat sync
    private bool _isCombatActive = false;
    private int _currentTurnPlayer = -1;
    private float _syncTimer = 0f;
    private const float SYNC_INTERVAL = 0.5f;

    // Progression sync
    private CoopProgressionState _progressionState = new();
    private Queue<ProgressionDelta> _pendingSyncs = new();

    // Host migration
    private int _newHostPeerId = -1;
    private bool _isMigrating = false;
    private float _migrationTimeout = 10f;

    // Events
    public event System.Action<CoopPlayerInfo> OnPlayerJoined;
    public event System.Action<int> OnPlayerLeft;
    public event System.Action OnCoopStarted;
    public event System.Action OnCoopEnded;
    public event System.Action<int> OnTurnChanged;
    public event System.Action<string> OnCombatSyncReceived;
    public event System.Action OnHostMigrationStarted;
    public event System.Action<int> OnHostMigrationComplete;
    public event System.Action<string> OnProgressionSync;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;

        // Register multiplayer peer signals
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    // ========== HOST / JOIN ==========

    public void HostGame(int port = 23456)
    {
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(port, MAX_PLAYERS);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Coop] Failed to host: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        _isHost = true;
        _localPeerId = 1;
        _hostPeerId = 1;

        // Register host player
        _players[1] = new CoopPlayerInfo
        {
            PeerId = 1,
            PlayerName = GetLocalPlayerName(),
            IsHost = true,
            IsReady = true,
            CurrentZone = "S09_GRAVEYARD",
            TeamIndex = 0
        };

        _connectedPlayers = 1;
        OnCoopStarted?.Invoke();
        GD.Print($"[Coop] Hosting on port {port}");
    }

    public void JoinGame(string ip, int port = 23456)
    {
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(ip, port);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Coop] Failed to join: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        _isClient = true;
        _localPeerId = Multiplayer.GetUniqueId();
        GD.Print($"[Coop] Joining {ip}:{port} as peer {_localPeerId}");
    }

    public void LeaveGame()
    {
        if (_isHost)
        {
            _peer?.Close();
        }
        else
        {
            Multiplayer.MultiplayerPeer = null;
        }

        _players.Clear();
        _connectedPlayers = 0;
        _isHost = false;
        _isClient = false;
        OnCoopEnded?.Invoke();
        GD.Print("[Coop] Left game");
    }

    // ========== PEER EVENTS ==========

    private void OnPeerConnected(long id)
    {
        int peerId = (int)id;
        if (!_isHost) return;

        if (_connectedPlayers >= MAX_PLAYERS)
        {
            GD.Print($"[Coop] Rejecting {peerId}: max players reached");
            // Send rejection via RPC
            RpcId(peerId, nameof(RpcRejectJoin), "Server full");
            // Force disconnect
            Multiplayer.MultiplayerPeer?.DisconnectPeer(peerId);
            return;
        }

        _players[peerId] = new CoopPlayerInfo
        {
            PeerId = peerId,
            PlayerName = $"Player_{peerId}",
            IsHost = false,
            IsReady = false,
            CurrentZone = "S09_GRAVEYARD",
            TeamIndex = 1
        };

        _connectedPlayers++;
        OnPlayerJoined?.Invoke(_players[peerId]);
        GD.Print($"[Coop] Player {peerId} connected ({_connectedPlayers}/{MAX_PLAYERS})");

        // Send host game state
        Rpc(nameof(RpcSyncGameState), SerializeGameState());
    }

    private void OnPeerDisconnected(long id)
    {
        int peerId = (int)id;
        if (_players.TryGetValue(peerId, out var player))
        {
            OnPlayerLeft?.Invoke(peerId);
            _players.Remove(peerId);
            _connectedPlayers--;
            GD.Print($"[Coop] Player {peerId} disconnected ({_connectedPlayers}/{MAX_PLAYERS})");

            // Host migration: if host left, reassign
            if (peerId == _hostPeerId && _isClient && _connectedPlayers > 0)
            {
                StartHostMigration();
            }
        }
    }

    private void OnConnectedToServer()
    {
        _isClient = true;
        _localPeerId = Multiplayer.GetUniqueId();
        GD.Print($"[Coop] Connected to server as peer {_localPeerId}");

        // Request player info from host
        RpcId(1, nameof(RpcRequestPlayerInfo), _localPeerId);
    }

    private void OnConnectionFailed()
    {
        GD.PrintErr("[Coop] Connection failed");
        OnCoopEnded?.Invoke();
    }

    private void OnServerDisconnected()
    {
        GD.Print("[Coop] Server disconnected");
        _isClient = false;

        if (!_isMigrating)
        {
            OnCoopEnded?.Invoke();
        }
    }

    // ========== HOST MIGRATION ==========

    private void StartHostMigration()
    {
        _isMigrating = true;
        _migrationTimeout = 10f;
        OnHostMigrationStarted?.Invoke();

        // Client becomes new host
        _isHost = true;
        _isClient = false;
        _hostPeerId = _localPeerId;

        // Recreate server
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(23456, MAX_PLAYERS);
        if (error == Error.Ok)
        {
            Multiplayer.MultiplayerPeer = _peer;
            _players[_localPeerId].IsHost = true;
            OnHostMigrationComplete?.Invoke(_localPeerId);
            GD.Print($"[Coop] Host migration: peer {_localPeerId} is new host");
        }
        else
        {
            GD.PrintErr($"[Coop] Host migration failed: {error}");
            OnCoopEnded?.Invoke();
        }

        _isMigrating = false;
    }

    public override void _Process(double delta)
    {
        if (_isMigrating)
        {
            _migrationTimeout -= (float)delta;
            if (_migrationTimeout <= 0)
            {
                _isMigrating = false;
                GD.Print("[Coop] Host migration timed out");
                OnCoopEnded?.Invoke();
            }
        }

        // Combat sync broadcasts
        if (_isCombatActive && _isHost && _connectedPlayers > 1)
        {
            _syncTimer += (float)delta;
            if (_syncTimer >= SYNC_INTERVAL)
            {
                _syncTimer = 0f;
                Rpc(nameof(RpcSyncCombatState), SerializeCombatState());
            }
        }
    }

    // ========== COMBAT SYNC ==========

    public void StartCombatSync(string encounterId)
    {
        _isCombatActive = true;
        _syncTimer = 0f;

        if (_connectedPlayers > 1)
        {
            Rpc(nameof(RpcStartCombat), encounterId);
            GD.Print($"[Coop] Combat started: {encounterId}");
        }
    }

    public void EndCombatSync()
    {
        _isCombatActive = false;
        if (_connectedPlayers > 1)
        {
            Rpc(nameof(RpcEndCombat));
        }
    }

    public void SyncTurnAction(int actorPeerId, string actionJson)
    {
        if (_connectedPlayers > 1)
        {
            Rpc(nameof(RpcReceiveAction), actorPeerId, actionJson);
            OnCombatSyncReceived?.Invoke(actionJson);
        }
    }

    public void SetPlayerTurn(int peerId)
    {
        _currentTurnPlayer = peerId;
        OnTurnChanged?.Invoke(peerId);
    }

    public bool IsPlayerTurn(int peerId)
    {
        return _currentTurnPlayer == peerId;
    }

    // ========== PROGRESSION SYNC ==========

    public void SyncProgression(ProgressionDelta delta)
    {
        if (_connectedPlayers > 1)
        {
            // Apply locally first
            ApplyProgressionDelta(delta);
            // Broadcast to other players
            Rpc(nameof(RpcSyncProgression), delta.Serialize());
        }
    }

    private void ApplyProgressionDelta(ProgressionDelta delta)
    {
        if (delta.XpGained > 0)
            _progressionState.TotalXp += delta.XpGained;
        if (delta.ScrapGained > 0)
            _progressionState.TotalScrap += delta.ScrapGained;
        if (!string.IsNullOrEmpty(delta.CompanionRecruited))
            _progressionState.RecruitedCompanions.Add(delta.CompanionRecruited);
        if (!string.IsNullOrEmpty(delta.QuestCompleted))
            _progressionState.CompletedQuests.Add(delta.QuestCompleted);
        if (!string.IsNullOrEmpty(delta.ZoneDiscovered))
            _progressionState.DiscoveredZones.Add(delta.ZoneDiscovered);

        foreach (var item in delta.ItemsGained)
        {
            if (!_progressionState.CollectedItems.Contains(item))
                _progressionState.CollectedItems.Add(item);
        }

        OnProgressionSync?.Invoke($"XP:{delta.XpGained} Scrap:{delta.ScrapGained}");
    }

    public CoopProgressionState GetSharedProgression() => _progressionState;

    // ========== RPCs ==========

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcRejectJoin(string reason)
    {
        GD.Print($"[Coop] Join rejected: {reason}");
        OnCoopEnded?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcRequestPlayerInfo(int peerId)
    {
        if (!_isHost) return;
        RpcId(peerId, nameof(RpcSendPlayerInfo), SerializePlayerInfo());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcSendPlayerInfo(string json)
    {
        // Deserialize and update player list
        // TODO: JSON deserialization
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcSyncGameState(string json)
    {
        GD.Print("[Coop] Game state sync received");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RpcSyncCombatState(string json)
    {
        // Update combat visual state from host
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcStartCombat(string encounterId)
    {
        _isCombatActive = true;
        GD.Print($"[Coop] Remote combat started: {encounterId}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcEndCombat()
    {
        _isCombatActive = false;
        GD.Print("[Coop] Remote combat ended");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcReceiveAction(int peerId, string actionJson)
    {
        GD.Print($"[Coop] Action from peer {peerId}: {actionJson.Left(50)}...");
        OnCombatSyncReceived?.Invoke(actionJson);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcSyncProgression(string deltaJson)
    {
        // Deserialize and apply progression delta
        // TODO: JSON deserialization
    }

    // ========== SERIALIZATION ==========

    private string SerializeGameState()
    {
        // Simplified — would use JSON in production
        return $"state:{_connectedPlayers}:zone:{_progressionState.DiscoveredZones.Count}";
    }

    private string SerializeCombatState()
    {
        return $"combat:{_currentTurnPlayer}:turn:{_syncTimer}";
    }

    private string SerializePlayerInfo()
    {
        return $"host:{_hostPeerId}:players:{_connectedPlayers}";
    }

    private string GetLocalPlayerName()
    {
        return $"Walker_{GD.Randi() % 1000}";
    }

    // ========== QUERIES ==========

    public bool IsHost() => _isHost;
    public bool IsClient() => _isClient;
    public bool IsCoopActive() => _connectedPlayers >= 2;
    public int GetPlayerCount() => _connectedPlayers;
    public int GetLocalPeerId() => _localPeerId;
    public int GetCurrentTurnPlayer() => _currentTurnPlayer;

    public CoopPlayerInfo GetPlayer(int peerId)
    {
        return _players.GetValueOrDefault(peerId);
    }

    public List<CoopPlayerInfo> GetAllPlayers()
    {
        return _players.Values.ToList();
    }
}

// ========== DATA CLASSES ==========

public class CoopPlayerInfo
{
    public int PeerId { get; set; }
    public string PlayerName { get; set; } = "";
    public bool IsHost { get; set; }
    public bool IsReady { get; set; }
    public string CurrentZone { get; set; } = "";
    public int TeamIndex { get; set; }
    public int Level { get; set; } = 1;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int ActionPoints { get; set; } = 6;
}

public class CoopProgressionState
{
    public int TotalXp { get; set; }
    public int TotalScrap { get; set; }
    public List<string> RecruitedCompanions { get; set; } = new();
    public List<string> CompletedQuests { get; set; } = new();
    public List<string> DiscoveredZones { get; set; } = new();
    public List<string> CollectedItems { get; set; } = new();
    public int CompletedEncounters { get; set; }
    public int DefeatedNemeses { get; set; }
}

public class ProgressionDelta
{
    public int XpGained { get; set; }
    public int ScrapGained { get; set; }
    public string CompanionRecruited { get; set; } = "";
    public string QuestCompleted { get; set; } = "";
    public string ZoneDiscovered { get; set; } = "";
    public List<string> ItemsGained { get; set; } = new();

    public string Serialize()
    {
        return $"xp:{XpGained}|scrap:{ScrapGained}|cmp:{CompanionRecruited}|q:{QuestCompleted}|z:{ZoneDiscovered}|items:{string.Join(",", ItemsGained)}";
    }

    public static ProgressionDelta Deserialize(string data)
    {
        var delta = new ProgressionDelta();
        var parts = data.Split('|');
        foreach (var part in parts)
        {
            var kv = part.Split(':');
            if (kv.Length < 2) continue;
            switch (kv[0])
            {
                case "xp": int.TryParse(kv[1], out int xpVal); delta.XpGained = xpVal; break;
                case "scrap": int.TryParse(kv[1], out int scrapVal); delta.ScrapGained = scrapVal; break;
                case "cmp": delta.CompanionRecruited = kv[1]; break;
                case "q": delta.QuestCompleted = kv[1]; break;
                case "z": delta.ZoneDiscovered = kv[1]; break;
                case "items": delta.ItemsGained = kv[1].Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList(); break;
            }
        }
        return delta;
    }
}