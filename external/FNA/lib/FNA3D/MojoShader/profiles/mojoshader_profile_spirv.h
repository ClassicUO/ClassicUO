/**
 * MojoShader; generate shader programs from bytecode of compiled
 *  Direct3D shaders.
 *
 * Please see the file LICENSE.txt in the source's root directory.
 *
 *  This file written by Ryan C. Gordon.
 */

#ifndef MOJOSHADER_PROFILE_SPIRV_H
#define MOJOSHADER_PROFILE_SPIRV_H

#if SUPPORT_PROFILE_SPIRV

#define MOJOSHADER_SPIRV_VS_SAMPLER_SET 0
#define MOJOSHADER_SPIRV_VS_UNIFORM_SET 1
#define MOJOSHADER_SPIRV_PS_SAMPLER_SET 2
#define MOJOSHADER_SPIRV_PS_UNIFORM_SET 3

// For baked-in constants in SPIR-V we want to store scalar values that we can
// use in composites, since OpConstantComposite uses result ids constituates
// rather than value literals.
// We'll store these lists grouped by type and have the lists themselves
// ordered by value in the ctx.spirv struct.
typedef struct ComponentList
{
    // result id from OpConstant
    uint32 id;
    union {
        float f;
        int i;
        uint32 u;
    } v;
    struct ComponentList *next;
} ComponentList;

typedef struct SpirvLoopInfo
{
    uint32 tid_counter;
    uint32 id_counter;
    uint32 id_counter_next;
    uint32 id_aL;
    uint32 id_label_header;
    uint32 id_label_continue;
    uint32 id_label_merge;
} SpirvLoopInfo;

typedef enum SpirvType
{
    ST_FLOAT = 0,
    ST_SINT = 1,
    ST_UINT = 2,
    ST_BOOL = 3,
} SpirvType;

typedef enum SpirvStorageClass
{
    SC_INPUT = 0,
    SC_OUTPUT = 1,
    SC_PRIVATE = 2,
    SC_UNIFORM_CONSTANT = 3,
} SpirvStorageClass;

/* Not all type parameter combinations are actually used, but it's all rounded up to 64 so
 * it's easier to work with.
 */
