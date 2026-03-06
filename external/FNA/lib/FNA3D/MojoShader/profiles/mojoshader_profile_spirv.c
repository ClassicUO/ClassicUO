/**
 * MojoShader; generate shader programs from bytecode of compiled
 *  Direct3D shaders.
 *
 * Please see the file LICENSE.txt in the source's root directory.
 *
 *  This file written by Ryan C. Gordon.
 */

#define __MOJOSHADER_INTERNAL__ 1
#include "mojoshader_profile.h"

#pragma GCC visibility push(hidden)

#if SUPPORT_PROFILE_SPIRV
#include "spirv/spirv.h"
#include "spirv/GLSL.std.450.h"
#include <float.h>

static const int SPV_NO_SWIZZLE = 0xE4; // 0xE4 == 11100100 ... 0 1 2 3. No swizzle.

#define EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(op) \
    void emit_SPIRV_##op(Context *ctx) { \
        fail(ctx, #op " unimplemented in spirv profile"); \
    }

typedef struct SpirvTexm3x3SetupResult
{
    // vec4 load results
    uint32 id_dst_pad0;
    uint32 id_dst_pad1;
    uint32 id_dst;

    // float dot results
    uint32 id_res_x;
    uint32 id_res_y;
    uint32 id_res_z;
} SpirvTexm3x3SetupResult;

static const char *spv_get_uniform_array_varname(Context *ctx,
                                                   const RegisterType regtype,
                                                   char *buf, const size_t len)
{
    const char *shadertype = ctx->shader_type_str;
    const char *type = "";
    switch (regtype)
    {
        case REG_TYPE_CONST: type = "vec4"; break;
        case REG_TYPE_CONSTINT: type = "ivec4"; break;
        case REG_TYPE_CONSTBOOL: type = "bool"; break;
        default: fail(ctx, "BUG: used a uniform we don't know how to define.");
    } // switch
    snprintf(buf, len, "%s_uniforms_%s", shadertype, type);
    return buf;
} // spv_get_uniform_array_varname

static uint32 spv_bumpid(Context *ctx)
{
    return (ctx->spirv.idmax += 1);
} // spv_bumpid

static RegisterList *spv_getreg(Context *ctx, const RegisterType regtype, const int regnum)
{
    RegisterList *r = reglist_find(&ctx->used_registers, regtype, regnum);
    if (!r)
    {
        failf(ctx, "register not found rt=%d, rn=%d", regtype, regnum);
        return NULL;
    } // if
    return r;
} // spv_getreg

static void spv_componentlist_free(Context *ctx, ComponentList *cl)
{
    ComponentList *next;
    while (cl)
    {
        next = cl->next;
        Free(ctx, cl);
        cl = next;
    } // while
} // spv_componentlist_free

static ComponentList *spv_componentlist_alloc(Context *ctx)
{
    ComponentList *ret = (ComponentList *) Malloc(ctx, sizeof(ComponentList));
    if (!ret) return NULL;
    ret->id = 0;
    ret->v.i = 0;
    ret->next = NULL;
    return ret;
} // spv_componentlist_alloc

static const char *get_SPIRV_varname_in_buf(Context *ctx, const RegisterType rt,
                                           const int regnum, char *buf,
                                           const size_t buflen)
{
    // turns out these are identical at the moment.
    return get_D3D_varname_in_buf(ctx, rt, regnum, buf, buflen);
} // get_SPIRV_varname_in_buf

const char *get_SPIRV_varname(Context *ctx, const RegisterType rt,
                                    const int regnum)
{
    // turns out these are identical at the moment.
    return get_D3D_varname(ctx, rt, regnum);
} // get_SPIRV_varname


static inline const char *get_SPIRV_const_array_varname_in_buf(Context *ctx,
                                                const int base, const int size,
                                                char *buf, const size_t buflen)
{
    snprintf(buf, buflen, "c_array_%d_%d", base, size);
    return buf;
} // get_SPIRV_const_array_varname_in_buf


const char *get_SPIRV_const_array_varname(Context *ctx, int base, int size)
{
    char buf[64];
    get_SPIRV_const_array_varname_in_buf(ctx, base, size, buf, sizeof (buf));
    return StrDup(ctx, buf);
} // get_SPIRV_const_array_varname

static uint32 spv_get_uniform_array_id(Context *ctx, const RegisterType regtype)
{
    uint32 id;
    switch (regtype)
    {
        case REG_TYPE_CONST:
            id = ctx->spirv.uniform_arrays.idvec4;
            if (id == 0)
            {
                id = spv_bumpid(ctx);
                ctx->spirv.uniform_arrays.idvec4 = id;
            } // if
            break;

        case REG_TYPE_CONSTINT:
            id = ctx->spirv.uniform_arrays.idivec4;
            if (id == 0)
            {
                id = spv_bumpid(ctx);
                ctx->spirv.uniform_arrays.idivec4 = id;
            } // if
            break;

        case REG_TYPE_CONSTBOOL:
            id = ctx->spirv.uniform_arrays.idbool;
            if (id == 0)
            {
                id = spv_bumpid(ctx);
                ctx->spirv.uniform_arrays.idbool = id;
            } // if
            break;

        default:
            fail(ctx, "Unexpected register type used to access uniform array.");
            id = 0;
    } // switch

    return id;
} // spv_get_uniform_array_id

static void spv_emit_part_va(Context* ctx, uint32 word_count, uint32 argc, SpvOp op, va_list args)
{
    assert(ctx->output != NULL);
    if (isfail(ctx))
        return;  // we failed previously, don't go on...

    uint32 word = op | (word_count << 16);
    buffer_append(ctx->output, &word, sizeof(word));
    while (--argc)
    {
        word = va_arg(args, uint32);
        buffer_append(ctx->output, &word, sizeof(word));
    } // while
} // spv_emit_part_va

static void spv_emit_part(Context* ctx, uint32 word_count, uint32 argc, SpvOp op, ...)
{
    va_list args;
    va_start(args, op);
    spv_emit_part_va(ctx, word_count, argc, op, args);
    va_end(args);
} // spv_emit_part

static void spv_emit(Context *ctx, uint32 word_count, SpvOp op, ...)
{
    va_list args;
    va_start(args, op);
    spv_emit_part_va(ctx, word_count, word_count, op, args);
    va_end(args);
} // spv_emit

static void spv_emit_word(Context *ctx, uint32 word)
{
    assert(ctx->output != NULL);
    if (isfail(ctx))
        return;  // we failed previously, don't go on...

    buffer_append(ctx->output, &word, sizeof(word));
} // spv_emit_word

static void spv_emit_str(Context *ctx, const char *str)
{
    size_t len;
    uint32 trail;
    assert(ctx->output != NULL);
    if (isfail(ctx))
        return;  // we failed previously, don't go on...

    if (str == NULL)
        return spv_emit_word(ctx, 0);
    len = strlen(str) + 1;
    buffer_append(ctx->output, str, len);
    len = len % 4;
    if (len)
    {
        trail = 0;
        buffer_append(ctx->output, &trail, 4 - len);
    } // if
} // spv_emit_str

// get the word count of a string
static uint32 spv_strlen(const char *str)
{
    size_t len = strlen(str);
    return (uint32) ((len / 4) + 1);
} // spv_strlen

// emits an OpName straight into ctx->globals
static void spv_output_name(Context *ctx, uint32 id, const char *str)
{
    if (isfail(ctx))
        return;  // we failed previously, don't go on...

    push_output(ctx, &ctx->globals);
    spv_emit_part(ctx, 2 + spv_strlen(str), 2, SpvOpName, id);
    spv_emit_str(ctx, str);
    pop_output(ctx);
} // spv_output_name

// emit an OpName instruction to identify a register
static void spv_output_regname(Context *ctx, uint32 id, RegisterType regtype, int regnum)
{
    char varname[64];
    snprintf(varname, sizeof(varname), "%s_", ctx->shader_type_str);
    size_t offset = strlen(varname);
    get_SPIRV_varname_in_buf(ctx, regtype, regnum, varname + offset, sizeof(varname) - offset);
    spv_output_name(ctx, id, varname);
} // spv_output_regname

// emits an OpDecorate BuiltIn straight into ctx->helpers
static void spv_output_builtin(Context *ctx, uint32 id, SpvBuiltIn builtin)
{
    if (isfail(ctx))
        return;  // we failed previously, don't go on...

    push_output(ctx, &ctx->helpers);
    spv_emit(ctx, 4, SpvOpDecorate, id, SpvDecorationBuiltIn, builtin);
    pop_output(ctx);
} // spv_output_builtin

static uint32 spv_output_location(Context *ctx, uint32 id, uint32 loc)
{
    push_output(ctx, &ctx->helpers);
    spv_emit(ctx, 4, SpvOpDecorate, id, SpvDecorationLocation, loc);
    pop_output(ctx);
    return (buffer_size(ctx->helpers) >> 2) - 1;
} // spv_output_location

static void spv_output_color_location(Context *ctx, uint32 id, uint32 index)
{
    SpirvPatchTable* table = &ctx->spirv.patch_table;
    push_output(ctx, &ctx->helpers);
    spv_emit(ctx, 4, SpvOpDecorate, id, SpvDecorationLocation, index);
    pop_output(ctx);
    table->output_offsets[index] = (buffer_size(ctx->helpers) >> 2) - 1;
} // spv_output_color_location

static void spv_output_attrib_location(Context *ctx, uint32 id,
                                       MOJOSHADER_usage usage, uint32 index)
{
    SpirvPatchTable* table = &ctx->spirv.patch_table;
    push_output(ctx, &ctx->helpers);
    spv_emit(ctx, 4, SpvOpDecorate, id, SpvDecorationLocation, 0xDEADBEEF);
    pop_output(ctx);
    table->attrib_offsets[usage][index] = (buffer_size(ctx->helpers) >> 2) - 1;
} // spv_output_attrib_location

static void spv_output_sampler_binding(Context *ctx, uint32 id, uint32 binding)
{
    if (isfail(ctx))
        return;

    uint32 set = 0;
    if (ctx->spirv.mode == SPIRV_MODE_VK)
    {
        set = shader_is_vertex(ctx) ? MOJOSHADER_SPIRV_VS_SAMPLER_SET
                                    : MOJOSHADER_SPIRV_PS_SAMPLER_SET;
    } // if

    push_output(ctx, &ctx->helpers);
    spv_emit(ctx, 4, SpvOpDecorate, id, SpvDecorationDescriptorSet, set);
    spv_emit(ctx, 4, SpvOpDecorate, id, SpvDecorationBinding, binding);
    pop_output(ctx);
} // spv_output_sampler_binding

static SpirvTypeIdx spv_change_base_type_vec_dim(SpirvTypeIdx sti, uint32 dim)
{
    uint32 dimSub1 = dim - 1;
    assert(STI_CORE_START_ <= sti && sti < STI_CORE_END_);
    assert(dimSub1 < 4);

    SpirvTypeIdx sti_base = (SpirvTypeIdx)(sti & ~0x3);
    SpirvTypeIdx sti_new = (SpirvTypeIdx)(sti_base | dimSub1);
    return sti_new;
} // spv_change_base_type_vec_dim

static uint32 spv_get_type(Context *ctx, SpirvTypeIdx tidx)
{
    assert(((uint32)tidx) < ((uint32)STI_LENGTH_));

    uint32 tid = ctx->spirv.tid[tidx];
    if (tid)
        return tid;

    push_output(ctx, &ctx->mainline_intro);
    if (STI_CORE_START_ <= tidx && tidx < STI_CORE_END_)
    {
        uint32 dim = tidx & 0x3;
        SpirvType type = (SpirvType)((tidx >> 2) & 0x3);
        if (dim)
        {
            uint32 tid_base = spv_get_type(ctx, (SpirvTypeIdx)(tidx - dim));
            tid = spv_bumpid(ctx);
            spv_emit(ctx, 4, SpvOpTypeVector, tid, tid_base, dim + 1);
        } // if
        else
        {
            tid = spv_bumpid(ctx);
            switch (type)
            {
                case ST_FLOAT: spv_emit(ctx, 3, SpvOpTypeFloat, tid, 32); break;
                case ST_SINT: spv_emit(ctx, 4, SpvOpTypeInt, tid, 32, 1); break;
                case ST_UINT: spv_emit(ctx, 4, SpvOpTypeInt, tid, 32, 0); break;
                case ST_BOOL: spv_emit(ctx, 2, SpvOpTypeBool, tid); break;
                default: assert(!"Unexpected value of SpirvType."); break;
            } // switch
        } // else
    } // if
    else if (STI_IMAGE2D <= tidx && tidx <= STI_IMAGECUBE)
    {
        static const SpvDim dim_table[] = {SpvDim2D, SpvDim3D, SpvDimCube};
        SpvDim dim = dim_table[tidx - STI_IMAGE2D];
        uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
        uint32 id_image = spv_bumpid(ctx);
        tid = spv_bumpid(ctx);
        spv_emit(ctx, 9, SpvOpTypeImage, id_image, tid_float, dim, 0, 0, 0, 1, SpvImageFormatUnknown);
        spv_emit(ctx, 3, SpvOpTypeSampledImage, tid, id_image);
    } // else if
    else if (tidx == STI_VOID)
    {
        tid = spv_bumpid(ctx);
        spv_emit(ctx, 2, SpvOpTypeVoid, tid);
    } // else if
    else if (tidx == STI_FUNC_VOID)
    {
        uint32 tid_void = spv_get_type(ctx, STI_VOID);
        tid = spv_bumpid(ctx);
        spv_emit(ctx, 3, SpvOpTypeFunction, tid, tid_void);
    } // else if
    else if (tidx == STI_FUNC_LIT)
    {
        uint32 tid_vec4 = spv_get_type(ctx, STI_VEC4);
        tid = spv_bumpid(ctx);
        spv_emit(ctx, 3 + 1, SpvOpTypeFunction, tid, tid_vec4, tid_vec4);
    } // else if
    else if (STI_PTR_START_ <= tidx && tidx < STI_PTR_END_)
    {
        uint32 dim = (tidx & (1 << 4)) ? 3 : 0;
        SpirvType type = (SpirvType)((tidx >> 2) & 0x3);
        uint32 tid_base = spv_get_type(ctx, (SpirvTypeIdx)((1 << 4) | (type << 2) | dim));
        static const SpvStorageClass sc_map[] = {
            SpvStorageClassInput,
            SpvStorageClassInput,
            SpvStorageClassOutput,
            SpvStorageClassOutput,
            SpvStorageClassPrivate,
            SpvStorageClassPrivate,
            SpvStorageClassUniformConstant,
            SpvStorageClassUniform,
        };
        SpvStorageClass sc = sc_map[((tidx & 0x3) << 1) | (ctx->spirv.mode == SPIRV_MODE_VK)];
        tid = spv_bumpid(ctx);
        spv_emit(ctx, 4, SpvOpTypePointer, tid, sc, tid_base);
    } // else if
    else if (STI_PTR_IMAGE2D <= tidx && tidx <= STI_PTR_IMAGECUBE)
    {
        uint32 tid_image = spv_get_type(ctx, (SpirvTypeIdx)(tidx - (STI_PTR_IMAGE2D - STI_IMAGE2D)));
        tid = spv_bumpid(ctx);
        spv_emit(ctx, 4, SpvOpTypePointer, tid, SpvStorageClassUniformConstant, tid_image);
    } // else if
    else if (tidx == STI_PTR_VEC2_I)
    {
        uint32 tid_base = spv_get_type(ctx, STI_VEC2);
        tid = spv_bumpid(ctx);
        spv_emit(ctx, 4, SpvOpTypePointer, tid, SpvStorageClassInput, tid_base);
    } // else if
    else
        assert(!"Unexpected value of type index.");
    pop_output(ctx);

    ctx->spirv.tid[tidx] = tid;
    return tid;
} // spv_get_type

static uint32 spv_gettrue(Context *ctx)
{
    if (ctx->spirv.idtrue)
        return ctx->spirv.idtrue;

    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
    uint32 id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 3, SpvOpConstantTrue, tid_bool, id);
    pop_output(ctx);
    return ctx->spirv.idtrue = id;
} // spv_gettrue

static uint32 spv_getfalse(Context *ctx)
{
    if (ctx->spirv.idfalse)
        return ctx->spirv.idfalse;

    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
    uint32 id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 3, SpvOpConstantFalse, tid_bool, id);
    pop_output(ctx);
    return ctx->spirv.idfalse = id;
} // spv_getfalse

static uint32 spv_getext(Context *ctx)
{
    if (ctx->spirv.idext)
        return ctx->spirv.idext;

    return ctx->spirv.idext = spv_bumpid(ctx);
} // spv_getext

static uint32 spv_output_scalar(Context *ctx, ComponentList *cl,
                             MOJOSHADER_attributeType type)
{
    uint32 idret, idtype;
    if (type == MOJOSHADER_ATTRIBUTE_FLOAT)
        idtype = spv_get_type(ctx, STI_FLOAT);
    else if (type == MOJOSHADER_ATTRIBUTE_INT)
        idtype = spv_get_type(ctx, STI_INT);
    else if (type == MOJOSHADER_ATTRIBUTE_UINT)
        idtype = spv_get_type(ctx, STI_UINT);
    else
    {
        failf(ctx, "spv_output_scalar: invalid attribute type %d", type);
        return 0;
    } // else
    idret = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 4, SpvOpConstant, idtype, idret, cl->v.u);
    pop_output(ctx);
    return idret;
} // spv_output_scalar

// The spv_getscalar* functions retrieve the result id of an OpConstant
// instruction with the corresponding value v, or generate a new one.
static uint32 spv_getscalarf(Context *ctx, float v)
{
    ComponentList *prev = &(ctx->spirv.cl.f), *cl = ctx->spirv.cl.f.next;
    while (cl)
    {
        if (v == cl->v.f)
            return cl->id;
        else if (v < cl->v.f)
            break;
        prev = cl;
        cl = cl->next;
    } // while
    cl = spv_componentlist_alloc(ctx);
    cl->next = prev->next;
    prev->next = cl;
    cl->v.f = v;
    cl->id = spv_output_scalar(ctx, cl, MOJOSHADER_ATTRIBUTE_FLOAT);
    return cl->id;
} // spv_getscalarf

static uint32 spv_getscalari(Context *ctx, int v)
{
    ComponentList *prev = &(ctx->spirv.cl.i), *cl = ctx->spirv.cl.i.next;
    while (cl)
    {
        if (v == cl->v.i)
            return cl->id;
        else if (v < cl->v.i)
            break;
        prev = cl;
        cl = cl->next;
    } // while
    cl = spv_componentlist_alloc(ctx);
    cl->next = prev->next;
    prev->next = cl;
    cl->v.i = v;
    cl->id = spv_output_scalar(ctx, cl, MOJOSHADER_ATTRIBUTE_INT);
    return cl->id;
} // spv_getscalari

static uint32 spv_get_constant_composite(Context *ctx, uint32 tid, uint32* cache, float scalar)
{
    uint32 i;

    assert(tid != 0);
    uint32 dim =
       (tid == ctx->spirv.tid[STI_VEC4]) ? 4 :
       (tid == ctx->spirv.tid[STI_VEC3]) ? 3 :
       (tid == ctx->spirv.tid[STI_VEC2]) ? 2 : 1;

    uint32 id = cache[dim - 1];
    if (id)
        return id;

    uint32 sid = spv_getscalarf(ctx, scalar);
    if (dim == 1)
    {
        cache[0] = sid;
        return sid;
    } // if

    id = spv_bumpid(ctx);
    push_output(ctx, &ctx->mainline_intro);
    spv_emit_part(ctx, 3 + dim, 3, SpvOpConstantComposite, tid, id);
    for (i = 0; i < dim; i++)
        spv_emit_word(ctx, sid);
    pop_output(ctx);
    cache[dim - 1] = id;
    return id;
} // spv_get_constant_composite

static uint32 spv_get_zero(Context *ctx, uint32 tid)
{
    return spv_get_constant_composite(ctx, tid, ctx->spirv.id_0_0, 0.0f);
} // spv_get_zero

static uint32 spv_get_one(Context *ctx, uint32 tid)
{
    return spv_get_constant_composite(ctx, tid, ctx->spirv.id_1_0, 1.0f);
} // spv_get_one

static uint32 spv_get_flt_max(Context *ctx, uint32 tid)
{
    return spv_get_constant_composite(ctx, tid, ctx->spirv.id_flt_max, FLT_MAX);
} // spv_get_one

static uint32 spv_getvec4_zero(Context *ctx)
{
    return spv_get_constant_composite(ctx, spv_get_type(ctx, STI_VEC4), ctx->spirv.id_0_0, 0.0f);
} // spv_getvec4_zero

static uint32 spv_getvec4_one(Context *ctx)
{
    return spv_get_constant_composite(ctx, spv_get_type(ctx, STI_VEC4), ctx->spirv.id_1_0, 1.0f);
} // spv_getvec4_one

// Make a 4-channel vector with a value broadcast across all channels. Roughly equivalent to `vec4(value)` in GLSL
static uint32 spv_vectorbroadcast(Context *ctx, uint32 tid, uint32 value)
{
    uint32 result = spv_bumpid(ctx);
    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 3 + 4, SpvOpCompositeConstruct, tid, result, value, value, value, value);
    pop_output(ctx);
    return result;
} // spv_vectorbroadcast

static void spv_branch_push(Context *ctx, uint32 id_merge, uint32 patch_offset)
{
    assert(((size_t)ctx->branch_labels_stack_index) < STATICARRAYLEN(ctx->branch_labels_stack));
    int pos = ctx->branch_labels_stack_index++;
    ctx->branch_labels_stack[pos] = id_merge;
    ctx->branch_labels_patch_stack[pos] = patch_offset;
} // spv_branch_push

static void spv_branch_get(Context *ctx, uint32* out_id_merge, uint32* out_patch_offset)
{
    assert(ctx->branch_labels_stack_index > 0);
    int pos = ctx->branch_labels_stack_index - 1;
    *out_id_merge = ctx->branch_labels_stack[pos];
    *out_patch_offset = ctx->branch_labels_patch_stack[pos];
} // spv_branch_get

static void spv_branch_pop(Context *ctx, uint32* out_id_merge, uint32* out_patch_offset)
{
    spv_branch_get(ctx, out_id_merge, out_patch_offset);
    ctx->branch_labels_stack_index--;
} // spv_branch_pop

static void spv_loop_push(Context *ctx, const SpirvLoopInfo *loop)
{
    assert(((size_t)ctx->spirv.loop_stack_idx) < STATICARRAYLEN(ctx->spirv.loop_stack));
    int pos = ctx->spirv.loop_stack_idx++;
    ctx->spirv.loop_stack[pos] = *loop;
} // spv_loop_push

static void spv_loop_get(Context *ctx, SpirvLoopInfo *loop)
{
    assert(ctx->spirv.loop_stack_idx > 0);
    int pos = ctx->spirv.loop_stack_idx - 1;
    *loop = ctx->spirv.loop_stack[pos];
} // spv_loop_get

