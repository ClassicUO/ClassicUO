/* FNA3D - 3D Graphics Library for FNA
 *
 * Copyright (c) 2020-2021 Ethan Lee
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

#if FNA3D_DRIVER_METAL

#include "FNA3D_Driver_Metal.h"
#include "FNA3D_PipelineCache.h"

/* Internal Structures */

typedef struct MetalTexture /* Cast from FNA3D_Texture* */
{
	MTLTexture *handle;
	uint8_t hasMipmaps;
	int32_t width;
	int32_t height;
	uint8_t isPrivate;
	uint8_t bound;
	FNA3D_SurfaceFormat format;
	struct MetalTexture *next; /* FIXME: Remove this */
} MetalTexture;

static MetalTexture NullTexture =
{
	NULL,
	0,
	0,
	0,
	0,
	0,
	FNA3D_SURFACEFORMAT_COLOR,
	NULL
};

typedef struct MetalSubBuffer
{
	MTLBuffer *buffer;
	uint8_t *ptr;
	int32_t offset;
} MetalSubBuffer;

typedef struct MetalBuffer /* Cast from FNA3D_Buffer* */
{
	int32_t size;
	int32_t subBufferCount;
	int32_t subBufferCapacity;
	int32_t currentSubBufferIndex;
	uint8_t bound;
	MetalSubBuffer *subBuffers;
} MetalBuffer;

typedef struct MetalBufferAllocator
{
	#define PHYSICAL_BUFFER_BASE_SIZE 4000000
	#define PHYSICAL_BUFFER_MAX_COUNT 7
	MTLBuffer *physicalBuffers[PHYSICAL_BUFFER_MAX_COUNT];
	int32_t totalAllocated[PHYSICAL_BUFFER_MAX_COUNT];
} MetalBufferAllocator;

typedef struct MetalRenderbuffer /* Cast from FNA3D_Renderbuffer* */
{
	MTLTexture *handle;
	MTLTexture *multiSampleHandle;
	MTLPixelFormat pixelFormat;
	int32_t multiSampleCount;
} MetalRenderbuffer;

typedef struct MetalEffect /* Cast from FNA3D_Effect* */
{
	MOJOSHADER_effect *effect;
	void *library; /* MTLLibrary */
} MetalEffect;

typedef struct MetalQuery /* Cast from FNA3D_Query* */
{
	MTLBuffer *handle;
} MetalQuery;

typedef struct MetalBackbuffer
{
	int32_t width;
	int32_t height;
	FNA3D_SurfaceFormat surfaceFormat;
	FNA3D_DepthFormat depthFormat;
	int32_t multiSampleCount;
	uint8_t preserveDepthStencil;

	MTLTexture *colorBuffer;
	MTLTexture *multiSampleColorBuffer;
	MTLTexture *depthStencilBuffer;
} MetalBackbuffer;

typedef struct PackedRenderPipeline
{
	MOJOSHADER_mtlShader *vshader, *pshader;
	PackedState blendState;
	MTLPixelFormat colorFormats[4];
	MTLPixelFormat depthFormat;
	MTLVertexDescriptor *vertexDescriptor;
	int32_t sampleCount;
} PackedRenderPipeline;

typedef struct PackedRenderPipelineMap
{
	PackedRenderPipeline key;
	MTLRenderPipelineState *value;
} PackedRenderPipelineMap;

typedef struct PackedRenderPipelineArray
{
	PackedRenderPipelineMap *elements;
	int32_t count;
	int32_t capacity;
} PackedRenderPipelineArray;

typedef struct MetalRenderer /* Cast from FNA3D_Renderer* */
{
	/* Associated FNA3D_Device */
	FNA3D_Device *parentDevice;

	/* The Faux-Backbuffer */
	MetalBackbuffer *backbuffer;
	uint8_t backbufferSizeChanged;
	FNA3D_Rect prevSrcRect;
	FNA3D_Rect prevDstRect;
	MTLBuffer *backbufferDrawBuffer;
	MTLSamplerState *backbufferSamplerState;
	MTLRenderPipelineState *backbufferPipeline;

	/* Capabilities */
	uint8_t isMac;
	uint8_t supportsS3tc;
	uint8_t supportsDxt1;
	uint8_t supportsOcclusionQueries;
	uint8_t maxMultiSampleCount;

	/* Basic Metal Objects */
	SDL_MetalView view;
	CAMetalLayer *layer;
	MTLDevice *device;
	MTLCommandQueue *queue;

	/* Active Metal State */
	MTLCommandBuffer *commandBuffer;
	MTLCommandBuffer *committedCommandBuffer;
	MTLRenderCommandEncoder *renderCommandEncoder;
	MTLBuffer *currentVisibilityBuffer;
	MTLVertexDescriptor *currentVertexDescriptor;
	uint8_t needNewRenderPass;
	uint8_t frameInProgress;

	/* Autorelease Pool */
	NSAutoreleasePool *pool;

	/* Blend State */
	FNA3D_Color blendColor;
	int32_t multiSampleMask;
	FNA3D_BlendState blendState;
	MTLRenderPipelineState *ldPipelineState;

	/* Stencil State */
	int32_t stencilRef;

	/* Rasterizer State */
	uint8_t scissorTestEnable;
	FNA3D_CullMode cullFrontFace;
	FNA3D_FillMode fillMode;
	float depthBias;
	float slopeScaleDepthBias;
	uint8_t multiSampleEnable;

	/* Viewport State */
	FNA3D_Viewport viewport;
	FNA3D_Rect scissorRect;
	int32_t currentAttachmentWidth;
	int32_t currentAttachmentHeight;

	/* Textures */
	MetalTexture *textures[MAX_TOTAL_SAMPLERS];
	MTLSamplerState *samplers[MAX_TOTAL_SAMPLERS];
	uint8_t textureNeedsUpdate[MAX_TOTAL_SAMPLERS];
	uint8_t samplerNeedsUpdate[MAX_TOTAL_SAMPLERS];
	MetalTexture *transientTextures;
	MetalTexture **texturesInUse;
	int32_t numTexturesInUse;
	int32_t maxTexturesInUse;

	/* Depth Stencil State */
	FNA3D_DepthStencilState depthStencilState;
	MTLDepthStencilState *defaultDepthStencilState;
	MTLDepthStencilState *ldDepthStencilState;
	MTLPixelFormat D16Format;
	MTLPixelFormat D24Format;
	MTLPixelFormat D24S8Format;

	/* Buffers */
	MetalBufferAllocator *bufferAllocator;
	MetalBuffer **buffersInUse;
	int32_t numBuffersInUse;
	int32_t maxBuffersInUse;
	MTLBuffer *ldUniformBuffer;
	int32_t ldVertUniformOffset;
	int32_t ldFragUniformOffset;
	MTLBuffer *ldVertexBuffers[MAX_BOUND_VERTEX_BUFFERS];
	int32_t ldVertexBufferOffsets[MAX_BOUND_VERTEX_BUFFERS];

	/* Render Targets */
	MTLTexture *currentAttachments[MAX_RENDERTARGET_BINDINGS];
	MTLPixelFormat currentColorFormats[MAX_RENDERTARGET_BINDINGS];
	MTLTexture *currentMSAttachments[MAX_RENDERTARGET_BINDINGS];
	FNA3D_CubeMapFace currentAttachmentSlices[MAX_RENDERTARGET_BINDINGS];
	MTLTexture *currentDepthStencilBuffer;
	FNA3D_DepthFormat currentDepthFormat;
	int32_t currentSampleCount;
	uint8_t preserveDepthStencil;

	/* Clear Cache */
	FNA3D_Vec4 clearColor;
	float clearDepth;
	int32_t clearStencil;
	uint8_t shouldClearColor;
	uint8_t shouldClearDepth;
	uint8_t shouldClearStencil;

	/* Pipeline State Object Caches */
	PackedRenderPipelineArray pipelineStateCache;
	PackedStateArray depthStencilStateCache;
	PackedStateArray samplerStateCache;
	PackedVertexBufferBindingsArray vertexDescriptorCache;

	/* MojoShader Interop */
	MOJOSHADER_mtlContext *mtlContext;
	MOJOSHADER_effect *currentEffect;
	const MOJOSHADER_effectTechnique *currentTechnique;
	uint32_t currentPass;
} MetalRenderer;

/* XNA->Metal Translation Arrays */

static MTLPixelFormat XNAToMTL_TextureFormat[] =
{
	MTLPixelFormatRGBA8Unorm,	/* SurfaceFormat.Color */
#if defined(__IPHONEOS__) || defined(__TVOS__)
	MTLPixelFormatB5G6R5Unorm,	/* SurfaceFormat.Bgr565 */
	MTLPixelFormatBGR5A1Unorm,	/* SurfaceFormat.Bgra5551 */
	MTLPixelFormatABGR4Unorm,	/* SurfaceFormat.Bgra4444 */
#else
	MTLPixelFormatBGRA8Unorm,	/* SurfaceFormat.Bgr565 */
	MTLPixelFormatBGRA8Unorm,	/* SurfaceFormat.Bgra5551 */
	MTLPixelFormatBGRA8Unorm,	/* SurfaceFormat.Bgra4444 */
#endif
	MTLPixelFormatBC1RGBA,		/* SurfaceFormat.Dxt1 */
	MTLPixelFormatBC2RGBA,		/* SurfaceFormat.Dxt3 */
	MTLPixelFormatBC3RGBA,		/* SurfaceFormat.Dxt5 */
	MTLPixelFormatRG8Snorm, 	/* SurfaceFormat.NormalizedByte2 */
	MTLPixelFormatRG16Snorm,	/* SurfaceFormat.NormalizedByte4 */
	MTLPixelFormatRGB10A2Unorm,	/* SurfaceFormat.Rgba1010102 */
	MTLPixelFormatRG16Unorm,	/* SurfaceFormat.Rg32 */
	MTLPixelFormatRGBA16Unorm,	/* SurfaceFormat.Rgba64 */
	MTLPixelFormatA8Unorm,		/* SurfaceFormat.Alpha8 */
	MTLPixelFormatR32Float,		/* SurfaceFormat.Single */
	MTLPixelFormatRG32Float,	/* SurfaceFormat.Vector2 */
	MTLPixelFormatRGBA32Float,	/* SurfaceFormat.Vector4 */
	MTLPixelFormatR16Float,		/* SurfaceFormat.HalfSingle */
	MTLPixelFormatRG16Float,	/* SurfaceFormat.HalfVector2 */
	MTLPixelFormatRGBA16Float,	/* SurfaceFormat.HalfVector4 */
	MTLPixelFormatRGBA16Float,	/* SurfaceFormat.HdrBlendable */
	MTLPixelFormatBGRA8Unorm,	/* SurfaceFormat.ColorBgraEXT */
};

static MTLPixelFormat XNAToMTL_DepthFormat(
	MetalRenderer *renderer,
	FNA3D_DepthFormat format
) {
	switch (format)
	{
		case FNA3D_DEPTHFORMAT_D16:	return renderer->D16Format;
		case FNA3D_DEPTHFORMAT_D24:	return renderer->D24Format;
		case FNA3D_DEPTHFORMAT_D24S8:	return renderer->D24S8Format;
		case FNA3D_DEPTHFORMAT_NONE:	return MTLPixelFormatInvalid;
	}
}

static MTLVertexFormat XNAToMTL_VertexAttribType[] =
{
	MTLVertexFormatFloat,			/* VertexElementFormat.Single */
	MTLVertexFormatFloat2,			/* VertexElementFormat.Vector2 */
	MTLVertexFormatFloat3,			/* VertexElementFormat.Vector3 */
	MTLVertexFormatFloat4,			/* VertexElementFormat.Vector4 */
	MTLVertexFormatUChar4Normalized,	/* VertexElementFormat.Color */
	MTLVertexFormatUChar4,			/* VertexElementFormat.Byte4 */
	MTLVertexFormatShort2,			/* VertexElementFormat.Short2 */
	MTLVertexFormatShort4,			/* VertexElementFormat.Short4 */
	MTLVertexFormatShort2Normalized,	/* VertexElementFormat.NormalizedShort2 */
	MTLVertexFormatShort4Normalized,	/* VertexElementFormat.NormalizedShort4 */
	MTLVertexFormatHalf2,			/* VertexElementFormat.HalfVector2 */
	MTLVertexFormatHalf4			/* VertexElementFormat.HalfVector4 */
};

static MTLIndexType XNAToMTL_IndexType[] =
{
	MTLIndexTypeUInt16,	/* IndexElementSize.SixteenBits */
	MTLIndexTypeUInt32	/* IndexElementSize.ThirtyTwoBits */
};

static MTLBlendFactor XNAToMTL_BlendMode[] =
{
	MTLBlendFactorOne,			/* Blend.One */
	MTLBlendFactorZero,			/* Blend.Zero */
	MTLBlendFactorSourceColor,		/* Blend.SourceColor */
	MTLBlendFactorOneMinusSourceColor,	/* Blend.InverseSourceColor */
	MTLBlendFactorSourceAlpha,		/* Blend.SourceAlpha */
	MTLBlendFactorOneMinusSourceAlpha,	/* Blend.InverseSourceAlpha */
	MTLBlendFactorDestinationColor, 	/* Blend.DestinationColor */
	MTLBlendFactorOneMinusDestinationColor,	/* Blend.InverseDestinationColor */
	MTLBlendFactorDestinationAlpha, 	/* Blend.DestinationAlpha */
	MTLBlendFactorOneMinusDestinationAlpha,	/* Blend.InverseDestinationAlpha */
	MTLBlendFactorBlendColor,		/* Blend.BlendFactor */
	MTLBlendFactorOneMinusBlendColor,	/* Blend.InverseBlendFactor */
	MTLBlendFactorSourceAlphaSaturated	/* Blend.SourceAlphaSaturation */
};

static MTLBlendOperation XNAToMTL_BlendOperation[] =
{
	MTLBlendOperationAdd,			/* BlendFunction.Add */
	MTLBlendOperationSubtract,		/* BlendFunction.Subtract */
	MTLBlendOperationReverseSubtract,	/* BlendFunction.ReverseSubtract */
	MTLBlendOperationMax,			/* BlendFunction.Max */
	MTLBlendOperationMin			/* BlendFunction.Min */
};

static int32_t XNAToMTL_ColorWriteMask(FNA3D_ColorWriteChannels channels)
{
	if (channels == FNA3D_COLORWRITECHANNELS_NONE)
	{
		return 0x0;
	}
	if (channels == FNA3D_COLORWRITECHANNELS_ALL)
	{
		return 0xf;
	}

	int ret = 0;
	if ((channels & FNA3D_COLORWRITECHANNELS_RED) != 0)
	{
		ret |= (0x1 << 3);
	}
	if ((channels & FNA3D_COLORWRITECHANNELS_GREEN) != 0)
	{
		ret |= (0x1 << 2);
	}
	if ((channels & FNA3D_COLORWRITECHANNELS_BLUE) != 0)
	{
		ret |= (0x1 << 1);
	}
	if ((channels & FNA3D_COLORWRITECHANNELS_ALPHA) != 0)
	{
		ret |= (0x1 << 0);
	}
	return ret;
}

static MTLCompareFunction XNAToMTL_CompareFunc[] =
{
	MTLCompareFunctionAlways,	/* CompareFunction.Always */
	MTLCompareFunctionNever,	/* CompareFunction.Never */
	MTLCompareFunctionLess, 	/* CompareFunction.Less */
	MTLCompareFunctionLessEqual,	/* CompareFunction.LessEqual */
	MTLCompareFunctionEqual,	/* CompareFunction.Equal */
	MTLCompareFunctionGreaterEqual,	/* CompareFunction.GreaterEqual */
	MTLCompareFunctionGreater,	/* CompareFunction.Greater */
	MTLCompareFunctionNotEqual	/* CompareFunction.NotEqual */
};

static MTLStencilOperation XNAToMTL_StencilOp[] =
{
	MTLStencilOperationKeep,		/* StencilOperation.Keep */
	MTLStencilOperationZero,		/* StencilOperation.Zero */
	MTLStencilOperationReplace,		/* StencilOperation.Replace */
	MTLStencilOperationIncrementWrap,	/* StencilOperation.Increment */
	MTLStencilOperationDecrementWrap,	/* StencilOperation.Decrement */
	MTLStencilOperationIncrementClamp,	/* StencilOperation.IncrementSaturation */
	MTLStencilOperationDecrementClamp,	/* StencilOperation.DecrementSaturation */
	MTLStencilOperationInvert		/* StencilOperation.Invert */
};

static MTLTriangleFillMode XNAToMTL_FillMode[] =
{
	MTLTriangleFillModeFill,	/* FillMode.Solid */
	MTLTriangleFillModeLines,	/* FillMode.WireFrame */
};

static float XNAToMTL_DepthBiasScale(MTLPixelFormat format)
{
	switch (format)
	{
		case MTLPixelFormatDepth16Unorm:
			return (float) ((1 << 16) - 1);

		case MTLPixelFormatDepth24UnormStencil8:
			return (float) ((1 << 24) - 1);

		case MTLPixelFormatDepth32Float:
		case MTLPixelFormatDepth32FloatStencil8:
			return (float) ((1 << 23) - 1);

		default:
			return 0.0f;
	}

	SDL_assert(0 && "Invalid depth pixel format!");
}

