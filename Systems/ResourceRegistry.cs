using Godot;
using System.Collections.Generic;
using TheSignal.Data;

namespace TheSignal.Systems;

public partial class ResourceRegistry : Node
{
    public static ResourceRegistry Instance { get; private set; }

    public Dictionary<string, QuestResource> Quests { get; } = new();
    public Dictionary<string, EnemyResource> Enemies { get; } = new();
    public Dictionary<string, AbilityResource> Abilities { get; } = new();
    public Dictionary<string, ItemResource> Items { get; } = new();
    public Dictionary<string, CompanionResource> Companions { get; } = new();
    public Dictionary<string, ZoneResource> Zones { get; } = new();
    public Dictionary<string, SignalNodeResource> SignalNodes { get; } = new();
    public Dictionary<string, MutationResource> Mutations { get; } = new();
    public Dictionary<string, CompanionSynergyResource> CompanionSynergies { get; } = new();
    public Dictionary<string, ZoneEventResource> ZoneEvents { get; } = new();

    public override void _Ready()
    {
        Instance = this;
        LoadAllResources();
    }

    private void LoadAllResources()
    {
        LoadDirectory<QuestResource>("res://Data/Quests/", Quests);
        LoadDirectory<EnemyResource>("res://Data/Enemies/", Enemies);
        LoadDirectory<AbilityResource>("res://Data/Abilities/", Abilities);
        LoadDirectory<ItemResource>("res://Data/Items/", Items);
        LoadDirectory<CompanionResource>("res://Data/Companions/", Companions);
        LoadDirectory<ZoneResource>("res://Data/Zones/", Zones);
        LoadDirectory<SignalNodeResource>("res://Data/Progression/SignalNodes/", SignalNodes);
        LoadDirectory<MutationResource>("res://Data/Progression/Mutations/", Mutations);
        LoadDirectory<CompanionSynergyResource>("res://Data/Companions/Synergy/", CompanionSynergies);
        LoadDirectory<ZoneEventResource>("res://Data/ZoneEvents/", ZoneEvents);

        GD.Print($"[ResourceRegistry] Loaded: {Quests.Count} quests, {Enemies.Count} enemies, {Abilities.Count} abilities, {Items.Count} items, {Companions.Count} companions, {Zones.Count} zones, {SignalNodes.Count} signal nodes, {Mutations.Count} mutations, {CompanionSynergies.Count} synergies, {ZoneEvents.Count} zone events");
    }

    private void LoadDirectory<T>(string path, Dictionary<string, T> dict) where T : Resource
    {
        var dir = DirAccess.Open(path);
        if (dir == null) return;

        dir.ListDirBegin();
        string file = dir.GetNext();
        while (file != "")
        {
            if (file.EndsWith(".tres") || file.EndsWith(".res"))
            {
                var resource = GD.Load<T>($"{path}{file}");
                if (resource != null)
                {
                    string id = resource.ResourceName;
                    if (string.IsNullOrEmpty(id)) id = file[..^5];
                    dict[id] = resource;
                }
            }
            file = dir.GetNext();
        }
        dir.ListDirEnd();
    }

    public QuestResource GetQuest(string id) => Quests.GetValueOrDefault(id);
    public EnemyResource GetEnemy(string id) => Enemies.GetValueOrDefault(id);
    public AbilityResource GetAbility(string id) => Abilities.GetValueOrDefault(id);
    public ItemResource GetItem(string id) => Items.GetValueOrDefault(id);
    public CompanionResource GetCompanion(string id) => Companions.GetValueOrDefault(id);
    public ZoneResource GetZone(string id) => Zones.GetValueOrDefault(id);
    public SignalNodeResource GetSignalNode(string id) => SignalNodes.GetValueOrDefault(id);
    public MutationResource GetMutation(string id) => Mutations.GetValueOrDefault(id);
    public CompanionSynergyResource GetCompanionSynergy(string id) => CompanionSynergies.GetValueOrDefault(id);
    public ZoneEventResource GetZoneEvent(string id) => ZoneEvents.GetValueOrDefault(id);

    public IReadOnlyDictionary<string, QuestResource> AllQuests => Quests;
    public IReadOnlyDictionary<string, EnemyResource> AllEnemies => Enemies;
    public IReadOnlyDictionary<string, AbilityResource> AllAbilities => Abilities;
    public IReadOnlyDictionary<string, ItemResource> AllItems => Items;
    public IReadOnlyDictionary<string, CompanionResource> AllCompanions => Companions;
    public IReadOnlyDictionary<string, ZoneResource> AllZones => Zones;
    public IReadOnlyDictionary<string, SignalNodeResource> AllSignalNodes => SignalNodes;
    public IReadOnlyDictionary<string, MutationResource> AllMutations => Mutations;
    public IReadOnlyDictionary<string, CompanionSynergyResource> AllCompanionSynergies => CompanionSynergies;
    public IReadOnlyDictionary<string, ZoneEventResource> AllZoneEvents => ZoneEvents;
}