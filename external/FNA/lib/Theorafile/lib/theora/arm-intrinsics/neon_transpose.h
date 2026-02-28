#ifndef _arm_neon_transpose_H
#define _arm_neon_transpose_H
#include "neon_a64_compat.h"

static void v_transpose8x8(const int16x8_t a[8], int16x8_t b[8]) {
    int16x8x2_t s0 = vtrnq_s16(a[0], a[1]);
    int16x8x2_t s1 = vtrnq_s16(a[2], a[3]);
    int16x8x2_t s2 = vtrnq_s16(a[4], a[5]);
    int16x8x2_t s3 = vtrnq_s16(a[6], a[7]);
    /*  1  9  3 11  5 13  7 15  s0[0] = VTRN1.1N p0 p1 */
    /*  2 10  4 12  6 14  8 16  s0[1] = VTRN2.1N p0 p1 */
    /* 17 25 19 27 21 29 23 31  s1[0] = VTRN1.1N p2 p3 */
    /* 18 26 20 28 22 30 24 32  s1[1] = VTRN2.1N p2 p3 */
    /* 33 41 35 43 37 45 39 47  s2[0] = VTRN1.1N p4 p5 */
    /* 34 42 36 44 38 46 40 48  s2[1] = VTRN2.1N p4 p5 */
    /* 49 57 51 59 53 61 55 63  s3[0] = VTRN1.1N p6 p7 */
    /* 50 58 52 60 54 62 56 64  s3[1] = VTRN2.1N p6 p7 */

    int32x4x2_t t0 = vtrnq_s32(vreinterpretq_s32_s16(s0.val[0]), vreinterpretq_s32_s16(s1.val[0]));
    int32x4x2_t t1 = vtrnq_s32(vreinterpretq_s32_s16(s0.val[1]), vreinterpretq_s32_s16(s1.val[1]));
    int32x4x2_t t2 = vtrnq_s32(vreinterpretq_s32_s16(s2.val[0]), vreinterpretq_s32_s16(s3.val[0]));
    int32x4x2_t t3 = vtrnq_s32(vreinterpretq_s32_s16(s2.val[1]), vreinterpretq_s32_s16(s3.val[1]));
    /*  1  9 17 25  5 13 21 29  t0[0] = VTRN1.2N s0[0] s1[0] */
    /*  2 10 18 26  6 14 22 30  t1[0] = VTRN1.2N s0[1] s1[1] */
    /*  3 11 19 27  7 15 23 31  t0[1] = VTRN2.2N s0[0] s1[0] */
    /*  4 12 20 28  8 16 24 32  t1[1] = VTRN2.2N s0[1] s1[1] */
    /* 33 41 49 57 37 45 53 61  t2[0] = VTRN1.2N s2[0] s3[0] */
    /* 34 42 50 58 38 46 54 62  t3[0] = VTRN1.2N s2[1] s3[1] */
    /* 35 43 51 59 39 47 55 63  t2[1] = VTRN2.2N s2[0] s3[0] */
    /* 36 44 52 60 40 48 56 64  t3[1] = VTRN2.2N s2[1] s3[1] */

    int64x2_t u0 = vuzp1q_s64(vreinterpretq_s64_s32(t0.val[0]), vreinterpretq_s64_s32(t2.val[0]));
    int64x2_t u1 = vuzp1q_s64(vreinterpretq_s64_s32(t1.val[0]), vreinterpretq_s64_s32(t3.val[0]));
    int64x2_t u2 = vuzp1q_s64(vreinterpretq_s64_s32(t0.val[1]), vreinterpretq_s64_s32(t2.val[1]));
    int64x2_t u3 = vuzp1q_s64(vreinterpretq_s64_s32(t1.val[1]), vreinterpretq_s64_s32(t3.val[1]));
    int64x2_t u4 = vuzp2q_s64(vreinterpretq_s64_s32(t0.val[0]), vreinterpretq_s64_s32(t2.val[0]));
    int64x2_t u5 = vuzp2q_s64(vreinterpretq_s64_s32(t1.val[0]), vreinterpretq_s64_s32(t3.val[0]));
    int64x2_t u6 = vuzp2q_s64(vreinterpretq_s64_s32(t0.val[1]), vreinterpretq_s64_s32(t2.val[1]));
    int64x2_t u7 = vuzp2q_s64(vreinterpretq_s64_s32(t1.val[1]), vreinterpretq_s64_s32(t3.val[1]));
    /*  1  9 17 25 33 41 49 57  u0 = VUZP1 t0[0] t2[0] */
    /*  2 10 18 26 34 42 50 58  u1 = VUZP1 t1[0] t3[0] */
    /*  3 11 19 27 35 43 51 59  u2 = VUZP1 t0[1] t2[1] */
    /*  4 12 20 28 36 44 52 60  u3 = VUZP1 t1[1] t3[1] */
    /*  5 13 21 29 37 45 53 61  u4 = VUZP2 t0[0] t2[0] */
    /*  6 14 22 30 38 46 54 62  u5 = VUZP2 t1[0] t3[0] */
    /*  7 15 23 31 39 47 55 63  u6 = VUZP2 t0[1] t2[1] */
    /*  8 16 24 32 40 48 56 64  u7 = VUZP2 t1[1] t3[1] */

    b[0] = vreinterpretq_s16_s64(u0);
    b[1] = vreinterpretq_s16_s64(u1);
    b[2] = vreinterpretq_s16_s64(u2);
    b[3] = vreinterpretq_s16_s64(u3);
    b[4] = vreinterpretq_s16_s64(u4);
    b[5] = vreinterpretq_s16_s64(u5);
    b[6] = vreinterpretq_s16_s64(u6);
    b[7] = vreinterpretq_s16_s64(u7);
}

#endif
