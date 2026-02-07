using Godot;

/// <summary>
/// Visual representation of the timed hit window (e.g., a collapsing ring).
/// </summary>
public partial class TimedHitUI : Control
{
    [Export] private Control _staticRing; // The target circle
    [Export] private Control _collapsingRing; // The moving circle
    [Export] private GpuParticles2D _collapsingParticles; // Particle trail for the ring
    
    [ExportGroup("Appearance")]
    [Export] public Color StaticRingColor { get; set; } = Colors.White;
    [Export] public Color StaticRingPulseColor { get; set; } = Colors.BlueViolet;
    [Export] public Color CollapsingRingColor { get; set; } = Colors.White;
    [Export] public Color MissColor { get; set; } = Colors.Red;
    [Export] public float StartScale { get; set; } = 6.0f;

    private float _duration;
    private float _elapsed;
    private bool _active;
    
    private Node3D _target3D;
    private Vector3 _targetOffset;

    public override void _Ready()
    {
        // Ensure pivots are centered for scaling
        if (_staticRing != null) _staticRing.PivotOffset = _staticRing.Size / 2;
        if (_collapsingRing != null) _collapsingRing.PivotOffset = _collapsingRing.Size / 2;
        
        Hide();
        SetProcess(false);
    }

    public void SetTarget(Node3D target, Vector3 offset)
    {
        _target3D = target;
        _targetOffset = offset;
        UpdateScreenPosition();
    }

    public void Start(float duration)
    {
        _duration = duration;
        _elapsed = 0;
        _active = true;
        
        Show();
        SetProcess(true);
        
        // Reset scales
        if (_collapsingRing != null) _collapsingRing.Scale = new Vector2(StartScale, StartScale); // Start large
        if (_collapsingRing != null) _collapsingRing.Modulate = CollapsingRingColor; // Reset color
        if (_collapsingParticles != null) _collapsingParticles.Emitting = true;

        // Pulse static ring to make it more visible
        if (_staticRing != null)
        {
            _staticRing.Modulate = StaticRingColor;
            var tween = CreateTween().SetLoops();
            tween.TweenProperty(_staticRing, "scale", Vector2.One * 1.1f, 0.5f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tween.Parallel().TweenProperty(_staticRing, "modulate", StaticRingPulseColor, 0.5f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(_staticRing, "scale", Vector2.One, 0.5f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tween.Parallel().TweenProperty(_staticRing, "modulate", StaticRingColor, 0.5f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }
    }

    public void Stop()
    {
        _active = false;
        if (_collapsingParticles != null) _collapsingParticles.Emitting = false;
        SetProcess(false);
        
        // Smooth fade out instead of instant destruction
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.2f);
        tween.Finished += QueueFree;
    }

    public override void _Process(double delta)
    {
        if (!_active) return;

        UpdateScreenPosition();

        _elapsed += (float)delta;
        float progress = _elapsed / _duration;

        if (_collapsingRing != null)
        {
            // Linearly interpolate scale from StartScale down to 1.0 (target)
            // We want it to hit 1.0 exactly when progress is 1.0.
            float currentScale = Mathf.Lerp(StartScale, 1.0f, progress);
            _collapsingRing.Scale = new Vector2(currentScale, currentScale);

            // Visual feedback for missing the window (overshoot)
            // We allow a small window past 1.0 (the "Good" window) before showing failure.
            float timePastTarget = _elapsed - _duration;
            const float GoodWindow = 0.2f; // Matches TimedHitManager.BaseGoodWindow

            if (timePastTarget > GoodWindow)
            {
                // Fade out over 0.2s past the window
                float fadeProgress = Mathf.Clamp((timePastTarget - GoodWindow) / 0.2f, 0f, 1f);
                _collapsingRing.Modulate = new Color(MissColor.R, MissColor.G, MissColor.B, 1.0f - fadeProgress);
            }
        }
    }

    private void UpdateScreenPosition()
    {
        if (!IsInstanceValid(_target3D)) return;
        
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        Vector3 worldPos = _target3D.GlobalPosition + _targetOffset;

        // Hide if the target is behind the camera
        if (camera.IsPositionBehind(worldPos))
        {
            if (Visible) Hide();
        }
        else
        {
            if (_active && !Visible) Show();
            
            Vector2 screenPos = camera.UnprojectPosition(worldPos);
            GlobalPosition = screenPos - (Size / 2); // Center the control on the target
        }
    }
}