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

#ifndef FNA3D_H
#define FNA3D_H

#ifdef _WIN32
#define FNA3DAPI __declspec(dllexport)
#define FNA3DCALL __cdecl
#else
#define FNA3DAPI
#define FNA3DCALL
#endif

/* -Wpedantic nameless union/struct silencing */
#ifndef FNA3DNAMELESS
#ifdef __GNUC__
#define FNA3DNAMELESS __extension__
#else
#define FNA3DNAMELESS
#endif /* __GNUC__ */
#endif /* FNA3DNAMELESS */

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif /* __cplusplus */

/* Type Declarations */

typedef struct FNA3D_Device FNA3D_Device;
typedef struct FNA3D_Texture FNA3D_Texture;
typedef struct FNA3D_Buffer FNA3D_Buffer;
typedef struct FNA3D_Renderbuffer FNA3D_Renderbuffer;
typedef struct FNA3D_Effect FNA3D_Effect;
typedef struct FNA3D_Query FNA3D_Query;

/* Enumerations, should match XNA 4.0 */

typedef enum FNA3D_PresentInterval
{
	/* For the default presentation interval, we try to use an OS-provided
	 * feature (if available) to sync when meeting the target framerate
	 * while tearing if the program misses vblank.
	 */
	FNA3D_PRESENTINTERVAL_DEFAULT,
	FNA3D_PRESENTINTERVAL_ONE,
	FNA3D_PRESENTINTERVAL_TWO,
	FNA3D_PRESENTINTERVAL_IMMEDIATE
} FNA3D_PresentInterval;

typedef enum FNA3D_DisplayOrientation
{
	FNA3D_DISPLAYORIENTATION_DEFAULT,
	FNA3D_DISPLAYORIENTATION_LANDSCAPELEFT,
	FNA3D_DISPLAYORIENTATION_LANDSCAPERIGHT,
	FNA3D_DISPLAYORIENTATION_PORTRAIT
} FNA3D_DisplayOrientation;

typedef enum FNA3D_RenderTargetUsage
{
	FNA3D_RENDERTARGETUSAGE_DISCARDCONTENTS,
	FNA3D_RENDERTARGETUSAGE_PRESERVECONTENTS,
	FNA3D_RENDERTARGETUSAGE_PLATFORMCONTENTS
} FNA3D_RenderTargetUsage;

typedef enum FNA3D_ClearOptions
{
	FNA3D_CLEAROPTIONS_TARGET	= 1,
	FNA3D_CLEAROPTIONS_DEPTHBUFFER	= 2,
	FNA3D_CLEAROPTIONS_STENCIL	= 4
} FNA3D_ClearOptions;

typedef enum FNA3D_PrimitiveType
{
	FNA3D_PRIMITIVETYPE_TRIANGLELIST,
	FNA3D_PRIMITIVETYPE_TRIANGLESTRIP,
	FNA3D_PRIMITIVETYPE_LINELIST,
	FNA3D_PRIMITIVETYPE_LINESTRIP,
	FNA3D_PRIMITIVETYPE_POINTLIST_EXT
} FNA3D_PrimitiveType;

typedef enum FNA3D_IndexElementSize
{
	FNA3D_INDEXELEMENTSIZE_16BIT,
	FNA3D_INDEXELEMENTSIZE_32BIT
} FNA3D_IndexElementSize;

typedef enum FNA3D_SurfaceFormat
{
	FNA3D_SURFACEFORMAT_COLOR,
	FNA3D_SURFACEFORMAT_BGR565,
	FNA3D_SURFACEFORMAT_BGRA5551,
	FNA3D_SURFACEFORMAT_BGRA4444,
	FNA3D_SURFACEFORMAT_DXT1,
	FNA3D_SURFACEFORMAT_DXT3,
	FNA3D_SURFACEFORMAT_DXT5,
	FNA3D_SURFACEFORMAT_NORMALIZEDBYTE2,
	FNA3D_SURFACEFORMAT_NORMALIZEDBYTE4,
	FNA3D_SURFACEFORMAT_RGBA1010102,
	FNA3D_SURFACEFORMAT_RG32,
	FNA3D_SURFACEFORMAT_RGBA64,
	FNA3D_SURFACEFORMAT_ALPHA8,
	FNA3D_SURFACEFORMAT_SINGLE,
	FNA3D_SURFACEFORMAT_VECTOR2,
	FNA3D_SURFACEFORMAT_VECTOR4,
	FNA3D_SURFACEFORMAT_HALFSINGLE,
	FNA3D_SURFACEFORMAT_HALFVECTOR2,
	FNA3D_SURFACEFORMAT_HALFVECTOR4,
	FNA3D_SURFACEFORMAT_HDRBLENDABLE,
	FNA3D_SURFACEFORMAT_COLORBGRA_EXT
} FNA3D_SurfaceFormat;

typedef enum FNA3D_DepthFormat
{
	FNA3D_DEPTHFORMAT_NONE,
	FNA3D_DEPTHFORMAT_D16,
	FNA3D_DEPTHFORMAT_D24,
	FNA3D_DEPTHFORMAT_D24S8
} FNA3D_DepthFormat;

typedef enum FNA3D_CubeMapFace
{
	FNA3D_CUBEMAPFACE_POSITIVEX,
	FNA3D_CUBEMAPFACE_NEGATIVEX,
	FNA3D_CUBEMAPFACE_POSITIVEY,
	FNA3D_CUBEMAPFACE_NEGATIVEY,
	FNA3D_CUBEMAPFACE_POSITIVEZ,
	FNA3D_CUBEMAPFACE_NEGATIVEZ
} FNA3D_CubeMapFace;

typedef enum FNA3D_BufferUsage
{
	FNA3D_BUFFERUSAGE_NONE,
	FNA3D_BUFFERUSAGE_WRITEONLY
} FNA3D_BufferUsage;

typedef enum FNA3D_SetDataOptions
{
	FNA3D_SETDATAOPTIONS_NONE,
	FNA3D_SETDATAOPTIONS_DISCARD,
	FNA3D_SETDATAOPTIONS_NOOVERWRITE
} FNA3D_SetDataOptions;

