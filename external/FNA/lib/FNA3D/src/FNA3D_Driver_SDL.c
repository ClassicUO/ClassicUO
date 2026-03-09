/* FNA3D - 3D Graphics Library for FNA
 *
 * Copyright (c) 2020-2024 Ethan Lee
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */

#if FNA3D_DRIVER_SDL

#include <SDL3/SDL.h>

#include "FNA3D_Driver.h"
#include "FNA3D_PipelineCache.h"

#define MAX_FRAMES_IN_FLIGHT 3
#define MAX_UPLOAD_CYCLE_COUNT 4
#define TRANSFER_BUFFER_SIZE 16777216 /* 16 MiB */

static inline SDL_GPUSampleCount XNAToSDL_SampleCount(int32_t sampleCount)
{
	if (sampleCount <= 1)
	{
		return SDL_GPU_SAMPLECOUNT_1;
	}
	else if (sampleCount == 2)
	{
		return SDL_GPU_SAMPLECOUNT_2;
	}
	else if (sampleCount <= 4)
	{
		return SDL_GPU_SAMPLECOUNT_4;
	}
	else if (sampleCount <= 8)
	{
		return SDL_GPU_SAMPLECOUNT_8;
	}
	else
	{
		FNA3D_LogWarn("Unexpected sample count: %d", sampleCount);
		return SDL_GPU_SAMPLECOUNT_1;
	}
}

static inline float XNAToSDL_DepthBiasScale(SDL_GPUTextureFormat format)
{
	switch (format)
	{
		case SDL_GPU_TEXTUREFORMAT_D16_UNORM:
			return (float) ((1 << 16) - 1);

		case SDL_GPU_TEXTUREFORMAT_D32_FLOAT:
		case SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT:
			return (float) ((1 << 23) - 1);

		case SDL_GPU_TEXTUREFORMAT_D24_UNORM:
		case SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT:
			return (float) ((1 << 24) - 1);

		default:
			return 0.0f;
	}
}

static inline uint32_t SDLGPU_INTERNAL_RoundToAlignment(
	uint32_t value,
	uint32_t alignment
) {
	return alignment * ((value + alignment - 1) / alignment);
}

static SDL_GPUTextureFormat XNAToSDL_SurfaceFormat[] =
{
	SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,		/* SurfaceFormat.Color */
	SDL_GPU_TEXTUREFORMAT_B5G6R5_UNORM,		/* SurfaceFormat.Bgr565 */
	SDL_GPU_TEXTUREFORMAT_B5G5R5A1_UNORM,		/* SurfaceFormat.Bgra5551 */
	SDL_GPU_TEXTUREFORMAT_B4G4R4A4_UNORM,		/* SurfaceFormat.Bgra4444 */
	SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM,		/* SurfaceFormat.Dxt1 */
	SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM,		/* SurfaceFormat.Dxt3 */
	SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM,		/* SurfaceFormat.Dxt5 */
	SDL_GPU_TEXTUREFORMAT_R8G8_SNORM,		/* SurfaceFormat.NormalizedByte2 */
	SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM,		/* SurfaceFormat.NormalizedByte4 */
	SDL_GPU_TEXTUREFORMAT_R10G10B10A2_UNORM,	/* SurfaceFormat.Rgba1010102 */
	SDL_GPU_TEXTUREFORMAT_R16G16_UNORM,		/* SurfaceFormat.Rg32 */
	SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UNORM,	/* SurfaceFormat.Rgba64 */
	SDL_GPU_TEXTUREFORMAT_A8_UNORM,			/* SurfaceFormat.Alpha8 */
	SDL_GPU_TEXTUREFORMAT_R32_FLOAT,		/* SurfaceFormat.Single */
	SDL_GPU_TEXTUREFORMAT_R32G32_FLOAT,		/* SurfaceFormat.Vector2 */
	SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT,	/* SurfaceFormat.Vector4 */
	SDL_GPU_TEXTUREFORMAT_R16_FLOAT,		/* SurfaceFormat.HalfSingle */
	SDL_GPU_TEXTUREFORMAT_R16G16_FLOAT,		/* SurfaceFormat.HalfVector2 */
	SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT,	/* SurfaceFormat.HalfVector4 */
	SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT,	/* SurfaceFormat.HdrBlendable */
	SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM,		/* SurfaceFormat.ColorBgraEXT */
	SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB,	/* SurfaceFormat.ColorSrgbEXT */
	SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM_SRGB,	/* SurfaceFormat.Dxt5SrgbEXT */
	SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM,		/* SurfaceFormat.Bc7EXT */
	SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM_SRGB,	/* SurfaceFormat.Bc7SrgbEXT */
	SDL_GPU_TEXTUREFORMAT_R8_UNORM,			/* SurfaceFormat.NormalizedByteEXT */
	SDL_GPU_TEXTUREFORMAT_R16_UNORM,		/* SurfaceFormat.NormalizedUShortEXT */
};

static SDL_GPUPrimitiveType XNAToSDL_PrimitiveType[] =
{
	SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,	/* FNA3D_PRIMITIVETYPE_TRIANGLELIST */
	SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP,	/* FNA3D_PRIMITIVETYPE_TRIANGLESTRIP */
	SDL_GPU_PRIMITIVETYPE_LINELIST,		/* FNA3D_PRIMITIVETYPE_LINELIST */
	SDL_GPU_PRIMITIVETYPE_LINESTRIP,	/* FNA3D_PRIMITIVETYPE_LINESTRIP */
	SDL_GPU_PRIMITIVETYPE_POINTLIST		/* FNA3D_PRIMITIVETYPE_POINTLIST_EXT */
};

static SDL_GPUIndexElementSize XNAToSDL_IndexElementSize[] =
{
	SDL_GPU_INDEXELEMENTSIZE_16BIT,	/* FNA3D_INDEXELEMENTSIZE_16BIT */
	SDL_GPU_INDEXELEMENTSIZE_32BIT	/* FNA3D_INDEXELEMENTSIZE_32BIT */
};

static SDL_GPUBlendFactor XNAToSDL_BlendFactor[] =
{
	SDL_GPU_BLENDFACTOR_ONE,			/* FNA3D_BLEND_ONE */
	SDL_GPU_BLENDFACTOR_ZERO,			/* FNA3D_BLEND_ZERO */
	SDL_GPU_BLENDFACTOR_SRC_COLOR,			/* FNA3D_BLEND_SOURCECOLOR */
	SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,	/* FNA3D_BLEND_INVERSESOURCECOLOR */
	SDL_GPU_BLENDFACTOR_SRC_ALPHA,			/* FNA3D_BLEND_SOURCEALPHA */
	SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,	/* FNA3D_BLEND_INVERSESOURCEALPHA */
	SDL_GPU_BLENDFACTOR_DST_COLOR,			/* FNA3D_BLEND_DESTINATIONCOLOR */
	SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,	/* FNA3D_BLEND_INVERSEDESTINATIONCOLOR */
	SDL_GPU_BLENDFACTOR_DST_ALPHA,			/* FNA3D_BLEND_DESTINATIONALPHA */
	SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,	/* FNA3D_BLEND_INVERSEDESTINATIONALPHA */
	SDL_GPU_BLENDFACTOR_CONSTANT_COLOR,		/* FNA3D_BLEND_BLENDFACTOR */
	SDL_GPU_BLENDFACTOR_ONE_MINUS_CONSTANT_COLOR,	/* FNA3D_BLEND_INVERSEBLENDFACTOR */
	SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE		/* FNA3D_BLEND_SOURCEALPHASATURATION */
};

static SDL_GPUBlendOp XNAToSDL_BlendOp[] =
{
	SDL_GPU_BLENDOP_ADD,			/* FNA3D_BLENDFUNCTION_ADD */
	SDL_GPU_BLENDOP_SUBTRACT,		/* FNA3D_BLENDFUNCTION_SUBTRACT */
	SDL_GPU_BLENDOP_REVERSE_SUBTRACT,	/* FNA3D_BLENDFUNCTION_REVERSESUBTRACT */
	SDL_GPU_BLENDOP_MAX,			/* FNA3D_BLENDFUNCTION_MAX */
	SDL_GPU_BLENDOP_MIN			/* FNA3D_BLENDFUNCTION_MIN */
};

static SDL_GPUFilter XNAToSDL_MagFilter[] =
{
	SDL_GPU_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_LINEAR */
	SDL_GPU_FILTER_NEAREST, /* FNA3D_TEXTUREFILTER_POINT */
	SDL_GPU_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_ANISOTROPIC */
	SDL_GPU_FILTER_LINEAR,	/* FNA3D_TEXTUREFILTER_LINEAR_MIPPOINT */
	SDL_GPU_FILTER_NEAREST, /* FNA3D_TEXTUREFILTER_POINT_MIPLINEAR */
	SDL_GPU_FILTER_NEAREST,	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPLINEAR */
	SDL_GPU_FILTER_NEAREST, /* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPPOINT */
	SDL_GPU_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPLINEAR */
	SDL_GPU_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPPOINT */
};

static SDL_GPUFilter XNAToSDL_MinFilter[] =
{
	SDL_GPU_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_LINEAR */
	SDL_GPU_FILTER_NEAREST, /* FNA3D_TEXTUREFILTER_POINT */
	SDL_GPU_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_ANISOTROPIC */
	SDL_GPU_FILTER_LINEAR,	/* FNA3D_TEXTUREFILTER_LINEAR_MIPPOINT */
	SDL_GPU_FILTER_NEAREST, /* FNA3D_TEXTUREFILTER_POINT_MIPLINEAR */
	SDL_GPU_FILTER_LINEAR,	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPLINEAR */
	SDL_GPU_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPPOINT */
	SDL_GPU_FILTER_NEAREST, /* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPLINEAR */
	SDL_GPU_FILTER_NEAREST, /* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPPOINT */
};

static SDL_GPUSamplerMipmapMode XNAToSDL_MipFilter[] =
{
	SDL_GPU_SAMPLERMIPMAPMODE_LINEAR, 	/* FNA3D_TEXTUREFILTER_LINEAR */
	SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,	/* FNA3D_TEXTUREFILTER_POINT */
	SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,	/* FNA3D_TEXTUREFILTER_ANISOTROPIC */
	SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,	/* FNA3D_TEXTUREFILTER_LINEAR_MIPPOINT */
	SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,	/* FNA3D_TEXTUREFILTER_POINT_MIPLINEAR */
	SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPLINEAR */
	SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPPOINT */
	SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPLINEAR */
	SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPPOINT */
};

static SDL_GPUSamplerAddressMode XNAToSDL_SamplerAddressMode[] =
{
	SDL_GPU_SAMPLERADDRESSMODE_REPEAT,		/* FNA3D_TEXTUREADDRESSMODE_WRAP */
	SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,	/* FNA3D_TEXTUREADDRESSMODE_CLAMP */
	SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT	/* FNA3D_TEXTUREADDRESSMODE_MIRROR */
};

static SDL_GPUVertexElementFormat XNAToSDL_VertexAttribType[] =
{
	SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,		/* FNA3D_VERTEXELEMENTFORMAT_SINGLE */
	SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,		/* FNA3D_VERTEXELEMENTFORMAT_VECTOR2 */
	SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,		/* FNA3D_VERTEXELEMENTFORMAT_VECTOR3 */
	SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,		/* FNA3D_VERTEXELEMENTFORMAT_VECTOR4 */
	SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,	/* FNA3D_VERTEXELEMENTFORMAT_COLOR */
	SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4,		/* FNA3D_VERTEXELEMENTFORMAT_BYTE4 */
	SDL_GPU_VERTEXELEMENTFORMAT_SHORT2,		/* FNA3D_VERTEXELEMENTFORMAT_SHORT2 */
	SDL_GPU_VERTEXELEMENTFORMAT_SHORT4,		/* FNA3D_VERTEXELEMENTFORMAT_SHORT4 */
	SDL_GPU_VERTEXELEMENTFORMAT_SHORT2_NORM,	/* FNA3D_VERTEXELEMENTFORMAT_NORMALIZEDSHORT2 */
	SDL_GPU_VERTEXELEMENTFORMAT_SHORT4_NORM,	/* FNA3D_VERTEXELEMENTFORMAT_NORMALIZEDSHORT4 */
	SDL_GPU_VERTEXELEMENTFORMAT_HALF2,		/* FNA3D_VERTEXELEMENTFORMAT_HALFVECTOR2 */
	SDL_GPU_VERTEXELEMENTFORMAT_HALF4		/* FNA3D_VERTEXELEMENTFORMAT_HALFVECTOR4 */
};

static SDL_GPUFillMode XNAToSDL_FillMode[] =
{
	SDL_GPU_FILLMODE_FILL,	/* FNA3D_FILLMODE_SOLID */
	SDL_GPU_FILLMODE_LINE	/* FNA3D_FILLMODE_WIREFRAME */
};

static SDL_GPUCullMode XNAToSDL_CullMode[] =
{
	SDL_GPU_CULLMODE_NONE,	/* FNA3D_CULLMODE_NONE */
	SDL_GPU_CULLMODE_FRONT,	/* FNA3D_CULLMODE_CULLCLOCKWISEFACE */
	SDL_GPU_CULLMODE_BACK	/* FNA3D_CULLMODE_CULLCOUNTERCLOCKWISEFACE */
};

static SDL_GPUCompareOp XNAToSDL_CompareOp[] =
{
	SDL_GPU_COMPAREOP_ALWAYS,		/* FNA3D_COMPAREFUNCTION_ALWAYS */
	SDL_GPU_COMPAREOP_NEVER,		/* FNA3D_COMPAREFUNCTION_NEVER */
	SDL_GPU_COMPAREOP_LESS,			/* FNA3D_COMPAREFUNCTION_LESS */
	SDL_GPU_COMPAREOP_LESS_OR_EQUAL,	/* FNA3D_COMPAREFUNCTION_LESSEQUAL */
	SDL_GPU_COMPAREOP_EQUAL,		/* FNA3D_COMPAREFUNCTION_EQUAL */
	SDL_GPU_COMPAREOP_GREATER_OR_EQUAL,	/* FNA3D_COMPAREFUNCTION_GREATEREQUAL */
	SDL_GPU_COMPAREOP_GREATER,		/* FNA3D_COMPAREFUNCTION_GREATER */
	SDL_GPU_COMPAREOP_NOT_EQUAL		/* FNA3D_COMPAREFUNCTION_NOTEQUAL */
};

static SDL_GPUStencilOp XNAToSDL_StencilOp[] =
{
	SDL_GPU_STENCILOP_KEEP,			/* FNA3D_STENCILOPERATION_KEEP */
	SDL_GPU_STENCILOP_ZERO,			/* FNA3D_STENCILOPERATION_ZERO */
	SDL_GPU_STENCILOP_REPLACE,		/* FNA3D_STENCILOPERATION_REPLACE */
	SDL_GPU_STENCILOP_INCREMENT_AND_WRAP,	/* FNA3D_STENCILOPERATION_INCREMENT */
	SDL_GPU_STENCILOP_DECREMENT_AND_WRAP,	/* FNA3D_STENCILOPERATION_DECREMENT */
	SDL_GPU_STENCILOP_INCREMENT_AND_CLAMP,	/* FNA3D_STENCILOPERATION_INCREMENTSATURATION */
	SDL_GPU_STENCILOP_DECREMENT_AND_CLAMP,	/* FNA3D_STENCILOPERATION_DECREMENTSATURATION */
	SDL_GPU_STENCILOP_INVERT		/* FNA3D_STENCILOPERATION_INVERT */
};

static inline bool XNAToSDL_PresentMode(
	SDL_GPUDevice *device,
	SDL_Window *window,
	FNA3D_PresentInterval interval,
	SDL_GPUPresentMode *presentMode
) {
	if (
		interval == FNA3D_PRESENTINTERVAL_DEFAULT ||
		interval == FNA3D_PRESENTINTERVAL_ONE )
	{
		if (SDL_GetHintBoolean("FNA3D_VULKAN_FORCE_MAILBOX_VSYNC", 0))
		{
			*presentMode = SDL_GPU_PRESENTMODE_MAILBOX;
			if (!SDL_WindowSupportsGPUPresentMode(device, window, *presentMode))
			{
				*presentMode = SDL_GPU_PRESENTMODE_VSYNC;
			}
		}
		else
		{
			*presentMode = SDL_GPU_PRESENTMODE_VSYNC;
		}
		return true;
	}
	else if (interval == FNA3D_PRESENTINTERVAL_IMMEDIATE)
	{
		*presentMode = SDL_GPU_PRESENTMODE_IMMEDIATE;
		if (!SDL_WindowSupportsGPUPresentMode(device, window, *presentMode))
		{
			*presentMode = SDL_GPU_PRESENTMODE_VSYNC;
		}
		return true;
	}
	else if (interval == FNA3D_PRESENTINTERVAL_TWO)
	{
		FNA3D_LogError("FNA3D_PRESENTINTERVAL_TWO not supported by SDL GPU backend!");
		return false;
	}
	else
	{
		FNA3D_LogError("Unrecognized presentation interval!");
		return false;
	}
}

static inline SDL_Rect ComputeRectIntersection(int x1, int x2, int y1, int y2, int w1, int w2, int h1, int h2)
{
	SDL_Rect newRect;
	newRect.x = SDL_max(x1, x2);
	newRect.y = SDL_max(y1, y2);
	newRect.w = SDL_min(x1 + w1, x2 + w2) - newRect.x;
	newRect.h = SDL_min(y1 + h1, y2 + h2) - newRect.y;
	if (newRect.w < 0 || newRect.h < 0) {
		FNA3D_LogWarn("Scissor rect and viewport appear not to overlap");
		newRect.x = x1;
		newRect.y = y1;
		newRect.w = 0;
		newRect.h = 0;
	}
	return newRect;
}

/* Indirection to cleanly handle Renderbuffers */
typedef struct SDLGPU_TextureHandle /* Cast from FNA3D_Texture* */
{
	SDL_GPUTexture *texture;
	SDL_GPUTextureCreateInfo createInfo;
	uint8_t boundAsRenderTarget;
} SDLGPU_TextureHandle;

typedef struct SDLGPU_Renderbuffer /* Cast from FNA3D_Renderbuffer* */
{
	SDLGPU_TextureHandle *textureHandle;
	SDL_GPUTextureFormat format;
	SDL_GPUSampleCount sampleCount;
} SDLGPU_Renderbuffer;

typedef struct SDLGPU_Effect /* Cast from FNA3D_Effect* */
{
	MOJOSHADER_effect *effect;
} SDLGPU_Effect;

typedef struct SDLGPU_BufferHandle /* Cast from FNA3D_Buffer* */
{
	SDL_GPUBuffer *buffer;
	uint32_t size;
} SDLGPU_BufferHandle;

typedef struct SamplerStateHashMap
{
	PackedState key;
	SDL_GPUSampler *value;
} SamplerStateHashMap;

typedef struct SamplerStateHashArray
{
	SamplerStateHashMap *elements;
	int32_t count;
	int32_t capacity;
} SamplerStateHashArray;

static inline SDL_GPUSampler* SamplerStateHashArray_Fetch(
	SamplerStateHashArray *arr,
	PackedState key
) {
	int32_t i;

	for (i = 0; i < arr->count; i += 1)
	{
		if (	key.a == arr->elements[i].key.a &&
			key.b == arr->elements[i].key.b		)
		{
			return arr->elements[i].value;
		}
	}

	return NULL;
}

