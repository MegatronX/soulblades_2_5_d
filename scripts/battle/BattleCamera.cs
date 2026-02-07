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

        // Apply Parallax Sweep (Rotate the offset vector around the focus point)
        if (Mathf.Abs(_sweepAngle) > 0.001f)
        {
            Vector3 offset = finalTargetPos - _focusPoint;
            offset = offset.Rotated(Vector3.Up, Mathf.DegToRad(_sweepAngle));
            finalTargetPos = _focusPoint + offset;
        }

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
    public void FrameAction(Node3D subjectA, Node3D subjectB)
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
        
        // Apply Dutch Angle if enabled in profile
        if (CurrentProfile.AllowedShots.HasFlag(CameraProfile.CameraShotOptions.DutchAngle))
        {
            // Randomize direction for dramatic effect, but keep it consistent for the duration of the frame
            float sign = GD.Randf() > 0.5f ? 1.0f : -1.0f;
            _targetRoll = Mathf.DegToRad(CurrentProfile.MaxDutchAngle) * sign;
        }
        else
        {
            _targetRoll = 0f;
        }
        
        // Apply Parallax Sweep if enabled
        if (CurrentProfile.AllowedShots.HasFlag(CameraProfile.CameraShotOptions.ParallaxSweep))
        {
            // Sweep 15 degrees over the duration of the action
            float sweepDir = GD.Randf() > 0.5f ? 1.0f : -1.0f;
            TriggerSweep(15.0f * sweepDir, 2.0f); // Duration should ideally match action length
        }
        else { _sweepAngle = 0f; }

        // Tween FOV for zoom effect
        var tween = CreateTween();
        tween.TweenProperty(this, "fov", CurrentProfile.ActionZoomFov, 0.5f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            
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
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(a => _sweepAngle = a), 0f, angle, duration)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
    }

    public void ResetToDefault()
    {
        MoveSpeed = _baseMoveSpeed; // Restore speed
        _targetRoll = 0f; // Reset tilt
        _sweepAngle = 0f; // Reset sweep
        
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
            attributes.DofBlurNearDistance = Mathf.Max(0.1f, dist - (FocusWidth / 2.0f));
            attributes.DofBlurFarDistance = dist + (FocusWidth / 2.0f);
            attributes.DofBlurFarEnabled = true;
            attributes.DofBlurNearEnabled = true;
        }
    }
}