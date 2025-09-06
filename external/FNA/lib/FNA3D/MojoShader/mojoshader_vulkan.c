/**
 * MojoShader; generate shader programs from bytecode of compiled
 *  Direct3D shaders.
 *
 * Please see the file LICENSE.txt in the source's root directory.
 *
 *  This file written by Ryan C. Gordon.
 */

#define __MOJOSHADER_INTERNAL__ 1
#include "mojoshader_internal.h"

#if SUPPORT_PROFILE_SPIRV

#include "vulkan/vulkan.h"

#define VULKAN_INSTANCE_FUNCTION(ret, func, params) \
    typedef ret (VKAPI_CALL *vkfntype_MOJOSHADER_##func) params;
#define VULKAN_DEVICE_FUNCTION(ret, func, params) \
	typedef ret (VKAPI_CALL *vkfntype_MOJOSHADER_##func) params;
#include "mojoshader_vulkan_vkfuncs.h"

#define UBO_BUFFER_SIZE 8000000 // 8MB
#define UBO_ACTUAL_SIZE (UBO_BUFFER_SIZE * 2) // Double so we can "rotate" the buffer and unblock main thread

// Internal struct defs...

typedef struct MOJOSHADER_vkShader
{
    const MOJOSHADER_parseData *parseData;
    uint16_t tag;
    uint32_t refcount;
} MOJOSHADER_vkShader;

typedef struct MOJOSHADER_vkProgram
{
    VkShaderModule vertexModule;
    VkShaderModule pixelModule;
    MOJOSHADER_vkShader *vertexShader;
    MOJOSHADER_vkShader *pixelShader;
} MOJOSHADER_vkProgram;

typedef struct MOJOSHADER_vkUniformBuffer
{
    VkBuffer buffer;
    VkDeviceMemory deviceMemory;
    VkDeviceSize bufferSize;
    VkDeviceSize dynamicOffset;
    VkDeviceSize currentBlockSize;
    VkDeviceSize currentBlockIncrement;
    uint8_t *mapPointer;
} MOJOSHADER_vkUniformBuffer;

// Error state...
static char error_buffer[1024] = { '\0' };

static void set_error(const char *str)
{
    snprintf(error_buffer, sizeof (error_buffer), "%s", str);
} // set_error

static inline void out_of_memory(void)
{
    set_error("out of memory");
} // out_of_memory

// Max entries for each register file type
#define MAX_REG_FILE_F 8192
#define MAX_REG_FILE_I 2047
#define MAX_REG_FILE_B 2047

typedef struct MOJOSHADER_vkContext
{
    VkInstance *instance;
    VkPhysicalDevice *physical_device;
    VkDevice *logical_device;
    PFN_vkGetInstanceProcAddr instance_proc_lookup;
    PFN_vkGetDeviceProcAddr device_proc_lookup;
    uint32_t graphics_queue_family_index;
    uint32_t maxUniformBufferRange;
    uint32_t minUniformBufferOffsetAlignment;

    uint32_t frameIndex;

    MOJOSHADER_malloc malloc_fn;
    MOJOSHADER_free free_fn;
    void *malloc_data;

    // The constant register files...
    // !!! FIXME: Man, it kills me how much memory this takes...
    // !!! FIXME:  ... make this dynamically allocated on demand.
    float vs_reg_file_f[MAX_REG_FILE_F * 4];
    int32_t vs_reg_file_i[MAX_REG_FILE_I * 4];
    uint8_t vs_reg_file_b[MAX_REG_FILE_B * 4];
    float ps_reg_file_f[MAX_REG_FILE_F * 4];
    int32_t ps_reg_file_i[MAX_REG_FILE_I * 4];
    uint8_t ps_reg_file_b[MAX_REG_FILE_B * 4];

    MOJOSHADER_vkUniformBuffer *vertUboBuffer;
    MOJOSHADER_vkUniformBuffer *fragUboBuffer;

    MOJOSHADER_vkProgram *bound_program;
    HashTable *linker_cache;

    // Note that these may not necessarily align with bound_program!
    // We need to store these so effects can have overlapping shaders.
    MOJOSHADER_vkShader *bound_vshader;
    MOJOSHADER_vkShader *bound_pshader;

    #define VULKAN_INSTANCE_FUNCTION(ret, func, params) \
        vkfntype_MOJOSHADER_##func func;
    #define VULKAN_DEVICE_FUNCTION(ret, func, params) \
        vkfntype_MOJOSHADER_##func func;
    #include "mojoshader_vulkan_vkfuncs.h"
} MOJOSHADER_vkContext;

static MOJOSHADER_vkContext *ctx = NULL;
static uint16_t tagCounter = 1;

static uint8_t find_memory_type(
    MOJOSHADER_vkContext *ctx,
	uint32_t typeFilter,
	VkMemoryPropertyFlags properties,
	uint32_t *result
) {
	uint32_t i;
	VkPhysicalDeviceMemoryProperties memoryProperties;
	ctx->vkGetPhysicalDeviceMemoryProperties(*ctx->physical_device, &memoryProperties);

	for (i = 0; i < memoryProperties.memoryTypeCount; i++)
	{
		if ((typeFilter & (1 << i))
		 && (memoryProperties.memoryTypes[i].propertyFlags & properties) == properties)
		{
			*result = i;
			return 1;
		} // if
	} // for

	return 0;
} // find_memory_type

static uint32_t next_highest_offset_alignment(uint32_t offset)
{
    return (
        (offset + ctx->minUniformBufferOffsetAlignment - 1) /
        ctx->minUniformBufferOffsetAlignment *
        ctx->minUniformBufferOffsetAlignment
    );
} // next_highest_offset_alignment

static MOJOSHADER_vkUniformBuffer *create_ubo(MOJOSHADER_vkContext *ctx)
{
    MOJOSHADER_vkUniformBuffer *result = (MOJOSHADER_vkUniformBuffer *) ctx->malloc_fn(
        sizeof(MOJOSHADER_vkUniformBuffer),
        ctx->malloc_data
    );
    VkBufferCreateInfo bufferCreateInfo =
    {
        VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO
    };
    VkMemoryRequirements memoryRequirements;
    VkMemoryAllocateInfo allocateInfo =
    {
        VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO
    };

    bufferCreateInfo.flags = 0;
    bufferCreateInfo.size = UBO_ACTUAL_SIZE;
    bufferCreateInfo.usage = VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT;
    bufferCreateInfo.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
    bufferCreateInfo.queueFamilyIndexCount = 1;
    bufferCreateInfo.pQueueFamilyIndices = &ctx->graphics_queue_family_index;

    ctx->vkCreateBuffer(
        *ctx->logical_device,
        &bufferCreateInfo,
        NULL,
        &result->buffer
    );

    ctx->vkGetBufferMemoryRequirements(
        *ctx->logical_device,
        result->buffer,
        &memoryRequirements
    );

    allocateInfo.allocationSize = UBO_ACTUAL_SIZE;

    if (!find_memory_type(ctx,
                          memoryRequirements.memoryTypeBits,
                          VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
                          &allocateInfo.memoryTypeIndex))
    {
        set_error("failed to find suitable memory type for UBO memory");
        return NULL;
    } // if

    ctx->vkAllocateMemory(*ctx->logical_device,
                          &allocateInfo,
                          NULL,
                          &result->deviceMemory
    );

    ctx->vkBindBufferMemory(*ctx->logical_device,
                            result->buffer,
                            result->deviceMemory,
                            0
    );

    ctx->vkMapMemory(*ctx->logical_device,
                     result->deviceMemory,
                     0,
                     UBO_ACTUAL_SIZE,
                     0,
                     (void**) &result->mapPointer
    );

    result->bufferSize = UBO_ACTUAL_SIZE;
    result->currentBlockSize = 0;
    result->currentBlockIncrement = 0;
    result->dynamicOffset = 0;

    return result;
} // create_ubo

static uint32_t uniform_data_size(MOJOSHADER_vkShader *shader)
{
    int32_t i;
    int32_t buflen = 0;
    const int32_t uniformSize = 16; // Yes, even the bool registers
    for (i = 0; i < shader->parseData->uniform_count; i++)
    {
        const int32_t arrayCount = shader->parseData->uniforms[i].array_count;
        buflen += (arrayCount ? arrayCount : 1) * uniformSize;
    } // for

    return buflen;
} // uniform_data_size

static VkBuffer get_uniform_buffer(MOJOSHADER_vkShader *shader)
{
    if (shader == NULL || shader->parseData->uniform_count == 0)
        return VK_NULL_HANDLE;

    if (shader->parseData->shader_type == MOJOSHADER_TYPE_VERTEX)
        return ctx->vertUboBuffer->buffer;
    else
        return ctx->fragUboBuffer->buffer;
} // get_uniform_buffer

static VkDeviceSize get_uniform_offset(MOJOSHADER_vkShader *shader)
{
    if (shader == NULL || shader->parseData->uniform_count == 0)
        return 0;

    if (shader->parseData->shader_type == MOJOSHADER_TYPE_VERTEX)
        return ctx->vertUboBuffer->dynamicOffset;
    else
        return ctx->fragUboBuffer->dynamicOffset;
} // get_uniform_offset

static VkDeviceSize get_uniform_size(MOJOSHADER_vkShader *shader)
{
    if (shader == NULL || shader->parseData->uniform_count == 0)
        return 0;

    if (shader->parseData->shader_type == MOJOSHADER_TYPE_VERTEX)
        return ctx->vertUboBuffer->currentBlockSize;
    else
        return ctx->fragUboBuffer->currentBlockSize;
} // get_uniform_size

static void update_uniform_buffer(MOJOSHADER_vkShader *shader)
{
    int32_t i, j;
    int32_t offset;
    uint8_t *contents;
    uint32_t *contentsI;
    float *regF; int *regI; uint8_t *regB;
    MOJOSHADER_vkUniformBuffer *ubo;

    if (shader == NULL || shader->parseData->uniform_count == 0)
        return;

    if (shader->parseData->shader_type == MOJOSHADER_TYPE_VERTEX)
    {
        regF = ctx->vs_reg_file_f;
        regI = ctx->vs_reg_file_i;
        regB = ctx->vs_reg_file_b;

        ubo = ctx->vertUboBuffer;
    } // if
    else
    {
        regF = ctx->ps_reg_file_f;
        regI = ctx->ps_reg_file_i;
        regB = ctx->ps_reg_file_b;

        ubo = ctx->fragUboBuffer;
    } // else

    ubo->dynamicOffset += ubo->currentBlockIncrement;

    ubo->currentBlockSize = next_highest_offset_alignment(uniform_data_size(shader));
    ubo->currentBlockIncrement = ubo->currentBlockSize;

    if (ubo->dynamicOffset + ubo->currentBlockSize >= ubo->bufferSize * ctx->frameIndex)
    {
        set_error("UBO overflow!!");
    } // if

    contents = ubo->mapPointer + ubo->dynamicOffset;

    offset = 0;
    for (i = 0; i < shader->parseData->uniform_count; i++)
    {
        const int32_t index = shader->parseData->uniforms[i].index;
        const int32_t arrayCount = shader->parseData->uniforms[i].array_count;
        const int32_t size = arrayCount ? arrayCount : 1;

        switch (shader->parseData->uniforms[i].type)
        {
            case MOJOSHADER_UNIFORM_FLOAT:
                memcpy(
                    contents + offset,
                    &regF[4 * index],
                    size * 16
                );
                break;

            case MOJOSHADER_UNIFORM_INT:
                memcpy(
                    contents + offset,
                    &regI[4 * index],
                    size * 16
                );
                break;

            case MOJOSHADER_UNIFORM_BOOL:
                contentsI = (uint32_t *) (contents + offset);
                for (j = 0; j < size; j++)
                    contentsI[j * 4] = regB[index + j];
                break;

            default:
                set_error(
                    "SOMETHING VERY WRONG HAPPENED WHEN UPDATING UNIFORMS"
                );
                assert(0);
                break;
        } // switch

        offset += size * 16;
    } // for

} // update_uniform_buffer

static void lookup_entry_points(MOJOSHADER_vkContext *ctx)
{
    #define VULKAN_INSTANCE_FUNCTION(ret, func, params) \
        ctx->func = (vkfntype_MOJOSHADER_##func) ctx->instance_proc_lookup(*ctx->instance, #func);
    #define VULKAN_DEVICE_FUNCTION(ret, func, params) \
        ctx->func = (vkfntype_MOJOSHADER_##func) ctx->device_proc_lookup(*ctx->logical_device, #func);
    #include "mojoshader_vulkan_vkfuncs.h"
} // lookup_entry_points

static int shader_bytecode_len(MOJOSHADER_vkShader *shader)
{
    return shader->parseData->output_len - sizeof(SpirvPatchTable);
} // shader_bytecode_len

static VkShaderModule compile_shader(MOJOSHADER_vkShader *shader)
{
    VkResult result;
    VkShaderModule module;
    VkShaderModuleCreateInfo shaderModuleCreateInfo =
    {
        VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO
    };

    shaderModuleCreateInfo.flags = 0;
    shaderModuleCreateInfo.codeSize = shader_bytecode_len(shader);
    shaderModuleCreateInfo.pCode = (uint32_t*) shader->parseData->output;

    result = ctx->vkCreateShaderModule(
        *ctx->logical_device,
        &shaderModuleCreateInfo,
        NULL,
        &module
    );

    if (result != VK_SUCCESS)
    {
        // FIXME: should display VK error code
        set_error("Error when creating VkShaderModule");
        ctx->vkDestroyShaderModule(
            *ctx->logical_device,
            module,
            NULL
        );
        return VK_NULL_HANDLE;
    } // if

    return module;
} // compile_shader

typedef struct
{
    MOJOSHADER_vkShader *vertex;
    MOJOSHADER_vkShader *fragment;
} BoundShaders;

static uint32_t hash_shaders(const void *sym, void *data)
{
    (void) data;
    const BoundShaders *s = (const BoundShaders *) sym;
    const uint16_t v = (s->vertex) ? s->vertex->tag : 0;
    const uint16_t f = (s->fragment) ? s->fragment->tag : 0;
    return ((uint32_t) v << 16) | (uint32_t) f;
} // hash_shaders

static int match_shaders(const void *_a, const void *_b, void *data)
{
    (void) data;
    const BoundShaders *a = (const BoundShaders *) _a;
    const BoundShaders *b = (const BoundShaders *) _b;

    const uint16_t av = (a->vertex) ? a->vertex->tag : 0;
    const uint16_t bv = (b->vertex) ? b->vertex->tag : 0;
    if (av != bv)
        return 0;

    const uint16_t af = (a->fragment) ? a->fragment->tag : 0;
    const uint16_t bf = (b->fragment) ? b->fragment->tag : 0;
    if (af != bf)
        return 0;

    return 1;
} // match_shaders

static void nuke_shaders(const void *key, const void *value, void *data)
{
    (void) data;
    ctx->free_fn((void *) key, ctx->malloc_data); // this was a BoundShaders struct.
    MOJOSHADER_vkDeleteProgram((MOJOSHADER_vkProgram *) value);
} // nuke_shaders

// Public API

MOJOSHADER_vkContext *MOJOSHADER_vkCreateContext(
    VkInstance *instance,
    VkPhysicalDevice *physical_device,
    VkDevice *logical_device,
    PFN_MOJOSHADER_vkGetInstanceProcAddr instance_lookup,
    PFN_MOJOSHADER_vkGetDeviceProcAddr device_lookup,
    unsigned int graphics_queue_family_index,
    unsigned int max_uniform_buffer_range,
    unsigned int min_uniform_buffer_offset_alignment,
    MOJOSHADER_malloc m, MOJOSHADER_free f,
    void *malloc_d
) {
    MOJOSHADER_vkContext* resultCtx;

    if (m == NULL) m = MOJOSHADER_internal_malloc;
    if (f == NULL) f = MOJOSHADER_internal_free;

    resultCtx = (MOJOSHADER_vkContext *) m(sizeof(MOJOSHADER_vkContext), malloc_d);
    if (resultCtx == NULL)
    {
        out_of_memory();
        goto init_fail;
    } // if

    memset(resultCtx, '\0', sizeof(MOJOSHADER_vkContext));
    resultCtx->malloc_fn = m;
    resultCtx->free_fn = f;
    resultCtx->malloc_data = malloc_d;

    resultCtx->instance = (VkInstance*) instance;
    resultCtx->physical_device = (VkPhysicalDevice*) physical_device;
    resultCtx->logical_device = (VkDevice*) logical_device;
    resultCtx->instance_proc_lookup = (PFN_vkGetInstanceProcAddr) instance_lookup;
    resultCtx->device_proc_lookup = (PFN_vkGetDeviceProcAddr) device_lookup;
    resultCtx->frameIndex = 0;
    resultCtx->graphics_queue_family_index = graphics_queue_family_index;
    resultCtx->maxUniformBufferRange = max_uniform_buffer_range;
    resultCtx->minUniformBufferOffsetAlignment = min_uniform_buffer_offset_alignment;

    lookup_entry_points(resultCtx);

    resultCtx->vertUboBuffer = create_ubo(resultCtx);
    resultCtx->fragUboBuffer = create_ubo(resultCtx);

    return resultCtx;

init_fail:
    if (resultCtx != NULL)
        f(resultCtx, malloc_d);
    return NULL;
} // MOJOSHADER_vkCreateContext

void MOJOSHADER_vkMakeContextCurrent(MOJOSHADER_vkContext *_ctx)
{
    ctx = _ctx;
} // MOJOSHADER_vkMakeContextCurrent

void MOJOSHADER_vkDestroyContext(MOJOSHADER_vkContext *_ctx)
{
    MOJOSHADER_vkContext *current_ctx = ctx;
    ctx = _ctx;

    MOJOSHADER_vkBindProgram(NULL);
    if (ctx->linker_cache)
        hash_destroy(ctx->linker_cache);

    ctx->vkDestroyBuffer(*ctx->logical_device,
                         ctx->vertUboBuffer->buffer,
                         NULL);

    ctx->vkDestroyBuffer(*ctx->logical_device,
                         ctx->fragUboBuffer->buffer,
                         NULL);

    ctx->vkFreeMemory(*ctx->logical_device,
                      ctx->vertUboBuffer->deviceMemory,
                      NULL);

    ctx->vkFreeMemory(*ctx->logical_device,
                      ctx->fragUboBuffer->deviceMemory,
                      NULL);

    ctx->free_fn(ctx->vertUboBuffer, ctx->malloc_data);
    ctx->free_fn(ctx->fragUboBuffer, ctx->malloc_data);

    ctx->free_fn(ctx, ctx->malloc_data);

    ctx = ((current_ctx == _ctx) ? NULL : current_ctx);
} // MOJOSHADER_vkDestroyContext

MOJOSHADER_vkShader *MOJOSHADER_vkCompileShader(
    const char *mainfn,
    const unsigned char *tokenbuf,
    const unsigned int bufsize,
    const MOJOSHADER_swizzle *swiz,
    const unsigned int swizcount,
    const MOJOSHADER_samplerMap *smap,
    const unsigned int smapcount
) {
    MOJOSHADER_vkShader *shader;

    const MOJOSHADER_parseData *pd = MOJOSHADER_parse(
        "spirv", mainfn,
        tokenbuf, bufsize,
        swiz, swizcount,
        smap, smapcount,
        ctx->malloc_fn,
        ctx->free_fn,
        ctx->malloc_data
    );

    if (pd->error_count > 0)
    {
        set_error(pd->errors[0].error);
        goto parse_shader_fail;
    } // if

    shader = (MOJOSHADER_vkShader *) ctx->malloc_fn(sizeof(MOJOSHADER_vkShader), ctx->malloc_data);
    if (shader == NULL)
    {
        out_of_memory();
        goto parse_shader_fail;
    } // if

    shader->parseData = pd;
    shader->refcount = 1;
    shader->tag = tagCounter++;
    return shader;

parse_shader_fail:
    MOJOSHADER_freeParseData(pd);
    if (shader != NULL)
        ctx->free_fn(shader, ctx->malloc_data);
    return NULL;
} // MOJOSHADER_vkCompileShader

void MOJOSHADER_vkShaderAddRef(MOJOSHADER_vkShader *shader)
{
    if (shader != NULL)
        shader->refcount++;
} // MOJOShader_vkShaderAddRef

void MOJOSHADER_vkDeleteShader(MOJOSHADER_vkShader *shader)
{
    if (shader != NULL)
    {
        if (shader->refcount > 1)
            shader->refcount--;
        else
        {
            // See if this was bound as an unlinked program anywhere...
            if (ctx->linker_cache)
            {
                const void *key = NULL;
                void *iter = NULL;
                int morekeys = hash_iter_keys(ctx->linker_cache, &key, &iter);
                while (morekeys)
                {
                    const BoundShaders *shaders = (const BoundShaders *) key;
                    // Do this here so we don't confuse the iteration by removing...
                    morekeys = hash_iter_keys(ctx->linker_cache, &key, &iter);
                    if ((shaders->vertex == shader) || (shaders->fragment == shader))
                    {
                        // Deletes the linked program
                        hash_remove(ctx->linker_cache, shaders);
                    } // if
                } // while
            } // if

            MOJOSHADER_freeParseData(shader->parseData);
            ctx->free_fn(shader, ctx->malloc_data);
        } // else
    } // if
} // MOJOSHADER_vkDeleteShader

const MOJOSHADER_parseData *MOJOSHADER_vkGetShaderParseData(
    MOJOSHADER_vkShader *shader
) {
    return (shader != NULL) ? shader->parseData : NULL;
} // MOJOSHADER_vkGetShaderParseData

void MOJOSHADER_vkDeleteProgram(MOJOSHADER_vkProgram *p)
{
    if (p->vertexModule != VK_NULL_HANDLE)
        ctx->vkDestroyShaderModule(*ctx->logical_device, p->vertexModule, NULL);
    if (p->pixelModule != VK_NULL_HANDLE)
        ctx->vkDestroyShaderModule(*ctx->logical_device, p->pixelModule, NULL);
    ctx->free_fn(p, ctx->malloc_data);
} // MOJOSHADER_vkDeleteProgram

MOJOSHADER_vkProgram *MOJOSHADER_vkLinkProgram(MOJOSHADER_vkShader *vshader,
                                               MOJOSHADER_vkShader *pshader)
{
    MOJOSHADER_vkProgram *result;

    if ((vshader == NULL) || (pshader == NULL)) // Both shaders MUST exist!
        return NULL;

    result = (MOJOSHADER_vkProgram *) ctx->malloc_fn(sizeof (MOJOSHADER_vkProgram),
                                                     ctx->malloc_data);
    if (result == NULL)
    {
        out_of_memory();
        return NULL;
    } // if

    MOJOSHADER_spirv_link_attributes(vshader->parseData, pshader->parseData);
    result->vertexModule = compile_shader(vshader);
    result->pixelModule = compile_shader(pshader);
    result->vertexShader = vshader;
    result->pixelShader = pshader;

    if (result->vertexModule == VK_NULL_HANDLE
     || result->pixelModule == VK_NULL_HANDLE)
    {
        MOJOSHADER_vkDeleteProgram(result);
        return NULL;
    }
    return result;
} // MOJOSHADER_vkLinkProgram

void MOJOSHADER_vkBindProgram(MOJOSHADER_vkProgram *p)
{
    ctx->bound_program = p;
} // MOJOSHADER_vkBindProgram

void MOJOSHADER_vkBindShaders(MOJOSHADER_vkShader *vshader,
                              MOJOSHADER_vkShader *pshader)
{
    if (ctx->linker_cache == NULL)
    {
        ctx->linker_cache = hash_create(NULL, hash_shaders, match_shaders,
                                        nuke_shaders, 0, ctx->malloc_fn,
                                        ctx->free_fn, ctx->malloc_data);

        if (ctx->linker_cache == NULL)
        {
            out_of_memory();
            return;
        } // if
    } // if

    MOJOSHADER_vkProgram *program = NULL;
    BoundShaders shaders;
    shaders.vertex = vshader;
    shaders.fragment = pshader;

    ctx->bound_vshader = vshader;
    ctx->bound_pshader = pshader;

    const void *val = NULL;
    if (hash_find(ctx->linker_cache, &shaders, &val))
        program = (MOJOSHADER_vkProgram *) val;
    else
    {
        program = MOJOSHADER_vkLinkProgram(vshader, pshader);
        if (program == NULL)
            return;

        BoundShaders *item = (BoundShaders *) ctx->malloc_fn(sizeof (BoundShaders),
                                                             ctx->malloc_data);
        if (item == NULL)
        {
            MOJOSHADER_vkDeleteProgram(program);
            return;
        } // if

        memcpy(item, &shaders, sizeof (BoundShaders));
        if (hash_insert(ctx->linker_cache, item, program) != 1)
        {
            ctx->free_fn(item, ctx->malloc_data);
            MOJOSHADER_vkDeleteProgram(program);
            out_of_memory();
            return;
        } // if
    } // else

    assert(program != NULL);
    ctx->bound_program = program;
} // MOJOSHADER_vkBindShaders

void MOJOSHADER_vkGetBoundShaders(MOJOSHADER_vkShader **vshader,
                                  MOJOSHADER_vkShader **pshader)
{
    if (vshader != NULL)
    {
        if (ctx->bound_program != NULL)
            *vshader = ctx->bound_program->vertexShader;
        else
            *vshader = ctx->bound_vshader; // In case a pshader isn't set yet
    } // if
    if (pshader != NULL)
    {
        if (ctx->bound_program != NULL)
            *pshader = ctx->bound_program->pixelShader;
        else
            *pshader = ctx->bound_pshader; // In case a vshader isn't set yet
    } // if
} // MOJOSHADER_vkGetBoundShaders

void MOJOSHADER_vkMapUniformBufferMemory(float **vsf, int **vsi, unsigned char **vsb,
                                         float **psf, int **psi, unsigned char **psb)
{
    *vsf = ctx->vs_reg_file_f;
    *vsi = ctx->vs_reg_file_i;
    *vsb = ctx->vs_reg_file_b;
    *psf = ctx->ps_reg_file_f;
    *psi = ctx->ps_reg_file_i;
    *psb = ctx->ps_reg_file_b;
} // MOJOSHADER_vkMapUniformBufferMemory

void MOJOSHADER_vkUnmapUniformBufferMemory()
{
    if (ctx->bound_program == NULL)
        return; // Ignore buffer updates until we have a real program linked
    update_uniform_buffer(ctx->bound_program->vertexShader);
    update_uniform_buffer(ctx->bound_program->pixelShader);
} // MOJOSHADER_vkUnmapUniformBufferMemory

void MOJOSHADER_vkGetUniformBuffers(VkBuffer *vbuf, unsigned long long *voff, unsigned long long *vsize,
                                    VkBuffer *pbuf, unsigned long long *poff, unsigned long long *psize)
{
    assert(ctx->bound_program != NULL);
    *vbuf = get_uniform_buffer(ctx->bound_program->vertexShader);
    *voff = get_uniform_offset(ctx->bound_program->vertexShader);
    *vsize = get_uniform_size(ctx->bound_program->vertexShader);
    *pbuf = get_uniform_buffer(ctx->bound_program->pixelShader);
    *poff = get_uniform_offset(ctx->bound_program->pixelShader);
    *psize = get_uniform_size(ctx->bound_program->pixelShader);
} // MOJOSHADER_vkGetUniformBuffers

void MOJOSHADER_vkEndFrame()
{
    ctx->frameIndex = (ctx->frameIndex + 1) % 2;

    // Reset counters
    // Offset by size of buffer to simulate "rotating" the buffers
    ctx->vertUboBuffer->dynamicOffset = UBO_BUFFER_SIZE * ctx->frameIndex;
    ctx->vertUboBuffer->currentBlockIncrement = 0;
    ctx->fragUboBuffer->dynamicOffset = UBO_BUFFER_SIZE * ctx->frameIndex;
    ctx->fragUboBuffer->currentBlockIncrement = 0;
} // MOJOSHADER_VkEndFrame

int MOJOSHADER_vkGetVertexAttribLocation(MOJOSHADER_vkShader *vert,
                                         MOJOSHADER_usage usage, int index)
{
    int32_t i;
    if (vert == NULL)
        return -1;

    for (i = 0; i < vert->parseData->attribute_count; i++)
    {
        if (vert->parseData->attributes[i].usage == usage &&
            vert->parseData->attributes[i].index == index)
        {
            return i;
        } // if
    } // for

    // failure
    return -1;
} //MOJOSHADER_vkGetVertexAttribLocation

void MOJOSHADER_vkGetShaderModules(VkShaderModule *vmodule,
                                   VkShaderModule *pmodule)
{
    assert(ctx->bound_program != NULL);
    if (vmodule != NULL)
        *vmodule = ctx->bound_program->vertexModule;
    if (pmodule != NULL)
        *pmodule = ctx->bound_program->pixelModule;
} //MOJOSHADER_vkGetShaderModules

const char *MOJOSHADER_vkGetError(void)
{
    return error_buffer;
} // MOJOSHADER_vkGetError

#endif /* SUPPORT_PROFILE_SPIRV */

// end of mojoshader_vulkan.c ...