static inline void SamplerStateHashArray_Insert(
	SamplerStateHashArray *arr,
	PackedState key,
	SDL_GPUSampler *value
) {
	SamplerStateHashMap map;
	map.key.a = key.a;
	map.key.b = key.b;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 4, SamplerStateHashMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

/* FIXME: This could be packed better */
typedef struct GraphicsPipelineHash
{
	PackedState blendState;
	PackedState rasterizerState;
	PackedState depthStencilState;
	uint32_t vertexBufferBindingsIndex;
	FNA3D_PrimitiveType primitiveType;
	SDL_GPUSampleCount sampleCount;
	uint32_t sampleMask;
	SDL_GPUShader *vertShader;
	SDL_GPUShader *fragShader;
	SDL_GPUTextureFormat colorFormats[MAX_RENDERTARGET_BINDINGS];
	uint32_t colorFormatCount;
	bool hasDepthStencilAttachment;
	SDL_GPUTextureFormat depthStencilFormat;
} GraphicsPipelineHash;

typedef struct GraphicsPipelineHashMap
{
	GraphicsPipelineHash key;
	SDL_GPUGraphicsPipeline *value;
} GraphicsPipelineHashMap;

typedef struct GraphicsPipelineHashArray
{
	GraphicsPipelineHashMap *elements;
	int32_t count;
	int32_t capacity;
} GraphicsPipelineHashArray;

#define NUM_PIPELINE_HASH_BUCKETS 1031

typedef struct GraphicsPipelineHashTable
{
	GraphicsPipelineHashArray buckets[NUM_PIPELINE_HASH_BUCKETS];
} GraphicsPipelineHashTable;

static inline uint64_t GraphicsPipelineHashTable_GetHashCode(GraphicsPipelineHash hash)
{
	/* The algorithm for this hashing function
	 * is taken from Josh Bloch's "Effective Java".
	 * (https://stackoverflow.com/a/113600/12492383)
	 */
	const uint64_t HASH_FACTOR = 97;
	uint32_t i;
	uint64_t result = 1;
	result = result * HASH_FACTOR + hash.blendState.a;
	result = result * HASH_FACTOR + hash.blendState.b;
	result = result * HASH_FACTOR + hash.rasterizerState.a;
	result = result * HASH_FACTOR + hash.rasterizerState.b;
	result = result * HASH_FACTOR + hash.depthStencilState.a;
	result = result * HASH_FACTOR + hash.depthStencilState.b;
	result = result * HASH_FACTOR + hash.vertexBufferBindingsIndex;
	result = result * HASH_FACTOR + hash.primitiveType;
	result = result * HASH_FACTOR + hash.sampleCount;
	result = result * HASH_FACTOR + hash.sampleMask;
	result = result * HASH_FACTOR + (uint64_t) (size_t) hash.vertShader;
	result = result * HASH_FACTOR + (uint64_t) (size_t) hash.fragShader;
	result = result * HASH_FACTOR + hash.colorFormatCount;
	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		result = result * HASH_FACTOR + hash.colorFormats[i];
	}
	result = result * HASH_FACTOR + hash.hasDepthStencilAttachment;
	result = result * HASH_FACTOR + hash.depthStencilFormat;
	return result;
}

static inline SDL_GPUGraphicsPipeline *GraphicsPipelineHashTable_Fetch(
	GraphicsPipelineHashTable *table,
	GraphicsPipelineHash key
) {
	int32_t i;
	uint64_t hashcode = GraphicsPipelineHashTable_GetHashCode(key);
	GraphicsPipelineHashArray *arr = &table->buckets[hashcode % NUM_PIPELINE_HASH_BUCKETS];

	for (i = 0; i < arr->count; i += 1)
	{
		const GraphicsPipelineHash *e = &arr->elements[i].key;
		if (	key.blendState.a == e->blendState.a &&
			key.blendState.b == e->blendState.b &&
			key.rasterizerState.a == e->rasterizerState.a &&
			key.rasterizerState.b == e->rasterizerState.b &&
			key.depthStencilState.a == e->depthStencilState.a &&
			key.depthStencilState.b == e->depthStencilState.b &&
			key.vertexBufferBindingsIndex == e->vertexBufferBindingsIndex &&
			key.primitiveType == e->primitiveType &&
			key.sampleMask == e->sampleMask &&
			key.vertShader == e->vertShader &&
			key.fragShader == e->fragShader &&
			key.colorFormatCount == e->colorFormatCount &&
			key.colorFormats[0] == e->colorFormats[0] &&
			key.colorFormats[1] == e->colorFormats[1] &&
			key.colorFormats[2] == e->colorFormats[2] &&
			key.colorFormats[3] == e->colorFormats[3] &&
			key.hasDepthStencilAttachment == e->hasDepthStencilAttachment &&
			key.depthStencilFormat == e->depthStencilFormat )
		{
			return arr->elements[i].value;
		}
	}

	return NULL;
}

static inline void GraphicsPipelineHashTable_Insert(
	GraphicsPipelineHashTable *table,
	GraphicsPipelineHash key,
	SDL_GPUGraphicsPipeline *value
) {
	uint64_t hashcode = GraphicsPipelineHashTable_GetHashCode(key);
	GraphicsPipelineHashArray *arr = &table->buckets[hashcode % NUM_PIPELINE_HASH_BUCKETS];
	GraphicsPipelineHashMap map;
	map.key = key;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 2, GraphicsPipelineHashMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

typedef struct SDLGPU_Renderer
{
	SDL_GPUDevice *device;
	SDL_GPUCommandBuffer *renderCommandBuffer;
	SDL_GPUCommandBuffer *uploadCommandBuffer;

	SDL_GPURenderPass *renderPass;
	uint8_t needNewRenderPass;

	SDL_GPUCopyPass *copyPass;
	SDL_Mutex *copyPassMutex;

	uint8_t shouldClearColorOnBeginPass;
	uint8_t shouldClearDepthOnBeginPass;
	uint8_t shouldClearStencilOnBeginPass;

	SDL_FColor clearColorValue;
	float clearDepthValue;
	Uint8 clearStencilValue;

	/* Defer render pass settings */
	SDLGPU_TextureHandle *nextRenderPassColorAttachments[MAX_RENDERTARGET_BINDINGS];
	SDLGPU_TextureHandle *nextRenderPassColorResolves[MAX_RENDERTARGET_BINDINGS];
	SDL_GPUCubeMapFace nextRenderPassColorAttachmentCubeFace[MAX_RENDERTARGET_BINDINGS];
	uint32_t nextRenderPassColorAttachmentCount;
	SDL_GPUSampleCount nextRenderPassMultisampleCount;

	SDLGPU_TextureHandle *nextRenderPassDepthStencilAttachment; /* may be NULL */

	uint8_t renderTargetInUse;

	uint8_t needNewGraphicsPipeline;
	int32_t currentVertexBufferBindingsIndex;

	SDL_GPUGraphicsPipeline *currentGraphicsPipeline;
	MOJOSHADER_sdlShaderData *currentVertexShader;
	MOJOSHADER_sdlShaderData *currentFragmentShader;

	PackedVertexBufferBindingsArray vertexBufferBindingsCache;

	FNA3D_Viewport viewport;

	/* Vertex buffer bind settings */
	uint32_t numVertexBindings;
	FNA3D_VertexBufferBinding vertexBindings[MAX_BOUND_VERTEX_BUFFERS];
	FNA3D_VertexElement vertexElements[MAX_BOUND_VERTEX_BUFFERS][MAX_VERTEX_ATTRIBUTES];
	SDL_GPUBufferBinding vertexBufferBindings[MAX_BOUND_VERTEX_BUFFERS];
	uint8_t needVertexBufferBind;

	/* Index buffer state shadowing */
	SDL_GPUBufferBinding indexBufferBinding;

	/* Sampler bind settings */
	SDL_GPUTextureSamplerBinding vertexTextureSamplerBindings[MAX_VERTEXTEXTURE_SAMPLERS];
	uint8_t needVertexSamplerBind;

	SDL_GPUTextureSamplerBinding fragmentTextureSamplerBindings[MAX_TEXTURE_SAMPLERS];
	uint8_t needFragmentSamplerBind;

	/* Pipeline state */
	FNA3D_BlendState fnaBlendState;
	FNA3D_RasterizerState fnaRasterizerState;
	FNA3D_DepthStencilState fnaDepthStencilState;
	FNA3D_PrimitiveType fnaPrimitiveType;
	float blendConstants[4];
	SDL_FColor currentBlendConstants;
	uint32_t stencilReference;
	uint32_t currentStencilReference;
	uint32_t multisampleMask;
	SDL_Rect scissorRect;

	/* Pipeline cache */
	GraphicsPipelineHash nextPipelineHash;
	SDL_GPUVertexBufferDescription vertexDescriptions[16];
	SDL_GPUVertexAttribute vertexAttributes[16 * MAX_VERTEX_ATTRIBUTES];
	uint32_t numVertexAttributes;

	/* Presentation structure */

	SDL_Window **windows;
	uint32_t numWindows;
	SDLGPU_TextureHandle *fauxBackbufferColorTexture;
	SDLGPU_TextureHandle *fauxBackbufferColorRenderbuffer;
	SDLGPU_TextureHandle *fauxBackbufferDepthStencil; /* may be NULL */

	/* Transfer structure */

	SDL_GPUTransferBuffer *textureDownloadBuffer;
	uint32_t textureDownloadBufferSize;

	SDL_GPUTransferBuffer *bufferDownloadBuffer;
	uint32_t bufferDownloadBufferSize;

	SDL_GPUTransferBuffer *textureUploadBuffer;
	uint32_t textureUploadBufferOffset;
	uint32_t textureUploadCycleCount;

	SDL_GPUTransferBuffer *bufferUploadBuffer;
	uint32_t bufferUploadBufferOffset;
	uint32_t bufferUploadCycleCount;

	/* RT tracking to reduce unnecessary cycling */

	SDLGPU_TextureHandle **boundRenderTargets;
	uint32_t boundRenderTargetCount;
	uint32_t boundRenderTargetCapacity;

	/* Hashing */

	GraphicsPipelineHashTable graphicsPipelineHashTable;
	SamplerStateHashArray samplerStateArray;

	/* MOJOSHADER */

	MOJOSHADER_sdlContext *mojoshaderContext;
	MOJOSHADER_effect *currentEffect;
	const MOJOSHADER_effectTechnique *currentTechnique;
	uint32_t currentPass;

	/* Dummy Samplers */

	SDL_GPUTexture *dummyTexture2D;
	SDL_GPUTexture *dummyTexture3D;
	SDL_GPUTexture *dummyTextureCube;
	SDL_GPUSampler *dummySampler;

	/* Backbuffer parameter cache */

	FNA3D_SurfaceFormat readbackBackbufferSurfaceFormat;
	FNA3D_DepthFormat readbackBackbufferDepthFormat;
	int32_t readbackBackbufferMultiSampleCount;

	/* Capabilities */

	uint8_t supportsBaseVertex;
	uint8_t supportsDXT1;
	uint8_t supportsBC2;
	uint8_t supportsBC3;
	uint8_t supportsBC7;
	uint8_t supportsSRGB;
	uint8_t supportsD24;
	uint8_t supportsD24S8;

} SDLGPU_Renderer;

/* Format Conversion */

static inline SDL_GPUTextureFormat XNAToSDL_DepthFormat(
	SDLGPU_Renderer* renderer,
	FNA3D_DepthFormat format
) {
	switch (format)
	{
	case FNA3D_DEPTHFORMAT_D16:
		return SDL_GPU_TEXTUREFORMAT_D16_UNORM;
	case FNA3D_DEPTHFORMAT_D24:
		return (renderer->supportsD24) ?
			SDL_GPU_TEXTUREFORMAT_D24_UNORM :
			SDL_GPU_TEXTUREFORMAT_D32_FLOAT;
	case FNA3D_DEPTHFORMAT_D24S8:
		return (renderer->supportsD24S8) ?
			SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT :
			SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT;
	default:
		FNA3D_LogError("Unrecognized depth format!");
		return 0;
	}
}

/* Submission / Presentation */

static void SDLGPU_INTERNAL_BeginCopyPass(
	SDLGPU_Renderer *renderer
) {
	if (renderer->copyPass == NULL)
	{
		renderer->copyPass = SDL_BeginGPUCopyPass(
			renderer->uploadCommandBuffer
		);
	}
}

static void SDLGPU_INTERNAL_EndCopyPass(
	SDLGPU_Renderer *renderer
) {
	if (renderer->copyPass != NULL)
	{
		SDL_EndGPUCopyPass(
			renderer->copyPass
		);

		renderer->copyPass = NULL;
	}
}

static void SDLGPU_INTERNAL_BindRenderTarget(
	SDLGPU_Renderer *renderer,
	SDLGPU_TextureHandle *textureHandle
) {
	uint32_t i;

	for (i = 0; i < renderer->boundRenderTargetCount; i += 1)
	{
		if (renderer->boundRenderTargets[i] == textureHandle)
		{
			return;
		}
	}

	if (renderer->boundRenderTargetCount >= renderer->boundRenderTargetCapacity)
	{
		renderer->boundRenderTargetCapacity *= 2;
		renderer->boundRenderTargets = SDL_realloc(
			renderer->boundRenderTargets,
			renderer->boundRenderTargetCapacity * sizeof(SDLGPU_TextureHandle*)
		);
	}

	renderer->boundRenderTargets[renderer->boundRenderTargetCount] =
		textureHandle;
	renderer->boundRenderTargetCount += 1;

	textureHandle->boundAsRenderTarget = 1;
}

static void SDLGPU_INTERNAL_BeginRenderPass(
	SDLGPU_Renderer* renderer
);

static void SDLGPU_INTERNAL_EndRenderPass(
	SDLGPU_Renderer *renderer
) {
	/* If we got to EndRenderPass without actually making a new render pass,
	 * we're looking at a clear-only pass, so just forcibly start it so we
	 * can have the clear execute instead of silently discarding it.
	 * -flibit
	 */
	if (renderer->needNewRenderPass)
	{
		SDLGPU_INTERNAL_BeginRenderPass(renderer);
	}

	if (renderer->renderPass != NULL)
	{
		SDL_EndGPURenderPass(
			renderer->renderPass
		);

		renderer->renderPass = NULL;
	}

	renderer->needNewRenderPass = 1;
	renderer->currentGraphicsPipeline = NULL;
	renderer->needNewGraphicsPipeline = 1;
	SDL_zero(renderer->currentBlendConstants);
	renderer->currentStencilReference = 0;
}

static void SDLGPU_INTERNAL_UpdateViewport(SDLGPU_Renderer *renderer)
{
	SDL_GPUViewport gpuViewport;
	SDL_Rect scissorRect;

	gpuViewport.x = (float) renderer->viewport.x;
	gpuViewport.y = (float) renderer->viewport.y;
	gpuViewport.w = (float) renderer->viewport.w;
	gpuViewport.h = (float) renderer->viewport.h;
	gpuViewport.min_depth = renderer->viewport.minDepth;
	gpuViewport.max_depth = renderer->viewport.maxDepth;

	SDL_SetGPUViewport(
		renderer->renderPass,
		&gpuViewport
	);

	// Update the scissor to match/complement new viewport dimensions
	if (renderer->fnaRasterizerState.scissorTestEnable)
	{
		scissorRect = ComputeRectIntersection(gpuViewport.x, renderer->scissorRect.x,
			gpuViewport.y, renderer->scissorRect.y,
			gpuViewport.w, renderer->scissorRect.w,
			gpuViewport.h, renderer->scissorRect.h);
	}
	else
	{
		scissorRect.x = gpuViewport.x;
		scissorRect.y = gpuViewport.y;
		scissorRect.w = gpuViewport.w;
		scissorRect.h = gpuViewport.h;
	}
	SDL_SetGPUScissor(
		renderer->renderPass,
		&scissorRect
	);
}

static void SDLGPU_INTERNAL_BeginRenderPass(
	SDLGPU_Renderer *renderer
) {
	SDL_GPUColorTargetInfo colorAttachmentInfos[MAX_RENDERTARGET_BINDINGS];
	SDL_GPUDepthStencilTargetInfo depthStencilAttachmentInfo;
	uint32_t i;

	if (!renderer->needNewRenderPass)
	{
		return;
	}

	/* We write to this variable twice to make absolutely sure we don't let End
	 * cause unnecessary Begins later
	 */
	renderer->needNewRenderPass = 0;
	SDLGPU_INTERNAL_EndRenderPass(renderer);
	renderer->needNewRenderPass = 0;

	/* Set up the next render pass */
	for (i = 0; i < renderer->nextRenderPassColorAttachmentCount; i += 1)
	{
		colorAttachmentInfos[i].texture = renderer->nextRenderPassColorAttachments[i]->texture;
		colorAttachmentInfos[i].layer_or_depth_plane = renderer->nextRenderPassColorAttachmentCubeFace[i];
		colorAttachmentInfos[i].mip_level = 0;

		colorAttachmentInfos[i].load_op =
			renderer->shouldClearColorOnBeginPass ?
				SDL_GPU_LOADOP_CLEAR :
				SDL_GPU_LOADOP_LOAD;

		/* We always have to store just in case changing render state breaks the render pass. */
		/* FIXME: perhaps there is a way around this? */
		if (renderer->nextRenderPassColorResolves[i] != NULL)
		{
			colorAttachmentInfos[i].store_op = SDL_GPU_STOREOP_RESOLVE_AND_STORE;
		}
		else
		{
			colorAttachmentInfos[i].store_op = SDL_GPU_STOREOP_STORE;
		}

		colorAttachmentInfos[i].cycle =
			renderer->nextRenderPassColorAttachments[i]->boundAsRenderTarget || colorAttachmentInfos[i].load_op == SDL_GPU_LOADOP_LOAD ?
				false :
				true; /* cycle if we can, it's fast! */

		if (renderer->nextRenderPassColorResolves[i] != NULL)
		{
			colorAttachmentInfos[i].resolve_texture = renderer->nextRenderPassColorResolves[i]->texture;
		}
		else
		{
			colorAttachmentInfos[i].resolve_texture = NULL;
		}
		colorAttachmentInfos[i].resolve_mip_level = 0;
		colorAttachmentInfos[i].resolve_layer = 0;
		colorAttachmentInfos[i].cycle_resolve_texture = colorAttachmentInfos[i].cycle;

		if (renderer->shouldClearColorOnBeginPass)
		{
			colorAttachmentInfos[i].clear_color = renderer->clearColorValue;
		}
		else
		{
			colorAttachmentInfos[i].clear_color.r = 0;
			colorAttachmentInfos[i].clear_color.g = 0;
			colorAttachmentInfos[i].clear_color.b = 0;
			colorAttachmentInfos[i].clear_color.a = 0;
		}

		SDLGPU_INTERNAL_BindRenderTarget(renderer, renderer->nextRenderPassColorAttachments[i]);
	}

	if (renderer->nextRenderPassDepthStencilAttachment != NULL)
	{
		SDL_zero(depthStencilAttachmentInfo);
		depthStencilAttachmentInfo.texture = renderer->nextRenderPassDepthStencilAttachment->texture;

		if (renderer->shouldClearDepthOnBeginPass)
		{
			depthStencilAttachmentInfo.load_op = SDL_GPU_LOADOP_CLEAR;
		}
		else
		{
			/* FIXME: is there a way to safely get rid of this load op? */
			depthStencilAttachmentInfo.load_op = SDL_GPU_LOADOP_LOAD;
		}

		if (renderer->shouldClearStencilOnBeginPass)
		{
			depthStencilAttachmentInfo.stencil_load_op = SDL_GPU_LOADOP_CLEAR;
		}
		else
		{
			/* FIXME: is there a way to safely get rid of this load op? */
			depthStencilAttachmentInfo.stencil_load_op = SDL_GPU_LOADOP_LOAD;
		}

		/* We always have to store just in case changing render state breaks the render pass. */
		/* FIXME: perhaps there is a way around this? */
		depthStencilAttachmentInfo.store_op = SDL_GPU_STOREOP_STORE;
		depthStencilAttachmentInfo.stencil_store_op = SDL_GPU_STOREOP_STORE;

		depthStencilAttachmentInfo.cycle =
			renderer->nextRenderPassDepthStencilAttachment->boundAsRenderTarget || depthStencilAttachmentInfo.load_op == SDL_GPU_LOADOP_LOAD || depthStencilAttachmentInfo.stencil_load_op == SDL_GPU_LOADOP_LOAD ?
				false :
				true; /* Cycle if we can! */

		/* FIXME: Do we want to default to 0 when the render pass load op isn't clear? */
		depthStencilAttachmentInfo.clear_depth = renderer->clearDepthValue;
		depthStencilAttachmentInfo.clear_stencil = renderer->clearStencilValue;

		SDLGPU_INTERNAL_BindRenderTarget(renderer, renderer->nextRenderPassDepthStencilAttachment);
	}

	renderer->renderPass = SDL_BeginGPURenderPass(
		renderer->renderCommandBuffer,
		colorAttachmentInfos,
		renderer->nextRenderPassColorAttachmentCount,
		renderer->nextRenderPassDepthStencilAttachment != NULL ? &depthStencilAttachmentInfo : NULL
	);

	SDLGPU_INTERNAL_UpdateViewport(renderer);

	renderer->shouldClearColorOnBeginPass = 0;
	renderer->shouldClearDepthOnBeginPass = 0;
	renderer->shouldClearStencilOnBeginPass = 0;

	renderer->needNewGraphicsPipeline = 1;
}

static void SDLGPU_INTERNAL_ResetUploadCommandBufferState(
	SDLGPU_Renderer *renderer
) {
	renderer->uploadCommandBuffer = SDL_AcquireGPUCommandBuffer(renderer->device);
	SDLGPU_INTERNAL_BeginCopyPass(renderer);

	/* Reset state */
	renderer->textureUploadCycleCount = 0;
	renderer->bufferUploadCycleCount = 0;
	renderer->textureUploadBufferOffset = 0;
	renderer->bufferUploadBufferOffset = 0;
}

static void SDLGPU_INTERNAL_ResetRenderCommandBufferState(
	SDLGPU_Renderer *renderer
) {
	renderer->renderCommandBuffer = SDL_AcquireGPUCommandBuffer(renderer->device);

	/* Reset state */
	renderer->needNewRenderPass = 1;
	renderer->needNewGraphicsPipeline = 1;
	renderer->needVertexBufferBind = 1;
	renderer->needVertexSamplerBind = 1;
	renderer->needFragmentSamplerBind = 1;
}

static void SDLGPU_ResetCommandBufferState(
	SDLGPU_Renderer *renderer
) {
	SDLGPU_INTERNAL_ResetUploadCommandBufferState(renderer);
	SDLGPU_INTERNAL_ResetRenderCommandBufferState(renderer);
}

static void SDLGPU_INTERNAL_FlushUploadCommandsAndAcquireFence(
	SDLGPU_Renderer *renderer,
	SDL_GPUFence **uploadFence
) {
	SDL_LockMutex(renderer->copyPassMutex);

	SDLGPU_INTERNAL_EndCopyPass(renderer);

	*uploadFence = SDL_SubmitGPUCommandBufferAndAcquireFence(
		renderer->uploadCommandBuffer
	);

	if (!*uploadFence) {
		const char *error = SDL_GetError();
		FNA3D_LogError("SDL_SubmitGPUCommandBufferAndAcquireFence failed: %s", error);
	}

	SDLGPU_INTERNAL_ResetUploadCommandBufferState(renderer);

	SDL_UnlockMutex(renderer->copyPassMutex);
}

static void SDLGPU_INTERNAL_FlushCommandsAndAcquireFence(
	SDLGPU_Renderer *renderer,
	SDL_GPUFence **uploadFence,
	SDL_GPUFence **renderFence
) {
	SDLGPU_INTERNAL_FlushUploadCommandsAndAcquireFence(renderer, uploadFence);

	SDLGPU_INTERNAL_EndRenderPass(renderer);

	*renderFence = SDL_SubmitGPUCommandBufferAndAcquireFence(
		renderer->renderCommandBuffer
	);

	SDLGPU_INTERNAL_ResetRenderCommandBufferState(renderer);
}

static void SDLGPU_INTERNAL_FlushUploadCommands(
	SDLGPU_Renderer *renderer
) {
	SDL_LockMutex(renderer->copyPassMutex);

	SDLGPU_INTERNAL_EndCopyPass(renderer);
	SDL_SubmitGPUCommandBuffer(renderer->uploadCommandBuffer);
	SDLGPU_INTERNAL_ResetUploadCommandBufferState(renderer);

	SDL_UnlockMutex(renderer->copyPassMutex);
}

static void SDLGPU_INTERNAL_FlushCommands(
	SDLGPU_Renderer *renderer
) {
	SDLGPU_INTERNAL_FlushUploadCommands(renderer);

	SDLGPU_INTERNAL_EndRenderPass(renderer);
	SDL_SubmitGPUCommandBuffer(renderer->renderCommandBuffer);
	SDLGPU_INTERNAL_ResetRenderCommandBufferState(renderer);
}

static void SDLGPU_INTERNAL_FlushCommandsAndStall(
	SDLGPU_Renderer *renderer
) {
	SDL_GPUFence* fences[2];

	SDLGPU_INTERNAL_FlushCommandsAndAcquireFence(
		renderer,
		&fences[0],
		&fences[1]
	);

	SDL_WaitForGPUFences(
		renderer->device,
		1,
		fences,
		2
	);

	SDL_ReleaseGPUFence(
		renderer->device,
		fences[0]
	);

	SDL_ReleaseGPUFence(
		renderer->device,
		fences[1]
	);
}

static void SDLGPU_INTERNAL_FlushUploadCommandsAndStall(
	SDLGPU_Renderer *renderer
)
{
	SDL_GPUFence* fences[1];

	SDLGPU_INTERNAL_FlushUploadCommandsAndAcquireFence(
		renderer,
		&fences[0]
	);

	SDL_WaitForGPUFences(
		renderer->device,
		1,
		fences,
		1
	);

	SDL_ReleaseGPUFence(
		renderer->device,
		fences[0]
	);
}

static bool SDLGPU_INTERNAL_ClaimWindow(
	SDLGPU_Renderer *renderer,
	SDL_Window *window
) {
	size_t i;
	for (i = 0; i < renderer->numWindows; i += 1)
	{
		if (window == renderer->windows[i])
		{
			return true;
		}
	}

	if (!SDL_ClaimWindowForGPUDevice(renderer->device, window))
	{
		FNA3D_LogError("Could not claim window for FNA3D renderer: %s", SDL_GetError());
		return false;
	}

	renderer->numWindows += 1;
	renderer->windows = (SDL_Window**) SDL_realloc(
		renderer->windows,
		sizeof(SDL_Window*) * renderer->numWindows
	);
	renderer->windows[renderer->numWindows - 1] = window;
	return true;
}

static void SDLGPU_SwapBuffers(
	FNA3D_Renderer *driverData,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDL_GPUTexture *swapchainTexture;
	SDL_GPUBlitInfo blitInfo;
	uint32_t width, height;
	uint32_t i;

	SDL_LockMutex(renderer->copyPassMutex);
	SDLGPU_INTERNAL_EndCopyPass(renderer);
	SDLGPU_INTERNAL_EndRenderPass(renderer);

	if (!SDLGPU_INTERNAL_ClaimWindow(
		renderer,
		(SDL_Window*) overrideWindowHandle
	)) {
		SDL_UnlockMutex(renderer->copyPassMutex);
		return;
	}

	if (SDL_WaitAndAcquireGPUSwapchainTexture(
		renderer->renderCommandBuffer,
		overrideWindowHandle,
		&swapchainTexture,
		&width,
		&height
	) && swapchainTexture != NULL) {
		blitInfo.source.texture = renderer->fauxBackbufferColorTexture->texture;
		blitInfo.source.mip_level = 0;
		blitInfo.source.layer_or_depth_plane = 0;
		if (sourceRectangle != NULL)
		{
			blitInfo.source.x = sourceRectangle->x;
			blitInfo.source.y = sourceRectangle->y;
			blitInfo.source.w = sourceRectangle->w;
			blitInfo.source.h = sourceRectangle->h;
		}
		else
		{
			blitInfo.source.x = 0;
			blitInfo.source.y = 0;
			blitInfo.source.w = renderer->fauxBackbufferColorTexture->createInfo.width;
			blitInfo.source.h = renderer->fauxBackbufferColorTexture->createInfo.height;
		}

		blitInfo.destination.texture = swapchainTexture;
		blitInfo.destination.mip_level = 0;
		blitInfo.destination.layer_or_depth_plane = 0;
		if (destinationRectangle != NULL)
		{
			blitInfo.destination.x = destinationRectangle->x;
			blitInfo.destination.y = destinationRectangle->y;
			blitInfo.destination.w = destinationRectangle->w;
			blitInfo.destination.h = destinationRectangle->h;
		}
		else
		{
			blitInfo.destination.x = 0;
			blitInfo.destination.y = 0;
			blitInfo.destination.w = width;
			blitInfo.destination.h = height;
		}

		blitInfo.load_op = SDL_GPU_LOADOP_DONT_CARE;
		blitInfo.clear_color.r = 0;
		blitInfo.clear_color.g = 0;
		blitInfo.clear_color.b = 0;
		blitInfo.clear_color.a = 0;
		blitInfo.flip_mode = SDL_FLIP_NONE;
		blitInfo.filter = SDL_GetHintBoolean("FNA3D_BACKBUFFER_SCALE_NEAREST", false) ?
			SDL_GPU_FILTER_NEAREST :
			SDL_GPU_FILTER_LINEAR;
		blitInfo.cycle = false;

		SDL_BlitGPUTexture(
			renderer->renderCommandBuffer,
			&blitInfo
		);
	}

	SDLGPU_INTERNAL_FlushCommands(renderer);

	/* Reset bound RT state */
	for (i = 0; i < renderer->boundRenderTargetCount; i += 1)
	{
		/* This can be null if the texture was released after a render
		 * pass was completed - this typically happens when resizing the
		 * swapchain.
		 */
		if (renderer->boundRenderTargets[i] != NULL)
		{
			renderer->boundRenderTargets[i]->boundAsRenderTarget = 0;
		}
	}
	renderer->boundRenderTargetCount = 0;

	SDL_UnlockMutex(renderer->copyPassMutex);
}

/* GDK Support */

#if SDL_PLATFORM_GDK
static bool SDLCALL SDLGPU_INTERNAL_GDKEventFilter(void* userdata, SDL_Event* event)
{
	/* These events are called from a system thread, so we need to try and
	 * intercept presentation and discard any SwapBuffers calls being made before
	 * the app could reasonably stop rendering.
	 */
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) userdata;
	if (event->type == SDL_EVENT_DID_ENTER_BACKGROUND)
	{
		SDL_LockMutex(renderer->copyPassMutex);
		SDL_GDKSuspendGPU(renderer->device);
		SDL_UnlockMutex(renderer->copyPassMutex);
	}
	else if (event->type == SDL_EVENT_WILL_ENTER_FOREGROUND)
	{
		SDL_LockMutex(renderer->copyPassMutex);
		SDL_GDKResumeGPU(renderer->device);
		SDL_UnlockMutex(renderer->copyPassMutex);
	}
	return true;
}
#endif

