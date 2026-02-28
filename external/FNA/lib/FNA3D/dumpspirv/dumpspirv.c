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

#include <SDL3/SDL.h>
#define __MOJOSHADER_INTERNAL__ 1
#include <mojoshader_internal.h>
#include <FNA3D.h>

static uint8_t compileFromFXB(const char *filename, const char *folder, SDL_IOStream *ops);
static uint8_t compileFromTrace(const char *filename, const char *folder, SDL_IOStream *ops);

int main(int argc, char** argv)
{
	int arg;
	char *folder;
	unsigned char buf[4];
	SDL_IOStream *ops;

	if (argc <= 1)
	{
		SDL_asprintf(&folder, "%sFNA3D_Trace.bin", SDL_GetBasePath());
		ops = SDL_IOFromFile(folder, "rb");
		if (ops == NULL)
		{
			SDL_Log("FNA3D_Trace.bin not found");
			return 1;
		}
		compileFromTrace(folder, SDL_GetPrefPath("FNA3D", "DumpSPIRV"), ops);
		SDL_CloseIO(ops);
		return 0;
	}

	for (arg = 1; arg < argc; arg += 1)
	{
		ops = SDL_IOFromFile(argv[arg], "rb");
		if (ops == NULL)
		{
			SDL_Log("%s not found, ignoring", argv[arg]);
			continue;
		}
		if (SDL_ReadIO(ops, buf, sizeof(buf)) < sizeof(buf))
		{
			SDL_Log("%s is too small, ignoring", argv[arg]);
			SDL_CloseIO(ops);
			continue;
		}
		SDL_SeekIO(ops, 0, SDL_IO_SEEK_SET);

		SDL_asprintf(&folder, "%s.spirv", argv[arg]);
		SDL_CreateDirectory(folder);

		if (	((buf[0] == 0x01) && (buf[1] == 0x09) && (buf[2] == 0xFF) && (buf[3] == 0xFE)) ||
			((buf[0] == 0xCF) && (buf[1] == 0x0B) && (buf[2] == 0xF0) && (buf[3] == 0xBC))	)
		{
			compileFromFXB(argv[arg], folder, ops);
		}
		else
		{
			compileFromTrace(argv[arg], folder, ops);
		}

		SDL_free(folder);
		SDL_CloseIO(ops);
	}

	return 0;
}

/*
 * MojoShader Effects Implementation
 */

#define MAX_REG_FILE_F 8192
#define MAX_REG_FILE_I 2047
#define MAX_REG_FILE_B 2047
static float reg_file_f[MAX_REG_FILE_F * 4];
static int32_t reg_file_i[MAX_REG_FILE_I * 4];
static uint8_t reg_file_b[MAX_REG_FILE_B * 4];

typedef struct TraceShader
{
	MOJOSHADER_parseData *pd;
	uint64_t refcount;
} TraceShader;

typedef struct TraceContext
{
	TraceShader *vertex;
	TraceShader *fragment;
} TraceContext;

static void* MOJOSHADERCALL compileShader(
	const void *ctx,
	const char *mainfn,
	const unsigned char *tokenbuf,
	const unsigned int bufsize,
	const MOJOSHADER_swizzle *swiz,
	const unsigned int swizcount,
	const MOJOSHADER_samplerMap *smap,
	const unsigned int smapcount
) {
	TraceShader *result;
	MOJOSHADER_parseData *pd;

	pd = (MOJOSHADER_parseData*) MOJOSHADER_parse(
		"spirv",
		mainfn,
		tokenbuf,
		bufsize,
		swiz,
		swizcount,
		smap,
		smapcount,
		NULL,
		NULL,
		NULL
	);
	SDL_assert(pd != NULL);

	result = (TraceShader*) SDL_malloc(sizeof(TraceShader));
	SDL_assert(result != NULL);

	result->pd = pd;
	result->refcount = 1;

	return result;
}

static void MOJOSHADERCALL addRef(void* shader)
{
	TraceShader *ts = (TraceShader*) shader;
	ts->refcount += 1;
}

static void MOJOSHADERCALL deleteShader(const void *ctx, void *shader)
{
	TraceShader *ts = (TraceShader*) shader;
	ts->refcount -= 1;
	if (ts->refcount == 0)
	{
		MOJOSHADER_freeParseData(ts->pd);
		SDL_free(ts);
	}
}

static MOJOSHADER_parseData* MOJOSHADERCALL getParseData(void *shader)
{
	TraceShader *ts = (TraceShader*) shader;
	return ts->pd;
}

static void MOJOSHADERCALL bindShaders(
    const void *ctx,
    void *vshader,
    void *pshader
) {
	TraceContext *tc = (TraceContext*) ctx;
	tc->vertex = (TraceShader*) vshader;
	tc->fragment = (TraceShader*) pshader;
}

static void MOJOSHADERCALL getBoundShaders(
    const void *ctx,
    void **vshader,
    void **pshader
) {
	TraceContext *tc = (TraceContext*) ctx;
	*vshader = tc->vertex;
	*pshader = tc->fragment;
}

static void MOJOSHADERCALL mapUniformBufferMemory(
    const void *ctx,
    float **vsf, int **vsi, unsigned char **vsb,
    float **psf, int **psi, unsigned char **psb
) {
	*vsf = reg_file_f;
	*vsi = reg_file_i;
	*vsb = reg_file_b;
	*psf = reg_file_f;
	*psi = reg_file_i;
	*psb = reg_file_b;
}

