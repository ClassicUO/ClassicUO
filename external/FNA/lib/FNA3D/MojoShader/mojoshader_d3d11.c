/**
 * MojoShader; generate shader programs from bytecode of compiled
 *  Direct3D shaders.
 *
 * Please see the file LICENSE.txt in the source's root directory.
 *
 *  This file written by Ryan C. Gordon.
 */

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN 1
#include <windows.h> // Include this early to avoid SDL conflicts
#endif

#define __MOJOSHADER_INTERNAL__ 1
#include "mojoshader_internal.h"

#if SUPPORT_PROFILE_HLSL

#define D3D11_NO_HELPERS
#define CINTERFACE
#define COBJMACROS
#include <d3d11.h>

/* d3dcompiler DLL */

#ifndef WINAPI_FAMILY_WINRT
#define WINAPI_FAMILY_WINRT 0
#endif
#if WINAPI_FAMILY_WINRT
#include <d3dcompiler.h>
#elif defined(_WIN32)
#define LOAD_D3DCOMPILER LoadLibrary("d3dcompiler_47.dll")
#define UNLOAD_D3DCOMPILER(d) FreeLibrary(d)
#define LOAD_D3DCOMPILE(d) GetProcAddress(d, "D3DCompile")
#else
#define USING_VKD3D
#if defined(__APPLE__)
#define LOAD_D3DCOMPILER dlopen("libvkd3d-utils.1.dylib", RTLD_NOW|RTLD_LOCAL)
#else
#define LOAD_D3DCOMPILER dlopen("libvkd3d-utils.so.1", RTLD_NOW|RTLD_LOCAL)
#endif
#define UNLOAD_D3DCOMPILER(d) dlclose(d)
#define LOAD_D3DCOMPILE(d) dlsym(d, "D3DCompile")
#endif

/* __stdcall declaration, largely taken from vkd3d_windows.h */

#ifdef _WIN32
#define D3DCOMPILER_API WINAPI
#else
# ifdef __stdcall
#  undef __stdcall
# endif
# ifdef __x86_64__
#  define __stdcall __attribute__((ms_abi))
# else
#  if (__GNUC__ > 4) || ((__GNUC__ == 4) && (__GNUC_MINOR__ >= 2)) || defined(__APPLE__)
#   define __stdcall __attribute__((__stdcall__)) __attribute__((__force_align_arg_pointer__))
#  else
#   define __stdcall __attribute__((__stdcall__))
#  endif
# endif
# define D3DCOMPILER_API __stdcall
#endif

/* vkd3d uses stdcall for its ID3D10Blob implementation */

#ifdef USING_VKD3D
typedef struct VKD3DBlob VKD3DBlob;
typedef struct VKD3DBlobVtbl
{
    HRESULT (__stdcall *QueryInterface)(
        VKD3DBlob *This,
        REFIID riid,
        void **ppvObject);
    ULONG (__stdcall *AddRef)(VKD3DBlob *This);
    ULONG (__stdcall *Release)(VKD3DBlob *This);
    void * (__stdcall *GetBufferPointer)(VKD3DBlob *This);
    SIZE_T (__stdcall *GetBufferSize)(VKD3DBlob *This);
} VKD3DBlobVtbl;
struct VKD3DBlob
{
    const VKD3DBlobVtbl *lpVtbl;
};
#define ID3D10Blob VKD3DBlob
#define ID3DBlob VKD3DBlob
#endif // USING_VKD3D

/* D3DCompile signature */

typedef HRESULT(D3DCOMPILER_API *PFN_D3DCOMPILE)(
    LPCVOID pSrcData,
    SIZE_T SrcDataSize,
    LPCSTR pSourceName,
    const D3D_SHADER_MACRO *pDefines,
    ID3DInclude *pInclude,
    LPCSTR pEntrypoint,
    LPCSTR pTarget,
    UINT Flags1,
    UINT Flags2,
    ID3DBlob **ppCode,
    ID3DBlob **ppErrorMsgs
);

// D3DCompile optimization can be overzealous and cause very visible bugs,
//  so we disable it when compiling shaders to preserve correctness.
#define D3D_SKIP_OPT (1 << 2)

/* Error state */

static char error_buffer[1024] = { '\0' };

static void set_error(const char *str)
{
    snprintf(error_buffer, sizeof (error_buffer), "%s", str);
} // set_error

static inline void out_of_memory(void)
{
    set_error("out of memory");
} // out_of_memory

/* Structs */

typedef struct d3d11ShaderMap
{
    void *val;
    union
    {
        struct
        {
            uint64 layoutHash;
            ID3D10Blob *blob;
        } vertex;
        struct
        {
            MOJOSHADER_d3d11Shader *vshader;
        } pixel;
    };
} d3d11ShaderMap;

