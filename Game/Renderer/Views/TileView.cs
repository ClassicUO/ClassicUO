using ClassicUO.Game.Map;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class TileView : View
    {
        private static readonly Point[] _surroundingIndexes = { new Point(0, -1), new Point(1, -1), new Point(-1, 0), new Point(1, 0), new Point(2, 0), new Point(-1, 1), new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(0, 2), new Point(1, 2) };

        private readonly Vector3[] _normals = new Vector3[4];

        private readonly SpriteVertex[] _vertex = { new SpriteVertex(new Vector3(), new Vector3(), new Vector3(0, 0, 0)), new SpriteVertex(new Vector3(), new Vector3(), new Vector3(1, 0, 0)), new SpriteVertex(new Vector3(), new Vector3(), new Vector3(0, 1, 0)), new SpriteVertex(new Vector3(), new Vector3(), new Vector3(1, 1, 0)) };

        private bool _needUpdateStrechedTile = true;
        private Vector3 _vertex0_yOffset, _vertex1_yOffset, _vertex2_yOffset, _vertex3_yOffset;


        public TileView(Tile tile) : base(tile)
        {
            IsStretched = !(tile.TileData.TexID <= 0 && (tile.TileData.Flags & 0x00000080) > 0);

            AllowedToDraw = !tile.IsIgnored;
        }


        public bool IsStretched { get; }
        public new Tile GameObject => (Tile)base.GameObject;


        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
            {
                return false;
            }

            if (Texture == null || Texture.IsDisposed)
            {
                if (IsStretched)
                {
                    Texture = TextureManager.GetOrCreateTexmapTexture(GameObject.TileData.TexID);
                }
                else
                {
                    Texture = TextureManager.GetOrCreateLandTexture(GameObject.Graphic);
                    Bounds = new Rectangle(0, GameObject.Position.Z * 4, 44, 44);
                }
            }

            if (_needUpdateStrechedTile)
            {
                UpdateStreched(World.Map);
                _needUpdateStrechedTile = false;
            }

            return !IsStretched ? base.Draw(spriteBatch, position) : Draw3DStretched(spriteBatch, position);
        }


        private bool Draw3DStretched(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            Texture.Ticks = World.Ticks;

            _vertex[0].Position = position + _vertex0_yOffset;
            _vertex[1].Position = position + _vertex1_yOffset;
            _vertex[2].Position = position + _vertex2_yOffset;
            _vertex[3].Position = position + _vertex3_yOffset;

            if (!spriteBatch.DrawSprite(Texture, _vertex))
            {
                return false;
            }

            MousePick(_vertex);

            return true;
        }

        private void UpdateStreched(Facet map)
        {
            float[] surroundingTilesZ = new float[_surroundingIndexes.Length];
            for (int i = 0; i < _surroundingIndexes.Length; i++)
            {
                surroundingTilesZ[i] = map.GetTileZ((short)(GameObject.Position.X + _surroundingIndexes[i].X), (short)(GameObject.Position.Y + _surroundingIndexes[i].Y));
            }

            sbyte currentZ = GameObject.Position.Z;
            sbyte leftZ = (sbyte)surroundingTilesZ[6];
            sbyte rightZ = (sbyte)surroundingTilesZ[3];
            sbyte bottomZ = (sbyte)surroundingTilesZ[7];

            if (!(currentZ == leftZ && currentZ == rightZ && currentZ == bottomZ))
            {
                sbyte low = 0, high = 0;
                sbyte sort = (sbyte)map.GetAverageZ(GameObject.Position.Z, leftZ, rightZ, bottomZ, ref low, ref high);
                if (sort != SortZ)
                {
                    SortZ = sort;
                    map.GetTile((short)GameObject.Position.X, (short)GameObject.Position.Y).ForceSort();
                }
            }

            _normals[0] = CalculateNormal(surroundingTilesZ[2], surroundingTilesZ[3], surroundingTilesZ[0], surroundingTilesZ[6]);
            _normals[1] = CalculateNormal(GameObject.Position.Z, surroundingTilesZ[4], surroundingTilesZ[1], surroundingTilesZ[7]);
            _normals[2] = CalculateNormal(surroundingTilesZ[5], surroundingTilesZ[7], GameObject.Position.Z, surroundingTilesZ[9]);
            _normals[3] = CalculateNormal(surroundingTilesZ[6], surroundingTilesZ[8], surroundingTilesZ[3], surroundingTilesZ[10]);

            _vertex0_yOffset = new Vector3(22, -(currentZ * 4), 0);
            _vertex1_yOffset = new Vector3(44f, 22 - rightZ * 4, 0);
            _vertex2_yOffset = new Vector3(0, 22 - leftZ * 4, 0);
            _vertex3_yOffset = new Vector3(22, 44f - bottomZ * 4, 0);

            _vertex[0].Normal = _normals[0];
            _vertex[1].Normal = _normals[1];
            _vertex[2].Normal = _normals[2];
            _vertex[3].Normal = _normals[3];

            Vector3 hue = RenderExtentions.GetHueVector(GameObject.Hue);
            if (_vertex[0].Hue != hue)
            {
                _vertex[0].Hue = _vertex[1].Hue = _vertex[2].Hue = _vertex[3].Hue = hue;
            }
        }

        private Vector3 CalculateNormal(float a,  float b,  float c,  float d)
        {            
            return Vector3.Normalize(new Vector3(a - b, 1f, c - d));
        }
    }
}