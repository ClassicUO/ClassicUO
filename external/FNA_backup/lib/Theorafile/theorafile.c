/* Theorafile - Ogg Theora Video Decoder Library
 *
 * Copyright (c) 2017-2021 Ethan Lee.
 * Based on TheoraPlay, Copyright (c) 2011-2016 Ryan C. Gordon.
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

#include "theorafile.h"

#include <stdio.h> /* fopen and friends */
#include <stdlib.h> /* realloc */
#include <string.h> /* memcpy, memset */

#define TF_DEFAULT_BUFFER_SIZE 4096

#ifdef _WIN32
#define inline __inline
#endif /* _WIN32 */

static inline int INTERNAL_readOggData(OggTheora_File *file)
{
	long buflen = TF_DEFAULT_BUFFER_SIZE;
	char *buffer = ogg_sync_buffer(&file->sync, buflen);
	if (buffer == NULL)
	{
		/* If you made it here, you ran out of RAM (wait, what?) */
		return -1;
	}

	buflen = file->io.read_func(buffer, 1, buflen, file->datasource);
	if (buflen <= 0)
	{
		return 0;
	}

	return (ogg_sync_wrote(&file->sync, buflen) == 0) ? 1 : -1;
}

static inline void INTERNAL_queueOggPage(OggTheora_File *file)
{
	if (file->tpackets)
	{
		ogg_stream_pagein(&file->tstream, &file->page);
	}
	if (file->vpackets)
	{
		ogg_stream_pagein(&file->vstream[file->vtrack], &file->page);
	}
}

static inline int INTERNAL_getNextPacket(
	OggTheora_File *file,
	ogg_stream_state *stream,
	ogg_packet *packet
) {
	while (ogg_stream_packetout(stream, packet) <= 0)
	{
		const int rc = INTERNAL_readOggData(file);
		if (rc == 0)
		{
			file->eos = 1;
			return 0;
		}
		else if (rc < 0)
		{
			/* If you made it here, something REALLY bad happened.
			 *
			 * Unfortunately, ogg_sync_wrote does not give out any
			 * codes, so I have no idea what that something is.
			 *
			 * Be sure you're not doing something nasty like
			 * accessing one file via multiple threads at one time.
			 * -flibit
			 */
			file->eos = 1;
			return 0;
		}
		else
		{
			while (ogg_sync_pageout(&file->sync, &file->page) > 0)
			{
				INTERNAL_queueOggPage(file);
			}
		}
	}
	return 1;
}

