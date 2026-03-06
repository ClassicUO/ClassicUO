#ifndef _INCLUDE_MOJOSHADER_INTERNAL_H_
#define _INCLUDE_MOJOSHADER_INTERNAL_H_

#ifndef __MOJOSHADER_INTERNAL__
#error Do not include this header from your applications.
#endif

// Shader bytecode format is described at MSDN:
//  http://msdn.microsoft.com/en-us/library/ff569705.aspx

#ifdef MOJOSHADER_USE_SDL_STDLIB
#ifdef USE_SDL3 /* Private define, for now */
#include <SDL3/SDL_assert.h>
#include <SDL3/SDL_endian.h>
#include <SDL3/SDL_stdinc.h>
#include <SDL3/SDL_loadso.h>
#else
#include <SDL_assert.h>
#include <SDL_endian.h>
#include <SDL_stdinc.h>
#include <SDL_loadso.h>
#endif
#include <math.h> /* Needed for isinf/isnan :( */

/* FIXME: These includes are needed for alloca :( */
#include <stdlib.h>
#if defined(__linux__) || defined(__sun)
#include <alloca.h>
#endif
#ifdef _MSC_VER
#include <malloc.h>
#endif

/* stdint.h */
typedef Uint8 uint8;
typedef Uint16 uint16;
typedef Uint32 uint32;
typedef Sint32 int32;
typedef Sint64 int64;
typedef Uint64 uint64;

/* assert.h */
#define assert SDL_assert

/* stdlib.h */
#define malloc SDL_malloc
#define free SDL_free

/* stdio.h */
#define sscanf SDL_sscanf
#ifdef snprintf
#undef snprintf
#endif
#define snprintf SDL_snprintf
#ifdef vsnprintf
#undef vsnprintf
#endif
#define vsnprintf SDL_vsnprintf

/* math.h */
#define acos SDL_acos
#define asin SDL_asin
#define atan SDL_atan
#define atan2 SDL_atan2
#define cos SDL_cos
#define exp SDL_exp
#define floor SDL_floor
#define log SDL_log
#define sin SDL_sin
#define sqrt SDL_sqrt

/* string.h */
#ifdef memcmp
#undef memcmp
#endif
#define memcmp SDL_memcmp
#ifdef memcpy
#undef memcpy
#endif
#define memcpy SDL_memcpy
#ifdef memset
#undef memset
#endif
#define memset SDL_memset
#ifdef strchr
#undef strchr
#endif
#define strchr SDL_strchr
#ifdef strcmp
#undef strcmp
#endif
#define strcmp SDL_strcmp
#ifdef strlen
#undef strlen
#endif
#define strlen SDL_strlen
#ifdef strncmp
#undef strncmp
#endif
#define strncmp SDL_strncmp
#ifdef strstr
#undef strstr
#endif
#define strstr SDL_strstr
#ifdef strcat
#undef strcat
#endif
/* TODO: Move MojoShader away from strcat! This len is awful! */
#define strcat(dst, src) SDL_strlcat(dst, src, SDL_strlen(src) + 1)
#ifdef strcpy
#undef strcpy
#endif
/* TODO: Move MojoShader away from strcpy! This len is awful! */
#define strcpy(dst, src) SDL_strlcpy(dst, src, SDL_strlen(src) + 1)

/* ctype.h */
#ifdef isalnum
#undef isalnum
#endif
#define isalnum SDL_isalnum

/* endian.h */
#define MOJOSHADER_BIG_ENDIAN (SDL_BYTEORDER == SDL_BIG_ENDIAN)

/* dlfcn.h */
#define dlopen(a, b) SDL_LoadObject(a)
#define dlclose SDL_UnloadObject
#define dlsym SDL_LoadFunction
#else /* MOJOSHADER_USE_SDL_STDLIB */
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <stdarg.h>
#include <assert.h>

#ifdef __BYTE_ORDER__
#define MOJOSHADER_BIG_ENDIAN (__BYTE_ORDER__ == __ORDER_BIG_ENDIAN__)
#elif defined(__linux__)
#include <endian.h>
#define MOJOSHADER_BIG_ENDIAN (__BYTE_ORDER == __BIG_ENDIAN)
#else
/* TODO: Detect little endian PowerPC? */
#if defined(__POWERPC__) || defined(__powerpc__)
#define MOJOSHADER_BIG_ENDIAN 1
#else
#define MOJOSHADER_BIG_ENDIAN 0
#endif
#endif
#endif /* MOJOSHADER_USE_SDL_STDLIB */

