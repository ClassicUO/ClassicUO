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

#if FNA3D_DRIVER_OPENGL

#include "FNA3D_Driver.h"
#include "FNA3D_Driver_OpenGL.h"

#ifdef USE_SDL3
#include <SDL3/SDL.h>
#else
#include <SDL.h>
static inline SDL_threadID SDL_GetCurrentThreadID()
{
	return SDL_ThreadID();
}
#define SDL_ThreadID SDL_threadID
#define SDL_Mutex SDL_mutex
#define SDL_Semaphore SDL_sem
#define SDL_SignalSemaphore SDL_SemPost
#define SDL_WaitSemaphore SDL_SemWait
#endif

/* We only use this to detect UIKit, for backbuffer creation */
#ifdef SDL_VIDEO_DRIVER_UIKIT
#include <SDL_syswm.h>
#endif /* SDL_VIDEO_DRIVER_UIKIT */

/* Internal Structures */

typedef struct FNA3D_Command FNA3D_Command; /* See Threading Support section */

typedef struct OpenGLTexture OpenGLTexture;
typedef struct OpenGLRenderbuffer OpenGLRenderbuffer;
typedef struct OpenGLBuffer OpenGLBuffer;
typedef struct OpenGLEffect OpenGLEffect;
typedef struct OpenGLQuery OpenGLQuery;

struct OpenGLTexture /* Cast from FNA3D_Texture* */
{
	uint32_t handle;
	GLenum target;
	uint8_t hasMipmaps;
	FNA3D_TextureAddressMode wrapS;
	FNA3D_TextureAddressMode wrapT;
	FNA3D_TextureAddressMode wrapR;
	FNA3D_TextureFilter filter;
	float anisotropy;
	int32_t maxMipmapLevel;
	float lodBias;
	FNA3D_SurfaceFormat format;
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
		} cube;
	};
	OpenGLTexture *next; /* linked list */
	uint8_t external;
};

static OpenGLTexture NullTexture =
{
	0,
	GL_TEXTURE_2D,
	0,
	FNA3D_TEXTUREADDRESSMODE_WRAP,
	FNA3D_TEXTUREADDRESSMODE_WRAP,
	FNA3D_TEXTUREADDRESSMODE_WRAP,
	FNA3D_TEXTUREFILTER_LINEAR,
	0.0f,
	0,
	0.0f,
	FNA3D_SURFACEFORMAT_COLOR,
	{
		{ 0, 0 }
	},
	NULL
};

struct OpenGLBuffer /* Cast from FNA3D_Buffer* */
{
	GLuint handle;
	intptr_t size;
	GLenum dynamic;
	OpenGLBuffer *next; /* linked list */
};

struct OpenGLRenderbuffer /* Cast from FNA3D_Renderbuffer* */
{
	GLuint handle;
	FNA3D_SurfaceFormat format;
	OpenGLRenderbuffer *next; /* linked list */
};

struct OpenGLEffect /* Cast from FNA3D_Effect* */
{
	MOJOSHADER_effect *effect;
	OpenGLEffect *next; /* linked list */
};

struct OpenGLQuery /* Cast from FNA3D_Query* */
{
	GLuint handle;
	OpenGLQuery *next; /* linked list */
};

typedef struct OpenGLBackbuffer
{
	#define BACKBUFFER_TYPE_NULL 0
	#define BACKBUFFER_TYPE_OPENGL 1
	uint8_t type;

	uint8_t isSrgb;
	int32_t width;
	int32_t height;
	FNA3D_DepthFormat depthFormat;
	int32_t multiSampleCount;
	struct
	{
		GLuint handle;

		GLuint texture;
		GLuint colorAttachment;
		GLuint depthStencilAttachment;
	} opengl;
} OpenGLBackbuffer;

typedef struct OpenGLVertexAttribute
{
	uint32_t currentBuffer;
	void *currentPointer;
	FNA3D_VertexElementFormat currentFormat;
	uint8_t currentNormalized;
	uint32_t currentStride;
} OpenGLVertexAttribute;

typedef struct OpenGLRenderer /* Cast from FNA3D_Renderer* */
{
	/* Associated FNA3D_Device */
	FNA3D_Device *parentDevice;

	/* Context */
	SDL_GLContext context;
	uint8_t useES3;
	uint8_t useCoreProfile;

	/* FIXME: https://github.com/KhronosGroup/EGL-Registry/pull/113 */
	uint8_t isEGL;

	/* The Faux-Backbuffer */
	OpenGLBackbuffer *backbuffer;
	FNA3D_DepthFormat windowDepthFormat;
	GLenum backbufferScaleMode;
	GLuint realBackbufferFBO;
	GLuint realBackbufferRBO;
	uint8_t srgbEnabled;

	/* VAO for Core Profile */
	GLuint vao;

	/* Capabilities */
	uint8_t supports_s3tc;
	uint8_t supports_dxt1;
	uint8_t supports_anisotropic_filtering;
	uint8_t supports_srgb_rendertarget;
	uint8_t supports_bc7;
	int32_t maxMultiSampleCount;
	int32_t maxMultiSampleCountFormat[21];
	int32_t windowSampleCount;

	/* Blend State */
	uint8_t alphaBlendEnable;
	FNA3D_Color blendColor;
	FNA3D_BlendFunction blendOp;
	FNA3D_BlendFunction blendOpAlpha;
	FNA3D_Blend srcBlend;
	FNA3D_Blend dstBlend;
	FNA3D_Blend srcBlendAlpha;
	FNA3D_Blend dstBlendAlpha;
	FNA3D_ColorWriteChannels colorWriteEnable;
	FNA3D_ColorWriteChannels colorWriteEnable1;
	FNA3D_ColorWriteChannels colorWriteEnable2;
	FNA3D_ColorWriteChannels colorWriteEnable3;
	int32_t multiSampleMask;

	/* Depth Stencil State */
	uint8_t zEnable;
	uint8_t zWriteEnable;
	FNA3D_CompareFunction depthFunc;
	uint8_t stencilEnable;
	int32_t stencilWriteMask;
	uint8_t separateStencilEnable;
	int32_t stencilRef;
	int32_t stencilMask;
	FNA3D_CompareFunction stencilFunc;
	FNA3D_StencilOperation stencilFail;
	FNA3D_StencilOperation stencilZFail;
	FNA3D_StencilOperation stencilPass;
	FNA3D_CompareFunction ccwStencilFunc;
	FNA3D_StencilOperation ccwStencilFail;
	FNA3D_StencilOperation ccwStencilZFail;
	FNA3D_StencilOperation ccwStencilPass;

	/* Rasterizer State */
	uint8_t scissorTestEnable;
	FNA3D_CullMode cullFrontFace;
	FNA3D_FillMode fillMode;
	float depthBias;
	float slopeScaleDepthBias;
	uint8_t multiSampleEnable;

	/* Viewport */
	FNA3D_Viewport viewport;
	FNA3D_Rect scissorRect;
	float depthRangeMin;
	float depthRangeMax;

	/* Textures */
	int32_t numTextureSlots;
	int32_t numVertexTextureSlots;
	int32_t vertexSamplerStart;
	OpenGLTexture *textures[MAX_TEXTURE_SAMPLERS + MAX_VERTEXTEXTURE_SAMPLERS];

	/* Buffer Binding Cache */
	GLuint currentVertexBuffer;
	GLuint currentIndexBuffer;

	/* ld, or LastDrawn, vertex attributes */
	int32_t ldBaseVertex;

	/* Render Targets */
	int32_t numAttachments;
	GLuint currentReadFramebuffer;
	GLuint currentDrawFramebuffer;
	GLuint targetFramebuffer;
	GLuint resolveFramebufferRead;
	GLuint resolveFramebufferDraw;
	GLuint currentAttachments[MAX_RENDERTARGET_BINDINGS];
	GLenum currentAttachmentTypes[MAX_RENDERTARGET_BINDINGS];
	int32_t currentDrawBuffers;
	GLenum drawBuffersArray[MAX_RENDERTARGET_BINDINGS + 2];
	GLuint currentRenderbuffer;
	FNA3D_DepthFormat currentDepthStencilFormat;
	GLuint attachments[MAX_RENDERTARGET_BINDINGS];
	GLenum attachmentTypes[MAX_RENDERTARGET_BINDINGS];

	/* Clear Cache */
	FNA3D_Vec4 currentClearColor;
	float currentClearDepth;
	int32_t currentClearStencil;

	/* Vertex Attributes */
	int32_t numVertexAttributes;
	OpenGLVertexAttribute attributes[MAX_VERTEX_ATTRIBUTES];
	uint8_t attributeEnabled[MAX_VERTEX_ATTRIBUTES];
	uint8_t previousAttributeEnabled[MAX_VERTEX_ATTRIBUTES];
	int32_t attributeDivisor[MAX_VERTEX_ATTRIBUTES];
	int32_t previousAttributeDivisor[MAX_VERTEX_ATTRIBUTES];

	/* MojoShader Interop */
	const char *shaderProfile;
	MOJOSHADER_glContext *shaderContext;
	MOJOSHADER_effect *currentEffect;
	const MOJOSHADER_effectTechnique *currentTechnique;
	uint32_t currentPass;
	uint8_t renderTargetBound;
	uint8_t effectApplied;

	/* Point Sprite Toggle */
	uint8_t togglePointSprite;

	/* Threading */
	SDL_ThreadID threadID;
	FNA3D_Command *commands;
	SDL_Mutex *commandsLock;
	OpenGLTexture *disposeTextures;
	SDL_Mutex *disposeTexturesLock;
	OpenGLRenderbuffer *disposeRenderbuffers;
	SDL_Mutex *disposeRenderbuffersLock;
	OpenGLBuffer *disposeVertexBuffers;
	SDL_Mutex *disposeVertexBuffersLock;
	OpenGLBuffer *disposeIndexBuffers;
	SDL_Mutex *disposeIndexBuffersLock;
	OpenGLEffect *disposeEffects;
	SDL_Mutex *disposeEffectsLock;
	OpenGLQuery *disposeQueries;
	SDL_Mutex *disposeQueriesLock;

	/* GL entry points */
	glfntype_glGetString glGetString; /* Loaded early! */
	#define GL_EXT(ext) \
		uint8_t supports_##ext;
	#define GL_PROC(ext, ret, func, parms) \
		glfntype_##func func;
	#define GL_PROC_EXT(ext, fallback, ret, func, parms) \
		glfntype_##func func;
	#include "FNA3D_Driver_OpenGL_glfuncs.h"
} OpenGLRenderer;

/* XNA->OpenGL Translation Arrays */

static int32_t XNAToGL_TextureFormat[] =
{
	GL_RGBA,			/* SurfaceFormat.Color */
	GL_RGB,				/* SurfaceFormat.Bgr565 */
	GL_BGRA,			/* SurfaceFormat.Bgra5551 */
	GL_BGRA,			/* SurfaceFormat.Bgra4444 */
	GL_COMPRESSED_TEXTURE_FORMATS,	/* SurfaceFormat.Dxt1 */
	GL_COMPRESSED_TEXTURE_FORMATS,	/* SurfaceFormat.Dxt3 */
	GL_COMPRESSED_TEXTURE_FORMATS,	/* SurfaceFormat.Dxt5 */
	GL_RG,				/* SurfaceFormat.NormalizedByte2 */
	GL_RGBA,			/* SurfaceFormat.NormalizedByte4 */
	GL_RGBA,			/* SurfaceFormat.Rgba1010102 */
	GL_RG,				/* SurfaceFormat.Rg32 */
	GL_RGBA,			/* SurfaceFormat.Rgba64 */
	GL_ALPHA,			/* SurfaceFormat.Alpha8 */
	GL_RED,				/* SurfaceFormat.Single */
	GL_RG,				/* SurfaceFormat.Vector2 */
	GL_RGBA,			/* SurfaceFormat.Vector4 */
	GL_RED,				/* SurfaceFormat.HalfSingle */
	GL_RG,				/* SurfaceFormat.HalfVector2 */
	GL_RGBA,			/* SurfaceFormat.HalfVector4 */
	GL_RGBA,			/* SurfaceFormat.HdrBlendable */
	GL_BGRA,			/* SurfaceFormat.ColorBgraEXT */
	GL_RGBA,			/* SurfaceFormat.ColorSrgbEXT */
	GL_COMPRESSED_TEXTURE_FORMATS,	/* SurfaceFormat.Dxt5SrgbEXT */
	GL_COMPRESSED_TEXTURE_FORMATS,	/* SurfaceFormat.Bc7EXT */
	GL_COMPRESSED_TEXTURE_FORMATS,	/* SurfaceFormat.Bc7SrgbEXT */
	GL_RED,				/* SurfaceFormat.NormalizedByteEXT */
	GL_RED,				/* SurfaceFormat.NormalizedUShortEXT */
};

static int32_t XNAToGL_TextureInternalFormat[] =
{
	GL_RGBA8,				/* SurfaceFormat.Color */
	GL_RGB8,				/* SurfaceFormat.Bgr565 */
	GL_RGB5_A1,				/* SurfaceFormat.Bgra5551 */
	GL_RGBA4,				/* SurfaceFormat.Bgra4444 */
	GL_COMPRESSED_RGBA_S3TC_DXT1_EXT,	/* SurfaceFormat.Dxt1 */
	GL_COMPRESSED_RGBA_S3TC_DXT3_EXT,	/* SurfaceFormat.Dxt3 */
	GL_COMPRESSED_RGBA_S3TC_DXT5_EXT,	/* SurfaceFormat.Dxt5 */
	GL_RG8,					/* SurfaceFormat.NormalizedByte2 */
	GL_RGBA8,				/* SurfaceFormat.NormalizedByte4 */
	GL_RGB10_A2_EXT,			/* SurfaceFormat.Rgba1010102 */
	GL_RG16,				/* SurfaceFormat.Rg32 */
	GL_RGBA16,				/* SurfaceFormat.Rgba64 */
	GL_ALPHA,				/* SurfaceFormat.Alpha8 */
	GL_R32F,				/* SurfaceFormat.Single */
	GL_RG32F,				/* SurfaceFormat.Vector2 */
	GL_RGBA32F,				/* SurfaceFormat.Vector4 */
	GL_R16F,				/* SurfaceFormat.HalfSingle */
	GL_RG16F,				/* SurfaceFormat.HalfVector2 */
	GL_RGBA16F,				/* SurfaceFormat.HalfVector4 */
	GL_RGBA16F,				/* SurfaceFormat.HdrBlendable */
	GL_RGBA8,				/* SurfaceFormat.ColorBgraEXT */
	GL_SRGB_ALPHA_EXT,			/* SurfaceFormat.ColorSrgbEXT */
	GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT,	/* SurfaceFormat.Dxt5SrgbEXT */
	GL_COMPRESSED_RGBA_BPTC_UNORM_EXT,	/* SurfaceFormat.BC7EXT */
	GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM_EXT,/* SurfaceFormat.BC7SrgbEXT */
	GL_R8,				/* SurfaceFormat.NormalizedByteEXT */
	GL_R16,				/* SurfaceFormat.NormalizedUShortEXT */
};

static int32_t XNAToGL_TextureDataType[] =
{
	GL_UNSIGNED_BYTE,		/* SurfaceFormat.Color */
	GL_UNSIGNED_SHORT_5_6_5,	/* SurfaceFormat.Bgr565 */
	GL_UNSIGNED_SHORT_5_5_5_1_REV,	/* SurfaceFormat.Bgra5551 */
	GL_UNSIGNED_SHORT_4_4_4_4_REV,	/* SurfaceFormat.Bgra4444 */
	GL_ZERO,			/* NOPE */
	GL_ZERO,			/* NOPE */
	GL_ZERO,			/* NOPE */
	GL_BYTE,			/* SurfaceFormat.NormalizedByte2 */
	GL_BYTE,			/* SurfaceFormat.NormalizedByte4 */
	GL_UNSIGNED_INT_2_10_10_10_REV,	/* SurfaceFormat.Rgba1010102 */
	GL_UNSIGNED_SHORT,		/* SurfaceFormat.Rg32 */
	GL_UNSIGNED_SHORT,		/* SurfaceFormat.Rgba64 */
	GL_UNSIGNED_BYTE,		/* SurfaceFormat.Alpha8 */
	GL_FLOAT,			/* SurfaceFormat.Single */
	GL_FLOAT,			/* SurfaceFormat.Vector2 */
	GL_FLOAT,			/* SurfaceFormat.Vector4 */
	GL_HALF_FLOAT,			/* SurfaceFormat.HalfSingle */
	GL_HALF_FLOAT,			/* SurfaceFormat.HalfVector2 */
	GL_HALF_FLOAT,			/* SurfaceFormat.HalfVector4 */
	GL_HALF_FLOAT,			/* SurfaceFormat.HdrBlendable */
	GL_UNSIGNED_BYTE,		/* SurfaceFormat.ColorBgraEXT */
	GL_UNSIGNED_BYTE,		/* SurfaceFormat.ColorSrgbEXT */
	GL_ZERO,			/* NOPE */
	GL_ZERO,			/* NOPE */
	GL_ZERO,			/* NOPE */
	GL_UNSIGNED_BYTE,		/* SurfaceFormat.NormalizedByteEXT */
	GL_UNSIGNED_SHORT,		/* SurfaceFormat.NormalizedUShortEXT */
};

static int32_t XNAToGL_BlendMode[] =
{
	GL_ONE,				/* Blend.One */
	GL_ZERO,			/* Blend.Zero */
	GL_SRC_COLOR,			/* Blend.SourceColor */
	GL_ONE_MINUS_SRC_COLOR,		/* Blend.InverseSourceColor */
	GL_SRC_ALPHA,			/* Blend.SourceAlpha */
	GL_ONE_MINUS_SRC_ALPHA,		/* Blend.InverseSourceAlpha */
	GL_DST_COLOR,			/* Blend.DestinationColor */
	GL_ONE_MINUS_DST_COLOR,		/* Blend.InverseDestinationColor */
	GL_DST_ALPHA,			/* Blend.DestinationAlpha */
	GL_ONE_MINUS_DST_ALPHA,		/* Blend.InverseDestinationAlpha */
	GL_CONSTANT_COLOR,		/* Blend.BlendFactor */
	GL_ONE_MINUS_CONSTANT_COLOR,	/* Blend.InverseBlendFactor */
	GL_SRC_ALPHA_SATURATE		/* Blend.SourceAlphaSaturation */
};

static int32_t XNAToGL_BlendEquation[] =
{
	GL_FUNC_ADD,			/* BlendFunction.Add */
	GL_FUNC_SUBTRACT,		/* BlendFunction.Subtract */
	GL_FUNC_REVERSE_SUBTRACT,	/* BlendFunction.ReverseSubtract */
	GL_MAX,				/* BlendFunction.Max */
	GL_MIN				/* BlendFunction.Min */
};

static int32_t XNAToGL_CompareFunc[] =
{
	GL_ALWAYS,	/* CompareFunction.Always */
	GL_NEVER,	/* CompareFunction.Never */
	GL_LESS,	/* CompareFunction.Less */
	GL_LEQUAL,	/* CompareFunction.LessEqual */
	GL_EQUAL,	/* CompareFunction.Equal */
	GL_GEQUAL,	/* CompareFunction.GreaterEqual */
	GL_GREATER,	/* CompareFunction.Greater */
	GL_NOTEQUAL	/* CompareFunction.NotEqual */
};

static int32_t XNAToGL_GLStencilOp[] =
{
	GL_KEEP,	/* StencilOperation.Keep */
	GL_ZERO,	/* StencilOperation.Zero */
	GL_REPLACE,	/* StencilOperation.Replace */
	GL_INCR_WRAP,	/* StencilOperation.Increment */
	GL_DECR_WRAP,	/* StencilOperation.Decrement */
	GL_INCR,	/* StencilOperation.IncrementSaturation */
	GL_DECR,	/* StencilOperation.DecrementSaturation */
	GL_INVERT	/* StencilOperation.Invert */
};

static int32_t XNAToGL_FrontFace[] =
{
	GL_ZERO,	/* NOPE */
	GL_CW,		/* CullMode.CullClockwiseFace */
	GL_CCW		/* CullMode.CullCounterClockwiseFace */
};

static int32_t XNAToGL_GLFillMode[] =
{
	GL_FILL,	/* FillMode.Solid */
	GL_LINE		/* FillMode.WireFrame */
};

static int32_t XNAToGL_Wrap[] =
{
	GL_REPEAT,		/* TextureAddressMode.Wrap */
	GL_CLAMP_TO_EDGE,	/* TextureAddressMode.Clamp */
	GL_MIRRORED_REPEAT	/* TextureAddressMode.Mirror */
};

static int32_t XNAToGL_MagFilter[] =
{
	GL_LINEAR,	/* TextureFilter.Linear */
	GL_NEAREST,	/* TextureFilter.Point */
	GL_LINEAR,	/* TextureFilter.Anisotropic */
	GL_LINEAR,	/* TextureFilter.LinearMipPoint */
	GL_NEAREST,	/* TextureFilter.PointMipLinear */
	GL_NEAREST,	/* TextureFilter.MinLinearMagPointMipLinear */
	GL_NEAREST,	/* TextureFilter.MinLinearMagPointMipPoint */
	GL_LINEAR,	/* TextureFilter.MinPointMagLinearMipLinear */
	GL_LINEAR	/* TextureFilter.MinPointMagLinearMipPoint */
};

static int32_t XNAToGL_MinMipFilter[] =
{
	GL_LINEAR_MIPMAP_LINEAR,	/* TextureFilter.Linear */
	GL_NEAREST_MIPMAP_NEAREST,	/* TextureFilter.Point */
	GL_LINEAR_MIPMAP_LINEAR,	/* TextureFilter.Anisotropic */
	GL_LINEAR_MIPMAP_NEAREST,	/* TextureFilter.LinearMipPoint */
	GL_NEAREST_MIPMAP_LINEAR,	/* TextureFilter.PointMipLinear */
	GL_LINEAR_MIPMAP_LINEAR,	/* TextureFilter.MinLinearMagPointMipLinear */
	GL_LINEAR_MIPMAP_NEAREST,	/* TextureFilter.MinLinearMagPointMipPoint */
	GL_NEAREST_MIPMAP_LINEAR,	/* TextureFilter.MinPointMagLinearMipLinear */
	GL_NEAREST_MIPMAP_NEAREST	/* TextureFilter.MinPointMagLinearMipPoint */
};

static int32_t XNAToGL_MinFilter[] =
{
	GL_LINEAR,	/* TextureFilter.Linear */
	GL_NEAREST,	/* TextureFilter.Point */
	GL_LINEAR,	/* TextureFilter.Anisotropic */
	GL_LINEAR,	/* TextureFilter.LinearMipPoint */
	GL_NEAREST,	/* TextureFilter.PointMipLinear */
	GL_LINEAR,	/* TextureFilter.MinLinearMagPointMipLinear */
	GL_LINEAR,	/* TextureFilter.MinLinearMagPointMipPoint */
	GL_NEAREST,	/* TextureFilter.MinPointMagLinearMipLinear */
	GL_NEAREST	/* TextureFilter.MinPointMagLinearMipPoint */
};

#if 0 /* Unused */
static int32_t XNAToGL_DepthStencilAttachment[] =
{
	GL_ZERO,			/* NOPE */
	GL_DEPTH_ATTACHMENT,		/* DepthFormat.Depth16 */
	GL_DEPTH_ATTACHMENT,		/* DepthFormat.Depth24 */
	GL_DEPTH_STENCIL_ATTACHMENT	/* DepthFormat.Depth24Stencil8 */
};
#endif

static int32_t XNAToGL_DepthStorage[] =
{
	GL_ZERO,		/* NOPE */
	GL_DEPTH_COMPONENT16,	/* DepthFormat.Depth16 */
	GL_DEPTH_COMPONENT24,	/* DepthFormat.Depth24 */
	GL_DEPTH24_STENCIL8	/* DepthFormat.Depth24Stencil8 */
};

static float XNAToGL_DepthBiasScale[] =
{
	0.0f,				/* DepthFormat.None */
	(float) ((1 << 16) - 1),	/* DepthFormat.Depth16 */
	(float) ((1 << 24) - 1),	/* DepthFormat.Depth24 */
	(float) ((1 << 24) - 1)		/* DepthFormat.Depth24Stencil8 */
};