int tf_open_callbacks(void *datasource, OggTheora_File *file, tf_callbacks io)
{
	ogg_packet packet;
	ogg_stream_state filler;
	th_setup_info *tsetup = NULL;
	int pp_level_max = 0;
	int errcode = TF_EUNKNOWN;
	vorbis_info vinfo;
	vorbis_comment vcomment;

	if (datasource == NULL)
	{
		return TF_ENODATASOURCE;
	}

	memset(file, '\0', sizeof(OggTheora_File));
	file->datasource = datasource;
	file->io = io;

	#define TF_OPEN_ASSERT(cond) \
		if (cond) goto fail;


	ogg_sync_init(&file->sync);
	vorbis_info_init(&vinfo);
	vorbis_comment_init(&vcomment);
	th_info_init(&file->tinfo);
	th_comment_init(&file->tcomment);

	/* Is there even data for us to read...? */
	TF_OPEN_ASSERT(INTERNAL_readOggData(file) <= 0)

	/* Read header */
	while (ogg_sync_pageout(&file->sync, &file->page) > 0)
	{
		if (!ogg_page_bos(&file->page))
		{
			/* Not a header! */
			INTERNAL_queueOggPage(file);
			break;
		}

		ogg_stream_init(&filler, ogg_page_serialno(&file->page));
		ogg_stream_pagein(&filler, &file->page);
		ogg_stream_packetout(&filler, &packet);

		if (!file->tpackets && (th_decode_headerin(
			&file->tinfo,
			&file->tcomment,
			&tsetup,
			&packet
		) >= 0)) {
			memcpy(&file->tstream, &filler, sizeof(filler));
			file->tpackets = 1;
		}
		else if (vorbis_synthesis_headerin(
			&vinfo,
			&vcomment,
			&packet
		) >= 0) {
			file->vinfo = realloc(file->vinfo, (file->vtracks + 1) * sizeof(file->vinfo[0]));
			file->vcomment = realloc(file->vcomment, (file->vtracks + 1) * sizeof(file->vcomment[0]));
			file->vstream = realloc(file->vstream, (file->vtracks + 1) * sizeof(file->vstream[0]));
			file->vinfo[file->vtracks] = vinfo;
			file->vcomment[file->vtracks] = vcomment;
			memcpy(&file->vstream[file->vtracks], &filler, sizeof(filler));
			file->vtracks += 1;
			file->vpackets += 1;

			/* Reset this for other possible Vorbis streams */
			vorbis_info_init(&vinfo);
			vorbis_comment_init(&vcomment);
		}
		else
		{
			/* Whatever it is, we don't care about it */
			ogg_stream_clear(&filler);
		}
	}

	vorbis_comment_clear(&vcomment);
	vorbis_info_clear(&vinfo);

	/* No audio OR video? */
	TF_OPEN_ASSERT(!file->tpackets && !file->vpackets)

	/* Apparently there are 2 more theora and 2 more vorbis headers next. */
	#define TPACKETS (file->tpackets && (file->tpackets < 3))
	#define VPACKETS (file->vpackets && (file->vpackets < (file->vtracks + 2)))
	while (TPACKETS || VPACKETS)
	{
		while (TPACKETS)
		{
			if (ogg_stream_packetout(
				&file->tstream,
				&packet
			) != 1) {
				/* Get more data? */
				break;
			}
			TF_OPEN_ASSERT(!th_decode_headerin(
				&file->tinfo,
				&file->tcomment,
				&tsetup,
				&packet
			))
			file->tpackets += 1;
		}

		while (VPACKETS)
		{
			if (ogg_stream_packetout(
				&file->vstream[file->vtrack],
				&packet
			) != 1) {
				/* Get more data? */
				break;
			}
			TF_OPEN_ASSERT(vorbis_synthesis_headerin(
				&file->vinfo[0],
				&file->vcomment[0],
				&packet
			))
			file->vpackets += 1;
		}

		/* Get another page, try again? */
		if (ogg_sync_pageout(&file->sync, &file->page) > 0)
		{
			INTERNAL_queueOggPage(file);
		}
		else
		{
			TF_OPEN_ASSERT(INTERNAL_readOggData(file) < 0)
		}
	}
	#undef TPACKETS
	#undef VPACKETS

	/* Set up Theora stream */
	if (file->tpackets)
	{
		/* th_decode_alloc() docs say to check for
		 * insanely large frames yourself.
		 */
		TF_OPEN_ASSERT(
			(file->tinfo.frame_width > 99999) ||
			(file->tinfo.frame_height > 99999)
		)

		/* FIXME: We treat "unspecified" as NTSC :shrug: */
		if (	(file->tinfo.colorspace != TH_CS_UNSPECIFIED) &&
			(file->tinfo.colorspace != TH_CS_ITU_REC_470M) &&
			(file->tinfo.colorspace != TH_CS_ITU_REC_470BG)	)
		{
			errcode = TF_EUNSUPPORTED;
			goto fail;
		}

		if (	file->tinfo.pixel_fmt != TH_PF_420 &&
			file->tinfo.pixel_fmt != TH_PF_422 &&
			file->tinfo.pixel_fmt != TH_PF_444	)
		{
			errcode = TF_EUNSUPPORTED;
			goto fail;
		}

		/* The decoder, at last! */
		file->tdec = th_decode_alloc(&file->tinfo, tsetup);
		TF_OPEN_ASSERT(!file->tdec)

		/* Disable all post-processing in the decoder.
		 * FIXME: Maybe an API to set this?
		 * FIXME: Could be TH_DECCTL_GET_PPLEVEL_MAX, for example!
		 * FIXME: Theoretically we could enable post-processing and then
		 * FIXME: drop the quality level if we're not keeping up.
		 */
		th_decode_ctl(
			file->tdec,
			TH_DECCTL_SET_PPLEVEL,
			&pp_level_max,
			sizeof(pp_level_max)
		);
	}

	/* Done with this now */
	if (tsetup != NULL)
	{
		th_setup_free(tsetup);
		tsetup = NULL;
	}

	/* Set up Vorbis stream */
	if (file->vpackets)
	{
		file->vdsp_init = vorbis_synthesis_init(
			&file->vdsp,
			&file->vinfo[file->vtrack]
		) == 0;
		TF_OPEN_ASSERT(!file->vdsp_init)
		file->vblock_init = vorbis_block_init(
			&file->vdsp,
			&file->vblock
		) == 0;
		TF_OPEN_ASSERT(!file->vblock_init)
	}

	#undef TF_OPEN_ASSERT

	/* Finally. */
	return 0;
fail:
	if (tsetup != NULL)
	{
		th_setup_free(tsetup);
	}
	tf_close(file);
	return errcode;
}

