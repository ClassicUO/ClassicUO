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

#ifdef USE_SDL3 /* Private define, for now */

#include <SDL3/SDL.h>

/* SDL_shadercross API */

typedef enum SDL_ShaderCross_ShaderStage
{
   SDL_SHADERCROSS_SHADERSTAGE_VERTEX,
   SDL_SHADERCROSS_SHADERSTAGE_FRAGMENT,
   SDL_SHADERCROSS_SHADERSTAGE_COMPUTE
} SDL_ShaderCross_ShaderStage;

typedef struct SDL_ShaderCross_SPIRV_Info
{
    const Uint8 *bytecode;                     /**< The SPIRV bytecode. */
    size_t bytecode_size;                      /**< The length of the SPIRV bytecode. */
    const char *entrypoint;                    /**< The entry point function name for the shader in UTF-8. */
    SDL_ShaderCross_ShaderStage shader_stage;  /**< The shader stage to transpile the shader with. */

    SDL_PropertiesID props;                    /**< A properties ID for extensions. Should be 0 if no extensions are needed. */
} SDL_ShaderCross_SPIRV_Info;

typedef struct SDL_ShaderCross_GraphicsShaderMetadata SDL_ShaderCross_GraphicsShaderMetadata;

typedef SDL_GPUShaderFormat (SDLCALL *PFN_SDL_ShaderCross_GetSPIRVShaderFormats)(void);
typedef SDL_ShaderCross_GraphicsShaderMetadata *(SDLCALL *PFN_SDL_ShaderCross_ReflectGraphicsSPIRV)(
    const Uint8 *bytecode,
    size_t bytecode_size,
    SDL_PropertiesID props);
typedef SDL_GPUShader *(SDLCALL *PFN_SDL_ShaderCross_CompileGraphicsShaderFromSPIRV)(
    SDL_GPUDevice *device,
    const SDL_ShaderCross_SPIRV_Info *info,
    const SDL_ShaderCross_GraphicsShaderMetadata *metadata,
    SDL_PropertiesID props);

static SDL_SharedObject *SDL_shadercross_lib = NULL;
static PFN_SDL_ShaderCross_GetSPIRVShaderFormats SDL_ShaderCross_GetSPIRVShaderFormats = NULL;
static PFN_SDL_ShaderCross_ReflectGraphicsSPIRV SDL_ShaderCross_ReflectGraphicsSPIRV = NULL;
static PFN_SDL_ShaderCross_CompileGraphicsShaderFromSPIRV SDL_ShaderCross_CompileGraphicsShaderFromSPIRV = NULL;

#ifdef _WIN32
#define SDL_SHADERCROSS_LIB_NAME "SDL3_shadercross.dll"
#elif defined(__APPLE__)
#define SDL_SHADERCROSS_LIB_NAME "libSDL3_shadercross.0.dylib"
#else
#define SDL_SHADERCROSS_LIB_NAME "libSDL3_shadercross.so.0"
#endif

/* Max entries for each register file type */
#define MAX_REG_FILE_F 8192
#define MAX_REG_FILE_I 2047
#define MAX_REG_FILE_B 2047

/* The destination shader format to use */
static SDL_GPUShaderFormat shader_format =
#ifdef __APPLE__
    SDL_GPU_SHADERFORMAT_MSL;
#else
    SDL_GPU_SHADERFORMAT_SPIRV;
#endif

typedef struct ShaderEntry
{
    uint64_t hash;
    uint32_t offset;
    uint32_t size;
} ShaderEntry;

typedef struct ShaderBlob
{
    uint64_t hash;
    void* binary;
} ShaderBlob;

struct MOJOSHADER_sdlContext
{
    SDL_GPUDevice *device;
    const char *profile;

    MOJOSHADER_malloc malloc_fn;
    MOJOSHADER_free free_fn;
    void *malloc_data;

