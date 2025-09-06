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

#if SUPPORT_PROFILE_D3D

const char *make_D3D_srcarg_string_in_buf(Context *ctx,
                                          const SourceArgInfo *arg,
                                          char *buf, size_t buflen)
{
    const char *premod_str = "";
    const char *postmod_str = "";
    switch (arg->src_mod)
    {
        case SRCMOD_NEGATE:
            premod_str = "-";
            break;

        case SRCMOD_BIASNEGATE:
            premod_str = "-";
            // fall through.
        case SRCMOD_BIAS:
            postmod_str = "_bias";
            break;

        case SRCMOD_SIGNNEGATE:
            premod_str = "-";
            // fall through.
        case SRCMOD_SIGN:
            postmod_str = "_bx2";
            break;

        case SRCMOD_COMPLEMENT:
            premod_str = "1-";
            break;

        case SRCMOD_X2NEGATE:
            premod_str = "-";
            // fall through.
        case SRCMOD_X2:
            postmod_str = "_x2";
            break;

        case SRCMOD_DZ:
            postmod_str = "_dz";
            break;

        case SRCMOD_DW:
            postmod_str = "_dw";
            break;

        case SRCMOD_ABSNEGATE:
            premod_str = "-";
            // fall through.
        case SRCMOD_ABS:
            postmod_str = "_abs";
            break;

        case SRCMOD_NOT:
            premod_str = "!";
            break;

        case SRCMOD_NONE:
        case SRCMOD_TOTAL:
             break;  // stop compiler whining.
    } // switch


    char regnum_str[16];
    const char *regtype_str = get_D3D_register_string(ctx, arg->regtype,
                                                      arg->regnum, regnum_str,
                                                      sizeof (regnum_str));

    if (regtype_str == NULL)
    {
        fail(ctx, "Unknown source register type.");
        *buf = '\0';
        return buf;
    } // if

    const char *rel_lbracket = "";
    const char *rel_rbracket = "";
    char rel_swizzle[4] = { '\0' };
    char rel_regnum_str[16] = { '\0' };
    const char *rel_regtype_str = "";
    if (arg->relative)
    {
        if (arg->relative_regtype == REG_TYPE_LOOP)
        {
            rel_swizzle[0] = '\0';
            rel_swizzle[1] = '\0';
            rel_swizzle[2] = '\0';
        } // if
        else
        {
            rel_swizzle[0] = '.';
            rel_swizzle[1] = swizzle_channels[arg->relative_component];
            rel_swizzle[2] = '\0';
        } // else

        rel_lbracket = "[";
        rel_rbracket = "]";
        rel_regtype_str = get_D3D_register_string(ctx, arg->relative_regtype,
                                                  arg->relative_regnum,
                                                  rel_regnum_str,
                                                  sizeof (rel_regnum_str));

        if (regtype_str == NULL)
        {
            fail(ctx, "Unknown relative source register type.");
            *buf = '\0';
            return buf;
        } // if
    } // if

    char swizzle_str[6];
    size_t i = 0;
    const int scalar = isscalar(ctx, ctx->shader_type, arg->regtype, arg->regnum);
    if (!scalar && !no_swizzle(arg->swizzle))
    {
        swizzle_str[i++] = '.';
        swizzle_str[i++] = swizzle_channels[arg->swizzle_x];
        swizzle_str[i++] = swizzle_channels[arg->swizzle_y];
        swizzle_str[i++] = swizzle_channels[arg->swizzle_z];
        swizzle_str[i++] = swizzle_channels[arg->swizzle_w];

        // .xyzz is the same as .xyz, .z is the same as .zzzz, etc.
        while (swizzle_str[i-1] == swizzle_str[i-2])
            i--;
    } // if
    swizzle_str[i] = '\0';
    assert(i < sizeof (swizzle_str));

    // !!! FIXME: c12[a0.x] actually needs to be c[a0.x + 12]
    snprintf(buf, buflen, "%s%s%s%s%s%s%s%s%s%s",
             premod_str, regtype_str, regnum_str, postmod_str,
             rel_lbracket, rel_regtype_str, rel_regnum_str, rel_swizzle,
             rel_rbracket, swizzle_str);
    // !!! FIXME: make sure the scratch buffer was large enough.
    return buf;
} // make_D3D_srcarg_string_in_buf