static void spv_loop_pop(Context *ctx, SpirvLoopInfo *loop)
{
    spv_loop_get(ctx, loop);
    ctx->spirv.loop_stack_idx--;
} // spv_loop_pop

static uint32 spv_loop_get_aL(Context *ctx)
{
    int i;

    // Find the first enclosing loop..endloop. There may be rep..endrep nested inside, so it might
    // not be at the top of the stack.
    for (i = ctx->spirv.loop_stack_idx - 1; i >= 0; i--)
    {
        uint32 id_aL = ctx->spirv.loop_stack[i].id_aL;
        if (id_aL)
            return id_aL;
    } // for

    assert(!"Referencing loop counter register aL in code not part of loop..endloop region.");
    return 0;
} // spv_loop_get_aL

static SpvOp spv_get_comparison(Context *ctx)
{
    static const SpvOp spv_cmp_ops[] = {
        SpvOpUndef,
        SpvOpFOrdGreaterThan,
        SpvOpFOrdEqual,
        SpvOpFOrdGreaterThanEqual,
        SpvOpFOrdLessThan,
        SpvOpFOrdNotEqual,
        SpvOpFOrdLessThanEqual,
    };

    if (ctx->instruction_controls >= STATICARRAYLEN(spv_cmp_ops))
    {
        fail(ctx, "unknown comparison control");
        return SpvOpUndef;
    } // if

    return spv_cmp_ops[ctx->instruction_controls];
} // spv_get_comparison

static void spv_check_read_reg_id(Context *ctx, RegisterList *r)
{
    if (r->spirv.iddecl == 0)
    {
        assert(r->regtype != REG_TYPE_SAMPLER || (shader_is_pixel(ctx) && !shader_version_atleast(ctx, 1, 4)));
        assert(r->regtype != REG_TYPE_TEXTURE || (shader_is_pixel(ctx) && !shader_version_atleast(ctx, 1, 4)));
        switch (r->regtype)
        {
            case REG_TYPE_SAMPLER: // s# (only ps_1_1)
            case REG_TYPE_TEXTURE: // t# (only ps_1_1)
            case REG_TYPE_INPUT: // v#
            case REG_TYPE_TEMP: // r#
            case REG_TYPE_CONST: // c#
            case REG_TYPE_CONSTINT: // i#
            case REG_TYPE_CONSTBOOL: // b#
            case REG_TYPE_LABEL: // l#
            case REG_TYPE_PREDICATE: // p0
                r->spirv.iddecl = spv_bumpid(ctx);
                break;

            case REG_TYPE_LOOP: // aL
                r->spirv.iddecl = spv_loop_get_aL(ctx);
                break;

            default:
            {
                char varname[64];
                get_SPIRV_varname_in_buf(ctx, r->regtype, r->regnum, varname, sizeof(varname));
                failf(ctx, "register type %s is unimplemented\n", varname);
                break;
            } // default
        } // switch
    } // if
} // spv_check_read_reg_id

static void spv_check_write_reg_id(Context *ctx, RegisterList *r)
{
    if (r->spirv.iddecl == 0)
    {
        switch (r->regtype)
        {
            // These registers require no declarations, so we can just create them as we see them
            case REG_TYPE_ADDRESS:
            case REG_TYPE_TEMP:
            case REG_TYPE_RASTOUT:
            case REG_TYPE_COLOROUT:
            case REG_TYPE_TEXCRDOUT:
            case REG_TYPE_DEPTHOUT:
            case REG_TYPE_ATTROUT:
            case REG_TYPE_PREDICATE:
                r->spirv.iddecl = spv_bumpid(ctx);
                break;

            // Other register types should be explicitly declared, so it is an error for them to have iddecl == 0 by now
            default:
            {
                char varname[64];
                get_SPIRV_varname_in_buf(ctx, r->regtype, r->regnum, varname, sizeof(varname));
                failf(ctx, "tried to write to undeclared register %s\n", varname);
                break;
            } // default
        } // switch
    } // if
} // spv_check_write_reg_id

static uint32 spv_ptrimage_from_texturetype(Context *ctx, TextureType ttype)
{
    switch (ttype)
    {
        case TEXTURE_TYPE_2D:
            return spv_get_type(ctx, STI_PTR_IMAGE2D);
        case TEXTURE_TYPE_CUBE:
            return spv_get_type(ctx, STI_PTR_IMAGECUBE);
        case TEXTURE_TYPE_VOLUME:
            return spv_get_type(ctx, STI_PTR_IMAGE3D);
        default:
            fail(ctx, "BUG: used a sampler we don't know how to define.");
            return 0;
    } // switch
} // spv_ptrimage_from_texturetype

static uint32 spv_image_from_texturetype(Context *ctx, TextureType ttype)
{
    switch (ttype)
    {
        case TEXTURE_TYPE_2D:
            return spv_get_type(ctx, STI_IMAGE2D);
        case TEXTURE_TYPE_CUBE:
            return spv_get_type(ctx, STI_IMAGECUBE);
        case TEXTURE_TYPE_VOLUME:
            return spv_get_type(ctx, STI_IMAGE3D);
        default:
            fail(ctx, "BUG: used a sampler we don't know how to define.");
            return 0;
    } // switch
} // spv_ptrimage_from_texturetype

static uint32 spv_access_uniform(Context *ctx, SpirvTypeIdx sti_ptr, RegisterType regtype, uint32 id_offset)
{
    uint32 tid_ptr = spv_get_type(ctx, sti_ptr);
    uint32 id_arr = spv_get_uniform_array_id(ctx, regtype);
    uint32 id_access = spv_bumpid(ctx);
    push_output(ctx, &ctx->mainline);
    if (ctx->spirv.mode == SPIRV_MODE_VK)
    {
        uint32 id_uniform_block = ctx->spirv.id_uniform_block;
        if (id_uniform_block == 0)
        {
            id_uniform_block = spv_bumpid(ctx);
            ctx->spirv.id_uniform_block = id_uniform_block;
        } // if
        spv_emit(ctx, 4+2, SpvOpAccessChain, tid_ptr, id_access, id_uniform_block, id_arr, id_offset);
    } // if
    else
    {
        spv_emit(ctx, 4+1, SpvOpAccessChain, tid_ptr, id_access, id_arr, id_offset);
    } // else
    pop_output(ctx);
    return id_access;
} // spv_access_uniform

static void spv_add_attrib_fixup(Context *ctx, RegisterList *r, unsigned int type_offset, unsigned int opcode_offset)
{
    uint32* next_types;
    uint32* next_opcodes;

    assert(r != NULL);
    #define TYPE_LOAD_OFFSET ctx->spirv.patch_table.attrib_type_load_offsets[r->usage][r->index]

    next_types = (uint32*) Malloc(ctx, sizeof(uint32) * (TYPE_LOAD_OFFSET.num_loads + 1));
    next_opcodes = (uint32*) Malloc(ctx, sizeof(uint32) * (TYPE_LOAD_OFFSET.num_loads + 1));

    memcpy(next_types, TYPE_LOAD_OFFSET.load_types, sizeof(uint32) * TYPE_LOAD_OFFSET.num_loads);
    memcpy(next_opcodes, TYPE_LOAD_OFFSET.load_opcodes, sizeof(uint32) * TYPE_LOAD_OFFSET.num_loads);

    Free(ctx, TYPE_LOAD_OFFSET.load_types);
    Free(ctx, TYPE_LOAD_OFFSET.load_opcodes);

    TYPE_LOAD_OFFSET.load_types = next_types;
    TYPE_LOAD_OFFSET.load_opcodes = next_opcodes;

    TYPE_LOAD_OFFSET.load_types[TYPE_LOAD_OFFSET.num_loads] = type_offset;
    TYPE_LOAD_OFFSET.load_opcodes[TYPE_LOAD_OFFSET.num_loads] = opcode_offset;

    TYPE_LOAD_OFFSET.num_loads += 1;
    #undef TYPE_LOAD_OFFSET
} // spv_add_attrib_fixup

static SpirvResult spv_loadreg(Context *ctx, RegisterList *r)
{
    const RegisterType regtype = r->regtype;
    uint32 copy_id;

    spv_check_read_reg_id(ctx, r);

    uint32 id_src = r->spirv.iddecl;
    SpirvResult result;
    if (regtype == REG_TYPE_SAMPLER)
    {
        RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, r->regnum);
        result.tid = spv_image_from_texturetype(ctx, (TextureType)sreg->index);
    } // if
    else if (regtype == REG_TYPE_CONSTBOOL)
    {
        if (!r->spirv.is_ssa)
            id_src = spv_access_uniform(ctx, STI_PTR_INT_U, regtype, r->spirv.iddecl);

        result.tid = spv_get_type(ctx, STI_INT);
    } // else if
    else if (regtype == REG_TYPE_CONSTINT)
    {
        if (!r->spirv.is_ssa)
            id_src = spv_access_uniform(ctx, STI_PTR_IVEC4_U, regtype, r->spirv.iddecl);

        result.tid = spv_get_type(ctx, STI_IVEC4);
    } // else if
    else if (regtype == REG_TYPE_CONST)
    {
        if (!r->spirv.is_ssa)
            id_src = spv_access_uniform(ctx, STI_PTR_VEC4_U, regtype, r->spirv.iddecl);

        result.tid = spv_get_type(ctx, STI_VEC4);
    } // else if
    else if (regtype == REG_TYPE_LOOP)
        result.tid = spv_get_type(ctx, STI_INT);
    else if (regtype == REG_TYPE_PREDICATE)
        result.tid = spv_get_type(ctx, STI_BVEC4);
    else
        result.tid = spv_get_type(ctx, STI_VEC4);

    // Constants can be used directly, no need to load them.
    assert(r->spirv.is_ssa == 0 || r->spirv.is_ssa == 1);
    if (r->spirv.is_ssa)
    {
        result.id = r->spirv.iddecl;
        return result;
    } // if

    assert(id_src);
    result.id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 4, SpvOpLoad, result.tid, result.id, id_src);
    if (shader_is_vertex(ctx) && regtype == REG_TYPE_INPUT)
    {
        copy_id = spv_bumpid(ctx);
        spv_emit(ctx, 4, SpvOpCopyObject, result.tid, copy_id, result.id);
        result.id = copy_id;
        pop_output(ctx);

        // Store the offsets of:
        // - OpLoad's type id, to change the input type
        // - OpCopyObject's opcode, to change to OpConvert if needed
        spv_add_attrib_fixup(ctx,
                             reglist_find(&ctx->attributes, r->regtype, r->regnum),
                             (buffer_size(ctx->mainline) >> 2) - 7,
                             (buffer_size(ctx->mainline) >> 2) - 4);
    } // if
    else
    {
        // Nothing left to do for this register
        pop_output(ctx);
    } // else

    return result;
} // spv_loadreg

static uint32 spv_emit_swizzle(Context *ctx, uint32 arg, uint32 rtid, const int swizzle, const int writemask)
{
    uint32 result = spv_bumpid(ctx);

    const int writemask0 = (writemask >> 0) & 0x1;
    const int writemask1 = (writemask >> 1) & 0x1;
    const int writemask2 = (writemask >> 2) & 0x1;
    const int writemask3 = (writemask >> 3) & 0x1;

    const uint32 swizzle_x = (swizzle >> 0) & 0x3;
    const uint32 swizzle_y = (swizzle >> 2) & 0x3;
    const uint32 swizzle_z = (swizzle >> 4) & 0x3;
    const uint32 swizzle_w = (swizzle >> 6) & 0x3;

    push_output(ctx, &ctx->mainline);
    // OpVectorShuffle takes two vectors to shuffle, but to do a swizzle
    // operation we can just ignore the second argument (meaning it can be
    // anything, and I am just making it `arg` for convenience)
    uint32 word_count = 5 + writemask0 + writemask1 + writemask2 + writemask3;
    spv_emit_part(ctx, word_count, 5, SpvOpVectorShuffle, rtid, result, arg, arg);
    if (writemask0) spv_emit_word(ctx, swizzle_x);
    if (writemask1) spv_emit_word(ctx, swizzle_y);
    if (writemask2) spv_emit_word(ctx, swizzle_z);
    if (writemask3) spv_emit_word(ctx, swizzle_w);
    pop_output(ctx);

    return result;
} // spv_emit_swizzle

SpirvResult spv_swizzle(Context *ctx, SpirvResult arg, const int swizzle, const int writemask)
{
    int i;

    // Nothing to do, so return the same SSA value
    if (no_swizzle(swizzle) && writemask_xyzw(writemask))
        return arg;

    assert(arg.tid != 0);
    assert(writemask == 1
        || writemask == 3
        || writemask == 7
        || writemask == 15
    );

    SpirvTypeIdx sti_arg = STI_VOID;
    for (i = STI_CORE_START_; i < STI_CORE_END_; i++)
    {
        if (ctx->spirv.tid[i] == arg.tid)
        {
            sti_arg = (SpirvTypeIdx)i;
            break;
        } // if
    } // for
    assert(sti_arg != STI_VOID);

    // We should not leave any value undefined, as it may end up used (eg. dot
    // product), which will make everything relying on it's result undefined.
    // Therefore, we specifically determine true dimensionality of the result.
    int resdim = 0;
    switch (writemask)
    {
        case 1:
            resdim = 1;
            break;

        case 3:
            resdim = 2;
            break;

        case 7:
            resdim = 3;
            break;

        case 15:
            resdim = 4;
            break;

        default:
            failf(ctx, "Unexpected write mask in swizzle: 0x%X");
            assert(0);
            break;
    } // switch

    SpirvTypeIdx sti_result = spv_change_base_type_vec_dim(sti_arg, resdim);

    SpirvResult result = {0};
    result.id = (resdim != 1 || sti_arg != sti_result) ? spv_bumpid(ctx) : arg.id;
    result.tid = spv_get_type(ctx, sti_result);
    assert(result.tid != 0);

    push_output(ctx, &ctx->mainline);
    if (resdim != 1)
    {
        // OpVectorShuffle takes two vectors to shuffle, but to do a swizzle
        // operation we can just ignore the second argument (meaning it can be
        // anything, and I am just making it `arg` for convenience)
        spv_emit_part(ctx, 5 + resdim, 5, SpvOpVectorShuffle, result.tid, result.id, arg.id, arg.id);

        for (i = 0; i < resdim; i++)
            spv_emit_word(ctx, (swizzle >> (2*i)) & 0x3);
    } // if
    else if (sti_arg != sti_result)
    {
        // OpVectorShuffle may not produce a scalar. Instead we use OpCompositeExtract.
        spv_emit(ctx, 5, SpvOpCompositeExtract, result.tid, result.id, arg.id, swizzle & 0x3);
    } // else if

    pop_output(ctx);

    return result;
} // make_GLSL_swizzle_string

static SpirvResult spv_load_srcarg(Context *ctx, const size_t idx, const int writemask)
{
    SpirvResult result = {0};
    if (idx >= STATICARRAYLEN(ctx->source_args))
    {
        fail(ctx, "Too many source args");
        return result;
    } // if

    const SourceArgInfo *arg = &ctx->source_args[idx];

    RegisterList *reg = spv_getreg(ctx, arg->regtype, arg->regnum);

    if (arg->relative)
    {
        if (arg->regtype == REG_TYPE_INPUT)
            fail(ctx, "relative input array access is unimplemented");
        else
        {
            assert(arg->regtype == REG_TYPE_CONST);
            const int arrayidx = arg->relative_array->index;
            const int offset = arg->regnum - arrayidx;
            assert(offset >= 0);

            int is_constant = (arg->relative_array->constant != NULL);
            uint32 id_array = 0;
            if (is_constant)
            {
                id_array = ctx->spirv.constant_arrays.idvec4;
                if (id_array == 0)
                {
                    id_array = spv_bumpid(ctx);
                    ctx->spirv.constant_arrays.idvec4 = id_array;
                } // if
            } // if

            RegisterList *reg_rel = spv_getreg(ctx, arg->relative_regtype, arg->relative_regnum);

            spv_check_read_reg_id(ctx, reg_rel);
            spv_check_read_reg_id(ctx, reg);

            uint32 id_int = spv_get_type(ctx, STI_INT);
            uint32 id_offset;
            if (reg_rel->regtype == REG_TYPE_LOOP)
                id_offset = reg_rel->spirv.iddecl;
            else
            {
                uint32 id_pint = spv_get_type(ctx, STI_PTR_INT_P);
                uint32 id_compidx = spv_getscalari(ctx, arg->relative_component);
                uint32 id_pcomp = spv_bumpid(ctx);
                spv_emit(ctx, 5, SpvOpAccessChain, id_pint, id_pcomp, reg_rel->spirv.iddecl, id_compidx);

                id_offset = spv_bumpid(ctx);
                spv_emit(ctx, 4, SpvOpLoad, id_int, id_offset, id_pcomp);
            } // else

            if (!is_constant)
            {
                uint32 id_arraybase = reg->spirv.iddecl;
                uint32 id_a = id_offset;
                uint32 id_b = id_arraybase;
                id_offset = spv_bumpid(ctx);
                spv_emit(ctx, 5, SpvOpIAdd, id_int, id_offset, id_a, id_b);
            } // if

            if (offset)
            {
                uint32 id_a = id_offset;
                uint32 id_b = spv_getscalari(ctx, offset);
                id_offset = spv_bumpid(ctx);
                spv_emit(ctx, 5, SpvOpIAdd, id_int, id_offset, id_a, id_b);
            } // if

            uint32 id_pvalue;
            if (is_constant)
            {
                uint32 id_pvec4 = spv_get_type(ctx, STI_PTR_VEC4_P);
                id_pvalue = spv_bumpid(ctx);
                spv_emit(ctx, 4+1, SpvOpAccessChain, id_pvec4, id_pvalue, id_array, id_offset);
            } // if
            else
            {
                id_pvalue = spv_access_uniform(ctx, STI_PTR_VEC4_U, arg->regtype, id_offset);
            } // else

            result.tid = spv_get_type(ctx, STI_VEC4);
            result.id = spv_bumpid(ctx);
            spv_emit(ctx, 4, SpvOpLoad, result.tid, result.id, id_pvalue);
        } // else
    } // if
    else
        result = spv_loadreg(ctx, reg);

    result = spv_swizzle(ctx, result, arg->swizzle, writemask);

    switch (arg->src_mod)
    {
        case SRCMOD_NEGATE:
        {
            uint32 id_neg = spv_bumpid(ctx);
            spv_emit(ctx, 4, SpvOpFNegate, result.tid, id_neg, result.id);
            result.id = id_neg;
            break;
        } // case

        case SRCMOD_BIASNEGATE:
        {
            uint32 id_half = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_0_5, 0.5f);
            uint32 id_tmp  = spv_bumpid(ctx);
            uint32 id_new  = spv_bumpid(ctx);
            spv_emit(ctx, 5, SpvOpFSub, result.tid, id_tmp, result.id, id_half);
            spv_emit(ctx, 4, SpvOpFNegate, result.tid, id_new, id_tmp);
            result.id = id_new;
            break;
        } // case

        case SRCMOD_BIAS:
        {
            uint32 id_half = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_0_5, 0.5f);
            uint32 id_new  = spv_bumpid(ctx);
            spv_emit(ctx, 5, SpvOpFSub, result.tid, id_new, result.id, id_half);
            result.id = id_new;
            break;
        } // case

        case SRCMOD_SIGNNEGATE:
        {
            uint32 id_half = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_0_5, 0.5f);
            uint32 id_two  = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_2_0, 2.0f);
            uint32 id_tmp0 = spv_bumpid(ctx);
            uint32 id_tmp1 = spv_bumpid(ctx);
            uint32 id_new  = spv_bumpid(ctx);
            spv_emit(ctx, 5, SpvOpFSub, result.tid, id_tmp0, result.id, id_half);
            spv_emit(ctx, 5, SpvOpFMul, result.tid, id_tmp1, id_tmp0, id_two);
            spv_emit(ctx, 4, SpvOpFNegate, result.tid, id_new, id_tmp1);
            result.id = id_new;
            break;
        } // case

        case SRCMOD_SIGN:
        {
            uint32 id_half = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_0_5, 0.5f);
            uint32 id_two  = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_2_0, 2.0f);
            uint32 id_tmp  = spv_bumpid(ctx);
            uint32 id_new  = spv_bumpid(ctx);
            spv_emit(ctx, 5, SpvOpFSub, result.tid, id_tmp, result.id, id_half);
            spv_emit(ctx, 5, SpvOpFMul, result.tid, id_new, id_tmp, id_two);
            result.id = id_new;
            break;
        } // case

        case SRCMOD_COMPLEMENT:
        {
            uint32 id_one = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_1_0, 1.0f);
            uint32 id_new = spv_bumpid(ctx);
            spv_emit(ctx, 5, SpvOpFSub, result.tid, id_new, id_one, result.id);
            result.id = id_new;
            break;
        } // case

        case SRCMOD_X2NEGATE:
        {
            uint32 id_two = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_2_0, 2.0f);
            uint32 id_tmp = spv_bumpid(ctx);
            uint32 id_new = spv_bumpid(ctx);
            spv_emit(ctx, 5, SpvOpFMul, result.tid, id_tmp, result.id, id_two);
            spv_emit(ctx, 4, SpvOpFNegate, result.tid, id_new, id_tmp);
            result.id = id_new;
            break;
        } // case

        case SRCMOD_X2:
        {
            uint32 id_two = spv_get_constant_composite(ctx, result.tid, ctx->spirv.id_2_0, 2.0f);
            uint32 id_new = spv_bumpid(ctx);
            spv_emit(ctx, 5, SpvOpFMul, result.tid, id_new, result.id, id_two);
            result.id = id_new;
            break;
        } // case

        // case SRCMOD_DZ:
        //     fail(ctx, "SRCMOD_DZ unsupported"); return buf; // !!! FIXME
        //     postmod_str = "_dz";
        //     break;

        // case SRCMOD_DW:
        //     fail(ctx, "SRCMOD_DW unsupported"); return buf; // !!! FIXME
        //     postmod_str = "_dw";
        //     break;

        case SRCMOD_ABSNEGATE:
        {
            uint32 id_abs = spv_bumpid(ctx);
            uint32 id_neg = spv_bumpid(ctx);
            spv_emit(ctx, 5 + 1, SpvOpExtInst, result.tid, id_abs, spv_getext(ctx), GLSLstd450FAbs, result.id);
            spv_emit(ctx, 4, SpvOpFNegate, result.tid, id_neg, id_abs);
            result.id = id_neg;
            break;
        } // case

        case SRCMOD_ABS:
        {
            uint32 id_abs = spv_bumpid(ctx);
            spv_emit(ctx, 5 + 1, SpvOpExtInst, result.tid, id_abs, spv_getext(ctx), GLSLstd450FAbs, result.id);
            result.id = id_abs;
            break;
        } // case

        case SRCMOD_NOT:
        {
            // We can't do OpLogicalNot on ints, so do (x ^ 1) instead
            uint32 id_one = spv_getscalari(ctx, 1);
            uint32 id_not = spv_bumpid(ctx);
            spv_emit(ctx, 5, SpvOpBitwiseXor, result.tid, id_not, result.id, id_one);
            result.id = id_not;
            break;
        } // case

        case SRCMOD_NONE:
        case SRCMOD_TOTAL:
            break;  // stop compiler whining.

        default:
            failf(ctx, "unsupported source modifier %d", arg->src_mod);
            return result;
    } // switch

    return result;
} // spv_load_srcarg

