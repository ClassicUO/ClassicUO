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
            "ClassicUO.exe.config",  
            //"ClassicUO.pdb",                    // to verify
            "cuoapi.dll",
            "FNA.dll",
            "FNA.dll.config",
            "monoconfig",
            "monomachineconfig",


            // Data



            // libs
            "libs/api/cuoapi.dll",

            "libs/lib64/libFAudio.so.0",
            "libs/lib64/libbdiplus.so",
            "libs/lib64/libmojoshader.so",
            "libs/lib64/libMonoPosixHelper.so",
            "libs/lib64/libMonoSupportW.so",
            "libs/lib64/libSDL2-2.0.so.0",
            "libs/lib64/libSDL2_image-2.0.so.0",
            "libs/lib64/libtheorafile.so",
            "libs/lib64/libz.so",

            "libs/osx/libFAudio.0.dylib",
            "libs/osx/libmojoshader.dylib",
            "libs/osx/libMonoPosixHelper.dylib",
            "libs/osx/libMonoSupportW.dylib",
            "libs/osx/lib/libSDL2-2.0.0.dylib",
            "libs/osx/libSDL2_image-2.0.0.dylib",
            "libs/osx/libtheorafile.dylib",

            "libs/x64/FAudio.dll",
            "libs/x64/MojoShader.dll",
            "libs/x64/SDL2.dll",
            "libs/x64/SDL2_image.dll",
            "libs/x64/zlib.dll",
            "libs/x64/libtheorafile.dll",
            "libs/x64/vcruntime140.dll",

            // dlls
            "Accessibility.dll",
            "Microsoft.CSharp.dll",
            "Mono.Posix.dll",
            "Mono.Security.dll",
            "mscorlib.dll",
            "Newtonsoft.Json.dll",
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
            "System.Xml.dll"
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
