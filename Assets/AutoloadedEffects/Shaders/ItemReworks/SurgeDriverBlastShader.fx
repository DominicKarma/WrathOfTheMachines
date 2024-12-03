sampler scrollTextureA : register(s1);
sampler scrollTextureB : register(s2);
sampler noiseTexture : register(s3);

float noiseOffset;
float globalTime;
float4 innerGlowColor;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;
    
    float4 color = input.Color;
    
    float horizontalDistance = distance(coords.y, 0.5);
    float innerGlow = clamp(smoothstep(0.4, 0.2, horizontalDistance) * 0.04 / horizontalDistance, 0, 255) * color.a;
    
    float scrollDistortion = tex2D(noiseTexture, coords * float2(1, 0.5) + float2(globalTime * -2.95, noiseOffset)) - 0.5;
    float scrollA = tex2D(scrollTextureA, coords * float2(2, 1) + float2(globalTime * -7.2 + noiseOffset, scrollDistortion * 0.18));
    float scrollB = tex2D(scrollTextureB, coords * float2(2, 1) + float2(globalTime * -6.8 + noiseOffset, scrollDistortion * 0.25)) * 0.6;
    float scroll = scrollA + scrollB;
    
    float4 result = innerGlowColor * innerGlow + pow(scroll, 2) * color * 3 + smoothstep(0.5, 3, innerGlow);
    float4 baseColor = result;
    
    float colorBias = (1 - scrollB) * result.a;
    result.r += colorBias * pow(1 - baseColor.b, 2) * 1.3;
    result.g += colorBias * pow(1 - baseColor.r, 2) * 2.1;
    result.a = 0;
    
    return result;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
