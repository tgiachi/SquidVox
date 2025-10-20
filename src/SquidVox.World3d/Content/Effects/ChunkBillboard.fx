// ChunkBillboard Shader - MonoGame HLSL
// Converted from OpenGL GLSL
// For rendering vegetation and 2D elements

// Uniforms
float texMultiplier;
float3 model;
float4x4 view;
float4x4 projection;

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

// Lighting constants
static const float3 ambient = float3(0.5, 0.5, 0.5);
static const float3 lightDirection = float3(0.8, 1.0, 0.7);
static const float3 normal = float3(0, -1, 0);

// Vertex shader input
struct VertexShaderInput
{
    float3 Position : POSITION0;
    float2 TexCoords : TEXCOORD0;
};

// Vertex shader output / Pixel shader input
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

// Vertex Shader
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = float4(input.Position + model, 1.0);
    float4 viewPosition = mul(worldPosition, view);
    output.Position = mul(viewPosition, projection);

    output.TexCoord = input.TexCoords * texMultiplier;

    return output;
}

// Pixel Shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 texResult = tex2D(texSampler, input.TexCoord);
    clip(texResult.a - 0.001); // Discard if alpha < 0.001

    float3 lightDir = normalize(-lightDirection);
    float diff = max(dot(normal, lightDir), 0.0);
    float3 diffuse = diff * float3(1, 1, 1);

    float4 lighting = float4(ambient + diffuse, 1.0);

    return texResult * lighting;
}

// Technique
technique ChunkBillboard
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
