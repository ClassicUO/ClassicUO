#region license

// Copyright (c) 2021, andreakarasho
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
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

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