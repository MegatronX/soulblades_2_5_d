using Godot;

[GlobalClass]
public partial class StatusCountWeaponPowerFormula : WeaponPowerFormula
{
    [Export(PropertyHint.Range, "0,255,1")]
    public int BasePower { get; private set; } = 40;

    [Export(PropertyHint.Range, "0,64,0.1")]
    public float PowerPerStatus { get; private set; } = 8f;

    [Export(PropertyHint.Range, "0,32,1")]
    public int StatusCountCap { get; private set; } = 8;

    protected override float EvaluateRawPower(WeaponPowerContext context)
    {
        var statusManager = context?.Holder?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        if (statusManager == null) return float.NaN;

        int activeCount = statusManager.GetActiveEffects()?.Count ?? 0;
        if (StatusCountCap > 0)
        {
            activeCount = Mathf.Min(activeCount, StatusCountCap);
        }

        return BasePower + (activeCount * Mathf.Max(0f, PowerPerStatus));
    }
}
