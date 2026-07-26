using Godot;
using TheSignal.Core;

namespace TheSignal.Systems;

public partial class UIManager : Node
{
    public static UIManager Instance { get; private set; }

    // UI Scenes
    [Export] public PackedScene MainMenuScene { get; set; }
    [Export] public PackedScene ExplorationHUDScene { get; set; }
    [Export] public PackedScene CombatHUDScene { get; set; }
    [Export] public PackedScene DialogueUIScene { get; set; }
    [Export] public PackedScene SectorMapScene { get; set; }
    [Export] public PackedScene PauseMenuScene { get; set; }
    [Export] public PackedScene NotificationScene { get; set; }
    [Export] public PackedScene LoadingScreenScene { get; set; }

    // Active UI instances
    private Control _mainMenu;
    private Control _explorationHUD;
    private Control _combatHUD;
    private Control _dialogueUI;
    private Control _sectorMap;
    private Control _pauseMenu;
    private CanvasLayer _notificationLayer;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;

        _notificationLayer = new CanvasLayer { Layer = 100 };
        AddChild(_notificationLayer);
    }

    // ========== SCREEN MANAGEMENT ==========

    public void ShowMainMenu()
    {
        HideAllScreens();
        if (_mainMenu == null && MainMenuScene != null)
        {
            _mainMenu = MainMenuScene.Instantiate<Control>();
            AddChild(_mainMenu);
        }
        _mainMenu?.Show();
    }

    public void ShowExplorationHUD()
    {
        HideAllScreens();
        if (_explorationHUD == null && ExplorationHUDScene != null)
        {
            _explorationHUD = ExplorationHUDScene.Instantiate<Control>();
            AddChild(_explorationHUD);
        }
        _explorationHUD?.Show();
    }

    public void ShowCombatHUD()
    {
        HideAllScreens();
        if (_combatHUD == null && CombatHUDScene != null)
        {
            _combatHUD = CombatHUDScene.Instantiate<Control>();
            AddChild(_combatHUD);
        }
        _combatHUD?.Show();
    }

    public void ShowDialogueUI()
    {
        HideAllScreens();
        if (_dialogueUI == null && DialogueUIScene != null)
        {
            _dialogueUI = DialogueUIScene.Instantiate<Control>();
            AddChild(_dialogueUI);
        }
        _dialogueUI?.Show();
    }

    public void ShowSectorMap()
    {
        HideAllScreens();
        if (_sectorMap == null && SectorMapScene != null)
        {
            _sectorMap = SectorMapScene.Instantiate<Control>();
            AddChild(_sectorMap);
        }
        _sectorMap?.Show();
    }

    public void ShowPauseMenu()
    {
        if (_pauseMenu == null && PauseMenuScene != null)
        {
            _pauseMenu = PauseMenuScene.Instantiate<Control>();
            AddChild(_pauseMenu);
        }
        _pauseMenu?.Show();
    }

    public void HidePauseMenu()
    {
        _pauseMenu?.Hide();
    }

    private void HideAllScreens()
    {
        _mainMenu?.Hide();
        _explorationHUD?.Hide();
        _combatHUD?.Hide();
        _dialogueUI?.Hide();
        _sectorMap?.Hide();
    }

    // ========== NOTIFICATIONS ==========

    public void ShowNotification(string message, float duration = 3f, NotificationType type = NotificationType.Info)
    {
        if (NotificationScene == null) return;

        var notification = NotificationScene.Instantiate<Control>();
        var label = notification.GetNodeOrNull<Label>("Label");
        if (label != null) label.Text = message;

        // Color by type
        var panel = notification.GetNodeOrNull<PanelContainer>("Panel");
        if (panel != null)
        {
            panel.Modulate = type switch
            {
                NotificationType.Info => new Color(0.2f, 0.6f, 1f),
                NotificationType.Success => new Color(0.2f, 0.8f, 0.3f),
                NotificationType.Warning => new Color(1f, 0.7f, 0.2f),
                NotificationType.Error => new Color(1f, 0.3f, 0.2f),
                NotificationType.Quest => new Color(0.8f, 0.4f, 1f),
                NotificationType.Loot => new Color(1f, 0.8f, 0.2f),
                _ => Colors.White
            };
        }

        _notificationLayer.AddChild(notification);

        // Animate in
        notification.Modulate = new Color(1, 1, 1, 0);
        var tween = GetTree().CreateTween();
        tween.TweenProperty(notification, "modulate:a", 1f, 0.3f);
        tween.TweenCallback(Callable.From(() => notification.Modulate = Colors.White)).SetDelay(duration);
        tween.TweenProperty(notification, "modulate:a", 0f, 0.3f);
        tween.TweenCallback(Callable.From(() => notification.QueueFree()));
    }

    public void ShowQuestNotification(string questName, QuestNotificationType type)
    {
        string message = type switch
        {
            QuestNotificationType.Started => $"Quest Started: {questName}",
            QuestNotificationType.Updated => $"Quest Updated: {questName}",
            QuestNotificationType.Completed => $"Quest Completed: {questName}",
            QuestNotificationType.Failed => $"Quest Failed: {questName}",
            _ => questName
        };
        ShowNotification(message, 5f, NotificationType.Quest);
    }

    public void ShowLootNotification(string itemName, int count = 1)
    {
        string message = count > 1 ? $"Obtained: {itemName} x{count}" : $"Obtained: {itemName}";
        ShowNotification(message, 4f, NotificationType.Loot);
    }

    public void ShowXPGain(int amount)
    {
        ShowNotification($"XP Gained: +{amount}", 2f, NotificationType.Success);
    }

    // ========== LOADING SCREEN ==========

    public void ShowLoadingScreen(string tip = "")
    {
        if (LoadingScreenScene != null)
        {
            var loading = LoadingScreenScene.Instantiate<Control>();
            var tipLabel = loading.GetNodeOrNull<Label>("TipLabel");
            if (tipLabel != null) tipLabel.Text = tip;
            AddChild(loading);
        }
    }

    public void HideLoadingScreen()
    {
        foreach (Node child in GetChildren())
        {
            if (((string)child.Name).Contains("LoadingScreen"))
                child.QueueFree();
        }
    }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Quest,
    Loot
}

public enum QuestNotificationType
{
    Started,
    Updated,
    Completed,
    Failed
}