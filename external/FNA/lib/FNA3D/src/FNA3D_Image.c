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

#include "FNA3D_Image.h"

#ifdef USE_SDL3
#include <SDL3/SDL.h>
#else
#include <SDL.h>
#define SDL_CreateSurface(a, b, c) \
	SDL_CreateRGBSurface( \
		0, \
		a, \
		b, \
		8 * ((c == SDL_PIXELFORMAT_RGBA32) ? 4 : 3), \
		0x000000FF, \
		0x0000FF00, \
		0x00FF0000, \
		(c == SDL_PIXELFORMAT_RGBA32) ? 0xFF000000 : 0 \
	)
#define SDL_CreateSurfaceFrom(a, b, c, d, e) \
	SDL_CreateRGBSurfaceFrom( \
		d, \
		a, \
		b, \
		8 * ((c == SDL_PIXELFORMAT_RGBA32) ? 4 : 3), \
		e, \
		0x000000FF, \
		0x0000FF00, \
		0x00FF0000, \
		(c == SDL_PIXELFORMAT_RGBA32) ? 0xFF000000 : 0 \
	)
#define SDL_BlitSurfaceScaled(a, b, c, d, e) SDL_BlitScaled(a, b, c, d)
#define SDL_DestroySurface SDL_FreeSurface
#define SDL_SURFACE_PREALLOCATED SDL_PREALLOC
#endif

extern void FNA3D_LogWarn(const char *fmt, ...);

#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wunused-but-set-variable"
#pragma GCC diagnostic ignored "-Wunused-function"
#ifndef __clang__
#pragma GCC diagnostic ignored "-Wmisleading-indentation"
#endif

#ifndef __STDC_WANT_SECURE_LIB__
#define __STDC_WANT_SECURE_LIB__ 1
#endif
#define sprintf_s SDL_snprintf

#define abs SDL_abs
#define ceilf SDL_ceilf
#define floorf SDL_floorf
#define ldexp SDL_scalbn
#define pow SDL_pow
#define strtol SDL_strtol

#ifdef memcmp
#undef memcmp
#endif
#define memcmp SDL_memcmp
#ifdef memcpy
#undef memcpy
#endif
#define memcpy SDL_memcpy
#ifdef memmove
#undef memmove
#endif
#define memmove SDL_memmove
#ifdef memset
#undef memset
#endif
#define memset SDL_memset
#ifdef strcmp
#undef strcmp
#endif
#define strcmp SDL_strcmp
#ifdef strlen
#undef strlen
#endif
#define strlen SDL_strlen
#ifdef strncmp
#undef strncmp
#endif
#define strncmp SDL_strncmp

/* These are per the Texture2D.FromStream spec */
#define STBI_ONLY_GIF
#define STBI_ONLY_PNG
#define STBI_ONLY_JPEG
#define STBI_ONLY_TGA
#define STBI_ONLY_QOI

/* These are per the Texture2D.SaveAs* spec */
#define STBIW_ONLY_PNG
#define STBIW_ONLY_JPEG

#define STBI_NO_STDIO
#define STB_IMAGE_STATIC
#define STBI_ASSERT SDL_assert
#ifdef USE_SDL3
#define STBI_MALLOC SDL_malloc
#define STBI_REALLOC SDL_realloc
#define STBI_FREE SDL_free
#else
#define STBI_MALLOC SDL_SIMDAlloc
#define STBI_REALLOC SDL_SIMDRealloc
#define STBI_FREE SDL_SIMDFree
#endif
#define STB_IMAGE_IMPLEMENTATION
#ifdef __MINGW32__
#define STBI_NO_THREAD_LOCALS /* FIXME: Port to SDL_TLS -flibit */
#endif
#include "stb_image.h"

#define MINIZ_NO_STDIO
#define MINIZ_NO_TIME
#define MINIZ_SDL_MALLOC
#define MZ_ASSERT(x) SDL_assert(x)
#include "miniz.h"

/* Thanks Daniel Gibson! */
static unsigned char* dgibson_stbi_zlib_compress(
	unsigned char *data,
	int data_len,
	int *out_len,
	int quality
) {
	mz_ulong buflen = mz_compressBound(data_len);
	unsigned char *buf = SDL_malloc(buflen);

	if (	buf == NULL ||
		mz_compress2(buf, &buflen, data, data_len, quality) != 0	)
	{
		SDL_free(buf);
		return NULL;
	}
	*out_len = buflen;
	return buf;
}

#define STBI_WRITE_NO_STDIO
#define STB_IMAGE_WRITE_STATIC
#define STBIW_ASSERT SDL_assert
#define STBIW_MALLOC SDL_malloc
#define STBIW_REALLOC SDL_realloc
#define STBIW_FREE SDL_free
#define STBIW_ZLIB_COMPRESS dgibson_stbi_zlib_compress
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image_write.h"

#pragma GCC diagnostic pop

/* Image Read API */

