sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

float globalTime;
float blurInterpolant;
float blurWeights[12];

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 blurredColor = 0;
    float blurOffset = blurInterpolant * 0.002;
    for (int i = 0; i < 12; i++)
    {
        float blurWeight = blurWeights[i] * 0.5;
        blurredColor += tex2D(baseTexture, coords - float2(0, i * blurOffset)) * blurWeight;
        blurredColor += tex2D(baseTexture, coords + float2(0, i * blurOffset)) * blurWeight;
    }
    
    return lerp(tex2D(baseTexture, coords), blurredColor, sqrt(blurInterpolant)) * sampleColor;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}