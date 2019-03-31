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

float HuesPerTexture;
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

float4 PixelShader_Hue(PS_INPUT IN) : COLOR0
{	
	// Get the initial pixel and discard it if the alpha == 0
	float4 color = tex2D(DrawSampler, IN.TexCoord);
	
	int mode = int(IN.Hue.y);

	if (mode == LIGHTS)
	{
		if (color.a != 0.0f && IN.Hue.x != 0.0f)
		{
			if (IN.Hue.x < HuesPerTexture)
				color.rgb *= tex2D(HueSampler0, float2(color.r, IN.Hue.x / 3000.0f)).rgb;
			else
				color.rgb *= tex2D(HueSampler1, float2(color.r, (IN.Hue.x - 3000.0f) / 3000.0f)).rgb;
		}
		return color;
	}

	if (color.a <= 0)
		discard;


	float alpha = 1 - IN.Hue.z;

	if (mode == COLOR)
	{
		if (IN.Hue.x < HuesPerTexture)
			color.rgb = tex2D(HueSampler0, float2(color.r, IN.Hue.x / 3000.0f)).rgb;
		else
			color.rgb = tex2D(HueSampler1, float2(color.r, (IN.Hue.x - 3000.0f) / 3000.0f)).rgb;
	}
	else if (mode == PARTIAL_COLOR)
	{
		if (color.r == color.g && color.r == color.b)
		{
			if (IN.Hue.x < HuesPerTexture)
				color.rgb = tex2D(HueSampler0, float2(color.r, IN.Hue.x / 3000.0f)).rgb;
			else
				color.rgb = tex2D(HueSampler1, float2(color.r, (IN.Hue.x - 3000.0f) / 3000.0f)).rgb;
		}
	}
	else if (mode == LAND)
	{
		float3 light = normalize(lightDirection);
		float3 normal = normalize(IN.Normal);
		float3 nDotL = max((dot(normal, light) + 0.5f), 0.0f);

		color.rgb = (color.rgb * nDotL);
	}
	else if (mode == LAND_COLOR)
	{
		if (IN.Hue.x < HuesPerTexture)
			color.rgb = tex2D(HueSampler0, float2(color.r, IN.Hue.x / 3000.0f)).rgb;
		else
			color.rgb = tex2D(HueSampler1, float2(color.r, (IN.Hue.x - 3000.0f) / 3000.0f)).rgb;

		float3 light = normalize(lightDirection);
		float3 normal = normalize(IN.Normal);
		float3 nDotL = max((dot(normal, light) + 0.5f), 0.0f);

		color.rgb = (color.rgb * nDotL);
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

	color *= alpha;

	return color;
}



technique HueTechnique
{
	pass p0
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShader_Hue();
	}
}

