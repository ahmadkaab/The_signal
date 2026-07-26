using Godot;
using System.Collections.Generic;

namespace TheSignal.Core.Stats;

[GlobalClass]
public partial class StatDefinition : Resource
{
    [Export] public string StatId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public bool IsCoreStat { get; set; } = false;
    [Export] public float BaseValue { get; set; } = 0f;
    [Export] public float MinValue { get; set; } = float.MinValue;
    [Export] public float MaxValue { get; set; } = float.MaxValue;
    [Export] public bool ShowInUI { get; set; } = true;
}

[GlobalClass]
public partial class CoreStatDefinitions : Resource
{
    private static readonly Dictionary<string, StatDefinition> _definitions = new();

    public static readonly StatDefinition Might = new()
    {
        StatId = "might",
        DisplayName = "Might",
        Description = "Physical strength. Increases melee damage, carry weight, and break-object checks.",
        IsCoreStat = true,
        BaseValue = 10f,
        MinValue = 1f,
        MaxValue = 100f
    };

    public static readonly StatDefinition Agility = new()
    {
        StatId = "agility",
        DisplayName = "Agility",
        Description = "Speed and reflexes. Increases Action Points, evasion, initiative, and ranged accuracy.",
        IsCoreStat = true,
        BaseValue = 10f,
        MinValue = 1f,
        MaxValue = 100f
    };

    public static readonly StatDefinition Constitution = new()
    {
        StatId = "constitution",
        DisplayName = "Constitution",
        Description = "Hardiness and biological stability. Increases HP, poison/disease resistance, and mutation slots.",
        IsCoreStat = true,
        BaseValue = 10f,
        MinValue = 1f,
        MaxValue = 100f
    };

    public static readonly StatDefinition Intelligence = new()
    {
        StatId = "intelligence",
        DisplayName = "Intelligence",
        Description = "Mental acuity and technical aptitude. Increases tech damage, hack range, scan radius, and chip slots.",
        IsCoreStat = true,
        BaseValue = 10f,
        MinValue = 1f,
        MaxValue = 100f
    };

    public static readonly StatDefinition Willpower = new()
    {
        StatId = "willpower",
        DisplayName = "Willpower",
        Description = "Mental fortitude and presence. Increases mental resistance, resonance power, and companion bond rate.",
        IsCoreStat = true,
        BaseValue = 10f,
        MinValue = 1f,
        MaxValue = 100f
    };

    public static readonly StatDefinition Resonance = new()
    {
        StatId = "resonance",
        DisplayName = "Resonance",
        Description = "Connection to The Signal. Increases Signal range, corruption control, and Oracle access.",
        IsCoreStat = true,
        BaseValue = 10f,
        MinValue = 1f,
        MaxValue = 100f
    };

    static CoreStatDefinitions()
    {
        Register(Might);
        Register(Agility);
        Register(Constitution);
        Register(Intelligence);
        Register(Willpower);
        Register(Resonance);
    }

    private static void Register(StatDefinition def)
    {
        _definitions[def.StatId] = def;
    }

    public static StatDefinition Get(string statId) => _definitions.TryGetValue(statId, out var def) ? def : null;
    public static IReadOnlyList<StatDefinition> AllCoreStats => new[]
    {
        Might, Agility, Constitution, Intelligence, Willpower, Resonance
    };
}

public class StatModifier
{
    public string SourceId { get; }
    public float FlatBonus { get; }
    public float PercentBonus { get; }
    public int Priority { get; }

    public StatModifier(string sourceId, float flatBonus = 0f, float percentBonus = 0f, int priority = 0)
    {
        SourceId = sourceId;
        FlatBonus = flatBonus;
        PercentBonus = percentBonus;
        Priority = priority;
    }
}

public class StatBlock
{
    private readonly Dictionary<string, float> _baseValues = new();
    private readonly Dictionary<string, List<StatModifier>> _modifiers = new();

    public StatBlock()
    {
        foreach (var def in CoreStatDefinitions.AllCoreStats)
        {
            _baseValues[def.StatId] = def.BaseValue;
            _modifiers[def.StatId] = new List<StatModifier>();
        }
    }

    public float GetBase(string statId) => _baseValues.TryGetValue(statId, out var v) ? v : 0f;

    public void SetBase(string statId, float value)
    {
        var def = CoreStatDefinitions.Get(statId);
        if (def != null)
            value = Mathf.Clamp(value, def.MinValue, def.MaxValue);
        _baseValues[statId] = value;
    }

    public void AddModifier(string statId, StatModifier mod)
    {
        if (!_modifiers.ContainsKey(statId))
            _modifiers[statId] = new List<StatModifier>();
        _modifiers[statId].Add(mod);
        _modifiers[statId].Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public void RemoveModifier(string statId, string sourceId)
    {
        if (_modifiers.TryGetValue(statId, out var list))
            list.RemoveAll(m => m.SourceId == sourceId);
    }

    public void ClearModifiers(string statId) => _modifiers[statId]?.Clear();

    public float GetFinal(string statId)
    {
        var baseVal = GetBase(statId);
        var flat = 0f;
        var percent = 1f;

        if (_modifiers.TryGetValue(statId, out var mods))
        {
            foreach (var m in mods)
            {
                flat += m.FlatBonus;
                percent += m.PercentBonus;
            }
        }

        var result = (baseVal + flat) * percent;

        var def = CoreStatDefinitions.Get(statId);
        if (def != null)
            result = Mathf.Clamp(result, def.MinValue, def.MaxValue);

        return result;
    }

    public Dictionary<string, float> GetAllFinal()
    {
        var result = new Dictionary<string, float>();
        foreach (var kvp in _baseValues)
            result[kvp.Key] = GetFinal(kvp.Key);
        return result;
    }
}