#include "mojoshader.h"

// This is the highest shader version we currently support.

#define MAX_SHADER_MAJOR 3
#define MAX_SHADER_MINOR 255  // vs_3_sw


// If SUPPORT_PROFILE_* isn't defined, we assume an implicit desire to support.
//  You get all the profiles unless you go out of your way to disable them.

#ifndef SUPPORT_PROFILE_D3D
#define SUPPORT_PROFILE_D3D 1
#endif

#ifndef SUPPORT_PROFILE_BYTECODE
#define SUPPORT_PROFILE_BYTECODE 1
#endif

#ifndef SUPPORT_PROFILE_HLSL
#define SUPPORT_PROFILE_HLSL 1
#endif

#ifndef SUPPORT_PROFILE_GLSL
#define SUPPORT_PROFILE_GLSL 1
#endif

#ifndef SUPPORT_PROFILE_GLSL120
#define SUPPORT_PROFILE_GLSL120 1
#endif

#ifndef SUPPORT_PROFILE_GLSLES
#define SUPPORT_PROFILE_GLSLES 1
#endif

#ifndef SUPPORT_PROFILE_GLSLES3
#define SUPPORT_PROFILE_GLSLES3 1
#endif

#ifndef SUPPORT_PROFILE_ARB1
#define SUPPORT_PROFILE_ARB1 1
#endif

#ifndef SUPPORT_PROFILE_ARB1_NV
#define SUPPORT_PROFILE_ARB1_NV 1
#endif

#ifndef SUPPORT_PROFILE_METAL
#define SUPPORT_PROFILE_METAL 1
#endif

#ifndef SUPPORT_PROFILE_SPIRV
#define SUPPORT_PROFILE_SPIRV 1
#endif

#ifndef SUPPORT_PROFILE_GLSPIRV
#define SUPPORT_PROFILE_GLSPIRV 1
#endif

#if SUPPORT_PROFILE_ARB1_NV && !SUPPORT_PROFILE_ARB1
#error nv profiles require arb1 profile. Fix your build.
#endif

#if SUPPORT_PROFILE_GLSL120 && !SUPPORT_PROFILE_GLSL
#error glsl120 profile requires glsl profile. Fix your build.
#endif

#if SUPPORT_PROFILE_GLSLES && !SUPPORT_PROFILE_GLSL
#error glsles profile requires glsl profile. Fix your build.
#endif

#if SUPPORT_PROFILE_GLSLES3 && !SUPPORT_PROFILE_GLSLES
#error glsles3 profile requires glsles profile. Fix your build.
#endif


#if SUPPORT_PROFILE_GLSPIRV && !SUPPORT_PROFILE_SPIRV
#error glspirv profile requires spirv profile. Fix your build.
#endif

// Microsoft's preprocessor has some quirks. In some ways, it doesn't work
//  like you'd expect a C preprocessor to function.
#ifndef MATCH_MICROSOFT_PREPROCESSOR
#define MATCH_MICROSOFT_PREPROCESSOR 1
#endif


// Get basic wankery out of the way here...

#ifdef _WINDOWS
#define ENDLINE_STR "\r\n"
#else
#define ENDLINE_STR "\n"
#endif

typedef unsigned int uint;  // this is a printf() helper. don't use for code.

// Locale-independent float printing replacement for snprintf
size_t MOJOSHADER_printFloat(char *text, size_t maxlen, float arg);

#ifdef _MSC_VER
#include <float.h>
#define isnan _isnan // !!! FIXME: not a safe replacement!
#if _MSC_VER < 1900 // pre MSVC 2015
#define isinf(x) (!_finite(x)) // FIXME: not a safe replacement!
#endif
#define va_copy(a, b) a = b
#endif

