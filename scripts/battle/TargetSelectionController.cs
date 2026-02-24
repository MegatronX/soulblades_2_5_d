using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class TargetSelectionController : Node
{
    [Export] private Node _playerTeamContainer;
    [Export] private Node _enemyTeamContainer;
    [Export] private Node _allyTeamContainer;
    
    [Export] 
    private PackedScene _targetIndicatorScene;

    private ActionData _currentAction;
    private BattleCommand _currentCommand;
    private Node _actor;
    private IInputProvider _inputProvider;
    private BattleController _battleController;
    
    private TargetingType _currentTargetingMode;
    private TargetingType _allowedTargeting;

    // Navigation State
    private List<List<Node>> _validParties = new();
    private int _currentPartyIndex = 0;
    private int _currentTargetIndex = 0;
    private bool _isActive = false;
    
    private Dictionary<Node, Node3D> _activeIndicators = new();

    public override void _Ready()
    {
        _battleController = GetTree().Root.FindChild("BattleController", true, false) as BattleController;

        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        this.Subscribe(
            () => eventBus.ActionSelectedForTargeting += OnActionSelected,
            () => eventBus.ActionSelectedForTargeting -= OnActionSelected
        );

        SetProcess(false);
    }

    private void OnActionSelected(BattleCommand command, Node actor)
    {
        if (!IsBattleActive()) return;
        ActionData actionData = command as ActionData;
        if (actionData == null && command is ItemBattleCommand itemCommand)
        {
            actionData = itemCommand.Action
                ?? itemCommand.Item?.Components?.OfType<ConsumableComponentData>().FirstOrDefault()?.ActionToPerform;
        }

        if (actionData == null) return;

        _currentCommand = command;
        _currentAction = actionData;
        _actor = actor;

        // 1. Get Input Provider
        var playerController = actor.GetNodeOrNull<PlayerController>(PlayerController.DefaultName);
        if (playerController == null)
        {
            GD.PrintErr($"TargetSelectionController: No PlayerController found on {actor.Name}");
            return;
        }
        _inputProvider = playerController.InputProvider;

        // 2. Calculate Allowed Targeting (Base + Modifiers)
        _allowedTargeting = actionData.AllowedTargeting;
        var modifiers = actor.FindChildren("*", recursive: true).OfType<IActionModifier>();
        foreach (var modifier in modifiers)
        {
            _allowedTargeting = modifier.ModifyAllowedTargeting(_allowedTargeting);
        }

        // 3. Initialize State
        _currentTargetingMode = actionData.BaseTargeting;
        RefreshTargets();
        
        // Default selection logic
        SelectDefaultTarget();

        _isActive = true;
        SetProcess(true);
        UpdateVisuals();
    }

    public override void _Process(double delta)
    {
        if (_isActive && !IsBattleActive())
        {
            AbortTargeting();
            return;
        }

        if (!_isActive || _inputProvider == null) return;

        // Handle Confirmation
        if (_inputProvider.IsActionJustPressed(GameInputAction.Confirm))
        {
            ConfirmTargeting();
            return;
        }

        // Handle Cancellation
        if (_inputProvider.IsActionJustPressed(GameInputAction.Cancel))
        {
            CancelTargeting();
            return;
        }

        // Handle Promotion/Demotion
        if (_inputProvider.IsActionJustPressed(GameInputAction.AuxLeft))
        {
            TryPromoteTargeting();
        }
        else if (_inputProvider.IsActionJustPressed(GameInputAction.AuxRight))
        {
            TryDemoteTargeting();
        }

        // Handle Navigation
        if (IsSingleTargetMode(_currentTargetingMode))
        {
            HandleSingleTargetNavigation();
        }
        else if (IsPartyTargetMode(_currentTargetingMode))
        {
            HandlePartyNavigation();
        }
    }

    private void HandleSingleTargetNavigation()
    {
        if (_validParties.Count == 0) return;
        var currentParty = _validParties[_currentPartyIndex];
        if (currentParty.Count == 0) return;

        // Up/Down: Cycle targets within the same party
        if (_inputProvider.IsActionJustPressed(GameInputAction.Up))
        {
            _currentTargetIndex--;
            if (_currentTargetIndex < 0) _currentTargetIndex = currentParty.Count - 1;
            UpdateVisuals();
            UISoundManager.Instance?.Play(UISoundType.Navigation);
        }
        else if (_inputProvider.IsActionJustPressed(GameInputAction.Down))
        {
            _currentTargetIndex++;
            if (_currentTargetIndex >= currentParty.Count) _currentTargetIndex = 0;
            UpdateVisuals();
            UISoundManager.Instance?.Play(UISoundType.Navigation);
        }

        // Left/Right: Switch party
        if (_inputProvider.IsActionJustPressed(GameInputAction.Left))
        {
            SwitchParty(-1);
        }
        else if (_inputProvider.IsActionJustPressed(GameInputAction.Right))
        {
            SwitchParty(1);
        }
    }

    private void SwitchParty(int direction)
    {
        if (_validParties.Count <= 1) return;

        _currentPartyIndex += direction;
        if (_currentPartyIndex < 0) _currentPartyIndex = _validParties.Count - 1;
        if (_currentPartyIndex >= _validParties.Count) _currentPartyIndex = 0;

        // Clamp target index to the nearest valid index in the new party
        var newParty = _validParties[_currentPartyIndex];
        if (_currentTargetIndex >= newParty.Count)
        {
            _currentTargetIndex = newParty.Count - 1;
        }
        if (_currentTargetIndex < 0) _currentTargetIndex = 0;

        UpdateVisuals();
        UISoundManager.Instance?.Play(UISoundType.Navigation);
    }

    private void HandlePartyNavigation()
    {
        if (_validParties.Count == 0) return;
        if (_currentTargetingMode == TargetingType.All) return; // Cannot navigate when targeting everything

        // Left/Right: Switch party
        if (_inputProvider.IsActionJustPressed(GameInputAction.Left))
        {
            SwitchParty(-1);
        }
        else if (_inputProvider.IsActionJustPressed(GameInputAction.Right))
        {
            SwitchParty(1);
        }
    }

    private void TryPromoteTargeting()
    {
        var nextMode = GetNextTargetingLevel(_currentTargetingMode, true);
        if (nextMode != _currentTargetingMode && (_allowedTargeting.HasFlag(nextMode) || nextMode == TargetingType.All))
        {
            // Check if we actually have permission for 'All' if that's the next step
            if (nextMode == TargetingType.All && !_allowedTargeting.HasFlag(TargetingType.All)) return;

            _currentTargetingMode = nextMode;
            RefreshTargets(); // Re-filter targets based on new mode
            UpdateVisuals();
            UISoundManager.Instance?.Play(UISoundType.PageFlip);
        }
    }

    private void TryDemoteTargeting()
    {
        var prevMode = GetNextTargetingLevel(_currentTargetingMode, false);
        if (prevMode != _currentTargetingMode && (_allowedTargeting.HasFlag(prevMode) || prevMode == _currentAction.BaseTargeting))
        {
             _currentTargetingMode = prevMode;
            RefreshTargets();
            UpdateVisuals();
            UISoundManager.Instance?.Play(UISoundType.PageFlip);
        }
    }

    private TargetingType GetNextTargetingLevel(TargetingType current, bool promote)
    {
        // Hierarchy: Single -> Party -> All
        
        // 1. Identify current "Base" type (Enemy, Ally, Self)
        bool isEnemy = current.HasFlag(TargetingType.AnyEnemy) || current == TargetingType.AnyEnemyParty;
        bool isAlly = current.HasFlag(TargetingType.AnyAlly) || current == TargetingType.AnyAllyParty;
        bool isSelf = current == TargetingType.Self || current == TargetingType.OwnParty;

        if (promote)
        {
            if (current == TargetingType.All) return TargetingType.All;

            // Single -> Party
            if (current == TargetingType.AnyEnemy) return TargetingType.AnyEnemyParty;
            if (current == TargetingType.AnyAlly) return TargetingType.AnyAllyParty;
            if (current == TargetingType.Self) return TargetingType.OwnParty;
            if (current == TargetingType.AnySingleTarget) return TargetingType.AnySingleParty;

            // Party -> All
            if (IsPartyTargetMode(current)) return TargetingType.All;
        }
        else
        {
            // All -> Party
            if (current == TargetingType.All)
            {
                // Demote back to the party type relevant to the action's base
                if (_currentAction.BaseTargeting.HasFlag(TargetingType.AnyEnemy)) return TargetingType.AnyEnemyParty;
                if (_currentAction.BaseTargeting.HasFlag(TargetingType.AnyAlly)) return TargetingType.AnyAllyParty;
                if (_currentAction.BaseTargeting == TargetingType.Self) return TargetingType.OwnParty;
                return TargetingType.AnySingleParty;
            }

            // Party -> Single
            if (current == TargetingType.AnyEnemyParty) return TargetingType.AnyEnemy;
            if (current == TargetingType.AnyAllyParty) return TargetingType.AnyAlly;
            if (current == TargetingType.OwnParty) return TargetingType.Self;
            if (current == TargetingType.AnySingleParty) return TargetingType.AnySingleTarget;
        }

        return current;
    }

    private void RefreshTargets()
    {
        // Capture current target to preserve selection across mode changes (e.g. Single -> Party)
        Node previousTarget = null;
        if (_validParties.Count > 0 && _currentPartyIndex >= 0 && _currentPartyIndex < _validParties.Count)
        {
            var party = _validParties[_currentPartyIndex];
            if (_currentTargetIndex >= 0 && _currentTargetIndex < party.Count)
            {
                previousTarget = party[_currentTargetIndex];
            }
        }

        _validParties.Clear();

        // Filter targets based on whether the action allows targeting dead units
        bool canTargetDead = _currentAction.CanTargetDead;
        bool IsValidTarget(Node n)
        {
            if (canTargetDead) return true;
            var s = n.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
            return s == null || s.CurrentHP > 0;
        }

        var players = _playerTeamContainer.GetChildren().Cast<Node>().Where(IsValidTarget).ToList();
        var allies = _allyTeamContainer.GetChildren().Cast<Node>().Where(IsValidTarget).ToList();
        var enemies = _enemyTeamContainer.GetChildren().Cast<Node>().Where(IsValidTarget).ToList();

        void AddParty(List<Node> party)
        {
            if (party.Count > 0) _validParties.Add(party);
        }

        // Filter based on current mode
        if (_currentTargetingMode == TargetingType.All || 
            _currentTargetingMode.HasFlag(TargetingType.AnySingleTarget) ||
            _currentTargetingMode.HasFlag(TargetingType.AnySingleParty))
        {
            AddParty(players);
            AddParty(allies);
            AddParty(enemies);
        }
        else
        {
            // Specific groups
            // Check for Ally/Player inclusion
            if (_currentTargetingMode.HasFlag(TargetingType.AnyAlly) || _currentTargetingMode.HasFlag(TargetingType.AnyAllyParty))
            {
                AddParty(players);
                AddParty(allies);
            }
            
            // Check for Self/OwnParty specific logic
            if (_currentTargetingMode == TargetingType.Self)
            {
                _validParties.Clear(); // Exclusive
                AddParty(new List<Node> { _actor });
            }
            else if (_currentTargetingMode == TargetingType.OwnParty)
            {
                _validParties.Clear(); // Exclusive
                if (players.Contains(_actor)) AddParty(players);
                else if (allies.Contains(_actor)) AddParty(allies);
                else if (enemies.Contains(_actor)) AddParty(enemies);
            }
            
            // Check for Enemy inclusion
            if (_currentTargetingMode.HasFlag(TargetingType.AnyEnemy) || _currentTargetingMode.HasFlag(TargetingType.AnyEnemyParty))
            {
                AddParty(enemies);
            }
        }

        // Attempt to restore selection to the previous target
        bool restored = false;
        if (previousTarget != null)
        {
            for (int i = 0; i < _validParties.Count; i++)
            {
                int index = _validParties[i].IndexOf(previousTarget);
                if (index != -1)
                {
                    _currentPartyIndex = i;
                    _currentTargetIndex = index;
                    restored = true;
                    break;
                }
            }
        }

        // If we couldn't restore (or didn't have a previous target), clamp indices to valid range
        if (!restored)
        {
            if (_validParties.Count == 0)
            {
                _currentPartyIndex = 0;
                _currentTargetIndex = 0;
            }
            else
            {
                if (_currentPartyIndex >= _validParties.Count) _currentPartyIndex = 0;
                if (_currentPartyIndex < 0) _currentPartyIndex = 0;
                
                var party = _validParties[_currentPartyIndex];
                if (_currentTargetIndex >= party.Count) _currentTargetIndex = 0;
                if (_currentTargetIndex < 0) _currentTargetIndex = 0;
            }
        }
    }

    private void SelectDefaultTarget()
    {
        if (_validParties.Count == 0) return;

        if (ShouldPreferAllies(_currentAction))
        {
            // Defensive: Prioritize Self -> Ally -> Enemy
            if (TrySelectSelf()) return;
            if (TrySelectAlly()) return;
            if (TrySelectEnemy()) return;
        }
        else
        {
            // Aggressive: Prioritize Enemy -> Self -> Ally
            if (TrySelectEnemy()) return;
            if (TrySelectSelf()) return;
            if (TrySelectAlly()) return;
        }
        
        // Fallback
        _currentPartyIndex = 0;
        _currentTargetIndex = 0;
    }

    private static bool ShouldPreferAllies(ActionData action)
    {
        if (action == null) return false;
        if (action.Intent == ActionIntent.Defensive) return true;

        return action.Category == ActionCategory.Heal
            || action.Category == ActionCategory.Support
            || action.Category == ActionCategory.Defensive;
    }

    private bool TrySelectSelf()
    {
        for (int i = 0; i < _validParties.Count; i++)
        {
            int index = _validParties[i].IndexOf(_actor);
            if (index != -1)
            {
                _currentPartyIndex = i;
                _currentTargetIndex = index;
                return true;
            }
        }
        return false;
    }

    private bool TrySelectEnemy()
    {
        var enemies = _enemyTeamContainer.GetChildren().Cast<Node>().ToList();
        return TrySelectGroup(enemies);
    }

    private bool TrySelectAlly()
    {
        var allies = _playerTeamContainer.GetChildren().Concat(_allyTeamContainer.GetChildren()).Cast<Node>().ToList();
        return TrySelectGroup(allies);
    }

    private bool TrySelectGroup(List<Node> groupMembers)
    {
        for (int i = 0; i < _validParties.Count; i++)
        {
            // Check if the party contains members of the target group
            if (_validParties[i].Count > 0 && _validParties[i].Any(m => groupMembers.Contains(m)))
            {
                _currentPartyIndex = i;
                _currentTargetIndex = 0;
                return true;
            }
        }
        return false;
    }

    private void UpdateVisuals()
    {
        var currentlyTargeted = GetCurrentSelection();

        // 1. Update 3D Indicators
        UpdateIndicators(currentlyTargeted);

        // 2. Update UI Highlight
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        var targetsArray = new Godot.Collections.Array<Node>(currentlyTargeted);
        eventBus.EmitSignal(GlobalEventBus.SignalName.TargetSelectionChanged, targetsArray);

        // 3. Emit Preview Event
        var preview = BuildActionPreview(currentlyTargeted);
        eventBus.EmitSignal(GlobalEventBus.SignalName.ActionPreviewRequested, preview, _actor); 
    }

    private TurnManager.ActionPreview BuildActionPreview(List<Node> selectedTargets)
    {
        var preview = new TurnManager.ActionPreview();
        if (_currentAction == null || _actor == null)
        {
            return preview;
        }

        ItemData sourceItem = (_currentCommand as ItemBattleCommand)?.Item;
        var rewrittenTargets = _battleController?.RewriteTargetsFromStatusRules(_actor, _currentAction, sourceItem, selectedTargets) ?? selectedTargets;
        rewrittenTargets ??= new List<Node>();

        var context = new ActionContext(_currentAction, _actor, rewrittenTargets, sourceItem);
        ApplyPreviewInitiationModifiers(context);

        float resolvedTickCost = _currentAction.TickCost + context.TickCostAdjustment;
        resolvedTickCost = Mathf.Max(-TurnManager.TickThreshold + 1f, resolvedTickCost);
        preview.ActionTickCost = resolvedTickCost;

        AddGuaranteedAppliedStatuses(preview, rewrittenTargets, context);
        return preview;
    }

    private void ApplyPreviewInitiationModifiers(ActionContext context)
    {
        if (context == null || _actor == null) return;

        // Mirror the initiation phase from BattleMechanics for turn-cost/status preview accuracy.
        foreach (var modifier in GetOrderedActionModifiersFrom(_actor))
        {
            modifier.OnActionInitiated(context, _actor);
        }

        IEnumerable<Node> allies = _battleController?.GetAllies(_actor) ?? Enumerable.Empty<Node>();
        foreach (var entry in GetOrderedActionModifiersFromMany(allies.Where(a => a != null && a != _actor)))
        {
            entry.Modifier.OnAllyActionInitiated(context, _actor, entry.Owner);
        }
    }

    private void AddGuaranteedAppliedStatuses(TurnManager.ActionPreview preview, List<Node> targets, ActionContext context)
    {
        if (preview == null || targets == null || context == null) return;

        var entries = new List<StatusEffectChanceEntry>();
        if (_currentAction?.StatusEffectsOnHit != null)
        {
            entries.AddRange(_currentAction.StatusEffectsOnHit);
        }
        if (context.ExtraStatusEffectsOnHit != null && context.ExtraStatusEffectsOnHit.Count > 0)
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

    private struct ModifierOwnerEntry
    {
        public Node Owner;
        public IActionModifier Modifier;
    }

    private IEnumerable<IActionModifier> GetOrderedActionModifiersFrom(Node owner)
    {
        return GetActionModifiersFrom(owner)
            .OrderByDescending(GetModifierPriority)
            .ToList();
    }

    private IEnumerable<ModifierOwnerEntry> GetOrderedActionModifiersFromMany(IEnumerable<Node> owners)
    {
        if (owners == null) return Enumerable.Empty<ModifierOwnerEntry>();

        var entries = new List<ModifierOwnerEntry>();
        foreach (var owner in owners)
        {
            if (owner == null) continue;
            foreach (var modifier in GetActionModifiersFrom(owner))
            {
                entries.Add(new ModifierOwnerEntry { Owner = owner, Modifier = modifier });
            }
        }

        return entries
            .OrderByDescending(entry => GetModifierPriority(entry.Modifier))
            .ToList();
    }

    private IEnumerable<IActionModifier> GetActionModifiersFrom(Node owner)
    {
        if (owner == null) return Enumerable.Empty<IActionModifier>();

        var modifiers = new List<IActionModifier>();
        modifiers.AddRange(owner.FindChildren("*", recursive: true).OfType<IActionModifier>());

        var statusManager = owner.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (statusManager != null)
        {
            modifiers.AddRange(statusManager.GetActionModifiers().OfType<IActionModifier>());
        }

        var abilityManager = owner.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);
        if (abilityManager != null)
        {
            modifiers.AddRange(abilityManager.GetActionModifiers());
        }

        return modifiers;
    }

    private static int GetModifierPriority(IActionModifier modifier)
    {
        return modifier is IPrioritizedModifier prioritized ? prioritized.Priority : 0;
    }

    private void UpdateIndicators(List<Node> currentlyTargeted)
    {
        // Identify indicators to remove (targets that are no longer selected)
        var toRemove = new List<Node>();
        foreach (var kvp in _activeIndicators)
        {
            // Safety check: If the target (Key) has been freed, mark for removal immediately.
            if (!GodotObject.IsInstanceValid(kvp.Key))
            {
                toRemove.Add(kvp.Key);
                if (GodotObject.IsInstanceValid(kvp.Value)) kvp.Value.QueueFree();
                continue;
            }

            if (!currentlyTargeted.Contains(kvp.Key))
            {
                if (GodotObject.IsInstanceValid(kvp.Value))
                {
                    kvp.Value.QueueFree();
                }
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var node in toRemove)
        {
            _activeIndicators.Remove(node);
        }

        // Add indicators for new targets
        foreach (var target in currentlyTargeted)
        {
            if (!_activeIndicators.ContainsKey(target))
            {
                Node3D indicator = _targetIndicatorScene != null 
                    ? _targetIndicatorScene.Instantiate<Node3D>() 
                    : new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.5f, 0.5f) } };
                
                indicator.Name = "TargetIndicator";
                // Calculate dynamic height based on the target's sprite/mesh
                float targetHeight = GetTargetHeight(target);
                indicator.Position = new Vector3(0, targetHeight + 0.5f, 0); 
                
                // Parent to target so it moves with idle animations
                target.AddChild(indicator);
                
                _activeIndicators[target] = indicator;
            }
        }
    }

    private float GetTargetHeight(Node target)
    {
        float maxHeight = 0f;
        bool foundVisual = false;

        foreach (var child in target.GetChildren())
        {
            // Check for 3D Sprites (Sprite3D, AnimatedSprite3D) or Meshes
            if (child is GeometryInstance3D visual && visual.Visible)
            {
                // GetAabb() returns the local bounds.
                var aabb = visual.GetAabb();
                // Calculate top Y in parent space (Position.Y + Top of AABB * Scale)
                float topY = visual.Position.Y + (aabb.End.Y * visual.Scale.Y);
                
                if (topY > maxHeight)
                {
                    maxHeight = topY;
                    foundVisual = true;
                }
            }
            // Fallback to collision shape if no visuals found yet
            else if (!foundVisual && child is CollisionShape3D col)
            {
                float topY = 0f;
                if (col.Shape is CapsuleShape3D capsule) topY = col.Position.Y + (capsule.Height / 2f);
                else if (col.Shape is BoxShape3D box) topY = col.Position.Y + (box.Size.Y / 2f);
                else if (col.Shape is CylinderShape3D cylinder) topY = col.Position.Y + (cylinder.Height / 2f);

                if (topY > maxHeight) maxHeight = topY;
            }
        }

        return maxHeight > 0 ? maxHeight : 2.0f; // Default to 2.0 if nothing found
    }

    private List<Node> GetCurrentSelection()
    {
        if (_validParties.Count == 0) return new List<Node>();

        if (_currentTargetingMode == TargetingType.All)
        {
            // Flatten all parties
            return _validParties.SelectMany(x => x).ToList();
        }
        else if (IsSingleTargetMode(_currentTargetingMode))
        {
            return new List<Node> { _validParties[_currentPartyIndex][_currentTargetIndex] };
        }
        else
        {
            // Party mode
            return new List<Node>(_validParties[_currentPartyIndex]);
        }
    }

    private void ConfirmTargeting()
    {
        UISoundManager.Instance?.Play(UISoundType.Confirm);
        var targets = new Godot.Collections.Array<Node>(GetCurrentSelection());
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        eventBus.EmitSignal(GlobalEventBus.SignalName.TargetingConfirmed, _currentCommand ?? _currentAction, targets);
        
        Cleanup();
    }

    private void CancelTargeting()
    {
        UISoundManager.Instance?.Play(UISoundType.Cancel);
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        eventBus.EmitSignal(GlobalEventBus.SignalName.ActionPreviewCancelled);
        eventBus.EmitSignal(GlobalEventBus.SignalName.TargetingCancelled);
        
        Cleanup();
    }

    private void AbortTargeting()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        _isActive = false;
        SetProcess(false);
        
        // Clear all indicators
        foreach (var kvp in _activeIndicators)
        {
            if (GodotObject.IsInstanceValid(kvp.Value)) kvp.Value.QueueFree();
        }
        _activeIndicators.Clear();
        
        var eventBus = GetNode<GlobalEventBus>(GlobalEventBus.Path);
        eventBus.EmitSignal(GlobalEventBus.SignalName.TargetSelectionChanged, new Godot.Collections.Array<Node>());
    }

    private bool IsBattleActive()
    {
        return _battleController != null && _battleController.CurrentState == BattleController.BattleState.InProgress;
    }

    private bool IsSingleTargetMode(TargetingType mode)
    {
        return mode == TargetingType.AnySingleTarget || 
               mode == TargetingType.AnyEnemy || 
               mode == TargetingType.AnyAlly || 
               mode == TargetingType.Self;
    }

    private bool IsPartyTargetMode(TargetingType mode)
    {
        return mode == TargetingType.AnySingleParty || 
               mode == TargetingType.AnyEnemyParty || 
               mode == TargetingType.AnyAllyParty || 
               mode == TargetingType.OwnParty ||
               mode == TargetingType.All;
    }
}