/* Drawing */

static void SDLGPU_INTERNAL_PrepareRenderPassClear(
	SDLGPU_Renderer *renderer,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil,
	uint8_t clearColor,
	uint8_t clearDepth,
	uint8_t clearStencil
) {
	if (!clearColor && !clearDepth && !clearStencil)
	{
		return;
	}

	renderer->shouldClearColorOnBeginPass |= clearColor;
	renderer->shouldClearDepthOnBeginPass |= clearDepth;
	renderer->shouldClearStencilOnBeginPass |= clearStencil;

	if (clearColor)
	{
		renderer->clearColorValue.r = color->x;
		renderer->clearColorValue.g = color->y;
		renderer->clearColorValue.b = color->z;
		renderer->clearColorValue.a = color->w;
	}

	if (clearDepth)
	{
		if (depth < 0.0f)
		{
			depth = 0.0f;
		}
		else if (depth > 1.0f)
		{
			depth = 1.0f;
		}

		renderer->clearDepthValue = depth;
	}

	if (clearStencil)
	{
		renderer->clearStencilValue = stencil;
	}

	renderer->needNewRenderPass = 1;
}

static void SDLGPU_Clear(
	FNA3D_Renderer *driverData,
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	uint8_t clearColor = (options & FNA3D_CLEAROPTIONS_TARGET) == FNA3D_CLEAROPTIONS_TARGET;
	uint8_t clearDepth = (options & FNA3D_CLEAROPTIONS_DEPTHBUFFER) == FNA3D_CLEAROPTIONS_DEPTHBUFFER;
	uint8_t clearStencil = (options & FNA3D_CLEAROPTIONS_STENCIL) == FNA3D_CLEAROPTIONS_STENCIL;

	SDLGPU_INTERNAL_PrepareRenderPassClear(
		renderer,
		color,
		depth,
		stencil,
		clearColor,
		clearDepth,
		clearStencil
	);
}

static void SDLGPU_SetRenderTargets(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents /* ignored */
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	int32_t i;

	if (
		renderer->shouldClearColorOnBeginPass ||
		renderer->shouldClearDepthOnBeginPass ||
		renderer->shouldClearStencilOnBeginPass
	) {
		SDLGPU_INTERNAL_BeginRenderPass(renderer);
	}

	if (numRenderTargets <= 0)
	{
		if (renderer->fauxBackbufferColorRenderbuffer != NULL)
		{
			renderer->nextRenderPassColorAttachments[0] = renderer->fauxBackbufferColorRenderbuffer;
			renderer->nextRenderPassColorResolves[0] = renderer->fauxBackbufferColorTexture;
			renderer->nextRenderPassMultisampleCount = renderer->fauxBackbufferColorRenderbuffer->createInfo.sample_count;
		}
		else
		{
			renderer->nextRenderPassColorAttachments[0] = renderer->fauxBackbufferColorTexture;
			renderer->nextRenderPassColorResolves[0] = NULL;
			renderer->nextRenderPassMultisampleCount = SDL_GPU_SAMPLECOUNT_1;
		}
		for (i = 1; i < MAX_RENDERTARGET_BINDINGS; i += 1)
		{
			renderer->nextRenderPassColorAttachments[i] = NULL;
			renderer->nextRenderPassColorResolves[i] = NULL;
		}
		renderer->nextRenderPassColorAttachmentCubeFace[0] = 0;
		renderer->nextRenderPassColorAttachmentCount = 1;

		renderer->nextRenderPassDepthStencilAttachment = renderer->fauxBackbufferDepthStencil;

		renderer->renderTargetInUse = 0;
	}
	else
	{
		for (i = 0; i < numRenderTargets; i += 1)
		{
			renderer->nextRenderPassColorAttachmentCubeFace[i] = (
				renderTargets[i].type == FNA3D_RENDERTARGET_TYPE_CUBE ?
					(SDL_GPUCubeMapFace) renderTargets[i].cube.face :
					0
			);

			if (renderTargets[i].colorBuffer != NULL)
			{
				renderer->nextRenderPassColorAttachments[i] = ((SDLGPU_Renderbuffer*) renderTargets[i].colorBuffer)->textureHandle;
				renderer->nextRenderPassMultisampleCount = ((SDLGPU_Renderbuffer*) renderTargets[i].colorBuffer)->sampleCount;
				renderer->nextRenderPassColorResolves[i] = (SDLGPU_TextureHandle*) renderTargets[i].texture;
			}
			else
			{
				renderer->nextRenderPassColorAttachments[i] = (SDLGPU_TextureHandle*) renderTargets[i].texture;
				renderer->nextRenderPassMultisampleCount = SDL_GPU_SAMPLECOUNT_1;
				renderer->nextRenderPassColorResolves[i] = NULL;
			}
		}
		for (; i < MAX_RENDERTARGET_BINDINGS; i += 1)
		{
			renderer->nextRenderPassColorAttachments[i] = NULL;
			renderer->nextRenderPassColorResolves[i] = NULL;
		}

		renderer->nextRenderPassColorAttachmentCount = numRenderTargets;

		if (depthStencilBuffer != NULL)
		{
			renderer->nextRenderPassDepthStencilAttachment = ((SDLGPU_Renderbuffer*) depthStencilBuffer)->textureHandle;
		}
		else
		{
			renderer->nextRenderPassDepthStencilAttachment = NULL;
		}

		renderer->renderTargetInUse = 1;
	}

	renderer->nextPipelineHash.sampleCount = renderer->nextRenderPassMultisampleCount;
	renderer->nextPipelineHash.colorFormatCount = renderer->nextRenderPassColorAttachmentCount;
	for (i = 0; i < (int32_t) renderer->nextRenderPassColorAttachmentCount; i += 1)
	{
		renderer->nextPipelineHash.colorFormats[i] = renderer->nextRenderPassColorAttachments[i]->createInfo.format;
	}
	for (; i < 4; i += 1)
	{
		renderer->nextPipelineHash.colorFormats[i] = SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
	}
	renderer->nextPipelineHash.hasDepthStencilAttachment = renderer->nextRenderPassDepthStencilAttachment != NULL;
	if (renderer->nextPipelineHash.hasDepthStencilAttachment)
	{
		renderer->nextPipelineHash.depthStencilFormat = renderer->nextRenderPassDepthStencilAttachment->createInfo.format;
	}
	else
	{
		renderer->nextPipelineHash.depthStencilFormat = SDL_GPU_TEXTUREFORMAT_D16_UNORM;
	}

	renderer->needNewRenderPass = 1;
}

static void SDLGPU_ResolveTarget(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *target
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_TextureHandle *texture = (SDLGPU_TextureHandle*) target->texture;

	if (texture->createInfo.num_levels <= 1)
	{
		/* Nothing to do, SDL_GPU resolves MSAA for us */
		return;
	}

	/* Rendering needs to finish to get the target data to make mips from */
	SDLGPU_INTERNAL_EndRenderPass(renderer);
	SDL_GenerateMipmapsForGPUTexture(renderer->renderCommandBuffer, texture->texture);
}

static void SDLGPU_INTERNAL_GenerateVertexInputInfo(
	SDLGPU_Renderer *renderer,
	SDL_GPUVertexBufferDescription *bindings,
	SDL_GPUVertexAttribute *attributes,
	uint32_t *attributeCount
) {
	MOJOSHADER_sdlShaderData *vertexShader, *blah;
	uint8_t attrUse[MOJOSHADER_USAGE_TOTAL][16];
	uint32_t attributeDescriptionCounter = 0;
	int32_t i, j, k;
	FNA3D_VertexDeclaration vertexDeclaration;
	FNA3D_VertexElement element;
	FNA3D_VertexElementUsage usage;
	MOJOSHADER_vertexAttribute mojoshaderVertexAttributes[16];
	int32_t index, attribLoc;

	MOJOSHADER_sdlGetBoundShaderData(renderer->mojoshaderContext, &vertexShader, &blah);

	SDL_memset(attrUse, '\0', sizeof(attrUse));
	for (i = 0; i < (int32_t) renderer->numVertexBindings; i += 1)
	{
		vertexDeclaration =
			renderer->vertexBindings[i].vertexDeclaration;

		for (j = 0; j < vertexDeclaration.elementCount; j += 1)
		{
			element = vertexDeclaration.elements[j];
			usage = element.vertexElementUsage;
			index = element.usageIndex;

			if (attrUse[usage][index])
			{
				index = -1;

				for (k = 0; k < MAX_VERTEX_ATTRIBUTES; k += 1)
				{
					if (!attrUse[usage][k])
					{
						index = k;
						break;
					}
				}

				if (index < 0)
				{
					FNA3D_LogError("Vertex usage collision!");
				}
			}

			attrUse[usage][index] = 1;

			attribLoc = MOJOSHADER_sdlGetVertexAttribLocation(
				vertexShader,
				VertexAttribUsage(usage),
				index
			);

			if (attribLoc == -1)
			{
				/* Stream not in use! */
				continue;
			}

			attributes[attributeDescriptionCounter].location = attribLoc;
			attributes[attributeDescriptionCounter].format = XNAToSDL_VertexAttribType[
				element.vertexElementFormat
			];
			attributes[attributeDescriptionCounter].offset = element.offset;
			attributes[attributeDescriptionCounter].buffer_slot = i;

			mojoshaderVertexAttributes[attributeDescriptionCounter].usage = VertexAttribUsage(element.vertexElementUsage);
			mojoshaderVertexAttributes[attributeDescriptionCounter].vertexElementFormat = (MOJOSHADER_vertexElementFormat) element.vertexElementFormat; /* FNA3D/MojoShader use the same enum values */
			mojoshaderVertexAttributes[attributeDescriptionCounter].usageIndex = index;

			attributeDescriptionCounter += 1;
		}

		bindings[i].slot = i;
		bindings[i].pitch = vertexDeclaration.vertexStride;

		if (renderer->vertexBindings[i].instanceFrequency > 0)
		{
			if (renderer->vertexBindings[i].instanceFrequency > 1)
			{
				FNA3D_LogError("Vertex instanceFrequency must be either 0 or 1!");
			}
			bindings[i].input_rate =
				SDL_GPU_VERTEXINPUTRATE_INSTANCE;
			bindings[i].instance_step_rate = 0;
		}
		else
		{
			bindings[i].input_rate =
				SDL_GPU_VERTEXINPUTRATE_VERTEX;
			bindings[i].instance_step_rate = 0; /* should be ignored */
		}
	}

	*attributeCount = attributeDescriptionCounter;

	MOJOSHADER_sdlLinkProgram(
		renderer->mojoshaderContext,
		mojoshaderVertexAttributes,
		attributeDescriptionCounter
	);
	MOJOSHADER_sdlGetShaders(
		renderer->mojoshaderContext,
		&renderer->nextPipelineHash.vertShader,
		&renderer->nextPipelineHash.fragShader
	);
}

