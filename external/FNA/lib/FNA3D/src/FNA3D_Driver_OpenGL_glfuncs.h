/* FNA3D - 3D Graphics Library for FNA
 *
 * Copyright (c) 2020-2024 Ethan Lee
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */

/* Extensions used by FNA3D */
GL_EXT(BaseGL)
GL_EXT(CoreGL)
GL_EXT(3DTexture)
GL_EXT(DoublePrecisionDepth)
GL_EXT(OES_single_precision)
GL_EXT(ARB_occlusion_query)
GL_EXT(NonES3)
GL_EXT(NonES3NonCore)
GL_EXT(ARB_framebuffer_object)
GL_EXT(EXT_framebuffer_blit)
GL_EXT(EXT_framebuffer_multisample)
GL_EXT(ARB_internalformat_query)
GL_EXT(ARB_invalidate_subdata)
GL_EXT(ARB_draw_instanced)
GL_EXT(ARB_instanced_arrays)
GL_EXT(ARB_draw_elements_base_vertex)
GL_EXT(EXT_draw_buffers2)
GL_EXT(ARB_texture_multisample)
GL_EXT(ARB_map_buffer_range)
GL_EXT(KHR_debug)
GL_EXT(GREMEDY_string_marker)

/* Basic entry points. If you don't have these, you're screwed. */
GL_PROC(BaseGL, void, glActiveTexture, (GLenum a))
GL_PROC(BaseGL, void, glBindBuffer, (GLenum a, GLuint b))
GL_PROC(BaseGL, void, glBindTexture, (GLenum a, GLuint b))
GL_PROC(BaseGL, void, glBlendColor, (GLfloat a, GLfloat b, GLfloat c, GLfloat d))
GL_PROC(BaseGL, void, glBlendEquationSeparate, (GLenum a, GLenum b))
GL_PROC(BaseGL, void, glBlendFuncSeparate, (GLenum a, GLenum b, GLenum c, GLenum d))
GL_PROC(BaseGL, void, glBufferData, (GLenum a, GLsizeiptr b, const GLvoid *c, GLenum d))
GL_PROC(BaseGL, void, glBufferSubData, (GLenum a, GLintptr b, GLsizeiptr c, const GLvoid *d))
GL_PROC(BaseGL, GLboolean, glUnmapBuffer, (GLenum a))
GL_PROC(BaseGL, void, glClear, (GLbitfield a))
GL_PROC(BaseGL, void, glClearColor, (GLclampf a, GLclampf b, GLclampf c, GLclampf d))
GL_PROC(BaseGL, void, glClearStencil, (GLint s))
GL_PROC(BaseGL, void, glColorMask, (GLboolean a, GLboolean b, GLboolean c, GLboolean d))
GL_PROC(BaseGL, void, glDeleteBuffers, (GLsizei a, const GLuint *b))
GL_PROC(BaseGL, void, glDeleteTextures, (GLsizei a, const GLuint *b))
GL_PROC(BaseGL, void, glDepthFunc, (GLenum a))
GL_PROC(BaseGL, void, glDepthMask, (GLboolean a))
GL_PROC(BaseGL, void, glDisable, (GLenum a))
GL_PROC(BaseGL, void, glDisableVertexAttribArray, (GLint a))
GL_PROC(BaseGL, void, glDrawArrays, (GLenum a, GLint b, GLsizei c))
GL_PROC(BaseGL, void, glDrawBuffers, (GLsizei a, const GLenum *b))
GL_PROC(BaseGL, void, glDrawRangeElements, (GLenum a, GLuint b, GLuint c, GLsizei d, GLenum e, const GLvoid *f))
GL_PROC(BaseGL, void, glEnable, (GLenum a))
GL_PROC(BaseGL, void, glEnableVertexAttribArray, (GLint a))
GL_PROC(BaseGL, void, glFrontFace, (GLenum a))
GL_PROC(BaseGL, void, glGenBuffers, (GLint a, GLuint *b))
GL_PROC(BaseGL, void, glGenTextures, (GLsizei a, GLuint *b))
GL_PROC(BaseGL, void, glGetIntegerv, (GLenum a, GLint *b))
GL_PROC(BaseGL, void, glPixelStorei, (GLenum a, GLint b))
GL_PROC(BaseGL, void, glPolygonOffset, (GLfloat a, GLfloat b))
GL_PROC(BaseGL, void, glReadPixels, (GLint a, GLint b, GLsizei c, GLsizei d, GLenum e, GLenum f, const GLvoid *g))
GL_PROC(BaseGL, void, glScissor, (GLint a, GLint b, GLint c, GLint d))
GL_PROC(BaseGL, void, glStencilFunc, (GLenum a, GLint b, GLint c))
GL_PROC(BaseGL, void, glStencilFuncSeparate, (GLenum a, GLenum b, GLenum c, GLenum d))
GL_PROC(BaseGL, void, glStencilOp, (GLenum a, GLenum b, GLenum c))
GL_PROC(BaseGL, void, glStencilOpSeparate, (GLenum a, GLenum b, GLenum c, GLenum d))
GL_PROC(BaseGL, void, glStencilMask, (GLint a))
GL_PROC(BaseGL, void, glCompressedTexImage2D, (GLenum a, GLint b, GLenum c, GLsizei d, GLsizei e, GLint f, GLsizei g, const GLvoid *h))
GL_PROC(BaseGL, void, glCompressedTexSubImage2D, (GLenum a, GLint b, GLint c, GLint d, GLsizei e, GLsizei f, GLenum g, GLsizei h, const GLvoid *i))
GL_PROC(BaseGL, void, glTexImage2D, (GLenum a, GLint b, GLint c, GLsizei d, GLsizei e, GLint f, GLenum g, GLenum h, const GLvoid *i))
GL_PROC(BaseGL, void, glTexParameterf, (GLenum a, GLenum b, GLfloat c))
GL_PROC(BaseGL, void, glTexParameteri, (GLenum a, GLenum b, GLint c))
GL_PROC(BaseGL, void, glTexSubImage2D, (GLenum a, GLint b, GLint c, GLint d, GLsizei e, GLsizei f, GLenum g, GLenum h, const GLvoid *i))
GL_PROC(BaseGL, void, glVertexAttribPointer, (GLuint a, GLint b, GLenum c, GLboolean d, GLsizei e, const GLvoid *f))
GL_PROC(BaseGL, void, glViewport, (GLint a, GLint b, GLsizei c, GLsizei d))

