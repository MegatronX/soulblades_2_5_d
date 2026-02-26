using Godot;

[GlobalClass]
public partial class MissingHpWeaponPowerFormula : WeaponPowerFormula
{
    [Export(PropertyHint.Range, "0.1,6,0.01")]
    public float CurveExponent { get; private set; } = 1.0f;

    protected override float EvaluateRawPower(WeaponPowerContext context)
    {
        var stats = context?.Holder?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        if (stats == null) return float.NaN;

        int maxHp = Mathf.Max(1, stats.GetStatValue(StatType.HP));
        float missingRatio = Mathf.Clamp((maxHp - stats.CurrentHP) / (float)maxHp, 0f, 1f);
        float curved = Mathf.Pow(missingRatio, Mathf.Max(0.1f, CurveExponent));

        int min = Mathf.Min(MinPower, MaxPower);
        int max = Mathf.Max(MinPower, MaxPower);
        return Mathf.Lerp(min, max, curved);
    }
}