#ifndef MOJOSHADER_USE_SDL_STDLIB
#ifdef _MSC_VER
#include <malloc.h>
#define snprintf _snprintf  // !!! FIXME: not a safe replacement!
#define vsnprintf _vsnprintf  // !!! FIXME: not a safe replacement!
#define strcasecmp stricmp
#define strncasecmp strnicmp
typedef unsigned __int8 uint8;
typedef unsigned __int16 uint16;
typedef unsigned __int32 uint32;
typedef unsigned __int64 uint64;
typedef __int32 int32;
typedef __int64 int64;
#ifdef _WIN64
typedef __int64 ssize_t;
#elif defined _WIN32
typedef __int32 ssize_t;
#else
#error Please define your platform.
#endif
// Warning Level 4 considered harmful.  :)
#pragma warning(disable: 4100)  // "unreferenced formal parameter"
#pragma warning(disable: 4389)  // "signed/unsigned mismatch"
#else
#include <stdint.h>
typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef int32_t int32;
typedef int64_t int64;
typedef uint64_t uint64;
#endif
#endif /* MOJOSHADER_USE_SDL_STDLIB */

#ifdef __GNUC__
#define ISPRINTF(x,y) __attribute__((format (printf, x, y)))
#else
#define ISPRINTF(x,y)
#endif

#define STATICARRAYLEN(x) ( (sizeof ((x))) / (sizeof ((x)[0])) )


// Byteswap magic...

#if (defined(__GNUC__) && MOJOSHADER_BIG_ENDIAN && (defined(__POWERPC__) || defined(__powerpc__)))
    static inline uint32 SWAP32(uint32 x)
    {
        __asm__ __volatile__("lwbrx %0,0,%1" : "=r" (x) : "r" (&x));
        return x;
    } // SWAP32
    static inline uint16 SWAP16(uint16 x)
    {
        __asm__ __volatile__("lhbrx %0,0,%1" : "=r" (x) : "r" (&x));
        return x;
    } // SWAP16
#elif MOJOSHADER_BIG_ENDIAN
    static inline uint32 SWAP32(uint32 x)
    {
        return ( (((x) >> 24) & 0x000000FF) | (((x) >>  8) & 0x0000FF00) |
                 (((x) <<  8) & 0x00FF0000) | (((x) << 24) & 0xFF000000) );
    } // SWAP32
    static inline uint16 SWAP16(uint16 x)
    {
        return ( (((x) >> 8) & 0x00FF) | (((x) << 8) & 0xFF00) );
    } // SWAP16
#else
#   define SWAP16(x) (x)
#   define SWAP32(x) (x)
#endif

#define SWAPDBL(x) (x)  // !!! FIXME

static inline int Min(const int a, const int b)
{
    return ((a < b) ? a : b);
} // Min


// Hashtables...

typedef struct HashTable HashTable;
typedef uint32 (*HashTable_HashFn)(const void *key, void *data);
typedef int (*HashTable_KeyMatchFn)(const void *a, const void *b, void *data);
typedef void (*HashTable_NukeFn)(const void *ctx, const void *key, const void *value, void *data);

HashTable *hash_create(void *data, const HashTable_HashFn hashfn,
                       const HashTable_KeyMatchFn keymatchfn,
                       const HashTable_NukeFn nukefn,
                       const int stackable,
                       MOJOSHADER_malloc m, MOJOSHADER_free f, void *d);
void hash_destroy(HashTable *table, const void *ctx);
int hash_insert(HashTable *table, const void *key, const void *value);
int hash_remove(HashTable *table, const void *key, const void *ctx);
int hash_find(const HashTable *table, const void *key, const void **_value);
int hash_iter(const HashTable *table, const void *key, const void **_value, void **iter);
int hash_iter_keys(const HashTable *table, const void **_key, void **iter);

uint32 hash_hash_string(const void *sym, void *unused);
int hash_keymatch_string(const void *a, const void *b, void *unused);


// String -> String map ...
typedef HashTable StringMap;
StringMap *stringmap_create(const int copy, MOJOSHADER_malloc m,
                            MOJOSHADER_free f, void *d);
void stringmap_destroy(StringMap *smap);
int stringmap_insert(StringMap *smap, const char *key, const char *value);
int stringmap_remove(StringMap *smap, const char *key);
int stringmap_find(const StringMap *smap, const char *key, const char **_val);


// String caching...

