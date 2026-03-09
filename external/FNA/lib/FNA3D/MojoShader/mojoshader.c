/**
 * MojoShader; generate shader programs from bytecode of compiled
 *  Direct3D shaders.
 *
 * Please see the file LICENSE.txt in the source's root directory.
 *
 *  This file written by Ryan C. Gordon.
 */

// !!! FIXME: this file really needs to be split up.
// !!! FIXME: I keep changing coding styles for symbols and typedefs.

// !!! FIXME: rules from MSDN about temp registers we probably don't check.
// - There are limited temporaries: vs_1_1 has 12 (ps_1_1 has _2_!).
// - SM2 apparently was variable, between 12 and 32. Shader Model 3 has 32.
// - A maximum of three temp registers can be used in a single instruction.

#define __MOJOSHADER_INTERNAL__ 1
#include "profiles/mojoshader_profile.h"

// Deal with register lists...  !!! FIXME: I sort of hate this.

static void free_reglist(MOJOSHADER_free f, void *d, RegisterList *item)
{
    while (item != NULL)
    {
        RegisterList *next = item->next;
        f(item, d);
        item = next;
    } // while
} // free_reglist

static inline const RegisterList *reglist_exists(RegisterList *prev,
                                                 const RegisterType regtype,
                                                 const int regnum)
{
    return (reglist_find(prev, regtype, regnum));
} // reglist_exists

static inline int register_was_written(Context *ctx, const RegisterType rtype,
                                       const int regnum)
{
    RegisterList *reg = reglist_find(&ctx->used_registers, rtype, regnum);
    return (reg && reg->written);
} // register_was_written

static inline int get_defined_register(Context *ctx, const RegisterType rtype,
                                       const int regnum)
{
    return (reglist_exists(&ctx->defined_registers, rtype, regnum) != NULL);
} // get_defined_register

static void add_attribute_register(Context *ctx, const RegisterType rtype,
                                const int regnum, const MOJOSHADER_usage usage,
                                const int index, const int writemask, int flags)
{
    RegisterList *item = reglist_insert(ctx, &ctx->attributes, rtype, regnum);
    item->usage = usage;
    item->index = index;
    item->writemask = writemask;
    item->misc = flags;

    if ((rtype == REG_TYPE_OUTPUT) && (usage == MOJOSHADER_USAGE_POINTSIZE))
        ctx->uses_pointsize = 1;  // note that we have to check this later.
    else if ((rtype == REG_TYPE_OUTPUT) && (usage == MOJOSHADER_USAGE_FOG))
        ctx->uses_fog = 1;  // note that we have to check this later.
} // add_attribute_register

static inline TextureType cvtMojoToD3DSamplerType(const MOJOSHADER_samplerType type)
{
    return (TextureType) (((int) type) + 2);
} // cvtMojoToD3DSamplerType

static inline MOJOSHADER_samplerType cvtD3DToMojoSamplerType(const TextureType type)
{
    return (MOJOSHADER_samplerType) (((int) type) - 2);
} // cvtD3DToMojoSamplerType

static inline void add_sampler(Context *ctx, const int regnum,
                               TextureType ttype, const int texbem)
{
    const RegisterType rtype = REG_TYPE_SAMPLER;

    // !!! FIXME: make sure it doesn't exist?
    // !!! FIXME:  (ps_1_1 assume we can add it multiple times...)
    RegisterList *item = reglist_insert(ctx, &ctx->samplers, rtype, regnum);

    if (ctx->samplermap != NULL)
    {
        unsigned int i;
        for (i = 0; i < ctx->samplermap_count; i++)
        {
            if (ctx->samplermap[i].index == regnum)
            {
                ttype = cvtMojoToD3DSamplerType(ctx->samplermap[i].type);
                break;
            } // if
        } // for
    } // if

    item->index = (int) ttype;
    item->misc |= texbem;
} // add_sampler

static inline void adjust_token_position(Context *ctx, const int incr)
{
    ctx->tokens += incr;
    ctx->tokencount -= incr;
    ctx->current_position += incr * sizeof (uint32);
} // adjust_token_position

// Generate emitter declarations for each profile with this macro...

#define PREDECLARE_PROFILE(prof) \
    void emit_##prof##_start(Context *ctx, const char *profilestr); \
    void emit_##prof##_end(Context *ctx); \
    void emit_##prof##_phase(Context *ctx); \
    void emit_##prof##_finalize(Context *ctx); \
    void emit_##prof##_global(Context *ctx, RegisterType regtype, int regnum);\
    void emit_##prof##_array(Context *ctx, VariableList *var); \
    void emit_##prof##_const_array(Context *ctx, const ConstantsList *clist, \
                                   int base, int size); \
    void emit_##prof##_uniform(Context *ctx, RegisterType regtype, int regnum,\
                               const VariableList *var); \
    void emit_##prof##_sampler(Context *ctx, int stage, TextureType ttype, \
                               int tb); \
    void emit_##prof##_attribute(Context *ctx, RegisterType regtype, \
                                 int regnum, MOJOSHADER_usage usage, \
                                 int index, int wmask, int flags); \
    void emit_##prof##_NOP(Context *ctx); \
    void emit_##prof##_MOV(Context *ctx); \
    void emit_##prof##_ADD(Context *ctx); \
    void emit_##prof##_SUB(Context *ctx); \
    void emit_##prof##_MAD(Context *ctx); \
    void emit_##prof##_MUL(Context *ctx); \
    void emit_##prof##_RCP(Context *ctx); \
    void emit_##prof##_RSQ(Context *ctx); \
    void emit_##prof##_DP3(Context *ctx); \
    void emit_##prof##_DP4(Context *ctx); \
    void emit_##prof##_MIN(Context *ctx); \
    void emit_##prof##_MAX(Context *ctx); \
    void emit_##prof##_SLT(Context *ctx); \
    void emit_##prof##_SGE(Context *ctx); \
    void emit_##prof##_EXP(Context *ctx); \
    void emit_##prof##_LOG(Context *ctx); \
    void emit_##prof##_LIT(Context *ctx); \
    void emit_##prof##_DST(Context *ctx); \
    void emit_##prof##_LRP(Context *ctx); \
    void emit_##prof##_FRC(Context *ctx); \
    void emit_##prof##_M4X4(Context *ctx); \
    void emit_##prof##_M4X3(Context *ctx); \
    void emit_##prof##_M3X4(Context *ctx); \
    void emit_##prof##_M3X3(Context *ctx); \
    void emit_##prof##_M3X2(Context *ctx); \
    void emit_##prof##_CALL(Context *ctx); \
    void emit_##prof##_CALLNZ(Context *ctx); \
    void emit_##prof##_LOOP(Context *ctx); \
    void emit_##prof##_ENDLOOP(Context *ctx); \
    void emit_##prof##_LABEL(Context *ctx); \
    void emit_##prof##_DCL(Context *ctx); \
    void emit_##prof##_POW(Context *ctx); \
    void emit_##prof##_CRS(Context *ctx); \
    void emit_##prof##_SGN(Context *ctx); \
    void emit_##prof##_ABS(Context *ctx); \
    void emit_##prof##_NRM(Context *ctx); \
    void emit_##prof##_SINCOS(Context *ctx); \
    void emit_##prof##_REP(Context *ctx); \
    void emit_##prof##_ENDREP(Context *ctx); \
    void emit_##prof##_IF(Context *ctx); \
    void emit_##prof##_IFC(Context *ctx); \
    void emit_##prof##_ELSE(Context *ctx); \
    void emit_##prof##_ENDIF(Context *ctx); \
    void emit_##prof##_BREAK(Context *ctx); \
    void emit_##prof##_BREAKC(Context *ctx); \
    void emit_##prof##_MOVA(Context *ctx); \
    void emit_##prof##_DEFB(Context *ctx); \
    void emit_##prof##_DEFI(Context *ctx); \
    void emit_##prof##_TEXCRD(Context *ctx); \
    void emit_##prof##_TEXKILL(Context *ctx); \
    void emit_##prof##_TEXLD(Context *ctx); \
    void emit_##prof##_TEXBEM(Context *ctx); \
    void emit_##prof##_TEXBEML(Context *ctx); \
    void emit_##prof##_TEXREG2AR(Context *ctx); \
    void emit_##prof##_TEXREG2GB(Context *ctx); \
    void emit_##prof##_TEXM3X2PAD(Context *ctx); \
    void emit_##prof##_TEXM3X2TEX(Context *ctx); \
    void emit_##prof##_TEXM3X3PAD(Context *ctx); \
    void emit_##prof##_TEXM3X3TEX(Context *ctx); \
    void emit_##prof##_TEXM3X3SPEC(Context *ctx); \
    void emit_##prof##_TEXM3X3VSPEC(Context *ctx); \
    void emit_##prof##_EXPP(Context *ctx); \
    void emit_##prof##_LOGP(Context *ctx); \
    void emit_##prof##_CND(Context *ctx); \
    void emit_##prof##_DEF(Context *ctx); \
    void emit_##prof##_TEXREG2RGB(Context *ctx); \
    void emit_##prof##_TEXDP3TEX(Context *ctx); \
    void emit_##prof##_TEXM3X2DEPTH(Context *ctx); \
    void emit_##prof##_TEXDP3(Context *ctx); \
    void emit_##prof##_TEXM3X3(Context *ctx); \
    void emit_##prof##_TEXDEPTH(Context *ctx); \
    void emit_##prof##_CMP(Context *ctx); \
    void emit_##prof##_BEM(Context *ctx); \
    void emit_##prof##_DP2ADD(Context *ctx); \
    void emit_##prof##_DSX(Context *ctx); \
    void emit_##prof##_DSY(Context *ctx); \
    void emit_##prof##_TEXLDD(Context *ctx); \
    void emit_##prof##_SETP(Context *ctx); \
    void emit_##prof##_TEXLDL(Context *ctx); \
    void emit_##prof##_BREAKP(Context *ctx); \
    void emit_##prof##_RESERVED(Context *ctx); \
    void emit_##prof##_RET(Context *ctx); \
    const char *get_##prof##_varname(Context *ctx, RegisterType rt, \
                                     int regnum); \
    const char *get_##prof##_const_array_varname(Context *ctx, \
                                                 int base, int size);

// Check for profile support...

#define AT_LEAST_ONE_PROFILE 0

#if !SUPPORT_PROFILE_BYTECODE
#define PROFILE_EMITTER_BYTECODE(op)
#else
#undef AT_LEAST_ONE_PROFILE
#define AT_LEAST_ONE_PROFILE 1
#define PROFILE_EMITTER_BYTECODE(op) emit_BYTECODE_##op,
PREDECLARE_PROFILE(BYTECODE)
#endif

#if !SUPPORT_PROFILE_D3D
#define PROFILE_EMITTER_D3D(op)
#else
#undef AT_LEAST_ONE_PROFILE
#define AT_LEAST_ONE_PROFILE 1
#define PROFILE_EMITTER_D3D(op) emit_D3D_##op,
PREDECLARE_PROFILE(D3D)
#endif

#if !SUPPORT_PROFILE_HLSL
#define PROFILE_EMITTER_HLSL(op)
#else
#undef AT_LEAST_ONE_PROFILE
#define AT_LEAST_ONE_PROFILE 1
#define PROFILE_EMITTER_HLSL(op) emit_HLSL_##op,
PREDECLARE_PROFILE(HLSL)
#endif

#if !SUPPORT_PROFILE_GLSL
#define PROFILE_EMITTER_GLSL(op)
#else
#undef AT_LEAST_ONE_PROFILE
#define AT_LEAST_ONE_PROFILE 1
#define PROFILE_EMITTER_GLSL(op) emit_GLSL_##op,
PREDECLARE_PROFILE(GLSL)
#endif

#if !SUPPORT_PROFILE_METAL
#define PROFILE_EMITTER_METAL(op)
#else
#undef AT_LEAST_ONE_PROFILE
#define AT_LEAST_ONE_PROFILE 1
#define PROFILE_EMITTER_METAL(op) emit_METAL_##op,
PREDECLARE_PROFILE(METAL)
#endif

#if !SUPPORT_PROFILE_ARB1
#define PROFILE_EMITTER_ARB1(op)
#else
#undef AT_LEAST_ONE_PROFILE
#define AT_LEAST_ONE_PROFILE 1
#define PROFILE_EMITTER_ARB1(op) emit_ARB1_##op,
PREDECLARE_PROFILE(ARB1)
#endif

#if !SUPPORT_PROFILE_SPIRV
#define PROFILE_EMITTER_SPIRV(op)
#else
#undef AT_LEAST_ONE_PROFILE
#define AT_LEAST_ONE_PROFILE 1
#define PROFILE_EMITTER_SPIRV(op) emit_SPIRV_##op,
PREDECLARE_PROFILE(SPIRV)
#endif

#if !AT_LEAST_ONE_PROFILE
#error No profiles are supported. Fix your build.
#endif

#define DEFINE_PROFILE(prof) { \
    MOJOSHADER_PROFILE_##prof, \
    emit_##prof##_start, \
    emit_##prof##_end, \
    emit_##prof##_phase, \
    emit_##prof##_global, \
    emit_##prof##_array, \
    emit_##prof##_const_array, \
    emit_##prof##_uniform, \
    emit_##prof##_sampler, \
    emit_##prof##_attribute, \
    emit_##prof##_finalize, \
    get_##prof##_varname, \
    get_##prof##_const_array_varname, \
},

static const Profile profiles[] =
{
#if SUPPORT_PROFILE_D3D
    DEFINE_PROFILE(D3D)
#endif
#if SUPPORT_PROFILE_BYTECODE
    DEFINE_PROFILE(BYTECODE)
#endif
#if SUPPORT_PROFILE_HLSL
    DEFINE_PROFILE(HLSL)
#endif
#if SUPPORT_PROFILE_GLSL
    DEFINE_PROFILE(GLSL)
#endif
#if SUPPORT_PROFILE_ARB1
    DEFINE_PROFILE(ARB1)
#endif
#if SUPPORT_PROFILE_METAL
    DEFINE_PROFILE(METAL)
#endif
#if SUPPORT_PROFILE_SPIRV
    DEFINE_PROFILE(SPIRV)
#endif
};

#undef DEFINE_PROFILE

// This is for profiles that extend other profiles...
static const struct { const char *from; const char *to; } profileMap[] =
{
    { MOJOSHADER_PROFILE_GLSPIRV, MOJOSHADER_PROFILE_SPIRV },
    { MOJOSHADER_PROFILE_GLSLES, MOJOSHADER_PROFILE_GLSL },
    { MOJOSHADER_PROFILE_GLSLES3, MOJOSHADER_PROFILE_GLSL },
    { MOJOSHADER_PROFILE_GLSL120, MOJOSHADER_PROFILE_GLSL },
    { MOJOSHADER_PROFILE_NV2, MOJOSHADER_PROFILE_ARB1 },
    { MOJOSHADER_PROFILE_NV3, MOJOSHADER_PROFILE_ARB1 },
    { MOJOSHADER_PROFILE_NV4, MOJOSHADER_PROFILE_ARB1 },
};

// The PROFILE_EMITTER_* items MUST be in the same order as profiles[]!
#define PROFILE_EMITTERS(op) { \
     PROFILE_EMITTER_D3D(op) \
     PROFILE_EMITTER_BYTECODE(op) \
     PROFILE_EMITTER_HLSL(op) \
     PROFILE_EMITTER_GLSL(op) \
     PROFILE_EMITTER_ARB1(op) \
     PROFILE_EMITTER_METAL(op) \
     PROFILE_EMITTER_SPIRV(op) \
}

static int parse_destination_token(Context *ctx, DestArgInfo *info)
{
    // !!! FIXME: recheck against the spec for ranges (like RASTOUT values, etc).
    if (ctx->tokencount == 0)
    {
        fail(ctx, "Out of tokens in destination parameter");
        return 0;
    } // if

    const uint32 token = SWAP32(*(ctx->tokens));
    const int reserved1 = (int) ((token >> 14) & 0x3); // bits 14 through 15
    const int reserved2 = (int) ((token >> 31) & 0x1); // bit 31

    info->token = ctx->tokens;
    info->regnum = (int) (token & 0x7ff);  // bits 0 through 10
    info->relative = (int) ((token >> 13) & 0x1); // bit 13
    info->orig_writemask = (int) ((token >> 16) & 0xF); // bits 16 through 19
    info->result_mod = (int) ((token >> 20) & 0xF); // bits 20 through 23
    info->result_shift = (int) ((token >> 24) & 0xF); // bits 24 through 27      abc
    info->regtype = (RegisterType) (((token >> 28) & 0x7) | ((token >> 8) & 0x18));  // bits 28-30, 11-12

    int writemask;
    if (isscalar(ctx, ctx->shader_type, info->regtype, info->regnum))
        writemask = 0x1;  // just x.
    else
        writemask = info->orig_writemask;

    set_dstarg_writemask(info, writemask);  // bits 16 through 19.

    // all the REG_TYPE_CONSTx types are the same register type, it's just
    //  split up so its regnum can be > 2047 in the bytecode. Clean it up.
    if (info->regtype == REG_TYPE_CONST2)
    {
        info->regtype = REG_TYPE_CONST;
        info->regnum += 2048;
    } // else if
    else if (info->regtype == REG_TYPE_CONST3)
    {
        info->regtype = REG_TYPE_CONST;
        info->regnum += 4096;
    } // else if
    else if (info->regtype == REG_TYPE_CONST4)
    {
        info->regtype = REG_TYPE_CONST;
        info->regnum += 6144;
    } // else if

    // swallow token for now, for multiple calls in a row.
    adjust_token_position(ctx, 1);

    if (reserved1 != 0x0)
        fail(ctx, "Reserved bit #1 in destination token must be zero");

    if (reserved2 != 0x1)
        fail(ctx, "Reserved bit #2 in destination token must be one");

    if (info->relative)
    {
        if (!shader_is_vertex(ctx))
            fail(ctx, "Relative addressing in non-vertex shader");
        if (!shader_version_atleast(ctx, 3, 0))
            fail(ctx, "Relative addressing in vertex shader version < 3.0");
        if ((!ctx->ctab.have_ctab) && (!ctx->ignores_ctab))
        {
            // it's hard to do this efficiently without!
            fail(ctx, "relative addressing unsupported without a CTAB");
        } // if

        // !!! FIXME: I don't have a shader that has a relative dest currently.
        fail(ctx, "Relative addressing of dest tokens is unsupported");
        return 2;
    } // if

    const int s = info->result_shift;
    if (s != 0)
    {
        if (!shader_is_pixel(ctx))
            fail(ctx, "Result shift scale in non-pixel shader");
        if (shader_version_atleast(ctx, 2, 0))
            fail(ctx, "Result shift scale in pixel shader version >= 2.0");
        if ( ! (((s >= 1) && (s <= 3)) || ((s >= 0xD) && (s <= 0xF))) )
            fail(ctx, "Result shift scale isn't 1 to 3, or 13 to 15.");
    } // if

    if (info->result_mod & MOD_PP)  // Partial precision (pixel shaders only)
    {
        if (!shader_is_pixel(ctx))
            fail(ctx, "Partial precision result mod in non-pixel shader");
    } // if

    if (info->result_mod & MOD_CENTROID)  // Centroid (pixel shaders only)
    {
        if (!shader_is_pixel(ctx))
            fail(ctx, "Centroid result mod in non-pixel shader");
        else if (!ctx->centroid_allowed)  // only on DCL opcodes!
            fail(ctx, "Centroid modifier not allowed here");
    } // if

    if (/*(info->regtype < 0) ||*/ (info->regtype > REG_TYPE_MAX))
        fail(ctx, "Register type is out of range");

    if (!isfail(ctx))
        set_used_register(ctx, info->regtype, info->regnum, 1);

    return 1;
} // parse_destination_token


