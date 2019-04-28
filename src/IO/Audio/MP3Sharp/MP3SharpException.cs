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