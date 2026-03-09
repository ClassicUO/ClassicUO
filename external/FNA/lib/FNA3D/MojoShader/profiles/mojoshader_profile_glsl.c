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

#if SUPPORT_PROFILE_GLSL

#define EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(op) \
    void emit_GLSL_##op(Context *ctx) { \
        fail(ctx, #op " unimplemented in glsl profile"); \
    }

static inline const char *get_GLSL_register_string(Context *ctx,
                        const RegisterType regtype, const int regnum,
                        char *regnum_str, const size_t regnum_size)
{
    // turns out these are identical at the moment.
    return get_D3D_register_string(ctx,regtype,regnum,regnum_str,regnum_size);
} // get_GLSL_register_string

const char *get_GLSL_uniform_type(Context *ctx, const RegisterType rtype)
{
    switch (rtype)
    {
        case REG_TYPE_CONST: return "vec4";
        case REG_TYPE_CONSTINT: return "ivec4";
        case REG_TYPE_CONSTBOOL: return "bool";
        default: fail(ctx, "BUG: used a uniform we don't know how to define.");
    } // switch

    return NULL;
} // get_GLSL_uniform_type

const char *get_GLSL_varname_in_buf(Context *ctx, RegisterType rt,
                                    int regnum, char *buf,
                                    const size_t len)
{
    char regnum_str[16];
    const char *regtype_str = get_GLSL_register_string(ctx, rt, regnum,
                                              regnum_str, sizeof (regnum_str));
    snprintf(buf,len,"%s_%s%s", ctx->shader_type_str, regtype_str, regnum_str);
    return buf;
} // get_GLSL_varname_in_buf


const char *get_GLSL_varname(Context *ctx, RegisterType rt, int regnum)
{
    char buf[64];
    get_GLSL_varname_in_buf(ctx, rt, regnum, buf, sizeof (buf));
    return StrDup(ctx, buf);
} // get_GLSL_varname


static inline const char *get_GLSL_const_array_varname_in_buf(Context *ctx,
                                                const int base, const int size,
                                                char *buf, const size_t buflen)
{
    const char *type = ctx->shader_type_str;
    snprintf(buf, buflen, "%s_const_array_%d_%d", type, base, size);
    return buf;
} // get_GLSL_const_array_varname_in_buf

const char *get_GLSL_const_array_varname(Context *ctx, int base, int size)
{
    char buf[64];
    get_GLSL_const_array_varname_in_buf(ctx, base, size, buf, sizeof (buf));
    return StrDup(ctx, buf);
} // get_GLSL_const_array_varname


static inline const char *get_GLSL_input_array_varname(Context *ctx,
                                                char *buf, const size_t buflen)
{
    snprintf(buf, buflen, "%s", "vertex_input_array");
    return buf;
} // get_GLSL_input_array_varname


const char *get_GLSL_uniform_array_varname(Context *ctx,
                                           const RegisterType regtype,
                                           char *buf, const size_t len)
{
    const char *shadertype = ctx->shader_type_str;
    const char *type = get_GLSL_uniform_type(ctx, regtype);
    snprintf(buf, len, "%s_uniforms_%s", shadertype, type);
    return buf;
} // get_GLSL_uniform_array_varname

const char *get_GLSL_destarg_varname(Context *ctx, char *buf, size_t len)
{
    const DestArgInfo *arg = &ctx->dest_arg;
    return get_GLSL_varname_in_buf(ctx, arg->regtype, arg->regnum, buf, len);
} // get_GLSL_destarg_varname

const char *get_GLSL_srcarg_varname(Context *ctx, const size_t idx,
                                    char *buf, size_t len)
{
    if (idx >= STATICARRAYLEN(ctx->source_args))
    {
        fail(ctx, "Too many source args");
        *buf = '\0';
        return buf;
    } // if

    const SourceArgInfo *arg = &ctx->source_args[idx];
    return get_GLSL_varname_in_buf(ctx, arg->regtype, arg->regnum, buf, len);
} // get_GLSL_srcarg_varname


const char *make_GLSL_destarg_assign(Context *, char *, const size_t,
                                     const char *, ...) ISPRINTF(4,5);

const char *make_GLSL_destarg_assign(Context *ctx, char *buf,
                                     const size_t buflen,
                                     const char *fmt, ...)
{
    int need_parens = 0;
    const DestArgInfo *arg = &ctx->dest_arg;

    if (arg->writemask == 0)
    {
        *buf = '\0';
        return buf;  // no writemask? It's a no-op.
    } // if

    char clampbuf[32] = { '\0' };
    const char *clampleft = "";
    const char *clampright = "";
    if (arg->result_mod & MOD_SATURATE)
    {
        const int vecsize = vecsize_from_writemask(arg->writemask);
        clampleft = "clamp(";
        if (vecsize == 1)
            clampright = ", 0.0, 1.0)";
        else
        {
            snprintf(clampbuf, sizeof (clampbuf),
                     ", vec%d(0.0), vec%d(1.0))", vecsize, vecsize);
            clampright = clampbuf;
        } // else
    } // if

    // MSDN says MOD_PP is a hint and many implementations ignore it. So do we.

    // CENTROID only allowed in DCL opcodes, which shouldn't come through here.
    assert((arg->result_mod & MOD_CENTROID) == 0);

    if (ctx->predicated)
    {
        fail(ctx, "predicated destinations unsupported");  // !!! FIXME
        *buf = '\0';
        return buf;
    } // if

    char operation[256];
    va_list ap;
    va_start(ap, fmt);
    const int len = vsnprintf(operation, sizeof (operation), fmt, ap);
    va_end(ap);
    if (len >= sizeof (operation))
    {
        fail(ctx, "operation string too large");  // I'm lazy.  :P
        *buf = '\0';
        return buf;
    } // if

    const char *result_shift_str = "";
    switch (arg->result_shift)
    {
        case 0x1: result_shift_str = " * 2.0"; break;
        case 0x2: result_shift_str = " * 4.0"; break;
        case 0x3: result_shift_str = " * 8.0"; break;
        case 0xD: result_shift_str = " / 8.0"; break;
        case 0xE: result_shift_str = " / 4.0"; break;
        case 0xF: result_shift_str = " / 2.0"; break;
    } // switch
    need_parens |= (result_shift_str[0] != '\0');

    char regnum_str[16];
    const char *regtype_str = get_GLSL_register_string(ctx, arg->regtype,
                                                       arg->regnum, regnum_str,
                                                       sizeof (regnum_str));
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

    const char *leftparen = (need_parens) ? "(" : "";
    const char *rightparen = (need_parens) ? ")" : "";

    snprintf(buf, buflen, "%s_%s%s%s = %s%s%s%s%s%s;",
             ctx->shader_type_str, regtype_str, regnum_str, writemask_str,
             clampleft, leftparen, operation, rightparen, result_shift_str,
             clampright);
    // !!! FIXME: make sure the scratch buffer was large enough.
    return buf;
} // make_GLSL_destarg_assign


char *make_GLSL_swizzle_string(char *swiz_str, const size_t strsize,
                               const int swizzle, const int writemask)
{
    size_t i = 0;
    if ( (!no_swizzle(swizzle)) || (!writemask_xyzw(writemask)) )
    {
        const int writemask0 = (writemask >> 0) & 0x1;
        const int writemask1 = (writemask >> 1) & 0x1;
        const int writemask2 = (writemask >> 2) & 0x1;
        const int writemask3 = (writemask >> 3) & 0x1;

        const int swizzle_x = (swizzle >> 0) & 0x3;
        const int swizzle_y = (swizzle >> 2) & 0x3;
        const int swizzle_z = (swizzle >> 4) & 0x3;
        const int swizzle_w = (swizzle >> 6) & 0x3;

        swiz_str[i++] = '.';
        if (writemask0) swiz_str[i++] = swizzle_channels[swizzle_x];
        if (writemask1) swiz_str[i++] = swizzle_channels[swizzle_y];
        if (writemask2) swiz_str[i++] = swizzle_channels[swizzle_z];
        if (writemask3) swiz_str[i++] = swizzle_channels[swizzle_w];
    } // if
    assert(i < strsize);
    swiz_str[i] = '\0';
    return swiz_str;
} // make_GLSL_swizzle_string