static void determine_constants_arrays(Context *ctx)
{
    // Only process this stuff once. This is called after all DEF* opcodes
    //  could have been parsed.
    if (ctx->determined_constants_arrays)
        return;

    ctx->determined_constants_arrays = 1;

    if (ctx->constant_count <= 1)
        return;  // nothing to sort or group.

    // Sort the linked list into an array for easier tapdancing...
    ConstantsList **array = (ConstantsList **) alloca(sizeof (ConstantsList *) * (ctx->constant_count + 1));
    ConstantsList *item = ctx->constants;
    int i;

    for (i = 0; i < ctx->constant_count; i++)
    {
        if (item == NULL)
        {
            fail(ctx, "BUG: mismatched constant list and count");
            return;
        } // if

        array[i] = item;
        item = item->next;
    } // for

    array[ctx->constant_count] = NULL;

    // bubble sort ftw.
    int sorted;
    do
    {
        sorted = 1;
        for (i = 0; i < ctx->constant_count-1; i++)
        {
            if (array[i]->constant.index > array[i+1]->constant.index)
            {
                ConstantsList *tmp = array[i];
                array[i] = array[i+1];
                array[i+1] = tmp;
                sorted = 0;
            } // if
        } // for
    } while (!sorted);

    // okay, sorted. While we're here, let's redo the linked list in order...
    for (i = 0; i < ctx->constant_count; i++)
        array[i]->next = array[i+1];
    ctx->constants = array[0];

    // now figure out the groupings of constants and add to ctx->variables...
    int start = -1;
    int prev = -1;
    int count = 0;
    const int hi = ctx->constant_count;
    for (i = 0; i <= hi; i++)
    {
        if (array[i] && (array[i]->constant.type != MOJOSHADER_UNIFORM_FLOAT))
            continue;  // we only care about REG_TYPE_CONST for array groups.

        if (start == -1)
        {
            prev = start = i;  // first REG_TYPE_CONST we've seen. Mark it!
            continue;
        } // if

        // not a match (or last item in the array)...see if we had a
        //  contiguous set before this point...
        if ( (array[i]) && (array[i]->constant.index == (array[prev]->constant.index + 1)) )
            count++;
        else
        {
            if (count > 0)  // multiple constants in the set?
            {
                VariableList *var;
                var = (VariableList *) Malloc(ctx, sizeof (VariableList));
                if (var == NULL)
                    break;

                var->type = MOJOSHADER_UNIFORM_FLOAT;
                var->index = array[start]->constant.index;
                var->count = (array[prev]->constant.index - var->index) + 1;
                var->constant = array[start];
                var->used = 0;
                var->emit_position = -1;
                var->next = ctx->variables;
                ctx->variables = var;
            } // if

            start = i;   // set this as new start of sequence.
        } // if

        prev = i;
    } // for
} // determine_constants_arrays

static void shader_model_1_input_usage(const int regnum, MOJOSHADER_usage *usage, int *index)
{
    *index = 0;
    switch (regnum)  // these are hardcoded for Shader Model 1: v0 is POSITION, v1 is BLENDWEIGHT, etc.
    {
        case 0: *usage = MOJOSHADER_USAGE_POSITION; break;
        case 1: *usage = MOJOSHADER_USAGE_BLENDWEIGHT; break;
        case 2: *usage = MOJOSHADER_USAGE_BLENDINDICES; break;
        case 3: *usage = MOJOSHADER_USAGE_NORMAL; break;
        case 4: *usage = MOJOSHADER_USAGE_POINTSIZE; break;
        case 5: *usage = MOJOSHADER_USAGE_COLOR; break;  // diffuse
        case 6: *usage = MOJOSHADER_USAGE_COLOR; *index = 1; break; // specular
        case 7: *usage = MOJOSHADER_USAGE_TEXCOORD; break;
        case 8: *usage = MOJOSHADER_USAGE_TEXCOORD; *index = 1; break;
        case 9: *usage = MOJOSHADER_USAGE_TEXCOORD; *index = 2; break;
        case 10: *usage = MOJOSHADER_USAGE_TEXCOORD; *index = 3; break;
        case 11: *usage = MOJOSHADER_USAGE_TEXCOORD; *index = 4; break;
        case 12: *usage = MOJOSHADER_USAGE_TEXCOORD; *index = 5; break;
        case 13: *usage = MOJOSHADER_USAGE_TEXCOORD; *index = 6; break;
        case 14: *usage = MOJOSHADER_USAGE_TEXCOORD; *index = 7; break;
        case 15: *usage = MOJOSHADER_USAGE_POSITION; *index = 1; break;
        case 16: *usage = MOJOSHADER_USAGE_NORMAL; *index = 1; break;
        default: *usage = MOJOSHADER_USAGE_UNKNOWN; break;
    } // switch
} // shader_model_1_input_usage

static int adjust_swizzle(const Context *ctx, const RegisterType regtype,
                          const int regnum, const int swizzle)
{
    if (regtype != REG_TYPE_INPUT)  // !!! FIXME: maybe lift this later?
        return swizzle;
    else if (ctx->swizzles_count == 0)
        return swizzle;

    MOJOSHADER_usage usage = MOJOSHADER_USAGE_UNKNOWN;
    int index = 0;

    // Shader Model 1 didn't have to predeclare attribute variables, so
    //  reglist_find won't be able to look them up at this point, but
    //  their usages don't change.
    if (!shader_version_atleast(ctx, 2, 0))
        shader_model_1_input_usage(regnum, &usage, &index);
    else
    {
        const RegisterList *reg = reglist_find(&ctx->attributes, regtype, regnum);
        if (reg == NULL)
            return swizzle;
        usage = reg->usage;
        index = reg->index;
    } // else

    if (usage == MOJOSHADER_USAGE_UNKNOWN)
        return swizzle;

    size_t i;
    for (i = 0; i < ctx->swizzles_count; i++)
    {
        const MOJOSHADER_swizzle *swiz = &ctx->swizzles[i];
        if ((swiz->usage == usage) && (swiz->index == index))
        {
            return ( (((int)(swiz->swizzles[((swizzle >> 0) & 0x3)])) << 0) |
                     (((int)(swiz->swizzles[((swizzle >> 2) & 0x3)])) << 2) |
                     (((int)(swiz->swizzles[((swizzle >> 4) & 0x3)])) << 4) |
                     (((int)(swiz->swizzles[((swizzle >> 6) & 0x3)])) << 6) );
        } // if
    } // for

    return swizzle;
} // adjust_swizzle


static int parse_source_token(Context *ctx, SourceArgInfo *info)
{
    int retval = 1;

    if (ctx->tokencount == 0)
    {
        fail(ctx, "Out of tokens in source parameter");
        return 0;
    } // if

    const uint32 token = SWAP32(*(ctx->tokens));
    const int reserved1 = (int) ((token >> 14) & 0x3); // bits 14 through 15
    const int reserved2 = (int) ((token >> 31) & 0x1); // bit 31

    info->token = ctx->tokens;
    info->regnum = (int) (token & 0x7ff);  // bits 0 through 10
    info->relative = (int) ((token >> 13) & 0x1); // bit 13
    const int swizzle = (int) ((token >> 16) & 0xFF); // bits 16 through 23
    info->src_mod = (SourceMod) ((token >> 24) & 0xF); // bits 24 through 27
    info->regtype = (RegisterType) (((token >> 28) & 0x7) | ((token >> 8) & 0x18));  // bits 28-30, 11-12

    // all the REG_TYPE_CONSTx types are the same register type, it's just
    //  split up so its regnum can be > 2047 in the bytecode. Clean it up.
    if (info->regtype == REG_TYPE_CONST2)
    {
        info->regtype = REG_TYPE_CONST;
        info->regnum += 2048;
    } // else if
    else if (info->regtype == REG_TYPE_CONST3)
    {
        info->regtype = REG_TYPE_CONST;
        info->regnum += 4096;
    } // else if
    else if (info->regtype == REG_TYPE_CONST4)
    {
        info->regtype = REG_TYPE_CONST;
        info->regnum += 6144;
    } // else if

    info->swizzle = adjust_swizzle(ctx, info->regtype, info->regnum, swizzle);
    info->swizzle_x = ((info->swizzle >> 0) & 0x3);
    info->swizzle_y = ((info->swizzle >> 2) & 0x3);
    info->swizzle_z = ((info->swizzle >> 4) & 0x3);
    info->swizzle_w = ((info->swizzle >> 6) & 0x3);

    // swallow token for now, for multiple calls in a row.
    adjust_token_position(ctx, 1);

    if (reserved1 != 0x0)
        fail(ctx, "Reserved bits #1 in source token must be zero");

    if (reserved2 != 0x1)
        fail(ctx, "Reserved bit #2 in source token must be one");

    if ((info->relative) && (ctx->tokencount == 0))
    {
        fail(ctx, "Out of tokens in relative source parameter");
        info->relative = 0;  // don't try to process it.
    } // if

    if (info->relative)
    {
        if ( (shader_is_pixel(ctx)) && (!shader_version_atleast(ctx, 3, 0)) )
            fail(ctx, "Relative addressing in pixel shader version < 3.0");

        // Shader Model 1 doesn't have an extra token to specify the
        //  relative register: it's always a0.x.
        if (!shader_version_atleast(ctx, 2, 0))
        {
            info->relative_regnum = 0;
            info->relative_regtype = REG_TYPE_ADDRESS;
            info->relative_component = 0;
        } // if

        else  // Shader Model 2 and later...
        {
            const uint32 reltoken = SWAP32(*(ctx->tokens));
            // swallow token for now, for multiple calls in a row.
            adjust_token_position(ctx, 1);

            const int relswiz = (int) ((reltoken >> 16) & 0xFF);
            info->relative_regnum = (int) (reltoken & 0x7ff);
            info->relative_regtype = (RegisterType)
                                        (((reltoken >> 28) & 0x7) |
                                        ((reltoken >> 8) & 0x18));

            if (((reltoken >> 31) & 0x1) == 0)
                fail(ctx, "bit #31 in relative address must be set");

            if ((reltoken & 0xF00E000) != 0)  // usused bits.
                fail(ctx, "relative address reserved bit must be zero");

            switch (info->relative_regtype)
            {
                case REG_TYPE_LOOP:
                case REG_TYPE_ADDRESS:
                    break;
                default:
                    fail(ctx, "invalid register for relative address");
                    break;
            } // switch

            if (info->relative_regnum != 0)  // true for now.
                fail(ctx, "invalid register for relative address");

            if ( (info->relative_regtype != REG_TYPE_LOOP) && !replicate_swizzle(relswiz) )
                fail(ctx, "relative address needs replicate swizzle");

            info->relative_component = (relswiz & 0x3);

            retval++;
        } // else

        if (info->regtype == REG_TYPE_INPUT)
        {
            if ( (shader_is_pixel(ctx)) || (!shader_version_atleast(ctx, 3, 0)) )
                fail(ctx, "relative addressing of input registers not supported in this shader model");
            ctx->have_relative_input_registers = 1;
        } // if
        else if (info->regtype == REG_TYPE_CONST)
        {
            // figure out what array we're in...
            if (!ctx->ignores_ctab)
            {
                if (!ctx->ctab.have_ctab)  // hard to do efficiently without!
                    fail(ctx, "relative addressing unsupported without a CTAB");
                else
                {
                    determine_constants_arrays(ctx);

                    VariableList *var;
                    const int reltarget = info->regnum;
                    for (var = ctx->variables; var != NULL; var = var->next)
                    {
                        const int lo = var->index;
                        if ( (reltarget >= lo) && (reltarget < (lo + var->count)) )
                            break;  // match!
                    } // for

                    if (var == NULL)
                        fail(ctx, "relative addressing of indeterminate array");
                    else
                    {
                        var->used = 1;
                        info->relative_array = var;
                        set_used_register(ctx, info->relative_regtype, info->relative_regnum, 0);
                    } // else
                } // else
            } // if
        } // else if
        else
        {
            fail(ctx, "relative addressing of invalid register");
        } // else
    } // if

    switch (info->src_mod)
    {
        case SRCMOD_NONE:
        case SRCMOD_ABSNEGATE:
        case SRCMOD_ABS:
        case SRCMOD_NEGATE:
            break; // okay in any shader model.

        // apparently these are only legal in Shader Model 1.x ...
        case SRCMOD_BIASNEGATE:
        case SRCMOD_BIAS:
        case SRCMOD_SIGNNEGATE:
        case SRCMOD_SIGN:
        case SRCMOD_COMPLEMENT:
        case SRCMOD_X2NEGATE:
        case SRCMOD_X2:
        case SRCMOD_DZ:
        case SRCMOD_DW:
            if (shader_version_atleast(ctx, 2, 0))
                fail(ctx, "illegal source mod for this Shader Model.");
            break;

        case SRCMOD_NOT:  // !!! FIXME: I _think_ this is right...
            if (shader_version_atleast(ctx, 2, 0))
            {
                if (info->regtype != REG_TYPE_PREDICATE
                 && info->regtype != REG_TYPE_CONSTBOOL)
                    fail(ctx, "NOT only allowed on bool registers.");
            } // if
            break;

        default:
            fail(ctx, "Unknown source modifier");
    } // switch

    // !!! FIXME: docs say this for sm3 ... check these!
    //  "The negate modifier cannot be used on second source register of these
    //   instructions: m3x2 - ps, m3x3 - ps, m3x4 - ps, m4x3 - ps, and
    //   m4x4 - ps."
    //  "If any version 3 shader reads from one or more constant float
    //   registers (c#), one of the following must be true.
    //    All of the constant floating-point registers must use the abs modifier.
    //    None of the constant floating-point registers can use the abs modifier.

    if (!isfail(ctx))
    {
        RegisterList *reg;
        reg = set_used_register(ctx, info->regtype, info->regnum, 0);
        // !!! FIXME: this test passes if you write to the register
        // !!! FIXME:  in this same instruction, because we parse the
        // !!! FIXME:  destination token first.
        // !!! FIXME: Microsoft's shader validation explicitly checks temp
        // !!! FIXME:  registers for this...do they check other writable ones?
        if ((info->regtype == REG_TYPE_TEMP) && (reg) && (!reg->written))
            failf(ctx, "Temp register r%d used uninitialized", info->regnum);
    } // if

    return retval;
} // parse_source_token


static int parse_predicated_token(Context *ctx)
{
    SourceArgInfo *arg = &ctx->predicate_arg;
    parse_source_token(ctx, arg);
    if (arg->regtype != REG_TYPE_PREDICATE)
        fail(ctx, "Predicated instruction but not predicate register!");
    if ((arg->src_mod != SRCMOD_NONE) && (arg->src_mod != SRCMOD_NOT))
        fail(ctx, "Predicated instruction register is not NONE or NOT");
    if ( !no_swizzle(arg->swizzle) && !replicate_swizzle(arg->swizzle) )
        fail(ctx, "Predicated instruction register has wrong swizzle");
    if (arg->relative)  // I'm pretty sure this is illegal...?
        fail(ctx, "relative addressing in predicated token");

    return 1;
} // parse_predicated_token


static int parse_args_NULL(Context *ctx)
{
    return 1;
} // parse_args_NULL


static int parse_args_DEF(Context *ctx)
{
    parse_destination_token(ctx, &ctx->dest_arg);
    if (ctx->dest_arg.regtype != REG_TYPE_CONST)
        fail(ctx, "DEF using non-CONST register");
    if (ctx->dest_arg.relative)  // I'm pretty sure this is illegal...?
        fail(ctx, "relative addressing in DEF");

    ctx->dwords[0] = SWAP32(ctx->tokens[0]);
    ctx->dwords[1] = SWAP32(ctx->tokens[1]);
    ctx->dwords[2] = SWAP32(ctx->tokens[2]);
    ctx->dwords[3] = SWAP32(ctx->tokens[3]);

    return 6;
} // parse_args_DEF


static int parse_args_DEFI(Context *ctx)
{
    parse_destination_token(ctx, &ctx->dest_arg);
    if (ctx->dest_arg.regtype != REG_TYPE_CONSTINT)
        fail(ctx, "DEFI using non-CONSTING register");
    if (ctx->dest_arg.relative)  // I'm pretty sure this is illegal...?
        fail(ctx, "relative addressing in DEFI");

    ctx->dwords[0] = SWAP32(ctx->tokens[0]);
    ctx->dwords[1] = SWAP32(ctx->tokens[1]);
    ctx->dwords[2] = SWAP32(ctx->tokens[2]);
    ctx->dwords[3] = SWAP32(ctx->tokens[3]);

    return 6;
} // parse_args_DEFI


static int parse_args_DEFB(Context *ctx)
{
    parse_destination_token(ctx, &ctx->dest_arg);
    if (ctx->dest_arg.regtype != REG_TYPE_CONSTBOOL)
        fail(ctx, "DEFB using non-CONSTBOOL register");
    if (ctx->dest_arg.relative)  // I'm pretty sure this is illegal...?
        fail(ctx, "relative addressing in DEFB");

    ctx->dwords[0] = *(ctx->tokens) ? 1 : 0;

    return 3;
} // parse_args_DEFB


static int valid_texture_type(const uint32 ttype)
{
    switch ((const TextureType) ttype)
    {
        case TEXTURE_TYPE_2D:
        case TEXTURE_TYPE_CUBE:
        case TEXTURE_TYPE_VOLUME:
            return 1;  // it's okay.
    } // switch

    return 0;
} // valid_texture_type


