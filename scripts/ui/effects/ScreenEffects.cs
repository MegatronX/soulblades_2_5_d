using Godot;

/// <summary>
/// Manages full-screen visual effects like flashes and color inversion.
/// </summary>
public partial class ScreenEffects : CanvasLayer
{
    private ColorRect _flashRect;
    private ColorRect _invertRect;
    private ShaderMaterial _invertMaterial;

    public override void _Ready()
    {
        // High layer to ensure it renders over most battle UI/3D scene
        Layer = 100; 
        
        // 1. Flash Rect (Simple Color Overlay)
        _flashRect = new ColorRect();
        _flashRect.Color = new Color(1, 1, 1, 0);
        _flashRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _flashRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_flashRect);

        // 2. Invert Rect (Shader)
        _invertRect = new ColorRect();
        _invertRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _invertRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        var shader = new Shader();
        shader.Code = @"
            shader_type canvas_item;
            uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
            uniform float strength : hint_range(0.0, 1.0) = 0.0;

            void fragment() {
                vec4 c = texture(screen_texture, SCREEN_UV);
                vec4 inverted = vec4(1.0 - c.rgb, c.a);
                COLOR = mix(c, inverted, strength);
            }
        ";
        
        _invertMaterial = new ShaderMaterial();
        _invertMaterial.Shader = shader;
        _invertRect.Material = _invertMaterial;
        
        AddChild(_invertRect);
    }

    public void Flash(Color color, float duration = 0.15f)
    {
        _flashRect.Color = color;
        
        var tween = CreateTween();
        tween.TweenProperty(_flashRect, "color:a", 0.0f, duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    public void Invert(float duration = 0.1f)
    {
        // Set strength to 1 immediately
        _invertMaterial.SetShaderParameter("strength", 1.0f);

        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(v => _invertMaterial.SetShaderParameter("strength", v)), 1.0f, 0.0f, duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }
}
