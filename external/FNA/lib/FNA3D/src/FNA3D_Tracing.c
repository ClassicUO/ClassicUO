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

#ifdef FNA3D_TRACING

#include "mojoshader.h"
#include "FNA3D_Tracing.h"

#ifdef USE_SDL3
#include <SDL3/SDL.h>

#undef SDL_bool
#undef SDL_TRUE
#undef SDL_FALSE
#define SDL_bool bool
#define SDL_TRUE true
#define SDL_FALSE false
#else
#include <SDL.h>
#define SDL_Mutex SDL_mutex
#define SDL_IOStream SDL_RWops
#define SDL_IOFromFile SDL_RWFromFile
#define SDL_WriteIO(a, b, c) SDL_RWwrite(a, b, c, 1)
#define SDL_CloseIO SDL_RWclose
#endif

static const uint8_t MARK_CREATEDEVICE			= 0;
static const uint8_t MARK_DESTROYDEVICE			= 1;
static const uint8_t MARK_SWAPBUFFERS			= 2;
static const uint8_t MARK_CLEAR				= 3;
static const uint8_t MARK_DRAWINDEXEDPRIMITIVES		= 4;
static const uint8_t MARK_DRAWINSTANCEDPRIMITIVES	= 5;
static const uint8_t MARK_DRAWPRIMITIVES		= 6;
static const uint8_t MARK_SETVIEWPORT			= 7;
static const uint8_t MARK_SETSCISSORRECT		= 8;
static const uint8_t MARK_SETBLENDFACTOR		= 9;
static const uint8_t MARK_SETMULTISAMPLEMASK		= 10;
static const uint8_t MARK_SETREFERENCESTENCIL		= 11;
static const uint8_t MARK_SETBLENDSTATE			= 12;
static const uint8_t MARK_SETDEPTHSTENCILSTATE		= 13;
static const uint8_t MARK_APPLYRASTERIZERSTATE		= 14;
static const uint8_t MARK_VERIFYSAMPLER			= 15;
static const uint8_t MARK_VERIFYVERTEXSAMPLER		= 16;
static const uint8_t MARK_APPLYVERTEXBUFFERBINDINGS	= 17;
static const uint8_t MARK_SETRENDERTARGETS		= 18;
static const uint8_t MARK_RESOLVETARGET			= 19;
static const uint8_t MARK_RESETBACKBUFFER		= 20;
static const uint8_t MARK_READBACKBUFFER		= 21;
static const uint8_t MARK_CREATETEXTURE2D		= 22;
static const uint8_t MARK_CREATETEXTURE3D		= 23;
static const uint8_t MARK_CREATETEXTURECUBE		= 24;
static const uint8_t MARK_ADDDISPOSETEXTURE		= 25;
static const uint8_t MARK_SETTEXTUREDATA2D		= 26;
static const uint8_t MARK_SETTEXTUREDATA3D		= 27;
static const uint8_t MARK_SETTEXTUREDATACUBE		= 28;
static const uint8_t MARK_SETTEXTUREDATAYUV		= 29;
static const uint8_t MARK_GETTEXTUREDATA2D		= 30;
static const uint8_t MARK_GETTEXTUREDATA3D		= 31;
static const uint8_t MARK_GETTEXTUREDATACUBE		= 32;
static const uint8_t MARK_GENCOLORRENDERBUFFER		= 33;
static const uint8_t MARK_GENDEPTHSTENCILRENDERBUFFER	= 34;
static const uint8_t MARK_ADDDISPOSERENDERBUFFER	= 35;
static const uint8_t MARK_GENVERTEXBUFFER		= 36;
static const uint8_t MARK_ADDDISPOSEVERTEXBUFFER	= 37;
static const uint8_t MARK_SETVERTEXBUFFERDATA		= 38;
static const uint8_t MARK_GETVERTEXBUFFERDATA		= 39;
static const uint8_t MARK_GENINDEXBUFFER		= 40;
static const uint8_t MARK_ADDDISPOSEINDEXBUFFER		= 41;
static const uint8_t MARK_SETINDEXBUFFERDATA		= 42;
static const uint8_t MARK_GETINDEXBUFFERDATA		= 43;
static const uint8_t MARK_CREATEEFFECT			= 44;
static const uint8_t MARK_CLONEEFFECT			= 45;
static const uint8_t MARK_ADDDISPOSEEFFECT		= 46;
static const uint8_t MARK_SETEFFECTTECHNIQUE		= 47;
static const uint8_t MARK_APPLYEFFECT			= 48;
static const uint8_t MARK_BEGINPASSRESTORE		= 49;
static const uint8_t MARK_ENDPASSRESTORE		= 50;
static const uint8_t MARK_CREATEQUERY			= 51;
static const uint8_t MARK_ADDDISPOSEQUERY		= 52;
static const uint8_t MARK_QUERYBEGIN			= 53;
static const uint8_t MARK_QUERYEND			= 54;
static const uint8_t MARK_QUERYPIXELCOUNT		= 55;
static const uint8_t MARK_SETSTRINGMARKER		= 56;

