using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles the visual presentation of battle actions.
/// Manages animations, VFX, damage numbers, and Timed Hit windows.
/// </summary>
[GlobalClass]
public partial class BattleAnimator : Node
{
    [Export] private PackedScene _timedHitUIScene;
    [Export] private PackedScene _perfectHitVfxScene;
    [Export] private PackedScene _damageNumberScene;
    
    [ExportGroup("Death VFX Defaults")]
    [Export] private PackedScene _defaultDeathVfx;
    [Export] private PackedScene _fireDeathVfx;

    [Signal]
    public delegate void TimedHitWindowOpenedEventHandler(TimedHitSettings settings, ActionContext context, float timeToHit);

    [Signal]
    public delegate void TimedHitWindowClosedEventHandler();

    // Map context to UI to handle multiple simultaneous windows
    private Dictionary<(ActionContext, TimedHitSettings), TimedHitUI> _activeUIs = new();

    private ScreenEffects _screenEffects;

    public override void _Ready()
    {
        _screenEffects = new ScreenEffects();
        AddChild(_screenEffects);
    }

    // In a real implementation, this would likely interface with a sequence editor or timeline resource.
    
    public async Task PlayWindup(ActionContext context)
    {
        var settings = context.SourceAction.VisualSettings;
        var character = context.Initiator as BaseCharacter;
        float waitTime = 0.5f; // Default fallback

        // 1. Play "Cast" or "Prepare" animation on initiator
        if (character != null && character.AnimationPlayer != null)
        {
            // Simple hardcoded check for now, ideally data-driven
            string animName = settings?.WindupAnimation ?? (context.SourceAction.Category == "Magic" ? "Cast" : "Prepare");
            if (character.AnimationPlayer.HasAnimation(animName))
            {
                character.AnimationPlayer.Play(animName);
                waitTime = (float)character.AnimationPlayer.GetAnimation(animName).Length;
            }
        }

        // 2. Spawn Windup VFX (e.g. Magic Circle)
        if (settings?.WindupVfx != null && context.Initiator is Node3D initiator3D)
        {
            SpawnVfx(settings.WindupVfx, initiator3D, settings.WindupVfxOffset, settings.ParentWindupVfx);
        }

        // 3. Wait for animation if requested
        if (settings?.WaitForWindupAnimation ?? true)
        {
            await ToSignal(GetTree().CreateTimer(waitTime), SceneTreeTimer.SignalName.Timeout);
        }
    }

