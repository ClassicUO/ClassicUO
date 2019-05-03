#define NOCOLOR 0
#define COLOR 1
#define PARTIAL_COLOR 2
#define LAND 6
#define LAND_COLOR 7
#define SPECTRAL 10
#define SHADOW 12
#define LIGHTS 13

float4x4 ProjectionMatrix;
float4x4 WorldMatrix;
float2 Viewport;

const int HuesPerTexture = 3000;
const float HUES_DELTA = 3000.0f;
float3 lightDirection;

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
	
	OUT.Position = mul(mul(IN.Position, WorldMatrix), ProjectionMatrix);
	
	OUT.TexCoord = IN.TexCoord; 
	OUT.Normal = IN.Normal;
	OUT.Hue = IN.Hue;
	
    return OUT;
}

float3 get_rgb(float red, float hue)
{
	if (hue < HuesPerTexture)
		return tex2D(HueSampler0, float2(red, hue / HUES_DELTA)).rgb;
	return tex2D(HueSampler1, float2(red, (hue - HUES_DELTA) / HUES_DELTA)).rgb;
}

float3 get_light(float3 norm)
{
	float3 light = normalize(lightDirection);
	float3 normal = normalize(norm);
	return max((dot(normal, light) + 0.5f), 0.0f);
}

float4 PixelShader_Hue(PS_INPUT IN) : COLOR0
{	
	float4 color = tex2D(DrawSampler, IN.TexCoord);
	
	int mode = int(IN.Hue.y);

	if (mode == LIGHTS)
	{
		if (color.a != 0.0f && IN.Hue.x != 0.0f)
		{
			color.rgb *= get_rgb(color.r, IN.Hue.x);
		}
		return color;
	}

	if (color.a == 0.0f)
		discard;


	float alpha = 1 - IN.Hue.z;

	if (mode == COLOR || (mode == PARTIAL_COLOR && color.r == color.g && color.r == color.b))
	{
		color.rgb = get_rgb(color.r, IN.Hue.x);
	}
	else if (mode == LAND)
	{
		color.rgb *= get_light(IN.Normal);
	}
	else if (mode == LAND_COLOR)
	{
		color.rgb = get_rgb(color.r, IN.Hue.x) * get_light(IN.Normal);
	}
	else if (mode == SPECTRAL)
	{
		alpha = 1 - (color.r * 1.5f);
		color.rgb = float3(0, 0, 0);
	}
	else if (mode == SHADOW)
	{
		alpha = 0.3f;
		color.rgb = float3(0, 0, 0);
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

