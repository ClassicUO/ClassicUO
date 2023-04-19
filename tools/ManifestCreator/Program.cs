using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

using var md5 = MD5.Create();

var ecludingList = new string[]
{
    //"ClassicUO.exe.config"
};


var cuoBinPath = Path.GetFullPath(args[0]);
Console.WriteLine("CUOPATH: {0}", cuoBinPath);

var version = args[1];
Console.WriteLine("VERSION: {0}", version);

var versionName = args[2];
Console.WriteLine("NAME: {0}", versionName);


Console.WriteLine("Starting to create manifest");
var releases = CreateManifestReleaseList(cuoBinPath, version, versionName);
var stream = CreateManfest(releases);

using (var reader = new StreamReader(stream, Encoding.UTF8, true))
{
    stream.Seek(0, SeekOrigin.Begin);

    File.WriteAllText("manifest.xml", reader.ReadToEnd(), Encoding.UTF8);
}

Console.WriteLine("Manifest created!");

List<ManifestRelease> CreateManifestReleaseList(string cuo_path, string version, string name)
{
    var list = new List<ManifestRelease>();
    var dir = new DirectoryInfo(cuo_path);

    if (!dir.Exists)
    {
        return list;
    }

    var release = new ManifestRelease(version, name, new List<HashFile>());

    foreach (var f in dir.GetFiles("*.*", SearchOption.AllDirectories)
        .Where(s => !ecludingList.Contains(s.Name)))
    {
        var path = f.FullName.Remove(0, cuo_path.Length);
        if (path.StartsWith(Path.DirectorySeparatorChar))
        {
            path = path.Remove(0, 1);
        }

        var hash_file = new HashFile(path, CalculateMD5(f.FullName));
        release.Files.Add(hash_file);
        Console.WriteLine(hash_file);
    }

    list.Add(release);

    return list;
}

Stream CreateManfest(List<ManifestRelease> releases)
{
    var stream = new MemoryStream();
    var xml = new XmlTextWriter(stream, Encoding.UTF8)
    {
        Formatting = Formatting.Indented,
        IndentChar = '\t',
        Indentation = 1
    };

    xml.WriteStartDocument(true);
    xml.WriteStartElement("releases");
    releases.ForEach(s => s.Save(xml));
    xml.WriteEndElement();
    xml.WriteEndDocument();

    xml.Flush();

    return stream;
}

string CalculateMD5(string filename)
{
    using (var stream = File.OpenRead(filename))
    {
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}


sealed class HashFile
{
    public HashFile(string fileName, string hash)
    {
        Filename = fileName;
        Hash = hash;
    }

    public string Filename { get; }
    public string Hash { get; }


    public void Save(XmlWriter xml)
    {
        xml.WriteStartElement("file");
        xml.WriteAttributeString("filename", Filename);
        xml.WriteAttributeString("hash", Hash);
        xml.WriteEndElement();
    }

    public override string ToString()
    {
        return $"{Filename} - {Hash}";
    }
}

sealed class ManifestRelease
{
    public ManifestRelease(string version, string name, List<HashFile> files)
    {
        Version = version;
        Name = name;
        Files = files;
    }

    public string Version { get; }
    public string Name { get; }
    public List<HashFile> Files { get; }


    public void Save(XmlWriter xml)
    {
        xml.WriteStartElement("release");
        xml.WriteAttributeString("name", Name);
        xml.WriteAttributeString("version", Version);

        xml.WriteStartElement("files");
        foreach (var file in Files)
        {
            file.Save(xml);
        }
        xml.WriteEndElement();

        xml.WriteEndElement();
    }
}