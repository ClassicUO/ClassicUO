/* FAudio - XAudio Reimplementation for FNA
 *
 * Copyright (c) 2011-2025 Ethan Lee and the FNA team
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
 * Katelyn Gadd <kg@luminance.org>
 *
 */

#include "FAudioFX.h"
#include "FAudio_internal.h"

/* Sample Collector FAPO Implementation */

const FAudioGUID FAudioFX_CLSID_Collector = /* 2.7 */
{
	0xCAC1105F,
	0x619B,
	0x4D04,
	{
		0x83,
		0x1A,
		0x44,
		0xE1,
		0xCB,
		0xF1,
		0x2D,
		0x58
	}
};

static FAPORegistrationProperties CollectorProperties =
{
	/* .clsid = */ {0},
	/* .FriendlyName = */
	{
		'C', 'o', 'l', 'l', 'e', 'c', 't', 'o', 'r', '\0'
	},
	/*.CopyrightInfo = */
	{
		'C', 'o', 'p', 'y', 'r', 'i', 'g', 'h', 't', ' ', '(', 'c', ')',
		'K', 'a', 't', 'e', 'l', 'y', 'n', ' ', 'G', 'a', 'd', 'd', '\0'
	},
	/*.MajorVersion = */ 0,
	/*.MinorVersion = */ 0,
	/*.Flags = */(
		FAPO_FLAG_CHANNELS_MUST_MATCH |
		FAPO_FLAG_FRAMERATE_MUST_MATCH |
		FAPO_FLAG_BITSPERSAMPLE_MUST_MATCH |
		FAPO_FLAG_BUFFERCOUNT_MUST_MATCH |
		FAPO_FLAG_INPLACE_SUPPORTED |
		FAPO_FLAG_INPLACE_REQUIRED
	),
	/*.MinInputBufferCount = */ 1,
	/*.MaxInputBufferCount = */ 1,
	/*.MinOutputBufferCount = */ 1,
	/*.MaxOutputBufferCount =*/ 1
};

typedef struct FAudioFXCollector
{
	FAPOBase base;
	uint16_t channels;
	float*   pBuffer;
	uint32_t bufferLength;
	uint32_t writeOffset;
} FAudioFXCollector;

uint32_t FAudioFXCollector_LockForProcess(
	FAudioFXCollector*fapo,
	uint32_t InputLockedParameterCount,
	const FAPOLockForProcessBufferParameters *pInputLockedParameters,
	uint32_t OutputLockedParameterCount,
	const FAPOLockForProcessBufferParameters *pOutputLockedParameters
) {
	/* Verify parameter counts... */
	if (	InputLockedParameterCount < fapo->base.m_pRegistrationProperties->MinInputBufferCount ||
		InputLockedParameterCount > fapo->base.m_pRegistrationProperties->MaxInputBufferCount ||
		OutputLockedParameterCount < fapo->base.m_pRegistrationProperties->MinOutputBufferCount ||
		OutputLockedParameterCount > fapo->base.m_pRegistrationProperties->MaxOutputBufferCount	)
	{
		return FAUDIO_E_INVALID_ARG;
	}


	/* Validate input/output formats */
	#define VERIFY_FORMAT_FLAG(flag, prop) \
		if (	(fapo->base.m_pRegistrationProperties->Flags & flag) && \
			(pInputLockedParameters->pFormat->prop != pOutputLockedParameters->pFormat->prop)	) \
		{ \
			return FAUDIO_E_INVALID_ARG; \
		}
	VERIFY_FORMAT_FLAG(FAPO_FLAG_CHANNELS_MUST_MATCH, nChannels)
	VERIFY_FORMAT_FLAG(FAPO_FLAG_FRAMERATE_MUST_MATCH, nSamplesPerSec)
	VERIFY_FORMAT_FLAG(FAPO_FLAG_BITSPERSAMPLE_MUST_MATCH, wBitsPerSample)
	#undef VERIFY_FORMAT_FLAG
	if (	(fapo->base.m_pRegistrationProperties->Flags & FAPO_FLAG_BUFFERCOUNT_MUST_MATCH) &&
		(InputLockedParameterCount != OutputLockedParameterCount)	)
	{
		return FAUDIO_E_INVALID_ARG;
	}

	fapo->channels = pInputLockedParameters->pFormat->nChannels;
	fapo->base.m_fIsLocked = 1;
	return 0;
}