static inline SpirvResult spv_load_srcarg_full(Context *ctx, const size_t idx)
{
    return spv_load_srcarg(ctx, idx, 0xF);
} // spv_load_srcarg_full

static void spv_assign_destarg(Context *ctx, SpirvResult value)
{
    const DestArgInfo *arg = &ctx->dest_arg;
    RegisterList *reg = spv_getreg(ctx, arg->regtype, arg->regnum);

    spv_check_write_reg_id(ctx, reg);

    if (arg->writemask == 0)
    {
        // Return without updating the reg->spirv.iddecl (all-zero writemask = no-op)
        return;
    } // if

    if (arg->result_mod & MOD_SATURATE)
    {
        uint32 ext, zero, one, new_value;

        // Don't inline these, compilers will run the varargs in different orders
        new_value = spv_bumpid(ctx);
        one = spv_get_one(ctx, value.tid);
        zero = spv_get_zero(ctx, value.tid);
        ext = spv_getext(ctx);

        push_output(ctx, &ctx->mainline);
        spv_emit(ctx, 5 + 3, SpvOpExtInst,
            value.tid, new_value, ext, GLSLstd450FClamp, value.id, zero, one
        );
        pop_output(ctx);
        value.id = new_value;
    } // if

    // MSDN says MOD_PP is a hint and many implementations ignore it. So do we.

    // CENTROID only allowed in DCL opcodes, which shouldn't come through here.
    assert((arg->result_mod & MOD_CENTROID) == 0);

    if (ctx->predicated)
    {
        fail(ctx, "predicated destinations unsupported");  // !!! FIXME
        return;
    } // if

    if (arg->result_shift)
    {
        float factor = 1.0f;
        uint32* cache = ctx->spirv.id_1_0;
        switch (arg->result_shift)
        {
            case 0x1: factor = 2.0f;   cache = ctx->spirv.id_2_0;   break;
            case 0x2: factor = 4.0f;   cache = ctx->spirv.id_4_0;   break;
            case 0x3: factor = 8.0f;   cache = ctx->spirv.id_8_0;   break;
            case 0xD: factor = 0.125f; cache = ctx->spirv.id_0_125; break;
            case 0xE: factor = 0.25f;  cache = ctx->spirv.id_0_25;  break;
            case 0xF: factor = 0.5f;   cache = ctx->spirv.id_0_5;   break;
            default:
                failf(ctx, "unexpected result shift %d", arg->result_shift);
        } // switch

        uint32 id_factor = spv_get_constant_composite(ctx, value.tid, cache, factor);
        push_output(ctx, &ctx->mainline);
        uint32 id_new = spv_bumpid(ctx);
        spv_emit(ctx, 5, SpvOpFMul, value.tid, id_new, value.id, id_factor);
        pop_output(ctx);
        value.id = id_new;
    } // if

    if (reg->regtype == REG_TYPE_DEPTHOUT
     || isscalar(ctx, ctx->shader_type, arg->regtype, arg->regnum))
    {
        assert(arg->writemask == 0x1);
        SpirvTypeIdx sti_reg = STI_FLOAT;
        uint32 rtid = spv_get_type(ctx, sti_reg);
        uint32 new_value = spv_bumpid(ctx);
        push_output(ctx, &ctx->mainline);
        spv_emit(ctx, 5, SpvOpCompositeExtract, rtid, new_value, value.id, 0);
        pop_output(ctx);
        value.tid = rtid;
        value.id = new_value;
    } // if
    else if (!writemask_xyzw(arg->writemask))
    {
        SpirvTypeIdx sti_reg;
        switch (reg->regtype)
        {
            case REG_TYPE_ADDRESS: sti_reg = STI_IVEC4; break;
            case REG_TYPE_PREDICATE: sti_reg = STI_BVEC4; break;
            default: sti_reg = STI_VEC4; break;
        } // switch

        uint32 rtid = spv_get_type(ctx, sti_reg);
        uint32 new_value = spv_bumpid(ctx);
        uint32 current_value = spv_bumpid(ctx);

        push_output(ctx, &ctx->mainline);

        spv_emit(ctx, 4, SpvOpLoad, rtid, current_value, reg->spirv.iddecl);

        // output id is new_value
        // select between current value and new value based on writemask
        // in the shuffle, components [0, 3] are the new value, and components
        // [4, 7] are the existing value
        spv_emit_part(ctx, 5 + 4, 5, SpvOpVectorShuffle, rtid, new_value, value.id, current_value);
        if (arg->writemask0) spv_emit_word(ctx, 0); else spv_emit_word(ctx, 4);
        if (arg->writemask1) spv_emit_word(ctx, 1); else spv_emit_word(ctx, 5);
        if (arg->writemask2) spv_emit_word(ctx, 2); else spv_emit_word(ctx, 6);
        if (arg->writemask3) spv_emit_word(ctx, 3); else spv_emit_word(ctx, 7);

        pop_output(ctx);

        value.tid = rtid;
        value.id = new_value;
    } // if

    switch (reg->regtype)
    {
        case REG_TYPE_OUTPUT:
        case REG_TYPE_ADDRESS:
        case REG_TYPE_TEMP:
        case REG_TYPE_DEPTHOUT:
        case REG_TYPE_COLOROUT:
        case REG_TYPE_RASTOUT:
        case REG_TYPE_ATTROUT:
        case REG_TYPE_PREDICATE:
            push_output(ctx, &ctx->mainline);
            spv_emit(ctx, 3, SpvOpStore, reg->spirv.iddecl, value.id);
            pop_output(ctx);
            break;

        default:
        {
            char varname[64];
            get_SPIRV_varname_in_buf(ctx, reg->regtype, reg->regnum, varname, sizeof(varname));
            failf(ctx, "register %s is unimplemented for storing", varname);
            break;
        } // default
    } // switch
} // spv_assign_destarg

static void spv_emit_vs_main_end(Context* ctx)
{
#if SUPPORT_PROFILE_GLSPIRV
#if defined(MOJOSHADER_DEPTH_CLIPPING) || defined(MOJOSHADER_FLIP_RENDERTARGET)
    if (!ctx->profile_supports_glspirv || !shader_is_vertex(ctx))
        return;

    uint32 tid_void = spv_get_type(ctx, STI_VOID);
    uint32 tid_func = spv_get_type(ctx, STI_FUNC_VOID);
    uint32 id_func = ctx->spirv.id_vs_main_end;
    uint32 id_label = spv_bumpid(ctx);
    assert(id_func != 0);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5, SpvOpFunction, tid_void, id_func, SpvFunctionControlMaskNone, tid_func);
    spv_emit(ctx, 2, SpvOpLabel, id_label);

    RegisterList *reg;
    for (reg = ctx->used_registers.next; reg != NULL; reg = reg->next)
    {
        if (reg->usage == MOJOSHADER_USAGE_POSITION &&
            (reg->regtype == REG_TYPE_RASTOUT || reg->regtype == REG_TYPE_OUTPUT))
            break;
    } // for
    SpirvResult output = spv_loadreg(ctx, reg);
    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    uint32 id_new_output;

#ifdef MOJOSHADER_FLIP_RENDERTARGET
    // gl_Position.y = gl_Position.y * vpFlip;
    uint32 tid_pvpflip = spv_bumpid(ctx);
    uint32 id_old_y = spv_bumpid(ctx);
    uint32 id_pvpflip = spv_bumpid(ctx);
    uint32 id_vpflip = spv_bumpid(ctx);
    uint32 id_new_y = spv_bumpid(ctx);
    id_new_output = spv_bumpid(ctx);

    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_old_y, output.id, 1);
    spv_emit(ctx, 4, SpvOpLoad, tid_float, id_vpflip, id_pvpflip);
    spv_emit(ctx, 5, SpvOpFMul, tid_float, id_new_y, id_old_y, id_vpflip);
    spv_emit(ctx, 6, SpvOpCompositeInsert, output.tid, id_new_output, id_new_y, output.id, 1);
    output.id = id_new_output;

    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 4, SpvOpTypePointer, tid_pvpflip, SpvStorageClassUniformConstant, tid_float);
    spv_emit(ctx, 4, SpvOpVariable, tid_pvpflip, id_pvpflip, SpvStorageClassUniformConstant);
    pop_output(ctx);

    spv_output_name(ctx, id_pvpflip, "vpFlip");
    ctx->spirv.patch_table.vpflip.offset = spv_output_location(ctx, id_pvpflip, ~0u);
#endif // MOJOSHADER_FLIP_RENDERTARGET

#ifdef MOJOSHADER_DEPTH_CLIPPING
    // gl_Position.z = gl_Position.z * 2.0 - gl_Position.w;
    uint32 id_2 = spv_getscalarf(ctx, 2.0f);
    uint32 id_old_z = spv_bumpid(ctx);
    uint32 id_old_w = spv_bumpid(ctx);
    uint32 id_2z = spv_bumpid(ctx);
    uint32 id_new_z = spv_bumpid(ctx);
    id_new_output = spv_bumpid(ctx);

    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_old_z, output.id, 2);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_old_w, output.id, 3);
    spv_emit(ctx, 5, SpvOpFMul, tid_float, id_2z, id_old_z, id_2);
    spv_emit(ctx, 5, SpvOpFSub, tid_float, id_new_z, id_2z, id_old_w);
    spv_emit(ctx, 6, SpvOpCompositeInsert, output.tid, id_new_output, id_new_z, output.id, 2);
    output.id = id_new_output;
#endif // MOJOSHADER_DEPTH_CLIPPING

    spv_emit(ctx, 3, SpvOpStore, reg->spirv.iddecl, output.id);
    spv_emit(ctx, 1, SpvOpReturn);
    spv_emit(ctx, 1, SpvOpFunctionEnd);
    pop_output(ctx);

    spv_output_name(ctx, id_func, "vs_epilogue");
#endif // defined(MOJOSHADER_DEPTH_CLIPPING) || defined(MOJOSHADER_FLIP_RENDERTARGET)
#endif // SUPPORT_PROFILE_GLSPIRV
} // spv_emit_vs_main_end

static void spv_emit_func_lit(Context *ctx)
{
    if (!ctx->spirv.id_func_lit)
        return;

    // vec4 LIT(const vec4 src)
    // {
    //     float retval_y, retval_z;
    //     if (src.x > 0.0) {
    //         retval_y = src.x;
    //         if (src.y > 0.0) {
    //             float power = clamp(src.w, -127.9961, 127.9961);
    //             retval_z = pow(src.y, power);
    //         } else {
    //             retval_z = 0.0;
    //         }
    //     } else {
    //         retval_y = 0.0;
    //         retval_z = 0.0;
    //     }
    //     vec4 retval = vec4(1.0, retval_y, retval_z, 1.0);
    //     return retval;
    // }

    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
    uint32 tid_vec4 = spv_get_type(ctx, STI_VEC4);
    uint32 tid_func = spv_get_type(ctx, STI_FUNC_LIT);
    uint32 id_func = ctx->spirv.id_func_lit;
    uint32 id_src = spv_bumpid(ctx);
    uint32 id_block_start = spv_bumpid(ctx);
    uint32 id_src_x = spv_bumpid(ctx);
    uint32 id_src_x_pos = spv_bumpid(ctx);
    uint32 id_0_0 = spv_get_zero(ctx, tid_float);
    uint32 id_branch0_true = spv_bumpid(ctx);
    uint32 id_src_y = spv_bumpid(ctx);
    uint32 id_src_y_pos = spv_bumpid(ctx);
    uint32 id_branch1_true = spv_bumpid(ctx);
    uint32 id_src_w = spv_bumpid(ctx);
    uint32 id_maxp = spv_getscalarf(ctx, 127.9961f);
    uint32 id_maxp_neg = spv_getscalarf(ctx, -127.9961f);
    uint32 id_power = spv_bumpid(ctx);
    uint32 id_pow_result = spv_bumpid(ctx);
    uint32 id_branch1_merge = spv_bumpid(ctx);
    uint32 id_branch1_result = spv_bumpid(ctx);
    uint32 id_branch0_merge = spv_bumpid(ctx);
    uint32 id_result_y = spv_bumpid(ctx);
    uint32 id_result_z = spv_bumpid(ctx);
    uint32 id_1_0 = spv_get_one(ctx, tid_float);
    uint32 id_result = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5, SpvOpFunction, tid_vec4, id_func, SpvFunctionControlMaskNone, tid_func);
    spv_emit(ctx, 3, SpvOpFunctionParameter, tid_vec4, id_src);

    // id_block_start
    spv_emit(ctx, 2, SpvOpLabel, id_block_start);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_src_x, id_src, 0);
    spv_emit(ctx, 5, SpvOpFOrdGreaterThan, tid_bool, id_src_x_pos, id_src_x, id_0_0);
    spv_emit(ctx, 3, SpvOpSelectionMerge, id_branch0_merge, 0);
    spv_emit(ctx, 4, SpvOpBranchConditional, id_src_x_pos, id_branch0_true, id_branch0_merge);

    // id_branch0_true
    spv_emit(ctx, 2, SpvOpLabel, id_branch0_true);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_src_y, id_src, 1);
    spv_emit(ctx, 5, SpvOpFOrdGreaterThan, tid_bool, id_src_y_pos, id_src_y, id_0_0);
    spv_emit(ctx, 3, SpvOpSelectionMerge, id_branch1_merge, 0);
    spv_emit(ctx, 4, SpvOpBranchConditional, id_src_y_pos, id_branch1_true, id_branch1_merge);

    // id_branch1_true
    spv_emit(ctx, 2, SpvOpLabel, id_branch1_true);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_src_w, id_src, 3);
    spv_emit(ctx, 5 + 3, SpvOpExtInst,
        tid_float, id_power, spv_getext(ctx), GLSLstd450FClamp, id_src_w, id_maxp_neg, id_maxp
    );
    spv_emit(ctx, 5 + 2, SpvOpExtInst,
        tid_float, id_pow_result, spv_getext(ctx), GLSLstd450Pow, id_src_y, id_power
    );
    spv_emit(ctx, 2, SpvOpBranch, id_branch1_merge);

    // id_branch1_merge
    spv_emit(ctx, 2, SpvOpLabel, id_branch1_merge);
    spv_emit(ctx, 7, SpvOpPhi, tid_float, id_branch1_result,
        id_pow_result, id_branch1_true,
        id_0_0, id_branch0_true
    );
    spv_emit(ctx, 2, SpvOpBranch, id_branch0_merge);

    // id_branch0_merge
    spv_emit(ctx, 2, SpvOpLabel, id_branch0_merge);
    spv_emit(ctx, 7, SpvOpPhi, tid_float, id_result_y,
        id_src_x, id_branch1_merge,
        id_0_0, id_block_start
    );
    spv_emit(ctx, 7, SpvOpPhi, tid_float, id_result_z,
        id_branch1_result, id_branch1_merge,
        id_0_0, id_block_start
    );
    spv_emit(ctx, 3 + 4, SpvOpCompositeConstruct, tid_vec4, id_result,
        id_1_0, id_result_y, id_result_z, id_1_0
    );
    spv_emit(ctx, 2, SpvOpReturnValue, id_result);
    spv_emit(ctx, 1, SpvOpFunctionEnd);

    pop_output(ctx);

    spv_output_name(ctx, ctx->spirv.id_func_lit, "LIT");
} // spv_emit_func_lit

static void spv_emit_func_end(Context *ctx)
{
    push_output(ctx, &ctx->mainline);

#if SUPPORT_PROFILE_GLSPIRV
#if defined(MOJOSHADER_DEPTH_CLIPPING) || defined(MOJOSHADER_FLIP_RENDERTARGET)
    if (ctx->profile_supports_glspirv
     && shader_is_vertex(ctx)
     && ctx->spirv.id_vs_main_end == 0)
    {
        ctx->spirv.id_vs_main_end = spv_bumpid(ctx);
        uint32 tid_void = spv_get_type(ctx, STI_VOID);
        uint32 id_res = spv_bumpid(ctx);

        push_output(ctx, &ctx->mainline);
        spv_emit(ctx, 4, SpvOpFunctionCall, tid_void, id_res, ctx->spirv.id_vs_main_end);
        pop_output(ctx);
    } // if
#endif // defined(MOJOSHADER_DEPTH_CLIPPING) || defined(MOJOSHADER_FLIP_RENDERTARGET)
#endif // SUPPORT_PROFILE_GLSPIRV

    spv_emit(ctx, 1, SpvOpReturn);
    spv_emit(ctx, 1, SpvOpFunctionEnd);
    pop_output(ctx);
} // spv_emit_func_end

static void spv_emit_vpos_glmode(Context *ctx, uint32 id)
{
    // In SM3.0 vPos only has x and y defined, but we should be
    // fine to leave the z and w attributes in that
    // SpvBuiltInFragCoord gives.

    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    uint32 tid_vec2 = spv_get_type(ctx, STI_VEC2);
    uint32 tid_vec4 = spv_get_type(ctx, STI_VEC4);
    uint32 tid_pvec4i = spv_get_type(ctx, STI_PTR_VEC4_I);
    uint32 tid_pvec2u = spv_bumpid(ctx);
    uint32 tid_pvec4p = spv_get_type(ctx, STI_PTR_VEC4_P);

    uint32 id_var_fragcoord = spv_bumpid(ctx);
    uint32 id_var_vposflip = spv_bumpid(ctx);
    uint32 id_var_vpos = id;

    uint32 id_fragcoord = spv_bumpid(ctx);
    uint32 id_fragcoord_y = spv_bumpid(ctx);
    uint32 id_vposflip = spv_bumpid(ctx);
    uint32 id_vposflip_x = spv_bumpid(ctx);
    uint32 id_vposflip_y = spv_bumpid(ctx);
    uint32 id_tmp = spv_bumpid(ctx);
    uint32 id_vpos_y = spv_bumpid(ctx);
    uint32 id_vpos = spv_bumpid(ctx);

    // vec4 gl_FragCoord = <compiler magic builtin>;
    // uniform vec2 vposFlip;
    // vec4 ps_vPos = vec4(
    //     gl_FragCoord.x,
    //     (gl_FragCoord.y * vposFlip.x) + vposFlip.y,
    //     gl_FragCoord.z,
    //     gl_FragCoord.w
    // );

    push_output(ctx, &ctx->mainline_intro);
    // Define uniform vec2*. This is the only place that uses it right now.
    spv_emit(ctx, 4, SpvOpTypePointer, tid_pvec2u, SpvStorageClassUniformConstant, tid_vec2);
    // Define all variables involved.
    spv_emit(ctx, 4, SpvOpVariable, tid_pvec4i, id_var_fragcoord, SpvStorageClassInput);
    spv_emit(ctx, 4, SpvOpVariable, tid_pvec2u, id_var_vposflip, SpvStorageClassUniformConstant);
    spv_emit(ctx, 4, SpvOpVariable, tid_pvec4p, id_var_vpos, SpvStorageClassPrivate);
    pop_output(ctx);

    spv_output_builtin(ctx, id_var_fragcoord, SpvBuiltInFragCoord);
    spv_output_name(ctx, id_var_vposflip, "vposFlip");

    // Initialize vPos using vPosFlip and built in FragCoord.
    push_output(ctx, &ctx->mainline_top);
    spv_emit(ctx, 4, SpvOpLoad, tid_vec4, id_fragcoord, id_var_fragcoord);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_fragcoord_y, id_fragcoord, 1);
    spv_emit(ctx, 4, SpvOpLoad, tid_vec2, id_vposflip, id_var_vposflip);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_vposflip_x, id_vposflip, 0);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_vposflip_y, id_vposflip, 1);
    spv_emit(ctx, 5, SpvOpFMul, tid_float, id_tmp, id_fragcoord_y, id_vposflip_x);
    spv_emit(ctx, 5, SpvOpFAdd, tid_float, id_vpos_y, id_tmp, id_vposflip_y);
    spv_emit(ctx, 6, SpvOpCompositeInsert, tid_vec4, id_vpos, id_vpos_y, id_fragcoord, 1);
    spv_emit(ctx, 3, SpvOpStore, id_var_vpos, id_vpos);
    pop_output(ctx);

    ctx->spirv.id_var_fragcoord = id_var_fragcoord;
    ctx->spirv.id_var_vpos = id_var_vpos;
    ctx->spirv.patch_table.vpflip.offset = spv_output_location(ctx, id_var_vposflip, ~0u);
} // spv_emit_vpos_glmode

static void spv_emit_vpos_vkmode(Context *ctx, uint32 id)
{
    // In SM3.0 vPos only has x and y defined, but we should be
    // fine to leave the z and w attributes in that
    // SpvBuiltInFragCoord gives.

    uint32 tid_vec4 = spv_get_type(ctx, STI_VEC4);
    uint32 tid_pvec4i = spv_get_type(ctx, STI_PTR_VEC4_I);
    uint32 tid_pvec4p = spv_get_type(ctx, STI_PTR_VEC4_P);

    uint32 id_var_fragcoord = spv_bumpid(ctx);
    uint32 id_var_vpos = id;

    uint32 id_fragcoord = spv_bumpid(ctx);
    uint32 id_vpos = spv_bumpid(ctx);

    // vec4 gl_FragCoord = <compiler magic builtin>;
    // vec4 ps_vPos = gl_FragCoord;

    push_output(ctx, &ctx->mainline_intro);
    // Define all variables involved.
    spv_emit(ctx, 4, SpvOpVariable, tid_pvec4i, id_var_fragcoord, SpvStorageClassInput);
    spv_emit(ctx, 4, SpvOpVariable, tid_pvec4p, id_var_vpos, SpvStorageClassPrivate);
    pop_output(ctx);

    spv_output_builtin(ctx, id_var_fragcoord, SpvBuiltInFragCoord);

    // Initialize vPos using built in FragCoord.
    push_output(ctx, &ctx->mainline_top);
    spv_emit(ctx, 4, SpvOpLoad, tid_vec4, id_fragcoord, id_var_fragcoord);
    spv_emit(ctx, 3, SpvOpStore, id_var_vpos, id_fragcoord);
    pop_output(ctx);

    ctx->spirv.id_var_fragcoord = id_var_fragcoord;
    ctx->spirv.id_var_vpos = id_var_vpos;
} // spv_emit_vpos_vkmode

static void spv_link_vs_attributes(Context *ctx, uint32 id,
                                   MOJOSHADER_usage usage, int index)
{
    if (usage == MOJOSHADER_USAGE_POSITION && index == 0)
        spv_output_builtin(ctx, id, SpvBuiltInPosition);
    else if (usage == MOJOSHADER_USAGE_POINTSIZE && index == 0)
    {
        spv_output_builtin(ctx, id, SpvBuiltInPointSize);
        ctx->spirv.patch_table.attrib_offsets[usage][index] = 1;
    } // else if
    else
        spv_output_attrib_location(ctx, id, usage, index);
} // spv_link_vs_attributes

