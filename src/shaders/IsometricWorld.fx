#define NOCOLOR 0
#define COLOR 1
#define PARTIAL_COLOR 2
#define HUE_TEXT_NO_BLACK 3
#define HUE_TEXT 4
#define LAND 6
#define LAND_COLOR 7
#define SPECTRAL 10
#define SHADOW 12
#define LIGHTS 13
#define COLOR_SWAP 32

float4x4 MatrixTransform;
float4x4 WorldMatrix;
float2 Viewport;
float Brightlight;

const static int HUES_DELTA = 3000;
const static float3 LIGHT_DIRECTION = float3(-1.0f, -1.0f, .5f);
const static float3 VEC3_ZERO = float3(0, 0, 0);

sampler DrawSampler : register(s0);
sampler HueSampler0 : register(s1);
sampler HueSampler1 : register(s2);

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

PS_INPUT VertexShaderFunction(VS_INPUT IN)
{
	PS_INPUT OUT;
	
	OUT.Position = mul(mul(IN.Position, WorldMatrix), MatrixTransform);
	
	OUT.TexCoord = IN.TexCoord; 
	OUT.Normal = IN.Normal;
	OUT.Hue = IN.Hue;
	
    return OUT;
}

float3 get_rgb(float red, float hue, bool swap)
{
	if (hue < HUES_DELTA)
	{
		if(swap)
			hue += HUES_DELTA;
		return tex2D(HueSampler0, float2(red, hue / 6000.0f)).rgb;
	}
	return tex2D(HueSampler1, float2(red, (hue - 3000.0f) / 3000.0f)).rgb;
}

float3 get_light(float3 norm)
{
	float3 light = normalize(LIGHT_DIRECTION);
	float3 normal = normalize(norm);
	return max((dot(normal, light) + 0.5f), 0.0f);
}

float4 PixelShader_Hue(PS_INPUT IN) : COLOR0
{	
	float4 color = tex2D(DrawSampler, IN.TexCoord);
	
	int mode = int(IN.Hue.y);
	bool swap = false;
	if(mode >= COLOR_SWAP)
	{
		mode -= COLOR_SWAP;
		swap = true;
	}

	if (mode == LIGHTS)
	{
		if (color.a != 0.0f && IN.Hue.x != 0.0f)
		{
			color.rgb *= get_rgb(color.r, IN.Hue.x, swap);
		}
		return color;
	}

	if (color.a == 0.0f)
		discard;


	float alpha = 1 - IN.Hue.z;

	if (mode == COLOR || (mode == PARTIAL_COLOR && color.r == color.g && color.r == color.b))
	{
		color.rgb = get_rgb(color.r, IN.Hue.x, swap);
	}
	else if (mode == LAND)
	{
		color.rgb *= get_light(IN.Normal);
	}
	else if (mode == LAND_COLOR)
	{
		color.rgb = get_rgb(color.r, IN.Hue.x, swap) * get_light(IN.Normal);
	}
	else if (mode == HUE_TEXT || (mode == HUE_TEXT_NO_BLACK && color.b > 0.04f))
	{
		float3 rgb = get_rgb(color.r + 30, IN.Hue.x, swap);

		color.rgb = rgb;
	}
	else if (mode == SPECTRAL)
	{
		alpha = 1 - (color.r * 1.5f);
		color.rgb = VEC3_ZERO;
	}
	else if (mode == SHADOW)
	{
		alpha = 0.5f;
		color.rgb = VEC3_ZERO;
	}

	return color * alpha;
}



technique HueTechnique
{
	pass p0
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShader_Hue();
	}
}

