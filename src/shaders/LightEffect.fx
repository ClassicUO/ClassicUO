sampler s0;

texture lightMask;
sampler lightSampler = sampler_state { Texture = lightMask; };

struct PS_INPUT
{
	float4 Position : POSITION0;
	float3 TexCoord : TEXCOORD0;
	float3 Normal	: TEXCOORD1;
	float3 Hue		: TEXCOORD2;
};

float4 PixelShaderLight(PS_INPUT IN) : COLOR0
{
	float4 color = tex2D(s0, IN.TexCoord);
	float4 lightColor = tex2D(lightSampler, IN.TexCoord);
	return color * lightColor;
}

technique Technique1  
{  
	pass Pass1  
	{  
		PixelShader = compile ps_3_0 PixelShaderLight();  
	}  
}  