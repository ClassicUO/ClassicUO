/********************************************************************
 *                                                                  *
 * THIS FILE IS PART OF THE OggTheora SOFTWARE CODEC SOURCE CODE.   *
 * USE, DISTRIBUTION AND REPRODUCTION OF THIS LIBRARY SOURCE IS     *
 * GOVERNED BY A BSD-STYLE SOURCE LICENSE INCLUDED WITH THIS SOURCE *
 * IN 'COPYING'. PLEASE READ THESE TERMS BEFORE DISTRIBUTING.       *
 *                                                                  *
 * THE Theora SOURCE CODE IS COPYRIGHT (C) 2002-2010                *
 * by the Xiph.Org Foundation and contributors http://www.xiph.org/ *
 *                                                                  *
 ********************************************************************

 CPU capability detection for ARM processors.

 function:
  last mod: $Id: cpu.c 17344 2010-07-21 01:42:18Z tterribe $

 ********************************************************************/

#include "armcpu.h"

#if !defined(OC_ARM_ASM) || !defined(OC_ARM_ASM_NEON)
ogg_uint32_t oc_cpu_flags_get(void){
  return 0;
}

#elif defined(__aarch64__) || defined(_M_ARM64)
ogg_uint32_t oc_cpu_flags_get(void) {
  return OC_CPU_ARM_NEON;
}

#elif defined(_MSC_VER)
/*For GetExceptionCode() and EXCEPTION_ILLEGAL_INSTRUCTION.*/
# define WIN32_LEAN_AND_MEAN
# define WIN32_EXTRA_LEAN
# include <windows.h>

ogg_uint32_t oc_cpu_flags_get(void){
  ogg_uint32_t flags;
  flags=0;
  /*MSVC has no inline __asm support for ARM, but it does let you __emit
     instructions via their assembled hex code.
    All of these instructions should be essentially nops.*/
# if defined(OC_ARM_ASM_NEON)
  __try{
#  if defined(__aarch64__) || defined(_M_ARM64)
    /*MOV v0.16B,v0.16B*/
    __emit(0x4EA01C00);
#  else
    /*VORR q0,q0,q0*/
    __emit(0xF2200150);
#  endif
    flags|=OC_CPU_ARM_NEON;
  }
  __except(GetExceptionCode()==EXCEPTION_ILLEGAL_INSTRUCTION){
    /*Ignore exception.*/
  }
# endif
  return flags;
}

#elif defined(__linux__)
# include <stdio.h>
# include <stdlib.h>
# include <string.h>

ogg_uint32_t oc_cpu_flags_get(void){
  ogg_uint32_t  flags;
  FILE         *fin;
  flags=0;
  /*Reading /proc/self/auxv would be easier, but that doesn't work reliably on
     Android.
    This also means that detection will fail in Scratchbox.*/
  fin=fopen("/proc/cpuinfo","r");
  if(fin!=NULL){
    /*512 should be enough for anybody (it's even enough for all the flags that
       x86 has accumulated... so far).*/
    char buf[512];
    while(fgets(buf,511,fin)!=NULL){
      if(memcmp(buf,"Features",8)==0){
        char *p;
        p=strstr(buf," neon");
        if(p!=NULL&&(p[5]==' '||p[5]=='\n'))flags|=OC_CPU_ARM_NEON;
        p=strstr(buf," asimd");
        if(p!=NULL&&(p[6]==' '||p[6]=='\n'))flags|=OC_CPU_ARM_NEON;
      }
    }
    fclose(fin);
  }
  return flags;
}

#elif defined(__riscos__)
#include <kernel.h>
#include <swis.h>

ogg_uint32_t oc_cpu_flags_get(void) {
  ogg_uint32_t flags = 0;

#if defined(OC_ARM_ASM_NEON)
  ogg_uint32_t mvfr1;
  test = _swix(VFPSupport_Features, _IN(0)|_OUT(2), 0, &mvfr1);
  if (test==NULL && (mvfr1 & 0xFFF00)==0x11100)flags|=OC_CPU_ARM_NEON;
#endif

  return flags;
}

#else
/*The feature registers which can tell us what the processor supports are
   accessible in priveleged modes only, so we can't have a general user-space
   detection method like on x86.*/
# error "Configured to use ARM asm but no CPU detection method available for " \
 "your platform.  Reconfigure with --disable-asm (or send patches)."
#endif
