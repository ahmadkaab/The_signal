# THE SIGNAL

Post-apocalyptic sci-fantasy tactical RPG. Godot 4.3 + C#.

## Project Structure

```
TheSignal/
├── Core/                       # Engine-agnostic systems
│   ├── Enums.cs               # All game enums
│   ├── Stats/                 # Stat system (base + derived)
│   │   ├── StatSystem.cs      # StatBlock, modifiers
│   │   └── DerivedStats.cs    # Combat stat calculation
│   ├── Progression/           # XP, leveling, player/party
│   │   ├── ProgressionFormulas.cs
│   │   ├── Player.cs
│   │   └── Party.cs
│   └── Save/                  # Serialization, versioning
│       └── SaveManager.cs
│
├── Data/                       # Godot Resources (.tres) - ALL CONTENT
│   ├── Items/                 # Weapons, armor, chips, consumables
│   ├── Enemies/               # Enemy archetypes, loot tables
│   ├── Abilities/             # Active/passive abilities, effects
│   ├── Quests/                # Quest definitions, stages, objectives
│   ├── Companions/            # Companion data, synergy trees
│   ├── Dialogue/              # Yarn Spinner (.yarn) files
│   ├── Zones/                 # Sector map nodes, local zones
│   ├── Factions/              # Reputation, vendors, relations
│   └── Progression/           # Signal nodes, mutations, formulas
│
├── Systems/                    # Autoload singletons
│   ├── GameManager.cs         # State machine, new/load game
│   ├── SaveManager.cs         # Atomic saves, migration
│   ├── ResourceRegistry.cs    # Loads all .tres resources
│   ├── QuestManager.cs        # Quest state machine
│   ├── CombatManager.cs       # Tactical combat engine
│   ├── WorldManager.cs        # Sector map, zones, flags
│   ├── DialogueManager.cs     # Yarn Spinner integration
│   ├── AudioManager.cs
│   ├── InputManager.cs
│   └── UIManager.cs
│
├── Combat/                     # Combat scene & components
│   ├── Grid/                  # Tilemap, cover, LoS
│   ├── Units/                 # PlayerUnit, EnemyUnit, Deployable
│   ├── Abilities/             # Effect resolution, targeting
│   └── AI/                    # Behavior trees
│
├── Exploration/               # Sector map & local zones
│   ├── SectorMap/             # Node graph, travel, events
│   └── LocalZone/             # 3D exploration, NPCs
│
├── Scenes/                    # Godot scenes (.tscn)
│   ├── Combat/                # TacticalCombat.tscn
│   ├── Exploration/           # SectorMap.tscn, LocalZone.tscn
│   ├── UI/                    # MainMenu, TacticalHUD, Inventory, etc.
│   └── Menus/                 # Pause, Settings, CharacterSheet
│
├── Assets/                    # Art, audio, fonts, shaders
│   ├── Art/
│   ├── Audio/
│   ├── Fonts/
│   └── Shaders/
│
├── Content/Authoring/         # Editor plugins (custom inspectors)
└── Addons/                    # YarnSpinner-Godot, etc.
```

## Core Systems

### Stat System (`Core/Stats/`)
- **6 Core Stats**: Might, Agility, Constitution, Intelligence, Willpower, Resonance
- **Derived Stats**: Computed from base + modifiers (never stored)
- **Modifier Stack**: Flat + Percent, priority-ordered, push/pop for buffs/gear

### Progression (`Core/Progression/`)
- **XP Curve**: Quadratic (100 * level² * 1.2)
- **Signal Points**: 1/level → unlock Signal Nodes (5 branches × 8 nodes)
- **Resonance Fragments**: Zone cleansing, bosses → equip Mutations (3 slots, corruption cost)
- **Companion Synergy**: 5 loyalty ranks → dual ultimate abilities

### Save System (`Core/Save/`)
- **Atomic writes**: temp file → rename (POSIX) + `.bak` backup (Windows)
- **Version migration**: Chain of pure functions v1→v2→v3...
- **Slots + Autosave**: Separate files, never clobber manual saves

### Combat (`Systems/CombatManager.cs`)
- **AP System**: 6 base AP, abilities 2-5 AP, Momentum (kill → +1 AP)
- **Cover**: Half (+30% Def) / Full (+60% Def, blocks LoS)
- **Damage**: `(Base + Weapon*0.5 + Stat*Scaling) * (1±10%) * Crit(1.5x) * (1-DR)`
- **Status Effects**: 25+ types with stack rules, durations, triggers

### Narrative (`Systems/DialogueManager.cs`)
- **Yarn Spinner**: Visual node editor, variables, commands, localization
- **Variable Bridge**: Game state ↔ Yarn variables (`$gold`, `$met_kael`, etc.)
- **Choices gate on conditions**: `<<if $resonance >= 10>>`

## Quick Start

### Prerequisites
- Godot 4.3+ (standard, not .NET)
- .NET SDK 8.0
- GodotSharp extension for VS Code / Rider

### Build & Run
```bash
cd TheSignal
dotnet build
# Open in Godot Editor → Project → Run
```

### First Run
1. Launch → Main Menu
2. **New Game** → Prologue (cryo pod awakening)
3. Choose first companion: Kael (Purified) / Mara (Rooted) / Both / Neither
4. Tutorial combat → Waystation/Grove unlocked
5. Act I begins: Sector Map opens

## Content Authoring

### Creating Items/Enemies/Quests
1. Right-click in FileSystem → **New Resource** → Select type (ItemResource, EnemyResource, etc.)
2. Save as `.tres` in appropriate `Data/` subfolder
3. ResourceRegistry auto-loads on startup

### Signal Nodes & Mutations
```
Data/Progression/SignalNodes/
  Resonance/RES_WAVE.tres
  Biology/BIO_ADAPT.tres
  ...
Data/Progression/Mutations/
  Offense/MUT_OVERLOAD.tres
  Defense/MUT_CARAPACE.tres
  ...
```

### Writing Dialogue
1. Open `.yarn` file in Yarn Spinner editor (or VS Code extension)
2. Write nodes with `=== node_name ===`
3. Use `<<set $var = value>>`, `<<if $condition>>`, `-> choice_target`
4. Line IDs = localization keys (e.g., `DLG_KAEL_001`)

## Development Phases

| Phase | Weeks | Deliverable |
|-------|-------|-------------|
| **0: Foundation** | 1-4 | Project setup, stat/save systems, CI |
| **1: Combat Core** | 5-12 | Grid, AP, abilities, AI, 3v3 playable |
| **2: Narrative** | 13-22 | Yarn integration, quests, companions |
| **3: Progression** | 23-30 | Skill trees, mutations, synergy, crafting |
| **4: Content** | 31-52+ | Full Act I, art, audio, polish |
| **5: Ship** | 8-12 | Optimization, localization, cert |

## Key Design Principles

1. **Data-Driven**: Zero hardcoded content. Everything is a Resource.
2. **Modifier Stack**: Never edit base stats directly. Push/pop modifiers.
3. **Versioned Saves**: Migration chain from day one.
4. **Single Source of Truth**: Derived stats always recomputed.
5. **Choices Matter**: Flags gate quests, change endings, alter companions.

## License

Apache-2.0 — See LICENSE file.
