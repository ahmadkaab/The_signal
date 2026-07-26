using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Systems;

namespace TheSignal.Scenes.UI;

public partial class SectorMap : Control
{
    [Export] public Control ZoneNodesContainer { get; set; }
    [Export] public Line2D ConnectionLines { get; set; }
    [Export] public Label SectorLabel { get; set; }
    [Export] public ProgressBar FuelBar { get; set; }
    [Export] public Label FuelText { get; set; }
    [Export] public Label ScrapText { get; set; }
    [Export] public PanelContainer ZoneInfoPanel { get; set; }
    [Export] public Label ZoneNameLabel { get; set; }
    [Export] public Label ZoneDescLabel { get; set; }
    [Export] public ProgressBar CorruptionBar { get; set; }
    [Export] public Button TravelButton { get; set; }
    [Export] public Button FastTravelButton { get; set; }
    [Export] public Label CurrentZoneLabel { get; set; }
    [Export] public PackedScene ZoneNodeScene { get; set; }
    [Export] public AudioStreamPlayer SectorMapAudio { get; set; }

    private Dictionary<string, ZoneNode> _zoneNodes = new();
    private string _selectedZoneId = "";
    private string _hoveredZoneId = "";

    public override void _Ready()
    {
        TravelButton.Pressed += OnTravelPressed;
        FastTravelButton.Pressed += OnFastTravelPressed;
        SectorMapManager.Instance.OnZoneChanged += OnZoneChanged;
        SectorMapManager.Instance.OnZoneDiscovered += (zoneId) => { OnZoneDiscovered(zoneId); };
        SectorMapManager.Instance.OnCorruptionChanged += OnCorruptionChanged;
        SectorMapManager.Instance.OnFuelChanged += OnFuelChanged;
        SectorMapManager.Instance.OnScrapChanged += OnScrapChanged;

        BuildMap();
        UpdateUI();
    }

    public override void _ExitTree()
    {
        SectorMapManager.Instance.OnZoneChanged -= OnZoneChanged;
        SectorMapManager.Instance.OnZoneDiscovered -= (zoneId) => { OnZoneDiscovered(zoneId); };
        SectorMapManager.Instance.OnCorruptionChanged -= OnCorruptionChanged;
        SectorMapManager.Instance.OnFuelChanged -= OnFuelChanged;
        SectorMapManager.Instance.OnScrapChanged -= OnScrapChanged;
    }

    private void BuildMap()
    {
        foreach (var child in ZoneNodesContainer.GetChildren())
            child.QueueFree();

        _zoneNodes.Clear();
        ConnectionLines.ClearPoints();

        var zones = ResourceRegistry.Instance.AllZones;
        var connections = SectorMapManager.Instance.Connections;

        // First pass: create all nodes
        foreach (var kvp in zones)
        {
            var zone = kvp.Value;
            var node = ZoneNodeScene.Instantiate<ZoneNode>();
            node.Initialize(kvp.Key, zone);
            ZoneNodesContainer.AddChild(node);
            _zoneNodes[kvp.Key] = node;
        }

        // Second pass: draw connections
        DrawConnections();
    }

    private void DrawConnections()
    {
        ConnectionLines.ClearPoints();

        foreach (var conn in SectorMapManager.Instance.Connections)
        {
            if (_zoneNodes.TryGetValue(conn.FromZoneId, out var fromNode) &&
                _zoneNodes.TryGetValue(conn.ToZoneId, out var toNode))
            {
                var fromPos = fromNode.GlobalPosition + ZoneNodesContainer.GlobalPosition;
                var toPos = toNode.GlobalPosition + ZoneNodesContainer.GlobalPosition;

                // Draw line
                ConnectionLines.AddPoint(fromPos);
                ConnectionLines.AddPoint(toPos);

                // Could add arrow heads, dash patterns for locked connections, etc.
            }
        }
    }

    private void OnZoneHovered(string zoneId)
    {
        _hoveredZoneId = zoneId;
        ShowZoneInfo(zoneId);
    }

    private void OnZoneSelected(string zoneId)
    {
        _selectedZoneId = zoneId;
        ShowZoneInfo(zoneId);
        UpdateTravelButton();
    }

