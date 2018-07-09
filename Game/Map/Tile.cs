using ClassicUO.Game.WorldObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Map
{
    public sealed class Tile : WorldObject
    {
        private readonly List<WorldObject> _objectsOnTile;

        public Tile()
        {
            _objectsOnTile = new List<WorldObject>();         
        }

        public Graphic TileID { get; set; }
        public IReadOnlyList<WorldObject> ObjectsOnTiles => _objectsOnTile;
        public override Position Position { get; set; }


        public void AddWorldObject(in WorldObject obj)
        {
            _objectsOnTile.Add(obj);
        }

        public void RemoveWorldObject(in WorldObject obj)
        {
            _objectsOnTile.Remove(obj);
        }

        public void Clear()
        {
            _objectsOnTile.Clear();
            DisposeView();
        }

        // create view only when TileID is initialized
        protected override WorldRenderObject CreateView()
            => TileID <= 0 ? null : new TileView(this);

        public new TileView ViewObject => (TileView)base.ViewObject;
    }

    public class TileView : WorldRenderObject
    {
        public TileView(in Tile tile) : base(tile)
        {
            var landData = AssetsLoader.TileData.LandData[tile.TileID];
            Ticks = tile.Position.Z * 4;

            IsStretched = !(landData.TexID <= 0 && (landData.Flags & 0x00000080) != 0);

            if (IsStretched)
            {
                Texture = TextureManager.GetOrCreateTexmapTexture(landData.TexID);
            }
            else
            {
                Texture = TextureManager.GetOrCreateLandTexture(tile.TileID);
                Bounds = new Rectangle(0, Ticks, 44, 44);
            }

        }

        public int Ticks { get; }
        public bool IsStretched { get; }


        private SpriteVertex[] _vertexBufferAlternate = {
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(0, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(1, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(0, 1, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(1, 1, 0))
        };

        Vector3 _vertex0_yOffset, _vertex1_yOffset, _vertex2_yOffset, _vertex3_yOffset;
        private readonly Vector3[] _normals = new Vector3[4];
        Surroundings _SurroundingTiles;

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (_MustUpdateSurroundings)
            {
                UpdateStreched(World.Map);
                _MustUpdateSurroundings = false;
            }

            if (!IsStretched)
                return base.Draw(spriteBatch, position);
            return Draw3DStretched(spriteBatch, position);
        }

        static Point[] kSurroundingsIndexes = {
            new Point(0, -1), new Point(1, -1),
            new Point(-1, 0), new Point(1, 0), new Point(2, 0),
            new Point(-1, 1), new Point(0, 1), new Point(1, 1), new Point(2, 1),
            new Point(0, 2), new Point(1, 2) };
        bool _MustUpdateSurroundings = true;


        private bool Draw3DStretched(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            _vertexBufferAlternate[0].Position = position + _vertex0_yOffset;
            _vertexBufferAlternate[1].Position = position + _vertex1_yOffset;
            _vertexBufferAlternate[2].Position = position + _vertex2_yOffset;
            _vertexBufferAlternate[3].Position = position + _vertex3_yOffset;

            if (!spriteBatch.DrawSprite(Texture, _vertexBufferAlternate))
            {
                return false;
            }
            return true;
        }


        private void UpdateStreched(in Facet map)
        {
            float[] surroundingTilesZ = new float[kSurroundingsIndexes.Length];
            for (int i = 0; i < kSurroundingsIndexes.Length; i++)
                surroundingTilesZ[i] = map.GetTileZ((short)(WorldObject.Position.X + kSurroundingsIndexes[i].X), (short)(WorldObject.Position.Y + kSurroundingsIndexes[i].Y));

            _SurroundingTiles = new Surroundings(
              surroundingTilesZ[7], surroundingTilesZ[3], surroundingTilesZ[6]);

            bool isFlat = _SurroundingTiles.IsFlat && _SurroundingTiles.East == WorldObject.Position.Z;
            if (!isFlat)
            {
                int low = 0, high = 0, sort = 0;
                sort = map.GetAverageZ(WorldObject.Position.Z, (int)_SurroundingTiles.South, (int)_SurroundingTiles.East, (int)_SurroundingTiles.Down, ref low, ref high);
                if (sort != SortZ)
                {
                    SortZ = (sbyte)sort;
                    map.GetTile((short)WorldObject.Position.X, (short)WorldObject.Position.Y)/*.ForceSort()*/;
                }
            }

            _normals[0] = CalculateNormal(
                surroundingTilesZ[2], surroundingTilesZ[3],
                surroundingTilesZ[0], surroundingTilesZ[6]);
            _normals[1] = CalculateNormal(
                WorldObject.Position.Z, surroundingTilesZ[4],
                surroundingTilesZ[1], surroundingTilesZ[7]);
            _normals[2] = CalculateNormal(
                surroundingTilesZ[5], surroundingTilesZ[7],
                WorldObject.Position.Z, surroundingTilesZ[9]);
            _normals[3] = CalculateNormal(
                surroundingTilesZ[6], surroundingTilesZ[8],
                surroundingTilesZ[3], surroundingTilesZ[10]);

            updateVertexBuffer();
        }

        void updateVertexBuffer()
        {
            _vertex0_yOffset = new Vector3(22, -(WorldObject.Position.Z * 4), 0);
            _vertex1_yOffset = new Vector3(44f, 22 - (_SurroundingTiles.East * 4), 0);
            _vertex2_yOffset = new Vector3(0, 22 - (_SurroundingTiles.South * 4), 0);
            _vertex3_yOffset = new Vector3(22, 44f - (_SurroundingTiles.Down * 4), 0);

            _vertexBufferAlternate[0].Normal = _normals[0];
            _vertexBufferAlternate[1].Normal = _normals[1];
            _vertexBufferAlternate[2].Normal = _normals[2];
            _vertexBufferAlternate[3].Normal = _normals[3];

            //Vector3 hue = Renderer.RenderExtentions.GetHueVector(WorldObject.Hue);
            //if (_vertexBufferAlternate[0].Hue != hue)
            //{
            //    _vertexBufferAlternate[0].Hue =
            //    _vertexBufferAlternate[1].Hue =
            //    _vertexBufferAlternate[2].Hue =
            //    _vertexBufferAlternate[3].Hue = hue;
            //}
        }


        private Vector3 CalculateNormal(in float a, in float b, in float c, in float d)
        {
            Vector3 v = new Vector3(a - b, 1f, c - d);
            v.Normalize();
            return v;
        }

        class Surroundings
        {
            public float Down;
            public float East;
            public float South;

            public Surroundings(float down, float east, float south)
            {
                Down = down;
                East = east;
                South = south;
            }

            public bool IsFlat
            {
                get
                {
                    return (Down == East && East == South);
                }
            }
        }
    }
}
