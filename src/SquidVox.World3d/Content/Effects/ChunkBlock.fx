// ChunkBlockSimple Shader - MonoGame HLSL
// Converted from OpenGL 3.3 GLSL
// For rendering solid and transparent blocks with directional lighting

// Uniforms
float3 model;
float4x4 view;
float4x4 projection;
float texMultiplier;

// Fog
bool fogEnabled;
float3 fogColor;
float fogStart;
float fogEnd;

// Lighting uniforms
float3 ambient;
float3 lightDirection;

// Texture
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

Texture3D BlockLightTexture;
sampler BlockLightSampler = sampler_state
{
    Texture = <BlockLightTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
};

float3 ChunkDimensions;

// Array of possible normals based on direction
static const float3 normals[7] = {
    float3( 0,  0,  1), // 0 - South
    float3( 0,  0, -1), // 1 - North
    float3( 1,  0,  0), // 2 - East
    float3(-1,  0,  0), // 3 - West
    float3( 0,  1,  0), // 4 - Top
    float3( 0, -1,  0), // 5 - Bottom
    float3( 0, -1,  0)  // 6 - Default
};

// Vertex shader input
struct VertexShaderInput
{
    float3 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TileCoord : TEXCOORD0;
    float2 TileBase : TEXCOORD1;
    float2 TileSize : TEXCOORD2;
    float3 BlockCoord : TEXCOORD3;
};

// Vertex shader output / Pixel shader input
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 Normal : TEXCOORD0;
    float FogFactor : TEXCOORD1;
    float2 TileCoord : TEXCOORD2;
    float2 TileBase : TEXCOORD3;
    float2 TileSize : TEXCOORD4;
    float3 BlockCoord : TEXCOORD5;
};

// Vertex Shader
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = float4(input.Position + model, 1.0);
    float4 viewPosition = mul(worldPosition, view);
    output.Position = mul(viewPosition, projection);

    output.TileCoord = input.TileCoord;
    output.TileBase = input.TileBase;
    output.TileSize = input.TileSize;
    output.BlockCoord = input.BlockCoord;

    // Extract direction from color.a and use it to get normal
    int direction = int(round(input.Color.a * 255.0f));
    output.Normal = normals[clamp(direction, 0, 6)];

    // Calculate fog
    if (fogEnabled)
    {
        float distance = length(viewPosition.xyz);
        output.FogFactor = saturate((fogEnd - distance) / (fogEnd - fogStart));
    }
    else
    {
        output.FogFactor = 1.0;
    }

    return output;
}

// Pixel Shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 tiledCoord = frac(float2(input.TileCoord.x, 1.0 - input.TileCoord.y));
    float2 atlasCoord = input.TileBase + tiledCoord * input.TileSize;
    float4 texResult = tex2D(texSampler, atlasCoord * texMultiplier);
    
    // Discard transparent pixels
    if (texResult.a == 0.0)
        discard;

    float3 lightDir = normalize(lightDirection);
    float diff = max(dot(input.Normal, lightDir), 0.0);
    float3 diffuse = diff * float3(1.0, 1.0, 1.0);

    float lightFactor = 1.0f;

    float3 color = texResult.rgb * (ambient + diffuse) * lightFactor;
    
    // Apply fog
    if (fogEnabled)
    {
        color = lerp(fogColor, color, input.FogFactor);
    }
    
    return float4(color, texResult.a);
}

// Technique
technique ChunkBlockSimple
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