static void spv_link_ps_attributes(Context *ctx, uint32 id, RegisterType regtype,
                                   MOJOSHADER_usage usage, int index)
{
    switch (regtype)
    {
        case REG_TYPE_COLOROUT:
            // Per KHR_glsl_shader:
            // The fragment-stage built-in gl_FragColor, which implies a broadcast to all
            // outputs, is not present in SPIR-V. Shaders where writing to gl_FragColor
            // is allowed can still write to it, but it only means to write to an output:
            // - of the same type as gl_FragColor
            // - decorated with location 0
            // - not decorated as a built-in variable.
            // There is no implicit broadcast.
            spv_output_color_location(ctx, id, index);
            break;
        case REG_TYPE_INPUT: // v# (MOJOSHADER_USAGE_COLOR aka `oC#` in vertex shader)
            spv_output_attrib_location(ctx, id, usage, index);
            break;
        case REG_TYPE_TEXTURE: // t# (MOJOSHADER_USAGE_TEXCOORD aka `oT#` in vertex shader)
            spv_output_attrib_location(ctx, id, MOJOSHADER_USAGE_TEXCOORD, index);
            break;
        case REG_TYPE_DEPTHOUT:
            spv_output_builtin(ctx, id, SpvBuiltInFragDepth);
            ctx->spirv.hasdepth = 1;
            break;
        case REG_TYPE_MISCTYPE:
            // inputs
            switch ((MiscTypeType)index)
            {
                case MISCTYPE_TYPE_POSITION: // vPos
                {
                    if (ctx->spirv.mode == SPIRV_MODE_GL)
                        spv_emit_vpos_glmode(ctx, id);
                    else
                        spv_emit_vpos_vkmode(ctx, id);
                    break;
                } // case

                case MISCTYPE_TYPE_FACE: // vFace
                {
                    // The much more wordy equivalent of:
                    // bool gl_FrontFacing = <compiler magic builtin>;
                    // vec4 vFace;
                    // vFace = vec4(gl_FrontFacing ? 1.0 : 0.0);

                    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
                    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
                    uint32 tid_vec4 = spv_get_type(ctx, STI_VEC4);
                    uint32 tid_pbooli = spv_get_type(ctx, STI_PTR_BOOL_I);
                    uint32 tid_pvec4p = spv_get_type(ctx, STI_PTR_VEC4_P);

                    uint32 id_1_0 = spv_getscalarf(ctx, 1.0f);
                    uint32 id_n1_0 = spv_getscalarf(ctx, -1.0f);

                    uint32 id_var_frontfacing = spv_bumpid(ctx);
                    uint32 id_var_vface = id;

                    uint32 id_frontfacing = spv_bumpid(ctx);
                    uint32 id_tmp = spv_bumpid(ctx);
                    uint32 id_vface = spv_bumpid(ctx);

                    push_output(ctx, &ctx->mainline_intro);
                    spv_emit(ctx, 4, SpvOpVariable, tid_pbooli, id_var_frontfacing, SpvStorageClassInput);
                    spv_emit(ctx, 4, SpvOpVariable, tid_pvec4p, id_var_vface, SpvStorageClassPrivate);
                    pop_output(ctx);

                    spv_output_builtin(ctx, id_var_frontfacing, SpvBuiltInFrontFacing);

                    push_output(ctx, &ctx->mainline_top);
                    spv_emit(ctx, 4, SpvOpLoad, tid_bool, id_frontfacing, id_var_frontfacing);
                    spv_emit(ctx, 6, SpvOpSelect, tid_float, id_tmp, id_frontfacing, id_1_0, id_n1_0);
                    spv_emit(ctx, 3 + 4, SpvOpCompositeConstruct, tid_vec4, id_vface, id_tmp, id_tmp, id_tmp, id_tmp);
                    spv_emit(ctx, 3, SpvOpStore, id_var_vface, id_vface);
                    pop_output(ctx);

                    ctx->spirv.id_var_frontfacing = id_var_frontfacing;
                    ctx->spirv.id_var_vface = id_var_vface;
                    break;
                } // case
            } // switch
            break;
        default:
            fail(ctx, "unknown pixel shader attribute register");
    } // switch
} // spv_link_ps_attributes

static void spv_texbem(Context* ctx, int luminanceCorrection)
{
    DestArgInfo *info = &ctx->dest_arg;
    uint32 sampler_idx = info->regnum;
    RegisterList *pSReg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, sampler_idx);
    RegisterList *pSrc = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum);
    RegisterList *pDst = spv_getreg(ctx, info->regtype, sampler_idx);

    push_output(ctx, &ctx->mainline);

    SpirvResult sampler = spv_loadreg(ctx, pSReg);
    SpirvResult src0 = spv_loadreg(ctx, pSrc);
    SpirvResult src1 = spv_loadreg(ctx, pDst);

    // <dst> = texture(
    //     <sampler>,
    //     vec2(
    //         (<sampler>_texbem.x * <src0>.x) + (<sampler>_texbem.z * <src0>.y) + <src1>.x,
    //         (<sampler>_texbem.y * <src0>.x) + (<sampler>_texbem.w * <src0>.y) + <src1>.y
    //     )
    // );

    // Load 2x2 transform matrix from uniform data (stored as vec4).
    assert(sampler_idx < 4);
    uint32 id_offset = ctx->spirv.sampler_extras[sampler_idx].idtexbem;
    if (!id_offset)
    {
        id_offset = spv_bumpid(ctx);
        ctx->spirv.sampler_extras[sampler_idx].idtexbem = id_offset;
    } // if
    uint32 tid_vec4 = spv_get_type(ctx, STI_VEC4);
    uint32 id_pmatrix = spv_access_uniform(ctx, STI_PTR_VEC4_U, REG_TYPE_CONST, id_offset);
    SpirvResult matrix;
    matrix.tid = tid_vec4;
    matrix.id = spv_bumpid(ctx);
    spv_emit(ctx, 4, SpvOpLoad, matrix.tid, matrix.id, id_pmatrix);

    // transform src0 using matrix and translate result using src1
    // ie. src0 * matrix + src1
    SpirvResult matrix_xy = spv_swizzle(ctx, matrix, 0x4, 0x3);
    SpirvResult matrix_zw = spv_swizzle(ctx, matrix, 0xE, 0x3);
    SpirvResult src0_xx = spv_swizzle(ctx, src0, 0x0, 0x3);
    SpirvResult src0_yy = spv_swizzle(ctx, src0, 0x5, 0x3);
    SpirvResult src1_xy = spv_swizzle(ctx, src1, 0x4, 0x3);
    uint32 tid_vec2 = src0_xx.tid;
    uint32 id_a = spv_bumpid(ctx);
    uint32 id_b = spv_bumpid(ctx);
    uint32 id_c = spv_bumpid(ctx);
    uint32 id_d = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpFMul, tid_vec2, id_a, matrix_xy.id, src0_xx.id);
    spv_emit(ctx, 5, SpvOpFMul, tid_vec2, id_b, matrix_zw.id, src0_yy.id);
    spv_emit(ctx, 5, SpvOpFAdd, tid_vec2, id_c, id_a, id_b);
    spv_emit(ctx, 5, SpvOpFAdd, tid_vec2, id_d, id_c, src1_xy.id);

    // sample texture
    SpirvResult result;
    result.tid = tid_vec4;
    result.id = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpImageSampleImplicitLod, result.tid, result.id, sampler.id, id_d);
    if (luminanceCorrection)
    {
        uint32 id_l_offset = ctx->spirv.sampler_extras[sampler_idx].idtexbeml;
        if (!id_l_offset)
        {
            id_l_offset = spv_bumpid(ctx);
            ctx->spirv.sampler_extras[sampler_idx].idtexbeml = id_l_offset;
        } // if

        // <dst> = <dst> * ((<src0>.z * <sampler>_texbeml.x) + <sampler>_texbeml.y)
        uint32 tid_float = spv_get_type(ctx, STI_FLOAT);

        SpirvResult src0_z = spv_swizzle(ctx, src0, 0x2, 0x1);
        uint32 id_l_ptr = spv_access_uniform(ctx, STI_PTR_VEC4_U, REG_TYPE_CONST, id_l_offset);

        SpirvResult l;
        l.tid = tid_vec4;
        l.id = spv_bumpid(ctx);

        spv_emit(ctx, 4, SpvOpLoad, l.tid, l.id, id_l_ptr);

        SpirvResult l_x = spv_swizzle(ctx, l, 0x0, 0x1);
        SpirvResult l_y = spv_swizzle(ctx, l, 0x1, 0x1);
        assert(tid_float == l_x.tid);
        assert(tid_float == l_y.tid);
        assert(tid_float == src0_z.tid);

        uint32 id_e = spv_bumpid(ctx);
        uint32 id_f = spv_bumpid(ctx);
        uint32 id_ffff = spv_bumpid(ctx);
        uint32 id_new = spv_bumpid(ctx);
        spv_emit(ctx, 5, SpvOpFMul, tid_float, id_e, src0_z.id, l_x.id);
        spv_emit(ctx, 5, SpvOpFAdd, tid_float, id_f, id_e, l_y.id);
        spv_emit(ctx, 3 + 4, SpvOpCompositeConstruct, tid_vec4, id_ffff,
            id_f, id_f, id_f, id_f
        );
        spv_emit(ctx, 5, SpvOpFMul, tid_vec4, id_new, result.id, id_ffff);
        result.id = id_new;
    } // if

    pop_output(ctx);

    spv_assign_destarg(ctx, result);
}

void emit_SPIRV_start(Context *ctx, const char *profilestr)
{
    if (!(shader_is_vertex(ctx) || shader_is_pixel(ctx)))
    {
        failf(ctx, "Shader type %u unsupported in this profile.",
              (uint) ctx->shader_type);
        return;
    } // if

    memset(&(ctx->spirv), '\0', sizeof(ctx->spirv));

#if SUPPORT_PROFILE_GLSPIRV
    if (strcmp(profilestr, MOJOSHADER_PROFILE_GLSPIRV) == 0)
    {
        ctx->profile_supports_glspirv = 1;
        ctx->spirv.mode = SPIRV_MODE_GL;
    } // if
    else
#endif // SUPPORT_PROFILE_GLSPIRV
    {
        ctx->spirv.mode = SPIRV_MODE_VK;
        if (strcmp(profilestr, MOJOSHADER_PROFILE_SPIRV) != 0)
            failf(ctx, "Profile '%s' unsupported or unknown.", profilestr);
    } // else

    ctx->spirv.idmain = spv_bumpid(ctx);

    // calls spv_getvoid as well
    uint32 tid_void = spv_get_type(ctx, STI_VOID);
    uint32 tid_func = spv_get_type(ctx, STI_FUNC_VOID);

    // slap the function declaration itself in mainline_top, so we can do type
    // declaration in mainline_intro (= before this in the output)
    push_output(ctx, &ctx->mainline_top);
    spv_emit(ctx, 5, SpvOpFunction, tid_void, ctx->spirv.idmain, SpvFunctionControlMaskNone, tid_func);
    spv_emit(ctx, 2, SpvOpLabel, spv_bumpid(ctx));
    pop_output(ctx);

    // also emit the name for the function
    spv_output_name(ctx, ctx->spirv.idmain, ctx->mainfn);

    set_output(ctx, &ctx->mainline);
} // emit_SPIRV_start

void emit_SPIRV_end(Context *ctx)
{
    if (ctx->previous_opcode != OPCODE_RET)
        spv_emit_func_end(ctx);
} // emit_SPIRV_end

void emit_SPIRV_phase(Context *ctx)
{
    // no-op
} // emit_SPIRV_phase

void emit_SPIRV_global(Context *ctx, RegisterType regtype, int regnum)
{
    RegisterList *r = reglist_find(&ctx->used_registers, regtype, regnum);

    SpvStorageClass sc = SpvStorageClassPrivate;
    uint32 tid = 0;
    switch (regtype)
    {
        case REG_TYPE_LABEL:
            failf(ctx, "unimplemented regtype %d", regtype);
            return;

        case REG_TYPE_LOOP:
            // Using SSA id to represent loop counters, instead of a variable.
            return;

        case REG_TYPE_PREDICATE:
            tid = spv_get_type(ctx, STI_PTR_BVEC4_P);
            break;

        case REG_TYPE_ADDRESS:
            if (shader_is_vertex(ctx))
                tid = spv_get_type(ctx, STI_PTR_IVEC4_P);
            else if (shader_is_pixel(ctx)) // actually REG_TYPE_TEXTURE
            {
                if (!shader_version_atleast(ctx, 1, 4))
                {
                    // ps_1_1 texture/address registers work like temporaries. They are initialized
                    // with tex coords and TEX instruction then reads tex coords from it and writes
                    // sampling result back into it. Because Input storage class is read-only, we
                    // create private variable that is initialized to value of input.

                    uint32 tid_pvec4_i = spv_get_type(ctx, STI_PTR_VEC4_I);
                    uint32 tid_pvec4_p = spv_get_type(ctx, STI_PTR_VEC4_P);
                    uint32 tid_vec4 = spv_get_type(ctx, STI_VEC4);
                    uint32 id_input_var = spv_bumpid(ctx);
                    uint32 id_private_var = r->spirv.iddecl;
                    uint32 id_tmp = spv_bumpid(ctx);

                    // Create one Input and one Private variable. Input variable is linked to prev stage.
                    push_output(ctx, &ctx->mainline_intro);
                    spv_emit(ctx, 4, SpvOpVariable, tid_pvec4_i, id_input_var, SpvStorageClassInput);
                    spv_emit(ctx, 4, SpvOpVariable, tid_pvec4_p, id_private_var, SpvStorageClassPrivate);
                    pop_output(ctx);
                    spv_link_ps_attributes(ctx, id_input_var, regtype, MOJOSHADER_USAGE_TEXCOORD, regnum);

                    // Initialize Private variable with Input variable.
                    push_output(ctx, &ctx->mainline_top);
                    spv_emit(ctx, 4, SpvOpLoad, tid_vec4, id_tmp, id_input_var);
                    spv_emit(ctx, 3, SpvOpStore, id_private_var, id_tmp);
                    pop_output(ctx);

                    // TEX instruction have already been emitted that work with Private variable.

                    // Overwrite Private variable with Input variable, so emit_SPIRV_finalize outputs
                    // OpEntryPoint with correct references to Input and Output variables.
                    ctx->spirv.id_implicit_input[regnum] = id_input_var;
                    r->spirv.iddecl = id_input_var;
                    spv_output_regname(ctx, id_input_var, regtype, regnum);
                    return;
                } // if
                tid = spv_get_type(ctx, STI_PTR_VEC4_P);
            } // else if
            break;

        case REG_TYPE_TEMP:
            if (regnum == 0 && shader_is_pixel(ctx) && !shader_version_atleast(ctx, 2, 0))
            {
                // Value of r0 is at the end of shader execution is color output.
                sc = SpvStorageClassOutput;
                tid = spv_get_type(ctx, STI_PTR_VEC4_O);
            }
            else
                tid = spv_get_type(ctx, STI_PTR_VEC4_P);
            break;

        default:
            fail(ctx, "BUG: Unexpected regtype in emit_SPIRV_global");
            return;
    } // switch

    // TODO: If the SSA id for this register is still 0 by this point, that
    // means no instructions actually loaded from/stored to this variable...

    if (r->spirv.iddecl == 0)
        r->spirv.iddecl = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 4, SpvOpVariable, tid, r->spirv.iddecl, sc);
    pop_output(ctx);

    spv_output_regname(ctx, r->spirv.iddecl, regtype, regnum);
} // emit_SPIRV_global

void emit_SPIRV_array(Context *ctx, VariableList *var)
{
    var->emit_position = ctx->uniform_float4_count;
} // emit_SPIRV_array

void emit_SPIRV_const_array(Context *ctx,
                            const struct ConstantsList *clist,
                            int base, int size)
{
    int i;

    assert(ctx->spirv.constant_arrays.idvec4 != 0);

    push_output(ctx, &ctx->mainline_intro);

    // FIXME: This code potentially duplicates constants defined using DEF ops.
    // FIXME: Multiple constant arrays probably won't work. Are those even possible?
    // Maybe it would be better to do this in emit_SPIRV_finalize and use used_registers for it?
    uint32 *constituents = (uint32 *)Malloc(ctx, size * sizeof(uint32));
    uint32 tid_constituent = spv_get_type(ctx, STI_VEC4);
    for (i = 0; i < size; i++)
    {
        while (clist->constant.type != MOJOSHADER_UNIFORM_FLOAT)
            clist = clist->next;
        assert(clist->constant.index == (base + i));

        uint32 id_x = spv_getscalarf(ctx, clist->constant.value.f[0]);
        uint32 id_y = spv_getscalarf(ctx, clist->constant.value.f[1]);
        uint32 id_z = spv_getscalarf(ctx, clist->constant.value.f[2]);
        uint32 id_w = spv_getscalarf(ctx, clist->constant.value.f[3]);

        uint32 id = spv_bumpid(ctx);
        spv_emit(ctx, 3 + 4, SpvOpConstantComposite, tid_constituent, id, id_x, id_y, id_z, id_w);
        constituents[i] = id;

        clist = clist->next;
    } // for

    uint32 id_array_len = spv_getscalari(ctx, size);

    uint32 tid_array = spv_bumpid(ctx);
    spv_emit(ctx, 4, SpvOpTypeArray, tid_array, tid_constituent, id_array_len);

    uint32 id_array = spv_bumpid(ctx);
    spv_emit_part(ctx, 3+size, 3, SpvOpConstantComposite, tid_array, id_array);
    for (i = 0; i < size; i++)
        spv_emit_word(ctx, constituents[i]);

    uint32 tid_parray = spv_bumpid(ctx);
    spv_emit(ctx, 4, SpvOpTypePointer, tid_parray, SpvStorageClassPrivate, tid_array);

    uint32 id_array_var = ctx->spirv.constant_arrays.idvec4;
    spv_emit(ctx, 5, SpvOpVariable, tid_parray, id_array_var, SpvStorageClassPrivate, id_array);

    Free(ctx, constituents);
    pop_output(ctx);
} // emit_SPIRV_const_array

void emit_SPIRV_uniform(Context *ctx, RegisterType regtype, int regnum,
                        const VariableList *var)
{
    RegisterList *r = reglist_find(&ctx->uniforms, regtype, regnum);

    // TODO: If the SSA id for this register is still 0 by this point, that means no instructions actually
    // loaded from/stored to this variable...

    if (r->spirv.iddecl == 0)
        r->spirv.iddecl = spv_bumpid(ctx);

    if (var == NULL)
    {
        uint32 tid = spv_get_type(ctx, STI_INT);
        int offset = 0;
        switch (regtype)
        {
            case REG_TYPE_CONST:
                offset = ctx->uniform_float4_count;
                break;

            case REG_TYPE_CONSTINT:
                offset = ctx->uniform_int4_count;
                break;

            case REG_TYPE_CONSTBOOL:
                offset = ctx->uniform_bool_count;
                break;

            default:
                fail(ctx, "BUG: used a uniform we don't know how to define.");
                return;
        } // switch

        push_output(ctx, &ctx->mainline_intro);
        spv_emit(ctx, 4, SpvOpConstant, tid, r->spirv.iddecl, offset);
        pop_output(ctx);

        char varname[64];
        get_SPIRV_varname_in_buf(ctx, regtype, regnum, varname, sizeof(varname));
        spv_output_name(ctx, r->spirv.iddecl, varname);
    } // if
    else
    {
        if (var->constant)
            fail(ctx, "const array not implemented");
        else
        {
            // Instructions needed to reference this constant before its value was known, so unique
            // id had to be generated. Unfortunately, this prevents reusing already emitted
            // constants.
            assert(var->emit_position != -1);
            push_output(ctx, &ctx->mainline_intro);
            spv_emit(ctx, 4, SpvOpConstant, spv_get_type(ctx, STI_INT), r->spirv.iddecl, var->emit_position);
            pop_output(ctx);

            char varname[64];
            get_SPIRV_varname_in_buf(ctx, regtype, regnum, varname, sizeof(varname));
            spv_output_name(ctx, r->spirv.iddecl, varname);
        } // else
    } // else
} // emit_SPIRV_uniform

void emit_SPIRV_sampler(Context *ctx, int stage, TextureType ttype, int texbem)
{
    uint32 type = spv_ptrimage_from_texturetype(ctx, ttype);

    RegisterList *sampler_reg;
    // Pre ps_2_0 samplers were not dcl-ed, so we won't find them using spv_getreg().
    if (shader_is_pixel(ctx) && !shader_version_atleast(ctx, 2, 0))
        sampler_reg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, stage);
    else
        sampler_reg = spv_getreg(ctx, REG_TYPE_SAMPLER, stage);

    uint32 result = sampler_reg->spirv.iddecl;

    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 4, SpvOpVariable, type, result, SpvStorageClassUniformConstant);
    if (texbem)  // This sampler used a ps_1_1 TEXBEM opcode?
    {
        uint32 tid_int = spv_get_type(ctx, STI_INT);
        uint32 id_texbem = ctx->spirv.sampler_extras[stage].idtexbem;
        uint32 id_texbeml = ctx->spirv.sampler_extras[stage].idtexbeml;
        const int offset = ctx->uniform_float4_count;
        ctx->uniform_float4_count += 2;
        if (id_texbem)
            spv_emit(ctx, 4, SpvOpConstant, tid_int, id_texbem, offset);
        if (id_texbeml)
            spv_emit(ctx, 4, SpvOpConstant, tid_int, id_texbeml, offset + 1);
    } // if
    pop_output(ctx);

    // hnn: specify uniform location for SPIR-V shaders (required per gl_arb_spirv spec)
    spv_output_sampler_binding(ctx, result, sampler_reg->regnum);

    if (ctx->spirv.mode == SPIRV_MODE_GL)
    {
        assert(sampler_reg->regnum < STATICARRAYLEN(ctx->spirv.patch_table.samplers));
        uint32 location_offset = spv_output_location(ctx, result, ~0u);
        ctx->spirv.patch_table.samplers[sampler_reg->regnum].offset = location_offset;
    }

    spv_output_regname(ctx, result, REG_TYPE_SAMPLER, stage);
} // emit_SPIRV_sampler