    /* The constant register files...
     * !!! FIXME: Man, it kills me how much memory this takes...
     * !!! FIXME:  ... make this dynamically allocated on demand.
     */
    float vs_reg_file_f[MAX_REG_FILE_F * 4];
    int32_t vs_reg_file_i[MAX_REG_FILE_I * 4];
    uint8_t vs_reg_file_b[MAX_REG_FILE_B * 4];
    float ps_reg_file_f[MAX_REG_FILE_F * 4];
    int32_t ps_reg_file_i[MAX_REG_FILE_I * 4];
    uint8_t ps_reg_file_b[MAX_REG_FILE_B * 4];

    uint8_t *uniform_staging;
    uint32_t uniform_staging_length;

    MOJOSHADER_sdlShaderData *bound_vshader_data;
    MOJOSHADER_sdlShaderData *bound_pshader_data;
    MOJOSHADER_sdlProgram *bound_program;
    HashTable *linker_cache;
};

struct MOJOSHADER_sdlShaderData
{
    const MOJOSHADER_parseData *parseData;
    uint16_t tag;
    uint32_t refcount;
    uint32_t samplerSlots;
    int32_t uniformBufferSize;
};

struct MOJOSHADER_sdlProgram
{
    SDL_GPUShader *vertexShader;
    SDL_GPUShader *pixelShader;
    MOJOSHADER_sdlShaderData *vertexShaderData;
    MOJOSHADER_sdlShaderData *pixelShaderData;
};

/* Error state... */

static char error_buffer[1024] = { '\0' };

static void set_error(const char *str)
{
    snprintf(error_buffer, sizeof (error_buffer), "%s", str);
} // set_error

static inline void out_of_memory(void)
{
    set_error("out of memory");
} // out_of_memory

/* Internals */

typedef struct LinkedShaderData
{
    MOJOSHADER_sdlShaderData *vertex;
    MOJOSHADER_sdlShaderData *fragment;
    MOJOSHADER_vertexAttribute vertexAttributes[16];
    uint32_t vertexAttributeCount;
} LinkedShaderData;

static uint32_t hash_shaders(const void *sym, void *data)
{
    (void) data;
    const LinkedShaderData *s = (const LinkedShaderData *) sym;
    const uint32_t HASH_FACTOR = 31;
    uint32_t hash = s->vertexAttributeCount;
    for (uint32_t i = 0; i < s->vertexAttributeCount; i += 1)
    {
        hash = hash * HASH_FACTOR + s->vertexAttributes[i].usage;
        hash = hash * HASH_FACTOR + s->vertexAttributes[i].usageIndex;
        hash = hash * HASH_FACTOR + s->vertexAttributes[i].vertexElementFormat;
    }
    hash = hash * HASH_FACTOR + s->vertex->tag;
    hash = hash * HASH_FACTOR + s->fragment->tag;
    return hash;
} // hash_shaders

static int match_shaders(const void *_a, const void *_b, void *data)
{
    (void) data;
    const LinkedShaderData *a = (const LinkedShaderData *) _a;
    const LinkedShaderData *b = (const LinkedShaderData *) _b;

    const uint16_t av = (a->vertex) ? a->vertex->tag : 0;
    const uint16_t bv = (b->vertex) ? b->vertex->tag : 0;
    if (av != bv)
        return 0;

    const uint16_t af = (a->fragment) ? a->fragment->tag : 0;
    const uint16_t bf = (b->fragment) ? b->fragment->tag : 0;
    if (af != bf)
        return 0;

    if (a->vertexAttributeCount != b->vertexAttributeCount)
        return 0;

    for (uint32_t i = 0; i < a->vertexAttributeCount; i += 1)
    {
        if (a->vertexAttributes[i].usage != b->vertexAttributes[i].usage)
        {
            return 0;
        }
        if (a->vertexAttributes[i].usageIndex != b->vertexAttributes[i].usageIndex)
        {
            return 0;
        }
        if (a->vertexAttributes[i].vertexElementFormat != b->vertexAttributes[i].vertexElementFormat)
        {
            return 0;
        }
    }

    return 1;
} // match_shaders

