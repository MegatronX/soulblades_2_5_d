using Godot;

/// <summary>
/// Generates a simple test arena with a floor and background pillars.
/// Attach this to a Node3D in your Battle Scene to instantly have geometry.
/// </summary>
[GlobalClass]
public partial class BattleArenaGenerator : Node3D
{
    [Export]
    public Color FloorColor { get; set; } = new Color("252530"); // Dark slate grey

    [Export]
    public Texture2D FloorTexture { get; set; }

    [Export]
    public Vector2 FloorTiling { get; set; } = new Vector2(20, 20);

    [Export]
    public Color BackgroundColor { get; set; } = new Color("1a1a24"); // Darker background

    [Export]
    public float FloorYOffset { get; set; } = 0.0f; // Default to 0. Adjust Sprite3D pivots instead.

    [Export]
    public Godot.Collections.Array<PackedScene> DecorationProps { get; set; } = new();

    [Export]
    public int DecorationCount { get; set; } = 20;

    [Export]
    public float DecorationMinRadius { get; set; } = 8.0f; // Keep center clear for combatants

    [Export]
    public float DecorationMaxRadius { get; set; } = 25.0f;

    [Export]
    public float CameraExclusionAngle { get; set; } = 60.0f; // Cone width in degrees to keep clear towards +Z

    public override void _Ready()
    {
        CreateFloor();
        CreateBackgroundPillars();
        ScatterDecorations();
    }

    public void ApplyProfile(BattleEnvironmentProfile profile)
    {
        if (profile == null) return;

        FloorColor = profile.FloorColor;
        BackgroundColor = profile.BackgroundColor;
        FloorTexture = profile.FloorTexture;
        FloorTiling = profile.FloorTiling;
        FloorYOffset = profile.FloorYOffset;
        DecorationProps = profile.DecorationProps;
        DecorationCount = profile.DecorationCount;
        DecorationMinRadius = profile.DecorationMinRadius;
        DecorationMaxRadius = profile.DecorationMaxRadius;
        CameraExclusionAngle = profile.CameraExclusionAngle;

        // Clear existing geometry
        foreach (Node child in GetChildren())
        {
            child.QueueFree();
        }

        // Regenerate with new settings
        CreateFloor();
        CreateBackgroundPillars();
        ScatterDecorations();
    }

    private void CreateFloor()
    {
        // A large plane for the ground
        var planeMesh = new PlaneMesh();
        planeMesh.Size = new Vector2(100, 100);

        var material = new StandardMaterial3D();
        material.AlbedoColor = FloorColor;

        if (FloorTexture != null)
        {
            material.AlbedoTexture = FloorTexture;
            material.Uv1Scale = new Vector3(FloorTiling.X, FloorTiling.Y, 1);
            material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        }

        material.Roughness = 0.8f; // Slightly rough to catch light but not be a mirror
        material.SpecularMode = BaseMaterial3D.SpecularModeEnum.SchlickGgx;

        var floor = new MeshInstance3D();
        floor.Name = "ArenaFloor";
        floor.Mesh = planeMesh;
        floor.MaterialOverride = material;
        floor.Position = new Vector3(0, FloorYOffset, 0);
        
        // Add collision just in case we need raycasting later
        floor.CreateTrimeshCollision();

        AddChild(floor);
    }

    private void CreateBackgroundPillars()
    {
        var material = new StandardMaterial3D();
        material.AlbedoColor = BackgroundColor;
        material.Roughness = 1.0f;

        // Create a row of pillars in the background to give depth
        for (int i = -4; i <= 4; i++)
        {
            var pillar = new MeshInstance3D();
            pillar.Name = $"Pillar_{i}";
            
            // Position: Spread out along X, sitting on the floor, pushed back in Z
            float xPos = i * 8.0f;
            float zPos = -12.0f; // Behind the combat area (which is usually around Z=0)
            
            // Add some random height variation
            float height = 8.0f + (Mathf.Abs(i) * 1.5f);
            pillar.Mesh = new BoxMesh { Size = new Vector3(2, height, 2) };
            pillar.MaterialOverride = material;
            pillar.Position = new Vector3(xPos, (height / 2.0f) + FloorYOffset, zPos);

            AddChild(pillar);
        }
    }

    private void ScatterDecorations()
    {
        if (DecorationProps == null || DecorationProps.Count == 0) return;

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        float exclusionRad = Mathf.DegToRad(CameraExclusionAngle / 2.0f);

        for (int i = 0; i < DecorationCount; i++)
        {
            // Pick a random prop scene
            var scene = DecorationProps[rng.RandiRange(0, DecorationProps.Count - 1)];
            var prop = scene.Instantiate<Node3D>();

            // Random position in a donut shape around the center
            float angle = rng.RandfRange(0, Mathf.Tau);

            float dist = rng.RandfRange(DecorationMinRadius, DecorationMaxRadius);
            
            float x = Mathf.Cos(angle) * dist;
            float z = Mathf.Sin(angle) * dist;

            prop.Position = new Vector3(x, FloorYOffset, z);
            prop.RotationDegrees = new Vector3(0, rng.RandfRange(0, 360), 0);
            
            // Optional: Random scale for variety
            float scale = rng.RandfRange(0.8f, 1.2f);
            prop.Scale = Vector3.One * scale;

            AddChild(prop);

            // Check if we are in the exclusion zone (towards +Z)
            float diff = Mathf.Abs(Mathf.AngleDifference(angle, Mathf.Pi / 2.0f));
            if (diff < exclusionRad)
            {
                // Fade out based on how close to the center of the exclusion zone it is
                float t = diff / exclusionRad;
                // Ensure it's not completely invisible, or use 0 for full fade
                float opacity = Mathf.Clamp(t, 0.1f, 1.0f); 
                ApplyPropFade(prop, opacity);
            }
        }
    }

    private void ApplyPropFade(Node node, float opacity)
    {
        if (node is MeshInstance3D meshInstance)
        {
            int surfaceCount = meshInstance.Mesh?.GetSurfaceCount() ?? 0;
            for (int i = 0; i < surfaceCount; i++)
            {
                Material mat = meshInstance.GetActiveMaterial(i);
                if (mat is StandardMaterial3D stdMat)
                {
                    var newMat = (StandardMaterial3D)stdMat.Duplicate();
                    newMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                    Color col = newMat.AlbedoColor;
                    col.A = opacity;
                    newMat.AlbedoColor = col;
                    meshInstance.SetSurfaceOverrideMaterial(i, newMat);
                }
            }
        }

        foreach (Node child in node.GetChildren())
        {
            ApplyPropFade(child, opacity);
        }
    }
}
