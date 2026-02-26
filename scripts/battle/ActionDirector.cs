using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Orchestrates the execution pipeline of a single battle action.
/// Coordinates Mechanics (Logic), Animator (Visuals), and Timing.
/// </summary>
public partial class ActionDirector : Node
{
    // --- Dependencies ---
    // These would be other major systems that the BattleManager coordinates with.
    // We can imagine they are child nodes or singletons.
    [Export] private BattleMechanics _battleMechanics;
    [Export] private BattleAnimator _battleAnimator;
    [Export] private TimedHitManager _timedHitManager;
    public TimedHitManager TimedHitManager => _timedHitManager;
    
    // In a real implementation, this would be populated with the characters in the battle.
    private List<Node> _allCombatants = new();
    private readonly HashSet<StatusEffectManager> _subscribedStatusManagers = new();
    private readonly HashSet<AbilityManager> _subscribedAbilityManagers = new();
    
    public ActionContext CurrentContext { get; private set; }

    public override void _Ready()
    {
        if (_battleMechanics == null)
        {
            _battleMechanics = GetNodeOrNull<BattleMechanics>("BattleMechanics");
        }
        if (_battleAnimator == null)
        {
            _battleAnimator = GetNodeOrNull<BattleAnimator>("BattleAnimator");
        }
        if (_timedHitManager == null)
        {
            _timedHitManager = GetNodeOrNull<TimedHitManager>("TimedHitManager");
        }
        
        // Connect signals using the Subscribe extension to ensure cleanup.
        if (_battleAnimator != null)
        {
            this.Subscribe(
                () => _battleAnimator.TimedHitWindowOpened += OnTimedHitWindowOpened,
                () => _battleAnimator.TimedHitWindowOpened -= OnTimedHitWindowOpened
            );
        }
        if (_timedHitManager != null)
        {
            this.Subscribe(
                () => _timedHitManager.TimedHitResolved += OnTimedHitResolved,
                () => _timedHitManager.TimedHitResolved -= OnTimedHitResolved
            );
            this.Subscribe(
                () => _timedHitManager.TimedHitResolvedDetailed += OnTimedHitResolvedDetailed,
                () => _timedHitManager.TimedHitResolvedDetailed -= OnTimedHitResolvedDetailed
            );
        }
        if (_battleMechanics != null)
        {
            this.Subscribe(
                () => _battleMechanics.HookEventRaised += OnBattleHookEvent,
                () => _battleMechanics.HookEventRaised -= OnBattleHookEvent
            );
        }

        SubscribeAllCombatantHooks();
    }

    public override void _ExitTree()
    {
        UnsubscribeAllCombatantHooks();
    }

    public void SetRNG(IRandomNumberGenerator rng)
    {
        _battleMechanics?.SetRNG(rng);
    }

    public void Initialize(IEnumerable<Node> combatants)
    {
        UnsubscribeAllCombatantHooks();
        _allCombatants.Clear();
        if (combatants != null)
        {
            _allCombatants.AddRange(combatants.Where(c => c != null && GodotObject.IsInstanceValid(c)));
        }
        SubscribeAllCombatantHooks();
    }

    public void RegisterCombatant(Node combatant)
    {
        if (!_allCombatants.Contains(combatant))
        {
            _allCombatants.Add(combatant);
            SubscribeCombatantHooks(combatant);
        }
    }

    public void RemoveCombatant(Node combatant)
    {
        UnsubscribeCombatantHooks(combatant);
        _allCombatants.Remove(combatant);
    }