#define TRACE_OBJECT(array, type) \
	static FNA3D_##type **trace##array = NULL; \
	static uint64_t trace##array##Count = 0; \
	static uint64_t FNA3D_Trace_Fetch##array(FNA3D_##type *object) \
	{ \
		uint64_t i; \
		for (i = 0; i < trace##array##Count; i += 1) \
		{ \
			if (object == trace##array[i]) \
			{ \
				return i; \
			} \
		} \
		SDL_assert(0 && "Trace object is missing!"); \
		return 0; \
	} \
	static void FNA3D_Trace_Register##array(FNA3D_##type *object) \
	{ \
		uint64_t i; \
		for (i = 0; i < trace##array##Count; i += 1) \
		{ \
			if (trace##array[i] == NULL) \
			{ \
				trace##array[i] = object; \
				return; \
			} \
		} \
		trace##array##Count += 1; \
		trace##array = SDL_realloc( \
			trace##array, \
			sizeof(FNA3D_##type*) * trace##array##Count \
		); \
		trace##array[i] = object; \
	}
TRACE_OBJECT(Texture, Texture)
TRACE_OBJECT(Renderbuffer, Renderbuffer)
TRACE_OBJECT(VertexBuffer, Buffer)
TRACE_OBJECT(IndexBuffer, Buffer)
TRACE_OBJECT(Query, Query)
static FNA3D_Effect **traceEffect = NULL;
static MOJOSHADER_effect **traceEffectData = NULL;
static uint64_t traceEffectCount = 0;
static uint64_t FNA3D_Trace_FetchEffect(FNA3D_Effect *effect)
{
	uint64_t i;
	for (i = 0; i < traceEffectCount; i += 1)
	{
		if (effect == traceEffect[i])
		{
			return i;
		}
	}
	SDL_assert(0 && "Trace object is missing!");
	return 0;
}
void FNA3D_Trace_RegisterEffect(FNA3D_Effect *effect, MOJOSHADER_effect *effectData)
{
	uint64_t i;
	for (i = 0; i < traceEffectCount; i += 1)
	{
		if (traceEffect[i] == NULL)
		{
			traceEffect[i] = effect;
			traceEffectData[i] = effectData;
			return;
		}
	}
	traceEffectCount += 1;
	traceEffect = SDL_realloc(
		traceEffect,
		sizeof(FNA3D_Effect*) * traceEffectCount
	);
	traceEffectData = SDL_realloc(
		traceEffectData,
		sizeof(MOJOSHADER_effect*) * traceEffectCount
	);
	traceEffect[i] = effect;
	traceEffectData[i] = effectData;
}
#undef TRACE_OBJECT

#define CHECK_AND_FLUSH_BUFFER(len) \
	if (len > traceBufferSize) \
	{ \
		traceBuffer = SDL_realloc(traceBuffer, len); \
		traceBufferSize = len; \
	} \
	if (traceBufferCurrentSize + len > traceBufferSize) \
	{ \
		FNA3D_Trace_FlushMemory(); \
	}
#define WRITE(val) \
	CHECK_AND_FLUSH_BUFFER(sizeof(val)) \
	SDL_memcpy((uint8_t*) traceBuffer + traceBufferCurrentSize, &val, sizeof(val)); \
	traceBufferCurrentSize += sizeof(val);
#define WRITEMEM(ptr, len) \
	CHECK_AND_FLUSH_BUFFER(len) \
	SDL_memcpy((uint8_t*) traceBuffer + traceBufferCurrentSize, ptr, len); \
	traceBufferCurrentSize += len;

static SDL_bool traceEnabled = SDL_FALSE;
static void* windowHandle = NULL;
static SDL_Mutex *traceLock = NULL;

static void* traceBuffer = NULL;
static uint32_t traceBufferCurrentSize = 0;
static uint32_t traceBufferSize = 64000000; /* 64MB */

static void FNA3D_Trace_FlushMemory()
{
	SDL_IOStream* traceFile = SDL_IOFromFile("FNA3D_Trace.bin", "ab");
	SDL_WriteIO(traceFile, traceBuffer, traceBufferCurrentSize);
	SDL_CloseIO(traceFile);
	traceBufferCurrentSize = 0;
}

void FNA3D_Trace_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
) {
	SDL_IOStream* traceFile;
	traceEnabled = !SDL_GetHintBoolean("FNA3D_DISABLE_TRACING", SDL_FALSE);
	if (!traceEnabled)
	{
		SDL_Log("FNA3D tracing disabled!");
		return;
	}
	SDL_Log("FNA3D tracing started!");
	traceFile = SDL_IOFromFile("FNA3D_Trace.bin", "wb");
	SDL_CloseIO(traceFile);
	traceBuffer = SDL_malloc(traceBufferSize);
	traceLock = SDL_CreateMutex();
	WRITE(MARK_CREATEDEVICE);
	WRITE(presentationParameters->backBufferWidth);
	WRITE(presentationParameters->backBufferHeight);
	WRITE(presentationParameters->backBufferFormat);
	WRITE(presentationParameters->multiSampleCount);
	windowHandle = presentationParameters->deviceWindowHandle;
	WRITE(presentationParameters->isFullScreen);
	WRITE(presentationParameters->depthStencilFormat);
	WRITE(presentationParameters->presentationInterval);
	WRITE(presentationParameters->displayOrientation);
	WRITE(presentationParameters->renderTargetUsage);
	WRITE(debugMode);
}

void FNA3D_Trace_DestroyDevice(void)
{
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_DESTROYDEVICE);

	#define FREE_TRACES(array) \
		if (trace##array != NULL) \
		{ \
			SDL_free(trace##array); \
			trace##array = NULL; \
		} \
		trace##array##Count = 0;
	FREE_TRACES(Texture)
	FREE_TRACES(Renderbuffer)
	FREE_TRACES(VertexBuffer)
	FREE_TRACES(IndexBuffer)
	FREE_TRACES(Query)
	if (traceEffect != NULL)
	{
		SDL_free(traceEffect);
		traceEffect = NULL;
	}
	if (traceEffectData != NULL)
	{
		SDL_free(traceEffectData);
		traceEffectData = NULL;
	}
	traceEffectCount = 0;
	#undef FREE_TRACES
	FNA3D_Trace_FlushMemory();
	SDL_free(traceBuffer);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SwapBuffers(
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
) {
	uint8_t hasSource = sourceRectangle != NULL;
	uint8_t hasDestination = destinationRectangle != NULL;

	if (!traceEnabled)
	{
		return;
	}

	SDL_assert(	overrideWindowHandle == NULL ||
			overrideWindowHandle == windowHandle	);

	SDL_LockMutex(traceLock);

	WRITE(MARK_SWAPBUFFERS);
	WRITE(hasSource);
	if (hasSource)
	{
		WRITE(sourceRectangle->x);
		WRITE(sourceRectangle->y);
		WRITE(sourceRectangle->w);
		WRITE(sourceRectangle->h);
	}
	WRITE(hasDestination);
	if (hasDestination)
	{
		WRITE(destinationRectangle->x);
		WRITE(destinationRectangle->y);
		WRITE(destinationRectangle->w);
		WRITE(destinationRectangle->h);
	}

	FNA3D_Trace_FlushMemory();
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_Clear(
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_CLEAR);
	WRITE(options);
	WRITE(color->x);
	WRITE(color->y);
	WRITE(color->z);
	WRITE(color->w);
	WRITE(depth);
	WRITE(stencil);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_DrawIndexedPrimitives(
	FNA3D_PrimitiveType primitiveType,
	int32_t baseVertex,
	int32_t minVertexIndex,
	int32_t numVertices,
	int32_t startIndex,
	int32_t primitiveCount,
	FNA3D_Buffer *indices,
	FNA3D_IndexElementSize indexElementSize
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchIndexBuffer(indices);
	WRITE(MARK_DRAWINDEXEDPRIMITIVES);
	WRITE(primitiveType);
	WRITE(baseVertex);
	WRITE(minVertexIndex);
	WRITE(numVertices);
	WRITE(startIndex);
	WRITE(primitiveCount);
	WRITE(obj);
	WRITE(indexElementSize);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_DrawInstancedPrimitives(
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
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchIndexBuffer(indices);
	WRITE(MARK_DRAWINSTANCEDPRIMITIVES);
	WRITE(primitiveType);
	WRITE(baseVertex);
	WRITE(minVertexIndex);
	WRITE(numVertices);
	WRITE(startIndex);
	WRITE(primitiveCount);
	WRITE(instanceCount);
	WRITE(obj);
	WRITE(indexElementSize);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_DrawPrimitives(
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_DRAWPRIMITIVES);
	WRITE(primitiveType);
	WRITE(vertexStart);
	WRITE(primitiveCount);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetViewport(FNA3D_Viewport *viewport)
{
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_SETVIEWPORT);
	WRITE(viewport->x);
	WRITE(viewport->y);
	WRITE(viewport->w);
	WRITE(viewport->h);
	WRITE(viewport->minDepth);
	WRITE(viewport->maxDepth);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetScissorRect(FNA3D_Rect *scissor)
{
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_SETSCISSORRECT);
	WRITE(scissor->x);
	WRITE(scissor->y);
	WRITE(scissor->w);
	WRITE(scissor->h);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetBlendFactor(
	FNA3D_Color *blendFactor
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_SETBLENDFACTOR);
	WRITE(blendFactor->r);
	WRITE(blendFactor->g);
	WRITE(blendFactor->b);
	WRITE(blendFactor->a);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetMultiSampleMask(int32_t mask)
{
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_SETMULTISAMPLEMASK);
	WRITE(mask);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetReferenceStencil(int32_t ref)
{
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_SETREFERENCESTENCIL);
	WRITE(ref);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetBlendState(
	FNA3D_BlendState *blendState
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_SETBLENDSTATE);
	WRITE(blendState->colorSourceBlend);
	WRITE(blendState->colorDestinationBlend);
	WRITE(blendState->colorBlendFunction);
	WRITE(blendState->alphaSourceBlend);
	WRITE(blendState->alphaDestinationBlend);
	WRITE(blendState->alphaBlendFunction);
	WRITE(blendState->colorWriteEnable);
	WRITE(blendState->colorWriteEnable1);
	WRITE(blendState->colorWriteEnable2);
	WRITE(blendState->colorWriteEnable3);
	WRITE(blendState->blendFactor.r);
	WRITE(blendState->blendFactor.g);
	WRITE(blendState->blendFactor.b);
	WRITE(blendState->blendFactor.a);
	WRITE(blendState->multiSampleMask);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetDepthStencilState(
	FNA3D_DepthStencilState *depthStencilState
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_SETDEPTHSTENCILSTATE);
	WRITE(depthStencilState->depthBufferEnable);
	WRITE(depthStencilState->depthBufferWriteEnable);
	WRITE(depthStencilState->depthBufferFunction);
	WRITE(depthStencilState->stencilEnable);
	WRITE(depthStencilState->stencilMask);
	WRITE(depthStencilState->stencilWriteMask);
	WRITE(depthStencilState->twoSidedStencilMode);
	WRITE(depthStencilState->stencilFail);
	WRITE(depthStencilState->stencilDepthBufferFail);
	WRITE(depthStencilState->stencilPass);
	WRITE(depthStencilState->stencilFunction);
	WRITE(depthStencilState->ccwStencilFail);
	WRITE(depthStencilState->ccwStencilDepthBufferFail);
	WRITE(depthStencilState->ccwStencilPass);
	WRITE(depthStencilState->ccwStencilFunction);
	WRITE(depthStencilState->referenceStencil);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_ApplyRasterizerState(
	FNA3D_RasterizerState *rasterizerState
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_APPLYRASTERIZERSTATE);
	WRITE(rasterizerState->fillMode);
	WRITE(rasterizerState->cullMode);
	WRITE(rasterizerState->depthBias);
	WRITE(rasterizerState->slopeScaleDepthBias);
	WRITE(rasterizerState->scissorTestEnable);
	WRITE(rasterizerState->multiSampleAntiAlias);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_VerifySampler(
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	WRITE(MARK_VERIFYSAMPLER);
	WRITE(index);
	WRITE(obj);
	WRITE(sampler->filter);
	WRITE(sampler->addressU);
	WRITE(sampler->addressV);
	WRITE(sampler->addressW);
	WRITE(sampler->mipMapLevelOfDetailBias);
	WRITE(sampler->maxAnisotropy);
	WRITE(sampler->maxMipLevel);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_VerifyVertexSampler(
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	WRITE(MARK_VERIFYVERTEXSAMPLER);
	WRITE(index);
	WRITE(obj);
	WRITE(sampler->filter);
	WRITE(sampler->addressU);
	WRITE(sampler->addressV);
	WRITE(sampler->addressW);
	WRITE(sampler->mipMapLevelOfDetailBias);
	WRITE(sampler->maxAnisotropy);
	WRITE(sampler->maxMipLevel);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_ApplyVertexBufferBindings(
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
) {
	uint64_t obj;
	int32_t i, j;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_APPLYVERTEXBUFFERBINDINGS);
	WRITE(numBindings);
	for (i = 0; i < numBindings; i += 1)
	{
		const FNA3D_VertexBufferBinding *binding = &bindings[i];
		obj = FNA3D_Trace_FetchVertexBuffer(binding->vertexBuffer);
		WRITE(obj);
		WRITE(binding->vertexDeclaration.vertexStride);
		WRITE(binding->vertexDeclaration.elementCount);
		for (j = 0; j < binding->vertexDeclaration.elementCount; j += 1)
		{
			const FNA3D_VertexElement *elem =
				&binding->vertexDeclaration.elements[j];
			WRITE(elem->offset);
			WRITE(elem->vertexElementFormat);
			WRITE(elem->vertexElementUsage);
			WRITE(elem->usageIndex);
		}
		WRITE(binding->vertexOffset);
		WRITE(binding->instanceFrequency);
	}
	WRITE(bindingsUpdated);
	WRITE(baseVertex);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetRenderTargets(
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
) {
	uint64_t obj;
	int32_t i;
	uint8_t nonNull;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_SETRENDERTARGETS);
	WRITE(numRenderTargets);
	for (i = 0; i < numRenderTargets; i += 1)
	{
		const FNA3D_RenderTargetBinding *binding = &renderTargets[i];
		WRITE(binding->type);
		if (binding->type == FNA3D_RENDERTARGET_TYPE_2D)
		{
			WRITE(binding->twod.width);
			WRITE(binding->twod.height);
		}
		else
		{
			SDL_assert(binding->type == FNA3D_RENDERTARGET_TYPE_CUBE);
			WRITE(binding->cube.size);
			WRITE(binding->cube.face);
		}

		WRITE(binding->levelCount);
		WRITE(binding->multiSampleCount);

		nonNull = binding->texture != NULL;
		WRITE(nonNull);
		if (nonNull)
		{
			obj = FNA3D_Trace_FetchTexture(binding->texture);
			WRITE(obj);
		}

		nonNull = binding->colorBuffer != NULL;
		WRITE(nonNull);
		if (nonNull)
		{
			obj = FNA3D_Trace_FetchRenderbuffer(binding->colorBuffer);
			WRITE(obj);
		}
	}

	nonNull = depthStencilBuffer != NULL;
	WRITE(nonNull);
	if (nonNull)
	{
		obj = FNA3D_Trace_FetchRenderbuffer(depthStencilBuffer);
		WRITE(obj);
	}

	WRITE(depthFormat);
	WRITE(preserveTargetContents);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_ResolveTarget(
	FNA3D_RenderTargetBinding *target
) {
	uint64_t obj;
	uint8_t nonNull;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_RESOLVETARGET);
	WRITE(target->type);
	if (target->type == FNA3D_RENDERTARGET_TYPE_2D)
	{
		WRITE(target->twod.width);
		WRITE(target->twod.height);
	}
	else
	{
		SDL_assert(target->type == FNA3D_RENDERTARGET_TYPE_CUBE);
		WRITE(target->cube.size);
		WRITE(target->cube.face);
	}

	WRITE(target->levelCount);
	WRITE(target->multiSampleCount);

	nonNull = target->texture != NULL;
	WRITE(nonNull);
	if (nonNull)
	{
		obj = FNA3D_Trace_FetchTexture(target->texture);
		WRITE(obj);
	}

	nonNull = target->colorBuffer != NULL;
	WRITE(nonNull);
	if (nonNull)
	{
		obj = FNA3D_Trace_FetchRenderbuffer(target->colorBuffer);
		WRITE(obj);
	}
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_ResetBackbuffer(
	FNA3D_PresentationParameters *presentationParameters
) {
	if (!traceEnabled)
	{
		return;
	}

	SDL_assert(presentationParameters->deviceWindowHandle == windowHandle);

	SDL_LockMutex(traceLock);
	WRITE(MARK_RESETBACKBUFFER);
	WRITE(presentationParameters->backBufferWidth);
	WRITE(presentationParameters->backBufferHeight);
	WRITE(presentationParameters->backBufferFormat);
	WRITE(presentationParameters->multiSampleCount);
	WRITE(presentationParameters->isFullScreen);
	WRITE(presentationParameters->depthStencilFormat);
	WRITE(presentationParameters->presentationInterval);
	WRITE(presentationParameters->displayOrientation);
	WRITE(presentationParameters->renderTargetUsage);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_ReadBackbuffer(
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t dataLength
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	WRITE(MARK_READBACKBUFFER);
	WRITE(x);
	WRITE(y);
	WRITE(w);
	WRITE(h);
	WRITE(dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_CreateTexture2D(
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget,
	FNA3D_Texture *retval
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterTexture(retval);
	WRITE(MARK_CREATETEXTURE2D);
	WRITE(format);
	WRITE(width);
	WRITE(height);
	WRITE(levelCount);
	WRITE(isRenderTarget);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_CreateTexture3D(
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount,
	FNA3D_Texture *retval
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterTexture(retval);
	WRITE(MARK_CREATETEXTURE3D);
	WRITE(format);
	WRITE(width);
	WRITE(height);
	WRITE(depth);
	WRITE(levelCount);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_CreateTextureCube(
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget,
	FNA3D_Texture *retval
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterTexture(retval);
	WRITE(MARK_CREATETEXTURECUBE);
	WRITE(format);
	WRITE(size);
	WRITE(levelCount);
	WRITE(isRenderTarget);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_AddDisposeTexture(
	FNA3D_Texture *texture
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	traceTexture[obj] = NULL;
	WRITE(MARK_ADDDISPOSETEXTURE);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetTextureData2D(
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	WRITE(MARK_SETTEXTUREDATA2D);
	WRITE(obj);
	WRITE(x);
	WRITE(y);
	WRITE(w);
	WRITE(h);
	WRITE(level);
	WRITE(dataLength);
	WRITEMEM(data, dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetTextureData3D(
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
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	WRITE(MARK_SETTEXTUREDATA3D);
	WRITE(obj);
	WRITE(x);
	WRITE(y);
	WRITE(z);
	WRITE(w);
	WRITE(h);
	WRITE(d);
	WRITE(level);
	WRITE(dataLength);
	WRITEMEM(data, dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetTextureDataCube(
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
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	WRITE(MARK_SETTEXTUREDATACUBE);
	WRITE(obj);
	WRITE(x);
	WRITE(y);
	WRITE(w);
	WRITE(h);
	WRITE(cubeMapFace);
	WRITE(level);
	WRITE(dataLength);
	WRITEMEM(data, dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetTextureDataYUV(
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
	uint64_t objY, objU, objV;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	objY = FNA3D_Trace_FetchTexture(y);
	objU = FNA3D_Trace_FetchTexture(u);
	objV = FNA3D_Trace_FetchTexture(v);
	WRITE(MARK_SETTEXTUREDATAYUV);
	WRITE(objY);
	WRITE(objU);
	WRITE(objV);
	WRITE(yWidth);
	WRITE(yHeight);
	WRITE(uvWidth);
	WRITE(uvHeight);
	WRITE(dataLength);
	WRITEMEM(data, dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GetTextureData2D(
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	int32_t dataLength
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	WRITE(MARK_GETTEXTUREDATA2D);
	WRITE(obj);
	WRITE(x);
	WRITE(y);
	WRITE(w);
	WRITE(h);
	WRITE(level);
	WRITE(dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GetTextureData3D(
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t z,
	int32_t w,
	int32_t h,
	int32_t d,
	int32_t level,
	int32_t dataLength
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	WRITE(MARK_GETTEXTUREDATA3D);
	WRITE(obj);
	WRITE(x);
	WRITE(y);
	WRITE(z);
	WRITE(w);
	WRITE(h);
	WRITE(d);
	WRITE(level);
	WRITE(dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GetTextureDataCube(
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	FNA3D_CubeMapFace cubeMapFace,
	int32_t level,
	int32_t dataLength
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchTexture(texture);
	WRITE(MARK_GETTEXTUREDATACUBE);
	WRITE(obj);
	WRITE(x);
	WRITE(y);
	WRITE(w);
	WRITE(h);
	WRITE(cubeMapFace);
	WRITE(level);
	WRITE(dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GenColorRenderbuffer(
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture,
	FNA3D_Renderbuffer *retval
) {
	uint64_t obj;
	uint8_t nonNull;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterRenderbuffer(retval);
	WRITE(MARK_GENCOLORRENDERBUFFER);
	WRITE(width);
	WRITE(height);
	WRITE(format);
	WRITE(multiSampleCount);
	nonNull = texture != NULL;
	WRITE(nonNull);
	if (nonNull)
	{
		obj = FNA3D_Trace_FetchTexture(texture);
		WRITE(obj);
	}
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GenDepthStencilRenderbuffer(
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount,
	FNA3D_Renderbuffer *retval
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterRenderbuffer(retval);
	WRITE(MARK_GENDEPTHSTENCILRENDERBUFFER);
	WRITE(width);
	WRITE(height);
	WRITE(format);
	WRITE(multiSampleCount);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_AddDisposeRenderbuffer(
	FNA3D_Renderbuffer *renderbuffer
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchRenderbuffer(renderbuffer);
	traceRenderbuffer[obj] = NULL;
	WRITE(MARK_ADDDISPOSERENDERBUFFER);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GenVertexBuffer(
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes,
	FNA3D_Buffer *retval
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterVertexBuffer(retval);
	WRITE(MARK_GENVERTEXBUFFER);
	WRITE(dynamic);
	WRITE(usage);
	WRITE(sizeInBytes);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_AddDisposeVertexBuffer(
	FNA3D_Buffer *buffer
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchVertexBuffer(buffer);
	traceVertexBuffer[obj] = NULL;
	WRITE(MARK_ADDDISPOSEVERTEXBUFFER);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetVertexBufferData(
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchVertexBuffer(buffer);
	WRITE(MARK_SETVERTEXBUFFERDATA);
	WRITE(obj);
	WRITE(offsetInBytes);
	WRITE(elementCount);
	WRITE(elementSizeInBytes);
	WRITE(vertexStride);
	WRITE(options);
	WRITEMEM(data, vertexStride * elementCount);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GetVertexBufferData(
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchVertexBuffer(buffer);
	WRITE(MARK_GETVERTEXBUFFERDATA);
	WRITE(obj);
	WRITE(offsetInBytes);
	WRITE(elementCount);
	WRITE(elementSizeInBytes);
	WRITE(vertexStride);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GenIndexBuffer(
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes,
	FNA3D_Buffer *retval
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterIndexBuffer(retval);
	WRITE(MARK_GENINDEXBUFFER);
	WRITE(dynamic);
	WRITE(usage);
	WRITE(sizeInBytes);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_AddDisposeIndexBuffer(
	FNA3D_Buffer *buffer
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchIndexBuffer(buffer);
	traceIndexBuffer[obj] = NULL;
	WRITE(MARK_ADDDISPOSEINDEXBUFFER);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetIndexBufferData(
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchIndexBuffer(buffer);
	WRITE(MARK_SETINDEXBUFFERDATA);
	WRITE(obj);
	WRITE(offsetInBytes);
	WRITE(dataLength);
	WRITE(options);
	WRITEMEM(data, dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_GetIndexBufferData(
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	int32_t dataLength
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchIndexBuffer(buffer);
	WRITE(MARK_GETINDEXBUFFERDATA);
	WRITE(obj);
	WRITE(offsetInBytes);
	WRITE(dataLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_CreateEffect(
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect *retval,
	MOJOSHADER_effect *retvalData
) {
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterEffect(retval, retvalData);
	WRITE(MARK_CREATEEFFECT);
	WRITE(effectCodeLength);
	WRITEMEM(effectCode, effectCodeLength);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_CloneEffect(
	FNA3D_Effect *cloneSource,
	FNA3D_Effect *retval,
	MOJOSHADER_effect *retvalData
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterEffect(retval, retvalData);
	obj = FNA3D_Trace_FetchEffect(cloneSource);
	WRITE(MARK_CLONEEFFECT);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_AddDisposeEffect(
	FNA3D_Effect *effect
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchEffect(effect);
	traceEffect[obj] = NULL;
	traceEffectData[obj] = NULL;
	WRITE(MARK_ADDDISPOSEEFFECT);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetEffectTechnique(
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
) {
	uint64_t obj;
	int32_t i;
	MOJOSHADER_effect *effectData;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchEffect(effect);
	effectData = traceEffectData[obj];
	WRITE(MARK_SETEFFECTTECHNIQUE);
	WRITE(obj);
	for (i = 0; i < effectData->technique_count; i += 1)
	{
		if (technique == &effectData->techniques[i])
		{
			break;
		}
	}
	WRITE(i);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_ApplyEffect(
	FNA3D_Effect *effect,
	uint32_t pass
) {
	uint64_t obj;
	MOJOSHADER_effect *effectData;
	int i;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchEffect(effect);
	effectData = traceEffectData[obj];
	WRITE(MARK_APPLYEFFECT);
	WRITE(obj);
	WRITE(pass);
	for (i = 0; i < effectData->param_count; i += 1)
	{
		WRITEMEM(effectData->params[i].value.values, effectData->params[i].value.value_count * 4);
	}
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_BeginPassRestore(
	FNA3D_Effect *effect
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchEffect(effect);
	WRITE(MARK_BEGINPASSRESTORE);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_EndPassRestore(
	FNA3D_Effect *effect
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchEffect(effect);
	WRITE(MARK_ENDPASSRESTORE);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_CreateQuery(FNA3D_Query *retval)
{
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	FNA3D_Trace_RegisterQuery(retval);
	WRITE(MARK_CREATEQUERY);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_AddDisposeQuery(FNA3D_Query *query)
{
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchQuery(query);
	traceQuery[obj] = NULL;
	WRITE(MARK_ADDDISPOSEQUERY);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_QueryBegin(FNA3D_Query *query)
{
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchQuery(query);
	WRITE(MARK_QUERYBEGIN);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_QueryEnd(FNA3D_Query *query)
{
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchQuery(query);
	WRITE(MARK_QUERYEND);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_QueryPixelCount(
	FNA3D_Query *query
) {
	uint64_t obj;
	if (!traceEnabled)
	{
		return;
	}
	SDL_LockMutex(traceLock);
	obj = FNA3D_Trace_FetchQuery(query);
	WRITE(MARK_QUERYPIXELCOUNT);
	WRITE(obj);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetStringMarker(const char *text)
{
	int32_t len;

	if (!traceEnabled)
	{
		return;
	}

	SDL_LockMutex(traceLock);
	len = (int32_t) SDL_strlen(text) + 1;
	WRITE(MARK_SETSTRINGMARKER);
	WRITE(len);
	WRITEMEM(text, len);
	SDL_UnlockMutex(traceLock);
}

void FNA3D_Trace_SetTextureName(void *texture, const char *text)
{
	SDL_assert(0 && "Trace_SetTextureName not implemented -kg");
	return;
}

#undef WRITE

#else

extern int this_tu_is_empty;

#endif /* FNA3D_TRACING */
