# T1F Fix Plan — The Signal Compile Errors

**Date:** 2026-07-26  
**Status:** Plan — ready for Bug-Fixer agents  
**Total remaining errors:** ~189 across 26 files  
**Strategy:** Fix in 3 priority waves — stub bulk-updates first (biggest wins), then calling code patches, then final type additions.

---

## Priority 1: Stub Files to Bulk-Update (Quickest Wins)

These changes add missing members to existing stub types. Each fix resolves 3–15 errors.

### 1.1 `Core/SaveSlotData.cs` — Add 5 missing properties (~5 errors, NewGamePlusManager)

The type `SaveSlotData` is used as a proxy for the whole save file metadata, but callers expect NG+/prestige fields.

**Add these properties:**
```csharp
public int PrestigeLevel { get; set; }
public List<string> ActiveMutations { get; set; } = new();
public List<string> EquippedItems { get; set; } = new();
public Dictionary<string, int> CompanionSynergyRanks { get; set; } = new();
public Dictionary<string, int> FactionReputation { get; set; } = new();
```

**File:** `G:/games_i_created/TheSignal/Core/SaveSlotData.cs`

---

### 1.2 `Core/Party.cs` — Fix GetSaveData/LoadSaveData return types (~2 errors, GameManager)

`Party.GetSaveData()` returns `SaveSlotData` but `GameManager.SaveGame()` assigns it to a `PartySaveData` property. `Party.LoadSaveData()` takes `SaveSlotData` but `GameManager.LoadGame()` passes `PartySaveData`.

**Replace both methods:**
```csharp
public PartySaveData GetSaveData()
{
    return new PartySaveData
    {
        ActiveCompanionIds = ActiveCompanionIds.ToList(), // Add System.Linq if needed
        // Populate other fields as available
    };
}

public void LoadSaveData(PartySaveData data)
{
    ActiveCompanionIds.Clear();
    if (data?.ActiveCompanionIds != null)
        foreach (var id in data.ActiveCompanionIds)
            ActiveCompanionIds.Add(id);
}
```

**File:** `G:/games_i_created/TheSignal/Core/Party.cs`  
**Add using:** `using TheSignal.Core.Save;`

---

### 1.3 `Core/Progression/Player.cs` — Add missing methods and fix accessibility (~8 errors, across GameManager, QuestManager, TutorialZone, PlayerController)

**Changes:**

1. **Add `GainXp(int)` overload** (without `ProgressionFormulas`):
   ```csharp
   public void GainXp(int amount)
   {
       GameManager.Instance?.ProgressionFormulas is ProgressionFormulas formulas
           ? GainXp(amount, formulas)
           : GainXp(amount, new ProgressionFormulas());
   }
   ```

2. **Change `SignalPoints` setter to public:**
   ```csharp
   public int SignalPoints { get; set; } = 0;
   ```

3. **Change `ResonanceFragments` setter to public:**
   ```csharp
   public int ResonanceFragments { get; set; } = 0;
   ```

4. **Add `AddItem` method:**
   ```csharp
   public void AddItem(string itemId, int count = 1)
   {
       // Stub — inventory system would handle this
       GD.Print($"[Player] AddItem: {itemId} x{count}");
   }
   ```

5. **Add `AddScrap` method:**
   ```csharp
   public void AddScrap(int amount)
   {
       // Stub — would integrate with inventory/resource system
       GD.Print($"[Player] AddScrap: +{amount}");
   }
   ```

6. **Add `TakeDamage` method:**
   ```csharp
   public void TakeDamage(int amount, DamageType type = DamageType.Physical)
   {
       CurrentHp = Mathf.Max(0, CurrentHp - amount);
       GD.Print($"[Player] TakeDamage: {amount} {type}");
   }
   ```

7. **Add `Heal` method:**
   ```csharp
   public void Heal(int amount)
   {
       CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
       GD.Print($"[Player] Heal: +{amount}");
   }
   ```

**Add using:** `using TheSignal.Systems;` (for GameManager reference)  
**File:** `G:/games_i_created/TheSignal/Core/Progression/Player.cs`

---

### 1.4 `Combat/Units/UnitInstance.cs` — Add Stats property and missing overloads (~10 errors, CombatManager, AbilityExecutor)

**Changes:**

