using Godot;

/// <summary>
/// Defines the logic for determining if an action is a critical hit.
/// </summary>
[GlobalClass]
public abstract partial class CritStrategy : Resource
{
    public abstract bool Calculate(ActionContext context, Node target, IRandomNumberGenerator rng);
}
