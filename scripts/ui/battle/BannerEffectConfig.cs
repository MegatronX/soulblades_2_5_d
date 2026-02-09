using Godot;

[GlobalClass]
public partial class BannerEffectConfig : Resource
{
    [Export] public float BannerDisplaySeconds = 1.2f;
    [Export] public float BannerFadeSeconds = 0.15f;
    [Export] public float BannerPopStartScale = 0.92f;
    [Export] public float BannerPopScale = 1.06f;
    [Export] public float BannerPopSeconds = 0.12f;
    [Export] public float BannerGlowSeconds = 0.2f;
    [Export] public Color BannerGlowColor = new Color(1.0f, 0.96f, 0.86f, 1f);
    [Export] public bool PlayBannerSound = true;

    [ExportGroup("Sparkles")]
    [Export] public bool PlayBannerSparkles = true;
    [Export] public Texture2D BannerSparkleTexture;
    [Export] public int BannerSparkleCount = 3;
    [Export] public Vector2 BannerSparkleSizeMin = new Vector2(4, 4);
    [Export] public Vector2 BannerSparkleSizeMax = new Vector2(8, 8);
    [Export] public float BannerSparkleInset = 6f;
    [Export] public float BannerSparkleStartScale = 0.4f;
    [Export] public float BannerSparkleDriftX = 10f;
    [Export] public float BannerSparkleDriftY = -10f;
    [Export] public float BannerSparkleFadeInSeconds = 0.08f;
    [Export] public float BannerSparkleHoldSeconds = 0.06f;
    [Export] public float BannerSparkleFadeOutSeconds = 0.2f;
    [Export] public Color BannerSparkleColor = new Color(1f, 0.95f, 0.7f, 0.9f);

    private static BannerEffectConfig _default;

    public static BannerEffectConfig Default => _default ??= new BannerEffectConfig();
}
