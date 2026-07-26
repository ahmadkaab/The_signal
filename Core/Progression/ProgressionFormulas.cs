using Godot;
using System.Collections.Generic;

namespace TheSignal.Core.Progression;

[GlobalClass]
public partial class ProgressionFormulas : Resource
{
    [Export] public int XpBase { get; set; } = 100;
    [Export] public float XpCurveExponent { get; set; } = 2.0f;
    [Export] public float XpCurveLinear { get; set; } = 1.2f;
    [Export] public int SpPerLevel { get; set; } = 1;

    [Export] public int FragmentZoneCleanse { get; set; } = 3;
    [Export] public int FragmentZoneCorrupt { get; set; } = 5;
    [Export] public int FragmentBossKill { get; set; } = 10;
    [Export] public int FragmentEchoFound { get; set; } = 1;

    [Export] public Godot.Collections.Dictionary<int, int> MutationSlots { get; set; } = new()
    {
        {5, 1}, {15, 2}, {25, 3}
    };

    [Export] public Godot.Collections.Array<int> LoyaltyRanks { get; set; } = new()
    {
        100, 300, 600, 1000, 1500
    };

    [Export] public float BaseDamageMult { get; set; } = 1.0f;
    [Export] public float ArmorReductionFlat { get; set; } = 0.5f;
    [Export] public float ArmorReductionPct { get; set; } = 0.01f;
    [Export] public float CritDamageMult { get; set; } = 1.5f;
    [Export] public float CritChanceBase { get; set; } = 0.05f;

    public int XpToNextLevel(int level)
    {
        return Mathf.FloorToInt(XpBase * Mathf.Pow(level, XpCurveExponent) * XpCurveLinear);
    }

    public int TotalXpToLevel(int level)
    {
        int total = 0;
        for (int i = 1; i < level; i++)
            total += XpToNextLevel(i);
        return total;
    }

    public int GetMutationSlots(int level)
    {
        int slots = 0;
        foreach (var kvp in MutationSlots)
        {
            if (level >= kvp.Key)
                slots = kvp.Value;
        }
        return slots;
    }

    public int GetLoyaltyRank(int loyalty)
    {
        for (int i = LoyaltyRanks.Count - 1; i >= 0; i--)
        {
            if (loyalty >= (int)LoyaltyRanks[i])
                return i + 1;
        }
        return 0;
    }

    public float CalculateDamage(float baseDamage, float weaponDamage, float statScaling, int targetArmor)
    {
        float raw = BaseDamageMult * (baseDamage + weaponDamage * 0.5f + statScaling);
        float mitigation = Mathf.Min(targetArmor * ArmorReductionPct, 0.8f);
        float flatReduction = targetArmor * ArmorReductionFlat;
        return Mathf.Max(1, raw * (1 - mitigation) - flatReduction);
    }

    public float CalculateCritDamage(float damage, float critDamageBonus)
    {
        return damage * (CritDamageMult + critDamageBonus);
    }
}