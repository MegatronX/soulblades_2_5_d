using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Keeps battle-facing character visuals in sync with live status/stat state:
/// animation speed, persistent tint/shader, and default idle animation.
/// </summary>
[GlobalClass]
public partial class CharacterVisualStateController : Node
{
    public const string NodeName = "CharacterVisualStateController";
    private const string GeneratedVisualMetaKey = "generated_visual";
    private const int ShaderOverlayLayerCount = 2;
    private const int ShaderOverlayBackIndex = 0;
    private const int ShaderOverlayFrontIndex = 1;
    // Expansion maps directly to overlay scale (1 + expansion) and shader remaps UVs accordingly.
    private const float ShaderOverlayExpansionToScaleFactor = 1.0f;

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
    private AbilityManager _abilityManager;

    private readonly List<SpriteBase3D> _spriteVisuals = new();
    private readonly Dictionary<SpriteBase3D, Color> _baseSpriteModulate = new();
    private readonly Dictionary<SpriteBase3D, Material> _baseSpriteMaterials = new();
    private readonly Dictionary<SpriteBase3D, Vector3> _baseSpriteScale = new();
    private readonly Dictionary<SpriteBase3D, Vector2> _baseSpriteOffset = new();
    private readonly Dictionary<SpriteBase3D, List<SpriteBase3D>> _mirrorImageVisuals = new();
    private readonly Dictionary<SpriteBase3D, List<Sprite3D>> _shaderOverlayVisuals = new();
    private readonly Dictionary<SpriteBase3D, float> _shaderOverlayScaleBySource = new();
    private readonly Dictionary<SpriteBase3D, ShaderMaterial> _runtimeShaderOverrides = new();
    private readonly Dictionary<Shader, bool> _shaderTextureUniformPresenceCache = new();
    private readonly Dictionary<Shader, bool> _shaderGhostExpansionUniformPresenceCache = new();
    private readonly Dictionary<Shader, bool> _shaderOverlayScaleUniformPresenceCache = new();

    private float _baseAnimationPlayerSpeedScale = 1.0f;
    private float _referenceSpeedStat = 1.0f;
    private int _cachedMaxHp = 1;
    private string _resolvedIdleAnimationName = "Idle";
    private int _lastObservedSpeedStat = -1;
    private int _speedFeedbackLockCount = 0;
    private int _pendingLockedSpeedStat = -1;
    private MirrorImageVisualSettings _activeMirrorConfig = MirrorImageVisualSettings.Default;
    private bool _mirrorImagesEnabled = false;
    private float _mirrorImageTimeSeconds = 0f;
    private HoverVisualSettings _activeHoverConfig = HoverVisualSettings.Default;
    private bool _hoverVisualEnabled = false;
    private float _hoverTimeSeconds = 0f;
    private bool _forceInjuredIdleFromVisuals = false;
    private bool _isInitialized;

    public override void _Ready()
    {
        _character = GetParent() as BaseCharacter;
        _animationPlayer = _character?.AnimationPlayer ?? _character?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _stats = _character?.GetNodeOrNull<StatsComponent>(StatsComponent.NodeName);
        _statusManager = _character?.GetNodeOrNull<StatusEffectManager>(StatusEffectManager.NodeName);
        _abilityManager = _character?.GetNodeOrNull<AbilityManager>(AbilityManager.NodeName);

        SubscribeSignals();
        // BaseCharacter randomizes animation speed in its own _Ready. Defer so we baseline after that.
        CallDeferred(nameof(InitializeVisualState));
    }

