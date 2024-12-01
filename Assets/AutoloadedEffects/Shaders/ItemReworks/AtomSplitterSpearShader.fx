sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

float globalTime;
float blurInterpolant;
float blurWeights[12];
float2 blurDirection;
float2 portalPosition;
float2 portalDirection;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float4 blurredColor = 0;
    float2 blurOffset = blurDirection * blurInterpolant * 0.0015;
    for (int i = 0; i < 12; i++)
    {
        float blurWeight = blurWeights[i] * 0.5;
        blurredColor += tex2D(baseTexture, coords - i * blurOffset) * blurWeight;
        blurredColor += tex2D(baseTexture, coords + i * blurOffset) * blurWeight;
    }
    
    float2 directionToPortal = normalize(portalPosition - position.xy);
    float enterPortalOpacity = -dot(directionToPortal, portalDirection);
    
    return lerp(tex2D(baseTexture, coords), blurredColor * 0.7, sqrt(blurInterpolant)) * sampleColor * enterPortalOpacity;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}