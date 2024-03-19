sampler noiseTexture : register(s0);
sampler techyNoiseTexture : register(s1);

float globalTime;
float edgeGlowIntensity;
float centerGlowIntensity;
float4 glowColor;

float2 RotatedBy(float2 v, float angle)
{
    float cosine = cos(angle);
    float sine = sin(angle);
    return float2(v.x * cosine - v.y * sine, v.x * sine + v.y * cosine);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Distort coordinates so that they resemble a pupil, somewhat.
    coords.x = (coords.x - 0.5) * 1.5 + 0.5;
    coords.x -= sin(coords.y * 3.141) * 0.18;
    
    // Calculate distance values.
    float distanceFromCenter = distance(coords, 0.5);
    float distanceFromEdge = distance(distanceFromCenter, 0.45);
    
    // Calculate and combine glow colors.
    float edgeFadeOut = smoothstep(0.5, 0.4, distanceFromCenter);
    float edgeGlowFactor = (1 + sin(globalTime * 60) * 0.045) * edgeGlowIntensity;
    float centerGlowFactor = (1 + cos(globalTime * 40) * 0.048) * centerGlowIntensity;
    float edgeGlow = pow(edgeGlowFactor / distanceFromEdge, 0.9);
    float centerGlow = pow(centerGlowFactor / distanceFromCenter, 2.5);
    float glow = edgeGlow + centerGlow;
    
    // Calculate noise values.
    float2 noiseCoords = RotatedBy(coords - 0.5, globalTime * 0.3) + 0.5;
    float noiseResult = clamp(tex2D(noiseTexture, noiseCoords).r + tex2D(techyNoiseTexture, coords * 0.5).r, 0, 2);
    
    // Combine everything together.
    float4 color = glowColor * pow(noiseResult, 3.5 + (0.5 - distanceFromCenter) * 10) * 0.63;
    return (glow + color * saturate(1 - glow)) * sampleColor * edgeFadeOut;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}