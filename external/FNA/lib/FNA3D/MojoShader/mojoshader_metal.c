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

#if (defined(__APPLE__) && defined(__MACH__))
#define PLATFORM_APPLE 1
#endif /* (defined(__APPLE__) && defined(__MACH__)) */

typedef struct MOJOSHADER_mtlShader
{
    const MOJOSHADER_parseData *parseData;
    uint32 refcount;
    void *handle; // MTLFunction*
} MOJOSHADER_mtlShader;

// profile-specific implementations...

#if SUPPORT_PROFILE_METAL && PLATFORM_APPLE
#ifdef MOJOSHADER_EFFECT_SUPPORT

#include "TargetConditionals.h"
#include <objc/message.h>
#define msg     ((void* (*)(void*, void*))objc_msgSend)
#define msg_s   ((void* (*)(void*, void*, const char*))objc_msgSend)
#define msg_p   ((void* (*)(void*, void*, void*))objc_msgSend)
#define msg_uu  ((void* (*)(void*, void*, uint64, uint64))objc_msgSend)
#define msg_ppp ((void* (*)(void*, void*, void*, void*, void*))objc_msgSend)

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

// Max entries for each register file type...
#define MAX_REG_FILE_F 8192
#define MAX_REG_FILE_I 2047
#define MAX_REG_FILE_B 2047

typedef struct MOJOSHADER_mtlContext
{
    // Allocators...
    MOJOSHADER_malloc malloc_fn;
    MOJOSHADER_free free_fn;
    void *malloc_data;

    // The constant register files...
    // !!! FIXME: Man, it kills me how much memory this takes...
    // !!! FIXME:  ... make this dynamically allocated on demand.
    float vs_reg_file_f[MAX_REG_FILE_F * 4];
    int vs_reg_file_i[MAX_REG_FILE_I * 4];
    uint8 vs_reg_file_b[MAX_REG_FILE_B];
    float ps_reg_file_f[MAX_REG_FILE_F * 4];
    int ps_reg_file_i[MAX_REG_FILE_I * 4];
    uint8 ps_reg_file_b[MAX_REG_FILE_B];

    // Pointer to the active MTLDevice.
    void* device;

    // The uniform MTLBuffer shared between all shaders in the context.
    void *ubo;

    // The current offsets into the UBO, per shader.
    int vertexUniformOffset;
    int pixelUniformOffset;
    int totalUniformOffset;

    // The currently bound shaders.
    MOJOSHADER_mtlShader *vertexShader;
    MOJOSHADER_mtlShader *pixelShader;

    // Objective-C Selectors
    void* classNSString;
    void* selAlloc;
    void* selContents;
    void* selInitWithUTF8String;
    void* selLength;
    void* selLocalizedDescription;
    void* selNewBufferWithLength;
    void* selNewFunctionWithName;
    void* selNewLibraryWithSource;
    void* selRelease;
    void* selUTF8String;
} MOJOSHADER_mtlContext;

static MOJOSHADER_mtlContext *ctx = NULL;

/* Uniform buffer utilities */

static inline int next_highest_alignment(int n)
{
    #if TARGET_OS_IOS || TARGET_OS_TV || TARGET_OS_SIMULATOR
    int align = 16;
    #else
    // !!! FIXME: Will Apple Silicon Macs have a different minimum alignment?
    int align = 256;
    #endif

    return align * ((n + align - 1) / align);
} // next_highest_alignment

static void update_uniform_buffer(MOJOSHADER_mtlShader *shader)
{
    if (shader == NULL || shader->parseData->uniform_count == 0)
        return;

    float *regF; int *regI; uint8 *regB;
    if (shader->parseData->shader_type == MOJOSHADER_TYPE_VERTEX)
    {
        ctx->vertexUniformOffset = ctx->totalUniformOffset;
        regF = ctx->vs_reg_file_f;
        regI = ctx->vs_reg_file_i;
        regB = ctx->vs_reg_file_b;
    } // if
    else
    {
        ctx->pixelUniformOffset = ctx->totalUniformOffset;
        regF = ctx->ps_reg_file_f;
        regI = ctx->ps_reg_file_i;
        regB = ctx->ps_reg_file_b;
    } // else

    void *contents = msg(ctx->ubo, ctx->selContents) + ctx->totalUniformOffset;
    int offset = 0;
    for (int i = 0; i < shader->parseData->uniform_count; i++)
    {
        if (shader->parseData->uniforms[i].constant)
            continue;

        int idx = shader->parseData->uniforms[i].index;
        int arrayCount = shader->parseData->uniforms[i].array_count;

        void *src = NULL;
        int size = arrayCount ? arrayCount : 1;

        switch (shader->parseData->uniforms[i].type)
        {
            case MOJOSHADER_UNIFORM_FLOAT:
                src = &regF[4 * idx];
                size *= 16;
                break;

            case MOJOSHADER_UNIFORM_INT:
                src = &regI[4 * idx];
                size *= 16;
                break;

            case MOJOSHADER_UNIFORM_BOOL:
                src = &regB[idx];
                break;

            default:
                assert(0); // This should never happen.
                break;
        } // switch

        memcpy(contents + offset, src, size);
        offset += size;
    } // for

    ctx->totalUniformOffset = next_highest_alignment(ctx->totalUniformOffset + offset);
    if (ctx->totalUniformOffset >= (int) msg(ctx->ubo, ctx->selLength))
    {
        // !!! FIXME: Is there a better way to handle this?
        assert(0 && "Uniform data exceeded the size of the buffer!");
    } // if
} // update_uniform_buffer