static int32_t XNAToGL_VertexAttribSize[] =
{
	1,	/* VertexElementFormat.Single */
	2,	/* VertexElementFormat.Vector2 */
	3,	/* VertexElementFormat.Vector3 */
	4,	/* VertexElementFormat.Vector4 */
	4,	/* VertexElementFormat.Color */
	4,	/* VertexElementFormat.Byte4 */
	2,	/* VertexElementFormat.Short2 */
	4,	/* VertexElementFormat.Short4 */
	2,	/* VertexElementFormat.NormalizedShort2 */
	4,	/* VertexElementFormat.NormalizedShort4 */
	2,	/* VertexElementFormat.HalfVector2 */
	4	/* VertexElementFormat.HalfVector4 */
};

static int32_t XNAToGL_VertexAttribType[] =
{
	GL_FLOAT,		/* VertexElementFormat.Single */
	GL_FLOAT,		/* VertexElementFormat.Vector2 */
	GL_FLOAT,		/* VertexElementFormat.Vector3 */
	GL_FLOAT,		/* VertexElementFormat.Vector4 */
	GL_UNSIGNED_BYTE,	/* VertexElementFormat.Color */
	GL_UNSIGNED_BYTE,	/* VertexElementFormat.Byte4 */
	GL_SHORT,		/* VertexElementFormat.Short2 */
	GL_SHORT,		/* VertexElementFormat.Short4 */
	GL_SHORT,		/* VertexElementFormat.NormalizedShort2 */
	GL_SHORT,		/* VertexElementFormat.NormalizedShort4 */
	GL_HALF_FLOAT,		/* VertexElementFormat.HalfVector2 */
	GL_HALF_FLOAT		/* VertexElementFormat.HalfVector4 */
};

static uint8_t XNAToGL_VertexAttribNormalized(FNA3D_VertexElement *element)
{
	return (	element->vertexElementUsage == FNA3D_VERTEXELEMENTUSAGE_COLOR ||
			element->vertexElementFormat == FNA3D_VERTEXELEMENTFORMAT_COLOR ||
			element->vertexElementFormat == FNA3D_VERTEXELEMENTFORMAT_NORMALIZEDSHORT2 ||
			element->vertexElementFormat == FNA3D_VERTEXELEMENTFORMAT_NORMALIZEDSHORT4	);
}

static int32_t XNAToGL_IndexType[] =
{
	GL_UNSIGNED_SHORT,	/* IndexElementSize.SixteenBits */
	GL_UNSIGNED_INT		/* IndexElementSize.ThirtyTwoBits */
};

static int32_t XNAToGL_Primitive[] =
{
	GL_TRIANGLES,		/* PrimitiveType.TriangleList */
	GL_TRIANGLE_STRIP,	/* PrimitiveType.TriangleStrip */
	GL_LINES,		/* PrimitiveType.LineList */
	GL_LINE_STRIP,		/* PrimitiveType.LineStrip */
	GL_POINTS		/* PrimitiveType.PointListEXT */
};

/* Threading Support */

struct FNA3D_Command
{
	#define FNA3D_COMMAND_CREATEEFFECT 0
	#define FNA3D_COMMAND_CLONEEFFECT 1
	#define FNA3D_COMMAND_GENVERTEXBUFFER 2
	#define FNA3D_COMMAND_GENINDEXBUFFER 3
	#define FNA3D_COMMAND_SETVERTEXBUFFERDATA 4
	#define FNA3D_COMMAND_SETINDEXBUFFERDATA 5
	#define FNA3D_COMMAND_GETVERTEXBUFFERDATA 6
	#define FNA3D_COMMAND_GETINDEXBUFFERDATA 7
	#define FNA3D_COMMAND_CREATETEXTURE2D 8
	#define FNA3D_COMMAND_CREATETEXTURE3D 9
	#define FNA3D_COMMAND_CREATETEXTURECUBE 10
	#define FNA3D_COMMAND_SETTEXTUREDATA2D 11
	#define FNA3D_COMMAND_SETTEXTUREDATA3D 12
	#define FNA3D_COMMAND_SETTEXTUREDATACUBE 13
	#define FNA3D_COMMAND_GETTEXTUREDATA2D 14
	#define FNA3D_COMMAND_GETTEXTUREDATA3D 15
	#define FNA3D_COMMAND_GETTEXTUREDATACUBE 16
	#define FNA3D_COMMAND_GENCOLORRENDERBUFFER 17
	#define FNA3D_COMMAND_GENDEPTHRENDERBUFFER 18
	uint8_t type;
	FNA3DNAMELESS union
	{
		struct
		{
			uint8_t *effectCode;
			uint32_t effectCodeLength;
			FNA3D_Effect **effect;
			MOJOSHADER_effect **effectData;
		} createEffect;

		struct
		{
			FNA3D_Effect *cloneSource;
			FNA3D_Effect **effect;
			MOJOSHADER_effect **effectData;
		} cloneEffect;

		struct
		{
			uint8_t dynamic;
			FNA3D_BufferUsage usage;
			int32_t sizeInBytes;
			FNA3D_Buffer *retval;
		} genVertexBuffer;

		struct
		{
			uint8_t dynamic;
			FNA3D_BufferUsage usage;
			int32_t sizeInBytes;
			FNA3D_Buffer *retval;
		} genIndexBuffer;

		struct
		{
			FNA3D_Buffer *buffer;
			int32_t offsetInBytes;
			void* data;
			int32_t elementCount;
			int32_t elementSizeInBytes;
			int32_t vertexStride;
			FNA3D_SetDataOptions options;
		} setVertexBufferData;

		struct
		{
			FNA3D_Buffer *buffer;
			int32_t offsetInBytes;
			void* data;
			int32_t dataLength;
			FNA3D_SetDataOptions options;
		} setIndexBufferData;

		struct
		{
			FNA3D_Buffer *buffer;
			int32_t offsetInBytes;
			void* data;
			int32_t elementCount;
			int32_t elementSizeInBytes;
			int32_t vertexStride;
		} getVertexBufferData;

		struct
		{
			FNA3D_Buffer *buffer;
			int32_t offsetInBytes;
			void* data;
			int32_t dataLength;
		} getIndexBufferData;

		struct
		{
			FNA3D_SurfaceFormat format;
			int32_t width;
			int32_t height;
			int32_t levelCount;
			uint8_t isRenderTarget;
			FNA3D_Texture *retval;
		} createTexture2D;

		struct
		{
			FNA3D_SurfaceFormat format;
			int32_t width;
			int32_t height;
			int32_t depth;
			int32_t levelCount;
			FNA3D_Texture *retval;
		} createTexture3D;

		struct
		{
			FNA3D_SurfaceFormat format;
			int32_t size;
			int32_t levelCount;
			uint8_t isRenderTarget;
			FNA3D_Texture *retval;
		} createTextureCube;

		struct
		{
			FNA3D_Texture *texture;
			int32_t x;
			int32_t y;
			int32_t w;
			int32_t h;
			int32_t level;
			void* data;
			int32_t dataLength;
		} setTextureData2D;

		struct
		{
			FNA3D_Texture *texture;
			int32_t x;
			int32_t y;
			int32_t z;
			int32_t w;
			int32_t h;
			int32_t d;
			int32_t level;
			void* data;
			int32_t dataLength;
		} setTextureData3D;

		struct
		{
			FNA3D_Texture *texture;
			int32_t x;
			int32_t y;
			int32_t w;
			int32_t h;
			FNA3D_CubeMapFace cubeMapFace;
			int32_t level;
			void* data;
			int32_t dataLength;
		} setTextureDataCube;

		struct
		{
			FNA3D_Texture *texture;
			int32_t x;
			int32_t y;
			int32_t w;
			int32_t h;
			int32_t level;
			void* data;
			int32_t dataLength;
		} getTextureData2D;

		struct
		{
			FNA3D_Texture *texture;
			int32_t x;
			int32_t y;
			int32_t z;
			int32_t w;
			int32_t h;
			int32_t d;
			int32_t level;
			void* data;
			int32_t dataLength;
		} getTextureData3D;

		struct
		{
			FNA3D_Texture *texture;
			int32_t x;
			int32_t y;
			int32_t w;
			int32_t h;
			FNA3D_CubeMapFace cubeMapFace;
			int32_t level;
			void* data;
			int32_t dataLength;
		} getTextureDataCube;

		struct
		{
			int32_t width;
			int32_t height;
			FNA3D_SurfaceFormat format;
			int32_t multiSampleCount;
			FNA3D_Texture *texture;
			FNA3D_Renderbuffer *retval;
		} genColorRenderbuffer;

		struct
		{
			int32_t width;
			int32_t height;
			FNA3D_DepthFormat format;
			int32_t multiSampleCount;
			FNA3D_Renderbuffer *retval;
		} genDepthStencilRenderbuffer;
	};
	SDL_Semaphore *semaphore;
	FNA3D_Command *next;
};

static void FNA3D_ExecuteCommand(
	FNA3D_Device *device,
	FNA3D_Command *cmd
) {
	switch (cmd->type)
	{
		case FNA3D_COMMAND_CREATEEFFECT:
			device->CreateEffect(
				device->driverData,
				cmd->createEffect.effectCode,
				cmd->createEffect.effectCodeLength,
				cmd->createEffect.effect,
				cmd->createEffect.effectData
			);
			break;
		case FNA3D_COMMAND_CLONEEFFECT:
			device->CloneEffect(
				device->driverData,
				cmd->cloneEffect.cloneSource,
				cmd->cloneEffect.effect,
				cmd->cloneEffect.effectData
			);
			break;
		case FNA3D_COMMAND_GENVERTEXBUFFER:
			cmd->genVertexBuffer.retval = device->GenVertexBuffer(
				device->driverData,
				cmd->genVertexBuffer.dynamic,
				cmd->genVertexBuffer.usage,
				cmd->genVertexBuffer.sizeInBytes
			);
			break;
		case FNA3D_COMMAND_GENINDEXBUFFER:
			cmd->genIndexBuffer.retval = device->GenIndexBuffer(
				device->driverData,
				cmd->genIndexBuffer.dynamic,
				cmd->genIndexBuffer.usage,
				cmd->genIndexBuffer.sizeInBytes
			);
			break;
		case FNA3D_COMMAND_SETVERTEXBUFFERDATA:
			device->SetVertexBufferData(
				device->driverData,
				cmd->setVertexBufferData.buffer,
				cmd->setVertexBufferData.offsetInBytes,
				cmd->setVertexBufferData.data,
				cmd->setVertexBufferData.elementCount,
				cmd->setVertexBufferData.elementSizeInBytes,
				cmd->setVertexBufferData.vertexStride,
				cmd->setVertexBufferData.options
			);
			break;
		case FNA3D_COMMAND_SETINDEXBUFFERDATA:
			device->SetIndexBufferData(
				device->driverData,
				cmd->setIndexBufferData.buffer,
				cmd->setIndexBufferData.offsetInBytes,
				cmd->setIndexBufferData.data,
				cmd->setIndexBufferData.dataLength,
				cmd->setIndexBufferData.options
			);
			break;
		case FNA3D_COMMAND_GETVERTEXBUFFERDATA:
			device->GetVertexBufferData(
				device->driverData,
				cmd->getVertexBufferData.buffer,
				cmd->getVertexBufferData.offsetInBytes,
				cmd->getVertexBufferData.data,
				cmd->getVertexBufferData.elementCount,
				cmd->getVertexBufferData.elementSizeInBytes,
				cmd->getVertexBufferData.vertexStride
			);
			break;
		case FNA3D_COMMAND_GETINDEXBUFFERDATA:
			device->GetIndexBufferData(
				device->driverData,
				cmd->getIndexBufferData.buffer,
				cmd->getIndexBufferData.offsetInBytes,
				cmd->getIndexBufferData.data,
				cmd->getIndexBufferData.dataLength
			);
			break;
		case FNA3D_COMMAND_CREATETEXTURE2D:
			cmd->createTexture2D.retval = device->CreateTexture2D(
				device->driverData,
				cmd->createTexture2D.format,
				cmd->createTexture2D.width,
				cmd->createTexture2D.height,
				cmd->createTexture2D.levelCount,
				cmd->createTexture2D.isRenderTarget
			);
			break;
		case FNA3D_COMMAND_CREATETEXTURE3D:
			cmd->createTexture3D.retval = device->CreateTexture3D(
				device->driverData,
				cmd->createTexture3D.format,
				cmd->createTexture3D.width,
				cmd->createTexture3D.height,
				cmd->createTexture3D.depth,
				cmd->createTexture3D.levelCount
			);
			break;
		case FNA3D_COMMAND_CREATETEXTURECUBE:
			cmd->createTextureCube.retval = device->CreateTextureCube(
				device->driverData,
				cmd->createTextureCube.format,
				cmd->createTextureCube.size,
				cmd->createTextureCube.levelCount,
				cmd->createTextureCube.isRenderTarget
			);
			break;
		case FNA3D_COMMAND_SETTEXTUREDATA2D:
			device->SetTextureData2D(
				device->driverData,
				cmd->setTextureData2D.texture,
				cmd->setTextureData2D.x,
				cmd->setTextureData2D.y,
				cmd->setTextureData2D.w,
				cmd->setTextureData2D.h,
				cmd->setTextureData2D.level,
				cmd->setTextureData2D.data,
				cmd->setTextureData2D.dataLength
			);
			break;
		case FNA3D_COMMAND_SETTEXTUREDATA3D:
			device->SetTextureData3D(
				device->driverData,
				cmd->setTextureData3D.texture,
				cmd->setTextureData3D.x,
				cmd->setTextureData3D.y,
				cmd->setTextureData3D.z,
				cmd->setTextureData3D.w,
				cmd->setTextureData3D.h,
				cmd->setTextureData3D.d,
				cmd->setTextureData3D.level,
				cmd->setTextureData3D.data,
				cmd->setTextureData3D.dataLength
			);
			break;
		case FNA3D_COMMAND_SETTEXTUREDATACUBE:
			device->SetTextureDataCube(
				device->driverData,
				cmd->setTextureDataCube.texture,
				cmd->setTextureDataCube.x,
				cmd->setTextureDataCube.y,
				cmd->setTextureDataCube.w,
				cmd->setTextureDataCube.h,
				cmd->setTextureDataCube.cubeMapFace,
				cmd->setTextureDataCube.level,
				cmd->setTextureDataCube.data,
				cmd->setTextureDataCube.dataLength
			);
			break;
		case FNA3D_COMMAND_GETTEXTUREDATA2D:
			device->GetTextureData2D(
				device->driverData,
				cmd->getTextureData2D.texture,
				cmd->getTextureData2D.x,
				cmd->getTextureData2D.y,
				cmd->getTextureData2D.w,
				cmd->getTextureData2D.h,
				cmd->getTextureData2D.level,
				cmd->getTextureData2D.data,
				cmd->getTextureData2D.dataLength
			);
			break;
		case FNA3D_COMMAND_GETTEXTUREDATA3D:
			device->GetTextureData3D(
				device->driverData,
				cmd->getTextureData3D.texture,
				cmd->getTextureData3D.x,
				cmd->getTextureData3D.y,
				cmd->getTextureData3D.z,
				cmd->getTextureData3D.w,
				cmd->getTextureData3D.h,
				cmd->getTextureData3D.d,
				cmd->getTextureData3D.level,
				cmd->getTextureData3D.data,
				cmd->getTextureData3D.dataLength
			);
			break;
		case FNA3D_COMMAND_GETTEXTUREDATACUBE:
			device->GetTextureDataCube(
				device->driverData,
				cmd->getTextureDataCube.texture,
				cmd->getTextureDataCube.x,
				cmd->getTextureDataCube.y,
				cmd->getTextureDataCube.w,
				cmd->getTextureDataCube.h,
				cmd->getTextureDataCube.cubeMapFace,
				cmd->getTextureDataCube.level,
				cmd->getTextureDataCube.data,
				cmd->getTextureDataCube.dataLength
			);
			break;
		case FNA3D_COMMAND_GENCOLORRENDERBUFFER:
			cmd->genColorRenderbuffer.retval = device->GenColorRenderbuffer(
				device->driverData,
				cmd->genColorRenderbuffer.width,
				cmd->genColorRenderbuffer.height,
				cmd->genColorRenderbuffer.format,
				cmd->genColorRenderbuffer.multiSampleCount,
				cmd->genColorRenderbuffer.texture
			);
			break;
		case FNA3D_COMMAND_GENDEPTHRENDERBUFFER:
			cmd->genDepthStencilRenderbuffer.retval = device->GenDepthStencilRenderbuffer(
				device->driverData,
				cmd->genDepthStencilRenderbuffer.width,
				cmd->genDepthStencilRenderbuffer.height,
				cmd->genDepthStencilRenderbuffer.format,
				cmd->genDepthStencilRenderbuffer.multiSampleCount
			);
			break;
		default:
			FNA3D_LogError(
				"Cannot execute unknown command (value = %d)",
				cmd->type
			);
			break;
	}
}

/* Inline Functions */

static inline void BindReadFramebuffer(OpenGLRenderer *renderer, GLuint handle)
{
	if (handle != renderer->currentReadFramebuffer)
	{
		renderer->glBindFramebuffer(GL_READ_FRAMEBUFFER, handle);
		renderer->currentReadFramebuffer = handle;
	}
}

static inline void BindDrawFramebuffer(OpenGLRenderer *renderer, GLuint handle)
{
	if (handle != renderer->currentDrawFramebuffer)
	{
		renderer->glBindFramebuffer(GL_DRAW_FRAMEBUFFER, handle);
		renderer->currentDrawFramebuffer = handle;
	}
}

static inline void BindFramebuffer(OpenGLRenderer *renderer, GLuint handle)
{
	if (	renderer->currentReadFramebuffer != handle &&
		renderer->currentDrawFramebuffer != handle	)
	{
		renderer->glBindFramebuffer(GL_FRAMEBUFFER, handle);
		renderer->currentReadFramebuffer = handle;
		renderer->currentDrawFramebuffer = handle;
	}
	else if (renderer->currentReadFramebuffer != handle)
	{
		renderer->glBindFramebuffer(GL_READ_FRAMEBUFFER, handle);
		renderer->currentReadFramebuffer = handle;
	}
	else if (renderer->currentDrawFramebuffer != handle)
	{
		renderer->glBindFramebuffer(GL_DRAW_FRAMEBUFFER, handle);
		renderer->currentDrawFramebuffer = handle;
	}
}

static inline void BindTexture(OpenGLRenderer *renderer, OpenGLTexture* tex)
{
	if (tex->target != renderer->textures[0]->target)
	{
		renderer->glBindTexture(renderer->textures[0]->target, 0);
	}
	if (renderer->textures[0] != tex)
	{
		renderer->glBindTexture(tex->target, tex->handle);
	}
	renderer->textures[0] = tex;
}

static inline void BindVertexBuffer(OpenGLRenderer *renderer, GLuint handle)
{
	if (handle != renderer->currentVertexBuffer)
	{
		renderer->glBindBuffer(GL_ARRAY_BUFFER, handle);
		renderer->currentVertexBuffer = handle;
	}
}

static inline void BindIndexBuffer(OpenGLRenderer *renderer, GLuint handle)
{
	if (handle != renderer->currentIndexBuffer)
	{
		renderer->glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, handle);
		renderer->currentIndexBuffer = handle;
	}
}

static inline void ToggleGLState(
	OpenGLRenderer *renderer,
	GLenum feature,
	uint8_t enable
) {
	if (enable)
	{
		renderer->glEnable(feature);
	}
	else
	{
		renderer->glDisable(feature);
	}
}

static inline void ApplySRGBFlag(OpenGLRenderer *renderer, uint8_t state)
{
	if (state == renderer->srgbEnabled)
	{
		return;
	}

	renderer->srgbEnabled = state;
	ToggleGLState(renderer, GL_FRAMEBUFFER_SRGB_EXT, state);
}

static inline void ForceToMainThread(
	OpenGLRenderer *renderer,
	FNA3D_Command *command
) {
	FNA3D_Command *curr;
	command->semaphore = SDL_CreateSemaphore(0);

	SDL_LockMutex(renderer->commandsLock);
	LinkedList_Add(renderer->commands, command, curr);
	SDL_UnlockMutex(renderer->commandsLock);

	SDL_WaitSemaphore(command->semaphore);
	SDL_DestroySemaphore(command->semaphore);
}

/* Forward Declarations for Internal Functions */

static void OPENGL_INTERNAL_CreateBackbuffer(
	OpenGLRenderer *renderer,
	FNA3D_PresentationParameters *parameters
);
static void OPENGL_INTERNAL_DisposeBackbuffer(OpenGLRenderer *renderer);
static void OPENGL_INTERNAL_DestroyTexture(
	OpenGLRenderer *renderer,
	OpenGLTexture *texture
);
static void OPENGL_INTERNAL_DestroyRenderbuffer(
	OpenGLRenderer *renderer,
	OpenGLRenderbuffer *renderbuffer
);
static void OPENGL_INTERNAL_DestroyVertexBuffer(
	OpenGLRenderer *renderer,
	OpenGLBuffer *buffer
);
static void OPENGL_INTERNAL_DestroyIndexBuffer(
	OpenGLRenderer *renderer,
	OpenGLBuffer *buffer
);
static void OPENGL_INTERNAL_DestroyEffect(
	OpenGLRenderer *renderer,
	OpenGLEffect *effect
);
static void OPENGL_INTERNAL_DestroyQuery(
	OpenGLRenderer *renderer,
	OpenGLQuery *query
);
static void OPENGL_GetBackbufferSize(
	FNA3D_Renderer *driverData,
	int32_t *w,
	int32_t *h
);

/* Renderer Implementation */

/* Quit */

static void OPENGL_DestroyDevice(FNA3D_Device *device)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) device->driverData;

	if (renderer->useCoreProfile)
	{
		renderer->glBindVertexArray(0);
		renderer->glDeleteVertexArrays(1, &renderer->vao);
	}

	renderer->glDeleteFramebuffers(1, &renderer->resolveFramebufferRead);
	renderer->resolveFramebufferRead = 0;
	renderer->glDeleteFramebuffers(1, &renderer->resolveFramebufferDraw);
	renderer->resolveFramebufferDraw = 0;
	renderer->glDeleteFramebuffers(1, &renderer->targetFramebuffer);
	renderer->targetFramebuffer = 0;

	if (renderer->backbuffer->type == BACKBUFFER_TYPE_OPENGL)
	{
		OPENGL_INTERNAL_DisposeBackbuffer(renderer);
	}
	SDL_free(renderer->backbuffer);
	renderer->backbuffer = NULL;

	MOJOSHADER_glMakeContextCurrent(NULL);
	MOJOSHADER_glDestroyContext(renderer->shaderContext);

	SDL_DestroyMutex(renderer->commandsLock);
	SDL_DestroyMutex(renderer->disposeTexturesLock);
	SDL_DestroyMutex(renderer->disposeRenderbuffersLock);
	SDL_DestroyMutex(renderer->disposeVertexBuffersLock);
	SDL_DestroyMutex(renderer->disposeIndexBuffersLock);
	SDL_DestroyMutex(renderer->disposeEffectsLock);
	SDL_DestroyMutex(renderer->disposeQueriesLock);

#ifdef USE_SDL3
	SDL_GL_DestroyContext(renderer->context);
#else
	SDL_GL_DeleteContext(renderer->context);
#endif

	SDL_free(renderer);
	SDL_free(device);
}

/* Presentation */

static inline void ExecuteCommands(OpenGLRenderer *renderer)
{
	FNA3D_Command *cmd, *next;

	SDL_LockMutex(renderer->commandsLock);
	cmd = renderer->commands;
	while (cmd != NULL)
	{
		FNA3D_ExecuteCommand(
			renderer->parentDevice,
			cmd
		);
		next = cmd->next;
		SDL_SignalSemaphore(cmd->semaphore);
		cmd = next;
	}
	renderer->commands = NULL; /* No heap memory to free! -caleb */
	SDL_UnlockMutex(renderer->commandsLock);
}

