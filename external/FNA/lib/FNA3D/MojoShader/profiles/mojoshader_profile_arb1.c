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

#if SUPPORT_PROFILE_ARB1

static inline const char *get_ARB1_register_string(Context *ctx,
                        const RegisterType regtype, const int regnum,
                        char *regnum_str, const size_t regnum_size)
{
    // turns out these are identical at the moment.
    return get_D3D_register_string(ctx,regtype,regnum,regnum_str,regnum_size);
} // get_ARB1_register_string

int allocate_scratch_register(Context *ctx)
{
    const int retval = ctx->scratch_registers++;
    if (retval >= ctx->max_scratch_registers)
        ctx->max_scratch_registers = retval + 1;
    return retval;
} // allocate_scratch_register

int allocate_branch_label(Context *ctx)
{
    return ctx->assigned_branch_labels++;
} // allocate_branch_label

const char *allocate_ARB1_scratch_reg_name(Context *ctx, char *buf,
                                           const size_t buflen)
{
    const int scratch = allocate_scratch_register(ctx);
    snprintf(buf, buflen, "scratch%d", scratch);
    return buf;
} // allocate_ARB1_scratch_reg_name

static inline const char *get_ARB1_branch_label_name(Context *ctx, const int id,
                                                char *buf, const size_t buflen)
{
    snprintf(buf, buflen, "branch_label%d", id);
    return buf;
} // get_ARB1_branch_label_name

const char *get_ARB1_varname_in_buf(Context *ctx, const RegisterType rt,
                                    const int regnum, char *buf,
                                    const size_t buflen)
{
    // turns out these are identical at the moment.
    return get_D3D_varname_in_buf(ctx, rt, regnum, buf, buflen);
} // get_ARB1_varname_in_buf

const char *get_ARB1_varname(Context *ctx, const RegisterType rt,
                             const int regnum)
{
    // turns out these are identical at the moment.
    return get_D3D_varname(ctx, rt, regnum);
} // get_ARB1_varname


static inline const char *get_ARB1_const_array_varname_in_buf(Context *ctx,
                                                const int base, const int size,
                                                char *buf, const size_t buflen)
{
    snprintf(buf, buflen, "c_array_%d_%d", base, size);
    return buf;
} // get_ARB1_const_array_varname_in_buf


const char *get_ARB1_const_array_varname(Context *ctx, int base, int size)
{
    char buf[64];
    get_ARB1_const_array_varname_in_buf(ctx, base, size, buf, sizeof (buf));
    return StrDup(ctx, buf);
} // get_ARB1_const_array_varname


const char *make_ARB1_srcarg_string_in_buf(Context *ctx,
                                           const SourceArgInfo *arg,
                                           char *buf, size_t buflen)
{
    // !!! FIXME: this can hit pathological cases where we look like this...
    //
    //    dp3 r1.xyz, t0_bx2, t0_bx2
    //    mad r1.xyz, t0_bias, 1-r1, t0_bx2
    //
    // ...which do a lot of duplicate work in arb1...
    //
    //    SUB scratch0, t0, { 0.5, 0.5, 0.5, 0.5 };
    //    MUL scratch0, scratch0, { 2.0, 2.0, 2.0, 2.0 };
    //    SUB scratch1, t0, { 0.5, 0.5, 0.5, 0.5 };
    //    MUL scratch1, scratch1, { 2.0, 2.0, 2.0, 2.0 };
    //    DP3 r1.xyz, scratch0, scratch1;
    //    SUB scratch0, t0, { 0.5, 0.5, 0.5, 0.5 };
    //    SUB scratch1, { 1.0, 1.0, 1.0, 1.0 }, r1;
    //    SUB scratch2, t0, { 0.5, 0.5, 0.5, 0.5 };
    //    MUL scratch2, scratch2, { 2.0, 2.0, 2.0, 2.0 };
    //    MAD r1.xyz, scratch0, scratch1, scratch2;
    //
    // ...notice that the dp3 calculates the same value into two scratch
    //  registers. This case is easier to handle; just see if multiple
    //  source args are identical, build it up once, and use the same
    //  scratch register for multiple arguments in that opcode.
    //  Even better still, only calculate things once across instructions,
    //  and be smart about letting it linger in a scratch register until we
    //  definitely don't need the calculation anymore. That's harder to
    //  write, though.

    char regnum_str[16] = { '\0' };

    // !!! FIXME: use get_ARB1_varname_in_buf() instead?
    const char *regtype_str = NULL;
    if (!arg->relative)
    {
        regtype_str = get_ARB1_register_string(ctx, arg->regtype,
                                               arg->regnum, regnum_str,
                                               sizeof (regnum_str));
    } // if

    const char *rel_lbracket = "";
    char rel_offset[32] = { '\0' };
    const char *rel_rbracket = "";
    char rel_swizzle[4] = { '\0' };
    const char *rel_regtype_str = "";
    if (arg->relative)
    {
        rel_regtype_str = get_ARB1_varname_in_buf(ctx, arg->relative_regtype,
                                                  arg->relative_regnum,
                                                  (char *) alloca(64), 64);

        rel_swizzle[0] = '.';
        rel_swizzle[1] = swizzle_channels[arg->relative_component];
        rel_swizzle[2] = '\0';

        if (!support_nv2(ctx))
        {
            // The address register in ARB1 only allows the '.x' component, so
            //  we need to load the component we need from a temp vector
            //  register into .x as needed.
            assert(arg->relative_regtype == REG_TYPE_ADDRESS);
            assert(arg->relative_regnum == 0);
            if (ctx->last_address_reg_component != arg->relative_component)
            {
                output_line(ctx, "ARL %s.x, addr%d.%c;", rel_regtype_str,
                            arg->relative_regnum,
                            swizzle_channels[arg->relative_component]);
                ctx->last_address_reg_component = arg->relative_component;
            } // if

            rel_swizzle[1] = 'x';
        } // if

        if (arg->regtype == REG_TYPE_INPUT)
            regtype_str = "vertex.attrib";
        else
        {
            assert(arg->regtype == REG_TYPE_CONST);
            const int arrayidx = arg->relative_array->index;
            const int arraysize = arg->relative_array->count;
            const int offset = arg->regnum - arrayidx;
            assert(offset >= 0);
            regtype_str = get_ARB1_const_array_varname_in_buf(ctx, arrayidx,
                                           arraysize, (char *) alloca(64), 64);
            if (offset != 0)
                snprintf(rel_offset, sizeof (rel_offset), " + %d", offset);
        } // else

        rel_lbracket = "[";
        rel_rbracket = "]";
    } // if

    // This is the source register with everything but swizzle and source mods.
    snprintf(buf, buflen, "%s%s%s%s%s%s%s", regtype_str, regnum_str,
             rel_lbracket, rel_regtype_str, rel_swizzle, rel_offset,
             rel_rbracket);

    // Some of the source mods need to generate instructions to a temp
    //  register, in which case we'll replace the register name.
    const SourceMod mod = arg->src_mod;
    const int inplace = ( (mod == SRCMOD_NONE) || (mod == SRCMOD_NEGATE) ||
                          ((mod == SRCMOD_ABS) && support_nv2(ctx)) );

    if (!inplace)
    {
        const size_t len = 64;
        char *stackbuf = (char *) alloca(len);
        regtype_str = allocate_ARB1_scratch_reg_name(ctx, stackbuf, len);
        regnum_str[0] = '\0'; // move value to scratch register.
        rel_lbracket = "";   // scratch register won't use array.
        rel_rbracket = "";
        rel_offset[0] = '\0';
        rel_swizzle[0] = '\0';
        rel_regtype_str = "";
    } // if

    const char *premod_str = "";
    const char *postmod_str = "";
    switch (mod)
    {
        case SRCMOD_NEGATE:
            premod_str = "-";
            break;

        case SRCMOD_BIASNEGATE:
            premod_str = "-";
            // fall through.
        case SRCMOD_BIAS:
            output_line(ctx, "SUB %s, %s, { 0.5, 0.5, 0.5, 0.5 };",
                        regtype_str, buf);
            break;

        case SRCMOD_SIGNNEGATE:
            premod_str = "-";
            // fall through.
        case SRCMOD_SIGN:
            output_line(ctx,
                "MAD %s, %s, { 2.0, 2.0, 2.0, 2.0 }, { -1.0, -1.0, -1.0, -1.0 };",
                regtype_str, buf);
            break;

        case SRCMOD_COMPLEMENT:
            output_line(ctx, "SUB %s, { 1.0, 1.0, 1.0, 1.0 }, %s;",
                        regtype_str, buf);
            break;

        case SRCMOD_X2NEGATE:
            premod_str = "-";
            // fall through.
        case SRCMOD_X2:
            output_line(ctx, "MUL %s, %s, { 2.0, 2.0, 2.0, 2.0 };",
                        regtype_str, buf);
            break;

        case SRCMOD_DZ:
            fail(ctx, "SRCMOD_DZ currently unsupported in arb1");
            postmod_str = "_dz";
            break;

        case SRCMOD_DW:
            fail(ctx, "SRCMOD_DW currently unsupported in arb1");
            postmod_str = "_dw";
            break;

        case SRCMOD_ABSNEGATE:
            premod_str = "-";
            // fall through.
        case SRCMOD_ABS:
            if (!support_nv2(ctx))  // GL_NV_vertex_program2_option adds this.
                output_line(ctx, "ABS %s, %s;", regtype_str, buf);
            else
            {
                premod_str = (mod == SRCMOD_ABSNEGATE) ? "-|" : "|";
                postmod_str = "|";
            } // else
            break;

        case SRCMOD_NOT:
            fail(ctx, "SRCMOD_NOT currently unsupported in arb1");
            premod_str = "!";
            break;

        case SRCMOD_NONE:
        case SRCMOD_TOTAL:
             break;  // stop compiler whining.
    } // switch

    char swizzle_str[6];
    size_t i = 0;

    if (support_nv4(ctx))  // vFace must be output as "vFace.x" in nv4.
    {
        if (arg->regtype == REG_TYPE_MISCTYPE)
        {
            if ( ((const MiscTypeType) arg->regnum) == MISCTYPE_TYPE_FACE )
            {
                swizzle_str[i++] = '.';
                swizzle_str[i++] = 'x';
            } // if
        } // if
    } // if

    const int scalar = isscalar(ctx, ctx->shader_type, arg->regtype, arg->regnum);
    if (!scalar && !no_swizzle(arg->swizzle))
    {
        swizzle_str[i++] = '.';

        // .xxxx is the same as .x, but .xx is illegal...scalar or full!
        if (replicate_swizzle(arg->swizzle))
            swizzle_str[i++] = swizzle_channels[arg->swizzle_x];
        else
        {
            swizzle_str[i++] = swizzle_channels[arg->swizzle_x];
            swizzle_str[i++] = swizzle_channels[arg->swizzle_y];
            swizzle_str[i++] = swizzle_channels[arg->swizzle_z];
            swizzle_str[i++] = swizzle_channels[arg->swizzle_w];
        } // else
    } // if
    swizzle_str[i] = '\0';
    assert(i < sizeof (swizzle_str));

    snprintf(buf, buflen, "%s%s%s%s%s%s%s%s%s%s", premod_str,
             regtype_str, regnum_str, rel_lbracket,
             rel_regtype_str, rel_swizzle, rel_offset, rel_rbracket,
             swizzle_str, postmod_str);
    // !!! FIXME: make sure the scratch buffer was large enough.
    return buf;
} // make_ARB1_srcarg_string_in_buf

