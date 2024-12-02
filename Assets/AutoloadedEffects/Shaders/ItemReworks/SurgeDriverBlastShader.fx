sampler scrollTextureA : register(s1);
sampler scrollTextureB : register(s2);
sampler noiseTexture : register(s3);

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
    
    float scrollDistortion = tex2D(noiseTexture, coords * 0.5 + float2(globalTime * -2, 0)) - 0.5;
    float scrollA = tex2D(scrollTextureA, coords * float2(2, 1) + float2(globalTime * -6.96, scrollDistortion * 0.15));
    float scrollB = tex2D(scrollTextureB, coords * float2(2, 1) + float2(globalTime * -6.96, scrollDistortion * 0.15)) * 0.6;
    float scroll = scrollA + scrollB;
    
    float4 result = innerGlowColor * innerGlow + pow(scroll, 2) * color * 3 + smoothstep(2, 3, innerGlow);
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