typedef enum FNA3D_Blend
{
	FNA3D_BLEND_ONE,
	FNA3D_BLEND_ZERO,
	FNA3D_BLEND_SOURCECOLOR,
	FNA3D_BLEND_INVERSESOURCECOLOR,
	FNA3D_BLEND_SOURCEALPHA,
	FNA3D_BLEND_INVERSESOURCEALPHA,
	FNA3D_BLEND_DESTINATIONCOLOR,
	FNA3D_BLEND_INVERSEDESTINATIONCOLOR,
	FNA3D_BLEND_DESTINATIONALPHA,
	FNA3D_BLEND_INVERSEDESTINATIONALPHA,
	FNA3D_BLEND_BLENDFACTOR,
	FNA3D_BLEND_INVERSEBLENDFACTOR,
	FNA3D_BLEND_SOURCEALPHASATURATION
} FNA3D_Blend;

typedef enum FNA3D_BlendFunction
{
	FNA3D_BLENDFUNCTION_ADD,
	FNA3D_BLENDFUNCTION_SUBTRACT,
	FNA3D_BLENDFUNCTION_REVERSESUBTRACT,
	FNA3D_BLENDFUNCTION_MAX,
	FNA3D_BLENDFUNCTION_MIN
} FNA3D_BlendFunction;

typedef enum FNA3D_ColorWriteChannels
{
	FNA3D_COLORWRITECHANNELS_NONE	= 0,
	FNA3D_COLORWRITECHANNELS_RED	= 1,
	FNA3D_COLORWRITECHANNELS_GREEN	= 2,
	FNA3D_COLORWRITECHANNELS_BLUE	= 4,
	FNA3D_COLORWRITECHANNELS_ALPHA	= 8,
	FNA3D_COLORWRITECHANNELS_ALL	= 15
} FNA3D_ColorWriteChannels;

typedef enum FNA3D_StencilOperation
{
	FNA3D_STENCILOPERATION_KEEP,
	FNA3D_STENCILOPERATION_ZERO,
	FNA3D_STENCILOPERATION_REPLACE,
	FNA3D_STENCILOPERATION_INCREMENT,
	FNA3D_STENCILOPERATION_DECREMENT,
	FNA3D_STENCILOPERATION_INCREMENTSATURATION,
	FNA3D_STENCILOPERATION_DECREMENTSATURATION,
	FNA3D_STENCILOPERATION_INVERT
} FNA3D_StencilOperation;

typedef enum FNA3D_CompareFunction
{
	FNA3D_COMPAREFUNCTION_ALWAYS,
	FNA3D_COMPAREFUNCTION_NEVER,
	FNA3D_COMPAREFUNCTION_LESS,
	FNA3D_COMPAREFUNCTION_LESSEQUAL,
	FNA3D_COMPAREFUNCTION_EQUAL,
	FNA3D_COMPAREFUNCTION_GREATEREQUAL,
	FNA3D_COMPAREFUNCTION_GREATER,
	FNA3D_COMPAREFUNCTION_NOTEQUAL
} FNA3D_CompareFunction;

typedef enum FNA3D_CullMode
{
	FNA3D_CULLMODE_NONE,
	FNA3D_CULLMODE_CULLCLOCKWISEFACE,
	FNA3D_CULLMODE_CULLCOUNTERCLOCKWISEFACE
} FNA3D_CullMode;

typedef enum FNA3D_FillMode
{
	FNA3D_FILLMODE_SOLID,
	FNA3D_FILLMODE_WIREFRAME
} FNA3D_FillMode;

typedef enum FNA3D_TextureAddressMode
{
	FNA3D_TEXTUREADDRESSMODE_WRAP,
	FNA3D_TEXTUREADDRESSMODE_CLAMP,
	FNA3D_TEXTUREADDRESSMODE_MIRROR
} FNA3D_TextureAddressMode;

typedef enum FNA3D_TextureFilter
{
	FNA3D_TEXTUREFILTER_LINEAR,
	FNA3D_TEXTUREFILTER_POINT,
	FNA3D_TEXTUREFILTER_ANISOTROPIC,
	FNA3D_TEXTUREFILTER_LINEAR_MIPPOINT,
	FNA3D_TEXTUREFILTER_POINT_MIPLINEAR,
	FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPLINEAR,
	FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPPOINT,
	FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPLINEAR,
	FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPPOINT
} FNA3D_TextureFilter;

typedef enum FNA3D_VertexElementFormat
{
	FNA3D_VERTEXELEMENTFORMAT_SINGLE,
	FNA3D_VERTEXELEMENTFORMAT_VECTOR2,
	FNA3D_VERTEXELEMENTFORMAT_VECTOR3,
	FNA3D_VERTEXELEMENTFORMAT_VECTOR4,
	FNA3D_VERTEXELEMENTFORMAT_COLOR,
	FNA3D_VERTEXELEMENTFORMAT_BYTE4,
	FNA3D_VERTEXELEMENTFORMAT_SHORT2,
	FNA3D_VERTEXELEMENTFORMAT_SHORT4,
	FNA3D_VERTEXELEMENTFORMAT_NORMALIZEDSHORT2,
	FNA3D_VERTEXELEMENTFORMAT_NORMALIZEDSHORT4,
	FNA3D_VERTEXELEMENTFORMAT_HALFVECTOR2,
	FNA3D_VERTEXELEMENTFORMAT_HALFVECTOR4
} FNA3D_VertexElementFormat;

typedef enum FNA3D_VertexElementUsage
{
	FNA3D_VERTEXELEMENTUSAGE_POSITION,
	FNA3D_VERTEXELEMENTUSAGE_COLOR,
	FNA3D_VERTEXELEMENTUSAGE_TEXTURECOORDINATE,
	FNA3D_VERTEXELEMENTUSAGE_NORMAL,
	FNA3D_VERTEXELEMENTUSAGE_BINORMAL,
	FNA3D_VERTEXELEMENTUSAGE_TANGENT,
	FNA3D_VERTEXELEMENTUSAGE_BLENDINDICES,
	FNA3D_VERTEXELEMENTUSAGE_BLENDWEIGHT,
	FNA3D_VERTEXELEMENTUSAGE_DEPTH,
	FNA3D_VERTEXELEMENTUSAGE_FOG,
	FNA3D_VERTEXELEMENTUSAGE_POINTSIZE,
	FNA3D_VERTEXELEMENTUSAGE_SAMPLE,
	FNA3D_VERTEXELEMENTUSAGE_TESSELATEFACTOR
} FNA3D_VertexElementUsage;

/* Structures, should match XNA 4.0 */

typedef struct FNA3D_Color
{
	uint8_t r;
	uint8_t g;
	uint8_t b;
	uint8_t a;
} FNA3D_Color;

