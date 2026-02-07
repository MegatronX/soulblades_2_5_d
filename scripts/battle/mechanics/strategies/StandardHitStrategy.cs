using Godot;

[GlobalClass]
public partial class StandardHitStrategy : HitStrategy
{
    public override bool Calculate(ActionContext context, Node target, IRandomNumberGenerator rng)
    {
        var damageComp = context.GetComponent<DamageComponent>();
        // If there's no damage component (e.g. a buff), it usually "hits" automatically.
        if (damageComp == null) return true;
        
        // Simple roll against accuracy
        return rng.RandRangeFloat(0, 100) < damageComp.Accuracy;
    }
}
