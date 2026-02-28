using Godot;

/// <summary>
/// Periodically plays an owl-call ambience clip.
/// </summary>
[GlobalClass]
public partial class OwlCallProp : Node3D
{
    [Export]
    public AudioStreamPlayer3D OwlPlayer { get; private set; }

    [Export(PropertyHint.Range, "2,60,0.1")]
    public float MinIntervalSeconds { get; private set; } = 8f;

    [Export(PropertyHint.Range, "2,60,0.1")]
    public float MaxIntervalSeconds { get; private set; } = 18f;

    private readonly RandomNumberGenerator _rng = new();
    private float _timer;

    public override void _Ready()
    {
        _rng.Randomize();
        if (OwlPlayer == null)
        {
            OwlPlayer = GetNodeOrNull<AudioStreamPlayer3D>("AudioStreamPlayer3D");
        }
        ResetTimer();
    }

    public override void _Process(double delta)
    {
        if (OwlPlayer == null || OwlPlayer.Stream == null) return;
        _timer -= (float)delta;
        if (_timer > 0f) return;

        OwlPlayer.Play();
        ResetTimer();
    }

    private void ResetTimer()
    {
        float min = Mathf.Min(MinIntervalSeconds, MaxIntervalSeconds);
        float max = Mathf.Max(MinIntervalSeconds, MaxIntervalSeconds);
        _timer = _rng.RandfRange(min, max);
    }
}
