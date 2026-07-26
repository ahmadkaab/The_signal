using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TheSignal.Systems;

namespace TheSignal.Core.Save;

public static class SaveDataManager
{
    private const int SAVE_VERSION = 1;
    private const string SAVE_DIR = "user://saves/";
    private const string AUTOSAVE_PATH = "user://autosave.json";
    private const string BACKUP_EXT = ".bak";

    public static void Initialize()
    {
        var dir = DirAccess.Open(SAVE_DIR);
        if (dir == null)
        {
            DirAccess.MakeDirRecursiveAbsolute(SAVE_DIR);
        }
    }

    public static void SaveGame(int slot, GameSaveData data)
    {
        data.Meta.Version = SAVE_VERSION;
        data.Meta.Timestamp = DateTime.UtcNow.ToString("o");
        data.Meta.Slot = slot;

        string json = JsonSerializer.Serialize(data, SaveContext.Options);
        string path = $"{SAVE_DIR}save_{slot}.json";
        string tempPath = path + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
            {
                File.Copy(path, path + BACKUP_EXT, true);
            }
            File.Move(tempPath, path, true);
            GD.Print($"Game saved to slot {slot}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Save failed: {e.Message}");
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public static GameSaveData LoadGame(int slot)
    {
        string path = $"{SAVE_DIR}save_{slot}.json";
        if (!File.Exists(path))
        {
            GD.Print($"No save found in slot {slot}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<GameSaveData>(json, SaveContext.Options);
            data = Migrate(data);
            GD.Print($"Game loaded from slot {slot} (v{data.Meta.Version} -> v{SAVE_VERSION})");
            return data;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Load failed: {e.Message}. Trying backup...");
            return LoadBackup(slot);
        }
    }

    public static void Autosave(GameSaveData data)
    {
        data.Meta.Version = SAVE_VERSION;
        data.Meta.Timestamp = DateTime.UtcNow.ToString("o");
        data.Meta.IsAutosave = true;

        string json = JsonSerializer.Serialize(data, SaveContext.Options);
        string tempPath = AUTOSAVE_PATH + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json);
            if (File.Exists(AUTOSAVE_PATH))
                File.Copy(AUTOSAVE_PATH, AUTOSAVE_PATH + BACKUP_EXT, true);
            File.Move(tempPath, AUTOSAVE_PATH, true);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Autosave failed: {e.Message}");
        }
    }

    public static GameSaveData LoadAutosave()
    {
        if (!File.Exists(AUTOSAVE_PATH)) return null;

        try
        {
            string json = File.ReadAllText(AUTOSAVE_PATH);
            var data = JsonSerializer.Deserialize<GameSaveData>(json, SaveContext.Options);
            return Migrate(data);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Autosave load failed: {e.Message}");
            return null;
        }
    }

    public static SaveMeta[] ListSaves()
    {
        var list = new List<SaveMeta>();
        var dir = DirAccess.Open(SAVE_DIR);
        if (dir == null) return list.ToArray();

        dir.ListDirBegin();
        string file = dir.GetNext();
        while (file != "")
        {
            if (file.EndsWith(".json") && file.StartsWith("save_"))
            {
                string path = SAVE_DIR + file;
                try
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<GameSaveData>(json, SaveContext.Options);
                    list.Add(data.Meta);
                }
                catch { }
            }
            file = dir.GetNext();
        }
        dir.ListDirEnd();
        list.Sort((a, b) => string.Compare(b.Timestamp, a.Timestamp, StringComparison.Ordinal));
        return list.ToArray();
    }

    public static void DeleteSave(int slot)
    {
        string path = $"{SAVE_DIR}save_{slot}.json";
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + BACKUP_EXT)) File.Delete(path + BACKUP_EXT);
    }

    private static GameSaveData Migrate(GameSaveData data)
    {
        while (data.Meta.Version < SAVE_VERSION)
        {
            data = MigrateVersion(data, data.Meta.Version);
            data.Meta.Version = SAVE_VERSION;
        }
        return data;
    }

    private static GameSaveData MigrateVersion(GameSaveData data, int fromVersion)
    {
        // Future migrations go here
        return data;
    }

    private static GameSaveData LoadBackup(int slot)
    {
        string path = $"{SAVE_DIR}save_{slot}.json{BACKUP_EXT}";
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<GameSaveData>(json, SaveContext.Options);
            GD.Print($"Loaded backup for slot {slot}");
            return Migrate(data);
        }
        catch { return null; }
    }
}

public class GameSaveData
{
    public SaveMeta Meta { get; set; } = new();
    public PlayerSaveData Player { get; set; } = new();
    public PartySaveData Party { get; set; } = new();
    public WorldSaveData World { get; set; } = new();
    public QuestSaveData Quests { get; set; } = new();
    public SectorMapSaveData SectorMap { get; set; } = new();
    public Dictionary<string, object> Flags { get; set; } = new();
}

public class SaveMeta
{
    public int Version { get; set; }
    public string Timestamp { get; set; }
    public int Slot { get; set; }
    public bool IsAutosave { get; set; }
    public int PlaytimeSeconds { get; set; }
    public string CurrentZone { get; set; }
    public int Level { get; set; }
}

public class PlayerSaveData
{
    public Dictionary<string, float> BaseStats { get; set; } = new();
    public int Level { get; set; } = 1;
    public int CurrentXp { get; set; } = 0;
    public int SignalPoints { get; set; } = 0;
    public int ResonanceFragments { get; set; } = 0;
    public List<string> UnlockedSignalNodes { get; set; } = new();
    public List<string> EquippedMutations { get; set; } = new();
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public Vector2 Position { get; set; }
    public string CurrentZone { get; set; }
}

public class PartySaveData
{
    public List<CompanionSaveData> Companions { get; set; } = new();
    public List<string> ActiveCompanionIds { get; set; } = new(); // Max 3
    public Dictionary<string, int> LoyaltyValues { get; set; } = new();
    public Dictionary<string, int> SynergyRanks { get; set; } = new();
}

public class CompanionSaveData
{
    public string CompanionId { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public Dictionary<string, float> BaseStats { get; set; } = new();
    public List<string> UnlockedAbilities { get; set; } = new();
    public Dictionary<string, int> AbilityCooldowns { get; set; } = new();
}

public class WorldSaveData
{
    public int CurrentSector { get; set; }
    public Dictionary<string, ZoneSaveData> Zones { get; set; } = new();
    public Dictionary<string, int> FactionReputation { get; set; } = new();
    public int WorldSeed { get; set; }
    public Dictionary<string, object> ZoneStates { get; set; } = new(); // Cleansed/Corrupted
}

public class ZoneSaveData
{
    public bool Discovered { get; set; }
    public bool Cleared { get; set; }
    public int CorruptionLevel { get; set; } // -100 to 100
    public List<string> CompletedEvents { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
}

public class QuestSaveData
{
    public Dictionary<string, QuestStateData> ActiveQuests { get; set; } = new();
    public Dictionary<string, QuestStateData> CompletedQuests { get; set; } = new();
    public Dictionary<string, QuestStateData> FailedQuests { get; set; } = new();
}

public class QuestStateData
{
    public string QuestId { get; set; }
    public string CurrentStage { get; set; }
    public Dictionary<string, int> ObjectiveProgress { get; set; } = new();
    public Dictionary<string, bool> ObjectiveComplete { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}

internal static class SaveContext
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new Vector2Converter() }
    };
}

internal class Vector2Converter : System.Text.Json.Serialization.JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return new Vector2(
            doc.RootElement.GetProperty("X").GetSingle(),
            doc.RootElement.GetProperty("Y").GetSingle()
        );
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}