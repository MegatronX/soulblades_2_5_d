using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A component that manages a character's known and equipped abilities.
/// It handles the AP cost and applies/removes the effects of abilities.
/// </summary>
public partial class AbilityManager : Node
{
    public const string NodeName = "AbilityManager";

    private readonly List<Ability> _knownAbilities = new();
    private readonly List<Ability> _equippedAbilities = new();

    [Signal]
    public delegate void KnownAbilitiesChangedEventHandler();

    [Signal]
    public delegate void EquippedAbilitiesChangedEventHandler();

    [Signal]
    public delegate void ApExperienceChangedEventHandler(int newValue);

    /// <summary>
    /// Emitted when equipped ability effects trigger runtime hooks.
    /// </summary>
    public event System.Action<BattleHookEvent> HookEventRaised;

    private Node _owner;
    private int _currentApExperience = 0;

    public override void _Ready()
    {
        _owner = GetParent();
    }

    public int GetCurrentApCost()
    {
        return _equippedAbilities.Sum(ability => ability.ApCost);
    }

    public int GetCurrentApExperience() => _currentApExperience;

    public void AddApExperience(int amount)
    {
        if (amount <= 0) return;
        _currentApExperience += amount;
        EmitSignal(SignalName.ApExperienceChanged, _currentApExperience);
    }

    // Public methods for the UI to get data
    public IReadOnlyList<Ability> GetKnownAbilities() => _knownAbilities;
    public IReadOnlyList<Ability> GetEquippedAbilities() => _equippedAbilities;

    public IEnumerable<IActionModifier> GetActionModifiers()
    {
        foreach (var ability in _equippedAbilities)
        {
            if (ability?.TriggeredEffects == null) continue;
            foreach (var effect in ability.TriggeredEffects)
            {
                if (effect is IActionModifier modifier)
                {
                    yield return modifier;
                }
            }
        }
    }

    /// <summary>
    /// Adds an ability to the character's list of known abilities.
    /// This would be called when learning an ability from equipment.
    /// </summary>
    public bool LearnAbility(Ability ability)
    {
        if (!_knownAbilities.Contains(ability))
        {
            _knownAbilities.Add(ability);
            EmitSignal(SignalName.KnownAbilitiesChanged);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Equips an ability if the character knows it, can afford the AP cost,
    /// and respects stacking rules.
    /// </summary>
    /// <returns>True if the ability was successfully equipped.</returns>
    public bool EquipAbility(Ability abilityToEquip)
    {
        // Prerequisite checks
        if (!_knownAbilities.Contains(abilityToEquip)) return false;
        if (!abilityToEquip.IsStackable && _equippedAbilities.Contains(abilityToEquip)) return false;

        // AP Cost Check (assuming AP is a stat)
        var stats = _owner.GetNode<StatsComponent>(StatsComponent.NodeName);
        if (stats != null)
        {
            int maxAp = (int)stats.GetStatValue(StatType.AP);
            if (GetCurrentApCost() + abilityToEquip.ApCost > maxAp)
            {
                return false;
            }
        }

        _equippedAbilities.Add(abilityToEquip);
        if (abilityToEquip.Effects != null)
        {
            foreach (var logic in abilityToEquip.Effects)
            {
                logic?.OnApply(_owner);
            }
        }
        EmitSignal(SignalName.EquippedAbilitiesChanged);
        return true;
    }

    /// <summary>
    /// Unequips an instance of an ability.
    /// </summary>
    public void UnequipAbility(Ability abilityToUnequip)
    {
        if (_equippedAbilities.Contains(abilityToUnequip))
        {
            _equippedAbilities.Remove(abilityToUnequip);
            if (abilityToUnequip.Effects != null)
            {
                foreach (var logic in abilityToUnequip.Effects)
                {
                    // Note: This correctly removes modifiers sourced by the EffectLogic instance.
                    logic?.OnRemove(_owner);
                }
            }
            EmitSignal(SignalName.EquippedAbilitiesChanged);
        }
    }

    /// <summary>
    /// Dispatches a trigger to all equipped ability effects that match it.
    /// </summary>
    public void ApplyTrigger(AbilityTrigger trigger, AbilityEffectContext context = null)
    {
        if (_equippedAbilities.Count == 0) return;

        context ??= new AbilityEffectContext(_owner, trigger);
        context.Ability = null;

        foreach (var ability in _equippedAbilities)
        {
            if (ability == null) continue;
            if (ability.TriggeredEffects == null || ability.TriggeredEffects.Count == 0) continue;

            context.Ability = ability;
            foreach (var effect in ability.TriggeredEffects)
            {
                if (effect == null || !effect.Matches(trigger)) continue;
                context.WasTriggered = false;
                effect.Apply(context);
                TryPlayTriggerVfx(effect, context);
                RaiseAbilityHookEvent(
                    BattleHookEventType.AbilityTriggered,
                    ability,
                    effect,
                    context,
                    context.ActionContext,
                    context.ActionResult);
            }
        }
    }

    private void TryPlayTriggerVfx(AbilityEffect effect, AbilityEffectContext context)
    {
        if (effect == null || context == null) return;
        if (effect.TriggerVfx == null) return;
        if (effect.RequireExplicitTrigger && !context.WasTriggered) return;

        var owner = context.Owner;
        if (owner == null) return;

        var eventBus = owner.GetNodeOrNull<GlobalEventBus>(GlobalEventBus.Path);
        if (eventBus == null) return;

        eventBus.EmitSignal(GlobalEventBus.SignalName.EffectVfxRequested, effect.TriggerVfx, owner, effect.TriggerVfxOffset, effect.AttachTriggerVfxToOwner);
    }

    private void RaiseAbilityHookEvent(
        BattleHookEventType eventType,
        Ability ability,
        AbilityEffect abilityEffect,
        AbilityEffectContext abilityContext,
        ActionContext actionContext = null,
        ActionResult actionResult = null,
        Node relatedNode = null)
    {
        if (_owner == null || abilityEffect == null) return;

        HookEventRaised?.Invoke(new BattleHookEvent
        {
            EventType = eventType,
            Owner = _owner,
            RelatedNode = relatedNode,
            ActionContext = actionContext,
            ActionResult = actionResult,
            Modifier = abilityEffect,
            Ability = ability,
            AbilityEffect = abilityEffect,
            AbilityContext = abilityContext
        });
    }
}