typedef struct MOJOSHADER_d3d11Shader
{
    const MOJOSHADER_parseData *parseData;
    uint32 refcount;
    ID3D11Buffer *ubo;
    size_t buflen;
    unsigned char *constantData;
    unsigned int mapCapacity;
    unsigned int numMaps;
    d3d11ShaderMap *shaderMaps;
} MOJOSHADER_d3d11Shader;

// Max entries for each register file type...
#define MAX_REG_FILE_F 8192
#define MAX_REG_FILE_I 2047
#define MAX_REG_FILE_B 2047

typedef struct MOJOSHADER_d3d11Context
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

    // Pointer to the active ID3D11Device.
    ID3D11Device *device;

    // Pointer to the ID3D11DeviceContext.
    ID3D11DeviceContext *deviceContext;

    // Currently bound vertex and pixel shaders.
    MOJOSHADER_d3d11Shader *vertexShader;
    MOJOSHADER_d3d11Shader *pixelShader;
    int vertexNeedsBound;
    int pixelNeedsBound;

    // D3DCompile function pointer.
    PFN_D3DCOMPILE D3DCompileFunc;
#if !WINAPI_FAMILY_WINRT
    HMODULE d3dcompilerDLL;
#endif
} MOJOSHADER_d3d11Context;

/* Uniform buffer utilities */

static inline int next_highest_alignment(int n)
{
    const int align = 16;
    return align * ((n + align - 1) / align);
} // next_highest_alignment

static inline void *get_uniform_buffer(MOJOSHADER_d3d11Shader *shader)
{
    return (shader == NULL || shader->ubo == NULL) ? NULL : shader->ubo;
} // get_uniform_buffer

static void update_uniform_buffer(
    MOJOSHADER_d3d11Context *ctx,
    MOJOSHADER_d3d11Shader *shader
) {
    int i, j;
    float *regF; int *regI; uint8 *regB;
    int needsUpdate;
    size_t offset;
    int idx;
    int arrayCount;
    void *src, *dst;
    size_t size;
    int *vecDst;

    if (shader == NULL || shader->ubo == NULL)
        return;

    if (shader->parseData->shader_type == MOJOSHADER_TYPE_VERTEX)
    {
        regF = ctx->vs_reg_file_f;
        regI = ctx->vs_reg_file_i;
        regB = ctx->vs_reg_file_b;
    } // if
    else
    {
        regF = ctx->ps_reg_file_f;
        regI = ctx->ps_reg_file_i;
        regB = ctx->ps_reg_file_b;
    } // else

    // Update the buffer contents
    needsUpdate = 0;
    offset = 0;
    for (i = 0; i < shader->parseData->uniform_count; i++)
    {
        if (shader->parseData->uniforms[i].constant)
            continue;

        idx = shader->parseData->uniforms[i].index;
        arrayCount = shader->parseData->uniforms[i].array_count;

        src = NULL;
        dst = NULL;
        size = arrayCount ? (arrayCount * 16) : 16;

        switch (shader->parseData->uniforms[i].type)
        {
            case MOJOSHADER_UNIFORM_FLOAT:
                src = &regF[4 * idx];
                dst = shader->constantData + offset;
                break;

            case MOJOSHADER_UNIFORM_INT:
                src = &regI[4 * idx];
                dst = shader->constantData + offset;
                break;

            case MOJOSHADER_UNIFORM_BOOL:
                // bool registers are a whole other mess, thanks to alignment.
                // The bool field is an int4 in HLSL 4+, so we have to cast the
                // bool to an int, then skip 3 ints. Super efficient, right?
                vecDst = (int*) (shader->constantData + offset);
                j = 0;
                do
                {
                    if (vecDst[j * 4] != regB[idx + j])
                    {
                        needsUpdate = 1;
                        vecDst[j * 4] = regB[idx + j];
                    } // if
                } while (++j < arrayCount);

                offset += size;
                continue; // Skip the rest, do NOT break!

            default:
                assert(0); // This should never happen.
                break;
        } // switch

        if (memcmp(dst, src, size) != 0)
        {
            memcpy(dst, src, size);
            needsUpdate = 1;
        } // if

        offset += size;
    } // for

    if (needsUpdate)
    {
        // Map the buffer
        D3D11_MAPPED_SUBRESOURCE res;
        ID3D11DeviceContext_Map((ID3D11DeviceContext*) ctx->deviceContext,
                                (ID3D11Resource*) shader->ubo, 0,
                                D3D11_MAP_WRITE_DISCARD, 0, &res);

        // Copy the contents
        memcpy(res.pData, shader->constantData, shader->buflen);

        // Unmap the buffer
        ID3D11DeviceContext_Unmap(
            (ID3D11DeviceContext*) ctx->deviceContext,
            (ID3D11Resource*) shader->ubo,
            0
        );
    } // if
} // update_uniform_buffer