    private void ShowZoneInfo(string zoneId)
    {
        if (!SectorMapManager.Instance.ZoneStates.TryGetValue(zoneId, out var state))
            return;

        var zone = state.ZoneResource;
        ZoneInfoPanel.Visible = true;
        ZoneNameLabel.Text = zone.DisplayName;
        ZoneDescLabel.Text = zone.Description;

        // Corruption bar: -100 to 100, center at 0
        CorruptionBar.Value = state.CorruptionLevel;
        if (state.CorruptionLevel <= -50)
            CorruptionBar.Modulate = new Color(0.2f, 1f, 0.3f);
        else if (state.CorruptionLevel >= 50)
            CorruptionBar.Modulate = new Color(1f, 0.3f, 0.2f);
        else
            CorruptionBar.Modulate = new Color(1f, 0.8f, 0.2f);

        UpdateTravelButton();
    }

    private void UpdateTravelButton()
    {
        bool canTravel = SectorMapManager.Instance.CanTravelTo(SectorMapManager.Instance.CurrentZoneId, _selectedZoneId);
        TravelButton.Disabled = !canTravel;

        if (canTravel)
        {
            var conn = SectorMapManager.Instance.Connections.Find(c =>
                c.FromZoneId == SectorMapManager.Instance.CurrentZoneId &&
                c.ToZoneId == _selectedZoneId);
            TravelButton.Text = conn != null ? $"Travel ({conn.FuelCost} Fuel)" : "Travel";
        }
        else
        {
            TravelButton.Text = "Cannot Travel";
        }

        // Fast travel only to discovered, cleared zones with fast travel connection
        var targetState = SectorMapManager.Instance.ZoneStates.GetValueOrDefault(_selectedZoneId);
        FastTravelButton.Disabled = targetState == null || !targetState.Discovered || !targetState.Cleared;
    }

    private void OnTravelPressed()
    {
        SectorMapManager.Instance.TravelTo(_selectedZoneId);
        // Close sector map, enter local zone
        GameManager.Instance.ChangeState(GameState.Exploration);
        // Local zone loading handled by GameManager/WorldManager
    }

    private void OnFastTravelPressed()
    {
        // Instant travel, higher fuel cost, only to cleared zones
        var conn = SectorMapManager.Instance.Connections.Find(c =>
            c.FromZoneId == SectorMapManager.Instance.CurrentZoneId &&
            c.ToZoneId == _selectedZoneId);

        if (conn != null)
        {
            SectorMapManager.Instance.Fuel -= conn.FuelCost * 2;
            SectorMapManager.Instance.TravelTo(_selectedZoneId);
            GameManager.Instance.ChangeState(GameState.Exploration);
        }
    }

    private void OnZoneChanged(string zoneId)
    {
        UpdateNodeVisuals();
        UpdateUI();
    }

    private void OnZoneDiscovered(string zoneId)
    {
        if (_zoneNodes.TryGetValue(zoneId, out var node))
        {
            node.SetDiscovered(true);
        }
        UpdateNodeVisuals();
    }

    private void OnCorruptionChanged(string zoneId, float level)
    {
        if (_zoneNodes.TryGetValue(zoneId, out var node))
        {
            node.SetCorruptionLevel(level);
        }

        if (zoneId == _selectedZoneId || zoneId == _hoveredZoneId)
        {
            ShowZoneInfo(zoneId);
        }
    }

    private void OnFuelChanged(int fuel)
    {
        FuelBar.Value = fuel;
        FuelText.Text = $"{fuel}/{SectorMapManager.Instance.MaxFuel}";
        UpdateTravelButton();
    }

    private void OnScrapChanged(int scrap)
    {
        ScrapText.Text = $"{scrap} SCRAP";
    }

    private void UpdateUI()
    {
        var mgr = SectorMapManager.Instance;
        FuelBar.MaxValue = mgr.MaxFuel;
        FuelBar.Value = mgr.Fuel;
        FuelText.Text = $"{mgr.Fuel}/{mgr.MaxFuel}";
        ScrapText.Text = $"{mgr.Scrap} SCRAP";

        var currentState = mgr.ZoneStates.GetValueOrDefault(mgr.CurrentZoneId);
        if (currentState != null)
        {
            CurrentZoneLabel.Text = $"Current: {currentState.ZoneResource.DisplayName}";
        }

        UpdateNodeVisuals();
    }

    private void UpdateNodeVisuals()
    {
        var mgr = SectorMapManager.Instance;

        foreach (var kvp in _zoneNodes)
        {
            var zoneId = kvp.Key;
            var node = kvp.Value;
            var state = mgr.ZoneStates.GetValueOrDefault(zoneId);

            if (state == null) continue;

            bool isCurrent = zoneId == mgr.CurrentZoneId;
            bool isSelected = zoneId == _selectedZoneId;
            bool canTravel = mgr.CanTravelTo(mgr.CurrentZoneId, zoneId);

            node.UpdateVisuals(isCurrent, isSelected, canTravel, state);
        }
    }
}