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

#include "FNA3D.h"

void FNA3D_Trace_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
);

void FNA3D_Trace_DestroyDevice(void);

void FNA3D_Trace_SwapBuffers(
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
);

void FNA3D_Trace_Clear(
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
);

void FNA3D_Trace_DrawIndexedPrimitives(
	FNA3D_PrimitiveType primitiveType,
	int32_t baseVertex,
	int32_t minVertexIndex,
	int32_t numVertices,
	int32_t startIndex,
	int32_t primitiveCount,
	FNA3D_Buffer *indices,
	FNA3D_IndexElementSize indexElementSize
);

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
);

void FNA3D_Trace_DrawPrimitives(
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
);

void FNA3D_Trace_SetViewport(FNA3D_Viewport *viewport);

void FNA3D_Trace_SetScissorRect(FNA3D_Rect *scissor);

void FNA3D_Trace_SetBlendFactor(
	FNA3D_Color *blendFactor
);

void FNA3D_Trace_SetMultiSampleMask(int32_t mask);

void FNA3D_Trace_SetReferenceStencil(int32_t ref);

void FNA3D_Trace_SetBlendState(
	FNA3D_BlendState *blendState
);

void FNA3D_Trace_SetDepthStencilState(
	FNA3D_DepthStencilState *depthStencilState
);

void FNA3D_Trace_ApplyRasterizerState(
	FNA3D_RasterizerState *rasterizerState
);

void FNA3D_Trace_VerifySampler(
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
);

void FNA3D_Trace_VerifyVertexSampler(
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
);

void FNA3D_Trace_ApplyVertexBufferBindings(
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
);

void FNA3D_Trace_SetRenderTargets(
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
);

void FNA3D_Trace_ResolveTarget(
	FNA3D_RenderTargetBinding *target
);

void FNA3D_Trace_ResetBackbuffer(
	FNA3D_PresentationParameters *presentationParameters
);

void FNA3D_Trace_ReadBackbuffer(
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t dataLength
);

void FNA3D_Trace_CreateTexture2D(
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget,
	FNA3D_Texture *retval
);

void FNA3D_Trace_CreateTexture3D(
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount,
	FNA3D_Texture *retval
);

void FNA3D_Trace_CreateTextureCube(
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget,
	FNA3D_Texture *retval
);

void FNA3D_Trace_AddDisposeTexture(
	FNA3D_Texture *texture
);

void FNA3D_Trace_SetTextureData2D(
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	void* data,
	int32_t dataLength
);

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
);

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
);

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
);

void FNA3D_Trace_GetTextureData2D(
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	int32_t dataLength
);

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
);

void FNA3D_Trace_GetTextureDataCube(
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	FNA3D_CubeMapFace cubeMapFace,
	int32_t level,
	int32_t dataLength
);

void FNA3D_Trace_GenColorRenderbuffer(
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture,
	FNA3D_Renderbuffer *retval
);

void FNA3D_Trace_GenDepthStencilRenderbuffer(
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount,
	FNA3D_Renderbuffer *retval
);

void FNA3D_Trace_AddDisposeRenderbuffer(
	FNA3D_Renderbuffer *renderbuffer
);

void FNA3D_Trace_GenVertexBuffer(
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes,
	FNA3D_Buffer *retval
);

void FNA3D_Trace_AddDisposeVertexBuffer(
	FNA3D_Buffer *buffer
);

void FNA3D_Trace_SetVertexBufferData(
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
);

void FNA3D_Trace_GetVertexBufferData(
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
);

void FNA3D_Trace_GenIndexBuffer(
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes,
	FNA3D_Buffer *retval
);

void FNA3D_Trace_AddDisposeIndexBuffer(
	FNA3D_Buffer *buffer
);

void FNA3D_Trace_SetIndexBufferData(
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
);

void FNA3D_Trace_GetIndexBufferData(
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	int32_t dataLength
);

void FNA3D_Trace_CreateEffect(
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect *retval,
	MOJOSHADER_effect *retvalData
);