static void MOJOSHADERCALL unmapUniformBufferMemory(
    const void *ctx
) {
	/* Don't need to care about uniforms for compilation */
}

static const char* MOJOSHADERCALL getError(const void *ctx)
{
	return "";
}

/*
 * FXB Compiler
 */

static uint8_t compileFromFXB(const char *filename, const char *folder, SDL_IOStream *ops)
{
	MOJOSHADER_effect *effect;
	TraceShader *shader;
	SDL_PathInfo shaderPathInfo;
	SDL_IOStream *shaderFile;
	uint32_t shaderCrc;
	char *shaderPath;
	int64_t size;
	void *fxb;
	int i;

	TraceContext traceCtx;
	const MOJOSHADER_effectShaderContext ctx =
	{
		compileShader,
		addRef,
		deleteShader,
		getParseData,
		bindShaders,
		getBoundShaders,
		mapUniformBufferMemory,
		unmapUniformBufferMemory,
		getError,
		&traceCtx, /* shaderContext */
		NULL, /* m */
		NULL, /* f */
		NULL /* malloc_data */
	};

	size = SDL_GetIOSize(ops);
	fxb = SDL_malloc(size);
	if (fxb == NULL)
	{
		return 0;
	}
	SDL_ReadIO(ops, fxb, size);

	effect = MOJOSHADER_compileEffect(
		(const unsigned char*) fxb,
		size,
		NULL,
		0,
		NULL,
		0,
		&ctx
	);
	SDL_free(fxb);
	if (effect == NULL)
	{
		SDL_Log("Compiling %s failed", filename);
		return 0;
	}

	for (i = 0; i < effect->object_count; i += 1)
	{
		if (	(effect->objects[i].type != MOJOSHADER_SYMTYPE_VERTEXSHADER) &&
			(effect->objects[i].type != MOJOSHADER_SYMTYPE_PIXELSHADER)	)
		{
			continue;
		}
		if (effect->objects[i].shader.is_preshader)
		{
			continue;
		}

		shader = (TraceShader*) effect->objects[i].shader.shader;

		shaderCrc = SDL_crc32(
			0,
			shader->pd->output,
			shader->pd->output_len - sizeof(SpirvPatchTable)
		);
		SDL_asprintf(&shaderPath, "%s/%x.spv", folder, shaderCrc);
		SDL_GetPathInfo(shaderPath, &shaderPathInfo);
		if (shaderPathInfo.type == SDL_PATHTYPE_NONE)
		{
			SDL_Log("New shader, crc %x\n", shaderCrc);
			shaderFile = SDL_IOFromFile(shaderPath, "wb");
			SDL_WriteIO(
				shaderFile,
				shader->pd->output,
				shader->pd->output_len - sizeof(SpirvPatchTable)
			);
			SDL_CloseIO(shaderFile);
		}
		SDL_free(shaderPath);
	}

	MOJOSHADER_deleteEffect(effect);
}

/*
 * FNA3D Trace Compiler
 */

/* This is ripped from FNA3D_Driver.h */
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
			SDL_assert(!"Unrecognized VertexElementUsage!");
			return (MOJOSHADER_usage) 0;
	}
}

/* Everything below is a horrible ripoff of FNA3D_Replay!
 * See FNA3D/replay/replay.c for the original source.
 * -flibit
 */

#define MARK_CREATEDEVICE			0
#define MARK_DESTROYDEVICE			1
#define MARK_SWAPBUFFERS			2
#define MARK_CLEAR				3
#define MARK_DRAWINDEXEDPRIMITIVES		4
#define MARK_DRAWINSTANCEDPRIMITIVES		5
#define MARK_DRAWPRIMITIVES			6
#define MARK_SETVIEWPORT			7
#define MARK_SETSCISSORRECT			8
#define MARK_SETBLENDFACTOR			9
#define MARK_SETMULTISAMPLEMASK			10
#define MARK_SETREFERENCESTENCIL		11
#define MARK_SETBLENDSTATE			12
#define MARK_SETDEPTHSTENCILSTATE		13
#define MARK_APPLYRASTERIZERSTATE		14
#define MARK_VERIFYSAMPLER			15
#define MARK_VERIFYVERTEXSAMPLER		16
#define MARK_APPLYVERTEXBUFFERBINDINGS		17
#define MARK_SETRENDERTARGETS			18
#define MARK_RESOLVETARGET			19
#define MARK_RESETBACKBUFFER			20
#define MARK_READBACKBUFFER			21
#define MARK_CREATETEXTURE2D			22
#define MARK_CREATETEXTURE3D			23
#define MARK_CREATETEXTURECUBE			24
#define MARK_ADDDISPOSETEXTURE			25
#define MARK_SETTEXTUREDATA2D			26
#define MARK_SETTEXTUREDATA3D			27
#define MARK_SETTEXTUREDATACUBE			28
#define MARK_SETTEXTUREDATAYUV			29
#define MARK_GETTEXTUREDATA2D			30
#define MARK_GETTEXTUREDATA3D			31
#define MARK_GETTEXTUREDATACUBE			32
#define MARK_GENCOLORRENDERBUFFER		33
#define MARK_GENDEPTHSTENCILRENDERBUFFER	34
#define MARK_ADDDISPOSERENDERBUFFER		35
#define MARK_GENVERTEXBUFFER			36
#define MARK_ADDDISPOSEVERTEXBUFFER		37
#define MARK_SETVERTEXBUFFERDATA		38
#define MARK_GETVERTEXBUFFERDATA		39
#define MARK_GENINDEXBUFFER			40
#define MARK_ADDDISPOSEINDEXBUFFER		41
#define MARK_SETINDEXBUFFERDATA			42
#define MARK_GETINDEXBUFFERDATA			43
#define MARK_CREATEEFFECT			44
#define MARK_CLONEEFFECT			45
#define MARK_ADDDISPOSEEFFECT			46
#define MARK_SETEFFECTTECHNIQUE			47
#define MARK_APPLYEFFECT			48
#define MARK_BEGINPASSRESTORE			49
#define MARK_ENDPASSRESTORE			50
#define MARK_CREATEQUERY			51
#define MARK_ADDDISPOSEQUERY			52
#define MARK_QUERYBEGIN				53
#define MARK_QUERYEND				54
#define MARK_QUERYPIXELCOUNT			55
#define MARK_SETSTRINGMARKER			56

