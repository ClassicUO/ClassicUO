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
using System.Runtime.Serialization;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     Instances of BitstreamException are thrown
    ///     when operations on a Bitstream fail.
    ///     <p>
    ///         The exception provides details of the exception condition
    ///         in two ways:
    ///         <ol>
    ///             <li>
    ///                 as an error-code describing the nature of the error
    ///             </li>
    ///             <br></br>
    ///             <li>
    ///                 as the Throwable instance, if any, that was thrown
    ///                 indicating that an exceptional condition has occurred.
    ///             </li>
    ///         </ol>
    ///     </p>
    /// </summary>
    [Serializable]
    public class BitstreamException : MP3SharpException
    {
        private int m_Errorcode;

        public BitstreamException(string message, Exception inner) : base(message, inner)
        {
            InitBlock();
        }

        public BitstreamException(int errorcode, Exception inner) : this(GetErrorString(errorcode), inner)
        {
            InitBlock();
            m_Errorcode = errorcode;
        }

        protected BitstreamException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            m_Errorcode = info.GetInt32("ErrorCode");
        }

        public virtual int ErrorCode => m_Errorcode;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            info.AddValue("ErrorCode", m_Errorcode);
            base.GetObjectData(info, context);
        }

        private void InitBlock()
        {
            m_Errorcode = BitstreamErrors.UNKNOWN_ERROR;
        }

        public static string GetErrorString(int errorcode)
        {
            return "Bitstream errorcode " + Convert.ToString(errorcode, 16);
        }
    }
}