static inline void expand_map(
    MOJOSHADER_d3d11Context *ctx,
    MOJOSHADER_d3d11Shader *shader
) {
    if (shader->numMaps == shader->mapCapacity)
    {
        d3d11ShaderMap *newMap = (d3d11ShaderMap *) ctx->malloc_fn(
            sizeof(d3d11ShaderMap) * shader->mapCapacity * 2,
            ctx->malloc_data
        );
        memcpy(newMap, shader->shaderMaps,
            sizeof(d3d11ShaderMap) * shader->mapCapacity);
        shader->mapCapacity *= 2;
        ctx->free_fn(shader->shaderMaps, ctx->malloc_data);
        shader->shaderMaps = newMap;
        newMap = NULL;
    } // if
} // expand_map

static inline int element_is_uint(DXGI_FORMAT format)
{
    return  format == DXGI_FORMAT_R32G32B32A32_UINT ||
            format == DXGI_FORMAT_R32G32B32_UINT ||
            format == DXGI_FORMAT_R16G16B16A16_UINT ||
            format == DXGI_FORMAT_R32G32_UINT ||
            format == DXGI_FORMAT_R10G10B10A2_UINT ||
            format == DXGI_FORMAT_R8G8B8A8_UINT ||
            format == DXGI_FORMAT_R16G16_UINT ||
            format == DXGI_FORMAT_R32_UINT ||
            format == DXGI_FORMAT_R8G8_UINT ||
            format == DXGI_FORMAT_R16_UINT ||
            format == DXGI_FORMAT_R8_UINT;
} // element_is_uint

static inline int element_is_int(DXGI_FORMAT format)
{
    return  format == DXGI_FORMAT_R32G32B32A32_SINT ||
            format == DXGI_FORMAT_R32G32B32_SINT ||
            format == DXGI_FORMAT_R16G16B16A16_SINT ||
            format == DXGI_FORMAT_R32G32_SINT ||
            format == DXGI_FORMAT_R8G8B8A8_SINT ||
            format == DXGI_FORMAT_R16G16_SINT ||
            format == DXGI_FORMAT_R32_SINT ||
            format == DXGI_FORMAT_R8G8_SINT ||
            format == DXGI_FORMAT_R16_SINT ||
            format == DXGI_FORMAT_R8_SINT;
} // element_is_int

/* Shader Compilation Utilities */

static ID3D11VertexShader *compileVertexShader(
    MOJOSHADER_d3d11Context *ctx,
    MOJOSHADER_d3d11Shader *shader,
    const char *src,
    int src_len,
    ID3D10Blob **blob
) {
    const MOJOSHADER_parseData *pd = shader->parseData;
    HRESULT result = ctx->D3DCompileFunc(src, src_len, pd->mainfn,
                                         NULL, NULL, pd->mainfn, "vs_4_0",
                                         D3D_SKIP_OPT, 0, blob, blob);

    if (result < 0)
    {
        set_error((const char *) ID3D10Blob_GetBufferPointer(*blob));
        ID3D10Blob_Release(*blob);
        return NULL;
    } // if

    void *bytecode = ID3D10Blob_GetBufferPointer(*blob);
    int bytecodeLength = ID3D10Blob_GetBufferSize(*blob);
    ID3D11VertexShader *ret = NULL;
    ID3D11Device_CreateVertexShader(ctx->device, bytecode, bytecodeLength,
                                    NULL, &ret);
    return ret;
} // compileVertexShader

