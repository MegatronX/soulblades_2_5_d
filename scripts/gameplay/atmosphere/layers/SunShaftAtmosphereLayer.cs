using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages dynamic sun-shaft spotlights.
/// </summary>
public sealed class SunShaftAtmosphereLayer : IAtmosphereLayer
{
    private sealed class Seed
    {
        public float AngleOffset;
        public float RadialScale;
        public float HeightOffset;
        public float Phase;
    }

    private Node3D _root;
    private readonly List<SpotLight3D> _lights = new();
    private readonly List<Seed> _seeds = new();

    public void Apply(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile)
    {
        EnsureRoot(context);
        if (_root == null) return;

        bool enabled = profile != null && profile.EnableSunShafts && profile.SunShaftCount > 0;
        if (!enabled)
        {
            _root.Visible = false;
            EnsureLightCount(0);
            _seeds.Clear();
            return;
        }

        _root.Visible = true;
        int count = Mathf.Max(0, profile.SunShaftCount);
        EnsureSeedCount(count, context.Rng);
        EnsureLightCount(count);

        float radius = Mathf.Max(0.1f, profile.SunShaftRadius);
        float height = Mathf.Max(0.1f, profile.SunShaftHeight);
        for (int i = 0; i < count; i++)
        {
            var light = _lights[i];
            var seed = _seeds[i];
            if (light == null || seed == null) continue;

            float angle = ((Mathf.Tau * i) / Mathf.Max(1, count)) + seed.AngleOffset;
            float radial = radius * Mathf.Lerp(0.45f, 1f, seed.RadialScale);
            float y = height + seed.HeightOffset;
            var pos = new Vector3(Mathf.Cos(angle) * radial, y, Mathf.Sin(angle) * radial);

            light.Position = pos;
            light.LookAt(new Vector3(pos.X * 0.2f, 0f, pos.Z * 0.2f), Vector3.Up);
            light.LightColor = profile.SunShaftColor;
            light.SpotRange = Mathf.Max(0.5f, profile.SunShaftRange);
            light.SpotAngle = Mathf.Clamp(profile.SunShaftAngle, 2f, 90f);
            light.ShadowEnabled = profile.SunShaftShadowsEnabled;
            light.LightSpecular = 0.2f;
        }

        Update(context, profile, 0f);
    }

    public void Update(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile, float delta)
    {
        if (_lights.Count == 0 || profile == null || !profile.EnableSunShafts) return;

        float canopy = context.GetResolvedCanopy(profile);
        float sunHeight = context.ComputeSunHeightFactor();
        float shaftGate = Mathf.InverseLerp(profile.SunHeightThreshold, 1f, sunHeight);
        shaftGate = Mathf.Clamp(shaftGate, 0f, 1f);

        float canopyFactor = Mathf.Pow(Mathf.Max(0f, canopy), Mathf.Max(0.01f, profile.SunShaftCanopyPower));
        float baseEnergy =
            profile.SunShaftEnergy *
            canopyFactor *
            shaftGate *
            Mathf.Max(0f, context.RuntimeSunShaftIntensityMultiplier);

        for (int i = 0; i < _lights.Count; i++)
        {
            var light = _lights[i];
            if (light == null) continue;

            float phase = (i < _seeds.Count && _seeds[i] != null) ? _seeds[i].Phase : 0f;
            float wave = Mathf.Sin((context.TimeSeconds * profile.SunShaftJitterSpeed) + phase);
            float jitterMul = 1f + (wave * profile.SunShaftEnergyJitter);
            light.LightEnergy = Mathf.Max(0f, baseEnergy * jitterMul);
        }
    }

    public void Clear(SceneAtmosphereRuntimeContext context)
    {
        EnsureRoot(context);
        if (_root != null)
        {
            _root.Visible = false;
        }

        EnsureLightCount(0);
        _seeds.Clear();
    }

    private void EnsureRoot(SceneAtmosphereRuntimeContext context)
    {
        if (_root != null && GodotObject.IsInstanceValid(_root)) return;
        if (context?.SystemNode == null) return;

        _root = context.SystemNode.GetNodeOrNull<Node3D>("SunShaftLights");
        if (_root == null)
        {
            _root = new Node3D { Name = "SunShaftLights" };
            context.SystemNode.AddChild(_root);
        }
    }

    private void EnsureLightCount(int count)
    {
        while (_lights.Count > count)
        {
            int last = _lights.Count - 1;
            if (_lights[last] != null && GodotObject.IsInstanceValid(_lights[last]))
            {
                _lights[last].QueueFree();
            }
            _lights.RemoveAt(last);
        }

        while (_lights.Count < count && _root != null)
        {
            var light = new SpotLight3D
            {
                Name = $"SunShaft_{_lights.Count + 1}"
            };
            _root.AddChild(light);
            _lights.Add(light);
        }
    }

    private static Seed CreateSeed(RandomNumberGenerator rng)
    {
        return new Seed
        {
            AngleOffset = rng.RandfRange(-0.2f, 0.2f),
            RadialScale = rng.RandfRange(0f, 1f),
            HeightOffset = rng.RandfRange(-0.5f, 0.9f),
            Phase = rng.RandfRange(0f, Mathf.Tau)
        };
    }

    private void EnsureSeedCount(int count, RandomNumberGenerator rng)
    {
        if (rng == null) return;

        while (_seeds.Count > count)
        {
            _seeds.RemoveAt(_seeds.Count - 1);
        }

        while (_seeds.Count < count)
        {
            _seeds.Add(CreateSeed(rng));
        }
    }
}