static MTLCullMode XNAToMTL_CullingEnabled[] =
{
	MTLCullModeNone,		/* CullMode.None */
	MTLCullModeFront,		/* CullMode.CullClockwiseFace */
	MTLCullModeBack 		/* CullMode.CullCounterClockwiseFace */
};

static MTLSamplerAddressMode XNAToMTL_Wrap[] =
{
	MTLSamplerAddressModeRepeat,		/* TextureAddressMode.Wrap */
	MTLSamplerAddressModeClampToEdge,	/* TextureAddressMode.Clamp */
	MTLSamplerAddressModeMirrorRepeat	/* TextureAddressMode.Mirror */
};

static MTLSamplerMinMagFilter XNAToMTL_MagFilter[] =
{
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.Linear */
	MTLSamplerMinMagFilterNearest,	/* TextureFilter.Point */
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.Anisotropic */
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.LinearMipPoint */
	MTLSamplerMinMagFilterNearest,	/* TextureFilter.PointMipLinear */
	MTLSamplerMinMagFilterNearest,	/* TextureFilter.MinLinearMagPointMipLinear */
	MTLSamplerMinMagFilterNearest,	/* TextureFilter.MinLinearMagPointMipPoint */
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.MinPointMagLinearMipLinear */
	MTLSamplerMinMagFilterLinear	/* TextureFilter.MinPointMagLinearMipPoint */
};

static MTLSamplerMipFilter XNAToMTL_MipFilter[] =
{
	MTLSamplerMipFilterLinear,	/* TextureFilter.Linear */
	MTLSamplerMipFilterNearest,	/* TextureFilter.Point */
	MTLSamplerMipFilterLinear,	/* TextureFilter.Anisotropic */
	MTLSamplerMipFilterNearest,	/* TextureFilter.LinearMipPoint */
	MTLSamplerMipFilterLinear,	/* TextureFilter.PointMipLinear */
	MTLSamplerMipFilterLinear,	/* TextureFilter.MinLinearMagPointMipLinear */
	MTLSamplerMipFilterNearest,	/* TextureFilter.MinLinearMagPointMipPoint */
	MTLSamplerMipFilterLinear,	/* TextureFilter.MinPointMagLinearMipLinear */
	MTLSamplerMipFilterNearest	/* TextureFilter.MinPointMagLinearMipPoint */
};

static MTLSamplerMinMagFilter XNAToMTL_MinFilter[] =
{
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.Linear */
	MTLSamplerMinMagFilterNearest,	/* TextureFilter.Point */
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.Anisotropic */
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.LinearMipPoint */
	MTLSamplerMinMagFilterNearest,	/* TextureFilter.PointMipLinear */
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.MinLinearMagPointMipLinear */
	MTLSamplerMinMagFilterLinear,	/* TextureFilter.MinLinearMagPointMipPoint */
	MTLSamplerMinMagFilterNearest,	/* TextureFilter.MinPointMagLinearMipLinear */
	MTLSamplerMinMagFilterNearest	/* TextureFilter.MinPointMagLinearMipPoint */
};

static MTLPrimitiveType XNAToMTL_Primitive[] =
{
	MTLPrimitiveTypeTriangle,	/* PrimitiveType.TriangleList */
	MTLPrimitiveTypeTriangleStrip,	/* PrimitiveType.TriangleStrip */
	MTLPrimitiveTypeLine,		/* PrimitiveType.LineList */
	MTLPrimitiveTypeLineStrip,	/* PrimitiveType.LineStrip */
	MTLPrimitiveTypePoint		/* PrimitiveType.PointListEXT */
};

/* Texture Helper Functions */

static inline int32_t METAL_INTERNAL_ClosestMSAAPower(int32_t value)
{
	/* Checking for the highest power of two _after_ than the given int:
	 * http://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
	 * Take result, divide by 2, get the highest power of two _before_!
	 * -flibit
	 */
	if (value == 1)
	{
		/* ... Except for 1, which is invalid for MSAA -flibit */
		return 0;
	}
	int result = value - 1;
	result |= result >> 1;
	result |= result >> 2;
	result |= result >> 4;
	result |= result >> 8;
	result |= result >> 16;
	result += 1;
	if (result == value)
	{
		return result;
	}
	return result >> 1;
}

static inline int32_t METAL_INTERNAL_GetCompatibleSampleCount(
	MetalRenderer *renderer,
	int32_t sampleCount
) {
	/* If the device does not support the requested
	 * multisample count, halve it until we find a
	 * value that is supported.
	 */
	while (	sampleCount > 0 &&
		!mtlDeviceSupportsSampleCount(renderer->device, sampleCount))
	{
		sampleCount = METAL_INTERNAL_ClosestMSAAPower(sampleCount / 2);
	}
	return sampleCount;
}

static MetalTexture* CreateTexture(
	MetalRenderer *renderer,
	MTLTexture *texture,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	MetalTexture *result = SDL_malloc(sizeof(MetalTexture));
	result->handle = texture;
	result->width = width;
	result->height = height;
	result->format = format;
	result->hasMipmaps = levelCount > 1;
	result->isPrivate = isRenderTarget;
	result->bound = 0;
	result->next = NULL;
	return result;
}

/* Render Command Encoder Functions */

static void METAL_INTERNAL_SetEncoderStencilReferenceValue(
	MetalRenderer *renderer
) {
	if (	renderer->renderCommandEncoder != NULL &&
		!renderer->needNewRenderPass			)
	{
		mtlSetStencilReferenceValue(
			renderer->renderCommandEncoder,
			renderer->stencilRef
		);
	}
}

static void METAL_INTERNAL_SetEncoderBlendColor(MetalRenderer *renderer)
{
	if (	renderer->renderCommandEncoder != NULL &&
		!renderer->needNewRenderPass			)
	{
		mtlSetBlendColor(
			renderer->renderCommandEncoder,
			renderer->blendColor.r / 255.0f,
			renderer->blendColor.g / 255.0f,
			renderer->blendColor.b / 255.0f,
			renderer->blendColor.a / 255.0f
		);
	}
}

static void METAL_INTERNAL_SetEncoderViewport(MetalRenderer *renderer)
{
	if (	renderer->renderCommandEncoder != NULL &&
		!renderer->needNewRenderPass			)
	{
		mtlSetViewport(
			renderer->renderCommandEncoder,
			renderer->viewport.x,
			renderer->viewport.y,
			renderer->viewport.w,
			renderer->viewport.h,
			(double) renderer->viewport.minDepth,
			(double) renderer->viewport.maxDepth
		);
	}
}

static void METAL_INTERNAL_SetEncoderScissorRect(MetalRenderer *renderer)
{
	FNA3D_Rect rect = renderer->scissorRect;
	int32_t x0, y0, x1, y1;

	if (	renderer->renderCommandEncoder != NULL &&
		!renderer->needNewRenderPass			)
	{
		if (!renderer->scissorTestEnable)
		{
			/* Default to the size of the current RT */
			x0 = 0;
			y0 = 0;
			x1 = renderer->currentAttachmentWidth;
			y1 = renderer->currentAttachmentHeight;
		}
		else
		{
			/* Keep the rect within the RT dimensions */
			x0 = SDL_max(0, rect.x);
			y0 = SDL_max(0, rect.y);
			x1 = SDL_min(rect.x + rect.w, renderer->currentAttachmentWidth - 1);
			y1 = SDL_min(rect.y + rect.h, renderer->currentAttachmentHeight - 1);
		}

		mtlSetScissorRect(
			renderer->renderCommandEncoder,
			x0,
			y0,
			x1 - x0,
			y1 - y0
		);
	}
}

static void METAL_INTERNAL_SetEncoderCullMode(MetalRenderer *renderer)
{
	if (	renderer->renderCommandEncoder != NULL &&
		!renderer->needNewRenderPass			)
	{
		mtlSetCullMode(
			renderer->renderCommandEncoder,
			XNAToMTL_CullingEnabled[renderer->cullFrontFace]
		);
	}
}

static void METAL_INTERNAL_SetEncoderFillMode(MetalRenderer *renderer)
{
	if (	renderer->renderCommandEncoder != NULL &&
		!renderer->needNewRenderPass			)
	{
		mtlSetTriangleFillMode(
			renderer->renderCommandEncoder,
			XNAToMTL_FillMode[renderer->fillMode]
		);
	}
}

static void METAL_INTERNAL_SetEncoderDepthBias(MetalRenderer *renderer)
{
	if (	renderer->renderCommandEncoder != NULL &&
		!renderer->needNewRenderPass			)
	{
		mtlSetDepthBias(
			renderer->renderCommandEncoder,
			renderer->depthBias,
			renderer->slopeScaleDepthBias,
			0.0f /* no clamp */
		);
	}
}

static void METAL_INTERNAL_EndPass(MetalRenderer *renderer)
{
	if (renderer->renderCommandEncoder != NULL)
	{
		mtlEndEncoding(renderer->renderCommandEncoder);
		renderer->renderCommandEncoder = NULL;
	}
}

static void METAL_INTERNAL_BeginFrame(FNA3D_Renderer *driverData)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;

	if (renderer->frameInProgress) return;

	/* Wait for the last command buffer to complete... */
	if (renderer->committedCommandBuffer != NULL)
	{
		mtlWaitUntilCompleted(renderer->committedCommandBuffer);
		objc_release(renderer->committedCommandBuffer);
		renderer->committedCommandBuffer = NULL;
	}

	/* The cycle begins anew! */
	renderer->frameInProgress = 1;
	renderer->pool = objc_autoreleasePoolPush();
	renderer->commandBuffer = mtlMakeCommandBuffer(renderer->queue);
}

static void METAL_INTERNAL_UpdateRenderPass(MetalRenderer *renderer)
{
	MTLRenderPassDescriptor *passDesc;
	MTLRenderPassColorAttachmentDescriptor *colorAttachment;
	MTLRenderPassDepthAttachmentDescriptor *depthAttachment;
	MTLRenderPassStencilAttachmentDescriptor *stencilAttachment;
	int32_t i;

	if (!renderer->needNewRenderPass)
	{
		/* Nothing for us to do! */
		return;
	}

	/* Normally the frame begins in BeginDraw(),
	 * but some games perform drawing outside
	 * of the Draw method (e.g. initializing
	 * render targets in LoadContent). This call
	 * ensures that we catch any unexpected draws.
	 * -caleb
	 */
	METAL_INTERNAL_BeginFrame((FNA3D_Renderer*) renderer);

	/* Wrap up rendering with the old encoder */
	METAL_INTERNAL_EndPass(renderer);

	/* Generate the descriptor */
	passDesc = mtlMakeRenderPassDescriptor();

	/* Bind color attachments */
	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		if (renderer->currentAttachments[i] == NULL)
		{
			continue;
		}

		colorAttachment = mtlGetColorAttachment(passDesc, i);
		mtlSetAttachmentTexture(
			colorAttachment,
			renderer->currentAttachments[i]
		);
		mtlSetAttachmentSlice(
			colorAttachment,
			renderer->currentAttachmentSlices[i]
		);

		/* Multisample? */
		if (renderer->currentSampleCount > 0)
		{
			mtlSetAttachmentTexture(
				colorAttachment,
				renderer->currentMSAttachments[i]
			);
			mtlSetAttachmentSlice(
				colorAttachment,
				0
			);
			mtlSetAttachmentResolveTexture(
				colorAttachment,
				renderer->currentAttachments[i]
			);
			mtlSetAttachmentStoreAction(
				colorAttachment,
				MTLStoreActionMultisampleResolve
			);
			mtlSetAttachmentResolveSlice(
				colorAttachment,
				renderer->currentAttachmentSlices[i]
			);
		}

		/* Clear color */
		if (renderer->shouldClearColor)
		{
			mtlSetAttachmentLoadAction(
				colorAttachment,
				MTLLoadActionClear
			);
			mtlSetAttachmentClearColor(
				colorAttachment,
				renderer->clearColor.x,
				renderer->clearColor.y,
				renderer->clearColor.z,
				renderer->clearColor.w
			);
		}
		else
		{
			mtlSetAttachmentLoadAction(
				colorAttachment,
				MTLLoadActionLoad
			);
		}
	}

	/* Bind depth attachment */
	if (renderer->currentDepthFormat != FNA3D_DEPTHFORMAT_NONE)
	{
		depthAttachment = mtlGetDepthAttachment(passDesc);
		mtlSetAttachmentTexture(
			depthAttachment,
			renderer->currentDepthStencilBuffer
		);
		mtlSetAttachmentStoreAction(
			depthAttachment,
			(renderer->preserveDepthStencil) ?
				MTLStoreActionStore :
				MTLStoreActionDontCare
		);

		/* Clear? */
		if (renderer->shouldClearDepth)
		{
			mtlSetAttachmentLoadAction(
				depthAttachment,
				MTLLoadActionClear
			);
			mtlSetAttachmentClearDepth(
				depthAttachment,
				renderer->clearDepth
			);
		}
		else
		{
			mtlSetAttachmentLoadAction(
				depthAttachment,
				(renderer->preserveDepthStencil) ?
					MTLLoadActionLoad :
					MTLLoadActionDontCare
			);
		}
	}

	/* Bind stencil attachment */
	if (renderer->currentDepthFormat == FNA3D_DEPTHFORMAT_D24S8)
	{
		stencilAttachment = mtlGetStencilAttachment(passDesc);
		mtlSetAttachmentTexture(
			stencilAttachment,
			renderer->currentDepthStencilBuffer
		);
		mtlSetAttachmentStoreAction(
			stencilAttachment,
			(renderer->preserveDepthStencil) ?
				MTLStoreActionStore :
				MTLStoreActionDontCare
		);

		/* Clear? */
		if (renderer->shouldClearStencil)
		{
			mtlSetAttachmentLoadAction(
				stencilAttachment,
				MTLLoadActionClear
			);
			mtlSetAttachmentClearStencil(
				stencilAttachment,
				renderer->clearStencil
			);
		}
		else
		{
			mtlSetAttachmentLoadAction(
				stencilAttachment,
				(renderer->preserveDepthStencil) ?
					MTLLoadActionLoad :
					MTLLoadActionDontCare
			);
		}
	}

	/* Get attachment size */
	renderer->currentAttachmentWidth = mtlGetTextureWidth(
		renderer->currentAttachments[0]
	);
	renderer->currentAttachmentHeight = mtlGetTextureHeight(
		renderer->currentAttachments[0]
	);

	/* Attach the visibility buffer, if needed */
	if (renderer->currentVisibilityBuffer != NULL)
	{
		mtlSetVisibilityResultBuffer(
			passDesc,
			renderer->currentVisibilityBuffer
		);
	}

	/* Make a new encoder */
	renderer->renderCommandEncoder = mtlMakeRenderCommandEncoder(
		renderer->commandBuffer,
		passDesc
	);

	/* Reset the flags */
	renderer->needNewRenderPass = 0;
	renderer->shouldClearColor = 0;
	renderer->shouldClearDepth = 0;
	renderer->shouldClearStencil = 0;

	/* Apply the dynamic state */
	METAL_INTERNAL_SetEncoderViewport(renderer);
	METAL_INTERNAL_SetEncoderScissorRect(renderer);
	METAL_INTERNAL_SetEncoderBlendColor(renderer);
	METAL_INTERNAL_SetEncoderStencilReferenceValue(renderer);
	METAL_INTERNAL_SetEncoderCullMode(renderer);
	METAL_INTERNAL_SetEncoderFillMode(renderer);
	METAL_INTERNAL_SetEncoderDepthBias(renderer);

	/* Start visibility buffer counting */
	if (renderer->currentVisibilityBuffer != NULL)
	{
		mtlSetVisibilityResultMode(
			renderer->renderCommandEncoder,
			MTLVisibilityResultModeCounting,
			0
		);
	}

	/* Reset the bindings */
	for (i = 0; i < MAX_TOTAL_SAMPLERS; i += 1)
	{
		if (renderer->textures[i] != &NullTexture)
		{
			renderer->textureNeedsUpdate[i] = 1;
		}
		if (renderer->samplers[i] != NULL)
		{
			renderer->samplerNeedsUpdate[i] = 1;
		}
	}
	renderer->ldDepthStencilState = NULL;
	renderer->ldUniformBuffer = NULL;
	renderer->ldVertUniformOffset = 0;
	renderer->ldFragUniformOffset = 0;
	renderer->ldPipelineState = NULL;
	for (i = 0; i < MAX_BOUND_VERTEX_BUFFERS; i += 1)
	{
		renderer->ldVertexBuffers[i] = NULL;
		renderer->ldVertexBufferOffsets[i] = 0;
	}
}

/* Pipeline Flush Functions */

static void METAL_INTERNAL_MarkBufferAsBound(
	MetalRenderer *renderer,
	MetalBuffer *buf
) {
	if (buf->bound) return;
	buf->bound = 1;

	if (renderer->numBuffersInUse == renderer->maxBuffersInUse)
	{
		renderer->maxBuffersInUse *= 2;
		renderer->buffersInUse = SDL_realloc(
			renderer->buffersInUse,
			sizeof(MetalBuffer*) * renderer->maxBuffersInUse
		);
	}

	renderer->buffersInUse[renderer->numBuffersInUse] = buf;
	renderer->numBuffersInUse += 1;
}