static void replaceVarname(
    MOJOSHADER_d3d11Context *ctx,
    const char *find,
    const char *replace,
    const char **source
) {
    const char *srcbuf = *source;
    size_t find_len = strlen(find);
    size_t replace_len = strlen(replace);

    #define IS_PARTIAL_TOKEN(token) \
        (isalnum(*(token + find_len)) || isalnum(*(token-1)))

    // How many times does `find` occur in the source buffer?
    int count = 0;
    char *ptr = (char *) strstr(srcbuf, find);
    while (ptr != NULL)
    {
        if (!IS_PARTIAL_TOKEN(ptr))
            count++;
        ptr = strstr(ptr + find_len, find);
    } // while

    // How big should we make the new text buffer?
    size_t oldlen = strlen(srcbuf) + 1;
    size_t newlen = oldlen + (count * (replace_len - find_len));

    // Easy case; just find/replace in the original buffer
    if (newlen == oldlen)
    {
        ptr = (char *) strstr(srcbuf, find);
        while (ptr != NULL)
        {
            if (!IS_PARTIAL_TOKEN(ptr))
                memcpy(ptr, replace, replace_len);
            ptr = strstr(ptr + find_len, find);
        } // while
        return;
    } // if

    // Allocate a new buffer
    char *newbuf = (char *) ctx->malloc_fn(newlen, ctx->malloc_data);
    memset(newbuf, '\0', newlen);

    // Find + replace
    char *prev_ptr = (char *) srcbuf;
    char *curr_ptr = (char *) newbuf;
    ptr = (char*) strstr(srcbuf, find);
    while (ptr != NULL)
    {
        memcpy(curr_ptr, prev_ptr, ptr - prev_ptr);
        curr_ptr += ptr - prev_ptr;

        if (!IS_PARTIAL_TOKEN(ptr))
        {
            memcpy(curr_ptr, replace, replace_len);
            curr_ptr += replace_len;
        } // if
        else
        {
            // Don't accidentally eat partial tokens...
            memcpy(curr_ptr, find, find_len);
            curr_ptr += find_len;
        } // else

        prev_ptr = ptr + find_len;
        ptr = strstr(prev_ptr, find);
    } // while

    #undef IS_PARTIAL_TOKEN

    // Copy the remaining part of the source buffer
    memcpy(curr_ptr, prev_ptr, (srcbuf + oldlen) - prev_ptr);

    // Free the source buffer
    ctx->free_fn((void *) srcbuf, ctx->malloc_data);

    // Point the source parameter to the new buffer
    *source = newbuf;
} // replaceVarname

static char *rewritePixelShader(
    MOJOSHADER_d3d11Context *ctx,
    MOJOSHADER_d3d11Shader *vshader,
    MOJOSHADER_d3d11Shader *pshader
) {
    const MOJOSHADER_parseData *vpd = vshader->parseData;
    const MOJOSHADER_parseData *ppd = pshader->parseData;
    const char *_Output = "_Output" ENDLINE_STR "{" ENDLINE_STR;
    const char *_Input = "_Input" ENDLINE_STR "{" ENDLINE_STR;
    const char *vsrc = vpd->output;
    const char *psrc = ppd->output;
    const char *a, *b, *vout, *pstart, *vface, *pend;
    size_t substr_len;
    char *pfinal;

    #define MAKE_STRBUF(buf) \
        substr_len = b - a; \
        buf = (const char *) ctx->malloc_fn(substr_len + 1, ctx->malloc_data); \
        memset((void *) buf, '\0', substr_len + 1); \
        memcpy((void *) buf, a, substr_len);

    // Copy the vertex function's output struct into a buffer
    a = strstr(vsrc, _Output) + strlen(_Output);
    b = a;
    while (*(b++) != '}');
    b--;
    MAKE_STRBUF(vout)

    // Split up the pixel shader text...

    // ...everything up to the input contents...
    a = psrc;
    b = strstr(psrc, _Input) + strlen(_Input);
    MAKE_STRBUF(pstart)

    // ...everything after the input contents.
    a = b;
    while (*(a++) != '}');
    a--;
    while (*(b++) != '\0');
    MAKE_STRBUF(pend)

    // Find matching semantics
    int i, j;
    int vfaceidx = -1;
    const char *pvarname, *vvarname;
    for (i = 0; i < ppd->attribute_count; i++)
    {
        int found_matching_vs_output_for_ps_input = 0;
        for (j = 0; j < vpd->output_count; j++)
        {
            if (ppd->attributes[i].usage == vpd->outputs[j].usage &&
                ppd->attributes[i].index == vpd->outputs[j].index)
            {
                found_matching_vs_output_for_ps_input = 1;
                pvarname = ppd->attributes[i].name;
                vvarname = vpd->outputs[j].name;
                if (strcmp(pvarname, vvarname) != 0)
                    replaceVarname(ctx, pvarname, vvarname, &pend);
            } // if
            else if (strcmp(ppd->attributes[i].name, "vPos") == 0 &&
                     vpd->outputs[j].usage == MOJOSHADER_USAGE_POSITION &&
                     vpd->outputs[j].index == 0)
            {
                found_matching_vs_output_for_ps_input = 1;
                pvarname = ppd->attributes[i].name;
                vvarname = vpd->outputs[j].name;
                if (strcmp(pvarname, vvarname) != 0)
                    replaceVarname(ctx, pvarname, vvarname, &pend);
            } // else if
        } // for

        if (strcmp(ppd->attributes[i].name, "vFace") == 0)
            vfaceidx = i;

        // A vertex shader that doesn't properly initialize all its outputs
        //  can produce a situation where vpd->outputs is missing a matching
        //  entry for the PS's inputs, even though fxc will happily compile
        //  both shaders together as a technique in FX mode
        // I don't know how to fix this yet, but a workaround is to
        //  correct your shader to zero-initialize all its outputs -kg
        assert(found_matching_vs_output_for_ps_input);
    } // for

    // Special handling for VFACE
    vface = (vfaceidx != -1) ? "\tbool m_vFace : SV_IsFrontFace;\n" : "";

    // Concatenate the shader pieces together
    substr_len = strlen(pstart) + strlen(vout) + strlen(vface) + strlen(pend);
    pfinal = (char *) ctx->malloc_fn(substr_len + 1, ctx->malloc_data);
    memset((void *) pfinal, '\0', substr_len + 1);
    memcpy(pfinal, pstart, strlen(pstart));
    memcpy(pfinal + strlen(pstart), vout, strlen(vout));
    memcpy(pfinal + strlen(pstart) + strlen(vout), vface, strlen(vface));
    memcpy(pfinal + strlen(pstart) + strlen(vout) + strlen(vface), pend, strlen(pend));

    // Free the temporary buffers
    ctx->free_fn((void *) vout, ctx->malloc_data);
    ctx->free_fn((void *) pstart, ctx->malloc_data);
    ctx->free_fn((void *) pend, ctx->malloc_data);

    #undef MAKE_STRBUF

    return pfinal;
} // spliceVertexShaderInput

