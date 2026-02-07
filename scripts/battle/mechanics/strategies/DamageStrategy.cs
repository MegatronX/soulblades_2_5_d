using Godot;

/// <summary>
/// Defines the logic for calculating the raw damage or healing value.
/// </summary>
[GlobalClass]
public abstract partial class DamageStrategy : Resource
{
    public abstract int Calculate(ActionContext context, Node target, ActionResult result, IRandomNumberGenerator rng);
}