/* Core Profile functions, only screwed if you attempt a core profile mask */
GL_PROC(CoreGL, void, glBindVertexArray, (GLuint a))
GL_PROC(CoreGL, void, glDeleteVertexArrays, (GLsizei a, const GLuint *b))
GL_PROC(CoreGL, void, glGenVertexArrays, (GLsizei a, GLuint *b))
GL_PROC(CoreGL, const GLubyte*, glGetStringi, (GLenum a, GLuint b))

/* Base vertex support makes life WAY easier for batching */
GL_PROC_EXT(ARB_draw_elements_base_vertex, OES, void, glDrawElementsInstancedBaseVertex, (GLenum a, GLsizei b, GLenum c, const GLvoid *d, GLsizei e, GLint f))
GL_PROC_EXT(ARB_draw_elements_base_vertex, OES, void, glDrawRangeElementsBaseVertex, (GLenum a, GLuint b, GLuint c, GLsizei d, GLenum e, const GLvoid *f, GLint g))

/* These are in every desktop driver and _should_ be in every ES3 driver */
GL_PROC_EXT(3DTexture, OES, void, glTexImage3D, (GLenum a, GLint b, GLint c, GLsizei d, GLsizei e, GLsizei f, GLint g, GLenum h, GLenum i, const GLvoid *j))
GL_PROC_EXT(3DTexture, OES, void, glTexSubImage3D, (GLenum a, GLint b, GLint c, GLint d, GLint e, GLsizei f, GLsizei g, GLsizei h, GLenum i, GLenum j, const GLvoid *k))

/* For some bizarre reason ES doesn't have double-precision depth range in any spec? */
GL_PROC(DoublePrecisionDepth, void, glClearDepth, (GLdouble a))
GL_PROC(DoublePrecisionDepth, void, glDepthRange, (GLdouble a, GLdouble b))
GL_PROC_EXT(OES_single_precision, OES, void, glClearDepthf, (GLfloat a))
GL_PROC_EXT(OES_single_precision, OES, void, glDepthRangef, (GLfloat a, GLfloat b))

/* This should be in every desktop GL driver, but ES might not have it! */
GL_PROC(ARB_occlusion_query, void, glBeginQuery, (GLenum a, GLuint b))
GL_PROC(ARB_occlusion_query, void, glDeleteQueries, (GLsizei a, const GLuint *b))
GL_PROC(ARB_occlusion_query, void, glEndQuery, (GLenum a))
GL_PROC(ARB_occlusion_query, void, glGenQueries, (GLsizei a, GLuint *b))
GL_PROC(ARB_occlusion_query, void, glGetQueryObjectuiv, (GLuint a, GLenum b, GLuint *c))

