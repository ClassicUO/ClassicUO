/**
 * MojoShader; generate shader programs from bytecode of compiled
 *  Direct3D shaders.
 *
 * Please see the file LICENSE.txt in the source's root directory.
 *
 *  This file written by Ryan C. Gordon.
 */

#ifndef MOJOSHADER_PROFILE_H
#define MOJOSHADER_PROFILE_H

#include "../mojoshader_internal.h"

#if SUPPORT_PROFILE_SPIRV
#include "mojoshader_profile_spirv.h"
#endif

typedef struct ConstantsList
{
    MOJOSHADER_constant constant;
    struct ConstantsList *next;
} ConstantsList;

typedef struct VariableList
{
    MOJOSHADER_uniformType type;
    int index;
    int count;
    ConstantsList *constant;
    int used;
    int emit_position;  // used in some profiles.
    struct VariableList *next;
} VariableList;

typedef struct RegisterList
{
    RegisterType regtype;
    int regnum;
    MOJOSHADER_usage usage;
    unsigned int index;
    int writemask;
    int misc;
    int written;
#if SUPPORT_PROFILE_SPIRV
    struct {
        uint32 iddecl;
        int is_ssa; // FIXME(krolli): Is there an existing way to tell constants and uniforms apart?
    } spirv;
#endif
    const VariableList *array;
    struct RegisterList *next;
} RegisterList;

typedef struct
{
    const uint32 *token;   // this is the unmolested token in the stream.
    int regnum;
    int swizzle;  // xyzw (all four, not split out).
    int swizzle_x;
    int swizzle_y;
    int swizzle_z;
    int swizzle_w;
    SourceMod src_mod;
    RegisterType regtype;
    int relative;
    RegisterType relative_regtype;
    int relative_regnum;
    int relative_component;
    const VariableList *relative_array;
} SourceArgInfo;

struct Profile;  // predeclare.

typedef struct CtabData
{
    int have_ctab;
    int symbol_count;
    MOJOSHADER_symbol *symbols;
} CtabData;

// Context...this is state that changes as we parse through a shader...
typedef struct Context
{
    int isfail;
    int out_of_memory;
    MOJOSHADER_malloc malloc;
    MOJOSHADER_free free;
    void *malloc_data;
    int current_position;
    const uint32 *orig_tokens;
    const uint32 *tokens;
    uint32 tokencount;
    int know_shader_size;
    const MOJOSHADER_swizzle *swizzles;
    unsigned int swizzles_count;
    const MOJOSHADER_samplerMap *samplermap;
    unsigned int samplermap_count;
    Buffer *output;
    Buffer *preflight;
    Buffer *globals;
    Buffer *inputs;
    Buffer *outputs;
    Buffer *helpers;
    Buffer *subroutines;
    Buffer *mainline_intro;
    Buffer *mainline_arguments;
    Buffer *mainline_top;
    Buffer *mainline;
    Buffer *postflight;
    Buffer *ignore;
    Buffer *output_stack[3];
    int indent_stack[3];
    int output_stack_len;
    int indent;
    const char *shader_type_str;
    const char *endline;
    const char *mainfn;
    int endline_len;
    int profileid;
    const struct Profile *profile;
    MOJOSHADER_shaderType shader_type;
    uint8 major_ver;
    uint8 minor_ver;
    DestArgInfo dest_arg;
    SourceArgInfo source_args[5];
    SourceArgInfo predicate_arg;  // for predicated instructions.
    uint32 dwords[4];
    uint32 version_token;
    int instruction_count;
    uint32 instruction_controls;
    uint32 previous_opcode;
    int coissue;
    int loops;
    int reps;
    int max_reps;
    int cmps;
    int scratch_registers;
    int max_scratch_registers;
    int branch_labels_stack_index;
    int branch_labels_stack[32];
    int assigned_branch_labels;
    int assigned_vertex_attributes;
    int last_address_reg_component;
    RegisterList used_registers;
    RegisterList defined_registers;
    ErrorList *errors;
    int constant_count;
    ConstantsList *constants;
    int uniform_count;
    int uniform_float4_count;
    int uniform_int4_count;
    int uniform_bool_count;
    RegisterList uniforms;
    int attribute_count;
    RegisterList attributes;
    int sampler_count;
    RegisterList samplers;
    VariableList *variables;  // variables to register mapping.
    int centroid_allowed;
    CtabData ctab;
    int have_relative_input_registers;
    int have_multi_color_outputs;
    int determined_constants_arrays;
    int predicated;
    int uses_pointsize;
    int uses_fog;
    int need_max_float;

    // !!! FIXME: move these into SUPPORT_PROFILE sections.
    int glsl_generated_lit_helper;
    int glsl_generated_texlod_setup;
    int glsl_generated_texm3x3spec_helper;
    int arb1_wrote_position;
    // !!! FIXME: move these into SUPPORT_PROFILE sections.

    int have_preshader;
    int ignores_ctab;
    int reset_texmpad;
    int texm3x2pad_dst0;
    int texm3x2pad_src0;
    int texm3x3pad_dst0;
    int texm3x3pad_src0;
    int texm3x3pad_dst1;
    int texm3x3pad_src1;
    MOJOSHADER_preshader *preshader;

#if SUPPORT_PROFILE_ARB1_NV
    int profile_supports_nv2;
    int profile_supports_nv3;
    int profile_supports_nv4;
#endif
#if SUPPORT_PROFILE_GLSL120
    int profile_supports_glsl120;
#endif
#if SUPPORT_PROFILE_GLSLES
    int profile_supports_glsles;
#endif
#if SUPPORT_PROFILE_GLSLES3
    int profile_supports_glsles3;
#endif

#if SUPPORT_PROFILE_METAL
    int metal_need_header_common;
    int metal_need_header_math;
    int metal_need_header_relational;
    int metal_need_header_geometric;
    int metal_need_header_graphics;
    int metal_need_header_texture;
#endif

#if SUPPORT_PROFILE_SPIRV
    int branch_labels_patch_stack[32];
    SpirvContext spirv;
#endif
#if SUPPORT_PROFILE_GLSPIRV
    int profile_supports_glspirv;
#endif

#if SUPPORT_PROFILE_HLSL
    char hlsl_outpos_name[16];
#endif
} Context;

