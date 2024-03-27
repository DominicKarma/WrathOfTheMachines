sampler baseTexture : register(s0);
sampler edgeShapeNoiseTexture : register(s1);

float globalTime;
float2 textureSize0;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Pixelate the results.
    float2 pixelationFactor = textureSize0 * 0.36;
    coords = floor(coords * pixelationFactor) / pixelationFactor;
     
    // Calculate the base color based on distance from the center.
    float distanceFromCenter = distance(coords, 0.5) + tex2D(edgeShapeNoiseTexture, coords * 1.5 + float2(0, globalTime)) * 0.025;
    float brightness = 0.3 * smoothstep(0.5, 0.3, distanceFromCenter) / distanceFromCenter;
    float4 color = saturate(sampleColor * brightness);
    
    // Apply color posteraization.
    color = float4(floor(color.rgb * 12) / 12, 1) * color.a;
    
    return color;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}