    public async Task PlayExecution(ActionContext context, List<ActionContext> targetContexts)
    {
        var settings = context.SourceAction.VisualSettings;
        var character = context.Initiator as BaseCharacter;
        var timedHitList = context.SourceAction.TimedHitSettings;

        Vector3 originalPos = Vector3.Zero;
        bool moved = false;
        bool cameraFramed = false;
        var camera = GetViewport().GetCamera3D() as BattleCamera;

        // 0. Close Distance (Melee)
        if (settings?.CloseDistance == true && character != null)
        {
            originalPos = character.GlobalPosition;
            Vector3? targetPos = CalculateApproachPosition(character, targetContexts, settings);
            if (targetPos.HasValue)
            {
                // Trigger Dynamic Camera Framing
                if (camera != null && targetContexts.Count > 0 && targetContexts[0].CurrentTarget is Node3D target3D)
                {
                    camera.FrameAction(character, target3D);
                    cameraFramed = true;
                }

                await MoveCharacter(character, targetPos.Value);
                moved = true;
            }
        }

        // --- TIMING LOGIC ---
        // Calculate the maximum pre-animation delay required by any timed hit.
        // If a hit needs 1.5s of UI time but happens 0.5s into the animation, we must wait 1.0s before starting the animation.
        float maxPreAnimDelay = 0f;
        // Only calculate delay for player characters. Enemies should attack immediately.
        if (context.Initiator.IsInGroup(GameGroups.PlayerCharacters) && timedHitList != null)
        {
            foreach (var hit in timedHitList)
            {
                float requiredHeadStart = hit.VisualShrinkDuration - hit.TimingOffset;
                if (requiredHeadStart > maxPreAnimDelay) maxPreAnimDelay = requiredHeadStart;
            }
        }

        // 2. Play Execution Animation (e.g. "Attack")
        if (character != null && character.AnimationPlayer != null)
        {
            string animName = settings?.ExecutionAnimation ?? "Attack";
            // Only play if defined and exists (Magic might not have an execution anim, just windup)
            if (!string.IsNullOrEmpty(animName) && character.AnimationPlayer.HasAnimation(animName))
            {
                // If we need to wait for UI to spin up, do it before playing animation
                if (maxPreAnimDelay > 0)
                {
                    await ToSignal(GetTree().CreateTimer(maxPreAnimDelay), SceneTreeTimer.SignalName.Timeout);
                }
                character.AnimationPlayer.Play(animName);
            }
        }

        // 3. Spawn Travel VFX (Run in parallel so delays don't block impact timing)
        if (settings?.TravelVfx != null && context.Initiator is Node3D initiator3D)
        {
            _ = PlayTravelVfxSequence(settings, initiator3D, targetContexts);
        }

        // 4. Handle Timed Hits (Concurrent)
        var hitTasks = new List<Task>();
        if (timedHitList != null && timedHitList.Count > 0)
        {
            foreach (var hitSetting in timedHitList)
            {
                // We pass the maxPreAnimDelay so the handler knows "Time 0" is actually shifted
                hitTasks.Add(HandleSingleTimedHit(context, targetContexts, hitSetting, maxPreAnimDelay));
            }
            await Task.WhenAll(hitTasks);
        }
        else
        {
            // Fallback if no timed hits defined: just wait a default duration and trigger impact once
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
            TriggerImpact(context, targetContexts, spawnVfx: true);
        }

        // 7. Return to Original Position
        if (moved && character != null)
        {
            // Reset camera as we pull back
            if (cameraFramed && camera != null)
            {
                camera.ResetToDefault();
                camera.FocusOnTarget(character); // Focus back on the actor
            }
            await MoveCharacter(character, originalPos, isReturning: true);
        }
    }

    private async Task HandleSingleTimedHit(ActionContext context, List<ActionContext> targetContexts, TimedHitSettings hitSettings, float globalDelay)
    {
        var visualSettings = context.SourceAction.VisualSettings;
        
        // Calculate absolute times relative to the start of this method (t=0)
        // We add globalDelay because the animation (and thus the "TimingOffset") starts after that delay.
        float impactTime = hitSettings.TimingOffset + globalDelay;
        float uiDuration = hitSettings.VisualShrinkDuration;
        float uiStartTime = impactTime - uiDuration;
        
        float vfxPrewarm = visualSettings?.ImpactVfxPrewarm ?? 0f;
        float vfxStartTime = impactTime - vfxPrewarm;

        // Create a sorted list of events
        var events = new List<(float Time, System.Action Action)>();

        // Event: Start UI
        TimedHitUI localUI = null;
        events.Add((uiStartTime, () => 
        {
            localUI = SpawnTimedHitUI(hitSettings, context, targetContexts, uiDuration);
        }));

        // Event: Spawn VFX (if prewarm is used, otherwise TriggerImpact handles it)
        if (vfxPrewarm > 0 && visualSettings?.ImpactVfx != null)
        {
            events.Add((vfxStartTime, () => 
            {
                foreach (var targetCtx in targetContexts)
                {
                    if (targetCtx.CurrentTarget is Node3D target3D)
                    {
                        SpawnVfx(visualSettings.ImpactVfx, target3D, visualSettings.ImpactVfxOffset, parent: false);
                    }
                }
            }));
        }

        // Event: Impact
        events.Add((impactTime, () => 
        {
            // Trigger impact logic (Animation + VFX if not prewarmed)
            TriggerImpact(context, targetContexts, spawnVfx: vfxPrewarm <= 0);
            
            if (localUI != null)
            {
                _activeUIs.Remove((context, hitSettings));
                localUI.Stop();
            }
            EmitSignal(SignalName.TimedHitWindowClosed);
        }));

        // Execute events in order
        events.Sort((a, b) => a.Time.CompareTo(b.Time));
        
        float currentTime = 0f;
        foreach (var evt in events)
        {
            float wait = evt.Time - currentTime;
            if (wait > 0)
            {
                await ToSignal(GetTree().CreateTimer(wait), SceneTreeTimer.SignalName.Timeout);
                currentTime += wait;
            }
            evt.Action();
        }

        // Small buffer after impact
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
    }

