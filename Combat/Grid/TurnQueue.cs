using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Combat.Units;
using TheSignal.Systems;

namespace TheSignal.Combat;

[GlobalClass]
public partial class TurnQueue : Node
{
    private List<TurnEntry> _queue = new();
    private int _currentIndex = 0;
    private int _roundNumber = 0;

    public event Action<TurnEntry> OnTurnStarted;
    public event Action<TurnEntry> OnTurnEnded;
    public event Action<int> OnRoundChanged;

    public TurnEntry CurrentTurn => _currentIndex < _queue.Count ? _queue[_currentIndex] : null;
    public IReadOnlyList<TurnEntry> Queue => _queue;
    public int CurrentIndex => _currentIndex;
    public int RoundNumber => _roundNumber;

    public void Initialize(List<UnitInstance> units)
    {
        _queue.Clear();
        _currentIndex = 0;
        _roundNumber = 0;

        foreach (var unit in units)
        {
            if (unit.CurrentHp > 0)
            {
                _queue.Add(new TurnEntry
                {
                    Unit = unit,
                    Initiative = unit.Initiative,
                    HasActedThisRound = false
                });
            }
        }

        SortQueue();
    }

    public void SortQueue()
    {
        _queue.Sort((a, b) =>
        {
            // Higher initiative first
            int initCompare = b.Initiative.CompareTo(a.Initiative);
            if (initCompare != 0) return initCompare;

            // Player/companion units go first on tie
            int typeCompare = a.Unit.Type.CompareTo(b.Unit.Type);
            return typeCompare;
        });
    }

    public TurnEntry GetNextTurn()
    {
        if (_queue.Count == 0) return null;

        // Skip dead units
        while (_currentIndex < _queue.Count && _queue[_currentIndex].Unit.CurrentHp <= 0)
        {
            _currentIndex++;
        }

        if (_currentIndex >= _queue.Count)
        {
            // End of round, start new round
            StartNewRound();
            return GetNextTurn();
        }

        var entry = _queue[_currentIndex];
        entry.HasActedThisRound = true;
        OnTurnStarted?.Invoke(entry);
        return entry;
    }

    public void EndCurrentTurn()
    {
        if (_currentIndex < _queue.Count)
        {
            var entry = _queue[_currentIndex];
            OnTurnEnded?.Invoke(entry);
        }

        _currentIndex++;
        GetNextTurn();
    }

    public void StartNewRound()
    {
        _roundNumber++;
        _currentIndex = 0;

        foreach (var entry in _queue)
        {
            entry.HasActedThisRound = false;
            // Reset AP, cooldowns, status effects
            entry.Unit.CurrentAp = entry.Unit.MaxAp;
            entry.Unit.ProcessTurnStart();
        }

        OnRoundChanged?.Invoke(_roundNumber);
    }

    public void RemoveUnit(UnitInstance unit)
    {
        int index = _queue.FindIndex(e => e.Unit == unit);
        if (index >= 0)
        {
            if (index < _currentIndex)
                _currentIndex--;
            _queue.RemoveAt(index);
        }
    }

    public void AddUnit(UnitInstance unit)
    {
        _queue.Add(new TurnEntry
        {
            Unit = unit,
            Initiative = unit.Initiative,
            HasActedThisRound = false
        });
        SortQueue();
    }

    public List<TurnEntry> GetUpcomingTurns(int count = 5)
    {
        var result = new List<TurnEntry>();
        int index = _currentIndex;
        int rounds = 0;

        while (result.Count < count && rounds < 2)
        {
            if (index >= _queue.Count)
            {
                index = 0;
                rounds++;
            }
            if (index < _queue.Count && _queue[index].Unit.CurrentHp > 0)
            {
                result.Add(_queue[index]);
            }
            index++;
        }
        return result;
    }

    public int GetTurnsUntilUnit(UnitInstance unit)
    {
        for (int i = _currentIndex; i < _queue.Count; i++)
        {
            if (_queue[i].Unit == unit) return i - _currentIndex;
        }
        for (int i = 0; i < _currentIndex; i++)
        {
            if (_queue[i].Unit == unit) return _queue.Count - _currentIndex + i;
        }
        return -1;
    }

    public void RecalculateInitiative()
    {
        foreach (var entry in _queue)
        {
            entry.Initiative = entry.Unit.Initiative;
        }
        SortQueue();
    }

    public TurnQueueSaveData GetSaveData()
    {
        var data = new TurnQueueSaveData
        {
            RoundNumber = _roundNumber,
            CurrentIndex = _currentIndex,
            Entries = new List<TurnEntryData>()
        };

        foreach (var entry in _queue)
        {
            data.Entries.Add(new TurnEntryData
            {
                UnitId = entry.Unit.UnitId,
                Initiative = entry.Initiative,
                HasActedThisRound = entry.HasActedThisRound
            });
        }

        return data;
    }

    public void LoadSaveData(TurnQueueSaveData data)
    {
        _roundNumber = data.RoundNumber;
        _currentIndex = data.CurrentIndex;
        _queue.Clear();

        foreach (var entryData in data.Entries)
        {
            // Unit will be resolved after loading
            _queue.Add(new TurnEntry
            {
                Unit = null, // Will be set later
                UnitId = entryData.UnitId,
                Initiative = entryData.Initiative,
                HasActedThisRound = entryData.HasActedThisRound
            });
        }
    }
}

public class TurnEntry
{
    public UnitInstance Unit { get; set; }
    public string UnitId { get; set; }
    public int Initiative { get; set; }
    public bool HasActedThisRound { get; set; }
}

public class TurnQueueSaveData
{
    public int RoundNumber { get; set; }
    public int CurrentIndex { get; set; }
    public List<TurnEntryData> Entries { get; set; } = new();
}

public class TurnEntryData
{
    public string UnitId { get; set; }
    public int Initiative { get; set; }
    public bool HasActedThisRound { get; set; }
}