static inline void METAL_INTERNAL_ResetBuffers(MetalRenderer *renderer)
{
	int32_t i;
	for (i = 0; i < renderer->numBuffersInUse; i += 1)
	{
		if (renderer->buffersInUse[i] != NULL)
		{
			renderer->buffersInUse[i]->bound = 0;
			renderer->buffersInUse[i]->currentSubBufferIndex = 0;
			renderer->buffersInUse[i] = NULL;
		}
	}
	renderer->numBuffersInUse = 0;
}

static void METAL_INTERNAL_MarkTextureAsBound(
	MetalRenderer *renderer,
	MetalTexture *tex
) {
	if (tex->bound) return;
	tex->bound = 1;

	if (renderer->numTexturesInUse == renderer->maxTexturesInUse)
	{
		renderer->maxTexturesInUse *= 2;
		renderer->texturesInUse = SDL_realloc(
			renderer->texturesInUse,
			sizeof(MetalTexture*) * renderer->maxTexturesInUse
		);
	}

	renderer->texturesInUse[renderer->numTexturesInUse] = tex;
	renderer->numTexturesInUse += 1;
}

static inline void METAL_INTERNAL_ResetTextures(MetalRenderer *renderer)
{
	int32_t i;
	for (i = 0; i < renderer->numTexturesInUse; i += 1)
	{
		if (renderer->texturesInUse[i] != NULL)
		{
			renderer->texturesInUse[i]->bound = 0;
			renderer->texturesInUse[i] = NULL;
		}
	}
	renderer->numTexturesInUse = 0;
}

static void METAL_INTERNAL_Flush(MetalRenderer *renderer)
{
	METAL_INTERNAL_EndPass(renderer);
	mtlCommitCommandBuffer(renderer->commandBuffer);
	mtlWaitUntilCompleted(renderer->commandBuffer);

	renderer->commandBuffer = mtlMakeCommandBuffer(renderer->queue);
	renderer->needNewRenderPass = 1;

	METAL_INTERNAL_ResetBuffers(renderer);
	METAL_INTERNAL_ResetTextures(renderer);
}

/* Buffer Helper Functions */

static int32_t METAL_INTERNAL_NextHighestAlignment(int32_t n, int32_t align)
{
	return align * ((n + align - 1) / align);
}

static void METAL_INTERNAL_AllocateSubBuffer(
	MetalRenderer *renderer,
	MetalBuffer *buffer
) {
	/* This allocation strategy uses a "bucketing" system with a fixed
	 *  number of buckets, each one larger than the last. Buffers will
	 *  go into the first available bucket that accomodates their size,
	 *  even if there is a larger bucket already allocated that they
	 *  could fit into.
	 *
	 * Depending on the data, this may lead to unnecessary allocations,
	 *  but on the other hand it ensures that small buffers don't crowd
	 *  out the larger buckets that are required for very big buffers.
	 *  The majority of FNA games probably won't exceed the first bucket.
	 *
	 * In the worst case, the first buffer allocated is >64MB and thus
	 *  requires the 128MB bucket. Any future buffers <64MB will *not*
	 *  get slotted into the 128MB bucket, even though they could fit.
	 *  Instead, smaller buckets will be allocated. The larger buckets
	 *  can receive overflow from the smaller buckets if they get full,
	 *  but all the smaller buckets will be allocated and filled first.
	 *
	 * The maximum bucket size is 256MB, which should be plenty of room.
	 *  If your game is using over 4+8+16+32+64+128+256 MB of buffer data,
	 *  you're probably doing something wrong.
	 *
	 * -caleb
	 */

	int32_t totalPhysicalSize, totalAllocated, i, alignedAlloc;
	MetalSubBuffer *subBuffer;

	/* Which physical buffer should we suballocate from? */
	for (i = 0; i < PHYSICAL_BUFFER_MAX_COUNT; i += 1)
	{
		totalPhysicalSize = PHYSICAL_BUFFER_BASE_SIZE << i;
		totalAllocated = renderer->bufferAllocator->totalAllocated[i];
		alignedAlloc = METAL_INTERNAL_NextHighestAlignment(
			totalAllocated + buffer->size,
			4
		);

		if (alignedAlloc <= totalPhysicalSize)
		{
			/* It fits! */
			break;
		}
	}
	if (i == PHYSICAL_BUFFER_MAX_COUNT)
	{
		/* FIXME: How should we handle this? */
		FNA3D_LogError("Oh crap, we're totally out of buffer room!!!");
		return;
	}

	/* Create the physical buffer if needed */
	if (renderer->bufferAllocator->physicalBuffers[i] == NULL)
	{
		renderer->bufferAllocator->physicalBuffers[i] = mtlNewBuffer(
			renderer->device,
			totalPhysicalSize,
			0 /* FIXME: MTLResourceHazardTrackingModeUntracked? */
		);
	}

	/* Reallocate the subbuffer array if we're at max capacity */
	if (buffer->subBufferCount == buffer->subBufferCapacity)
	{
		buffer->subBufferCapacity *= 2;
		buffer->subBuffers = SDL_realloc(
			buffer->subBuffers,
			sizeof(MetalSubBuffer) * buffer->subBufferCapacity
		);
	}

	/* Populate the given MetalSubBuffer */
	subBuffer = &buffer->subBuffers[buffer->subBufferCount];
	subBuffer->buffer = renderer->bufferAllocator->physicalBuffers[i];
	subBuffer->offset = totalAllocated;
	subBuffer->ptr = (
		(uint8_t*) mtlGetBufferContents(subBuffer->buffer) +
		subBuffer->offset
	);

	/* Mark how much we've just allocated, rounding up for alignment */
	renderer->bufferAllocator->totalAllocated[i] = alignedAlloc;
	buffer->subBufferCount += 1;
}

static FNA3D_Buffer* METAL_INTERNAL_CreateBuffer(
	FNA3D_Renderer *driverData,
	int32_t size
) {
	MetalBuffer *result = SDL_malloc(sizeof(MetalBuffer));
	SDL_memset(result, '\0', sizeof(MetalBuffer));

	result->size = size;
	result->subBufferCapacity = 4;
	result->subBuffers = SDL_malloc(
		sizeof(MetalSubBuffer) * result->subBufferCapacity
	);

	return (FNA3D_Buffer*) result;
}

static void METAL_INTERNAL_DestroyBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalBuffer *mtlBuffer = (MetalBuffer*) buffer;
	int32_t i;

	if (mtlBuffer->bound)
	{
		for (i = 0; i < renderer->numBuffersInUse; i += 1)
		{
			if (renderer->buffersInUse[i] == mtlBuffer)
			{
				renderer->buffersInUse[i] = NULL;
			}
		}
	}

	SDL_free(mtlBuffer->subBuffers);
	mtlBuffer->subBuffers = NULL;

	SDL_free(mtlBuffer);
}

static void METAL_INTERNAL_SetBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalBuffer *mtlBuffer = (MetalBuffer*) buffer;
	int32_t prevIndex;

	#define CURIDX mtlBuffer->currentSubBufferIndex
	#define SUBBUF mtlBuffer->subBuffers[CURIDX]

	prevIndex = CURIDX;

	if (mtlBuffer->bound)
	{
		if (options == FNA3D_SETDATAOPTIONS_NONE)
		{
			METAL_INTERNAL_Flush(renderer);
			mtlBuffer->bound = 1;
		}
		else if (options == FNA3D_SETDATAOPTIONS_DISCARD)
		{
			CURIDX += 1;
		}
	}

	/* Create a new SubBuffer if needed */
	if (CURIDX == mtlBuffer->subBufferCount)
	{
		METAL_INTERNAL_AllocateSubBuffer(renderer, mtlBuffer);
	}

	/* Copy over previous contents when needed */
	if (	options == FNA3D_SETDATAOPTIONS_NONE &&
		dataLength < mtlBuffer->size &&
		CURIDX != prevIndex			)
	{
		SDL_memcpy(
			SUBBUF.ptr,
			mtlBuffer->subBuffers[prevIndex].ptr,
			mtlBuffer->size
		);
	}

	/* Copy the data! */
	SDL_memcpy(
		SUBBUF.ptr + offsetInBytes,
		data,
		dataLength
	);

	#undef SUBBUF
	#undef CURIDX
}

/* Pipeline State Object Creation / Retrieval */

static PackedRenderPipeline METAL_INTERNAL_GetPackedRenderPipeline(
	MetalRenderer *renderer
) {
	PackedRenderPipeline result;

	MOJOSHADER_mtlGetBoundShaders(&result.vshader, &result.pshader);
	result.blendState = GetPackedBlendState(renderer->blendState);
	result.colorFormats[0] = renderer->currentColorFormats[0];
	result.colorFormats[1] = renderer->currentColorFormats[1];
	result.colorFormats[2] = renderer->currentColorFormats[2];
	result.colorFormats[3] = renderer->currentColorFormats[3];
	result.depthFormat = XNAToMTL_DepthFormat(
		renderer,
		renderer->currentDepthFormat
	);
	result.vertexDescriptor = renderer->currentVertexDescriptor;
	result.sampleCount = renderer->currentSampleCount;

	return result;
}

static MTLRenderPipelineState* METAL_INTERNAL_PackedRenderPipelineArray_Fetch(
	PackedRenderPipelineArray arr,
	PackedRenderPipeline key
) {
	int32_t i;
	PackedRenderPipeline other;

	for (i = 0; i < arr.count; i += 1)
	{
		other = arr.elements[i].key;
		if (	key.vshader == other.vshader &&
			key.pshader == other.pshader &&
			key.vertexDescriptor == other.vertexDescriptor &&
			key.blendState.a == other.blendState.a &&
			key.blendState.b == other.blendState.b &&
			key.depthFormat == other.depthFormat &&
			key.sampleCount == other.sampleCount &&
			key.colorFormats[0] == other.colorFormats[0] &&
			key.colorFormats[1] == other.colorFormats[1] &&
			key.colorFormats[2] == other.colorFormats[2] &&
			key.colorFormats[3] == other.colorFormats[3]	)
		{
			return arr.elements[i].value;
		}
	}

	return NULL;
}

static void METAL_INTERNAL_PackedRenderPipelineArray_Insert(
	PackedRenderPipelineArray *arr,
	PackedRenderPipeline key,
	MTLRenderPipelineState* value
) {
	PackedRenderPipelineMap map;
	map.key = key;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 4, PackedRenderPipelineMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

static MTLRenderPipelineState* METAL_INTERNAL_FetchRenderPipeline(
	MetalRenderer *renderer
) {
	PackedRenderPipeline packedPipeline;
	MTLRenderPipelineDescriptor *pipelineDesc;
	MTLFunction *vertHandle;
	MTLFunction *fragHandle;
	uint8_t alphaBlendEnable;
	int32_t i;
	MTLRenderPipelineColorAttachmentDescriptor *colorAttachment;
	MOJOSHADER_mtlShader *vert, *pixl;
	MTLRenderPipelineState *result;

	/* Can we just reuse an existing pipeline? */
	packedPipeline = METAL_INTERNAL_GetPackedRenderPipeline(renderer);
	result = METAL_INTERNAL_PackedRenderPipelineArray_Fetch(
		renderer->pipelineStateCache,
		packedPipeline
	);
	if (result != NULL)
	{
		/* We already have this state cached! */
		return result;
	}

	/* We have to make a new pipeline... */
	pipelineDesc = mtlNewRenderPipelineDescriptor();
	MOJOSHADER_mtlGetBoundShaders(&vert, &pixl);
	vertHandle = MOJOSHADER_mtlGetFunctionHandle(vert);
	fragHandle = MOJOSHADER_mtlGetFunctionHandle(pixl);
	mtlSetPipelineVertexFunction(
		pipelineDesc,
		vertHandle
	);
	mtlSetPipelineFragmentFunction(
		pipelineDesc,
		fragHandle
	);
	mtlSetPipelineVertexDescriptor(
		pipelineDesc,
		renderer->currentVertexDescriptor
	);
	mtlSetDepthAttachmentPixelFormat(
		pipelineDesc,
		XNAToMTL_DepthFormat(renderer, renderer->currentDepthFormat)
	);
	if (renderer->currentDepthFormat == FNA3D_DEPTHFORMAT_D24S8)
	{
		mtlSetStencilAttachmentPixelFormat(
			pipelineDesc,
			XNAToMTL_DepthFormat(renderer, renderer->currentDepthFormat)
		);
	}
	mtlSetPipelineSampleCount(
		pipelineDesc,
		SDL_max(1, renderer->currentSampleCount)
	);

	/* Apply the blend state */
	alphaBlendEnable = !(
		renderer->blendState.colorSourceBlend == FNA3D_BLEND_ONE &&
		renderer->blendState.colorDestinationBlend == FNA3D_BLEND_ZERO &&
		renderer->blendState.alphaSourceBlend == FNA3D_BLEND_ONE &&
		renderer->blendState.alphaDestinationBlend == FNA3D_BLEND_ZERO
	);
	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		if (renderer->currentAttachments[i] == NULL)
		{
			/* There's no attachment bound at this index. */
			continue;
		}

		colorAttachment = mtlGetColorAttachment(
			pipelineDesc,
			i
		);
		mtlSetAttachmentPixelFormat(
			colorAttachment,
			renderer->currentColorFormats[i]
		);
		mtlSetAttachmentBlendingEnabled(
			colorAttachment,
			alphaBlendEnable
		);
		if (alphaBlendEnable)
		{
			mtlSetAttachmentSourceRGBBlendFactor(
				colorAttachment,
				XNAToMTL_BlendMode[
					renderer->blendState.colorSourceBlend
				]
			);
			mtlSetAttachmentDestinationRGBBlendFactor(
				colorAttachment,
				XNAToMTL_BlendMode[
					renderer->blendState.colorDestinationBlend
				]
			);
			mtlSetAttachmentSourceAlphaBlendFactor(
				colorAttachment,
				XNAToMTL_BlendMode[
					renderer->blendState.alphaSourceBlend
				]
			);
			mtlSetAttachmentDestinationAlphaBlendFactor(
				colorAttachment,
				XNAToMTL_BlendMode[
					renderer->blendState.alphaDestinationBlend
				]
			);
			mtlSetAttachmentRGBBlendOperation(
				colorAttachment,
				XNAToMTL_BlendOperation[
					renderer->blendState.colorBlendFunction
				]
			);
			mtlSetAttachmentAlphaBlendOperation(
				colorAttachment,
				XNAToMTL_BlendOperation[
					renderer->blendState.alphaBlendFunction
				]
			);
		}

		/* FIXME: So how exactly do we factor in
		 * COLORWRITEENABLE for buffer 0? Do we just assume that
		 * the default is just buffer 0, and all other calls
		 * update the other write masks?
		 */
		if (i == 0)
		{
			mtlSetAttachmentWriteMask(
				colorAttachment,
				XNAToMTL_ColorWriteMask(
					renderer->blendState.colorWriteEnable
				)
			);
		}
		else if (i == 1)
		{
			mtlSetAttachmentWriteMask(
				mtlGetColorAttachment(pipelineDesc, 1),
				XNAToMTL_ColorWriteMask(
					renderer->blendState.colorWriteEnable1
				)
			);
		}
		else if (i == 2)
		{
			mtlSetAttachmentWriteMask(
				mtlGetColorAttachment(pipelineDesc, 2),
				XNAToMTL_ColorWriteMask(
					renderer->blendState.colorWriteEnable2
				)
			);
		}
		else if (i == 3)
		{
			mtlSetAttachmentWriteMask(
				mtlGetColorAttachment(pipelineDesc, 3),
				XNAToMTL_ColorWriteMask(
					renderer->blendState.colorWriteEnable3
				)
			);
		}
	}

	/* Bake the render pipeline! */
	result = mtlNewRenderPipelineState(
		renderer->device,
		pipelineDesc
	);
	METAL_INTERNAL_PackedRenderPipelineArray_Insert(
		&renderer->pipelineStateCache,
		packedPipeline,
		result
	);

	/* Clean up */
	objc_release(pipelineDesc);

	/* Return the pipeline! */
	return result;
}