typedef struct FNA3D_Rect
{
	int32_t x;
	int32_t y;
	int32_t w;
	int32_t h;
} FNA3D_Rect;

typedef struct FNA3D_Vec4
{
	float x;
	float y;
	float z;
	float w;
} FNA3D_Vec4;

typedef struct FNA3D_Viewport
{
	int32_t x;
	int32_t y;
	int32_t w;
	int32_t h;
	float minDepth;
	float maxDepth;
} FNA3D_Viewport;

typedef struct FNA3D_PresentationParameters
{
	int32_t backBufferWidth;
	int32_t backBufferHeight;
	FNA3D_SurfaceFormat backBufferFormat;
	int32_t multiSampleCount;
	void* deviceWindowHandle;
	uint8_t isFullScreen;
	FNA3D_DepthFormat depthStencilFormat;
	FNA3D_PresentInterval presentationInterval;
	FNA3D_DisplayOrientation displayOrientation;
	FNA3D_RenderTargetUsage renderTargetUsage;
} FNA3D_PresentationParameters;

typedef struct FNA3D_BlendState
{
	FNA3D_Blend colorSourceBlend;
	FNA3D_Blend colorDestinationBlend;
	FNA3D_BlendFunction colorBlendFunction;
	FNA3D_Blend alphaSourceBlend;
	FNA3D_Blend alphaDestinationBlend;
	FNA3D_BlendFunction alphaBlendFunction;
	FNA3D_ColorWriteChannels colorWriteEnable;
	FNA3D_ColorWriteChannels colorWriteEnable1;
	FNA3D_ColorWriteChannels colorWriteEnable2;
	FNA3D_ColorWriteChannels colorWriteEnable3;
	FNA3D_Color blendFactor;
	int32_t multiSampleMask;
} FNA3D_BlendState;

typedef struct FNA3D_DepthStencilState
{
	uint8_t depthBufferEnable;
	uint8_t depthBufferWriteEnable;
	FNA3D_CompareFunction depthBufferFunction;
	uint8_t stencilEnable;
	int32_t stencilMask;
	int32_t stencilWriteMask;
	uint8_t twoSidedStencilMode;
	FNA3D_StencilOperation stencilFail;
	FNA3D_StencilOperation stencilDepthBufferFail;
	FNA3D_StencilOperation stencilPass;
	FNA3D_CompareFunction stencilFunction;
	FNA3D_StencilOperation ccwStencilFail;
	FNA3D_StencilOperation ccwStencilDepthBufferFail;
	FNA3D_StencilOperation ccwStencilPass;
	FNA3D_CompareFunction ccwStencilFunction;
	int32_t referenceStencil;
} FNA3D_DepthStencilState;

typedef struct FNA3D_RasterizerState
{
	FNA3D_FillMode fillMode;
	FNA3D_CullMode cullMode;
	float depthBias;
	float slopeScaleDepthBias;
	uint8_t scissorTestEnable;
	uint8_t multiSampleAntiAlias;
} FNA3D_RasterizerState;

typedef struct FNA3D_SamplerState
{
	FNA3D_TextureFilter filter;
	FNA3D_TextureAddressMode addressU;
	FNA3D_TextureAddressMode addressV;
	FNA3D_TextureAddressMode addressW;
	float mipMapLevelOfDetailBias;
	int32_t maxAnisotropy;
	int32_t maxMipLevel;
} FNA3D_SamplerState;

typedef struct FNA3D_VertexElement
{
	int32_t offset;
	FNA3D_VertexElementFormat vertexElementFormat;
	FNA3D_VertexElementUsage vertexElementUsage;
	int32_t usageIndex;
} FNA3D_VertexElement;

typedef struct FNA3D_VertexDeclaration
{
	int32_t vertexStride;
	int32_t elementCount;
	FNA3D_VertexElement *elements;
} FNA3D_VertexDeclaration;

typedef struct FNA3D_VertexBufferBinding
{
	FNA3D_Buffer *vertexBuffer;
	FNA3D_VertexDeclaration vertexDeclaration;
	int32_t vertexOffset;
	int32_t instanceFrequency;
} FNA3D_VertexBufferBinding;

typedef struct FNA3D_RenderTargetBinding
{
	/* Basic target information */
	#define FNA3D_RENDERTARGET_TYPE_2D 0
	#define FNA3D_RENDERTARGET_TYPE_CUBE 1
	uint8_t type;
	FNA3DNAMELESS union
	{
		struct
		{
			int32_t width;
			int32_t height;
		} twod;
		struct
		{
			int32_t size;
			FNA3D_CubeMapFace face;
		} cube;
	};

	/* If this is >1, you MUST call ResolveTarget after rendering! */
	int32_t levelCount;

	/* If this is >1, colorBuffer MUST be non-NULL! */
	int32_t multiSampleCount;

	/* Destination texture. This MUST be non-NULL! */
	FNA3D_Texture *texture;

	/* If this is non-NULL, you MUST call ResolveTarget after rendering! */
	FNA3D_Renderbuffer *colorBuffer;
} FNA3D_RenderTargetBinding;

/* Version API */

#define FNA3D_ABI_VERSION	 0
#define FNA3D_MAJOR_VERSION	21
#define FNA3D_MINOR_VERSION	 6
#define FNA3D_PATCH_VERSION	 0

#define FNA3D_COMPILED_VERSION ( \
	(FNA3D_ABI_VERSION * 100 * 100 * 100) + \
	(FNA3D_MAJOR_VERSION * 100 * 100) + \
	(FNA3D_MINOR_VERSION * 100) + \
	(FNA3D_PATCH_VERSION) \
)

FNA3DAPI uint32_t FNA3D_LinkedVersion(void);

/* Functions */

/* Logging */

typedef void (FNA3DCALL * FNA3D_LogFunc)(const char *msg);

/* Reroutes FNA3D's logging to custom logging functions.
 *
 * info:	Basic logs that might be useful to have stored for support.
 * warn:	Something went wrong, but it's really just annoying, not fatal.
 * error:	You better have this stored somewhere because it's crashing now!
 */
FNA3DAPI void FNA3D_HookLogFunctions(
	FNA3D_LogFunc info,
	FNA3D_LogFunc warn,
	FNA3D_LogFunc error
);

/* Init/Quit */

