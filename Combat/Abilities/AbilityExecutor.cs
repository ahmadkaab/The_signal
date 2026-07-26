using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Combat.Units;
using TheSignal.Combat.Grid;
using TheSignal.Systems;

namespace TheSignal.Combat;

public partial class AbilityExecutor : Node
{
    private CombatManager _combatManager;
    private CombatGrid _grid;

    public override void _Ready()
    {
        _combatManager = CombatManager.Instance;
        _grid = GetNode<CombatGrid>("%CombatGrid");
    }

    public void ExecuteAbility(UnitInstance caster, AbilityResource ability, Vector2I targetTile, UnitInstance targetUnit)
    {
        if (!CanExecute(caster, ability))
        {
            GD.Print($"Cannot execute {ability.DisplayName}: requirements not met");
            return;
        }

        // Pay costs
        caster.CurrentAp -= ability.ApCost;
        if (ability.ResonanceCost > 0)
        {
            // Deduct from resonance pool
        }

        // Apply cooldown
        if (ability.CooldownTurns > 0)
        {
            caster.AbilityCooldowns[ability.AbilityId] = ability.CooldownTurns;
        }

        // Determine targets based on ability type
        var targets = GetTargets(caster, ability, targetTile, targetUnit);

        // Create action record
        var action = new CombatAction
        {
            Actor = caster,
            Ability = ability,
            TargetTile = targetTile,
            TargetUnit = targetUnit,
            // AllTargets removed - not on Systems.CombatAction
            Timestamp = (long)Time.GetTicksMsec()
        };

        // Execute effects
        foreach (var target in targets)
        {
            ApplyAbilityEffects(action, target);
        }

        // Handle area effects
        if (ability.TargetType == AbilityTargetType.AreaCircle || ability.TargetType == AbilityTargetType.AreaCone)
        {
            CreateFieldEffect(caster, ability, targetTile);
        }

        // Handle summons
        foreach (var summon in ability.SummonEffects)
        {
            SummonUnit(caster, summon, targetTile);
        }

        // Handle position changes (apply to primary target)
        var primaryTarget = targetUnit ?? (targets.Count > 0 ? targets[0] : null);
        foreach (var posEffect in ability.PositionEffects)
        {
            if (primaryTarget != null)
                ApplyPositionEffect(caster, primaryTarget, posEffect);
        }

        // Handle resource effects
        foreach (var resEffect in ability.ResourceEffects)
        {
            if (primaryTarget != null)
                ApplyResourceEffect(caster, primaryTarget, resEffect);
        }

        // Trigger on-action events
        _combatManager.NotifyActionExecuted(action);

        // Check for combo triggers
        CheckComboTriggers(caster, ability, targets);
    }

    private bool CanExecute(UnitInstance caster, AbilityResource ability)
    {
        if (caster.CurrentAp < ability.ApCost) return false;
        if (caster.AbilityCooldowns.TryGetValue(ability.AbilityId, out int cd) && cd > 0) return false;

        // Check requirements
        if (ability.MinLevel > 0 && caster.Level < ability.MinLevel) return false;
        if (ability.RequiredWeaponTags != WeaponTags.None)
        {
            if (!caster.HasWeaponTags(ability.RequiredWeaponTags.ToString())) return false;
        }
        if (ability.RequiredSignalNodes.Count > 0)
        {
            foreach (var node in ability.RequiredSignalNodes)
            {
                if (!GameManager.Instance.Player.UnlockedSignalNodes.Contains(node))
                    return false;
            }
        }
        if (ability.RequiredMutations.Count > 0)
        {
            foreach (var mut in ability.RequiredMutations)
            {
                if (!GameManager.Instance.Player.EquippedMutations.Contains(mut))
                    return false;
            }
        }

        return true;
    }

    private List<UnitInstance> GetTargets(UnitInstance caster, AbilityResource ability, Vector2I targetTile, UnitInstance targetUnit)
    {
        var targets = new List<UnitInstance>();

        switch (ability.TargetType)
        {
            case AbilityTargetType.Self:
                targets.Add(caster);
                break;

            case AbilityTargetType.SingleEnemy:
            case AbilityTargetType.SingleAlly:
            case AbilityTargetType.SingleAny:
                if (targetUnit != null && IsValidTarget(caster, ability, targetUnit))
                    targets.Add(targetUnit);
                break;

            case AbilityTargetType.AreaCircle:
                var inRadius = _grid.GetCellsInCircle(targetTile, ability.Radius);
                foreach (var coord in inRadius)
                {
                    var unit = _grid.GetUnitAt(coord);
                    if (unit != null && IsValidTarget(caster, ability, unit))
                        targets.Add(unit);
                }
                break;

            case AbilityTargetType.AreaCone:
                var inCone = _grid.GetCellsInCone(caster.GridPosition, targetTile, ability.Range, 90);
                foreach (var coord in inCone)
                {
                    var unit = _grid.GetUnitAt(coord);
                    if (unit != null && IsValidTarget(caster, ability, unit))
                        targets.Add(unit);
                }
                break;

            case AbilityTargetType.Line:
                var inLine = _grid.GetCellsInLine(caster.GridPosition, targetTile);
                foreach (var coord in inLine)
                {
                    var unit = _grid.GetUnitAt(coord);
                    if (unit != null && IsValidTarget(caster, ability, unit))
                        targets.Add(unit);
                }
                break;

            case AbilityTargetType.Global:
                foreach (var unit in _combatManager.State.AllUnits)
                {
                    if (IsValidTarget(caster, ability, unit))
                        targets.Add(unit);
                }
                break;
        }

        return targets;
    }

