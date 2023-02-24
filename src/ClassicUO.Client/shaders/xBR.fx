// all identified optimizations have been amalgamated into this file
float2 textureSize;
float4x4 MatrixTransform;

const static float coef = 2.0;
const static float3 yuv_weighted = float3(14.352, 28.176, 5.472);

sampler decal : register(s0);

float4 df(float4 A, float4 B)
{
    return abs(A - B);
}

struct VS_INPUT
{
    float4 Position : POSITION0;
    float3 Normal	: NORMAL0;
    float3 TexCoord : TEXCOORD0;
    float3 Hue		: TEXCOORD1;
};

struct PS_INPUT
{
    float4 Position : POSITION0;
    float3 TexCoord : TEXCOORD0;
    float3 Normal	: TEXCOORD1;
    float3 Hue		: TEXCOORD2;
};

float4 weighted_distance(float4 a, float4 b, float4 c, float4 d,
    float4 e, float4 f, float4 g, float4 h)
{
    return (df(a, b) + df(a, c) + df(d, e) + df(d, f) + 4.0 * df(g, h));
}

PS_INPUT main_vertex(VS_INPUT IN)
{
    PS_INPUT OUT;

    float2 ps = 1.0 / textureSize;

    OUT.Position = mul(IN.Position, MatrixTransform);
    OUT.TexCoord = IN.TexCoord;
    OUT.Normal = float3(ps.x, ps.y, 0);
    OUT.Hue = IN.Hue;

    return OUT;
}

