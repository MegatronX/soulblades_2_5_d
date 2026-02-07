using Godot;
using System.Collections.Generic;
/*
[TestFixture]
public class StatsComponentTests
{
    private StatsComponent _statsComponent;

    [SetUp]
    public void Setup()
    {
        // For each test, create a fresh instance of the StatsComponent.
        _statsComponent = new StatsComponent();

        // Manually provide the base stats, simulating what Godot's [Export] would do.
        var baseStatsResource = new BaseStats
        {
            HP = 100,
            Strength = 10
        };

        // We use reflection to set the private `_baseStatsResource` field for testing purposes.
        var field = typeof(StatsComponent).GetField("_baseStatsResource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(_statsComponent, baseStatsResource);

        // Manually call the _Ready method to trigger initialization.
        _statsComponent.GetType().GetMethod("InitializeStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_statsComponent, null);
    }

    [Test]
    public void InitializesWithBaseStats()
    {
        Assert.AreEqual(100, _statsComponent.GetStatValue(StatType.HP));
        Assert.AreEqual(10, _statsComponent.GetStatValue(StatType.Strength));
        Assert.AreEqual(100, _statsComponent.CurrentHP);
    }

    [Test]
    public void AddsAdditiveModifierCorrectly()
    {
        var source = new object();
        var modifier = new StatModifier(5, StatModType.Additive, source);

        _statsComponent.AddModifier(modifier);

        Assert.AreEqual(15, _statsComponent.GetStatValue(StatType.Strength));
    }

    [Test]
    public void AddsMultiplicativeModifierCorrectly()
    {
        var source = new object();
        // e.g., a "Haste" buff that increases speed by 50%
        var modifier = new StatModifier(1.5f, StatModType.Multiplicative, source);

        _statsComponent.AddModifier(modifier);
        
        // Strength base is 10. 10 * 1.5 = 15
        Assert.AreEqual(15, _statsComponent.GetStatValue(StatType.Strength));
    }

    [Test]
    public void CombinesModifiersCorrectly()
    {
        // Additive modifier from a sword
        var sword = new object();
        _statsComponent.AddModifier(new StatModifier(5, StatModType.Additive, sword));

        // Multiplicative modifier from a "Frenzy" buff
        var frenzyBuff = new object();
        _statsComponent.AddModifier(new StatModifier(2.0f, StatModType.Multiplicative, frenzyBuff));

        // Calculation should be: (Base + Additive) * Multiplicative
        // (10 + 5) * 2.0 = 30.
        Assert.AreEqual(30, _statsComponent.GetStatValue(StatType.Strength));
    }

    [Test]
    public void RemovesModifiersBySource()
    {
        var sword = new object();
        _statsComponent.AddModifier(new StatModifier(5, StatModType.Additive, sword));

        var shield = new object();
        _statsComponent.AddModifier(new StatModifier(2, StatModType.Additive, shield));

        // Base 10 + 5 (sword) + 2 (shield) = 17
        Assert.AreEqual(17, _statsComponent.GetStatValue(StatType.Strength));

        // "Unequip" the sword
        _statsComponent.RemoveAllModifiersFromSource(sword);

        // Base 10 + 2 (shield) = 12
        Assert.AreEqual(12, _statsComponent.GetStatValue(StatType.Strength));
    }

    [Test]
    public void PredictsStatValueWithoutApplying()
    {
        var hypotheticalRing = new object();
        var ringModifier = new StatModifier(20, StatModType.Additive, hypotheticalRing);

        int predictedValue = _statsComponent.PredictStatValue(StatType.Strength, ringModifier);

        // Prediction should be Base 10 + 20 = 30
        Assert.AreEqual(30, predictedValue);

        // The actual stat value should remain unchanged.
        Assert.AreEqual(10, _statsComponent.GetStatValue(StatType.Strength));
    }

    [Test]
    public void ClampsCurrentHPOnDamage()
    {
        // Deal 120 damage to a character with 100 HP
        _statsComponent.ModifyCurrentHP(-120);
        Assert.AreEqual(0, _statsComponent.CurrentHP);
    }

    [Test]
    public void ClampsCurrentHPOnHeal()
    {
        // Deal 20 damage, then heal for 50
        _statsComponent.ModifyCurrentHP(-20); // HP is now 80
        _statsComponent.ModifyCurrentHP(50);  // Should heal to 100, not 130

        Assert.AreEqual(100, _statsComponent.CurrentHP);
    }

    [Test]
    public void ClampsCurrentHPOnMaxHPChange()
    {
        // Apply a debuff that reduces Max HP by 50%
        var curse = new object();
        _statsComponent.AddModifier(new StatModifier(0.5f, StatModType.Multiplicative, curse));

        // Force a recalculation
        _statsComponent.GetStatValue(StatType.HP);

        // Max HP is now 50. CurrentHP should be clamped down from 100 to 50.
        Assert.AreEqual(50, _statsComponent.GetStatValue(StatType.HP));
        Assert.AreEqual(50, _statsComponent.CurrentHP);
    }
}*/