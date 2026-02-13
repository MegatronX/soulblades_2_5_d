using Godot;

[GlobalClass]
public partial class AutoStatusAbilityEffect : AbilityEffect
{
    [Export] public StatusEffect StatusEffect { get; private set; }
    [Export] public bool IgnoreResistances { get; private set; } = false;

    public override void Apply(AbilityEffectContext context)
    {
        if (context == null || context.Owner == null || StatusEffect == null) return;
        if (!Matches(context.Trigger)) return;

        var statusManager = context.Owner.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (statusManager == null) return;

        bool applied = false;
        if (IgnoreResistances)
        {
            applied = statusManager.ApplyEffect(StatusEffect, context.ActionDirector);
        }
        else
        {
            applied = statusManager.TryApplyEffect(StatusEffect, context.ActionDirector, 100f, null);
        }

        if (applied)
        {
            context.WasTriggered = true;
        }
    }
}