1. **Add `Stats` property** (code uses `target.Stats.Armor`, `actor.Stats.CritChance`, `actor.Stats.CritDamage`):
   ```csharp
   // Self-referencing property — UnitInstance IS the stats holder
   public UnitInstance Stats => this;
   ```

2. **Add extra `ApplyStatModifier` overload** with 6 params:
   ```csharp
   public void ApplyStatModifier(StatType stat, float flatBonus, float percentBonus, int duration, string sourceId, bool isDebuff)
   {
       // Stub
       ApplyStatModifier(stat.ToString(), flatBonus, percentBonus, duration);
   }
   ```

   Since the original signature uses `string stat` but callers pass `StatType stat`, change one or both to match. **Recommended:** keep the string overload and add a StatType overload:
   ```csharp
   public void ApplyStatModifier(StatType stat, float flatBonus, float percentBonus, int duration, string sourceId, bool isDebuff)
   {
       ApplyStatModifier(stat.ToString(), flatBonus, percentBonus, duration);
   }
   ```

3. **Add `ApplyStatusEffect` overload** with individual params:
   ```csharp
   public void ApplyStatusEffect(StatusEffectType effectType, int duration, int stacks, int maxStacks)
   {
       var effect = new StatusEffectInstance
       {
           EffectId = $"{effectType}_{UnitId}",
           EffectType = effectType,
           RemainingTurns = duration,
           Stacks = stacks,
           MaxStacks = maxStacks,
           Potency = 1,
           Source = this
       };
       ApplyStatusEffect(effect);
   }
   ```

**Add using:** No new using statements needed.  
**File:** `G:/games_i_created/TheSignal/Combat/Units/UnitInstance.cs`

---

### 1.5 `Data/CompanionResource.cs` — Add UnitScene property (~1 error, TutorialZone)

**Add property:**
```csharp
[Export] public PackedScene UnitScene { get; set; }
```

**File:** `G:/games_i_created/TheSignal/Data/CompanionResource.cs`

---

### 1.6 `Data/CombatEncounter.cs` — Add alias properties (~6 errors, EncounterGenerator)

The generated code from EncounterGenerator sets `EncounterName`, `MinLevel`, `MaxLevel`, `EnemyUnits`, `Rewards` but the type has `DisplayName`, no MinLevel/MaxLevel, `EnemySpawns`, and `Rewards` as `EncounterRewards` (not `QuestRewards`).

**Add alias/extra properties:**
```csharp
// Alias for DisplayName
[Export] public string EncounterName { get => DisplayName; set => DisplayName = value; }

// Min/Max level
[Export] public int MinLevel { get; set; } = 1;
[Export] public int MaxLevel { get; set; } = 10;

// Enemy spawns (as List<EnemySpawnInfo>)
public List<EnemySpawnInfo> EnemyUnits { get; set; } = new();

// Rewards (QuestRewards type)
[Export] public QuestRewards Rewards { get; set; }
```

**Also create (`EnemySpawnInfo` class)** — add at bottom of same file:
```csharp
public class EnemySpawnInfo
{
    public UnitData UnitData { get; set; }
    public int Position { get; set; }
    public int SpawnDelay { get; set; }
}
```

**Add using:** `using TheSignal.Systems;` (for QuestRewards)  
**File:** `G:/games_i_created/TheSignal/Data/CombatEncounter.cs`

---

### 1.7 `Systems/SectorMapManager.cs` — Add Initialize method and zone events (~3 errors, GameManager, SectorMapManager)

**Add `Initialize()` method to SectorMapManager:**
```csharp
public void Initialize()
{
    LoadZoneResources();
    BuildConnections();
    InitializeZoneStates();
    GD.Print("[SectorMapManager] Initialized");
}
```

**Add `OnZoneCleansed` and `OnZoneCorrupted` events** (referenced on lines 278, 284):
```csharp
public event ZoneEventDelegate OnZoneCleansed;
public event ZoneEventDelegate OnZoneCorrupted;
```

**File:** `G:/games_i_created/TheSignal/Systems/SectorMapManager.cs`

---

### 1.8 `Systems/ZoneState` and `ZoneStateData` — Add missing properties (~5 errors, WorldManager, ZoneNode)

**Add to `ZoneState` class** (in SectorMapManager.cs):
```csharp
public bool HasActiveEncounter { get; set; }
public bool HasActiveQuest { get; set; }
public HashSet<string> CollectedItems { get; set; } = new();
```

