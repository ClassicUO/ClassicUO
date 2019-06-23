    //XBR_SCALE "xBR Scale" 4.0 1.0 5.0 1.0
    //XBR_Y_WEIGHT "Y Weight" 48.0 0.0 100.0 1.0
    //XBR_EQ_THRESHOLD "Eq Threshold" 25.0 0.0 50.0 1.0
    //XBR_LV2_COEFFICIENT "Lv2 Coefficient" 2.0 1.0 3.0 0.1

    float XBR_SCALE = 4.0;
    float XBR_Y_WEIGHT = 48.0;
    float XBR_EQ_THRESHOLD = 25.0;
    float XBR_LV2_COEFFICIENT = 2.0;

    float2 textureSize;
    float4x4 MatrixTransform;
    sampler decal : register(s0);
    // END PARAMETERS //

// Uncomment just one of the three params below to choose the corner detection
#define CORNER_A
//#define CORNER_B
//#define CORNER_C
//#define CORNER_D

    const static float4 Ao = float4( 1.0, -1.0, -1.0, 1.0 );
    const static float4 Bo = float4( 1.0,  1.0, -1.0,-1.0 );
    const static float4 Co = float4( 1.5,  0.5, -0.5, 0.5 );
    const static float4 Ax = float4( 1.0, -1.0, -1.0, 1.0 );
    const static float4 Bx = float4( 0.5,  2.0, -0.5,-2.0 );
    const static float4 Cx = float4( 1.0,  1.0, -0.5, 0.0 );
    const static float4 Ay = float4( 1.0, -1.0, -1.0, 1.0 );
    const static float4 By = float4( 2.0,  0.5, -2.0,-0.5 );
    const static float4 Cy = float4( 2.0,  0.0, -1.0, 0.5 );
    const static float4 Ci = float4(0.25, 0.25, 0.25, 0.25);

    const static float3 Y = float3(0.2126, 0.7152, 0.0722);


    float4 df(float4 A, float4 B)
    {
    	return float4(abs(A-B));
    }

    float c_df(float3 c1, float3 c2) 
    {
            float3 df = abs(c1 - c2);
            return df.r + df.g + df.b;
    }

    bool4 eq(float4 A, float4 B)
    {
    	return (df(A, B) < float4(XBR_EQ_THRESHOLD,XBR_EQ_THRESHOLD,XBR_EQ_THRESHOLD,XBR_EQ_THRESHOLD));
    }

    float4 weighted_distance(float4 a, float4 b, float4 c, float4 d, float4 e, float4 f, float4 g, float4 h)
    {
    	return (df(a,b) + df(a,c) + df(d,e) + df(d,f) + 4.0*df(g,h));
    }


    struct out_vertex {
    	float4 position : POSITION;
    	float4 color    : COLOR;
    	float2 texCoord : TEXCOORD0;
    	float4 t1       : TEXCOORD1;
    	float4 t2       : TEXCOORD2;
    	float4 t3       : TEXCOORD3;
    	float4 t4       : TEXCOORD4;
    	float4 t5       : TEXCOORD5;
    	float4 t6       : TEXCOORD6;
    	float4 t7       : TEXCOORD7;
    };

    /*    VERTEX_SHADER    */
    out_vertex main_vertex
    (
    	float4 position	: POSITION,
    	float4 color	: COLOR,
    	float2 texCoord : TEXCOORD0
    )
    {
    	out_vertex OUT;

    	OUT.position = mul(position, MatrixTransform);
    	OUT.color = color;

    	float2 ps = float2(1.0/textureSize.x, 1.0/textureSize.y);
    	float dx = ps.x;
    	float dy = ps.y;

    	//    A1 B1 C1
    	// A0  A  B  C C4
    	// D0  D  E  F F4
    	// G0  G  H  I I4
    	//    G5 H5 I5

    	OUT.texCoord = texCoord;
    	OUT.t1 = texCoord.xxxy + float4( -dx, 0, dx,-2.0*dy); // A1 B1 C1
    	OUT.t2 = texCoord.xxxy + float4( -dx, 0, dx,    -dy); //  A  B  C
    	OUT.t3 = texCoord.xxxy + float4( -dx, 0, dx,      0); //  D  E  F
    	OUT.t4 = texCoord.xxxy + float4( -dx, 0, dx,     dy); //  G  H  I
    	OUT.t5 = texCoord.xxxy + float4( -dx, 0, dx, 2.0*dy); // G5 H5 I5
    	OUT.t6 = texCoord.xyyy + float4(-2.0*dx,-dy, 0,  dy); // A0 D0 G0
    	OUT.t7 = texCoord.xyyy + float4( 2.0*dx,-dy, 0,  dy); // C4 F4 I4

    	return OUT;
    }


    /*    FRAGMENT SHADER    */
    float4 main_fragment(in out_vertex VAR) : COLOR0
    {
    	bool4 edri, edr, edr_left, edr_up, px; // px = pixel, edr = edge detection rule
    	bool4 interp_restriction_lv0, interp_restriction_lv1, interp_restriction_lv2_left, interp_restriction_lv2_up;
    	float4 fx, fx_left, fx_up; // inequations of straight lines.

    	float4 delta         = float4(1.0/XBR_SCALE, 1.0/XBR_SCALE, 1.0/XBR_SCALE, 1.0/XBR_SCALE);
    	float4 deltaL        = float4(0.5/XBR_SCALE, 1.0/XBR_SCALE, 0.5/XBR_SCALE, 1.0/XBR_SCALE);
    	float4 deltaU        = deltaL.yxwz;

    	float2 fp = frac(VAR.texCoord*textureSize);

    	float3 A1 = tex2D(decal, VAR.t1.xw).rgb;
    	float3 B1 = tex2D(decal, VAR.t1.yw).rgb;
    	float3 C1 = tex2D(decal, VAR.t1.zw).rgb;

    	float3 A  = tex2D(decal, VAR.t2.xw).rgb;
    	float3 B  = tex2D(decal, VAR.t2.yw).rgb;
    	float3 C  = tex2D(decal, VAR.t2.zw).rgb;

    	float3 D  = tex2D(decal, VAR.t3.xw).rgb;
    	float3 E  = tex2D(decal, VAR.t3.yw).rgb;
    	float3 F  = tex2D(decal, VAR.t3.zw).rgb;

    	float3 G  = tex2D(decal, VAR.t4.xw).rgb;
    	float3 H  = tex2D(decal, VAR.t4.yw).rgb;
    	float3 I  = tex2D(decal, VAR.t4.zw).rgb;

    	float3 G5 = tex2D(decal, VAR.t5.xw).rgb;
    	float3 H5 = tex2D(decal, VAR.t5.yw).rgb;
    	float3 I5 = tex2D(decal, VAR.t5.zw).rgb;

    	float3 A0 = tex2D(decal, VAR.t6.xy).rgb;
    	float3 D0 = tex2D(decal, VAR.t6.xz).rgb;
    	float3 G0 = tex2D(decal, VAR.t6.xw).rgb;

    	float3 C4 = tex2D(decal, VAR.t7.xy).rgb;
    	float3 F4 = tex2D(decal, VAR.t7.xz).rgb;
    	float3 I4 = tex2D(decal, VAR.t7.xw).rgb;

    	float4 b = mul( float4x3(B, D, H, F), XBR_Y_WEIGHT*Y );
    	float4 c = mul( float4x3(C, A, G, I), XBR_Y_WEIGHT*Y );
    	float4 e = mul( float4x3(E, E, E, E), XBR_Y_WEIGHT*Y );
    	float4 d = b.yzwx;
    	float4 f = b.wxyz;
    	float4 g = c.zwxy;
    	float4 h = b.zwxy;
    	float4 i = c.wxyz;

    	float4 i4 = mul( float4x3(I4, C1, A0, G5), XBR_Y_WEIGHT*Y );
    	float4 i5 = mul( float4x3(I5, C4, A1, G0), XBR_Y_WEIGHT*Y );
    	float4 h5 = mul( float4x3(H5, F4, B1, D0), XBR_Y_WEIGHT*Y );
    	float4 f4 = h5.yzwx;

    	// These inequations define the line below which interpolation occurs.
    	fx      = (Ao*fp.y+Bo*fp.x); 
    	fx_left = (Ax*fp.y+Bx*fp.x);
    	fx_up   = (Ay*fp.y+By*fp.x);

            interp_restriction_lv1 = interp_restriction_lv0 = ((e!=f) && (e!=h));

    #ifdef CORNER_B
    	interp_restriction_lv1      = (interp_restriction_lv0  &&  ( !eq(f,b) && !eq(h,d) || eq(e,i) && !eq(f,i4) && !eq(h,i5) || eq(e,g) || eq(e,c) ) );
    #endif
    #ifdef CORNER_D
    	float4 c1 = i4.yzwx;
    	float4 g0 = i5.wxyz;
    	interp_restriction_lv1      = (interp_restriction_lv0  &&  ( !eq(f,b) && !eq(h,d) || eq(e,i) && !eq(f,i4) && !eq(h,i5) || eq(e,g) || eq(e,c) ) && (f!=f4 && f!=i || h!=h5 && h!=i || h!=g || f!=c || eq(b,c1) && eq(d,g0)));
    #endif
    #ifdef CORNER_C
    	interp_restriction_lv1      = (interp_restriction_lv0  && ( !eq(f,b) && !eq(f,c) || !eq(h,d) && !eq(h,g) || eq(e,i) && (!eq(f,f4) && !eq(f,i4) || !eq(h,h5) && !eq(h,i5)) || eq(e,g) || eq(e,c)) );
    #endif

    	interp_restriction_lv2_left = ((e!=g) && (d!=g));
    	interp_restriction_lv2_up   = ((e!=c) && (b!=c));

    	float4 fx45i = saturate((fx      + delta  -Co - Ci)/(2*delta ));
    	float4 fx45  = saturate((fx      + delta  -Co     )/(2*delta ));
    	float4 fx30  = saturate((fx_left + deltaL -Cx     )/(2*deltaL));
    	float4 fx60  = saturate((fx_up   + deltaU -Cy     )/(2*deltaU));

    	float4 wd1 = weighted_distance( e, c, g, i, h5, f4, h, f);
    	float4 wd2 = weighted_distance( h, d, i5, f, i4, b, e, i);

    	edri     = (wd1 <= wd2) && interp_restriction_lv0;
    	edr      = (wd1 <  wd2) && interp_restriction_lv1;
    #ifdef CORNER_A
    	edr      = edr && (!edri.yzwx || !edri.wxyz);
    	edr_left = ((XBR_LV2_COEFFICIENT*df(f,g)) <= df(h,c)) && interp_restriction_lv2_left && edr && (!edri.yzwx && eq(e,c));
    	edr_up   = (df(f,g) >= (XBR_LV2_COEFFICIENT*df(h,c))) && interp_restriction_lv2_up && edr && (!edri.wxyz && eq(e,g));
    #endif
    #ifndef CORNER_A
    	edr_left = ((XBR_LV2_COEFFICIENT*df(f,g)) <= df(h,c)) && interp_restriction_lv2_left && edr;
    	edr_up   = (df(f,g) >= (XBR_LV2_COEFFICIENT*df(h,c))) && interp_restriction_lv2_up && edr;
    #endif

    	fx45  = edr*fx45;
    	fx30  = edr_left*fx30;
    	fx60  = edr_up*fx60;
    	fx45i = edri*fx45i;

    	px = (df(e,f) <= df(e,h));

    	float4 maximos = max(max(fx30, fx60), max(fx45, fx45i));

    	float3 res1 = E;
    	res1 = lerp(res1, lerp(H, F, px.x), maximos.x);
    	res1 = lerp(res1, lerp(B, D, px.z), maximos.z);
    	
    	float3 res2 = E;
    	res2 = lerp(res2, lerp(F, B, px.y), maximos.y);
    	res2 = lerp(res2, lerp(D, H, px.w), maximos.w);
    	
    	float3 res = lerp(res1, res2, step(c_df(E, res1), c_df(E, res2)));

    	return float4(res, 1.0);
    }

    technique T0
    {
    	pass P0
    	{
    		VertexShader = compile vs_3_0 main_vertex();
    		PixelShader = compile ps_3_0 main_fragment();
    	}
    }