/* Selects the most suitable graphics rendering backend for the system, then
 * provides the application with context-sensitive bitflags for the OS window.
 *
 * Returns a bitflag value, typically SDL_WindowFlags masks.
 */
FNA3DAPI uint32_t FNA3D_PrepareWindowAttributes(void);

/* After your window is created, call this to check for high-DPI support.
 *
 * window:	The OS window handle, typically an SDL_Window*.
 * w:		Filled with the width of the window's drawable canvas.
 * h:		Filled with the height of the window's drawable canvas.
 */
FNA3DAPI void FNA3D_GetDrawableSize(void* window, int32_t *w, int32_t *h);

/* Creates a rendering context for use on the calling thread.
 *
 * presentationParameters:	The initial device/backbuffer settings.
 * debugMode:			Enable debugging and backend validation features
 *				at the cost of reduced overall performance.
 *
 * Returns a device ready for use. Be sure to only call device functions from
 * the thread that it was created on!
 */
FNA3DAPI FNA3D_Device* FNA3D_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
);

/* Destroys a rendering context previously returned by FNA3D_CreateDevice. */
FNA3DAPI void FNA3D_DestroyDevice(FNA3D_Device *device);

/* Presentation */

/* Presents the backbuffer to the window.
 *
 * sourceRectangle:		The region of the buffer to present (or NULL).
 * destinationRectangle:	The region of the window to update (or NULL).
 * overrideWindowHandle:	The OS window handle (not really "overridden").
 */
FNA3DAPI void FNA3D_SwapBuffers(
	FNA3D_Device *device,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
);

/* Drawing */

/* Clears the active draw buffers of any previous contents.
 *
 * options:	Bitflags to specify color/depth/stencil buffers for clearing.
 * color:	The new value of the cleared color buffer. It is STRONGLY
 *		recommended to use 0.0f and 1.0f for all color channels!
 * depth:	The new value of the cleared depth buffer.
 * stencil:	The new value of the cleared stencil buffer.
 */
FNA3DAPI void FNA3D_Clear(
	FNA3D_Device *device,
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
);

/* Draws data from vertex/index buffers.
 *
 * primitiveType:	The primitive topology of the vertex data.
 * baseVertex:		The starting offset to read from the vertex buffer.
 * minVertexIndex:	The lowest index value expected from the index buffer.
 * numVertices:		The highest offset expected from the index buffer.
 * startIndex:		The starting offset to read from the index buffer.
 * primitiveCount:	The number of primitives to draw.
 * indices:		The index buffer to bind for this draw call.
 * indexElementSize:	The size of the index type for this index buffer.
 */
FNA3DAPI void FNA3D_DrawIndexedPrimitives(
	FNA3D_Device *device,
	FNA3D_PrimitiveType primitiveType,
	int32_t baseVertex,
	int32_t minVertexIndex,
	int32_t numVertices,
	int32_t startIndex,
	int32_t primitiveCount,
	FNA3D_Buffer *indices,
	FNA3D_IndexElementSize indexElementSize
);

/* Draws data from vertex/index buffers with instancing enabled.
 *
 * primitiveType:	The primitive topology of the vertex data.
 * baseVertex:		The starting offset to read from the vertex buffer.
 * minVertexIndex:	The lowest index value expected from the index buffer.
 * numVertices:		The highest offset expected from the index buffer.
 * startIndex:		The starting offset to read from the index buffer.
 * primitiveCount:	The number of primitives to draw.
 * instanceCount:	The number of instances that will be drawn.
 * indices:		The index buffer to bind for this draw call.
 * indexElementSize:	The size of the index type for this index buffer.
 */