typedef struct StringCache StringCache;
StringCache *stringcache_create(MOJOSHADER_malloc m,MOJOSHADER_free f,void *d);
const char *stringcache(StringCache *cache, const char *str);
const char *stringcache_len(StringCache *cache, const char *str,
                            const unsigned int len);
const char *stringcache_fmt(StringCache *cache, const char *fmt, ...);
int stringcache_iscached(StringCache *cache, const char *str);
void stringcache_destroy(StringCache *cache);


// Error lists...

typedef struct ErrorList ErrorList;
ErrorList *errorlist_create(MOJOSHADER_malloc m, MOJOSHADER_free f, void *d);
int errorlist_add(ErrorList *list, const char *fname,
                      const int errpos, const char *str);
int errorlist_add_fmt(ErrorList *list, const char *fname,
                      const int errpos, const char *fmt, ...) ISPRINTF(4,5);
int errorlist_add_va(ErrorList *list, const char *_fname,
                     const int errpos, const char *fmt, va_list va);
int errorlist_count(ErrorList *list);
MOJOSHADER_error *errorlist_flatten(ErrorList *list); // resets the list!
void errorlist_destroy(ErrorList *list);



// Dynamic buffers...

typedef struct BufferBlock
{
    uint8 *data;
    size_t bytes;
    struct BufferBlock *next;
} BufferBlock;
typedef struct Buffer
{
    size_t total_bytes;
    BufferBlock *head;
    BufferBlock *tail;
    size_t block_size;
    MOJOSHADER_malloc m;
    MOJOSHADER_free f;
    void *d;
} Buffer;
Buffer *buffer_create(size_t blksz,MOJOSHADER_malloc m,MOJOSHADER_free f,void *d);
char *buffer_reserve(Buffer *buffer, const size_t len);
int buffer_append(Buffer *buffer, const void *_data, size_t len);
int buffer_append_fmt(Buffer *buffer, const char *fmt, ...) ISPRINTF(2,3);
int buffer_append_va(Buffer *buffer, const char *fmt, va_list va);
size_t buffer_size(Buffer *buffer);
void buffer_empty(Buffer *buffer);
char *buffer_flatten(Buffer *buffer);
char *buffer_merge(Buffer **buffers, const size_t n, size_t *_len);
void buffer_destroy(Buffer *buffer);
void buffer_patch(Buffer *buffer, const size_t start,
                  const void *data, const size_t len);



// This is the ID for a D3DXSHADER_CONSTANTTABLE in the bytecode comments.
#define CTAB_ID 0x42415443  // 0x42415443 == 'CTAB'
#define CTAB_SIZE 28  // sizeof (D3DXSHADER_CONSTANTTABLE).
#define CINFO_SIZE 20  // sizeof (D3DXSHADER_CONSTANTINFO).
#define CTYPEINFO_SIZE 16  // sizeof (D3DXSHADER_TYPEINFO).
#define CMEMBERINFO_SIZE 8  // sizeof (D3DXSHADER_STRUCTMEMBERINFO)

// Preshader magic values...
#define PRES_ID 0x53455250  // 0x53455250 == 'PRES'
#define PRSI_ID 0x49535250  // 0x49535250 == 'PRSI'
#define CLIT_ID 0x54494C43  // 0x54494C43 == 'CLIT'
#define FXLC_ID 0x434C5846  // 0x434C5846 == 'FXLC'

// we need to reference these by explicit value occasionally...
#define OPCODE_RCP 6
#define OPCODE_RSQ 7
#define OPCODE_RET 28
#define OPCODE_IF 40
#define OPCODE_IFC 41
#define OPCODE_BREAK 44
#define OPCODE_BREAKC 45
#define OPCODE_TEXLD 66
#define OPCODE_SETP 94

// TEXLD becomes a different instruction with these instruction controls.
#define CONTROL_TEXLD  0
#define CONTROL_TEXLDP 1
#define CONTROL_TEXLDB 2

// #define this to force app to supply an allocator, so there's no reference
//  to the C runtime's malloc() and free()...
#if MOJOSHADER_FORCE_ALLOCATOR
#define MOJOSHADER_internal_malloc NULL
#define MOJOSHADER_internal_free NULL
#else
void * MOJOSHADERCALL MOJOSHADER_internal_malloc(int bytes, void *d);
void MOJOSHADERCALL MOJOSHADER_internal_free(void *ptr, void *d);
#endif


