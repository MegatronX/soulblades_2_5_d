using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Spawns/despawns ambient prop scenes around a focus target.
/// </summary>
[GlobalClass]
public partial class AmbientPropSystem : Node3D
{
    private sealed class AmbientPropRuntimeState
    {
        public Vector3 OriginGlobal;
        public float Phase;
        public float MotionRadius;
        public float MotionSpeed;
        public float VerticalBobAmplitude;
        public float LifetimeSeconds;
        public float AgeSeconds;
        public bool EnableMotion;
        public bool EnableFade;
        public float SpawnFadeSeconds;
        public float DespawnFadeSeconds;
        public float FadeInElapsedSeconds;
        public bool IsDespawning;
        public float FadeOutElapsedSeconds;
        public IAmbientPropFadeReceiver CustomFadeReceiver;
        public readonly List<SpriteBase3D> SpriteTargets = new();
        public readonly List<Color> SpriteBaseModulates = new();
        public readonly List<CanvasItem> CanvasTargets = new();
        public readonly List<Color> CanvasBaseModulates = new();
        public readonly List<Light3D> LightTargets = new();
        public readonly List<float> LightBaseEnergy = new();
    }

    [Export]
    public AmbientPropProfile Profile { get; private set; }

    [Export]
    public NodePath FollowTargetPath { get; private set; }

    [Export]
    public bool AutoFindPlayerCharacter { get; private set; } = true;

    [ExportGroup("Debug Overlay (runtime spawn/despawn diagnostics)")]
    [Export]
    public bool EnableDebugOverlay { get; private set; } = false;

    [Export]
    public bool DebugOverlayStartVisible { get; private set; } = false;

    [Export]
    public Key DebugOverlayToggleKey { get; private set; } = Key.F9;

    [Export(PropertyHint.Range, "0.01,2,0.01,suffix:s")]
    public float DebugOverlayRefreshSeconds { get; private set; } = 0.15f;

    [Export(PropertyHint.Range, "10,48,1,suffix:px")]
    public int DebugOverlayFontSize { get; private set; } = 13;

    [Export(PropertyHint.Range, "1,20,1,suffix:events")]
    public int DebugOverlayRecentDespawnLimit { get; private set; } = 6;

    private readonly List<Node3D> _activeProps = new();
    private readonly Dictionary<Node3D, AmbientPropSpawnEntry> _activePropEntries = new();
    private readonly Dictionary<Node3D, AmbientPropRuntimeState> _runtimeStates = new();
    private readonly Dictionary<AmbientPropSpawnEntry, float> _entryCooldowns = new();
    private readonly Dictionary<string, int> _spawnReasonCounts = new();
    private readonly Dictionary<string, int> _despawnReasonCounts = new();
    private readonly Queue<string> _recentDespawnEvents = new();
    private readonly RandomNumberGenerator _rng = new();
    private double _spawnTimer;
    private Node3D _followTarget;
    private Camera3D _spawnCamera;
    private CanvasLayer _debugCanvas;
    private PanelContainer _debugPanel;
    private Label _debugLabel;
    private double _debugRefreshAccumulator;
    private string _lastSpawnReason = "None";
    private string _lastDespawnReason = "None";
    private string _lastSpawnFailureReason = "None";

    public override void _Ready()
    {
        _rng.Randomize();
        _followTarget = GetNodeOrNull<Node3D>(FollowTargetPath);
        _spawnCamera = ResolveSpawnCamera();
        if (EnableDebugOverlay)
        {
            BuildDebugOverlay();
            RefreshDebugOverlay();
        }

        SpawnInitial();
    }

    public override void _Input(InputEvent @event)
    {
        if (!EnableDebugOverlay || _debugPanel == null) return;
        if (@event is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed || keyEvent.Echo) return;
        if (keyEvent.Keycode != DebugOverlayToggleKey) return;

        _debugPanel.Visible = !_debugPanel.Visible;
        if (_debugPanel.Visible)
        {
            RefreshDebugOverlay();
        }
    }

