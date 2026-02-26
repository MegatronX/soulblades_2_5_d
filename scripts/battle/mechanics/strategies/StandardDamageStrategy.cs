using Godot;

[GlobalClass]
public partial class StandardDamageStrategy : DamageStrategy
{
    public override int Calculate(ActionContext context, Node target, ActionResult result, IRandomNumberGenerator rng)
    {
        var damageComp = context.GetComponent<DamageComponent>();
        if (damageComp == null) return 0;

        if (context.SourceAction.Flags.HasFlag(ActionFlags.FixedDamage))
        {
            int fixedValue = damageComp.Power;
            if (context.SourceAction.Category == ActionCategory.Heal && fixedValue > 0)
            {
                fixedValue = -fixedValue;
            }
            return fixedValue;
        }

        // Retrieve stats
        var attackerStats = context.Initiator.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        var targetStats = target.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);

        if (attackerStats == null || targetStats == null) return damageComp.Power;

        // Simple Formula: (Atk * Power) / Def
        // Use PhysicalRatio to blend Atk/Mag and Def/Res
        float physRatio = context.SourceAction.PhysicalRatio;
        
        float atk = (attackerStats.GetStatValue(StatType.Strength) * physRatio) +
                    (attackerStats.GetStatValue(StatType.Magic) * (1 - physRatio));

        float weaponAtk = ResolveWeaponAttackBlend(context, target, physRatio);
        atk += weaponAtk;
        
        float def = (targetStats.GetStatValue(StatType.Defense) * physRatio) + 
                    (targetStats.GetStatValue(StatType.MagicDefense) * (1 - physRatio));

        if (def <= 0) def = 1;

        float rawDamage = (atk * damageComp.Power) / def;

        // Apply Crit
        if (result.IsCritical) rawDamage *= 1.5f;

        // Apply Accumulated Timed Hit Bonus
        rawDamage *= context.TimedHitMultiplier;

        // Apply runtime scalar from statuses/reactions (e.g. Focused, Echo Cast).
        rawDamage *= context.ActionPowerScalar;

        return Mathf.Max(1, Mathf.RoundToInt(rawDamage));
    }

    private static float ResolveWeaponAttackBlend(ActionContext context, Node target, float physicalRatio)
    {
        if (context == null) return 0f;
        if (context.SourceItem != null) return 0f; // Item actions should not inherit weapon power by default.

        var initiator = context.Initiator;
        if (initiator == null) return 0f;

        var equipment = initiator.GetNodeOrNull<EquipmentManager>(EquipmentManager.DefaultName)
            ?? initiator.GetNodeOrNull<EquipmentManager>("EquipmentManager");
        if (equipment == null) return 0f;

        var weapon = equipment.GetEquippedEquippable(EquipmentSlotType.Weapon);
        if (weapon == null || !weapon.IsWeapon) return 0f;

        weapon.ResolveWeaponAttackRatings(
            initiator,
            context,
            target,
            context.BattleMechanics,
            out int physicalRating,
            out int magicalRating);

        return (physicalRating * physicalRatio) + (magicalRating * (1f - physicalRatio));
    }
}
