shader_type spatial;
render_mode blend_mix, cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform vec4 shallow_color : source_color = vec4(0.14, 0.34, 0.30, 1.0);
uniform vec4 deep_color : source_color = vec4(0.06, 0.12, 0.14, 1.0);
uniform vec4 foam_color : source_color = vec4(0.76, 0.92, 0.95, 1.0);

uniform float water_alpha : hint_range(0.0, 1.0, 0.01) = 0.86;
uniform float flow_speed : hint_range(0.0, 4.0, 0.01) = 0.22;
uniform vec2 flow_direction = vec2(0.0, -1.0);
uniform bool use_flow_map = false;
uniform sampler2D flow_map : source_color;
uniform float flow_map_influence : hint_range(0.0, 1.0, 0.01) = 1.0;

uniform float detail_tiling : hint_range(0.1, 64.0, 0.1) = 9.0;
uniform float depth_variation : hint_range(0.0, 1.0, 0.01) = 0.16;
uniform float highlight_intensity : hint_range(0.0, 1.0, 0.01) = 0.22;
uniform float roughness : hint_range(0.0, 1.0, 0.01) = 0.58;
uniform float specular : hint_range(0.0, 1.0, 0.01) = 0.22;
uniform float emission_strength : hint_range(0.0, 1.0, 0.01) = 0.08;

uniform float edge_foam_width : hint_range(0.0, 0.5, 0.001) = 0.085;
uniform float foam_tiling : hint_range(0.1, 64.0, 0.1) = 18.0;
uniform float foam_threshold : hint_range(0.0, 1.0, 0.01) = 0.62;
uniform float foam_intensity : hint_range(0.0, 2.0, 0.01) = 0.62;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);

    float a = hash(i + vec2(0.0, 0.0));
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

void fragment() {
    vec2 uv = UV;

    vec2 dir = flow_direction;
    if (length(dir) < 0.0001) {
        dir = vec2(0.0, -1.0);
    }
    dir = normalize(dir);

    if (use_flow_map) {
        vec2 mapped = texture(flow_map, uv).rg * 2.0 - 1.0;
        if (length(mapped) > 0.001) {
            vec2 mapped_norm = normalize(mapped);
            dir = normalize(mix(dir, mapped_norm, clamp(flow_map_influence, 0.0, 1.0)));
        }
    }

    float t = TIME * flow_speed;
    vec2 uv0 = uv * detail_tiling + dir * t;
    vec2 uv1 = uv * (detail_tiling * 1.73) + vec2(-dir.y, dir.x) * (t * 0.62);

    float n0 = noise(uv0);
    float n1 = noise(uv1 + vec2(13.7, 7.4));
    float flow_mix = mix(n0, n1, 0.45);
    float ripples = 0.5 + 0.5 * sin((uv0.x + uv0.y) * 6.28318 + TIME * flow_speed * 3.8);

    float depth_mask = clamp(uv.y + (flow_mix - 0.5) * depth_variation, 0.0, 1.0);
    vec3 water_color = mix(shallow_color.rgb, deep_color.rgb, depth_mask);

    float shimmer = smoothstep(0.66, 1.0, flow_mix) * highlight_intensity + ripples * 0.08;
    water_color += vec3(shimmer);

    float edge_dist = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
    float edge_mask = 1.0 - smoothstep(0.0, edge_foam_width, edge_dist);
    float foam_noise = noise(uv * foam_tiling - dir * (t * 1.8));
    float foam_mask = smoothstep(foam_threshold, 1.0, foam_noise) * edge_mask * foam_intensity;
    water_color = mix(water_color, foam_color.rgb, clamp(foam_mask, 0.0, 1.0));

    ALBEDO = water_color;
    ROUGHNESS = clamp(roughness + (1.0 - flow_mix) * 0.18, 0.02, 1.0);
    SPECULAR = specular;
    EMISSION = water_color * emission_strength * (0.3 + 0.7 * flow_mix);
    ALPHA = clamp(water_alpha, 0.0, 1.0);
}
