#ifndef _neon_a64_compat_H
#define _neon_a64_compat_H
#include <arm_neon.h>

#if !defined(__aarch64__) && !defined(_M_ARM64)
# define vuzp1q_s64(a, b)  vcombine_s64(vget_low_s64(a), vget_low_s64(b))
# define vuzp2q_s64(a, b)  vcombine_s64(vget_high_s64(a), vget_high_s64(b))
# define vmovl_high_s16(a) vmovl_s16(vget_high_s16(a))
# define vuzp2q_s16(a, b)  vuzpq_s16(a, b).val[1]

#define vaddv_s32(p) vget_lane_s32(vpadd_s32(p, p), 0)

static inline int32_t vaddvq_s32(int32x4_t p) {
    int32x2_t r = vadd_s32(vget_high_s32(p), vget_low_s32(p));
    return vaddv_s32(r);
}

static inline int32_t vaddlvq_s16(int16x8_t p) {
    return vaddvq_s32(vpaddlq_s16(p));
}

#define vaddv_u32(p) vget_lane_u32(vpadd_u32(p, p), 0)

static inline uint32_t vaddvq_u32(uint32x4_t p) {
    uint32x2_t r = vadd_u32(vget_high_u32(p), vget_low_u32(p));
    return vaddv_u32(r);
}

static inline uint32_t vaddlvq_u16(uint16x8_t p) {
    return vaddvq_u32(vpaddlq_u16(p));
}
#endif

#endif //_neon_a64_compat_H
