using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Confuse can rewrite a selected target to a random living combatant.
/// </summary>
[GlobalClass]
public partial class ConfuseStatusEffect : StatusEffect, IStatusActionRule
{
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float RandomizeTargetChance { get; private set; } = 0.5f;

    public bool TryRewriteTargets(ActionData action, ItemData sourceItem, Node owner, List<Node> currentTargets, BattleController battleController, IRandomNumberGenerator rng, out List<Node> rewrittenTargets)
    {
        rewrittenTargets = null;
        if (action == null || owner == null || battleController == null) return false;
        if (currentTargets == null || currentTargets.Count == 0) return false;
        if (RandomizeTargetChance <= 0f) return false;

        float roll = rng != null ? rng.RandRangeFloat(0f, 1f) : GD.Randf();
        if (roll > RandomizeTargetChance) return false;

        var candidates = battleController.GetLivingCombatants().Where(c => c != null).ToList();
        if (candidates.Count == 0) return false;

        int index = rng != null ? rng.RandRangeInt(0, candidates.Count - 1) : new RandomNumberGenerator().RandiRange(0, candidates.Count - 1);
        rewrittenTargets = new List<Node> { candidates[index] };
        return true;
    }
}