// Use these macros so we can remove all bits of these profiles from the build.
#if SUPPORT_PROFILE_ARB1_NV
#define support_nv2(ctx) ((ctx)->profile_supports_nv2)
#define support_nv3(ctx) ((ctx)->profile_supports_nv3)
#define support_nv4(ctx) ((ctx)->profile_supports_nv4)
#else
#define support_nv2(ctx) (0)
#define support_nv3(ctx) (0)
#define support_nv4(ctx) (0)
#endif

#if SUPPORT_PROFILE_GLSL120
#define support_glsl120(ctx) ((ctx)->profile_supports_glsl120)
#else
#define support_glsl120(ctx) (0)
#endif

#if SUPPORT_PROFILE_GLSLES3
#define support_glsles3(ctx) ((ctx)->profile_supports_glsles3)
#else
#define support_glsles3(ctx) (0)
#endif

#if SUPPORT_PROFILE_GLSLES
#define support_glsles(ctx) ((ctx)->profile_supports_glsles || support_glsles3(ctx))
#else
#define support_glsles(ctx) (0)
#endif

// Profile entry points...

// one emit function for each opcode in each profile.
typedef void (*emit_function)(Context *ctx);

// one emit function for starting output in each profile.
typedef void (*emit_start)(Context *ctx, const char *profilestr);

// one emit function for ending output in each profile.
typedef void (*emit_end)(Context *ctx);

// one emit function for phase opcode output in each profile.
typedef void (*emit_phase)(Context *ctx);

// one emit function for finalizing output in each profile.
typedef void (*emit_finalize)(Context *ctx);

// one emit function for global definitions in each profile.
typedef void (*emit_global)(Context *ctx, RegisterType regtype, int regnum);

// one emit function for relative uniform arrays in each profile.
typedef void (*emit_array)(Context *ctx, VariableList *var);

// one emit function for relative constants arrays in each profile.
typedef void (*emit_const_array)(Context *ctx,
                                 const struct ConstantsList *constslist,
                                 int base, int size);

// one emit function for uniforms in each profile.
typedef void (*emit_uniform)(Context *ctx, RegisterType regtype, int regnum,
                             const VariableList *var);

