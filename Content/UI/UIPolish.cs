using Godot;
using System.Collections.Generic;
using TheSignal.Systems;

namespace TheSignal.Content.UI;

[GlobalClass]
public partial class UIPolish : Node
{
    public static UIPolish Instance { get; private set; }

    // Tooltip
    private Control _tooltipPanel;
    private Label _tooltipTitle;
    private RichTextLabel _tooltipDesc;
    private TextureRect _tooltipIcon;
    private Godot.Timer _tooltipTimer;

    // Controller navigation
    private bool _controllerMode = false;
    private Control _currentFocus;

    // Accessibility
    public bool ColorblindMode { get; private set; } = false;
    public ColorblindType CurrentColorblindType { get; private set; } = ColorblindType.None;
    public float TextScale { get; private set; } = 1.0f;
    public float ContrastMultiplier { get; private set; } = 1.0f;
    public bool HighContrastMode { get; private set; } = false;
    public bool ReduceMotion { get; private set; } = false;
    public bool LargeTextMode { get; private set; } = false;

    // HUD animation tweens
    private Dictionary<string, Tween> _activeTweens = new();

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        CreateTooltip();
        DetectInputDevice();
    }

    // ========== TOOLTIP SYSTEM ==========

    private void CreateTooltip()
    {
        _tooltipPanel = new PanelContainer
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 1000
        };
        _tooltipPanel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);

        var vbox = new VBoxContainer();
        _tooltipTitle = new Label();
        _tooltipTitle.AddThemeFontSizeOverride("font_size", 14);
        _tooltipDesc = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            CustomMinimumSize = new Vector2(200, 0)
        };
        _tooltipDesc.AddThemeFontSizeOverride("font_size", 11);
        _tooltipIcon = new TextureRect { CustomMinimumSize = new Vector2(32, 32) };

        vbox.AddChild(_tooltipIcon);
        vbox.AddChild(_tooltipTitle);
        vbox.AddChild(_tooltipDesc);
        _tooltipPanel.AddChild(vbox);
        AddChild(_tooltipPanel);

        _tooltipTimer = new Godot.Timer { OneShot = true, WaitTime = 0.5f };
        _tooltipTimer.Timeout += ShowTooltip;
        AddChild(_tooltipTimer);
    }

    public void RegisterTooltip(Control target, string title, string description, Texture2D icon = null)
    {
        target.MouseEntered += () =>
        {
            _tooltipTitle.Text = title;
            _tooltipDesc.Text = description;
            if (icon != null) _tooltipIcon.Texture = icon;
            _tooltipTimer.Start();
        };
        target.MouseExited += () =>
        {
            _tooltipTimer.Stop();
            _tooltipPanel.Visible = false;
        };
        target.MouseFilter = Control.MouseFilterEnum.Pass;
    }

    public void RegisterTooltipOnFocus(Control target, string title, string description)
    {
        target.FocusEntered += () =>
        {
            _tooltipTitle.Text = title;
            _tooltipDesc.Text = description;
            ShowTooltip();
        };
        target.FocusExited += () => _tooltipPanel.Visible = false;
    }

    private void ShowTooltip()
    {
        _tooltipPanel.Visible = true;
        var mousePos = GetViewport().GetMousePosition();
        _tooltipPanel.GlobalPosition = mousePos + Vector2.One * 20;
        // Clamp to screen
        var screen = GetViewport().GetVisibleRect().Size;
        if (_tooltipPanel.GlobalPosition.X + _tooltipPanel.Size.X > screen.X)
            _tooltipPanel.GlobalPosition = new Vector2(screen.X - _tooltipPanel.Size.X - 10, _tooltipPanel.GlobalPosition.Y);
        if (_tooltipPanel.GlobalPosition.Y + _tooltipPanel.Size.Y > screen.Y)
            _tooltipPanel.GlobalPosition = new Vector2(_tooltipPanel.GlobalPosition.X, screen.Y - _tooltipPanel.Size.Y - 10);
    }

    // ========== CONTROLLER NAVIGATION ==========

    private void DetectInputDevice()
    {
        // Listen for controller input
        var input = InputManager.Instance;
        if (input != null)
        {
            // Check if any joypad is connected
            _controllerMode = Input.GetConnectedJoypads().Count > 0;
        }
    }

    public bool IsControllerMode => _controllerMode;

    public void SetControllerMode(bool active)
    {
        _controllerMode = active;
        Input.MouseMode = active ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
        GD.Print($"[UI Polish] Controller mode: {active}");
    }

    /// <summary>
    /// Sets up automatic controller navigation on a container.
    /// Uses the standard UI navigation system built into Godot.
    /// </summary>
    public void SetupFocusNavigation(Control root)
    {
        // Ensure all focusable children have consistent navigation
        var focusables = GetAllFocusable(root);
        for (int i = 0; i < focusables.Count; i++)
        {
            var btn = focusables[i] as Control;
            if (btn == null) continue;

            // Set focus neighbours for D-pad navigation
            if (i > 0) btn.FocusNeighborTop = focusables[i - 1].GetPath();
            if (i < focusables.Count - 1) btn.FocusNeighborBottom = focusables[i + 1].GetPath();
        }
    }

    private List<Control> GetAllFocusable(Control parent)
    {
        var result = new List<Control>();
        foreach (Node child in parent.GetChildren())
        {
            if (child is Control ctrl && ctrl.FocusMode != Control.FocusModeEnum.None)
                result.Add(ctrl);
            if (child.GetChildCount() > 0)
                result.AddRange(GetAllFocusable(ctrl));
        }
        return result;
    }

    // ========== ACCESSIBILITY ==========

    public void SetColorblindMode(ColorblindType type)
    {
        ColorblindMode = type != ColorblindType.None;
        CurrentColorblindType = type;
        GD.Print($"[Accessibility] Colorblind mode: {type}");

        // Apply color filters to UI
        var colorMatrix = GetColorblindMatrix(type);
        // Apply to viewport or UI canvas
    }

    public void SetTextScale(float scale)
    {
        TextScale = Mathf.Clamp(scale, 0.8f, 2.0f);
        GD.Print($"[Accessibility] Text scale: {TextScale:F2}x");
        GetTree().CallGroup("tooltips", "ThemeFontSize", (int)(12 * TextScale));
    }

    public void SetHighContrast(bool enabled)
    {
        HighContrastMode = enabled;
        ContrastMultiplier = enabled ? 1.5f : 1.0f;
        GD.Print($"[Accessibility] High contrast: {enabled}");
        // Apply theme overrides
    }

    public void SetReduceMotion(bool enabled)
    {
        ReduceMotion = enabled;
        GD.Print($"[Accessibility] Reduce motion: {enabled}");
    }

    public void SetLargeText(bool enabled)
    {
        LargeTextMode = enabled;
        TextScale = enabled ? 1.5f : 1.0f;
        SetTextScale(TextScale);
    }

    private Color GetColorblindMatrix(ColorblindType type)
    {
        return type switch
        {
            ColorblindType.Protanopia => new Color(0.567f, 0.433f, 0f),
            ColorblindType.Deuteranopia => new Color(0.625f, 0.375f, 0f),
            ColorblindType.Tritanopia => new Color(0.95f, 0.433f, 0.475f),
            _ => Colors.White
        };
    }

    // ========== HUD ANIMATIONS ==========

    public void AnimateHudElement(Control element, HudAnimation anim, float duration = 0.3f)
    {
        if (ReduceMotion) return;

        KillTween(element.Name);

        var tween = CreateTween().SetParallel();
        _activeTweens[element.Name] = tween;

        switch (anim)
        {
            case HudAnimation.FadeIn:
                element.Modulate = Colors.Transparent;
                tween.TweenProperty(element, "modulate:a", 1f, duration);
                break;

            case HudAnimation.FadeOut:
                tween.TweenProperty(element, "modulate:a", 0f, duration);
                tween.TweenCallback(Callable.From(() => element.Visible = false));
                break;

            case HudAnimation.SlideIn:
                element.Position = new Vector2(-element.Size.X, element.Position.Y);
                tween.TweenProperty(element, "position:x", 0f, duration).SetTrans(Tween.TransitionType.Back);
                break;

            case HudAnimation.Pulse:
                tween.TweenProperty(element, "scale", Vector2.One * 1.1f, duration * 0.5f);
                tween.TweenProperty(element, "scale", Vector2.One, duration * 0.5f);
                break;

            case HudAnimation.Shake:
                var originalPos = element.Position;
                tween.TweenProperty(element, "position:x", originalPos.X + 10, duration * 0.1f);
                tween.TweenProperty(element, "position:x", originalPos.X - 10, duration * 0.1f);
                tween.TweenProperty(element, "position:x", originalPos.X + 5, duration * 0.1f);
                tween.TweenProperty(element, "position:x", originalPos.X - 5, duration * 0.1f);
                tween.TweenProperty(element, "position:x", originalPos.X, duration * 0.1f);
                break;

            case HudAnimation.CountUp:
                // For numerical displays (HP, XP, etc.)
                break;
        }
    }

    public void KillTween(string name)
    {
        if (_activeTweens.TryGetValue(name, out var tween))
        {
            tween.Kill();
            _activeTweens.Remove(name);
        }
    }

    public void OnDamageTaken(Control hud, int currentHP, int maxHP)
    {
        // Red flash
        var flash = new ColorRect
        {
            Color = new Color(1, 0, 0, 0.3f),
            Size = hud.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        hud.AddChild(flash);
        var tween = CreateTween();
        tween.TweenProperty(flash, "modulate:a", 0f, 0.5f);
        tween.TweenCallback(Callable.From(() => flash.QueueFree()));

        // Shake HUD
        AnimateHudElement(hud, HudAnimation.Shake);
    }

    public void OnLevelUp(Control hud)
    {
        AnimateHudElement(hud, HudAnimation.Pulse);
        AnimateHudElement(hud, HudAnimation.FadeIn);
    }
}

public enum ColorblindType
{
    None,
    Protanopia,
    Deuteranopia,
    Tritanopia
}

public enum HudAnimation
{
    FadeIn,
    FadeOut,
    SlideIn,
    Pulse,
    Shake,
    CountUp
}