const char *make_GLSL_srcarg_string(Context *ctx, const size_t idx,
                                    const int writemask, char *buf,
                                    const size_t buflen)
{
    *buf = '\0';

    if (idx >= STATICARRAYLEN(ctx->source_args))
    {
        fail(ctx, "Too many source args");
        return buf;
    } // if

    const SourceArgInfo *arg = &ctx->source_args[idx];

    const char *premod_str = "";
    const char *postmod_str = "";
    switch (arg->src_mod)
    {
        case SRCMOD_NEGATE:
            premod_str = "-";
            break;

        case SRCMOD_BIASNEGATE:
            premod_str = "-(";
            postmod_str = " - 0.5)";
            break;

        case SRCMOD_BIAS:
            premod_str = "(";
            postmod_str = " - 0.5)";
            break;

        case SRCMOD_SIGNNEGATE:
            premod_str = "-((";
            postmod_str = " - 0.5) * 2.0)";
            break;

        case SRCMOD_SIGN:
            premod_str = "((";
            postmod_str = " - 0.5) * 2.0)";
            break;

        case SRCMOD_COMPLEMENT:
            premod_str = "(1.0 - ";
            postmod_str = ")";
            break;

        case SRCMOD_X2NEGATE:
            premod_str = "-(";
            postmod_str = " * 2.0)";
            break;

        case SRCMOD_X2:
            premod_str = "(";
            postmod_str = " * 2.0)";
            break;

        case SRCMOD_DZ:
            fail(ctx, "SRCMOD_DZ unsupported"); return buf; // !!! FIXME
            postmod_str = "_dz";
            break;

        case SRCMOD_DW:
            fail(ctx, "SRCMOD_DW unsupported"); return buf; // !!! FIXME
            postmod_str = "_dw";
            break;

        case SRCMOD_ABSNEGATE:
            premod_str = "-abs(";
            postmod_str = ")";
            break;

        case SRCMOD_ABS:
            premod_str = "abs(";
            postmod_str = ")";
            break;

        case SRCMOD_NOT:
            premod_str = "!";
            break;

        case SRCMOD_NONE:
        case SRCMOD_TOTAL:
             break;  // stop compiler whining.
    } // switch

    const char *regtype_str = NULL;

    if (!arg->relative)
    {
        regtype_str = get_GLSL_varname_in_buf(ctx, arg->regtype, arg->regnum,
                                              (char *) alloca(64), 64);
    } // if

    const char *rel_lbracket = "";
    char rel_offset[32] = { '\0' };
    const char *rel_rbracket = "";
    char rel_swizzle[4] = { '\0' };
    const char *rel_regtype_str = "";
    if (arg->relative)
    {
        if (arg->regtype == REG_TYPE_INPUT)
            regtype_str=get_GLSL_input_array_varname(ctx,(char*)alloca(64),64);
        else
        {
            assert(arg->regtype == REG_TYPE_CONST);
            const int arrayidx = arg->relative_array->index;
            const int offset = arg->regnum - arrayidx;
            assert(offset >= 0);
            if (arg->relative_array->constant)
            {
                const int arraysize = arg->relative_array->count;
                regtype_str = get_GLSL_const_array_varname_in_buf(ctx,
                                arrayidx, arraysize, (char *) alloca(64), 64);
                if (offset != 0)
                    snprintf(rel_offset, sizeof (rel_offset), "%d + ", offset);
            } // if
            else
            {
                regtype_str = get_GLSL_uniform_array_varname(ctx, arg->regtype,
                                                      (char *) alloca(64), 64);
                if (offset == 0)
                {
                    snprintf(rel_offset, sizeof (rel_offset),
                             "ARRAYBASE_%d + ", arrayidx);
                } // if
                else
                {
                    snprintf(rel_offset, sizeof (rel_offset),
                             "(ARRAYBASE_%d + %d) + ", arrayidx, offset);
                } // else
            } // else
        } // else

        rel_lbracket = "[";

        if (arg->relative_regtype == REG_TYPE_LOOP)
        {
            rel_regtype_str = "aL";
            rel_swizzle[0] = '\0';
            rel_swizzle[1] = '\0';
            rel_swizzle[2] = '\0';
        } // if
        else
        {
            rel_regtype_str = get_GLSL_varname_in_buf(ctx, arg->relative_regtype,
                                                      arg->relative_regnum,
                                                      (char *) alloca(64), 64);
            rel_swizzle[0] = '.';
            rel_swizzle[1] = swizzle_channels[arg->relative_component];
            rel_swizzle[2] = '\0';
        } // else
        rel_rbracket = "]";
    } // if

    char swiz_str[6] = { '\0' };
    if (!isscalar(ctx, ctx->shader_type, arg->regtype, arg->regnum))
    {
        make_GLSL_swizzle_string(swiz_str, sizeof (swiz_str),
                                 arg->swizzle, writemask);
    } // if

    if (regtype_str == NULL)
    {
        fail(ctx, "Unknown source register type.");
        return buf;
    } // if

    snprintf(buf, buflen, "%s%s%s%s%s%s%s%s%s",
             premod_str, regtype_str, rel_lbracket, rel_offset,
             rel_regtype_str, rel_swizzle, rel_rbracket, swiz_str,
             postmod_str);
    // !!! FIXME: make sure the scratch buffer was large enough.
    return buf;
} // make_GLSL_srcarg_string

// generate some convenience functions.
#define MAKE_GLSL_SRCARG_STRING_(mask, bitmask) \
    static inline const char *make_GLSL_srcarg_string_##mask(Context *ctx, \
                                                const size_t idx, char *buf, \
                                                const size_t buflen) { \
        return make_GLSL_srcarg_string(ctx, idx, bitmask, buf, buflen); \
    }
MAKE_GLSL_SRCARG_STRING_(x, (1 << 0))
MAKE_GLSL_SRCARG_STRING_(y, (1 << 1))
MAKE_GLSL_SRCARG_STRING_(z, (1 << 2))
MAKE_GLSL_SRCARG_STRING_(w, (1 << 3))
MAKE_GLSL_SRCARG_STRING_(scalar, (1 << 0))
MAKE_GLSL_SRCARG_STRING_(full, 0xF)
MAKE_GLSL_SRCARG_STRING_(masked, ctx->dest_arg.writemask)
MAKE_GLSL_SRCARG_STRING_(vec3, 0x7)
MAKE_GLSL_SRCARG_STRING_(vec2, 0x3)
#undef MAKE_GLSL_SRCARG_STRING_

// special cases for comparison opcodes...

const char *get_GLSL_comparison_string_scalar(Context *ctx)
{
    const char *comps[] = { "", ">", "==", ">=", "<", "!=", "<=" };
    if (ctx->instruction_controls >= STATICARRAYLEN(comps))
    {
        fail(ctx, "unknown comparison control");
        return "";
    } // if

    return comps[ctx->instruction_controls];
} // get_GLSL_comparison_string_scalar

const char *get_GLSL_comparison_string_vector(Context *ctx)
{
    const char *comps[] = {
        "", "greaterThan", "equal", "greaterThanEqual", "lessThan",
        "notEqual", "lessThanEqual"
    };

    if (ctx->instruction_controls >= STATICARRAYLEN(comps))
    {
        fail(ctx, "unknown comparison control");
        return "";
    } // if

    return comps[ctx->instruction_controls];
} // get_GLSL_comparison_string_vector

// special extensions needed for texldd/texldl...

static void prepend_glsl_texlod_extensions(Context *ctx)
{
    // !!! FIXME:
    // GLSL 1.30 introduced textureGrad() for this, but it looks like the
    //  functions are overloaded instead of texture2DGrad() (etc).

    // !!! FIXME:
    // The spec says we can't use GLSL's texture*Lod() built-ins from fragment
    //  shaders for some inexplicable reason.
    // For now, you'll just have to suffer with the potentially wrong mipmap
    //  until I can figure something out.

    // ARB_shader_texture_lod and EXT_gpu_shader4 added texture2DLod/Grad*(),
    //  so we'll use them if available. Failing that, we'll just fallback
    //  to a regular texture2D call and hope the mipmap it chooses is close
    //  enough.
    if (!ctx->glsl_generated_texlod_setup && !support_glsles3(ctx))
    {
        ctx->glsl_generated_texlod_setup = 1;
        push_output(ctx, &ctx->preflight);
        output_line(ctx, "#if GL_ARB_shader_texture_lod");
        output_line(ctx, "#extension GL_ARB_shader_texture_lod : enable");
        output_line(ctx, "#define texture2DGrad texture2DGradARB");
        output_line(ctx, "#define texture2DProjGrad texture2DProjARB");
        output_line(ctx, "#elif GL_EXT_gpu_shader4");
        output_line(ctx, "#extension GL_EXT_gpu_shader4 : enable");
        output_line(ctx, "#else");
        output_line(ctx, "#define texture2DGrad(a,b,c,d) texture2D(a,b)");
        output_line(ctx, "#define texture2DProjGrad(a,b,c,d) texture2DProj(a,b)");
        if (shader_is_pixel(ctx))
            output_line(ctx, "#define texture2DLod(a,b,c) texture2D(a,b)");
        output_line(ctx, "#endif");
        output_blank_line(ctx);
        pop_output(ctx);
    } // if
} // prepend_glsl_texlod_extensions


void emit_GLSL_start(Context *ctx, const char *profilestr)
{
    if (!shader_is_vertex(ctx) && !shader_is_pixel(ctx))
    {
        failf(ctx, "Shader type %u unsupported in this profile.",
              (uint) ctx->shader_type);
        return;
    } // if

    else if (strcmp(profilestr, MOJOSHADER_PROFILE_GLSL) == 0)
    {
        // No gl_FragData[] before GLSL 1.10, so we have to force the version.
        push_output(ctx, &ctx->preflight);
        output_line(ctx, "#version 110");
        pop_output(ctx);
    } // else if

    #if SUPPORT_PROFILE_GLSL120
    else if (strcmp(profilestr, MOJOSHADER_PROFILE_GLSL120) == 0)
    {
        ctx->profile_supports_glsl120 = 1;
        push_output(ctx, &ctx->preflight);
        output_line(ctx, "#version 120");
        pop_output(ctx);
    } // else if
    #endif

    #if SUPPORT_PROFILE_GLSLES3
    else if (strcmp(profilestr, MOJOSHADER_PROFILE_GLSLES3) == 0)
    {
        ctx->profile_supports_glsles3 = 1;
        push_output(ctx, &ctx->preflight);
        output_line(ctx, "#version 300 es");
        output_line(ctx, "#define texture2D texture");
        output_line(ctx, "#define texture2DLod textureLod");
        output_line(ctx, "#define texture2DProj textureProj");
        output_line(ctx, "#define texture2DGrad textureGrad");
        output_line(ctx, "#define texture2DProjGrad textureProjGrad");
        output_line(ctx, "#define texture3D texture");
        output_line(ctx, "#define texture3DLod textureLod");
        output_line(ctx, "#define textureCube texture");
        output_line(ctx, "#define textureCubeLod textureLod");
        output_line(ctx, "#define texture2DRect texture");
        output_line(ctx, "#define texture2DRectProj textureProj");
        output_line(ctx, "#define texture2DRectLod textureLod");
        if (shader_is_vertex(ctx))
            output_line(ctx, "precision highp float;");
        else
            output_line(ctx, "precision mediump float;");
        output_line(ctx, "precision mediump int;");
        pop_output(ctx);
    } // else if
    #endif

    #if SUPPORT_PROFILE_GLSLES
    else if (strcmp(profilestr, MOJOSHADER_PROFILE_GLSLES) == 0)
    {
        ctx->profile_supports_glsles = 1;
        push_output(ctx, &ctx->preflight);
        output_line(ctx, "#version 100");
        if (shader_is_vertex(ctx))
            output_line(ctx, "precision highp float;");
        else
            output_line(ctx, "precision mediump float;");
        output_line(ctx, "precision mediump int;");
        pop_output(ctx);
    } // else if
    #endif

    else
    {
        failf(ctx, "Profile '%s' unsupported or unknown.", profilestr);
        return;
    } // else

    push_output(ctx, &ctx->mainline_intro);
    output_line(ctx, "void main()");
    output_line(ctx, "{");
    pop_output(ctx);

    set_output(ctx, &ctx->mainline);
    ctx->indent++;
} // emit_GLSL_start