const char *get_ARB1_destarg_varname(Context *ctx, char *buf,
                                     const size_t buflen)
{
    const DestArgInfo *arg = &ctx->dest_arg;
    return get_ARB1_varname_in_buf(ctx, arg->regtype, arg->regnum, buf, buflen);
} // get_ARB1_destarg_varname

const char *get_ARB1_srcarg_varname(Context *ctx, const size_t idx,
                                    char *buf, const size_t buflen)
{
    if (idx >= STATICARRAYLEN(ctx->source_args))
    {
        fail(ctx, "Too many source args");
        *buf = '\0';
        return buf;
    } // if

    const SourceArgInfo *arg = &ctx->source_args[idx];
    return get_ARB1_varname_in_buf(ctx, arg->regtype, arg->regnum, buf, buflen);
} // get_ARB1_srcarg_varname


const char *make_ARB1_destarg_string(Context *ctx, char *buf,
                                     const size_t buflen)
{
    const DestArgInfo *arg = &ctx->dest_arg;

    *buf = '\0';

    const char *sat_str = "";
    if (arg->result_mod & MOD_SATURATE)
    {
        // nv4 can use ".SAT" in all program types.
        // For less than nv4, the "_SAT" modifier is only available in
        //  fragment shaders. Every thing else will fake it later in
        //  emit_ARB1_dest_modifiers() ...
        if (support_nv4(ctx))
            sat_str = ".SAT";
        else if (shader_is_pixel(ctx))
            sat_str = "_SAT";
    } // if

    const char *pp_str = "";
    if (arg->result_mod & MOD_PP)
    {
        // Most ARB1 profiles can't do partial precision (MOD_PP), but that's
        //  okay. The spec says lots of Direct3D implementations ignore the
        //  flag anyhow.
        if (support_nv4(ctx))
            pp_str = "H";
    } // if

    // CENTROID only allowed in DCL opcodes, which shouldn't come through here.
    assert((arg->result_mod & MOD_CENTROID) == 0);

    char regnum_str[16];
    const char *regtype_str = get_ARB1_register_string(ctx, arg->regtype,
                                                       arg->regnum, regnum_str,
                                                       sizeof (regnum_str));
    if (regtype_str == NULL)
    {
        fail(ctx, "Unknown destination register type.");
        return buf;
    } // if

    char writemask_str[6];
    size_t i = 0;
    const int scalar = isscalar(ctx, ctx->shader_type, arg->regtype, arg->regnum);
    if (!scalar && !writemask_xyzw(arg->writemask))
    {
        writemask_str[i++] = '.';
        if (arg->writemask0) writemask_str[i++] = 'x';
        if (arg->writemask1) writemask_str[i++] = 'y';
        if (arg->writemask2) writemask_str[i++] = 'z';
        if (arg->writemask3) writemask_str[i++] = 'w';
    } // if
    writemask_str[i] = '\0';
    assert(i < sizeof (writemask_str));

    //const char *pred_left = "";
    //const char *pred_right = "";
    char pred[32] = { '\0' };
    if (ctx->predicated)
    {
        fail(ctx, "dest register predication currently unsupported in arb1");
        return buf;
        //pred_left = "(";
        //pred_right = ") ";
        make_ARB1_srcarg_string_in_buf(ctx, &ctx->predicate_arg,
                                       pred, sizeof (pred));
    } // if

    snprintf(buf, buflen, "%s%s %s%s%s", pp_str, sat_str,
             regtype_str, regnum_str, writemask_str);
    // !!! FIXME: make sure the scratch buffer was large enough.
    return buf;
} // make_ARB1_destarg_string


void emit_ARB1_dest_modifiers(Context *ctx)
{
    const DestArgInfo *arg = &ctx->dest_arg;

    if (arg->result_shift != 0x0)
    {
        char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
        const char *multiplier = NULL;

        switch (arg->result_shift)
        {
            case 0x1: multiplier = "2.0"; break;
            case 0x2: multiplier = "4.0"; break;
            case 0x3: multiplier = "8.0"; break;
            case 0xD: multiplier = "0.125"; break;
            case 0xE: multiplier = "0.25"; break;
            case 0xF: multiplier = "0.5"; break;
        } // switch

        if (multiplier != NULL)
        {
            char var[64]; get_ARB1_destarg_varname(ctx, var, sizeof (var));
            output_line(ctx, "MUL%s, %s, %s;", dst, var, multiplier);
        } // if
    } // if

    if (arg->result_mod & MOD_SATURATE)
    {
        // nv4 and/or pixel shaders just used the "SAT" modifier, instead.
        if ( (!support_nv4(ctx)) && (!shader_is_pixel(ctx)) )
        {
            char var[64]; get_ARB1_destarg_varname(ctx, var, sizeof (var));
            char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
            output_line(ctx, "MIN%s, %s, 1.0;", dst, var);
            output_line(ctx, "MAX%s, %s, 0.0;", dst, var);
        } // if
    } // if
} // emit_ARB1_dest_modifiers


const char *make_ARB1_srcarg_string(Context *ctx, const size_t idx,
                                    char *buf, const size_t buflen)
{
    if (idx >= STATICARRAYLEN(ctx->source_args))
    {
        fail(ctx, "Too many source args");
        *buf = '\0';
        return buf;
    } // if

    const SourceArgInfo *arg = &ctx->source_args[idx];
    return make_ARB1_srcarg_string_in_buf(ctx, arg, buf, buflen);
} // make_ARB1_srcarg_string

void emit_ARB1_opcode_ds(Context *ctx, const char *opcode)
{
    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
    output_line(ctx, "%s%s, %s;", opcode, dst, src0);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_opcode_ds

void emit_ARB1_opcode_dss(Context *ctx, const char *opcode)
{
    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_ARB1_srcarg_string(ctx, 1, src1, sizeof (src1));
    output_line(ctx, "%s%s, %s, %s;", opcode, dst, src0, src1);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_opcode_dss

void emit_ARB1_opcode_dsss(Context *ctx, const char *opcode)
{
    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_ARB1_srcarg_string(ctx, 1, src1, sizeof (src1));
    char src2[64]; make_ARB1_srcarg_string(ctx, 2, src2, sizeof (src2));
    output_line(ctx, "%s%s, %s, %s, %s;", opcode, dst, src0, src1, src2);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_opcode_dsss


#define EMIT_ARB1_OPCODE_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        emit_ARB1_opcode(ctx, #op); \
    }
#define EMIT_ARB1_OPCODE_D_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        emit_ARB1_opcode_d(ctx, #op); \
    }
#define EMIT_ARB1_OPCODE_S_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        emit_ARB1_opcode_s(ctx, #op); \
    }
#define EMIT_ARB1_OPCODE_SS_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        emit_ARB1_opcode_ss(ctx, #op); \
    }
#define EMIT_ARB1_OPCODE_DS_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        emit_ARB1_opcode_ds(ctx, #op); \
    }
#define EMIT_ARB1_OPCODE_DSS_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        emit_ARB1_opcode_dss(ctx, #op); \
    }
#define EMIT_ARB1_OPCODE_DSSS_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        emit_ARB1_opcode_dsss(ctx, #op); \
    }
#define EMIT_ARB1_OPCODE_DSSSS_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        emit_ARB1_opcode_dssss(ctx, #op); \
    }
#define EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(op) \
    void emit_ARB1_##op(Context *ctx) { \
        failf(ctx, #op " unimplemented in %s profile", ctx->profile->name); \
    }


void emit_ARB1_start(Context *ctx, const char *profilestr)
{
    const char *shader_str = NULL;
    const char *shader_full_str = NULL;
    if (shader_is_vertex(ctx))
    {
        shader_str = "vp";
        shader_full_str = "vertex";
    } // if
    else if (shader_is_pixel(ctx))
    {
        shader_str = "fp";
        shader_full_str = "fragment";
    } // else if
    else
    {
        failf(ctx, "Shader type %u unsupported in this profile.",
              (uint) ctx->shader_type);
        return;
    } // if

    set_output(ctx, &ctx->preflight);

    if (strcmp(profilestr, MOJOSHADER_PROFILE_ARB1) == 0)
        output_line(ctx, "!!ARB%s1.0", shader_str);

    #if SUPPORT_PROFILE_ARB1_NV
    else if (strcmp(profilestr, MOJOSHADER_PROFILE_NV2) == 0)
    {
        ctx->profile_supports_nv2 = 1;
        output_line(ctx, "!!ARB%s1.0", shader_str);
        output_line(ctx, "OPTION NV_%s_program2;", shader_full_str);
    } // else if

    else if (strcmp(profilestr, MOJOSHADER_PROFILE_NV3) == 0)
    {
        // there's no NV_fragment_program3, so just use 2.
        const int ver = shader_is_pixel(ctx) ? 2 : 3;
        ctx->profile_supports_nv2 = 1;
        ctx->profile_supports_nv3 = 1;
        output_line(ctx, "!!ARB%s1.0", shader_str);
        output_line(ctx, "OPTION NV_%s_program%d;", shader_full_str, ver);
    } // else if

    else if (strcmp(profilestr, MOJOSHADER_PROFILE_NV4) == 0)
    {
        ctx->profile_supports_nv2 = 1;
        ctx->profile_supports_nv3 = 1;
        ctx->profile_supports_nv4 = 1;
        output_line(ctx, "!!NV%s4.0", shader_str);
    } // else if
    #endif

    else
    {
        failf(ctx, "Profile '%s' unsupported or unknown.", profilestr);
    } // else

    set_output(ctx, &ctx->mainline);
} // emit_ARB1_start

