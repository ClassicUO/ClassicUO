// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using System;

namespace ClassicUO.Assets
{
    /// <summary>
    /// Facade over the asset-manager collaborators. Keeps the existing public
    /// surface (<c>FileManager.BasePath</c>, <c>FileManager.GetUOFilePath</c>,
    /// <c>FileManager.IsUOPInstallation</c>, <c>FileManager.Load(...)</c>) and
    /// the full set of <c>*Loader</c> instances, while delegating to three
    /// cohesive collaborators: <see cref="IUOFilePathResolver"/> resolves
    /// install paths and the UOP/MUL flag, <see cref="IUOAssetLoaderRunner"/>
    /// orchestrates the cold-start load pipeline + verdata patching, and
    /// <see cref="IArtDefReader"/> applies the <c>art.def</c> aliasing table.
    /// </summary>
    public sealed class UOFileManager : IDisposable
    {
        private readonly IUOFilePathResolver _paths;
        private readonly IUOAssetLoaderRunner _runner;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public UOFileManager(ClientVersion clientVersion, string uoPath) : this(clientVersion, uoPath, new UOFilesOverrideMap())
        {
        }

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public UOFileManager(ClientVersion clientVersion, string uoPath, UOFilesOverrideMap overrideMap)
            : this(clientVersion, new UOFilePathResolver(clientVersion, uoPath, overrideMap), new UOAssetLoaderRunner(new ArtDefReader()))
        {
        }

        /// <summary>Full DI seam — inject all collaborators.</summary>
        internal UOFileManager(ClientVersion clientVersion, IUOFilePathResolver paths, IUOAssetLoaderRunner runner)
        {
            Version = clientVersion;
            _paths = paths;
            _runner = runner;

            Animations = new AnimationsLoader(this);
            AnimData = new AnimDataLoader(this);
            Arts = new ArtLoader(this);
            Maps = new MapLoader(this);
            Clilocs = new ClilocLoader(this);
            Gumps = new GumpsLoader(this);
            Fonts = new FontsLoader(this);
            Hues = new HuesLoader(this);
            TileData = new TileDataLoader(this);
            Multis = new MultiLoader(this);
            Skills = new SkillsLoader(this);
            Texmaps = new TexmapsLoader(this);
            Speeches = new SpeechesLoader(this);
            Lights = new LightsLoader(this);
            Sounds = new SoundsLoader(this);
            MultiMaps = new MultiMapLoader(this);
            Verdata = new VerdataLoader(this);
            Professions = new ProfessionLoader(this);
            TileArt = new TileArtLoader(this);
            StringDictionary = new StringDictionaryLoader(this);
        }

        public ClientVersion Version { get; }
        public string BasePath => _paths.BasePath;
        public bool IsUOPInstallation => _paths.IsUOPInstallation;

        /// <summary>Internal seam exposing the path resolver for collaborators.</summary>
        internal IUOFilePathResolver PathResolver => _paths;

        public AnimationsLoader Animations { get; }
        public AnimDataLoader AnimData { get; }
        public ArtLoader Arts { get; }
        public MapLoader Maps { get; set; }
        public ClilocLoader Clilocs { get; }
        public GumpsLoader Gumps { get; }
        public FontsLoader Fonts { get; }
        public HuesLoader Hues { get; }
        public TileDataLoader TileData { get; }
        public MultiLoader Multis { get; }
        public SkillsLoader Skills { get; }
        public TexmapsLoader Texmaps { get; }
        public SpeechesLoader Speeches { get; }
        public LightsLoader Lights { get; }
        public SoundsLoader Sounds { get; }
        public MultiMapLoader MultiMaps { get; }
        public VerdataLoader Verdata { get; }
        public ProfessionLoader Professions { get; }
        public TileArtLoader TileArt { get; }
        public StringDictionaryLoader StringDictionary { get; }


        public void Dispose()
        {
            Animations.Dispose();
            AnimData.Dispose();
            Arts.Dispose();
            Maps.Dispose();
            Clilocs.Dispose();
            Gumps.Dispose();
            Fonts.Dispose();
            Hues.Dispose();
            TileData.Dispose();
            Multis.Dispose();
            Skills.Dispose();
            Texmaps.Dispose();
            Speeches.Dispose();
            Lights.Dispose();
            Sounds.Dispose();
            MultiMaps.Dispose();
            Verdata.Dispose();
            Professions.Dispose();
            TileArt.Dispose();
            StringDictionary.Dispose();
        }

        public string GetUOFilePath(string file) => _paths.GetUOFilePath(file);

        public void Load(bool useVerdata, string lang, string mapsLayouts = "") => _runner.Run(this, useVerdata, lang, mapsLayouts);
    }
}
