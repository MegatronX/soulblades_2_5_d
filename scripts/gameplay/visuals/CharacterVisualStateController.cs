using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Keeps battle-facing character visuals in sync with live status/stat state:
/// animation speed, persistent tint/shader, and default idle animation.
/// </summary>
[GlobalClass]
public partial class CharacterVisualStateController : Node
{
    public const string NodeName = "CharacterVisualStateController";

    private readonly struct MirrorImageVisualConfig
    {
        public readonly int Count;
        public readonly float Alpha;
        public readonly float Spread;
        public readonly float DriftSpeed;

        public MirrorImageVisualConfig(int count, float alpha, float spread, float driftSpeed)
        {
            Count = Mathf.Clamp(count, 1, 8);
            Alpha = Mathf.Clamp(alpha, 0f, 1f);
            Spread = Mathf.Max(0f, spread);
            DriftSpeed = Mathf.Max(0f, driftSpeed);
        }

        public static MirrorImageVisualConfig Default => new(1, 0f, 0f, 0f);

        public bool ApproxEquals(MirrorImageVisualConfig other)
        {
            return Count == other.Count
                && Mathf.Abs(Alpha - other.Alpha) <= 0.0001f
                && Mathf.Abs(Spread - other.Spread) <= 0.0001f
                && Mathf.Abs(DriftSpeed - other.DriftSpeed) <= 0.0001f;
        }
    }

    [ExportGroup("Idle Animation")]
    [Export]
    private string _idleAnimationName = "Idle";

    [Export]
    private string _injuredAnimationName = "Injured";

    [Export(PropertyHint.Range, "0,1,0.01")]
    private float _injuredHpThreshold = 0.25f;

    [ExportGroup("Speed Feedback")]
    [Export]
    private bool _enableSpeedDrivenAnimationRate = true;

    [Export(PropertyHint.Range, "0.1,8,0.01")]
    private float _minAnimationSpeedScale = 0.5f;

    [Export(PropertyHint.Range, "0.1,8,0.01")]
    private float _maxAnimationSpeedScale = 3.0f;

    [Export(PropertyHint.Range, "0.1,4,0.01")]
    private float _speedRatioExponent = 1.0f;

    [Export]
    private bool _useBaseSpeedAsReference = true;

    [Export(PropertyHint.Range, "0,9999,1")]
    private int _referenceSpeedOverride = 0;

    [Export]
    private bool _pollSpeedStatWhileInBattle = true;

    [Export]
    private bool _debugLogSpeedUpdates = false;

    private BaseCharacter _character;
    private AnimationPlayer _animationPlayer;
    private StatsComponent _stats;
    private StatusEffectManager _statusManager;

    private readonly List<SpriteBase3D> _spriteVisuals = new();
    private readonly Dictionary<SpriteBase3D, Color> _baseSpriteModulate = new();
    private readonly Dictionary<SpriteBase3D, Material> _baseSpriteMaterials = new();
    private readonly Dictionary<SpriteBase3D, Vector3> _baseSpriteScale = new();
    private readonly Dictionary<SpriteBase3D, List<SpriteBase3D>> _mirrorImageVisuals = new();

    private float _baseAnimationPlayerSpeedScale = 1.0f;
    private float _referenceSpeedStat = 1.0f;
    private int _cachedMaxHp = 1;
    private string _resolvedIdleAnimationName = "Idle";
    private int _lastObservedSpeedStat = -1;
    private int _speedFeedbackLockCount = 0;
    private int _pendingLockedSpeedStat = -1;
    private MirrorImageVisualConfig _activeMirrorConfig = MirrorImageVisualConfig.Default;
    private bool _mirrorImagesEnabled = false;
    private float _mirrorImageTime = 0f;
    private bool _isInitialized;

    public override void _Ready()
    {
        _character = GetParent() as BaseCharacter;
        _animationPlayer = _character?.AnimationPlayer ?? _character?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _stats = _character?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        _statusManager = _character?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);

