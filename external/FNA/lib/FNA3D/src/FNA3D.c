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

#include "FNA3D_Driver.h"
#include "FNA3D_Tracing.h"

#ifdef USE_SDL3
#include <SDL3/SDL.h>
#else
#include <SDL.h>
#endif

#if !SDL_VERSION_ATLEAST(2, 26, 0)
#error "SDL version older than 2.26.0"
#endif /* !SDL_VERSION_ATLEAST */

/* Drivers */

static const FNA3D_Driver *drivers[] = {
#if FNA3D_DRIVER_SDL
	&SDLGPUDriver,
#endif
#if FNA3D_DRIVER_D3D11
	&D3D11Driver,
#endif
#if FNA3D_DRIVER_OPENGL
	&OpenGLDriver,
#endif
	NULL
};

/* Logging */

static void FNA3D_Default_LogInfo(const char *msg)
{
	SDL_LogInfo(
		SDL_LOG_CATEGORY_APPLICATION,
		"%s",
		msg
	);
}

static void FNA3D_Default_LogWarn(const char *msg)
{
	SDL_LogWarn(
		SDL_LOG_CATEGORY_APPLICATION,
		"%s",
		msg
	);
}

static void FNA3D_Default_LogError(const char *msg)
{
	SDL_LogError(
		SDL_LOG_CATEGORY_APPLICATION,
		"%s",
		msg
	);
}

static FNA3D_LogFunc FNA3D_LogInfoFunc = FNA3D_Default_LogInfo;
static FNA3D_LogFunc FNA3D_LogWarnFunc = FNA3D_Default_LogWarn;
static FNA3D_LogFunc FNA3D_LogErrorFunc = FNA3D_Default_LogError;

#define MAX_MESSAGE_SIZE 1024

void FNA3D_LogInfo(const char *fmt, ...)
{
	char msg[MAX_MESSAGE_SIZE];
	va_list ap;
	va_start(ap, fmt);
	SDL_vsnprintf(msg, sizeof(msg), fmt, ap);
	va_end(ap);
	FNA3D_LogInfoFunc(msg);
}

void FNA3D_LogWarn(const char *fmt, ...)
{
	char msg[MAX_MESSAGE_SIZE];
	va_list ap;
	va_start(ap, fmt);
	SDL_vsnprintf(msg, sizeof(msg), fmt, ap);
	va_end(ap);
	FNA3D_LogWarnFunc(msg);
}

void FNA3D_LogError(const char *fmt, ...)
{
	char msg[MAX_MESSAGE_SIZE];
	va_list ap;
	va_start(ap, fmt);
	SDL_vsnprintf(msg, sizeof(msg), fmt, ap);
	va_end(ap);
	FNA3D_LogErrorFunc(msg);
}

#undef MAX_MESSAGE_SIZE

void FNA3D_HookLogFunctions(
	FNA3D_LogFunc info,
	FNA3D_LogFunc warn,
	FNA3D_LogFunc error
) {
	FNA3D_LogInfoFunc = info;
	FNA3D_LogWarnFunc = warn;
	FNA3D_LogErrorFunc = error;
}

/* Version API */

uint32_t FNA3D_LinkedVersion(void)
{
	return FNA3D_COMPILED_VERSION;
}

/* Driver Functions */

static int32_t selectedDriver = -1;

