using Godot;

/// <summary>
/// Reusable status base for temporarily overriding a combatant's AI controller strategy.
/// Handles apply/remove lifecycle, preserving and restoring prior AI state.
/// </summary>
[GlobalClass]
public partial class AIOverrideStatusEffect : StatusEffect
{
    private const string StatePreviousStrategy = "ai_override_prev_strategy";
    private const string StatePreviousSuppressed = "ai_override_prev_suppressed";
    private const string StateCreatedAiController = "ai_override_created_ai";
    private const string StateOverrideStrategy = "ai_override_strategy";

    [ExportGroup("AI Override")]
    [Export]
    public bool EnableAiOverride { get; private set; } = false;

    [Export]
    public bool CreateAiControllerIfMissing { get; private set; } = true;

    [Export]
    public bool RemoveCreatedAiControllerOnRemove { get; private set; } = true;

    [Export]
    public bool UnsuppressAiWhileActive { get; private set; } = true;

    [Export(PropertyHint.ResourceType, "AIStrategy")]
    public AIStrategy OverrideStrategy { get; private set; }

    public override void OnApply(Node owner, ActionDirector actionDirector)
    {
        base.OnApply(owner, actionDirector);
        ApplyAiOverride(owner);
    }

    public override void OnRemove(Node owner, ActionDirector actionDirector)
    {
        base.OnRemove(owner, actionDirector);
        RemoveAiOverride(owner);
    }

    protected virtual bool ShouldApplyAiOverride(Node owner)
    {
        return EnableAiOverride;
    }

    protected virtual AIStrategy BuildOverrideStrategy(Node owner)
    {
        return OverrideStrategy;
    }

    private void ApplyAiOverride(Node owner)
    {
        if (owner == null) return;
        if (!ShouldApplyAiOverride(owner)) return;

        var manager = owner.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (manager == null) return;

        var strategy = BuildOverrideStrategy(owner);
        if (strategy == null) return;

        var aiController = owner.GetNodeOrNull<AIController>(AIController.DefaultName);
        bool createdController = false;
        if (aiController == null && CreateAiControllerIfMissing)
        {
            aiController = new AIController
            {
                Name = AIController.DefaultName,
                IsSuppressed = false
            };
            owner.AddChild(aiController);
            createdController = true;
        }

        if (aiController == null) return;

        manager.SetState(this, StatePreviousStrategy, aiController.Strategy);
        manager.SetState(this, StatePreviousSuppressed, aiController.IsSuppressed);
        manager.SetState(this, StateCreatedAiController, createdController);
        manager.SetState(this, StateOverrideStrategy, strategy);

        aiController.Strategy = strategy;
        if (UnsuppressAiWhileActive)
        {
            aiController.IsSuppressed = false;
        }
    }

    private void RemoveAiOverride(Node owner)
    {
        if (owner == null) return;
        if (!ShouldApplyAiOverride(owner)) return;

        var manager = owner.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (manager == null) return;

        var aiController = owner.GetNodeOrNull<AIController>(AIController.DefaultName);
        if (aiController == null) return;

        if (!manager.TryGetState(this, StateOverrideStrategy, out var overrideValue))
        {
            return;
        }

        var overrideStrategy = overrideValue.As<AIStrategy>();
        if (overrideStrategy != null && !ReferenceEquals(aiController.Strategy, overrideStrategy))
        {
            return;
        }

        bool createdController = manager.TryGetState(this, StateCreatedAiController, out var createdValue) && createdValue.AsBool();
        if (createdController && RemoveCreatedAiControllerOnRemove)
        {
            aiController.QueueFree();
            return;
        }

        AIStrategy previousStrategy = null;
        if (manager.TryGetState(this, StatePreviousStrategy, out var previousStrategyValue))
        {
            previousStrategy = previousStrategyValue.As<AIStrategy>();
        }

        aiController.Strategy = previousStrategy;

        if (manager.TryGetState(this, StatePreviousSuppressed, out var suppressedValue))
        {
            aiController.IsSuppressed = suppressedValue.AsBool();
        }
    }
}
