shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque;

uniform sampler2D texture_albedo : source_color, filter_nearest;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float slice_angle : hint_range(0.0, 360.0) = 45.0;
uniform float separation_dist : hint_range(0.0, 1.0) = 0.2;
uniform float noise_scale = 64.0;
uniform float shake_intensity : hint_range(0.0, 0.1) = 0.03;
uniform vec4 burn_color : source_color = vec4(0.3, 0.3, 0.3, 0.25);

// Simple pseudo-random noise
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    vec2 uv = UV;
    
    // --- Slice Logic ---
    float angle_rad = radians(slice_angle);
    vec2 dir = vec2(cos(angle_rad), sin(angle_rad));
    vec2 normal = vec2(-dir.y, dir.x);
    
    // Center UV for rotation calculation (assuming 0-1 UV space)
    vec2 centered_uv = uv - 0.5;
    float dist = dot(centered_uv, normal);
    
    // Offset UVs based on which side of the line they are on
    float side = sign(dist);
    vec2 offset = normal * side * progress * separation_dist;
    
    // Shake Logic: Jitter the UVs based on progress to simulate instability
    // Include 'side' in the hash so the two halves shake independently
    vec2 shake = vec2(hash(vec2(progress, side)), hash(vec2(progress, side + 2.0))) - 0.5;
    shake *= shake_intensity * progress; // Increase shake as it dissolves
    
    vec2 final_uv = uv - offset - shake;
    
    // Check if the sampled UV crossed the cut line.
    // If we are on side A, but sampling from side B, it means we are seeing the "ghost" of the other half.
    float final_dist = dot(final_uv - 0.5, normal);
    if (dist * final_dist < 0.0) {
        discard;
    }
    
    // --- Sample Texture ---
    vec4 albedo = texture(texture_albedo, final_uv);
    
    // --- Dissolve Logic ---
    // Use final_uv so the noise pattern moves with the sliced parts.
    // Floor the UVs to create "pixels" or "chunks" of dust.
    float n = hash(floor(final_uv * noise_scale));
    
    if (n < progress) {
        discard;
    }
    
    // --- Edge Burn Effect ---
    if (n < progress + 0.05) {
        ALBEDO = burn_color.rgb * 2.0; // Boost brightness
    } else {
        ALBEDO = albedo.rgb;
    }
    ALPHA = albedo.a;
    
    // Clip pixels that moved outside original bounds to avoid tiling artifacts
    if (final_uv.x < 0.0 || final_uv.x > 1.0 || final_uv.y < 0.0 || final_uv.y > 1.0) {
        discard;
    }
}