uint32_t FNA3D_PrepareWindowAttributes(void)
{
	uint32_t result = 0;
	uint32_t i;
	const char *hint = SDL_GetHint("FNA3D_FORCE_DRIVER");
	const char *gpuhint;

	/* We used to have our own Vulkan renderer, but that work is now in SDL
	 * instead. For maximum compatibility, alias this to SDL_GPU!
	 *
	 * And hey, since we're here, let's do this for D3D12/Metal too.
	 * -flibit
	 */
#ifdef USE_SDL3
	if (hint != NULL)
	{
		gpuhint = NULL;
		if (SDL_strcasecmp(hint, "Vulkan") == 0)
		{
#ifdef __APPLE__
			/* We were using MoltenVK anyway, so just skip the middle man */
			gpuhint = "metal";
#else
			gpuhint = "vulkan";
#endif
		}
		else if (SDL_strcasecmp(hint, "D3D12") == 0)
		{
			gpuhint = "direct3d12";
		}
		else if (SDL_strcasecmp(hint, "Metal") == 0)
		{
			gpuhint = "metal";
		}

		if (gpuhint != NULL)
		{
			hint = "SDLGPU";
			SDL_SetHintWithPriority(
				SDL_HINT_GPU_DRIVER,
				gpuhint,
				SDL_HINT_OVERRIDE
			);
		}
	}
#endif

	for (i = 0; drivers[i] != NULL; i += 1)
	{
		if (hint != NULL)
		{
			if (SDL_strcasecmp(hint, drivers[i]->Name) != 0)
			{
				continue;
			}
		}
		if (drivers[i]->PrepareWindowAttributes(&result))
		{
			break;
		}
	}
	if (drivers[i] == NULL)
	{
		FNA3D_LogError("No supported FNA3D driver found!");
	}
	else
	{
		selectedDriver = i;
	}
	return result;
}

FNA3DAPI void FNA3D_GetDrawableSize(void* window, int32_t *w, int32_t *h)
{
	SDL_GetWindowSizeInPixels((SDL_Window*) window, w, h);
}

/* Init/Quit */

FNA3D_Device* FNA3D_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
) {
	TRACE_CREATEDEVICE
	if (selectedDriver < 0)
	{
		FNA3D_LogError("Call FNA3D_PrepareWindowAttributes first!");
		return NULL;
	}

	return drivers[selectedDriver]->CreateDevice(
		presentationParameters,
		debugMode
	);
}

void FNA3D_DestroyDevice(FNA3D_Device *device)
{
	TRACE_DESTROYDEVICE
	if (device == NULL)
	{
		return;
	}

	device->DestroyDevice(device);
}

/* Presentation */

void FNA3D_SwapBuffers(
	FNA3D_Device *device,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
) {
	TRACE_SWAPBUFFERS
	if (device == NULL)
	{
		return;
	}
	device->SwapBuffers(
		device->driverData,
		sourceRectangle,
		destinationRectangle,
		overrideWindowHandle
	);
}

/* Drawing */

void FNA3D_Clear(
	FNA3D_Device *device,
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
) {
	TRACE_CLEAR
	if (device == NULL)
	{
		return;
	}
	device->Clear(device->driverData, options, color, depth, stencil);
}

void FNA3D_DrawIndexedPrimitives(
	FNA3D_Device *device,
	FNA3D_PrimitiveType primitiveType,
	int32_t baseVertex,
	int32_t minVertexIndex,
	int32_t numVertices,
	int32_t startIndex,
	int32_t primitiveCount,
	FNA3D_Buffer *indices,
	FNA3D_IndexElementSize indexElementSize
) {
	TRACE_DRAWINDEXEDPRIMITIVES
	if (device == NULL)
	{
		return;
	}
	device->DrawIndexedPrimitives(
		device->driverData,
		primitiveType,
		baseVertex,
		minVertexIndex,
		numVertices,
		startIndex,
		primitiveCount,
		indices,
		indexElementSize
	);
}

void FNA3D_DrawInstancedPrimitives(
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
) {
	TRACE_DRAWINSTANCEDPRIMITIVES
	if (device == NULL)
	{
		return;
	}
	device->DrawInstancedPrimitives(
		device->driverData,
		primitiveType,
		baseVertex,
		minVertexIndex,
		numVertices,
		startIndex,
		primitiveCount,
		instanceCount,
		indices,
		indexElementSize
	);
}

void FNA3D_DrawPrimitives(
	FNA3D_Device *device,
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
) {
	TRACE_DRAWPRIMITIVES
	if (device == NULL)
	{
		return;
	}
	device->DrawPrimitives(
		device->driverData,
		primitiveType,
		vertexStart,
		primitiveCount
	);
}

/* Mutable Render States */

void FNA3D_SetViewport(FNA3D_Device *device, FNA3D_Viewport *viewport)
{
	TRACE_SETVIEWPORT
	if (device == NULL)
	{
		return;
	}
	device->SetViewport(device->driverData, viewport);
}

