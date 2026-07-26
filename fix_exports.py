#!/usr/bin/env python3
"""Remove [Export] from List<> and Dictionary<> properties in C# files."""

import os

files_to_fix = [
    "Data/AbilityResource.cs",
    "Data/CombatEncounter.cs",
    "Data/EnemyResource.cs",
    "Data/ItemResource.cs",
    "Data/UnitData.cs",
    "Data/ZoneResource.cs",
    "Combat/Units/UnitInstance.cs",
    "Systems/ResourceRegistry.cs",
]

for fname in files_to_fix:
    path = f"G:/games_i_created/TheSignal/{fname}"
    if not os.path.exists(path):
        print(f"MISSING: {path}")
        continue
    
    with open(path, 'r') as f:
        content = f.read()
    
    original = content
    
    # Remove [Export] when it's followed by public List< or public Dictionary<
    import re
    
    # Pattern: [Export] followed by whitespace then public List<...> or public Dictionary<..., ...>
    # This matches both same-line: [Export] public List<...> 
    # And multi-line: [Export]\n    public List<...>
    
    def fix_export(match):
        print(f"  Removing [Export] from: {match.group(0).strip()}")
        return match.group(2)  # Return just the property declaration without [Export]
    
    content = re.sub(
        r'\[Export\]\s*(public\s+(?:List|Dictionary)<[^>]+>\s+\w+\s*\{[^}]+\}\s*)',
        r'\1',
        content
    )
    
    # Also handle Dictionary<K,V> edge case with nested >>
    # The regex above won't catch Dictionary<FactionId, int> because of the nested <>
    # Let's do a simpler approach - just find and replace
    lines = content.split('\n')
    new_lines = []
    for line in lines:
        stripped = line.strip()
        # Check if line has [Export] AND (List< or Dictionary<) in the type
        if '[Export]' in stripped and (' List<' in stripped or ' Dictionary<' in stripped or stripped.count('List<') > 0 or stripped.count('Dictionary<') > 0):
            # Skip lines that have both [Export] and generic collection type
            # But only if the generic is the property type, not part of init
            # Check if after the = there's a List or Dictionary (don't remove those)
            # Simpler: check if List< or Dictionary< appears before the =
            before_equals = line.split('=')[0] if '=' in line else line
            if 'List<' in before_equals or 'Dictionary<' in before_equals:
                print(f"  SKIP (check): {stripped[:100]}")
                # Actually let's still include this
                new_line = line.replace('[Export] ', '').replace('[Export]', '')
                print(f"  Removing [Export] from: {stripped[:100]}")
                new_lines.append(new_line)
                continue
        new_lines.append(line)
    
    content = '\n'.join(new_lines)
    
    if content != original:
        with open(path, 'w') as f:
            f.write(content)
        print(f"  Fixed: {path}")
    else:
        print(f"  No changes: {path}")

print("\nDone!")