// result modifiers.
// !!! FIXME: why isn't this an enum?
#define MOD_SATURATE 0x01
#define MOD_PP 0x02
#define MOD_CENTROID 0x04

typedef enum
{
    REG_TYPE_TEMP = 0,
    REG_TYPE_INPUT = 1,
    REG_TYPE_CONST = 2,
    REG_TYPE_ADDRESS = 3,
    REG_TYPE_TEXTURE = 3,  // ALSO 3!
    REG_TYPE_RASTOUT = 4,
    REG_TYPE_ATTROUT = 5,
    REG_TYPE_TEXCRDOUT = 6,
    REG_TYPE_OUTPUT = 6,  // ALSO 6!
    REG_TYPE_CONSTINT = 7,
    REG_TYPE_COLOROUT = 8,
    REG_TYPE_DEPTHOUT = 9,
    REG_TYPE_SAMPLER = 10,
    REG_TYPE_CONST2 = 11,
    REG_TYPE_CONST3 = 12,
    REG_TYPE_CONST4 = 13,
    REG_TYPE_CONSTBOOL = 14,
    REG_TYPE_LOOP = 15,
    REG_TYPE_TEMPFLOAT16 = 16,
    REG_TYPE_MISCTYPE = 17,
    REG_TYPE_LABEL = 18,
    REG_TYPE_PREDICATE = 19,
    REG_TYPE_MAX = 19
} RegisterType;

typedef enum
{
    TEXTURE_TYPE_2D = 2,
    TEXTURE_TYPE_CUBE = 3,
    TEXTURE_TYPE_VOLUME = 4,
} TextureType;

typedef enum
{
    RASTOUT_TYPE_POSITION = 0,
    RASTOUT_TYPE_FOG = 1,
    RASTOUT_TYPE_POINT_SIZE = 2,
    RASTOUT_TYPE_MAX = 2
} RastOutType;

typedef enum
{
    MISCTYPE_TYPE_POSITION = 0,
    MISCTYPE_TYPE_FACE = 1,
    MISCTYPE_TYPE_MAX = 1
} MiscTypeType;

// source modifiers.
typedef enum
{
    SRCMOD_NONE,
    SRCMOD_NEGATE,
    SRCMOD_BIAS,
    SRCMOD_BIASNEGATE,
    SRCMOD_SIGN,
    SRCMOD_SIGNNEGATE,
    SRCMOD_COMPLEMENT,
    SRCMOD_X2,
    SRCMOD_X2NEGATE,
    SRCMOD_DZ,
    SRCMOD_DW,
    SRCMOD_ABS,
    SRCMOD_ABSNEGATE,
    SRCMOD_NOT,
    SRCMOD_TOTAL
} SourceMod;


typedef struct
{
    const uint32 *token;   // this is the unmolested token in the stream.
    int regnum;
    int relative;
    int writemask;   // xyzw or rgba (all four, not split out).
    int writemask0;  // x or red
    int writemask1;  // y or green
    int writemask2;  // z or blue
    int writemask3;  // w or alpha
    int orig_writemask;   // writemask before mojoshader tweaks it.
    int result_mod;
    int result_shift;
    RegisterType regtype;
} DestArgInfo;

// NOTE: This will NOT know a dcl_psize or dcl_fog output register should be
//        scalar! This function doesn't have access to that information.
static inline int scalar_register(const MOJOSHADER_shaderType shader_type,
                                  const RegisterType regtype, const int regnum)
{
    switch (regtype)
    {
        case REG_TYPE_RASTOUT:
            if (((const RastOutType) regnum) == RASTOUT_TYPE_FOG)
                return 1;
            else if (((const RastOutType) regnum) == RASTOUT_TYPE_POINT_SIZE)
                return 1;
            return 0;

        case REG_TYPE_DEPTHOUT:
        case REG_TYPE_CONSTBOOL:
        case REG_TYPE_LOOP:
            return 1;

        case REG_TYPE_MISCTYPE:
            if ( ((const MiscTypeType) regnum) == MISCTYPE_TYPE_FACE )
                return 1;
            return 0;

        case REG_TYPE_PREDICATE:
            return (shader_type == MOJOSHADER_TYPE_PIXEL) ? 1 : 0;

        default: break;
    } // switch

    return 0;
} // scalar_register


