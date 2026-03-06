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

#ifndef FNA3D_IMAGE_H
#define FNA3D_IMAGE_H

#ifdef _WIN32
#define FNA3DAPI __declspec(dllexport)
#define FNA3DCALL __cdecl
#else
#define FNA3DAPI
#define FNA3DCALL
#endif

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif /* __cplusplus */

/* Image Read API */

typedef int32_t (FNA3DCALL * FNA3D_Image_ReadFunc)(
	void* context,
	char *data,
	int32_t size
);
typedef void (FNA3DCALL * FNA3D_Image_SkipFunc)(
	void* context,
	int32_t n
);
typedef int32_t (FNA3DCALL * FNA3D_Image_EOFFunc)(void* context);

/* Decodes PNG/JPG/GIF data into raw RGBA8 texture data.
 *
 * readFunc:	Callback used to pull data from the stream.
 * skipFunc:	Callback used to seek around a stream.
 * eofFunc:	Callback used to check that we're reached the end of a stream.
 * context:	User pointer passed back to the above callbacks.
 * w:		Filled with the width of the image.
 * h:		Filled with the height of the image.
 * len:		Filled with the size (in bytes) of the return value.
 * forceW:	Forced width of the returned image (-1 to ignore).
 * forceH:	Forced height of the returned image (-1 to ignore).
 * zoom:	When forcing dimensions, enable this to crop instead of stretch.
 *
 * Returns a block of memory suitable for use with FNA3D_SetTextureData2D.
 * Be sure to free the memory with FNA3D_Image_Free after use!
 */
FNA3DAPI uint8_t* FNA3D_Image_Load(
	FNA3D_Image_ReadFunc readFunc,
	FNA3D_Image_SkipFunc skipFunc,
	FNA3D_Image_EOFFunc eofFunc,
	void* context,
	int32_t *w,
	int32_t *h,
	int32_t *len,
	int32_t forceW,
	int32_t forceH,
	uint8_t zoom
);

/* Frees memory returned by FNA3D_Image_Load. (Do NOT free the memory yourself!)
 *
 * mem: A pointer previously returned by FNA3D_Image_Load.
 */
FNA3DAPI void FNA3D_Image_Free(uint8_t *mem);

/* Image Write API */

typedef void (FNA3DCALL * FNA3D_Image_WriteFunc)(
	void* context,
	void* data,
	int32_t size
);

/* Encodes RGBA8 image data into PNG data.
 *
 * writeFunc:	Callback used to write data to a stream.
 * context:	User pointer passed back to the above callback.
 * srcW:	The original width of the image data.
 * srcH:	The original height of the image data.
 * dstW:	The requested width of the PNG data.
 * dstH:	The requested height of the PNG data.
 * data:	The raw RGBA8 image data.
 */
FNA3DAPI void FNA3D_Image_SavePNG(
	FNA3D_Image_WriteFunc writeFunc,
	void* context,
	int32_t srcW,
	int32_t srcH,
	int32_t dstW,
	int32_t dstH,
	uint8_t *data
);

/* Encodes RGBA8 image data into JPG data, discarding the alpha channel.
 *
 * writeFunc:	Callback used to write data to a stream.
 * context:	User pointer passed back to the above callback.
 * srcW:	The original width of the image data.
 * srcH:	The original height of the image data.
 * dstW:	The requested width of the JPG data.
 * dstH:	The requested height of the JPG data.
 * data:	The raw RGBA8 image data.
 * quality:	The JPG compression quality (0 - 100).
 */
FNA3DAPI void FNA3D_Image_SaveJPG(
	FNA3D_Image_WriteFunc writeFunc,
	void* context,
	int32_t srcW,
	int32_t srcH,
	int32_t dstW,
	int32_t dstH,
	uint8_t *data,
	int32_t quality
);

#ifdef __cplusplus
}
#endif /* __cplusplus */

#endif /* FNA3D_IMAGE_H */

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