// !!! FIXME: this function is kind of a mess.
static int parse_args_DCL(Context *ctx)
{
    int unsupported = 0;
    const uint32 token = SWAP32(*(ctx->tokens));
    const int reserved1 = (int) ((token >> 31) & 0x1); // bit 31
    uint32 reserved_mask = 0x00000000;

    if (reserved1 != 0x1)
        fail(ctx, "Bit #31 in DCL token must be one");

    ctx->centroid_allowed = 1;
    adjust_token_position(ctx, 1);
    parse_destination_token(ctx, &ctx->dest_arg);
    ctx->centroid_allowed = 0;

    if (ctx->dest_arg.result_shift != 0)  // I'm pretty sure this is illegal...?
        fail(ctx, "shift scale in DCL");
    if (ctx->dest_arg.relative)  // I'm pretty sure this is illegal...?
        fail(ctx, "relative addressing in DCL");

    const RegisterType regtype = ctx->dest_arg.regtype;
    const int regnum = ctx->dest_arg.regnum;
    if ( (shader_is_pixel(ctx)) && (shader_version_atleast(ctx, 3, 0)) )
    {
        if (regtype == REG_TYPE_INPUT)
        {
            const uint32 usage = (token & 0xF);
            const uint32 index = ((token >> 16) & 0xF);
            reserved_mask = 0x7FF0FFE0;
            ctx->dwords[0] = usage;
            ctx->dwords[1] = index;
        } // if

        else if (regtype == REG_TYPE_MISCTYPE)
        {
            const MiscTypeType mt = (MiscTypeType) regnum;
            if (mt == MISCTYPE_TYPE_POSITION)
                reserved_mask = 0x7FFFFFFF;
            else if (mt == MISCTYPE_TYPE_FACE)
            {
                reserved_mask = 0x7FFFFFFF;
                if (!writemask_xyzw(ctx->dest_arg.orig_writemask))
                    fail(ctx, "DCL face writemask must be full");
                if (ctx->dest_arg.result_mod != 0)
                    fail(ctx, "DCL face result modifier must be zero");
                if (ctx->dest_arg.result_shift != 0)
                    fail(ctx, "DCL face shift scale must be zero");
            } // else if
            else
            {
                unsupported = 1;
            } // else

            ctx->dwords[0] = (uint32) MOJOSHADER_USAGE_UNKNOWN;
            ctx->dwords[1] = 0;
        } // else if

        else if (regtype == REG_TYPE_TEXTURE)
        {
            const uint32 usage = (token & 0xF);
            const uint32 index = ((token >> 16) & 0xF);
            if (usage == MOJOSHADER_USAGE_TEXCOORD)
            {
                if (index > 7)
                    fail(ctx, "DCL texcoord usage must have 0-7 index");
            } // if
            else if (usage == MOJOSHADER_USAGE_COLOR)
            {
                if (index != 0)
                    fail(ctx, "DCL color usage must have 0 index");
            } // else if
            else
            {
                fail(ctx, "Invalid DCL texture usage");
            } // else

            reserved_mask = 0x7FF0FFE0;
            ctx->dwords[0] = usage;
            ctx->dwords[1] = index;
        } // else if

        else if (regtype == REG_TYPE_SAMPLER)
        {
            const uint32 ttype = ((token >> 27) & 0xF);
            if (!valid_texture_type(ttype))
                fail(ctx, "unknown sampler texture type");
            reserved_mask = 0x7FFFFFF;
            ctx->dwords[0] = ttype;
        } // else if

        else
        {
            unsupported = 1;
        } // else
    } // if

    else if ( (shader_is_pixel(ctx)) && (shader_version_atleast(ctx, 2, 0)) )
    {
        if (regtype == REG_TYPE_INPUT)
        {
            ctx->dwords[0] = (uint32) MOJOSHADER_USAGE_COLOR;
            ctx->dwords[1] = regnum;
            reserved_mask = 0x7FFFFFFF;
        } // if
        else if (regtype == REG_TYPE_TEXTURE)
        {
            ctx->dwords[0] = (uint32) MOJOSHADER_USAGE_TEXCOORD;
            ctx->dwords[1] = regnum;
            reserved_mask = 0x7FFFFFFF;
        } // else if
        else if (regtype == REG_TYPE_SAMPLER)
        {
            const uint32 ttype = ((token >> 27) & 0xF);
            if (!valid_texture_type(ttype))
                fail(ctx, "unknown sampler texture type");
            reserved_mask = 0x7FFFFFF;
            ctx->dwords[0] = ttype;
        } // else if
        else
        {
            unsupported = 1;
        } // else
    } // if

    else if ( (shader_is_vertex(ctx)) && (shader_version_atleast(ctx, 3, 0)) )
    {
        if ((regtype == REG_TYPE_INPUT) || (regtype == REG_TYPE_OUTPUT))
        {
            const uint32 usage = (token & 0xF);
            const uint32 index = ((token >> 16) & 0xF);
            reserved_mask = 0x7FF0FFE0;
            ctx->dwords[0] = usage;
            ctx->dwords[1] = index;
        } // if
        else if (regtype == REG_TYPE_TEXTURE)
        {
            const uint32 usage = (token & 0xF);
            const uint32 index = ((token >> 16) & 0xF);
            if (usage == MOJOSHADER_USAGE_TEXCOORD)
            {
                if (index > 7)
                    fail(ctx, "DCL texcoord usage must have 0-7 index");
            } // if
            else if (usage == MOJOSHADER_USAGE_COLOR)
            {
                if (index != 0)
                    fail(ctx, "DCL texcoord usage must have 0 index");
            } // else if
            else
                fail(ctx, "Invalid DCL texture usage");

            reserved_mask = 0x7FF0FFE0;
            ctx->dwords[0] = usage;
            ctx->dwords[1] = index;
        } // else if
        else if (regtype == REG_TYPE_SAMPLER)
        {
            const uint32 ttype = ((token >> 27) & 0xF);
            if (!valid_texture_type(ttype))
                fail(ctx, "Unknown sampler texture type");
            reserved_mask = 0x0FFFFFFF;
            ctx->dwords[0] = ttype;
        } // else if
        else
        {
            unsupported = 1;
        } // else
    } // else if

    else if ( (shader_is_vertex(ctx)) && (shader_version_atleast(ctx, 1, 1)) )
    {
        if (regtype == REG_TYPE_INPUT)
        {
            const uint32 usage = (token & 0xF);
            const uint32 index = ((token >> 16) & 0xF);
            reserved_mask = 0x7FF0FFE0;
            ctx->dwords[0] = usage;
            ctx->dwords[1] = index;
        } // if
        else
        {
            unsupported = 1;
        } // else
    } // else if

    else
    {
        unsupported = 1;
    } // else

    if (unsupported)
        fail(ctx, "invalid DCL register type for this shader model");

    if ((token & reserved_mask) != 0)
        fail(ctx, "reserved bits in DCL dword aren't zero");

    return 3;
} // parse_args_DCL


static int parse_args_D(Context *ctx)
{
    int retval = 1;
    retval += parse_destination_token(ctx, &ctx->dest_arg);
    return retval;
} // parse_args_D


static int parse_args_S(Context *ctx)
{
    int retval = 1;
    retval += parse_source_token(ctx, &ctx->source_args[0]);
    return retval;
} // parse_args_S


static int parse_args_SS(Context *ctx)
{
    int retval = 1;
    retval += parse_source_token(ctx, &ctx->source_args[0]);
    retval += parse_source_token(ctx, &ctx->source_args[1]);
    return retval;
} // parse_args_SS


static int parse_args_DS(Context *ctx)
{
    int retval = 1;
    retval += parse_destination_token(ctx, &ctx->dest_arg);
    retval += parse_source_token(ctx, &ctx->source_args[0]);
    return retval;
} // parse_args_DS


static int parse_args_DSS(Context *ctx)
{
    int retval = 1;
    retval += parse_destination_token(ctx, &ctx->dest_arg);
    retval += parse_source_token(ctx, &ctx->source_args[0]);
    retval += parse_source_token(ctx, &ctx->source_args[1]);
    return retval;
} // parse_args_DSS


static int parse_args_DSSS(Context *ctx)
{
    int retval = 1;
    retval += parse_destination_token(ctx, &ctx->dest_arg);
    retval += parse_source_token(ctx, &ctx->source_args[0]);
    retval += parse_source_token(ctx, &ctx->source_args[1]);
    retval += parse_source_token(ctx, &ctx->source_args[2]);
    return retval;
} // parse_args_DSSS


static int parse_args_DSSSS(Context *ctx)
{
    int retval = 1;
    retval += parse_destination_token(ctx, &ctx->dest_arg);
    retval += parse_source_token(ctx, &ctx->source_args[0]);
    retval += parse_source_token(ctx, &ctx->source_args[1]);
    retval += parse_source_token(ctx, &ctx->source_args[2]);
    retval += parse_source_token(ctx, &ctx->source_args[3]);
    return retval;
} // parse_args_DSSSS


static int parse_args_SINCOS(Context *ctx)
{
    // this opcode needs extra registers for sm2 and lower.
    if (!shader_version_atleast(ctx, 3, 0))
        return parse_args_DSSS(ctx);
    return parse_args_DS(ctx);
} // parse_args_SINCOS


static int parse_args_TEXCRD(Context *ctx)
{
    // added extra register in ps_1_4.
    if (shader_version_atleast(ctx, 1, 4))
        return parse_args_DS(ctx);
    return parse_args_D(ctx);
} // parse_args_TEXCRD


static int parse_args_TEXLD(Context *ctx)
{
    // different registers in px_1_3, ps_1_4, and ps_2_0!
    if (shader_version_atleast(ctx, 2, 0))
        return parse_args_DSS(ctx);
    else if (shader_version_atleast(ctx, 1, 4))
        return parse_args_DS(ctx);
    return parse_args_D(ctx);
} // parse_args_TEXLD


// State machine functions...

static ConstantsList *alloc_constant_listitem(Context *ctx)
{
    ConstantsList *item = (ConstantsList *) Malloc(ctx, sizeof (ConstantsList));
    if (item == NULL)
        return NULL;

    memset(&item->constant, '\0', sizeof (MOJOSHADER_constant));
    item->next = ctx->constants;
    ctx->constants = item;
    ctx->constant_count++;

    return item;
} // alloc_constant_listitem


static void state_DEF(Context *ctx)
{
    const RegisterType regtype = ctx->dest_arg.regtype;
    const int regnum = ctx->dest_arg.regnum;

    // !!! FIXME: fail if same register is defined twice.

    if (ctx->instruction_count != 0)
        fail(ctx, "DEF token must come before any instructions");
    else if (regtype != REG_TYPE_CONST)
        fail(ctx, "DEF token using invalid register");
    else
    {
        ConstantsList *item = alloc_constant_listitem(ctx);
        if (item != NULL)
        {
            item->constant.index = regnum;
            item->constant.type = MOJOSHADER_UNIFORM_FLOAT;
            memcpy(item->constant.value.f, ctx->dwords,
                   sizeof (item->constant.value.f));
            set_defined_register(ctx, regtype, regnum);
        } // if
    } // else
} // state_DEF

static void state_DEFI(Context *ctx)
{
    const RegisterType regtype = ctx->dest_arg.regtype;
    const int regnum = ctx->dest_arg.regnum;

    // !!! FIXME: fail if same register is defined twice.

    if (ctx->instruction_count != 0)
        fail(ctx, "DEFI token must come before any instructions");
    else if (regtype != REG_TYPE_CONSTINT)
        fail(ctx, "DEFI token using invalid register");
    else
    {
        ConstantsList *item = alloc_constant_listitem(ctx);
        if (item != NULL)
        {
            item->constant.index = regnum;
            item->constant.type = MOJOSHADER_UNIFORM_INT;
            memcpy(item->constant.value.i, ctx->dwords,
                   sizeof (item->constant.value.i));

            set_defined_register(ctx, regtype, regnum);
        } // if
    } // else
} // state_DEFI

static void state_DEFB(Context *ctx)
{
    const RegisterType regtype = ctx->dest_arg.regtype;
    const int regnum = ctx->dest_arg.regnum;

    // !!! FIXME: fail if same register is defined twice.

    if (ctx->instruction_count != 0)
        fail(ctx, "DEFB token must come before any instructions");
    else if (regtype != REG_TYPE_CONSTBOOL)
        fail(ctx, "DEFB token using invalid register");
    else
    {
        ConstantsList *item = alloc_constant_listitem(ctx);
        if (item != NULL)
        {
            item->constant.index = regnum;
            item->constant.type = MOJOSHADER_UNIFORM_BOOL;
            item->constant.value.b = ctx->dwords[0] ? 1 : 0;
            set_defined_register(ctx, regtype, regnum);
        } // if
    } // else
} // state_DEFB

static void state_DCL(Context *ctx)
{
    const DestArgInfo *arg = &ctx->dest_arg;
    const RegisterType regtype = arg->regtype;
    const int regnum = arg->regnum;
    const int wmask = arg->writemask;
    const int mods = arg->result_mod;

    // parse_args_DCL() does a lot of state checking before we get here.

    // !!! FIXME: apparently vs_3_0 can use sampler registers now.
    // !!! FIXME:  (but only s0 through s3, not all 16 of them.)

    if (ctx->instruction_count != 0)
        fail(ctx, "DCL token must come before any instructions");

    else if (shader_is_vertex(ctx) || shader_is_pixel(ctx))
    {
        if (regtype == REG_TYPE_SAMPLER)
            add_sampler(ctx, regnum, (TextureType) ctx->dwords[0], 0);
        else
        {
            const MOJOSHADER_usage usage = (const MOJOSHADER_usage) ctx->dwords[0];
            const int index = ctx->dwords[1];
            if (usage >= MOJOSHADER_USAGE_TOTAL)
            {
                fail(ctx, "unknown DCL usage");
                return;
            } // if
            add_attribute_register(ctx, regtype, regnum, usage, index, wmask, mods);
        } // else
    } // if

    else
    {
        fail(ctx, "unsupported shader type."); // should be caught elsewhere.
        return;
    } // else

    set_defined_register(ctx, regtype, regnum);
} // state_DCL

static void state_TEXCRD(Context *ctx)
{
    if (shader_version_atleast(ctx, 2, 0))
        fail(ctx, "TEXCRD in Shader Model >= 2.0");  // apparently removed.
} // state_TEXCRD

static void state_FRC(Context *ctx)
{
    const DestArgInfo *dst = &ctx->dest_arg;

    if (dst->result_mod & MOD_SATURATE)  // according to msdn...
        fail(ctx, "FRC destination can't use saturate modifier");

    else if (!shader_version_atleast(ctx, 2, 0))
    {
        if (!writemask_y(dst->writemask) && !writemask_xy(dst->writemask))
            fail(ctx, "FRC writemask must be .y or .xy for shader model 1.x");
    } // else if
} // state_FRC


// replicate the matrix registers to source args. The D3D profile will
//  only use the one legitimate argument, but this saves other profiles
//  from having to build this.
static void srcarg_matrix_replicate(Context *ctx, const int idx,
                                       const int rows)
{
    int i;
    SourceArgInfo *src = &ctx->source_args[idx];
    SourceArgInfo *dst = &ctx->source_args[idx+1];
    for (i = 0; i < (rows-1); i++, dst++)
    {
        memcpy(dst, src, sizeof (SourceArgInfo));
        dst->regnum += (i + 1);
        set_used_register(ctx, dst->regtype, dst->regnum, 0);
    } // for
} // srcarg_matrix_replicate

static void state_M4X4(Context *ctx)
{
    const DestArgInfo *info = &ctx->dest_arg;
    if (!writemask_xyzw(info->writemask))
        fail(ctx, "M4X4 writemask must be full");

// !!! FIXME: MSDN:
//The xyzw (default) mask is required for the destination register. Negate and swizzle modifiers are allowed for src0, but not for src1.
//Swizzle and negate modifiers are invalid for the src0 register. The dest and src0 registers cannot be the same.

    srcarg_matrix_replicate(ctx, 1, 4);
} // state_M4X4

static void state_M4X3(Context *ctx)
{
    const DestArgInfo *info = &ctx->dest_arg;
    if (!writemask_xyz(info->writemask))
        fail(ctx, "M4X3 writemask must be .xyz");

// !!! FIXME: MSDN stuff

    srcarg_matrix_replicate(ctx, 1, 3);
} // state_M4X3

static void state_M3X4(Context *ctx)
{
    const DestArgInfo *info = &ctx->dest_arg;
    if (!writemask_xyzw(info->writemask))
        fail(ctx, "M3X4 writemask must be .xyzw");

// !!! FIXME: MSDN stuff

    srcarg_matrix_replicate(ctx, 1, 4);
} // state_M3X4

static void state_M3X3(Context *ctx)
{
    const DestArgInfo *info = &ctx->dest_arg;
    if (!writemask_xyz(info->writemask))
        fail(ctx, "M3X3 writemask must be .xyz");

// !!! FIXME: MSDN stuff

    srcarg_matrix_replicate(ctx, 1, 3);
} // state_M3X3

static void state_M3X2(Context *ctx)
{
    const DestArgInfo *info = &ctx->dest_arg;
    if (!writemask_xy(info->writemask))
        fail(ctx, "M3X2 writemask must be .xy");

// !!! FIXME: MSDN stuff

    srcarg_matrix_replicate(ctx, 1, 2);
} // state_M3X2

static void state_RET(Context *ctx)
{
    // MSDN all but says that assembly shaders are more or less serialized
    //  HLSL functions, and a RET means you're at the end of one, unlike how
    //  most CPUs would behave. This is actually really helpful,
    //  since we can use high-level constructs and not a mess of GOTOs,
    //  which is a godsend for GLSL...this also means we can consider things
    //  like a LOOP without a matching ENDLOOP within a label's section as
    //  an error.
    if (ctx->loops > 0)
        fail(ctx, "LOOP without ENDLOOP");
    if (ctx->reps > 0)
        fail(ctx, "REP without ENDREP");
} // state_RET

static void check_label_register(Context *ctx, int arg, const char *opcode)
{
    const SourceArgInfo *info = &ctx->source_args[arg];
    const RegisterType regtype = info->regtype;
    const int regnum = info->regnum;

    if (regtype != REG_TYPE_LABEL)
        failf(ctx, "%s with a non-label register specified", opcode);
    if (!shader_version_atleast(ctx, 2, 0))
        failf(ctx, "%s not supported in Shader Model 1", opcode);
    if ((shader_version_atleast(ctx, 2, 255)) && (regnum > 2047))
        fail(ctx, "label register number must be <= 2047");
    if (regnum > 15)
        fail(ctx, "label register number must be <= 15");
} // check_label_register

