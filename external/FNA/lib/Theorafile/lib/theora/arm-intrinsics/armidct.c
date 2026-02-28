#include "armfrag.h"

#if defined(OC_ARM_ASM_NEON)
typedef int32_t ogg_int32_t;
#include "../dct.h"
#include "neon_a64_compat.h"
#include "neon_transpose.h"

static inline int16x8_t scale_nu_s16(int16x8_t a, uint16_t b) {
    if (!(b & 1))
        return vqdmulhq_n_s16(a, b >> 1);
    if (b < 32768)
        return vshrq_n_s16(vqdmulhq_n_s16(a, b), 1);

    int32x4_t al = vmovl_s16(vget_low_s16(a));
    int32x4_t ah = vmovl_high_s16(a);
    al = vmulq_n_s32(al, b);
    ah = vmulq_n_s32(ah, b);
    return vuzp2q_s16(vreinterpretq_s16_s32(al), vreinterpretq_s16_s32(ah));
}

static void v_transpose4x8(const int16x4_t a[8], int16x8_t b[4]) {
    int16x8x2_t s0 = vtrnq_s16(vcombine_s16(a[0], a[4]), vcombine_s16(a[1], a[5]));
    int16x8x2_t s1 = vtrnq_s16(vcombine_s16(a[2], a[6]), vcombine_s16(a[3], a[7]));
    /*  1  5  3  7 17 21 19 23  s0[0] = VTRN1.1N p0 p1 */
    /*  2  6  4  8 18 22 20 24  s0[1] = VTRN2.1N p0 p1 */
    /*  9 13 11 15 25 29 27 31  s1[0] = VTRN1.1N p2 p3 */
    /* 10 14 12 16 26 30 28 32  s1[1] = VTRN2.1N p2 p3 */

    int32x4x2_t t0 = vtrnq_s32(vreinterpretq_s32_s16(s0.val[0]), vreinterpretq_s32_s16(s1.val[0]));
    int32x4x2_t t1 = vtrnq_s32(vreinterpretq_s32_s16(s0.val[1]), vreinterpretq_s32_s16(s1.val[1]));
    /*  1  5  9 13 17 21 25 29  t0[0] = VTRN1.2N s0[0] s1[0] */
    /*  2  6 10 14 18 22 26 30  t1[0] = VTRN1.2N s0[1] s1[1] */
    /*  3  7 11 15 19 23 27 31  t0[1] = VTRN2.2N s0[0] s1[0] */
    /*  4  8 12 16 20 24 28 32  t1[1] = VTRN2.2N s0[1] s1[1] */

    b[0] = vreinterpretq_s16_s32(t0.val[0]);
    b[1] = vreinterpretq_s16_s32(t1.val[0]);
    b[2] = vreinterpretq_s16_s32(t0.val[1]);
    b[3] = vreinterpretq_s16_s32(t1.val[1]);
}