static void nuke_shaders(
    const void *_ctx,
    const void *key,
    const void *value,
    void *data
) {
    MOJOSHADER_sdlContext *ctx = (MOJOSHADER_sdlContext *) _ctx;
    (void) data;
    ctx->free_fn((void *) key, ctx->malloc_data); // this was a LinkedShaderData struct.
    MOJOSHADER_sdlDeleteProgram(ctx, (MOJOSHADER_sdlProgram *) value);
} // nuke_shaders

static uint8_t update_uniform_buffer(
    MOJOSHADER_sdlContext *ctx,
    SDL_GPUCommandBuffer *cb,
    MOJOSHADER_sdlShaderData *shader,
    float *regF,
    int *regI,
    uint8_t *regB
) {
    int32_t i, j;
    int32_t offset;
    uint32_t *contentsI;

    if (shader->uniformBufferSize > ctx->uniform_staging_length)
    {
        ctx->free_fn(ctx->uniform_staging, ctx->malloc_data);
        ctx->uniform_staging = (uint8_t*) ctx->malloc_fn(shader->uniformBufferSize, ctx->malloc_data);
        ctx->uniform_staging_length = shader->uniformBufferSize;
    } // if

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
                    ctx->uniform_staging + offset,
                    &regF[4 * index],
                    size * 16
                );
                break;

            case MOJOSHADER_UNIFORM_INT:
                memcpy(
                    ctx->uniform_staging + offset,
                    &regI[4 * index],
                    size * 16
                );
                break;

            case MOJOSHADER_UNIFORM_BOOL:
                contentsI = (uint32_t *) (ctx->uniform_staging + offset);
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

    return 1; // FIXME: Return 0 when uniform data is unchanged
} // update_uniform_buffer

/* Public API */

unsigned int MOJOSHADER_sdlGetShaderFormats(void)
{
    int ret = shader_format;
    if (SDL_ShaderCross_GetSPIRVShaderFormats != NULL)
        ret |= SDL_ShaderCross_GetSPIRVShaderFormats();
    else
    {
        // We only care about this on Windows, hardcode the DLL name :shrug:
        SDL_shadercross_lib = SDL_LoadObject(SDL_SHADERCROSS_LIB_NAME);
        if (SDL_shadercross_lib != NULL)
        {
            SDL_ShaderCross_GetSPIRVShaderFormats = (PFN_SDL_ShaderCross_GetSPIRVShaderFormats) SDL_LoadFunction(
                SDL_shadercross_lib,
                "SDL_ShaderCross_GetSPIRVShaderFormats"
            );
            ret |= SDL_ShaderCross_GetSPIRVShaderFormats();
            SDL_ShaderCross_GetSPIRVShaderFormats = NULL;
            SDL_UnloadObject(SDL_shadercross_lib);
            SDL_shadercross_lib = NULL;
        } // if
    } // else
    return ret;
} // MOJOSHADER_sdlGetShaderFormats

