using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Combat;
using TheSignal.Combat.Grid;
using TheSignal.Combat.Units;
using TheSignal.Systems;

namespace TheSignal.Systems;

public partial class CombatManager : Node
{
    public static CombatManager Instance { get; private set; }

    private CombatGrid _grid;

    public CombatState State { get; private set; } = new();
    public TurnQueue TurnQueue { get; private set; } = new();

    public void NotifyActionExecuted(CombatAction action) => OnActionExecuted?.Invoke(action);
    public void NotifyUnitDied(UnitInstance unit) => OnUnitDied?.Invoke(unit);

    public event Action<CombatState> OnCombatStarted;
    public event Action<CombatState> OnCombatEnded;
    public event Action<CombatTurn> OnTurnChanged;
    public event Action<CombatAction> OnActionExecuted;
    public event Action<UnitInstance> OnUnitDied;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        _grid = GetNode<CombatGrid>("%CombatGrid");
    }

    public void StartCombat(List<UnitInstance> playerUnits, List<UnitInstance> enemyUnits, string zoneId)
    {
        State = new CombatState
        {
            PlayerUnits = playerUnits,
            EnemyUnits = enemyUnits,
            AllUnits = new List<UnitInstance>(),
            CurrentTurn = 0,
            ZoneId = zoneId,
            IsPlayerTurn = true,
            TurnNumber = 0
        };

        State.AllUnits.AddRange(playerUnits);
        State.AllUnits.AddRange(enemyUnits);

        // Position units on grid
        PositionUnitsOnGrid();

        // Initialize turn queue
        TurnQueue.Initialize(State.AllUnits);
        TurnQueue.OnTurnStarted += OnTurnStarted;
        TurnQueue.OnTurnEnded += OnTurnEnded;
        TurnQueue.OnRoundChanged += OnRoundChanged;

        // Begin first turn
        var firstTurn = TurnQueue.GetNextTurn();
        if (firstTurn != null)
        {
            OnTurnStarted(firstTurn);
        }

        GameManager.Instance.ChangeState(GameState.Combat);
        OnCombatStarted?.Invoke(State);
    }

    private void PositionUnitsOnGrid()
    {
        // Position player units on left side (columns 1-3)
        for (int i = 0; i < State.PlayerUnits.Count; i++)
        {
            var unit = State.PlayerUnits[i];
            unit.GridPosition = new Vector2I(1 + i % 3, 7 + i / 3);
            _grid.SetUnitOccupying(unit.GridPosition, unit);
        }

        // Position enemy units on right side (columns 17-19)
        for (int i = 0; i < State.EnemyUnits.Count; i++)
        {
            var unit = State.EnemyUnits[i];
            unit.GridPosition = new Vector2I(17 + i % 3, 7 + i / 3);
            _grid.SetUnitOccupying(unit.GridPosition, unit);
        }
    }

    private void OnTurnStarted(TurnEntry entry)
    {
        State.IsPlayerTurn = entry.Unit.Type == UnitType.Player || entry.Unit.Type == UnitType.Companion;
        entry.Unit.CurrentAp = entry.Unit.MaxAp;
        entry.Unit.OnTurnStart();

        OnTurnChanged?.Invoke(new CombatTurn
        {
            Unit = entry.Unit,
            TurnNumber = State.TurnNumber,
            IsPlayerTurn = State.IsPlayerTurn
        });

        if (!State.IsPlayerTurn)
        {
            ExecuteAiTurn(entry.Unit);
        }
    }

    private void OnTurnEnded(TurnEntry entry)
    {
        entry.Unit.OnTurnEnd();
        State.CurrentTurn++;
    }

    private void OnRoundChanged(int roundNumber)
    {
        State.TurnNumber = roundNumber;
    }

    public void EndTurn()
    {
        TurnQueue.EndCurrentTurn();
    }

    public void ExecuteAction(UnitInstance actor, AbilityResource ability, Vector2I targetTile, UnitInstance targetUnit)
    {
        if (actor.CurrentAp < ability.ApCost) return;
        if (ability.CooldownTurns > 0 && actor.AbilityCooldowns.TryGetValue(ability.AbilityId, out int cd) && cd > 0) return;

        actor.CurrentAp -= ability.ApCost;
        if (ability.CooldownTurns > 0)
            actor.AbilityCooldowns[ability.AbilityId] = ability.CooldownTurns;

        var action = new CombatAction
        {
            Actor = actor,
            Ability = ability,
            TargetTile = targetTile,
            TargetUnit = targetUnit,
            Timestamp = (long)Time.GetTicksMsec()
        };

        ResolveAction(action);
        OnActionExecuted?.Invoke(action);

        // Reduce cooldowns
        var keys = new List<string>(actor.AbilityCooldowns.Keys);
        foreach (var key in keys)
        {
            actor.AbilityCooldowns[key]--;
            if (actor.AbilityCooldowns[key] <= 0)
                actor.AbilityCooldowns.Remove(key);
        }
    }

    private void ResolveAction(CombatAction action)
    {
        var ability = action.Ability;
        var actor = action.Actor;

        switch (ability.TargetType)
        {
            case AbilityTargetType.SingleEnemy:
            case AbilityTargetType.SingleAlly:
            case AbilityTargetType.SingleAny:
                if (action.TargetUnit != null)
                    ApplyEffect(action, action.TargetUnit);
                break;
            case AbilityTargetType.AreaCircle:
                var unitsInRadius = _grid.GetUnitsInCircle(action.TargetTile, ability.Radius);
                foreach (var u in unitsInRadius)
                    ApplyEffect(action, u);
                break;
            case AbilityTargetType.AreaCone:
                var unitsInCone = _grid.GetUnitsInCone(actor.GridPosition, action.TargetTile, ability.Range, 90);
                foreach (var u in unitsInCone)
                    ApplyEffect(action, u);
                break;
            case AbilityTargetType.Self:
                ApplyEffect(action, actor);
                break;
            case AbilityTargetType.Global:
                foreach (var u in State.AllUnits)
                    ApplyEffect(action, u);
                break;
        }
    }

    private void ApplyEffect(CombatAction action, UnitInstance target)
    {
        var ability = action.Ability;
        var actor = action.Actor;

        // Calculate damage
        if (ability.BaseDamage > 0)
        {
            float statValue = ability.ScalingStat switch
            {
                ScalingStat.Might => actor.BaseStats.GetValueOrDefault("might", 0),
                ScalingStat.Agility => actor.BaseStats.GetValueOrDefault("agility", 0),
                ScalingStat.Constitution => actor.BaseStats.GetValueOrDefault("constitution", 0),
                ScalingStat.Intelligence => actor.BaseStats.GetValueOrDefault("intelligence", 0),
                ScalingStat.Willpower => actor.BaseStats.GetValueOrDefault("willpower", 0),
                ScalingStat.Resonance => actor.BaseStats.GetValueOrDefault("resonance", 0),
                ScalingStat.WeaponDamage => actor.WeaponDamage,
                _ => 0
            };

            float damage = GameManager.Instance.ProgressionFormulas.CalculateDamage(
                ability.BaseDamage,
                actor.WeaponDamage,
                statValue * ability.StatScaling,
                target.Stats.Armor
            );

            // Crit check
            bool isCrit = GD.Randf() < (actor.Stats.CritChance / 100f);
            if (isCrit)
            {
                damage = GameManager.Instance.ProgressionFormulas.CalculateCritDamage(damage, actor.Stats.CritDamage / 100f);
                action.IsCritical = true;
            }

            // Apply resistances
            damage = ApplyResistances(damage, ability.DamageType, target);

            int damageDealt = Mathf.Max(1, Mathf.RoundToInt(damage));
            target.CurrentHp -= damageDealt;

            action.DamageDealt[target.UnitId] = damageDealt;

            if (target.CurrentHp <= 0)
            {
                OnUnitDied?.Invoke(target);
            }

            // Apply on-hit status effects
            foreach (var se in ability.StatusEffects)
            {
                if (se.OnHit && GD.Randf() < se.Chance)
                {
                    target.ApplyStatusEffect(se.EffectType.ToString(), se.Duration, se.Stacks, se.MaxStacks);
                    action.StatusEffectsApplied[target.UnitId].Add(se.EffectType);
                }
            }
        }

        // Apply stat modifiers
        foreach (var sm in ability.StatModifiers)
        {
            target.ApplyStatModifier(sm.Stat.ToString(), sm.FlatBonus, sm.PercentBonus, sm.Duration, sm.SourceId, sm.IsDebuff);
        }

        // Apply position effects
        foreach (var pe in ability.PositionEffects)
        {
            ApplyPositionEffect(target, pe);
        }

        // Apply resource effects
        foreach (var re in ability.ResourceEffects)
        {
            ApplyResourceEffect(actor, re);
        }

        // Summons
        foreach (var se in ability.SummonEffects)
        {
            SummonUnit(actor, se);
        }

        // Fields
        foreach (var fe in ability.FieldEffects)
        {
            CreateField(actor, action.TargetTile, fe);
        }
    }

    private float ApplyResistances(float damage, DamageType type, UnitInstance target)
    {
        float resist = type switch
        {
            DamageType.Physical => target.ResistPhysical,
            DamageType.Resonance => target.ResistResonance,
            DamageType.Fire => target.ResistFire,
            DamageType.Poison => target.ResistPoison,
            DamageType.Shock => target.ResistShock,
            DamageType.Psychic => target.ResistPsychic,
            _ => 0
        };
        return damage * (1f - Mathf.Clamp(resist, 0f, 0.9f));
    }

    private void ApplyPositionEffect(UnitInstance target, PositionEffect effect)
    {
        Vector2I newPos = target.GridPosition;

        switch (effect.Type)
        {
            case PositionEffectType.Teleport:
                newPos = effect.TargetTile;
                break;
            case PositionEffectType.Push:
                newPos = target.GridPosition + ((Vector2)(target.GridPosition - FindCasterGridPosition(effect))).Normalized() * effect.Distance;
                break;
            case PositionEffectType.Pull:
                newPos = target.GridPosition + ((Vector2)(FindCasterGridPosition(effect) - target.GridPosition)).Normalized() * effect.Distance;
                break;
            case PositionEffectType.Swap:
                var casterPos = FindCasterGridPosition(effect);
                // Find the caster unit instance
                var casterUnit = FindCasterUnit(effect);
                if (casterUnit != null)
                {
                    casterUnit.GridPosition = target.GridPosition;
                }
                newPos = casterPos;
                break;
            case PositionEffectType.Knockback:
                newPos = target.GridPosition + ((Vector2)(target.GridPosition - FindCasterGridPosition(effect))).Normalized() * effect.Distance;
                break;
        }

        if (effect.IgnoreOccupied || !_grid.IsCellOccupied(newPos))
        {
            _grid.SetUnitOccupying(target.GridPosition, null);
            target.GridPosition = newPos;
            _grid.SetUnitOccupying(newPos, target);
        }
    }

    private void ApplyResourceEffect(UnitInstance actor, ResourceEffect effect)
    {
        switch (effect.Resource)
        {
            case ResourceType.ActionPoints:
                if (effect.IsCost) actor.CurrentAp = Mathf.Max(0, actor.CurrentAp - effect.Amount);
                else if (effect.IsRefund) actor.CurrentAp = Mathf.Min(actor.MaxAp, actor.CurrentAp + effect.Amount);
                else actor.CurrentAp = Mathf.Min(actor.MaxAp, actor.CurrentAp + effect.Amount);
                break;
            case ResourceType.Health:
                if (effect.IsCost) actor.CurrentHp = Mathf.Max(0, actor.CurrentHp - effect.Amount);
                else actor.CurrentHp = Mathf.Min(actor.MaxHp, actor.CurrentHp + effect.Amount);
                break;
            case ResourceType.Shield:
                // Add shield
                break;
        }
    }

    private void SummonUnit(UnitInstance caster, SummonEffect effect)
    {
        var enemyDef = ResourceRegistry.Instance.GetEnemy(effect.UnitId);
        if (enemyDef == null) return;

        var summoned = SpawnUnit(enemyDef, caster.GridPosition);
        summoned.Type = UnitType.Deployable;
        summoned.Duration = effect.Duration;
        summoned.MaxSummons = effect.MaxSummons;
        summoned.InheritStats = effect.InheritStats;
        summoned.StatMultiplier = effect.StatMultiplier;
    }

    private void CreateField(UnitInstance caster, Vector2I center, FieldEffect effect)
    {
        var field = new FieldInstance
        {
            FieldEffectId = effect.EffectId ?? effect.Type.ToString(),
            Position = center,
            RemainingTurns = effect.Duration,
            OwnerId = caster.UnitId,
            IsHostile = effect.IsHostile
        };

        State.ActiveFields[center] = field;

        // Visual
        _grid.HighlightRange(_grid.GetCellsInCircle(center, effect.Radius), GetFieldColor(effect.Type));
    }

    private Color GetFieldColor(FieldType type)
    {
        return type switch
        {
            FieldType.Smoke => new Color(0.3f, 0.3f, 0.3f, 0.5f),
            FieldType.Fire => new Color(1f, 0.3f, 0f, 0.5f),
            FieldType.Poison => new Color(0.3f, 1f, 0.3f, 0.5f),
            FieldType.Electric => new Color(0.3f, 0.3f, 1f, 0.5f),
            FieldType.Resonance => new Color(0.5f, 0f, 1f, 0.5f),
            FieldType.NullField => new Color(0.1f, 0.1f, 0.1f, 0.5f),
            FieldType.Healing => new Color(0f, 1f, 0.5f, 0.5f),
            FieldType.Stasis => new Color(0.5f, 0.5f, 1f, 0.5f),
            _ => new Color(1f, 1f, 1f, 0.3f)
        };
    }

    private void ExecuteAiTurn(UnitInstance unit)
    {
        // Basic AI: move toward nearest enemy, use best available ability
        var enemies = unit.Type == UnitType.Enemy ? State.PlayerUnits : State.EnemyUnits;
        var target = FindNearestEnemy(unit, enemies);

        if (target != null)
        {
            var abilities = GetAvailableAbilities(unit);
            if (abilities.Count > 0)
            {
                var ability = abilities[0];
                ExecuteAction(unit, ability, target.GridPosition, target);
            }
            else
            {
                var basicAttack = ResourceRegistry.Instance.GetAbility("basic_attack");
                if (basicAttack != null)
                    ExecuteAction(unit, basicAttack, target.GridPosition, target);
            }
        }

        EndTurn();
    }

    private UnitInstance FindNearestEnemy(UnitInstance unit, List<UnitInstance> enemies)
    {
        UnitInstance nearest = null;
        float minDist = float.MaxValue;

        foreach (var e in enemies)
        {
            if (e.CurrentHp <= 0) continue;
            float dist = unit.GridPosition.DistanceTo(e.GridPosition);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = e;
            }
        }
        return nearest;
    }

    private List<AbilityResource> GetAvailableAbilities(UnitInstance unit)
    {
        var result = new List<AbilityResource>();
        foreach (var abilityId in unit.AbilityIds)
        {
            var ability = ResourceRegistry.Instance.GetAbility(abilityId);
            if (ability != null && unit.CurrentAp >= ability.ApCost)
            {
                if (!unit.AbilityCooldowns.ContainsKey(abilityId) || unit.AbilityCooldowns[abilityId] <= 0)
                    result.Add(ability);
            }
        }
        return result;
    }

    private bool IsCombatOver()
    {
        bool playerAlive = State.PlayerUnits.Exists(u => u.CurrentHp > 0);
        bool enemyAlive = State.EnemyUnits.Exists(u => u.CurrentHp > 0);
        return !playerAlive || !enemyAlive;
    }

    private void EndCombat()
    {
        bool playerWon = State.EnemyUnits.TrueForAll(u => u.CurrentHp <= 0);
        OnCombatEnded?.Invoke(State);

        if (playerWon)
        {
            GrantRewards();
        }

        GameManager.Instance.ChangeState(GameState.Exploration);
    }

    private void GrantRewards()
    {
        int totalXp = 0;
        int totalScrap = 0;
        int totalFragments = 0;

        foreach (var enemy in State.EnemyUnits)
        {
            var def = ResourceRegistry.Instance.GetEnemy(enemy.EnemyId);
            if (def != null)
            {
                totalXp += def.XpReward;
                totalScrap += def.ScrapReward;
                totalFragments += def.ResonanceFragmentReward;
            }
        }

        GameManager.Instance.Player.GainXp(totalXp);
        // Add scrap and fragments

        GD.Print($"Combat Victory! XP: {totalXp}, Scrap: {totalScrap}, Fragments: {totalFragments}");
    }
}

