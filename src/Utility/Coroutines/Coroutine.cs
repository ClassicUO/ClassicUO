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
using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Utility.Coroutines
{
    internal sealed class Coroutine : IDisposable
    {
        private IEnumerator<IWaitCondition> _enumerator;
        private string _name;

        public CoroutineStatus Status { get; private set; }

        public void Dispose()
        {
            _enumerator.Dispose();
            Status = CoroutineStatus.Disposed;
        }

        public static Coroutine Start(Scene scene, IEnumerable<IWaitCondition> method, string name = null)
        {
            return scene.Coroutines.StartNew(method.GetEnumerator(), name);
        }

        public static Coroutine Start(Scene scene, IEnumerator<IWaitCondition> enumerator, string name = null)
        {
            return scene.Coroutines.StartNew(enumerator, name);
        }

        internal void Setup(IEnumerator<IWaitCondition> values, string name)
        {
            Status = CoroutineStatus.Running;
            _name = name;
            _enumerator?.Dispose();
            _enumerator = values;

            if (!_enumerator.MoveNext())
                Status = CoroutineStatus.Complete;
        }

        internal void Update()
        {
            if (Status != CoroutineStatus.Running)
                return;

            IWaitCondition currentYield = _enumerator.Current;

            if (currentYield == null || currentYield.Update())
            {
                if (!_enumerator.MoveNext())
                    Status = CoroutineStatus.Complete;
            }
        }

        internal void Restart()
        {
            _enumerator.Reset();
            _enumerator.MoveNext();
        }

        public void Pause()
        {
            if (Status == CoroutineStatus.Running)
                Status = CoroutineStatus.Paused;
        }

        public void Resume()
        {
            if (Status == CoroutineStatus.Paused)
                Status = CoroutineStatus.Running;
        }

        public void Cancel()
        {
            _enumerator.Dispose();
            Status = CoroutineStatus.Cancelled;
        }
    }
}