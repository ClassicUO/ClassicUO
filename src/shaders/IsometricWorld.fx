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
		if (swap)
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
		
	if (color.a == 0.0f)
		discard;

	int mode = int(IN.Hue.y);

	bool swap = false;
	if (mode >= COLOR_SWAP)
	{
		mode -= COLOR_SWAP;
		swap = true;
	}

	float alpha = 1 - IN.Hue.z;

	if (mode == COLOR || (mode == PARTIAL_COLOR && color.r == color.g && color.r == color.b))
	{
		color.rgb = get_rgb(color.r, IN.Hue.x, swap);
	}
	else if (mode > 5)
	{
		if (mode > 9)
		{
			float red = color.r;
			
			if (mode > 10)
			{
				if (mode > 11)
				{
					if (mode > 12)
					{
						if (IN.Hue.x != 0.0f)
						{
							color.rgb *= get_rgb(color.r, IN.Hue.x, swap);
						}
						return color * alpha;
					}

					red = 0.6f;
				}
				else
				{
					red *= 0.5f;
				}
			}
			else
			{
				red *= 1.5f;
			}

			alpha = 1 - red;
			color.rgb = VEC3_ZERO;
		}
		else
		{
			float3 norm = get_light(IN.Normal);

			if (mode > 6)
			{
				color.rgb = get_rgb(color.r, IN.Hue.x, swap) * norm;
			}
			else
			{
				color.rgb *= norm;
			}
		}
	}
	else if (mode == 4 || (mode == 3 && (color.r > 0.08 || color.g > 0.08 || color.b > 0.08)) || (mode == 5 && color.r > 0.08 )  )
	{
		color.rgb = get_rgb(color.r + 90, IN.Hue.x, swap);
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