static void idct8x8(const int16x8_t x[8], int16x8_t y[8]) {
    int16x8_t t[8], r;

    /*Stage 1:*/
    /*0-1 butterfly.*/
    t[0] = scale_nu_s16(vaddq_s16(x[0], x[4]), OC_C4S4);
    t[1] = scale_nu_s16(vsubq_s16(x[0], x[4]), OC_C4S4);
    /*2-3 rotation by 6pi/16.*/
    t[2] = vsubq_s16(scale_nu_s16(x[2], OC_C6S2), scale_nu_s16(x[6], OC_C2S6));
    t[3] = vaddq_s16(scale_nu_s16(x[2], OC_C2S6), scale_nu_s16(x[6], OC_C6S2));
    /*4-7 rotation by 7pi/16.*/
    t[4] = vsubq_s16(scale_nu_s16(x[1], OC_C7S1), scale_nu_s16(x[7], OC_C1S7));
    t[7] = vaddq_s16(scale_nu_s16(x[1], OC_C1S7), scale_nu_s16(x[7], OC_C7S1));
    /*5-6 rotation by 3pi/16.*/
    t[5] = vsubq_s16(scale_nu_s16(x[5], OC_C3S5), scale_nu_s16(x[3], OC_C5S3));
    t[6] = vaddq_s16(scale_nu_s16(x[5], OC_C5S3), scale_nu_s16(x[3], OC_C3S5));

    /*Stage 2:*/
    /*4-5 butterfly.*/
    r = vaddq_s16(t[4], t[5]);
    t[5] = scale_nu_s16(vsubq_s16(t[4], t[5]), OC_C4S4);
    t[4] = r;
    /*7-6 butterfly.*/
    r = vaddq_s16(t[7], t[6]);
    t[6] = scale_nu_s16(vsubq_s16(t[7], t[6]), OC_C4S4);
    t[7] = r;

    /*Stage 3:*/
    /*0-3 butterfly.*/
    r = vaddq_s16(t[0], t[3]);
    t[3] = vsubq_s16(t[0], t[3]);
    t[0] = r;
    /*1-2 butterfly.*/
    r = vaddq_s16(t[1], t[2]);
    t[2] = vsubq_s16(t[1], t[2]);
    t[1] = r;
    /*6-5 butterfly.*/
    r = vaddq_s16(t[6], t[5]);
    t[5] = vsubq_s16(t[6], t[5]);
    t[6] = r;

    /*Stage 4:*/
    /*0-7 butterfly.*/
    y[0] = vaddq_s16(t[0], t[7]);
    y[7] = vsubq_s16(t[0], t[7]);
    /*1-6 butterfly.*/
    y[1] = vaddq_s16(t[1], t[6]);
    y[6] = vsubq_s16(t[1], t[6]);
    /*2-5 butterfly.*/
    y[2] = vaddq_s16(t[2], t[5]);
    y[5] = vsubq_s16(t[2], t[5]);
    /*3-4 butterfly.*/
    y[3] = vaddq_s16(t[3], t[4]);
    y[4] = vsubq_s16(t[3], t[4]);
}

// cos(n*pi/16) (resp. sin(m*pi/16)) scaled by 2147483648
// GCC doesn't have vld1_s32_x4
//static const int32_t I32_COSINE[8] = {
//        2106220352,  418953276, // C1 C7
//        1984016189,  821806413, // C2 C6
//        1785567396, 1193077991, // C3 C5
//        1518500250, 1518500250, // C4 C4
//};
static const int32_t I32_COSINE[8] = {
        OC_C1S7 << 15, OC_C2S6 << 15, OC_C3S5 << 15, OC_C4S4 << 15,
        OC_C7S1 << 15, OC_C6S2 << 15, OC_C5S3 << 15, OC_C4S4 << 15,
};

static void idct4x4_4x8(const int16x4_t x[4], int16x4_t y[8]) {
    int32x4_t s[4];
    int16x4_t t[8], r;
    int32x2x4_t c = vld4_s32(I32_COSINE);

    /*Stage 1:*/
    t[0] = vmovn_s32(vqdmulhq_lane_s32(vmovl_s16(x[0]), c.val[3], 0)); // C4S4
    t[1] = vmovn_s32(vqdmulhq_lane_s32(vmovl_s16(x[0]), c.val[3], 0)); // C4S4
    t[2] = vmovn_s32(vqdmulhq_lane_s32(vmovl_s16(x[2]), c.val[1], 1)); // C6S2
    t[3] = vmovn_s32(vqdmulhq_lane_s32(vmovl_s16(x[2]), c.val[1], 0)); // C2S6
    s[0] = vqdmulhq_lane_s32(vmovl_s16(x[1]), c.val[0], 1); // C7S1
    s[3] = vqdmulhq_lane_s32(vmovl_s16(x[1]), c.val[0], 0); // C1S7
    s[1] = vqdmulhq_lane_s32(vmovl_s16(x[3]), c.val[2], 1); // C5S3
    s[2] = vqdmulhq_lane_s32(vmovl_s16(x[3]), c.val[2], 0); // C3S5
    s[1] = vnegq_s32(s[1]);

    /*Stage 2:*/
    t[5] = vmovn_s32(vqdmulhq_lane_s32(vsubq_s32(s[0], s[1]), c.val[3], 0)); // C4S4
    t[4] = vmovn_s32(vaddq_s32(s[0], s[1]));
    t[6] = vmovn_s32(vqdmulhq_lane_s32(vsubq_s32(s[3], s[2]), c.val[3], 0)); // C4S4
    t[7] = vmovn_s32(vaddq_s32(s[3], s[2]));

    /*Stage 3:*/
    r = vadd_s16(t[0], t[3]);
    t[3] = vsub_s16(t[0], t[3]);
    t[0] = r;
    r = vadd_s16(t[1], t[2]);
    t[2] = vsub_s16(t[1], t[2]);
    t[1] = r;
    r = vadd_s16(t[6], t[5]);
    t[5] = vsub_s16(t[6], t[5]);
    t[6] = r;

    /*Stage 4:*/
    y[0] = vadd_s16(t[0], t[7]);
    y[7] = vsub_s16(t[0], t[7]);
    y[1] = vadd_s16(t[1], t[6]);
    y[6] = vsub_s16(t[1], t[6]);
    y[2] = vadd_s16(t[2], t[5]);
    y[5] = vsub_s16(t[2], t[5]);
    y[3] = vadd_s16(t[3], t[4]);
    y[4] = vsub_s16(t[3], t[4]);
}