MOJOSHADER_sdlContext *MOJOSHADER_sdlCreateContext(
    SDL_GPUDevice *device,
    MOJOSHADER_malloc m,
    MOJOSHADER_free f,
    void *malloc_d
) {
    MOJOSHADER_sdlContext* resultCtx;

    if (m == NULL) m = MOJOSHADER_internal_malloc;
    if (f == NULL) f = MOJOSHADER_internal_free;

    resultCtx = (MOJOSHADER_sdlContext*) m(sizeof(MOJOSHADER_sdlContext), malloc_d);
    if (resultCtx == NULL)
    {
        out_of_memory();
        goto init_fail;
    } // if

    SDL_memset(resultCtx, '\0', sizeof(MOJOSHADER_sdlContext));
    resultCtx->device = device;
    resultCtx->malloc_fn = m;
    resultCtx->free_fn = f;
    resultCtx->malloc_data = malloc_d;

        resultCtx->profile = (shader_format == SDL_GPU_SHADERFORMAT_SPIRV) ? "spirv" : "metal";

    // We only care about ShaderCross if the device doesn't natively support the profile
    if (!(SDL_GetGPUShaderFormats(device) & shader_format))
    {
        SDL_shadercross_lib = SDL_LoadObject(SDL_SHADERCROSS_LIB_NAME);
        if (SDL_shadercross_lib != NULL)
        {
            SDL_ShaderCross_GetSPIRVShaderFormats = (PFN_SDL_ShaderCross_GetSPIRVShaderFormats) SDL_LoadFunction(
                SDL_shadercross_lib,
                "SDL_ShaderCross_GetSPIRVShaderFormats"
            );
            SDL_ShaderCross_ReflectGraphicsSPIRV = (PFN_SDL_ShaderCross_ReflectGraphicsSPIRV) SDL_LoadFunction(
                SDL_shadercross_lib,
                "SDL_ShaderCross_ReflectGraphicsSPIRV"
            );
            SDL_ShaderCross_CompileGraphicsShaderFromSPIRV = (PFN_SDL_ShaderCross_CompileGraphicsShaderFromSPIRV) SDL_LoadFunction(
                SDL_shadercross_lib,
                "SDL_ShaderCross_CompileGraphicsShaderFromSPIRV"
            );
        } // if
    } // if

    return resultCtx;

init_fail:
    if (resultCtx != NULL)
        f(resultCtx, malloc_d);
    return NULL;
} // MOJOSHADER_sdlCreateContext

const char *MOJOSHADER_sdlGetError(
    MOJOSHADER_sdlContext *ctx
) {
    return error_buffer;
} // MOJOSHADER_sdlGetError

void MOJOSHADER_sdlDestroyContext(
    MOJOSHADER_sdlContext *ctx
) {
    uint32_t i;

    if (SDL_shadercross_lib != NULL)
    {
        SDL_ShaderCross_GetSPIRVShaderFormats = NULL;
        SDL_ShaderCross_ReflectGraphicsSPIRV = NULL;
        SDL_ShaderCross_CompileGraphicsShaderFromSPIRV = NULL;
        SDL_UnloadObject(SDL_shadercross_lib);
        SDL_shadercross_lib = NULL;
    } // if

    if (ctx->linker_cache)
        hash_destroy(ctx->linker_cache, ctx);

    ctx->free_fn(ctx->uniform_staging, ctx->malloc_data);

    ctx->free_fn(ctx, ctx->malloc_data);
} // MOJOSHADER_sdlDestroyContext

static uint16_t shaderTagCounter = 1;