static inline void DisposeResources(OpenGLRenderer *renderer)
{
	OpenGLTexture *tex, *texNext;
	OpenGLEffect *eff, *effNext;
	OpenGLBuffer *buf, *bufNext;
	OpenGLRenderbuffer *ren, *renNext;
	OpenGLQuery *qry, *qryNext;

	/* All heap allocations are freed by func! -caleb */
	#define DISPOSE(prefix, list, func) \
		SDL_LockMutex(list##Lock); \
		prefix = list; \
		while (prefix != NULL) \
		{ \
			prefix##Next = prefix->next; \
			OPENGL_INTERNAL_##func(renderer, prefix); \
			prefix = prefix##Next; \
		} \
		list = NULL; \
		SDL_UnlockMutex(list##Lock);

	DISPOSE(tex, renderer->disposeTextures, DestroyTexture)
	DISPOSE(ren, renderer->disposeRenderbuffers, DestroyRenderbuffer)
	DISPOSE(buf, renderer->disposeVertexBuffers, DestroyVertexBuffer)
	DISPOSE(buf, renderer->disposeIndexBuffers, DestroyIndexBuffer)
	DISPOSE(eff, renderer->disposeEffects, DestroyEffect)
	DISPOSE(qry, renderer->disposeQueries, DestroyQuery)

	#undef DISPOSE
}

static void OPENGL_SwapBuffers(
	FNA3D_Renderer *driverData,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
) {
	int32_t srcX, srcY, srcW, srcH;
	int32_t dstX, dstY, dstW, dstH;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;

	/* Only the faux-backbuffer supports presenting
	 * specific regions given to Present().
	 * -flibit
	 */
	if (renderer->backbuffer->type == BACKBUFFER_TYPE_OPENGL)
	{
		if (sourceRectangle != NULL)
		{
			srcX = sourceRectangle->x;
			srcY = sourceRectangle->y;
			srcW = sourceRectangle->w;
			srcH = sourceRectangle->h;
		}
		else
		{
			srcX = 0;
			srcY = 0;
			srcW = renderer->backbuffer->width;
			srcH = renderer->backbuffer->height;
		}
		if (destinationRectangle != NULL)
		{
			dstX = destinationRectangle->x;
			dstY = destinationRectangle->y;
			dstW = destinationRectangle->w;
			dstH = destinationRectangle->h;
		}
		else
		{
			dstX = 0;
			dstY = 0;
			SDL_GetWindowSizeInPixels(
				(SDL_Window*) overrideWindowHandle,
				&dstW,
				&dstH
			);
		}

		if (renderer->scissorTestEnable)
		{
			renderer->glDisable(GL_SCISSOR_TEST);
		}

		if (	renderer->backbuffer->multiSampleCount > 0 &&
			(srcX != dstX || srcY != dstY || srcW != dstW || srcH != dstH)	)
		{
			/* We have to resolve the renderbuffer to a texture first.
			 * For whatever reason, we can't blit a multisample renderbuffer
			 * to the backbuffer. Not sure why, but oh well.
			 * -flibit
			 */
			if (renderer->backbuffer->opengl.texture == 0)
			{
				renderer->glGenTextures(1, &renderer->backbuffer->opengl.texture);
				renderer->glBindTexture(GL_TEXTURE_2D, renderer->backbuffer->opengl.texture);
				renderer->glTexImage2D(
					GL_TEXTURE_2D,
					0,
					GL_RGBA,
					renderer->backbuffer->width,
					renderer->backbuffer->height,
					0,
					GL_RGBA,
					GL_UNSIGNED_BYTE,
					NULL
				);
				renderer->glBindTexture(
					renderer->textures[0]->target,
					renderer->textures[0]->handle
				);
			}
			BindFramebuffer(renderer, renderer->resolveFramebufferDraw);
			renderer->glFramebufferTexture2D(
				GL_FRAMEBUFFER,
				GL_COLOR_ATTACHMENT0,
				GL_TEXTURE_2D,
				renderer->backbuffer->opengl.texture,
				0
			);
			BindReadFramebuffer(renderer, renderer->backbuffer->opengl.handle);
			renderer->glBlitFramebuffer(
				0, 0, renderer->backbuffer->width, renderer->backbuffer->height,
				0, 0, renderer->backbuffer->width, renderer->backbuffer->height,
				GL_COLOR_BUFFER_BIT,
				GL_LINEAR
			);
			/* Invalidate the MSAA faux-backbuffer */
			if (renderer->supports_ARB_invalidate_subdata)
			{
				renderer->glInvalidateFramebuffer(
					GL_READ_FRAMEBUFFER,
					renderer->numAttachments + 2,
					renderer->drawBuffersArray
				);
			}
			BindReadFramebuffer(renderer, renderer->resolveFramebufferDraw);
		}
		else
		{
			BindReadFramebuffer(renderer, renderer->backbuffer->opengl.handle);
		}
		BindDrawFramebuffer(renderer, renderer->realBackbufferFBO);

		renderer->glBlitFramebuffer(
			srcX, srcY, srcW, srcH,
			dstX, dstY, dstW, dstH,
			GL_COLOR_BUFFER_BIT,
			renderer->backbufferScaleMode
		);
		/* Invalidate the faux-backbuffer */
		if (renderer->supports_ARB_invalidate_subdata)
		{
			renderer->glInvalidateFramebuffer(
				GL_READ_FRAMEBUFFER,
				renderer->numAttachments + 2,
				renderer->drawBuffersArray
			);
		}

		BindFramebuffer(renderer, renderer->realBackbufferFBO);

		if (renderer->scissorTestEnable)
		{
			renderer->glEnable(GL_SCISSOR_TEST);
		}

		SDL_GL_SwapWindow((SDL_Window*) overrideWindowHandle);

		BindFramebuffer(renderer, renderer->backbuffer->opengl.handle);
	}
	else
	{
		/* Nothing left to do, just swap! */
		SDL_GL_SwapWindow((SDL_Window*) overrideWindowHandle);
	}

	/* Run any threaded commands */
	ExecuteCommands(renderer);

	/* Destroy any disposed resources */
	DisposeResources(renderer);
}

/* Drawing */

static void OPENGL_Clear(
	FNA3D_Renderer *driverData,
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	uint8_t clearTarget, clearDepth, clearStencil;
	GLenum clearMask;

	/* glClear depends on the scissor rectangle! */
	if (renderer->scissorTestEnable)
	{
		renderer->glDisable(GL_SCISSOR_TEST);
	}

	clearTarget = (options & FNA3D_CLEAROPTIONS_TARGET) != 0;
	clearDepth = (options & FNA3D_CLEAROPTIONS_DEPTHBUFFER) != 0;
	clearStencil = (options & FNA3D_CLEAROPTIONS_STENCIL) != 0;

	/* Get the clear mask, set the clear properties if needed */
	clearMask = GL_ZERO;
	if (clearTarget)
	{
		clearMask |= GL_COLOR_BUFFER_BIT;
		if (	color->x != renderer->currentClearColor.x ||
			color->y != renderer->currentClearColor.y ||
			color->z != renderer->currentClearColor.z ||
			color->w != renderer->currentClearColor.w	)
		{
			renderer->glClearColor(
				color->x,
				color->y,
				color->z,
				color->w
			);
			renderer->currentClearColor = *color;
		}
		/* glClear depends on the color write mask! */
		if (renderer->colorWriteEnable != FNA3D_COLORWRITECHANNELS_ALL)
		{
			/* FIXME: ColorWriteChannels1/2/3? -flibit */
			renderer->glColorMask(1, 1, 1, 1);
		}
	}
	if (clearDepth)
	{
		clearMask |= GL_DEPTH_BUFFER_BIT;
		if (depth != renderer->currentClearDepth)
		{
			if (renderer->supports_DoublePrecisionDepth)
			{
				renderer->glClearDepth((double) depth);
			}
			else
			{
				renderer->glClearDepthf(depth);
			}
			renderer->currentClearDepth = depth;
		}
		/* glClear depends on the depth write mask! */
		if (!renderer->zWriteEnable)
		{
			renderer->glDepthMask(1);
		}
	}
	if (clearStencil)
	{
		clearMask |= GL_STENCIL_BUFFER_BIT;
		if (stencil != renderer->currentClearStencil)
		{
			renderer->glClearStencil(stencil);
			renderer->currentClearStencil = stencil;
		}
		/* glClear depends on the stencil write mask! */
		if (renderer->stencilWriteMask != -1)
		{
			/* AKA 0xFFFFFFFF, ugh -flibit */
			renderer->glStencilMask(-1);
		}
	}

	/* CLEAR! */
	renderer->glClear(clearMask);

	/* Clean up after ourselves. */
	if (renderer->scissorTestEnable)
	{
		renderer->glEnable(GL_SCISSOR_TEST);
	}
	if (clearTarget && renderer->colorWriteEnable != FNA3D_COLORWRITECHANNELS_ALL)
	{
		/* FIXME: ColorWriteChannels1/2/3? -flibit */
		renderer->glColorMask(
			(renderer->colorWriteEnable & FNA3D_COLORWRITECHANNELS_RED) != 0,
			(renderer->colorWriteEnable & FNA3D_COLORWRITECHANNELS_GREEN) != 0,
			(renderer->colorWriteEnable & FNA3D_COLORWRITECHANNELS_BLUE) != 0,
			(renderer->colorWriteEnable & FNA3D_COLORWRITECHANNELS_ALPHA) != 0
		);
	}
	if (clearDepth && !renderer->zWriteEnable)
	{
		renderer->glDepthMask(0);
	}
	if (clearStencil && renderer->stencilWriteMask != -1) /* AKA 0xFFFFFFFF, ugh -flibit */
	{
		renderer->glStencilMask(renderer->stencilWriteMask);
	}
}

static void OPENGL_DrawIndexedPrimitives(
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
	uint8_t tps;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *buffer = (OpenGLBuffer*) indices;

	BindIndexBuffer(renderer, buffer->handle);

	tps = (	renderer->togglePointSprite &&
		primitiveType == FNA3D_PRIMITIVETYPE_POINTLIST_EXT	);
	if (tps)
	{
		renderer->glEnable(GL_POINT_SPRITE);
		renderer->glTexEnvi(GL_POINT_SPRITE, GL_COORD_REPLACE, GL_TRUE);
	}

	/* Draw! */
	if (renderer->supports_ARB_draw_elements_base_vertex)
	{
		renderer->glDrawRangeElementsBaseVertex(
			XNAToGL_Primitive[primitiveType],
			minVertexIndex,
			minVertexIndex + numVertices - 1,
			PrimitiveVerts(primitiveType, primitiveCount),
			XNAToGL_IndexType[indexElementSize],
			(void*) (size_t) (startIndex * IndexSize(indexElementSize)),
			baseVertex
		);
	}
	else
	{
		renderer->glDrawRangeElements(
			XNAToGL_Primitive[primitiveType],
			minVertexIndex,
			minVertexIndex + numVertices - 1,
			PrimitiveVerts(primitiveType, primitiveCount),
			XNAToGL_IndexType[indexElementSize],
			(void*) (size_t) (startIndex * IndexSize(indexElementSize))
		);
	}

	if (tps)
	{
		renderer->glTexEnvi(GL_POINT_SPRITE, GL_COORD_REPLACE, GL_FALSE);
		renderer->glDisable(GL_POINT_SPRITE);
	}
}

static void OPENGL_DrawInstancedPrimitives(
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
	/* Note that minVertexIndex and numVertices are NOT used! */

	uint8_t tps;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *buffer = (OpenGLBuffer*) indices;

	SDL_assert(renderer->supports_ARB_draw_instanced);

	BindIndexBuffer(renderer, buffer->handle);

	tps = (	renderer->togglePointSprite &&
		primitiveType == FNA3D_PRIMITIVETYPE_POINTLIST_EXT	);
	if (tps)
	{
		renderer->glEnable(GL_POINT_SPRITE);
		renderer->glTexEnvi(GL_POINT_SPRITE, GL_COORD_REPLACE, GL_TRUE);
	}

	/* Draw! */
	if (renderer->supports_ARB_draw_elements_base_vertex)
	{
		renderer->glDrawElementsInstancedBaseVertex(
			XNAToGL_Primitive[primitiveType],
			PrimitiveVerts(primitiveType, primitiveCount),
			XNAToGL_IndexType[indexElementSize],
			(void*) (size_t) (startIndex * IndexSize(indexElementSize)),
			instanceCount,
			baseVertex
		);
	}
	else
	{
		renderer->glDrawElementsInstanced(
			XNAToGL_Primitive[primitiveType],
			PrimitiveVerts(primitiveType, primitiveCount),
			XNAToGL_IndexType[indexElementSize],
			(void*) (size_t) (startIndex * IndexSize(indexElementSize)),
			instanceCount
		);
	}

	if (tps)
	{
		renderer->glTexEnvi(GL_POINT_SPRITE, GL_COORD_REPLACE, GL_FALSE);
		renderer->glDisable(GL_POINT_SPRITE);
	}
}

static void OPENGL_DrawPrimitives(
	FNA3D_Renderer *driverData,
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
) {
	uint8_t tps;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;

	tps = (	renderer->togglePointSprite &&
		primitiveType == FNA3D_PRIMITIVETYPE_POINTLIST_EXT	);
	if (tps)
	{
		renderer->glEnable(GL_POINT_SPRITE);
		renderer->glTexEnvi(GL_POINT_SPRITE, GL_COORD_REPLACE, GL_TRUE);
	}

	/* Draw! */
	renderer->glDrawArrays(
		XNAToGL_Primitive[primitiveType],
		vertexStart,
		PrimitiveVerts(primitiveType, primitiveCount)
	);

	if (tps)
	{
		renderer->glTexEnvi(GL_POINT_SPRITE, GL_COORD_REPLACE, GL_FALSE);
		renderer->glDisable(GL_POINT_SPRITE);
	}
}

/* Mutable Render States */

static void OPENGL_SetViewport(FNA3D_Renderer *driverData, FNA3D_Viewport *viewport)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	int32_t bbw, bbh;
	FNA3D_Viewport vp = *viewport;

	/* Flip viewport when target is not bound */
	if (!renderer->renderTargetBound)
	{
		OPENGL_GetBackbufferSize(driverData, &bbw, &bbh);
		vp.y = bbh - viewport->y - viewport->h;
	}

	if (	vp.x != renderer->viewport.x ||
		vp.y != renderer->viewport.y ||
		vp.w != renderer->viewport.w ||
		vp.h != renderer->viewport.h	)
	{
		renderer->viewport = vp;
		renderer->glViewport(
			vp.x,
			vp.y,
			vp.w,
			vp.h
		);
	}

	if (	viewport->minDepth != renderer->depthRangeMin ||
		viewport->maxDepth != renderer->depthRangeMax	)
	{
		renderer->depthRangeMin = viewport->minDepth;
		renderer->depthRangeMax = viewport->maxDepth;

		if (renderer->supports_DoublePrecisionDepth)
		{
			renderer->glDepthRange(
				(double) viewport->minDepth,
				(double) viewport->maxDepth
			);
		}
		else
		{
			renderer->glDepthRangef(
				viewport->minDepth,
				viewport->maxDepth
			);
		}
	}
}

static void OPENGL_SetScissorRect(FNA3D_Renderer *driverData, FNA3D_Rect *scissor)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	int32_t bbw, bbh;
	FNA3D_Rect sr = *scissor;

	/* Flip rectangle when target is not bound */
	if (!renderer->renderTargetBound)
	{
		OPENGL_GetBackbufferSize(driverData, &bbw, &bbh);
		sr.y = bbh - scissor->y - scissor->h;
	}

	if (	sr.x != renderer->scissorRect.x ||
		sr.y != renderer->scissorRect.y ||
		sr.w != renderer->scissorRect.w ||
		sr.h != renderer->scissorRect.h	)
	{
		renderer->scissorRect = sr;
		renderer->glScissor(
			sr.x,
			sr.y,
			sr.w,
			sr.h
		);
	}
}

static void OPENGL_GetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	SDL_memcpy(blendFactor, &renderer->blendColor, sizeof(FNA3D_Color));
}

static void OPENGL_SetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	if (	renderer->blendColor.r != blendFactor->r ||
		renderer->blendColor.g != blendFactor->g ||
		renderer->blendColor.b != blendFactor->b ||
		renderer->blendColor.a != blendFactor->a	)
	{
		renderer->blendColor.r = blendFactor->r;
		renderer->blendColor.g = blendFactor->g;
		renderer->blendColor.b = blendFactor->b;
		renderer->blendColor.a = blendFactor->a;
		renderer->glBlendColor(
			renderer->blendColor.r / 255.0f,
			renderer->blendColor.g / 255.0f,
			renderer->blendColor.b / 255.0f,
			renderer->blendColor.a / 255.0f
		);
	}
}

static int32_t OPENGL_GetMultiSampleMask(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	SDL_assert(renderer->supports_ARB_texture_multisample);
	return renderer->multiSampleMask;
}

static void OPENGL_SetMultiSampleMask(FNA3D_Renderer *driverData, int32_t mask)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	SDL_assert(renderer->supports_ARB_texture_multisample);
	if (mask != renderer->multiSampleMask)
	{
		if (mask == -1)
		{
			renderer->glDisable(GL_SAMPLE_MASK);
		}
		else
		{
			if (renderer->multiSampleMask == -1)
			{
				renderer->glEnable(GL_SAMPLE_MASK);
			}
			/* FIXME: Index...? -flibit */
			renderer->glSampleMaski(0, (GLuint) mask);
		}
		renderer->multiSampleMask = mask;
	}
}

static int32_t OPENGL_GetReferenceStencil(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return renderer->stencilRef;
}

static void OPENGL_SetReferenceStencil(FNA3D_Renderer *driverData, int32_t ref)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	if (ref != renderer->stencilRef)
	{
		renderer->stencilRef = ref;
		if (renderer->separateStencilEnable)
		{
			renderer->glStencilFuncSeparate(
				GL_FRONT,
				XNAToGL_CompareFunc[renderer->stencilFunc],
				renderer->stencilRef,
				renderer->stencilMask
			);
			renderer->glStencilFuncSeparate(
				GL_BACK,
				XNAToGL_CompareFunc[renderer->stencilFunc],
				renderer->stencilRef,
				renderer->stencilMask
			);
		}
		else
		{
			renderer->glStencilFunc(
				XNAToGL_CompareFunc[renderer->stencilFunc],
				renderer->stencilRef,
				renderer->stencilMask
			);
		}
	}
}

/* Immutable Render States */

static void OPENGL_SetBlendState(
	FNA3D_Renderer *driverData,
	FNA3D_BlendState *blendState
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;

	uint8_t newEnable = !(
		blendState->colorSourceBlend == FNA3D_BLEND_ONE &&
		blendState->colorDestinationBlend == FNA3D_BLEND_ZERO &&
		blendState->alphaSourceBlend == FNA3D_BLEND_ONE &&
		blendState->alphaDestinationBlend == FNA3D_BLEND_ZERO
	);

	if (newEnable != renderer->alphaBlendEnable)
	{
		renderer->alphaBlendEnable = newEnable;
		ToggleGLState(renderer, GL_BLEND, renderer->alphaBlendEnable);
	}

	if (renderer->alphaBlendEnable)
	{
		if (	blendState->blendFactor.r != renderer->blendColor.r ||
			blendState->blendFactor.g != renderer->blendColor.g ||
			blendState->blendFactor.b != renderer->blendColor.b ||
			blendState->blendFactor.a != renderer->blendColor.a	)
		{
			renderer->blendColor = blendState->blendFactor;
			renderer->glBlendColor(
				renderer->blendColor.r / 255.0f,
				renderer->blendColor.g / 255.0f,
				renderer->blendColor.b / 255.0f,
				renderer->blendColor.a / 255.0f
			);
		}

		if (	blendState->colorSourceBlend != renderer->srcBlend ||
			blendState->colorDestinationBlend != renderer->dstBlend ||
			blendState->alphaSourceBlend != renderer->srcBlendAlpha ||
			blendState->alphaDestinationBlend != renderer->dstBlendAlpha	)
		{
			renderer->srcBlend = blendState->colorSourceBlend;
			renderer->dstBlend = blendState->colorDestinationBlend;
			renderer->srcBlendAlpha = blendState->alphaSourceBlend;
			renderer->dstBlendAlpha = blendState->alphaDestinationBlend;
			renderer->glBlendFuncSeparate(
				XNAToGL_BlendMode[renderer->srcBlend],
				XNAToGL_BlendMode[renderer->dstBlend],
				XNAToGL_BlendMode[renderer->srcBlendAlpha],
				XNAToGL_BlendMode[renderer->dstBlendAlpha]
			);
		}

		if (	blendState->colorBlendFunction != renderer->blendOp ||
			blendState->alphaBlendFunction != renderer->blendOpAlpha	)
		{
			renderer->blendOp = blendState->colorBlendFunction;
			renderer->blendOpAlpha = blendState->alphaBlendFunction;
			renderer->glBlendEquationSeparate(
				XNAToGL_BlendEquation[renderer->blendOp],
				XNAToGL_BlendEquation[renderer->blendOpAlpha]
			);
		}
	}

	if (blendState->colorWriteEnable != renderer->colorWriteEnable)
	{
		renderer->colorWriteEnable = blendState->colorWriteEnable;
		renderer->glColorMask(
			(renderer->colorWriteEnable & FNA3D_COLORWRITECHANNELS_RED) != 0,
			(renderer->colorWriteEnable & FNA3D_COLORWRITECHANNELS_GREEN) != 0,
			(renderer->colorWriteEnable & FNA3D_COLORWRITECHANNELS_BLUE) != 0,
			(renderer->colorWriteEnable & FNA3D_COLORWRITECHANNELS_ALPHA) != 0
		);
	}

	/* FIXME: So how exactly do we factor in
	 * COLORWRITEENABLE for buffer 0? Do we just assume that
	 * the default is just buffer 0, and all other calls
	 * update the other write masks afterward? Or do we
	 * assume that COLORWRITEENABLE only touches 0, and the
	 * other 3 buffers are left alone unless we don't have
	 * EXT_draw_buffers2?
	 * -flibit
	 */
	if (blendState->colorWriteEnable1 != renderer->colorWriteEnable1)
	{
		SDL_assert(renderer->supports_EXT_draw_buffers2);
		renderer->colorWriteEnable1 = blendState->colorWriteEnable1;
		renderer->glColorMaski(
			1,
			(renderer->colorWriteEnable1 & FNA3D_COLORWRITECHANNELS_RED) != 0,
			(renderer->colorWriteEnable1 & FNA3D_COLORWRITECHANNELS_GREEN) != 0,
			(renderer->colorWriteEnable1 & FNA3D_COLORWRITECHANNELS_BLUE) != 0,
			(renderer->colorWriteEnable1 & FNA3D_COLORWRITECHANNELS_ALPHA) != 0
		);
	}
	if (blendState->colorWriteEnable2 != renderer->colorWriteEnable2)
	{
		SDL_assert(renderer->supports_EXT_draw_buffers2);
		renderer->colorWriteEnable2 = blendState->colorWriteEnable2;
		renderer->glColorMaski(
			2,
			(renderer->colorWriteEnable2 & FNA3D_COLORWRITECHANNELS_RED) != 0,
			(renderer->colorWriteEnable2 & FNA3D_COLORWRITECHANNELS_GREEN) != 0,
			(renderer->colorWriteEnable2 & FNA3D_COLORWRITECHANNELS_BLUE) != 0,
			(renderer->colorWriteEnable2 & FNA3D_COLORWRITECHANNELS_ALPHA) != 0
		);
	}
	if (blendState->colorWriteEnable3 != renderer->colorWriteEnable3)
	{
		SDL_assert(renderer->supports_EXT_draw_buffers2);
		renderer->colorWriteEnable3 = blendState->colorWriteEnable3;
		renderer->glColorMaski(
			3,
			(renderer->colorWriteEnable3 & FNA3D_COLORWRITECHANNELS_RED) != 0,
			(renderer->colorWriteEnable3 & FNA3D_COLORWRITECHANNELS_GREEN) != 0,
			(renderer->colorWriteEnable3 & FNA3D_COLORWRITECHANNELS_BLUE) != 0,
			(renderer->colorWriteEnable3 & FNA3D_COLORWRITECHANNELS_ALPHA) != 0
		);
	}

	if (blendState->multiSampleMask != renderer->multiSampleMask)
	{
		SDL_assert(renderer->supports_ARB_texture_multisample);
		if (blendState->multiSampleMask == -1)
		{
			renderer->glDisable(GL_SAMPLE_MASK);
		}
		else
		{
			if (renderer->multiSampleMask == -1)
			{
				renderer->glEnable(GL_SAMPLE_MASK);
			}
			/* FIXME: index...? -flibit */
			renderer->glSampleMaski(0, (uint32_t) blendState->multiSampleMask);
		}
		renderer->multiSampleMask = blendState->multiSampleMask;
	}
}

