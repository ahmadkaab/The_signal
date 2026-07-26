using Godot;
using TheSignal.Data;

namespace TheSignal.Systems;

public class EnemySpawnInfo
{
    public UnitData UnitData { get; set; }
    public Vector2I Position { get; set; }
    public int SpawnDelay { get; set; }
}