void FNA3D_Trace_CloneEffect(
	FNA3D_Effect *cloneSource,
	FNA3D_Effect *retval,
	MOJOSHADER_effect *retvalData
);

void FNA3D_Trace_AddDisposeEffect(
	FNA3D_Effect *effect
);

void FNA3D_Trace_SetEffectTechnique(
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
);

void FNA3D_Trace_ApplyEffect(
	FNA3D_Effect *effect,
	uint32_t pass
);

void FNA3D_Trace_BeginPassRestore(
	FNA3D_Effect *effect
);

void FNA3D_Trace_EndPassRestore(
	FNA3D_Effect *effect
);

void FNA3D_Trace_CreateQuery(FNA3D_Query *retval);

void FNA3D_Trace_AddDisposeQuery(FNA3D_Query *query);

void FNA3D_Trace_QueryBegin(FNA3D_Query *query);

void FNA3D_Trace_QueryEnd(FNA3D_Query *query);

void FNA3D_Trace_QueryPixelCount(
	FNA3D_Query *query
);

void FNA3D_Trace_SetStringMarker(const char *text);

void FNA3D_Trace_SetTextureName(void *texture, const char *text);

#define TRACE_CREATEDEVICE FNA3D_Trace_CreateDevice(presentationParameters, debugMode);
#define TRACE_DESTROYDEVICE FNA3D_Trace_DestroyDevice();
#define TRACE_SWAPBUFFERS FNA3D_Trace_SwapBuffers(sourceRectangle, destinationRectangle, overrideWindowHandle);
#define TRACE_CLEAR FNA3D_Trace_Clear(options, color, depth, stencil);
#define TRACE_DRAWINDEXEDPRIMITIVES FNA3D_Trace_DrawIndexedPrimitives(primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount, indices, indexElementSize);
#define TRACE_DRAWINSTANCEDPRIMITIVES FNA3D_Trace_DrawInstancedPrimitives(primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount, instanceCount, indices, indexElementSize);
#define TRACE_DRAWPRIMITIVES FNA3D_Trace_DrawPrimitives(primitiveType, vertexStart, primitiveCount);
#define TRACE_SETVIEWPORT FNA3D_Trace_SetViewport(viewport);
#define TRACE_SETSCISSORRECT FNA3D_Trace_SetScissorRect(scissor);
#define TRACE_SETBLENDFACTOR FNA3D_Trace_SetBlendFactor(blendFactor);
#define TRACE_SETMULTISAMPLEMASK FNA3D_Trace_SetMultiSampleMask(mask);
#define TRACE_SETREFERENCESTENCIL FNA3D_Trace_SetReferenceStencil(ref);
#define TRACE_SETBLENDSTATE FNA3D_Trace_SetBlendState(blendState);
#define TRACE_SETDEPTHSTENCILSTATE FNA3D_Trace_SetDepthStencilState(depthStencilState);
#define TRACE_APPLYRASTERIZERSTATE FNA3D_Trace_ApplyRasterizerState(rasterizerState);
#define TRACE_VERIFYSAMPLER FNA3D_Trace_VerifySampler(index, texture, sampler);
#define TRACE_VERIFYVERTEXSAMPLER FNA3D_Trace_VerifyVertexSampler(index, texture, sampler);
#define TRACE_APPLYVERTEXBUFFERBINDINGS FNA3D_Trace_ApplyVertexBufferBindings(bindings, numBindings, bindingsUpdated, baseVertex);
#define TRACE_SETRENDERTARGETS FNA3D_Trace_SetRenderTargets(renderTargets, numRenderTargets, depthStencilBuffer, depthFormat, preserveTargetContents);
#define TRACE_RESOLVETARGET FNA3D_Trace_ResolveTarget(target);
#define TRACE_RESETBACKBUFFER FNA3D_Trace_ResetBackbuffer(presentationParameters);
#define TRACE_READBACKBUFFER FNA3D_Trace_ReadBackbuffer(x, y, w, h, dataLength);
#define TRACE_CREATETEXTURE2D FNA3D_Trace_CreateTexture2D(format, width, height, levelCount, isRenderTarget, result);
#define TRACE_CREATETEXTURE3D FNA3D_Trace_CreateTexture3D(format, width, height, depth, levelCount, result);
#define TRACE_CREATETEXTURECUBE FNA3D_Trace_CreateTextureCube(format, size, levelCount, isRenderTarget, result);
#define TRACE_ADDDISPOSETEXTURE FNA3D_Trace_AddDisposeTexture(texture);
#define TRACE_SETTEXTUREDATA2D FNA3D_Trace_SetTextureData2D(texture, x,  y, w, h, level, data, dataLength);
#define TRACE_SETTEXTUREDATA3D FNA3D_Trace_SetTextureData3D(texture, x, y, z, w, h, d, level, data, dataLength);
#define TRACE_SETTEXTUREDATACUBE FNA3D_Trace_SetTextureDataCube(texture, x, y, w, h, cubeMapFace, level, data, dataLength);
#define TRACE_SETTEXTUREDATAYUV FNA3D_Trace_SetTextureDataYUV(y, u, v, yWidth, yHeight, uvWidth, uvHeight, data, dataLength);
#define TRACE_GETTEXTUREDATA2D FNA3D_Trace_GetTextureData2D(texture, x, y, w, h, level, dataLength);
#define TRACE_GETTEXTUREDATA3D FNA3D_Trace_GetTextureData3D(texture, x, y, z, w, h, d, level, dataLength);
#define TRACE_GETTEXTUREDATACUBE FNA3D_Trace_GetTextureDataCube(texture, x, y, w, h, cubeMapFace, level, dataLength);
#define TRACE_GENCOLORRENDERBUFFER FNA3D_Trace_GenColorRenderbuffer(width, height, format, multiSampleCount, texture, result);
#define TRACE_GENDEPTHSTENCILRENDERBUFFER FNA3D_Trace_GenDepthStencilRenderbuffer(width, height, format, multiSampleCount, result);
#define TRACE_ADDDISPOSERENDERBUFFER FNA3D_Trace_AddDisposeRenderbuffer(renderbuffer);
#define TRACE_GENVERTEXBUFFER FNA3D_Trace_GenVertexBuffer(dynamic, usage, sizeInBytes, result);
#define TRACE_ADDDISPOSEVERTEXBUFFER FNA3D_Trace_AddDisposeVertexBuffer(buffer);
#define TRACE_SETVERTEXBUFFERDATA FNA3D_Trace_SetVertexBufferData(buffer, offsetInBytes, data, elementCount, elementSizeInBytes, vertexStride, options);
#define TRACE_GETVERTEXBUFFERDATA FNA3D_Trace_GetVertexBufferData(buffer, offsetInBytes, elementCount, elementSizeInBytes, vertexStride);
#define TRACE_GENINDEXBUFFER FNA3D_Trace_GenIndexBuffer(dynamic, usage, sizeInBytes, result);
#define TRACE_ADDDISPOSEINDEXBUFFER FNA3D_Trace_AddDisposeIndexBuffer(buffer);
#define TRACE_SETINDEXBUFFERDATA FNA3D_Trace_SetIndexBufferData(buffer, offsetInBytes, data, dataLength, options);
#define TRACE_GETINDEXBUFFERDATA FNA3D_Trace_GetIndexBufferData(buffer, offsetInBytes, dataLength);
#define TRACE_CREATEEFFECT FNA3D_Trace_CreateEffect(effectCode, effectCodeLength, *effect, *effectData);
#define TRACE_CLONEEFFECT FNA3D_Trace_CloneEffect(cloneSource, *effect, *effectData);
#define TRACE_ADDDISPOSEEFFECT FNA3D_Trace_AddDisposeEffect(effect);
#define TRACE_SETEFFECTTECHNIQUE FNA3D_Trace_SetEffectTechnique(effect, technique);
#define TRACE_APPLYEFFECT FNA3D_Trace_ApplyEffect(effect, pass);
#define TRACE_BEGINPASSRESTORE FNA3D_Trace_BeginPassRestore(effect);
#define TRACE_ENDPASSRESTORE FNA3D_Trace_EndPassRestore(effect);
#define TRACE_CREATEQUERY FNA3D_Trace_CreateQuery(result);
#define TRACE_ADDDISPOSEQUERY FNA3D_Trace_AddDisposeQuery(query);
#define TRACE_QUERYBEGIN FNA3D_Trace_QueryBegin(query);
#define TRACE_QUERYEND FNA3D_Trace_QueryEnd(query);
#define TRACE_QUERYPIXELCOUNT FNA3D_Trace_QueryPixelCount(query);
#define TRACE_SETSTRINGMARKER FNA3D_Trace_SetStringMarker(text);
#define TRACE_SETTEXTURENAME FNA3D_Trace_SetTextureName(texture, text);

