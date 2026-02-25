using Godot;

/// <summary>
/// A deterministic stat delta used by turn-order preview simulation.
/// </summary>
public readonly struct TurnPreviewStatDelta
{
    public TurnPreviewStatDelta(StatType stat, int additive = 0, float multiplier = 1.0f)
    {
        Stat = stat;
        Additive = additive;
        Multiplier = multiplier <= 0f ? 1.0f : multiplier;
    }

    public StatType Stat { get; }
    public int Additive { get; }
    public float Multiplier { get; }

    public bool IsNoOp => Additive == 0 && Mathf.IsEqualApprox(Multiplier, 1.0f);
}
