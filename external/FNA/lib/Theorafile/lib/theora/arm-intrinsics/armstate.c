#include "armint.h"

static const unsigned char OC_FZIG_ZAG_NEON[128]={
   0, 8, 1, 2, 9,16,24,17,
  10, 3, 4,11,18,25,32,40,
  33,26,19,12, 5, 6,13,20,
  27,34,41,48,56,49,42,35,
  28,21,14, 7,15,22,29,36,
  43,50,57,58,51,44,37,30,
  23,31,38,45,52,59,60,53,
  46,39,47,54,61,62,55,63,
  64,64,64,64,64,64,64,64,
  64,64,64,64,64,64,64,64,
  64,64,64,64,64,64,64,64,
  64,64,64,64,64,64,64,64,
  64,64,64,64,64,64,64,64,
  64,64,64,64,64,64,64,64,
  64,64,64,64,64,64,64,64,
  64,64,64,64,64,64,64,64
};

void oc_state_accel_init_arm(oc_theora_state *_state){
  oc_state_accel_init_c(_state);
  _state->cpu_flags=oc_cpu_flags_get();
#if defined(OC_ARM_ASM_NEON)
  if(_state->cpu_flags & OC_CPU_ARM_NEON){
# if defined(OC_STATE_USE_VTABLE)
    _state->opt_vtable.loop_filter_init=oc_loop_filter_init_neon;
    _state->opt_vtable.state_loop_filter_frag_rows=oc_state_loop_filter_frag_rows_neon;
    _state->opt_vtable.frag_copy=oc_frag_copy_neon;
    _state->opt_vtable.frag_copy_list=oc_frag_copy_list_neon;

    _state->opt_vtable.state_frag_recon=oc_state_frag_recon_neon;
    _state->opt_vtable.idct8x8=oc_idct8x8_neon;
    _state->opt_vtable.frag_recon_intra=oc_frag_recon_intra_neon;
    _state->opt_vtable.frag_recon_inter=oc_frag_recon_inter_neon;
    _state->opt_vtable.frag_recon_inter2=oc_frag_recon_inter2_neon;
# endif
    _state->opt_data.dct_fzig_zag=OC_FZIG_ZAG_NEON;
  }
#endif
}
