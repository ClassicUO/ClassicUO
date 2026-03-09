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

#ifndef FNA3D_DRIVER_METAL_H
#define FNA3D_DRIVER_METAL_H

#include "FNA3D_Driver.h"

#include <SDL.h>
#define OBJC_OLD_DISPATCH_PROTOTYPES 1
#include <objc/message.h>

/* Metal Enums */

typedef enum MTLLoadAction
{
	MTLLoadActionDontCare,
	MTLLoadActionLoad,
	MTLLoadActionClear
} MTLLoadAction;

typedef enum MTLStoreAction
{
	MTLStoreActionDontCare,
	MTLStoreActionStore,
	MTLStoreActionMultisampleResolve
} MTLStoreAction;

typedef enum MTLPrimitiveType
{
	MTLPrimitiveTypePoint,
	MTLPrimitiveTypeLine,
	MTLPrimitiveTypeLineStrip,
	MTLPrimitiveTypeTriangle,
	MTLPrimitiveTypeTriangleStrip
} MTLPrimitiveType;

typedef enum MTLIndexType
{
	MTLIndexTypeUInt16,
	MTLIndexTypeUInt32
} MTLIndexType;

typedef enum MTLPixelFormat
{
	MTLPixelFormatInvalid			= 0,
	MTLPixelFormatA8Unorm			= 1,
	MTLPixelFormatR16Float     		= 25,
	MTLPixelFormatRG8Snorm			= 32,
	MTLPixelFormatB5G6R5Unorm 		= 40,
	MTLPixelFormatABGR4Unorm		= 42,
	MTLPixelFormatBGR5A1Unorm		= 43,
	MTLPixelFormatR32Float			= 55,
	MTLPixelFormatRG16Unorm			= 60,
	MTLPixelFormatRG16Snorm			= 62,
	MTLPixelFormatRG16Float			= 65,
	MTLPixelFormatRGBA8Unorm		= 70,
	MTLPixelFormatBGRA8Unorm		= 80,
	MTLPixelFormatRGB10A2Unorm		= 90,
	MTLPixelFormatRG32Float			= 105,
	MTLPixelFormatRGBA16Unorm		= 110,
	MTLPixelFormatRGBA16Float		= 115,
	MTLPixelFormatRGBA32Float		= 125,
	MTLPixelFormatBC1RGBA			= 130,
	MTLPixelFormatBC2RGBA			= 132,
	MTLPixelFormatBC3RGBA			= 134,
	MTLPixelFormatDepth16Unorm		= 250,
	MTLPixelFormatDepth32Float		= 252,
	MTLPixelFormatDepth24UnormStencil8	= 255,
	MTLPixelFormatDepth32FloatStencil8	= 260
} MTLPixelFormat;

typedef enum MTLSamplerMinMagFilter
{
	MTLSamplerMinMagFilterNearest,
	MTLSamplerMinMagFilterLinear
} MTLSamplerMinMagFilter;

typedef enum MTLTextureUsage
{
	MTLTextureUsageShaderRead = 1,
	MTLTextureUsageRenderTarget = 4
} MTLTextureUsage;

typedef enum MTLTextureType
{
	MTLTextureType2DMultisample = 4,
	MTLTextureType3DTexture = 7
} MTLTextureType;

typedef enum MTLStorageMode
{
	MTLStorageModePrivate = 2
} MTLStorageMode;

typedef enum MTLBlendFactor
{
	MTLBlendFactorZero,
	MTLBlendFactorOne,
	MTLBlendFactorSourceColor,
	MTLBlendFactorOneMinusSourceColor,
	MTLBlendFactorSourceAlpha,
	MTLBlendFactorOneMinusSourceAlpha,
	MTLBlendFactorDestinationColor,
	MTLBlendFactorOneMinusDestinationColor,
	MTLBlendFactorDestinationAlpha,
	MTLBlendFactorOneMinusDestinationAlpha,
	MTLBlendFactorSourceAlphaSaturated,
	MTLBlendFactorBlendColor,
	MTLBlendFactorOneMinusBlendColor
} MTLBlendFactor;

typedef enum MTLBlendOperation
{
	MTLBlendOperationAdd,
	MTLBlendOperationSubtract,
	MTLBlendOperationReverseSubtract,
	MTLBlendOperationMin,
	MTLBlendOperationMax
} MTLBlendOperation;

typedef enum MTLCullMode
{
	MTLCullModeNone,
	MTLCullModeFront,
	MTLCullModeBack
} MTLCullMode;

typedef enum MTLTriangleFillMode
{
	MTLTriangleFillModeFill,
	MTLTriangleFillModeLines
} MTLTriangleFillMode;

typedef enum MTLSamplerAddressMode
{
	MTLSamplerAddressModeClampToEdge = 0,
	MTLSamplerAddressModeRepeat = 2,
	MTLSamplerAddressModeMirrorRepeat = 3
} MTLSamplerAddressMode;

typedef enum MTLSamplerMipFilter
{
	MTLSamplerMipFilterNearest = 1,
	MTLSamplerMipFilterLinear = 2
} MTLSamplerMipFilter;

typedef enum MTLVertexFormat
{
	MTLVertexFormatUChar4 = 3,
	MTLVertexFormatUChar4Normalized = 9,
	MTLVertexFormatShort2 = 16,
	MTLVertexFormatShort4 = 18,
	MTLVertexFormatShort2Normalized = 22,
	MTLVertexFormatShort4Normalized = 24,
	MTLVertexFormatHalf2 = 25,
	MTLVertexFormatHalf4 = 27,
	MTLVertexFormatFloat = 28,
	MTLVertexFormatFloat2 = 29,
	MTLVertexFormatFloat3 = 30,
	MTLVertexFormatFloat4 = 31
} MTLVertexFormat;

typedef enum MTLVertexStepFunction
{
	MTLVertexStepFunctionPerInstance = 2
} MTLVertexStepFunction;

typedef enum MTLCompareFunction
{
	MTLCompareFunctionNever,
	MTLCompareFunctionLess,
	MTLCompareFunctionEqual,
	MTLCompareFunctionLessEqual,
	MTLCompareFunctionGreater,
	MTLCompareFunctionNotEqual,
	MTLCompareFunctionGreaterEqual,
	MTLCompareFunctionAlways
} MTLCompareFunction;

typedef enum MTLStencilOperation
{
	MTLStencilOperationKeep,
	MTLStencilOperationZero,
	MTLStencilOperationReplace,
	MTLStencilOperationIncrementClamp,
	MTLStencilOperationDecrementClamp,
	MTLStencilOperationInvert,
	MTLStencilOperationIncrementWrap,
	MTLStencilOperationDecrementWrap
} MTLStencilOperation;

typedef enum MTLVisibilityResultMode
{
	MTLVisibilityResultModeDisabled = 0,
	MTLVisibilityResultModeCounting = 2
} MTLVisibilityResultMode;

typedef enum MTLResourceOptions
{
	MTLResourceOptionsCPUCacheModeDefaultCache,
	MTLResourceOptionsCPUCacheModeWriteCombined
} MTLResourceOptions;

typedef enum MTLPurgeableState
{
	MTLPurgeableStateNonVolatile = 2,
	MTLPurgeableStateEmpty = 4
} MTLPurgeableState;

/* Metal Structs */

typedef struct MTLClearColor
{
	double red;
	double green;
	double blue;
	double alpha;
} MTLClearColor;

typedef struct MTLViewport
{
	double x;
	double y;
	double w;
	double h;
	double znear;
	double zfar;
} MTLViewport;

typedef struct MTLScissorRect
{
	uint64_t x;
	uint64_t y;
	uint64_t w;
	uint64_t h;
} MTLScissorRect;

typedef struct MTLOrigin
{
	uint64_t x;
	uint64_t y;
	uint64_t z;
} MTLOrigin;

typedef struct MTLSize
{
	uint64_t width;
	uint64_t height;
	uint64_t depth;
} MTLSize;

typedef struct MTLRegion
{
	MTLOrigin origin;
	MTLSize size;
} MTLRegion;

typedef struct CGSize
{
	double width;
	double height;
} CGSize;

typedef struct NSRange
{
	uint64_t loc;
	uint64_t len;
} NSRange;

typedef struct NSOperatingSystemVersion
{
	int64_t major;
	int64_t minor;
	int64_t patch;
} NSOperatingSystemVersion;

/* Opaque Pointer Typedefs */

