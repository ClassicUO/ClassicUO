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

#ifndef FNA3D_DRIVER_H
#define FNA3D_DRIVER_H

#include "mojoshader.h"
#include "FNA3D.h"
#include "FNA3D_SysRenderer.h"

/* Windows/Visual Studio cruft */
#ifdef _WIN32
#define inline __inline
#endif

#ifdef __cplusplus
#define FNA3D_SHAREDINTERNAL extern "C"
#else
#define FNA3D_SHAREDINTERNAL extern
#endif

/* Logging */

FNA3D_SHAREDINTERNAL void FNA3D_LogInfo(const char *fmt, ...);
FNA3D_SHAREDINTERNAL void FNA3D_LogWarn(const char *fmt, ...);
FNA3D_SHAREDINTERNAL void FNA3D_LogError(const char *fmt, ...);

/* Internal Helper Utilities */

#define LinkedList_Add(start, toAdd, curr) \
	toAdd->next = NULL; \
	if (start == NULL) \
	{ \
		start = toAdd; \
	} \
	else \
	{ \
		curr = start; \
		while (curr->next != NULL) \
		{ \
			curr = curr->next; \
		} \
		curr->next = toAdd; \
	}

/* This macro does NOT destroy `toRemove`!
 * It only removes the element from the list.
 */
#define LinkedList_Remove(start, toRemove, curr, prev) \
	curr = start; \
	while (curr != NULL) \
	{ \
		if (curr == toRemove) \
		{ \
			if (curr == start) \
			{ \
				start = curr->next; \
			} \
			else \
			{ \
				prev->next = curr->next; \
			} \
			break; \
		} \
		prev = curr; \
		curr = curr->next; \
	} \
	if (curr == NULL) \
	{ \
		SDL_assert(0 && "LinkedList element not found!"); \
	} \

static inline int32_t Texture_GetBlockSize(
	FNA3D_SurfaceFormat format
) {
	switch (format)
	{
	case FNA3D_SURFACEFORMAT_DXT1:
	case FNA3D_SURFACEFORMAT_DXT3:
	case FNA3D_SURFACEFORMAT_DXT5:
	case FNA3D_SURFACEFORMAT_DXT5SRGB_EXT:
	case FNA3D_SURFACEFORMAT_BC7_EXT:
	case FNA3D_SURFACEFORMAT_BC7SRGB_EXT:
		return 4;
	case FNA3D_SURFACEFORMAT_ALPHA8:
	case FNA3D_SURFACEFORMAT_BGR565:
	case FNA3D_SURFACEFORMAT_BGRA4444:
	case FNA3D_SURFACEFORMAT_BGRA5551:
	case FNA3D_SURFACEFORMAT_HALFSINGLE:
	case FNA3D_SURFACEFORMAT_NORMALIZEDBYTE2:
	case FNA3D_SURFACEFORMAT_COLOR:
	case FNA3D_SURFACEFORMAT_SINGLE:
	case FNA3D_SURFACEFORMAT_RG32:
	case FNA3D_SURFACEFORMAT_HALFVECTOR2:
	case FNA3D_SURFACEFORMAT_NORMALIZEDBYTE4:
	case FNA3D_SURFACEFORMAT_RGBA1010102:
	case FNA3D_SURFACEFORMAT_COLORBGRA_EXT:
	case FNA3D_SURFACEFORMAT_COLORSRGB_EXT:
	case FNA3D_SURFACEFORMAT_HALFVECTOR4:
	case FNA3D_SURFACEFORMAT_RGBA64:
	case FNA3D_SURFACEFORMAT_VECTOR2:
	case FNA3D_SURFACEFORMAT_HDRBLENDABLE:
	case FNA3D_SURFACEFORMAT_VECTOR4:
	case FNA3D_SURFACEFORMAT_USHORT_EXT:
	case FNA3D_SURFACEFORMAT_BYTE_EXT:
		return 1;
	default:
		FNA3D_LogError(
			"Unrecognized SurfaceFormat!"
		);
		return 0;
	}
}

