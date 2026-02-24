using Godot;
using System.Collections.Generic;

/// <summary>
/// Base class for the runtime version of an action component.
/// These are attached to an ActionContext and contain mutable properties.
/// </summary>
[GlobalClass]
public partial class ActionComponent : RefCounted
{
    public ActionComponentData SourceData { get; protected set; }

    public virtual ActionComponent DeepCopy()
    {
        // Base implementation just creates a new component with the same source.
        return CreateRuntimeComponent(SourceData);
    }

    /// <summary>
    /// Factory method to create the correct runtime component from its data resource.
    /// </summary>
    public static ActionComponent CreateRuntimeComponent(ActionComponentData data)
    {
        if (data is DamageComponentData damageData)
        {
            return new DamageComponent(damageData);
        }
        if (data is ChainActionComponentData chainData)
        {
            return new ChainActionComponent(chainData);
        }
        if (data is OverflowCostComponentData overflowCostData)
        {
            return new OverflowCostComponent(overflowCostData);
        }
        // Add other component types here...
        
        // Fallback for components without a specific runtime version.
        var genericComponent = new ActionComponent { SourceData = data };
        return genericComponent;
    }
}

/// <summary>
/// The runtime version of a DamageComponent, with mutable properties.
/// </summary>
public partial class DamageComponent : ActionComponent
{
    public int Power { get; set; }
    public int Accuracy { get; set; }
    public Dictionary<ElementType, float> ElementalWeights { get; set; } = new();

    public DamageComponent(DamageComponentData data)
    {
        if (data != null)
        {
            SourceData = data;
            Power = data.Power;
            Accuracy = data.Accuracy;
            
            if (data.ElementalWeights != null)
            {
                foreach (var kvp in data.ElementalWeights)
                {
                    ElementalWeights[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    public override ActionComponent DeepCopy()
    {
        // Create a new instance and copy current runtime values
        var copy = new DamageComponent(SourceData as DamageComponentData);
        copy.Power = Power;
        copy.Accuracy = Accuracy;
        copy.ElementalWeights = new Dictionary<ElementType, float>(ElementalWeights);
        return copy;
    }
}

/// <summary>
/// The runtime version of a ChainActionComponent.
/// </summary>
public partial class ChainActionComponent : ActionComponent
{
    public ChainActionComponent(ChainActionComponentData data)
    {
        SourceData = data;
    }

    public override ActionComponent DeepCopy() => new ChainActionComponent(SourceData as ChainActionComponentData);
}

/// <summary>
/// Runtime overflow cost payload copied from OverflowCostComponentData.
/// </summary>
public partial class OverflowCostComponent : ActionComponent
{
    public int Cost { get; set; }
    public OverflowSpendType SpendType { get; set; } = OverflowSpendType.Utility;
    public bool IgnorePerRoundSpendLimits { get; set; } = false;
    public string SpendReason { get; set; } = string.Empty;

    public OverflowCostComponent(OverflowCostComponentData data)
    {
        if (data == null) return;

        SourceData = data;
        Cost = data.Cost;
        SpendType = data.SpendType;
        IgnorePerRoundSpendLimits = data.IgnorePerRoundSpendLimits;
        SpendReason = data.SpendReason;
    }

    public override ActionComponent DeepCopy()
    {
        var copy = new OverflowCostComponent(SourceData as OverflowCostComponentData)
        {
            Cost = Cost,
            SpendType = SpendType,
            IgnorePerRoundSpendLimits = IgnorePerRoundSpendLimits,
            SpendReason = SpendReason
        };
        return copy;
    }
}
