using Godot;

/// <summary>
/// Dispatches resource-based visual effect hooks for statuses and abilities.
/// </summary>
public static class BattleVisualEffectRunner
{
    private static BattleVisualEffect AsVisualEffect(Resource resource)
    {
        return resource as BattleVisualEffect;
    }

    public static void DispatchStatusEvent(
        StatusEffect status,
        BattleVisualEventType eventType,
        Node owner,
        StatusEffectManager statusManager,
        StatusEffectManager.StatusEffectInstance statusInstance,
        ActionDirector actionDirector = null,
        ActionContext actionContext = null,
        ActionResult actionResult = null,
        Node relatedNode = null,
        CharacterVisualStateController visualController = null,
        int sourceOrder = 0)
    {
        if (status == null || owner == null) return;
        if (status.VisualEffects == null || status.VisualEffects.Count == 0) return;

        for (int i = 0; i < status.VisualEffects.Count; i++)
        {
            var effect = AsVisualEffect(status.VisualEffects[i]);
            if (effect == null || !effect.Matches(eventType)) continue;

            var context = new BattleVisualEffectContext
            {
                EventType = eventType,
                Owner = owner,
                RelatedNode = relatedNode,
                StatusEffect = status,
                StatusManager = statusManager,
                StatusInstance = statusInstance,
                ActionDirector = actionDirector,
                ActionContext = actionContext,
                ActionResult = actionResult,
                VisualController = visualController,
                SourcePriority = status.Priority,
                EffectPriority = effect.Priority,
                SourceOrder = (sourceOrder * 256) + i,
            };

            effect.OnEvent(context);
        }
    }

    public static void ContributeStatusPersistent(
        StatusEffect status,
        Node owner,
        StatusEffectManager statusManager,
        StatusEffectManager.StatusEffectInstance statusInstance,
        BattleVisualStateAccumulator state,
        CharacterVisualStateController visualController,
        int sourceOrder)
    {
        if (status == null || state == null) return;
        if (status.VisualEffects == null || status.VisualEffects.Count == 0) return;

        for (int i = 0; i < status.VisualEffects.Count; i++)
        {
            var effect = AsVisualEffect(status.VisualEffects[i]);
            if (effect == null || !effect.Matches(BattleVisualEventType.Persistent)) continue;

            var context = new BattleVisualEffectContext
            {
                EventType = BattleVisualEventType.Persistent,
                Owner = owner,
                StatusEffect = status,
                StatusManager = statusManager,
                StatusInstance = statusInstance,
                VisualController = visualController,
                SourcePriority = status.Priority,
                EffectPriority = effect.Priority,
                SourceOrder = (sourceOrder * 256) + i,
            };

            effect.ContributePersistent(state, context);
        }
    }

    public static void DispatchAbilityEffectEvent(
        AbilityEffect abilityEffect,
        Ability ability,
        BattleVisualEventType eventType,
        Node owner,
        AbilityEffectContext abilityContext = null,
        ActionContext actionContext = null,
        ActionResult actionResult = null,
        Node relatedNode = null,
        CharacterVisualStateController visualController = null,
        int sourceOrder = 0)
    {
        if (abilityEffect == null || owner == null) return;
        if (abilityEffect.VisualEffects == null || abilityEffect.VisualEffects.Count == 0) return;

        for (int i = 0; i < abilityEffect.VisualEffects.Count; i++)
        {
            var effect = AsVisualEffect(abilityEffect.VisualEffects[i]);
            if (effect == null || !effect.Matches(eventType)) continue;

            var context = new BattleVisualEffectContext
            {
                EventType = eventType,
                Owner = owner,
                RelatedNode = relatedNode,
                Ability = ability,
                AbilityEffect = abilityEffect,
                AbilityContext = abilityContext,
                ActionContext = actionContext ?? abilityContext?.ActionContext,
                ActionResult = actionResult ?? abilityContext?.ActionResult,
                VisualController = visualController,
                SourcePriority = abilityEffect.Priority,
                EffectPriority = effect.Priority,
                SourceOrder = (sourceOrder * 256) + i,
            };

            effect.OnEvent(context);
        }
    }

    public static void ContributeAbilityEffectPersistent(
        AbilityEffect abilityEffect,
        Ability ability,
        Node owner,
        BattleVisualStateAccumulator state,
        CharacterVisualStateController visualController,
        int sourceOrder)
    {
        if (abilityEffect == null || state == null) return;
        if (abilityEffect.VisualEffects == null || abilityEffect.VisualEffects.Count == 0) return;

        for (int i = 0; i < abilityEffect.VisualEffects.Count; i++)
        {
            var effect = AsVisualEffect(abilityEffect.VisualEffects[i]);
            if (effect == null || !effect.Matches(BattleVisualEventType.Persistent)) continue;

            var context = new BattleVisualEffectContext
            {
                EventType = BattleVisualEventType.Persistent,
                Owner = owner,
                Ability = ability,
                AbilityEffect = abilityEffect,
                VisualController = visualController,
                SourcePriority = abilityEffect.Priority,
                EffectPriority = effect.Priority,
                SourceOrder = (sourceOrder * 256) + i,
            };

            effect.ContributePersistent(state, context);
        }
    }
}