static ID3D11PixelShader *compilePixelShader(
    MOJOSHADER_d3d11Context *ctx,
    MOJOSHADER_d3d11Shader *vshader,
    MOJOSHADER_d3d11Shader *pshader
) {
    ID3D11PixelShader *retval = NULL;
    const char *source;
    ID3DBlob *blob;
    HRESULT result;
    int needs_free;

    if (pshader->parseData->attribute_count > 0)
    {
        source = rewritePixelShader(ctx, vshader, pshader);
        needs_free = 1;
    } // if
    else
    {
        source = pshader->parseData->output;
        needs_free = 0;
    } // else

    result = ctx->D3DCompileFunc(source, strlen(source),
                                 pshader->parseData->mainfn, NULL, NULL,
                                 pshader->parseData->mainfn, "ps_4_0",
                                 D3D_SKIP_OPT, 0, &blob, &blob);

    if (needs_free)
        ctx->free_fn((void *) source, ctx->malloc_data);

    if (result < 0)
    {
        set_error((const char *) ID3D10Blob_GetBufferPointer(blob));
        return NULL;
    } // if

    ID3D11Device_CreatePixelShader(ctx->device,
                                   ID3D10Blob_GetBufferPointer(blob),
                                   ID3D10Blob_GetBufferSize(blob),
                                   NULL, &retval);

    ID3D10Blob_Release(blob);
    return retval;
} // compilePixelShader

/* Public API */

MOJOSHADER_d3d11Context* MOJOSHADER_d3d11CreateContext(
    void *device,
    void *deviceContext,
    MOJOSHADER_malloc m,
    MOJOSHADER_free f,
    void *malloc_d
) {
    MOJOSHADER_d3d11Context *ctx;
    PFN_D3DCOMPILE compileFunc;

#if WINAPI_FAMILY_WINRT
    compileFunc = D3DCompile;
#else
    HMODULE compileDLL;
    compileDLL = LOAD_D3DCOMPILER;
    if (compileDLL == NULL)
        return NULL;
    compileFunc = (PFN_D3DCOMPILE) LOAD_D3DCOMPILE(compileDLL);
    if (compileFunc == NULL)
    {
        UNLOAD_D3DCOMPILER(compileDLL);
        return NULL;
    } // if
#ifdef USING_VKD3D
    unsigned int major, minor;
    const char *(*vkd3d_shader_get_version)(unsigned int *, unsigned int *);
    vkd3d_shader_get_version = (const char *(*)(unsigned int *, unsigned int *)) dlsym(compileDLL, "vkd3d_shader_get_version");
    if (vkd3d_shader_get_version == NULL)
    {
        UNLOAD_D3DCOMPILER(compileDLL);
        return NULL;
    }
    vkd3d_shader_get_version(&major, &minor);
    if (!((major > 1) || (major == 1 && minor >= 10)))
    {
        UNLOAD_D3DCOMPILER(compileDLL);
        return NULL;
    }
#endif
#endif

    if (m == NULL) m = MOJOSHADER_internal_malloc;
    if (f == NULL) f = MOJOSHADER_internal_free;

    ctx = (MOJOSHADER_d3d11Context *) m(sizeof(MOJOSHADER_d3d11Context), malloc_d);
    if (ctx == NULL)
    {
        out_of_memory();
        goto init_fail;
    } // if

    memset(ctx, '\0', sizeof (MOJOSHADER_d3d11Context));
    ctx->malloc_fn = m;
    ctx->free_fn = f;
    ctx->malloc_data = malloc_d;

    // Store references to the D3D device and immediate context
    ctx->device = (ID3D11Device*) device;
    ctx->deviceContext = (ID3D11DeviceContext*) deviceContext;

    // Store the d3dcompiler info
    ctx->D3DCompileFunc = compileFunc;
#if !WINAPI_FAMILY_WINRT
    ctx->d3dcompilerDLL = compileDLL;
#endif

    return ctx;

init_fail:
    if (ctx != NULL)
        f(ctx, malloc_d);
    return NULL;
} // MOJOSHADER_d3d11CreateContext