static void OPENGL_SetDepthStencilState(
	FNA3D_Renderer *driverData,
	FNA3D_DepthStencilState *depthStencilState
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;

	if (depthStencilState->depthBufferEnable != renderer->zEnable)
	{
		renderer->zEnable = depthStencilState->depthBufferEnable;
		ToggleGLState(renderer, GL_DEPTH_TEST, renderer->zEnable);
	}

	if (renderer->zEnable)
	{
		if (depthStencilState->depthBufferWriteEnable != renderer->zWriteEnable)
		{
			renderer->zWriteEnable = depthStencilState->depthBufferWriteEnable;
			renderer->glDepthMask(renderer->zWriteEnable);
		}

		if (depthStencilState->depthBufferFunction != renderer->depthFunc)
		{
			renderer->depthFunc = depthStencilState->depthBufferFunction;
			renderer->glDepthFunc(XNAToGL_CompareFunc[renderer->depthFunc]);
		}
	}

	if (depthStencilState->stencilEnable != renderer->stencilEnable)
	{
		renderer->stencilEnable = depthStencilState->stencilEnable;
		ToggleGLState(renderer, GL_STENCIL_TEST, renderer->stencilEnable);
	}

	if (renderer->stencilEnable)
	{
		if (depthStencilState->stencilWriteMask != renderer->stencilWriteMask)
		{
			renderer->stencilWriteMask = depthStencilState->stencilWriteMask;
			renderer->glStencilMask(renderer->stencilWriteMask);
		}

		/* TODO: Can we split up StencilFunc/StencilOp nicely? -flibit */
		if (	depthStencilState->twoSidedStencilMode != renderer->separateStencilEnable ||
			depthStencilState->referenceStencil != renderer->stencilRef ||
			depthStencilState->stencilMask != renderer->stencilMask ||
			depthStencilState->stencilFunction != renderer->stencilFunc ||
			depthStencilState->ccwStencilFunction != renderer->ccwStencilFunc ||
			depthStencilState->stencilFail != renderer->stencilFail ||
			depthStencilState->stencilDepthBufferFail != renderer->stencilZFail ||
			depthStencilState->stencilPass != renderer->stencilPass ||
			depthStencilState->ccwStencilFail != renderer->ccwStencilFail ||
			depthStencilState->ccwStencilDepthBufferFail != renderer->ccwStencilZFail ||
			depthStencilState->ccwStencilPass != renderer->ccwStencilPass			)
		{
			renderer->separateStencilEnable = depthStencilState->twoSidedStencilMode;
			renderer->stencilRef = depthStencilState->referenceStencil;
			renderer->stencilMask = depthStencilState->stencilMask;
			renderer->stencilFunc = depthStencilState->stencilFunction;
			renderer->stencilFail = depthStencilState->stencilFail;
			renderer->stencilZFail = depthStencilState->stencilDepthBufferFail;
			renderer->stencilPass = depthStencilState->stencilPass;
			if (renderer->separateStencilEnable)
			{
				renderer->ccwStencilFunc = depthStencilState->ccwStencilFunction;
				renderer->ccwStencilFail = depthStencilState->ccwStencilFail;
				renderer->ccwStencilZFail = depthStencilState->ccwStencilDepthBufferFail;
				renderer->ccwStencilPass = depthStencilState->ccwStencilPass;
				renderer->glStencilFuncSeparate(
					GL_FRONT,
					XNAToGL_CompareFunc[renderer->stencilFunc],
					renderer->stencilRef,
					renderer->stencilMask
				);
				renderer->glStencilFuncSeparate(
					GL_BACK,
					XNAToGL_CompareFunc[renderer->ccwStencilFunc],
					renderer->stencilRef,
					renderer->stencilMask
				);
				renderer->glStencilOpSeparate(
					GL_FRONT,
					XNAToGL_GLStencilOp[renderer->stencilFail],
					XNAToGL_GLStencilOp[renderer->stencilZFail],
					XNAToGL_GLStencilOp[renderer->stencilPass]
				);
				renderer->glStencilOpSeparate(
					GL_BACK,
					XNAToGL_GLStencilOp[renderer->ccwStencilFail],
					XNAToGL_GLStencilOp[renderer->ccwStencilZFail],
					XNAToGL_GLStencilOp[renderer->ccwStencilPass]
				);
			}
			else
			{
				renderer->glStencilFunc(
					XNAToGL_CompareFunc[renderer->stencilFunc],
					renderer->stencilRef,
					renderer->stencilMask
				);
				renderer->glStencilOp(
					XNAToGL_GLStencilOp[renderer->stencilFail],
					XNAToGL_GLStencilOp[renderer->stencilZFail],
					XNAToGL_GLStencilOp[renderer->stencilPass]
				);
			}
		}
	}
}

static void OPENGL_ApplyRasterizerState(
	FNA3D_Renderer *driverData,
	FNA3D_RasterizerState *rasterizerState
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	FNA3D_CullMode actualMode;
	float realDepthBias;

	if (rasterizerState->scissorTestEnable != renderer->scissorTestEnable)
	{
		renderer->scissorTestEnable = rasterizerState->scissorTestEnable;
		ToggleGLState(renderer, GL_SCISSOR_TEST, renderer->scissorTestEnable);
	}

	if (renderer->renderTargetBound)
	{
		actualMode = rasterizerState->cullMode;
	}
	else
	{
		/* When not rendering offscreen the faces change order. */
		if (rasterizerState->cullMode == FNA3D_CULLMODE_NONE)
		{
			actualMode = rasterizerState->cullMode;
		}
		else
		{
			actualMode = (
				rasterizerState->cullMode == FNA3D_CULLMODE_CULLCLOCKWISEFACE ?
					FNA3D_CULLMODE_CULLCOUNTERCLOCKWISEFACE :
					FNA3D_CULLMODE_CULLCLOCKWISEFACE
			);
		}
	}
	if (actualMode != renderer->cullFrontFace)
	{
		if ((actualMode == FNA3D_CULLMODE_NONE) != (renderer->cullFrontFace == FNA3D_CULLMODE_NONE))
		{
			ToggleGLState(renderer, GL_CULL_FACE, actualMode != FNA3D_CULLMODE_NONE);
		}
		renderer->cullFrontFace = actualMode;
		if (renderer->cullFrontFace != FNA3D_CULLMODE_NONE)
		{
			renderer->glFrontFace(XNAToGL_FrontFace[renderer->cullFrontFace]);
		}
	}

	if (rasterizerState->fillMode != renderer->fillMode)
	{
		renderer->fillMode = rasterizerState->fillMode;
		renderer->glPolygonMode(
			GL_FRONT_AND_BACK,
			XNAToGL_GLFillMode[renderer->fillMode]
		);
	}

	realDepthBias = rasterizerState->depthBias * XNAToGL_DepthBiasScale[
		renderer->renderTargetBound ?
			renderer->currentDepthStencilFormat :
			renderer->backbuffer->depthFormat
	];
	if (	realDepthBias != renderer->depthBias ||
		rasterizerState->slopeScaleDepthBias != renderer->slopeScaleDepthBias	)
	{
		if (	realDepthBias == 0.0f &&
			rasterizerState->slopeScaleDepthBias == 0.0f)
		{
			/* We're changing to disabled bias, disable! */
			renderer->glDisable(GL_POLYGON_OFFSET_FILL);
		}
		else
		{
			if (renderer->depthBias == 0.0f && renderer->slopeScaleDepthBias == 0.0f)
			{
				/* We're changing away from disabled bias, enable! */
				renderer->glEnable(GL_POLYGON_OFFSET_FILL);
			}
			renderer->glPolygonOffset(
				rasterizerState->slopeScaleDepthBias,
				realDepthBias
			);
		}
		renderer->depthBias = realDepthBias;
		renderer->slopeScaleDepthBias = rasterizerState->slopeScaleDepthBias;
	}

	/* If you're reading this, you have a user with broken MSAA!
	 * Here's the deal: On all modern drivers this should work,
	 * but there was a period of time where, for some reason,
	 * IHVs all took a nap and decided that they didn't have to
	 * respect GL_MULTISAMPLE toggles. A couple sources:
	 *
	 * https://developer.apple.com/library/content/documentation/GraphicsImaging/Conceptual/OpenGL-MacProgGuide/opengl_fsaa/opengl_fsaa.html
	 *
	 * https://www.opengl.org/discussion_boards/showthread.php/172025-glDisable(GL_MULTISAMPLE)-has-no-effect
	 *
	 * So yeah. Have em update their driver. If they're on Intel,
	 * tell them to install Linux. Yes, really.
	 * -flibit
	 */
	if (rasterizerState->multiSampleAntiAlias != renderer->multiSampleEnable)
	{
		renderer->multiSampleEnable = rasterizerState->multiSampleAntiAlias;
		ToggleGLState(renderer, GL_MULTISAMPLE, renderer->multiSampleEnable);
	}
}

static void OPENGL_VerifySampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *tex = (OpenGLTexture*) texture;

	if (texture == NULL)
	{
		if (renderer->textures[index] != &NullTexture)
		{
			if (index != 0)
			{
				renderer->glActiveTexture(GL_TEXTURE0 + index);
			}
			renderer->glBindTexture(renderer->textures[index]->target, 0);
			if (index != 0)
			{
				/* Keep this state sane. -flibit */
				renderer->glActiveTexture(GL_TEXTURE0);
			}
			renderer->textures[index] = &NullTexture;
		}
		return;
	}

	if (	tex == renderer->textures[index] &&
		sampler->addressU == tex->wrapS &&
		sampler->addressV == tex->wrapT &&
		sampler->addressW == tex->wrapR &&
		sampler->filter == tex->filter &&
		sampler->maxAnisotropy == tex->anisotropy &&
		sampler->maxMipLevel == tex->maxMipmapLevel &&
		sampler->mipMapLevelOfDetailBias == tex->lodBias	)
	{
		/* Nothing's changing, forget it. */
		return;
	}

	/* Set the active texture slot */
	if (index != 0)
	{
		renderer->glActiveTexture(GL_TEXTURE0 + index);
	}

	/* Bind the correct texture */
	if (tex != renderer->textures[index])
	{
		if (tex->target != renderer->textures[index]->target)
		{
			/* If we're changing targets, unbind the old texture first! */
			renderer->glBindTexture(renderer->textures[index]->target, 0);
		}
		renderer->glBindTexture(tex->target, tex->handle);
		renderer->textures[index] = tex;
	}

	/* Apply the sampler states to the GL texture */
	if (sampler->addressU != tex->wrapS)
	{
		tex->wrapS = sampler->addressU;
		renderer->glTexParameteri(
			tex->target,
			GL_TEXTURE_WRAP_S,
			XNAToGL_Wrap[tex->wrapS]
		);
	}
	if (sampler->addressV != tex->wrapT)
	{
		tex->wrapT = sampler->addressV;
		renderer->glTexParameteri(
			tex->target,
			GL_TEXTURE_WRAP_T,
			XNAToGL_Wrap[tex->wrapT]
		);
	}
	if (sampler->addressW != tex->wrapR)
	{
		tex->wrapR = sampler->addressW;
		renderer->glTexParameteri(
			tex->target,
			GL_TEXTURE_WRAP_R,
			XNAToGL_Wrap[tex->wrapR]
		);
	}
	if (	sampler->filter != tex->filter ||
		sampler->maxAnisotropy != tex->anisotropy	)
	{
		tex->filter = sampler->filter;
		tex->anisotropy = (float) sampler->maxAnisotropy;
		renderer->glTexParameteri(
			tex->target,
			GL_TEXTURE_MAG_FILTER,
			XNAToGL_MagFilter[tex->filter]
		);
		renderer->glTexParameteri(
			tex->target,
			GL_TEXTURE_MIN_FILTER,
			tex->hasMipmaps ?
				XNAToGL_MinMipFilter[tex->filter] :
				XNAToGL_MinFilter[tex->filter]
		);
		if (renderer->supports_anisotropic_filtering)
		{
			renderer->glTexParameterf(
				tex->target,
				GL_TEXTURE_MAX_ANISOTROPY_EXT,
				(tex->filter == FNA3D_TEXTUREFILTER_ANISOTROPIC) ?
					SDL_max(tex->anisotropy, 1.0f) :
					1.0f
			);
		}
	}
	if (sampler->maxMipLevel != tex->maxMipmapLevel)
	{
		tex->maxMipmapLevel = sampler->maxMipLevel;
		renderer->glTexParameteri(
			tex->target,
			GL_TEXTURE_BASE_LEVEL,
			tex->maxMipmapLevel
		);
	}
	if (sampler->mipMapLevelOfDetailBias != tex->lodBias && !renderer->useES3)
	{
		tex->lodBias = sampler->mipMapLevelOfDetailBias;
		renderer->glTexParameterf(
			tex->target,
			GL_TEXTURE_LOD_BIAS,
			tex->lodBias
		);
	}

	if (index != 0)
	{
		/* Keep this state sane. -flibit */
		renderer->glActiveTexture(GL_TEXTURE0);
	}
}

static void OPENGL_VerifyVertexSampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OPENGL_VerifySampler(
		driverData,
		index +	renderer->vertexSamplerStart,
		texture,
		sampler
	);
}

static inline void OPENGL_INTERNAL_FlushGLVertexAttributes(OpenGLRenderer *renderer)
{
	int32_t i, divisor;
	for (i = 0; i < renderer->numVertexAttributes; i += 1)
	{
		if (renderer->attributeEnabled[i])
		{
			renderer->attributeEnabled[i] = 0;
			if (!renderer->previousAttributeEnabled[i])
			{
				renderer->glEnableVertexAttribArray(i);
				renderer->previousAttributeEnabled[i] = 1;
			}
		}
		else if (renderer->previousAttributeEnabled[i])
		{
			renderer->glDisableVertexAttribArray(i);
			renderer->previousAttributeEnabled[i] = 0;
		}

		divisor = renderer->attributeDivisor[i];
		if (divisor != renderer->previousAttributeDivisor[i])
		{
			renderer->glVertexAttribDivisor(i, divisor);
			renderer->previousAttributeDivisor[i] = divisor;
		}
	}
}

static void OPENGL_ApplyVertexBufferBindings(
	FNA3D_Renderer *driverData,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
) {
	uint8_t *basePtr, *ptr;
	uint8_t normalized;
	int32_t i, j, k;
	int32_t usage, index, attribLoc;
	uint8_t attrUse[MOJOSHADER_USAGE_TOTAL][16];
	FNA3D_VertexElement *element;
	FNA3D_VertexDeclaration *vertexDeclaration;
	OpenGLVertexAttribute *attr;
	OpenGLBuffer *buffer;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;

	if (renderer->supports_ARB_draw_elements_base_vertex)
	{
		baseVertex = 0;
	}

	if (	bindingsUpdated ||
		baseVertex != renderer->ldBaseVertex ||
		renderer->effectApplied	)
	{
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
			buffer = (OpenGLBuffer*) bindings[i].vertexBuffer;
			BindVertexBuffer(renderer, buffer->handle);
			vertexDeclaration = &bindings[i].vertexDeclaration;
			basePtr = (uint8_t*) (size_t) (
				vertexDeclaration->vertexStride *
				(bindings[i].vertexOffset + baseVertex)
			);
			for (j = 0; j < vertexDeclaration->elementCount; j += 1)
			{
				element = &vertexDeclaration->elements[j];
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
				attribLoc = MOJOSHADER_glGetVertexAttribLocation(
					VertexAttribUsage(usage),
					index
				);
				if (attribLoc == -1)
				{
					/* Stream not in use! */
					continue;
				}
				renderer->attributeEnabled[attribLoc] = 1;
				attr = &renderer->attributes[attribLoc];
				ptr = basePtr + element->offset;
				normalized = XNAToGL_VertexAttribNormalized(element);
				if (	attr->currentBuffer != buffer->handle ||
					attr->currentPointer != ptr ||
					attr->currentFormat != element->vertexElementFormat ||
					attr->currentNormalized != normalized ||
					attr->currentStride != vertexDeclaration->vertexStride	)
				{
					renderer->glVertexAttribPointer(
						attribLoc,
						XNAToGL_VertexAttribSize[element->vertexElementFormat],
						XNAToGL_VertexAttribType[element->vertexElementFormat],
						normalized,
						vertexDeclaration->vertexStride,
						ptr
					);
					attr->currentBuffer = buffer->handle;
					attr->currentPointer = ptr;
					attr->currentFormat = element->vertexElementFormat;
					attr->currentNormalized = normalized;
					attr->currentStride = vertexDeclaration->vertexStride;
				}
				if (renderer->supports_ARB_instanced_arrays)
				{
					renderer->attributeDivisor[attribLoc] = bindings[i].instanceFrequency;
				}
			}
		}
		OPENGL_INTERNAL_FlushGLVertexAttributes(renderer);

		renderer->ldBaseVertex = baseVertex;
		renderer->effectApplied = 0;
	}

	MOJOSHADER_glProgramReady();
	MOJOSHADER_glProgramViewportInfo(
		renderer->viewport.w, renderer->viewport.h,
		renderer->backbuffer->width, renderer->backbuffer->height,
		renderer->renderTargetBound
	);
}

/* Render Targets */

static void OPENGL_SetRenderTargets(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLRenderbuffer *rb = (OpenGLRenderbuffer*) depthStencilBuffer;
	uint8_t isSrgb = 0;
	FNA3D_RenderTargetBinding *rt;
	int32_t i;
	GLuint handle;

	/* Bind the right framebuffer, if needed */
	if (numRenderTargets <= 0)
	{
		BindFramebuffer(
			renderer,
			renderer->backbuffer->type == BACKBUFFER_TYPE_OPENGL ?
				renderer->backbuffer->opengl.handle :
				renderer->realBackbufferFBO
		);
		renderer->renderTargetBound = 0;
		ApplySRGBFlag(renderer, renderer->backbuffer->isSrgb);
		return;
	}
	else
	{
		BindFramebuffer(renderer, renderer->targetFramebuffer);
		renderer->renderTargetBound = 1;
	}

	for (i = 0; i < numRenderTargets; i += 1)
	{
		rt = &renderTargets[i];
		if (rt->colorBuffer != NULL)
		{
			renderer->attachments[i] = ((OpenGLRenderbuffer*) rt->colorBuffer)->handle;
			renderer->attachmentTypes[i] = GL_RENDERBUFFER;
			isSrgb |= (((OpenGLRenderbuffer*)rt->colorBuffer)->format == FNA3D_SURFACEFORMAT_COLORSRGB_EXT);
		}
		else
		{
			renderer->attachments[i] = ((OpenGLTexture*) rt->texture)->handle;
			if (rt->type == FNA3D_RENDERTARGET_TYPE_2D)
			{
				renderer->attachmentTypes[i] = GL_TEXTURE_2D;
			}
			else
			{
				renderer->attachmentTypes[i] = GL_TEXTURE_CUBE_MAP_POSITIVE_X + rt->cube.face;
			}
			isSrgb |= (((OpenGLTexture*) rt->texture)->format == FNA3D_SURFACEFORMAT_COLORSRGB_EXT);
		}
	}

	ApplySRGBFlag(renderer, isSrgb);

	/* Update the color attachments, DrawBuffers state */
	for (i = 0; i < numRenderTargets; i += 1)
	{
		const uint8_t handleChange = renderer->attachments[i] != renderer->currentAttachments[i];
		const uint8_t typeChange = renderer->attachmentTypes[i] != renderer->currentAttachmentTypes[i];
		const uint8_t renderbuffersInvolved = (
			renderer->attachmentTypes[i] == GL_RENDERBUFFER ||
			renderer->currentAttachmentTypes[i] == GL_RENDERBUFFER
		);

		/* Only detach the previous attachment here if all of the following are met:
		 * - There must be an attachment to unset in the first place
		 * - The type of the attachment index must be changing in some way
		 * - Either the handles must be different, or a renderbuffer is involved
		 *   and therefore a GL handle collision has occurred (WTF)
		 */
		if (	typeChange &&
			renderer->currentAttachments[i] != 0 &&
			(handleChange || renderbuffersInvolved)	)
		{
			if (renderer->currentAttachmentTypes[i] == GL_RENDERBUFFER)
			{
				renderer->glFramebufferRenderbuffer(
					GL_FRAMEBUFFER,
					GL_COLOR_ATTACHMENT0 + i,
					GL_RENDERBUFFER,
					0
				);
			}
			else
			{
				renderer->glFramebufferTexture2D(
					GL_FRAMEBUFFER,
					GL_COLOR_ATTACHMENT0 + i,
					renderer->currentAttachmentTypes[i],
					0,
					0
				);
			}
		}

		if (handleChange || typeChange)
		{
			if (renderer->attachmentTypes[i] == GL_RENDERBUFFER)
			{
				renderer->glFramebufferRenderbuffer(
					GL_FRAMEBUFFER,
					GL_COLOR_ATTACHMENT0 + i,
					GL_RENDERBUFFER,
					renderer->attachments[i]
				);
			}
			else
			{
				renderer->glFramebufferTexture2D(
					GL_FRAMEBUFFER,
					GL_COLOR_ATTACHMENT0 + i,
					renderer->attachmentTypes[i],
					renderer->attachments[i],
					0
				);
			}
			renderer->currentAttachments[i] = renderer->attachments[i];
			renderer->currentAttachmentTypes[i] = renderer->attachmentTypes[i];
		}
	}
	while (i < renderer->numAttachments)
	{
		if (renderer->currentAttachments[i] != 0)
		{
			if (renderer->currentAttachmentTypes[i] == GL_RENDERBUFFER)
			{
				renderer->glFramebufferRenderbuffer(
					GL_FRAMEBUFFER,
					GL_COLOR_ATTACHMENT0 + i,
					GL_RENDERBUFFER,
					0
				);
			}
			else
			{
				renderer->glFramebufferTexture2D(
					GL_FRAMEBUFFER,
					GL_COLOR_ATTACHMENT0 + i,
					renderer->currentAttachmentTypes[i],
					0,
					0
				);
			}
			renderer->currentAttachments[i] = 0;
			renderer->currentAttachmentTypes[i] = GL_TEXTURE_2D;
		}
		i += 1;
	}
	if (numRenderTargets != renderer->currentDrawBuffers)
	{
		renderer->glDrawBuffers(numRenderTargets, renderer->drawBuffersArray);
		renderer->currentDrawBuffers = numRenderTargets;
	}

	/* Update the depth/stencil attachment */
	/* FIXME: Notice that we do separate attach calls for the stencil.
	 * We _should_ be able to do a single attach for depthstencil, but
	 * some drivers (like Mesa) cannot into GL_DEPTH_STENCIL_ATTACHMENT.
	 * Use XNAToGL.DepthStencilAttachment when this isn't a problem.
	 * -flibit
	 */
	if (depthStencilBuffer == NULL)
	{
		handle = 0;
	}
	else
	{
		handle = rb->handle;
	}
	if (handle != renderer->currentRenderbuffer)
	{
		if (renderer->currentDepthStencilFormat == FNA3D_DEPTHFORMAT_D24S8)
		{
			renderer->glFramebufferRenderbuffer(
				GL_FRAMEBUFFER,
				GL_STENCIL_ATTACHMENT,
				GL_RENDERBUFFER,
				0
			);
		}
		renderer->currentDepthStencilFormat = depthFormat;
		renderer->glFramebufferRenderbuffer(
			GL_FRAMEBUFFER,
			GL_DEPTH_ATTACHMENT,
			GL_RENDERBUFFER,
			handle
		);
		if (renderer->currentDepthStencilFormat == FNA3D_DEPTHFORMAT_D24S8)
		{
			renderer->glFramebufferRenderbuffer(
				GL_FRAMEBUFFER,
				GL_STENCIL_ATTACHMENT,
				GL_RENDERBUFFER,
				handle
			);
		}
		renderer->currentRenderbuffer = handle;
	}
}

static void OPENGL_ResolveTarget(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *target
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	GLuint prevBuffer;
	OpenGLTexture *prevTex;
	OpenGLTexture *rtTex = (OpenGLTexture*) target->texture;
	int32_t width, height;
	GLenum textureTarget;

	if (target->type == FNA3D_RENDERTARGET_TYPE_2D)
	{
		textureTarget = GL_TEXTURE_2D;
		width = target->twod.width;
		height = target->twod.height;
	}
	else
	{
		textureTarget = GL_TEXTURE_CUBE_MAP_POSITIVE_X + target->cube.face;
		width = target->cube.size;
		height = target->cube.size;
	}

	if (target->multiSampleCount > 0)
	{
		prevBuffer = renderer->currentDrawFramebuffer;

		/* Set up the texture framebuffer */
		BindFramebuffer(renderer, renderer->resolveFramebufferDraw);
		renderer->glFramebufferTexture2D(
			GL_FRAMEBUFFER,
			GL_COLOR_ATTACHMENT0,
			textureTarget,
			rtTex->handle,
			0
		);

		/* Set up the renderbuffer framebuffer */
		BindFramebuffer(renderer, renderer->resolveFramebufferRead);
		renderer->glFramebufferRenderbuffer(
			GL_FRAMEBUFFER,
			GL_COLOR_ATTACHMENT0,
			GL_RENDERBUFFER,
			((OpenGLRenderbuffer*) target->colorBuffer)->handle
		);

		/* Blit! */
		if (renderer->scissorTestEnable)
		{
			renderer->glDisable(GL_SCISSOR_TEST);
		}
		BindDrawFramebuffer(renderer, renderer->resolveFramebufferDraw);
		renderer->glBlitFramebuffer(
			0, 0, width, height,
			0, 0, width, height,
			GL_COLOR_BUFFER_BIT,
			GL_LINEAR
		);
		/* Invalidate the MSAA buffer */
		if (renderer->supports_ARB_invalidate_subdata)
		{
			renderer->glInvalidateFramebuffer(
				GL_READ_FRAMEBUFFER,
				renderer->numAttachments + 2,
				renderer->drawBuffersArray
			);
		}
		if (renderer->scissorTestEnable)
		{
			renderer->glEnable(GL_SCISSOR_TEST);
		}

		BindFramebuffer(renderer, prevBuffer);
	}

	/* If the target has mipmaps, regenerate them now */
	if (target->levelCount > 1)
	{
		prevTex = renderer->textures[0];
		BindTexture(renderer, rtTex);
		renderer->glGenerateMipmap(textureTarget);
		BindTexture(renderer, prevTex);
	}
}

