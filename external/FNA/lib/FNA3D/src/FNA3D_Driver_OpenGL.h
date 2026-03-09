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

#ifndef FNA3D_DRIVER_OPENGL_H
#define FNA3D_DRIVER_OPENGL_H

/* Types */
typedef unsigned int	GLenum;
typedef unsigned int	GLbitfield;
typedef void		GLvoid;
typedef unsigned int	GLuint;
typedef int		GLint;
typedef unsigned char	GLubyte;
typedef int		GLsizei;
typedef float		GLfloat;
typedef float		GLclampf;
typedef double  	GLdouble;
typedef char		GLchar;
typedef uintptr_t	GLsizeiptr;
typedef intptr_t	GLintptr;
typedef unsigned char	GLboolean;

/* Hint */
#define GL_DONT_CARE					0x1100

/* 0/1 */
#define GL_ZERO 					0x0000
#define GL_ONE  					0x0001

/* True/False */
#define GL_FALSE					0x0000
#define GL_TRUE 					0x0001

/* Types */
#define GL_BYTE 					0x1400
#define GL_UNSIGNED_BYTE				0x1401
#define GL_SHORT					0x1402
#define GL_UNSIGNED_SHORT				0x1403
#define GL_UNSIGNED_INT 				0x1405
#define GL_FLOAT					0x1406
#define GL_HALF_FLOAT					0x140B
#define GL_UNSIGNED_SHORT_4_4_4_4_REV			0x8365
#define GL_UNSIGNED_SHORT_5_5_5_1_REV			0x8366
#define GL_UNSIGNED_INT_2_10_10_10_REV  		0x8368
#define GL_UNSIGNED_SHORT_5_6_5 			0x8363
#define GL_UNSIGNED_INT_24_8				0x84FA

/* Strings */
#define GL_VENDOR					0x1F00
#define GL_RENDERER					0x1F01
#define GL_VERSION					0x1F02
#define GL_EXTENSIONS					0x1F03

/* Clear Mask */
#define GL_COLOR_BUFFER_BIT				0x4000
#define GL_DEPTH_BUFFER_BIT				0x0100
#define GL_STENCIL_BUFFER_BIT				0x0400

/* Enable Caps */
#define GL_SCISSOR_TEST 				0x0C11
#define GL_DEPTH_TEST					0x0B71
#define GL_STENCIL_TEST 				0x0B90

/* Points */
#define GL_POINT_SPRITE 				0x8861
#define GL_COORD_REPLACE				0x8862

/* Polygons */
#define GL_LINE 					0x1B01
#define GL_FILL 					0x1B02
#define GL_CW						0x0900
#define GL_CCW  					0x0901
#define GL_FRONT					0x0404
#define GL_BACK 					0x0405
#define GL_FRONT_AND_BACK				0x0408
#define GL_CULL_FACE					0x0B44
#define GL_POLYGON_OFFSET_FILL  			0x8037

/* Texture Type */
#define GL_TEXTURE_2D					0x0DE1
#define GL_TEXTURE_3D					0x806F
#define GL_TEXTURE_CUBE_MAP				0x8513
#define GL_TEXTURE_CUBE_MAP_POSITIVE_X  		0x8515

/* Blend Mode */
#define GL_BLEND					0x0BE2
#define GL_SRC_COLOR					0x0300
#define GL_ONE_MINUS_SRC_COLOR  			0x0301
#define GL_SRC_ALPHA					0x0302
#define GL_ONE_MINUS_SRC_ALPHA  			0x0303
#define GL_DST_ALPHA					0x0304
#define GL_ONE_MINUS_DST_ALPHA  			0x0305
#define GL_DST_COLOR					0x0306
#define GL_ONE_MINUS_DST_COLOR  			0x0307
#define GL_SRC_ALPHA_SATURATE				0x0308
#define GL_CONSTANT_COLOR				0x8001
#define GL_ONE_MINUS_CONSTANT_COLOR			0x8002

/* Equations */
#define GL_MIN  					0x8007
#define GL_MAX  					0x8008
#define GL_FUNC_ADD					0x8006
#define GL_FUNC_SUBTRACT				0x800A
#define GL_FUNC_REVERSE_SUBTRACT			0x800B

