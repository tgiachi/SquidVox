// ChunkSolid Shader - MonoGame HLSL
// Converted from OpenGL GLSL

// Uniforms
float texMultiplier;
float3 model;
float4x4 view;
float4x4 projection;

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
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
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

    float4 worldPosition = float4(input.Position + model, 1.0);
    float4 viewPosition = mul(worldPosition, view);
    output.Position = mul(viewPosition, projection);

    output.TexCoord = input.TexCoords * texMultiplier;
    
    int direction = int(input.Color.a);
    output.Normal = normals[clamp(direction, 0, 6)];

    return output;
}

// Pixel Shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 texResult = tex2D(texSampler, input.TexCoord);
    clip(texResult.a - 0.001); // Discard if alpha < 0.001

    float3 lightDir = normalize(-lightDirection);
    float diff = max(dot(input.Normal, lightDir), 0.0);
    float3 diffuse = diff * float3(1, 1, 1);

    float4 lighting = float4(ambient + diffuse, 1.0);

    return texResult * lighting;
}

// Technique
technique ChunkSolid
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
