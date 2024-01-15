#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2024 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#if NET7_0_OR_GREATER

#region Using Statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Xml;
#endregion

namespace ClassicUO
{
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
    internal static class DllMap
    {
        #region Private Static Variables

        private static readonly Dictionary<string, string> _mapDict = new Dictionary<string, string>();
=======
    internal static class FNADllMap
=======
#if !NETFRAMEWORK
    // only works in .NET Core. disable in .NET framework
    public static class DllMap
>>>>>>> fixed order
=======
    internal static class DllMap
>>>>>>> appconfig
    {
        #region Private Static Variables

<<<<<<< HEAD
<<<<<<< HEAD
        private static Dictionary<string, string> mapDictionary
            = new Dictionary<string, string>();
>>>>>>> update dllmap
=======
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void AddDllDirectory(string lpPathName);
>>>>>>> fixed order
=======
        private static readonly Dictionary<string, string> _mapDict = new Dictionary<string, string>();
>>>>>>> appconfig

        #endregion

        #region Private Static Methods

        private static string GetPlatformName()
        {
<<<<<<< HEAD
<<<<<<< HEAD
            string mappedName;
<<<<<<< HEAD
            if (!_mapDict.TryGetValue(libraryName, out mappedName))
=======
            if (!mapDictionary.TryGetValue(libraryName, out mappedName))
>>>>>>> update dllmap
=======
            Optimise = optimise;

            // Our executable needs to know how to find the native libraries
            // For Windows, we can set this to be x86 or x64 directory at runtime (below)
            // For Linux we need to move our native libraries to 'netcoredeps' which is set by .net core
            // For OSX we need to set an environment variable (DYLD_LIBRARY_PATH) outside of the process by a script
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
>>>>>>> fixed order
=======
            if (OperatingSystem.IsWindows())
            {
                return "windows";
            }
            else if (OperatingSystem.IsMacOS())
            {
                return "osx";
            }
            else if (OperatingSystem.IsLinux())
            {
                return "linux";
            }
            else if (OperatingSystem.IsFreeBSD())
            {
                return "freebsd";
            }
            else
            {
                // What is this platform??
                return "unknown";
            }
        }

        #endregion

        #region DllImportResolver Callback Methods

        private static IntPtr MapAndLoad(
            string libraryName,
            Assembly assembly,
            DllImportSearchPath? dllImportSearchPath
        )
        {
            string mappedName;
            if (!_mapDict.TryGetValue(libraryName, out mappedName))
>>>>>>> appconfig
            {
                mappedName = libraryName;
            }

            return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
        }

        private static IntPtr LoadStaticLibrary(
            string libraryName,
            Assembly assembly,
            DllImportSearchPath? dllImportSearchPath
        )
        {
<<<<<<< HEAD
<<<<<<< HEAD
            return NativeLibrary.GetMainProgramHandle();
        }
<<<<<<< HEAD

        #endregion

        public static void Init(Assembly assembly)
=======
=======
            NativeLibrary.SetDllImportResolver(assembly, MapAndLoad);
>>>>>>> fixed order

            // Do setup so that MapLibraryName is faster than reading the XML each time
=======
            return NativeLibrary.GetMainProgramHandle();
        }
>>>>>>> appconfig

        #endregion

<<<<<<< HEAD
<<<<<<< HEAD
        [ModuleInitializer]
        public static void Init()
>>>>>>> update dllmap
        {
            if (!RuntimeFeature.IsDynamicCodeCompiled)
=======
            // 2) Setup MapDictionary
            // For Windows use hardcoded values
            // Why?  This is our development platform and we wanted the fastest start time possible (eliminates XML Load)
            if (OS == "windows" && Optimise)
>>>>>>> fixed order
            {
                MapDictionary = new Dictionary<string, string>
                {
<<<<<<< HEAD
                    NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), LoadStaticLibrary);
<<<<<<< HEAD
                    return;
                }

                //return;
=======
                }

                return;
>>>>>>> update dllmap
=======
                    { "SDL2", "SDL2.dll" },
                    { "SDL_image", "SDL_image.dll" },
                    { "FAudio", "FAudio.dll" }
                };
>>>>>>> fixed order
            }
            else
=======
        public static void Init(Assembly assembly)
        {
            if (!RuntimeFeature.IsDynamicCodeCompiled)
>>>>>>> appconfig
            {
                /* NativeAOT platforms don't perform dynamic loading,
				 * so setting a DllImportResolver is unnecessary.
				 *
				 * However, iOS and tvOS with Mono AOT statically link
				 * their dependencies, so we need special handling for them.
				 */
                if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
                {
                    NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), LoadStaticLibrary);
                    return;
                }

<<<<<<< HEAD
<<<<<<< HEAD
=======
                //return;
            }

>>>>>>> appconfig
            // Get the platform and architecture
            string os = GetPlatformName();
            string cpu = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            string wordsize = (IntPtr.Size * 8).ToString();
