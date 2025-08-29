struct Point
{
    float inputHue;
    float hueShift;
    float saturation;
    float luminance;
};
cbuffer Constants : register(b0)
{
    int numPoints;
    float factor;
    float colorTolerance;
    int numIgnoredColors;
    float4 ignoredColors[16];
    Point points[16];
};

Texture2D<float4> InputTexture : register(t0);
SamplerState InputSampler : register(s0);

float3 RGBToHSL(float3 color)
{
    float r = color.r;
    float g = color.g;
    float b = color.b;

    float max_val = max(max(r, g), b);
    float min_val = min(min(r, g), b);

    float h = 0.0;
    float s = 0.0;
    float l = (max_val + min_val) / 2.0;
    if (max_val == min_val)
    {
        h = s = 0.0;
    }
    else
    {
        float d = max_val - min_val;
        float divisor = (l > 0.5) ? (2.0 - max_val - min_val) : (max_val + min_val);
        if (divisor > 0.0)
        {
            s = d / divisor;
        }
        else
        {
            s = 0.0;
        }

        if (max_val == r)
        {
            h = (g - b) / d + (g < b ? 6.0 : 0.0);
        }
        else if (max_val == g)
        {
            h = (b - r) / d + 2.0;
        }
        else
        {
            h = (r - g) / d + 4.0;
        }
        h /= 6.0;
    }
    return float3(h, s, l);
}

float HueToRGB(float f1, float f2, float hue)
{
    hue = frac(hue + 1.0);
    if ((6.0 * hue) < 1.0)
        return f1 + (f2 - f1) * 6.0 * hue;
    if ((2.0 * hue) < 1.0)
        return f2;
    if ((3.0 * hue) < 2.0)
        return f1 + (f2 - f1) * ((2.0 / 3.0) - hue) * 6.0;
    return f1;
}

float3 HSLToRGB(float3 hsl)
{
    if (hsl.y == 0.0)
    {
        return float3(hsl.z, hsl.z, hsl.z);
    }
    else
    {
        float f2 = hsl.z < 0.5 ?
        hsl.z * (1.0 + hsl.y) : (hsl.z + hsl.y) - (hsl.y * hsl.z);
        float f1 = 2.0 * hsl.z - f2;
        return float3(
            HueToRGB(f1, f2, hsl.x + (1.0 / 3.0)),
            HueToRGB(f1, f2, hsl.x),
            HueToRGB(f1, f2, hsl.x - (1.0 / 3.0))
        );
    }
}

float hueDifference(float h1, float h2)
{
    float diff = abs(h1 - h2);
    return min(diff, 360.0 - diff);
}

float4 main(float4 pos : SV_POSITION, float4 posScene : SCENE_POSITION, float4 uv0 : TEXCOORD0) : SV_Target
{
    float4 color = InputTexture.Sample(InputSampler, uv0.xy);
    if (color.a < 0.001)
    {
        return color;
    }

    float3 originalRgb = color.rgb / color.a;
    
    for (int j = 0; j < numIgnoredColors; j++)
    {
        float3 ignoredRgb = ignoredColors[j].rgb;
        float distance = length(originalRgb - ignoredRgb);
        if (distance < colorTolerance)
        {
            return color;
        }
    }

    if (numPoints < 1)
    {
        return color;
    }

    float3 hsl = RGBToHSL(originalRgb);
    float inputHueDeg = hsl.x * 360.0;
    float hueShift = 0.0;
    float saturation = 1.0;
    float luminance = 1.0;
    if (numPoints == 1)
    {
        hueShift = points[0].hueShift;
        saturation = points[0].saturation;
        luminance = points[0].luminance;
    }
    else
    {
        int p1_idx = 0;
        int p2_idx = 1;

        for (int i = 0; i < numPoints; i++)
        {
            if (points[i].inputHue > inputHueDeg)
            {
                break;
            }
            p1_idx = i;
        }
        p2_idx = (p1_idx + 1) % numPoints;

        float h1 = points[p1_idx].inputHue;
        float h2 = points[p2_idx].inputHue;

        if (h2 < h1)
            h2 += 360;
        float currentHue = inputHueDeg;
        if (currentHue < h1)
            currentHue += 360;
        float totalDist = h2 - h1;
        float t = (totalDist > 0.001) ? saturate((currentHue - h1) / totalDist) : 0.0;
        hueShift = lerp(points[p1_idx].hueShift, points[p2_idx].hueShift, t);
        saturation = lerp(points[p1_idx].saturation, points[p2_idx].saturation, t);
        luminance = lerp(points[p1_idx].luminance, points[p2_idx].luminance, t);
    }

    float3 modifiedHsl = hsl;
    modifiedHsl.x = frac((modifiedHsl.x * 360.0 + hueShift) / 360.0);
    modifiedHsl.y = saturate(modifiedHsl.y * saturation);
    modifiedHsl.z = saturate(modifiedHsl.z * luminance);
    
    float3 finalRgb = HSLToRGB(modifiedHsl);
    finalRgb = lerp(originalRgb, finalRgb, factor);
    return float4(saturate(finalRgb) * color.a, color.a);
}