static SDL_GPUGraphicsPipeline* SDLGPU_INTERNAL_FetchGraphicsPipeline(
	SDLGPU_Renderer *renderer
) {
	SDL_GPUGraphicsPipeline *pipeline;
	SDL_GPUGraphicsPipelineCreateInfo createInfo;
	SDL_GPUColorTargetDescription colorAttachmentDescriptions[MAX_RENDERTARGET_BINDINGS];

	/* We have to do this to link the vertex attribute modified shader program */
	SDLGPU_INTERNAL_GenerateVertexInputInfo(
		renderer,
		renderer->vertexDescriptions,
		renderer->vertexAttributes,
		&renderer->numVertexAttributes
	);

	pipeline = GraphicsPipelineHashTable_Fetch(
		&renderer->graphicsPipelineHashTable,
		renderer->nextPipelineHash
	);

	if (pipeline != NULL)
	{
		return pipeline;
	}

	createInfo.primitive_type = XNAToSDL_PrimitiveType[renderer->fnaPrimitiveType];

	/* Active Shader */
	createInfo.vertex_shader = renderer->nextPipelineHash.vertShader;
	createInfo.fragment_shader = renderer->nextPipelineHash.fragShader;

	/* Vertex Input State */

	createInfo.vertex_input_state.vertex_buffer_descriptions = renderer->vertexDescriptions;
	createInfo.vertex_input_state.num_vertex_buffers = renderer->numVertexBindings;
	createInfo.vertex_input_state.vertex_attributes = renderer->vertexAttributes;
	createInfo.vertex_input_state.num_vertex_attributes = renderer->numVertexAttributes;

	/* Rasterizer */

	createInfo.rasterizer_state.cull_mode = XNAToSDL_CullMode[renderer->fnaRasterizerState.cullMode];
	createInfo.rasterizer_state.depth_bias_clamp = 0.0f;
	createInfo.rasterizer_state.depth_bias_constant_factor = renderer->fnaRasterizerState.depthBias;
	createInfo.rasterizer_state.enable_depth_bias = 1;
	createInfo.rasterizer_state.enable_depth_clip = 1;
	createInfo.rasterizer_state.depth_bias_slope_factor = renderer->fnaRasterizerState.slopeScaleDepthBias;
	createInfo.rasterizer_state.fill_mode = XNAToSDL_FillMode[renderer->fnaRasterizerState.fillMode];
	createInfo.rasterizer_state.front_face = SDL_GPU_FRONTFACE_CLOCKWISE;

	/* Multisample */

	SDL_zero(createInfo.multisample_state);
	createInfo.multisample_state.sample_count = renderer->nextRenderPassMultisampleCount;
	if (renderer->multisampleMask != 0xFFFFFFFF)
	{
		createInfo.multisample_state.enable_mask = true;
		createInfo.multisample_state.sample_mask = renderer->multisampleMask;
	}
	else
	{
		createInfo.multisample_state.enable_mask = false;
		createInfo.multisample_state.sample_mask = 0;
	}

	/* Blend State */

	colorAttachmentDescriptions[0].blend_state.enable_blend = !(
		renderer->fnaBlendState.colorSourceBlend == FNA3D_BLEND_ONE &&
		renderer->fnaBlendState.colorDestinationBlend == FNA3D_BLEND_ZERO &&
		renderer->fnaBlendState.alphaSourceBlend == FNA3D_BLEND_ONE &&
		renderer->fnaBlendState.alphaDestinationBlend == FNA3D_BLEND_ZERO
	);
	if (colorAttachmentDescriptions[0].blend_state.enable_blend)
	{
		colorAttachmentDescriptions[0].blend_state.src_color_blendfactor = XNAToSDL_BlendFactor[
			renderer->fnaBlendState.colorSourceBlend
		];
		colorAttachmentDescriptions[0].blend_state.src_alpha_blendfactor = XNAToSDL_BlendFactor[
			renderer->fnaBlendState.alphaSourceBlend
		];
		colorAttachmentDescriptions[0].blend_state.dst_color_blendfactor = XNAToSDL_BlendFactor[
			renderer->fnaBlendState.colorDestinationBlend
		];
		colorAttachmentDescriptions[0].blend_state.dst_alpha_blendfactor = XNAToSDL_BlendFactor[
			renderer->fnaBlendState.alphaDestinationBlend
		];

		colorAttachmentDescriptions[0].blend_state.color_blend_op = XNAToSDL_BlendOp[
			renderer->fnaBlendState.colorBlendFunction
		];
		colorAttachmentDescriptions[0].blend_state.alpha_blend_op = XNAToSDL_BlendOp[
			renderer->fnaBlendState.alphaBlendFunction
		];
	}
	else
	{
		colorAttachmentDescriptions[0].blend_state.src_color_blendfactor = SDL_GPU_BLENDFACTOR_ONE;
		colorAttachmentDescriptions[0].blend_state.src_alpha_blendfactor = SDL_GPU_BLENDFACTOR_ONE;
		colorAttachmentDescriptions[0].blend_state.dst_color_blendfactor = SDL_GPU_BLENDFACTOR_ZERO;
		colorAttachmentDescriptions[0].blend_state.dst_alpha_blendfactor = SDL_GPU_BLENDFACTOR_ZERO;
		colorAttachmentDescriptions[0].blend_state.color_blend_op = SDL_GPU_BLENDOP_ADD;
		colorAttachmentDescriptions[0].blend_state.alpha_blend_op = SDL_GPU_BLENDOP_ADD;
	}

	colorAttachmentDescriptions[1].blend_state = colorAttachmentDescriptions[0].blend_state;
	colorAttachmentDescriptions[2].blend_state = colorAttachmentDescriptions[0].blend_state;
	colorAttachmentDescriptions[3].blend_state = colorAttachmentDescriptions[0].blend_state;

	colorAttachmentDescriptions[0].blend_state.color_write_mask =
		renderer->fnaBlendState.colorWriteEnable;
	colorAttachmentDescriptions[1].blend_state.color_write_mask =
		renderer->fnaBlendState.colorWriteEnable1;
	colorAttachmentDescriptions[2].blend_state.color_write_mask =
		renderer->fnaBlendState.colorWriteEnable2;
	colorAttachmentDescriptions[3].blend_state.color_write_mask =
		renderer->fnaBlendState.colorWriteEnable3;

	/* FIXME: Can this be disabled when mask is R|G|B|A? -flibit */
	colorAttachmentDescriptions[0].blend_state.enable_color_write_mask = true;
	colorAttachmentDescriptions[1].blend_state.enable_color_write_mask = true;
	colorAttachmentDescriptions[2].blend_state.enable_color_write_mask = true;
	colorAttachmentDescriptions[3].blend_state.enable_color_write_mask = true;

	colorAttachmentDescriptions[0].format = renderer->nextPipelineHash.colorFormats[0];
	colorAttachmentDescriptions[1].format = renderer->nextPipelineHash.colorFormats[1];
	colorAttachmentDescriptions[2].format = renderer->nextPipelineHash.colorFormats[2];
	colorAttachmentDescriptions[3].format = renderer->nextPipelineHash.colorFormats[3];

	createInfo.target_info.num_color_targets = renderer->nextRenderPassColorAttachmentCount;
	createInfo.target_info.color_target_descriptions = colorAttachmentDescriptions;
	createInfo.target_info.has_depth_stencil_target = renderer->nextPipelineHash.hasDepthStencilAttachment;
	createInfo.target_info.depth_stencil_format = renderer->nextPipelineHash.depthStencilFormat;

	/* Depth Stencil */

	createInfo.depth_stencil_state.enable_depth_test =
		renderer->fnaDepthStencilState.depthBufferEnable;
	createInfo.depth_stencil_state.enable_depth_write =
		renderer->fnaDepthStencilState.depthBufferWriteEnable;
	createInfo.depth_stencil_state.compare_op = XNAToSDL_CompareOp[
		renderer->fnaDepthStencilState.depthBufferFunction
	];
	createInfo.depth_stencil_state.enable_stencil_test =
		renderer->fnaDepthStencilState.stencilEnable;

	createInfo.depth_stencil_state.front_stencil_state.compare_op = XNAToSDL_CompareOp[
		renderer->fnaDepthStencilState.stencilFunction
	];
	createInfo.depth_stencil_state.front_stencil_state.depth_fail_op = XNAToSDL_StencilOp[
		renderer->fnaDepthStencilState.stencilDepthBufferFail
	];
	createInfo.depth_stencil_state.front_stencil_state.fail_op = XNAToSDL_StencilOp[
		renderer->fnaDepthStencilState.stencilFail
	];
	createInfo.depth_stencil_state.front_stencil_state.pass_op = XNAToSDL_StencilOp[
		renderer->fnaDepthStencilState.stencilPass
	];

	if (renderer->fnaDepthStencilState.twoSidedStencilMode)
	{
		createInfo.depth_stencil_state.back_stencil_state.compare_op = XNAToSDL_CompareOp[
			renderer->fnaDepthStencilState.ccwStencilFunction
		];
		createInfo.depth_stencil_state.back_stencil_state.depth_fail_op = XNAToSDL_StencilOp[
			renderer->fnaDepthStencilState.ccwStencilDepthBufferFail
		];
		createInfo.depth_stencil_state.back_stencil_state.fail_op = XNAToSDL_StencilOp[
			renderer->fnaDepthStencilState.ccwStencilFail
		];
		createInfo.depth_stencil_state.back_stencil_state.pass_op = XNAToSDL_StencilOp[
			renderer->fnaDepthStencilState.ccwStencilPass
		];
	}
	else
	{
		createInfo.depth_stencil_state.back_stencil_state = createInfo.depth_stencil_state.front_stencil_state;
	}

	createInfo.depth_stencil_state.compare_mask =
		renderer->fnaDepthStencilState.stencilMask;
	createInfo.depth_stencil_state.write_mask =
		renderer->fnaDepthStencilState.stencilWriteMask;

	/* Finally, after 1000 years, create the pipeline! */

	createInfo.props = 0;
	pipeline = SDL_CreateGPUGraphicsPipeline(
		renderer->device,
		&createInfo
	);

	SDL_stack_free(resourceSetLayoutInfos);

	if (pipeline == NULL)
	{
		FNA3D_LogError("Failed to create graphics pipeline!");
	}

	GraphicsPipelineHashTable_Insert(
		&renderer->graphicsPipelineHashTable,
		renderer->nextPipelineHash,
		pipeline
	);

	return pipeline;
}

static void SDLGPU_INTERNAL_BindGraphicsPipeline(
	SDLGPU_Renderer *renderer
) {
	SDL_GPUGraphicsPipeline *pipeline;
	MOJOSHADER_sdlShaderData *vertShaderData, *fragShaderData;

	MOJOSHADER_sdlGetBoundShaderData(
		renderer->mojoshaderContext,
		&vertShaderData,
		&fragShaderData
	);

	if (
		!renderer->needNewGraphicsPipeline &&
		renderer->currentVertexShader == vertShaderData &&
		renderer->currentFragmentShader == fragShaderData
	) {
		return;
	}

	pipeline = SDLGPU_INTERNAL_FetchGraphicsPipeline(renderer);

	if (pipeline != renderer->currentGraphicsPipeline)
	{
		SDL_BindGPUGraphicsPipeline(
			renderer->renderPass,
			pipeline
		);

		renderer->currentGraphicsPipeline = pipeline;
	}

	MOJOSHADER_sdlUpdateUniformBuffers(
		renderer->mojoshaderContext,
		renderer->renderCommandBuffer
	);

	renderer->currentVertexShader = vertShaderData;
	renderer->currentFragmentShader = fragShaderData;

	/* Reset deferred binding state */
	renderer->needNewGraphicsPipeline = 0;
	renderer->needFragmentSamplerBind = 1;
	renderer->needVertexSamplerBind = 1;
	renderer->needVertexBufferBind = 1;
	renderer->indexBufferBinding.buffer = NULL;
}

static SDL_GPUSampler* SDLGPU_INTERNAL_FetchSamplerState(
	SDLGPU_Renderer *renderer,
	FNA3D_SamplerState *samplerState
) {
	SDL_GPUSamplerCreateInfo samplerCreateInfo;
	SDL_GPUSampler *sampler;

	PackedState hash = GetPackedSamplerState(*samplerState);
	sampler = SamplerStateHashArray_Fetch(
		&renderer->samplerStateArray,
		hash
	);
	if (sampler != NULL)
	{
		return sampler;
	}

	samplerCreateInfo.mag_filter = XNAToSDL_MagFilter[samplerState->filter];
	samplerCreateInfo.min_filter = XNAToSDL_MinFilter[samplerState->filter];
	samplerCreateInfo.mipmap_mode = XNAToSDL_MipFilter[samplerState->filter];
	samplerCreateInfo.address_mode_u = XNAToSDL_SamplerAddressMode[
		samplerState->addressU
	];
	samplerCreateInfo.address_mode_v = XNAToSDL_SamplerAddressMode[
		samplerState->addressV
	];
	samplerCreateInfo.address_mode_w = XNAToSDL_SamplerAddressMode[
		samplerState->addressW
	];

	samplerCreateInfo.mip_lod_bias = samplerState->mipMapLevelOfDetailBias;
	samplerCreateInfo.enable_anisotropy = (samplerState->filter == FNA3D_TEXTUREFILTER_ANISOTROPIC);
	samplerCreateInfo.max_anisotropy = (float) SDL_max(1, samplerState->maxAnisotropy);
	samplerCreateInfo.enable_compare = 0;
	samplerCreateInfo.compare_op = 0;
	samplerCreateInfo.min_lod = (float) samplerState->maxMipLevel;
	samplerCreateInfo.max_lod = 1000.0f;
	samplerCreateInfo.props = 0;

	sampler = SDL_CreateGPUSampler(
		renderer->device,
		&samplerCreateInfo
	);

	if (sampler == NULL)
	{
		FNA3D_LogError("Failed to create sampler!");
		return NULL;
	}

	SamplerStateHashArray_Insert(
		&renderer->samplerStateArray,
		hash,
		sampler
	);

	return sampler;
}

static void SDLGPU_VerifyVertexSampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;
	SDL_GPUSampler *gpuSampler;
	MOJOSHADER_sdlShaderData *vertShader, *blah;
	MOJOSHADER_samplerType samplerType;

	MOJOSHADER_sdlGetBoundShaderData(renderer->mojoshaderContext, &vertShader, &blah);

	renderer->needVertexSamplerBind = 1;

	if (texture == NULL || sampler == NULL)
	{
		renderer->vertexTextureSamplerBindings[index].sampler = renderer->dummySampler;

		if (vertShader)
		{
			const MOJOSHADER_parseData *pd = MOJOSHADER_sdlGetShaderParseData(vertShader);
			if (index < pd->sampler_count)
			{
				samplerType = MOJOSHADER_sdlGetShaderParseData(vertShader)->samplers[index].type;

				if (samplerType == MOJOSHADER_SAMPLER_2D)
				{
					renderer->vertexTextureSamplerBindings[index].texture = renderer->dummyTexture2D;
				}
				else if (samplerType == MOJOSHADER_SAMPLER_VOLUME)
				{
					renderer->vertexTextureSamplerBindings[index].texture = renderer->dummyTexture3D;
				}
				else
				{
					renderer->vertexTextureSamplerBindings[index].texture = renderer->dummyTextureCube;
				}
			}
			else
			{
				renderer->vertexTextureSamplerBindings[index].texture = renderer->dummyTexture2D;
			}
		}
		else
		{
			renderer->vertexTextureSamplerBindings[index].texture = renderer->dummyTexture2D;
		}

		return;
	}

	if (textureHandle->texture != renderer->vertexTextureSamplerBindings[index].texture)
	{
		renderer->vertexTextureSamplerBindings[index].texture = textureHandle->texture;
	}

	gpuSampler = SDLGPU_INTERNAL_FetchSamplerState(
		renderer,
		sampler
	);

	if (gpuSampler != renderer->vertexTextureSamplerBindings[index].sampler)
	{
		renderer->vertexTextureSamplerBindings[index].sampler = gpuSampler;
		renderer->needVertexSamplerBind = 1;
	}
}

static void SDLGPU_VerifySampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;
	SDL_GPUSampler *gpuSampler;
	MOJOSHADER_sdlShaderData *blah, *fragShader;
	MOJOSHADER_samplerType samplerType;

	MOJOSHADER_sdlGetBoundShaderData(renderer->mojoshaderContext, &blah, &fragShader);

	renderer->needFragmentSamplerBind = 1;

	if (texture == NULL || sampler == NULL)
	{
		renderer->fragmentTextureSamplerBindings[index].sampler = renderer->dummySampler;

		if (fragShader)
		{
			const MOJOSHADER_parseData *pd = MOJOSHADER_sdlGetShaderParseData(fragShader);
			if (index < pd->sampler_count)
			{
				samplerType = pd->samplers[index].type;
				if (samplerType == MOJOSHADER_SAMPLER_2D)
				{
					renderer->fragmentTextureSamplerBindings[index].texture = renderer->dummyTexture2D;
				}
				else if (samplerType == MOJOSHADER_SAMPLER_VOLUME)
				{
					renderer->fragmentTextureSamplerBindings[index].texture = renderer->dummyTexture3D;
				}
				else
				{
					renderer->fragmentTextureSamplerBindings[index].texture = renderer->dummyTextureCube;
				}
			}
			else
			{
				renderer->fragmentTextureSamplerBindings[index].texture = renderer->dummyTexture2D;
			}
		}
		else
		{
			renderer->fragmentTextureSamplerBindings[index].texture = renderer->dummyTexture2D;
		}

		return;
	}

	if (textureHandle->texture != renderer->fragmentTextureSamplerBindings[index].texture)
	{
		renderer->fragmentTextureSamplerBindings[index].texture = textureHandle->texture;
	}

	gpuSampler = SDLGPU_INTERNAL_FetchSamplerState(
		renderer,
		sampler
	);

	if (gpuSampler != renderer->fragmentTextureSamplerBindings[index].sampler)
	{
		renderer->fragmentTextureSamplerBindings[index].sampler = gpuSampler;
		renderer->needFragmentSamplerBind = 1;
	}
}