/* Public API */

MOJOSHADER_mtlContext *MOJOSHADER_mtlCreateContext(void* mtlDevice,
                                    MOJOSHADER_malloc m, MOJOSHADER_free f,
                                    void *malloc_d)
{
    MOJOSHADER_mtlContext *retval = NULL;
    MOJOSHADER_mtlContext *current_ctx = ctx;
    int i;

    ctx = NULL;

    if (m == NULL) m = MOJOSHADER_internal_malloc;
    if (f == NULL) f = MOJOSHADER_internal_free;

    ctx = (MOJOSHADER_mtlContext *) m(sizeof (MOJOSHADER_mtlContext), malloc_d);
    if (ctx == NULL)
    {
        out_of_memory();
        goto init_fail;
    } // if

    memset(ctx, '\0', sizeof (MOJOSHADER_mtlContext));
    ctx->malloc_fn = m;
    ctx->free_fn = f;
    ctx->malloc_data = malloc_d;

    // Initialize the Metal state
    ctx->device = mtlDevice;

    // Grab references to Objective-C selectors
    ctx->classNSString = objc_getClass("NSString");
    ctx->selAlloc = sel_registerName("alloc");
    ctx->selContents = sel_registerName("contents");
    ctx->selInitWithUTF8String = sel_registerName("initWithUTF8String:");
    ctx->selLength = sel_registerName("length");
    ctx->selLocalizedDescription = sel_registerName("localizedDescription");
    ctx->selNewBufferWithLength = sel_registerName("newBufferWithLength:options:");
    ctx->selNewFunctionWithName = sel_registerName("newFunctionWithName:");
    ctx->selNewLibraryWithSource = sel_registerName("newLibraryWithSource:options:error:");
    ctx->selRelease = sel_registerName("release");
    ctx->selUTF8String = sel_registerName("UTF8String");

    // Create the uniform buffer
    ctx->ubo = msg_uu(mtlDevice, ctx->selNewBufferWithLength,
                      next_highest_alignment(1000000), 0);

    retval = ctx;
    ctx = current_ctx;
    return retval;

init_fail:
    if (ctx != NULL)
        f(ctx, malloc_d);
    ctx = current_ctx;
    return NULL;
} // MOJOSHADER_mtlCreateContext


void MOJOSHADER_mtlMakeContextCurrent(MOJOSHADER_mtlContext *_ctx)
{
    ctx = _ctx;
} // MOJOSHADER_mtlMakeContextCurrent