    /// <summary>
    /// The primary entry point for processing a chosen action.
    /// </summary>
    /// <param name="masterContext">The initial ActionContext created from the selected action and targets.</param>
    public async Task ProcessAction(ActionContext masterContext)
    {
        CurrentContext = masterContext;
        var speedFeedbackLocks = new List<CharacterVisualStateController>();
        try {
        // --- Phase 0: Cleanup ---
        // Ensure we don't have any references to freed objects (e.g. enemies killed in previous turns)
        PruneInvalidCombatants();
        LockSpeedFeedbackForAction(masterContext, speedFeedbackLocks);

        // --- Phase 1: OnInitiated Hooks ---
        // These hooks modify the master context before it's copied for each target.
        // Ideal for "outgoing" effects like "Charge Up" or ally buffs like "Commander's Aura".
        ApplyInitiationModifiers(masterContext);

        // --- Phase 2: Global Interception Hooks ---
        // This is for global effects like "Storm Drain" that can hijack an action.
        ApplyGlobalModifiers(masterContext);

        // --- Phase 2.5: Animation Windup ---
        if (_battleAnimator != null)
        {
            await _battleAnimator.PlayWindup(masterContext);
        }

        // --- Phase 3: Per-Target Modification ---
        // Create a unique, modifiable context for each target and let them react.
        var finalContexts = _battleMechanics.ProcessTargeting(masterContext, _allCombatants);

        // --- Phase 4: Preliminary Calculation ---
        // Calculate Hit/Crit/Base Damage, but DO NOT apply yet.
        _battleMechanics.CalculatePreliminary(finalContexts);

        // --- Phase 5: Animation Execution & Timed Hits ---
        if (_battleAnimator != null)
        {
            // This plays the attack animation and opens timed hit windows.
            // The Input system should listen to BattleAnimator signals to set flags on the ActionContext.
            await _battleAnimator.PlayExecution(masterContext, finalContexts);
        }

        // Propagate Timed Hit results from the Master Context (where the UI interaction happened)
        // to the individual Target Contexts (where calculation and logging happen).
        foreach (var ctx in finalContexts)
        {
            ctx.LastTimedHitRating = masterContext.LastTimedHitRating;
            ctx.TimedHitMultiplier = masterContext.TimedHitMultiplier;
            // Propagate events like "TimedHitSuccess"
            ctx.RuntimeEvents.UnionWith(masterContext.RuntimeEvents);
        }

        // --- Phase 6: Final Calculation & Application ---
        _battleMechanics.CalculateFinalAndApply(finalContexts);
        
        // --- Phase 6.5: Apply Costs ---
        // Costs are applied after successful execution (or before, depending on design choice)
        _battleMechanics.ApplyCosts(masterContext);

        // --- Phase 7: Reaction Animation ---
        if (_battleAnimator != null)
        {
            await _battleAnimator.PlayReaction(finalContexts);
        }
        UnlockSpeedFeedback(speedFeedbackLocks);

        // --- Phase 8: Reactions (Counter Attacks) ---
        // Check if any targets want to react to what just happened.
        _battleMechanics.ProcessPostExecution(finalContexts);

        // Process master-context reactions (e.g. Echo Cast) before per-target reactions.
        foreach (var reaction in masterContext.PendingReactions.ToList())
        {
            await ProcessAction(reaction);
        }

        foreach (var ctx in finalContexts)
        {
            foreach (var reaction in ctx.PendingReactions)
            {
                await ProcessAction(reaction);
            }
        }

        // --- Phase 9: Handle Chain Actions ---
        // After the primary action resolves, check if it should chain into another.
        await HandleChainActions(masterContext, finalContexts);

        // Log Action Results
        foreach (var ctx in finalContexts)
        {
            string targetName = GodotObject.IsInstanceValid(ctx.CurrentTarget) ? ctx.CurrentTarget.Name : "Target (Defeated)";
            var result = ctx.GetResult(ctx.CurrentTarget);
            GD.Print($"[Action Result] Action: {ctx.SourceAction.CommandName}, Target: {targetName}, Hit: {result.IsHit}, Crit: {result.IsCritical}, Timing: {ctx.LastTimedHitRating}");
        }

        // Notify systems (like AI) that an action has completed
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        eventBus.EmitSignal(GlobalEventBus.SignalName.ActionExecuted, masterContext);
        }
        finally {
            UnlockSpeedFeedback(speedFeedbackLocks);
            CurrentContext = null;
        }
    }

    private void OnTimedHitWindowOpened(TimedHitSettings settings, ActionContext context, float timeToHit, int windowIndex)
    {
        _timedHitManager?.StartWindow(settings, context, timeToHit, windowIndex);
    }

    private void OnTimedHitResolved(TimedHitRating rating, ActionContext context, TimedHitSettings settings)
    {
        _battleAnimator?.PlayTimedHitEffect(rating, context, settings);
    }

    private void OnTimedHitResolvedDetailed(
        TimedHitRating rating,
        ActionContext context,
        TimedHitSettings settings,
        float signedOffsetSeconds,
        float absoluteOffsetSeconds,
        int windowIndex)
    {
        _battleAnimator?.PlayTimedTimingFeedback(rating, context, settings, signedOffsetSeconds);
    }

    public ActionContext BuildPreviewContext(ActionData action, Node initiator, IEnumerable<Node> targets, ItemData sourceItem = null)
    {
        if (action == null || initiator == null) return null;

        PruneInvalidCombatants();
        var targetList = targets?
            .Where(t => t != null && GodotObject.IsInstanceValid(t))
            .ToList() ?? new List<Node>();

        var context = new ActionContext(action, initiator, targetList, sourceItem);
        ApplyInitiationModifiers(context);
        ApplyGlobalModifiers(context);
        return context;
    }

