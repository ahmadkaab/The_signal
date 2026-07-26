using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Scenes.Player;
using TheSignal.Combat.Units;
using TheSignal.Systems;

namespace TheSignal.Scenes.Exploration;

public partial class TutorialZone : Node3D
{
    [Export] public Marker3D PlayerStart { get; set; }
    [Export] public Marker3D KaelSpawn { get; set; }
    [Export] public Marker3D MaraSpawn { get; set; }
    [Export] public Marker3D EnemySpawn1 { get; set; }
    [Export] public Marker3D EnemySpawn2 { get; set; }
    [Export] public Area3D WaystationEntrance { get; set; }
    [Export] public Area3D GroveEntrance { get; set; }
    [Export] public Area3D FreeportEntrance { get; set; }
    [Export] public CanvasLayer TutorialUI { get; set; }

    private PlayerController _player;
    private UnitInstance _playerUnit;
    private UnitInstance _kael;
    private UnitInstance _mara;
    private UnitInstance _enemy1;
    private UnitInstance _enemy2;
    private int _tutorialStage = 0;
    private bool _combatStarted = false;
    private bool _combatResolved = false;
    private string _chosenCompanion = "";

    public override void _Ready()
    {
        InitializeTutorial();
        
        WaystationEntrance.BodyEntered += OnWaystationEntered;
        GroveEntrance.BodyEntered += OnGroveEntered;
        FreeportEntrance.BodyEntered += OnFreeportEntered;
    }

    private void InitializeTutorial()
    {
        // Spawn player
        _player = SpawnPlayer();
        _playerUnit = CreatePlayerUnitInstance();
        
        // Start prologue dialogue
        GameManager.Instance.DialogueManager.StartDialogue("PrologueAwakening");
        
        // Listen for dialogue events
        GameManager.Instance.DialogueManager.OnDialogueEnded += OnPrologueComplete;
    }

    private UnitInstance CreatePlayerUnitInstance()
    {
        // Create a UnitInstance for the player for combat purposes
        var unit = new UnitInstance();
        unit.UnitId = "player";
        unit.DisplayName = GameManager.Instance.Player.Name;
        unit.Type = UnitType.Player;
        unit.MaxHp = GameManager.Instance.Player.MaxHp;
        unit.CurrentHp = GameManager.Instance.Player.CurrentHp;
        unit.MaxAp = 6;
        unit.CurrentAp = 6;
        unit.Level = GameManager.Instance.Player.Level;
        unit.Initiative = 15;
        unit.MoveRange = 4;
        unit.WeaponDamage = 5;
        unit.Armor = 0;
        unit.CritChance = 5;
        unit.CritDamage = 50;
        return unit;
    }

    private PlayerController SpawnPlayer()
    {
        var playerScene = GD.Load<PackedScene>("res://Scenes/Player/PlayerController.tscn");
        var player = playerScene.Instantiate<PlayerController>();
        AddChild(player);
        player.GlobalPosition = PlayerStart.GlobalPosition;
        
        // Initialize player data from GameManager
        player.Initialize(GameManager.Instance.Player);
        
        // Give starter gear
        player.AddItem("pulse_pistol", 1);
        player.AddItem("stimulant", 3);
        player.AddItem("resonance_core_cracked", 1);
        
        return player;
    }

    private void OnPrologueComplete(string nodeName)
    {
        GameManager.Instance.DialogueManager.OnDialogueEnded -= OnPrologueComplete;
        
        // Check which companion was chosen via flags
        var flags = GameManager.Instance.WorldManager.WorldFlags;
        _chosenCompanion = flags.TryGetValue("chose_companion", out var choseVal) && choseVal ? "companion" : "none";
        
        // Start corridor walk with companion(s)
        StartCorridorWalk();
    }

