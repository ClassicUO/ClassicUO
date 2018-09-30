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
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class StaticView : View
    {
        public StaticView(Static st) : base(st) => AllowedToDraw = !IsNoDrawable(st.Graphic);


        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed) return false;

            if (Texture == null || Texture.IsDisposed)
            {
                Texture = Art.GetStaticTexture(GameObject.Graphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + GameObject.Position.Z * 4,
                    Texture.Width, Texture.Height);
            }

            Static st = (Static) GameObject;

            float alpha = 0;

            if (TileData.IsFoliage((long) st.ItemData.Flags))
            {
                bool check = World.Player.Position.X <= st.Position.X && World.Player.Position.Y <= st.Position.Y;

                if (!check)
                {
                    check = World.Player.Position.Y <= st.Position.Y && World.Player.Position.X <= st.Position.X + 1;

                    if (!check)
                        check = World.Player.Position.X <= st.Position.X && World.Player.Position.Y <= st.Position.Y + 1;
                }

                if (check)
                {
                    //Rectangle pos = new Rectangle((int)position.X + Bounds.X, (int)position.Y + Bounds.Y, Bounds.Width, Bounds.Height);
                    ////Rectangle pos1 = new Rectangle((int)position.X - World.Player.View.Bounds.X, (int)position.Y - World.Player.View.Bounds.Y, World.Player.View.Bounds.Width, World.Player.View.Bounds.Height);

                    //if (pos.InRect(((MobileView)World.Player.View).BoudsStrange))
                    //    alpha = .6f;
                }
            }

            HueVector = RenderExtentions.GetHueVector(GameObject.Hue, false, alpha, false );
            MessageOverHead(spriteBatch, position, Bounds.Y - 22);
            return base.Draw(spriteBatch, position, objectList);
        }


        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int) vertex[0].Position.X;
            int y = list.MousePosition.Y - (int) vertex[0].Position.Y;

            if (Art.Contains(GameObject.Graphic, x, y)) list.Add(GameObject, vertex[0].Position);
        }
    }
}