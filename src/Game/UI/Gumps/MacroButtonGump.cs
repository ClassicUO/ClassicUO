﻿#region license

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
using System.Collections.Generic;
using System.IO;

using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MacroButtonGump : AnchorableGump
    {
        public Macro _macro;
        private Texture2D backgroundTexture;
        private Label label;

        public MacroButtonGump(Macro macro, int x, int y) : this()
        {
            X = x;
            Y = y;
            _macro = macro;

            BuildGump();
        }

        public MacroButtonGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            CanBeSaved = true;
            WantUpdateSize = false;
            AnchorGroupName = "spell";
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
        }

        private void BuildGump()
        {
            Width = 88;
            Height = 44;

            label = new Label(_macro.Name, true, 1001, Width, 255, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 0,
                Width = Width - 10,
            };
            label.Y = (Height >> 1) - (label.Height >> 1);
            Add(label);

            backgroundTexture = Textures.GetTexture(new Color(30, 30, 30));
        }

        protected override void OnMouseEnter(int x, int y)
        {
            label.Hue = 53;
            backgroundTexture = Textures.GetTexture(Color.DimGray);
            base.OnMouseEnter(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            label.Hue = 1001;
            backgroundTexture = Textures.GetTexture(new Color(30, 30, 30));
            base.OnMouseExit(x, y);
        }


        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            Point offset = Mouse.LDroppedOffset;

            if (Engine.Profile.Current.CastSpellsByOneClick && button == MouseButton.Left && !Keyboard.Alt && Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
            {
                RunMacro();
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (Engine.Profile.Current.CastSpellsByOneClick || button != MouseButton.Left)
                return false;

            RunMacro();
            
            return true;
        }

        private void RunMacro()
        {
            if (_macro != null)
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();
                gs.Macros.SetMacroToExecute(_macro.FirstNode);
                gs.Macros.WaitForTargetTimer = 0;
                gs.Macros.Update();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            _hueVector.Z = 0.1f;

            batcher.Draw2D(backgroundTexture, x, y, Width, Height, ref _hueVector);

            _hueVector.Z = 0;
            batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

            base.Draw(batcher, x, y);
            return true;
        }

        public override void Save(BinaryWriter writer)
        {
            if(_macro != null)
            {
                int macroid = Engine.SceneManager.GetScene<GameScene>().Macros.GetAllMacros().IndexOf(_macro);

                LocalSerial = (uint) macroid + 1000;

                base.Save(writer);
                writer.Write((byte) 0); //version
                writer.Write(_macro.Name);
                writer.Write(LocalSerial);
            }
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            byte version = reader.ReadByte();
            string name = reader.ReadString();
            LocalSerial = reader.ReadUInt32();

            Macro macro = Engine.SceneManager.GetScene<GameScene>().Macros.FindMacro(name);

            if (macro != null)
            {
                _macro = macro;
                BuildGump();
            }

        }
    }
}