const char *make_D3D_destarg_string(Context *ctx, char *buf,
                                    const size_t buflen)
{
    const DestArgInfo *arg = &ctx->dest_arg;

    const char *result_shift_str = "";
    switch (arg->result_shift)
    {
        case 0x1: result_shift_str = "_x2"; break;
        case 0x2: result_shift_str = "_x4"; break;
        case 0x3: result_shift_str = "_x8"; break;
        case 0xD: result_shift_str = "_d8"; break;
        case 0xE: result_shift_str = "_d4"; break;
        case 0xF: result_shift_str = "_d2"; break;
    } // switch

    const char *sat_str = (arg->result_mod & MOD_SATURATE) ? "_sat" : "";
    const char *pp_str = (arg->result_mod & MOD_PP) ? "_pp" : "";
    const char *cent_str = (arg->result_mod & MOD_CENTROID) ? "_centroid" : "";

    char regnum_str[16];
    const char *regtype_str = get_D3D_register_string(ctx, arg->regtype,
                                                      arg->regnum, regnum_str,
                                                      sizeof (regnum_str));
    if (regtype_str == NULL)
    {
        fail(ctx, "Unknown destination register type.");
        *buf = '\0';
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

    const char *pred_left = "";
    const char *pred_right = "";
    char pred[32] = { '\0' };
    if (ctx->predicated)
    {
        pred_left = "(";
        pred_right = ") ";
        make_D3D_srcarg_string_in_buf(ctx, &ctx->predicate_arg,
                                      pred, sizeof (pred));
    } // if

    // may turn out something like "_x2_sat_pp_centroid (!p0.x) r0.xyzw" ...
    snprintf(buf, buflen, "%s%s%s%s %s%s%s%s%s%s",
             result_shift_str, sat_str, pp_str, cent_str,
             pred_left, pred, pred_right,
             regtype_str, regnum_str, writemask_str);
    // !!! FIXME: make sure the scratch buffer was large enough.
    return buf;
} // make_D3D_destarg_string


const char *make_D3D_srcarg_string(Context *ctx, const size_t idx,
                                   char *buf, size_t buflen)
{
    if (idx >= STATICARRAYLEN(ctx->source_args))
    {
        fail(ctx, "Too many source args");
        *buf = '\0';
        return buf;
    } // if

    const SourceArgInfo *arg = &ctx->source_args[idx];
    return make_D3D_srcarg_string_in_buf(ctx, arg, buf, buflen);
} // make_D3D_srcarg_string

const char *get_D3D_const_array_varname(Context *ctx, int base, int size)
{
    char buf[64];
    snprintf(buf, sizeof (buf), "c_array_%d_%d", base, size);
    return StrDup(ctx, buf);
} // get_D3D_const_array_varname


void emit_D3D_start(Context *ctx, const char *profilestr)
{
    const uint major = (uint) ctx->major_ver;
    const uint minor = (uint) ctx->minor_ver;
    char minor_str[16];

    ctx->ignores_ctab = 1;

    if (minor == 0xFF)
        strcpy(minor_str, "sw");
    else if ((major > 1) && (minor == 1))
        strcpy(minor_str, "x");  // for >= SM2, apparently this is "x". Weird.
    else
        snprintf(minor_str, sizeof (minor_str), "%u", (uint) minor);

    output_line(ctx, "%s_%u_%s", ctx->shader_type_str, major, minor_str);
} // emit_D3D_start


void emit_D3D_end(Context *ctx)
{
    output_line(ctx, "end");
} // emit_D3D_end


void emit_D3D_phase(Context *ctx)
{
    output_line(ctx, "phase");
} // emit_D3D_phase


void emit_D3D_finalize(Context *ctx)
{
    // no-op.
} // emit_D3D_finalize


void emit_D3D_global(Context *ctx, RegisterType regtype, int regnum)
{
    // no-op.
} // emit_D3D_global


void emit_D3D_array(Context *ctx, VariableList *var)
{
    // no-op.
} // emit_D3D_array


void emit_D3D_const_array(Context *ctx, const ConstantsList *clist,
                          int base, int size)
{
    // no-op.
} // emit_D3D_const_array


void emit_D3D_uniform(Context *ctx, RegisterType regtype, int regnum,
                      const VariableList *var)
{
    // no-op.
} // emit_D3D_uniform


void emit_D3D_sampler(Context *ctx, int s, TextureType ttype, int tb)
{
    // no-op.
} // emit_D3D_sampler


void emit_D3D_attribute(Context *ctx, RegisterType regtype, int regnum,
                        MOJOSHADER_usage usage, int index, int wmask,
                        int flags)
{
    // no-op.
} // emit_D3D_attribute


void emit_D3D_RESERVED(Context *ctx)
{
    // do nothing; fails in the state machine.
} // emit_D3D_RESERVED


// Generic D3D opcode emitters. A list of macros generate all the entry points
//  that call into these...

char *lowercase(char *dst, const char *src)
{
    int i = 0;
    do
    {
        const char ch = src[i];
        dst[i] = (((ch >= 'A') && (ch <= 'Z')) ? (ch - ('A' - 'a')) : ch);
    } while (src[i++]);
    return dst;
} // lowercase


void emit_D3D_opcode_d(Context *ctx, const char *opcode)
{
    char dst[64]; make_D3D_destarg_string(ctx, dst, sizeof (dst));
    opcode = lowercase((char *) alloca(strlen(opcode) + 1), opcode);
    output_line(ctx, "%s%s%s", ctx->coissue ? "+" : "", opcode, dst);
} // emit_D3D_opcode_d


void emit_D3D_opcode_s(Context *ctx, const char *opcode)
{
    char src0[64]; make_D3D_srcarg_string(ctx, 0, src0, sizeof (src0));
    opcode = lowercase((char *) alloca(strlen(opcode) + 1), opcode);
    output_line(ctx, "%s%s %s", ctx->coissue ? "+" : "", opcode, src0);
} // emit_D3D_opcode_s


void emit_D3D_opcode_ss(Context *ctx, const char *opcode)
{
    char src0[64]; make_D3D_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_D3D_srcarg_string(ctx, 1, src1, sizeof (src1));
    opcode = lowercase((char *) alloca(strlen(opcode) + 1), opcode);
    output_line(ctx, "%s%s %s, %s", ctx->coissue ? "+" : "", opcode, src0, src1);
} // emit_D3D_opcode_ss


void emit_D3D_opcode_ds(Context *ctx, const char *opcode)
{
    char dst[64]; make_D3D_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_D3D_srcarg_string(ctx, 0, src0, sizeof (src0));
    opcode = lowercase((char *) alloca(strlen(opcode) + 1), opcode);
    output_line(ctx, "%s%s%s, %s", ctx->coissue ? "+" : "", opcode, dst, src0);
} // emit_D3D_opcode_ds


void emit_D3D_opcode_dss(Context *ctx, const char *opcode)
{
    char dst[64]; make_D3D_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_D3D_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_D3D_srcarg_string(ctx, 1, src1, sizeof (src1));
    opcode = lowercase((char *) alloca(strlen(opcode) + 1), opcode);
    output_line(ctx, "%s%s%s, %s, %s", ctx->coissue ? "+" : "",
                opcode, dst, src0, src1);
} // emit_D3D_opcode_dss


void emit_D3D_opcode_dsss(Context *ctx, const char *opcode)
{
    char dst[64]; make_D3D_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_D3D_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_D3D_srcarg_string(ctx, 1, src1, sizeof (src1));
    char src2[64]; make_D3D_srcarg_string(ctx, 2, src2, sizeof (src2));
    opcode = lowercase((char *) alloca(strlen(opcode) + 1), opcode);
    output_line(ctx, "%s%s%s, %s, %s, %s", ctx->coissue ? "+" : "", 
                opcode, dst, src0, src1, src2);
} // emit_D3D_opcode_dsss


void emit_D3D_opcode_dssss(Context *ctx, const char *opcode)
{
    char dst[64]; make_D3D_destarg_string(ctx, dst, sizeof (dst));
    char src0[64]; make_D3D_srcarg_string(ctx, 0, src0, sizeof (src0));
    char src1[64]; make_D3D_srcarg_string(ctx, 1, src1, sizeof (src1));
    char src2[64]; make_D3D_srcarg_string(ctx, 2, src2, sizeof (src2));
    char src3[64]; make_D3D_srcarg_string(ctx, 3, src3, sizeof (src3));
    opcode = lowercase((char *) alloca(strlen(opcode) + 1), opcode);
    output_line(ctx,"%s%s%s, %s, %s, %s, %s", ctx->coissue ? "+" : "",
                opcode, dst, src0, src1, src2, src3);
} // emit_D3D_opcode_dssss


void emit_D3D_opcode(Context *ctx, const char *opcode)
{
    opcode = lowercase((char *) alloca(strlen(opcode) + 1), opcode);
    output_line(ctx, "%s%s", ctx->coissue ? "+" : "", opcode);
} // emit_D3D_opcode


#define EMIT_D3D_OPCODE_FUNC(op) \
    void emit_D3D_##op(Context *ctx) { \
        emit_D3D_opcode(ctx, #op); \
    }
#define EMIT_D3D_OPCODE_D_FUNC(op) \
    void emit_D3D_##op(Context *ctx) { \
        emit_D3D_opcode_d(ctx, #op); \
    }
#define EMIT_D3D_OPCODE_S_FUNC(op) \
    void emit_D3D_##op(Context *ctx) { \
        emit_D3D_opcode_s(ctx, #op); \
    }
#define EMIT_D3D_OPCODE_SS_FUNC(op) \
    void emit_D3D_##op(Context *ctx) { \
        emit_D3D_opcode_ss(ctx, #op); \
    }
#define EMIT_D3D_OPCODE_DS_FUNC(op) \
    void emit_D3D_##op(Context *ctx) { \
        emit_D3D_opcode_ds(ctx, #op); \
    }
#define EMIT_D3D_OPCODE_DSS_FUNC(op) \
    void emit_D3D_##op(Context *ctx) { \
        emit_D3D_opcode_dss(ctx, #op); \
    }
#define EMIT_D3D_OPCODE_DSSS_FUNC(op) \
    void emit_D3D_##op(Context *ctx) { \
        emit_D3D_opcode_dsss(ctx, #op); \
    }
#define EMIT_D3D_OPCODE_DSSSS_FUNC(op) \
    void emit_D3D_##op(Context *ctx) { \
        emit_D3D_opcode_dssss(ctx, #op); \
    }

EMIT_D3D_OPCODE_FUNC(NOP)
EMIT_D3D_OPCODE_DS_FUNC(MOV)
EMIT_D3D_OPCODE_DSS_FUNC(ADD)
EMIT_D3D_OPCODE_DSS_FUNC(SUB)
EMIT_D3D_OPCODE_DSSS_FUNC(MAD)
EMIT_D3D_OPCODE_DSS_FUNC(MUL)
EMIT_D3D_OPCODE_DS_FUNC(RCP)
EMIT_D3D_OPCODE_DS_FUNC(RSQ)
EMIT_D3D_OPCODE_DSS_FUNC(DP3)
EMIT_D3D_OPCODE_DSS_FUNC(DP4)
EMIT_D3D_OPCODE_DSS_FUNC(MIN)
EMIT_D3D_OPCODE_DSS_FUNC(MAX)
EMIT_D3D_OPCODE_DSS_FUNC(SLT)
EMIT_D3D_OPCODE_DSS_FUNC(SGE)
EMIT_D3D_OPCODE_DS_FUNC(EXP)
EMIT_D3D_OPCODE_DS_FUNC(LOG)
EMIT_D3D_OPCODE_DS_FUNC(LIT)
EMIT_D3D_OPCODE_DSS_FUNC(DST)
EMIT_D3D_OPCODE_DSSS_FUNC(LRP)
EMIT_D3D_OPCODE_DS_FUNC(FRC)
EMIT_D3D_OPCODE_DSS_FUNC(M4X4)
EMIT_D3D_OPCODE_DSS_FUNC(M4X3)
EMIT_D3D_OPCODE_DSS_FUNC(M3X4)
EMIT_D3D_OPCODE_DSS_FUNC(M3X3)
EMIT_D3D_OPCODE_DSS_FUNC(M3X2)
EMIT_D3D_OPCODE_S_FUNC(CALL)
EMIT_D3D_OPCODE_SS_FUNC(CALLNZ)
EMIT_D3D_OPCODE_SS_FUNC(LOOP)
EMIT_D3D_OPCODE_FUNC(RET)
EMIT_D3D_OPCODE_FUNC(ENDLOOP)
EMIT_D3D_OPCODE_S_FUNC(LABEL)
EMIT_D3D_OPCODE_DSS_FUNC(POW)
EMIT_D3D_OPCODE_DSS_FUNC(CRS)
EMIT_D3D_OPCODE_DSSS_FUNC(SGN)
EMIT_D3D_OPCODE_DS_FUNC(ABS)
EMIT_D3D_OPCODE_DS_FUNC(NRM)
EMIT_D3D_OPCODE_S_FUNC(REP)
EMIT_D3D_OPCODE_FUNC(ENDREP)
EMIT_D3D_OPCODE_S_FUNC(IF)
EMIT_D3D_OPCODE_FUNC(ELSE)
EMIT_D3D_OPCODE_FUNC(ENDIF)
EMIT_D3D_OPCODE_FUNC(BREAK)
EMIT_D3D_OPCODE_DS_FUNC(MOVA)
EMIT_D3D_OPCODE_D_FUNC(TEXKILL)
EMIT_D3D_OPCODE_DS_FUNC(TEXBEM)
EMIT_D3D_OPCODE_DS_FUNC(TEXBEML)
EMIT_D3D_OPCODE_DS_FUNC(TEXREG2AR)
EMIT_D3D_OPCODE_DS_FUNC(TEXREG2GB)
EMIT_D3D_OPCODE_DS_FUNC(TEXM3X2PAD)
EMIT_D3D_OPCODE_DS_FUNC(TEXM3X2TEX)
EMIT_D3D_OPCODE_DS_FUNC(TEXM3X3PAD)
EMIT_D3D_OPCODE_DS_FUNC(TEXM3X3TEX)
EMIT_D3D_OPCODE_DSS_FUNC(TEXM3X3SPEC)
EMIT_D3D_OPCODE_DS_FUNC(TEXM3X3VSPEC)
EMIT_D3D_OPCODE_DS_FUNC(EXPP)
EMIT_D3D_OPCODE_DS_FUNC(LOGP)
EMIT_D3D_OPCODE_DSSS_FUNC(CND)
EMIT_D3D_OPCODE_DS_FUNC(TEXREG2RGB)
EMIT_D3D_OPCODE_DS_FUNC(TEXDP3TEX)
EMIT_D3D_OPCODE_DS_FUNC(TEXM3X2DEPTH)
EMIT_D3D_OPCODE_DS_FUNC(TEXDP3)
EMIT_D3D_OPCODE_DS_FUNC(TEXM3X3)
EMIT_D3D_OPCODE_D_FUNC(TEXDEPTH)
EMIT_D3D_OPCODE_DSSS_FUNC(CMP)
EMIT_D3D_OPCODE_DSS_FUNC(BEM)
EMIT_D3D_OPCODE_DSSS_FUNC(DP2ADD)
EMIT_D3D_OPCODE_DS_FUNC(DSX)
EMIT_D3D_OPCODE_DS_FUNC(DSY)
EMIT_D3D_OPCODE_DSSSS_FUNC(TEXLDD)
EMIT_D3D_OPCODE_DSS_FUNC(TEXLDL)
EMIT_D3D_OPCODE_S_FUNC(BREAKP)

// special cases for comparison opcodes...
const char *get_D3D_comparison_string(Context *ctx)
{
    const char *comps[] = {
        "", "_gt", "_eq", "_ge", "_lt", "_ne", "_le"
    };

    if (ctx->instruction_controls >= STATICARRAYLEN(comps))
    {
        fail(ctx, "unknown comparison control");
        return "";
    } // if

    return comps[ctx->instruction_controls];
} // get_D3D_comparison_string

void emit_D3D_BREAKC(Context *ctx)
{
    char op[16];
    snprintf(op, sizeof (op), "break%s", get_D3D_comparison_string(ctx));
    emit_D3D_opcode_ss(ctx, op);
} // emit_D3D_BREAKC

void emit_D3D_IFC(Context *ctx)
{
    char op[16];
    snprintf(op, sizeof (op), "if%s", get_D3D_comparison_string(ctx));
    emit_D3D_opcode_ss(ctx, op);
} // emit_D3D_IFC

void emit_D3D_SETP(Context *ctx)
{
    char op[16];
    snprintf(op, sizeof (op), "setp%s", get_D3D_comparison_string(ctx));
    emit_D3D_opcode_dss(ctx, op);
} // emit_D3D_SETP

void emit_D3D_DEF(Context *ctx)
{
    char dst[64];
    make_D3D_destarg_string(ctx, dst, sizeof (dst));
    const float *val = (const float *) ctx->dwords; // !!! FIXME: could be int?
    char val0[32];
    char val1[32];
    char val2[32];
    char val3[32];
    floatstr(ctx, val0, sizeof (val0), val[0], 0);
    floatstr(ctx, val1, sizeof (val1), val[1], 0);
    floatstr(ctx, val2, sizeof (val2), val[2], 0);
    floatstr(ctx, val3, sizeof (val3), val[3], 0);
    output_line(ctx, "def%s, %s, %s, %s, %s", dst, val0, val1, val2, val3);
} // emit_D3D_DEF

void emit_D3D_DEFI(Context *ctx)
{
    char dst[64];
    make_D3D_destarg_string(ctx, dst, sizeof (dst));
    const int32 *x = (const int32 *) ctx->dwords;
    output_line(ctx, "defi%s, %d, %d, %d, %d", dst,
                (int) x[0], (int) x[1], (int) x[2], (int) x[3]);
} // emit_D3D_DEFI

void emit_D3D_DEFB(Context *ctx)
{
    char dst[64];
    make_D3D_destarg_string(ctx, dst, sizeof (dst));
    output_line(ctx, "defb%s, %s", dst, ctx->dwords[0] ? "true" : "false");
} // emit_D3D_DEFB


static const char *usagestrs[] = {
    "_position", "_blendweight", "_blendindices", "_normal", "_psize",
    "_texcoord", "_tangent", "_binormal", "_tessfactor", "_positiont",
    "_color", "_fog", "_depth", "_sample"
};

void emit_D3D_DCL(Context *ctx)
{
    char dst[64];
    make_D3D_destarg_string(ctx, dst, sizeof (dst));
    const DestArgInfo *arg = &ctx->dest_arg;
    const char *usage_str = "";
    char index_str[16] = { '\0' };

    if (arg->regtype == REG_TYPE_SAMPLER)
    {
        switch ((const TextureType) ctx->dwords[0])
        {
            case TEXTURE_TYPE_2D: usage_str = "_2d"; break;
            case TEXTURE_TYPE_CUBE: usage_str = "_cube"; break;
            case TEXTURE_TYPE_VOLUME: usage_str = "_volume"; break;
            default: fail(ctx, "unknown sampler texture type"); return;
        } // switch
    } // if

    else if (arg->regtype == REG_TYPE_MISCTYPE)
    {
        switch ((const MiscTypeType) arg->regnum)
        {
            case MISCTYPE_TYPE_POSITION:
            case MISCTYPE_TYPE_FACE:
                usage_str = "";  // just become "dcl vFace" or whatever.
                break;
            default: fail(ctx, "unknown misc register type"); return;
        } // switch
    } // else if

    else
    {
        const uint32 usage = ctx->dwords[0];
        const uint32 index = ctx->dwords[1];
        usage_str = usagestrs[usage];
        if (index != 0)
            snprintf(index_str, sizeof (index_str), "%u", (uint) index);
    } // else

    output_line(ctx, "dcl%s%s%s", usage_str, index_str, dst);
} // emit_D3D_DCL


void emit_D3D_TEXCRD(Context *ctx)
{
    // this opcode looks and acts differently depending on the shader model.
    if (shader_version_atleast(ctx, 1, 4))
        emit_D3D_opcode_ds(ctx, "texcrd");
    else
        emit_D3D_opcode_d(ctx, "texcoord");
} // emit_D3D_TEXCOORD

void emit_D3D_TEXLD(Context *ctx)
{
    // this opcode looks and acts differently depending on the shader model.
    if (shader_version_atleast(ctx, 2, 0))
    {
        if (ctx->instruction_controls == CONTROL_TEXLD)
           emit_D3D_opcode_dss(ctx, "texld");
        else if (ctx->instruction_controls == CONTROL_TEXLDP)
           emit_D3D_opcode_dss(ctx, "texldp");
        else if (ctx->instruction_controls == CONTROL_TEXLDB)
           emit_D3D_opcode_dss(ctx, "texldb");
    } // if

    else if (shader_version_atleast(ctx, 1, 4))
    {
        emit_D3D_opcode_ds(ctx, "texld");
    } // else if

    else
    {
        emit_D3D_opcode_d(ctx, "tex");
    } // else
} // emit_D3D_TEXLD

void emit_D3D_SINCOS(Context *ctx)
{
    // this opcode needs extra registers for sm2 and lower.
    if (!shader_version_atleast(ctx, 3, 0))
        emit_D3D_opcode_dsss(ctx, "sincos");
    else
        emit_D3D_opcode_ds(ctx, "sincos");
} // emit_D3D_SINCOS

#endif  // SUPPORT_PROFILE_D3D

#pragma GCC visibility pop