    private TimedHitUI SpawnTimedHitUI(TimedHitSettings settings, ActionContext context, List<ActionContext> targetContexts, float duration)
    {
        // Only show timed hits for player characters
        if (!context.Initiator.IsInGroup(GameGroups.PlayerCharacters))
        {
            return null;
        }

        TimedHitUI uiInstance = null;
        // Spawn UI on the target(s)
        var primaryTarget = targetContexts.Count > 0 ? targetContexts[0].CurrentTarget : null;
        
        if (primaryTarget != null && _timedHitUIScene != null)
        {
            uiInstance = _timedHitUIScene.Instantiate<TimedHitUI>();
            AddChild(uiInstance);
            
            if (primaryTarget is Node3D target3D)
            {
                uiInstance.SetTarget(target3D, new Vector3(0, 1.5f, 0));
            }
            
            uiInstance.Start(duration);
            _activeUIs[(context, settings)] = uiInstance;
        }

        EmitSignal(SignalName.TimedHitWindowOpened, settings, context, duration);
        return uiInstance;
    }

    private void TriggerImpact(ActionContext context, List<ActionContext> targetContexts, bool spawnVfx)
    {
        var visualSettings = context.SourceAction.VisualSettings;
        var camera = GetViewport().GetCamera3D() as BattleCamera;
        bool anyCrit = false;

        foreach (var targetCtx in targetContexts)
        {
            var target = targetCtx.CurrentTarget;
            var result = targetCtx.GetResult(target);

            // 1. Spawn Impact VFX
            if (spawnVfx && visualSettings?.ImpactVfx != null && target is Node3D target3D)
            {
                SpawnVfx(visualSettings.ImpactVfx, target3D, visualSettings.ImpactVfxOffset, parent: false);
            }

            // 2. Play Hit/Dodge Animation
            PlayTargetAnimation(target, result, visualSettings);
            
            // 3. Camera Shake
            if (result.IsHit && camera != null)
            {
                // Stronger shake for crits
                float intensity = result.IsCritical ? 1.0f : 0.2f;
                camera.Shake(intensity);
            }

            if (result.IsCritical) anyCrit = true;
        }

        // 4. Critical Flash
        if (anyCrit && _screenEffects != null)
        {
            _screenEffects.Flash(Colors.White, 0.15f);
        }
    }

    private async Task PlayTravelVfxSequence(VisualActionSettings settings, Node3D initiator3D, List<ActionContext> targetContexts)
    {
        if (settings.TravelDelay > 0)
        {
            await ToSignal(GetTree().CreateTimer(settings.TravelDelay), SceneTreeTimer.SignalName.Timeout);
        }

        foreach (var targetCtx in targetContexts)
        {
            if (targetCtx.CurrentTarget is Node3D target3D)
            {
                SpawnTravelVfx(settings.TravelVfx, initiator3D.GlobalPosition, target3D.GlobalPosition, settings.TravelDuration);
            }
        }
    }

    public async Task PlayReaction(List<ActionContext> targetContexts)
    {
        foreach (var ctx in targetContexts)
        {
            var target = ctx.CurrentTarget;
            var result = ctx.GetResult(target);

            // Spawn Damage Numbers
            SpawnDamageNumber(target, result, ctx.LastTimedHitRating);
            // If "10000 Needles", we might loop here spawning numbers
        }

        // Wait a moment for reactions to settle
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
    }

    private void SpawnDamageNumber(Node target, ActionResult result, TimedHitRating rating)
    {
        if (_damageNumberScene == null || !result.IsHit) return;
        
        var instance = _damageNumberScene.Instantiate<DamageNumber>();
        AddChild(instance);
        
        instance.Configure(target, result.TotalDamage, result.IsCritical, result.IsHeal, rating, result.DamageElements);
    }