MOJOSHADER_sdlShaderData *MOJOSHADER_sdlCompileShader(
    MOJOSHADER_sdlContext *ctx,
    const char *mainfn,
    const unsigned char *tokenbuf,
    const unsigned int bufsize,
    const MOJOSHADER_swizzle *swiz,
    const unsigned int swizcount,
    const MOJOSHADER_samplerMap *smap,
    const unsigned int smapcount
) {
    MOJOSHADER_sdlShaderData *shader = NULL;;
    int maxSamplerIndex = 0;
    int i;

    const MOJOSHADER_parseData *pd = MOJOSHADER_parse(
        ctx->profile, mainfn,
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

    shader = (MOJOSHADER_sdlShaderData*) ctx->malloc_fn(sizeof(MOJOSHADER_sdlShaderData), ctx->malloc_data);
    if (shader == NULL)
    {
        out_of_memory();
        goto parse_shader_fail;
    } // if

    shader->parseData = pd;
    shader->refcount = 1;
    shader->tag = shaderTagCounter++;

    /* XNA allows empty shader slots in the middle, so we have to find the actual max binding index */
    for (i = 0; i < pd->sampler_count; i += 1)
    {
        if (pd->samplers[i].index > maxSamplerIndex)
        {
            maxSamplerIndex = pd->samplers[i].index;
        }
    }

    shader->samplerSlots = (uint32_t) maxSamplerIndex + 1;

    shader->uniformBufferSize = 0;
    for (i = 0; i < pd->uniform_count; i++)
    {
        shader->uniformBufferSize += SDL_max(pd->uniforms[i].array_count, 1);
    } // for
    shader->uniformBufferSize *= 16; // Yes, even the bool registers are this size

    return shader;

parse_shader_fail:
    MOJOSHADER_freeParseData(pd);
    if (shader != NULL)
        ctx->free_fn(shader, ctx->malloc_data);
    return NULL;
} // MOJOSHADER_sdlCompileShader

static MOJOSHADER_sdlProgram *compile_program(
    MOJOSHADER_sdlContext *ctx,
    MOJOSHADER_sdlShaderData *vshader,
    MOJOSHADER_sdlShaderData *pshader)
{
    SDL_GPUShaderCreateInfo createInfo;
    SDL_ShaderCross_SPIRV_Info crossCreateInfo;
    SDL_ShaderCross_GraphicsShaderMetadata *whoCares;
    MOJOSHADER_sdlProgram *program = (MOJOSHADER_sdlProgram*) ctx->malloc_fn(sizeof(MOJOSHADER_sdlProgram),
                                                                             ctx->malloc_data);
    if (program == NULL)
    {
        out_of_memory();
        return NULL;
    } // if

    char *vshaderSource = (char*) vshader->parseData->output;
    char *pshaderSource = (char*) pshader->parseData->output;
    size_t vshaderCodeSize = vshader->parseData->output_len;
    size_t pshaderCodeSize = pshader->parseData->output_len;

    // Last-minute fixups before we actually invoke the compiler
    if (shader_format == SDL_GPU_SHADERFORMAT_SPIRV)
    {
        vshaderCodeSize -= sizeof(SpirvPatchTable);
        pshaderCodeSize -= sizeof(SpirvPatchTable);
    } // if
    else if (shader_format == SDL_GPU_SHADERFORMAT_MSL)
    {
        // Handle texcoord0 -> point_coord conversion
        if (strstr((char*) vshader->parseData->output, "[[point_size]]"))
        {
            pshaderSource = (char *) ctx->malloc_fn(strlen(pshader->parseData->output) + 1, ctx->malloc_data);
            if (!pshaderSource)
            {
                out_of_memory();
                return NULL;
            }
            strcpy(pshaderSource, pshader->parseData->output);

            // !!! FIXME: This assumes all texcoord0 attributes in the effect are
            // !!! FIXME:  actually point coords! It ain't necessarily so! -caleb
            const char *repl = "[[  point_coord  ]]";
            char *ptr;
            while ((ptr = strstr(pshaderSource, "[[user(texcoord0)]]")))
            {
                memcpy(ptr, repl, strlen(repl));

                // "float4" -> "float2"
                int spaces = 0;
                while (spaces < 2)
                    if (*(ptr--) == ' ')
                        spaces++;
                *ptr = '2';
            } // while
        } // if
    } // else if

    if (SDL_ShaderCross_CompileGraphicsShaderFromSPIRV != NULL)
    {
        crossCreateInfo.bytecode = (const Uint8*) vshaderSource;
        crossCreateInfo.bytecode_size = vshaderCodeSize;
        crossCreateInfo.entrypoint = vshader->parseData->mainfn;
        crossCreateInfo.shader_stage = SDL_SHADERCROSS_SHADERSTAGE_VERTEX;
        crossCreateInfo.props = 0;

        whoCares = SDL_ShaderCross_ReflectGraphicsSPIRV(
            crossCreateInfo.bytecode,
            crossCreateInfo.bytecode_size,
            0
        );

        program->vertexShader = SDL_ShaderCross_CompileGraphicsShaderFromSPIRV(
            ctx->device,
            &crossCreateInfo,
            whoCares,
            0
        );

        SDL_free(whoCares);
    } // if
    else
    {
        SDL_zero(createInfo);
        createInfo.code = (const Uint8*) vshaderSource;
        createInfo.code_size = vshaderCodeSize;
        createInfo.entrypoint = vshader->parseData->mainfn;
        createInfo.format = shader_format;
        createInfo.stage = SDL_GPU_SHADERSTAGE_VERTEX;
        createInfo.num_samplers = vshader->samplerSlots;
        createInfo.num_uniform_buffers = 1;

        program->vertexShader = SDL_CreateGPUShader(
            ctx->device,
            &createInfo
        );
    } // else

    if (program->vertexShader == NULL)
    {
        set_error(SDL_GetError());
        ctx->free_fn(program, ctx->malloc_data);
        return NULL;
    } // if

    if (SDL_ShaderCross_CompileGraphicsShaderFromSPIRV != NULL)
    {
        crossCreateInfo.bytecode = (const Uint8*) pshaderSource;
        crossCreateInfo.bytecode_size = pshaderCodeSize;
        crossCreateInfo.entrypoint = pshader->parseData->mainfn;
        crossCreateInfo.shader_stage = SDL_SHADERCROSS_SHADERSTAGE_FRAGMENT;
        crossCreateInfo.props = 0;

        whoCares = SDL_ShaderCross_ReflectGraphicsSPIRV(
            crossCreateInfo.bytecode,
            crossCreateInfo.bytecode_size,
            0
        );

        program->pixelShader = SDL_ShaderCross_CompileGraphicsShaderFromSPIRV(
            ctx->device,
            &crossCreateInfo,
            whoCares,
            0
        );

        SDL_free(whoCares);
    } // if
    else
    {
        createInfo.code = (const Uint8*) pshaderSource;
        createInfo.code_size = pshaderCodeSize;
        createInfo.entrypoint = pshader->parseData->mainfn;
        createInfo.format = shader_format;
        createInfo.stage = SDL_GPU_SHADERSTAGE_FRAGMENT;
        createInfo.num_samplers = pshader->samplerSlots;

        program->pixelShader = SDL_CreateGPUShader(
            ctx->device,
            &createInfo
        );
    } // else

    if (program->pixelShader == NULL)
    {
        set_error(SDL_GetError());
        SDL_ReleaseGPUShader(ctx->device, program->vertexShader);
        ctx->free_fn(program, ctx->malloc_data);
        return NULL;
    } // if

    if (pshaderSource != pshader->parseData->output)
        ctx->free_fn(pshaderSource, ctx->malloc_data);

    return program;
} // compile_program

MOJOSHADER_sdlProgram *MOJOSHADER_sdlLinkProgram(
    MOJOSHADER_sdlContext *ctx,
    MOJOSHADER_vertexAttribute *vertexAttributes,
    int vertexAttributeCount
) {
    MOJOSHADER_sdlProgram *program = NULL;

    MOJOSHADER_sdlShaderData *vshader = ctx->bound_vshader_data;
    MOJOSHADER_sdlShaderData *pshader = ctx->bound_pshader_data;

    if ((vshader == NULL) || (pshader == NULL)) /* Both shaders MUST exist! */
        return NULL;

    if (ctx->linker_cache == NULL)
    {
        ctx->linker_cache = hash_create(NULL, hash_shaders, match_shaders,
                                        nuke_shaders, 0, ctx->malloc_fn,
                                        ctx->free_fn, ctx->malloc_data);

        if (ctx->linker_cache == NULL)
        {
            out_of_memory();
            return NULL;
        } // if
    } // if

    LinkedShaderData shaders;
    shaders.vertex = vshader;
    shaders.fragment = pshader;
    memset(shaders.vertexAttributes, 0, sizeof(MOJOSHADER_vertexAttribute) * 16);
    shaders.vertexAttributeCount = vertexAttributeCount;
    for (int i = 0; i < vertexAttributeCount; i += 1)
    {
        shaders.vertexAttributes[i] = vertexAttributes[i];
    }

    const void *val = NULL;

    if (hash_find(ctx->linker_cache, &shaders, &val))
    {
        ctx->bound_program = (MOJOSHADER_sdlProgram *) val;
        return ctx->bound_program;
    }

    if (shader_format == SDL_GPU_SHADERFORMAT_SPIRV)
        MOJOSHADER_linkSPIRVShaders(vshader->parseData, pshader->parseData,
                                    vertexAttributes, vertexAttributeCount);
    program = compile_program(ctx, vshader, pshader);

    if (program == NULL)
        return NULL;

    program->vertexShaderData = vshader;
    program->pixelShaderData = pshader;

    LinkedShaderData *item = (LinkedShaderData *) ctx->malloc_fn(sizeof (LinkedShaderData),
                                                         ctx->malloc_data);

    if (item == NULL)
    {
        MOJOSHADER_sdlDeleteProgram(ctx, program);
    }

    memcpy(item, &shaders, sizeof(LinkedShaderData));
    if (hash_insert(ctx->linker_cache, item, program) != 1)
    {
        ctx->free_fn(item, ctx->malloc_data);
        MOJOSHADER_sdlDeleteProgram(ctx, program);
        out_of_memory();
        return NULL;
    }

    ctx->bound_program = program;
    return program;
} // MOJOSHADER_sdlLinkProgram

void MOJOSHADER_sdlShaderAddRef(MOJOSHADER_sdlShaderData *shader)
{
    if (shader != NULL)
        shader->refcount++;
} // MOJOSHADER_sdlShaderAddRef

void MOJOSHADER_sdlDeleteShader(
    MOJOSHADER_sdlContext *ctx,
    MOJOSHADER_sdlShaderData *shader
) {
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
                    const LinkedShaderData *shaders = (const LinkedShaderData *) key;
                    // Do this here so we don't confuse the iteration by removing...
                    morekeys = hash_iter_keys(ctx->linker_cache, &key, &iter);
                    if ((shaders->vertex == shader) || (shaders->fragment == shader))
                    {
                        // Deletes the linked program
                        hash_remove(ctx->linker_cache, shaders, ctx);
                    } // if
                } // while
            } // if

            MOJOSHADER_freeParseData(shader->parseData);
            ctx->free_fn(shader, ctx->malloc_data);
        } // else
    } // if
} // MOJOSHADER_sdlDeleteShader