float4 main_fragment(PS_INPUT IN) : COLOR0
{
    bool4 edr, edr_left, edr_up, px; // px = pixel, edr = edge detection rule
    bool4 ir_lv1, ir_lv2_left, ir_lv2_up;
    bool4 nc; // new_color
    bool4 fx, fx_left, fx_up; // inequations of straight lines.

    float2 fp = frac(IN.TexCoord * textureSize);
    float2 dx = float2(IN.Normal.x, 0);
    float2 dy = float2(0, IN.Normal.y);

    float3 A = tex2D(decal, IN.TexCoord - dx - dy).xyz;
    float3 B = tex2D(decal, IN.TexCoord - dy).xyz;
    float3 C = tex2D(decal, IN.TexCoord + dx - dy).xyz;
    float3 D = tex2D(decal, IN.TexCoord - dx).xyz;
    float3 E = tex2D(decal, IN.TexCoord).xyz;
    float3 F = tex2D(decal, IN.TexCoord + dx).xyz;
    float3 G = tex2D(decal, IN.TexCoord - dx + dy).xyz;
    float3 H = tex2D(decal, IN.TexCoord + dy).xyz;
    float3 I = tex2D(decal, IN.TexCoord + dx + dy).xyz;
    float3 A1 = tex2D(decal, IN.TexCoord - dx - 2.0 * dy).xyz;
    float3 C1 = tex2D(decal, IN.TexCoord + dx - 2.0 * dy).xyz;
    float3 A0 = tex2D(decal, IN.TexCoord - 2.0 * dx - dy).xyz;
    float3 G0 = tex2D(decal, IN.TexCoord - 2.0 * dx + dy).xyz;
    float3 C4 = tex2D(decal, IN.TexCoord + 2.0 * dx - dy).xyz;
    float3 I4 = tex2D(decal, IN.TexCoord + 2.0 * dx + dy).xyz;
    float3 G5 = tex2D(decal, IN.TexCoord - dx + 2.0 * dy).xyz;
    float3 I5 = tex2D(decal, IN.TexCoord + dx + 2.0 * dy).xyz;
    float3 B1 = tex2D(decal, IN.TexCoord - 2.0 * dy).xyz;
    float3 D0 = tex2D(decal, IN.TexCoord - 2.0 * dx).xyz;
    float3 H5 = tex2D(decal, IN.TexCoord + 2.0 * dy).xyz;
    float3 F4 = tex2D(decal, IN.TexCoord + 2.0 * dx).xyz;

    float4 b = mul(float4x3(B, D, H, F), yuv_weighted);
    float4 c = mul(float4x3(C, A, G, I), yuv_weighted);
    float4 e = mul(float4x3(E, E, E, E), yuv_weighted);
    float4 d = b.yzwx;
    float4 f = b.wxyz;
    float4 g = c.zwxy;
    float4 h = b.zwxy;
    float4 i = c.wxyz;

    float4 i4 = mul(float4x3(I4, C1, A0, G5), yuv_weighted);
    float4 i5 = mul(float4x3(I5, C4, A1, G0), yuv_weighted);
    float4 h5 = mul(float4x3(H5, F4, B1, D0), yuv_weighted);
    float4 f4 = h5.yzwx;

    float4 Ao = float4(1.0, -1.0, -1.0, 1.0);
    float4 Bo = float4(1.0, 1.0, -1.0, -1.0);
    float4 Co = float4(1.5, 0.5, -0.5, 0.5);
    float4 Ax = float4(1.0, -1.0, -1.0, 1.0);
    float4 Bx = float4(0.5, 2.0, -0.5, -2.0);
    float4 Cx = float4(1.0, 1.0, -0.5, 0.0);
    float4 Ay = float4(1.0, -1.0, -1.0, 1.0);
    float4 By = float4(2.0, 0.5, -2.0, -0.5);
    float4 Cy = float4(2.0, 0.0, -1.0, 0.5);

    // These inequations define the line below which interpolation occurs.
    fx.x = (Ao.x * fp.y + Bo.x * fp.x > Co.x);
    fx.y = (Ao.y * fp.y + Bo.y * fp.x > Co.y);
    fx.z = (Ao.z * fp.y + Bo.z * fp.x > Co.z);
    fx.w = (Ao.w * fp.y + Bo.w * fp.x > Co.w);

    fx_left.x = (Ax.x * fp.y + Bx.x * fp.x > Cx.x);
    fx_left.y = (Ax.y * fp.y + Bx.y * fp.x > Cx.y);
    fx_left.z = (Ax.z * fp.y + Bx.z * fp.x > Cx.z);
    fx_left.w = (Ax.w * fp.y + Bx.w * fp.x > Cx.w);

    fx_up.x = (Ay.x * fp.y + By.x * fp.x > Cy.x);
    fx_up.y = (Ay.y * fp.y + By.y * fp.x > Cy.y);
    fx_up.z = (Ay.z * fp.y + By.z * fp.x > Cy.z);
    fx_up.w = (Ay.w * fp.y + By.w * fp.x > Cy.w);

    ir_lv1 = ((e != f) && (e != h));
    ir_lv2_left = ((e != g) && (d != g));
    ir_lv2_up = ((e != c) && (b != c));

    float4 w1 = weighted_distance(e, c, g, i, h5, f4, h, f);
    float4 w2 = weighted_distance(h, d, i5, f, i4, b, e, i);
    float4 df_fg = df(f, g);
    float4 df_hc = df(h, c);
    float4 t1 = (coef * df_fg);
    float4 t2 = df_hc;
    float4 t3 = df_fg;
    float4 t4 = (coef * df_hc);

    edr = (w1 < w2) && ir_lv1;
    edr_left = (t1 <= t2) && ir_lv2_left;
    edr_up = (t4 <= t3) && ir_lv2_up;

    nc = (edr && (fx || edr_left && fx_left || edr_up && fx_up));

    t1 = df(e, f);
    t2 = df(e, h);
    px = t1 <= t2;

    float3 res = nc.x ? px.x ? F : H :
                 nc.y ? px.y ? B : F :
                 nc.z ? px.z ? D : B :
                 nc.w ? px.w ? H : D : E;

    return float4(res.xyz, 1.0);
}

technique T0
{
    pass P0
    {
        VertexShader = compile vs_3_0 main_vertex();
        PixelShader = compile ps_3_0 main_fragment();
    }
}