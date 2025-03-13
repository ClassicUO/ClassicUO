// SPDX-License-Identifier: BSD-2-Clause

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

        protected override ArraySegment<byte> GetBuffer()
        {
            return _waveBuffer;
        }
    }
}
