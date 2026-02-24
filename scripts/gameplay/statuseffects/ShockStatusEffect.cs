using Godot;

/// <summary>
/// Shock slows the target and weakens the next MP restore received.
/// Reapplication rearms the MP rider.
/// </summary>
[GlobalClass]
public partial class ShockStatusEffect : StackingStatusEffect, IMpRestoreModifierStatusEffect
{
    [Export]
    public int MaxShockStacks { get; private set; } = 3;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BaseSpeedPenalty { get; private set; } = 0.20f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AdditionalSpeedPenaltyPerStack { get; private set; } = 0.05f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BaseMpRestorePenalty { get; private set; } = 0.30f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AdditionalMpRestorePenaltyPerStack { get; private set; } = 0.10f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float MaxMpRestorePenalty { get; private set; } = 0.50f;

    public override int ResolveMaxStacks(Node owner)
    {
        return Mathf.Max(1, MaxShockStacks);
    }

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        var manager = owner?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        var instance = manager?.GetEffectInstance(this);
        if (manager == null || instance == null) return;

        manager.SetState(this, "shock_rider_pending", true);
        UpdateSpeedModifier(owner, manager, instance);
    }

    public override bool OnReapply(StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance, ActionDirector actionDirector, IRandomNumberGenerator rng = null)
    {
        bool changed = base.OnReapply(manager, instance, actionDirector, rng);
        manager?.SetState(this, "shock_rider_pending", true);
        return changed;
    }

    public override void OnStacksChanged(Node owner, StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance, ActionDirector actionDirector)
    {
        UpdateSpeedModifier(owner, manager, instance);
    }

    public override void OnRemove(Node owner, ActionDirector actionDirector)
    {
        base.OnRemove(owner, actionDirector);
        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        stats?.RemoveAllModifiersFromSource(this);
    }

    public int ModifyIncomingMpRestore(Node owner, StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance, int incomingAmount)
    {
        if (manager == null || instance == null) return incomingAmount;
        if (!manager.TryGetState(this, "shock_rider_pending", out var pending) || !pending.AsBool())
        {
            return incomingAmount;
        }

        int stacks = Mathf.Max(1, instance.Stacks);
        float penalty = BaseMpRestorePenalty + ((stacks - 1) * AdditionalMpRestorePenaltyPerStack);
        penalty = Mathf.Clamp(penalty, 0f, MaxMpRestorePenalty);
        int modified = Mathf.Max(0, Mathf.RoundToInt(incomingAmount * (1.0f - penalty)));
        manager.SetState(this, "shock_rider_pending", false);
        return modified;
    }

    private void UpdateSpeedModifier(Node owner, StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance)
    {
        var stats = owner?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null || instance == null) return;

        stats.RemoveAllModifiersFromSource(this);

        int stacks = Mathf.Max(1, instance.Stacks);
        float totalPenalty = BaseSpeedPenalty + ((stacks - 1) * AdditionalSpeedPenaltyPerStack);
        float speedMultiplier = Mathf.Clamp(1.0f - totalPenalty, 0.1f, 1.0f);
        var modifier = new StatModifier(StatType.Speed, speedMultiplier, ModifierType.Multiplicative, this);
        stats.AddModifier(modifier);
    }
}