typedef struct CAMetalLayer CAMetalLayer;
typedef struct MTLBlitCommandEncoder MTLBlitCommandEncoder;
typedef struct MTLBuffer MTLBuffer;
typedef struct MTLCommandBuffer MTLCommandBuffer;
typedef struct MTLCommandQueue MTLCommandQueue;
typedef struct MTLDepthStencilDescriptor MTLDepthStencilDescriptor;
typedef struct MTLDepthStencilState MTLDepthStencilState;
typedef struct MTLDevice MTLDevice;
typedef struct MTLDrawable MTLDrawable;
typedef struct MTLFunction MTLFunction;
typedef struct MTLLibrary MTLLibrary;
typedef struct MTLRenderCommandEncoder MTLRenderCommandEncoder;
typedef struct MTLRenderPassColorAttachmentDescriptor MTLRenderPassColorAttachmentDescriptor;
typedef struct MTLRenderPassDepthAttachmentDescriptor MTLRenderPassDepthAttachmentDescriptor;
typedef struct MTLRenderPassDescriptor MTLRenderPassDescriptor;
typedef struct MTLRenderPassStencilAttachmentDescriptor MTLRenderPassStencilAttachmentDescriptor;
typedef struct MTLRenderPipelineColorAttachmentDescriptor MTLRenderPipelineColorAttachmentDescriptor;
typedef struct MTLRenderPipelineDescriptor MTLRenderPipelineDescriptor;
typedef struct MTLRenderPipelineState MTLRenderPipelineState;
typedef struct MTLSamplerState MTLSamplerState;
typedef struct MTLTexture MTLTexture;
typedef struct MTLTextureDescriptor MTLTextureDescriptor;
typedef struct MTLSamplerDescriptor MTLSamplerDescriptor;
typedef struct MTLStencilDescriptor MTLStencilDescriptor;
typedef struct MTLVertexAttributeDescriptor MTLVertexAttributeDescriptor;
typedef struct MTLVertexBufferLayoutDescriptor MTLVertexBufferLayoutDescriptor;
typedef struct MTLVertexDescriptor MTLVertexDescriptor;
typedef struct NSAutoreleasePool NSAutoreleasePool;
typedef struct NSError NSError;
typedef struct NSString NSString;

/* ObjC Runtime Function Declarations
 *
 * These are not exposed in a public header,
 * but they're guaranteed to exist on all our
 * supported platforms. -caleb
 */

void* objc_retain(void*);
void* objc_release(void*);
void objc_autoreleasePoolPop(void*);
void* objc_autoreleasePoolPush(void);

/* MTLCreateSystemDefaultDevice Function Prototype */

typedef void* (*pfn_CreateSystemDefaultDevice)(void);
static pfn_CreateSystemDefaultDevice MTLCreateSystemDefaultDevice;

/* Command Buffer Callback Block Prototype */

typedef void (^MTLCommandBufferHandler)(MTLCommandBuffer*);

/* ObjC Class References */

static Class classMTLTextureDescriptor;
static Class classMTLRenderPassDescriptor;
static Class classMTLRenderPipelineDescriptor;
static Class classMTLDepthStencilDescriptor;
static Class classMTLSamplerDescriptor;
static Class classMTLStencilDescriptor;
static Class classMTLVertexDescriptor;
static Class classNSProcessInfo;
static Class classNSString;

/* ObjC Method References */

static SEL selAddCompletedHandler;
static SEL selAlloc;
static SEL selAttributes;
static SEL selBlitCommandEncoder;
static SEL selColorAttachments;
static SEL selCommandBuffer;
static SEL selCommit;
static SEL selContents;
static SEL selCopyFromTexture;
static SEL selDepthAttachment;
static SEL selDisplaySyncEnabled;
static SEL selDrawIndexedPrimitives;
static SEL selDrawPrimitives;
static SEL selDrawableSize;
static SEL selEndEncoding;
static SEL selGenerateMipmapsForTexture;
static SEL selGetBytes;
static SEL selHeight;
static SEL selInitWithUTF8String;
static SEL selInsertDebugSignpost;
static SEL selIsD24S8Supported;
static SEL selLayer;
static SEL selLayouts;
static SEL selLocalizedDescription;
static SEL selMipmapLevelCount;
static SEL selName;
static SEL selNew;
static SEL selNewBufferWithLength;
static SEL selNewCommandQueue;
static SEL selNewDepthStencilStateWithDescriptor;
static SEL selNewFunctionWithName;
static SEL selNewLibraryWithSource;
static SEL selNewRenderPipelineStateWithDescriptor;
static SEL selNewSamplerStateWithDescriptor;
static SEL selNewTextureWithDescriptor;
static SEL selNextDrawable;
static SEL selObjectAtIndexedSubscript;
static SEL selIsOperatingSystemAtLeastVersion;
static SEL selPixelFormat;
static SEL selPresentDrawable;
static SEL selProcessInfo;
static SEL selRenderCommandEncoderWithDescriptor;
static SEL selRenderPassDescriptor;
static SEL selReplaceRegion;
static SEL selRespondsToSelector;
static SEL selSetAlphaBlendOperation;
static SEL selSetBackFaceStencil;
static SEL selSetBlendColor;
static SEL selSetBlendingEnabled;
static SEL selSetBufferIndex;
static SEL selSetClearColor;
static SEL selSetClearDepth;
static SEL selSetClearStencil;
static SEL selSetCullMode;
static SEL selSetDepth;
static SEL selSetDepthAttachmentPixelFormat;
static SEL selSetDepthBias;
static SEL selSetDepthCompareFunction;
static SEL selSetDepthFailureOperation;
static SEL selSetDepthStencilPassOperation;
static SEL selSetDepthStencilState;
static SEL selSetDepthWriteEnabled;
static SEL selSetDestinationAlphaBlendFactor;
static SEL selSetDestinationRGBBlendFactor;
static SEL selSetDevice;
static SEL selSetFormat;
static SEL selSetFragmentBuffer;
static SEL selSetFragmentBufferOffset;
static SEL selSetFragmentFunction;
static SEL selSetFragmentSamplerState;
static SEL selSetFragmentTexture;
static SEL selSetFramebufferOnly;
static SEL selSetFrontFaceStencil;
static SEL selSetLoadAction;
static SEL selSetLodMinClamp;
static SEL selSetMagFilter;
static SEL selSetMagnificationFilter;
static SEL selSetMaxAnisotropy;
static SEL selSetMinFilter;
static SEL selSetMipFilter;
static SEL selSetOffset;
static SEL selSetPixelFormat;
static SEL selSetPurgeableState;
static SEL selSetRAddressMode;
static SEL selSetReadMask;
static SEL selSetRenderPipelineState;
static SEL selSetResolveSlice;
static SEL selSetResolveTexture;
static SEL selSetRgbBlendOperation;
static SEL selSetSAddressMode;
static SEL selSetSampleCount;
static SEL selSetScissorRect;
static SEL selSetSlice;
static SEL selSetSourceAlphaBlendFactor;
static SEL selSetSourceRGBBlendFactor;
static SEL selSetStencilAttachmentPixelFormat;
static SEL selSetStencilCompareFunction;
static SEL selSetStencilFailureOperation;
static SEL selSetStencilReference;
static SEL selSetStepFunction;
static SEL selSetStepRate;
static SEL selSetStorageMode;
static SEL selSetStoreAction;
static SEL selSetStride;
static SEL selSetTAddressMode;
static SEL selSetTexture;
static SEL selSetTextureType;
static SEL selSetTriangleFillMode;
static SEL selSetUsage;
static SEL selSetVertexBuffer;
static SEL selSetVertexBufferOffset;
static SEL selSetVertexDescriptor;
static SEL selSetVertexFunction;
static SEL selSetVertexSamplerState;
static SEL selSetVertexTexture;
static SEL selSetViewport;
static SEL selSetVisibilityResultBuffer;
static SEL selSetVisibilityResultMode;
static SEL selSetWriteMask;
static SEL selStencilAttachment;
static SEL selSupportsFamily;
static SEL selSupportsFeatureSet;
static SEL selSupportsTextureSampleCount;
static SEL selSynchronizeResource;
static SEL selTexture;
static SEL selTexture2DDescriptor;
static SEL selTextureCubeDescriptor;
static SEL selUTF8String;
static SEL selVertexDescriptor;
static SEL selWaitUntilCompleted;
static SEL selWidth;

