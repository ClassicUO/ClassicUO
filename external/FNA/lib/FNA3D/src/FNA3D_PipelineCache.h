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

#ifndef FNA3D_PIPELINECACHE_H
#define FNA3D_PIPELINECACHE_H

#include "FNA3D_Driver.h"

/* Packed Pipeline States */

typedef struct PackedState
{
	uint64_t a;
	uint64_t b;
} PackedState;

typedef struct PackedStateMap
{
	PackedState key;
	void* value;
} PackedStateMap;

typedef struct PackedStateArray
{
	PackedStateMap *elements;
	int32_t count;
	int32_t capacity;
} PackedStateArray;

FNA3D_SHAREDINTERNAL PackedState GetPackedBlendState(FNA3D_BlendState blendState);
FNA3D_SHAREDINTERNAL PackedState GetPackedDepthStencilState(FNA3D_DepthStencilState dsState);
FNA3D_SHAREDINTERNAL PackedState GetPackedRasterizerState(FNA3D_RasterizerState rastState, float bias);
FNA3D_SHAREDINTERNAL PackedState GetPackedSamplerState(FNA3D_SamplerState samplerState);
FNA3D_SHAREDINTERNAL void* PackedStateArray_Fetch(PackedStateArray arr, PackedState key);
FNA3D_SHAREDINTERNAL void PackedStateArray_Insert(PackedStateArray *arr, PackedState key, void* value);

/* Vertex Buffer Bindings */

typedef struct PackedVertexBufferBindings
{
	void* vertexShader;
	uint32_t hash;
} PackedVertexBufferBindings;

typedef struct PackedVertexBufferBindingsMap
{
	PackedVertexBufferBindings key;
	void* value;
} PackedVertexBufferBindingsMap;

/* FIXME: Can we make this common to both packed and vertex structs? */
typedef struct VertexBufferBindingsArray
{
	PackedVertexBufferBindingsMap *elements;
	int32_t count;
	int32_t capacity;
} PackedVertexBufferBindingsArray;

FNA3D_SHAREDINTERNAL void* PackedVertexBufferBindingsArray_Fetch(
	PackedVertexBufferBindingsArray arr,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	void* vertexShader,
	int32_t *outIndex,
	uint32_t *outHash
);
FNA3D_SHAREDINTERNAL void PackedVertexBufferBindingsArray_Insert(
	PackedVertexBufferBindingsArray *arr,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	void* vertexShader,
	void* value
);

/* Macros */

#define EXPAND_ARRAY_IF_NEEDED(arr, initialValue, type)	\
	if (arr->count == arr->capacity)		\
	{						\
		if (arr->capacity == 0)			\
		{					\
			arr->capacity = initialValue;	\
		}					\
		else					\
		{					\
			arr->capacity *= 2;		\
		}					\
		arr->elements = (type*) SDL_realloc(	\
			arr->elements,			\
			arr->capacity * sizeof(type)	\
		);					\
	}

#endif /* FNA3D_PIPELINECACHE_H */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
