struct Point
{
    float inputValue;
    float hueShift;
    float saturation;
    float luminance;
};

cbuffer Constants : register(b0)
{
    int numHuePoints;
    int numLuminancePoints;
    int numSaturationPoints;
    int numIgnoredColors;
    float factor;
    float colorTolerance;
    int interpolationMode;
    float _padding;
    float4 ignoredColors[16];
    Point huePoints[16];
    Point luminancePoints[16];
    Point saturationPoints[16];
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
        s = l > 0.5 ? d / (2.0 - max_val - min_val) : d / (max_val + min_val);
        
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

float HueToRGB(float p, float q, float t)
{
    if (t < 0.0)
        t += 1.0;
    if (t > 1.0)
        t -= 1.0;
    if (t < 1.0 / 6.0)
        return p + (q - p) * 6.0 * t;
    if (t < 1.0 / 2.0)
        return q;
    if (t < 2.0 / 3.0)
        return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
    return p;
}

float3 HSLToRGB(float3 hsl)
{
    if (hsl.y == 0.0)
    {
        return float3(hsl.z, hsl.z, hsl.z);
    }
    else
    {
        float q = hsl.z < 0.5 ? hsl.z * (1.0 + hsl.y) : hsl.z + hsl.y - hsl.z * hsl.y;
        float p = 2.0 * hsl.z - q;
        return float3(
            HueToRGB(p, q, hsl.x + 1.0 / 3.0),
            HueToRGB(p, q, hsl.x),
            HueToRGB(p, q, hsl.x - 1.0 / 3.0)
        );
    }
}

float catmullRom(float p0, float p1, float p2, float p3, float t)
{
    return 0.5 * (
        (2.0 * p1) +
        (-p0 + p2) * t +
        (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t * t +
        (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t * t * t
    );
}

struct CorrectionValues
{
    float hueShift;
    float saturation;
    float luminance;
};

CorrectionValues EvaluateCurve(Point points[16], int numPoints, float inputValue, bool isHue)
{
    CorrectionValues result = (CorrectionValues) 0;
    result.saturation = 1.0;
    result.luminance = 1.0;

    if (numPoints == 0)
        return result;
    
    if (numPoints == 1)
    {
        result.hueShift = points[0].hueShift;
        result.saturation = points[0].saturation;
        result.luminance = points[0].luminance;
        return result;
    }

    int p1_idx = 0;
    for (int i = 0; i < numPoints; i++)
    {
        if (points[i].inputValue > inputValue)
        {
            break;
        }
        p1_idx = i;
    }
    
    int p2_idx = p1_idx + 1;
    if (isHue)
        p2_idx %= numPoints;
    else
        p2_idx = min(p2_idx, numPoints - 1);

    float h1 = points[p1_idx].inputValue;
    float h2 = points[p2_idx].inputValue;
    float currentVal = inputValue;

    if (isHue)
    {
        if (h2 < h1)
            h2 += 360.0;
        if (currentVal < h1)
            currentVal += 360.0;
    }

    float totalDist = h2 - h1;
    float t = (totalDist > 0.0001) ? saturate((currentVal - h1) / totalDist) : 0.0;
    
    if (interpolationMode == 0)
    {
        result.hueShift = lerp(points[p1_idx].hueShift, points[p2_idx].hueShift, t);
        result.saturation = lerp(points[p1_idx].saturation, points[p2_idx].saturation, t);
        result.luminance = lerp(points[p1_idx].luminance, points[p2_idx].luminance, t);
    }
    else
    {
        int p0_idx = p1_idx - 1;
        int p3_idx = p2_idx + 1;

        if (isHue)
        {
            p0_idx = (p1_idx - 1 + numPoints) % numPoints;
            p3_idx = (p2_idx + 1) % numPoints;
        }
        else
        {
            p0_idx = max(0, p0_idx);
            p3_idx = min(numPoints - 1, p3_idx);
        }

        Point p0 = points[p0_idx];
        Point p1 = points[p1_idx];
        Point p2 = points[p2_idx];
        Point p3 = points[p3_idx];

        result.hueShift = catmullRom(p0.hueShift, p1.hueShift, p2.hueShift, p3.hueShift, t);
        result.saturation = catmullRom(p0.saturation, p1.saturation, p2.saturation, p3.saturation, t);
        result.luminance = catmullRom(p0.luminance, p1.luminance, p2.luminance, p3.luminance, t);
    }
    return result;
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

    float3 hsl = RGBToHSL(originalRgb);
    float3 modifiedHsl = hsl;

    CorrectionValues hueCorrection = EvaluateCurve(huePoints, numHuePoints, hsl.x * 360.0, true);
    modifiedHsl.x = frac((modifiedHsl.x * 360.0 + hueCorrection.hueShift) / 360.0);
    modifiedHsl.y = saturate(modifiedHsl.y * hueCorrection.saturation);
    modifiedHsl.z = saturate(modifiedHsl.z * hueCorrection.luminance);

    CorrectionValues lumCorrection = EvaluateCurve(luminancePoints, numLuminancePoints, modifiedHsl.z, false);
    modifiedHsl.y = saturate(modifiedHsl.y * lumCorrection.saturation);

    CorrectionValues satCorrection = EvaluateCurve(saturationPoints, numSaturationPoints, modifiedHsl.y, false);
    modifiedHsl.y = saturate(modifiedHsl.y * satCorrection.saturation);
    
    float3 finalRgb = HSLToRGB(modifiedHsl);
    finalRgb = lerp(originalRgb, finalRgb, factor);
    
    return float4(saturate(finalRgb) * color.a, color.a);
}