static void state_LABEL(Context *ctx)
{
    if (ctx->previous_opcode != OPCODE_RET)
        fail(ctx, "LABEL not followed by a RET");
    check_label_register(ctx, 0, "LABEL");
    set_defined_register(ctx, REG_TYPE_LABEL, ctx->source_args[0].regnum);
} // state_LABEL

static void check_call_loop_wrappage(Context *ctx, const int regnum)
{
    // msdn says subroutines inherit aL register if you're in a loop when
    //  you call, and further more _if you ever call this function in a loop,
    //  it must always be called in a loop_. So we'll just pass our loop
    //  variable as a function parameter in those cases.

    const int current_usage = (ctx->loops > 0) ? 1 : -1;
    RegisterList *reg = reglist_find(&ctx->used_registers, REG_TYPE_LABEL, regnum);

    if (reg == NULL)
        fail(ctx, "Invalid label for CALL");
    else if (reg->misc == 0)
        reg->misc = current_usage;
    else if (reg->misc != current_usage)
    {
        if (current_usage == 1)
            fail(ctx, "CALL to this label must be wrapped in LOOP/ENDLOOP");
        else
            fail(ctx, "CALL to this label must not be wrapped in LOOP/ENDLOOP");
    } // else if
} // check_call_loop_wrappage

static void state_CALL(Context *ctx)
{
    check_label_register(ctx, 0, "CALL");
    check_call_loop_wrappage(ctx, ctx->source_args[0].regnum);
} // state_CALL

static void state_CALLNZ(Context *ctx)
{
    const RegisterType regtype = ctx->source_args[1].regtype;
    if ((regtype != REG_TYPE_CONSTBOOL) && (regtype != REG_TYPE_PREDICATE))
        fail(ctx, "CALLNZ argument isn't constbool or predicate register");
    check_label_register(ctx, 0, "CALLNZ");
    check_call_loop_wrappage(ctx, ctx->source_args[0].regnum);
} // state_CALLNZ

static void state_MOVA(Context *ctx)
{
    if (ctx->dest_arg.regtype != REG_TYPE_ADDRESS)
        fail(ctx, "MOVA argument isn't address register");
} // state_MOVA

static void state_RCP(Context *ctx)
{
    if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "RCP without replicate swizzle");
} // state_RCP

static void state_RSQ(Context *ctx)
{
    if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "RSQ without replicate swizzle");
} // state_RSQ

static void state_LOOP(Context *ctx)
{
    if (ctx->source_args[0].regtype != REG_TYPE_LOOP)
        fail(ctx, "LOOP argument isn't loop register");
    else if (ctx->source_args[1].regtype != REG_TYPE_CONSTINT)
        fail(ctx, "LOOP argument isn't constint register");
    else
        ctx->loops++;
} // state_LOOP

static void state_ENDLOOP(Context *ctx)
{
    // !!! FIXME: check that we aren't straddling an IF block.
    if (ctx->loops <= 0)
        fail(ctx, "ENDLOOP without LOOP");
    ctx->loops--;
} // state_ENDLOOP

static void state_BREAKP(Context *ctx)
{
    const RegisterType regtype = ctx->source_args[0].regtype;
    if (regtype != REG_TYPE_PREDICATE)
        fail(ctx, "BREAKP argument isn't predicate register");
    else if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "BREAKP without replicate swizzle");
    else if ((ctx->loops == 0) && (ctx->reps == 0))
        fail(ctx, "BREAKP outside LOOP/ENDLOOP or REP/ENDREP");
} // state_BREAKP

static void state_BREAK(Context *ctx)
{
    if ((ctx->loops == 0) && (ctx->reps == 0))
        fail(ctx, "BREAK outside LOOP/ENDLOOP or REP/ENDREP");
} // state_BREAK

static void state_SETP(Context *ctx)
{
    const RegisterType regtype = ctx->dest_arg.regtype;
    if (regtype != REG_TYPE_PREDICATE)
        fail(ctx, "SETP argument isn't predicate register");
} // state_SETP

static void state_REP(Context *ctx)
{
    const RegisterType regtype = ctx->source_args[0].regtype;
    if (regtype != REG_TYPE_CONSTINT)
        fail(ctx, "REP argument isn't constint register");

    ctx->reps++;
    if (ctx->reps > ctx->max_reps)
        ctx->max_reps = ctx->reps;
} // state_REP

static void state_ENDREP(Context *ctx)
{
    // !!! FIXME: check that we aren't straddling an IF block.
    if (ctx->reps <= 0)
        fail(ctx, "ENDREP without REP");
    ctx->reps--;
} // state_ENDREP

static void state_CMP(Context *ctx)
{
    ctx->cmps++;

    // extra limitations for ps <= 1.4 ...
    if (!shader_version_atleast(ctx, 1, 4))
    {
        int i;
        const DestArgInfo *dst = &ctx->dest_arg;
        const RegisterType dregtype = dst->regtype;
        const int dregnum = dst->regnum;

        if (ctx->cmps > 3)
            fail(ctx, "only 3 CMP instructions allowed in this shader model");

        for (i = 0; i < 3; i++)
        {
            const SourceArgInfo *src = &ctx->source_args[i];
            const RegisterType sregtype = src->regtype;
            const int sregnum = src->regnum;
            if ((dregtype == sregtype) && (dregnum == sregnum))
                fail(ctx, "CMP dest can't match sources in this shader model");
        } // for

        ctx->instruction_count++;  // takes an extra slot in ps_1_2 and _3.
    } // if
} // state_CMP

static void state_DP4(Context *ctx)
{
    // extra limitations for ps <= 1.4 ...
    if (!shader_version_atleast(ctx, 1, 4))
        ctx->instruction_count++;  // takes an extra slot in ps_1_2 and _3.
} // state_DP4

static void state_CND(Context *ctx)
{
    // apparently it was removed...it's not in the docs past ps_1_4 ...
    if (shader_version_atleast(ctx, 2, 0))
        fail(ctx, "CND not allowed in this shader model");

    // extra limitations for ps <= 1.4 ...
    else if (!shader_version_atleast(ctx, 1, 4))
    {
        const SourceArgInfo *src = &ctx->source_args[0];
        if ((src->regtype != REG_TYPE_TEMP) || (src->regnum != 0) ||
            (src->swizzle != 0xFF))
        {
            fail(ctx, "CND src must be r0.a in this shader model");
        } // if
    } // if
} // state_CND

static void state_POW(Context *ctx)
{
    if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "POW src0 must have replicate swizzle");
    else if (!replicate_swizzle(ctx->source_args[1].swizzle))
        fail(ctx, "POW src1 must have replicate swizzle");
} // state_POW

static void state_LOG(Context *ctx)
{
    if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "LOG src0 must have replicate swizzle");
} // state_LOG

static void state_LOGP(Context *ctx)
{
    if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "LOGP src0 must have replicate swizzle");
} // state_LOGP

static void state_SINCOS(Context *ctx)
{
    const DestArgInfo *dst = &ctx->dest_arg;
    const int mask = dst->writemask;
    if (!writemask_x(mask) && !writemask_y(mask) && !writemask_xy(mask))
        fail(ctx, "SINCOS write mask must be .x or .y or .xy");

    else if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "SINCOS src0 must have replicate swizzle");

    else if (dst->result_mod & MOD_SATURATE)  // according to msdn...
        fail(ctx, "SINCOS destination can't use saturate modifier");

    // this opcode needs extra registers, with extra limitations, for <= sm2.
    else if (!shader_version_atleast(ctx, 3, 0))
    {
        int i;
        for (i = 1; i < 3; i++)
        {
            if (ctx->source_args[i].regtype != REG_TYPE_CONST)
            {
                failf(ctx, "SINCOS src%d must be constfloat", i);
                return;
            } // if
        } // for

        if (ctx->source_args[1].regnum == ctx->source_args[2].regnum)
            fail(ctx, "SINCOS src1 and src2 must be different registers");
    } // if
} // state_SINCOS

static void state_IF(Context *ctx)
{
    const RegisterType regtype = ctx->source_args[0].regtype;
    if ((regtype != REG_TYPE_PREDICATE) && (regtype != REG_TYPE_CONSTBOOL))
        fail(ctx, "IF src0 must be CONSTBOOL or PREDICATE");
    // !!! FIXME: track if nesting depth.
} // state_IF

static void state_IFC(Context *ctx)
{
    if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "IFC src0 must have replicate swizzle");
    else if (!replicate_swizzle(ctx->source_args[1].swizzle))
        fail(ctx, "IFC src1 must have replicate swizzle");
    // !!! FIXME: track if nesting depth.
} // state_IFC

static void state_BREAKC(Context *ctx)
{
    if (!replicate_swizzle(ctx->source_args[0].swizzle))
        fail(ctx, "BREAKC src1 must have replicate swizzle");
    else if (!replicate_swizzle(ctx->source_args[1].swizzle))
        fail(ctx, "BREAKC src2 must have replicate swizzle");
    else if ((ctx->loops == 0) && (ctx->reps == 0))
        fail(ctx, "BREAKC outside LOOP/ENDLOOP or REP/ENDREP");
} // state_BREAKC

static void state_TEXKILL(Context *ctx)
{
    // The MSDN docs say this should be a source arg, but the driver docs
    //  say it's a dest arg. That's annoying.
    const DestArgInfo *info = &ctx->dest_arg;
    const RegisterType regtype = info->regtype;
    if (!writemask_xyzw(info->writemask))
        fail(ctx, "TEXKILL writemask must be .xyzw");
    else if ((regtype != REG_TYPE_TEMP) && (regtype != REG_TYPE_TEXTURE))
        fail(ctx, "TEXKILL must use a temp or texture register");

    // !!! FIXME: "If a temporary register is used, all components must have been previously written."
    // !!! FIXME: "If a texture register is used, all components that are read must have been declared."
    // !!! FIXME: there are further limitations in ps_1_3 and earlier.
} // state_TEXKILL

// Some rules that apply to some of the fruity ps_1_1 texture opcodes...
static void state_texops(Context *ctx, const char *opcode,
                         const int dims, const int texbem)
{
    const DestArgInfo *dst = &ctx->dest_arg;
    const SourceArgInfo *src = &ctx->source_args[0];
    if (dst->regtype != REG_TYPE_TEXTURE)
        failf(ctx, "%s destination must be a texture register", opcode);
    if (src->regtype != REG_TYPE_TEXTURE)
        failf(ctx, "%s source must be a texture register", opcode);
    if (src->regnum >= dst->regnum)  // so says MSDN.
        failf(ctx, "%s dest must be a higher register than source", opcode);

    if (dims)
    {
        TextureType ttyp = (dims == 2) ? TEXTURE_TYPE_2D : TEXTURE_TYPE_CUBE;
        add_sampler(ctx, dst->regnum, ttyp, texbem);
    } // if

    add_attribute_register(ctx, REG_TYPE_TEXTURE, dst->regnum,
                           MOJOSHADER_USAGE_TEXCOORD, dst->regnum, 0xF, 0);

    // Strictly speaking, there should be a TEX opcode prior to this call that
    //  should fill in this metadata, but I'm not sure that's required for the
    //  shader to assemble in D3D, so we'll do this so we don't fail with a
    //  cryptic error message even if the developer didn't do the TEX.
    add_attribute_register(ctx, REG_TYPE_TEXTURE, src->regnum,
                           MOJOSHADER_USAGE_TEXCOORD, src->regnum, 0xF, 0);
} // state_texops

static void state_texbem(Context *ctx, const char *opcode)
{
    // The TEXBEM equasion, according to MSDN:
    //u' = TextureCoordinates(stage m)u + D3DTSS_BUMPENVMAT00(stage m)*t(n)R
    //         + D3DTSS_BUMPENVMAT10(stage m)*t(n)G
    //v' = TextureCoordinates(stage m)v + D3DTSS_BUMPENVMAT01(stage m)*t(n)R
    //         + D3DTSS_BUMPENVMAT11(stage m)*t(n)G
    //t(m)RGBA = TextureSample(stage m)
    //
    // ...TEXBEML adds this at the end:
    //t(m)RGBA = t(m)RGBA * [(t(n)B * D3DTSS_BUMPENVLSCALE(stage m)) +
    //           D3DTSS_BUMPENVLOFFSET(stage m)]

    if (shader_version_atleast(ctx, 1, 4))
        failf(ctx, "%s opcode not available after Shader Model 1.3", opcode);

    if (!shader_version_atleast(ctx, 1, 2))
    {
        if (ctx->source_args[0].src_mod == SRCMOD_SIGN)
            failf(ctx, "%s forbids _bx2 on source reg before ps_1_2", opcode);
    } // if

    // !!! FIXME: MSDN:
    // !!! FIXME: Register data that has been read by a texbem
    // !!! FIXME:  or texbeml instruction cannot be read later,
    // !!! FIXME:  except by another texbem or texbeml.

    state_texops(ctx, opcode, 2, 1);
} // state_texbem

static void state_TEXBEM(Context *ctx)
{
    state_texbem(ctx, "TEXBEM");
} // state_TEXBEM

static void state_TEXBEML(Context *ctx)
{
    state_texbem(ctx, "TEXBEML");
} // state_TEXBEML

static void state_TEXM3X2PAD(Context *ctx)
{
    if (shader_version_atleast(ctx, 1, 4))
        fail(ctx, "TEXM3X2PAD opcode not available after Shader Model 1.3");
    state_texops(ctx, "TEXM3X2PAD", 0, 0);
    // !!! FIXME: check for correct opcode existance and order more rigorously?
    ctx->texm3x2pad_src0 = ctx->source_args[0].regnum;
    ctx->texm3x2pad_dst0 = ctx->dest_arg.regnum;
} // state_TEXM3X2PAD

static void state_TEXM3X2TEX(Context *ctx)
{
    if (shader_version_atleast(ctx, 1, 4))
        fail(ctx, "TEXM3X2TEX opcode not available after Shader Model 1.3");
    if (ctx->texm3x2pad_dst0 == -1)
        fail(ctx, "TEXM3X2TEX opcode without matching TEXM3X2PAD");
    // !!! FIXME: check for correct opcode existance and order more rigorously?
    state_texops(ctx, "TEXM3X2TEX", 2, 0);
    ctx->reset_texmpad = 1;

    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER,
                                      ctx->dest_arg.regnum);
    const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);

    // A samplermap might change this to something nonsensical.
    if (ttype != TEXTURE_TYPE_2D)
        fail(ctx, "TEXM3X2TEX needs a 2D sampler");
} // state_TEXM3X2TEX

static void state_TEXM3X3PAD(Context *ctx)
{
    if (shader_version_atleast(ctx, 1, 4))
        fail(ctx, "TEXM3X2TEX opcode not available after Shader Model 1.3");
    state_texops(ctx, "TEXM3X3PAD", 0, 0);

    // !!! FIXME: check for correct opcode existance and order more rigorously?
    if (ctx->texm3x3pad_dst0 == -1)
    {
        ctx->texm3x3pad_src0 = ctx->source_args[0].regnum;
        ctx->texm3x3pad_dst0 = ctx->dest_arg.regnum;
    } // if
    else if (ctx->texm3x3pad_dst1 == -1)
    {
        ctx->texm3x3pad_src1 = ctx->source_args[0].regnum;
        ctx->texm3x3pad_dst1 = ctx->dest_arg.regnum;
    } // else
} // state_TEXM3X3PAD

static void state_texm3x3(Context *ctx, const char *opcode, const int dims)
{
    // !!! FIXME: check for correct opcode existance and order more rigorously?
    if (shader_version_atleast(ctx, 1, 4))
        failf(ctx, "%s opcode not available after Shader Model 1.3", opcode);
    if (ctx->texm3x3pad_dst1 == -1)
        failf(ctx, "%s opcode without matching TEXM3X3PADs", opcode);
    state_texops(ctx, opcode, dims, 0);
    ctx->reset_texmpad = 1;

    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER,
                                      ctx->dest_arg.regnum);
    const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);

    // A samplermap might change this to something nonsensical.
    if ((ttype != TEXTURE_TYPE_VOLUME) && (ttype != TEXTURE_TYPE_CUBE))
        failf(ctx, "%s needs a 3D or Cubemap sampler", opcode);
} // state_texm3x3

static void state_TEXM3X3(Context *ctx)
{
    if (!shader_version_atleast(ctx, 1, 2))
        fail(ctx, "TEXM3X3 opcode not available in Shader Model 1.1");
    state_texm3x3(ctx, "TEXM3X3", 0);
} // state_TEXM3X3

static void state_TEXM3X3TEX(Context *ctx)
{
    state_texm3x3(ctx, "TEXM3X3TEX", 3);
} // state_TEXM3X3TEX

static void state_TEXM3X3SPEC(Context *ctx)
{
    state_texm3x3(ctx, "TEXM3X3SPEC", 3);
    if (ctx->source_args[1].regtype != REG_TYPE_CONST)
        fail(ctx, "TEXM3X3SPEC final arg must be a constant register");
} // state_TEXM3X3SPEC

static void state_TEXM3X3VSPEC(Context *ctx)
{
    state_texm3x3(ctx, "TEXM3X3VSPEC", 3);
} // state_TEXM3X3VSPEC


