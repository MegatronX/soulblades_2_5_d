using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Single writer for scene-level visual state (environment and main light).
/// Contributors submit intents; director composes and applies deterministically.
/// </summary>
[GlobalClass]
public partial class SceneVisualDirector : Node3D
{
    public const string NodeName = "SceneVisualDirector";

    [ExportGroup("Scene References")]
    [Export]
    public WorldEnvironment WorldEnvironment { get; private set; }

    [Export]
    public DirectionalLight3D MainLight { get; private set; }

    private sealed class ContributionEntry
    {
        public SceneVisualIntent Intent;
        public int Sequence;
    }

    private sealed class BaselineState
    {
        public Color AmbientLightColor = Colors.White;
        public Color FogLightColor = Colors.White;
        public float AmbientLightEnergy = 1f;
        public float GlowIntensity = 1f;
        public float FogDensity = 0f;
        public bool FogEnabled;
        public float AmbientSkyContribution = 1f;
        public bool AdjustmentEnabled;
        public float AdjustmentBrightness = 1f;
        public float AdjustmentSaturation = 1f;

        public Color MainLightColor = Colors.White;
        public float MainLightEnergy = 1f;
        public bool MainLightShadowEnabled = true;
    }

    private readonly Dictionary<string, ContributionEntry> _contributions = new();
    private int _sequenceCounter;
    private bool _baselineCaptured;
    private readonly BaselineState _baseline = new();

    public override void _Ready()
    {
        ResolveReferences();
        CaptureBaseline();
        ApplyComposedState();
    }

    public void SetContribution(string sourceId, SceneVisualIntent intent)
    {
        if (string.IsNullOrEmpty(sourceId) || intent == null) return;
        ResolveReferences();
        CaptureBaseline();

        _sequenceCounter++;
        _contributions[sourceId] = new ContributionEntry
        {
            Intent = intent,
            Sequence = _sequenceCounter
        };
        ApplyComposedState();
    }

    public void ClearContribution(string sourceId)
    {
        if (string.IsNullOrEmpty(sourceId)) return;
        if (!_contributions.Remove(sourceId)) return;
        ApplyComposedState();
    }

    public void ClearAllContributions()
    {
        if (_contributions.Count == 0) return;
        _contributions.Clear();
        ApplyComposedState();
    }

    public void Reapply()
    {
        ResolveReferences();
        CaptureBaseline();
        ApplyComposedState();
    }

    private void ResolveReferences()
    {
        if (WorldEnvironment == null)
        {
            WorldEnvironment = GetTree()?.CurrentScene?.FindChild("WorldEnvironment", true, false) as WorldEnvironment;
        }

        if (MainLight == null)
        {
            MainLight = GetTree()?.CurrentScene?.FindChild("MainLight", true, false) as DirectionalLight3D
                ?? GetTree()?.CurrentScene?.FindChild("DirectionalLight3D", true, false) as DirectionalLight3D;
        }
    }

    private void CaptureBaseline()
    {
        if (_baselineCaptured) return;

        var env = WorldEnvironment?.Environment;
        if (env != null)
        {
            _baseline.AmbientLightColor = env.AmbientLightColor;
            _baseline.FogLightColor = env.FogLightColor;
            _baseline.AmbientLightEnergy = env.AmbientLightEnergy;
            _baseline.GlowIntensity = env.GlowIntensity;
            _baseline.FogDensity = env.FogDensity;
            _baseline.FogEnabled = env.FogEnabled;
            _baseline.AmbientSkyContribution = env.AmbientLightSkyContribution;
            _baseline.AdjustmentEnabled = env.AdjustmentEnabled;
            _baseline.AdjustmentBrightness = env.AdjustmentBrightness;
            _baseline.AdjustmentSaturation = env.AdjustmentSaturation;
        }

        if (MainLight != null)
        {
            _baseline.MainLightColor = MainLight.LightColor;
            _baseline.MainLightEnergy = MainLight.LightEnergy;
            _baseline.MainLightShadowEnabled = MainLight.ShadowEnabled;
        }

        _baselineCaptured = true;
    }

