sampler uImage0 : register(s0);
float3 uColor;
float uFill;
float uTime;

float4 MainPS(float2 coords : TEXCOORD0) : COLOR
{
    float surfaceY = 1.0 - uFill;
    float below = coords.y - surfaceY;
    float liquidMask = saturate(below * 120.0 + 0.5);

    float depthT = saturate(below / max(uFill, 0.001));
    float3 liq = lerp(uColor, uColor * 0.45, depthT);

    float flow = sin(coords.x * 8.0 + coords.y * 16.0 - uTime);
    float rung = frac(coords.y * 7.0 - uTime * 0.22);
    rung = 1.0 - abs(rung * 2.0 - 1.0);
    rung *= rung;
    liq += uColor * (flow * 0.10 + rung * 0.14);

    float core = saturate(1.0 - abs(coords.x - 0.5) * 2.2);
    liq += (uColor * 0.5 + 0.5) * core * core * 0.18;

    float surf = saturate(1.0 - abs(below) * 40.0);
    liq += (uColor * 0.3 + 0.7) * surf * 0.4;

    float3 col = lerp(uColor * 0.10 + 0.02, liq, liquidMask);

    float sheen = saturate(1.0 - abs(coords.x - 0.16) * 6.0) * 0.10;
    col += sheen;

    float edge = min(min(coords.x, 1.0 - coords.x), min(coords.y, 1.0 - coords.y));
    col *= saturate(0.7 + edge * 5.0);
    col = lerp(col, uColor * 0.9 + 0.1, saturate(1.0 - edge * 40.0) * 0.5);

    return float4(col, 1.0);
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}
