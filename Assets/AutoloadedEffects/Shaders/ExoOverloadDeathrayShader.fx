sampler scrollTextureA : register(s1);
sampler scrollTextureB : register(s2);

float globalTime;
float3 scrollColorA;
float3 scrollColorB;
matrix uWorldViewProjection;

float3 exoPalette[8];

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
    float2 TextureCoordinates : TEXCOORD0;
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

float3 PaletteLerp(float interpolant)
{
    int startIndex = clamp(interpolant * 6, 0, 6);
    int endIndex = startIndex + 1;
    return lerp(exoPalette[startIndex], exoPalette[endIndex], frac(interpolant * 6));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = input.Color;
    
    float scrollTime = globalTime * 0.6;
    float colorInterpolantA = smoothstep(0.33, 0.1, tex2D(scrollTextureA, coords + scrollTime * float2(-4.4, -0.3)));
    float colorInterpolantB = smoothstep(0.3, 0.2, tex2D(scrollTextureA, coords * float2(0.9, 1) + scrollTime * float2(-3, 0.3)));
    float darkeningInterpolant = smoothstep(0.5, 0.42, tex2D(scrollTextureB, coords * float2(1, 1.2) + scrollTime * float2(-3, 0)));
    darkeningInterpolant += smoothstep(0.08, 0.01, coords.x);
    
    color = lerp(color, float4(scrollColorA, 1), colorInterpolantA);
    color = lerp(color, float4(scrollColorB, 1), colorInterpolantB);
    color = lerp(color, float4(0, 0, 0, 1), darkeningInterpolant);
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
