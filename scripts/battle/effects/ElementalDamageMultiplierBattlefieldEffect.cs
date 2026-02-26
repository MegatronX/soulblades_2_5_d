using Godot;
using System.Collections.Generic;

/// <summary>
/// Generic battlefield modifier that scales action damage by element composition.
/// Useful for weather/terrain effects (e.g. Rain boosts Water/Lightning, weakens Fire).
/// </summary>
[GlobalClass]
public partial class ElementalDamageMultiplierBattlefieldEffect : BattlefieldEffect
{
    [Export]
    public bool RequireDamagingAction { get; private set; } = true;

    [Export]
    public Godot.Collections.Dictionary<ElementType, float> ElementMultipliers { get; private set; } = new();

    public override void OnActionBroadcast(ActionContext context, Node owner)
    {
        if (context == null) return;
        if (RequireDamagingAction && !StatusRuleUtils.IsDamagingAction(context)) return;

        var damage = context.GetComponent<DamageComponent>();
        if (damage == null || damage.Power <= 0) return;
        if (ElementMultipliers == null || ElementMultipliers.Count == 0) return;

        float totalMultiplier = ResolveElementalMultiplier(damage.ElementalWeights, ElementMultipliers);
        if (Mathf.IsEqualApprox(totalMultiplier, 1.0f)) return;

        damage.Power = Mathf.Max(0, Mathf.RoundToInt(damage.Power * totalMultiplier));
        context.ModificationLog.Add($"{EffectName}: x{totalMultiplier:0.00}");
    }

    private static float ResolveElementalMultiplier(
        Dictionary<ElementType, float> weights,
        Godot.Collections.Dictionary<ElementType, float> multipliers)
    {
        if (weights == null || weights.Count == 0)
        {
            return 1.0f;
        }

        float weightedTotal = 0f;
        float totalWeight = 0f;

        foreach (var kvp in weights)
        {
            float weight = kvp.Value;
            if (weight <= 0f) continue;

            float multiplier = 1.0f;
            if (multipliers != null && multipliers.TryGetValue(kvp.Key, out var mapped))
            {
                multiplier = mapped;
            }

            weightedTotal += weight * multiplier;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            return 1.0f;
        }

        if (totalWeight < 1.0f)
        {
            weightedTotal += (1.0f - totalWeight);
        }

        return weightedTotal;
    }
}
