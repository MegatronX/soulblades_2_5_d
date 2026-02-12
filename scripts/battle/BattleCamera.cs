using Godot;
using System.Collections.Generic;

/// <summary>
/// A camera controller for a 2.5D battle system.
/// It maintains a fixed angle but pans smoothly to focus on targets.
/// </summary>
[GlobalClass]
public partial class BattleCamera : Camera3D
{
    [Export]
    public CameraProfile CurrentProfile { get; set; }

    [Export]
    public float MoveSpeed { get; set; } = 5.0f;

    [Export]
    public float DefaultFov { get; set; } = 40.0f; // Lower FOV (35-50) looks better for HD-2D

    [Export]
    public Vector3 DefaultOffset { get; set; } = new Vector3(0, 4.0f, 10.0f); // Adjusted back slightly

    [Export]
    public Vector3 LookAtOffset { get; set; } = new Vector3(0, 1.0f, 0); // Look at chest height, not feet

    [Export]
    public bool EnableAutoFocus { get; set; } = true;

    [Export]
    public float FocusWidth { get; set; } = 60.0f; // Increased to keep 2D sprites sharp across the arena

    [Export(PropertyHint.Range, "0.0, 1.0")]
    public float TargetFollowRatio { get; set; } = 0.3f; // Increased slightly for more emphasis

    private Vector3 _targetPosition;
    private Vector3 _focusPoint;
    private float _targetRoll;
    
    // Cinematic Modifiers
    private float _sweepAngle = 0f;
    private bool _sweepActive = false;
    private bool _sweepLockFocus = true;
    private Vector3 _sweepPivot = Vector3.Zero;
    private Vector3 _sweepBaseOffset = Vector3.Zero;
    private Vector3 _pushOffset = Vector3.Zero;
    private Vector3 _panOffset = Vector3.Zero;
    private Tween _pushTween;
    private Tween _panTween;
    private Tween _impactTween;
    private Tween _speedRampTween;
    private Tween _focusWidthTween;
    private float _focusWidthOverride = -1f;
    private CameraProfile.CameraShotOptions _activeActionShots = CameraProfile.CameraShotOptions.None;
    private bool _actionSequenceStarted = false;

    private static readonly CameraProfile.CameraShotOptions[] _shotFlags =
    {
        CameraProfile.CameraShotOptions.DutchAngle,
        CameraProfile.CameraShotOptions.DollyZoom,
        CameraProfile.CameraShotOptions.ParallaxSweep,
        CameraProfile.CameraShotOptions.PushIn,
        CameraProfile.CameraShotOptions.WhipPan,
        CameraProfile.CameraShotOptions.OrbitArc,
        CameraProfile.CameraShotOptions.ImpactSnap,
        CameraProfile.CameraShotOptions.RackFocus,
        CameraProfile.CameraShotOptions.SpeedRamp,
        CameraProfile.CameraShotOptions.PreStrike
    };
    
    // Shake State
    private Vector3 _actualPosition; // Position without shake
    private class ActiveShake
    {
        public CameraShakePattern Pattern;
        public float TimeRemaining;
        public float Seed;
    }
    private List<ActiveShake> _activeShakes = new();

    private float _baseFov;
    private Vector3 _baseOffset;
    private float _baseMoveSpeed;

    public override void _Ready()
    {
        // Set initial position based on a hypothetical center point (0,0,0)
        _targetPosition = Vector3.Zero + DefaultOffset;
        _focusPoint = Vector3.Zero + LookAtOffset;
        
        GlobalPosition = _targetPosition;
        _actualPosition = GlobalPosition;
        LookAt(_focusPoint, Vector3.Up);
        
        Fov = DefaultFov;

        _baseFov = DefaultFov;
        _baseOffset = DefaultOffset;
        _baseMoveSpeed = MoveSpeed;
    }