    public float ResolveTickCost(ActionContext context)
    {
        if (context?.SourceAction == null) return 0f;
        float resolvedTickCost = context.SourceAction.TickCost + context.TickCostAdjustment;
        return Mathf.Max(-TurnManager.TickThreshold + 1f, resolvedTickCost);
    }

    /// <summary>
    /// Checks for and processes any chain actions after the initial action has resolved.
    /// </summary>
    // Note: This needs to be async now too
    private async Task HandleChainActions(ActionContext originalContext, List<ActionContext> resolvedContexts)
    {
        // Find the chain component on the original action.
        var chainComponent = originalContext.GetComponent<ChainActionComponent>();
        if (chainComponent == null || chainComponent.SourceData is not ChainActionComponentData chainData)
        {
            return; // This action cannot chain.
        }

        // We only need to check conditions against the first resolved context,
        // as runtime events like a timed hit are typically tied to the overall action.
        var primaryContext = resolvedContexts.FirstOrDefault();
        if (primaryContext == null) return;

        // Check if all conditions for the chain are met.
        bool allConditionsMet = chainData.Conditions.All(c => c.IsMet(primaryContext));

        if (allConditionsMet)
        {
            GD.Print($"Chain action '{chainData.ChainedAction.ResourceName}' triggered!");
            // Create a new master context for the chained action and process it immediately.
            // The new action uses the same initiator but targets the final target of the first action.
            var newMasterContext = new ActionContext(chainData.ChainedAction, primaryContext.Initiator, new[] { primaryContext.CurrentTarget });
            await ProcessAction(newMasterContext);
        }
    }

    private bool IsAlly(Node a, Node b)
    {
        return a.IsInGroup(GameGroups.PlayerCharacters) == b.IsInGroup(GameGroups.PlayerCharacters);
    }

    private void ApplyInitiationModifiers(ActionContext context)
    {
        if (context == null || context.Initiator == null || _battleMechanics == null) return;

        var allies = _allCombatants.Where(c => c != context.Initiator && IsAlly(c, context.Initiator));
        _battleMechanics.ProcessInitiation(context, allies);
    }

    private void ApplyGlobalModifiers(ActionContext context)
    {
        if (context == null || _battleMechanics == null) return;
        _battleMechanics.ProcessGlobalMods(context, _allCombatants);
    }

    private void PruneInvalidCombatants()
    {
        for (int i = _allCombatants.Count - 1; i >= 0; i--)
        {
            var combatant = _allCombatants[i];
            if (combatant != null && GodotObject.IsInstanceValid(combatant)) continue;
            UnsubscribeCombatantHooks(combatant);
            _allCombatants.RemoveAt(i);
        }
        PruneInvalidHookSubscriptions();
    }

    private void LockSpeedFeedbackForAction(ActionContext context, List<CharacterVisualStateController> lockedControllers)
    {
        if (lockedControllers == null) return;

        var seen = new HashSet<CharacterVisualStateController>();

        void LockNode(Node node)
        {
            if (node == null || !GodotObject.IsInstanceValid(node)) return;
            var visualController = node.GetNodeOrNull<CharacterVisualStateController>(CharacterVisualStateController.NodeName);
            if (visualController == null) return;
            if (!seen.Add(visualController)) return;

            visualController.PushSpeedFeedbackLock();
            lockedControllers.Add(visualController);
        }

        LockNode(context?.Initiator);
        foreach (var combatant in _allCombatants)
        {
            LockNode(combatant);
        }
    }

    private static void UnlockSpeedFeedback(List<CharacterVisualStateController> lockedControllers)
    {
        if (lockedControllers == null || lockedControllers.Count == 0) return;

        foreach (var controller in lockedControllers)
        {
            controller?.PopSpeedFeedbackLock();
        }

        lockedControllers.Clear();
    }

    private void SubscribeAllCombatantHooks()
    {
        foreach (var combatant in _allCombatants)
        {
            SubscribeCombatantHooks(combatant);
        }
    }

    private void SubscribeCombatantHooks(Node combatant)
    {
        if (combatant == null || !GodotObject.IsInstanceValid(combatant)) return;

        var statusManager = combatant.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (statusManager != null && _subscribedStatusManagers.Add(statusManager))
        {
            statusManager.HookEventRaised += OnBattleHookEvent;
        }

        var abilityManager = combatant.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
        if (abilityManager != null && _subscribedAbilityManagers.Add(abilityManager))
        {
            abilityManager.HookEventRaised += OnBattleHookEvent;
        }
    }