#else

#define TRACE_CREATEDEVICE
#define TRACE_DESTROYDEVICE
#define TRACE_SWAPBUFFERS
#define TRACE_CLEAR
#define TRACE_DRAWINDEXEDPRIMITIVES
#define TRACE_DRAWINSTANCEDPRIMITIVES
#define TRACE_DRAWPRIMITIVES
#define TRACE_SETVIEWPORT
#define TRACE_SETSCISSORRECT
#define TRACE_SETBLENDFACTOR
#define TRACE_SETMULTISAMPLEMASK
#define TRACE_SETREFERENCESTENCIL
#define TRACE_SETBLENDSTATE
#define TRACE_SETDEPTHSTENCILSTATE
#define TRACE_APPLYRASTERIZERSTATE
#define TRACE_VERIFYSAMPLER
#define TRACE_VERIFYVERTEXSAMPLER
#define TRACE_APPLYVERTEXBUFFERBINDINGS
#define TRACE_SETRENDERTARGETS
#define TRACE_RESOLVETARGET
#define TRACE_RESETBACKBUFFER
#define TRACE_READBACKBUFFER
#define TRACE_CREATETEXTURE2D
#define TRACE_CREATETEXTURE3D
#define TRACE_CREATETEXTURECUBE
#define TRACE_ADDDISPOSETEXTURE
#define TRACE_SETTEXTUREDATA2D
#define TRACE_SETTEXTUREDATA3D
#define TRACE_SETTEXTUREDATACUBE
#define TRACE_SETTEXTUREDATAYUV
#define TRACE_GETTEXTUREDATA2D
#define TRACE_GETTEXTUREDATA3D
#define TRACE_GETTEXTUREDATACUBE
#define TRACE_GENCOLORRENDERBUFFER
#define TRACE_GENDEPTHSTENCILRENDERBUFFER
#define TRACE_ADDDISPOSERENDERBUFFER
#define TRACE_GENVERTEXBUFFER
#define TRACE_ADDDISPOSEVERTEXBUFFER
#define TRACE_SETVERTEXBUFFERDATA
#define TRACE_GETVERTEXBUFFERDATA
#define TRACE_GENINDEXBUFFER
#define TRACE_ADDDISPOSEINDEXBUFFER
#define TRACE_SETINDEXBUFFERDATA
#define TRACE_GETINDEXBUFFERDATA
#define TRACE_CREATEEFFECT
#define TRACE_CLONEEFFECT
#define TRACE_ADDDISPOSEEFFECT
#define TRACE_SETEFFECTTECHNIQUE
#define TRACE_APPLYEFFECT
#define TRACE_BEGINPASSRESTORE
#define TRACE_ENDPASSRESTORE
#define TRACE_CREATEQUERY
#define TRACE_ADDDISPOSEQUERY
#define TRACE_QUERYBEGIN
#define TRACE_QUERYEND
#define TRACE_QUERYPIXELCOUNT
#define TRACE_SETSTRINGMARKER
#define TRACE_SETTEXTURENAME

#endif /* FNA3D_TRACING */