static void state_TEXLD(Context *ctx)
{
    if (shader_version_atleast(ctx, 2, 0))
    {
        const SourceArgInfo *src0 = &ctx->source_args[0];
        const SourceArgInfo *src1 = &ctx->source_args[1];

        // !!! FIXME: verify texldp restrictions:
        //http://msdn.microsoft.com/en-us/library/bb206221(VS.85).aspx
        // !!! FIXME: ...and texldb, too.
        //http://msdn.microsoft.com/en-us/library/bb206217(VS.85).aspx

        //const RegisterType rt0 = src0->regtype;

        // !!! FIXME: msdn says it has to be temp, but Microsoft's HLSL
        // !!! FIXME:  compiler is generating code that uses oC0 for a dest.
        //if (ctx->dest_arg.regtype != REG_TYPE_TEMP)
        //    fail(ctx, "TEXLD dest must be a temp register");

        // !!! FIXME: this can be an REG_TYPE_INPUT, DCL'd to TEXCOORD.
        //else if ((rt0 != REG_TYPE_TEXTURE) && (rt0 != REG_TYPE_TEMP))
        //    fail(ctx, "TEXLD src0 must be texture or temp register");
        //else

        if (src0->src_mod != SRCMOD_NONE)
            fail(ctx, "TEXLD src0 must have no modifiers");
        else if (src1->regtype != REG_TYPE_SAMPLER)
            fail(ctx, "TEXLD src1 must be sampler register");
        else if (src1->src_mod != SRCMOD_NONE)
            fail(ctx, "TEXLD src1 must have no modifiers");
        else if ( (ctx->instruction_controls != CONTROL_TEXLD) &&
                  (ctx->instruction_controls != CONTROL_TEXLDP) &&
                  (ctx->instruction_controls != CONTROL_TEXLDB) )
        {
            fail(ctx, "TEXLD has unknown control bits");
        } // else if

        // Shader Model 3 added swizzle support to this opcode.
        if (!shader_version_atleast(ctx, 3, 0))
        {
            if (!no_swizzle(src0->swizzle))
                fail(ctx, "TEXLD src0 must not swizzle");
            else if (!no_swizzle(src1->swizzle))
                fail(ctx, "TEXLD src1 must not swizzle");
        } // if

        if ( ((TextureType) ctx->source_args[1].regnum) == TEXTURE_TYPE_CUBE )
            ctx->instruction_count += 3;
    } // if

    else if (shader_version_atleast(ctx, 1, 4))
    {
        // !!! FIXME: checks for ps_1_4 version here...
    } // else if

    else
    {
        // !!! FIXME: add (other?) checks for ps_1_1 version here...
        const DestArgInfo *info = &ctx->dest_arg;
        const int sampler = info->regnum;
        if (info->regtype != REG_TYPE_TEXTURE)
            fail(ctx, "TEX param must be a texture register");
        add_sampler(ctx, sampler, TEXTURE_TYPE_2D, 0);
    } // else
} // state_TEXLD

static void state_TEXLDL(Context *ctx)
{
    if (!shader_version_atleast(ctx, 3, 0))
        fail(ctx, "TEXLDL in version < Shader Model 3.0");
    else if (ctx->source_args[1].regtype != REG_TYPE_SAMPLER)
        fail(ctx, "TEXLDL src1 must be sampler register");
    else
    {
        if ( ((TextureType) ctx->source_args[1].regnum) == TEXTURE_TYPE_CUBE )
            ctx->instruction_count += 3;
    } // else
} // state_TEXLDL

static void state_DP2ADD(Context *ctx)
{
    if (!replicate_swizzle(ctx->source_args[2].swizzle))
        fail(ctx, "DP2ADD src2 must have replicate swizzle");
} // state_DP2ADD


// Lookup table for instruction opcodes...
typedef struct
{
    const char *opcode_string;
    int slots;  // number of instruction slots this opcode eats.
    MOJOSHADER_shaderType shader_types;  // mask of types that can use opcode.
    args_function parse_args;
    state_function state;
    emit_function emitter[STATICARRAYLEN(profiles)];
} Instruction;

// These have to be in the right order! This array is indexed by the value
//  of the instruction token.
static const Instruction instructions[] =
{
    #define INSTRUCTION_STATE(op, opstr, slots, a, t, w) { \
        opstr, slots, t, parse_args_##a, state_##op, PROFILE_EMITTERS(op) \
    },

    #define INSTRUCTION(op, opstr, slots, a, t, w) { \
        opstr, slots, t, parse_args_##a, NULL, PROFILE_EMITTERS(op) \
    },

    #define MOJOSHADER_DO_INSTRUCTION_TABLE 1
    #include "mojoshader_internal.h"
};


// parse various token types...

static int parse_instruction_token(Context *ctx)
{
    int retval = 0;
    const int start_position = ctx->current_position;
    const uint32 *start_tokens = ctx->tokens;
    const uint32 start_tokencount = ctx->tokencount;
    const uint32 token = SWAP32(*(ctx->tokens));
    const uint32 opcode = (token & 0xFFFF);
    const uint32 controls = ((token >> 16) & 0xFF);
    const uint32 insttoks = ((token >> 24) & 0x0F);
    const int coissue = (token & 0x40000000) ? 1 : 0;
    const int predicated = (token & 0x10000000) ? 1 : 0;

    if ( opcode >= (sizeof (instructions) / sizeof (instructions[0])) )
        return 0;  // not an instruction token, or just not handled here.

    const Instruction *instruction = &instructions[opcode];
    const emit_function emitter = instruction->emitter[ctx->profileid];

    if ((token & 0x80000000) != 0)
        fail(ctx, "instruction token high bit must be zero.");  // so says msdn.

    if (instruction->opcode_string == NULL)
    {
        fail(ctx, "Unknown opcode.");
        return insttoks + 1;  // pray that you resync later.
    } // if

    ctx->coissue = coissue;
    if (coissue)
    {
        if (!shader_is_pixel(ctx))
            fail(ctx, "coissue instruction on non-pixel shader");
        if (shader_version_atleast(ctx, 2, 0))
            fail(ctx, "coissue instruction in Shader Model >= 2.0");
    } // if

    if ((ctx->shader_type & instruction->shader_types) == 0)
    {
        failf(ctx, "opcode '%s' not available in this shader type.",
                instruction->opcode_string);
    } // if

    memset(ctx->dwords, '\0', sizeof (ctx->dwords));
    ctx->instruction_controls = controls;
    ctx->predicated = predicated;

    // Update the context with instruction's arguments.
    adjust_token_position(ctx, 1);
    retval = instruction->parse_args(ctx);

    if (predicated)
        retval += parse_predicated_token(ctx);

    // parse_args() moves these forward for convenience...reset them.
    ctx->tokens = start_tokens;
    ctx->tokencount = start_tokencount;
    ctx->current_position = start_position;

    if (instruction->state != NULL)
        instruction->state(ctx);

    ctx->instruction_count += instruction->slots;

    if (!isfail(ctx))
        emitter(ctx);  // call the profile's emitter.

    if (ctx->reset_texmpad)
    {
        ctx->texm3x2pad_dst0 = -1;
        ctx->texm3x2pad_src0 = -1;
        ctx->texm3x3pad_dst0 = -1;
        ctx->texm3x3pad_src0 = -1;
        ctx->texm3x3pad_dst1 = -1;
        ctx->texm3x3pad_src1 = -1;
        ctx->reset_texmpad = 0;
    } // if

    ctx->previous_opcode = opcode;
    ctx->scratch_registers = 0;  // reset after every instruction.

    if (!shader_version_atleast(ctx, 2, 0))
    {
        if (insttoks != 0)  // reserved field in shaders < 2.0 ...
            fail(ctx, "instruction token count must be zero");
    } // if
    else
    {
        if (((uint32)retval) != (insttoks+1))
        {
            failf(ctx, "wrong token count (%u, not %u) for opcode '%s'.",
                    (uint) retval, (uint) (insttoks+1),
                    instruction->opcode_string);
            retval = insttoks + 1;  // try to keep sync.
        } // if
    } // else

    return retval;
} // parse_instruction_token


static int parse_version_token(Context *ctx, const char *profilestr)
{
    if (ctx->tokencount == 0)
    {
        fail(ctx, "Expected version token, got none at all.");
        return 0;
    } // if

    const uint32 token = SWAP32(*(ctx->tokens));
    const uint32 shadertype = ((token >> 16) & 0xFFFF);
    const uint8 major = (uint8) ((token >> 8) & 0xFF);
    const uint8 minor = (uint8) (token & 0xFF);

    ctx->version_token = token;

    // 0xFFFF == pixel shader, 0xFFFE == vertex shader
    if (shadertype == 0xFFFF)
    {
        ctx->shader_type = MOJOSHADER_TYPE_PIXEL;
        ctx->shader_type_str = "ps";
    } // if
    else if (shadertype == 0xFFFE)
    {
        ctx->shader_type = MOJOSHADER_TYPE_VERTEX;
        ctx->shader_type_str = "vs";
    } // else if
    else  // geometry shader? Bogus data?
    {
        fail(ctx, "Unsupported shader type or not a shader at all");
        return -1;
    } // else

    ctx->major_ver = major;
    ctx->minor_ver = minor;

    if (!shader_version_supported(major, minor))
    {
        failf(ctx, "Shader Model %u.%u is currently unsupported.",
                (uint) major, (uint) minor);
    } // if

    if (!isfail(ctx))
        ctx->profile->start_emitter(ctx, profilestr);

    return 1;  // ate one token.
} // parse_version_token


static int parse_ctab_string(const uint8 *start, const uint32 bytes,
                             const uint32 name)
{
    // Make sure strings don't overflow the CTAB buffer...
    if (name < bytes)
    {
        int i;
        const int slenmax = bytes - name;
        const char *namestr = (const char *) (start + name);
        for (i = 0; i < slenmax; i++)
        {
            if (namestr[i] == '\0')
                return 1;  // it's okay.
        } // for
    } // if

    return 0;  // overflowed.
} // parse_ctab_string


static int parse_ctab_typeinfo(Context *ctx, const uint8 *start,
                               const uint32 bytes, const uint32 pos,
                               MOJOSHADER_symbolTypeInfo *info,
                               const int depth)
{
    if ((bytes <= pos) || ((bytes - pos) < 16))
        return 0;  // corrupt CTAB.

    const uint16 *typeptr = (const uint16 *) (start + pos);

    info->parameter_class = (MOJOSHADER_symbolClass) SWAP16(typeptr[0]);
    info->parameter_type = (MOJOSHADER_symbolType) SWAP16(typeptr[1]);
    info->rows = (unsigned int) SWAP16(typeptr[2]);
    info->columns = (unsigned int) SWAP16(typeptr[3]);
    info->elements = (unsigned int) SWAP16(typeptr[4]);

    if (info->parameter_class >= MOJOSHADER_SYMCLASS_TOTAL)
    {
        failf(ctx, "Unknown parameter class (0x%X)", info->parameter_class);
        info->parameter_class = MOJOSHADER_SYMCLASS_SCALAR;
    } // if

    if (info->parameter_type >= MOJOSHADER_SYMTYPE_TOTAL)
    {
        failf(ctx, "Unknown parameter type (0x%X)", info->parameter_type);
        info->parameter_type = MOJOSHADER_SYMTYPE_INT;
    } // if

    const unsigned int member_count = (unsigned int) SWAP16(typeptr[5]);
    info->member_count = 0;
    info->members = NULL;

    if ((pos + 16 + (member_count * 8)) >= bytes)
        return 0;  // corrupt CTAB.

    if (member_count > 0)
    {
        if (depth > 300)  // make sure we aren't in an infinite loop here.
        {
            fail(ctx, "Possible infinite loop in CTAB structure.");
            return 0;
        } // if

        const size_t len = sizeof (MOJOSHADER_symbolStructMember) * member_count;
        info->members = (MOJOSHADER_symbolStructMember *) Malloc(ctx, len);
        if (info->members == NULL)
            return 1;  // we'll check ctx->out_of_memory later.
        memset(info->members, '\0', len);
        info->member_count = member_count;
    } // else

    unsigned int i;
    const uint32 *member = (const uint32 *) (start + typeptr[6]);
    for (i = 0; i < member_count; i++)
    {
        MOJOSHADER_symbolStructMember *mbr = &info->members[i];
        const uint32 name = SWAP32(member[0]);
        const uint32 memberinfopos = SWAP32(member[1]);
        member += 2;

        if (!parse_ctab_string(start, bytes, name))
            return 0;  // info->members will be free()'d elsewhere.

        mbr->name = StrDup(ctx, (const char *) (start + name));
        if (mbr->name == NULL)
            return 1;  // we'll check ctx->out_of_memory later.
        if (!parse_ctab_typeinfo(ctx, start, bytes, memberinfopos, &mbr->info, depth + 1))
            return 0;
        if (ctx->out_of_memory)
            return 1;  // drop out now.
    } // for

    return 1;
} // parse_ctab_typeinfo


// Microsoft's tools add a CTAB comment to all shaders. This is the
//  "constant table," or specifically: D3DXSHADER_CONSTANTTABLE:
//  http://msdn.microsoft.com/en-us/library/bb205440(VS.85).aspx
// This may tell us high-level truths about an otherwise generic low-level
//  registers, for instance, how large an array actually is, etc.
static void parse_constant_table(Context *ctx, const uint32 *tokens,
                                 const uint32 bytes, const uint32 okay_version,
                                 const int setvariables, CtabData *ctab)
{
    const uint32 id = SWAP32(tokens[1]);
    if (id != CTAB_ID)
        return;  // not the constant table.

    if (ctab->have_ctab)  // !!! FIXME: can you have more than one?
    {
        fail(ctx, "Shader has multiple CTAB sections");
        return;
    } // if

    ctab->have_ctab = 1;

    const uint8 *start = (uint8 *) &tokens[2];

    if (bytes < 32)
    {
        fail(ctx, "Truncated CTAB data");
        return;
    } // if

    const uint32 size = SWAP32(tokens[2]);
    const uint32 creator = SWAP32(tokens[3]);
    const uint32 version = SWAP32(tokens[4]);
    const uint32 constants = SWAP32(tokens[5]);
    const uint32 constantinfo = SWAP32(tokens[6]);
    const uint32 target = SWAP32(tokens[8]);

    if (size != CTAB_SIZE)
        goto corrupt_ctab;
    else if (constants > 1000000)  // sanity check.
        goto corrupt_ctab;

    if (version != okay_version) goto corrupt_ctab;
    if (creator >= bytes) goto corrupt_ctab;
    if (constantinfo >= bytes) goto corrupt_ctab;
    if ((bytes - constantinfo) < (constants * CINFO_SIZE)) goto corrupt_ctab;
    if (target >= bytes) goto corrupt_ctab;
    if (!parse_ctab_string(start, bytes, target)) goto corrupt_ctab;
    // !!! FIXME: check that (start+target) points to "ps_3_0", etc.

    ctab->symbols = NULL;
    if (constants > 0)
    {
        ctab->symbols = (MOJOSHADER_symbol *) Malloc(ctx, sizeof (MOJOSHADER_symbol) * constants);
        if (ctab->symbols == NULL)
            return;
        memset(ctab->symbols, '\0', sizeof (MOJOSHADER_symbol) * constants);
    } // if
    ctab->symbol_count = constants;

    uint32 i = 0;
    for (i = 0; i < constants; i++)
    {
        const uint8 *ptr = start + constantinfo + (i * CINFO_SIZE);
        const uint32 name = SWAP32(*((uint32 *) (ptr + 0)));
        const uint16 regset = SWAP16(*((uint16 *) (ptr + 4)));
        const uint16 regidx = SWAP16(*((uint16 *) (ptr + 6)));
        const uint16 regcnt = SWAP16(*((uint16 *) (ptr + 8)));
        const uint32 typeinf = SWAP32(*((uint32 *) (ptr + 12)));
        const uint32 defval = SWAP32(*((uint32 *) (ptr + 16)));
        MOJOSHADER_uniformType mojotype = MOJOSHADER_UNIFORM_UNKNOWN;

        if (!parse_ctab_string(start, bytes, name)) goto corrupt_ctab;
        if (defval >= bytes) goto corrupt_ctab;

        switch (regset)
        {
            case 0: mojotype = MOJOSHADER_UNIFORM_BOOL; break;
            case 1: mojotype = MOJOSHADER_UNIFORM_INT; break;
            case 2: mojotype = MOJOSHADER_UNIFORM_FLOAT; break;
            case 3: /* SAMPLER */ break;
            default: goto corrupt_ctab;
        } // switch

        if ((setvariables) && (mojotype != MOJOSHADER_UNIFORM_UNKNOWN))
        {
            VariableList *item;
            item = (VariableList *) Malloc(ctx, sizeof (VariableList));
            if (item != NULL)
            {
                item->type = mojotype;
                item->index = regidx;
                item->count = regcnt;
                item->constant = NULL;
                item->used = 0;
                item->emit_position = -1;
                item->next = ctx->variables;
                ctx->variables = item;
            } // if
        } // if

        // Add the symbol.
        const char *namecpy = StrDup(ctx, (const char *) (start + name));
        if (namecpy == NULL)
            return;

        MOJOSHADER_symbol *sym = &ctab->symbols[i];
        sym->name = namecpy;
        sym->register_set = (MOJOSHADER_symbolRegisterSet) regset;
        sym->register_index = (unsigned int) regidx;
        sym->register_count = (unsigned int) regcnt;
        if (!parse_ctab_typeinfo(ctx, start, bytes, typeinf, &sym->info, 0))
            goto corrupt_ctab;  // sym->name will get free()'d later.
        else if (ctx->out_of_memory)
            return;  // just bail now.
    } // for

    return;

corrupt_ctab:
    fail(ctx, "Shader has corrupt CTAB data");
} // parse_constant_table


static void free_symbols(MOJOSHADER_free f, void *d, MOJOSHADER_symbol *syms,
                         const int symcount);


static int is_comment_token(Context *ctx, const uint32 tok, uint32 *tokcount)
{
    const uint32 token = SWAP32(tok);
    if ((token & 0xFFFF) == 0xFFFE)  // actually a comment token?
    {
        if ((token & 0x80000000) != 0)
            fail(ctx, "comment token high bit must be zero.");  // so says msdn.
        *tokcount = ((token >> 16) & 0xFFFF);
        return 1;
    } // if

    return 0;
} // is_comment_token


typedef struct PreshaderBlockInfo
{
    const uint32 *tokens;
    uint32 tokcount;
    int seen;
} PreshaderBlockInfo;