    private void PlayTargetAnimation(Node target, ActionResult result, VisualActionSettings settings)
    {
        if (target is BaseCharacter character && character.AnimationPlayer != null)
        {
            string anim = result.IsHit ? (settings?.TargetHitAnimation ?? "Hit") : "Dodge";
            if (!string.IsNullOrEmpty(anim) && character.AnimationPlayer.HasAnimation(anim))
            {
                character.AnimationPlayer.Play(anim);
            }
        }
    }

    public void PlayDeathEffect(Node combatant, ActionContext killingContext)
    {
        if (combatant is not Node3D combatant3D) return;

        // 1. Play Death Animation
        if (combatant is BaseCharacter character && character.AnimationPlayer != null)
        {
            if (character.AnimationPlayer.HasAnimation("Death"))
            {
                character.AnimationPlayer.Play("Death");
            }
        }

        // 2. Determine VFX based on killing context
        PackedScene vfxToPlay = _defaultDeathVfx;

        // Example: Check for element in context components
        if (killingContext != null)
        {
            var damageComp = killingContext.GetComponent<DamageComponent>();
            if (damageComp != null)
            {
                // Assuming DamageComponent has an Element property (enum or string)
                // This is pseudo-code based on typical implementation
                if (damageComp.ElementalWeights.GetValueOrDefault(ElementType.Fire, 0.0f) > 0.5f) 
                {
                    vfxToPlay = _fireDeathVfx ?? _defaultDeathVfx;
                }
            }
        }

        // 3. Spawn VFX
        if (vfxToPlay != null)
        {
            var vfxInstance = SpawnVfx(vfxToPlay, combatant3D, Vector3.Zero, false);
            
            // If this is a special death effect script, configure it with the target
            if (vfxInstance is IDeathEffect deathEffect)
            {
                deathEffect.Configure(combatant3D);
            }
        }
        
        // 4. Fade out sprite (optional fallback if no animation)
        // var tween = CreateTween();
        // tween.TweenProperty(combatant3D, "scale", Vector3.Zero, 0.5f);
    }

    private Node3D SpawnVfx(PackedScene vfxScene, Node3D target, Vector3 offset, bool parent)
    {
        var vfx = vfxScene.Instantiate<Node3D>();
        if (parent)
        {
            target.AddChild(vfx);
            vfx.Position = offset;
        }
        else
        {
            // Add to CurrentScene so it cleans up automatically if the battle ends.
            (GetTree().CurrentScene ?? this).AddChild(vfx);
            vfx.GlobalPosition = target.GlobalPosition + offset;
        }
        // Note: The VFX scene is responsible for queueing itself free (e.g. via AnimationPlayer or Timer)
        return vfx;
    }

    private void SpawnTravelVfx(PackedScene vfxScene, Vector3 startPos, Vector3 endPos, float duration)
    {
        var vfx = vfxScene.Instantiate<Node3D>();
        // Use CurrentScene or this node to ensure cleanup on scene change.
        // Attaching to Root can cause leaks if the tween is interrupted by a scene change.
        (GetTree().CurrentScene ?? this).AddChild(vfx);
        vfx.GlobalPosition = startPos;
        vfx.LookAt(endPos, Vector3.Up);

        var tween = CreateTween();
        tween.TweenProperty(vfx, "global_position", endPos, duration);
        tween.Finished += () => 
        {
            if (GodotObject.IsInstanceValid(vfx))
            {
                vfx.QueueFree();
            }
        };
    }

    public void PlayTimedHitEffect(TimedHitRating rating, ActionContext context, TimedHitSettings settings)
    {
        GD.Print($"[BattleAnimator] Timed Hit Resolved: {rating}");

        if (_perfectHitVfxScene != null)
        {
            // Find the UI associated with this hit
            if (context != null && settings != null && _activeUIs.TryGetValue((context, settings), out var ui) && IsInstanceValid(ui))
            {
                // Instantiate the VFX
                var vfx = _perfectHitVfxScene.Instantiate<Control>();
                AddChild(vfx);
                
                // Center it over the active timing ring
                // Assuming the VFX scene is a Control with centered content or particles
                vfx.GlobalPosition = ui.GlobalPosition + (ui.Size / 2.0f) - (vfx.Size / 2.0f);
                
                // Note: The VFX scene should handle its own animation and queue_free() 
            }
        }

        // Apply Camera Effects
        if (settings != null)
        {
            CameraEffect effect = null;
            if (rating == TimedHitRating.Perfect)
            {
                effect = settings.PerfectCameraEffect;
            }
            else if (rating == TimedHitRating.Great)
            {
                effect = settings.GreatCameraEffect;
            }

            // Try to find the active BattleCamera
            var camera = GetViewport().GetCamera3D() as BattleCamera;
            if (effect != null && camera != null)
            {
                effect.Apply(camera);
            }
        }
    }

