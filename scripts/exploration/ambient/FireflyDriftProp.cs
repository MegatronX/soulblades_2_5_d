using Godot;

/// <summary>
/// Lightweight ambient movement for firefly-style props.
/// </summary>
[GlobalClass]
public partial class FireflyDriftProp : Node3D, IAmbientPropFadeReceiver
{
    [ExportCategory("Firefly Drift Prop (override or profile-driven)")]
    [Export]
    public FireflyVisualProfile VisualProfile { get; private set; }

    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "0.05,5,0.01,suffix:hz")]
    public float DriftSpeed { get; private set; } = 0.7f;

    [Export(PropertyHint.Range, "0.01,3,0.01,suffix:m")]
    public float DriftRadius { get; private set; } = 0.35f;

    [Export(PropertyHint.Range, "0.01,3,0.01,suffix:m")]
    public float BobAmplitude { get; private set; } = 0.2f;

    [ExportGroup("Visual")]
    [Export]
    public NodePath SpritePath { get; private set; } = "Sprite3D";

    [Export]
    public Texture2D SpriteTexture { get; private set; }

    [Export(PropertyHint.Range, "0.0005,0.2,0.0005,suffix:px")]
    public float SpritePixelSize { get; private set; } = 0.012f;

    [Export]
    public Color SpriteModulate { get; private set; } = new Color(1f, 0.95f, 0.74f, 0.9f);

    [Export]
    public bool SpriteBillboardEnabled { get; private set; } = true;

    [Export]
    public bool HideMeshWhenSpritePresent { get; private set; } = true;

    [Export]
    public NodePath MeshPath { get; private set; } = "MeshInstance3D";

    [Export]
    public NodePath GlowLightPath { get; private set; } = "GlowLight";

    [Export(PropertyHint.Range, "0,10,0.01,suffix:energy")]
    public float GlowLightEnergy { get; private set; } = 0.6f;

    [ExportGroup("Glow Pulse")]
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

    [ExportGroup("Sprite Frames")]
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

    private Vector3 _origin;
    private float _phase;
    private float _glowPhase;
    private float _frameAccumulator;
    private Sprite3D _sprite;
    private OmniLight3D _light;
    private Color _baseSpriteModulate = Colors.White;
    private float _baseLightEnergy;
    private int _frameCount = 1;
    private int _animatedFrameStart = 0;
    private float _ambientFadeMultiplier = 1f;

    public override void _Ready()
    {
        ApplyProfileOverrides();
        _origin = Position;
        _phase = GD.Randf() * Mathf.Tau;
        _glowPhase = GD.Randf() * Mathf.Tau;
        ConfigureVisuals();
    }

    public override void _Process(double delta)
    {
        float dt = Mathf.Max(0f, (float)delta);
        _phase += dt * DriftSpeed;
        float x = Mathf.Cos(_phase) * DriftRadius;
        float z = Mathf.Sin(_phase * 1.3f) * DriftRadius;
        float y = Mathf.Sin(_phase * 2f) * BobAmplitude;
        Position = _origin + new Vector3(x, y, z);

        UpdateGlowPulse(dt);
        UpdateSpriteAnimation(dt);
    }

    private void ConfigureVisuals()
    {
        _sprite = GetNodeOrNull<Sprite3D>(SpritePath);
        if (_sprite != null)
        {
            if (SpriteTexture != null)
            {
                _sprite.Texture = SpriteTexture;
            }

            _sprite.PixelSize = Mathf.Max(0.0001f, SpritePixelSize);
            _sprite.Modulate = SpriteModulate;
            _sprite.Billboard = SpriteBillboardEnabled
                ? BaseMaterial3D.BillboardModeEnum.Enabled
                : BaseMaterial3D.BillboardModeEnum.Disabled;

            _baseSpriteModulate = _sprite.Modulate;
            int totalFrames = Mathf.Max(1, _sprite.Hframes * _sprite.Vframes);
            _animatedFrameStart = Mathf.Clamp(AnimatedFrameStartIndex, 0, totalFrames - 1);
            int availableFrames = Mathf.Max(1, totalFrames - _animatedFrameStart);
            int animationFrameCount = availableFrames;
            if (AnimatedFrameCountLimit > 0)
            {
                animationFrameCount = Mathf.Clamp(AnimatedFrameCountLimit, 1, availableFrames);
            }

            if (!AnimateSpriteFrames)
            {
                // Keep atlas layout intact when animation is off; forcing H/V to 1x1
                // breaks frame-subset sprite sheets and causes visual popping.
                _frameCount = 1;
                if (ForceSingleFrameWhenAnimationDisabled)
                {
                    _sprite.Frame = _animatedFrameStart;
                }
            }
            else
            {
                _frameCount = animationFrameCount;
                int maxFrame = _animatedFrameStart + _frameCount - 1;
                if (_sprite.Frame < _animatedFrameStart || _sprite.Frame > maxFrame)
                {
                    _sprite.Frame = _animatedFrameStart;
                }

                if (RandomizeStartFrame && _frameCount > 1)
                {
                    _sprite.Frame = _animatedFrameStart + (int)(GD.Randi() % (uint)_frameCount);
                }
            }
        }
        else
        {
            _frameCount = 1;
        }

        if (HideMeshWhenSpritePresent)
        {
            var mesh = GetNodeOrNull<MeshInstance3D>(MeshPath);
            if (mesh != null && _sprite != null && _sprite.Texture != null)
            {
                mesh.Visible = false;
            }
        }

        _light = GetNodeOrNull<OmniLight3D>(GlowLightPath);
        if (_light != null)
        {
            _baseLightEnergy = Mathf.Max(0f, GlowLightEnergy);
            _light.LightEnergy = _baseLightEnergy * _ambientFadeMultiplier;
            _light.LightColor = new Color(SpriteModulate.R, SpriteModulate.G, SpriteModulate.B);
        }

        ApplyAmbientFadeNow();
    }

    private void UpdateGlowPulse(float dt)
    {
        if (_light == null || !GodotObject.IsInstanceValid(_light)) return;

        if (!EnableGlowPulse)
        {
            _light.LightEnergy = _baseLightEnergy * _ambientFadeMultiplier;
            if (_sprite != null && GodotObject.IsInstanceValid(_sprite))
            {
                var modulate = _baseSpriteModulate;
                modulate.A *= _ambientFadeMultiplier;
                _sprite.Modulate = modulate;
            }
            return;
        }

        float cyclesPerSecond = Mathf.Max(0.01f, GlowPulseCyclesPerSecond);
        _glowPhase += dt * cyclesPerSecond * Mathf.Tau;

        float wave = 0.5f + (0.5f * Mathf.Sin(_glowPhase));
        float minFactor = Mathf.Clamp(GlowPulseMinFactor, 0f, 1f);
        float glowFactor = Mathf.Lerp(minFactor, 1f, wave);
        _light.LightEnergy = Mathf.Max(0f, _baseLightEnergy * glowFactor * _ambientFadeMultiplier);

        if (PulseSpriteAlpha && _sprite != null && GodotObject.IsInstanceValid(_sprite))
        {
            float alphaMin = Mathf.Clamp(SpritePulseMinAlphaFactor, 0f, 1f);
            float alphaFactor = Mathf.Lerp(alphaMin, 1f, wave);
            var modulate = _baseSpriteModulate;
            modulate.A *= alphaFactor * _ambientFadeMultiplier;
            _sprite.Modulate = modulate;
        }
    }

    private void UpdateSpriteAnimation(float dt)
    {
        if (!AnimateSpriteFrames) return;
        if (_sprite == null || !GodotObject.IsInstanceValid(_sprite)) return;
        if (_frameCount <= 1) return;

        _frameAccumulator += dt * Mathf.Max(0.1f, SpriteFrameFps);
        while (_frameAccumulator >= 1f)
        {
            int localFrame = _sprite.Frame - _animatedFrameStart;
            if (localFrame < 0 || localFrame >= _frameCount)
            {
                localFrame = 0;
            }

            localFrame = (localFrame + 1) % _frameCount;
            _sprite.Frame = _animatedFrameStart + localFrame;
            _frameAccumulator -= 1f;
        }
    }

    public void SetAmbientFadeMultiplier(float alphaMultiplier)
    {
        _ambientFadeMultiplier = Mathf.Clamp(alphaMultiplier, 0f, 1f);
        ApplyAmbientFadeNow();
    }

    private void ApplyAmbientFadeNow()
    {
        if (_sprite != null && GodotObject.IsInstanceValid(_sprite))
        {
            var modulate = _baseSpriteModulate;
            modulate.A *= _ambientFadeMultiplier;
            _sprite.Modulate = modulate;
        }

        if (_light != null && GodotObject.IsInstanceValid(_light))
        {
            _light.LightEnergy = _baseLightEnergy * _ambientFadeMultiplier;
        }
    }

    private void ApplyProfileOverrides()
    {
        if (VisualProfile == null) return;

        DriftSpeed = VisualProfile.DriftSpeed;
        DriftRadius = VisualProfile.DriftRadius;
        BobAmplitude = VisualProfile.BobAmplitude;
        if (VisualProfile.SpriteTexture != null)
        {
            SpriteTexture = VisualProfile.SpriteTexture;
        }

        SpritePixelSize = VisualProfile.SpritePixelSize;
        SpriteModulate = VisualProfile.SpriteModulate;
        GlowLightEnergy = VisualProfile.GlowLightEnergy;
        EnableGlowPulse = VisualProfile.EnableGlowPulse;
        GlowPulseCyclesPerSecond = VisualProfile.GlowPulseCyclesPerSecond;
        GlowPulseMinFactor = VisualProfile.GlowPulseMinFactor;
        PulseSpriteAlpha = VisualProfile.PulseSpriteAlpha;
        SpritePulseMinAlphaFactor = VisualProfile.SpritePulseMinAlphaFactor;
        AnimateSpriteFrames = VisualProfile.AnimateSpriteFrames;
        ForceSingleFrameWhenAnimationDisabled = VisualProfile.ForceSingleFrameWhenAnimationDisabled;
        SpriteFrameFps = VisualProfile.SpriteFrameFps;
        AnimatedFrameStartIndex = VisualProfile.AnimatedFrameStartIndex;
        AnimatedFrameCountLimit = VisualProfile.AnimatedFrameCountLimit;
        RandomizeStartFrame = VisualProfile.RandomizeStartFrame;
    }
}
