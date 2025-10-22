// Clouds Shader - MonoGame HLSL
// Converted from OpenGL ES 3.0 GLSL
// Renders volumetric clouds with face-based shading

// Uniforms
float4x4 Model;
float4x4 View;
float4x4 Projection;

// Lighting uniforms
float3 ambient;
float3 lightDirection;

// Vertex shader input
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
};

// Vertex shader output / Pixel shader input
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 Normal : TEXCOORD0;
};

// Vertex Shader
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    // Transform normal to world space
    float3x3 normalMatrix = transpose((float3x3)Model);
    output.Normal = mul(input.Normal, normalMatrix);
    
    // Transform position
    float4 worldPos = mul(input.Position, Model);
    float4 viewPos = mul(worldPos, View);
    output.Position = mul(viewPos, Projection);
    
    return output;
}

// Pixel Shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float3 cloudColor = float3(1.0, 1.0, 1.0);
    float3 norm = normalize(input.Normal);
    
    float faceShading = 1.0;
    
    // Top/bottom faces (Y axis)
    if (abs(norm.y) > 0.9)
    {
        faceShading = 1.0;
    }
    // Left/right faces (X axis)
    else if (abs(norm.x) > 0.9)
    {
        faceShading = 0.85;
    }
    // Front/back faces (Z axis)
    else
    {
        faceShading = 0.7;
    }
    
    cloudColor *= faceShading;
    
    return float4(cloudColor, 0.75);
}

technique Clouds
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