const MOJOSHADER_parseData *MOJOSHADER_sdlGetShaderParseData(
    MOJOSHADER_sdlShaderData *shader
) {
    return (shader != NULL) ? shader->parseData : NULL;
} // MOJOSHADER_sdlGetShaderParseData

void MOJOSHADER_sdlDeleteProgram(
    MOJOSHADER_sdlContext *ctx,
    MOJOSHADER_sdlProgram *p
) {
    if (ctx->bound_program == p)
        ctx->bound_program = NULL;
    if (p->vertexShader != NULL)
        SDL_ReleaseGPUShader(ctx->device, p->vertexShader);
    if (p->pixelShader != NULL)
        SDL_ReleaseGPUShader(ctx->device, p->pixelShader);
    ctx->free_fn(p, ctx->malloc_data);
} // MOJOSHADER_sdlDeleteProgram

void MOJOSHADER_sdlBindProgram(
    MOJOSHADER_sdlContext *ctx,
    MOJOSHADER_sdlProgram *p
) {
    ctx->bound_program = p;
} // MOJOSHADER_sdlBindProgram

void MOJOSHADER_sdlBindShaders(
    MOJOSHADER_sdlContext *ctx,
    MOJOSHADER_sdlShaderData *vshader,
    MOJOSHADER_sdlShaderData *pshader
) {
    MOJOSHADER_sdlProgram *program = NULL;
    ctx->bound_vshader_data = vshader;
    ctx->bound_pshader_data = pshader;
} // MOJOSHADER_sdlBindShaders