    private Vector3? CalculateApproachPosition(Node3D initiator, List<ActionContext> targets, VisualActionSettings settings)
    {
        if (targets.Count == 0) return null;

        // Filter for valid 3D targets
        var validTargets = targets.Select(t => t.CurrentTarget).OfType<Node3D>().ToList();
        if (validTargets.Count == 0) return null;

        if (validTargets.Count == 1)
        {
            return GetPositionNearTarget(initiator, validTargets[0], settings.CloseDistanceOffset);
        }
        else
        {
            switch (settings.ApproachStrategy)
            {
                case MultiTargetApproachStrategy.First:
                    return GetPositionNearTarget(initiator, validTargets[0], settings.CloseDistanceOffset);
                
                case MultiTargetApproachStrategy.Average:
                    Vector3 avgPos = Vector3.Zero;
                    foreach (var t in validTargets) avgPos += t.GlobalPosition;
                    avgPos /= validTargets.Count;
                    // We still want to stop 'Offset' distance away from the center point, relative to where we started
                    Vector3 dir = (avgPos - initiator.GlobalPosition).Normalized();
                    return avgPos - (dir * settings.CloseDistanceOffset);

                case MultiTargetApproachStrategy.None:
                default:
                    return null;
            }
        }
    }

    private Vector3 GetPositionNearTarget(Node3D initiator, Node3D target, float offset)
    {
        // Calculate target radius based on visuals (AABB)
        float targetRadius = 0.5f; // Default
        foreach (var child in target.GetChildren())
        {
            if (child is GeometryInstance3D visual && visual.Visible)
            {
                // AABB is local, scale applies to it. We approximate radius from X/Z extent.
                var aabb = visual.GetAabb();
                float scaledX = aabb.Size.X * visual.Scale.X;
                float scaledZ = aabb.Size.Z * visual.Scale.Z;
                targetRadius = Mathf.Max(targetRadius, Mathf.Max(scaledX, scaledZ) / 2.0f);
            }
        }

        Vector3 direction = (target.GlobalPosition - initiator.GlobalPosition).Normalized();
        // Stop at edge of target radius + offset
        return target.GlobalPosition - (direction * (targetRadius + offset));
    }

    private async Task MoveCharacter(BaseCharacter character, Vector3 targetPos, bool isReturning = false)
    {
        float moveSpeed = 15.0f; // Fast run for combat
        float dist = character.GlobalPosition.DistanceTo(targetPos);
        float duration = dist / moveSpeed;

        if (duration <= 0.05f) return;

        // Play Run Animation
        string runAnim = "Run"; // Standard run
        if (isReturning && character.AnimationPlayer != null && character.AnimationPlayer.HasAnimation("RunBack"))
        {
            runAnim = "RunBack"; // Optional backward run
        }

        if (character.AnimationPlayer != null && character.AnimationPlayer.HasAnimation(runAnim))
        {
            character.AnimationPlayer.Play(runAnim);
        }

        // Face target if moving forward
        if (!isReturning)
        {
            //character.LookAt(new Vector3(targetPos.X, character.GlobalPosition.Y, targetPos.Z), Vector3.Up);
        }

        var tween = CreateTween();
        tween.TweenProperty(character, "global_position", targetPos, duration)
            .SetTrans(Tween.TransitionType.Quad).SetEase(isReturning ? Tween.EaseType.Out : Tween.EaseType.InOut);
        
        await ToSignal(tween, Tween.SignalName.Finished);

        // Return to Idle
        if (character.AnimationPlayer != null && character.AnimationPlayer.HasAnimation("Idle"))
            character.AnimationPlayer.Play("Idle");
    }
}