/* Comparisons */
#define GL_NEVER					0x0200
#define GL_LESS 					0x0201
#define GL_EQUAL					0x0202
#define GL_LEQUAL					0x0203
#define GL_GREATER					0x0204
#define GL_NOTEQUAL					0x0205
#define GL_GEQUAL					0x0206
#define GL_ALWAYS					0x0207

/* Stencil Operations */
#define GL_INVERT					0x150A
#define GL_KEEP 					0x1E00
#define GL_REPLACE					0x1E01
#define GL_INCR 					0x1E02
#define GL_DECR 					0x1E03
#define GL_INCR_WRAP					0x8507
#define GL_DECR_WRAP					0x8508

/* Wrap Modes */
#define GL_REPEAT					0x2901
#define GL_CLAMP_TO_EDGE				0x812F
#define GL_MIRRORED_REPEAT				0x8370

/* Filters */
#define GL_NEAREST					0x2600
#define GL_LINEAR					0x2601
#define GL_NEAREST_MIPMAP_NEAREST			0x2700
#define GL_NEAREST_MIPMAP_LINEAR			0x2702
#define GL_LINEAR_MIPMAP_NEAREST			0x2701
#define GL_LINEAR_MIPMAP_LINEAR 			0x2703

/* Attachments */
#define GL_COLOR_ATTACHMENT0				0x8CE0
#define GL_DEPTH_ATTACHMENT				0x8D00
#define GL_STENCIL_ATTACHMENT				0x8D20
#define GL_DEPTH_STENCIL_ATTACHMENT			0x821A

/* Texture Formats */
#define GL_RED  					0x1903
#define GL_ALPHA					0x1906
#define GL_RGB  					0x1907
#define GL_RGBA 					0x1908
#define GL_RGB8 					0x8051
#define GL_RGBA8					0x8058
#define GL_RGBA4					0x8056
#define GL_RGB5_A1					0x8057
#define GL_RGB10_A2_EXT 				0x8059
#define GL_RGBA16					0x805B
#define GL_BGRA 					0x80E1
#define GL_DEPTH_COMPONENT16				0x81A5
#define GL_DEPTH_COMPONENT24				0x81A6
#define GL_RG   					0x8227
#define GL_RG8  					0x822B
#define GL_RG16 					0x822C
#define GL_R16F 					0x822D
#define GL_R32F 					0x822E
#define GL_RG16F					0x822F
#define GL_RG32F					0x8230
#define GL_RGBA32F					0x8814
#define GL_RGBA16F					0x881A
#define GL_DEPTH24_STENCIL8				0x88F0
#define GL_COMPRESSED_TEXTURE_FORMATS			0x86A3
#define GL_COMPRESSED_RGBA_S3TC_DXT1_EXT		0x83F1
#define GL_COMPRESSED_RGBA_S3TC_DXT3_EXT		0x83F2
#define GL_COMPRESSED_RGBA_S3TC_DXT5_EXT		0x83F3
#define GL_SRGB_EXT					0x8C40
#define GL_SRGB8_EXT					0x8C41
#define GL_SRGB_ALPHA_EXT				0x8C42
#define GL_SRGB8_ALPHA8_EXT				0x8C43
#define GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT		0x8C4F
#define GL_COMPRESSED_RGBA_BPTC_UNORM_EXT		0x8E8C
#define GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM_EXT		0x8E8D
#define GL_R8UI                           0x8232
#define GL_R16UI                          0x8234
#define GL_R8                             0x8229
#define GL_R16                            0x822A

/* EXT_framebuffer_sRGB */
#define GL_FRAMEBUFFER_SRGB_EXT				0x8DB9
#define GL_FRAMEBUFFER_SRGB_CAPABLE_EXT			0x8DBA

/* Texture Internal Formats */
#define GL_DEPTH_COMPONENT				0x1902
#define GL_DEPTH_STENCIL				0x84F9

/* Textures */
#define GL_TEXTURE_WRAP_S				0x2802
#define GL_TEXTURE_WRAP_T				0x2803
#define GL_TEXTURE_WRAP_R				0x8072
#define GL_TEXTURE_MAG_FILTER				0x2800
#define GL_TEXTURE_MIN_FILTER				0x2801
#define GL_TEXTURE_MAX_ANISOTROPY_EXT			0x84FE
#define GL_TEXTURE_BASE_LEVEL				0x813C
#define GL_TEXTURE_MAX_LEVEL				0x813D
#define GL_TEXTURE_LOD_BIAS				0x8501
#define GL_UNPACK_ALIGNMENT				0x0CF5

