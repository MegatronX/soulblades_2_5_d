using Godot;

[GlobalClass]
public partial class HealerEvaluator : AIEvaluator
{
    public override float Evaluate(ActionData action, Node user, Node target, AIController controller)
    {
        if (action.Category != "Heal") return 0f;

        var targetStats = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (targetStats != null && targetStats.GetStatValue(StatType.HP) > 0)
        {
            float hpPercent = (float)targetStats.CurrentHP / targetStats.GetStatValue(StatType.HP);
            
            if (hpPercent < 0.5f) 
            {
                // Score increases as HP gets lower
                return ((1.0f - hpPercent) * 100f) * Weight;
            }
            else
            {
                // Penalty for healing high HP targets
                return -50f * Weight;
            }
        }

        return 0f;
    }
}
