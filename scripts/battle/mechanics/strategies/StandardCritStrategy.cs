using Godot;

[GlobalClass]
public partial class StandardCritStrategy : CritStrategy
{
    public override bool Calculate(ActionContext context, Node target, IRandomNumberGenerator rng)
    {
        float bonus = context?.BonusCritChancePercent ?? 0f;
        float critChance = Mathf.Clamp((context?.SourceAction?.CritChance ?? 0) + bonus, 0f, 100f);
        return rng.RandRangeFloat(0, 100) < critChance;
    }
}