    private bool IsValidTarget(UnitInstance caster, AbilityResource ability, UnitInstance target)
    {
        if (target.CurrentHp <= 0) return false;

        bool isEnemy = target.Type == UnitType.Enemy || (target.Type == UnitType.Neutral && caster.Type != UnitType.Enemy);
        bool isAlly = target.Type == UnitType.Player || target.Type == UnitType.Companion || (target.Type == UnitType.Neutral && caster.Type != UnitType.Enemy);

        return ability.TargetType switch
        {
            AbilityTargetType.SingleEnemy => isEnemy,
            AbilityTargetType.SingleAlly => isAlly,
            AbilityTargetType.SingleAny => true,
            AbilityTargetType.AreaCircle => ability.CanTargetEnemies && isEnemy || ability.CanTargetAllies && isAlly,
            AbilityTargetType.AreaCone => ability.CanTargetEnemies && isEnemy || ability.CanTargetAllies && isAlly,
            AbilityTargetType.Line => ability.CanTargetEnemies && isEnemy || ability.CanTargetAllies && isAlly,
            AbilityTargetType.Global => ability.CanTargetEnemies && isEnemy || ability.CanTargetAllies && isAlly,
            _ => false
        };
    }

    private void ApplyAbilityEffects(CombatAction action, UnitInstance target)
    {
        var ability = action.Ability;

        // Damage/healing
        if (ability.BaseDamage != 0)
        {
            float statValue = GetStatValue(action.Actor, ability.ScalingStat);
            float rawDamage = ability.BaseDamage + statValue * ability.StatScaling;

            if (action.Actor.Stats.CritChance / 100f > GD.Randf())
            {
                rawDamage *= 1f + action.Actor.Stats.CritDamage / 100f;
                action.IsCritical = true;
            }

            // Apply resistances
            float resistance = GetResistance(target, ability.DamageType);
            float finalDamage = rawDamage * (1f - Mathf.Clamp(resistance, 0f, 0.9f));

            // Apply armor reduction
            finalDamage -= target.Stats.Armor * GameManager.Instance.ProgressionFormulas.ArmorReductionFlat;

            int damageDealt = Mathf.Max(1, Mathf.RoundToInt(finalDamage));
            target.CurrentHp -= damageDealt;

            action.DamageDealt[target.UnitId] = damageDealt;

            if (target.CurrentHp <= 0)
            {
                _combatManager.NotifyUnitDied(target);
            }
        }

        // Status effects
        foreach (var se in ability.StatusEffects)
        {
            if (se.OnHit && GD.Randf() < se.Chance)
            {
                target.ApplyStatusEffect(se.EffectType.ToString(), se.Duration, se.Stacks, se.MaxStacks);
                action.StatusEffectsApplied[target.UnitId].Add(se.EffectType);
            }
        }

        // Stat modifiers
        foreach (var sm in ability.StatModifiers)
        {
            target.ApplyStatModifier(sm.Stat.ToString(), sm.FlatBonus, sm.PercentBonus, sm.Duration, sm.SourceId, sm.IsDebuff);
        }
    }

    private void CreateFieldEffect(UnitInstance caster, AbilityResource ability, Vector2I center)
    {
        foreach (var field in ability.FieldEffects)
        {
            var fieldInstance = new FieldInstance
            {
                FieldEffectId = field.Type.ToString(),
                Position = center,
                RemainingTurns = field.Duration,
                OwnerId = caster.UnitId,
                IsHostile = false
            };

            _combatManager.State.ActiveFields[center] = fieldInstance;

            // Visual
            _grid.HighlightRange(_grid.GetCellsInCircle(center, field.Radius), GetFieldColor(field.Type));
        }
    }

