using Godot;
using System;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Core.Progression;
using TheSignal.Core.Save;
using TheSignal.Data;
using TheSignal.Combat;
using TheSignal.Combat.Units;
using TheSignal.Systems;

namespace TheSignal;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public GameState PreviousState { get; private set; } = GameState.MainMenu;

    // Core systems
    public Player Player { get; private set; }
    public Party Party { get; private set; }
    public WorldManager WorldManager { get; private set; }
    public QuestManager QuestManager { get; private set; }
    public DialogueManager DialogueManager { get; private set; }
    public CombatManager CombatManager { get; private set; }
    public SectorMapManager SectorMapManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public ResourceRegistry ResourceRegistry { get; private set; }
    public ProgressionFormulas ProgressionFormulas { get; private set; }

    // Events
    public event Action<GameState> OnStateChanged;
    public event Action OnGameSaved;
    public event Action OnGameLoaded;
    public event Action OnNewGameStarted;

    public override void _EnterTree()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready()
    {
        InitializeSystems();
        ChangeState(GameState.MainMenu);
    }

    private void InitializeSystems()
    {
        GD.Print("[GameManager] Initializing systems...");

        // Load progression formulas
        ProgressionFormulas = GD.Load<ProgressionFormulas>("res://Data/Progression/Formulas/ProgressionFormulas.tres");

        // Initialize core systems
        ResourceRegistry = GetNode<ResourceRegistry>("%ResourceRegistry");
        WorldManager = GetNode<WorldManager>("%WorldManager");
        QuestManager = GetNode<QuestManager>("%QuestManager");
        DialogueManager = GetNode<DialogueManager>("%DialogueManager");
        CombatManager = GetNode<CombatManager>("%CombatManager");
        SectorMapManager = GetNode<SectorMapManager>("%SectorMapManager");
        UIManager = GetNode<UIManager>("%UIManager");

        // Initialize player and party
        Player = new Player();
        Party = new Party();

        // Initialize save system
        SaveManager.Initialize();

        GD.Print("[GameManager] All systems initialized");
    }

    public void ChangeState(GameState newState)
    {
        PreviousState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        GD.Print($"[GameManager] State: {PreviousState} -> {CurrentState}");

        // Handle state-specific logic
        switch (newState)
        {
            case GameState.MainMenu:
                UIManager.ShowMainMenu();
                break;
            case GameState.Exploration:
                UIManager.ShowExplorationHUD();
                break;
            case GameState.Combat:
                UIManager.ShowCombatHUD();
                break;
            case GameState.Dialogue:
                UIManager.ShowDialogueUI();
                break;
            case GameState.SectorMap:
                UIManager.ShowSectorMap();
                break;
            case GameState.Paused:
                UIManager.ShowPauseMenu();
                break;
        }
    }

    public void ReturnToPreviousState()
    {
        ChangeState(PreviousState);
    }

    // ========== GAME FLOW ==========

    public void StartNewGame()
    {
        GD.Print("[GameManager] Starting new game...");

        // Reset all systems
        Player.Initialize();
        Party.Initialize();
        WorldManager.Initialize();
        QuestManager.Initialize();
        SectorMapManager.Initialize();

        // Start prologue quest
        QuestManager.StartQuest("prologue_awakening");

        // Start prologue dialogue
        DialogueManager.StartDialogue("PrologueAwakening");

        ChangeState(GameState.Dialogue);
        OnNewGameStarted?.Invoke();
    }

    public void LoadGame(string saveName)
    {
        GD.Print($"[GameManager] Loading game: {saveName}");

        var data = SaveManager.LoadGame(saveName);
        if (data == null)
        {
            GD.PrintErr($"[GameManager] Failed to load save: {saveName}");
            return;
        }

        // Load all systems
        Player.LoadSaveData(data.Player);
        Party.LoadSaveData(data.Party);
        WorldManager.LoadSaveData(data.World);
        QuestManager.LoadSaveData(data.Quests);
        SectorMapManager.LoadSaveData(data.SectorMap);

        ChangeState(GameState.Exploration);
        OnGameLoaded?.Invoke();
    }

    public void SaveGame(string saveName)
    {
        GD.Print($"[GameManager] Saving game: {saveName}");

        var data = new GameSaveData
        {
            Player = Player.GetSaveData(),
            Party = Party.GetSaveData(),
            World = WorldManager.GetSaveData(),
            Quests = QuestManager.GetSaveData(),
            SectorMap = SectorMapManager.GetSaveData()
        };

        SaveManager.SaveGame(saveName, data);
        OnGameSaved?.Invoke();
    }

    public void QuickSave()
    {
        SaveManager.Autosave(new GameSaveData
        {
            Player = Player.GetSaveData(),
            Party = Party.GetSaveData(),
            World = WorldManager.GetSaveData(),
            Quests = QuestManager.GetSaveData(),
            SectorMap = SectorMapManager.GetSaveData()
        });
    }

    // ========== COMBAT INTEGRATION ==========

    public void EnterCombat(List<UnitInstance> playerUnits, List<UnitInstance> enemyUnits, string zoneId)
    {
        GD.Print($"[GameManager] Entering combat in {zoneId}");

        ChangeState(GameState.Combat);
        CombatManager.StartCombat(playerUnits, enemyUnits, zoneId);
    }

    public void ExitCombat(bool playerWon)
    {
        GD.Print($"[GameManager] Exiting combat. Player won: {playerWon}");

        if (playerWon)
        {
            // Grant rewards handled by CombatManager
        }
        else
        {
            // Handle defeat - return to last safe zone
            WorldManager.ReturnToLastSafeZone();
        }

        ChangeState(GameState.Exploration);
    }

    // ========== DIALOGUE INTEGRATION ==========

    public void StartDialogue(string yarnNode, string startNode = "")
    {
        DialogueManager.StartDialogue(yarnNode);
        ChangeState(GameState.Dialogue);
    }

    public void EndDialogue()
    {
        DialogueManager.StopDialogue();
        ReturnToPreviousState();
    }

    // ========== SECTOR MAP INTEGRATION ==========

    public void OpenSectorMap()
    {
        ChangeState(GameState.SectorMap);
    }

    public void CloseSectorMap()
    {
        ReturnToPreviousState();
    }

    public void TravelToZone(string zoneId)
    {
        if (SectorMapManager.TravelTo(zoneId))
        {
            // Load local zone scene
            LoadLocalZone(zoneId);
        }
    }

    private void LoadLocalZone(string zoneId)
    {
        var zone = ResourceRegistry.Instance.GetZone(zoneId);
        if (zone?.LocalZoneScene != null)
        {
            var scene = zone.LocalZoneScene.Instantiate<Node3D>();
            GetTree().Root.AddChild(scene);
            // Position player at zone entrance
        }
    }

    // ========== QUICK SAVE/LOAD ==========

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("quick_save"))
        {
            QuickSave();
            UIManager.ShowNotification("Game Quick Saved");
        }
        else if (@event.IsActionPressed("quick_load"))
        {
            var data = SaveManager.LoadAutosave();
            if (data != null)
            {
                Player.LoadSaveData(data.Player);
                Party.LoadSaveData(data.Party);
                WorldManager.LoadSaveData(data.World);
                QuestManager.LoadSaveData(data.Quests);
                SectorMapManager.LoadSaveData(data.SectorMap);
                UIManager.ShowNotification("Quick Load Complete");
            }
        }
        else if (@event.IsActionPressed("toggle_pause"))
        {
            if (CurrentState == GameState.Paused)
                ReturnToPreviousState();
            else
                ChangeState(GameState.Paused);
        }
    }
}

// ========== SAVE DATA STRUCTURES ==========

public class GameSaveData
{
    public SaveMeta Meta { get; set; } = new();
    public PlayerSaveData Player { get; set; }
    public PartySaveData Party { get; set; }
    public WorldSaveData World { get; set; }
    public QuestSaveData Quests { get; set; }
    public SectorMapSaveData SectorMap { get; set; }
}