FNA3DAPI void FNA3D_DrawInstancedPrimitives(
	FNA3D_Device *device,
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

/* Draws data from vertex buffers.
 * primitiveType:	The primitive topology of the vertex data.
 * vertexStart:		The starting offset to read from the vertex buffer.
 * primitiveCount:	The number of primitives to draw.
 */
FNA3DAPI void FNA3D_DrawPrimitives(
	FNA3D_Device *device,
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
);

/* Mutable Render States */

/* Sets the view dimensions for rendering, relative to the active render target.
 * It is required to call this at least once after calling SetRenderTargets, as
 * the renderer may need to adjust these dimensions to fit the backend's
 * potentially goofy coordinate systems.
 *
 * viewport: The new view dimensions for future draw calls.
 */
FNA3DAPI void FNA3D_SetViewport(FNA3D_Device *device, FNA3D_Viewport *viewport);

/* Sets the scissor box for rendering, relative to the active render target.
 * It is required to call this at least once after calling SetRenderTargets, as
 * the renderer may need to adjust these dimensions to fit the backend's
 * potentially goofy coordinate systems.
 *
 * scissor: The new scissor box for future draw calls.
 */
FNA3DAPI void FNA3D_SetScissorRect(FNA3D_Device *device, FNA3D_Rect *scissor);

/* Gets the blending factor used for current draw calls.
 *
 * blendFactor: Filled with color being used as the device blend factor.
 */
FNA3DAPI void FNA3D_GetBlendFactor(
	FNA3D_Device *device,
	FNA3D_Color *blendFactor
);

/* Sets the blending factor used for future draw calls.
 *
 * blendFactor: The color to use as the device blend factor.
 */
FNA3DAPI void FNA3D_SetBlendFactor(
	FNA3D_Device *device,
	FNA3D_Color *blendFactor
);

/* Gets the mask from which multisample fragment data is sampled from.
 *
 * Returns the coverage mask used to determine sample locations.
 */
FNA3DAPI int32_t FNA3D_GetMultiSampleMask(FNA3D_Device *device);

/* Sets the mask with which multisample fragment data will be sampled from.
 *
 * mask: The new coverage mask to use for determining sample locations.
 */
FNA3DAPI void FNA3D_SetMultiSampleMask(FNA3D_Device *device, int32_t mask);

/* Gets the reference value used for certain types of stencil testing.
 *
 * Returns the stencil reference value.
 */
FNA3DAPI int32_t FNA3D_GetReferenceStencil(FNA3D_Device *device);

/* Sets the reference value used for certain types of stencil testing.
 *
 * ref: The new stencil reference value.
 */
FNA3DAPI void FNA3D_SetReferenceStencil(FNA3D_Device *device, int32_t ref);

/* Immutable Render States */

/* Applies a blending state to use for future draw calls. This only needs to be
 * called when the state actually changes. Redundant calls may negatively affect
 * performance!
 *
 * blendState: The new parameters to use for color blending.
 */
FNA3DAPI void FNA3D_SetBlendState(
	FNA3D_Device *device,
	FNA3D_BlendState *blendState
);

/* Applies depth/stencil states to use for future draw calls. This only needs to
 * be called when the states actually change. Redundant calls may negatively
 * affect performance!
 *
 * depthStencilState: The new parameters to use for depth/stencil work.
 */
FNA3DAPI void FNA3D_SetDepthStencilState(
	FNA3D_Device *device,
	FNA3D_DepthStencilState *depthStencilState
);

/* Applies the rasterizing state to use for future draw calls.
 * It's generally a good idea to call this for each draw call, but if you really
 * wanted to you could try reducing it to when the state changes and when the
 * render target state changes.
 *
 * rasterizerState: The new parameters to use for rasterization work.
 */
FNA3DAPI void FNA3D_ApplyRasterizerState(
	FNA3D_Device *device,
	FNA3D_RasterizerState *rasterizerState
);

/* Updates a sampler slot with new texture/sampler data for future draw calls.
 * This should only be called on slots that have modified texture/sampler state.
 * Redundant calls may negatively affect performance!
 *
 * index:	The sampler slot to update.
 * texture:	The texture bound to this sampler.
 * sampler:	The new parameters to use for this slot's texture sampling.
 */
FNA3DAPI void FNA3D_VerifySampler(
	FNA3D_Device *device,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
);

/* Updates a vertex sampler slot with new texture/sampler data for future draw
 * calls. This should only be called on slots that have modified texture/sampler
 * state. Redundant calls may negatively affect performance!
 *
 * index:	The vertex sampler slot to update.
 * texture:	The texture bound to this sampler.
 * sampler:	The new parameters to use for this slot's texture sampling.
 */
FNA3DAPI void FNA3D_VerifyVertexSampler(
	FNA3D_Device *device,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
);

/* Updates the vertex attribute state to read from a set of vertex buffers. This
 * should be the very last thing you call before making a draw call, as this
 * does all the final prep work for the shader program before it's ready to use.
 *
 * bindings:		The vertex buffers and their attribute data.
 * numBindings:		The number of elements in the bindings array.
 * bindingsUpdated:	If the bindings array hasn't changed since the last
 *			update, this can be false. We'll only update the shader
 *			state, updating vertex attribute data only if we 100%
 *			have to, for a tiny performance improvement.
 * baseVertex:		This should be the same as the `baseVertex` parameter
 *			from your Draw*Primitives call, if applicable. Not every
 *			rendering backend has native base vertex support, so we
 *			work around it by passing this here.
 */
FNA3DAPI void FNA3D_ApplyVertexBufferBindings(
	FNA3D_Device *device,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
);

/* Render Targets */

/* Sets the color/depth/stencil buffers to write future draw calls to.
 *
 * renderTargets:	The targets to write to, or NULL for the backbuffer.
 * numRenderTargets:	The size of the renderTargets array (can be 0).
 * depthStencilBuffer:	The depth/stencil renderbuffer (can be NULL).
 * depthFormat:		The format of the depth/stencil renderbuffer.
 * preserveTargetContents:
 * 			Set this to 1 to store the color/depth/stencil contents
 * 			for future use. Most of the time you'll want to
 * 			keep this at 0 to not waste GPU bandwidth.
 */
FNA3DAPI void FNA3D_SetRenderTargets(
	FNA3D_Device *device,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
);

/* After unsetting a render target, call this to resolve multisample targets or
 * generate mipmap data for the final texture.
 *
 * target: The render target to resolve once rendering is complete.
 */
FNA3DAPI void FNA3D_ResolveTarget(
	FNA3D_Device *device,
	FNA3D_RenderTargetBinding *target
);

/* Backbuffer Functions */

/* After modifying the OS window state, call this to reset the backbuffer to
 * match your window changes.
 *
 * presentationParameters: The new settings for the backbuffer.
 */
FNA3DAPI void FNA3D_ResetBackbuffer(
	FNA3D_Device *device,
	FNA3D_PresentationParameters *presentationParameters
);

/* Read the backbuffer's contents directly into client memory. This function is
 * basically one giant CPU/GPU sync point, do NOT ever call this during any
 * performance-critical situation! Just use it for screenshots.
 *
 * x:		The x offset of the backbuffer region to read.
 * y:		The y offset of the backbuffer region to read.
 * w:		The width of the backbuffer region to read.
 * h:		The height of the backbuffer region to read.
 * data:	The pointer to read the backbuffer data into.
 * dataLength:	The size of the backbuffer data in bytes.
 */
FNA3DAPI void FNA3D_ReadBackbuffer(
	FNA3D_Device *device,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	void* data,
	int32_t dataLength
);

/* Gets the current dimensions of the backbuffer.
 *
 * w:	Filled with the backbuffer's width.
 * h:	Filled with the backbuffer's height.
 */
FNA3DAPI void FNA3D_GetBackbufferSize(
	FNA3D_Device *device,
	int32_t *w,
	int32_t *h
);

/* Gets the current pixel format of the backbuffer.
 *
 * Returns the backbuffer's pixel format.
 */
FNA3DAPI FNA3D_SurfaceFormat FNA3D_GetBackbufferSurfaceFormat(
	FNA3D_Device *device
);

/* Gets the format of the backbuffer's depth/stencil buffer.
 *
 * Returns the backbuffer's depth/stencil format.
 */
FNA3DAPI FNA3D_DepthFormat FNA3D_GetBackbufferDepthFormat(FNA3D_Device *device);

/* Gets the multisample sample count of the backbuffer.
 *
 * Returns the backbuffer's multisample sample count.
 */
FNA3DAPI int32_t FNA3D_GetBackbufferMultiSampleCount(FNA3D_Device *device);

/* Textures */

/* Creates a 2D texture to be applied to VerifySampler.
 *
 * format:		The pixel format of the texture data.
 * width:		The width of the texture image.
 * height:		The height of the texture image.
 * levelCount:		The number of mipmap levels to allocate.
 * isRenderTarget:	Set this to 1 when using this with SetRenderTargets.
 *
 * Returns an allocated FNA3D_Texture* object. Note that the contents of the
 * texture are undefined, so you must call SetData at least once before drawing!
 */
FNA3DAPI FNA3D_Texture* FNA3D_CreateTexture2D(
	FNA3D_Device *device,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget
);

/* Creates a 3D texture to be applied to VerifySampler.
 *
 * format:		The pixel format of the texture data.
 * width:		The width of the texture image.
 * height:		The height of the texture image.
 * depth:		The depth of the texture image.
 * levelCount:		The number of mipmap levels to allocate.
 *
 * Returns an allocated FNA3D_Texture* object. Note that the contents of the
 * texture are undefined, so you must call SetData at least once before drawing!
 */
FNA3DAPI FNA3D_Texture* FNA3D_CreateTexture3D(
	FNA3D_Device *device,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount
);

/* Creates a texture cube to be applied to VerifySampler.
 *
 * format:		The pixel format of the texture data.
 * size:		The length of a single edge of the texture cube.
 * levelCount:		The number of mipmap levels to allocate.
 * isRenderTarget:	Set this to 1 when using this with SetRenderTargets.
 *
 * Returns an allocated FNA3D_Texture* object. Note that the contents of the
 * texture are undefined, so you must call SetData at least once before drawing!
 */
FNA3DAPI FNA3D_Texture* FNA3D_CreateTextureCube(
	FNA3D_Device *device,
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget
);

/* Sends a texture to be destroyed by the renderer. Note that we call it
 * "AddDispose" because it may not be immediately destroyed by the renderer if
 * this is not called from the main thread (for example, if a garbage collector
 * deletes the resource instead of the programmer).
 *
 * texture: The FNA3D_Texture to be destroyed.
 */
FNA3DAPI void FNA3D_AddDisposeTexture(
	FNA3D_Device *device,
	FNA3D_Texture *texture
);

/* Uploads image data to a 2D texture object.
 *
 * texture:	The texture to be updated.
 * x:		The x offset of the subregion being updated.
 * y:		The y offset of the subregion being updated.
 * w:		The width of the subregion being updated.
 * h:		The height of the subregion being updated.
 * level:	The mipmap level being updated.
 * data:	A pointer to the image data.
 * dataLength:	The size of the image data in bytes.
 */
FNA3DAPI void FNA3D_SetTextureData2D(
	FNA3D_Device *device,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
);

/* Uploads image data to a 3D texture object.
 *
 * texture:	The texture to be updated.
 * x:		The x offset of the subregion being updated.
 * y:		The y offset of the subregion being updated.
 * z:		The z offset of the subregion being updated.
 * w:		The width of the subregion being updated.
 * h:		The height of the subregion being updated.
 * d:		The depth of the subregion being updated.
 * level:	The mipmap level being updated.
 * data:	A pointer to the image data.
 * dataLength:	The size of the image data in bytes.
 */
FNA3DAPI void FNA3D_SetTextureData3D(
	FNA3D_Device *device,
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

/* Uploads image data to a single face of a texture cube object.
 *
 * texture:	The texture to be updated.
 * x:		The x offset of the subregion being updated.
 * y:		The y offset of the subregion being updated.
 * w:		The width of the subregion being updated.
 * h:		The height of the subregion being updated.
 * cubeMapFace:	The face of the cube being updated.
 * level:	The mipmap level being updated.
 * data:	A pointer to the image data.
 * dataLength:	The size of the image data in bytes.
 */
FNA3DAPI void FNA3D_SetTextureDataCube(
	FNA3D_Device *device,
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

/* Uploads YUV image data to three ALPHA8 texture objects.
 *
 * y:		The texture storing the Y data.
 * u:		The texture storing the U (Cb) data.
 * v:		The texture storing the V (Cr) data.
 * yWidth:	The width of the Y plane.
 * yHeight:	The height of the Y plane.
 * uvWidth:	The width of the U/V planes.
 * uvHeight:	The height of the U/V planes.
 * data:	A pointer to the raw YUV image data.
 * dataLength:	The size of the image data in bytes.
 */
FNA3DAPI void FNA3D_SetTextureDataYUV(
	FNA3D_Device *device,
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

/* Pulls image data from a 2D texture into client memory. Like any GetData,
 * this is generally asking for a massive CPU/GPU sync point, don't call this
 * unless there's absolutely no other way to use the image data!
 *
 * texture:	The texture object being read.
 * x:		The x offset of the subregion being read.
 * y:		The y offset of the subregion being read.
 * w:		The width of the subregion being read.
 * h:		The height of the subregion being read.
 * level:	The mipmap level being read.
 * data:	The pointer being filled with the image data.
 * dataLength:	The size of the image data in bytes.
 */
FNA3DAPI void FNA3D_GetTextureData2D(
	FNA3D_Device *device,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
);

/* Pulls image data from a 3D texture into client memory. Like any GetData,
 * this is generally asking for a massive CPU/GPU sync point, don't call this
 * unless there's absolutely no other way to use the image data!
 *
 * texture:	The texture object being read.
 * x:		The x offset of the subregion being read.
 * y:		The y offset of the subregion being read.
 * z:		The z offset of the subregion being read.
 * w:		The width of the subregion being read.
 * h:		The height of the subregion being read.
 * d:		The depth of the subregion being read.
 * level:	The mipmap level being read.
 * data:	The pointer being filled with the image data.
 * dataLength:	The size of the image data in bytes.
 */
FNA3DAPI void FNA3D_GetTextureData3D(
	FNA3D_Device *device,
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

/* Pulls image data from a single face of a texture cube object into client
 * memory. Like any GetData, this is generally asking for a massive CPU/GPU sync
 * point, don't call this unless there's absolutely no other way to use the
 * image data!
 *
 * texture:	The texture object being read.
 * x:		The x offset of the subregion being read.
 * y:		The y offset of the subregion being read.
 * w:		The width of the subregion being read.
 * h:		The height of the subregion being read.
 * cubeMapFace:	The face of the cube being read.
 * level:	The mipmap level being read.
 * data:	The pointer being filled with the image data.
 * dataLength:	The size of the image data in bytes.
 */
FNA3DAPI void FNA3D_GetTextureDataCube(
	FNA3D_Device *device,
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

/* Creates a color buffer to be used by SetRenderTargets/ResolveTarget.
 *
 * width:		The width of the color buffer.
 * height:		The height of the color buffer.
 * format:		The pixel format of the color buffer.
 * multiSampleCount:	The MSAA value for the color buffer.
 * texture:		The texture that this buffer will be resolving to.
 *
 * Returns a color FNA3D_Renderbuffer object.
 */
FNA3DAPI FNA3D_Renderbuffer* FNA3D_GenColorRenderbuffer(
	FNA3D_Device *device,
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture
);

/* Creates a depth/stencil buffer to be used by SetRenderTargets.
 *
 * width:		The width of the depth/stencil buffer.
 * height:		The height of the depth/stencil buffer.
 * format:		The storage format of the depth/stencil buffer.
 * multiSampleCount:	The MSAA value for the depth/stencil buffer.
 *
 * Returns a depth/stencil FNA3D_Renderbuffer object.
 */
FNA3DAPI FNA3D_Renderbuffer* FNA3D_GenDepthStencilRenderbuffer(
	FNA3D_Device *device,
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount
);

/* Sends a renderbuffer to be destroyed by the renderer. Note that we call it
 * "AddDispose" because it may not be immediately destroyed by the renderer if
 * this is not called from the main thread (for example, if a garbage collector
 * deletes the resource instead of the programmer).
 *
 * renderbuffer: The FNA3D_Renderbuffer to be destroyed.
 */
FNA3DAPI void FNA3D_AddDisposeRenderbuffer(
	FNA3D_Device *device,
	FNA3D_Renderbuffer *renderbuffer
);

/* Vertex Buffers */

/* Creates a vertex buffer to be used by Draw*Primitives.
 *
 * dynamic:		Set to 1 if this buffer will be updated frequently.
 * usage:		Set to WRITEONLY if you do not intend to call GetData.
 * sizeInBytes:		The length of the vertex buffer.
 *
 * Returns an allocated FNA3D_Buffer* object. Note that the contents of the
 * buffer are undefined, so you must call SetData at least once before drawing!
 */
FNA3DAPI FNA3D_Buffer* FNA3D_GenVertexBuffer(
	FNA3D_Device *device,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
);

/* Sends a vertex buffer to be destroyed by the renderer. Note that we call it
 * "AddDispose" because it may not be immediately destroyed by the renderer if
 * this is not called from the main thread (for example, if a garbage collector
 * deletes the resource instead of the programmer).
 *
 * buffer: The FNA3D_Buffer to be destroyed.
 */
FNA3DAPI void FNA3D_AddDisposeVertexBuffer(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer
);

/* Sets a region of the vertex buffer with client data.
 *
 * buffer:		The vertex buffer to be updated.
 * offsetInBytes:	The starting offset of the buffer to write into.
 * data:		The client data to write into the buffer.
 * elementCount:	The number of elements from the client buffer to write.
 * elementSizeInBytes:	The size of each element in the client buffer.
 * vertexStride:	Try to set this to the same value as elementSizeInBytes.
 *			XNA has this ridiculous thing where if vertexStride is
 *			greater than elementSizeInBytes, it tries to do partial
 *			updates of each vertex with the client data's smaller
 *			elements. It's... just, really bad. Don't try to use it.
 *			You probably just want '1' for both parameters, so that
 *			elementCount can just be the buffer length in bytes.
 * options:		Try not to call NONE if this is a dynamic buffer!
 */
FNA3DAPI void FNA3D_SetVertexBufferData(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
);

/* Pulls data from a region of the vertex buffer into a client pointer.
 *
 * buffer:		The vertex buffer to be read from.
 * offsetInBytes:	The starting offset of the buffer to write into.
 * data:		The client data to write into from the buffer.
 * elementCount:	The number of elements from the client buffer to read.
 * elementSizeInBytes:	The size of each element in the client buffer.
 * vertexStride:	Try to set this to the same value as elementSizeInBytes.
 *			XNA has this ridiculous thing where if vertexStride is
 *			greater than elementSizeInBytes, it tries to do partial
 *			updates of each vertex with the client data's smaller
 *			elements. It's... just, really bad. Don't try to use it.
 *			You probably just want '1' for both parameters, so that
 *			elementCount can just be the buffer length in bytes.
 */
FNA3DAPI void FNA3D_GetVertexBufferData(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
);

/* Index Buffers */

/* Creates an index buffer to be used by Draw*Primitives.
 *
 * dynamic:		Set to 1 if this buffer will be updated frequently.
 * usage:		Set to WRITEONLY if you do not intend to call GetData.
 * sizeInBytes:		The length of the vertex buffer.
 *
 * Returns an allocated FNA3D_Buffer* object. Note that the contents of the
 * buffer are undefined, so you must call SetData at least once before drawing!
 */
FNA3DAPI FNA3D_Buffer* FNA3D_GenIndexBuffer(
	FNA3D_Device *device,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
);

/* Sends an index buffer to be destroyed by the renderer. Note that we call it
 * "AddDispose" because it may not be immediately destroyed by the renderer if
 * this is not called from the main thread (for example, if a garbage collector
 * deletes the resource instead of the programmer).
 *
 * buffer: The FNA3D_Buffer to be destroyed.
 */
FNA3DAPI void FNA3D_AddDisposeIndexBuffer(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer
);

/* Sets a region of the index buffer with client data.
 *
 * buffer:		The index buffer to be updated.
 * offsetInBytes:	The starting offset of the buffer to write into.
 * data:		The client data to write into the buffer.
 * dataLength:		The size (in bytes) of the client data.
 * options:		Try not to call NONE if this is a dynamic buffer!
 */
FNA3DAPI void FNA3D_SetIndexBufferData(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
);

/* Pulls data from a region of the index buffer into a client pointer.
 *
 * buffer:		The index buffer to be read from.
 * offsetInBytes:	The starting offset of the buffer to read from.
 * data:		The pointer to read buffer data into.
 * dataLength:		The size (in bytes) of the client data.
 */
FNA3DAPI void FNA3D_GetIndexBufferData(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength
);

/* Effects */

/* When using this API, be sure to include mojoshader.h! */
#ifndef _INCL_MOJOSHADER_H_
typedef struct MOJOSHADER_effect MOJOSHADER_effect;
typedef struct MOJOSHADER_effectTechnique MOJOSHADER_effectTechnique;
typedef struct MOJOSHADER_effectStateChanges MOJOSHADER_effectStateChanges;
#endif /* _INCL_MOJOSHADER_H_ */

/* Parses and compiles a Direct3D 9 Effects Framework binary.
 *
 * effectCode:		The D3D9 Effect binary blob.
 * effectCodeLength:	The size (in bytes) of the blob.
 * effect:		Filled with the compiled FNA3D_Effect*.
 * effectData:		Filled with the parsed Effect Framework data. This
 *			pointer is valid until the effect is disposed.
 */
FNA3DAPI void FNA3D_CreateEffect(
	FNA3D_Device *device,
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
);

/* Copies a compiled Effect, including its current technique/parameter data.
 *
 * cloneSource:	The FNA3D_Effect to copy.
 * effect:	Filled with the new compiled FNA3D_Effect*.
 * effectData:	Filled with the copied Effect Framework data.
 */
FNA3DAPI void FNA3D_CloneEffect(
	FNA3D_Device *device,
	FNA3D_Effect *cloneSource,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
);

/* Sends an Effect to be destroyed by the renderer. Note that we call it
 * "AddDispose" because it may not be immediately destroyed by the renderer if
 * this is not called from the main thread (for example, if a garbage collector
 * deletes the resource instead of the programmer).
 *
 * effect: The FNA3D_Effect to be destroyed.
 */
FNA3DAPI void FNA3D_AddDisposeEffect(
	FNA3D_Device *device,
	FNA3D_Effect *effect
);

/* Sets the active technique on the Effect.
 *
 * effect:	The Effect to be modified.
 * technique:	The technique to be used by future ApplyEffect calls.
 */
FNA3DAPI void FNA3D_SetEffectTechnique(
	FNA3D_Device *device,
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
);

/* Applies an effect pass from a given Effect, setting the active shader program
 * and committing any parameter data changes to be used by future draw calls.
 *
 * effect:		The Effect to be applied.
 * pass:		The current technique's pass index to be applied.
 * stateChanges:	Structure to be filled with any render state changes
 *			made by the Effect. This must be valid for the entire
 * 			duration that this Effect is being applied.
 */
FNA3DAPI void FNA3D_ApplyEffect(
	FNA3D_Device *device,
	FNA3D_Effect *effect,
	uint32_t pass,
	MOJOSHADER_effectStateChanges *stateChanges
);

/* Applies an effect pass from a given Effect, setting the active shader program
 * and committing and parameter data changes to be used by future draw calls,
 * while also caching the current program object to be stored once this Effect's
 * pass has been completed.
 *
 * effect:		The Effect to be applied.
 * stateChanges:	Structure to be filled with any render state changes
 *			made by the Effect. This must be valid for the entire
 * 			duration that this Effect is being applied.
 */
FNA3DAPI void FNA3D_BeginPassRestore(
	FNA3D_Device *device,
	FNA3D_Effect *effect,
	MOJOSHADER_effectStateChanges *stateChanges
);

/* Ends a pass started by BeginPassRestore, unsetting the current Effect and
 * restoring the previous shader state from before BeginPassRestore was called.
 *
 * effect: The Effect that was applied at BeginPassRestore.
 */
FNA3DAPI void FNA3D_EndPassRestore(
	FNA3D_Device *device,
	FNA3D_Effect *effect
);

/* Queries */

/* Creates an object used to run occlusion queries.
 *
 * Returns an FNA3D_Query object.
 */
FNA3DAPI FNA3D_Query* FNA3D_CreateQuery(FNA3D_Device *device);

/* Sends a query object to be destroyed by the renderer. Note that we call it
 * "AddDispose" because it may not be immediately destroyed by the renderer if
 * this is not called from the main thread (for example, if a garbage collector
 * deletes the resource instead of the programmer).
 *
 * query: The FNA3D_Query to be destroyed.
 */
FNA3DAPI void FNA3D_AddDisposeQuery(FNA3D_Device *device, FNA3D_Query *query);

/* Marks the start of when a query object should count pixels written.
 *
 * query: The FNA3D_Query to start.
 */
FNA3DAPI void FNA3D_QueryBegin(FNA3D_Device *device, FNA3D_Query *query);

/* Marks the end of when a query object should count pixels written. Note that
 * this does NOT mean the query has finished executing, you will need to poll
 * QueryComplete before checking the pixel count.
 *
 * query: The FNA3D_Query to stop.
 */
FNA3DAPI void FNA3D_QueryEnd(FNA3D_Device *device, FNA3D_Query *query);

/* Call this until the function returns 1 to safely query for pixel counts.
 *
 * query: The FNA3D_Query to sync with.
 *
 * Returns 1 when complete, 0 when still in execution.
 */
FNA3DAPI uint8_t FNA3D_QueryComplete(FNA3D_Device *device, FNA3D_Query *query);

/* Query the pixels counted between the begin/end markers set for the object.
 *
 * query: The FNA3D_Query to poll for pixel count
 *
 * Returns the pixels written during the begin/end period.
 */
FNA3DAPI int32_t FNA3D_QueryPixelCount(
	FNA3D_Device *device,
	FNA3D_Query *query
);

/* Feature Queries */

/* Returns 1 if the renderer natively supports DXT1 texture data. */
FNA3DAPI uint8_t FNA3D_SupportsDXT1(FNA3D_Device *device);

/* Returns 1 if the renderer natively supports S3TC texture data. */
FNA3DAPI uint8_t FNA3D_SupportsS3TC(FNA3D_Device *device);

/* Returns 1 if the renderer natively supports hardware instancing. */
FNA3DAPI uint8_t FNA3D_SupportsHardwareInstancing(FNA3D_Device *device);

/* Returns 1 if the renderer natively supports asynchronous buffer writing. */
FNA3DAPI uint8_t FNA3D_SupportsNoOverwrite(FNA3D_Device *device);

/* Returns the number of sampler slots supported by the renderer. */
FNA3DAPI void FNA3D_GetMaxTextureSlots(
	FNA3D_Device *device,
	int32_t *textures,
	int32_t *vertexTextures
);

/* Returns the highest multisample count supported for anti-aliasing.
 *
 * format:		The pixel format to query for MSAA support.
 * multiSampleCount:	The max MSAA value requested for this format.
 *
 * Returns a hardware-specific version of min(preferred, possible).
 */
FNA3DAPI int32_t FNA3D_GetMaxMultiSampleCount(
	FNA3D_Device *device,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount
);

/* Debugging */

/* Sets an arbitrary string constant to be stored in a rendering API trace,
 * useful for labeling call streams for debugging purposes.
 *
 * text: The string constant to mark in the API call stream.
 */
FNA3DAPI void FNA3D_SetStringMarker(FNA3D_Device *device, const char *text);

#ifdef __cplusplus
}
#endif /* __cplusplus */

#endif /* FNA3D_H */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