    public override void _Process(double delta)
    {
        // Smoothly interpolate position
        Vector3 finalTargetPos = _targetPosition;

        // Apply Parallax Sweep / Orbit Arc (Rotate the offset vector around a fixed pivot)
        if (_sweepActive && Mathf.Abs(_sweepAngle) > 0.001f)
        {
            Vector3 offset = _sweepBaseOffset.Rotated(Vector3.Up, Mathf.DegToRad(_sweepAngle));
            finalTargetPos = _sweepPivot + offset;
            if (_sweepLockFocus)
            {
                _focusPoint = _sweepPivot + LookAtOffset;
            }
        }

        finalTargetPos += _pushOffset + _panOffset;

        // Lerp the actual position (physics/tracking)
        _actualPosition = _actualPosition.Lerp(finalTargetPos, (float)delta * MoveSpeed);
        
        // Apply shake on top of the actual position
        GlobalPosition = _actualPosition + CalculateShakeOffset((float)delta);
        
        // Smoothly interpolate rotation to look at focus point with optional roll
        if (GlobalPosition.DistanceSquaredTo(_focusPoint) > 0.001f)
        {
            // Calculate the target transform looking at the focus point
            Transform3D targetXform = GlobalTransform.LookingAt(_focusPoint, Vector3.Up);
            // Apply Dutch Angle (Roll) around the local Z axis
            targetXform.Basis = targetXform.Basis.Rotated(targetXform.Basis.Z, _targetRoll);

            Quaternion currentRot = GlobalTransform.Basis.GetRotationQuaternion();
            Quaternion targetRot = targetXform.Basis.GetRotationQuaternion();
            GlobalTransform = new Transform3D(new Basis(currentRot.Slerp(targetRot, (float)delta * MoveSpeed)), GlobalPosition);
        }

        UpdateAutoFocus();
    }

    /// <summary>
    /// Moves the camera to frame a specific target (e.g., the active character).
    /// </summary>
    public void FocusOnTarget(Node3D target)
    {
        if (target == null) return;

        float ratio = CurrentProfile?.TargetFollowRatio ?? TargetFollowRatio;

        // We dampen the movement so the camera doesn't travel all the way to the target.
        // This keeps the view more grounded in the arena center.
        _targetPosition = (target.GlobalPosition * ratio) + DefaultOffset;
        
        // Also dampen the focus point so we don't look strictly at the character, keeping the view wider.
        _focusPoint = (target.GlobalPosition * ratio) + LookAtOffset;
    }

    /// <summary>
    /// Resets the camera to view the center of the arena.
    /// </summary>
    public void ResetView(Vector3 arenaCenter)
    {
        _targetPosition = arenaCenter + DefaultOffset;
        _focusPoint = arenaCenter + LookAtOffset;
    }

