using Godot;

/// <summary>
/// Prioritizes disabling actions against high-threat targets.
/// </summary>
[GlobalClass]
public partial class TacticalEvaluator : AIEvaluator
{
    [Export] public ActionCategory DebuffCategory { get; set; } = ActionCategory.Debuff;

    public override float Evaluate(ActionData action, Node user, Node target, AIController controller)
    {
        // We are looking for debuffs/disables
        if (action.Category != DebuffCategory) return 0f;

        // Get threat of the target
        float threat = controller.GetThreat(target);

        // If threat is high, we REALLY want to debuff them.
        // Scale threat to score (e.g. 100 threat = 50 score)
        float score = threat * 0.5f;

        return score * Weight;
    }
}