    public override void _ExitTree()
    {
        ClearHoverVisualState(resetOffsets: true);
        ClearMirrorImages();
        ClearShaderOverlays();
        ClearRuntimeShaderOverrides();
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

        UpdateHoverVisuals((float)delta);
        UpdateMirrorImageVisuals((float)delta);
        UpdateShaderOverlayVisuals();
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

    public int GetActiveMirrorGhostCount()
    {
        if (_mirrorImageVisuals.Count == 0) return 0;

        int total = 0;
        foreach (var pair in _mirrorImageVisuals)
        {
            var ghosts = pair.Value;
            if (ghosts == null) continue;
            for (int i = 0; i < ghosts.Count; i++)
            {
                if (ghosts[i] != null && GodotObject.IsInstanceValid(ghosts[i]))
                {
                    total++;
                }
            }
        }

        return total;
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

        if (_abilityManager != null)
        {
            _abilityManager.EquippedAbilitiesChanged += OnAbilitiesChanged;
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

        if (_abilityManager != null)
        {
            _abilityManager.EquippedAbilitiesChanged -= OnAbilitiesChanged;
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

    private void OnAbilitiesChanged()
    {
        if (!_isInitialized) return;
        RefreshPersistentVisuals();
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
        bool shouldUseInjured = hasInjured && (IsInjuredHpState() || HasForceInjuredIdleVisualState());

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

    private bool HasForceInjuredIdleVisualState()
    {
        return _forceInjuredIdleFromVisuals;
    }

    private void RefreshPersistentVisuals()
    {
        if (_spriteVisuals.Count == 0) return;

        var state = BuildPersistentVisualState();

        bool hasTint = state.TryGetTint(out Color tintColor, out float tintStrength);
        bool hasShader = state.TryGetShader(out ShaderMaterial shaderMaterial);
        float scaleMultiplier = state.GetClampedScale();
        _forceInjuredIdleFromVisuals = state.ForceInjuredIdle;

        var overlaySourcesToKeep = new HashSet<SpriteBase3D>();
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
                if (hasShader && ShouldUseShaderOverlay(shaderMaterial, out float overlayScale))
                {
                    geometry.MaterialOverride = baseMaterial;
                    if (sprite is Sprite3D sourceSprite)
                    {
                        EnsureShaderOverlaySprite(sourceSprite, shaderMaterial, overlayScale);
                        overlaySourcesToKeep.Add(sourceSprite);
                    }
                    else
                    {
                        RemoveShaderOverlaySprite(sprite);
                    }
                }
                else
                {
                    RemoveShaderOverlaySprite(sprite);
                    geometry.MaterialOverride = hasShader
                        ? ResolveShaderMaterialOverride(sprite, shaderMaterial)
                        : baseMaterial;
                }
            }
            else
            {
                RemoveShaderOverlaySprite(sprite);
            }
        }

        CleanupShaderOverlays(overlaySourcesToKeep);
        SyncMirrorImageVisualState(state);
        SyncHoverVisualState(state);
    }

    private BattleVisualStateAccumulator BuildPersistentVisualState()
    {
        var state = new BattleVisualStateAccumulator();
        Node owner = _character ?? GetParent();

        int sourceOrder = 0;
        if (_statusManager != null)
        {
            var activeEffects = _statusManager.GetActiveEffects();
            for (int i = 0; i < activeEffects.Count; i++)
            {
                var instance = activeEffects[i];
                var effect = instance?.EffectData;
                if (effect == null) continue;
                BattleVisualEffectRunner.ContributeStatusPersistent(
                    effect,
                    owner,
                    _statusManager,
                    instance,
                    state,
                    this,
                    sourceOrder);
                sourceOrder++;
            }
        }

        if (_abilityManager != null)
        {
            var equipped = _abilityManager.GetEquippedAbilities();
            for (int i = 0; i < equipped.Count; i++)
            {
                var ability = equipped[i];
                if (ability?.TriggeredEffects == null || ability.TriggeredEffects.Count == 0) continue;

                for (int j = 0; j < ability.TriggeredEffects.Count; j++)
                {
                    var effect = ability.TriggeredEffects[j];
                    if (effect == null) continue;
                    BattleVisualEffectRunner.ContributeAbilityEffectPersistent(
                        effect,
                        ability,
                        owner,
                        state,
                        this,
                        sourceOrder);
                    sourceOrder++;
                }
            }
        }

        return state;
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

    private void ClearRuntimeShaderOverrides()
    {
        _runtimeShaderOverrides.Clear();
        _shaderTextureUniformPresenceCache.Clear();
        _shaderGhostExpansionUniformPresenceCache.Clear();
        _shaderOverlayScaleUniformPresenceCache.Clear();
    }

    private bool ShouldUseShaderOverlay(ShaderMaterial template, out float overlayScaleMultiplier)
    {
        overlayScaleMultiplier = 1f;
        if (template == null || template.Shader == null) return false;
        if (!ShaderUsesGhostExpansion(template.Shader)) return false;

        float expansion = GetShaderFloatParameter(template, "ghost_expansion");
        if (expansion <= 0.0001f) return false;

        overlayScaleMultiplier = 1f + (Mathf.Clamp(expansion, 0f, 1f) * ShaderOverlayExpansionToScaleFactor);
        return true;
    }

    private bool ShaderUsesGhostExpansion(Shader shader)
    {
        if (shader == null) return false;
        if (_shaderGhostExpansionUniformPresenceCache.TryGetValue(shader, out bool cached))
        {
            return cached;
        }

        bool hasUniform = !string.IsNullOrEmpty(shader.Code)
            && shader.Code.Contains("ghost_expansion", StringComparison.Ordinal);
        _shaderGhostExpansionUniformPresenceCache[shader] = hasUniform;
        return hasUniform;
    }

    private bool ShaderUsesOverlayScale(Shader shader)
    {
        if (shader == null) return false;
        if (_shaderOverlayScaleUniformPresenceCache.TryGetValue(shader, out bool cached))
        {
            return cached;
        }

        bool hasUniform = !string.IsNullOrEmpty(shader.Code)
            && shader.Code.Contains("overlay_scale", StringComparison.Ordinal);
        _shaderOverlayScaleUniformPresenceCache[shader] = hasUniform;
        return hasUniform;
    }

    private static float GetShaderFloatParameter(ShaderMaterial material, string parameterName)
    {
        if (material == null || string.IsNullOrEmpty(parameterName)) return 0f;
        Variant value = material.GetShaderParameter(parameterName);
        if (value.VariantType == Variant.Type.Nil) return 0f;

        string text = value.ToString();
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            return parsed;
        }

        return 0f;
    }

    private Material ResolveShaderMaterialOverride(SpriteBase3D sprite, ShaderMaterial template)
    {
        if (template == null) return null;
        if (sprite == null || !GodotObject.IsInstanceValid(sprite)) return template;

        if (!_runtimeShaderOverrides.TryGetValue(sprite, out var runtime)
            || runtime == null
            || !GodotObject.IsInstanceValid(runtime)
            || runtime.Shader != template.Shader)
        {
            runtime = template.Duplicate() as ShaderMaterial;
            if (runtime == null) return template;
            _runtimeShaderOverrides[sprite] = runtime;
        }

        TryBindSpriteTextureToShader(sprite, runtime);
        return runtime;
    }

    private void TryBindSpriteTextureToShader(SpriteBase3D sprite, ShaderMaterial shaderMaterial)
    {
        if (sprite == null || shaderMaterial == null) return;
        if (shaderMaterial.Shader == null) return;

        if (!ShaderUsesTextureAlbedo(shaderMaterial.Shader)) return;

        if (sprite is Sprite3D sprite3D && sprite3D.Texture != null)
        {
            shaderMaterial.SetShaderParameter("texture_albedo", sprite3D.Texture);
        }
    }

    private bool ShaderUsesTextureAlbedo(Shader shader)
    {
        if (shader == null) return false;
        if (_shaderTextureUniformPresenceCache.TryGetValue(shader, out bool cached))
        {
            return cached;
        }

        bool hasUniform = !string.IsNullOrEmpty(shader.Code)
            && shader.Code.Contains("texture_albedo", StringComparison.Ordinal);
        _shaderTextureUniformPresenceCache[shader] = hasUniform;
        return hasUniform;
    }

    private void EnsureShaderOverlaySprite(Sprite3D source, ShaderMaterial shaderTemplate, float overlayScale)
    {
        if (source == null || !GodotObject.IsInstanceValid(source)) return;
        if (shaderTemplate == null) return;

        if (!_shaderOverlayVisuals.TryGetValue(source, out var overlays) || overlays == null)
        {
            overlays = new List<Sprite3D>(ShaderOverlayLayerCount);
            _shaderOverlayVisuals[source] = overlays;
        }

        var parent = source.GetParent();
        if (parent == null || !GodotObject.IsInstanceValid(parent)) return;

        while (overlays.Count < ShaderOverlayLayerCount)
        {
            int overlayIndex = overlays.Count;
            var overlay = source.Duplicate() as Sprite3D ?? new Sprite3D();
            overlay.Name = $"{source.Name}_ShaderOverlay{overlayIndex}";
            overlay.SetMeta(GeneratedVisualMetaKey, true);
            while (overlay.GetChildCount() > 0)
            {
                overlay.GetChild(0).QueueFree();
            }

            parent.AddChild(overlay);
            overlays.Add(overlay);
        }

        _shaderOverlayScaleBySource[source] = overlayScale;

        for (int i = 0; i < overlays.Count; i++)
        {
            var overlay = overlays[i];
            if (overlay == null || !GodotObject.IsInstanceValid(overlay)) continue;

            int sourceIndex = source.GetIndex();
            int maxIndex = parent.GetChildCount() - 1;
            int targetIndex = i == ShaderOverlayBackIndex
                ? Mathf.Clamp(sourceIndex, 0, maxIndex)
                : Mathf.Clamp(sourceIndex + 2, 0, maxIndex);
            parent.MoveChild(overlay, targetIndex);

            ConfigureShaderOverlayRendering(source, overlay, i);
            SyncShaderOverlayFromSource(source, overlay, i);

            if (overlay is GeometryInstance3D overlayGeometry)
            {
                if (ResolveShaderMaterialOverride(overlay, shaderTemplate) is ShaderMaterial shaderOverride)
                {
                    if (ShaderUsesOverlayScale(shaderOverride.Shader))
                    {
                        shaderOverride.SetShaderParameter("overlay_scale", overlayScale);
                    }
                    overlayGeometry.MaterialOverride = shaderOverride;
                }
                else
                {
                    overlayGeometry.MaterialOverride = null;
                }
            }
        }
    }

    private static void ConfigureShaderOverlayRendering(Sprite3D source, Sprite3D overlay, int overlayIndex)
    {
        if (source == null || overlay == null) return;

        if (source is VisualInstance3D sourceVisual && overlay is VisualInstance3D overlayVisual)
        {
            overlayVisual.Layers = sourceVisual.Layers;
        }

        overlay.AlphaCut = SpriteBase3D.AlphaCutMode.Disabled;
        overlay.AlphaScissorThreshold = 0f;
        overlay.NoDepthTest = true;
        overlay.Shaded = false;
        overlay.Transparent = true;
        overlay.RenderPriority = source.RenderPriority + (overlayIndex == ShaderOverlayFrontIndex ? 8 : 3);
    }

    private static float GetSpriteFramePixelWidth(Sprite3D source)
    {
        if (source == null) return 64f;
        var texture = source.Texture;
        float textureWidth = texture?.GetWidth() ?? 64f;
        int hframes = Mathf.Max(1, source.Hframes);
        return Mathf.Max(1f, textureWidth / hframes);
    }

    private void SyncShaderOverlayFromSource(Sprite3D source, Sprite3D overlay, int overlayIndex)
    {
        if (source == null || overlay == null) return;
        if (!GodotObject.IsInstanceValid(source) || !GodotObject.IsInstanceValid(overlay)) return;

        float overlayScale = _shaderOverlayScaleBySource.GetValueOrDefault(source, 1f);
        overlayScale = Mathf.Max(1f, overlayScale);

        float direction = overlayIndex == ShaderOverlayFrontIndex ? 1f : -1f;
        float frameWidthPixels = GetSpriteFramePixelWidth(source);
        float lateralPixelOffset = Mathf.Max(1f, frameWidthPixels * 0.03f);
        float offsetDivisor = overlayScale > 0.0001f ? overlayScale : 1f;

        overlay.Position = source.Position;
        overlay.Rotation = source.Rotation;

        overlay.Scale = source.Scale * overlayScale;
        overlay.Visible = source.Visible;
        overlay.Modulate = source.Modulate;

        overlay.Texture = source.Texture;
        overlay.Hframes = source.Hframes;
        overlay.Vframes = source.Vframes;
        overlay.Frame = source.Frame;
        // Keep source alignment exact, then apply only a small horizontal pixel shift per overlay layer.
        overlay.Offset = (source.Offset / offsetDivisor) + new Vector2((lateralPixelOffset * direction) / offsetDivisor, 0f);
        overlay.PixelSize = source.PixelSize;
        overlay.Axis = source.Axis;
        overlay.Billboard = source.Billboard;
        overlay.TextureFilter = source.TextureFilter;
        overlay.FlipH = source.FlipH;
        overlay.FlipV = source.FlipV;
    }

    private void UpdateShaderOverlayVisuals()
    {
        if (_shaderOverlayVisuals.Count == 0) return;

        var staleSources = new List<SpriteBase3D>();
        foreach (var pair in _shaderOverlayVisuals)
        {
            if (pair.Key is not Sprite3D source || source == null || !GodotObject.IsInstanceValid(source))
            {
                QueueFreeShaderOverlays(pair.Value);
                staleSources.Add(pair.Key);
                continue;
            }

            var overlays = pair.Value;
            if (overlays == null || overlays.Count == 0)
            {
                staleSources.Add(pair.Key);
                continue;
            }

            for (int i = 0; i < overlays.Count; i++)
            {
                var overlay = overlays[i];
                if (overlay == null || !GodotObject.IsInstanceValid(overlay)) continue;
                SyncShaderOverlayFromSource(source, overlay, i);
            }
        }

        for (int i = 0; i < staleSources.Count; i++)
        {
            _shaderOverlayVisuals.Remove(staleSources[i]);
            _shaderOverlayScaleBySource.Remove(staleSources[i]);
        }
    }

    private void CleanupShaderOverlays(HashSet<SpriteBase3D> keepSources)
    {
        if (_shaderOverlayVisuals.Count == 0) return;

        var stale = new List<SpriteBase3D>();
        foreach (var pair in _shaderOverlayVisuals)
        {
            if (keepSources != null && keepSources.Contains(pair.Key)) continue;
            stale.Add(pair.Key);
        }

        for (int i = 0; i < stale.Count; i++)
        {
            RemoveShaderOverlaySprite(stale[i]);
        }
    }

    private static void QueueFreeShaderOverlays(List<Sprite3D> overlays)
    {
        if (overlays == null) return;
        for (int i = 0; i < overlays.Count; i++)
        {
            var overlay = overlays[i];
            if (overlay == null || !GodotObject.IsInstanceValid(overlay)) continue;
            overlay.QueueFree();
        }
    }

    private void RemoveShaderOverlaySprite(SpriteBase3D source)
    {
        if (source == null) return;
        if (!_shaderOverlayVisuals.TryGetValue(source, out var overlays)) return;

        if (overlays != null)
        {
            for (int i = 0; i < overlays.Count; i++)
            {
                var overlay = overlays[i];
                if (overlay == null || !GodotObject.IsInstanceValid(overlay)) continue;
                _runtimeShaderOverrides.Remove(overlay);
                overlay.QueueFree();
            }
        }

        _shaderOverlayVisuals.Remove(source);
        _shaderOverlayScaleBySource.Remove(source);
    }

    private void ClearShaderOverlays()
    {
        foreach (var pair in _shaderOverlayVisuals)
        {
            var overlays = pair.Value;
            if (overlays == null) continue;
            for (int i = 0; i < overlays.Count; i++)
            {
                var overlay = overlays[i];
                if (overlay == null || !GodotObject.IsInstanceValid(overlay)) continue;
                _runtimeShaderOverrides.Remove(overlay);
                overlay.QueueFree();
            }
        }

        _shaderOverlayVisuals.Clear();
        _shaderOverlayScaleBySource.Clear();
    }

    private void SyncMirrorImageVisualState(BattleVisualStateAccumulator state)
    {
        if (state == null || !state.TryGetMirrorImages(out var config))
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

    private void SyncHoverVisualState(BattleVisualStateAccumulator state)
    {
        if (state == null || !state.TryGetHover(out var config))
        {
            ClearHoverVisualState(resetOffsets: true);
            return;
        }

        bool configChanged = !_hoverVisualEnabled || !_activeHoverConfig.ApproxEquals(config);
        _activeHoverConfig = config;
        _hoverVisualEnabled = true;

        if (configChanged)
        {
            _hoverTimeSeconds = 0f;
        }

        UpdateHoverVisuals();
    }

    private void UpdateHoverVisuals(float deltaSeconds = 0f)
    {
        if (!_hoverVisualEnabled || _spriteVisuals.Count == 0) return;

        _hoverTimeSeconds += Mathf.Max(0f, deltaSeconds) * Mathf.Max(0f, _activeHoverConfig.BobSpeed);
        for (int i = 0; i < _spriteVisuals.Count; i++)
        {
            var sprite = _spriteVisuals[i];
            if (sprite == null || !GodotObject.IsInstanceValid(sprite)) continue;

            Vector2 baseOffset = _baseSpriteOffset.GetValueOrDefault(sprite, sprite.Offset);
            float phase = (_hoverTimeSeconds * Mathf.Tau) + (i * _activeHoverConfig.PhaseOffsetPerSprite);
            float bobSignal = EvaluateHoverBobSignal(_activeHoverConfig.BobWaveform, phase);
            float bobOffset = bobSignal * _activeHoverConfig.BobAmplitudePixels;

            // Sprite offsets use screen-space orientation where negative Y moves visuals upward.
            float liftedY = -_activeHoverConfig.BaseLiftPixels + bobOffset;
            sprite.Offset = baseOffset + new Vector2(0f, liftedY);
        }
    }

    private static float EvaluateHoverBobSignal(HoverBobWaveform waveform, float phaseRadians)
    {
        float t = Mathf.PosMod(phaseRadians / Mathf.Tau, 1f);
        switch (waveform)
        {
            case HoverBobWaveform.SmoothStepPingPong:
            {
                float ping = Mathf.PingPong(t * 2f, 1f);
                float smooth = ping * ping * (3f - (2f * ping));
                return (smooth * 2f) - 1f;
            }
            case HoverBobWaveform.Triangle:
                return 1f - (4f * Mathf.Abs(t - 0.5f));
            case HoverBobWaveform.Sawtooth:
                return (t * 2f) - 1f;
            case HoverBobWaveform.Sine:
            default:
                return Mathf.Sin(phaseRadians);
        }
    }

    private void ClearHoverVisualState(bool resetOffsets)
    {
        if (resetOffsets && _baseSpriteOffset.Count > 0)
        {
            foreach (var pair in _baseSpriteOffset)
            {
                var sprite = pair.Key;
                if (sprite == null || !GodotObject.IsInstanceValid(sprite)) continue;
                sprite.Offset = pair.Value;
            }
        }

        _activeHoverConfig = HoverVisualSettings.Default;
        _hoverVisualEnabled = false;
        _hoverTimeSeconds = 0f;
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
        _mirrorImageTimeSeconds = 0f;
        UpdateMirrorImageVisuals();

        if (!_mirrorImagesEnabled)
        {
            string ownerName = _character?.Name ?? Name;
            GD.PushWarning($"[CharacterVisualStateController] Mirror images requested for '{ownerName}' but no ghost sprites were created.");
        }
    }

    private static SpriteBase3D CreateMirrorImageGhost(SpriteBase3D source, int index)
    {
        if (source == null || !GodotObject.IsInstanceValid(source)) return null;

        if (source.Duplicate() is not SpriteBase3D ghost) return null;

        ghost.Name = $"{source.Name}_MirrorImage{index + 1}";
        ghost.SetMeta(GeneratedVisualMetaKey, true);
        ghost.Visible = source.Visible;
        ghost.Modulate = new Color(1f, 1f, 1f, 0.55f);
        ConfigureMirrorGhostRendering(source, ghost, index);
        while (ghost.GetChildCount() > 0)
        {
            ghost.GetChild(0).QueueFree();
        }

        return ghost;
    }

    private static void ConfigureMirrorGhostRendering(SpriteBase3D source, SpriteBase3D ghost, int index)
    {
        if (source == null || ghost == null) return;

        if (source is VisualInstance3D sourceVisual && ghost is VisualInstance3D ghostVisual)
        {
            // Preserve rendering layer visibility with the source sprite.
            ghostVisual.Layers = sourceVisual.Layers;
        }

        ghost.Billboard = source.Billboard;
        ghost.Centered = source.Centered;
        ghost.FixedSize = source.FixedSize;
        ghost.DoubleSided = source.DoubleSided;
        ghost.TextureFilter = source.TextureFilter;
        ghost.Axis = source.Axis;
        ghost.PixelSize = source.PixelSize;
        ghost.Offset = source.Offset;
        ghost.FlipH = source.FlipH;
        ghost.FlipV = source.FlipV;

        // Ensure mirror ghosts remain visible even if source sprite uses cutout rendering.
        ghost.AlphaCut = SpriteBase3D.AlphaCutMode.Disabled;
        ghost.AlphaScissorThreshold = 0f;
        ghost.NoDepthTest = true;
        ghost.Shaded = false;
        ghost.Transparent = true;
        ghost.RenderPriority = source.RenderPriority - (index + 1);

        // Keep mirror ghosts as clean sprite copies; don't inherit status shader overlays.
        if (ghost is GeometryInstance3D ghostGeometry)
        {
            ghostGeometry.MaterialOverride = null;
        }
    }

    private void UpdateMirrorImageVisuals(float deltaSeconds = 0f)
    {
        if (!_mirrorImagesEnabled || _mirrorImageVisuals.Count == 0) return;
        _mirrorImageTimeSeconds += Mathf.Max(0f, deltaSeconds);

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
        float trailingProgress = (index + 1f) / Mathf.Max(1f, count + 1f);
        float spreadRank = center > 0f ? Mathf.Abs((index - center) / center) : trailingProgress;

        ghost.Rotation = source.Rotation;
        ghost.Scale = source.Scale;
        ghost.Visible = source.Visible;

        float frameWidthPixels = source is Sprite3D sourceSpriteForWidth
            ? GetSpriteFramePixelWidth(sourceSpriteForWidth)
            : 64f;
        float spreadPixels = Mathf.Max(
            _activeMirrorConfig.MinSpreadPixels,
            frameWidthPixels * (_activeMirrorConfig.SpreadBase + (_activeMirrorConfig.Spread * _activeMirrorConfig.SpreadScale)));
        float trailingXSign = source.FlipH ? 1f : -1f;
        float trailingYSign = source.FlipV ? 1f : -1f;
        float driftSpeed = Mathf.Max(0f, _activeMirrorConfig.DriftSpeed);
        float driftPhase = (_mirrorImageTimeSeconds * driftSpeed * _activeMirrorConfig.DriftPhaseScale)
            + (index * _activeMirrorConfig.DriftPhaseIndexStep);
        float driftWaveA = Mathf.Sin(driftPhase);
        float driftWaveB = Mathf.Cos(driftPhase * _activeMirrorConfig.DriftZFrequency);
        float pulseFrequency = Mathf.Max(0f, _activeMirrorConfig.InOutPulseFrequency);
        float pulsePhase = (_mirrorImageTimeSeconds * pulseFrequency * Mathf.Tau)
            + (index * _activeMirrorConfig.InOutPulseIndexStep);
        float pulseScale = 1f + (Mathf.Sin(pulsePhase) * _activeMirrorConfig.InOutPulseAmplitude);
        pulseScale = Mathf.Max(_activeMirrorConfig.InOutMinScale, pulseScale);

        float xOffset = trailingXSign * spreadPixels * (_activeMirrorConfig.TrailXBase + (trailingProgress * _activeMirrorConfig.TrailXScale)) * pulseScale;
        xOffset += driftWaveA * spreadPixels * _activeMirrorConfig.DriftXAmplitude;
        float yOffset = trailingYSign * spreadPixels * (_activeMirrorConfig.TrailYBase + (_activeMirrorConfig.TrailYScale * trailingProgress)) * pulseScale;
        yOffset += driftWaveA * spreadPixels * _activeMirrorConfig.DriftYAmplitude;
        float zOffset = (-_activeMirrorConfig.DepthBase * (_activeMirrorConfig.DepthBias + trailingProgress) * pulseScale)
            + (driftWaveB * _activeMirrorConfig.DriftZAmplitude * trailingProgress);

        ghost.Position = source.Position + new Vector3(0f, yOffset * source.PixelSize * _activeMirrorConfig.WorldYOffsetScale, zOffset);

        int foregroundGhostCount = Mathf.Clamp(_activeMirrorConfig.ForegroundGhostCount, 0, Mathf.Max(0, count));
        if (index < foregroundGhostCount)
        {
            // nearer entries render in front, remaining entries trail behind
            ghost.RenderPriority = source.RenderPriority + (foregroundGhostCount - index);
        }
        else
        {
            int behindIndex = index - foregroundGhostCount;
            ghost.RenderPriority = source.RenderPriority - (behindIndex + 1);
        }

        // Apply offsets/tint for all SpriteBase3D variants (Sprite3D and AnimatedSprite3D).
        ghost.Offset = source.Offset + new Vector2(xOffset, yOffset);
        ghost.PixelSize = source.PixelSize;
        ghost.Axis = source.Axis;
        ghost.Billboard = source.Billboard;
        ghost.TextureFilter = source.TextureFilter;
        ghost.FlipH = source.FlipH;
        ghost.FlipV = source.FlipV;

        Color srcModulate = source.Modulate;
        float alphaBase = Mathf.Clamp(_activeMirrorConfig.Alpha, 0.02f, 1f);
        float alpha = (alphaBase * Mathf.Lerp(_activeMirrorConfig.AlphaNearScale, _activeMirrorConfig.AlphaFarScale, trailingProgress))
            + _activeMirrorConfig.AlphaBias;
        alpha = Mathf.Clamp(alpha * srcModulate.A, _activeMirrorConfig.MinVisibleAlpha, 1f);
        float tintStrength = Mathf.Clamp(
            _activeMirrorConfig.TintStrengthBase + (_activeMirrorConfig.TintStrengthBySpread * spreadRank),
            0f,
            1f);
        Color trailTint = new Color(
            _activeMirrorConfig.TrailTintColor.R,
            _activeMirrorConfig.TrailTintColor.G,
            _activeMirrorConfig.TrailTintColor.B,
            1f);
        Color ghostColor = srcModulate.Lerp(trailTint, tintStrength);
        ghost.Modulate = new Color(ghostColor.R, ghostColor.G, ghostColor.B, alpha);

        if (source is VisualInstance3D sourceVisual && ghost is VisualInstance3D ghostVisual)
        {
            ghostVisual.Layers = sourceVisual.Layers;
        }

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
        _activeMirrorConfig = MirrorImageVisualSettings.Default;
        _mirrorImagesEnabled = false;
        _mirrorImageTimeSeconds = 0f;
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
        _baseSpriteOffset.Clear();

        if (_character == null) return;

        CacheVisualNodesRecursive(_character);
    }

    private void CacheVisualNodesRecursive(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is SpriteBase3D sprite)
            {
                if (sprite.HasMeta(GeneratedVisualMetaKey))
                {
                    continue;
                }
                _spriteVisuals.Add(sprite);
                _baseSpriteModulate[sprite] = sprite.Modulate;
                _baseSpriteScale[sprite] = sprite.Scale;
                _baseSpriteOffset[sprite] = sprite.Offset;
                if (sprite is GeometryInstance3D geometry)
                {
                    _baseSpriteMaterials[sprite] = geometry.MaterialOverride;
                }
            }

            CacheVisualNodesRecursive(child);
        }
    }
}