/* Objective-C Class/Selector Initialization */

static inline void InitObjC()
{
	/* Get class references first! */
	classMTLTextureDescriptor		= objc_getClass("MTLTextureDescriptor");
	classMTLRenderPassDescriptor		= objc_getClass("MTLRenderPassDescriptor");
	classMTLRenderPipelineDescriptor	= objc_getClass("MTLRenderPipelineDescriptor");
	classMTLDepthStencilDescriptor  	= objc_getClass("MTLDepthStencilDescriptor");
	classMTLSamplerDescriptor		= objc_getClass("MTLSamplerDescriptor");
	classMTLStencilDescriptor		= objc_getClass("MTLStencilDescriptor");
	classMTLVertexDescriptor		= objc_getClass("MTLVertexDescriptor");
	classNSProcessInfo			= objc_getClass("NSProcessInfo");
	classNSString				= objc_getClass("NSString");

	/* Here come the method references. Hold onto your butts. */
	selAddCompletedHandler			= sel_registerName("addCompletedHandler:");
	selAlloc				= sel_registerName("alloc");
	selAttributes				= sel_registerName("attributes");
	selBlitCommandEncoder			= sel_registerName("blitCommandEncoder");
	selColorAttachments			= sel_registerName("colorAttachments");
	selCommandBuffer			= sel_registerName("commandBuffer");
	selCommit				= sel_registerName("commit");
	selContents				= sel_registerName("contents");
	selCopyFromTexture			= sel_registerName("copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:");
	selDepthAttachment			= sel_registerName("depthAttachment");
	selDisplaySyncEnabled			= sel_registerName("setDisplaySyncEnabled:");
	selDrawIndexedPrimitives		= sel_registerName("drawIndexedPrimitives:indexCount:indexType:indexBuffer:indexBufferOffset:instanceCount:");
	selDrawPrimitives			= sel_registerName("drawPrimitives:vertexStart:vertexCount:");
	selDrawableSize				= sel_registerName("drawableSize");
	selEndEncoding				= sel_registerName("endEncoding");
	selGenerateMipmapsForTexture		= sel_registerName("generateMipmapsForTexture:");
	selGetBytes				= sel_registerName("getBytes:bytesPerRow:bytesPerImage:fromRegion:mipmapLevel:slice:");
	selHeight				= sel_registerName("height");
	selInitWithUTF8String			= sel_registerName("initWithUTF8String:");
	selInsertDebugSignpost			= sel_registerName("insertDebugSignpost:");
	selIsD24S8Supported			= sel_registerName("isDepth24Stencil8PixelFormatSupported");
	selIsOperatingSystemAtLeastVersion	= sel_registerName("isOperatingSystemAtLeastVersion:");
	selLayer				= sel_registerName("layer");
	selLayouts				= sel_registerName("layouts");
	selLocalizedDescription			= sel_registerName("localizedDescription");
	selMipmapLevelCount			= sel_registerName("mipmapLevelCount");
	selName					= sel_registerName("name");
	selNew					= sel_registerName("new");
	selNewBufferWithLength			= sel_registerName("newBufferWithLength:options:");
	selNewCommandQueue			= sel_registerName("newCommandQueue");
	selNewDepthStencilStateWithDescriptor	= sel_registerName("newDepthStencilStateWithDescriptor:");
	selNewFunctionWithName			= sel_registerName("newFunctionWithName:");
	selNewLibraryWithSource			= sel_registerName("newLibraryWithSource:options:error:");
	selNewRenderPipelineStateWithDescriptor = sel_registerName("newRenderPipelineStateWithDescriptor:error:");
	selNewSamplerStateWithDescriptor	= sel_registerName("newSamplerStateWithDescriptor:");
	selNewTextureWithDescriptor		= sel_registerName("newTextureWithDescriptor:");
	selNextDrawable 			= sel_registerName("nextDrawable");
	selObjectAtIndexedSubscript		= sel_registerName("objectAtIndexedSubscript:");
	selPixelFormat				= sel_registerName("pixelFormat");
	selPresentDrawable			= sel_registerName("presentDrawable:");
	selProcessInfo  			= sel_registerName("processInfo");
	selRenderCommandEncoderWithDescriptor	= sel_registerName("renderCommandEncoderWithDescriptor:");
	selRenderPassDescriptor			= sel_registerName("renderPassDescriptor");
	selReplaceRegion			= sel_registerName("replaceRegion:mipmapLevel:slice:withBytes:bytesPerRow:bytesPerImage:");
	selRespondsToSelector			= sel_registerName("respondsToSelector:");
	selSetAlphaBlendOperation		= sel_registerName("setAlphaBlendOperation:");
	selSetBackFaceStencil			= sel_registerName("setBackFaceStencil:");
	selSetBlendColor			= sel_registerName("setBlendColorRed:green:blue:alpha:");
	selSetBlendingEnabled			= sel_registerName("setBlendingEnabled:");
	selSetBufferIndex			= sel_registerName("setBufferIndex:");
	selSetClearColor			= sel_registerName("setClearColor:");
	selSetClearDepth			= sel_registerName("setClearDepth:");
	selSetClearStencil			= sel_registerName("setClearStencil:");
	selSetCullMode				= sel_registerName("setCullMode:");
	selSetDepth				= sel_registerName("setDepth:");
	selSetDepthAttachmentPixelFormat	= sel_registerName("setDepthAttachmentPixelFormat:");
	selSetDepthBias				= sel_registerName("setDepthBias:slopeScale:clamp:");
	selSetDepthCompareFunction		= sel_registerName("setDepthCompareFunction:");
	selSetDepthFailureOperation		= sel_registerName("setDepthFailureOperation:");
	selSetDepthStencilPassOperation		= sel_registerName("setDepthStencilPassOperation:");
	selSetDepthStencilState			= sel_registerName("setDepthStencilState:");
	selSetDepthWriteEnabled			= sel_registerName("setDepthWriteEnabled:");
	selSetDestinationAlphaBlendFactor	= sel_registerName("setDestinationAlphaBlendFactor:");
	selSetDestinationRGBBlendFactor		= sel_registerName("setDestinationRGBBlendFactor:");
	selSetDevice				= sel_registerName("setDevice:");
	selSetFormat				= sel_registerName("setFormat:");
	selSetFragmentBuffer			= sel_registerName("setFragmentBuffer:offset:atIndex:");
	selSetFragmentBufferOffset		= sel_registerName("setFragmentBufferOffset:atIndex:");
	selSetFragmentFunction			= sel_registerName("setFragmentFunction:");
	selSetFragmentSamplerState		= sel_registerName("setFragmentSamplerState:atIndex:");
	selSetFragmentTexture			= sel_registerName("setFragmentTexture:atIndex:");
	selSetFramebufferOnly			= sel_registerName("setFramebufferOnly:");
	selSetFrontFaceStencil			= sel_registerName("setFrontFaceStencil:");
	selSetLoadAction			= sel_registerName("setLoadAction:");
	selSetLodMinClamp			= sel_registerName("setLodMinClamp:");
	selSetMagFilter				= sel_registerName("setMagFilter:");
	selSetMagnificationFilter		= sel_registerName("setMagnificationFilter:");
	selSetMaxAnisotropy			= sel_registerName("setMaxAnisotropy:");
	selSetMinFilter				= sel_registerName("setMinFilter:");
	selSetMipFilter				= sel_registerName("setMipFilter:");
	selSetOffset				= sel_registerName("setOffset:");
	selSetPixelFormat			= sel_registerName("setPixelFormat:");
	selSetPurgeableState			= sel_registerName("setPurgeableState:");
	selSetRAddressMode			= sel_registerName("setRAddressMode:");
	selSetReadMask				= sel_registerName("setReadMask:");
	selSetRenderPipelineState		= sel_registerName("setRenderPipelineState:");
	selSetResolveSlice			= sel_registerName("setResolveSlice:");
	selSetResolveTexture			= sel_registerName("setResolveTexture:");
	selSetRgbBlendOperation			= sel_registerName("setRgbBlendOperation:");
	selSetSAddressMode			= sel_registerName("setSAddressMode:");
	selSetSampleCount			= sel_registerName("setSampleCount:");
	selSetScissorRect			= sel_registerName("setScissorRect:");
	selSetSlice				= sel_registerName("setSlice:");
	selSetSourceAlphaBlendFactor		= sel_registerName("setSourceAlphaBlendFactor:");
	selSetSourceRGBBlendFactor		= sel_registerName("setSourceRGBBlendFactor:");
	selSetStencilAttachmentPixelFormat	= sel_registerName("setStencilAttachmentPixelFormat:");
	selSetStencilCompareFunction		= sel_registerName("setStencilCompareFunction:");
	selSetStencilFailureOperation		= sel_registerName("setStencilFailureOperation:");
	selSetStencilReference			= sel_registerName("setStencilReferenceValue:");
	selSetStepFunction			= sel_registerName("setStepFunction:");
	selSetStepRate				= sel_registerName("setStepRate:");
	selSetStorageMode			= sel_registerName("setStorageMode:");
	selSetStoreAction			= sel_registerName("setStoreAction:");
	selSetStride				= sel_registerName("setStride:");
	selSetTAddressMode			= sel_registerName("setTAddressMode:");
	selSetTexture				= sel_registerName("setTexture:");
	selSetTextureType			= sel_registerName("setTextureType:");
	selSetTriangleFillMode			= sel_registerName("setTriangleFillMode:");
	selSetUsage				= sel_registerName("setUsage:");
	selSetVertexBuffer			= sel_registerName("setVertexBuffer:offset:atIndex:");
	selSetVertexBufferOffset		= sel_registerName("setVertexBufferOffset:atIndex:");
	selSetVertexDescriptor			= sel_registerName("setVertexDescriptor:");
	selSetVertexFunction			= sel_registerName("setVertexFunction:");
	selSetVertexSamplerState		= sel_registerName("setVertexSamplerState:atIndex:");
	selSetVertexTexture			= sel_registerName("setVertexTexture:atIndex:");
	selSetViewport				= sel_registerName("setViewport:");
	selSetVisibilityResultBuffer		= sel_registerName("setVisibilityResultBuffer:");
	selSetVisibilityResultMode		= sel_registerName("setVisibilityResultMode:offset:");
	selSetWriteMask				= sel_registerName("setWriteMask:");
	selSetWriteMask				= sel_registerName("setWriteMask:");
	selStencilAttachment			= sel_registerName("stencilAttachment");
	selSupportsFamily			= sel_registerName("supportsFamily:");
	selSupportsFeatureSet			= sel_registerName("supportsFeatureSet:");
	selSupportsTextureSampleCount		= sel_registerName("supportsTextureSampleCount:");
	selSynchronizeResource			= sel_registerName("synchronizeResource:");
	selTexture				= sel_registerName("texture");
	selTexture2DDescriptor			= sel_registerName("texture2DDescriptorWithPixelFormat:width:height:mipmapped:");
	selTextureCubeDescriptor		= sel_registerName("textureCubeDescriptorWithPixelFormat:size:mipmapped:");
	selUTF8String				= sel_registerName("UTF8String");
	selVertexDescriptor			= sel_registerName("vertexDescriptor");
	selWaitUntilCompleted			= sel_registerName("waitUntilCompleted");
	selWidth				= sel_registerName("width");
}

