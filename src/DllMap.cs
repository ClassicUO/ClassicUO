using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace ClassicUO
{
#if !NETFRAMEWORK
    // only works in .NET Core. disable in .NET framework
    public static class DllMap
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDefaultDllDirectories(int directoryFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void AddDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        private const int LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        public static Dictionary<string, string> MapDictionary;
        public static string OS;
        public static string CPU;
        public static bool Optimise;

        public static void Initialise(bool optimise = true)
        {
            Optimise = optimise;

            // Our executable needs to know how to find the native libraries
            // For Windows, we can set this to be x86 or x64 directory at runtime (below)
            // For Linux we need to move our native libraries to 'netcoredeps' which is set by .net core
            // For OSX we need to set an environment variable (DYLD_LIBRARY_PATH) outside of the process by a script
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
                    AddDllDirectory(
                        Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            Environment.Is64BitProcess ? "x64" : "x86"
                        )
                    );
                }
                catch
                {
                    // Pre-Windows 7, KB2533623
                    SetDllDirectory(
                        Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            Environment.Is64BitProcess ? "x64" : "x86"
                        )
                    );
                }
            }

            // .NET Core also doesn't use DllImport but we can replicate this using NativeLibrary as per below
            // Uses FNA.dll.config to dictate what the name of the native library is per platform and architecture
            var fnaAssembly = Assembly.GetAssembly(typeof(Microsoft.Xna.Framework.Graphics.ColorWriteChannels));
            Register(fnaAssembly);
        }

        // Register a call-back for native library resolution.
        private static void Register(Assembly assembly)
        {
            NativeLibrary.SetDllImportResolver(assembly, MapAndLoad);

            // Do setup so that MapLibraryName is faster than reading the XML each time

            // 1) Get platform & cpu
            OS = GetCurrentPlatform().ToString().ToLowerInvariant();
            CPU = GetCurrentRuntimeArchitecture().ToString().ToLowerInvariant();

            // 2) Setup MapDictionary
            // For Windows use hardcoded values
            // Why?  This is our development platform and we wanted the fastest start time possible (eliminates XML Load)
            if (OS == "windows" && Optimise)
            {
                MapDictionary = new Dictionary<string, string>
                {
                    { "SDL2", "SDL2.dll" },
                    { "SDL_image", "SDL_image.dll" },
                    { "FAudio", "FAudio.dll" }
                };
            }
            else
            {
                // For every other platform use XML file
                // Read in config XML and only store details we're interested in within MapDictionary
                var xmlPath = Path.Combine(
                    Path.GetDirectoryName(assembly.Location),
                    Path.GetFileNameWithoutExtension(assembly.Location) + ".dll.config"
                );

                if (!File.Exists(xmlPath))
                {
                    Console.WriteLine($"=== Cannot find XML: {xmlPath}");
                    return;
                }

                var root = XElement.Load(xmlPath);

                MapDictionary = new Dictionary<string, string>();
                ParseXml(root, true);  // Direct match on OS & CPU first
                ParseXml(root, false); // Loose match on CPU second (won't allow duplicates)
            }
        }

        private static void ParseXml(XContainer root, bool matchCPU)
        {
            foreach (var el in root.Elements("dllmap"))
            {
                // Ignore entries for other OSs
                if (el.Attribute("os")!.ToString().IndexOf(OS) < 0)
                {
                    continue;
                }

                // Ignore entries for other CPUs
                if (matchCPU)
                {
                    if (el.Attribute("cpu") == null)
                    {
                        continue;
                    }

                    if (el.Attribute("cpu")!.ToString().IndexOf(CPU) < 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (el.Attribute("cpu") != null && el.Attribute("cpu")!.ToString().IndexOf(CPU) < 0)
                    {
                        continue;
                    }
                }

                var oldLib = el.Attribute("dll")!.Value;
                var newLib = el.Attribute("target")!.Value;
                if (string.IsNullOrWhiteSpace(oldLib) || string.IsNullOrWhiteSpace(newLib))
                {
                    continue;
                }

                // Don't allow duplicates
                if (MapDictionary.ContainsKey(oldLib))
                {
                    continue;
                }

                MapDictionary.Add(oldLib, newLib);
            }
        }

        // The callback: which loads the mapped library in place of the original
        private static IntPtr MapAndLoad(string libraryName, Assembly assembly, DllImportSearchPath? dllImportSearchPath)
        {
            string mappedName;
            mappedName = MapLibraryName(libraryName, out mappedName)
                ? mappedName
                : libraryName;
            return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
        }

        // Parse the assembly.xml file, and map the old name to the new name of a library.
        private static bool MapLibraryName(string originalLibName, out string mappedLibName) => MapDictionary.TryGetValue(originalLibName, out mappedLibName);

        // Below pinched from Mono.DllMap project:   https://github.com/Firwood-Software/AdvancedDLSupport/tree/1b7394211a655b2f77649ce3b610a3161215cbdc/Mono.DllMap
        private static DllMapOS GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return DllMapOS.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return DllMapOS.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return DllMapOS.OSX;
            }

            var operatingDesc = RuntimeInformation.OSDescription.ToUpperInvariant();
            foreach (var system in Enum.GetValues(typeof(DllMapOS))
                .Cast<DllMapOS>()
                .Except(new[] { DllMapOS.Linux, DllMapOS.Windows, DllMapOS.OSX }))
            {
                if (operatingDesc.Contains(system.ToString().ToUpperInvariant()))
                {
                    return system;
                }
            }

            throw new PlatformNotSupportedException($"Couldn't detect platform: {RuntimeInformation.OSDescription}");
        }

        private static DllMapArchitecture GetCurrentRuntimeArchitecture()
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm:
                    return DllMapArchitecture.ARM;
                case Architecture.X64:
                    return DllMapArchitecture.x86_64;
                case Architecture.X86:
                    return DllMapArchitecture.x86;
            }

            typeof(object).Module.GetPEKind(out _, out var machine);
            switch (machine)
            {
                case ImageFileMachine.I386:
                    {
                        return DllMapArchitecture.x86;
                    }
                case ImageFileMachine.AMD64:
                    {
                        return DllMapArchitecture.x86_64;
                    }
                case ImageFileMachine.ARM:
                    {
                        return DllMapArchitecture.ARM;
                    }
                case ImageFileMachine.IA64:
                    {
                        return DllMapArchitecture.IA64;
                    }
            }

            throw new PlatformNotSupportedException("Couldn't detect the current architecture.");
        }

        private enum DllMapOS
        {
            Linux = 1 << 0,
            OSX = 1 << 1,
            Solaris = 1 << 2,
            FreeBSD = 1 << 3,
            OpenBSD = 1 << 4,
            NetBSD = 1 << 5,
            Windows = 1 << 6,
            AIX = 1 << 7,
            HPUX = 1 << 8
        }

        private enum DllMapArchitecture
        {
            x86 = 1 << 0,
            x86_64 = 1 << 1,
            SPARC = 1 << 2,
            PPC = 1 << 3,
            S390 = 1 << 4,
            S390X = 1 << 5,
            ARM = 1 << 6,
            ARMV8 = 1 << 7,
            MIPS = 1 << 8,
            Alpha = 1 << 9,
            HPPA = 1 << 10,
            IA64 = 1 << 11
        }
    }

#endif
}