void emit_ARB1_end(Context *ctx)
{
    // ps_1_* writes color to r0 instead oC0. We move it to the right place.
    // We don't have to worry about a RET opcode messing this up, since
    //  RET isn't available before ps_2_0.
    if (shader_is_pixel(ctx) && !shader_version_atleast(ctx, 2, 0))
    {
        set_used_register(ctx, REG_TYPE_COLOROUT, 0, 1);
        output_line(ctx, "MOV oC0, r0;");
    } // if

    output_line(ctx, "END");
} // emit_ARB1_end

void emit_ARB1_phase(Context *ctx)
{
    // no-op in arb1.
} // emit_ARB1_phase

static inline const char *arb1_float_temp(const Context *ctx)
{
    // nv4 lets you specify data type.
    return (support_nv4(ctx)) ? "FLOAT TEMP" : "TEMP";
} // arb1_float_temp

void emit_ARB1_finalize(Context *ctx)
{
    push_output(ctx, &ctx->preflight);

    if (shader_is_vertex(ctx) && !ctx->arb1_wrote_position)
        output_line(ctx, "OPTION ARB_position_invariant;");

    if (shader_is_pixel(ctx) && ctx->have_multi_color_outputs)
        output_line(ctx, "OPTION ARB_draw_buffers;");

    pop_output(ctx);

    const char *tmpstr = arb1_float_temp(ctx);
    int i;
    push_output(ctx, &ctx->globals);
    for (i = 0; i < ctx->max_scratch_registers; i++)
    {
        char buf[64];
        allocate_ARB1_scratch_reg_name(ctx, buf, sizeof (buf));
        output_line(ctx, "%s %s;", tmpstr, buf);
    } // for

    // nv2 fragment programs (and anything nv4) have a real REP/ENDREP.
    if ( (support_nv2(ctx)) && (!shader_is_pixel(ctx)) && (!support_nv4(ctx)) )
    {
        // set up temps for nv2 REP/ENDREP emulation through branching.
        for (i = 0; i < ctx->max_reps; i++)
            output_line(ctx, "TEMP rep%d;", i);
    } // if

    pop_output(ctx);
    assert(ctx->scratch_registers == ctx->max_scratch_registers);
} // emit_ARB1_finalize

void emit_ARB1_global(Context *ctx, RegisterType regtype, int regnum)
{
    // !!! FIXME: dependency on ARB1 profile.  // !!! FIXME about FIXME: huh?
    char varname[64];
    get_ARB1_varname_in_buf(ctx, regtype, regnum, varname, sizeof (varname));

    push_output(ctx, &ctx->globals);
    switch (regtype)
    {
        case REG_TYPE_ADDRESS:
            if (shader_is_pixel(ctx))  // actually REG_TYPE_TEXTURE.
            {
                // We have to map texture registers to temps for ps_1_1, since
                //  they work like temps, initialize with tex coords, and the
                //  ps_1_1 TEX opcode expects to overwrite it.
                if (!shader_version_atleast(ctx, 1, 4))
                {
                    output_line(ctx, "%s %s;", arb1_float_temp(ctx), varname);
                    push_output(ctx, &ctx->mainline_top);
                    output_line(ctx, "MOV %s, fragment.texcoord[%d];",
                                varname, regnum);
                    pop_output(ctx);
                } // if
                break;
            } // if

            // nv4 replaced address registers with generic int registers.
            if (support_nv4(ctx))
                output_line(ctx, "INT TEMP %s;", varname);
            else
            {
                // nv2 has four-component address already, but stock arb1 has
                //  to emulate it in a temporary, and move components to the
                //  scalar ADDRESS register on demand.
                output_line(ctx, "ADDRESS %s;", varname);
                if (!support_nv2(ctx))
                    output_line(ctx, "TEMP addr%d;", regnum);
            } // else
            break;

        //case REG_TYPE_PREDICATE:
        //    output_line(ctx, "bvec4 %s;", varname);
        //    break;
        case REG_TYPE_TEMP:
            output_line(ctx, "%s %s;", arb1_float_temp(ctx), varname);
            break;
        //case REG_TYPE_LOOP:
        //    break; // no-op. We declare these in for loops at the moment.
        //case REG_TYPE_LABEL:
        //    break; // no-op. If we see it here, it means we optimized it out.
        default:
            fail(ctx, "BUG: we used a register we don't know how to define.");
            break;
    } // switch
    pop_output(ctx);
} // emit_ARB1_global

void emit_ARB1_array(Context *ctx, VariableList *var)
{
    // All uniforms are now packed tightly into the program.local array,
    //  instead of trying to map them to the d3d registers. So this needs to
    //  map to the next piece of the array we haven't used yet. Thankfully,
    //  arb1 lets you make a PARAM array that maps to a subset of another
    //  array; we don't need to do offsets, since myarray[0] can map to
    //  program.local[5] without any extra math from us.
    const int base = var->index;
    const int size = var->count;
    const int arb1base = ctx->uniform_float4_count +
                         ctx->uniform_int4_count +
                         ctx->uniform_bool_count;
    char varname[64];
    get_ARB1_const_array_varname_in_buf(ctx, base, size, varname, sizeof (varname));
    push_output(ctx, &ctx->globals);
    output_line(ctx, "PARAM %s[%d] = { program.local[%d..%d] };", varname,
                size, arb1base, (arb1base + size) - 1);
    pop_output(ctx);
    var->emit_position = arb1base;
} // emit_ARB1_array

void emit_ARB1_const_array(Context *ctx, const ConstantsList *clist,
                                  int base, int size)
{
    char varname[64];
    get_ARB1_const_array_varname_in_buf(ctx, base, size, varname, sizeof (varname));
    int i;

    push_output(ctx, &ctx->globals);
    output_line(ctx, "PARAM %s[%d] = {", varname, size);
    ctx->indent++;

    for (i = 0; i < size; i++)
    {
        while (clist->constant.type != MOJOSHADER_UNIFORM_FLOAT)
            clist = clist->next;
        assert(clist->constant.index == (base + i));

        char val0[32];
        char val1[32];
        char val2[32];
        char val3[32];
        floatstr(ctx, val0, sizeof (val0), clist->constant.value.f[0], 1);
        floatstr(ctx, val1, sizeof (val1), clist->constant.value.f[1], 1);
        floatstr(ctx, val2, sizeof (val2), clist->constant.value.f[2], 1);
        floatstr(ctx, val3, sizeof (val3), clist->constant.value.f[3], 1);

        output_line(ctx, "{ %s, %s, %s, %s }%s", val0, val1, val2, val3,
                    (i < (size-1)) ? "," : "");

        clist = clist->next;
    } // for

    ctx->indent--;
    output_line(ctx, "};");
    pop_output(ctx);
} // emit_ARB1_const_array

void emit_ARB1_uniform(Context *ctx, RegisterType regtype, int regnum,
                       const VariableList *var)
{
    // We pack these down into the program.local array, so if we only use
    //  register c439, it'll actually map to program.local[0]. This will
    //  prevent overflows when we actually have enough resources to run.

    const char *arrayname = "program.local";
    int index = 0;

    char varname[64];
    get_ARB1_varname_in_buf(ctx, regtype, regnum, varname, sizeof (varname));

    push_output(ctx, &ctx->globals);

    if (var == NULL)
    {
        // all types share one array (rather, all types convert to float4).
        index = ctx->uniform_float4_count + ctx->uniform_int4_count +
                ctx->uniform_bool_count;
    } // if

    else
    {
        const int arraybase = var->index;
        if (var->constant)
        {
            const int arraysize = var->count;
            arrayname = get_ARB1_const_array_varname_in_buf(ctx, arraybase,
                                        arraysize, (char *) alloca(64), 64);
            index = (regnum - arraybase);
        } // if
        else
        {
            assert(var->emit_position != -1);
            index = (regnum - arraybase) + var->emit_position;
        } // else
    } // else

    output_line(ctx, "PARAM %s = %s[%d];", varname, arrayname, index);
    pop_output(ctx);
} // emit_ARB1_uniform

void emit_ARB1_sampler(Context *ctx,int stage,TextureType ttype,int tb)
{
    // this is mostly a no-op...you don't predeclare samplers in arb1.

    if (tb)  // This sampler used a ps_1_1 TEXBEM opcode?
    {
        const int index = ctx->uniform_float4_count + ctx->uniform_int4_count +
                          ctx->uniform_bool_count;
        char var[64];
        get_ARB1_varname_in_buf(ctx, REG_TYPE_SAMPLER, stage, var, sizeof(var));
        push_output(ctx, &ctx->globals);
        output_line(ctx, "PARAM %s_texbem = program.local[%d];", var, index);
        output_line(ctx, "PARAM %s_texbeml = program.local[%d];", var, index+1);
        pop_output(ctx);
        ctx->uniform_float4_count += 2;
    } // if
} // emit_ARB1_sampler