void MOJOSHADER_sdlGetBoundShaderData(
    MOJOSHADER_sdlContext *ctx,
    MOJOSHADER_sdlShaderData **vshaderdata,
    MOJOSHADER_sdlShaderData **pshaderdata
) {
    if (vshaderdata != NULL)
    {
        *vshaderdata = ctx->bound_vshader_data;
    } // if
    if (pshaderdata != NULL)
    {
        *pshaderdata = ctx->bound_pshader_data;
    } // if
} // MOJOSHADER_sdlGetBoundShaderData

void MOJOSHADER_sdlMapUniformBufferMemory(
    MOJOSHADER_sdlContext *ctx,
    float **vsf, int **vsi, unsigned char **vsb,
    float **psf, int **psi, unsigned char **psb
) {
    *vsf = ctx->vs_reg_file_f;
    *vsi = ctx->vs_reg_file_i;
    *vsb = ctx->vs_reg_file_b;
    *psf = ctx->ps_reg_file_f;
    *psi = ctx->ps_reg_file_i;
    *psb = ctx->ps_reg_file_b;
} // MOJOSHADER_sdlMapUniformBufferMemory

void MOJOSHADER_sdlUnmapUniformBufferMemory(MOJOSHADER_sdlContext *ctx)
{
    /* no-op! real work done in sdlUpdateUniformBuffers */
} // MOJOSHADER_sdlUnmapUniformBufferMemory

