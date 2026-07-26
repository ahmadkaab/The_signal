using Godot;
using TheSignal.Core.Save;
using TheSignal.Systems;

namespace TheSignal.Scenes.UI;

public partial class SaveSlot : PanelContainer
{
    [Export] public Label SaveName { get; set; }
    [Export] public Label PlaytimeLabel { get; set; }
    [Export] public Label LevelLabel { get; set; }
    [Export] public Label ZoneLabel { get; set; }
    [Export] public Button LoadButton { get; set; }
    [Export] public Button DeleteButton { get; set; }

    public event Action<SaveInfo> LoadRequested;

    public SaveInfo SaveInfo { get; private set; }

    public void Initialize(SaveInfo info)
    {
        SaveInfo = info;
        SaveName.Text = info.DisplayName;
        PlaytimeLabel.Text = FormatTime(info.PlaytimeSeconds);
        LevelLabel.Text = $"Lv. {info.PlayerLevel}";
        ZoneLabel.Text = info.CurrentZone;

        LoadButton.Pressed += () => LoadRequested?.Invoke(info);
        DeleteButton.Pressed += OnDelete;
    }

    private void OnDelete()
    {
        SaveManager.DeleteSave(SaveInfo.FileName);
        QueueFree();
    }

    private string FormatTime(int seconds)
    {
        int h = seconds / 3600;
        int m = (seconds % 3600) / 60;
        return $"{h}h {m}m";
    }
}