    public override void _Process(double delta)
    {
        _followTarget ??= ResolveFollowTarget();
        _spawnCamera ??= ResolveSpawnCamera();
        TickEntryCooldowns(delta);
        UpdateRuntimeMotionLifetimeAndFade(delta);
        PruneDestroyed();
        PruneByDistance();
        UpdateDebugOverlay(delta);

        if (!HasSpawnContent()) return;
        if (_activeProps.Count >= Profile.MaxActiveProps)
        {
            _lastSpawnFailureReason = "Max active reached.";
            return;
        }

        _spawnTimer -= delta;
        if (_spawnTimer > 0d) return;

        _spawnTimer = Mathf.Max(0.1f, Profile.SpawnIntervalSeconds);
        TrySpawnOne("interval");
    }

    private void SpawnInitial()
    {
        if (!HasSpawnContent()) return;
        int spawnCount = Mathf.Clamp(Profile.InitialSpawnCount, 0, Profile.MaxActiveProps);
        for (int i = 0; i < spawnCount; i++)
        {
            TrySpawnOne("initial");
        }
    }

    private void TrySpawnOne(string spawnReason)
    {
        if (!HasSpawnContent()) return;
        if (_activeProps.Count >= Profile.MaxActiveProps)
        {
            _lastSpawnFailureReason = "Max active reached.";
            return;
        }

        var packed = ResolveSpawnScene(out var spawnEntry);
        if (packed == null)
        {
            _lastSpawnFailureReason = "No eligible spawn entries (cooldowns/caps).";
            return;
        }

        var instance = packed.Instantiate();
        if (instance is not Node3D node3D)
        {
            instance?.QueueFree();
            _lastSpawnFailureReason = "Spawned scene is not Node3D.";
            return;
        }

        Vector3 spawnPosition = ResolveSpawnPosition(out bool foundValidPosition);
        if (Profile.SpawnOnlyInCameraView && !foundValidPosition)
        {
            instance?.QueueFree();
            _lastSpawnFailureReason = "No valid position in camera view.";
            return;
        }

        // Set local position before AddChild so _Ready() on spawned props can capture
        // the correct origin and avoid first-frame snap/disappear artifacts.
        node3D.Position = ToLocal(spawnPosition);
        AddChild(node3D);
        _activeProps.Add(node3D);
        if (spawnEntry != null)
        {
            _activePropEntries[node3D] = spawnEntry;
            if (spawnEntry.SpawnCooldownSeconds > 0f)
            {
                _entryCooldowns[spawnEntry] = spawnEntry.SpawnCooldownSeconds;
            }
        }

        ConfigureRuntimeState(node3D, spawnEntry);
        RecordSpawn(spawnReason, spawnEntry);
        _lastSpawnFailureReason = "None";
    }

    private Vector3 ResolveSpawnPosition(out bool foundValidPosition)
    {
        Vector3 anchor = _followTarget?.GlobalPosition ?? GlobalPosition;
        if (Profile == null)
        {
            foundValidPosition = true;
            return anchor;
        }

        int attempts = Profile.SpawnOnlyInCameraView ? Mathf.Max(1, Profile.SpawnPositionAttempts) : 1;
        Camera3D camera = Profile.SpawnOnlyInCameraView ? (_spawnCamera ?? ResolveSpawnCamera()) : null;
        float margin = Mathf.Clamp(Profile.ViewportSpawnMargin, 0f, 0.45f);

        for (int i = 0; i < attempts; i++)
        {
            float angle = _rng.RandfRange(0f, Mathf.Tau);
            float radius = _rng.RandfRange(0f, Mathf.Max(0.1f, Profile.SpawnRadius));
            float height = _rng.RandfRange(-Profile.VerticalJitter, Profile.VerticalJitter);
            Vector3 candidate = anchor + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);

            if (!Profile.SpawnOnlyInCameraView || camera == null || IsWorldPointInCameraView(camera, candidate, margin))
            {
                foundValidPosition = true;
                return candidate;
            }
        }