void emit_SPIRV_attribute(Context *ctx, RegisterType regtype, int regnum,
                          MOJOSHADER_usage usage, int index, int wmask,
                          int flags)
{
    uint32 tid;
    RegisterList *r = spv_getreg(ctx, regtype, regnum);

    ctx->spirv.inoutcount += 1;

    spv_output_regname(ctx, r->spirv.iddecl, regtype, regnum);

    if (shader_is_vertex(ctx))
    {
        // pre-vs3 output registers.
        // these don't ever happen in DCL opcodes, I think. Map to vs_3_*
        //  output registers.
        if (!shader_version_atleast(ctx, 3, 0))
        {
            if (regtype == REG_TYPE_RASTOUT)
            {
                regtype = REG_TYPE_OUTPUT;
                index = regnum;
                switch ((const RastOutType) regnum)
                {
                    case RASTOUT_TYPE_POSITION:
                        usage = MOJOSHADER_USAGE_POSITION;
                        break;
                    case RASTOUT_TYPE_FOG:
                        usage = MOJOSHADER_USAGE_FOG;
                        break;
                    case RASTOUT_TYPE_POINT_SIZE:
                        usage = MOJOSHADER_USAGE_POINTSIZE;
                        break;
                } // switch
            } // if

            else if (regtype == REG_TYPE_ATTROUT)
            {
                regtype = REG_TYPE_OUTPUT;
                usage = MOJOSHADER_USAGE_COLOR;
                index = regnum;
            } // else if

            else if (regtype == REG_TYPE_TEXCRDOUT)
            {
                regtype = REG_TYPE_OUTPUT;
                usage = MOJOSHADER_USAGE_TEXCOORD;
                index = regnum;
            } // else if
        } // if
        assert(r->usage == MOJOSHADER_USAGE_UNKNOWN);
        r->usage = usage;

        switch (regtype)
        {
            case REG_TYPE_INPUT:
            {
                ctx->spirv.patch_table.tid_vec4_p = spv_get_type(ctx, STI_PTR_VEC4_I);
                ctx->spirv.patch_table.tid_ivec4_p = spv_get_type(ctx, STI_PTR_IVEC4_I);
                ctx->spirv.patch_table.tid_uvec4_p = spv_get_type(ctx, STI_PTR_UVEC4_I);
                ctx->spirv.patch_table.tid_vec4 = spv_get_type(ctx, STI_VEC4);
                ctx->spirv.patch_table.tid_ivec4 = spv_get_type(ctx, STI_IVEC4);
                ctx->spirv.patch_table.tid_uvec4 = spv_get_type(ctx, STI_UVEC4);

                push_output(ctx, &ctx->mainline_intro);
                tid = spv_get_type(ctx, STI_PTR_VEC4_I);
                spv_emit(ctx, 4, SpvOpVariable, tid, r->spirv.iddecl, SpvStorageClassInput);
                pop_output(ctx);

                ctx->spirv.patch_table.attrib_type_offsets[usage][index] = (buffer_size(ctx->mainline_intro) >> 2) - 3;

                // hnn: generate location decorators for the input
                spv_output_location(ctx, r->spirv.iddecl, regnum);
                break;
            }

            case REG_TYPE_OUTPUT:
            {
                push_output(ctx, &ctx->mainline_intro);
                SpirvTypeIdx sti = STI_PTR_VEC4_O;
                if (usage == MOJOSHADER_USAGE_POINTSIZE
                 || usage == MOJOSHADER_USAGE_FOG)
                {
                    sti = STI_PTR_FLOAT_O;
                } // if

                tid = spv_get_type(ctx, sti);
                spv_emit(ctx, 4, SpvOpVariable, tid, r->spirv.iddecl, SpvStorageClassOutput);
                pop_output(ctx);

                spv_link_vs_attributes(ctx, r->spirv.iddecl, usage, index);
                break;
            } // case

            default:
                fail(ctx, "unknown vertex shader attribute register");
        } // switch
    } // if

    else if (shader_is_pixel(ctx))
    {
        // samplers DCLs get handled in emit_SPIRV_sampler().

        if (flags & MOD_CENTROID)  // !!! FIXME
        {
            failf(ctx, "centroid unsupported in %s profile", ctx->profile->name);
            return;
        } // if

        switch (regtype)
        {
            case REG_TYPE_COLOROUT:
                spv_link_ps_attributes(ctx, r->spirv.iddecl, regtype, usage, regnum);
                push_output(ctx, &ctx->mainline_intro);
                tid = spv_get_type(ctx, STI_PTR_VEC4_O);
                spv_emit(ctx, 4, SpvOpVariable, tid, r->spirv.iddecl, SpvStorageClassOutput);
                pop_output(ctx);
                break;
            case REG_TYPE_DEPTHOUT:
                // maps to BuiltIn FragDepth
                spv_link_ps_attributes(ctx, r->spirv.iddecl, regtype, usage, index);
                push_output(ctx, &ctx->mainline_intro);
                tid = spv_get_type(ctx, STI_PTR_FLOAT_O);
                spv_emit(ctx, 4, SpvOpVariable, tid, r->spirv.iddecl, SpvStorageClassOutput);
                pop_output(ctx);
                break;
            case REG_TYPE_MISCTYPE:
                assert((MiscTypeType)regnum == MISCTYPE_TYPE_FACE || (MiscTypeType)regnum == MISCTYPE_TYPE_POSITION);
                // SpvBuiltInFrontFacing is a input bool, and for the DX bytecode
                // we need to map it to a float that's either -1.0 or 1.0.
                // SpvBuiltInFragCoord needs to be modified using vposFlip uniform
                // to match vPos.
                // Both of these take place in spv_link_ps_attributes() so don't
                // create an input variable for it here.
                spv_link_ps_attributes(ctx, r->spirv.iddecl, regtype, usage, regnum);
                break;

            case REG_TYPE_TEXTURE:
            case REG_TYPE_INPUT:
                // ps_1_1 is dealt with in emit_SPIRV_global().
                if (usage != MOJOSHADER_USAGE_TEXCOORD || shader_version_atleast(ctx, 1, 4))
                {
                    if (usage == MOJOSHADER_USAGE_TEXCOORD && index == 0)
                    {
                        // This can be either BuiltInPointCoord (vec2) or normal TEXCOORD0 input (vec4).
                        // To determine correct type, we need to wait until link-time when we can see
                        // vertex shader outputs and then patch in correct types. To avoid having to
                        // fix all loads from the input variable, we never access it directly, but
                        // instead go through private variable that is always vec4.
                        // Here we generate input and private variables and helper code that gets
                        // patched at link-time. See SpirvPatchTable for details on patching.
                        SpirvPatchTable* table = &ctx->spirv.patch_table;

                        uint32 tid_pvec2i = spv_get_type(ctx, STI_PTR_VEC2_I);
                        uint32 tid_pvec4i = spv_get_type(ctx, STI_PTR_VEC4_I);
                        uint32 tid_pvec4p = spv_get_type(ctx, STI_PTR_VEC4_P);
                        uint32 tid_vec2 = spv_get_type(ctx, STI_VEC2);
                        uint32 tid_vec4 = spv_get_type(ctx, STI_VEC4);

                        table->tid_pvec2i = tid_pvec2i;
                        table->tid_vec2 = tid_vec2;
                        table->tid_pvec4i = tid_pvec4i;
                        table->tid_vec4 = tid_vec4;

                        push_output(ctx, &ctx->mainline_intro);
                        ctx->spirv.id_var_texcoord0_private = r->spirv.iddecl;
                        ctx->spirv.id_var_texcoord0_input = spv_bumpid(ctx);
                        table->pointcoord_var_offset = buffer_size(ctx->mainline_intro) >> 2;
                        spv_emit(ctx, 4, SpvOpVariable, tid_pvec4i, ctx->spirv.id_var_texcoord0_input, SpvStorageClassInput);
                        spv_emit(ctx, 4, SpvOpVariable, tid_pvec4p, ctx->spirv.id_var_texcoord0_private, SpvStorageClassPrivate);
                        pop_output(ctx);

                        spv_link_ps_attributes(ctx, ctx->spirv.id_var_texcoord0_input, regtype, usage, index);
                        spv_output_name(ctx, ctx->spirv.id_var_texcoord0_input, "ps_PointCoordOrTexCoord0");

                        push_output(ctx, &ctx->mainline_top);
                        uint32 id_loaded = spv_bumpid(ctx);
                        uint32 id_shuffled = spv_bumpid(ctx);
                        table->pointcoord_load_offset = buffer_size(ctx->mainline_top) >> 2;
                        spv_emit(ctx, 4, SpvOpLoad, tid_vec4, id_loaded, ctx->spirv.id_var_texcoord0_input);
                        spv_emit(ctx, 9, SpvOpVectorShuffle, tid_vec4, id_shuffled, id_loaded, id_loaded, 0, 1, 2, 3);
                        spv_emit(ctx, 3, SpvOpStore, ctx->spirv.id_var_texcoord0_private, id_shuffled);
                        pop_output(ctx);
                    } // if
                    else
                    {
                        spv_link_ps_attributes(ctx, r->spirv.iddecl, regtype, usage, index);
                        push_output(ctx, &ctx->mainline_intro);
                        tid = spv_get_type(ctx, STI_PTR_VEC4_I);
                        spv_emit(ctx, 4, SpvOpVariable, tid, r->spirv.iddecl, SpvStorageClassInput);
                        pop_output(ctx);
                    } // else
                } // if
                break;
            default:
                fail(ctx, "unknown pixel shader attribute register");
        } // switch
    } // else if

    else
        fail(ctx, "Unknown shader type");  // state machine should catch this.
} // emit_SPIRV_attribute

static void spv_emit_uniform_constant_array(Context *ctx,
                                            const RegisterType regtype,
                                            const int size, uint32 id_var,
                                            uint32 id_type_base,
                                            uint32* dst_location_offset)
{
    assert(size > 0);
    assert(id_var != 0);
    assert(ctx->spirv.mode == SPIRV_MODE_GL);

    uint32 id_size = spv_getscalari(ctx, size);
    uint32 id_type = spv_bumpid(ctx);
    uint32 id_type_ptr = spv_bumpid(ctx);
    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 4, SpvOpTypeArray, id_type, id_type_base, id_size);
    spv_emit(ctx, 4, SpvOpTypePointer, id_type_ptr, SpvStorageClassUniformConstant, id_type);
    spv_emit(ctx, 4, SpvOpVariable, id_type_ptr, id_var, SpvStorageClassUniformConstant);
    pop_output(ctx);

    char buf[64];
    spv_get_uniform_array_varname(ctx, regtype, buf, sizeof(buf));
    spv_output_name(ctx, id_var, buf);

    *dst_location_offset = spv_output_location(ctx, id_var, ~0u);
} // spv_emit_uniform_constant_array

void emit_SPIRV_finalize(Context *ctx)
{
    size_t i, j, k, max;

    /* The generator's magic number, this could be registered with Khronos
     * if we wanted to. 0 is fine though, so use that for now. */
    uint32 genmagic = 0x00000000;

    /* Vertex shader main() function may need to do some position adjustments. However,
    position may be written in subroutines, so we can't write position adjust code
    at the end of main(), because output register might not be in ctx->used_registers
    yet. Instead, we do adjust in a subroutine generated here and called at the
    end of main(). */
    spv_emit_vs_main_end(ctx);
    spv_emit_func_lit(ctx);

    uint8 emit_vec4 = ctx->uniform_float4_count > 0 && ctx->spirv.uniform_arrays.idvec4;
    uint8 emit_ivec4 = ctx->uniform_int4_count > 0 && ctx->spirv.uniform_arrays.idivec4;
    uint8 emit_bool = ctx->uniform_bool_count > 0 && ctx->spirv.uniform_arrays.idbool;
    uint8 emit_any = emit_vec4 | emit_ivec4 | emit_bool;
    if (ctx->spirv.mode == SPIRV_MODE_GL)
    {
        if (emit_vec4)
            spv_emit_uniform_constant_array(ctx, REG_TYPE_CONST,
                ctx->uniform_float4_count,
                ctx->spirv.uniform_arrays.idvec4,
                spv_get_type(ctx, STI_VEC4),
                &ctx->spirv.patch_table.array_vec4.offset
            );

        if (emit_ivec4)
            spv_emit_uniform_constant_array(ctx, REG_TYPE_CONSTINT,
                ctx->uniform_int4_count,
                ctx->spirv.uniform_arrays.idivec4,
                spv_get_type(ctx, STI_IVEC4),
                &ctx->spirv.patch_table.array_ivec4.offset
            );

        if (emit_bool)
            spv_emit_uniform_constant_array(ctx, REG_TYPE_CONSTBOOL,
                ctx->uniform_bool_count,
                ctx->spirv.uniform_arrays.idbool,
                spv_get_type(ctx, STI_INT),
                &ctx->spirv.patch_table.array_bool.offset
            );
    } // if
    else if (emit_any)
    {
        assert(ctx->spirv.mode == SPIRV_MODE_VK);
        uint32 member_tid[3];
        uint32 member_offset[3];
        uint32 member_count = 0;
        uint32 struct_size = 0;

        uint32 tid_arr_idx = spv_get_type(ctx, STI_INT);

        push_output(ctx, &ctx->mainline_intro);

        if (emit_vec4)
        {
            int size = ctx->uniform_float4_count;
            uint32 id_size = spv_getscalari(ctx, size);
            uint32 tid_type_base = spv_get_type(ctx, STI_VEC4);
            uint32 tid_array = spv_bumpid(ctx);
            spv_emit(ctx, 4, SpvOpTypeArray, tid_array, tid_type_base, id_size);
            uint32 i = member_count++;
            spv_emit(ctx, 4, SpvOpConstant, tid_arr_idx, ctx->spirv.uniform_arrays.idvec4, i);
            member_tid[i] = tid_array;
            member_offset[i] = struct_size;
            struct_size += size * 16;
        } // if

        if (emit_ivec4)
        {
            int size = ctx->uniform_int4_count;
            uint32 id_size = spv_getscalari(ctx, size);
            uint32 tid_type_base = spv_get_type(ctx, STI_IVEC4);
            uint32 tid_array = spv_bumpid(ctx);
            spv_emit(ctx, 4, SpvOpTypeArray, tid_array, tid_type_base, id_size);
            uint32 i = member_count++;
            spv_emit(ctx, 4, SpvOpConstant, tid_arr_idx, ctx->spirv.uniform_arrays.idivec4, i);
            member_tid[i] = tid_array;
            member_offset[i] = struct_size;
            struct_size += size * 16;
        } // if

        if (emit_bool)
        {
            int size = ctx->uniform_bool_count;
            uint32 id_size = spv_getscalari(ctx, size);
            uint32 tid_type_base = spv_get_type(ctx, STI_INT);
            uint32 tid_array = spv_bumpid(ctx);
            spv_emit(ctx, 4, SpvOpTypeArray, tid_array, tid_type_base, id_size);
            uint32 i = member_count++;
            spv_emit(ctx, 4, SpvOpConstant, tid_arr_idx, ctx->spirv.uniform_arrays.idbool, i);
            member_tid[i] = tid_array;
            member_offset[i] = struct_size;
            struct_size += size * 16;
        } // if

        uint32 tid_struct = spv_bumpid(ctx);
        uint32 tid_pstruct = spv_bumpid(ctx);
        uint32 id_pstruct = ctx->spirv.id_uniform_block;
        spv_emit_part(ctx, 2 + member_count, 2, SpvOpTypeStruct, tid_struct);
        for (i = 0; i < member_count; i++)
            spv_emit_word(ctx, member_tid[i]);
        spv_emit(ctx, 4, SpvOpTypePointer, tid_pstruct, SpvStorageClassUniform, tid_struct);
        spv_emit(ctx, 4, SpvOpVariable, tid_pstruct, id_pstruct, SpvStorageClassUniform);

        pop_output(ctx);

        char buf[64];
        snprintf(buf, sizeof(buf), "%s_uniforms", ctx->shader_type_str);
        spv_output_name(ctx, id_pstruct, buf);

        uint32 set = shader_is_vertex(ctx) ? MOJOSHADER_SPIRV_VS_UNIFORM_SET
                                           : MOJOSHADER_SPIRV_PS_UNIFORM_SET;
        push_output(ctx, &ctx->helpers);
        spv_emit(ctx, 3+0, SpvOpDecorate, tid_struct, SpvDecorationBlock);
        spv_emit(ctx, 3+1, SpvOpDecorate, id_pstruct, SpvDecorationDescriptorSet, set);
        spv_emit(ctx, 3+1, SpvOpDecorate, id_pstruct, SpvDecorationBinding, 0);

        for (uint32 i = 0; i < member_count; i++)
        {
            spv_emit(ctx, 3+1, SpvOpDecorate, member_tid[i], SpvDecorationArrayStride, 16);
            spv_emit(ctx, 4+1, SpvOpMemberDecorate, tid_struct, i, SpvDecorationOffset, member_offset[i]);
        } // for

        pop_output(ctx);
    } // else if

    push_output(ctx, &ctx->preflight);

    spv_emit_word(ctx, SpvMagicNumber);
    spv_emit_word(ctx, SpvVersion);
    spv_emit_word(ctx, genmagic);
    // "Bound: where all <id>s in this module are guaranteed to satisfy 0 < id < Bound"
    // `idmax` holds the last id that was given out, so we need to emit `idmax + 1`
    spv_emit_word(ctx, ctx->spirv.idmax + 1);
    spv_emit_word(ctx, 0);

    spv_emit(ctx, 2, SpvOpCapability, SpvCapabilityShader);

    // only non-zero when actually needed
    if (ctx->spirv.idext)
    {
        const char *extstr = "GLSL.std.450";
        spv_emit_part(ctx, 2 + spv_strlen(extstr), 2, SpvOpExtInstImport, ctx->spirv.idext);
        spv_emit_str(ctx, extstr);
    } // if

    spv_emit(ctx, 3, SpvOpMemoryModel, SpvAddressingModelLogical, SpvMemoryModelSimple);

    assert(shader_is_vertex(ctx) || shader_is_pixel(ctx));
    SpvExecutionModel model = SpvExecutionModelVertex;
    if (shader_is_pixel(ctx))
        model = SpvExecutionModelFragment;

    /* 3 is for opcode + exec. model + idmain */
    uint32 inoutcount = ctx->spirv.inoutcount;

    uint32 implicit_input_count = sizeof(ctx->spirv.id_implicit_input) / sizeof(uint32);
    if (shader_is_pixel(ctx))
    {
        if (!shader_version_atleast(ctx, 1, 4))
        {
            for (uint32 i = 0; i < implicit_input_count; i++)
            {
                if (ctx->spirv.id_implicit_input[i])
                    inoutcount += 1;
            } // for
        } // if

        if (!shader_version_atleast(ctx, 2, 0))
            inoutcount += 1;
    } // if

    spv_emit_part(ctx, 3 + spv_strlen(ctx->mainfn) + inoutcount, 3, SpvOpEntryPoint,
        model, ctx->spirv.idmain
    );
    spv_emit_str(ctx, ctx->mainfn);

    RegisterList *p = &ctx->attributes, *r = NULL;
    // !!! FIXME: The first element of the list is always empty and I don't know why!
    p = p->next;
    while (p)
    {
        r = spv_getreg(ctx, p->regtype, p->regnum);
        if (r)
        {
            if (r->spirv.iddecl == ctx->spirv.id_var_vpos)
                spv_emit_word(ctx, ctx->spirv.id_var_fragcoord);
            else if (r->spirv.iddecl == ctx->spirv.id_var_vface)
                spv_emit_word(ctx, ctx->spirv.id_var_frontfacing);
            else if (r->spirv.iddecl == ctx->spirv.id_var_texcoord0_private)
                spv_emit_word(ctx, ctx->spirv.id_var_texcoord0_input);
            else
                spv_emit_word(ctx, r->spirv.iddecl);
        } // if
        else
        {
            char varname[64];
            get_SPIRV_varname_in_buf(ctx, p->regtype, p->regnum, varname, sizeof (varname));
            failf(
                ctx,
                "missing attribute register %s (rt=%u, rn=%u, u=%u)",
                varname, p->regtype, p->regnum, p->usage
            );
        } // else
        p = p->next;
    } // while

    // only applies to pixel shaders
    if (shader_is_pixel(ctx))
    {
        if (!shader_version_atleast(ctx, 1, 4))
        {
            for (uint32 i = 0; i < implicit_input_count; i++)
            {
                uint32 id = ctx->spirv.id_implicit_input[i];
                if (id)
                    spv_emit_word(ctx, id);
            } // for
        } // if

        if (!shader_version_atleast(ctx, 2, 0))
        {
            // r0 is used as color output.
            r = spv_getreg(ctx, REG_TYPE_TEMP, 0);
            spv_emit_word(ctx, r->spirv.iddecl);
        } // if

        // vk semantics = default origin is upper left
        // gl semantics = default origin is lower left
        spv_emit(ctx, 3, SpvOpExecutionMode, ctx->spirv.idmain, SpvExecutionModeOriginUpperLeft);

        // This must be explicitly marked when FragDepth is in use!
        if (ctx->spirv.hasdepth)
            spv_emit(ctx, 3, SpvOpExecutionMode, ctx->spirv.idmain, SpvExecutionModeDepthReplacing);
    } // if

    pop_output(ctx);

    // Generate final patch table.

    uint32 base_offset = 0;
    if (ctx->preflight) base_offset += buffer_size(ctx->preflight);
    if (ctx->globals)   base_offset += buffer_size(ctx->globals);
    if (ctx->inputs)    base_offset += buffer_size(ctx->inputs);
    if (ctx->outputs)   base_offset += buffer_size(ctx->outputs);
    base_offset >>= 2;

    int32 location_count = 0;
    SpirvPatchTable* table = &ctx->spirv.patch_table;
    if (table->vpflip.offset)
    {
        table->vpflip.offset += base_offset;
        table->vpflip.location = location_count;
        location_count += 1;
    } // if
    else
        table->vpflip.location = -1;

    if (table->array_vec4.offset)
    {
        table->array_vec4.offset += base_offset;
        table->array_vec4.location = location_count;
        location_count += ctx->uniform_float4_count;
    } // if
    else
        table->array_vec4.location = -1;

    if (table->array_ivec4.offset)
    {
        table->array_ivec4.offset += base_offset;
        table->array_ivec4.location = location_count;
        location_count += ctx->uniform_int4_count;
    } // if
    else
        table->array_ivec4.location = -1;

    if (table->array_bool.offset)
    {
        table->array_bool.offset += base_offset;
        table->array_bool.location = location_count;
        location_count += ctx->uniform_bool_count;
    } // if
    else
        table->array_bool.location = -1;

    for (i = 0, max = STATICARRAYLEN(table->samplers); i < max; i++)
    {
        SpirvPatchEntry* entry = &table->samplers[i];
        if (entry->offset)
        {
            entry->offset += base_offset;
            entry->location = location_count;
            location_count++;
        } // if
        else
            entry->location = -1;
    } // for

    table->location_count = location_count;

    for (i = 0; i < MOJOSHADER_USAGE_TOTAL; i++)
        for (j = 0; j < 16; j++)
            if (table->attrib_offsets[i][j])
                table->attrib_offsets[i][j] += base_offset;
    for (i = 0; i < 16; i++)
        if (table->output_offsets[i])
            table->output_offsets[i] += base_offset;

    base_offset <<= 2;
    if (ctx->helpers)     base_offset += buffer_size(ctx->helpers);
    if (ctx->subroutines) base_offset += buffer_size(ctx->subroutines);
    base_offset >>= 2;

    if (table->pointcoord_var_offset)
        table->pointcoord_var_offset += base_offset;

    for (i = 0; i < MOJOSHADER_USAGE_TOTAL; i++)
        for (j = 0; j < 16; j++)
            if (table->attrib_type_offsets[i][j])
                table->attrib_type_offsets[i][j] += base_offset;

    base_offset <<= 2;
    if (ctx->mainline_intro)     base_offset += buffer_size(ctx->mainline_intro);
    if (ctx->mainline_arguments) base_offset += buffer_size(ctx->mainline_arguments);
    base_offset >>= 2;

    if (table->pointcoord_load_offset)
        table->pointcoord_load_offset += base_offset;

    base_offset <<= 2;
    if (ctx->mainline_top) base_offset += buffer_size(ctx->mainline_top);
    base_offset >>= 2;

    for (i = 0; i < MOJOSHADER_USAGE_TOTAL; i++)
        for (j = 0; j < 16; j++)
            if (table->attrib_type_offsets[i][j])
                for (k = 0; k < table->attrib_type_load_offsets[i][j].num_loads; k++)
                {
                     table->attrib_type_load_offsets[i][j].load_types[k] += base_offset;
                     table->attrib_type_load_offsets[i][j].load_opcodes[k] += base_offset;
                } // for

    push_output(ctx, &ctx->postflight);
    buffer_append(ctx->output, &ctx->spirv.patch_table, sizeof(ctx->spirv.patch_table));
    pop_output(ctx);

    spv_componentlist_free(ctx, ctx->spirv.cl.f.next);
    spv_componentlist_free(ctx, ctx->spirv.cl.i.next);
    spv_componentlist_free(ctx, ctx->spirv.cl.u.next);
} // emit_SPIRV_finalize

