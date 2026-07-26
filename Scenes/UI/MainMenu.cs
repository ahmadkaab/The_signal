using Godot;
using TheSignal.Core.Save;
using TheSignal.Systems;

namespace TheSignal.Scenes.UI;

public partial class MainMenu : Control
{
    [Export] public Button NewGameButton { get; set; }
    [Export] public Button LoadGameButton { get; set; }
    [Export] public Button SettingsButton { get; set; }
    [Export] public Button QuitButton { get; set; }
    [Export] public VBoxContainer SaveSlotsContainer { get; set; }
    [Export] public PackedScene SaveSlotScene { get; set; }
    [Export] public Control LoadMenu { get; set; }
    [Export] public Control SettingsMenu { get; set; }
    [Export] public AnimationPlayer AnimPlayer { get; set; }

    public override void _Ready()
    {
        NewGameButton.Pressed += OnNewGame;
        LoadGameButton.Pressed += OnLoadGame;
        SettingsButton.Pressed += OnSettings;
        QuitButton.Pressed += OnQuit;

        RefreshSaveSlots();

        if (AnimPlayer != null)
            AnimPlayer.Play("fade_in");
    }

    private void OnNewGame()
    {
        GameManager.Instance.StartNewGame();
    }

    private void OnLoadGame()
    {
        LoadMenu.Visible = true;
        RefreshSaveSlots();
    }

    private void OnSettings()
    {
        SettingsMenu.Visible = true;
    }

    private void OnQuit()
    {
        GetTree().Quit();
    }

    private void RefreshSaveSlots()
    {
        foreach (Node child in SaveSlotsContainer.GetChildren())
            child.QueueFree();

        var saves = SaveManager.GetSaveFiles();
        foreach (var save in saves)
        {
            var slot = SaveSlotScene.Instantiate<SaveSlot>();
            slot.Initialize(save);
            slot.LoadRequested += OnLoadSave;
            SaveSlotsContainer.AddChild(slot);
        }
    }

    private void OnLoadSave(SaveInfo save)
    {
        GameManager.Instance.LoadGame(save.FileName);
        LoadMenu.Visible = false;
    }
}

public class SaveInfo
{
    public string FileName { get; set; }
    public string DisplayName { get; set; }
    public int PlaytimeSeconds { get; set; }
    public int PlayerLevel { get; set; }
    public string CurrentZone { get; set; }
    public string Timestamp { get; set; }
}