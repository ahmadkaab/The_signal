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

    public event Action<TheSignal.Scenes.UI.SaveInfo> LoadRequested;

    public TheSignal.Scenes.UI.SaveInfo SaveInfo { get; private set; }

    public void Initialize(TheSignal.Scenes.UI.SaveInfo info)
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
        SaveManager.DeleteSave(int.TryParse(SaveInfo.FileName.Replace("save_", "").Replace(".json", ""), out int slot) ? slot : 0);
        QueueFree();
    }

    private string FormatTime(int seconds)
    {
        int h = seconds / 3600;
        int m = (seconds % 3600) / 60;
        return $"{h}h {m}m";
    }
}