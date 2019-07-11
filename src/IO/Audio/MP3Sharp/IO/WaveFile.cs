using System.IO;

using ClassicUO.IO.Audio.MP3Sharp.Support;

namespace ClassicUO.IO.Audio.MP3Sharp.IO
{
    /// <summary>
    ///     Class allowing WaveFormat Access
    /// </summary>
    internal class WaveFile : RiffFile
    {
        public const int MAX_WAVE_CHANNELS = 2;
        private readonly int m_NumSamples;
        private readonly RiffChunkHeader m_PcmData;
        private readonly WaveFormatChunk m_WaveFormat;
        private bool m_JustWriteLengthBytes;
        private long m_PcmDataOffset; // offset of 'pcm_data' in output file

        /// <summary>
        ///     Constructs a new WaveFile instance.
        /// </summary>
        public WaveFile()
        {
            m_PcmData = new RiffChunkHeader(this);
            m_WaveFormat = new WaveFormatChunk(this);
            m_PcmData.CkId = FourCC("data");
            m_PcmData.CkSize = 0;
            m_NumSamples = 0;
        }

        /// <summary>
        ///     *
        ///     *
        ///     public int OpenForRead (String Filename)
        ///     {
        ///     // Verify filename parameter as best we can...
        ///     if (Filename == null)
        ///     {
        ///     return DDC_INVALID_CALL;
        ///     }
        ///     int retcode = Open ( Filename, RFM_READ );
        /// </summary>
        /// <summary>
        ///     if ( retcode == DDC_SUCCESS )
        ///     {
        ///     retcode = Expect ( "WAVE", 4 );
        /// </summary>
        /// <summary>
        ///     if ( retcode == DDC_SUCCESS )
        ///     {
        ///     retcode = Read(wave_format,24);
        /// </summary>
        /// <summary>
        ///     if ( retcode == DDC_SUCCESS && !wave_format.VerifyValidity() )
        ///     {
        ///     // This isn't standard PCM, so we don't know what it is!
        ///     retcode = DDC_FILE_ERROR;
        ///     }
        /// </summary>
        /// <summary>
        ///     if ( retcode == DDC_SUCCESS )
        ///     {
        ///     pcm_data_offset = CurrentFilePosition();
        /// </summary>
        /// <summary>
        ///     // Figure out number of samples from
        ///     // file size, current file position, and
        ///     // WAVE header.
        ///     retcode = Read (pcm_data, 8 );
        ///     num_samples = filelength(fileno(file)) - CurrentFilePosition();
        ///     num_samples /= NumChannels();
        ///     num_samples /= (BitsPerSample() / 8);
        ///     }
        ///     }
        ///     }
        ///     return retcode;
        ///     }
        /// </summary>
        /// <summary>
        ///     Pass in either a FileName or a Stream.
        /// </summary>
        public virtual int OpenForWrite(string filename, Stream stream, int samplingRate, short bitsPerSample,
                                        short numChannels)
        {
            // Verify parameters...
            if (bitsPerSample != 8 && bitsPerSample != 16 || numChannels < 1 || numChannels > 2) return DDC_INVALID_CALL;

            m_WaveFormat.Data.Config(samplingRate, bitsPerSample, numChannels);

            int retcode = 0;

            if (stream != null)
                Open(stream, RFM_WRITE);
            else
                Open(filename, RFM_WRITE);

            if (retcode == DDC_SUCCESS)
            {
                sbyte[] theWave =
                {
                    (sbyte) SupportClass.Identity('W'), (sbyte) SupportClass.Identity('A'),
                    (sbyte) SupportClass.Identity('V'), (sbyte) SupportClass.Identity('E')
                };
                retcode = Write(theWave, 4);

                if (retcode == DDC_SUCCESS)
                {
                    // Ecriture de wave_format
                    retcode = Write(m_WaveFormat.Header, 8);
                    retcode = Write(m_WaveFormat.Data.FormatTag, 2);
                    retcode = Write(m_WaveFormat.Data.NumChannels, 2);
                    retcode = Write(m_WaveFormat.Data.NumSamplesPerSec, 4);
                    retcode = Write(m_WaveFormat.Data.NumAvgBytesPerSec, 4);
                    retcode = Write(m_WaveFormat.Data.NumBlockAlign, 2);
                    retcode = Write(m_WaveFormat.Data.NumBitsPerSample, 2);

                    if (retcode == DDC_SUCCESS)
                    {
                        m_PcmDataOffset = CurrentFilePosition();
                        retcode = Write(m_PcmData, 8);
                    }
                }
            }

            return retcode;
        }

