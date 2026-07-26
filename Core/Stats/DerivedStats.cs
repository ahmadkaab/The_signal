using Godot;
using TheSignal.Core.Stats;

namespace TheSignal.Core.Stats;

public class DerivedStatCalculator
{
    public DerivedCombat Stats { get; private set; }
    private readonly StatBlock _base;
    private readonly float _level;

    public DerivedStatCalculator(StatBlock baseStats, float level)
    {
        _base = baseStats;
        _level = level;
        Stats = Compute();
    }

    private DerivedCombat Compute()
    {
        var stats = new DerivedCombat();

        var might = _base.GetFinal("might");
        var agility = _base.GetFinal("agility");
        var constitution = _base.GetFinal("constitution");
        var intelligence = _base.GetFinal("intelligence");
        var willpower = _base.GetFinal("willpower");
        var resonance = _base.GetFinal("resonance");

        var levelMult = 1.0f + (_level - 1) * 0.05f;

        stats.MaxHp = (20.0f + constitution * 8f) * levelMult;
        stats.CurrentHp = stats.MaxHp;
        stats.MeleeDamage = might * 2f;
        stats.RangedDamage = agility * 1.2f + intelligence * 0.5f;
        stats.TechDamage = intelligence * 1.5f;
        stats.ResonanceDamage = willpower * 0.8f + resonance * 1.5f;
        stats.Armor = Mathf.FloorToInt(constitution * 0.5f + willpower * 0.3f);
        stats.MaxAp = 6 + Mathf.FloorToInt(agility * 0.5f);
        stats.CurrentAp = stats.MaxAp;
        stats.Evasion = agility * 0.5f;
        stats.Accuracy = 80 + agility * 1.2f;
        stats.CritChance = 5 + agility * 0.3f + intelligence * 0.1f;
        stats.CritDamage = 50 + willpower * 0.5f;
        stats.Initiative = agility * 2f + willpower * 0.5f;
        stats.MoveRange = Mathf.FloorToInt(3 + agility * 0.2f);
        stats.HackRange = intelligence * 0.5f;
        stats.ScanRadius = Mathf.FloorToInt(5 + intelligence * 0.3f);
        stats.CarryWeight = 50 + might * 5f;
        stats.DiseaseResist = constitution * 1.5f;
        stats.MentalResist = willpower * 1.8f;
        stats.MutationResist = willpower * 0.5f + resonance * 0.3f;
        stats.SignalRange = resonance * 2f;
        stats.ResonancePower = willpower + resonance * 2f;

        return stats;
    }

    public void Recompute()
    {
        var hpRatio = Stats.MaxHp > 0 ? Stats.CurrentHp / Stats.MaxHp : 0f;
        var apCurrent = Stats.CurrentAp;
        var computed = Compute();

        var previous = Stats;
        Stats = computed;
        Stats.CurrentHp = computed.MaxHp * hpRatio;
        Stats.CurrentAp = Mathf.Min(apCurrent, computed.MaxAp);
    }
}

public class DerivedCombat
{
    public float MaxHp { get; set; }
    public float CurrentHp { get; set; }
    public float MeleeDamage { get; set; }
    public float RangedDamage { get; set; }
    public float TechDamage { get; set; }
    public float ResonanceDamage { get; set; }
    public int Armor { get; set; }
    public int MaxAp { get; set; }
    public int CurrentAp { get; set; }
    public float Evasion { get; set; }
    public float Accuracy { get; set; }
    public float CritChance { get; set; }
    public float CritDamage { get; set; }
    public float Initiative { get; set; }
    public int MoveRange { get; set; }
    public float HackRange { get; set; }
    public float ScanRadius { get; set; }
    public float CarryWeight { get; set; }
    public float DiseaseResist { get; set; }
    public float MentalResist { get; set; }
    public float MutationResist { get; set; }
    public float SignalRange { get; set; }
    public float ResonancePower { get; set; }
}