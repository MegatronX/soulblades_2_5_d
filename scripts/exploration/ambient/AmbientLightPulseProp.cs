using Godot;

/// <summary>
/// Reusable ambient prop behavior that gently pulses an attached OmniLight3D.
/// Useful for glowing mushrooms/crystals.
/// </summary>
[GlobalClass]
public partial class AmbientLightPulseProp : Node3D
{
    [Export]
    public NodePath LightPath { get; private set; } = "OmniLight3D";

    [Export(PropertyHint.Range, "0,12,0.01")]
    public float BaseEnergy { get; private set; } = 1.2f;

    [Export(PropertyHint.Range, "0,8,0.01")]
    public float PulseAmplitude { get; private set; } = 0.35f;

    [Export(PropertyHint.Range, "0.05,10,0.01")]
    public float PulseSpeed { get; private set; } = 1.4f;

    [Export(PropertyHint.Range, "0.1,16,0.01")]
    public float PulseRangeMultiplier { get; private set; } = 1.0f;

    private OmniLight3D _light;
    private float _phase;
    private float _baseRange = 1f;

    public override void _Ready()
    {
        _light = GetNodeOrNull<OmniLight3D>(LightPath);
        _phase = GD.Randf() * Mathf.Tau;
        if (_light != null)
        {
            _baseRange = Mathf.Max(0.1f, _light.OmniRange);
        }
        ApplyLightState();
    }

    public override void _Process(double delta)
    {
        if (_light == null || !GodotObject.IsInstanceValid(_light)) return;

        _phase += (float)delta * Mathf.Max(0.01f, PulseSpeed);
        ApplyLightState();
    }

    private void ApplyLightState()
    {
        if (_light == null) return;

        float pulse = 0.5f + (0.5f * Mathf.Sin(_phase));
        float energy = Mathf.Max(0f, BaseEnergy + ((pulse - 0.5f) * 2f * PulseAmplitude));
        _light.LightEnergy = energy;

        if (PulseRangeMultiplier > 0f)
        {
            _light.OmniRange = Mathf.Max(0.1f, _baseRange * PulseRangeMultiplier);
        }
    }
}
