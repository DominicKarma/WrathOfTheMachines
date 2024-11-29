sampler baseTexture : register(s0);

float globalTime;
float blur;
float glowIntensity;
float spin;
float gradientCount;
float2 sizeCorrection;
float2 pixelationFactor;
float4 gradient[8];

float4 PaletteLerp(float interpolant)
{
    int startIndex = clamp(frac(interpolant) * gradientCount, 0, gradientCount - 1);
    int endIndex = (startIndex + 1) % gradientCount;
    return lerp(gradient[startIndex], gradient[endIndex], frac(interpolant * gradientCount));
}

float2 RotatedBy(in float2 coords, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    
    float2 correctedCoords = (coords - 0.5) * sizeCorrection;
    
    return float2(correctedCoords.x * c - correctedCoords.y * s, correctedCoords.x * s + correctedCoords.y * c) + 0.5;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Apply base pixelation to the refraction rotor.
    coords = floor(coords / pixelationFactor) * pixelationFactor;
    
    float distanceFromCenter = distance(coords, 0.5);
    
    // Smear the color with rotational blur.
    float4 smearedColor = 0;
    for (int i = 0; i < 32; i++)
    {
        float2 rotatedCoords = RotatedBy(coords, i * blur + spin);
        bool erasePixel = rotatedCoords.x < 0 || rotatedCoords.y < 0 || rotatedCoords.x > 1 || rotatedCoords.y > 1;
   
        smearedColor += tex2D(baseTexture, rotatedCoords) * (1 - erasePixel) / clamp(32 - blur * 200, 1, 32);
    }
    
    // Calculate a center glow color based on the angle from the center of the texture.
    // This is intended to enhance the vibrancy of the inner rainbow part of the refraction rotor when it's spinning extremely fast.
    float glow = pow(glowIntensity / (distanceFromCenter - 0.11), 2) * smoothstep(0.4, 0.2, distanceFromCenter);
    float glowColorHue = atan2(coords.y - 0.5, coords.x - 0.5) / 6.283 + 0.5;
    float4 glowColor = PaletteLerp(glowColorHue);
    
    // Combine the rotationally smeared color and glow.
    return saturate(smearedColor + glowColor * glow) * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}