using Godot;

/// <summary>
/// A composite strategy that delegates calculation to specific sub-strategies.
/// Assign this to ActionData to define how the action behaves mechanically.
/// </summary>
[GlobalClass]
public partial class CalculationStrategy : Resource
{
    [Export] public HitStrategy HitLogic { get; private set; }
    [Export] public CritStrategy CritLogic { get; private set; }
    [Export] public DamageStrategy DamageLogic { get; private set; }

    public bool CalculateHit(ActionContext context, Node target, IRandomNumberGenerator rng)
    {
        // Default to true (hit) if no logic is assigned.
        return HitLogic?.Calculate(context, target, rng) ?? true;
    }

    public bool CalculateCrit(ActionContext context, Node target, IRandomNumberGenerator rng)
    {
        // Default to false (no crit) if no logic is assigned.
        return CritLogic?.Calculate(context, target, rng) ?? false;
    }

    public int CalculateDamage(ActionContext context, Node target, ActionResult result, IRandomNumberGenerator rng)
    {
        // Default to 0 damage if no logic is assigned.
        return DamageLogic?.Calculate(context, target, result, rng) ?? 0;
    }
}
