sampler noiseScrollTexture : register(s1);

float globalTime;
float edgeGlowIntensity;
float2 laserDirection;
float3 edgeColorSubtraction;
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

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = input.Color;
    
    // Account for texture distortion artifacts in accordance with the primitive distortion fixes.
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;
    
    float distanceFromCenter = distance(coords.y, 0.5);
    float edgeGlow = edgeGlowIntensity / pow(distanceFromCenter, 0.9);
    color = saturate(color * edgeGlow);
    color.rgb -= distanceFromCenter * edgeColorSubtraction;
    
    float noise = tex2D(noiseScrollTexture, coords * float2(0.8, 1.75) + float2(globalTime * -3.3, 0));
    return color * smoothstep(0.5, 0.3, distanceFromCenter) * (noise + 1 + step(0.5, noise + (0.5 - distanceFromCenter)));
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