void emit_GLSL_RET(Context *ctx);
void emit_GLSL_end(Context *ctx)
{
    // ps_1_* writes color to r0 instead oC0. We move it to the right place.
    // We don't have to worry about a RET opcode messing this up, since
    //  RET isn't available before ps_2_0.
    if (shader_is_pixel(ctx) && !shader_version_atleast(ctx, 2, 0))
    {
        const char *shstr = ctx->shader_type_str;
        set_used_register(ctx, REG_TYPE_COLOROUT, 0, 1);
        output_line(ctx, "%s_oC0 = %s_r0;", shstr, shstr);
    } // if
    else if (shader_is_vertex(ctx))
    {
#ifdef MOJOSHADER_FLIP_RENDERTARGET
        output_line(ctx, "gl_Position.y = gl_Position.y * vpFlip;");
#endif
#ifdef MOJOSHADER_DEPTH_CLIPPING
        output_line(ctx, "gl_Position.z = gl_Position.z * 2.0 - gl_Position.w;");
#endif
    } // else if

    // force a RET opcode if we're at the end of the stream without one.
    if (ctx->previous_opcode != OPCODE_RET)
        emit_GLSL_RET(ctx);
} // emit_GLSL_end

void emit_GLSL_phase(Context *ctx)
{
    // no-op in GLSL.
} // emit_GLSL_phase

void output_GLSL_uniform_array(Context *ctx, const RegisterType regtype,
                               const int size)
{
    if (size > 0)
    {
        char buf[64];
        get_GLSL_uniform_array_varname(ctx, regtype, buf, sizeof (buf));
        const char *typ;
        switch (regtype)
        {
            case REG_TYPE_CONST: typ = "vec4"; break;
            case REG_TYPE_CONSTINT: typ ="ivec4"; break;
            case REG_TYPE_CONSTBOOL: typ = "bool"; break;
            default:
            {
                fail(ctx, "BUG: used a uniform we don't know how to define.");
                return;
            } // default
        } // switch
        output_line(ctx, "uniform %s %s[%d];", typ, buf, size);
    } // if
} // output_GLSL_uniform_array

void emit_GLSL_finalize(Context *ctx)
{
    // throw some blank lines around to make source more readable.
    push_output(ctx, &ctx->globals);
    output_blank_line(ctx);
    pop_output(ctx);

    // If we had a relative addressing of REG_TYPE_INPUT, we need to build
    //  an array for it at the start of main(). GLSL doesn't let you specify
    //  arrays of attributes.
    //vec4 blah_array[BIGGEST_ARRAY];
    if (ctx->have_relative_input_registers) // !!! FIXME
        fail(ctx, "Relative addressing of input registers not supported.");

    push_output(ctx, &ctx->preflight);
    output_GLSL_uniform_array(ctx, REG_TYPE_CONST, ctx->uniform_float4_count);
    output_GLSL_uniform_array(ctx, REG_TYPE_CONSTINT, ctx->uniform_int4_count);
    output_GLSL_uniform_array(ctx, REG_TYPE_CONSTBOOL, ctx->uniform_bool_count);
#ifdef MOJOSHADER_FLIP_RENDERTARGET
    if (shader_is_vertex(ctx))
        output_line(ctx, "uniform float vpFlip;");
#endif
    if (ctx->need_max_float)
        output_line(ctx, "const float FLT_MAX = 1e38;");
    pop_output(ctx);
} // emit_GLSL_finalize

void emit_GLSL_global(Context *ctx, RegisterType regtype, int regnum)
{
    char varname[64];
    get_GLSL_varname_in_buf(ctx, regtype, regnum, varname, sizeof (varname));

    push_output(ctx, &ctx->globals);
    switch (regtype)
    {
        case REG_TYPE_ADDRESS:
            if (shader_is_vertex(ctx))
                output_line(ctx, "ivec4 %s;", varname);
            else if (shader_is_pixel(ctx))  // actually REG_TYPE_TEXTURE.
            {
                // We have to map texture registers to temps for ps_1_1, since
                //  they work like temps, initialize with tex coords, and the
                //  ps_1_1 TEX opcode expects to overwrite it.
                if (!shader_version_atleast(ctx, 1, 4))
                {
                    // GLSL ES does not have gl_TexCoord!
                    // Also, gl_TexCoord[4+] is unreliable!
#if SUPPORT_PROFILE_GLSLES
                    const int skipGLTexCoord = support_glsles(ctx) || (regnum >= 4);
#else
                    const int skipGLTexCoord = (regnum >= 4);
#endif
                    if (skipGLTexCoord)
                        output_line(ctx, "vec4 %s = io_%i_%i;",
                                    varname, MOJOSHADER_USAGE_TEXCOORD, regnum);
                    else
                        output_line(ctx, "vec4 %s = gl_TexCoord[%d];",
                                    varname, regnum);
                } // if
            } // else if
            break;
        case REG_TYPE_PREDICATE:
            output_line(ctx, "bvec4 %s;", varname);
            break;
        case REG_TYPE_TEMP:
            output_line(ctx, "vec4 %s;", varname);
            break;
        case REG_TYPE_LOOP:
            break; // no-op. We declare these in for loops at the moment.
        case REG_TYPE_LABEL:
            break; // no-op. If we see it here, it means we optimized it out.
        default:
            fail(ctx, "BUG: we used a register we don't know how to define.");
            break;
    } // switch
    pop_output(ctx);
} // emit_GLSL_global

void emit_GLSL_array(Context *ctx, VariableList *var)
{
    // All uniforms (except constant arrays, which only get pushed once at
    //  compile time) are now packed into a single array, so we can batch
    //  the uniform transfers. So this doesn't actually define an array
    //  here; the one, big array is emitted during finalization instead.
    // However, we need to #define the offset into the one, big array here,
    //  and let dereferences use that #define.
    const int base = var->index;
    const int glslbase = ctx->uniform_float4_count;
    push_output(ctx, &ctx->globals);
    output_line(ctx, "#define ARRAYBASE_%d %d", base, glslbase);
    pop_output(ctx);
    var->emit_position = glslbase;
} // emit_GLSL_array

void emit_GLSL_const_array(Context *ctx, const ConstantsList *clist,
                           int base, int size)
{
    char varname[64];
    get_GLSL_const_array_varname_in_buf(ctx,base,size,varname,sizeof(varname));

#if 0
    // !!! FIXME: fails on Nvidia's and Apple's GL, even with #version 120.
    // !!! FIXME:  (the 1.20 spec says it should work, though, I think...)
    if (support_glsl120(ctx))
    {
        // GLSL 1.20 can do constant arrays.
        const char *cstr = NULL;
        push_output(ctx, &ctx->globals);
        output_line(ctx, "const vec4 %s[%d] = vec4[%d](", varname, size, size);
        ctx->indent++;

        int i;
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

            output_line(ctx, "vec4(%s, %s, %s, %s)%s", val0, val1, val2, val3,
                        (i < (size-1)) ? "," : "");

            clist = clist->next;
        } // for

        ctx->indent--;
        output_line(ctx, ");");
        pop_output(ctx);
    } // if

    else
#endif
    {
        // stock GLSL 1.0 can't do constant arrays, so make a uniform array
        //  and have the OpenGL glue assign it at link time. Lame!
        push_output(ctx, &ctx->globals);
        output_line(ctx, "uniform vec4 %s[%d];", varname, size);
        pop_output(ctx);
    } // else
} // emit_GLSL_const_array

void emit_GLSL_uniform(Context *ctx, RegisterType regtype, int regnum,
                       const VariableList *var)
{
    // Now that we're pushing all the uniforms as one big array, pack these
    //  down, so if we only use register c439, it'll actually map to
    //  glsl_uniforms_vec4[0]. As we push one big array, this will prevent
    //  uploading unused data.

    char varname[64];
    char name[64];
    int index = 0;

    get_GLSL_varname_in_buf(ctx, regtype, regnum, varname, sizeof (varname));

    push_output(ctx, &ctx->globals);

    if (var == NULL)
    {
        get_GLSL_uniform_array_varname(ctx, regtype, name, sizeof (name));

        if (regtype == REG_TYPE_CONST)
            index = ctx->uniform_float4_count;
        else if (regtype == REG_TYPE_CONSTINT)
            index = ctx->uniform_int4_count;
        else if (regtype == REG_TYPE_CONSTBOOL)
            index = ctx->uniform_bool_count;
        else  // get_GLSL_uniform_array_varname() would have called fail().
            assert(!(ctx->isfail));

        output_line(ctx, "#define %s %s[%d]", varname, name, index);
    } // if

    else
    {
        const int arraybase = var->index;
        if (var->constant)
        {
            get_GLSL_const_array_varname_in_buf(ctx, arraybase, var->count,
                                                name, sizeof (name));
            index = (regnum - arraybase);
        } // if
        else
        {
            assert(var->emit_position != -1);
            get_GLSL_uniform_array_varname(ctx, regtype, name, sizeof (name));
            index = (regnum - arraybase) + var->emit_position;
        } // else

        output_line(ctx, "#define %s %s[%d]", varname, name, index);
    } // else

    pop_output(ctx);
} // emit_GLSL_uniform

void emit_GLSL_sampler(Context *ctx,int stage,TextureType ttype,int tb)
{
    const char *type = "";
    switch (ttype)
    {
        case TEXTURE_TYPE_2D: type = "sampler2D"; break;
        case TEXTURE_TYPE_CUBE: type = "samplerCube"; break;
        case TEXTURE_TYPE_VOLUME: type = "sampler3D"; break;
        default: fail(ctx, "BUG: used a sampler we don't know how to define.");
    } // switch

    char var[64];
    get_GLSL_varname_in_buf(ctx, REG_TYPE_SAMPLER, stage, var, sizeof (var));

    push_output(ctx, &ctx->globals);
    output_line(ctx, "uniform %s %s;", type, var);
    if (tb)  // This sampler used a ps_1_1 TEXBEM opcode?
    {
        char name[64];
        const int index = ctx->uniform_float4_count;
        ctx->uniform_float4_count += 2;
        get_GLSL_uniform_array_varname(ctx, REG_TYPE_CONST, name, sizeof (name));
        output_line(ctx, "#define %s_texbem %s[%d]", var, name, index);
        output_line(ctx, "#define %s_texbeml %s[%d]", var, name, index+1);
    } // if
    pop_output(ctx);
} // emit_GLSL_sampler

