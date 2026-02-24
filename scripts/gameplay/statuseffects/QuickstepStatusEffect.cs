using Godot;

/// <summary>
/// Consumed on next initiated action to reduce action TickCost (higher priority).
/// </summary>
[GlobalClass]
public partial class QuickstepStatusEffect : StatusEffect
{
    [Export]
    public float TickCostReduction { get; private set; } = 400f;

    public override void OnActionInitiated(ActionContext context, Node owner)
    {
        if (!StatusRuleUtils.IsOwnerTurnContext(context, owner)) return;

        context.TickCostAdjustment -= Mathf.Max(0f, TickCostReduction);
        owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName)?.RemoveEffect(this, null);
    }
}
