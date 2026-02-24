using Godot;

/// <summary>
/// Next spell repeats at reduced potency and consumes this status.
/// </summary>
[GlobalClass]
public partial class EchoCastStatusEffect : StatusEffect
{
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float RepeatPotencyScalar { get; private set; } = 0.5f;

    public override void OnActionInitiated(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTurnContext(context, owner)) return;
        if (!StatusRuleUtils.IsSpellLike(context.SourceAction)) return;
        if (context.RuntimeEvents.Contains("EchoCastRepeat")) return;

        var repeat = new ActionContext(context.SourceAction, owner, context.InitialTargets, context.SourceItem)
        {
            ActionPowerScalar = Mathf.Clamp(RepeatPotencyScalar, 0f, 1f),
            SkipActionCosts = true
        };
        repeat.RuntimeEvents.Add("EchoCastRepeat");

        context.PendingReactions.Add(repeat);
        owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName)?.RemoveEffect(this, null);
    }
}
