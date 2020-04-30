#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.IO.Audio.MP3Sharp;
using ClassicUO.Utility.Logging;

using csmidi;

using Microsoft.Xna.Framework.Audio;

namespace ClassicUO.IO.Audio
{
    sealed class UOMidMusic : Sound
    {
        private const int NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK = 0x8000;
        private readonly byte[] m_WaveBuffer = new byte[NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK];


        private MidiFile _mid_file;
        private bool _playing;
        private readonly bool _reapeat;
        private int _index_0, _index_1;

        private string Path => System.IO.Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, $"music/{Name}.mid");

        public UOMidMusic(int index, string name, bool reapeat) : base(name, index)
        {
            _reapeat = reapeat;
            _playing = false;
        }

        protected override byte[] GetBuffer()
        {
            try
            {
                if (_playing)
                {
                    int done = 0;

                    for (int i = 0; i < _mid_file.midiTracks.Count; i++)
                    {
                        for (int j = 0; j < _mid_file.midiTracks[i].midiEvents.Count; j++)
                        {
                            var m = _mid_file.midiTracks[i].midiEvents[j];
                            byte[] data = m.getEventData();

                            Array.Copy(data, 0, m_WaveBuffer, done + m.absoluteTicks * 2, data.Length);
                            done += data.Length;
                        }
                    }

                    if (done != NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK)
                    {
                        if (_reapeat)
                        {
                            _index_0 = 0;
                            _index_1 = 0;
                        }
                        else
                        {
                            if (done == 0)
                                Stop();
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

        public override void Update()
        {
            OnBufferNeeded(null, null);
        }

        protected override void OnBufferNeeded(object sender, EventArgs e)
        {
            if (_playing)
            {
                while (m_ThisInstance.PendingBufferCount < 3)
                {
                    byte[] buffer = GetBuffer();

                    if (m_ThisInstance.IsDisposed)
                        return;

                    m_ThisInstance.SubmitBuffer(buffer);
                }
            }
        }

        protected override void BeforePlay()
        {
            if (_playing)
                Stop();

            try
            {
                _mid_file = new MidiFile();
                _mid_file.loadMidiFromFile(Path);
                _playing = true;
            }
            catch
            {
                _playing = false;
            }
        }

        protected override void AfterStop()
        {
            if (_playing)
            {
                _playing = false;
                _mid_file?.midiTracks?.Clear();
                _mid_file = null;
            }
        }
    }


    sealed class UOMusic : Sound
    {
        private const int NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK = 0x8000; // 32768 bytes, about 0.9 seconds
        private readonly bool m_Repeat;
        private readonly byte[] m_WaveBuffer = new byte[NUMBER_OF_PCM_BYTES_TO_READ_PER_CHUNK];
        private bool m_Playing;
        private MP3Stream m_Stream;


        public UOMusic(int index, string name, bool loop)
            : base(name, index)
        {
            m_Repeat = loop;
            m_Playing = false;
            Channels = AudioChannels.Stereo;
            Delay = 0;
        }

        private string Path => System.IO.Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, Client.Version > ClientVersion.CV_5090 ? $"Music/Digital/{Name}.mp3" : $"music/{Name}.mp3");

        public override void Update()
        {
            // sanity - if the buffer empties, we will lose our sound effect. Thus we must continually check if it is dead.
            OnBufferNeeded(null, null);
        }

        protected override byte[] GetBuffer()
        {
            try
            {
                if (m_Playing)
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
                                Stop();
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
                while (m_ThisInstance.PendingBufferCount < 3)
                {
                    byte[] buffer = GetBuffer();

                    if (m_ThisInstance.IsDisposed)
                        return;

                    m_ThisInstance.SubmitBuffer(buffer);
                }
            }
        }

        protected override void BeforePlay()
        {
            if (m_Playing) 
                Stop();

            try
            {
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
                m_Stream.Close();
                m_Stream = null;
            }
        }
    }
}