**Add to `ZoneStateData` class** (in SectorMapManager.cs):
```csharp
public bool Cleansed { get; set; }
public bool Corrupted { get; set; }
public List<string> CollectedItems { get; set; } = new();
```

Also add `CompletedEncounters` to ZoneStateData (ZoneState has `CompletedEncounters` as `HashSet<string>`, code references it):
Wait — ZoneStateData already has `CompletedEncounters`? No, looking at the code on line 88 in WorldManager:
```csharp
CompletedEncounters = new List<string>(kvp.Value.CompletedEncounters),
CollectedItems = new List<string>(kvp.Value.CollectedItems)
```

ZoneStateData does NOT have `CompletedEncounters`. Let me check — no, looking at lines 396-405:
```csharp
public class ZoneStateData
{
    public bool Discovered { get; set; }
    public bool Cleared { get; set; }
    public float CorruptionLevel { get; set; }
    public bool Visited { get; set; }
    public List<string> CompletedEncounters { get; set; } = new();
    public List<string> CompletedEvents { get; set; } = new();
    public Dictionary<string, long> EventCooldowns { get; set; } = new();
}
```

OK so ZoneStateData already has `CompletedEncounters` and `CompletedEvents`. Let me check if WorldManager references `Cleansed` and `Corrupted` on ZoneStateData — WorldManager creates ZoneStateData on lines 83-90:
```csharp
zones[kvp.Key] = new ZoneStateData
{
    CorruptionLevel = kvp.Value.CorruptionLevel,
    Cleansed = kvp.Value.IsCleansed,
    Corrupted = kvp.Value.IsCorrupted,
    CompletedEncounters = new List<string>(kvp.Value.CompletedEncounters),
    CollectedItems = new List<string>(kvp.Value.CollectedItems)
};
```

So WorldManager sets `Cleansed`, `Corrupted`, and `CollectedItems` on ZoneStateData. These need to be added.

**File:** `G:/games_i_created/TheSignal/Systems/SectorMapManager.cs`

---

## Priority 2: Calling Code to Patch

These are edits in the calling files that fix mismatches between what the code expects and what types actually provide.

### 2.1 `Systems/CombatManager.cs` — Fix `Grid` reference, `BeginTurn`, `AdvanceTurn` (~5 errors)

1. **Replace `Grid.SetUnitOccupying(...)` / `Grid.GetUnitsInCircle(...)` etc. with a local `CombatGrid` reference:**
   - Add a field: `private CombatGrid _grid;`
   - Initialize in `_Ready()`: `_grid = GetNode<CombatGrid>("%CombatGrid");`
   - Replace all `Grid.SetUnitOccupying(...)` → `_grid.SetUnitOccupying(...)`
   - Replace all `Grid.GetUnitsInCircle(...)` → `_grid.GetCellsInCircle(...)` (same pattern)

2. **Fix `BeginTurn(firstTurn)` call (line 60):** rename to `OnTurnStarted(firstTurn)` since that's the existing method.

3. **Fix `TurnQueue.AdvanceTurn()` call (line 118):** rename to `TurnQueue.EndCurrentTurn()` which is the existing method.

### 2.2 `Systems/QuestBoardManager.cs` — Fix tuple types and QuestType (~15 errors)

1. **Fix tuple declarations (lines 255-276 and 297-313):** The tuple count is 7 or 8, but `foreach` deconstructs into different numbers.
   - Line 255: Tuple is `(string, string, string, string, int, int, string)` — 7 elements
   - Line 278: Destructured as `(questId, zone, title, desc, minLvl, reward, rewardType)` — 7 variables
   - Line 297: Tuple is a string array, not tuples! `string[] uniques = { ("...", "...", ...) }` — string arrays can't hold tuples.
   
   **Fix:** Change the tuples to proper `var` arrays of named tuples:
   For repeatables:
   ```csharp
   var repeatables = new[]
   {
       (questId: "Q_REP_...", zone: "S09_...", title: "...", desc: "...", minLvl: 1, reward: 10, rewardType: "scrap"),
       ...
   };
   ```
   For uniques:
   ```csharp
   var uniques = new[]
   {
       (questId: "Q_UNIQUE_...", zone: "S08_...", title: "...", desc: "...", minLvl: 5, reward: 30, rewardType: "rare_weapon", completion: "..."),
       ...
   };
   ```

