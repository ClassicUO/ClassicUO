using System;
using System.Runtime.Serialization;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     The DecoderException represents the class of
    ///     errors that can occur when decoding MPEG audio.
    /// </summary>
    [Serializable]
    internal class DecoderException : MP3SharpException
    {
        private int m_ErrorCode;

        public DecoderException(string message, Exception inner) : base(message, inner)
        {
            InitBlock();
        }

        public DecoderException(int errorcode, Exception inner) : this(GetErrorString(errorcode), inner)
        {
            InitBlock();
            m_ErrorCode = errorcode;
        }

        protected DecoderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            m_ErrorCode = info.GetInt32("ErrorCode");
        }

        public virtual int ErrorCode => m_ErrorCode;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            info.AddValue("ErrorCode", m_ErrorCode);
            base.GetObjectData(info, context);
        }

        private void InitBlock()
        {
            m_ErrorCode = DecoderErrors.UNKNOWN_ERROR;
        }

        public static string GetErrorString(int errorcode)
        {
            // REVIEW: use resource file to map error codes
            // to locale-sensitive strings. 

            return "Decoder errorcode " + Convert.ToString(errorcode, 16);
        }
    }
}