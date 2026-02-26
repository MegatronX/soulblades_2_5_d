using Godot;

/// <summary>
/// A component that defines an item as "equippable" and holds the effects
/// it grants to the wielder.
/// </summary>
[GlobalClass]
public partial class EquippableComponentData : ItemComponentData
{
    private const int MinWeaponRating = 10;
    private const int MaxWeaponRating = 255;

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

    [ExportGroup("Weapon Ratings")]
    [Export(PropertyHint.Range, "0,255,1")]
    public int PhysicalAttackRating { get; private set; } = 0;

    [Export(PropertyHint.Range, "0,255,1")]
    public int MagicalAttackRating { get; private set; } = 0;

    [Export(PropertyHint.ResourceType, "WeaponPowerFormula")]
    public WeaponPowerFormula PhysicalAttackRatingFormula { get; private set; }

    [Export(PropertyHint.ResourceType, "WeaponPowerFormula")]
    public WeaponPowerFormula MagicalAttackRatingFormula { get; private set; }

    [Export]
    public bool UsePhysicalRatingAsMagicFallback { get; private set; } = true;

    [ExportGroup("Command Override")]
    [Export(PropertyHint.ResourceType, "BattleCommand")]
    public BattleCommand AttackCommandOverride { get; private set; }

    public bool IsWeapon => SlotType == EquipmentSlotType.Weapon;

    public void ResolveWeaponAttackRatings(
        Node holder,
        ActionContext actionContext,
        Node target,
        BattleMechanics battleMechanics,
        out int physicalRating,
        out int magicalRating)
    {
        physicalRating = 0;
        magicalRating = 0;
        if (!IsWeapon) return;

        var formulaContext = WeaponPowerContext.Create(holder, actionContext, target, battleMechanics);

        physicalRating = ResolveSingleWeaponRating(
            formulaContext,
            PhysicalAttackRating,
            PhysicalAttackRatingFormula);

        magicalRating = ResolveSingleWeaponRating(
            formulaContext,
            MagicalAttackRating,
            MagicalAttackRatingFormula);

        if (magicalRating <= 0 && UsePhysicalRatingAsMagicFallback)
        {
            magicalRating = physicalRating;
        }
    }

    private static int ResolveSingleWeaponRating(
        WeaponPowerContext context,
        int fixedRating,
        WeaponPowerFormula formula)
    {
        int clampedFixed = fixedRating > 0
            ? Mathf.Clamp(fixedRating, MinWeaponRating, MaxWeaponRating)
            : 0;

        if (formula == null)
        {
            return clampedFixed;
        }

        int fallback = clampedFixed > 0 ? clampedFixed : MinWeaponRating;
        return formula.EvaluatePower(context, fallback);
    }

    // You could also add:
    // [Export] public Godot.Collections.Array<StatusEffect> AutoStatusEffects { get; private set; }
    // [Export] public ElementType AttackElementOverride { get; private set; }
}