void MOJOSHADER_d3d11DestroyContext(MOJOSHADER_d3d11Context *ctx)
{
#if !WINAPI_FAMILY_WINRT
    UNLOAD_D3DCOMPILER(ctx->d3dcompilerDLL);
#endif
    ctx->free_fn(ctx, ctx->malloc_data);
} // MOJOSHADER_d3d11DestroyContext

MOJOSHADER_d3d11Shader *MOJOSHADER_d3d11CompileShader(MOJOSHADER_d3d11Context *ctx,
                                                      const char *mainfn,
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
    int i;

    const MOJOSHADER_parseData *pd = MOJOSHADER_parse("hlsl", mainfn, tokenbuf,
                                                     bufsize, swiz, swizcount,
                                                     smap, smapcount, m, f, d);

    if (pd->error_count > 0)
    {
        // !!! FIXME: put multiple errors in the buffer? Don't use
        // !!! FIXME:  MOJOSHADER_d3d11GetError() for this?
        set_error(pd->errors[0].error);
        goto compile_shader_fail;
    } // if

    MOJOSHADER_d3d11Shader *retval = (MOJOSHADER_d3d11Shader *) m(sizeof(MOJOSHADER_d3d11Shader), d);
    if (retval == NULL)
        goto compile_shader_fail;

    retval->parseData = pd;
    retval->refcount = 1;
    retval->ubo = NULL;
    retval->constantData = NULL;
    retval->buflen = 0;
    retval->numMaps = 0;

    // Allocate shader maps
    retval->mapCapacity = 4; // arbitrary!
    retval->shaderMaps = (d3d11ShaderMap *) m(retval->mapCapacity * sizeof(d3d11ShaderMap), d);
    if (retval->shaderMaps == NULL)
        goto compile_shader_fail;

    memset(retval->shaderMaps, '\0', retval->mapCapacity * sizeof(d3d11ShaderMap));

    // Create the uniform buffer, if needed
    if (pd->uniform_count > 0)
    {
        // Calculate how big we need to make the buffer
        for (i = 0; i < pd->uniform_count; i++)
        {
            const int arrayCount = pd->uniforms[i].array_count;
            retval->buflen += (arrayCount ? arrayCount : 1) * 16;
        } // for

        D3D11_BUFFER_DESC bdesc;
        bdesc.ByteWidth = next_highest_alignment(retval->buflen);
        bdesc.Usage = D3D11_USAGE_DYNAMIC;
        bdesc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
        bdesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
        bdesc.MiscFlags = 0;
        bdesc.StructureByteStride = 0;
        ID3D11Device_CreateBuffer((ID3D11Device*) ctx->device, &bdesc, NULL,
                                  (ID3D11Buffer**) &retval->ubo);

        // Additionally allocate a CPU-side staging buffer
        retval->constantData = (unsigned char *) m(retval->buflen, d);
        memset(retval->constantData, '\0', retval->buflen);
    } // if

    return retval;

compile_shader_fail:
    MOJOSHADER_freeParseData(pd);
    return NULL;
} // MOJOSHADER_d3d11CompileShader

void MOJOSHADER_d3d11ShaderAddRef(MOJOSHADER_d3d11Shader *shader)
{
    if (shader != NULL)
        shader->refcount++;
} // MOJOSHADER_d3d11ShaderAddRef

void MOJOSHADER_d3d11DeleteShader(
    MOJOSHADER_d3d11Context *ctx,
    MOJOSHADER_d3d11Shader *shader
) {
    if (shader != NULL)
    {
        if (shader->refcount > 1)
            shader->refcount--;
        else
        {
            if (shader->ubo != NULL)
            {
                ID3D11Buffer_Release((ID3D11Buffer*) shader->ubo);
                ctx->free_fn(shader->constantData, ctx->malloc_data);
            } // if

            if (shader->parseData->shader_type == MOJOSHADER_TYPE_VERTEX)
            {
                for (int i = 0; i < shader->numMaps; i++)
                {
                    ID3D11VertexShader_Release((ID3D11VertexShader *) shader->shaderMaps[i].val);
                    ID3D10Blob_Release(shader->shaderMaps[i].vertex.blob);
                } // for
            } // if
            else if (shader->parseData->shader_type == MOJOSHADER_TYPE_PIXEL)
            {
                for (int i = 0; i < shader->numMaps; i++)
                    ID3D11PixelShader_Release((ID3D11PixelShader *) shader->shaderMaps[i].val);
            } // else if

            ctx->free_fn(shader->shaderMaps, ctx->malloc_data);
            shader->shaderMaps = NULL;
            MOJOSHADER_freeParseData(shader->parseData);
            ctx->free_fn(shader, ctx->malloc_data);
        } // else
    } // if
} // MOJOSHADER_d3d11DeleteShader