// Preshaders only show up in compiled Effect files. The format is
//  undocumented, and even the instructions aren't the same opcodes as you
//  would find in a regular shader. These things show up because the HLSL
//  compiler can detect work that sets up constant registers that could
//  be moved out of the shader itself. Preshaders run once, then the shader
//  itself runs many times, using the constant registers the preshader has set
//  up. There are cases where the preshaders are 3+ times as many instructions
//  as the shader itself, so this can be a big performance win.
// My presumption is that Microsoft's Effects framework runs the preshaders on
//  the CPU, then loads the constant register file appropriately before handing
//  off to the GPU. As such, we do the same.
static void parse_preshader(Context *ctx, const uint32 *tokens, uint32 tokcount)
{
#ifndef MOJOSHADER_EFFECT_SUPPORT
    fail(ctx, "Preshader found, but effect support is disabled!");
#else
    uint32 i;

    assert(ctx->have_preshader == 0);  // !!! FIXME: can you have more than one?
    ctx->have_preshader = 1;

    // !!! FIXME: I don't know what specific versions signify, but we need to
    // !!! FIXME:  save this to test against the CTAB version field, if
    // !!! FIXME:  nothing else.
    // !!! FIXME: 0x02 0x0? is probably the version (fx_2_?),
    // !!! FIXME:  and 0x4658 is the magic, like a real shader's version token.
    const uint32 version_magic = 0x46580000;
    const uint32 min_version = 0x00000200 | version_magic;
    const uint32 max_version = 0x00000201 | version_magic;
    const uint32 version = SWAP32(tokens[0]);
    if (version < min_version || version > max_version)
    {
        fail(ctx, "Unsupported preshader version.");
        return;  // fail because the shader will malfunction w/o this.
    } // if

    tokens++;
    tokcount--;

    // All sections of a preshader are packed into separate comment tokens,
    //  inside the containing comment token block. Find them all before
    //  we start, so we don't care about the order they appear in the file.
    PreshaderBlockInfo ctab = { 0, 0, 0 };
    PreshaderBlockInfo prsi = { 0, 0, 0 };
    PreshaderBlockInfo fxlc = { 0, 0, 0 };
    PreshaderBlockInfo clit = { 0, 0, 0 };

    while (tokcount > 0)
    {
        uint32 subtokcount = 0;
        if ( (!is_comment_token(ctx, *tokens, &subtokcount)) ||
             (subtokcount > tokcount) )
        {
            // !!! FIXME: Standalone preshaders have this EOS-looking token,
            // !!! FIXME:  sometimes followed by tokens that don't appear to
            // !!! FIXME:  have anything to do with the rest of the blob.
            // !!! FIXME: So for now, treat this as a special "EOS" comment.
            if (SWAP32(*tokens) == 0xFFFF)
                break;

            fail(ctx, "Bogus preshader data.");
            return;
        } // if

        tokens++;
        tokcount--;

        const uint32 *nexttokens = tokens + subtokcount;
        const uint32 nexttokcount = tokcount - subtokcount;

        if (subtokcount > 0)
        {
            switch (SWAP32(*tokens))
            {
                #define PRESHADER_BLOCK_CASE(id, var) \
                    case id##_ID: { \
                        if (var.seen) { \
                            fail(ctx, "Multiple " #id " preshader blocks."); \
                            return; \
                        } \
                        var.tokens = tokens; \
                        var.tokcount = subtokcount; \
                        var.seen = 1; \
                        break; \
                    }
                PRESHADER_BLOCK_CASE(CTAB, ctab);
                PRESHADER_BLOCK_CASE(PRSI, prsi);
                PRESHADER_BLOCK_CASE(FXLC, fxlc);
                PRESHADER_BLOCK_CASE(CLIT, clit);
                default: fail(ctx, "Bogus preshader section."); return;
                #undef PRESHADER_BLOCK_CASE
            } // switch
        } // if

        tokens = nexttokens;
        tokcount = nexttokcount;
    } // while

    if (!ctab.seen) { fail(ctx, "No CTAB block in preshader."); return; }
    if (!fxlc.seen) { fail(ctx, "No FXLC block in preshader."); return; }
    if (!clit.seen) { fail(ctx, "No CLIT block in preshader."); return; }
    // prsi.seen is optional, apparently.

    MOJOSHADER_preshader *preshader = (MOJOSHADER_preshader *)
                                    Malloc(ctx, sizeof (MOJOSHADER_preshader));
    if (preshader == NULL)
        return;

    memset(preshader, '\0', sizeof (MOJOSHADER_preshader));
    preshader->malloc = ctx->malloc;
    preshader->free = ctx->free;
    preshader->malloc_data = ctx->malloc_data;

    ctx->preshader = preshader;

    // Let's set up the constant literals first...
    if (clit.tokcount == 0)
        fail(ctx, "Bogus CLIT block in preshader.");
    else
    {
        const uint32 lit_count = SWAP32(clit.tokens[1]);
        if (lit_count > ((clit.tokcount - 2) / 2))
        {
            fail(ctx, "Bogus CLIT block in preshader.");
            return;
        } // if
        else if (lit_count > 0)
        {
            preshader->literal_count = (unsigned int) lit_count;
            assert(sizeof (double) == 8);  // just in case.
            const size_t len = sizeof (double) * lit_count;
            preshader->literals = (double *) Malloc(ctx, len);
            if (preshader->literals == NULL)
                return;  // oh well.
            const double *litptr = (const double *) (clit.tokens + 2);
            // !!! FIXME: This should be a SWAPDBL loop, but in addition to
            // !!! FIXME:  never having an implementation, it turns out some
            // !!! FIXME:  MinGW versions do not optimize this correctly, which
            // !!! FIXME:  can cause random crashes on Win64. -flibit
            //for (i = 0; i < lit_count; i++)
            //    preshader->literals[i] = SWAPDBL(litptr[i]);
            memcpy(preshader->literals, litptr, len);
        } // else if
    } // else

    // Parse out the PRSI block. This is used to map the output registers.
    uint32 output_map_count = 0;
    const uint32 *output_map = NULL;
    if (prsi.seen)
    {
        if (prsi.tokcount < 8)
        {
            fail(ctx, "Bogus preshader PRSI data");
            return;
        } // if

        //const uint32 first_output_reg = SWAP32(prsi.tokens[1]);
        // !!! FIXME: there are a lot of fields here I don't know about.
        // !!! FIXME:  maybe [2] and [3] are for int4 and bool registers?
        //const uint32 output_reg_count = SWAP32(prsi.tokens[4]);
        // !!! FIXME:  maybe [5] and [6] are for int4 and bool registers?
        output_map_count = SWAP32(prsi.tokens[7]);

        prsi.tokcount -= 8;
        prsi.tokens += 8;

        if (prsi.tokcount < ((output_map_count + 1) * 2))
        {
            fail(ctx, "Bogus preshader PRSI data");
            return;
        } // if

        output_map = prsi.tokens;
    } // if

    // Now we'll figure out the CTAB...
    CtabData ctabdata = { 0, 0, 0 };
    parse_constant_table(ctx, ctab.tokens - 1, ctab.tokcount * 4,
                         version, 0, &ctabdata);

    // preshader owns this now. Don't free it in this function.
    preshader->symbol_count = ctabdata.symbol_count;
    preshader->symbols = ctabdata.symbols;

    if (!ctabdata.have_ctab)
    {
        fail(ctx, "Bogus preshader CTAB data");
        return;
    } // if

    // The FXLC block has the actual instructions...
    uint32 opcode_count = SWAP32(fxlc.tokens[1]);

    const size_t len = sizeof (MOJOSHADER_preshaderInstruction) * opcode_count;
    preshader->instruction_count = (unsigned int) opcode_count;
    preshader->instructions = (MOJOSHADER_preshaderInstruction *) Malloc(ctx, len);
    if (preshader->instructions == NULL)
        return;
    memset(preshader->instructions, '\0', len);

    fxlc.tokens += 2;
    fxlc.tokcount -= 2;
    if (opcode_count > (fxlc.tokcount / 2))
    {
        fail(ctx, "Bogus preshader FXLC block.");
        return;
    } // if

    MOJOSHADER_preshaderInstruction *inst = preshader->instructions;
    while (opcode_count--)
    {
        const uint32 opcodetok = SWAP32(fxlc.tokens[0]);
        MOJOSHADER_preshaderOpcode opcode = MOJOSHADER_PRESHADEROP_NOP;
        switch ((opcodetok >> 16) & 0xFFFF)
        {
            case 0x1000: opcode = MOJOSHADER_PRESHADEROP_MOV; break;
            case 0x1010: opcode = MOJOSHADER_PRESHADEROP_NEG; break;
            case 0x1030: opcode = MOJOSHADER_PRESHADEROP_RCP; break;
            case 0x1040: opcode = MOJOSHADER_PRESHADEROP_FRC; break;
            case 0x1050: opcode = MOJOSHADER_PRESHADEROP_EXP; break;
            case 0x1060: opcode = MOJOSHADER_PRESHADEROP_LOG; break;
            case 0x1070: opcode = MOJOSHADER_PRESHADEROP_RSQ; break;
            case 0x1080: opcode = MOJOSHADER_PRESHADEROP_SIN; break;
            case 0x1090: opcode = MOJOSHADER_PRESHADEROP_COS; break;
            case 0x10A0: opcode = MOJOSHADER_PRESHADEROP_ASIN; break;
            case 0x10B0: opcode = MOJOSHADER_PRESHADEROP_ACOS; break;
            case 0x10C0: opcode = MOJOSHADER_PRESHADEROP_ATAN; break;
            case 0x2000: opcode = MOJOSHADER_PRESHADEROP_MIN; break;
            case 0x2010: opcode = MOJOSHADER_PRESHADEROP_MAX; break;
            case 0x2020: opcode = MOJOSHADER_PRESHADEROP_LT; break;
            case 0x2030: opcode = MOJOSHADER_PRESHADEROP_GE; break;
            case 0x2040: opcode = MOJOSHADER_PRESHADEROP_ADD; break;
            case 0x2050: opcode = MOJOSHADER_PRESHADEROP_MUL; break;
            case 0x2060: opcode = MOJOSHADER_PRESHADEROP_ATAN2; break;
            case 0x2080: opcode = MOJOSHADER_PRESHADEROP_DIV; break;
            case 0x3000: opcode = MOJOSHADER_PRESHADEROP_CMP; break;
            case 0x3010: opcode = MOJOSHADER_PRESHADEROP_MOVC; break;
            case 0x5000: opcode = MOJOSHADER_PRESHADEROP_DOT; break;
            case 0x5020: opcode = MOJOSHADER_PRESHADEROP_NOISE; break;
            case 0xA000: opcode = MOJOSHADER_PRESHADEROP_MIN_SCALAR; break;
            case 0xA010: opcode = MOJOSHADER_PRESHADEROP_MAX_SCALAR; break;
            case 0xA020: opcode = MOJOSHADER_PRESHADEROP_LT_SCALAR; break;
            case 0xA030: opcode = MOJOSHADER_PRESHADEROP_GE_SCALAR; break;
            case 0xA040: opcode = MOJOSHADER_PRESHADEROP_ADD_SCALAR; break;
            case 0xA050: opcode = MOJOSHADER_PRESHADEROP_MUL_SCALAR; break;
            case 0xA060: opcode = MOJOSHADER_PRESHADEROP_ATAN2_SCALAR; break;
            case 0xA080: opcode = MOJOSHADER_PRESHADEROP_DIV_SCALAR; break;
            case 0xD000: opcode = MOJOSHADER_PRESHADEROP_DOT_SCALAR; break;
            case 0xD020: opcode = MOJOSHADER_PRESHADEROP_NOISE_SCALAR; break;
            default: fail(ctx, "Unknown preshader opcode."); break;
        } // switch

        uint32 operand_count = SWAP32(fxlc.tokens[1]) + 1;  // +1 for dest.

        inst->opcode = opcode;
        inst->element_count = (unsigned int) (opcodetok & 0xFF);
        inst->operand_count = (unsigned int) operand_count;

        fxlc.tokens += 2;
        fxlc.tokcount -= 2;
        if ((operand_count * 3) > fxlc.tokcount)
        {
            fail(ctx, "Bogus preshader FXLC block.");
            return;
        } // if

        MOJOSHADER_preshaderOperand *operand = inst->operands;
        while (operand_count--)
        {
            const unsigned int item = (unsigned int) SWAP32(fxlc.tokens[2]);

            // !!! FIXME: Is this used anywhere other than INPUT? -flibit
            const uint32 numarrays = SWAP32(fxlc.tokens[0]);
            switch (SWAP32(fxlc.tokens[1]))
            {
                case 1:  // literal from CLIT block.
                {
                    if (item > preshader->literal_count)
                    {
                        fail(ctx, "Bogus preshader literal index.");
                        break;
                    } // if
                    operand->type = MOJOSHADER_PRESHADEROPERAND_LITERAL;
                    break;
                } // case

                case 2:  // item from ctabdata.
                {
                    MOJOSHADER_symbol *sym = ctabdata.symbols;
                    const uint32 symcount = (uint32) ctabdata.symbol_count;
                    for (i = 0; i < symcount; i++, sym++)
                    {
                        const uint32 base = sym->register_index * 4;
                        const uint32 count = sym->register_count * 4;
                        assert(sym->register_set==MOJOSHADER_SYMREGSET_FLOAT4);
                        if ( (base <= item) && ((base + count) > item) )
                            break;
                    } // for
                    if (i == ctabdata.symbol_count)
                    {
                        fail(ctx, "Bogus preshader input index.");
                        break;
                    } // if
                    operand->type = MOJOSHADER_PRESHADEROPERAND_INPUT;
                    if (numarrays > 0)
                    {
                        // malloc the array symbol name array
                        const uint32 siz = numarrays * sizeof (uint32);
                        operand->array_register_count = numarrays;
                        operand->array_registers = (uint32 *) Malloc(ctx, siz);
                        memset(operand->array_registers, '\0', siz);
                        // Get each register base, indicating the arrays used.
                        // !!! FIXME: fail if fxlc.tokcount*2 > numarrays ?
                        for (i = 0; i < numarrays; i++)
                        {
                            const uint32 jmp = SWAP32(fxlc.tokens[4]);
                            const uint32 bigjmp = (jmp >> 4) * 4;
                            const uint32 ltljmp = (jmp >> 2) & 3;
                            operand->array_registers[i] = bigjmp + ltljmp;
                            fxlc.tokens += 2;
                            fxlc.tokcount -= 2;
                        } // for
                    } // if
                    break;
                } // case

                case 4:
                {
                    operand->type = MOJOSHADER_PRESHADEROPERAND_OUTPUT;

                    for (i = 0; i < output_map_count; i++)
                    {
                        const uint32 base = output_map[(i*2)] * 4;
                        const uint32 count = output_map[(i*2)+1] * 4;
                        if ( (base <= item) && ((base + count) > item) )
                            break;
                    } // for

                    if (i == output_map_count)
                    {
                        if (prsi.seen)  // No PRSI tokens, no output map.
                            fail(ctx, "Bogus preshader output index.");
                    } // if

                    break;
                } // case

                case 7:
                {
                    operand->type = MOJOSHADER_PRESHADEROPERAND_TEMP;
                    if (item >= preshader->temp_count)
                        preshader->temp_count = item + 1;
                    break;
                } // case

                default:
                    assert(0 && "Unhandled fxlc.tokens[1] in parse_preshader!");
                    break;
            } // switch

            operand->index = item;

            fxlc.tokens += 3;
            fxlc.tokcount -= 3;
            operand++;
        } // while

        inst++;
    } // while

    // Registers need to be vec4, round up to nearest 4
    preshader->temp_count = (preshader->temp_count + 3) & ~3;

    unsigned int largest = 0;
    const MOJOSHADER_symbol *sym = preshader->symbols;
    const uint32 symcount = (uint32) preshader->symbol_count;
    for (i = 0; i < symcount; i++, sym++)
    {
        const unsigned int val = sym->register_index + sym->register_count;
        if (val > largest)
            largest = val;
    } // for

    if (largest > 0)
    {
        const size_t len = largest * sizeof (float) * 4;
        preshader->registers = (float *) Malloc(ctx, len);
        memset(preshader->registers, '\0', len);
        preshader->register_count = largest;
    } // if
#endif
} // parse_preshader

static int parse_comment_token(Context *ctx)
{
    uint32 commenttoks = 0;
    if (is_comment_token(ctx, *ctx->tokens, &commenttoks))
    {
        if ((commenttoks >= 2) && (commenttoks < ctx->tokencount))
        {
            const uint32 id = SWAP32(ctx->tokens[1]);
            if (id == PRES_ID)
                parse_preshader(ctx, ctx->tokens + 2, commenttoks - 2);
            else if (id == CTAB_ID)
            {
                parse_constant_table(ctx, ctx->tokens, commenttoks * 4,
                                     ctx->version_token, 1, &ctx->ctab);
            } // else if
        } // if
        return commenttoks + 1;  // comment data plus the initial token.
    } // if

    return 0;  // not a comment token.
} // parse_comment_token


static int parse_end_token(Context *ctx)
{
    if (SWAP32(*(ctx->tokens)) != 0x0000FFFF)   // end token always 0x0000FFFF.
        return 0;  // not us, eat no tokens.

    if (!ctx->know_shader_size)  // this is the end of stream!
        ctx->tokencount = 1;
    else if (ctx->tokencount != 1)  // we _must_ be last. If not: fail.
        fail(ctx, "end token before end of stream");

    if (!isfail(ctx))
        ctx->profile->end_emitter(ctx);

    return 1;
} // parse_end_token


static int parse_phase_token(Context *ctx)
{
    // !!! FIXME: needs state; allow only one phase token per shader, I think?
    if (SWAP32(*(ctx->tokens)) != 0x0000FFFD) // phase token always 0x0000FFFD.
        return 0;  // not us, eat no tokens.

    if ( (!shader_is_pixel(ctx)) || (!shader_version_exactly(ctx, 1, 4)) )
        fail(ctx, "phase token only available in 1.4 pixel shaders");

    if (!isfail(ctx))
        ctx->profile->phase_emitter(ctx);

    return 1;
} // parse_phase_token


static int parse_token(Context *ctx)
{
    int rc = 0;

    assert(ctx->output_stack_len == 0);

    if (ctx->tokencount == 0)
        fail(ctx, "unexpected end of shader.");

    else if ((rc = parse_comment_token(ctx)) != 0)
        return rc;

    else if ((rc = parse_end_token(ctx)) != 0)
        return rc;

    else if ((rc = parse_phase_token(ctx)) != 0)
        return rc;

    else if ((rc = parse_instruction_token(ctx)) != 0)
        return rc;

    failf(ctx, "unknown token (0x%x)", (uint) *ctx->tokens);
    return 1;  // good luck!
} // parse_token


static int find_profile_id(const char *profile)
{
    size_t i;
    for (i = 0; i < STATICARRAYLEN(profileMap); i++)
    {
        const char *name = profileMap[i].from;
        if (strcmp(name, profile) == 0)
        {
            profile = profileMap[i].to;
            break;
        } // if
    } // for

    for (i = 0; i < STATICARRAYLEN(profiles); i++)
    {
        const char *name = profiles[i].name;
        if (strcmp(name, profile) == 0)
            return i;
    } // for

    return -1;  // no match.
} // find_profile_id