/* Backbuffer Functions */

static void OPENGL_INTERNAL_CreateBackbuffer(
	OpenGLRenderer *renderer,
	FNA3D_PresentationParameters *parameters
) {
	int32_t useFauxBackbuffer;
	int32_t drawX, drawY;
	SDL_GetWindowSizeInPixels(
		(SDL_Window*) parameters->deviceWindowHandle,
		&drawX,
		&drawY
	);
	useFauxBackbuffer = (	drawX != parameters->backBufferWidth ||
				drawY != parameters->backBufferHeight	);
	useFauxBackbuffer = (	useFauxBackbuffer ||
				(parameters->multiSampleCount > 0)	);

	if (useFauxBackbuffer)
	{
		if (	renderer->backbuffer == NULL ||
			renderer->backbuffer->type == BACKBUFFER_TYPE_NULL	)
		{
			if (!renderer->supports_EXT_framebuffer_blit)
			{
				FNA3D_LogError(
					"Your hardware does not support the faux-backbuffer!"
					"\n\nKeep the window/backbuffer resolution the same."
				);
				return;
			}
			if (renderer->backbuffer != NULL)
			{
				SDL_free(renderer->backbuffer);
			}
			renderer->backbuffer = (OpenGLBackbuffer*) SDL_malloc(
				sizeof(OpenGLBackbuffer)
			);
			renderer->backbuffer->type = BACKBUFFER_TYPE_OPENGL;

			renderer->backbuffer->width = parameters->backBufferWidth;
			renderer->backbuffer->height = parameters->backBufferHeight;
			renderer->backbuffer->depthFormat = parameters->depthStencilFormat;
			renderer->backbuffer->isSrgb = parameters->backBufferFormat == FNA3D_SURFACEFORMAT_COLORSRGB_EXT;
			renderer->backbuffer->multiSampleCount = parameters->multiSampleCount;
			renderer->backbuffer->opengl.texture = 0;

			/* Generate and bind the FBO. */
			renderer->glGenFramebuffers(
				1,
				&renderer->backbuffer->opengl.handle
			);
			BindFramebuffer(
				renderer,
				renderer->backbuffer->opengl.handle
			);

			/* Create and attach the color buffer */
			renderer->glGenRenderbuffers(
				1,
				&renderer->backbuffer->opengl.colorAttachment
			);
			renderer->glBindRenderbuffer(
				GL_RENDERBUFFER,
				renderer->backbuffer->opengl.colorAttachment
			);
			if (renderer->backbuffer->multiSampleCount > 0)
			{
				renderer->glRenderbufferStorageMultisample(
					GL_RENDERBUFFER,
					renderer->backbuffer->multiSampleCount,
					GL_RGBA8,
					renderer->backbuffer->width,
					renderer->backbuffer->height
				);
			}
			else
			{
				renderer->glRenderbufferStorage(
					GL_RENDERBUFFER,
					GL_RGBA8,
					renderer->backbuffer->width,
					renderer->backbuffer->height
				);
			}
			renderer->glFramebufferRenderbuffer(
				GL_FRAMEBUFFER,
				GL_COLOR_ATTACHMENT0,
				GL_RENDERBUFFER,
				renderer->backbuffer->opengl.colorAttachment
			);

			if (renderer->backbuffer->depthFormat == FNA3D_DEPTHFORMAT_NONE)
			{
				/* Don't bother creating a DS buffer */
				renderer->backbuffer->opengl.depthStencilAttachment = 0;

				/* Keep this state sane. */
				renderer->glBindRenderbuffer(
					GL_RENDERBUFFER,
					renderer->realBackbufferRBO
				);

				return;
			}

			renderer->glGenRenderbuffers(
				1,
				&renderer->backbuffer->opengl.depthStencilAttachment
			);
			renderer->glBindRenderbuffer(
				GL_RENDERBUFFER,
				renderer->backbuffer->opengl.depthStencilAttachment
			);
			if (renderer->backbuffer->multiSampleCount > 0)
			{
				renderer->glRenderbufferStorageMultisample(
					GL_RENDERBUFFER,
					renderer->backbuffer->multiSampleCount,
					XNAToGL_DepthStorage[
						renderer->backbuffer->depthFormat
					],
					renderer->backbuffer->width,
					renderer->backbuffer->height
				);
			}
			else
			{
				renderer->glRenderbufferStorage(
					GL_RENDERBUFFER,
					XNAToGL_DepthStorage[
						renderer->backbuffer->depthFormat
					],
					renderer->backbuffer->width,
					renderer->backbuffer->height
				);
			}
			renderer->glFramebufferRenderbuffer(
				GL_FRAMEBUFFER,
				GL_DEPTH_ATTACHMENT,
				GL_RENDERBUFFER,
				renderer->backbuffer->opengl.depthStencilAttachment
			);
			if (renderer->backbuffer->depthFormat == FNA3D_DEPTHFORMAT_D24S8)
			{
				renderer->glFramebufferRenderbuffer(
					GL_FRAMEBUFFER,
					GL_STENCIL_ATTACHMENT,
					GL_RENDERBUFFER,
					renderer->backbuffer->opengl.depthStencilAttachment
				);
			}

			/* Keep this state sane. */
			renderer->glBindRenderbuffer(
				GL_RENDERBUFFER,
				renderer->realBackbufferRBO
			);
		}
		else
		{
			renderer->backbuffer->width = parameters->backBufferWidth;
			renderer->backbuffer->height = parameters->backBufferHeight;
			renderer->backbuffer->isSrgb = parameters->backBufferFormat == FNA3D_SURFACEFORMAT_COLORSRGB_EXT;
			renderer->backbuffer->multiSampleCount = parameters->multiSampleCount;
			if (renderer->backbuffer->opengl.texture != 0)
			{
				renderer->glDeleteTextures(
					1,
					&renderer->backbuffer->opengl.texture
				);
				renderer->backbuffer->opengl.texture = 0;
			}

			if (renderer->renderTargetBound)
			{
				renderer->glBindFramebuffer(
					GL_FRAMEBUFFER,
					renderer->backbuffer->opengl.handle
				);
			}

			/* Detach color attachment */
			renderer->glFramebufferRenderbuffer(
				GL_FRAMEBUFFER,
				GL_COLOR_ATTACHMENT0,
				GL_RENDERBUFFER,
				0
			);

			/* Detach depth/stencil attachment, if applicable */
			if (renderer->backbuffer->opengl.depthStencilAttachment != 0)
			{
				renderer->glFramebufferRenderbuffer(
					GL_FRAMEBUFFER,
					GL_DEPTH_ATTACHMENT,
					GL_RENDERBUFFER,
					0
				);
				if (renderer->backbuffer->depthFormat == FNA3D_DEPTHFORMAT_D24S8)
				{
					renderer->glFramebufferRenderbuffer(
						GL_FRAMEBUFFER,
						GL_STENCIL_ATTACHMENT,
						GL_RENDERBUFFER,
						0
					);
				}
			}

			/* Update our color attachment to the new resolution. */
			renderer->glBindRenderbuffer(
				GL_RENDERBUFFER,
				renderer->backbuffer->opengl.colorAttachment
			);
			if (renderer->backbuffer->multiSampleCount > 0)
			{
				renderer->glRenderbufferStorageMultisample(
					GL_RENDERBUFFER,
					renderer->backbuffer->multiSampleCount,
					GL_RGBA8,
					renderer->backbuffer->width,
					renderer->backbuffer->height
				);
			}
			else
			{
				renderer->glRenderbufferStorage(
					GL_RENDERBUFFER,
					GL_RGBA8,
					renderer->backbuffer->width,
					renderer->backbuffer->height
				);
			}
			renderer->glFramebufferRenderbuffer(
				GL_FRAMEBUFFER,
				GL_COLOR_ATTACHMENT0,
				GL_RENDERBUFFER,
				renderer->backbuffer->opengl.colorAttachment
			);

			/* Generate/Delete depth/stencil attachment, if needed */
			if (parameters->depthStencilFormat == FNA3D_DEPTHFORMAT_NONE)
			{
				if (renderer->backbuffer->opengl.depthStencilAttachment != 0)
				{
					renderer->glDeleteRenderbuffers(
						1,
						&renderer->backbuffer->opengl.depthStencilAttachment
					);
					renderer->backbuffer->opengl.depthStencilAttachment = 0;
				}
			}
			else if (renderer->backbuffer->opengl.depthStencilAttachment == 0)
			{
				renderer->glGenRenderbuffers(
					1,
					&renderer->backbuffer->opengl.depthStencilAttachment
				);
			}

			/* Update the depth/stencil buffer, if applicable */
			renderer->backbuffer->depthFormat = parameters->depthStencilFormat;
			if (renderer->backbuffer->opengl.depthStencilAttachment != 0)
			{
				renderer->glBindRenderbuffer(
					GL_RENDERBUFFER,
					renderer->backbuffer->opengl.depthStencilAttachment
				);
				if (renderer->backbuffer->multiSampleCount > 0)
				{
					renderer->glRenderbufferStorageMultisample(
						GL_RENDERBUFFER,
						renderer->backbuffer->multiSampleCount,
						XNAToGL_DepthStorage[renderer->backbuffer->depthFormat],
						renderer->backbuffer->width,
						renderer->backbuffer->height
					);
				}
				else
				{
					renderer->glRenderbufferStorage(
						GL_RENDERBUFFER,
						XNAToGL_DepthStorage[renderer->backbuffer->depthFormat],
						renderer->backbuffer->width,
						renderer->backbuffer->height
					);
				}
				renderer->glFramebufferRenderbuffer(
					GL_FRAMEBUFFER,
					GL_DEPTH_ATTACHMENT,
					GL_RENDERBUFFER,
					renderer->backbuffer->opengl.depthStencilAttachment
				);
				if (renderer->backbuffer->depthFormat == FNA3D_DEPTHFORMAT_D24S8)
				{
					renderer->glFramebufferRenderbuffer(
						GL_FRAMEBUFFER,
						GL_STENCIL_ATTACHMENT,
						GL_RENDERBUFFER,
						renderer->backbuffer->opengl.depthStencilAttachment
					);
				}
			}

			if (renderer->renderTargetBound)
			{
				renderer->glBindFramebuffer(
					GL_FRAMEBUFFER,
					renderer->targetFramebuffer
				);
			}

			/* Keep this state sane. */
			renderer->glBindRenderbuffer(
				GL_RENDERBUFFER,
				renderer->realBackbufferRBO
			);
		}
	}
	else
	{
		if (	renderer->backbuffer == NULL ||
			renderer->backbuffer->type == BACKBUFFER_TYPE_OPENGL	)
		{
			if (renderer->backbuffer != NULL)
			{
				OPENGL_INTERNAL_DisposeBackbuffer(renderer);
				SDL_free(renderer->backbuffer);
			}
			renderer->backbuffer = (OpenGLBackbuffer*) SDL_malloc(
				sizeof(OpenGLBackbuffer)
			);
			renderer->backbuffer->type = BACKBUFFER_TYPE_NULL;
		}
		renderer->backbuffer->width = parameters->backBufferWidth;
		renderer->backbuffer->height = parameters->backBufferHeight;
		renderer->backbuffer->depthFormat = renderer->windowDepthFormat;
		renderer->backbuffer->isSrgb = parameters->backBufferFormat == FNA3D_SURFACEFORMAT_COLORSRGB_EXT;
		renderer->backbuffer->multiSampleCount = 0;
	}

	if (renderer->backbuffer)
	{
		ApplySRGBFlag(renderer, renderer->backbuffer->isSrgb);
	}
}

static void OPENGL_INTERNAL_DisposeBackbuffer(OpenGLRenderer *renderer)
{
	#define GLBACKBUFFER renderer->backbuffer->opengl

	BindFramebuffer(renderer, renderer->realBackbufferFBO);
	renderer->glDeleteFramebuffers(1, &GLBACKBUFFER.handle);
	renderer->glDeleteRenderbuffers(1, &GLBACKBUFFER.colorAttachment);
	if (GLBACKBUFFER.depthStencilAttachment != 0)
	{
		renderer->glDeleteRenderbuffers(1, &GLBACKBUFFER.depthStencilAttachment);
	}
	if (GLBACKBUFFER.texture != 0)
	{
		renderer->glDeleteTextures(1, &GLBACKBUFFER.texture);
	}
	GLBACKBUFFER.handle = 0;

	#undef GLBACKBUFFER
}

static uint8_t OPENGL_INTERNAL_ReadTargetIfApplicable(
	FNA3D_Renderer *driverData,
	FNA3D_Texture* textureIn,
	int32_t level,
	void* data,
	int32_t subX,
	int32_t subY,
	int32_t subW,
	int32_t subH
) {
	GLuint prevReadBuffer, prevWriteBuffer;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *texture = (OpenGLTexture*) textureIn;
	uint8_t texUnbound = (	renderer->currentDrawBuffers != 1 ||
				renderer->currentAttachments[0] != texture->handle	);
	if (texUnbound && !renderer->useES3)
	{
		return 0;
	}

	prevReadBuffer = renderer->currentReadFramebuffer;
	prevWriteBuffer = renderer->currentDrawFramebuffer;
	if (texUnbound)
	{
		BindFramebuffer(renderer, renderer->resolveFramebufferRead);
		renderer->glFramebufferTexture2D(
			GL_FRAMEBUFFER,
			GL_COLOR_ATTACHMENT0,
			GL_TEXTURE_2D,
			texture->handle,
			level
		);
	}
	else
	{
		BindReadFramebuffer(renderer, renderer->targetFramebuffer);
	}

	/* glReadPixels should be faster than reading
	 * back from the render target if we are already bound.
	 */
	renderer->glReadPixels(
		subX,
		subY,
		subW,
		subH,
		GL_RGBA, /* FIXME: Assumption! */
		GL_UNSIGNED_BYTE,
		data
	);

	if (texUnbound)
	{
		if (prevReadBuffer == prevWriteBuffer)
		{
			BindFramebuffer(renderer, prevReadBuffer);
		}
		else
		{
			BindReadFramebuffer(renderer, prevReadBuffer);
			BindDrawFramebuffer(renderer, prevWriteBuffer);
		}
	}
	else
	{
		BindReadFramebuffer(renderer, prevReadBuffer);
	}
	return 1;
}

static void OPENGL_INTERNAL_SetPresentationInterval(
	FNA3D_PresentInterval presentInterval,
	uint8_t isEGL
) {
	int32_t enableLateSwapTear;

	if (	presentInterval == FNA3D_PRESENTINTERVAL_DEFAULT ||
		presentInterval == FNA3D_PRESENTINTERVAL_ONE	)
	{
		enableLateSwapTear = (
			!isEGL &&
			SDL_GetHintBoolean("FNA3D_ENABLE_LATESWAPTEAR", 0)
		);
		if (!enableLateSwapTear)
		{
			SDL_GL_SetSwapInterval(1);
		}
		else
		{
#ifdef USE_SDL3
			if (SDL_GL_SetSwapInterval(-1))
#else
			if (SDL_GL_SetSwapInterval(-1) != -1)
#endif
			{
				FNA3D_LogInfo(
					"Using EXT_swap_control_tear VSync!"
				);
			}
			else
			{
				FNA3D_LogInfo(
					"EXT_swap_control_tear unsupported."
					" Fall back to standard VSync."
				);
				SDL_ClearError();
				SDL_GL_SetSwapInterval(1);
			}
		}
	}
	else if (presentInterval == FNA3D_PRESENTINTERVAL_IMMEDIATE)
	{
		SDL_GL_SetSwapInterval(0);
	}
	else if (presentInterval == FNA3D_PRESENTINTERVAL_TWO)
	{
		SDL_GL_SetSwapInterval(2);
	}
	else
	{
		FNA3D_LogError(
			"Unrecognized PresentInterval: %d",
			presentInterval
		);
	}
}

static void OPENGL_ResetBackbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_PresentationParameters *presentationParameters
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OPENGL_INTERNAL_CreateBackbuffer(renderer, presentationParameters);
	OPENGL_INTERNAL_SetPresentationInterval(
		presentationParameters->presentationInterval,
		renderer->isEGL
	);
}

static void OPENGL_ReadBackbuffer(
	FNA3D_Renderer *driverData,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	void* data,
	int32_t dataLength
) {
	GLuint prevReadBuffer, prevDrawBuffer;
	int32_t pitch, row;
	uint8_t *temp;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	uint8_t *dataPtr = (uint8_t*) data;

	prevReadBuffer = renderer->currentReadFramebuffer;

	if (renderer->backbuffer->multiSampleCount > 0)
	{
		/* We have to resolve the renderbuffer to a texture first. */
		prevDrawBuffer = renderer->currentDrawFramebuffer;

		if (renderer->backbuffer->opengl.texture == 0)
		{
			renderer->glGenTextures(
				1,
				&renderer->backbuffer->opengl.texture
			);
			renderer->glBindTexture(
				GL_TEXTURE_2D,
				renderer->backbuffer->opengl.texture
			);
			renderer->glTexImage2D(
				GL_TEXTURE_2D,
				0,
				GL_RGBA,
				renderer->backbuffer->width,
				renderer->backbuffer->height,
				0,
				GL_RGBA,
				GL_UNSIGNED_BYTE,
				NULL
			);
			renderer->glBindTexture(
				renderer->textures[0]->target,
				renderer->textures[0]->handle
			);
		}
		BindFramebuffer(renderer, renderer->resolveFramebufferDraw);
		renderer->glFramebufferTexture2D(
			GL_FRAMEBUFFER,
			GL_COLOR_ATTACHMENT0,
			GL_TEXTURE_2D,
			renderer->backbuffer->opengl.texture,
			0
		);
		BindReadFramebuffer(renderer, renderer->backbuffer->opengl.handle);
		renderer->glBlitFramebuffer(
			0, 0, renderer->backbuffer->width, renderer->backbuffer->height,
			0, 0, renderer->backbuffer->width, renderer->backbuffer->height,
			GL_COLOR_BUFFER_BIT,
			GL_LINEAR
		);
		/* Don't invalidate the backbuffer here! */
		BindDrawFramebuffer(renderer, prevDrawBuffer);
		BindReadFramebuffer(renderer, renderer->resolveFramebufferDraw);
	}
	else
	{
		BindReadFramebuffer(
			renderer,
			(renderer->backbuffer->type == BACKBUFFER_TYPE_OPENGL) ?
				renderer->backbuffer->opengl.handle :
				0
		);
	}

	renderer->glReadPixels(
		x,
		y,
		w,
		h,
		GL_RGBA,
		GL_UNSIGNED_BYTE,
		data
	);

	BindReadFramebuffer(renderer, prevReadBuffer);

	/* Now we get to do a software-based flip! Yes, really! -flibit */
	pitch = w * 4;
	temp = (uint8_t*) SDL_malloc(pitch);
	for (row = 0; row < h / 2; row += 1)
	{
		/* Top to temp, bottom to top, temp to bottom */
		SDL_memcpy(temp, dataPtr + (row * pitch), pitch);
		SDL_memcpy(dataPtr + (row * pitch), dataPtr + ((h - row - 1) * pitch), pitch);
		SDL_memcpy(dataPtr + ((h - row - 1) * pitch), temp, pitch);
	}
	SDL_free(temp);
}

static void OPENGL_GetBackbufferSize(
	FNA3D_Renderer *driverData,
	int32_t *w,
	int32_t *h
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	*w = renderer->backbuffer->width;
	*h = renderer->backbuffer->height;
}

static FNA3D_SurfaceFormat OPENGL_GetBackbufferSurfaceFormat(
	FNA3D_Renderer *driverData
) {
	return FNA3D_SURFACEFORMAT_COLOR;
}

static FNA3D_DepthFormat OPENGL_GetBackbufferDepthFormat(
	FNA3D_Renderer *driverData
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return renderer->backbuffer->depthFormat;
}

static int32_t OPENGL_GetBackbufferMultiSampleCount(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return renderer->backbuffer->multiSampleCount;
}

/* Textures */

static inline OpenGLTexture* OPENGL_INTERNAL_CreateTexture(
	OpenGLRenderer *renderer,
	GLenum target,
	FNA3D_SurfaceFormat format,
	int32_t levelCount
) {
	OpenGLTexture* result = (OpenGLTexture*) SDL_malloc(
		sizeof(OpenGLTexture)
	);

	renderer->glGenTextures(1, &result->handle);
	result->target = target;
	result->hasMipmaps = (levelCount > 1);
	result->wrapS = FNA3D_TEXTUREADDRESSMODE_WRAP;
	result->wrapT = FNA3D_TEXTUREADDRESSMODE_WRAP;
	result->wrapR = FNA3D_TEXTUREADDRESSMODE_WRAP;
	result->filter = FNA3D_TEXTUREFILTER_LINEAR;
	result->anisotropy = 4.0f;
	result->maxMipmapLevel = 0;
	result->lodBias = 0.0f;
	result->format = format;
	result->next = NULL;
	result->external = 0;

	BindTexture(renderer, result);
	renderer->glTexParameteri(
		result->target,
		GL_TEXTURE_WRAP_S,
		XNAToGL_Wrap[result->wrapS]
	);
	renderer->glTexParameteri(
		result->target,
		GL_TEXTURE_WRAP_T,
		XNAToGL_Wrap[result->wrapT]
	);
	renderer->glTexParameteri(
		result->target,
		GL_TEXTURE_WRAP_R,
		XNAToGL_Wrap[result->wrapR]
	);
	renderer->glTexParameteri(
		result->target,
		GL_TEXTURE_MAG_FILTER,
		XNAToGL_MagFilter[result->filter]
	);
	renderer->glTexParameteri(
		result->target,
		GL_TEXTURE_MIN_FILTER,
		result->hasMipmaps ?
			XNAToGL_MinMipFilter[result->filter] :
			XNAToGL_MinFilter[result->filter]
	);
	if (renderer->supports_anisotropic_filtering)
	{
		renderer->glTexParameterf(
			result->target,
			GL_TEXTURE_MAX_ANISOTROPY_EXT,
			(result->filter == FNA3D_TEXTUREFILTER_ANISOTROPIC) ?
				SDL_max(result->anisotropy, 1.0f) :
				1.0f
		);
	}
	renderer->glTexParameteri(
		result->target,
		GL_TEXTURE_BASE_LEVEL,
		result->maxMipmapLevel
	);
	if (!renderer->useES3)
	{
		renderer->glTexParameterf(
			result->target,
			GL_TEXTURE_LOD_BIAS,
			result->lodBias
		);
	}
	return result;
}

static inline int32_t OPENGL_INTERNAL_Texture_GetPixelStoreAlignment(
	FNA3D_SurfaceFormat format
) {
	/* https://github.com/FNA-XNA/FNA/pull/238
	 * https://www.khronos.org/registry/OpenGL/specs/gl/glspec21.pdf
	 * OpenGL 2.1 Specification, section 3.6.1, table 3.1 specifies that
	 * the pixelstorei alignment cannot exceed 8
	 */
	return SDL_min(8, Texture_GetFormatSize(format));
}

