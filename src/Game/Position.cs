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

using System;

namespace ClassicUO.Game
{
    internal readonly struct Position
    {
        public static readonly Position INVALID = new Position(0xFFFF, 0xFFFF);

        public Position(ushort x, ushort y, sbyte z = 0) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly ushort X;
        public readonly ushort Y;
        public readonly sbyte Z;

        public static bool operator ==(Position p1, Position p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
        }

        public static bool operator !=(Position p1, Position p2)
        {
            return p1.X != p2.X || p1.Y != p2.Y || p1.Z != p2.Z;
        }

        public static Position operator +(Position p1, Position p2)
        {
            return new Position((ushort) (p1.X + p2.X), (ushort) (p1.Y + p2.Y), (sbyte) (p1.Z + p2.Z));
        }

        public static Position operator -(Position p1, Position p2)
        {
            return new Position((ushort) (p1.X - p2.X), (ushort) (p1.Y - p2.Y), (sbyte) (p1.Z - p2.Z));
        }

        public static Position operator *(Position p1, Position p2)
        {
            return new Position((ushort) (p1.X * p2.X), (ushort) (p1.Y * p2.Y), (sbyte) (p1.Z * p2.Z));
        }

        public static Position operator /(Position p1, Position p2)
        {
            return new Position((ushort) (p1.X / p2.X), (ushort) (p1.Y / p2.Y), (sbyte) (p1.Z / p2.Z));
        }

        public static bool operator <(Position p1, Position p2)
        {
            return p1.X < p2.X && p1.Y < p2.Y;
        }

        public static bool operator >(Position p1, Position p2)
        {
            return p1.X > p2.X && p1.Y > p2.Y;
        }

        public static bool operator <=(Position p1, Position p2)
        {
            return p1.X <= p2.X && p1.Y <= p2.Y;
        }

        public static bool operator >=(Position p1, Position p2)
        {
            return p1.X >= p2.X && p1.Y >= p2.Y;
        }

        public int DistanceTo(Position position)
        {
            return Math.Max(Math.Abs(position.X - X), Math.Abs(position.Y - Y));
        }

        public int DistanceTo(int x, int y)
        {
            return Math.Max(Math.Abs(x - X), Math.Abs(y - Y));
        }

        public double DistanceToSqrt(Position position)
        {
            int a = position.X - X;
            int b = position.Y - Y;

            return Math.Sqrt(a * a + b * b);
        }

        public override int GetHashCode()
        {
            return X ^ Y ^ Z;
        }

        public override bool Equals(object obj)
        {
            return obj is Position position && this == position;
        }

        public override string ToString()
        {
            return $"{X}.{Y}.{Z}";
        }

        public static Position Parse(string str)
        {
            string[] args = str.Split('.');

            return new Position(ushort.Parse(args[0]), ushort.Parse(args[1]), sbyte.Parse(args[2]));
        }
    }
}