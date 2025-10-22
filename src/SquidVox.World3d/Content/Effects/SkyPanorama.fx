// Sky Panorama Shader - MonoGame HLSL
// Converted from OpenGL GLSL
// For rendering animated 2D panoramic skybox with scrolling effect

// Uniforms
float4x4 Matrix;        // Combined MVP matrix
float Timer;            // Animation timer for scrolling

// Texture
texture SkyTexture;
sampler SkyTextureSampler = sampler_state
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
    float3 Normal : NORMAL0;
    float2 UV : TEXCOORD0;
};

// Vertex shader output / Pixel shader input
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 UV : TEXCOORD0;
};

// Vertex Shader
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Transform position with combined MVP matrix
    output.Position = mul(input.Position, Matrix);

    // Pass UV coordinates to fragment shader
    output.UV = input.UV;

    return output;
}

// Pixel Shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Animate UV coordinates: scroll horizontally based on timer
    float2 animatedUV = float2(input.UV.x + Timer, input.UV.y);

    // Sample the sky texture with animated coordinates
    return tex2D(SkyTextureSampler, animatedUV);
}

technique SkyPanorama
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
