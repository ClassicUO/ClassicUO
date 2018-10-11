float4x4 ProjectionMatrix;
float4x4 WorldMatrix;
float2 Viewport;

bool DrawLighting;
float3 lightDirection;
float lightIntensity;

const float HuesPerTexture = 3000;
const float ToGrayScale = 3;



sampler DrawSampler : register(s0);
sampler HueSampler0 : register(s1);
sampler HueSampler1 : register(s2);
sampler HueSampler2 : register(s3);
sampler MiniMapSampler : register(s4);

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
	
	// Half pixel offset for correct texel centering.
	OUT.Position.x -= 0.5 / Viewport.x;
	OUT.Position.y += 0.5 / Viewport.y;

	OUT.TexCoord = IN.TexCoord; 
	OUT.Normal = IN.Normal;
	OUT.Hue = IN.Hue;
	
    return OUT;
}

float4 PixelShader_Hue(PS_INPUT IN) : COLOR0
{	
	// Get the initial pixel and discard it if the alpha == 0
	float4 color = tex2D(DrawSampler, IN.TexCoord);
	if (color.a <= 0)
		discard;

	float alpha = 1 - IN.Hue.z;

	// ethereal mount fix
	if (IN.Hue.x == 1 && IN.Hue.z > 0.0f)
		alpha = 1 - color.r * 1.5;


	// flag for no lighting
	bool drawLighting = true;
	if (IN.Hue.y >= 4)
	{
		IN.Hue.y -= 4;
		drawLighting = false;
	}
	
	// Hue the color if the hue vector y component is greater than 0.
	if (IN.Hue.y > 0)
	{
		float4 hueColor;

		if (IN.Hue.x < HuesPerTexture)
		{
			hueColor = tex2D(HueSampler0, float2(color.r , IN.Hue.x / HuesPerTexture));
		}
		else 
		{
			hueColor = tex2D(HueSampler1, float2(color.r, (IN.Hue.x - HuesPerTexture) / HuesPerTexture));
		}

		hueColor.a = color.a;
		
		

		if (IN.Hue.y >= 2) 
		{
			// partial hue - map any grayscale pixels to the hue. Colored pixels remain colored.
			if ((color.r == color.g) && (color.r == color.b))
				color = hueColor;
		}
		else
		{
			// normal hue - map the hue to the grayscale.
			color = hueColor;
		}
	}

	// Hue.z is the transparency value. alpha = (1 - Hue.z)	
	color *= alpha;
	

	// Darken the color based on the ambient lighting and the normal.
	if (DrawLighting && drawLighting)
	{
		float3 light = normalize(lightDirection);
		float3 normal = normalize(IN.Normal);
		float3 nDotL = min(saturate(dot(light, normal)), 1.0f);

		color.rgb = saturate((color.rgb * nDotL * lightIntensity * 0.2f + color.rgb * lightIntensity * 0.8f));
	}

	return color;
}

float4 PixelShader_MiniMap(PS_INPUT IN) : COLOR0
{
	// Get the initial pixel and discard it if the alpha == 0
	float4 color = tex2D(DrawSampler, IN.TexCoord);
	if ((color.r == 1) && (color.g == 0) && (color.b == 1))
	{
		color = tex2D(MiniMapSampler, IN.Normal);
	}

	if (color.a == 0)
		discard;
	

	return color;
}

float4 PixelShader_Grayscale(PS_INPUT IN) : COLOR0
{
	// Get the initial pixel and discard it if the alpha == 0
	float4 color = tex2D(DrawSampler, IN.TexCoord);
	if (color.a == 0)
		discard;

	float greyscaleAverage = (0.2989 * color.r + 0.5870 * color.g + 0.1140 * color.b);
	color = float4(greyscaleAverage, greyscaleAverage, greyscaleAverage, color.a);

	// Darken the color based on the ambient lighting and the normal.
	if (DrawLighting)
	{
		float3 light = normalize(lightDirection);
			float3 normal = normalize(IN.Normal);
			float3 nDotL = min(saturate(dot(light, normal)), 1.0f);

			color.rgb = saturate((color.rgb * nDotL * lightIntensity * 0.2f + color.rgb * lightIntensity * 0.8f));
	}

	return color;
}

float4 PixelShader_ShadowSet(PS_INPUT IN) : COLOR0
{
	// Get the initial pixel and discard it if the alpha == 0
	float4 color = tex2D(DrawSampler, IN.TexCoord);
	if (color.a == 0)
		discard;
	// if pixel was opaque, return black half-transparent.
	// we use the stencil buffer to only write one shadow pixel to each screen pixel.
	return float4(0, 0, 0, .5);
}


technique HueTechnique
{
	pass p0
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShader_Hue();
	}
}

technique MiniMapTechnique
{
	pass p0
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShader_MiniMap();
	}
}

technique GrayscaleTechnique
{
	pass p0
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShader_Grayscale();
	}
}

technique ShadowSetTechnique
{
	pass p0
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShader_ShadowSet();
	}
}
