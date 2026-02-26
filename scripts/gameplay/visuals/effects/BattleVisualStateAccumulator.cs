using Godot;

public sealed class BattleVisualStateAccumulator
{
    private bool _hasTint;
    private Color _tintColor = Colors.White;
    private float _tintStrength = 0f;
    private int _tintPriority = int.MinValue;
    private int _tintOrder = int.MinValue;

    private ShaderMaterial _shader;
    private int _shaderPriority = int.MinValue;
    private int _shaderOrder = int.MinValue;

    private bool _hasMirror;
    private MirrorImageVisualSettings _mirrorSettings = MirrorImageVisualSettings.Default;
    private int _mirrorPriority = int.MinValue;
    private int _mirrorOrder = int.MinValue;

    private bool _hasHover;
    private HoverVisualSettings _hoverSettings = HoverVisualSettings.Default;
    private int _hoverPriority = int.MinValue;
    private int _hoverOrder = int.MinValue;

    public float ScaleMultiplier { get; private set; } = 1f;
    public bool ForceInjuredIdle { get; private set; } = false;

    public bool TryGetTint(out Color color, out float strength)
    {
        color = _tintColor;
        strength = _tintStrength;
        return _hasTint && strength > 0f;
    }

    public bool TryGetShader(out ShaderMaterial shader)
    {
        shader = _shader;
        return shader != null;
    }

    public bool TryGetMirrorImages(out MirrorImageVisualSettings settings)
    {
        settings = _mirrorSettings;
        return _hasMirror && settings.Alpha > 0f && settings.Count > 0;
    }

    public bool TryGetHover(out HoverVisualSettings settings)
    {
        settings = _hoverSettings;
        return _hasHover && settings.IsEnabled;
    }

    public float GetClampedScale(float min = 0.2f, float max = 4f)
    {
        return Mathf.Clamp(ScaleMultiplier, min, max);
    }

    public void MultiplyScale(float multiplier)
    {
        if (multiplier <= 0f) return;
        if (Mathf.IsEqualApprox(multiplier, 1f)) return;
        ScaleMultiplier *= multiplier;
    }

    public void SetForceInjuredIdle(bool value = true)
    {
        if (value)
        {
            ForceInjuredIdle = true;
        }
    }

    public void ConsiderTint(Color tintColor, float strength, int priority, int order)
    {
        float clamped = Mathf.Clamp(strength, 0f, 1f);
        if (clamped <= 0f) return;

        if (!_hasTint || priority > _tintPriority || (priority == _tintPriority && order >= _tintOrder))
        {
            _hasTint = true;
            _tintColor = tintColor;
            _tintStrength = clamped;
            _tintPriority = priority;
            _tintOrder = order;
        }
    }

    public void ConsiderShader(ShaderMaterial shader, int priority, int order)
    {
        if (shader == null) return;

        if (_shader == null || priority > _shaderPriority || (priority == _shaderPriority && order >= _shaderOrder))
        {
            _shader = shader;
            _shaderPriority = priority;
            _shaderOrder = order;
        }
    }

    public void ConsiderMirrorImages(MirrorImageVisualSettings settings, int priority, int order)
    {
        if (settings.Count <= 0 || settings.Alpha <= 0f) return;

        if (!_hasMirror || priority > _mirrorPriority || (priority == _mirrorPriority && order >= _mirrorOrder))
        {
            _hasMirror = true;
            _mirrorSettings = settings;
            _mirrorPriority = priority;
            _mirrorOrder = order;
        }
    }

    public void ConsiderHover(HoverVisualSettings settings, int priority, int order)
    {
        if (!settings.IsEnabled) return;

        if (!_hasHover || priority > _hoverPriority || (priority == _hoverPriority && order >= _hoverOrder))
        {
            _hasHover = true;
            _hoverSettings = settings;
            _hoverPriority = priority;
            _hoverOrder = order;
        }
    }
}
