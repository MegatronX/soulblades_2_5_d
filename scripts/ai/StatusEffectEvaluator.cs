using Godot;

/// <summary>
/// Evaluates actions that apply status effects (Debuffs).
/// Checks for immunity memory to avoid spamming ineffective moves.
/// </summary>
[GlobalClass]
public partial class StatusEffectEvaluator : AIEvaluator
{
    [Export] public ActionCategory TargetCategory { get; set; } = ActionCategory.Debuff; // Matches ActionData.Category

    public override float Evaluate(ActionData action, Node user, Node target, AIController controller)
    {
        // 1. Only evaluate relevant actions
        if (action.Category != TargetCategory) return 0f;

        // 2. Check Memory: Have we tried this before and failed?
        if (controller.IsImmune(target, action.Category))
        {
            return -100f * Weight; // Strongly discourage
        }

        // 3. Check Current State: Does the target already have this effect?
        // (Requires looking up the StatusEffectManager on the target)
        var statusManager = target.GetNodeOrNull<StatusEffectManager>("StatusEffectManager");
        if (statusManager != null)
        {
            // This assumes we can check if a specific effect is active.
            // For a generic check, we might need to look at the action's components to see what effect it applies.
            // For now, we'll assume a heuristic: if they have *any* debuff, maybe lower priority?
            // Or better: if we can't check specific effect presence easily, we rely on the cooldown/duration logic.
        }

        // 4. Base Score: High value for debuffing
        return 40f * Weight;
    }
}