static inline int32_t Texture_GetFormatSize(
	FNA3D_SurfaceFormat format
) {
	switch (format)
	{
		case FNA3D_SURFACEFORMAT_DXT1:
			return 8;
		case FNA3D_SURFACEFORMAT_DXT3:
		case FNA3D_SURFACEFORMAT_DXT5:
		case FNA3D_SURFACEFORMAT_DXT5SRGB_EXT:
		case FNA3D_SURFACEFORMAT_BC7_EXT:
		case FNA3D_SURFACEFORMAT_BC7SRGB_EXT:
			return 16;
		case FNA3D_SURFACEFORMAT_ALPHA8:
		case FNA3D_SURFACEFORMAT_BYTE_EXT:
			return 1;
		case FNA3D_SURFACEFORMAT_BGR565:
		case FNA3D_SURFACEFORMAT_BGRA4444:
		case FNA3D_SURFACEFORMAT_BGRA5551:
		case FNA3D_SURFACEFORMAT_HALFSINGLE:
		case FNA3D_SURFACEFORMAT_NORMALIZEDBYTE2:
		case FNA3D_SURFACEFORMAT_USHORT_EXT:
			return 2;
		case FNA3D_SURFACEFORMAT_COLOR:
		case FNA3D_SURFACEFORMAT_SINGLE:
		case FNA3D_SURFACEFORMAT_RG32:
		case FNA3D_SURFACEFORMAT_HALFVECTOR2:
		case FNA3D_SURFACEFORMAT_NORMALIZEDBYTE4:
		case FNA3D_SURFACEFORMAT_RGBA1010102:
		case FNA3D_SURFACEFORMAT_COLORBGRA_EXT:
		case FNA3D_SURFACEFORMAT_COLORSRGB_EXT:
			return 4;
		case FNA3D_SURFACEFORMAT_HALFVECTOR4:
		case FNA3D_SURFACEFORMAT_RGBA64:
		case FNA3D_SURFACEFORMAT_VECTOR2:
		case FNA3D_SURFACEFORMAT_HDRBLENDABLE:
			return 8;
		case FNA3D_SURFACEFORMAT_VECTOR4:
			return 16;
		default:
			FNA3D_LogError(
				"Unrecognized SurfaceFormat!"
			);
			return 0;
	}
}

static inline int32_t PrimitiveVerts(
	FNA3D_PrimitiveType primitiveType,
	int32_t primitiveCount
) {
	switch (primitiveType)
	{
		case FNA3D_PRIMITIVETYPE_TRIANGLELIST:
			return primitiveCount * 3;
		case FNA3D_PRIMITIVETYPE_TRIANGLESTRIP:
			return primitiveCount + 2;
		case FNA3D_PRIMITIVETYPE_LINELIST:
			return primitiveCount * 2;
		case FNA3D_PRIMITIVETYPE_LINESTRIP:
			return primitiveCount + 1;
		case FNA3D_PRIMITIVETYPE_POINTLIST_EXT:
			return primitiveCount;
		default:
			FNA3D_LogError(
				"Unrecognized primitive type!"
			);
			return 0;
	}
}

static inline MOJOSHADER_usage VertexAttribUsage(
	FNA3D_VertexElementUsage usage
) {
	switch (usage)
	{
		case FNA3D_VERTEXELEMENTUSAGE_POSITION:
			return MOJOSHADER_USAGE_POSITION;
		case FNA3D_VERTEXELEMENTUSAGE_COLOR:
			return MOJOSHADER_USAGE_COLOR;
		case FNA3D_VERTEXELEMENTUSAGE_TEXTURECOORDINATE:
			return MOJOSHADER_USAGE_TEXCOORD;
		case FNA3D_VERTEXELEMENTUSAGE_NORMAL:
			return MOJOSHADER_USAGE_NORMAL;
		case FNA3D_VERTEXELEMENTUSAGE_BINORMAL:
			return MOJOSHADER_USAGE_BINORMAL;
		case FNA3D_VERTEXELEMENTUSAGE_TANGENT:
			return MOJOSHADER_USAGE_TANGENT;
		case FNA3D_VERTEXELEMENTUSAGE_BLENDINDICES:
			return MOJOSHADER_USAGE_BLENDINDICES;
		case FNA3D_VERTEXELEMENTUSAGE_BLENDWEIGHT:
			return MOJOSHADER_USAGE_BLENDWEIGHT;
		case FNA3D_VERTEXELEMENTUSAGE_FOG:
			return MOJOSHADER_USAGE_FOG;
		case FNA3D_VERTEXELEMENTUSAGE_POINTSIZE:
			return MOJOSHADER_USAGE_POINTSIZE;
		case FNA3D_VERTEXELEMENTUSAGE_SAMPLE:
			return MOJOSHADER_USAGE_SAMPLE;
		case FNA3D_VERTEXELEMENTUSAGE_TESSELATEFACTOR:
			return MOJOSHADER_USAGE_TESSFACTOR;
		default:
			FNA3D_LogError(
				"Unrecognized VertexElementUsage!"
			);
			return (MOJOSHADER_usage) 0;
	}
}