<<<<<<< HEAD
<<<<<<< HEAD
=======

            // Get the executing assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
>>>>>>> update dllmap

            // Locate the config file
            string xmlPath = Path.Combine(
                AppContext.BaseDirectory,
                assembly.GetName().Name + ".dll.config"
            );
<<<<<<< HEAD

=======
>>>>>>> update dllmap
            if (!File.Exists(xmlPath))
            {
                // Let's hope for the best...
                return;
=======
                var root = XElement.Load(xmlPath);

                MapDictionary = new Dictionary<string, string>();
                ParseXml(root, true);  // Direct match on OS & CPU first
                ParseXml(root, false); // Loose match on CPU second (won't allow duplicates)
>>>>>>> fixed order
=======

            // Locate the config file
            string xmlPath = Path.Combine(
                AppContext.BaseDirectory,
                assembly.GetName().Name + ".dll.config"
            );

            if (!File.Exists(xmlPath))
            {
                // Let's hope for the best...
                return;
>>>>>>> appconfig
            }

            // Load the XML
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            // The NativeLibrary API cannot remap function names. :(
            if (xmlDoc.GetElementsByTagName("dllentry").Count > 0)
            {
                string msg = "Function remapping is not supported by .NET Core. Ignoring dllentry elements...";
                Console.WriteLine(msg);

                // Log it in the debugger for non-console apps.
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(msg);
                }
<<<<<<< HEAD
<<<<<<< HEAD
            }
<<<<<<< HEAD

            // Parse the XML into a mapping dictionary
            foreach (XmlNode node in xmlDoc.GetElementsByTagName("dllmap"))
            {
                XmlAttribute attribute;

=======

            // Parse the XML into a mapping dictionary
            foreach (XmlNode node in xmlDoc.GetElementsByTagName("dllmap"))
            {
                XmlAttribute attribute;

>>>>>>> update dllmap
                // Check the OS
                attribute = node.Attributes["os"];
                if (attribute != null)
=======

                // Ignore entries for other CPUs
                if (matchCPU)
>>>>>>> fixed order
=======
            }

            // Parse the XML into a mapping dictionary
            foreach (XmlNode node in xmlDoc.GetElementsByTagName("dllmap"))
            {
                XmlAttribute attribute;

                // Check the OS
                attribute = node.Attributes["os"];
                if (attribute != null)
>>>>>>> appconfig
                {
                    bool containsOS = attribute.Value.Contains(os);
                    bool invert = attribute.Value.StartsWith("!");
                    if ((!containsOS && !invert) || (containsOS && invert))
                    {
                        continue;
                    }
                }

                // Check the CPU
                attribute = node.Attributes["cpu"];
                if (attribute != null)
                {
                    bool containsCPU = attribute.Value.Contains(cpu);
                    bool invert = attribute.Value.StartsWith("!");
                    if ((!containsCPU && !invert) || (containsCPU && invert))
                    {
                        continue;
                    }
                }

                // Check the word size
                attribute = node.Attributes["wordsize"];
                if (attribute != null)
                {
                    bool containsWordsize = attribute.Value.Contains(wordsize);
                    bool invert = attribute.Value.StartsWith("!");
                    if ((!containsWordsize && !invert) || (containsWordsize && invert))
                    {
                        continue;
                    }
                }

                // Check for the existence of 'dll' and 'target' attributes
                XmlAttribute dllAttribute = node.Attributes["dll"];
                XmlAttribute targetAttribute = node.Attributes["target"];
                if (dllAttribute == null || targetAttribute == null)
                {
                    continue;
                }

                // Get the actual library names
                string oldLib = dllAttribute.Value;
                string newLib = targetAttribute.Value;
                if (string.IsNullOrWhiteSpace(oldLib) || string.IsNullOrWhiteSpace(newLib))
                {
                    continue;
                }

<<<<<<< HEAD
<<<<<<< HEAD
                // Don't allow duplicates
<<<<<<< HEAD
                if (_mapDict.ContainsKey(oldLib))
=======
                if (mapDictionary.ContainsKey(oldLib))
>>>>>>> update dllmap
=======
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
>>>>>>> fixed order
=======
                // Don't allow duplicates
                if (_mapDict.ContainsKey(oldLib))
>>>>>>> appconfig
                {
                    continue;
                }

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
                _mapDict.Add(oldLib, newLib);
=======
                mapDictionary.Add(oldLib, newLib);
>>>>>>> update dllmap
=======
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
>>>>>>> fixed order
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
<<<<<<< HEAD
=======

<<<<<<< HEAD
        #endregion
>>>>>>> update dllmap
=======
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
=======
                _mapDict.Add(oldLib, newLib);
            }

            // Set the resolver callback
            NativeLibrary.SetDllImportResolver(assembly, MapAndLoad);
>>>>>>> appconfig
        }
>>>>>>> fixed order
    }
}

#endif // NET7_0_OR_GREATER