    private void UnsubscribeCombatantHooks(Node combatant)
    {
        if (combatant == null || !GodotObject.IsInstanceValid(combatant)) return;

        var statusManager = combatant.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (statusManager != null && _subscribedStatusManagers.Remove(statusManager))
        {
            statusManager.HookEventRaised -= OnBattleHookEvent;
        }

        var abilityManager = combatant.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
        if (abilityManager != null && _subscribedAbilityManagers.Remove(abilityManager))
        {
            abilityManager.HookEventRaised -= OnBattleHookEvent;
        }
    }

    private void UnsubscribeAllCombatantHooks()
    {
        foreach (var statusManager in _subscribedStatusManagers.ToList())
        {
            if (statusManager == null || !GodotObject.IsInstanceValid(statusManager)) continue;
            statusManager.HookEventRaised -= OnBattleHookEvent;
        }
        _subscribedStatusManagers.Clear();

        foreach (var abilityManager in _subscribedAbilityManagers.ToList())
        {
            if (abilityManager == null || !GodotObject.IsInstanceValid(abilityManager)) continue;
            abilityManager.HookEventRaised -= OnBattleHookEvent;
        }
        _subscribedAbilityManagers.Clear();
    }

    private void PruneInvalidHookSubscriptions()
    {
        foreach (var statusManager in _subscribedStatusManagers.ToList())
        {
            if (statusManager != null && GodotObject.IsInstanceValid(statusManager)) continue;
            _subscribedStatusManagers.Remove(statusManager);
        }

        foreach (var abilityManager in _subscribedAbilityManagers.ToList())
        {
            if (abilityManager != null && GodotObject.IsInstanceValid(abilityManager)) continue;
            _subscribedAbilityManagers.Remove(abilityManager);
        }
    }

    private void OnBattleHookEvent(BattleHookEvent hookEvent)
    {
        if (hookEvent == null) return;
        if (!TryMapHookToVisualEvent(hookEvent.EventType, out var visualEventType)) return;

        if (hookEvent.StatusEffect != null)
        {
            BattleVisualEffectRunner.DispatchStatusEvent(
                hookEvent.StatusEffect,
                visualEventType,
                hookEvent.Owner,
                hookEvent.StatusManager,
                hookEvent.StatusInstance,
                hookEvent.ActionDirector,
                hookEvent.ActionContext,
                hookEvent.ActionResult,
                hookEvent.RelatedNode);
        }

        if (hookEvent.AbilityEffect != null)
        {
            BattleVisualEffectRunner.DispatchAbilityEffectEvent(
                hookEvent.AbilityEffect,
                hookEvent.Ability,
                visualEventType,
                hookEvent.Owner,
                hookEvent.AbilityContext,
                hookEvent.ActionContext,
                hookEvent.ActionResult,
                hookEvent.RelatedNode);
        }
    }

    private static bool TryMapHookToVisualEvent(BattleHookEventType hookType, out BattleVisualEventType visualType)
    {
        visualType = BattleVisualEventType.Persistent;
        switch (hookType)
        {
            case BattleHookEventType.StatusApplied:
                visualType = BattleVisualEventType.StatusApplied;
                return true;
            case BattleHookEventType.StatusRemoved:
                visualType = BattleVisualEventType.StatusRemoved;
                return true;
            case BattleHookEventType.TurnStart:
                visualType = BattleVisualEventType.TurnStart;
                return true;
            case BattleHookEventType.TurnEnd:
                visualType = BattleVisualEventType.TurnEnd;
                return true;
            case BattleHookEventType.ActionInitiated:
                visualType = BattleVisualEventType.ActionInitiated;
                return true;
            case BattleHookEventType.AllyActionInitiated:
                visualType = BattleVisualEventType.AllyActionInitiated;
                return true;
            case BattleHookEventType.ActionBroadcast:
                visualType = BattleVisualEventType.ActionBroadcast;
                return true;
            case BattleHookEventType.ActionTargeted:
                visualType = BattleVisualEventType.ActionTargeted;
                return true;
            case BattleHookEventType.ActionPostExecution:
                visualType = BattleVisualEventType.ActionPostExecution;
                return true;
            case BattleHookEventType.AbilityTriggered:
                visualType = BattleVisualEventType.AbilityTriggered;
                return true;
            default:
                return false;
        }
    }
}