static void idct8x4_8x8(const int16x8_t x[4], int16x8_t y[8]) {
    int16x8_t t[8], r;

    /*Stage 1:*/
    t[0] = scale_nu_s16(x[0], OC_C4S4);
    t[1] = scale_nu_s16(x[0], OC_C4S4);
    t[2] = scale_nu_s16(x[2], OC_C6S2);
    t[3] = scale_nu_s16(x[2], OC_C2S6);
    t[4] = scale_nu_s16(x[1], OC_C7S1);
    t[7] = scale_nu_s16(x[1], OC_C1S7);
    t[5] = vnegq_s16(scale_nu_s16(x[3], OC_C5S3));
    t[6] = scale_nu_s16(x[3], OC_C3S5);

    /*Stage 2:*/
    r = vaddq_s16(t[4], t[5]);
    t[5] = scale_nu_s16(vsubq_s16(t[4], t[5]), OC_C4S4);
    t[4] = r;
    r = vaddq_s16(t[7], t[6]);
    t[6] = scale_nu_s16(vsubq_s16(t[7], t[6]), OC_C4S4);
    t[7] = r;

    /*Stage 3:*/
    r = vaddq_s16(t[0], t[3]);
    t[3] = vsubq_s16(t[0], t[3]);
    t[0] = r;
    r = vaddq_s16(t[1], t[2]);
    t[2] = vsubq_s16(t[1], t[2]);
    t[1] = r;
    r = vaddq_s16(t[6], t[5]);
    t[5] = vsubq_s16(t[6], t[5]);
    t[6] = r;

    /*Stage 4:*/
    y[0] = vaddq_s16(t[0], t[7]);
    y[7] = vsubq_s16(t[0], t[7]);
    y[1] = vaddq_s16(t[1], t[6]);
    y[6] = vsubq_s16(t[1], t[6]);
    y[2] = vaddq_s16(t[2], t[5]);
    y[5] = vsubq_s16(t[2], t[5]);
    y[3] = vaddq_s16(t[3], t[4]);
    y[4] = vsubq_s16(t[3], t[4]);
}

static void scale_final(const int16x8_t y[8], int16_t _y[64]) {
    for (int i = 0; i < 8; i++)
        vst1q_s16(&_y[i * 8], vrshrq_n_s16(y[i], 4));
}

static void oc_idct8x8_4(int16_t _y[64], int16_t _x[64]) {
    int16x4_t x0[4], y0[8];
    int16x8_t x1[4], y1[8];
    for (int i = 0; i < 4; i++)
        x0[i] = vld1_s16(&_x[i * 8]);
    idct4x4_4x8(x0, y0);
    v_transpose4x8(y0, x1);
    idct8x4_8x8(x1, y1);
    scale_final(y1, _y);
    for (int i = 0; i < 4; i++)
        vst1_s16(&_x[i * 8], vcreate_s16(0));
}

static void oc_idct8x8_8(int16_t _y[64], int16_t _x[64]) {
    int16x8_t x[8], y[8];
    for (int i = 0; i < 8; i++)
        x[i] = vld1q_s16(&_x[i * 8]);
    idct8x8(x, y);
    v_transpose8x8(y, x);
    idct8x8(x, y);
    scale_final(y, _y);
    for (int i = 0; i < 8; i++)
        vst1q_s16(&_x[i * 8], veorq_s16(x[0], x[0]));
}

void oc_idct8x8_neon(int16_t _y[64],int16_t _x[64],int _last_zzi) {
    if (_last_zzi <= 10)
        oc_idct8x8_4(_y, _x);
    else
        oc_idct8x8_8(_y, _x);
}
#endif
