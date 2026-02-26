using Godot;

[GlobalClass]
public abstract partial class WeaponPowerFormula : Resource
{
    [ExportGroup("Output Range")]
    [Export(PropertyHint.Range, "10,255,1")]
    public int MinPower { get; private set; } = 10;

    [Export(PropertyHint.Range, "10,255,1")]
    public int MaxPower { get; private set; } = 255;

    public int EvaluatePower(
        WeaponPowerContext context,
        int fallbackPower = 10)
    {
        float raw = EvaluateRawPower(context);
        if (float.IsNaN(raw) || float.IsInfinity(raw))
        {
            raw = fallbackPower;
        }

        int min = Mathf.Min(MinPower, MaxPower);
        int max = Mathf.Max(MinPower, MaxPower);
        return Mathf.Clamp(Mathf.RoundToInt(raw), min, max);
    }

    public int EvaluatePower(
        Node holder,
        ActionContext actionContext,
        Node target,
        BattleMechanics battleMechanics,
        int fallbackPower = 10)
    {
        var context = WeaponPowerContext.Create(holder, actionContext, target, battleMechanics);
        return EvaluatePower(context, fallbackPower);
    }

    protected abstract float EvaluateRawPower(WeaponPowerContext context);
}
