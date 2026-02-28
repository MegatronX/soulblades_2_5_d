using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages diffuse omni fill lights for forest/interior lighting context.
/// </summary>
public sealed class DiffuseFillAtmosphereLayer : IAtmosphereLayer
{
    private sealed class Seed
    {
        public float AngleOffset;
        public float RadialScale;
    }

    private Node3D _root;
    private readonly List<OmniLight3D> _lights = new();
    private readonly List<Seed> _seeds = new();

    public void Apply(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile)
    {
        EnsureRoot(context);
        if (_root == null) return;

        bool enabled = profile != null && profile.EnableDiffuseFillLights && profile.DiffuseFillCount > 0;
        if (!enabled)
        {
            _root.Visible = false;
            EnsureLightCount(0);
            _seeds.Clear();
            return;
        }

        _root.Visible = true;
        int count = Mathf.Max(0, profile.DiffuseFillCount);
        EnsureSeedCount(count, context.Rng);
        EnsureLightCount(count);

        float radius = Mathf.Max(0.1f, profile.DiffuseFillRadius);
        float y = Mathf.Max(0f, profile.DiffuseFillHeight);

        for (int i = 0; i < count; i++)
        {
            var light = _lights[i];
            var seed = _seeds[i];
            if (light == null || seed == null) continue;

            float angle = ((Mathf.Tau * i) / Mathf.Max(1, count)) + seed.AngleOffset;
            float radial = radius * Mathf.Lerp(0.35f, 1f, seed.RadialScale);
            light.Position = new Vector3(Mathf.Cos(angle) * radial, y, Mathf.Sin(angle) * radial);

            light.LightColor = profile.DiffuseFillColor;
            light.OmniRange = Mathf.Max(0.5f, profile.DiffuseFillRange);
            light.ShadowEnabled = false;
            light.LightSpecular = 0.05f;
        }

        Update(context, profile, 0f);
    }

    public void Update(SceneAtmosphereRuntimeContext context, SceneAtmosphereProfile profile, float delta)
    {
        if (_lights.Count == 0 || profile == null || !profile.EnableDiffuseFillLights) return;

        float canopy = context.GetResolvedCanopy(profile);
        float canopyFactor = profile.DiffuseFillScalesWithCanopy
            ? Mathf.Lerp(0.35f, 1f, canopy)
            : 1f;

        float fillEnergy =
            profile.DiffuseFillEnergy *
            canopyFactor *
            Mathf.Max(0f, context.RuntimeDiffuseFillIntensityMultiplier);

        foreach (var light in _lights)
        {
            if (light == null) continue;
            light.LightEnergy = Mathf.Max(0f, fillEnergy);
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

        _root = context.SystemNode.GetNodeOrNull<Node3D>("DiffuseFillLights");
        if (_root == null)
        {
            _root = new Node3D { Name = "DiffuseFillLights" };
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
            var light = new OmniLight3D
            {
                Name = $"DiffuseFill_{_lights.Count + 1}"
            };
            _root.AddChild(light);
            _lights.Add(light);
        }
    }

    private static Seed CreateSeed(RandomNumberGenerator rng)
    {
        return new Seed
        {
            AngleOffset = rng.RandfRange(-0.35f, 0.35f),
            RadialScale = rng.RandfRange(0f, 1f)
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
