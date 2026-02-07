using Godot;

[GlobalClass]
public partial class BattleEnvironment : Node
{
    [Export]
    public WorldEnvironment WorldEnvironment { get; set; }

    [Export]
    public DirectionalLight3D MainLight { get; set; }

    public override void _Ready()
    {
        SetupEnvironment();
        SetupLighting();
    }

    private void SetupEnvironment()
    {
        if (WorldEnvironment == null)
        {
            WorldEnvironment = GetParent().GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
            if (WorldEnvironment == null)
            {
                GD.Print("No WorldEnvironment found. Creating a default HD-2D environment.");
                WorldEnvironment = new WorldEnvironment();
                WorldEnvironment.Name = "WorldEnvironment";
                GetParent().CallDeferred("add_child", WorldEnvironment);
            }
        }

        // Ensure Environment resource exists
        if (WorldEnvironment.Environment == null)
        {
            WorldEnvironment.Environment = new Environment();
        }
        var env = WorldEnvironment.Environment;

        // Ensure CameraAttributes resource exists (required for the BattleCamera's DoF logic)
        if (WorldEnvironment.CameraAttributes == null)
        {
            WorldEnvironment.CameraAttributes = new CameraAttributesPractical();
        }

        // --- HD-2D Aesthetic Settings ---

        // 1. Background & Sky
        // Using a procedural sky with deep, dramatic colors typical of JRPGs.
        env.BackgroundMode = Environment.BGMode.Sky;
        var skyMat = new ProceduralSkyMaterial();
        skyMat.SkyTopColor = new Color("1a1a2e");      // Deep Night Blue
        skyMat.SkyHorizonColor = new Color("5d4e6d");  // Muted Purple Horizon
        skyMat.GroundBottomColor = new Color("0f0f15"); // Dark Ground
        skyMat.GroundHorizonColor = new Color("3e3b4b");
        
        var sky = new Sky();
        sky.SkyMaterial = skyMat;
        env.Sky = sky;

        // 2. Ambient Light
        // A mix of sky light and a base color to ensure sprites aren't too dark in shadows.
        env.AmbientLightSource = Environment.AmbientSource.Sky;
        env.AmbientLightColor = new Color("6c6c80");
        env.AmbientLightSkyContribution = 0.5f;

        // 3. Tonemapping
        // Filmic is essential for handling the high dynamic range of glow/bloom without clipping.
        env.TonemapMode = Environment.ToneMapper.Filmic;
        env.TonemapExposure = 1.0f;

        // 4. Glow (Bloom)
        // Adds the "dreamy" look and makes magic effects pop.
        env.GlowEnabled = true;
        env.GlowIntensity = 0.5f;
        env.GlowBloom = 0.2f;
        env.GlowBlendMode = Environment.GlowBlendModeEnum.Softlight;

        // 5. Fog
        // Adds depth by fading distant objects into the horizon color.
        env.FogEnabled = true;
        env.FogLightColor = new Color("5d4e6d"); // Match horizon
        env.FogDensity = 0.005f; // Subtle fog
        env.FogAerialPerspective = 0.8f;

        // 6. SSAO (Screen Space Ambient Occlusion)
        // Adds contact shadows where objects meet the floor, grounding them.
        env.SsaoEnabled = true;
        env.SsaoRadius = 1.5f;
        env.SsaoIntensity = 2.0f;
        env.SsaoPower = 1.5f;

        // 7. Color Correction (Adjustment)
        // Slight boost to saturation and contrast for a vibrant look.
        env.AdjustmentEnabled = true;
        env.AdjustmentSaturation = 1.1f;
        env.AdjustmentContrast = 1.1f;
    }

    private void SetupLighting()
    {
        if (MainLight == null)
        {
            MainLight = GetParent().GetNodeOrNull<DirectionalLight3D>("DirectionalLight3D");
            if (MainLight == null)
            {
                GD.Print("No DirectionalLight3D found. Creating default HD-2D lighting.");
                MainLight = new DirectionalLight3D();
                MainLight.Name = "DirectionalLight3D";
                GetParent().CallDeferred("add_child", MainLight);
            }
        }

        // Configure Light
        // A side angle (45 deg Y) creates volume on sprites and geometry.
        MainLight.RotationDegrees = new Vector3(-60, 15, 0); 
        
        //MainLight.LightColor = new Color("ff193e"); // Warm Sunlight
        MainLight.LightColor = new Color("fff4e0"); // Warm Sunlight
        MainLight.LightEnergy = 1.2f;
        
        // Shadows are crucial for HD-2D
        MainLight.ShadowEnabled = true;
        MainLight.ShadowBlur = 2.0f; // Soft shadows
    }
}