    private void StartCorridorWalk()
    {
        _tutorialStage = 1;
        
        // Spawn companion(s) based on choice
        switch (_chosenCompanion)
        {
            case "kael":
                _kael = SpawnCompanion("kael", KaelSpawn.GlobalPosition);
                break;
            case "mara":
                _mara = SpawnCompanion("mara", MaraSpawn.GlobalPosition);
                break;
            case "both":
                _kael = SpawnCompanion("kael", KaelSpawn.GlobalPosition);
                _mara = SpawnCompanion("mara", MaraSpawn.GlobalPosition);
                break;
            case "none":
            default:
                // Solo - no companions
                break;
        }

        // Start companion banter dialogue
        if (_chosenCompanion == "kael")
            GameManager.Instance.DialogueManager.StartDialogue("CompanionRecruitment_Kael");
        else if (_chosenCompanion == "mara")
            GameManager.Instance.DialogueManager.StartDialogue("CompanionRecruitment_Mara");
        else if (_chosenCompanion == "both")
            GameManager.Instance.DialogueManager.StartDialogue("CompanionRecruitment_Both");
        else
            GameManager.Instance.DialogueManager.StartDialogue("CompanionRecruitment_None");
        
        GameManager.Instance.DialogueManager.OnDialogueEnded += OnCorridorDialogueComplete;
    }

    private UnitInstance SpawnCompanion(string companionId, Vector3 position)
    {
        var data = ResourceRegistry.Instance.GetCompanion(companionId);
        if (data == null) return null;

        var unitScene = data.UnitScene;
        if (unitScene == null) return null;

        var unit = unitScene.Instantiate<UnitInstance>();
        AddChild(unit);
        unit.GlobalPosition = position;
        unit.DisplayName = data.DisplayName;
        unit.CompanionId = data.CompanionId;
        unit.Level = data.StartingLevel;
        unit.Type = UnitType.Companion;
        
        // Add to party
        GameManager.Instance.Party.AddActiveCompanion(companionId, true);
        
        return unit;
    }

    private void OnCorridorDialogueComplete(string nodeName)
    {
        GameManager.Instance.DialogueManager.OnDialogueEnded -= OnCorridorDialogueComplete;
        
        // Trigger combat encounter
        StartTutorialCombat();
    }

    private void StartTutorialCombat()
    {
        if (_combatStarted) return;
        _combatStarted = true;
        _tutorialStage = 2;

        // Spawn enemies
        _enemy1 = SpawnEnemy("rust_mutant_scout", EnemySpawn1.GlobalPosition);
        _enemy2 = SpawnEnemy("rust_mutant_scout", EnemySpawn2.GlobalPosition);

        // Transition to combat scene
        GameManager.Instance.ChangeState(GameState.Combat);
        
        var combatManager = CombatManager.Instance;
        combatManager.OnCombatEnded += OnTutorialCombatEnded;

        var playerUnits = new List<UnitInstance> { _playerUnit };
        if (_kael != null) playerUnits.Add(_kael);
        if (_mara != null) playerUnits.Add(_mara);

        var enemyUnits = new List<UnitInstance> { _enemy1, _enemy2 };

        combatManager.StartCombat(playerUnits, enemyUnits, "tutorial_combat");
    }

    private UnitInstance SpawnEnemy(string enemyId, Vector3 position)
    {
        var data = ResourceRegistry.Instance.GetEnemy(enemyId);
        if (data == null) return null;

        var unitScene = data.UnitScene ?? GD.Load<PackedScene>("res://Combat/Units/UnitInstance.tscn");
        var unit = unitScene.Instantiate<UnitInstance>();
        AddChild(unit);
        unit.GlobalPosition = position;
        unit.EnemyId = data.EnemyId;
        unit.DisplayName = data.DisplayName;
        unit.Level = data.Level;
        unit.Type = UnitType.Enemy;
        return unit;
    }

