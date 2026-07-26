using Godot;
using System.Collections.Generic;

namespace TheSignal.Systems;

public partial class InputManager : Node
{
    public static InputManager Instance { get; private set; }

    // Input actions (defined in project.godot)
    public const string MoveUp = "move_up";
    public const string MoveDown = "move_down";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Interact = "interact";
    public const string Dodge = "dodge";
    public const string Sprint = "sprint";
    public const string Jump = "jump";
    public const string Crouch = "crouch";
    public const string QuickSave = "quick_save";
    public const string QuickLoad = "quick_load";
    public const string Pause = "pause";
    public const string MenuJournal = "menu_journal";
    public const string MenuInventory = "menu_inventory";
    public const string MenuCharacter = "menu_character";
    public const string TacticalConfirm = "tactical_confirm";
    public const string TacticalCancel = "tactical_cancel";
    public const string TacticalEndTurn = "tactical_end_turn";
    public const string TacticalOverwatch = "tactical_overwatch";
    public const string TacticalAbility1 = "tactical_ability_1";
    public const string TacticalAbility2 = "tactical_ability_2";
    public const string TacticalAbility3 = "tactical_ability_3";
    public const string TacticalAbility4 = "tactical_ability_4";

    public Vector2 MovementInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool DodgePressed { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool PausePressed { get; private set; }

    public event Action OnPausePressed;
    public event Action OnQuickSave;
    public event Action OnQuickLoad;
    public event Action OnInteract;
    public event Action OnDodge;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        // Exploration movement input
        float x = 0, y = 0;
        if (Input.IsActionPressed(MoveRight)) x += 1;
        if (Input.IsActionPressed(MoveLeft)) x -= 1;
        if (Input.IsActionPressed(MoveDown)) y += 1;
        if (Input.IsActionPressed(MoveUp)) y -= 1;
        
        MovementInput = new Vector2(x, y).Normalized();

        IsSprinting = Input.IsActionPressed(Sprint);

        // Edge-triggered inputs
        bool interactNow = Input.IsActionJustPressed(Interact);
        bool dodgeNow = Input.IsActionJustPressed(Dodge);
        bool jumpNow = Input.IsActionJustPressed(Jump);
        bool crouchNow = Input.IsActionPressed(Crouch);
        bool pauseNow = Input.IsActionJustPressed(Pause);
        bool quickSaveNow = Input.IsActionJustPressed(QuickSave);
        bool quickLoadNow = Input.IsActionJustPressed(QuickLoad);

        if (interactNow) { InteractPressed = true; OnInteract?.Invoke(); }
        if (dodgeNow) { DodgePressed = true; OnDodge?.Invoke(); }
        if (jumpNow) JumpPressed = true;
        CrouchHeld = crouchNow;
        if (pauseNow) OnPausePressed?.Invoke();
        if (quickSaveNow) OnQuickSave?.Invoke();
        if (quickLoadNow) OnQuickLoad?.Invoke();
    }

    public void ResetInputFlags()
    {
        InteractPressed = false;
        DodgePressed = false;
        JumpPressed = false;
    }

    public Vector2 GetMousePosition() => GetViewport().GetMousePosition();
}