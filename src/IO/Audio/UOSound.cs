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

namespace ClassicUO.IO.Audio
{
    internal class UOSound : Sound
    {
        private readonly byte[] _waveBuffer;
        private bool _isPlaying;

        public UOSound(string name, int index, byte[] buffer)
            : base(name, index)
        {
            _isPlaying = false;
            _waveBuffer = buffer;
            Delay = (uint) ((buffer.Length - 32) / 88.2f);
        }

        public bool IsPlaying()
        {
            return _isPlaying;
        }

        protected override void OnBufferNeeded(object sender, EventArgs e)
        {
            // not needed.
        }

        protected override byte[] GetBuffer()
        {
            return _waveBuffer;
        }

        protected override void BeforePlay()
        {
            _isPlaying = true;
        }

        protected override void AfterStop()
        {
            _isPlaying = false;
        }
    }
}