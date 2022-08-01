#define NONE 0
#define HUED 1
#define PARTIAL_HUED 2
#define HUE_TEXT_NO_BLACK 3
#define HUE_TEXT 4
#define LAND 5
#define LAND_COLOR 6
#define SPECTRAL 7
#define SHADOW 8
#define LIGHTS 9
#define EFFECT_HUED 10
#define GUMP 20

const static float3 LIGHT_DIRECTION = float3(0.0f, 1.0f, 1.0f);



float4x4 MatrixTransform;
float4x4 WorldMatrix;
float2 Viewport;
float Brightlight;
const float HuesPerTexture = 2048;


sampler DrawSampler : register(s0);
sampler HueSampler0 : register(s1);
sampler HueSampler1 : register(s2);
sampler HueSampler2 : register(s3);

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

float3 get_rgb(float gray, float hue)
{
	if (hue < HuesPerTexture)
	{
		float2 texcoord = float2(gray % 32, hue / HuesPerTexture);

		return tex2D(HueSampler0, texcoord).rgb;
	}
	else
	{
		float2 texcoord = float2(gray % 32, (hue - HuesPerTexture) / HuesPerTexture);

		return tex2D(HueSampler1, texcoord).rgb;
	}
}

float get_light(float3 norm)
{
	float3 light = normalize(LIGHT_DIRECTION);
	float3 normal = normalize(norm);
	float base = (max(dot(normal, light), 0.0f) / 2.0f) + 0.5f;

	// At 45 degrees (the angle the flat tiles are lit at) it must come out
	// to (cos(45) / 2) + 0.5 or 0.85355339...
	return base + ((Brightlight * (base - 0.85355339f)) - (base - 0.85355339f));
}

float3 get_colored_light(float shader, float gray)
{
	float2 texcoord = float2(gray, (shader - 0.5) / 63);

	return tex2D(HueSampler2, texcoord).rgb;
}

PS_INPUT VertexShaderFunction(VS_INPUT IN)
{
	PS_INPUT OUT;
	
	OUT.Position = mul(mul(IN.Position, WorldMatrix), MatrixTransform);
	
	OUT.Position.x -= 0.5 / Viewport.x;
	OUT.Position.y += 0.5 / Viewport.y;

	OUT.TexCoord = IN.TexCoord; 
	OUT.Normal = IN.Normal;
	OUT.Hue = IN.Hue;
	
	return OUT;
}

float4 PixelShader_Hue(PS_INPUT IN) : COLOR0
{	
	float4 color = tex2D(DrawSampler, IN.TexCoord.xy);
		
	if (color.a == 0.0f)
		discard;

	int mode = int(IN.Hue.y);
	float alpha = IN.Hue.z;

	if (mode == NONE)
	{
		return color * alpha;
	}

	float hue = IN.Hue.x;

	if (mode >= GUMP)
	{
		mode -= GUMP;

		if (color.r < 0.02f)
		{
			hue = 0;
		}
	}

	if (mode == HUED)
	{
		color.rgb = get_rgb(color.r, hue);
	}
	else if (mode == PARTIAL_HUED)
	{
		if (color.r == color.g && color.r == color.b)
		{
			// Gray pixels are hued
			color.rgb = get_rgb(color.r, hue);
		}
	}
	else if (mode == HUE_TEXT_NO_BLACK)
	{
		if (color.r > 0.04f || color.g > 0.04f || color.b > 0.04f)
		{
			color.rgb = get_rgb(31, hue);
		}
	}
	else if (mode == HUE_TEXT)
	{
		// 31 is max red, so this is just selecting the color of the darkest pixel in the hue
		color.rgb = get_rgb(31, hue);
	}
	else if (mode == LAND)
	{
		color.rgb *= get_light(IN.Normal);
	}
	else if (mode == LAND_COLOR)
	{
		color.rgb = get_rgb(color.r, hue) * get_light(IN.Normal);
	}
	else if (mode == SPECTRAL)
	{
		alpha = 1.0f - (color.r * 1.5f);
		color.r = 0;
		color.g = 0;
		color.b = 0;
	}
	else if (mode == SHADOW)
	{
		alpha = 0.4f;
		color.r = 0;
		color.g = 0;
		color.b = 0;
	}
	else if (mode == LIGHTS)
	{
		color.rgb = get_colored_light(IN.Hue.x - 1, color.r);
	}
	else if (mode == EFFECT_HUED)
	{
		color.rgb = get_rgb(color.g, hue);
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