        SubscribeSignals();
        // BaseCharacter randomizes animation speed in its own _Ready. Defer so we baseline after that.
        CallDeferred(nameof(InitializeVisualState));
    }

    public override void _ExitTree()
    {
        ClearMirrorImages();
        UnsubscribeSignals();
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized) return;
        if (_pollSpeedStatWhileInBattle && _enableSpeedDrivenAnimationRate && _stats != null)
        {
            int speed = Mathf.Max(1, _stats.GetStatValue(StatType.Speed));
            if (speed != _lastObservedSpeedStat)
            {
                ApplyAnimationSpeedFromStat(speed);
            }
        }

        UpdateMirrorImageVisuals((float)delta);
    }

    public string GetResolvedIdleAnimationName()
    {
        return _resolvedIdleAnimationName;
    }

    public void PushSpeedFeedbackLock()
    {
        _speedFeedbackLockCount = Mathf.Max(0, _speedFeedbackLockCount) + 1;
    }

    public void PopSpeedFeedbackLock(bool applyDeferredUpdate = true)
    {
        if (_speedFeedbackLockCount <= 0)
        {
            _speedFeedbackLockCount = 0;
            return;
        }

        _speedFeedbackLockCount--;
        if (_speedFeedbackLockCount > 0) return;

        if (!applyDeferredUpdate) return;

        int pendingSpeed = _pendingLockedSpeedStat;
        _pendingLockedSpeedStat = -1;
        if (pendingSpeed > 0)
        {
            ApplyAnimationSpeedFromStat(pendingSpeed);
            return;
        }

        if (_stats != null)
        {
            ApplyAnimationSpeedFromStat(Mathf.Max(1, _stats.GetStatValue(StatType.Speed)));
        }
    }

    public void PlayResolvedIdleAnimation()
    {
        if (_animationPlayer == null) return;
        if (string.IsNullOrEmpty(_resolvedIdleAnimationName)) return;
        if (!_animationPlayer.HasAnimation(_resolvedIdleAnimationName)) return;
        _animationPlayer.Play(_resolvedIdleAnimationName);
    }

    public void RefreshAllVisualState(bool playIdleIfPossible = false, bool rebaselineAnimationSpeed = false)
    {
        if (!_isInitialized) return;

        if (rebaselineAnimationSpeed)
        {
            _referenceSpeedStat = ResolveReferenceSpeed();
        }

        RefreshAnimationSpeed();
        RefreshPersistentVisuals();
        if (_stats != null)
        {
            _cachedMaxHp = Mathf.Max(1, _stats.GetStatValue(StatType.HP));
        }
        RefreshIdleAnimation(playIdleIfPossible);
    }

    private void InitializeVisualState()
    {
        CacheVisualNodes();
        _baseAnimationPlayerSpeedScale = _animationPlayer?.SpeedScale ?? 1.0f;

        if (_stats != null)
        {
            _referenceSpeedStat = ResolveReferenceSpeed();
            _cachedMaxHp = Mathf.Max(1, _stats.GetStatValue(StatType.HP));
        }
        else
        {
            _referenceSpeedStat = 1.0f;
            _cachedMaxHp = 1;
        }

        _isInitialized = true;
        RefreshAllVisualState(playIdleIfPossible: true, rebaselineAnimationSpeed: true);
    }

    private void SubscribeSignals()
    {
        if (_stats != null)
        {
            _stats.StatValueChanged += OnStatValueChanged;
            _stats.CurrentHPChanged += OnCurrentHpChanged;
        }

        if (_statusManager != null)
        {
            _statusManager.StatusEffectApplied += OnStatusEffectChanged;
            _statusManager.StatusEffectRemoved += OnStatusEffectChanged;
        }
    }

    private void UnsubscribeSignals()
    {
        if (_stats != null)
        {
            _stats.StatValueChanged -= OnStatValueChanged;
            _stats.CurrentHPChanged -= OnCurrentHpChanged;
        }

        if (_statusManager != null)
        {
            _statusManager.StatusEffectApplied -= OnStatusEffectChanged;
            _statusManager.StatusEffectRemoved -= OnStatusEffectChanged;
        }
    }

    private void OnStatusEffectChanged(StatusEffect effectData, Node owner)
    {
        if (!_isInitialized) return;
        if (_character != null && owner != null && owner != _character) return;

        RefreshAllVisualState(playIdleIfPossible: true);
    }

    private void OnCurrentHpChanged(int newHp, int maxHp)
    {
        if (!_isInitialized) return;
        _cachedMaxHp = Mathf.Max(1, maxHp);
        RefreshIdleAnimation(playIdleIfPossible: true);
    }

    private void OnStatValueChanged(long statTypeValue, int newValue)
    {
        if (!_isInitialized) return;

        var statType = (StatType)statTypeValue;
        if (statType == StatType.Speed)
        {
            ApplyAnimationSpeedFromStat(newValue);
            return;
        }

        if (statType == StatType.HP)
        {
            _cachedMaxHp = Mathf.Max(1, newValue);
            RefreshIdleAnimation(playIdleIfPossible: true);
        }
    }

    private void RefreshAnimationSpeed()
    {
        if (!_enableSpeedDrivenAnimationRate) return;
        if (_animationPlayer == null || _stats == null) return;

        int speed = Mathf.Max(1, _stats.GetStatValue(StatType.Speed));
        ApplyAnimationSpeedFromStat(speed);
    }

    private void ApplyAnimationSpeedFromStat(int speedValue)
    {
        if (!_enableSpeedDrivenAnimationRate) return;
        if (_animationPlayer == null) return;

        float speed = Mathf.Max(1f, speedValue);
        int roundedSpeed = Mathf.RoundToInt(speed);
        if (_speedFeedbackLockCount > 0)
        {
            _pendingLockedSpeedStat = roundedSpeed;
            return;
        }

        _lastObservedSpeedStat = roundedSpeed;

        if (_referenceSpeedStat <= 0f)
        {
            _referenceSpeedStat = ResolveReferenceSpeed();
        }

        float reference = Mathf.Max(1f, _referenceSpeedStat);
        float ratio = Mathf.Max(0.01f, speed / reference);
        ratio = Mathf.Pow(ratio, _speedRatioExponent);

        float previousScale = _animationPlayer.SpeedScale;
        float scaled = _baseAnimationPlayerSpeedScale * ratio;
        _animationPlayer.SpeedScale = Mathf.Clamp(scaled, _minAnimationSpeedScale, _maxAnimationSpeedScale);

        if (_debugLogSpeedUpdates && Mathf.Abs(previousScale - _animationPlayer.SpeedScale) > 0.0001f)
        {
            string ownerName = _character?.Name ?? Name;
            GD.Print($"[CharacterVisualStateController] {ownerName} speed={speed} reference={reference} animSpeedScale={_animationPlayer.SpeedScale:0.###}");
        }
    }

    private float ResolveReferenceSpeed()
    {
        if (_referenceSpeedOverride > 0)
        {
            return _referenceSpeedOverride;
        }

        if (_stats == null) return 1.0f;

        if (_useBaseSpeedAsReference)
        {
            int baseSpeed = _stats.GetBaseStatValue(StatType.Speed);
            if (baseSpeed > 0)
            {
                return baseSpeed;
            }
        }

        return Mathf.Max(1f, _stats.GetStatValue(StatType.Speed));
    }

    private void RefreshIdleAnimation(bool playIdleIfPossible)
    {
        _resolvedIdleAnimationName = ResolveIdleAnimationName();

        if (!playIdleIfPossible) return;
        if (_animationPlayer == null) return;

        if (!_animationPlayer.IsPlaying() || IsIdleAnimation(_animationPlayer.CurrentAnimation))
        {
            PlayResolvedIdleAnimation();
        }
    }

    private string ResolveIdleAnimationName()
    {
        if (_animationPlayer == null) return _idleAnimationName;

        bool hasIdle = !string.IsNullOrEmpty(_idleAnimationName) && _animationPlayer.HasAnimation(_idleAnimationName);
        bool hasInjured = !string.IsNullOrEmpty(_injuredAnimationName) && _animationPlayer.HasAnimation(_injuredAnimationName);
        bool shouldUseInjured = hasInjured && (IsInjuredHpState() || HasStatusDrivenInjuredIdle());

        if (shouldUseInjured)
        {
            return _injuredAnimationName;
        }

        if (hasIdle) return _idleAnimationName;
        if (hasInjured) return _injuredAnimationName;
        return string.Empty;
    }

    private bool IsInjuredHpState()
    {
        if (_stats == null) return false;
        if (_injuredHpThreshold <= 0f) return false;
        int maxHp = Mathf.Max(1, _cachedMaxHp);
        float hpPercent = (float)_stats.CurrentHP / maxHp;
        return hpPercent <= _injuredHpThreshold;
    }

    private bool HasStatusDrivenInjuredIdle()
    {
        if (_statusManager == null) return false;

        var activeEffects = _statusManager.GetActiveEffects();
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var effect = activeEffects[i]?.EffectData;
            if (effect != null && effect.ForceInjuredIdleAnimation)
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshPersistentVisuals()
    {
        if (_spriteVisuals.Count == 0) return;

        bool hasTint = TryResolveActiveTint(out Color tintColor, out float tintStrength);
        bool hasShader = TryResolvePersistentShader(out ShaderMaterial shaderMaterial);
        float scaleMultiplier = ResolveActiveScaleMultiplier();

        foreach (var sprite in _spriteVisuals)
        {
            if (sprite == null || !GodotObject.IsInstanceValid(sprite)) continue;

            Color baseModulate = _baseSpriteModulate.GetValueOrDefault(sprite, Colors.White);
            sprite.Modulate = hasTint
                ? BlendTint(baseModulate, tintColor, tintStrength)
                : baseModulate;

            Vector3 baseScale = _baseSpriteScale.GetValueOrDefault(sprite, Vector3.One);
            sprite.Scale = baseScale * scaleMultiplier;

            if (sprite is GeometryInstance3D geometry)
            {
                Material baseMaterial = _baseSpriteMaterials.GetValueOrDefault(sprite);
                geometry.MaterialOverride = hasShader ? shaderMaterial : baseMaterial;
            }
        }

        SyncMirrorImageVisualState();
    }

    private float ResolveActiveScaleMultiplier()
    {
        if (_statusManager == null) return 1f;

        float product = 1f;
        bool anyOverride = false;

        var activeEffects = _statusManager.GetActiveEffects();
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var effect = activeEffects[i]?.EffectData;
            if (effect == null) continue;

            float multiplier = effect.ScaleMultiplier;
            if (multiplier <= 0f) continue;
            if (Mathf.IsEqualApprox(multiplier, 1f)) continue;

            product *= multiplier;
            anyOverride = true;
        }

        if (!anyOverride) return 1f;
        return Mathf.Clamp(product, 0.2f, 4f);
    }

    private bool TryResolveActiveTint(out Color tintColor, out float tintStrength)
    {
        tintColor = Colors.White;
        tintStrength = 0f;
        if (_statusManager == null) return false;

        bool found = false;
        int bestPriority = int.MinValue;
        int bestIndex = -1;

        var activeEffects = _statusManager.GetActiveEffects();
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var effect = activeEffects[i]?.EffectData;
            if (effect == null) continue;
            if (!effect.TryGetActiveTint(out Color candidateColor, out float candidateStrength)) continue;

            int priority = effect.Priority;
            if (!found || priority > bestPriority || (priority == bestPriority && i > bestIndex))
            {
                found = true;
                bestPriority = priority;
                bestIndex = i;
                tintColor = candidateColor;
                tintStrength = candidateStrength;
            }
        }

        return found;
    }

    private bool TryResolvePersistentShader(out ShaderMaterial shaderMaterial)
    {
        shaderMaterial = null;
        if (_statusManager == null) return false;

        bool found = false;
        int bestPriority = int.MinValue;
        int bestIndex = -1;

        var activeEffects = _statusManager.GetActiveEffects();
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var effect = activeEffects[i]?.EffectData;
            if (effect?.PersistentShader == null) continue;

            int priority = effect.Priority;
            if (!found || priority > bestPriority || (priority == bestPriority && i > bestIndex))
            {
                found = true;
                bestPriority = priority;
                bestIndex = i;
                shaderMaterial = effect.PersistentShader;
            }
        }

        return found;
    }

    private static Color BlendTint(Color baseColor, Color tintColor, float strength)
    {
        float t = Mathf.Clamp(strength, 0f, 1f);
        var multiplied = new Color(
            baseColor.R * tintColor.R,
            baseColor.G * tintColor.G,
            baseColor.B * tintColor.B,
            baseColor.A);
        return baseColor.Lerp(multiplied, t);
    }

    private bool TryResolveMirrorImageConfig(out MirrorImageVisualConfig config)
    {
        config = MirrorImageVisualConfig.Default;
        if (_statusManager == null) return false;

        bool found = false;
        int bestPriority = int.MinValue;
        int bestIndex = -1;

        var activeEffects = _statusManager.GetActiveEffects();
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var effect = activeEffects[i]?.EffectData;
            if (effect == null) continue;
            if (!effect.TryGetMirrorImageConfig(out int count, out float alpha, out float spread, out float driftSpeed)) continue;

            int priority = effect.Priority;
            if (!found || priority > bestPriority || (priority == bestPriority && i > bestIndex))
            {
                found = true;
                bestPriority = priority;
                bestIndex = i;
                config = new MirrorImageVisualConfig(count, alpha, spread, driftSpeed);
            }
        }

        return found;
    }

    private void SyncMirrorImageVisualState()
    {
        if (!TryResolveMirrorImageConfig(out var config))
        {
            ClearMirrorImages();
            return;
        }

        bool needsRebuild = !_mirrorImagesEnabled
            || !_activeMirrorConfig.ApproxEquals(config)
            || NeedsMirrorImageRebuild(config.Count);

        _activeMirrorConfig = config;
        _mirrorImagesEnabled = true;

        if (needsRebuild)
        {
            RebuildMirrorImages(config.Count);
        }
    }

    private bool NeedsMirrorImageRebuild(int requiredCount)
    {
        if (_mirrorImageVisuals.Count != _spriteVisuals.Count) return true;

        foreach (var source in _spriteVisuals)
        {
            if (source == null || !GodotObject.IsInstanceValid(source)) return true;
            if (!_mirrorImageVisuals.TryGetValue(source, out var ghosts)) return true;
            if (ghosts == null || ghosts.Count != requiredCount) return true;
        }

        return false;
    }

    private void RebuildMirrorImages(int count)
    {
        ClearMirrorImages();

        foreach (var source in _spriteVisuals)
        {
            if (source == null || !GodotObject.IsInstanceValid(source)) continue;

            var parent = source.GetParent();
            if (parent == null || !GodotObject.IsInstanceValid(parent)) continue;

            var ghosts = new List<SpriteBase3D>(count);
            for (int i = 0; i < count; i++)
            {
                var ghost = CreateMirrorImageGhost(source, i);
                if (ghost == null) continue;

                parent.AddChild(ghost);
                int sourceIndex = source.GetIndex();
                int maxIndex = parent.GetChildCount() - 1;
                parent.MoveChild(ghost, Mathf.Clamp(sourceIndex, 0, maxIndex));
                ghosts.Add(ghost);
            }

            if (ghosts.Count > 0)
            {
                _mirrorImageVisuals[source] = ghosts;
            }
        }

        _mirrorImagesEnabled = _mirrorImageVisuals.Count > 0;
        _mirrorImageTime = 0f;
        UpdateMirrorImageVisuals(0f);
    }

    private static SpriteBase3D CreateMirrorImageGhost(SpriteBase3D source, int index)
    {
        if (source == null || !GodotObject.IsInstanceValid(source)) return null;
        if (source.Duplicate() is not SpriteBase3D ghost) return null;

        ghost.Name = $"{source.Name}_MirrorImage{index + 1}";
        ghost.Visible = source.Visible;
        ghost.Modulate = new Color(1f, 1f, 1f, 0f);
        while (ghost.GetChildCount() > 0)
        {
            ghost.GetChild(0).QueueFree();
        }

        return ghost;
    }

    private void UpdateMirrorImageVisuals(float delta)
    {
        if (!_mirrorImagesEnabled || _mirrorImageVisuals.Count == 0) return;
        _mirrorImageTime += Mathf.Max(0f, delta);

        var staleSources = new List<SpriteBase3D>();
        foreach (var pair in _mirrorImageVisuals)
        {
            var source = pair.Key;
            var ghosts = pair.Value;
            if (source == null || !GodotObject.IsInstanceValid(source))
            {
                QueueFreeGhosts(ghosts);
                staleSources.Add(source);
                continue;
            }

            int count = ghosts?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                var ghost = ghosts[i];
                if (ghost == null || !GodotObject.IsInstanceValid(ghost)) continue;
                SyncMirrorGhostFromSource(source, ghost, i, count);
            }
        }

        for (int i = 0; i < staleSources.Count; i++)
        {
            _mirrorImageVisuals.Remove(staleSources[i]);
        }

        if (_mirrorImageVisuals.Count == 0)
        {
            _mirrorImagesEnabled = false;
        }
    }

    private void SyncMirrorGhostFromSource(SpriteBase3D source, SpriteBase3D ghost, int index, int count)
    {
        float center = (count - 1) * 0.5f;
        float normalized = center > 0f ? (index - center) / center : 0f;
        float phase = (_mirrorImageTime * _activeMirrorConfig.DriftSpeed) + (index * 0.75f);
        float lateral = (normalized * _activeMirrorConfig.Spread) + (Mathf.Sin(phase) * _activeMirrorConfig.Spread * 0.22f);
        float vertical = Mathf.Cos(phase * 1.13f) * _activeMirrorConfig.Spread * 0.08f;

        ghost.Position = source.Position + new Vector3(lateral, vertical, 0f);
        ghost.Rotation = source.Rotation;
        ghost.Scale = source.Scale * (1f + (Mathf.Abs(normalized) * 0.03f));
        ghost.Visible = source.Visible;

        Color srcModulate = source.Modulate;
        float alpha = _activeMirrorConfig.Alpha * Mathf.Lerp(1f, 0.55f, Mathf.Abs(normalized));
        ghost.Modulate = new Color(srcModulate.R, srcModulate.G, srcModulate.B, Mathf.Clamp(srcModulate.A * alpha, 0f, 1f));

        if (source is Sprite3D sourceSprite && ghost is Sprite3D ghostSprite)
        {
            ghostSprite.Texture = sourceSprite.Texture;
            ghostSprite.Hframes = sourceSprite.Hframes;
            ghostSprite.Vframes = sourceSprite.Vframes;
            ghostSprite.Frame = sourceSprite.Frame;
        }
    }

    private void ClearMirrorImages()
    {
        foreach (var pair in _mirrorImageVisuals)
        {
            QueueFreeGhosts(pair.Value);
        }

        _mirrorImageVisuals.Clear();
        _activeMirrorConfig = MirrorImageVisualConfig.Default;
        _mirrorImagesEnabled = false;
        _mirrorImageTime = 0f;
    }

    private static void QueueFreeGhosts(List<SpriteBase3D> ghosts)
    {
        if (ghosts == null) return;

        for (int i = 0; i < ghosts.Count; i++)
        {
            var ghost = ghosts[i];
            if (ghost == null || !GodotObject.IsInstanceValid(ghost)) continue;
            ghost.QueueFree();
        }
    }

    private bool IsIdleAnimation(string animationName)
    {
        if (string.IsNullOrEmpty(animationName)) return true;
        if (!string.IsNullOrEmpty(_idleAnimationName) && string.Equals(animationName, _idleAnimationName, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(_injuredAnimationName) && string.Equals(animationName, _injuredAnimationName, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private void CacheVisualNodes()
    {
        _spriteVisuals.Clear();
        _baseSpriteModulate.Clear();
        _baseSpriteMaterials.Clear();
        _baseSpriteScale.Clear();

        if (_character == null) return;

        CacheVisualNodesRecursive(_character);
    }

    private void CacheVisualNodesRecursive(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is SpriteBase3D sprite)
            {
                _spriteVisuals.Add(sprite);
                _baseSpriteModulate[sprite] = sprite.Modulate;
                _baseSpriteScale[sprite] = sprite.Scale;
                if (sprite is GeometryInstance3D geometry)
                {
                    _baseSpriteMaterials[sprite] = geometry.MaterialOverride;
                }
            }

            CacheVisualNodesRecursive(child);
        }
    }
}
