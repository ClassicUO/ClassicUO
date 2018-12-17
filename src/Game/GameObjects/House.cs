#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.Scenes;
using ClassicUO.Utility.Coroutines;

namespace ClassicUO.Game.GameObjects
{
    public sealed class House : IEquatable<Serial>, IDisposable
    {
        public House(Serial serial, uint revision, bool isCustom)
        {
            Serial = serial;
            Revision = revision;
            IsCustom = isCustom;
        }

        public Serial Serial { get; }
        public uint Revision { get; private set; }
        public List<Multi> Components { get; } = new List<Multi>();
        public bool IsCustom { get; }

        public void Generate()
        {
            Components.ForEach(s =>
            {
                s.AddToTile();
            });
        }

        public bool Equals(Serial other) => Serial == other;

        public void Dispose()
        {
            Components.ForEach(s => s.Dispose());
            Components.Clear();
        }
    }
}