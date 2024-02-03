#region license

// Copyright (c) 2024, andreakarasho
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

namespace ClassicUO.IO.Audio
{
    public class UOSound : Sound
    {
        private readonly byte[] _waveBuffer;

        public UOSound(string name, int index, byte[] buffer) : base(name, index)
        {
            _waveBuffer = buffer;
            Delay = (uint) ((buffer.Length - 32) / 88.2f);
        }

        public bool CalculateByDistance { get; set; }
        public int X, Y;

        protected override void OnBufferNeeded(object sender, EventArgs e)
        {
            // not needed.
            //if (World.InGame && X >= 0 && Y >= 0 && CalculateByDistance)
            //{
            //    int distX = Math.Abs(X - World.Player.X);
            //    int distY = Math.Abs(Y - World.Player.Y);
            //    int distance = Math.Max(distX, distY);

            //    float volume = ProfileManager.CurrentProfile.SoundVolume / Constants.SOUND_DELTA;
            //    float distanceFactor = 0.0f;

            //    if (distance >= 1)
            //    {
            //        float volumeByDist = volume / (World.ClientViewRange + 1);
            //        distanceFactor = volumeByDist * distance;
            //    }

            //    if (distance > World.ClientViewRange)
            //    {
            //        Stop();
            //        Dispose();
            //        return;
            //    }

            //    if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSound || !Client.Game.IsActive && !ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            //        volume = 0;

            //    if (Client.Game.IsActive)
            //    {
            //        if (!ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            //            volume = ProfileManager.CurrentProfile.SoundVolume / Constants.SOUND_DELTA;
            //    }
            //    else if (!ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            //        volume = 0;

            //    VolumeFactor = distanceFactor;
            //    Volume = volume;
            //}
        }

        protected override byte[] GetBuffer()
        {
            return _waveBuffer;
        }
    }
}