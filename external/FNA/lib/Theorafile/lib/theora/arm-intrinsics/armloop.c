#include "armint.h"

#if defined(OC_ARM_ASM_NEON)
#include <stddef.h>
#include <string.h>
#include <arm_neon.h>

static inline uint8x8x2_t loop_filter_neon(uint8x8x4_t pix, uint16_t lim2u) {
    uint16x8_t lim2 = vdupq_n_u16(lim2u);

    int16x8_t r2 = vreinterpretq_s16_u16(vsubl_u8(pix.val[2], pix.val[1]));
    int16x8_t r = vreinterpretq_s16_u16(vsubl_u8(pix.val[0], pix.val[3]));
    r = vaddq_s16(r, r2);
    r = vaddq_s16(r, vshlq_n_s16(r2, 1));
    r = vrshrq_n_s16(r, 3);

    uint16x8_t absR = vreinterpretq_u16_s16(vabsq_s16(r));
    int16x8_t sgnR = vshrq_n_s16(r, 15);

    uint16x8_t absF = vminq_u16(absR, vqsubq_u16(lim2, absR)); // |f| = MIN(|R|,MAX(2L-|R|, 0));
    int16x8_t f = veorq_s16(vaddq_s16(sgnR, vreinterpretq_s16_u16(absF)), sgnR);

    uint16x8_t p1 = vaddw_u8(vreinterpretq_u16_s16(f), pix.val[1]);
    int16x8_t p2 = vsubq_s16(vreinterpretq_s16_u16(vmovl_u8(pix.val[2])), f);

    uint8x8x2_t fr = {{vqmovun_s16(vreinterpretq_s16_u16(p1)), vqmovun_s16(p2)}};
    return fr;
}

static void loop_filter_neon_h(unsigned char *_pix,int _ystride,uint16_t lim2) {
    _pix -= 2;
    uint8x8x4_t cols = {};
    cols = vld4_lane_u8(&_pix[_ystride * 0], cols, 0);
    cols = vld4_lane_u8(&_pix[_ystride * 1], cols, 1);
    cols = vld4_lane_u8(&_pix[_ystride * 2], cols, 2);
    cols = vld4_lane_u8(&_pix[_ystride * 3], cols, 3);
    cols = vld4_lane_u8(&_pix[_ystride * 4], cols, 4);
    cols = vld4_lane_u8(&_pix[_ystride * 5], cols, 5);
    cols = vld4_lane_u8(&_pix[_ystride * 6], cols, 6);
    cols = vld4_lane_u8(&_pix[_ystride * 7], cols, 7);

    uint8x8x2_t fp = loop_filter_neon(cols, lim2);

    _pix += 1;
    vst2_lane_u8(&_pix[_ystride * 0], fp, 0);
    vst2_lane_u8(&_pix[_ystride * 1], fp, 1);
    vst2_lane_u8(&_pix[_ystride * 2], fp, 2);
    vst2_lane_u8(&_pix[_ystride * 3], fp, 3);
    vst2_lane_u8(&_pix[_ystride * 4], fp, 4);
    vst2_lane_u8(&_pix[_ystride * 5], fp, 5);
    vst2_lane_u8(&_pix[_ystride * 6], fp, 6);
    vst2_lane_u8(&_pix[_ystride * 7], fp, 7);
}

static void loop_filter_neon_v(unsigned char *_pix,int _ystride,uint16_t lim2) {
    _pix -= _ystride * 2;
    uint8x8x4_t rows = {{
            vld1_u8(&_pix[_ystride * 0]),
            vld1_u8(&_pix[_ystride * 1]),
            vld1_u8(&_pix[_ystride * 2]),
            vld1_u8(&_pix[_ystride * 3])
    }};

    uint8x8x2_t fp = loop_filter_neon(rows, lim2);

    vst1_u8(&_pix[_ystride * 1], fp.val[0]);
    vst1_u8(&_pix[_ystride * 2], fp.val[1]);
}

/*Direct copy of the generic functions,
  other than _bv being used to store 2*flimit */

void oc_loop_filter_init_neon(signed char _bv[256],int _flimit) {
    uint16_t lim2u = _flimit * 2;
    memcpy(_bv, &lim2u, sizeof(uint16_t));
}

void oc_state_loop_filter_frag_rows_neon(const oc_theora_state *_state,
                                         signed char *_bv,int _refi,int _pli,int _fragy0,int _fragy_end) {
    const oc_fragment_plane *fplane;
    const oc_fragment *frags;
    const ptrdiff_t *frag_buf_offs;
    unsigned char *ref_frame_data;
    ptrdiff_t fragi_top;
    ptrdiff_t fragi_bot;
    ptrdiff_t fragi0;
    ptrdiff_t fragi0_end;
    int ystride;
    int nhfrags;
    uint16_t lim2;
    fplane = _state->fplanes + _pli;
    nhfrags = fplane->nhfrags;
    fragi_top = fplane->froffset;
    fragi_bot = fragi_top + fplane->nfrags;
    fragi0 = fragi_top + _fragy0 * (ptrdiff_t) nhfrags;
    fragi0_end = fragi_top + _fragy_end * (ptrdiff_t) nhfrags;
    ystride = _state->ref_ystride[_pli];
    frags = _state->frags;
    frag_buf_offs = _state->frag_buf_offs;
    ref_frame_data = _state->ref_frame_data[_refi];
    memcpy(&lim2, _bv, sizeof(uint16_t));
    /*The following loops are constructed somewhat non-intuitively on purpose.
      The main idea is: if a block boundary has at least one coded fragment on
       it, the filter is applied to it.
      However, the order that the filters are applied in matters, and VP3 chose
       the somewhat strange ordering used below.*/
    while (fragi0 < fragi0_end) {
        ptrdiff_t fragi;
        ptrdiff_t fragi_end;
        fragi = fragi0;
        fragi_end = fragi + nhfrags;
        while (fragi < fragi_end) {
            if (frags[fragi].coded) {
                unsigned char *ref;
                ref = ref_frame_data + frag_buf_offs[fragi];
                if (fragi > fragi0)loop_filter_neon_h(ref, ystride, lim2);
                if (fragi0 > fragi_top)loop_filter_neon_v(ref, ystride, lim2);
                if (fragi + 1 < fragi_end && !frags[fragi + 1].coded) {
                    loop_filter_neon_h(ref + 8, ystride, lim2);
                }
                if (fragi + nhfrags < fragi_bot && !frags[fragi + nhfrags].coded) {
                    loop_filter_neon_v(ref + (ystride << 3), ystride, lim2);
                }
            }
            fragi++;
        }
        fragi0 += nhfrags;
    }
}
#endif