// !!! FIXME: a lot of cut-and-paste here from emit_GLSL_attribute().
void emit_ARB1_attribute(Context *ctx, RegisterType regtype, int regnum,
                         MOJOSHADER_usage usage, int index, int wmask,
                         int flags)
{
    // !!! FIXME: this function doesn't deal with write masks at all yet!
    const char *usage_str = NULL;
    const char *arrayleft = "";
    const char *arrayright = "";
    char index_str[16] = { '\0' };

    char varname[64];
    get_ARB1_varname_in_buf(ctx, regtype, regnum, varname, sizeof (varname));

    //assert((flags & MOD_PP) == 0);  // !!! FIXME: is PP allowed?

    if (index != 0)  // !!! FIXME: a lot of these MUST be zero.
        snprintf(index_str, sizeof (index_str), "%u", (uint) index);

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

        // to avoid limitations of various GL entry points for input
        // attributes (glSecondaryColorPointer() can only take 3 component
        // items, glVertexPointer() can't do GL_UNSIGNED_BYTE, many other
        // issues), we set up all inputs as generic vertex attributes, so we
        // can pass data in just about any form, and ignore the built-in GLSL
        // attributes like gl_SecondaryColor. Output needs to use the the
        // built-ins, though, but we don't have to worry about the GL entry
        // point limitations there.

        if (regtype == REG_TYPE_INPUT)
        {
            const int attr = ctx->assigned_vertex_attributes++;
            push_output(ctx, &ctx->globals);
            output_line(ctx, "ATTRIB %s = vertex.attrib[%d];", varname, attr);
            pop_output(ctx);
        } // if

        else if (regtype == REG_TYPE_OUTPUT)
        {
            switch (usage)
            {
                case MOJOSHADER_USAGE_POSITION:
                    ctx->arb1_wrote_position = 1;
                    usage_str = "result.position";
                    break;
                case MOJOSHADER_USAGE_POINTSIZE:
                    usage_str = "result.pointsize";
                    break;
                case MOJOSHADER_USAGE_COLOR:
                    index_str[0] = '\0';  // no explicit number.
                    if (index == 0)
                        usage_str = "result.color.primary";
                    else if (index == 1)
                        usage_str = "result.color.secondary";
                    break;
                case MOJOSHADER_USAGE_FOG:
                    usage_str = "result.fogcoord";
                    break;
                case MOJOSHADER_USAGE_TEXCOORD:
                    snprintf(index_str, sizeof (index_str), "%u", (uint) index);
                    usage_str = "result.texcoord";
                    arrayleft = "[";
                    arrayright = "]";
                    break;
                default:
                    // !!! FIXME: we need to deal with some more built-in varyings here.
                    break;
            } // switch

            // !!! FIXME: the #define is a little hacky, but it means we don't
            // !!! FIXME:  have to track these separately if this works.
            push_output(ctx, &ctx->globals);
            // no mapping to built-in var? Just make it a regular global, pray.
            if (usage_str == NULL)
                output_line(ctx, "%s %s;", arb1_float_temp(ctx), varname);
            else
            {
                output_line(ctx, "OUTPUT %s = %s%s%s%s;", varname, usage_str,
                            arrayleft, index_str, arrayright);
            } // else
            pop_output(ctx);
        } // else if

        else
        {
            fail(ctx, "unknown vertex shader attribute register");
        } // else
    } // if

    else if (shader_is_pixel(ctx))
    {
        const char *paramtype_str = "ATTRIB";

        // samplers DCLs get handled in emit_ARB1_sampler().

        if (flags & MOD_CENTROID)
        {
            if (!support_nv4(ctx))  // GL_NV_fragment_program4 adds centroid.
            {
                // !!! FIXME: should we just wing it without centroid here?
                failf(ctx, "centroid unsupported in %s profile",
                      ctx->profile->name);
                return;
            } // if

            paramtype_str = "CENTROID ATTRIB";
        } // if

        if (regtype == REG_TYPE_COLOROUT)
        {
            paramtype_str = "OUTPUT";
            usage_str = "result.color";
            if (ctx->have_multi_color_outputs)
            {
                // We have to gamble that you have GL_ARB_draw_buffers.
                // You probably do at this point if you have a sane setup.
                snprintf(index_str, sizeof (index_str), "%u", (uint) regnum);
                arrayleft = "[";
                arrayright = "]";
            } // if
        } // if

        else if (regtype == REG_TYPE_DEPTHOUT)
        {
            paramtype_str = "OUTPUT";
            usage_str = "result.depth";
        } // else if

        // !!! FIXME: can you actualy have a texture register with COLOR usage?
        else if ((regtype == REG_TYPE_TEXTURE) || (regtype == REG_TYPE_INPUT))
        {
            if (usage == MOJOSHADER_USAGE_TEXCOORD)
            {
                // ps_1_1 does a different hack for this attribute.
                //  Refer to emit_ARB1_global()'s REG_TYPE_TEXTURE code.
                if (shader_version_atleast(ctx, 1, 4))
                {
                    snprintf(index_str, sizeof (index_str), "%u", (uint) index);
                    usage_str = "fragment.texcoord";
                    arrayleft = "[";
                    arrayright = "]";
                } // if
            } // if

            else if (usage == MOJOSHADER_USAGE_COLOR)
            {
                index_str[0] = '\0';  // no explicit number.
                if (index == 0)
                    usage_str = "fragment.color.primary";
                else if (index == 1)
                    usage_str = "fragment.color.secondary";
                else
                    fail(ctx, "unsupported color index");
            } // else if
        } // else if

        else if (regtype == REG_TYPE_MISCTYPE)
        {
            const MiscTypeType mt = (MiscTypeType) regnum;
            if (mt == MISCTYPE_TYPE_FACE)
            {
                if (support_nv4(ctx))  // FINALLY, a vFace equivalent in nv4!
                {
                    index_str[0] = '\0';  // no explicit number.
                    usage_str = "fragment.facing";
                } // if
                else
                {
                    failf(ctx, "vFace unsupported in %s profile",
                          ctx->profile->name);
                } // else
            } // if
            else if (mt == MISCTYPE_TYPE_POSITION)
            {
                index_str[0] = '\0';  // no explicit number.
                usage_str = "fragment.position";  // !!! FIXME: is this the same coord space as D3D?
            } // else if
            else
            {
                fail(ctx, "BUG: unhandled misc register");
            } // else
        } // else if

        else
        {
            fail(ctx, "unknown pixel shader attribute register");
        } // else

        if (usage_str != NULL)
        {
            push_output(ctx, &ctx->globals);
            output_line(ctx, "%s %s = %s%s%s%s;", paramtype_str, varname,
                        usage_str, arrayleft, index_str, arrayright);
            pop_output(ctx);
        } // if
    } // else if

    else
    {
        fail(ctx, "Unknown shader type");  // state machine should catch this.
    } // else
} // emit_ARB1_attribute

void emit_ARB1_RESERVED(Context *ctx) { /* no-op. */ }

void emit_ARB1_NOP(Context *ctx)
{
    // There is no NOP in arb1. Just don't output anything here.
} // emit_ARB1_NOP

EMIT_ARB1_OPCODE_DS_FUNC(MOV)
EMIT_ARB1_OPCODE_DSS_FUNC(ADD)
EMIT_ARB1_OPCODE_DSS_FUNC(SUB)
EMIT_ARB1_OPCODE_DSSS_FUNC(MAD)
EMIT_ARB1_OPCODE_DSS_FUNC(MUL)
EMIT_ARB1_OPCODE_DS_FUNC(RCP)

void emit_ARB1_RSQ(Context *ctx)
{
    // nv4 doesn't force abs() on this, so negative values will generate NaN.
    // The spec says you should force the abs() yourself.
    if (!support_nv4(ctx))
    {
        emit_ARB1_opcode_ds(ctx, "RSQ");  // pre-nv4 implies ABS.
        return;
    } // if

    // we can optimize this to use nv2's |abs| construct in some cases.
    if ( (ctx->source_args[0].src_mod == SRCMOD_NONE) ||
         (ctx->source_args[0].src_mod == SRCMOD_NEGATE) ||
         (ctx->source_args[0].src_mod == SRCMOD_ABSNEGATE) )
        ctx->source_args[0].src_mod = SRCMOD_ABS;

    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));

    if (ctx->source_args[0].src_mod == SRCMOD_ABS)
        output_line(ctx, "RSQ%s, %s;", dst, src0);
    else
    {
        char buf[64]; allocate_ARB1_scratch_reg_name(ctx, buf, sizeof (buf));
        output_line(ctx, "ABS %s, %s;", buf, src0);
        output_line(ctx, "RSQ%s, %s.x;", dst, buf);
    } // else

    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_RSQ

EMIT_ARB1_OPCODE_DSS_FUNC(DP3)
EMIT_ARB1_OPCODE_DSS_FUNC(DP4)
EMIT_ARB1_OPCODE_DSS_FUNC(MIN)
EMIT_ARB1_OPCODE_DSS_FUNC(MAX)
EMIT_ARB1_OPCODE_DSS_FUNC(SLT)
EMIT_ARB1_OPCODE_DSS_FUNC(SGE)

void emit_ARB1_EXP(Context *ctx) { emit_ARB1_opcode_ds(ctx, "EX2"); }

static void arb1_log(Context *ctx, const char *opcode)
{
    // !!! FIXME: SRCMOD_NEGATE can be made into SRCMOD_ABS here, too
    // we can optimize this to use nv2's |abs| construct in some cases.
    if ( (ctx->source_args[0].src_mod == SRCMOD_NONE) ||
         (ctx->source_args[0].src_mod == SRCMOD_ABSNEGATE) )
        ctx->source_args[0].src_mod = SRCMOD_ABS;

    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));

    if (ctx->source_args[0].src_mod == SRCMOD_ABS)
        output_line(ctx, "%s%s, %s;", opcode, dst, src0);
    else
    {
        char buf[64]; allocate_ARB1_scratch_reg_name(ctx, buf, sizeof (buf));
        output_line(ctx, "ABS %s, %s;", buf, src0);
        output_line(ctx, "%s%s, %s.x;", opcode, dst, buf);
    } // else

    emit_ARB1_dest_modifiers(ctx);
} // arb1_log


void emit_ARB1_LOG(Context *ctx)
{
    arb1_log(ctx, "LG2");
} // emit_ARB1_LOG


EMIT_ARB1_OPCODE_DS_FUNC(LIT)
EMIT_ARB1_OPCODE_DSS_FUNC(DST)

void emit_ARB1_LRP(Context *ctx)
{
    if (shader_is_pixel(ctx))  // fragment shaders have a matching LRP opcode.
        emit_ARB1_opcode_dsss(ctx, "LRP");
    else
    {
        char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
        char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
        char src1[64]; make_ARB1_srcarg_string(ctx, 1, src1, sizeof (src1));
        char src2[64]; make_ARB1_srcarg_string(ctx, 2, src2, sizeof (src2));
        char buf[64]; allocate_ARB1_scratch_reg_name(ctx, buf, sizeof (buf));

        // LRP is: dest = src2 + src0 * (src1 - src2)
        output_line(ctx, "SUB %s, %s, %s;", buf, src1, src2);
        output_line(ctx, "MAD%s, %s, %s, %s;", dst, buf, src0, src2);
        emit_ARB1_dest_modifiers(ctx);
    } // else
} // emit_ARB1_LRP

EMIT_ARB1_OPCODE_DS_FUNC(FRC)

static void arb1_MxXy(Context *ctx, const int x, const int y)
{
    DestArgInfo *dstarg = &ctx->dest_arg;
    const int origmask = dstarg->writemask;
    char src0[64];
    int i;

    make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));

    for (i = 0; i < y; i++)
    {
        char dst[64];
        char row[64];
        make_ARB1_srcarg_string(ctx, i + 1, row, sizeof (row));
        set_dstarg_writemask(dstarg, 1 << i);
        make_ARB1_destarg_string(ctx, dst, sizeof (dst));
        output_line(ctx, "DP%d%s, %s, %s;", x, dst, src0, row);
    } // for

    set_dstarg_writemask(dstarg, origmask);
    emit_ARB1_dest_modifiers(ctx);
} // arb1_MxXy

