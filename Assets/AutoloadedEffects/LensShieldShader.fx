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
    float edgeGlow = pow(edgeGlowFactor / distanceFromEdge, 0.8);
    float centerGlow = pow(centerGlowFactor / distanceFromCenter, 2.5);
    float glow = edgeGlow + centerGlow;
    
    // Calculate noise values.
    float2 noiseCoords = RotatedBy(coords - 0.5, globalTime * 1.75) + 0.5;
    float2 techNoiseCoords = coords * 0.45;
    float radialNoise = tex2D(noiseTexture, noiseCoords).r;
    float techNoise = 0;
    float techNoiseAmplitude = 0.75;
    float techNoiseFrequency = 1;
    for (int i = 0; i < 4; i++)
    {
        techNoise += tex2D(techyNoiseTexture, techNoiseCoords + float2(0, globalTime * 0.03) * techNoiseFrequency + radialNoise * 0.02).r * techNoiseAmplitude;
        techNoiseFrequency *= 1.67;
        techNoiseAmplitude *= 0.6;
    }
    
    float noiseResult = clamp(radialNoise + techNoise, 0, 2);
    
    // Combine everything together.
    float4 color = glowColor * pow(noiseResult, 3.5 + (0.5 - distanceFromCenter) * 10) * 0.83;
    return (glow + color * saturate(1 - glow)) * sampleColor * edgeFadeOut;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}