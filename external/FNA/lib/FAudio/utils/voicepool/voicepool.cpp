#include <SDL3/SDL.h>
#include <FAudio.h>
#include "../uicommon/imgui.h"
#include "../wavcommon/wavs.h"
#include <vector>
#include <stdlib.h> /* free */

/* UI Vars */
const char *TOOL_NAME = "Voice Pool";
int TOOL_WIDTH = 320;
int TOOL_HEIGHT = 190;

/* Engine Vars */
static bool loaded = false;
static FAudio *faudio = NULL;
static FAudioMasteringVoice *master = NULL;
static std::vector<FAudioSourceVoice*> monoPool;
static std::vector<FAudioSourceVoice*> stereoPool;
static std::vector<unsigned int> monoPoolRate;
static std::vector<unsigned int> stereoPoolRate;
static float *sounds[6];
static drwav_uint64 soundLength[6];
static unsigned int soundRate[6];

void FAudioTool_Quit()
{
	if (loaded)
	{
		for (size_t i = 0; i < monoPool.size(); i += 1)
		{
			FAudioVoice_DestroyVoice(monoPool[i]);
		}
		monoPool.clear();
		monoPoolRate.clear();
		for (size_t i = 0; i < stereoPool.size(); i += 1)
		{
			FAudioVoice_DestroyVoice(stereoPool[i]);
		}
		stereoPool.clear();
		stereoPoolRate.clear();

		FAudioVoice_DestroyVoice(master);
		FAudio_Release(faudio);

		for (size_t i = 0; i < 6; i += 1)
		{
			free(sounds[i]);
		}

		loaded = false;
	}
}