static uint8_t compileFromTrace(const char *filename, const char *folder, SDL_IOStream *ops)
{
	#define READ(val) SDL_ReadIO(ops, &val, sizeof(val))

	TraceContext traceCtx;
	const MOJOSHADER_effectShaderContext ctx =
	{
		compileShader,
		addRef,
		deleteShader,
		getParseData,
		bindShaders,
		getBoundShaders,
		mapUniformBufferMemory,
		unmapUniformBufferMemory,
		getError,
		&traceCtx, /* shaderContext */
		NULL, /* m */
		NULL, /* f */
		NULL /* malloc_data */
	};

	uint8_t mark, run;

	/* CreateDevice, ResetBackbuffer */
	FNA3D_Device *device;
	FNA3D_PresentationParameters presentationParameters;
	uint8_t debugMode;

	/* SwapBuffers */
	uint8_t hasSource, hasDestination;
	FNA3D_Rect sourceRectangle;
	FNA3D_Rect destinationRectangle;

	/* Clear */
	FNA3D_ClearOptions options;
	FNA3D_Vec4 color;
	float depth;
	int32_t stencil;

	/* Draw*Primitives */
	FNA3D_PrimitiveType primitiveType;
	int32_t baseVertex;
	int32_t minVertexIndex;
	int32_t numVertices;
	int32_t startIndex;
	int32_t primitiveCount;
	int32_t instanceCount;
	FNA3D_IndexElementSize indexElementSize;
	int32_t vertexStart;

	/* SetViewport */
	FNA3D_Viewport viewport;

	/* SetScissorRect */
	FNA3D_Rect scissor;

	/* SetBlendFactor */
	FNA3D_Color blendFactor;

	/* SetMultiSampleMask */
	int32_t mask;

	/* SetReferenceStencil */
	int32_t ref;

	/* SetBlendState */
	FNA3D_BlendState blendState;

	/* SetDepthStencilState */
	FNA3D_DepthStencilState depthStencilState;

	/* ApplyRasterizerState */
	FNA3D_RasterizerState rasterizerState;

	/* Verify*Sampler */
	int32_t index;
	FNA3D_SamplerState sampler;

	/* ApplyVertexBufferBindings */
	FNA3D_VertexBufferBinding *bindings;
	FNA3D_VertexBufferBinding *binding;
	FNA3D_VertexElement *elem;
	int32_t numBindings;
	uint8_t bindingsUpdated;
	int32_t vi, vj;

	/* SetRenderTargets */
	FNA3D_RenderTargetBinding *renderTargets;
	FNA3D_RenderTargetBinding *target;
	int32_t numRenderTargets;
	FNA3D_Renderbuffer *depthStencilBuffer;
	FNA3D_DepthFormat depthFormat;
	uint8_t preserveTargetContents;
	int32_t ri;

	/* ResolveTarget */
	FNA3D_RenderTargetBinding resolveTarget;

	/* Gen*Renderbuffer */
	int32_t multiSampleCount;

	/* *BufferData */
	int32_t offsetInBytes;
	int32_t elementCount;
	int32_t elementSizeInBytes;
	int32_t vertexStride;
	FNA3D_SetDataOptions dataOptions;

	/* SetEffectTechnique */
	int32_t technique;

	/* ApplyEffect */
	uint32_t pass;
	MOJOSHADER_effectStateChanges changes;

	/* Miscellaneous allocations, dimensions, blah blah... */
	int32_t x, y, z, w, h, d, level, levelCount, sizeInBytes, dataLength;
	FNA3D_CubeMapFace cubeMapFace;
	FNA3D_SurfaceFormat format;
	FNA3D_BufferUsage usage;
	uint8_t isRenderTarget, dynamic;
	uint8_t nonNull;
	void* miscBuffer;

	/* Objects */
	FNA3D_Texture *texture;
	FNA3D_Renderbuffer *renderbuffer;
	FNA3D_Buffer *buffer;
	FNA3D_Effect *effect;
	MOJOSHADER_effect *effectData;
	FNA3D_Query *query;

	/* Trace Objects */
	FNA3D_Texture **traceTexture = NULL;
	uint64_t traceTextureCount = 0;
	FNA3D_Renderbuffer **traceRenderbuffer = NULL;
	uint64_t traceRenderbufferCount = 0;
	FNA3D_Buffer **traceVertexBuffer = NULL;
	uint64_t traceVertexBufferCount = 0;
	FNA3D_Buffer **traceIndexBuffer = NULL;
	uint64_t traceIndexBufferCount = 0;
	FNA3D_Effect **traceEffect = NULL;
	MOJOSHADER_effect **traceEffectData = NULL;
	uint64_t traceEffectCount = 0;
	FNA3D_Query **traceQuery = NULL;
	uint64_t traceQueryCount = 0;
	uint64_t i, j, k;
	#define REGISTER_OBJECT(array, type, object) \
		for (i = 0; i < trace##array##Count; i += 1) \
		{ \
			if (trace##array[i] == NULL) \
			{ \
				trace##array[i] = object; \
				break; \
			} \
		} \
		if (i == trace##array##Count) \
		{ \
			trace##array##Count += 1; \
			trace##array = (FNA3D_##type**) SDL_realloc( \
				trace##array, \
				sizeof(FNA3D_##type*) * trace##array##Count \
			); \
			trace##array[i] = object; \
		}

	/* Compiler objects */
	int numElements;
	int curElement;
	MOJOSHADER_vertexAttribute *vtxDecl;
	int patchLen;
	Uint32 shaderCrc;
	char *shaderPath;
	SDL_IOStream *shaderFile;
	SDL_PathInfo shaderPathInfo;
	MOJOSHADER_effect *currentEffect = NULL;
	const MOJOSHADER_effectTechnique *currentTechnique = NULL;
	uint32_t currentPass = 0;
	uint32_t numPasses;
	MOJOSHADER_effectStateChanges stateChanges;

	/* Beginning of the file should be a CreateDevice call */
	READ(mark);
	if (mark != MARK_CREATEDEVICE)
	{
		SDL_Log("%s is a bad trace!", filename);
		return 0;
	}
	READ(presentationParameters.backBufferWidth);
	READ(presentationParameters.backBufferHeight);
	READ(presentationParameters.backBufferFormat);
	READ(presentationParameters.multiSampleCount);
	READ(presentationParameters.isFullScreen);
	READ(presentationParameters.depthStencilFormat);
	READ(presentationParameters.presentationInterval);
	READ(presentationParameters.displayOrientation);
	READ(presentationParameters.renderTargetUsage);
	READ(debugMode);

	/* Go through all the calls, let vsync do the timing if applicable */
	run = 1;
	READ(mark);
	while (run && mark != MARK_DESTROYDEVICE)
	{
		switch (mark)
		{
		case MARK_SWAPBUFFERS:
			READ(hasSource);
			if (hasSource)
			{
				READ(sourceRectangle.x);
				READ(sourceRectangle.y);
				READ(sourceRectangle.w);
				READ(sourceRectangle.h);
			}
			READ(hasDestination);
			if (hasDestination)
			{
				READ(destinationRectangle.x);
				READ(destinationRectangle.y);
				READ(destinationRectangle.w);
				READ(destinationRectangle.h);
			}
			break;
		case MARK_CLEAR:
			READ(options);
			READ(color.x);
			READ(color.y);
			READ(color.z);
			READ(color.w);
			READ(depth);
			READ(stencil);
			break;
		case MARK_DRAWINDEXEDPRIMITIVES:
			READ(primitiveType);
			READ(baseVertex);
			READ(minVertexIndex);
			READ(numVertices);
			READ(startIndex);
			READ(primitiveCount);
			READ(i);
			READ(indexElementSize);
			break;
		case MARK_DRAWINSTANCEDPRIMITIVES:
			READ(primitiveType);
			READ(baseVertex);
			READ(minVertexIndex);
			READ(numVertices);
			READ(startIndex);
			READ(primitiveCount);
			READ(instanceCount);
			READ(i);
			READ(indexElementSize);
			break;
		case MARK_DRAWPRIMITIVES:
			READ(primitiveType);
			READ(vertexStart);
			READ(primitiveCount);
			break;
		case MARK_SETVIEWPORT:
			READ(viewport.x);
			READ(viewport.y);
			READ(viewport.w);
			READ(viewport.h);
			READ(viewport.minDepth);
			READ(viewport.maxDepth);
			break;
		case MARK_SETSCISSORRECT:
			READ(scissor.x);
			READ(scissor.y);
			READ(scissor.w);
			READ(scissor.h);
			break;
		case MARK_SETBLENDFACTOR:
			READ(blendFactor.r);
			READ(blendFactor.g);
			READ(blendFactor.b);
			READ(blendFactor.a);
			break;
		case MARK_SETMULTISAMPLEMASK:
			READ(mask);
			break;
		case MARK_SETREFERENCESTENCIL:
			READ(ref);
			break;
		case MARK_SETBLENDSTATE:
			READ(blendState.colorSourceBlend);
			READ(blendState.colorDestinationBlend);
			READ(blendState.colorBlendFunction);
			READ(blendState.alphaSourceBlend);
			READ(blendState.alphaDestinationBlend);
			READ(blendState.alphaBlendFunction);
			READ(blendState.colorWriteEnable);
			READ(blendState.colorWriteEnable1);
			READ(blendState.colorWriteEnable2);
			READ(blendState.colorWriteEnable3);
			READ(blendState.blendFactor.r);
			READ(blendState.blendFactor.g);
			READ(blendState.blendFactor.b);
			READ(blendState.blendFactor.a);
			READ(blendState.multiSampleMask);
			break;
		case MARK_SETDEPTHSTENCILSTATE:
			READ(depthStencilState.depthBufferEnable);
			READ(depthStencilState.depthBufferWriteEnable);
			READ(depthStencilState.depthBufferFunction);
			READ(depthStencilState.stencilEnable);
			READ(depthStencilState.stencilMask);
			READ(depthStencilState.stencilWriteMask);
			READ(depthStencilState.twoSidedStencilMode);
			READ(depthStencilState.stencilFail);
			READ(depthStencilState.stencilDepthBufferFail);
			READ(depthStencilState.stencilPass);
			READ(depthStencilState.stencilFunction);
			READ(depthStencilState.ccwStencilFail);
			READ(depthStencilState.ccwStencilDepthBufferFail);
			READ(depthStencilState.ccwStencilPass);
			READ(depthStencilState.ccwStencilFunction);
			READ(depthStencilState.referenceStencil);
			break;
		case MARK_APPLYRASTERIZERSTATE:
			READ(rasterizerState.fillMode);
			READ(rasterizerState.cullMode);
			READ(rasterizerState.depthBias);
			READ(rasterizerState.slopeScaleDepthBias);
			READ(rasterizerState.scissorTestEnable);
			READ(rasterizerState.multiSampleAntiAlias);
			break;
		case MARK_VERIFYSAMPLER:
			READ(index);
			READ(i);
			READ(sampler.filter);
			READ(sampler.addressU);
			READ(sampler.addressV);
			READ(sampler.addressW);
			READ(sampler.mipMapLevelOfDetailBias);
			READ(sampler.maxAnisotropy);
			READ(sampler.maxMipLevel);
			break;
		case MARK_VERIFYVERTEXSAMPLER:
			READ(index);
			READ(i);
			READ(sampler.filter);
			READ(sampler.addressU);
			READ(sampler.addressV);
			READ(sampler.addressW);
			READ(sampler.mipMapLevelOfDetailBias);
			READ(sampler.maxAnisotropy);
			READ(sampler.maxMipLevel);
			break;
		case MARK_APPLYVERTEXBUFFERBINDINGS:
			READ(numBindings);
			bindings = (FNA3D_VertexBufferBinding*) SDL_malloc(
				sizeof(FNA3D_VertexBufferBinding) *
				numBindings
			);
			numElements = 0;
			for (vi = 0; vi < numBindings; vi += 1)
			{
				binding = &bindings[vi];
				READ(i);
				binding->vertexBuffer = traceVertexBuffer[i];
				READ(binding->vertexDeclaration.vertexStride);
				READ(binding->vertexDeclaration.elementCount);
				binding->vertexDeclaration.elements = (FNA3D_VertexElement*) SDL_malloc(
					sizeof(FNA3D_VertexElement) *
					binding->vertexDeclaration.elementCount
				);
				numElements += binding->vertexDeclaration.elementCount;
				for (vj = 0; vj < binding->vertexDeclaration.elementCount; vj += 1)
				{
					elem = &binding->vertexDeclaration.elements[vj];
					READ(elem->offset);
					READ(elem->vertexElementFormat);
					READ(elem->vertexElementUsage);
					READ(elem->usageIndex);
				}
				READ(binding->vertexOffset);
				READ(binding->instanceFrequency);
			}
			READ(bindingsUpdated);
			READ(baseVertex);

			vtxDecl = (MOJOSHADER_vertexAttribute*) SDL_malloc(
				sizeof(MOJOSHADER_vertexAttribute) * numElements
			);
			curElement = 0;
			for (vi = 0; vi < numBindings; vi += 1)
			{
				binding = &bindings[vi];
				for (vj = 0; vj < binding->vertexDeclaration.elementCount; vj += 1)
				{
					elem = &binding->vertexDeclaration.elements[vj];

					vtxDecl[curElement].usage = VertexAttribUsage(elem->vertexElementUsage);
					vtxDecl[curElement].vertexElementFormat = elem->vertexElementFormat;
					vtxDecl[curElement].usageIndex = elem->usageIndex;
					curElement += 1;
				}
			}
			patchLen = MOJOSHADER_linkSPIRVShaders(
				traceCtx.vertex->pd,
				traceCtx.fragment->pd,
				vtxDecl,
				numElements
			);
			SDL_free(vtxDecl);

			shaderCrc = SDL_crc32(
				0,
				traceCtx.vertex->pd->output,
				traceCtx.vertex->pd->output_len - patchLen
			);
			SDL_asprintf(&shaderPath, "%s/%x.vert.spv", folder, shaderCrc);
			SDL_GetPathInfo(shaderPath, &shaderPathInfo);
			if (shaderPathInfo.type == SDL_PATHTYPE_NONE)
			{
				SDL_Log("New vertex shader, crc %x\n", shaderCrc);
				shaderFile = SDL_IOFromFile(shaderPath, "wb");
				SDL_WriteIO(
					shaderFile,
					traceCtx.vertex->pd->output,
					traceCtx.vertex->pd->output_len - patchLen
				);
				SDL_CloseIO(shaderFile);
			}
			SDL_free(shaderPath);

			shaderCrc = SDL_crc32(
				0,
				traceCtx.fragment->pd->output,
				traceCtx.fragment->pd->output_len - patchLen
			);
			SDL_asprintf(&shaderPath, "%s/%x.frag.spv", folder, shaderCrc);
			SDL_GetPathInfo(shaderPath, &shaderPathInfo);
			if (shaderPathInfo.type == SDL_PATHTYPE_NONE)
			{
				SDL_Log("New fragment shader, crc %x\n", shaderCrc);
				shaderFile = SDL_IOFromFile(shaderPath, "wb");
				SDL_WriteIO(
					shaderFile,
					traceCtx.fragment->pd->output,
					traceCtx.fragment->pd->output_len - patchLen
				);
				SDL_CloseIO(shaderFile);
			}
			SDL_free(shaderPath);

			for (vi = 0; vi < numBindings; vi += 1)
			{
				binding = &bindings[vi];
				SDL_free(binding->vertexDeclaration.elements);
			}
			SDL_free(bindings);
			break;
		case MARK_SETRENDERTARGETS:
			READ(numRenderTargets);
			if (numRenderTargets == 0)
			{
				renderTargets = NULL;
			}
			else
			{
				renderTargets = (FNA3D_RenderTargetBinding*) SDL_malloc(
					sizeof(FNA3D_RenderTargetBinding) *
					numRenderTargets
				);
				for (ri = 0; ri < numRenderTargets; ri += 1)
				{
					target = &renderTargets[ri];
					READ(target->type);
					if (target->type == FNA3D_RENDERTARGET_TYPE_2D)
					{
						READ(target->twod.width);
						READ(target->twod.height);
					}
					else
					{
						SDL_assert(target->type == FNA3D_RENDERTARGET_TYPE_CUBE);
						READ(target->cube.size);
						READ(target->cube.face);
					}

					READ(target->levelCount);
					READ(target->multiSampleCount);

					READ(nonNull);
					if (nonNull)
					{
						READ(i);
						target->texture = traceTexture[i];
					}
					else
					{
						target->texture = NULL;
					}

					READ(nonNull);
					if (nonNull)
					{
						READ(i);
						target->colorBuffer = traceRenderbuffer[i];
					}
					else
					{
						target->colorBuffer = NULL;
					}
				}
			}

			READ(nonNull);
			if (nonNull)
			{
				READ(i);
				depthStencilBuffer = traceRenderbuffer[i];
			}
			else
			{
				depthStencilBuffer = NULL;
			}

			READ(depthFormat);
			READ(preserveTargetContents);

			SDL_free(renderTargets);
			break;
		case MARK_RESOLVETARGET:
			READ(resolveTarget.type);
			if (resolveTarget.type == FNA3D_RENDERTARGET_TYPE_2D)
			{
				READ(resolveTarget.twod.width);
				READ(resolveTarget.twod.height);
			}
			else
			{
				SDL_assert(resolveTarget.type == FNA3D_RENDERTARGET_TYPE_CUBE);
				READ(resolveTarget.cube.size);
				READ(resolveTarget.cube.face);
			}

			READ(resolveTarget.levelCount);
			READ(resolveTarget.multiSampleCount);

			READ(nonNull);
			if (nonNull)
			{
				READ(i);
				resolveTarget.texture = traceTexture[i];
			}
			else
			{
				resolveTarget.texture = NULL;
			}

			READ(nonNull);
			if (nonNull)
			{
				READ(i);
				resolveTarget.colorBuffer = traceRenderbuffer[i];
			}
			else
			{
				resolveTarget.colorBuffer = NULL;
			}

			break;
		case MARK_RESETBACKBUFFER:
			READ(presentationParameters.backBufferWidth);
			READ(presentationParameters.backBufferHeight);
			READ(presentationParameters.backBufferFormat);
			READ(presentationParameters.multiSampleCount);
			READ(presentationParameters.isFullScreen);
			READ(presentationParameters.depthStencilFormat);
			READ(presentationParameters.presentationInterval);
			READ(presentationParameters.displayOrientation);
			READ(presentationParameters.renderTargetUsage);
			break;
		case MARK_READBACKBUFFER:
			READ(x);
			READ(y);
			READ(w);
			READ(h);
			READ(dataLength);
			break;
		case MARK_CREATETEXTURE2D:
			READ(format);
			READ(w);
			READ(h);
			READ(levelCount);
			READ(isRenderTarget);
			texture = (FNA3D_Texture*) 0xDEADBEEF;
			REGISTER_OBJECT(Texture, Texture, texture)
			break;
		case MARK_CREATETEXTURE3D:
			READ(format);
			READ(w);
			READ(h);
			READ(d);
			READ(levelCount);
			texture = (FNA3D_Texture*) 0xDEADBEEF;
			REGISTER_OBJECT(Texture, Texture, texture)
			break;
		case MARK_CREATETEXTURECUBE:
			READ(format);
			READ(w);
			READ(levelCount);
			READ(isRenderTarget);
			texture = (FNA3D_Texture*) 0xDEADBEEF;
			REGISTER_OBJECT(Texture, Texture, texture)
			break;
		case MARK_ADDDISPOSETEXTURE:
			READ(i);
			traceTexture[i] = NULL;
			break;
		case MARK_SETTEXTUREDATA2D:
			READ(i);
			READ(x);
			READ(y);
			READ(w);
			READ(h);
			READ(level);
			READ(dataLength);
			SDL_SeekIO(ops, dataLength, SDL_IO_SEEK_CUR);
			break;
		case MARK_SETTEXTUREDATA3D:
			READ(i);
			READ(x);
			READ(y);
			READ(z);
			READ(w);
			READ(h);
			READ(d);
			READ(level);
			READ(dataLength);
			SDL_SeekIO(ops, dataLength, SDL_IO_SEEK_CUR);
			break;
		case MARK_SETTEXTUREDATACUBE:
			READ(i);
			READ(x);
			READ(y);
			READ(w);
			READ(h);
			READ(cubeMapFace);
			READ(level);
			READ(dataLength);
			SDL_SeekIO(ops, dataLength, SDL_IO_SEEK_CUR);
			break;
		case MARK_SETTEXTUREDATAYUV:
			READ(i);
			READ(j);
			READ(k);
			READ(x);
			READ(y);
			READ(w);
			READ(h);
			READ(dataLength);
			SDL_SeekIO(ops, dataLength, SDL_IO_SEEK_CUR);
			break;
		case MARK_GETTEXTUREDATA2D:
			READ(i);
			READ(x);
			READ(y);
			READ(w);
			READ(h);
			READ(level);
			READ(dataLength);
			break;
		case MARK_GETTEXTUREDATA3D:
			READ(i);
			READ(x);
			READ(y);
			READ(z);
			READ(w);
			READ(h);
			READ(d);
			READ(level);
			READ(dataLength);
			break;
		case MARK_GETTEXTUREDATACUBE:
			READ(i);
			READ(x);
			READ(y);
			READ(w);
			READ(h);
			READ(cubeMapFace);
			READ(level);
			READ(dataLength);
			break;
		case MARK_GENCOLORRENDERBUFFER:
			READ(w);
			READ(h);
			READ(format);
			READ(multiSampleCount);
			READ(nonNull);
			if (nonNull)
			{
				READ(i);
				texture = traceTexture[i];
			}
			else
			{
				texture = NULL;
			}
			renderbuffer = (FNA3D_Renderbuffer*) 0xDEADBEEF;
			REGISTER_OBJECT(Renderbuffer, Renderbuffer, renderbuffer)
			break;
		case MARK_GENDEPTHSTENCILRENDERBUFFER:
			READ(w);
			READ(h);
			READ(depthFormat);
			READ(multiSampleCount);
			renderbuffer = (FNA3D_Renderbuffer*) 0xDEADBEEF;
			REGISTER_OBJECT(Renderbuffer, Renderbuffer, renderbuffer)
			break;
		case MARK_ADDDISPOSERENDERBUFFER:
			READ(i);
			traceRenderbuffer[i] = NULL;
			break;
		case MARK_GENVERTEXBUFFER:
			READ(dynamic);
			READ(usage);
			READ(sizeInBytes);
			buffer = (FNA3D_Buffer*) 0xDEADBEEF;
			REGISTER_OBJECT(VertexBuffer, Buffer, buffer)
			break;
		case MARK_ADDDISPOSEVERTEXBUFFER:
			READ(i);
			traceVertexBuffer[i] = NULL;
			break;
		case MARK_SETVERTEXBUFFERDATA:
			READ(i);
			READ(offsetInBytes);
			READ(elementCount);
			READ(elementSizeInBytes);
			READ(vertexStride);
			READ(dataOptions);
			SDL_SeekIO(ops, vertexStride * elementCount, SDL_IO_SEEK_CUR);
			break;
		case MARK_GETVERTEXBUFFERDATA:
			READ(i);
			READ(offsetInBytes);
			READ(elementCount);
			READ(elementSizeInBytes);
			READ(vertexStride);
			break;
		case MARK_GENINDEXBUFFER:
			READ(dynamic);
			READ(usage);
			READ(sizeInBytes);
			buffer = (FNA3D_Buffer*) 0xDEADBEEF;
			REGISTER_OBJECT(IndexBuffer, Buffer, buffer)
			break;
		case MARK_ADDDISPOSEINDEXBUFFER:
			READ(i);
			traceIndexBuffer[i] = NULL;
			break;
		case MARK_SETINDEXBUFFERDATA:
			READ(i);
			READ(offsetInBytes);
			READ(dataLength);
			READ(dataOptions);
			SDL_SeekIO(ops, dataLength, SDL_IO_SEEK_CUR);
			break;
		case MARK_GETINDEXBUFFERDATA:
			READ(i);
			READ(offsetInBytes);
			READ(dataLength);
			break;
		case MARK_CREATEEFFECT:
			READ(dataLength);
			miscBuffer = SDL_malloc(dataLength);
			SDL_ReadIO(ops, miscBuffer, dataLength);
			effect = (FNA3D_Effect*) 0xDEADBEEF;
			effectData = MOJOSHADER_compileEffect(
				(const unsigned char*) miscBuffer,
				dataLength,
				NULL,
				0,
				NULL,
				0,
				&ctx
			);
			SDL_free(miscBuffer);
			for (i = 0; i < traceEffectCount; i += 1)
			{
				if (traceEffect[i] == NULL)
				{
					traceEffect[i] = effect;
					traceEffectData[i] = effectData;
					break;
				}
			}
			if (i == traceEffectCount)
			{
				traceEffectCount += 1;
				traceEffect = (FNA3D_Effect**) SDL_realloc(
					traceEffect,
					sizeof(FNA3D_Effect*) * traceEffectCount
				);
				traceEffectData = (MOJOSHADER_effect**) SDL_realloc(
					traceEffectData,
					sizeof(MOJOSHADER_effect*) * traceEffectCount
				);
				traceEffect[i] = effect;
				traceEffectData[i] = effectData;
			}
			break;
		case MARK_CLONEEFFECT:
			READ(i);
			effect = (FNA3D_Effect*) 0xDEADBEEF;
			effectData = MOJOSHADER_cloneEffect(traceEffectData[i]);
			for (i = 0; i < traceEffectCount; i += 1)
			{
				if (traceEffect[i] == NULL)
				{
					traceEffect[i] = effect;
					traceEffectData[i] = effectData;
					break;
				}
			}
			if (i == traceEffectCount)
			{
				traceEffectCount += 1;
				traceEffect = (FNA3D_Effect**) SDL_realloc(
					traceEffect,
					sizeof(FNA3D_Effect*) * traceEffectCount
				);
				traceEffectData = (MOJOSHADER_effect**) SDL_realloc(
					traceEffectData,
					sizeof(MOJOSHADER_effect*) * traceEffectCount
				);
				traceEffect[i] = effect;
				traceEffectData[i] = effectData;
			}
			break;
		case MARK_ADDDISPOSEEFFECT:
			READ(i);
			MOJOSHADER_deleteEffect(traceEffectData[i]);
			traceEffect[i] = NULL;
			traceEffectData[i] = NULL;
			break;
		case MARK_SETEFFECTTECHNIQUE:
			READ(i);
			READ(technique);
			effectData = traceEffectData[i];
			MOJOSHADER_effectSetTechnique(
				effectData, 
				&effectData->techniques[technique]
			);
			break;
		case MARK_APPLYEFFECT:
			READ(i);
			READ(pass);
			effectData = traceEffectData[i];
			for (vi = 0; vi < effectData->param_count; vi += 1)
			{
				SDL_ReadIO(
					ops,
					effectData->params[vi].value.values,
					effectData->params[vi].value.value_count * 4
				);
			}
			if (effectData == currentEffect)
			{
				if (
					effectData->current_technique == currentTechnique &&
					pass == currentPass
				) {
					MOJOSHADER_effectCommitChanges(
						currentEffect
					);

					break;
				}

				MOJOSHADER_effectEndPass(currentEffect);
				MOJOSHADER_effectBeginPass(currentEffect, pass);
				currentTechnique = effectData->current_technique;
				currentPass = pass;

				break;
			}
			else if (currentEffect != NULL)
			{
				MOJOSHADER_effectEndPass(currentEffect);
				MOJOSHADER_effectEnd(currentEffect);
			}

			MOJOSHADER_effectBegin(
				effectData,
				&numPasses,
				0,
				&stateChanges
			);

			MOJOSHADER_effectBeginPass(effectData, pass);
			currentEffect = effectData;
			currentTechnique = effectData->current_technique;
			currentPass = pass;
			break;
		case MARK_BEGINPASSRESTORE:
			READ(i);
			effectData = traceEffectData[i];
			MOJOSHADER_effectBegin(
				effectData,
				&numPasses,
				1,
				&stateChanges
			);
			MOJOSHADER_effectBeginPass(effectData, 0);
			break;
		case MARK_ENDPASSRESTORE:
			READ(i);
			effectData = traceEffectData[i];
			MOJOSHADER_effectEndPass(effectData);
			MOJOSHADER_effectEnd(effectData);
			break;
		case MARK_CREATEQUERY:
			query = (FNA3D_Query*) 0xDEADBEEF;
			REGISTER_OBJECT(Query, Query, query)
			break;
		case MARK_ADDDISPOSEQUERY:
			READ(i);
			traceQuery[i] = NULL;
			break;
		case MARK_QUERYBEGIN:
			READ(i);
			break;
		case MARK_QUERYEND:
			READ(i);
			break;
		case MARK_QUERYPIXELCOUNT:
			READ(i);
			break;
		case MARK_SETSTRINGMARKER:
			READ(dataLength);
			SDL_SeekIO(ops, dataLength, SDL_IO_SEEK_CUR);
			break;
		case MARK_CREATEDEVICE:
		case MARK_DESTROYDEVICE:
			SDL_assert(0 && "Unexpected mark!");
			break;
		default:
			SDL_assert(0 && "Unrecognized mark!");
			break;
		}
		READ(mark);
	}

	/* Clean up. We out. */
	#define FREE_TRACES(type) \
		if (trace##type##Count > 0) \
		{ \
			SDL_free(trace##type); \
			trace##type = NULL; \
			trace##type##Count = 0; \
		}
	FREE_TRACES(Texture)
	FREE_TRACES(Renderbuffer)
	FREE_TRACES(VertexBuffer)
	FREE_TRACES(IndexBuffer)
	FREE_TRACES(Effect)
	FREE_TRACES(Query)
	#undef FREE_TRACES
	return !run;

	#undef REGISTER_OBJECT
	#undef READ
}
