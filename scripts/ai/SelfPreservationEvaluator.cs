using Godot;

/// <summary>
/// Prioritizes self-targeting defensive or healing actions when HP is low.
/// </summary>
[GlobalClass]
public partial class SelfPreservationEvaluator : AIEvaluator
{
    [Export(PropertyHint.Range, "0.0, 1.0")] 
    public float HealthThreshold { get; set; } = 0.4f; // Trigger when below 40% HP

    public override float Evaluate(ActionData action, Node user, Node target, AIController controller)
    {
        // We only care about actions targeting self
        if (target != user) return 0f;

        // Check if action is helpful (Heal or Defend)
        bool isHeal = action.Category == ActionCategory.Heal;
        bool isDefensive = action.Intent == ActionIntent.Defensive;

        if (!isHeal && !isDefensive) return 0f;

        var stats = user.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return 0f;

        float maxHP = stats.GetStatValue(StatType.HP);
        if (maxHP <= 0) return 0f;

        float hpPercent = (float)stats.CurrentHP / maxHP;

        if (hpPercent < HealthThreshold)
        {
            // The lower the HP, the higher the score.
            // Urgency goes from 0.0 (at threshold) to 1.0 (at 0 HP).
            float urgency = (HealthThreshold - hpPercent) / HealthThreshold; 
            
            // Base score of 50, scaling up to 150 based on urgency
            return (50f + (urgency * 100f)) * Weight;
        }

        return 0f;
    }
}