/* Multitexture */
#define GL_TEXTURE0					0x84C0
#define GL_MAX_TEXTURE_IMAGE_UNITS			0x8872
#define GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS		0x8B4C

/* Buffer objects */
#define GL_ARRAY_BUFFER 				0x8892
#define GL_ELEMENT_ARRAY_BUFFER 			0x8893
#define GL_STREAM_DRAW  				0x88E0
#define GL_STATIC_DRAW  				0x88E4
#define GL_MAX_VERTEX_ATTRIBS				0x8869

/* NoOverwrite Uploads */
#define GL_MAP_WRITE_BIT				0x0002
#define GL_MAP_UNSYNCHRONIZED_BIT			0x0020

/* Render targets */
#define GL_FRAMEBUFFER  				0x8D40
#define GL_READ_FRAMEBUFFER				0x8CA8
#define GL_DRAW_FRAMEBUFFER				0x8CA9
#define GL_RENDERBUFFER 				0x8D41
#define GL_MAX_DRAW_BUFFERS				0x8824

/* Draw Primitives */
#define GL_POINTS					0x0000
#define GL_LINES					0x0001
#define GL_LINE_STRIP					0x0003
#define GL_TRIANGLES					0x0004
#define GL_TRIANGLE_STRIP				0x0005

/* Query Objects */
#define GL_QUERY_RESULT 				0x8866
#define GL_QUERY_RESULT_AVAILABLE			0x8867
#define GL_SAMPLES_PASSED				0x8914

/* Multisampling */
#define GL_MULTISAMPLE  				0x809D
#define GL_MAX_SAMPLES  				0x8D57
#define GL_SAMPLE_MASK  				0x8E51
#define GL_SAMPLES					0x80A9

/* 3.2 Core Profile */
#define GL_NUM_EXTENSIONS				0x821D

/* Debug Source */
#define GL_DEBUG_SOURCE_API				0x8246
#define GL_DEBUG_SOURCE_WINDOW_SYSTEM			0x8247
#define GL_DEBUG_SOURCE_SHADER_COMPILER			0x8248
#define GL_DEBUG_SOURCE_THIRD_PARTY			0x8249
#define GL_DEBUG_SOURCE_APPLICATION			0x824A
#define GL_DEBUG_SOURCE_OTHER				0x824B

/* Debug Type */
#define GL_DEBUG_TYPE_ERROR				0x824C
#define GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR		0x824D
#define GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR		0x824E
#define GL_DEBUG_TYPE_PORTABILITY			0x824F
#define GL_DEBUG_TYPE_PERFORMANCE			0x8250
#define GL_DEBUG_TYPE_OTHER				0x8251

/* Debug Severity */
#define GL_DEBUG_SEVERITY_HIGH  			0x9146
#define GL_DEBUG_SEVERITY_MEDIUM			0x9147
#define GL_DEBUG_SEVERITY_LOW				0x9148
#define GL_DEBUG_SEVERITY_NOTIFICATION  		0x826B

/* In case this needs to be exported in a certain way... */
#ifdef _WIN32 /* Windows OpenGL uses stdcall */
#define GLAPIENTRY __stdcall
#else
#define GLAPIENTRY
#endif

/* Debug callback typedef */
typedef void (GLAPIENTRY *DEBUGPROC)(
	GLenum source,
	GLenum type,
	GLuint id,
	GLenum severity,
	GLsizei length,
	const GLchar *message,
	const void *userParam
);

/* Function typedefs */
#define GL_EXT(ext)
#define GL_PROC(ext, ret, func, parms) \
	typedef ret (GLAPIENTRY *glfntype_##func) parms;
#define GL_PROC_EXT(ext, fallbacl, ret, func, parms) \
	typedef ret (GLAPIENTRY *glfntype_##func) parms;
#include "FNA3D_Driver_OpenGL_glfuncs.h"

/* glGetString is a bit different since we load it early */
typedef const GLubyte* (GLAPIENTRY *glfntype_glGetString)(GLenum a);

#endif /* FNA3D_DRIVER_OPENGL_H */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
