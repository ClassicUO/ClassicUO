#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct VertexPositionColorTexture4 : IVertexType
    {
        public const int RealStride = 96;

        VertexDeclaration IVertexType.VertexDeclaration => VertexPositionColorTexture.VertexDeclaration;

        public Vector3 Position0;
        public Color Color0;
        public Vector2 TextureCoordinate0;
        public Vector3 Position1;
        public Color Color1;
        public Vector2 TextureCoordinate1;
        public Vector3 Position2;
        public Color Color2;
        public Vector2 TextureCoordinate2;
        public Vector3 Position3;
        public Color Color3;
        public Vector2 TextureCoordinate3;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PositionNormalTextureColor : IVertexType
    {
 
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 TextureCoordinate;
        public Vector3 Hue;

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), // position
                                                                                           new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0), // normal
                                                                                           new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0), // tex coord
                                                                                           new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1) // hue
                                                                                         );

        public const int SizeInBytes = sizeof(float) * 12 * 4;

#if DEBUG
        public override string ToString()
        {
            return string.Format("VPNTH: <{0}> <{1}>", Position.ToString(), TextureCoordinate.ToString());
        }
#endif
    }
}