/* Function Casts for objc_msgSend */
#define msg_b		((uint8_t (*)(void*, SEL)) objc_msgSend)
#define msg_bi		((uint8_t (*)(void*, SEL, int32_t)) objc_msgSend)
#define msg_b_osversion	((uint8_t (*)(void*, SEL, NSOperatingSystemVersion)) objc_msgSend)
#define msg_bp		((uint8_t (*)(void*, SEL, void*)) objc_msgSend)
#define msg_bU		((uint8_t (*)(void*, SEL, uint64_t)) objc_msgSend)
#define msg_cgsize	((CGSize (*)(void*, SEL)) objc_msgSend)
#define msg_i		((int32_t (*)(void*, SEL)) objc_msgSend)
#define msg_ii		((int32_t (*)(void*, SEL, int32_t)) objc_msgSend)
#define msg_p		((void* (*)(void*, SEL)) objc_msgSend)
#define msg_pp		((void* (*)(void*, SEL, void*)) objc_msgSend)
#define msg_ppp 	((void* (*)(void*, SEL, void*, void*)) objc_msgSend)
#define msg_pppp	((void* (*)(void*, SEL, void*, void*, void*)) objc_msgSend)
#define msg_piUb	((void* (*)(void*, SEL, int32_t, uint64_t, uint8_t)) objc_msgSend)
#define msg_piUUb	((void* (*)(void*, SEL, int32_t, uint64_t, uint64_t, uint8_t)) objc_msgSend)
#define msg_pU		((void* (*)(void*, SEL, uint64_t)) objc_msgSend)
#define msg_pUi 	((void* (*)(void*, SEL, uint64_t, int32_t)) objc_msgSend)
#define msg_ps		((void* (*)(void*, SEL, const char*)) objc_msgSend)
#define msg_U		((uint64_t (*)(void*, SEL)) objc_msgSend)
#define msg_v		((void (*)(void*, SEL)) objc_msgSend)
#define msg_vb		((void (*)(void*, SEL, uint8_t)) objc_msgSend)
#define msg_v_color	((void (*)(void*, SEL, MTLClearColor)) objc_msgSend)
#define msg_vd		((void (*)(void*, SEL, double)) objc_msgSend)
#define msg_vf		((void (*)(void*, SEL, float)) objc_msgSend)
#define msg_vfff	((void (*)(void*, SEL, float, float, float)) objc_msgSend)
#define msg_vffff	((void (*)(void*, SEL, float, float, float, float)) objc_msgSend)
#define msg_vi		((void (*)(void*, SEL, int32_t)) objc_msgSend)
#define msg_viU 	((void (*)(void*, SEL, int32_t, uint64_t)) objc_msgSend)
#define msg_viUipUU	((void (*)(void*, SEL, int32_t, uint64_t, int32_t, void*, uint64_t, uint64_t)) objc_msgSend)
#define msg_viUU	((void (*)(void*, SEL, int32_t, uint64_t, uint64_t)) objc_msgSend)
#define msg_vp		((void (*)(void*, SEL, void*)) objc_msgSend)
#define msg_vpU 	((void (*)(void*, SEL, void*, uint64_t)) objc_msgSend)
#define msg_vpUU	((void (*)(void*, SEL, void*, uint64_t, uint64_t)) objc_msgSend)
#define msg_vpUU_origin_size_pUU_origin \
	((void (*)(void*, SEL, void*, uint64_t, uint64_t, MTLOrigin, MTLSize, void*, uint64_t, uint64_t, MTLOrigin)) objc_msgSend)
#define msg_vpUU_region_UU \
	((void (*)(void*, SEL, void*, uint64_t, uint64_t, MTLRegion, uint64_t, uint64_t)) objc_msgSend)
#define msg_v_region_UUpUU \
	((void (*)(void*, SEL, MTLRegion, uint64_t, uint64_t, void*, uint64_t, uint64_t)) objc_msgSend)
#define msg_v_scissor	((void (*)(void*, SEL, MTLScissorRect)) objc_msgSend)
#define msg_vu		((void (*)(void*, SEL, uint32_t)) objc_msgSend)
#define msg_vU		((void (*)(void*, SEL, uint64_t)) objc_msgSend)
#define msg_vUU 	((void (*)(void*, SEL, uint64_t, uint64_t)) objc_msgSend)
#define msg_v_viewport	((void (*)(void*, SEL, MTLViewport)) objc_msgSend)

/* NSString <-> UTF8 */

static inline const char* NSStringToUTF8(NSString *nsstr)
{
	return (const char*) msg_p(
		nsstr,
		selUTF8String
	);
}

static inline NSString* UTF8ToNSString(const char *str)
{
	return (NSString*) msg_ps(
		msg_p(classNSString, selAlloc),
		selInitWithUTF8String,
		str
	);
}

/* Error Handling */

static inline const char* GetNSErrorDescription(NSError *error)
{
	return NSStringToUTF8(
		(NSString*) msg_p(
			error,
			selLocalizedDescription
		)
	);
}

/* Responds to Selector */

static inline uint8_t RespondsToSelector(void* obj, SEL selector)
{
	return msg_bp(obj, selRespondsToSelector, selector);
}

/* iOS / tvOS GPU Check */

