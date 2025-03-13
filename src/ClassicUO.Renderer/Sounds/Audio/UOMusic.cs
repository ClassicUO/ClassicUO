// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Audio;
using MP3Sharp;
using System;

namespace ClassicUO.IO.Audio
{
    public class UOMusic : Sound
    {
        private const int NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK = 0x8000; // 32768 bytes, about 0.9 seconds
        private bool m_Playing;
        private readonly bool m_Repeat;
        private MP3Stream m_Stream;
        private readonly byte[] m_WaveBuffer = new byte[NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK];


        public UOMusic(int index, string name, bool loop, string fileName) : base(name, index)
        {
            m_Repeat = loop;
            m_Playing = false;
            Channels = AudioChannels.Stereo;
            Delay = 0;

            Path = fileName;
        }

        private string Path { get; }

        public void Update()
        {
            // sanity - if the buffer empties, we will lose our sound effect. Thus we must continually check if it is dead.
            OnBufferNeeded(null, null);
        }

        protected override ArraySegment<byte> GetBuffer()
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

                    return new ArraySegment<byte>(m_WaveBuffer, 0, bytesReturned);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            Stop();

            return ArraySegment<byte>.Empty;
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
                    var buffer = GetBuffer();

                    if (SoundInstance.IsDisposed || buffer.Count == 0)
                    {
                        break;
                    }

                    SoundInstance.SubmitBuffer(buffer.Array, buffer.Offset, buffer.Count);
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
