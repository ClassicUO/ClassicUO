using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ClassicUO
{
    internal static class DllMap
    {
        static readonly Dictionary<string, string> mapDictionary = new Dictionary<string, string>();

        internal static void Init()
        {
            if (!CUOEnviroment.IsUnix)
                return;

            // Get the platform and architecture
            string os = getPlatformName();
            string cpu = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            string wordsize = (IntPtr.Size * 8).ToString();

            // Get the executing assembly
            Assembly assembly = Assembly.GetAssembly(typeof(Microsoft.Xna.Framework.Graphics.ColorWriteChannels));

            // Locate the config file
            string xmlPath = Path.Combine(AppContext.BaseDirectory, "FNA.dll.config");

            if (!File.Exists(xmlPath))
            {
                // Let's hope for the best...
                return;
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
            }

            // Parse the XML into a mapping dictionary
            foreach (XmlNode node in xmlDoc.GetElementsByTagName("dllmap"))
            {
                XmlAttribute attribute;

                // Check the OS
                attribute = node.Attributes["os"];
                if (attribute != null)
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

                // Don't allow duplicates
                if (mapDictionary.ContainsKey(oldLib))
                {
                    continue;
                }

                mapDictionary.Add(oldLib, newLib);
            }

            NativeLibrary.SetDllImportResolver(assembly, mapAndLoad);

            static IntPtr mapAndLoad(
                string libraryName,
                Assembly assembly,
                DllImportSearchPath? dllImportSearchPath
            )
            {
                string mappedName;
                if (!mapDictionary.TryGetValue(libraryName, out mappedName))
                {
                    mappedName = libraryName;
                }

                return NativeLibrary.Load(mappedName, assembly, dllImportSearchPath);
            }

            static string getPlatformName()
            {
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
        }
    }
}