void emit_ARB1_M4X4(Context *ctx) { arb1_MxXy(ctx, 4, 4); }
void emit_ARB1_M4X3(Context *ctx) { arb1_MxXy(ctx, 4, 3); }
void emit_ARB1_M3X4(Context *ctx) { arb1_MxXy(ctx, 3, 4); }
void emit_ARB1_M3X3(Context *ctx) { arb1_MxXy(ctx, 3, 3); }
void emit_ARB1_M3X2(Context *ctx) { arb1_MxXy(ctx, 3, 2); }

void emit_ARB1_CALL(Context *ctx)
{
    if (!support_nv2(ctx))  // no branching in stock ARB1.
    {
        failf(ctx, "branching unsupported in %s profile", ctx->profile->name);
        return;
    } // if

    char labelstr[64];
    get_ARB1_srcarg_varname(ctx, 0, labelstr, sizeof (labelstr));
    output_line(ctx, "CAL %s;", labelstr);
} // emit_ARB1_CALL

void emit_ARB1_CALLNZ(Context *ctx)
{
    // !!! FIXME: if src1 is a constbool that's true, we can remove the
    // !!! FIXME:  if. If it's false, we can make this a no-op.

    if (!support_nv2(ctx))  // no branching in stock ARB1.
        failf(ctx, "branching unsupported in %s profile", ctx->profile->name);
    else
    {
        // !!! FIXME: double-check this.
        char labelstr[64];
        char scratch[64];
        char src1[64];
        get_ARB1_srcarg_varname(ctx, 0, labelstr, sizeof (labelstr));
        get_ARB1_srcarg_varname(ctx, 1, src1, sizeof (src1));
        allocate_ARB1_scratch_reg_name(ctx, scratch, sizeof (scratch));
        output_line(ctx, "MOVC %s, %s;", scratch, src1);
        output_line(ctx, "CAL %s (NE.x);", labelstr);
    } // else
} // emit_ARB1_CALLNZ

// !!! FIXME: needs BRA in nv2, LOOP in nv2 fragment progs, and REP in nv4.
EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(LOOP)

void emit_ARB1_RET(Context *ctx)
{
    // don't fail() if no nv2...maybe we're just ending the mainline?
    //  if we're ending a LABEL that had no CALL, this would all be written
    //  to ctx->ignore anyhow, so this should be "safe" ... arb1 profile will
    //  just end up throwing all this code out.
    if (support_nv2(ctx))  // no branching in stock ARB1.
        output_line(ctx, "RET;");
    set_output(ctx, &ctx->mainline); // in case we were ignoring this function.
} // emit_ARB1_RET


EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(ENDLOOP)

void emit_ARB1_LABEL(Context *ctx)
{
    if (!support_nv2(ctx))  // no branching in stock ARB1.
        return;  // don't fail()...maybe we never use it, but do fail in CALL.

    const int label = ctx->source_args[0].regnum;
    RegisterList *reg = reglist_find(&ctx->used_registers, REG_TYPE_LABEL, label);

    // MSDN specs say CALL* has to come before the LABEL, so we know if we
    //  can ditch the entire function here as unused.
    if (reg == NULL)
        set_output(ctx, &ctx->ignore);  // Func not used. Parse, but don't output.

    // !!! FIXME: it would be nice if we could determine if a function is
    // !!! FIXME:  only called once and, if so, forcibly inline it.

    //const char *uses_loopreg = ((reg) && (reg->misc == 1)) ? "int aL" : "";
    char labelstr[64];
    get_ARB1_srcarg_varname(ctx, 0, labelstr, sizeof (labelstr));
    output_line(ctx, "%s:", labelstr);
} // emit_ARB1_LABEL


void emit_ARB1_POW(Context *ctx)
{
    // we can optimize this to use nv2's |abs| construct in some cases.
    if ( (ctx->source_args[0].src_mod == SRCMOD_NONE) ||
         (ctx->source_args[0].src_mod == SRCMOD_ABSNEGATE) )
        ctx->source_args[0].src_mod = SRCMOD_ABS;

    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_ARB1_srcarg_string(ctx, 1, src1, sizeof (src1));

    if (ctx->source_args[0].src_mod == SRCMOD_ABS)
        output_line(ctx, "POW%s, %s, %s;", dst, src0, src1);
    else
    {
        char buf[64]; allocate_ARB1_scratch_reg_name(ctx, buf, sizeof (buf));
        output_line(ctx, "ABS %s, %s;", buf, src0);
        output_line(ctx, "POW%s, %s.x, %s;", dst, buf, src1);
    } // else

    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_POW

void emit_ARB1_CRS(Context *ctx) { emit_ARB1_opcode_dss(ctx, "XPD"); }

void emit_ARB1_SGN(Context *ctx)
{
    if (support_nv2(ctx))
        emit_ARB1_opcode_ds(ctx, "SSG");
    else
    {
        char dst[64];
        char src0[64];
        char scratch1[64];
        char scratch2[64];
        make_ARB1_destarg_string(ctx, dst, sizeof (dst));
        make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
        allocate_ARB1_scratch_reg_name(ctx, scratch1, sizeof (scratch1));
        allocate_ARB1_scratch_reg_name(ctx, scratch2, sizeof (scratch2));
        output_line(ctx, "SLT %s, %s, 0.0;", scratch1, src0);
        output_line(ctx, "SLT %s, -%s, 0.0;", scratch2, src0);
        output_line(ctx, "ADD%s -%s, %s;", dst, scratch1, scratch2);
        emit_ARB1_dest_modifiers(ctx);
    } // else
} // emit_ARB1_SGN

EMIT_ARB1_OPCODE_DS_FUNC(ABS)

void emit_ARB1_NRM(Context *ctx)
{
    // nv2 fragment programs (and anything nv4) have a real NRM.
    if ( (support_nv4(ctx)) || ((support_nv2(ctx)) && (shader_is_pixel(ctx))) )
        emit_ARB1_opcode_ds(ctx, "NRM");
    else
    {
        char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
        char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
        char buf[64]; allocate_ARB1_scratch_reg_name(ctx, buf, sizeof (buf));
        output_line(ctx, "DP3 %s.w, %s, %s;", buf, src0, src0);
        output_line(ctx, "RSQ %s.w, %s.w;", buf, buf);
        output_line(ctx, "MUL%s, %s.w, %s;", dst, buf, src0);
        emit_ARB1_dest_modifiers(ctx);
    } // else
} // emit_ARB1_NRM


void emit_ARB1_SINCOS(Context *ctx)
{
    // we don't care about the temp registers that <= sm2 demands; ignore them.
    const int mask = ctx->dest_arg.writemask;

    // arb1 fragment programs and everything nv4 have sin/cos/sincos opcodes.
    if ((shader_is_pixel(ctx)) || (support_nv4(ctx)))
    {
        char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
        char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
        if (writemask_x(mask))
            output_line(ctx, "COS%s, %s;", dst, src0);
        else if (writemask_y(mask))
            output_line(ctx, "SIN%s, %s;", dst, src0);
        else if (writemask_xy(mask))
            output_line(ctx, "SCS%s, %s;", dst, src0);
    } // if

    // nv2+ profiles have sin and cos opcodes.
    else if (support_nv2(ctx))
    {
        char dst[64]; get_ARB1_destarg_varname(ctx, dst, sizeof (dst));
        char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
        if (writemask_x(mask))
            output_line(ctx, "COS %s.x, %s;", dst, src0);
        else if (writemask_y(mask))
            output_line(ctx, "SIN %s.y, %s;", dst, src0);
        else if (writemask_xy(mask))
        {
            output_line(ctx, "SIN %s.x, %s;", dst, src0);
            output_line(ctx, "COS %s.y, %s;", dst, src0);
        } // else if
    } // if

    else  // big nasty.
    {
        char dst[64]; get_ARB1_destarg_varname(ctx, dst, sizeof (dst));
        char src0[64]; get_ARB1_srcarg_varname(ctx, 0, src0, sizeof (src0));
        const int need_sin = (writemask_x(mask) || writemask_xy(mask));
        const int need_cos = (writemask_y(mask) || writemask_xy(mask));
        char scratch[64];

        if (need_sin || need_cos)
            allocate_ARB1_scratch_reg_name(ctx, scratch, sizeof (scratch));

        // These sin() and cos() approximations originally found here:
        //    http://www.devmaster.net/forums/showthread.php?t=5784
        //
        // const float B = 4.0f / M_PI;
        // const float C = -4.0f / (M_PI * M_PI);
        // float y = B * x + C * x * fabs(x);
        //
        // // optional better precision...
        // const float P = 0.225f;
        // y = P * (y * fabs(y) - y) + y;
        //
        //
        // That first thing can be reduced to:
        // const float y = ((1.2732395447351626861510701069801f * x) +
        //             ((-0.40528473456935108577551785283891f * x) * fabs(x)));

        if (need_sin)
        {
            // !!! FIXME: use SRCMOD_ABS here?
            output_line(ctx, "ABS %s.x, %s.x;", dst, src0);
            output_line(ctx, "MUL %s.x, %s.x, -0.40528473456935108577551785283891;", dst, dst);
            output_line(ctx, "MUL %s.x, %s.x, 1.2732395447351626861510701069801;", scratch, src0);
            output_line(ctx, "MAD %s.x, %s.x, %s.x, %s.x;", dst, dst, src0, scratch);
        } // if

        // cosine is sin(x + M_PI/2), but you have to wrap x to pi:
        //  if (x+(M_PI/2) > M_PI)
        //      x -= 2 * M_PI;
        //
        // which is...
        //  if (x+(1.57079637050628662109375) > 3.1415927410125732421875)
        //      x += -6.283185482025146484375;

        if (need_cos)
        {
            output_line(ctx, "ADD %s.x, %s.x, 1.57079637050628662109375;", scratch, src0);
            output_line(ctx, "SGE %s.y, %s.x, 3.1415927410125732421875;", scratch, scratch);
            output_line(ctx, "MAD %s.x, %s.y, -6.283185482025146484375, %s.x;", scratch, scratch, scratch);
            output_line(ctx, "ABS %s.x, %s.x;", dst, src0);
            output_line(ctx, "MUL %s.x, %s.x, -0.40528473456935108577551785283891;", dst, dst);
            output_line(ctx, "MUL %s.x, %s.x, 1.2732395447351626861510701069801;", scratch, src0);
            output_line(ctx, "MAD %s.y, %s.x, %s.x, %s.x;", dst, dst, src0, scratch);
        } // if
    } // else

    // !!! FIXME: might not have done anything. Don't emit if we didn't.
    if (!(ctx->isfail))
        emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_SINCOS


void emit_ARB1_REP(Context *ctx)
{
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));

    // nv2 fragment programs (and everything nv4) have a real REP.
    if ( (support_nv4(ctx)) || ((support_nv2(ctx)) && (shader_is_pixel(ctx))) )
        output_line(ctx, "REP %s;", src0);

    else if (support_nv2(ctx))
    {
        // no REP, but we can use branches.
        char failbranch[32];
        char topbranch[32];
        const int toplabel = allocate_branch_label(ctx);
        const int faillabel = allocate_branch_label(ctx);
        get_ARB1_branch_label_name(ctx,faillabel,failbranch,sizeof(failbranch));
        get_ARB1_branch_label_name(ctx,toplabel,topbranch,sizeof(topbranch));

        assert(((size_t) ctx->branch_labels_stack_index) <
                STATICARRAYLEN(ctx->branch_labels_stack)-1);

        ctx->branch_labels_stack[ctx->branch_labels_stack_index++] = toplabel;
        ctx->branch_labels_stack[ctx->branch_labels_stack_index++] = faillabel;

        char scratch[32];
        snprintf(scratch, sizeof (scratch), "rep%d", ctx->reps);
        output_line(ctx, "MOVC %s.x, %s;", scratch, src0);
        output_line(ctx, "BRA %s (LE.x);", failbranch);
        output_line(ctx, "%s:", topbranch);
    } // else if

    else  // stock ARB1 has no branching.
    {
        fail(ctx, "branching unsupported in this profile");
    } // else
} // emit_ARB1_REP


