#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    public struct SpriteVertex : IVertexType
    {
        public SpriteVertex(Vector3 position, Vector3 normal, Vector3 textureCoordinate)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            Hue = Vector3.Zero;
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 TextureCoordinate;
        public Vector3 Hue;

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;


        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), // position
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal,
                    0), // normal
                new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate,
                    0), // tex coord
                new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate,
                    1) // hue
            );

        public static readonly SpriteVertex[] PolyBuffer =
        {
            new SpriteVertex(new Vector3(), new Vector3(0, 0, 1), new Vector3(0, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(0, 0, 1), new Vector3(0, 1, 0)),
            new SpriteVertex(new Vector3(), new Vector3(0, 0, 1), new Vector3(1, 1, 0))
        };

        public static readonly SpriteVertex[] PolyBufferFlipped =
        {
            new SpriteVertex(new Vector3(), new Vector3(0, 0, 1), new Vector3(0, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(0, 0, 1), new Vector3(0, 1, 0)),
            new SpriteVertex(new Vector3(), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(0, 0, 1), new Vector3(1, 1, 0))
        };

        public static int SizeInBytes => sizeof(float) * 12;

        public override string ToString() =>
            string.Format("VPNTH: <{0}> <{1}>", Position.ToString(), TextureCoordinate.ToString());
    }
}