void emit_GLSL_attribute(Context *ctx, RegisterType regtype, int regnum,
                         MOJOSHADER_usage usage, int index, int wmask,
                         int flags)
{
    // !!! FIXME: this function doesn't deal with write masks at all yet!
    const char *qualifier_in;
    const char *qualifier_out;
    const char *usage_str = NULL;
    const char *arrayleft = "";
    const char *arrayright = "";
    char index_str[16] = { '\0' };
    char var[64];

    get_GLSL_varname_in_buf(ctx, regtype, regnum, var, sizeof (var));

    //assert((flags & MOD_PP) == 0);  // !!! FIXME: is PP allowed?

    if (index != 0)  // !!! FIXME: a lot of these MUST be zero.
        snprintf(index_str, sizeof (index_str), "%u", (uint) index);

    if (shader_is_vertex(ctx))
    {
        if (support_glsles3(ctx))
        {
            qualifier_in = "in";
            qualifier_out = "out";
        }
        else
        {
            qualifier_in = "attribute";
            qualifier_out = "varying";
        }

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
            push_output(ctx, &ctx->globals);
            output_line(ctx, "%s vec4 %s;", qualifier_in, var);
            pop_output(ctx);
        } // if

        else if (regtype == REG_TYPE_OUTPUT)
        {
            switch (usage)
            {
                case MOJOSHADER_USAGE_POSITION:
                    if (index == 0)
                    {
                        usage_str = "gl_Position";
                    } // if
                    break;
                case MOJOSHADER_USAGE_POINTSIZE:
                    if (index == 0)
                    {
                        usage_str = "gl_PointSize";
                    } // if
                    else
                    {
                        push_output(ctx, &ctx->globals);
#if SUPPORT_PROFILE_GLSLES
                        if (support_glsles(ctx))
                            output_line(ctx, "%s highp float io_%i_%i;", qualifier_out, usage, index);
                        else
#endif
                        output_line(ctx, "varying float io_%i_%i;", usage, index);
                        output_line(ctx, "#define %s io_%i_%i", var, usage, index);
                        pop_output(ctx);
                        return;
                    }
                    break;
                case MOJOSHADER_USAGE_COLOR:
#if SUPPORT_PROFILE_GLSLES
                    if (support_glsles(ctx))
                        break; // GLSL ES does not have gl_FrontColor
#endif
                    index_str[0] = '\0';  // no explicit number.
                    if (index == 0)
                    {
                        usage_str = "gl_FrontColor";
                    } // if
                    else if (index == 1)
                    {
                        usage_str = "gl_FrontSecondaryColor";
                    } // else if
                    break;
                case MOJOSHADER_USAGE_FOG:
#if SUPPORT_PROFILE_GLSLES
                    if (support_glsles(ctx))
                        break; // GLSL ES does not have gl_FogFragCoord
#endif
                    if (index == 0)
                    {
                        usage_str = "gl_FogFragCoord";
                    } // if
                    else
                    {
                        push_output(ctx, &ctx->globals);
#if SUPPORT_PROFILE_GLSLES
                        if (support_glsles(ctx))
                            output_line(ctx, "%s highp float io_%i_%i;", qualifier_out, usage, index);
                        else
#endif
                        output_line(ctx, "varying float io_%i_%i;", usage, index);
                        output_line(ctx, "#define %s io_%i_%i", var, usage, index);
                        pop_output(ctx);
                        return;
                    }
                    break;
                case MOJOSHADER_USAGE_TEXCOORD:
#if SUPPORT_PROFILE_GLSLES
                    if (support_glsles(ctx))
                        break; // GLSL ES does not have gl_TexCoord
#endif
                    if (index >= 4)
                        break; // gl_TexCoord[4+] is unreliable!
                    snprintf(index_str, sizeof (index_str), "%u", (uint) index);
                    usage_str = "gl_TexCoord";
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
            {
#if SUPPORT_PROFILE_GLSLES
                if (support_glsles(ctx))
                    output_line(ctx, "%s highp vec4 io_%i_%i;", qualifier_out, usage, index);
                else
#endif
                output_line(ctx, "varying vec4 io_%i_%i;", usage, index);
                output_line(ctx, "#define %s io_%i_%i", var, usage, index);
            } // if
            else
            {
                output_line(ctx, "#define %s %s%s%s%s", var, usage_str,
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
        if (support_glsles3(ctx))
            qualifier_in = "in";
        else
            qualifier_in = "varying";

        // samplers DCLs get handled in emit_GLSL_sampler().

        if (flags & MOD_CENTROID)  // !!! FIXME
        {
            failf(ctx, "centroid unsupported in %s profile", ctx->profile->name);
            return;
        } // if

        if (regtype == REG_TYPE_COLOROUT)
        {
            // gl_FragColor/FragData were deprecated in favor of manually
            // defining your own output variables, GLSL ES 3.x enforces the new
            // syntax, so let's use it.
            if (support_glsles3(ctx))
            {
                usage_str = "_gl_FragData";
                snprintf(index_str, sizeof (index_str), "_%u", (uint) regnum );

                push_output(ctx, &ctx->globals);
                output_line(ctx, "layout(location = %u) out highp vec4 %s%s;", regnum, usage_str, index_str);
                pop_output(ctx);
            }
            else
            {
                if (!ctx->have_multi_color_outputs)
                    usage_str = "gl_FragColor";  // maybe faster?
                else
                {
                    snprintf(index_str, sizeof (index_str), "%u", (uint) regnum);
                    usage_str = "gl_FragData";
                    arrayleft = "[";
                    arrayright = "]";
                } // else
            } // else
        } // if

        else if (regtype == REG_TYPE_DEPTHOUT)
            usage_str = "gl_FragDepth";

        // !!! FIXME: can you actualy have a texture register with COLOR usage?
        else if ((regtype == REG_TYPE_TEXTURE) || (regtype == REG_TYPE_INPUT))
        {
#if SUPPORT_PROFILE_GLSLES
            if (!support_glsles(ctx))
            {
#endif
            if (usage == MOJOSHADER_USAGE_TEXCOORD)
            {
                // ps_1_1 does a different hack for this attribute.
                //  Refer to emit_GLSL_global()'s REG_TYPE_ADDRESS code.
                if (!shader_version_atleast(ctx, 1, 4))
                    return;
                else if (index < 4)  // gl_TexCoord[4+] is unreliable!
                {
                    snprintf(index_str, sizeof (index_str), "%u", (uint) index);
                    usage_str = "gl_TexCoord";
                    arrayleft = "[";
                    arrayright = "]";
                } // else if
            } // if

            else if (usage == MOJOSHADER_USAGE_COLOR)
            {
                index_str[0] = '\0';  // no explicit number.
                if (index == 0)
                {
                    usage_str = "gl_Color";
                } // if
                else if (index == 1)
                {
                    usage_str = "gl_SecondaryColor";
                } // else if
                // FIXME: Does this even matter when we have varyings? -flibit
                // else
                //    fail(ctx, "unsupported color index");
            } // else if
#if SUPPORT_PROFILE_GLSLES
            } // if
#endif
        } // else if

        else if (regtype == REG_TYPE_MISCTYPE)
        {
            const MiscTypeType mt = (MiscTypeType) regnum;
            if (mt == MISCTYPE_TYPE_FACE)
            {
                push_output(ctx, &ctx->mainline_intro);
                ctx->indent++;
                output_line(ctx, "float %s = gl_FrontFacing ? 1.0 : -1.0;", var);
                pop_output(ctx);
                return;
            } // if
            else if (mt == MISCTYPE_TYPE_POSITION)
            {
                push_output(ctx, &ctx->globals);
                output_line(ctx, "uniform vec2 vposFlip;");
                pop_output(ctx);

                // TODO: For half-pixel offset compensation, floor() this value!
                push_output(ctx, &ctx->mainline_intro);
                ctx->indent++;
                output_line(ctx, "vec4 %s = vec4(gl_FragCoord.x, (gl_FragCoord.y * vposFlip.x) + vposFlip.y, gl_FragCoord.z, gl_FragCoord.w);", var);
                pop_output(ctx);
                return;
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

        push_output(ctx, &ctx->globals);
        // no mapping to built-in var? Just make it a regular global, pray.
        if (usage_str == NULL)
        {
#if SUPPORT_PROFILE_GLSLES
            if (support_glsles(ctx))
                output_line(ctx, "%s highp vec4 io_%i_%i;", qualifier_in, usage, index);
            else
#endif
            output_line(ctx, "varying vec4 io_%i_%i;", usage, index);
            output_line(ctx, "#define %s io_%i_%i", var, usage, index);
        } // if
        else
        {
            output_line(ctx, "#define %s %s%s%s%s", var, usage_str,
                        arrayleft, index_str, arrayright);
        } // else
        pop_output(ctx);
    } // else if

    else
    {
        fail(ctx, "Unknown shader type");  // state machine should catch this.
    } // else
} // emit_GLSL_attribute

void emit_GLSL_NOP(Context *ctx)
{
    // no-op is a no-op.  :)
} // emit_GLSL_NOP

void emit_GLSL_MOV(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "%s", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_MOV

void emit_GLSL_ADD(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "%s + %s", src0, src1);
    output_line(ctx, "%s", code);
} // emit_GLSL_ADD

void emit_GLSL_SUB(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "%s - %s", src0, src1);
    output_line(ctx, "%s", code);
} // emit_GLSL_SUB

void emit_GLSL_MAD(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char src2[64]; make_GLSL_srcarg_string_masked(ctx, 2, src2, sizeof (src2));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "(%s * %s) + %s", src0, src1, src2);
    output_line(ctx, "%s", code);
} // emit_GLSL_MAD

void emit_GLSL_MUL(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "%s * %s", src0, src1);
    output_line(ctx, "%s", code);
} // emit_GLSL_MUL

void emit_GLSL_RCP(Context *ctx)
{
    const int vecsize = vecsize_from_writemask(ctx->dest_arg.writemask);
    char cast[16] = { '\0' };
    if (vecsize != 1)
        snprintf(cast, sizeof (cast), "vec%d", vecsize);
    char src0[64]; make_GLSL_srcarg_string_scalar(ctx, 0, src0, sizeof (src0));
    char code[128];
    ctx->need_max_float = 1;
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "%s((%s == 0.0) ? FLT_MAX : 1.0 / %s)", cast, src0, src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_RCP

void emit_GLSL_RSQ(Context *ctx)
{
    const int vecsize = vecsize_from_writemask(ctx->dest_arg.writemask);
    char cast[16] = { '\0' };
    if (vecsize != 1)
        snprintf(cast, sizeof (cast), "vec%d", vecsize);

    char src0[64]; make_GLSL_srcarg_string_scalar(ctx, 0, src0, sizeof (src0));
    char code[128];
    ctx->need_max_float = 1;
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "%s((%s == 0.0) ? FLT_MAX : inversesqrt(abs(%s)))", cast, src0, src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_RSQ

void emit_GLSL_dotprod(Context *ctx, const char *src0, const char *src1,
                       const char *extra)
{
    const int vecsize = vecsize_from_writemask(ctx->dest_arg.writemask);
    char castleft[16] = { '\0' };
    const char *castright = "";
    if (vecsize != 1)
    {
        snprintf(castleft, sizeof (castleft), "vec%d(", vecsize);
        castright = ")";
    } // if

    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "%sdot(%s, %s)%s%s",
                             castleft, src0, src1, extra, castright);
    output_line(ctx, "%s", code);
} // emit_GLSL_dotprod

void emit_GLSL_DP3(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_vec3(ctx, 1, src1, sizeof (src1));
    emit_GLSL_dotprod(ctx, src0, src1, "");
} // emit_GLSL_DP3

void emit_GLSL_DP4(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_full(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_full(ctx, 1, src1, sizeof (src1));
    emit_GLSL_dotprod(ctx, src0, src1, "");
} // emit_GLSL_DP4

void emit_GLSL_MIN(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "min(%s, %s)", src0, src1);
    output_line(ctx, "%s", code);
} // emit_GLSL_MIN

void emit_GLSL_MAX(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "max(%s, %s)", src0, src1);
    output_line(ctx, "%s", code);
} // emit_GLSL_MAX

void emit_GLSL_SLT(Context *ctx)
{
    const int vecsize = vecsize_from_writemask(ctx->dest_arg.writemask);
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];

    // float(bool) or vec(bvec) results in 0.0 or 1.0, like SLT wants.
    if (vecsize == 1)
        make_GLSL_destarg_assign(ctx, code, sizeof (code), "float(%s < %s)", src0, src1);
    else
    {
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "vec%d(lessThan(%s, %s))",
                                 vecsize, src0, src1);
    } // else
    output_line(ctx, "%s", code);
} // emit_GLSL_SLT

