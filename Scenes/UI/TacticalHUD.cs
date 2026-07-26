using Godot;
using TheSignal.Core;
using TheSignal.Core.Stats;
using TheSignal.Core.Progression;
using TheSignal.Combat.Units;
using TheSignal.Data;
using TheSignal.Systems;

namespace TheSignal.Scenes.UI;

public partial class TacticalHUD : Control
{
    [Export] public Label UnitNameLabel { get; set; }
    [Export] public ProgressBar HpBar { get; set; }
    [Export] public Label HpText { get; set; }
    [Export] public ProgressBar ApBar { get; set; }
    [Export] public Label ApText { get; set; }
    [Export] public GridContainer AbilityBar { get; set; }
    [Export] public PackedScene AbilityButtonScene { get; set; }
    [Export] public Label TurnOrderLabel { get; set; }
    [Export] public Button EndTurnButton { get; set; }
    [Export] public Button OverwatchButton { get; set; }
    [Export] public Label CombatLogLabel { get; set; }
    [Export] public TextureRect TargetPreview { get; set; }
    [Export] public Label DamagePreviewLabel { get; set; }

    private UnitInstance _selectedUnit;
    private AbilityResource _selectedAbility;

    public override void _Ready()
    {
        EndTurnButton.Pressed += () => CombatManager.Instance.EndTurn();
        OverwatchButton.Pressed += () => CombatManager.Instance.EnterOverwatch(_selectedUnit);
    }

    public void UpdateForUnit(UnitInstance unit)
    {
        _selectedUnit = unit;
        UnitNameLabel.Text = unit.Name;
        HpBar.MaxValue = unit.MaxHp;
        HpBar.Value = unit.CurrentHp;
        HpText.Text = $"{unit.CurrentHp}/{unit.MaxHp}";
        ApBar.MaxValue = unit.MaxAp;
        ApBar.Value = unit.CurrentAp;
        ApText.Text = $"{unit.CurrentAp}/{unit.MaxAp}";

        RebuildAbilityBar(unit);
    }

    private void RebuildAbilityBar(UnitInstance unit)
    {
        foreach (Node child in AbilityBar.GetChildren())
            child.QueueFree();

        foreach (string abilityId in unit.AbilityIds)
        {
            var ability = ResourceRegistry.Instance.GetAbility(abilityId);
            if (ability == null) continue;

            var btn = AbilityButtonScene.Instantiate<AbilityButton>();
            btn.Initialize(ability, unit);
            btn.Clicked += OnAbilitySelected;
            AbilityBar.AddChild(btn);
        }
    }

    private void OnAbilitySelected(AbilityResource ability)
    {
        _selectedAbility = ability;
        CombatManager.Instance.SelectAbility(ability);
        UpdateTargetPreview(ability);
    }

    private void UpdateTargetPreview(AbilityResource ability)
    {
        // Show valid targets, range indicator, damage preview
    }

    public void UpdateTurnOrder(List<UnitInstance> queue, int currentIndex)
    {
        string text = "Turn Order: ";
        for (int i = 0; i < queue.Count; i++)
        {
            if (i == currentIndex) text += $"[b]{queue[i].Name}[/b]";
            else text += queue[i].Name;
            if (i < queue.Count - 1) text += " > ";
        }
        TurnOrderLabel.Text = text;
    }

    public void AddCombatLog(string message)
    {
        CombatLogLabel.Text = $"[ {System.DateTime.Now:HH:mm} ] {message}\n" + CombatLogLabel.Text;
    }
}

public partial class AbilityButton : Button
{
    [Export] public TextureRect Icon { get; set; }
    [Export] public Label CooldownLabel { get; set; }
    [Export] public Label ApCostLabel { get; set; }
    [Export] public ColorRect CooldownOverlay { get; set; }

    public event Action<AbilityResource> Clicked;

    private AbilityResource _ability;
    private UnitInstance _unit;

    public void Initialize(AbilityResource ability, UnitInstance unit)
    {
        _ability = ability;
        _unit = unit;

        Icon.Texture = ability.Icon;
        ApCostLabel.Text = ability.ApCost.ToString();
        UpdateCooldown();

        Pressed += () => Clicked?.Invoke(_ability);
    }

    public void UpdateCooldown()
    {
        int cd = _unit.AbilityCooldowns.GetValueOrDefault(_ability.AbilityId, 0);
        if (cd > 0)
        {
            CooldownOverlay.Visible = true;
            CooldownLabel.Text = cd.ToString();
            Disabled = true;
        }
        else
        {
            CooldownOverlay.Visible = false;
            Disabled = _unit.CurrentAp < _ability.ApCost;
        }
    }
}