void emit_SPIRV_NOP(Context *ctx)
{
    // no-op is a no-op.  :)
    // TODO: (hnn) SPIR-V has OpNop :O
} // emit_SPIRV_NOP

void emit_SPIRV_DEF(Context *ctx)
{
    RegisterList *rl;
    uint32 val0, val1, val2, val3, idv4;
    const float *raw = (const float *) ctx->dwords;

    rl = spv_getreg(ctx, ctx->dest_arg.regtype, ctx->dest_arg.regnum);
    rl->spirv.iddecl = spv_bumpid(ctx);
    rl->spirv.is_ssa = 1;

    val0 = spv_getscalarf(ctx, raw[0]);
    val1 = spv_getscalarf(ctx, raw[1]);
    val2 = spv_getscalarf(ctx, raw[2]);
    val3 = spv_getscalarf(ctx, raw[3]);

    idv4 = spv_get_type(ctx, STI_VEC4);

    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 3 + 4, SpvOpConstantComposite, idv4, rl->spirv.iddecl, val0, val1, val2, val3);
    pop_output(ctx);
} // emit_SPIRV_DEF

void emit_SPIRV_DEFI(Context *ctx)
{
    RegisterList *rl;
    uint32 val0, val1, val2, val3, idiv4;
    const int *raw = (const int *) ctx->dwords;

    rl = spv_getreg(ctx, ctx->dest_arg.regtype, ctx->dest_arg.regnum);
    rl->spirv.iddecl = spv_bumpid(ctx);
    rl->spirv.is_ssa = 1;

    val0 = spv_getscalari(ctx, raw[0]);
    val1 = spv_getscalari(ctx, raw[1]);
    val2 = spv_getscalari(ctx, raw[2]);
    val3 = spv_getscalari(ctx, raw[3]);

    idiv4 = spv_get_type(ctx, STI_IVEC4);

    push_output(ctx, &ctx->mainline_intro);
    spv_emit(ctx, 3 + 4, SpvOpConstantComposite, idiv4, rl->spirv.iddecl, val0, val1, val2, val3);
    pop_output(ctx);
} // emit_SPIRV_DEFI

void emit_SPIRV_DEFB(Context *ctx)
{
    RegisterList *rl = spv_getreg(ctx, ctx->dest_arg.regtype, ctx->dest_arg.regnum);
    rl->spirv.iddecl = ctx->dwords[0] ? spv_gettrue(ctx) : spv_getfalse(ctx);
    rl->spirv.is_ssa = 1;
} // emit_SPIRV_DEFB

void emit_SPIRV_DCL(Context *ctx)
{
    // state_DCL handles checking if the registers are valid for this
    // instruction, and collecting samplers and attribs
    RegisterList *reg = spv_getreg(ctx, ctx->dest_arg.regtype, ctx->dest_arg.regnum);

    // This id will be assigned to in emit_SPIRV_attribute, but
    // emit_SPIRV_attribute is called after instructions are emitted,
    // so we generate the id here so it can be used in instructions
    reg->spirv.iddecl = spv_bumpid(ctx);
} // emit_SPIRV_DCL

static void emit_SPIRV_dotproduct(Context *ctx, SpirvResult src0, SpirvResult src1)
{
    SpirvResult result;

    assert(src0.tid == src1.tid);

    result.tid = spv_get_type(ctx, STI_FLOAT);
    result.id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5, SpvOpDot, result.tid, result.id, src0.id, src1.id);

    // Broadcast scalar result across all channels of a vec4
    result.tid = spv_get_type(ctx, STI_VEC4);
    result.id = spv_vectorbroadcast(ctx, result.tid, result.id);
    pop_output(ctx);

    spv_assign_destarg(ctx, result);
} // emit_SPIRV_dotproduct

void emit_SPIRV_DP4(Context *ctx)
{
    SpirvResult src0 = spv_load_srcarg_full(ctx, 0);
    SpirvResult src1 = spv_load_srcarg_full(ctx, 1);

    emit_SPIRV_dotproduct(ctx, src0, src1);
} // emit_SPIRV_DP4

void emit_SPIRV_DP3(Context *ctx)
{
    SpirvResult src0 = spv_load_srcarg(ctx, 0, 0x7);
    SpirvResult src1 = spv_load_srcarg(ctx, 1, 0x7);

    emit_SPIRV_dotproduct(ctx, src0, src1);
} // emit_SPIRV_DP3

static void spv_emit_begin_ds(Context *ctx, SpirvResult* dst, SpirvResult* src)
{
    *src = spv_load_srcarg_full(ctx, 0);
    dst->tid = spv_get_type(ctx, STI_VEC4);
    dst->id = spv_bumpid(ctx);
    push_output(ctx, &ctx->mainline);
} // spv_emit_begin_ds

static void spv_emit_begin_dss(Context *ctx, SpirvResult* dst, SpirvResult* src0, SpirvResult* src1)
{
    *src0 = spv_load_srcarg_full(ctx, 0);
    *src1 = spv_load_srcarg_full(ctx, 1);
    dst->tid = spv_get_type(ctx, STI_VEC4);
    dst->id = spv_bumpid(ctx);
    push_output(ctx, &ctx->mainline);
} // spv_emit_begin_dss

static void spv_emit_begin_dsss(Context *ctx, SpirvResult* dst,
                                SpirvResult* src0, SpirvResult* src1, SpirvResult* src2)
{
    *src0 = spv_load_srcarg_full(ctx, 0);
    *src1 = spv_load_srcarg_full(ctx, 1);
    *src2 = spv_load_srcarg_full(ctx, 2);
    dst->tid = spv_get_type(ctx, STI_VEC4);
    dst->id = spv_bumpid(ctx);
    push_output(ctx, &ctx->mainline);
} // spv_emit_begin_dsss

static void spv_emit_end(Context *ctx, SpirvResult dst)
{
    pop_output(ctx);
    spv_assign_destarg(ctx, dst);
} // spv_emit_end

static SpirvTexm3x3SetupResult spv_texm3x3_setup(Context *ctx)
{
    SpirvTexm3x3SetupResult result;

    DestArgInfo *pDstInfo = &ctx->dest_arg;

    RegisterList *pSrc0 = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0);
    RegisterList *pSrc1 = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0);
    RegisterList *pSrc2 = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1);
    RegisterList *pSrc3 = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1);
    RegisterList *pSrc4 = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum);
    RegisterList *pDst  = spv_getreg(ctx, pDstInfo->regtype, pDstInfo->regnum);

    SpirvResult src0 = spv_loadreg(ctx, pSrc0);
    SpirvResult src1 = spv_loadreg(ctx, pSrc1);
    SpirvResult src2 = spv_loadreg(ctx, pSrc2);
    SpirvResult src3 = spv_loadreg(ctx, pSrc3);
    SpirvResult src4 = spv_loadreg(ctx, pSrc4);
    SpirvResult dst  = spv_loadreg(ctx, pDst);

    result.id_dst_pad0 = src0.id;
    result.id_dst_pad1 = src2.id;
    result.id_dst      = dst.id;

    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    uint32 tid_vec3  = spv_get_type(ctx, STI_VEC3);

    uint32 id_src0_xyz = spv_bumpid(ctx);
    uint32 id_src1_xyz = spv_bumpid(ctx);
    uint32 id_src2_xyz = spv_bumpid(ctx);
    uint32 id_src3_xyz = spv_bumpid(ctx);
    uint32 id_src4_xyz = spv_bumpid(ctx);
    uint32 id_dst_xyz  = spv_bumpid(ctx);
    uint32 id_res_x    = spv_bumpid(ctx);
    uint32 id_res_y    = spv_bumpid(ctx);
    uint32 id_res_z    = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);

    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, tid_vec3, id_src0_xyz, src0.id, src0.id, 0, 1, 2);
    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, tid_vec3, id_src1_xyz, src1.id, src1.id, 0, 1, 2);
    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, tid_vec3, id_src2_xyz, src2.id, src2.id, 0, 1, 2);
    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, tid_vec3, id_src3_xyz, src3.id, src3.id, 0, 1, 2);
    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, tid_vec3, id_src4_xyz, src4.id, src4.id, 0, 1, 2);
    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, tid_vec3, id_dst_xyz,  dst.id,  dst.id,  0, 1, 2);

    spv_emit(ctx, 5, SpvOpDot, tid_float, id_res_x, id_src0_xyz, id_src1_xyz);
    spv_emit(ctx, 5, SpvOpDot, tid_float, id_res_y, id_src2_xyz, id_src3_xyz);
    spv_emit(ctx, 5, SpvOpDot, tid_float, id_res_z, id_dst_xyz,  id_src4_xyz);

    pop_output(ctx);

    result.id_res_x = id_res_x;
    result.id_res_y = id_res_y;
    result.id_res_z = id_res_z;

    return result;
} // spv_texm3x3_setup

static uint32 spv_reflect(Context *ctx, uint32 id_normal, uint32 id_eyeray)
{
    // reflect(E : vec3 = eyeray, N : vec3 = normal) -> vec3
    // 2 * [(N*E) / (N*N)] * N - E

    uint32 tid_vec3     = spv_get_type(ctx, STI_VEC3);
    uint32 id_2         = spv_getscalarf(ctx, 2.0f);
    uint32 id_2_v3      = spv_bumpid(ctx);
    uint32 id_refl_0    = spv_bumpid(ctx);
    uint32 id_refl_1    = spv_bumpid(ctx);
    uint32 id_refl_2    = spv_bumpid(ctx);
    uint32 id_refl_3    = spv_bumpid(ctx);
    uint32 id_refl_4    = spv_bumpid(ctx);
    uint32 id_reflected = spv_bumpid(ctx);

    spv_emit(ctx, 3 + 3, SpvOpCompositeConstruct, tid_vec3, id_2_v3, id_2, id_2, id_2);
    spv_emit(ctx, 5, SpvOpFMul, tid_vec3, id_refl_0, id_normal, id_eyeray);
    spv_emit(ctx, 5, SpvOpFMul, tid_vec3, id_refl_1, id_normal, id_normal);
    spv_emit(ctx, 5, SpvOpFDiv, tid_vec3, id_refl_2, id_refl_0, id_refl_1);
    spv_emit(ctx, 5, SpvOpFMul, tid_vec3, id_refl_3, id_refl_2, id_normal);
    spv_emit(ctx, 5, SpvOpFMul, tid_vec3, id_refl_4, id_refl_3, id_2_v3);
    spv_emit(ctx, 5, SpvOpFSub, tid_vec3, id_reflected, id_refl_4, id_eyeray);

    return id_reflected;
} // spv_reflect

void emit_SPIRV_ADD(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);
    spv_emit(ctx, 5, SpvOpFAdd, dst.tid, dst.id, src0.id, src1.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_ADD

void emit_SPIRV_SUB(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);
    spv_emit(ctx, 5, SpvOpFSub, dst.tid, dst.id, src0.id, src1.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_SUB

void emit_SPIRV_MUL(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);
    spv_emit(ctx, 5, SpvOpFMul, dst.tid, dst.id, src0.id, src1.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_MUL

void emit_SPIRV_SLT(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);

    // https://msdn.microsoft.com/en-us/library/windows/desktop/cc308050(v=vs.85).aspx
    // "The comparisons EQ, GT, GE, LT, and LE, when either or both operands is NaN returns FALSE"
    uint32 bool_result = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpFOrdLessThan, spv_get_type(ctx, STI_BVEC4), bool_result, src0.id, src1.id);

    uint32 ones  = spv_getvec4_one(ctx);
    uint32 zeros = spv_getvec4_zero(ctx);
    spv_emit(ctx, 6, SpvOpSelect, dst.tid, dst.id, bool_result, ones, zeros);

    spv_emit_end(ctx, dst);
} // emit_SPIRV_SLT

void emit_SPIRV_SGE(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);

    // https://msdn.microsoft.com/en-us/library/windows/desktop/cc308050(v=vs.85).aspx
    // "The comparisons EQ, GT, GE, LT, and LE, when either or both operands is NaN returns FALSE"
    uint32 bool_result = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpFOrdGreaterThanEqual, spv_get_type(ctx, STI_BVEC4), bool_result, src0.id, src1.id);

    uint32 ones  = spv_getvec4_one(ctx);
    uint32 zeros = spv_getvec4_zero(ctx);

    spv_emit(ctx, 6, SpvOpSelect, dst.tid, dst.id, bool_result, ones, zeros);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_SGE

void emit_SPIRV_MIN(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);
    spv_emit(ctx, 5 + 2, SpvOpExtInst, dst.tid, dst.id, spv_getext(ctx), GLSLstd450FMin, src0.id, src1.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_MIN

void emit_SPIRV_MAX(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);
    spv_emit(ctx, 5 + 2, SpvOpExtInst, dst.tid, dst.id, spv_getext(ctx), GLSLstd450FMax, src0.id, src1.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_MAX

void emit_SPIRV_POW(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);
    uint32 id_abs = spv_bumpid(ctx);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, src0.tid, id_abs, spv_getext(ctx), GLSLstd450FAbs, src0.id);
    spv_emit(ctx, 5 + 2, SpvOpExtInst, dst.tid, dst.id, spv_getext(ctx), GLSLstd450Pow, id_abs, src1.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_POW

static uint32 spv_extract_vec3(Context *ctx, uint32 input)
{
    uint32 vec3 = spv_get_type(ctx, STI_VEC3);
    uint32 result = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, vec3, result, input, input, 0, 1, 2);
    pop_output(ctx);

    return result;
} // spv_extract_vec3

void emit_SPIRV_CRS(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);

    uint32 vec3 = spv_get_type(ctx, STI_VEC3);
    uint32 src0_vec3 = spv_extract_vec3(ctx, src0.id);
    uint32 src1_vec3 = spv_extract_vec3(ctx, src1.id);
    uint32 result_vec3 = spv_bumpid(ctx);

    spv_emit(ctx, 5 + 2, SpvOpExtInst, vec3, result_vec3, spv_getext(ctx),
             GLSLstd450Cross, src0_vec3, src1_vec3);

    // According to DirectX docs, CRS doesn't allow `w` in its writemask, so we
    // can make this component anything and the code generated by
    // `spv_assign_destarg()` will just throw it away.
    spv_emit(ctx, 5 + 4, SpvOpVectorShuffle, dst.tid, dst.id,
             result_vec3, result_vec3, 0, 1, 2, 0xFFFFFFFF);

    spv_emit_end(ctx, dst);
} // emit_SPIRV_CRS

void emit_SPIRV_MAD(Context *ctx)
{
    SpirvResult src0 = spv_load_srcarg_full(ctx, 0);
    SpirvResult src1 = spv_load_srcarg_full(ctx, 1);
    SpirvResult src2 = spv_load_srcarg_full(ctx, 2);
    assert(src0.tid == src1.tid);
    assert(src0.tid == src2.tid);
    uint32 mul_result = spv_bumpid(ctx);
    SpirvResult result;
    result.tid = src0.tid;
    result.id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5, SpvOpFMul, src0.tid, mul_result, src0.id, src1.id);
    spv_emit(ctx, 5, SpvOpFAdd, src0.tid, result.id, mul_result, src2.id);
    pop_output(ctx);

    spv_assign_destarg(ctx, result);
} // emit_SPIRV_MAD

void emit_SPIRV_TEXKILL(Context *ctx)
{
    const DestArgInfo *pDstInfo = &ctx->dest_arg;
    RegisterList *pDst  = spv_getreg(ctx, pDstInfo->regtype, pDstInfo->regnum);
    SpirvResult dst = spv_loadreg(ctx, pDst);

    uint32 vec3 = spv_get_type(ctx, STI_VEC3);
    uint32 bvec3 = spv_get_type(ctx, STI_BVEC3);

    uint32 zeros = spv_get_zero(ctx, vec3);

    push_output(ctx, &ctx->mainline);
    uint32 res_swiz = spv_emit_swizzle(ctx, dst.id, vec3, (0 << 0) | (1 << 2) | (2 << 4), 0x7);
    uint32 res_lt = spv_bumpid(ctx);
    uint32 res_any = spv_bumpid(ctx);
    uint32 label_true = spv_bumpid(ctx);
    uint32 label_merge = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpFOrdLessThan, bvec3, res_lt, res_swiz, zeros);
    spv_emit(ctx, 4, SpvOpAny, spv_get_type(ctx, STI_BOOL), res_any, res_lt);
    spv_emit(ctx, 3, SpvOpSelectionMerge, label_merge, 0);
    spv_emit(ctx, 4, SpvOpBranchConditional, res_any, label_true, label_merge);
    spv_emit(ctx, 2, SpvOpLabel, label_true);
    spv_emit(ctx, 1, SpvOpKill);
    spv_emit(ctx, 2, SpvOpLabel, label_merge);
    pop_output(ctx);
} // emit_SPIRV_TEXKILL

void emit_SPIRV_DP2ADD(Context *ctx)
{
    SpirvResult src0 = spv_load_srcarg(ctx, 0, 0x3);
    SpirvResult src1 = spv_load_srcarg(ctx, 1, 0x3);
    SpirvResult src2 = spv_load_srcarg(ctx, 2, 0x1);

    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    uint32 id_dot = spv_bumpid(ctx);
    uint32 id_add = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5, SpvOpDot, tid_float, id_dot, src0.id, src1.id);
    spv_emit(ctx, 5, SpvOpFAdd, tid_float, id_add, id_dot, src2.id);
    SpirvResult result;
    result.tid = spv_get_type(ctx, STI_VEC4);
    result.id = spv_vectorbroadcast(ctx, result.tid, id_add);
    pop_output(ctx);

    spv_assign_destarg(ctx, result);
} // emit_SPIRV_DP2ADD

void emit_SPIRV_MOV(Context *ctx)
{
    SpirvResult src0 = spv_load_srcarg_full(ctx, 0);
    spv_assign_destarg(ctx, src0);
} // emit_SPIRV_MOV

void emit_SPIRV_RCP(Context *ctx)
{
    /*
    if (src != 0.0f)
        dst = 1.0f / src;
    else
        dst = FLT_MAX;
    */

    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);

    SpirvTypeIdx sti_bvec =
       (src.tid == ctx->spirv.tid[STI_VEC4]) ? STI_BVEC4 :
       (src.tid == ctx->spirv.tid[STI_VEC3]) ? STI_BVEC3 :
       (src.tid == ctx->spirv.tid[STI_VEC2]) ? STI_BVEC2 : STI_BOOL;

    uint32 tid_bvec = spv_get_type(ctx, sti_bvec);
    uint32 id_one = spv_get_one(ctx, src.tid);
    uint32 id_zero = spv_get_zero(ctx, src.tid);
    uint32 id_flt_max = spv_get_flt_max(ctx, src.tid);
    uint32 id_mask = spv_bumpid(ctx);
    uint32 id_div = spv_bumpid(ctx);

    spv_emit(ctx, 5, SpvOpFOrdNotEqual, tid_bvec, id_mask, src.id, id_zero);
    spv_emit(ctx, 5, SpvOpFDiv, dst.tid, id_div, id_one, src.id);
    spv_emit(ctx, 6, SpvOpSelect, dst.tid, dst.id, id_mask, id_div, id_flt_max);

    spv_emit_end(ctx, dst);
} // emit_SPIRV_RCP

void emit_SPIRV_RSQ(Context *ctx)
{
    /*
    if (src != 0.0f)
        dst = 1.0f / abs(src);
    else
        dst = FLT_MAX;
    */
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);

    SpirvTypeIdx sti_bvec =
       (src.tid == ctx->spirv.tid[STI_VEC4]) ? STI_BVEC4 :
       (src.tid == ctx->spirv.tid[STI_VEC3]) ? STI_BVEC3 :
       (src.tid == ctx->spirv.tid[STI_VEC2]) ? STI_BVEC2 : STI_BOOL;

    uint32 tid_bvec = spv_get_type(ctx, sti_bvec);
    uint32 id_zero = spv_get_zero(ctx, src.tid);
    uint32 id_flt_max = spv_get_flt_max(ctx, src.tid);
    uint32 id_mask = spv_bumpid(ctx);
    uint32 id_abs = spv_bumpid(ctx);
    uint32 id_rsq = spv_bumpid(ctx);

    spv_emit(ctx, 5, SpvOpFOrdNotEqual, tid_bvec, id_mask, src.id, id_zero);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, dst.tid, id_abs, spv_getext(ctx), GLSLstd450FAbs, src.id);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, dst.tid, id_rsq, spv_getext(ctx), GLSLstd450InverseSqrt, id_abs);
    spv_emit(ctx, 6, SpvOpSelect, dst.tid, dst.id, id_mask, id_rsq, id_flt_max);

    spv_emit_end(ctx, dst);
} // emit_SPIRV_RSQ

void emit_SPIRV_EXP(Context *ctx)
{
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, dst.tid, dst.id, spv_getext(ctx), GLSLstd450Exp2, src.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_EXP

void emit_SPIRV_SGN(Context *ctx)
{
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);

    // SGN also takes a src1 and src2 to use for intermediate results, they are
    // left undefined after the instruction executes, and as such it is
    // perfectly valid for us to not touch those registers in our implementation
    spv_emit(ctx, 5 + 1, SpvOpExtInst, dst.tid, dst.id, spv_getext(ctx), GLSLstd450FSign, src.id);

    spv_emit_end(ctx, dst);
} // emit_SPIRV_SGN

