using Godot;

/// <summary>
/// Reusable tuning profile for firefly-style ambient movement and glow behavior.
/// </summary>
[GlobalClass]
public partial class FireflyVisualProfile : Resource
{
    [ExportCategory("Firefly Visual Profile")]
    [ExportGroup("Movement (orbit and bob feel)")]
    [Export(PropertyHint.Range, "0.05,5,0.01,suffix:hz")]
    public float DriftSpeed { get; private set; } = 0.7f;

    [Export(PropertyHint.Range, "0.01,3,0.01,suffix:m")]
    public float DriftRadius { get; private set; } = 0.35f;

    [Export(PropertyHint.Range, "0.01,3,0.01,suffix:m")]
    public float BobAmplitude { get; private set; } = 0.2f;

    [ExportGroup("Visual (sprite + base glow)")]
    [Export]
    public Texture2D SpriteTexture { get; private set; }

    [Export(PropertyHint.Range, "0.0005,0.2,0.0005,suffix:px")]
    public float SpritePixelSize { get; private set; } = 0.012f;

    [Export]
    public Color SpriteModulate { get; private set; } = new Color(1f, 0.95f, 0.74f, 0.9f);

    [Export(PropertyHint.Range, "0,10,0.01,suffix:energy")]
    public float GlowLightEnergy { get; private set; } = 0.6f;

    [ExportGroup("Glow Pulse (slow twinkle controls)")]
    [Export]
    public bool EnableGlowPulse { get; private set; } = true;

    [Export(PropertyHint.Range, "0.01,2,0.01,suffix:hz")]
    public float GlowPulseCyclesPerSecond { get; private set; } = 0.22f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float GlowPulseMinFactor { get; private set; } = 0.55f;

    [Export]
    public bool PulseSpriteAlpha { get; private set; } = true;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float SpritePulseMinAlphaFactor { get; private set; } = 0.72f;

    [ExportGroup("Sprite Frames (atlas animation subset)")]
    [Export]
    public bool AnimateSpriteFrames { get; private set; } = true;

    [Export]
    public bool ForceSingleFrameWhenAnimationDisabled { get; private set; } = true;

    [Export(PropertyHint.Range, "0.1,12,0.1,suffix:fps")]
    public float SpriteFrameFps { get; private set; } = 1.4f;

    [Export(PropertyHint.Range, "0,512,1")]
    public int AnimatedFrameStartIndex { get; private set; } = 0;

    [Export(PropertyHint.Range, "0,256,1")]
    public int AnimatedFrameCountLimit { get; private set; } = 0;

    [Export]
    public bool RandomizeStartFrame { get; private set; } = true;
}