int MOJOSHADER_sdlGetUniformBufferSize(MOJOSHADER_sdlShaderData *shader)
{
    if (shader == NULL)
        return 0;
    return shader->uniformBufferSize;
} // MOJOSHADER_sdlGetUniformBufferSize

void MOJOSHADER_sdlUpdateUniformBuffers(MOJOSHADER_sdlContext *ctx,
                                        SDL_GPUCommandBuffer *cb)
{
    if (MOJOSHADER_sdlGetUniformBufferSize(ctx->bound_program->vertexShaderData) > 0)
    {
        if (update_uniform_buffer(ctx, cb, ctx->bound_program->vertexShaderData,
                                  ctx->vs_reg_file_f,
                                  ctx->vs_reg_file_i,
                                  ctx->vs_reg_file_b))
        {
            SDL_PushGPUVertexUniformData(
                cb,
                0,
                ctx->uniform_staging,
                ctx->bound_program->vertexShaderData->uniformBufferSize
            );
        } // if
    } // if
    if (MOJOSHADER_sdlGetUniformBufferSize(ctx->bound_program->pixelShaderData) > 0)
    {
        if (update_uniform_buffer(ctx, cb, ctx->bound_program->pixelShaderData,
                                  ctx->ps_reg_file_f,
                                  ctx->ps_reg_file_i,
                                  ctx->ps_reg_file_b))
        {
            SDL_PushGPUFragmentUniformData(
                cb,
                0,
                ctx->uniform_staging,
                ctx->bound_program->pixelShaderData->uniformBufferSize
            );
        } // if
    } // if
} // MOJOSHADER_sdlUpdateUniformBuffers

int MOJOSHADER_sdlGetVertexAttribLocation(
    MOJOSHADER_sdlShaderData *vert,
    MOJOSHADER_usage usage, int index
) {
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
} // MOJOSHADER_sdlGetVertexAttribLocation

void MOJOSHADER_sdlGetShaders(
    MOJOSHADER_sdlContext *ctx,
    SDL_GPUShader **vshader,
    SDL_GPUShader **pshader
) {
    assert(ctx->bound_program != NULL);
    if (vshader != NULL)
        *vshader = ctx->bound_program->vertexShader;
    if (pshader != NULL)
        *pshader = ctx->bound_program->pixelShader;
} // MOJOSHADER_sdlGetShaders

unsigned int MOJOSHADER_sdlGetSamplerSlots(MOJOSHADER_sdlShaderData *shader)
{
    assert(shader != NULL);
    return shader->samplerSlots;
} // MOJOSHADER_sdlGetSamplerSlots

#endif // USE_SDL3