void FNA3D_SetScissorRect(FNA3D_Device *device, FNA3D_Rect *scissor)
{
	TRACE_SETSCISSORRECT
	if (device == NULL)
	{
		return;
	}
	device->SetScissorRect(device->driverData, scissor);
}

void FNA3D_GetBlendFactor(
	FNA3D_Device *device,
	FNA3D_Color *blendFactor
) {
	/* Not traced! */
	if (device == NULL)
	{
		return;
	}
	device->GetBlendFactor(device->driverData, blendFactor);
}

void FNA3D_SetBlendFactor(
	FNA3D_Device *device,
	FNA3D_Color *blendFactor
) {
	TRACE_SETBLENDFACTOR
	if (device == NULL)
	{
		return;
	}
	device->SetBlendFactor(device->driverData, blendFactor);
}

int32_t FNA3D_GetMultiSampleMask(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->GetMultiSampleMask(device->driverData);
}

void FNA3D_SetMultiSampleMask(FNA3D_Device *device, int32_t mask)
{
	TRACE_SETMULTISAMPLEMASK
	if (device == NULL)
	{
		return;
	}
	device->SetMultiSampleMask(device->driverData, mask);
}

int32_t FNA3D_GetReferenceStencil(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->GetReferenceStencil(device->driverData);
}

void FNA3D_SetReferenceStencil(FNA3D_Device *device, int32_t ref)
{
	TRACE_SETREFERENCESTENCIL
	if (device == NULL)
	{
		return;
	}
	device->SetReferenceStencil(device->driverData, ref);
}

/* Immutable Render States */

void FNA3D_SetBlendState(
	FNA3D_Device *device,
	FNA3D_BlendState *blendState
) {
	TRACE_SETBLENDSTATE
	if (device == NULL)
	{
		return;
	}
	device->SetBlendState(device->driverData, blendState);
}

void FNA3D_SetDepthStencilState(
	FNA3D_Device *device,
	FNA3D_DepthStencilState *depthStencilState
) {
	TRACE_SETDEPTHSTENCILSTATE
	if (device == NULL)
	{
		return;
	}
	device->SetDepthStencilState(device->driverData, depthStencilState);
}

void FNA3D_ApplyRasterizerState(
	FNA3D_Device *device,
	FNA3D_RasterizerState *rasterizerState
) {
	TRACE_APPLYRASTERIZERSTATE
	if (device == NULL)
	{
		return;
	}
	device->ApplyRasterizerState(device->driverData, rasterizerState);
}

void FNA3D_VerifySampler(
	FNA3D_Device *device,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	TRACE_VERIFYSAMPLER
	if (device == NULL)
	{
		return;
	}
	device->VerifySampler(device->driverData, index, texture, sampler);
}

void FNA3D_VerifyVertexSampler(
	FNA3D_Device *device,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	TRACE_VERIFYVERTEXSAMPLER
	if (device == NULL)
	{
		return;
	}
	device->VerifyVertexSampler(device->driverData, index, texture, sampler);
}

void FNA3D_ApplyVertexBufferBindings(
	FNA3D_Device *device,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
) {
	TRACE_APPLYVERTEXBUFFERBINDINGS
	if (device == NULL)
	{
		return;
	}
	device->ApplyVertexBufferBindings(
		device->driverData,
		bindings,
		numBindings,
		bindingsUpdated,
		baseVertex
	);
}

/* Render Targets */

void FNA3D_SetRenderTargets(
	FNA3D_Device *device,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
) {
	TRACE_SETRENDERTARGETS
	if (device == NULL)
	{
		return;
	}
	device->SetRenderTargets(
		device->driverData,
		renderTargets,
		numRenderTargets,
		depthStencilBuffer,
		depthFormat,
		preserveTargetContents
	);
}

void FNA3D_ResolveTarget(
	FNA3D_Device *device,
	FNA3D_RenderTargetBinding *target
) {
	TRACE_RESOLVETARGET
	if (device == NULL)
	{
		return;
	}
	device->ResolveTarget(device->driverData, target);
}

