shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_prepass_alpha, shadows_disabled;

uniform sampler2D texture_albedo : source_color, filter_nearest;
uniform vec4 ghost_tint : source_color = vec4(0.60, 0.82, 1.0, 1.0);
uniform float ghost_strength : hint_range(0.0, 1.5) = 1.0;
uniform float ghost_offset : hint_range(0.0, 0.08) = 0.026;
// Read by CharacterVisualStateController to scale an overlay sprite.
// The shader itself does not expand geometry.
uniform float ghost_expansion : hint_range(0.0, 1.0) = 0.35;
uniform float overlay_scale : hint_range(1.0, 3.0) = 1.0;

vec4 sample_sprite(vec2 uv)
{
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
    {
        return vec4(0.0);
    }
    return texture(texture_albedo, uv);
}

void fragment()
{
    vec2 off1 = vec2(ghost_offset, 0.0);
    vec2 off2 = vec2(-ghost_offset * 0.72, ghost_offset * 0.58);
    vec2 off3 = vec2(0.0, -ghost_offset * 0.74);

    // When rendered on an enlarged overlay sprite, remap UVs back into source content space.
    // This preserves base sprite size and creates real margin outside frame bounds for trails.
    vec2 uv = ((UV - vec2(0.5)) * max(1.0, overlay_scale)) + vec2(0.5);
    vec4 base = sample_sprite(uv);
    vec4 sample_a = sample_sprite(uv + off1);
    vec4 sample_b = sample_sprite(uv + off2);
    vec4 sample_c = sample_sprite(uv + off3);

    float ghost_alpha = max(max(sample_a.a, sample_b.a), sample_c.a) * ghost_strength;
    vec3 ghost_rgb = max(max(sample_a.rgb, sample_b.rgb), sample_c.rgb);

    // Render full displaced silhouettes (not "exterior only"), so ghosts do not get sliced.
    float alpha = clamp(ghost_alpha * (1.0 - (base.a * 0.18)), 0.0, 1.0);
    vec3 ghost_color = mix(ghost_rgb, ghost_tint.rgb, 0.72);
    vec3 color = ghost_color + (ghost_tint.rgb * alpha * 0.18);

    ALBEDO = color;
    ALPHA = alpha;
}