void emit_SPIRV_ABS(Context *ctx)
{
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, dst.tid, dst.id, spv_getext(ctx), GLSLstd450FAbs, src.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_ABS

void emit_SPIRV_NRM(Context *ctx)
{
    /*
        float dot = dot(src, src);

        float f;
        if (dot != 0)
            f = (float)(1/sqrt(dot));
        else
            f = FLT_MAX;

        dst = src0*f;
    */

    SpirvResult src = spv_load_srcarg_full(ctx, 0);
    uint32 tid_vec3 = spv_get_type(ctx, STI_VEC3);
    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
    uint32 id_zero = spv_getscalarf(ctx, 0.0f);
    uint32 id_flt_max = spv_getscalarf(ctx, FLT_MAX);
    uint32 id_src_xyz = spv_bumpid(ctx);
    uint32 id_dot = spv_bumpid(ctx);
    uint32 id_dot_valid = spv_bumpid(ctx);
    uint32 id_f = spv_bumpid(ctx);
    uint32 id_f_sane = spv_bumpid(ctx);
    uint32 id_f_vec = spv_bumpid(ctx);

    SpirvResult dst;
    dst.tid = src.tid;
    dst.id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, tid_vec3, id_src_xyz, src.id, src.id, 0, 1, 2);
    spv_emit(ctx, 5, SpvOpDot, tid_float, id_dot, id_src_xyz, id_src_xyz);
    spv_emit(ctx, 5, SpvOpFOrdNotEqual, tid_bool, id_dot_valid, id_dot, id_zero);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, tid_float, id_f, spv_getext(ctx), GLSLstd450InverseSqrt, id_dot);
    spv_emit(ctx, 6, SpvOpSelect, tid_float, id_f_sane, id_dot_valid, id_f, id_flt_max);
    spv_emit(ctx, 3 + 4, SpvOpCompositeConstruct, dst.tid, id_f_vec, id_f_sane, id_f_sane, id_f_sane, id_f_sane);
    spv_emit(ctx, 5, SpvOpFMul, dst.tid, dst.id, src.id, id_f_vec);
    pop_output(ctx);
    spv_assign_destarg(ctx, dst);
} // emit_SPIRV_NRM

void emit_SPIRV_FRC(Context *ctx)
{
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, dst.tid, dst.id, spv_getext(ctx), GLSLstd450Fract, src.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_FRC

void emit_SPIRV_LOG(Context *ctx)
{
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);

    // LOG(x) := (x == vec4(0.0)) ? vec4(-FLT_MAX) : log2(abs(x))

    // abs(x)
    uint32 abs_src0 = spv_bumpid(ctx);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, dst.tid, abs_src0, spv_getext(ctx), GLSLstd450FAbs, src.id);

    // vec4(0.0)
    uint32 vec4_zero = spv_vectorbroadcast(ctx, dst.tid, spv_getscalarf(ctx, 0.0f));

    // x == vec4(0.0)
    uint32 is_zero = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpFOrdEqual, spv_get_type(ctx, STI_BVEC4), is_zero, abs_src0, vec4_zero);

    // log2(abs(x))
    uint32 log2_of_nonzero = spv_bumpid(ctx);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, dst.tid, log2_of_nonzero, spv_getext(ctx), GLSLstd450Log2, abs_src0);

    // vec4(-FLT_MAX)
    uint32 vec4_neg_flt_max = spv_vectorbroadcast(ctx, dst.tid, spv_getscalarf(ctx, -FLT_MAX));

    // (x == vec4(0.0)) ? vec4(-FLT_MAX) : log2(abs(x))
    spv_emit(ctx, 6, SpvOpSelect, dst.tid, dst.id, is_zero, vec4_neg_flt_max, log2_of_nonzero);

    spv_emit_end(ctx, dst);
} // emit_SPIRV_LOG

void emit_SPIRV_SINCOS(Context *ctx)
{
    SpirvResult src = spv_load_srcarg(ctx, 0, 0x1);

    // For vs_2_0 and vs_2_x this instruction also has a src1 and src2 which provide a couple of constants
    // We just ignore these in any case

    // float V = src0.x;

    int writemask = ctx->dest_arg.writemask;
    uint32 id_zero = spv_get_zero(ctx, src.tid);

    uint32 id_cos;
    if (writemask & 1) // .x = cos(V)
    {
        id_cos = spv_bumpid(ctx);
        spv_emit(ctx, 5 + 1, SpvOpExtInst, src.tid, id_cos, spv_getext(ctx), GLSLstd450Cos, src.id);
    } // if
    else
        id_cos = id_zero;

    uint32 id_sin;
    if (writemask & 2) // .y = sin(V)
    {
        id_sin = spv_bumpid(ctx);
        spv_emit(ctx, 5 + 1, SpvOpExtInst, src.tid, id_sin, spv_getext(ctx), GLSLstd450Sin, src.id);
    } // if
    else
        id_sin = id_zero;

    SpirvResult dst;
    dst.tid = spv_get_type(ctx, STI_VEC4);
    dst.id = spv_bumpid(ctx);
    spv_emit(ctx, 3 + 4, SpvOpCompositeConstruct, dst.tid, dst.id, id_cos, id_sin, id_zero, id_zero);

    spv_assign_destarg(ctx, dst);
} // emit_SPIRV_SINCOS

void emit_SPIRV_MOVA(Context *ctx)
{
    SpirvResult src = spv_load_srcarg_full(ctx, 0);
    assert(src.tid == spv_get_type(ctx, STI_VEC4));

    uint32 id_rounded = spv_bumpid(ctx);

    SpirvResult dst;
    dst.tid = spv_get_type(ctx, STI_IVEC4);
    dst.id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5 + 1, SpvOpExtInst, spv_get_type(ctx, STI_VEC4), id_rounded,
             spv_getext(ctx), GLSLstd450Round, src.id);
    spv_emit(ctx, 4, SpvOpConvertFToS, dst.tid, dst.id, id_rounded);
    pop_output(ctx);

    spv_assign_destarg(ctx, dst);
} // emit_SPIRV_MOVA

void emit_SPIRV_CMP(Context *ctx)
{
    SpirvResult dst, src0, src1, src2;
    spv_emit_begin_dsss(ctx, &dst, &src0, &src1, &src2);
    uint32 id_0_0 = spv_get_zero(ctx, src0.tid);

    uint32 id_cmp = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpFUnordGreaterThanEqual, spv_get_type(ctx, STI_BVEC4), id_cmp, src0.id, id_0_0);
    spv_emit(ctx, 6, SpvOpSelect, dst.tid, dst.id, id_cmp, src1.id, src2.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_CMP

void emit_SPIRV_CND(Context *ctx)
{
    SpirvResult dst, src0, src1, src2;
    spv_emit_begin_dsss(ctx, &dst, &src0, &src1, &src2);
    uint32 id_0_5 = spv_get_constant_composite(ctx, src0.tid, ctx->spirv.id_0_5, 0.5f);

    uint32 id_cmp = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpFUnordGreaterThan, spv_get_type(ctx, STI_BVEC4), id_cmp, src0.id, id_0_5);
    spv_emit(ctx, 6, SpvOpSelect, dst.tid, dst.id, id_cmp, src1.id, src2.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_CND

void emit_SPIRV_LIT(Context *ctx)
{
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);

    if (!ctx->spirv.id_func_lit)
        ctx->spirv.id_func_lit = spv_bumpid(ctx);

    spv_emit(ctx, 5, SpvOpFunctionCall, dst.tid, dst.id, ctx->spirv.id_func_lit, src.id);

    spv_emit_end(ctx, dst);
} // emit_SPIRV_LIT

void emit_SPIRV_DST(Context *ctx)
{
    SpirvResult dst, src0, src1;
    spv_emit_begin_dss(ctx, &dst, &src0, &src1);

    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    dst.tid = spv_get_type(ctx, STI_VEC4);
    uint32 id_1_0 = spv_getscalarf(ctx, 1.0f);
    uint32 id_src0_y = spv_bumpid(ctx);
    uint32 id_src1_y = spv_bumpid(ctx);
    uint32 id_src0_z = spv_bumpid(ctx);
    uint32 id_src1_w = spv_bumpid(ctx);
    uint32 id_dst_y = spv_bumpid(ctx);
    dst.id = spv_bumpid(ctx);

    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_src0_y, src0.id, 1);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_src1_y, src1.id, 1);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_src0_z, src0.id, 2);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_src1_w, src1.id, 3);
    spv_emit(ctx, 5, SpvOpFMul, tid_float, id_dst_y, id_src0_y, id_src1_y);
    spv_emit(ctx, 3 + 4, SpvOpCompositeConstruct, dst.tid, dst.id, id_1_0, id_dst_y, id_src0_z, id_src1_w);

    spv_emit_end(ctx, dst);
} // emit_SPIRV_DST

void emit_SPIRV_LRP(Context *ctx)
{
    // lerp(x, y, a) = x + a*(y - x)
    //               = x*(1 - a) + y*a
    SpirvResult a = spv_load_srcarg_full(ctx, 0); // 'scale'
    SpirvResult y = spv_load_srcarg_full(ctx, 1); // 'end'
    SpirvResult x = spv_load_srcarg_full(ctx, 2); // 'start'
    assert(x.tid == y.tid);
    SpirvResult result;
    result.id = spv_bumpid(ctx);
    result.tid = x.tid;

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5 + 3, SpvOpExtInst, result.tid, result.id, spv_getext(ctx), GLSLstd450FMix, x.id, y.id, a.id);
    pop_output(ctx);

    spv_assign_destarg(ctx, result);
} // emit_SPIRV_LRP

static void spv_emit_vecXmatrix(Context *ctx, int rows, int writemask)
{
    int i;

    assert(rows <= 4);
    assert(writemask == 0x7 || writemask == 0xF);

    uint32 src0 = spv_load_srcarg(ctx, 0, writemask).id;
    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);

    RegisterType src1type = ctx->source_args[1].regtype;
    int src1num = ctx->source_args[1].regnum;

    uint32 result_components[4];
    for (i = 0; i < rows; i++)
    {
        SpirvResult row = spv_loadreg(ctx, spv_getreg(ctx, src1type, src1num + i));
        row = spv_swizzle(ctx, row, SPV_NO_SWIZZLE, writemask);
        uint32 dot_result = spv_bumpid(ctx);

        push_output(ctx, &ctx->mainline);
        spv_emit(ctx, 5, SpvOpDot, tid_float, dot_result, src0, row.id);
        pop_output(ctx);

        result_components[i] = dot_result;
    } // for

    SpirvResult r;
    r.tid = spv_get_type(ctx, STI_VEC4);
    r.id = spv_bumpid(ctx);

    uint32 id_zero = 0;
    if (rows < 4)
        id_zero = spv_getscalarf(ctx, 0.0f);

    push_output(ctx, &ctx->mainline);
    spv_emit_part(ctx, 3 + 4, 3, SpvOpCompositeConstruct, r.tid, r.id);
    for (i = 0; i < rows; i++) spv_emit_word(ctx, result_components[i]);
    for (i = rows; i < 4; i++) spv_emit_word(ctx, id_zero);
    pop_output(ctx);

    spv_assign_destarg(ctx, r);
} // spv_emit_vecXmatrix

void emit_SPIRV_M4X4(Context *ctx)
{
    // float4 * (4 columns, 4 rows) -> float4
    spv_emit_vecXmatrix(ctx, 4, 0xF);
} // emit_SPIRV_M4X4

void emit_SPIRV_M4X3(Context *ctx)
{
    // float4 * (4 columns, 3 rows) -> float3
    spv_emit_vecXmatrix(ctx, 3, 0xF);
} // emit_SPIRV_M4X3

void emit_SPIRV_M3X4(Context *ctx)
{
    // float3 * (3 columns, 4 rows) -> float4
    spv_emit_vecXmatrix(ctx, 4, 0x7);
} // emit_SPIRV_M3X4

void emit_SPIRV_M3X3(Context *ctx)
{
    // float3 * (3 columns, 3 rows) -> float3
    spv_emit_vecXmatrix(ctx, 3, 0x7);
} // emit_SPIRV_M3X3

void emit_SPIRV_M3X2(Context *ctx)
{
    // float3 * (3 columns, 2 rows) -> float2
    spv_emit_vecXmatrix(ctx, 2, 0x7);
} // emit_SPIRV_M3X2

void emit_SPIRV_TEXLD(Context *ctx)
{
    if (!shader_version_atleast(ctx, 1, 4))
    {
        DestArgInfo *dst_info = &ctx->dest_arg;

        RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, dst_info->regnum);
        RegisterList *treg = spv_getreg(ctx, dst_info->regtype, dst_info->regnum);

        // Variables are not declared using dcl opcodes, so handle it in this instruction.
        assert(sreg->spirv.iddecl == 0);
        assert(treg->spirv.iddecl == 0);

        // Prep the result
        SpirvResult result;
        result.tid = spv_get_type(ctx, STI_VEC4);
        result.id = spv_bumpid(ctx);
        SpirvResult sampler = spv_loadreg(ctx, sreg);
        // OpImageSampleImplicitLod should ignore the components of this argument that
        // it doesn't need, so we don't need to mask it
        SpirvResult texcoord = spv_loadreg(ctx, treg);

        // Generate the instruction.
        // OpImageSampleImplicitLod should ignore the components of the
        // texcoord that it doesn't need, so we don't need to mask it.
        push_output(ctx, &ctx->mainline);
        spv_emit(ctx, 5, SpvOpImageSampleImplicitLod, result.tid, result.id,
                 sampler.id, texcoord.id);
        pop_output(ctx);

        // Emit the result, finally.
        assert(!isscalar(ctx, ctx->shader_type, sreg->regtype, sreg->regnum));
        spv_assign_destarg(ctx, result);
    } // if

    else if (!shader_version_atleast(ctx, 2, 0))
    {
        // ps_1_4 is different, too!
        fail(ctx, "TEXLD == Shader Model 1.4 unimplemented.");  // !!! FIXME
        return;
    } // else if

    else
    {
        const SourceArgInfo *samp_arg = &ctx->source_args[1];
        RegisterList *sampler_reg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, samp_arg->regnum);
        const SourceArgInfo *texcoord_arg = &ctx->source_args[0];
        RegisterList *texcoord_reg = spv_getreg(ctx, texcoord_arg->regtype, texcoord_arg->regnum);

        if (sampler_reg == NULL)
        {
            fail(ctx, "TEXLD using undeclared sampler");
            return;
        } // if

        // OpImageSampleImplicitLod should ignore the components of this argument that
        // it doesn't need, so we don't need to mask it
        uint32 texcoord = spv_load_srcarg_full(ctx, 0).id;
        uint32 sampler = spv_load_srcarg_full(ctx, 1).id;

        // Special case for TEXLDB
        // !!! FIXME: does the d3d bias value map directly to GLSL?
        uint32 bias;
        uint32 instruction_length;
        if (ctx->instruction_controls == CONTROL_TEXLDB)
        {
            uint32 float_tid = spv_get_type(ctx, STI_FLOAT);
            bias = spv_bumpid(ctx);
            instruction_length = 7;

            // The w component of texcoord_reg specifies the bias. Extract it from texcoord_reg
            push_output(ctx, &ctx->mainline);
            spv_emit(ctx, 4 + 1, SpvOpCompositeExtract, float_tid, bias, texcoord, 3);
            pop_output(ctx);
        } // if
        else
        {
            bias = 0;
            instruction_length = 5;
        } // else

        // Determine the opcode
        SpvOp opcode;
        if (ctx->instruction_controls == CONTROL_TEXLDP)
        {
            if ((TextureType) sampler_reg->index == TEXTURE_TYPE_CUBE)
                fail(ctx, "TEXLDP on a cubemap");  // !!! FIXME: is this legal?
            else if ((TextureType) sampler_reg->index == TEXTURE_TYPE_2D)
            {
                // Need to move w to z, z can be discarded entirely
                uint32 vec3_tid = spv_get_type(ctx, STI_VEC3);
                texcoord = spv_emit_swizzle(ctx, texcoord, vec3_tid, (0 << 0) | (1 << 2) | (3 << 4), 0x7);
            }
            opcode = SpvOpImageSampleProjImplicitLod;
        } // if
        else
            opcode = SpvOpImageSampleImplicitLod;

        // Prep the result
        uint32 vec4_tid = spv_get_type(ctx, STI_VEC4);
        uint32 result = spv_bumpid(ctx);

        // Generate the instruction.
        // OpImageSampleImplicitLod should ignore the components of the
        // texcoord that it doesn't need, so we don't need to mask it.
        push_output(ctx, &ctx->mainline);
        spv_emit_part(ctx, instruction_length, 5, opcode, vec4_tid, result,
                      sampler, texcoord);
        if (ctx->instruction_controls == CONTROL_TEXLDB)
        {
            // ... include the bias operand, if applicable
            spv_emit_word(ctx, SpvImageOperandsBiasMask);
            spv_emit_word(ctx, bias);
        } // if
        pop_output(ctx);

        // Emit the result, finally.
        assert(!isscalar(ctx, ctx->shader_type, samp_arg->regtype, samp_arg->regnum));
        SpirvResult r;
        r.id = result;
        r.tid = vec4_tid;
        spv_assign_destarg(ctx, r);
    } // else
} // emit_SPIRV_TEXLD

void emit_SPIRV_IF(Context *ctx)
{
    SpirvResult src0 = spv_load_srcarg(ctx, 0, 0x1);
    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
    uint32 id_cond = src0.id;

    // Predicate register is already boolean so no need to convert.
    if (src0.tid != tid_bool)
    {
        uint32 id_zero = spv_getscalari(ctx, 0);
        id_cond = spv_bumpid(ctx);
        spv_emit(ctx, 5, SpvOpINotEqual, tid_bool, id_cond, src0.id, id_zero);
    } // if

    uint32 id_label_branch = spv_bumpid(ctx);
    uint32 id_label_merge = spv_bumpid(ctx);
    spv_emit(ctx, 3, SpvOpSelectionMerge, id_label_merge, 0);
    spv_emit(ctx, 4, SpvOpBranchConditional, id_cond, id_label_branch, id_label_merge);
    spv_branch_push(ctx, id_label_merge, buffer_size(ctx->output) - 4);
    spv_emit(ctx, 2, SpvOpLabel, id_label_branch);
} // emit_SPIRV_IF

void emit_SPIRV_IFC(Context *ctx)
{
    SpvOp cmp_op = spv_get_comparison(ctx);
    SpirvResult src0 = spv_load_srcarg(ctx, 0, 0x1);
    SpirvResult src1 = spv_load_srcarg(ctx, 1, 0x1);
    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
    uint32 id_cond = spv_bumpid(ctx);
    uint32 id_label_branch = spv_bumpid(ctx);
    uint32 id_label_merge = spv_bumpid(ctx);

    spv_emit(ctx, 5, cmp_op, tid_bool, id_cond, src0.id, src1.id);
    spv_emit(ctx, 3, SpvOpSelectionMerge, id_label_merge, 0);
    spv_emit(ctx, 4, SpvOpBranchConditional, id_cond, id_label_branch, id_label_merge);
    spv_branch_push(ctx, id_label_merge, buffer_size(ctx->output) - 4);
    spv_emit(ctx, 2, SpvOpLabel, id_label_branch);
} // emit_SPIRV_IFC

void emit_SPIRV_ELSE(Context *ctx)
{
    uint32 id_label_merge, patch_offset;
    spv_branch_get(ctx, &id_label_merge, &patch_offset);
    uint32 id_label_else = spv_bumpid(ctx);

    buffer_patch(ctx->output, patch_offset, &id_label_else, sizeof(id_label_else));
    spv_emit(ctx, 2, SpvOpBranch, id_label_merge);
    spv_emit(ctx, 2, SpvOpLabel, id_label_else);
} // emit_SPIRV_ELSE

void emit_SPIRV_ENDIF(Context *ctx)
{
    uint32 id_label_merge, patch_offset;
    spv_branch_pop(ctx, &id_label_merge, &patch_offset);

    spv_emit(ctx, 2, SpvOpBranch, id_label_merge);
    spv_emit(ctx, 2, SpvOpLabel, id_label_merge);
} // emit_SPIRV_ENDIF

void emit_SPIRV_REP(Context *ctx)
{
    SpirvLoopInfo loop = {0};
    uint32 id_label_init = spv_bumpid(ctx);
    loop.id_label_header = spv_bumpid(ctx);
    uint32 id_label_cond = spv_bumpid(ctx);
    uint32 id_label_body = spv_bumpid(ctx);
    loop.id_label_continue = spv_bumpid(ctx);
    loop.id_label_merge = spv_bumpid(ctx);

    // emit end of previous block
    spv_emit(ctx, 2, SpvOpBranch, id_label_init);

    // emit loop init block
    spv_emit(ctx, 2, SpvOpLabel, id_label_init);
    // This block only exists to allow use of SpvOpPhi in loop header block.
    // SpvOpPhi needs to refer to predecessor by it's label ID, so insert dummy
    // block just so we know what the ID is.
    SpirvResult src0 = spv_load_srcarg(ctx, 0, 0x1);

    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
    loop.tid_counter = src0.tid;
    loop.id_counter = spv_bumpid(ctx);
    loop.id_counter_next = spv_bumpid(ctx);

    uint32 id_cond = spv_bumpid(ctx);
    uint32 id_zero = spv_getscalari(ctx, 0);
    spv_emit(ctx, 2, SpvOpBranch, loop.id_label_header);

    // emit loop header block
    spv_emit(ctx, 2, SpvOpLabel, loop.id_label_header);
    spv_emit(ctx, 7, SpvOpPhi, loop.tid_counter, loop.id_counter,
        src0.id, id_label_init,
        loop.id_counter_next, loop.id_label_continue
    );
    spv_emit(ctx, 4, SpvOpLoopMerge, loop.id_label_merge, loop.id_label_continue, 0);
    spv_emit(ctx, 2, SpvOpBranch, id_label_cond);

    // emit loop condition block
    spv_emit(ctx, 2, SpvOpLabel, id_label_cond);
    spv_emit(ctx, 5, SpvOpINotEqual, tid_bool, id_cond, loop.id_counter, id_zero);
    spv_emit(ctx, 4, SpvOpBranchConditional, id_cond, id_label_body, loop.id_label_merge);

    // emit start of loop body block
    spv_emit(ctx, 2, SpvOpLabel, id_label_body);

    spv_loop_push(ctx, &loop);
} // emit_SPIRV_REP

void emit_SPIRV_ENDREP(Context *ctx)
{
    uint32 id_one = spv_getscalari(ctx, 1);
    SpirvLoopInfo loop;
    spv_loop_pop(ctx, &loop);

    // emit end of loop body block
    spv_emit(ctx, 2, SpvOpBranch, loop.id_label_continue);

    // emit loop continue block
    spv_emit(ctx, 2, SpvOpLabel, loop.id_label_continue);
    spv_emit(ctx, 5, SpvOpISub, loop.tid_counter, loop.id_counter_next, loop.id_counter, id_one);
    spv_emit(ctx, 2, SpvOpBranch, loop.id_label_header);

    // emit start of next block
    spv_emit(ctx, 2, SpvOpLabel, loop.id_label_merge);
} // emit_SPIRV_ENDREP

