using Godot;

/// <summary>
/// Temporary shield that absorbs incoming HP damage before it sticks.
/// </summary>
[GlobalClass]
public partial class BarrierStatusEffect : StackingStatusEffect
{
    [ExportGroup("Barrier")]
    [Export]
    public int BarrierOnApply { get; private set; } = 500;

    [Export]
    public int BarrierOnReapply { get; private set; } = 500;

    [Export]
    public int BarrierCap { get; private set; } = 999999;

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (manager == null) return;

        int amount = Mathf.Clamp(BarrierOnApply, 0, BarrierCap <= 0 ? int.MaxValue : BarrierCap);
        manager.SetState(this, "barrier_value", amount);
    }

    public override bool OnReapply(StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance, ActionDirector actionDirector, IRandomNumberGenerator rng = null)
    {
        bool changed = base.OnReapply(manager, instance, actionDirector, rng);
        if (manager == null) return changed;

        int current = GetBarrierValue(manager);
        int cap = BarrierCap <= 0 ? int.MaxValue : BarrierCap;
        int next = Mathf.Clamp(current + Mathf.Max(0, BarrierOnReapply), 0, cap);
        changed = changed || next != current;
        manager.SetState(this, "barrier_value", next);
        return changed;
    }

    public override void OnActionPostExecution(ActionContext context, Node owner, ActionResult result)
    {
        if (context == null || owner == null || result == null) return;
        if (!StatusRuleUtils.IsOwnerTargetContext(context, owner)) return;
        if (!result.IsHit || result.FinalDamage <= 0) return;

        var manager = owner.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        var stats = owner.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (manager == null || stats == null) return;

        int barrier = GetBarrierValue(manager);
        if (barrier <= 0)
        {
            manager.RemoveEffect(this, null);
            return;
        }

        int absorbed = Mathf.Min(barrier, result.FinalDamage);
        if (absorbed <= 0) return;

        stats.ModifyCurrentHP(absorbed);
        barrier -= absorbed;
        manager.SetState(this, "barrier_value", barrier);
        if (barrier <= 0)
        {
            manager.RemoveEffect(this, null);
        }
    }

    private int GetBarrierValue(StatusEffectManager manager)
    {
        if (manager == null) return 0;
        if (!manager.TryGetState(this, "barrier_value", out var value)) return 0;
        return Mathf.Max(0, value.AsInt32());
    }
}
