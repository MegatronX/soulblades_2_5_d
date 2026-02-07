using Godot;

/// <summary>
/// Defines the logic for determining if an action hits its target.
/// </summary>
[GlobalClass]
public abstract partial class HitStrategy : Resource
{
    public abstract bool Calculate(ActionContext context, Node target, IRandomNumberGenerator rng);
}
