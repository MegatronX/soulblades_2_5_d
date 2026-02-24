using Godot;

/// <summary>
/// Float currently reduces Earth-element damage and can be queried by hazard systems.
/// </summary>
[GlobalClass]
public partial class FloatStatusEffect : StatusEffect
{
    [Export(PropertyHint.Range, "0,2,0.01")]
    public float EarthDamageTakenMultiplier { get; private set; } = 0.67f;

    [Export]
    public bool IgnoreGroundHazardTicks { get; private set; } = true;

    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTargetContext(context, owner)) return;
        if (!StatusRuleUtils.IsDamagingAction(context)) return;
        if (context.SourceAction == null) return;
        if (EarthDamageTakenMultiplier == 1.0f) return;

        var damage = context.GetComponent<DamageComponent>();
        if (damage == null || damage.ElementalWeights == null || damage.ElementalWeights.Count == 0) return;

        if (!damage.ElementalWeights.TryGetValue(ElementType.Earth, out float earthWeight)) return;
        if (earthWeight <= 0f) return;

        damage.Power = Mathf.Max(0, Mathf.RoundToInt(damage.Power * EarthDamageTakenMultiplier));
    }
}