static MTLDepthStencilState* METAL_INTERNAL_FetchDepthStencilState(
	MetalRenderer *renderer
) {
	PackedState packedState;
	MTLDepthStencilState *result;
	MTLDepthStencilDescriptor *dsDesc;
	MTLStencilDescriptor *front, *back;
	uint8_t zEnable, sEnable, zFormat;

	/* Just use the default depth-stencil state
	 * if depth and stencil testing are disabled,
	 * or if there is no bound depth attachment.
	 * This wards off Metal validation errors.
	 * -caleb
	 */
	zEnable = renderer->depthStencilState.depthBufferEnable;
	sEnable = renderer->depthStencilState.stencilEnable;
	zFormat = (renderer->currentDepthFormat != FNA3D_DEPTHFORMAT_NONE);
	if ((!zEnable && !sEnable) || (!zFormat))
	{
		return renderer->defaultDepthStencilState;
	}

	/* Can we just reuse an existing state? */
	packedState = GetPackedDepthStencilState(renderer->depthStencilState);
	result = PackedStateArray_Fetch(
		renderer->depthStencilStateCache,
		packedState
	);
	if (result != NULL)
	{
		/* This state has already been cached! */
		return result;
	}

	/* We have to make a new DepthStencilState... */
	dsDesc = mtlNewDepthStencilDescriptor();
	if (zEnable)
	{
		mtlSetDepthCompareFunction(
			dsDesc,
			XNAToMTL_CompareFunc[
				renderer->depthStencilState.depthBufferFunction
			]
		);
		mtlSetDepthWriteEnabled(
			dsDesc,
			renderer->depthStencilState.depthBufferWriteEnable
		);
	}

	/* Create stencil descriptors */
	front = NULL;
	back = NULL;

	if (sEnable)
	{
		front = mtlNewStencilDescriptor();
		mtlSetStencilFailureOperation(
			front,
			XNAToMTL_StencilOp[
				renderer->depthStencilState.stencilFail
			]
		);
		mtlSetDepthFailureOperation(
			front,
			XNAToMTL_StencilOp[
				renderer->depthStencilState.stencilDepthBufferFail
			]
		);
		mtlSetDepthStencilPassOperation(
			front,
			XNAToMTL_StencilOp[
				renderer->depthStencilState.stencilPass
			]
		);
		mtlSetStencilCompareFunction(
			front,
			XNAToMTL_CompareFunc[
				renderer->depthStencilState.stencilFunction
			]
		);
		mtlSetStencilReadMask(
			front,
			(uint32_t) renderer->depthStencilState.stencilMask
		);
		mtlSetStencilWriteMask(
			front,
			(uint32_t) renderer->depthStencilState.stencilWriteMask
		);

		if (!renderer->depthStencilState.twoSidedStencilMode)
		{
			back = front;
		}
	}

	if (front != back)
	{
		back = mtlNewStencilDescriptor();
		mtlSetStencilFailureOperation(
			back,
			XNAToMTL_StencilOp[
				renderer->depthStencilState.ccwStencilFail
			]
		);
		mtlSetDepthFailureOperation(
			back,
			XNAToMTL_StencilOp[
				renderer->depthStencilState.ccwStencilDepthBufferFail
			]
		);
		mtlSetDepthStencilPassOperation(
			back,
			XNAToMTL_StencilOp[
				renderer->depthStencilState.ccwStencilPass
			]
		);
		mtlSetStencilCompareFunction(
			back,
			XNAToMTL_CompareFunc[
				renderer->depthStencilState.ccwStencilFunction
			]
		);
		mtlSetStencilReadMask(
			back,
			(uint32_t) renderer->depthStencilState.stencilMask
		);
		mtlSetStencilWriteMask(
			back,
			(uint32_t) renderer->depthStencilState.stencilWriteMask
		);
	}

	mtlSetFrontFaceStencil(
		dsDesc,
		front
	);
	mtlSetBackFaceStencil(
		dsDesc,
		back
	);

	/* Bake the state! */
	result = mtlNewDepthStencilState(
		renderer->device,
		dsDesc
	);
	PackedStateArray_Insert(
		&renderer->depthStencilStateCache,
		packedState,
		result
	);

	/* Clean up */
	objc_release(dsDesc);

	/* Return the state! */
	return result;
}

static MTLSamplerState* METAL_INTERNAL_FetchSamplerState(
	MetalRenderer *renderer,
	FNA3D_SamplerState *samplerState,
	uint8_t hasMipmaps
) {
	PackedState packedState;
	MTLSamplerState *result;
	MTLSamplerDescriptor *desc;

	/* Can we reuse an existing state? */
	packedState = GetPackedSamplerState(*samplerState);
	result = PackedStateArray_Fetch(
		renderer->samplerStateCache,
		packedState
	);
	if (result != NULL)
	{
		/* This state has already been cached! */
		return result;
	}

	/* We have to make a new sampler state... */
	desc = mtlNewSamplerDescriptor();

	mtlSetSampler_sAddressMode(
		desc,
		XNAToMTL_Wrap[samplerState->addressU]
	);
	mtlSetSampler_tAddressMode(
		desc,
		XNAToMTL_Wrap[samplerState->addressV]
	);
	mtlSetSampler_rAddressMode(
		desc,
		XNAToMTL_Wrap[samplerState->addressW]
	);
	mtlSetSamplerMagFilter(
		desc,
		XNAToMTL_MagFilter[samplerState->filter]
	);
	mtlSetSamplerMinFilter(
		desc,
		XNAToMTL_MinFilter[samplerState->filter]
	);
	if (hasMipmaps)
	{
		mtlSetSamplerMipFilter(
			desc,
			XNAToMTL_MipFilter[samplerState->filter]
		);
	}
	mtlSetSamplerLodMinClamp(
		desc,
		samplerState->maxMipLevel
	);
	mtlSetSamplerMaxAnisotropy(
		desc,
		samplerState->filter == FNA3D_TEXTUREFILTER_ANISOTROPIC ?
			SDL_max(1, samplerState->maxAnisotropy) :
			1
	);

	/* FIXME:
	 * The only way to set lod bias in metal is via the MSL
	 * bias() function in a shader. So we can't do:
	 *
	 * 	mtlSetSamplerLodBias(
	 *		samplerDesc,
	 *		samplerState.MipMapLevelOfDetailBias
	 *	);
	 *
	 * What should we do instead?
	 *
	 * -caleb
	 */

	/* Bake the state! */
	result = mtlNewSamplerState(
		renderer->device,
		desc
	);
	PackedStateArray_Insert(
		&renderer->samplerStateCache,
		packedState,
		result
	);

	/* Clean up */
	objc_release(desc);

	/* Return the state! */
	return result;
}

static MTLTexture* METAL_INTERNAL_FetchTransientTexture(
	MetalRenderer *renderer,
	MetalTexture *fromTexture
) {
	MTLTextureDescriptor *desc;
	MetalTexture *result, *curr;

	/* Can we just reuse an existing texture? */
	curr = renderer->transientTextures;
	while (curr != NULL)
	{
		if (	curr->format == fromTexture->format &&
			curr->width == fromTexture->width &&
			curr->height == fromTexture->height &&
			curr->hasMipmaps == fromTexture->hasMipmaps	)
		{
			mtlSetPurgeableState(
				curr->handle,
				MTLPurgeableStateNonVolatile
			);
			return curr->handle;
		}
		curr = curr->next;
	}

	/* We have to make a new texture... */
	desc = mtlMakeTexture2DDescriptor(
		XNAToMTL_TextureFormat[fromTexture->format],
		fromTexture->width,
		fromTexture->height,
		fromTexture->hasMipmaps
	);
	result = CreateTexture(
		renderer,
		mtlNewTexture(renderer->device, desc),
		fromTexture->format,
		fromTexture->width,
		fromTexture->height,
		fromTexture->hasMipmaps ? 2 : 0,
		0
	);
	LinkedList_Add(renderer->transientTextures, result, curr);
	return result->handle;
}

static MTLVertexDescriptor* METAL_INTERNAL_FetchVertexBufferBindingsDescriptor(
	MetalRenderer *renderer,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings
) {
	int32_t whatever, i, j, k, usage, index, attribLoc;
	uint32_t noReallyWhatever;
	uint8_t attrUse[MOJOSHADER_USAGE_TOTAL][16];
	FNA3D_VertexDeclaration vertexDeclaration;
	FNA3D_VertexElement element;
	MTLVertexAttributeDescriptor *attrib;
	MTLVertexBufferLayoutDescriptor *layout;
	MOJOSHADER_mtlShader *vertexShader, *blah;
	MTLVertexDescriptor *result;

	/* We need the vertex shader... */
	MOJOSHADER_mtlGetBoundShaders(&vertexShader, &blah);

	/* Can we just reuse an existing descriptor? */
	result = PackedVertexBufferBindingsArray_Fetch(
		renderer->vertexDescriptorCache,
		bindings,
		numBindings,
		vertexShader,
		&whatever,
		&noReallyWhatever
	);
	if (result != NULL)
	{
		/* This descriptor has already been cached! */
		return result;
	}

	/* We have to make a new vertex descriptor... */
	result = mtlMakeVertexDescriptor();
	objc_retain(result);

	/* There's this weird case where you can have overlapping
	 * vertex usage/index combinations. It seems like the first
	 * attrib gets priority, so whenever a duplicate attribute
	 * exists, give it the next available index. If that fails, we
	 * have to crash :/
	 * -flibit
	 */
	SDL_memset(attrUse, '\0', sizeof(attrUse));
	for (i = 0; i < numBindings; i += 1)
	{
		/* Describe vertex attributes */
		vertexDeclaration = bindings[i].vertexDeclaration;
		for (j = 0; j < vertexDeclaration.elementCount; j += 1)
		{
			element = vertexDeclaration.elements[j];
			usage = element.vertexElementUsage;
			index = element.usageIndex;
			if (attrUse[usage][index])
			{
				index = -1;
				for (k = 0; k < 16; k += 1)
				{
					if (!attrUse[usage][k])
					{
						index = k;
						break;
					}
				}
				if (index < 0)
				{
					FNA3D_LogError(
						"Vertex usage collision!"
					);
				}
			}
			attrUse[usage][index] = 1;
			attribLoc = MOJOSHADER_mtlGetVertexAttribLocation(
				vertexShader,
				VertexAttribUsage(usage),
				index
			);
			if (attribLoc == -1)
			{
				/* Stream not in use! */
				continue;
			}
			attrib = mtlGetVertexAttributeDescriptor(
				result,
				attribLoc
			);
			mtlSetVertexAttributeFormat(
				attrib,
				XNAToMTL_VertexAttribType[element.vertexElementFormat]
			);
			mtlSetVertexAttributeOffset(
				attrib,
				element.offset
			);
			mtlSetVertexAttributeBufferIndex(
				attrib,
				i
			);
		}

		/* Describe vertex buffer layout */
		layout = mtlGetVertexBufferLayoutDescriptor(
			result,
			i
		);
		mtlSetVertexBufferLayoutStride(
			layout,
			vertexDeclaration.vertexStride
		);
		if (bindings[i].instanceFrequency > 0)
		{
			mtlSetVertexBufferLayoutStepFunction(
				layout,
				MTLVertexStepFunctionPerInstance
			);
			mtlSetVertexBufferLayoutStepRate(
				layout,
				bindings[i].instanceFrequency
			);
		}
	}

	/* Store and return the vertex descriptor! */
	PackedVertexBufferBindingsArray_Insert(
		&renderer->vertexDescriptorCache,
		bindings,
		numBindings,
		vertexShader,
		result
	);
	return result;
}

/* Forward Declarations */

static void METAL_INTERNAL_DestroyFramebuffer(MetalRenderer *renderer);

static void METAL_SetRenderTargets(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
);

static void METAL_GetTextureData2D(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
);

/* Renderer Implementation */

/* Quit */

static void METAL_DestroyDevice(FNA3D_Device *device)
{
	MetalRenderer *renderer = (MetalRenderer*) device->driverData;
	int32_t i;
	MetalTexture *tex, *next;

	/* Stop rendering */
	METAL_INTERNAL_EndPass(renderer);

	/* Release depth stencil states */
	for (i = 0; i < renderer->depthStencilStateCache.count; i += 1)
	{
		objc_release(renderer->depthStencilStateCache.elements[i].value);
	}
	SDL_free(renderer->depthStencilStateCache.elements);

	/* Release sampler states */
	for (i = 0; i < renderer->samplerStateCache.count; i += 1)
	{
		objc_release(renderer->samplerStateCache.elements[i].value);
	}
	SDL_free(renderer->samplerStateCache.elements);

	/* Release pipeline states */
	for (i = 0; i < renderer->pipelineStateCache.count; i += 1)
	{
		objc_release(renderer->pipelineStateCache.elements[i].value);
	}
	SDL_free(renderer->pipelineStateCache.elements);

	/* Release vertex descriptors */
	for (i = 0; i < renderer->vertexDescriptorCache.count; i += 1)
	{
		objc_release(renderer->vertexDescriptorCache.elements[i].value);
	}
	SDL_free(renderer->vertexDescriptorCache.elements);

	/* Release transient textures */
	tex = renderer->transientTextures;
	while (tex != NULL)
	{
		next = tex->next;
		objc_release(tex->handle);
		SDL_free(tex);
		tex = next;
	}
	renderer->transientTextures = NULL;

	/* Destroy the physical buffers and allocator */
	for (i = 0; i < PHYSICAL_BUFFER_MAX_COUNT; i += 1)
	{
		if (renderer->bufferAllocator->physicalBuffers[i] != NULL)
		{
			objc_release(renderer->bufferAllocator->physicalBuffers[i]);
		}
	}
	SDL_free(renderer->bufferAllocator);
	SDL_free(renderer->buffersInUse);

	/* Destroy the backbuffer */
	METAL_INTERNAL_DestroyFramebuffer(renderer);
	SDL_free(renderer->backbuffer);

	/* Destroy the view */
	SDL_Metal_DestroyView(renderer->view);

	/* Destroy the MojoShader context */
	MOJOSHADER_mtlDestroyContext(renderer->mtlContext);

	SDL_free(renderer);
	SDL_free(device);
}

/* Presentation */

static void METAL_INTERNAL_UpdateBackbufferVertexBuffer(
	MetalRenderer *renderer,
	FNA3D_Rect *srcRect,
	FNA3D_Rect *dstRect,
	int32_t drawableWidth,
	int32_t drawableHeight
) {
	float backbufferWidth = (float) renderer->backbuffer->width;
	float backbufferHeight = (float) renderer->backbuffer->height;
	float sx0, sy0, sx1, sy1;
	float dx0, dy0, dx1, dy1;
	float data[16];

	/* Cache the new info */
	renderer->backbufferSizeChanged = 0;
	renderer->prevSrcRect = *srcRect;
	renderer->prevDstRect = *dstRect;

	/* Scale the texture coordinates to (0, 1) */
	sx0 = srcRect->x / backbufferWidth;
	sy0 = srcRect->y / backbufferHeight;
	sx1 = (srcRect->x + srcRect->w) / backbufferWidth;
	sy1 = (srcRect->y + srcRect->h) / backbufferHeight;

	/* Scale the position coordinates to (-1, 1) */
	dx0 = (dstRect->x / (float) drawableWidth) * 2.0f - 1.0f;
	dy0 = (dstRect->y / (float) drawableHeight) * 2.0f - 1.0f;
	dx1 = ((dstRect->x + dstRect->w) / (float) drawableWidth) * 2.0f - 1.0f;
	dy1 = ((dstRect->y + dstRect->h) / (float) drawableHeight) * 2.0f - 1.0f;

	/* Stuff the data into an array */
	data[0] = dx0;
	data[1] = dy0;
	data[2] = sx0;
	data[3] = sy0;

	data[4] = dx1;
	data[5] = dy0;
	data[6] = sx1;
	data[7] = sy0;

	data[8] = dx1;
	data[9] = dy1;
	data[10] = sx1;
	data[11] = sy1;

	data[12] = dx0;
	data[13] = dy1;
	data[14] = sx0;
	data[15] = sy1;

	/* Copy the data into the buffer */
	SDL_memcpy(
		mtlGetBufferContents(renderer->backbufferDrawBuffer),
		data,
		sizeof(data)
	);
}

static void METAL_INTERNAL_BlitFramebuffer(
	MetalRenderer *renderer,
	MTLTexture *srcTex,
	MTLTexture *dstTex
) {
	MTLRenderPassDescriptor *pass;
	MTLRenderCommandEncoder *rce;

	pass = mtlMakeRenderPassDescriptor();
	mtlSetAttachmentTexture(
		mtlGetColorAttachment(pass, 0),
		dstTex
	);
	rce = mtlMakeRenderCommandEncoder(
		renderer->commandBuffer,
		pass
	);
	mtlSetRenderPipelineState(rce, renderer->backbufferPipeline);
	mtlSetVertexBuffer(rce, renderer->backbufferDrawBuffer, 0, 0);
	mtlSetFragmentTexture(rce, srcTex, 0);
	mtlSetFragmentSamplerState(rce, renderer->backbufferSamplerState, 0);
	mtlDrawIndexedPrimitives(
		rce,
		MTLPrimitiveTypeTriangle,
		6,
		MTLIndexTypeUInt16,
		renderer->backbufferDrawBuffer,
		16 * sizeof(float),
		1
	);
	mtlEndEncoding(rce);
}

