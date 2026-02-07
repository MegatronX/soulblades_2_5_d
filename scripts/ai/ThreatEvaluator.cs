using Godot;

[GlobalClass]
public partial class ThreatEvaluator : AIEvaluator
{
    public override float Evaluate(ActionData action, Node user, Node target, AIController controller)
    {
        // Only consider threat for offensive actions against enemies
        if (action.Intent != ActionIntent.Aggressive) return 0f;

        float threat = controller.GetThreat(target);
        
        // Normalize or scale threat to a reasonable score range (e.g. 1 damage = 0.1 score)
        return (threat * 0.1f) * Weight;
    }
}