int tf_fopen(const char *fname, OggTheora_File *file)
{
	tf_callbacks io =
	{
		(size_t (*) (void*, size_t, size_t, void*)) fread,
		(int (*) (void*, ogg_int64_t, int)) fseek,
		(int (*) (void*)) fclose,
	};
	return tf_open_callbacks(
		fopen(fname, "rb"),
		file,
		io
	);
}

void tf_close(OggTheora_File *file)
{
	int i;

	/* Theora Data */
	if (file->tdec != NULL)
	{
		th_decode_free(file->tdec);
	}

	/* Vorbis Data */
	if (file->vblock_init)
	{
		vorbis_block_clear(&file->vblock);
	}
	if (file->vdsp_init)
	{
		vorbis_dsp_clear(&file->vdsp);
	}

	/* Stream Data */
	if (file->tpackets)
	{
		ogg_stream_clear(&file->tstream);
	}
	if (file->vpackets)
	{
		ogg_stream_clear(&file->vstream[file->vtrack]);
	}

	/* Metadata */
	th_info_clear(&file->tinfo);
	th_comment_clear(&file->tcomment);
	for (i = 0; i < file->vtracks; i += 1)
	{
		vorbis_comment_clear(&file->vcomment[i]);
		vorbis_info_clear(&file->vinfo[i]);
	}
	free(file->vstream);
	free(file->vcomment);
	free(file->vinfo);

	/* Current State */
	ogg_sync_clear(&file->sync);

	/* I/O Data */
	if (file->io.close_func != NULL)
	{
		file->io.close_func(file->datasource);
	}
}

int tf_hasvideo(OggTheora_File *file)
{
	return file->tpackets != 0;
}

int tf_hasaudio(OggTheora_File *file)
{
	return file->vpackets != 0;
}

void tf_videoinfo(
	OggTheora_File *file,
	int *width,
	int *height,
	double *fps,
	th_pixel_fmt *fmt
) {
	if (width != NULL)
	{
		*width = file->tinfo.pic_width;
	}
	if (height != NULL)
	{
		*height = file->tinfo.pic_height;
	}
	if (fps != NULL)
	{
		if (file->tinfo.fps_denominator != 0)
		{
			*fps = (
				((double) file->tinfo.fps_numerator) /
				((double) file->tinfo.fps_denominator)
			);
		}
		else
		{
			*fps = 0.0;
		}
	}
	if (fmt != NULL)
	{
		*fmt = file->tinfo.pixel_fmt;
	}
}

void tf_audioinfo(OggTheora_File *file, int *channels, int *samplerate)
{
	if (channels != NULL)
	{
		*channels = file->vinfo[file->vtrack].channels;
	}
	if (samplerate != NULL)
	{
		*samplerate = file->vinfo[file->vtrack].rate;
	}
}

int tf_setaudiotrack(OggTheora_File *file, int vtrack)
{
	/* Note there may be a slight delay changing track midstream. */
	if (vtrack >= 0 && vtrack < file->vtracks)
	{
		file->vtrack = vtrack;
		return 1;
	}
	else
	{
		return 0;
	}
}

int tf_eos(OggTheora_File *file)
{
	return file->eos;
}

