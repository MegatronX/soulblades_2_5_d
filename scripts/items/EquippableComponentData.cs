using Godot;

/// <summary>
/// A component that defines an item as "equippable" and holds the effects
/// it grants to the wielder.
/// </summary>
[GlobalClass]
public partial class EquippableComponentData : ItemComponentData
{
    [Export]
    public EquipmentSlotType SlotType { get; set; }

    /// <summary>
    /// A list of direct stat modifications this item provides.
    /// </summary>
    [Export(PropertyHint.ResourceType, "StatModifier")]
    public Godot.Collections.Array<StatModifier> StatBoosts { get; private set; } = new();

    /// <summary>
    /// A list of abilities this item grants to the wielder.
    /// </summary>
    [Export(PropertyHint.ResourceType, "AbilityData")]
    public Godot.Collections.Array<AbilityData> GrantedAbilities { get; private set; } = new();

    // You could also add:
    // [Export] public Godot.Collections.Array<StatusEffect> AutoStatusEffects { get; private set; }
    // [Export] public ElementType AttackElementOverride { get; private set; }
}