static void METAL_SwapBuffers(
	FNA3D_Renderer *driverData,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	FNA3D_Rect srcRect, dstRect;
	CGSize drawableSize;
	MTLDrawable *drawable;

	/* Just in case Present() is called
	 * before any rendering happens...
	 */
	METAL_INTERNAL_BeginFrame(driverData);

	/* Bind the backbuffer and finalize rendering */
	METAL_SetRenderTargets(
		driverData,
		NULL,
		0,
		NULL,
		FNA3D_DEPTHFORMAT_NONE,
		0
	);
	METAL_INTERNAL_EndPass(renderer);

	/* Determine the regions to present */
	drawableSize = mtlGetDrawableSize(renderer->layer);
	if (sourceRectangle != NULL)
	{
		srcRect.x = sourceRectangle->x;
		srcRect.y = sourceRectangle->y;
		srcRect.w = sourceRectangle->w;
		srcRect.h = sourceRectangle->h;
	}
	else
	{
		srcRect.x = 0;
		srcRect.y = 0;
		srcRect.w = renderer->backbuffer->width;
		srcRect.h = renderer->backbuffer->height;
	}
	if (destinationRectangle != NULL)
	{
		dstRect.x = destinationRectangle->x;
		dstRect.y = destinationRectangle->y;
		dstRect.w = destinationRectangle->w;
		dstRect.h = destinationRectangle->h;
	}
	else
	{
		dstRect.x = 0;
		dstRect.y = 0;
		dstRect.w = (int32_t) drawableSize.width;
		dstRect.h = (int32_t) drawableSize.height;
	}

	/* Update the cached vertex buffer, if needed */
	if (	renderer->backbufferSizeChanged ||
		renderer->prevSrcRect.x != srcRect.x ||
		renderer->prevSrcRect.y != srcRect.y ||
		renderer->prevSrcRect.w != srcRect.w ||
		renderer->prevSrcRect.h != srcRect.h ||
		renderer->prevDstRect.x != dstRect.x ||
		renderer->prevDstRect.y != dstRect.y ||
		renderer->prevDstRect.w != dstRect.w ||
		renderer->prevDstRect.h != dstRect.h	)
	{
		METAL_INTERNAL_UpdateBackbufferVertexBuffer(
			renderer,
			&srcRect,
			&dstRect,
			(int32_t) drawableSize.width,
			(int32_t) drawableSize.height
		);
	}

	/* Get the next drawable */
	drawable = mtlNextDrawable(renderer->layer);

	/* "Blit" the backbuffer to the drawable */
	if (srcRect.w != 0 && srcRect.h != 0 && dstRect.w != 0 && dstRect.h != 0)
	{
		METAL_INTERNAL_BlitFramebuffer(
			renderer,
			renderer->currentAttachments[0],
			mtlGetTextureFromDrawable(drawable)
		);
	}

	/* Commit the command buffer for presentation */
	mtlPresentDrawable(renderer->commandBuffer, drawable);
	mtlCommitCommandBuffer(renderer->commandBuffer);

	/* Track the committed command buffer */
	objc_retain(renderer->commandBuffer);
	renderer->committedCommandBuffer = renderer->commandBuffer;
	renderer->commandBuffer = NULL;

	/* Release allocations from the past frame */
	objc_autoreleasePoolPop(renderer->pool);

	/* Reset buffer and texture internal states */
	METAL_INTERNAL_ResetBuffers(renderer);
	METAL_INTERNAL_ResetTextures(renderer);
	MOJOSHADER_mtlEndFrame();

	/* We're done here. */
	renderer->frameInProgress = 0;
}

/* Drawing */

static void METAL_Clear(
	FNA3D_Renderer *driverData,
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	uint8_t clearTarget = (options & FNA3D_CLEAROPTIONS_TARGET) == FNA3D_CLEAROPTIONS_TARGET;
	uint8_t clearDepth = (options & FNA3D_CLEAROPTIONS_DEPTHBUFFER) == FNA3D_CLEAROPTIONS_DEPTHBUFFER;
	uint8_t clearStencil = (options & FNA3D_CLEAROPTIONS_STENCIL) == FNA3D_CLEAROPTIONS_STENCIL;

	if (clearTarget)
	{
		SDL_memcpy(&renderer->clearColor, color, sizeof(FNA3D_Vec4));
		renderer->shouldClearColor = 1;
	}
	if (clearDepth)
	{
		renderer->clearDepth = depth;
		renderer->shouldClearDepth = 1;
	}
	if (clearStencil)
	{
		renderer->clearStencil = stencil;
		renderer->shouldClearStencil = 1;
	}

	renderer->needNewRenderPass |= clearTarget | clearDepth | clearStencil;
}

static void METAL_DrawInstancedPrimitives(
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
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalBuffer *indexBuffer = (MetalBuffer*) indices;
	MetalSubBuffer subbuf = indexBuffer->subBuffers[
		indexBuffer->currentSubBufferIndex
	];
	int32_t totalIndexOffset;

	METAL_INTERNAL_MarkBufferAsBound(renderer, indexBuffer);
	totalIndexOffset = (
		(startIndex * IndexSize(indexElementSize)) +
		subbuf.offset
	);
	mtlDrawIndexedPrimitives(
		renderer->renderCommandEncoder,
		XNAToMTL_Primitive[primitiveType],
		PrimitiveVerts(primitiveType, primitiveCount),
		XNAToMTL_IndexType[indexElementSize],
		subbuf.buffer,
		totalIndexOffset,
		instanceCount
	);
}

static void METAL_DrawIndexedPrimitives(
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
	METAL_DrawInstancedPrimitives(
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

static void METAL_DrawPrimitives(
	FNA3D_Renderer *driverData,
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	mtlDrawPrimitives(
		renderer->renderCommandEncoder,
		XNAToMTL_Primitive[primitiveType],
		vertexStart,
		PrimitiveVerts(primitiveType, primitiveCount)
	);
}

/* Mutable Render States */

static void METAL_SetViewport(FNA3D_Renderer *driverData, FNA3D_Viewport *viewport)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	FNA3D_Viewport vp = *viewport;

	if (	vp.x != renderer->viewport.x ||
		vp.y != renderer->viewport.y ||
		vp.w != renderer->viewport.w ||
		vp.h != renderer->viewport.h ||
		vp.minDepth != renderer->viewport.minDepth ||
		vp.maxDepth != renderer->viewport.maxDepth	)
	{
		renderer->viewport = vp;
		METAL_INTERNAL_SetEncoderViewport(renderer); /* Dynamic state! */
	}
}

static void METAL_SetScissorRect(FNA3D_Renderer *driverData, FNA3D_Rect *scissor)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	if (	scissor->x != renderer->scissorRect.x ||
		scissor->y != renderer->scissorRect.y ||
		scissor->w != renderer->scissorRect.w ||
		scissor->h != renderer->scissorRect.h	)
	{
		renderer->scissorRect = *scissor;
		METAL_INTERNAL_SetEncoderScissorRect(renderer); /* Dynamic state! */
	}
}

static void METAL_GetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	SDL_memcpy(blendFactor, &renderer->blendColor, sizeof(FNA3D_Color));
}

static void METAL_SetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	if (	renderer->blendColor.r != blendFactor->r ||
		renderer->blendColor.g != blendFactor->g ||
		renderer->blendColor.b != blendFactor->b ||
		renderer->blendColor.a != blendFactor->a	)
	{
		renderer->blendColor.r = blendFactor->r;
		renderer->blendColor.g = blendFactor->g;
		renderer->blendColor.b = blendFactor->b;
		renderer->blendColor.a = blendFactor->a;
		METAL_INTERNAL_SetEncoderBlendColor(renderer);
	}
}

static int32_t METAL_GetMultiSampleMask(FNA3D_Renderer *driverData)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	return renderer->multiSampleMask;
}

static void METAL_SetMultiSampleMask(FNA3D_Renderer *driverData, int32_t mask)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	renderer->multiSampleMask = mask;
	/* FIXME: Metal does not support multisample masks. Workarounds...? */
}

static int32_t METAL_GetReferenceStencil(FNA3D_Renderer *driverData)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	return renderer->stencilRef;
}

static void METAL_SetReferenceStencil(FNA3D_Renderer *driverData, int32_t ref)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	if (renderer->stencilRef != ref)
	{
		renderer->stencilRef = ref;
		METAL_INTERNAL_SetEncoderStencilReferenceValue(renderer);
	}
}

/* Immutable Render States */

static void METAL_SetBlendState(
	FNA3D_Renderer *driverData,
	FNA3D_BlendState *blendState
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	SDL_memcpy(
		&renderer->blendState,
		blendState,
		sizeof(FNA3D_BlendState)
	);
	METAL_SetBlendFactor(
		driverData,
		&blendState->blendFactor
	); /* Dynamic state! */
}

static void METAL_SetDepthStencilState(
	FNA3D_Renderer *driverData,
	FNA3D_DepthStencilState *depthStencilState
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	SDL_memcpy(
		&renderer->depthStencilState,
		depthStencilState,
		sizeof(FNA3D_DepthStencilState)
	);
	METAL_SetReferenceStencil(
		driverData,
		depthStencilState->referenceStencil
	); /* Dynamic state! */
}

static void METAL_ApplyRasterizerState(
	FNA3D_Renderer *driverData,
	FNA3D_RasterizerState *rasterizerState
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	float realDepthBias;

	if (rasterizerState->scissorTestEnable != renderer->scissorTestEnable)
	{
		renderer->scissorTestEnable = rasterizerState->scissorTestEnable;
		METAL_INTERNAL_SetEncoderScissorRect(renderer); /* Dynamic state! */
	}

	if (rasterizerState->cullMode != renderer->cullFrontFace)
	{
		renderer->cullFrontFace = rasterizerState->cullMode;
		METAL_INTERNAL_SetEncoderCullMode(renderer); /* Dynamic state! */
	}

	if (rasterizerState->fillMode != renderer->fillMode)
	{
		renderer->fillMode = rasterizerState->fillMode;
		METAL_INTERNAL_SetEncoderFillMode(renderer); /* Dynamic state! */
	}

	realDepthBias = rasterizerState->depthBias * XNAToMTL_DepthBiasScale(
		XNAToMTL_DepthFormat(renderer, renderer->currentDepthFormat)
	);
	if (	realDepthBias != renderer->depthBias ||
		rasterizerState->slopeScaleDepthBias != renderer->slopeScaleDepthBias)
	{
		renderer->depthBias = realDepthBias;
		renderer->slopeScaleDepthBias = rasterizerState->slopeScaleDepthBias;
		METAL_INTERNAL_SetEncoderDepthBias(renderer); /* Dynamic state! */
	}

	if (rasterizerState->multiSampleAntiAlias != renderer->multiSampleEnable)
	{
		renderer->multiSampleEnable = rasterizerState->multiSampleAntiAlias;
		/* FIXME: Metal does not support toggling MSAA. Workarounds...? */
	}
}

static void METAL_VerifySampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture *mtlTexture = (MetalTexture*) texture;
	MTLSamplerState *mtlSamplerState;

	if (texture == NULL)
	{
		if (renderer->textures[index] != &NullTexture)
		{
			renderer->textures[index] = &NullTexture;
			renderer->textureNeedsUpdate[index] = 1;
		}
		if (renderer->samplers[index] == NULL)
		{
			/* Some shaders require non-null samplers
			 * even if they aren't actually used.
			 * -caleb
			 */
			renderer->samplers[index] = METAL_INTERNAL_FetchSamplerState(
				renderer,
				sampler,
				0
			);
			renderer->samplerNeedsUpdate[index] = 1;
		}
		return;
	}

	/* Bind the correct texture */
	if (mtlTexture != renderer->textures[index])
	{
		renderer->textures[index] = mtlTexture;
		renderer->textureNeedsUpdate[index] = 1;
	}
	METAL_INTERNAL_MarkTextureAsBound(renderer, mtlTexture);

	/* Update the sampler state, if needed */
	mtlSamplerState = METAL_INTERNAL_FetchSamplerState(
		renderer,
		sampler,
		mtlTexture->hasMipmaps
	);
	if (mtlSamplerState != renderer->samplers[index])
	{
		renderer->samplers[index] = mtlSamplerState;
		renderer->samplerNeedsUpdate[index] = 1;
	}
}

static void METAL_VerifyVertexSampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	METAL_VerifySampler(
		driverData,
		MAX_TEXTURE_SAMPLERS + index,
		texture,
		sampler
	);
}

static void METAL_INTERNAL_BindResources(MetalRenderer *renderer)
{
	int32_t i;
	MTLBuffer *ubo;
	int32_t vOff, fOff;
	MTLDepthStencilState *depthStencilState;
	MTLRenderPipelineState *pipelineState;

	/* Bind textures and their sampler states */
	for (i = 0; i < MAX_TOTAL_SAMPLERS; i += 1)
	{
		if (renderer->textureNeedsUpdate[i])
		{
			if (i < MAX_TEXTURE_SAMPLERS)
			{
				mtlSetFragmentTexture(
					renderer->renderCommandEncoder,
					renderer->textures[i]->handle,
					i
				);
			}
			else
			{
				mtlSetVertexTexture(
					renderer->renderCommandEncoder,
					renderer->textures[i]->handle,
					i - MAX_TEXTURE_SAMPLERS
				);
			}
			renderer->textureNeedsUpdate[i] = 0;
		}
		if (renderer->samplerNeedsUpdate[i])
		{
			if (i < MAX_TEXTURE_SAMPLERS)
			{
				mtlSetFragmentSamplerState(
					renderer->renderCommandEncoder,
					renderer->samplers[i],
					i
				);
			}
			else
			{
				mtlSetVertexSamplerState(
					renderer->renderCommandEncoder,
					renderer->samplers[i],
					i - MAX_TEXTURE_SAMPLERS
				);
			}
			renderer->samplerNeedsUpdate[i] = 0;
		}
	}

	/* In MojoShader output, the uniform register is always 16 */
	#define UNIFORM_REG 16

	/* Bind the uniform buffers */
	MOJOSHADER_mtlGetUniformData((void**) &ubo, &vOff, &fOff);
	if (ubo != renderer->ldUniformBuffer)
	{
		mtlSetVertexBuffer(
			renderer->renderCommandEncoder,
			ubo,
			vOff,
			UNIFORM_REG
		);
		mtlSetFragmentBuffer(
			renderer->renderCommandEncoder,
			ubo,
			fOff,
			UNIFORM_REG
		);
		renderer->ldUniformBuffer = ubo;
		renderer->ldVertUniformOffset = vOff;
		renderer->ldFragUniformOffset = fOff;
	}

	if (vOff != renderer->ldVertUniformOffset)
	{
		mtlSetVertexBufferOffset(
			renderer->renderCommandEncoder,
			vOff,
			UNIFORM_REG
		);
		renderer->ldVertUniformOffset = vOff;
	}

	if (fOff != renderer->ldFragUniformOffset)
	{
		mtlSetFragmentBufferOffset(
			renderer->renderCommandEncoder,
			fOff,
			UNIFORM_REG
		);
		renderer->ldFragUniformOffset = fOff;
	}

	#undef UNIFORM_REG

	/* Bind the depth-stencil state */
	depthStencilState = METAL_INTERNAL_FetchDepthStencilState(renderer);
	if (depthStencilState != renderer->ldDepthStencilState)
	{
		mtlSetDepthStencilState(
			renderer->renderCommandEncoder,
			depthStencilState
		);
		renderer->ldDepthStencilState = depthStencilState;
	}

	/* Finally, bind the pipeline state */
	pipelineState = METAL_INTERNAL_FetchRenderPipeline(renderer);
	if (pipelineState != renderer->ldPipelineState)
	{
		mtlSetRenderPipelineState(
			renderer->renderCommandEncoder,
			pipelineState
		);
		renderer->ldPipelineState = pipelineState;
	}
}

static void METAL_ApplyVertexBufferBindings(
	FNA3D_Renderer *driverData,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalBuffer *vertexBuffer;
	MetalSubBuffer subbuf;
	int32_t i, offset;

	/* Translate the bindings array into a descriptor */
	renderer->currentVertexDescriptor = METAL_INTERNAL_FetchVertexBufferBindingsDescriptor(
		renderer,
		bindings,
		numBindings
	);

	/* Prepare for rendering */
	METAL_INTERNAL_UpdateRenderPass(renderer);
	METAL_INTERNAL_BindResources(renderer);

	/* Bind the vertex buffers */
	for (i = 0; i < numBindings; i += 1)
	{
		vertexBuffer = (MetalBuffer*) bindings[i].vertexBuffer;
		if (vertexBuffer == NULL)
		{
			continue;
		}

		subbuf = vertexBuffer->subBuffers[
			vertexBuffer->currentSubBufferIndex
		];

		offset = subbuf.offset + (
			(bindings[i].vertexOffset + baseVertex) *
			bindings[i].vertexDeclaration.vertexStride
		);

		METAL_INTERNAL_MarkBufferAsBound(renderer, vertexBuffer);
		if (renderer->ldVertexBuffers[i] != subbuf.buffer)
		{
			mtlSetVertexBuffer(
				renderer->renderCommandEncoder,
				subbuf.buffer,
				offset,
				i
			);
			renderer->ldVertexBuffers[i] = subbuf.buffer;
			renderer->ldVertexBufferOffsets[i] = offset;
		}
		else if (renderer->ldVertexBufferOffsets[i] != offset)
		{
			mtlSetVertexBufferOffset(
				renderer->renderCommandEncoder,
				offset,
				i
			);
			renderer->ldVertexBufferOffsets[i] = offset;
		}
	}
}

/* Render Targets */

