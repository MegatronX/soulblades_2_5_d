using Godot;

/// <summary>
/// Base class for statuses that should maintain one active instance and track intensity via stacks.
/// </summary>
[GlobalClass]
public partial class StackingStatusEffect : StatusEffect
{
    [ExportGroup("Stacks")]
    [Export]
    public int MaxStacks { get; private set; } = 1;

    [Export]
    public int BossMaxStacks { get; private set; } = -1;

    [Export]
    public bool AddStackOnReapply { get; private set; } = true;

    [Export]
    public bool RefreshDurationOnReapply { get; private set; } = true;

    [Export]
    public int ExtendDurationTurnsOnReapply { get; private set; } = 0;

    [Export]
    public int DurationTurnCap { get; private set; } = -1;

    public virtual int ResolveMaxStacks(Node owner)
    {
        bool isBoss = owner != null && !owner.IsInGroup(GameGroups.PlayerCharacters);
        if (isBoss && BossMaxStacks > 0)
        {
            return Mathf.Max(1, BossMaxStacks);
        }

        return Mathf.Max(1, MaxStacks);
    }

    public virtual bool OnReapply(StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance, ActionDirector actionDirector, IRandomNumberGenerator rng = null)
    {
        if (manager == null || instance == null) return false;
        bool changed = false;

        int maxStacks = ResolveMaxStacks(manager.Owner);
        if (AddStackOnReapply)
        {
            int previous = instance.Stacks;
            instance.Stacks = Mathf.Clamp(instance.Stacks + 1, 1, maxStacks);
            changed = changed || instance.Stacks != previous;
        }

        if (RefreshDurationOnReapply)
        {
            int previousTurns = instance.RemainingTurns;
            instance.RemainingTurns = manager.RollDuration(this, rng);
            if (DurationTurnCap > 0)
            {
                instance.RemainingTurns = Mathf.Min(instance.RemainingTurns, DurationTurnCap);
            }
            changed = changed || instance.RemainingTurns != previousTurns;
        }
        else if (ExtendDurationTurnsOnReapply != 0)
        {
            int previousTurns = instance.RemainingTurns;
            instance.RemainingTurns = Mathf.Max(1, instance.RemainingTurns + ExtendDurationTurnsOnReapply);
            if (DurationTurnCap > 0)
            {
                instance.RemainingTurns = Mathf.Min(instance.RemainingTurns, DurationTurnCap);
            }
            changed = changed || instance.RemainingTurns != previousTurns;
        }

        OnStacksChanged(manager.Owner, manager, instance, actionDirector);
        return changed;
    }

    public virtual void OnStacksChanged(Node owner, StatusEffectManager manager, StatusEffectManager.StatusEffectInstance instance, ActionDirector actionDirector)
    {
    }
}