        /// <summary>
        ///     Write 16-bit audio
        /// </summary>
        public virtual int WriteData(short[] data, int numData)
        {
            int extraBytes = numData * 2;
            m_PcmData.CkSize += extraBytes;

            return Write(data, extraBytes);
        }

        public override int Close()
        {
            int rc = DDC_SUCCESS;

            if (Fmode == RFM_WRITE)
                rc = Backpatch(m_PcmDataOffset, m_PcmData, 8);

            if (!m_JustWriteLengthBytes)
            {
                if (rc == DDC_SUCCESS)
                    rc = base.Close();
            }

            return rc;
        }

        public int Close(bool justWriteLengthBytes)
        {
            m_JustWriteLengthBytes = justWriteLengthBytes;
            int ret = Close();
            m_JustWriteLengthBytes = false;

            return ret;
        }

        // [Hz]
        public virtual int SamplingRate()
        {
            return m_WaveFormat.Data.NumSamplesPerSec;
        }

        public virtual short BitsPerSample()
        {
            return m_WaveFormat.Data.NumBitsPerSample;
        }

        public virtual short NumChannels()
        {
            return m_WaveFormat.Data.NumChannels;
        }

        public virtual int NumSamples()
        {
            return m_NumSamples;
        }

        /// <summary>
        ///     Open for write using another wave file's parameters...
        /// </summary>
        public virtual int OpenForWrite(string filename, WaveFile otherWave)
        {
            return OpenForWrite(filename, null, otherWave.SamplingRate(), otherWave.BitsPerSample(),
                                otherWave.NumChannels());
        }

        internal sealed class WaveFormatChunkData
        {
            public short FormatTag; // Format category (PCM=1)
            public int NumAvgBytesPerSec;
            public short NumBitsPerSample;
            public short NumBlockAlign;
            public short NumChannels; // Number of channels (mono=1, stereo=2)
            public int NumSamplesPerSec; // Sampling rate [Hz]

            public WaveFormatChunkData(WaveFile enclosingInstance)
            {
                InitBlock(enclosingInstance);
                FormatTag = 1; // PCM
                Config(44100, 16, 1);
            }

            public WaveFile EnclosingInstance { get; private set; }

            private void InitBlock(WaveFile enclosingInstance)
            {
                EnclosingInstance = enclosingInstance;
            }

            public void Config(int newSamplingRate, short newBitsPerSample, short newNumChannels)
            {
                NumSamplesPerSec = newSamplingRate;
                NumChannels = newNumChannels;
                NumBitsPerSample = newBitsPerSample;
                NumAvgBytesPerSec = NumChannels * NumSamplesPerSec * NumBitsPerSample / 8;
                NumBlockAlign = (short) (NumChannels * NumBitsPerSample / 8);
            }
        }

        internal class WaveFormatChunk
        {
            public WaveFormatChunkData Data;
            public RiffChunkHeader Header;

            public WaveFormatChunk(WaveFile enclosingInstance)
            {
                InitBlock(enclosingInstance);
                Header = new RiffChunkHeader(enclosingInstance);
                Data = new WaveFormatChunkData(enclosingInstance);
                Header.CkId = FourCC("fmt ");
                Header.CkSize = 16;
            }

            public WaveFile EnclosingInstance { get; private set; }

            private void InitBlock(WaveFile enclosingInstance)
            {
                EnclosingInstance = enclosingInstance;
            }

            public virtual int VerifyValidity()
            {
                bool ret = Header.CkId == FourCC("fmt ") && (Data.NumChannels == 1 || Data.NumChannels == 2) &&
                           Data.NumAvgBytesPerSec == Data.NumChannels * Data.NumSamplesPerSec * Data.NumBitsPerSample / 8 &&
                           Data.NumBlockAlign == Data.NumChannels * Data.NumBitsPerSample / 8;

                return ret ? 1 : 0;
            }
        }

        internal class WaveFileSample
        {
            public short[] Chan;

            public WaveFileSample(WaveFile enclosingInstance)
            {
                InitBlock(enclosingInstance);
                Chan = new short[MAX_WAVE_CHANNELS];
            }

            public WaveFile EnclosingInstance { get; private set; }

            private void InitBlock(WaveFile enclosingInstance)
            {
                EnclosingInstance = enclosingInstance;
            }
        }
    }
}