using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace ManifestCreator
{
    class Program
    {
        private static readonly MD5 _md5 = MD5.Create();

        private static string[] _file_list =
        {
            "ClassicUO.exe",
            "ClassicUO",
            "ClassicUO.bin.osx",
            "ClassicUO.bin.x86_64",
            //"ClassicUO.exe.config",  
            //"ClassicUO.pdb",                    // to verify
            "cuoapi.dll",
            "FNA.dll",
            "FNA.dll.config",
            "monoconfig",
            "monomachineconfig",
            "netstandard.dll",

            // Data



            // libs

            // linux
            "lib64/libFAudio.so.0",
            "lib64/libFNA3D.so.0",
            "lib64/libSDL2-2.0.so.0",
            "lib64/libtheorafile.so",

            // osx
            "osx/libFAudio.0.dylib",
            "osx/libFNA3D.0.dylib",
            "osx/libSDL2-2.0.0.dylib",
            "osx/libtheorafile.dylib",
            "osx/libMoltenVK.dylib",
            "osx/libvulkan.1.dylib",
            "vulkan/icd.d/MoltenVK_icd.json",

            // windows
            "x64/FAudio.dll",
            "x64/FNA3D.dll",
            "x64/SDL2.dll",
            "x64/zlib.dll",
            "x64/libtheorafile.dll",
            "x64/vcruntime140.dll",

            // dlls
            "Accessibility.dll",
            "Microsoft.CSharp.dll",
            "Mono.Posix.dll",
            "Mono.Security.dll",
            "mscorlib.dll",
            "MP3Sharp.dll",
            "System.Configuration.dll",
            "System.Core.dll",
            "System.Data.dll",
            "System.dll",
            "System.Drawing.dll",
            "System.IO.Compression.dll",
            "System.IO.Compression.FileSystem.dll",
            "System.Numerics.dll",
            "System.Runtime.Serialization.dll",
            "System.Windows.Forms.dll",
            "System.Xml.dll",
            "System.Xml.Linq.dll",

            "System.Buffers.dll",
            "System.Memory.dll",
            "System.Numerics.Vectors.dll",
            "System.Runtime.CompilerServices.Unsafe.dll",
            "System.Text.Encoding.CodePages.dll",

            // removed.
            //"Newtonsoft.Json.dll"
        };


        static void Main(string[] args)
        {
            string cuopath = args[0];
            Console.WriteLine("CUOPATH: {0}", cuopath);
          
            string new_version = args[1];
            Console.WriteLine("VERSION: {0}", new_version);

            string name = args[2];
            Console.WriteLine("NAME: {0}", name);


            Console.WriteLine("Starting to create manifest");
            List<ManifestRelease> releases = CreateManifestReleaseList(cuopath, new_version, name);

            Stream stream = CreateManfest(releases);

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                stream.Seek(0, SeekOrigin.Begin);

                File.WriteAllText("manifest.xml", reader.ReadToEnd(), Encoding.UTF8);
            }

            Console.WriteLine("Manifest created!");
        }

        private static List<ManifestRelease> CreateManifestReleaseList(string cuo_path, string version, string name)
        {
            List<ManifestRelease> list = new List<ManifestRelease>();

            DirectoryInfo dir = new DirectoryInfo(cuo_path);

            if (dir.Exists)
            {
                ManifestRelease release = new ManifestRelease()
                {
                    version = version,
                    name = name
                };

                foreach (var f in _file_list)
                {
                    string path = Path.Combine(cuo_path, f);

                    if (File.Exists(path))
                    {
                        var hash_file = new HashFile()
                        {
                            filename = f,
                            hash = CalculateMD5(path)
                        };
                        release.files.Add(hash_file);

                        Console.WriteLine(hash_file);
                    }
                }

                //var files = dir.GetFiles("*.*", SearchOption.AllDirectories).Where(s => Path.HasExtension("exe"));

                
                //foreach (FileInfo file in files)
                //{
                //    var hash_file = new HashFile()
                //    {
                //        filename = file.FullName.Replace(cuo_path, ""),
                //        hash = CalculateMD5(file.FullName)
                //    };
                //    release.files.Add(hash_file);

                //    Console.WriteLine(hash_file);
                //}

                list.Add(release);
            }

            return list;
        }

        private static Stream CreateManfest(List<ManifestRelease> releases)
        {
            MemoryStream stream = new MemoryStream();

            XmlTextWriter xml = new XmlTextWriter(stream, Encoding.UTF8)
            {
                Formatting = Formatting.Indented, IndentChar = '\t', Indentation = 1
            };

            xml.WriteStartDocument(true);
            xml.WriteStartElement("releases");

            foreach (ManifestRelease manifestRelease in releases)
            {
                manifestRelease.Save(xml);
            }

            xml.WriteEndElement();
            xml.WriteEndDocument();

            xml.Flush();

            return stream;
        }

        private static string CalculateMD5(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                byte[] hash = _md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }


        class HashFile
        {
            public string filename;
            public string hash;

            public void Save(XmlWriter xml)
            {
                xml.WriteStartElement("file");
                xml.WriteAttributeString("filename", filename);
                xml.WriteAttributeString("hash", hash);
                xml.WriteEndElement();
            }

            public override string ToString()
            {
                return $"{filename} - {hash}";
            }
        }

        class ManifestRelease
        {
            public string version = "";
            public string name = "";
            public List<HashFile> files = new List<HashFile>();

            public void Save(XmlWriter xml)
            {
                xml.WriteStartElement("release");
                xml.WriteAttributeString("name", name);
                xml.WriteAttributeString("version", version);

                xml.WriteStartElement("files");
                foreach (HashFile file in files)
                {
                    file.Save(xml);
                }
                xml.WriteEndElement();

                xml.WriteEndElement();
            }
        }
    }
}
