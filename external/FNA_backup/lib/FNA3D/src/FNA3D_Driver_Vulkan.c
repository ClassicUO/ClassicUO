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

#if FNA3D_DRIVER_VULKAN

/* Needed for VK_KHR_portability_subset */
#define VK_ENABLE_BETA_EXTENSIONS

#define VK_NO_PROTOTYPES
#include "vulkan/vulkan.h"

#include "FNA3D_Driver.h"
#include "FNA3D_PipelineCache.h"

#include <SDL.h>
#include <SDL_vulkan.h>

/* Global Vulkan Loader Entry Points */

static PFN_vkGetInstanceProcAddr vkGetInstanceProcAddr = NULL;

#define VULKAN_GLOBAL_FUNCTION(name) \
	static PFN_##name name = NULL;
#include "FNA3D_Driver_Vulkan_vkfuncs.h"

/* Vulkan Extensions */

typedef struct VulkanExtensions
{
	/* These extensions are required! */

	/* Globally supported */
	uint8_t KHR_swapchain;
	/* Core since 1.1 */
	uint8_t KHR_maintenance1;
	uint8_t KHR_dedicated_allocation;
	uint8_t KHR_get_memory_requirements2;

	/* These extensions are optional! */

	/* Core since 1.2, but requires annoying paperwork to implement */
	uint8_t KHR_driver_properties;
	/* EXT, probably not going to be Core */
	uint8_t EXT_vertex_attribute_divisor;
	/* Only required for special implementations (i.e. MoltenVK) */
	uint8_t KHR_portability_subset;
	/* Vendor-specific extensions */
	uint8_t GGP_frame_token;
} VulkanExtensions;

static inline uint8_t CheckDeviceExtensions(
	VkExtensionProperties *extensions,
	uint32_t numExtensions,
	VulkanExtensions *supports
) {
	uint32_t i;

	SDL_memset(supports, '\0', sizeof(VulkanExtensions));
	for (i = 0; i < numExtensions; i += 1)
	{
		const char *name = extensions[i].extensionName;
		#define CHECK(ext) \
			if (SDL_strcmp(name, "VK_" #ext) == 0) \
			{ \
				supports->ext = 1; \
			}
		CHECK(KHR_swapchain)
		else CHECK(KHR_maintenance1)
		else CHECK(KHR_dedicated_allocation)
		else CHECK(KHR_get_memory_requirements2)
		else CHECK(KHR_driver_properties)
		else CHECK(EXT_vertex_attribute_divisor)
		else CHECK(KHR_portability_subset)
		else CHECK(GGP_frame_token)
		#undef CHECK
	}

	return (	supports->KHR_swapchain &&
			supports->KHR_maintenance1 &&
			supports->KHR_dedicated_allocation &&
			supports->KHR_get_memory_requirements2	);
}

static inline uint32_t GetDeviceExtensionCount(VulkanExtensions *supports)
{
	return (
		supports->KHR_swapchain +
		supports->KHR_maintenance1 +
		supports->KHR_dedicated_allocation +
		supports->KHR_get_memory_requirements2 +
		supports->KHR_driver_properties +
		supports->EXT_vertex_attribute_divisor +
		supports->KHR_portability_subset +
		supports->GGP_frame_token
	);
}

static inline void CreateDeviceExtensionArray(
	VulkanExtensions *supports,
	const char **extensions
) {
	uint8_t cur = 0;
	#define CHECK(ext) \
		if (supports->ext) \
		{ \
			extensions[cur++] = "VK_" #ext; \
		}
	CHECK(KHR_swapchain)
	CHECK(KHR_maintenance1)
	CHECK(KHR_dedicated_allocation)
	CHECK(KHR_get_memory_requirements2)
	CHECK(KHR_driver_properties)
	CHECK(EXT_vertex_attribute_divisor)
	CHECK(KHR_portability_subset)
	CHECK(GGP_frame_token)
	#undef CHECK
}

/* Constants/Limits */

#define TEXTURE_COUNT MAX_TOTAL_SAMPLERS
#define MAX_MULTISAMPLE_MASK_SIZE 2
#define MAX_QUERIES 16
#define MAX_UNIFORM_DESCRIPTOR_SETS 1024
#define COMMAND_LIMIT 100
#define STARTING_ALLOCATION_SIZE 64000000 /* 64MB */
#define ALLOCATION_INCREMENT 16000000 /* 16MB */
#define MAX_ALLOCATION_SIZE 256000000 /* 256MB */
#define FAST_TEXTURE_STAGING_SIZE 64000000 /* 64MB */
#define STARTING_SLOW_TEXTURE_STAGING_SIZE 16000000 /* 16MB */
#define MAX_SLOW_TEXTURE_STAGING_SIZE 256000000 /* 256MB */

/* Should be equivalent to the number of values in FNA3D_PrimitiveType */
#define PRIMITIVE_TYPES_COUNT 5

#define STARTING_SAMPLER_DESCRIPTOR_POOL_SIZE 16
#define DESCRIPTOR_SET_DEACTIVATE_FRAMES 10

#define DEFAULT_PIPELINE_CACHE_FILE_NAME "FNA3D_Vulkan_PipelineCache.blob"

#define IDENTITY_SWIZZLE \
{ \
	VK_COMPONENT_SWIZZLE_IDENTITY, \
	VK_COMPONENT_SWIZZLE_IDENTITY, \
	VK_COMPONENT_SWIZZLE_IDENTITY, \
	VK_COMPONENT_SWIZZLE_IDENTITY \
}

static const VkComponentMapping RGBA_SWIZZLE =
{
	VK_COMPONENT_SWIZZLE_R,
	VK_COMPONENT_SWIZZLE_G,
	VK_COMPONENT_SWIZZLE_B,
	VK_COMPONENT_SWIZZLE_A
};

static const uint8_t DEVICE_PRIORITY[] =
{
	0,	/* VK_PHYSICAL_DEVICE_TYPE_OTHER */
	3,	/* VK_PHYSICAL_DEVICE_TYPE_INTEGRATED_GPU */
	4,	/* VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU */
	2,	/* VK_PHYSICAL_DEVICE_TYPE_VIRTUAL_GPU */
	1	/* VK_PHYSICAL_DEVICE_TYPE_CPU */
};

/* Enumerations */

typedef enum VulkanResourceAccessType
{
	/* Reads */
	RESOURCE_ACCESS_NONE, /* For initialization */
	RESOURCE_ACCESS_INDEX_BUFFER,
	RESOURCE_ACCESS_VERTEX_BUFFER,
	RESOURCE_ACCESS_VERTEX_SHADER_READ_UNIFORM_BUFFER,
	RESOURCE_ACCESS_VERTEX_SHADER_READ_SAMPLED_IMAGE,
	RESOURCE_ACCESS_FRAGMENT_SHADER_READ_UNIFORM_BUFFER,
	RESOURCE_ACCESS_FRAGMENT_SHADER_READ_SAMPLED_IMAGE,
	RESOURCE_ACCESS_FRAGMENT_SHADER_READ_COLOR_ATTACHMENT,
	RESOURCE_ACCESS_FRAGMENT_SHADER_READ_DEPTH_STENCIL_ATTACHMENT,
	RESOURCE_ACCESS_COLOR_ATTACHMENT_READ,
	RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ,
	RESOURCE_ACCESS_TRANSFER_READ,
	RESOURCE_ACCESS_HOST_READ,
	RESOURCE_ACCESS_PRESENT,
	RESOURCE_ACCESS_END_OF_READ,

	/* Writes */
	RESOURCE_ACCESS_VERTEX_SHADER_WRITE,
	RESOURCE_ACCESS_FRAGMENT_SHADER_WRITE,
	RESOURCE_ACCESS_COLOR_ATTACHMENT_WRITE,
	RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE,
	RESOURCE_ACCESS_TRANSFER_WRITE,
	RESOURCE_ACCESS_HOST_WRITE,

	/* Read-Writes */
	RESOURCE_ACCESS_COLOR_ATTACHMENT_READ_WRITE,
	RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_WRITE,
	RESOURCE_ACCESS_MEMORY_TRANSFER_READ_WRITE,
	RESOURCE_ACCESS_GENERAL,

	/* Count */
	RESOURCE_ACCESS_TYPES_COUNT
} VulkanResourceAccessType;

typedef enum CreateSwapchainResult
{
	CREATE_SWAPCHAIN_FAIL,
	CREATE_SWAPCHAIN_SUCCESS,
	CREATE_SWAPCHAIN_SURFACE_ZERO,
} CreateSwapchainResult;

/* Image Barriers */

typedef struct VulkanResourceAccessInfo
{
	VkPipelineStageFlags stageMask;
	VkAccessFlags accessMask;
	VkImageLayout imageLayout;
} VulkanResourceAccessInfo;

static const VulkanResourceAccessInfo AccessMap[RESOURCE_ACCESS_TYPES_COUNT] =
{
	/* RESOURCE_ACCESS_NONE */
	{
		0,
		0,
		VK_IMAGE_LAYOUT_UNDEFINED
	},

	/* RESOURCE_ACCESS_INDEX_BUFFER */
	{
		VK_PIPELINE_STAGE_VERTEX_INPUT_BIT,
		VK_ACCESS_INDEX_READ_BIT,
		VK_IMAGE_LAYOUT_UNDEFINED
	},

	/* RESOURCE_ACCESS_VERTEX_BUFFER */
	{
		VK_PIPELINE_STAGE_VERTEX_INPUT_BIT,
		VK_ACCESS_INDEX_READ_BIT,
		VK_IMAGE_LAYOUT_UNDEFINED
	},

	/* RESOURCE_ACCESS_VERTEX_SHADER_READ_UNIFORM_BUFFER */
	{
		VK_PIPELINE_STAGE_VERTEX_SHADER_BIT,
		VK_ACCESS_SHADER_READ_BIT,
		VK_IMAGE_LAYOUT_UNDEFINED
	},

	/* RESOURCE_ACCESS_VERTEX_SHADER_READ_SAMPLED_IMAGE */
	{
		VK_PIPELINE_STAGE_VERTEX_SHADER_BIT,
		VK_ACCESS_SHADER_READ_BIT,
		VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
	},

	/* RESOURCE_ACCESS_FRAGMENT_SHADER_READ_UNIFORM_BUFFER */
	{
		VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,
		VK_ACCESS_UNIFORM_READ_BIT,
		VK_IMAGE_LAYOUT_UNDEFINED
	},

	/* RESOURCE_ACCESS_FRAGMENT_SHADER_READ_SAMPLED_IMAGE */
	{
		VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,
		VK_ACCESS_SHADER_READ_BIT,
		VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
	},

	/* RESOURCE_ACCESS_FRAGMENT_SHADER_READ_COLOR_ATTACHMENT */
	{
		VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,
		VK_ACCESS_INPUT_ATTACHMENT_READ_BIT,
		VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
	},

	/* RESOURCE_ACCESS_FRAGMENT_SHADER_READ_DEPTH_STENCIL_ATTACHMENT */
	{
		VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,
		VK_ACCESS_INPUT_ATTACHMENT_READ_BIT,
		VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL
	},

	/* RESOURCE_ACCESS_COLOR_ATTACHMENT_READ */
	{
		VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
		VK_ACCESS_COLOR_ATTACHMENT_READ_BIT,
		VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
	},

	/* RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ */
	{
		VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT,
		VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT,
		VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL
	},

	/* RESOURCE_ACCESS_TRANSFER_READ */
	{
		VK_PIPELINE_STAGE_TRANSFER_BIT,
		VK_ACCESS_TRANSFER_READ_BIT,
		VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL
	},

	/* RESOURCE_ACCESS_HOST_READ */
	{
		VK_PIPELINE_STAGE_HOST_BIT,
		VK_ACCESS_HOST_READ_BIT,
		VK_IMAGE_LAYOUT_GENERAL
	},

	/* RESOURCE_ACCESS_PRESENT */
	{
		0,
		0,
		VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
	},

	/* RESOURCE_ACCESS_END_OF_READ */
	{
		0,
		0,
		VK_IMAGE_LAYOUT_UNDEFINED
	},

	/* RESOURCE_ACCESS_VERTEX_SHADER_WRITE */
	{
		VK_PIPELINE_STAGE_VERTEX_SHADER_BIT,
		VK_ACCESS_SHADER_WRITE_BIT,
		VK_IMAGE_LAYOUT_GENERAL
	},

	/* RESOURCE_ACCESS_FRAGMENT_SHADER_WRITE */
	{
		VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,
		VK_ACCESS_SHADER_WRITE_BIT,
		VK_IMAGE_LAYOUT_GENERAL
	},

	/* RESOURCE_ACCESS_COLOR_ATTACHMENT_WRITE */
	{
		VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
		VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
		VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
	},

	/* RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE */
	{
		VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT,
		VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT,
		VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
	},

	/* RESOURCE_ACCESS_TRANSFER_WRITE */
	{
		VK_PIPELINE_STAGE_TRANSFER_BIT,
		VK_ACCESS_TRANSFER_WRITE_BIT,
		VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL
	},

	/* RESOURCE_ACCESS_HOST_WRITE */
	{
		VK_PIPELINE_STAGE_HOST_BIT,
		VK_ACCESS_HOST_WRITE_BIT,
		VK_IMAGE_LAYOUT_GENERAL
	},

	/* RESOURCE_ACCESS_COLOR_ATTACHMENT_READ_WRITE */
	{
		VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
		VK_ACCESS_COLOR_ATTACHMENT_READ_BIT | VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
		VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
	},

	/* RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_WRITE */
	{
		VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT,
		VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT | VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT,
		VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL
	},

	/* RESOURCE_ACCESS_MEMORY_TRANSFER_READ_WRITE */
	{
		VK_PIPELINE_STAGE_TRANSFER_BIT,
		VK_ACCESS_TRANSFER_READ_BIT | VK_ACCESS_TRANSFER_WRITE_BIT,
		VK_IMAGE_LAYOUT_UNDEFINED
	},

	/* RESOURCE_ACCESS_GENERAL */
	{
		VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
		VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT,
		VK_IMAGE_LAYOUT_GENERAL
	}
};

/* Descriptor Set Data */

typedef struct SamplerDescriptorSetData
{
	VkDescriptorImageInfo descriptorImageInfo[MAX_TEXTURE_SAMPLERS]; /* used for vertex samplers as well */
} SamplerDescriptorSetData;

typedef struct SamplerDescriptorSetHashMap
{
	uint64_t key;
	SamplerDescriptorSetData descriptorSetData;
	VkDescriptorSet descriptorSet;
	uint8_t inactiveFrameCount;
} SamplerDescriptorSetHashMap;

typedef struct SamplerDescriptorSetHashArray
{
	uint32_t *elements;
	int32_t count;
	int32_t capacity;
} SamplerDescriptorSetHashArray;

#define NUM_DESCRIPTOR_SET_HASH_BUCKETS 1031

static inline uint64_t SamplerDescriptorSetHashTable_GetHashCode(
	SamplerDescriptorSetData *descriptorSetData,
	uint32_t samplerCount
) {
	const uint64_t HASH_FACTOR = 97;
	uint32_t i;
	uint64_t result = 1;

	for (i = 0; i < samplerCount; i++)
	{
		result = result * HASH_FACTOR + (uint64_t) descriptorSetData->descriptorImageInfo[i].imageView;
		result = result * HASH_FACTOR + (uint64_t) descriptorSetData->descriptorImageInfo[i].sampler;
	}

	return result;
}

typedef struct ShaderResources
{
	SamplerDescriptorSetHashArray buckets[NUM_DESCRIPTOR_SET_HASH_BUCKETS]; /* these buckets store indices */
	SamplerDescriptorSetHashMap *elements; /* where the hash map elements are stored */
	uint32_t count;
	uint32_t capacity;

	VkDescriptorPool *samplerDescriptorPools;
	uint32_t samplerDescriptorPoolCount;
	uint32_t nextPoolSize;

	VkDescriptorSetLayout samplerLayout;
	uint8_t *samplerBindingIndices;
	uint32_t samplerCount;

	VkDescriptorSet *inactiveDescriptorSets;
	uint32_t inactiveDescriptorSetCount;
	uint32_t inactiveDescriptorSetCapacity;

	VkDescriptorSet uniformDescriptorSet;
	VkDescriptorBufferInfo uniformBufferInfo;
} ShaderResources;

typedef struct ShaderResourcesHashMap
{
	MOJOSHADER_vkShader *key;
	ShaderResources *value;
} ShaderResourcesHashMap;

typedef struct ShaderResourcesHashArray
{
	ShaderResourcesHashMap *elements;
	int32_t count;
	int32_t capacity;
} ShaderResourcesHashArray;

#define NUM_SHADER_RESOURCES_BUCKETS 1031

typedef struct ShaderResourcesHashTable
{
	ShaderResourcesHashArray buckets[NUM_SHADER_RESOURCES_BUCKETS];
} ShaderResourcesHashTable;

static inline ShaderResources *ShaderResourcesHashTable_Fetch(
	ShaderResourcesHashTable *table,
	MOJOSHADER_vkShader *key
) {
	int32_t i;
	uint64_t hashcode = (uint64_t) (size_t) key;
	ShaderResourcesHashArray *arr = &table->buckets[hashcode % NUM_SHADER_RESOURCES_BUCKETS];

	for (i = 0; i < arr->count; i += 1)
	{
		const MOJOSHADER_vkShader *e = arr->elements[i].key;
		if (key == e)
		{
			return arr->elements[i].value;
		}
	}

	return NULL;
}

static inline void ShaderResourcesHashTable_Insert(
	ShaderResourcesHashTable *table,
	MOJOSHADER_vkShader *key,
	ShaderResources *value
) {
	uint64_t hashcode = (uint64_t) (size_t) key;
	ShaderResourcesHashArray *arr = &table->buckets[hashcode % NUM_SHADER_RESOURCES_BUCKETS];

	ShaderResourcesHashMap map;
	map.key = key;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 2, ShaderResourcesHashMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

/* Internal Structures */

typedef struct SwapChainSupportDetails
{
	VkSurfaceCapabilitiesKHR capabilities;
	VkSurfaceFormatKHR *formats;
	uint32_t formatsLength;
	VkPresentModeKHR *presentModes;
	uint32_t presentModesLength;
} SwapChainSupportDetails;

/* FIXME: This could be packed better */
typedef struct PipelineHash
{
	PackedState blendState;
	PackedState rasterizerState;
	PackedState depthStencilState;
	uint32_t vertexBufferBindingsIndex;
	FNA3D_PrimitiveType primitiveType;
	VkSampleMask sampleMask;
	MOJOSHADER_vkShader *vertShader;
	MOJOSHADER_vkShader *fragShader;
	/* Pipelines have to be compatible with a render pass */
	VkRenderPass renderPass;
} PipelineHash;

typedef struct PipelineHashMap
{
	PipelineHash key;
	VkPipeline value;
} PipelineHashMap;

typedef struct PipelineHashArray
{
	PipelineHashMap *elements;
	int32_t count;
	int32_t capacity;
} PipelineHashArray;

#define NUM_PIPELINE_HASH_BUCKETS 1031

typedef struct PipelineHashTable
{
	PipelineHashArray buckets[NUM_PIPELINE_HASH_BUCKETS];
} PipelineHashTable;

static inline uint64_t PipelineHashTable_GetHashCode(PipelineHash hash)
{
	/* The algorithm for this hashing function
	 * is taken from Josh Bloch's "Effective Java".
	 * (https://stackoverflow.com/a/113600/12492383)
	 */
	const uint64_t HASH_FACTOR = 97;
	uint64_t result = 1;
	result = result * HASH_FACTOR + hash.blendState.a;
	result = result * HASH_FACTOR + hash.blendState.b;
	result = result * HASH_FACTOR + hash.rasterizerState.a;
	result = result * HASH_FACTOR + hash.rasterizerState.b;
	result = result * HASH_FACTOR + hash.depthStencilState.a;
	result = result * HASH_FACTOR + hash.depthStencilState.b;
	result = result * HASH_FACTOR + hash.vertexBufferBindingsIndex;
	result = result * HASH_FACTOR + hash.primitiveType;
	result = result * HASH_FACTOR + hash.sampleMask;
	result = result * HASH_FACTOR + (uint64_t) (size_t) hash.vertShader;
	result = result * HASH_FACTOR + (uint64_t) (size_t) hash.fragShader;
	result = result * HASH_FACTOR + (uint64_t) (size_t) hash.renderPass;
	return result;
}

static inline VkPipeline PipelineHashTable_Fetch(
	PipelineHashTable *table,
	PipelineHash key
) {
	int32_t i;
	uint64_t hashcode = PipelineHashTable_GetHashCode(key);
	PipelineHashArray *arr = &table->buckets[hashcode % NUM_PIPELINE_HASH_BUCKETS];

	for (i = 0; i < arr->count; i += 1)
	{
		const PipelineHash *e = &arr->elements[i].key;
		if (	key.blendState.a == e->blendState.a &&
			key.blendState.b == e->blendState.b &&
			key.rasterizerState.a == e->rasterizerState.a &&
			key.rasterizerState.b == e->rasterizerState.b &&
			key.depthStencilState.a == e->depthStencilState.a &&
			key.depthStencilState.b == e->depthStencilState.b &&
			key.vertexBufferBindingsIndex == e->vertexBufferBindingsIndex &&
			key.primitiveType == e->primitiveType &&
			key.sampleMask == e->sampleMask &&
			key.vertShader == e->vertShader &&
			key.fragShader == e->fragShader &&
			key.renderPass == e->renderPass	)
		{
			return arr->elements[i].value;
		}
	}

	return VK_NULL_HANDLE;
}

static inline void PipelineHashTable_Insert(
	PipelineHashTable *table,
	PipelineHash key,
	VkPipeline value
) {
	uint64_t hashcode = PipelineHashTable_GetHashCode(key);
	PipelineHashArray *arr = &table->buckets[hashcode % NUM_PIPELINE_HASH_BUCKETS];
	PipelineHashMap map;
	map.key = key;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 2, PipelineHashMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

typedef struct RenderPassHash
{
	VkFormat colorAttachmentFormatOne;
	VkFormat colorAttachmentFormatTwo;
	VkFormat colorAttachmentFormatThree;
	VkFormat colorAttachmentFormatFour;
	VkFormat depthStencilAttachmentFormat;
	uint32_t width;
	uint32_t height;
	uint32_t multiSampleCount;
	uint8_t  clearColor;
	uint8_t  clearDepth;
	uint8_t  clearStencil;
	uint8_t  preserveTargetContents;
} RenderPassHash;

typedef struct RenderPassHashMap
{
	RenderPassHash key;
	VkRenderPass value;
} RenderPassHashMap;

typedef struct RenderPassHashArray
{
	RenderPassHashMap *elements;
	int32_t count;
	int32_t capacity;
} RenderPassHashArray;

static inline VkRenderPass RenderPassHashArray_Fetch(
	RenderPassHashArray *arr,
	RenderPassHash key
) {
	int32_t i;

	for (i = 0; i < arr->count; i += 1)
	{
		const RenderPassHash *e = &arr->elements[i].key;
		if (	key.colorAttachmentFormatOne == e->colorAttachmentFormatOne &&
			key.colorAttachmentFormatTwo == e->colorAttachmentFormatTwo &&
			key.colorAttachmentFormatThree == e->colorAttachmentFormatThree &&
			key.colorAttachmentFormatFour == e->colorAttachmentFormatFour &&
			key.depthStencilAttachmentFormat == e->depthStencilAttachmentFormat &&
			key.width == e->width &&
			key.height == e->height &&
			key.multiSampleCount == e->multiSampleCount &&
			key.clearColor == e->clearColor &&
			key.clearDepth == e->clearDepth &&
			key.clearStencil == e->clearStencil &&
			key.preserveTargetContents == e->preserveTargetContents	)
		{
			return arr->elements[i].value;
		}
	}

	return VK_NULL_HANDLE;
}

static inline void RenderPassHashArray_Insert(
	RenderPassHashArray *arr,
	RenderPassHash key,
	VkRenderPass value
) {
	RenderPassHashMap map;

	map.key = key;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 4, RenderPassHashMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

typedef struct FramebufferHash
{
	VkImageView colorAttachmentViews[MAX_RENDERTARGET_BINDINGS];
	VkImageView colorMultiSampleAttachmentViews[MAX_RENDERTARGET_BINDINGS];
	VkImageView depthStencilAttachmentView;
	uint32_t width;
	uint32_t height;
} FramebufferHash;

typedef struct FramebufferHashMap
{
	FramebufferHash key;
	VkFramebuffer value;
} FramebufferHashMap;

typedef struct FramebufferHashArray
{
	FramebufferHashMap *elements;
	int32_t count;
	int32_t capacity;
} FramebufferHashArray;

static inline VkFramebuffer FramebufferHashArray_Fetch(
	FramebufferHashArray *arr,
	FramebufferHash key
) {
	int32_t i;

	for (i = 0; i < arr->count; i += 1)
	{
		const FramebufferHash *e = &arr->elements[i].key;
		if (	SDL_memcmp(key.colorAttachmentViews, e->colorAttachmentViews, sizeof(key.colorAttachmentViews)) == 0 &&
			SDL_memcmp(key.colorAttachmentViews, e->colorAttachmentViews, sizeof(key.colorAttachmentViews)) == 0 &&
			key.depthStencilAttachmentView == e->depthStencilAttachmentView &&
			key.width == e->width &&
			key.height == e->height	)
		{
			return arr->elements[i].value;
		}
	}

	return VK_NULL_HANDLE;
}

static inline void FramebufferHashArray_Insert(
	FramebufferHashArray *arr,
	FramebufferHash key,
	VkFramebuffer value
) {
	FramebufferHashMap map;
	map.key = key;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 4, FramebufferHashMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

static inline void FramebufferHashArray_Remove(
	FramebufferHashArray *arr,
	uint32_t index
) {
	if (index != arr->count - 1)
	{
		arr->elements[index] = arr->elements[arr->count - 1];
	}

	arr->count -= 1;
}

typedef struct SamplerStateHashMap
{
	PackedState key;
	VkSampler value;
} SamplerStateHashMap;

typedef struct SamplerStateHashArray
{
	SamplerStateHashMap *elements;
	int32_t count;
	int32_t capacity;
} SamplerStateHashArray;

static inline VkSampler SamplerStateHashArray_Fetch(
	SamplerStateHashArray *arr,
	PackedState key
) {
	int32_t i;

	for (i = 0; i < arr->count; i += 1)
	{
		if (	key.a == arr->elements[i].key.a &&
			key.b == arr->elements[i].key.b		)
		{
			return arr->elements[i].value;
		}
	}

	return VK_NULL_HANDLE;
}

static inline void SamplerStateHashArray_Insert(
	SamplerStateHashArray *arr,
	PackedState key,
	VkSampler value
) {
	SamplerStateHashMap map;
	map.key.a = key.a;
	map.key.b = key.b;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 4, SamplerStateHashMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

typedef struct DescriptorSetLayoutHash
{
	VkDescriptorType descriptorType;
	uint16_t bitmask;
	VkShaderStageFlagBits stageFlag;
} DescriptorSetLayoutHash;

typedef struct DescriptorSetLayoutHashMap
{
	DescriptorSetLayoutHash key;
	VkDescriptorSetLayout value;
} DescriptorSetLayoutHashMap;

typedef struct DescriptorSetLayoutHashArray
{
	DescriptorSetLayoutHashMap *elements;
	int32_t count;
	int32_t capacity;
} DescriptorSetLayoutHashArray;

#define NUM_DESCRIPTOR_SET_LAYOUT_BUCKETS 1031

typedef struct DescriptorSetLayoutHashTable
{
	DescriptorSetLayoutHashArray buckets[NUM_DESCRIPTOR_SET_LAYOUT_BUCKETS];
} DescriptorSetLayoutHashTable;

static inline uint64_t DescriptorSetLayoutHashTable_GetHashCode(DescriptorSetLayoutHash key)
{
	const uint64_t HASH_FACTOR = 97;
	uint64_t result = 1;
	result = result * HASH_FACTOR + key.descriptorType;
	result = result * HASH_FACTOR + key.bitmask;
	result = result * HASH_FACTOR + key.stageFlag;
	return result;
}

static inline VkDescriptorSetLayout DescriptorSetLayoutHashTable_Fetch(
	DescriptorSetLayoutHashTable *table,
	DescriptorSetLayoutHash key
) {
	int32_t i;
	uint64_t hashcode = DescriptorSetLayoutHashTable_GetHashCode(key);
	DescriptorSetLayoutHashArray *arr = &table->buckets[hashcode % NUM_DESCRIPTOR_SET_LAYOUT_BUCKETS];

	for (i = 0; i < arr->count; i += 1)
	{
		const DescriptorSetLayoutHash *e = &arr->elements[i].key;
		if (    key.descriptorType == e->descriptorType &&
			key.bitmask == e->bitmask &&
			key.stageFlag == e->stageFlag   )
		{
			return arr->elements[i].value;
		}
	}

	return VK_NULL_HANDLE;
}

static inline void DescriptorSetLayoutHashTable_Insert(
	DescriptorSetLayoutHashTable *table,
	DescriptorSetLayoutHash key,
	VkDescriptorSetLayout value
) {
	uint64_t hashcode = DescriptorSetLayoutHashTable_GetHashCode(key);
	DescriptorSetLayoutHashArray *arr = &table->buckets[hashcode % NUM_DESCRIPTOR_SET_LAYOUT_BUCKETS];

	DescriptorSetLayoutHashMap map;
	map.key = key;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 4, DescriptorSetLayoutHashMap);

	arr->elements[arr->count] = map;
	arr->count += 1;
}

typedef struct PipelineLayoutHash
{
	VkDescriptorSetLayout vertexSamplerLayout;
	VkDescriptorSetLayout fragSamplerLayout;
	VkDescriptorSetLayout vertexUniformLayout;
	VkDescriptorSetLayout fragUniformLayout;
} PipelineLayoutHash;

typedef struct PipelineLayoutHashMap
{
	PipelineLayoutHash key;
	VkPipelineLayout value;
} PipelineLayoutHashMap;

typedef struct PipelineLayoutHashArray
{
	PipelineLayoutHashMap *elements;
	int32_t count;
	int32_t capacity;
} PipelineLayoutHashArray;

#define NUM_PIPELINE_LAYOUT_BUCKETS 1031

typedef struct PipelineLayoutHashTable
{
	PipelineLayoutHashArray buckets[NUM_PIPELINE_LAYOUT_BUCKETS];
} PipelineLayoutHashTable;

static inline uint64_t PipelineLayoutHashTable_GetHashCode(PipelineLayoutHash key)
{
	const uint64_t HASH_FACTOR = 97;
	uint64_t result = 1;
	result = result * HASH_FACTOR + (uint64_t) key.vertexSamplerLayout;
	result = result * HASH_FACTOR + (uint64_t) key.fragSamplerLayout;
	result = result * HASH_FACTOR + (uint64_t) key.vertexUniformLayout;
	result = result * HASH_FACTOR + (uint64_t) key.fragUniformLayout;
	return result;
}

static inline VkPipelineLayout PipelineLayoutHashArray_Fetch(
	PipelineLayoutHashTable *table,
	PipelineLayoutHash key
) {
	int32_t i;
	uint64_t hashcode = PipelineLayoutHashTable_GetHashCode(key);
	PipelineLayoutHashArray *arr = &table->buckets[hashcode % NUM_PIPELINE_LAYOUT_BUCKETS];

	for (i = 0; i < arr->count; i += 1)
	{
		const PipelineLayoutHash *e = &arr->elements[i].key;
		if (	key.vertexSamplerLayout == e->vertexSamplerLayout &&
			key.fragSamplerLayout == e->fragSamplerLayout &&
			key.vertexUniformLayout == e->vertexUniformLayout &&
			key.fragUniformLayout == e->fragUniformLayout	)
		{
			return arr->elements[i].value;
		}
	}

	return VK_NULL_HANDLE;
}

static inline void PipelineLayoutHashArray_Insert(
	PipelineLayoutHashTable *table,
	PipelineLayoutHash key,
	VkPipelineLayout value
) {
	uint64_t hashcode = PipelineLayoutHashTable_GetHashCode(key);
	PipelineLayoutHashArray *arr = &table->buckets[hashcode % NUM_PIPELINE_HASH_BUCKETS];

	PipelineLayoutHashMap map;
	map.key = key;
	map.value = value;

	EXPAND_ARRAY_IF_NEEDED(arr, 4, PipelineLayoutHashMap)

	arr->elements[arr->count] = map;
	arr->count += 1;
}

typedef struct VulkanMemoryAllocation VulkanMemoryAllocation;
typedef struct VulkanBuffer VulkanBuffer;
typedef struct VulkanSubBuffer VulkanSubBuffer;
typedef struct VulkanTexture VulkanTexture;

typedef struct VulkanMemoryFreeRegion
{
	VulkanMemoryAllocation *allocation;
	VkDeviceSize offset;
	VkDeviceSize size;
	uint32_t allocationIndex;
	uint32_t sortedIndex;
} VulkanMemoryFreeRegion;

typedef struct VulkanMemoryUsedRegion
{
	VulkanMemoryAllocation *allocation;
	VkDeviceSize offset;
	VkDeviceSize size;
	VkDeviceSize resourceOffset; /* differs from offset based on alignment */
	VkDeviceSize resourceSize; /* differs from size based on alignment */
	VkDeviceSize alignment;
	uint8_t isBuffer;
	/* used to copy resource */
	FNA3DNAMELESS union
	{
		VulkanSubBuffer *vulkanSubBuffer;
		VulkanTexture *vulkanTexture;
	};
} VulkanMemoryUsedRegion;

typedef struct VulkanMemorySubAllocator
{
	uint32_t memoryTypeIndex;
	VkDeviceSize nextAllocationSize;
	VulkanMemoryAllocation **allocations;
	uint32_t allocationCount;
	VulkanMemoryFreeRegion **sortedFreeRegions;
	uint32_t sortedFreeRegionCount;
	uint32_t sortedFreeRegionCapacity;
} VulkanMemorySubAllocator;

struct VulkanMemoryAllocation
{
	VulkanMemorySubAllocator *allocator;
	VkDeviceMemory memory;
	VkDeviceSize size;
	VulkanMemoryUsedRegion **usedRegions;
	uint32_t usedRegionCount;
	uint32_t usedRegionCapacity;
	VulkanMemoryFreeRegion **freeRegions;
	uint32_t freeRegionCount;
	uint32_t freeRegionCapacity;
	uint8_t dedicated;
	uint8_t availableForAllocation;
	VkDeviceSize freeSpace;
	VkDeviceSize usedSpace;
	uint8_t *mapPointer;
	SDL_mutex *mapLock;
};

typedef struct VulkanMemoryAllocator
{
	VulkanMemorySubAllocator subAllocators[VK_MAX_MEMORY_TYPES];
} VulkanMemoryAllocator;

struct VulkanTexture /* Cast from FNA3D_Texture* */
{
	VulkanMemoryUsedRegion *usedRegion;
	VkImage image;
	VkImageView view;
	VkImageView rtViews[6];
	VkExtent2D dimensions;
	uint32_t depth;
	uint8_t external;
	VkFormat surfaceFormat;
	uint32_t layerCount;
	uint32_t levelCount;
	uint8_t isRenderTarget;
	VulkanResourceAccessType resourceAccessType;
	VkImageCreateInfo imageCreateInfo; /* used for resource copy */
	VkImageViewCreateInfo viewCreateInfo; /* used for resource copy */
	FNA3DNAMELESS union
	{
		FNA3D_SurfaceFormat colorFormat;
		FNA3D_DepthFormat depthStencilFormat;
	};
};

static VulkanTexture NullTexture =
{
	(VulkanMemoryUsedRegion*) 0,
	(VkImage) 0,
	(VkImageView) 0,
	{ 0, 0, 0, 0, 0, 0 },
	{0, 0},
	0,
	0,
	VK_FORMAT_UNDEFINED,
	0,
	RESOURCE_ACCESS_NONE
};

struct VulkanSubBuffer
{
	VulkanBuffer *parent;
	VulkanMemoryUsedRegion *usedRegion;
	VkBuffer buffer;
	VulkanResourceAccessType resourceAccessType;
	int8_t bound;
	VkBufferCreateInfo bufferCreateInfo; /* used for resource copy */
};

/*
 * Our VulkanBuffer is actually a series of sub-buffers
 * so we can properly support overwrites without flushing
 */
struct VulkanBuffer /* cast from FNA3D_Buffer */
{
	VkDeviceSize size;
	VulkanSubBuffer **subBuffers;
	int32_t subBufferCount;
	int32_t subBufferCapacity;
	int32_t currentSubBufferIndex;
	VulkanResourceAccessType resourceAccessType;
	uint8_t bound;
	uint8_t boundSubmitted;
	VkBufferUsageFlags usage;
	uint8_t preferDeviceLocal;
	uint8_t isStagingBuffer;
};

typedef struct VulkanStagingBuffer
{
	VulkanBuffer *fastBuffer; /* can be NULL */
	VkDeviceSize fastBufferOffset;

	VulkanBuffer *slowBuffer; /* always exists */
	VkDeviceSize slowBufferOffset;

	uint8_t pendingTransfer;
	uint8_t transferInProgress;
} VulkanStagingBuffer;

typedef struct VulkanColorBuffer
{
	VulkanTexture *handle;
	VulkanTexture *multiSampleTexture;
	uint32_t multiSampleCount;
} VulkanColorBuffer;

typedef struct VulkanDepthStencilBuffer
{
	VulkanTexture *handle;
} VulkanDepthStencilBuffer;

typedef struct VulkanRenderbuffer /* Cast from FNA3D_Renderbuffer* */
{
	VulkanColorBuffer *colorBuffer;
	VulkanDepthStencilBuffer *depthBuffer;
} VulkanRenderbuffer;

typedef struct VulkanEffect /* Cast from FNA3D_Effect* */
{
	MOJOSHADER_effect *effect;
} VulkanEffect;

typedef struct VulkanQuery /* Cast from FNA3D_Query* */
{
	uint32_t index;
} VulkanQuery;

typedef struct VulkanRenderer
{
	FNA3D_Device *parentDevice;
	VkInstance instance;
	VkPhysicalDevice physicalDevice;
	VkPhysicalDeviceProperties2 physicalDeviceProperties;
	VkPhysicalDeviceDriverPropertiesKHR physicalDeviceDriverProperties;
	VkDevice logicalDevice;

	FNA3D_PresentInterval presentInterval;
	void* deviceWindowHandle;

	uint32_t queueFamilyIndex;
	VkQueue unifiedQueue;

	VkSurfaceKHR surface;
	VkSwapchainKHR swapChain;
	VkImage *swapChainImages;
	VulkanResourceAccessType *swapChainResourceAccessTypes;
	VkImageView *swapChainImageViews;
	uint32_t swapChainImageCount;
	VkExtent2D swapChainExtent;

	PackedVertexBufferBindingsArray vertexBufferBindingsCache;
	VkPipelineCache pipelineCache;

	VkRenderPass renderPass;
	VkPipeline currentPipeline;
	VkPipelineLayout currentPipelineLayout;
	int32_t currentVertexBufferBindingsIndex;

	/* Command Buffers */
	VkCommandPool commandPool;
	VkCommandBuffer *inactiveCommandBuffers;
	VkCommandBuffer *activeCommandBuffers;
	VkCommandBuffer *submittedCommandBuffers;
	uint32_t inactiveCommandBufferCount;
	uint32_t activeCommandBufferCount;
	uint32_t submittedCommandBufferCount;
	uint32_t allocatedCommandBufferCount;
	uint32_t currentCommandCount;
	VkCommandBuffer currentCommandBuffer;
	uint32_t numActiveCommands;

	/* Special command buffer for performing defrag copies */
	VkCommandBuffer defragCommandBuffer;

	/* Queries */
	VkQueryPool queryPool;
	int8_t freeQueryIndexStack[MAX_QUERIES];
	int8_t freeQueryIndexStackHead;

	VkFormat swapchainFormat;
	VkComponentMapping swapchainSwizzle;
	VulkanColorBuffer fauxBackbufferColor;
	VulkanTexture *fauxBackbufferMultiSampleColor;
	VulkanDepthStencilBuffer fauxBackbufferDepthStencil;
	VkFramebuffer fauxBackbufferFramebuffer;
	uint32_t fauxBackbufferWidth;
	uint32_t fauxBackbufferHeight;
	uint32_t fauxBackbufferMultiSampleCount;

	VulkanTexture *colorAttachments[MAX_RENDERTARGET_BINDINGS];
	VulkanTexture *colorMultiSampleAttachments[MAX_RENDERTARGET_BINDINGS];
	FNA3D_CubeMapFace attachmentCubeFaces[MAX_RENDERTARGET_BINDINGS];
	uint32_t multiSampleCount;
	uint32_t colorAttachmentCount;

	VulkanTexture *depthStencilAttachment;
	FNA3D_DepthFormat currentDepthFormat;

	FNA3D_Viewport viewport;
	FNA3D_Rect scissorRect;

	VkSampleMask multiSampleMask[MAX_MULTISAMPLE_MASK_SIZE];
	FNA3D_BlendState blendState;

	FNA3D_DepthStencilState depthStencilState;
	FNA3D_RasterizerState rasterizerState;
	FNA3D_PrimitiveType currentPrimitiveType;

	VulkanMemoryAllocator *memoryAllocator;
	VkPhysicalDeviceMemoryProperties memoryProperties;
	VkDeviceSize maxDeviceLocalHeapUsage;
	VkDeviceSize deviceLocalHeapUsage;

	VulkanBuffer **buffersInUse;
	uint32_t numBuffersInUse;
	uint32_t maxBuffersInUse;

	VulkanBuffer **submittedBuffers;
	uint32_t numSubmittedBuffers;
	uint32_t maxSubmittedBuffers;

	VulkanStagingBuffer *textureStagingBuffer;

	uint32_t numVertexBindings;
	FNA3D_VertexBufferBinding vertexBindings[MAX_BOUND_VERTEX_BUFFERS];
	FNA3D_VertexElement vertexElements[MAX_BOUND_VERTEX_BUFFERS][MAX_VERTEX_ATTRIBUTES];
	VkBuffer boundVertexBuffers[MAX_BOUND_VERTEX_BUFFERS];
	VkDeviceSize boundVertexBufferOffsets[MAX_BOUND_VERTEX_BUFFERS];

	/* Should be equal to swap chain count */
	VkBuffer ldVertUniformBuffer;
	VkBuffer ldFragUniformBuffer;
	VkDeviceSize ldVertUniformOffset;
	VkDeviceSize ldFragUniformOffset;
	VkDeviceSize ldVertUniformSize;
	VkDeviceSize ldFragUniformSize;

	int32_t stencilRef;

	int32_t numSamplers;
	int32_t numTextureSlots;
	int32_t numVertexTextureSlots;

	VulkanTexture *textures[TEXTURE_COUNT];
	VkSampler samplers[TEXTURE_COUNT];
	uint8_t textureNeedsUpdate[TEXTURE_COUNT];
	uint8_t samplerNeedsUpdate[TEXTURE_COUNT];

	VulkanBuffer *dummyVertUniformBuffer;
	VulkanBuffer *dummyFragUniformBuffer;

	VkSampler dummyVertSamplerState;
	VkSampler dummyVertSampler3DState;
	VkSampler dummyVertSamplerCubeState;
	VkSampler dummyFragSamplerState;
	VkSampler dummyFragSampler3DState;
	VkSampler dummyFragSamplerCubeState;

	VulkanTexture *dummyVertTexture;
	VulkanTexture *dummyVertTexture3D;
	VulkanTexture *dummyVertTextureCube;
	VulkanTexture *dummyFragTexture;
	VulkanTexture *dummyFragTexture3D;
	VulkanTexture *dummyFragTextureCube;

	VkDescriptorPool uniformBufferDescriptorPool;
	VkDescriptorSetLayout vertexUniformBufferDescriptorSetLayout;
	VkDescriptorSetLayout fragUniformBufferDescriptorSetLayout;
	VkDescriptorSet dummyVertexUniformBufferDescriptorSet;
	VkDescriptorSet dummyFragUniformBufferDescriptorSet;

	uint8_t vertexSamplerDescriptorSetDataNeedsUpdate;
	uint8_t fragSamplerDescriptorSetDataNeedsUpdate;

	VkDescriptorSet currentVertexSamplerDescriptorSet;
	VkDescriptorSet currentFragSamplerDescriptorSet;

	ShaderResourcesHashTable shaderResourcesHashTable;
	DescriptorSetLayoutHashTable descriptorSetLayoutTable;
	PipelineLayoutHashTable pipelineLayoutTable;
	PipelineHashTable pipelineHashTable;
	RenderPassHashArray renderPassArray;
	FramebufferHashArray framebufferArray;
	SamplerStateHashArray samplerStateArray;

	VkFence inFlightFence;
	VkFence defragFence;
	VkSemaphore imageAvailableSemaphore;
	VkSemaphore renderFinishedSemaphore;
	VkSemaphore defragSemaphore;

	uint8_t bufferDefragInProgress;
	uint8_t needDefrag;
	uint32_t defragTimer;
	uint8_t resourceFreed;

	/* MojoShader Interop */
	MOJOSHADER_vkContext *mojoshaderContext;
	MOJOSHADER_effect *currentEffect;
	const MOJOSHADER_effectTechnique *currentTechnique;
	uint32_t currentPass;

	VkShaderModule currentVertShader;
	VkShaderModule currentFragShader;

	/* Storing references to objects that need to be destroyed
	 * so we don't have to stall or invalidate the command buffer
	 */

	VulkanRenderbuffer **renderbuffersToDestroy;
	uint32_t renderbuffersToDestroyCount;
	uint32_t renderbuffersToDestroyCapacity;

	VulkanBuffer **buffersToDestroy;
	uint32_t buffersToDestroyCount;
	uint32_t buffersToDestroyCapacity;

	VulkanEffect **effectsToDestroy;
	uint32_t effectsToDestroyCount;
	uint32_t effectsToDestroyCapacity;

	VulkanTexture **texturesToDestroy;
	uint32_t texturesToDestroyCount;
	uint32_t texturesToDestroyCapacity;

	VulkanRenderbuffer **submittedRenderbuffersToDestroy;
	uint32_t submittedRenderbuffersToDestroyCount;
	uint32_t submittedRenderbuffersToDestroyCapacity;

	VulkanBuffer **submittedBuffersToDestroy;
	uint32_t submittedBuffersToDestroyCount;
	uint32_t submittedBuffersToDestroyCapacity;

	VulkanEffect **submittedEffectsToDestroy;
	uint32_t submittedEffectsToDestroyCount;
	uint32_t submittedEffectsToDestroyCapacity;

	VulkanTexture **submittedTexturesToDestroy;
	uint32_t submittedTexturesToDestroyCount;
	uint32_t submittedTexturesToDestroyCapacity;

	VkBuffer *defragmentedBuffersToDestroy;
	uint32_t defragmentedBuffersToDestroyCount;
	uint32_t defragmentedBuffersToDestroyCapacity;

	VkImage *defragmentedImagesToDestroy;
	uint32_t defragmentedImagesToDestroyCount;
	uint32_t defragmentedImagesToDestroyCapacity;

	VkImageView *defragmentedImageViewsToDestroy;
	uint32_t defragmentedImageViewsToDestroyCount;
	uint32_t defragmentedImageViewsToDestroyCapacity;

	VulkanMemoryUsedRegion **usedRegionsToDestroy;
	uint32_t usedRegionsToDestroyCount;
	uint32_t usedRegionsToDestroyCapacity;

	uint8_t renderPassInProgress;
	uint8_t needNewRenderPass;
	uint8_t renderTargetBound;
	uint8_t needNewPipeline;

	uint8_t shouldClearColorOnBeginPass;
	uint8_t shouldClearDepthOnBeginPass;
	uint8_t shouldClearStencilOnBeginPass;
	uint8_t preserveTargetContents;
	uint8_t drawCallMadeThisPass;

	VkClearColorValue clearColorValue;
	VkClearDepthStencilValue clearDepthStencilValue;

	/* Depth Formats (may not match the format implied by the name!) */
	VkFormat D16Format;
	VkFormat D24Format;
	VkFormat D24S8Format;

	/* Capabilities */
	uint8_t supportsDxt1;
	uint8_t supportsS3tc;
	uint8_t supportsDebugUtils;
	uint8_t debugMode;
	VulkanExtensions supports;

	/* Submission */
	FNA3D_Rect *presentSourceRectangle;
	FNA3D_Rect *presentDestinationRectangle;
	void *presentOverrideWindowHandle;

	uint8_t submitCounter; /* used so we don't clobber data being used by GPU */

	/* Threading */
	SDL_mutex *commandLock;
	SDL_mutex *passLock;
	SDL_mutex *disposeLock;
	SDL_mutex *allocatorLock;
	SDL_mutex *stagingLock;

	#define VULKAN_INSTANCE_FUNCTION(name) \
		PFN_##name name;
	#define VULKAN_DEVICE_FUNCTION(name) \
		PFN_##name name;
	#include "FNA3D_Driver_Vulkan_vkfuncs.h"
} VulkanRenderer;

/* Command Buffer Recording Macro */

#define RECORD_CMD(cmdCall)					\
	SDL_LockMutex(renderer->commandLock);			\
	if (renderer->currentCommandBuffer == NULL)		\
	{							\
		VULKAN_INTERNAL_BeginCommandBuffer(renderer);	\
	}							\
	cmdCall;						\
	renderer->numActiveCommands += 1;			\
	SDL_UnlockMutex(renderer->commandLock);

/* XNA->Vulkan Translation Arrays */

static VkIndexType XNAToVK_IndexType[] =
{
	VK_INDEX_TYPE_UINT16,	/* FNA3D_INDEXELEMENTSIZE_16BIT */
	VK_INDEX_TYPE_UINT32	/* FNA3D_INDEXELEMENTSIZE_32BIT */
};

static inline VkSampleCountFlagBits XNAToVK_SampleCount(int32_t sampleCount)
{
	if (sampleCount <= 1)
	{
		return VK_SAMPLE_COUNT_1_BIT;
	}
	else if (sampleCount == 2)
	{
		return VK_SAMPLE_COUNT_2_BIT;
	}
	else if (sampleCount <= 4)
	{
		return VK_SAMPLE_COUNT_4_BIT;
	}
	else if (sampleCount <= 8)
	{
		return VK_SAMPLE_COUNT_8_BIT;
	}
	else if (sampleCount <= 16)
	{
		return VK_SAMPLE_COUNT_16_BIT;
	}
	else if (sampleCount <= 32)
	{
		return VK_SAMPLE_COUNT_32_BIT;
	}
	else if (sampleCount <= 64)
	{
		return VK_SAMPLE_COUNT_64_BIT;
	}
	else
	{
		FNA3D_LogWarn("Unexpected sample count: %d", sampleCount);
		return VK_SAMPLE_COUNT_1_BIT;
	}
}

static VkComponentMapping XNAToVK_SurfaceSwizzle[] =
{
	IDENTITY_SWIZZLE,	/* SurfaceFormat.Color */
	{			/* SurfaceFormat.Bgr565 */
		VK_COMPONENT_SWIZZLE_B,
		VK_COMPONENT_SWIZZLE_G,
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_ONE
	},
	{			/* SurfaceFormat.Bgra5551 */
		VK_COMPONENT_SWIZZLE_B,
		VK_COMPONENT_SWIZZLE_G,
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_A
	},
	{			/* SurfaceFormat.Bgra4444 */
		VK_COMPONENT_SWIZZLE_B,
		VK_COMPONENT_SWIZZLE_G,
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_A
	},
	IDENTITY_SWIZZLE,	/* SurfaceFormat.Dxt1 */
	IDENTITY_SWIZZLE,	/* SurfaceFormat.Dxt3 */
	IDENTITY_SWIZZLE,	/* SurfaceFormat.Dxt5 */
	{			/* SurfaceFormat.NormalizedByte2 */
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_G,
		VK_COMPONENT_SWIZZLE_ONE,
		VK_COMPONENT_SWIZZLE_ONE
	},
	IDENTITY_SWIZZLE,	/* SurfaceFormat.NormalizedByte4 */
	IDENTITY_SWIZZLE,	/* SurfaceFormat.Rgba1010102 */
	{			/* SurfaceFormat.Rg32 */
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_G,
		VK_COMPONENT_SWIZZLE_ONE,
		VK_COMPONENT_SWIZZLE_ONE
	},
	IDENTITY_SWIZZLE,	/* SurfaceFormat.Rgba64 */
	{			/* SurfaceFormat.Alpha8 */
		VK_COMPONENT_SWIZZLE_ZERO,
		VK_COMPONENT_SWIZZLE_ZERO,
		VK_COMPONENT_SWIZZLE_ZERO,
		VK_COMPONENT_SWIZZLE_R
	},
	{			/* SurfaceFormat.Single */
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_ONE,
		VK_COMPONENT_SWIZZLE_ONE,
		VK_COMPONENT_SWIZZLE_ONE
	},
	{			/* SurfaceFormat.Vector2 */
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_G,
		VK_COMPONENT_SWIZZLE_ONE,
		VK_COMPONENT_SWIZZLE_ONE
	},
	IDENTITY_SWIZZLE,	/* SurfaceFormat.Vector4 */
	{			/* SurfaceFormat.HalfSingle */
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_ONE,
		VK_COMPONENT_SWIZZLE_ONE,
		VK_COMPONENT_SWIZZLE_ONE
	},
	{			/* SurfaceFormat.HalfVector2 */
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_G,
		VK_COMPONENT_SWIZZLE_ONE,
		VK_COMPONENT_SWIZZLE_ONE
	},
	IDENTITY_SWIZZLE,	/* SurfaceFormat.HalfVector4 */
	IDENTITY_SWIZZLE,	/* SurfaceFormat.HdrBlendable */
	{			/* SurfaceFormat.ColorBgraEXT */
		VK_COMPONENT_SWIZZLE_B,
		VK_COMPONENT_SWIZZLE_G,
		VK_COMPONENT_SWIZZLE_R,
		VK_COMPONENT_SWIZZLE_A
	}
};

static VkFormat XNAToVK_SurfaceFormat[] =
{
	VK_FORMAT_R8G8B8A8_UNORM,		/* SurfaceFormat.Color */
	VK_FORMAT_R5G6B5_UNORM_PACK16,		/* SurfaceFormat.Bgr565 */
	VK_FORMAT_A1R5G5B5_UNORM_PACK16,	/* SurfaceFormat.Bgra5551 */
	VK_FORMAT_B4G4R4A4_UNORM_PACK16,	/* SurfaceFormat.Bgra4444 */
	VK_FORMAT_BC1_RGBA_UNORM_BLOCK,		/* SurfaceFormat.Dxt1 */
	VK_FORMAT_BC2_UNORM_BLOCK,		/* SurfaceFormat.Dxt3 */
	VK_FORMAT_BC3_UNORM_BLOCK,		/* SurfaceFormat.Dxt5 */
	VK_FORMAT_R8G8_SNORM,			/* SurfaceFormat.NormalizedByte2 */
	VK_FORMAT_R8G8B8A8_SNORM,		/* SurfaceFormat.NormalizedByte4 */
	VK_FORMAT_A2R10G10B10_UNORM_PACK32,	/* SurfaceFormat.Rgba1010102 */
	VK_FORMAT_R16G16_UNORM,			/* SurfaceFormat.Rg32 */
	VK_FORMAT_R16G16B16A16_UNORM,		/* SurfaceFormat.Rgba64 */
	VK_FORMAT_R8_UNORM,			/* SurfaceFormat.Alpha8 */
	VK_FORMAT_R32_SFLOAT,			/* SurfaceFormat.Single */
	VK_FORMAT_R32G32_SFLOAT,		/* SurfaceFormat.Vector2 */
	VK_FORMAT_R32G32B32A32_SFLOAT,		/* SurfaceFormat.Vector4 */
	VK_FORMAT_R16_SFLOAT,			/* SurfaceFormat.HalfSingle */
	VK_FORMAT_R16G16_SFLOAT,		/* SurfaceFormat.HalfVector2 */
	VK_FORMAT_R16G16B16A16_SFLOAT,		/* SurfaceFormat.HalfVector4 */
	VK_FORMAT_R16G16B16A16_SFLOAT,		/* SurfaceFormat.HdrBlendable */
	VK_FORMAT_R8G8B8A8_UNORM		/* SurfaceFormat.ColorBgraEXT */
};

static inline VkFormat XNAToVK_DepthFormat(
	VulkanRenderer *renderer,
	FNA3D_DepthFormat format
) {
	switch (format)
	{
		case FNA3D_DEPTHFORMAT_D16:
			return renderer->D16Format;
		case FNA3D_DEPTHFORMAT_D24:
			return renderer->D24Format;
		case FNA3D_DEPTHFORMAT_D24S8:
			return renderer->D24S8Format;
		default:
			return VK_FORMAT_UNDEFINED;
	}
}

static inline float XNAToVK_DepthBiasScale(VkFormat format)
{
	switch (format)
	{
		case VK_FORMAT_D16_UNORM:
			return (float) ((1 << 16) - 1);

		case VK_FORMAT_D24_UNORM_S8_UINT:
			return (float) ((1 << 24) - 1);

		case VK_FORMAT_D32_SFLOAT:
		case VK_FORMAT_D32_SFLOAT_S8_UINT:
			return (float) ((1 << 23) - 1);

		default:
			return 0.0f;
	}
}

static inline uint8_t DepthFormatContainsStencil(VkFormat format)
{
	switch (format)
	{
		case VK_FORMAT_D16_UNORM:
		case VK_FORMAT_D32_SFLOAT:
			return 0;
		case VK_FORMAT_D24_UNORM_S8_UINT:
		case VK_FORMAT_D32_SFLOAT_S8_UINT:
			return 1;
		default:
			SDL_assert(0 && "Invalid depth pixel format");
			return 0;
	}
}

static inline uint8_t IsDepthFormat(VkFormat format)
{
	return
		format == VK_FORMAT_D16_UNORM ||
		format == VK_FORMAT_D16_UNORM_S8_UINT ||
		format == VK_FORMAT_D24_UNORM_S8_UINT ||
		format == VK_FORMAT_D32_SFLOAT ||
		format == VK_FORMAT_D32_SFLOAT_S8_UINT;
}

static VkBlendFactor XNAToVK_BlendFactor[] =
{
	VK_BLEND_FACTOR_ONE, 				/* FNA3D_BLEND_ONE */
	VK_BLEND_FACTOR_ZERO, 				/* FNA3D_BLEND_ZERO */
	VK_BLEND_FACTOR_SRC_COLOR, 			/* FNA3D_BLEND_SOURCECOLOR */
	VK_BLEND_FACTOR_ONE_MINUS_SRC_COLOR,		/* FNA3D_BLEND_INVERSESOURCECOLOR */
	VK_BLEND_FACTOR_SRC_ALPHA,			/* FNA3D_BLEND_SOURCEALPHA */
	VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA,		/* FNA3D_BLEND_INVERSESOURCEALPHA */
	VK_BLEND_FACTOR_DST_COLOR,			/* FNA3D_BLEND_DESTINATIONCOLOR */
	VK_BLEND_FACTOR_ONE_MINUS_DST_COLOR,		/* FNA3D_BLEND_INVERSEDESTINATIONCOLOR */
	VK_BLEND_FACTOR_DST_ALPHA,			/* FNA3D_BLEND_DESTINATIONALPHA */
	VK_BLEND_FACTOR_ONE_MINUS_DST_ALPHA,		/* FNA3D_BLEND_INVERSEDESTINATIONALPHA */
	VK_BLEND_FACTOR_CONSTANT_COLOR,			/* FNA3D_BLEND_BLENDFACTOR */
	VK_BLEND_FACTOR_ONE_MINUS_CONSTANT_COLOR,	/* FNA3D_BLEND_INVERSEBLENDFACTOR */
	VK_BLEND_FACTOR_SRC_ALPHA_SATURATE 		/* FNA3D_BLEND_SOURCEALPHASATURATION */
};

static VkBlendOp XNAToVK_BlendOp[] =
{
	VK_BLEND_OP_ADD, 		/* FNA3D_BLENDFUNCTION_ADD */
	VK_BLEND_OP_SUBTRACT, 		/* FNA3D_BLENDFUNCTION_SUBTRACT */
	VK_BLEND_OP_REVERSE_SUBTRACT, 	/* FNA3D_BLENDFUNCTION_REVERSESUBTRACT */
	VK_BLEND_OP_MAX, 		/* FNA3D_BLENDFUNCTION_MAX */
	VK_BLEND_OP_MIN			/* FNA3D_BLENDFUNCTION_MIN */
};

static VkPolygonMode XNAToVK_PolygonMode[] =
{
	VK_POLYGON_MODE_FILL, 	/* FNA3D_FILLMODE_SOLID */
	VK_POLYGON_MODE_LINE	/* FNA3D_FILLMODE_WIREFRAME */
};

static VkCullModeFlags XNAToVK_CullMode[] =
{
	VK_CULL_MODE_NONE, 	/* FNA3D_CULLMODE_NONE */
	VK_CULL_MODE_FRONT_BIT, /* FNA3D_CULLMODE_CULLCLOCKWISEFACE */
	VK_CULL_MODE_BACK_BIT	/* FNA3D_CULLMODE_CULLCOUNTERCLOCKWISEFACE */
};

static VkPrimitiveTopology XNAToVK_Topology[] =
{
	VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST,	/* FNA3D_PRIMITIVETYPE_TRIANGLELIST */
	VK_PRIMITIVE_TOPOLOGY_TRIANGLE_STRIP,	/* FNA3D_PRIMITIVETYPE_TRIANGLESTRIP */
	VK_PRIMITIVE_TOPOLOGY_LINE_LIST,	/* FNA3D_PRIMITIVETYPE_LINELIST */
	VK_PRIMITIVE_TOPOLOGY_LINE_STRIP,	/* FNA3D_PRIMITIVETYPE_LINESTRIP */
	VK_PRIMITIVE_TOPOLOGY_POINT_LIST	/* FNA3D_PRIMITIVETYPE_POINTLIST_EXT */
};

static VkSamplerAddressMode XNAToVK_SamplerAddressMode[] =
{
	VK_SAMPLER_ADDRESS_MODE_REPEAT, 	/* FNA3D_TEXTUREADDRESSMODE_WRAP */
	VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE, 	/* FNA3D_TEXTUREADDRESSMODE_CLAMP */
	VK_SAMPLER_ADDRESS_MODE_MIRRORED_REPEAT /* FNA3D_TEXTUREADDRESSMODE_MIRROR */
};

static VkFilter XNAToVK_MagFilter[] =
{
	VK_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_LINEAR */
	VK_FILTER_NEAREST, 	/* FNA3D_TEXTUREFILTER_POINT */
	VK_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_ANISOTROPIC */
	VK_FILTER_LINEAR,	/* FNA3D_TEXTUREFILTER_LINEAR_MIPPOINT */
	VK_FILTER_NEAREST, 	/* FNA3D_TEXTUREFILTER_POINT_MIPLINEAR */
	VK_FILTER_NEAREST,	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPLINEAR */
	VK_FILTER_NEAREST, 	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPPOINT */
	VK_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPLINEAR */
	VK_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPPOINT */
};

static VkSamplerMipmapMode XNAToVK_MipFilter[] =
{
	VK_SAMPLER_MIPMAP_MODE_LINEAR, 	/* FNA3D_TEXTUREFILTER_LINEAR */
	VK_SAMPLER_MIPMAP_MODE_NEAREST, /* FNA3D_TEXTUREFILTER_POINT */
	VK_SAMPLER_MIPMAP_MODE_LINEAR, 	/* FNA3D_TEXTUREFILTER_ANISOTROPIC */
	VK_SAMPLER_MIPMAP_MODE_NEAREST,	/* FNA3D_TEXTUREFILTER_LINEAR_MIPPOINT */
	VK_SAMPLER_MIPMAP_MODE_LINEAR, 	/* FNA3D_TEXTUREFILTER_POINT_MIPLINEAR */
	VK_SAMPLER_MIPMAP_MODE_LINEAR,	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPLINEAR */
	VK_SAMPLER_MIPMAP_MODE_NEAREST, /* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPPOINT */
	VK_SAMPLER_MIPMAP_MODE_LINEAR, 	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPLINEAR */
	VK_SAMPLER_MIPMAP_MODE_NEAREST, /* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPPOINT */
};

static VkFilter XNAToVK_MinFilter[] =
{
	VK_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_LINEAR */
	VK_FILTER_NEAREST, 	/* FNA3D_TEXTUREFILTER_POINT */
	VK_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_ANISOTROPIC */
	VK_FILTER_LINEAR,	/* FNA3D_TEXTUREFILTER_LINEAR_MIPPOINT */
	VK_FILTER_NEAREST, 	/* FNA3D_TEXTUREFILTER_POINT_MIPLINEAR */
	VK_FILTER_LINEAR,	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPLINEAR */
	VK_FILTER_LINEAR, 	/* FNA3D_TEXTUREFILTER_MINLINEAR_MAGPOINT_MIPPOINT */
	VK_FILTER_NEAREST, 	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPLINEAR */
	VK_FILTER_NEAREST, 	/* FNA3D_TEXTUREFILTER_MINPOINT_MAGLINEAR_MIPPOINT */
};

static VkCompareOp XNAToVK_CompareOp[] =
{
	VK_COMPARE_OP_ALWAYS, 		/* FNA3D_COMPAREFUNCTION_ALWAYS */
	VK_COMPARE_OP_NEVER, 		/* FNA3D_COMPAREFUNCTION_NEVER */
	VK_COMPARE_OP_LESS,		/* FNA3D_COMPAREFUNCTION_LESS */
	VK_COMPARE_OP_LESS_OR_EQUAL, 	/* FNA3D_COMPAREFUNCTION_LESSEQUAL */
	VK_COMPARE_OP_EQUAL,		/* FNA3D_COMPAREFUNCTION_EQUAL */
	VK_COMPARE_OP_GREATER_OR_EQUAL,	/* FNA3D_COMPAREFUNCTION_GREATEREQUAL */
	VK_COMPARE_OP_GREATER,		/* FNA3D_COMPAREFUNCTION_GREATER */
	VK_COMPARE_OP_NOT_EQUAL		/* FNA3D_COMPAREFUNCTION_NOTEQUAL */
};

static VkStencilOp XNAToVK_StencilOp[] =
{
	VK_STENCIL_OP_KEEP,			/* FNA3D_STENCILOPERATION_KEEP */
	VK_STENCIL_OP_ZERO,			/* FNA3D_STENCILOPERATION_ZERO */
	VK_STENCIL_OP_REPLACE,			/* FNA3D_STENCILOPERATION_REPLACE */
	VK_STENCIL_OP_INCREMENT_AND_WRAP, 	/* FNA3D_STENCILOPERATION_INCREMENT */
	VK_STENCIL_OP_DECREMENT_AND_WRAP,	/* FNA3D_STENCILOPERATION_DECREMENT */
	VK_STENCIL_OP_INCREMENT_AND_CLAMP, 	/* FNA3D_STENCILOPERATION_INCREMENTSATURATION */
	VK_STENCIL_OP_DECREMENT_AND_CLAMP,	/* FNA3D_STENCILOPERATION_DECREMENTSATURATION */
	VK_STENCIL_OP_INVERT			/* FNA3D_STENCILOPERATION_INVERT */
};

static VkFormat XNAToVK_VertexAttribType[] =
{
	VK_FORMAT_R32_SFLOAT,		/* FNA3D_VERTEXELEMENTFORMAT_SINGLE */
	VK_FORMAT_R32G32_SFLOAT,	/* FNA3D_VERTEXELEMENTFORMAT_VECTOR2 */
	VK_FORMAT_R32G32B32_SFLOAT,	/* FNA3D_VERTEXELEMENTFORMAT_VECTOR3 */
	VK_FORMAT_R32G32B32A32_SFLOAT, 	/* FNA3D_VERTEXELEMENTFORMAT_VECTOR4 */
	VK_FORMAT_R8G8B8A8_UNORM,	/* FNA3D_VERTEXELEMENTFORMAT_COLOR */
	VK_FORMAT_R8G8B8A8_USCALED,	/* FNA3D_VERTEXELEMENTFORMAT_BYTE4 */
	VK_FORMAT_R16G16_SSCALED, 	/* FNA3D_VERTEXELEMENTFORMAT_SHORT2 */
	VK_FORMAT_R16G16B16A16_SSCALED,	/* FNA3D_VERTEXELEMENTFORMAT_SHORT4 */
	VK_FORMAT_R16G16_SNORM,		/* FNA3D_VERTEXELEMENTFORMAT_NORMALIZEDSHORT2 */
	VK_FORMAT_R16G16B16A16_SNORM,	/* FNA3D_VERTEXELEMENTFORMAT_NORMALIZEDSHORT4 */
	VK_FORMAT_R16G16_SFLOAT,	/* FNA3D_VERTEXELEMENTFORMAT_HALFVECTOR2 */
	VK_FORMAT_R16G16B16A16_SFLOAT	/* FNA3D_VERTEXELEMENTFORMAT_HALFVECTOR4 */
};

/* Error Handling */

static inline const char* VkErrorMessages(VkResult code)
{
	#define ERR_TO_STR(e) \
		case e: return #e;
	switch (code)
	{
		ERR_TO_STR(VK_ERROR_OUT_OF_HOST_MEMORY)
		ERR_TO_STR(VK_ERROR_OUT_OF_DEVICE_MEMORY)
		ERR_TO_STR(VK_ERROR_FRAGMENTED_POOL)
		ERR_TO_STR(VK_ERROR_OUT_OF_POOL_MEMORY)
		ERR_TO_STR(VK_ERROR_INITIALIZATION_FAILED)
		ERR_TO_STR(VK_ERROR_LAYER_NOT_PRESENT)
		ERR_TO_STR(VK_ERROR_EXTENSION_NOT_PRESENT)
		ERR_TO_STR(VK_ERROR_FEATURE_NOT_PRESENT)
		ERR_TO_STR(VK_ERROR_TOO_MANY_OBJECTS)
		ERR_TO_STR(VK_ERROR_DEVICE_LOST)
		ERR_TO_STR(VK_ERROR_INCOMPATIBLE_DRIVER)
		ERR_TO_STR(VK_ERROR_OUT_OF_DATE_KHR)
		ERR_TO_STR(VK_ERROR_SURFACE_LOST_KHR)
		ERR_TO_STR(VK_ERROR_FULL_SCREEN_EXCLUSIVE_MODE_LOST_EXT)
		ERR_TO_STR(VK_SUBOPTIMAL_KHR)
		default: return "Unhandled VkResult!";
	}
	#undef ERR_TO_STR
}

#define VULKAN_ERROR_CHECK(res, fn, ret) \
	if (res != VK_SUCCESS) \
	{ \
		FNA3D_LogError("%s %s", #fn, VkErrorMessages(res)); \
		return ret; \
	}

/* Forward Declarations */

static void VULKAN_GetBackbufferSize(
	FNA3D_Renderer *driverData,
	int32_t *w,
	int32_t *h
);

static void VULKAN_GetTextureData2D(
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

static void VULKAN_INTERNAL_BufferMemoryBarrier(
	VulkanRenderer *renderer,
	VulkanResourceAccessType nextResourceAccessType,
	VkBuffer buffer,
	VulkanResourceAccessType *resourceAccessType
);

static void VULKAN_INTERNAL_ImageMemoryBarrier(
	VulkanRenderer *renderer,
	VulkanResourceAccessType nextAccess,
	VkImageAspectFlags aspectMask,
	uint32_t baseLayer,
	uint32_t layerCount,
	uint32_t baseLevel,
	uint32_t levelCount,
	uint8_t discardContents,
	VkImage image,
	VulkanResourceAccessType *resourceAccessType
);

static void VULKAN_INTERNAL_RemoveBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
);

static void VULKAN_INTERNAL_RecreateSwapchain(VulkanRenderer *renderer, uint8_t flush);

static void VULKAN_INTERNAL_MaybeEndRenderPass(VulkanRenderer *renderer, uint8_t allowBreak);

static void VULKAN_INTERNAL_FlushCommands(VulkanRenderer *renderer, uint8_t sync);

static void VULKAN_INTERNAL_PerformDeferredDestroys(VulkanRenderer *renderer);

static void ShaderResources_InvalidateDescriptorSet(VulkanRenderer* renderer, VkImageView view);

/* Vulkan: Internal Implementation */

/* Vulkan: Extensions */

static inline uint8_t SupportsInstanceExtension(
	const char *ext,
	VkExtensionProperties *availableExtensions,
	uint32_t numAvailableExtensions
) {
	uint32_t i;
	for (i = 0; i < numAvailableExtensions; i += 1)
	{
		if (SDL_strcmp(ext, availableExtensions[i].extensionName) == 0)
		{
			return 1;
		}
	}
	return 0;
}

static uint8_t VULKAN_INTERNAL_CheckInstanceExtensions(
	const char **requiredExtensions,
	uint32_t requiredExtensionsLength,
	uint8_t *supportsDebugUtils
) {
	uint32_t extensionCount, i;
	VkExtensionProperties *availableExtensions;
	uint8_t allExtensionsSupported = 1;

	vkEnumerateInstanceExtensionProperties(
		NULL,
		&extensionCount,
		NULL
	);
	availableExtensions = SDL_malloc(
		extensionCount * sizeof(VkExtensionProperties)
	);
	vkEnumerateInstanceExtensionProperties(
		NULL,
		&extensionCount,
		availableExtensions
	);

	for (i = 0; i < requiredExtensionsLength; i += 1)
	{
		if (!SupportsInstanceExtension(
			requiredExtensions[i],
			availableExtensions,
			extensionCount
		)) {
			allExtensionsSupported = 0;
			break;
		}
	}

	/* This is optional, but nice to have! */
	*supportsDebugUtils = SupportsInstanceExtension(
		VK_EXT_DEBUG_UTILS_EXTENSION_NAME,
		availableExtensions,
		extensionCount
	);

	SDL_free(availableExtensions);
	return allExtensionsSupported;
}

static uint8_t VULKAN_INTERNAL_CheckDeviceExtensions(
	VulkanRenderer *renderer,
	VkPhysicalDevice physicalDevice,
	VulkanExtensions *physicalDeviceExtensions
) {
	uint32_t extensionCount;
	VkExtensionProperties *availableExtensions;
	uint8_t allExtensionsSupported;

	renderer->vkEnumerateDeviceExtensionProperties(
		physicalDevice,
		NULL,
		&extensionCount,
		NULL
	);
	availableExtensions = (VkExtensionProperties*) SDL_malloc(
		extensionCount * sizeof(VkExtensionProperties)
	);
	renderer->vkEnumerateDeviceExtensionProperties(
		physicalDevice,
		NULL,
		&extensionCount,
		availableExtensions
	);

	allExtensionsSupported = CheckDeviceExtensions(
		availableExtensions,
		extensionCount,
		physicalDeviceExtensions
	);

	SDL_free(availableExtensions);
	return allExtensionsSupported;
}

/* Vulkan: Validation Layers */

static uint8_t VULKAN_INTERNAL_CheckValidationLayers(
	const char** validationLayers,
	uint32_t validationLayersLength
) {
	uint32_t layerCount;
	VkLayerProperties *availableLayers;
	uint32_t i, j;
	uint8_t layerFound;

	vkEnumerateInstanceLayerProperties(&layerCount, NULL);
	availableLayers = (VkLayerProperties*) SDL_malloc(
		layerCount * sizeof(VkLayerProperties)
	);
	vkEnumerateInstanceLayerProperties(&layerCount, availableLayers);

	for (i = 0; i < validationLayersLength; i += 1)
	{
		layerFound = 0;

		for (j = 0; j < layerCount; j += 1)
		{
			if (SDL_strcmp(validationLayers[i], availableLayers[j].layerName) == 0)
			{
				layerFound = 1;
				break;
			}
		}

		if (!layerFound)
		{
			break;
		}
	}

	SDL_free(availableLayers);
	return layerFound;
}

/* Vulkan: Device Feature Queries */

static uint8_t VULKAN_INTERNAL_QuerySwapChainSupport(
	VulkanRenderer *renderer,
	VkPhysicalDevice physicalDevice,
	VkSurfaceKHR surface,
	SwapChainSupportDetails *outputDetails
) {
	VkResult result;
	uint32_t formatCount;
	uint32_t presentModeCount;

	result = renderer->vkGetPhysicalDeviceSurfaceCapabilitiesKHR(
		physicalDevice,
		surface,
		&outputDetails->capabilities
	);
	VULKAN_ERROR_CHECK(result, vkGetPhysicalDeviceSurfaceCapabilitiesKHR, 0)

	renderer->vkGetPhysicalDeviceSurfaceFormatsKHR(
		physicalDevice,
		surface,
		&formatCount,
		NULL
	);

	if (formatCount != 0)
	{
		outputDetails->formats = (VkSurfaceFormatKHR*) SDL_malloc(
			sizeof(VkSurfaceFormatKHR) * formatCount
		);
		outputDetails->formatsLength = formatCount;

		if (!outputDetails->formats)
		{
			SDL_OutOfMemory();
			return 0;
		}

		result = renderer->vkGetPhysicalDeviceSurfaceFormatsKHR(
			physicalDevice,
			surface,
			&formatCount,
			outputDetails->formats
		);
		if (result != VK_SUCCESS)
		{
			FNA3D_LogError(
				"vkGetPhysicalDeviceSurfaceFormatsKHR: %s",
				VkErrorMessages(result)
			);

			SDL_free(outputDetails->formats);
			return 0;
		}
	}

	renderer->vkGetPhysicalDeviceSurfacePresentModesKHR(
		physicalDevice,
		surface,
		&presentModeCount,
		NULL
	);

	if (presentModeCount != 0)
	{
		outputDetails->presentModes = (VkPresentModeKHR*) SDL_malloc(
			sizeof(VkPresentModeKHR) * presentModeCount
		);
		outputDetails->presentModesLength = presentModeCount;

		if (!outputDetails->presentModes)
		{
			SDL_OutOfMemory();
			return 0;
		}

		result = renderer->vkGetPhysicalDeviceSurfacePresentModesKHR(
			physicalDevice,
			surface,
			&presentModeCount,
			outputDetails->presentModes
		);
		if (result != VK_SUCCESS)
		{
			FNA3D_LogError(
				"vkGetPhysicalDeviceSurfacePresentModesKHR: %s",
				VkErrorMessages(result)
			);

			SDL_free(outputDetails->formats);
			SDL_free(outputDetails->presentModes);
			return 0;
		}
	}

	return 1;
}

static uint8_t VULKAN_INTERNAL_ChooseSwapSurfaceFormat(
	VkFormat desiredFormat,
	VkSurfaceFormatKHR *availableFormats,
	uint32_t availableFormatsLength,
	VkSurfaceFormatKHR *outputFormat
) {
	uint32_t i;
	for (i = 0; i < availableFormatsLength; i += 1)
	{
		if (	availableFormats[i].format == desiredFormat &&
			availableFormats[i].colorSpace == VK_COLOR_SPACE_SRGB_NONLINEAR_KHR	)
		{
			*outputFormat = availableFormats[i];
			return 1;
		}
	}
	return 0;
}

static uint8_t VULKAN_INTERNAL_ChooseSwapPresentMode(
	FNA3D_PresentInterval desiredPresentInterval,
	VkPresentModeKHR *availablePresentModes,
	uint32_t availablePresentModesLength,
	VkPresentModeKHR *outputPresentMode
) {
	#define CHECK_MODE(m) \
		for (i = 0; i < availablePresentModesLength; i += 1) \
		{ \
			if (availablePresentModes[i] == m) \
			{ \
				*outputPresentMode = m; \
				FNA3D_LogInfo("Using " #m "!"); \
				return 1; \
			} \
		} \
		FNA3D_LogInfo(#m " unsupported.");

	uint32_t i;
	if (	desiredPresentInterval == FNA3D_PRESENTINTERVAL_DEFAULT ||
		desiredPresentInterval == FNA3D_PRESENTINTERVAL_ONE	)
	{
		if (SDL_GetHintBoolean("FNA3D_DISABLE_LATESWAPTEAR", 0))
		{
			*outputPresentMode = VK_PRESENT_MODE_FIFO_KHR;
			return 1;
		}
		if (SDL_GetHintBoolean("FNA3D_VULKAN_FORCE_MAILBOX_VSYNC", 0))
		{
			CHECK_MODE(VK_PRESENT_MODE_MAILBOX_KHR)
		}
		CHECK_MODE(VK_PRESENT_MODE_FIFO_RELAXED_KHR)
	}
	else if (desiredPresentInterval ==  FNA3D_PRESENTINTERVAL_IMMEDIATE)
	{
		CHECK_MODE(VK_PRESENT_MODE_IMMEDIATE_KHR)
	}
	else if (desiredPresentInterval == FNA3D_PRESENTINTERVAL_TWO)
	{
		FNA3D_LogError("FNA3D_PRESENTINTERVAL_TWO not supported in Vulkan");
		return 0;
	}
	else
	{
		FNA3D_LogError(
			"Unrecognized PresentInterval: %d",
			desiredPresentInterval
		);
		return 0;
	}

	#undef CHECK_MODE

	FNA3D_LogInfo("Fall back to VK_PRESENT_MODE_FIFO_KHR.");
	*outputPresentMode = VK_PRESENT_MODE_FIFO_KHR;
	return 1;
}

static uint8_t VULKAN_INTERNAL_FindMemoryType(
	VulkanRenderer *renderer,
	uint32_t typeFilter,
	VkMemoryPropertyFlags requiredProperties,
	VkMemoryPropertyFlags ignoredProperties,
	uint32_t *result
) {
	uint32_t i;

	for (i = 0; i < renderer->memoryProperties.memoryTypeCount; i += 1)
	{
		if (	(typeFilter & (1 << i)) &&
			(renderer->memoryProperties.memoryTypes[i].propertyFlags & requiredProperties) == requiredProperties &&
			(renderer->memoryProperties.memoryTypes[i].propertyFlags & ignoredProperties) == 0	)
		{
			*result = i;
			return 1;
		}
	}

	FNA3D_LogWarn(
		"Failed to find memory type %X, required %X, ignored %X",
		typeFilter,
		requiredProperties,
		ignoredProperties
	);
	return 0;
}

static uint8_t VULKAN_INTERNAL_IsDeviceSuitable(
	VulkanRenderer *renderer,
	VkPhysicalDevice physicalDevice,
	VulkanExtensions *physicalDeviceExtensions,
	VkSurfaceKHR surface,
	uint32_t *queueFamilyIndex,
	uint8_t *deviceRank
) {
	uint32_t queueFamilyCount, i;
	SwapChainSupportDetails swapChainSupportDetails;
	VkQueueFamilyProperties *queueProps;
	VkBool32 supportsPresent;
	uint8_t querySuccess, foundSuitableDevice = 0;
	VkPhysicalDeviceProperties deviceProperties;

	*queueFamilyIndex = UINT32_MAX;
	*deviceRank = 0;

	/* Note: If no dedicated device exists,
	 * one that supports our features would be fine
	 */

	if (!VULKAN_INTERNAL_CheckDeviceExtensions(
		renderer,
		physicalDevice,
		physicalDeviceExtensions
	)) {
		return 0;
	}

	renderer->vkGetPhysicalDeviceQueueFamilyProperties(
		physicalDevice,
		&queueFamilyCount,
		NULL
	);

	/* FIXME: Need better structure for checking vs storing support details */
	querySuccess = VULKAN_INTERNAL_QuerySwapChainSupport(
		renderer,
		physicalDevice,
		surface,
		&swapChainSupportDetails
	);
	SDL_free(swapChainSupportDetails.formats);
	SDL_free(swapChainSupportDetails.presentModes);
	if (	querySuccess == 0 ||
		swapChainSupportDetails.formatsLength == 0 ||
		swapChainSupportDetails.presentModesLength == 0	)
	{
		return 0;
	}

	queueProps = (VkQueueFamilyProperties*) SDL_stack_alloc(
		VkQueueFamilyProperties,
		queueFamilyCount
	);
	renderer->vkGetPhysicalDeviceQueueFamilyProperties(
		physicalDevice,
		&queueFamilyCount,
		queueProps
	);

	for (i = 0; i < queueFamilyCount; i += 1)
	{
		renderer->vkGetPhysicalDeviceSurfaceSupportKHR(
			physicalDevice,
			i,
			surface,
			&supportsPresent
		);
		if (	supportsPresent &&
			(queueProps[i].queueFlags & VK_QUEUE_GRAPHICS_BIT) != 0	&&
			(queueProps[i].queueFlags & VK_QUEUE_COMPUTE_BIT) != 0 &&
			(queueProps[i].queueFlags & VK_QUEUE_TRANSFER_BIT) != 0	)
		{
			*queueFamilyIndex = i;
			foundSuitableDevice = 1;
			break;
		}
	}

	SDL_stack_free(queueProps);

	if (foundSuitableDevice)
	{
		/* Try to make sure we pick the best device available */
		renderer->vkGetPhysicalDeviceProperties(
			physicalDevice,
			&deviceProperties
		);
		*deviceRank = DEVICE_PRIORITY[deviceProperties.deviceType];
		return 1;
	}

	/* This device is useless for us, next! */
	return 0;
}

/* Vulkan: vkInstance/vkDevice Creation */

static uint8_t VULKAN_INTERNAL_CreateInstance(
	VulkanRenderer *renderer,
	FNA3D_PresentationParameters *presentationParameters
) {
	VkResult vulkanResult;
	VkApplicationInfo appInfo;
	const char **instanceExtensionNames;
	uint32_t instanceExtensionCount;
	VkInstanceCreateInfo createInfo;
	static const char *layerNames[] = { "VK_LAYER_KHRONOS_validation" };

	appInfo.sType = VK_STRUCTURE_TYPE_APPLICATION_INFO;
	appInfo.pNext = NULL;
	appInfo.pApplicationName = NULL;
	appInfo.applicationVersion = 0;
	appInfo.pEngineName = "FNA3D";
	appInfo.engineVersion = FNA3D_COMPILED_VERSION;
	appInfo.apiVersion = VK_MAKE_VERSION(1, 0, 0);

	if (!SDL_Vulkan_GetInstanceExtensions(
		(SDL_Window*) presentationParameters->deviceWindowHandle,
		&instanceExtensionCount,
		NULL
	)) {
		FNA3D_LogWarn(
			"SDL_Vulkan_GetInstanceExtensions(): getExtensionCount: %s",
			SDL_GetError()
		);

		return 0;
	}

	/* Extra space for the following extensions:
	 * VK_KHR_get_physical_device_properties2
	 * VK_EXT_debug_utils
	 */
	instanceExtensionNames = SDL_stack_alloc(
		const char*,
		instanceExtensionCount + 2
	);

	if (!SDL_Vulkan_GetInstanceExtensions(
		(SDL_Window*) presentationParameters->deviceWindowHandle,
		&instanceExtensionCount,
		instanceExtensionNames
	)) {
		FNA3D_LogWarn(
			"SDL_Vulkan_GetInstanceExtensions(): %s",
			SDL_GetError()
		);
		goto create_instance_fail;
	}

	/* Core since 1.1 */
	instanceExtensionNames[instanceExtensionCount++] =
		VK_KHR_GET_PHYSICAL_DEVICE_PROPERTIES_2_EXTENSION_NAME;

	if (!VULKAN_INTERNAL_CheckInstanceExtensions(
		instanceExtensionNames,
		instanceExtensionCount,
		&renderer->supportsDebugUtils
	)) {
		FNA3D_LogWarn(
			"Required Vulkan instance extensions not supported"
		);
		goto create_instance_fail;
	}

	if (renderer->supportsDebugUtils)
	{
		/* Append the debug extension to the end */
		instanceExtensionNames[instanceExtensionCount++] =
			VK_EXT_DEBUG_UTILS_EXTENSION_NAME;
	}
	else
	{
		FNA3D_LogWarn(
			"%s is not supported!",
			VK_EXT_DEBUG_UTILS_EXTENSION_NAME
		);
	}

	createInfo.sType = VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
	createInfo.pNext = NULL;
	createInfo.flags = 0;
	createInfo.pApplicationInfo = &appInfo;
	createInfo.ppEnabledLayerNames = layerNames;
	createInfo.enabledExtensionCount = instanceExtensionCount;
	createInfo.ppEnabledExtensionNames = instanceExtensionNames;
	if (renderer->debugMode)
	{
		createInfo.enabledLayerCount = SDL_arraysize(layerNames);
		if (VULKAN_INTERNAL_CheckValidationLayers(
			layerNames,
			createInfo.enabledLayerCount
		)) {
			FNA3D_LogInfo("Vulkan validation enabled! Expect debug-level performance!");
		}
		else
		{
			FNA3D_LogWarn("Validation layers not found, continuing without validation");
			createInfo.enabledLayerCount = 0;
		}
	}
	else
	{
		createInfo.enabledLayerCount = 0;
	}

	vulkanResult = vkCreateInstance(&createInfo, NULL, &renderer->instance);
	if (vulkanResult != VK_SUCCESS)
	{
		FNA3D_LogWarn(
			"vkCreateInstance failed: %s",
			VkErrorMessages(vulkanResult)
		);
		goto create_instance_fail;
	}

	SDL_stack_free((char*) instanceExtensionNames);
	return 1;

create_instance_fail:
	SDL_stack_free((char*) instanceExtensionNames);
	return 0;
}

static uint8_t VULKAN_INTERNAL_DeterminePhysicalDevice(VulkanRenderer *renderer)
{
	VkResult vulkanResult;
	VkPhysicalDevice *physicalDevices;
	VulkanExtensions *physicalDeviceExtensions;
	uint32_t physicalDeviceCount, i, suitableIndex;
	uint32_t queueFamilyIndex, suitableQueueFamilyIndex;
	VkDeviceSize deviceLocalHeapSize;
	const char *deviceLocalHeapUsageFactorStr;
	float deviceLocalHeapUsageFactor = 1.0f;
	uint8_t deviceRank, highestRank;

	vulkanResult = renderer->vkEnumeratePhysicalDevices(
		renderer->instance,
		&physicalDeviceCount,
		NULL
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkEnumeratePhysicalDevices, 0)

	if (physicalDeviceCount == 0)
	{
		FNA3D_LogWarn("Failed to find any GPUs with Vulkan support");
		return 0;
	}

	physicalDevices = SDL_stack_alloc(VkPhysicalDevice, physicalDeviceCount);
	physicalDeviceExtensions = SDL_stack_alloc(VulkanExtensions, physicalDeviceCount);

	vulkanResult = renderer->vkEnumeratePhysicalDevices(
		renderer->instance,
		&physicalDeviceCount,
		physicalDevices
	);

	if (vulkanResult != VK_SUCCESS)
	{
		FNA3D_LogWarn(
			"vkEnumeratePhysicalDevices failed: %s",
			VkErrorMessages(vulkanResult)
		);
		SDL_stack_free(physicalDevices);
		SDL_stack_free(physicalDeviceExtensions);
		return 0;
	}

	/* Any suitable device will do, but we'd like the best */
	suitableIndex = -1;
	deviceRank = 0;
	highestRank = 0;
	for (i = 0; i < physicalDeviceCount; i += 1)
	{
		const uint8_t suitable = VULKAN_INTERNAL_IsDeviceSuitable(
			renderer,
			physicalDevices[i],
			&physicalDeviceExtensions[i],
			renderer->surface,
			&queueFamilyIndex,
			&deviceRank
		);
		if (deviceRank >= highestRank)
		{
			/* We found a better device type, but does it work? */
			if (suitable)
			{
				/* Yes, use this for rendering. */
				suitableIndex = i;
				suitableQueueFamilyIndex = queueFamilyIndex;
			}
			else if (deviceRank > highestRank)
			{
				/* In this case, we found a... "realer?" GPU,
				 * but it doesn't actually support our Vulkan.
				 * We should disqualify all devices below as a
				 * result, because if we don't we end up
				 * ignoring real hardware and risk using
				 * something like LLVMpipe instead!
				 * -flibit
				 */
				suitableIndex = -1;
			}
			highestRank = deviceRank;
		}
	}

	if (suitableIndex != -1)
	{
		renderer->supports = physicalDeviceExtensions[suitableIndex];
		renderer->physicalDevice = physicalDevices[suitableIndex];
		renderer->queueFamilyIndex = suitableQueueFamilyIndex;
	}
	else
	{
		SDL_stack_free(physicalDevices);
		SDL_stack_free(physicalDeviceExtensions);
		return 0;
	}

	renderer->physicalDeviceProperties.sType =
		VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2;
	if (renderer->supports.KHR_driver_properties)
	{
		renderer->physicalDeviceDriverProperties.sType =
			VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES_KHR;
		renderer->physicalDeviceDriverProperties.pNext = NULL;

		renderer->physicalDeviceProperties.pNext =
			&renderer->physicalDeviceDriverProperties;
	}
	else
	{
		renderer->physicalDeviceProperties.pNext = NULL;
	}

	renderer->vkGetPhysicalDeviceProperties2KHR(
		renderer->physicalDevice,
		&renderer->physicalDeviceProperties
	);

	renderer->vkGetPhysicalDeviceMemoryProperties(
		renderer->physicalDevice,
		&renderer->memoryProperties
	);

	deviceLocalHeapSize = 0;
	for (i = 0; i < renderer->memoryProperties.memoryHeapCount; i += 1)
	{
		if (	renderer->memoryProperties.memoryHeaps[i].flags &
			VK_MEMORY_HEAP_DEVICE_LOCAL_BIT	)
		{
			if (renderer->memoryProperties.memoryHeaps[i].size > deviceLocalHeapSize)
			{
				deviceLocalHeapSize = renderer->memoryProperties.memoryHeaps[i].size;
			}
		}
	}

	deviceLocalHeapUsageFactorStr = SDL_GetHint("FNA3D_VULKAN_DEVICE_LOCAL_HEAP_USAGE_FACTOR");
	if (deviceLocalHeapUsageFactorStr != NULL)
	{
		double factor = SDL_atof(deviceLocalHeapUsageFactorStr);
		if (factor > 0.0 && factor < 1.0)
		{
			deviceLocalHeapUsageFactor = factor;
		}
	}

	renderer->maxDeviceLocalHeapUsage = deviceLocalHeapSize * deviceLocalHeapUsageFactor;
	renderer->deviceLocalHeapUsage = 0;

	SDL_stack_free(physicalDevices);
	SDL_stack_free(physicalDeviceExtensions);
	return 1;
}

static uint8_t VULKAN_INTERNAL_CreateLogicalDevice(VulkanRenderer *renderer)
{
	VkResult vulkanResult;
	VkDeviceCreateInfo deviceCreateInfo;
	VkPhysicalDeviceFeatures deviceFeatures;
	VkPhysicalDevicePortabilitySubsetFeaturesKHR portabilityFeatures;
	const char **deviceExtensions;

	VkDeviceQueueCreateInfo queueCreateInfo;
	float queuePriority = 1.0f;

	queueCreateInfo.sType =
		VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
	queueCreateInfo.pNext = NULL;
	queueCreateInfo.flags = 0;
	queueCreateInfo.queueFamilyIndex = renderer->queueFamilyIndex;
	queueCreateInfo.queueCount = 1;
	queueCreateInfo.pQueuePriorities = &queuePriority;

	/* specifying used device features */

	SDL_zero(deviceFeatures);
	deviceFeatures.occlusionQueryPrecise = VK_TRUE;
	deviceFeatures.fillModeNonSolid = VK_TRUE;
	deviceFeatures.samplerAnisotropy = VK_TRUE;

	/* Creating the logical device */

	deviceCreateInfo.sType = VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO;
	if (renderer->supports.KHR_portability_subset)
	{
		portabilityFeatures.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PORTABILITY_SUBSET_FEATURES_KHR;
		portabilityFeatures.pNext = NULL;
		portabilityFeatures.constantAlphaColorBlendFactors = VK_FALSE;
		portabilityFeatures.events = VK_FALSE;
		portabilityFeatures.imageViewFormatReinterpretation = VK_FALSE;
		portabilityFeatures.imageViewFormatSwizzle = VK_TRUE;
		portabilityFeatures.imageView2DOn3DImage = VK_FALSE;
		portabilityFeatures.multisampleArrayImage = VK_FALSE;
		portabilityFeatures.mutableComparisonSamplers = VK_FALSE;
		portabilityFeatures.pointPolygons = VK_FALSE;
		portabilityFeatures.samplerMipLodBias = VK_FALSE; /* Technically should be true, but eh */
		portabilityFeatures.separateStencilMaskRef = VK_FALSE;
		portabilityFeatures.shaderSampleRateInterpolationFunctions = VK_FALSE;
		portabilityFeatures.tessellationIsolines = VK_FALSE;
		portabilityFeatures.tessellationPointMode = VK_FALSE;
		portabilityFeatures.triangleFans = VK_FALSE;
		portabilityFeatures.vertexAttributeAccessBeyondStride = VK_FALSE;
		deviceCreateInfo.pNext = &portabilityFeatures;
	}
	else
	{
		deviceCreateInfo.pNext = NULL;
	}
	deviceCreateInfo.flags = 0;
	deviceCreateInfo.queueCreateInfoCount = 1;
	deviceCreateInfo.pQueueCreateInfos = &queueCreateInfo;
	deviceCreateInfo.enabledLayerCount = 0;
	deviceCreateInfo.ppEnabledLayerNames = NULL;
	deviceCreateInfo.enabledExtensionCount = GetDeviceExtensionCount(
		&renderer->supports
	);
	deviceExtensions = SDL_stack_alloc(
		const char*,
		deviceCreateInfo.enabledExtensionCount
	);
	CreateDeviceExtensionArray(&renderer->supports, deviceExtensions);
	deviceCreateInfo.ppEnabledExtensionNames = deviceExtensions;
	deviceCreateInfo.pEnabledFeatures = &deviceFeatures;

	vulkanResult = renderer->vkCreateDevice(
		renderer->physicalDevice,
		&deviceCreateInfo,
		NULL,
		&renderer->logicalDevice
	);
	SDL_stack_free(deviceExtensions);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateDevice, 0)

	/* Load vkDevice entry points */

	#define VULKAN_DEVICE_FUNCTION(name) \
		renderer->name = (PFN_##name) \
			renderer->vkGetDeviceProcAddr( \
				renderer->logicalDevice, \
				#name \
			);
	#include "FNA3D_Driver_Vulkan_vkfuncs.h"

	renderer->vkGetDeviceQueue(
		renderer->logicalDevice,
		renderer->queueFamilyIndex,
		0,
		&renderer->unifiedQueue
	);

	return 1;
}

/* Vulkan: Command Buffers */

static void VULKAN_INTERNAL_BeginCommandBuffer(VulkanRenderer *renderer)
{
	VkCommandBufferAllocateInfo allocateInfo;
	VkCommandBufferBeginInfo beginInfo;
	VkResult result;

	beginInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
	beginInfo.pNext = NULL;
	beginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
	beginInfo.pInheritanceInfo = NULL;

	/* If we are out of unused command buffers, allocate some more */
	if (renderer->inactiveCommandBufferCount == 0)
	{
		renderer->activeCommandBuffers = SDL_realloc(
			renderer->activeCommandBuffers,
			sizeof(VkCommandBuffer) * renderer->allocatedCommandBufferCount * 2
		);

		renderer->inactiveCommandBuffers = SDL_realloc(
			renderer->inactiveCommandBuffers,
			sizeof(VkCommandBuffer) * renderer->allocatedCommandBufferCount * 2
		);

		renderer->submittedCommandBuffers = SDL_realloc(
			renderer->submittedCommandBuffers,
			sizeof(VkCommandBuffer) * renderer->allocatedCommandBufferCount * 2
		);

		allocateInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
		allocateInfo.pNext = NULL;
		allocateInfo.commandPool = renderer->commandPool;
		allocateInfo.commandBufferCount = renderer->allocatedCommandBufferCount;
		allocateInfo.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;

		result = renderer->vkAllocateCommandBuffers(
			renderer->logicalDevice,
			&allocateInfo,
			renderer->inactiveCommandBuffers
		);
		VULKAN_ERROR_CHECK(result, vkAllocateCommandBuffers,)

		renderer->inactiveCommandBufferCount = renderer->allocatedCommandBufferCount;
		renderer->allocatedCommandBufferCount *= 2;
	}

	renderer->currentCommandBuffer =
		renderer->inactiveCommandBuffers[renderer->inactiveCommandBufferCount - 1];

	renderer->activeCommandBuffers[renderer->activeCommandBufferCount] = renderer->currentCommandBuffer;

	renderer->activeCommandBufferCount += 1;
	renderer->inactiveCommandBufferCount -= 1;

	result = renderer->vkBeginCommandBuffer(
		renderer->currentCommandBuffer,
		&beginInfo
	);
	VULKAN_ERROR_CHECK(result, vkBeginCommandBuffer,)
}

static void VULKAN_INTERNAL_EndCommandBuffer(
	VulkanRenderer *renderer,
	uint8_t startNext,
	uint8_t allowFlush
) {
	VkResult result;

	if (renderer->renderPassInProgress)
	{
		VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 0);
		renderer->needNewRenderPass = 1;
	}

	result = renderer->vkEndCommandBuffer(
		renderer->currentCommandBuffer
	);
	VULKAN_ERROR_CHECK(result, vkEndCommandBuffer,)

	renderer->currentCommandBuffer = NULL;
	renderer->numActiveCommands = 0;

	if (allowFlush)
	{
		/* TODO: Figure out how to properly submit commands mid-frame */
	}

	if (startNext)
	{
		VULKAN_INTERNAL_BeginCommandBuffer(renderer);
	}
}

/* Vulkan: Memory Allocation */

static inline VkDeviceSize VULKAN_INTERNAL_NextHighestAlignment(
	VkDeviceSize n,
	VkDeviceSize align
) {
	return align * ((n + align - 1) / align);
}

static void VULKAN_INTERNAL_MakeMemoryUnavailable(
	VulkanRenderer* renderer,
	VulkanMemoryAllocation *allocation
) {
	uint32_t i, j;
	VulkanMemoryFreeRegion *freeRegion;

	allocation->availableForAllocation = 0;

	for (i = 0; i < allocation->freeRegionCount; i += 1)
	{
		freeRegion = allocation->freeRegions[i];

		/* close the gap in the sorted list */
		if (allocation->allocator->sortedFreeRegionCount > 1)
		{
			for (j = freeRegion->sortedIndex; j < allocation->allocator->sortedFreeRegionCount - 1; j += 1)
			{
				allocation->allocator->sortedFreeRegions[j] =
					allocation->allocator->sortedFreeRegions[j + 1];

				allocation->allocator->sortedFreeRegions[j]->sortedIndex = j;
			}
		}

		allocation->allocator->sortedFreeRegionCount -= 1;
	}
}

static void VULKAN_INTERNAL_RemoveMemoryFreeRegion(
	VulkanRenderer *renderer,
	VulkanMemoryFreeRegion *freeRegion
) {
	uint32_t i;

	SDL_LockMutex(renderer->allocatorLock);

	if (freeRegion->allocation->availableForAllocation)
	{
		/* close the gap in the sorted list */
		if (freeRegion->allocation->allocator->sortedFreeRegionCount > 1)
		{
			for (i = freeRegion->sortedIndex; i < freeRegion->allocation->allocator->sortedFreeRegionCount - 1; i += 1)
			{
				freeRegion->allocation->allocator->sortedFreeRegions[i] =
					freeRegion->allocation->allocator->sortedFreeRegions[i + 1];

				freeRegion->allocation->allocator->sortedFreeRegions[i]->sortedIndex = i;
			}
		}

		freeRegion->allocation->allocator->sortedFreeRegionCount -= 1;
	}

	/* close the gap in the buffer list */
	if (freeRegion->allocation->freeRegionCount > 1 && freeRegion->allocationIndex != freeRegion->allocation->freeRegionCount - 1)
	{
		freeRegion->allocation->freeRegions[freeRegion->allocationIndex] =
			freeRegion->allocation->freeRegions[freeRegion->allocation->freeRegionCount - 1];

		freeRegion->allocation->freeRegions[freeRegion->allocationIndex]->allocationIndex =
			freeRegion->allocationIndex;
	}

	freeRegion->allocation->freeRegionCount -= 1;

	freeRegion->allocation->freeSpace -= freeRegion->size;

	SDL_free(freeRegion);

	SDL_UnlockMutex(renderer->allocatorLock);
}

static void VULKAN_INTERNAL_NewMemoryFreeRegion(
	VulkanRenderer *renderer,
	VulkanMemoryAllocation *allocation,
	VkDeviceSize offset,
	VkDeviceSize size
) {
	VulkanMemoryFreeRegion *newFreeRegion;
	VkDeviceSize newOffset, newSize;
	int32_t insertionIndex = 0;
	int32_t i;

	SDL_LockMutex(renderer->allocatorLock);

	/* look for an adjacent region to merge */
	for (i = allocation->freeRegionCount - 1; i >= 0; i -= 1)
	{
		/* check left side */
		if (allocation->freeRegions[i]->offset + allocation->freeRegions[i]->size == offset)
		{
			newOffset = allocation->freeRegions[i]->offset;
			newSize = allocation->freeRegions[i]->size + size;

			VULKAN_INTERNAL_RemoveMemoryFreeRegion(renderer, allocation->freeRegions[i]);
			VULKAN_INTERNAL_NewMemoryFreeRegion(renderer, allocation, newOffset, newSize);

			SDL_UnlockMutex(renderer->allocatorLock);
			return;
		}

		/* check right side */
		if (allocation->freeRegions[i]->offset == offset + size)
		{
			newOffset = offset;
			newSize = allocation->freeRegions[i]->size + size;

			VULKAN_INTERNAL_RemoveMemoryFreeRegion(renderer, allocation->freeRegions[i]);
			VULKAN_INTERNAL_NewMemoryFreeRegion(renderer, allocation, newOffset, newSize);

			SDL_UnlockMutex(renderer->allocatorLock);
			return;
		}
	}

	/* region is not contiguous with another free region, make a new one */
	allocation->freeRegionCount += 1;
	if (allocation->freeRegionCount > allocation->freeRegionCapacity)
	{
		allocation->freeRegionCapacity *= 2;
		allocation->freeRegions = SDL_realloc(
			allocation->freeRegions,
			sizeof(VulkanMemoryFreeRegion*) * allocation->freeRegionCapacity
		);
	}

	newFreeRegion = SDL_malloc(sizeof(VulkanMemoryFreeRegion));
	newFreeRegion->offset = offset;
	newFreeRegion->size = size;
	newFreeRegion->allocation = allocation;

	allocation->freeSpace += size;

	allocation->freeRegions[allocation->freeRegionCount - 1] = newFreeRegion;
	newFreeRegion->allocationIndex = allocation->freeRegionCount - 1;

	if (allocation->availableForAllocation)
	{
		for (i = 0; i < allocation->allocator->sortedFreeRegionCount; i += 1)
		{
			if (allocation->allocator->sortedFreeRegions[i]->size < size)
			{
				/* this is where the new region should go */
				break;
			}

			insertionIndex += 1;
		}

		if (allocation->allocator->sortedFreeRegionCount + 1 > allocation->allocator->sortedFreeRegionCapacity)
		{
			allocation->allocator->sortedFreeRegionCapacity *= 2;
			allocation->allocator->sortedFreeRegions = SDL_realloc(
				allocation->allocator->sortedFreeRegions,
				sizeof(VulkanMemoryFreeRegion*) * allocation->allocator->sortedFreeRegionCapacity
			);
		}

		/* perform insertion sort */
		if (allocation->allocator->sortedFreeRegionCount > 0 && insertionIndex != allocation->allocator->sortedFreeRegionCount)
		{
			for (i = allocation->allocator->sortedFreeRegionCount; i > insertionIndex && i > 0; i -= 1)
			{
				allocation->allocator->sortedFreeRegions[i] = allocation->allocator->sortedFreeRegions[i - 1];
				allocation->allocator->sortedFreeRegions[i]->sortedIndex = i;
			}
		}

		allocation->allocator->sortedFreeRegionCount += 1;
		allocation->allocator->sortedFreeRegions[insertionIndex] = newFreeRegion;
		newFreeRegion->sortedIndex = insertionIndex;
	}

	SDL_UnlockMutex(renderer->allocatorLock);
}

static VulkanMemoryUsedRegion* VULKAN_INTERNAL_NewMemoryUsedRegion(
	VulkanRenderer *renderer,
	VulkanMemoryAllocation *allocation,
	VkDeviceSize offset,
	VkDeviceSize size,
	VkDeviceSize resourceOffset,
	VkDeviceSize resourceSize,
	VkDeviceSize alignment
) {
	SDL_LockMutex(renderer->allocatorLock);

	VulkanMemoryUsedRegion *memoryUsedRegion;

	if (allocation->usedRegionCount == allocation->usedRegionCapacity)
	{
		allocation->usedRegionCapacity *= 2;
		allocation->usedRegions = SDL_realloc(
			allocation->usedRegions,
			allocation->usedRegionCapacity * sizeof(VulkanMemoryUsedRegion*)
		);
	}

	memoryUsedRegion = SDL_malloc(sizeof(VulkanMemoryUsedRegion));
	memoryUsedRegion->allocation = allocation;
	memoryUsedRegion->offset = offset;
	memoryUsedRegion->size = size;
	memoryUsedRegion->resourceOffset = resourceOffset;
	memoryUsedRegion->resourceSize = resourceSize;
	memoryUsedRegion->alignment = alignment;

	allocation->usedSpace += size;

	allocation->usedRegions[allocation->usedRegionCount] = memoryUsedRegion;
	allocation->usedRegionCount += 1;

	SDL_UnlockMutex(renderer->allocatorLock);

	return memoryUsedRegion;
}

static void VULKAN_INTERNAL_RemoveMemoryUsedRegion(
	VulkanRenderer *renderer,
	VulkanMemoryUsedRegion *usedRegion
) {
	SDL_LockMutex(renderer->allocatorLock);

	uint32_t i;

	for (i = 0; i < usedRegion->allocation->usedRegionCount; i += 1)
	{
		if (usedRegion->allocation->usedRegions[i] == usedRegion)
		{
			/* plug the hole */
			if (i != usedRegion->allocation->usedRegionCount - 1)
			{
				usedRegion->allocation->usedRegions[i] = usedRegion->allocation->usedRegions[usedRegion->allocation->usedRegionCount - 1];
			}

			break;
		}
	}

	usedRegion->allocation->usedSpace -= usedRegion->size;

	usedRegion->allocation->usedRegionCount -= 1;

	VULKAN_INTERNAL_NewMemoryFreeRegion(
		renderer,
		usedRegion->allocation,
		usedRegion->offset,
		usedRegion->size
	);

	if (!usedRegion->allocation->dedicated)
	{
		renderer->needDefrag = 1;
	}

	SDL_free(usedRegion);

	renderer->resourceFreed = 1;
	SDL_UnlockMutex(renderer->allocatorLock);
}

static uint8_t VULKAN_INTERNAL_FindBufferMemoryRequirements(
	VulkanRenderer *renderer,
	VkBuffer buffer,
	VkMemoryPropertyFlags requiredMemoryProperties,
	VkMemoryPropertyFlags ignoredMemoryProperties,
	VkMemoryRequirements2KHR *pMemoryRequirements,
	uint32_t *pMemoryTypeIndex
) {
	VkBufferMemoryRequirementsInfo2KHR bufferRequirementsInfo;
	bufferRequirementsInfo.sType =
		VK_STRUCTURE_TYPE_BUFFER_MEMORY_REQUIREMENTS_INFO_2_KHR;
	bufferRequirementsInfo.pNext = NULL;
	bufferRequirementsInfo.buffer = buffer;

	renderer->vkGetBufferMemoryRequirements2KHR(
		renderer->logicalDevice,
		&bufferRequirementsInfo,
		pMemoryRequirements
	);

	return VULKAN_INTERNAL_FindMemoryType(
		renderer,
		pMemoryRequirements->memoryRequirements.memoryTypeBits,
		requiredMemoryProperties,
		ignoredMemoryProperties,
		pMemoryTypeIndex
	);
}

static uint8_t VULKAN_INTERNAL_FindImageMemoryRequirements(
	VulkanRenderer *renderer,
	VkImage image,
	VkMemoryPropertyFlags requiredMemoryPropertyFlags,
	VkMemoryPropertyFlags ignoredMemoryPropertyFlags,
	VkMemoryRequirements2KHR *pMemoryRequirements,
	uint32_t *pMemoryTypeIndex
) {
	VkImageMemoryRequirementsInfo2KHR imageRequirementsInfo;
	imageRequirementsInfo.sType =
		VK_STRUCTURE_TYPE_IMAGE_MEMORY_REQUIREMENTS_INFO_2_KHR;
	imageRequirementsInfo.pNext = NULL;
	imageRequirementsInfo.image = image;

	renderer->vkGetImageMemoryRequirements2KHR(
		renderer->logicalDevice,
		&imageRequirementsInfo,
		pMemoryRequirements
	);

	return VULKAN_INTERNAL_FindMemoryType(
		renderer,
		pMemoryRequirements->memoryRequirements.memoryTypeBits,
		requiredMemoryPropertyFlags,
		ignoredMemoryPropertyFlags,
		pMemoryTypeIndex
	);

	return 1;
}

static void VULKAN_INTERNAL_DeallocateMemory(
	VulkanRenderer *renderer,
	VulkanMemorySubAllocator *allocator,
	uint32_t allocationIndex
) {
	uint32_t i;
	uint8_t isDeviceLocal =
		(renderer->memoryProperties.memoryTypes[allocator->memoryTypeIndex].propertyFlags &
		VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT) != 0;

	VulkanMemoryAllocation *allocation = allocator->allocations[allocationIndex];

	SDL_LockMutex(renderer->allocatorLock);

	for (i = 0; i < allocation->freeRegionCount; i += 1)
	{
		VULKAN_INTERNAL_RemoveMemoryFreeRegion(
			renderer,
			allocation->freeRegions[i]
		);
	}
	SDL_free(allocation->freeRegions);

	/* no need to iterate used regions because deallocate
	 * only happens when there are 0 used regions
	 */
	SDL_free(allocation->usedRegions);

	renderer->vkFreeMemory(
		renderer->logicalDevice,
		allocation->memory,
		NULL
	);

	if (isDeviceLocal)
	{
		renderer->deviceLocalHeapUsage -= allocation->size;
	}

	SDL_DestroyMutex(allocation->mapLock);
	SDL_free(allocation);

	if (allocationIndex != allocator->allocationCount - 1)
	{
		allocator->allocations[allocationIndex] = allocator->allocations[allocator->allocationCount - 1];
	}

	allocator->allocationCount -= 1;

	SDL_UnlockMutex(renderer->allocatorLock);
}

static uint8_t VULKAN_INTERNAL_AllocateMemory(
	VulkanRenderer *renderer,
	VkBuffer buffer,
	VkImage image,
	uint32_t memoryTypeIndex,
	VkDeviceSize allocationSize,
	uint8_t dedicated,
	uint8_t isHostVisible,
	VulkanMemoryAllocation **pMemoryAllocation
) {
	VulkanMemoryAllocation *allocation;
	VulkanMemorySubAllocator *allocator = &renderer->memoryAllocator->subAllocators[memoryTypeIndex];
	VkMemoryAllocateInfo allocInfo;
	VkMemoryDedicatedAllocateInfoKHR dedicatedInfo;
	VkResult result;

	allocInfo.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO;
	allocInfo.memoryTypeIndex = memoryTypeIndex;
	allocInfo.allocationSize = allocationSize;

	allocation = SDL_malloc(sizeof(VulkanMemoryAllocation));
	allocation->size = allocationSize;
	allocation->freeSpace = 0; /* added by FreeRegions */
	allocation->usedSpace = 0; /* added by UsedRegions */
	allocation->mapLock = SDL_CreateMutex();

	allocator->allocationCount += 1;
	allocator->allocations = SDL_realloc(
		allocator->allocations,
		sizeof(VulkanMemoryAllocation*) * allocator->allocationCount
	);

	allocator->allocations[
		allocator->allocationCount - 1
	] = allocation;

	if (dedicated)
	{
		dedicatedInfo.sType =
			VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO_KHR;
		dedicatedInfo.pNext = NULL;
		dedicatedInfo.buffer = buffer;
		dedicatedInfo.image = image;

		allocInfo.pNext = &dedicatedInfo;
		allocation->dedicated = 1;
		allocation->availableForAllocation = 0;
	}
	else
	{
		allocInfo.pNext = NULL;
		allocation->dedicated = 0;
		allocation->availableForAllocation = 1;
	}

	allocation->usedRegions = SDL_malloc(sizeof(VulkanMemoryUsedRegion*));
	allocation->usedRegionCount = 0;
	allocation->usedRegionCapacity = 1;

	allocation->freeRegions = SDL_malloc(sizeof(VulkanMemoryFreeRegion*));
	allocation->freeRegionCount = 0;
	allocation->freeRegionCapacity = 1;

	allocation->allocator = allocator;

	result = renderer->vkAllocateMemory(
		renderer->logicalDevice,
		&allocInfo,
		NULL,
		&allocation->memory
	);

	if (result != VK_SUCCESS)
	{
		/* Uh oh, we couldn't allocate, time to clean up */
		SDL_free(allocation->freeRegions);

		allocator->allocationCount -= 1;
		allocator->allocations = SDL_realloc(
			allocator->allocations,
			sizeof(VulkanMemoryAllocation*) * allocator->allocationCount
		);

		SDL_free(allocation);

		FNA3D_LogWarn("vkAllocateMemory: %s", VkErrorMessages(result));
		return 0;
	}

	/* persistent mapping for host memory */
	if (isHostVisible)
	{
		result = renderer->vkMapMemory(
			renderer->logicalDevice,
			allocation->memory,
			0,
			VK_WHOLE_SIZE,
			0,
			(void**) &allocation->mapPointer
		);
		VULKAN_ERROR_CHECK(result, vkMapMemory, 0)
	}
	else
	{
		allocation->mapPointer = NULL;
	}

	VULKAN_INTERNAL_NewMemoryFreeRegion(
		renderer,
		allocation,
		0,
		allocation->size
	);

	*pMemoryAllocation = allocation;
	return 1;
}

static uint8_t VULKAN_INTERNAL_BindBufferMemory(
	VulkanRenderer *renderer,
	VulkanMemoryUsedRegion *usedRegion,
	VkDeviceSize alignedOffset,
	VkBuffer buffer
) {
	VkResult vulkanResult;

	SDL_LockMutex(usedRegion->allocation->mapLock);

	vulkanResult = renderer->vkBindBufferMemory(
		renderer->logicalDevice,
		buffer,
		usedRegion->allocation->memory,
		alignedOffset
	);

	SDL_UnlockMutex(usedRegion->allocation->mapLock);

	VULKAN_ERROR_CHECK(vulkanResult, vkBindBufferMemory, 0)

	return 1;
}

static uint8_t VULKAN_INTERNAL_BindImageMemory(
	VulkanRenderer *renderer,
	VulkanMemoryUsedRegion *usedRegion,
	VkDeviceSize alignedOffset,
	VkImage image
) {
	VkResult vulkanResult;

	SDL_LockMutex(usedRegion->allocation->mapLock);

	vulkanResult = renderer->vkBindImageMemory(
		renderer->logicalDevice,
		image,
		usedRegion->allocation->memory,
		alignedOffset
	);

	SDL_UnlockMutex(usedRegion->allocation->mapLock);

	VULKAN_ERROR_CHECK(vulkanResult, vkBindBufferMemory, 0)

	return 1;
}

static uint8_t VULKAN_INTERNAL_BindResourceMemory(
	VulkanRenderer* renderer,
	uint32_t memoryTypeIndex,
	VkMemoryRequirements2KHR* memoryRequirements,
	VkMemoryDedicatedRequirementsKHR* dedicatedRequirements,
	uint8_t forceDedicated,
	VkDeviceSize resourceSize, /* may be different from requirements size! */
	VkBuffer buffer, /* may be VK_NULL_HANDLE */
	VkImage image, /* may be VK_NULL_HANDLE */
	VulkanMemoryUsedRegion** pMemoryUsedRegion
) {
	VulkanMemoryAllocation *allocation;
	VulkanMemorySubAllocator *allocator;
	VulkanMemoryFreeRegion *region;
	VulkanMemoryUsedRegion *usedRegion;

	VkDeviceSize requiredSize, allocationSize;
	VkDeviceSize alignedOffset;
	uint32_t newRegionSize, newRegionOffset;
	uint8_t shouldAllocDedicated =
		forceDedicated ||
		dedicatedRequirements->prefersDedicatedAllocation ||
		dedicatedRequirements->requiresDedicatedAllocation;
	uint8_t isDeviceLocal, isHostVisible, allocationResult;

	isHostVisible =
		(renderer->memoryProperties.memoryTypes[memoryTypeIndex].propertyFlags &
		VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT) != 0;

	isDeviceLocal =
		(renderer->memoryProperties.memoryTypes[memoryTypeIndex].propertyFlags &
		VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT) != 0;

	allocator = &renderer->memoryAllocator->subAllocators[memoryTypeIndex];
	requiredSize = memoryRequirements->memoryRequirements.size;

	if (	(buffer == VK_NULL_HANDLE && image == VK_NULL_HANDLE) ||
		(buffer != VK_NULL_HANDLE && image != VK_NULL_HANDLE)	)
	{
		FNA3D_LogError("BindResourceMemory must be given either a VulkanSubBuffer or a VulkanTexture");
		return 0;
	}

	SDL_LockMutex(renderer->allocatorLock);

	/* find the largest free region and use it */
	if (!shouldAllocDedicated && allocator->sortedFreeRegionCount > 0)
	{
		region = allocator->sortedFreeRegions[0];
		allocation = region->allocation;

		alignedOffset = VULKAN_INTERNAL_NextHighestAlignment(
			region->offset,
			memoryRequirements->memoryRequirements.alignment
		);

		if (alignedOffset + requiredSize <= region->offset + region->size)
		{
			usedRegion = VULKAN_INTERNAL_NewMemoryUsedRegion(
				renderer,
				allocation,
				region->offset,
				requiredSize + (alignedOffset - region->offset),
				alignedOffset,
				resourceSize,
				memoryRequirements->memoryRequirements.alignment
			);

			usedRegion->isBuffer = buffer != VK_NULL_HANDLE;

			newRegionSize = region->size - ((alignedOffset - region->offset) + requiredSize);
			newRegionOffset = alignedOffset + requiredSize;

			/* remove and add modified region to re-sort */
			VULKAN_INTERNAL_RemoveMemoryFreeRegion(renderer, region);

			/* if size is 0, no need to re-insert */
			if (newRegionSize != 0)
			{
				VULKAN_INTERNAL_NewMemoryFreeRegion(
					renderer,
					allocation,
					newRegionOffset,
					newRegionSize
				);
			}

			SDL_UnlockMutex(renderer->allocatorLock);

			if (buffer != VK_NULL_HANDLE)
			{
				if (!VULKAN_INTERNAL_BindBufferMemory(
					renderer,
					usedRegion,
					alignedOffset,
					buffer
				)) {
					VULKAN_INTERNAL_RemoveMemoryUsedRegion(
						renderer,
						usedRegion
					);

					return 0;
				}
			}
			else if (image != VK_NULL_HANDLE)
			{
				if (!VULKAN_INTERNAL_BindImageMemory(
					renderer,
					usedRegion,
					alignedOffset,
					image
				)) {
					VULKAN_INTERNAL_RemoveMemoryUsedRegion(
						renderer,
						usedRegion
					);

					return 0;
				}
			}

			*pMemoryUsedRegion = usedRegion;
			return 1;
		}
	}

	/* No suitable free regions exist, allocate a new memory region */

	if (shouldAllocDedicated)
	{
		allocationSize = requiredSize;
	}
	else if (requiredSize > allocator->nextAllocationSize)
	{
		/* allocate a page of required size aligned to ALLOCATION_INCREMENT increments */
		allocationSize =
			VULKAN_INTERNAL_NextHighestAlignment(requiredSize, ALLOCATION_INCREMENT);
	}
	else
	{
		allocationSize = allocator->nextAllocationSize;
	}

	if (	isDeviceLocal &&
		(renderer->deviceLocalHeapUsage + allocationSize > renderer->maxDeviceLocalHeapUsage)	)
	{
		/* we are oversubscribing device local memory */
		SDL_UnlockMutex(renderer->allocatorLock);
		return 2;
	}

	allocationResult = VULKAN_INTERNAL_AllocateMemory(
		renderer,
		buffer,
		image,
		memoryTypeIndex,
		allocationSize,
		shouldAllocDedicated,
		isHostVisible,
		&allocation
	);

	/* Uh oh, we're out of memory */
	if (allocationResult == 0)
	{
		SDL_UnlockMutex(renderer->allocatorLock);

		/* Responsibility of the caller to handle being out of memory */
		FNA3D_LogWarn("Failed to allocate memory!");
		return 2;
	}

	if (isDeviceLocal)
	{
		renderer->deviceLocalHeapUsage += allocationSize;
	}

	usedRegion = VULKAN_INTERNAL_NewMemoryUsedRegion(
		renderer,
		allocation,
		0,
		requiredSize,
		0,
		resourceSize,
		memoryRequirements->memoryRequirements.alignment
	);

	usedRegion->isBuffer = buffer != VK_NULL_HANDLE;

	region = allocation->freeRegions[0];

	newRegionOffset = region->offset + requiredSize;
	newRegionSize = region->size - requiredSize;

	VULKAN_INTERNAL_RemoveMemoryFreeRegion(renderer, region);

	if (newRegionSize != 0)
	{
		VULKAN_INTERNAL_NewMemoryFreeRegion(
			renderer,
			allocation,
			newRegionOffset,
			newRegionSize
		);
	}

	SDL_UnlockMutex(renderer->allocatorLock);

	if (buffer != VK_NULL_HANDLE)
	{
		if (!VULKAN_INTERNAL_BindBufferMemory(
			renderer,
			usedRegion,
			0,
			buffer
		)) {
			VULKAN_INTERNAL_RemoveMemoryUsedRegion(
				renderer,
				usedRegion
			);

			return 0;
		}
	}
	else if (image != VK_NULL_HANDLE)
	{
		if (!VULKAN_INTERNAL_BindImageMemory(
			renderer,
			usedRegion,
			0,
			image
		)) {
			VULKAN_INTERNAL_RemoveMemoryUsedRegion(
				renderer,
				usedRegion
			);

			return 0;
		}
	}

	*pMemoryUsedRegion = usedRegion;
	return 1;
}

static uint8_t VULKAN_INTERNAL_BindMemoryForImage(
	VulkanRenderer* renderer,
	VkImage image,
	uint8_t isRenderTarget,
	VulkanMemoryUsedRegion** usedRegion
) {
	uint8_t bindResult = 0;
	uint32_t memoryTypeIndex;
	VkMemoryPropertyFlags requiredMemoryPropertyFlags;
	VkMemoryPropertyFlags ignoredMemoryPropertyFlags;
	VkMemoryDedicatedRequirementsKHR dedicatedRequirements =
	{
		VK_STRUCTURE_TYPE_MEMORY_DEDICATED_REQUIREMENTS_KHR,
		NULL
	};
	VkMemoryRequirements2KHR memoryRequirements =
	{
		VK_STRUCTURE_TYPE_MEMORY_REQUIREMENTS_2_KHR,
		&dedicatedRequirements
	};

	/* Prefer GPU allocation */
	requiredMemoryPropertyFlags = VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
	ignoredMemoryPropertyFlags = 0;

	if (VULKAN_INTERNAL_FindImageMemoryRequirements(
		renderer,
		image,
		requiredMemoryPropertyFlags,
		ignoredMemoryPropertyFlags,
		&memoryRequirements,
		&memoryTypeIndex
	)) {
		bindResult = VULKAN_INTERNAL_BindResourceMemory(
			renderer,
			memoryTypeIndex,
			&memoryRequirements,
			&dedicatedRequirements,
			isRenderTarget,
			memoryRequirements.memoryRequirements.size,
			VK_NULL_HANDLE,
			image,
			usedRegion
		);
	}

	/* Bind failed, try again without device local */
	if (bindResult != 1)
	{
		requiredMemoryPropertyFlags = 0;
		ignoredMemoryPropertyFlags = VK_MEMORY_HEAP_DEVICE_LOCAL_BIT;

		if (isRenderTarget)
		{
			FNA3D_LogWarn("RenderTarget is allocated in host memory, pre-allocate your targets!");
		}

		FNA3D_LogWarn("Out of device local memory, falling back to host memory");

		if (VULKAN_INTERNAL_FindImageMemoryRequirements(
			renderer,
			image,
			requiredMemoryPropertyFlags,
			ignoredMemoryPropertyFlags,
			&memoryRequirements,
			&memoryTypeIndex
		)) {
			bindResult = VULKAN_INTERNAL_BindResourceMemory(
				renderer,
				memoryTypeIndex,
				&memoryRequirements,
				&dedicatedRequirements,
				isRenderTarget,
				memoryRequirements.memoryRequirements.size,
				VK_NULL_HANDLE,
				image,
				usedRegion
			);
		}
	}

	return bindResult;
}

static uint8_t VULKAN_INTERNAL_BindMemoryForBuffer(
	VulkanRenderer* renderer,
	VkBuffer buffer,
	VkDeviceSize size,
	uint8_t preferDeviceLocal,
	uint8_t isStagingBuffer,
	VulkanMemoryUsedRegion** usedRegion
) {
	uint8_t bindResult = 0;
	uint32_t memoryTypeIndex;
	VkMemoryPropertyFlags requiredMemoryPropertyFlags;
	VkMemoryPropertyFlags ignoredMemoryPropertyFlags;
	VkMemoryDedicatedRequirementsKHR dedicatedRequirements =
	{
		VK_STRUCTURE_TYPE_MEMORY_DEDICATED_REQUIREMENTS_KHR,
		NULL
	};
	VkMemoryRequirements2KHR memoryRequirements =
	{
		VK_STRUCTURE_TYPE_MEMORY_REQUIREMENTS_2_KHR,
		&dedicatedRequirements
	};

	requiredMemoryPropertyFlags =
		VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
		VK_MEMORY_PROPERTY_HOST_COHERENT_BIT;

	if (preferDeviceLocal)
	{
		requiredMemoryPropertyFlags |=
			VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
	}
	ignoredMemoryPropertyFlags = 0;

	/* Attempt to bind memory */
	if (VULKAN_INTERNAL_FindBufferMemoryRequirements(
		renderer,
		buffer,
		requiredMemoryPropertyFlags,
		ignoredMemoryPropertyFlags,
		&memoryRequirements,
		&memoryTypeIndex
	)) {
		bindResult = VULKAN_INTERNAL_BindResourceMemory(
			renderer,
			memoryTypeIndex,
			&memoryRequirements,
			&dedicatedRequirements,
			isStagingBuffer,
			size,
			buffer,
			VK_NULL_HANDLE,
			usedRegion
		);
	}

	/* Bind failed, try again if originally preferred device local */
	if (bindResult != 1 && preferDeviceLocal)
	{
		requiredMemoryPropertyFlags =
			VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
			VK_MEMORY_PROPERTY_HOST_COHERENT_BIT;

		if (VULKAN_INTERNAL_FindBufferMemoryRequirements(
			renderer,
			buffer,
			requiredMemoryPropertyFlags,
			ignoredMemoryPropertyFlags,
			&memoryRequirements,
			&memoryTypeIndex
		)) {
			bindResult = VULKAN_INTERNAL_BindResourceMemory(
				renderer,
				memoryTypeIndex,
				&memoryRequirements,
				&dedicatedRequirements,
				isStagingBuffer,
				size,
				buffer,
				VK_NULL_HANDLE,
				usedRegion
			);
		}
	}

	return bindResult;
}

static uint8_t VULKAN_INTERNAL_FindAllocationToDefragment(
	VulkanRenderer *renderer,
	VulkanMemorySubAllocator *allocator,
	uint32_t *allocationIndexToDefrag
) {
	uint32_t i, j;

	for (i = 0; i < VK_MAX_MEMORY_TYPES; i += 1)
	{
		*allocator = renderer->memoryAllocator->subAllocators[i];

		for (j = 0; j < allocator->allocationCount; j += 1)
		{
			if (allocator->allocations[j]->freeRegionCount > 1)
			{
				*allocationIndexToDefrag = j;
				return 1;
			}
		}
	}

	return 0;
}

static uint8_t VULKAN_INTERNAL_DefragmentMemory(
	VulkanRenderer *renderer
) {
	VulkanMemorySubAllocator allocator;
	VulkanMemoryAllocation *allocation;
	uint32_t allocationIndexToDefrag;
	VulkanMemoryUsedRegion *currentRegion;
	VulkanMemoryUsedRegion *newRegion;
	VkBuffer copyBuffer;
	VkBufferCopy bufferCopy;
	VkImage copyImage;
	VkImageCopy *imageCopyRegions;
	VkImageAspectFlags aspectFlags;
	VkCommandBufferBeginInfo beginInfo;
	VkPipelineStageFlags waitFlags = VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;
	VkSubmitInfo submitInfo;
	VkResult result;
	uint32_t i, level;
	VulkanResourceAccessType copyResourceAccessType = RESOURCE_ACCESS_NONE;

	renderer->needDefrag = 0;

	renderer->vkResetCommandBuffer(
		renderer->defragCommandBuffer,
		VK_COMMAND_BUFFER_RESET_RELEASE_RESOURCES_BIT
	);

	beginInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
	beginInfo.pNext = NULL;
	beginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
	beginInfo.pInheritanceInfo = NULL;

	renderer->vkBeginCommandBuffer(
		renderer->defragCommandBuffer,
		&beginInfo
	);

	if (VULKAN_INTERNAL_FindAllocationToDefragment(
		renderer,
		&allocator,
		&allocationIndexToDefrag
	)) {
		renderer->currentCommandBuffer = renderer->defragCommandBuffer; /* FIXME: this is a kludge!! */

		allocation = allocator.allocations[allocationIndexToDefrag];

		VULKAN_INTERNAL_MakeMemoryUnavailable(
			renderer,
			allocation
		);

		for (i = 0; i < allocation->usedRegionCount; i += 1)
		{
			currentRegion = allocation->usedRegions[i];
			copyResourceAccessType = RESOURCE_ACCESS_NONE;

			if (currentRegion->isBuffer)
			{
				currentRegion->vulkanSubBuffer->bufferCreateInfo.usage |= VK_BUFFER_USAGE_TRANSFER_DST_BIT;

				result = renderer->vkCreateBuffer(
					renderer->logicalDevice,
					&currentRegion->vulkanSubBuffer->bufferCreateInfo,
					NULL,
					&copyBuffer
				);
				VULKAN_ERROR_CHECK(result, vkCreateBuffer, 0)

				if (
					VULKAN_INTERNAL_BindMemoryForBuffer(
						renderer,
						copyBuffer,
						currentRegion->resourceSize,
						currentRegion->vulkanSubBuffer->parent->preferDeviceLocal,
						0,
						&newRegion
					) != 1)
				{
					/* Out of memory, abort */
					renderer->vkDestroyBuffer(
						renderer->logicalDevice,
						copyBuffer,
						NULL
					);
					break;
				}

				VULKAN_INTERNAL_BufferMemoryBarrier(
					renderer,
					RESOURCE_ACCESS_TRANSFER_READ,
					currentRegion->vulkanSubBuffer->buffer,
					&currentRegion->vulkanSubBuffer->resourceAccessType
				);

				VULKAN_INTERNAL_BufferMemoryBarrier(
					renderer,
					RESOURCE_ACCESS_TRANSFER_WRITE,
					copyBuffer,
					&copyResourceAccessType
				);

				bufferCopy.srcOffset = 0;
				bufferCopy.dstOffset = 0;
				bufferCopy.size = currentRegion->resourceSize;

				renderer->vkCmdCopyBuffer(
					renderer->defragCommandBuffer,
					currentRegion->vulkanSubBuffer->buffer,
					copyBuffer,
					1,
					&bufferCopy
				);

				if (renderer->defragmentedBuffersToDestroyCount >= renderer->defragmentedBuffersToDestroyCapacity)
				{
					renderer->defragmentedBuffersToDestroyCapacity *= 2;
					renderer->defragmentedBuffersToDestroy = SDL_realloc(
						renderer->defragmentedBuffersToDestroy,
						sizeof(VkBuffer) * renderer->defragmentedBuffersToDestroyCapacity
					);
				}

				if (renderer->usedRegionsToDestroyCount >= renderer->usedRegionsToDestroyCapacity)
				{
					renderer->usedRegionsToDestroyCapacity *= 2;
					renderer->usedRegionsToDestroy = SDL_realloc(
						renderer->usedRegionsToDestroy,
						sizeof(VulkanMemoryUsedRegion*) * renderer->usedRegionsToDestroyCapacity
					);
				}

				renderer->defragmentedBuffersToDestroy[
					renderer->defragmentedBuffersToDestroyCount
				] = currentRegion->vulkanSubBuffer->buffer;

				renderer->defragmentedBuffersToDestroyCount += 1;

				renderer->usedRegionsToDestroy[
					renderer->usedRegionsToDestroyCount
				] = currentRegion;

				renderer->usedRegionsToDestroyCount += 1;

				newRegion->isBuffer = 1;
				newRegion->vulkanSubBuffer = currentRegion->vulkanSubBuffer;
				newRegion->vulkanSubBuffer->usedRegion = newRegion; /* lol */
				newRegion->vulkanSubBuffer->buffer = copyBuffer;
				newRegion->vulkanSubBuffer->resourceAccessType = copyResourceAccessType;

				renderer->bufferDefragInProgress = 1;
				renderer->needDefrag = 1;
			}
			else
			{
				result = renderer->vkCreateImage(
					renderer->logicalDevice,
					&currentRegion->vulkanTexture->imageCreateInfo,
					NULL,
					&copyImage
				);

				VULKAN_ERROR_CHECK(result, vkCreateImage, 0)

				if (VULKAN_INTERNAL_BindMemoryForImage(
					renderer,
					copyImage,
					0,
					&newRegion
				) != 1)
				{
					/* Out of memory, abort */
					renderer->vkDestroyImage(
						renderer->logicalDevice,
						copyImage,
						NULL
					);

					break;
				}

				if (IsDepthFormat(currentRegion->vulkanTexture->surfaceFormat))
				{
					aspectFlags = VK_IMAGE_ASPECT_DEPTH_BIT;

					if (DepthFormatContainsStencil(currentRegion->vulkanTexture->surfaceFormat))
					{
						aspectFlags |= VK_IMAGE_ASPECT_STENCIL_BIT;
					}
				}
				else
				{
					aspectFlags = VK_IMAGE_ASPECT_COLOR_BIT;
				}

				VULKAN_INTERNAL_ImageMemoryBarrier(
					renderer,
					RESOURCE_ACCESS_TRANSFER_READ,
					aspectFlags,
					0,
					currentRegion->vulkanTexture->layerCount,
					0,
					currentRegion->vulkanTexture->levelCount,
					0,
					currentRegion->vulkanTexture->image,
					&currentRegion->vulkanTexture->resourceAccessType
				);

				VULKAN_INTERNAL_ImageMemoryBarrier(
					renderer,
					RESOURCE_ACCESS_TRANSFER_WRITE,
					aspectFlags,
					0,
					currentRegion->vulkanTexture->layerCount,
					0,
					currentRegion->vulkanTexture->levelCount,
					0,
					copyImage,
					&copyResourceAccessType
				);

				imageCopyRegions = SDL_stack_alloc(VkImageCopy, currentRegion->vulkanTexture->levelCount);

				for (level = 0; level < currentRegion->vulkanTexture->levelCount; level += 1)
				{
					imageCopyRegions[level].srcOffset.x = 0;
					imageCopyRegions[level].srcOffset.y = 0;
					imageCopyRegions[level].srcOffset.z = 0;
					imageCopyRegions[level].srcSubresource.aspectMask = aspectFlags;
					imageCopyRegions[level].srcSubresource.baseArrayLayer = 0;
					imageCopyRegions[level].srcSubresource.layerCount = currentRegion->vulkanTexture->layerCount;
					imageCopyRegions[level].srcSubresource.mipLevel = level;
					imageCopyRegions[level].extent.width = currentRegion->vulkanTexture->dimensions.width >> level;
					imageCopyRegions[level].extent.height = currentRegion->vulkanTexture->dimensions.height >> level;
					imageCopyRegions[level].extent.depth = currentRegion->vulkanTexture->depth;
					imageCopyRegions[level].dstOffset.x = 0;
					imageCopyRegions[level].dstOffset.y = 0;
					imageCopyRegions[level].dstOffset.z = 0;
					imageCopyRegions[level].dstSubresource.aspectMask = aspectFlags;
					imageCopyRegions[level].dstSubresource.baseArrayLayer = 0;
					imageCopyRegions[level].dstSubresource.layerCount = currentRegion->vulkanTexture->layerCount;
					imageCopyRegions[level].dstSubresource.mipLevel = level;
				}

				renderer->vkCmdCopyImage(
					renderer->defragCommandBuffer,
					currentRegion->vulkanTexture->image,
					AccessMap[currentRegion->vulkanTexture->resourceAccessType].imageLayout,
					copyImage,
					AccessMap[copyResourceAccessType].imageLayout,
					currentRegion->vulkanTexture->levelCount,
					imageCopyRegions
				);

				SDL_stack_free(imageCopyRegions);

				if (renderer->defragmentedImagesToDestroyCount >= renderer->defragmentedImagesToDestroyCapacity)
				{
					renderer->defragmentedImagesToDestroyCapacity *= 2;
					renderer->defragmentedImagesToDestroy = SDL_realloc(
						renderer->defragmentedImagesToDestroy,
						sizeof(VkImage) * renderer->defragmentedImagesToDestroyCapacity
					);
				}

				if (renderer->defragmentedImageViewsToDestroyCount >= renderer->defragmentedImageViewsToDestroyCapacity)
				{
					renderer->defragmentedImageViewsToDestroyCapacity *= 2;
					renderer->defragmentedImageViewsToDestroy = SDL_realloc(
						renderer->defragmentedImageViewsToDestroy,
						sizeof(VkImageView) * renderer->defragmentedImageViewsToDestroyCapacity
					);
				}

				if (renderer->usedRegionsToDestroyCount >= renderer->usedRegionsToDestroyCapacity)
				{
					renderer->usedRegionsToDestroyCapacity *= 2;
					renderer->usedRegionsToDestroy = SDL_realloc(
						renderer->usedRegionsToDestroy,
						sizeof(VulkanMemoryUsedRegion*) * renderer->usedRegionsToDestroyCapacity
					);
				}

				renderer->defragmentedImagesToDestroy[
					renderer->defragmentedImagesToDestroyCount
				] = currentRegion->vulkanTexture->image;

				renderer->defragmentedImagesToDestroyCount += 1;

				renderer->defragmentedImageViewsToDestroy[
					renderer->defragmentedImageViewsToDestroyCount
				] = currentRegion->vulkanTexture->view;

				renderer->defragmentedImageViewsToDestroyCount += 1;

				renderer->usedRegionsToDestroy[
					renderer->usedRegionsToDestroyCount
				] = currentRegion;

				renderer->usedRegionsToDestroyCount += 1;

				currentRegion->vulkanTexture->viewCreateInfo.image = copyImage;

				renderer->vkCreateImageView(
					renderer->logicalDevice,
					&currentRegion->vulkanTexture->viewCreateInfo,
					NULL,
					&currentRegion->vulkanTexture->view
				);

				newRegion->isBuffer = 0;

				newRegion->vulkanTexture = currentRegion->vulkanTexture;
				newRegion->vulkanTexture->usedRegion = newRegion; /* lol */
				newRegion->vulkanTexture->image = copyImage;
				newRegion->vulkanTexture->resourceAccessType = copyResourceAccessType;

				renderer->needDefrag = 1;
			}
		}
	}

	renderer->vkEndCommandBuffer(
		renderer->defragCommandBuffer
	);

	submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
	submitInfo.pNext = NULL;
	submitInfo.commandBufferCount = 1;
	submitInfo.pCommandBuffers = &renderer->defragCommandBuffer;
	submitInfo.waitSemaphoreCount = 1;
	submitInfo.pWaitSemaphores = &renderer->defragSemaphore;
	submitInfo.pWaitDstStageMask = &waitFlags;
	submitInfo.signalSemaphoreCount = 0;
	submitInfo.pSignalSemaphores = NULL;

	result = renderer->vkResetFences(
		renderer->logicalDevice,
		1,
		&renderer->defragFence
	);
	VULKAN_ERROR_CHECK(result, vkResetFences, 0)

		result = renderer->vkQueueSubmit(
			renderer->unifiedQueue,
			1,
			&submitInfo,
			renderer->defragFence
		);
	VULKAN_ERROR_CHECK(result, vkQueueSubmit, 0)

		renderer->currentCommandBuffer = NULL;
	renderer->numActiveCommands = 0;

	renderer->defragTimer = 0;

	return 1;
}

/* Vulkan: Resource Disposal */

static void VULKAN_INTERNAL_DestroyBuffer(
	VulkanRenderer *renderer,
	VulkanBuffer *buffer
) {
	uint32_t i;

	if (buffer->bound)
	{
		for (i = 0; i < renderer->numBuffersInUse; i += 1)
		{
			if (renderer->buffersInUse[i] == buffer)
			{
				renderer->buffersInUse[i] = NULL;
			}
		}
	}

	if (buffer->boundSubmitted)
	{
		for (i = 0; i < renderer->numSubmittedBuffers; i += 1)
		{
			if (renderer->submittedBuffers[i] == buffer)
			{
				renderer->submittedBuffers[i] = NULL;
			}
		}
	}

	for (i = 0; i < buffer->subBufferCount; i += 1)
	{
		renderer->vkDestroyBuffer(
			renderer->logicalDevice,
			buffer->subBuffers[i]->buffer,
			NULL
		);

		VULKAN_INTERNAL_RemoveMemoryUsedRegion(
			renderer,
			buffer->subBuffers[i]->usedRegion
		);

		SDL_free(buffer->subBuffers[i]);
	}

	SDL_free(buffer->subBuffers);
	buffer->subBuffers = NULL;

	SDL_free(buffer);
}

static void VULKAN_INTERNAL_DestroyImageView(
	VulkanRenderer* renderer,
	VkImageView imageView
) {
	ShaderResources_InvalidateDescriptorSet(
		renderer,
		imageView
	);

	renderer->vkDestroyImageView(
		renderer->logicalDevice,
		imageView,
		NULL
	);
}

/* When a render target image view is destroyed we need to invalidate
 * the framebuffers and descriptor sets that reference it */
static void VULKAN_INTERNAL_RemoveViewFramebuffer(
	VulkanRenderer *renderer,
	VkImageView imageView
) {
	int32_t i, j;

	for (i = renderer->framebufferArray.count - 1; i >= 0; i -= 1)
	{
		if (renderer->framebufferArray.elements[i].key.depthStencilAttachmentView == imageView)
		{
			renderer->vkDestroyFramebuffer(
				renderer->logicalDevice,
				renderer->framebufferArray.elements[i].value,
				NULL
			);

			FramebufferHashArray_Remove(
				&renderer->framebufferArray,
				i
			);
		}
		else
		{
			for (j = 0; j < MAX_RENDERTARGET_BINDINGS; j += 1)
			{
				if (
					renderer->framebufferArray.elements[i].key.colorAttachmentViews[j] == imageView ||
					renderer->framebufferArray.elements[i].key.colorMultiSampleAttachmentViews[j] == imageView
				)
				{
					renderer->vkDestroyFramebuffer(
						renderer->logicalDevice,
						renderer->framebufferArray.elements[i].value,
						NULL
					);

					FramebufferHashArray_Remove(
						&renderer->framebufferArray,
						i
					);

					break;
				}
			}
		}
	}

	VULKAN_INTERNAL_DestroyImageView(
		renderer,
		imageView
	);
}

static void VULKAN_INTERNAL_DestroyTexture(
	VulkanRenderer *renderer,
	VulkanTexture *texture
) {
	int32_t i;

	if (texture->external)
	{
		SDL_free(texture);
		return;
	}

	VULKAN_INTERNAL_DestroyImageView(
		renderer,
		texture->view
	);

	if (texture->isRenderTarget)
	{
		if (texture->rtViews[0] != texture->view)
		{
			VULKAN_INTERNAL_RemoveViewFramebuffer(
				renderer,
				texture->rtViews[0]
			);
		}

		if (texture->rtViews[1] != VK_NULL_HANDLE)
		{
			/* Free all the other cube RT views */
			for (i = 1; i < 6; i += 1)
			{
				VULKAN_INTERNAL_RemoveViewFramebuffer(
					renderer,
					texture->rtViews[i]
				);
			}
		}
	}

	renderer->vkDestroyImage(
		renderer->logicalDevice,
		texture->image,
		NULL
	);

	VULKAN_INTERNAL_RemoveMemoryUsedRegion(
		renderer,
		texture->usedRegion
	);

	SDL_free(texture);
}

static void VULKAN_INTERNAL_DestroyRenderbuffer(
	VulkanRenderer *renderer,
	VulkanRenderbuffer *renderbuffer
) {
	uint8_t isDepthStencil = (renderbuffer->colorBuffer == NULL);

	if (isDepthStencil)
	{
		VULKAN_INTERNAL_DestroyTexture(
			renderer,
			renderbuffer->depthBuffer->handle
		);
		SDL_free(renderbuffer->depthBuffer);
	}
	else
	{
		if (renderbuffer->colorBuffer->multiSampleTexture != NULL)
		{
			VULKAN_INTERNAL_DestroyTexture(
				renderer,
				renderbuffer->colorBuffer->multiSampleTexture
			);
		}

		/* The image is owned by the texture it's from,
		 * so we don't free it here.
		 */
		SDL_free(renderbuffer->colorBuffer);
	}

	SDL_free(renderbuffer);
}

static void VULKAN_INTERNAL_DestroyEffect(
	VulkanRenderer *renderer,
	VulkanEffect *vulkanEffect
) {
	MOJOSHADER_effect *effectData = vulkanEffect->effect;

	if (effectData == renderer->currentEffect)
	{
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectEnd(renderer->currentEffect);
		renderer->currentEffect = NULL;
		renderer->currentTechnique = NULL;
		renderer->currentPass = 0;
	}
	MOJOSHADER_deleteEffect(effectData);
	SDL_free(vulkanEffect);
}

static void VULKAN_INTERNAL_PerformDeferredDestroys(VulkanRenderer *renderer)
{
	int32_t i, j;
	VulkanMemorySubAllocator *allocator;

	/* Destroy old resources that were copied on defragment */

	for (i = 0; i < renderer->defragmentedBuffersToDestroyCount; i += 1)
	{
		renderer->vkDestroyBuffer(
			renderer->logicalDevice,
			renderer->defragmentedBuffersToDestroy[i],
			NULL
		);
	}

	renderer->defragmentedBuffersToDestroyCount = 0;

	for (i = 0; i < renderer->defragmentedImagesToDestroyCount; i += 1)
	{
		renderer->vkDestroyImage(
			renderer->logicalDevice,
			renderer->defragmentedImagesToDestroy[i],
			NULL
		);
	}

	renderer->defragmentedImagesToDestroyCount = 0;

	for (i = 0; i < renderer->defragmentedImageViewsToDestroyCount; i += 1)
	{
		VULKAN_INTERNAL_DestroyImageView(
			renderer,
			renderer->defragmentedImageViewsToDestroy[i]
		);
	}

	renderer->defragmentedImageViewsToDestroyCount = 0;

	for (i = 0; i < renderer->usedRegionsToDestroyCount; i += 1)
	{
		VULKAN_INTERNAL_RemoveMemoryUsedRegion(
			renderer,
			renderer->usedRegionsToDestroy[i]
		);
	}

	renderer->usedRegionsToDestroyCount = 0;

	/* Destroy submitted resources */

	SDL_LockMutex(renderer->disposeLock);

	for (i = 0; i < renderer->submittedRenderbuffersToDestroyCount; i += 1)
	{
		VULKAN_INTERNAL_DestroyRenderbuffer(
			renderer,
			renderer->submittedRenderbuffersToDestroy[i]
		);
	}
	renderer->submittedRenderbuffersToDestroyCount = 0;

	for (i = 0; i < renderer->submittedBuffersToDestroyCount; i += 1)
	{
		VULKAN_INTERNAL_DestroyBuffer(
			renderer,
			renderer->submittedBuffersToDestroy[i]
		);
	}
	renderer->submittedBuffersToDestroyCount = 0;

	for (i = 0; i < renderer->submittedEffectsToDestroyCount; i += 1)
	{
		VULKAN_INTERNAL_DestroyEffect(
			renderer,
			renderer->submittedEffectsToDestroy[i]
		);
	}
	renderer->submittedEffectsToDestroyCount = 0;

	for (i = 0; i < renderer->submittedTexturesToDestroyCount; i += 1)
	{
		VULKAN_INTERNAL_DestroyTexture(
			renderer,
			renderer->submittedTexturesToDestroy[i]
		);
	}
	renderer->submittedTexturesToDestroyCount = 0;

	/* Re-size submitted destroy lists */

	if (renderer->submittedRenderbuffersToDestroyCapacity < renderer->renderbuffersToDestroyCount)
	{
		renderer->submittedRenderbuffersToDestroy = SDL_realloc(
			renderer->submittedRenderbuffersToDestroy,
			sizeof(VulkanRenderbuffer*) * renderer->renderbuffersToDestroyCount
		);

		renderer->submittedRenderbuffersToDestroyCapacity = renderer->renderbuffersToDestroyCount;
	}

	if (renderer->submittedBuffersToDestroyCapacity < renderer->buffersToDestroyCount)
	{
		renderer->submittedBuffersToDestroy = SDL_realloc(
			renderer->submittedBuffersToDestroy,
			sizeof(VulkanBuffer*) * renderer->buffersToDestroyCount
		);

		renderer->submittedBuffersToDestroyCapacity = renderer->buffersToDestroyCount;
	}

	if (renderer->submittedEffectsToDestroyCapacity < renderer->effectsToDestroyCount)
	{
		renderer->submittedEffectsToDestroy = SDL_realloc(
			renderer->submittedEffectsToDestroy,
			sizeof(VulkanEffect*) * renderer->effectsToDestroyCount
		);

		renderer->submittedEffectsToDestroyCapacity = renderer->effectsToDestroyCount;
	}

	if (renderer->submittedTexturesToDestroyCapacity < renderer->texturesToDestroyCount)
	{
		renderer->submittedTexturesToDestroy = SDL_realloc(
			renderer->submittedTexturesToDestroy,
			sizeof(VulkanTexture*) * renderer->texturesToDestroyCount
		);

		renderer->submittedTexturesToDestroyCapacity = renderer->texturesToDestroyCount;
	}

	/* Rotate destroy lists */

	for (i = 0; i < renderer->renderbuffersToDestroyCount; i += 1)
	{
		renderer->submittedRenderbuffersToDestroy[i] = renderer->renderbuffersToDestroy[i];
	}
	renderer->submittedRenderbuffersToDestroyCount = renderer->renderbuffersToDestroyCount;
	renderer->renderbuffersToDestroyCount = 0;

	for (i = 0; i < renderer->buffersToDestroyCount; i += 1)
	{
		renderer->submittedBuffersToDestroy[i] = renderer->buffersToDestroy[i];
	}
	renderer->submittedBuffersToDestroyCount = renderer->buffersToDestroyCount;
	renderer->buffersToDestroyCount = 0;

	for (i = 0; i < renderer->effectsToDestroyCount; i += 1)
	{
		renderer->submittedEffectsToDestroy[i] = renderer->effectsToDestroy[i];
	}
	renderer->submittedEffectsToDestroyCount = renderer->effectsToDestroyCount;
	renderer->effectsToDestroyCount = 0;

	for (i = 0; i < renderer->texturesToDestroyCount; i += 1)
	{
		renderer->submittedTexturesToDestroy[i] = renderer->texturesToDestroy[i];
	}
	renderer->submittedTexturesToDestroyCount = renderer->texturesToDestroyCount;
	renderer->texturesToDestroyCount = 0;

	SDL_UnlockMutex(renderer->disposeLock);

	/* free empty allocations */
	SDL_LockMutex(renderer->allocatorLock);

	for (i = 0; i < VK_MAX_MEMORY_TYPES; i += 1)
	{
		allocator = &renderer->memoryAllocator->subAllocators[i];

		for (j = allocator->allocationCount - 1; j >= 0; j -= 1)
		{
			if (allocator->allocations[j]->usedRegionCount == 0)
			{
				VULKAN_INTERNAL_DeallocateMemory(
					renderer,
					allocator,
					j
				);
			}
		}
	}

	SDL_UnlockMutex(renderer->allocatorLock);
}

/* Vulkan: Memory Barriers */

static void VULKAN_INTERNAL_BufferMemoryBarrier(
	VulkanRenderer *renderer,
	VulkanResourceAccessType nextResourceAccessType,
	VkBuffer buffer,
	VulkanResourceAccessType *resourceAccessType
) {
	VkPipelineStageFlags srcStages = 0;
	VkPipelineStageFlags dstStages = 0;
	VkBufferMemoryBarrier memoryBarrier;
	VulkanResourceAccessType prevAccess, nextAccess;
	const VulkanResourceAccessInfo *prevAccessInfo, *nextAccessInfo;

	if (*resourceAccessType == nextResourceAccessType)
	{
		return;
	}

	SDL_LockMutex(renderer->passLock);

	memoryBarrier.sType = VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER;
	memoryBarrier.pNext = NULL;
	memoryBarrier.srcAccessMask = 0;
	memoryBarrier.dstAccessMask = 0;
	memoryBarrier.srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
	memoryBarrier.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
	memoryBarrier.buffer = buffer;
	memoryBarrier.offset = 0;
	memoryBarrier.size = VK_WHOLE_SIZE;

	prevAccess = *resourceAccessType;
	prevAccessInfo = &AccessMap[prevAccess];

	srcStages |= prevAccessInfo->stageMask;

	if (prevAccess > RESOURCE_ACCESS_END_OF_READ)
	{
		memoryBarrier.srcAccessMask |= prevAccessInfo->accessMask;
	}

	nextAccess = nextResourceAccessType;
	nextAccessInfo = &AccessMap[nextAccess];

	dstStages |= nextAccessInfo->stageMask;

	if (memoryBarrier.srcAccessMask != 0)
	{
		memoryBarrier.dstAccessMask |= nextAccessInfo->accessMask;
	}

	if (srcStages == 0)
	{
		srcStages = VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
	}
	if (dstStages == 0)
	{
		dstStages = VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT;
	}

	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);
	renderer->needNewRenderPass = 1;

	RECORD_CMD(renderer->vkCmdPipelineBarrier(
		renderer->currentCommandBuffer,
		srcStages,
		dstStages,
		0,
		0,
		NULL,
		1,
		&memoryBarrier,
		0,
		NULL
	));

	*resourceAccessType = nextResourceAccessType;

	SDL_UnlockMutex(renderer->passLock);
}

static void VULKAN_INTERNAL_ImageMemoryBarrier(
	VulkanRenderer *renderer,
	VulkanResourceAccessType nextAccess,
	VkImageAspectFlags aspectMask,
	uint32_t baseLayer,
	uint32_t layerCount,
	uint32_t baseLevel,
	uint32_t levelCount,
	uint8_t discardContents,
	VkImage image,
	VulkanResourceAccessType *resourceAccessType
) {
	VkPipelineStageFlags srcStages = 0;
	VkPipelineStageFlags dstStages = 0;
	VkImageMemoryBarrier memoryBarrier;
	VulkanResourceAccessType prevAccess;
	const VulkanResourceAccessInfo *pPrevAccessInfo, *pNextAccessInfo;

	if (*resourceAccessType == nextAccess)
	{
		return;
	}

	SDL_LockMutex(renderer->passLock);

	memoryBarrier.sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER;
	memoryBarrier.pNext = NULL;
	memoryBarrier.srcAccessMask = 0;
	memoryBarrier.dstAccessMask = 0;
	memoryBarrier.oldLayout = VK_IMAGE_LAYOUT_UNDEFINED;
	memoryBarrier.newLayout = VK_IMAGE_LAYOUT_UNDEFINED;
	memoryBarrier.srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
	memoryBarrier.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
	memoryBarrier.image = image;
	memoryBarrier.subresourceRange.aspectMask = aspectMask;
	memoryBarrier.subresourceRange.baseArrayLayer = baseLayer;
	memoryBarrier.subresourceRange.layerCount = layerCount;
	memoryBarrier.subresourceRange.baseMipLevel = baseLevel;
	memoryBarrier.subresourceRange.levelCount = levelCount;

	prevAccess = *resourceAccessType;
	pPrevAccessInfo = &AccessMap[prevAccess];

	srcStages |= pPrevAccessInfo->stageMask;

	if (prevAccess > RESOURCE_ACCESS_END_OF_READ)
	{
		memoryBarrier.srcAccessMask |= pPrevAccessInfo->accessMask;
	}

	if (discardContents)
	{
		memoryBarrier.oldLayout = VK_IMAGE_LAYOUT_UNDEFINED;
	}
	else
	{
		memoryBarrier.oldLayout = pPrevAccessInfo->imageLayout;
	}

	pNextAccessInfo = &AccessMap[nextAccess];

	dstStages |= pNextAccessInfo->stageMask;

	memoryBarrier.dstAccessMask |= pNextAccessInfo->accessMask;
	memoryBarrier.newLayout = pNextAccessInfo->imageLayout;

	if (srcStages == 0)
	{
		srcStages = VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
	}
	if (dstStages == 0)
	{
		dstStages = VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT;
	}

	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);
	renderer->needNewRenderPass = 1;

	RECORD_CMD(renderer->vkCmdPipelineBarrier(
		renderer->currentCommandBuffer,
		srcStages,
		dstStages,
		0,
		0,
		NULL,
		0,
		NULL,
		1,
		&memoryBarrier
	));

	*resourceAccessType = nextAccess;

	SDL_UnlockMutex(renderer->passLock);
}

/* Allocator functions */

static uint8_t VULKAN_INTERNAL_AllocateSubBuffer(
	VulkanRenderer *renderer,
	VulkanBuffer *vulkanBuffer
) {
	VulkanSubBuffer *subBuffer = SDL_malloc(sizeof(VulkanSubBuffer));
	VkBufferCreateInfo bufferCreateInfo;
	VkResult vulkanResult;
	uint8_t bindResult = 0;

	subBuffer->parent = vulkanBuffer;

	bufferCreateInfo.sType = VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO;
	bufferCreateInfo.pNext = NULL;
	bufferCreateInfo.flags = 0;
	bufferCreateInfo.size = vulkanBuffer->size;
	bufferCreateInfo.usage = vulkanBuffer->usage;
	bufferCreateInfo.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
	bufferCreateInfo.queueFamilyIndexCount = 1;
	bufferCreateInfo.pQueueFamilyIndices = &renderer->queueFamilyIndex;

	vulkanResult = renderer->vkCreateBuffer(
		renderer->logicalDevice,
		&bufferCreateInfo,
		NULL,
		&subBuffer->buffer
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateBuffer, 0)

	subBuffer->bufferCreateInfo = bufferCreateInfo;

	bindResult = VULKAN_INTERNAL_BindMemoryForBuffer(
		renderer,
		subBuffer->buffer,
		vulkanBuffer->size,
		vulkanBuffer->preferDeviceLocal,
		vulkanBuffer->isStagingBuffer,
		&subBuffer->usedRegion
	);

	/* Binding failed, bail out! */
	if (bindResult != 1)
	{
		renderer->vkDestroyBuffer(
			renderer->logicalDevice,
			subBuffer->buffer,
			NULL);

		SDL_free(subBuffer);
		return bindResult;
	}

	subBuffer->usedRegion->vulkanSubBuffer = subBuffer; /* lol */

	subBuffer->resourceAccessType = vulkanBuffer->resourceAccessType;
	subBuffer->bound = -1;

	/* Reallocate the subbuffer array if we're at max capacity */
	if (vulkanBuffer->subBufferCount == vulkanBuffer->subBufferCapacity)
	{
		vulkanBuffer->subBufferCapacity *= 2;
		vulkanBuffer->subBuffers = SDL_realloc(
			vulkanBuffer->subBuffers,
			sizeof(VulkanSubBuffer) * vulkanBuffer->subBufferCapacity
		);
	}

	vulkanBuffer->subBuffers[vulkanBuffer->subBufferCount] = subBuffer;
	vulkanBuffer->subBufferCount += 1;

	VULKAN_INTERNAL_BufferMemoryBarrier(
		renderer,
		vulkanBuffer->resourceAccessType,
		subBuffer->buffer,
		&subBuffer->resourceAccessType
	);

	return 1;
}

static VulkanBuffer* VULKAN_INTERNAL_CreateBuffer(
	VulkanRenderer *renderer,
	VkDeviceSize size,
	VulkanResourceAccessType resourceAccessType,
	VkBufferUsageFlags usage,
	uint8_t isDeviceLocal,
	uint8_t isStagingBuffer,
	uint32_t subBufferCount
) {
	uint32_t i;
	uint8_t allocateResult;

	VulkanBuffer *buffer = SDL_malloc(sizeof(VulkanBuffer));

	buffer->size = size;

	buffer->currentSubBufferIndex = 0;
	buffer->bound = 0;
	buffer->boundSubmitted = 0;
	buffer->resourceAccessType = resourceAccessType;
	buffer->usage = usage | VK_BUFFER_USAGE_TRANSFER_SRC_BIT;
	buffer->preferDeviceLocal = isDeviceLocal;
	buffer->isStagingBuffer = isStagingBuffer;

	buffer->subBufferCount = 0;
	buffer->subBufferCapacity = subBufferCount;
	buffer->subBuffers = SDL_malloc(
		sizeof(VulkanSubBuffer) * buffer->subBufferCapacity
	);

	for (i = 0; i < buffer->subBufferCapacity; i += 1)
	{
		/* Populate one sub buffer */
		allocateResult = VULKAN_INTERNAL_AllocateSubBuffer(
			renderer,
			buffer
		);

		if (allocateResult != 1)
		{
			return NULL;
		}
	}

	return buffer;
}

/* Staging buffer functions */

static void VULKAN_INTERNAL_ResetTextureStagingBuffer(
	VulkanRenderer *renderer
) {
	renderer->textureStagingBuffer->slowBufferOffset = 0;
	renderer->textureStagingBuffer->fastBufferOffset = 0;

	if (renderer->textureStagingBuffer->pendingTransfer)
	{
		renderer->textureStagingBuffer->transferInProgress = 1;
		renderer->textureStagingBuffer->pendingTransfer = 0;
	}
	else
	{
		renderer->textureStagingBuffer->transferInProgress = 0;
	}
}

static void VULKAN_INTERNAL_CreateFastStagingBuffer(
	VulkanRenderer *renderer,
	VkDeviceSize size
) {
	renderer->textureStagingBuffer->fastBuffer = (VulkanBuffer*) VULKAN_INTERNAL_CreateBuffer(
		renderer,
		size,
		RESOURCE_ACCESS_MEMORY_TRANSFER_READ_WRITE,
		VK_BUFFER_USAGE_TRANSFER_SRC_BIT | VK_BUFFER_USAGE_TRANSFER_DST_BIT,
		1,
		1,
		1
	);
}

static void VULKAN_INTERNAL_CreateSlowStagingBuffer(
	VulkanRenderer *renderer,
	VkDeviceSize size
) {
	renderer->textureStagingBuffer->slowBuffer = (VulkanBuffer*) VULKAN_INTERNAL_CreateBuffer(
		renderer,
		size,
		RESOURCE_ACCESS_MEMORY_TRANSFER_READ_WRITE,
		VK_BUFFER_USAGE_TRANSFER_SRC_BIT | VK_BUFFER_USAGE_TRANSFER_DST_BIT,
		0,
		1,
		1
	);

	if (renderer->textureStagingBuffer->slowBuffer == NULL)
	{
		FNA3D_LogError("Failed to create slow texture staging buffer!");
	}
}

static void VULKAN_INTERNAL_ExpandSlowStagingBuffer(
	VulkanRenderer *renderer,
	VkDeviceSize requiredSize
) {
	VkDeviceSize nextStagingSize = renderer->textureStagingBuffer->slowBuffer->size;

	if (renderer->textureStagingBuffer->slowBuffer->size < MAX_SLOW_TEXTURE_STAGING_SIZE)
	{
		nextStagingSize *= 2;

		while (nextStagingSize < requiredSize)
		{
			nextStagingSize *= 2;
		}

		VULKAN_INTERNAL_RemoveBuffer(
			(FNA3D_Renderer*) renderer,
			(FNA3D_Buffer*) renderer->textureStagingBuffer->slowBuffer
		);

		VULKAN_INTERNAL_CreateSlowStagingBuffer(
			renderer,
			nextStagingSize
		);
	}
}

static void VULKAN_INTERNAL_WaitForStagingTransfers(
	VulkanRenderer *renderer
) {
	VkResult result;
	VkFence fences[2];

	if (renderer->textureStagingBuffer->transferInProgress || renderer->bufferDefragInProgress)
	{
		fences[0] = renderer->inFlightFence;
		fences[1] = renderer->defragFence;

		result = renderer->vkWaitForFences(
			renderer->logicalDevice,
			2,
			fences,
			VK_TRUE,
			UINT64_MAX
		);

		/* Just warn if this fails, odds are a crash is already in progress */
		if (result != VK_SUCCESS)
		{
			FNA3D_LogWarn("vkWaitForFences: %s", VkErrorMessages(result));
		}

		renderer->textureStagingBuffer->transferInProgress = 0;
		renderer->bufferDefragInProgress = 0;
	}
}

static inline uint8_t VULKAN_INTERNAL_GetBufferOffset(
	VulkanBuffer *stagingBuffer,
	VkDeviceSize *finalOffset,
	VkDeviceSize *stagingOffset,
	uint32_t uploadLength,
	VkDeviceSize alignment
) {
	if (stagingBuffer == NULL)
	{
		return 0;
	}

	/* Offset needs to be aligned to the texel size */
	*stagingOffset = *finalOffset;
	*stagingOffset += alignment - (*stagingOffset % alignment);

	if ((*stagingOffset + uploadLength) < stagingBuffer->size)
	{
		*finalOffset = *stagingOffset + uploadLength;
		return 1;
	}
	return 0;
}

static void VULKAN_INTERNAL_CopyToStagingBuffer(
	VulkanRenderer *renderer,
	void* data,
	uint32_t uploadLength,
	uint32_t copyLength,
	VulkanSubBuffer **pStagingBuffer,
	VkDeviceSize *pOffset,
	FNA3D_SurfaceFormat format
) {
	VulkanSubBuffer *stagingSubBuffer;
	uint8_t *stagingBufferPointer;
	VkDeviceSize offset = 0;
	VkDeviceSize fmtAlignment = VULKAN_INTERNAL_NextHighestAlignment(
		Texture_GetFormatSize(format),
		renderer->physicalDeviceProperties.properties.limits.optimalBufferCopyOffsetAlignment
	);

	VULKAN_INTERNAL_WaitForStagingTransfers(renderer);

	/* Where will we be staging this data? */
	if (VULKAN_INTERNAL_GetBufferOffset(
		renderer->textureStagingBuffer->fastBuffer,
		&renderer->textureStagingBuffer->fastBufferOffset,
		&offset,
		uploadLength,
		fmtAlignment
	)) {
		/* We have access to a fast buffer! */
		stagingSubBuffer = renderer->textureStagingBuffer->fastBuffer->subBuffers[0];
	}
	else
	{
		/* We had to fall back to a slow buffer... */
		if (!VULKAN_INTERNAL_GetBufferOffset(
			renderer->textureStagingBuffer->slowBuffer,
			&renderer->textureStagingBuffer->slowBufferOffset,
			&offset,
			uploadLength,
			fmtAlignment
		)) {
			/* ... and it ran out of space, good grief */
			VULKAN_INTERNAL_FlushCommands(renderer, 1);
			VULKAN_INTERNAL_ExpandSlowStagingBuffer(renderer, uploadLength);

			VULKAN_INTERNAL_GetBufferOffset(
				renderer->textureStagingBuffer->slowBuffer,
				&renderer->textureStagingBuffer->slowBufferOffset,
				&offset,
				uploadLength,
				fmtAlignment
			);
		}

		stagingSubBuffer = renderer->textureStagingBuffer->slowBuffer->subBuffers[0];
	}

	stagingBufferPointer =
		stagingSubBuffer->usedRegion->allocation->mapPointer +
		stagingSubBuffer->usedRegion->resourceOffset +
		offset;

	SDL_memcpy(
		stagingBufferPointer,
		data,
		copyLength
	);

	*pStagingBuffer = stagingSubBuffer;
	*pOffset = offset;

	renderer->textureStagingBuffer->pendingTransfer = 1;
}

static void VULKAN_INTERNAL_PrepareCopyFromStagingBuffer(
	VulkanRenderer *renderer,
	VkDeviceSize dataLength,
	VulkanSubBuffer **pStagingSubBuffer,
	VkDeviceSize *pOffset,
	void **pStagingBufferPointer
) {
	VULKAN_INTERNAL_WaitForStagingTransfers(renderer);

	if (	renderer->textureStagingBuffer->fastBuffer != NULL &&
		renderer->textureStagingBuffer->fastBufferOffset + dataLength < renderer->textureStagingBuffer->fastBuffer->size	)
	{
		*pStagingSubBuffer = renderer->textureStagingBuffer->fastBuffer->subBuffers[0];
		*pOffset = renderer->textureStagingBuffer->fastBufferOffset;
		*pStagingBufferPointer =
			renderer->textureStagingBuffer->fastBuffer->subBuffers[0]->usedRegion->allocation->mapPointer +
			renderer->textureStagingBuffer->fastBuffer->subBuffers[0]->usedRegion->resourceOffset +
			renderer->textureStagingBuffer->fastBufferOffset;
	}
	else
	{
		if (renderer->textureStagingBuffer->slowBufferOffset + dataLength > renderer->textureStagingBuffer->slowBuffer->size)
		{
			VULKAN_INTERNAL_FlushCommands(renderer, 1);
			VULKAN_INTERNAL_ExpandSlowStagingBuffer(renderer, dataLength);
		}

		*pStagingSubBuffer = renderer->textureStagingBuffer->slowBuffer->subBuffers[0];
		*pOffset = renderer->textureStagingBuffer->slowBufferOffset;
		*pStagingBufferPointer =
			renderer->textureStagingBuffer->slowBuffer->subBuffers[0]->usedRegion->allocation->mapPointer +
			renderer->textureStagingBuffer->slowBuffer->subBuffers[0]->usedRegion->resourceOffset +
			renderer->textureStagingBuffer->slowBufferOffset;
	}

	renderer->textureStagingBuffer->pendingTransfer = 1;
}

static void VULKAN_INTERNAL_CreateTextureStagingBuffer(
	VulkanRenderer *renderer
) {
	renderer->textureStagingBuffer = SDL_malloc(sizeof(VulkanStagingBuffer));

	VULKAN_INTERNAL_CreateFastStagingBuffer(renderer, FAST_TEXTURE_STAGING_SIZE);
	renderer->textureStagingBuffer->fastBufferOffset = 0;

	VULKAN_INTERNAL_CreateSlowStagingBuffer(renderer, STARTING_SLOW_TEXTURE_STAGING_SIZE);
	renderer->textureStagingBuffer->slowBufferOffset = 0;

	renderer->textureStagingBuffer->pendingTransfer = 0;
	renderer->textureStagingBuffer->transferInProgress = 0;
}

/* Vulkan: Descriptor Set Logic */

static uint8_t VULKAN_INTERNAL_CreateDescriptorPool(
	VulkanRenderer *renderer,
	VkDescriptorType descriptorType,
	uint32_t descriptorSetCount,
	uint32_t descriptorCount,
	VkDescriptorPool *pDescriptorPool
) {
	VkResult vulkanResult;

	VkDescriptorPoolSize descriptorPoolSize;
	VkDescriptorPoolCreateInfo descriptorPoolInfo;

	descriptorPoolSize.type = descriptorType;
	descriptorPoolSize.descriptorCount = descriptorCount;

	descriptorPoolInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO;
	descriptorPoolInfo.pNext = NULL;
	descriptorPoolInfo.flags = 0;
	descriptorPoolInfo.maxSets = descriptorSetCount;
	descriptorPoolInfo.poolSizeCount = 1;
	descriptorPoolInfo.pPoolSizes = &descriptorPoolSize;

	vulkanResult = renderer->vkCreateDescriptorPool(
		renderer->logicalDevice,
		&descriptorPoolInfo,
		NULL,
		pDescriptorPool
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateDescriptorPool, 0)

	return 1;
}

static uint8_t VULKAN_INTERNAL_AllocateDescriptorSets(
	VulkanRenderer *renderer,
	VkDescriptorPool descriptorPool,
	VkDescriptorSetLayout descriptorSetLayout,
	uint32_t descriptorSetCount,
	VkDescriptorSet *descriptorSetArray
) {
	VkResult vulkanResult;
	uint32_t i;
	VkDescriptorSetAllocateInfo descriptorSetAllocateInfo;
	VkDescriptorSetLayout *descriptorSetLayouts = SDL_stack_alloc(VkDescriptorSetLayout, descriptorSetCount);

	for (i = 0; i < descriptorSetCount; i += 1)
	{
		descriptorSetLayouts[i] = descriptorSetLayout;
	}

	descriptorSetAllocateInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO;
	descriptorSetAllocateInfo.pNext = NULL;
	descriptorSetAllocateInfo.descriptorPool = descriptorPool;
	descriptorSetAllocateInfo.descriptorSetCount = descriptorSetCount;
	descriptorSetAllocateInfo.pSetLayouts = descriptorSetLayouts;

	vulkanResult = renderer->vkAllocateDescriptorSets(
		renderer->logicalDevice,
		&descriptorSetAllocateInfo,
		descriptorSetArray
	);

	SDL_stack_free(descriptorSetLayouts);

	VULKAN_ERROR_CHECK(vulkanResult, vkAllocateDescriptorSets, 0)

	return 1;
}

static uint16_t VULKAN_INTERNAL_FetchSamplerBitmask(
	MOJOSHADER_vkShader *shader
) {
	const MOJOSHADER_parseData *parseData = MOJOSHADER_vkGetShaderParseData(shader);
	uint16_t bitmask = 0;
	uint8_t i;

	for (i = 0; i < parseData->sampler_count; i += 1)
	{
		bitmask |= (1 << parseData->samplers[i].index);
	}

	return bitmask;
}

static VkDescriptorSetLayout VULKAN_INTERNAL_FetchSamplerDescriptorSetLayout(
	VulkanRenderer *renderer,
	MOJOSHADER_vkShader *shader,
	VkShaderStageFlagBits stageFlag
) {
	DescriptorSetLayoutHash descriptorSetLayoutHash;
	VkDescriptorSetLayout descriptorSetLayout;

	VkDescriptorSetLayoutBinding setLayoutBindings[MAX_TEXTURE_SAMPLERS];
	VkDescriptorSetLayoutCreateInfo setLayoutCreateInfo;

	uint32_t samplerCount = MOJOSHADER_vkGetShaderParseData(shader)->sampler_count;
	MOJOSHADER_sampler *samplerInfos = MOJOSHADER_vkGetShaderParseData(shader)->samplers;

	VkResult vulkanResult;
	uint32_t i;

	descriptorSetLayoutHash.descriptorType = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
	descriptorSetLayoutHash.stageFlag = stageFlag;
	descriptorSetLayoutHash.bitmask = VULKAN_INTERNAL_FetchSamplerBitmask(shader);

	descriptorSetLayout = DescriptorSetLayoutHashTable_Fetch(&renderer->descriptorSetLayoutTable, descriptorSetLayoutHash);

	if (descriptorSetLayout != VK_NULL_HANDLE)
	{
		return descriptorSetLayout;
	}

	if (samplerCount == 0) /* dummy sampler case */
	{
		setLayoutBindings[0].binding = 0;
		setLayoutBindings[0].descriptorCount = 1;
		setLayoutBindings[0].descriptorType = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
		setLayoutBindings[0].stageFlags = stageFlag;
		setLayoutBindings[0].pImmutableSamplers = NULL;
	}
	else
	{
		for (i = 0; i < samplerCount; i += 1)
		{
			setLayoutBindings[i].binding = samplerInfos[i].index;
			setLayoutBindings[i].descriptorCount = 1;
			setLayoutBindings[i].descriptorType = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
			setLayoutBindings[i].stageFlags = stageFlag;
			setLayoutBindings[i].pImmutableSamplers = NULL;
		}
	}

	setLayoutCreateInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
	setLayoutCreateInfo.pNext = NULL;
	setLayoutCreateInfo.flags = 0;
	setLayoutCreateInfo.bindingCount = SDL_max(samplerCount, 1);
	setLayoutCreateInfo.pBindings = setLayoutBindings;

	vulkanResult = renderer->vkCreateDescriptorSetLayout(
		renderer->logicalDevice,
		&setLayoutCreateInfo,
		NULL,
		&descriptorSetLayout
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateDescriptorSetLayout, VK_NULL_HANDLE)

	DescriptorSetLayoutHashTable_Insert(
		&renderer->descriptorSetLayoutTable,
		descriptorSetLayoutHash,
		descriptorSetLayout
	);

	return descriptorSetLayout;
}

static void ShaderResources_Destroy(
	VulkanRenderer *renderer,
	ShaderResources *shaderResources
) {
	uint32_t i;

	for (i = 0; i < shaderResources->samplerDescriptorPoolCount; i += 1)
	{
		renderer->vkDestroyDescriptorPool(
			renderer->logicalDevice,
			shaderResources->samplerDescriptorPools[i],
			NULL
		);
	}

	SDL_free(shaderResources->samplerDescriptorPools);
	SDL_free(shaderResources->samplerBindingIndices);
	SDL_free(shaderResources->inactiveDescriptorSets);
	SDL_free(shaderResources->elements);

	for (i = 0; i < NUM_DESCRIPTOR_SET_HASH_BUCKETS; i += 1)
	{
		SDL_free(shaderResources->buckets[i].elements);
	}

	SDL_free(shaderResources);
}

static ShaderResources *ShaderResources_Init(
	VulkanRenderer *renderer,
	MOJOSHADER_vkShader *shader,
	VkShaderStageFlagBits shaderStageFlag
) {
	uint32_t i;
	VkBuffer vUniform, fUniform;
	VkWriteDescriptorSet writeDescriptorSet;
	unsigned long long vOff, fOff, vSize, fSize;
	ShaderResources *shaderResources = SDL_malloc(sizeof(ShaderResources));

	/*
	 * Lock to prevent mojoshader resources
	 * from being overwritten during initialization
	 */
	SDL_LockMutex(renderer->passLock);

	shaderResources->elements = SDL_malloc(sizeof(SamplerDescriptorSetHashMap) * 16);
	shaderResources->count = 0;
	shaderResources->capacity = 16;

	for (i = 0; i < NUM_DESCRIPTOR_SET_HASH_BUCKETS; i += 1)
	{
		shaderResources->buckets[i].elements = NULL;
		shaderResources->buckets[i].count = 0;
		shaderResources->buckets[i].capacity = 0;
	}

	shaderResources->samplerLayout = VULKAN_INTERNAL_FetchSamplerDescriptorSetLayout(renderer, shader, shaderStageFlag);
	shaderResources->samplerCount = MOJOSHADER_vkGetShaderParseData(shader)->sampler_count;

	shaderResources->samplerDescriptorPools = SDL_malloc(sizeof(VkDescriptorPool));
	shaderResources->samplerDescriptorPoolCount = 1;

	VULKAN_INTERNAL_CreateDescriptorPool(
		renderer,
		VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
		STARTING_SAMPLER_DESCRIPTOR_POOL_SIZE,
		STARTING_SAMPLER_DESCRIPTOR_POOL_SIZE * SDL_max(shaderResources->samplerCount, 1), /* in case of dummy data */
		&shaderResources->samplerDescriptorPools[0]
	);

	shaderResources->nextPoolSize = STARTING_SAMPLER_DESCRIPTOR_POOL_SIZE * 2;

	shaderResources->samplerBindingIndices = SDL_malloc(sizeof(uint8_t) * shaderResources->samplerCount);

	for (i = 0; i < shaderResources->samplerCount; i += 1)
	{
		shaderResources->samplerBindingIndices[i] = MOJOSHADER_vkGetShaderParseData(shader)->samplers[i].index;
	}

	shaderResources->inactiveDescriptorSetCapacity = STARTING_SAMPLER_DESCRIPTOR_POOL_SIZE;
	shaderResources->inactiveDescriptorSetCount = STARTING_SAMPLER_DESCRIPTOR_POOL_SIZE - 1;
	shaderResources->inactiveDescriptorSets = SDL_malloc(sizeof(VkDescriptorSet) * STARTING_SAMPLER_DESCRIPTOR_POOL_SIZE);

	VULKAN_INTERNAL_AllocateDescriptorSets(
		renderer,
		shaderResources->samplerDescriptorPools[0],
		shaderResources->samplerLayout,
		STARTING_SAMPLER_DESCRIPTOR_POOL_SIZE,
		shaderResources->inactiveDescriptorSets
	);

	MOJOSHADER_vkGetUniformBuffers(&vUniform, &vOff, &vSize, &fUniform, &fOff, &fSize);

	if (shaderStageFlag == VK_SHADER_STAGE_VERTEX_BIT)
	{
		VULKAN_INTERNAL_AllocateDescriptorSets(
			renderer,
			renderer->uniformBufferDescriptorPool,
			renderer->vertexUniformBufferDescriptorSetLayout,
			1,
			&shaderResources->uniformDescriptorSet
		);

		shaderResources->uniformBufferInfo.buffer = vUniform;
		shaderResources->uniformBufferInfo.offset = 0;
		shaderResources->uniformBufferInfo.range = vSize;
	}
	else
	{
		VULKAN_INTERNAL_AllocateDescriptorSets(
			renderer,
			renderer->uniformBufferDescriptorPool,
			renderer->fragUniformBufferDescriptorSetLayout,
			1,
			&shaderResources->uniformDescriptorSet
		);

		shaderResources->uniformBufferInfo.buffer = fUniform;
		shaderResources->uniformBufferInfo.offset = 0;
		shaderResources->uniformBufferInfo.range = fSize;
	}

	if (shaderResources->uniformBufferInfo.buffer != VK_NULL_HANDLE)
	{
		writeDescriptorSet.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
		writeDescriptorSet.pNext = NULL;
		writeDescriptorSet.descriptorCount = 1;
		writeDescriptorSet.descriptorType = VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
		writeDescriptorSet.dstArrayElement = 0;
		writeDescriptorSet.dstBinding = 0;
		writeDescriptorSet.dstSet = shaderResources->uniformDescriptorSet;
		writeDescriptorSet.pBufferInfo = &shaderResources->uniformBufferInfo;
		writeDescriptorSet.pImageInfo = NULL;

		renderer->vkUpdateDescriptorSets(
			renderer->logicalDevice,
			1,
			&writeDescriptorSet,
			0,
			NULL
		);
	}

	SDL_UnlockMutex(renderer->passLock);

	return shaderResources;
}

static ShaderResources* VULKAN_INTERNAL_FetchShaderResources(
	VulkanRenderer *renderer,
	MOJOSHADER_vkShader *shader,
	VkShaderStageFlagBits shaderStageFlag
) {
	ShaderResources *shaderResources = ShaderResourcesHashTable_Fetch(&renderer->shaderResourcesHashTable, shader);

	if (shaderResources == VK_NULL_HANDLE)
	{
		shaderResources = ShaderResources_Init(renderer, shader, shaderStageFlag);
		ShaderResourcesHashTable_Insert(&renderer->shaderResourcesHashTable, shader, shaderResources);
	}

	return shaderResources;
}

static inline uint8_t SamplerDescriptorSetDataEqual(
	SamplerDescriptorSetData *a,
	SamplerDescriptorSetData *b,
	uint8_t samplerCount
) {
	uint32_t i;

	for (i = 0; i < samplerCount; i += 1)
	{
		if (	a->descriptorImageInfo[i].imageLayout != b->descriptorImageInfo[i].imageLayout ||
			a->descriptorImageInfo[i].imageView != b->descriptorImageInfo[i].imageView ||
			a->descriptorImageInfo[i].sampler != b->descriptorImageInfo[i].sampler	)
		{
			return 0;
		}
	}

	return 1;
}

/* Fetches or creates a new descriptor set based on data */
static VkDescriptorSet ShaderResources_FetchDescriptorSet(
	VulkanRenderer *renderer,
	ShaderResources *shaderResources,
	SamplerDescriptorSetData *value
) {
	uint32_t i;
	VkDescriptorSet newDescriptorSet;
	VkWriteDescriptorSet writeDescriptorSets[MAX_TEXTURE_SAMPLERS];
	SamplerDescriptorSetHashMap *map;
	uint64_t hashcode = SamplerDescriptorSetHashTable_GetHashCode(value, SDL_max(shaderResources->samplerCount, 1));
	SamplerDescriptorSetHashArray *arr = &shaderResources->buckets[hashcode % NUM_DESCRIPTOR_SET_HASH_BUCKETS];

	for (i = 0; i < arr->count; i += 1)
	{
		SamplerDescriptorSetHashMap *e = &shaderResources->elements[arr->elements[i]];
		if (SamplerDescriptorSetDataEqual(value, &e->descriptorSetData, shaderResources->samplerCount))
		{
			e->inactiveFrameCount = 0;
			return e->descriptorSet;
		}
	}

	/* If no match exists, assign a new descriptor set and prepare it for update */
	/* If no inactive descriptor sets remain, create a new pool and allocate new inactive sets */

	if (shaderResources->inactiveDescriptorSetCount == 0)
	{
		shaderResources->samplerDescriptorPoolCount += 1;
		shaderResources->samplerDescriptorPools = SDL_realloc(
			shaderResources->samplerDescriptorPools,
			sizeof(VkDescriptorPool) * shaderResources->samplerDescriptorPoolCount
		);

		VULKAN_INTERNAL_CreateDescriptorPool(
			renderer,
			VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
			shaderResources->nextPoolSize,
			shaderResources->nextPoolSize * SDL_max(shaderResources->samplerCount, 1), /* dont want 0 in case of dummy data */
			&shaderResources->samplerDescriptorPools[shaderResources->samplerDescriptorPoolCount - 1]
		);

		shaderResources->inactiveDescriptorSetCapacity += shaderResources->nextPoolSize;

		shaderResources->inactiveDescriptorSets = SDL_realloc(
			shaderResources->inactiveDescriptorSets,
			sizeof(VkDescriptorSet) * shaderResources->inactiveDescriptorSetCapacity
		);

		VULKAN_INTERNAL_AllocateDescriptorSets(
			renderer,
			shaderResources->samplerDescriptorPools[shaderResources->samplerDescriptorPoolCount - 1],
			shaderResources->samplerLayout,
			shaderResources->nextPoolSize,
			shaderResources->inactiveDescriptorSets
		);

		shaderResources->inactiveDescriptorSetCount = shaderResources->nextPoolSize;

		shaderResources->nextPoolSize *= 2;
	}

	newDescriptorSet = shaderResources->inactiveDescriptorSets[shaderResources->inactiveDescriptorSetCount - 1];
	shaderResources->inactiveDescriptorSetCount -= 1;

	for (i = 0; i < shaderResources->samplerCount; i += 1)
	{
		writeDescriptorSets[i].sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
		writeDescriptorSets[i].pNext = NULL;
		writeDescriptorSets[i].descriptorCount = 1;
		writeDescriptorSets[i].descriptorType = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
		writeDescriptorSets[i].dstArrayElement = 0;
		writeDescriptorSets[i].dstBinding = shaderResources->samplerBindingIndices[i];
		writeDescriptorSets[i].dstSet = newDescriptorSet;
		writeDescriptorSets[i].pBufferInfo = NULL;
		writeDescriptorSets[i].pImageInfo = &value->descriptorImageInfo[i];
	}

	renderer->vkUpdateDescriptorSets(
		renderer->logicalDevice,
		shaderResources->samplerCount,
		writeDescriptorSets,
		0,
		NULL
	);

	EXPAND_ARRAY_IF_NEEDED(arr, 2, uint32_t)
	arr->elements[arr->count] = shaderResources->count;
	arr->count += 1;

	if (shaderResources->count == shaderResources->capacity)
	{
		shaderResources->capacity *= 2;

		shaderResources->elements = SDL_realloc(
			shaderResources->elements,
			sizeof(SamplerDescriptorSetHashMap) * shaderResources->capacity
		);
	}
	map = &shaderResources->elements[shaderResources->count];
	map->key = hashcode;
	for (i = 0; i < shaderResources->samplerCount; i += 1)
	{
		map->descriptorSetData.descriptorImageInfo[i].imageLayout =
			value->descriptorImageInfo[i].imageLayout;
		map->descriptorSetData.descriptorImageInfo[i].imageView =
			value->descriptorImageInfo[i].imageView;
		map->descriptorSetData.descriptorImageInfo[i].sampler =
			value->descriptorImageInfo[i].sampler;
	}
	map->descriptorSet = newDescriptorSet;
	map->inactiveFrameCount = 0;
	shaderResources->count += 1;

	return newDescriptorSet;
}

static void ShaderResources_DeactivateDescriptorSet(
	ShaderResources* shaderResources,
	uint32_t index
) {
	uint32_t j;
	SamplerDescriptorSetHashArray* arr;

	arr = &shaderResources->buckets[shaderResources->elements[index].key % NUM_DESCRIPTOR_SET_HASH_BUCKETS];

	/* remove index from bucket */
	for (j = 0; j < arr->count; j += 1)
	{
		if (arr->elements[j] == index)
		{
			if (j < arr->count - 1)
			{
				arr->elements[j] = arr->elements[arr->count - 1];
			}

			arr->count -= 1;
			break;
		}
	}

	/* remove element from table and place in inactive sets */

	shaderResources->inactiveDescriptorSets[shaderResources->inactiveDescriptorSetCount] = shaderResources->elements[index].descriptorSet;
	shaderResources->inactiveDescriptorSetCount += 1;

	/* move another descriptor set to fill the hole */
	if (index < shaderResources->count - 1)
	{
		shaderResources->elements[index] = shaderResources->elements[shaderResources->count - 1];

		/* update index in bucket */
		arr = &shaderResources->buckets[shaderResources->elements[index].key % NUM_DESCRIPTOR_SET_HASH_BUCKETS];

		for (j = 0; j < arr->count; j += 1)
		{
			if (arr->elements[j] == shaderResources->count - 1)
			{
				arr->elements[j] = index;
				break;
			}
		}
	}

	shaderResources->count -= 1;

}

static void ShaderResources_InvalidateDescriptorSet(
	VulkanRenderer *renderer,
	VkImageView view
) {
	int32_t i, j, m, n;
	ShaderResources* shaderResources;

	for (i = 0; i < NUM_SHADER_RESOURCES_BUCKETS; i += 1)
	{
		for (j = 0; j < renderer->shaderResourcesHashTable.buckets[i].count; j += 1)
		{
			shaderResources = renderer->shaderResourcesHashTable.buckets[i].elements[j].value;

			for (m = shaderResources->count - 1; m >= 0; m -= 1)
			{
				for (n = 0; n < shaderResources->samplerCount; n += 1)
				{
					if (shaderResources->elements[m].descriptorSetData.descriptorImageInfo[n].imageView == view)
					{
						ShaderResources_DeactivateDescriptorSet(shaderResources, m);
					}
				}
			}
		}
	}
}

static void ShaderResources_DeactivateUnusedDescriptorSets(
	ShaderResources *shaderResources
) {
	int32_t i;

	for (i = shaderResources->count - 1; i >= 0; i -= 1)
	{
		shaderResources->elements[i].inactiveFrameCount += 1;

		if (shaderResources->elements[i].inactiveFrameCount + 1 > DESCRIPTOR_SET_DEACTIVATE_FRAMES)
		{
			ShaderResources_DeactivateDescriptorSet(shaderResources, i);
		}
	}
}

/* Must take an array of descriptor sets of size 4 */
static void VULKAN_INTERNAL_FetchDescriptorSetDataAndOffsets(
	VulkanRenderer *renderer,
	ShaderResources *vertShaderResources,
	ShaderResources *fragShaderResources,
	VkDescriptorSet *descriptorSets,
	uint32_t *dynamicOffsets
) {
	VkBuffer vUniform, fUniform;
	unsigned long long vOff, fOff, vSize, fSize; /* MojoShader type */

	MOJOSHADER_vkShader *vertShader, *fragShader;
	MOJOSHADER_samplerType samplerType;

	SamplerDescriptorSetData vertexSamplerDescriptorSetData;
	SamplerDescriptorSetData fragSamplerDescriptorSetData;

	uint32_t i;

	MOJOSHADER_vkGetBoundShaders(&vertShader, &fragShader);

	if (renderer->vertexSamplerDescriptorSetDataNeedsUpdate)
	{
		if (vertShaderResources->samplerCount == 0)
		{
			/* in case we have 0 samplers, bind dummy data */
			vertexSamplerDescriptorSetData.descriptorImageInfo[0].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
			vertexSamplerDescriptorSetData.descriptorImageInfo[0].imageView = renderer->dummyVertTexture->view;
			vertexSamplerDescriptorSetData.descriptorImageInfo[0].sampler = renderer->dummyVertSamplerState;
		}
		else
		{
			for (i = 0; i < vertShaderResources->samplerCount; i += 1)
			{
				if (renderer->textures[MAX_TEXTURE_SAMPLERS + vertShaderResources->samplerBindingIndices[i]] != &NullTexture)
				{
					vertexSamplerDescriptorSetData.descriptorImageInfo[i].imageView = renderer->textures[MAX_TEXTURE_SAMPLERS + vertShaderResources->samplerBindingIndices[i]]->view;
					vertexSamplerDescriptorSetData.descriptorImageInfo[i].sampler = renderer->samplers[MAX_TEXTURE_SAMPLERS + vertShaderResources->samplerBindingIndices[i]];
					vertexSamplerDescriptorSetData.descriptorImageInfo[i].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
				}
				else
				{
					samplerType = MOJOSHADER_vkGetShaderParseData(vertShader)->samplers[i].type;
					if (samplerType == MOJOSHADER_SAMPLER_2D)
					{
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].imageView = renderer->dummyVertTexture->view;
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].sampler = renderer->dummyVertSamplerState;
					}
					else if (samplerType == MOJOSHADER_SAMPLER_VOLUME)
					{
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].imageView = renderer->dummyVertTexture3D->view;
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].sampler = renderer->dummyVertSampler3DState;
					}
					else if (samplerType == MOJOSHADER_SAMPLER_CUBE)
					{
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].imageView = renderer->dummyVertTextureCube->view;
						vertexSamplerDescriptorSetData.descriptorImageInfo[i].sampler = renderer->dummyVertSamplerCubeState;
					}
				}
			}
		}

		renderer->currentVertexSamplerDescriptorSet = ShaderResources_FetchDescriptorSet(
			renderer,
			vertShaderResources,
			&vertexSamplerDescriptorSetData
		);
	}

	if (renderer->fragSamplerDescriptorSetDataNeedsUpdate)
	{
		if (fragShaderResources->samplerCount == 0)
		{
			fragSamplerDescriptorSetData.descriptorImageInfo[0].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
			fragSamplerDescriptorSetData.descriptorImageInfo[0].imageView = renderer->dummyFragTexture->view;
			fragSamplerDescriptorSetData.descriptorImageInfo[0].sampler = renderer->dummyFragSamplerState;
		}
		else
		{
			for (i = 0; i < fragShaderResources->samplerCount; i += 1)
			{
				if (renderer->textures[fragShaderResources->samplerBindingIndices[i]] != &NullTexture)
				{
					fragSamplerDescriptorSetData.descriptorImageInfo[i].imageView = renderer->textures[fragShaderResources->samplerBindingIndices[i]]->view;
					fragSamplerDescriptorSetData.descriptorImageInfo[i].sampler = renderer->samplers[fragShaderResources->samplerBindingIndices[i]];
					fragSamplerDescriptorSetData.descriptorImageInfo[i].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
				}
				else
				{

					samplerType = MOJOSHADER_vkGetShaderParseData(fragShader)->samplers[i].type;
					if (samplerType == MOJOSHADER_SAMPLER_2D)
					{
						fragSamplerDescriptorSetData.descriptorImageInfo[i].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
						fragSamplerDescriptorSetData.descriptorImageInfo[i].imageView = renderer->dummyFragTexture->view;
						fragSamplerDescriptorSetData.descriptorImageInfo[i].sampler = renderer->dummyFragSamplerState;
					}
					else if (samplerType == MOJOSHADER_SAMPLER_VOLUME)
					{
						fragSamplerDescriptorSetData.descriptorImageInfo[i].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
						fragSamplerDescriptorSetData.descriptorImageInfo[i].imageView = renderer->dummyFragTexture3D->view;
						fragSamplerDescriptorSetData.descriptorImageInfo[i].sampler = renderer->dummyFragSampler3DState;
					}
					else if (samplerType == MOJOSHADER_SAMPLER_CUBE)
					{
						fragSamplerDescriptorSetData.descriptorImageInfo[i].imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
						fragSamplerDescriptorSetData.descriptorImageInfo[i].imageView = renderer->dummyFragTextureCube->view;
						fragSamplerDescriptorSetData.descriptorImageInfo[i].sampler = renderer->dummyFragSamplerCubeState;
					}
				}
			}
		}

		renderer->currentFragSamplerDescriptorSet = ShaderResources_FetchDescriptorSet(
			renderer,
			fragShaderResources,
			&fragSamplerDescriptorSetData
		);
	}

	renderer->vertexSamplerDescriptorSetDataNeedsUpdate = 0;
	renderer->fragSamplerDescriptorSetDataNeedsUpdate = 0;

	descriptorSets[0] = renderer->currentVertexSamplerDescriptorSet;
	descriptorSets[1] = renderer->currentFragSamplerDescriptorSet;
	descriptorSets[2] = vertShaderResources->uniformDescriptorSet;
	descriptorSets[3] = fragShaderResources->uniformDescriptorSet;

	MOJOSHADER_vkGetUniformBuffers(&vUniform, &vOff, &vSize, &fUniform, &fOff, &fSize);

	dynamicOffsets[0] = vOff;
	dynamicOffsets[1] = fOff;
}

static void VULKAN_INTERNAL_ResetDescriptorSetData(VulkanRenderer *renderer)
{
	uint32_t i, j;
	ShaderResources *shaderResources;

	for (i = 0; i < NUM_SHADER_RESOURCES_BUCKETS; i += 1)
	{
		for (j = 0; j < renderer->shaderResourcesHashTable.buckets[i].count; j += 1)
		{
			shaderResources = renderer->shaderResourcesHashTable.buckets[i].elements[j].value;
			ShaderResources_DeactivateUnusedDescriptorSets(shaderResources);
		}
	}

	renderer->vertexSamplerDescriptorSetDataNeedsUpdate = 1;
	renderer->fragSamplerDescriptorSetDataNeedsUpdate = 1;
}

/* Vulkan: Command Submission */

static void VULKAN_INTERNAL_SwapChainBlit(
	VulkanRenderer *renderer,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	uint32_t swapChainImageIndex
) {
	FNA3D_Rect srcRect;
	FNA3D_Rect dstRect;
	VkImageBlit blit;

	if (sourceRectangle != NULL)
	{
		srcRect = *sourceRectangle;
	}
	else
	{
		srcRect.x = 0;
		srcRect.y = 0;
		srcRect.w = renderer->fauxBackbufferWidth;
		srcRect.h = renderer->fauxBackbufferHeight;
	}

	if (destinationRectangle != NULL)
	{
		dstRect = *destinationRectangle;
	}
	else
	{
		dstRect.x = 0;
		dstRect.y = 0;
		dstRect.w = renderer->swapChainExtent.width;
		dstRect.h = renderer->swapChainExtent.height;
	}

	/* Blit the framebuffer! */

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_READ,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		1,
		0,
		1,
		0,
		renderer->fauxBackbufferColor.handle->image,
		&renderer->fauxBackbufferColor.handle->resourceAccessType
	);

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_WRITE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		1,
		0,
		1,
		0,
		renderer->swapChainImages[swapChainImageIndex],
		&renderer->swapChainResourceAccessTypes[swapChainImageIndex]
	);

	blit.srcOffsets[0].x = srcRect.x;
	blit.srcOffsets[0].y = srcRect.y;
	blit.srcOffsets[0].z = 0;
	blit.srcOffsets[1].x = srcRect.x + srcRect.w;
	blit.srcOffsets[1].y = srcRect.y + srcRect.h;
	blit.srcOffsets[1].z = 1;

	blit.srcSubresource.mipLevel = 0;
	blit.srcSubresource.baseArrayLayer = 0;
	blit.srcSubresource.layerCount = 1;
	blit.srcSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;

	blit.dstOffsets[0].x = dstRect.x;
	blit.dstOffsets[0].y = dstRect.y;
	blit.dstOffsets[0].z = 0;
	blit.dstOffsets[1].x = dstRect.x + dstRect.w;
	blit.dstOffsets[1].y = dstRect.y + dstRect.h;
	blit.dstOffsets[1].z = 1;

	blit.dstSubresource.mipLevel = 0;
	blit.dstSubresource.baseArrayLayer = 0;
	blit.dstSubresource.layerCount = 1;
	blit.dstSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;

	RECORD_CMD(renderer->vkCmdBlitImage(
		renderer->currentCommandBuffer,
		renderer->fauxBackbufferColor.handle->image,
		VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
		renderer->swapChainImages[swapChainImageIndex],
		VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
		1,
		&blit,
		VK_FILTER_LINEAR
	));

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_PRESENT,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		1,
		0,
		1,
		0,
		renderer->swapChainImages[swapChainImageIndex],
		&renderer->swapChainResourceAccessTypes[swapChainImageIndex]
	);

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_COLOR_ATTACHMENT_READ_WRITE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		1,
		0,
		1,
		0,
		renderer->fauxBackbufferColor.handle->image,
		&renderer->fauxBackbufferColor.handle->resourceAccessType
	);
}

static void VULKAN_INTERNAL_SubmitCommands(
	VulkanRenderer *renderer,
	uint8_t present
) {
	VkFence fences[2];
	VkSemaphore semaphores[2];
	VkSubmitInfo submitInfo;
	uint32_t i, j;
	VkResult result, acquireResult, presentResult = VK_SUCCESS;
	uint8_t acquireSuccess = 0;
	uint8_t performDefrag = 0;
	uint32_t swapChainImageIndex;

	FNA3D_Rect *sourceRectangle, *destinationRectangle;

	SDL_DisplayMode mode;
	VkPipelineStageFlags waitStages = VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
	VkPresentInfoKHR presentInfo;
	struct
	{
		VkStructureType sType;
		const void *pNext;
		uint64_t frameToken;
	} presentInfoGGP;

	sourceRectangle = renderer->presentSourceRectangle;
	destinationRectangle = renderer->presentDestinationRectangle;

	/* Must end render pass before ending command buffer */
	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 0);

	if (present)
	{
		SDL_GetCurrentDisplayMode(
			SDL_GetWindowDisplayIndex(
				renderer->presentOverrideWindowHandle
			),
			&mode
		);

		/* Begin next frame */
		acquireResult = renderer->vkAcquireNextImageKHR(
			renderer->logicalDevice,
			renderer->swapChain,
			10000000000 / mode.refresh_rate, /* ~10 frames, so we'll progress even if throttled to zero. */
			renderer->imageAvailableSemaphore,
			VK_NULL_HANDLE,
			&swapChainImageIndex
		);

		if (acquireResult == VK_SUCCESS || acquireResult == VK_SUBOPTIMAL_KHR)
		{
			VULKAN_INTERNAL_SwapChainBlit(
				renderer,
				sourceRectangle,
				destinationRectangle,
				swapChainImageIndex
			);

			acquireSuccess = 1;
		}
	}

	if (renderer->activeCommandBufferCount <= 1 && renderer->numActiveCommands == 0)
	{
		/* No commands recorded, bailing out */
		return;
	}

	if (renderer->currentCommandBuffer != NULL)
	{
		VULKAN_INTERNAL_EndCommandBuffer(renderer, 0, 0);
	}

	fences[0] = renderer->inFlightFence;
	fences[1] = renderer->defragFence;

	/* Wait for the previous submission to complete */
	result = renderer->vkWaitForFences(
		renderer->logicalDevice,
		2,
		fences,
		VK_TRUE,
		UINT64_MAX
	);

	/* This may return an error on exit, so just quietly bail if waiting
	 * fails for any reason
	 */
	if (result != VK_SUCCESS)
	{
		FNA3D_LogWarn("vkWaitForFences: %s", VkErrorMessages(result));
		return;
	}

	/* Reset descriptor set data */
	VULKAN_INTERNAL_ResetDescriptorSetData(renderer);

	/* Cleanup */
	VULKAN_INTERNAL_PerformDeferredDestroys(renderer);

	renderer->bufferDefragInProgress = 0;

	renderer->submitCounter = (renderer->submitCounter + 1) % 2;

	/* Mark sub buffers of previously submitted buffers as unbound */
	for (i = 0; i < renderer->numSubmittedBuffers; i += 1)
	{
		if (renderer->submittedBuffers[i] != NULL)
		{
			renderer->submittedBuffers[i]->boundSubmitted = 0;

			for (j = 0; j < renderer->submittedBuffers[i]->subBufferCount; j += 1)
			{
				if (renderer->submittedBuffers[i]->subBuffers[j]->bound == renderer->submitCounter)
				{
					renderer->submittedBuffers[i]->subBuffers[j]->bound = -1;
				}
			}

			renderer->submittedBuffers[i] = NULL;
		}
	}

	renderer->numSubmittedBuffers = 0;

	/* Mark currently bound buffers as submitted buffers */

	if (renderer->numBuffersInUse > renderer->maxSubmittedBuffers)
	{
		renderer->submittedBuffers = SDL_realloc(
			renderer->submittedBuffers,
			sizeof(VulkanBuffer*) * renderer->numBuffersInUse
		);

		renderer->maxSubmittedBuffers = renderer->numBuffersInUse;
	}

	for (i = 0; i < renderer->numBuffersInUse; i += 1)
	{
		if (renderer->buffersInUse[i] != NULL)
		{
			renderer->buffersInUse[i]->bound = 0;
			renderer->buffersInUse[i]->boundSubmitted = 1;

			renderer->submittedBuffers[i] = renderer->buffersInUse[i];
			renderer->buffersInUse[i] = NULL;
		}
	}

	renderer->numSubmittedBuffers = renderer->numBuffersInUse;
	renderer->numBuffersInUse = 0;

	/* Reset the previously submitted command buffers */
	for (i = 0; i < renderer->submittedCommandBufferCount; i += 1)
	{
		result = renderer->vkResetCommandBuffer(
			renderer->submittedCommandBuffers[i],
			VK_COMMAND_BUFFER_RESET_RELEASE_RESOURCES_BIT
		);
		VULKAN_ERROR_CHECK(result, vkResetCommandBuffer,)
	}

	/* Mark the previously submitted command buffers as inactive */
	for (i = 0; i < renderer->submittedCommandBufferCount; i += 1)
	{
		renderer->inactiveCommandBuffers[renderer->inactiveCommandBufferCount] = renderer->submittedCommandBuffers[i];
		renderer->inactiveCommandBufferCount += 1;
	}

	renderer->submittedCommandBufferCount = 0;

	/* Decide if we will be defragmenting */
	if (renderer->resourceFreed)
	{
		renderer->defragTimer = 0;
	}
	renderer->resourceFreed = 0;

	if (renderer->needDefrag)
	{
		renderer->defragTimer += 1;

		if (renderer->defragTimer > 5)
		{
			performDefrag = 1;
		}
	}

	/* Prepare the command buffer fence for submission */
	renderer->vkResetFences(
		renderer->logicalDevice,
		1,
		&renderer->inFlightFence
	);

	/* Prepare command submission */
	submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
	submitInfo.pNext = NULL;
	submitInfo.commandBufferCount = renderer->activeCommandBufferCount;
	submitInfo.pCommandBuffers = renderer->activeCommandBuffers;
	submitInfo.signalSemaphoreCount = 0;

	if (present && acquireSuccess)
	{
		submitInfo.waitSemaphoreCount = 1;
		submitInfo.pWaitSemaphores = &renderer->imageAvailableSemaphore;
		submitInfo.pWaitDstStageMask = &waitStages;
		semaphores[submitInfo.signalSemaphoreCount] = renderer->renderFinishedSemaphore;
		submitInfo.signalSemaphoreCount += 1;
	}
	else
	{
		submitInfo.waitSemaphoreCount = 0;
		submitInfo.pWaitSemaphores = NULL;
		submitInfo.pWaitDstStageMask = NULL;
	}

	if (performDefrag)
	{
		semaphores[submitInfo.signalSemaphoreCount] = renderer->defragSemaphore;
		submitInfo.signalSemaphoreCount += 1;
	}

	submitInfo.pSignalSemaphores = semaphores;

	/* Submit the commands, finally. */
	result = renderer->vkQueueSubmit(
		renderer->unifiedQueue,
		1,
		&submitInfo,
		renderer->inFlightFence
	);
	VULKAN_ERROR_CHECK(result, vkQueueSubmit,)

	/* Rotate the UBOs */
	MOJOSHADER_vkEndFrame();

	/* Reset the texture staging buffer */
	VULKAN_INTERNAL_ResetTextureStagingBuffer(renderer);

	/* Mark active command buffers as submitted */
	for (i = 0; i < renderer->activeCommandBufferCount; i += 1)
	{
		renderer->submittedCommandBuffers[renderer->submittedCommandBufferCount] = renderer->activeCommandBuffers[i];
		renderer->submittedCommandBufferCount += 1;
	}

	renderer->activeCommandBufferCount = 0;

	/* Present, if applicable */
	if (present && acquireSuccess)
	{
		if (renderer->supports.GGP_frame_token)
		{
			const void* token = SDL_GetWindowData(
				(SDL_Window*) renderer->presentOverrideWindowHandle,
				"GgpFrameToken"
			);
			presentInfoGGP.sType = VK_STRUCTURE_TYPE_PRESENT_FRAME_TOKEN_GGP;
			presentInfoGGP.pNext = NULL;
			presentInfoGGP.frameToken = (uint64_t) (size_t) token;
			presentInfo.pNext = &presentInfoGGP;
		}
		else
		{
			presentInfo.pNext = NULL;
		}
		presentInfo.sType = VK_STRUCTURE_TYPE_PRESENT_INFO_KHR;
		presentInfo.pNext = NULL;
		presentInfo.waitSemaphoreCount = 1;
		presentInfo.pWaitSemaphores =
			&renderer->renderFinishedSemaphore;
		presentInfo.swapchainCount = 1;
		presentInfo.pSwapchains = &renderer->swapChain;
		presentInfo.pImageIndices = &swapChainImageIndex;
		presentInfo.pResults = NULL;

		presentResult = renderer->vkQueuePresentKHR(
			renderer->unifiedQueue,
			&presentInfo
		);
	}

	/* Now that commands are totally done, check if we need new swapchain */
	if (present)
	{
		if (	acquireResult == VK_ERROR_OUT_OF_DATE_KHR ||
			acquireResult == VK_SUBOPTIMAL_KHR ||
			presentResult == VK_ERROR_OUT_OF_DATE_KHR ||
			presentResult == VK_SUBOPTIMAL_KHR	)
		{
			VULKAN_INTERNAL_RecreateSwapchain(renderer, 0);
		}

		/* This can technically happen anywhere, but this is the nicest
		 * place for it to happen so we try to log it here.
		 */
		if (	acquireResult == VK_ERROR_DEVICE_LOST ||
			presentResult == VK_ERROR_DEVICE_LOST	)
		{
			FNA3D_LogError("Vulkan device was lost!");
		}

		if (!acquireSuccess)
		{
			FNA3D_LogInfo("Failed to acquire swapchain image, not presenting");
		}
	}

	if (performDefrag)
	{
		VULKAN_INTERNAL_DefragmentMemory(renderer);
	}

	/* Activate the next command buffer */
	VULKAN_INTERNAL_BeginCommandBuffer(renderer);
}

static void VULKAN_INTERNAL_FlushCommands(VulkanRenderer *renderer, uint8_t sync)
{
	VkResult result;
	VkFence fences[2];

	SDL_LockMutex(renderer->passLock);
	SDL_LockMutex(renderer->commandLock);
	SDL_LockMutex(renderer->stagingLock);

	VULKAN_INTERNAL_SubmitCommands(renderer, 0);

	if (sync)
	{
		fences[0] = renderer->inFlightFence;
		fences[1] = renderer->defragFence;

		result = renderer->vkWaitForFences(
			renderer->logicalDevice,
			2,
			fences,
			VK_TRUE,
			UINT64_MAX
		);

		/* Just warn if this fails, odds are a crash is already in progress */
		if (result != VK_SUCCESS)
		{
			FNA3D_LogWarn("vkWaitForFences: %s", VkErrorMessages(result));
		}

		renderer->bufferDefragInProgress = 0;
	}

	SDL_UnlockMutex(renderer->passLock);
	SDL_UnlockMutex(renderer->commandLock);
	SDL_UnlockMutex(renderer->stagingLock);
}

static void VULKAN_INTERNAL_FlushCommandsAndPresent(
	VulkanRenderer *renderer,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
) {
	SDL_LockMutex(renderer->passLock);
	SDL_LockMutex(renderer->commandLock);
	SDL_LockMutex(renderer->stagingLock);

	renderer->presentSourceRectangle = sourceRectangle;
	renderer->presentDestinationRectangle = destinationRectangle;
	renderer->presentOverrideWindowHandle = overrideWindowHandle;

	VULKAN_INTERNAL_SubmitCommands(renderer, 1);

	SDL_UnlockMutex(renderer->passLock);
	SDL_UnlockMutex(renderer->commandLock);
	SDL_UnlockMutex(renderer->stagingLock);
}

/* Vulkan: Swapchain */

static inline VkExtent2D ChooseSwapExtent(
	void* windowHandle,
	const VkSurfaceCapabilitiesKHR capabilities
) {
	VkExtent2D actualExtent;
	int32_t drawableWidth, drawableHeight;

	SDL_Vulkan_GetDrawableSize(
		(SDL_Window*) windowHandle,
		&drawableWidth,
		&drawableHeight
	);
	if (	drawableWidth < capabilities.minImageExtent.width ||
		drawableWidth > capabilities.maxImageExtent.width ||
		drawableHeight < capabilities.minImageExtent.height ||
		drawableHeight > capabilities.maxImageExtent.height	)
	{
		FNA3D_LogWarn("Drawable size not possible for this VkSurface!");

		if (capabilities.currentExtent.width != UINT32_MAX)
		{
			FNA3D_LogWarn("Falling back to currentExtent.");
			return capabilities.currentExtent;
		}
		else
		{
			FNA3D_LogError("No fallback swapchain size available!");
		}
	}

	actualExtent.width = drawableWidth;
	actualExtent.height = drawableHeight;
	return actualExtent;
}

static CreateSwapchainResult VULKAN_INTERNAL_CreateSwapchain(
	VulkanRenderer *renderer
) {
	VkResult vulkanResult;
	SwapChainSupportDetails swapChainSupportDetails;
	VkSurfaceFormatKHR surfaceFormat;
	VkPresentModeKHR presentMode;
	VkExtent2D extent;
	uint32_t imageCount, swapChainImageCount, i;
	VkSwapchainCreateInfoKHR swapChainCreateInfo;
	VkImage *swapChainImages;
	VkImageViewCreateInfo createInfo;
	VkImageView swapChainImageView;

	if (!VULKAN_INTERNAL_QuerySwapChainSupport(
		renderer,
		renderer->physicalDevice,
		renderer->surface,
		&swapChainSupportDetails
	)) {
		FNA3D_LogError("Device does not support swap chain creation");
		return CREATE_SWAPCHAIN_FAIL;
	}

	renderer->swapchainFormat = VK_FORMAT_R8G8B8A8_UNORM;
	renderer->swapchainSwizzle.r = VK_COMPONENT_SWIZZLE_IDENTITY;
	renderer->swapchainSwizzle.g = VK_COMPONENT_SWIZZLE_IDENTITY;
	renderer->swapchainSwizzle.b = VK_COMPONENT_SWIZZLE_IDENTITY;
	renderer->swapchainSwizzle.a = VK_COMPONENT_SWIZZLE_IDENTITY;
	if (!VULKAN_INTERNAL_ChooseSwapSurfaceFormat(
		renderer->swapchainFormat,
		swapChainSupportDetails.formats,
		swapChainSupportDetails.formatsLength,
		&surfaceFormat
	)) {
		FNA3D_LogWarn("RGBA8 swapchain unsupported, falling back to BGRA8 with swizzle");
		renderer->swapchainFormat = VK_FORMAT_B8G8R8A8_UNORM;
		renderer->swapchainSwizzle.r = VK_COMPONENT_SWIZZLE_B;
		renderer->swapchainSwizzle.g = VK_COMPONENT_SWIZZLE_G;
		renderer->swapchainSwizzle.b = VK_COMPONENT_SWIZZLE_R;
		renderer->swapchainSwizzle.a = VK_COMPONENT_SWIZZLE_A;
		if (!VULKAN_INTERNAL_ChooseSwapSurfaceFormat(
			renderer->swapchainFormat,
			swapChainSupportDetails.formats,
			swapChainSupportDetails.formatsLength,
			&surfaceFormat
		)) {
			SDL_free(swapChainSupportDetails.formats);
			SDL_free(swapChainSupportDetails.presentModes);
			FNA3D_LogError("Device does not support swap chain format");
			return CREATE_SWAPCHAIN_FAIL;
		}
	}

	if (!VULKAN_INTERNAL_ChooseSwapPresentMode(
		renderer->presentInterval,
		swapChainSupportDetails.presentModes,
		swapChainSupportDetails.presentModesLength,
		&presentMode
	)) {
		SDL_free(swapChainSupportDetails.formats);
		SDL_free(swapChainSupportDetails.presentModes);
		FNA3D_LogError("Device does not support swap chain present mode");
		return CREATE_SWAPCHAIN_FAIL;
	}

	extent = ChooseSwapExtent(
		renderer->deviceWindowHandle,
		swapChainSupportDetails.capabilities
	);

	if (extent.width == 0 || extent.height == 0)
	{
		return CREATE_SWAPCHAIN_SURFACE_ZERO;
	}

	imageCount = swapChainSupportDetails.capabilities.minImageCount + 1;

	if (	swapChainSupportDetails.capabilities.maxImageCount > 0 &&
		imageCount > swapChainSupportDetails.capabilities.maxImageCount	)
	{
		imageCount = swapChainSupportDetails.capabilities.maxImageCount;
	}

	if (presentMode == VK_PRESENT_MODE_MAILBOX_KHR)
	{
		/* Required for proper triple-buffering.
		 * Note that this is below the above maxImageCount check!
		 * If the driver advertises MAILBOX but does not support 3 swap
		 * images, it's not real mailbox support, so let it fail hard.
		 * -flibit
		 */
		imageCount = SDL_max(imageCount, 3);
	}

	swapChainCreateInfo.sType = VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR;
	swapChainCreateInfo.pNext = NULL;
	swapChainCreateInfo.flags = 0;
	swapChainCreateInfo.surface = renderer->surface;
	swapChainCreateInfo.minImageCount = imageCount;
	swapChainCreateInfo.imageFormat = surfaceFormat.format;
	swapChainCreateInfo.imageColorSpace = surfaceFormat.colorSpace;
	swapChainCreateInfo.imageExtent = extent;
	swapChainCreateInfo.imageArrayLayers = 1;
	swapChainCreateInfo.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT;
	swapChainCreateInfo.imageSharingMode = VK_SHARING_MODE_EXCLUSIVE;
	swapChainCreateInfo.queueFamilyIndexCount = 0;
	swapChainCreateInfo.pQueueFamilyIndices = NULL;
	swapChainCreateInfo.preTransform = swapChainSupportDetails.capabilities.currentTransform;
	swapChainCreateInfo.compositeAlpha = VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;
	swapChainCreateInfo.presentMode = presentMode;
	swapChainCreateInfo.clipped = VK_TRUE;
	swapChainCreateInfo.oldSwapchain = VK_NULL_HANDLE;

	vulkanResult = renderer->vkCreateSwapchainKHR(
		renderer->logicalDevice,
		&swapChainCreateInfo,
		NULL,
		&renderer->swapChain
	);

	SDL_free(swapChainSupportDetails.formats);
	SDL_free(swapChainSupportDetails.presentModes);

	VULKAN_ERROR_CHECK(vulkanResult, vkCreateSwapchainKHR, CREATE_SWAPCHAIN_FAIL)

	renderer->vkGetSwapchainImagesKHR(
		renderer->logicalDevice,
		renderer->swapChain,
		&swapChainImageCount,
		NULL
	);

	renderer->swapChainImages = (VkImage*) SDL_malloc(
		sizeof(VkImage) * swapChainImageCount
	);
	if (!renderer->swapChainImages)
	{
		SDL_OutOfMemory();
		return CREATE_SWAPCHAIN_FAIL;
	}

	renderer->swapChainImageViews = (VkImageView*) SDL_malloc(
		sizeof(VkImageView) * swapChainImageCount
	);
	if (!renderer->swapChainImageViews)
	{
		SDL_OutOfMemory();
		return CREATE_SWAPCHAIN_FAIL;
	}

	renderer->swapChainResourceAccessTypes = (VulkanResourceAccessType*) SDL_malloc(
		sizeof(VulkanResourceAccessType) * swapChainImageCount
	);
	if (!renderer->swapChainResourceAccessTypes)
	{
		SDL_OutOfMemory();
		return CREATE_SWAPCHAIN_FAIL;
	}

	swapChainImages = SDL_stack_alloc(VkImage, swapChainImageCount);
	renderer->vkGetSwapchainImagesKHR(
		renderer->logicalDevice,
		renderer->swapChain,
		&swapChainImageCount,
		swapChainImages
	);
	renderer->swapChainImageCount = swapChainImageCount;
	renderer->swapChainExtent = extent;

	createInfo.sType = VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
	createInfo.pNext = NULL;
	createInfo.flags = 0;
	createInfo.viewType = VK_IMAGE_VIEW_TYPE_2D;
	createInfo.format = surfaceFormat.format;
	createInfo.components = renderer->swapchainSwizzle;
	createInfo.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
	createInfo.subresourceRange.baseMipLevel = 0;
	createInfo.subresourceRange.levelCount = 1;
	createInfo.subresourceRange.baseArrayLayer = 0;
	createInfo.subresourceRange.layerCount = 1;
	for (i = 0; i < swapChainImageCount; i += 1)
	{
		createInfo.image = swapChainImages[i];

		vulkanResult = renderer->vkCreateImageView(
			renderer->logicalDevice,
			&createInfo,
			NULL,
			&swapChainImageView
		);

		if (vulkanResult != VK_SUCCESS)
		{
			FNA3D_LogError("vkCreateImageView: %s", VkErrorMessages(vulkanResult));
			SDL_stack_free(swapChainImages);
			return CREATE_SWAPCHAIN_FAIL;
		}

		renderer->swapChainImages[i] = swapChainImages[i];
		renderer->swapChainImageViews[i] = swapChainImageView;
		renderer->swapChainResourceAccessTypes[i] = RESOURCE_ACCESS_NONE;
	}

	SDL_stack_free(swapChainImages);
	return CREATE_SWAPCHAIN_SUCCESS;
}

static void VULKAN_INTERNAL_DestroySwapchain(VulkanRenderer *renderer)
{
	uint32_t i;

	for (i = 0; i < renderer->framebufferArray.count; i += 1)
	{
		renderer->vkDestroyFramebuffer(
			renderer->logicalDevice,
			renderer->framebufferArray.elements[i].value,
			NULL
		);
	}
	SDL_free(renderer->framebufferArray.elements);
	renderer->framebufferArray.elements = NULL;
	renderer->framebufferArray.count = 0;
	renderer->framebufferArray.capacity = 0;

	for (i = 0; i < renderer->swapChainImageCount; i += 1)
	{
		renderer->vkDestroyImageView(
			renderer->logicalDevice,
			renderer->swapChainImageViews[i],
			NULL
		);
	}

	SDL_free(renderer->swapChainImages);
	renderer->swapChainImages = NULL;
	SDL_free(renderer->swapChainImageViews);
	renderer->swapChainImageViews = NULL;
	SDL_free(renderer->swapChainResourceAccessTypes);
	renderer->swapChainResourceAccessTypes = NULL;

	renderer->vkDestroySwapchainKHR(
		renderer->logicalDevice,
		renderer->swapChain,
		NULL
	);
}

static void VULKAN_INTERNAL_RecreateSwapchain(VulkanRenderer *renderer, uint8_t flush)
{
	CreateSwapchainResult createSwapchainResult;
	SwapChainSupportDetails swapChainSupportDetails;
	VkExtent2D extent;

	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);
	if (flush)
	{
		VULKAN_INTERNAL_FlushCommands(renderer, 1);
	}

	renderer->vkDeviceWaitIdle(renderer->logicalDevice);

	VULKAN_INTERNAL_QuerySwapChainSupport(
		renderer,
		renderer->physicalDevice,
		renderer->surface,
		&swapChainSupportDetails
	);

	extent = ChooseSwapExtent(
		renderer->deviceWindowHandle,
		swapChainSupportDetails.capabilities
	);

	SDL_free(swapChainSupportDetails.formats);
	SDL_free(swapChainSupportDetails.presentModes);

	if (extent.width == 0 || extent.height == 0)
	{
		return;
	}

	VULKAN_INTERNAL_DestroySwapchain(renderer);
	createSwapchainResult = VULKAN_INTERNAL_CreateSwapchain(renderer);

	if (createSwapchainResult == CREATE_SWAPCHAIN_FAIL)
	{
		return;
	}

	renderer->vkDeviceWaitIdle(renderer->logicalDevice);
}

/* Vulkan: Buffer Objects */

static void VULKAN_INTERNAL_RemoveBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanBuffer *vulkanBuffer = (VulkanBuffer*) buffer;

	SDL_LockMutex(renderer->disposeLock);

	/* Queue buffer for destruction */
	if (renderer->buffersToDestroyCount + 1 >= renderer->buffersToDestroyCapacity)
	{
		renderer->buffersToDestroyCapacity *= 2;

		renderer->buffersToDestroy = SDL_realloc(
			renderer->buffersToDestroy,
			sizeof(VulkanBuffer*) * renderer->buffersToDestroyCapacity
		);
	}

	renderer->buffersToDestroy[
		renderer->buffersToDestroyCount
	] = vulkanBuffer;
	renderer->buffersToDestroyCount += 1;

	SDL_UnlockMutex(renderer->disposeLock);
}

static void VULKAN_INTERNAL_MarkAsBound(
	VulkanRenderer *renderer,
	VulkanBuffer *buf
) {
	VulkanSubBuffer *subbuf = buf->subBuffers[buf->currentSubBufferIndex];
	subbuf->bound = renderer->submitCounter;

	/* Don't rebind a bound buffer */
	if (buf->bound) return;

	buf->bound = 1;

	if (renderer->numBuffersInUse == renderer->maxBuffersInUse)
	{
		renderer->maxBuffersInUse *= 2;
		renderer->buffersInUse = SDL_realloc(
			renderer->buffersInUse,
			sizeof(VulkanBuffer*) * renderer->maxBuffersInUse
		);
	}

	renderer->buffersInUse[renderer->numBuffersInUse] = buf;
	renderer->numBuffersInUse += 1;
}

static void VULKAN_INTERNAL_SetBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanBuffer *vulkanBuffer = (VulkanBuffer*) buffer;
	uint32_t prevIndex;
	uint8_t allocateResult;
	uint32_t i;

	#define CURIDX vulkanBuffer->currentSubBufferIndex
	#define SUBBUF vulkanBuffer->subBuffers[CURIDX]

	prevIndex = CURIDX;

	if (options != FNA3D_SETDATAOPTIONS_NOOVERWRITE)
	{
		/* If buffer has not been bound this frame, set the first unbound index */
		if (!vulkanBuffer->bound)
		{
			for (i = 0; i < vulkanBuffer->subBufferCount; i += 1)
			{
				if (vulkanBuffer->subBuffers[i]->bound == -1)
				{
					break;
				}
			}
			CURIDX = i;
		}
	}

	/*
	 * If buffer was bound and options is NONE or DISCARD
	 * find the next available unbound sub-buffer
	 */
	if (vulkanBuffer->bound)
	{
		if (options == FNA3D_SETDATAOPTIONS_NONE || options == FNA3D_SETDATAOPTIONS_DISCARD)
		{
			while (CURIDX < vulkanBuffer->subBufferCount && SUBBUF->bound != -1)
			{
				CURIDX += 1;
			}
		}
	}

	/* Create a new SubBuffer if needed */
	if (CURIDX == vulkanBuffer->subBufferCount)
	{
		allocateResult = VULKAN_INTERNAL_AllocateSubBuffer(renderer, vulkanBuffer);
		if (allocateResult == 2)
		{
			/* Out of memory, flush commands to free memory */
			VULKAN_INTERNAL_FlushCommands(renderer, 1);
		}
		else if (allocateResult == 0)
		{
			/* Something went very wrong, time to die */
			FNA3D_LogError("Failed to allocate VulkanSubBuffer!");
			return;
		}
	}

	if (renderer->bufferDefragInProgress)
	{
		renderer->vkWaitForFences(
			renderer->logicalDevice,
			1,
			&renderer->defragFence,
			VK_TRUE,
			UINT64_MAX
		);

		renderer->bufferDefragInProgress = 0;
	}

	/* If options is NONE and buffer was bound, copy the previous data into the new buffer */
	if (options == FNA3D_SETDATAOPTIONS_NONE && prevIndex != CURIDX)
	{
		SDL_memcpy(
			SUBBUF->usedRegion->allocation->mapPointer + SUBBUF->usedRegion->resourceOffset,
			vulkanBuffer->subBuffers[prevIndex]->usedRegion->allocation->mapPointer + vulkanBuffer->subBuffers[prevIndex]->usedRegion->resourceOffset,
			vulkanBuffer->size
		);
	}

	SDL_memcpy(
		SUBBUF->usedRegion->allocation->mapPointer + SUBBUF->usedRegion->resourceOffset + offsetInBytes,
		data,
		dataLength
	);

	#undef SUBBUF
	#undef CURIDX
}

static uint8_t VULKAN_INTERNAL_CreateTexture(
	VulkanRenderer *renderer,
	uint32_t width,
	uint32_t height,
	uint32_t depth,
	uint8_t isCube,
	uint8_t isRenderTarget,
	VkSampleCountFlagBits samples,
	uint32_t levelCount,
	VkFormat format,
	VkComponentMapping swizzle,
	VkImageAspectFlags aspectMask,
	VkImageType imageType,
	VkImageUsageFlags usage,
	VulkanTexture *texture
) {
	VkResult result;
	uint8_t bindResult = 0;
	VkImageCreateInfo imageCreateInfo;
	VkImageViewCreateInfo imageViewCreateInfo;
	uint8_t layerCount = isCube ? 6 : 1;
	uint32_t i;

	imageCreateInfo.sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO;
	imageCreateInfo.pNext = NULL;
	imageCreateInfo.flags = isCube ? VK_IMAGE_CREATE_CUBE_COMPATIBLE_BIT : 0;
	imageCreateInfo.imageType = imageType;
	imageCreateInfo.format = format;
	imageCreateInfo.extent.width = width;
	imageCreateInfo.extent.height = height;
	imageCreateInfo.extent.depth = depth;
	imageCreateInfo.mipLevels = levelCount;
	imageCreateInfo.arrayLayers = layerCount;
	imageCreateInfo.samples = samples;
	imageCreateInfo.tiling = VK_IMAGE_TILING_OPTIMAL;
	imageCreateInfo.usage = usage;
	imageCreateInfo.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
	imageCreateInfo.queueFamilyIndexCount = 0;
	imageCreateInfo.pQueueFamilyIndices = NULL;
	imageCreateInfo.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;

	result = renderer->vkCreateImage(
		renderer->logicalDevice,
		&imageCreateInfo,
		NULL,
		&texture->image
	);
	VULKAN_ERROR_CHECK(result, vkCreateImage, 0)

	texture->isRenderTarget = isRenderTarget;
	texture->imageCreateInfo = imageCreateInfo;

	bindResult = VULKAN_INTERNAL_BindMemoryForImage(
		renderer,
		texture->image,
		isRenderTarget,
		&texture->usedRegion
	);

	/* Binding failed, bail out! */
	if (bindResult != 1)
	{
		renderer->vkDestroyImage(
			renderer->logicalDevice,
			texture->image,
			NULL
		);

		return bindResult;
	}

	texture->usedRegion->vulkanTexture = texture; /* lol */

	imageViewCreateInfo.sType = VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
	imageViewCreateInfo.pNext = NULL;
	imageViewCreateInfo.flags = 0;
	imageViewCreateInfo.image = texture->image;
	imageViewCreateInfo.format = format;
	imageViewCreateInfo.components = swizzle;
	imageViewCreateInfo.subresourceRange.aspectMask = aspectMask;
	imageViewCreateInfo.subresourceRange.baseMipLevel = 0;
	imageViewCreateInfo.subresourceRange.levelCount = levelCount;
	imageViewCreateInfo.subresourceRange.baseArrayLayer = 0;
	imageViewCreateInfo.subresourceRange.layerCount = layerCount;

	if (isCube)
	{
		imageViewCreateInfo.viewType = VK_IMAGE_VIEW_TYPE_CUBE;
	}
	else if (imageType == VK_IMAGE_TYPE_2D)
	{
		imageViewCreateInfo.viewType = VK_IMAGE_VIEW_TYPE_2D;
	}
	else if (imageType == VK_IMAGE_TYPE_3D)
	{
		imageViewCreateInfo.viewType = VK_IMAGE_VIEW_TYPE_3D;
	}
	else
	{
		FNA3D_LogError("Invalid image type: %u", imageType);
	}

	result = renderer->vkCreateImageView(
		renderer->logicalDevice,
		&imageViewCreateInfo,
		NULL,
		&texture->view
	);
	VULKAN_ERROR_CHECK(result, vkCreateImageView, 0)

	texture->viewCreateInfo = imageViewCreateInfo;

	/* FIXME: Reduce this memset to the minimum necessary for init! */
	SDL_memset(texture->rtViews, '\0', sizeof(texture->rtViews));
	if (isRenderTarget)
	{
		if (!isCube && levelCount == 1)
		{
			/* Framebuffer views don't like swizzling */
			imageViewCreateInfo.components.r = VK_COMPONENT_SWIZZLE_IDENTITY;
			imageViewCreateInfo.components.g = VK_COMPONENT_SWIZZLE_IDENTITY;
			imageViewCreateInfo.components.b = VK_COMPONENT_SWIZZLE_IDENTITY;
			imageViewCreateInfo.components.a = VK_COMPONENT_SWIZZLE_IDENTITY;

			result = renderer->vkCreateImageView(
				renderer->logicalDevice,
				&imageViewCreateInfo,
				NULL,
				&texture->rtViews[0]
			);
			VULKAN_ERROR_CHECK(result, vkCreateImageView, 0)
		}
		else
		{
			/* Create a framebuffer-compatible view for each layer */
			for (i = 0; i < layerCount; i += 1)
			{
				imageViewCreateInfo.viewType = VK_IMAGE_VIEW_TYPE_2D;
				imageViewCreateInfo.subresourceRange.levelCount = 1;
				imageViewCreateInfo.subresourceRange.layerCount = 1;
				imageViewCreateInfo.subresourceRange.baseArrayLayer = i;
				imageViewCreateInfo.components.r = VK_COMPONENT_SWIZZLE_IDENTITY;
				imageViewCreateInfo.components.g = VK_COMPONENT_SWIZZLE_IDENTITY;
				imageViewCreateInfo.components.b = VK_COMPONENT_SWIZZLE_IDENTITY;
				imageViewCreateInfo.components.a = VK_COMPONENT_SWIZZLE_IDENTITY;

				result = renderer->vkCreateImageView(
					renderer->logicalDevice,
					&imageViewCreateInfo,
					NULL,
					&texture->rtViews[i]
				);
				VULKAN_ERROR_CHECK(result, vkCreateImageView, 0)
			}
		}
	}

	texture->dimensions.width = width;
	texture->dimensions.height = height;
	texture->depth = depth;
	texture->surfaceFormat = format;
	texture->levelCount = levelCount;
	texture->layerCount = layerCount;
	texture->resourceAccessType = RESOURCE_ACCESS_NONE;
	texture->external = 0;

	return 1;
}

static void VULKAN_INTERNAL_GetTextureData(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	int32_t level,
	int32_t layer,
	void* data,
	int32_t dataLength
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *vulkanTexture = (VulkanTexture*) texture;
	VulkanSubBuffer *stagingSubBuffer;
	VulkanResourceAccessType prevResourceAccess;
	VkBufferImageCopy imageCopy;
	VkDeviceSize offset;
	uint8_t *dataPtr = (uint8_t*) data;
	uint8_t *stagingBufferPointer;

	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);

	SDL_LockMutex(renderer->passLock);
	SDL_LockMutex(renderer->stagingLock);

	VULKAN_INTERNAL_PrepareCopyFromStagingBuffer(
		renderer,
		dataLength,
		&stagingSubBuffer,
		&offset,
		(void**) &stagingBufferPointer
	);

	/* Cache this so we can restore it later */
	prevResourceAccess = vulkanTexture->resourceAccessType;

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_READ,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		vulkanTexture->layerCount,
		0,
		vulkanTexture->levelCount,
		0,
		vulkanTexture->image,
		&vulkanTexture->resourceAccessType
	);

	/* Save texture data to staging buffer */

	imageCopy.imageExtent.width = w;
	imageCopy.imageExtent.height = h;
	imageCopy.imageExtent.depth = 1;
	imageCopy.imageOffset.x = x;
	imageCopy.imageOffset.y = y;
	imageCopy.imageOffset.z = 0;
	imageCopy.imageSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
	imageCopy.imageSubresource.baseArrayLayer = layer;
	imageCopy.imageSubresource.layerCount = 1;
	imageCopy.imageSubresource.mipLevel = level;
	imageCopy.bufferOffset = offset;
	imageCopy.bufferRowLength = w;
	imageCopy.bufferImageHeight = h;

	RECORD_CMD(renderer->vkCmdCopyImageToBuffer(
		renderer->currentCommandBuffer,
		vulkanTexture->image,
		AccessMap[vulkanTexture->resourceAccessType].imageLayout,
		stagingSubBuffer->buffer,
		1,
		&imageCopy
	));

	/* Restore the image layout and wait for completion of the render pass */

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		prevResourceAccess,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		vulkanTexture->layerCount,
		0,
		vulkanTexture->levelCount,
		0,
		vulkanTexture->image,
		&vulkanTexture->resourceAccessType
	);

	VULKAN_INTERNAL_FlushCommands(renderer, 1);

	/* Read from staging buffer */

	SDL_memcpy(
		dataPtr,
		stagingBufferPointer,
		BytesPerImage(w, h, vulkanTexture->colorFormat)
	);

	SDL_UnlockMutex(renderer->stagingLock);
	SDL_UnlockMutex(renderer->passLock);
}

/* Vulkan: Mutable State Commands */

static void VULKAN_INTERNAL_SetViewportCommand(VulkanRenderer *renderer)
{
	VkViewport vulkanViewport;

	/* Flipping the viewport for compatibility with D3D */
	vulkanViewport.x = (float) renderer->viewport.x;
	vulkanViewport.width = (float) renderer->viewport.w;
	vulkanViewport.minDepth = renderer->viewport.minDepth;
	vulkanViewport.maxDepth = renderer->viewport.maxDepth;
#ifdef __APPLE__
	/* For MoltenVK we just disable viewport flipping, bypassing the Vulkan spec */
	vulkanViewport.y = (float) renderer->viewport.y;
	vulkanViewport.height = (float) renderer->viewport.h;
#else
	/* For everyone else, we have KHR_maintenance1 to do the flipping for us */
	vulkanViewport.y = (float) (renderer->viewport.y + renderer->viewport.h);
	vulkanViewport.height = (float) -renderer->viewport.h;
#endif

	RECORD_CMD(renderer->vkCmdSetViewport(
		renderer->currentCommandBuffer,
		0,
		1,
		&vulkanViewport
	));
}

static void VULKAN_INTERNAL_SetScissorRectCommand(VulkanRenderer *renderer)
{
	VkOffset2D offset;
	VkExtent2D extent;
	VkRect2D vulkanScissorRect;

	if (renderer->renderPassInProgress)
	{
		if (!renderer->rasterizerState.scissorTestEnable)
		{
			offset.x = 0;
			offset.y = 0;
			extent = renderer->colorAttachments[0]->dimensions;
		}
		else
		{
			offset.x = renderer->scissorRect.x;
			offset.y = renderer->scissorRect.y;
			extent.width = renderer->scissorRect.w;
			extent.height = renderer->scissorRect.h;
		}

		vulkanScissorRect.offset = offset;
		vulkanScissorRect.extent = extent;

		RECORD_CMD(renderer->vkCmdSetScissor(
			renderer->currentCommandBuffer,
			0,
			1,
			&vulkanScissorRect
		));
	}
}

static void VULKAN_INTERNAL_SetStencilReferenceValueCommand(
	VulkanRenderer *renderer
) {
	if (renderer->renderPassInProgress)
	{
		RECORD_CMD(renderer->vkCmdSetStencilReference(
			renderer->currentCommandBuffer,
			VK_STENCIL_FRONT_AND_BACK,
			renderer->stencilRef
		));
	}
}

static void VULKAN_INTERNAL_SetDepthBiasCommand(VulkanRenderer *renderer)
{
	if (renderer->renderPassInProgress)
	{
		RECORD_CMD(renderer->vkCmdSetDepthBias(
			renderer->currentCommandBuffer,
			renderer->rasterizerState.depthBias,
			0.0f, /* no clamp */
			renderer->rasterizerState.slopeScaleDepthBias
		));
	}
}

/* Vulkan: Pipeline State Objects */

static VkPipelineLayout VULKAN_INTERNAL_FetchPipelineLayout(
	VulkanRenderer *renderer,
	MOJOSHADER_vkShader *vertShader,
	MOJOSHADER_vkShader *fragShader
) {
	VkDescriptorSetLayout setLayouts[4];

	PipelineLayoutHash pipelineLayoutHash;
	VkPipelineLayoutCreateInfo layoutCreateInfo;
	VkPipelineLayout layout;
	VkResult vulkanResult;

	pipelineLayoutHash.vertexSamplerLayout = VULKAN_INTERNAL_FetchSamplerDescriptorSetLayout(renderer, vertShader, VK_SHADER_STAGE_VERTEX_BIT);
	pipelineLayoutHash.fragSamplerLayout = VULKAN_INTERNAL_FetchSamplerDescriptorSetLayout(renderer, fragShader, VK_SHADER_STAGE_FRAGMENT_BIT);
	pipelineLayoutHash.vertexUniformLayout = renderer->vertexUniformBufferDescriptorSetLayout;
	pipelineLayoutHash.fragUniformLayout = renderer->fragUniformBufferDescriptorSetLayout;

	layout = PipelineLayoutHashArray_Fetch(
		&renderer->pipelineLayoutTable,
		pipelineLayoutHash
	);

	if (layout != VK_NULL_HANDLE)
	{
		return layout;
	}

	setLayouts[0] = pipelineLayoutHash.vertexSamplerLayout;
	setLayouts[1] = pipelineLayoutHash.fragSamplerLayout;
	setLayouts[2] = renderer->vertexUniformBufferDescriptorSetLayout;
	setLayouts[3] = renderer->fragUniformBufferDescriptorSetLayout;

	layoutCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO;
	layoutCreateInfo.pNext = NULL;
	layoutCreateInfo.flags = 0;
	layoutCreateInfo.setLayoutCount = 4;
	layoutCreateInfo.pSetLayouts = setLayouts;
	layoutCreateInfo.pushConstantRangeCount = 0;
	layoutCreateInfo.pPushConstantRanges = NULL;

	vulkanResult = renderer->vkCreatePipelineLayout(
		renderer->logicalDevice,
		&layoutCreateInfo,
		NULL,
		&layout
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreatePipelineLayout, VK_NULL_HANDLE)

	PipelineLayoutHashArray_Insert(
		&renderer->pipelineLayoutTable,
		pipelineLayoutHash,
		layout
	);
	return layout;
}

static void VULKAN_INTERNAL_GenerateVertexInputInfo(
	VulkanRenderer *renderer,
	VkVertexInputBindingDescription *bindingDescriptions,
	VkVertexInputAttributeDescription* attributeDescriptions,
	uint32_t *attributeDescriptionCount,
	VkVertexInputBindingDivisorDescriptionEXT *divisorDescriptions,
	uint32_t *divisorDescriptionCount
) {
	MOJOSHADER_vkShader *vertexShader, *blah;
	uint8_t attrUse[MOJOSHADER_USAGE_TOTAL][16];
	uint32_t attributeDescriptionCounter = 0;
	uint32_t divisorDescriptionCounter = 0;
	int32_t i, j, k;
	FNA3D_VertexDeclaration vertexDeclaration;
	FNA3D_VertexElement element;
	FNA3D_VertexElementUsage usage;
	int32_t index, attribLoc;
	VkVertexInputAttributeDescription vInputAttribDescription;
	VkVertexInputBindingDescription vertexInputBindingDescription;
	VkVertexInputBindingDivisorDescriptionEXT divisorDescription;

	MOJOSHADER_vkGetBoundShaders(&vertexShader, &blah);

	SDL_memset(attrUse, '\0', sizeof(attrUse));
	for (i = 0; i < (int32_t) renderer->numVertexBindings; i += 1)
	{
		vertexDeclaration =
			renderer->vertexBindings[i].vertexDeclaration;

		for (j = 0; j < vertexDeclaration.elementCount; j += 1)
		{
			element = vertexDeclaration.elements[j];
			usage = element.vertexElementUsage;
			index = element.usageIndex;

			if (attrUse[usage][index])
			{
				index = -1;

				for (k = 0; k < MAX_VERTEX_ATTRIBUTES; k += 1)
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

			attribLoc = MOJOSHADER_vkGetVertexAttribLocation(
				vertexShader,
				VertexAttribUsage(usage),
				index
			);

			if (attribLoc == -1)
			{
				/* Stream not in use! */
				continue;
			}

			vInputAttribDescription.location = attribLoc;
			vInputAttribDescription.format = XNAToVK_VertexAttribType[
				element.vertexElementFormat
			];
			vInputAttribDescription.offset = element.offset;
			vInputAttribDescription.binding = i;

			attributeDescriptions[attributeDescriptionCounter] =
				vInputAttribDescription;
			attributeDescriptionCounter += 1;
		}

		vertexInputBindingDescription.binding = i;
		vertexInputBindingDescription.stride =
			vertexDeclaration.vertexStride;

		if (renderer->vertexBindings[i].instanceFrequency > 0)
		{
			vertexInputBindingDescription.inputRate =
				VK_VERTEX_INPUT_RATE_INSTANCE;

			divisorDescription.binding = i;
			divisorDescription.divisor =
				renderer->vertexBindings[i].instanceFrequency;

			divisorDescriptions[divisorDescriptionCounter] =
				divisorDescription;
			divisorDescriptionCounter += 1;
		}
		else
		{
			vertexInputBindingDescription.inputRate =
				VK_VERTEX_INPUT_RATE_VERTEX;
		}

		bindingDescriptions[i] = vertexInputBindingDescription;
	}

	*attributeDescriptionCount = attributeDescriptionCounter;
	*divisorDescriptionCount = divisorDescriptionCounter;
}

static VkPipeline VULKAN_INTERNAL_FetchPipeline(VulkanRenderer *renderer)
{
	VkResult vulkanResult;
	VkPipeline pipeline;
	VkPipelineViewportStateCreateInfo viewportStateInfo;
	VkPipelineInputAssemblyStateCreateInfo inputAssemblyInfo;
	VkVertexInputBindingDescription *bindingDescriptions;
	VkVertexInputAttributeDescription *attributeDescriptions;
	uint32_t attributeDescriptionCount;
	VkVertexInputBindingDivisorDescriptionEXT *divisorDescriptions;
	uint32_t divisorDescriptionCount;
	VkPipelineVertexInputStateCreateInfo vertexInputInfo;
	VkPipelineVertexInputDivisorStateCreateInfoEXT divisorStateInfo;
	VkPipelineRasterizationStateCreateInfo rasterizerInfo;
	VkPipelineMultisampleStateCreateInfo multisamplingInfo;
	VkPipelineColorBlendAttachmentState colorBlendAttachments[MAX_RENDERTARGET_BINDINGS];
	VkPipelineColorBlendStateCreateInfo colorBlendStateInfo;
	VkStencilOpState frontStencilState, backStencilState;
	VkPipelineDepthStencilStateCreateInfo depthStencilStateInfo;
	static const VkDynamicState dynamicStates[] =
	{
		VK_DYNAMIC_STATE_VIEWPORT,
		VK_DYNAMIC_STATE_SCISSOR,
		VK_DYNAMIC_STATE_BLEND_CONSTANTS,
		VK_DYNAMIC_STATE_STENCIL_REFERENCE,
		VK_DYNAMIC_STATE_DEPTH_BIAS
	};
	VkPipelineDynamicStateCreateInfo dynamicStateInfo;
	MOJOSHADER_vkShader *vertShader, *fragShader;
	VkPipelineShaderStageCreateInfo stageInfos[2];
	VkGraphicsPipelineCreateInfo pipelineCreateInfo;

	PipelineHash hash;
	hash.blendState = GetPackedBlendState(renderer->blendState);
	hash.rasterizerState = GetPackedRasterizerState(
		renderer->rasterizerState,
		renderer->rasterizerState.depthBias * XNAToVK_DepthBiasScale(
			XNAToVK_DepthFormat(
				renderer,
				renderer->currentDepthFormat
			)
		)
	);
	hash.depthStencilState = GetPackedDepthStencilState(
		renderer->depthStencilState
	);
	hash.vertexBufferBindingsIndex = renderer->currentVertexBufferBindingsIndex;
	hash.primitiveType = renderer->currentPrimitiveType;
	hash.sampleMask = renderer->multiSampleMask[0];
	MOJOSHADER_vkGetBoundShaders(&vertShader, &fragShader);
	hash.vertShader = vertShader;
	hash.fragShader = fragShader;
	hash.renderPass = renderer->renderPass;

	renderer->currentPipelineLayout = VULKAN_INTERNAL_FetchPipelineLayout(
		renderer,
		vertShader,
		fragShader
	);

	pipeline = PipelineHashTable_Fetch(&renderer->pipelineHashTable, hash);
	if (pipeline != VK_NULL_HANDLE)
	{
		return pipeline;
	}

	/* Viewport / Scissor */

	/* NOTE: because viewport and scissor are dynamic,
	 * values must be set using the command buffer
	 */
	viewportStateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO;
	viewportStateInfo.pNext = NULL;
	viewportStateInfo.flags = 0;
	viewportStateInfo.viewportCount = 1;
	viewportStateInfo.pViewports = NULL;
	viewportStateInfo.scissorCount = 1;
	viewportStateInfo.pScissors = NULL;

	/* Input Assembly */

	inputAssemblyInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO;
	inputAssemblyInfo.pNext = NULL;
	inputAssemblyInfo.flags = 0;
	inputAssemblyInfo.topology = XNAToVK_Topology[
		renderer->currentPrimitiveType
	];
	inputAssemblyInfo.primitiveRestartEnable = VK_FALSE;

	/* Vertex Input */

	bindingDescriptions = (VkVertexInputBindingDescription*) SDL_malloc(
		renderer->numVertexBindings *
		sizeof(VkVertexInputBindingDescription)
	);
	attributeDescriptions = (VkVertexInputAttributeDescription*) SDL_malloc(
		renderer->numVertexBindings *
		MAX_VERTEX_ATTRIBUTES *
		sizeof(VkVertexInputAttributeDescription)
	);
	divisorDescriptions = (VkVertexInputBindingDivisorDescriptionEXT*) SDL_malloc(
		renderer->numVertexBindings *
		sizeof(VkVertexInputBindingDivisorDescriptionEXT)
	);

	VULKAN_INTERNAL_GenerateVertexInputInfo(
		renderer,
		bindingDescriptions,
		attributeDescriptions,
		&attributeDescriptionCount,
		divisorDescriptions,
		&divisorDescriptionCount
	);

	vertexInputInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO;
	if (divisorDescriptionCount > 0)
	{
		divisorStateInfo.sType =
			VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_DIVISOR_STATE_CREATE_INFO_EXT;
		divisorStateInfo.pNext = NULL;
		divisorStateInfo.vertexBindingDivisorCount =
			divisorDescriptionCount;
		divisorStateInfo.pVertexBindingDivisors = divisorDescriptions;
		vertexInputInfo.pNext = &divisorStateInfo;
	}
	else
	{
		vertexInputInfo.pNext = NULL;
	}

	vertexInputInfo.flags = 0;
	vertexInputInfo.vertexBindingDescriptionCount =
		renderer->numVertexBindings;
	vertexInputInfo.pVertexBindingDescriptions = bindingDescriptions;
	vertexInputInfo.vertexAttributeDescriptionCount =
		attributeDescriptionCount;
	vertexInputInfo.pVertexAttributeDescriptions = attributeDescriptions;

	/* Rasterizer */

	rasterizerInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO;
	rasterizerInfo.pNext = NULL;
	rasterizerInfo.flags = 0;
	rasterizerInfo.depthClampEnable = VK_FALSE;
	rasterizerInfo.rasterizerDiscardEnable = VK_FALSE;
	rasterizerInfo.polygonMode = XNAToVK_PolygonMode[
		renderer->rasterizerState.fillMode
	];
	rasterizerInfo.cullMode = XNAToVK_CullMode[
		renderer->rasterizerState.cullMode
	];
	/* This is reversed because we are flipping the viewport -cosmonaut */
	rasterizerInfo.frontFace = VK_FRONT_FACE_CLOCKWISE;
	rasterizerInfo.depthBiasEnable = VK_TRUE;
	rasterizerInfo.depthBiasConstantFactor = 0.0f;
	rasterizerInfo.depthBiasClamp = 0.0f;
	rasterizerInfo.depthBiasSlopeFactor = 0.0f;
	rasterizerInfo.lineWidth = 1.0f;

	/* Multisample */

	multisamplingInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO;
	multisamplingInfo.pNext = NULL;
	multisamplingInfo.flags = 0;
	multisamplingInfo.rasterizationSamples = XNAToVK_SampleCount(
		renderer->multiSampleCount
	);
	multisamplingInfo.sampleShadingEnable = VK_FALSE;
	multisamplingInfo.minSampleShading = 1.0f;
	multisamplingInfo.pSampleMask = renderer->multiSampleMask;
	multisamplingInfo.alphaToCoverageEnable = VK_FALSE;
	multisamplingInfo.alphaToOneEnable = VK_FALSE;

	/* Blend */

	colorBlendAttachments[0].blendEnable = !(
		renderer->blendState.colorSourceBlend == FNA3D_BLEND_ONE &&
		renderer->blendState.colorDestinationBlend == FNA3D_BLEND_ZERO &&
		renderer->blendState.alphaSourceBlend == FNA3D_BLEND_ONE &&
		renderer->blendState.alphaDestinationBlend == FNA3D_BLEND_ZERO
	);
	if (colorBlendAttachments[0].blendEnable)
	{
		colorBlendAttachments[0].srcColorBlendFactor = XNAToVK_BlendFactor[
			renderer->blendState.colorSourceBlend
		];
		colorBlendAttachments[0].srcAlphaBlendFactor = XNAToVK_BlendFactor[
			renderer->blendState.alphaSourceBlend
		];
		colorBlendAttachments[0].dstColorBlendFactor = XNAToVK_BlendFactor[
			renderer->blendState.colorDestinationBlend
		];
		colorBlendAttachments[0].dstAlphaBlendFactor = XNAToVK_BlendFactor[
			renderer->blendState.alphaDestinationBlend
		];

		colorBlendAttachments[0].colorBlendOp = XNAToVK_BlendOp[
			renderer->blendState.colorBlendFunction
		];
		colorBlendAttachments[0].alphaBlendOp = XNAToVK_BlendOp[
			renderer->blendState.alphaBlendFunction
		];
	}
	else
	{
		colorBlendAttachments[0].srcColorBlendFactor = VK_BLEND_FACTOR_ONE;
		colorBlendAttachments[0].srcAlphaBlendFactor = VK_BLEND_FACTOR_ONE;
		colorBlendAttachments[0].dstColorBlendFactor = VK_BLEND_FACTOR_ZERO;
		colorBlendAttachments[0].dstAlphaBlendFactor = VK_BLEND_FACTOR_ZERO;
		colorBlendAttachments[0].colorBlendOp = VK_BLEND_OP_ADD;
		colorBlendAttachments[0].alphaBlendOp = VK_BLEND_OP_ADD;
	}
	colorBlendAttachments[1] = colorBlendAttachments[0];
	colorBlendAttachments[2] = colorBlendAttachments[0];
	colorBlendAttachments[3] = colorBlendAttachments[0];

	colorBlendAttachments[0].colorWriteMask =
		renderer->blendState.colorWriteEnable;
	colorBlendAttachments[1].colorWriteMask =
		renderer->blendState.colorWriteEnable1;
	colorBlendAttachments[2].colorWriteMask =
		renderer->blendState.colorWriteEnable2;
	colorBlendAttachments[3].colorWriteMask =
		renderer->blendState.colorWriteEnable3;

	colorBlendStateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO;
	colorBlendStateInfo.pNext = NULL;
	colorBlendStateInfo.flags = 0;
	colorBlendStateInfo.logicOpEnable = VK_FALSE;
	colorBlendStateInfo.logicOp = VK_LOGIC_OP_COPY;
	colorBlendStateInfo.attachmentCount = renderer->colorAttachmentCount;
	colorBlendStateInfo.pAttachments = colorBlendAttachments;
	colorBlendStateInfo.blendConstants[0] = 0.0f;
	colorBlendStateInfo.blendConstants[1] = 0.0f;
	colorBlendStateInfo.blendConstants[2] = 0.0f;
	colorBlendStateInfo.blendConstants[3] = 0.0f;

	/* Stencil */

	frontStencilState.failOp = XNAToVK_StencilOp[
		renderer->depthStencilState.stencilFail
	];
	frontStencilState.passOp = XNAToVK_StencilOp[
		renderer->depthStencilState.stencilPass
	];
	frontStencilState.depthFailOp = XNAToVK_StencilOp[
		renderer->depthStencilState.stencilDepthBufferFail
	];
	frontStencilState.compareOp = XNAToVK_CompareOp[
		renderer->depthStencilState.stencilFunction
	];
	frontStencilState.compareMask = renderer->depthStencilState.stencilMask;
	frontStencilState.writeMask = renderer->depthStencilState.stencilWriteMask;
	frontStencilState.reference = renderer->depthStencilState.referenceStencil;

	if (renderer->depthStencilState.twoSidedStencilMode)
	{
		backStencilState.failOp = XNAToVK_StencilOp[
			renderer->depthStencilState.ccwStencilFail
		];
		backStencilState.passOp = XNAToVK_StencilOp[
			renderer->depthStencilState.ccwStencilPass
		];
		backStencilState.depthFailOp = XNAToVK_StencilOp[
			renderer->depthStencilState.ccwStencilDepthBufferFail
		];
		backStencilState.compareOp = XNAToVK_CompareOp[
			renderer->depthStencilState.ccwStencilFunction
		];
		backStencilState.compareMask = renderer->depthStencilState.stencilMask;
		backStencilState.writeMask = renderer->depthStencilState.stencilWriteMask;
		backStencilState.reference = renderer->depthStencilState.referenceStencil;
	}
	else
	{
		backStencilState = frontStencilState;
	}

	/* Depth Stencil */

	depthStencilStateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO;
	depthStencilStateInfo.pNext = NULL;
	depthStencilStateInfo.flags = 0; /* Unused */
	depthStencilStateInfo.depthTestEnable =
		renderer->depthStencilState.depthBufferEnable;
	depthStencilStateInfo.depthWriteEnable =
		renderer->depthStencilState.depthBufferWriteEnable;
	depthStencilStateInfo.depthCompareOp = XNAToVK_CompareOp[
		renderer->depthStencilState.depthBufferFunction
	];
	depthStencilStateInfo.depthBoundsTestEnable = 0; /* Unused */
	depthStencilStateInfo.stencilTestEnable =
		renderer->depthStencilState.stencilEnable;
	depthStencilStateInfo.front = frontStencilState;
	depthStencilStateInfo.back = backStencilState;
	depthStencilStateInfo.minDepthBounds = 0; /* Unused */
	depthStencilStateInfo.maxDepthBounds = 0; /* Unused */

	/* Dynamic State */

	dynamicStateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO;
	dynamicStateInfo.pNext = NULL;
	dynamicStateInfo.flags = 0;
	dynamicStateInfo.dynamicStateCount = SDL_arraysize(dynamicStates);
	dynamicStateInfo.pDynamicStates = dynamicStates;

	/* Shaders */

	stageInfos[0].sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
	stageInfos[0].pNext = NULL;
	stageInfos[0].flags = 0;
	stageInfos[0].stage = VK_SHADER_STAGE_VERTEX_BIT;
	stageInfos[0].pName = MOJOSHADER_vkGetShaderParseData(vertShader)->mainfn;
	stageInfos[0].pSpecializationInfo = NULL;

	stageInfos[1].sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
	stageInfos[1].pNext = NULL;
	stageInfos[1].flags = 0;
	stageInfos[1].stage = VK_SHADER_STAGE_FRAGMENT_BIT;
	stageInfos[1].pName = MOJOSHADER_vkGetShaderParseData(fragShader)->mainfn;
	stageInfos[1].pSpecializationInfo = NULL;

	MOJOSHADER_vkGetShaderModules(
		&stageInfos[0].module,
		&stageInfos[1].module
	);

	/* Pipeline */

	pipelineCreateInfo.sType = VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO;
	pipelineCreateInfo.pNext = NULL;
	pipelineCreateInfo.flags = 0;
	pipelineCreateInfo.stageCount = 2;
	pipelineCreateInfo.pStages = stageInfos;
	pipelineCreateInfo.pVertexInputState = &vertexInputInfo;
	pipelineCreateInfo.pInputAssemblyState = &inputAssemblyInfo;
	pipelineCreateInfo.pTessellationState = NULL;
	pipelineCreateInfo.pViewportState = &viewportStateInfo;
	pipelineCreateInfo.pRasterizationState = &rasterizerInfo;
	pipelineCreateInfo.pMultisampleState = &multisamplingInfo;
	pipelineCreateInfo.pDepthStencilState = &depthStencilStateInfo;
	pipelineCreateInfo.pColorBlendState = &colorBlendStateInfo;
	pipelineCreateInfo.pDynamicState = &dynamicStateInfo;
	pipelineCreateInfo.layout = renderer->currentPipelineLayout;
	pipelineCreateInfo.renderPass = renderer->renderPass;
	pipelineCreateInfo.subpass = 0;
	pipelineCreateInfo.basePipelineHandle = VK_NULL_HANDLE;
	pipelineCreateInfo.basePipelineIndex = 0;

	vulkanResult = renderer->vkCreateGraphicsPipelines(
		renderer->logicalDevice,
		renderer->pipelineCache,
		1,
		&pipelineCreateInfo,
		NULL,
		&pipeline
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateGraphicsPipelines, VK_NULL_HANDLE)

	SDL_free(bindingDescriptions);
	SDL_free(attributeDescriptions);
	SDL_free(divisorDescriptions);

	PipelineHashTable_Insert(&renderer->pipelineHashTable, hash, pipeline);
	return pipeline;
}

static void VULKAN_INTERNAL_BindPipeline(VulkanRenderer *renderer)
{
	VkShaderModule vertShader, fragShader;
	MOJOSHADER_vkGetShaderModules(&vertShader, &fragShader);

	if (	renderer->needNewPipeline ||
		renderer->currentVertShader != vertShader ||
		renderer->currentFragShader != fragShader	)
	{
		VkPipeline pipeline = VULKAN_INTERNAL_FetchPipeline(renderer);

		if (pipeline != renderer->currentPipeline)
		{
			RECORD_CMD(renderer->vkCmdBindPipeline(
				renderer->currentCommandBuffer,
				VK_PIPELINE_BIND_POINT_GRAPHICS,
				pipeline
			));
			renderer->currentPipeline = pipeline;
		}

		renderer->needNewPipeline = 0;
		renderer->currentVertShader = vertShader;
		renderer->currentFragShader = fragShader;
	}
}

/* Vulkan: The Faux-Backbuffer */

static uint8_t VULKAN_INTERNAL_CreateFauxBackbuffer(
	VulkanRenderer *renderer,
	FNA3D_PresentationParameters *presentationParameters
) {
	VkFormat vulkanDepthStencilFormat;
	VkImageAspectFlags depthAspectFlags = VK_IMAGE_ASPECT_DEPTH_BIT;

	renderer->fauxBackbufferColor.handle = (VulkanTexture*) SDL_malloc(sizeof(VulkanTexture));

	if (!VULKAN_INTERNAL_CreateTexture(
		renderer,
		presentationParameters->backBufferWidth,
		presentationParameters->backBufferHeight,
		1,
		0,
		1,
		VK_SAMPLE_COUNT_1_BIT,
		1,
		renderer->swapchainFormat,
		renderer->swapchainSwizzle,
		VK_IMAGE_ASPECT_COLOR_BIT,
		VK_IMAGE_TYPE_2D,
		/* FIXME: Transfer bit probably only needs to be set on 0? */
		(
			VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT |
			VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
			VK_IMAGE_USAGE_TRANSFER_DST_BIT
		),
		renderer->fauxBackbufferColor.handle
	)) {
		FNA3D_LogError("Failed to create faux backbuffer colorbuffer");
		return 0;
	}
	renderer->fauxBackbufferColor.handle->colorFormat =
		presentationParameters->backBufferFormat;

	renderer->fauxBackbufferWidth = presentationParameters->backBufferWidth;
	renderer->fauxBackbufferHeight = presentationParameters->backBufferHeight;

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_COLOR_ATTACHMENT_READ_WRITE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		renderer->fauxBackbufferColor.handle->layerCount,
		0,
		renderer->fauxBackbufferColor.handle->levelCount,
		0,
		renderer->fauxBackbufferColor.handle->image,
		&renderer->fauxBackbufferColor.handle->resourceAccessType
	);

	renderer->fauxBackbufferMultiSampleCount =
		presentationParameters->multiSampleCount;
	renderer->fauxBackbufferMultiSampleColor = NULL;

	if (renderer->fauxBackbufferMultiSampleCount > 0)
	{
		renderer->fauxBackbufferMultiSampleColor = (VulkanTexture*) SDL_malloc(
			sizeof(VulkanTexture)
		);

		VULKAN_INTERNAL_CreateTexture(
			renderer,
			presentationParameters->backBufferWidth,
			presentationParameters->backBufferHeight,
			1,
			0,
			1,
			XNAToVK_SampleCount(presentationParameters->multiSampleCount),
			1,
			renderer->swapchainFormat,
			renderer->swapchainSwizzle,
			VK_IMAGE_ASPECT_COLOR_BIT,
			VK_IMAGE_TYPE_2D,
			VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT,
			renderer->fauxBackbufferMultiSampleColor
		);
		/* FIXME: Swapchain format may not be an FNA3D_SurfaceFormat! */
		renderer->fauxBackbufferMultiSampleColor->colorFormat = FNA3D_SURFACEFORMAT_COLOR;

		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			RESOURCE_ACCESS_COLOR_ATTACHMENT_READ_WRITE,
			VK_IMAGE_ASPECT_COLOR_BIT,
			0,
			renderer->fauxBackbufferMultiSampleColor->layerCount,
			0,
			renderer->fauxBackbufferMultiSampleColor->levelCount,
			0,
			renderer->fauxBackbufferMultiSampleColor->image,
			&renderer->fauxBackbufferMultiSampleColor->resourceAccessType
		);
	}

	/* Create faux backbuffer depth stencil image */

	renderer->fauxBackbufferDepthStencil.handle = NULL;
	if (presentationParameters->depthStencilFormat != FNA3D_DEPTHFORMAT_NONE)
	{
		renderer->fauxBackbufferDepthStencil.handle = (VulkanTexture*) SDL_malloc(
			sizeof(VulkanTexture)
		);

		vulkanDepthStencilFormat = XNAToVK_DepthFormat(
			renderer,
			presentationParameters->depthStencilFormat
		);

		if (DepthFormatContainsStencil(vulkanDepthStencilFormat))
		{
			depthAspectFlags |= VK_IMAGE_ASPECT_STENCIL_BIT;
		}

		if (!VULKAN_INTERNAL_CreateTexture(
			renderer,
			presentationParameters->backBufferWidth,
			presentationParameters->backBufferHeight,
			1,
			0,
			1,
			XNAToVK_SampleCount(
				presentationParameters->multiSampleCount
			),
			1,
			vulkanDepthStencilFormat,
			RGBA_SWIZZLE,
			depthAspectFlags,
			VK_IMAGE_TYPE_2D,
			(
				VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT |
				VK_IMAGE_USAGE_TRANSFER_DST_BIT
			),
			renderer->fauxBackbufferDepthStencil.handle
		)) {
			FNA3D_LogError("Failed to create depth stencil image");
			return 0;
		}
		renderer->fauxBackbufferDepthStencil.handle->depthStencilFormat =
			presentationParameters->depthStencilFormat;

		/* Layout transition if required */

		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_WRITE,
			depthAspectFlags,
			0,
			renderer->fauxBackbufferDepthStencil.handle->layerCount,
			0,
			renderer->fauxBackbufferDepthStencil.handle->levelCount,
			0,
			renderer->fauxBackbufferDepthStencil.handle->image,
			&renderer->fauxBackbufferDepthStencil.handle->resourceAccessType
		);

		if (!renderer->renderTargetBound)
		{
			renderer->depthStencilAttachment =
				renderer->fauxBackbufferDepthStencil.handle;
		}
	}

	if (!renderer->renderTargetBound)
	{
		renderer->colorAttachments[0] =
			renderer->fauxBackbufferColor.handle;
		renderer->colorAttachmentCount = 1;

		if (renderer->fauxBackbufferMultiSampleCount > 0)
		{
			renderer->colorMultiSampleAttachments[0] =
				renderer->fauxBackbufferMultiSampleColor;
			renderer->multiSampleCount =
				renderer->fauxBackbufferMultiSampleCount;
		}
	}

	return 1;
}

static void VULKAN_INTERNAL_DestroyFauxBackbuffer(VulkanRenderer *renderer)
{
	renderer->vkDestroyFramebuffer(
		renderer->logicalDevice,
		renderer->fauxBackbufferFramebuffer,
		NULL
	);

	VULKAN_INTERNAL_DestroyTexture(
		renderer,
		renderer->fauxBackbufferColor.handle
	);

	if (renderer->fauxBackbufferMultiSampleColor != NULL)
	{
		VULKAN_INTERNAL_DestroyTexture(
			renderer,
			renderer->fauxBackbufferMultiSampleColor
		);
	}

	if (renderer->fauxBackbufferDepthStencil.handle != NULL)
	{
		VULKAN_INTERNAL_DestroyTexture(
			renderer,
			renderer->fauxBackbufferDepthStencil.handle
		);
	}
}

/* Vulkan: Render Passes */

static VkRenderPass VULKAN_INTERNAL_FetchRenderPass(VulkanRenderer *renderer)
{
	VkResult vulkanResult;
	VkRenderPass renderPass;
	VkAttachmentDescription attachmentDescriptions[2 * MAX_RENDERTARGET_BINDINGS + 1];
	uint32_t attachmentDescriptionsCount = 0;
	uint32_t i;
	VkAttachmentReference colorAttachmentReferences[MAX_RENDERTARGET_BINDINGS];
	uint32_t colorAttachmentReferenceCount = 0;
	VkAttachmentReference resolveReferences[MAX_RENDERTARGET_BINDINGS + 1];
	uint32_t resolveReferenceCount = 0;
	VkAttachmentReference depthStencilAttachmentReference;
	VkSubpassDescription subpass;
	VkRenderPassCreateInfo renderPassCreateInfo;

	RenderPassHash hash;
	hash.colorAttachmentFormatOne = renderer->colorAttachments[0]->surfaceFormat;
	hash.colorAttachmentFormatTwo = renderer->colorAttachments[1] != NULL ?
		renderer->colorAttachments[1]->surfaceFormat :
		VK_FORMAT_UNDEFINED;
	hash.colorAttachmentFormatThree = renderer->colorAttachments[2] != NULL ?
		renderer->colorAttachments[2]->surfaceFormat :
		VK_FORMAT_UNDEFINED;
	hash.colorAttachmentFormatFour = renderer->colorAttachments[3] != NULL ?
		renderer->colorAttachments[3]->surfaceFormat :
		VK_FORMAT_UNDEFINED;
	hash.depthStencilAttachmentFormat = renderer->depthStencilAttachment != NULL ?
		renderer->depthStencilAttachment->surfaceFormat :
		VK_FORMAT_UNDEFINED;
	hash.clearColor = renderer->shouldClearColorOnBeginPass;
	hash.clearDepth = renderer->shouldClearDepthOnBeginPass;
	hash.clearStencil = renderer->shouldClearStencilOnBeginPass;
	hash.preserveTargetContents = renderer->preserveTargetContents;

	hash.width = renderer->colorAttachments[0]->dimensions.width;
	hash.height = renderer->colorAttachments[0]->dimensions.height;
	hash.multiSampleCount = renderer->multiSampleCount;

	/* The render pass is already cached, can return it */
	renderPass = RenderPassHashArray_Fetch(
		&renderer->renderPassArray,
		hash
	);
	if (renderPass != VK_NULL_HANDLE)
	{
		return renderPass;
	}

	/*
	 * FIXME: We have to always store just in case changing render state
	 * breaks the render pass. Otherwise we risk discarding necessary data.
	 * The only way to avoid this would be to buffer draw calls so we can
	 * ensure that only the final render pass before switching render targets
	 * may be discarded.
	 */

	for (i = 0; i < renderer->colorAttachmentCount; i += 1)
	{
		if (renderer->multiSampleCount > 0)
		{
			/* Resolve attachment and multisample attachment */

			attachmentDescriptions[attachmentDescriptionsCount].flags = 0;
			attachmentDescriptions[attachmentDescriptionsCount].format =
				renderer->colorAttachments[i]->surfaceFormat;
			attachmentDescriptions[attachmentDescriptionsCount].samples =
				VK_SAMPLE_COUNT_1_BIT;
			attachmentDescriptions[attachmentDescriptionsCount].loadOp =
				hash.clearColor ?
					VK_ATTACHMENT_LOAD_OP_CLEAR :
					VK_ATTACHMENT_LOAD_OP_LOAD;
			attachmentDescriptions[attachmentDescriptionsCount].storeOp =
				VK_ATTACHMENT_STORE_OP_STORE;
			attachmentDescriptions[attachmentDescriptionsCount].stencilLoadOp =
				VK_ATTACHMENT_LOAD_OP_DONT_CARE;
			attachmentDescriptions[attachmentDescriptionsCount].stencilStoreOp =
				VK_ATTACHMENT_STORE_OP_DONT_CARE;
			attachmentDescriptions[attachmentDescriptionsCount].initialLayout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
			attachmentDescriptions[attachmentDescriptionsCount].finalLayout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

			resolveReferences[resolveReferenceCount].attachment =
				attachmentDescriptionsCount;
			resolveReferences[resolveReferenceCount].layout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

			attachmentDescriptionsCount += 1;
			resolveReferenceCount += 1;

			attachmentDescriptions[attachmentDescriptionsCount].flags = 0;
			attachmentDescriptions[attachmentDescriptionsCount].format =
				renderer->colorMultiSampleAttachments[i]->surfaceFormat;
			attachmentDescriptions[attachmentDescriptionsCount].samples =
				XNAToVK_SampleCount(renderer->multiSampleCount);
			attachmentDescriptions[attachmentDescriptionsCount].loadOp =
				hash.clearColor ?
					VK_ATTACHMENT_LOAD_OP_CLEAR :
					VK_ATTACHMENT_LOAD_OP_LOAD;
			attachmentDescriptions[attachmentDescriptionsCount].storeOp =
				VK_ATTACHMENT_STORE_OP_STORE;
				/*
				 * Once above FIXME is resolved:
				 * 	hash.preserveTargetContents ?
				 *		VK_ATTACHMENT_STORE_OP_STORE :
				 *		VK_ATTACHMENT_STORE_OP_DONT_CARE;
				 */
			attachmentDescriptions[attachmentDescriptionsCount].stencilLoadOp =
				VK_ATTACHMENT_LOAD_OP_DONT_CARE;
			attachmentDescriptions[attachmentDescriptionsCount].stencilStoreOp =
				VK_ATTACHMENT_STORE_OP_DONT_CARE;
			attachmentDescriptions[attachmentDescriptionsCount].initialLayout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
			attachmentDescriptions[attachmentDescriptionsCount].finalLayout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

			colorAttachmentReferences[colorAttachmentReferenceCount].attachment =
				attachmentDescriptionsCount;
			colorAttachmentReferences[colorAttachmentReferenceCount].layout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

			attachmentDescriptionsCount += 1;
			colorAttachmentReferenceCount += 1;
		}
		else
		{
			/* Regular old attachment */

			attachmentDescriptions[attachmentDescriptionsCount].flags = 0;
			attachmentDescriptions[attachmentDescriptionsCount].format =
				renderer->colorAttachments[i]->surfaceFormat;
			attachmentDescriptions[attachmentDescriptionsCount].samples =
				VK_SAMPLE_COUNT_1_BIT;
			attachmentDescriptions[attachmentDescriptionsCount].loadOp =
				hash.clearColor ?
					VK_ATTACHMENT_LOAD_OP_CLEAR :
					VK_ATTACHMENT_LOAD_OP_LOAD;
			attachmentDescriptions[attachmentDescriptionsCount].storeOp =
				VK_ATTACHMENT_STORE_OP_STORE;
			attachmentDescriptions[attachmentDescriptionsCount].stencilLoadOp =
				VK_ATTACHMENT_LOAD_OP_DONT_CARE;
			attachmentDescriptions[attachmentDescriptionsCount].stencilStoreOp =
				VK_ATTACHMENT_STORE_OP_DONT_CARE;
			attachmentDescriptions[attachmentDescriptionsCount].initialLayout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
			attachmentDescriptions[attachmentDescriptionsCount].finalLayout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

			attachmentDescriptionsCount += 1;

			colorAttachmentReferences[colorAttachmentReferenceCount].attachment = i;
			colorAttachmentReferences[colorAttachmentReferenceCount].layout =
				VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

			colorAttachmentReferenceCount += 1;
		}
	}

	subpass.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
	subpass.flags = 0;
	subpass.inputAttachmentCount = 0;
	subpass.pInputAttachments = NULL;
	subpass.colorAttachmentCount = renderer->colorAttachmentCount;
	subpass.pColorAttachments = colorAttachmentReferences;
	subpass.preserveAttachmentCount = 0;
	subpass.pPreserveAttachments = NULL;

	if (renderer->depthStencilAttachment == NULL)
	{
		subpass.pDepthStencilAttachment = NULL;
	}
	else
	{
		attachmentDescriptions[attachmentDescriptionsCount].flags = 0;
		attachmentDescriptions[attachmentDescriptionsCount].format =
			renderer->depthStencilAttachment->surfaceFormat;
		attachmentDescriptions[attachmentDescriptionsCount].samples =
			XNAToVK_SampleCount(renderer->multiSampleCount);
		attachmentDescriptions[attachmentDescriptionsCount].loadOp =
			hash.clearDepth ?
				VK_ATTACHMENT_LOAD_OP_CLEAR :
				VK_ATTACHMENT_LOAD_OP_LOAD;
		attachmentDescriptions[attachmentDescriptionsCount].storeOp =
			VK_ATTACHMENT_STORE_OP_STORE;
			/*
			 * Once above FIXME is resolved:
			 * 	hash.preserveTargetContents ?
			 *		VK_ATTACHMENT_STORE_OP_STORE :
			 *		VK_ATTACHMENT_STORE_OP_DONT_CARE;
			 */
		attachmentDescriptions[attachmentDescriptionsCount].stencilLoadOp =
			hash.clearStencil ?
				VK_ATTACHMENT_LOAD_OP_CLEAR :
				VK_ATTACHMENT_LOAD_OP_LOAD;
		attachmentDescriptions[attachmentDescriptionsCount].stencilStoreOp =
			VK_ATTACHMENT_STORE_OP_STORE;
			/*
			 * Once above FIXME is resolved:
			 * 	hash.preserveTargetContents ?
			 *		VK_ATTACHMENT_STORE_OP_STORE :
			 *		VK_ATTACHMENT_STORE_OP_DONT_CARE;
			 */
		attachmentDescriptions[attachmentDescriptionsCount].initialLayout =
			VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;
		attachmentDescriptions[attachmentDescriptionsCount].finalLayout =
			VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;

		depthStencilAttachmentReference.attachment =
			attachmentDescriptionsCount;
		depthStencilAttachmentReference.layout =
			VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;

		subpass.pDepthStencilAttachment =
			&depthStencilAttachmentReference;

		attachmentDescriptionsCount += 1;
	}

	if (renderer->multiSampleCount > 0)
	{
		subpass.pResolveAttachments = resolveReferences;
	}
	else
	{
		subpass.pResolveAttachments = NULL;
	}

	renderPassCreateInfo.sType = VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO;
	renderPassCreateInfo.pNext = NULL;
	renderPassCreateInfo.flags = 0;
	renderPassCreateInfo.attachmentCount = attachmentDescriptionsCount;
	renderPassCreateInfo.pAttachments = attachmentDescriptions;
	renderPassCreateInfo.subpassCount = 1;
	renderPassCreateInfo.pSubpasses = &subpass;
	renderPassCreateInfo.dependencyCount = 0;
	renderPassCreateInfo.pDependencies = NULL;

	vulkanResult = renderer->vkCreateRenderPass(
		renderer->logicalDevice,
		&renderPassCreateInfo,
		NULL,
		&renderPass
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateRenderPass, VK_NULL_HANDLE)

	RenderPassHashArray_Insert(
		&renderer->renderPassArray,
		hash,
		renderPass
	);
	return renderPass;
}

static VkFramebuffer VULKAN_INTERNAL_FetchFramebuffer(
	VulkanRenderer *renderer,
	VkRenderPass renderPass
) {
	VkFramebuffer framebuffer;
	VkImageView imageViewAttachments[2 * MAX_RENDERTARGET_BINDINGS + 1];
	uint32_t i, attachmentCount;
	VkFramebufferCreateInfo framebufferInfo;
	VkResult vulkanResult;

	FramebufferHash hash;
	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		hash.colorAttachmentViews[i] = (
			renderer->colorAttachments[i] != NULL ?
				renderer->colorAttachments[i]->rtViews[
					renderer->attachmentCubeFaces[i]
				] :
				VK_NULL_HANDLE
		);
		hash.colorMultiSampleAttachmentViews[i] = (
			renderer->colorMultiSampleAttachments[i] != NULL ?
				renderer->colorMultiSampleAttachments[i]->rtViews[
					renderer->attachmentCubeFaces[i]
				] :
				VK_NULL_HANDLE
		);
	}
	hash.depthStencilAttachmentView = (
		renderer->depthStencilAttachment != NULL ?
			renderer->depthStencilAttachment->rtViews[0] :
			VK_NULL_HANDLE
	);
	hash.width = renderer->colorAttachments[0]->dimensions.width;
	hash.height = renderer->colorAttachments[0]->dimensions.height;

	/* Framebuffer is cached, can return it */
	framebuffer = FramebufferHashArray_Fetch(
		&renderer->framebufferArray,
		hash
	);
	if (framebuffer != VK_NULL_HANDLE)
	{
		return framebuffer;
	}

	/* Otherwise make a new one */

	attachmentCount = 0;

	for (i = 0; i < renderer->colorAttachmentCount; i += 1)
	{
		imageViewAttachments[attachmentCount] =
			renderer->colorAttachments[i]->rtViews[
				renderer->attachmentCubeFaces[i]
			];
		attachmentCount += 1;

		if (renderer->multiSampleCount > 0)
		{
			imageViewAttachments[attachmentCount] =
				renderer->colorMultiSampleAttachments[i]->rtViews[
					renderer->attachmentCubeFaces[i]
				];
			attachmentCount += 1;
		}
	}
	if (renderer->depthStencilAttachment != NULL)
	{
		imageViewAttachments[attachmentCount] =
			renderer->depthStencilAttachment->rtViews[0];
		attachmentCount += 1;
	}

	framebufferInfo.sType = VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO;
	framebufferInfo.pNext = NULL;
	framebufferInfo.flags = 0;
	framebufferInfo.renderPass = renderPass;
	framebufferInfo.attachmentCount = attachmentCount;
	framebufferInfo.pAttachments = imageViewAttachments;
	framebufferInfo.width = renderer->colorAttachments[0]->dimensions.width;
	framebufferInfo.height = renderer->colorAttachments[0]->dimensions.height;
	framebufferInfo.layers = 1;

	vulkanResult = renderer->vkCreateFramebuffer(
		renderer->logicalDevice,
		&framebufferInfo,
		NULL,
		&framebuffer
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateFramebuffer, VK_NULL_HANDLE)

	FramebufferHashArray_Insert(
		&renderer->framebufferArray,
		hash,
		framebuffer
	);
	return framebuffer;
}

static void VULKAN_INTERNAL_MaybeEndRenderPass(
	VulkanRenderer *renderer,
	uint8_t allowBreak
) {
	SDL_LockMutex(renderer->passLock);

	if (renderer->renderPassInProgress)
	{
		RECORD_CMD(renderer->vkCmdEndRenderPass(renderer->currentCommandBuffer));

		renderer->renderPassInProgress = 0;
		renderer->needNewRenderPass = 1;
		renderer->drawCallMadeThisPass = 0;

		/* Unlocking long-term lock */
		SDL_UnlockMutex(renderer->passLock);

		if (allowBreak && (renderer->numActiveCommands >= COMMAND_LIMIT))
		{
			VULKAN_INTERNAL_EndCommandBuffer(renderer, 1, 1);
		}
	}

	SDL_UnlockMutex(renderer->passLock);
}

static void VULKAN_INTERNAL_BeginRenderPass(
	VulkanRenderer *renderer
) {
	VkRenderPassBeginInfo renderPassBeginInfo;
	VkFramebuffer framebuffer;
	VkImageAspectFlags depthAspectFlags;
	float blendConstants[4];
	VkClearValue clearValues[2 * MAX_RENDERTARGET_BINDINGS + 1];
	VkClearColorValue clearColorValue;
	uint32_t clearValueCount = 0;
	uint32_t i;

	if (!renderer->needNewRenderPass)
	{
		return;
	}

	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);

	SDL_LockMutex(renderer->passLock);

	renderer->renderPass = VULKAN_INTERNAL_FetchRenderPass(renderer);
	framebuffer = VULKAN_INTERNAL_FetchFramebuffer(
		renderer,
		renderer->renderPass
	);

	renderer->needNewPipeline = 1;

	renderPassBeginInfo.sType = VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO;
	renderPassBeginInfo.pNext = NULL;
	renderPassBeginInfo.renderPass = renderer->renderPass;
	renderPassBeginInfo.framebuffer = framebuffer;
	renderPassBeginInfo.renderArea.offset.x = 0;
	renderPassBeginInfo.renderArea.offset.y = 0;
	renderPassBeginInfo.renderArea.extent.width =
		renderer->colorAttachments[0]->dimensions.width;
	renderPassBeginInfo.renderArea.extent.height =
		renderer->colorAttachments[0]->dimensions.height;

	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		if (renderer->colorAttachments[i] != NULL)
		{
			VULKAN_INTERNAL_ImageMemoryBarrier(
				renderer,
				RESOURCE_ACCESS_COLOR_ATTACHMENT_READ_WRITE,
				VK_IMAGE_ASPECT_COLOR_BIT,
				0,
				renderer->colorAttachments[i]->layerCount,
				0,
				1,
				0,
				renderer->colorAttachments[i]->image,
				&renderer->colorAttachments[i]->resourceAccessType
			);

			/*
			 * In MRT scenarios we need to provide as many clearValue structs
			 * as RTs in the current framebuffer even if they aren't cleared
			 */

			if (renderer->shouldClearColorOnBeginPass)
			{
				clearColorValue = renderer->clearColorValue;
			}
			else
			{
				clearColorValue.float32[0] = 0;
				clearColorValue.float32[1] = 0;
				clearColorValue.float32[2] = 0;
				clearColorValue.float32[3] = 0;
			}

			clearValues[clearValueCount].color.float32[0] = clearColorValue.float32[0];
			clearValues[clearValueCount].color.float32[1] = clearColorValue.float32[1];
			clearValues[clearValueCount].color.float32[2] = clearColorValue.float32[2];
			clearValues[clearValueCount].color.float32[3] = clearColorValue.float32[3];
			clearValueCount += 1;

			if (renderer->colorMultiSampleAttachments[i] != NULL)
			{
				clearValues[clearValueCount].color.float32[0] = clearColorValue.float32[0];
				clearValues[clearValueCount].color.float32[1] = clearColorValue.float32[1];
				clearValues[clearValueCount].color.float32[2] = clearColorValue.float32[2];
				clearValues[clearValueCount].color.float32[3] = clearColorValue.float32[3];
				clearValueCount += 1;
			}
		}
	}

	if (renderer->depthStencilAttachment != NULL)
	{
		depthAspectFlags = VK_IMAGE_ASPECT_DEPTH_BIT;
		if (DepthFormatContainsStencil(
			renderer->depthStencilAttachment->surfaceFormat
		)) {
			depthAspectFlags |= VK_IMAGE_ASPECT_STENCIL_BIT;
		}

		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_WRITE,
			depthAspectFlags,
			0,
			renderer->depthStencilAttachment->layerCount,
			0,
			1,
			0,
			renderer->depthStencilAttachment->image,
			&renderer->depthStencilAttachment->resourceAccessType
		);

		if (renderer->shouldClearDepthOnBeginPass || renderer->shouldClearStencilOnBeginPass)
		{
			clearValues[clearValueCount].depthStencil.depth = renderer->clearDepthStencilValue.depth;
			clearValues[clearValueCount].depthStencil.stencil = renderer->clearDepthStencilValue.stencil;
			clearValueCount += 1;
		}
	}

	renderPassBeginInfo.clearValueCount = clearValueCount;
	renderPassBeginInfo.pClearValues = clearValues;

	RECORD_CMD(renderer->vkCmdBeginRenderPass(
		renderer->currentCommandBuffer,
		&renderPassBeginInfo,
		VK_SUBPASS_CONTENTS_INLINE
	));

	renderer->renderPassInProgress = 1;

	VULKAN_INTERNAL_SetViewportCommand(renderer);
	VULKAN_INTERNAL_SetScissorRectCommand(renderer);
	VULKAN_INTERNAL_SetStencilReferenceValueCommand(renderer);
	VULKAN_INTERNAL_SetDepthBiasCommand(renderer);

	blendConstants[0] = renderer->blendState.blendFactor.r / 255.0f;
	blendConstants[1] = renderer->blendState.blendFactor.g / 255.0f;
	blendConstants[2] = renderer->blendState.blendFactor.b / 255.0f;
	blendConstants[3] = renderer->blendState.blendFactor.a / 255.0f;

	RECORD_CMD(renderer->vkCmdSetBlendConstants(
		renderer->currentCommandBuffer,
		blendConstants
	));

	/* Reset bindings for the current frame in flight */

	for (i = 0; i < MAX_TOTAL_SAMPLERS; i += 1)
	{
		if (renderer->textures[i] != &NullTexture)
		{
			renderer->textureNeedsUpdate[i] = 1;
		}
		if (renderer->samplers[i] != VK_NULL_HANDLE)
		{
			renderer->samplerNeedsUpdate[i] = 1;
		}
	}

	renderer->ldFragUniformBuffer = VK_NULL_HANDLE;
	renderer->ldFragUniformOffset = 0;
	renderer->ldFragUniformSize = 0;
	renderer->ldVertUniformBuffer = VK_NULL_HANDLE;
	renderer->ldVertUniformOffset = 0;
	renderer->ldVertUniformSize = 0;
	renderer->currentPipeline = VK_NULL_HANDLE;

	renderer->needNewRenderPass = 0;
	renderer->shouldClearColorOnBeginPass = 0;
	renderer->shouldClearDepthOnBeginPass = 0;
	renderer->shouldClearStencilOnBeginPass = 0;
}

static void VULKAN_INTERNAL_BeginRenderPassClear(
	VulkanRenderer *renderer,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil,
	uint8_t clearColor,
	uint8_t clearDepth,
	uint8_t clearStencil
) {
	if (!clearColor && !clearDepth && !clearStencil)
	{
		return;
	}

	renderer->shouldClearColorOnBeginPass |= clearColor;
	renderer->shouldClearDepthOnBeginPass |= clearDepth;
	renderer->shouldClearStencilOnBeginPass |= clearStencil;

	if (clearColor)
	{
		renderer->clearColorValue.float32[0] = color->x;
		renderer->clearColorValue.float32[1] = color->y;
		renderer->clearColorValue.float32[2] = color->z;
		renderer->clearColorValue.float32[3] = color->w;
	}

	if (clearDepth)
	{
		if (depth < 0.0f)
		{
			depth = 0.0f;
		}
		else if (depth > 1.0f)
		{
			depth = 1.0f;
		}

		renderer->clearDepthStencilValue.depth = depth;
	}

	if (clearStencil)
	{
		renderer->clearDepthStencilValue.stencil = stencil;
	}

	renderer->needNewRenderPass = 1;
}

static void VULKAN_INTERNAL_MidRenderPassClear(
	VulkanRenderer *renderer,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil,
	uint8_t clearColor,
	uint8_t clearDepth,
	uint8_t clearStencil
) {
	VkClearAttachment clearAttachments[2 * MAX_RENDERTARGET_BINDINGS + 1];
	VkClearRect clearRect;
	VkClearValue clearValue =
	{{{
		color->x,
		color->y,
		color->z,
		color->w
	}}};
	uint8_t shouldClearDepthStencil = (
		(clearDepth || clearStencil) &&
		renderer->depthStencilAttachment != NULL
	);
	uint32_t i, attachmentCount;

	if (!clearColor && !shouldClearDepthStencil)
	{
		return;
	}

	attachmentCount = 0;

	clearRect.baseArrayLayer = 0;
	clearRect.layerCount = 1;
	clearRect.rect.offset.x = 0;
	clearRect.rect.offset.y = 0;
	clearRect.rect.extent.width = renderer->colorAttachments[0]->dimensions.width;
	clearRect.rect.extent.height = renderer->colorAttachments[0]->dimensions.height;

	if (clearColor)
	{
		for (i = 0; i < renderer->colorAttachmentCount; i += 1)
		{
			clearAttachments[attachmentCount].aspectMask =
				VK_IMAGE_ASPECT_COLOR_BIT;
			clearAttachments[attachmentCount].colorAttachment =
				attachmentCount;
			clearAttachments[attachmentCount].clearValue =
				clearValue;
			attachmentCount += 1;

			/* Do NOT clear the multisample image here!
			 * Vulkan treats them both as the same color attachment.
			 * Vulkan is a very good and not confusing at all API.
			 */
		}
	}

	if (shouldClearDepthStencil)
	{
		clearAttachments[attachmentCount].aspectMask = 0;
		clearAttachments[attachmentCount].colorAttachment = 0;
		if (clearDepth)
		{
			if (depth < 0.0f)
			{
				depth = 0.0f;
			}
			else if (depth > 1.0f)
			{
				depth = 1.0f;
			}
			clearAttachments[attachmentCount].aspectMask |= VK_IMAGE_ASPECT_DEPTH_BIT;
			clearAttachments[attachmentCount].clearValue.depthStencil.depth = depth;
		}
		else
		{
			clearAttachments[attachmentCount].clearValue.depthStencil.depth = 0.0f;
		}
		if (clearStencil)
		{
			clearAttachments[attachmentCount].aspectMask |= VK_IMAGE_ASPECT_STENCIL_BIT;
			clearAttachments[attachmentCount].clearValue.depthStencil.stencil = stencil;
		}
		else
		{
			clearAttachments[attachmentCount].clearValue.depthStencil.stencil = 0;
		}

		attachmentCount += 1;
	}

	RECORD_CMD(renderer->vkCmdClearAttachments(
		renderer->currentCommandBuffer,
		attachmentCount,
		clearAttachments,
		1,
		&clearRect
	));
}

/* Vulkan: Sampler State */

static VkSampler VULKAN_INTERNAL_FetchSamplerState(
	VulkanRenderer *renderer,
	FNA3D_SamplerState *samplerState,
	uint32_t levelCount
) {
	VkSamplerCreateInfo createInfo;
	VkSampler state;
	VkResult result;

	PackedState hash = GetPackedSamplerState(*samplerState);
	state = SamplerStateHashArray_Fetch(
		&renderer->samplerStateArray,
		hash
	);
	if (state != VK_NULL_HANDLE)
	{
		return state;
	}

	createInfo.sType = VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO;
	createInfo.pNext = NULL;
	createInfo.flags = 0;
	createInfo.magFilter = XNAToVK_MagFilter[samplerState->filter];
	createInfo.minFilter = XNAToVK_MinFilter[samplerState->filter];
	createInfo.mipmapMode = XNAToVK_MipFilter[samplerState->filter];
	createInfo.addressModeU = XNAToVK_SamplerAddressMode[
		samplerState->addressU
	];
	createInfo.addressModeV = XNAToVK_SamplerAddressMode[
		samplerState->addressV
	];
	createInfo.addressModeW = XNAToVK_SamplerAddressMode[
		samplerState->addressW
	];
	createInfo.mipLodBias = samplerState->mipMapLevelOfDetailBias;
	createInfo.anisotropyEnable = (samplerState->filter == FNA3D_TEXTUREFILTER_ANISOTROPIC);
	createInfo.maxAnisotropy = SDL_min(
		(float) SDL_max(1, samplerState->maxAnisotropy),
		renderer->physicalDeviceProperties.properties.limits.maxSamplerAnisotropy
	);
	createInfo.compareEnable = 0;
	createInfo.compareOp = 0;
	createInfo.minLod = (float) samplerState->maxMipLevel;
	createInfo.maxLod = VK_LOD_CLAMP_NONE;
	createInfo.borderColor = VK_BORDER_COLOR_FLOAT_TRANSPARENT_BLACK;
	createInfo.unnormalizedCoordinates = 0;

	result = renderer->vkCreateSampler(
		renderer->logicalDevice,
		&createInfo,
		NULL,
		&state
	);
	VULKAN_ERROR_CHECK(result, vkCreateSampler, 0)

	SamplerStateHashArray_Insert(
		&renderer->samplerStateArray,
		hash,
		state
	);

	return state;
}

/* Renderer Implementation */

/* Quit */

static void VULKAN_DestroyDevice(FNA3D_Device *device)
{
	VulkanRenderer *renderer = (VulkanRenderer*) device->driverData;
	ShaderResources *shaderResources;
	PipelineHashArray hashArray;
	VulkanMemorySubAllocator *allocator;
	uint32_t i;
	int32_t j, k;
	VkResult waitResult, pipelineCacheResult;
	size_t pipelineCacheSize;
	uint8_t *pipelineCacheData;
	SDL_RWops *pipelineCacheFile;
	const char *pipelineCacheFileName;

	VULKAN_INTERNAL_FlushCommands(renderer, 1);

	waitResult = renderer->vkDeviceWaitIdle(renderer->logicalDevice);

	/* Just treat this as a warning, we're about to exit anyway... */
	if (waitResult != VK_SUCCESS)
	{
		FNA3D_LogWarn("vkDeviceWaitIdle: %s\n", VkErrorMessages(waitResult));
	}

	/* Save the pipeline cache to disk */

	pipelineCacheResult = renderer->vkGetPipelineCacheData(
		renderer->logicalDevice,
		renderer->pipelineCache,
		&pipelineCacheSize,
		NULL
	);

	if (pipelineCacheResult == VK_SUCCESS)
	{
		pipelineCacheFileName = SDL_GetHint("FNA3D_VULKAN_PIPELINE_CACHE_FILE_NAME");
		if (pipelineCacheFileName == NULL)
		{
			pipelineCacheFileName = DEFAULT_PIPELINE_CACHE_FILE_NAME;
		}
		if (pipelineCacheFileName[0] == '\0')
		{
			/* For intentionally empty file names, assume caching is disabled */
			pipelineCacheFile = NULL;
		}
		else
		{
			pipelineCacheFile = SDL_RWFromFile(pipelineCacheFileName, "wb");
		}

		if (pipelineCacheFile != NULL)
		{
			pipelineCacheData = SDL_malloc(pipelineCacheSize);

			renderer->vkGetPipelineCacheData(
				renderer->logicalDevice,
				renderer->pipelineCache,
				&pipelineCacheSize,
				pipelineCacheData
			);

			SDL_RWwrite(pipelineCacheFile, pipelineCacheData, sizeof(uint8_t), pipelineCacheSize);
			SDL_RWclose(pipelineCacheFile);

			SDL_free(pipelineCacheData);
		}
		else
		{
			FNA3D_LogWarn("Could not open pipeline cache file for writing!");
		}
	}
	else
	{
		FNA3D_LogWarn("vkGetPipelineCacheData: %s", VkErrorMessages(pipelineCacheResult));
		FNA3D_LogWarn("Error getting data from pipeline cache, aborting save!");
	}

	/* Clean up! */

	VULKAN_INTERNAL_DestroyBuffer(renderer, renderer->dummyVertUniformBuffer);
	VULKAN_INTERNAL_DestroyBuffer(renderer, renderer->dummyFragUniformBuffer);

	VULKAN_INTERNAL_DestroyTexture(renderer, renderer->dummyVertTexture);
	VULKAN_INTERNAL_DestroyTexture(renderer, renderer->dummyVertTexture3D);
	VULKAN_INTERNAL_DestroyTexture(renderer, renderer->dummyVertTextureCube);

	VULKAN_INTERNAL_DestroyTexture(renderer, renderer->dummyFragTexture);
	VULKAN_INTERNAL_DestroyTexture(renderer, renderer->dummyFragTexture3D);
	VULKAN_INTERNAL_DestroyTexture(renderer, renderer->dummyFragTextureCube);

	/* We have to do this twice so the rotation happens correctly */
	VULKAN_INTERNAL_PerformDeferredDestroys(renderer);
	VULKAN_INTERNAL_PerformDeferredDestroys(renderer);

	VULKAN_INTERNAL_DestroyBuffer(renderer, renderer->textureStagingBuffer->fastBuffer);
	VULKAN_INTERNAL_DestroyBuffer(renderer, renderer->textureStagingBuffer->slowBuffer);
	SDL_free(renderer->textureStagingBuffer);

	MOJOSHADER_vkDestroyContext(renderer->mojoshaderContext);
	VULKAN_INTERNAL_DestroyFauxBackbuffer(renderer);

	renderer->vkDestroySemaphore(
		renderer->logicalDevice,
		renderer->imageAvailableSemaphore,
		NULL
	);

	renderer->vkDestroySemaphore(
		renderer->logicalDevice,
		renderer->renderFinishedSemaphore,
		NULL
	);

	renderer->vkDestroySemaphore(
		renderer->logicalDevice,
		renderer->defragSemaphore,
		NULL
	);

	renderer->vkDestroyFence(
		renderer->logicalDevice,
		renderer->inFlightFence,
		NULL
	);

	renderer->vkDestroyFence(
		renderer->logicalDevice,
		renderer->defragFence,
		NULL
	);

	renderer->vkDestroyQueryPool(
		renderer->logicalDevice,
		renderer->queryPool,
		NULL
	);

	renderer->vkDestroyCommandPool(
		renderer->logicalDevice,
		renderer->commandPool,
		NULL
	);

	for (i = 0; i < NUM_PIPELINE_HASH_BUCKETS; i += 1)
	{
		hashArray = renderer->pipelineHashTable.buckets[i];
		for (j = 0; j < hashArray.count; j += 1)
		{
			renderer->vkDestroyPipeline(
				renderer->logicalDevice,
				renderer->pipelineHashTable.buckets[i].elements[j].value,
				NULL
			);
		}
		if (hashArray.elements != NULL)
		{
			SDL_free(hashArray.elements);
		}
	}

	for (i = 0; i < NUM_SHADER_RESOURCES_BUCKETS; i += 1)
	{
		for (j = 0; j < renderer->shaderResourcesHashTable.buckets[i].count; j += 1)
		{
			shaderResources = renderer->shaderResourcesHashTable.buckets[i].elements[j].value;
			ShaderResources_Destroy(renderer, shaderResources);
		}

		SDL_free(renderer->shaderResourcesHashTable.buckets[i].elements);
	}

	renderer->vkDestroyDescriptorPool(
		renderer->logicalDevice,
		renderer->uniformBufferDescriptorPool,
		NULL
	);

	for (i = 0; i < NUM_DESCRIPTOR_SET_LAYOUT_BUCKETS; i += 1)
	{
		for (j = 0; j < renderer->descriptorSetLayoutTable.buckets[i].count; j += 1)
		{
			renderer->vkDestroyDescriptorSetLayout(
				renderer->logicalDevice,
				renderer->descriptorSetLayoutTable.buckets[i].elements[j].value,
				NULL
			);
		}

		SDL_free(renderer->descriptorSetLayoutTable.buckets[i].elements);
	}

	renderer->vkDestroyDescriptorSetLayout(
		renderer->logicalDevice,
		renderer->vertexUniformBufferDescriptorSetLayout,
		NULL
	);

	renderer->vkDestroyDescriptorSetLayout(
		renderer->logicalDevice,
		renderer->fragUniformBufferDescriptorSetLayout,
		NULL
	);

	for (i = 0; i < NUM_PIPELINE_LAYOUT_BUCKETS; i += 1)
	{
		for (j = 0; j < renderer->pipelineLayoutTable.buckets[i].count; j += 1)
		{
			renderer->vkDestroyPipelineLayout(
				renderer->logicalDevice,
				renderer->pipelineLayoutTable.buckets[i].elements[j].value,
				NULL
			);
		}

		SDL_free(renderer->pipelineLayoutTable.buckets[i].elements);
	}

	renderer->vkDestroyPipelineCache(
		renderer->logicalDevice,
		renderer->pipelineCache,
		NULL
	);

	for (i = 0; i < renderer->renderPassArray.count; i += 1)
	{
		renderer->vkDestroyRenderPass(
			renderer->logicalDevice,
			renderer->renderPassArray.elements[i].value,
			NULL
		);
	}

	for (i = 0; i < renderer->samplerStateArray.count; i += 1)
	{
		renderer->vkDestroySampler(
			renderer->logicalDevice,
			renderer->samplerStateArray.elements[i].value,
			NULL
		);
	}

	renderer->vkDestroySampler(
		renderer->logicalDevice,
		renderer->dummyVertSamplerState,
		NULL
	);
	renderer->vkDestroySampler(
		renderer->logicalDevice,
		renderer->dummyVertSampler3DState,
		NULL
	);
	renderer->vkDestroySampler(
		renderer->logicalDevice,
		renderer->dummyVertSamplerCubeState,
		NULL
	);

	renderer->vkDestroySampler(
		renderer->logicalDevice,
		renderer->dummyFragSamplerState,
		NULL
	);
	renderer->vkDestroySampler(
		renderer->logicalDevice,
		renderer->dummyFragSampler3DState,
		NULL
	);
	renderer->vkDestroySampler(
		renderer->logicalDevice,
		renderer->dummyFragSamplerCubeState,
		NULL
	);

	VULKAN_INTERNAL_DestroySwapchain(renderer);

	renderer->vkDestroySurfaceKHR(
		renderer->instance,
		renderer->surface,
		NULL
	);

	for (i = 0; i < VK_MAX_MEMORY_TYPES; i += 1)
	{
		allocator = &renderer->memoryAllocator->subAllocators[i];

		for (j = allocator->allocationCount - 1; j >= 0; j -= 1)
		{
			for (k = allocator->allocations[j]->usedRegionCount - 1; k >= 0; k -= 1)
			{
				VULKAN_INTERNAL_RemoveMemoryUsedRegion(
					renderer,
					allocator->allocations[j]->usedRegions[k]
				);
			}

			VULKAN_INTERNAL_DeallocateMemory(
				renderer,
				allocator,
				j
			);
		}

		if (renderer->memoryAllocator->subAllocators[i].allocations != NULL)
		{
			SDL_free(renderer->memoryAllocator->subAllocators[i].allocations);
		}

		SDL_free(renderer->memoryAllocator->subAllocators[i].sortedFreeRegions);
	}

	SDL_free(renderer->memoryAllocator);

	SDL_DestroyMutex(renderer->commandLock);
	SDL_DestroyMutex(renderer->passLock);
	SDL_DestroyMutex(renderer->disposeLock);
	SDL_DestroyMutex(renderer->allocatorLock);
	SDL_DestroyMutex(renderer->stagingLock);

	SDL_free(renderer->buffersInUse);
	SDL_free(renderer->submittedBuffers);

	SDL_free(renderer->inactiveCommandBuffers);
	SDL_free(renderer->activeCommandBuffers);
	SDL_free(renderer->submittedCommandBuffers);

	renderer->vkDestroyDevice(renderer->logicalDevice, NULL);
	renderer->vkDestroyInstance(renderer->instance, NULL);

	SDL_free(renderer->renderPassArray.elements);
	SDL_free(renderer->samplerStateArray.elements);
	SDL_free(renderer->vertexBufferBindingsCache.elements);

	SDL_free(renderer->submittedRenderbuffersToDestroy);
	SDL_free(renderer->submittedBuffersToDestroy);
	SDL_free(renderer->submittedTexturesToDestroy);
	SDL_free(renderer->submittedEffectsToDestroy);

	SDL_free(renderer->defragmentedBuffersToDestroy);
	SDL_free(renderer->defragmentedImagesToDestroy);
	SDL_free(renderer->defragmentedImageViewsToDestroy);
	SDL_free(renderer->usedRegionsToDestroy);

	SDL_free(renderer->renderbuffersToDestroy);
	SDL_free(renderer->buffersToDestroy);
	SDL_free(renderer->effectsToDestroy);
	SDL_free(renderer->texturesToDestroy);

	SDL_free(renderer);
	SDL_free(device);
}

/* Presentation */

static void VULKAN_SwapBuffers(
	FNA3D_Renderer *driverData,
	FNA3D_Rect *sourceRectangle,
	FNA3D_Rect *destinationRectangle,
	void* overrideWindowHandle
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;

	/* Perform any pending clears before presenting */
	if (	renderer->shouldClearColorOnBeginPass ||
		renderer->shouldClearDepthOnBeginPass ||
		renderer->shouldClearStencilOnBeginPass	)
	{
		VULKAN_INTERNAL_BeginRenderPass(renderer);
	}

	VULKAN_INTERNAL_FlushCommandsAndPresent(
		renderer,
		sourceRectangle,
		destinationRectangle,
		overrideWindowHandle
	);

	renderer->needNewRenderPass = 1;
}

/* Drawing */

static void VULKAN_Clear(
	FNA3D_Renderer *driverData,
	FNA3D_ClearOptions options,
	FNA3D_Vec4 *color,
	float depth,
	int32_t stencil
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	uint8_t clearColor = (options & FNA3D_CLEAROPTIONS_TARGET) == FNA3D_CLEAROPTIONS_TARGET;
	uint8_t clearDepth = (options & FNA3D_CLEAROPTIONS_DEPTHBUFFER) == FNA3D_CLEAROPTIONS_DEPTHBUFFER;
	uint8_t clearStencil = (options & FNA3D_CLEAROPTIONS_STENCIL) == FNA3D_CLEAROPTIONS_STENCIL;

	if (renderer->renderPassInProgress && renderer->drawCallMadeThisPass && !renderer->needNewRenderPass)
	{
		VULKAN_INTERNAL_MidRenderPassClear(
			renderer,
			color,
			depth,
			stencil,
			clearColor,
			clearDepth,
			clearStencil
		);
	}
	else
	{
		VULKAN_INTERNAL_BeginRenderPassClear(
			renderer,
			color,
			depth,
			stencil,
			clearColor,
			clearDepth,
			clearStencil
		);
	}
}

static void VULKAN_DrawInstancedPrimitives(
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
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanBuffer *indexBuffer = (VulkanBuffer*) indices;
	VulkanSubBuffer *subbuf = indexBuffer->subBuffers[
		indexBuffer->currentSubBufferIndex
	];
	VkDescriptorSet descriptorSets[4];
	MOJOSHADER_vkShader *vertShader, *fragShader;
	ShaderResources *vertShaderResources, *fragShaderResources;
	uint32_t dynamicOffsets[2];

	/* Note that minVertexIndex/numVertices are NOT used! */

	VULKAN_INTERNAL_BufferMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_INDEX_BUFFER,
		subbuf->buffer,
		&subbuf->resourceAccessType
	);

	VULKAN_INTERNAL_MarkAsBound(renderer, indexBuffer);

	if (primitiveType != renderer->currentPrimitiveType)
	{
		renderer->currentPrimitiveType = primitiveType;
		renderer->needNewRenderPass = 1;
	}

	VULKAN_INTERNAL_BeginRenderPass(renderer);
	VULKAN_INTERNAL_BindPipeline(renderer);

	if (renderer->numVertexBindings > 0)
	{
		/* FIXME: State shadowing for vertex buffers? -flibit */
		RECORD_CMD(renderer->vkCmdBindVertexBuffers(
			renderer->currentCommandBuffer,
			0,
			renderer->numVertexBindings,
			renderer->boundVertexBuffers,
			renderer->boundVertexBufferOffsets
		));
	}
	/* FIXME: State shadowing for index buffers? -flibit */
	RECORD_CMD(renderer->vkCmdBindIndexBuffer(
		renderer->currentCommandBuffer,
		subbuf->buffer,
		0,
		XNAToVK_IndexType[indexElementSize]
	));

	MOJOSHADER_vkGetBoundShaders(&vertShader, &fragShader);
	vertShaderResources = VULKAN_INTERNAL_FetchShaderResources(
		renderer,
		vertShader,
		VK_SHADER_STAGE_VERTEX_BIT
	);
	fragShaderResources = VULKAN_INTERNAL_FetchShaderResources(
		renderer,
		fragShader,
		VK_SHADER_STAGE_FRAGMENT_BIT
	);

	VULKAN_INTERNAL_FetchDescriptorSetDataAndOffsets(
		renderer,
		vertShaderResources,
		fragShaderResources,
		descriptorSets,
		dynamicOffsets
	);

	RECORD_CMD(renderer->vkCmdBindDescriptorSets(
		renderer->currentCommandBuffer,
		VK_PIPELINE_BIND_POINT_GRAPHICS,
		renderer->currentPipelineLayout,
		0,
		4,
		descriptorSets,
		2,
		dynamicOffsets
	));

	RECORD_CMD(renderer->vkCmdDrawIndexed(
		renderer->currentCommandBuffer,
		PrimitiveVerts(primitiveType, primitiveCount),
		instanceCount,
		startIndex,
		baseVertex,
		0
	));

	renderer->drawCallMadeThisPass = 1;
}

static void VULKAN_DrawIndexedPrimitives(
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
	VULKAN_DrawInstancedPrimitives(
		driverData,
		primitiveType,
		baseVertex,
		minVertexIndex,
		numVertices,
		startIndex,
		primitiveCount,
		1,
		indices,
		indexElementSize
	);
}

static void VULKAN_DrawPrimitives(
	FNA3D_Renderer *driverData,
	FNA3D_PrimitiveType primitiveType,
	int32_t vertexStart,
	int32_t primitiveCount
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VkDescriptorSet descriptorSets[4];
	MOJOSHADER_vkShader *vertShader, *fragShader;
	ShaderResources *vertShaderResources, *fragShaderResources;
	uint32_t dynamicOffsets[2];

	if (primitiveType != renderer->currentPrimitiveType)
	{
		renderer->currentPrimitiveType = primitiveType;
		renderer->needNewRenderPass = 1;
	}
	VULKAN_INTERNAL_BeginRenderPass(renderer);
	VULKAN_INTERNAL_BindPipeline(renderer);

	if (renderer->numVertexBindings > 0)
	{
		/* FIXME: State shadowing for vertex buffers? -flibit */
		RECORD_CMD(renderer->vkCmdBindVertexBuffers(
			renderer->currentCommandBuffer,
			0,
			renderer->numVertexBindings,
			renderer->boundVertexBuffers,
			renderer->boundVertexBufferOffsets
		));
	}

	MOJOSHADER_vkGetBoundShaders(&vertShader, &fragShader);
	vertShaderResources = VULKAN_INTERNAL_FetchShaderResources(
		renderer,
		vertShader,
		VK_SHADER_STAGE_VERTEX_BIT
	);
	fragShaderResources = VULKAN_INTERNAL_FetchShaderResources(
		renderer,
		fragShader,
		VK_SHADER_STAGE_FRAGMENT_BIT
	);

	VULKAN_INTERNAL_FetchDescriptorSetDataAndOffsets(
		renderer,
		vertShaderResources,
		fragShaderResources,
		descriptorSets,
		dynamicOffsets
	);

	RECORD_CMD(renderer->vkCmdBindDescriptorSets(
		renderer->currentCommandBuffer,
		VK_PIPELINE_BIND_POINT_GRAPHICS,
		renderer->currentPipelineLayout,
		0,
		4,
		descriptorSets,
		2,
		dynamicOffsets
	));

	RECORD_CMD(renderer->vkCmdDraw(
		renderer->currentCommandBuffer,
		PrimitiveVerts(primitiveType, primitiveCount),
		1,
		vertexStart,
		0
	));

	renderer->drawCallMadeThisPass = 1;
}

/* Mutable Render States */

static void VULKAN_SetViewport(
	FNA3D_Renderer *driverData,
	FNA3D_Viewport *viewport
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;

	if (	viewport->x != renderer->viewport.x ||
		viewport->y != renderer->viewport.y ||
		viewport->w != renderer->viewport.w ||
		viewport->h != renderer->viewport.h ||
		viewport->minDepth != renderer->viewport.minDepth ||
		viewport->maxDepth != renderer->viewport.maxDepth	)
	{
		renderer->viewport = *viewport;
		VULKAN_INTERNAL_SetViewportCommand(renderer);
	}
}

static void VULKAN_SetScissorRect(
	FNA3D_Renderer *driverData,
	FNA3D_Rect *scissor
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;

	if (	scissor->x != renderer->scissorRect.x ||
		scissor->y != renderer->scissorRect.y ||
		scissor->w != renderer->scissorRect.w ||
		scissor->h != renderer->scissorRect.h	)
	{
		renderer->scissorRect = *scissor;
		VULKAN_INTERNAL_SetScissorRectCommand(renderer);
	}
}

static void VULKAN_GetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	SDL_memcpy(
		blendFactor,
		&renderer->blendState.blendFactor,
		sizeof(FNA3D_Color)
	);
}

static void VULKAN_SetBlendFactor(
	FNA3D_Renderer *driverData,
	FNA3D_Color *blendFactor
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	const float blendConstants[] =
	{
		blendFactor->r,
		blendFactor->g,
		blendFactor->b,
		blendFactor->a
	};

	if (	blendFactor->r != renderer->blendState.blendFactor.r ||
		blendFactor->g != renderer->blendState.blendFactor.g ||
		blendFactor->b != renderer->blendState.blendFactor.b ||
		blendFactor->a != renderer->blendState.blendFactor.a	)
	{
		renderer->blendState.blendFactor = *blendFactor;
		renderer->needNewPipeline = 1;

		RECORD_CMD(renderer->vkCmdSetBlendConstants(
			renderer->currentCommandBuffer,
			blendConstants
		));
	}
}

static int32_t VULKAN_GetMultiSampleMask(FNA3D_Renderer *driverData)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	return renderer->multiSampleMask[0];
}

static void VULKAN_SetMultiSampleMask(FNA3D_Renderer *driverData, int32_t mask)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	if (renderer->debugMode && renderer->multiSampleCount> 32) {
		FNA3D_LogWarn(
			"Using a 32-bit multisample mask for a 64-sample rasterizer."
			" Last 32 bits of the mask will all be 1."
		);
	}
	if (renderer->multiSampleMask[0] != mask)
	{
		renderer->multiSampleMask[0] = mask;
		renderer->needNewPipeline = 1;
	}
}

static int32_t VULKAN_GetReferenceStencil(FNA3D_Renderer *driverData)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	return renderer->stencilRef;
}

static void VULKAN_SetReferenceStencil(FNA3D_Renderer *driverData, int32_t ref)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	if (renderer->stencilRef != ref)
	{
		renderer->stencilRef = ref;
		VULKAN_INTERNAL_SetStencilReferenceValueCommand(renderer);
	}
}

/* Immutable Render States */

static void VULKAN_SetBlendState(
	FNA3D_Renderer *driverData,
	FNA3D_BlendState *blendState
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;

	VULKAN_SetBlendFactor(
		driverData,
		&blendState->blendFactor
	);

	VULKAN_SetMultiSampleMask(
		driverData,
		blendState->multiSampleMask
	);

	if (SDL_memcmp(&renderer->blendState, blendState, sizeof(FNA3D_BlendState)) != 0)
	{
		SDL_memcpy(&renderer->blendState, blendState, sizeof(FNA3D_BlendState));
		renderer->needNewPipeline = 1;
	}
}

static void VULKAN_SetDepthStencilState(
	FNA3D_Renderer *driverData,
	FNA3D_DepthStencilState *depthStencilState
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;

	/* TODO: Arrange these checks in an optimized priority */
	if (	renderer->depthStencilState.depthBufferEnable != depthStencilState->depthBufferEnable ||
		renderer->depthStencilState.depthBufferWriteEnable != depthStencilState->depthBufferWriteEnable ||
		renderer->depthStencilState.depthBufferFunction != depthStencilState->depthBufferFunction ||
		renderer->depthStencilState.stencilEnable != depthStencilState->stencilEnable ||
		renderer->depthStencilState.stencilMask != depthStencilState->stencilMask ||
		renderer->depthStencilState.stencilWriteMask != depthStencilState->stencilWriteMask ||
		renderer->depthStencilState.twoSidedStencilMode != depthStencilState->twoSidedStencilMode ||
		renderer->depthStencilState.stencilFail != depthStencilState->stencilFail ||
		renderer->depthStencilState.stencilDepthBufferFail != depthStencilState->stencilDepthBufferFail ||
		renderer->depthStencilState.stencilPass != depthStencilState->stencilPass ||
		renderer->depthStencilState.stencilFunction != depthStencilState->stencilFunction ||
		renderer->depthStencilState.ccwStencilFail != depthStencilState->ccwStencilFail ||
		renderer->depthStencilState.ccwStencilDepthBufferFail != depthStencilState->ccwStencilDepthBufferFail ||
		renderer->depthStencilState.ccwStencilPass != depthStencilState->ccwStencilPass ||
		renderer->depthStencilState.ccwStencilFunction != depthStencilState->ccwStencilFunction ||
		renderer->depthStencilState.referenceStencil != depthStencilState->referenceStencil	)
	{
		renderer->needNewPipeline = 1;

		SDL_memcpy(
			&renderer->depthStencilState,
			depthStencilState,
			sizeof(FNA3D_DepthStencilState)
		);
	}

	/* Dynamic state */
	VULKAN_SetReferenceStencil(
		driverData,
		depthStencilState->referenceStencil
	);
}

static void VULKAN_ApplyRasterizerState(
	FNA3D_Renderer *driverData,
	FNA3D_RasterizerState *rasterizerState
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	float realDepthBias;

	if (rasterizerState->scissorTestEnable != renderer->rasterizerState.scissorTestEnable)
	{
		renderer->rasterizerState.scissorTestEnable = rasterizerState->scissorTestEnable;
		VULKAN_INTERNAL_SetScissorRectCommand(renderer);
		renderer->needNewPipeline = 1;
	}

	realDepthBias = rasterizerState->depthBias * XNAToVK_DepthBiasScale(
		XNAToVK_DepthFormat(
			renderer,
			renderer->currentDepthFormat
		)
	);

	if (	realDepthBias != renderer->rasterizerState.depthBias ||
		rasterizerState->slopeScaleDepthBias != renderer->rasterizerState.slopeScaleDepthBias	)
	{
		renderer->rasterizerState.depthBias = realDepthBias;
		renderer->rasterizerState.slopeScaleDepthBias = rasterizerState->slopeScaleDepthBias;
		VULKAN_INTERNAL_SetDepthBiasCommand(renderer);
		renderer->needNewPipeline = 1;
	}

	if (	rasterizerState->cullMode != renderer->rasterizerState.cullMode ||
		rasterizerState->fillMode != renderer->rasterizerState.fillMode ||
		rasterizerState->multiSampleAntiAlias != renderer->rasterizerState.multiSampleAntiAlias	)
	{
		renderer->rasterizerState.cullMode = rasterizerState->cullMode;
		renderer->rasterizerState.fillMode = rasterizerState->fillMode;
		renderer->rasterizerState.multiSampleAntiAlias = rasterizerState->multiSampleAntiAlias;
		renderer->needNewPipeline = 1;
	}
}

static void VULKAN_VerifySampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *vulkanTexture = (VulkanTexture*) texture;
	VkSampler vkSamplerState;

	if (texture == NULL)
	{
		if (renderer->textures[index] != &NullTexture)
		{
			renderer->textures[index] = &NullTexture;
			renderer->textureNeedsUpdate[index] = 1;
		}

		if (renderer->samplers[index] == VK_NULL_HANDLE)
		{
			vkSamplerState = VULKAN_INTERNAL_FetchSamplerState(
				renderer,
				sampler,
				0
			);

			renderer->samplers[index] = vkSamplerState;
			renderer->samplerNeedsUpdate[index] = 1;
		}

		return;
	}

	if (!vulkanTexture->external)
	{
		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			RESOURCE_ACCESS_FRAGMENT_SHADER_READ_SAMPLED_IMAGE,
			VK_IMAGE_ASPECT_COLOR_BIT,
			0,
			vulkanTexture->layerCount,
			0,
			vulkanTexture->levelCount,
			0,
			vulkanTexture->image,
			&vulkanTexture->resourceAccessType
		);
	}

	if (vulkanTexture != renderer->textures[index])
	{
		renderer->textures[index] = vulkanTexture;
		renderer->textureNeedsUpdate[index] = 1;

		if (index >= MAX_TEXTURE_SAMPLERS)
		{
			renderer->vertexSamplerDescriptorSetDataNeedsUpdate = 1;
		}
		else
		{
			renderer->fragSamplerDescriptorSetDataNeedsUpdate = 1;
		}
	}

	vkSamplerState = VULKAN_INTERNAL_FetchSamplerState(
		renderer,
		sampler,
		vulkanTexture->levelCount
	);

	if (vkSamplerState != renderer->samplers[index])
	{
		renderer->samplers[index] = vkSamplerState;
		renderer->samplerNeedsUpdate[index] = 1;

		if (index >= MAX_TEXTURE_SAMPLERS)
		{
			renderer->vertexSamplerDescriptorSetDataNeedsUpdate = 1;
		}
		else
		{
			renderer->fragSamplerDescriptorSetDataNeedsUpdate = 1;
		}
	}
}

static void VULKAN_VerifyVertexSampler(
	FNA3D_Renderer *driverData,
	int32_t index,
	FNA3D_Texture *texture,
	FNA3D_SamplerState *sampler
) {
	VULKAN_VerifySampler(
		driverData,
		MAX_TEXTURE_SAMPLERS + index,
		texture,
		sampler
	);
}

static void VULKAN_ApplyVertexBufferBindings(
	FNA3D_Renderer *driverData,
	FNA3D_VertexBufferBinding *bindings,
	int32_t numBindings,
	uint8_t bindingsUpdated,
	int32_t baseVertex
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	MOJOSHADER_vkShader *vertexShader, *blah;
	int32_t i, bindingsIndex;
	uint32_t hash;
	void* bindingsResult;
	FNA3D_VertexBufferBinding *src, *dst;
	VulkanBuffer *vertexBuffer;
	VulkanSubBuffer *subbuf;
	VkDeviceSize offset;

	/* Check VertexBufferBindings */
	MOJOSHADER_vkGetBoundShaders(&vertexShader, &blah);
	bindingsResult = PackedVertexBufferBindingsArray_Fetch(
		renderer->vertexBufferBindingsCache,
		bindings,
		numBindings,
		vertexShader,
		&bindingsIndex,
		&hash
	);
	if (bindingsResult == NULL)
	{
		PackedVertexBufferBindingsArray_Insert(
			&renderer->vertexBufferBindingsCache,
			bindings,
			numBindings,
			vertexShader,
			(void*) 69420
		);
	}

	if (bindingsUpdated)
	{
		renderer->numVertexBindings = numBindings;
		for (i = 0; i < renderer->numVertexBindings; i += 1)
		{
			src = &bindings[i];
			dst = &renderer->vertexBindings[i];
			dst->vertexBuffer = src->vertexBuffer;
			dst->vertexOffset = src->vertexOffset;
			dst->instanceFrequency = src->instanceFrequency;
			dst->vertexDeclaration.vertexStride = src->vertexDeclaration.vertexStride;
			dst->vertexDeclaration.elementCount = src->vertexDeclaration.elementCount;
			SDL_memcpy(
				dst->vertexDeclaration.elements,
				src->vertexDeclaration.elements,
				sizeof(FNA3D_VertexElement) * src->vertexDeclaration.elementCount
			);
		}
	}

	if (bindingsIndex != renderer->currentVertexBufferBindingsIndex)
	{
		renderer->currentVertexBufferBindingsIndex = bindingsIndex;
		renderer->needNewPipeline = 1;
	}

	for (i = 0; i < numBindings; i += 1)
	{
		vertexBuffer = (VulkanBuffer*) bindings[i].vertexBuffer;
		if (vertexBuffer == NULL)
		{
			continue;
		}

		subbuf = vertexBuffer->subBuffers[
			vertexBuffer->currentSubBufferIndex
		];

		offset =
			bindings[i].vertexOffset *
			bindings[i].vertexDeclaration.vertexStride
		;

		renderer->boundVertexBuffers[i] = subbuf->buffer;
		renderer->boundVertexBufferOffsets[i] = offset;

		VULKAN_INTERNAL_BufferMemoryBarrier(
			renderer,
			RESOURCE_ACCESS_VERTEX_BUFFER,
			subbuf->buffer,
			&subbuf->resourceAccessType
		);

		VULKAN_INTERNAL_MarkAsBound(renderer, vertexBuffer);
	}
}

/* Render Targets */

static void VULKAN_SetRenderTargets(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *renderTargets,
	int32_t numRenderTargets,
	FNA3D_Renderbuffer *depthStencilBuffer,
	FNA3D_DepthFormat depthFormat,
	uint8_t preserveTargetContents
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanColorBuffer *cb;
	VulkanTexture *tex;
	int32_t i;

	/* Perform any pending clears before switching render targets */
	if (	renderer->shouldClearColorOnBeginPass ||
		renderer->shouldClearDepthOnBeginPass ||
		renderer->shouldClearStencilOnBeginPass	)
	{
		VULKAN_INTERNAL_BeginRenderPass(renderer);
	}

	renderer->preserveTargetContents = preserveTargetContents;

	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		renderer->colorAttachments[i] = NULL;
		renderer->colorMultiSampleAttachments[i] = NULL;
	}
	renderer->depthStencilAttachment = NULL;
	renderer->multiSampleCount = renderer->fauxBackbufferMultiSampleCount;

	if (numRenderTargets <= 0)
	{
		renderer->colorAttachments[0] = renderer->fauxBackbufferColor.handle;
		renderer->attachmentCubeFaces[0] = (FNA3D_CubeMapFace) 0;
		renderer->colorAttachmentCount = 1;

		if (renderer->fauxBackbufferMultiSampleCount > 1)
		{
			renderer->colorMultiSampleAttachments[0] =
				renderer->fauxBackbufferMultiSampleColor;
		}
		renderer->depthStencilAttachment =
			renderer->fauxBackbufferDepthStencil.handle;

		renderer->renderTargetBound = 0;
	}
	else
	{
		for (i = 0; i < numRenderTargets; i += 1)
		{
			renderer->attachmentCubeFaces[i] = (
				renderTargets[i].type == FNA3D_RENDERTARGET_TYPE_CUBE ?
					renderTargets[i].cube.face :
					(FNA3D_CubeMapFace) 0
			);

			if (renderTargets[i].colorBuffer != NULL)
			{
				cb = ((VulkanRenderbuffer*) renderTargets[i].colorBuffer)->colorBuffer;
				renderer->colorAttachments[i] = cb->handle;
				renderer->multiSampleCount = cb->multiSampleCount;

				if (cb->multiSampleCount > 0)
				{
					renderer->colorMultiSampleAttachments[i] = cb->multiSampleTexture;
				}
			}
			else
			{
				tex = (VulkanTexture*) renderTargets[i].texture;
				renderer->colorAttachments[i] = tex;
				renderer->multiSampleCount = 0;
			}
		}

		renderer->colorAttachmentCount = numRenderTargets;

		/* update depth stencil buffer */

		if (depthStencilBuffer != NULL)
		{
			renderer->depthStencilAttachment = ((VulkanRenderbuffer*) depthStencilBuffer)->depthBuffer->handle;
			renderer->currentDepthFormat = depthFormat;
		}
		else
		{
			renderer->depthStencilAttachment = NULL;
		}

		renderer->renderTargetBound = 1;
	}

	renderer->needNewRenderPass = 1;
}

static void VULKAN_ResolveTarget(
	FNA3D_Renderer *driverData,
	FNA3D_RenderTargetBinding *target
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *vulkanTexture = (VulkanTexture*) target->texture;
	int32_t layerCount = (target->type == FNA3D_RENDERTARGET_TYPE_CUBE) ? 6 : 1;
	int32_t level;
	VulkanResourceAccessType origAccessType;
	VkImageBlit blit;

	/* The target is resolved during the render pass. */

	/* If the target has mipmaps, regenerate them now */
	if (target->levelCount > 1)
	{
		VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);

		origAccessType = vulkanTexture->resourceAccessType;

		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			RESOURCE_ACCESS_TRANSFER_READ,
			VK_IMAGE_ASPECT_COLOR_BIT,
			0,
			layerCount,
			0,
			1,
			0,
			vulkanTexture->image,
			&vulkanTexture->resourceAccessType
		);

		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			RESOURCE_ACCESS_TRANSFER_WRITE,
			VK_IMAGE_ASPECT_COLOR_BIT,
			0,
			layerCount,
			1,
			target->levelCount - 1,
			1,
			vulkanTexture->image,
			&vulkanTexture->resourceAccessType
		);

		for (level = 1; level < target->levelCount; level += 1)
		{
			blit.srcOffsets[0].x = 0;
			blit.srcOffsets[0].y = 0;
			blit.srcOffsets[0].z = 0;

			blit.srcOffsets[1].x = vulkanTexture->dimensions.width;
			blit.srcOffsets[1].y = vulkanTexture->dimensions.height;
			blit.srcOffsets[1].z = 1;

			blit.dstOffsets[0].x = 0;
			blit.dstOffsets[0].y = 0;
			blit.dstOffsets[0].z = 0;

			blit.dstOffsets[1].x = vulkanTexture->dimensions.width >> level;
			blit.dstOffsets[1].y = vulkanTexture->dimensions.height >> level;
			blit.dstOffsets[1].z = 1;

			blit.srcSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
			blit.srcSubresource.baseArrayLayer = 0;
			blit.srcSubresource.layerCount = layerCount;
			blit.srcSubresource.mipLevel = 0;

			blit.dstSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
			blit.dstSubresource.baseArrayLayer = 0;
			blit.dstSubresource.layerCount = layerCount;
			blit.dstSubresource.mipLevel = level;

			RECORD_CMD(renderer->vkCmdBlitImage(
				renderer->currentCommandBuffer,
				vulkanTexture->image,
				VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
				vulkanTexture->image,
				VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
				1,
				&blit,
				VK_FILTER_LINEAR
			));
		}

		/* Transition level >= 1 back to the original access type */
		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			origAccessType,
			VK_IMAGE_ASPECT_COLOR_BIT,
			0,
			layerCount,
			1,
			target->levelCount - 1,
			0,
			vulkanTexture->image,
			&vulkanTexture->resourceAccessType
		);

		/* The 0th mip requires a little access type switcheroo... */
		vulkanTexture->resourceAccessType = RESOURCE_ACCESS_TRANSFER_READ;
		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			origAccessType,
			VK_IMAGE_ASPECT_COLOR_BIT,
			0,
			layerCount,
			0,
			1,
			0,
			vulkanTexture->image,
			&vulkanTexture->resourceAccessType
		);
	}
}

/* Backbuffer Functions */

static void VULKAN_ResetBackbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_PresentationParameters *presentationParameters
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	renderer->presentInterval = presentationParameters->presentationInterval;
	renderer->deviceWindowHandle = presentationParameters->deviceWindowHandle;

	VULKAN_INTERNAL_RecreateSwapchain(renderer, 1);
	VULKAN_INTERNAL_DestroyFauxBackbuffer(renderer);
	VULKAN_INTERNAL_CreateFauxBackbuffer(
		renderer,
		presentationParameters
	);
}

static void VULKAN_ReadBackbuffer(
	FNA3D_Renderer *driverData,
	int32_t x,
	int32_t y,
	int32_t w,
	int32_t h,
	void* data,
	int32_t dataLength
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;

	VULKAN_GetTextureData2D(
		driverData,
		(FNA3D_Texture*) renderer->fauxBackbufferColor.handle,
		x,
		y,
		w,
		h,
		0,
		data,
		dataLength
	);
}

static void VULKAN_GetBackbufferSize(
	FNA3D_Renderer *driverData,
	int32_t *w,
	int32_t *h
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	*w = renderer->fauxBackbufferWidth;
	*h = renderer->fauxBackbufferHeight;
}

static FNA3D_SurfaceFormat VULKAN_GetBackbufferSurfaceFormat(
	FNA3D_Renderer *driverData
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	return renderer->fauxBackbufferColor.handle->colorFormat;
}

static FNA3D_DepthFormat VULKAN_GetBackbufferDepthFormat(
	FNA3D_Renderer *driverData
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	if (renderer->fauxBackbufferDepthStencil.handle == NULL)
	{
		return FNA3D_DEPTHFORMAT_NONE;
	}
	return renderer->fauxBackbufferDepthStencil.handle->depthStencilFormat;
}

static int32_t VULKAN_GetBackbufferMultiSampleCount(FNA3D_Renderer *driverData)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	return renderer->fauxBackbufferMultiSampleCount;
}

/* Textures */

static FNA3D_Texture* VULKAN_CreateTexture2D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *result;
	uint32_t usageFlags = (
		VK_IMAGE_USAGE_SAMPLED_BIT |
		VK_IMAGE_USAGE_TRANSFER_DST_BIT |
		VK_IMAGE_USAGE_TRANSFER_SRC_BIT
	);

	result = (VulkanTexture*) SDL_malloc(sizeof(VulkanTexture));

	if (isRenderTarget)
	{
		usageFlags |= VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
	}

	VULKAN_INTERNAL_CreateTexture(
		renderer,
		width,
		height,
		1,
		0,
		isRenderTarget,
		VK_SAMPLE_COUNT_1_BIT,
		levelCount,
		XNAToVK_SurfaceFormat[format],
		XNAToVK_SurfaceSwizzle[format],
		VK_IMAGE_ASPECT_COLOR_BIT,
		VK_IMAGE_TYPE_2D,
		usageFlags,
		result
	);
	result->colorFormat = format;

	return (FNA3D_Texture*) result;
}

static FNA3D_Texture* VULKAN_CreateTexture3D(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t width,
	int32_t height,
	int32_t depth,
	int32_t levelCount
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *result;
	uint32_t usageFlags = (
		VK_IMAGE_USAGE_SAMPLED_BIT |
		VK_IMAGE_USAGE_TRANSFER_DST_BIT |
		VK_IMAGE_USAGE_TRANSFER_SRC_BIT
	);

	result = (VulkanTexture*) SDL_malloc(sizeof(VulkanTexture));

	VULKAN_INTERNAL_CreateTexture(
		renderer,
		width,
		height,
		depth,
		0,
		0,
		VK_SAMPLE_COUNT_1_BIT,
		levelCount,
		XNAToVK_SurfaceFormat[format],
		XNAToVK_SurfaceSwizzle[format],
		VK_IMAGE_ASPECT_COLOR_BIT,
		VK_IMAGE_TYPE_3D,
		usageFlags,
		result
	);
	result->colorFormat = format;

	return (FNA3D_Texture*) result;
}

static FNA3D_Texture* VULKAN_CreateTextureCube(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t size,
	int32_t levelCount,
	uint8_t isRenderTarget
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *result;
	uint32_t usageFlags = (
		VK_IMAGE_USAGE_SAMPLED_BIT |
		VK_IMAGE_USAGE_TRANSFER_DST_BIT |
		VK_IMAGE_USAGE_TRANSFER_SRC_BIT
	);

	result = (VulkanTexture*) SDL_malloc(sizeof(VulkanTexture));

	if (isRenderTarget)
	{
		usageFlags |= VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
	}

	VULKAN_INTERNAL_CreateTexture(
		renderer,
		size,
		size,
		1,
		1,
		isRenderTarget,
		VK_SAMPLE_COUNT_1_BIT,
		levelCount,
		XNAToVK_SurfaceFormat[format],
		XNAToVK_SurfaceSwizzle[format],
		VK_IMAGE_ASPECT_COLOR_BIT,
		VK_IMAGE_TYPE_2D,
		usageFlags,
		result
	);
	result->colorFormat = format;

	return (FNA3D_Texture*) result;
}

static void VULKAN_AddDisposeTexture(
	FNA3D_Renderer *driverData,
	FNA3D_Texture *texture
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *vulkanTexture = (VulkanTexture*) texture;
	uint32_t i;

	for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
	{
		if (renderer->colorAttachments[i] != NULL)
		{
			if (vulkanTexture->view == renderer->colorAttachments[i]->rtViews[renderer->attachmentCubeFaces[i]])
			{
				renderer->colorAttachments[i] = NULL;
			}
		}
	}

	for (i = 0; i < TEXTURE_COUNT; i += 1)
	{
		if (vulkanTexture == renderer->textures[i])
		{
			renderer->textures[i] = &NullTexture;
			renderer->textureNeedsUpdate[i] = 1;
		}
	}

	/* Queue texture for destruction */
	SDL_LockMutex(renderer->disposeLock);
	if (renderer->texturesToDestroyCount + 1 >= renderer->texturesToDestroyCapacity)
	{
		renderer->texturesToDestroyCapacity *= 2;

		renderer->texturesToDestroy = SDL_realloc(
			renderer->texturesToDestroy,
			sizeof(VulkanTexture*) * renderer->texturesToDestroyCapacity
		);
	}
	renderer->texturesToDestroy[renderer->texturesToDestroyCount] = vulkanTexture;
	renderer->texturesToDestroyCount += 1;
	SDL_UnlockMutex(renderer->disposeLock);
}

static void VULKAN_INTERNAL_SetTextureData(
	VulkanRenderer *renderer,
	VulkanTexture *texture,
	int32_t x,
	int32_t y,
	int32_t z,
	int32_t w,
	int32_t h,
	int32_t d,
	int32_t level,
	int32_t layer,
	void *data,
	int32_t dataLength
) {
	VkBufferImageCopy imageCopy;
	int32_t uploadLength = BytesPerImage(w, h, texture->colorFormat) * d;
	int32_t copyLength = SDL_min(dataLength, uploadLength);
	VulkanSubBuffer *stagingSubBuffer;
	VkDeviceSize offset;
	int32_t bufferRowLength = w;
	int32_t bufferImageHeight = h;

	if (dataLength > uploadLength)
	{
		FNA3D_LogWarn(
			"dataLength %i too long for texture upload, w: %i, h: %i, max upload length: %i",
			dataLength,
			w,
			h,
			uploadLength
		);
	}

	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);

	SDL_LockMutex(renderer->passLock);
	SDL_LockMutex(renderer->stagingLock);

	VULKAN_INTERNAL_CopyToStagingBuffer(
		renderer,
		data,
		uploadLength,
		copyLength,
		&stagingSubBuffer,
		&offset,
		texture->colorFormat
	);

	VULKAN_INTERNAL_BufferMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_READ,
		stagingSubBuffer->buffer,
		&stagingSubBuffer->resourceAccessType
	);

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_WRITE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		texture->layerCount,
		0,
		texture->levelCount,
		0,
		texture->image,
		&texture->resourceAccessType
	);

	/* DXT texture buffers must be at least 4x4 */
	if (	texture->colorFormat == FNA3D_SURFACEFORMAT_DXT1 ||
		texture->colorFormat == FNA3D_SURFACEFORMAT_DXT3 ||
		texture->colorFormat == FNA3D_SURFACEFORMAT_DXT5	)
	{
		bufferRowLength = SDL_max(4, w);
		bufferImageHeight = SDL_max(4, h);
	}

	imageCopy.imageExtent.width = w;
	imageCopy.imageExtent.height = h;
	imageCopy.imageExtent.depth = d;
	imageCopy.imageOffset.x = x;
	imageCopy.imageOffset.y = y;
	imageCopy.imageOffset.z = z;
	imageCopy.imageSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
	imageCopy.imageSubresource.baseArrayLayer = layer;
	imageCopy.imageSubresource.layerCount = 1;
	imageCopy.imageSubresource.mipLevel = level;
	imageCopy.bufferOffset = offset;
	imageCopy.bufferRowLength = bufferRowLength;
	imageCopy.bufferImageHeight = bufferImageHeight;

	RECORD_CMD(renderer->vkCmdCopyBufferToImage(
		renderer->currentCommandBuffer,
		stagingSubBuffer->buffer,
		texture->image,
		AccessMap[texture->resourceAccessType].imageLayout,
		1,
		&imageCopy
	));

	SDL_UnlockMutex(renderer->stagingLock);
	SDL_UnlockMutex(renderer->passLock);
}

static void VULKAN_SetTextureData2D(
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
	VULKAN_INTERNAL_SetTextureData(
		(VulkanRenderer*) driverData,
		(VulkanTexture*) texture,
		x,
		y,
		0,
		w,
		h,
		1,
		level,
		0,
		data,
		dataLength
	);
}

static void VULKAN_SetTextureData3D(
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
	VULKAN_INTERNAL_SetTextureData(
		(VulkanRenderer*) driverData,
		(VulkanTexture*) texture,
		x,
		y,
		z,
		w,
		h,
		d,
		level,
		0,
		data,
		dataLength
	);
}

static void VULKAN_SetTextureDataCube(
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
	VULKAN_INTERNAL_SetTextureData(
		(VulkanRenderer*) driverData,
		(VulkanTexture*) texture,
		x,
		y,
		0,
		w,
		h,
		1,
		level,
		cubeMapFace,
		data,
		dataLength
	);
}

static void VULKAN_SetTextureDataYUV(
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
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *tex;
	VulkanSubBuffer *stagingSubBuffer;
	int32_t yDataLength = BytesPerImage(yWidth, yHeight, FNA3D_SURFACEFORMAT_ALPHA8);
	int32_t uvDataLength = BytesPerImage(uvWidth, uvHeight, FNA3D_SURFACEFORMAT_ALPHA8);
	int32_t uploadLength = yDataLength + uvDataLength * 2;
	int32_t copyLength = SDL_min(dataLength, uploadLength);
	VkBufferImageCopy imageCopy;
	VkDeviceSize offset;

	if (dataLength > uploadLength)
	{
		FNA3D_LogWarn(
			"dataLength %i too long for texture upload, max upload length: %i",
			dataLength,
			uploadLength
		);
	}

	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);

	SDL_LockMutex(renderer->passLock);
	SDL_LockMutex(renderer->stagingLock);

	VULKAN_INTERNAL_CopyToStagingBuffer(
		renderer,
		data,
		uploadLength,
		copyLength,
		&stagingSubBuffer,
		&offset,
		FNA3D_SURFACEFORMAT_ALPHA8
	);

	VULKAN_INTERNAL_BufferMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_READ,
		stagingSubBuffer->buffer,
		&stagingSubBuffer->resourceAccessType
	);

	/* Initialize values that are the same for Y, U, and V */

	imageCopy.imageExtent.depth = 1;
	imageCopy.imageOffset.x = 0;
	imageCopy.imageOffset.y = 0;
	imageCopy.imageOffset.z = 0;
	imageCopy.imageSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
	imageCopy.imageSubresource.baseArrayLayer = 0;
	imageCopy.imageSubresource.layerCount = 1;
	imageCopy.imageSubresource.mipLevel = 0;

	/* Y */

	tex = (VulkanTexture*) y;

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_WRITE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		tex->layerCount,
		0,
		tex->levelCount,
		0,
		tex->image,
		&tex->resourceAccessType
	);

	imageCopy.imageExtent.width = yWidth;
	imageCopy.imageExtent.height = yHeight;
	imageCopy.bufferOffset = offset;
	imageCopy.bufferRowLength = yWidth;
	imageCopy.bufferImageHeight = yHeight;

	RECORD_CMD(renderer->vkCmdCopyBufferToImage(
		renderer->currentCommandBuffer,
		stagingSubBuffer->buffer,
		tex->image,
		AccessMap[tex->resourceAccessType].imageLayout,
		1,
		&imageCopy
	));

	/* These apply to both U and V */

	imageCopy.imageExtent.width = uvWidth;
	imageCopy.imageExtent.height = uvHeight;
	imageCopy.bufferRowLength = uvWidth;
	imageCopy.bufferImageHeight = uvHeight;

	/* U */

	imageCopy.bufferOffset = offset + yDataLength;

	tex = (VulkanTexture*) u;

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_WRITE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		tex->layerCount,
		0,
		tex->levelCount,
		0,
		tex->image,
		&tex->resourceAccessType
	);

	RECORD_CMD(renderer->vkCmdCopyBufferToImage(
		renderer->currentCommandBuffer,
		stagingSubBuffer->buffer,
		tex->image,
		AccessMap[tex->resourceAccessType].imageLayout,
		1,
		&imageCopy
	));

	/* V */

	imageCopy.bufferOffset = offset + yDataLength + uvDataLength;

	tex = (VulkanTexture*) v;

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_WRITE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		tex->layerCount,
		0,
		tex->levelCount,
		0,
		tex->image,
		&tex->resourceAccessType
	);

	RECORD_CMD(renderer->vkCmdCopyBufferToImage(
		renderer->currentCommandBuffer,
		stagingSubBuffer->buffer,
		tex->image,
		AccessMap[tex->resourceAccessType].imageLayout,
		1,
		&imageCopy
	));

	SDL_UnlockMutex(renderer->stagingLock);
	SDL_UnlockMutex(renderer->passLock);
}

static void VULKAN_GetTextureData2D(
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
	VULKAN_INTERNAL_GetTextureData(
		driverData,
		texture,
		x,
		y,
		w,
		h,
		level,
		0,
		data,
		dataLength
	);
}

static void VULKAN_GetTextureData3D(
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
	FNA3D_LogError(
		"GetTextureData3D is unsupported!"
	);
}

static void VULKAN_GetTextureDataCube(
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
	VULKAN_INTERNAL_GetTextureData(
		driverData,
		texture,
		x,
		y,
		w,
		h,
		level,
		cubeMapFace,
		data,
		dataLength
	);
}

/* Renderbuffers */

static FNA3D_Renderbuffer* VULKAN_GenColorRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount,
	FNA3D_Texture *texture
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanTexture *vlkTexture = (VulkanTexture*) texture;
	VulkanRenderbuffer *renderbuffer;

	/* Create and return the renderbuffer */
	renderbuffer = (VulkanRenderbuffer*) SDL_malloc(sizeof(VulkanRenderbuffer));
	renderbuffer->depthBuffer = NULL;
	renderbuffer->colorBuffer = (VulkanColorBuffer*) SDL_malloc(
		sizeof(VulkanColorBuffer)
	);
	renderbuffer->colorBuffer->handle = vlkTexture;
	renderbuffer->colorBuffer->multiSampleTexture = NULL;
	renderbuffer->colorBuffer->multiSampleCount = 0;

	if (multiSampleCount > 1)
	{
		renderbuffer->colorBuffer->multiSampleTexture =
			(VulkanTexture*) SDL_malloc(sizeof(VulkanTexture));
		VULKAN_INTERNAL_CreateTexture(
			renderer,
			width,
			height,
			1,
			0,
			1,
			XNAToVK_SampleCount(multiSampleCount),
			1,
			XNAToVK_SurfaceFormat[format],
			XNAToVK_SurfaceSwizzle[format],
			VK_IMAGE_ASPECT_COLOR_BIT,
			VK_IMAGE_TYPE_2D,
			VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT,
			renderbuffer->colorBuffer->multiSampleTexture
		);
		renderbuffer->colorBuffer->multiSampleTexture->colorFormat = format;

		renderbuffer->colorBuffer->multiSampleCount = multiSampleCount;

		VULKAN_INTERNAL_ImageMemoryBarrier(
			renderer,
			RESOURCE_ACCESS_COLOR_ATTACHMENT_READ_WRITE,
			VK_IMAGE_ASPECT_COLOR_BIT,
			0,
			renderbuffer->colorBuffer->multiSampleTexture->layerCount,
			0,
			renderbuffer->colorBuffer->multiSampleTexture->levelCount,
			0,
			renderbuffer->colorBuffer->multiSampleTexture->image,
			&renderbuffer->colorBuffer->multiSampleTexture->resourceAccessType
		);
	}

	return (FNA3D_Renderbuffer*) renderbuffer;
}

static FNA3D_Renderbuffer* VULKAN_GenDepthStencilRenderbuffer(
	FNA3D_Renderer *driverData,
	int32_t width,
	int32_t height,
	FNA3D_DepthFormat format,
	int32_t multiSampleCount
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanRenderbuffer *renderbuffer;
	VkImageAspectFlags depthAspectFlags = VK_IMAGE_ASPECT_DEPTH_BIT;
	VkFormat depthFormat = XNAToVK_DepthFormat(renderer, format);

	if (DepthFormatContainsStencil(depthFormat))
	{
		depthAspectFlags |= VK_IMAGE_ASPECT_STENCIL_BIT;
	}

	renderbuffer = (VulkanRenderbuffer*) SDL_malloc(
		sizeof(VulkanRenderbuffer)
	);
	renderbuffer->colorBuffer = NULL;
	renderbuffer->depthBuffer = (VulkanDepthStencilBuffer*) SDL_malloc(
		sizeof(VulkanDepthStencilBuffer)
	);

	renderbuffer->depthBuffer->handle = (VulkanTexture*) SDL_malloc(
		sizeof(VulkanTexture)
	);
	if (!VULKAN_INTERNAL_CreateTexture(
		renderer,
		width,
		height,
		1,
		0,
		1,
		XNAToVK_SampleCount(multiSampleCount),
		1,
		depthFormat,
		RGBA_SWIZZLE,
		depthAspectFlags,
		VK_IMAGE_TYPE_2D,
		VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT,
		renderbuffer->depthBuffer->handle
	)) {
		FNA3D_LogError("Failed to create depth stencil image");
		return NULL;
	}
	renderbuffer->depthBuffer->handle->depthStencilFormat = format;

	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_WRITE,
		depthAspectFlags,
		0,
		renderbuffer->depthBuffer->handle->layerCount,
		0,
		renderbuffer->depthBuffer->handle->levelCount,
		0,
		renderbuffer->depthBuffer->handle->image,
		&renderbuffer->depthBuffer->handle->resourceAccessType
	);

	return (FNA3D_Renderbuffer*) renderbuffer;
}

static void VULKAN_AddDisposeRenderbuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Renderbuffer *renderbuffer
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanRenderbuffer *vlkRenderBuffer = (VulkanRenderbuffer*) renderbuffer;
	uint8_t isDepthStencil = (vlkRenderBuffer->colorBuffer == NULL);
	uint32_t i;

	if (isDepthStencil)
	{
		if (renderer->depthStencilAttachment == vlkRenderBuffer->depthBuffer->handle)
		{
			renderer->depthStencilAttachment = NULL;
		}
	}
	else
	{
		/* Iterate through color attachments */
		for (i = 0; i < MAX_RENDERTARGET_BINDINGS; i += 1)
		{
			if (renderer->colorAttachments[i] == vlkRenderBuffer->colorBuffer->handle)
			{
				renderer->colorAttachments[i] = NULL;
			}

		}
	}

	SDL_LockMutex(renderer->disposeLock);
	if (renderer->renderbuffersToDestroyCount + 1 >= renderer->renderbuffersToDestroyCapacity)
	{
		renderer->renderbuffersToDestroyCapacity *= 2;

		renderer->renderbuffersToDestroy = SDL_realloc(
			renderer->renderbuffersToDestroy,
			sizeof(VulkanRenderbuffer*) * renderer->renderbuffersToDestroyCapacity
		);
	}

	renderer->renderbuffersToDestroy[renderer->renderbuffersToDestroyCount] = vlkRenderBuffer;
	renderer->renderbuffersToDestroyCount += 1;
	SDL_UnlockMutex(renderer->disposeLock);
}

/* Vertex Buffers */

static FNA3D_Buffer* VULKAN_GenVertexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	return (FNA3D_Buffer*) VULKAN_INTERNAL_CreateBuffer(
		(VulkanRenderer*) driverData,
		sizeInBytes,
		RESOURCE_ACCESS_VERTEX_BUFFER,
		VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,
		0,
		0,
		1
	);
}

static void VULKAN_AddDisposeVertexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	VULKAN_INTERNAL_RemoveBuffer(driverData, buffer);
}

static void VULKAN_SetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride,
	FNA3D_SetDataOptions options
) {
	/* FIXME: use staging buffer for elementSizeInBytes < vertexStride */
	VULKAN_INTERNAL_SetBufferData(
		driverData,
		buffer,
		offsetInBytes,
		data,
		elementCount * vertexStride,
		options
	);
}

static void VULKAN_GetVertexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t elementCount,
	int32_t elementSizeInBytes,
	int32_t vertexStride
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanBuffer *vulkanBuffer = (VulkanBuffer*) buffer;
	VulkanSubBuffer *subbuf = vulkanBuffer->subBuffers[vulkanBuffer->currentSubBufferIndex];
	uint8_t *dataBytes, *cpy, *src, *dst;
	uint8_t useStagingBuffer;
	int32_t i;

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

	VULKAN_INTERNAL_BufferMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_READ,
		subbuf->buffer,
		&subbuf->resourceAccessType
	);

	SDL_memcpy(
		cpy,
		subbuf->usedRegion->allocation->mapPointer + subbuf->usedRegion->resourceOffset + offsetInBytes,
		elementCount * vertexStride
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

	VULKAN_INTERNAL_BufferMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_VERTEX_BUFFER,
		subbuf->buffer,
		&subbuf->resourceAccessType
	);
}

/* Index Buffers */

static FNA3D_Buffer* VULKAN_GenIndexBuffer(
	FNA3D_Renderer *driverData,
	uint8_t dynamic,
	FNA3D_BufferUsage usage,
	int32_t sizeInBytes
) {
	return (FNA3D_Buffer*) VULKAN_INTERNAL_CreateBuffer(
		(VulkanRenderer*) driverData,
		sizeInBytes,
		RESOURCE_ACCESS_INDEX_BUFFER,
		VK_BUFFER_USAGE_INDEX_BUFFER_BIT,
		0,
		0,
		1
	);
}

static void VULKAN_AddDisposeIndexBuffer(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer
) {
	VULKAN_INTERNAL_RemoveBuffer(driverData, buffer);
}

static void VULKAN_SetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength,
	FNA3D_SetDataOptions options
) {
	VULKAN_INTERNAL_SetBufferData(
		driverData,
		buffer,
		offsetInBytes,
		data,
		dataLength,
		options
	);
}

static void VULKAN_GetIndexBufferData(
	FNA3D_Renderer *driverData,
	FNA3D_Buffer *buffer,
	int32_t offsetInBytes,
	void* data,
	int32_t dataLength
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanBuffer *vulkanBuffer = (VulkanBuffer*) buffer;
	VulkanSubBuffer *subbuf = vulkanBuffer->subBuffers[vulkanBuffer->currentSubBufferIndex];

	VULKAN_INTERNAL_BufferMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_TRANSFER_READ,
		subbuf->buffer,
		&subbuf->resourceAccessType
	);

	SDL_memcpy(
		data,
		subbuf->usedRegion->allocation->mapPointer + subbuf->usedRegion->resourceOffset + offsetInBytes,
		dataLength
	);

	VULKAN_INTERNAL_BufferMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_INDEX_BUFFER,
		subbuf->buffer,
		&subbuf->resourceAccessType
	);
}

/* Effects */

static inline void ShaderResourcesHashTable_Remove(
	VulkanRenderer *renderer,
	MOJOSHADER_vkShader *key
) {
	int32_t i;
	uint64_t hashcode = (uint64_t) (size_t) key;
	ShaderResourcesHashArray *arr =
		&renderer->shaderResourcesHashTable.buckets[hashcode % NUM_SHADER_RESOURCES_BUCKETS];
	ShaderResourcesHashMap *element;

	for (i = arr->count - 1; i >= 0; i -= 1)
	{
		element = &arr->elements[i];
		if (element->key == key)
		{
			ShaderResources_Destroy(renderer, element->value);

			SDL_memmove(
				arr->elements + i,
				arr->elements + i + 1,
				sizeof(ShaderResourcesHashMap) * (arr->count - i - 1)
			);

			arr->count -= 1;
		}
	}
}

static void VULKAN_INTERNAL_DeleteShader(void* shader)
{
	MOJOSHADER_vkShader *vkShader = (MOJOSHADER_vkShader*) shader;
	const MOJOSHADER_parseData *pd;
	VulkanRenderer *renderer;
	PipelineHashArray *pipelineHashArray;
	int32_t i, j;

	pd = MOJOSHADER_vkGetShaderParseData(vkShader);
	renderer = (VulkanRenderer*) pd->malloc_data;

	ShaderResourcesHashTable_Remove(renderer, vkShader);

	/* invalidate any pipeline containing shader */
	for (i = 0; i < NUM_PIPELINE_HASH_BUCKETS; i += 1)
	{
		pipelineHashArray = &renderer->pipelineHashTable.buckets[i];
		for (j = pipelineHashArray->count - 1; j >= 0; j -= 1)
		{
			if (	pipelineHashArray->elements[j].key.vertShader == vkShader ||
				pipelineHashArray->elements[j].key.fragShader == vkShader	)
			{
				renderer->vkDestroyPipeline(
					renderer->logicalDevice,
					pipelineHashArray->elements[j].value,
					NULL
				);

				SDL_memmove(
					pipelineHashArray->elements + j,
					pipelineHashArray->elements + j + 1,
					sizeof(PipelineHashMap) * (pipelineHashArray->count - j - 1)
				);

				pipelineHashArray->count -= 1;
			}
		}
	}

	MOJOSHADER_vkDeleteShader(vkShader);
}

static void VULKAN_CreateEffect(
	FNA3D_Renderer *driverData,
	uint8_t *effectCode,
	uint32_t effectCodeLength,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	MOJOSHADER_effectShaderContext shaderBackend;
	VulkanEffect *result;
	int32_t i;

	shaderBackend.compileShader = (MOJOSHADER_compileShaderFunc) MOJOSHADER_vkCompileShader;
	shaderBackend.shaderAddRef = (MOJOSHADER_shaderAddRefFunc) MOJOSHADER_vkShaderAddRef;
	shaderBackend.deleteShader = VULKAN_INTERNAL_DeleteShader;
	shaderBackend.getParseData = (MOJOSHADER_getParseDataFunc) MOJOSHADER_vkGetShaderParseData;
	shaderBackend.bindShaders = (MOJOSHADER_bindShadersFunc) MOJOSHADER_vkBindShaders;
	shaderBackend.getBoundShaders = (MOJOSHADER_getBoundShadersFunc) MOJOSHADER_vkGetBoundShaders;
	shaderBackend.mapUniformBufferMemory = MOJOSHADER_vkMapUniformBufferMemory;
	shaderBackend.unmapUniformBufferMemory = MOJOSHADER_vkUnmapUniformBufferMemory;
	shaderBackend.m = NULL;
	shaderBackend.f = NULL;
	shaderBackend.malloc_data = driverData;

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

	result = (VulkanEffect*) SDL_malloc(sizeof(VulkanEffect));
	result->effect = *effectData;
	*effect = (FNA3D_Effect*) result;
}

static void VULKAN_CloneEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *cloneSource,
	FNA3D_Effect **effect,
	MOJOSHADER_effect **effectData
) {
	VulkanEffect *vulkanCloneSource = (VulkanEffect*) cloneSource;
	VulkanEffect *result;

	*effectData = MOJOSHADER_cloneEffect(vulkanCloneSource->effect);
	if (*effectData == NULL)
	{
		FNA3D_LogError(MOJOSHADER_vkGetError());
	}

	result = (VulkanEffect*) SDL_malloc(sizeof(VulkanEffect));
	result->effect = *effectData;
	*effect = (FNA3D_Effect*) result;
}

static void VULKAN_AddDisposeEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanEffect *vulkanEffect = (VulkanEffect*) effect;

	SDL_LockMutex(renderer->disposeLock);
	if (renderer->effectsToDestroyCount + 1 >= renderer->effectsToDestroyCapacity)
	{
		renderer->effectsToDestroyCapacity *= 2;

		renderer->effectsToDestroy = SDL_realloc(
			renderer->effectsToDestroy,
			sizeof(VulkanEffect*) * renderer->effectsToDestroyCapacity
		);
	}

	renderer->effectsToDestroy[renderer->effectsToDestroyCount] = vulkanEffect;
	renderer->effectsToDestroyCount += 1;
	SDL_UnlockMutex(renderer->disposeLock);
}

static void VULKAN_SetEffectTechnique(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	MOJOSHADER_effectTechnique *technique
) {
	VulkanEffect *vkEffect = (VulkanEffect*) effect;
	MOJOSHADER_effectSetTechnique(vkEffect->effect, technique);
}

static void VULKAN_ApplyEffect(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	uint32_t pass,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanEffect *fnaEffect = (VulkanEffect*) effect;
	MOJOSHADER_effect *effectData = fnaEffect->effect;
	const MOJOSHADER_effectTechnique *technique = fnaEffect->effect->current_technique;
	uint32_t numPasses;

	/*
	 * Lock to prevent mojoshader from overwriting resource data
	 * while resources are initialized
	 */
	SDL_LockMutex(renderer->passLock);

	renderer->vertexSamplerDescriptorSetDataNeedsUpdate = 1;
	renderer->fragSamplerDescriptorSetDataNeedsUpdate = 1;
	renderer->needNewPipeline = 1;

	if (effectData == renderer->currentEffect)
	{
		if (	technique == renderer->currentTechnique &&
			pass == renderer->currentPass	)
		{
			MOJOSHADER_effectCommitChanges(
				renderer->currentEffect
			);

			SDL_UnlockMutex(renderer->passLock);
			return;
		}
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectBeginPass(renderer->currentEffect, pass);
		renderer->currentTechnique = technique;
		renderer->currentPass = pass;

		SDL_UnlockMutex(renderer->passLock);
		return;
	}
	else if (renderer->currentEffect != NULL)
	{
		MOJOSHADER_effectEndPass(renderer->currentEffect);
		MOJOSHADER_effectEnd(renderer->currentEffect);
	}

	MOJOSHADER_effectBegin(
		effectData,
		&numPasses,
		0,
		stateChanges
	);

	MOJOSHADER_effectBeginPass(effectData, pass);
	renderer->currentEffect = effectData;
	renderer->currentTechnique = technique;
	renderer->currentPass = pass;

	SDL_UnlockMutex(renderer->passLock);
}

static void VULKAN_BeginPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect,
	MOJOSHADER_effectStateChanges *stateChanges
) {
	MOJOSHADER_effect *effectData = ((VulkanEffect *) effect)->effect;
	uint32_t whatever;

	MOJOSHADER_effectBegin(
			effectData,
			&whatever,
			1,
			stateChanges
	);
	MOJOSHADER_effectBeginPass(effectData, 0);
}

static void VULKAN_EndPassRestore(
	FNA3D_Renderer *driverData,
	FNA3D_Effect *effect
) {
	MOJOSHADER_effect *effectData = ((VulkanEffect *) effect)->effect;
	MOJOSHADER_effectEndPass(effectData);
	MOJOSHADER_effectEnd(effectData);
}

/* Queries */

static FNA3D_Query* VULKAN_CreateQuery(FNA3D_Renderer *driverData)
{
	VulkanRenderer *renderer = (VulkanRenderer *) driverData;
	VulkanQuery *query = (VulkanQuery*) SDL_malloc(sizeof(VulkanQuery));

	if (renderer->freeQueryIndexStackHead == -1)
	{
		FNA3D_LogError(
			"Query limit of %d has been exceeded!",
			MAX_QUERIES
		);
		return NULL;
	}

	query->index = renderer->freeQueryIndexStackHead;
	renderer->freeQueryIndexStackHead = renderer->freeQueryIndexStack[renderer->freeQueryIndexStackHead];
	return (FNA3D_Query *) query;
}

static void VULKAN_AddDisposeQuery(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanQuery *vulkanQuery = (VulkanQuery*) query;

	/* Push the now-free index to the stack */
	SDL_LockMutex(renderer->disposeLock);
	renderer->freeQueryIndexStack[vulkanQuery->index] =
		renderer->freeQueryIndexStackHead;
	renderer->freeQueryIndexStackHead = vulkanQuery->index;
	SDL_UnlockMutex(renderer->disposeLock);

	SDL_free(vulkanQuery);
}

static void VULKAN_QueryBegin(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanQuery *vulkanQuery = (VulkanQuery*) query;

	/* Need to do this between passes */
	VULKAN_INTERNAL_MaybeEndRenderPass(renderer, 1);

	RECORD_CMD(renderer->vkCmdResetQueryPool(
		renderer->currentCommandBuffer,
		renderer->queryPool,
		vulkanQuery->index,
		1
	));

	RECORD_CMD(renderer->vkCmdBeginQuery(
		renderer->currentCommandBuffer,
		renderer->queryPool,
		vulkanQuery->index,
		VK_QUERY_CONTROL_PRECISE_BIT
	));
}

static void VULKAN_QueryEnd(FNA3D_Renderer *driverData, FNA3D_Query *query)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanQuery *vulkanQuery = (VulkanQuery*) query;

	/* Assume that the user is calling this in
	 * the same pass as they started it
	 */

	RECORD_CMD(renderer->vkCmdEndQuery(
		renderer->currentCommandBuffer,
		renderer->queryPool,
		vulkanQuery->index
	));
}

static uint8_t VULKAN_QueryComplete(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanQuery *vulkanQuery = (VulkanQuery*) query;
	VkResult vulkanResult;
	uint32_t queryResult;

	vulkanResult = renderer->vkGetQueryPoolResults(
		renderer->logicalDevice,
		renderer->queryPool,
		vulkanQuery->index,
		1,
		sizeof(queryResult),
		&queryResult,
		0,
		0
	);

	return vulkanResult == VK_SUCCESS;
}

static int32_t VULKAN_QueryPixelCount(
	FNA3D_Renderer *driverData,
	FNA3D_Query *query
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VulkanQuery *vulkanQuery = (VulkanQuery*) query;
	VkResult vulkanResult;
	uint32_t queryResult;

	vulkanResult = renderer->vkGetQueryPoolResults(
		renderer->logicalDevice,
		renderer->queryPool,
		vulkanQuery->index,
		1,
		sizeof(queryResult),
		&queryResult,
		0,
		0
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkGetQueryPoolResults, 0)

	/* FIXME maybe signed/unsigned integer problems? */
	return queryResult;
}

/* Feature Queries */

static uint8_t VULKAN_SupportsDXT1(FNA3D_Renderer *driverData)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	return renderer->supportsDxt1;
}

static uint8_t VULKAN_SupportsS3TC(FNA3D_Renderer *driverData)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	return renderer->supportsS3tc;
}

static uint8_t VULKAN_SupportsHardwareInstancing(FNA3D_Renderer *driverData)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	return renderer->supports.EXT_vertex_attribute_divisor;
}

static uint8_t VULKAN_SupportsNoOverwrite(FNA3D_Renderer *driverData)
{
	return 1;
}

static void VULKAN_GetMaxTextureSlots(
	FNA3D_Renderer *driverData,
	int32_t *textures,
	int32_t *vertexTextures
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	*textures = renderer->numTextureSlots;
	*vertexTextures = renderer->numVertexTextureSlots;
}

static int32_t VULKAN_GetMaxMultiSampleCount(
	FNA3D_Renderer *driverData,
	FNA3D_SurfaceFormat format,
	int32_t multiSampleCount
) {
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VkSampleCountFlags flags = renderer->physicalDeviceProperties.properties.limits.framebufferColorSampleCounts;
	int32_t maxSupported = 1;

	if (flags & VK_SAMPLE_COUNT_64_BIT)
	{
		maxSupported = 64;
	}
	else if (flags & VK_SAMPLE_COUNT_32_BIT)
	{
		maxSupported = 32;
	}
	else if (flags & VK_SAMPLE_COUNT_16_BIT)
	{
		maxSupported = 16;
	}
	else if (flags & VK_SAMPLE_COUNT_8_BIT)
	{
		maxSupported = 8;
	}
	else if (flags & VK_SAMPLE_COUNT_4_BIT)
	{
		maxSupported = 4;
	}
	else if (flags & VK_SAMPLE_COUNT_2_BIT)
	{
		maxSupported = 2;
	}

	return SDL_min(multiSampleCount, maxSupported);
}

/* Debugging */

static void VULKAN_SetStringMarker(FNA3D_Renderer *driverData, const char *text)
{
	VulkanRenderer *renderer = (VulkanRenderer*) driverData;
	VkDebugUtilsLabelEXT labelInfo;

	if (renderer->supportsDebugUtils)
	{
		labelInfo.sType = VK_STRUCTURE_TYPE_DEBUG_UTILS_LABEL_EXT;
		labelInfo.pNext = NULL;
		labelInfo.pLabelName = text;

		RECORD_CMD(renderer->vkCmdInsertDebugUtilsLabelEXT(
			renderer->currentCommandBuffer,
			&labelInfo
		));
	}
}

/* External Interop */

static void VULKAN_GetSysRenderer(
	FNA3D_Renderer *driverData,
	FNA3D_SysRendererEXT *sysrenderer
) {
	VulkanRenderer* renderer = (VulkanRenderer*) driverData;

	sysrenderer->rendererType = FNA3D_RENDERER_TYPE_VULKAN_EXT;
	sysrenderer->renderer.vulkan.instance = renderer->instance;
	sysrenderer->renderer.vulkan.physicalDevice = renderer->physicalDevice;
	sysrenderer->renderer.vulkan.logicalDevice = renderer->logicalDevice;
	sysrenderer->renderer.vulkan.queueFamilyIndex = renderer->queueFamilyIndex;
}

static FNA3D_Texture* VULKAN_CreateSysTexture(
	FNA3D_Renderer *driverData,
	FNA3D_SysTextureEXT *systexture
) {
	VulkanTexture *texture;

	if (systexture->rendererType != FNA3D_RENDERER_TYPE_VULKAN_EXT)
	{
		return NULL;
	}

	texture = (VulkanTexture*) SDL_malloc(sizeof(VulkanTexture));

	texture->image = (VkImage) systexture->texture.vulkan.image;
	texture->view = (VkImageView) systexture->texture.vulkan.view;
	texture->external = 1;

	/* unused by external */
	texture->usedRegion = NULL;
	texture->colorFormat = 0;
	texture->depth = 0;
	texture->depthStencilFormat = 0;
	texture->dimensions.width = 0;
	texture->dimensions.height = 0;
	texture->layerCount = 0;
	texture->levelCount = 0;
	texture->resourceAccessType = RESOURCE_ACCESS_NONE;
	texture->rtViews[0] = VK_NULL_HANDLE;
	texture->rtViews[1] = VK_NULL_HANDLE;
	texture->rtViews[2] = VK_NULL_HANDLE;
	texture->rtViews[3] = VK_NULL_HANDLE;
	texture->rtViews[4] = VK_NULL_HANDLE;
	texture->rtViews[5] = VK_NULL_HANDLE;
	texture->surfaceFormat = 0;

	return (FNA3D_Texture*) texture;
}

/* Driver */

static uint8_t VULKAN_PrepareWindowAttributes(uint32_t *flags)
{
	SDL_Window *dummyWindowHandle;
	FNA3D_PresentationParameters presentationParameters;
	VulkanRenderer *renderer;
	uint8_t result;

	/* Required for MoltenVK support */
	SDL_setenv("MVK_CONFIG_FULL_IMAGE_VIEW_SWIZZLE", "1", 1);
	SDL_setenv("MVK_CONFIG_SHADER_CONVERSION_FLIP_VERTEX_Y", "0", 1);

	if (SDL_Vulkan_LoadLibrary(NULL) < 0)
	{
		FNA3D_LogWarn("Vulkan: SDL_Vulkan_LoadLibrary failed!");
		return 0;
	}

#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wpedantic"
	vkGetInstanceProcAddr = (PFN_vkGetInstanceProcAddr) SDL_Vulkan_GetVkGetInstanceProcAddr();
#pragma GCC diagnostic pop
	if (vkGetInstanceProcAddr == NULL)
	{
		FNA3D_LogWarn(
			"SDL_Vulkan_GetVkGetInstanceProcAddr(): %s",
			SDL_GetError()
		);
		return 0;
	}

	#define VULKAN_GLOBAL_FUNCTION(name) \
		name = (PFN_##name) vkGetInstanceProcAddr(VK_NULL_HANDLE, #name); \
		if (name == NULL) \
		{ \
			FNA3D_LogWarn("vkGetInstanceProcAddr(VK_NULL_HANDLE, \"" #name "\") failed"); \
			return 0; \
		}
	#include "FNA3D_Driver_Vulkan_vkfuncs.h"

	/* Test if we can create a vulkan device */

	/* Create a dummy window, otherwise we cannot query swapchain support */
	dummyWindowHandle = SDL_CreateWindow(
		"FNA3D Vulkan",
		0, 0,
		128, 128,
		SDL_WINDOW_VULKAN | SDL_WINDOW_HIDDEN
	);

	if (dummyWindowHandle == NULL)
	{
		FNA3D_LogWarn("Vulkan: Could not create dummy window");
		return 0;
	}

	presentationParameters.deviceWindowHandle = dummyWindowHandle;

	/* partially set up VulkanRenderer so we can fall back in case of device non-compliance */
	renderer = (VulkanRenderer*) SDL_malloc(sizeof(VulkanRenderer));
	SDL_memset(renderer, '\0', sizeof(VulkanRenderer));
	renderer->deviceWindowHandle = dummyWindowHandle;

	if (!VULKAN_INTERNAL_CreateInstance(renderer, &presentationParameters))
	{
		SDL_DestroyWindow(dummyWindowHandle);
		SDL_free(renderer);
		FNA3D_LogWarn("Vulkan: Could not create Vulkan instance");
		return 0;
	}

	if (!SDL_Vulkan_CreateSurface(
		(SDL_Window*) presentationParameters.deviceWindowHandle,
		renderer->instance,
		&renderer->surface
	)) {
		SDL_DestroyWindow(dummyWindowHandle);
		SDL_free(renderer);
		FNA3D_LogWarn(
			"SDL_Vulkan_CreateSurface failed: %s",
			SDL_GetError()
		);
		return 0;
	}

	#define VULKAN_INSTANCE_FUNCTION(name) \
		renderer->name = (PFN_##name) vkGetInstanceProcAddr(renderer->instance, #name);
	#include "FNA3D_Driver_Vulkan_vkfuncs.h"

	result = VULKAN_INTERNAL_DeterminePhysicalDevice(renderer);

	renderer->vkDestroySurfaceKHR(
		renderer->instance,
		renderer->surface,
		NULL
	);
	renderer->vkDestroyInstance(renderer->instance, NULL);
	SDL_DestroyWindow(dummyWindowHandle);
	SDL_free(renderer);

	if (!result)
	{
		FNA3D_LogWarn("Vulkan: Failed to determine a suitable physical device");
	}
	else
	{
		*flags = SDL_WINDOW_VULKAN;
	}
	return result;
}

static void VULKAN_GetDrawableSize(void* window, int32_t *w, int32_t *h)
{
	SDL_Vulkan_GetDrawableSize((SDL_Window*) window, w, h);
}

static FNA3D_Device* VULKAN_CreateDevice(
	FNA3D_PresentationParameters *presentationParameters,
	uint8_t debugMode
) {
	uint32_t i;
	VkResult vulkanResult;

	/* Variables: Create the FNA3D_Device */
	FNA3D_Device *result;
	VulkanRenderer *renderer;

	/* Variables: Choose depth formats */
	VkImageFormatProperties imageFormatProperties;

	/* Variables: Create command pool and command buffer */
	VkCommandPoolCreateInfo commandPoolCreateInfo;
	VkCommandBufferAllocateInfo commandBufferAllocateInfo;

	/* Variables: Create fence and semaphores */
	VkFenceCreateInfo fenceInfo;
	VkSemaphoreCreateInfo semaphoreInfo;

	/* Variables: Create pipeline cache */
	VkPipelineCacheCreateInfo pipelineCacheCreateInfo;
	const char *pipelineCacheFileName;
	size_t pipelineCacheSize;
	uint8_t *pipelineCacheBytes;

	/* Variables: Create descriptor set layouts */
	VkDescriptorSetLayoutBinding layoutBinding;
	VkDescriptorSetLayoutCreateInfo layoutCreateInfo;

	/* Variables: Check for DXT1/S3TC Support */
	VkFormatProperties formatPropsBC1, formatPropsBC2, formatPropsBC3;

	/* Variables: Create query pool */
	VkQueryPoolCreateInfo queryPoolCreateInfo;

	/* Variables: Create dummy data */
	VkSamplerCreateInfo samplerCreateInfo;
	uint8_t dummyData[] = { 0 };

	/* Variables: Create UBO pool and dummy UBO descriptor sets */
	VkDescriptorPoolSize descriptorPoolSize;
	VkDescriptorPoolCreateInfo descriptorPoolInfo;
	VkDescriptorSetAllocateInfo descriptorSetAllocateInfo;
	VkWriteDescriptorSet writeDescriptorSets[2];
	VkDescriptorBufferInfo bufferInfos[2];

	/*
	 * Create the FNA3D_Device
	 */

	result = (FNA3D_Device*) SDL_malloc(sizeof(FNA3D_Device));
	ASSIGN_DRIVER(VULKAN)

	renderer = (VulkanRenderer*) SDL_malloc(sizeof(VulkanRenderer));
	SDL_memset(renderer, '\0', sizeof(VulkanRenderer));
	renderer->debugMode = debugMode;
	renderer->parentDevice = result;
	result->driverData = (FNA3D_Renderer*) renderer;

	renderer->presentInterval = presentationParameters->presentationInterval;
	renderer->deviceWindowHandle = presentationParameters->deviceWindowHandle;

	/*
	 * Create the vkInstance
	 */

	if (!VULKAN_INTERNAL_CreateInstance(renderer, presentationParameters))
	{
		FNA3D_LogError("Error creating vulkan instance");
		return NULL;
	}

	/*
	 * Create the WSI vkSurface
	 */

	if (!SDL_Vulkan_CreateSurface(
		(SDL_Window*) presentationParameters->deviceWindowHandle,
		renderer->instance,
		&renderer->surface
	)) {
		FNA3D_LogError(
			"SDL_Vulkan_CreateSurface failed: %s",
			SDL_GetError()
		);
		return NULL;
	}

	/*
	 * Get vkInstance entry points
	 */

	#define VULKAN_INSTANCE_FUNCTION(name) \
		renderer->name = (PFN_##name) vkGetInstanceProcAddr(renderer->instance, #name);
	#include "FNA3D_Driver_Vulkan_vkfuncs.h"

	/*
	 * Choose/Create vkDevice
	 */

	if (!VULKAN_INTERNAL_DeterminePhysicalDevice(renderer))
	{
		FNA3D_LogError("Failed to determine a suitable physical device");
		return NULL;
	}

	FNA3D_LogInfo("FNA3D Driver: Vulkan");
	FNA3D_LogInfo(
		"Vulkan Device: %s",
		renderer->physicalDeviceProperties.properties.deviceName
	);
	if (renderer->supports.KHR_driver_properties)
	{
		FNA3D_LogInfo(
			"Vulkan Driver: %s %s",
			renderer->physicalDeviceDriverProperties.driverName,
			renderer->physicalDeviceDriverProperties.driverInfo
		);
		FNA3D_LogInfo(
			"Vulkan Conformance: %u.%u.%u",
			renderer->physicalDeviceDriverProperties.conformanceVersion.major,
			renderer->physicalDeviceDriverProperties.conformanceVersion.minor,
			renderer->physicalDeviceDriverProperties.conformanceVersion.patch
		);
	}
	else
	{
		FNA3D_LogInfo("KHR_driver_properties unsupported! Bother your vendor about this!");
	}
	FNA3D_LogWarn(
		"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n"
		"FNA3D Vulkan is still in development! You have been warned!\n"
		"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
	);

	if (!VULKAN_INTERNAL_CreateLogicalDevice(renderer))
	{
		FNA3D_LogError("Failed to create logical device");
		return NULL;
	}

	/*
	 * Initialize memory allocator
	 */

	renderer->memoryAllocator = (VulkanMemoryAllocator*) SDL_malloc(
		sizeof(VulkanMemoryAllocator)
	);

	for (i = 0; i < VK_MAX_MEMORY_TYPES; i += 1)
	{
		renderer->memoryAllocator->subAllocators[i].memoryTypeIndex = i;
		renderer->memoryAllocator->subAllocators[i].nextAllocationSize = STARTING_ALLOCATION_SIZE;
		renderer->memoryAllocator->subAllocators[i].allocations = NULL;
		renderer->memoryAllocator->subAllocators[i].allocationCount = 0;
		renderer->memoryAllocator->subAllocators[i].sortedFreeRegions = SDL_malloc(
			sizeof(VulkanMemoryFreeRegion*) * 4
		);
		renderer->memoryAllocator->subAllocators[i].sortedFreeRegionCount = 0;
		renderer->memoryAllocator->subAllocators[i].sortedFreeRegionCapacity = 4;
	}

	/*
	 * Initialize buffer space
	 */

	renderer->maxBuffersInUse = 32;
	renderer->buffersInUse = (VulkanBuffer**) SDL_malloc(
		sizeof(VulkanBuffer*) * renderer->maxBuffersInUse
	);

	renderer->maxSubmittedBuffers = 32;
	renderer->numSubmittedBuffers = 0;
	renderer->submittedBuffers = (VulkanBuffer**) SDL_malloc(
		sizeof(VulkanBuffer*) * renderer->maxSubmittedBuffers
	);

	VULKAN_INTERNAL_CreateTextureStagingBuffer(renderer);

	/*
	 * Choose depth formats
	 */

	vulkanResult = renderer->vkGetPhysicalDeviceImageFormatProperties(
		renderer->physicalDevice,
		VK_FORMAT_D16_UNORM,
		VK_IMAGE_TYPE_2D,
		VK_IMAGE_TILING_OPTIMAL,
		VK_IMAGE_ASPECT_DEPTH_BIT,
		0,
		&imageFormatProperties
	);

	if (vulkanResult == VK_ERROR_FORMAT_NOT_SUPPORTED)
	{
		renderer->D16Format = VK_FORMAT_D32_SFLOAT;
	}
	else
	{
		renderer->D16Format = VK_FORMAT_D16_UNORM;
	}

	/* Vulkan doesn't even have plain D24 in the spec! */
	renderer->D24Format = VK_FORMAT_D32_SFLOAT;

	vulkanResult = renderer->vkGetPhysicalDeviceImageFormatProperties(
		renderer->physicalDevice,
		VK_FORMAT_D24_UNORM_S8_UINT,
		VK_IMAGE_TYPE_2D,
		VK_IMAGE_TILING_OPTIMAL,
		VK_IMAGE_ASPECT_DEPTH_BIT | VK_IMAGE_ASPECT_STENCIL_BIT,
		0,
		&imageFormatProperties
	);

	if (vulkanResult == VK_ERROR_FORMAT_NOT_SUPPORTED)
	{
		renderer->D24S8Format = VK_FORMAT_D32_SFLOAT_S8_UINT;
	}
	else
	{
		renderer->D24S8Format = VK_FORMAT_D24_UNORM_S8_UINT;
	}

	/*
	 * Create "deferred destroy" storage
	 */

	renderer->renderbuffersToDestroyCapacity = 16;
	renderer->renderbuffersToDestroyCount = 0;

	renderer->renderbuffersToDestroy = (VulkanRenderbuffer**) SDL_malloc(
		sizeof(VulkanRenderbuffer*) *
		renderer->renderbuffersToDestroyCapacity
	);

	renderer->submittedRenderbuffersToDestroyCapacity = 16;
	renderer->submittedRenderbuffersToDestroyCount = 0;

	renderer->submittedRenderbuffersToDestroy = (VulkanRenderbuffer**) SDL_malloc(
		sizeof(VulkanRenderbuffer*) *
		renderer->submittedRenderbuffersToDestroyCapacity
	);

	renderer->buffersToDestroyCapacity = 16;
	renderer->buffersToDestroyCount = 0;

	renderer->buffersToDestroy = (VulkanBuffer**) SDL_malloc(
		sizeof(VulkanBuffer*) *
		renderer->buffersToDestroyCapacity
	);

	renderer->submittedBuffersToDestroyCapacity = 16;
	renderer->submittedBuffersToDestroyCount = 0;

	renderer->submittedBuffersToDestroy = (VulkanBuffer**) SDL_malloc(
		sizeof(VulkanBuffer*) *
		renderer->submittedBuffersToDestroyCapacity
	);

	renderer->effectsToDestroyCapacity = 16;
	renderer->effectsToDestroyCount = 0;

	renderer->effectsToDestroy = (VulkanEffect**) SDL_malloc(
		sizeof(VulkanEffect*) *
		renderer->effectsToDestroyCapacity
	);

	renderer->submittedEffectsToDestroyCapacity = 16;
	renderer->submittedEffectsToDestroyCount = 0;

	renderer->submittedEffectsToDestroy = (VulkanEffect**) SDL_malloc(
		sizeof(VulkanEffect*) *
		renderer->submittedEffectsToDestroyCapacity
	);

	renderer->texturesToDestroyCapacity = 16;
	renderer->texturesToDestroyCount = 0;

	renderer->texturesToDestroy = (VulkanTexture**) SDL_malloc(
		sizeof(VulkanTexture*) *
		renderer->texturesToDestroyCapacity
	);

	renderer->submittedTexturesToDestroyCapacity = 16;
	renderer->submittedTexturesToDestroyCount = 0;

	renderer->submittedTexturesToDestroy = (VulkanTexture**) SDL_malloc(
		sizeof(VulkanTexture*) *
		renderer->submittedTexturesToDestroyCapacity
	);

	renderer->defragmentedBuffersToDestroyCapacity = 16;
	renderer->defragmentedBuffersToDestroyCount = 0;

	renderer->defragmentedBuffersToDestroy = (VkBuffer*) SDL_malloc(
		sizeof(VkBuffer) *
		renderer->defragmentedBuffersToDestroyCapacity
	);

	renderer->defragmentedImagesToDestroyCapacity = 16;
	renderer->defragmentedImagesToDestroyCount = 0;

	renderer->defragmentedImagesToDestroy = (VkImage*) SDL_malloc(
		sizeof(VkImage) *
		renderer->defragmentedImagesToDestroyCapacity
	);

	renderer->defragmentedImageViewsToDestroyCapacity = 16;
	renderer->defragmentedImageViewsToDestroyCount = 0;

	renderer->defragmentedImageViewsToDestroy = (VkImageView*) SDL_malloc(
		sizeof(VkImageView) *
		renderer->defragmentedImageViewsToDestroyCapacity
	);

	renderer->usedRegionsToDestroyCapacity = 16;
	renderer->usedRegionsToDestroyCount = 0;

	renderer->usedRegionsToDestroy = (VulkanMemoryUsedRegion**) SDL_malloc(
		sizeof(VulkanMemoryUsedRegion*) *
		renderer->usedRegionsToDestroyCapacity
	);

	/*
	 * Create MojoShader context
	 */

	renderer->mojoshaderContext = MOJOSHADER_vkCreateContext(
		&renderer->instance,
		&renderer->physicalDevice,
		&renderer->logicalDevice,
		(PFN_MOJOSHADER_vkGetInstanceProcAddr) vkGetInstanceProcAddr,
		(PFN_MOJOSHADER_vkGetDeviceProcAddr) renderer->vkGetDeviceProcAddr,
		renderer->queueFamilyIndex,
		renderer->physicalDeviceProperties.properties.limits.maxUniformBufferRange,
		renderer->physicalDeviceProperties.properties.limits.minUniformBufferOffsetAlignment,
		NULL,
		NULL,
		renderer
	);
	if (renderer->mojoshaderContext != NULL)
	{
		MOJOSHADER_vkMakeContextCurrent(renderer->mojoshaderContext);
	}
	else
	{
		FNA3D_LogError("Failed to create MojoShader context");
		return NULL;
	}

	/*
	 * Create initial swapchain
	 */

	if (VULKAN_INTERNAL_CreateSwapchain(renderer) != CREATE_SWAPCHAIN_SUCCESS)
	{
		FNA3D_LogError("Failed to create swap chain");
		return NULL;
	}

	/*
	 * Create fence and semaphores
	 */

	fenceInfo.sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO;
	fenceInfo.pNext = NULL;
	fenceInfo.flags = VK_FENCE_CREATE_SIGNALED_BIT;

	semaphoreInfo.sType = VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO;
	semaphoreInfo.pNext = NULL;
	semaphoreInfo.flags = 0;

	vulkanResult = renderer->vkCreateSemaphore(
		renderer->logicalDevice,
		&semaphoreInfo,
		NULL,
		&renderer->imageAvailableSemaphore
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateSemaphore, NULL)

	vulkanResult = renderer->vkCreateSemaphore(
		renderer->logicalDevice,
		&semaphoreInfo,
		NULL,
		&renderer->renderFinishedSemaphore
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateSemaphore, NULL)

	vulkanResult = renderer->vkCreateSemaphore(
		renderer->logicalDevice,
		&semaphoreInfo,
		NULL,
		&renderer->defragSemaphore
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateSemaphore, NULL)

	vulkanResult = renderer->vkCreateFence(
		renderer->logicalDevice,
		&fenceInfo,
		NULL,
		&renderer->inFlightFence
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateFence, NULL)

	vulkanResult = renderer->vkCreateFence(
		renderer->logicalDevice,
		&fenceInfo,
		NULL,
		&renderer->defragFence
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateFence, NULL)

	/*
	 * Create command pool and buffers
	 */

	commandPoolCreateInfo.sType = VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO;
	commandPoolCreateInfo.pNext = NULL;
	commandPoolCreateInfo.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
	commandPoolCreateInfo.queueFamilyIndex = renderer->queueFamilyIndex;
	vulkanResult = renderer->vkCreateCommandPool(
		renderer->logicalDevice,
		&commandPoolCreateInfo,
		NULL,
		&renderer->commandPool
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateCommandPool, NULL)

	renderer->allocatedCommandBufferCount = 4;
	renderer->inactiveCommandBuffers = SDL_malloc(sizeof(VkCommandBuffer) * renderer->allocatedCommandBufferCount);
	renderer->activeCommandBuffers = SDL_malloc(sizeof(VkCommandBuffer) * renderer->allocatedCommandBufferCount);
	renderer->submittedCommandBuffers = SDL_malloc(sizeof(VkCommandBuffer) * renderer->allocatedCommandBufferCount);
	renderer->inactiveCommandBufferCount = renderer->allocatedCommandBufferCount;
	renderer->activeCommandBufferCount = 0;
	renderer->submittedCommandBufferCount = 0;

	commandBufferAllocateInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
	commandBufferAllocateInfo.pNext = NULL;
	commandBufferAllocateInfo.commandPool = renderer->commandPool;
	commandBufferAllocateInfo.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
	commandBufferAllocateInfo.commandBufferCount = renderer->allocatedCommandBufferCount;
	vulkanResult = renderer->vkAllocateCommandBuffers(
		renderer->logicalDevice,
		&commandBufferAllocateInfo,
		renderer->inactiveCommandBuffers
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkAllocateCommandBuffers, NULL)

	renderer->currentCommandCount = 0;

	commandBufferAllocateInfo.commandBufferCount = 1;

	vulkanResult = renderer->vkAllocateCommandBuffers(
		renderer->logicalDevice,
		&commandBufferAllocateInfo,
		&renderer->defragCommandBuffer
	);

	VULKAN_INTERNAL_BeginCommandBuffer(renderer);

	/*
	 * Create the initial faux-backbuffer
	 */

	if (!VULKAN_INTERNAL_CreateFauxBackbuffer(renderer, presentationParameters))
	{
		FNA3D_LogError("Failed to create faux backbuffer");
		return NULL;
	}

	/*
	 * Create the pipeline cache
	 */

	pipelineCacheCreateInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_CACHE_CREATE_INFO;
	pipelineCacheCreateInfo.pNext = NULL;
	pipelineCacheCreateInfo.flags = 0;

	pipelineCacheFileName = SDL_GetHint("FNA3D_VULKAN_PIPELINE_CACHE_FILE_NAME");
	if (pipelineCacheFileName == NULL)
	{
		pipelineCacheFileName = DEFAULT_PIPELINE_CACHE_FILE_NAME;
	}
	if (pipelineCacheFileName[0] == '\0')
	{
		/* For intentionally empty file names, assume caching is disabled */
		pipelineCacheBytes = NULL;
	}
	else
	{
		pipelineCacheBytes = SDL_LoadFile(pipelineCacheFileName, &pipelineCacheSize);
	}

	if (pipelineCacheBytes != NULL)
	{
		FNA3D_LogInfo("Pipeline cache found, loading...");
		pipelineCacheCreateInfo.pInitialData = pipelineCacheBytes;
		pipelineCacheCreateInfo.initialDataSize = pipelineCacheSize;
	}
	else
	{
		pipelineCacheCreateInfo.initialDataSize = 0;
		pipelineCacheCreateInfo.pInitialData = NULL;
	}

	vulkanResult = renderer->vkCreatePipelineCache(
		renderer->logicalDevice,
		&pipelineCacheCreateInfo,
		NULL,
		&renderer->pipelineCache
	);

	if (pipelineCacheBytes != NULL)
	{
		SDL_free(pipelineCacheBytes);

		/* The pipeline cache was invalid, try again with no input data */
		if (vulkanResult != VK_SUCCESS)
		{
			FNA3D_LogWarn("Pipeline cache preload failed, ignoring");
			pipelineCacheCreateInfo.initialDataSize = 0;
			pipelineCacheCreateInfo.pInitialData = NULL;
			vulkanResult = renderer->vkCreatePipelineCache(
				renderer->logicalDevice,
				&pipelineCacheCreateInfo,
				NULL,
				&renderer->pipelineCache
			);
		}
	}

	VULKAN_ERROR_CHECK(vulkanResult, vkCreatePipelineCache, NULL)

	/*
	 * Define sampler counts
	 */

	renderer->numSamplers = SDL_min(
		renderer->physicalDeviceProperties.properties.limits.maxSamplerAllocationCount,
		MAX_TEXTURE_SAMPLERS + MAX_VERTEXTEXTURE_SAMPLERS
	);
	renderer->numTextureSlots = SDL_min(
		renderer->numSamplers,
		MAX_TEXTURE_SAMPLERS
	);
	renderer->numVertexTextureSlots = SDL_min(
		SDL_max(renderer->numSamplers - MAX_TEXTURE_SAMPLERS, 0),
		MAX_VERTEXTEXTURE_SAMPLERS
	);

	/* Define vertex UBO set layout */
	layoutBinding.binding = 0;
	layoutBinding.descriptorType = VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
	layoutBinding.descriptorCount = 1;
	layoutBinding.stageFlags = VK_SHADER_STAGE_VERTEX_BIT;
	layoutBinding.pImmutableSamplers = NULL;

	layoutCreateInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
	layoutCreateInfo.pNext = NULL;
	layoutCreateInfo.flags = 0;
	layoutCreateInfo.bindingCount = 1;
	layoutCreateInfo.pBindings = &layoutBinding;

	vulkanResult = renderer->vkCreateDescriptorSetLayout(
		renderer->logicalDevice,
		&layoutCreateInfo,
		NULL,
		&renderer->vertexUniformBufferDescriptorSetLayout
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateDescriptorSetLayout, NULL)

	/* Define frag UBO set layout */
	layoutBinding.binding = 0;
	layoutBinding.descriptorType = VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
	layoutBinding.descriptorCount = 1;
	layoutBinding.stageFlags = VK_SHADER_STAGE_FRAGMENT_BIT;
	layoutBinding.pImmutableSamplers = NULL;

	layoutCreateInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
	layoutCreateInfo.pNext = NULL;
	layoutCreateInfo.flags = 0;
	layoutCreateInfo.bindingCount = 1;
	layoutCreateInfo.pBindings = &layoutBinding;

	vulkanResult = renderer->vkCreateDescriptorSetLayout(
		renderer->logicalDevice,
		&layoutCreateInfo,
		NULL,
		&renderer->fragUniformBufferDescriptorSetLayout
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateDescriptorSetLayout, NULL)

	renderer->vertexSamplerDescriptorSetDataNeedsUpdate = 1;
	renderer->fragSamplerDescriptorSetDataNeedsUpdate = 1;

	/*
	 * Init various renderer properties
	 */

	renderer->currentDepthFormat = presentationParameters->depthStencilFormat;
	renderer->currentPipeline = VK_NULL_HANDLE;
	renderer->needNewRenderPass = 1;
	renderer->needNewPipeline = 1;

	/*
	 * Check for DXT1/S3TC support
	 */

	renderer->vkGetPhysicalDeviceFormatProperties(
		renderer->physicalDevice,
		XNAToVK_SurfaceFormat[FNA3D_SURFACEFORMAT_DXT1],
		&formatPropsBC1
	);
	renderer->vkGetPhysicalDeviceFormatProperties(
		renderer->physicalDevice,
		XNAToVK_SurfaceFormat[FNA3D_SURFACEFORMAT_DXT3],
		&formatPropsBC2
	);
	renderer->vkGetPhysicalDeviceFormatProperties(
		renderer->physicalDevice,
		XNAToVK_SurfaceFormat[FNA3D_SURFACEFORMAT_DXT5],
		&formatPropsBC3
	);

	#define SUPPORTED_FORMAT(fmt) \
		((fmt.optimalTilingFeatures & VK_FORMAT_FEATURE_SAMPLED_IMAGE_BIT) && \
		(fmt.optimalTilingFeatures & VK_FORMAT_FEATURE_TRANSFER_DST_BIT))
	renderer->supportsDxt1 = SUPPORTED_FORMAT(formatPropsBC1);
	renderer->supportsS3tc = (
		SUPPORTED_FORMAT(formatPropsBC2) ||
		SUPPORTED_FORMAT(formatPropsBC3)
	);

	/*
	 * Initialize renderer members not covered by SDL_memset('\0')
	 */

	SDL_memset(
		renderer->multiSampleMask,
		-1,
		sizeof(renderer->multiSampleMask) /* AKA 0xFFFFFFFF */
	);
	for (i = 0; i < MAX_BOUND_VERTEX_BUFFERS; i += 1)
	{
		renderer->vertexBindings[i].vertexDeclaration.elements =
			renderer->vertexElements[i];
	}

	/*
	 * Create query pool
	 */

	queryPoolCreateInfo.sType = VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO;
	queryPoolCreateInfo.pNext = NULL;
	queryPoolCreateInfo.flags = 0;
	queryPoolCreateInfo.queryType = VK_QUERY_TYPE_OCCLUSION;
	queryPoolCreateInfo.queryCount = MAX_QUERIES;
	queryPoolCreateInfo.pipelineStatistics = 0;
	vulkanResult = renderer->vkCreateQueryPool(
		renderer->logicalDevice,
		&queryPoolCreateInfo,
		NULL,
		&renderer->queryPool
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateQueryPool, NULL)

	/* Set up the stack, the value at each index is the next available
	 * index, or -1 if no such index exists.
	 */
	for (i = 0; i < MAX_QUERIES - 1; i += 1)
	{
		renderer->freeQueryIndexStack[i] = i + 1;
	}
	renderer->freeQueryIndexStack[MAX_QUERIES - 1] = -1;

	/* Initialize hash tables */

	for (i = 0; i < NUM_SHADER_RESOURCES_BUCKETS; i += 1)
	{
		renderer->shaderResourcesHashTable.buckets[i].elements = NULL;
		renderer->shaderResourcesHashTable.buckets[i].count = 0;
		renderer->shaderResourcesHashTable.buckets[i].capacity = 0;
	}

	for (i = 0; i < NUM_DESCRIPTOR_SET_LAYOUT_BUCKETS; i += 1)
	{
		renderer->descriptorSetLayoutTable.buckets[i].elements = NULL;
		renderer->descriptorSetLayoutTable.buckets[i].count = 0;
		renderer->descriptorSetLayoutTable.buckets[i].capacity = 0;
	}
	/*
	 * Create dummy data
	 */

	renderer->dummyVertTexture = (VulkanTexture*) VULKAN_CreateTexture2D(
		(FNA3D_Renderer*) renderer,
		FNA3D_SURFACEFORMAT_COLOR,
		1,
		1,
		1,
		0
	);
	renderer->dummyVertTexture3D = (VulkanTexture*) VULKAN_CreateTexture3D(
		(FNA3D_Renderer*) renderer,
		FNA3D_SURFACEFORMAT_COLOR,
		1,
		1,
		1,
		1
	);
	renderer->dummyVertTextureCube = (VulkanTexture*) VULKAN_CreateTextureCube(
		(FNA3D_Renderer*) renderer,
		FNA3D_SURFACEFORMAT_COLOR,
		1,
		1,
		0
	);
	renderer->dummyFragTexture = (VulkanTexture*) VULKAN_CreateTexture2D(
		(FNA3D_Renderer*) renderer,
		FNA3D_SURFACEFORMAT_COLOR,
		1,
		1,
		1,
		0
	);
	renderer->dummyFragTexture3D = (VulkanTexture*) VULKAN_CreateTexture3D(
		(FNA3D_Renderer*) renderer,
		FNA3D_SURFACEFORMAT_COLOR,
		1,
		1,
		1,
		1
	);
	renderer->dummyFragTextureCube = (VulkanTexture*) VULKAN_CreateTextureCube(
		(FNA3D_Renderer*) renderer,
		FNA3D_SURFACEFORMAT_COLOR,
		1,
		1,
		0
	);
	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_VERTEX_SHADER_READ_SAMPLED_IMAGE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		1,
		0,
		1,
		1,
		renderer->dummyVertTexture->image,
		&renderer->dummyVertTexture->resourceAccessType
	);
	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_VERTEX_SHADER_READ_SAMPLED_IMAGE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		1,
		0,
		1,
		1,
		renderer->dummyVertTexture3D->image,
		&renderer->dummyVertTexture3D->resourceAccessType
	);
	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_VERTEX_SHADER_READ_SAMPLED_IMAGE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		6,
		0,
		1,
		1,
		renderer->dummyVertTextureCube->image,
		&renderer->dummyVertTextureCube->resourceAccessType
	);
	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_FRAGMENT_SHADER_READ_SAMPLED_IMAGE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		1,
		0,
		1,
		1,
		renderer->dummyFragTexture->image,
		&renderer->dummyFragTexture->resourceAccessType
	);
	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_FRAGMENT_SHADER_READ_SAMPLED_IMAGE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		1,
		0,
		1,
		1,
		renderer->dummyFragTexture3D->image,
		&renderer->dummyFragTexture3D->resourceAccessType
	);
	VULKAN_INTERNAL_ImageMemoryBarrier(
		renderer,
		RESOURCE_ACCESS_FRAGMENT_SHADER_READ_SAMPLED_IMAGE,
		VK_IMAGE_ASPECT_COLOR_BIT,
		0,
		6,
		0,
		1,
		1,
		renderer->dummyFragTextureCube->image,
		&renderer->dummyFragTextureCube->resourceAccessType
	);
	renderer->dummyVertUniformBuffer = (VulkanBuffer*) VULKAN_INTERNAL_CreateBuffer(
		renderer,
		1,
		RESOURCE_ACCESS_VERTEX_SHADER_READ_UNIFORM_BUFFER,
		VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
		0,
		0,
		1
	);
	VULKAN_INTERNAL_SetBufferData(
		(FNA3D_Renderer*) renderer,
		(FNA3D_Buffer*) renderer->dummyVertUniformBuffer,
		0,
		&dummyData,
		1,
		FNA3D_SETDATAOPTIONS_NOOVERWRITE
	);
	renderer->dummyFragUniformBuffer = (VulkanBuffer*) VULKAN_INTERNAL_CreateBuffer(
		renderer,
		1,
		RESOURCE_ACCESS_FRAGMENT_SHADER_READ_UNIFORM_BUFFER,
		VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
		0,
		0,
		1
	);
	VULKAN_INTERNAL_SetBufferData(
		(FNA3D_Renderer*) renderer,
		(FNA3D_Buffer*) renderer->dummyFragUniformBuffer,
		0,
		&dummyData,
		1,
		FNA3D_SETDATAOPTIONS_NOOVERWRITE
	);

	samplerCreateInfo.sType = VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO;
	samplerCreateInfo.pNext = NULL;
	samplerCreateInfo.flags = 0;
	samplerCreateInfo.magFilter = VK_FILTER_LINEAR;
	samplerCreateInfo.minFilter = VK_FILTER_LINEAR;
	samplerCreateInfo.mipmapMode = VK_SAMPLER_MIPMAP_MODE_NEAREST;
	samplerCreateInfo.addressModeU = VK_SAMPLER_ADDRESS_MODE_REPEAT;
	samplerCreateInfo.addressModeV = VK_SAMPLER_ADDRESS_MODE_REPEAT;
	samplerCreateInfo.addressModeW = VK_SAMPLER_ADDRESS_MODE_REPEAT;
	samplerCreateInfo.mipLodBias = 0;
	samplerCreateInfo.anisotropyEnable = 0;
	samplerCreateInfo.maxAnisotropy = 1;
	samplerCreateInfo.compareEnable = 0;
	samplerCreateInfo.compareOp = 0;
	samplerCreateInfo.minLod = 0;
	samplerCreateInfo.maxLod = 1;
	samplerCreateInfo.borderColor = VK_BORDER_COLOR_FLOAT_TRANSPARENT_BLACK;
	samplerCreateInfo.unnormalizedCoordinates = 0;
	renderer->vkCreateSampler(
		renderer->logicalDevice,
		&samplerCreateInfo,
		NULL,
		&renderer->dummyVertSamplerState
	);
	renderer->vkCreateSampler(
		renderer->logicalDevice,
		&samplerCreateInfo,
		NULL,
		&renderer->dummyVertSampler3DState
	);
	renderer->vkCreateSampler(
		renderer->logicalDevice,
		&samplerCreateInfo,
		NULL,
		&renderer->dummyVertSamplerCubeState
	);
	renderer->vkCreateSampler(
		renderer->logicalDevice,
		&samplerCreateInfo,
		NULL,
		&renderer->dummyFragSamplerState
	);
	renderer->vkCreateSampler(
		renderer->logicalDevice,
		&samplerCreateInfo,
		NULL,
		&renderer->dummyFragSampler3DState
	);
	renderer->vkCreateSampler(
		renderer->logicalDevice,
		&samplerCreateInfo,
		NULL,
		&renderer->dummyFragSamplerCubeState
	);

	descriptorPoolSize.type = VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
	descriptorPoolSize.descriptorCount = MAX_UNIFORM_DESCRIPTOR_SETS;

	descriptorPoolInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO;
	descriptorPoolInfo.pNext = NULL;
	descriptorPoolInfo.flags = 0;
	descriptorPoolInfo.maxSets = MAX_UNIFORM_DESCRIPTOR_SETS;
	descriptorPoolInfo.poolSizeCount = 1;
	descriptorPoolInfo.pPoolSizes = &descriptorPoolSize;

	vulkanResult = renderer->vkCreateDescriptorPool(
		renderer->logicalDevice,
		&descriptorPoolInfo,
		NULL,
		&renderer->uniformBufferDescriptorPool
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkCreateDescriptorPool, 0)

	descriptorSetAllocateInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO;
	descriptorSetAllocateInfo.pNext = NULL;
	descriptorSetAllocateInfo.descriptorPool = renderer->uniformBufferDescriptorPool;
	descriptorSetAllocateInfo.descriptorSetCount = 1;
	descriptorSetAllocateInfo.pSetLayouts = &renderer->vertexUniformBufferDescriptorSetLayout;

	vulkanResult = renderer->vkAllocateDescriptorSets(
		renderer->logicalDevice,
		&descriptorSetAllocateInfo,
		&renderer->dummyVertexUniformBufferDescriptorSet
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkAllocateDescriptorSets, 0)

	descriptorSetAllocateInfo.pSetLayouts = &renderer->fragUniformBufferDescriptorSetLayout;

	vulkanResult = renderer->vkAllocateDescriptorSets(
		renderer->logicalDevice,
		&descriptorSetAllocateInfo,
		&renderer->dummyFragUniformBufferDescriptorSet
	);
	VULKAN_ERROR_CHECK(vulkanResult, vkAllocateDescriptorSets, 0)

	bufferInfos[0].buffer = renderer->dummyVertUniformBuffer->subBuffers[renderer->dummyVertUniformBuffer->currentSubBufferIndex]->buffer;
	bufferInfos[0].offset = 0;
	bufferInfos[0].range = renderer->dummyVertUniformBuffer->size;

	writeDescriptorSets[0].sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
	writeDescriptorSets[0].pNext = NULL;
	writeDescriptorSets[0].descriptorCount = 1;
	writeDescriptorSets[0].descriptorType = VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
	writeDescriptorSets[0].dstArrayElement = 0;
	writeDescriptorSets[0].dstBinding = 0;
	writeDescriptorSets[0].dstSet = renderer->dummyVertexUniformBufferDescriptorSet;
	writeDescriptorSets[0].pBufferInfo = &bufferInfos[0];
	writeDescriptorSets[0].pImageInfo = NULL;
	writeDescriptorSets[0].pTexelBufferView = NULL;

	bufferInfos[1].buffer = renderer->dummyFragUniformBuffer->subBuffers[renderer->dummyFragUniformBuffer->currentSubBufferIndex]->buffer;
	bufferInfos[1].offset = 0;
	bufferInfos[1].range = renderer->dummyFragUniformBuffer->size;

	writeDescriptorSets[1].sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
	writeDescriptorSets[1].pNext = NULL;
	writeDescriptorSets[1].descriptorCount = 1;
	writeDescriptorSets[1].descriptorType = VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
	writeDescriptorSets[1].dstArrayElement = 0;
	writeDescriptorSets[1].dstBinding = 0;
	writeDescriptorSets[1].dstSet = renderer->dummyFragUniformBufferDescriptorSet;
	writeDescriptorSets[1].pBufferInfo = &bufferInfos[1];
	writeDescriptorSets[1].pImageInfo = NULL;
	writeDescriptorSets[1].pTexelBufferView = NULL;

	renderer->vkUpdateDescriptorSets(
		renderer->logicalDevice,
		2,
		writeDescriptorSets,
		0,
		NULL
	);

	/* init texture storage */

	for (i = 0; i < MAX_TEXTURE_SAMPLERS; i += 1)
	{
		renderer->textures[i] = &NullTexture;
		renderer->samplers[i] = renderer->dummyFragSamplerState;
	}

	for (i = 0; i < MAX_VERTEXTEXTURE_SAMPLERS; i += 1)
	{
		renderer->textures[MAX_TEXTURE_SAMPLERS + i] = &NullTexture;
		renderer->samplers[MAX_TEXTURE_SAMPLERS + i] = renderer->dummyVertSamplerState;
	}

	renderer->bufferDefragInProgress = 0;
	renderer->needDefrag = 0;
	renderer->defragTimer = 0;
	renderer->resourceFreed = 0;

	renderer->submitCounter = 0;

	renderer->commandLock = SDL_CreateMutex();
	renderer->passLock = SDL_CreateMutex();
	renderer->disposeLock = SDL_CreateMutex();
	renderer->allocatorLock = SDL_CreateMutex();
	renderer->stagingLock = SDL_CreateMutex();

	return result;
}

FNA3D_Driver VulkanDriver = {
	"Vulkan",
	VULKAN_PrepareWindowAttributes,
	VULKAN_GetDrawableSize,
	VULKAN_CreateDevice
};

#else

extern int this_tu_is_empty;

#endif /* FNA3D_DRIVER_VULKAN */
