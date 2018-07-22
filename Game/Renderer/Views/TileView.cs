using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer;
using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Renderer.Views
{
    public class TileView : View
    {
        private Vector3 _vertex0_yOffset, _vertex1_yOffset, _vertex2_yOffset, _vertex3_yOffset;
        private readonly Vector3[] _normals = new Vector3[4];
        private readonly SpriteVertex[] _vertex =
        {
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(0, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(1, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(0, 1, 0)),
            new SpriteVertex(new Vector3(), new Vector3(),  new Vector3(1, 1, 0))
        };

        private static Point[] _surroundingIndexes =
        {
            new Point(0, -1), new Point(1, -1),
            new Point(-1, 0), new Point(1, 0), new Point(2, 0),
            new Point(-1, 1), new Point(0, 1), new Point(1, 1), new Point(2, 1),
            new Point(0, 2), new Point(1, 2)
        };

        private bool _needUpdateStrechedTile = true;



        public TileView(in Tile tile) : base(tile)
        {
            IsStretched = !(tile.TileData.TexID <= 0 && (tile.TileData.Flags & 0x00000080) > 0);

            AllowedToDraw = !(tile.IsIgnored);
        }


        public bool IsStretched { get; }
        public new Tile WorldObject => (Tile)base.WorldObject;


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (!AllowedToDraw)
                return false;


            if (Texture == null || Texture.IsDisposed)
            {
                if (IsStretched)
                {
                    Texture = TextureManager.GetOrCreateTexmapTexture(WorldObject.TileData.TexID);
                }
                else
                {
                    Texture = TextureManager.GetOrCreateLandTexture(WorldObject.Graphic);
                    Bounds = new Rectangle(0, WorldObject.Position.Z * 4, 44, 44);
                }
            }

            if (_needUpdateStrechedTile)
            {
                UpdateStreched(World.Map);
                _needUpdateStrechedTile = false;
            }

            if (!IsStretched)
                return base.Draw(spriteBatch, position);
            return Draw3DStretched(spriteBatch, position);
        }

        private bool Draw3DStretched(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            Texture.Ticks = World.Ticks;

            _vertex[0].Position = position + _vertex0_yOffset;
            _vertex[1].Position = position + _vertex1_yOffset;
            _vertex[2].Position = position + _vertex2_yOffset;
            _vertex[3].Position = position + _vertex3_yOffset;

            if (!spriteBatch.DrawSprite(Texture, _vertex))
                return false;

            MousePick(_vertex);

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
                sbyte low = 0, high = 0, sort = 0;
                sort = (sbyte)map.GetAverageZ(WorldObject.Position.Z, leftZ, rightZ, bottomZ, ref low, ref high);
                if (sort != SortZ)
                {
                    SortZ = sort;
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

            _vertex[0].Normal = _normals[0];
            _vertex[1].Normal = _normals[1];
            _vertex[2].Normal = _normals[2];
            _vertex[3].Normal = _normals[3];

            Vector3 hue = RenderExtentions.GetHueVector(WorldObject.Hue);
            if (_vertex[0].Hue != hue)
            {
                _vertex[0].Hue =
                _vertex[1].Hue =
                _vertex[2].Hue =
                _vertex[3].Hue = hue;
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