static void METAL_SetRenderTargets(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalBackbuffer *bb;
	MetalRenderbuffer *rb;
	MetalTexture *tex;
	int32_t i;

	/* Perform any pending clears before switching render targets */
	if (	renderer->shouldClearColor ||
		renderer->shouldClearDepth ||
		renderer->shouldClearStencil	)
	{
		METAL_INTERNAL_UpdateRenderPass(renderer);
	}

	/* Force an update to the render pass */
	renderer->needNewRenderPass = 1;

	/* Reset attachments */
	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		renderer->currentAttachments[i] = NULL;
		renderer->currentColorFormats[i] = MTLPixelFormatInvalid;
		renderer->currentMSAttachments[i] = NULL;
		renderer->currentAttachmentSlices[i] = 0;
	}
	renderer->currentDepthStencilBuffer = NULL;
	renderer->currentDepthFormat = FNA3D_DEPTHFORMAT_NONE;
	renderer->currentSampleCount = 0;

	/* Bind the backbuffer, if applicable */
	if (numRenderTargets <= 0)
	{
		bb = renderer->backbuffer;
		renderer->currentAttachments[0] = bb->colorBuffer;
		renderer->currentColorFormats[0] = XNAToMTL_TextureFormat[
			bb->surfaceFormat
		];
		renderer->currentDepthStencilBuffer = bb->depthStencilBuffer;
		renderer->currentDepthFormat = bb->depthFormat;
		renderer->preserveDepthStencil = bb->preserveDepthStencil;
		renderer->currentSampleCount = bb->multiSampleCount;
		renderer->currentMSAttachments[0] = bb->multiSampleColorBuffer;
		renderer->currentAttachmentSlices[0] = 0;
		return;
	}

	/* Update color buffers */
	for (i = 0; i < numRenderTargets; i += 1)
	{
		if (renderTargets[i].type == FNA3D_RENDERTARGET_TYPE_CUBE)
		{
			renderer->currentAttachmentSlices[i] = renderTargets[i].cube.face;
		}
		else
		{
			renderer->currentAttachmentSlices[i] = 0;
		}
		if (renderTargets[i].colorBuffer != NULL)
		{
			rb = (MetalRenderbuffer*) renderTargets[i].colorBuffer;
			renderer->currentAttachments[i] = rb->handle;
			renderer->currentColorFormats[i] = rb->pixelFormat;
			renderer->currentSampleCount = rb->multiSampleCount;
			renderer->currentMSAttachments[i] = rb->multiSampleHandle;
		}
		else
		{
			tex = (MetalTexture*) renderTargets[i].texture;
			renderer->currentAttachments[i] = tex->handle;
			renderer->currentColorFormats[i] = XNAToMTL_TextureFormat[
				tex->format
			];
			renderer->currentSampleCount = 0;
		}
	}

	/* Update depth stencil buffer */
	renderer->currentDepthStencilBuffer = (
		depthStencilBuffer == NULL ?
			NULL :
			((MetalRenderbuffer*) depthStencilBuffer)->handle
	);
	renderer->currentDepthFormat = (
		depthStencilBuffer == NULL ?
			FNA3D_DEPTHFORMAT_NONE :
			depthFormat
	);
	renderer->preserveDepthStencil = preserveTargetContents;
}

static void METAL_ResolveTarget(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *target
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture *texture = (MetalTexture*) target->texture;
	MTLBlitCommandEncoder *blit;

	/* The target is resolved at the end of each render pass. */

	/* If the target has mipmaps, regenerate them now. */
	if (target->levelCount > 1)
	{
		METAL_INTERNAL_EndPass(renderer);

		blit = mtlMakeBlitCommandEncoder(renderer->commandBuffer);
		mtlGenerateMipmapsForTexture(
			blit,
			texture->handle
		);
		mtlEndEncoding(blit);

		renderer->needNewRenderPass = 1;
	}
}

/* Backbuffer Functions */

static void METAL_INTERNAL_CreateFramebuffer(
	MetalRenderer *renderer,
	FNA3D_PresentationParameters *presentationParameters
) {
	int32_t newWidth, newHeight;
	MTLTextureDescriptor *colorBufferDesc;
	MTLTextureDescriptor *depthStencilBufferDesc;
	MetalBackbuffer *bb = renderer->backbuffer;

	/* Update the backbuffer size */
	newWidth = presentationParameters->backBufferWidth;
	newHeight = presentationParameters->backBufferHeight;
	if (bb->width != newWidth || bb->height != newHeight)
	{
		renderer->backbufferSizeChanged = 1;
	}
	bb->width = newWidth;
	bb->height = newHeight;

	/* Update other presentation parameters */
	bb->surfaceFormat = presentationParameters->backBufferFormat;
	bb->depthFormat = presentationParameters->depthStencilFormat;
	bb->multiSampleCount = METAL_INTERNAL_GetCompatibleSampleCount(
		renderer,
		presentationParameters->multiSampleCount
	);

	/* Update color buffer to the new resolution */
	colorBufferDesc = mtlMakeTexture2DDescriptor(
		XNAToMTL_TextureFormat[bb->surfaceFormat],
		bb->width,
		bb->height,
		0
	);
	mtlSetStorageMode(colorBufferDesc, MTLStorageModePrivate);
	mtlSetTextureUsage(
		colorBufferDesc,
		MTLTextureUsageRenderTarget | MTLTextureUsageShaderRead
	);
	bb->colorBuffer = mtlNewTexture(renderer->device, colorBufferDesc);
	if (bb->multiSampleCount > 0)
	{
		mtlSetTextureType(colorBufferDesc, MTLTextureType2DMultisample);
		mtlSetTextureSampleCount(colorBufferDesc, bb->multiSampleCount);
		mtlSetTextureUsage(colorBufferDesc, MTLTextureUsageRenderTarget);
		bb->multiSampleColorBuffer = mtlNewTexture(
			renderer->device,
			colorBufferDesc
		);
	}

	/* Update the depth/stencil buffer, if applicable */
	if (bb->depthFormat != FNA3D_DEPTHFORMAT_NONE)
	{
		depthStencilBufferDesc = mtlMakeTexture2DDescriptor(
			XNAToMTL_DepthFormat(renderer, bb->depthFormat),
			bb->width,
			bb->height,
			0
		);
		mtlSetStorageMode(depthStencilBufferDesc, MTLStorageModePrivate);
		mtlSetTextureUsage(depthStencilBufferDesc, MTLTextureUsageRenderTarget);
		if (bb->multiSampleCount > 0)
		{
			mtlSetTextureType(
				depthStencilBufferDesc,
				MTLTextureType2DMultisample
			);
			mtlSetTextureSampleCount(
				depthStencilBufferDesc,
				bb->multiSampleCount
			);
		}
		bb->depthStencilBuffer = mtlNewTexture(
			renderer->device,
			depthStencilBufferDesc
		);
		bb->preserveDepthStencil = (
			presentationParameters->renderTargetUsage !=
				FNA3D_RENDERTARGETUSAGE_DISCARDCONTENTS
		);
	}

	/* This is the default render target */
	METAL_SetRenderTargets(
		(FNA3D_Renderer*) renderer,
		NULL,
		0,
		NULL,
		FNA3D_DEPTHFORMAT_NONE,
		0
	);
}

static void METAL_INTERNAL_DestroyFramebuffer(MetalRenderer *renderer)
{
	objc_release(renderer->backbuffer->colorBuffer);
	renderer->backbuffer->colorBuffer = NULL;

	objc_release(renderer->backbuffer->multiSampleColorBuffer);
	renderer->backbuffer->multiSampleColorBuffer = NULL;

	objc_release(renderer->backbuffer->depthStencilBuffer);
	renderer->backbuffer->depthStencilBuffer = NULL;
}

static void METAL_INTERNAL_SetPresentationInterval(
	MetalRenderer *renderer,
	FNA3D_PresentInterval presentInterval
) {
	/* Toggling vsync is only supported on macOS 10.13+ */
	if (!RespondsToSelector(renderer->layer, selDisplaySyncEnabled))
	{
		FNA3D_LogWarn(
			"Cannot set presentation interval! "
			"Only vsync is supported."
		);
		return;
	}

	if (	presentInterval == FNA3D_PRESENTINTERVAL_DEFAULT ||
		presentInterval == FNA3D_PRESENTINTERVAL_ONE	)
	{
		mtlSetDisplaySyncEnabled(renderer->layer, 1);
	}
	else if (presentInterval == FNA3D_PRESENTINTERVAL_IMMEDIATE)
	{
		mtlSetDisplaySyncEnabled(renderer->layer, 0);
	}
	else if (presentInterval == FNA3D_PRESENTINTERVAL_TWO)
	{
		/* FIXME:
		 * There is no built-in support for
		 * present-every-other-frame in Metal.
		 * We could work around this, but do
		 * any games actually use this mode...?
		 * -caleb
		 */
		mtlSetDisplaySyncEnabled(renderer->layer, 1);
	}
	else
	{
		SDL_assert(0 && "Unrecognized PresentInterval!");
	}
}

static void METAL_ResetBackbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_PresentationParameters *presentationParameters
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	METAL_INTERNAL_DestroyFramebuffer(renderer);
	METAL_INTERNAL_CreateFramebuffer(
		renderer,
		presentationParameters
	);
	METAL_INTERNAL_SetPresentationInterval(
		renderer,
		presentationParameters->presentationInterval
	);
}

static void METAL_ReadBackbuffer(
	FNA3D_Renderer *driverData,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	void* data,
	int32_t dataLength
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture backbufferTexture;

	/* Create a pseudo-texture we can feed to GetTextureData2D.
	 * These are the only members we need to initialize.
	 * -caleb
	 */
	backbufferTexture.width = renderer->backbuffer->width;
	backbufferTexture.height = renderer->backbuffer->height;
	backbufferTexture.format = renderer->backbuffer->surfaceFormat;
	backbufferTexture.hasMipmaps = 0;
	backbufferTexture.isPrivate = 1;

	METAL_GetTextureData2D(
		driverData,
		(FNA3D_Texture*) &backbufferTexture,
		x,
		y,
		w,
		h,
		0,
		data,
		dataLength
	);
}

static void METAL_GetBackbufferSize(
	FNA3D_Renderer *driverData,
	int32_t *w,
	int32_t *h
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	*w = renderer->backbuffer->width;
	*h = renderer->backbuffer->height;
}

static FNA3D_SurfaceFormat METAL_GetBackbufferSurfaceFormat(FNA3D_Renderer *driverData)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	return renderer->backbuffer->surfaceFormat;
}

static FNA3D_DepthFormat METAL_GetBackbufferDepthFormat(FNA3D_Renderer *driverData)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	return renderer->backbuffer->depthFormat;
}

static int32_t METAL_GetBackbufferMultiSampleCount(FNA3D_Renderer *driverData)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	return renderer->backbuffer->multiSampleCount;
}

/* Textures */

static FNA3D_Texture* METAL_CreateTexture2D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MTLTextureDescriptor *desc = mtlMakeTexture2DDescriptor(
		XNAToMTL_TextureFormat[format],
		width,
		height,
		levelCount > 1
	);

	if (isRenderTarget)
	{
		mtlSetStorageMode(desc, MTLStorageModePrivate);
		mtlSetTextureUsage(
			desc,
			MTLTextureUsageRenderTarget | MTLTextureUsageShaderRead
		);
	}

	return (FNA3D_Texture*) CreateTexture(
		renderer,
		mtlNewTexture(renderer->device, desc),
		format,
		width,
		height,
		levelCount,
		isRenderTarget
	);
}

static FNA3D_Texture* METAL_CreateTexture3D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MTLTextureDescriptor *desc = mtlMakeTexture2DDescriptor(
		XNAToMTL_TextureFormat[format],
		width,
		height,
		levelCount > 1
	);

	/* Make it 3D! */
	mtlSetTextureDepth(desc, depth);
	mtlSetTextureType(desc, MTLTextureType3DTexture);

	return (FNA3D_Texture*) CreateTexture(
		renderer,
		mtlNewTexture(renderer->device, desc),
		format,
		width,
		height,
		levelCount,
		0
	);
}

static FNA3D_Texture* METAL_CreateTextureCube(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MTLTextureDescriptor *desc = mtlMakeTextureCubeDescriptor(
		XNAToMTL_TextureFormat[format],
		size,
		levelCount > 1
	);

	if (isRenderTarget)
	{
		mtlSetStorageMode(desc, MTLStorageModePrivate);
		mtlSetTextureUsage(
			desc,
			MTLTextureUsageRenderTarget | MTLTextureUsageShaderRead
		);
	}

	return (FNA3D_Texture*) CreateTexture(
		renderer,
		mtlNewTexture(renderer->device, desc),
		format,
		size,
		size,
		levelCount,
		isRenderTarget
	);
}

static void METAL_AddDisposeTexture(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture *mtlTexture = (MetalTexture*) texture;
	int32_t i;

	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		if (mtlTexture->handle == renderer->currentAttachments[i])
		{
			renderer->currentAttachments[i] = NULL;
		}
	}
	for (i = 0; i < MAX_TOTAL_SAMPLERS; i += 1)
	{
		if (mtlTexture->handle == renderer->textures[i]->handle)
		{
			renderer->textures[i] = &NullTexture;
			renderer->textureNeedsUpdate[i] = 1;
		}
	}

	objc_release(mtlTexture->handle);
	mtlTexture->handle = NULL;

	SDL_free(mtlTexture);
}

static void METAL_SetTextureData2D(
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
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture *mtlTexture = (MetalTexture*) texture;
	MTLTexture *handle = mtlTexture->handle;
	MTLBlitCommandEncoder *blit;

	MTLOrigin origin = {x, y, 0};
	MTLSize size = {w, h, 1};
	MTLRegion region = {origin, size};

	if (mtlTexture->bound)
	{
		METAL_INTERNAL_Flush(renderer);
	}

	if (mtlTexture->isPrivate)
	{
		/* We need an active command buffer */
		METAL_INTERNAL_BeginFrame(driverData);

		/* Fetch a CPU-accessible texture */
		handle = METAL_INTERNAL_FetchTransientTexture(
			renderer,
			mtlTexture
		);
	}

	/* Write the data */
	mtlReplaceRegion(
		handle,
		region,
		level,
		0,
		data,
		BytesPerRow(w, mtlTexture->format),
		0
	);

	/* Blit the temp texture to the actual texture */
	if (mtlTexture->isPrivate)
	{
		/* End the render pass */
		METAL_INTERNAL_EndPass(renderer);

		/* Blit! */
		blit = mtlMakeBlitCommandEncoder(renderer->commandBuffer);
		mtlBlitTextureToTexture(
			blit,
			handle,
			0,
			level,
			origin,
			size,
			mtlTexture->handle,
			0,
			level,
			origin
		);

		/* Submit the blit command to the GPU and wait... */
		mtlEndEncoding(blit);
		METAL_INTERNAL_Flush(renderer);

		/* We're done with the temp texture */
		mtlSetPurgeableState(
			handle,
			MTLPurgeableStateEmpty
		);
	}
}

static void METAL_SetTextureData3D(
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
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture *mtlTexture = (MetalTexture*) texture;
	MTLOrigin origin = {x, y, z};
	MTLSize size = {w, h, d};
	MTLRegion region = {origin, size};

	if (mtlTexture->bound)
	{
		METAL_INTERNAL_Flush(renderer);
	}

	mtlReplaceRegion(
		mtlTexture->handle,
		region,
		level,
		0,
		data,
		BytesPerRow(w, mtlTexture->format),
		BytesPerImage(w, h, mtlTexture->format)
	);
}

static void METAL_SetTextureDataCube(
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
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture *mtlTexture = (MetalTexture*) texture;
	MTLTexture *handle = mtlTexture->handle;
	MTLBlitCommandEncoder *blit;

	MTLOrigin origin = {x, y, 0};
	MTLSize size = {w, h, 1};
	MTLRegion region = {origin, size};
	int32_t slice = cubeMapFace;

	if (mtlTexture->bound)
	{
		METAL_INTERNAL_Flush(renderer);
	}

	if (mtlTexture->isPrivate)
	{
		/* We need an active command buffer */
		METAL_INTERNAL_BeginFrame(driverData);

		/* Fetch a CPU-accessible texture */
		handle = METAL_INTERNAL_FetchTransientTexture(
			renderer,
			mtlTexture
		);

		/* Transient textures have no slices */
		slice = 0;
	}

	/* Write the data */
	mtlReplaceRegion(
		handle,
		region,
		level,
		slice,
		data,
		BytesPerRow(w, mtlTexture->format),
		0
	);

	/* Blit the temp texture to the actual texture */
	if (mtlTexture->isPrivate)
	{
		/* End the render pass */
		METAL_INTERNAL_EndPass(renderer);

		/* Blit! */
		blit = mtlMakeBlitCommandEncoder(renderer->commandBuffer);
		mtlBlitTextureToTexture(
			blit,
			handle,
			slice,
			level,
			origin,
			size,
			mtlTexture->handle,
			cubeMapFace,
			level,
			origin
		);

		/* Submit the blit command to the GPU and wait... */
		mtlEndEncoding(blit);
		METAL_INTERNAL_Flush(renderer);

		/* We're done with the temp texture */
		mtlSetPurgeableState(
			handle,
			MTLPurgeableStateEmpty
		);
	}
}