static inline uint8_t HasModernAppleGPU(MTLDevice *device)
{
	/* "Modern GPU" = A9 or later */
	uint8_t iosCompat, tvosCompat;

	/* Can we use the GPUFamily API? */
	if (RespondsToSelector(device, selSupportsFamily))
	{
		return msg_ii(
			device,
			selSupportsFamily,
			3002 /* GPUFamilyCommon2 */
		);
	}

	/* Fall back to checking FeatureSets... */
	iosCompat = msg_bi(
		device,
		selSupportsFeatureSet,
		4 /* iOS_GPUFamily3_v1 */
	);
	tvosCompat = msg_bi(
		device,
		selSupportsFeatureSet,
		30003 /* tvOS_GPUFamily2_v1 */
	);
	return iosCompat || tvosCompat;
}

/* OS Version Check */

static inline uint8_t OperatingSystemAtLeast(
	int64_t major,
	int64_t minor,
	int64_t patch
) {
	NSOperatingSystemVersion version = {major, minor, patch};
	return msg_b_osversion(
		msg_p(
			classNSProcessInfo,
			selProcessInfo
		),
		selIsOperatingSystemAtLeastVersion,
		version
	);
}

/* MTLDevice Functions */

static inline const char* mtlGetDeviceName(MTLDevice *device)
{
	return NSStringToUTF8((NSString*) msg_p(
		device,
		selName
	));
}

static inline uint8_t mtlDeviceSupportsSampleCount(
	MTLDevice *device,
	uint64_t count
) {
	return msg_bU(
		device,
		selSupportsTextureSampleCount,
		count
	);
}

static inline uint8_t mtlDeviceSupportsDepth24Stencil8(MTLDevice *device)
{
	return msg_b(device, selIsD24S8Supported);
}

static inline MTLCommandQueue* mtlNewCommandQueue(MTLDevice *device)
{
	return (MTLCommandQueue*) msg_p(
		device,
		selNewCommandQueue
	);
}

/* Resource Creation Functions */

static inline MTLBuffer* mtlNewBuffer(
	MTLDevice *device,
	uint64_t length,
	MTLResourceOptions options
) {
	return (MTLBuffer*) msg_pUi(
		device,
		selNewBufferWithLength,
		length,
		options
	);
}

static inline MTLTexture* mtlNewTexture(
	MTLDevice *device,
	MTLTextureDescriptor *texDesc
) {
	return (MTLTexture*) msg_pp(
		device,
		selNewTextureWithDescriptor,
		texDesc
	);
}

/* resource can be an MTLTextureDescriptor* or MTLBuffer* */
static inline void mtlSetStorageMode(
	void* resource,
	MTLStorageMode mode
) {
	msg_vi(resource, selSetStorageMode, mode);
}

static inline MTLSamplerState* mtlNewSamplerState(
	MTLDevice *device,
	MTLSamplerDescriptor *sampDesc
) {
	return (MTLSamplerState*) msg_pp(
		device,
		selNewSamplerStateWithDescriptor,
		sampDesc
	);
}

static inline MTLDepthStencilState* mtlNewDepthStencilState(
	MTLDevice *device,
	MTLDepthStencilDescriptor *descriptor
) {
	return (MTLDepthStencilState*) msg_pp(
		device,
		selNewDepthStencilStateWithDescriptor,
		descriptor
	);
}

static inline MTLRenderPipelineState* mtlNewRenderPipelineState(
	MTLDevice *device,
	MTLRenderPipelineDescriptor *pipelineDescriptor
) {
	NSError *error = NULL;
	MTLRenderPipelineState *result;
	result = (MTLRenderPipelineState*) msg_ppp(
		device,
		selNewRenderPipelineStateWithDescriptor,
		pipelineDescriptor,
		&error
	);
	if (error != NULL)
	{
		FNA3D_LogError(
			"Metal Error: %s",
			GetNSErrorDescription(error)
		);
	}
	return result;
}

/* Command Buffer Functions */

static inline MTLCommandBuffer* mtlMakeCommandBuffer(
	MTLCommandQueue *queue
) {
	return (MTLCommandBuffer*) msg_p(
		queue,
		selCommandBuffer
	);
}

static inline MTLRenderCommandEncoder* mtlMakeRenderCommandEncoder(
	MTLCommandBuffer *commandBuffer,
	MTLRenderPassDescriptor *renderPassDesc
) {
	return (MTLRenderCommandEncoder*) msg_pp(
		commandBuffer,
		selRenderCommandEncoderWithDescriptor,
		renderPassDesc
	);
}

static inline MTLBlitCommandEncoder* mtlMakeBlitCommandEncoder(
	MTLCommandBuffer *commandBuffer
) {
	return (MTLBlitCommandEncoder*) msg_p(
		commandBuffer,
		selBlitCommandEncoder
	);
}

static inline void mtlPresentDrawable(
	MTLCommandBuffer *commandBuffer,
	MTLDrawable *drawable
) {
	msg_vp(
		commandBuffer,
		selPresentDrawable,
		drawable
	);
}

static inline void mtlAddCompletedHandler(
	MTLCommandBuffer *commandBuffer,
	MTLCommandBufferHandler callback
) {
	msg_vp(
		commandBuffer,
		selAddCompletedHandler,
		callback
	);
}

static inline void mtlCommitCommandBuffer(
	MTLCommandBuffer *commandBuffer
) {
	msg_v(commandBuffer, selCommit);
}

static inline void mtlWaitUntilCompleted(
	MTLCommandBuffer *commandBuffer
) {
	msg_v(commandBuffer, selWaitUntilCompleted);
}

/* Buffer Functions */

static inline void* mtlGetBufferContents(MTLBuffer *buffer)
{
	return msg_p(buffer, selContents);
}

/* Attachment Functions */

/* If desc is MTLRenderPassDescriptor*, return MTLRenderPassColorAttachmentDescriptor*
 * If desc is MTLRenderPipelineDescriptor*, return MTLRenderPipelineColorAttachmentDescriptor*
 */
static inline void* mtlGetColorAttachment(void* desc, uint64_t index)
{
	return msg_pU(
		msg_p(
			desc,
			selColorAttachments
		),
		selObjectAtIndexedSubscript,
		index
	);
}

static inline MTLRenderPassDepthAttachmentDescriptor* mtlGetDepthAttachment(
	MTLRenderPassDescriptor *renderPassDesc
) {
	return (MTLRenderPassDepthAttachmentDescriptor*) msg_p(
		renderPassDesc,
		selDepthAttachment
	);
}

static inline MTLRenderPassStencilAttachmentDescriptor* mtlGetStencilAttachment(
	MTLRenderPassDescriptor *renderPassDesc
) {
	return (MTLRenderPassStencilAttachmentDescriptor*) msg_p(
		renderPassDesc,
		selStencilAttachment
	);
}

/* attachment can be Color, Depth, or Stencil! */
static inline void mtlSetAttachmentLoadAction(
	void* attachment,
	MTLLoadAction loadAction
) {
	msg_vi(attachment, selSetLoadAction, loadAction);
}

/* attachment can be Color, Depth, or Stencil! */
static inline void mtlSetAttachmentStoreAction(
	void* attachment,
	MTLStoreAction storeAction
) {
	msg_vi(attachment, selSetStoreAction, storeAction);
}

/* attachment can be Color, Depth, or Stencil! */
static inline void mtlSetAttachmentTexture(
	void* attachment,
	MTLTexture *texture
) {
	msg_vp(attachment, selSetTexture, texture);
}

/* attachment can be Color, Depth, or Stencil! */
static inline void mtlSetAttachmentSlice(
	void* attachment,
	uint64_t slice
) {
	msg_vU(attachment, selSetSlice, slice);
}

/* attachment can be Color, Depth, or Stencil! */
static inline void mtlSetAttachmentPixelFormat(
	void* attachment,
	MTLPixelFormat pixelFormat
) {
	msg_vi(attachment, selSetPixelFormat, pixelFormat);
}

/* attachment can be Color, Depth, or Stencil! */
static inline void mtlSetAttachmentResolveTexture(
	void* attachment,
	MTLTexture *resolveTexture
) {
	msg_vp(attachment, selSetResolveTexture, resolveTexture);
}

/* attachment can be Color, Depth, or Stencil! */
static inline void mtlSetAttachmentResolveSlice(
	void* attachment,
	uint64_t resolveSlice
) {
	msg_vU(attachment, selSetResolveSlice, resolveSlice);
}

static inline void mtlSetAttachmentClearColor(
	MTLRenderPassColorAttachmentDescriptor *attachment,
	float r,
	float g,
	float b,
	float a
) {
	MTLClearColor clearColor = {r, g, b, a};
	msg_v_color(attachment, selSetClearColor, clearColor);
}