static inline int32_t IndexSize(FNA3D_IndexElementSize size)
{
	return (size == FNA3D_INDEXELEMENTSIZE_16BIT) ? 2 : 4;
}

static inline int32_t BytesPerRow(
	int32_t width,
	FNA3D_SurfaceFormat format
) {
	int32_t blocksPerRow = width;
	int32_t blockSize = Texture_GetBlockSize(format);

	if (blockSize > 1)
	{
		blocksPerRow = (width + blockSize - 1) / blockSize;
	}

	return blocksPerRow * Texture_GetFormatSize(format);
}

static inline int32_t BytesPerImage(
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format
) {
	int32_t blocksPerRow = width;
	int32_t blocksPerColumn = height;
	int32_t blockSize = Texture_GetBlockSize(format);

	if (blockSize > 1)
	{
		blocksPerRow = (width + blockSize - 1) / blockSize;
		blocksPerColumn = (height + blockSize - 1) / blockSize;
	}

	return blocksPerRow * blocksPerColumn * Texture_GetFormatSize(format);
}

/* XNA GraphicsDevice Limits */

#define MAX_TEXTURE_SAMPLERS		16
#define MAX_VERTEXTEXTURE_SAMPLERS	4
#define MAX_TOTAL_SAMPLERS		(MAX_TEXTURE_SAMPLERS + MAX_VERTEXTEXTURE_SAMPLERS)

#define MAX_VERTEX_ATTRIBUTES		16
#define MAX_BOUND_VERTEX_BUFFERS	16

#define MAX_RENDERTARGET_BINDINGS	4

/* FNA3D_Device Definition */

typedef struct FNA3D_Renderer FNA3D_Renderer;

struct FNA3D_Device
{
	/* Quit */

	void (*DestroyDevice)(FNA3D_Device *device);

	/* Presentation */

	void (*SwapBuffers)(
		FNA3D_Renderer *driverData,
		FNA3D_Rect *sourceRectangle,
		FNA3D_Rect *destinationRectangle,
		void* overrideWindowHandle
	);

	/* Drawing */

	void (*Clear)(
		FNA3D_Renderer *driverData,
		FNA3D_ClearOptions options,
		FNA3D_Vec4 *color,
		float depth,
		int32_t stencil
	);

	void (*DrawIndexedPrimitives)(
		FNA3D_Renderer *driverData,
		FNA3D_PrimitiveType primitiveType,
		int32_t baseVertex,
		int32_t minVertexIndex,
		int32_t numVertices,
		int32_t startIndex,
		int32_t primitiveCount,
		FNA3D_Buffer *indices,
		FNA3D_IndexElementSize indexElementSize
	);
	void (*DrawInstancedPrimitives)(
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
	);
	void (*DrawPrimitives)(
		FNA3D_Renderer *driverData,
		FNA3D_PrimitiveType primitiveType,
		int32_t vertexStart,
		int32_t primitiveCount
	);

	/* Mutable Render States */

	void (*SetViewport)(FNA3D_Renderer *driverData, FNA3D_Viewport *viewport);
	void (*SetScissorRect)(FNA3D_Renderer *driverData, FNA3D_Rect *scissor);

	void (*GetBlendFactor)(
		FNA3D_Renderer *driverData,
		FNA3D_Color *blendFactor
	);
	void (*SetBlendFactor)(
		FNA3D_Renderer *driverData,
		FNA3D_Color *blendFactor
	);

	int32_t (*GetMultiSampleMask)(FNA3D_Renderer *driverData);
	void (*SetMultiSampleMask)(FNA3D_Renderer *driverData, int32_t mask);

