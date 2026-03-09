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

#ifdef USE_SDL3
#include <SDL3/SDL.h>
#define SDL_WINDOW_FULLSCREEN_DESKTOP SDL_WINDOW_FULLSCREEN
#else
#include <SDL.h>
#define SDL_Mutex SDL_mutex
#define SDL_IOStream SDL_RWops
#define SDL_IOFromFile SDL_RWFromFile
#define SDL_ReadIO(a, b, c) SDL_RWread(a, b, c, 1)
#define SDL_CloseIO SDL_RWclose
#define SDL_CreateWindow(a, b, c, d) \
	SDL_CreateWindow(a, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, b, c, d)
#define SDL_EVENT_QUIT SDL_QUIT
#endif
#include <mojoshader.h>
#include <FNA3D.h>

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
#define MARK_SETTEXTURENAME			57

typedef enum
{
	VSYNC_DEFAULT,
	VSYNC_FORCE_ON,
	VSYNC_FORCE_OFF
} VSyncMode;

/* #define TOO_MUCH_RAM */
#ifdef TOO_MUCH_RAM
typedef struct FAKEIO
{
	uint8_t *buffer;
	uint8_t *current;
} FAKEIO;
static FAKEIO* FAKE_IOFromFile(const char *file, const char *mode)
{
	size_t len;
	void *blob;

	FAKEIO *io = (FAKEIO*) SDL_malloc(sizeof(io));
	if (io == NULL)
	{
		return NULL;
	}

	blob = SDL_LoadFile(file, &len);
	if (blob == NULL)
	{
		SDL_free(io);
		return NULL;
	}

	io->buffer = (uint8_t*) blob;
	io->current = io->buffer;
	return io;
}

static bool FAKE_CloseIO(FAKEIO *io)
{
	SDL_free(io->buffer);
	SDL_free(io);
	return true;
}

static size_t FAKE_ReadIO(FAKEIO *io, void *ptr, size_t size)
{
	/* Size checks? Where we're going we don't need size checks */
	SDL_memcpy(ptr, io->current, size);
	io->current += size;
}

#define SDL_IOStream FAKEIO
#define SDL_IOFromFile FAKE_IOFromFile
#define SDL_CloseIO FAKE_CloseIO
#define SDL_ReadIO FAKE_ReadIO
#endif /* TOO_MUCH_RAM */