static inline void mtlSetAttachmentClearDepth(
	MTLRenderPassDepthAttachmentDescriptor *attachment,
	double clearDepth
) {
	msg_vd(attachment, selSetClearDepth, clearDepth);
}

static inline void mtlSetAttachmentClearStencil(
	MTLRenderPassStencilAttachmentDescriptor *attachment,
	uint32_t clearStencil
) {
	msg_vu(attachment, selSetClearStencil, clearStencil);
}

static inline void mtlSetAttachmentBlendingEnabled(
	MTLRenderPipelineColorAttachmentDescriptor *attachment,
	uint8_t enabled
) {
	msg_vb(attachment, selSetBlendingEnabled, enabled);
}

static inline void mtlSetAttachmentAlphaBlendOperation(
	MTLRenderPipelineColorAttachmentDescriptor *attachment,
	MTLBlendOperation op
) {
	msg_vi(attachment, selSetAlphaBlendOperation, op);
}

static inline void mtlSetAttachmentRGBBlendOperation(
	MTLRenderPipelineColorAttachmentDescriptor *attachment,
	MTLBlendOperation op
) {
	msg_vi(attachment, selSetRgbBlendOperation, op);
}

static inline void mtlSetAttachmentDestinationAlphaBlendFactor(
	MTLRenderPipelineColorAttachmentDescriptor *attachment,
	MTLBlendFactor blend
) {
	msg_vi(attachment, selSetDestinationAlphaBlendFactor, blend);
}

static inline void mtlSetAttachmentDestinationRGBBlendFactor(
	MTLRenderPipelineColorAttachmentDescriptor *attachment,
	MTLBlendFactor blend
) {
	msg_vi(attachment, selSetDestinationRGBBlendFactor, blend);
}

static inline void mtlSetAttachmentSourceAlphaBlendFactor(
	MTLRenderPipelineColorAttachmentDescriptor *attachment,
	MTLBlendFactor blend
) {
	msg_vi(attachment, selSetSourceAlphaBlendFactor, blend);
}

static inline void mtlSetAttachmentSourceRGBBlendFactor(
	MTLRenderPipelineColorAttachmentDescriptor *attachment,
	MTLBlendFactor blend
) {
	msg_vi(attachment, selSetSourceRGBBlendFactor, blend);
}

static inline void mtlSetAttachmentWriteMask(
	MTLRenderPipelineColorAttachmentDescriptor *attachment,
	int32_t mask /* MTLColorWriteMask */
) {
	msg_vi(attachment, selSetWriteMask, mask);
}

/* Render Pass Descriptor Functions */

static inline MTLRenderPassDescriptor* mtlMakeRenderPassDescriptor()
{
	return (MTLRenderPassDescriptor*) msg_p(
		classMTLRenderPassDescriptor,
		selRenderPassDescriptor
	);
}

static inline void mtlSetVisibilityResultBuffer(
	MTLRenderPassDescriptor *renderPassDesc,
	MTLBuffer *buffer
) {
	msg_vp(
		renderPassDesc,
		selSetVisibilityResultBuffer,
		buffer
	);
}

static inline void mtlSetVisibilityResultMode(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLVisibilityResultMode mode,
	uint64_t offset
) {
	msg_viU(
		renderCommandEncoder,
		selSetVisibilityResultMode,
		mode,
		offset
	);
}

static inline void mtlSetBlendColor(
	MTLRenderCommandEncoder *renderCommandEncoder,
	float red,
	float green,
	float blue,
	float alpha
) {
	msg_vffff(
		renderCommandEncoder,
		selSetBlendColor,
		red,
		green,
		blue,
		alpha
	);
}

static inline void mtlSetStencilReferenceValue(
	MTLRenderCommandEncoder *renderCommandEncoder,
	uint32_t referenceValue
) {
	msg_vu(
		renderCommandEncoder,
		selSetStencilReference,
		referenceValue
	);
}

static inline void mtlSetViewport(
	MTLRenderCommandEncoder *renderCommandEncoder,
	double x,
	double y,
	double w,
	double h,
	double minDepth,
	double maxDepth
) {
	MTLViewport viewport = {x, y, w, h, minDepth, maxDepth};
	msg_v_viewport(
		renderCommandEncoder,
		selSetViewport,
		viewport
	);
}

static inline void mtlSetScissorRect(
	MTLRenderCommandEncoder *renderCommandEncoder,
	uint64_t x,
	uint64_t y,
	uint64_t w,
	uint64_t h
) {
	MTLScissorRect rect = {x, y, w, h};
	msg_v_scissor(
		renderCommandEncoder,
		selSetScissorRect,
		rect
	);
}

static inline void mtlSetCullMode(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLCullMode cullMode
) {
	msg_vi(
		renderCommandEncoder,
		selSetCullMode,
		cullMode
	);
}

static inline void mtlSetTriangleFillMode(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLTriangleFillMode fillMode
) {
	msg_vi(
		renderCommandEncoder,
		selSetTriangleFillMode,
		fillMode
	);
}

static inline void mtlSetDepthBias(
	MTLRenderCommandEncoder *renderCommandEncoder,
	float depthBias,
	float slopeScaleDepthBias,
	float clamp
) {
	msg_vfff(
		renderCommandEncoder,
		selSetDepthBias,
		depthBias,
		slopeScaleDepthBias,
		clamp
	);
}

static inline void mtlSetDepthStencilState(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLDepthStencilState *depthStencilState
) {
	msg_vp(
		renderCommandEncoder,
		selSetDepthStencilState,
		depthStencilState
	);
}

static inline void mtlInsertDebugSignpost(
	MTLRenderCommandEncoder *renderCommandEncoder,
	const char* message
) {
	msg_vp(
		renderCommandEncoder,
		selInsertDebugSignpost,
		UTF8ToNSString(message)
	);
}

static inline void mtlSetRenderPipelineState(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLRenderPipelineState *pipelineState
) {
	msg_vp(
		renderCommandEncoder,
		selSetRenderPipelineState,
		pipelineState
	);
}

static inline void mtlSetVertexBuffer(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLBuffer *vertexBuffer,
	uint64_t offset,
	uint64_t index
) {
	msg_vpUU(
		renderCommandEncoder,
		selSetVertexBuffer,
		vertexBuffer,
		offset,
		index
	);
}

static inline void mtlSetVertexBufferOffset(
	MTLRenderCommandEncoder *renderCommandEncoder,
	uint64_t offset,
	uint64_t index
) {
	msg_vUU(
		renderCommandEncoder,
		selSetVertexBufferOffset,
		offset,
		index
	);
}

static inline void mtlSetVertexSamplerState(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLSamplerState *samplerState,
	uint64_t index
) {
	msg_vpU(
		renderCommandEncoder,
		selSetVertexSamplerState,
		samplerState,
		index
	);
}

static inline void mtlSetVertexTexture(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLTexture *vertexTexture,
	uint64_t index
) {
	msg_vpU(
		renderCommandEncoder,
		selSetVertexTexture,
		vertexTexture,
		index
	);
}

static inline void mtlSetFragmentBuffer(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLBuffer *fragmentBuffer,
	uint64_t offset,
	uint64_t index
) {
	msg_vpUU(
		renderCommandEncoder,
		selSetFragmentBuffer,
		fragmentBuffer,
		offset,
		index
	);
}

static inline void mtlSetFragmentBufferOffset(
	MTLRenderCommandEncoder *renderCommandEncoder,
	uint64_t offset,
	uint64_t index
) {
	msg_vUU(
		renderCommandEncoder,
		selSetFragmentBufferOffset,
		offset,
		index
	);
}

static inline void mtlSetFragmentSamplerState(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLSamplerState *samplerState,
	uint64_t index
) {
	msg_vpU(
		renderCommandEncoder,
		selSetFragmentSamplerState,
		samplerState,
		index
	);
}

static inline void mtlSetFragmentTexture(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLTexture *fragmentTexture,
	uint64_t index
) {
	msg_vpU(
		renderCommandEncoder,
		selSetFragmentTexture,
		fragmentTexture,
		index
	);
}

/* commandEncoder can be a Render or Blit encoder! */
static inline void mtlEndEncoding(void* commandEncoder)
{
	msg_v(commandEncoder, selEndEncoding);
}

