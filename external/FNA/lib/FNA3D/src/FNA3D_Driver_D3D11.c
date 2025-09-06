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

#if FNA3D_DRIVER_D3D11

#include "FNA3D_Driver.h"
#include "FNA3D_PipelineCache.h"
#include "FNA3D_Driver_D3D11.h"

#include <SDL.h>
#include <SDL_syswm.h>

/* D3D11 Libraries */

#if defined(_WIN32)
#define D3DCOMPILER_DLL	"d3dcompiler_47.dll"
#define D3D11_DLL	"d3d11.dll"
#define DXGI_DLL	"dxgi.dll"
#elif defined(__APPLE__)
#define D3DCOMPILER_DLL	"libd3dcompiler.dylib"
#define D3D11_DLL	"libd3d11.dylib"
#define DXGI_DLL	"libdxgi.dylib"
#else
#define D3DCOMPILER_DLL	"libd3dcompiler.so"
#define D3D11_DLL	"libd3d11.so"
#define DXGI_DLL	"libdxgi.so"
#endif

#ifdef __WINRT__
#include <dxgi1_2.h>
#include <d3dcompiler.h>
#else
#include <dxgi.h>
#endif

#define ERROR_CHECK(msg) \
	if (FAILED(res)) \
	{ \
		D3D11_INTERNAL_LogError(renderer->device, msg, res); \
	}
#define ERROR_CHECK_RETURN(msg, ret) \
	if (FAILED(res)) \
	{ \
		D3D11_INTERNAL_LogError(renderer->device, msg, res); \
		return ret; \
	}
#define ERROR_CHECK_UNLOCK_RETURN(msg, ret) \
	if (FAILED(res)) \
	{ \
		D3D11_INTERNAL_LogError(renderer->device, msg, res); \
		SDL_UnlockMutex(renderer->ctxLock); \
		return ret; \
	}

/* Internal Structures */

typedef struct D3D11Texture /* Cast FNA3D_Texture* to this! */
{
	/* D3D Handles */
	ID3D11Resource *handle; /* ID3D11Texture2D or ID3D11Texture3D */
	ID3D11ShaderResourceView *shaderView;

	/* Basic Info */
	int32_t levelCount;
	uint8_t isRenderTarget;
	FNA3D_SurfaceFormat format;

	/* Dimensions */
	uint8_t rtType;
	FNA3DNAMELESS union
	{
		struct
		{
			int32_t width;
			int32_t height;
			ID3D11RenderTargetView *rtView;
		} twod;
		struct
		{
			int32_t width;
			int32_t height;
			int32_t depth;
		} threed;
		struct
		{
			int32_t size;
			ID3D11RenderTargetView **rtViews;
		} cube;
	};
	ID3D11Resource *staging; /* ID3D11Texture2D or ID3D11Texture3D */
} D3D11Texture;

static D3D11Texture NullTexture =
{
	NULL,
	NULL,
	1,
	0,
	FNA3D_SURFACEFORMAT_COLOR,
	0,
	{
		{ 0, 0 }
	},
	NULL
};

typedef struct D3D11Renderbuffer /* Cast FNA3D_Renderbuffer* to this! */
{
	ID3D11Texture2D *handle;
	int32_t multiSampleCount;

	#define RENDERBUFFER_COLOR 0
	#define RENDERBUFFER_DEPTH 1
	uint8_t type;
	FNA3DNAMELESS union
	{
		struct
		{
			FNA3D_SurfaceFormat format;
			ID3D11RenderTargetView *rtView;
		} color;
		struct
		{
			FNA3D_DepthFormat format;
			ID3D11DepthStencilView *dsView;
		} depth;
	};
} D3D11Renderbuffer;

typedef struct D3D11Buffer /* Cast FNA3D_Buffer* to this! */
{
	ID3D11Buffer *handle;
	uint8_t dynamic;
	int32_t size;
} D3D11Buffer;

typedef struct D3D11Effect /* Cast FNA3D_Effect* to this! */
{
	MOJOSHADER_effect *effect;
} D3D11Effect;

typedef struct D3D11Query /* Cast FNA3D_Query* to this! */
{
	ID3D11Query *handle;
} D3D11Query;

typedef struct D3D11Renderer /* Cast FNA3D_Renderer* to this! */
{
	/* Persistent D3D11 Objects */
	ID3D11Device *device;
	ID3D11DeviceContext *context;
	void* d3d11_dll;
	void* dxgi_dll;
	void* factory; /* IDXGIFactory1 or IDXGIFactory2 */
	IDXGIAdapter1 *adapter;
	IDXGISwapChain *swapchain;
	ID3DUserDefinedAnnotation *annotation;
	SDL_mutex *ctxLock;

	/* The Faux-Backbuffer */
	struct
	{
		int32_t width;
		int32_t height;

		/* Color */
		FNA3D_SurfaceFormat surfaceFormat;
		ID3D11Texture2D *colorBuffer;
		ID3D11RenderTargetView *colorView;
		ID3D11ShaderResourceView *shaderView;
		ID3D11Texture2D *stagingBuffer;

		/* Depth Stencil */
		FNA3D_DepthFormat depthFormat;
		ID3D11Texture2D *depthStencilBuffer;
		ID3D11DepthStencilView *depthStencilView;

		/* Multisample */
		int32_t multiSampleCount;
		ID3D11Texture2D *resolveBuffer;
	} backbuffer;
	uint8_t backbufferSizeChanged;
	FNA3D_Rect prevSrcRect;
	FNA3D_Rect prevDstRect;
	ID3D11VertexShader *fauxBlitVS;
	ID3D11PixelShader *fauxBlitPS;
	ID3D11SamplerState *fauxBlitSampler;
	ID3D11Buffer *fauxBlitVertexBuffer;
	ID3D11Buffer *fauxBlitIndexBuffer;
	ID3D11InputLayout *fauxBlitLayout;
	ID3D11RasterizerState *fauxRasterizer;
	ID3D11BlendState *fauxBlendState;

	/* Capabilities */
	uint8_t debugMode;
	uint32_t supportsDxt1;
	uint32_t supportsS3tc;
	int32_t maxMultiSampleCount;
	D3D_FEATURE_LEVEL featureLevel;

	/* Presentation */
	uint8_t syncInterval;

	/* Blend State */
	ID3D11BlendState *blendState;
	FNA3D_Color blendFactor;
	int32_t multiSampleMask;

	/* Depth Stencil State */
	ID3D11DepthStencilState *depthStencilState;
	int32_t stencilRef;

	/* Rasterizer State */
	FNA3D_Viewport viewport;
	FNA3D_Rect scissorRect;
	ID3D11RasterizerState *rasterizerState;

	/* Textures */
	D3D11Texture *textures[MAX_TOTAL_SAMPLERS];
	ID3D11SamplerState *samplers[MAX_TOTAL_SAMPLERS];

	/* Input Assembly */
	ID3D11InputLayout *inputLayout;
	FNA3D_PrimitiveType topology;
	ID3D11Buffer *vertexBuffers[MAX_BOUND_VERTEX_BUFFERS];
	uint32_t vertexBufferOffsets[MAX_BOUND_VERTEX_BUFFERS];
	uint32_t vertexBufferStrides[MAX_BOUND_VERTEX_BUFFERS];
	ID3D11Buffer *indexBuffer;
	FNA3D_IndexElementSize indexElementSize;

	/* Resource Caches */
	PackedStateArray blendStateCache;
	PackedStateArray depthStencilStateCache;
	PackedStateArray rasterizerStateCache;
	PackedStateArray samplerStateCache;
	PackedVertexBufferBindingsArray inputLayoutCache;

	/* Render Targets */
	int32_t numRenderTargets;
	ID3D11RenderTargetView *swapchainRTView;
	ID3D11RenderTargetView *renderTargetViews[MAX_RENDERTARGET_BINDINGS];
	ID3D11DepthStencilView *depthStencilView;
	FNA3D_DepthFormat currentDepthFormat;

	/* MojoShader Interop */
	MOJOSHADER_effect *currentEffect;
	const MOJOSHADER_effectTechnique *currentTechnique;
	uint32_t currentPass;
	uint8_t effectApplied;
} D3D11Renderer;

/* XNA->D3D11 Translation Arrays */

static DXGI_FORMAT XNAToD3D_TextureFormat[] =
{
	DXGI_FORMAT_R8G8B8A8_UNORM,	/* SurfaceFormat.Color */
	DXGI_FORMAT_B5G6R5_UNORM,	/* SurfaceFormat.Bgr565 */
	DXGI_FORMAT_B5G5R5A1_UNORM,	/* SurfaceFormat.Bgra5551 */
	DXGI_FORMAT_B4G4R4A4_UNORM,	/* SurfaceFormat.Bgra4444 */
	DXGI_FORMAT_BC1_UNORM,		/* SurfaceFormat.Dxt1 */
	DXGI_FORMAT_BC2_UNORM,		/* SurfaceFormat.Dxt3 */
	DXGI_FORMAT_BC3_UNORM,		/* SurfaceFormat.Dxt5 */
	DXGI_FORMAT_R8G8_SNORM, 	/* SurfaceFormat.NormalizedByte2 */
	DXGI_FORMAT_R8G8B8A8_SNORM,	/* SurfaceFormat.NormalizedByte4 */
	DXGI_FORMAT_R10G10B10A2_UNORM,	/* SurfaceFormat.Rgba1010102 */
	DXGI_FORMAT_R16G16_UNORM,	/* SurfaceFormat.Rg32 */
	DXGI_FORMAT_R16G16B16A16_UNORM,	/* SurfaceFormat.Rgba64 */
	DXGI_FORMAT_A8_UNORM,		/* SurfaceFormat.Alpha8 */
	DXGI_FORMAT_R32_FLOAT,		/* SurfaceFormat.Single */
	DXGI_FORMAT_R32G32_FLOAT,	/* SurfaceFormat.Vector2 */
	DXGI_FORMAT_R32G32B32A32_FLOAT,	/* SurfaceFormat.Vector4 */
	DXGI_FORMAT_R16_FLOAT,		/* SurfaceFormat.HalfSingle */
	DXGI_FORMAT_R16G16_FLOAT,	/* SurfaceFormat.HalfVector2 */
	DXGI_FORMAT_R16G16B16A16_FLOAT,	/* SurfaceFormat.HalfVector4 */
	DXGI_FORMAT_R16G16B16A16_FLOAT,	/* SurfaceFormat.HdrBlendable */
	DXGI_FORMAT_B8G8R8A8_UNORM,	/* SurfaceFormat.ColorBgraEXT */
};

static DXGI_FORMAT XNAToD3D_DepthFormat[] =
{
	DXGI_FORMAT_UNKNOWN,		/* DepthFormat.None */
	DXGI_FORMAT_D16_UNORM,		/* DepthFormat.Depth16 */
	DXGI_FORMAT_D24_UNORM_S8_UINT,	/* DepthFormat.Depth24 */
	DXGI_FORMAT_D24_UNORM_S8_UINT	/* DepthFormat.Depth24Stencil8 */
};

static LPCSTR XNAToD3D_VertexAttribSemanticName[] =
{
	"POSITION",			/* VertexElementUsage.Position */
	"COLOR",			/* VertexElementUsage.Color */
	"TEXCOORD",			/* VertexElementUsage.TextureCoordinate */
	"NORMAL",			/* VertexElementUsage.Normal */
	"BINORMAL",			/* VertexElementUsage.Binormal */
	"TANGENT",			/* VertexElementUsage.Tangent */
	"BLENDINDICES",			/* VertexElementUsage.BlendIndices */
	"BLENDWEIGHT",			/* VertexElementUsage.BlendWeight */
	"SV_DEPTH",			/* VertexElementUsage.Depth */
	"FOG",				/* VertexElementUsage.Fog */
	"PSIZE",			/* VertexElementUsage.PointSize */
	"SV_SampleIndex",		/* VertexElementUsage.Sample */
	"TESSFACTOR"			/* VertexElementUsage.TessellateFactor */
};

static DXGI_FORMAT XNAToD3D_VertexAttribFormat[] =
{
	DXGI_FORMAT_R32_FLOAT,		/* VertexElementFormat.Single */
	DXGI_FORMAT_R32G32_FLOAT,	/* VertexElementFormat.Vector2 */
	DXGI_FORMAT_R32G32B32_FLOAT,	/* VertexElementFormat.Vector3 */
	DXGI_FORMAT_R32G32B32A32_FLOAT,	/* VertexElementFormat.Vector4 */
	DXGI_FORMAT_R8G8B8A8_UNORM,	/* VertexElementFormat.Color */
	DXGI_FORMAT_R8G8B8A8_UINT,	/* VertexElementFormat.Byte4 */
	DXGI_FORMAT_R16G16_SINT,	/* VertexElementFormat.Short2 */
	DXGI_FORMAT_R16G16B16A16_SINT,	/* VertexElementFormat.Short4 */
	DXGI_FORMAT_R16G16_SNORM,	/* VertexElementFormat.NormalizedShort2 */
	DXGI_FORMAT_R16G16B16A16_SNORM,	/* VertexElementFormat.NormalizedShort4 */
	DXGI_FORMAT_R16G16_FLOAT,	/* VertexElementFormat.HalfVector2 */
	DXGI_FORMAT_R16G16B16A16_FLOAT	/* VertexElementFormat.HalfVector4 */
};

static DXGI_FORMAT XNAToD3D_IndexType[] =
{
	DXGI_FORMAT_R16_UINT,		/* IndexElementSize.SixteenBits */
	DXGI_FORMAT_R32_UINT		/* IndexElementSize.ThirtyTwoBits */
};

static D3D11_BLEND XNAToD3D_BlendMode[] =
{
	D3D11_BLEND_ONE,		/* Blend.One */
	D3D11_BLEND_ZERO,		/* Blend.Zero */
	D3D11_BLEND_SRC_COLOR,		/* Blend.SourceColor */
	D3D11_BLEND_INV_SRC_COLOR,	/* Blend.InverseSourceColor */
	D3D11_BLEND_SRC_ALPHA,		/* Blend.SourceAlpha */
	D3D11_BLEND_INV_SRC_ALPHA,	/* Blend.InverseSourceAlpha */
	D3D11_BLEND_DEST_COLOR,		/* Blend.DestinationColor */
	D3D11_BLEND_INV_DEST_COLOR,	/* Blend.InverseDestinationColor */
	D3D11_BLEND_DEST_ALPHA,		/* Blend.DestinationAlpha */
	D3D11_BLEND_INV_DEST_ALPHA,	/* Blend.InverseDestinationAlpha */
	D3D11_BLEND_BLEND_FACTOR,	/* Blend.BlendFactor */
	D3D11_BLEND_INV_BLEND_FACTOR,	/* Blend.InverseBlendFactor */
	D3D11_BLEND_SRC_ALPHA_SAT	/* Blend.SourceAlphaSaturation */
};

static D3D11_BLEND XNAToD3D_BlendModeAlpha[] =
{
	D3D11_BLEND_ONE,		/* Blend.One */
	D3D11_BLEND_ZERO,		/* Blend.Zero */
	D3D11_BLEND_SRC_ALPHA,		/* Blend.SourceColor */
	D3D11_BLEND_INV_SRC_ALPHA,	/* Blend.InverseSourceColor */
	D3D11_BLEND_SRC_ALPHA,		/* Blend.SourceAlpha */
	D3D11_BLEND_INV_SRC_ALPHA,	/* Blend.InverseSourceAlpha */
	D3D11_BLEND_DEST_ALPHA,		/* Blend.DestinationColor */
	D3D11_BLEND_INV_DEST_ALPHA,	/* Blend.InverseDestinationColor */
	D3D11_BLEND_DEST_ALPHA,		/* Blend.DestinationAlpha */
	D3D11_BLEND_INV_DEST_ALPHA,	/* Blend.InverseDestinationAlpha */
	D3D11_BLEND_BLEND_FACTOR,	/* Blend.BlendFactor */
	D3D11_BLEND_INV_BLEND_FACTOR,	/* Blend.InverseBlendFactor */
	D3D11_BLEND_SRC_ALPHA_SAT	/* Blend.SourceAlphaSaturation */
};

static D3D11_BLEND_OP XNAToD3D_BlendOperation[] =
{
	D3D11_BLEND_OP_ADD,		/* BlendFunction.Add */
	D3D11_BLEND_OP_SUBTRACT,	/* BlendFunction.Subtract */
	D3D11_BLEND_OP_REV_SUBTRACT,	/* BlendFunction.ReverseSubtract */
	D3D11_BLEND_OP_MAX,		/* BlendFunction.Max */
	D3D11_BLEND_OP_MIN		/* BlendFunction.Min */
};

static D3D11_COMPARISON_FUNC XNAToD3D_CompareFunc[] =
{
	D3D11_COMPARISON_ALWAYS,	/* CompareFunction.Always */
	D3D11_COMPARISON_NEVER,		/* CompareFunction.Never */
	D3D11_COMPARISON_LESS,		/* CompareFunction.Less */
	D3D11_COMPARISON_LESS_EQUAL,	/* CompareFunction.LessEqual */
	D3D11_COMPARISON_EQUAL,		/* CompareFunction.Equal */
	D3D11_COMPARISON_GREATER_EQUAL,	/* CompareFunction.GreaterEqual */
	D3D11_COMPARISON_GREATER,	/* CompareFunction.Greater */
	D3D11_COMPARISON_NOT_EQUAL	/* CompareFunction.NotEqual */
};

static D3D11_STENCIL_OP XNAToD3D_StencilOp[] =
{
	D3D11_STENCIL_OP_KEEP,		/* StencilOperation.Keep */
	D3D11_STENCIL_OP_ZERO,		/* StencilOperation.Zero */
	D3D11_STENCIL_OP_REPLACE,	/* StencilOperation.Replace */
	D3D11_STENCIL_OP_INCR,		/* StencilOperation.Increment */
	D3D11_STENCIL_OP_DECR,		/* StencilOperation.Decrement */
	D3D11_STENCIL_OP_INCR_SAT,	/* StencilOperation.IncrementSaturation */
	D3D11_STENCIL_OP_DECR_SAT,	/* StencilOperation.DecrementSaturation */
	D3D11_STENCIL_OP_INVERT		/* StencilOperation.Invert */
};

static D3D11_FILL_MODE XNAToD3D_FillMode[] =
{
	D3D11_FILL_SOLID,		/* FillMode.Solid */
	D3D11_FILL_WIREFRAME		/* FillMode.WireFrame */
};

static float XNAToD3D_DepthBiasScale[] =
{
	0.0f,				/* DepthFormat.None */
	(float) ((1 << 16) - 1),	/* DepthFormat.Depth16 */
	(float) ((1 << 24) - 1),	/* DepthFormat.Depth24 */
	(float) ((1 << 24) - 1) 	/* DepthFormat.Depth24Stencil8 */
};

static D3D11_CULL_MODE XNAToD3D_CullMode[] =
{
	D3D11_CULL_NONE,		/* CullMode.None */
	D3D11_CULL_BACK,		/* CullMode.CullClockwiseFace */
	D3D11_CULL_FRONT 		/* CullMode.CullCounterClockwiseFace */
};

static D3D11_TEXTURE_ADDRESS_MODE XNAToD3D_Wrap[] =
{
	D3D11_TEXTURE_ADDRESS_WRAP,	/* TextureAddressMode.Wrap */
	D3D11_TEXTURE_ADDRESS_CLAMP,	/* TextureAddressMode.Clamp */
	D3D11_TEXTURE_ADDRESS_MIRROR	/* TextureAddressMode.Mirror */
};

