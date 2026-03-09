#include "armfrag.h"

#if defined(OC_ARM_ASM_NEON)
#include <arm_neon.h>
#include "../state.h"

void oc_frag_copy_neon(unsigned char *_dst,const unsigned char *_src,int _ystride) {
  for (int i = 0; i < 8; i++)
    vst1_u8(&_dst[i * _ystride], vld1_u8(&_src[i * _ystride]));
}

void oc_frag_copy_list_neon(unsigned char *_dst_frame, const unsigned char *_src_frame,int _ystride,
                            const ptrdiff_t *_fragis,ptrdiff_t _nfragis,const ptrdiff_t *_frag_buf_offs) {
  ptrdiff_t fragii;
  for (fragii = 0; fragii < _nfragis; fragii++) {
    ptrdiff_t frag_buf_off;
    frag_buf_off = _frag_buf_offs[_fragis[fragii]];
    oc_frag_copy_neon(_dst_frame + frag_buf_off,
                      _src_frame + frag_buf_off, _ystride);
  }
}

void oc_frag_recon_intra_neon(unsigned char *_dst,
                              int _ystride, const int16_t _residue[64]) {
  for (int i = 0; i < 8; i++)
    vst1_u8(&_dst[i * _ystride],
            vqmovun_s16(vaddq_s16(vld1q_s16(&_residue[i * 8]),
                                  vdupq_n_s16(128))));
}

void oc_frag_recon_inter_neon(unsigned char *_dst, const unsigned char *_src,
                              int _ystride,const int16_t _residue[64]) {
  for (int i = 0; i < 8; i++)
    vst1_u8(&_dst[i * _ystride],
            vqmovun_s16(vaddq_s16(vld1q_s16(&_residue[i * 8]),
                                  vreinterpretq_s16_u16(vmovl_u8(vld1_u8(&_src[i * _ystride]))))));
}

void oc_frag_recon_inter2_neon(unsigned char *_dst,const unsigned char *_src1, const unsigned char *_src2,
                               int _ystride,const int16_t _residue[64]) {
  for (int i = 0; i < 8; i++)
    vst1_u8(&_dst[i * _ystride],
            vqmovun_s16(vaddq_s16(vld1q_s16(&_residue[i * 8]),
                                  vreinterpretq_s16_u16(vmovl_u8(vhadd_u8(vld1_u8(&_src1[i * _ystride]),
                                                                          vld1_u8(&_src2[i * _ystride])))))));
}

void oc_state_frag_recon_neon(const oc_theora_state *_state,ptrdiff_t _fragi,
 int _pli,int16_t _dct_coeffs[128],int _last_zzi,uint16_t _dc_quant){
  unsigned char *dst;
  ptrdiff_t      frag_buf_off;
  int            ystride;
  int            refi;
  /*Apply the inverse transform.*/
  /*Special case only having a DC component.*/
  if(_last_zzi<2){
    ogg_int16_t p;
    int         ci;
    /*We round this dequant product (and not any of the others) because there's
       no iDCT rounding.*/
    p=(ogg_int16_t)(_dct_coeffs[0]*(ogg_int32_t)_dc_quant+15>>5);
    /*LOOP VECTORIZES.*/
    /*Apparently GCC doesn't want to vectorize it. This is the only line changed.*/
//    for(ci=0;ci<64;ci++)_dct_coeffs[64+ci]=p;
    for(ci=8;ci<16;ci++) vst1q_s16(&_dct_coeffs[ci*8], vdupq_n_s16(p));
  }
  else{
    /*First, dequantize the DC coefficient.*/
    _dct_coeffs[0]=(ogg_int16_t)(_dct_coeffs[0]*(int)_dc_quant);
    oc_idct8x8_neon(_dct_coeffs+64,_dct_coeffs,_last_zzi);
  }
  /*Fill in the target buffer.*/
  frag_buf_off=_state->frag_buf_offs[_fragi];
  refi=_state->frags[_fragi].refi;
  ystride=_state->ref_ystride[_pli];
  dst=_state->ref_frame_data[OC_FRAME_SELF]+frag_buf_off;
  if(refi==OC_FRAME_SELF)oc_frag_recon_intra_neon(dst,ystride,_dct_coeffs+64);
  else{
    const unsigned char *ref;
    int                  mvoffsets[2];
    ref=_state->ref_frame_data[refi]+frag_buf_off;
    if(oc_state_get_mv_offsets(_state,mvoffsets,_pli,
     _state->frag_mvs[_fragi])>1){
      oc_frag_recon_inter2_neon(
       dst,ref+mvoffsets[0],ref+mvoffsets[1],ystride,_dct_coeffs+64);
    }
    else{
      oc_frag_recon_inter_neon(dst,ref+mvoffsets[0],ystride,_dct_coeffs+64);
    }
  }
}

#endif