void FAudioFXCollector_UnlockForProcess(FAudioFXCollector* fapo)
{
	fapo->base.m_fIsLocked = 0;
}

void FAudioFXCollector_Process(
	FAudioFXCollector* fapo,
	uint32_t InputProcessParameterCount,
	const FAPOProcessBufferParameters* pInputProcessParameters,
	uint32_t OutputProcessParameterCount,
	FAPOProcessBufferParameters* pOutputProcessParameters,
	int32_t IsEnabled
) {
	uint32_t channels = fapo->channels,
		bufferLength = fapo->bufferLength,
		sampleCount = pInputProcessParameters->ValidFrameCount;
	uint32_t writeOffset = fapo->writeOffset;
	float* pBuffer = (float*)pInputProcessParameters->pBuffer;
	float* pOutput = fapo->pBuffer;
	float multiplier = 1.0f / channels;

	for (uint32_t i = 0; i < sampleCount; i++) {
		// Accumulate all channels and average them
		float accumulator = 0;
		for (uint32_t j = 0; j < channels; j++)
			accumulator += pBuffer[j];
		accumulator *= multiplier;

		// Advance to next set of samples
		pBuffer += channels;

		// Write to output buffer
		pOutput[writeOffset++] = accumulator;
		// Wrap write offset
		if (writeOffset >= bufferLength)
			writeOffset = 0;
	}

	fapo->writeOffset = writeOffset;
	FAPOBase_EndProcess(&fapo->base);
}

void FAudioFXCollector_Free(void* fapo)
{
	FAudioFXCollector *collector = (FAudioFXCollector*) fapo;
	collector->base.pFree(fapo);
}

void FAudioFXCollector_GetParameters(
	FAudioFXCollector* fapo,
	FAudioFXCollectorState* pParameters,
	uint32_t ParameterByteSize
) {
	FAudio_assert(pParameters);
	FAudio_assert(ParameterByteSize == sizeof(FAudioFXCollectorState));
	pParameters->WriteOffset = fapo->writeOffset;
}

/* Public API */

uint32_t FAudioCreateCollectorEXT(FAPO** ppApo, uint32_t Flags, float* pBuffer, uint32_t bufferLength)
{
	return FAudioCreateCollectorWithCustomAllocatorEXT(
		ppApo,
		Flags,
		pBuffer,
		bufferLength,
		FAudio_malloc,
		FAudio_free,
		FAudio_realloc
	);
}

FAUDIOAPI uint32_t FAudioCreateCollectorWithCustomAllocatorEXT(
	FAPO** ppApo,
	uint32_t Flags,
	float* pBuffer,
	uint32_t bufferLength,
	FAudioMallocFunc customMalloc,
	FAudioFreeFunc customFree,
	FAudioReallocFunc customRealloc
) {
	FAudio_assert(ppApo);
	FAudio_assert(pBuffer);
	FAudio_assert(bufferLength);

	/* Allocate... */
	FAudioFXCollector *result = (FAudioFXCollector*) customMalloc(
		sizeof(FAudioFXCollector)
	);

	/* Initialize... */
	FAudio_memcpy(
		&CollectorProperties.clsid,
		&FAudioFX_CLSID_Collector,
		sizeof(FAudioGUID)
	);
	CreateFAPOBaseWithCustomAllocatorEXT(
		&result->base,
		&CollectorProperties,
		NULL,
		0,
		1,
		customMalloc,
		customFree,
		customRealloc
	);

	/* Function table... */
	result->base.base.LockForProcess = (LockForProcessFunc)
		FAudioFXCollector_LockForProcess;
	result->base.base.UnlockForProcess = (UnlockForProcessFunc)
		FAudioFXCollector_UnlockForProcess;
	result->base.base.Process = (ProcessFunc)
		FAudioFXCollector_Process;
	result->base.base.GetParameters = (GetParametersFunc)
		FAudioFXCollector_GetParameters;
	result->base.Destructor = FAudioFXCollector_Free;

	result->pBuffer = pBuffer;
	result->writeOffset = 0;
	result->bufferLength = bufferLength;

	/* Finally. */
	*ppApo = &result->base.base;
	return 0;
}

/* vim: set noexpandtab shiftwidth=8 tabstop=8: */
