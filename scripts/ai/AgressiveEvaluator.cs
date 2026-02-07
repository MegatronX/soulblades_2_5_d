using Godot;

[GlobalClass]
public partial class AggressiveEvaluator : AIEvaluator
{
    public override float Evaluate(ActionData action, Node user, Node target, AIController controller)
    {
        if (action.Category != "Attack") return 0f;

        float score = 50f; // Base preference for attacking

        // Bonus for low HP targets (Finish them off)
        var targetStats = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (targetStats != null && targetStats.GetStatValue(StatType.HP) > 0)
        {
            float hpPercent = (float)targetStats.CurrentHP / targetStats.GetStatValue(StatType.HP);
            if (hpPercent < 0.3f) score += 30f;
        }

        return score * Weight;
    }
}