void emit_GLSL_SGE(Context *ctx)
{
    const int vecsize = vecsize_from_writemask(ctx->dest_arg.writemask);
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];

    // float(bool) or vec(bvec) results in 0.0 or 1.0, like SGE wants.
    if (vecsize == 1)
    {
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "float(%s >= %s)", src0, src1);
    } // if
    else
    {
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "vec%d(greaterThanEqual(%s, %s))",
                                 vecsize, src0, src1);
    } // else
    output_line(ctx, "%s", code);
} // emit_GLSL_SGE

void emit_GLSL_EXP(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "exp2(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_EXP

void emit_GLSL_LOG(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "log2(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_LOG

void emit_GLSL_LIT_helper(Context *ctx)
{
    const char *maxp = "127.9961"; // value from the dx9 reference.

    if (ctx->glsl_generated_lit_helper)
        return;

    ctx->glsl_generated_lit_helper = 1;

    push_output(ctx, &ctx->helpers);
    output_line(ctx, "vec4 LIT(const vec4 src)");
    output_line(ctx, "{"); ctx->indent++;
    output_line(ctx,   "float power = clamp(src.w, -%s, %s);",maxp,maxp);
    output_line(ctx,   "vec4 retval = vec4(1.0, 0.0, 0.0, 1.0);");
    output_line(ctx,   "if (src.x > 0.0) {"); ctx->indent++;
    output_line(ctx,     "retval.y = src.x;");
    output_line(ctx,     "if (src.y > 0.0) {"); ctx->indent++;
    output_line(ctx,       "retval.z = pow(src.y, power);"); ctx->indent--;
    output_line(ctx,     "}"); ctx->indent--;
    output_line(ctx,   "}");
    output_line(ctx,   "return retval;"); ctx->indent--;
    output_line(ctx, "}");
    output_blank_line(ctx);
    pop_output(ctx);
} // emit_GLSL_LIT_helper

void emit_GLSL_LIT(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_full(ctx, 0, src0, sizeof (src0));
    char code[128];
    emit_GLSL_LIT_helper(ctx);
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "LIT(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_LIT

void emit_GLSL_DST(Context *ctx)
{
    // !!! FIXME: needs to take ctx->dst_arg.writemask into account.
    char src0_y[64]; make_GLSL_srcarg_string_y(ctx, 0, src0_y, sizeof (src0_y));
    char src1_y[64]; make_GLSL_srcarg_string_y(ctx, 1, src1_y, sizeof (src1_y));
    char src0_z[64]; make_GLSL_srcarg_string_z(ctx, 0, src0_z, sizeof (src0_z));
    char src1_w[64]; make_GLSL_srcarg_string_w(ctx, 1, src1_w, sizeof (src1_w));

    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                             "vec4(1.0, %s * %s, %s, %s)",
                             src0_y, src1_y, src0_z, src1_w);
    output_line(ctx, "%s", code);
} // emit_GLSL_DST

void emit_GLSL_LRP(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char src2[64]; make_GLSL_srcarg_string_masked(ctx, 2, src2, sizeof (src2));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "mix(%s, %s, %s)",
                             src2, src1, src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_LRP

void emit_GLSL_FRC(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "fract(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_FRC

void emit_GLSL_M4X4(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_full(ctx, 0, src0, sizeof (src0));
    char row0[64]; make_GLSL_srcarg_string_full(ctx, 1, row0, sizeof (row0));
    char row1[64]; make_GLSL_srcarg_string_full(ctx, 2, row1, sizeof (row1));
    char row2[64]; make_GLSL_srcarg_string_full(ctx, 3, row2, sizeof (row2));
    char row3[64]; make_GLSL_srcarg_string_full(ctx, 4, row3, sizeof (row3));
    char code[256];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                    "vec4(dot(%s, %s), dot(%s, %s), dot(%s, %s), dot(%s, %s))",
                    src0, row0, src0, row1, src0, row2, src0, row3);
    output_line(ctx, "%s", code);
} // emit_GLSL_M4X4

void emit_GLSL_M4X3(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_full(ctx, 0, src0, sizeof (src0));
    char row0[64]; make_GLSL_srcarg_string_full(ctx, 1, row0, sizeof (row0));
    char row1[64]; make_GLSL_srcarg_string_full(ctx, 2, row1, sizeof (row1));
    char row2[64]; make_GLSL_srcarg_string_full(ctx, 3, row2, sizeof (row2));
    char code[256];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                "vec3(dot(%s, %s), dot(%s, %s), dot(%s, %s))",
                                src0, row0, src0, row1, src0, row2);
    output_line(ctx, "%s", code);
} // emit_GLSL_M4X3

void emit_GLSL_M3X4(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
    char row0[64]; make_GLSL_srcarg_string_vec3(ctx, 1, row0, sizeof (row0));
    char row1[64]; make_GLSL_srcarg_string_vec3(ctx, 2, row1, sizeof (row1));
    char row2[64]; make_GLSL_srcarg_string_vec3(ctx, 3, row2, sizeof (row2));
    char row3[64]; make_GLSL_srcarg_string_vec3(ctx, 4, row3, sizeof (row3));

    char code[256];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                "vec4(dot(%s, %s), dot(%s, %s), "
                                     "dot(%s, %s), dot(%s, %s))",
                                src0, row0, src0, row1,
                                src0, row2, src0, row3);
    output_line(ctx, "%s", code);
} // emit_GLSL_M3X4

void emit_GLSL_M3X3(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
    char row0[64]; make_GLSL_srcarg_string_vec3(ctx, 1, row0, sizeof (row0));
    char row1[64]; make_GLSL_srcarg_string_vec3(ctx, 2, row1, sizeof (row1));
    char row2[64]; make_GLSL_srcarg_string_vec3(ctx, 3, row2, sizeof (row2));
    char code[256];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                "vec3(dot(%s, %s), dot(%s, %s), dot(%s, %s))",
                                src0, row0, src0, row1, src0, row2);
    output_line(ctx, "%s", code);
} // emit_GLSL_M3X3

void emit_GLSL_M3X2(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
    char row0[64]; make_GLSL_srcarg_string_vec3(ctx, 1, row0, sizeof (row0));
    char row1[64]; make_GLSL_srcarg_string_vec3(ctx, 2, row1, sizeof (row1));

    char code[256];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                "vec2(dot(%s, %s), dot(%s, %s))",
                                src0, row0, src0, row1);
    output_line(ctx, "%s", code);
} // emit_GLSL_M3X2

void emit_GLSL_CALL(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    if (ctx->loops > 0)
        output_line(ctx, "%s(aL);", src0);
    else
        output_line(ctx, "%s();", src0);
} // emit_GLSL_CALL

void emit_GLSL_CALLNZ(Context *ctx)
{
    // !!! FIXME: if src1 is a constbool that's true, we can remove the
    // !!! FIXME:  if. If it's false, we can make this a no-op.
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));

    if (ctx->loops > 0)
        output_line(ctx, "if (%s) { %s(aL); }", src1, src0);
    else
        output_line(ctx, "if (%s) { %s(); }", src1, src0);
} // emit_GLSL_CALLNZ

void emit_GLSL_LOOP(Context *ctx)
{
    // !!! FIXME: swizzle?
    char var[64]; get_GLSL_srcarg_varname(ctx, 1, var, sizeof (var));
    assert(ctx->source_args[0].regnum == 0);  // in case they add aL1 someday.
    output_line(ctx, "{");
    ctx->indent++;
    output_line(ctx, "const int aLend = %s.x + %s.y;", var, var);
    output_line(ctx, "for (int aL = %s.y; aL < aLend; aL += %s.z) {", var, var);
    ctx->indent++;
} // emit_GLSL_LOOP