static void SDLGPU_ApplyVertexBufferBindings(
	FNA3D_Renderer *driverData,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	MOJOSHADER_sdlShaderData *vertexShader, *blah;
	void* bindingsResult;
	FNA3D_VertexBufferBinding *src, *dst;
	int32_t i, bindingsIndex;
	uint32_t hash;

	if (renderer->supportsBaseVertex)
	{
		baseVertex = 0;
	}

	/* Check VertexBufferBindings */
	MOJOSHADER_sdlGetBoundShaderData(renderer->mojoshaderContext, &vertexShader, &blah);

	bindingsResult = PackedVertexBufferBindingsArray_Fetch(
		renderer->vertexBufferBindingsCache,
		bindings,
		numBindings,
		vertexShader,
		&bindingsIndex,
		&hash
	);

	if (bindingsResult == NULL)
	{
		PackedVertexBufferBindingsArray_Insert(
			&renderer->vertexBufferBindingsCache,
			bindings,
			numBindings,
			vertexShader,
			(void*) 69420
		);
	}

	if (bindingsUpdated)
	{
		renderer->numVertexBindings = numBindings;
		for (i = 0; i < numBindings; i += 1)
		{
			src = &bindings[i];
			dst = &renderer->vertexBindings[i];
			dst->vertexBuffer = src->vertexBuffer;
			dst->vertexOffset = src->vertexOffset;
			dst->instanceFrequency = src->instanceFrequency;
			dst->vertexDeclaration.vertexStride = src->vertexDeclaration.vertexStride;
			dst->vertexDeclaration.elementCount = src->vertexDeclaration.elementCount;
			SDL_memcpy(
				dst->vertexDeclaration.elements,
				src->vertexDeclaration.elements,
				sizeof(FNA3D_VertexElement) * src->vertexDeclaration.elementCount
			);
		}
	}

	if (bindingsIndex != renderer->currentVertexBufferBindingsIndex)
	{
		renderer->currentVertexBufferBindingsIndex = bindingsIndex;
		renderer->nextPipelineHash.vertexBufferBindingsIndex = renderer->currentVertexBufferBindingsIndex;
		renderer->needNewGraphicsPipeline = 1;
	}

	/* Don't actually bind buffers yet because pipelines are lazily bound */
	for (i = 0; i < numBindings; i += 1)
	{
		renderer->vertexBufferBindings[i].buffer = ((SDLGPU_BufferHandle*) bindings[i].vertexBuffer)->buffer;
		renderer->vertexBufferBindings[i].offset = (bindings[i].vertexOffset + baseVertex) * bindings[i].vertexDeclaration.vertexStride;
	}

	renderer->needVertexBufferBind = 1;
}

static void SDLGPU_SetViewport(
	FNA3D_Renderer *driverData,
	FNA3D_Viewport *viewport
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	if (	viewport->x != renderer->viewport.x ||
		viewport->y != renderer->viewport.y ||
		viewport->w != renderer->viewport.w ||
		viewport->h != renderer->viewport.h ||
		viewport->minDepth != renderer->viewport.minDepth ||
		viewport->maxDepth != renderer->viewport.maxDepth	)
	{
		renderer->viewport = *viewport;

		if (renderer->renderPass != NULL)
		{
			SDLGPU_INTERNAL_UpdateViewport(renderer);
		}
	}
}

static void SDLGPU_SetScissorRect(
	FNA3D_Renderer *driverData,
	FNA3D_Rect *scissor
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	renderer->scissorRect = ComputeRectIntersection(renderer->viewport.x, scissor->x,
		renderer->viewport.y, scissor->y,
		renderer->viewport.w, scissor->w,
		renderer->viewport.h, scissor->h);

	if (renderer->renderPass != NULL && renderer->fnaRasterizerState.scissorTestEnable)
	{
		SDL_SetGPUScissor(
			renderer->renderPass,
			&renderer->scissorRect
		);
	}
}

static void SDLGPU_GetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	blendFactor->r = (uint8_t) SDL_roundf(renderer->blendConstants[0] * 255.0f);
	blendFactor->g = (uint8_t) SDL_roundf(renderer->blendConstants[1] * 255.0f);
	blendFactor->b = (uint8_t) SDL_roundf(renderer->blendConstants[2] * 255.0f);
	blendFactor->a = (uint8_t) SDL_roundf(renderer->blendConstants[3] * 255.0f);
}

static void SDLGPU_SetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	if (
		blendFactor->r != renderer->blendConstants[0] ||
		blendFactor->g != renderer->blendConstants[1] ||
		blendFactor->b != renderer->blendConstants[2] ||
		blendFactor->a != renderer->blendConstants[3]
	) {
		renderer->blendConstants[0] = (float) blendFactor->r / 255.0f;
		renderer->blendConstants[1] = (float) blendFactor->g / 255.0f;
		renderer->blendConstants[2] = (float) blendFactor->b / 255.0f;
		renderer->blendConstants[3] = (float) blendFactor->a / 255.0f;

		renderer->needNewGraphicsPipeline = 1;
	}
}

static int32_t SDLGPU_GetMultiSampleMask(
	FNA3D_Renderer *driverData
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return (int32_t) renderer->multisampleMask;
}

static void SDLGPU_SetMultiSampleMask(
	FNA3D_Renderer *driverData,
	int32_t mask
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	if (renderer->multisampleMask != (uint32_t) mask)
	{
		renderer->multisampleMask = (uint32_t) mask;
		renderer->nextPipelineHash.sampleMask = renderer->multisampleMask;
		renderer->needNewGraphicsPipeline = 1;
	}
}

static int32_t SDLGPU_GetReferenceStencil(
	FNA3D_Renderer *driverData
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return (int32_t) renderer->stencilReference;
}

static void SDLGPU_SetReferenceStencil(
	FNA3D_Renderer *driverData,
	int32_t ref
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	if (renderer->stencilReference != (uint32_t) ref)
	{
		renderer->stencilReference = (uint32_t) ref;
		renderer->needNewGraphicsPipeline = 1;
	}
}

static void SDLGPU_SetBlendState(
	FNA3D_Renderer *driverData,
	FNA3D_BlendState *blendState
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	SDLGPU_SetBlendFactor(
		driverData,
		&blendState->blendFactor
	);

	SDLGPU_SetMultiSampleMask(
		driverData,
		blendState->multiSampleMask
	);

	if (SDL_memcmp(&renderer->fnaBlendState, blendState, sizeof(FNA3D_BlendState)) != 0)
	{
		SDL_memcpy(&renderer->fnaBlendState, blendState, sizeof(FNA3D_BlendState));
		renderer->nextPipelineHash.blendState = GetPackedBlendState(renderer->fnaBlendState);
		renderer->needNewGraphicsPipeline = 1;
	}
}

static void SDLGPU_SetDepthStencilState(
	FNA3D_Renderer *driverData,
	FNA3D_DepthStencilState *depthStencilState
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	/* TODO: Arrange these checks in an optimized priority */
	if (	renderer->fnaDepthStencilState.depthBufferEnable != depthStencilState->depthBufferEnable ||
		renderer->fnaDepthStencilState.depthBufferWriteEnable != depthStencilState->depthBufferWriteEnable ||
		renderer->fnaDepthStencilState.depthBufferFunction != depthStencilState->depthBufferFunction ||
		renderer->fnaDepthStencilState.stencilEnable != depthStencilState->stencilEnable ||
		renderer->fnaDepthStencilState.stencilMask != depthStencilState->stencilMask ||
		renderer->fnaDepthStencilState.stencilWriteMask != depthStencilState->stencilWriteMask ||
		renderer->fnaDepthStencilState.twoSidedStencilMode != depthStencilState->twoSidedStencilMode ||
		renderer->fnaDepthStencilState.stencilFail != depthStencilState->stencilFail ||
		renderer->fnaDepthStencilState.stencilDepthBufferFail != depthStencilState->stencilDepthBufferFail ||
		renderer->fnaDepthStencilState.stencilPass != depthStencilState->stencilPass ||
		renderer->fnaDepthStencilState.stencilFunction != depthStencilState->stencilFunction ||
		renderer->fnaDepthStencilState.ccwStencilFail != depthStencilState->ccwStencilFail ||
		renderer->fnaDepthStencilState.ccwStencilDepthBufferFail != depthStencilState->ccwStencilDepthBufferFail ||
		renderer->fnaDepthStencilState.ccwStencilPass != depthStencilState->ccwStencilPass ||
		renderer->fnaDepthStencilState.ccwStencilFunction != depthStencilState->ccwStencilFunction ||
		renderer->fnaDepthStencilState.referenceStencil != depthStencilState->referenceStencil	)
	{
		SDL_memcpy(
			&renderer->fnaDepthStencilState,
			depthStencilState,
			sizeof(FNA3D_DepthStencilState)
		);

		renderer->nextPipelineHash.depthStencilState = GetPackedDepthStencilState(
			renderer->fnaDepthStencilState
		);

		renderer->needNewGraphicsPipeline = 1;
	}

	SDLGPU_SetReferenceStencil(
		driverData,
		depthStencilState->referenceStencil
	);
}

static void SDLGPU_ApplyRasterizerState(
	FNA3D_Renderer *driverData,
	FNA3D_RasterizerState *rasterizerState
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDL_GPUTextureFormat depthStencilFormat = SDL_GPU_TEXTUREFORMAT_D16_UNORM;
	SDL_Rect scissorRect;
	float realDepthBias;

	if (renderer->nextRenderPassDepthStencilAttachment != NULL)
	{
		depthStencilFormat = renderer->nextRenderPassDepthStencilAttachment->createInfo.format;
	}

	if (rasterizerState->scissorTestEnable != renderer->fnaRasterizerState.scissorTestEnable)
	{
		renderer->fnaRasterizerState.scissorTestEnable = rasterizerState->scissorTestEnable;

		if (renderer->renderPass != NULL)
		{
			if (renderer->fnaRasterizerState.scissorTestEnable)
			{
				scissorRect = ComputeRectIntersection(renderer->viewport.x, renderer->scissorRect.x,
					renderer->viewport.y, renderer->scissorRect.y,
					renderer->viewport.w, renderer->scissorRect.w,
					renderer->viewport.h, renderer->scissorRect.h);
			}
			else
			{
				scissorRect.x = renderer->viewport.x;
				scissorRect.y = renderer->viewport.y;
				scissorRect.w = renderer->viewport.w;
				scissorRect.h = renderer->viewport.h;
			}
			SDL_SetGPUScissor(
				renderer->renderPass,
				&scissorRect
			);
		}
	}

	realDepthBias = rasterizerState->depthBias * XNAToSDL_DepthBiasScale(
		depthStencilFormat
	);

	if (
		rasterizerState->cullMode != renderer->fnaRasterizerState.cullMode ||
		rasterizerState->fillMode != renderer->fnaRasterizerState.fillMode ||
		rasterizerState->multiSampleAntiAlias != renderer->fnaRasterizerState.multiSampleAntiAlias ||
		realDepthBias != renderer->fnaRasterizerState.depthBias ||
		rasterizerState->slopeScaleDepthBias != renderer->fnaRasterizerState.slopeScaleDepthBias
	) {
		renderer->fnaRasterizerState.cullMode = rasterizerState->cullMode;
		renderer->fnaRasterizerState.fillMode = rasterizerState->fillMode;
		renderer->fnaRasterizerState.multiSampleAntiAlias = rasterizerState->multiSampleAntiAlias;
		renderer->fnaRasterizerState.depthBias = realDepthBias;
		renderer->fnaRasterizerState.slopeScaleDepthBias = rasterizerState->slopeScaleDepthBias;

		renderer->nextPipelineHash.rasterizerState = GetPackedRasterizerState(
			renderer->fnaRasterizerState,
			renderer->fnaRasterizerState.depthBias
		);

		renderer->needNewGraphicsPipeline = 1;
	}
}

static void SDLGPU_INTERNAL_BindVertexSamplers(
	SDLGPU_Renderer *renderer
) {
	MOJOSHADER_sdlShaderData *vertShaderData, *blah;

	MOJOSHADER_sdlGetBoundShaderData(
		renderer->mojoshaderContext,
		&vertShaderData,
		&blah
	);

	SDL_BindGPUVertexSamplers(
		renderer->renderPass,
		0,
		renderer->vertexTextureSamplerBindings,
		MOJOSHADER_sdlGetSamplerSlots(vertShaderData)
	);
}

static void SDLGPU_INTERNAL_BindFragmentSamplers(
	SDLGPU_Renderer *renderer
) {
	MOJOSHADER_sdlShaderData *blah, *fragShaderData;

	MOJOSHADER_sdlGetBoundShaderData(
		renderer->mojoshaderContext,
		&blah,
		&fragShaderData
	);

	SDL_BindGPUFragmentSamplers(
		renderer->renderPass,
		0,
		renderer->fragmentTextureSamplerBindings,
		MOJOSHADER_sdlGetSamplerSlots(fragShaderData)
	);
}

/* Actually bind all deferred state before drawing! */
static void SDLGPU_INTERNAL_BindDeferredState(
	SDLGPU_Renderer *renderer,
	FNA3D_PrimitiveType primitiveType,
	SDL_GPUBuffer *indexBuffer, /* can be NULL */
	SDL_GPUIndexElementSize indexElementSize
) {
	if (primitiveType != renderer->fnaPrimitiveType)
	{
		renderer->fnaPrimitiveType = primitiveType;
		renderer->nextPipelineHash.primitiveType = renderer->fnaPrimitiveType;
		renderer->needNewGraphicsPipeline = 1;
	}

	SDLGPU_INTERNAL_BeginRenderPass(renderer);

	SDLGPU_INTERNAL_BindGraphicsPipeline(renderer);

	if (	renderer->currentBlendConstants.r != renderer->blendConstants[0] ||
		renderer->currentBlendConstants.g != renderer->blendConstants[1] ||
		renderer->currentBlendConstants.b != renderer->blendConstants[2] ||
		renderer->currentBlendConstants.a != renderer->blendConstants[3]	)
	{
		renderer->currentBlendConstants.r = renderer->blendConstants[0];
		renderer->currentBlendConstants.g = renderer->blendConstants[1];
		renderer->currentBlendConstants.b = renderer->blendConstants[2];
		renderer->currentBlendConstants.a = renderer->blendConstants[3];
		SDL_SetGPUBlendConstants(renderer->renderPass, renderer->currentBlendConstants);
	}

	if (renderer->currentStencilReference != renderer->stencilReference)
	{
		SDL_SetGPUStencilReference(renderer->renderPass, renderer->stencilReference);
		renderer->currentStencilReference = renderer->stencilReference;
	}

	if (renderer->needVertexSamplerBind || renderer->needFragmentSamplerBind)
	{
		if (renderer->needVertexSamplerBind)
		{
			SDLGPU_INTERNAL_BindVertexSamplers(renderer);
		}

		if (renderer->needFragmentSamplerBind)
		{
			SDLGPU_INTERNAL_BindFragmentSamplers(renderer);
		}
	}

	if (
		indexBuffer != NULL &&
		renderer->indexBufferBinding.buffer != indexBuffer
	) {
		renderer->indexBufferBinding.buffer = indexBuffer;

		SDL_BindGPUIndexBuffer(
			renderer->renderPass,
			&renderer->indexBufferBinding,
			indexElementSize
		);
	}

	if (renderer->needVertexBufferBind)
	{
		SDL_BindGPUVertexBuffers(
			renderer->renderPass,
			0,
			renderer->vertexBufferBindings,
			renderer->numVertexBindings
		);
	}
}

static void SDLGPU_DrawInstancedPrimitives(
	FNA3D_Renderer *driverData,
	FNA3D_PrimitiveType primitiveType,
	int32_t baseVertex,
	int32_t minVertexIndex,
	int32_t numVertices,
	int32_t startIndex,
	int32_t primitiveCount,
	int32_t instanceCount,
	FNA3D_Buffer *indices,
	FNA3D_IndexElementSize indexElementSize
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	/* Note that minVertexIndex/numVertices are NOT used! */

	if (!renderer->supportsBaseVertex)
	{
		baseVertex = 0;
	}

	SDLGPU_INTERNAL_BindDeferredState(
		renderer,
		primitiveType,
		((SDLGPU_BufferHandle*) indices)->buffer,
		XNAToSDL_IndexElementSize[indexElementSize]
	);

	SDL_DrawGPUIndexedPrimitives(
		renderer->renderPass,
		PrimitiveVerts(primitiveType, primitiveCount),
		instanceCount,
		startIndex,
		baseVertex,
		0
	);
}

static void SDLGPU_DrawIndexedPrimitives(
	FNA3D_Renderer *driverData,
	FNA3D_PrimitiveType primitiveType,
	int32_t baseVertex,
	int32_t minVertexIndex,
	int32_t numVertices,
	int32_t startIndex,
	int32_t primitiveCount,
	FNA3D_Buffer *indices,
	FNA3D_IndexElementSize indexElementSize
) {
	SDLGPU_DrawInstancedPrimitives(
		driverData,
		primitiveType,
		baseVertex,
		minVertexIndex,
		numVertices,
		startIndex,
		primitiveCount,
		1,
		indices,
		indexElementSize
	);
}

static void SDLGPU_DrawPrimitives(
	FNA3D_Renderer *driverData,
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	SDLGPU_INTERNAL_BindDeferredState(
		renderer,
		primitiveType,
		NULL,
		SDL_GPU_INDEXELEMENTSIZE_16BIT
	);

	SDL_DrawGPUPrimitives(
		renderer->renderPass,
		PrimitiveVerts(primitiveType, primitiveCount),
		1,
		vertexStart,
		0
	);
}

/* Backbuffer Functions */

static void SDLGPU_INTERNAL_FreeTextureHandle(
	SDLGPU_Renderer *renderer,
	SDLGPU_TextureHandle *handle
) {
	uint32_t i;
	if (handle->boundAsRenderTarget)
	{
		for (i = 0; i < renderer->boundRenderTargetCount; i += 1)
		{
			if (renderer->boundRenderTargets[i] == handle)
			{
				renderer->boundRenderTargets[i] = NULL;
				break;
			}
		}
	}
	SDL_ReleaseGPUTexture(
		renderer->device,
		handle->texture
	);
	SDL_free(handle);
}

static void SDLGPU_INTERNAL_DestroyFauxBackbuffer(
	SDLGPU_Renderer *renderer
) {
	if (renderer->fauxBackbufferColorRenderbuffer != NULL)
	{
		SDLGPU_INTERNAL_FreeTextureHandle(
			renderer,
			renderer->fauxBackbufferColorRenderbuffer
		);
		renderer->fauxBackbufferColorRenderbuffer = NULL;
	}

	SDLGPU_INTERNAL_FreeTextureHandle(
		renderer,
		renderer->fauxBackbufferColorTexture
	);
	renderer->fauxBackbufferColorTexture = NULL;

	if (renderer->fauxBackbufferDepthStencil != NULL)
	{
		SDLGPU_INTERNAL_FreeTextureHandle(
			renderer,
			renderer->fauxBackbufferDepthStencil
		);
		renderer->fauxBackbufferDepthStencil = NULL;
	}
}

