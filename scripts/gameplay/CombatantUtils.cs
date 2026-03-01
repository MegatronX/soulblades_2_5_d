using Godot;

/// <summary>
/// Shared helper methods for combatant node validation.
/// </summary>
public static class CombatantUtils
{
    public static bool IsValidLivingCombatant(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node)) return false;
        var stats = node.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        return stats != null && stats.CurrentHP > 0;
    }
}
