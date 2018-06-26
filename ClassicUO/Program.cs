using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;


namespace ClassicUO
{
    class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetDllDirectory(string lpPathName);

        static void Main(string[] args)
        {
			if(Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				SetDllDirectory(Path.Combine(
					AppDomain.CurrentDomain.BaseDirectory,
					Environment.Is64BitProcess ? "x64" : "x86"
				));
			}

            Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");

            using (GameLoop game = new GameLoop())
            {
				bool isHighDPI = Environment.GetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI") == "1";
				if(isHighDPI)
					Debug.WriteLine("HiDPI Enabled");

                game.Run();
            }
        }
    }
}