static SDLGPU_TextureHandle* SDLGPU_INTERNAL_CreateTextureWithHandle(
	SDLGPU_Renderer *renderer,
	uint32_t width,
	uint32_t height,
	uint32_t depth,
	SDL_GPUTextureFormat format,
	uint32_t layerCount,
	uint32_t levelCount,
	SDL_GPUTextureUsageFlags usageFlags,
	SDL_GPUSampleCount sampleCount
) {
	SDL_GPUTextureCreateInfo textureCreateInfo;
	SDL_GPUTexture *texture;
	SDLGPU_TextureHandle *textureHandle;

	textureCreateInfo.width = width;
	textureCreateInfo.height = height;
	textureCreateInfo.format = format;
	textureCreateInfo.num_levels = levelCount;
	if (layerCount == 6)
	{
		textureCreateInfo.type = SDL_GPU_TEXTURETYPE_CUBE;
		textureCreateInfo.layer_count_or_depth = layerCount;
	}
	else if (depth > 1)
	{
		textureCreateInfo.type = SDL_GPU_TEXTURETYPE_3D;
		textureCreateInfo.layer_count_or_depth = depth;
	}
	else
	{
		textureCreateInfo.type = SDL_GPU_TEXTURETYPE_2D;
		textureCreateInfo.layer_count_or_depth = 1;
	}
	textureCreateInfo.usage = usageFlags;
	textureCreateInfo.sample_count = sampleCount;
	textureCreateInfo.props = 0;

	texture = SDL_CreateGPUTexture(
		renderer->device,
		&textureCreateInfo
	);

	if (texture == NULL)
	{
		FNA3D_LogError("Failed to create texture!");
		return NULL;
	}

	textureHandle = SDL_malloc(sizeof(SDLGPU_TextureHandle));
	textureHandle->texture = texture;
	textureHandle->createInfo = textureCreateInfo;
	textureHandle->boundAsRenderTarget = 0;

	return textureHandle;
}

static void SDLGPU_INTERNAL_CreateFauxBackbuffer(
	SDLGPU_Renderer *renderer,
	FNA3D_PresentationParameters *presentationParameters
) {
	SDL_GPUSampleCount sampleCount = XNAToSDL_SampleCount(presentationParameters->multiSampleCount);

	if (sampleCount > SDL_GPU_SAMPLECOUNT_1)
	{
		renderer->fauxBackbufferColorRenderbuffer = SDLGPU_INTERNAL_CreateTextureWithHandle(
			renderer,
			presentationParameters->backBufferWidth,
			presentationParameters->backBufferHeight,
			1,
			XNAToSDL_SurfaceFormat[presentationParameters->backBufferFormat],
			1,
			1,
			SDL_GPU_TEXTUREUSAGE_COLOR_TARGET,
			sampleCount
		);
	}
	renderer->fauxBackbufferColorTexture = SDLGPU_INTERNAL_CreateTextureWithHandle(
		renderer,
		presentationParameters->backBufferWidth,
		presentationParameters->backBufferHeight,
		1,
		XNAToSDL_SurfaceFormat[presentationParameters->backBufferFormat],
		1,
		1,
		SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | SDL_GPU_TEXTUREUSAGE_SAMPLER,
		SDL_GPU_SAMPLECOUNT_1
	);

	if (presentationParameters->depthStencilFormat != FNA3D_DEPTHFORMAT_NONE)
	{
		renderer->fauxBackbufferDepthStencil = SDLGPU_INTERNAL_CreateTextureWithHandle(
			renderer,
			presentationParameters->backBufferWidth,
			presentationParameters->backBufferHeight,
			1,
			XNAToSDL_DepthFormat(renderer, presentationParameters->depthStencilFormat),
			1,
			1,
			SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET,
			sampleCount
		);
	}

	renderer->readbackBackbufferSurfaceFormat = presentationParameters->backBufferFormat;
	renderer->readbackBackbufferDepthFormat = presentationParameters->depthStencilFormat;
	renderer->readbackBackbufferMultiSampleCount = presentationParameters->multiSampleCount;

	/* Set default render pass state if necessary */
	if (!renderer->renderTargetInUse)
	{
		if (renderer->fauxBackbufferColorRenderbuffer != NULL)
		{
			renderer->nextRenderPassColorAttachments[0] = renderer->fauxBackbufferColorRenderbuffer;
			renderer->nextRenderPassColorResolves[0] = renderer->fauxBackbufferColorTexture;
			renderer->nextRenderPassMultisampleCount = renderer->fauxBackbufferColorRenderbuffer->createInfo.sample_count;
		}
		else
		{
			renderer->nextRenderPassColorAttachments[0] = renderer->fauxBackbufferColorTexture;
			renderer->nextRenderPassColorResolves[0] = NULL;
			renderer->nextRenderPassMultisampleCount = SDL_GPU_SAMPLECOUNT_1;
		}
		renderer->nextRenderPassColorAttachmentCubeFace[0] = 0;
		renderer->nextRenderPassColorAttachmentCount = 1;

		renderer->nextRenderPassDepthStencilAttachment = renderer->fauxBackbufferDepthStencil;

		renderer->nextPipelineHash.sampleCount = renderer->nextRenderPassMultisampleCount;
		renderer->nextPipelineHash.colorFormatCount = renderer->nextRenderPassColorAttachmentCount;
		renderer->nextPipelineHash.colorFormats[0] = renderer->nextRenderPassColorAttachments[0]->createInfo.format;
		renderer->nextPipelineHash.colorFormats[1] = SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
		renderer->nextPipelineHash.colorFormats[2] = SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
		renderer->nextPipelineHash.colorFormats[3] = SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
		renderer->nextPipelineHash.hasDepthStencilAttachment = renderer->nextRenderPassDepthStencilAttachment != NULL;
		if (renderer->nextPipelineHash.hasDepthStencilAttachment)
		{
			renderer->nextPipelineHash.depthStencilFormat = renderer->nextRenderPassDepthStencilAttachment->createInfo.format;
		}
		else
		{
			renderer->nextPipelineHash.depthStencilFormat = SDL_GPU_TEXTUREFORMAT_D16_UNORM;
		}
	}
}

static void SDLGPU_ResetBackbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_PresentationParameters *presentationParameters
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDL_GPUSwapchainComposition swapchainComposition;
	SDL_GPUPresentMode presentMode;

	SDL_LockMutex(renderer->copyPassMutex);
	SDLGPU_INTERNAL_FlushCommandsAndStall(renderer);

	SDLGPU_INTERNAL_DestroyFauxBackbuffer(renderer);

	if (presentationParameters->deviceWindowHandle != NULL)
	{
		SDLGPU_INTERNAL_ClaimWindow(
			renderer,
			(SDL_Window*) presentationParameters->deviceWindowHandle
		);
	}

	SDLGPU_INTERNAL_CreateFauxBackbuffer(
		renderer,
		presentationParameters
	);

	swapchainComposition = (presentationParameters->backBufferFormat == FNA3D_SURFACEFORMAT_COLORSRGB_EXT)
		? SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR
		: SDL_GPU_SWAPCHAINCOMPOSITION_SDR;

	if (SDL_GetHintBoolean("FNA3D_ENABLE_HDR_COLORSPACE", false))
	{
		if (presentationParameters->backBufferFormat == FNA3D_SURFACEFORMAT_RGBA1010102)
		{
			swapchainComposition = SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084;
		}
		else if (	presentationParameters->backBufferFormat == FNA3D_SURFACEFORMAT_HALFVECTOR4 ||
				presentationParameters->backBufferFormat == FNA3D_SURFACEFORMAT_HDRBLENDABLE	)
		{
			swapchainComposition = SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR;
		}
	}

	if (!XNAToSDL_PresentMode(
		renderer->device,
		presentationParameters->deviceWindowHandle,
		presentationParameters->presentationInterval,
		&presentMode
	)) {
		FNA3D_LogError("Failed to set suitable present mode!");
		return;
	}

	SDL_SetGPUSwapchainParameters(
		renderer->device,
		presentationParameters->deviceWindowHandle,
		swapchainComposition,
		presentMode
	);

	SDL_UnlockMutex(renderer->copyPassMutex);
}

static void SDLGPU_GetBackbufferSize(
	FNA3D_Renderer *driverData,
	int32_t *w,
	int32_t *h
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	*w = (int32_t) renderer->fauxBackbufferColorTexture->createInfo.width;
	*h = (int32_t) renderer->fauxBackbufferColorTexture->createInfo.height;
}

static FNA3D_SurfaceFormat SDLGPU_GetBackbufferSurfaceFormat(
	FNA3D_Renderer *driverData
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return renderer->readbackBackbufferSurfaceFormat;
}

static FNA3D_DepthFormat SDLGPU_GetBackbufferDepthFormat(
	FNA3D_Renderer *driverData
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return renderer->readbackBackbufferDepthFormat;
}

static int32_t SDLGPU_GetBackbufferMultiSampleCount(
	FNA3D_Renderer *driverData
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return renderer->readbackBackbufferMultiSampleCount;
}

/* Textures */

static FNA3D_Texture* SDLGPU_CreateTexture2D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	SDL_GPUTextureUsageFlags usageFlags = SDL_GPU_TEXTUREUSAGE_SAMPLER;

	if (isRenderTarget)
	{
		usageFlags |= SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;
	}

	return (FNA3D_Texture*) SDLGPU_INTERNAL_CreateTextureWithHandle(
		(SDLGPU_Renderer*) driverData,
		(uint32_t) width,
		(uint32_t) height,
		1,
		XNAToSDL_SurfaceFormat[format],
		1,
		levelCount,
		usageFlags,
		SDL_GPU_SAMPLECOUNT_1
	);
}

static FNA3D_Texture* SDLGPU_CreateTexture3D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount
) {
	return (FNA3D_Texture*) SDLGPU_INTERNAL_CreateTextureWithHandle(
		(SDLGPU_Renderer*) driverData,
		(uint32_t) width,
		(uint32_t) height,
		(uint32_t) depth,
		XNAToSDL_SurfaceFormat[format],
		1,
		levelCount,
		SDL_GPU_TEXTUREUSAGE_SAMPLER,
		SDL_GPU_SAMPLECOUNT_1
	);
}

static FNA3D_Texture* SDLGPU_CreateTextureCube(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	SDL_GPUTextureUsageFlags usageFlags = SDL_GPU_TEXTUREUSAGE_SAMPLER;

	if (isRenderTarget)
	{
		usageFlags |= SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;
	}

	return (FNA3D_Texture*) SDLGPU_INTERNAL_CreateTextureWithHandle(
		(SDLGPU_Renderer*) driverData,
		(uint32_t) size,
		(uint32_t) size,
		1,
		XNAToSDL_SurfaceFormat[format],
		6,
		levelCount,
		usageFlags,
		SDL_GPU_SAMPLECOUNT_1
	);
}

static FNA3D_Renderbuffer* SDLGPU_GenColorRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture
) {
	SDLGPU_TextureHandle *textureHandle;
	SDLGPU_Renderbuffer *colorBufferHandle;
	SDL_GPUSampleCount sampleCount = XNAToSDL_SampleCount(multiSampleCount);

	textureHandle = SDLGPU_INTERNAL_CreateTextureWithHandle(
		(SDLGPU_Renderer*) driverData,
		(uint32_t) width,
		(uint32_t) height,
		1,
		XNAToSDL_SurfaceFormat[format],
		1,
		1,
		SDL_GPU_TEXTUREUSAGE_COLOR_TARGET,
		sampleCount
	);

	if (textureHandle == NULL)
	{
		FNA3D_LogError("Failed to recreate color buffer texture!");
		return NULL;
	}

	colorBufferHandle = SDL_malloc(sizeof(SDLGPU_Renderbuffer));
	colorBufferHandle->textureHandle = textureHandle;
	colorBufferHandle->sampleCount = sampleCount;
	colorBufferHandle->format = XNAToSDL_SurfaceFormat[format];

	return (FNA3D_Renderbuffer*) colorBufferHandle;
}

static FNA3D_Renderbuffer* SDLGPU_GenDepthStencilRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_TextureHandle *textureHandle;
	SDLGPU_Renderbuffer *renderbuffer;

	textureHandle = SDLGPU_INTERNAL_CreateTextureWithHandle(
		renderer,
		(uint32_t) width,
		(uint32_t) height,
		1,
		XNAToSDL_DepthFormat(renderer, format),
		1,
		1,
		SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET,
		XNAToSDL_SampleCount(multiSampleCount)
	);

	if (textureHandle == NULL)
	{
		FNA3D_LogError("Failed to create depth stencil buffer!");
		return NULL;
	}

	renderbuffer = SDL_malloc(sizeof(SDLGPU_Renderbuffer));
	renderbuffer->textureHandle = textureHandle;
	renderbuffer->sampleCount = XNAToSDL_SampleCount(multiSampleCount);
	renderbuffer->format = XNAToSDL_DepthFormat(renderer, format);

	return (FNA3D_Renderbuffer*) renderbuffer;
}

static void SDLGPU_AddDisposeTexture(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;

	SDLGPU_INTERNAL_FreeTextureHandle(renderer, textureHandle);
}

static void SDLGPU_AddDisposeRenderbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Renderbuffer *renderbuffer
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_Renderbuffer *renderbufferHandle = (SDLGPU_Renderbuffer*) renderbuffer;

	SDLGPU_INTERNAL_FreeTextureHandle(renderer, renderbufferHandle->textureHandle);

	SDL_free(renderbufferHandle);
}

static void SDLGPU_INTERNAL_SetTextureData(
	SDLGPU_Renderer *renderer,
	SDL_GPUTexture *texture,
	SDL_GPUTextureFormat format,
	uint32_t x,
	uint32_t y,
	uint32_t z,
	uint32_t w,
	uint32_t h,
	uint32_t d,
	uint32_t layer,
	uint32_t mipLevel,
	void* data,
	uint32_t dataLength,
	bool cycleTexture
) {
	SDL_LockMutex(renderer->copyPassMutex);

	SDL_GPUTextureRegion textureRegion;
	SDL_GPUTextureTransferInfo textureCopyParams;
	SDL_GPUTransferBufferCreateInfo transferBufferCreateInfo;
	SDL_GPUTransferBuffer *transferBuffer = renderer->textureUploadBuffer;
	uint32_t transferOffset;
	bool cycle = renderer->textureUploadBufferOffset == 0;
	bool usingTemporaryTransferBuffer = false;
	uint8_t *dst;

	renderer->textureUploadBufferOffset = SDLGPU_INTERNAL_RoundToAlignment(
		renderer->textureUploadBufferOffset,
		SDL_GPUTextureFormatTexelBlockSize(format)
	);
	transferOffset = renderer->textureUploadBufferOffset;

	if (dataLength >= TRANSFER_BUFFER_SIZE)
	{
		/* Upload is too big, create a temporary transfer buffer */
		transferBufferCreateInfo.usage = SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
		transferBufferCreateInfo.size = dataLength;
		transferBufferCreateInfo.props = 0;
		transferBuffer = SDL_CreateGPUTransferBuffer(
			renderer->device,
			&transferBufferCreateInfo
		);
		usingTemporaryTransferBuffer = true;
		cycle = false;
		transferOffset = 0;
	}
	else if (
		renderer->textureUploadBufferOffset + dataLength >= TRANSFER_BUFFER_SIZE
	) {
		if (renderer->textureUploadCycleCount < MAX_UPLOAD_CYCLE_COUNT)
		{
			/* Cycle transfer buffer if necessary */
			cycle = true;
			renderer->textureUploadCycleCount += 1;
			renderer->textureUploadBufferOffset = 0;
			transferOffset = 0;
		}
		else
		{
			/* We cycled transfers a lot, send the upload commands to reduce further transfer memory usage */
			SDLGPU_INTERNAL_FlushUploadCommands(renderer);
			cycle = true;
			transferOffset = 0;
		}
	}

	dst = (uint8_t*) SDL_MapGPUTransferBuffer(renderer->device, transferBuffer, cycle);
	SDL_memcpy(dst + transferOffset, data, dataLength);
	SDL_UnmapGPUTransferBuffer(renderer->device, transferBuffer);

	textureRegion.texture = texture;
	textureRegion.layer = layer;
	textureRegion.mip_level = mipLevel;
	textureRegion.x = x;
	textureRegion.y = y;
	textureRegion.z = z;
	textureRegion.w = w;
	textureRegion.h = h;
	textureRegion.d = d;

	textureCopyParams.transfer_buffer = transferBuffer;
	textureCopyParams.offset = transferOffset;
	textureCopyParams.pixels_per_row = 0;	/* default, assume tightly packed */
	textureCopyParams.rows_per_layer = 0;	/* default, assume tightly packed */

	SDL_UploadToGPUTexture(
		renderer->copyPass,
		&textureCopyParams,
		&textureRegion,
		cycleTexture
	);

	if (usingTemporaryTransferBuffer)
	{
		SDL_ReleaseGPUTransferBuffer(renderer->device, transferBuffer);
	}
	else
	{
		renderer->textureUploadBufferOffset += dataLength;
	}

	SDL_UnlockMutex(renderer->copyPassMutex);
}

static void SDLGPU_SetTextureData2D(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;

	bool cycleTexture = (x == 0) && (y == 0) && (level == 0) &&
		(w == textureHandle->createInfo.width) &&
		(h == textureHandle->createInfo.height) &&
		(textureHandle->createInfo.layer_count_or_depth <= 1) &&
		(textureHandle->createInfo.num_levels <= 1);

	SDLGPU_INTERNAL_SetTextureData(
		(SDLGPU_Renderer*) driverData,
		textureHandle->texture,
		textureHandle->createInfo.format,
		(uint32_t) x,
		(uint32_t) y,
		0,
		(uint32_t) w,
		(uint32_t) h,
		1,
		0,
		(uint32_t) level,
		data,
		dataLength,
		cycleTexture
	);
}

static void SDLGPU_SetTextureData3D(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t z,
	int32_t w,
	int32_t h,
	int32_t d,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;

	SDLGPU_INTERNAL_SetTextureData(
		(SDLGPU_Renderer*) driverData,
		textureHandle->texture,
		textureHandle->createInfo.format,
		(uint32_t) x,
		(uint32_t) y,
		(uint32_t) z,
		(uint32_t) w,
		(uint32_t) h,
		(uint32_t) d,
		0,
		(uint32_t) level,
		data,
		dataLength,
		0 /* cycleTexture */
	);
}

static void SDLGPU_SetTextureDataCube(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	FNA3D_CubeMapFace cubeMapFace,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;

	SDLGPU_INTERNAL_SetTextureData(
		(SDLGPU_Renderer*) driverData,
		textureHandle->texture,
		textureHandle->createInfo.format,
		(uint32_t) x,
		(uint32_t) y,
		0,
		(uint32_t) w,
		(uint32_t) h,
		1,
		(uint32_t) cubeMapFace,
		(uint32_t) level,
		data,
		dataLength,
		0 /* cycleTexture */
	);
}

static void SDLGPU_SetTextureDataYUV(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *y,
	FNA3D_Texture *u,
	FNA3D_Texture *v,
	int32_t yWidth,
	int32_t yHeight,
	int32_t uvWidth,
	int32_t uvHeight,
	void* data,
	int32_t dataLength
) {
	SDLGPU_TextureHandle *yHandle = (SDLGPU_TextureHandle*) y;
	SDLGPU_TextureHandle *uHandle = (SDLGPU_TextureHandle*) u;
	SDLGPU_TextureHandle *vHandle = (SDLGPU_TextureHandle*) v;

	int32_t yDataLength = BytesPerImage(yWidth, yHeight, FNA3D_SURFACEFORMAT_ALPHA8);
	int32_t uvDataLength = BytesPerImage(uvWidth, uvHeight, FNA3D_SURFACEFORMAT_ALPHA8);

	SDLGPU_INTERNAL_SetTextureData(
		(SDLGPU_Renderer*) driverData,
		yHandle->texture,
		yHandle->createInfo.format,
		0,
		0,
		0,
		(uint32_t) yWidth,
		(uint32_t) yHeight,
		1,
		0,
		0,
		data,
		yDataLength,
		0 /* cycleTexture */
	);

	SDLGPU_INTERNAL_SetTextureData(
		(SDLGPU_Renderer*) driverData,
		uHandle->texture,
		uHandle->createInfo.format,
		0,
		0,
		0,
		(uint32_t) uvWidth,
		(uint32_t) uvHeight,
		1,
		0,
		0,
		(uint8_t*) data + yDataLength,
		uvDataLength,
		0 /* cycleTexture */
	);

	SDLGPU_INTERNAL_SetTextureData(
		(SDLGPU_Renderer*) driverData,
		vHandle->texture,
		vHandle->createInfo.format,
		0,
		0,
		0,
		(uint32_t) uvWidth,
		(uint32_t) uvHeight,
		1,
		0,
		0,
		(uint8_t*) data + yDataLength + uvDataLength,
		uvDataLength,
		0 /* cycleTexture */
	);
}

