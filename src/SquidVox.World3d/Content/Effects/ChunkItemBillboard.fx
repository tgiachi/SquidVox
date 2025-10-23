// ChunkItemBillboard Shader - MonoGame HLSL
// Camera-facing billboard rendering for item sprites

float texMultiplier;
float3 model;
float4x4 view;
float4x4 projection;

bool fogEnabled;
float3 fogColor;
float fogStart;
float fogEnd;

float3 ambient;
float3 lightDirection;

float3 cameraRight;
float3 cameraUp;
float3 cameraForward;

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

struct VertexShaderInput
{
    float3 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
    float2 Offset : TEXCOORD1;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
    float FogFactor : TEXCOORD1;
    float3 Normal : TEXCOORD2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float3 worldCenter = input.Position + model;
    float3 billboardOffset = cameraRight * input.Offset.x + cameraUp * input.Offset.y;
    float3 worldPosition = worldCenter + billboardOffset;

    float4 worldPos4 = float4(worldPosition, 1.0);
    float4 viewPosition = mul(worldPos4, view);
    output.Position = mul(viewPosition, projection);

    output.TexCoord = input.TexCoord * texMultiplier;
    output.Color = input.Color;

    output.Normal = normalize(-cameraForward);

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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 texResult = tex2D(texSampler, input.TexCoord);
    clip(texResult.a - 0.001);

    float3 lightDir = normalize(-lightDirection);
    float diff = max(dot(input.Normal, lightDir), 0.0);
    float3 lighting = saturate(ambient + diff);

    float4 color = texResult * input.Color * float4(lighting, 1.0);

    if (fogEnabled)
    {
        color.rgb = lerp(fogColor, color.rgb, input.FogFactor);
    }

    return color;
}

technique ChunkItemBillboard
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
