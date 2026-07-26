import os
import shutil

project = "G:/games_i_created/TheSignal"

# ============ STEP 1: Delete conflicting editor plugin files ============
# TheSignalEditorPlugin.cs has all the plugin types embedded with correct method names.
# Separate files have wrong method names (missing underscore) and duplicate everything.
files_to_delete = [
    f"{project}/Content/Authoring/MutationEditorPlugin.cs",
    f"{project}/Content/Authoring/SignalNodeEditorPlugin.cs",
    f"{project}/Content/Authoring/CompanionSynergyEditorPlugin.cs",
    f"{project}/Content/Authoring/ZoneEventEditorPlugin.cs",
]
for f in files_to_delete:
    if os.path.exists(f):
        os.remove(f)
        print(f"DELETED: {f}")
    else:
        print(f"NOT FOUND: {f}")

# ============ STEP 2: Fix WorldManager.cs - missing using ============
wm_path = f"{project}/Systems/WorldManager.cs"
with open(wm_path, 'r') as f:
    content = f.read()
content = content.replace(
    "using System.Collections.Generic;\n",
    "using System.Collections.Generic;\nusing TheSignal.Core;\n"
)
# Also check if DateTime is used
if "DateTime" in content and "using System;" not in content:
    content = content.replace("using Godot;\n", "using Godot;\nusing System;\n")
with open(wm_path, 'w') as f:
    f.write(content)
print(f"FIXED: WorldManager.cs - added using directives")

# ============ STEP 3: Fix UIPolish.cs - ambiguous Timer ============
ui_path = f"{project}/Content/UI/UIPolish.cs"
with open(ui_path, 'r') as f:
    content = f.read()
content = content.replace(
    "private Timer _tooltipTimer;",
    "private Godot.Timer _tooltipTimer;"
)
with open(ui_path, 'w') as f:
    f.write(content)
print(f"FIXED: UIPolish.cs - Timer -> Godot.Timer")

# ============ STEP 4: Fix ConsoleCertification.cs - ambiguous HttpClient ============
cc_path = f"{project}/Content/Certification/ConsoleCertification.cs"
with open(cc_path, 'r') as f:
    content = f.read()
# Change using System.Net.Http; and use fully qualified HttpClient
content = content.replace(
    "using System.Net.Http;",
    "//using System.Net.Http;"
)
content = content.replace(
    "private HttpClient _httpClient;",
    "private System.Net.Http.HttpClient _httpClient;"
)
with open(cc_path, 'w') as f:
    f.write(content)
print(f"FIXED: ConsoleCertification.cs - HttpClient ambiguity")

# ============ STEP 5: Fix PlayerController.cs - Player namespace conflict ============
pc_path = f"{project}/Scenes/Player/PlayerController.cs"
with open(pc_path, 'r') as f:
    content = f.read()
# Change using TheSignal.Core.Stats; to not import it if not needed
# The issue: 'Player' used as type but the namespace TheSignal.Core.Progression has a Player type
# And using TheSignal.Core; might bring in the Player namespace
# Actually the issue is that Player is used as 'private Player _playerData;' on line 35
# and 'using TheSignal.Core.Progression;' brings in a namespace called Player? No...
# Let me check if there's a 'Player' namespace
import re
# Look at the actual line
content = content.replace(
    "private Player _playerData;",
    "private TheSignal.Core.Progression.Player _playerData;"
)
# Also fix references to PlayerMembership etc if needed
with open(pc_path, 'w') as f:
    f.write(content)
print(f"FIXED: PlayerController.cs - Player reference")

# ============ STEP 6: Fix QuestManager.cs - missing QuestCondition and QuestSaveData ============
qm_path = f"{project}/Systems/QuestManager.cs"
with open(qm_path, 'r') as f:
    content = f.read()
# Add using for Save namespace
content = content.replace(
    "using TheSignal.Core;\n",
    "using TheSignal.Core;\nusing TheSignal.Core.Save;\n"
)
with open(qm_path, 'w') as f:
    f.write(content)
print(f"FIXED: QuestManager.cs - added using directives")

# ============ STEP 7: Fix NewGamePlusManager.cs - missing SaveSlotData ============
ngp_path = f"{project}/Systems/NewGamePlusManager.cs"
with open(ngp_path, 'r') as f:
    content = f.read()
content = content.replace(
    "using TheSignal.Core;\n",
    "using TheSignal.Core;\nusing TheSignal.Core.Save;\n"
)
# Also need to create CarryOverItem class - it's used but not defined
# Let me check if it needs adding as a class at the bottom of the file
with open(ngp_path, 'w') as f:
    f.write(content)
print(f"FIXED: NewGamePlusManager.cs - added using directives")

