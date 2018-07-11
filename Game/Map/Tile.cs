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
            _objectsOnTile.Add(this);
        }

        public Graphic TileID { get; set; }
        public IReadOnlyList<WorldObject> ObjectsOnTiles => _objectsOnTile;
        public override Position Position { get; set; }

        public void AddWorldObject(in WorldObject obj)
        {
            _objectsOnTile.Add(obj);

            Sort();
        }

        public void RemoveWorldObject(in WorldObject obj)
        {
            _objectsOnTile.Remove(obj);
        }

        public void Clear()
        {
            _objectsOnTile.Clear();
            _objectsOnTile.Add(this);
            DisposeView();
            TileID = 0;
            Position = new Position(0, 0);
            
        }

        public void Sort()
        {
            
            for (int i = 0; i < _objectsOnTile.Count - 1; i++)
            {
                int j = i + 1;
                while (j > 0)
                {
                    int result = Compare(_objectsOnTile[j - 1], _objectsOnTile[j]);
                    if (result > 0)
                    {
                        WorldObject temp = _objectsOnTile[j - 1];
                        _objectsOnTile[j - 1] = _objectsOnTile[j];
                        _objectsOnTile[j] = temp;
                    }
                    j--;
                }
            }
        }

        private static int Compare(in WorldObject x, in WorldObject y)
        {
            (int xZ, int xType, int xThreshold, int xTierbreaker) = GetSortValues(x);
            (int yZ, int yType, int yThreshold, int yTierbreaker) = GetSortValues(y);

            xZ += xThreshold;
            yZ += yThreshold;

            int comparison = xZ - yZ;
            if (comparison == 0)
                comparison = xType - yType;
            if (comparison == 0)
                comparison = xThreshold - yThreshold;
            if (comparison == 0)
                comparison = xTierbreaker - yTierbreaker;

            return comparison;
        }

        private static (int, int, int, int) GetSortValues(in WorldObject e)
        {
            if (e is Tile tile)
            {
                return (tile.ViewObject.SortZ, 0, 0, 0);
            }
            else if (e is Static staticitem)
            {
                var itemdata = AssetsLoader.TileData.StaticData[staticitem.TileID];

                return (staticitem.Position.Z, 1, (itemdata.Height > 0 ? 1 : 0) + ((itemdata.Flags & 0x00000001) != 0 ? 0 : 1), staticitem.Index);
            }
            
            return (0, 0, 0, 0);        
        }

        // create view only when TileID is initialized
        protected override WorldRenderObject CreateView()
            => TileID <= 0 ? null : new TileView(this);

        public new TileView ViewObject => (TileView)base.ViewObject;
    }



    public class TileView : WorldRenderObject
    {
        private Vector3 _vertex0_yOffset, _vertex1_yOffset, _vertex2_yOffset, _vertex3_yOffset;
        private readonly Vector3[] _normals = new Vector3[4];
        private readonly SpriteVertex[] _vertexBufferAlternate = 
        {
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(0, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(1, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(0, 1, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(1, 1, 0))
        };
        //private Surroundings _SurroundingTiles;

        private static Point[] _surroundingIndexes = 
        {
            new Point(0, -1), new Point(1, -1),
            new Point(-1, 0), new Point(1, 0), new Point(2, 0),
            new Point(-1, 1), new Point(0, 1), new Point(1, 1), new Point(2, 1),
            new Point(0, 2), new Point(1, 2)
        };

        // 6, 3, 7

        bool _MustUpdateSurroundings = true;


        public TileView(in Tile tile) : base(tile)
        {
            var landData = AssetsLoader.TileData.LandData[tile.TileID];
            Ticks = tile.Position.Z * 4;

            IsStretched = !(landData.TexID <= 0 && (landData.Flags & 0x00000080) > 0);

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
        public bool IsStretched { get; set; }


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

        private bool Draw3DStretched(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            _vertexBufferAlternate[0].Position = position + _vertex0_yOffset;
            _vertexBufferAlternate[1].Position = position + _vertex1_yOffset;
            _vertexBufferAlternate[2].Position = position + _vertex2_yOffset;
            _vertexBufferAlternate[3].Position = position + _vertex3_yOffset;


            if (!spriteBatch.DrawSprite(Texture, _vertexBufferAlternate))
                return false;


            return true;
        }

        private void UpdateStreched(in Facet map)
        {
            float[] surroundingTilesZ = new float[_surroundingIndexes.Length];
            for (int i = 0; i < _surroundingIndexes.Length; i++)
                surroundingTilesZ[i] = map.GetTileZ((short)(WorldObject.Position.X + _surroundingIndexes[i].X), (short)(WorldObject.Position.Y + _surroundingIndexes[i].Y));

            sbyte currentZ = WorldObject.Position.Z;
            sbyte leftZ = (sbyte)surroundingTilesZ[6];
            sbyte rightZ = (sbyte)surroundingTilesZ[3];
            sbyte bottomZ = (sbyte)surroundingTilesZ[7];

            if (!(currentZ == leftZ && currentZ == rightZ && currentZ == bottomZ))
            {
                int low = 0, high = 0, sort = 0;
                sort = map.GetAverageZ(WorldObject.Position.Z, leftZ, rightZ, bottomZ, ref low, ref high);
                if (sort != SortZ)
                {
                    SortZ = (sbyte)sort;
                    map.GetTile((short)WorldObject.Position.X, (short)WorldObject.Position.Y).Sort()/*.ForceSort()*/;
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

            _vertex0_yOffset = new Vector3(22, -(currentZ * 4), 0);
            _vertex1_yOffset = new Vector3(44f, 22 - (rightZ * 4), 0);
            _vertex2_yOffset = new Vector3(0, 22 - (leftZ * 4), 0);
            _vertex3_yOffset = new Vector3(22, 44f - (bottomZ * 4), 0);

            _vertexBufferAlternate[0].Normal = _normals[0];
            _vertexBufferAlternate[1].Normal = _normals[1];
            _vertexBufferAlternate[2].Normal = _normals[2];
            _vertexBufferAlternate[3].Normal = _normals[3];

            Vector3 hue = RenderExtentions.GetHueVector(WorldObject.Hue);
            if (_vertexBufferAlternate[0].Hue != hue)
            {
                _vertexBufferAlternate[0].Hue =
                _vertexBufferAlternate[1].Hue =
                _vertexBufferAlternate[2].Hue =
                _vertexBufferAlternate[3].Hue = hue;
            }
        }

        private Vector3 CalculateNormal(in float a, in float b, in float c, in float d)
        {
            Vector3 v = new Vector3(a - b, 1f, c - d);
            v.Normalize();
            return v;
        }
    }
}
