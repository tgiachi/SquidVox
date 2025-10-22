float4x4 World;
float4x4 View;
float4x4 Projection;

float3 CameraRight;
float3 RainDirection;
float Time;
float Intensity;
float DropWidth;

struct RainVertexInput
{
    float3 Position : POSITION0;
    float2 Corner : TEXCOORD0;
    float Length : TEXCOORD1;
    float Alpha : TEXCOORD2;
};

struct RainVertexOutput
{
    float4 Position : POSITION0;
    float2 Corner : TEXCOORD0;
    float Alpha : TEXCOORD1;
    float Length : TEXCOORD2;
};

RainVertexOutput VertexShaderFunction(RainVertexInput input)
{
    RainVertexOutput output;

    float3 basePos = mul(float4(input.Position, 1.0), World).xyz;
    float3 right = normalize(CameraRight);
    float3 direction = normalize(RainDirection);

    float widthOffset = (input.Corner.x - 0.5) * DropWidth;
    float lengthOffset = input.Corner.y * input.Length;
    float3 worldPos = basePos + right * widthOffset + direction * lengthOffset;

    float4 viewPos = mul(float4(worldPos, 1.0), View);
    output.Position = mul(viewPos, Projection);
    output.Corner = input.Corner;
    output.Alpha = input.Alpha;
    output.Length = input.Length;

    return output;
}

float4 PixelShaderFunction(RainVertexOutput input) : COLOR0
{
    float density = saturate(Intensity);
    if (density <= 0.001)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float headFade = saturate(1.0 - input.Corner.y);
    float streakFade = saturate(0.35 + headFade * 0.65);
    float shimmer = 0.5 + 0.5 * sin((input.Length * 0.6) + Time * 12.0 + input.Corner.y * 9.42);

    float alpha = saturate(input.Alpha * density * streakFade);
    float brightness = saturate(0.6 + shimmer * 0.3);

    float3 color = float3(0.55, 0.68, 0.9) * brightness;

    return float4(color, alpha);
}

technique Rain
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