    private void OnTutorialCombatEnded(CombatState state)
    {
        var combatManager = CombatManager.Instance;
        combatManager.OnCombatEnded -= OnTutorialCombatEnded;
        
        _combatResolved = true;
        _tutorialStage = 3;

        bool playerWon = state.EnemyUnits.TrueForAll(u => u.CurrentHp <= 0);
        
        if (playerWon)
        {
            // Post-combat dialogue
            if (_chosenCompanion == "kael")
                GameManager.Instance.DialogueManager.StartDialogue("tutorial_combat_victory_kael");
            else if (_chosenCompanion == "mara")
                GameManager.Instance.DialogueManager.StartDialogue("tutorial_combat_victory_mara");
            else if (_chosenCompanion == "both")
                GameManager.Instance.DialogueManager.StartDialogue("tutorial_combat_victory_both");
            else
                GameManager.Instance.DialogueManager.StartDialogue("tutorial_combat_victory_solo");

            GameManager.Instance.DialogueManager.OnDialogueEnded += OnVictoryDialogueComplete;
        }
        else
        {
            // Defeat - restart combat or game over
            GameManager.Instance.DialogueManager.StartDialogue("tutorial_defeat");
            GameManager.Instance.DialogueManager.OnDialogueEnded += OnDefeatDialogueComplete;
        }
    }

    private void OnVictoryDialogueComplete(string nodeName)
    {
        GameManager.Instance.DialogueManager.OnDialogueEnded -= OnVictoryDialogueComplete;
        
        // Grant XP and rewards
        GameManager.Instance.Player.GainXp(50);
        GameManager.Instance.Player.ResonanceFragments += 3;
        GameManager.Instance.Player.AddScrap(20);
        GameManager.Instance.Player.AddItem("stimulant", 2);

        // Transition to hub based on companion choice
        TransitionToHub();
    }

    private void OnDefeatDialogueComplete(string nodeName)
    {
        GameManager.Instance.DialogueManager.OnDialogueEnded -= OnDefeatDialogueComplete;
        
        // Restart combat
        _combatStarted = false;
        _combatResolved = false;
        _enemy1?.QueueFree();
        _enemy2?.QueueFree();
        
        // Small delay then restart
        var timer = GetTree().CreateTimer(2.0);
        timer.Timeout += StartTutorialCombat;
    }

    private void TransitionToHub()
    {
        _tutorialStage = 4;
        
        string hubScene = _chosenCompanion switch
        {
            "kael" => "res://Scenes/Exploration/Waystation.tscn",
            "mara" => "res://Scenes/Exploration/Grove.tscn",
            "both" => "res://Scenes/Exploration/HybridBase.tscn",
            _ => "res://Scenes/Exploration/Waystation.tscn" // solo defaults to waystation
        };

        // Fade out and load hub
        GD.Print("[TutorialZone] Fading out and transitioning to hub...");
        var fadeTimer = GetTree().CreateTimer(1.0f);
        fadeTimer.Timeout += () =>
        {
            GameManager.Instance.ChangeState(GameState.Exploration);
            GetTree().ChangeSceneToFile(hubScene);
        };
    }

    private void OnWaystationEntered(Node3D body)
    {
        if (body == _player && _chosenCompanion != "mara")
        {
            TransitionToHub();
        }
    }

    private void OnGroveEntered(Node3D body)
    {
        if (body == _player && _chosenCompanion != "kael")
        {
            TransitionToHub();
        }
    }

    private void OnFreeportEntered(Node3D body)
    {
        if (body == _player)
        {
            // Freeport accessible to all
            GD.Print("[TutorialZone] Transitioning to Freeport...");
            var freeportTimer = GetTree().CreateTimer(1.0f);
            freeportTimer.Timeout += () =>
            {
                GetTree().ChangeSceneToFile("res://Scenes/UI/HubZone.tscn");
                GameManager.Instance.ChangeState(GameState.Exploration);
            };
        }
    }

    public override void _ExitTree()
    {
        WaystationEntrance.BodyEntered -= OnWaystationEntered;
        GroveEntrance.BodyEntered -= OnGroveEntered;
        FreeportEntrance.BodyEntered -= OnFreeportEntered;
    }
}
