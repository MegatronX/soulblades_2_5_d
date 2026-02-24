using Godot;

/// <summary>
/// Reflect variant that consumes charges each time a targeted spell is reflected.
/// </summary>
[GlobalClass]
public partial class ReflectChargesStatusEffect : ReflectStatusEffect
{
    [Export]
    public int InitialCharges { get; private set; } = 1;

    [Export]
    public int MaxCharges { get; private set; } = 3;

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (manager == null) return;
        int charges = Mathf.Clamp(InitialCharges, 1, Mathf.Max(1, MaxCharges));
        manager.SetState(this, "reflect_charges", charges);
    }

    public override void OnActionTargeted(ActionContext context, Node owner)
    {
        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (manager == null) return;

        int charges = GetCharges(manager);
        if (charges <= 0)
        {
            manager.RemoveEffect(this, null);
            return;
        }

        int beforeTargetId = context?.CurrentTarget?.GetInstanceId().GetHashCode() ?? 0;
        base.OnActionTargeted(context, owner);
        int afterTargetId = context?.CurrentTarget?.GetInstanceId().GetHashCode() ?? 0;

        // Treat target change as reflect trigger.
        if (beforeTargetId == afterTargetId) return;

        charges = Mathf.Max(0, charges - 1);
        manager.SetState(this, "reflect_charges", charges);
        if (charges <= 0)
        {
            manager.RemoveEffect(this, null);
        }
    }

    private int GetCharges(StatusEffectManager manager)
    {
        if (!manager.TryGetState(this, "reflect_charges", out var value))
        {
            return 0;
        }
        return Mathf.Max(0, value.AsInt32());
    }
}
