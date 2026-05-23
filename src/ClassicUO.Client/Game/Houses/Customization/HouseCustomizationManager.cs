// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Houses.Customization
{
    internal struct CustomBuildObject
    {
        public CustomBuildObject(ushort graphic)
        {
            Graphic = graphic;
            X = Y = Z = 0;
        }

        public ushort Graphic;
        public int X, Y, Z;
    }

    /// <summary>
    /// Facade over the customization-mode collaborators: keeps the existing
    /// public surface (<c>_world.CustomHouseManager.X</c>) and delegates to
    /// three cohesive classes. Definition-file ingestion + table lookups live
    /// in <see cref="IHouseDataLoader"/>, placement rules in
    /// <see cref="IHouseValidator"/>, and the actual build / erase / preview
    /// loop in <see cref="IHouseBuilder"/>. The facade still owns the flat
    /// state (selected graphic, current floor, vision flags, etc.) so the
    /// many UI call sites that read those fields directly keep working.
    /// </summary>
    internal sealed class HouseCustomizationManager
    {
        public readonly List<CustomHouseWallCategory> Walls = new List<CustomHouseWallCategory>();
        public readonly List<CustomHouseFloor> Floors = new List<CustomHouseFloor>();
        public readonly List<CustomHouseDoor> Doors = new List<CustomHouseDoor>();
        public readonly List<CustomHouseMiscCategory> Miscs = new List<CustomHouseMiscCategory>();
        public readonly List<CustomHouseStair> Stairs = new List<CustomHouseStair>();
        public readonly List<CustomHouseTeleport> Teleports = new List<CustomHouseTeleport>();
        public readonly List<CustomHouseRoofCategory> Roofs = new List<CustomHouseRoofCategory>();
        public readonly List<CustomHousePlaceInfo> ObjectsInfo = new List<CustomHousePlaceInfo>();

        private readonly World _world;
        private readonly IHouseDataLoader _data;
        private readonly IHouseValidator _validator;
        private readonly IHouseBuilder _builder;
        private Rectangle _bounds;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public HouseCustomizationManager(World world, uint serial)
            : this(world, serial, w => new HouseDataLoader(w))
        {
        }

        /// <summary>Test / extension seam — inject a custom data loader factory.</summary>
        internal HouseCustomizationManager(World world, uint serial, Func<World, IHouseDataLoader> dataLoaderFactory)
        {
            _world = world;
            Serial = serial;

            _data = dataLoaderFactory(world);
            _validator = new HouseValidator(this, _data, world);
            _builder = new HouseBuilder(this, _validator, world);

            // TODO: don't load the file txt every time the housemanager get initialized
            _data.Load(Walls, Floors, Doors, Miscs, Stairs, Teleports, Roofs, ObjectsInfo);

            InitializeHouse();
        }

        public int Category = -1, MaxPage = 1, CurrentFloor = 1, FloorCount = 4, RoofZ = 1, MinHouseZ = -120, Components, Fixtures, MaxComponets, MaxFixtures;
        public bool Erasing, SeekTile, ShowWindow, CombinedStair;


        public readonly int[] FloorVisionState = new int[4];


        public ushort SelectedGraphic;

        public readonly uint Serial;
        public Point StartPos, EndPos;
        public CUSTOM_HOUSE_GUMP_STATE State = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;

        /// <summary>Foundation bounding rectangle, exposed to collaborators that need to validate / iterate the plot.</summary>
        internal Rectangle Bounds => _bounds;

        /// <summary>Data-loader / lookup collaborator, exposed so the builder can resolve graphics without re-implementing search.</summary>
        internal IHouseDataLoader DataLoader => _data;

        private void InitializeHouse()
        {
            Item foundation = _world.Items.Get(Serial);

            if (foundation == null)
            {
                return;
            }

            MinHouseZ = foundation.Z + 7;

            if (foundation.MultiInfo.HasValue)
            {
                var multi = foundation.MultiInfo.Value;

                StartPos.X = foundation.X + multi.X + 1;
                StartPos.Y = foundation.Y + multi.Y + 1;
                EndPos.X = foundation.X + multi.Width + 1;
                EndPos.Y = foundation.Y + multi.Height + 1;
            }

            int width = Math.Abs(EndPos.X - StartPos.X);
            int height = Math.Abs(EndPos.Y - StartPos.Y);

            _bounds = new Rectangle(StartPos.X - 1, StartPos.Y - 1, width + 1, height + 2);

            if (width >= 13 || height >= 13)
            {
                FloorCount = 4;
            }
            else
            {
                FloorCount = 3;
            }

            int plotWidth = width + 1;
            int plotHeight = height + 1;
            int componentsOnFloor = (plotWidth - 1) * (plotHeight - 1);

            MaxComponets = FloorCount * (componentsOnFloor + 2 * (plotWidth + plotHeight) - 4) - (int) (FloorCount * componentsOnFloor * -0.25) + 2 * plotWidth + 3 * plotHeight - 5;

            MaxFixtures = MaxComponets / 20;
        }

        // ---- Builder facade ----
        public void GenerateFloorPlace() => _builder.GenerateFloorPlace();
        public void OnTargetWorld(GameObject place) => _builder.OnTargetWorld(place);
        public void SetTargetMulti() => _builder.SetTargetMulti();

        // ---- Validator facade ----
        public bool CanBuildHere(List<CustomBuildObject> list, out CUSTOM_HOUSE_BUILD_TYPE type) => _validator.CanBuildHere(list, out type);
        public bool CanEraseHere(GameObject place, out CUSTOM_HOUSE_BUILD_TYPE type) => _validator.CanEraseHere(place, out type);
        public (int, int) ExistsInList(ref CUSTOM_HOUSE_GUMP_STATE state, ushort graphic) => _validator.ExistsInList(ref state, graphic);
        public bool ValidateItemPlace(Item foundationItem, Multi item, int minZ, int maxZ, List<Point> validatedFloors) => _validator.ValidateItemPlace(foundationItem, item, minZ, maxZ, validatedFloors);
        public bool ValidatePlaceStructure(Item foundationItem, House house, IEnumerable<Multi> multi, int minZ, int maxZ, int flags) => _validator.ValidatePlaceStructure(foundationItem, house, multi, minZ, maxZ, flags);
    }

    internal enum CUSTOM_HOUSE_GUMP_STATE
    {
        CHGS_WALL = 0,
        CHGS_DOOR,
        CHGS_FLOOR,
        CHGS_STAIR,
        CHGS_ROOF,
        CHGS_MISC,
        CHGS_MENU,
        CHGS_FIXTURE
    }

    internal enum CUSTOM_HOUSE_FLOOR_VISION_STATE
    {
        CHGVS_NORMAL = 0,
        CHGVS_TRANSPARENT_CONTENT,
        CHGVS_HIDE_CONTENT,
        CHGVS_TRANSPARENT_FLOOR,
        CHGVS_HIDE_FLOOR,
        CHGVS_TRANSLUCENT_FLOOR,
        CHGVS_HIDE_ALL
    }

    internal enum CUSTOM_HOUSE_BUILD_TYPE
    {
        CHBT_NORMAL = 0,
        CHBT_ROOF,
        CHBT_FLOOR,
        CHBT_STAIR
    }

    [Flags]
    internal enum CUSTOM_HOUSE_MULTI_OBJECT_FLAGS
    {
        CHMOF_GENERIC_INTERNAL = 0x01,
        CHMOF_FLOOR = 0x02,
        CHMOF_STAIR = 0x04,
        CHMOF_ROOF = 0x08,
        CHMOF_FIXTURE = 0x10,
        CHMOF_TRANSPARENT = 0x20,
        CHMOF_IGNORE_IN_RENDER = 0x40,
        CHMOF_VALIDATED_PLACE = 0x80,
        CHMOF_INCORRECT_PLACE = 0x100,

        CHMOF_DONT_REMOVE = 0x200,
        CHMOF_PREVIEW = 0x400
    }

    [Flags]
    internal enum CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS
    {
        CHVCF_TOP = 0x01,
        CHVCF_BOTTOM = 0x02,
        CHVCF_N = 0x04,
        CHVCF_E = 0x08,
        CHVCF_S = 0x10,
        CHVCF_W = 0x20,
        CHVCF_DIRECT_SUPPORT = 0x40,
        CHVCF_CANGO_W = 0x80,
        CHVCF_CANGO_N = 0x100
    }
}