	int32_t (*GetReferenceStencil)(FNA3D_Renderer *driverData);
	void (*SetReferenceStencil)(FNA3D_Renderer *driverData, int32_t ref);

	/* Immutable Render States */

	void (*SetBlendState)(
		FNA3D_Renderer *driverData,
		FNA3D_BlendState *blendState
	);
	void (*SetDepthStencilState)(
		FNA3D_Renderer *driverData,
		FNA3D_DepthStencilState *depthStencilState
	);
	void (*ApplyRasterizerState)(
		FNA3D_Renderer *driverData,
		FNA3D_RasterizerState *rasterizerState
	);
	void (*VerifySampler)(
		FNA3D_Renderer *driverData,
		int32_t index,
		FNA3D_Texture *texture,
		FNA3D_SamplerState *sampler
	);
	void (*VerifyVertexSampler)(
		FNA3D_Renderer *driverData,
		int32_t index,
		FNA3D_Texture *texture,
		FNA3D_SamplerState *sampler
	);

	void (*ApplyVertexBufferBindings)(
		FNA3D_Renderer *driverData,
		FNA3D_VertexBufferBinding *bindings,
		int32_t numBindings,
		uint8_t bindingsUpdated,
		int32_t baseVertex
	);

	/* Render Targets */

	void (*SetRenderTargets)(
		FNA3D_Renderer *driverData,
		FNA3D_RenderTargetBinding *renderTargets,
		int32_t numRenderTargets,
		FNA3D_Renderbuffer *depthStencilBuffer,
		FNA3D_DepthFormat depthFormat,
		uint8_t preserveTargetContents
	);

	void (*ResolveTarget)(
		FNA3D_Renderer *driverData,
		FNA3D_RenderTargetBinding *target
	);

	/* Backbuffer Functions */

	void (*ResetBackbuffer)(
		FNA3D_Renderer *driverData,
		FNA3D_PresentationParameters *presentationParameters
	);

	void (*ReadBackbuffer)(
		FNA3D_Renderer *driverData,
		int32_t x,
		int32_t y,
		int32_t w,
		int32_t h,
		void* data,
		int32_t dataLength
	);

	void (*GetBackbufferSize)(
		FNA3D_Renderer *driverData,
		int32_t *w,
		int32_t *h
	);

	FNA3D_SurfaceFormat (*GetBackbufferSurfaceFormat)(FNA3D_Renderer *driverData);

	FNA3D_DepthFormat (*GetBackbufferDepthFormat)(FNA3D_Renderer *driverData);

	int32_t (*GetBackbufferMultiSampleCount)(FNA3D_Renderer *driverData);

	/* Textures */