static void METAL_SetTextureDataYUV(
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
	uint8_t* dataPtr = (uint8_t*) data;
	MTLOrigin origin = {0, 0, 0};
	MTLSize sizeY = {yWidth, yHeight, 1};
	MTLSize sizeUV = {uvWidth, uvHeight, 1};
	MTLRegion regionY = {origin, sizeY};
	MTLRegion regionUV = {origin, sizeUV};

	mtlReplaceRegion(
		((MetalTexture*) y)->handle,
		regionY,
		0,
		0,
		dataPtr,
		yWidth,
		0
	);
	dataPtr += yWidth * yHeight;

	mtlReplaceRegion(
		((MetalTexture*) u)->handle,
		regionUV,
		0,
		0,
		dataPtr,
		uvWidth,
		0
	);
	dataPtr += uvWidth * uvHeight;

	mtlReplaceRegion(
		((MetalTexture*) v)->handle,
		regionUV,
		0,
		0,
		dataPtr,
		uvWidth,
		0
	);
}

static void METAL_GetTextureData2D(
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
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture *mtlTexture = (MetalTexture*) texture;
	MTLTexture *handle = mtlTexture->handle;
	MTLBlitCommandEncoder *blit;

	MTLOrigin origin = {x, y, 0};
	MTLSize size = {w, h, 1};
	MTLRegion region = {origin, size};

	if (mtlTexture->isPrivate)
	{
		/* We need an active command buffer */
		METAL_INTERNAL_BeginFrame(driverData);

		/* Fetch a CPU-accessible texture */
		handle = METAL_INTERNAL_FetchTransientTexture(
			renderer,
			mtlTexture
		);

		/* End the render pass */
		METAL_INTERNAL_EndPass(renderer);

		/* Blit the actual texture to a CPU-accessible texture */
		blit = mtlMakeBlitCommandEncoder(renderer->commandBuffer);
		mtlBlitTextureToTexture(
			blit,
			mtlTexture->handle,
			0,
			level,
			origin,
			size,
			handle,
			0,
			level,
			origin
		);

		/* Managed resources require explicit synchronization */
		if (renderer->isMac)
		{
			mtlSynchronizeResource(blit, handle);
		}

		/* Submit the blit command to the GPU and wait... */
		mtlEndEncoding(blit);
		METAL_INTERNAL_Flush(renderer);
	}

	mtlGetTextureBytes(
		handle,
		data,
		BytesPerRow(w, mtlTexture->format),
		0,
		region,
		level,
		0
	);

	if (mtlTexture->isPrivate)
	{
		/* We're done with the temp texture */
		mtlSetPurgeableState(
			handle,
			MTLPurgeableStateEmpty
		);
	}
}

static void METAL_GetTextureData3D(
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
	MetalTexture *mtlTexture = (MetalTexture*) texture;
	MTLOrigin origin = {x, y, z};
	MTLSize size = {w, h, d};
	MTLRegion region = {origin, size};

	mtlGetTextureBytes(
		mtlTexture->handle,
		data,
		BytesPerRow(w, mtlTexture->format),
		BytesPerImage(w, h, mtlTexture->format),
		region,
		level,
		0
	);
}

static void METAL_GetTextureDataCube(
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
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalTexture *mtlTexture = (MetalTexture*) texture;
	MTLTexture *handle = mtlTexture->handle;
	MTLBlitCommandEncoder *blit;

	MTLOrigin origin = {x, y, 0};
	MTLSize size = {w, h, 1};
	MTLRegion region = {origin, size};
	int32_t slice = cubeMapFace;

	if (mtlTexture->isPrivate)
	{
		/* We need an active command buffer */
		METAL_INTERNAL_BeginFrame(driverData);

		/* Fetch a CPU-accessible texture */
		handle = METAL_INTERNAL_FetchTransientTexture(
			renderer,
			mtlTexture
		);

		/* Transient textures have no slices */
		slice = 0;

		/* End the render pass */
		METAL_INTERNAL_EndPass(renderer);

		/* Blit the actual texture to a CPU-accessible texture */
		blit = mtlMakeBlitCommandEncoder(renderer->commandBuffer);
		mtlBlitTextureToTexture(
			blit,
			mtlTexture->handle,
			cubeMapFace,
			level,
			origin,
			size,
			handle,
			slice,
			level,
			origin
		);

		/* Managed resources require explicit synchronization */
		if (renderer->isMac)
		{
			mtlSynchronizeResource(blit, handle);
		}

		/* Submit the blit command to the GPU and wait... */
		mtlEndEncoding(blit);
		METAL_INTERNAL_Flush(renderer);
	}

	mtlGetTextureBytes(
		handle,
		data,
		BytesPerRow(w, mtlTexture->format),
		0,
		region,
		level,
		0
	);

	if (mtlTexture->isPrivate)
	{
		/* We're done with the temp texture */
		mtlSetPurgeableState(
			handle,
			MTLPurgeableStateEmpty
		);
	}
}

/* Renderbuffers */

static FNA3D_Renderbuffer* METAL_GenColorRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MTLPixelFormat pixelFormat = XNAToMTL_TextureFormat[format];
	int32_t sampleCount = METAL_INTERNAL_GetCompatibleSampleCount(
		renderer,
		multiSampleCount
	);
	MTLTextureDescriptor *desc;
	MTLTexture *multiSampleTexture;
	MetalRenderbuffer *result;

	/* Generate a multisample texture */
	desc = mtlMakeTexture2DDescriptor(
		pixelFormat,
		width,
		height,
		0
	);
	mtlSetStorageMode(desc, MTLStorageModePrivate);
	mtlSetTextureUsage(desc, MTLTextureUsageRenderTarget);
	mtlSetTextureType(desc, MTLTextureType2DMultisample);
	mtlSetTextureSampleCount(desc, sampleCount);
	multiSampleTexture = mtlNewTexture(
		renderer->device,
		desc
	);

	/* Create and return the renderbuffer */
	result = SDL_malloc(sizeof(MetalRenderbuffer));
	result->handle = ((MetalTexture*) texture)->handle;
	result->pixelFormat = pixelFormat;
	result->multiSampleCount = sampleCount;
	result->multiSampleHandle = multiSampleTexture;
	return (FNA3D_Renderbuffer*) result;
}

static FNA3D_Renderbuffer* METAL_GenDepthStencilRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MTLPixelFormat pixelFormat = XNAToMTL_DepthFormat(renderer, format);
	int32_t sampleCount = METAL_INTERNAL_GetCompatibleSampleCount(
		renderer,
		multiSampleCount
	);
	MTLTextureDescriptor *desc;
	MTLTexture *depthTexture;
	MetalRenderbuffer *result;

	/* Generate a depth texture */
	desc = mtlMakeTexture2DDescriptor(
		pixelFormat,
		width,
		height,
		0
	);
	mtlSetStorageMode(desc, MTLStorageModePrivate);
	mtlSetTextureUsage(desc, MTLTextureUsageRenderTarget);
	if (multiSampleCount > 0)
	{
		mtlSetTextureType(desc, MTLTextureType2DMultisample);
		mtlSetTextureSampleCount(desc, sampleCount);
	}
	depthTexture = mtlNewTexture(
		renderer->device,
		desc
	);

	/* Create and return the renderbuffer */
	result = SDL_malloc(sizeof(MetalRenderbuffer));
	result->handle = depthTexture;
	result->pixelFormat = pixelFormat;
	result->multiSampleCount = sampleCount;
	result->multiSampleHandle = NULL;
	return (FNA3D_Renderbuffer*) result;
}

static void METAL_AddDisposeRenderbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Renderbuffer *renderbuffer
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalRenderbuffer *mtlRenderbuffer = (MetalRenderbuffer*) renderbuffer;
	uint8_t isDepthStencil = (mtlRenderbuffer->multiSampleHandle == NULL);
	int32_t i;

	if (isDepthStencil)
	{
		if (mtlRenderbuffer->handle == renderer->currentDepthStencilBuffer)
		{
			renderer->currentDepthStencilBuffer = NULL;
		}
		objc_release(mtlRenderbuffer->handle);
		mtlRenderbuffer->handle = NULL;
	}
	else
	{
		for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
		{
			if (mtlRenderbuffer->multiSampleHandle == renderer->currentMSAttachments[i])
			{
				renderer->currentMSAttachments[i] = NULL;
			}
		}
		objc_release(mtlRenderbuffer->multiSampleHandle);
		mtlRenderbuffer->multiSampleHandle = NULL;

		/* Don't release the regular handle since
		 * it's owned by the associated FNA3D_Texture.
		 */
	}
	SDL_free(mtlRenderbuffer);
}

/* Vertex Buffers */

static FNA3D_Buffer* METAL_GenVertexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	/* Note that dynamic and usage are NOT used! */
	return METAL_INTERNAL_CreateBuffer(driverData, sizeInBytes);
}

static void METAL_AddDisposeVertexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	METAL_INTERNAL_DestroyBuffer(driverData, buffer);
}

static void METAL_SetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
) {
	/* FIXME: Staging buffer for elementSizeInBytes < vertexStride! */
	METAL_INTERNAL_SetBufferData(
		driverData,
		buffer,
		offsetInBytes,
		data,
		elementCount * vertexStride,
		options
	);
}

static void METAL_GetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
) {
	MetalBuffer *mtlBuffer = (MetalBuffer*) buffer;
	uint8_t *dataBytes, *cpy, *src, *dst;
	uint8_t useStagingBuffer;
	int32_t i;

	dataBytes = (uint8_t*) data;
	useStagingBuffer = elementSizeInBytes < vertexStride;
	if (useStagingBuffer)
	{
		cpy = (uint8_t*) SDL_malloc(elementCount * vertexStride);
	}
	else
	{
		cpy = dataBytes;
	}

	src = mtlBuffer->subBuffers[mtlBuffer->currentSubBufferIndex].ptr;
	SDL_memcpy(
		cpy,
		src + offsetInBytes,
		elementCount * vertexStride
	);

	if (useStagingBuffer)
	{
		src = cpy;
		dst = dataBytes;
		for (i = 0; i < elementCount; i += 1)
		{
			SDL_memcpy(dst, src, elementSizeInBytes);
			dst += elementSizeInBytes;
			src += vertexStride;
		}
		SDL_free(cpy);
	}
}

/* Index Buffers */

static FNA3D_Buffer* METAL_GenIndexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	/* Note that dynamic and usage are NOT used! */
	return METAL_INTERNAL_CreateBuffer(driverData, sizeInBytes);
}

static void METAL_AddDisposeIndexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	METAL_INTERNAL_DestroyBuffer(driverData, buffer);
}

static void METAL_SetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	METAL_INTERNAL_SetBufferData(
		driverData,
		buffer,
		offsetInBytes,
		data,
		dataLength,
		options
	);
}

static void METAL_GetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength
) {
	MetalBuffer *mtlBuffer = (MetalBuffer*) buffer;
	uint8_t *ptr = mtlBuffer->subBuffers[mtlBuffer->currentSubBufferIndex].ptr;
	SDL_memcpy(
		data,
		ptr + offsetInBytes,
		dataLength
	);
}

/* Effects */

static void METAL_INTERNAL_DeleteShader(void* shader)
{
	MOJOSHADER_mtlShader *mtlShader = (MOJOSHADER_mtlShader*) shader;
	const MOJOSHADER_parseData *pd;
	MetalRenderer *renderer;
	PackedRenderPipelineArray *pArr;
	PackedVertexBufferBindingsArray *vArr;
	int32_t i;

	pd = MOJOSHADER_mtlGetShaderParseData(mtlShader);
	renderer = (MetalRenderer*) pd->malloc_data;
	pArr = &renderer->pipelineStateCache;
	vArr = &renderer->vertexDescriptorCache;

	/* Run through the caches in reverse order, to minimize the damage of
	 * doing memmove a bunch of times. Also, do the pipeline cache first,
	 * as they are dependent on the vertex descriptors.
	 */

	for (i = pArr->count - 1; i >= 0; i -= 1)
	{
		const PackedRenderPipelineMap *elem = &pArr->elements[i];
		if (	shader == elem->key.vshader ||
			shader == elem->key.pshader	)
		{
			objc_release(elem->value);
			SDL_memmove(
				pArr->elements + i,
				pArr->elements + i + 1,
				sizeof(PackedRenderPipelineMap) * (pArr->count - i - 1)
			);
			pArr->count -= 1;
		}
	}

	for (i = vArr->count - 1; i >= 0; i -= 1)
	{
		const PackedVertexBufferBindingsMap *elem = &vArr->elements[i];
		if (elem->key.vertexShader == shader)
		{
			objc_release(elem->value);
			SDL_memmove(
				vArr->elements + i,
				vArr->elements + i + 1,
				sizeof(PackedVertexBufferBindingsMap) * (vArr->count - i - 1)
			);
			vArr->count -= 1;
		}
	}

	MOJOSHADER_mtlDeleteShader(mtlShader);
}

static void METAL_CreateEffect(
	FNA3D_Renderer *driverData,
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	int32_t i;
	MOJOSHADER_effectShaderContext shaderBackend;
	MetalEffect *result;

	shaderBackend.compileShader = (MOJOSHADER_compileShaderFunc) MOJOSHADER_mtlCompileShader;
	shaderBackend.shaderAddRef = (MOJOSHADER_shaderAddRefFunc) MOJOSHADER_mtlShaderAddRef;
	shaderBackend.deleteShader = METAL_INTERNAL_DeleteShader;
	shaderBackend.getParseData = (MOJOSHADER_getParseDataFunc) MOJOSHADER_mtlGetShaderParseData;
	shaderBackend.bindShaders = (MOJOSHADER_bindShadersFunc) MOJOSHADER_mtlBindShaders;
	shaderBackend.getBoundShaders = (MOJOSHADER_getBoundShadersFunc) MOJOSHADER_mtlGetBoundShaders;
	shaderBackend.mapUniformBufferMemory = MOJOSHADER_mtlMapUniformBufferMemory;
	shaderBackend.unmapUniformBufferMemory = MOJOSHADER_mtlUnmapUniformBufferMemory;
	shaderBackend.getError = MOJOSHADER_mtlGetError;
	shaderBackend.m = NULL;
	shaderBackend.f = NULL;
	shaderBackend.malloc_data = driverData;

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

	result = (MetalEffect*) SDL_malloc(sizeof(MetalEffect));
	result->effect = *effectData;
	result->library = MOJOSHADER_mtlCompileLibrary(*effectData);
	*effect = (FNA3D_Effect*) result;
}

static void METAL_CloneEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *cloneSource,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	MetalEffect *mtlCloneSource = (MetalEffect*) cloneSource;
	MetalEffect *result;

	*effectData = MOJOSHADER_cloneEffect(mtlCloneSource->effect);
	if (*effectData == NULL)
	{
		FNA3D_LogError(
			"%s", MOJOSHADER_mtlGetError()
		);
	}

	result = (MetalEffect*) SDL_malloc(sizeof(MetalEffect));
	result->effect = *effectData;
	result->library = MOJOSHADER_mtlCompileLibrary(*effectData);
	*effect = (FNA3D_Effect*) result;
}

static void METAL_AddDisposeEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalEffect *mtlEffect = (MetalEffect*) effect;
	if (mtlEffect->effect == renderer->currentEffect)
	{
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectEnd(renderer->currentEffect);
		renderer->currentEffect = NULL;
		renderer->currentTechnique = NULL;
		renderer->currentPass = 0;
	}
	MOJOSHADER_mtlDeleteLibrary(mtlEffect->library);
	MOJOSHADER_deleteEffect(mtlEffect->effect);
	SDL_free(effect);
}

static void METAL_SetEffectTechnique(
	FNA3D_Renderer *renderer,
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
) {
	/* FIXME: Why doesn't this function do anything? */
	MetalEffect *mtlEffect = (MetalEffect*) effect;
	MOJOSHADER_effectSetTechnique(mtlEffect->effect, technique);
}

static void METAL_ApplyEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	uint32_t pass,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalEffect *fnaEffect = (MetalEffect*) effect;
	MOJOSHADER_effect *effectData = fnaEffect->effect;
	const MOJOSHADER_effectTechnique *technique = fnaEffect->effect->current_technique;
	uint32_t whatever;

	/* If a frame isn't already in progress,
	 * wait until one begins to avoid overwriting
	 * the previous frame's uniform buffers.
	 */
	METAL_INTERNAL_BeginFrame(driverData);

	if (effectData == renderer->currentEffect)
	{
		if (	technique == renderer->currentTechnique &&
			pass == renderer->currentPass		)
		{
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
		&whatever,
		0,
		stateChanges
	);
	MOJOSHADER_effectBeginPass(effectData, pass);
	renderer->currentEffect = effectData;
	renderer->currentTechnique = technique;
	renderer->currentPass = pass;
}

static void METAL_BeginPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	MOJOSHADER_effect *effectData = ((MetalEffect*) effect)->effect;
	uint32_t whatever;

	/* If a frame isn't already in progress,
	 * wait until one begins to avoid overwriting
	 * the previous frame's uniform buffers.
	 */
	METAL_INTERNAL_BeginFrame(driverData);

	MOJOSHADER_effectBegin(
		effectData,
		&whatever,
		1,
		stateChanges
	);
	MOJOSHADER_effectBeginPass(effectData, 0);
}

static void METAL_EndPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	MOJOSHADER_effect *effectData = ((MetalEffect*) effect)->effect;
	MOJOSHADER_effectEndPass(effectData);
	MOJOSHADER_effectEnd(effectData);
}

/* Queries */