extern MOJOSHADER_error MOJOSHADER_out_of_mem_error;
extern MOJOSHADER_parseData MOJOSHADER_out_of_mem_data;


#if SUPPORT_PROFILE_SPIRV
// Patching SPIR-V binaries before linking is needed to ensure locations do not
// overlap between shader stages. Unfortunately, OpDecorate takes Literal, so we
// can't use Result <id> from OpSpecConstant and leave this up to specialization
// mechanism.
// Patch table must be propagated from parsing to program linking, but since
// MOJOSHADER_parseData is public and I'd like to avoid changing ABI and exposing
// this, it is appended to MOJOSHADER_parseData::output using postflight buffer.
typedef struct SpirvPatchEntry
{
    uint32 offset;
    int32 location;
} SpirvPatchEntry;

typedef struct SpirvPatchTable
{
    // Patches for uniforms
    SpirvPatchEntry vpflip;
    SpirvPatchEntry array_vec4;
    SpirvPatchEntry array_ivec4;
    SpirvPatchEntry array_bool;
    SpirvPatchEntry samplers[16];
    int32 location_count;

    // TEXCOORD0 is patched to PointCoord if VS outputs PointSize.
    // In `helpers`: [OpDecorate|id|Location|0xDEADBEEF] -> [OpDecorate|id|BuiltIn|PointCoord]
    // Offset derived from attrib_offsets[TEXCOORD][0].
    uint32 pointcoord_var_offset; // in `mainline_intro`, [OpVariable|tid|id|StorageClass], patch tid to pvec2i
    uint32 pointcoord_load_offset; // in `mainline_top`, [OpLoad|tid|id|src_id], patch tid to vec2
    uint32 tid_pvec2i;
    uint32 tid_vec2;
    uint32 tid_pvec4i;

    /// Patches for TEXCOORD0 and vertex attribute types
    uint32 tid_vec4;
    uint32 tid_ivec4;
    uint32 tid_uvec4;

    // Patches for vertex attribute types
    uint32 tid_vec4_p;
    uint32 tid_ivec4_p;
    uint32 tid_uvec4_p;
    uint32 attrib_type_offsets[MOJOSHADER_USAGE_TOTAL][16];
    struct
    {
        uint32 num_loads;
        uint32 *load_types;
        uint32 *load_opcodes;
    } attrib_type_load_offsets[MOJOSHADER_USAGE_TOTAL][16];

    // Patches for linking vertex output/pixel input
    uint32 attrib_offsets[MOJOSHADER_USAGE_TOTAL][16];
    uint32 output_offsets[16];
} SpirvPatchTable;

void MOJOSHADER_spirv_link_attributes(const MOJOSHADER_parseData *vertex,
                                      const MOJOSHADER_parseData *pixel,
                                      int is_glspirv);
#endif

#endif  // _INCLUDE_MOJOSHADER_INTERNAL_H_


#if MOJOSHADER_DO_INSTRUCTION_TABLE
// These have to be in the right order! Arrays are indexed by the value
//  of the instruction token.

// INSTRUCTION_STATE means this opcode has to update the state machine
//  (we're entering an ELSE block, etc). INSTRUCTION means there's no
//  state, just go straight to the emitters.

// !!! FIXME: Some of these MOJOSHADER_TYPE_ANYs need to have their scope
// !!! FIXME:  reduced to just PIXEL or VERTEX.