2. **Fix `QuestType = "repeatable"` (line 141):** `QuestResource.QuestType` is of type `QuestType` (enum), not string. Change:
   ```csharp
   QuestType = QuestType.Side  // or appropriate enum value
   ```
   Or add a string alias property to QuestResource:
   ```csharp
   // In QuestResource.cs
   [Export] public string QuestType { get => Type.ToString(); set { if (Enum.TryParse<QuestType>(value, out var t)) Type = t; } }
   ```

### 2.3 `Combat/Abilities/AbilityExecutor.cs` — Fix event access, namespace, variable scope (~7 errors)

1. **Fix `_combatManager.OnActionExecuted?.Invoke(action)` (line 88):** Change to trigger a method on CombatManager instead:
   ```csharp
   _combatManager.NotifyActionExecuted(action);
   ```
   And add to CombatManager:
   ```csharp
   public void NotifyActionExecuted(CombatAction action) => OnActionExecuted?.Invoke(action);
   ```

2. **Fix namespace of `CombatAction` (line 47):** The local `CombatAction` class is in namespace `TheSignal.Combat` (same file), but `CombatManager`'s event expects `TheSignal.Systems.CombatAction`. Remove the local `CombatAction` class and use one shared definition.

   **Recommended:** Move the `CombatAction` class from AbilityExecutor.cs to CombatManager.cs (or its own file) in the `TheSignal.Systems` namespace.

3. **Fix `target` variable scope (lines 76-79):** The loop variable `target` from `foreach (var target in targets)` is used inside `ApplyPositionEffect` and `ApplyResourceEffect` at the outer scope. Move these calls inside the foreach loop.

### 2.4 `Scenes/Exploration/TutorialZone.cs` — Fix Initialize signature and type (~5 errors)

1. **Fix `unit.Initialize(data)` calls (lines 131, 181):** `UnitInstance.Initialize` expects `(CombatGrid, UnitData)`. Create overload:
   ```csharp
   // In TutorialZone — use helper method or add overload to UnitInstance
   unit.Initialize(/* pass data fields directly */);
   ```
   **Best fix:** Since these are exploration-unit initializations (not combat), add a simpler `Initialize` overload to UnitInstance:
   ```csharp
   public void Initialize(CompanionResource data)
   {
       UnitId = data.CompanionId;
       DisplayName = data.DisplayName;
       // Set other fields...
   }
   ```
   Or pass the enemy data directly:
   ```csharp
   public void Initialize(EnemyResource data)
   {
       UnitId = data.EnemyId;
       DisplayName = data.DisplayName;
       // etc.
   }
   ```

2. **Fix `PlayerController` added to `List<UnitInstance>` (line 163):** `PlayerController` is a `CharacterBody3D`, not `UnitInstance`. Either make `PlayerController` extend `UnitInstance` or create a wrapper.

   **Recommended:** In TutorialZone, wrap the player:
   ```csharp
   var playerUnit = new UnitInstance();
   playerUnit.UnitId = "player";
   playerUnit.DisplayName = "Walker";
   playerUnit.Type = UnitType.Player;
   // Set other stats
   var playerUnits = new List<UnitInstance> { playerUnit };
   ```

3. **Fix `UnitScene` null check (line 125-126):**
   ```csharp
   var unitScene = data.UnitScene;
   if (unitScene == null)
   {
       unitScene = GD.Load<PackedScene>("res://Combat/Units/UnitInstance.tscn");
   }
   ```

### 2.5 `Systems/QuestManager.cs` — Fix QuestCondition usage and property access (~5 errors)

1. **Fix `QuestCondition` usage:** `QuestCondition` is an enum, but code uses `cond.Key` (string) and `cond.Check(value)` (method).

   **Recommended:** Change `MatchesConditions` to not use `cond.Key`:
   ```csharp
   private bool MatchesConditions(Godot.Collections.Dictionary conditions, Dictionary<string, object> data)
   {
       foreach (var key in conditions.Keys)
       {
           var keyStr = key.AsString();
           if (!data.TryGetValue(keyStr, out var value)) return false;
           // Check condition value
       }
       return true;
   }
   ```

2. **Fix `Player.SignalPoints +=` / `Player.ResonanceFragments +=`:** These are now public set (fixed in Priority 1.3).

