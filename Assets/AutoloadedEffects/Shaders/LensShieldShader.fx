sampler noiseTexture : register(s0);
sampler techyNoiseTexture : register(s1);

float globalTime;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Decide color data, starting with the sample color as a base.
    float4 color = sampleColor;
    
    // Acquire texture data on a per-fragment basis. Each color channel encodes a specific thing.
    // Blue = influence of noise.
    // Red = Tendency towards white.
    // Green = Shade.
    float4 textureData = tex2D(noiseTexture, coords);
    
    // Brighten colors based on the red color channel.
    float brightnessPulse = sin(coords.x * 20 + globalTime * 15);
    float brightness = textureData.r * (brightnessPulse * 0.5 + 1.5);
    color = lerp(color, color.a, brightness);
    
    // Darken colors based on the green color channel.
    color -= float4(0, 0.81, 0.44, 0) * textureData.g * 0.5;
    
    // Calculate polar coordinates relative to the pupil position for the upcoming noise calculations.
    float2 pupilPosition = float2(0.5, 0.34);
    float angleToPupil = atan2(pupilPosition.y - coords.y, pupilPosition.x - coords.x);
    float distanceFromPupil = distance(coords, pupilPosition);
    float2 polar = float2(angleToPupil / 6.283 + 0.5, distanceFromPupil);
    polar.x += globalTime * 0.2;
    polar.y = frac(polar.y + globalTime * 0.5);
    
    // Apply noise based on the blue color channel.
    float noise = tex2D(techyNoiseTexture, polar * 2 + float2(0, globalTime * 0.15));
    color += noise * color.a * textureData.b;

    // Bias colors towards yellows slightly.
    float yellowInfluence = saturate(textureData.b + distanceFromPupil * 0.5);
    color += float4(1, 0.2, -1, 0) * yellowInfluence * 0.2;
    
    // Brighten the overall result.
    float generalBrightness = sin(globalTime * 5 - yellowInfluence * 10) * 0.5 + 0.5;
    color *= generalBrightness * 0.15 + 1;
    
    return color * textureData.a;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}