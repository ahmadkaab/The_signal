using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Systems;

namespace TheSignal.Combat.Units;

[GlobalClass]
public partial class UnitInstance : CharacterBody3D
{
    [Export] public string UnitId { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public UnitType Type { get; set; }
    [Export] public string EnemyId { get; set; }
    [Export] public string CompanionId { get; set; }

    // Combat Stats
    [Export] public int MaxHp { get; set; } = 100;
    [Export] public int CurrentHp { get; set; } = 100;
    [Export] public int MaxAp { get; set; } = 6;
    [Export] public int CurrentAp { get; set; } = 6;
    [Export] public int Armor { get; set; } = 0;
    [Export] public int Evasion { get; set; } = 0;
    [Export] public int Accuracy { get; set; } = 80;
    [Export] public int CritChance { get; set; } = 5;
    [Export] public int CritDamage { get; set; } = 50;
    [Export] public int Initiative { get; set; } = 10;
    [Export] public int MoveRange { get; set; } = 3;
    [Export] public int WeaponDamage { get; set; } = 5;
    [Export] public DamageType WeaponDamageType { get; set; } = DamageType.Physical;

    // Resistances
    [Export] public float ResistPhysical { get; set; } = 0f;
    [Export] public float ResistResonance { get; set; } = 0f;
    [Export] public float ResistFire { get; set; } = 0f;
    [Export] public float ResistPoison { get; set; } = 0f;
    [Export] public float ResistShock { get; set; } = 0f;
    [Export] public float ResistPsychic { get; set; } = 0f;

    // Progression
    [Export] public int Level { get; set; } = 1;
    [Export] public int Exp { get; set; } = 0;
    public Dictionary<string, int> BaseStats { get; set; } = new();

    // State
    [Export] public Vector2I GridPosition { get; set; }
    public List<string> AbilityIds { get; set; } = new();
    public Dictionary<string, int> AbilityCooldowns { get; set; } = new();
    public Dictionary<string, StatusEffectInstance> ActiveStatusEffects { get; set; } = new();
    [Export] public bool HasActedThisTurn { get; set; } = false;
    [Export] public bool IsInOverwatch { get; set; } = false;
    [Export] public Vector2I OverwatchDirection { get; set; }
    [Export] public int OverwatchRange { get; set; }

    // Components
    private AnimationPlayer _animPlayer;
    private MeshInstance3D _bodyMesh;
    private Label3D _nameLabel;
    private ProgressBar _hpBar;
    private Node3D _selectionIndicator;
    private Node3D _apIndicator;

    public UnitInstance Stats => this;

    public Vector3 TargetWorldPosition { get; private set; }
    private bool _isMoving = false;
    private float _moveSpeed = 5f;

    public override void _Ready()
    {
        _animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _bodyMesh = GetNode<MeshInstance3D>("BodyMesh");
        _nameLabel = GetNode<Label3D>("NameLabel");
        _hpBar = GetNode<ProgressBar>("HPBar");
        _selectionIndicator = GetNode<Node3D>("SelectionIndicator");
        _apIndicator = GetNode<Node3D>("APIndicator");

        _nameLabel.Text = DisplayName;
        UpdateHpBar();
        SetSelected(false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isMoving)
        {
            Vector3 velocity = (TargetWorldPosition - GlobalPosition).Normalized() * _moveSpeed;
            if ((TargetWorldPosition - GlobalPosition).Length() < 0.1f)
            {
                GlobalPosition = TargetWorldPosition;
                _isMoving = false;
                _animPlayer.Play("idle");
            }
            else
            {
                Velocity = velocity;
                MoveAndSlide();
            }
        }
    }

    public void Initialize(CombatGrid grid, UnitData data)
    {
        UnitId = data.UnitId;
        DisplayName = data.DisplayName;
        Type = data.Type;
        EnemyId = data.EnemyId;
        CompanionId = data.CompanionId;

        MaxHp = data.MaxHp;
        CurrentHp = data.CurrentHp;
        MaxAp = data.MaxAp;
        CurrentAp = data.CurrentAp;
        Armor = data.Armor;
        Evasion = data.Evasion;
        Accuracy = data.Accuracy;
        CritChance = data.CritChance;
        CritDamage = data.CritDamage;
        Initiative = data.Initiative;
        MoveRange = data.MoveRange;
        WeaponDamage = data.WeaponDamage;
        WeaponDamageType = data.WeaponDamageType;

        ResistPhysical = data.ResistPhysical;
        ResistResonance = data.ResistResonance;
        ResistFire = data.ResistFire;
        ResistPoison = data.ResistPoison;
        ResistShock = data.ResistShock;
        ResistPsychic = data.ResistPsychic;

        AbilityIds = new List<string>(data.AbilityIds);
        GridPosition = data.GridPosition;

        // Position on grid
        GlobalPosition = grid.GridToWorld(GridPosition);
        TargetWorldPosition = GlobalPosition;
    }

    public void MoveTo(Vector2I targetGridPos, CombatGrid grid)
    {
        GridPosition = targetGridPos;
        TargetWorldPosition = grid.GridToWorld(targetGridPos);
        _isMoving = true;
        _animPlayer.Play("walk");
    }

    public bool SpendAp(int amount)
    {
        if (CurrentAp >= amount)
        {
            CurrentAp -= amount;
            UpdateApIndicator();
            return true;
        }
        return false;
    }

    public void RestoreAp(int amount)
    {
        CurrentAp = Mathf.Min(CurrentAp + amount, MaxAp);
        UpdateApIndicator();
    }

    public void ResetAp()
    {
        CurrentAp = MaxAp;
        HasActedThisTurn = false;
        IsInOverwatch = false;
        UpdateApIndicator();
    }

    public void TakeDamage(int amount, DamageType type, UnitInstance attacker = null)
    {
        // Apply armor
        float damage = amount;
        damage = Mathf.Max(1, damage - Armor * 0.5f);

        // Apply resistances
        float resist = type switch
        {
            DamageType.Physical => ResistPhysical,
            DamageType.Resonance => ResistResonance,
            DamageType.Fire => ResistFire,
            DamageType.Poison => ResistPoison,
            DamageType.Shock => ResistShock,
            DamageType.Psychic => ResistPsychic,
            DamageType.True => 0f,
            _ => 0f
        };
        damage *= (1f - Mathf.Clamp(resist, 0f, 0.9f));

        // Check evasion
        if (GD.Randf() * 100 < Evasion)
        {
            ShowFloatingText("Miss!", Colors.Gray);
            return;
        }

        // Check crit
        bool isCrit = GD.Randf() * 100 < (attacker?.CritChance ?? CritChance);
        if (isCrit)
        {
            damage *= 1f + (attacker?.CritDamage ?? CritDamage) / 100f;
        }

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
        CurrentHp = Mathf.Max(0, CurrentHp - finalDamage);
        UpdateHpBar();

        if (isCrit)
            ShowFloatingText($"CRIT {finalDamage}!", new Color(1, 0.8f, 0.2f));
        else
            ShowFloatingText($"{finalDamage}", Colors.Red);

        if (CurrentHp <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        UpdateHpBar();
        ShowFloatingText($"+{amount}", Colors.Green);
    }

    public void ApplyStatusEffect(StatusEffectInstance effect)
    {
        if (ActiveStatusEffects.TryGetValue(effect.EffectId, out var existing))
        {
            existing.Stacks = Mathf.Min(existing.Stacks + effect.Stacks, effect.MaxStacks);
            existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, effect.RemainingTurns);
        }
        else
        {
            ActiveStatusEffects[effect.EffectId] = effect;
        }
    }

    public void RemoveStatusEffect(string effectId)
    {
        ActiveStatusEffects.Remove(effectId);
    }

    public void ProcessTurnStart()
    {
        HasActedThisTurn = false;

        // Process status effects
        var toRemove = new List<string>();
        foreach (var kvp in ActiveStatusEffects)
        {
            kvp.Value.RemainingTurns--;
            if (kvp.Value.RemainingTurns <= 0)
                toRemove.Add(kvp.Key);

            // Apply per-turn effects
            ApplyStatusEffectTick(kvp.Value);
        }
        foreach (var id in toRemove)
            ActiveStatusEffects.Remove(id);

        // Reduce cooldowns
        var cdKeys = new List<string>(AbilityCooldowns.Keys);
        foreach (var key in cdKeys)
        {
            AbilityCooldowns[key]--;
            if (AbilityCooldowns[key] <= 0)
                AbilityCooldowns.Remove(key);
        }
    }

    public void ProcessTurnEnd()
    {
        // End of turn processing
    }

    private void ApplyStatusEffectTick(StatusEffectInstance effect)
    {
        switch (effect.EffectType)
        {
            case StatusEffectType.Bleed:
                TakeDamage(effect.Potency, DamageType.Physical);
                break;
            case StatusEffectType.Poison:
                TakeDamage(effect.Potency, DamageType.Poison);
                break;
            case StatusEffectType.Burn:
                TakeDamage(effect.Potency, DamageType.Fire);
                break;
            case StatusEffectType.Shock:
                TakeDamage(effect.Potency, DamageType.Shock);
                break;
            case StatusEffectType.Regeneration:
                Heal(effect.Potency);
                break;
        }
    }

    public bool CanUseAbility(string abilityId)
    {
        var ability = ResourceRegistry.Instance.GetAbility(abilityId);
        if (ability == null) return false;
        if (CurrentAp < ability.ApCost) return false;
        if (AbilityCooldowns.ContainsKey(abilityId)) return false;
        return true;
    }

    public void UseAbility(string abilityId)
    {
        var ability = ResourceRegistry.Instance.GetAbility(abilityId);
        if (ability == null) return;

        SpendAp(ability.ApCost);
        if (ability.CooldownTurns > 0)
            AbilityCooldowns[abilityId] = ability.CooldownTurns;

        HasActedThisTurn = true;
    }

    public void EnterOverwatch(Vector2I direction, int range)
    {
        if (CurrentAp >= 2)
        {
            SpendAp(2);
            IsInOverwatch = true;
            OverwatchDirection = direction;
            OverwatchRange = range;
            _animPlayer.Play("overwatch");
        }
    }

    public bool CheckOverwatchTrigger(UnitInstance target, Vector2I targetPos)
    {
        if (!IsInOverwatch) return false;

        var dir = targetPos - GridPosition;
        if (dir.Length() > OverwatchRange) return false;

        // Check if in overwatch cone
        float angle = Mathf.Abs(Mathf.Wrap(Mathf.Atan2(dir.Y, dir.X) - Mathf.Atan2(OverwatchDirection.Y, OverwatchDirection.X), -Mathf.Pi, Mathf.Pi));
        if (angle > Mathf.DegToRad(45)) return false;

        return true;
    }

    public void FireOverwatch(UnitInstance target)
    {
        if (!IsInOverwatch) return;

        var basicAttack = ResourceRegistry.Instance.GetAbility("basic_attack");
        if (basicAttack != null)
        {
            UseAbility("basic_attack");
            // CombatManager will handle the actual attack
        }

        IsInOverwatch = false;
        _animPlayer.Play("idle");
    }

    private void UpdateHpBar()
    {
        if (_hpBar != null)
        {
            _hpBar.MaxValue = MaxHp;
            _hpBar.Value = CurrentHp;
        }
    }

    private void UpdateApIndicator()
    {
        // Update AP pips on the unit
    }

    private void ShowFloatingText(string text, Color color)
    {
        // Spawn floating damage text
    }

    public void SetSelected(bool selected)
    {
        _selectionIndicator.Visible = selected;
    }

    public void Die()
    {
        _animPlayer.Play("death");
        SetPhysicsProcess(false);
        SetProcess(false);
    }

    public UnitData GetData()
    {
        return new UnitData
        {
            UnitId = UnitId,
            DisplayName = DisplayName,
            Type = Type,
            EnemyId = EnemyId,
            CompanionId = CompanionId,
            MaxHp = MaxHp,
            CurrentHp = CurrentHp,
            MaxAp = MaxAp,
            CurrentAp = CurrentAp,
            Armor = Armor,
            Evasion = Evasion,
            Accuracy = Accuracy,
            CritChance = CritChance,
            CritDamage = CritDamage,
            Initiative = Initiative,
            MoveRange = MoveRange,
            WeaponDamage = WeaponDamage,
            WeaponDamageType = WeaponDamageType,
            ResistPhysical = ResistPhysical,
            ResistResonance = ResistResonance,
            ResistFire = ResistFire,
            ResistPoison = ResistPoison,
            ResistShock = ResistShock,
            ResistPsychic = ResistPsychic,
            AbilityIds = new List<string>(AbilityIds),
            GridPosition = GridPosition
        };
    }

    public bool HasWeaponTags(string tag)
    {
        return false; // Stub
    }

    public void ApplyStatModifier(string stat, float flatBonus, float percentBonus, int duration)
    {
        // Stub
    }

    public void ApplyStatModifier(string stat, float flatBonus, float percentBonus, int duration, string sourceId, bool isDebuff)
    {
        ApplyStatModifier(stat, flatBonus, percentBonus, duration);
    }

    public void ApplyStatusEffect(string effectType, int duration, int stacks, int maxStacks)
    {
        ApplyStatusEffect(new StatusEffectInstance { EffectId = $"{effectType}_{UnitId}", RemainingTurns = duration, Stacks = stacks, MaxStacks = maxStacks, Potency = 1, Source = this });
    }

    public void OnTurnStart()
    {
        // Stub
    }

    public void OnTurnEnd()
    {
        // Stub
    }
}

public class UnitData
{
    public string UnitId { get; set; }
    public string DisplayName { get; set; }
    public UnitType Type { get; set; }
    public string EnemyId { get; set; }
    public string CompanionId { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxAp { get; set; }
    public int CurrentAp { get; set; }
    public int Armor { get; set; }
    public int Evasion { get; set; }
    public int Accuracy { get; set; }
    public int CritChance { get; set; }
    public int CritDamage { get; set; }
    public int Initiative { get; set; }
    public int MoveRange { get; set; }
    public int WeaponDamage { get; set; }
    public DamageType WeaponDamageType { get; set; }
    public float ResistPhysical { get; set; }
    public float ResistResonance { get; set; }
    public float ResistFire { get; set; }
    public float ResistPoison { get; set; }
    public float ResistShock { get; set; }
    public float ResistPsychic { get; set; }
    public List<string> AbilityIds { get; set; } = new();
    public Vector2I GridPosition { get; set; }
}

public class StatusEffectInstance
{
    public string EffectId { get; set; }
    public StatusEffectType EffectType { get; set; }
    public int Stacks { get; set; }
    public int MaxStacks { get; set; }
    public int RemainingTurns { get; set; }
    public int Potency { get; set; }
    public UnitInstance Source { get; set; }
}