3. **Fix `obj.Id` (line 37 in QuestManager):** The `ClientResources.Array<QuestObjective>` items have `Id` property as `[Export] public string Id { get; set; }` — this is correct. But line 37 does `foreach (var obj in def.Stages[def.StartStage].Objectives)` — the `Objectives` is `Godot.Collections.Array<QuestObjective>`, so `var obj` should work.

### 2.6 `Content/UI/UIPolish.cs` — Fix MouseFilterEnum and ThemeFontSize (~5 errors)

1. **Fix `MouseFilterEnum.Ignore` (lines 49, 90):** In Godot 4.3+, use `Control.MouseFilterEnum.Ignore`:
   ```csharp
   MouseFilter = Control.MouseFilterEnum.Ignore
   ```

2. **Fix `Label.ThemeFontSize` (line 55):** Should be:
   ```csharp
   _tooltipTitle = new Label();
   _tooltipTitle.AddThemeFontSizeOverride("font_size", 14);
   // Or
   _tooltipTitle.AddThemeConstantOverride("font_size", 14);
   ```
   Actually in Godot 4, theme properties are set via `AddThemeFontSizeOverride`:
   ```csharp
   _tooltipTitle = new Label();
   _tooltipTitle.AddThemeFontSizeOverride("font_size", 14);
   _tooltipDesc = new RichTextLabel();
   _tooltipDesc.AddThemeFontSizeOverride("font_size", 11);
   ```

3. **Fix `MouseFilterEnum.Pass` (line 90):** → `Control.MouseFilterEnum.Pass`

4. **Fix `InputManager.Instance` reference (line 122):** Add `using TheSignal.Systems;` or fully qualify.

### 2.7 `Scenes/Player/PlayerController.cs` — Fix ProgressionFormulas method and AnimationPlayer (~3 errors)

1. **Fix `ProgressionFormulas.GetDerivedStats(...)` (line 57):** This method doesn't exist. Either:
   - Add it to `ProgressionFormulas`: `public DerivedStats GetDerivedStats(StatBlock stats, int level) { return new DerivedStats(); }`
   - Or inline the stat calculation in PlayerController.

2. **Fix `AnimationPlayer.PlaybackSpeed` (lines 162, 167):** In Godot 4.3+, the property is `SpeedScale`:
   ```csharp
   _animationPlayer.SpeedScale = speed / SprintSpeed;
   ```

### 2.8 `Scenes/UI/TacticalHUD.cs` — Fix CombatManager references (~5 errors)

1. **Fix `CombatManager.Instance.EndTurn()` (line 31):** It exists — `public void EndTurn()`. This should work.
   Wait — `TacticalHUD` uses `CombatManager.Instance` but the namespace is `TheSignal.Scenes.UI`. Let me check — the file has `using TheSignal.Systems;` (line 7 is cut off in the read, let me check line 6...) No, looking at the file imports:
   ```csharp
   using Godot;
   using TheSignal.Core;
   using TheSignal.Core.Stats;
   using TheSignal.Core.Progression;
   using TheSignal.Combat.Units;
   using TheSignal.Data;
   ```
   Missing: `using TheSignal.Systems;`. Add it.

2. **Fix `ResourceRegistry.Instance` (line 56):** Missing using directive — add `using TheSignal.Systems;`.

3. **Fix `Time.GetTimeDict()` (line 92):** Godot 4.3's `Time` singleton doesn't have `GetTimeDict()`. Replace with:
   ```csharp
   var now = Time.GetTimeDictFromSystem();
   // Alternative:
   var dt = System.DateTime.Now;
   $"{dt.Hour:D2}:{dt.Minute:D2}"
   ```

---

## Priority 3: New Types & Final Additions

### 3.1 `Combat/Grid/TurnQueue.cs` — Add `AdvanceTurn()` method

The CombatManager calls `TurnQueue.AdvanceTurn()`. Add:
```csharp
public void AdvanceTurn()
{
    EndCurrentTurn();
}
```

### 3.2 `Core/Save/SaveManager.cs` — Add `Initialize()` static method call compat

The core save manager class is `SaveDataManager` (not `SaveManager`). Either:
- `GameManager.cs` calls `SaveManager.Initialize()` — change to `SaveDataManager.Initialize()`
- Or add an alias:
  ```csharp
  public static class SaveManager
  {
      public static void Initialize() => SaveDataManager.Initialize();
      // ... delegate other methods
  }
  ```

