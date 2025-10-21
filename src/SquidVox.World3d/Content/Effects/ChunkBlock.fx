// ChunkBlock Shader - Converted from GLSL to MonoGame HLSL
// Features: AO, per-vertex lighting, fog, daylight system
//
// MonoGame uses row-major matrices and transposes them automatically when passing to shaders.
// We use mul(vector, matrix) for correct MonoGame/XNA compatibility.

// Uniforms
float4x4 mvpMatrix;       // Combined Model-View-Projection matrix
float4x4 worldMatrix;     // World matrix for normal transformation
float3 camera;            // Camera position for fog calculation
float fog_distance;       // Distance at which fog reaches maximum
bool ortho;               // Orthographic projection flag
float timer;              // Time for sky animation
float daylight;           // Daylight intensity (0-1)

// Textures
texture tex;
sampler texSampler = sampler_state
{
    Texture = <tex>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture sky_tex;
sampler skySampler = sampler_state
{
    Texture = <sky_tex>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Constants
static const float PI = 3.14159265;
static const float3 light_direction = normalize(float3(-1.0, 1.0, -1.0));

// Vertex shader input
struct VertexShaderInput
{
    float4 Position : POSITION0;  // xyz position, w unused
    float3 Normal : NORMAL0;      // Normal vector
    float4 UV : TEXCOORD0;        // xy=texcoords, z=AO, w=light
};

// Vertex shader output / Pixel shader input
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 UV : TEXCOORD0;
    float AO : TEXCOORD1;
    float Light : TEXCOORD2;
    float FogFactor : TEXCOORD3;
    float FogHeight : TEXCOORD4;
    float Diffuse : TEXCOORD5;
    float3 WorldPos : TEXCOORD6;
};

// Vertex Shader
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Calculate world position
    float4 worldPosition = mul(input.Position, worldMatrix);
    output.WorldPos = worldPosition.xyz;

    // Transform to clip space
    output.Position = mul(input.Position, mvpMatrix);

    // Pass through UV coordinates
    output.UV = input.UV.xy;

    // Extract AO from UV.z (0.3 base + 0.7 based on inverse of uv.z)
    output.AO = 0.3 + (1.0 - input.UV.z) * 0.7;

    // Extract per-vertex light from UV.w
    output.Light = input.UV.w;

    // Transform normal to world space (use only rotation part of world matrix)
    // For normals, we should use the inverse transpose, but for uniform scale it's the same as the matrix itself
    // MonoGame/XNA uses mul(vector, matrix) order
    float3 worldNormal = normalize(mul(input.Normal, (float3x3)worldMatrix));

    // Calculate diffuse lighting using world-space normal
    output.Diffuse = max(0.0, dot(worldNormal, light_direction));

    // Fog calculation using world space position
    float camera_distance = distance(camera, worldPosition.xyz);
    float fogCalc = pow(saturate(camera_distance / fog_distance), 4.0);

    float dy = worldPosition.y - camera.y;
    float dx = distance(worldPosition.xz, camera.xz);
    float fogHeightCalc = (atan2(dy, dx) + PI / 2.0) / PI;

    // Use ortho as a multiplier to avoid branching (ortho is bool, converted to 0.0 or 1.0)
    float orthoMask = 1.0 - (ortho ? 1.0 : 0.0);
    output.FogFactor = fogCalc * orthoMask;
    output.FogHeight = fogHeightCalc;

    return output;
}

// Pixel Shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Sample texture
    float4 texColor = tex2D(texSampler, input.UV);
    float3 color = texColor.rgb;

    // Discard fully transparent pixels
    clip(texColor.a - 0.001);

    // Use diffuse and AO directly
    float df = input.Diffuse;
    float ao = input.AO;

    // Add per-vertex light to AO and diffuse
    ao = min(1.0, ao + input.Light);
    df = min(1.0, df + input.Light);

    // Calculate lighting value based on daylight + per-vertex light
    float value = min(1.0, daylight + input.Light);
    float3 light_color = float3(value * 0.3 + 0.2, value * 0.3 + 0.2, value * 0.3 + 0.2);
    float3 ambient = float3(value * 0.3 + 0.2, value * 0.3 + 0.2, value * 0.3 + 0.2);

    // Combine lighting
    float3 light = ambient + light_color * df;
    color = clamp(color * light * ao, float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0));

    // Skip fog for now to avoid sky texture requirement
    // float3 sky_color = tex2D(skySampler, float2(timer, input.FogHeight)).rgb;
    // color = lerp(color, sky_color, input.FogFactor);

    return float4(color, 1.0);
}

// Technique
technique ChunkBlock
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