typedef enum SpirvTypeIdx
{
    STI_VOID          = 0,
    STI_FUNC_VOID     = 1,
    STI_FUNC_LIT      = 2,
    STI_IMAGE2D       = 3,
    STI_IMAGE3D       = 4,
    STI_IMAGECUBE     = 5,
    STI_PTR_IMAGE2D   = 6,
    STI_PTR_IMAGE3D   = 7,
    STI_PTR_IMAGECUBE = 8,
    STI_PTR_VEC2_I    = 9, // special case, needed only for point coord input.

    // 6 unused entries

    // 4 base types * 4 vector sizes = 16 entries
    STI_FLOAT = (0 << 5) | (1 << 4) | (ST_FLOAT << 2) | 0,
    STI_VEC2  = (0 << 5) | (1 << 4) | (ST_FLOAT << 2) | 1,
    STI_VEC3  = (0 << 5) | (1 << 4) | (ST_FLOAT << 2) | 2,
    STI_VEC4  = (0 << 5) | (1 << 4) | (ST_FLOAT << 2) | 3,
    STI_INT   = (0 << 5) | (1 << 4) | (ST_SINT  << 2) | 0,
    STI_IVEC2 = (0 << 5) | (1 << 4) | (ST_SINT  << 2) | 1,
    STI_IVEC3 = (0 << 5) | (1 << 4) | (ST_SINT  << 2) | 2,
    STI_IVEC4 = (0 << 5) | (1 << 4) | (ST_SINT  << 2) | 3,
    STI_UINT  = (0 << 5) | (1 << 4) | (ST_UINT  << 2) | 0,
    STI_UVEC2 = (0 << 5) | (1 << 4) | (ST_UINT  << 2) | 1,
    STI_UVEC3 = (0 << 5) | (1 << 4) | (ST_UINT  << 2) | 2,
    STI_UVEC4 = (0 << 5) | (1 << 4) | (ST_UINT  << 2) | 3,
    STI_BOOL  = (0 << 5) | (1 << 4) | (ST_BOOL  << 2) | 0,
    STI_BVEC2 = (0 << 5) | (1 << 4) | (ST_BOOL  << 2) | 1,
    STI_BVEC3 = (0 << 5) | (1 << 4) | (ST_BOOL  << 2) | 2,
    STI_BVEC4 = (0 << 5) | (1 << 4) | (ST_BOOL  << 2) | 3,

    // 2 dims (vec4 + scalar) * 4 base types * 4 storage classes
    STI_PTR_FLOAT_I = (1 << 5) | (0 << 4) | (ST_FLOAT << 2) | SC_INPUT,
    STI_PTR_FLOAT_O = (1 << 5) | (0 << 4) | (ST_FLOAT << 2) | SC_OUTPUT,
    STI_PTR_FLOAT_P = (1 << 5) | (0 << 4) | (ST_FLOAT << 2) | SC_PRIVATE,
    STI_PTR_FLOAT_U = (1 << 5) | (0 << 4) | (ST_FLOAT << 2) | SC_UNIFORM_CONSTANT,
    STI_PTR_INT_I   = (1 << 5) | (0 << 4) | (ST_SINT  << 2) | SC_INPUT,
    STI_PTR_INT_O   = (1 << 5) | (0 << 4) | (ST_SINT  << 2) | SC_OUTPUT,
    STI_PTR_INT_P   = (1 << 5) | (0 << 4) | (ST_SINT  << 2) | SC_PRIVATE,
    STI_PTR_INT_U   = (1 << 5) | (0 << 4) | (ST_SINT  << 2) | SC_UNIFORM_CONSTANT,
    STI_PTR_UINT_I  = (1 << 5) | (0 << 4) | (ST_UINT  << 2) | SC_INPUT,
    STI_PTR_UINT_O  = (1 << 5) | (0 << 4) | (ST_UINT  << 2) | SC_OUTPUT,
    STI_PTR_UINT_P  = (1 << 5) | (0 << 4) | (ST_UINT  << 2) | SC_PRIVATE,
    STI_PTR_UINT_U  = (1 << 5) | (0 << 4) | (ST_UINT  << 2) | SC_UNIFORM_CONSTANT,
    STI_PTR_BOOL_I  = (1 << 5) | (0 << 4) | (ST_BOOL  << 2) | SC_INPUT,
    STI_PTR_BOOL_O  = (1 << 5) | (0 << 4) | (ST_BOOL  << 2) | SC_OUTPUT,
    STI_PTR_BOOL_P  = (1 << 5) | (0 << 4) | (ST_BOOL  << 2) | SC_PRIVATE,
    STI_PTR_BOOL_U  = (1 << 5) | (0 << 4) | (ST_BOOL  << 2) | SC_UNIFORM_CONSTANT,
    STI_PTR_VEC4_I  = (1 << 5) | (1 << 4) | (ST_FLOAT << 2) | SC_INPUT,
    STI_PTR_VEC4_O  = (1 << 5) | (1 << 4) | (ST_FLOAT << 2) | SC_OUTPUT,
    STI_PTR_VEC4_P  = (1 << 5) | (1 << 4) | (ST_FLOAT << 2) | SC_PRIVATE,
    STI_PTR_VEC4_U  = (1 << 5) | (1 << 4) | (ST_FLOAT << 2) | SC_UNIFORM_CONSTANT,
    STI_PTR_IVEC4_I = (1 << 5) | (1 << 4) | (ST_SINT  << 2) | SC_INPUT,
    STI_PTR_IVEC4_O = (1 << 5) | (1 << 4) | (ST_SINT  << 2) | SC_OUTPUT,
    STI_PTR_IVEC4_P = (1 << 5) | (1 << 4) | (ST_SINT  << 2) | SC_PRIVATE,
    STI_PTR_IVEC4_U = (1 << 5) | (1 << 4) | (ST_SINT  << 2) | SC_UNIFORM_CONSTANT,
    STI_PTR_UVEC4_I = (1 << 5) | (1 << 4) | (ST_UINT  << 2) | SC_INPUT,
    STI_PTR_UVEC4_O = (1 << 5) | (1 << 4) | (ST_UINT  << 2) | SC_OUTPUT,
    STI_PTR_UVEC4_P = (1 << 5) | (1 << 4) | (ST_UINT  << 2) | SC_PRIVATE,
    STI_PTR_UVEC4_U = (1 << 5) | (1 << 4) | (ST_UINT  << 2) | SC_UNIFORM_CONSTANT,
    STI_PTR_BVEC4_I = (1 << 5) | (1 << 4) | (ST_BOOL  << 2) | SC_INPUT,
    STI_PTR_BVEC4_O = (1 << 5) | (1 << 4) | (ST_BOOL  << 2) | SC_OUTPUT,
    STI_PTR_BVEC4_P = (1 << 5) | (1 << 4) | (ST_BOOL  << 2) | SC_PRIVATE,
    STI_PTR_BVEC4_U = (1 << 5) | (1 << 4) | (ST_BOOL  << 2) | SC_UNIFORM_CONSTANT,

    // 2 + 6 + 16 + 32 = 56 entries (+ 8 unused)

    // Helpers
    STI_LENGTH_,

    STI_MISC_START_ = 0,
    STI_MISC_END_   = 8,
    STI_CORE_START_ = (0 << 5) | (1 << 4),
    STI_PTR_START_  = (1 << 5) | (0 << 4),
    STI_CORE_END_   = STI_PTR_START_,
    STI_PTR_END_    = STI_LENGTH_,
} SpirvTypeIdx;