void emit_GLSL_RET(Context *ctx)
{
    // thankfully, the MSDN specs say a RET _has_ to end a function...no
    //  early returns. So if you hit one, you know you can safely close
    //  a high-level function.
    ctx->indent--;
    output_line(ctx, "}");
    output_blank_line(ctx);
    set_output(ctx, &ctx->subroutines);  // !!! FIXME: is this for LABEL? Maybe set it there so we don't allocate unnecessarily.
} // emit_GLSL_RET

void emit_GLSL_ENDLOOP(Context *ctx)
{
    ctx->indent--;
    output_line(ctx, "}");
    ctx->indent--;
    output_line(ctx, "}");
} // emit_GLSL_ENDLOOP

void emit_GLSL_LABEL(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    const int label = ctx->source_args[0].regnum;
    RegisterList *reg = reglist_find(&ctx->used_registers, REG_TYPE_LABEL, label);
    assert(ctx->output == ctx->subroutines);  // not mainline, etc.
    assert(ctx->indent == 0);  // we shouldn't be in the middle of a function.

    // MSDN specs say CALL* has to come before the LABEL, so we know if we
    //  can ditch the entire function here as unused.
    if (reg == NULL)
        set_output(ctx, &ctx->ignore);  // Func not used. Parse, but don't output.

    // !!! FIXME: it would be nice if we could determine if a function is
    // !!! FIXME:  only called once and, if so, forcibly inline it.

    const char *uses_loopreg = ((reg) && (reg->misc == 1)) ? "int aL" : "";
    output_line(ctx, "void %s(%s)", src0, uses_loopreg);
    output_line(ctx, "{");
    ctx->indent++;
} // emit_GLSL_LABEL

void emit_GLSL_DCL(Context *ctx)
{
    // no-op. We do this in our emit_attribute() and emit_uniform().
} // emit_GLSL_DCL

void emit_GLSL_POW(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                             "pow(abs(%s), %s)", src0, src1);
    output_line(ctx, "%s", code);
} // emit_GLSL_POW

void emit_GLSL_CRS(Context *ctx)
{
    // !!! FIXME: needs to take ctx->dst_arg.writemask into account.
    char src0[64]; make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_vec3(ctx, 1, src1, sizeof (src1));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                             "cross(%s, %s)", src0, src1);
    output_line(ctx, "%s", code);
} // emit_GLSL_CRS

void emit_GLSL_SGN(Context *ctx)
{
    // (we don't need the temporary registers specified for the D3D opcode.)
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "sign(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_SGN

void emit_GLSL_ABS(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "abs(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_ABS

void emit_GLSL_NRM(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "normalize(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_NRM

void emit_GLSL_SINCOS(Context *ctx)
{
    // we don't care about the temp registers that <= sm2 demands; ignore them.
    //  sm2 also talks about what components are left untouched vs. undefined,
    //  but we just leave those all untouched with GLSL write masks (which
    //  would fulfill the "undefined" requirement, too).
    const int mask = ctx->dest_arg.writemask;
    char src0[64]; make_GLSL_srcarg_string_scalar(ctx, 0, src0, sizeof (src0));
    char code[128] = { '\0' };

    if (writemask_x(mask))
        make_GLSL_destarg_assign(ctx, code, sizeof (code), "cos(%s)", src0);
    else if (writemask_y(mask))
        make_GLSL_destarg_assign(ctx, code, sizeof (code), "sin(%s)", src0);
    else if (writemask_xy(mask))
    {
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "vec2(cos(%s), sin(%s))", src0, src0);
    } // else if

    output_line(ctx, "%s", code);
} // emit_GLSL_SINCOS

void emit_GLSL_REP(Context *ctx)
{
    // !!! FIXME:
    // msdn docs say legal loop values are 0 to 255. We can check DEFI values
    //  at parse time, but if they are pulling a value from a uniform, do
    //  we clamp here?
    // !!! FIXME: swizzle is legal here, right?
    char src0[64]; make_GLSL_srcarg_string_x(ctx, 0, src0, sizeof (src0));
    const uint rep = (uint) ctx->reps;
    output_line(ctx, "for (int rep%u = 0; rep%u < %s; rep%u++) {",
                rep, rep, src0, rep);
    ctx->indent++;
} // emit_GLSL_REP

void emit_GLSL_ENDREP(Context *ctx)
{
    ctx->indent--;
    output_line(ctx, "}");
} // emit_GLSL_ENDREP

void emit_GLSL_IF(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_scalar(ctx, 0, src0, sizeof (src0));
    output_line(ctx, "if (%s) {", src0);
    ctx->indent++;
} // emit_GLSL_IF

void emit_GLSL_IFC(Context *ctx)
{
    const char *comp = get_GLSL_comparison_string_scalar(ctx);
    char src0[64]; make_GLSL_srcarg_string_scalar(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_scalar(ctx, 1, src1, sizeof (src1));
    output_line(ctx, "if (%s %s %s) {", src0, comp, src1);
    ctx->indent++;
} // emit_GLSL_IFC

void emit_GLSL_ELSE(Context *ctx)
{
    ctx->indent--;
    output_line(ctx, "} else {");
    ctx->indent++;
} // emit_GLSL_ELSE

void emit_GLSL_ENDIF(Context *ctx)
{
    ctx->indent--;
    output_line(ctx, "}");
} // emit_GLSL_ENDIF

void emit_GLSL_BREAK(Context *ctx)
{
    output_line(ctx, "break;");
} // emit_GLSL_BREAK

void emit_GLSL_BREAKC(Context *ctx)
{
    const char *comp = get_GLSL_comparison_string_scalar(ctx);
    char src0[64]; make_GLSL_srcarg_string_scalar(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_scalar(ctx, 1, src1, sizeof (src1));
    output_line(ctx, "if (%s %s %s) { break; }", src0, comp, src1);
} // emit_GLSL_BREAKC

void emit_GLSL_MOVA(Context *ctx)
{
    const int vecsize = vecsize_from_writemask(ctx->dest_arg.writemask);
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];

    if (vecsize == 1)
    {
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "int(floor(abs(%s) + 0.5) * sign(%s))",
                                 src0, src0);
    } // if

    else
    {
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                            "ivec%d(floor(abs(%s) + vec%d(0.5)) * sign(%s))",
                            vecsize, src0, vecsize, src0);
    } // else

    output_line(ctx, "%s", code);
} // emit_GLSL_MOVA

void emit_GLSL_DEFB(Context *ctx)
{
    char varname[64]; get_GLSL_destarg_varname(ctx, varname, sizeof (varname));
    push_output(ctx, &ctx->globals);
    output_line(ctx, "const bool %s = %s;",
                varname, ctx->dwords[0] ? "true" : "false");
    pop_output(ctx);
} // emit_GLSL_DEFB

void emit_GLSL_DEFI(Context *ctx)
{
    char varname[64]; get_GLSL_destarg_varname(ctx, varname, sizeof (varname));
    const int32 *x = (const int32 *) ctx->dwords;
    push_output(ctx, &ctx->globals);
    output_line(ctx, "const ivec4 %s = ivec4(%d, %d, %d, %d);",
                varname, (int) x[0], (int) x[1], (int) x[2], (int) x[3]);
    pop_output(ctx);
} // emit_GLSL_DEFI

EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXCRD)

void emit_GLSL_TEXKILL(Context *ctx)
{
    char dst[64]; get_GLSL_destarg_varname(ctx, dst, sizeof (dst));
    output_line(ctx, "if (any(lessThan(%s.xyz, vec3(0.0)))) discard;", dst);
} // emit_GLSL_TEXKILL

void emit_GLSL_TEXLD(Context *ctx)
{
    if (!shader_version_atleast(ctx, 1, 4))
    {
        DestArgInfo *info = &ctx->dest_arg;
        char dst[64];
        char sampler[64];
        char code[128] = {0};

        RegisterList *sreg;
        sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER, info->regnum);
        const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);

        // !!! FIXME: this code counts on the register not having swizzles, etc.
        get_GLSL_destarg_varname(ctx, dst, sizeof (dst));
        get_GLSL_varname_in_buf(ctx, REG_TYPE_SAMPLER, info->regnum,
                                sampler, sizeof (sampler));

        if (ttype == TEXTURE_TYPE_2D)
        {
            make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                     "texture2D(%s, %s.xy)",
                                     sampler, dst);
        }
        else if (ttype == TEXTURE_TYPE_CUBE)
        {
            make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                     "textureCube(%s, %s.xyz)",
                                     sampler, dst);
        }
        else if (ttype == TEXTURE_TYPE_VOLUME)
        {
            make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                     "texture3D(%s, %s.xyz)",
                                     sampler, dst);
        }
        else
        {
            fail(ctx, "unexpected texture type");
        } // else
        output_line(ctx, "%s", code);
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
        RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER,
                                          samp_arg->regnum);
        const char *funcname = NULL;
        char src0[64] = { '\0' };
        char src1[64]; get_GLSL_srcarg_varname(ctx, 1, src1, sizeof (src1)); // !!! FIXME: SRC_MOD?

        if (sreg == NULL)
        {
            fail(ctx, "TEXLD using undeclared sampler");
            return;
        } // if

        // !!! FIXME: does the d3d bias value map directly to GLSL?
        const char *biassep = "";
        char bias[64] = { '\0' };
        if (ctx->instruction_controls == CONTROL_TEXLDB)
        {
            biassep = ", ";
            make_GLSL_srcarg_string_w(ctx, 0, bias, sizeof (bias));
        } // if

        switch ((const TextureType) sreg->index)
        {
            case TEXTURE_TYPE_2D:
                if (ctx->instruction_controls == CONTROL_TEXLDP)
                {
                    funcname = "texture2DProj";
                    make_GLSL_srcarg_string_full(ctx, 0, src0, sizeof (src0));
                } // if
                else  // texld/texldb
                {
                    funcname = "texture2D";
                    make_GLSL_srcarg_string_vec2(ctx, 0, src0, sizeof (src0));
                } // else
                break;
            case TEXTURE_TYPE_CUBE:
                if (ctx->instruction_controls == CONTROL_TEXLDP)
                    fail(ctx, "TEXLDP on a cubemap");  // !!! FIXME: is this legal?
                funcname = "textureCube";
                make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
                break;
            case TEXTURE_TYPE_VOLUME:
                if (ctx->instruction_controls == CONTROL_TEXLDP)
                {
                    funcname = "texture3DProj";
                    make_GLSL_srcarg_string_full(ctx, 0, src0, sizeof (src0));
                } // if
                else  // texld/texldb
                {
                    funcname = "texture3D";
                    make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
                } // else
                break;
            default:
                fail(ctx, "unknown texture type");
                return;
        } // switch

        assert(!isscalar(ctx, ctx->shader_type, samp_arg->regtype, samp_arg->regnum));
        char swiz_str[6] = { '\0' };
        make_GLSL_swizzle_string(swiz_str, sizeof (swiz_str),
                                 samp_arg->swizzle, ctx->dest_arg.writemask);

        char code[128];
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "%s(%s, %s%s%s)%s", funcname,
                                 src1, src0, biassep, bias, swiz_str);

        output_line(ctx, "%s", code);
    } // else
} // emit_GLSL_TEXLD
    