// one emit function for samplers in each profile.
typedef void (*emit_sampler)(Context *ctx, int stage, TextureType ttype,
                             int texbem);

// one emit function for attributes in each profile.
typedef void (*emit_attribute)(Context *ctx, RegisterType regtype, int regnum,
                               MOJOSHADER_usage usage, int index, int wmask,
                               int flags);

// one args function for each possible sequence of opcode arguments.
typedef int (*args_function)(Context *ctx);

// one state function for each opcode where we have state machine updates.
typedef void (*state_function)(Context *ctx);

// one function for varnames in each profile.
typedef const char *(*varname_function)(Context *c, RegisterType t, int num);

// one function for const var array in each profile.
typedef const char *(*const_array_varname_function)(Context *c, int base, int size);

typedef struct Profile
{
    const char *name;
    emit_start start_emitter;
    emit_end end_emitter;
    emit_phase phase_emitter;
    emit_global global_emitter;
    emit_array array_emitter;
    emit_const_array const_array_emitter;
    emit_uniform uniform_emitter;
    emit_sampler sampler_emitter;
    emit_attribute attribute_emitter;
    emit_finalize finalize_emitter;
    varname_function get_varname;
    const_array_varname_function get_const_array_varname;
} Profile;

// Common utilities...

void out_of_memory(Context *ctx);
void *Malloc(Context *ctx, const size_t len);
char *StrDup(Context *ctx, const char *str);
void Free(Context *ctx, void *ptr);
void * MOJOSHADERCALL MallocBridge(int bytes, void *data);
void MOJOSHADERCALL FreeBridge(void *ptr, void *data);

int set_output(Context *ctx, Buffer **section);
void push_output(Context *ctx, Buffer **section);
void pop_output(Context *ctx);

uint32 ver_ui32(const uint8 major, const uint8 minor);
int shader_version_supported(const uint8 maj, const uint8 min);
int shader_version_atleast(const Context *ctx, const uint8 maj,
                           const uint8 min);
int shader_version_exactly(const Context *ctx, const uint8 maj,
                           const uint8 min);
int shader_is_pixel(const Context *ctx);
int shader_is_vertex(const Context *ctx);

int isfail(const Context *ctx);
void failf(Context *ctx, const char *fmt, ...);
void fail(Context *ctx, const char *reason);

void output_line(Context *ctx, const char *fmt, ...);
void output_blank_line(Context *ctx);

void floatstr(Context *ctx, char *buf, size_t bufsize, float f,
              int leavedecimal);

RegisterList *reglist_insert(Context *ctx, RegisterList *prev,
                             const RegisterType regtype,
                             const int regnum);
RegisterList *reglist_find(const RegisterList *prev,
                           const RegisterType rtype,
                           const int regnum);
RegisterList *set_used_register(Context *ctx,
                                const RegisterType regtype,
                                const int regnum,
                                const int written);
void set_defined_register(Context *ctx, const RegisterType rtype,
                          const int regnum);

int writemask_xyzw(const int writemask);
int writemask_xyz(const int writemask);
int writemask_xy(const int writemask);
int writemask_x(const int writemask);
int writemask_y(const int writemask);
int replicate_swizzle(const int swizzle);
int no_swizzle(const int swizzle);
int vecsize_from_writemask(const int m);
void set_dstarg_writemask(DestArgInfo *dst, const int mask);

int isscalar(Context *ctx, const MOJOSHADER_shaderType shader_type,
             const RegisterType rtype, const int rnum);

static const char swizzle_channels[] = { 'x', 'y', 'z', 'w' };

const char *get_D3D_register_string(Context *ctx,
                                    RegisterType regtype,
                                    int regnum, char *regnum_str,
                                    size_t regnum_size);

// !!! FIXME: These should stay in the mojoshader_profile_d3d file
// !!! FIXME: but ARB1 relies on them, so we have to move them here.
// !!! FIXME: If/when we kill off ARB1, we can move these back.
const char *get_D3D_varname_in_buf(Context *ctx, RegisterType rt,
                                   int regnum, char *buf,
                                   const size_t len);
const char *get_D3D_varname(Context *ctx, RegisterType rt, int regnum);

#endif