static FNA3D_Query* METAL_CreateQuery(FNA3D_Renderer *driverData)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalQuery *result;
	SDL_assert(renderer->supportsOcclusionQueries);

	result = (MetalQuery*) SDL_malloc(sizeof(MetalQuery));
	result->handle = mtlNewBuffer(
		renderer->device,
		sizeof(uint64_t),
		0
	);
	return (FNA3D_Query*) result;
}

static void METAL_AddDisposeQuery(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	MetalQuery *mtlQuery = (MetalQuery*) query;
	objc_release(mtlQuery->handle);
	mtlQuery->handle = NULL;
	SDL_free(mtlQuery);
}

static void METAL_QueryBegin(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	MetalQuery *mtlQuery = (MetalQuery*) query;

	/* Stop the current pass */
	METAL_INTERNAL_EndPass(renderer);

	/* Attach the visibility buffer to a new render pass */
	renderer->currentVisibilityBuffer = mtlQuery->handle;
	renderer->needNewRenderPass = 1;
}

static void METAL_QueryEnd(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	if (renderer->renderCommandEncoder != NULL)
	{
		/* Stop counting */
		mtlSetVisibilityResultMode(
			renderer->renderCommandEncoder,
			MTLVisibilityResultModeDisabled,
			0
		);
	}
	renderer->currentVisibilityBuffer = NULL;
}

static uint8_t METAL_QueryComplete(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	/* FIXME:
	 * There's no easy way to check for completion
	 * of the query. The only accurate way would be
	 * to monitor the completion of the command buffer
	 * associated with each query, but that gets tricky
	 * since in the event of a stalled buffer overwrite or
	 * something similar, a new command buffer would be
	 * created, likely screwing up the visibility test.
	 *
	 * The below code is obviously wrong, but it happens
	 * to work for the Lens Flare XNA sample. Maybe it'll
	 * work for your game too?
	 *
	 * (Although if you're making a new game with FNA,
	 * you really shouldn't be using queries anyway...)
	 *
	 * -caleb
	 */
	return 1;
}

static int32_t METAL_QueryPixelCount(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	MetalQuery *mtlQuery = (MetalQuery*) query;
	void* contents = mtlGetBufferContents(mtlQuery->handle);
	return (int32_t) (*((uint64_t*) contents));
}

/* Feature Queries */

static uint8_t METAL_SupportsDXT1(FNA3D_Renderer *driverData)
{
	return ((MetalRenderer*) driverData)->supportsDxt1;
}

static uint8_t METAL_SupportsS3TC(FNA3D_Renderer *driverData)
{
	return ((MetalRenderer*) driverData)->supportsS3tc;
}

static uint8_t METAL_SupportsHardwareInstancing(FNA3D_Renderer *driverData)
{
	return 1;
}

static uint8_t METAL_SupportsNoOverwrite(FNA3D_Renderer *driverData)
{
	return 1;
}

static void METAL_GetMaxTextureSlots(
	FNA3D_Renderer *driverData,
	int32_t *textures,
	int32_t *vertexTextures
) {
	*textures = MAX_TEXTURE_SAMPLERS;
	*vertexTextures = MAX_VERTEXTEXTURE_SAMPLERS;
}

static int32_t METAL_GetMaxMultiSampleCount(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;

	/* FIXME: Format-specific MSAA queries? */
	return SDL_min(renderer->maxMultiSampleCount, multiSampleCount);
}

/* Debugging */

static void METAL_SetStringMarker(FNA3D_Renderer *driverData, const char *text)
{
	MetalRenderer *renderer = (MetalRenderer*) driverData;
	if (renderer->renderCommandEncoder != NULL)
	{
		mtlInsertDebugSignpost(renderer->renderCommandEncoder, text);
	}
}

/* External Interop */

static void METAL_GetSysRenderer(
	FNA3D_Renderer *driverData,
	FNA3D_SysRendererEXT *sysrenderer
) {
	MetalRenderer *renderer = (MetalRenderer*) driverData;

	sysrenderer->rendererType = FNA3D_RENDERER_TYPE_METAL_EXT;
	sysrenderer->renderer.metal.device = renderer->device;
	sysrenderer->renderer.metal.view = renderer->view;
}

static FNA3D_Texture* METAL_CreateSysTexture(
	FNA3D_Renderer *driverData,
	FNA3D_SysTextureEXT *systexture
) {
	MetalTexture *result;

	if (systexture->rendererType != FNA3D_RENDERER_TYPE_METAL_EXT)
	{
		return NULL;
	}

	result = (MetalTexture*) SDL_malloc(sizeof(MetalTexture));
	SDL_zerop(result);

	result->handle = (MTLTexture*) systexture->texture.metal.handle;
	result->hasMipmaps = mtlGetTextureLevelCount(result->handle) > 1;

	/* Everything else either happens to be 0 or is unused anyway! */

	objc_retain(result->handle);
	return (FNA3D_Texture*) result;
}

/* Driver */

static uint8_t METAL_PrepareWindowAttributes(uint32_t *flags)
{
	/* Let's find out if the OS supports Metal... */
	const char *osVersion = SDL_GetPlatform();
	uint8_t isApplePlatform = (
		(strcmp(osVersion, "Mac OS X") == 0) ||
		(strcmp(osVersion, "iOS") == 0) ||
		(strcmp(osVersion, "tvOS") == 0)
	);
	void* metalFramework;

	if (!isApplePlatform)
	{
		/* What are you even doing here...? */
		return 0;
	}

	/* Try loading MTLCreateSystemDefaultDevice */
	metalFramework = SDL_LoadObject(
		"/System/Library/Frameworks/Metal.framework/Metal"
	);
	if (metalFramework == NULL)
	{
		/* Can't load the Metal framework! */
		return 0;
	}
	MTLCreateSystemDefaultDevice =
		(pfn_CreateSystemDefaultDevice) SDL_LoadFunction(
			metalFramework,
			"MTLCreateSystemDefaultDevice"
		);
	if (MTLCreateSystemDefaultDevice() == NULL)
	{
		/* This OS is too old for Metal! */
		return 0;
	}

	/* We're good to go, so initialize the Objective-C references. */
	InitObjC();

#if SDL_VERSION_ATLEAST(2, 0, 13)
	*flags = SDL_WINDOW_METAL;
#else
	SDL_SetHint(SDL_HINT_VIDEO_EXTERNAL_CONTEXT, "1");
#endif
	return 1;
}

void METAL_GetDrawableSize(void* window, int32_t *w, int32_t *h)
{
	SDL_MetalView tempView = SDL_Metal_CreateView((SDL_Window*) window);
	CAMetalLayer *layer = mtlGetLayer(tempView);
	CGSize size = mtlGetDrawableSize(layer);
	*w = size.width;
	*h = size.height;
	SDL_Metal_DestroyView(tempView);
}

static void METAL_INTERNAL_InitializeFauxBackbuffer(
	MetalRenderer *renderer,
	uint8_t scaleNearest
) {
	uint16_t indices[6] =
	{
		0, 1, 3,
		1, 2, 3
	};
	uint8_t* ptr;
	const char *shaderSource;
	NSString *nsShaderSource, *nsVertShader, *nsFragShader;
	MTLLibrary *library;
	MTLFunction *vertexFunc, *fragFunc;
	MTLSamplerDescriptor *samplerDesc;
	MTLRenderPipelineDescriptor *pipelineDesc;
	MTLSamplerMinMagFilter filter = (
		scaleNearest ?
			MTLSamplerMinMagFilterNearest :
			MTLSamplerMinMagFilterLinear
	);

	/* Create a combined vertex / index buffer
	 * for rendering the faux backbuffer.
	 */
	renderer->backbufferDrawBuffer = mtlNewBuffer(
		renderer->device,
		16 * sizeof(float) + sizeof(indices),
		MTLResourceOptionsCPUCacheModeWriteCombined
	);
	ptr = (uint8_t*) mtlGetBufferContents(
		renderer->backbufferDrawBuffer
	);
	SDL_memcpy(
		ptr + (16 * sizeof(float)),
		indices,
		sizeof(indices)
	);

	/* Create vertex and fragment shaders for the faux backbuffer */
	shaderSource =
		"#include <metal_stdlib>				\n"
		"using namespace metal;					\n"
		"struct VertexIn {					\n"
		"	packed_float2 position; 			\n"
		"	packed_float2 texCoord; 			\n"
		"}; 							\n"
		"struct VertexOut { 					\n"
		"	float4 position [[ position ]];	 		\n"
		"	float2 texCoord; 				\n"
		"}; 							\n"
		"vertex VertexOut vertexShader( 			\n"
		"	uint vertexID [[ vertex_id ]], 			\n"
		"	constant VertexIn *vertexArray [[ buffer(0) ]]	\n"
		") { 							\n"
		"	VertexOut out;					\n"
		"	out.position = float4(				\n"
		"		vertexArray[vertexID].position,		\n"
		"		0.0,					\n"
		"		1.0					\n"
		"	);						\n"
		"	out.position.y *= -1; 				\n"
		"	out.texCoord = vertexArray[vertexID].texCoord;	\n"
		"	return out; 					\n"
		"} 							\n"
		"fragment float4 fragmentShader( 			\n"
		"	VertexOut in [[stage_in]], 			\n"
		"	texture2d<half> colorTexture [[ texture(0) ]],	\n"
		"	sampler s0 [[sampler(0)]]			\n"
		") {							\n"
		"	const half4 colorSample = colorTexture.sample(	\n"
		"		s0,					\n"
		"		in.texCoord				\n"
		"	);						\n"
		"	return float4(colorSample);			\n"
		"}							\n";

	nsShaderSource	= UTF8ToNSString(shaderSource);
	nsVertShader	= UTF8ToNSString("vertexShader");
	nsFragShader	= UTF8ToNSString("fragmentShader");

	library = mtlNewLibraryWithSource(
		renderer->device,
		nsShaderSource
	);
	vertexFunc = mtlNewFunctionWithName(library, nsVertShader);
	fragFunc = mtlNewFunctionWithName(library, nsFragShader);

	objc_release(nsShaderSource);
	objc_release(nsVertShader);
	objc_release(nsFragShader);

	/* Create sampler state */
	samplerDesc = mtlNewSamplerDescriptor();
	mtlSetSamplerMinFilter(samplerDesc, filter);
	mtlSetSamplerMagFilter(samplerDesc, filter);
	renderer->backbufferSamplerState = mtlNewSamplerState(
		renderer->device,
		samplerDesc
	);
	objc_release(samplerDesc);

	/* Create render pipeline */
	pipelineDesc = mtlNewRenderPipelineDescriptor();
	mtlSetPipelineVertexFunction(pipelineDesc, vertexFunc);
	mtlSetPipelineFragmentFunction(pipelineDesc, fragFunc);
	mtlSetAttachmentPixelFormat(
		mtlGetColorAttachment(pipelineDesc, 0),
		mtlGetLayerPixelFormat(renderer->layer)
	);
	renderer->backbufferPipeline = mtlNewRenderPipelineState(
		renderer->device,
		pipelineDesc
	);
	objc_release(pipelineDesc);
	objc_release(vertexFunc);
	objc_release(fragFunc);
}

FNA3D_Device* METAL_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
) {
	uint8_t supportsD24S8;
	int32_t i;
	MTLDepthStencilDescriptor *dsDesc;
	MetalRenderer *renderer;
	FNA3D_Device *result;

	/* Create the FNA3D_Device */
	result = (FNA3D_Device*) SDL_malloc(sizeof(FNA3D_Device));
	ASSIGN_DRIVER(METAL)

	/* Init the MetalRenderer */
	renderer = (MetalRenderer*) SDL_malloc(sizeof(MetalRenderer));
	SDL_memset(renderer, '\0', sizeof(MetalRenderer));

	/* The FNA3D_Device and MetalRenderer need to reference each other */
	renderer->parentDevice = result;
	result->driverData = (FNA3D_Renderer*) renderer;

	/* Create the MTLDevice and MTLCommandQueue */
	renderer->device = MTLCreateSystemDefaultDevice();
	renderer->queue = mtlNewCommandQueue(renderer->device);

	/* Create the Metal view and get its layer */
	renderer->view = SDL_Metal_CreateView(
		(SDL_Window*) presentationParameters->deviceWindowHandle
	);
	renderer->layer = mtlGetLayer(renderer->view);

	/* Set up the layer */
	mtlSetLayerDevice(renderer->layer, renderer->device);
	mtlSetLayerFramebufferOnly(renderer->layer, 1);
	mtlSetLayerMagnificationFilter(
		renderer->layer,
		UTF8ToNSString("nearest")
	);

	/* Log driver info */
	FNA3D_LogInfo(
		"FNA3D Driver: Metal\nDevice Name: %s",
		mtlGetDeviceName(renderer->device)
	);

	/* Set device properties */
	renderer->isMac = (strcmp(SDL_GetPlatform(), "Mac OS X") == 0);
	renderer->supportsS3tc = renderer->supportsDxt1 = renderer->isMac;
	renderer->maxMultiSampleCount = (
		mtlDeviceSupportsSampleCount(renderer->device, 8) ? 8 : 4
	);
	renderer->supportsOcclusionQueries = (
		renderer->isMac ||
		HasModernAppleGPU(renderer->device)
	);

	/* Determine supported depth formats */
	renderer->D16Format = MTLPixelFormatDepth32Float;
	renderer->D24Format = MTLPixelFormatDepth32Float;
	renderer->D24S8Format = MTLPixelFormatDepth32FloatStencil8;

	if (renderer->isMac)
	{
		supportsD24S8 = mtlDeviceSupportsDepth24Stencil8(renderer->device);
		if (supportsD24S8)
		{
			renderer->D24S8Format = MTLPixelFormatDepth24UnormStencil8;

			/* Gross, but at least it's a unorm format! -caleb */
			renderer->D24Format = MTLPixelFormatDepth24UnormStencil8;
			renderer->D16Format = MTLPixelFormatDepth24UnormStencil8;
		}

		/* Depth16Unorm requires macOS 10.12+ */
		if (OperatingSystemAtLeast(10, 12, 0))
		{
			renderer->D16Format = MTLPixelFormatDepth16Unorm;
		}
	}
	else
	{
		/* Depth16Unorm requires iOS/tvOS 13+ */
		if (OperatingSystemAtLeast(13, 0, 0))
		{
			renderer->D16Format = MTLPixelFormatDepth16Unorm;
		}
	}

	/* Initialize MojoShader context */
	renderer->mtlContext = MOJOSHADER_mtlCreateContext(
		renderer->device,
		NULL,
		NULL,
		renderer
	);
	MOJOSHADER_mtlMakeContextCurrent(renderer->mtlContext);

	/* Initialize texture and sampler collections */
	for (i = 0; i < MAX_TOTAL_SAMPLERS; i += 1)
	{
		renderer->textures[i] = &NullTexture;
		renderer->samplers[i] = NULL;
	}

	/* Create a default depth stencil state */
	dsDesc = mtlNewDepthStencilDescriptor();
	renderer->defaultDepthStencilState = mtlNewDepthStencilState(
		renderer->device,
		dsDesc
	);
	objc_release(dsDesc);

	/* Create and initialize the faux-backbuffer */
	renderer->backbuffer = (MetalBackbuffer*) SDL_malloc(
		sizeof(MetalBackbuffer)
	);
	SDL_memset(renderer->backbuffer, '\0', sizeof(MetalBackbuffer));
	METAL_INTERNAL_CreateFramebuffer(
		renderer,
		presentationParameters
	);
	METAL_INTERNAL_InitializeFauxBackbuffer(
		renderer,
		SDL_GetHintBoolean("FNA3D_BACKBUFFER_SCALE_NEAREST", SDL_FALSE)
	);
	METAL_INTERNAL_SetPresentationInterval(
		renderer,
		presentationParameters->presentationInterval
	);

	/* Initialize buffer allocator */
	renderer->bufferAllocator = (MetalBufferAllocator*) SDL_malloc(
		sizeof(MetalBufferAllocator)
	);
	SDL_memset(renderer->bufferAllocator, '\0', sizeof(MetalBufferAllocator));

	/* Initialize buffers/textures-in-use arrays */
	renderer->maxBuffersInUse = 8; /* arbitrary! */
	renderer->buffersInUse = (MetalBuffer**) SDL_malloc(
		sizeof(MetalBuffer*) * renderer->maxBuffersInUse
	);

	renderer->maxTexturesInUse = 8; /* arbitrary! */
	renderer->texturesInUse = (MetalTexture**) SDL_malloc(
		sizeof(MetalTexture*) * renderer->maxTexturesInUse
	);

	/* Initialize renderer members not covered by SDL_memset('\0') */
	renderer->multiSampleMask = -1; /* AKA 0xFFFFFFFF, ugh -flibit */
	renderer->multiSampleEnable = 1;
	renderer->viewport.maxDepth = 1.0f;
	renderer->clearDepth = 1.0f;

	/* Return the FNA3D_Device */
	return result;
}

FNA3D_Driver MetalDriver = {
	"Metal",
	METAL_PrepareWindowAttributes,
	METAL_GetDrawableSize,
	METAL_CreateDevice
};

#else

extern int this_tu_is_empty;

#endif /* FNA3D_DRIVER_METAL */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
