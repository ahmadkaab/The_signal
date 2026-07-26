using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Core.Save;

namespace TheSignal.Systems;

public class QuestManager
{
    public static QuestManager Instance { get; } = new();
    public Dictionary<string, QuestInstance> ActiveQuests { get; } = new();
    public Dictionary<string, QuestInstance> CompletedQuests { get; } = new();
    public Dictionary<string, QuestInstance> FailedQuests { get; } = new();

    public event System.Action<string> OnQuestStarted;
    public event System.Action<string> OnQuestCompleted;
    public event System.Action<string> OnQuestFailed;
    public event System.Action<string, string, int> OnObjectiveProgress;

    public void Initialize()
    {
        // Load quest definitions from resources
    }

    public void StartQuest(string questId)
    {
        var def = ResourceRegistry.Instance.GetQuest(questId);
        if (def == null) return;

        var instance = new QuestInstance
        {
            QuestId = questId,
            CurrentStage = def.StartStage,
            State = QuestState.Active
        };
        foreach (var obj in def.Stages[def.StartStage].Objectives)
        {
            instance.ObjectiveProgress[obj.Id] = 0;
            instance.ObjectiveComplete[obj.Id] = false;
        }

        ActiveQuests[questId] = instance;
        OnQuestStarted?.Invoke(questId);
        GD.Print($"Quest Started: {def.DisplayName}");
    }

    public void AdvanceObjective(string questId, string eventType, Godot.Collections.Dictionary data)
    {
        if (!ActiveQuests.TryGetValue(questId, out var instance)) return;

        var def = ResourceRegistry.Instance.GetQuest(questId);
        if (def == null) return;

        var stage = def.Stages[instance.CurrentStage];
        bool stageComplete = true;

        foreach (var obj in stage.Objectives)
        {
            if (instance.ObjectiveComplete[obj.Id]) continue;
            if (obj.EventType == eventType && ConditionsMatch(obj.Conditions, data))
            {
                instance.ObjectiveProgress[obj.Id]++;
                OnObjectiveProgress?.Invoke(questId, obj.Id, instance.ObjectiveProgress[obj.Id]);

                if (instance.ObjectiveProgress[obj.Id] >= obj.RequiredCount)
                {
                    instance.ObjectiveComplete[obj.Id] = true;
                    GD.Print($"Objective Complete: {questId} - {obj.Id}");
                }
            }
            if (!instance.ObjectiveComplete[obj.Id])
                stageComplete = false;
        }

        if (stageComplete)
        {
            CompleteStage(questId);
        }
    }

    private bool ConditionsMatch(Godot.Collections.Dictionary conditions, Godot.Collections.Dictionary data)
    {
        foreach (var key in conditions.Keys)
        {
            string keyStr = key.ToString();
            if (!data.ContainsKey(keyStr) && !data.ContainsKey(key))
                return false;
            var expected = conditions[key];
            var actual = data.ContainsKey(key) ? data[key] : data[keyStr];
            if (!expected.Equals(actual))
                return false;
        }
        return true;
    }

    private void CompleteStage(string questId)
    {
        var instance = ActiveQuests[questId];
        var def = ResourceRegistry.Instance.GetQuest(questId);
        var stage = def.Stages[instance.CurrentStage];

        // Grant rewards
        GrantRewards(stage.Rewards);

        if (stage.NextStage != null)
        {
            instance.CurrentStage = stage.NextStage;
            var nextStage = def.Stages[instance.CurrentStage];
            foreach (var obj in nextStage.Objectives)
            {
                instance.ObjectiveProgress[obj.Id] = 0;
                instance.ObjectiveComplete[obj.Id] = false;
            }
        }
        else
        {
            CompleteQuest(questId);
        }
    }

    private void CompleteQuest(string questId)
    {
        var instance = ActiveQuests[questId];
        instance.State = QuestState.Complete;
        ActiveQuests.Remove(questId);
        CompletedQuests[questId] = instance;
        OnQuestCompleted?.Invoke(questId);
        GD.Print($"Quest Completed: {questId}");
    }

    public void FailQuest(string questId)
    {
        if (!ActiveQuests.TryGetValue(questId, out var instance)) return;
        instance.State = QuestState.Failed;
        ActiveQuests.Remove(questId);
        FailedQuests[questId] = instance;
        OnQuestFailed?.Invoke(questId);
    }

    private void GrantRewards(QuestRewards rewards)
    {
        GameManager.Instance.Player.GainXp(rewards.Xp);
        GameManager.Instance.Player.SignalPoints += rewards.SignalPoints;
        GameManager.Instance.Player.ResonanceFragments += rewards.ResonanceFragments;
        // Items, companions, etc.
    }

    public QuestSaveData GetSaveData()
    {
        return new QuestSaveData
        {
            ActiveQuests = SerializeQuests(ActiveQuests),
            CompletedQuests = SerializeQuests(CompletedQuests),
            FailedQuests = SerializeQuests(FailedQuests)
        };
    }

    public void LoadSaveData(QuestSaveData data)
    {
        ActiveQuests.Clear();
        CompletedQuests.Clear();
        FailedQuests.Clear();

        foreach (var kvp in data.ActiveQuests)
            ActiveQuests[kvp.Key] = DeserializeQuest(kvp.Value);
        foreach (var kvp in data.CompletedQuests)
            CompletedQuests[kvp.Key] = DeserializeQuest(kvp.Value);
        foreach (var kvp in data.FailedQuests)
            FailedQuests[kvp.Key] = DeserializeQuest(kvp.Value);
    }

    private QuestInstance DeserializeQuest(QuestStateData data)
    {
        return new QuestInstance
        {
            QuestId = data.QuestId,
            CurrentStage = data.CurrentStage,
            State = QuestState.Active,
            ObjectiveProgress = new Dictionary<string, int>(data.ObjectiveProgress),
            ObjectiveComplete = new Dictionary<string, bool>(data.ObjectiveComplete),
            Variables = new Dictionary<string, object>(data.Variables)
        };
    }

    private Dictionary<string, QuestStateData> SerializeQuests(Dictionary<string, QuestInstance> quests)
    {
        var result = new Dictionary<string, QuestStateData>();
        foreach (var kvp in quests)
        {
            result[kvp.Key] = new QuestStateData
            {
                QuestId = kvp.Value.QuestId,
                CurrentStage = kvp.Value.CurrentStage,
                ObjectiveProgress = new Dictionary<string, int>(kvp.Value.ObjectiveProgress),
                ObjectiveComplete = new Dictionary<string, bool>(kvp.Value.ObjectiveComplete),
                Variables = new Dictionary<string, object>(kvp.Value.Variables)
            };
        }
        return result;
    }
}

public class QuestInstance
{
    public string QuestId { get; set; }
    public string CurrentStage { get; set; }
    public QuestState State { get; set; } = QuestState.Active;
    public Dictionary<string, int> ObjectiveProgress { get; set; } = new();
    public Dictionary<string, bool> ObjectiveComplete { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}
