using ClassicUO.Sdk.IO;
using System;
using System.IO;

namespace ClassicUO.Network
{
    sealed class PacketLogger
    {
        public static PacketLogger Default { get; set; } = new PacketLogger();



        private StreamWriter _logFile;

        public bool Enabled { get; set; }



        public StreamWriter CreateFile()
        {
            _logFile?.Dispose();
            var filePath = Path.Combine(Directory.CreateDirectory(Path.Combine(CUOEnviroment.ExecutablePath, "Logs", "Network")).FullName, "packets.log");
            return _logFile = new StreamWriter(File.OpenWrite(filePath));
        }

        public void Log(Span<byte> message, bool toServer)
        {
            if (!Enabled) return;

            Span<char> span = stackalloc char[256];
            var output = new ValueStringBuilder(span);
            {
                int off = sizeof(ulong) + 2;

                output.Append(' ', off);
                output.Append(string.Format("Ticks: {0} | {1} |  ID: {2:X2}   Length: {3}\n", Time.Ticks, (toServer ? "Client -> Server" : "Server -> Client"), message[0], message.Length));

                if (message[0] == 0x80 || message[0] == 0x91)
                {
                    output.Append(' ', off);
                    output.Append("[ACCOUNT CREDENTIALS HIDDEN]\n");
                }
                else
                {
                    output.Append(' ', off);
                    output.Append("0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F\n");

                    output.Append(' ', off);
                    output.Append("-- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --\n");

                    ulong address = 0;

                    for (int i = 0; i < message.Length; i += 16, address += 16)
                    {
                        output.Append($"{address:X8}");

                        for (int j = 0; j < 16; ++j)
                        {
                            if ((j % 8) == 0)
                            {
                                output.Append(" ");
                            }

                            if (i + j < message.Length)
                            {
                                output.Append($" {message[i + j]:X2}");
                            }
                            else
                            {
                                output.Append("   ");
                            }
                        }

                        output.Append("  ");

                        for (int j = 0; j < 16 && i + j < message.Length; ++j)
                        {
                            var c = message[i + j];

                            if (c >= 0x20 && c < 0x80)
                            {
                                output.Append((char)c);
                            }
                            else
                            {
                                output.Append('.');
                            }
                        }

                        output.Append('\n');
                    }
                }

                output.Append('\n');
                output.Append('\n');

                var s = output.ToString();

                if (_logFile != null)
                {
                    _logFile.Write(s);
                }
                else
                {
                    Console.WriteLine(s);
                }

                output.Dispose();
            }
        }
    }
}