void *MOJOSHADER_mtlCompileLibrary(MOJOSHADER_effect *effect)
{
    MOJOSHADER_malloc m = ctx->malloc_fn;
    MOJOSHADER_free f = ctx->free_fn;
    void *d = ctx->malloc_data;

    int i, src_len, src_pos, output_len;
    char *shader_source, *ptr;
    const char *repl;
    MOJOSHADER_effectObject *object;
    MOJOSHADER_mtlShader *shader;
    void *retval, *compileError, *shader_source_ns, *fnname;

    // Count the number of shaders before allocating
    src_len = 0;
    for (i = 0; i < effect->object_count; i++)
    {
        object = &effect->objects[i];
        if (object->type == MOJOSHADER_SYMTYPE_PIXELSHADER
         || object->type == MOJOSHADER_SYMTYPE_VERTEXSHADER)
        {
            if (!object->shader.is_preshader)
            {
                shader = (MOJOSHADER_mtlShader*) object->shader.shader;
                src_len += shader->parseData->output_len;
            } // if
        } // if
    } // for

    // Allocate shader source buffer
    shader_source = (char *) m(src_len + 1, d);
    memset(shader_source, '\0', src_len + 1);
    src_pos = 0;

    // Copy all the source text into the buffer
    for (i = 0; i < effect->object_count; i++)
    {
        object = &effect->objects[i];
        if (object->type == MOJOSHADER_SYMTYPE_PIXELSHADER
         || object->type == MOJOSHADER_SYMTYPE_VERTEXSHADER)
        {
            if (!object->shader.is_preshader)
            {
                shader = (MOJOSHADER_mtlShader*) object->shader.shader;
                memcpy(&shader_source[src_pos], shader->parseData->output,
                                                shader->parseData->output_len);
                src_pos += shader->parseData->output_len;
            } // if
        } // if
    } // for

    // Handle texcoord0 -> point_coord conversion
    if (strstr(shader_source, "[[point_size]]"))
    {
        // !!! FIXME: This assumes all texcoord0 attributes in the effect are
        // !!! FIXME:  actually point coords! It ain't necessarily so! -caleb
        repl = "[[  point_coord  ]]";
        while ((ptr = strstr(shader_source, "[[user(texcoord0)]]")))
        {
            memcpy(ptr, repl, strlen(repl));

            // "float4" -> "float2"
            int spaces = 0;
            while (spaces < 2)
                if (*(ptr--) == ' ')
                    spaces++;
            memcpy(ptr, "2", sizeof(char));
        } // while
    } // if

    // Compile the source into a library
    compileError = NULL;
    shader_source_ns = msg_s(
        msg(ctx->classNSString, ctx->selAlloc),
        ctx->selInitWithUTF8String,
        shader_source
    );
    retval = msg_ppp(ctx->device, ctx->selNewLibraryWithSource,
                        shader_source_ns, NULL, &compileError);
    f(shader_source, d);
    msg(shader_source_ns, ctx->selRelease);

    if (retval == NULL)
    {
        compileError = msg(compileError, ctx->selLocalizedDescription);
        set_error((char*) msg(compileError, ctx->selUTF8String));
        return NULL;
    } // if

    // Run through the shaders again, getting the function handles
    for (i = 0; i < effect->object_count; i++)
    {
        object = &effect->objects[i];
        if (object->type == MOJOSHADER_SYMTYPE_PIXELSHADER
         || object->type == MOJOSHADER_SYMTYPE_VERTEXSHADER)
        {
            if (object->shader.is_preshader)
                continue;

            shader = (MOJOSHADER_mtlShader*) object->shader.shader;
            fnname = msg_s(
                msg(ctx->classNSString, ctx->selAlloc),
                ctx->selInitWithUTF8String,
                shader->parseData->mainfn
            );
            shader->handle = msg_p(
                retval,
                ctx->selNewFunctionWithName,
                fnname
            );
            msg(fnname, ctx->selRelease);
        } // if
    } // for

    return retval;
} // MOJOSHADER_mtlCompileLibrary


MOJOSHADER_mtlShader *MOJOSHADER_mtlCompileShader(const char *mainfn,
                                            const unsigned char *tokenbuf,
                                            const unsigned int bufsize,
                                            const MOJOSHADER_swizzle *swiz,
                                            const unsigned int swizcount,
                                            const MOJOSHADER_samplerMap *smap,
                                            const unsigned int smapcount)
{
    MOJOSHADER_malloc m = ctx->malloc_fn;
    MOJOSHADER_free f = ctx->free_fn;
    void *d = ctx->malloc_data;

    const MOJOSHADER_parseData *pd = MOJOSHADER_parse("metal", mainfn, tokenbuf,
                                                     bufsize, swiz, swizcount,
                                                     smap, smapcount, m, f, d);
    if (pd->error_count > 0)
    {
        // !!! FIXME: put multiple errors in the buffer? Don't use
        // !!! FIXME:  MOJOSHADER_mtlGetError() for this?
        set_error(pd->errors[0].error);
        goto compile_shader_fail;
    } // if

    MOJOSHADER_mtlShader *retval = (MOJOSHADER_mtlShader *) m(sizeof(MOJOSHADER_mtlShader), d);
    if (retval == NULL)
        goto compile_shader_fail;

    retval->parseData = pd;
    retval->refcount = 1;
    retval->handle = NULL; // populated by MOJOSHADER_mtlCompileLibrary

    return retval;

compile_shader_fail:
    MOJOSHADER_freeParseData(retval->parseData);
    f(retval, d);
    return NULL;
} // MOJOSHADER_mtlCompileShader


void MOJOSHADER_mtlShaderAddRef(MOJOSHADER_mtlShader *shader)
{
    if (shader != NULL)
        shader->refcount++;
} // MOJOSHADER_mtlShaderAddRef