void FAudioTool_Update()
{
	bool play_wave = false;

	static int wave_index = (int) AudioWave_SnareDrum01;
	static bool wave_stereo = false;

	/* UI Work */

	ImGui::SetNextWindowPos(ImVec2(0.0f, 0.0f));
	ImGui::SetNextWindowSize(ImVec2(640.0f, 125.0f));
	ImGui::Begin("Wave file to play");
		ImGui::RadioButton("Snare Drum (Forte)", &wave_index, (int) AudioWave_SnareDrum01);
		ImGui::RadioButton("Snare Drum (Fortissimo)", &wave_index, (int) AudioWave_SnareDrum02);
		ImGui::RadioButton("Snare Drum (Mezzo-Forte)", &wave_index, (int) AudioWave_SnareDrum03); 

		play_wave = ImGui::Button("Play");
		ImGui::SameLine();
		ImGui::Checkbox("Stereo", &wave_stereo);
	ImGui::End();

	ImGui::SetNextWindowPos(ImVec2(0.0f, 125.0f));
	ImGui::SetNextWindowSize(ImVec2(640.0f, 65.0f));
	ImGui::Begin("Current pool stats");
		ImGui::Text("Mono Voices: %d", monoPool.size());
		ImGui::Text("Stereo Voices: %d", stereoPool.size());
	ImGui::End();

	/* Engine Work */

	if (!loaded)
	{
		uint32_t hr = FAudioCreate(&faudio, 0, FAUDIO_DEFAULT_PROCESSOR);
		SDL_assert(hr == 0);
		hr = FAudio_CreateMasteringVoice(
			faudio,
			&master,
			FAUDIO_DEFAULT_CHANNELS,
			FAUDIO_DEFAULT_SAMPLERATE,
			0,
			0,
			NULL
		);
		SDL_assert(hr == 0);

		unsigned int channels;
		sounds[0] = WAVS_Open(AudioWave_SnareDrum01, false,  &channels, &soundRate[0], &soundLength[0]);
		SDL_assert(sounds[0] != NULL);
		SDL_assert(channels == 1);
		sounds[1] = WAVS_Open(AudioWave_SnareDrum02, false,  &channels, &soundRate[1], &soundLength[1]);
		SDL_assert(sounds[1] != NULL);
		SDL_assert(channels == 1);
		sounds[2] = WAVS_Open(AudioWave_SnareDrum03, false,  &channels, &soundRate[2], &soundLength[2]);
		SDL_assert(sounds[2] != NULL);
		SDL_assert(channels == 1);
		sounds[3] = WAVS_Open(AudioWave_SnareDrum01, true,  &channels, &soundRate[3], &soundLength[3]);
		SDL_assert(sounds[3] != NULL);
		SDL_assert(channels == 2);
		sounds[4] = WAVS_Open(AudioWave_SnareDrum02, true,  &channels, &soundRate[4], &soundLength[4]);
		SDL_assert(sounds[4] != NULL);
		SDL_assert(channels == 2);
		sounds[5] = WAVS_Open(AudioWave_SnareDrum03, true,  &channels, &soundRate[5], &soundLength[5]);
		SDL_assert(sounds[5] != NULL);
		SDL_assert(channels == 2);

		loaded = true;
	}

	if (play_wave)
	{
		FAudioSourceVoice *src;
		unsigned int rate;
		size_t index = wave_index + (wave_stereo ? 3 : 0);

		bool found = false;
		const std::vector<FAudioSourceVoice*>& pool = wave_stereo ? stereoPool : monoPool;
		const std::vector<unsigned int>& poolRate = wave_stereo ? stereoPoolRate : monoPoolRate;
		for (size_t i = 0; i < pool.size(); i += 1)
		{
			FAudioVoiceState state;
			FAudioSourceVoice_GetState(pool[i], &state, FAUDIO_VOICE_NOSAMPLESPLAYED);
			if (state.BuffersQueued == 0)
			{
				/* Nothing is queued, reuse this voice! */
				src = pool[i];
				rate = poolRate[i];
				found = true;
				break;
			}
		}
		if (found)
		{
			/* At most we can change the sample rate, can't change channel count */
			if (soundRate[index] != rate)
			{
				FAudioSourceVoice_SetSourceSampleRate(src, soundRate[index]);
			}
		}
		else
		{
			/* Pool didn't have anything left, make a new voice */
			FAudioWaveFormatEx waveFormat;
			waveFormat.wFormatTag = 3;
			waveFormat.nChannels = wave_stereo ? 2 : 1;
			waveFormat.nSamplesPerSec = soundRate[index];
			waveFormat.nBlockAlign = waveFormat.nChannels * sizeof(float);
			waveFormat.nAvgBytesPerSec = waveFormat.nSamplesPerSec * waveFormat.nBlockAlign;
			waveFormat.wBitsPerSample = sizeof(float) * 8;
			waveFormat.cbSize = 0;

			uint32_t hr = FAudio_CreateSourceVoice(
				faudio,
				&src,
				&waveFormat,
				0,
				FAUDIO_DEFAULT_FREQ_RATIO,
				NULL,
				NULL,
				NULL
			);

			/* Just start this now, let the buffer call "play" instead */
			FAudioSourceVoice_Start(src, 0, FAUDIO_COMMIT_NOW);

			if (wave_stereo)
			{
				stereoPool.push_back(src);
				stereoPoolRate.push_back(soundRate[index]);
			}
			else
			{
				monoPool.push_back(src);
				monoPoolRate.push_back(soundRate[index]);
			}
		}

		/* Buffer and play, finally */
		FAudioBuffer buffer;
		buffer.Flags = FAUDIO_END_OF_STREAM;
		buffer.AudioBytes = soundLength[index] * sizeof(float);
		buffer.pAudioData = (uint8_t*) sounds[index];
		buffer.PlayBegin = 0;
		buffer.PlayLength = soundLength[index] / (wave_stereo ? 2 : 1);
		buffer.LoopBegin = 0;
		buffer.LoopLength = 0;
		buffer.LoopCount = 0;
		buffer.pContext = NULL;
		FAudioSourceVoice_SubmitSourceBuffer(src, &buffer, NULL);
	}
}
