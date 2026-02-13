using Godot;

[GlobalClass]
public partial class ChemistAbilityEffect : AbilityEffect
{
    [Export(PropertyHint.Range, "1.0,5.0,0.1")]
    public float HealingMultiplier { get; private set; } = 2.0f;

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float NoConsumeChancePercent { get; private set; } = 25f;

    [Export]
    public bool OnlyHealingItems { get; private set; } = true;

    public override void Apply(AbilityEffectContext context)
    {
        if (context == null) return;
        if (!Matches(context.Trigger)) return;

        switch (context.Trigger)
        {
            case AbilityTrigger.DamageCalculated:
                ApplyHealingMultiplier(context);
                break;
            case AbilityTrigger.ItemConsume:
                ApplyConsumeReduction(context);
                break;
        }
    }

    private void ApplyHealingMultiplier(AbilityEffectContext context)
    {
        if (!IsHealingItemContext(context)) return;

        if (context.ActionResult == null) return;
        if (!context.ActionResult.IsHeal) return;

        if (HealingMultiplier <= 1f) return;

        context.ActionResult.FinalDamage = Mathf.RoundToInt(context.ActionResult.FinalDamage * HealingMultiplier);
        context.WasTriggered = true;
    }

    private void ApplyConsumeReduction(AbilityEffectContext context)
    {
        if (NoConsumeChancePercent <= 0f) return;
        if (!IsHealingItemContext(context)) return;

        float reduction = Mathf.Clamp(NoConsumeChancePercent, 0f, 100f) / 100f;
        context.Multiplier *= Mathf.Clamp(1f - reduction, 0f, 1f);
        context.WasTriggered = true;
    }

    private bool IsHealingItemContext(AbilityEffectContext context)
    {
        if (context?.ActionContext == null) return false;
        if (context.ActionContext.SourceItem == null) return false;

        if (!OnlyHealingItems) return true;

        if (context.ActionResult != null && context.ActionResult.IsHeal) return true;

        var category = context.ActionContext.SourceAction?.Category ?? ActionCategory.None;
        return category == ActionCategory.Heal;
    }
}
