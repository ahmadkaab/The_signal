using Godot;
using TheSignal.Data;

namespace TheSignal.Combat.Units;

public partial class CompanionInstance : UnitInstance
{
    public string CompanionId { get; set; }
    public int LoyaltyLevel { get; set; } = 1;
    
    public bool HasComboWith(UnitInstance other, AbilityResource ability)
    {
        return false; // Stub - real logic needs companion synergy data
    }
    
    public AbilityResource GetComboAbility(UnitInstance other, AbilityResource ability)
    {
        return null; // Stub
    }
}