// In addition to result ID we also need type ID (can't assume everything is vec4).
typedef struct SpirvResult
{
    uint32 tid;
    uint32 id;
} SpirvResult;

// This doesn't 100% correspond to glslangValidator semantics. It just says which mode to use at
// runtime (different from compile-time support being enabled). Technically, we could generate the
// same for both, but that would require GL code to use UBOs.
typedef enum SpirvMode
{
    SPIRV_MODE_GL,
    SPIRV_MODE_VK,
} SpirvMode;

typedef struct SpirvContext
{
#if SUPPORT_PROFILE_GLSPIRV
    uint32 id_vs_main_end;
#endif // SUPPORT_PROFILE_GLSPIRV
    SpirvMode mode;
    uint32 hasdepth;
    // ext. glsl instructions have been imported
    uint32 idext;
    uint32 idmax;
    uint32 idmain;
    uint32 id_func_lit;
    uint32 inoutcount;
    uint32 id_var_fragcoord;
    uint32 id_var_vpos;
    uint32 id_var_frontfacing;
    uint32 id_var_vface;
    uint32 id_var_texcoord0_input;
    uint32 id_var_texcoord0_private;
    // ids for types so we can reuse them after they're declared
    uint32 tid[STI_LENGTH_];
    uint32 idtrue;
    uint32 idfalse;
    uint32 id_0_0[4];
    uint32 id_0_125[4];
    uint32 id_0_25[4];
    uint32 id_0_5[4];
    uint32 id_1_0[4];
    uint32 id_2_0[4];
    uint32 id_4_0[4];
    uint32 id_8_0[4];
    uint32 id_flt_max[4];
    struct {
        uint32 idvec4;
        uint32 idivec4;
        uint32 idbool;
    } uniform_arrays;
    uint32 id_uniform_block;
    struct {
        uint32 idvec4;
    } constant_arrays;
    struct {
        ComponentList f;
        ComponentList i;
        ComponentList u;
    } cl;

    SpirvPatchTable patch_table;

    // Required only on ps_1_3 and below, which only has 4 registers for this purpose.
    struct {
        uint32 idtexbem;
        uint32 idtexbeml;
    } sampler_extras[4];

    // TEX opcode in ps_1_3 and below has one implicit texcoord input attribute for each texture
    // register. We use this array to hold SSA id of this input attribute (see emit_SPIRV_global
    // for details).
    uint32 id_implicit_input[4];

    int loop_stack_idx;
    SpirvLoopInfo loop_stack[32];
} SpirvContext;

#endif // if SUPPORT_PROFILE_SPIRV

#endif