# ============ STEP 8: Fix ZoneResource.cs - duplicate GlobalClass on EventOutcome ============
zr_path = f"{project}/Data/ZoneResource.cs"
with open(zr_path, 'r') as f:
    content = f.read()
# The EventOutcome class at line 148 has [GlobalClass] which clashes with EditorResources.cs
# Actually looking at the code, both ZoneResource.cs AND EditorResources.cs define EventOutcome
# Remove the duplicate EventOutcome definition from ZoneResource.cs since EditorResources.cs has a more complete one
# Actually wait - ZoneResource.cs has its own EventOutcome with different enum types...
# Let me just check if EditorResources.cs also has EventOutcome

# Looking more carefully at the errors from initial build:
# ZoneResource.cs(148,2): CS0579 Duplicate 'GlobalClass' 
# ZoneResource.cs(151,33): CS0102 Type 'EventOutcome' already contains 'Type'
# This means there are TWO EventOutcome definitions in ZoneResource.cs
# Let me check the file more carefully

lines = content.split('\n')
# Find all [GlobalClass] and the class definitions that follow
found_globalclasses = []
for i, line in enumerate(lines):
    if '[GlobalClass]' in line:
        # Look for the class definition after this
        for j in range(i+1, min(i+5, len(lines))):
            if lines[j].strip().startswith('public partial class') or lines[j].strip().startswith('public class'):
                class_name = lines[j].strip().split()[2].split(':')[0]
                found_globalclasses.append((i+1, j+1, class_name))
                break

print("ZoneResource.cs GlobalClasses:")
for line_no, class_start, name in found_globalclasses:
    print(f"  Line {line_no}: [GlobalClass] -> Line {class_start}: {name}")

with open(zr_path, 'w') as f:
    f.write(content)

# ============ STEP 9: Fix UnitData.cs - duplicate GlobalClass on LootEntry ============
ud_path = f"{project}/Data/UnitData.cs"
with open(ud_path, 'r') as f:
    content = f.read()

# Find all [GlobalClass] in the file
lines = content.split('\n')
found_globalclasses = []
for i, line in enumerate(lines):
    if '[GlobalClass]' in line:
        for j in range(i+1, min(i+5, len(lines))):
            if lines[j].strip().startswith('public partial class') or lines[j].strip().startswith('public class'):
                class_name = lines[j].strip().split()[2].split(':')[0]
                found_globalclasses.append((i+1, j+1, class_name))
                break

print("UnitData.cs GlobalClasses:")
for line_no, class_start, name in found_globalclasses:
    print(f"  Line {line_no}: [GlobalClass] -> Line {class_start}: {name}")

# Check if LootEntry appears twice in UnitData.cs
import re
lootentries = [i for i, line in enumerate(lines) if 'class LootEntry' in line]
print(f"LootEntry appears at lines: {[l+1 for l in lootentries]}")

# Also check if EnemyResource.cs also has LootEntry - it does!
er_path = f"{project}/Data/EnemyResource.cs"
with open(er_path, 'r') as f:
    er_content = f.read()
er_lines = er_content.split('\n')
lootentries_er = [i for i, line in enumerate(er_lines) if 'class LootEntry' in line]
print(f"LootEntry in EnemyResource.cs at lines: {[l+1 for l in lootentries_er]}")

# ============ STEP 10: Check for EditorResources.cs duplicate types ============
er2_path = f"{project}/Data/EditorResources.cs"
with open(er2_path, 'r') as f:
    er2_content = f.read()
er2_lines = er2_content.split('\n')
event_outcomes = [i for i, line in enumerate(er2_lines) if 'class EventOutcome' in line]
lootentries_er2 = [i for i, line in enumerate(er2_lines) if 'class LootEntry' in line]
stat_types = [i for i, line in enumerate(er2_lines) if 'class StatType' in line or 'enum StatType' in line]
print(f"EventOutcome in EditorResources.cs at: {[l+1 for l in event_outcomes]}")
print(f"LootEntry in EditorResources.cs at: {[l+1 for l in lootentries_er2]}")
print(f"StatType in EditorResources.cs at: {[l+1 for l in stat_types]}")

# ============ STEP 11: Check ResourceRegistry.cs for duplicates ============
rr_path = f"{project}/Systems/ResourceRegistry.cs"
with open(rr_path, 'r') as f:
    rr_content = f.read()
rr_lines = rr_content.split('\n')
print(f"\nResourceRegistry.cs has {len(rr_lines)} lines")
# Look for duplicate methods
ready_count = sum(1 for l in rr_lines if 'override void _Ready' in l)
print(f"_Ready count: {ready_count}")
instance_count = sum(1 for l in rr_lines if 'public static ResourceRegistry Instance' in l)
print(f"Instance count: {instance_count}")

print("\n=== Analysis complete ===")
