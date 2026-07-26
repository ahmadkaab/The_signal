using Godot;
using TheSignal.Data;
using TheSignal.Systems;

namespace TheSignal.Scenes.UI;

public partial class ZoneNode : PanelContainer
{
    [Export] public TextureRect BaseIcon { get; set; }
    [Export] public TextureRect StateIcon { get; set; }
    [Export] public TextureRect CurrentMarker { get; set; }
    [Export] public Label Label { get; set; }
    [Export] public TextureRect EncounterMarker { get; set; }
    [Export] public TextureRect QuestMarker { get; set; }
    [Export] public AudioStreamPlayer AudioPlayer { get; set; }

    private string _zoneId;
    private ZoneResource _zoneResource;

    public string ZoneId => _zoneId;

    public void Initialize(string zoneId, ZoneResource zoneResource)
    {
        _zoneId = zoneId;
        _zoneResource = zoneResource;
        Label.Text = zoneResource.DisplayName;
        UpdateVisuals(false, false, false, null);
    }

    public void UpdateVisuals(bool isCurrent, bool isSelected, bool canTravel, ZoneState state)
    {
        CurrentMarker.Visible = isCurrent;

        if (state != null)
        {
            float corruption = state.CorruptionLevel;

            if (corruption <= -50)
            {
                // Cleansed - blue/white
                StateIcon.Texture = GD.Load<Texture2D>("res://Assets/Art/UI/zone_node_cleansed.png");
                StateIcon.Visible = true;
                BaseIcon.Modulate = new Color(0.4f, 0.8f, 1f, 1f);
            }
            else if (corruption >= 50)
            {
                // Corrupted - red/orange
                StateIcon.Texture = GD.Load<Texture2D>("res://Assets/Art/UI/zone_node_corrupted.png");
                StateIcon.Visible = true;
                BaseIcon.Modulate = new Color(1f, 0.3f, 0.2f, 1f);
            }
            else
            {
                // Neutral
                StateIcon.Visible = false;
                BaseIcon.Modulate = state.Discovered ? new Color(1f, 1f, 1f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            // Encounter/Quest markers
            EncounterMarker.Visible = state.HasActiveEncounter;
            QuestMarker.Visible = state.HasActiveQuest;

            // Modulate based on travel availability
            if (!state.Discovered)
            {
                SelfModulate = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
            else if (canTravel)
            {
                SelfModulate = new Color(0.8f, 1f, 0.8f, 1f);
            }
            else
            {
                SelfModulate = Colors.White;
            }
        }

        // Selection highlight
        if (isSelected)
        {
            AddThemeStyleboxOverride("panel", CreateHighlightStylebox());
        }
        else
        {
            AddThemeStyleboxOverride("panel", new StyleBoxFlat());
        }
    }

    private StyleBoxFlat CreateHighlightStylebox()
    {
        var style = new StyleBoxFlat();
        style.BorderWidthBottom = 3;
        style.BorderWidthTop = 3;
        style.BorderWidthLeft = 3;
        style.BorderWidthRight = 3;
        style.BorderColor = new Color(0.2f, 0.8f, 1f, 1f);
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        return style;
    }

    public void SetDiscovered(bool discovered)
    {
        // Trigger discovery animation
        if (discovered)
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate", Colors.White, 0.5f);
        }
    }

    public void SetCorruptionLevel(float level)
    {
        // Update visual corruption state
        if (level <= -50)
        {
            StateIcon.Texture = GD.Load<Texture2D>("res://Assets/Art/UI/zone_node_cleansed.png");
            StateIcon.Visible = true;
        }
        else if (level >= 50)
        {
            StateIcon.Texture = GD.Load<Texture2D>("res://Assets/Art/UI/zone_node_corrupted.png");
            StateIcon.Visible = true;
        }
        else
        {
            StateIcon.Visible = false;
        }
    }

    public void PlayHoverSound()
    {
        if (AudioPlayer?.Stream != null)
            AudioPlayer.Play();
    }
}