static Context *build_context(const char *profile,
                              const char *mainfn,
                              const unsigned char *tokenbuf,
                              const unsigned int bufsize,
                              const MOJOSHADER_swizzle *swiz,
                              const unsigned int swizcount,
                              const MOJOSHADER_samplerMap *smap,
                              const unsigned int smapcount,
                              MOJOSHADER_malloc m, MOJOSHADER_free f, void *d)
{
    if (m == NULL) m = MOJOSHADER_internal_malloc;
    if (f == NULL) f = MOJOSHADER_internal_free;

    Context *ctx = (Context *) m(sizeof (Context), d);
    if (ctx == NULL)
        return NULL;

    memset(ctx, '\0', sizeof (Context));
    ctx->malloc = m;
    ctx->free = f;
    ctx->malloc_data = d;
    ctx->tokens = (const uint32 *) tokenbuf;
    ctx->orig_tokens = (const uint32 *) tokenbuf;
    ctx->know_shader_size = (bufsize != 0);
    ctx->tokencount = ctx->know_shader_size ? (bufsize / sizeof (uint32)) : 0xFFFFFFFF;
    ctx->swizzles = swiz;
    ctx->swizzles_count = swizcount;
    ctx->samplermap = smap;
    ctx->samplermap_count = smapcount;
    ctx->endline = ENDLINE_STR;
    ctx->endline_len = strlen(ctx->endline);
    ctx->last_address_reg_component = -1;
    ctx->current_position = MOJOSHADER_POSITION_BEFORE;
    ctx->texm3x2pad_dst0 = -1;
    ctx->texm3x2pad_src0 = -1;
    ctx->texm3x3pad_dst0 = -1;
    ctx->texm3x3pad_src0 = -1;
    ctx->texm3x3pad_dst1 = -1;
    ctx->texm3x3pad_src1 = -1;

    ctx->errors = errorlist_create(MallocBridge, FreeBridge, ctx);
    if (ctx->errors == NULL)
    {
        f(ctx, d);
        return NULL;
    } // if

    if (!set_output(ctx, &ctx->mainline))
    {
        errorlist_destroy(ctx->errors);
        f(ctx, d);
        return NULL;
    } // if

    if (mainfn != NULL)
    {
        if (strlen(mainfn) > 55)  // !!! FIXME: just to keep things sane. Lots of hardcoded stack arrays...
            failf(ctx, "Main function name '%s' is too big", mainfn);
        else
            ctx->mainfn = StrDup(ctx, mainfn);
    } // if

    if (profile != NULL)
    {
        const int profileid = find_profile_id(profile);
        ctx->profileid = profileid;
        if (profileid >= 0)
            ctx->profile = &profiles[profileid];
        else
            failf(ctx, "Profile '%s' is unknown or unsupported", profile);
    } // if

    return ctx;
} // build_context


static void free_constants_list(MOJOSHADER_free f, void *d, ConstantsList *item)
{
    while (item != NULL)
    {
        ConstantsList *next = item->next;
        f(item, d);
        item = next;
    } // while
} // free_constants_list


static void free_variable_list(MOJOSHADER_free f, void *d, VariableList *item)
{
    while (item != NULL)
    {
        VariableList *next = item->next;
        f(item, d);
        item = next;
    } // while
} // free_variable_list


static void free_sym_typeinfo(MOJOSHADER_free f, void *d,
                              MOJOSHADER_symbolTypeInfo *typeinfo)
{
    unsigned int i;
    for (i = 0; i < typeinfo->member_count; i++)
    {
        f((void *) typeinfo->members[i].name, d);
        free_sym_typeinfo(f, d, &typeinfo->members[i].info);
    } // for
    f((void *) typeinfo->members, d);
} // free_sym_members


static void free_symbols(MOJOSHADER_free f, void *d, MOJOSHADER_symbol *syms,
                         const int symcount)
{
    int i;
    for (i = 0; i < symcount; i++)
    {
        f((void *) syms[i].name, d);
        free_sym_typeinfo(f, d, &syms[i].info);
    } // for
    f((void *) syms, d);
} // free_symbols


static void destroy_context(Context *ctx)
{
    if (ctx != NULL)
    {
        MOJOSHADER_free f = ((ctx->free != NULL) ? ctx->free : MOJOSHADER_internal_free);
        void *d = ctx->malloc_data;
        buffer_destroy(ctx->preflight);
        buffer_destroy(ctx->globals);
        buffer_destroy(ctx->inputs);
        buffer_destroy(ctx->outputs);
        buffer_destroy(ctx->helpers);
        buffer_destroy(ctx->subroutines);
        buffer_destroy(ctx->mainline_intro);
        buffer_destroy(ctx->mainline_arguments);
        buffer_destroy(ctx->mainline_top);
        buffer_destroy(ctx->mainline);
        buffer_destroy(ctx->postflight);
        buffer_destroy(ctx->ignore);
        free_constants_list(f, d, ctx->constants);
        free_reglist(f, d, ctx->used_registers.next);
        free_reglist(f, d, ctx->defined_registers.next);
        free_reglist(f, d, ctx->uniforms.next);
        free_reglist(f, d, ctx->attributes.next);
        free_reglist(f, d, ctx->samplers.next);
        free_variable_list(f, d, ctx->variables);
        errorlist_destroy(ctx->errors);
        free_symbols(f, d, ctx->ctab.symbols, ctx->ctab.symbol_count);
        MOJOSHADER_freePreshader(ctx->preshader);
        f((void *) ctx->mainfn, d);
        f(ctx, d);
    } // if
} // destroy_context


static char *build_output(Context *ctx, size_t *len)
{
    // add a byte for a null terminator.
    Buffer *buffers[] = {
        ctx->preflight, ctx->globals, ctx->inputs, ctx->outputs, ctx->helpers,
        ctx->subroutines, ctx->mainline_intro, ctx->mainline_arguments,
        ctx->mainline_top, ctx->mainline, ctx->postflight
        // don't append ctx->ignore ... that's why it's called "ignore"
    };
    char *retval = buffer_merge(buffers, STATICARRAYLEN(buffers), len);
    return retval;
} // build_output


static inline const char *alloc_varname(Context *ctx, const RegisterList *reg)
{
    return ctx->profile->get_varname(ctx, reg->regtype, reg->regnum);
} // alloc_varname


// !!! FIXME: this code is sort of hard to follow:
// !!! FIXME:  "var->used" only applies to arrays (at the moment, at least,
// !!! FIXME:  but this might be buggy at a later time?), and this code
// !!! FIXME:  relies on that.
// !!! FIXME: "variables" means "things we found in a CTAB" but it's not
// !!! FIXME:  all registers, etc.
// !!! FIXME: "const_array" means an array for d3d "const" registers (c0, c1,
// !!! FIXME:  etc), but not a constant array, although they _can_ be.
// !!! FIXME: It's just a mess.  :/
static MOJOSHADER_uniform *build_uniforms(Context *ctx)
{
    const size_t len = sizeof (MOJOSHADER_uniform) * ctx->uniform_count;
    MOJOSHADER_uniform *retval = (MOJOSHADER_uniform *) Malloc(ctx, len);

    if (retval != NULL)
    {
        MOJOSHADER_uniform *wptr = retval;
        memset(wptr, '\0', len);

        VariableList *var;
        int written = 0;
        for (var = ctx->variables; var != NULL; var = var->next)
        {
            if (var->used)
            {
                const char *name = ctx->profile->get_const_array_varname(ctx,
                                                      var->index, var->count);
                if (name != NULL)
                {
                    wptr->type = MOJOSHADER_UNIFORM_FLOAT;
                    wptr->index = var->index;
                    wptr->array_count = var->count;
                    wptr->constant = (var->constant != NULL) ? 1 : 0;
                    wptr->name = name;
                    wptr++;
                    written++;
                } // if
            } // if
        } // for

        RegisterList *item = ctx->uniforms.next;
        MOJOSHADER_uniformType type = MOJOSHADER_UNIFORM_FLOAT;
        while (written < ctx->uniform_count)
        {
            int skip = 0;

            // !!! FIXME: does this fail if written > ctx->uniform_count?
            if (item == NULL)
            {
                fail(ctx, "BUG: mismatched uniform list and count");
                break;
            } // if

            int index = item->regnum;
            switch (item->regtype)
            {
                case REG_TYPE_CONST:
                    skip = (item->array != NULL);
                    type = MOJOSHADER_UNIFORM_FLOAT;
                    break;

                case REG_TYPE_CONSTINT:
                    type = MOJOSHADER_UNIFORM_INT;
                    break;

                case REG_TYPE_CONSTBOOL:
                    type = MOJOSHADER_UNIFORM_BOOL;
                    break;

                default:
                    fail(ctx, "unknown uniform datatype");
                    break;
            } // switch

            if (!skip)
            {
                wptr->type = type;
                wptr->index = index;
                wptr->array_count = 0;
                wptr->name = alloc_varname(ctx, item);
                wptr++;
                written++;
            } // if

            item = item->next;
        } // for
    } // if

    return retval;
} // build_uniforms


static MOJOSHADER_constant *build_constants(Context *ctx)
{
    const size_t len = sizeof (MOJOSHADER_constant) * ctx->constant_count;
    MOJOSHADER_constant *retval = (MOJOSHADER_constant *) Malloc(ctx, len);

    if (retval != NULL)
    {
        ConstantsList *item = ctx->constants;
        int i;

        for (i = 0; i < ctx->constant_count; i++)
        {
            if (item == NULL)
            {
                fail(ctx, "BUG: mismatched constant list and count");
                break;
            } // if

            memcpy(&retval[i], &item->constant, sizeof (MOJOSHADER_constant));
            item = item->next;
        } // for
    } // if

    return retval;
} // build_constants


static MOJOSHADER_sampler *build_samplers(Context *ctx)
{
    const size_t len = sizeof (MOJOSHADER_sampler) * ctx->sampler_count;
    MOJOSHADER_sampler *retval = (MOJOSHADER_sampler *) Malloc(ctx, len);

    if (retval != NULL)
    {
        RegisterList *item = ctx->samplers.next;
        int i;

        memset(retval, '\0', len);

        for (i = 0; i < ctx->sampler_count; i++)
        {
            if (item == NULL)
            {
                fail(ctx, "BUG: mismatched sampler list and count");
                break;
            } // if

            assert(item->regtype == REG_TYPE_SAMPLER);
            retval[i].type = cvtD3DToMojoSamplerType((TextureType) item->index);
            retval[i].index = item->regnum;
            retval[i].name = alloc_varname(ctx, item);
            retval[i].texbem = (item->misc != 0) ? 1 : 0;
            item = item->next;
        } // for
    } // if

    return retval;
} // build_samplers


static MOJOSHADER_attribute *build_attributes(Context *ctx, int *_count)
{
    int count = 0;

    if (ctx->attribute_count == 0)
    {
        *_count = 0;
        return NULL;  // nothing to do.
    } // if

    const size_t len = sizeof (MOJOSHADER_attribute) * ctx->attribute_count;
    MOJOSHADER_attribute *retval = (MOJOSHADER_attribute *) Malloc(ctx, len);

    if (retval != NULL)
    {
        RegisterList *item = ctx->attributes.next;
        MOJOSHADER_attribute *wptr = retval;
        int ignore = 0;
        int i;

        memset(retval, '\0', len);

        for (i = 0; i < ctx->attribute_count; i++)
        {
            if (item == NULL)
            {
                fail(ctx, "BUG: mismatched attribute list and count");
                break;
            } // if

            switch (item->regtype)
            {
                case REG_TYPE_RASTOUT:
                case REG_TYPE_ATTROUT:
                case REG_TYPE_TEXCRDOUT:
                case REG_TYPE_COLOROUT:
                case REG_TYPE_DEPTHOUT:
                    ignore = 1;
                    break;
                default:
                    ignore = 0;
                    break;
            } // switch

            if (!ignore)
            {
                wptr->usage = item->usage;
                wptr->index = item->index;
                wptr->name = alloc_varname(ctx, item);
                wptr++;
                count++;
            } // if

            item = item->next;
        } // for
    } // if

    *_count = count;
    return retval;
} // build_attributes

static MOJOSHADER_attribute *build_outputs(Context *ctx, int *_count)
{
    int count = 0;

    if (ctx->attribute_count == 0)
    {
        *_count = 0;
        return NULL;  // nothing to do.
    } // if

    const size_t len = sizeof (MOJOSHADER_attribute) * ctx->attribute_count;
    MOJOSHADER_attribute *retval = (MOJOSHADER_attribute *) Malloc(ctx, len);

    if (retval != NULL)
    {
        RegisterList *item = ctx->attributes.next;
        MOJOSHADER_attribute *wptr = retval;
        int i;

        memset(retval, '\0', len);

        for (i = 0; i < ctx->attribute_count; i++)
        {
            if (item == NULL)
            {
                fail(ctx, "BUG: mismatched attribute list and count");
                break;
            } // if

            switch (item->regtype)
            {
                case REG_TYPE_RASTOUT:
                case REG_TYPE_ATTROUT:
                case REG_TYPE_TEXCRDOUT:
                case REG_TYPE_COLOROUT:
                case REG_TYPE_DEPTHOUT:
                    wptr->usage = item->usage;
                    wptr->index = item->index;
                    wptr->name = alloc_varname(ctx, item);
                    wptr++;
                    count++;
                    break;
                default:
                    break;
            } // switch


            item = item->next;
        } // for
    } // if

    *_count = count;
    return retval;
} // build_outputs


static MOJOSHADER_parseData *build_parsedata(Context *ctx)
{
    char *output = NULL;
    MOJOSHADER_constant *constants = NULL;
    MOJOSHADER_uniform *uniforms = NULL;
    MOJOSHADER_attribute *attributes = NULL;
    MOJOSHADER_attribute *outputs = NULL;
    MOJOSHADER_sampler *samplers = NULL;
    MOJOSHADER_swizzle *swizzles = NULL;
    MOJOSHADER_error *errors = NULL;
    MOJOSHADER_parseData *retval = NULL;
    size_t output_len = 0;
    int attribute_count = 0;
    int output_count = 0;

    if (ctx->out_of_memory)
        return &MOJOSHADER_out_of_mem_data;

    retval = (MOJOSHADER_parseData*) Malloc(ctx, sizeof(MOJOSHADER_parseData));
    if (retval == NULL)
        return &MOJOSHADER_out_of_mem_data;

    memset(retval, '\0', sizeof (MOJOSHADER_parseData));

    if (!isfail(ctx))
        output = build_output(ctx, &output_len);

    if (!isfail(ctx))
        constants = build_constants(ctx);

    if (!isfail(ctx))
        uniforms = build_uniforms(ctx);

    if (!isfail(ctx))
        attributes = build_attributes(ctx, &attribute_count);

    if (!isfail(ctx))
        outputs = build_outputs(ctx, &output_count);

    if (!isfail(ctx))
        samplers = build_samplers(ctx);

    const int error_count = errorlist_count(ctx->errors);
    errors = errorlist_flatten(ctx->errors);

    if (!isfail(ctx))
    {
        if (ctx->swizzles_count > 0)
        {
            const int len = ctx->swizzles_count * sizeof (MOJOSHADER_swizzle);
            swizzles = (MOJOSHADER_swizzle *) Malloc(ctx, len);
            if (swizzles != NULL)
                memcpy(swizzles, ctx->swizzles, len);
        } // if
    } // if

    // check again, in case build_output, etc, ran out of memory.
    if (isfail(ctx))
    {
        int i;

        Free(ctx, output);
        Free(ctx, constants);
        Free(ctx, swizzles);

        if (uniforms != NULL)
        {
            for (i = 0; i < ctx->uniform_count; i++)
                Free(ctx, (void *) uniforms[i].name);
            Free(ctx, uniforms);
        } // if

        if (attributes != NULL)
        {
            for (i = 0; i < attribute_count; i++)
                Free(ctx, (void *) attributes[i].name);
            Free(ctx, attributes);
        } // if

        if (outputs != NULL)
        {
            for (i = 0; i < output_count; i++)
                Free(ctx, (void *) outputs[i].name);
            Free(ctx, outputs);
        } // if

        if (samplers != NULL)
        {
            for (i = 0; i < ctx->sampler_count; i++)
                Free(ctx, (void *) samplers[i].name);
            Free(ctx, samplers);
        } // if

        if (ctx->out_of_memory)
        {
            for (i = 0; i < error_count; i++)
            {
                Free(ctx, (void *) errors[i].filename);
                Free(ctx, (void *) errors[i].error);
            } // for
            Free(ctx, errors);
            Free(ctx, retval);
            return &MOJOSHADER_out_of_mem_data;
        } // if
    } // if
    else
    {
        retval->profile = ctx->profile->name;
        retval->output = output;
        retval->output_len = (int) output_len;
        retval->instruction_count = ctx->instruction_count;
        retval->shader_type = ctx->shader_type;
        retval->major_ver = (int) ctx->major_ver;
        retval->minor_ver = (int) ctx->minor_ver;
        retval->uniform_count = ctx->uniform_count;
        retval->uniforms = uniforms;
        retval->constant_count = ctx->constant_count;
        retval->constants = constants;
        retval->sampler_count = ctx->sampler_count;
        retval->samplers = samplers;
        retval->attribute_count = attribute_count;
        retval->attributes = attributes;
        retval->output_count = output_count;
        retval->outputs = outputs;
        retval->swizzle_count = ctx->swizzles_count;
        retval->swizzles = swizzles;
        retval->symbol_count = ctx->ctab.symbol_count;
        retval->symbols = ctx->ctab.symbols;
        retval->preshader = ctx->preshader;
        retval->mainfn = ctx->mainfn;

#if SUPPORT_PROFILE_SPIRV
        if (strcmp(retval->profile, MOJOSHADER_PROFILE_SPIRV) == 0
         || strcmp(retval->profile, MOJOSHADER_PROFILE_GLSPIRV) == 0)
        {
            size_t i, max;
            int binary_size = retval->output_len - sizeof(SpirvPatchTable);
            uint32 *binary = (uint32 *) retval->output;
            SpirvPatchTable *table = (SpirvPatchTable *) &retval->output[binary_size];

            if (table->vpflip.offset)      binary[table->vpflip.offset]      = table->vpflip.location;
            if (table->array_vec4.offset)  binary[table->array_vec4.offset]  = table->array_vec4.location;
            if (table->array_ivec4.offset) binary[table->array_ivec4.offset] = table->array_ivec4.location;
            if (table->array_bool.offset)  binary[table->array_bool.offset]  = table->array_bool.location;

            for (i = 0, max = STATICARRAYLEN(table->samplers); i < max; i++)
            {
                SpirvPatchEntry entry = table->samplers[i];
                if (entry.offset)
                    binary[entry.offset] = entry.location;
            } // for
        } // if
#endif // SUPPORT_PROFILE_SPIRV

        // we don't own these now, retval does.
        ctx->ctab.symbols = NULL;
        ctx->preshader = NULL;
        ctx->ctab.symbol_count = 0;
        ctx->mainfn = NULL;
    } // else

    retval->error_count = error_count;
    retval->errors = errors;
    retval->malloc = (ctx->malloc == MOJOSHADER_internal_malloc) ? NULL : ctx->malloc;
    retval->free = (ctx->free == MOJOSHADER_internal_free) ? NULL : ctx->free;
    retval->malloc_data = ctx->malloc_data;

    return retval;
} // build_parsedata

