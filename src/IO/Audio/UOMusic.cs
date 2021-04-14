#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Configuration;
using ClassicUO.Data;
using MP3Sharp;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Audio;

namespace ClassicUO.IO.Audio
{
    internal class UOMusic : Sound
    {
        private const int NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK = 0x8000; // 32768 bytes, about 0.9 seconds
        private bool m_Playing;
        private readonly bool m_Repeat;
        private MP3Stream m_Stream;
        private readonly byte[] m_WaveBuffer = new byte[NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK];


        public UOMusic(int index, string name, bool loop, string basePath) : base(name, index)
        {
            m_Repeat = loop;
            m_Playing = false;
            Channels = AudioChannels.Stereo;
            Delay = 0;
            
            Path = System.IO.Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, $"{basePath}/{Name}.mp3");
        }

        private string Path { get; }

        public void Update()
        {
            // sanity - if the buffer empties, we will lose our sound effect. Thus we must continually check if it is dead.
            OnBufferNeeded(null, null);
        }

        protected override byte[] GetBuffer()
        {
            try
            {
                if (m_Playing && SoundInstance != null)
                {
                    int bytesReturned = m_Stream.Read(m_WaveBuffer, 0, m_WaveBuffer.Length);

                    if (bytesReturned != NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK)
                    {
                        if (m_Repeat)
                        {
                            m_Stream.Position = 0;
                            m_Stream.Read(m_WaveBuffer, bytesReturned, m_WaveBuffer.Length - bytesReturned);
                        }
                        else
                        {
                            if (bytesReturned == 0)
                            {
                                Stop();
                            }
                        }
                    }

                    return m_WaveBuffer;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            Stop();

            return null;
        }

        protected override void OnBufferNeeded(object sender, EventArgs e)
        {
            if (m_Playing)
            {
                if (SoundInstance == null)
                {
                    Stop();

                    return;
                }

                while (SoundInstance.PendingBufferCount < 3)
                {
                    byte[] buffer = GetBuffer();

                    if (SoundInstance.IsDisposed || buffer == null)
                    {
                        break;
                    }

                    SoundInstance.SubmitBuffer(buffer);
                }
            }
        }

        protected override void BeforePlay()
        {
            if (m_Playing)
            {
                Stop();
            }

            try
            {
                if (m_Stream != null)
                {
                    m_Stream.Close();
                    m_Stream = null;
                }

                m_Stream = new MP3Stream(Path, NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK);
                Frequency = m_Stream.Frequency;

                m_Playing = true;
            }
            catch
            {
                // file in use or access denied.
                m_Playing = false;
            }
        }

        protected override void AfterStop()
        {
            if (m_Playing)
            {
                m_Playing = false;
                m_Stream?.Close();
                m_Stream = null;
            }
        }
    }
}