void emit_ARB1_ENDREP(Context *ctx)
{
    // nv2 fragment programs (and everything nv4) have a real ENDREP.
    if ( (support_nv4(ctx)) || ((support_nv2(ctx)) && (shader_is_pixel(ctx))) )
        output_line(ctx, "ENDREP;");

    else if (support_nv2(ctx))
    {
        // no ENDREP, but we can use branches.
        assert(ctx->branch_labels_stack_index >= 2);

        char failbranch[32];
        char topbranch[32];
        const int faillabel = ctx->branch_labels_stack[--ctx->branch_labels_stack_index];
        const int toplabel = ctx->branch_labels_stack[--ctx->branch_labels_stack_index];
        get_ARB1_branch_label_name(ctx,faillabel,failbranch,sizeof(failbranch));
        get_ARB1_branch_label_name(ctx,toplabel,topbranch,sizeof(topbranch));

        char scratch[32];
        snprintf(scratch, sizeof (scratch), "rep%d", ctx->reps);
        output_line(ctx, "SUBC %s.x, %s.x, 1.0;", scratch, scratch);
        output_line(ctx, "BRA %s (GT.x);", topbranch);
        output_line(ctx, "%s:", failbranch);
    } // else if

    else  // stock ARB1 has no branching.
    {
        fail(ctx, "branching unsupported in this profile");
    } // else
} // emit_ARB1_ENDREP


void nv2_if(Context *ctx)
{
    // The condition code register MUST be set up before this!
    // nv2 fragment programs (and everything nv4) have a real IF.
    if ( (support_nv4(ctx)) || (shader_is_pixel(ctx)) )
        output_line(ctx, "IF EQ.x;");
    else
    {
        // there's no IF construct, but we can use a branch to a label.
        char failbranch[32];
        const int label = allocate_branch_label(ctx);
        get_ARB1_branch_label_name(ctx, label, failbranch, sizeof (failbranch));

        assert(((size_t) ctx->branch_labels_stack_index)
                 < STATICARRAYLEN(ctx->branch_labels_stack));

        ctx->branch_labels_stack[ctx->branch_labels_stack_index++] = label;

        // !!! FIXME: should this be NE? (EQ would jump to the ELSE for the IF condition, right?).
        output_line(ctx, "BRA %s (EQ.x);", failbranch);
    } // else
} // nv2_if


void emit_ARB1_IF(Context *ctx)
{
    if (support_nv2(ctx))
    {
        char buf[64]; allocate_ARB1_scratch_reg_name(ctx, buf, sizeof (buf));
        char src0[64]; get_ARB1_srcarg_varname(ctx, 0, src0, sizeof (src0));
        output_line(ctx, "MOVC %s.x, %s;", buf, src0);
        nv2_if(ctx);
    } // if

    else  // stock ARB1 has no branching.
    {
        failf(ctx, "branching unsupported in %s profile", ctx->profile->name);
    } // else
} // emit_ARB1_IF


void emit_ARB1_ELSE(Context *ctx)
{
    // nv2 fragment programs (and everything nv4) have a real ELSE.
    if ( (support_nv4(ctx)) || ((support_nv2(ctx)) && (shader_is_pixel(ctx))) )
        output_line(ctx, "ELSE;");

    else if (support_nv2(ctx))
    {
        // there's no ELSE construct, but we can use a branch to a label.
        assert(ctx->branch_labels_stack_index > 0);

        // At the end of the IF block, unconditionally jump to the ENDIF.
        const int endlabel = allocate_branch_label(ctx);
        char endbranch[32];
        get_ARB1_branch_label_name(ctx,endlabel,endbranch,sizeof (endbranch));
        output_line(ctx, "BRA %s;", endbranch);

        // Now mark the ELSE section with a lable.
        const int elselabel = ctx->branch_labels_stack[ctx->branch_labels_stack_index-1];
        char elsebranch[32];
        get_ARB1_branch_label_name(ctx,elselabel,elsebranch,sizeof(elsebranch));
        output_line(ctx, "%s:", elsebranch);

        // Replace the ELSE label with the ENDIF on the label stack.
        ctx->branch_labels_stack[ctx->branch_labels_stack_index-1] = endlabel;
    } // else if

    else  // stock ARB1 has no branching.
    {
        failf(ctx, "branching unsupported in %s profile", ctx->profile->name);
    } // else
} // emit_ARB1_ELSE


void emit_ARB1_ENDIF(Context *ctx)
{
    // nv2 fragment programs (and everything nv4) have a real ENDIF.
    if ( (support_nv4(ctx)) || ((support_nv2(ctx)) && (shader_is_pixel(ctx))) )
        output_line(ctx, "ENDIF;");

    else if (support_nv2(ctx))
    {
        // there's no ENDIF construct, but we can use a branch to a label.
        assert(ctx->branch_labels_stack_index > 0);
        const int endlabel = ctx->branch_labels_stack[--ctx->branch_labels_stack_index];
        char endbranch[32];
        get_ARB1_branch_label_name(ctx,endlabel,endbranch,sizeof (endbranch));
        output_line(ctx, "%s:", endbranch);
    } // if

    else  // stock ARB1 has no branching.
    {
        failf(ctx, "branching unsupported in %s profile", ctx->profile->name);
    } // else
} // emit_ARB1_ENDIF


void emit_ARB1_BREAK(Context *ctx)
{
    // nv2 fragment programs (and everything nv4) have a real BREAK.
    if ( (support_nv4(ctx)) || ((support_nv2(ctx)) && (shader_is_pixel(ctx))) )
        output_line(ctx, "BRK;");

    else if (support_nv2(ctx))
    {
        // no BREAK, but we can use branches.
        assert(ctx->branch_labels_stack_index >= 2);
        const int faillabel = ctx->branch_labels_stack[ctx->branch_labels_stack_index];
        char failbranch[32];
        get_ARB1_branch_label_name(ctx,faillabel,failbranch,sizeof(failbranch));
        output_line(ctx, "BRA %s;", failbranch);
    } // else if

    else  // stock ARB1 has no branching.
    {
        failf(ctx, "branching unsupported in %s profile", ctx->profile->name);
    } // else
} // emit_ARB1_BREAK


void emit_ARB1_MOVA(Context *ctx)
{
    // nv2 and nv3 can use the ARR opcode.
    // But nv4 removed ARR (and ADDRESS registers!). Just ROUND to an INT.
    if (support_nv4(ctx))
        emit_ARB1_opcode_ds(ctx, "ROUND.S");  // !!! FIXME: don't use a modifier here.
    else if ((support_nv2(ctx)) || (support_nv3(ctx)))
        emit_ARB1_opcode_ds(ctx, "ARR");
    else
    {
        char src0[64];
        char scratch[64];
        char addr[32];

        make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
        allocate_ARB1_scratch_reg_name(ctx, scratch, sizeof (scratch));
        snprintf(addr, sizeof (addr), "addr%d", ctx->dest_arg.regnum);

        // !!! FIXME: we can optimize this if src_mod is ABS or ABSNEGATE.

        // ARL uses floor(), but D3D expects round-to-nearest.
        // There is probably a more efficient way to do this.
        if (shader_is_pixel(ctx))  // CMP only exists in fragment programs.  :/
            output_line(ctx, "CMP %s, %s, -1.0, 1.0;", scratch, src0);
        else
        {
            output_line(ctx, "SLT %s, %s, 0.0;", scratch, src0);
            output_line(ctx, "MAD %s, %s, -2.0, 1.0;", scratch, scratch);
        } // else

        output_line(ctx, "ABS %s, %s;", addr, src0);
        output_line(ctx, "ADD %s, %s, 0.5;", addr, addr);
        output_line(ctx, "FLR %s, %s;", addr, addr);
        output_line(ctx, "MUL %s, %s, %s;", addr, addr, scratch);

        // we don't handle these right now, since emit_ARB1_dest_modifiers(ctx)
        //  wants to look at dest_arg, not our temp register.
        assert(ctx->dest_arg.result_mod == 0);
        assert(ctx->dest_arg.result_shift == 0);

        // we assign to the actual address register as needed.
        ctx->last_address_reg_component = -1;
    } // else
} // emit_ARB1_MOVA


void emit_ARB1_TEXKILL(Context *ctx)
{
    // d3d kills on xyz, arb1 kills on xyzw. Fix the swizzle.
    //  We just map the x component to w. If it's negative, the fragment
    //  would discard anyhow, otherwise, it'll pass through okay. This saves
    //  us a temp register.
    char dst[64];
    get_ARB1_destarg_varname(ctx, dst, sizeof (dst));
    output_line(ctx, "KIL %s.xyzx;", dst);
} // emit_ARB1_TEXKILL