	FNA3D_Texture* (*CreateTexture2D)(
		FNA3D_Renderer *driverData,
		FNA3D_SurfaceFormat format,
		int32_t width,
		int32_t height,
		int32_t levelCount,
		uint8_t isRenderTarget
	);
	FNA3D_Texture* (*CreateTexture3D)(
		FNA3D_Renderer *driverData,
		FNA3D_SurfaceFormat format,
		int32_t width,
		int32_t height,
		int32_t depth,
		int32_t levelCount
	);
	FNA3D_Texture* (*CreateTextureCube)(
		FNA3D_Renderer *driverData,
		FNA3D_SurfaceFormat format,
		int32_t size,
		int32_t levelCount,
		uint8_t isRenderTarget
	);
	void (*AddDisposeTexture)(
		FNA3D_Renderer *driverData,
		FNA3D_Texture *texture
	);
	void (*SetTextureData2D)(
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
	void (*SetTextureData3D)(
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
	);
	void (*SetTextureDataCube)(
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
	);
	void (*SetTextureDataYUV)(
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
	);
	void (*GetTextureData2D)(
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
	void (*GetTextureData3D)(
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
	);
	void (*GetTextureDataCube)(
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
	);

	/* Renderbuffers */

	FNA3D_Renderbuffer* (*GenColorRenderbuffer)(
		FNA3D_Renderer *driverData,
		int32_t width,
		int32_t height,
		FNA3D_SurfaceFormat format,
		int32_t multiSampleCount,
		FNA3D_Texture *texture
	);
	FNA3D_Renderbuffer* (*GenDepthStencilRenderbuffer)(
		FNA3D_Renderer *driverData,
		int32_t width,
		int32_t height,
		FNA3D_DepthFormat format,
		int32_t multiSampleCount
	);
	void (*AddDisposeRenderbuffer)(
		FNA3D_Renderer *driverData,
		FNA3D_Renderbuffer *renderbuffer
	);

	/* Vertex Buffers */

	FNA3D_Buffer* (*GenVertexBuffer)(
		FNA3D_Renderer *driverData,
		uint8_t dynamic,
		FNA3D_BufferUsage usage,
		int32_t sizeInBytes
	);
	void (*AddDisposeVertexBuffer)(
		FNA3D_Renderer *driverData,
		FNA3D_Buffer *buffer
	);
	void (*SetVertexBufferData)(
		FNA3D_Renderer *driverData,
		FNA3D_Buffer *buffer,
		int32_t offsetInBytes,
		void* data,
		int32_t elementCount,
		int32_t elementSizeInBytes,
		int32_t vertexStride,
		FNA3D_SetDataOptions options
	);
	void (*GetVertexBufferData)(
		FNA3D_Renderer *driverData,
		FNA3D_Buffer *buffer,
		int32_t offsetInBytes,
		void* data,
		int32_t elementCount,
		int32_t elementSizeInBytes,
		int32_t vertexStride
	);

	/* Index Buffers */

	FNA3D_Buffer* (*GenIndexBuffer)(
		FNA3D_Renderer *driverData,
		uint8_t dynamic,
		FNA3D_BufferUsage usage,
		int32_t sizeInBytes
	);
	void (*AddDisposeIndexBuffer)(
		FNA3D_Renderer *driverData,
		FNA3D_Buffer *buffer
	);
	void (*SetIndexBufferData)(
		FNA3D_Renderer *driverData,
		FNA3D_Buffer *buffer,
		int32_t offsetInBytes,
		void* data,
		int32_t dataLength,
		FNA3D_SetDataOptions options
	);
	void (*GetIndexBufferData)(
		FNA3D_Renderer *driverData,
		FNA3D_Buffer *buffer,
		int32_t offsetInBytes,
		void* data,
		int32_t dataLength
	);

	/* Effects */

	void (*CreateEffect)(
		FNA3D_Renderer *driverData,
		uint8_t *effectCode,
		uint32_t effectCodeLength,
		FNA3D_Effect **effect,
		MOJOSHADER_effect **result
	);
	void (*CloneEffect)(
		FNA3D_Renderer *driverData,
		FNA3D_Effect *cloneSource,
		FNA3D_Effect **effect,
		MOJOSHADER_effect **result
	);
	void (*AddDisposeEffect)(
		FNA3D_Renderer *driverData,
		FNA3D_Effect *effect
	);
	void (*SetEffectTechnique)(
		FNA3D_Renderer *driverData,
		FNA3D_Effect *effect,
		MOJOSHADER_effectTechnique *technique
	);
	void (*ApplyEffect)(
		FNA3D_Renderer *driverData,
		FNA3D_Effect *effect,
		uint32_t pass,
		MOJOSHADER_effectStateChanges *stateChanges
	);
	void (*BeginPassRestore)(
		FNA3D_Renderer *driverData,
		FNA3D_Effect *effect,
		MOJOSHADER_effectStateChanges *stateChanges
	);
	void (*EndPassRestore)(
		FNA3D_Renderer *driverData,
		FNA3D_Effect *effect
	);

	/* Queries */

	FNA3D_Query* (*CreateQuery)(FNA3D_Renderer *driverData);
	void (*AddDisposeQuery)(FNA3D_Renderer *driverData, FNA3D_Query *query);
	void (*QueryBegin)(FNA3D_Renderer *driverData, FNA3D_Query *query);
	void (*QueryEnd)(FNA3D_Renderer *driverData, FNA3D_Query *query);
	uint8_t (*QueryComplete)(FNA3D_Renderer *driverData, FNA3D_Query *query);
	int32_t (*QueryPixelCount)(
		FNA3D_Renderer *driverData,
		FNA3D_Query *query
	);

	/* Feature Queries */

	uint8_t (*SupportsDXT1)(FNA3D_Renderer *driverData);
	uint8_t (*SupportsS3TC)(FNA3D_Renderer *driverData);
	uint8_t (*SupportsBC7)(FNA3D_Renderer *driverData);
	uint8_t (*SupportsHardwareInstancing)(FNA3D_Renderer *driverData);
	uint8_t (*SupportsNoOverwrite)(FNA3D_Renderer *driverData);
	uint8_t (*SupportsSRGBRenderTargets)(FNA3D_Renderer *driverData);

	void (*GetMaxTextureSlots)(
		FNA3D_Renderer *driverData,
		int32_t *textures,
		int32_t *vertexTextures
	);
	int32_t (*GetMaxMultiSampleCount)(
		FNA3D_Renderer *driverData,
		FNA3D_SurfaceFormat format,
		int32_t multiSampleCount
	);

	/* Debugging */

	void (*SetStringMarker)(FNA3D_Renderer *driverData, const char *text);
	void (*SetTextureName)(FNA3D_Renderer *driverData, FNA3D_Texture *texture, const char *text);

	/* External Interop */

	void (*GetSysRenderer)(
		FNA3D_Renderer *driverData,
		FNA3D_SysRendererEXT *renderer
	);

	FNA3D_Texture* (*CreateSysTexture)(
		FNA3D_Renderer *driverData,
		FNA3D_SysTextureEXT *externalTextureInfo
	);

	/* Opaque pointer for the Driver */
	FNA3D_Renderer *driverData;
};

#define ASSIGN_DRIVER_FUNC(func, name) \
	result->func = name##_##func;
#define ASSIGN_DRIVER(name) \
	ASSIGN_DRIVER_FUNC(DestroyDevice, name) \
	ASSIGN_DRIVER_FUNC(SwapBuffers, name) \
	ASSIGN_DRIVER_FUNC(Clear, name) \
	ASSIGN_DRIVER_FUNC(DrawIndexedPrimitives, name) \
	ASSIGN_DRIVER_FUNC(DrawInstancedPrimitives, name) \
	ASSIGN_DRIVER_FUNC(DrawPrimitives, name) \
	ASSIGN_DRIVER_FUNC(SetViewport, name) \
	ASSIGN_DRIVER_FUNC(SetScissorRect, name) \
	ASSIGN_DRIVER_FUNC(GetBlendFactor, name) \
	ASSIGN_DRIVER_FUNC(SetBlendFactor, name) \
	ASSIGN_DRIVER_FUNC(GetMultiSampleMask, name) \
	ASSIGN_DRIVER_FUNC(SetMultiSampleMask, name) \
	ASSIGN_DRIVER_FUNC(GetReferenceStencil, name) \
	ASSIGN_DRIVER_FUNC(SetReferenceStencil, name) \
	ASSIGN_DRIVER_FUNC(SetBlendState, name) \
	ASSIGN_DRIVER_FUNC(SetDepthStencilState, name) \
	ASSIGN_DRIVER_FUNC(ApplyRasterizerState, name) \
	ASSIGN_DRIVER_FUNC(VerifySampler, name) \
	ASSIGN_DRIVER_FUNC(VerifyVertexSampler, name) \
	ASSIGN_DRIVER_FUNC(ApplyVertexBufferBindings, name) \
	ASSIGN_DRIVER_FUNC(SetRenderTargets, name) \
	ASSIGN_DRIVER_FUNC(ResolveTarget, name) \
	ASSIGN_DRIVER_FUNC(ResetBackbuffer, name) \
	ASSIGN_DRIVER_FUNC(ReadBackbuffer, name) \
	ASSIGN_DRIVER_FUNC(GetBackbufferSize, name) \
	ASSIGN_DRIVER_FUNC(GetBackbufferSurfaceFormat, name) \
	ASSIGN_DRIVER_FUNC(GetBackbufferDepthFormat, name) \
	ASSIGN_DRIVER_FUNC(GetBackbufferMultiSampleCount, name) \
	ASSIGN_DRIVER_FUNC(CreateTexture2D, name) \
	ASSIGN_DRIVER_FUNC(CreateTexture3D, name) \
	ASSIGN_DRIVER_FUNC(CreateTextureCube, name) \
	ASSIGN_DRIVER_FUNC(AddDisposeTexture, name) \
	ASSIGN_DRIVER_FUNC(SetTextureData2D, name) \
	ASSIGN_DRIVER_FUNC(SetTextureData3D, name) \
	ASSIGN_DRIVER_FUNC(SetTextureDataCube, name) \
	ASSIGN_DRIVER_FUNC(SetTextureDataYUV, name) \
	ASSIGN_DRIVER_FUNC(GetTextureData2D, name) \
	ASSIGN_DRIVER_FUNC(GetTextureData3D, name) \
	ASSIGN_DRIVER_FUNC(GetTextureDataCube, name) \
	ASSIGN_DRIVER_FUNC(GenColorRenderbuffer, name) \
	ASSIGN_DRIVER_FUNC(GenDepthStencilRenderbuffer, name) \
	ASSIGN_DRIVER_FUNC(AddDisposeRenderbuffer, name) \
	ASSIGN_DRIVER_FUNC(GenVertexBuffer, name) \
	ASSIGN_DRIVER_FUNC(GenIndexBuffer, name) \
	ASSIGN_DRIVER_FUNC(AddDisposeVertexBuffer, name) \
	ASSIGN_DRIVER_FUNC(SetVertexBufferData, name) \
	ASSIGN_DRIVER_FUNC(GetVertexBufferData, name) \
	ASSIGN_DRIVER_FUNC(AddDisposeIndexBuffer, name) \
	ASSIGN_DRIVER_FUNC(SetIndexBufferData, name) \
	ASSIGN_DRIVER_FUNC(GetIndexBufferData, name) \
	ASSIGN_DRIVER_FUNC(CreateEffect, name) \
	ASSIGN_DRIVER_FUNC(CloneEffect, name) \
	ASSIGN_DRIVER_FUNC(AddDisposeEffect, name) \
	ASSIGN_DRIVER_FUNC(SetEffectTechnique, name) \
	ASSIGN_DRIVER_FUNC(ApplyEffect, name) \
	ASSIGN_DRIVER_FUNC(BeginPassRestore, name) \
	ASSIGN_DRIVER_FUNC(EndPassRestore, name) \
	ASSIGN_DRIVER_FUNC(CreateQuery, name) \
	ASSIGN_DRIVER_FUNC(AddDisposeQuery, name) \
	ASSIGN_DRIVER_FUNC(QueryBegin, name) \
	ASSIGN_DRIVER_FUNC(QueryEnd, name) \
	ASSIGN_DRIVER_FUNC(QueryComplete, name) \
	ASSIGN_DRIVER_FUNC(QueryPixelCount, name) \
	ASSIGN_DRIVER_FUNC(SupportsDXT1, name) \
	ASSIGN_DRIVER_FUNC(SupportsS3TC, name) \
	ASSIGN_DRIVER_FUNC(SupportsBC7, name) \
	ASSIGN_DRIVER_FUNC(SupportsHardwareInstancing, name) \
	ASSIGN_DRIVER_FUNC(SupportsNoOverwrite, name) \
	ASSIGN_DRIVER_FUNC(SupportsSRGBRenderTargets, name) \
	ASSIGN_DRIVER_FUNC(GetMaxTextureSlots, name) \
	ASSIGN_DRIVER_FUNC(GetMaxMultiSampleCount, name) \
	ASSIGN_DRIVER_FUNC(SetStringMarker, name) \
	ASSIGN_DRIVER_FUNC(SetTextureName, name) \
	ASSIGN_DRIVER_FUNC(GetSysRenderer, name) \
	ASSIGN_DRIVER_FUNC(CreateSysTexture, name)

typedef struct FNA3D_Driver
{
	const char *Name;
	uint8_t (*PrepareWindowAttributes)(uint32_t *flags);
	FNA3D_Device* (*CreateDevice)(
		FNA3D_PresentationParameters *presentationParameters,
		uint8_t debugMode
	);
} FNA3D_Driver;

FNA3D_SHAREDINTERNAL FNA3D_Driver D3D11Driver;
FNA3D_SHAREDINTERNAL FNA3D_Driver OpenGLDriver;
FNA3D_SHAREDINTERNAL FNA3D_Driver SDLGPUDriver;

#endif /* FNA3D_DRIVER_H */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