/* Backbuffer Functions */

void FNA3D_ResetBackbuffer(
	FNA3D_Device *device,
	FNA3D_PresentationParameters *presentationParameters
) {
	TRACE_RESETBACKBUFFER
	if (device == NULL)
	{
		return;
	}
	device->ResetBackbuffer(device->driverData, presentationParameters);
}

void FNA3D_ReadBackbuffer(
	FNA3D_Device *device,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	void* data,
	int32_t dataLength
) {
	TRACE_READBACKBUFFER
	if (device == NULL)
	{
		return;
	}
	device->ReadBackbuffer(
		device->driverData,
		x,
		y,
		w,
		h,
		data,
		dataLength
	);
}

void FNA3D_GetBackbufferSize(
	FNA3D_Device *device,
	int32_t *w,
	int32_t *h
) {
	/* Not traced! */
	if (device == NULL)
	{
		*w = 0;
		*h = 0;
		return;
	}
	device->GetBackbufferSize(device->driverData, w, h);
}

FNA3D_SurfaceFormat FNA3D_GetBackbufferSurfaceFormat(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return FNA3D_SURFACEFORMAT_COLOR;
	}
	return device->GetBackbufferSurfaceFormat(device->driverData);
}

FNA3D_DepthFormat FNA3D_GetBackbufferDepthFormat(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return FNA3D_DEPTHFORMAT_NONE;
	}
	return device->GetBackbufferDepthFormat(device->driverData);
}

int32_t FNA3D_GetBackbufferMultiSampleCount(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->GetBackbufferMultiSampleCount(device->driverData);
}

/* Textures */

FNA3D_Texture* FNA3D_CreateTexture2D(
	FNA3D_Device *device,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	FNA3D_Texture *result;
	if (device == NULL)
	{
		return NULL;
	}
	result = device->CreateTexture2D(
		device->driverData,
		format,
		width,
		height,
		levelCount,
		isRenderTarget
	);
	TRACE_CREATETEXTURE2D
	return result;
}

FNA3D_Texture* FNA3D_CreateTexture3D(
	FNA3D_Device *device,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	FNA3D_Texture *result;
	if (device == NULL)
	{
		return NULL;
	}
	result = device->CreateTexture3D(
		device->driverData,
		format,
		width,
		height,
		depth,
		levelCount
	);
	TRACE_CREATETEXTURE3D
	return result;
}

FNA3D_Texture* FNA3D_CreateTextureCube(
	FNA3D_Device *device,
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	FNA3D_Texture *result;
	if (device == NULL)
	{
		return NULL;
	}
	result = device->CreateTextureCube(
		device->driverData,
		format,
		size,
		levelCount,
		isRenderTarget
	);
	TRACE_CREATETEXTURECUBE
	return result;
}

void FNA3D_AddDisposeTexture(
	FNA3D_Device *device,
	FNA3D_Texture *texture
) {
	TRACE_ADDDISPOSETEXTURE
	if (device == NULL || texture == NULL)
	{
		return;
	}
	device->AddDisposeTexture(device->driverData, texture);
}

void FNA3D_SetTextureData2D(
	FNA3D_Device *device,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	TRACE_SETTEXTUREDATA2D
	if (device == NULL)
	{
		return;
	}
	device->SetTextureData2D(
		device->driverData,
		texture,
		x,
		y,
		w,
		h,
		level,
		data,
		dataLength
	);
}

void FNA3D_SetTextureData3D(
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
) {
	TRACE_SETTEXTUREDATA3D
	if (device == NULL)
	{
		return;
	}
	device->SetTextureData3D(
		device->driverData,
		texture,
		x,
		y,
		z,
		w,
		h,
		d,
		level,
		data,
		dataLength
	);
}

void FNA3D_SetTextureDataCube(
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
) {
	TRACE_SETTEXTUREDATACUBE
	if (device == NULL)
	{
		return;
	}
	device->SetTextureDataCube(
		device->driverData,
		texture,
		x,
		y,
		w,
		h,
		cubeMapFace,
		level,
		data,
		dataLength
	);
}