const MOJOSHADER_parseData *MOJOSHADER_d3d11GetShaderParseData(
                                                MOJOSHADER_d3d11Shader *shader)
{
    return (shader != NULL) ? shader->parseData : NULL;
} // MOJOSHADER_d3d11GetParseData

void MOJOSHADER_d3d11BindShaders(
    MOJOSHADER_d3d11Context *ctx,
    MOJOSHADER_d3d11Shader *vshader,
    MOJOSHADER_d3d11Shader *pshader
) {
    // Use the last bound shaders in case of NULL
    if (vshader != NULL)
    {
        ctx->vertexShader = vshader;
        ctx->vertexNeedsBound = 1;
    } // if

    if (pshader != NULL)
    {
        ctx->pixelShader = pshader;
        ctx->pixelNeedsBound = 1;
    } // if
} // MOJOSHADER_d3d11BindShaders

void MOJOSHADER_d3d11GetBoundShaders(
    MOJOSHADER_d3d11Context *ctx,
    MOJOSHADER_d3d11Shader **vshader,
    MOJOSHADER_d3d11Shader **pshader
) {
    *vshader = ctx->vertexShader;
    *pshader = ctx->pixelShader;
} // MOJOSHADER_d3d11GetBoundShaders

void MOJOSHADER_d3d11MapUniformBufferMemory(
    MOJOSHADER_d3d11Context *ctx,
    float **vsf, int **vsi, unsigned char **vsb,
    float **psf, int **psi, unsigned char **psb
) {
    *vsf = ctx->vs_reg_file_f;
    *vsi = ctx->vs_reg_file_i;
    *vsb = ctx->vs_reg_file_b;
    *psf = ctx->ps_reg_file_f;
    *psi = ctx->ps_reg_file_i;
    *psb = ctx->ps_reg_file_b;
} // MOJOSHADER_d3d11MapUniformBufferMemory

void MOJOSHADER_d3d11UnmapUniformBufferMemory(MOJOSHADER_d3d11Context *ctx)
{
    /* This has nothing to do with unmapping memory
     * and everything to do with updating uniform
     * buffers with the latest parameter contents.
     */
    MOJOSHADER_d3d11Shader *vs, *ps;
    MOJOSHADER_d3d11GetBoundShaders(ctx, &vs, &ps);
    update_uniform_buffer(ctx, vs);
    update_uniform_buffer(ctx, ps);
} // MOJOSHADER_d3d11UnmapUniformBufferMemory

int MOJOSHADER_d3d11GetVertexAttribLocation(MOJOSHADER_d3d11Shader *vert,
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
} // MOJOSHADER_d3d11GetVertexAttribLocation