    private void ApplyComposedState()
    {
        var env = WorldEnvironment?.Environment;
        if (!_baselineCaptured || (env == null && MainLight == null)) return;

        Color ambientColor = _baseline.AmbientLightColor;
        Color fogColor = _baseline.FogLightColor;
        float ambientEnergy = _baseline.AmbientLightEnergy;
        float glowIntensity = _baseline.GlowIntensity;
        float fogDensity = _baseline.FogDensity;
        bool fogEnabled = _baseline.FogEnabled;
        float skyContribution = _baseline.AmbientSkyContribution;
        bool adjustmentEnabled = _baseline.AdjustmentEnabled;
        float adjustmentBrightness = _baseline.AdjustmentBrightness;
        float adjustmentSaturation = _baseline.AdjustmentSaturation;

        Color mainLightColor = _baseline.MainLightColor;
        float mainLightEnergy = _baseline.MainLightEnergy;
        bool mainLightShadows = _baseline.MainLightShadowEnabled;

        foreach (var entry in GetOrderedContributions())
        {
            SceneVisualIntent intent = entry.Intent;
            if (intent == null) continue;

            if (intent.UseAmbientTint)
            {
                ambientColor = ambientColor.Lerp(intent.AmbientTint, Mathf.Clamp(intent.AmbientTintStrength, 0f, 1f));
            }

            if (intent.UseFogTint)
            {
                fogColor = fogColor.Lerp(intent.FogTint, Mathf.Clamp(intent.FogTintStrength, 0f, 1f));
            }

            ambientColor *= Mathf.Clamp(intent.AmbientColorMultiplier, 0f, 8f);
            fogColor *= Mathf.Clamp(intent.FogColorMultiplier, 0f, 8f);
            ambientEnergy *= Mathf.Clamp(intent.AmbientEnergyMultiplier, 0f, 8f);
            glowIntensity *= Mathf.Clamp(intent.GlowIntensityMultiplier, 0f, 8f);
            fogDensity *= Mathf.Clamp(intent.FogDensityMultiplier, 0f, 8f);
            fogDensity += intent.FogDensityAdd;
            skyContribution *= Mathf.Clamp(intent.AmbientSkyContributionMultiplier, 0f, 8f);

            if (intent.FogEnabledOverride.HasValue)
            {
                fogEnabled = intent.FogEnabledOverride.Value;
            }

            if (intent.OverrideMainLightColor)
            {
                mainLightColor = intent.MainLightColorOverride;
            }

            if (intent.UseMainLightTint)
            {
                mainLightColor = mainLightColor.Lerp(intent.MainLightTint, Mathf.Clamp(intent.MainLightTintStrength, 0f, 1f));
            }

            mainLightEnergy *= Mathf.Clamp(intent.MainLightEnergyMultiplier, 0f, 8f);

            if (intent.MainLightShadowEnabledOverride.HasValue)
            {
                mainLightShadows = intent.MainLightShadowEnabledOverride.Value;
            }

            if (intent.AdjustmentEnabledOverride.HasValue)
            {
                adjustmentEnabled = intent.AdjustmentEnabledOverride.Value;
            }

            adjustmentBrightness *= Mathf.Clamp(intent.AdjustmentBrightnessMultiplier, 0f, 8f);
            adjustmentSaturation *= Mathf.Clamp(intent.AdjustmentSaturationMultiplier, 0f, 8f);
        }

        if (env != null)
        {
            env.AmbientLightColor = ambientColor;
            env.FogLightColor = fogColor;
            env.AmbientLightEnergy = ambientEnergy;
            env.GlowIntensity = glowIntensity;
            env.FogDensity = Mathf.Max(0f, fogDensity);
            env.FogEnabled = fogEnabled;
            env.AmbientLightSkyContribution = Mathf.Max(0f, skyContribution);
            env.AdjustmentEnabled = adjustmentEnabled;
            env.AdjustmentBrightness = Mathf.Max(0f, adjustmentBrightness);
            env.AdjustmentSaturation = Mathf.Max(0f, adjustmentSaturation);
        }

        if (MainLight != null)
        {
            MainLight.LightColor = mainLightColor;
            MainLight.LightEnergy = Mathf.Max(0f, mainLightEnergy);
            MainLight.ShadowEnabled = mainLightShadows;
        }
    }

    private IEnumerable<ContributionEntry> GetOrderedContributions()
    {
        return _contributions.Values
            .OrderBy(entry => entry.Intent?.Layer ?? int.MaxValue)
            .ThenBy(entry => entry.Sequence);
    }
}