/* Buffers */

static FNA3D_Buffer* SDLGPU_GenVertexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	SDL_GPUBufferCreateInfo createInfo;
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	SDLGPU_BufferHandle *bufferHandle =
		SDL_malloc(sizeof(SDLGPU_BufferHandle));

	createInfo.usage = SDL_GPU_BUFFERUSAGE_VERTEX;
	createInfo.size = sizeInBytes;
	createInfo.props = 0;
	bufferHandle->buffer = SDL_CreateGPUBuffer(
		renderer->device,
		&createInfo
	);
	bufferHandle->size = sizeInBytes;

	return (FNA3D_Buffer*) bufferHandle;
}

static FNA3D_Buffer* SDLGPU_GenIndexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	SDL_GPUBufferCreateInfo createInfo;
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_BufferHandle *bufferHandle =
		SDL_malloc(sizeof(SDLGPU_BufferHandle));

	createInfo.usage = SDL_GPU_BUFFERUSAGE_INDEX;
	createInfo.size = sizeInBytes;
	createInfo.props = 0;
	bufferHandle->buffer = SDL_CreateGPUBuffer(
		renderer->device,
		&createInfo
	);
	bufferHandle->size = (uint32_t) sizeInBytes;

	return (FNA3D_Buffer*) bufferHandle;
}

static void SDLGPU_AddDisposeVertexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_BufferHandle *bufferHandle = (SDLGPU_BufferHandle*) buffer;

	SDL_ReleaseGPUBuffer(
		renderer->device,
		bufferHandle->buffer
	);

	SDL_free(bufferHandle);
}

static void SDLGPU_AddDisposeIndexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_BufferHandle *bufferHandle = (SDLGPU_BufferHandle*) buffer;

	SDL_ReleaseGPUBuffer(
		renderer->device,
		bufferHandle->buffer
	);

	SDL_free(bufferHandle);
}

static void SDLGPU_INTERNAL_SetBufferData(
	SDLGPU_Renderer *renderer,
	SDL_GPUBuffer *buffer,
	uint32_t dstOffset,
	void *data,
	uint32_t dataLength,
	bool cycle
) {
	SDL_LockMutex(renderer->copyPassMutex);

	SDL_GPUTransferBufferCreateInfo transferBufferCreateInfo;
	SDL_GPUTransferBufferLocation transferLocation;
	SDL_GPUBufferRegion bufferRegion;
	SDL_GPUTransferBuffer *transferBuffer = renderer->bufferUploadBuffer;
	uint32_t transferOffset = renderer->bufferUploadBufferOffset;
	bool transferCycle = renderer->bufferUploadBufferOffset == 0;
	bool usingTemporaryTransferBuffer = false;
	uint8_t *dst;

	if (dataLength >= TRANSFER_BUFFER_SIZE)
	{
		/* Upload is too big, create a temporary transfer buffer */
		transferBufferCreateInfo.usage = SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
		transferBufferCreateInfo.size = dataLength;
		transferBufferCreateInfo.props = 0;
		transferBuffer = SDL_CreateGPUTransferBuffer(
			renderer->device,
			&transferBufferCreateInfo
		);
		usingTemporaryTransferBuffer = true;
		transferCycle = false;
		transferOffset = 0;
	}
	else if (
		renderer->bufferUploadBufferOffset + dataLength >= TRANSFER_BUFFER_SIZE
	) {
		if (renderer->bufferUploadCycleCount < MAX_UPLOAD_CYCLE_COUNT)
		{
			/* Cycle transfer buffer if necessary */
			transferCycle = true;
			renderer->bufferUploadCycleCount += 1;
			renderer->bufferUploadBufferOffset = 0;
			transferOffset = 0;
		}
		else
		{
			/* We cycled transfers a lot, send the upload commands to reduce further transfer memory usage */
			SDLGPU_INTERNAL_FlushUploadCommands(renderer);
			transferCycle = true;
			transferOffset = 0;
		}
	}

	dst = (uint8_t*) SDL_MapGPUTransferBuffer(renderer->device, transferBuffer, transferCycle);
	SDL_memcpy(dst + transferOffset, data, dataLength);
	SDL_UnmapGPUTransferBuffer(renderer->device, transferBuffer);

	transferLocation.transfer_buffer = transferBuffer;
	transferLocation.offset = transferOffset;

	bufferRegion.buffer = buffer;
	bufferRegion.offset = dstOffset;
	bufferRegion.size = dataLength;

	SDL_UploadToGPUBuffer(
		renderer->copyPass,
		&transferLocation,
		&bufferRegion,
		cycle
	);

	if (usingTemporaryTransferBuffer)
	{
		SDL_ReleaseGPUTransferBuffer(renderer->device, transferBuffer);
	}
	else
	{
		renderer->bufferUploadBufferOffset += dataLength;
	}

	SDL_UnlockMutex(renderer->copyPassMutex);
}

static void SDLGPU_SetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
) {
	SDLGPU_BufferHandle *bufferHandle = (SDLGPU_BufferHandle*) buffer;

	bool cycle;
	uint32_t dataLen = (uint32_t) elementCount * (uint32_t) vertexStride;

	if (options == FNA3D_SETDATAOPTIONS_DISCARD)
	{
		cycle = true;
	}
	else if (options == FNA3D_SETDATAOPTIONS_NONE && dataLen == bufferHandle->size)
	{
		/* full buffer update can cycle for efficiency */
		cycle = true;
	}
	else
	{
		cycle = false;
	}

	SDLGPU_INTERNAL_SetBufferData(
		(SDLGPU_Renderer*) driverData,
		bufferHandle->buffer,
		(uint32_t) offsetInBytes,
		data,
		elementCount * vertexStride,
		cycle
	);
}

static void SDLGPU_SetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	SDLGPU_BufferHandle *bufferHandle = (SDLGPU_BufferHandle*) buffer;

	bool cycle;
	if (options == FNA3D_SETDATAOPTIONS_DISCARD)
	{
		cycle = true;
	}
	else if (options == FNA3D_SETDATAOPTIONS_NONE && dataLength == bufferHandle->size)
	{
		/* full buffer update can cycle for efficiency */
		cycle = true;
	}
	else
	{
		cycle = false;
	}

	SDLGPU_INTERNAL_SetBufferData(
		(SDLGPU_Renderer*) driverData,
		bufferHandle->buffer,
		(uint32_t) offsetInBytes,
		data,
		dataLength,
		cycle
	);
}

/* Transfer */

static void SDLGPU_INTERNAL_GetTextureData(
	SDLGPU_Renderer *renderer,
	SDL_GPUTexture *texture,
	uint32_t x,
	uint32_t y,
	uint32_t z,
	uint32_t w,
	uint32_t h,
	uint32_t d,
	uint32_t layer,
	uint32_t level,
	void* data,
	uint32_t dataLength
) {
	SDL_GPUTextureRegion region;
	SDL_GPUTextureTransferInfo textureCopyParams;
	SDL_GPUTransferBufferCreateInfo transferBufferCreateInfo;
	uint8_t *src;

	SDL_LockMutex(renderer->copyPassMutex);

	/* Create transfer buffer if necessary */
	if (renderer->textureDownloadBuffer == NULL)
	{
		transferBufferCreateInfo.usage = SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD;
		transferBufferCreateInfo.size = dataLength;
		transferBufferCreateInfo.props = 0;
		renderer->textureDownloadBuffer = SDL_CreateGPUTransferBuffer(
			renderer->device,
			&transferBufferCreateInfo
		);

		renderer->textureDownloadBufferSize = dataLength;
	}
	else if (renderer->textureDownloadBufferSize < dataLength)
	{
		SDL_ReleaseGPUTransferBuffer(
			renderer->device,
			renderer->textureDownloadBuffer
		);

		transferBufferCreateInfo.usage = SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD;
		transferBufferCreateInfo.size = dataLength;
		transferBufferCreateInfo.props = 0;
		renderer->textureDownloadBuffer = SDL_CreateGPUTransferBuffer(
			renderer->device,
			&transferBufferCreateInfo
		);

		renderer->textureDownloadBufferSize = dataLength;
	}

	/* Set up texture download */
	region.texture = texture;
	region.mip_level = level;
	region.layer = layer;
	region.x = x;
	region.y = y;
	region.z = z;
	region.w = w;
	region.h = h;
	region.d = d;

	/* All zeroes, assume tight packing */
	textureCopyParams.transfer_buffer = renderer->textureDownloadBuffer;
	textureCopyParams.offset = 0;
	textureCopyParams.pixels_per_row = 0;
	textureCopyParams.rows_per_layer = 0;

	/* Flush rendering so the target data is up-to-date */
	SDLGPU_INTERNAL_FlushCommands(renderer);

	SDL_DownloadFromGPUTexture(
		renderer->copyPass,
		&region,
		&textureCopyParams
	);

	/* Flush again and stall so the transfer data is up to date */
	SDLGPU_INTERNAL_FlushUploadCommandsAndStall(renderer);

	/* Copy into data pointer */
	src = (uint8_t*) SDL_MapGPUTransferBuffer(renderer->device, renderer->textureDownloadBuffer, false);
	SDL_memcpy(data, src, dataLength);
	SDL_UnmapGPUTransferBuffer(renderer->device, renderer->textureDownloadBuffer);

	SDL_UnlockMutex(renderer->copyPassMutex);
}

static void SDLGPU_INTERNAL_GetBufferData(
	SDLGPU_Renderer *renderer,
	SDL_GPUBuffer *buffer,
	uint32_t offset,
	void *data,
	uint32_t dataLength
) {
	SDL_GPUBufferRegion bufferRegion;
	SDL_GPUTransferBufferLocation transferLocation;
	SDL_GPUTransferBufferCreateInfo transferBufferCreateInfo;
	uint8_t *src;

	SDL_LockMutex(renderer->copyPassMutex);

	/* Create transfer buffer if necessary */
	if (renderer->bufferDownloadBuffer == NULL)
	{
		transferBufferCreateInfo.usage = SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD;
		transferBufferCreateInfo.size = dataLength;
		transferBufferCreateInfo.props = 0;
		renderer->bufferDownloadBuffer = SDL_CreateGPUTransferBuffer(
			renderer->device,
			&transferBufferCreateInfo
		);

		renderer->bufferDownloadBufferSize = dataLength;
	}
	else if (renderer->bufferDownloadBufferSize < dataLength)
	{
		SDL_ReleaseGPUTransferBuffer(
			renderer->device,
			renderer->bufferDownloadBuffer
		);

		transferBufferCreateInfo.usage = SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD;
		transferBufferCreateInfo.size = dataLength;
		transferBufferCreateInfo.props = 0;
		renderer->bufferDownloadBuffer = SDL_CreateGPUTransferBuffer(
			renderer->device,
			&transferBufferCreateInfo
		);

		renderer->bufferDownloadBufferSize = dataLength;
	}

	/* Set up buffer download */
	bufferRegion.buffer = buffer;
	bufferRegion.offset = offset;
	bufferRegion.size = dataLength;
	transferLocation.transfer_buffer = renderer->bufferDownloadBuffer;
	transferLocation.offset = 0;

	SDL_DownloadFromGPUBuffer(
		renderer->copyPass,
		&bufferRegion,
		&transferLocation
	);

	/* Flush and stall so the data is up to date */
	SDLGPU_INTERNAL_FlushUploadCommandsAndStall(renderer);

	/* Copy into data pointer */
	src = (uint8_t*) SDL_MapGPUTransferBuffer(renderer->device, renderer->bufferDownloadBuffer, false);
	SDL_memcpy(data, src, dataLength);
	SDL_UnmapGPUTransferBuffer(renderer->device, renderer->bufferDownloadBuffer);

	SDL_UnlockMutex(renderer->copyPassMutex);
}

static void SDLGPU_GetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
) {
	SDLGPU_BufferHandle *bufferHandle = (SDLGPU_BufferHandle*) buffer;

	SDLGPU_INTERNAL_GetBufferData(
		(SDLGPU_Renderer*) driverData,
		bufferHandle->buffer,
		offsetInBytes,
		data,
		elementCount * vertexStride
	);
}

static void SDLGPU_GetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength
) {
	SDLGPU_BufferHandle *bufferHandle = (SDLGPU_BufferHandle*) buffer;

	SDLGPU_INTERNAL_GetBufferData(
		(SDLGPU_Renderer*) driverData,
		bufferHandle->buffer,
		offsetInBytes,
		data,
		dataLength
	);
}

static void SDLGPU_GetTextureData2D(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;

	SDLGPU_INTERNAL_GetTextureData(
		(SDLGPU_Renderer*) driverData,
		textureHandle->texture,
		(uint32_t) x,
		(uint32_t) y,
		0,
		(uint32_t) w,
		(uint32_t) h,
		1,
		0,
		level,
		data,
		(uint32_t) dataLength
	);
}

static void SDLGPU_GetTextureData3D(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t z,
	int32_t w,
	int32_t h,
	int32_t d,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	FNA3D_LogError(
		"GetTextureData3D is unsupported!"
	);
}

static void SDLGPU_GetTextureDataCube(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	FNA3D_CubeMapFace cubeMapFace,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;

	SDLGPU_INTERNAL_GetTextureData(
		(SDLGPU_Renderer*) driverData,
		textureHandle->texture,
		(uint32_t) x,
		(uint32_t) y,
		0,
		(uint32_t) w,
		(uint32_t) h,
		1,
		(uint32_t) cubeMapFace,
		level,
		data,
		dataLength
	);
}

static void SDLGPU_ReadBackbuffer(
	FNA3D_Renderer *driverData,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	void* data,
	int32_t dataLength
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	SDLGPU_INTERNAL_GetTextureData(
		renderer,
		renderer->fauxBackbufferColorTexture->texture,
		(uint32_t) x,
		(uint32_t) y,
		0,
		(uint32_t) w,
		(uint32_t) h,
		1,
		0,
		0,
		data,
		(uint32_t) dataLength
	);
}

/* Effects */

static void SDLGPU_CreateEffect(
	FNA3D_Renderer *driverData,
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	MOJOSHADER_effectShaderContext shaderBackend;
	SDLGPU_Effect *result;
	int32_t i;

	shaderBackend.shaderContext = renderer->mojoshaderContext;
	shaderBackend.compileShader = (MOJOSHADER_compileShaderFunc) MOJOSHADER_sdlCompileShader;
	shaderBackend.shaderAddRef = (MOJOSHADER_shaderAddRefFunc) MOJOSHADER_sdlShaderAddRef;
	shaderBackend.deleteShader = (MOJOSHADER_deleteShaderFunc) MOJOSHADER_sdlDeleteShader;
	shaderBackend.getParseData = (MOJOSHADER_getParseDataFunc) MOJOSHADER_sdlGetShaderParseData;
	shaderBackend.bindShaders = (MOJOSHADER_bindShadersFunc) MOJOSHADER_sdlBindShaders;
	shaderBackend.getBoundShaders = (MOJOSHADER_getBoundShadersFunc) MOJOSHADER_sdlGetBoundShaderData;
	shaderBackend.mapUniformBufferMemory = (MOJOSHADER_mapUniformBufferMemoryFunc) MOJOSHADER_sdlMapUniformBufferMemory;
	shaderBackend.unmapUniformBufferMemory = (MOJOSHADER_unmapUniformBufferMemoryFunc) MOJOSHADER_sdlUnmapUniformBufferMemory;
	shaderBackend.getError = (MOJOSHADER_getErrorFunc) MOJOSHADER_sdlGetError;
	shaderBackend.m = NULL;
	shaderBackend.f = NULL;
	shaderBackend.malloc_data = NULL;

	*effectData = MOJOSHADER_compileEffect(
		effectCode,
		effectCodeLength,
		NULL,
		0,
		NULL,
		0,
		&shaderBackend
	);

	for (i = 0; i < (*effectData)->error_count; i += 1)
	{
		FNA3D_LogError(
			"MOJOSHADER_compileEffect Error: %s",
			(*effectData)->errors[i].error
		);
	}

	result = (SDLGPU_Effect*) SDL_malloc(sizeof(SDLGPU_Effect));
	result->effect = *effectData;
	*effect = (FNA3D_Effect*) result;
}

static void SDLGPU_CloneEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *cloneSource,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_Effect *sdlCloneSource = (SDLGPU_Effect*) cloneSource;
	SDLGPU_Effect *result;

	*effectData = MOJOSHADER_cloneEffect(sdlCloneSource->effect);
	if (*effectData == NULL)
	{
		FNA3D_LogError(MOJOSHADER_sdlGetError(renderer->mojoshaderContext));
	}

	result = (SDLGPU_Effect*) SDL_malloc(sizeof(SDLGPU_Effect));
	result->effect = *effectData;
	*effect = (FNA3D_Effect*) result;
}

/* TODO: check if we need to defer this */
static void SDLGPU_AddDisposeEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_Effect *gpuEffect = (SDLGPU_Effect*) effect;
	MOJOSHADER_effect *effectData = gpuEffect->effect;

	if (effectData == renderer->currentEffect)
	{
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectEnd(renderer->currentEffect);
		renderer->currentEffect = NULL;
		renderer->currentTechnique = NULL;
		renderer->currentPass = 0;
	}
	MOJOSHADER_deleteEffect(effectData);
	SDL_free(gpuEffect);
}

static void SDLGPU_SetEffectTechnique(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
) {
	SDLGPU_Effect *gpuEffect = (SDLGPU_Effect*) effect;
	MOJOSHADER_effectSetTechnique(gpuEffect->effect, technique);
}

static void SDLGPU_ApplyEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	uint32_t pass,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_Effect *gpuEffect = (SDLGPU_Effect*) effect;
	MOJOSHADER_effect *effectData = gpuEffect->effect;
	const MOJOSHADER_effectTechnique *technique = gpuEffect->effect->current_technique;
	uint32_t numPasses;

	renderer->needFragmentSamplerBind = 1;
	renderer->needVertexSamplerBind = 1;
	renderer->needNewGraphicsPipeline = 1;

	if (effectData == renderer->currentEffect)
	{
		if (
			technique == renderer->currentTechnique &&
			pass == renderer->currentPass
		) {
			MOJOSHADER_effectCommitChanges(
				renderer->currentEffect
			);

			return;
		}

		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectBeginPass(renderer->currentEffect, pass);
		renderer->currentTechnique = technique;
		renderer->currentPass = pass;

		return;
	}
	else if (renderer->currentEffect != NULL)
	{
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectEnd(renderer->currentEffect);
	}

	MOJOSHADER_effectBegin(
		effectData,
		&numPasses,
		0,
		stateChanges
	);

	MOJOSHADER_effectBeginPass(effectData, pass);
	renderer->currentEffect = effectData;
	renderer->currentTechnique = technique;
	renderer->currentPass = pass;
}

static void SDLGPU_BeginPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	MOJOSHADER_effect *effectData = ((SDLGPU_Effect*) effect)->effect;
	uint32_t whatever;

	MOJOSHADER_effectBegin(
			effectData,
			&whatever,
			1,
			stateChanges
	);
	MOJOSHADER_effectBeginPass(effectData, 0);
}

static void SDLGPU_EndPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	MOJOSHADER_effect *effectData = ((SDLGPU_Effect*) effect)->effect;
	MOJOSHADER_effectEndPass(effectData);
	MOJOSHADER_effectEnd(effectData);
}

/* Queries */

static FNA3D_Query* SDLGPU_CreateQuery(FNA3D_Renderer *driverData)
{
	FNA3D_LogError("Occlusion queries are not supported by SDL_GPU!");
	return NULL;
}

static void SDLGPU_AddDisposeQuery(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	FNA3D_LogError("Occlusion queries are not supported by SDL_GPU!");
}