void FNA3D_SetTextureDataYUV(
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
) {
	TRACE_SETTEXTUREDATAYUV
	if (device == NULL)
	{
		return;
	}
	device->SetTextureDataYUV(
		device->driverData,
		y,
		u,
		v,
		yWidth,
		yHeight,
		uvWidth,
		uvHeight,
		data,
		dataLength
	);
}

void FNA3D_GetTextureData2D(
	FNA3D_Device *device,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
) {
	TRACE_GETTEXTUREDATA2D
	if (device == NULL)
	{
		return;
	}
	device->GetTextureData2D(
		device->driverData,
		texture,
		x,
		y,
		w,
		h,
		level,
		data,
		dataLength
	);
}

void FNA3D_GetTextureData3D(
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
) {
	TRACE_SETTEXTUREDATA3D
	if (device == NULL)
	{
		return;
	}
	device->GetTextureData3D(
		device->driverData,
		texture,
		x,
		y,
		z,
		w,
		h,
		d,
		level,
		data,
		dataLength
	);
}

void FNA3D_GetTextureDataCube(
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
) {
	TRACE_GETTEXTUREDATACUBE
	if (device == NULL)
	{
		return;
	}
	device->GetTextureDataCube(
		device->driverData,
		texture,
		x,
		y,
		w,
		h,
		cubeMapFace,
		level,
		data,
		dataLength
	);
}

/* Renderbuffers */

FNA3D_Renderbuffer* FNA3D_GenColorRenderbuffer(
	FNA3D_Device *device,
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	FNA3D_Renderbuffer *result;
	if (device == NULL)
	{
		return NULL;
	}
	result = device->GenColorRenderbuffer(
		device->driverData,
		width,
		height,
		format,
		multiSampleCount,
		texture
	);
	TRACE_GENCOLORRENDERBUFFER
	return result;
}

FNA3D_Renderbuffer* FNA3D_GenDepthStencilRenderbuffer(
	FNA3D_Device *device,
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	FNA3D_Renderbuffer *result;
	if (device == NULL)
	{
		return NULL;
	}
	result = device->GenDepthStencilRenderbuffer(
		device->driverData,
		width,
		height,
		format,
		multiSampleCount
	);
	TRACE_GENDEPTHSTENCILRENDERBUFFER
	return result;
}

void FNA3D_AddDisposeRenderbuffer(
	FNA3D_Device *device,
	FNA3D_Renderbuffer *renderbuffer
) {
	TRACE_ADDDISPOSERENDERBUFFER
	if (device == NULL || renderbuffer == NULL)
	{
		return;
	}
	device->AddDisposeRenderbuffer(
		device->driverData,
		renderbuffer
	);
}

/* Vertex Buffers */

FNA3D_Buffer* FNA3D_GenVertexBuffer(
	FNA3D_Device *device,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	FNA3D_Buffer *result;
	if (device == NULL)
	{
		return NULL;
	}
	result = device->GenVertexBuffer(
		device->driverData,
		dynamic,
		usage,
		sizeInBytes
	);
	TRACE_GENVERTEXBUFFER
	return result;
}

void FNA3D_AddDisposeVertexBuffer(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer
) {
	TRACE_ADDDISPOSEVERTEXBUFFER
	if (device == NULL || buffer == NULL)
	{
		return;
	}
	device->AddDisposeVertexBuffer(device->driverData, buffer);
}

void FNA3D_SetVertexBufferData(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
) {
	TRACE_SETVERTEXBUFFERDATA
	if (device == NULL)
	{
		return;
	}
	device->SetVertexBufferData(
		device->driverData,
		buffer,
		offsetInBytes,
		data,
		elementCount,
		elementSizeInBytes,
		vertexStride,
		options
	);
}

void FNA3D_GetVertexBufferData(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
) {
	TRACE_GETVERTEXBUFFERDATA
	if (device == NULL)
	{
		return;
	}
	device->GetVertexBufferData(
		device->driverData,
		buffer,
		offsetInBytes,
		data,
		elementCount,
		elementSizeInBytes,
		vertexStride
	);
}

/* Index Buffers */