static void process_definitions(Context *ctx)
{
    // !!! FIXME: apparently, pre ps_3_0, sampler registers don't need to be
    // !!! FIXME:  DCL'd before use (default to 2d?). We aren't checking
    // !!! FIXME:  this at the moment, though.

    determine_constants_arrays(ctx);  // in case this hasn't been called yet.

    RegisterList *uitem = &ctx->uniforms;
    RegisterList *prev = &ctx->used_registers;
    RegisterList *item = prev->next;

    while (item != NULL)
    {
        RegisterList *next = item->next;
        const RegisterType regtype = item->regtype;
        const int regnum = item->regnum;
        MOJOSHADER_usage usage;

        if (!get_defined_register(ctx, regtype, regnum))
        {
            // haven't already dealt with this one.
            switch (regtype)
            {
                // !!! FIXME: I'm not entirely sure this is right...
                case REG_TYPE_RASTOUT:
                case REG_TYPE_ATTROUT:
                case REG_TYPE_TEXCRDOUT:
                case REG_TYPE_COLOROUT:
                case REG_TYPE_DEPTHOUT:
                    if (shader_is_vertex(ctx)&&shader_version_atleast(ctx,3,0))
                    {
                        fail(ctx, "vs_3 can't use output registers"
                                  " without declaring them first.");
                        return;
                    } // if

                    // Apparently this is an attribute that wasn't DCL'd.
                    //  Add it to the attribute list; deal with it later.
                    if (regtype == REG_TYPE_RASTOUT)
                    {
                        if ((RastOutType) regnum == RASTOUT_TYPE_POSITION)
                            usage = MOJOSHADER_USAGE_POSITION;
                        else if ((RastOutType) regnum == RASTOUT_TYPE_FOG)
                            usage = MOJOSHADER_USAGE_FOG;
                        else if ((RastOutType) regnum==RASTOUT_TYPE_POINT_SIZE)
                            usage = MOJOSHADER_USAGE_POINTSIZE;
                    } // if
                    else if (regtype == REG_TYPE_ATTROUT ||
                             regtype == REG_TYPE_COLOROUT)
                    {
                        usage = MOJOSHADER_USAGE_COLOR;
                    } // else if
                    else if (regtype == REG_TYPE_TEXCRDOUT)
                        usage = MOJOSHADER_USAGE_TEXCOORD;
                    else if (regtype == REG_TYPE_DEPTHOUT)
                        usage = MOJOSHADER_USAGE_DEPTH;

                    add_attribute_register(ctx, regtype, regnum, usage,
                                           regnum, 0xF, 0);
                    break;

                case REG_TYPE_ADDRESS:
                case REG_TYPE_PREDICATE:
                case REG_TYPE_TEMP:
                case REG_TYPE_LOOP:
                case REG_TYPE_LABEL:
                    ctx->profile->global_emitter(ctx, regtype, regnum);
                    break;

                case REG_TYPE_CONST:
                case REG_TYPE_CONSTINT:
                case REG_TYPE_CONSTBOOL:
                    // separate uniforms into a different list for now.
                    prev->next = next;
                    item->next = NULL;
                    uitem->next = item;
                    uitem = item;
                    item = prev;
                    break;

                case REG_TYPE_INPUT:
                    // You don't have to dcl_ your inputs in Shader Model 1.
                    if (!shader_version_atleast(ctx,2,0))
                    {
                        if (shader_is_pixel(ctx))
                        {
                            add_attribute_register(ctx, regtype, regnum,
                                                   MOJOSHADER_USAGE_COLOR, regnum,
                                                   0xF, 0);
                            break;
                        } // if
                        else if (shader_is_vertex(ctx))
                        {
                            int index = 0;
                            shader_model_1_input_usage(regnum, &usage, &index);
                            if (usage != MOJOSHADER_USAGE_UNKNOWN)
                            {
                                add_attribute_register(ctx, regtype, regnum, usage, index, 0xF, 0);
                                break;
                            } // if
                        } // else if
                    } // if

                    // fall through...

                default:
                    fail(ctx, "BUG: we used a register we don't know how to define.");
            } // switch
        } // if

        prev = item;
        item = next;
    } // while

    // okay, now deal with uniform/constant arrays...
    for (VariableList *var = ctx->variables; var != NULL; var = var->next)
    {
        if (var->used)
        {
            if (var->constant)
            {
                ctx->profile->const_array_emitter(ctx, var->constant,
                                                  var->index, var->count);
            } // if
            else
            {
                ctx->profile->array_emitter(ctx, var);
                ctx->uniform_float4_count += var->count;
            } // else
            ctx->uniform_count++;
        } // if
    } // for

    // ...and uniforms...
    for (item = ctx->uniforms.next; item != NULL; item = item->next)
    {
        int arraysize = -1;
        VariableList *var = NULL;

        // check if this is a register contained in an array...
        if (item->regtype == REG_TYPE_CONST)
        {
            for (var = ctx->variables; var != NULL; var = var->next)
            {
                if (!var->used)
                    continue;

                const int regnum = item->regnum;
                const int lo = var->index;
                if ( (regnum >= lo) && (regnum < (lo + var->count)) )
                {
                    assert(!var->constant);
                    item->array = var;  // used when building parseData.
                    arraysize = var->count;
                    break;
                } // if
            } // for
        } // if

        ctx->profile->uniform_emitter(ctx, item->regtype, item->regnum, var);

        if (arraysize < 0)  // not part of an array?
        {
            ctx->uniform_count++;
            switch (item->regtype)
            {
                case REG_TYPE_CONST: ctx->uniform_float4_count++; break;
                case REG_TYPE_CONSTINT: ctx->uniform_int4_count++; break;
                case REG_TYPE_CONSTBOOL: ctx->uniform_bool_count++; break;
                default: break;
            } // switch
        } // if
    } // for

    // ...and samplers...
    for (item = ctx->samplers.next; item != NULL; item = item->next)
    {
        ctx->sampler_count++;
        ctx->profile->sampler_emitter(ctx, item->regnum,
                                      (TextureType) item->index,
                                      item->misc != 0);
    } // for

    // ...and attributes...
    for (item = ctx->attributes.next; item != NULL; item = item->next)
    {
        ctx->attribute_count++;
        ctx->profile->attribute_emitter(ctx, item->regtype, item->regnum,
                                        item->usage, item->index,
                                        item->writemask, item->misc);
    } // for
} // process_definitions


static void verify_swizzles(Context *ctx)
{
    size_t i;
    const char *failmsg = "invalid swizzle";
    for (i = 0; i < ctx->swizzles_count; i++)
    {
        const MOJOSHADER_swizzle *swiz = &ctx->swizzles[i];
        if (swiz->swizzles[0] > 3) { fail(ctx, failmsg); return; }
        if (swiz->swizzles[1] > 3) { fail(ctx, failmsg); return; }
        if (swiz->swizzles[2] > 3) { fail(ctx, failmsg); return; }
        if (swiz->swizzles[3] > 3) { fail(ctx, failmsg); return; }
    } // for
} // verify_swizzles


// API entry point...

// !!! FIXME:
// MSDN: "Shader validation will fail CreatePixelShader on any shader that
//  attempts to read from a temporary register that has not been written by a
//  previous instruction."  (true for ps_1_*, maybe others). Check this.

const MOJOSHADER_parseData *MOJOSHADER_parse(const char *profile,
                                             const char *mainfn,
                                             const unsigned char *tokenbuf,
                                             const unsigned int bufsize,
                                             const MOJOSHADER_swizzle *swiz,
                                             const unsigned int swizcount,
                                             const MOJOSHADER_samplerMap *smap,
                                             const unsigned int smapcount,
                                             MOJOSHADER_malloc m,
                                             MOJOSHADER_free f, void *d)
{
    MOJOSHADER_parseData *retval = NULL;
    Context *ctx = NULL;
    int rc = 0;
    int failed = 0;

    if ( ((m == NULL) && (f != NULL)) || ((m != NULL) && (f == NULL)) )
        return &MOJOSHADER_out_of_mem_data;  // supply both or neither.

    ctx = build_context(profile, mainfn, tokenbuf, bufsize, swiz, swizcount,
                        smap, smapcount, m, f, d);
    if (ctx == NULL)
        return &MOJOSHADER_out_of_mem_data;

    if (profile == NULL)  // build_context allows NULL; check this ourselves.
        fail(ctx, "Profile name is NULL");

    if (isfail(ctx))
    {
        retval = build_parsedata(ctx);
        destroy_context(ctx);
        return retval;
    } // if

    verify_swizzles(ctx);

    if (!ctx->mainfn)
        ctx->mainfn = StrDup(ctx, "main");

    // Version token always comes first.
    ctx->current_position = 0;
    rc = parse_version_token(ctx, profile);

    // drop out now if this definitely isn't bytecode. Saves lots of
    //  meaningless errors flooding through.
    if (rc < 0)
    {
        retval = build_parsedata(ctx);
        destroy_context(ctx);
        return retval;
    } // if

    if ( ((uint32) rc) > ctx->tokencount )
    {
        fail(ctx, "Corrupted or truncated shader");
        ctx->tokencount = rc;
    } // if

    adjust_token_position(ctx, rc);

    // parse out the rest of the tokens after the version token...
    while (ctx->tokencount > 0)
    {
        if (!ctx->know_shader_size)
            ctx->tokencount = 0xFFFFFFFF;  // keep this value obscenely large.

        // reset for each token.
        if (isfail(ctx))
        {
            failed = 1;
            ctx->isfail = 0;
        } // if

        rc = parse_token(ctx);
        if ( ((uint32) rc) > ctx->tokencount )
        {
            fail(ctx, "Corrupted or truncated shader");
            break;
        } // if

        adjust_token_position(ctx, rc);
    } // while

    ctx->current_position = MOJOSHADER_POSITION_AFTER;

    // for ps_1_*, the output color is written to r0...throw an
    //  error if this register was never written. This isn't
    //  important for vertex shaders, or shader model 2+.
    if (shader_is_pixel(ctx) && !shader_version_atleast(ctx, 2, 0))
    {
        if (!register_was_written(ctx, REG_TYPE_TEMP, 0))
            fail(ctx, "r0 (pixel shader 1.x color output) never written to");
    } // if

    if (!failed)
    {
        process_definitions(ctx);
        failed = isfail(ctx);
    } // if

    if (!failed)
        ctx->profile->finalize_emitter(ctx);

    ctx->isfail = failed;
    retval = build_parsedata(ctx);
    destroy_context(ctx);
    return retval;
} // MOJOSHADER_parse


void MOJOSHADER_freeParseData(const MOJOSHADER_parseData *_data)
{
    MOJOSHADER_parseData *data = (MOJOSHADER_parseData *) _data;
    if ((data == NULL) || (data == &MOJOSHADER_out_of_mem_data))
        return;  // no-op.

    MOJOSHADER_free f = (data->free == NULL) ? MOJOSHADER_internal_free : data->free;
    void *d = data->malloc_data;
    int i;

    // we don't f(data->profile), because that's internal static data.

    f((void *) data->mainfn, d);
    f((void *) data->output, d);
    f((void *) data->constants, d);
    f((void *) data->swizzles, d);

    for (i = 0; i < data->error_count; i++)
    {
        f((void *) data->errors[i].error, d);
        f((void *) data->errors[i].filename, d);
    } // for
    f((void *) data->errors, d);

    for (i = 0; i < data->uniform_count; i++)
        f((void *) data->uniforms[i].name, d);
    f((void *) data->uniforms, d);

    for (i = 0; i < data->attribute_count; i++)
        f((void *) data->attributes[i].name, d);
    f((void *) data->attributes, d);

    for (i = 0; i < data->output_count; i++)
        f((void *) data->outputs[i].name, d);
    f((void *) data->outputs, d);

    for (i = 0; i < data->sampler_count; i++)
        f((void *) data->samplers[i].name, d);
    f((void *) data->samplers, d);

    free_symbols(f, d, data->symbols, data->symbol_count);
    MOJOSHADER_freePreshader(data->preshader);

    f(data, d);
} // MOJOSHADER_freeParseData


int MOJOSHADER_version(void)
{
    return MOJOSHADER_VERSION;
} // MOJOSHADER_version


const char *MOJOSHADER_changeset(void)
{
    return MOJOSHADER_CHANGESET;
} // MOJOSHADER_changeset


int MOJOSHADER_maxShaderModel(const char *profile)
{
    #define PROFILE_SHADER_MODEL(p,v) if (strcmp(profile, p) == 0) return v;
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_D3D, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_BYTECODE, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_HLSL, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_GLSL, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_GLSL120, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_GLSLES, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_ARB1, 2);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_NV2, 2);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_NV3, 2);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_NV4, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_METAL, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_SPIRV, 3);
    PROFILE_SHADER_MODEL(MOJOSHADER_PROFILE_GLSPIRV, 3);
    #undef PROFILE_SHADER_MODEL
    return -1;  // unknown profile?
} // MOJOSHADER_maxShaderModel


const MOJOSHADER_preshader *MOJOSHADER_parsePreshader(const unsigned char *buf,
                                                      const unsigned int buflen,
                                                      MOJOSHADER_malloc m,
                                                      MOJOSHADER_free f,
                                                      void *d)
{
    MOJOSHADER_preshader *retval = NULL;

    // We need just enough Context for allocators and error state.
    Context *ctx = build_context(NULL, NULL, buf, buflen, NULL, 0, NULL, 0, m, f, d);
    if (ctx == NULL)
    {
        return retval; // !!! FIXME: Out of memory struct for MOJOSHADER_preshader
    }
    parse_preshader(ctx, ctx->tokens, ctx->tokencount);
    if (!isfail(ctx))
    {
        retval = ctx->preshader;
        ctx->preshader = NULL;  // don't let destroy_context() eat the retval.
    } // if

    destroy_context(ctx);
    return retval;
} // MOJOSHADER_parsePreshader

void MOJOSHADER_freePreshader(const MOJOSHADER_preshader *preshader)
{
    if (preshader != NULL)
    {
        unsigned int i, j;
        void *d = preshader->malloc_data;
        MOJOSHADER_free f = preshader->free;
        if (f == NULL) f = MOJOSHADER_internal_free;

        f((void *) preshader->literals, d);
        for (i = 0; i < preshader->instruction_count; i++)
        {
            for (j = 0; j < preshader->instructions[i].operand_count; j++)
                f((void *) preshader->instructions[i].operands[j].array_registers, d);
        } // for
        f((void *) preshader->instructions, d);
        f((void *) preshader->registers, d);
        free_symbols(f, d, preshader->symbols, preshader->symbol_count);
        f((void *) preshader, d);
    } // if
} // MOJOSHADER_freePreshader

#if SUPPORT_PROFILE_SPIRV
#include <spirv/spirv.h> /* SpvOp, SpvOpConvertUToF, SpvOpConvertSToF, SpvOpCopyObject */
#endif

int MOJOSHADER_linkSPIRVShaders(const MOJOSHADER_parseData *vertex_spirv,
                                const MOJOSHADER_parseData *pixel_spirv,
                                const MOJOSHADER_vertexAttribute *vertexAttributes,
                                const int vertexAttributeCount)
{
#if SUPPORT_PROFILE_SPIRV
    // We have to patch the SPIR-V output to ensure type consistency. The non-float types are:
    // BYTE4  - 5
    // SHORT2 - 6
    // SHORT4 - 7
    int vDataLen = vertex_spirv->output_len - sizeof(SpirvPatchTable);
    SpirvPatchTable *vTable = (SpirvPatchTable *) &vertex_spirv->output[vDataLen];

    for (int i = 0; i < vertexAttributeCount; i += 1)
    {
        const MOJOSHADER_vertexAttribute *element = &vertexAttributes[i];
        uint32 typeDecl, typeLoad;
        SpvOp opcodeLoad;

        if (element->vertexElementFormat >= MOJOSHADER_VERTEXELEMENTFORMAT_BYTE4 && element->vertexElementFormat <= MOJOSHADER_VERTEXELEMENTFORMAT_SHORT4)
        {
            const int isByte = (element->vertexElementFormat == MOJOSHADER_VERTEXELEMENTFORMAT_BYTE4);
            typeDecl = isByte ? vTable->tid_uvec4_p : vTable->tid_ivec4_p;
            typeLoad = isByte ? vTable->tid_uvec4 : vTable->tid_ivec4;
            opcodeLoad = isByte ? SpvOpConvertUToF : SpvOpConvertSToF;
        }
        else
        {
            typeDecl = vTable->tid_vec4_p;
            typeLoad = vTable->tid_vec4;
            opcodeLoad = SpvOpCopyObject;
        }

        uint32 typeDeclOffset = vTable->attrib_type_offsets[element->usage][element->usageIndex];
        if (typeDeclOffset > 0) // Vertex attribs passed may not exist in the vertex shader
        {
            ((uint32*)vertex_spirv->output)[typeDeclOffset] = typeDecl;
            for (uint32 j = 0; j < vTable->attrib_type_load_offsets[element->usage][element->usageIndex].num_loads; j += 1)
            {

                uint32 typeLoadOffset = vTable->attrib_type_load_offsets[element->usage][element->usageIndex].load_types[j];
                uint32 opcodeLoadOffset = vTable->attrib_type_load_offsets[element->usage][element->usageIndex].load_opcodes[j];
                if (typeLoadOffset > 0)
                    ((uint32*)vertex_spirv->output)[typeLoadOffset] = typeLoad;
                if (opcodeLoadOffset > 0)
                {
                    uint32* ptr_to_opcode_u32 = &((uint32*)vertex_spirv->output)[opcodeLoadOffset];
                    *ptr_to_opcode_u32 = (*ptr_to_opcode_u32 & 0xFFFF0000) | opcodeLoad;
                } // if
            } // for
        } // if
    } // if

    MOJOSHADER_spirv_link_attributes(vertex_spirv, pixel_spirv, 0);
    return sizeof(SpirvPatchTable);
#else
    return 0;
#endif
}

// end of mojoshader.c ...

