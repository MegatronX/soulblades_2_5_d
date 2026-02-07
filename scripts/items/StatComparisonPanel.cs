using Godot;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

/// <summary>
/// A UI component that displays the difference in stats between a character's
/// current equipment and a potential new item.
/// </summary>
[GlobalClass]
public partial class StatComparisonPanel : PanelContainer
{
    [Export]
    private VBoxContainer _statsContainer;

    private StatsComponent _characterStats;

    public void LinkToCharacter(Node character)
    {
        _characterStats = character.GetNode<StatsComponent>(StatsComponent.NodeName);
    }

    /// <summary>
    /// Updates the display to show the comparison between two items.
    /// </summary>
    public void UpdatePreview(ItemData currentItem, ItemData previewItem, bool currentItemAlreadyEquipped, bool previewItemAlreadyEquipped)
    {
        // 1. Clear out any old stat preview rows.
        RemoveAllChildren(_statsContainer);

        if (_characterStats == null) return;

        // Get the components for the current and previewed items.
        var currentEquippable = currentItem?.Components?.OfType<EquippableComponentData>().FirstOrDefault();
        var previewEquippable = previewItem?.Components?.OfType<EquippableComponentData>().FirstOrDefault();

        // 2. Iterate through ALL stats defined in the StatType enum to ensure a consistent order.
        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
        {
            float baseStat = _characterStats.GetStatValue(statType);

            // Calculate the final value with the CURRENT item.
            float currentValue = currentItemAlreadyEquipped ? baseStat : CalculateFinalStat(baseStat, currentEquippable, statType);

            // Calculate the final value with the PREVIEW item.
            var baseStatWithoutEquipedItem = currentItemAlreadyEquipped 
            ? _characterStats.GetStatValueWithoutModifiersFromSource(statType, currentItem): baseStat;
            float previewValue = previewItemAlreadyEquipped ? baseStat : CalculateFinalStat(baseStatWithoutEquipedItem, previewEquippable, statType);

            // If there's no change, don't create a row for this stat.
           // if (Mathf.IsEqualApprox(currentValue, previewValue)) continue;

            CreateStatRow(statType, (int)currentValue, (int)previewValue);
        }
    }

    private float CalculateFinalStat(float baseValue, EquippableComponentData newItem, StatType statToCalculate)
    {
        if (newItem == null) return baseValue;

        float additiveBonus = 0;
        float multiplicativeBonus = 1.0f;

        foreach (var mod in newItem.StatBoosts.Where(m => m.StatToModify == statToCalculate))
        {
            if (mod.Type == ModifierType.Additive) additiveBonus += mod.Value;
            else if (mod.Type == ModifierType.Multiplicative) multiplicativeBonus *= mod.Value;
        }

        // Standard calculation order: (Base + Additive) * Multiplicative
        return (baseValue + additiveBonus) * multiplicativeBonus;
    }

    private void CreateStatRow(StatType statType, int oldValue, int newValue)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

        var statNameLabel = new Label
        {
            Text = statType.ToString(),
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        var oldValueLabel = new Label
        {
            Text = oldValue.ToString(),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var arrowLabel = new Label { Text = " -> ", HorizontalAlignment = HorizontalAlignment.Center };

        var newValueLabel = new Label
        {
            Text = newValue.ToString(),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // --- Color Coding Logic ---
        float difference = newValue - oldValue;
        if (difference != 0)
        {
            // Avoid division by zero if the old value was 0.
            float percentChange = oldValue == 0 ? 1.0f : difference / oldValue;
            newValueLabel.Modulate = GetColorForChange(percentChange);
        }
        // --------------------------

        row.AddChild(statNameLabel);
        row.AddChild(oldValueLabel);
        row.AddChild(arrowLabel);
        row.AddChild(newValueLabel);

        _statsContainer.AddChild(row);
    }

    private Color GetColorForChange(float percentChange)
    {
        // Define our base colors.
        var increaseColor = new Color(0.6f, 0.8f, 1.0f); // Light Blue
        var decreaseColor = new Color(1.0f, 0.6f, 0.6f); // Light Red

        // Clamp the interpolation factor to a max of 50% change for full color intensity.
        float lerpFactor = Mathf.Min(Mathf.Abs(percentChange) / 0.5f, 1.0f);

        return percentChange > 0
            ? Colors.White.Lerp(increaseColor, lerpFactor)
            : Colors.White.Lerp(decreaseColor, lerpFactor);
    }

    private void RemoveAllChildren(Node parentNode)
    {
        foreach (var child in parentNode.GetChildren())
        {
            child.QueueFree();
        }
    }
}