static inline void mtlDrawIndexedPrimitives(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLPrimitiveType primitiveType,
	uint64_t indexCount,
	MTLIndexType indexType,
	MTLBuffer *indexBuffer,
	uint64_t indexBufferOffset,
	uint64_t instanceCount
) {
	msg_viUipUU(
		renderCommandEncoder,
		selDrawIndexedPrimitives,
		primitiveType,
		indexCount,
		indexType,
		indexBuffer,
		indexBufferOffset,
		instanceCount
	);
}

static inline void mtlDrawPrimitives(
	MTLRenderCommandEncoder *renderCommandEncoder,
	MTLPrimitiveType primitive,
	uint64_t vertexStart,
	uint64_t vertexCount
) {
	msg_viUU(
		renderCommandEncoder,
		selDrawPrimitives,
		primitive,
		vertexStart,
		vertexCount
	);
}

/* Layer and Drawable Functions */

static inline CAMetalLayer* mtlGetLayer(void* view)
{
	return (CAMetalLayer*) msg_p(view, selLayer);
}

static inline void mtlSetLayerDevice(
	CAMetalLayer *layer,
	MTLDevice *device
) {
	msg_vp(layer, selSetDevice, device);
}

static inline MTLDrawable* mtlNextDrawable(CAMetalLayer *layer)
{
	return (MTLDrawable*) msg_p(layer, selNextDrawable);
}

static inline MTLPixelFormat mtlGetLayerPixelFormat(CAMetalLayer *layer)
{
	return (MTLPixelFormat) msg_i(layer, selPixelFormat);
}

static inline CGSize mtlGetDrawableSize(CAMetalLayer *layer)
{
	return msg_cgsize(layer, selDrawableSize);
}

static inline void mtlSetDisplaySyncEnabled(
	CAMetalLayer *layer,
	uint8_t enabled
) {
	msg_vb(layer, selDisplaySyncEnabled, enabled);
}

static inline void mtlSetLayerFramebufferOnly(
	CAMetalLayer *layer,
	uint8_t framebufferOnly
) {
	msg_vb(layer, selSetFramebufferOnly, framebufferOnly);
}

static inline void mtlSetLayerMagnificationFilter(
	CAMetalLayer *layer,
	NSString *filter
) {
	msg_vp(layer, selSetMagnificationFilter, filter);
}

static inline MTLTexture* mtlGetTextureFromDrawable(MTLDrawable *drawable)
{
	return (MTLTexture*) msg_p(drawable, selTexture);
}

/* Texture Descriptor Functions */

static inline MTLTextureDescriptor* mtlMakeTexture2DDescriptor(
	MTLPixelFormat format,
	uint64_t width,
	uint64_t height,
	uint8_t mipmapped
) {
	return (MTLTextureDescriptor*) msg_piUUb(
		classMTLTextureDescriptor,
		selTexture2DDescriptor,
		format,
		width,
		height,
		mipmapped
	);
}

static inline MTLTextureDescriptor* mtlMakeTextureCubeDescriptor(
	MTLPixelFormat format,
	uint64_t size,
	uint8_t mipmapped
) {
	return (MTLTextureDescriptor*) msg_piUb(
		classMTLTextureDescriptor,
		selTextureCubeDescriptor,
		format,
		size,
		mipmapped
	);
}

static inline void mtlSetTextureUsage(
	MTLTextureDescriptor *texDesc,
	MTLTextureUsage usage
) {
	msg_vi(texDesc, selSetUsage, usage);
}

static inline void mtlSetTextureType(
	MTLTextureDescriptor *texDesc,
	MTLTextureType textureType
) {
	msg_vi(texDesc, selSetTextureType, textureType);
}

static inline void mtlSetTextureSampleCount(
	MTLTextureDescriptor *texDesc,
	uint64_t sampleCount
) {
	msg_vU(texDesc, selSetSampleCount, sampleCount);
}

static inline void mtlSetTextureDepth(
	MTLTextureDescriptor *texDesc,
	uint64_t depth
) {
	msg_vU(texDesc, selSetDepth, depth);
}

/* Texture Functions */

static inline void mtlReplaceRegion(
	MTLTexture *texture,
	MTLRegion region,
	uint64_t level,
	uint64_t slice,
	void* pixelBytes,
	uint64_t bytesPerRow,
	uint64_t bytesPerImage
) {
	msg_v_region_UUpUU(
		texture,
		selReplaceRegion,
		region,
		level,
		slice,
		pixelBytes,
		bytesPerRow,
		bytesPerImage
	);
}

static inline void mtlGetTextureBytes(
	MTLTexture *texture,
	void* pixelBytes,
	uint64_t bytesPerRow,
	uint64_t bytesPerImage,
	MTLRegion region,
	uint64_t level,
	uint64_t slice
) {
	msg_vpUU_region_UU(
		texture,
		selGetBytes,
		pixelBytes,
		bytesPerRow,
		bytesPerImage,
		region,
		level,
		slice
	);
}

static inline uint64_t mtlGetTextureWidth(MTLTexture *texture)
{
	return msg_U(texture, selWidth);
}

static inline uint64_t mtlGetTextureHeight(MTLTexture *texture)
{
	return msg_U(texture, selHeight);
}

static inline uint64_t mtlGetTextureLevelCount(MTLTexture *texture)
{
	return msg_U(texture, selMipmapLevelCount);
}

/* resource can be an MTLTexture* or MTLBuffer* */
static inline MTLPurgeableState mtlSetPurgeableState(
	void* resource,
	MTLPurgeableState state
) {
	return (MTLPurgeableState) msg_ii(
		resource,
		selSetPurgeableState,
		state
	);
}

/* Blit Command Encoder Functions */

static inline void mtlBlitTextureToTexture(
	MTLBlitCommandEncoder *blitCommandEncoder,
	MTLTexture *srcTexture,
	uint64_t srcSlice,
	uint64_t srcLevel,
	MTLOrigin origin,
	MTLSize size,
	MTLTexture *dstTexture,
	uint64_t dstSlice,
	uint64_t dstLevel,
	MTLOrigin dstOrigin
) {
	msg_vpUU_origin_size_pUU_origin(
		blitCommandEncoder,
		selCopyFromTexture,
		srcTexture,
		srcSlice,
		srcLevel,
		origin,
		size,
		dstTexture,
		dstSlice,
		dstLevel,
		dstOrigin
	);
}

/* resource can be a MTLTexture* or a MTLBuffer* */
static inline void mtlSynchronizeResource(
	MTLBlitCommandEncoder *blitCommandEncoder,
	void* resource
) {
	msg_vp(blitCommandEncoder, selSynchronizeResource, resource);
}

static inline void mtlGenerateMipmapsForTexture(
	MTLBlitCommandEncoder *blitCommandEncoder,
	MTLTexture *texture
) {
	msg_vp(
		blitCommandEncoder,
		selGenerateMipmapsForTexture,
		texture
	);
}

/* Render Pipeline State Functions */

static inline MTLRenderPipelineDescriptor* mtlNewRenderPipelineDescriptor()
{
	return (MTLRenderPipelineDescriptor*) msg_p(
		classMTLRenderPipelineDescriptor,
		selNew
	);
}

static inline void mtlSetPipelineVertexFunction(
	MTLRenderPipelineDescriptor *pipelineDesc,
	MTLFunction *vertexFunction
) {
	msg_vp(pipelineDesc, selSetVertexFunction, vertexFunction);
}

static inline void mtlSetPipelineFragmentFunction(
	MTLRenderPipelineDescriptor *pipelineDesc,
	MTLFunction *fragmentFunction
) {
	msg_vp(pipelineDesc, selSetFragmentFunction, fragmentFunction);
}

static inline void mtlSetPipelineVertexDescriptor(
	MTLRenderPipelineDescriptor *pipelineDesc,
	MTLVertexDescriptor *vertexDesc
) {
	msg_vp(pipelineDesc, selSetVertexDescriptor, vertexDesc);
}

static inline void mtlSetDepthAttachmentPixelFormat(
	MTLRenderPipelineDescriptor *pipelineDesc,
	MTLPixelFormat format
) {
	msg_vi(pipelineDesc, selSetDepthAttachmentPixelFormat, format);
}

static inline void mtlSetStencilAttachmentPixelFormat(
	MTLRenderPipelineDescriptor *pipelineDesc,
	MTLPixelFormat format
) {
	msg_vi(pipelineDesc, selSetStencilAttachmentPixelFormat, format);
}