static FNA3D_Texture* OPENGL_CreateTexture2D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *result;
	GLenum glFormat, glInternalFormat, glType;
	int32_t blockSize, levelWidth, levelHeight, i;
	uint32_t requiredBytes;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_CREATETEXTURE2D;
		cmd.createTexture2D.format = format;
		cmd.createTexture2D.width = width;
		cmd.createTexture2D.height = height;
		cmd.createTexture2D.levelCount = levelCount;
		cmd.createTexture2D.isRenderTarget = isRenderTarget;
		ForceToMainThread(renderer, &cmd);
		return cmd.createTexture2D.retval;
	}

	result = (OpenGLTexture*) OPENGL_INTERNAL_CreateTexture(
		renderer,
		GL_TEXTURE_2D,
		format,
		levelCount
	);

	result->twod.width = width;
	result->twod.height = height;

	glFormat = XNAToGL_TextureFormat[format];
	glInternalFormat = XNAToGL_TextureInternalFormat[format];
	if (glFormat == GL_COMPRESSED_TEXTURE_FORMATS)
	{
		for (i = 0; i < levelCount; i += 1)
		{
			levelWidth = SDL_max(width >> i, 1);
			levelHeight = SDL_max(height >> i, 1);
			blockSize = Texture_GetBlockSize(format);
			requiredBytes =
				(int32_t) ((levelWidth + (blockSize-1)) / blockSize) *
				(int32_t) ((levelHeight + (blockSize-1)) / blockSize) *
				Texture_GetFormatSize(format);

			renderer->glCompressedTexImage2D(
				GL_TEXTURE_2D,
				i,
				glInternalFormat,
				levelWidth,
				levelHeight,
				0,
				requiredBytes,
				NULL
			);
		}
	}
	else
	{
		glType = XNAToGL_TextureDataType[format];
		for (i = 0; i < levelCount; i += 1)
		{
			renderer->glTexImage2D(
				GL_TEXTURE_2D,
				i,
				glInternalFormat,
				SDL_max(width >> i, 1),
				SDL_max(height >> i, 1),
				0,
				glFormat,
				glType,
				NULL
			);
		}
	}

	return (FNA3D_Texture*) result;
}

static FNA3D_Texture* OPENGL_CreateTexture3D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *result;
	GLenum glFormat, glInternalFormat, glType;
	int32_t i;
	FNA3D_Command cmd;

	SDL_assert(renderer->supports_3DTexture);

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_CREATETEXTURE3D;
		cmd.createTexture3D.format = format;
		cmd.createTexture3D.width = width;
		cmd.createTexture3D.height = height;
		cmd.createTexture3D.depth = depth;
		cmd.createTexture3D.levelCount = levelCount;
		ForceToMainThread(renderer, &cmd);
		return cmd.createTexture3D.retval;
	}

	result = OPENGL_INTERNAL_CreateTexture(
		renderer,
		GL_TEXTURE_3D,
		format,
		levelCount
	);

	glFormat = XNAToGL_TextureFormat[format];
	glInternalFormat = XNAToGL_TextureInternalFormat[format];
	glType = XNAToGL_TextureDataType[format];
	for (i = 0; i < levelCount; i += 1)
	{
		renderer->glTexImage3D(
			GL_TEXTURE_3D,
			i,
			glInternalFormat,
			SDL_max(width >> i, 1),
			SDL_max(height >> i, 1),
			SDL_max(depth >> i, 1),
			0,
			glFormat,
			glType,
			NULL
		);
	}
	return (FNA3D_Texture*) result;
}

static FNA3D_Texture* OPENGL_CreateTextureCube(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *result;
	GLenum glFormat, glInternalFormat;
	int32_t blockSize, levelSize, i, l;
	uint32_t requiredBytes;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_CREATETEXTURECUBE;
		cmd.createTextureCube.format = format;
		cmd.createTextureCube.size = size;
		cmd.createTextureCube.levelCount = levelCount;
		cmd.createTextureCube.isRenderTarget = isRenderTarget;
		ForceToMainThread(renderer, &cmd);
		return cmd.createTextureCube.retval;
	}

	result = OPENGL_INTERNAL_CreateTexture(
		renderer,
		GL_TEXTURE_CUBE_MAP,
		format,
		levelCount
	);

	result->cube.size = size;

	glFormat = XNAToGL_TextureFormat[format];
	glInternalFormat = XNAToGL_TextureInternalFormat[format];
	if (glFormat == GL_COMPRESSED_TEXTURE_FORMATS)
	{
		for (i = 0; i < 6; i += 1)
		{
			for (l = 0; l < levelCount; l += 1)
			{
				levelSize = SDL_max(size >> l, 1);
				blockSize = Texture_GetBlockSize(format);
				requiredBytes =
					(int32_t) ((levelSize + (blockSize-1)) / blockSize) *
					(int32_t) ((levelSize + (blockSize-1)) / blockSize) *
					Texture_GetFormatSize(format);

				renderer->glCompressedTexImage2D(
					GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
					l,
					glInternalFormat,
					levelSize,
					levelSize,
					0,
					requiredBytes,
					NULL
				);
			}
		}
	}
	else
	{
		GLenum glType = XNAToGL_TextureDataType[format];
		for (i = 0; i < 6; i += 1)
		{
			for (l = 0; l < levelCount; l += 1)
			{
				levelSize = SDL_max(size >> l, 1);
				renderer->glTexImage2D(
					GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
					l,
					glInternalFormat,
					levelSize,
					levelSize,
					0,
					glFormat,
					glType,
					NULL
				);
			}
		}
	}

	return (FNA3D_Texture*) result;
}

static void OPENGL_INTERNAL_DestroyTexture(
	OpenGLRenderer *renderer,
	OpenGLTexture *texture
) {
	int32_t i;
	for (i = 0; i < renderer->numAttachments; i += 1)
	{
		if (texture->handle == renderer->currentAttachments[i])
		{
			/* Force an attachment update, this no longer exists! */
			renderer->currentAttachments[i] = UINT32_MAX;
		}
	}
	for (i = 0; i < renderer->numTextureSlots + renderer->numVertexTextureSlots; i += 1)
	{
		if (renderer->textures[i] == texture)
		{
			/* Remove this texture from the sampler cache */
			renderer->textures[i] = &NullTexture;
		}
	}
	if (!texture->external)
	{
		renderer->glDeleteTextures(1, &texture->handle);
	}
	SDL_free(texture);
}

static void OPENGL_AddDisposeTexture(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *glTexture = (OpenGLTexture*) texture;
	OpenGLTexture *curr;

	if (renderer->threadID == SDL_GetCurrentThreadID())
	{
		OPENGL_INTERNAL_DestroyTexture(renderer, glTexture);
	}
	else
	{
		SDL_LockMutex(renderer->disposeTexturesLock);
		LinkedList_Add(renderer->disposeTextures, glTexture, curr);
		SDL_UnlockMutex(renderer->disposeTexturesLock);
	}
}

static void OPENGL_SetTextureData2D(
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
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *glTexture = (OpenGLTexture*) texture;
	GLenum glFormat;
	int32_t packSize;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_SETTEXTUREDATA2D;
		cmd.setTextureData2D.texture = texture;
		cmd.setTextureData2D.x = x;
		cmd.setTextureData2D.y = y;
		cmd.setTextureData2D.w = w;
		cmd.setTextureData2D.h = h;
		cmd.setTextureData2D.level = level;
		cmd.setTextureData2D.data = data;
		cmd.setTextureData2D.dataLength = dataLength;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	BindTexture(renderer, glTexture);

	glFormat = XNAToGL_TextureFormat[glTexture->format];
	if (glFormat == GL_COMPRESSED_TEXTURE_FORMATS)
	{
		/* Note that we're using glInternalFormat, not glFormat.
		 * In this case, they should actually be the same thing,
		 * but we use glFormat somewhat differently for
		 * compressed textures.
		 * -flibit
		 */
		renderer->glCompressedTexSubImage2D(
			GL_TEXTURE_2D,
			level,
			x,
			y,
			w,
			h,
			XNAToGL_TextureInternalFormat[glTexture->format],
			dataLength,
			data
		);
	}
	else
	{
		/* Set pixel alignment to match texel size in bytes. */
		packSize = OPENGL_INTERNAL_Texture_GetPixelStoreAlignment(glTexture->format);
		if (packSize != 4)
		{
			renderer->glPixelStorei(
				GL_UNPACK_ALIGNMENT,
				packSize
			);
		}

		renderer->glTexSubImage2D(
			GL_TEXTURE_2D,
			level,
			x,
			y,
			w,
			h,
			glFormat,
			XNAToGL_TextureDataType[glTexture->format],
			data
		);

		/* Keep this state sane -flibit */
		if (packSize != 4)
		{
			renderer->glPixelStorei(
				GL_UNPACK_ALIGNMENT,
				4
			);
		}
	}
}

static void OPENGL_SetTextureData3D(
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
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *glTexture = (OpenGLTexture*) texture;
	FNA3D_Command cmd;

	SDL_assert(renderer->supports_3DTexture);

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_SETTEXTUREDATA3D;
		cmd.setTextureData3D.texture = texture;
		cmd.setTextureData3D.x = x;
		cmd.setTextureData3D.y = y;
		cmd.setTextureData3D.z = z;
		cmd.setTextureData3D.w = w;
		cmd.setTextureData3D.h = h;
		cmd.setTextureData3D.d = d;
		cmd.setTextureData3D.level = level;
		cmd.setTextureData3D.data = data;
		cmd.setTextureData3D.dataLength = dataLength;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	BindTexture(renderer, glTexture);

	renderer->glTexSubImage3D(
		GL_TEXTURE_3D,
		level,
		x,
		y,
		z,
		w,
		h,
		d,
		XNAToGL_TextureFormat[glTexture->format],
		XNAToGL_TextureDataType[glTexture->format],
		data
	);
}

static void OPENGL_SetTextureDataCube(
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
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *glTexture = (OpenGLTexture*) texture;
	GLenum glFormat;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_SETTEXTUREDATACUBE;
		cmd.setTextureDataCube.texture = texture;
		cmd.setTextureDataCube.x = x;
		cmd.setTextureDataCube.y = y;
		cmd.setTextureDataCube.w = w;
		cmd.setTextureDataCube.h = h;
		cmd.setTextureDataCube.cubeMapFace = cubeMapFace;
		cmd.setTextureDataCube.level = level;
		cmd.setTextureDataCube.data = data;
		cmd.setTextureDataCube.dataLength = dataLength;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	BindTexture(renderer, glTexture);

	glFormat = XNAToGL_TextureFormat[glTexture->format];
	if (glFormat == GL_COMPRESSED_TEXTURE_FORMATS)
	{
		/* Note that we're using glInternalFormat, not glFormat.
		 * In this case, they should actually be the same thing,
		 * but we use glFormat somewhat differently for
		 * compressed textures.
		 * -flibit
		 */
		renderer->glCompressedTexSubImage2D(
			GL_TEXTURE_CUBE_MAP_POSITIVE_X + cubeMapFace,
			level,
			x,
			y,
			w,
			h,
			XNAToGL_TextureInternalFormat[glTexture->format],
			dataLength,
			data
		);
	}
	else
	{
		renderer->glTexSubImage2D(
			GL_TEXTURE_CUBE_MAP_POSITIVE_X + cubeMapFace,
			level,
			x,
			y,
			w,
			h,
			glFormat,
			XNAToGL_TextureDataType[glTexture->format],
			data
		);
	}
}

static void OPENGL_SetTextureDataYUV(
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
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	uint8_t *dataPtr = (uint8_t*) data;

	renderer->glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
	BindTexture(renderer, (OpenGLTexture*) y);
	renderer->glTexSubImage2D(
		GL_TEXTURE_2D,
		0,
		0,
		0,
		yWidth,
		yHeight,
		GL_ALPHA,
		GL_UNSIGNED_BYTE,
		dataPtr
	);
	dataPtr += yWidth * yHeight;
	BindTexture(renderer, (OpenGLTexture*) u);
	renderer->glTexSubImage2D(
		GL_TEXTURE_2D,
		0,
		0,
		0,
		uvWidth,
		uvHeight,
		GL_ALPHA,
		GL_UNSIGNED_BYTE,
		dataPtr
	);
	dataPtr += uvWidth * uvHeight;
	BindTexture(renderer, (OpenGLTexture*) v);
	renderer->glTexSubImage2D(
		GL_TEXTURE_2D,
		0,
		0,
		0,
		uvWidth,
		uvHeight,
		GL_ALPHA,
		GL_UNSIGNED_BYTE,
		dataPtr
	);
	renderer->glPixelStorei(GL_UNPACK_ALIGNMENT, 4);
}

static void OPENGL_GetTextureData2D(
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
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *glTexture;
	GLenum glFormat;
	int32_t glFormatSize;
	uint8_t *texData;
	int32_t row;
	int32_t textureWidth, textureHeight;
	uint8_t *dataPtr = (uint8_t*) data;
	FNA3D_Command cmd;

	SDL_assert(renderer->supports_NonES3);

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_GETTEXTUREDATA2D;
		cmd.getTextureData2D.texture = texture;
		cmd.getTextureData2D.x = x;
		cmd.getTextureData2D.y = y;
		cmd.getTextureData2D.w = w;
		cmd.getTextureData2D.h = h;
		cmd.getTextureData2D.level = level;
		cmd.getTextureData2D.data = data;
		cmd.getTextureData2D.dataLength = dataLength;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	if (level == 0 && OPENGL_INTERNAL_ReadTargetIfApplicable(
		driverData,
		texture,
		level,
		data,
		x,
		y,
		w,
		h
	)) {
		return;
	}

	glTexture = (OpenGLTexture*) texture;
	textureWidth = glTexture->twod.width >> level;
	textureHeight = glTexture->twod.height >> level;
	BindTexture(renderer, glTexture);
	glFormat = XNAToGL_TextureFormat[glTexture->format];
	if (glFormat == GL_COMPRESSED_TEXTURE_FORMATS)
	{
		FNA3D_LogError(
			"GetData with compressed textures unsupported!"
		);
		return;
	}
	else if (	x == 0 &&
			y == 0 &&
			w == textureWidth &&
			h == textureHeight	)
	{
		/* Just throw the whole texture into the user array. */
		renderer->glGetTexImage(
			GL_TEXTURE_2D,
			level,
			glFormat,
			XNAToGL_TextureDataType[glTexture->format],
			data
		);
	}
	else
	{
		glFormatSize = Texture_GetFormatSize(glTexture->format);

		/* Get the whole texture... */
		texData = (uint8_t*) SDL_malloc(
			textureWidth *
			textureHeight *
			glFormatSize
		);

		renderer->glGetTexImage(
			GL_TEXTURE_2D,
			level,
			glFormat,
			XNAToGL_TextureDataType[glTexture->format],
			texData
		);

		/* Now, blit the rect region into the user array. */
		for (row = y; row < y + h; row += 1)
		{
			SDL_memcpy(
				dataPtr,
				texData + (((row * textureWidth) + x) * glFormatSize),
				glFormatSize * w
			);
			dataPtr += glFormatSize * w;
		}
		SDL_free(texData);
	}
}

static void OPENGL_GetTextureData3D(
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
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	SDL_assert(renderer->supports_NonES3);

	FNA3D_LogError(
		"GetTextureData3D is unsupported!"
	);
}

static void OPENGL_GetTextureDataCube(
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
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLTexture *glTexture;
	GLenum glFormat;
	int32_t glFormatSize;
	uint8_t *texData;
	int32_t row;
	int32_t textureSize;
	uint8_t *dataPtr = (uint8_t*) data;
	FNA3D_Command cmd;

	SDL_assert(renderer->supports_NonES3);

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_GETTEXTUREDATACUBE;
		cmd.getTextureDataCube.texture = texture;
		cmd.getTextureDataCube.x = x;
		cmd.getTextureDataCube.y = y;
		cmd.getTextureDataCube.w = w;
		cmd.getTextureDataCube.h = h;
		cmd.getTextureDataCube.cubeMapFace = cubeMapFace;
		cmd.getTextureDataCube.level = level;
		cmd.getTextureDataCube.data = data;
		cmd.getTextureDataCube.dataLength = dataLength;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	glTexture = (OpenGLTexture*) texture;
	textureSize = glTexture->cube.size >> level;
	BindTexture(renderer, glTexture);
	glFormat = XNAToGL_TextureFormat[glTexture->format];
	if (glFormat == GL_COMPRESSED_TEXTURE_FORMATS)
	{
		FNA3D_LogError(
			"GetData with compressed textures unsupported!"
		);
		return;
	}
	else if (	x == 0 &&
			y == 0 &&
			w == textureSize &&
			h == textureSize	)
	{
		/* Just throw the whole texture into the user array. */
		renderer->glGetTexImage(
			GL_TEXTURE_CUBE_MAP_POSITIVE_X + cubeMapFace,
			level,
			glFormat,
			XNAToGL_TextureDataType[glTexture->format],
			data
		);
	}
	else
	{
		glFormatSize = Texture_GetFormatSize(glTexture->format);

		/* Get the whole texture... */
		texData = (uint8_t*) SDL_malloc(
			textureSize *
			textureSize *
			glFormatSize
		);

		renderer->glGetTexImage(
			GL_TEXTURE_CUBE_MAP_POSITIVE_X + cubeMapFace,
			level,
			glFormat,
			XNAToGL_TextureDataType[glTexture->format],
			texData
		);

		/* Now, blit the rect region into the user array. */
		for (row = y; row < y + h; row += 1)
		{
			SDL_memcpy(
				dataPtr,
				texData + (((row * textureSize) + x) * glFormatSize),
				glFormatSize * w
			);
			dataPtr += glFormatSize * w;
		}
		SDL_free(texData);
	}
}

/* Renderbuffers */

static FNA3D_Renderbuffer* OPENGL_GenColorRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLRenderbuffer *renderbuffer;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_GENCOLORRENDERBUFFER;
		cmd.genColorRenderbuffer.width = width;
		cmd.genColorRenderbuffer.height = height;
		cmd.genColorRenderbuffer.format = format;
		cmd.genColorRenderbuffer.multiSampleCount = multiSampleCount;
		cmd.genColorRenderbuffer.texture = texture;
		ForceToMainThread(renderer, &cmd);
		return cmd.genColorRenderbuffer.retval;
	}

	renderbuffer = (OpenGLRenderbuffer*) SDL_malloc(
		sizeof(OpenGLRenderbuffer)
	);
	renderbuffer->next = NULL;
	renderbuffer->format = format;

	renderer->glGenRenderbuffers(1, &renderbuffer->handle);
	renderer->glBindRenderbuffer(GL_RENDERBUFFER, renderbuffer->handle);
	if (multiSampleCount > 0)
	{
		renderer->glRenderbufferStorageMultisample(
			GL_RENDERBUFFER,
			multiSampleCount,
			XNAToGL_TextureInternalFormat[format],
			width,
			height
		);
	}
	else
	{
		renderer->glRenderbufferStorage(
			GL_RENDERBUFFER,
			XNAToGL_TextureInternalFormat[format],
			width,
			height
		);
	}
	renderer->glBindRenderbuffer(GL_RENDERBUFFER, renderer->realBackbufferRBO);

	return (FNA3D_Renderbuffer*) renderbuffer;
}

static FNA3D_Renderbuffer* OPENGL_GenDepthStencilRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLRenderbuffer *renderbuffer;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_GENDEPTHRENDERBUFFER;
		cmd.genDepthStencilRenderbuffer.width = width;
		cmd.genDepthStencilRenderbuffer.height = height;
		cmd.genDepthStencilRenderbuffer.format = format;
		cmd.genDepthStencilRenderbuffer.multiSampleCount = multiSampleCount;
		ForceToMainThread(renderer, &cmd);
		return cmd.genDepthStencilRenderbuffer.retval;
	}

	renderbuffer = (OpenGLRenderbuffer*) SDL_malloc(
		sizeof(OpenGLRenderbuffer)
	);
	renderbuffer->next = NULL;

	renderer->glGenRenderbuffers(1, &renderbuffer->handle);
	renderer->glBindRenderbuffer(GL_RENDERBUFFER, renderbuffer->handle);
	if (multiSampleCount > 0)
	{
		renderer->glRenderbufferStorageMultisample(
			GL_RENDERBUFFER,
			multiSampleCount,
			XNAToGL_DepthStorage[format],
			width,
			height
		);
	}
	else
	{
		renderer->glRenderbufferStorage(
			GL_RENDERBUFFER,
			XNAToGL_DepthStorage[format],
			width,
			height
		);
	}
	renderer->glBindRenderbuffer(GL_RENDERBUFFER, renderer->realBackbufferRBO);

	return (FNA3D_Renderbuffer*) renderbuffer;
}

static void OPENGL_INTERNAL_DestroyRenderbuffer(
	OpenGLRenderer *renderer,
	OpenGLRenderbuffer *renderbuffer
) {
	/* Check color attachments */
	int32_t i;
	for (i = 0; i < renderer->numAttachments; i += 1)
	{
		if (renderbuffer->handle == renderer->currentAttachments[i])
		{
			/* Force an attachment update, this no longer exists! */
			renderer->currentAttachments[i] = UINT32_MAX;
		}
	}

	/* Check depth/stencil attachment */
	if (renderbuffer->handle == renderer->currentRenderbuffer)
	{
		/* Force a renderbuffer update, this no longer exists! */
		renderer->currentRenderbuffer = UINT32_MAX;
	}

	/* Finally. */
	renderer->glDeleteRenderbuffers(1, &renderbuffer->handle);
	SDL_free(renderbuffer);
}

static void OPENGL_AddDisposeRenderbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Renderbuffer *renderbuffer
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLRenderbuffer *buffer = (OpenGLRenderbuffer*) renderbuffer;
	OpenGLRenderbuffer *curr;

	if (renderer->threadID == SDL_GetCurrentThreadID())
	{
		OPENGL_INTERNAL_DestroyRenderbuffer(renderer, buffer);
	}
	else
	{
		SDL_LockMutex(renderer->disposeRenderbuffersLock);
		LinkedList_Add(renderer->disposeRenderbuffers, buffer, curr);
		SDL_UnlockMutex(renderer->disposeRenderbuffersLock);
	}
}

/* Vertex Buffers */

static FNA3D_Buffer* OPENGL_GenVertexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *result = NULL;
	GLuint handle;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_GENVERTEXBUFFER;
		cmd.genVertexBuffer.dynamic = dynamic;
		cmd.genVertexBuffer.usage = usage;
		cmd.genVertexBuffer.sizeInBytes = sizeInBytes;
		ForceToMainThread(renderer, &cmd);
		return cmd.genVertexBuffer.retval;
	}

	renderer->glGenBuffers(1, &handle);

	result = (OpenGLBuffer*) SDL_malloc(sizeof(OpenGLBuffer));
	result->handle = handle;
	result->size = (intptr_t) sizeInBytes;
	result->dynamic = (dynamic ? GL_STREAM_DRAW : GL_STATIC_DRAW);
	result->next = NULL;

	BindVertexBuffer(renderer, handle);
	renderer->glBufferData(
		GL_ARRAY_BUFFER,
		result->size,
		NULL,
		result->dynamic
	);

	return (FNA3D_Buffer*) result;
}

static void OPENGL_INTERNAL_DestroyVertexBuffer(
	OpenGLRenderer *renderer,
	OpenGLBuffer *buffer
) {
	int32_t i;

	if (buffer->handle == renderer->currentVertexBuffer)
	{
		renderer->glBindBuffer(GL_ARRAY_BUFFER, 0);
		renderer->currentVertexBuffer = 0;
	}
	for (i = 0; i < renderer->numVertexAttributes; i += 1)
	{
		if (buffer->handle == renderer->attributes[i].currentBuffer)
		{
			/* Force the next vertex attrib update! */
			renderer->attributes[i].currentBuffer = UINT32_MAX;
		}
	}
	renderer->glDeleteBuffers(1, &buffer->handle);

	SDL_free(buffer);
}

static void OPENGL_AddDisposeVertexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *glBuffer = (OpenGLBuffer*) buffer;
	OpenGLBuffer *curr;

	if (renderer->threadID == SDL_GetCurrentThreadID())
	{
		OPENGL_INTERNAL_DestroyVertexBuffer(renderer, glBuffer);
	}
	else
	{
		SDL_LockMutex(renderer->disposeVertexBuffersLock);
		LinkedList_Add(renderer->disposeVertexBuffers, glBuffer, curr);
		SDL_UnlockMutex(renderer->disposeVertexBuffersLock);
	}
}