static D3D11_FILTER XNAToD3D_Filter[] =
{
	D3D11_FILTER_MIN_MAG_MIP_LINEAR,		/* TextureFilter.Linear */
	D3D11_FILTER_MIN_MAG_MIP_POINT,			/* TextureFilter.Point */
	D3D11_FILTER_ANISOTROPIC,			/* TextureFilter.Anisotropic */
	D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT,		/* TextureFilter.LinearMipPoint */
	D3D11_FILTER_MIN_MAG_POINT_MIP_LINEAR,		/* TextureFilter.PointMipLinear */
	D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR,	/* TextureFilter.MinLinearMagPointMipLinear */
	D3D11_FILTER_MIN_LINEAR_MAG_MIP_POINT,		/* TextureFilter.MinLinearMagPointMipPoint */
	D3D11_FILTER_MIN_POINT_MAG_MIP_LINEAR,		/* TextureFilter.MinPointMagLinearMipLinear */
	D3D11_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT	/* TextureFilter.MinPointMagLinearMipPoint */
};

static D3D_PRIMITIVE_TOPOLOGY XNAToD3D_Primitive[] =
{
	D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST,	/* PrimitiveType.TriangleList */
	D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP,	/* PrimitiveType.TriangleStrip */
	D3D_PRIMITIVE_TOPOLOGY_LINELIST,	/* PrimitiveType.LineList */
	D3D_PRIMITIVE_TOPOLOGY_LINESTRIP,	/* PrimitiveType.LineStrip */
	D3D_PRIMITIVE_TOPOLOGY_POINTLIST	/* PrimitiveType.PointListEXT */
};

/* Faux-Backbuffer Blit Shader Sources */

static const char* FAUX_BLIT_VERTEX_SHADER =
	"void main("
	"	inout float4 position : SV_POSITION,"
	"	inout float2 texcoord : TEXCOORD0"
	") {"
	"	position.y *= -1;"
	"	position.zw = float2(0.0f, 1.0f);"
	"}";

static const char* FAUX_BLIT_PIXEL_SHADER =
	"Texture2D Texture : register(t0);"
	"sampler TextureSampler : register(s0);"
	"float4 main("
	"	float4 position : SV_POSITION,"
	"	float2 texcoord : TEXCOORD0"
	") : SV_TARGET {"
	"	return Texture.Sample(TextureSampler, texcoord);"
	"}";

/* Helper Functions */

static void D3D11_INTERNAL_LogError(
	ID3D11Device *device,
	const char *msg,
	HRESULT res
) {
	#define MAX_ERROR_LEN 1024 /* FIXME: Arbitrary! */

	/* Buffer for text, ensure space for \0 terminator after buffer */
	char wszMsgBuff[MAX_ERROR_LEN + 1];
	DWORD dwChars; /* Number of chars returned. */

	if (res == DXGI_ERROR_DEVICE_REMOVED)
	{
		res = ID3D11Device_GetDeviceRemovedReason(device);
	}

	/* Try to get the message from the system errors. */
	dwChars = FormatMessage(
		FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL,
		res,
		0,
		wszMsgBuff,
		MAX_ERROR_LEN,
		NULL
	);

	/* No message? Screw it, just post the code. */
	if (dwChars == 0)
	{
		FNA3D_LogError("%s! Error Code: 0x%08X", msg, res);
		return;
	}

	/* Ensure valid range */
	dwChars = SDL_min(dwChars, MAX_ERROR_LEN);

	/* Trim whitespace from tail of message */
	while (dwChars > 0)
	{
		if (wszMsgBuff[dwChars - 1] <= ' ')
		{
			dwChars--;
		}
		else
		{
			break;
		}
	}

	/* Ensure null-terminated string */
	wszMsgBuff[dwChars] = '\0';

	FNA3D_LogError("%s! Error Code: %s (0x%08X)", msg, wszMsgBuff, res);
}

static inline uint32_t D3D11_INTERNAL_CalcSubresource(
	uint32_t mipLevel,
	uint32_t arraySlice,
	uint32_t numLevels
) {
	return mipLevel + (arraySlice * numLevels);
}

static uint8_t D3D11_INTERNAL_BlendEquals(
	FNA3D_Color *a,
	FNA3D_Color *b
) {
	return SDL_memcmp(a, b, sizeof(FNA3D_Color)) == 0;
}

/* Pipeline State Object Caching */

static ID3D11BlendState* D3D11_INTERNAL_FetchBlendState(
	D3D11Renderer *renderer,
	FNA3D_BlendState *state
) {
	PackedState packedState;
	D3D11_BLEND_DESC desc = {0};
	ID3D11BlendState *result;
	HRESULT res;

	/* Can we just reuse an existing state? */
	packedState = GetPackedBlendState(*state);
	result = (ID3D11BlendState*) PackedStateArray_Fetch(
		renderer->blendStateCache,
		packedState
	);
	if (result != NULL)
	{
		/* The state is already cached! */
		return result;
	}

	/* We need to make a new blend state... */
	desc.AlphaToCoverageEnable = 0;
	desc.IndependentBlendEnable = 0;
	desc.RenderTarget[0].BlendEnable = !(
		state->colorSourceBlend == FNA3D_BLEND_ONE &&
		state->colorDestinationBlend == FNA3D_BLEND_ZERO &&
		state->alphaSourceBlend == FNA3D_BLEND_ONE &&
		state->alphaDestinationBlend == FNA3D_BLEND_ZERO
	);
	if (desc.RenderTarget[0].BlendEnable)
	{
		desc.RenderTarget[0].BlendOp = XNAToD3D_BlendOperation[
			state->colorBlendFunction
		];
		desc.RenderTarget[0].BlendOpAlpha = XNAToD3D_BlendOperation[
			state->alphaBlendFunction
		];
		desc.RenderTarget[0].DestBlend = XNAToD3D_BlendMode[
			state->colorDestinationBlend
		];
		desc.RenderTarget[0].DestBlendAlpha = XNAToD3D_BlendModeAlpha[
			state->alphaDestinationBlend
		];
		desc.RenderTarget[0].SrcBlend = XNAToD3D_BlendMode[
			state->colorSourceBlend
		];
		desc.RenderTarget[0].SrcBlendAlpha = XNAToD3D_BlendModeAlpha[
			state->alphaSourceBlend
		];
	}

	/* All other states should match for all targets... */
	desc.RenderTarget[1] = desc.RenderTarget[0];
	desc.RenderTarget[2] = desc.RenderTarget[0];
	desc.RenderTarget[3] = desc.RenderTarget[0];

	/* ... except RenderTargetWriteMask. */
	desc.RenderTarget[0].RenderTargetWriteMask = (
		(uint32_t) state->colorWriteEnable
	);
	desc.RenderTarget[1].RenderTargetWriteMask = (
		(uint32_t) state->colorWriteEnable1
	);
	desc.RenderTarget[2].RenderTargetWriteMask = (
		(uint32_t) state->colorWriteEnable2
	);
	desc.RenderTarget[3].RenderTargetWriteMask = (
		(uint32_t) state->colorWriteEnable3
	);

	/* Bake the state! */
	res = ID3D11Device_CreateBlendState(
		renderer->device,
		&desc,
		&result
	);
	ERROR_CHECK_RETURN("Blend state creation failed", NULL)
	PackedStateArray_Insert(
		&renderer->blendStateCache,
		packedState,
		result
	);

	/* Return the state! */
	return result;
}

static ID3D11DepthStencilState* D3D11_INTERNAL_FetchDepthStencilState(
	D3D11Renderer *renderer,
	FNA3D_DepthStencilState *state
) {
	PackedState packedState;
	D3D11_DEPTH_STENCIL_DESC desc;
	D3D11_DEPTH_STENCILOP_DESC front, back;
	ID3D11DepthStencilState *result;
	HRESULT res;

	/* Can we just reuse an existing state? */
	packedState = GetPackedDepthStencilState(*state);
	result = (ID3D11DepthStencilState*) PackedStateArray_Fetch(
		renderer->depthStencilStateCache,
		packedState
	);
	if (result != NULL)
	{
		/* The state is already cached! */
		return result;
	}

	/* We have to make a new depth stencil state... */
	desc.DepthEnable = state->depthBufferEnable;
	desc.DepthWriteMask = (
		state->depthBufferEnable && state->depthBufferWriteEnable ?
			D3D11_DEPTH_WRITE_MASK_ALL :
			D3D11_DEPTH_WRITE_MASK_ZERO
	);
	desc.DepthFunc = XNAToD3D_CompareFunc[
		state->depthBufferFunction
	];
	desc.StencilEnable = state->stencilEnable;
	desc.StencilReadMask = (uint8_t) state->stencilMask;
	desc.StencilWriteMask = (uint8_t) state->stencilWriteMask;
	front.StencilDepthFailOp = XNAToD3D_StencilOp[
		state->stencilDepthBufferFail
	];
	front.StencilFailOp = XNAToD3D_StencilOp[
		state->stencilFail
	];
	front.StencilFunc = XNAToD3D_CompareFunc[
		state->stencilFunction
	];
	front.StencilPassOp = XNAToD3D_StencilOp[
		state->stencilPass
	];
	if (state->twoSidedStencilMode)
	{
		back.StencilDepthFailOp = XNAToD3D_StencilOp[
			state->ccwStencilDepthBufferFail
		];
		back.StencilFailOp = XNAToD3D_StencilOp[
			state->ccwStencilFail
		];
		back.StencilFunc = XNAToD3D_CompareFunc[
			state->ccwStencilFunction
		];
		back.StencilPassOp = XNAToD3D_StencilOp[
			state->ccwStencilPass
		];
	}
	else
	{
		back = front;
	}
	desc.FrontFace = front;
	desc.BackFace = back;

	/* Bake the state! */
	res = ID3D11Device_CreateDepthStencilState(
		renderer->device,
		&desc,
		&result
	);
	ERROR_CHECK_RETURN("Depth-stencil state creation failed", NULL)
	PackedStateArray_Insert(
		&renderer->depthStencilStateCache,
		packedState,
		result
	);

	/* Return the state! */
	return result;
}

static ID3D11RasterizerState* D3D11_INTERNAL_FetchRasterizerState(
	D3D11Renderer *renderer,
	FNA3D_RasterizerState *state
) {
	PackedState packedState;
	float depthBias;
	D3D11_RASTERIZER_DESC desc;
	ID3D11RasterizerState *result;
	HRESULT res;

	depthBias = state->depthBias * XNAToD3D_DepthBiasScale[
		renderer->currentDepthFormat
	];

	/* Can we just reuse an existing state? */
	packedState = GetPackedRasterizerState(*state, depthBias);
	result = (ID3D11RasterizerState*) PackedStateArray_Fetch(
		renderer->rasterizerStateCache,
		packedState
	);
	if (result != NULL)
	{
		/* The state is already cached! */
		return result;
	}

	/* We have to make a new rasterizer state... */
	desc.AntialiasedLineEnable = 0;
	desc.CullMode = XNAToD3D_CullMode[state->cullMode];
	desc.DepthBias = (int32_t) depthBias;
	desc.DepthBiasClamp = D3D11_FLOAT32_MAX;
	desc.DepthClipEnable = 1;
	desc.FillMode = XNAToD3D_FillMode[state->fillMode];
	desc.FrontCounterClockwise = 1;
	desc.MultisampleEnable = state->multiSampleAntiAlias;
	desc.ScissorEnable = state->scissorTestEnable;
	desc.SlopeScaledDepthBias = state->slopeScaleDepthBias;

	/* Bake the state! */
	res = ID3D11Device_CreateRasterizerState(
		renderer->device,
		&desc,
		&result
	);
	ERROR_CHECK_RETURN("Rasterizer state creation failed", NULL)
	PackedStateArray_Insert(
		&renderer->rasterizerStateCache,
		packedState,
		result
	);

	/* Return the state! */
	return result;
}

static ID3D11SamplerState* D3D11_INTERNAL_FetchSamplerState(
	D3D11Renderer *renderer,
	FNA3D_SamplerState *state
) {
	PackedState packedState;
	D3D11_SAMPLER_DESC desc;
	ID3D11SamplerState *result;
	HRESULT res;

	/* Can we just reuse an existing state? */
	packedState = GetPackedSamplerState(*state);
	result = (ID3D11SamplerState*) PackedStateArray_Fetch(
		renderer->samplerStateCache,
		packedState
	);
	if (result != NULL)
	{
		/* The state is already cached! */
		return result;
	}

	/* We have to make a new sampler state... */
	desc.AddressU = XNAToD3D_Wrap[state->addressU];
	desc.AddressV = XNAToD3D_Wrap[state->addressV];
	desc.AddressW = XNAToD3D_Wrap[state->addressW];
	desc.BorderColor[0] = 1.0f;
	desc.BorderColor[1] = 1.0f;
	desc.BorderColor[2] = 1.0f;
	desc.BorderColor[3] = 1.0f;
	desc.ComparisonFunc = D3D11_COMPARISON_NEVER;
	desc.Filter = XNAToD3D_Filter[state->filter];
	desc.MaxAnisotropy = (uint32_t) state->maxAnisotropy;
	desc.MaxLOD = D3D11_FLOAT32_MAX;
	desc.MinLOD = (float) state->maxMipLevel;
	desc.MipLODBias = state->mipMapLevelOfDetailBias;

	/* Bake the state! */
	res = ID3D11Device_CreateSamplerState(
		renderer->device,
		&desc,
		&result
	);
	ERROR_CHECK_RETURN("Sampler state creation failed", NULL)
	PackedStateArray_Insert(
		&renderer->samplerStateCache,
		packedState,
		result
	);

	/* Return the state! */
	return result;
}

static ID3D11InputLayout* D3D11_INTERNAL_FetchBindingsInputLayout(
	D3D11Renderer *renderer,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint32_t *hash
) {
	int32_t numElements, i, j, k, usage, index, attribLoc, bindingsIndex;
	uint8_t attrUse[MOJOSHADER_USAGE_TOTAL][16];
	D3D11_INPUT_ELEMENT_DESC elements[16]; /* D3DCAPS9 MaxStreams <= 16 */
	D3D11_INPUT_ELEMENT_DESC *d3dElement;
	MOJOSHADER_d3d11Shader *vertexShader, *blah;
	void *bytecode;
	int32_t bytecodeLength;
	HRESULT res;
	ID3D11InputLayout *result;

	/* We need the vertex shader... */
	MOJOSHADER_d3d11GetBoundShaders(&vertexShader, &blah);

	/* Can we just reuse an existing input layout? */
	result = (ID3D11InputLayout*) PackedVertexBufferBindingsArray_Fetch(
		renderer->inputLayoutCache,
		bindings,
		numBindings,
		vertexShader,
		&bindingsIndex,
		hash
	);
	if (result != NULL)
	{
		/* This input layout has already been cached! */
		return result;
	}

	/* We have to make a new input layout... */

	/* There's this weird case where you can have overlapping
	 * vertex usage/index combinations. It seems like the first
	 * attrib gets priority, so whenever a duplicate attribute
	 * exists, give it the next available index. If that fails, we
	 * have to crash :/
	 * -flibit
	 */
	SDL_zero(attrUse);

	/* Determine how many elements are actually in use */
	numElements = 0;
	for (i = 0; i < numBindings; i += 1)
	{
		/* Describe vertex attributes */
		const FNA3D_VertexBufferBinding *binding = &bindings[i];
		for (j = 0; j < binding->vertexDeclaration.elementCount; j += 1)
		{
			const FNA3D_VertexElement *element = &binding->vertexDeclaration.elements[j];
			usage = element->vertexElementUsage;
			index = element->usageIndex;

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
			attribLoc = MOJOSHADER_d3d11GetVertexAttribLocation(
				vertexShader,
				VertexAttribUsage(usage),
				index
			);
			if (attribLoc == -1)
			{
				/* Stream not in use! */
				continue;
			}

			numElements += 1;

			d3dElement = &elements[attribLoc];
			d3dElement->SemanticName = XNAToD3D_VertexAttribSemanticName[usage];
			d3dElement->SemanticIndex = index;
			d3dElement->Format = XNAToD3D_VertexAttribFormat[
				element->vertexElementFormat
			];
			d3dElement->InputSlot = i;
			d3dElement->AlignedByteOffset = element->offset;
			d3dElement->InputSlotClass = (
				binding->instanceFrequency > 0 ?
					D3D11_INPUT_PER_INSTANCE_DATA :
					D3D11_INPUT_PER_VERTEX_DATA
			);
			d3dElement->InstanceDataStepRate = (
				binding->instanceFrequency > 0 ?
					binding->instanceFrequency :
					0
			);
		}
	}

	MOJOSHADER_d3d11CompileVertexShader(
		(unsigned long long) *hash,
		elements,
		numElements,
		&bytecode,
		&bytecodeLength
	);
	res = ID3D11Device_CreateInputLayout(
		renderer->device,
		elements,
		numElements,
		bytecode,
		bytecodeLength,
		&result
	);

	/* Check for errors now that elements is freed */
	ERROR_CHECK_RETURN("Could not compile input layout", NULL)

	/* Return the new input layout! */
	PackedVertexBufferBindingsArray_Insert(
		&renderer->inputLayoutCache,
		bindings,
		numBindings,
		vertexShader,
		result
	);
	return result;
}

/* Forward Declarations */

static void D3D11_INTERNAL_DestroyFramebuffer(D3D11Renderer *renderer);