static void arb1_texbem(Context *ctx, const int luminance)
{
    // !!! FIXME: this code counts on the register not having swizzles, etc.
    const int stage = ctx->dest_arg.regnum;
    char dst[64]; get_ARB1_destarg_varname(ctx, dst, sizeof (dst));
    char src[64]; get_ARB1_srcarg_varname(ctx, 0, src, sizeof (src));
    char tmp[64]; allocate_ARB1_scratch_reg_name(ctx, tmp, sizeof (tmp));
    char sampler[64];
    get_ARB1_varname_in_buf(ctx, REG_TYPE_SAMPLER, stage,
                            sampler, sizeof (sampler));

    output_line(ctx, "MUL %s, %s_texbem.xzyw, %s.xyxy;", tmp, sampler, src);
    output_line(ctx, "ADD %s.xy, %s.xzxx, %s.ywxx;", tmp, tmp, tmp);
    output_line(ctx, "ADD %s.xy, %s, %s;", tmp, tmp, dst);
    output_line(ctx, "TEX %s, %s, texture[%d], 2D;", dst, tmp, stage);

    if (luminance)  // TEXBEML, not just TEXBEM?
    {
        output_line(ctx, "MAD %s, %s.zzzz, %s_texbeml.xxxx, %s_texbeml.yyyy;",
                    tmp, src, sampler, sampler);
        output_line(ctx, "MUL %s, %s, %s;", dst, dst, tmp);
    } // if

    emit_ARB1_dest_modifiers(ctx);
} // arb1_texbem

void emit_ARB1_TEXBEM(Context *ctx)
{
    arb1_texbem(ctx, 0);
} // emit_ARB1_TEXBEM

void emit_ARB1_TEXBEML(Context *ctx)
{
    arb1_texbem(ctx, 1);
} // emit_ARB1_TEXBEML

EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2AR)
EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2GB)


void emit_ARB1_TEXM3X2PAD(Context *ctx)
{
    // no-op ... work happens in emit_ARB1_TEXM3X2TEX().
} // emit_ARB1_TEXM3X2PAD

void emit_ARB1_TEXM3X2TEX(Context *ctx)
{
    if (ctx->texm3x2pad_src0 == -1)
        return;

    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    const int stage = ctx->dest_arg.regnum;
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x2pad_src0,
                            src0, sizeof (src0));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x2pad_dst0,
                            src1, sizeof (src1));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src2, sizeof (src2));
    get_ARB1_destarg_varname(ctx, dst, sizeof (dst));

    output_line(ctx, "DP3 %s.y, %s, %s;", dst, src2, dst);
    output_line(ctx, "DP3 %s.x, %s, %s;", dst, src0, src1);
    output_line(ctx, "TEX %s, %s, texture[%d], 2D;", dst, dst, stage);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_TEXM3X2TEX


void emit_ARB1_TEXM3X3PAD(Context *ctx)
{
    // no-op ... work happens in emit_ARB1_TEXM3X3*().
} // emit_ARB1_TEXM3X3PAD


void emit_ARB1_TEXM3X3TEX(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char src3[64];
    char src4[64];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    const int stage = ctx->dest_arg.regnum;
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0,
                            src0, sizeof (src0));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0,
                            src1, sizeof (src1));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1,
                            src2, sizeof (src2));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1,
                            src3, sizeof (src3));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src4, sizeof (src4));
    get_ARB1_destarg_varname(ctx, dst, sizeof (dst));

    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, stage);
    const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);
    const char *ttypestr = (ttype == TEXTURE_TYPE_CUBE) ? "CUBE" : "3D";

    output_line(ctx, "DP3 %s.z, %s, %s;", dst, dst, src4);
    output_line(ctx, "DP3 %s.x, %s, %s;", dst, src0, src1);
    output_line(ctx, "DP3 %s.y, %s, %s;", dst, src2, src3);
    output_line(ctx, "TEX %s, %s, texture[%d], %s;", dst, dst, stage, ttypestr);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_TEXM3X3TEX

void emit_ARB1_TEXM3X3SPEC(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char src3[64];
    char src4[64];
    char src5[64];
    char tmp[64];
    char tmp2[64];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    const int stage = ctx->dest_arg.regnum;
    allocate_ARB1_scratch_reg_name(ctx, tmp, sizeof (tmp));
    allocate_ARB1_scratch_reg_name(ctx, tmp2, sizeof (tmp2));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0,
                            src0, sizeof (src0));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0,
                            src1, sizeof (src1));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1,
                            src2, sizeof (src2));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1,
                            src3, sizeof (src3));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src4, sizeof (src4));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[1].regnum,
                            src5, sizeof (src5));
    get_ARB1_destarg_varname(ctx, dst, sizeof (dst));

    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, stage);
    const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);
    const char *ttypestr = (ttype == TEXTURE_TYPE_CUBE) ? "CUBE" : "3D";

    output_line(ctx, "DP3 %s.z, %s, %s;", dst, dst, src4);
    output_line(ctx, "DP3 %s.x, %s, %s;", dst, src0, src1);
    output_line(ctx, "DP3 %s.y, %s, %s;", dst, src2, src3);
    output_line(ctx, "MUL %s, %s, %s;", tmp, dst, dst);    // normal * normal
    output_line(ctx, "MUL %s, %s, %s;", tmp2, dst, src5);  // normal * eyeray

    // !!! FIXME: This is goofy. There's got to be a way to do vector-wide
    // !!! FIXME:  divides or reciprocals...right?
    output_line(ctx, "RCP %s.x, %s.x;", tmp2, tmp2);
    output_line(ctx, "RCP %s.y, %s.y;", tmp2, tmp2);
    output_line(ctx, "RCP %s.z, %s.z;", tmp2, tmp2);
    output_line(ctx, "RCP %s.w, %s.w;", tmp2, tmp2);
    output_line(ctx, "MUL %s, %s, %s;", tmp, tmp, tmp2);

    output_line(ctx, "MUL %s, %s, { 2.0, 2.0, 2.0, 2.0 };", tmp, tmp);
    output_line(ctx, "MAD %s, %s, %s, -%s;", tmp, tmp, dst, src5);
    output_line(ctx, "TEX %s, %s, texture[%d], %s;", dst, tmp, stage, ttypestr);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_TEXM3X3SPEC

void emit_ARB1_TEXM3X3VSPEC(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char src3[64];
    char src4[64];
    char tmp[64];
    char tmp2[64];
    char tmp3[64];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    const int stage = ctx->dest_arg.regnum;
    allocate_ARB1_scratch_reg_name(ctx, tmp, sizeof (tmp));
    allocate_ARB1_scratch_reg_name(ctx, tmp2, sizeof (tmp2));
    allocate_ARB1_scratch_reg_name(ctx, tmp3, sizeof (tmp3));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0,
                            src0, sizeof (src0));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0,
                            src1, sizeof (src1));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1,
                            src2, sizeof (src2));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1,
                            src3, sizeof (src3));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src4, sizeof (src4));
    get_ARB1_destarg_varname(ctx, dst, sizeof (dst));

    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, stage);
    const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);
    const char *ttypestr = (ttype == TEXTURE_TYPE_CUBE) ? "CUBE" : "3D";

    output_line(ctx, "MOV %s.x, %s.w;", tmp3, src0);
    output_line(ctx, "MOV %s.y, %s.w;", tmp3, src2);
    output_line(ctx, "MOV %s.z, %s.w;", tmp3, dst);
    output_line(ctx, "DP3 %s.z, %s, %s;", dst, dst, src4);
    output_line(ctx, "DP3 %s.x, %s, %s;", dst, src0, src1);
    output_line(ctx, "DP3 %s.y, %s, %s;", dst, src2, src3);
    output_line(ctx, "MUL %s, %s, %s;", tmp, dst, dst);    // normal * normal
    output_line(ctx, "MUL %s, %s, %s;", tmp2, dst, tmp3);  // normal * eyeray

    // !!! FIXME: This is goofy. There's got to be a way to do vector-wide
    // !!! FIXME:  divides or reciprocals...right?
    output_line(ctx, "RCP %s.x, %s.x;", tmp2, tmp2);
    output_line(ctx, "RCP %s.y, %s.y;", tmp2, tmp2);
    output_line(ctx, "RCP %s.z, %s.z;", tmp2, tmp2);
    output_line(ctx, "RCP %s.w, %s.w;", tmp2, tmp2);
    output_line(ctx, "MUL %s, %s, %s;", tmp, tmp, tmp2);

    output_line(ctx, "MUL %s, %s, { 2.0, 2.0, 2.0, 2.0 };", tmp, tmp);
    output_line(ctx, "MAD %s, %s, %s, -%s;", tmp, tmp, dst, tmp3);
    output_line(ctx, "TEX %s, %s, texture[%d], %s;", dst, tmp, stage, ttypestr);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_TEXM3X3VSPEC

void emit_ARB1_EXPP(Context *ctx) { emit_ARB1_opcode_ds(ctx, "EX2"); }
void emit_ARB1_LOGP(Context *ctx) { arb1_log(ctx, "LG2"); }

void emit_ARB1_CND(Context *ctx)
{
    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_ARB1_srcarg_string(ctx, 1, src1, sizeof (src1));
    char src2[64]; make_ARB1_srcarg_string(ctx, 2, src2, sizeof (src2));
    char tmp[64]; allocate_ARB1_scratch_reg_name(ctx, tmp, sizeof (tmp));

    // CND compares against 0.5, but we need to compare against 0.0...
    //  ...subtract to make up the difference.
    output_line(ctx, "SUB %s, %s, { 0.5, 0.5, 0.5, 0.5 };", tmp, src0);
    // D3D tests (src0 >= 0.0), but ARB1 tests (src0 < 0.0) ... so just
    //  switch src1 and src2 to get the same results.
    output_line(ctx, "CMP%s, %s, %s, %s;", dst, tmp, src2, src1);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_CND

EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2RGB)
EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(TEXDP3TEX)
EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(TEXM3X2DEPTH)
EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(TEXDP3)

void emit_ARB1_TEXM3X3(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char src3[64];
    char src4[64];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0,
                            src0, sizeof (src0));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0,
                            src1, sizeof (src1));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1,
                            src2, sizeof (src2));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1,
                            src3, sizeof (src3));
    get_ARB1_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src4, sizeof (src4));
    get_ARB1_destarg_varname(ctx, dst, sizeof (dst));

    output_line(ctx, "DP3 %s.z, %s, %s;", dst, dst, src4);
    output_line(ctx, "DP3 %s.x, %s, %s;", dst, src0, src1);
    output_line(ctx, "DP3 %s.y, %s, %s;", dst, src2, src3);
    output_line(ctx, "MOV %s.w, { 1.0, 1.0, 1.0, 1.0 };", dst);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_TEXM3X3

EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(TEXDEPTH)

