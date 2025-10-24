// Dynamic Sky Shader - MonoGame HLSL
// Converted from OpenGL ES 3.0 GLSL
// Renders a procedural day/night cycle skybox

// Uniforms
float4x4 World;       // World matrix (centers skybox on camera)
float4x4 View;        // View matrix
float4x4 Projection;  // Projection matrix
float Time;           // Time of day (0.0 to 1.0)
float3 SunDirection;  // Normalized sun direction
float UseTexture;
float TextureStrength;
Texture2D SkyTexture;
sampler SkySampler = sampler_state
{
    Texture = <SkyTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Clamp;
};

// Vertex shader input
struct VertexShaderInput
{
    float4 Position : POSITION0;
};

// Vertex shader output / Pixel shader input
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 TexCoords : TEXCOORD0;
};

// Vertex Shader
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Pass position as texture coordinates (direction vector for procedural sky)
    // This is the vertex position in object space, which works as a direction
    output.TexCoords = input.Position.xyz;

    // Transform vertex position through standard WVP pipeline
    // The World matrix centers the skybox on the camera position
    float4 worldPos = mul(float4(input.Position.xyz, 1.0), World);
    float4 viewPos = mul(worldPos, View);
    float4 projPos = mul(viewPos, Projection);

    // CRITICAL: Set z = w to ensure depth becomes 1.0 after perspective divide
    // This renders the skybox at maximum depth (far plane)
    // and ensures it's always behind all other geometry
    output.Position = projPos.xyww;

    return output;
}

// Sky color calculation
float3 GetSkyColor(float3 direction, float t)
{
    float3 dir = normalize(direction);
    
    float vertical_pos = dir.y;
    float sun_angle = t * 2.0 * 3.14159265359;
    float sun_height = sin(sun_angle);
    
    // Color definitions
    float3 day_top = float3(0.502, 0.659, 1.0);
    float3 day_bottom = float3(0.753, 0.847, 1.0);
    
    float3 sunset_top = float3(0.761, 0.322, 0.224);
    float3 sunset_bottom = float3(0.471, 0.329, 0.349);
    
    float3 night_top = float3(0.004, 0.004, 0.008);
    float3 night_bottom = float3(0.035, 0.043, 0.075);
    
    float3 top_color, bottom_color;
    
    // Full day
    if (sun_height > 0.7)
    {
        top_color = day_top;
        bottom_color = day_bottom;
    }
    // Day to sunset transition
    else if (sun_height > 0.0)
    {
        float blend = sun_height / 0.7;
        top_color = lerp(sunset_top, day_top, blend);
        bottom_color = lerp(sunset_bottom, day_bottom, blend);
    }
    // Sunset to night transition
    else if (sun_height > -0.3)
    {
        float blend = (sun_height + 0.3) / 0.3;
        top_color = lerp(night_top, sunset_top, blend);
        bottom_color = lerp(night_bottom, sunset_bottom, blend);
    }
    // Full night
    else
    {
        top_color = night_top;
        bottom_color = night_bottom;
    }
    
    // Horizon rotation effect
    float horizon_rotation = sun_angle * 0.1;
    float rotated_vertical_pos = vertical_pos + sin(horizon_rotation) * 0.1;
    
    float transition_width = 0.08;
    float horizon_center = 0.0;
    
    float blend_factor;
    
    if (rotated_vertical_pos < horizon_center - transition_width)
    {
        blend_factor = 1.0;
    }
    else if (rotated_vertical_pos > horizon_center + transition_width)
    {
        blend_factor = 0.0;
    }
    else
    {
        float transition_pos = (rotated_vertical_pos - horizon_center) / transition_width;
        blend_factor = 1.0 - smoothstep(-1.0, 1.0, transition_pos);
    }
    
    float3 sky_color = lerp(top_color, bottom_color, blend_factor);
    
    // Horizontal variation
    float horizontal_variation = sin(dir.x * 3.14159265359) * cos(dir.z * 3.14159265359) * 0.02;
    sky_color += horizontal_variation;
    
    // Horizon glow during sunrise/sunset
    if (sun_height > -0.3 && sun_height < 0.3)
    {
        float glow_intensity = 1.0 - abs(sun_height) / 0.3;
        float horizon_distance = abs(rotated_vertical_pos - horizon_center);
        float glow = exp(-horizon_distance * 8.0) * glow_intensity * 0.3;
        sky_color += float3(glow * 1.0, glow * 0.6, glow * 0.2);
    }
    
    return sky_color;
}

float3 SampleSkyTexture(float3 direction)
{
    float3 dir = normalize(direction);
    float longitude = atan2(dir.x, dir.z);
    float latitude = asin(clamp(dir.y, -1.0, 1.0));

    float u = longitude / (2.0 * 3.14159265359) + 0.5;
    float v = 0.5 - latitude / 3.14159265359;

    return tex2D(SkySampler, float2(u, v)).rgb;
}

// Renders the sun as a bright disc in the sky
float3 RenderSun(float3 direction, float3 sky_color)
{
    float3 dir = normalize(direction);
    float3 sun_dir = normalize(SunDirection);

    // Only render sun when it's above horizon (Y > 0)
    if (sun_dir.y <= 0.0)
        return sky_color;

    // Calculate angular distance from sun center using dot product
    float sun_dot = dot(dir, sun_dir);

    // Convert to angular distance (acos is expensive, use approximation or threshold)
    // For performance, we can use dot product directly:
    // dot = 1.0 means same direction (0 degrees)
    // dot = 0.0 means 90 degrees

    // Sun parameters (using dot product thresholds)
    float sun_threshold = 0.9995;      // ~1.8 degrees - main sun disc
    float glow_threshold = 0.998;      // ~3.6 degrees - sun glow

    // Sun disc (very bright)
    if (sun_dot > sun_threshold)
    {
        float intensity = (sun_dot - sun_threshold) / (1.0 - sun_threshold);
        float3 sun_color = float3(1.0, 0.98, 0.9) * (1.0 + intensity * 2.0);
        return lerp(sky_color, sun_color, 0.98);
    }
    // Sun glow
    else if (sun_dot > glow_threshold)
    {
        float glow_factor = (sun_dot - glow_threshold) / (sun_threshold - glow_threshold);
        glow_factor = pow(abs(glow_factor), 1.5);

        // Glow color varies based on time (warmer at sunset)
        float3 glow_color = lerp(
            float3(1.0, 0.9, 0.7),    // Day glow
            float3(1.0, 0.7, 0.4),    // Sunset glow
            saturate(1.0 - sun_dir.y * 2.0)
        );

        return lerp(sky_color, glow_color, glow_factor * 0.6);
    }

    return sky_color;
}

// Pixel Shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Get base sky color (procedural gradient)
    float3 procedural_color = GetSkyColor(input.TexCoords, Time);

    // Optionally blend with texture
    float3 texture_color = SampleSkyTexture(input.TexCoords);
    float blend = saturate(TextureStrength * UseTexture);
    float3 sky_color = lerp(procedural_color, texture_color, blend);

    // Add the sun (uses SunDirection from C#)
    sky_color = RenderSun(input.TexCoords, sky_color);

    return float4(sky_color, 1.0);
}

technique DynamicSky
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