static void D3D11_SetRenderTargets(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
);
static void D3D11_GetTextureData2D(
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

static void D3D11_GetDrawableSize(void *window, int32_t *w, int32_t *h);

static void* D3D11_PLATFORM_LoadD3D11();
static void D3D11_PLATFORM_UnloadD3D11(void* module);
static PFN_D3D11_CREATE_DEVICE D3D11_PLATFORM_GetCreateDeviceFunc(void* module);

static void* D3D11_PLATFORM_LoadCompiler();
static void D3D11_PLATFORM_UnloadCompiler(void* module);
static PFN_D3DCOMPILE D3D11_PLATFORM_GetCompileFunc(void* module);

static HRESULT D3D11_PLATFORM_CreateDXGIFactory(
	void** module,
	void** factory
);
static void D3D11_PLATFORM_UnloadDXGI(void* module);
static void D3D11_PLATFORM_GetDefaultAdapter(
	void* factory,
	IDXGIAdapter1 **adapter
);

static void D3D11_PLATFORM_CreateSwapChain(
	D3D11Renderer *renderer,
	FNA3D_PresentationParameters *pp
);

/* Renderer Implementation */

/* Quit */

static void D3D11_DestroyDevice(FNA3D_Device *device)
{
	D3D11Renderer* renderer = (D3D11Renderer*) device->driverData;
	int32_t i;

	/* Unbind all render objects */
	ID3D11DeviceContext_ClearState(renderer->context);

	/* Release faux backbuffer and swapchain */
	D3D11_INTERNAL_DestroyFramebuffer(renderer);
	ID3D11BlendState_Release(renderer->fauxBlendState);
	ID3D11Buffer_Release(renderer->fauxBlitIndexBuffer);
	ID3D11InputLayout_Release(renderer->fauxBlitLayout);
	ID3D11PixelShader_Release(renderer->fauxBlitPS);
	ID3D11SamplerState_Release(renderer->fauxBlitSampler);
	ID3D11VertexShader_Release(renderer->fauxBlitVS);
	ID3D11RasterizerState_Release(renderer->fauxRasterizer);
	ID3D11Buffer_Release(renderer->fauxBlitVertexBuffer);
	IDXGISwapChain_Release(renderer->swapchain);

	/* Release blend states */
	for (i = 0; i < renderer->blendStateCache.count; i += 1)
	{
		ID3D11BlendState_Release(
			(ID3D11BlendState*) renderer->blendStateCache.elements[i].value
		);
	}
	SDL_free(renderer->blendStateCache.elements);

	/* Release depth stencil states */
	for (i = 0; i < renderer->depthStencilStateCache.count; i += 1)
	{
		ID3D11DepthStencilState_Release(
			(ID3D11DepthStencilState*) renderer->depthStencilStateCache.elements[i].value
		);
	}
	SDL_free(renderer->depthStencilStateCache.elements);

	/* Release rasterizer states */
	for (i = 0; i < renderer->rasterizerStateCache.count; i += 1)
	{
		ID3D11RasterizerState_Release(
			(ID3D11RasterizerState*) renderer->rasterizerStateCache.elements[i].value
		);
	}
	SDL_free(renderer->rasterizerStateCache.elements);

	/* Release sampler states */
	for (i = 0; i < renderer->samplerStateCache.count; i += 1)
	{
		ID3D11SamplerState_Release(
			(ID3D11SamplerState*) renderer->samplerStateCache.elements[i].value
		);
	}
	SDL_free(renderer->samplerStateCache.elements);

	/* Release input layouts */
	for (i = 0; i < renderer->inputLayoutCache.count; i += 1)
	{
		ID3D11InputLayout_Release(
			(ID3D11InputLayout*) renderer->inputLayoutCache.elements[i].value
		);
	}
	SDL_free(renderer->inputLayoutCache.elements);

	/* Release the annotation, if applicable */
	if (renderer->annotation != NULL)
	{
		ID3DUserDefinedAnnotation_Release(renderer->annotation);
	}

	/* Release the factory */
	IUnknown_Release((IUnknown*) renderer->factory);

	/* Release the MojoShader context */
	MOJOSHADER_d3d11DestroyContext();

	/* Release the device */
	ID3D11DeviceContext_Release(renderer->context);
	ID3D11Device_Release(renderer->device);

	/* Release the DLLs */
	D3D11_PLATFORM_UnloadD3D11(renderer->d3d11_dll);
	D3D11_PLATFORM_UnloadDXGI(renderer->dxgi_dll);

	SDL_DestroyMutex(renderer->ctxLock);
	SDL_free(renderer);
	SDL_free(device);
}

/* Presentation */

static void D3D11_INTERNAL_UpdateBackbufferVertexBuffer(
	D3D11Renderer *renderer,
	FNA3D_Rect *srcRect,
	FNA3D_Rect *dstRect,
	int32_t drawableWidth,
	int32_t drawableHeight
) {
	float backbufferWidth = (float) renderer->backbuffer.width;
	float backbufferHeight = (float) renderer->backbuffer.height;
	float sx0, sy0, sx1, sy1;
	float dx0, dy0, dx1, dy1;
	float data[16];
	D3D11_MAPPED_SUBRESOURCE mappedBuffer;
	HRESULT res;

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
	mappedBuffer.pData = NULL;
	mappedBuffer.DepthPitch = 0;
	mappedBuffer.RowPitch = 0;
	SDL_LockMutex(renderer->ctxLock);
	res = ID3D11DeviceContext_Map(
		renderer->context,
		(ID3D11Resource*) renderer->fauxBlitVertexBuffer,
		0,
		D3D11_MAP_WRITE_DISCARD,
		0,
		&mappedBuffer
	);
	ERROR_CHECK_UNLOCK_RETURN("Could not map backbuffer vertex buffer for writing",)
	SDL_memcpy(mappedBuffer.pData, data, sizeof(data));
	ID3D11DeviceContext_Unmap(
		renderer->context,
		(ID3D11Resource*) renderer->fauxBlitVertexBuffer,
		0
	);
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_INTERNAL_BlitFramebuffer(
	D3D11Renderer *renderer,
	int32_t drawableWidth,
	int32_t drawableHeight
) {
	DXGI_SWAP_CHAIN_DESC swapchainDesc;
	D3D11_VIEWPORT tempViewport;
	const uint32_t vertexStride = 16;
	const uint32_t offsets[] = { 0 };
	float blendFactor[] = { 1.0f, 1.0f, 1.0f, 1.0f };
	ID3D11VertexShader *oldVertexShader;
	ID3D11PixelShader *oldPixelShader;
	ID3D11ClassInstance *whatever;
	uint32_t noReallyWhatever = 0;
	D3D11_VIEWPORT origViewport =
	{
		(float) renderer->viewport.x,
		(float) renderer->viewport.y,
		(float) renderer->viewport.w,
		(float) renderer->viewport.h,
		renderer->viewport.minDepth,
		renderer->viewport.maxDepth
	};

	SDL_LockMutex(renderer->ctxLock);

	/* HACK ahead: During a window resize operation, the swapchain size does not necessarily
	 *  match the size of the drawable. As a result, we need to set our viewport to match the
	 *  size of the swapchain instead of the size of the drawable.
	 * If we don't do this, during the resize the damaged area will contain garbage pixels and
	 *  the framebuffer will be drawn at the incorrect size as well.
	 * The result of this hack is that the framebuffer is scaled up to match whatever the size
	 *  of the drawable happens to be (this will introduce some blurring, and does not match
	 *  the behavior of OpenGL/Vulkan - just show black pixels in the damaged area - but is
	 *  better than the alternatives.)
	 * The ideal solution would be to resize the swapchain to fit, but resizing a swapchain
	 *  requires us to first release any references to its backbuffers, so attempting to
	 *  resize it here will always fail.
	 */
	IDXGISwapChain_GetDesc(renderer->swapchain, &swapchainDesc);
	tempViewport.TopLeftX = 0;
	tempViewport.TopLeftY = 0;
	tempViewport.Width = (float) swapchainDesc.BufferDesc.Width;
	tempViewport.Height = (float) swapchainDesc.BufferDesc.Height;
	tempViewport.MinDepth = 0;
	tempViewport.MaxDepth = 1;

	/* Push the current shader state */
	ID3D11DeviceContext_VSGetShader(
		renderer->context,
		&oldVertexShader,
		&whatever,
		&noReallyWhatever
	);
	ID3D11DeviceContext_PSGetShader(
		renderer->context,
		&oldPixelShader,
		&whatever,
		&noReallyWhatever
	);

	/* Bind the swapchain render target */
	ID3D11DeviceContext_OMSetRenderTargets(
		renderer->context,
		1,
		&renderer->swapchainRTView,
		NULL
	);

	/* Bind the vertex and index buffers */
	ID3D11DeviceContext_IASetVertexBuffers(
		renderer->context,
		0,
		1,
		&renderer->fauxBlitVertexBuffer,
		&vertexStride,
		offsets
	);
	ID3D11DeviceContext_IASetIndexBuffer(
		renderer->context,
		renderer->fauxBlitIndexBuffer,
		DXGI_FORMAT_R16_UINT,
		0
	);

	/* Set the rest of the pipeline state */
	ID3D11DeviceContext_RSSetViewports(
		renderer->context,
		1,
		&tempViewport
	);
	ID3D11DeviceContext_OMSetBlendState(
		renderer->context,
		renderer->fauxBlendState,
		blendFactor,
		0xffffffff
	);
	ID3D11DeviceContext_OMSetDepthStencilState(
		renderer->context,
		NULL,
		0
	);
	ID3D11DeviceContext_RSSetState(
		renderer->context,
		renderer->fauxRasterizer
	);
	ID3D11DeviceContext_IASetInputLayout(
		renderer->context,
		renderer->fauxBlitLayout
	);
	ID3D11DeviceContext_VSSetShader(
		renderer->context,
		renderer->fauxBlitVS,
		NULL,
		0
	);
	ID3D11DeviceContext_PSSetShader(
		renderer->context,
		renderer->fauxBlitPS,
		NULL,
		0
	);
	ID3D11DeviceContext_PSSetShaderResources(
		renderer->context,
		0,
		1,
		&renderer->backbuffer.shaderView
	);
	ID3D11DeviceContext_PSSetSamplers(
		renderer->context,
		0,
		1,
		&renderer->fauxBlitSampler
	);
	if (renderer->topology != FNA3D_PRIMITIVETYPE_TRIANGLELIST)
	{
		renderer->topology = FNA3D_PRIMITIVETYPE_TRIANGLELIST;
		ID3D11DeviceContext_IASetPrimitiveTopology(
			renderer->context,
			D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST
		);
	}

	/* Draw the faux backbuffer! */
	ID3D11DeviceContext_DrawIndexed(renderer->context, 6, 0, 0);

	/* Restore the old state */
	blendFactor[0] = renderer->blendFactor.r / 255.0f;
	blendFactor[1] = renderer->blendFactor.g / 255.0f;
	blendFactor[2] = renderer->blendFactor.b / 255.0f;
	blendFactor[3] = renderer->blendFactor.a / 255.0f;
	ID3D11DeviceContext_RSSetViewports(
		renderer->context,
		1,
		&origViewport
	);
	ID3D11DeviceContext_OMSetBlendState(
		renderer->context,
		renderer->blendState,
		blendFactor,
		renderer->multiSampleMask
	);
	ID3D11DeviceContext_OMSetDepthStencilState(
		renderer->context,
		renderer->depthStencilState,
		renderer->stencilRef
	);
	ID3D11DeviceContext_RSSetState(
		renderer->context,
		renderer->rasterizerState
	);
	ID3D11DeviceContext_IASetInputLayout(
		renderer->context,
		renderer->inputLayout
	);
	ID3D11DeviceContext_VSSetShader(
		renderer->context,
		oldVertexShader,
		NULL,
		0
	);
	ID3D11DeviceContext_PSSetShader(
		renderer->context,
		oldPixelShader,
		NULL,
		0
	);
	if (oldVertexShader != NULL)
	{
		ID3D11VertexShader_Release(oldVertexShader);
	}
	if (oldPixelShader != NULL)
	{
		ID3D11PixelShader_Release(oldPixelShader);
	}
	ID3D11DeviceContext_IASetVertexBuffers(
		renderer->context,
		0,
		MAX_BOUND_VERTEX_BUFFERS,
		renderer->vertexBuffers,
		renderer->vertexBufferStrides,
		renderer->vertexBufferOffsets
	);
	ID3D11DeviceContext_IASetIndexBuffer(
		renderer->context,
		renderer->indexBuffer,
		XNAToD3D_IndexType[renderer->indexElementSize],
		0
	);
	ID3D11DeviceContext_PSSetShaderResources(
		renderer->context,
		0,
		1,
		&renderer->textures[0]->shaderView
	);
	ID3D11DeviceContext_PSSetSamplers(
		renderer->context,
		0,
		1,
		&renderer->samplers[0]
	);

	/* Bind the faux-backbuffer */
	D3D11_SetRenderTargets(
		(FNA3D_Renderer*) renderer,
		NULL,
		0,
		NULL,
		FNA3D_DEPTHFORMAT_NONE,
		0
	);

	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_SwapBuffers(
	FNA3D_Renderer *driverData,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	int32_t drawableWidth, drawableHeight;
	FNA3D_Rect srcRect, dstRect;

	/* Determine the regions to present */
	D3D11_GetDrawableSize(
		overrideWindowHandle,
		&drawableWidth,
		&drawableHeight
	);
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
		srcRect.w = renderer->backbuffer.width;
		srcRect.h = renderer->backbuffer.height;
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
		dstRect.w = drawableWidth;
		dstRect.h = drawableHeight;
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
		D3D11_INTERNAL_UpdateBackbufferVertexBuffer(
			renderer,
			&srcRect,
			&dstRect,
			drawableWidth,
			drawableHeight
		);
	}

	SDL_LockMutex(renderer->ctxLock);

	/* Resolve the faux-backbuffer if needed */
	if (renderer->backbuffer.multiSampleCount > 1)
	{
		ID3D11DeviceContext_ResolveSubresource(
			renderer->context,
			(ID3D11Resource*) renderer->backbuffer.resolveBuffer,
			0,
			(ID3D11Resource*) renderer->backbuffer.colorBuffer,
			0,
			XNAToD3D_TextureFormat[renderer->backbuffer.surfaceFormat]
		);
	}

	/* "Blit" the faux-backbuffer to the swapchain image */
	D3D11_INTERNAL_BlitFramebuffer(renderer, drawableWidth, drawableHeight);

	SDL_UnlockMutex(renderer->ctxLock);

	/* Present! */
	IDXGISwapChain_Present(renderer->swapchain, renderer->syncInterval, 0);
}

/* Drawing */

static void D3D11_Clear(
	FNA3D_Renderer *driverData,
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	int32_t i;
	uint32_t dsClearFlags;
	float clearColor[4] = {color->x, color->y, color->z, color->w};

	SDL_LockMutex(renderer->ctxLock);

	/* Clear color? */
	if (options & FNA3D_CLEAROPTIONS_TARGET)
	{
		for (i = 0; i < renderer->numRenderTargets; i += 1)
		{
			/* Clear! */
			ID3D11DeviceContext_ClearRenderTargetView(
				renderer->context,
				renderer->renderTargetViews[i],
				clearColor
			);
		}
	}

	/* Clear depth/stencil? */
	dsClearFlags = 0;
	if (options & FNA3D_CLEAROPTIONS_DEPTHBUFFER)
	{
		dsClearFlags |= D3D11_CLEAR_DEPTH;
	}
	if (options & FNA3D_CLEAROPTIONS_STENCIL)
	{
		dsClearFlags |= D3D11_CLEAR_STENCIL;
	}
	if (dsClearFlags != 0 && renderer->depthStencilView != NULL)
	{
		/* Clear! */
		ID3D11DeviceContext_ClearDepthStencilView(
			renderer->context,
			renderer->depthStencilView,
			dsClearFlags,
			depth,
			(uint8_t) stencil
		);
	}

	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_DrawIndexedPrimitives(
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
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *d3dIndices = (D3D11Buffer*) indices;

	SDL_LockMutex(renderer->ctxLock);

	/* Bind index buffer */
	if (	renderer->indexBuffer != d3dIndices->handle ||
		renderer->indexElementSize != indexElementSize 	)
	{
		renderer->indexBuffer = d3dIndices->handle;
		renderer->indexElementSize = indexElementSize;
		ID3D11DeviceContext_IASetIndexBuffer(
			renderer->context,
			d3dIndices->handle,
			XNAToD3D_IndexType[indexElementSize],
			0
		);
	}

	/* Set up draw state */
	if (renderer->topology != primitiveType)
	{
		renderer->topology = primitiveType;
		ID3D11DeviceContext_IASetPrimitiveTopology(
			renderer->context,
			XNAToD3D_Primitive[primitiveType]
		);
	}

	/* Draw! */
	ID3D11DeviceContext_DrawIndexed(
		renderer->context,
		PrimitiveVerts(primitiveType, primitiveCount),
		(uint32_t) startIndex,
		baseVertex
	);

	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_DrawInstancedPrimitives(
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
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *d3dIndices = (D3D11Buffer*) indices;

	SDL_LockMutex(renderer->ctxLock);

	/* Bind index buffer */
	if (	renderer->indexBuffer != d3dIndices->handle ||
		renderer->indexElementSize != indexElementSize 	)
	{
		renderer->indexBuffer = d3dIndices->handle;
		renderer->indexElementSize = indexElementSize;
		ID3D11DeviceContext_IASetIndexBuffer(
			renderer->context,
			d3dIndices->handle,
			XNAToD3D_IndexType[indexElementSize],
			0
		);
	}

	/* Set up draw state */
	if (renderer->topology != primitiveType)
	{
		renderer->topology = primitiveType;
		ID3D11DeviceContext_IASetPrimitiveTopology(
			renderer->context,
			XNAToD3D_Primitive[primitiveType]
		);
	}

	/* Draw! */
	ID3D11DeviceContext_DrawIndexedInstanced(
		renderer->context,
		PrimitiveVerts(primitiveType, primitiveCount),
		instanceCount,
		(uint32_t) startIndex,
		baseVertex,
		0
	);

	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_DrawPrimitives(
	FNA3D_Renderer *driverData,
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;

	SDL_LockMutex(renderer->ctxLock);

	/* Bind draw state */
	if (renderer->topology != primitiveType)
	{
		renderer->topology = primitiveType;
		ID3D11DeviceContext_IASetPrimitiveTopology(
			renderer->context,
			XNAToD3D_Primitive[primitiveType]
		);
	}

	/* Draw! */
	ID3D11DeviceContext_Draw(
		renderer->context,
		(uint32_t) PrimitiveVerts(primitiveType, primitiveCount),
		(uint32_t) vertexStart
	);

	SDL_UnlockMutex(renderer->ctxLock);
}

/* Mutable Render States */

static void D3D11_SetViewport(FNA3D_Renderer *driverData, FNA3D_Viewport *viewport)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11_VIEWPORT vp =
	{
		(float) viewport->x,
		(float) viewport->y,
		(float) viewport->w,
		(float) viewport->h,
		viewport->minDepth,
		viewport->maxDepth
	};

	if (	renderer->viewport.x != viewport->x ||
		renderer->viewport.y != viewport->y ||
		renderer->viewport.w != viewport->w ||
		renderer->viewport.h != viewport->h ||
		renderer->viewport.minDepth != viewport->minDepth ||
		renderer->viewport.maxDepth != viewport->maxDepth	)
	{
		SDL_LockMutex(renderer->ctxLock);
		SDL_memcpy(&renderer->viewport, viewport, sizeof(FNA3D_Viewport));
		ID3D11DeviceContext_RSSetViewports(
			renderer->context,
			1,
			&vp
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

static void D3D11_SetScissorRect(FNA3D_Renderer *driverData, FNA3D_Rect *scissor)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11_RECT rect =
	{
		scissor->x,
		scissor->y,
		scissor->x + scissor->w,
		scissor->y + scissor->h
	};

	if (	renderer->scissorRect.x != scissor->x ||
		renderer->scissorRect.y != scissor->y ||
		renderer->scissorRect.w != scissor->w ||
		renderer->scissorRect.h != scissor->h	)
	{
		SDL_LockMutex(renderer->ctxLock);
		SDL_memcpy(&renderer->scissorRect, scissor, sizeof(FNA3D_Rect));
		ID3D11DeviceContext_RSSetScissorRects(
			renderer->context,
			1,
			&rect
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

static void D3D11_GetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	*blendFactor = renderer->blendFactor;
}

static void D3D11_SetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	float factor[4];
	if (!D3D11_INTERNAL_BlendEquals(&renderer->blendFactor, blendFactor))
	{
		factor[0] = blendFactor->r / 255.0f;
		factor[1] = blendFactor->g / 255.0f;
		factor[2] = blendFactor->b / 255.0f;
		factor[3] = blendFactor->a / 255.0f;
		renderer->blendFactor = *blendFactor;
		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_OMSetBlendState(
			renderer->context,
			renderer->blendState,
			factor,
			renderer->multiSampleMask
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

static int32_t D3D11_GetMultiSampleMask(FNA3D_Renderer *driverData)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	return renderer->multiSampleMask;
}

static void D3D11_SetMultiSampleMask(FNA3D_Renderer *driverData, int32_t mask)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	float factor[4];
	if (renderer->multiSampleMask != mask)
	{
		renderer->multiSampleMask = mask;
		factor[0] = renderer->blendFactor.r / 255.0f;
		factor[1] = renderer->blendFactor.g / 255.0f;
		factor[2] = renderer->blendFactor.b / 255.0f;
		factor[3] = renderer->blendFactor.a / 255.0f;
		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_OMSetBlendState(
			renderer->context,
			renderer->blendState,
			factor,
			renderer->multiSampleMask
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

static int32_t D3D11_GetReferenceStencil(FNA3D_Renderer *driverData)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	return renderer->stencilRef;
}

static void D3D11_SetReferenceStencil(FNA3D_Renderer *driverData, int32_t ref)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	if (renderer->stencilRef != ref)
	{
		renderer->stencilRef = ref;
		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_OMSetDepthStencilState(
			renderer->context,
			renderer->depthStencilState,
			(uint32_t) renderer->stencilRef
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

/* Immutable Render States */

static void D3D11_SetBlendState(
	FNA3D_Renderer *driverData,
	FNA3D_BlendState *blendState
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	ID3D11BlendState *bs = D3D11_INTERNAL_FetchBlendState(renderer, blendState);
	float factor[4];

	if (	renderer->blendState != bs ||
		!D3D11_INTERNAL_BlendEquals(&renderer->blendFactor, &blendState->blendFactor) ||
		renderer->multiSampleMask != blendState->multiSampleMask	)
	{
		renderer->blendState = bs;
		factor[0] = blendState->blendFactor.r / 255.0f;
		factor[1] = blendState->blendFactor.g / 255.0f;
		factor[2] = blendState->blendFactor.b / 255.0f;
		factor[3] = blendState->blendFactor.a / 255.0f;
		renderer->blendFactor = blendState->blendFactor;
		renderer->multiSampleMask = blendState->multiSampleMask;
		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_OMSetBlendState(
			renderer->context,
			bs,
			factor,
			(uint32_t) renderer->multiSampleMask
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

static void D3D11_SetDepthStencilState(
	FNA3D_Renderer *driverData,
	FNA3D_DepthStencilState *depthStencilState
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	ID3D11DepthStencilState *ds = D3D11_INTERNAL_FetchDepthStencilState(
		renderer,
		depthStencilState
	);

	if (	renderer->depthStencilState != ds ||
		renderer->stencilRef != depthStencilState->referenceStencil	)
	{
		renderer->depthStencilState = ds;
		renderer->stencilRef = depthStencilState->referenceStencil;
		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_OMSetDepthStencilState(
			renderer->context,
			ds,
			(uint32_t) renderer->stencilRef
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

static void D3D11_ApplyRasterizerState(
	FNA3D_Renderer *driverData,
	FNA3D_RasterizerState *rasterizerState
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	ID3D11RasterizerState *rs = D3D11_INTERNAL_FetchRasterizerState(
		renderer,
		rasterizerState
	);

	if (renderer->rasterizerState != rs)
	{
		renderer->rasterizerState = rs;
		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_RSSetState(
			renderer->context,
			rs
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

static void D3D11_VerifySampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *d3dTexture = (D3D11Texture*) texture;
	ID3D11SamplerState *d3dSamplerState;

	if (texture == NULL)
	{
		if (renderer->textures[index] != &NullTexture)
		{
			renderer->textures[index] = &NullTexture;
			renderer->samplers[index] = NULL;
			SDL_LockMutex(renderer->ctxLock);
			if (index < MAX_TEXTURE_SAMPLERS)
			{
				ID3D11DeviceContext_PSSetShaderResources(
					renderer->context,
					index,
					1,
					&NullTexture.shaderView
				);
				ID3D11DeviceContext_PSSetSamplers(
					renderer->context,
					index,
					1,
					&renderer->samplers[index]
				);
			}
			else
			{
				ID3D11DeviceContext_VSSetShaderResources(
					renderer->context,
					index - MAX_TEXTURE_SAMPLERS,
					1,
					&NullTexture.shaderView
				);
				ID3D11DeviceContext_VSSetSamplers(
					renderer->context,
					index - MAX_TEXTURE_SAMPLERS,
					1,
					&renderer->samplers[index]
				);
			}
			SDL_UnlockMutex(renderer->ctxLock);
		}
		return;
	}

	/* Bind the correct texture */
	if (d3dTexture != renderer->textures[index])
	{
		renderer->textures[index] = d3dTexture;
		SDL_LockMutex(renderer->ctxLock);
		if (index < MAX_TEXTURE_SAMPLERS)
		{
			ID3D11DeviceContext_PSSetShaderResources(
				renderer->context,
				index,
				1,
				&d3dTexture->shaderView
			);
		}
		else
		{
			ID3D11DeviceContext_VSSetShaderResources(
				renderer->context,
				index - MAX_TEXTURE_SAMPLERS,
				1,
				&d3dTexture->shaderView
			);
		}
		SDL_UnlockMutex(renderer->ctxLock);
	}

	/* Update the sampler state, if needed */
	d3dSamplerState = D3D11_INTERNAL_FetchSamplerState(
		renderer,
		sampler
	);
	if (d3dSamplerState != renderer->samplers[index])
	{
		renderer->samplers[index] = d3dSamplerState;
		SDL_LockMutex(renderer->ctxLock);
		if (index < MAX_TEXTURE_SAMPLERS)
		{
			ID3D11DeviceContext_PSSetSamplers(
				renderer->context,
				index,
				1,
				&d3dSamplerState
			);
		}
		else
		{
			ID3D11DeviceContext_VSSetSamplers(
				renderer->context,
				index - MAX_TEXTURE_SAMPLERS,
				1,
				&d3dSamplerState
			);
		}
		SDL_UnlockMutex(renderer->ctxLock);
	}
}

static void D3D11_VerifyVertexSampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	D3D11_VerifySampler(
		driverData,
		MAX_TEXTURE_SAMPLERS + index,
		texture,
		sampler
	);
}

static void D3D11_ApplyVertexBufferBindings(
	FNA3D_Renderer *driverData,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *vertexBuffer;
	ID3D11InputLayout *inputLayout;
	int32_t i, stride, offset;
	uint32_t hash;

	SDL_LockMutex(renderer->ctxLock);

	if (!bindingsUpdated && !renderer->effectApplied)
	{
		SDL_UnlockMutex(renderer->ctxLock);
		return;
	}

	/* Translate the bindings array into an input layout */
	inputLayout = D3D11_INTERNAL_FetchBindingsInputLayout(
		renderer,
		bindings,
		numBindings,
		&hash
	);

	if (renderer->inputLayout != inputLayout)
	{
		renderer->inputLayout = inputLayout;
		ID3D11DeviceContext_IASetInputLayout(
			renderer->context,
			inputLayout
		);
	}

	/* Bind the vertex buffers */
	for (i = 0; i < numBindings; i += 1)
	{
		vertexBuffer = (D3D11Buffer*) bindings[i].vertexBuffer;
		stride = bindings[i].vertexDeclaration.vertexStride;
		if (	renderer->vertexBuffers[i] != vertexBuffer->handle ||
			renderer->vertexBufferStrides[i] != stride ||
			renderer->vertexBufferOffsets[i] != bindings[i].vertexOffset	)
		{
			renderer->vertexBuffers[i] = vertexBuffer->handle;
			if (vertexBuffer == NULL)
			{
				renderer->vertexBufferStrides[i] = 0;
				renderer->vertexBufferOffsets[i] = 0;
				continue;
			}

			offset = bindings[i].vertexOffset * stride;
			ID3D11DeviceContext_IASetVertexBuffers(
				renderer->context,
				i,
				1,
				&vertexBuffer->handle,
				(uint32_t*) &stride,
				(uint32_t*) &offset
			);

			renderer->vertexBufferOffsets[i] = offset;
			renderer->vertexBufferStrides[i] = stride;
		}
	}

	MOJOSHADER_d3d11ProgramReady((unsigned long long) hash);
	renderer->effectApplied = 0;

	SDL_UnlockMutex(renderer->ctxLock);
}

/* Render Targets */

static void D3D11_INTERNAL_RestoreTargetTextures(D3D11Renderer *renderer)
{
	/* For textures that were bound while this target was active, rebind.
	 * D3D11 implicitly unsets these to prevent simultaneous read/write.
	 * -flibit
	 */
	int32_t i, j, k;
	uint8_t bound;
	for (i = 0; i < renderer->numRenderTargets; i += 1)
	{
		const ID3D11RenderTargetView *view = renderer->renderTargetViews[i];
		for (j = 0; j < MAX_TOTAL_SAMPLERS; j += 1)
		{
			const D3D11Texture *texture = renderer->textures[j];
			if (!texture->isRenderTarget)
			{
				continue;
			}
			if (texture->rtType == FNA3D_RENDERTARGET_TYPE_2D)
			{
				bound = (texture->twod.rtView == view);
			}
			else
			{
				bound = 0;
				for (k = 0; k < 6; k += 1)
				{
					if (texture->cube.rtViews[k] == view)
					{
						bound = 1;
						break;
					}
				}
			}
			if (bound)
			{
				if (j < MAX_TEXTURE_SAMPLERS)
				{
					ID3D11DeviceContext_PSSetShaderResources(
						renderer->context,
						j,
						1,
						&texture->shaderView
					);
					ID3D11DeviceContext_PSSetSamplers(
						renderer->context,
						j,
						1,
						&renderer->samplers[j]
					);
				}
				else
				{
					ID3D11DeviceContext_VSSetShaderResources(
						renderer->context,
						j - MAX_TEXTURE_SAMPLERS,
						1,
						&texture->shaderView
					);
					ID3D11DeviceContext_VSSetSamplers(
						renderer->context,
						j - MAX_TEXTURE_SAMPLERS,
						1,
						&renderer->samplers[j]
					);
				}
			}
		}
	}
}

static void D3D11_SetRenderTargets(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *tex;
	D3D11Renderbuffer *rb;
	ID3D11RenderTargetView *views[MAX_RENDERTARGET_BINDINGS];
	int32_t i;

	/* Bind the backbuffer, if applicable */
	if (numRenderTargets <= 0)
	{
		views[0] = renderer->backbuffer.colorView;
		renderer->currentDepthFormat = renderer->backbuffer.depthFormat;
		renderer->depthStencilView = renderer->backbuffer.depthStencilView;

		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_OMSetRenderTargets(
			renderer->context,
			1,
			views,
			renderer->depthStencilView
		);
		D3D11_INTERNAL_RestoreTargetTextures(renderer);
		SDL_UnlockMutex(renderer->ctxLock);

		renderer->renderTargetViews[0] = views[0];
		for (i = 1; i < MAX_RENDERTARGET_BINDINGS; i += 1)
		{
			renderer->renderTargetViews[i] = NULL;
		}
		renderer->numRenderTargets = 1;
		return;
	}

	/* Update color buffers */
	for (i = 0; i < numRenderTargets; i += 1)
	{
		if (renderTargets[i].colorBuffer != NULL)
		{
			rb = (D3D11Renderbuffer*) renderTargets[i].colorBuffer;
			views[i] = rb->color.rtView;
		}
		else
		{
			tex = (D3D11Texture*) renderTargets[i].texture;
			if (tex->rtType == FNA3D_RENDERTARGET_TYPE_2D)
			{
				views[i] = tex->twod.rtView;
			}
			else if (tex->rtType == FNA3D_RENDERTARGET_TYPE_CUBE)
			{
				views[i] = tex->cube.rtViews[
					renderTargets[i].cube.face
				];
			}
		}
	}
	while (i < MAX_RENDERTARGET_BINDINGS)
	{
		views[i++] = NULL;
	}

	/* Update depth stencil buffer */
	renderer->depthStencilView = (
		depthStencilBuffer == NULL ?
			NULL :
			((D3D11Renderbuffer*) depthStencilBuffer)->depth.dsView
	);
	renderer->currentDepthFormat = (
		depthStencilBuffer == NULL ?
			FNA3D_DEPTHFORMAT_NONE :
			depthFormat
	);

	/* Actually set the render targets, finally. */
	SDL_LockMutex(renderer->ctxLock);
	ID3D11DeviceContext_OMSetRenderTargets(
		renderer->context,
		numRenderTargets,
		views,
		renderer->depthStencilView
	);
	D3D11_INTERNAL_RestoreTargetTextures(renderer);
	SDL_UnlockMutex(renderer->ctxLock);

	/* Remember color attachments */
	SDL_memcpy(renderer->renderTargetViews, views, sizeof(views));
	renderer->numRenderTargets = numRenderTargets;
}

static void D3D11_ResolveTarget(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *target
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *tex = (D3D11Texture*) target->texture;
	D3D11Renderbuffer *rb = (D3D11Renderbuffer*) target->colorBuffer;
	uint32_t slice = 0;

	SDL_LockMutex(renderer->ctxLock);

	if (target->multiSampleCount > 0)
	{
		if (target->type == FNA3D_RENDERTARGET_TYPE_CUBE)
		{
			slice = target->cube.face;
		}
		ID3D11DeviceContext_ResolveSubresource(
			renderer->context,
			(ID3D11Resource*) tex->handle,
			D3D11_INTERNAL_CalcSubresource(0, slice, tex->levelCount),
			(ID3D11Resource*) rb->handle,
			0,
			XNAToD3D_TextureFormat[tex->format]
		);
	}

	/* If the target has mipmaps, regenerate them now */
	if (target->levelCount > 1)
	{
		ID3D11DeviceContext_GenerateMips(
			renderer->context,
			tex->shaderView
		);
	}

	SDL_UnlockMutex(renderer->ctxLock);
}

/* Backbuffer Functions */

static void D3D11_INTERNAL_CreateFramebuffer(
	D3D11Renderer *renderer,
	FNA3D_PresentationParameters *presentationParameters
) {
	int32_t w, h;
	HRESULT res;
	D3D11_TEXTURE2D_DESC colorBufferDesc;
	D3D11_RENDER_TARGET_VIEW_DESC colorViewDesc;
	D3D11_SHADER_RESOURCE_VIEW_DESC shaderViewDesc;
	D3D11_TEXTURE2D_DESC depthStencilDesc;
	D3D11_DEPTH_STENCIL_VIEW_DESC depthStencilViewDesc;
	D3D11_RENDER_TARGET_VIEW_DESC swapchainViewDesc;
	ID3D11Texture2D *swapchainTexture;

	#define BB renderer->backbuffer

	/* Update the backbuffer size */
	w = presentationParameters->backBufferWidth;
	h = presentationParameters->backBufferHeight;
	if (BB.width != w || BB.height != h)
	{
		renderer->backbufferSizeChanged = 1;
	}
	BB.width = w;
	BB.height = h;

	/* Update other presentation parameters */
	BB.surfaceFormat = presentationParameters->backBufferFormat;
	BB.depthFormat = presentationParameters->depthStencilFormat;
	BB.multiSampleCount = presentationParameters->multiSampleCount;

	/* Update color buffer to the new resolution */
	colorBufferDesc.Width = BB.width;
	colorBufferDesc.Height = BB.height;
	colorBufferDesc.MipLevels = 1;
	colorBufferDesc.ArraySize = 1;
	colorBufferDesc.Format = XNAToD3D_TextureFormat[BB.surfaceFormat];
	colorBufferDesc.SampleDesc.Count = (BB.multiSampleCount > 1 ? BB.multiSampleCount : 1);
	colorBufferDesc.SampleDesc.Quality = 0;
	colorBufferDesc.Usage = D3D11_USAGE_DEFAULT;
	colorBufferDesc.BindFlags = D3D11_BIND_RENDER_TARGET;
	if (BB.multiSampleCount <= 1)
	{
		colorBufferDesc.BindFlags |= D3D11_BIND_SHADER_RESOURCE;
	}
	colorBufferDesc.CPUAccessFlags = 0;
	colorBufferDesc.MiscFlags = 0;
	res = ID3D11Device_CreateTexture2D(
		renderer->device,
		&colorBufferDesc,
		NULL,
		&BB.colorBuffer
	);
	ERROR_CHECK_RETURN("Backbuffer color buffer creation failed",)

	/* Update color buffer view */
	colorViewDesc.Format = colorBufferDesc.Format;
	if (BB.multiSampleCount > 1)
	{
		colorViewDesc.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2DMS;
	}
	else
	{
		colorViewDesc.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2D;
		colorViewDesc.Texture2D.MipSlice = 0;
	}
	res = ID3D11Device_CreateRenderTargetView(
		renderer->device,
		(ID3D11Resource*) BB.colorBuffer,
		&colorViewDesc,
		&BB.colorView
	);
	ERROR_CHECK_RETURN("Backbuffer color buffer RT view creation failed",)

	/* Update resolve texture, if applicable */
	if (BB.multiSampleCount > 1)
	{
		colorBufferDesc.Width = BB.width;
		colorBufferDesc.Height = BB.height;
		colorBufferDesc.MipLevels = 1;
		colorBufferDesc.ArraySize = 1;
		colorBufferDesc.Format = XNAToD3D_TextureFormat[BB.surfaceFormat];
		colorBufferDesc.SampleDesc.Count = 1;
		colorBufferDesc.SampleDesc.Quality = 0;
		colorBufferDesc.Usage = D3D11_USAGE_DEFAULT;
		colorBufferDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
		colorBufferDesc.CPUAccessFlags = 0;
		colorBufferDesc.MiscFlags = 0;
		res = ID3D11Device_CreateTexture2D(
			renderer->device,
			&colorBufferDesc,
			NULL,
			&BB.resolveBuffer
		);
		ERROR_CHECK_RETURN("Backbuffer multisample resolve buffer creation failed",)
	}

	/* Update shader resource view */
	shaderViewDesc.Format = colorBufferDesc.Format;
	shaderViewDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
	shaderViewDesc.Texture2D.MipLevels = 1;
	shaderViewDesc.Texture2D.MostDetailedMip = 0;
	res = ID3D11Device_CreateShaderResourceView(
		renderer->device,
		(ID3D11Resource*) ((BB.multiSampleCount > 1) ? BB.resolveBuffer : BB.colorBuffer),
		&shaderViewDesc,
		&BB.shaderView
	);
	ERROR_CHECK_RETURN("Backbuffer shader view creation failed",)

	/* Update the depth/stencil buffer, if applicable */
	if (BB.depthFormat != FNA3D_DEPTHFORMAT_NONE)
	{
		depthStencilDesc.Width = BB.width;
		depthStencilDesc.Height = BB.height;
		depthStencilDesc.MipLevels = 1;
		depthStencilDesc.ArraySize = 1;
		depthStencilDesc.Format = XNAToD3D_DepthFormat[BB.depthFormat];
		depthStencilDesc.SampleDesc.Count = (BB.multiSampleCount > 1 ? BB.multiSampleCount : 1);
		depthStencilDesc.SampleDesc.Quality = 0;
		depthStencilDesc.Usage = D3D11_USAGE_DEFAULT;
		depthStencilDesc.BindFlags = D3D11_BIND_DEPTH_STENCIL;
		depthStencilDesc.CPUAccessFlags = 0;
		depthStencilDesc.MiscFlags = 0;
		res = ID3D11Device_CreateTexture2D(
			renderer->device,
			&depthStencilDesc,
			NULL,
			&BB.depthStencilBuffer
		);
		ERROR_CHECK_RETURN("Backbuffer depth-stencil buffer creation failed",)

		/* Update the depth-stencil view */
		depthStencilViewDesc.Format = depthStencilDesc.Format;
		depthStencilViewDesc.Flags = 0;
		if (BB.multiSampleCount > 1)
		{
			depthStencilViewDesc.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2DMS;
		}
		else
		{
			depthStencilViewDesc.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2D;
			depthStencilViewDesc.Texture2D.MipSlice = 0;

		}
		res = ID3D11Device_CreateDepthStencilView(
			renderer->device,
			(ID3D11Resource*) BB.depthStencilBuffer,
			&depthStencilViewDesc,
			&BB.depthStencilView
		);
		ERROR_CHECK_RETURN("Backbuffer depth-stencil view creation failed",)
	}

	/* Create the swapchain */
	if (renderer->swapchain == NULL)
	{
		D3D11_PLATFORM_CreateSwapChain(renderer, presentationParameters);
	}
	else
	{
		/* Resize the swapchain to the new window size */
		res = IDXGISwapChain_ResizeBuffers(
			renderer->swapchain,
			0,			/* keep # of buffers the same */
			0,			/* get width from window */
			0,			/* get height from window */
			DXGI_FORMAT_UNKNOWN,	/* keep the old format */
			0
		);
		ERROR_CHECK_RETURN("Could not resize swapchain",)
	}

	/* Create a render target view for the swapchain */
	swapchainViewDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	swapchainViewDesc.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2D;
	swapchainViewDesc.Texture2D.MipSlice = 0;

	res = IDXGISwapChain_GetBuffer(
		renderer->swapchain,
		0,
		&D3D_IID_ID3D11Texture2D,
		(void**) &swapchainTexture
	);
	ERROR_CHECK_RETURN("Could not get buffer from swapchain",)

	res = ID3D11Device_CreateRenderTargetView(
		renderer->device,
		(ID3D11Resource*) swapchainTexture,
		&swapchainViewDesc,
		&renderer->swapchainRTView
	);
	ERROR_CHECK_RETURN("Swapchain RT view creation failed",)

	/* Cleanup is required for any GetBuffer call! */
	ID3D11Texture2D_Release(swapchainTexture);
	swapchainTexture = NULL;

	/* This is the default render target */
	D3D11_SetRenderTargets(
		(FNA3D_Renderer*) renderer,
		NULL,
		0,
		NULL,
		FNA3D_DEPTHFORMAT_NONE,
		0
	);

	#undef BB
}

static void D3D11_INTERNAL_DestroyFramebuffer(D3D11Renderer *renderer)
{
	#define BB renderer->backbuffer

	if (BB.colorBuffer != NULL)
	{
		ID3D11RenderTargetView_Release(BB.colorView);
		BB.colorView = NULL;

		ID3D11ShaderResourceView_Release(BB.shaderView);
		BB.shaderView = NULL;

		ID3D11Texture2D_Release(BB.colorBuffer);
		BB.colorBuffer = NULL;
	}

	if (BB.stagingBuffer != NULL)
	{
		ID3D11Texture2D_Release(BB.stagingBuffer);
		BB.stagingBuffer = NULL;
	}

	if (BB.depthStencilBuffer != NULL)
	{
		ID3D11DepthStencilView_Release(BB.depthStencilView);
		BB.depthStencilView = NULL;

		ID3D11Texture2D_Release(BB.depthStencilBuffer);
		BB.depthStencilBuffer = NULL;
	}

	if (BB.resolveBuffer != NULL)
	{
		ID3D11Texture2D_Release(BB.resolveBuffer);
		BB.resolveBuffer = NULL;
	}

	if (renderer->swapchainRTView != NULL)
	{
		ID3D11RenderTargetView_Release(renderer->swapchainRTView);
		renderer->swapchainRTView = NULL;
	}

	#undef BB
}

static void D3D11_INTERNAL_SetPresentationInterval(
	D3D11Renderer *renderer,
	FNA3D_PresentInterval presentInterval
) {
	if (	presentInterval == FNA3D_PRESENTINTERVAL_DEFAULT ||
		presentInterval == FNA3D_PRESENTINTERVAL_ONE	)
	{
		renderer->syncInterval = 1;
	}
	else if (presentInterval == FNA3D_PRESENTINTERVAL_TWO)
	{
		renderer->syncInterval = 2;
	}
	else if (presentInterval == FNA3D_PRESENTINTERVAL_IMMEDIATE)
	{
		renderer->syncInterval = 0;
	}
	else
	{
		FNA3D_LogError(
			"Unrecognized PresentInterval: %d",
			presentInterval
		);
	}
}

static void D3D11_ResetBackbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_PresentationParameters *presentationParameters
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;

	D3D11_INTERNAL_DestroyFramebuffer(renderer);
	D3D11_INTERNAL_CreateFramebuffer(
		renderer,
		presentationParameters
	);
	D3D11_INTERNAL_SetPresentationInterval(
		renderer,
		presentationParameters->presentationInterval
	);
}

static void D3D11_ReadBackbuffer(
	FNA3D_Renderer *driverData,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	void* data,
	int32_t dataLength
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture backbufferTexture;

	if (renderer->backbuffer.multiSampleCount > 1)
	{
		/* We have to resolve the backbuffer first. */
		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_ResolveSubresource(
			renderer->context,
			(ID3D11Resource*) renderer->backbuffer.resolveBuffer,
			0,
			(ID3D11Resource*) renderer->backbuffer.colorBuffer,
			0,
			XNAToD3D_TextureFormat[renderer->backbuffer.surfaceFormat]
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}

	/* Create a pseudo-texture we can feed to GetTextureData2D.
	 * These are the only members we need to initialize.
	 * -caleb
	 */
	backbufferTexture.twod.width = renderer->backbuffer.width;
	backbufferTexture.twod.height = renderer->backbuffer.height;
	backbufferTexture.format = renderer->backbuffer.surfaceFormat;
	backbufferTexture.levelCount = 1;
	backbufferTexture.handle = (
		renderer->backbuffer.multiSampleCount > 1 ?
			(ID3D11Resource*) renderer->backbuffer.resolveBuffer :
			(ID3D11Resource*) renderer->backbuffer.colorBuffer
	);
	backbufferTexture.staging = (ID3D11Resource*) renderer->backbuffer.stagingBuffer;

	D3D11_GetTextureData2D(
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

static void D3D11_GetBackbufferSize(
	FNA3D_Renderer *driverData,
	int32_t *w,
	int32_t *h
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	*w = renderer->backbuffer.width;
	*h = renderer->backbuffer.height;
}

static FNA3D_SurfaceFormat D3D11_GetBackbufferSurfaceFormat(
	FNA3D_Renderer *driverData
) {
	return ((D3D11Renderer*) driverData)->backbuffer.surfaceFormat;
}

static FNA3D_DepthFormat D3D11_GetBackbufferDepthFormat(
	FNA3D_Renderer *driverData
) {
	return ((D3D11Renderer*) driverData)->backbuffer.depthFormat;
}

static int32_t D3D11_GetBackbufferMultiSampleCount(
	FNA3D_Renderer *driverData
) {
	return ((D3D11Renderer*) driverData)->backbuffer.multiSampleCount;
}

/* Textures */

static FNA3D_Texture* D3D11_CreateTexture2D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *result;
	ID3D11Texture2D *texture;
	D3D11_TEXTURE2D_DESC desc;
	D3D11_RENDER_TARGET_VIEW_DESC rtViewDesc;
	HRESULT res;

	/* Initialize descriptor */
	desc.Width = width;
	desc.Height = height;
	desc.MipLevels = levelCount;
	desc.ArraySize = 1;
	desc.Format = XNAToD3D_TextureFormat[format];
	desc.SampleDesc.Count = 1;
	desc.SampleDesc.Quality = 0;
	desc.Usage = D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
	desc.CPUAccessFlags = 0;
	desc.MiscFlags = 0;

	if (isRenderTarget)
	{
		desc.BindFlags |= D3D11_BIND_RENDER_TARGET;
		desc.MiscFlags = D3D11_RESOURCE_MISC_GENERATE_MIPS;
	}

	/* Create the texture */
	res = ID3D11Device_CreateTexture2D(
		renderer->device,
		&desc,
		NULL,
		&texture
	);
	ERROR_CHECK_RETURN("Texture2D creation failed", NULL)

	/* Initialize D3D11Texture */
	result = (D3D11Texture*) SDL_malloc(sizeof(D3D11Texture));
	SDL_memset(result, '\0', sizeof(D3D11Texture));
	result->handle = (ID3D11Resource*) texture;
	result->levelCount = levelCount;
	result->isRenderTarget = isRenderTarget;
	result->format = format;
	result->twod.width = width;
	result->twod.height = height;

	/* Create the shader resource view */
	res = ID3D11Device_CreateShaderResourceView(
		renderer->device,
		result->handle,
		NULL,
		&result->shaderView
	);
	ERROR_CHECK_RETURN("Texture2D shader view creation failed", NULL)

	/* Create the render target view, if applicable */
	if (isRenderTarget)
	{
		result->rtType = FNA3D_RENDERTARGET_TYPE_2D;
		rtViewDesc.Format = desc.Format;
		rtViewDesc.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2D;
		rtViewDesc.Texture2D.MipSlice = 0;
		res = ID3D11Device_CreateRenderTargetView(
			renderer->device,
			result->handle,
			&rtViewDesc,
			&result->twod.rtView
		);
		ERROR_CHECK_RETURN("Texture2D render target creation failed", NULL)
	}

	return (FNA3D_Texture*) result;
}

static FNA3D_Texture* D3D11_CreateTexture3D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *result;
	ID3D11Texture3D *texture;
	D3D11_TEXTURE3D_DESC desc;
	HRESULT res;

	/* Initialize descriptor */
	desc.Width = width;
	desc.Height = height;
	desc.Depth = depth;
	desc.MipLevels = levelCount;
	desc.Format = XNAToD3D_TextureFormat[format];
	desc.Usage = D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
	desc.CPUAccessFlags = 0;
	desc.MiscFlags = 0;

	/* Create the texture */
	res = ID3D11Device_CreateTexture3D(
		renderer->device,
		&desc,
		NULL,
		&texture
	);
	ERROR_CHECK_RETURN("Texture3D creation failed", NULL)

	/* Initialize D3D11Texture */
	result = (D3D11Texture*) SDL_malloc(sizeof(D3D11Texture));
	SDL_memset(result, '\0', sizeof(D3D11Texture));
	result->handle = (ID3D11Resource*) texture;
	result->levelCount = levelCount;
	result->isRenderTarget = 0;
	result->format = format;
	result->threed.width = width;
	result->threed.height = height;
	result->threed.depth = depth;

	/* Create the shader resource view */
	res = ID3D11Device_CreateShaderResourceView(
		renderer->device,
		result->handle,
		NULL,
		&result->shaderView
	);
	ERROR_CHECK_RETURN("Texture3D shader view creation failed", NULL)

	return (FNA3D_Texture*) result;
}

static FNA3D_Texture* D3D11_CreateTextureCube(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *result;
	ID3D11Texture2D *texture;
	D3D11_TEXTURE2D_DESC desc;
	D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc;
	D3D11_RENDER_TARGET_VIEW_DESC rtViewDesc;
	int32_t i;
	HRESULT res;

	/* Initialize descriptor */
	desc.Width = size;
	desc.Height = size;
	desc.MipLevels = levelCount;
	desc.ArraySize = 6;
	desc.Format = XNAToD3D_TextureFormat[format];
	desc.SampleDesc.Count = 1;
	desc.SampleDesc.Quality = 0;
	desc.Usage = D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
	desc.CPUAccessFlags = 0;
	desc.MiscFlags = D3D11_RESOURCE_MISC_TEXTURECUBE;

	if (isRenderTarget)
	{
		desc.BindFlags |= D3D11_BIND_RENDER_TARGET;
		desc.MiscFlags |= D3D11_RESOURCE_MISC_GENERATE_MIPS;
	}

	/* Create the texture */
	res = ID3D11Device_CreateTexture2D(
		renderer->device,
		&desc,
		NULL,
		&texture
	);
	ERROR_CHECK_RETURN("TextureCube creation failed", NULL)

	/* Initialize D3D11Texture */
	result = (D3D11Texture*) SDL_malloc(sizeof(D3D11Texture));
	SDL_memset(result, '\0', sizeof(D3D11Texture));
	result->handle = (ID3D11Resource*) texture;
	result->levelCount = levelCount;
	result->isRenderTarget = isRenderTarget;
	result->format = format;
	result->cube.size = size;

	/* Create the shader resource view */
	srvDesc.Format = desc.Format;
	srvDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURECUBE;
	srvDesc.TextureCube.MipLevels = levelCount;
	srvDesc.TextureCube.MostDetailedMip = 0;
	res = ID3D11Device_CreateShaderResourceView(
		renderer->device,
		result->handle,
		&srvDesc,
		&result->shaderView
	);
	ERROR_CHECK_RETURN("TextureCube shader view creation failed", NULL)

	/* Create the render target view, if applicable */
	if (isRenderTarget)
	{
		result->rtType = FNA3D_RENDERTARGET_TYPE_CUBE;
		result->cube.rtViews = (ID3D11RenderTargetView**) SDL_malloc(
			6 * sizeof(ID3D11RenderTargetView*)
		);
		rtViewDesc.Format = desc.Format;
		rtViewDesc.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2DARRAY;
		rtViewDesc.Texture2DArray.ArraySize = 1; /* One slice per view */
		rtViewDesc.Texture2DArray.MipSlice = 0;
		for (i = 0; i < 6; i += 1)
		{
			rtViewDesc.Texture2DArray.FirstArraySlice = i;
			res = ID3D11Device_CreateRenderTargetView(
				renderer->device,
				result->handle,
				&rtViewDesc,
				&result->cube.rtViews[i]
			);
			ERROR_CHECK_RETURN("TextureCube render target view creation failed", NULL)
		}
	}

	return (FNA3D_Texture*) result;
}

static void D3D11_AddDisposeTexture(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *tex = (D3D11Texture*) texture;
	int32_t i, j;

	/* Unbind the texture */
	for (i = 0; i < MAX_TOTAL_SAMPLERS; i += 1)
	{
		if (renderer->textures[i] == tex)
		{
			renderer->textures[i] = &NullTexture;
		}
	}

	/* Unbind and release the render target views, if applicable */
	if (tex->isRenderTarget)
	{
		for (i = 0; i < renderer->numRenderTargets; i += 1)
		{
			if (tex->rtType == FNA3D_RENDERTARGET_TYPE_2D)
			{
				if (tex->twod.rtView == renderer->renderTargetViews[i])
				{
					renderer->renderTargetViews[i] = NULL;
				}
			}
			else if (tex->rtType == FNA3D_RENDERTARGET_TYPE_CUBE)
			{
				for (j = 0; j < 6; j += 1)
				{
					if (tex->cube.rtViews[j] == renderer->renderTargetViews[i])
					{
						renderer->renderTargetViews[i] = NULL;
					}
				}
			}
		}

		if (tex->rtType == FNA3D_RENDERTARGET_TYPE_2D)
		{
			ID3D11RenderTargetView_Release(tex->twod.rtView);
		}
		else if (tex->rtType == FNA3D_RENDERTARGET_TYPE_CUBE)
		{
			for (i = 0; i < 6; i += 1)
			{
				ID3D11RenderTargetView_Release(tex->cube.rtViews[i]);
			}
			SDL_free(tex->cube.rtViews);
		}
	}

	/* Release the shader resource view and texture */
	ID3D11ShaderResourceView_Release(tex->shaderView);
	IUnknown_Release(tex->handle);

	SDL_free(texture);
}

static void D3D11_SetTextureData2D(
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
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *d3dTexture = (D3D11Texture*) texture;
	D3D11_BOX dstBox;

	/* DXT formats require w and h to be multiples of 4 */
	if (	d3dTexture->format == FNA3D_SURFACEFORMAT_DXT1 ||
		d3dTexture->format == FNA3D_SURFACEFORMAT_DXT3 ||
		d3dTexture->format == FNA3D_SURFACEFORMAT_DXT5	)
	{
		w = (w + 3) & ~3;
		h = (h + 3) & ~3;
	}

	dstBox.left = x;
	dstBox.top = y;
	dstBox.front = 0;
	dstBox.right = x + w;
	dstBox.bottom = y + h;
	dstBox.back = 1;

	SDL_LockMutex(renderer->ctxLock);
	ID3D11DeviceContext_UpdateSubresource(
		renderer->context,
		d3dTexture->handle,
		D3D11_INTERNAL_CalcSubresource(level, 0, d3dTexture->levelCount),
		&dstBox,
		data,
		BytesPerRow(w, d3dTexture->format),
		0
	);
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_SetTextureData3D(
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
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *d3dTexture = (D3D11Texture*) texture;
	D3D11_BOX dstBox;

	/* DXT formats require w and h to be multiples of 4 */
	if (	d3dTexture->format == FNA3D_SURFACEFORMAT_DXT1 ||
		d3dTexture->format == FNA3D_SURFACEFORMAT_DXT3 ||
		d3dTexture->format == FNA3D_SURFACEFORMAT_DXT5	)
	{
		w = (w + 3) & ~3;
		h = (h + 3) & ~3;
	}

	dstBox.left = x;
	dstBox.top = y;
	dstBox.front = z;
	dstBox.right = x + w;
	dstBox.bottom = y + h;
	dstBox.back = z + d;

	SDL_LockMutex(renderer->ctxLock);
	ID3D11DeviceContext_UpdateSubresource(
		renderer->context,
		d3dTexture->handle,
		D3D11_INTERNAL_CalcSubresource(level, 0, d3dTexture->levelCount),
		&dstBox,
		data,
		BytesPerRow(w, d3dTexture->format),
		BytesPerImage(w, h, d3dTexture->format)
	);
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_SetTextureDataCube(
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
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *d3dTexture = (D3D11Texture*) texture;
	D3D11_BOX dstBox;

	/* DXT formats require w and h to be multiples of 4 */
	if (	d3dTexture->format == FNA3D_SURFACEFORMAT_DXT1 ||
		d3dTexture->format == FNA3D_SURFACEFORMAT_DXT3 ||
		d3dTexture->format == FNA3D_SURFACEFORMAT_DXT5	)
	{
		w = (w + 3) & ~3;
		h = (h + 3) & ~3;
	}

	dstBox.left = x;
	dstBox.top = y;
	dstBox.front = 0;
	dstBox.right = x + w;
	dstBox.bottom = y + h;
	dstBox.back = 1;

	SDL_LockMutex(renderer->ctxLock);
	ID3D11DeviceContext_UpdateSubresource(
		renderer->context,
		d3dTexture->handle,
		D3D11_INTERNAL_CalcSubresource(
			level,
			cubeMapFace,
			d3dTexture->levelCount
		),
		&dstBox,
		data,
		BytesPerRow(w, d3dTexture->format),
		BytesPerImage(w, h, d3dTexture->format)
	);
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_SetTextureDataYUV(
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
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *d3dY = (D3D11Texture*) y;
	D3D11Texture *d3dU = (D3D11Texture*) u;
	D3D11Texture *d3dV = (D3D11Texture*) v;
	D3D11_BOX yBox = {0, 0, 0, yWidth, yHeight, 1};
	D3D11_BOX uvBox = {0, 0, 0, uvWidth, uvHeight, 1};
	int32_t yRow, uvRow;
	uint8_t *dataPtr = (uint8_t*) data;

	yRow = BytesPerRow(yWidth, FNA3D_SURFACEFORMAT_ALPHA8);
	uvRow = BytesPerRow(uvWidth, FNA3D_SURFACEFORMAT_ALPHA8);
	SDL_LockMutex(renderer->ctxLock);
	ID3D11DeviceContext_UpdateSubresource(
		renderer->context,
		d3dY->handle,
		0,
		&yBox,
		dataPtr,
		yRow,
		0
	);
	dataPtr += yWidth * yHeight;
	ID3D11DeviceContext_UpdateSubresource(
		renderer->context,
		d3dU->handle,
		0,
		&uvBox,
		dataPtr,
		uvRow,
		0
	);
	dataPtr += uvWidth * uvHeight;
	ID3D11DeviceContext_UpdateSubresource(
		renderer->context,
		d3dV->handle,
		0,
		&uvBox,
		dataPtr,
		uvRow,
		0
	);
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_GetTextureData2D(
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
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *tex = (D3D11Texture*) texture;
	D3D11_TEXTURE2D_DESC stagingDesc;
	ID3D11Resource *stagingTexture;
	uint32_t subresourceIndex = D3D11_INTERNAL_CalcSubresource(
		level,
		0,
		tex->levelCount
	);
	int32_t texW = tex->twod.width >> level;
	int32_t texH = tex->twod.height >> level;
	D3D11_BOX srcBox = {0, 0, 0, texW, texH, 1};
	D3D11_MAPPED_SUBRESOURCE subresource;
	uint8_t *dataPtr = (uint8_t*) data;
	int32_t row;
	int32_t formatSize = Texture_GetFormatSize(tex->format);
	HRESULT res;

	if (	tex->format == FNA3D_SURFACEFORMAT_DXT1 ||
		tex->format == FNA3D_SURFACEFORMAT_DXT3 ||
		tex->format == FNA3D_SURFACEFORMAT_DXT5	)
	{
		FNA3D_LogError(
			"GetData with compressed textures unsupported!"
		);
		return;
	}

	/* Create staging texture if needed */
	if (tex->isRenderTarget)
	{
		stagingTexture = tex->staging;
	}
	else
	{
		stagingTexture = NULL;
	}
	if (stagingTexture == NULL)
	{
		stagingDesc.Width = tex->twod.width;
		stagingDesc.Height = tex->twod.height;
		stagingDesc.MipLevels = tex->levelCount;
		stagingDesc.ArraySize = 1;
		stagingDesc.Format = XNAToD3D_TextureFormat[tex->format];
		stagingDesc.SampleDesc.Count = 1;
		stagingDesc.SampleDesc.Quality = 0;
		stagingDesc.Usage = D3D11_USAGE_STAGING;
		stagingDesc.BindFlags = 0;
		stagingDesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
		stagingDesc.MiscFlags = 0;

		res = ID3D11Device_CreateTexture2D(
			renderer->device,
			&stagingDesc,
			NULL,
			(ID3D11Texture2D**) &stagingTexture
		);
		ERROR_CHECK_RETURN("Texture2D staging buffer creation failed",)

		/* Targets will probably call this a lot, so try to keep this all the time */
		if (tex->isRenderTarget)
		{
			tex->staging = stagingTexture;
		}
	}

	SDL_LockMutex(renderer->ctxLock);

	/* Copy data into staging texture */
	ID3D11DeviceContext_CopySubresourceRegion(
		renderer->context,
		stagingTexture,
		subresourceIndex,
		0,
		0,
		0,
		tex->handle,
		subresourceIndex,
		&srcBox
	);

	/* Read from the staging texture */
	res = ID3D11DeviceContext_Map(
		renderer->context,
		stagingTexture,
		subresourceIndex,
		D3D11_MAP_READ,
		0,
		&subresource
	);
	ERROR_CHECK_UNLOCK_RETURN("Could not map Texture2D for reading",)
	for (row = y; row < y + h; row += 1)
	{
		SDL_memcpy(
			dataPtr,
			(uint8_t*) subresource.pData + (row * subresource.RowPitch) + (x * formatSize),
			formatSize * w
		);
		dataPtr += formatSize * w;
	}
	ID3D11DeviceContext_Unmap(
		renderer->context,
		stagingTexture,
		subresourceIndex
	);

	SDL_UnlockMutex(renderer->ctxLock);

	if (!tex->isRenderTarget)
	{
		ID3D11Resource_Release(stagingTexture);
	}
}

static void D3D11_GetTextureData3D(
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

static void D3D11_GetTextureDataCube(
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
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Texture *tex = (D3D11Texture*) texture;
	D3D11_TEXTURE2D_DESC stagingDesc;
	ID3D11Resource *stagingTexture;
	uint32_t srcSubresourceIndex = D3D11_INTERNAL_CalcSubresource(
		level,
		cubeMapFace,
		tex->levelCount
	);
	uint32_t dstSubresourceIndex = D3D11_INTERNAL_CalcSubresource(
		level,
		0,
		tex->levelCount
	);
	int32_t texSize = tex->cube.size >> level;
	D3D11_BOX srcBox = {0, 0, 0, texSize, texSize, 1};
	D3D11_MAPPED_SUBRESOURCE subresource;
	uint8_t *dataPtr = (uint8_t*) data;
	int32_t row;
	int32_t formatSize = Texture_GetFormatSize(tex->format);
	HRESULT res;

	if (	tex->format == FNA3D_SURFACEFORMAT_DXT1 ||
		tex->format == FNA3D_SURFACEFORMAT_DXT3 ||
		tex->format == FNA3D_SURFACEFORMAT_DXT5	)
	{
		FNA3D_LogError(
			"GetData with compressed textures unsupported!"
		);
		return;
	}

	/* Create staging texture if needed */
	if (tex->isRenderTarget)
	{
		stagingTexture = tex->staging;
	}
	else
	{
		stagingTexture = NULL;
	}
	if (stagingTexture == NULL)
	{
		stagingDesc.Width = tex->cube.size;
		stagingDesc.Height = tex->cube.size;
		stagingDesc.MipLevels = tex->levelCount;
		stagingDesc.ArraySize = 1;
		stagingDesc.Format = XNAToD3D_TextureFormat[tex->format];
		stagingDesc.SampleDesc.Count = 1;
		stagingDesc.SampleDesc.Quality = 0;
		stagingDesc.Usage = D3D11_USAGE_STAGING;
		stagingDesc.BindFlags = 0;
		stagingDesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
		stagingDesc.MiscFlags = 0;

		res = ID3D11Device_CreateTexture2D(
			renderer->device,
			&stagingDesc,
			NULL,
			(ID3D11Texture2D**) &stagingTexture
		);
		ERROR_CHECK_RETURN("TextureCube staging buffer creation failed",)

		/* Targets will probably call this a lot, so try to keep this all the time */
		if (tex->isRenderTarget)
		{
			tex->staging = stagingTexture;
		}
	}

	SDL_LockMutex(renderer->ctxLock);

	/* Copy data into staging texture */
	ID3D11DeviceContext_CopySubresourceRegion(
		renderer->context,
		stagingTexture,
		dstSubresourceIndex,
		0,
		0,
		0,
		tex->handle,
		srcSubresourceIndex,
		&srcBox
	);

	/* Read from the staging texture */
	res = ID3D11DeviceContext_Map(
		renderer->context,
		stagingTexture,
		dstSubresourceIndex,
		D3D11_MAP_READ,
		0,
		&subresource
	);
	ERROR_CHECK_UNLOCK_RETURN("Could not map TextureCube for reading",)
	for (row = y; row < y + h; row += 1)
	{
		SDL_memcpy(
			dataPtr,
			(uint8_t*) subresource.pData + (row * subresource.RowPitch) + (x * formatSize),
			formatSize * w
		);
		dataPtr += formatSize * w;
	}
	ID3D11DeviceContext_Unmap(
		renderer->context,
		stagingTexture,
		dstSubresourceIndex
	);

	SDL_UnlockMutex(renderer->ctxLock);

	if (!tex->isRenderTarget)
	{
		ID3D11Resource_Release(stagingTexture);
	}
}

/* Renderbuffers */

static FNA3D_Renderbuffer* D3D11_GenColorRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11_TEXTURE2D_DESC desc;
	D3D11Renderbuffer *result;
	HRESULT res;

	/* Initialize the renderbuffer */
	result = (D3D11Renderbuffer*) SDL_malloc(sizeof(D3D11Renderbuffer));
	SDL_memset(result, '\0', sizeof(D3D11Renderbuffer));
	result->multiSampleCount = multiSampleCount;
	result->type = RENDERBUFFER_COLOR;
	result->color.format = format;

	/* Create the backing texture */
	desc.Width = width;
	desc.Height = height;
	desc.MipLevels = 1;
	desc.ArraySize = 1;
	desc.Format = XNAToD3D_TextureFormat[format];
	desc.SampleDesc.Count = multiSampleCount;
	desc.SampleDesc.Quality = D3D11_STANDARD_MULTISAMPLE_PATTERN;
	desc.Usage = D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_RENDER_TARGET;
	desc.CPUAccessFlags = 0;
	desc.MiscFlags = 0;

	res = ID3D11Device_CreateTexture2D(
		renderer->device,
		&desc,
		NULL,
		&result->handle
	);
	ERROR_CHECK_RETURN("Color renderbuffer creation failed", NULL)

	/* Create the render target view */
	res = ID3D11Device_CreateRenderTargetView(
		renderer->device,
		(ID3D11Resource*) result->handle,
		NULL,
		&result->color.rtView
	);
	ERROR_CHECK_RETURN("Color renderbuffer RT view creation failed", NULL)

	return (FNA3D_Renderbuffer*) result;
}

static FNA3D_Renderbuffer* D3D11_GenDepthStencilRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11_TEXTURE2D_DESC desc;
	D3D11Renderbuffer *result;
	HRESULT res;

	/* Initialize the renderbuffer */
	result = (D3D11Renderbuffer*) SDL_malloc(sizeof(D3D11Renderbuffer));
	SDL_memset(result, '\0', sizeof(D3D11Renderbuffer));
	result->multiSampleCount = multiSampleCount;
	result->type = RENDERBUFFER_DEPTH;
	result->depth.format = format;

	/* Create the backing texture */
	desc.Width = width;
	desc.Height = height;
	desc.MipLevels = 1;
	desc.ArraySize = 1;
	desc.Format = XNAToD3D_DepthFormat[format];
	desc.SampleDesc.Count = (multiSampleCount > 1 ? multiSampleCount : 1);
	desc.SampleDesc.Quality = (
		multiSampleCount > 1 ? D3D11_STANDARD_MULTISAMPLE_PATTERN : 0
	);
	desc.Usage = D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_DEPTH_STENCIL;
	desc.CPUAccessFlags = 0;
	desc.MiscFlags = 0;

	res = ID3D11Device_CreateTexture2D(
		renderer->device,
		&desc,
		NULL,
		&result->handle
	);
	ERROR_CHECK_RETURN("Depth-stencil renderbuffer creation failed", NULL)

	/* Create the render target view */
	res = ID3D11Device_CreateDepthStencilView(
		renderer->device,
		(ID3D11Resource*) result->handle,
		NULL,
		&result->depth.dsView
	);
	ERROR_CHECK_RETURN("Depth-stencil renderbuffer RT view creation failed", NULL)

	return (FNA3D_Renderbuffer*) result;
}

static void D3D11_AddDisposeRenderbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Renderbuffer *renderbuffer
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Renderbuffer *d3dRenderbuffer = (D3D11Renderbuffer*) renderbuffer;
	int32_t i;

	if (d3dRenderbuffer->type == RENDERBUFFER_DEPTH)
	{
		if (d3dRenderbuffer->depth.dsView == renderer->depthStencilView)
		{
			renderer->depthStencilView = NULL;
		}
		ID3D11DepthStencilView_Release(d3dRenderbuffer->depth.dsView);
		d3dRenderbuffer->depth.dsView = NULL;
	}
	else
	{
		for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
		{
			if (d3dRenderbuffer->color.rtView == renderer->renderTargetViews[i])
			{
				renderer->renderTargetViews[i] = NULL;
			}
		}
		ID3D11RenderTargetView_Release(d3dRenderbuffer->color.rtView);
	}

	ID3D11Texture2D_Release(d3dRenderbuffer->handle);
	d3dRenderbuffer->handle = NULL;
	SDL_free(renderbuffer);
}

/* Vertex Buffers */

static FNA3D_Buffer* D3D11_GenVertexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *result = (D3D11Buffer*) SDL_malloc(sizeof(D3D11Buffer));
	D3D11_BUFFER_DESC desc;
	HRESULT res;

	/* Initialize the descriptor */
	desc.ByteWidth = sizeInBytes;
	desc.Usage = dynamic ? D3D11_USAGE_DYNAMIC : D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
	desc.CPUAccessFlags = dynamic ? D3D11_CPU_ACCESS_WRITE : 0;
	desc.MiscFlags = 0;
	desc.StructureByteStride = 0;

	/* Make the buffer */
	res = ID3D11Device_CreateBuffer(
		renderer->device,
		&desc,
		NULL,
		&result->handle
	);
	ERROR_CHECK_RETURN("Vertex buffer creation failed", NULL)

	/* Return the result */
	result->dynamic = dynamic;
	result->size = desc.ByteWidth;
	return (FNA3D_Buffer*) result;
}

static void D3D11_AddDisposeVertexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *d3dBuffer = (D3D11Buffer*) buffer;
	ID3D11Buffer *nullVertexBuffers[] = {NULL};
	uint32_t whatever[1] = {0};
	int32_t i;

	for (i = 0; i < MAX_BOUND_VERTEX_BUFFERS; i += 1)
	{
		if (renderer->vertexBuffers[i] == d3dBuffer->handle)
		{
			renderer->vertexBuffers[i] = NULL;
			SDL_LockMutex(renderer->ctxLock);
			ID3D11DeviceContext_IASetVertexBuffers(
				renderer->context,
				i,
				1,
				nullVertexBuffers,
				whatever,
				whatever
			);
			SDL_UnlockMutex(renderer->ctxLock);
		}
	}

	ID3D11Buffer_Release(d3dBuffer->handle);
	SDL_free(buffer);
}

static void D3D11_SetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *d3dBuffer = (D3D11Buffer*) buffer;
	D3D11_MAPPED_SUBRESOURCE subres = {0, 0, 0};
	int32_t dataLen = vertexStride * elementCount;
	D3D11_BOX dstBox = {offsetInBytes, 0, 0, offsetInBytes + dataLen, 1, 1};
	HRESULT res;

	SDL_LockMutex(renderer->ctxLock);
	if (d3dBuffer->dynamic)
	{
		if (	renderer->debugMode &&
			options == FNA3D_SETDATAOPTIONS_NONE &&
			dataLen < d3dBuffer->size		)
		{
			FNA3D_LogWarn(
				"Dynamic buffer using SetDataOptions.None, expect bad performance and broken output!"
			);
		}

		res = ID3D11DeviceContext_Map(
			renderer->context,
			(ID3D11Resource*) d3dBuffer->handle,
			0,
			options == FNA3D_SETDATAOPTIONS_NOOVERWRITE ?
				D3D11_MAP_WRITE_NO_OVERWRITE :
				D3D11_MAP_WRITE_DISCARD,
			0,
			&subres
		);
		ERROR_CHECK_UNLOCK_RETURN("Could not map vertex buffer for writing",)
		SDL_memcpy(
			(uint8_t*) subres.pData + offsetInBytes,
			data,
			dataLen
		);
		ID3D11DeviceContext_Unmap(
			renderer->context,
			(ID3D11Resource*) d3dBuffer->handle,
			0
		);
	}
	else
	{
		ID3D11DeviceContext_UpdateSubresource(
			renderer->context,
			(ID3D11Resource*) d3dBuffer->handle,
			0,
			&dstBox,
			data,
			dataLen,
			dataLen
		);
	}
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_GetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *d3dBuffer = (D3D11Buffer*) buffer;
	D3D11_BUFFER_DESC desc;
	ID3D11Resource *stagingBuffer;
	int32_t dataLength = vertexStride * elementCount;
	uint8_t *src, *dst;
	int32_t i;
	D3D11_MAPPED_SUBRESOURCE subres;
	D3D11_BOX srcBox = {offsetInBytes, 0, 0, offsetInBytes + dataLength, 1, 1};
	HRESULT res;

	/* Create staging buffer */
	desc.ByteWidth = d3dBuffer->size;
	desc.Usage = D3D11_USAGE_STAGING;
	desc.BindFlags = 0;
	desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
	desc.MiscFlags = 0;
	desc.StructureByteStride = 0;
	res = ID3D11Device_CreateBuffer(
		renderer->device,
		&desc,
		NULL,
		(ID3D11Buffer**) &stagingBuffer
	);
	ERROR_CHECK_RETURN("Could not create vertex buffer staging buffer",)

	SDL_LockMutex(renderer->ctxLock);

	/* Copy data into staging buffer */
	ID3D11DeviceContext_CopySubresourceRegion(
		renderer->context,
		stagingBuffer,
		0,
		0,
		0,
		0,
		(ID3D11Resource*) d3dBuffer->handle,
		0,
		&srcBox
	);

	/* Read from the staging buffer */
	res = ID3D11DeviceContext_Map(
		renderer->context,
		stagingBuffer,
		0,
		D3D11_MAP_READ,
		0,
		&subres
	);
	ERROR_CHECK_UNLOCK_RETURN("Could not map vertex buffer for reading",)
	if (elementSizeInBytes < vertexStride)
	{
		dst = (uint8_t*) data;
		src = (uint8_t*) subres.pData;
		for (i = 0; i < elementCount; i += 1)
		{
			SDL_memcpy(dst, src, elementSizeInBytes);
			dst += elementSizeInBytes;
			src += vertexStride;
		}
	}
	else
	{
		SDL_memcpy(
			data,
			subres.pData,
			dataLength
		);
	}
	ID3D11DeviceContext_Unmap(
		renderer->context,
		stagingBuffer,
		0
	);

	SDL_UnlockMutex(renderer->ctxLock);

	ID3D11Resource_Release(stagingBuffer);
}

/* Index Buffers */

static FNA3D_Buffer* D3D11_GenIndexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *result = (D3D11Buffer*) SDL_malloc(sizeof(D3D11Buffer));
	D3D11_BUFFER_DESC desc;
	HRESULT res;

	/* Initialize the descriptor */
	desc.ByteWidth = sizeInBytes;
	desc.Usage = dynamic ? D3D11_USAGE_DYNAMIC : D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_INDEX_BUFFER;
	desc.CPUAccessFlags = dynamic ? D3D11_CPU_ACCESS_WRITE : 0;
	desc.MiscFlags = 0;
	desc.StructureByteStride = 0;

	/* Make the buffer */
	res = ID3D11Device_CreateBuffer(
		renderer->device,
		&desc,
		NULL,
		&result->handle
	);
	ERROR_CHECK_RETURN("Index buffer creation failed", NULL)

	/* Return the result */
	result->dynamic = dynamic;
	result->size = desc.ByteWidth;
	return (FNA3D_Buffer*) result;
}

static void D3D11_AddDisposeIndexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *d3dBuffer = (D3D11Buffer*) buffer;

	if (d3dBuffer->handle == renderer->indexBuffer)
	{
		renderer->indexBuffer = NULL;
		SDL_LockMutex(renderer->ctxLock);
		ID3D11DeviceContext_IASetIndexBuffer(
			renderer->context,
			NULL,
			DXGI_FORMAT_R16_UINT,
			0
		);
		SDL_UnlockMutex(renderer->ctxLock);
	}

	ID3D11Buffer_Release(d3dBuffer->handle);
	SDL_free(buffer);
}

static void D3D11_SetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *d3dBuffer = (D3D11Buffer*) buffer;
	D3D11_MAPPED_SUBRESOURCE subres = {0, 0, 0};
	D3D11_BOX dstBox = {offsetInBytes, 0, 0, offsetInBytes + dataLength, 1, 1};
	HRESULT res;

	SDL_LockMutex(renderer->ctxLock);
	if (d3dBuffer->dynamic)
	{
		if (	renderer->debugMode &&
			options == FNA3D_SETDATAOPTIONS_NONE &&
			dataLength < d3dBuffer->size		)
		{
			FNA3D_LogWarn(
				"Dynamic buffer using SetDataOptions.None, expect bad performance and broken output!"
			);
		}

		res = ID3D11DeviceContext_Map(
			renderer->context,
			(ID3D11Resource*) d3dBuffer->handle,
			0,
			options == FNA3D_SETDATAOPTIONS_NOOVERWRITE ?
				D3D11_MAP_WRITE_NO_OVERWRITE :
				D3D11_MAP_WRITE_DISCARD,
			0,
			&subres
		);
		ERROR_CHECK_UNLOCK_RETURN("Could not map index buffer for writing",)
		SDL_memcpy(
			(uint8_t*) subres.pData + offsetInBytes,
			data,
			dataLength
		);
		ID3D11DeviceContext_Unmap(
			renderer->context,
			(ID3D11Resource*) d3dBuffer->handle,
			0
		);
	}
	else
	{
		ID3D11DeviceContext_UpdateSubresource(
			renderer->context,
			(ID3D11Resource*) d3dBuffer->handle,
			0,
			&dstBox,
			data,
			dataLength,
			dataLength
		);
	}
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_GetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Buffer *d3dBuffer = (D3D11Buffer*) buffer;
	D3D11_BUFFER_DESC desc;
	ID3D11Resource *stagingBuffer;
	D3D11_MAPPED_SUBRESOURCE subres;
	D3D11_BOX srcBox = {offsetInBytes, 0, 0, offsetInBytes + dataLength, 1, 1};
	HRESULT res;

	/* Create staging buffer */
	desc.ByteWidth = d3dBuffer->size;
	desc.Usage = D3D11_USAGE_STAGING;
	desc.BindFlags = 0;
	desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
	desc.MiscFlags = 0;
	desc.StructureByteStride = 0;
	res = ID3D11Device_CreateBuffer(
		renderer->device,
		&desc,
		NULL,
		(ID3D11Buffer**) &stagingBuffer
	);
	ERROR_CHECK_RETURN("Index buffer staging buffer creation failed",)

	SDL_LockMutex(renderer->ctxLock);

	/* Copy data into staging buffer */
	ID3D11DeviceContext_CopySubresourceRegion(
		renderer->context,
		stagingBuffer,
		0,
		0,
		0,
		0,
		(ID3D11Resource*) d3dBuffer->handle,
		0,
		&srcBox
	);

	/* Read from the staging buffer */
	res = ID3D11DeviceContext_Map(
		renderer->context,
		stagingBuffer,
		0,
		D3D11_MAP_READ,
		0,
		&subres
	);
	ERROR_CHECK_UNLOCK_RETURN("Could not map index buffer for reading",)
	SDL_memcpy(
		data,
		subres.pData,
		dataLength
	);
	ID3D11DeviceContext_Unmap(
		renderer->context,
		stagingBuffer,
		0
	);

	SDL_UnlockMutex(renderer->ctxLock);

	ID3D11Resource_Release(stagingBuffer);
}

/* Effects */

static void D3D11_INTERNAL_DeleteShader(void* shader)
{
	MOJOSHADER_d3d11Shader *d3dShader = (MOJOSHADER_d3d11Shader*) shader;
	const MOJOSHADER_parseData *pd;
	D3D11Renderer *renderer;
	PackedVertexBufferBindingsArray *arr;
	int32_t i;

	pd = MOJOSHADER_d3d11GetShaderParseData(d3dShader);
	renderer = (D3D11Renderer*) pd->malloc_data;
	arr = &renderer->inputLayoutCache;

	/* Run through input layout cache in reverse order, to minimize the
	 * damage of doing memmove a bunch of times
	 */
	for (i = arr->count - 1; i >= 0; i -= 1)
	{
		const PackedVertexBufferBindingsMap *elem = &arr->elements[i];
		if (elem->key.vertexShader == shader)
		{
			ID3D11InputLayout_Release(
				(ID3D11InputLayout*) elem->value
			);
			SDL_memmove(
				arr->elements + i,
				arr->elements + i + 1,
				sizeof(PackedVertexBufferBindingsMap) * (arr->count - i - 1)
			);
			arr->count -= 1;
		}
	}

	MOJOSHADER_d3d11DeleteShader(d3dShader);
}

static void D3D11_CreateEffect(
	FNA3D_Renderer *driverData,
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	int32_t i;
	MOJOSHADER_effectShaderContext shaderBackend;
	D3D11Effect *result;

	shaderBackend.compileShader = (MOJOSHADER_compileShaderFunc) MOJOSHADER_d3d11CompileShader;
	shaderBackend.shaderAddRef = (MOJOSHADER_shaderAddRefFunc) MOJOSHADER_d3d11ShaderAddRef;
	shaderBackend.deleteShader = D3D11_INTERNAL_DeleteShader;
	shaderBackend.getParseData = (MOJOSHADER_getParseDataFunc) MOJOSHADER_d3d11GetShaderParseData;
	shaderBackend.bindShaders = (MOJOSHADER_bindShadersFunc) MOJOSHADER_d3d11BindShaders;
	shaderBackend.getBoundShaders = (MOJOSHADER_getBoundShadersFunc) MOJOSHADER_d3d11GetBoundShaders;
	shaderBackend.mapUniformBufferMemory = MOJOSHADER_d3d11MapUniformBufferMemory;
	shaderBackend.unmapUniformBufferMemory = MOJOSHADER_d3d11UnmapUniformBufferMemory;
	shaderBackend.getError = MOJOSHADER_d3d11GetError;
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

	result = (D3D11Effect*) SDL_malloc(sizeof(D3D11Effect));
	result->effect = *effectData;
	*effect = (FNA3D_Effect*) result;
}

static void D3D11_CloneEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *cloneSource,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	D3D11Effect *d3dCloneSource = (D3D11Effect*) cloneSource;
	D3D11Effect *result;

	*effectData = MOJOSHADER_cloneEffect(d3dCloneSource->effect);
	if (*effectData == NULL)
	{
		FNA3D_LogError(
			"%s", MOJOSHADER_d3d11GetError()
		);
	}

	result = (D3D11Effect*) SDL_malloc(sizeof(D3D11Effect));
	result->effect = *effectData;
	*effect = (FNA3D_Effect*) result;
}

static void D3D11_AddDisposeEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	MOJOSHADER_effect *effectData = ((D3D11Effect*) effect)->effect;

	SDL_LockMutex(renderer->ctxLock);
	if (effectData == renderer->currentEffect)
	{
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectEnd(renderer->currentEffect);
		renderer->currentEffect = NULL;
		renderer->currentTechnique = NULL;
		renderer->currentPass = 0;
		renderer->effectApplied = 1;
	}
	MOJOSHADER_deleteEffect(effectData);
	SDL_UnlockMutex(renderer->ctxLock);
	SDL_free(effect);
}

static void D3D11_SetEffectTechnique(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
) {
	/* FIXME: Why doesn't this function do anything? */
	D3D11Effect *d3dEffect = (D3D11Effect*) effect;
	MOJOSHADER_effectSetTechnique(d3dEffect->effect, technique);
}

static void D3D11_ApplyEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	uint32_t pass,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	MOJOSHADER_effect *effectData = ((D3D11Effect*) effect)->effect;
	const MOJOSHADER_effectTechnique *technique = effectData->current_technique;
	uint32_t whatever;

	SDL_LockMutex(renderer->ctxLock);
	renderer->effectApplied = 1;
	if (effectData == renderer->currentEffect)
	{
		if (	technique == renderer->currentTechnique &&
			pass == renderer->currentPass		)
		{
			MOJOSHADER_effectCommitChanges(
				renderer->currentEffect
			);
			SDL_UnlockMutex(renderer->ctxLock);
			return;
		}
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectBeginPass(renderer->currentEffect, pass);
		renderer->currentTechnique = technique;
		renderer->currentPass = pass;
		SDL_UnlockMutex(renderer->ctxLock);
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
	SDL_UnlockMutex(renderer->ctxLock);
	renderer->currentEffect = effectData;
	renderer->currentTechnique = technique;
	renderer->currentPass = pass;
}

static void D3D11_BeginPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	MOJOSHADER_effect *effectData = ((D3D11Effect*) effect)->effect;
	uint32_t whatever;
	SDL_LockMutex(renderer->ctxLock);
	MOJOSHADER_effectBegin(
		effectData,
		&whatever,
		1,
		stateChanges
	);
	MOJOSHADER_effectBeginPass(effectData, 0);
	renderer->effectApplied = 1;
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_EndPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	MOJOSHADER_effect *effectData = ((D3D11Effect*) effect)->effect;
	SDL_LockMutex(renderer->ctxLock);
	MOJOSHADER_effectEndPass(effectData);
	MOJOSHADER_effectEnd(effectData);
	renderer->effectApplied = 1;
	SDL_UnlockMutex(renderer->ctxLock);
}

/* Queries */

static FNA3D_Query* D3D11_CreateQuery(FNA3D_Renderer *driverData)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Query *query = (D3D11Query*) SDL_malloc(sizeof(D3D11Query));
	D3D11_QUERY_DESC desc;
	HRESULT res;

	desc.Query = D3D11_QUERY_OCCLUSION;
	desc.MiscFlags = 0;

	res = ID3D11Device_CreateQuery(
		renderer->device,
		&desc,
		&query->handle
	);
	ERROR_CHECK_RETURN("Query creation failed", NULL)

	return (FNA3D_Query*) query;
}

static void D3D11_AddDisposeQuery(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	D3D11Query *d3dQuery = (D3D11Query*) query;
	ID3D11Query_Release(d3dQuery->handle);
	SDL_free(query);
}

static void D3D11_QueryBegin(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Query *d3dQuery = (D3D11Query*) query;
	SDL_LockMutex(renderer->ctxLock);
	ID3D11DeviceContext_Begin(
		renderer->context,
		(ID3D11Asynchronous*) d3dQuery->handle
	);
	SDL_UnlockMutex(renderer->ctxLock);
}

static void D3D11_QueryEnd(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Query *d3dQuery = (D3D11Query*) query;
	SDL_LockMutex(renderer->ctxLock);
	ID3D11DeviceContext_End(
		renderer->context,
		(ID3D11Asynchronous*) d3dQuery->handle
	);
	SDL_UnlockMutex(renderer->ctxLock);
}

static uint8_t D3D11_QueryComplete(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Query *d3dQuery = (D3D11Query*) query;
	uint8_t result;
	SDL_LockMutex(renderer->ctxLock);
	result = ID3D11DeviceContext_GetData(
		renderer->context,
		(ID3D11Asynchronous*) d3dQuery->handle,
		NULL,
		0,
		0
	) == S_OK;
	SDL_UnlockMutex(renderer->ctxLock);
	return result;
}

static int32_t D3D11_QueryPixelCount(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	D3D11Query *d3dQuery = (D3D11Query*) query;
	uint64_t result;
	HRESULT res;

	SDL_LockMutex(renderer->ctxLock);
	res = ID3D11DeviceContext_GetData(
		renderer->context,
		(ID3D11Asynchronous*) d3dQuery->handle,
		&result,
		sizeof(result),
		0
	);
	ERROR_CHECK("QueryPixelCount failed")
	SDL_UnlockMutex(renderer->ctxLock);

	return (int32_t) result;
}

/* Feature Queries */

static uint8_t D3D11_SupportsDXT1(FNA3D_Renderer *driverData)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	return renderer->supportsDxt1;
}

static uint8_t D3D11_SupportsS3TC(FNA3D_Renderer *driverData)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	return renderer->supportsS3tc;
}

static uint8_t D3D11_SupportsHardwareInstancing(FNA3D_Renderer *driverData)
{
	return 1;
}

static uint8_t D3D11_SupportsNoOverwrite(FNA3D_Renderer *driverData)
{
	return 1;
}

static void D3D11_GetMaxTextureSlots(
	FNA3D_Renderer *driverData,
	int32_t *textures,
	int32_t *vertexTextures
) {
	*textures = D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT;
	*vertexTextures = D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT;
}

static int32_t D3D11_GetMaxMultiSampleCount(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	uint32_t levels;
	do
	{
		/* FIXME: This returns a result code, but the
		 * docs don't say if/when it can fail...
		 */
		ID3D11Device_CheckMultisampleQualityLevels(
			renderer->device,
			XNAToD3D_TextureFormat[format],
			multiSampleCount,
			&levels
		);
		if (levels > 0)
		{
			break;
		}
		multiSampleCount >>= 1;
	} while (multiSampleCount > 0);
	return multiSampleCount;
}

/* Debugging */

static void D3D11_SetStringMarker(FNA3D_Renderer *driverData, const char *text)
{
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;
	wchar_t wstr[256];
	MultiByteToWideChar(CP_ACP, 0, text, -1, wstr, 256);
	ID3DUserDefinedAnnotation_SetMarker(
		renderer->annotation,
		wstr
	);
}

/* External Interop */

static void D3D11_GetSysRenderer(
	FNA3D_Renderer *driverData,
	FNA3D_SysRendererEXT *sysrenderer
) {
	D3D11Renderer *renderer = (D3D11Renderer*) driverData;

	sysrenderer->rendererType = FNA3D_RENDERER_TYPE_D3D11_EXT;
	sysrenderer->renderer.d3d11.device = renderer->device;
	sysrenderer->renderer.d3d11.context = renderer->context;
}

static FNA3D_Texture* D3D11_CreateSysTexture(
	FNA3D_Renderer *driverData,
	FNA3D_SysTextureEXT *systexture
) {
	D3D11Texture *result;

	if (systexture->rendererType != FNA3D_RENDERER_TYPE_D3D11_EXT)
	{
		return NULL;
	}

	result = (D3D11Texture*) SDL_malloc(sizeof(D3D11Texture));
	SDL_zerop(result);

	result->handle = (ID3D11Resource*) systexture->texture.d3d11.handle;
	result->shaderView = (ID3D11ShaderResourceView*) systexture->texture.d3d11.shaderView;

	/* Everything else either happens to be 0 or is unused anyway! */

	IUnknown_AddRef(result->handle);
	ID3D11ShaderResourceView_AddRef(result->shaderView);
	return (FNA3D_Texture*) result;
}

/* Driver */

static uint8_t D3D11_PrepareWindowAttributes(uint32_t *flags)
{
	void* module = NULL;
	PFN_D3D11_CREATE_DEVICE D3D11CreateDeviceFunc;
	D3D_FEATURE_LEVEL levels[] =
	{
		D3D_FEATURE_LEVEL_11_1,
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0
	};
	HRESULT res;

	module = D3D11_PLATFORM_LoadD3D11();
	if (module == NULL)
	{
		return 0;
	}
	D3D11CreateDeviceFunc = D3D11_PLATFORM_GetCreateDeviceFunc(module);
	if (D3D11CreateDeviceFunc == NULL)
	{
		SDL_UnloadObject(module);
		return 0;
	}

	res = D3D11CreateDeviceFunc(
		NULL,
		D3D_DRIVER_TYPE_HARDWARE,
		NULL,
		D3D11_CREATE_DEVICE_BGRA_SUPPORT,
		levels,
		SDL_arraysize(levels),
		D3D11_SDK_VERSION,
		NULL,
		NULL,
		NULL
	);

	D3D11_PLATFORM_UnloadD3D11(module);

	if (FAILED(res))
	{
		FNA3D_LogWarn("D3D11 is unsupported! Error Code: %08X", res);
		return 0;
	}

	/* No window flags required */
	SDL_SetHint(SDL_HINT_VIDEO_EXTERNAL_CONTEXT, "1");
	return 1;
}

static void D3D11_GetDrawableSize(void* window, int32_t *w, int32_t *h)
{
	SDL_GetWindowSize((SDL_Window*) window, w, h);
}

static void D3D11_INTERNAL_InitializeFauxBackbuffer(
	D3D11Renderer *renderer,
	uint8_t scaleNearest
) {
	void* d3dCompilerModule;
	PFN_D3DCOMPILE D3DCompileFunc;
	ID3DBlob *blob;
	D3D11_INPUT_ELEMENT_DESC ePosition;
	D3D11_INPUT_ELEMENT_DESC eTexcoord;
	D3D11_INPUT_ELEMENT_DESC elements[2];
	D3D11_SAMPLER_DESC samplerDesc;
	D3D11_BUFFER_DESC vbufDesc;
	uint16_t indices[] =
	{
		0, 1, 3,
		1, 2, 3
	};
	D3D11_SUBRESOURCE_DATA indicesData;
	D3D11_BUFFER_DESC ibufDesc;
	D3D11_RASTERIZER_DESC rastDesc;
	D3D11_BLEND_DESC blendDesc;
	HRESULT res;

	/* Load the D3DCompile function */
	d3dCompilerModule = D3D11_PLATFORM_LoadCompiler();
	if (d3dCompilerModule == NULL)
	{
		FNA3D_LogError("Could not find " D3DCOMPILER_DLL);
	}
	D3DCompileFunc = D3D11_PLATFORM_GetCompileFunc(d3dCompilerModule);
	if (D3DCompileFunc == NULL)
	{
		FNA3D_LogError("Could not load function D3DCompile!");
	}

	/* Compile and create the vertex shader */
	res = D3DCompileFunc(
		FAUX_BLIT_VERTEX_SHADER, SDL_strlen(FAUX_BLIT_VERTEX_SHADER),
		"Faux-Backbuffer Blit Vertex Shader", NULL, NULL,
		"main", "vs_4_0", 0, 0, &blob, &blob
	);
	ERROR_CHECK_RETURN("Backbuffer vshader failed to compile",)

	res = ID3D11Device_CreateVertexShader(
		renderer->device,
		ID3D10Blob_GetBufferPointer(blob),
		ID3D10Blob_GetBufferSize(blob),
		NULL,
		&renderer->fauxBlitVS
	);
	ERROR_CHECK_RETURN("Backbuffer vshader creation failed",)

	/* Create the vertex shader input layout */
	ePosition.SemanticName = "SV_POSITION";
	ePosition.SemanticIndex = 0;
	ePosition.Format = DXGI_FORMAT_R32G32_FLOAT;
	ePosition.InputSlot = 0;
	ePosition.AlignedByteOffset = 0;
	ePosition.InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
	ePosition.InstanceDataStepRate = 0;

	eTexcoord.SemanticName = "TEXCOORD";
	eTexcoord.SemanticIndex = 0;
	eTexcoord.Format = DXGI_FORMAT_R32G32_FLOAT;
	eTexcoord.InputSlot = 0;
	eTexcoord.AlignedByteOffset = sizeof(float) * 2;
	eTexcoord.InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
	eTexcoord.InstanceDataStepRate = 0;

	elements[0] = ePosition;
	elements[1] = eTexcoord;
	res = ID3D11Device_CreateInputLayout(
		renderer->device,
		elements,
		2,
		ID3D10Blob_GetBufferPointer(blob),
		ID3D10Blob_GetBufferSize(blob),
		&renderer->fauxBlitLayout
	);
	ERROR_CHECK_RETURN("Backbuffer input layout creation failed",)

	/* Compile and create the pixel shader */
	res = D3DCompileFunc(
		FAUX_BLIT_PIXEL_SHADER, SDL_strlen(FAUX_BLIT_PIXEL_SHADER),
		"Faux-Backbuffer Blit Pixel Shader", NULL, NULL,
		"main", "ps_4_0", 0, 0, &blob, &blob
	);
	ERROR_CHECK_RETURN("Backbuffer pshader failed to compile",)

	ID3D11Device_CreatePixelShader(
		renderer->device,
		ID3D10Blob_GetBufferPointer(blob),
		ID3D10Blob_GetBufferSize(blob),
		NULL,
		&renderer->fauxBlitPS
	);
	ERROR_CHECK_RETURN("Backbuffer pshader creation failed",)

	/* We're done with the compiler now */
	D3D11_PLATFORM_UnloadCompiler(d3dCompilerModule);

	/* Create the faux backbuffer sampler state */
	samplerDesc.Filter = (
		scaleNearest ?
			D3D11_FILTER_MIN_MAG_MIP_POINT :
			D3D11_FILTER_MIN_MAG_MIP_LINEAR
	);
	samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_CLAMP;
	samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_CLAMP;
	samplerDesc.AddressW = D3D11_TEXTURE_ADDRESS_CLAMP;
	samplerDesc.MipLODBias = 0;
	samplerDesc.MaxAnisotropy = 1;
	samplerDesc.ComparisonFunc = D3D11_COMPARISON_ALWAYS;
	samplerDesc.MinLOD = 0;
	samplerDesc.MaxLOD = 0;
	res = ID3D11Device_CreateSamplerState(
		renderer->device,
		&samplerDesc,
		&renderer->fauxBlitSampler
	);
	ERROR_CHECK_RETURN("Backbuffer sampler state creation failed",)

	/* Create the faux backbuffer vertex buffer */
	vbufDesc.ByteWidth = 16 * sizeof(float);
	vbufDesc.Usage = D3D11_USAGE_DYNAMIC;
	vbufDesc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
	vbufDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
	vbufDesc.MiscFlags = 0;
	vbufDesc.StructureByteStride = 0;
	res = ID3D11Device_CreateBuffer(
		renderer->device,
		&vbufDesc,
		NULL,
		&renderer->fauxBlitVertexBuffer
	);
	ERROR_CHECK_RETURN("Backbuffer vertex buffer creation failed",)

	/* Initialize faux backbuffer index data */
	indicesData.pSysMem = &indices[0];
	indicesData.SysMemPitch = 0;
	indicesData.SysMemSlicePitch = 0;

	/* Create the faux backbuffer index buffer */
	ibufDesc.ByteWidth = 6 * sizeof(uint16_t);
	ibufDesc.Usage = D3D11_USAGE_IMMUTABLE;
	ibufDesc.BindFlags = D3D11_BIND_INDEX_BUFFER;
	ibufDesc.CPUAccessFlags = 0;
	ibufDesc.MiscFlags = 0;
	ibufDesc.StructureByteStride = 0;
	res = ID3D11Device_CreateBuffer(
		renderer->device,
		&ibufDesc,
		&indicesData,
		&renderer->fauxBlitIndexBuffer
	);
	ERROR_CHECK_RETURN("Backbuffer index buffer creation failed",)

	/* Create the faux backbuffer rasterizer state */
	rastDesc.AntialiasedLineEnable = 0;
	rastDesc.CullMode = D3D11_CULL_NONE;
	rastDesc.DepthBias = 0;
	rastDesc.DepthBiasClamp = 0;
	rastDesc.DepthClipEnable = 1;
	rastDesc.FillMode = D3D11_FILL_SOLID;
	rastDesc.FrontCounterClockwise = 0;
	rastDesc.MultisampleEnable = 0;
	rastDesc.ScissorEnable = 0;
	rastDesc.SlopeScaledDepthBias = 0;
	res = ID3D11Device_CreateRasterizerState(
		renderer->device,
		&rastDesc,
		&renderer->fauxRasterizer
	);
	ERROR_CHECK_RETURN("Backbuffer rasterizer state creation failed",)

	/* Create the faux backbuffer blend state */
	SDL_zero(blendDesc);
	blendDesc.AlphaToCoverageEnable = 0;
	blendDesc.IndependentBlendEnable = 0;
	blendDesc.RenderTarget[0].BlendEnable = 0;
	blendDesc.RenderTarget[0].SrcBlend = D3D11_BLEND_ONE;
	blendDesc.RenderTarget[0].DestBlend = D3D11_BLEND_ZERO;
	blendDesc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
	blendDesc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
	blendDesc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
	res = ID3D11Device_CreateBlendState(
		renderer->device,
		&blendDesc,
		&renderer->fauxBlendState
	);
	ERROR_CHECK_RETURN("Backbuffer blend state creation failed",)
}

static FNA3D_Device* D3D11_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
) {
	FNA3D_Device *result;
	D3D11Renderer *renderer;
	DXGI_ADAPTER_DESC1 adapterDesc;
	PFN_D3D11_CREATE_DEVICE D3D11CreateDeviceFunc;
	D3D_FEATURE_LEVEL levels[] =
	{
		D3D_FEATURE_LEVEL_11_1,
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0
	};
	uint32_t flags, supportsDxt3, supportsDxt5;
	int32_t i;
	HRESULT res;

	/* Allocate and zero out the renderer */
	renderer = (D3D11Renderer*) SDL_malloc(sizeof(D3D11Renderer));
	SDL_memset(renderer, '\0', sizeof(D3D11Renderer));

	/* Load CreateDXGIFactory1 */
	res = D3D11_PLATFORM_CreateDXGIFactory(
		&renderer->dxgi_dll,
		&renderer->factory
	);
	ERROR_CHECK_RETURN("Could not create DXGIFactory", NULL)
	D3D11_PLATFORM_GetDefaultAdapter(
		renderer->factory,
		&renderer->adapter
	);

	IDXGIAdapter1_GetDesc1(renderer->adapter, &adapterDesc);

	/* Load D3D11CreateDevice */
	renderer->d3d11_dll = D3D11_PLATFORM_LoadD3D11();
	if (renderer->d3d11_dll == NULL)
	{
		FNA3D_LogError("Could not find " D3D11_DLL);
		return NULL;
	}
	D3D11CreateDeviceFunc = D3D11_PLATFORM_GetCreateDeviceFunc(
		renderer->d3d11_dll
	);
	if (D3D11CreateDeviceFunc == NULL)
	{
		FNA3D_LogError("Could not load function D3D11CreateDevice!");
		return NULL;
	}

	/* Create the D3D11Device */
	flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
	if (debugMode)
	{
		flags |= D3D11_CREATE_DEVICE_DEBUG;
	}

try_create_device:
	res = D3D11CreateDeviceFunc(
		NULL, /* FIXME: Should be renderer->adapter! */
		D3D_DRIVER_TYPE_HARDWARE,
		NULL,
		flags,
		levels,
		SDL_arraysize(levels),
		D3D11_SDK_VERSION,
		&renderer->device,
		&renderer->featureLevel,
		&renderer->context
	);

	if (FAILED(res) && debugMode)
	{
		/* Creating a debug mode device will fail on some systems due to the necessary
		 * debug infrastructure not being available. Remove the debug flag and retry. */
		FNA3D_LogWarn("Creating device in debug mode failed with error %08X. Trying non-debug.", res);
		flags ^= D3D11_CREATE_DEVICE_DEBUG;
		debugMode = 0;
		goto try_create_device;
	}

	ERROR_CHECK_RETURN("Could not create D3D11Device", NULL)

	/* Print driver info */
	FNA3D_LogInfo("FNA3D Driver: D3D11");
	FNA3D_LogInfo("D3D11 Adapter: %S", adapterDesc.Description);

	/* Determine DXT/S3TC support.
	 * Note that we do NOT error check the return values!
	 */
	ID3D11Device_CheckFormatSupport(
		renderer->device,
		XNAToD3D_TextureFormat[FNA3D_SURFACEFORMAT_DXT1],
		&renderer->supportsDxt1
	);
	ID3D11Device_CheckFormatSupport(
		renderer->device,
		XNAToD3D_TextureFormat[FNA3D_SURFACEFORMAT_DXT3],
		&supportsDxt3
	);
	ID3D11Device_CheckFormatSupport(
		renderer->device,
		XNAToD3D_TextureFormat[FNA3D_SURFACEFORMAT_DXT5],
		&supportsDxt5
	);
	renderer->supportsS3tc = (supportsDxt3 || supportsDxt5);

	/* Initialize MojoShader context */
	MOJOSHADER_d3d11CreateContext(
		renderer->device,
		renderer->context,
		NULL,
		NULL,
		renderer
	);

	/* Initialize texture and sampler collections */
	for (i = 0; i < MAX_TOTAL_SAMPLERS; i += 1)
	{
		renderer->textures[i] = &NullTexture;
		renderer->samplers[i] = NULL;
	}

	/* Initialize SetStringMarker support, if available */
	if (renderer->featureLevel == D3D_FEATURE_LEVEL_11_1)
	{
		res = ID3D11DeviceContext_QueryInterface(
			renderer->context,
			&D3D_IID_ID3DUserDefinedAnnotation,
			(void**) &renderer->annotation
		);
		ERROR_CHECK("Could not get UserDefinedAnnotation")
	}
	else
	{
		FNA3D_LogInfo("SetStringMarker not supported!");
	}

	/* Initialize renderer members not covered by SDL_memset('\0') */
	renderer->debugMode = debugMode;
	renderer->blendFactor.r = 0xFF;
	renderer->blendFactor.g = 0xFF;
	renderer->blendFactor.b = 0xFF;
	renderer->blendFactor.a = 0xFF;
	renderer->multiSampleMask = -1; /* AKA 0xFFFFFFFF, ugh -flibit */
	renderer->topology = (FNA3D_PrimitiveType) -1; /* Force an update */

	/* Create and initialize the faux-backbuffer */
	D3D11_INTERNAL_CreateFramebuffer(
		renderer,
		presentationParameters
	);
	D3D11_INTERNAL_InitializeFauxBackbuffer(
		renderer,
		SDL_GetHintBoolean("FNA3D_BACKBUFFER_SCALE_NEAREST", SDL_FALSE)
	);
	D3D11_INTERNAL_SetPresentationInterval(
		renderer,
		presentationParameters->presentationInterval
	);

	/* A mutex, for ID3D11Context */
	renderer->ctxLock = SDL_CreateMutex();

	/* Create and return the FNA3D_Device */
	result = (FNA3D_Device*) SDL_malloc(sizeof(FNA3D_Device));
	result->driverData = (FNA3D_Renderer*) renderer;
	ASSIGN_DRIVER(D3D11)
	return result;
}

#ifdef __WINRT__

/* WinRT Platform Implementation */

static void* D3D11_PLATFORM_LoadD3D11()
{
	return (size_t) 69420;
}

static void D3D11_PLATFORM_UnloadD3D11(void* module)
{
	/* No-op */
}

static PFN_D3D11_CREATE_DEVICE D3D11_PLATFORM_GetCreateDeviceFunc(void* module)
{
	return D3D11CreateDevice;
}

static void* D3D11_PLATFORM_LoadCompiler()
{
	return (size_t) 42069;
}
static void D3D11_PLATFORM_UnloadCompiler(void* module)
{
	/* No-op */
}

static PFN_D3DCOMPILE D3D11_PLATFORM_GetCompileFunc(void* module)
{
	return D3DCompile;
}

static HRESULT D3D11_PLATFORM_CreateDXGIFactory(
	void** module,
	void** factory
) {
	return CreateDXGIFactory1(
		&D3D_IID_IDXGIFactory2,
		factory
	);
}

static void D3D11_PLATFORM_UnloadDXGI(void* module)
{
	/* No-op */
}

static void D3D11_PLATFORM_GetDefaultAdapter(
	void* factory,
	IDXGIAdapter1 **adapter
) {
	IDXGIFactory2_EnumAdapters1(
		(IDXGIFactory2*) factory,
		0,
		adapter
	);
}

static void D3D11_PLATFORM_CreateSwapChain(
	D3D11Renderer *renderer,
	FNA3D_PresentationParameters *pp
) {
	DXGI_SWAP_CHAIN_DESC1 swapchainDesc;
	HRESULT res;
	SDL_SysWMinfo info;

	SDL_VERSION(&info.version);
	SDL_GetWindowWMInfo((SDL_Window*) pp->deviceWindowHandle, &info);

	/* Initialize swapchain descriptor */
	swapchainDesc.Width = 0;
	swapchainDesc.Height = 0;
	swapchainDesc.Format = XNAToD3D_TextureFormat[pp->backBufferFormat];
	swapchainDesc.Stereo = 0;
	swapchainDesc.SampleDesc.Count = 1;
	swapchainDesc.SampleDesc.Quality = 0;
	swapchainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	swapchainDesc.BufferCount = 3;
	swapchainDesc.Scaling = DXGI_MODE_SCALING_UNSPECIFIED;
	swapchainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;
	swapchainDesc.AlphaMode = DXGI_ALPHA_MODE_UNSPECIFIED;
	swapchainDesc.Flags = 0;

	/* Create the swap chain! */
	res = IDXGIFactory2_CreateSwapChainForCoreWindow(
		(IDXGIFactory2*) renderer->factory,
		(IUnknown*) renderer->device,
		(IUnknown*) info.info.winrt.window,
		&swapchainDesc,
		NULL,
		(IDXGISwapChain1**) &renderer->swapchain
	);
	ERROR_CHECK("Could not create swapchain")
}

#else

/* Win32 Platform Implementation */

static void* D3D11_PLATFORM_LoadD3D11()
{
	return SDL_LoadObject(D3D11_DLL);
}

static void D3D11_PLATFORM_UnloadD3D11(void* module)
{
	SDL_UnloadObject(module);
}

static PFN_D3D11_CREATE_DEVICE D3D11_PLATFORM_GetCreateDeviceFunc(void* module)
{
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
	return (PFN_D3D11_CREATE_DEVICE) SDL_LoadFunction(
		module,
		"D3D11CreateDevice"
	);
#pragma GCC diagnostic pop
}

static void* D3D11_PLATFORM_LoadCompiler()
{
	return SDL_LoadObject(D3DCOMPILER_DLL);
}

static void D3D11_PLATFORM_UnloadCompiler(void* module)
{
	SDL_UnloadObject(module);
}

static PFN_D3DCOMPILE D3D11_PLATFORM_GetCompileFunc(void* module)
{
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
	return (PFN_D3DCOMPILE) SDL_LoadFunction(module, "D3DCompile");
#pragma GCC diagnostic pop
}

static HRESULT D3D11_PLATFORM_CreateDXGIFactory(
	void** module,
	void** factory
) {
	typedef HRESULT(WINAPI *PFN_CREATE_DXGI_FACTORY)(const GUID *riid, void **ppFactory);
	PFN_CREATE_DXGI_FACTORY CreateDXGIFactoryFunc;

	/* Load DXGI... */
	*module = SDL_LoadObject(DXGI_DLL);
	if (*module == NULL)
	{
		FNA3D_LogError("Could not find " DXGI_DLL);
		return -1;
	}

	/* Load CreateFactory... */
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
	CreateDXGIFactoryFunc = (PFN_CREATE_DXGI_FACTORY) SDL_LoadFunction(
		*module,
		"CreateDXGIFactory1"
	);
#pragma GCC diagnostic pop
	if (CreateDXGIFactoryFunc == NULL)
	{
		FNA3D_LogError("Could not load function CreateDXGIFactory1!");
		return -1;
	}

	/* ... Create, finally. */
	return CreateDXGIFactoryFunc(
		&D3D_IID_IDXGIFactory1,
		factory
	);
}

static void D3D11_PLATFORM_UnloadDXGI(void* module)
{
	SDL_UnloadObject(module);
}

static void D3D11_PLATFORM_GetDefaultAdapter(
	void* factory,
	IDXGIAdapter1 **adapter
) {
	IDXGIFactory1_EnumAdapters1(
		(IDXGIFactory1*) factory,
		0,
		adapter
	);
}

static void D3D11_PLATFORM_CreateSwapChain(
	D3D11Renderer *renderer,
	FNA3D_PresentationParameters *pp
) {
	IDXGIOutput *output;
	IDXGIFactory1* pParent;
	DXGI_SWAP_CHAIN_DESC swapchainDesc;
	DXGI_MODE_DESC swapchainBufferDesc;
	SDL_SysWMinfo info;
	HWND dxgiHandle;
	HRESULT res;

	SDL_VERSION(&info.version);
	SDL_GetWindowWMInfo((SDL_Window*) pp->deviceWindowHandle, &info);
	dxgiHandle = info.info.win.window;

	/* Initialize swapchain buffer descriptor */
	swapchainBufferDesc.Width = 0;
	swapchainBufferDesc.Height = 0;
	swapchainBufferDesc.RefreshRate.Numerator = 0;
	swapchainBufferDesc.RefreshRate.Denominator = 0;
	swapchainBufferDesc.Format = XNAToD3D_TextureFormat[pp->backBufferFormat];
	swapchainBufferDesc.ScanlineOrdering = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
	swapchainBufferDesc.Scaling = DXGI_MODE_SCALING_UNSPECIFIED;

	IDXGIAdapter_EnumOutputs(
		(IDXGIAdapter*) renderer->adapter,
		0,
		&output
	);
	IDXGIOutput_FindClosestMatchingMode(
		output,
		&swapchainBufferDesc,
		&swapchainDesc.BufferDesc,
		(IUnknown*) renderer->device
	);

	/* Initialize the swapchain descriptor */
	swapchainDesc.BufferDesc = swapchainBufferDesc;
	swapchainDesc.SampleDesc.Count = 1;
	swapchainDesc.SampleDesc.Quality = 0;
	swapchainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	swapchainDesc.BufferCount = 3;
	swapchainDesc.OutputWindow = dxgiHandle;
	swapchainDesc.Windowed = 1;
	swapchainDesc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
	swapchainDesc.Flags = 0;

	/* Create the swapchain! */
	res = IDXGIFactory1_CreateSwapChain(
		(IDXGIFactory1*) renderer->factory,
		(IUnknown*) renderer->device,
		&swapchainDesc,
		&renderer->swapchain
	);
	ERROR_CHECK("Could not create swapchain")

	/*
	 * The swapchain's parent is a separate factory from the factory that
	 * we used to create the swapchain, and only that parent can be used to
	 * set the window association. Trying to set an association on our factory
	 * will silently fail and doesn't even verify arguments or return errors.
	 * See https://gamedev.net/forums/topic/634235-dxgidisabling-altenter/4999955/
	 */
	res = IDXGISwapChain_GetParent(
		(IDXGISwapChain*) renderer->swapchain,
		&D3D_IID_IDXGIFactory1,
		(void**) &pParent
	);
	if (FAILED(res))
	{
		FNA3D_LogWarn(
			"Could not get swapchain parent! Error Code: %08X",
			res
		);
	}
	else
	{
		/* Disable DXGI window crap */
		res = IDXGIFactory1_MakeWindowAssociation(
			pParent,
			dxgiHandle,
			DXGI_MWA_NO_WINDOW_CHANGES
		);
		if (FAILED(res))
		{
			FNA3D_LogWarn(
				"MakeWindowAssociation failed! Error Code: %08X",
				res
			);
		}
	}
}

#endif

FNA3D_Driver D3D11Driver = {
	"D3D11",
	D3D11_PrepareWindowAttributes,
	D3D11_GetDrawableSize,
	D3D11_CreateDevice
};

#else

extern int this_tu_is_empty;

#endif /* FNA3D_DRIVER_D3D11 */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