int MOJOSHADER_d3d11CompileVertexShader(
    MOJOSHADER_d3d11Context *ctx,
    unsigned long long inputLayoutHash,
    void* elements,
    int elementCount,
    void **bytecode,
    int *bytecodeLength
) {
    MOJOSHADER_d3d11Shader *vshader = ctx->vertexShader;
    ID3D11VertexShader *vs;
    ID3D10Blob *blob;

    // Don't compile if there's already a mapping for this layout.
    for (int i = 0; i < vshader->numMaps; i++)
    {
        if (inputLayoutHash == vshader->shaderMaps[i].vertex.layoutHash)
        {
            blob = vshader->shaderMaps[i].vertex.blob;
            *bytecode = ID3D10Blob_GetBufferPointer(blob);
            *bytecodeLength = ID3D10Blob_GetBufferSize(blob);
            return 0;
        } // if
    } // for

    // Check for and replace non-float types
    D3D11_INPUT_ELEMENT_DESC *d3dElements = (D3D11_INPUT_ELEMENT_DESC*) elements;
    const char *origSource = vshader->parseData->output;
    int srcLength = vshader->parseData->output_len;
    char *newSource = (char*) origSource;
    for (int i = 0; i < elementCount; i += 1)
    {
        D3D11_INPUT_ELEMENT_DESC e = d3dElements[i];

        const char *replace;
        if (element_is_uint(e.Format))
            replace = " uint4";
        else if (element_is_int(e.Format))
            replace = "  int4";
        else
            replace = NULL;

        if (replace != NULL)
        {
            char sem[16];
            memset(sem, '\0', sizeof(sem));
            snprintf(sem, sizeof(sem), "%s%d", e.SemanticName, e.SemanticIndex);
            // !!! FIXME: POSITIONT has no index. What to do? -caleb

            if (newSource == origSource)
            {
                newSource = (char *) ctx->malloc_fn(srcLength + 1,
                                                    ctx->malloc_data);
                strcpy(newSource, origSource);
            } // if

            char *ptr = strstr(newSource, sem);
            assert(ptr != NULL && "Could not find semantic in shader source!");

            int spaces = 0;
            while (spaces < 3)
                if (*(--ptr) == ' ') spaces++;
            memcpy(ptr - strlen("float4"), replace, strlen(replace));
        } // if
    } // for

    vs = compileVertexShader(ctx, vshader, newSource, srcLength, &blob);
    if (newSource != origSource)
    {
        ctx->free_fn((void *) newSource, ctx->malloc_data);
    } // if
    if (vs == NULL)
    {
        // Error was already set, just return
        return -1;
    } // if

    // Expand the map array, if needed
    expand_map(ctx, vshader);

    // Add the new mapping
    vshader->shaderMaps[vshader->numMaps].vertex.layoutHash = inputLayoutHash;
    vshader->shaderMaps[ctx->vertexShader->numMaps].val = vs;
    vshader->shaderMaps[ctx->vertexShader->numMaps].vertex.blob = blob;
    ctx->vertexShader->numMaps++;

    // Return the bytecode info
    *bytecode = ID3D10Blob_GetBufferPointer(blob);
    *bytecodeLength = ID3D10Blob_GetBufferSize(blob);
    return 0;
} // MOJOSHADER_d3d11CompileVertexShader

int MOJOSHADER_d3d11ProgramReady(
    MOJOSHADER_d3d11Context *ctx,
    unsigned long long inputLayoutHash
) {
    MOJOSHADER_d3d11Shader *vshader = ctx->vertexShader;
    MOJOSHADER_d3d11Shader *pshader = ctx->pixelShader;
    ID3D11VertexShader *realVS = NULL;
    ID3D11PixelShader *realPS = NULL;

    // Vertex shader...
    if (ctx->vertexNeedsBound)
    {
        for (int i = 0; i < vshader->numMaps; i++)
        {
            if (inputLayoutHash == vshader->shaderMaps[i].vertex.layoutHash)
            {
                realVS = (ID3D11VertexShader *) vshader->shaderMaps[i].val;
                break;
            } // if
        } // for

        if (realVS == NULL)
        {
            set_error("Vertex shader was not found, did you call d3d11CompileVertexShader?");
            return -1;
        } // if

        ctx->vertexNeedsBound = 0;
    } // if

    // Pixel shader...
    if (ctx->pixelNeedsBound)
    {
        // Is there already a mapping for the current vertex shader?
        for (int i = 0; i < pshader->numMaps; i++)
        {
            if (pshader->shaderMaps[i].pixel.vshader == vshader)
            {
                realPS = (ID3D11PixelShader *) pshader->shaderMaps[i].val;
                break;
            } // if
        } // for

        // We have to create a new vertex/pixel shader mapping...
        if (realPS == NULL)
        {
            realPS = compilePixelShader(ctx, vshader, pshader);

            if (realPS == NULL)
            {
                // Error already set by compilePixelShader, just return
                return -1;
            } // if

            // Expand the map array, if needed
            expand_map(ctx, pshader);

            // Add the new mapping
            pshader->shaderMaps[pshader->numMaps].pixel.vshader = vshader;
            pshader->shaderMaps[pshader->numMaps].val = realPS;
            pshader->numMaps++;
        } // if
    } // if

    // Set shader state at the end, in case of errors above
    if (realVS != NULL)
    {
        ID3D11DeviceContext_VSSetShader(ctx->deviceContext, realVS, NULL, 0);
        ID3D11DeviceContext_VSSetConstantBuffers(ctx->deviceContext, 0, 1,
                                                 &vshader->ubo);
    } // if
    if (realPS != NULL)
    {
        ID3D11DeviceContext_PSSetShader(ctx->deviceContext, realPS, NULL, 0);
        ID3D11DeviceContext_PSSetConstantBuffers(ctx->deviceContext, 0, 1,
                                                 &pshader->ubo);
    } // if
    return 0;
} // MOJOSHADER_d3d11ProgramReady

const char *MOJOSHADER_d3d11GetError(MOJOSHADER_d3d11Context *ctx)
{
    return error_buffer;
} // MOJOSHADER_d3d11GetError

#endif /* SUPPORT_PROFILE_HLSL */

// end of mojoshader_d3d11.c ...
