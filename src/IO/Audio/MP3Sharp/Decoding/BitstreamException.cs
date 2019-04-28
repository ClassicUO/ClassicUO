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