### 3.3 `Core/Progression/ProgressionFormulas.cs` — Add `GetDerivedStats()` method

```csharp
public DerivedStats GetDerivedStats(StatBlock stats, int level)
{
    return new DerivedStats
    {
        Might = (int)stats.GetBase("might"),
        Agility = (int)stats.GetBase("agility"),
        Constitution = (int)stats.GetBase("constitution"),
        Intelligence = (int)stats.GetBase("intelligence"),
        Willpower = (int)stats.GetBase("willpower"),
        Resonance = (int)stats.GetBase("resonance")
    };
}
```

Add `using TheSignal.Core.Stats;` if needed.

### 3.4 `Combat/Grid/CombatGrid.cs` — Fix TileMap.SetCellModulate

The `SetCellModulate` method may not exist in Godot 4.3 for TileMap. Replace with:
```csharp
// In HighlightCell method — line 314
_highlightTileMap.SetCell(0, coord, 0, new Vector2I(2, 0));
// Remove SetCellModulate
// TileMap in Godot 4 uses tile animations and layers differently
// Add tile modulation via tile_set or use a separate highlight system
```

### 3.5 `Systems/FactionWarManager.cs` — Fix GameManager.TotalPlayTime (~1 error)

`GameManager` doesn't have `TotalPlayTime`. Either:
- Add to GameManager: `public float TotalPlayTime { get; set; }`
- Or replace in FactionWarManager:
  ```csharp
  LastChanged = GameManager.Instance?.TotalPlayTime ?? 0f
  ```
  → Change to:
  ```csharp
  LastChanged = 0f  // Or use a real timer
  ```

### 3.6 `Systems/GameManager.cs` — Fix SaveManager.LoadGame args (~2 errors)

`SaveManager.LoadGame(string)` called but `SaveDataManager.LoadGame(int)` is the actual signature. Fix the call:
```csharp
// Change:
var data = SaveManager.LoadGame(saveName);
// To:
int slot = int.TryParse(saveName, out var s) ? s : 0;
var data = SaveDataManager.LoadGame(slot);
```

Wait, looking at the code more carefully:
- `Systems/SaveManager.cs`: Uses `SaveManager.SaveGame(int slot, ...)` and `SaveManager.LoadGame(int slot)`
- `Core/Save/SaveManager.cs`: Uses `SaveDataManager.SaveGame(int slot, ...)` and `SaveDataManager.LoadGame(int slot)`
- `GameManager.cs`: Calls `SaveManager.LoadGame(saveName)` — expecting string overload.

Need to reconcile these. The Systems/SaveManager.cs has `SaveManager` (static class) with `SaveGame(int, GameSaveData)` and `LoadGame(int)`. GameManager calls `SaveManager.LoadGame(saveName)` with a string arg, not int.

**Fix GameManager.cs** to pass an int:
```csharp
var data = SaveManager.LoadGame(0); // or parse slot from name
```

### 3.7 `Systems/SaveManager.cs` — Fix SaveMeta vs SaveInfo mismatch

The `Systems/SaveManager.cs` returns `List<SaveInfo>` from `GetSaveFiles()` but adds `data.Meta` (which is `SaveMeta`, not `SaveInfo`).

`SaveInfo` (in `Core/SaveInfo.cs`) is a struct with fields. `SaveMeta` (in `Core/Save/SaveManager.cs`) is a class with properties.

**Fix:** Either change `SaveManager.GetSaveFiles()` to return `SaveMeta[]`, or create a conversion:
```csharp
public static SaveMeta[] GetSaves() => SaveDataManager.ListSaves();
```

### 3.8 `Content/Performance/PerformanceManager.cs` — Fix RenderingServer API

Godot 4.3 `RenderingServer.GetRenderInfo` uses different API. Fix the call on line 263:
```csharp
// Change:
RenderingServer.GetRenderInfo(RenderingServer.RenderInfoType.Total, RenderingServer.RenderInfoKey.PrimitivesInFrame)
// In Godot 4.3, this is:
RenderingServer.GetRenderInfo(RenderingServer.RenderInfoTypeEnum.VisiblePrimitiveCount)
// Or simply remove the detailed stats and use:
GD.Print($"  Draw Calls: {Performance.GetMonitor(Performance.Monitor.RenderDrawCallsInFrame)}");
```

