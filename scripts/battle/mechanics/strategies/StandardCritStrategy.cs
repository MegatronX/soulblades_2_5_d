using Godot;

[GlobalClass]
public partial class StandardCritStrategy : CritStrategy
{
    public override bool Calculate(ActionContext context, Node target, IRandomNumberGenerator rng)
    {
        return rng.RandRangeFloat(0, 100) < context.SourceAction.CritChance;
    }
}
