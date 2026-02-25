using Godot;
using System.Collections.Generic;

/// <summary>
/// Builds deterministic turn-order preview payloads from the current targeting state.
/// </summary>
public sealed class ActionPreviewBuilder
{
    private readonly BattleController _battleController;
    private readonly ActionDirector _actionDirector;

    public ActionPreviewBuilder(BattleController battleController, ActionDirector actionDirector)
    {
        _battleController = battleController;
        _actionDirector = actionDirector;
    }

    public TurnManager.ActionPreview Build(BattleCommand command, ActionData action, Node actor, List<Node> selectedTargets)
    {
        var preview = new TurnManager.ActionPreview();
        if (action == null || actor == null)
        {
            return preview;
        }

        ItemData sourceItem = (command as ItemBattleCommand)?.Item;
        var rewrittenTargets = _battleController?.RewriteTargetsFromStatusRules(actor, action, sourceItem, selectedTargets) ?? selectedTargets;
        rewrittenTargets ??= new List<Node>();

        var context = _actionDirector?.BuildPreviewContext(action, actor, rewrittenTargets, sourceItem)
            ?? new ActionContext(action, actor, rewrittenTargets, sourceItem);

        preview.ActionTickCost = _actionDirector != null
            ? _actionDirector.ResolveTickCost(context)
            : Mathf.Max(-TurnManager.TickThreshold + 1f, action.TickCost + context.TickCostAdjustment);

        AddGuaranteedAppliedStatuses(preview, rewrittenTargets, action, context);
        return preview;
    }

    private static void AddGuaranteedAppliedStatuses(TurnManager.ActionPreview preview, List<Node> targets, ActionData action, ActionContext context)
    {
        if (preview == null || targets == null) return;

        var entries = new List<StatusEffectChanceEntry>();
        if (action?.StatusEffectsOnHit != null)
        {
            entries.AddRange(action.StatusEffectsOnHit);
        }
        if (context?.ExtraStatusEffectsOnHit != null && context.ExtraStatusEffectsOnHit.Count > 0)
        {
            entries.AddRange(context.ExtraStatusEffectsOnHit);
        }
        if (entries.Count == 0) return;

        foreach (var target in targets)
        {
            if (target == null || !GodotObject.IsInstanceValid(target)) continue;
            if (!preview.AppliedEffects.TryGetValue(target, out var targetEffects))
            {
                targetEffects = new List<StatusEffect>();
                preview.AppliedEffects[target] = targetEffects;
            }

            foreach (var entry in entries)
            {
                if (entry?.Effect == null) continue;
                if (entry.ChancePercent < 100f) continue; // Keep preview deterministic.

                if (!targetEffects.Contains(entry.Effect))
                {
                    targetEffects.Add(entry.Effect);
                }
            }
        }
    }
}