const MOJOSHADER_parseData *MOJOSHADER_mtlGetShaderParseData(
                                                MOJOSHADER_mtlShader *shader)
{
    return (shader != NULL) ? shader->parseData : NULL;
} // MOJOSHADER_mtlGetParseData


void MOJOSHADER_mtlBindShaders(MOJOSHADER_mtlShader *vshader,
                               MOJOSHADER_mtlShader *pshader)
{
    // Use the last bound shaders in case of NULL
    if (vshader != NULL)
        ctx->vertexShader = vshader;

    if (pshader != NULL)
        ctx->pixelShader = pshader;
} // MOJOSHADER_mtlBindShaders


void MOJOSHADER_mtlGetBoundShaders(MOJOSHADER_mtlShader **vshader,
                                   MOJOSHADER_mtlShader **pshader)
{
    *vshader = ctx->vertexShader;
    *pshader = ctx->pixelShader;
} // MOJOSHADER_mtlGetBoundShaders


void MOJOSHADER_mtlMapUniformBufferMemory(float **vsf, int **vsi, unsigned char **vsb,
                                          float **psf, int **psi, unsigned char **psb)
{
    *vsf = ctx->vs_reg_file_f;
    *vsi = ctx->vs_reg_file_i;
    *vsb = ctx->vs_reg_file_b;
    *psf = ctx->ps_reg_file_f;
    *psi = ctx->ps_reg_file_i;
    *psb = ctx->ps_reg_file_b;
} // MOJOSHADER_mtlMapUniformBufferMemory


void MOJOSHADER_mtlUnmapUniformBufferMemory()
{
    /* This has nothing to do with unmapping memory
     * and everything to do with updating uniform
     * buffers with the latest parameter contents.
     */
    update_uniform_buffer(ctx->vertexShader);
    update_uniform_buffer(ctx->pixelShader);
} // MOJOSHADER_mtlUnmapUniformBufferMemory


void MOJOSHADER_mtlGetUniformData(void **buf, int *voff, int *poff)
{
    *buf = ctx->ubo;
    *voff = ctx->vertexUniformOffset;
    *poff = ctx->pixelUniformOffset;
} // MOJOSHADER_mtlGetUniformBuffers


void *MOJOSHADER_mtlGetFunctionHandle(MOJOSHADER_mtlShader *shader)
{
    if (shader == NULL)
        return NULL;

    return shader->handle;
} // MOJOSHADER_mtlGetFunctionHandle


void MOJOSHADER_mtlEndFrame()
{
    ctx->totalUniformOffset = 0;
    ctx->vertexUniformOffset = 0;
    ctx->pixelUniformOffset = 0;
} // MOJOSHADER_mtlEndFrame


int MOJOSHADER_mtlGetVertexAttribLocation(MOJOSHADER_mtlShader *vert,
                                          MOJOSHADER_usage usage, int index)
{
    if (vert == NULL)
        return -1;

    for (int i = 0; i < vert->parseData->attribute_count; i++)
    {
        if (vert->parseData->attributes[i].usage == usage &&
            vert->parseData->attributes[i].index == index)
        {
            return i;
        } // if
    } // for

    // failure, couldn't find requested attribute
    return -1;
} // MOJOSHADER_mtlGetVertexAttribLocation


const char *MOJOSHADER_mtlGetError(void)
{
    return error_buffer;
} // MOJOSHADER_mtlGetError


void MOJOSHADER_mtlDeleteLibrary(void *library)
{
    msg(library, ctx->selRelease);
} // MOJOSHADER_mtlDeleteLibrary


void MOJOSHADER_mtlDeleteShader(MOJOSHADER_mtlShader *shader)
{
    if (shader != NULL)
    {
        if (shader->refcount > 1)
            shader->refcount--;
        else
        {
            msg(shader->handle, ctx->selRelease);
            MOJOSHADER_freeParseData(shader->parseData);
            ctx->free_fn(shader, ctx->malloc_data);
        } // else
    } // if
} // MOJOSHADER_mtlDeleteShader


void MOJOSHADER_mtlDestroyContext(MOJOSHADER_mtlContext *_ctx)
{
    MOJOSHADER_mtlContext *current_ctx = ctx;
    ctx = _ctx;

    if (ctx->ubo != NULL)
        msg(ctx->ubo, ctx->selRelease);

    if (ctx != NULL)
        ctx->free_fn(ctx, ctx->malloc_data);
    ctx = ((current_ctx == _ctx) ? NULL : current_ctx);
} // MOJOSHADER_mtlDestroyContext

#endif /* MOJOSHADER_EFFECT_SUPPORT */
#endif /* SUPPORT_PROFILE_METAL && PLATFORM_APPLE */

// end of mojoshader_metal.c ...