    /// <summary>
    /// Dynamically frames two subjects (e.g. attacker and target) based on the current profile.
    /// </summary>
    public void FrameAction(Node3D subjectA, Node3D subjectB, ActionCameraSettings actionSettings = null)
    {
        if (CurrentProfile == null || !CurrentProfile.EnableDynamicFraming) return;

        Vector3 midPoint = (subjectA.GlobalPosition + subjectB.GlobalPosition) / 2.0f;
        
        // Calculate separation to adjust framing distance dynamically.
        // This ensures that if targets are far apart, the camera backs up to keep them in frame.
        float separation = subjectA.GlobalPosition.DistanceTo(subjectB.GlobalPosition);
        float dynamicDistance = Mathf.Max(CurrentProfile.FramingDistance, separation * 1.1f);

        // Calculate a new offset based on the profile
        // We preserve the direction of the DefaultOffset but use the profile's distance/height
        Vector3 dir = DefaultOffset.Normalized();
        Vector3 actionOffset = dir * dynamicDistance;
        actionOffset.Y = CurrentProfile.FramingHeight;

        _targetPosition = midPoint + actionOffset;
        _focusPoint = midPoint + LookAtOffset;
        
        if (!_actionSequenceStarted)
        {
            BeginActionSequence(actionSettings);
        }
        var effectiveShots = _activeActionShots;

        // Apply Dutch Angle if enabled in profile + action
        if (effectiveShots.HasFlag(CameraProfile.CameraShotOptions.DutchAngle))
        {
            // Randomize direction for dramatic effect, but keep it consistent for the duration of the frame
            float sign = GD.Randf() > 0.5f ? 1.0f : -1.0f;
            _targetRoll = Mathf.DegToRad(CurrentProfile.MaxDutchAngle) * sign;
        }
        else
        {
            _targetRoll = 0f;
        }
        
        // Apply Orbit Arc or Parallax Sweep if enabled
        if (effectiveShots.HasFlag(CameraProfile.CameraShotOptions.OrbitArc))
        {
            float sweepDir = GD.Randf() > 0.5f ? 1.0f : -1.0f;
            TriggerOrbitArc(CurrentProfile.OrbitArcAngle * sweepDir, CurrentProfile.OrbitArcSeconds);
        }
        else if (effectiveShots.HasFlag(CameraProfile.CameraShotOptions.ParallaxSweep))
        {
            float sweepDir = GD.Randf() > 0.5f ? 1.0f : -1.0f;
            TriggerSweep(CurrentProfile.ParallaxSweepAngle * sweepDir, CurrentProfile.ParallaxSweepSeconds);
        }
        else
        {
            _sweepActive = false;
            _sweepAngle = 0f;
        }

        bool preStrike = effectiveShots.HasFlag(CameraProfile.CameraShotOptions.PreStrike);
        bool pushIn = effectiveShots.HasFlag(CameraProfile.CameraShotOptions.PushIn);
        if (preStrike && pushIn)
        {
            TriggerPreStrikeAndPushIn();
        }
        else if (preStrike)
        {
            TriggerPreStrike();
        }
        else if (pushIn)
        {
            TriggerPushIn();
        }

        if (effectiveShots.HasFlag(CameraProfile.CameraShotOptions.WhipPan))
        {
            TriggerWhipPan();
        }

        if (effectiveShots.HasFlag(CameraProfile.CameraShotOptions.SpeedRamp))
        {
            TriggerSpeedRamp();
        }

        if (effectiveShots.HasFlag(CameraProfile.CameraShotOptions.RackFocus))
        {
            TriggerRackFocus();
        }

        // Tween FOV for zoom effect (or dolly zoom)
        if (effectiveShots.HasFlag(CameraProfile.CameraShotOptions.DollyZoom))
        {
            TriggerDollyZoom(CurrentProfile.DollyZoomFov, CurrentProfile.DollyZoomSeconds);
        }
        else
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "fov", CurrentProfile.ActionZoomFov, 0.5f)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }
            
        // Temporarily increase speed for snap
        MoveSpeed = CurrentProfile.TransitionSpeed;
    }

    /// <summary>
    /// Performs a Dolly Zoom (Vertigo Effect).
    /// Changes FOV while moving camera to keep subject size constant.
    /// </summary>
    public void TriggerDollyZoom(float targetFov, float duration)
    {
        float startFov = Fov;
        float startDist = GlobalPosition.DistanceTo(_focusPoint);
        
        // Calculate the frustum height constant at the current distance
        // Constant = distance * tan(fov / 2)
        float frustumConstant = startDist * Mathf.Tan(Mathf.DegToRad(startFov) / 2.0f);

        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>((currentFov) => 
        {
            Fov = currentFov;
            // Recalculate distance needed to maintain framing
            float newDist = frustumConstant / Mathf.Tan(Mathf.DegToRad(currentFov) / 2.0f);
            
            // Apply to target position
            Vector3 dir = (_targetPosition - _focusPoint).Normalized();
            _targetPosition = _focusPoint + (dir * newDist);
            
            // Snap immediately to avoid lerp lag ruining the optical illusion
            GlobalPosition = _targetPosition;
            
        }), startFov, targetFov, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    public void TriggerSweep(float angle, float duration)
    {
        _sweepActive = true;
        _sweepLockFocus = true;
        _sweepPivot = _focusPoint;
        _sweepBaseOffset = _targetPosition - _focusPoint;
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(a => _sweepAngle = a), 0f, angle, duration)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(Callable.From(() =>
        {
            _sweepAngle = 0f;
            _sweepActive = false;
        }));
    }

    public void TriggerOrbitArc(float angle, float duration)
    {
        _sweepActive = true;
        _sweepLockFocus = true;
        _sweepPivot = _focusPoint;
        _sweepBaseOffset = _targetPosition - _focusPoint;
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(a => _sweepAngle = a), 0f, angle, duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(Callable.From(() =>
        {
            _sweepAngle = 0f;
            _sweepActive = false;
        }));
    }

    public void BeginActionSequence(ActionCameraSettings actionSettings = null)
    {
        _activeActionShots = SelectShots(GetEffectiveShots(actionSettings));
        _actionSequenceStarted = true;
    }

    public void EndActionSequence()
    {
        _activeActionShots = CameraProfile.CameraShotOptions.None;
        _actionSequenceStarted = false;
    }

    public bool IsShotActive(CameraProfile.CameraShotOptions shot)
    {
        return _activeActionShots.HasFlag(shot);
    }

    private void TriggerPushIn()
    {
        if (CurrentProfile.PushInDistance <= 0f) return;
        _pushTween?.Kill();
        var dir = (_focusPoint - _targetPosition).Normalized();
        var offset = dir * CurrentProfile.PushInDistance;
        _pushTween = CreateTween();
        _pushTween.TweenMethod(Callable.From<Vector3>(v => _pushOffset = v), Vector3.Zero, offset, CurrentProfile.PushInSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        if (CurrentProfile.PushInHoldSeconds > 0f)
        {
            _pushTween.TweenInterval(CurrentProfile.PushInHoldSeconds);
        }
        _pushTween.TweenMethod(Callable.From<Vector3>(v => _pushOffset = v), offset, Vector3.Zero, CurrentProfile.PushInReturnSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private void TriggerPreStrike()
    {
        if (CurrentProfile.PreStrikeDistance <= 0f) return;
        _pushTween?.Kill();
        var dir = (_focusPoint - _targetPosition).Normalized();
        var offset = -dir * CurrentProfile.PreStrikeDistance;
        _pushTween = CreateTween();
        _pushTween.TweenMethod(Callable.From<Vector3>(v => _pushOffset = v), Vector3.Zero, offset, CurrentProfile.PreStrikeSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _pushTween.TweenMethod(Callable.From<Vector3>(v => _pushOffset = v), offset, Vector3.Zero, CurrentProfile.PreStrikeReturnSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private void TriggerPreStrikeAndPushIn()
    {
        if (CurrentProfile.PreStrikeDistance <= 0f && CurrentProfile.PushInDistance <= 0f) return;
        _pushTween?.Kill();
        var dir = (_focusPoint - _targetPosition).Normalized();
        var backOffset = -dir * CurrentProfile.PreStrikeDistance;
        var forwardOffset = dir * CurrentProfile.PushInDistance;
        _pushTween = CreateTween();
        if (CurrentProfile.PreStrikeDistance > 0f)
        {
            _pushTween.TweenMethod(Callable.From<Vector3>(v => _pushOffset = v), Vector3.Zero, backOffset, CurrentProfile.PreStrikeSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        }
        if (CurrentProfile.PushInDistance > 0f)
        {
            _pushTween.TweenMethod(Callable.From<Vector3>(v => _pushOffset = v), backOffset, forwardOffset, CurrentProfile.PushInSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            if (CurrentProfile.PushInHoldSeconds > 0f)
            {
                _pushTween.TweenInterval(CurrentProfile.PushInHoldSeconds);
            }
        }
        _pushTween.TweenMethod(Callable.From<Vector3>(v => _pushOffset = v), forwardOffset, Vector3.Zero, CurrentProfile.PushInReturnSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private void TriggerWhipPan()
    {
        if (CurrentProfile.WhipPanDistance <= 0f) return;
        _panTween?.Kill();
        var right = GlobalTransform.Basis.X.Normalized();
        float sign = GD.Randf() > 0.5f ? 1.0f : -1.0f;
        var offset = right * (CurrentProfile.WhipPanDistance * sign);
        _panTween = CreateTween();
        _panTween.TweenMethod(Callable.From<Vector3>(v => _panOffset = v), Vector3.Zero, offset, CurrentProfile.WhipPanSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _panTween.TweenMethod(Callable.From<Vector3>(v => _panOffset = v), offset, Vector3.Zero, CurrentProfile.WhipPanReturnSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    public void TriggerImpactSnap()
    {
        if (CurrentProfile.ImpactSnapFov <= 0f) return;
        _impactTween?.Kill();
        float baseFov = Fov;
        _impactTween = CreateTween();
        if (CurrentProfile.ImpactSnapDelay > 0f)
        {
            _impactTween.TweenInterval(CurrentProfile.ImpactSnapDelay);
        }
        _impactTween.TweenProperty(this, "fov", CurrentProfile.ImpactSnapFov, CurrentProfile.ImpactSnapSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _impactTween.TweenCallback(Callable.From(() => Shake(CurrentProfile.ImpactShakeIntensity)));
        _impactTween.TweenProperty(this, "fov", baseFov, CurrentProfile.ImpactSnapReturnSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private void TriggerRackFocus()
    {
        if (CurrentProfile.RackFocusWidth <= 0f) return;
        _focusWidthTween?.Kill();
        float start = _focusWidthOverride > 0f ? _focusWidthOverride : FocusWidth;
        _focusWidthTween = CreateTween();
        _focusWidthTween.TweenMethod(Callable.From<float>(v => _focusWidthOverride = v), start, CurrentProfile.RackFocusWidth, CurrentProfile.RackFocusSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _focusWidthTween.TweenMethod(Callable.From<float>(v => _focusWidthOverride = v), CurrentProfile.RackFocusWidth, FocusWidth, CurrentProfile.RackFocusReturnSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _focusWidthTween.TweenCallback(Callable.From(() => _focusWidthOverride = -1f));
    }

    private void TriggerSpeedRamp()
    {
        if (CurrentProfile.SpeedRampMultiplier <= 0f) return;
        _speedRampTween?.Kill();
        float startSpeed = MoveSpeed;
        float targetSpeed = _baseMoveSpeed * CurrentProfile.SpeedRampMultiplier;
        _speedRampTween = CreateTween();
        _speedRampTween.TweenProperty(this, "MoveSpeed", targetSpeed, CurrentProfile.SpeedRampSeconds * 0.4f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _speedRampTween.TweenProperty(this, "MoveSpeed", startSpeed, CurrentProfile.SpeedRampSeconds * 0.6f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    public void ResetToDefault()
    {
        MoveSpeed = _baseMoveSpeed; // Restore speed
        _targetRoll = 0f; // Reset tilt
        _sweepAngle = 0f; // Reset sweep
        _sweepActive = false;
        _pushOffset = Vector3.Zero;
        _panOffset = Vector3.Zero;
        _focusWidthOverride = -1f;
        EndActionSequence();
        
        var tween = CreateTween();
        tween.TweenProperty(this, "fov", _baseFov, 1.0f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    /// <summary>
    /// Adds a slight "impact" shake. Call this when a hit occurs.
    /// </summary>
    public void Shake(float intensity = 0.5f)
    {
        // Simple instant offset, relying on Lerp in _Process to smooth it back
        _actualPosition += new Vector3(
            GD.Randf() * intensity * (CurrentProfile?.ShakeMultiplier ?? 1.0f),
            GD.Randf() * intensity * (CurrentProfile?.ShakeMultiplier ?? 1.0f),
            0
        );
    }

    /// <summary>
    /// Applies a noise-based shake pattern.
    /// </summary>
    public void ApplyShakePattern(CameraShakePattern pattern)
    {
        if (pattern == null) return;
        _activeShakes.Add(new ActiveShake 
        { 
            Pattern = pattern, 
            TimeRemaining = pattern.Duration,
            Seed = GD.Randf() * 1000f // Random offset for noise lookup
        });
    }

    private Vector3 CalculateShakeOffset(float delta)
    {
        Vector3 totalOffset = Vector3.Zero;
        float multiplier = CurrentProfile?.ShakeMultiplier ?? 1.0f;

        for (int i = _activeShakes.Count - 1; i >= 0; i--)
        {
            var shake = _activeShakes[i];
            shake.TimeRemaining -= delta;

            if (shake.TimeRemaining <= 0)
            {
                _activeShakes.RemoveAt(i);
                continue;
            }

            if (shake.Pattern.Noise == null) continue;

            // Calculate intensity based on decay
            float progress = 1.0f - (shake.TimeRemaining / shake.Pattern.Duration);
            float intensity = shake.Pattern.Amplitude;

            if (shake.Pattern.DecayCurve != null)
            {
                intensity *= shake.Pattern.DecayCurve.Sample(progress);
            }
            else
            {
                intensity *= (shake.TimeRemaining / shake.Pattern.Duration); // Linear fade
            }

            // Sample noise
            float noiseTime = (shake.Pattern.Duration - shake.TimeRemaining) * shake.Pattern.Frequency;
            float x = shake.Pattern.Noise.GetNoise2D(shake.Seed, noiseTime);
            float y = shake.Pattern.Noise.GetNoise2D(shake.Seed + 100, noiseTime); // Offset Y sample

            totalOffset += new Vector3(x, y, 0) * intensity;
        }

        return totalOffset * multiplier;
    }

    private void UpdateAutoFocus()
    {
        if (!EnableAutoFocus) return;

        // Try to get attributes from the camera first, then the world environment
        var attributes = Attributes as CameraAttributesPractical;
        
        if (attributes == null && GetWorld3D().CameraAttributes is CameraAttributesPractical worldAttributes)
        {
            attributes = worldAttributes;
        }

        if (attributes != null)
        {
            float dist = GlobalPosition.DistanceTo(_focusPoint);
            float width = _focusWidthOverride > 0f ? _focusWidthOverride : FocusWidth;
            attributes.DofBlurNearDistance = Mathf.Max(0.1f, dist - (width / 2.0f));
            attributes.DofBlurFarDistance = dist + (width / 2.0f);
            attributes.DofBlurFarEnabled = true;
            attributes.DofBlurNearEnabled = true;
        }
    }

    private CameraProfile.CameraShotOptions GetEffectiveShots(ActionCameraSettings actionSettings)
    {
        var profileShots = CurrentProfile?.AllowedShots ?? CameraProfile.CameraShotOptions.None;
        if (actionSettings == null || actionSettings.AllowedShots == CameraProfile.CameraShotOptions.None)
        {
            return profileShots;
        }
        return profileShots & actionSettings.AllowedShots;
    }

    private CameraProfile.CameraShotOptions SelectShots(CameraProfile.CameraShotOptions effectiveShots)
    {
        if (CurrentProfile == null || !CurrentProfile.RandomizeShots)
        {
            return effectiveShots;
        }

        var available = new List<CameraProfile.CameraShotOptions>();
        foreach (var flag in _shotFlags)
        {
            if (effectiveShots.HasFlag(flag))
            {
                available.Add(flag);
            }
        }

        if (available.Count == 0) return CameraProfile.CameraShotOptions.None;

        int max = CurrentProfile.MaxConcurrentShots;
        if (max <= 0 || max > available.Count) max = available.Count;
        if (max >= available.Count) return effectiveShots;

        var selected = CameraProfile.CameraShotOptions.None;
        for (int i = 0; i < max; i++)
        {
            int idx = GD.RandRange(0, available.Count - 1);
            selected |= available[idx];
            available.RemoveAt(idx);
        }

        return selected;
    }
}
