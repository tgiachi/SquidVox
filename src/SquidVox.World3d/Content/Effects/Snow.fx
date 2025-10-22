float4x4 World;
float4x4 View;
float4x4 Projection;

float3 CameraRight;
float3 CameraUp;
float Time;
float Intensity;

struct SnowVertexInput
{
    float3 Position : POSITION0;
    float2 Corner : TEXCOORD0;
    float Size : TEXCOORD1;
    float Alpha : TEXCOORD2;
};

struct SnowVertexOutput
{
    float4 Position : POSITION0;
    float2 Corner : TEXCOORD0;
    float Alpha : TEXCOORD1;
    float Size : TEXCOORD2;
};

SnowVertexOutput VertexShaderFunction(SnowVertexInput input)
{
    SnowVertexOutput output;

    float3 basePos = mul(float4(input.Position, 1.0), World).xyz;
    float3 right = normalize(CameraRight);
    float3 upVec = normalize(CameraUp);

    float flutter = sin(Time * 0.9 + basePos.x * 0.25 + basePos.z * 0.19) * 0.35;
    float3 jitter = right * flutter * 0.4 + upVec * flutter * 0.2;

    float2 centered = input.Corner - 0.5;
    float3 worldPos = basePos + jitter + right * centered.x * input.Size + upVec * centered.y * input.Size;

    float4 viewPos = mul(float4(worldPos, 1.0), View);
    output.Position = mul(viewPos, Projection);
    output.Corner = input.Corner;
    output.Alpha = input.Alpha;
    output.Size = input.Size;

    return output;
}

float4 PixelShaderFunction(SnowVertexOutput input) : COLOR0
{
    float density = saturate(Intensity);
    if (density <= 0.001)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float2 uv = input.Corner - 0.5;
    float dist = dot(uv, uv);
    float softMask = saturate(1.0 - dist * 4.0);

    float sparkle = 0.65 + 0.35 * sin(Time * 4.0 + uv.x * 8.0 + uv.y * 6.0);
    float alpha = saturate(input.Alpha * density * softMask);

    float3 color = float3(0.92, 0.95, 1.0) * sparkle;

    return float4(color, alpha);
}

technique Snow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
