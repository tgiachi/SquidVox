// ChunkFluid Shader - MonoGame HLSL
// Converted from OpenGL GLSL
// Includes wave animation and texture animation

// Uniforms
float texMultiplier;
float3 model;
float4x4 view;
float4x4 projection;
float time;

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

// Animation constants
static const int aFrames = 32;
static const float animationTime = 5.0;
static const int texNum = 16;
static const float PI = 3.1415926535;

// Array of possible normals based on direction
static const float3 normals[7] = {
    float3( 0,  0,  1), // 0
    float3( 0,  0, -1), // 1
    float3( 1,  0,  0), // 2
    float3(-1,  0,  0), // 3
    float3( 0,  1,  0), // 4
    float3( 0, -1,  0), // 5
    float3( 0, -1,  0)  // 6
};

// Vertex shader input
struct VertexShaderInput
{
    float3 Position : POSITION0;
    float2 TexCoords : TEXCOORD0;
    float Direction : TEXCOORD1;
    float Top : TEXCOORD2;
};

// Vertex shader output / Pixel shader input
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float3 Normal : TEXCOORD1;
};

// Vertex Shader
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float3 pos = input.Position;

    // Apply wave animation for top surface
    if (int(input.Top) == 1)
    {
        pos.y -= 0.1;
        pos.y += (sin(pos.x * PI / 2.0 + time) + sin(pos.z * PI / 2.0 + time * 1.5)) * 0.05;
    }

    float4 worldPosition = float4(pos + model, 1.0);
    float4 viewPosition = mul(worldPosition, view);
    output.Position = mul(viewPosition, projection);

    // Animated texture coordinates
    float2 currentTex = input.TexCoords;
    float animProgress = fmod(time / animationTime, 1.0);
    float currentFrame = floor(animProgress * aFrames);
    currentTex.x += fmod(currentFrame, texNum);
    currentTex.y += floor(currentFrame / texNum);
    output.TexCoord = currentTex * texMultiplier;

    output.Normal = normals[int(input.Direction)];

    return output;
}

// Pixel Shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 texResult = tex2D(texSampler, input.TexCoord);

    float3 lightDir = normalize(-lightDirection);
    float diff = max(dot(input.Normal, lightDir), 0.0);
    float3 diffuse = diff * float3(1, 1, 1);

    float3 color = texResult.rgb * (ambient + diffuse);

    return float4(color, 0.7);
}

// Technique
technique ChunkFluid
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