uint8_t* FNA3D_Image_Load(
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
) {
	uint8_t *result;
	uint8_t *pixels;
	int32_t format;
	float scale;
	SDL_Rect crop;
	uint8_t scaleWidth;
	SDL_Surface *surface, *newSurface;
	stbi_io_callbacks cb;
	int32_t i;

	cb.read = readFunc;
	cb.skip = skipFunc;
	cb.eof = eofFunc;
	result = stbi_load_from_callbacks(
		&cb,
		context,
		w,
		h,
		&format,
		STBI_rgb_alpha
	);

	if (result == NULL)
	{
		FNA3D_LogWarn("Image loading failed: %s", stbi_failure_reason());
	}

	if (forceW != -1 && forceH != -1)
	{
		surface = SDL_CreateSurfaceFrom(
			*w,
			*h,
			SDL_PIXELFORMAT_RGBA32,
			result,
			(*w) * 4
		);
#if SDL_MAJOR_VERSION < 3
		surface->flags |= SDL_SIMD_ALIGNED;
#endif

		if (zoom)
		{
			scaleWidth = surface->w < surface->h;
		}
		else
		{
			scaleWidth = surface->w > surface->h;
		}

		if (scaleWidth)
		{
			scale = forceW / (float) surface->w;
		}
		else
		{
			scale = forceH / (float) surface->h;
		}

		if (zoom)
		{
			*w = forceW;
			*h = forceH;
			if (scaleWidth)
			{
				crop.x = 0;
				crop.y = (int) (surface->h / 2 - (forceH / scale) / 2);
				crop.w = surface->w;
				crop.h = (int) (forceH / scale);
			}
			else
			{
				crop.x = (int) (surface->w / 2 - (forceW / scale) / 2);
				crop.y = 0;
				crop.w = (int) (forceW / scale);
				crop.h = surface->h;
			}
		}
		else
		{
			*w = (int) (surface->w * scale);
			*h = (int) (surface->h * scale);
		}

		/* Alloc surface, blit! */
		newSurface = SDL_CreateSurface(
			*w,
			*h,
			SDL_PIXELFORMAT_RGBA32
		);
		SDL_SetSurfaceBlendMode(surface, SDL_BLENDMODE_NONE);
		if (zoom)
		{
			SDL_BlitSurfaceScaled(
				surface,
				&crop,
				newSurface,
				NULL,
				SDL_SCALEMODE_LINEAR /* FIXME: is this correct */
			);
		}
		else
		{
			SDL_BlitSurfaceScaled(
				surface,
				NULL,
				newSurface,
				NULL,
				SDL_SCALEMODE_LINEAR /* FIXME: is this correct */
			);
		}
		SDL_DestroySurface(surface);
		SDL_free(result);

		/* We're going to cheat and let the client take the memory! */
		result = (uint8_t*) newSurface->pixels;
		newSurface->flags |= SDL_SURFACE_PREALLOCATED;
		SDL_DestroySurface(newSurface);
	}

	/* Ensure that the alpha pixels are... well, actual alpha.
	 * You think this looks stupid, but be assured: Your paint program is
	 * almost certainly even stupider.
	 * -flibit
	 */
	pixels = result;
	*len = (*w) * (*h) * 4;
	for (i = 0; i < *len; i += 4, pixels += 4)
	{
		if (pixels[3] == 0)
		{
			pixels[0] = 0;
			pixels[1] = 0;
			pixels[2] = 0;
		}
	}

	return result;
}

void FNA3D_Image_Free(uint8_t *mem)
{
	STBI_FREE(mem);
}

/* Image Write API */

void FNA3D_Image_SavePNG(
	FNA3D_Image_WriteFunc writeFunc,
	void* context,
	int32_t srcW,
	int32_t srcH,
	int32_t dstW,
	int32_t dstH,
	uint8_t *data
) {
	SDL_Surface *surface, *scaledSurface = NULL;
	uint8_t *pixels;
	uint8_t scale = (srcW != dstW) || (srcH != dstH);

	/* Only blit to scale, the format is already correct */
	if (scale)
	{
		surface = SDL_CreateSurfaceFrom(
			srcW,
			srcH,
			SDL_PIXELFORMAT_RGBA32,
			data,
			srcW * 4
		);
		scaledSurface = SDL_CreateSurface(
			dstW,
			dstH,
			SDL_PIXELFORMAT_RGBA32
		);
		SDL_SetSurfaceBlendMode(surface, SDL_BLENDMODE_NONE);
		SDL_BlitSurfaceScaled(surface, NULL, scaledSurface, NULL, SDL_SCALEMODE_LINEAR);
		SDL_DestroySurface(surface);
		pixels = (uint8_t*) scaledSurface->pixels;
	}
	else
	{
		pixels = data;
	}

	/* Write the image data, finally. */
	stbi_write_png_to_func(
		writeFunc,
		context,
		dstW,
		dstH,
		4,
		pixels,
		dstW * 4
	);

	/* Clean up. We out. */
	if (scale)
	{
		SDL_DestroySurface(scaledSurface);
	}
}

void FNA3D_Image_SaveJPG(
	FNA3D_Image_WriteFunc writeFunc,
	void* context,
	int32_t srcW,
	int32_t srcH,
	int32_t dstW,
	int32_t dstH,
	uint8_t *data,
	int32_t quality
) {
	/* Get an RGB24 surface at the specified width/height */
	SDL_Surface *surface = SDL_CreateSurfaceFrom(
		srcW,
		srcH,
		SDL_PIXELFORMAT_RGBA32,
		data,
		srcW * 4
	);
	SDL_Surface *convertSurface = SDL_CreateSurface(
		dstW,
		dstH,
		SDL_PIXELFORMAT_RGB24
	);
	SDL_SetSurfaceBlendMode(surface, SDL_BLENDMODE_NONE);
	SDL_BlitSurfaceScaled(surface, NULL, convertSurface, NULL, SDL_SCALEMODE_LINEAR);
	SDL_DestroySurface(surface);
	surface = convertSurface;

	/* Write the image, finally. */
	stbi_write_jpg_to_func(
		writeFunc,
		context,
		dstW,
		dstH,
		3,
		surface->pixels,
		quality
	);

	/* Clean up. We out. */
	SDL_DestroySurface(surface);
}

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
