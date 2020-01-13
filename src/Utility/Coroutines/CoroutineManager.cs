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

using System.Collections.Generic;

namespace ClassicUO.Utility.Coroutines
{
    internal class CoroutineManager
    {
        private static readonly QueuedPool<Coroutine> _pool = new QueuedPool<Coroutine>();
        private readonly List<Coroutine> _coroutines = new List<Coroutine>();
        private readonly List<Coroutine> _scheduled = new List<Coroutine>();
        private readonly List<Coroutine> _trashcan = new List<Coroutine>();

        public Coroutine StartNew(IEnumerator<IWaitCondition> enumerator, string name)
        {
            Coroutine coroutine = _pool.GetOne();
            coroutine.Setup(enumerator, name);

            _scheduled.Add(coroutine);

            return coroutine;
        }

        public void Clear()
        {
            _coroutines.ForEach(s => s.Cancel());
            _coroutines.ForEach(s => s.Dispose());

            _scheduled.Clear();
            _coroutines.Clear();
        }

        public void Update()
        {
            _coroutines.AddRange(_scheduled);
            _scheduled.Clear();

            _coroutines.ForEach(s =>
            {
                s.Update();

                if (s.Status != CoroutineStatus.Running && s.Status != CoroutineStatus.Paused)
                    _trashcan.Add(s);
            });

            for (int i = 0; i < _trashcan.Count; i++)
            {
                Coroutine c = _trashcan[i];
                _trashcan.RemoveAt(i--);
                _pool.ReturnOne(c);
            }

            _trashcan.Clear();
        }
    }
}