static inline void mtlSetPipelineSampleCount(
	MTLRenderPipelineDescriptor *pipelineDesc,
	uint64_t sampleCount
) {
	msg_vU(pipelineDesc, selSetSampleCount, sampleCount);
}

/* Sampler Descriptor Functions */

static inline MTLSamplerDescriptor* mtlNewSamplerDescriptor()
{
	return (MTLSamplerDescriptor*) msg_p(
		classMTLSamplerDescriptor,
		selNew
	);
}

static inline void mtlSetSamplerMinFilter(
	MTLSamplerDescriptor *samplerDesc,
	MTLSamplerMinMagFilter filter
) {
	msg_vi(samplerDesc, selSetMinFilter, filter);
}

static inline void mtlSetSamplerMagFilter(
	MTLSamplerDescriptor *samplerDesc,
	MTLSamplerMinMagFilter filter
) {
	msg_vi(samplerDesc, selSetMagFilter, filter);
}

static inline void mtlSetSamplerMipFilter(
	MTLSamplerDescriptor *samplerDesc,
	MTLSamplerMipFilter filter
) {
	msg_vi(samplerDesc, selSetMipFilter, filter);
}

static inline void mtlSetSamplerLodMinClamp(
	MTLSamplerDescriptor *samplerDesc,
	float clamp
) {
	msg_vf(samplerDesc, selSetLodMinClamp, clamp);
}

static inline void mtlSetSamplerMaxAnisotropy(
	MTLSamplerDescriptor *samplerDesc,
	uint64_t maxAnisotropy
) {
	msg_vU(samplerDesc, selSetMaxAnisotropy, maxAnisotropy);
}

static inline void mtlSetSampler_rAddressMode(
	MTLSamplerDescriptor *samplerDesc,
	MTLSamplerAddressMode mode
) {
	msg_vi(samplerDesc, selSetRAddressMode, mode);
}

static inline void mtlSetSampler_sAddressMode(
	MTLSamplerDescriptor *samplerDesc,
	MTLSamplerAddressMode mode
) {
	msg_vi(samplerDesc, selSetSAddressMode, mode);
}

static inline void mtlSetSampler_tAddressMode(
	MTLSamplerDescriptor *samplerDesc,
	MTLSamplerAddressMode mode
) {
	msg_vi(samplerDesc, selSetTAddressMode, mode);
}

/* Vertex Descriptor Functions */

static inline MTLVertexDescriptor* mtlMakeVertexDescriptor()
{
	return (MTLVertexDescriptor*) msg_p(
		classMTLVertexDescriptor,
		selVertexDescriptor
	);
}

static inline MTLVertexAttributeDescriptor* mtlGetVertexAttributeDescriptor(
	MTLVertexDescriptor *vertexDesc,
	uint64_t index
) {
	return (MTLVertexAttributeDescriptor*) msg_pU(
		msg_p(
			vertexDesc,
			selAttributes
		),
		selObjectAtIndexedSubscript,
		index
	);
}

static inline void mtlSetVertexAttributeFormat(
	MTLVertexAttributeDescriptor* vertexAttribute,
	MTLVertexFormat format
) {
	msg_vi(vertexAttribute, selSetFormat, format);
}

static inline void mtlSetVertexAttributeOffset(
	MTLVertexAttributeDescriptor* vertexAttribute,
	uint64_t offset
) {
	msg_vU(vertexAttribute, selSetOffset, offset);
}

static inline void mtlSetVertexAttributeBufferIndex(
	MTLVertexAttributeDescriptor* vertexAttribute,
	uint64_t bufferIndex
) {
	msg_vU(vertexAttribute, selSetBufferIndex, bufferIndex);
}

static inline MTLVertexBufferLayoutDescriptor* mtlGetVertexBufferLayoutDescriptor(
	MTLVertexDescriptor *vertexDesc,
	uint64_t index
) {
	return (MTLVertexBufferLayoutDescriptor*) msg_pU(
		msg_p(
			vertexDesc,
			selLayouts
		),
		selObjectAtIndexedSubscript,
		index
	);
}

static inline void mtlSetVertexBufferLayoutStride(
	MTLVertexBufferLayoutDescriptor *vertexBufferLayout,
	uint64_t stride
) {
	msg_vU(vertexBufferLayout, selSetStride, stride);
}

static inline void mtlSetVertexBufferLayoutStepFunction(
	MTLVertexBufferLayoutDescriptor *vertexBufferLayout,
	MTLVertexStepFunction stepFunc
) {
	msg_vi(vertexBufferLayout, selSetStepFunction, stepFunc);
}

static inline void mtlSetVertexBufferLayoutStepRate(
	MTLVertexBufferLayoutDescriptor *vertexBufferLayout,
	uint64_t stepRate
) {
	msg_vU(vertexBufferLayout, selSetStepRate, stepRate);
}

/* Library Functions */

static inline MTLLibrary* mtlNewLibraryWithSource(
	MTLDevice *device,
	NSString *shaderSource
) {
	NSError *error = NULL;
	MTLLibrary *result = (MTLLibrary*) msg_pppp(
		device,
		selNewLibraryWithSource,
		shaderSource,
		NULL,
		&error
	);
	if (error != NULL)
	{
		FNA3D_LogError(
			"Metal Error: %s",
			GetNSErrorDescription(error)
		);
	}
	return result;
}

static inline MTLFunction* mtlNewFunctionWithName(
	MTLLibrary *library,
	NSString *shaderName
) {
	return (MTLFunction*) msg_pp(
		library,
		selNewFunctionWithName,
		shaderName
	);
}

/* Depth-Stencil State Functions */

static inline MTLDepthStencilDescriptor* mtlNewDepthStencilDescriptor()
{
	return (MTLDepthStencilDescriptor*) msg_p(
		classMTLDepthStencilDescriptor,
		selNew
	);
}

static inline MTLStencilDescriptor* mtlNewStencilDescriptor()
{
	return (MTLStencilDescriptor*) msg_p(
		classMTLStencilDescriptor,
		selNew
	);
}

static inline void mtlSetDepthCompareFunction(
	MTLDepthStencilDescriptor *depthStencilDesc,
	MTLCompareFunction func
) {
	msg_vi(depthStencilDesc, selSetDepthCompareFunction, func);
}

static inline void mtlSetDepthWriteEnabled(
	MTLDepthStencilDescriptor *depthStencilDesc,
	uint8_t enabled
) {
	msg_vb(depthStencilDesc, selSetDepthWriteEnabled, enabled);
}

static inline void mtlSetBackFaceStencil(
	MTLDepthStencilDescriptor *depthStencilDesc,
	MTLStencilDescriptor *stencilDesc
) {
	msg_vp(depthStencilDesc, selSetBackFaceStencil, stencilDesc);
}

static inline void mtlSetFrontFaceStencil(
	MTLDepthStencilDescriptor *depthStencilDesc,
	MTLStencilDescriptor *stencilDesc
) {
	msg_vp(depthStencilDesc, selSetFrontFaceStencil, stencilDesc);
}

static inline void mtlSetStencilFailureOperation(
	MTLStencilDescriptor *stencilDesc,
	MTLStencilOperation op
) {
	msg_vi(stencilDesc, selSetStencilFailureOperation, op);
}

static inline void mtlSetDepthFailureOperation(
	MTLStencilDescriptor *stencilDesc,
	MTLStencilOperation op
) {
	msg_vi(stencilDesc, selSetDepthFailureOperation, op);
}

static inline void mtlSetDepthStencilPassOperation(
	MTLStencilDescriptor *stencilDesc,
	MTLStencilOperation op
) {
	msg_vi(stencilDesc, selSetDepthStencilPassOperation, op);
}

static inline void mtlSetStencilCompareFunction(
	MTLStencilDescriptor *stencilDesc,
	MTLCompareFunction func
) {
	msg_vi(stencilDesc, selSetStencilCompareFunction, func);
}

static inline void mtlSetStencilReadMask(
	MTLStencilDescriptor *stencilDesc,
	uint32_t mask
) {
	msg_vu(stencilDesc, selSetReadMask, mask);
}

static inline void mtlSetStencilWriteMask(
	MTLStencilDescriptor *stencilDesc,
	uint32_t mask
) {
	msg_vu(stencilDesc, selSetWriteMask, mask);
}

#endif /* FNA3D_DRIVER_METAL_H */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