### 3.9 `Content/Certification/ConsoleCertification.cs` — Fix TrophySystem usage (~1 error)

```csharp
// Line 239: TrophySystem is a class, not a variable
// Change:
private TrophySystem TrophySystem { get; } = new();
```

Add an instance property or change the check:
```csharp
// Replace line 239:
"TRC-003" => new TrophySystem() != null
// With:
"TRC-003" => true
```

### 3.10 `Content/Certification/ConsoleCertification.cs` — Fix NotificationType (~1 error)

```csharp
// Line 321: The namespace for NotificationType may not be accessible
// Add: using TheSignal.Systems;  (NotificationType is in UIManager.cs)
```

Or since `NotificationType` is in `TheSignal.Systems` namespace and ConsoleCertification doesn't import it, add:
```csharp
using TheSignal.Systems;
```

But `NotificationType` is defined at file scope in `Systems/UIManager.cs`, not in a namespace block. So it's in `TheSignal.Systems` namespace. Add that using.

### 3.11 `Platform/EOSIntegration.cs` — Fix Action<string>.Invoke (~1 error)

Line: `action?.Invoke()` where `Action<string>` requires a string arg:
```csharp
// Change from:
onComplete?.Invoke();
// To:
onComplete?.Invoke("result");
```
But need to see the actual file to know the right value. This needs context.

### 3.12 `Systems/CoopManager.cs` — Fix out/ref property usage (~2 errors)

Properties can't be `out` or `ref`. Lines like:
```csharp
_peer.CreateServer(port, MAX_PLAYERS);
```
The error is `CS0206` — this means a property is used as ref/out. Need to check line numbers. Likely:
```csharp
// Line 75, 106:
var error = _peer.CreateServer(port, MAX_PLAYERS);
```
`ENetMultiplayerPeer.CreateServer` returns `Error` — this should work. The CS0206 error might be on a different pattern. Without the actual line numbers it's hard.

Looking at the error description: "A non ref-returning property or indexer may not be used as an out or ref value". This might be an issue where a property is used with `out`. Need to check the actual code.

## Execution Order

The Bug-Fixer agents should process in this exact order due to dependencies:

### Phase 1: Stub Updates (5 agents in parallel)
1. **Agent A:** `Core/SaveSlotData.cs` + `Core/Party.cs` — Add NG+ fields, fix return types
2. **Agent B:** `Core/Progression/Player.cs` — Add methods, fix accessibility  
3. **Agent C:** `Combat/Units/UnitInstance.cs` — Add `Stats`, overloads  
4. **Agent D:** `Data/CombatEncounter.cs` + `Data/CompanionResource.cs` — Add alias properties  
5. **Agent E:** `Systems/SectorMapManager.cs` — Add `Initialize()`, events, `ZoneState`/`ZoneStateData` fields

### Phase 2: Calling Code Patches (5 agents in parallel)
1. **Agent F:** `Systems/CombatManager.cs` — Fix Grid/BeginTurn/AdvanceTurn  
2. **Agent G:** `Systems/QuestBoardManager.cs` — Fix tuples/QuestType  
3. **Agent H:** `Combat/Abilities/AbilityExecutor.cs` + `Systems/QuestManager.cs` — Fix event/variable/expression  
4. **Agent I:** `Scenes/Exploration/TutorialZone.cs` + `Scenes/Player/PlayerController.cs` — Fix signatures  
5. **Agent J:** `Scenes/UI/TacticalHUD.cs` + `Content/UI/UIPolish.cs` — Using directives, API fixes

### Phase 3: Final Type Additions (sequential, some overlap)
1. **Agent K:** `Combat/Grid/TurnQueue.cs` + `Combat/Grid/CombatGrid.cs` — Add methods  
2. **Agent L:** `Core/Progression/ProgressionFormulas.cs` — Add GetDerivedStats  
3. **Agent M:** Remaining one-off fixes in `Systems/FactionWarManager.cs`, `Systems/GameManager.cs`, `Systems/SaveManager.cs`, etc.

## Verification

After each phase, run:
```bash
cd "G:/games_i_created/TheSignal"
dotnet build 2>&1 | grep "error CS" | wc -l
```

Track total error count decreasing from ~189 toward 0.