void emit_GLSL_TEXBEM(Context *ctx)
{
    DestArgInfo *info = &ctx->dest_arg;
    char dst[64]; get_GLSL_destarg_varname(ctx, dst, sizeof (dst));
    char src[64]; get_GLSL_srcarg_varname(ctx, 0, src, sizeof (src));
    char sampler[64];
    char code[512];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    get_GLSL_varname_in_buf(ctx, REG_TYPE_SAMPLER, info->regnum,
                            sampler, sizeof (sampler));

    make_GLSL_destarg_assign(ctx, code, sizeof (code),
        "texture2D(%s, vec2(%s.x + (%s_texbem.x * %s.x) + (%s_texbem.z * %s.y),"
        " %s.y + (%s_texbem.y * %s.x) + (%s_texbem.w * %s.y)))",
        sampler,
        dst, sampler, src, sampler, src,
        dst, sampler, src, sampler, src);

    output_line(ctx, "%s", code);
} // emit_GLSL_TEXBEM


void emit_GLSL_TEXBEML(Context *ctx)
{
    // !!! FIXME: this code counts on the register not having swizzles, etc.
    DestArgInfo *info = &ctx->dest_arg;
    char dst[64]; get_GLSL_destarg_varname(ctx, dst, sizeof (dst));
    char src[64]; get_GLSL_srcarg_varname(ctx, 0, src, sizeof (src));
    char sampler[64];
    char code[512];

    get_GLSL_varname_in_buf(ctx, REG_TYPE_SAMPLER, info->regnum,
                            sampler, sizeof (sampler));

    make_GLSL_destarg_assign(ctx, code, sizeof (code),
        "(texture2D(%s, vec2(%s.x + (%s_texbem.x * %s.x) + (%s_texbem.z * %s.y),"
        " %s.y + (%s_texbem.y * %s.x) + (%s_texbem.w * %s.y)))) *"
        " ((%s.z * %s_texbeml.x) + %s_texbem.y)",
        sampler,
        dst, sampler, src, sampler, src,
        dst, sampler, src, sampler, src,
        src, sampler, sampler);

    output_line(ctx, "%s", code);
} // emit_GLSL_TEXBEML

EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2AR) // !!! FIXME
EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2GB) // !!! FIXME


void emit_GLSL_TEXM3X2PAD(Context *ctx)
{
    // no-op ... work happens in emit_GLSL_TEXM3X2TEX().
} // emit_GLSL_TEXM3X2PAD

void emit_GLSL_TEXM3X2TEX(Context *ctx)
{
    if (ctx->texm3x2pad_src0 == -1)
        return;

    DestArgInfo *info = &ctx->dest_arg;
    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char sampler[64];
    char code[512];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    get_GLSL_varname_in_buf(ctx, REG_TYPE_SAMPLER, info->regnum,
                            sampler, sizeof (sampler));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x2pad_src0,
                            src0, sizeof (src0));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x2pad_dst0,
                            src1, sizeof (src1));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src2, sizeof (src2));
    get_GLSL_destarg_varname(ctx, dst, sizeof (dst));

    make_GLSL_destarg_assign(ctx, code, sizeof (code),
        "texture2D(%s, vec2(dot(%s.xyz, %s.xyz), dot(%s.xyz, %s.xyz)))",
        sampler, src0, src1, src2, dst);

    output_line(ctx, "%s", code);
} // emit_GLSL_TEXM3X2TEX

void emit_GLSL_TEXM3X3PAD(Context *ctx)
{
    // no-op ... work happens in emit_GLSL_TEXM3X3*().
} // emit_GLSL_TEXM3X3PAD

void emit_GLSL_TEXM3X3TEX(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    DestArgInfo *info = &ctx->dest_arg;
    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char src3[64];
    char src4[64];
    char sampler[64];
    char code[512];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    get_GLSL_varname_in_buf(ctx, REG_TYPE_SAMPLER, info->regnum,
                            sampler, sizeof (sampler));

    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0,
                            src0, sizeof (src0));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0,
                            src1, sizeof (src1));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1,
                            src2, sizeof (src2));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1,
                            src3, sizeof (src3));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src4, sizeof (src4));
    get_GLSL_destarg_varname(ctx, dst, sizeof (dst));

    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER,
                                      info->regnum);
    const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);
    const char *ttypestr = (ttype == TEXTURE_TYPE_CUBE) ? "Cube" : "3D";

    make_GLSL_destarg_assign(ctx, code, sizeof (code),
        "texture%s(%s,"
            " vec3(dot(%s.xyz, %s.xyz),"
            " dot(%s.xyz, %s.xyz),"
            " dot(%s.xyz, %s.xyz)))",
        ttypestr, sampler, src0, src1, src2, src3, dst, src4);

    output_line(ctx, "%s", code);
} // emit_GLSL_TEXM3X3TEX

void emit_GLSL_TEXM3X3SPEC_helper(Context *ctx)
{
    if (ctx->glsl_generated_texm3x3spec_helper)
        return;

    ctx->glsl_generated_texm3x3spec_helper = 1;

    push_output(ctx, &ctx->helpers);
    output_line(ctx, "vec3 TEXM3X3SPEC_reflection(const vec3 normal, const vec3 eyeray)");
    output_line(ctx, "{"); ctx->indent++;
    output_line(ctx,   "return (2.0 * ((normal * eyeray) / (normal * normal)) * normal) - eyeray;"); ctx->indent--;
    output_line(ctx, "}");
    output_blank_line(ctx);
    pop_output(ctx);
} // emit_GLSL_TEXM3X3SPEC_helper

void emit_GLSL_TEXM3X3SPEC(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    DestArgInfo *info = &ctx->dest_arg;
    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char src3[64];
    char src4[64];
    char src5[64];
    char sampler[64];
    char code[512];

    emit_GLSL_TEXM3X3SPEC_helper(ctx);

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    get_GLSL_varname_in_buf(ctx, REG_TYPE_SAMPLER, info->regnum,
                            sampler, sizeof (sampler));

    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0,
                            src0, sizeof (src0));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0,
                            src1, sizeof (src1));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1,
                            src2, sizeof (src2));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1,
                            src3, sizeof (src3));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src4, sizeof (src4));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[1].regnum,
                            src5, sizeof (src5));
    get_GLSL_destarg_varname(ctx, dst, sizeof (dst));

    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER,
                                      info->regnum);
    const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);
    const char *ttypestr = (ttype == TEXTURE_TYPE_CUBE) ? "Cube" : "3D";

    make_GLSL_destarg_assign(ctx, code, sizeof (code),
        "texture%s(%s, "
            "TEXM3X3SPEC_reflection("
                "vec3("
                    "dot(%s.xyz, %s.xyz), "
                    "dot(%s.xyz, %s.xyz), "
                    "dot(%s.xyz, %s.xyz)"
                "),"
                "%s.xyz,"
            ")"
        ")",
        ttypestr, sampler, src0, src1, src2, src3, dst, src4, src5);

    output_line(ctx, "%s", code);
} // emit_GLSL_TEXM3X3SPEC

void emit_GLSL_TEXM3X3VSPEC(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    DestArgInfo *info = &ctx->dest_arg;
    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char src3[64];
    char src4[64];
    char sampler[64];
    char code[512];

    emit_GLSL_TEXM3X3SPEC_helper(ctx);

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    get_GLSL_varname_in_buf(ctx, REG_TYPE_SAMPLER, info->regnum,
                            sampler, sizeof (sampler));

    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0,
                            src0, sizeof (src0));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0,
                            src1, sizeof (src1));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1,
                            src2, sizeof (src2));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1,
                            src3, sizeof (src3));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src4, sizeof (src4));
    get_GLSL_destarg_varname(ctx, dst, sizeof (dst));

    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER,
                                      info->regnum);
    const TextureType ttype = (TextureType) (sreg ? sreg->index : 0);
    const char *ttypestr = (ttype == TEXTURE_TYPE_CUBE) ? "Cube" : "3D";

    make_GLSL_destarg_assign(ctx, code, sizeof (code),
        "texture%s(%s, "
            "TEXM3X3SPEC_reflection("
                "vec3("
                    "dot(%s.xyz, %s.xyz), "
                    "dot(%s.xyz, %s.xyz), "
                    "dot(%s.xyz, %s.xyz)"
                "), "
                "vec3(%s.w, %s.w, %s.w)"
            ")"
        ")",
        ttypestr, sampler, src0, src1, src2, src3, dst, src4, src0, src2, dst);

    output_line(ctx, "%s", code);
} // emit_GLSL_TEXM3X3VSPEC

void emit_GLSL_EXPP(Context *ctx)
{
    // !!! FIXME: msdn's asm docs don't list this opcode, I'll have to check the driver documentation.
    emit_GLSL_EXP(ctx);  // I guess this is just partial precision EXP?
} // emit_GLSL_EXPP

void emit_GLSL_LOGP(Context *ctx)
{
    // LOGP is just low-precision LOG, but we'll take the higher precision.
    emit_GLSL_LOG(ctx);
} // emit_GLSL_LOGP