FNA3D_Buffer* FNA3D_GenIndexBuffer(
	FNA3D_Device *device,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	FNA3D_Buffer *result;
	if (device == NULL)
	{
		return NULL;
	}
	result = device->GenIndexBuffer(
		device->driverData,
		dynamic,
		usage,
		sizeInBytes
	);
	TRACE_GENINDEXBUFFER
	return result;
}

void FNA3D_AddDisposeIndexBuffer(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer
) {
	TRACE_ADDDISPOSEINDEXBUFFER
	if (device == NULL || buffer == NULL)
	{
		return;
	}
	device->AddDisposeIndexBuffer(device->driverData, buffer);
}

void FNA3D_SetIndexBufferData(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	TRACE_SETINDEXBUFFERDATA
	if (device == NULL)
	{
		return;
	}
	device->SetIndexBufferData(
		device->driverData,
		buffer,
		offsetInBytes,
		data,
		dataLength,
		options
	);
}

void FNA3D_GetIndexBufferData(
	FNA3D_Device *device,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength
) {
	TRACE_GETINDEXBUFFERDATA
	if (device == NULL)
	{
		return;
	}
	device->GetIndexBufferData(
		device->driverData,
		buffer,
		offsetInBytes,
		data,
		dataLength
	);
}

/* Effects */

void FNA3D_CreateEffect(
	FNA3D_Device *device,
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	if (device == NULL)
	{
		*effect = NULL;
		*effectData = NULL;
		return;
	}
	device->CreateEffect(
		device->driverData,
		effectCode,
		effectCodeLength,
		effect,
		effectData
	);
	TRACE_CREATEEFFECT
}

void FNA3D_CloneEffect(
	FNA3D_Device *device,
	FNA3D_Effect *cloneSource,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	if (device == NULL)
	{
		*effect = NULL;
		*effectData = NULL;
		return;
	}
	device->CloneEffect(
		device->driverData,
		cloneSource,
		effect,
		effectData
	);
	TRACE_CLONEEFFECT
}

void FNA3D_AddDisposeEffect(
	FNA3D_Device *device,
	FNA3D_Effect *effect
) {
	TRACE_ADDDISPOSEEFFECT
	if (device == NULL || effect == NULL)
	{
		return;
	}
	device->AddDisposeEffect(device->driverData, effect);
}

void FNA3D_SetEffectTechnique(
	FNA3D_Device *device,
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
) {
	TRACE_SETEFFECTTECHNIQUE
	if (device == NULL)
	{
		return;
	}
	device->SetEffectTechnique(device->driverData, effect, technique);
}

void FNA3D_ApplyEffect(
	FNA3D_Device *device,
	FNA3D_Effect *effect,
	uint32_t pass,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	TRACE_APPLYEFFECT
	if (device == NULL)
	{
		return;
	}
	device->ApplyEffect(
		device->driverData,
		effect,
		pass,
		stateChanges
	);
}

void FNA3D_BeginPassRestore(
	FNA3D_Device *device,
	FNA3D_Effect *effect,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	TRACE_BEGINPASSRESTORE
	if (device == NULL)
	{
		return;
	}
	device->BeginPassRestore(
		device->driverData,
		effect,
		stateChanges
	);
}

void FNA3D_EndPassRestore(
	FNA3D_Device *device,
	FNA3D_Effect *effect
) {
	TRACE_ENDPASSRESTORE
	if (device == NULL)
	{
		return;
	}
	device->EndPassRestore(device->driverData, effect);
}

/* Queries */

FNA3D_Query* FNA3D_CreateQuery(FNA3D_Device *device)
{
	/* We're stuck tracing _after_ the call instead of _before_, because
	 * of threading issues. This can cause timing issues!
	 */
	FNA3D_Query *result;
	if (device == NULL)
	{
		return NULL;
	}
	result = device->CreateQuery(device->driverData);
	TRACE_CREATEQUERY
	return result;
}

void FNA3D_AddDisposeQuery(FNA3D_Device *device, FNA3D_Query *query)
{
	TRACE_ADDDISPOSEQUERY
	if (device == NULL || query == NULL)
	{
		return;
	}
	device->AddDisposeQuery(device->driverData, query);
}

