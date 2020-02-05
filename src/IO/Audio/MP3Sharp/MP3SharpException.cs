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
using System.IO;
using System.Runtime.Serialization;

using ClassicUO.IO.Audio.MP3Sharp.Support;

namespace ClassicUO.IO.Audio.MP3Sharp
{
    /// <summary>
    ///     MP3SharpException is the base class for all API-level
    ///     exceptions thrown by MP3Sharp. To facilitate conversion and
    ///     common handling of exceptions from other domains, the class
    ///     can delegate some functionality to a contained Throwable instance.
    /// </summary>
    [Serializable]
    public class MP3SharpException : Exception
    {
        public MP3SharpException()
        {
        }

        public MP3SharpException(string message) : base(message)
        {
        }

        public MP3SharpException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MP3SharpException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public void PrintStackTrace()
        {
            SupportClass.WriteStackTrace(this, Console.Error);
        }

        public void PrintStackTrace(StreamWriter ps)
        {
            if (InnerException == null)
                SupportClass.WriteStackTrace(this, ps);
            else
                SupportClass.WriteStackTrace(InnerException, Console.Error);
        }
    }
}