public class CombatState
{
    public List<UnitInstance> PlayerUnits { get; set; } = new();
    public List<UnitInstance> EnemyUnits { get; set; } = new();
    public List<UnitInstance> AllUnits { get; set; } = new();
    public int CurrentTurn { get; set; }
    public int TurnNumber { get; set; }
    public string ZoneId { get; set; }
    public bool IsPlayerTurn { get; set; }
    public Dictionary<Vector2I, FieldInstance> ActiveFields { get; set; } = new();
    
    public void SpawnUnit(TheSignal.Data.UnitData data, Vector2I position)
    {
        var unit = new UnitInstance();
        unit.Initialize(_grid, data);
        unit.GridPosition = position;
        AddChild(unit);
        AllUnits.Add(unit);
        if (data is EnemyUnitData || data.Type == UnitType.Enemy)
            EnemyUnits.Add(unit);
    }
    
    public void EnterOverwatch(UnitInstance unit)
    {
        // Stub
    }
    
    public void SelectAbility(AbilityResource ability)
    {
        State.SelectedAbility = ability;
    }
}

public class CombatTurn
{
    public UnitInstance Unit { get; set; }
    public int TurnNumber { get; set; }
    public bool IsPlayerTurn { get; set; }
}

public class CombatAction
{
    public UnitInstance Actor { get; set; }
    public AbilityResource Ability { get; set; }
    public Vector2I TargetTile { get; set; }
    public UnitInstance TargetUnit { get; set; }
    public long Timestamp { get; set; }
    public bool IsCritical { get; set; }
    public Dictionary<string, int> DamageDealt { get; set; } = new();
    public Dictionary<string, List<StatusEffectType>> StatusEffectsApplied { get; set; } = new();
}