    private void SummonUnit(UnitInstance caster, SummonEffect summon, Vector2I targetTile)
    {
        var enemyDef = ResourceRegistry.Instance.GetEnemy(summon.UnitId);
        if (enemyDef == null) return;

        var summoned = _combatManager.SpawnUnit(null, targetTile);
        summoned.Type = UnitType.Deployable;

        if (summon.InheritStats)
        {
            // stub
        }
        {
            // Apply stat inheritance
        }
    }

    private void ApplyPositionEffect(UnitInstance caster, UnitInstance target, PositionEffect effect)
    {
        Vector2I newPos = target.GridPosition;

        switch (effect.Type)
        {
            case PositionEffectType.Teleport:
                newPos = effect.TargetTile;
                break;
            case PositionEffectType.Push:
                newPos = (Vector2I)((Vector2)target.GridPosition + ((Vector2)(target.GridPosition - caster.GridPosition)).Normalized() * effect.Distance);
                break;
            case PositionEffectType.Pull:
                newPos = (Vector2I)((Vector2)caster.GridPosition + ((Vector2)(caster.GridPosition - target.GridPosition)).Normalized() * effect.Distance);
                break;
            case PositionEffectType.Swap:
                var casterPos = caster.GridPosition;
                caster.GridPosition = target.GridPosition;
                target.GridPosition = casterPos;
                break;
            case PositionEffectType.Knockback:
                newPos = (Vector2I)((Vector2)target.GridPosition + ((Vector2)(target.GridPosition - caster.GridPosition)).Normalized() * effect.Distance);
                break;
            case PositionEffectType.Slide:
                // Slide along a direction
                break;
        }

        if (effect.IgnoreOccupied || !_grid.IsCellOccupied(newPos))
        {
            _grid.SetUnitOccupying(target.GridPosition, null);
            target.GridPosition = newPos;
            _grid.SetUnitOccupying(newPos, target);
        }
    }

    private void ApplyResourceEffect(UnitInstance caster, UnitInstance target, ResourceEffect effect)
    {
        switch (effect.Resource)
        {
            case ResourceType.ActionPoints:
                if (effect.IsCost) target.CurrentAp = Mathf.Max(0, target.CurrentAp - effect.Amount);
                else if (effect.IsRefund) target.CurrentAp = Mathf.Min(target.MaxAp, target.CurrentAp + effect.Amount);
                else target.CurrentAp = Mathf.Min(target.MaxAp, target.CurrentAp + effect.Amount);
                break;
            case ResourceType.ResonanceFragments:
                // Handle resonance fragments
                break;
            case ResourceType.Health:
                if (effect.IsCost) target.CurrentHp = Mathf.Max(0, target.CurrentHp - effect.Amount);
                else target.CurrentHp = Mathf.Min(target.MaxHp, target.CurrentHp + effect.Amount);
                break;
            case ResourceType.Shield:
                // Add shield
                break;
        }
    }

    private float GetStatValue(UnitInstance unit, ScalingStat stat)
    {
        return stat switch
        {
            ScalingStat.Might => unit.BaseStats.GetValueOrDefault("might", 0),
            ScalingStat.Agility => unit.BaseStats.GetValueOrDefault("agility", 0),
            ScalingStat.Constitution => unit.BaseStats.GetValueOrDefault("constitution", 0),
            ScalingStat.Intelligence => unit.BaseStats.GetValueOrDefault("intelligence", 0),
            ScalingStat.Willpower => unit.BaseStats.GetValueOrDefault("willpower", 0),
            ScalingStat.Resonance => unit.BaseStats.GetValueOrDefault("resonance", 0),
            ScalingStat.WeaponDamage => unit.WeaponDamage,
            _ => 0
        };
    }

    private float GetResistance(UnitInstance unit, DamageType type)
    {
        return type switch
        {
            DamageType.Physical => unit.ResistPhysical,
            DamageType.Resonance => unit.ResistResonance,
            DamageType.Fire => unit.ResistFire,
            DamageType.Poison => unit.ResistPoison,
            DamageType.Shock => unit.ResistShock,
            DamageType.Psychic => unit.ResistPsychic,
            _ => 0
        };
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

    private void CheckComboTriggers(UnitInstance caster, AbilityResource ability, List<UnitInstance> targets)
    {
        // Check for companion combo abilities
        foreach (var target in targets)
        {
            if (target.Type == UnitType.Companion)
            {
                var companion = target as CompanionInstance;
                if (companion != null && companion.HasComboWith(caster, ability))
                {
                    // Trigger combo ability
                    var comboAbility = companion.GetComboAbility(caster, ability);
                    if (comboAbility != null)
                    {
                        ExecuteAbility(target, comboAbility, target.GridPosition, caster);
                    }
                }
            }
        }
    }
}

