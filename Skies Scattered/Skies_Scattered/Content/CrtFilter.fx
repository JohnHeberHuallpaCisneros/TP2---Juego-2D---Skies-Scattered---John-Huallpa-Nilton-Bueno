
#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 OutputSize;
float ScanlineStrength;
float VignetteStrength;
float MaskStrength;
Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float4 color = tex2D(SpriteTextureSampler, uv) * input.Color;
    float scanline = 0.5 + 0.5 * sin(uv.y * OutputSize.y * 3.14159265);
    float2 centered = uv * 2.0 - 1.0;
    float edge = saturate(dot(centered, centered) * 0.5);
    color.rgb *= (1.0 - ScanlineStrength * scanline) * (1.0 - VignetteStrength * edge * edge);
    float column = floor(uv.x * OutputSize.x);
    float stripe = frac(column / 3.0);
    float3 mask = stripe < 0.333 ? float3(1.0, 1.0 - MaskStrength, 1.0 - MaskStrength) :
        stripe < 0.666 ? float3(1.0 - MaskStrength, 1.0, 1.0 - MaskStrength) :
        float3(1.0 - MaskStrength, 1.0 - MaskStrength, 1.0);
    color.rgb *= mask * 1.04;
    return color;
}

technique SpriteDrawing
{
    pass P0 { PixelShader = compile PS_SHADERMODEL MainPS(); }
}
