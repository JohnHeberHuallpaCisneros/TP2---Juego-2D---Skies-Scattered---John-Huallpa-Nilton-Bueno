
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float Time;
float Strength;
float Frequency;

float4 PaletteDark;
float4 PaletteLight;
float PaletteStrength;

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
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

    uv.x += sin(uv.y * Frequency + Time) * Strength;

    float4 original = tex2D(SpriteTextureSampler, uv);

float brightness = dot(original.rgb, float3(0.299, 0.587, 0.114));

    float3 paletteColor = lerp(
        PaletteDark.rgb,
        PaletteLight.rgb,
        brightness
    );

    float3 finalColor = lerp(
        original.rgb,
        paletteColor,
        PaletteStrength
    );

    return float4(
        finalColor * input.Color.rgb,
        original.a * input.Color.a
    );
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}