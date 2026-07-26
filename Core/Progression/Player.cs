using Godot;
using System.Collections.Generic;
using TheSignal.Core.Save;
using TheSignal.Core.Stats;
using TheSignal.Systems;

namespace TheSignal.Core.Progression;

public class Player
{
    public string Name { get; set; } = "Signal Walker";
    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; } = 0;
    public int SignalPoints { get; set; } = 0;
    public int ResonanceFragments { get; set; } = 0;
    public int CurrentHp { get; private set; }
    public int MaxHp { get; private set; }
    public StatBlock BaseStats { get; } = new();
    public List<string> UnlockedSignalNodes { get; } = new();
    public List<string> EquippedMutations { get; } = new();
    public Vector2 Position { get; set; }
    public string CurrentZoneId { get; set; }

    public void Initialize()
    {
        foreach (var def in CoreStatDefinitions.AllCoreStats)
        {
            BaseStats.SetBase(def.StatId, def.BaseValue);
        }
        RecalculateDerived();
        CurrentHp = MaxHp;
    }

    public void GainXp(int amount, ProgressionFormulas formulas)
    {
        CurrentXp += amount;
        int xpToNext = formulas.XpToNextLevel(Level);
        while (CurrentXp >= xpToNext)
        {
            CurrentXp -= xpToNext;
            LevelUp(formulas);
            xpToNext = formulas.XpToNextLevel(Level);
        }
    }

    public void GainXp(int amount)
    {
        var formulas = GameManager.Instance?.ProgressionFormulas ?? new ProgressionFormulas();
        GainXp(amount, formulas);
    }

    private void LevelUp(ProgressionFormulas formulas)
    {
        Level++;
        SignalPoints += formulas.SpPerLevel;
        GD.Print($"Level Up! Now Level {Level}. Signal Points: {SignalPoints}");

        if (formulas.MutationSlots.TryGetValue(Level, out int slots))
        {
            GD.Print($"Mutation Slot Unlocked! Total slots: {slots}");
        }

        RecalculateDerived();
        CurrentHp = MaxHp;
    }

    public void RecalculateDerived()
    {
        int vit = (int)BaseStats.GetBase("constitution");
        int str = (int)BaseStats.GetBase("might");
        int agi = (int)BaseStats.GetBase("agility");
        int ins = (int)BaseStats.GetBase("intelligence");
        int wil = (int)BaseStats.GetBase("willpower");
        int res = (int)BaseStats.GetBase("resonance");

        MaxHp = 20 + vit * 8 + Level * 5;
        // Derived stats available via getters
    }

    public int GetActionPoints() => 6 + (int)BaseStats.GetBase("agility") / 4;
    public int GetInitiative() => 10 + (int)BaseStats.GetBase("agility") + (int)BaseStats.GetBase("resonance") / 2;
    public int GetResonancePower() => 5 + (int)BaseStats.GetBase("resonance") * 2;

    public bool UnlockSignalNode(string nodeId)
    {
        if (UnlockedSignalNodes.Contains(nodeId)) return false;
        UnlockedSignalNodes.Add(nodeId);
        return true;
    }

    public bool EquipMutation(string mutationId)
    {
        if (EquippedMutations.Contains(mutationId)) return false;
        EquippedMutations.Add(mutationId);
        return true;
    }

    public bool UnequipMutation(string mutationId) => EquippedMutations.Remove(mutationId);

    public void AddItem(string itemId, int count = 1) { GD.Print($"[Player] AddItem: {itemId} x{count}"); }
    public void AddScrap(int amount) { GD.Print($"[Player] AddScrap: +{amount}"); }
    public void TakeDamage(int amount, string damageType = "Physical") { CurrentHp = Mathf.Max(0, CurrentHp - amount); }
    public void Heal(int amount) { CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount); }

    public PlayerSaveData GetSaveData()
    {
        var stats = new Dictionary<string, float>();
        foreach (var def in CoreStatDefinitions.AllCoreStats)
        {
            stats[def.StatId] = BaseStats.GetBase(def.StatId);
        }

        return new PlayerSaveData
        {
            BaseStats = stats,
            Level = Level,
            CurrentXp = CurrentXp,
            SignalPoints = SignalPoints,
            ResonanceFragments = ResonanceFragments,
            UnlockedSignalNodes = new List<string>(UnlockedSignalNodes),
            EquippedMutations = new List<string>(EquippedMutations),
            CurrentHp = CurrentHp,
            MaxHp = MaxHp,
            Position = Position,
            CurrentZone = CurrentZoneId
        };
    }

    public void LoadSaveData(PlayerSaveData data)
    {
        Level = data.Level;
        CurrentXp = data.CurrentXp;
        SignalPoints = data.SignalPoints;
        ResonanceFragments = data.ResonanceFragments;
        UnlockedSignalNodes.Clear();
        UnlockedSignalNodes.AddRange(data.UnlockedSignalNodes);
        EquippedMutations.Clear();
        EquippedMutations.AddRange(data.EquippedMutations);
        CurrentHp = data.CurrentHp;
        MaxHp = data.MaxHp;
        Position = data.Position;
        CurrentZoneId = data.CurrentZone;

        foreach (var kvp in data.BaseStats)
        {
            BaseStats.SetBase(kvp.Key, kvp.Value);
        }
        RecalculateDerived();
    }
}