static void OPENGL_SetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *glBuffer = (OpenGLBuffer*) buffer;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_SETVERTEXBUFFERDATA;
		cmd.setVertexBufferData.buffer = buffer;
		cmd.setVertexBufferData.offsetInBytes = offsetInBytes;
		cmd.setVertexBufferData.data = data;
		cmd.setVertexBufferData.elementCount = elementCount;
		cmd.setVertexBufferData.elementSizeInBytes = elementSizeInBytes;
		cmd.setVertexBufferData.vertexStride = vertexStride;
		cmd.setVertexBufferData.options = options;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	BindVertexBuffer(renderer, glBuffer->handle);

	/* FIXME: Staging buffer for elementSizeInBytes < vertexStride! */

	if (	options == FNA3D_SETDATAOPTIONS_NOOVERWRITE &&
		renderer->supports_ARB_map_buffer_range	)
	{
		void *ptr = renderer->glMapBufferRange(
			GL_ARRAY_BUFFER,
			(GLintptr) offsetInBytes,
			(GLsizeiptr) (elementCount * vertexStride),
			GL_MAP_WRITE_BIT | GL_MAP_UNSYNCHRONIZED_BIT
		);
		if (ptr != NULL)
		{
			SDL_memcpy(ptr, data, (elementCount * vertexStride));
			renderer->glUnmapBuffer(GL_ARRAY_BUFFER);
		}
		return;
	}

	if (options == FNA3D_SETDATAOPTIONS_DISCARD)
	{
		renderer->glBufferData(
			GL_ARRAY_BUFFER,
			glBuffer->size,
			NULL,
			glBuffer->dynamic
		);
	}

	renderer->glBufferSubData(
		GL_ARRAY_BUFFER,
		(GLintptr) offsetInBytes,
		(GLsizeiptr) (elementCount * vertexStride),
		data
	);
}

static void OPENGL_GetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *glBuffer = (OpenGLBuffer*) buffer;
	uint8_t *dataBytes, *cpy, *src, *dst;
	uint8_t useStagingBuffer;
	int32_t i;
	FNA3D_Command cmd;

	SDL_assert(renderer->supports_NonES3);

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_GETVERTEXBUFFERDATA;
		cmd.getVertexBufferData.buffer = buffer;
		cmd.getVertexBufferData.offsetInBytes = offsetInBytes;
		cmd.getVertexBufferData.data = data;
		cmd.getVertexBufferData.elementCount = elementCount;
		cmd.getVertexBufferData.elementSizeInBytes = elementSizeInBytes;
		cmd.getVertexBufferData.vertexStride = vertexStride;
		ForceToMainThread(renderer, &cmd);
		return;
	}

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

	BindVertexBuffer(renderer, glBuffer->handle);

	renderer->glGetBufferSubData(
		GL_ARRAY_BUFFER,
		(GLintptr) offsetInBytes,
		(GLsizeiptr) (elementCount * vertexStride),
		cpy
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

static FNA3D_Buffer* OPENGL_GenIndexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *result = NULL;
	GLuint handle;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_GENINDEXBUFFER;
		cmd.genIndexBuffer.dynamic = dynamic;
		cmd.genIndexBuffer.usage = usage;
		cmd.genIndexBuffer.sizeInBytes = sizeInBytes;
		ForceToMainThread(renderer, &cmd);
		return cmd.genIndexBuffer.retval;
	}

	renderer->glGenBuffers(1, &handle);

	result = (OpenGLBuffer*) SDL_malloc(sizeof(OpenGLBuffer));
	result->handle = handle;
	result->size = (intptr_t) sizeInBytes;
	result->dynamic = (dynamic ? GL_STREAM_DRAW : GL_STATIC_DRAW);
	result->next = NULL;

	BindIndexBuffer(renderer, handle);
	renderer->glBufferData(
		GL_ELEMENT_ARRAY_BUFFER,
		result->size,
		NULL,
		result->dynamic
	);

	return (FNA3D_Buffer*) result;
}

static void OPENGL_INTERNAL_DestroyIndexBuffer(
	OpenGLRenderer *renderer,
	OpenGLBuffer *buffer
) {
	if (buffer->handle == renderer->currentIndexBuffer)
	{
		renderer->glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
		renderer->currentIndexBuffer = 0;
	}
	renderer->glDeleteBuffers(1, &buffer->handle);
	SDL_free(buffer);
}

static void OPENGL_AddDisposeIndexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *glBuffer = (OpenGLBuffer*) buffer;
	OpenGLBuffer *curr;

	if (renderer->threadID == SDL_GetCurrentThreadID())
	{
		OPENGL_INTERNAL_DestroyIndexBuffer(renderer, glBuffer);
	}
	else
	{
		SDL_LockMutex(renderer->disposeIndexBuffersLock);
		LinkedList_Add(renderer->disposeIndexBuffers, glBuffer, curr);
		SDL_UnlockMutex(renderer->disposeIndexBuffersLock);
	}
}

static void OPENGL_SetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *glBuffer = (OpenGLBuffer*) buffer;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_SETINDEXBUFFERDATA;
		cmd.setIndexBufferData.buffer = buffer;
		cmd.setIndexBufferData.offsetInBytes = offsetInBytes;
		cmd.setIndexBufferData.data = data;
		cmd.setIndexBufferData.dataLength = dataLength;
		cmd.setIndexBufferData.options = options;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	BindIndexBuffer(renderer, glBuffer->handle);

	if (	options == FNA3D_SETDATAOPTIONS_NOOVERWRITE &&
		renderer->supports_ARB_map_buffer_range	)
	{
		void *ptr = renderer->glMapBufferRange(
			GL_ELEMENT_ARRAY_BUFFER,
			(GLintptr) offsetInBytes,
			(GLsizeiptr) dataLength,
			GL_MAP_WRITE_BIT | GL_MAP_UNSYNCHRONIZED_BIT
		);
		if (ptr != NULL)
		{
			SDL_memcpy(ptr, data, dataLength);
			renderer->glUnmapBuffer(GL_ELEMENT_ARRAY_BUFFER);
		}
		return;
	}

	if (options == FNA3D_SETDATAOPTIONS_DISCARD)
	{
		renderer->glBufferData(
			GL_ELEMENT_ARRAY_BUFFER,
			glBuffer->size,
			NULL,
			glBuffer->dynamic
		);
	}

	renderer->glBufferSubData(
		GL_ELEMENT_ARRAY_BUFFER,
		(GLintptr) offsetInBytes,
		(GLsizeiptr) dataLength,
		data
	);
}

static void OPENGL_GetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLBuffer *glBuffer = (OpenGLBuffer*) buffer;
	FNA3D_Command cmd;

	SDL_assert(renderer->supports_NonES3);

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_GETINDEXBUFFERDATA;
		cmd.getIndexBufferData.buffer = buffer;
		cmd.getIndexBufferData.offsetInBytes = offsetInBytes;
		cmd.getIndexBufferData.data = data;
		cmd.getIndexBufferData.dataLength = dataLength;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	BindIndexBuffer(renderer, glBuffer->handle);

	renderer->glGetBufferSubData(
		GL_ELEMENT_ARRAY_BUFFER,
		(GLintptr) offsetInBytes,
		(GLsizeiptr) dataLength,
		data
	);
}

/* Effects */

static void* MOJOSHADERCALL OPENGL_INTERNAL_CompileShader(
	const void *ctx,
	const char *mainfn,
	const unsigned char *tokenbuf,
	const unsigned int bufsize,
	const MOJOSHADER_swizzle *swiz,
	const unsigned int swizcount,
	const MOJOSHADER_samplerMap *smap,
	const unsigned int smapcount
) {
	return MOJOSHADER_glCompileShader(
		tokenbuf,
		bufsize,
		swiz,
		swizcount,
		smap,
		smapcount
	);
}

static void MOJOSHADERCALL OPENGL_INTERNAL_DeleteShader(
	const void *ctx,
	void *shader
) {
	MOJOSHADER_glShader *glShader = (MOJOSHADER_glShader*) shader;
	MOJOSHADER_glDeleteShader(glShader);
}

static void MOJOSHADERCALL OPENGL_INTERNAL_BindShaders(
	const void *ctx,
	void *vshader,
	void *pshader
) {
	MOJOSHADER_glShader *glVShader = (MOJOSHADER_glShader*) vshader;
	MOJOSHADER_glShader *glPShader = (MOJOSHADER_glShader*) pshader;

	MOJOSHADER_glBindShaders(glVShader, glPShader);
}

static void MOJOSHADERCALL OPENGL_INTERNAL_GetBoundShaders(
	const void *ctx,
	void **vshader,
	void **pshader
) {
	MOJOSHADER_glShader **glVShader = (MOJOSHADER_glShader**) vshader;
	MOJOSHADER_glShader **glPShader = (MOJOSHADER_glShader**) pshader;
	MOJOSHADER_glGetBoundShaders(glVShader, glPShader);
}

static void MOJOSHADERCALL OPENGL_INTERNAL_MapUniformBufferMemory(
	const void *ctx,
	float **vsf, int **vsi, unsigned char **vsb,
	float **psf, int **psi, unsigned char **psb
) {
	MOJOSHADER_glMapUniformBufferMemory(vsf, vsi, vsb, psf, psi, psb);
}

static void MOJOSHADERCALL OPENGL_INTERNAL_UnmapUniformBufferMemory(
	const void *ctx
) {
	MOJOSHADER_glUnmapUniformBufferMemory();
}

static const char* MOJOSHADERCALL OPENGL_INTERNAL_GetShaderError(const void *ctx)
{
	return MOJOSHADER_glGetError();
}

static void OPENGL_CreateEffect(
	FNA3D_Renderer *driverData,
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLEffect *result;
	int32_t i;
	FNA3D_Command cmd;
	MOJOSHADER_effectShaderContext shaderBackend;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_CREATEEFFECT;
		cmd.createEffect.effectCode = effectCode;
		cmd.createEffect.effectCodeLength = effectCodeLength;
		cmd.createEffect.effect = effect;
		cmd.createEffect.effectData = effectData;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	shaderBackend.shaderContext = renderer->shaderContext;
	shaderBackend.compileShader = OPENGL_INTERNAL_CompileShader;
	shaderBackend.shaderAddRef = (MOJOSHADER_shaderAddRefFunc) MOJOSHADER_glShaderAddRef;
	shaderBackend.deleteShader = OPENGL_INTERNAL_DeleteShader;
	shaderBackend.getParseData = (MOJOSHADER_getParseDataFunc) MOJOSHADER_glGetShaderParseData;
	shaderBackend.bindShaders = OPENGL_INTERNAL_BindShaders;
	shaderBackend.getBoundShaders = OPENGL_INTERNAL_GetBoundShaders;
	shaderBackend.mapUniformBufferMemory = OPENGL_INTERNAL_MapUniformBufferMemory;
	shaderBackend.unmapUniformBufferMemory = OPENGL_INTERNAL_UnmapUniformBufferMemory;
	shaderBackend.getError = OPENGL_INTERNAL_GetShaderError;
	shaderBackend.m = NULL;
	shaderBackend.f = NULL;
	shaderBackend.malloc_data = renderer;

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

	result = (OpenGLEffect*) SDL_malloc(sizeof(OpenGLEffect));
	result->effect = *effectData;
	result->next = NULL;
	*effect = (FNA3D_Effect*) result;
}

static void OPENGL_CloneEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *cloneSource,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLEffect *glCloneSource = (OpenGLEffect*) cloneSource;
	OpenGLEffect *result;
	FNA3D_Command cmd;

	if (renderer->threadID != SDL_GetCurrentThreadID())
	{
		cmd.type = FNA3D_COMMAND_CLONEEFFECT;
		cmd.cloneEffect.cloneSource = cloneSource;
		cmd.cloneEffect.effect = effect;
		cmd.cloneEffect.effectData = effectData;
		ForceToMainThread(renderer, &cmd);
		return;
	}

	*effectData = MOJOSHADER_cloneEffect(glCloneSource->effect);
	if (*effectData == NULL)
	{
		FNA3D_LogError(
			"%s", MOJOSHADER_glGetError()
		);
	}

	result = (OpenGLEffect*) SDL_malloc(sizeof(OpenGLEffect));
	result->effect = *effectData;
	result->next = NULL;
	*effect = (FNA3D_Effect*) result;
}

static void OPENGL_INTERNAL_DestroyEffect(
	OpenGLRenderer *renderer,
	OpenGLEffect *effect
) {
	MOJOSHADER_effect *glEffect = effect->effect;
	if (glEffect == renderer->currentEffect)
	{
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectEnd(renderer->currentEffect);
		renderer->currentEffect = NULL;
		renderer->currentTechnique = NULL;
		renderer->currentPass = 0;
		renderer->effectApplied = 1;
	}
	MOJOSHADER_deleteEffect(glEffect);
	SDL_free(effect);
}

static void OPENGL_AddDisposeEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLEffect *fnaEffect = (OpenGLEffect*) effect;
	OpenGLEffect *curr;

	if (renderer->threadID == SDL_GetCurrentThreadID())
	{
		OPENGL_INTERNAL_DestroyEffect(renderer, fnaEffect);
	}
	else
	{
		SDL_LockMutex(renderer->disposeEffectsLock);
		LinkedList_Add(renderer->disposeEffects, fnaEffect, curr);
		SDL_UnlockMutex(renderer->disposeEffectsLock);
	}
}

static void OPENGL_SetEffectTechnique(
	FNA3D_Renderer *renderer,
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
) {
	/* FIXME: Why doesn't this function do anything? */
	OpenGLEffect *fnaEffect = (OpenGLEffect*) effect;
	MOJOSHADER_effectSetTechnique(fnaEffect->effect, technique);
}

static void OPENGL_ApplyEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	uint32_t pass,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLEffect *fnaEffect = (OpenGLEffect*) effect;
	MOJOSHADER_effect *effectData = fnaEffect->effect;
	const MOJOSHADER_effectTechnique *technique = fnaEffect->effect->current_technique;
	uint32_t whatever;

	renderer->effectApplied = 1;
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

static void OPENGL_BeginPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	MOJOSHADER_effect *effectData = ((OpenGLEffect*) effect)->effect;
	uint32_t whatever;

	MOJOSHADER_effectBegin(
		effectData,
		&whatever,
		1,
		stateChanges
	);
	MOJOSHADER_effectBeginPass(effectData, 0);
	renderer->effectApplied = 1;
}

static void OPENGL_EndPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	MOJOSHADER_effect *effectData = ((OpenGLEffect*) effect)->effect;

	MOJOSHADER_effectEndPass(effectData);
	MOJOSHADER_effectEnd(effectData);
	renderer->effectApplied = 1;
}

/* Queries */

static FNA3D_Query* OPENGL_CreateQuery(FNA3D_Renderer *driverData)
{
	OpenGLQuery *result;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	SDL_assert(renderer->supports_ARB_occlusion_query);

	result = (OpenGLQuery*) SDL_malloc(sizeof(OpenGLQuery));
	renderer->glGenQueries(1, &result->handle);
	result->next = NULL;

	return (FNA3D_Query*) result;
}

static void OPENGL_INTERNAL_DestroyQuery(
	OpenGLRenderer *renderer,
	OpenGLQuery *query
) {
	renderer->glDeleteQueries(
		1,
		&query->handle
	);
	SDL_free(query);
}

static void OPENGL_AddDisposeQuery(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLQuery *glQuery = (OpenGLQuery*) query;
	OpenGLQuery *curr;

	SDL_assert(renderer->supports_ARB_occlusion_query);

	if (renderer->threadID == SDL_GetCurrentThreadID())
	{
		OPENGL_INTERNAL_DestroyQuery(renderer, glQuery);
	}
	else
	{
		SDL_LockMutex(renderer->disposeQueriesLock);
		LinkedList_Add(renderer->disposeQueries, glQuery, curr);
		SDL_UnlockMutex(renderer->disposeQueriesLock);
	}
}

static void OPENGL_QueryBegin(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLQuery *glQuery = (OpenGLQuery*) query;

	SDL_assert(renderer->supports_ARB_occlusion_query);

	renderer->glBeginQuery(
		GL_SAMPLES_PASSED,
		glQuery->handle
	);
}

static void OPENGL_QueryEnd(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;

	SDL_assert(renderer->supports_ARB_occlusion_query);

	/* May need to check active queries...? */
	renderer->glEndQuery(
		GL_SAMPLES_PASSED
	);
}

static uint8_t OPENGL_QueryComplete(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	GLuint result;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLQuery *glQuery = (OpenGLQuery*) query;

	SDL_assert(renderer->supports_ARB_occlusion_query);

	renderer->glGetQueryObjectuiv(
		glQuery->handle,
		GL_QUERY_RESULT_AVAILABLE,
		&result
	);
	return result != 0;
}

static int32_t OPENGL_QueryPixelCount(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	GLuint result;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	OpenGLQuery *glQuery = (OpenGLQuery*) query;

	SDL_assert(renderer->supports_ARB_occlusion_query);

	renderer->glGetQueryObjectuiv(
		glQuery->handle,
		GL_QUERY_RESULT,
		&result
	);
	return (int32_t) result;
}

/* Feature Queries */

static uint8_t OPENGL_SupportsDXT1(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return renderer->supports_dxt1;
}

static uint8_t OPENGL_SupportsS3TC(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return renderer->supports_s3tc;
}

static uint8_t OPENGL_SupportsBC7(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return renderer->supports_bc7;
}

static uint8_t OPENGL_SupportsHardwareInstancing(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return (	renderer->supports_ARB_draw_instanced &&
			renderer->supports_ARB_instanced_arrays	);
}

static uint8_t OPENGL_SupportsNoOverwrite(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return renderer->supports_ARB_map_buffer_range;
}

static uint8_t OPENGL_SupportsSRGBRenderTargets(FNA3D_Renderer *driverData)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	return renderer->supports_srgb_rendertarget;
}

static void OPENGL_GetMaxTextureSlots(
	FNA3D_Renderer *driverData,
	int32_t *textures,
	int32_t *vertexTextures
) {
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	*textures = renderer->numTextureSlots;
	*vertexTextures = renderer->numVertexTextureSlots;
}

static int32_t OPENGL_GetMaxMultiSampleCount(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount
) {
	int32_t maxSamples;
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;

	if (renderer->supports_ARB_internalformat_query)
	{
		maxSamples = renderer->maxMultiSampleCountFormat[format];
	}
	else
	{
		/* This number isn't very good, but it's all we have... */
		maxSamples = renderer->maxMultiSampleCount;
	}
	if (renderer->windowSampleCount > 0)
	{
		/* Desperate attempt to align multisample count with the window
		 * sample count, otherwise glBlitFramebuffer will return
		 * GL_INVALID_OPERATION
		 */
		maxSamples = SDL_min(maxSamples, renderer->windowSampleCount);
	}
	return SDL_min(maxSamples, multiSampleCount);
}

/* Debugging */

static void OPENGL_SetStringMarker(FNA3D_Renderer *driverData, const char *text)
{
	OpenGLRenderer *renderer = (OpenGLRenderer*) driverData;
	if (renderer->supports_GREMEDY_string_marker)
	{
		renderer->glStringMarkerGREMEDY(SDL_strlen(text), text);
	}
}

static void OPENGL_SetTextureName(FNA3D_Renderer* driverData, FNA3D_Texture* texture, const char* text)
{
	/* No OpenGL API for this that I'm aware of -kg */
	return;
}

static const char *debugSourceStr[] = {
	"GL_DEBUG_SOURCE_API",
	"GL_DEBUG_SOURCE_WINDOW_SYSTEM",
	"GL_DEBUG_SOURCE_SHADER_COMPILER",
	"GL_DEBUG_SOURCE_THIRD_PARTY",
	"GL_DEBUG_SOURCE_APPLICATION",
	"GL_DEBUG_SOURCE_OTHER"
};
static const char *debugTypeStr[] = {
	"GL_DEBUG_TYPE_ERROR",
	"GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR",
	"GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR",
	"GL_DEBUG_TYPE_PORTABILITY",
	"GL_DEBUG_TYPE_PERFORMANCE",
	"GL_DEBUG_TYPE_OTHER"
};
/* Debug severity values are disjointed */
static inline const char *GetSeverityString(GLenum severity)
{
	switch (severity)
	{
	case GL_DEBUG_SEVERITY_HIGH: return "GL_DEBUG_SEVERITY_HIGH";
	case GL_DEBUG_SEVERITY_MEDIUM: return "GL_DEBUG_SEVERITY_MEDIUM";
	case GL_DEBUG_SEVERITY_LOW: return "GL_DEBUG_SEVERITY_LOW";
	case GL_DEBUG_SEVERITY_NOTIFICATION: return "GL_DEBUG_SEVERITY_NOTIFICATION";
	default:
		return "FNA3D_UNKNOWN_SEVERITY";
	}
}

static void GLAPIENTRY DebugCall(
	GLenum source,
	GLenum type,
	GLuint id,
	GLenum severity,
	GLsizei length,
	const GLchar *message,
	const void *userParam
) {
	if (type == GL_DEBUG_TYPE_ERROR)
	{
		FNA3D_LogError(
			"%s\n\tSource: %s\n\tType: %s\n\tSeverity: %s",
			message,
			debugSourceStr[source - GL_DEBUG_SOURCE_API],
			debugTypeStr[type - GL_DEBUG_TYPE_ERROR],
			GetSeverityString(severity)
		);
	}
	else
	{
		FNA3D_LogWarn(
			"%s\n\tSource: %s\n\tType: %s\n\tSeverity: %s",
			message,
			debugSourceStr[source - GL_DEBUG_SOURCE_API],
			debugTypeStr[type - GL_DEBUG_TYPE_ERROR],
			GetSeverityString(severity)
		);
	}
}

/* External Interop */

static void OPENGL_GetSysRenderer(
	FNA3D_Renderer* driverData,
	FNA3D_SysRendererEXT *sysrenderer
) {
	OpenGLRenderer* renderer = (OpenGLRenderer*) driverData;

	sysrenderer->rendererType = FNA3D_RENDERER_TYPE_OPENGL_EXT;
	sysrenderer->renderer.opengl.context = renderer->context;
}

static FNA3D_Texture* OPENGL_CreateSysTexture(
	FNA3D_Renderer* driverData,
	FNA3D_SysTextureEXT *systexture
) {
	OpenGLTexture* result;

	if (systexture->rendererType != FNA3D_RENDERER_TYPE_OPENGL_EXT)
	{
		return NULL;
	}

	result = (OpenGLTexture*) SDL_malloc(
		sizeof(OpenGLTexture)
	);

	SDL_zerop(result);

	result->handle = systexture->texture.opengl.handle;
	result->target = (GLenum) systexture->texture.opengl.target;
	result->external = 1;

	return (FNA3D_Texture*) result;
}

/* Load GL Entry Points */