void emit_ARB1_CMP(Context *ctx)
{
    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_ARB1_srcarg_string(ctx, 1, src1, sizeof (src1));
    char src2[64]; make_ARB1_srcarg_string(ctx, 2, src2, sizeof (src2));
    // D3D tests (src0 >= 0.0), but ARB1 tests (src0 < 0.0) ... so just
    //  switch src1 and src2 to get the same results.
    output_line(ctx, "CMP%s, %s, %s, %s;", dst, src0, src2, src1);
    emit_ARB1_dest_modifiers(ctx);
} // emit_ARB1_CMP

EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(BEM)


void emit_ARB1_DP2ADD(Context *ctx)
{
    if (support_nv4(ctx))  // nv4 has a built-in equivalent to DP2ADD.
        emit_ARB1_opcode_dsss(ctx, "DP2A");
    else
    {
        char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));
        char src0[64]; make_ARB1_srcarg_string(ctx, 0, src0, sizeof (src0));
        char src1[64]; make_ARB1_srcarg_string(ctx, 1, src1, sizeof (src1));
        char src2[64]; make_ARB1_srcarg_string(ctx, 2, src2, sizeof (src2));
        char scratch[64];

        // DP2ADD is:
        //  dst = (src0.r * src1.r) + (src0.g * src1.g) + src2.replicate_swiz
        allocate_ARB1_scratch_reg_name(ctx, scratch, sizeof (scratch));
        output_line(ctx, "MUL %s, %s, %s;", scratch, src0, src1);
        output_line(ctx, "ADD %s, %s.x, %s.y;", scratch, scratch, scratch);
        output_line(ctx, "ADD%s, %s.x, %s;", dst, scratch, src2);
        emit_ARB1_dest_modifiers(ctx);
    } // else
} // emit_ARB1_DP2ADD


void emit_ARB1_DSX(Context *ctx)
{
    if (support_nv2(ctx))  // nv2 has a built-in equivalent to DSX.
        emit_ARB1_opcode_ds(ctx, "DDX");
    else
        failf(ctx, "DSX unsupported in %s profile", ctx->profile->name);
} // emit_ARB1_DSX


void emit_ARB1_DSY(Context *ctx)
{
    if (support_nv2(ctx))  // nv2 has a built-in equivalent to DSY.
        emit_ARB1_opcode_ds(ctx, "DDY");
    else
        failf(ctx, "DSY unsupported in %s profile", ctx->profile->name);
} // emit_ARB1_DSY

static void arb1_texld(Context *ctx, const char *opcode, const int texldd)
{
    // !!! FIXME: Hack: "TEXH" is invalid in nv4. Fix this more cleanly.
    if ((ctx->dest_arg.result_mod & MOD_PP) && (support_nv4(ctx)))
        ctx->dest_arg.result_mod &= ~MOD_PP;

    char dst[64]; make_ARB1_destarg_string(ctx, dst, sizeof (dst));

    const int sm1 = !shader_version_atleast(ctx, 1, 4);
    const int regnum = sm1 ? ctx->dest_arg.regnum : ctx->source_args[1].regnum;
    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, regnum);

    const char *ttype = NULL;
    char src0[64];
    if (sm1)
        get_ARB1_destarg_varname(ctx, src0, sizeof (src0));
    else
        get_ARB1_srcarg_varname(ctx, 0, src0, sizeof (src0));
    //char src1[64]; get_ARB1_srcarg_varname(ctx, 1, src1, sizeof (src1));  // !!! FIXME: SRC_MOD?

    char src2[64] = { 0 };
    char src3[64] = { 0 };

    if (texldd)
    {
        make_ARB1_srcarg_string(ctx, 2, src2, sizeof (src2));
        make_ARB1_srcarg_string(ctx, 3, src3, sizeof (src3));
    } // if

    // !!! FIXME: this should be in state_TEXLD, not in the arb1/glsl emitters.
    if (sreg == NULL)
    {
        fail(ctx, "TEXLD using undeclared sampler");
        return;
    } // if

    // SM1 only specifies dst, so don't check swizzle there.
    if ( !sm1 && (!no_swizzle(ctx->source_args[1].swizzle)) )
    {
        // !!! FIXME: does this ever actually happen?
        fail(ctx, "BUG: can't handle TEXLD with sampler swizzle at the moment");
    } // if

    switch ((const TextureType) sreg->index)
    {
        case TEXTURE_TYPE_2D: ttype = "2D"; break; // !!! FIXME: "RECT"?
        case TEXTURE_TYPE_CUBE: ttype = "CUBE"; break;
        case TEXTURE_TYPE_VOLUME: ttype = "3D"; break;
        default: fail(ctx, "unknown texture type"); return;
    } // switch

    if (texldd)
    {
        output_line(ctx, "%s%s, %s, %s, %s, texture[%d], %s;", opcode, dst,
                    src0, src2, src3, regnum, ttype);
    } // if
    else
    {
        output_line(ctx, "%s%s, %s, texture[%d], %s;", opcode, dst, src0,
                    regnum, ttype);
    } // else
} // arb1_texld


void emit_ARB1_TEXLDD(Context *ctx)
{
    // With GL_NV_fragment_program2, we can use the TXD opcode.
    //  In stock arb1, we can settle for a standard texld, which isn't
    //  perfect, but oh well.
    if (support_nv2(ctx))
        arb1_texld(ctx, "TXD", 1);
    else
        arb1_texld(ctx, "TEX", 0);
} // emit_ARB1_TEXLDD


void emit_ARB1_TEXLDL(Context *ctx)
{
    if ((shader_is_vertex(ctx)) && (!support_nv3(ctx)))
    {
        failf(ctx, "Vertex shader TEXLDL unsupported in %s profile",
              ctx->profile->name);
        return;
    } // if

    else if ((shader_is_pixel(ctx)) && (!support_nv2(ctx)))
    {
        failf(ctx, "Pixel shader TEXLDL unsupported in %s profile",
              ctx->profile->name);
        return;
    } // if

    // !!! FIXME: this doesn't map exactly to TEXLDL. Review this.
    arb1_texld(ctx, "TXL", 0);
} // emit_ARB1_TEXLDL


EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(BREAKP)
EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(BREAKC)

void emit_ARB1_IFC(Context *ctx)
{
    if (support_nv2(ctx))
    {
        const char *comps[] = {
            "", "SGTC", "SEQC", "SGEC", "SGTC", "SNEC", "SLEC"
        };

        if (ctx->instruction_controls >= STATICARRAYLEN(comps))
        {
            fail(ctx, "unknown comparison control");
            return;
        } // if

        char src0[64];
        char src1[64];
        char scratch[64];

        const char *comp = comps[ctx->instruction_controls];
        get_ARB1_srcarg_varname(ctx, 0, src0, sizeof (src0));
        get_ARB1_srcarg_varname(ctx, 1, src1, sizeof (src1));
        allocate_ARB1_scratch_reg_name(ctx, scratch, sizeof (scratch));
        output_line(ctx, "%s %s.x, %s, %s;", comp, scratch, src0, src1);
        nv2_if(ctx);
    } // if

    else  // stock ARB1 has no branching.
    {
        failf(ctx, "branching unsupported in %s profile", ctx->profile->name);
    } // else
} // emit_ARB1_IFC


EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(SETP)

void emit_ARB1_DEF(Context *ctx)
{
    const float *val = (const float *) ctx->dwords; // !!! FIXME: could be int?
    char dst[64]; get_ARB1_destarg_varname(ctx, dst, sizeof (dst));
    char val0[32]; floatstr(ctx, val0, sizeof (val0), val[0], 1);
    char val1[32]; floatstr(ctx, val1, sizeof (val1), val[1], 1);
    char val2[32]; floatstr(ctx, val2, sizeof (val2), val[2], 1);
    char val3[32]; floatstr(ctx, val3, sizeof (val3), val[3], 1);

    push_output(ctx, &ctx->globals);
    output_line(ctx, "PARAM %s = { %s, %s, %s, %s };",
                dst, val0, val1, val2, val3);
    pop_output(ctx);
} // emit_ARB1_DEF

void emit_ARB1_DEFI(Context *ctx)
{
    char dst[64]; get_ARB1_destarg_varname(ctx, dst, sizeof (dst));
    const int32 *x = (const int32 *) ctx->dwords;
    push_output(ctx, &ctx->globals);
    output_line(ctx, "PARAM %s = { %d, %d, %d, %d };",
                dst, (int) x[0], (int) x[1], (int) x[2], (int) x[3]);
    pop_output(ctx);
} // emit_ARB1_DEFI

void emit_ARB1_DEFB(Context *ctx)
{
    char dst[64]; get_ARB1_destarg_varname(ctx, dst, sizeof (dst));
    push_output(ctx, &ctx->globals);
    output_line(ctx, "PARAM %s = %d;", dst, ctx->dwords[0] ? 1 : 0);
    pop_output(ctx);
} // emit_ARB1_DEFB

void emit_ARB1_DCL(Context *ctx)
{
    // no-op. We do this in our emit_attribute() and emit_uniform().
} // emit_ARB1_DCL

EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC(TEXCRD)

void emit_ARB1_TEXLD(Context *ctx)
{
    if (!shader_version_atleast(ctx, 1, 4))
    {
        arb1_texld(ctx, "TEX", 0);
        return;
    } // if

    else if (!shader_version_atleast(ctx, 2, 0))
    {
        // ps_1_4 is different, too!
        fail(ctx, "TEXLD == Shader Model 1.4 unimplemented.");  // !!! FIXME
        return;
    } // if

    // !!! FIXME: do texldb and texldp map between OpenGL and D3D correctly?
    if (ctx->instruction_controls == CONTROL_TEXLD)
        arb1_texld(ctx, "TEX", 0);
    else if (ctx->instruction_controls == CONTROL_TEXLDP)
        arb1_texld(ctx, "TXP", 0);
    else if (ctx->instruction_controls == CONTROL_TEXLDB)
        arb1_texld(ctx, "TXB", 0);
} // emit_ARB1_TEXLD

#undef EMIT_ARB1_OPCODE_FUNC
#undef EMIT_ARB1_OPCODE_D_FUNC
#undef EMIT_ARB1_OPCODE_S_FUNC
#undef EMIT_ARB1_OPCODE_SS_FUNC
#undef EMIT_ARB1_OPCODE_DS_FUNC
#undef EMIT_ARB1_OPCODE_DSS_FUNC
#undef EMIT_ARB1_OPCODE_DSSS_FUNC
#undef EMIT_ARB1_OPCODE_DSSSS_FUNC
#undef EMIT_ARB1_OPCODE_UNIMPLEMENTED_FUNC

#endif  // SUPPORT_PROFILE_ARB1

#pragma GCC visibility pop