INSTRUCTION(NOP, "NOP", 1, NULL, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(MOV, "MOV", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(ADD, "ADD", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(SUB, "SUB", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(MAD, "MAD", 1, DSSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(MUL, "MUL", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(RCP, "RCP", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(RSQ, "RSQ", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(DP3, "DP3", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(DP4, "DP4", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(MIN, "MIN", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(MAX, "MAX", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(SLT, "SLT", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(SGE, "SGE", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(EXP, "EXP", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(LOG, "LOG", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(LIT, "LIT", 3, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(DST, "DST", 1, DSS, MOJOSHADER_TYPE_VERTEX, 0xF)
INSTRUCTION(LRP, "LRP", 2, DSSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(FRC, "FRC", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(M4X4, "M4X4", 4, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(M4X3, "M4X3", 3, DSS, MOJOSHADER_TYPE_ANY, 0x7)
INSTRUCTION_STATE(M3X4, "M3X4", 4, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(M3X3, "M3X3", 3, DSS, MOJOSHADER_TYPE_ANY, 0x7)
INSTRUCTION_STATE(M3X2, "M3X2", 2, DSS, MOJOSHADER_TYPE_ANY, 0x3)
INSTRUCTION_STATE(CALL, "CALL", 2, S, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(CALLNZ, "CALLNZ", 3, SS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(LOOP, "LOOP", 3, SS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(RET, "RET", 1, NULL, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(ENDLOOP, "ENDLOOP", 2, NULL, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(LABEL, "LABEL", 0, S, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(DCL, "DCL", 0, DCL, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(POW, "POW", 3, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(CRS, "CRS", 2, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(SGN, "SGN", 3, DSSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(ABS, "ABS", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(NRM, "NRM", 3, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(SINCOS, "SINCOS", 8, SINCOS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(REP, "REP", 3, S, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(ENDREP, "ENDREP", 2, NULL, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(IF, "IF", 3, S, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(IFC, "IF", 3, SS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(ELSE, "ELSE", 1, NULL, MOJOSHADER_TYPE_ANY, 0xF)  // !!! FIXME: state!
INSTRUCTION(ENDIF, "ENDIF", 1, NULL, MOJOSHADER_TYPE_ANY, 0xF) // !!! FIXME: state!
INSTRUCTION_STATE(BREAK, "BREAK", 1, NULL, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(BREAKC, "BREAK", 3, SS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(MOVA, "MOVA", 1, DS, MOJOSHADER_TYPE_VERTEX, 0xF)
INSTRUCTION_STATE(DEFB, "DEFB", 0, DEFB, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(DEFI, "DEFI", 0, DEFI, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION_STATE(TEXCRD, "TEXCRD", 1, TEXCRD, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXKILL, "TEXKILL", 2, D, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXLD, "TEXLD", 1, TEXLD, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXBEM, "TEXBEM", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXBEML, "TEXBEML", 2, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(TEXREG2AR, "TEXREG2AR", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(TEXREG2GB, "TEXREG2GB", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXM3X2PAD, "TEXM3X2PAD", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXM3X2TEX, "TEXM3X2TEX", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXM3X3PAD, "TEXM3X3PAD", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXM3X3TEX, "TEXM3X3TEX", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(RESERVED, 0, 0, NULL, MOJOSHADER_TYPE_UNKNOWN, 0xF)
INSTRUCTION_STATE(TEXM3X3SPEC, "TEXM3X3SPEC", 1, DSS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXM3X3VSPEC, "TEXM3X3VSPEC", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(EXPP, "EXPP", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(LOGP, "LOGP", 1, DS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(CND, "CND", 1, DSSS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(DEF, "DEF", 0, DEF, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION(TEXREG2RGB, "TEXREG2RGB", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(TEXDP3TEX, "TEXDP3TEX", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(TEXM3X2DEPTH, "TEXM3X2DEPTH", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(TEXDP3, "TEXDP3", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(TEXM3X3, "TEXM3X3", 1, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(TEXDEPTH, "TEXDEPTH", 1, D, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(CMP, "CMP", 1, DSSS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(BEM, "BEM", 2, DSS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(DP2ADD, "DP2ADD", 2, DSSS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(DSX, "DSX", 2, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(DSY, "DSY", 2, DS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION(TEXLDD, "TEXLDD", 3, DSSSS, MOJOSHADER_TYPE_PIXEL, 0xF)
INSTRUCTION_STATE(SETP, "SETP", 1, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(TEXLDL, "TEXLDL", 2, DSS, MOJOSHADER_TYPE_ANY, 0xF)
INSTRUCTION_STATE(BREAKP, "BREAKP", 3, S, MOJOSHADER_TYPE_ANY, 0xF)

#undef MOJOSHADER_DO_INSTRUCTION_TABLE
#undef INSTRUCTION
#undef INSTRUCTION_STATE

#endif

// end of mojoshader_internal.h ...