/* These do NOT exist in OpenGL ES. You probably shouldn't ship these anyway. */
GL_PROC(NonES3, void, glGetBufferSubData, (GLenum a, GLintptr b, GLsizeiptr c, GLvoid *d))
GL_PROC(NonES3, void, glGetTexImage, (GLenum a, GLint b, GLenum c, GLenum d, GLvoid *e))
GL_PROC(NonES3, void, glPolygonMode, (GLenum a, GLenum b))

/* This is a terrible function for point sprites, don't worry about it */
GL_PROC(NonES3NonCore, void, glTexEnvi, (GLenum a, GLenum b, GLint c))

/* Needed for render targets. We're flexible, but not _that_ flexible. */
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glBindFramebuffer, (GLenum a, GLuint b))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glBindRenderbuffer, (GLenum a, GLuint b))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glDeleteFramebuffers, (GLsizei a, const GLuint *b))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glDeleteRenderbuffers, (GLsizei a, const GLuint *b))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glFramebufferRenderbuffer, (GLenum a, GLenum b, GLenum c, GLuint d))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glFramebufferTexture2D, (GLenum a, GLenum b, GLenum c, GLuint d, GLint e))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glGenerateMipmap, (GLenum a))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glGenFramebuffers, (GLsizei a, GLuint *b))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glGenRenderbuffers, (GLsizei a, GLuint *b))
GL_PROC_EXT(ARB_framebuffer_object, EXT, void, glRenderbufferStorage, (GLenum a, GLenum b, GLsizei c, GLsizei d))

/* Needed for the faux-backbuffer, this is technically ARB_framebuffer_object */
GL_PROC_EXT(EXT_framebuffer_blit, EXT, void, glBlitFramebuffer, (GLint a, GLint b, GLint c, GLint d, GLint e, GLint f, GLint g, GLint h, GLbitfield i, GLenum j))

/* Multisampling is nice to have, but isn't used all the time, technically ARB_framebuffer_object */
GL_PROC_EXT(EXT_framebuffer_multisample, EXT, void, glRenderbufferStorageMultisample, (GLenum a, GLsizei b, GLenum c, GLsizei d, GLsizei e))
GL_PROC(ARB_internalformat_query, void, glGetInternalformativ, (GLenum a, GLenum b, GLenum c, GLsizei d, GLint *e))

/* This is mostly needed by ES3, where loads/stores are a huge slowdown */
GL_PROC(ARB_invalidate_subdata, void, glInvalidateFramebuffer, (GLenum a, GLsizei b, const GLenum *c))

/* Hardware instancing is nice to have, but isn't used all the time */
GL_PROC(ARB_draw_instanced, void, glDrawElementsInstanced, (GLenum a, GLsizei b, GLenum c, const GLvoid *d, GLsizei e))
GL_PROC(ARB_instanced_arrays, void, glVertexAttribDivisor, (GLuint a, GLuint b))

/* Indexed color mask is a weird thing.
 * IndexedEXT was introduced in EXT_draw_buffers2, then
 * it was introduced in GL 3.0 as "ColorMaski" with no
 * extension at all, and OpenGL ES introduced it as
 * ColorMaskiEXT via EXT_draw_buffers_indexed and AGAIN
 * as ColorMaskiOES via OES_draw_buffers_indexed at the
 * exact same time. WTF.
 * -flibit
 */
GL_PROC(EXT_draw_buffers2, void, glColorMaski, (GLuint a, GLboolean b, GLboolean c, GLboolean d, GLboolean e))

/* Probably used by nobody, honestly */
GL_PROC(ARB_texture_multisample, void, glSampleMaski, (GLuint a, GLuint b))

/* Technically UnmapBuffer is core, but useless without MapBufferRange */
GL_PROC_EXT(ARB_map_buffer_range, EXT, GLvoid*, glMapBufferRange, (GLenum a, GLintptr b, GLsizeiptr c, GLbitfield d))

/* "NOTE: when implemented in an OpenGL ES context, all entry points defined
 * by this extension must have a "KHR" suffix. When implemented in an
 * OpenGL context, all entry points must have NO suffix, as shown below."
 * https://www.khronos.org/registry/OpenGL/extensions/KHR/KHR_debug.txt
 */
GL_PROC_EXT(KHR_debug, KHR, void, glDebugMessageCallback, (DEBUGPROC a, const GLvoid *b))
GL_PROC_EXT(KHR_debug, KHR, void, glDebugMessageControl, (GLenum a, GLenum b, GLenum c, GLsizei d, const GLuint *e, GLboolean f))

/* Nice feature for apitrace */
GL_PROC(GREMEDY_string_marker, void, glStringMarkerGREMEDY, (GLsizei a, const GLchar *b))

/* Redefine these every time you include this header! */
#undef GL_EXT
#undef GL_PROC
#undef GL_PROC_EXT

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