void tf_reset(OggTheora_File *file)
{
	if (file->tpackets)
	{
		ogg_stream_reset(&file->tstream);
	}
	if (file->vpackets)
	{
		ogg_stream_reset(&file->vstream[file->vtrack]);
	}
	ogg_sync_reset(&file->sync);
	file->io.seek_func(file->datasource, 0, SEEK_SET);
	file->eos = 0;
}

int tf_readvideo(OggTheora_File *file, char *buffer, int numframes)
{
	int i;
	char *dst = buffer;
	ogg_int64_t granulepos = 0;
	ogg_packet packet;
	th_ycbcr_buffer ycbcr;
	int rc;
	int w, h, off;
	unsigned char *plane;
	int stride;
	int retval = 0;

	for (i = 0; i < numframes; i += 1)
	{
		/* Keep trying to get a usable packet */
		if (!INTERNAL_getNextPacket(file, &file->tstream, &packet))
		{
			/* ... unless there's nothing left for us to read. */
			if (retval)
			{
				break;
			}
			return 0;
		}

		rc = th_decode_packetin(
			file->tdec,
			&packet,
			&granulepos
		);

		if (rc == 0) /* New frame! */
		{
			retval = 1;
		}
		else if (rc != TH_DUPFRAME)
		{
			return 0; /* Why did we get here...? */
		}
	}

	if (retval) /* New frame! */
	{
		if (th_decode_ycbcr_out(file->tdec, ycbcr) != 0)
		{
			return 0; /* Uhh?! */
		}

		#define TF_COPY_CHANNEL(chan) \
			plane = ycbcr[chan].data + off; \
			stride = ycbcr[chan].stride; \
			for (i = 0; i < h; i += 1, dst += w) \
			{ \
				memcpy( \
					dst, \
					plane + (stride * i), \
					w \
				); \
			}
		/* Y */
		w = file->tinfo.pic_width;
		h = file->tinfo.pic_height;
		off = (
			(file->tinfo.pic_x & ~1) +
			ycbcr[0].stride *
			(file->tinfo.pic_y & ~1)
		);
		TF_COPY_CHANNEL(0)

		/* U/V */
		if (file->tinfo.pixel_fmt == TH_PF_420)
		{
			/* Subsampled in both dimensions */
			w /= 2;
			h /= 2;
			off = (
				(file->tinfo.pic_x / 2) +
				(ycbcr[1].stride) *
				(file->tinfo.pic_y / 2)
			);
		}
		else if (file->tinfo.pixel_fmt == TH_PF_422)
		{
			/* Subsampled only horizontally */
			w /= 2;
			off = (
				(file->tinfo.pic_x / 2) +
				(ycbcr[1].stride) *
				(file->tinfo.pic_y & ~1)
			);
		}
		TF_COPY_CHANNEL(1)
		TF_COPY_CHANNEL(2)
		#undef TF_COPY_CHANNEL
	}
	return retval;
}

int tf_readaudio(OggTheora_File *file, float *buffer, int samples)
{
	int offset = 0;
	int chan, frame;
	ogg_packet packet;
	float **pcm = NULL;

	while (offset < samples)
	{
		const int frames = vorbis_synthesis_pcmout(&file->vdsp, &pcm);
		if (frames > 0)
		{
			/* I bet this beats the crap out of the CPU cache... */
			for (frame = 0; frame < frames; frame += 1)
			for (chan = 0; chan < file->vinfo[file->vtrack].channels; chan += 1)
			{
				buffer[offset++] = pcm[chan][frame];
				if (offset >= samples)
				{
					vorbis_synthesis_read(
						&file->vdsp,
						frame
					);
					return offset;
				}
			}
			vorbis_synthesis_read(&file->vdsp, frames);
		}
		else /* No audio available left in current packet? */
		{
			/* Keep trying to get a usable packet */
			if (!INTERNAL_getNextPacket(file, &file->vstream[file->vtrack], &packet))
			{
				/* ... unless there's nothing left for us to read. */
				return offset;
			}
			if (vorbis_synthesis(
				&file->vblock,
				&packet
			) == 0) {
				vorbis_synthesis_blockin(
					&file->vdsp,
					&file->vblock
				);
			}
		}
	}
	return offset;
}
