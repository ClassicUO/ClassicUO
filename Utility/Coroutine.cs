#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Utility
{
    public static class CoroutineManager
    {
        private static readonly List<Coroutine> _coroutines = new List<Coroutine>();

        public static Coroutine StartCoroutine(in IEnumerator routine)
        {
            Coroutine coroutine = new Coroutine(routine);
            _coroutines.Add(coroutine);
            return coroutine;
        }

        public static void Update()
        {
            for (int i = 0; i < _coroutines.Count; i++)
            {
                Coroutine coroutine = _coroutines[i];

                if (!coroutine.MoveNext())
                {
                    _coroutines[i] = _coroutines[_coroutines.Count - 1];
                    _coroutines.RemoveAt(_coroutines.Count - 1);
                }
            }
        }
    }

    public class YieldInstruction
    {
        internal IEnumerator routine;

        internal YieldInstruction()
        {
        }

        internal bool MoveNext()
        {
            if (routine.Current is YieldInstruction yieldInstruction)
            {
                if (yieldInstruction.MoveNext())
                    return true;
            }

            return routine.MoveNext();
        }
    }

    public class Coroutine : YieldInstruction
    {
        public Coroutine(in IEnumerator routine) => this.routine = routine;
    }

    public class WaitForSeconds : YieldInstruction
    {
        public WaitForSeconds(in float seconds)
        {
            DateTime delay = DateTime.Now.AddSeconds(seconds);
            routine = Count(delay);
        }

        private IEnumerator Count(DateTime delay)
        {
            while (DateTime.Now < delay)
                yield return true;
        }
    }

    public class WaitUntil : YieldInstruction
    {
        public WaitUntil(in Func<bool> func) => routine = Until(func);

        public WaitUntil(Func<bool> func, float seconds)
        {
            DateTime delay = DateTime.Now.AddSeconds(seconds);
            routine = UntilWithTimeout(func, delay);
        }

        private IEnumerator Until(Func<bool> func)
        {
            while (!func())
                yield return true;
        }

        private IEnumerator UntilWithTimeout(Func<bool> func, DateTime delay)
        {
            while (!func() && DateTime.Now < delay)
                yield return true;
        }
    }
}