static void SDLGPU_QueryBegin(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	FNA3D_LogError("Occlusion queries are not supported by SDL_GPU!");
}

static void SDLGPU_QueryEnd(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	FNA3D_LogError("Occlusion queries are not supported by SDL_GPU!");
}

static uint8_t SDLGPU_QueryComplete(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	FNA3D_LogError("Occlusion queries are not supported by SDL_GPU!");
	return 0;
}

static int32_t SDLGPU_QueryPixelCount(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	FNA3D_LogError("Occlusion queries are not supported by SDL_GPU!");
	return 0;
}

/* Support Checks */

static uint8_t SDLGPU_SupportsDXT1(FNA3D_Renderer *driverData)
{
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return renderer->supportsDXT1;
}

static uint8_t SDLGPU_SupportsS3TC(FNA3D_Renderer *driverData)
{
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return renderer->supportsBC2 || renderer->supportsBC3;
}

static uint8_t SDLGPU_SupportsBC7(FNA3D_Renderer *driverData)
{
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return renderer->supportsBC7;
}

static uint8_t SDLGPU_SupportsHardwareInstancing(FNA3D_Renderer *driverData)
{
	return 1;
}

static uint8_t SDLGPU_SupportsNoOverwrite(FNA3D_Renderer *driverData)
{
	return 1;
}

static uint8_t SDLGPU_SupportsSRGBRenderTargets(FNA3D_Renderer *driverData)
{
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	return renderer->supportsSRGB;
}

static void SDLGPU_GetMaxTextureSlots(
	FNA3D_Renderer *driverData,
	int32_t *textures,
	int32_t *vertexTextures
) {
	*textures = MAX_TEXTURE_SAMPLERS;
	*vertexTextures = MAX_VERTEXTEXTURE_SAMPLERS;
}

static int32_t SDLGPU_GetMaxMultiSampleCount(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	bool supports2 = SDL_GPUTextureSupportsSampleCount(renderer->device, XNAToSDL_SurfaceFormat[format], SDL_GPU_SAMPLECOUNT_2);
	bool supports4 = SDL_GPUTextureSupportsSampleCount(renderer->device, XNAToSDL_SurfaceFormat[format], SDL_GPU_SAMPLECOUNT_4);
	bool supports8 = SDL_GPUTextureSupportsSampleCount(renderer->device, XNAToSDL_SurfaceFormat[format], SDL_GPU_SAMPLECOUNT_8);

	if (supports8) return SDL_min(multiSampleCount, 8);
	if (supports4) return SDL_min(multiSampleCount, 4);
	if (supports2) return SDL_min(multiSampleCount, 2);
	return 1;
}

/* Debugging */

static void SDLGPU_SetStringMarker(
	FNA3D_Renderer *driverData,
	const char *text
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;

	SDL_InsertGPUDebugLabel(
		renderer->renderCommandBuffer,
		text
	);
}

static void SDLGPU_SetTextureName(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	const char *text
) {
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) driverData;
	SDLGPU_TextureHandle *textureHandle = (SDLGPU_TextureHandle*) texture;

	SDL_SetGPUTextureName(
		renderer->device,
		textureHandle->texture,
		text
	);
}

/* External Interop */

static void SDLGPU_GetSysRenderer(
	FNA3D_Renderer *driverData,
	FNA3D_SysRendererEXT *sysrenderer
) {
	/* TODO */
	SDL_memset(sysrenderer, '\0', sizeof(FNA3D_SysRendererEXT));
	sysrenderer->rendererType = FNA3D_RENDERER_TYPE_SDL_GPU_EXT;
}

static FNA3D_Texture* SDLGPU_CreateSysTexture(
	FNA3D_Renderer *driverData,
	FNA3D_SysTextureEXT *systexture
) {
	/* TODO */
	return NULL;
}

/* Destroy */

static void SDLGPU_DestroyDevice(FNA3D_Device *device)
{
	SDLGPU_Renderer *renderer = (SDLGPU_Renderer*) device->driverData;
	int32_t i, j;

	// Completely flush command buffers and stall
	SDL_LockMutex(renderer->copyPassMutex);

	/* Ignore any pending render passes, they're getting canceled anyway */
	renderer->needNewRenderPass = 0;

	SDLGPU_INTERNAL_FlushCommands(renderer);
	// avoid command buffer leaks by explicitly canceling newly-acquired command buffers
	SDL_CancelGPUCommandBuffer(renderer->uploadCommandBuffer);
	SDL_CancelGPUCommandBuffer(renderer->renderCommandBuffer);
	SDL_WaitForGPUIdle(renderer->device);

	SDL_UnlockMutex(renderer->copyPassMutex);
	SDL_DestroyMutex(renderer->copyPassMutex);

	if (renderer->textureDownloadBuffer != NULL)
	{
		SDL_ReleaseGPUTransferBuffer(
			renderer->device,
			renderer->textureDownloadBuffer
		);
	}

	if (renderer->bufferDownloadBuffer != NULL)
	{
		SDL_ReleaseGPUTransferBuffer(
			renderer->device,
			renderer->bufferDownloadBuffer
		);
	}

	SDL_ReleaseGPUTransferBuffer(renderer->device, renderer->textureUploadBuffer);
	SDL_ReleaseGPUTransferBuffer(renderer->device, renderer->bufferUploadBuffer);

	SDLGPU_INTERNAL_DestroyFauxBackbuffer(renderer);

	for (i = 0; i < NUM_PIPELINE_HASH_BUCKETS; i += 1)
	{
		for (j = 0; j < renderer->graphicsPipelineHashTable.buckets[i].count; j += 1)
		{
			SDL_ReleaseGPUGraphicsPipeline(
				renderer->device,
				renderer->graphicsPipelineHashTable.buckets[i].elements[j].value
			);
		}
	}

	for (i = 0; i < renderer->samplerStateArray.count; i += 1)
	{
		SDL_ReleaseGPUSampler(
			renderer->device,
			renderer->samplerStateArray.elements[i].value
		);
	}

	SDL_ReleaseGPUTexture(
		renderer->device,
		renderer->dummyTexture2D
	);

	SDL_ReleaseGPUTexture(
		renderer->device,
		renderer->dummyTexture3D
	);

	SDL_ReleaseGPUTexture(
		renderer->device,
		renderer->dummyTextureCube
	);

	SDL_ReleaseGPUSampler(
		renderer->device,
		renderer->dummySampler
	);

	MOJOSHADER_sdlDestroyContext(renderer->mojoshaderContext);

#if SDL_PLATFORM_GDK
	SDL_RemoveEventWatch(SDLGPU_INTERNAL_GDKEventFilter, renderer);
#endif

	SDL_DestroyGPUDevice(renderer->device);

	SDL_free(renderer->windows);
	SDL_free(renderer);
	SDL_free(device);
}

/* Initialization */

static SDL_PropertiesID SDLGPU_INTERNAL_FillProperties(bool debugMode)
{
	SDL_PropertiesID props = SDL_CreateProperties();
	SDL_GPUShaderFormat formats = MOJOSHADER_sdlGetShaderFormats();

	SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_DEBUGMODE_BOOLEAN, debugMode);
	SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_PREFERLOWPOWER_BOOLEAN, SDL_GetHintBoolean("FNA3D_PREFER_LOW_POWER", false));

	SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_PRIVATE_BOOLEAN, !!(formats & SDL_GPU_SHADERFORMAT_PRIVATE));
	SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_SPIRV_BOOLEAN, !!(formats & SDL_GPU_SHADERFORMAT_SPIRV));
	SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXIL_BOOLEAN, !!(formats & SDL_GPU_SHADERFORMAT_DXIL));
	SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_MSL_BOOLEAN, !!(formats & SDL_GPU_SHADERFORMAT_MSL));
	SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_METALLIB_BOOLEAN, !!(formats & SDL_GPU_SHADERFORMAT_METALLIB));

	SDL_SetBooleanProperty(props, "SDL.gpu.device.create.feature.clip_distance", false);
	SDL_SetBooleanProperty(props, "SDL.gpu.device.create.feature.depth_clamping", false);
	SDL_SetBooleanProperty(props, "SDL.gpu.device.create.feature.indirect_draw_first_instance", false);
	SDL_SetBooleanProperty(props, "SDL.gpu.device.create.d3d12.allowtier1resourcebinding", true);
	SDL_SetBooleanProperty(props, "SDL.gpu.device.create.vulkan.requirehardwareacceleration", true);
	SDL_SetBooleanProperty(props, "SDL.gpu.device.create.metal.allowmacfamily1", true);

	return props;
}

static uint8_t SDLGPU_PrepareWindowAttributes(uint32_t *flags)
{
	uint8_t result;
	SDL_PropertiesID props;

	/* FIXME: We don't have a good way to ask for debug mode this early... */
	props = SDLGPU_INTERNAL_FillProperties(false);
	result = SDL_GPUSupportsProperties(props);
	SDL_DestroyProperties(props);

	if (!result)
	{
		FNA3D_LogWarn("SDL_GPUSupportsProperties failed: %s", SDL_GetError());
	}
	return result;
}

static FNA3D_Device* SDLGPU_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
) {
	SDLGPU_Renderer *renderer;
	SDL_PropertiesID props;
	SDL_GPUDevice *device;
	SDL_GPUSwapchainComposition swapchainComposition;
	SDL_GPUTextureCreateInfo textureCreateInfo;
	SDL_GPUSamplerCreateInfo samplerCreateInfo;
	SDL_GPUTransferBufferCreateInfo transferBufferCreateInfo;
	SDL_GPUPresentMode desiredPresentMode;
	uint64_t dummyInt = 0;
	FNA3D_Device *result;
	int32_t i;

	SDL_SetLogPriority(
		SDL_LOG_CATEGORY_GPU,
		debugMode ? SDL_LOG_PRIORITY_DEBUG : SDL_LOG_PRIORITY_INFO);

	props = SDLGPU_INTERNAL_FillProperties(debugMode);
	device = SDL_CreateGPUDeviceWithProperties(props);
	SDL_DestroyProperties(props);

	if (device == NULL)
	{
		FNA3D_LogError("Failed to create SDLGPU device: %s", SDL_GetError());
		return NULL;
	}

	result = SDL_malloc(sizeof(FNA3D_Device));
	ASSIGN_DRIVER(SDLGPU)

	renderer = SDL_malloc(sizeof(SDLGPU_Renderer));
	SDL_memset(renderer, '\0', sizeof(SDLGPU_Renderer));

	renderer->device = device;
	renderer->copyPassMutex = SDL_CreateMutex();

	result->driverData = (FNA3D_Renderer*) renderer;

	swapchainComposition = SDL_GPU_SWAPCHAINCOMPOSITION_SDR;

	if (SDL_GetHintBoolean("FNA3D_ENABLE_HDR_COLORSPACE", false))
	{
		if (presentationParameters->backBufferFormat == FNA3D_SURFACEFORMAT_RGBA1010102)
		{
			swapchainComposition = SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084;
		}
		else if (	presentationParameters->backBufferFormat == FNA3D_SURFACEFORMAT_HALFVECTOR4 ||
				presentationParameters->backBufferFormat == FNA3D_SURFACEFORMAT_HDRBLENDABLE	)
		{
			swapchainComposition = SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR;
		}
	}

	if (!SDLGPU_INTERNAL_ClaimWindow(
		renderer,
		(SDL_Window*) presentationParameters->deviceWindowHandle
	)) {
		SDL_free(renderer);
		SDL_free(result);
		return NULL;
	}

	if (!XNAToSDL_PresentMode(
		renderer->device,
		presentationParameters->deviceWindowHandle,
		presentationParameters->presentationInterval,
		&desiredPresentMode
	)) {
		FNA3D_LogError("Failed to set suitable present mode!");
		SDL_free(renderer);
		SDL_free(result);
		return NULL;
	}

	if (!SDL_SetGPUSwapchainParameters(
		renderer->device,
		presentationParameters->deviceWindowHandle,
		swapchainComposition,
		desiredPresentMode
	)) {
		FNA3D_LogError("Failed to set up swapchain!");
		SDL_free(renderer);
		SDL_free(result);
		return NULL;
	}

	SDLGPU_INTERNAL_CreateFauxBackbuffer(
		renderer,
		presentationParameters
	);

	if (renderer->fauxBackbufferColorTexture == NULL)
	{
		FNA3D_LogError("Failed to create faux backbuffer!");
		SDL_free(renderer);
		SDL_free(result);
		return NULL;
	}

	transferBufferCreateInfo.usage = SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
	transferBufferCreateInfo.size = TRANSFER_BUFFER_SIZE;
	transferBufferCreateInfo.props = 0;
	renderer->textureUploadBufferOffset = 0;
	renderer->textureUploadBuffer = SDL_CreateGPUTransferBuffer(
		renderer->device,
		&transferBufferCreateInfo
	);

	if (renderer->textureUploadBuffer == NULL)
	{
		FNA3D_LogError("Failed to create texture transfer buffer!");
		SDL_free(renderer);
		SDL_free(result);
		return NULL;
	}

	transferBufferCreateInfo.usage = SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD;
	transferBufferCreateInfo.size = TRANSFER_BUFFER_SIZE;
	transferBufferCreateInfo.props = 0;
	renderer->bufferUploadBufferOffset = 0;
	renderer->bufferUploadBuffer = SDL_CreateGPUTransferBuffer(
		renderer->device,
		&transferBufferCreateInfo
	);

	/*
	 * Initialize renderer members not covered by SDL_memset('\0')
	 */

	renderer->multisampleMask = 0xFFFFFFFF;
	renderer->nextPipelineHash.sampleMask = renderer->multisampleMask;

	for (i = 0; i < MAX_BOUND_VERTEX_BUFFERS; i += 1)
	{
		renderer->vertexBindings[i].vertexDeclaration.elements =
			renderer->vertexElements[i];
	}

	renderer->mojoshaderContext = MOJOSHADER_sdlCreateContext(
		device,
		NULL,
		NULL,
		NULL
	);
	if (renderer->mojoshaderContext == NULL)
	{
		FNA3D_LogError("Could not create MojoShader context: %s", MOJOSHADER_sdlGetError(NULL));
		SDL_free(renderer);
		SDL_free(result);
		return NULL;
	}

	/* Determine capabilities */

	renderer->supportsDXT1 = SDL_GPUTextureSupportsFormat(
		renderer->device,
		SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM,
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTUREUSAGE_SAMPLER
	);
	renderer->supportsBC2 = SDL_GPUTextureSupportsFormat(
		renderer->device,
		SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM,
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTUREUSAGE_SAMPLER
	);
	renderer->supportsBC3 = SDL_GPUTextureSupportsFormat(
		renderer->device,
		SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM,
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTUREUSAGE_SAMPLER
	);
	renderer->supportsBC7 = SDL_GPUTextureSupportsFormat(
		renderer->device,
		SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM,
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTUREUSAGE_SAMPLER
	);
	renderer->supportsSRGB = SDL_GPUTextureSupportsFormat(
		renderer->device,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB,
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | SDL_GPU_TEXTUREUSAGE_SAMPLER
	);
	renderer->supportsD24 = SDL_GPUTextureSupportsFormat(
		renderer->device,
		SDL_GPU_TEXTUREFORMAT_D24_UNORM,
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET
	);
	renderer->supportsD24S8 = SDL_GPUTextureSupportsFormat(
		renderer->device,
		SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET
	);
	renderer->supportsBaseVertex = 1; /* FIXME: moltenVK fix */

	/* Set up dummy resources */

	textureCreateInfo.width = 1;
	textureCreateInfo.height = 1;
	textureCreateInfo.layer_count_or_depth = 1;
	textureCreateInfo.format = SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
	textureCreateInfo.type = SDL_GPU_TEXTURETYPE_2D;
	textureCreateInfo.num_levels = 1;
	textureCreateInfo.usage = SDL_GPU_TEXTUREUSAGE_SAMPLER;
	textureCreateInfo.sample_count = SDL_GPU_SAMPLECOUNT_1;
	textureCreateInfo.props = 0;

	renderer->dummyTexture2D = SDL_CreateGPUTexture(
		renderer->device,
		&textureCreateInfo
	);

	textureCreateInfo.layer_count_or_depth = 2;
	textureCreateInfo.type = SDL_GPU_TEXTURETYPE_3D;

	renderer->dummyTexture3D = SDL_CreateGPUTexture(
		renderer->device,
		&textureCreateInfo
	);

	textureCreateInfo.layer_count_or_depth = 6;
	textureCreateInfo.type = SDL_GPU_TEXTURETYPE_CUBE;

	renderer->dummyTextureCube = SDL_CreateGPUTexture(
		renderer->device,
		&textureCreateInfo
	);

	samplerCreateInfo.address_mode_u = SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
	samplerCreateInfo.address_mode_v = SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
	samplerCreateInfo.address_mode_w = SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
	samplerCreateInfo.enable_anisotropy = 0;
	samplerCreateInfo.max_anisotropy = 0;
	samplerCreateInfo.enable_compare = 0;
	samplerCreateInfo.compare_op = SDL_GPU_COMPAREOP_NEVER;
	samplerCreateInfo.mag_filter = SDL_GPU_FILTER_NEAREST;
	samplerCreateInfo.min_filter = SDL_GPU_FILTER_NEAREST;
	samplerCreateInfo.mipmap_mode = SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;
	samplerCreateInfo.mip_lod_bias = 0;
	samplerCreateInfo.min_lod = 1;
	samplerCreateInfo.max_lod = 1;
	samplerCreateInfo.props = 0;

	renderer->dummySampler = SDL_CreateGPUSampler(
		renderer->device,
		&samplerCreateInfo
	);

	for (i = 0; i < MAX_VERTEXTEXTURE_SAMPLERS; i += 1)
	{
		renderer->vertexTextureSamplerBindings[i].texture = renderer->dummyTexture2D;
		renderer->vertexTextureSamplerBindings[i].sampler = renderer->dummySampler;
	}

	for (i = 0; i < MAX_TEXTURE_SAMPLERS; i += 1)
	{
		renderer->fragmentTextureSamplerBindings[i].texture = renderer->dummyTexture2D;
		renderer->fragmentTextureSamplerBindings[i].sampler = renderer->dummySampler;
	}

	renderer->boundRenderTargetCapacity = 4;
	renderer->boundRenderTargetCount = 0;
	renderer->boundRenderTargets = SDL_malloc(
		renderer->boundRenderTargetCapacity * sizeof(SDLGPU_TextureHandle*)
	);

	/* Acquire command buffer, we are ready for takeoff */
	SDLGPU_ResetCommandBufferState(renderer);

	/* Enqueue dummy uploads */

	SDLGPU_INTERNAL_SetTextureData(
		renderer,
		renderer->dummyTexture2D,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
		0,
		0,
		0,
		1,
		1,
		1,
		0,
		0,
		&dummyInt,
		sizeof(uint32_t),
		0 /* cycleTexture */
	);

	SDLGPU_INTERNAL_SetTextureData(
		renderer,
		renderer->dummyTexture3D,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
		0,
		0,
		0,
		1,
		1,
		2,
		0,
		0,
		&dummyInt,
		sizeof(uint64_t),
		0 /* cycleTexture */
	);

	for (i = 0; i < 6; i += 1)
	{
		SDLGPU_INTERNAL_SetTextureData(
			renderer,
			renderer->dummyTextureCube,
			SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
			0, 0, 0,
			1, 1, 1,
			i,
			0,
			&dummyInt,
			sizeof(uint32_t),
			0 /* cycleTexture */
		);
	}

#if SDL_PLATFORM_GDK
	SDL_AddEventWatch(SDLGPU_INTERNAL_GDKEventFilter, renderer);
#endif

	return result;
}

/* Driver struct */

FNA3D_Driver SDLGPUDriver = {
	"SDLGPU",
	SDLGPU_PrepareWindowAttributes,
	SDLGPU_CreateDevice
};

#else

extern int this_tu_is_empty;

#endif /* FNA3D_DRIVER_SDL */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