void emit_SPIRV_LOOP(Context *ctx)
{
    SpirvLoopInfo loop = {0};
    uint32 id_label_init = spv_bumpid(ctx);
    loop.id_label_header = spv_bumpid(ctx);
    uint32 id_label_cond = spv_bumpid(ctx);
    uint32 id_label_body = spv_bumpid(ctx);
    loop.id_label_continue = spv_bumpid(ctx);
    loop.id_label_merge = spv_bumpid(ctx);

    /*
        i#.x = iteration count; every round we decrement it and terminate on 0.
        i#.y = aL initial value; every round we subtract aL step from it.
        i#.z = aL step value;

        We use copy of i# as iteration variable. Compared to rep loop, we only
        need to add single instruction for extracting current aL value as single
        int.

        rep i0
            for (int i = i0.x; i; i--)

        loop aL, i0
            for (int3 i = i0, int aL = i.y; i.x; i.x--, aL += i.z)
    */

    // emit end of previous block
    spv_emit(ctx, 2, SpvOpBranch, id_label_init);

    // emit loop init block
    spv_emit(ctx, 2, SpvOpLabel, id_label_init);
    // This block only exists to allow use of SpvOpPhi in loop header block.
    // SpvOpPhi needs to refer to predecessor by it's label ID, so insert dummy block just so we
    // know what the ID is.

    // src0 has aL register. Does it hold any interesting information?
    SpirvResult src1 = spv_load_srcarg(ctx, 1, 0x7);
    uint32 tid_int = spv_get_type(ctx, STI_INT);
    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);

    loop.tid_counter = src1.tid;
    loop.id_counter = spv_bumpid(ctx);
    loop.id_counter_next = spv_bumpid(ctx);
    loop.id_aL = spv_bumpid(ctx);
    uint32 id_counter_x = spv_bumpid(ctx);

    uint32 id_cond = spv_bumpid(ctx);
    uint32 id_zero = spv_getscalari(ctx, 0);
    spv_emit(ctx, 2, SpvOpBranch, loop.id_label_header);

    // emit loop header block
    spv_emit(ctx, 2, SpvOpLabel, loop.id_label_header);
    spv_emit(ctx, 7, SpvOpPhi, loop.tid_counter, loop.id_counter,
        src1.id, id_label_init,
        loop.id_counter_next, loop.id_label_continue
    );
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_int, loop.id_aL, loop.id_counter, 1);
    spv_emit(ctx, 4, SpvOpLoopMerge, loop.id_label_merge, loop.id_label_continue, 0);
    spv_emit(ctx, 2, SpvOpBranch, id_label_cond);

    // emit loop condition block
    spv_emit(ctx, 2, SpvOpLabel, id_label_cond);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_int, id_counter_x, loop.id_counter, 0);
    spv_emit(ctx, 5, SpvOpINotEqual, tid_bool, id_cond, id_counter_x, id_zero);
    spv_emit(ctx, 4, SpvOpBranchConditional, id_cond, id_label_body, loop.id_label_merge);

    // emit start of loop body block
    spv_emit(ctx, 2, SpvOpLabel, id_label_body);

    spv_loop_push(ctx, &loop);
} // emit_SPIRV_LOOP

void emit_SPIRV_ENDLOOP(Context *ctx)
{
    uint32 tid_int = spv_get_type(ctx, STI_INT);
    uint32 tid_ivec2 = spv_get_type(ctx, STI_IVEC2);

    uint32 id_minus_one = spv_getscalari(ctx, -1);
    uint32 id_counter_z = spv_bumpid(ctx);
    uint32 id_inc = spv_bumpid(ctx);
    uint32 id_counter_xy = spv_bumpid(ctx);
    uint32 id_counter_next_xy = spv_bumpid(ctx);

    SpirvLoopInfo loop;
    spv_loop_pop(ctx, &loop);

    // emit end of loop body block
    spv_emit(ctx, 2, SpvOpBranch, loop.id_label_continue);

    // emit loop continue block
    spv_emit(ctx, 2, SpvOpLabel, loop.id_label_continue);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_int, id_counter_z, loop.id_counter, 2);
    spv_emit(ctx, 5, SpvOpCompositeConstruct, tid_ivec2, id_inc, id_minus_one, id_counter_z);
    spv_emit(ctx, 7, SpvOpVectorShuffle, tid_ivec2, id_counter_xy, loop.id_counter, loop.id_counter, 0, 1);
    spv_emit(ctx, 5, SpvOpIAdd, tid_ivec2, id_counter_next_xy, id_counter_xy, id_inc);
    spv_emit(ctx, 5, SpvOpCompositeConstruct, loop.tid_counter, loop.id_counter_next, id_counter_next_xy, id_counter_z);
    spv_emit(ctx, 2, SpvOpBranch, loop.id_label_header);

    // emit start of next block
    spv_emit(ctx, 2, SpvOpLabel, loop.id_label_merge);
} // emit_SPIRV_ENDLOOP

void emit_SPIRV_BREAKC(Context *ctx)
{
    SpirvLoopInfo loop;
    spv_loop_get(ctx, &loop);

    SpvOp cmp_op = spv_get_comparison(ctx);
    SpirvResult src0 = spv_load_srcarg(ctx, 0, 0x1);
    SpirvResult src1 = spv_load_srcarg(ctx, 1, 0x1);
    uint32 tid_bool = spv_get_type(ctx, STI_BOOL);
    uint32 id_cond = spv_bumpid(ctx);
    uint32 id_label_merge = spv_bumpid(ctx);

    // emit branch to merge target
    spv_emit(ctx, 5, cmp_op, tid_bool, id_cond, src0.id, src1.id);
    spv_emit(ctx, 3, SpvOpSelectionMerge, id_label_merge, 0);
    spv_emit(ctx, 4, SpvOpBranchConditional, id_cond, loop.id_label_merge, id_label_merge);
    spv_emit(ctx, 2, SpvOpLabel, id_label_merge);
} // emit_SPIRV_BREAKC

void emit_SPIRV_BREAKP(Context *ctx)
{
    SpirvLoopInfo loop;
    spv_loop_get(ctx, &loop);

    SpirvResult src0 = spv_load_srcarg(ctx, 0, 0x1);

    uint32 id_label_merge = spv_bumpid(ctx);

    spv_emit(ctx, 3, SpvOpSelectionMerge, id_label_merge, 0);
    spv_emit(ctx, 4, SpvOpBranchConditional, src0.id, loop.id_label_merge, id_label_merge);
    spv_emit(ctx, 2, SpvOpLabel, id_label_merge);
} // emit_SPIRV_BREAKP

void emit_SPIRV_LABEL(Context *ctx)
{
    const SourceArgInfo* arg = &ctx->source_args[0];
    RegisterList *reg = spv_getreg(ctx, arg->regtype, arg->regnum);
    spv_check_read_reg_id(ctx, reg);

    uint32 tid_void = spv_get_type(ctx, STI_VOID);
    uint32 tid_func = spv_get_type(ctx, STI_FUNC_VOID);
    uint32 id_func = reg->spirv.iddecl;
    uint32 id_label = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5, SpvOpFunction, tid_void, id_func, 0, tid_func);
    spv_emit(ctx, 2, SpvOpLabel, id_label);
    pop_output(ctx);
} // emit_SPIRV_LABEL

void emit_SPIRV_RET(Context *ctx)
{
    spv_emit_func_end(ctx);
} // emit_SPIRV_RET

void emit_SPIRV_CALL(Context *ctx)
{
    const SourceArgInfo* arg = &ctx->source_args[0];
    RegisterList *reg = spv_getreg(ctx, arg->regtype, arg->regnum);
    spv_check_read_reg_id(ctx, reg);

    uint32 tid_void = spv_get_type(ctx, STI_VOID);
    uint32 id_res = spv_bumpid(ctx);
    uint32 id_func = reg->spirv.iddecl;

    push_output(ctx, &ctx->mainline);
    if (ctx->loops > 0)
        failf(ctx, "Function calls referencing aL not implemented.");
    else
        spv_emit(ctx, 4, SpvOpFunctionCall, tid_void, id_res, id_func);

    pop_output(ctx);
} // emit_SPIRV_CALL

void emit_SPIRV_CALLNZ(Context *ctx)
{
    const SourceArgInfo* arg = &ctx->source_args[0];
    RegisterList *reg = spv_getreg(ctx, arg->regtype, arg->regnum);
    spv_check_read_reg_id(ctx, reg);

    SpirvResult src1 = spv_load_srcarg(ctx, 1, 0x1);

    uint32 tid_void = spv_get_type(ctx, STI_VOID);
    uint32 id_label_then = spv_bumpid(ctx);
    uint32 id_func = reg->spirv.iddecl;
    uint32 id_call_res = spv_bumpid(ctx);
    uint32 id_label_merge = spv_bumpid(ctx);

    spv_emit(ctx, 3, SpvOpSelectionMerge, id_label_merge, 0);
    spv_emit(ctx, 4, SpvOpBranchConditional, src1.id, id_label_then, id_label_merge);

    spv_emit(ctx, 2, SpvOpLabel, id_label_then);
    if (ctx->loops > 0)
        failf(ctx, "Function calls referencing aL not implemented.");
    else
        spv_emit(ctx, 4, SpvOpFunctionCall, tid_void, id_call_res, id_func);
    spv_emit(ctx, 2, SpvOpBranch, id_label_merge);

    spv_emit(ctx, 2, SpvOpLabel, id_label_merge);
} // emit_SPIRV_CALLNZ

void emit_SPIRV_TEXLDD(Context *ctx)
{
    const SourceArgInfo *samp_arg = &ctx->source_args[1];
    if (!reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, samp_arg->regnum))
    {
        fail(ctx, "TEXLDD using undeclared sampler");
        return;
    } // if

    // Prep the result
    SpirvResult result;
    result.tid = spv_get_type(ctx, STI_VEC4);
    result.id = spv_bumpid(ctx);

    SpirvResult texcoord = spv_load_srcarg_full(ctx, 0);
    SpirvResult sampler = spv_load_srcarg_full(ctx, 1);
    SpirvResult grad_x = spv_load_srcarg_full(ctx, 2);
    SpirvResult grad_y = spv_load_srcarg_full(ctx, 3);

    // Generate the instruction.
    // SpvOpImageSampleExplicitLod should ignore the components of the
    // texcoord that it doesn't need, so we don't need to mask it.
    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 8, SpvOpImageSampleExplicitLod, result.tid, result.id, sampler.id,
             texcoord.id, SpvImageOperandsGradMask, grad_x.id, grad_y.id);
    pop_output(ctx);

    // Emit the result, finally.
    assert(!isscalar(ctx, ctx->shader_type, samp_arg->regtype, samp_arg->regnum));
    spv_assign_destarg(ctx, result);
} // emit_SPIRV_TEXLDD

void emit_SPIRV_SETP(Context *ctx)
{
    SpirvResult src0 = spv_load_srcarg_full(ctx, 0);
    SpirvResult src1 = spv_load_srcarg_full(ctx, 1);

    SpirvResult dst;
    dst.tid = spv_get_type(ctx, STI_BVEC4);
    dst.id = spv_bumpid(ctx);

    SpvOp cmp_op = spv_get_comparison(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5, cmp_op, dst.tid, dst.id, src0.id, src1.id);
    pop_output(ctx);

    spv_assign_destarg(ctx, dst);
} // emit_SPIRV_SETP

void emit_SPIRV_TEXLDL(Context *ctx)
{
    const SourceArgInfo *samp_arg = &ctx->source_args[1];
    RegisterList *sampler_reg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, samp_arg->regnum);
    if (sampler_reg == NULL)
    {
        fail(ctx, "TEXLDL using undeclared sampler");
        return;
    } // if
    assert(!isscalar(ctx, ctx->shader_type, samp_arg->regtype, samp_arg->regnum));

    // Prep the result
    SpirvResult result;
    result.tid = spv_get_type(ctx, STI_VEC4);
    result.id = spv_bumpid(ctx);

    SpirvResult sampler = spv_load_srcarg_full(ctx, 1);
    SpirvResult texcoord = spv_load_srcarg_full(ctx, 0);

    // The w component of texcoord_reg specifies the LOD. Extract it from texcoord_reg
    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    uint32 id_lod = spv_bumpid(ctx);

    // Generate the instruction.
    // SpvOpImageSampleExplicitLod should ignore the components of the
    // texcoord that it doesn't need, so we don't need to mask it.
    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 4 + 1, SpvOpCompositeExtract, tid_float, id_lod, texcoord.id, 3);
    spv_emit(ctx, 7, SpvOpImageSampleExplicitLod, result.tid, result.id, sampler.id,
             texcoord.id, SpvImageOperandsLodMask, id_lod);
    pop_output(ctx);

    // Emit the result, finally.
    spv_assign_destarg(ctx, result);
} // emit_SPIRV_TEXLDL

void emit_SPIRV_BREAK(Context *ctx)
{
    uint32 id_label_merge = spv_bumpid(ctx);
    spv_emit(ctx, 3, SpvOpSelectionMerge, id_label_merge, 0);
    spv_emit(ctx, 2, SpvOpBranch, id_label_merge);
    spv_emit(ctx, 2, SpvOpLabel, id_label_merge);
} // emit_SPIRV_BREAK

void emit_SPIRV_TEXM3X2PAD(Context *ctx)
{
    // no-op ... work happens in emit_SPIRV_TEXM3X2TEX().
} // emit_SPIRV_TEXM3X2PAD

void emit_SPIRV_TEXM3X2TEX(Context *ctx)
{
    if (ctx->texm3x2pad_src0 == -1)
        return;

    DestArgInfo *pDstInfo = &ctx->dest_arg;

    RegisterList *pSReg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, pDstInfo->regnum);
    RegisterList *pSrc0 = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->texm3x2pad_src0);
    RegisterList *pSrc1 = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->texm3x2pad_dst0);
    RegisterList *pSrc2 = spv_getreg(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum);
    RegisterList *pDst  = spv_getreg(ctx, pDstInfo->regtype, pDstInfo->regnum);

    SpirvResult sampler = spv_loadreg(ctx, pSReg);
    SpirvResult src0 = spv_loadreg(ctx, pSrc0);
    SpirvResult src1 = spv_loadreg(ctx, pSrc1);
    SpirvResult src2 = spv_loadreg(ctx, pSrc2);
    SpirvResult src3 = spv_loadreg(ctx, pDst);

    src0 = spv_swizzle(ctx, src0, SPV_NO_SWIZZLE, 0x7);
    src1 = spv_swizzle(ctx, src1, SPV_NO_SWIZZLE, 0x7);
    src2 = spv_swizzle(ctx, src2, SPV_NO_SWIZZLE, 0x7);
    src3 = spv_swizzle(ctx, src3, SPV_NO_SWIZZLE, 0x7);

    SpirvResult result;
    uint32 tid_float = spv_get_type(ctx, STI_FLOAT);
    uint32 tid_vec2  = spv_get_type(ctx, STI_VEC2);
    result.tid  = spv_get_type(ctx, STI_VEC4);
    uint32 id_x = spv_bumpid(ctx);
    uint32 id_y = spv_bumpid(ctx);
    uint32 id_texcoord = spv_bumpid(ctx);
    result.id = spv_bumpid(ctx);
    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 5, SpvOpDot, tid_float, id_x, src0.id, src1.id);
    spv_emit(ctx, 5, SpvOpDot, tid_float, id_y, src2.id, src3.id);
    spv_emit(ctx, 3+2, SpvOpCompositeConstruct, tid_vec2, id_texcoord, id_x, id_y);
    spv_emit(ctx, 5, SpvOpImageSampleImplicitLod, result.tid, result.id, sampler.id, id_texcoord);
    pop_output(ctx);
    spv_assign_destarg(ctx, result);
} // emit_SPIRV_TEXM3X2TEX

void emit_SPIRV_TEXM3X3PAD(Context *ctx)
{
    // no-op ... work happens in emit_SPIRV_TEXM3X3*().
} // emit_SPIRV_TEXM3X3PAD

void emit_SPIRV_TEXM3X3(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    // vec4(
    //      dot({src0}.xyz, {src1}.xyz),
    //      dot({src2}.xyz, {src3}.xyz),
    //      dot({dst}.xyz,  {src4}.xyz),
    //      1
    // )

    uint32 id_1 = spv_getscalarf(ctx, 1.0f);

    SpirvTexm3x3SetupResult setup = spv_texm3x3_setup(ctx);

    SpirvResult result;
    result.tid = spv_get_type(ctx, STI_VEC4);
    result.id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 3 + 4, SpvOpCompositeConstruct, result.tid, result.id,
        setup.id_res_x, setup.id_res_y, setup.id_res_z, id_1
    );
    pop_output(ctx);

    spv_assign_destarg(ctx, result);
} // emit_SPIRV_TEXM3X3

void emit_SPIRV_TEXM3X3TEX(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    RegisterList *pSReg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, ctx->dest_arg.regnum);

    // texture{ttypestr}({sampler},
    //     vec3(
    //         dot({src0}.xyz, {src1}.xyz),
    //         dot({src2}.xyz, {src3}.xyz),
    //         dot({dst}.xyz,  {src4}.xyz)
    //     ),
    // )

    SpirvResult sampler = spv_loadreg(ctx, pSReg);

    SpirvTexm3x3SetupResult setup = spv_texm3x3_setup(ctx);

    uint32 tid_vec3    = spv_get_type(ctx, STI_VEC3);
    uint32 tid_vec4    = spv_get_type(ctx, STI_VEC4);
    uint32 id_tc       = spv_bumpid(ctx);

    SpirvResult result;
    result.tid = tid_vec4;
    result.id = spv_bumpid(ctx);

    push_output(ctx, &ctx->mainline);
    spv_emit(ctx, 3 + 3, SpvOpCompositeConstruct, tid_vec3, id_tc,
        setup.id_res_x, setup.id_res_y, setup.id_res_z
    );
    spv_emit(ctx, 5, SpvOpImageSampleImplicitLod, result.tid, result.id, sampler.id, id_tc);
    pop_output(ctx);

    spv_assign_destarg(ctx, result);
} // emit_SPIRV_TEXM3X3TEX

void emit_SPIRV_TEXM3X3SPEC(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    RegisterList *pSReg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, ctx->dest_arg.regnum);
    RegisterList *pSrc5 = spv_getreg(ctx, ctx->source_args[1].regtype, ctx->source_args[1].regnum);

    SpirvTexm3x3SetupResult setup = spv_texm3x3_setup(ctx);

    uint32 tid_vec3 = spv_get_type(ctx, STI_VEC3);

    push_output(ctx, &ctx->mainline);

    uint32 id_normal = spv_bumpid(ctx);
    spv_emit(ctx, 3 + 3, SpvOpCompositeConstruct, tid_vec3, id_normal,
        setup.id_res_x, setup.id_res_y, setup.id_res_z
    );

    SpirvResult src5 = spv_loadreg(ctx, pSrc5);

    uint32 id_eyeray  = spv_bumpid(ctx);
    spv_emit(ctx, 5 + 3, SpvOpVectorShuffle, tid_vec3, id_eyeray, src5.id, src5.id, 0, 1, 2);

    uint32 id_reflected = spv_reflect(ctx, id_normal, id_eyeray);

    SpirvResult sampler = spv_loadreg(ctx, pSReg);

    SpirvResult result;
    result.tid = spv_get_type(ctx, STI_VEC4);
    result.id = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpImageSampleImplicitLod, result.tid, result.id, sampler.id, id_reflected);

    pop_output(ctx);

    spv_assign_destarg(ctx, result);
} // emit_SPIRV_TEXM3X3SPEC

void emit_SPIRV_TEXM3X3VSPEC(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    RegisterList *pSReg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, ctx->dest_arg.regnum);

    SpirvTexm3x3SetupResult setup = spv_texm3x3_setup(ctx);

    uint32 tid_float    = spv_get_type(ctx, STI_FLOAT);
    uint32 tid_vec3     = spv_get_type(ctx, STI_VEC3);

    push_output(ctx, &ctx->mainline);

    uint32 id_normal = spv_bumpid(ctx);
    spv_emit(ctx, 3 + 3, SpvOpCompositeConstruct, tid_vec3, id_normal,
        setup.id_res_x, setup.id_res_y, setup.id_res_z
    );

    uint32 id_eyeray_x = spv_bumpid(ctx);
    uint32 id_eyeray_y = spv_bumpid(ctx);
    uint32 id_eyeray_z = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_eyeray_x, setup.id_dst_pad0, 3);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_eyeray_y, setup.id_dst_pad1, 3);
    spv_emit(ctx, 5, SpvOpCompositeExtract, tid_float, id_eyeray_z, setup.id_dst,      3);

    uint32 id_eyeray = spv_bumpid(ctx);
    spv_emit(ctx, 3 + 3, SpvOpCompositeConstruct, tid_vec3, id_eyeray,
        id_eyeray_x, id_eyeray_y, id_eyeray_z
    );

    uint32 id_reflected = spv_reflect(ctx, id_normal, id_eyeray);

    SpirvResult sampler = spv_loadreg(ctx, pSReg);

    SpirvResult result;
    result.tid = spv_get_type(ctx, STI_VEC4);
    result.id = spv_bumpid(ctx);
    spv_emit(ctx, 5, SpvOpImageSampleImplicitLod, result.tid, result.id, sampler.id, id_reflected);

    pop_output(ctx);

    spv_assign_destarg(ctx, result);
} // emit_SPIRV_TEXM3X3VSPEC

void emit_SPIRV_TEXBEM(Context *ctx)
{
    spv_texbem(ctx, 0);
} // emit_SPIRV_TEXBEM

void emit_SPIRV_TEXBEML(Context *ctx)
{
    spv_texbem(ctx, 1);
} // emit_SPIRV_TEXBEML

void emit_SPIRV_EXPP(Context *ctx)
{
    // !!! FIXME: msdn's asm docs don't list this opcode, I'll have to check the driver documentation.
    emit_SPIRV_EXP(ctx);  // I guess this is just partial precision EXP?
} // emit_SPIRV_EXPP

void emit_SPIRV_LOGP(Context *ctx)
{
    // LOGP is just low-precision LOG, but we'll take the higher precision.
    emit_SPIRV_LOG(ctx);
} // emit_SPIRV_LOGP

void emit_SPIRV_DSX(Context *ctx)
{
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);
    spv_emit(ctx, 4, SpvOpDPdx, dst.tid, dst.id, src.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_DSX

void emit_SPIRV_DSY(Context *ctx)
{
    SpirvResult dst, src;
    spv_emit_begin_ds(ctx, &dst, &src);
    spv_emit(ctx, 4, SpvOpDPdy, dst.tid, dst.id, src.id);
    spv_emit_end(ctx, dst);
} // emit_SPIRV_DSY

void emit_SPIRV_RESERVED(Context *ctx)
{
    // do nothing; fails in the state machine.
} // emit_SPIRV_RESERVED

// !!! FIXME: The following are unimplemented even in the GLSL emitter.
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(TEXCRD)
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2AR)
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2GB)
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2RGB)
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(TEXDP3TEX)
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(TEXM3X2DEPTH)
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(TEXDP3)
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(TEXDEPTH)
EMIT_SPIRV_OPCODE_UNIMPLEMENTED_FUNC(BEM)

#endif  // SUPPORT_PROFILE_SPIRV

#pragma GCC visibility pop
