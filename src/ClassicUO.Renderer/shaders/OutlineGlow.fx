float4x4 MatrixTransform;
float4x4 WorldMatrix;
float2 Viewport;

texture SpriteTexture;
float4 OutlineColor = float4(0.55, 0.0, 0.0, 1.0);
float OutlineThickness = 2.0;
float2 TextureSize = float2(256.0, 256.0);

sampler2D DrawSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VS_INPUT
{
    float3 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float3 TexCoord : TEXCOORD0;
    float3 Hue      : TEXCOORD1;
};

struct PS_INPUT
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

PS_INPUT VertexShaderFunction(VS_INPUT IN)
{
    PS_INPUT OUT;
    float4 pos = float4(IN.Position, 1.0);
    pos = mul(pos, WorldMatrix);
    pos = mul(pos, MatrixTransform);
    OUT.Position = pos;
    OUT.TexCoord = IN.TexCoord.xy;
    return OUT;
}

float4 PixelShaderFunction(PS_INPUT IN) : COLOR0
{
    float4 color = tex2D(DrawSampler, IN.TexCoord);
    if (color.a > 0.5)
        return color;

    float2 pixelOffset = OutlineThickness / TextureSize;
    float4 up    = tex2D(DrawSampler, IN.TexCoord + float2(0, -pixelOffset.y));
    float4 down  = tex2D(DrawSampler, IN.TexCoord + float2(0, pixelOffset.y));
    float4 left  = tex2D(DrawSampler, IN.TexCoord + float2(-pixelOffset.x, 0));
    float4 right = tex2D(DrawSampler, IN.TexCoord + float2(pixelOffset.x, 0));

    if (up.a > 0.5 || down.a > 0.5 || left.a > 0.5 || right.a > 0.5)
        return OutlineColor;

    return color;
}

technique Outline
{
    pass P0
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader  = compile ps_2_0 PixelShaderFunction();
    }
}