static inline void LoadEntryPoints(
	OpenGLRenderer *renderer,
	const char *driverInfo,
	uint8_t debugMode
) {
	int32_t i;
	const char *baseErrorString = (
		renderer->useES3 ?
			"OpenGL ES 3.0 support is required!" :
			"OpenGL 2.1 support is required!"
	);

	#define GL_EXT(ext) \
		renderer->supports_##ext = 1;
	#define GL_PROC(ext, ret, func, parms) \
		renderer->func = (glfntype_##func) SDL_GL_GetProcAddress(#func); \
		if (renderer->func == NULL) \
		{ \
			renderer->supports_##ext = 0; \
		}
	#define GL_PROC_EXT(ext, fallback, ret, func, parms) \
		renderer->func = (glfntype_##func) SDL_GL_GetProcAddress(#func); \
		if (renderer->func == NULL) \
		{ \
			renderer->func = (glfntype_##func) SDL_GL_GetProcAddress(#func #fallback); \
			if (renderer->func == NULL) \
			{ \
				renderer->supports_##ext = 0; \
			} \
		}
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
	#include "FNA3D_Driver_OpenGL_glfuncs.h"
#pragma GCC diagnostic pop

	/* Weeding out the GeForce FX cards... */
	if (!renderer->supports_BaseGL)
	{
		FNA3D_LogError(
			"%s\n%s",
			baseErrorString,
			driverInfo
		);
		return;
	}

	/* No depth precision whatsoever? Something's busted. */
	if (	!renderer->supports_DoublePrecisionDepth &&
		!renderer->supports_OES_single_precision	)
	{
		FNA3D_LogError(
			"%s\n%s",
			baseErrorString,
			driverInfo
		);
		return;
	}

	/* If you asked for core profile, you better have it! */
	if (renderer->useCoreProfile && !renderer->supports_CoreGL)
	{
		FNA3D_LogError(
			"OpenGL 3.2 Core support is required!\n%s",
			driverInfo
		);
		return;
	}

	/* Some stuff is okay for ES3, not for desktop. */
	if (renderer->useES3)
	{
		if (!renderer->supports_3DTexture)
		{
			FNA3D_LogWarn(
				"3D textures unsupported, beware..."
			);
		}
		if (!renderer->supports_ARB_occlusion_query)
		{
			FNA3D_LogWarn(
				"Occlusion queries unsupported, beware..."
			);
		}
		if (!renderer->supports_ARB_invalidate_subdata)
		{
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
			renderer->glInvalidateFramebuffer =
				(glfntype_glInvalidateFramebuffer) SDL_GL_GetProcAddress(
					"glDiscardFramebufferEXT"
			);
#pragma GCC diagnostic pop
			renderer->supports_ARB_invalidate_subdata =
				renderer->glInvalidateFramebuffer != NULL;
		}
	}
	else
	{
		if (	!renderer->supports_3DTexture ||
			!renderer->supports_ARB_occlusion_query ||
			!renderer->supports_NonES3	)
		{
			FNA3D_LogError(
				"%s\n%s",
				baseErrorString,
				driverInfo
			);
			return;
		}
	}

	/* AKA: The shitty TexEnvi check */
	if (	!renderer->useES3 &&
		!renderer->useCoreProfile &&
		!renderer->supports_NonES3NonCore	)
	{
		FNA3D_LogError(
			"%s\n%s",
			baseErrorString,
			driverInfo
		);
		return;
	}

	/* ColorMask is an absolute mess */
	if (!renderer->supports_EXT_draw_buffers2)
	{
		#define LOAD_COLORMASK(suffix) \
		renderer->glColorMaski = (glfntype_glColorMaski) \
			SDL_GL_GetProcAddress("glColorMask" #suffix);
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
		LOAD_COLORMASK(IndexedEXT)
		if (renderer->glColorMaski == NULL) LOAD_COLORMASK(iOES)
		if (renderer->glColorMaski == NULL) LOAD_COLORMASK(iEXT)
#pragma GCC diagnostic pop
		if (renderer->glColorMaski != NULL)
		{
			renderer->supports_EXT_draw_buffers2 = 1;
		}
		#undef LOAD_COLORMASK
	}

	/* Possibly bogus if a game never uses render targets? */
	if (!renderer->supports_ARB_framebuffer_object)
	{
		FNA3D_LogError(
			"OpenGL framebuffer support is required!\n%s",
			driverInfo
		);
		return;
	}

	/* Check this before KHR_debug inits, to prevent complaints about
	 * unsupported render formats from showing up
	 */
	if (renderer->supports_ARB_internalformat_query)
	{
		for (i = 0; i < 21; i += 1)
		{
			renderer->glGetInternalformativ(
				GL_RENDERBUFFER,
				XNAToGL_TextureInternalFormat[i],
				GL_SAMPLES,
				1,
				&renderer->maxMultiSampleCountFormat[i]
			);
		}
	}

	/* MAP_UNSYNCHRONIZED _should_ be faster, but on threaded drivers it can stall hard */
	if (!SDL_GetHintBoolean("FNA3D_OPENGL_ALLOW_MAP_UNSYNCHRONIZED", 0))
	{
		renderer->supports_ARB_map_buffer_range = 0;
	}

	/* Everything below this check is for debug contexts */
	if (!debugMode)
	{
		return;
	}

	if (renderer->supports_KHR_debug)
	{
		renderer->glDebugMessageControl(
			GL_DONT_CARE,
			GL_DONT_CARE,
			GL_DONT_CARE,
			0,
			NULL,
			GL_TRUE
		);
		renderer->glDebugMessageControl(
			GL_DONT_CARE,
			GL_DEBUG_TYPE_OTHER,
			GL_DEBUG_SEVERITY_LOW,
			0,
			NULL,
			GL_FALSE
		);
		renderer->glDebugMessageControl(
			GL_DONT_CARE,
			GL_DEBUG_TYPE_OTHER,
			GL_DEBUG_SEVERITY_NOTIFICATION,
			0,
			NULL,
			GL_FALSE
		);
		renderer->glDebugMessageCallback(DebugCall, NULL);
	}
	else
	{
		FNA3D_LogWarn(
			"ARB_debug_output/KHR_debug not supported!"
		);
	}

	if (!renderer->supports_GREMEDY_string_marker)
	{
		FNA3D_LogWarn(
			"GREMEDY_string_marker not supported!"
		);
	}
}

static void* MOJOSHADERCALL GLGetProcAddress(const char *ep, void* d)
{
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
	return SDL_GL_GetProcAddress(ep);
#pragma GCC diagnostic pop
}

static inline void CheckExtensions(
	const char *ext,
	uint8_t *supportsS3tc,
	uint8_t *supportsDxt1,
	uint8_t *supportsAnisotropicFiltering,
	uint8_t *supportsSRGBRenderTargets,
	uint8_t *supportsBC7
) {
	uint8_t s3tc = (
		SDL_strstr(ext, "GL_EXT_texture_compression_s3tc") ||
		SDL_strstr(ext, "GL_OES_texture_compression_S3TC") ||
		SDL_strstr(ext, "GL_EXT_texture_compression_dxt3") ||
		SDL_strstr(ext, "GL_EXT_texture_compression_dxt5")
	);
	uint8_t bc7 = (
		SDL_strstr(ext, "GL_ARB_texture_compression_bptc") != NULL
	);
	uint8_t anisotropicFiltering = (
		SDL_strstr(ext, "GL_EXT_texture_filter_anisotropic") ||
		SDL_strstr(ext, "GL_ARB_texture_filter_anisotropic")
	);
	uint8_t srgbFrameBuffer = (
		SDL_strstr(ext, "GL_EXT_framebuffer_sRGB") != NULL
	);

	if (s3tc)
	{
		*supportsS3tc = 1;
	}
	if (s3tc || SDL_strstr(ext, "GL_EXT_texture_compression_dxt1"))
	{
		*supportsDxt1 = 1;
	}
	if (anisotropicFiltering)
	{
		*supportsAnisotropicFiltering = 1;
	}
	if (srgbFrameBuffer)
	{
		*supportsSRGBRenderTargets = 1;
	}
	if (bc7)
	{
		*supportsBC7 = 1;
	}
}

/* Driver */

static uint8_t OPENGL_PrepareWindowAttributes(uint32_t *flags)
{
	uint8_t forceES3, forceCore, forceCompat;
	const char *osVersion;
	int32_t depthSize, stencilSize;
	const char *depthFormatHint;

	/* GLContext environment variables */
	forceES3 = SDL_GetHintBoolean("FNA3D_OPENGL_FORCE_ES3", 0);
	forceCore = SDL_GetHintBoolean("FNA3D_OPENGL_FORCE_CORE_PROFILE", 0);
	forceCompat = SDL_GetHintBoolean("FNA3D_OPENGL_FORCE_COMPATIBILITY_PROFILE", 0);

	/* Some platforms are GLES only */
	osVersion = SDL_GetPlatform();
	forceES3 |= (
		(SDL_strcmp(osVersion, "iOS") == 0) ||
		(SDL_strcmp(osVersion, "tvOS") == 0) ||
		(SDL_strcmp(osVersion, "Stadia") == 0) ||
		(SDL_strcmp(osVersion, "Android") == 0) ||
		(SDL_strcmp(osVersion, "Emscripten") == 0)
	);

	/* Window depth format */
	depthSize = 24;
	stencilSize = 8;
	depthFormatHint = SDL_GetHint("FNA3D_OPENGL_WINDOW_DEPTHSTENCILFORMAT");
	if (depthFormatHint != NULL)
	{
		if (SDL_strcasecmp(depthFormatHint, "None") == 0)
		{
			depthSize = 0;
			stencilSize = 0;
		}
		else if (SDL_strcasecmp(depthFormatHint, "Depth16") == 0)
		{
			depthSize = 16;
			stencilSize = 0;
		}
		else if (SDL_strcasecmp(depthFormatHint, "Depth24") == 0)
		{
			depthSize = 24;
			stencilSize = 0;
		}
		else if (SDL_strcasecmp(depthFormatHint, "Depth24Stencil8") == 0)
		{
			depthSize = 24;
			stencilSize = 8;
		}
	}

	/* Set context attributes */
	SDL_GL_SetAttribute(SDL_GL_RED_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_GREEN_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_BLUE_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_ALPHA_SIZE, 8);
	SDL_GL_SetAttribute(SDL_GL_DEPTH_SIZE, depthSize);
	SDL_GL_SetAttribute(SDL_GL_STENCIL_SIZE, stencilSize);
	SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);
	if (forceES3)
	{
		SDL_GL_SetAttribute(SDL_GL_RETAINED_BACKING, 0);
		SDL_GL_SetAttribute(SDL_GL_ACCELERATED_VISUAL, 1);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 3);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 0);
		SDL_GL_SetAttribute(
			SDL_GL_CONTEXT_PROFILE_MASK,
			SDL_GL_CONTEXT_PROFILE_ES
		);
	}
	else if (forceCore)
	{
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 4);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 6);
		SDL_GL_SetAttribute(
			SDL_GL_CONTEXT_PROFILE_MASK,
			SDL_GL_CONTEXT_PROFILE_CORE
		);
	}
	else if (forceCompat)
	{
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 2);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 1);
		SDL_GL_SetAttribute(
			SDL_GL_CONTEXT_PROFILE_MASK,
			SDL_GL_CONTEXT_PROFILE_COMPATIBILITY
		);
	}

	/* If there's no GL library, bail!
	 * Only do this after all the flags above are set, as they may affect
	 * which GL library actually gets loaded (desktop vs ES, for example).
	 * -flibit
	 */
#ifdef USE_SDL3
	if (!SDL_GL_LoadLibrary(NULL))
#else
	if (SDL_GL_LoadLibrary(NULL) < 0)
#endif
	{
		return 0;
	}

	*flags = SDL_WINDOW_OPENGL;
	return 1;
}

FNA3D_Device* OPENGL_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
) {
	int32_t flags;
	int32_t depthSize, stencilSize;
	const char *rendererStr, *versionStr, *vendorStr;
	char driverInfo[256];
	int32_t i;
	int32_t numExtensions, numSamplers, numAttributes, numAttachments;
	OpenGLRenderer *renderer;
	FNA3D_Device *result;
#ifdef SDL_VIDEO_DRIVER_UIKIT
	SDL_SysWMinfo wmInfo;
#endif /* SDL_VIDEO_DRIVER_UIKIT */

	/* Create the FNA3D_Device */
	result = (FNA3D_Device*) SDL_malloc(sizeof(FNA3D_Device));
	ASSIGN_DRIVER(OPENGL)

	/* Init the OpenGLRenderer */
	renderer = (OpenGLRenderer*) SDL_malloc(sizeof(OpenGLRenderer));
	SDL_memset(renderer, '\0', sizeof(OpenGLRenderer));

	/* The FNA3D_Device and OpenGLRenderer need to reference each other */
	renderer->parentDevice = result;
	result->driverData = (FNA3D_Renderer*) renderer;

	/* Debug context support */
	if (debugMode && SDL_strcmp("Emscripten", SDL_GetPlatform()) != 0)
	{
		SDL_GL_SetAttribute(
			SDL_GL_CONTEXT_FLAGS,
			SDL_GL_CONTEXT_DEBUG_FLAG
		);
	}

	/* Create OpenGL context */
	renderer->context = SDL_GL_CreateContext(
		(SDL_Window*) presentationParameters->deviceWindowHandle
	);

	/* Check for a possible ES/Core context */
	SDL_GL_GetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, &flags);
	renderer->useES3 = (flags & SDL_GL_CONTEXT_PROFILE_ES) != 0;
	renderer->useCoreProfile = (flags & SDL_GL_CONTEXT_PROFILE_CORE) != 0;

	/* Check for EGL-based contexts */
	renderer->isEGL = (	renderer->useES3 ||
				SDL_strcmp(SDL_GetCurrentVideoDriver(), "wayland") == 0	);

	/* Check for a possible debug context */
	SDL_GL_GetAttribute(SDL_GL_CONTEXT_FLAGS, &flags);
	debugMode = (flags & SDL_GL_CONTEXT_DEBUG_FLAG) != 0;

	/* Check the window's depth/stencil format */
	SDL_GL_GetAttribute(SDL_GL_DEPTH_SIZE, &depthSize);
	SDL_GL_GetAttribute(SDL_GL_STENCIL_SIZE, &stencilSize);
	if (depthSize == 0 && stencilSize == 0)
	{
		renderer->windowDepthFormat = FNA3D_DEPTHFORMAT_NONE;
	}
	else if (depthSize == 16 && stencilSize == 0)
	{
		renderer->windowDepthFormat = FNA3D_DEPTHFORMAT_D16;
	}
	else if (depthSize == 24 && stencilSize == 0)
	{
		renderer->windowDepthFormat = FNA3D_DEPTHFORMAT_D24;
	}
	else if (depthSize == 24 && stencilSize == 8)
	{
		renderer->windowDepthFormat = FNA3D_DEPTHFORMAT_D24S8;
	}
	else if (depthSize == 32 && stencilSize == 8)
	{
		/* There's like a 99% chance this is GDI, expect a
		 * NoSuitableGraphicsDevice soon after this line...
		 */
		FNA3D_LogWarn("Non-standard D32S8 window depth format!");
		renderer->windowDepthFormat = FNA3D_DEPTHFORMAT_D24S8;
	}
	else
	{
		FNA3D_LogError(
			"Unrecognized window depth/stencil format: %d %d",
			depthSize,
			stencilSize
		);
		renderer->windowDepthFormat = FNA3D_DEPTHFORMAT_D24S8;
	}

	/* Lastly, check for backbuffer multisampling. This is extremely rare,
	 * but Xwayland may try to introduce it (maybe because of DPI scaling?)
	 */
	SDL_GL_GetAttribute(SDL_GL_MULTISAMPLESAMPLES, &renderer->windowSampleCount);
	if (renderer->windowSampleCount > 1)
	{
		FNA3D_LogWarn("Window surface is multisampled! This is an OS bug!");
	}

	/* Set the swap interval now that we know enough about the GL context */
	OPENGL_INTERNAL_SetPresentationInterval(
		presentationParameters->presentationInterval,
		renderer->isEGL
	);

	/* UIKit needs special treatment for backbuffer behavior */
#ifdef SDL_VIDEO_DRIVER_UIKIT
	SDL_VERSION(&wmInfo.version);
	SDL_GetWindowWMInfo(
		(SDL_Window*) presentationParameters->deviceWindowHandle,
		&wmInfo
	);
	if (wmInfo.subsystem == SDL_SYSWM_UIKIT)
	{
		renderer->realBackbufferFBO = wmInfo.info.uikit.framebuffer;
		renderer->realBackbufferRBO = wmInfo.info.uikit.colorbuffer;
	}
#endif /* SDL_VIDEO_DRIVER_UIKIT */

	/* Print GL information */
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
	renderer->glGetString = (glfntype_glGetString) SDL_GL_GetProcAddress("glGetString");
#pragma GCC diagnostic pop
	if (!renderer->glGetString)
	{
		FNA3D_LogError("GRAPHICS DRIVER IS EXTREMELY BROKEN!");
		SDL_assert(0 && "GRAPHICS DRIVER IS EXTREMELY BROKEN!");
	}
	rendererStr =	(const char*) renderer->glGetString(GL_RENDERER);
	versionStr =	(const char*) renderer->glGetString(GL_VERSION);
	vendorStr =	(const char*) renderer->glGetString(GL_VENDOR);

	FNA3D_LogInfo("FNA3D Driver: OpenGL");
	FNA3D_LogInfo("OpenGL Renderer: %s", rendererStr);
	FNA3D_LogInfo("OpenGL Driver: %s", versionStr);
	FNA3D_LogInfo("OpenGL Vendor: %s", vendorStr);

	/* Initialize entry points */
	SDL_snprintf(
		driverInfo, sizeof(driverInfo),
		"OpenGL Renderer: %s\nOpenGL Driver: %s\nOpenGL Vendor: %s",
		rendererStr, versionStr, vendorStr
	);
	LoadEntryPoints(renderer, driverInfo, debugMode);

	/* Initialize shader context */
	renderer->shaderProfile = SDL_GetHint("FNA3D_MOJOSHADER_PROFILE");
	if (renderer->shaderProfile == NULL || renderer->shaderProfile[0] == '\0')
	{
		renderer->shaderProfile = MOJOSHADER_glBestProfile(
			GLGetProcAddress,
			NULL,
			NULL,
			NULL,
			NULL
		);

		/* SPIR-V is very new and not really necessary. */
		if (	(SDL_strcasecmp(renderer->shaderProfile, "glspirv") == 0) &&
			!renderer->useCoreProfile	)
		{
			renderer->shaderProfile = "glsl120";
		}
	}
	renderer->shaderContext = MOJOSHADER_glCreateContext(
		renderer->shaderProfile,
		GLGetProcAddress,
		NULL,
		NULL,
		NULL,
		renderer
	);
	MOJOSHADER_glMakeContextCurrent(renderer->shaderContext);
	FNA3D_LogInfo("MojoShader Profile: %s", renderer->shaderProfile);

	/* Some users might want pixely upscaling... */
	renderer->backbufferScaleMode = SDL_GetHintBoolean(
		"FNA3D_BACKBUFFER_SCALE_NEAREST", 0
	) ? GL_NEAREST : GL_LINEAR;

	/* Load the extension list, initialize extension-dependent components */
	renderer->supports_s3tc = 0;
	renderer->supports_dxt1 = 0;
	renderer->supports_anisotropic_filtering = 0;
	renderer->supports_srgb_rendertarget = 0;
	renderer->supports_bc7 = 0;
	if (renderer->useCoreProfile)
	{
		renderer->glGetIntegerv(GL_NUM_EXTENSIONS, &numExtensions);
		for (i = 0; i < numExtensions; i += 1)
		{
			CheckExtensions(
				(const char*) renderer->glGetStringi(GL_EXTENSIONS, i),
				&renderer->supports_s3tc,
				&renderer->supports_dxt1,
				&renderer->supports_anisotropic_filtering,
				&renderer->supports_srgb_rendertarget,
				&renderer->supports_bc7
			);

			if (renderer->supports_s3tc && renderer->supports_dxt1 && renderer->supports_srgb_rendertarget && renderer->supports_bc7)
			{
				/* No need to look further. */
				break;
			}
		}
	}
	else
	{
		CheckExtensions(
			(const char*) renderer->glGetString(GL_EXTENSIONS),
			&renderer->supports_s3tc,
			&renderer->supports_dxt1,
			&renderer->supports_anisotropic_filtering,
			&renderer->supports_srgb_rendertarget,
			&renderer->supports_bc7
		);
	}

	/* Check the max multisample count, override parameters if necessary */
	if (renderer->supports_EXT_framebuffer_multisample)
	{
		renderer->glGetIntegerv(
			GL_MAX_SAMPLES,
			&renderer->maxMultiSampleCount
		);
	}
	if (renderer->supports_ARB_internalformat_query)
	{
		presentationParameters->multiSampleCount = SDL_min(
			presentationParameters->multiSampleCount,
			renderer->maxMultiSampleCountFormat[presentationParameters->backBufferFormat]
		);
	}
	else
	{
		/* This number isn't very good, but it's all we have... */
		presentationParameters->multiSampleCount = SDL_min(
			presentationParameters->multiSampleCount,
			renderer->maxMultiSampleCount
		);
	}

	/* Initialize the faux backbuffer */
	OPENGL_INTERNAL_CreateBackbuffer(renderer, presentationParameters);

	/* Initialize texture collection array */
	renderer->glGetIntegerv(GL_MAX_TEXTURE_IMAGE_UNITS, &numSamplers);
	numSamplers = SDL_min(
		numSamplers,
		MAX_TEXTURE_SAMPLERS + MAX_VERTEXTEXTURE_SAMPLERS
	);
	renderer->numTextureSlots = SDL_min(
		numSamplers,
		MAX_TEXTURE_SAMPLERS
	);
	renderer->numVertexTextureSlots = SDL_min(
		SDL_max(numSamplers - MAX_TEXTURE_SAMPLERS, 0),
		MAX_VERTEXTEXTURE_SAMPLERS
	);
	renderer->vertexSamplerStart = numSamplers - renderer->numVertexTextureSlots;
	for (i = 0; i < numSamplers; i += 1)
	{
		renderer->textures[i] = &NullTexture;
	}

	/* Initialize vertex attribute state arrays */
	renderer->ldBaseVertex = -1;
	renderer->glGetIntegerv(GL_MAX_VERTEX_ATTRIBS, &numAttributes);
	numAttributes = SDL_min(numAttributes, MAX_VERTEX_ATTRIBUTES);
	for (i = 0; i < numAttributes; i += 1)
	{
		renderer->attributes[i].currentBuffer = 0;
		renderer->attributes[i].currentPointer = NULL;
		renderer->attributes[i].currentFormat = FNA3D_VERTEXELEMENTFORMAT_SINGLE;
		renderer->attributes[i].currentNormalized = 0;
		renderer->attributes[i].currentStride = 0;

		renderer->attributeEnabled[i] = 0;
		renderer->previousAttributeEnabled[i] = 0;
		renderer->attributeDivisor[i] = 0;
		renderer->previousAttributeDivisor[i] = 0;
	}
	renderer->numVertexAttributes = numAttributes;

	/* Initialize render target FBO and state arrays */
	renderer->glGetIntegerv(GL_MAX_DRAW_BUFFERS, &numAttachments);
	numAttachments = SDL_min(numAttachments, MAX_RENDERTARGET_BINDINGS);
	for (i = 0; i < numAttachments; i += 1)
	{
		renderer->attachments[i] = 0;
		renderer->attachmentTypes[i] = 0;
		renderer->currentAttachments[i] = 0;
		renderer->currentAttachmentTypes[i] = GL_TEXTURE_2D;
		renderer->drawBuffersArray[i] = GL_COLOR_ATTACHMENT0 + i;
	}
	renderer->numAttachments = numAttachments;

	renderer->drawBuffersArray[numAttachments] = GL_DEPTH_ATTACHMENT;
	renderer->drawBuffersArray[numAttachments + 1] = GL_STENCIL_ATTACHMENT;
	renderer->glGenFramebuffers(1, &renderer->targetFramebuffer);
	renderer->glGenFramebuffers(1, &renderer->resolveFramebufferRead);
	renderer->glGenFramebuffers(1, &renderer->resolveFramebufferDraw);

	if (renderer->useCoreProfile)
	{
		/* Generate and bind a VAO, to shut Core up */
		renderer->glGenVertexArrays(1, &renderer->vao);
		renderer->glBindVertexArray(renderer->vao);
	}
	else if (!renderer->useES3)
	{
		/* Compatibility contexts require that point sprites be enabled
		 * explicitly. However, drivers (and the Steam overlay) are
		 * really fucking bad at not knowing that point sprite state
		 * should only affect point rendering. So, here we are.
		 * -flibit
		 */
		const char *os = SDL_GetPlatform();
		if (	(SDL_strcmp(os, "Mac OS X") == 0) || /* Mainly Intel */
			(SDL_strcmp(os, "Linux") == 0)	) /* Mainly Gallium */
		{
			renderer->togglePointSprite = 1;
		}
		else
		{
			renderer->glEnable(GL_POINT_SPRITE);
			renderer->glTexEnvi(GL_POINT_SPRITE, GL_COORD_REPLACE, GL_TRUE);
		}
	}

	/* Initialize renderer members not covered by SDL_memset('\0') */
	renderer->dstBlend = FNA3D_BLEND_ZERO; /* ZERO is really 1. -caleb */
	renderer->dstBlendAlpha = FNA3D_BLEND_ZERO; /* ZERO is really 1. -caleb */
	renderer->colorWriteEnable = FNA3D_COLORWRITECHANNELS_ALL;
	renderer->colorWriteEnable1 = FNA3D_COLORWRITECHANNELS_ALL;
	renderer->colorWriteEnable2 = FNA3D_COLORWRITECHANNELS_ALL;
	renderer->colorWriteEnable3 = FNA3D_COLORWRITECHANNELS_ALL;
	renderer->multiSampleMask = -1; /* AKA 0xFFFFFFFF, ugh -flibit */
	renderer->stencilWriteMask = -1; /* AKA 0xFFFFFFFF, ugh -flibit */
	renderer->stencilMask = -1; /* AKA 0xFFFFFFFF, ugh -flibit */
	renderer->multiSampleEnable = 1;
	renderer->depthRangeMax = 1.0f;
	renderer->currentClearDepth = 1.0f;

	/* The creation thread will be the "main" thread */
	renderer->threadID = SDL_GetCurrentThreadID();
	renderer->commandsLock = SDL_CreateMutex();
	renderer->disposeTexturesLock = SDL_CreateMutex();
	renderer->disposeRenderbuffersLock = SDL_CreateMutex();
	renderer->disposeVertexBuffersLock = SDL_CreateMutex();
	renderer->disposeIndexBuffersLock = SDL_CreateMutex();
	renderer->disposeEffectsLock = SDL_CreateMutex();
	renderer->disposeQueriesLock = SDL_CreateMutex();

	/* Return the FNA3D_Device */
	return result;
}

FNA3D_Driver OpenGLDriver = {
	"OpenGL",
	OPENGL_PrepareWindowAttributes,
	OPENGL_CreateDevice
};

#else

extern int this_tu_is_empty;

#endif /* FNA3D_DRIVER_OPENGL */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
