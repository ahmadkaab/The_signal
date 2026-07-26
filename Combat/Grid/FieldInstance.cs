using Godot;

namespace TheSignal.Combat.Grid
{
    public partial class FieldInstance : RefCounted
    {
        public string FieldEffectId;
        public Vector2I Position;
        public string OwnerId;
        public int RemainingTurns;
        public bool IsHostile;
        public Godot.Collections.Dictionary Properties = new();
    }
}