        foundValidPosition = !Profile.SpawnOnlyInCameraView;
        return anchor;
    }

    private void PruneByDistance()
    {
        if (_followTarget == null || Profile == null) return;
        float maxDistance = Mathf.Max(Profile.DespawnRadius, Profile.SpawnRadius + 1f);
        float maxDistanceSq = maxDistance * maxDistance;

        for (int i = _activeProps.Count - 1; i >= 0; i--)
        {
            var prop = _activeProps[i];
            if (prop == null || !GodotObject.IsInstanceValid(prop))
            {
                _activeProps.RemoveAt(i);
                _activePropEntries.Remove(prop);
                _runtimeStates.Remove(prop);
                continue;
            }

            if (prop.GlobalPosition.DistanceSquaredTo(_followTarget.GlobalPosition) > maxDistanceSq)
            {
                BeginDespawn(prop, "distance");
            }
        }
    }

    private void PruneDestroyed()
    {
        for (int i = _activeProps.Count - 1; i >= 0; i--)
        {
            var prop = _activeProps[i];
            if (prop != null && GodotObject.IsInstanceValid(prop))
            {
                continue;
            }

            _activeProps.RemoveAt(i);
            _activePropEntries.Remove(prop);
            _runtimeStates.Remove(prop);
            RecordDespawn("invalid_or_removed", prop, null);
        }
    }

    private Node3D ResolveFollowTarget()
    {
        if (_followTarget != null && GodotObject.IsInstanceValid(_followTarget))
        {
            return _followTarget;
        }

        if (!AutoFindPlayerCharacter) return null;
        return GetTree()?.GetNodesInGroup(GameGroups.PlayerCharacters).OfType<Node3D>().FirstOrDefault();
    }

    private bool HasSpawnContent()
    {
        if (Profile == null) return false;
        return Profile.SpawnEntries != null && Profile.SpawnEntries.Count > 0;
    }

    private PackedScene ResolveSpawnScene(out AmbientPropSpawnEntry selectedEntry)
    {
        selectedEntry = null;
        if (Profile == null) return null;
        if (Profile.SpawnEntries == null || Profile.SpawnEntries.Count == 0) return null;

        var candidates = new List<AmbientPropSpawnEntry>();
        int totalWeight = 0;
        foreach (var entry in Profile.SpawnEntries)
        {
            if (entry?.PropScene == null) continue;
            if (entry.MaxActiveCount > 0 && CountActiveForEntry(entry) >= entry.MaxActiveCount) continue;
            if (entry.SpawnCooldownSeconds > 0f
                && _entryCooldowns.TryGetValue(entry, out float cooldown)
                && cooldown > 0f)
            {
                continue;
            }

            int weight = Mathf.Max(1, entry.Weight);
            totalWeight += weight;
            candidates.Add(entry);
        }

        if (candidates.Count == 0 || totalWeight <= 0) return null;

        int roll = _rng.RandiRange(0, totalWeight - 1);
        foreach (var entry in candidates)
        {
            int weight = Mathf.Max(1, entry.Weight);
            if (roll < weight)
            {
                selectedEntry = entry;
                return entry.PropScene;
            }
            roll -= weight;
        }

        selectedEntry = candidates[0];
        return selectedEntry.PropScene;
    }

    private int CountActiveForEntry(AmbientPropSpawnEntry entry)
    {
        if (entry == null || _activePropEntries.Count == 0) return 0;
        int count = 0;
        foreach (var kvp in _activePropEntries)
        {
            if (kvp.Value == entry && kvp.Key != null && GodotObject.IsInstanceValid(kvp.Key))
            {
                count++;
            }
        }
        return count;
    }

    private void TickEntryCooldowns(double delta)
    {
        if (_entryCooldowns.Count == 0) return;
        float dt = Mathf.Max(0f, (float)delta);
        if (dt <= 0f) return;

        foreach (var entry in _entryCooldowns.Keys.ToList())
        {
            float remaining = _entryCooldowns[entry] - dt;
            if (remaining <= 0f)
            {
                _entryCooldowns.Remove(entry);
            }
            else
            {
                _entryCooldowns[entry] = remaining;
            }
        }
    }

    private Camera3D ResolveSpawnCamera()
    {
        if (Profile != null && !Profile.SpawnCameraPath.IsEmpty)
        {
            var explicitCamera = GetNodeOrNull<Camera3D>(Profile.SpawnCameraPath);
            if (explicitCamera != null && GodotObject.IsInstanceValid(explicitCamera))
            {
                return explicitCamera;
            }
        }

        var viewportCamera = GetViewport()?.GetCamera3D();
        if (viewportCamera != null && GodotObject.IsInstanceValid(viewportCamera))
        {
            return viewportCamera;
        }

        return GetTree()?.CurrentScene?.FindChild("Camera3D", true, false) as Camera3D;
    }

    private bool IsWorldPointInCameraView(Camera3D camera, Vector3 worldPoint, float margin)
    {
        if (camera == null || !GodotObject.IsInstanceValid(camera)) return false;
        if (camera.IsPositionBehind(worldPoint)) return false;

        var viewport = GetViewport();
        if (viewport == null) return false;

        Rect2 rect = viewport.GetVisibleRect();
        if (rect.Size.X <= 0f || rect.Size.Y <= 0f) return false;

        Vector2 screenPoint = camera.UnprojectPosition(worldPoint);
        float marginX = rect.Size.X * margin;
        float marginY = rect.Size.Y * margin;
        float minX = rect.Position.X + marginX;
        float maxX = rect.End.X - marginX;
        float minY = rect.Position.Y + marginY;
        float maxY = rect.End.Y - marginY;

        return screenPoint.X >= minX
            && screenPoint.X <= maxX
            && screenPoint.Y >= minY
            && screenPoint.Y <= maxY;
    }

    private void ConfigureRuntimeState(Node3D prop, AmbientPropSpawnEntry entry)
    {
        if (prop == null || !GodotObject.IsInstanceValid(prop)) return;

        bool enableMotion = entry?.EnableSystemMotion ?? false;
        float lifetime = ResolveLifetimeSeconds(entry);
        bool enableFade = Profile?.EnableSpawnDespawnFade ?? false;

        float motionRadius = 0f;
        float motionSpeed = 0f;
        float bobAmplitude = 0f;
        if (enableMotion && entry != null)
        {
            motionRadius = Mathf.Max(0f, entry.MotionRadius);
            float speedMin = Mathf.Max(0.01f, entry.MotionSpeedMin);
            float speedMax = Mathf.Max(speedMin, entry.MotionSpeedMax);
            motionSpeed = _rng.RandfRange(speedMin, speedMax);
            bobAmplitude = Mathf.Max(0f, entry.VerticalBobAmplitude);
        }

        var state = new AmbientPropRuntimeState
        {
            OriginGlobal = prop.GlobalPosition,
            Phase = _rng.RandfRange(0f, Mathf.Tau),
            MotionRadius = motionRadius,
            MotionSpeed = motionSpeed,
            VerticalBobAmplitude = bobAmplitude,
            LifetimeSeconds = lifetime,
            AgeSeconds = 0f,
            EnableMotion = enableMotion,
            EnableFade = enableFade,
            SpawnFadeSeconds = Mathf.Max(0f, Profile?.SpawnFadeSeconds ?? 0f),
            DespawnFadeSeconds = Mathf.Max(0f, Profile?.DespawnFadeSeconds ?? 0f),
            FadeInElapsedSeconds = 0f,
            IsDespawning = false,
            FadeOutElapsedSeconds = 0f,
            CustomFadeReceiver = prop as IAmbientPropFadeReceiver
        };

        PopulateFadeTargets(prop, state);
        _runtimeStates[prop] = state;

        if (state.EnableFade && state.SpawnFadeSeconds > 0f)
        {
            ApplyFadeToProp(state, 0f);
        }
        else
        {
            ApplyFadeToProp(state, 1f);
        }
    }

    private float ResolveLifetimeSeconds(AmbientPropSpawnEntry entry)
    {
        if (entry == null) return 0f;

        float min = Mathf.Max(0f, entry.LifetimeMinSeconds);
        float max = Mathf.Max(min, entry.LifetimeMaxSeconds);
        if (max <= 0f) return 0f;
        if (Mathf.IsEqualApprox(min, max)) return max;
        return _rng.RandfRange(min, max);
    }

    private void UpdateRuntimeMotionLifetimeAndFade(double delta)
    {
        if (_runtimeStates.Count == 0) return;
        float dt = Mathf.Max(0f, (float)delta);
        if (dt <= 0f) return;

        foreach (var kvp in _runtimeStates.ToArray())
        {
            Node3D prop = kvp.Key;
            AmbientPropRuntimeState state = kvp.Value;

            if (prop == null || !GodotObject.IsInstanceValid(prop))
            {
                _runtimeStates.Remove(prop);
                continue;
            }

            if (state.IsDespawning)
            {
                if (!state.EnableFade || state.DespawnFadeSeconds <= 0f)
                {
                    RemoveProp(prop);
                    continue;
                }

                state.FadeOutElapsedSeconds += dt;
                float fadeOutAlpha = 1f - Mathf.Clamp(state.FadeOutElapsedSeconds / state.DespawnFadeSeconds, 0f, 1f);
                ApplyFadeToProp(state, fadeOutAlpha);
                if (fadeOutAlpha <= 0.001f)
                {
                    RemoveProp(prop);
                }
                continue;
            }

            state.AgeSeconds += dt;
            if (state.LifetimeSeconds > 0f && state.AgeSeconds >= state.LifetimeSeconds)
            {
                BeginDespawn(prop, "lifetime");
                continue;
            }

            float fadeInAlpha = 1f;
            if (state.EnableFade && state.SpawnFadeSeconds > 0f)
            {
                state.FadeInElapsedSeconds += dt;
                fadeInAlpha = Mathf.Clamp(state.FadeInElapsedSeconds / state.SpawnFadeSeconds, 0f, 1f);
            }

            ApplyFadeToProp(state, fadeInAlpha);
            if (!state.EnableMotion) continue;

            state.Phase += dt * Mathf.Max(0.01f, state.MotionSpeed);
            float x = Mathf.Cos(state.Phase) * state.MotionRadius;
            float z = Mathf.Sin((state.Phase * 1.27f) + 0.61f) * state.MotionRadius;
            float y = Mathf.Sin(state.Phase * 2.05f) * state.VerticalBobAmplitude;
            prop.GlobalPosition = state.OriginGlobal + new Vector3(x, y, z);
        }
    }

    private void BeginDespawn(Node3D prop, string reason)
    {
        if (prop == null || !GodotObject.IsInstanceValid(prop))
        {
            RecordDespawn(reason, prop, null);
            return;
        }

        _activePropEntries.TryGetValue(prop, out var entry);

        if (!_runtimeStates.TryGetValue(prop, out var state))
        {
            RecordDespawn(reason, prop, entry);
            RemoveProp(prop);
            return;
        }

        if (state.IsDespawning) return;

        RecordDespawn(reason, prop, entry);
        if (!state.EnableFade || state.DespawnFadeSeconds <= 0f)
        {
            RemoveProp(prop);
            return;
        }

        state.IsDespawning = true;
        state.FadeOutElapsedSeconds = 0f;
    }

    private void RemoveProp(Node3D prop)
    {
        _activeProps.Remove(prop);
        _activePropEntries.Remove(prop);
        _runtimeStates.Remove(prop);
        if (prop != null && GodotObject.IsInstanceValid(prop))
        {
            prop.QueueFree();
        }
    }

    private void PopulateFadeTargets(Node3D root, AmbientPropRuntimeState state)
    {
        if (root == null || state == null) return;
        if (state.CustomFadeReceiver != null) return;

        AddFadeTargetsRecursive(root, state);
    }

    private void AddFadeTargetsRecursive(Node node, AmbientPropRuntimeState state)
    {
        if (node is SpriteBase3D sprite)
        {
            state.SpriteTargets.Add(sprite);
            state.SpriteBaseModulates.Add(sprite.Modulate);
        }
        else if (node is CanvasItem canvasItem && node is not Control)
        {
            state.CanvasTargets.Add(canvasItem);
            state.CanvasBaseModulates.Add(canvasItem.Modulate);
        }

        if (node is Light3D light)
        {
            state.LightTargets.Add(light);
            state.LightBaseEnergy.Add(light.LightEnergy);
        }

        foreach (Node child in node.GetChildren())
        {
            AddFadeTargetsRecursive(child, state);
        }
    }

    private void ApplyFadeToProp(AmbientPropRuntimeState state, float alpha)
    {
        if (state == null) return;
        float clampedAlpha = Mathf.Clamp(alpha, 0f, 1f);

        if (state.CustomFadeReceiver != null)
        {
            state.CustomFadeReceiver.SetAmbientFadeMultiplier(clampedAlpha);
            return;
        }

        for (int i = 0; i < state.SpriteTargets.Count; i++)
        {
            var sprite = state.SpriteTargets[i];
            if (sprite == null || !GodotObject.IsInstanceValid(sprite)) continue;
            var baseColor = state.SpriteBaseModulates[i];
            baseColor.A *= clampedAlpha;
            sprite.Modulate = baseColor;
        }

        for (int i = 0; i < state.CanvasTargets.Count; i++)
        {
            var canvas = state.CanvasTargets[i];
            if (canvas == null || !GodotObject.IsInstanceValid(canvas)) continue;
            var baseColor = state.CanvasBaseModulates[i];
            baseColor.A *= clampedAlpha;
            canvas.Modulate = baseColor;
        }

        for (int i = 0; i < state.LightTargets.Count; i++)
        {
            var light = state.LightTargets[i];
            if (light == null || !GodotObject.IsInstanceValid(light)) continue;
            light.LightEnergy = state.LightBaseEnergy[i] * clampedAlpha;
        }
    }

    private void RecordSpawn(string reason, AmbientPropSpawnEntry entry)
    {
        string sceneLabel = entry?.PropScene?.ResourcePath?.GetFile() ?? "UnknownScene";
        string key = $"{reason}: {sceneLabel}";
        _lastSpawnReason = key;
        if (_spawnReasonCounts.TryGetValue(key, out int count))
        {
            _spawnReasonCounts[key] = count + 1;
        }
        else
        {
            _spawnReasonCounts[key] = 1;
        }
    }

    private void RecordDespawn(string reason, Node3D prop, AmbientPropSpawnEntry entry)
    {
        _lastDespawnReason = reason;
        if (_despawnReasonCounts.TryGetValue(reason, out int count))
        {
            _despawnReasonCounts[reason] = count + 1;
        }
        else
        {
            _despawnReasonCounts[reason] = 1;
        }

        string timestamp = FormatRuntimeTimestamp();
        string propLabel = BuildDespawnPropLabel(prop, entry);
        string eventLine = $"{timestamp} | {reason} | {propLabel}";
        _recentDespawnEvents.Enqueue(eventLine);
        int maxEvents = Mathf.Max(1, DebugOverlayRecentDespawnLimit);
        while (_recentDespawnEvents.Count > maxEvents)
        {
            _recentDespawnEvents.Dequeue();
        }
    }

    private void BuildDebugOverlay()
    {
        if (_debugCanvas != null) return;

        _debugCanvas = new CanvasLayer
        {
            Name = "AmbientPropDebugOverlay"
        };
        AddChild(_debugCanvas);

        _debugPanel = new PanelContainer
        {
            Visible = DebugOverlayStartVisible
        };
        _debugPanel.AnchorLeft = 0f;
        _debugPanel.AnchorTop = 0f;
        _debugPanel.AnchorRight = 0f;
        _debugPanel.AnchorBottom = 0f;
        _debugPanel.OffsetLeft = 20f;
        _debugPanel.OffsetTop = 100f;
        _debugPanel.OffsetRight = 420f;
        _debugPanel.OffsetBottom = 300f;
        _debugCanvas.AddChild(_debugPanel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.58f),
            BorderColor = new Color(1f, 1f, 1f, 0.25f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        _debugPanel.AddThemeStyleboxOverride("panel", style);

        _debugLabel = new Label();
        _debugLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _debugLabel.LabelSettings = new LabelSettings
        {
            FontSize = Mathf.Max(10, DebugOverlayFontSize),
            FontColor = new Color(0.9f, 0.96f, 1f),
            OutlineColor = Colors.Black,
            OutlineSize = 1
        };
        _debugPanel.AddChild(_debugLabel);
    }

    private void UpdateDebugOverlay(double delta)
    {
        if (!EnableDebugOverlay || _debugPanel == null || !_debugPanel.Visible) return;

        _debugRefreshAccumulator += delta;
        if (_debugRefreshAccumulator < Mathf.Max(0.01f, DebugOverlayRefreshSeconds)) return;
        _debugRefreshAccumulator = 0d;
        RefreshDebugOverlay();
    }

    private void RefreshDebugOverlay()
    {
        if (_debugLabel == null) return;

        string spawnSummary = FormatTopReasonCounts(_spawnReasonCounts);
        string despawnSummary = FormatTopReasonCounts(_despawnReasonCounts);
        string recentDespawns = FormatRecentDespawns();
        _debugLabel.Text =
            $"Ambient Props [{DebugOverlayToggleKey}]\n" +
            $"Active: {_activeProps.Count} / {Profile?.MaxActiveProps ?? 0}\n" +
            $"RuntimeStates: {_runtimeStates.Count}\n" +
            $"Last Spawn: {_lastSpawnReason}\n" +
            $"Last Despawn: {_lastDespawnReason}\n" +
            $"Last Spawn Skip: {_lastSpawnFailureReason}\n" +
            $"Spawn Reasons: {spawnSummary}\n" +
            $"Despawn Reasons: {despawnSummary}\n" +
            $"Recent Despawns:\n{recentDespawns}";
    }

    private static string FormatTopReasonCounts(Dictionary<string, int> counts)
    {
        if (counts == null || counts.Count == 0) return "None";

        return string.Join(", ",
            counts
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => $"{kvp.Key} ({kvp.Value})"));
    }

    private string FormatRecentDespawns()
    {
        if (_recentDespawnEvents.Count == 0) return "  (none)";
        return string.Join("\n", _recentDespawnEvents.Select(evt => $"  - {evt}"));
    }

    private string BuildDespawnPropLabel(Node3D prop, AmbientPropSpawnEntry entry)
    {
        string sceneLabel = entry?.PropScene?.ResourcePath?.GetFile();
        string nodeLabel = "UnknownProp";
        if (prop != null && GodotObject.IsInstanceValid(prop))
        {
            nodeLabel = prop.Name.ToString();
        }

        if (string.IsNullOrWhiteSpace(sceneLabel))
        {
            return nodeLabel;
        }

        return $"{sceneLabel} ({nodeLabel})";
    }

    private string FormatRuntimeTimestamp()
    {
        double seconds = Time.GetTicksMsec() / 1000.0;
        int totalSeconds = Mathf.Max(0, (int)seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes:00}:{secs:00}";
    }
}