// common code between CMP and CND.
void emit_GLSL_comparison_operations(Context *ctx, const char *cmp)
{
    int i, j;
    DestArgInfo *dst = &ctx->dest_arg;
    const SourceArgInfo *srcarg0 = &ctx->source_args[0];
    const int origmask = dst->writemask;
    int used_swiz[4] = { 0, 0, 0, 0 };
    const int writemask[4] = { dst->writemask0, dst->writemask1,
                               dst->writemask2, dst->writemask3 };
    const int src0swiz[4] = { srcarg0->swizzle_x, srcarg0->swizzle_y,
                              srcarg0->swizzle_z, srcarg0->swizzle_w };

    for (i = 0; i < 4; i++)
    {
        int mask = (1 << i);

        if (!writemask[i]) continue;
        if (used_swiz[i]) continue;

        // This is a swizzle we haven't checked yet.
        used_swiz[i] = 1;

        // see if there are any other elements swizzled to match (.yyyy)
        for (j = i + 1; j < 4; j++)
        {
            if (!writemask[j]) continue;
            if (src0swiz[i] != src0swiz[j]) continue;
            mask |= (1 << j);
            used_swiz[j] = 1;
        } // for

        // okay, (mask) should be the writemask of swizzles we like.

        //return make_GLSL_srcarg_string(ctx, idx, (1 << 0));

        char src0[64];
        char src1[64];
        char src2[64];
        make_GLSL_srcarg_string(ctx, 0, (1 << i), src0, sizeof (src0));
        make_GLSL_srcarg_string(ctx, 1, mask, src1, sizeof (src1));
        make_GLSL_srcarg_string(ctx, 2, mask, src2, sizeof (src2));

        set_dstarg_writemask(dst, mask);

        char code[128];
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "((%s %s) ? %s : %s)",
                                 src0, cmp, src1, src2);
        output_line(ctx, "%s", code);
    } // for

    set_dstarg_writemask(dst, origmask);
} // emit_GLSL_comparison_operations

void emit_GLSL_CND(Context *ctx)
{
    emit_GLSL_comparison_operations(ctx, "> 0.5");
} // emit_GLSL_CND

void emit_GLSL_DEF(Context *ctx)
{
    const float *val = (const float *) ctx->dwords; // !!! FIXME: could be int?
    char varname[64]; get_GLSL_destarg_varname(ctx, varname, sizeof (varname));
    char val0[32]; floatstr(ctx, val0, sizeof (val0), val[0], 1);
    char val1[32]; floatstr(ctx, val1, sizeof (val1), val[1], 1);
    char val2[32]; floatstr(ctx, val2, sizeof (val2), val[2], 1);
    char val3[32]; floatstr(ctx, val3, sizeof (val3), val[3], 1);

    push_output(ctx, &ctx->globals);
    output_line(ctx, "const vec4 %s = vec4(%s, %s, %s, %s);",
                varname, val0, val1, val2, val3);
    pop_output(ctx);
} // emit_GLSL_DEF

EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXREG2RGB) // !!! FIXME
EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXDP3TEX) // !!! FIXME
EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXM3X2DEPTH) // !!! FIXME
EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXDP3) // !!! FIXME

void emit_GLSL_TEXM3X3(Context *ctx)
{
    if (ctx->texm3x3pad_src1 == -1)
        return;

    char dst[64];
    char src0[64];
    char src1[64];
    char src2[64];
    char src3[64];
    char src4[64];
    char code[512];

    // !!! FIXME: this code counts on the register not having swizzles, etc.
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst0,
                            src0, sizeof (src0));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src0,
                            src1, sizeof (src1));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_dst1,
                            src2, sizeof (src2));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->texm3x3pad_src1,
                            src3, sizeof (src3));
    get_GLSL_varname_in_buf(ctx, REG_TYPE_TEXTURE, ctx->source_args[0].regnum,
                            src4, sizeof (src4));
    get_GLSL_destarg_varname(ctx, dst, sizeof (dst));

    make_GLSL_destarg_assign(ctx, code, sizeof (code),
        "vec4(dot(%s.xyz, %s.xyz), dot(%s.xyz, %s.xyz), dot(%s.xyz, %s.xyz), 1.0)",
        src0, src1, src2, src3, dst, src4);

    output_line(ctx, "%s", code);
} // emit_GLSL_TEXM3X3

EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(TEXDEPTH) // !!! FIXME

void emit_GLSL_CMP(Context *ctx)
{
    emit_GLSL_comparison_operations(ctx, ">= 0.0");
} // emit_GLSL_CMP

EMIT_GLSL_OPCODE_UNIMPLEMENTED_FUNC(BEM) // !!! FIXME

void emit_GLSL_DP2ADD(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_vec2(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_vec2(ctx, 1, src1, sizeof (src1));
    char src2[64]; make_GLSL_srcarg_string_scalar(ctx, 2, src2, sizeof (src2));
    char extra[128]; snprintf(extra, sizeof (extra), " + %s", src2);
    emit_GLSL_dotprod(ctx, src0, src1, extra);
} // emit_GLSL_DP2ADD

void emit_GLSL_DSX(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "dFdx(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_DSX

void emit_GLSL_DSY(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code), "dFdy(%s)", src0);
    output_line(ctx, "%s", code);
} // emit_GLSL_DSY

void emit_GLSL_TEXLDD(Context *ctx)
{
    const SourceArgInfo *samp_arg = &ctx->source_args[1];
    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER,
                                          samp_arg->regnum);
    const char *funcname = NULL;
    char src0[64] = { '\0' };
    char src1[64]; get_GLSL_srcarg_varname(ctx, 1, src1, sizeof (src1)); // !!! FIXME: SRC_MOD?
    char src2[64] = { '\0' };
    char src3[64] = { '\0' };

    if (sreg == NULL)
    {
        fail(ctx, "TEXLDD using undeclared sampler");
        return;
    } // if

    switch ((const TextureType) sreg->index)
    {
        case TEXTURE_TYPE_2D:
            funcname = "texture2D";
            make_GLSL_srcarg_string_vec2(ctx, 0, src0, sizeof (src0));
            make_GLSL_srcarg_string_vec2(ctx, 2, src2, sizeof (src2));
            make_GLSL_srcarg_string_vec2(ctx, 3, src3, sizeof (src3));
            break;
        case TEXTURE_TYPE_CUBE:
            funcname = "textureCube";
            make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
            make_GLSL_srcarg_string_vec3(ctx, 2, src2, sizeof (src2));
            make_GLSL_srcarg_string_vec3(ctx, 3, src3, sizeof (src3));
            break;
        case TEXTURE_TYPE_VOLUME:
            funcname = "texture3D";
            make_GLSL_srcarg_string_vec3(ctx, 0, src0, sizeof (src0));
            make_GLSL_srcarg_string_vec3(ctx, 2, src2, sizeof (src2));
            make_GLSL_srcarg_string_vec3(ctx, 3, src3, sizeof (src3));
            break;
        default:
            fail(ctx, "unknown texture type");
            return;
    } // switch

    assert(!isscalar(ctx, ctx->shader_type, samp_arg->regtype, samp_arg->regnum));
    char swiz_str[6] = { '\0' };
    make_GLSL_swizzle_string(swiz_str, sizeof (swiz_str),
                             samp_arg->swizzle, ctx->dest_arg.writemask);

    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof (code),
                             "%sGrad(%s, %s, %s, %s)%s", funcname,
                             src1, src0, src2, src3, swiz_str);

    prepend_glsl_texlod_extensions(ctx);
    output_line(ctx, "%s", code);
} // emit_GLSL_TEXLDD

void emit_GLSL_SETP(Context *ctx)
{
    const int vecsize = vecsize_from_writemask(ctx->dest_arg.writemask);
    char src0[64]; make_GLSL_srcarg_string_masked(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_GLSL_srcarg_string_masked(ctx, 1, src1, sizeof (src1));
    char code[128];

    // destination is always predicate register (which is type bvec4).
    if (vecsize == 1)
    {
        const char *comp = get_GLSL_comparison_string_scalar(ctx);
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "(%s %s %s)", src0, comp, src1);
    } // if
    else
    {
        const char *comp = get_GLSL_comparison_string_vector(ctx);
        make_GLSL_destarg_assign(ctx, code, sizeof (code),
                                 "%s(%s, %s)", comp, src0, src1);
    } // else

    output_line(ctx, "%s", code);
} // emit_GLSL_SETP

void emit_GLSL_TEXLDL(Context *ctx)
{
    const SourceArgInfo *samp_arg = &ctx->source_args[1];
    RegisterList *sreg = reglist_find(&ctx->samplers, REG_TYPE_SAMPLER,
                                          samp_arg->regnum);
    const char *pattern = NULL;
    char src0[64];
    char src1[64];
    make_GLSL_srcarg_string_full(ctx, 0, src0, sizeof (src0));
    get_GLSL_srcarg_varname(ctx, 1, src1, sizeof (src1)); // !!! FIXME: SRC_MOD?

    if (sreg == NULL)
    {
        fail(ctx, "TEXLDL using undeclared sampler");
        return;
    } // if

    // HLSL tex2dlod accepts (sampler, uv.xyz, uv.w) where uv.w is the LOD
    // GLSL seems to want the dimensionality to match the sampler (.xy vs .xyz)
    //  so we vary the swizzle accordingly
    switch ((const TextureType) sreg->index)
    {
        case TEXTURE_TYPE_2D:
            pattern = "texture2DLod(%s, %s.xy, %s.w)%s";
            break;
        case TEXTURE_TYPE_CUBE:
            pattern = "textureCubeLod(%s, %s.xyz, %s.w)%s";
            break;
        case TEXTURE_TYPE_VOLUME:
            pattern = "texture3DLod(%s, %s.xyz, %s.w)%s";
            break;
        default:
            fail(ctx, "unknown texture type");
            return;
    } // switch

    assert(!isscalar(ctx, ctx->shader_type, samp_arg->regtype, samp_arg->regnum));
    char swiz_str[6] = { '\0' };
    make_GLSL_swizzle_string(swiz_str, sizeof (swiz_str),
                             samp_arg->swizzle, ctx->dest_arg.writemask);

    char code[128];
    make_GLSL_destarg_assign(ctx, code, sizeof(code),
        pattern, src1, src0, src0, swiz_str);

    prepend_glsl_texlod_extensions(ctx);
    output_line(ctx, "%s", code);
} // emit_GLSL_TEXLDL

void emit_GLSL_BREAKP(Context *ctx)
{
    char src0[64]; make_GLSL_srcarg_string_scalar(ctx, 0, src0, sizeof (src0));
    output_line(ctx, "if (%s) { break; }", src0);
} // emit_GLSL_BREAKP

void emit_GLSL_RESERVED(Context *ctx)
{
    // do nothing; fails in the state machine.
} // emit_GLSL_RESERVED

#endif  // SUPPORT_PROFILE_GLSL

#pragma GCC visibility pop
