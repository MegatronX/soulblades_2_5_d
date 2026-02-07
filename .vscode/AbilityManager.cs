using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A component that manages a character's known and equipped abilities.
/// It handles the AP cost and applies/removes the effects of abilities.
/// </summary>
public partial class AbilityManager : Node
{
    private readonly List<Ability> _knownAbilities = new();
    private readonly List<Ability> _equippedAbilities = new();

    private Node _owner;

    public override void _Ready()
    {
        _owner = GetParent();
    }

    public int GetCurrentApCost()
    {
        return _equippedAbilities.Sum(ability => ability.ApCost);
    }

    /// <summary>
    /// Adds an ability to the character's list of known abilities.
    /// This would be called when learning an ability from equipment.
    /// </summary>
    public void LearnAbility(Ability ability)
    {
        if (!_knownAbilities.Contains(ability))
        {
            _knownAbilities.Add(ability);
        }
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
        var stats = _owner.GetNode<StatsComponent>("StatsComponent");
        if (stats != null)
        {
            int maxAp = (int)stats.GetStatValue(StatType.AP);
            if (GetCurrentApCost() + abilityToEquip.ApCost > maxAp)
            {
                return false;
            }
        }

        _equippedAbilities.Add(abilityToEquip);
        foreach (var logic in abilityToEquip.Effects)
        {
            logic.OnApply(_owner);
        }
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
            foreach (var logic in abilityToUnequip.Effects)
            {
                // Note: This correctly removes modifiers sourced by the EffectLogic instance.
                logic.OnRemove(_owner);
            }
        }
    }
}