static uint8_t replay(
	const char *filename,
	uint8_t forceDebugMode,
	VSyncMode vsync,
	uint8_t fullscreen,
	uint32_t delayMS
) {
	#define READ(val) SDL_ReadIO(ops, &val, sizeof(val))

#ifdef USE_SDL3
	const SDL_DisplayMode *mode;
#endif
	SDL_WindowFlags flags;
	SDL_IOStream *ops;
	SDL_Event evt;
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

	/* Check for the trace file */
	ops = SDL_IOFromFile(filename, "rb");
	if (ops == NULL)
	{
		SDL_Log("%s not found!", filename);
		return 0;
	}

	/* Beginning of the file should be a CreateDevice call */
	READ(mark);
	if (mark != MARK_CREATEDEVICE)
	{
		SDL_Log("%s is a bad trace!", filename);
		SDL_CloseIO(ops);
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

	if (vsync == VSYNC_FORCE_ON)
	{
		presentationParameters.presentationInterval = FNA3D_PRESENTINTERVAL_ONE;
	}
	else if (vsync == VSYNC_FORCE_OFF)
	{
		presentationParameters.presentationInterval = FNA3D_PRESENTINTERVAL_IMMEDIATE;
	}

	presentationParameters.isFullScreen |= fullscreen;

	/* Create a window alongside the device */
	flags = FNA3D_PrepareWindowAttributes();
	if (presentationParameters.isFullScreen)
	{
		flags |= SDL_WINDOW_FULLSCREEN_DESKTOP;
	}
#ifdef USE_SDL3
	flags |= SDL_WINDOW_HIGH_PIXEL_DENSITY;
	mode = SDL_GetDesktopDisplayMode(SDL_GetPrimaryDisplay());
	SDL_Log("Pixel density is %f", mode->pixel_density);
#endif
	presentationParameters.deviceWindowHandle = SDL_CreateWindow(
		"FNA3D Replay",
#ifdef USE_SDL3
		(int) (presentationParameters.backBufferWidth / mode->pixel_density),
		(int) (presentationParameters.backBufferHeight / mode->pixel_density),
#else
		presentationParameters.backBufferWidth,
		presentationParameters.backBufferHeight,
#endif
		flags
	);
	device = FNA3D_CreateDevice(&presentationParameters, debugMode || forceDebugMode);

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
			FNA3D_SwapBuffers(
				device,
				hasSource ? &sourceRectangle : NULL,
				hasDestination ? &destinationRectangle : NULL,
				presentationParameters.deviceWindowHandle
			);
			while (SDL_PollEvent(&evt) > 0)
			{
				if (evt.type == SDL_EVENT_QUIT)
				{
					run = 0;
				}
			}
			if (delayMS > 0)
			{
				SDL_Delay(delayMS);
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
			FNA3D_Clear(device, options, &color, depth, stencil);
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
			FNA3D_DrawIndexedPrimitives(
				device,
				primitiveType,
				baseVertex,
				minVertexIndex,
				numVertices,
				startIndex,
				primitiveCount,
				traceIndexBuffer[i],
				indexElementSize
			);
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
			FNA3D_DrawInstancedPrimitives(
				device,
				primitiveType,
				baseVertex,
				minVertexIndex,
				numVertices,
				startIndex,
				primitiveCount,
				instanceCount,
				traceIndexBuffer[i],
				indexElementSize
			);
			break;
		case MARK_DRAWPRIMITIVES:
			READ(primitiveType);
			READ(vertexStart);
			READ(primitiveCount);
			FNA3D_DrawPrimitives(
				device,
				primitiveType,
				vertexStart,
				primitiveCount
			);
			break;
		case MARK_SETVIEWPORT:
			READ(viewport.x);
			READ(viewport.y);
			READ(viewport.w);
			READ(viewport.h);
			READ(viewport.minDepth);
			READ(viewport.maxDepth);
			FNA3D_SetViewport(device, &viewport);
			break;
		case MARK_SETSCISSORRECT:
			READ(scissor.x);
			READ(scissor.y);
			READ(scissor.w);
			READ(scissor.h);
			FNA3D_SetScissorRect(device, &scissor);
			break;
		case MARK_SETBLENDFACTOR:
			READ(blendFactor.r);
			READ(blendFactor.g);
			READ(blendFactor.b);
			READ(blendFactor.a);
			FNA3D_SetBlendFactor(device, &blendFactor);
			break;
		case MARK_SETMULTISAMPLEMASK:
			READ(mask);
			FNA3D_SetMultiSampleMask(device, mask);
			break;
		case MARK_SETREFERENCESTENCIL:
			READ(ref);
			FNA3D_SetReferenceStencil(device, ref);
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
			FNA3D_SetBlendState(device, &blendState);
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
			FNA3D_SetDepthStencilState(device, &depthStencilState);
			break;
		case MARK_APPLYRASTERIZERSTATE:
			READ(rasterizerState.fillMode);
			READ(rasterizerState.cullMode);
			READ(rasterizerState.depthBias);
			READ(rasterizerState.slopeScaleDepthBias);
			READ(rasterizerState.scissorTestEnable);
			READ(rasterizerState.multiSampleAntiAlias);
			FNA3D_ApplyRasterizerState(device, &rasterizerState);
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
			FNA3D_VerifySampler(
				device,
				index,
				traceTexture[i],
				&sampler
			);
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
			FNA3D_VerifyVertexSampler(
				device,
				index,
				traceTexture[i],
				&sampler
			);
			break;
		case MARK_APPLYVERTEXBUFFERBINDINGS:
			READ(numBindings);
			bindings = (FNA3D_VertexBufferBinding*) SDL_malloc(
				sizeof(FNA3D_VertexBufferBinding) *
				numBindings
			);
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
			FNA3D_ApplyVertexBufferBindings(
				device,
				bindings,
				numBindings,
				bindingsUpdated,
				baseVertex
			);
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

			FNA3D_SetRenderTargets(
				device,
				renderTargets,
				numRenderTargets,
				depthStencilBuffer,
				depthFormat,
				preserveTargetContents
			);

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

			FNA3D_ResolveTarget(device, &resolveTarget);
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
			if (vsync == VSYNC_FORCE_ON)
			{
				presentationParameters.presentationInterval = FNA3D_PRESENTINTERVAL_ONE;
			}
			else if (vsync == VSYNC_FORCE_OFF)
			{
				presentationParameters.presentationInterval = FNA3D_PRESENTINTERVAL_IMMEDIATE;
			}
			presentationParameters.isFullScreen |= fullscreen;
			SDL_SetWindowFullscreen(
				presentationParameters.deviceWindowHandle,
				presentationParameters.isFullScreen ?
					SDL_WINDOW_FULLSCREEN_DESKTOP :
					0
			);
			SDL_SetWindowSize(
				presentationParameters.deviceWindowHandle,
#ifdef USE_SDL3
				(int) (presentationParameters.backBufferWidth / mode->pixel_density),
				(int) (presentationParameters.backBufferHeight / mode->pixel_density)
#else
				presentationParameters.backBufferWidth,
				presentationParameters.backBufferHeight
#endif
			);
			FNA3D_ResetBackbuffer(device, &presentationParameters);
			break;
		case MARK_READBACKBUFFER:
			READ(x);
			READ(y);
			READ(w);
			READ(h);
			READ(dataLength);
			miscBuffer = SDL_malloc(dataLength);
			FNA3D_ReadBackbuffer(
				device,
				x,
				y,
				w,
				h,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
			break;
		case MARK_CREATETEXTURE2D:
			READ(format);
			READ(w);
			READ(h);
			READ(levelCount);
			READ(isRenderTarget);
			texture = FNA3D_CreateTexture2D(
				device,
				format,
				w,
				h,
				levelCount,
				isRenderTarget
			);
			REGISTER_OBJECT(Texture, Texture, texture)
			break;
		case MARK_CREATETEXTURE3D:
			READ(format);
			READ(w);
			READ(h);
			READ(d);
			READ(levelCount);
			texture = FNA3D_CreateTexture3D(
				device,
				format,
				w,
				h,
				d,
				levelCount
			);
			REGISTER_OBJECT(Texture, Texture, texture)
			break;
		case MARK_CREATETEXTURECUBE:
			READ(format);
			READ(w);
			READ(levelCount);
			READ(isRenderTarget);
			texture = FNA3D_CreateTextureCube(
				device,
				format,
				w,
				levelCount,
				isRenderTarget
			);
			REGISTER_OBJECT(Texture, Texture, texture)
			break;
		case MARK_ADDDISPOSETEXTURE:
			READ(i);
			FNA3D_AddDisposeTexture(device, traceTexture[i]);
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
			miscBuffer = SDL_malloc(dataLength);
			SDL_ReadIO(ops, miscBuffer, dataLength);
			FNA3D_SetTextureData2D(
				device,
				traceTexture[i],
				x,
				y,
				w,
				h,
				level,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
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
			miscBuffer = SDL_malloc(dataLength);
			SDL_ReadIO(ops, miscBuffer, dataLength);
			FNA3D_SetTextureData3D(
				device,
				traceTexture[i],
				x,
				y,
				z,
				w,
				h,
				d,
				level,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
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
			miscBuffer = SDL_malloc(dataLength);
			SDL_ReadIO(ops, miscBuffer, dataLength);
			FNA3D_SetTextureDataCube(
				device,
				traceTexture[i],
				x,
				y,
				w,
				h,
				cubeMapFace,
				level,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
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
			miscBuffer = SDL_malloc(dataLength);
			SDL_ReadIO(ops, miscBuffer, dataLength);
			FNA3D_SetTextureDataYUV(
				device,
				traceTexture[i],
				traceTexture[j],
				traceTexture[k],
				x,
				y,
				w,
				h,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
			break;
		case MARK_GETTEXTUREDATA2D:
			READ(i);
			READ(x);
			READ(y);
			READ(w);
			READ(h);
			READ(level);
			READ(dataLength);
			miscBuffer = SDL_malloc(dataLength);
			FNA3D_GetTextureData2D(
				device,
				traceTexture[i],
				x,
				y,
				w,
				h,
				level,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
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
			miscBuffer = SDL_malloc(dataLength);
			FNA3D_GetTextureData3D(
				device,
				traceTexture[i],
				x,
				y,
				z,
				w,
				h,
				d,
				level,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
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
			miscBuffer = SDL_malloc(dataLength);
			FNA3D_GetTextureDataCube(
				device,
				traceTexture[i],
				x,
				y,
				w,
				h,
				cubeMapFace,
				level,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
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
			renderbuffer = FNA3D_GenColorRenderbuffer(
				device,
				w,
				h,
				format,
				multiSampleCount,
				texture
			);
			REGISTER_OBJECT(Renderbuffer, Renderbuffer, renderbuffer)
			break;
		case MARK_GENDEPTHSTENCILRENDERBUFFER:
			READ(w);
			READ(h);
			READ(depthFormat);
			READ(multiSampleCount);
			renderbuffer = FNA3D_GenDepthStencilRenderbuffer(
				device,
				w,
				h,
				depthFormat,
				multiSampleCount
			);
			REGISTER_OBJECT(Renderbuffer, Renderbuffer, renderbuffer)
			break;
		case MARK_ADDDISPOSERENDERBUFFER:
			READ(i);
			FNA3D_AddDisposeRenderbuffer(
				device,
				traceRenderbuffer[i]
			);
			traceRenderbuffer[i] = NULL;
			break;
		case MARK_GENVERTEXBUFFER:
			READ(dynamic);
			READ(usage);
			READ(sizeInBytes);
			buffer = FNA3D_GenVertexBuffer(
				device,
				dynamic,
				usage,
				sizeInBytes
			);
			REGISTER_OBJECT(VertexBuffer, Buffer, buffer)
			break;
		case MARK_ADDDISPOSEVERTEXBUFFER:
			READ(i);
			FNA3D_AddDisposeVertexBuffer(
				device,
				traceVertexBuffer[i]
			);
			traceVertexBuffer[i] = NULL;
			break;
		case MARK_SETVERTEXBUFFERDATA:
			READ(i);
			READ(offsetInBytes);
			READ(elementCount);
			READ(elementSizeInBytes);
			READ(vertexStride);
			READ(dataOptions);
			miscBuffer = SDL_malloc(vertexStride * elementCount);
			SDL_ReadIO(ops, miscBuffer, vertexStride * elementCount);
			FNA3D_SetVertexBufferData(
				device,
				traceVertexBuffer[i],
				offsetInBytes,
				miscBuffer,
				elementCount,
				elementSizeInBytes,
				vertexStride,
				dataOptions
			);
			SDL_free(miscBuffer);
			break;
		case MARK_GETVERTEXBUFFERDATA:
			READ(i);
			READ(offsetInBytes);
			READ(elementCount);
			READ(elementSizeInBytes);
			READ(vertexStride);
			miscBuffer = SDL_malloc(vertexStride * elementCount);
			FNA3D_GetVertexBufferData(
				device,
				traceVertexBuffer[i],
				offsetInBytes,
				miscBuffer,
				elementCount,
				elementSizeInBytes,
				vertexStride
			);
			SDL_free(miscBuffer);
			break;
		case MARK_GENINDEXBUFFER:
			READ(dynamic);
			READ(usage);
			READ(sizeInBytes);
			buffer = FNA3D_GenIndexBuffer(
				device,
				dynamic,
				usage,
				sizeInBytes
			);
			REGISTER_OBJECT(IndexBuffer, Buffer, buffer)
			break;
		case MARK_ADDDISPOSEINDEXBUFFER:
			READ(i);
			FNA3D_AddDisposeIndexBuffer(
				device,
				traceIndexBuffer[i]
			);
			traceIndexBuffer[i] = NULL;
			break;
		case MARK_SETINDEXBUFFERDATA:
			READ(i);
			READ(offsetInBytes);
			READ(dataLength);
			READ(dataOptions);
			miscBuffer = SDL_malloc(dataLength);
			SDL_ReadIO(ops, miscBuffer, dataLength);
			FNA3D_SetIndexBufferData(
				device,
				traceIndexBuffer[i],
				offsetInBytes,
				miscBuffer,
				dataLength,
				dataOptions
			);
			SDL_free(miscBuffer);
			break;
		case MARK_GETINDEXBUFFERDATA:
			READ(i);
			READ(offsetInBytes);
			READ(dataLength);
			miscBuffer = SDL_malloc(dataLength);
			FNA3D_GetIndexBufferData(
				device,
				traceIndexBuffer[i],
				offsetInBytes,
				miscBuffer,
				dataLength
			);
			SDL_free(miscBuffer);
			break;
		case MARK_CREATEEFFECT:
			READ(dataLength);
			miscBuffer = SDL_malloc(dataLength);
			SDL_ReadIO(ops, miscBuffer, dataLength);
			FNA3D_CreateEffect(
				device,
				(uint8_t*) miscBuffer,
				dataLength,
				&effect,
				&effectData
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
			FNA3D_CloneEffect(
				device,
				traceEffect[i],
				&effect,
				&effectData
			);
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
			FNA3D_AddDisposeEffect(device, traceEffect[i]);
			traceEffect[i] = NULL;
			traceEffectData[i] = NULL;
			break;
		case MARK_SETEFFECTTECHNIQUE:
			READ(i);
			READ(technique);
			FNA3D_SetEffectTechnique(
				device,
				traceEffect[i],
				&traceEffectData[i]->techniques[technique]
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
			FNA3D_ApplyEffect(
				device,
				traceEffect[i],
				pass,
				&changes
			);
			break;
		case MARK_BEGINPASSRESTORE:
			READ(i);
			FNA3D_BeginPassRestore(
				device,
				traceEffect[i],
				&changes
			);
			break;
		case MARK_ENDPASSRESTORE:
			READ(i);
			FNA3D_EndPassRestore(device, traceEffect[i]);
			break;
		case MARK_CREATEQUERY:
			query = FNA3D_CreateQuery(device);
			REGISTER_OBJECT(Query, Query, query)
			break;
		case MARK_ADDDISPOSEQUERY:
			READ(i);
			FNA3D_AddDisposeQuery(device, traceQuery[i]);
			traceQuery[i] = NULL;
			break;
		case MARK_QUERYBEGIN:
			READ(i);
			FNA3D_QueryBegin(device, traceQuery[i]);
			break;
		case MARK_QUERYEND:
			READ(i);
			FNA3D_QueryEnd(device, traceQuery[i]);
			break;
		case MARK_QUERYPIXELCOUNT:
			READ(i);
			while (!FNA3D_QueryComplete(device, traceQuery[i]))
			{
				SDL_Delay(0);
			}
			FNA3D_QueryBegin(device, traceQuery[i]);
			break;
		case MARK_SETSTRINGMARKER:
			READ(dataLength);
			miscBuffer = SDL_malloc(dataLength);
			SDL_ReadIO(ops, miscBuffer, dataLength);
			FNA3D_SetStringMarker(device, (char*) miscBuffer);
			SDL_free(miscBuffer);
			break;
		case MARK_SETTEXTURENAME:
			SDL_assert(0 && "Not implemented: SETTEXTURENAME");
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
	SDL_CloseIO(ops);
	#define FREE_TRACES(type) \
		if (trace##type##Count > 0) \
		{ \
			for (i = 0; i < trace##type##Count; i += 1) \
			{ \
				if (trace##type[i] != NULL) \
				{ \
					FNA3D_AddDispose##type( \
						device, \
						trace##type[i] \
					); \
				} \
			} \
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
	if (traceEffectData != NULL)
	{
		SDL_free(traceEffectData);
		traceEffectData = NULL;
	}
	#undef FREE_TRACES
	FNA3D_DestroyDevice(device);
	SDL_DestroyWindow(presentationParameters.deviceWindowHandle);
	return !run;

	#undef REGISTER_OBJECT
	#undef READ
}

int main(int argc, char **argv)
{
	int i;
	uint8_t forceDebugMode = 0;
	uint8_t forceFullscreen = 0;
	VSyncMode vsync = VSYNC_DEFAULT;
	uint32_t delayMS = 0;

	SDL_Init(SDL_INIT_VIDEO);

	/* Make sure we don't recursively trace... */
	SDL_SetHint("FNA3D_DISABLE_TRACING", "1");

	for (i = 1; i < argc; i += 1)
	{
		if (SDL_strcmp(argv[i], "-debug") == 0)
		{
			forceDebugMode = 1;
		}
		else if (SDL_strcmp(argv[i], "-vsync") == 0)
		{
			vsync = VSYNC_FORCE_ON;
		}
		else if (SDL_strcmp(argv[i], "-novsync") == 0)
		{
			vsync = VSYNC_FORCE_OFF;
		}
		else if (SDL_strcmp(argv[i], "-fullscreen") == 0)
		{
			forceFullscreen = 1;
		}
		else if (SDL_strstr(argv[i], "-delayms=") == argv[i])
		{
			delayMS = SDL_atoi(argv[i] + SDL_strlen("-delayms="));
		}
		else
		{
			/* Unrecognized, assume we're looking at traces now */
			break;
		}
	}

	if (i == argc)
	{
		const char *defaultName = "FNA3D_Trace.bin";
		const char *rootPath = SDL_GetBasePath();
		size_t pathLen = SDL_strlen(rootPath) + SDL_strlen(defaultName) + 1;
		char *path = (char*) SDL_malloc(pathLen);
		SDL_snprintf(path, pathLen, "%s%s", rootPath, defaultName);
#ifndef USE_SDL3
		SDL_free(rootPath);
#endif
		replay(path, forceDebugMode, vsync, forceFullscreen, delayMS);
		SDL_free(path);
	}
	else
	{
		for (; i < argc; i += 1)
		{
			if (replay(argv[i], forceDebugMode, vsync, forceFullscreen, delayMS))
			{
				break;
			}
		}
	}

	SDL_Quit();
	return 0;
}