void FNA3D_QueryBegin(FNA3D_Device *device, FNA3D_Query *query)
{
	TRACE_QUERYBEGIN
	if (device == NULL)
	{
		return;
	}
	device->QueryBegin(device->driverData, query);
}

void FNA3D_QueryEnd(FNA3D_Device *device, FNA3D_Query *query)
{
	TRACE_QUERYEND
	if (device == NULL)
	{
		return;
	}
	device->QueryEnd(device->driverData, query);
}

uint8_t FNA3D_QueryComplete(FNA3D_Device *device, FNA3D_Query *query)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 1;
	}
	return device->QueryComplete(device->driverData, query);
}

int32_t FNA3D_QueryPixelCount(
	FNA3D_Device *device,
	FNA3D_Query *query
) {
	TRACE_QUERYPIXELCOUNT
	if (device == NULL)
	{
		return 0;
	}
	return device->QueryPixelCount(device->driverData, query);
}

/* Feature Queries */

uint8_t FNA3D_SupportsDXT1(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->SupportsDXT1(device->driverData);
}

uint8_t FNA3D_SupportsS3TC(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->SupportsS3TC(device->driverData);
}

uint8_t FNA3D_SupportsBC7(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->SupportsBC7(device->driverData);
}

uint8_t FNA3D_SupportsHardwareInstancing(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->SupportsHardwareInstancing(device->driverData);
}

uint8_t FNA3D_SupportsNoOverwrite(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->SupportsNoOverwrite(device->driverData);
}


uint8_t FNA3D_SupportsSRGBRenderTargets(FNA3D_Device *device)
{
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->SupportsSRGBRenderTargets(device->driverData);
}

void FNA3D_GetMaxTextureSlots(
	FNA3D_Device *device,
	int32_t *textures,
	int32_t *vertexTextures
) {
	/* Not traced! */
	if (device == NULL)
	{
		return;
	}
	device->GetMaxTextureSlots(
		device->driverData,
		textures,
		vertexTextures
	);
}

int32_t FNA3D_GetMaxMultiSampleCount(
	FNA3D_Device *device,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount
) {
	/* Not traced! */
	if (device == NULL)
	{
		return 0;
	}
	return device->GetMaxMultiSampleCount(
		device->driverData,
		format,
		multiSampleCount
	);
}

/* Debugging */

void FNA3D_SetStringMarker(FNA3D_Device *device, const char *text)
{
	TRACE_SETSTRINGMARKER
	if (device == NULL)
	{
		return;
	}
	device->SetStringMarker(device->driverData, text);
}

void FNA3D_SetTextureName(FNA3D_Device* device, FNA3D_Texture* texture, const char* text)
{
	TRACE_SETTEXTURENAME
	if ((device == NULL) || (texture == NULL))
	{
		return;
	}

	SDL_assert(text);

	device->SetTextureName(device->driverData, texture, text);
}
/* External Interop */

void FNA3D_GetSysRendererEXT(
	FNA3D_Device *device,
	FNA3D_SysRendererEXT *sysrenderer
) {
#ifdef FNA3D_TRACING
	SDL_assert(0 && "Tracing does not support SysRendererEXT!");
#endif
	if (	device == NULL ||
		sysrenderer == NULL ||
		sysrenderer->version != FNA3D_SYSRENDERER_VERSION_EXT	)
	{
		return;
	}
	device->GetSysRenderer(
		device->driverData,
		sysrenderer
	);
}

FNA3D_Texture* FNA3D_CreateSysTextureEXT(
	FNA3D_Device *device,
	FNA3D_SysTextureEXT *systexture
) {
#ifdef FNA3D_TRACING
	SDL_assert(0 && "Tracing does not support SysTextureEXT!");
#endif
	if (	device == NULL ||
		systexture == NULL ||
		systexture->version != FNA3D_SYSRENDERER_VERSION_EXT	)
	{
		return NULL;
	}
	return device->CreateSysTexture(
		device->driverData,
		systexture
	);
}

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
