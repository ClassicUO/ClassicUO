#ifndef _arm_intrinsics_armfrag_H
#define _arm_intrinsics_armfrag_H 1
#include <stdint.h>
#include <stddef.h>
#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#ifdef OC_ARM_ASM_NEON
typedef struct oc_theora_state oc_theora_state;
void oc_idct8x8_neon(int16_t _y[64],int16_t _x[64],int _last_zzi);
void oc_loop_filter_init_neon(signed char _bv[256],int _flimit);
void oc_state_loop_filter_frag_rows_neon(const oc_theora_state *_state,
                                         signed char *_bv,int _refi,int _pli,int _fragy0,int _fragy_end);
void oc_frag_copy_neon(unsigned char *_dst,const unsigned char *_src,int _ystride);
void oc_frag_copy_list_neon(unsigned char *_dst_frame, const unsigned char *_src_frame,int _ystride,
                            const ptrdiff_t *_fragis,ptrdiff_t _nfragis,const ptrdiff_t *_frag_buf_offs);
void oc_frag_recon_intra_neon(unsigned char *_dst,
                              int _ystride, const int16_t _residue[64]);
void oc_frag_recon_inter_neon(unsigned char *_dst, const unsigned char *_src,
                              int _ystride,const int16_t _residue[64]);
void oc_frag_recon_inter2_neon(unsigned char *_dst,const unsigned char *_src1, const unsigned char *_src2,
                               int _ystride,const int16_t _residue[64]);
void oc_state_frag_recon_neon(const oc_theora_state *_state,ptrdiff_t _fragi,
                              int _pli,int16_t _dct_coeffs[128],int _last_zzi,uint16_t _dc_quant);
#endif

#endif //_arm_intrinsics_armfrag_H
