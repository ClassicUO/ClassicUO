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

using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class StaticView : View
    {
        public StaticView(Static st) : base(st)
        {
            AllowedToDraw = !IsNoDrawable(st.Graphic);
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;
            Static st = (Static) GameObject;

            if (st.Effect == null)
            {
                if (Texture == null || Texture.IsDisposed)
                {
                    Texture = Art.GetStaticTexture(GameObject.Graphic);
                    Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44, Texture.Width, Texture.Height);
                }

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
                        //Rectangle fol = Bounds;
                        //fol.X = (int) position.X - Bounds.X;
                        //fol.Y = (int) position.Y - Bounds.Y;

                        //Rectangle prect = World.Player.View.Bounds;
                        //prect.X += (int) World.Player.DrawX;
                        //prect.Y += (int) World.Player.DrawY;

                        //if (fol.Contains(prect))
                        //{
                        //    alpha = .6f;
                        //}
                    }
                }

                HueVector = ShaderHuesTraslator.GetHueVector(GameObject.Hue, false, alpha, false);
                MessageOverHead(spriteBatch, position, Bounds.Y - 22);

                return base.Draw(spriteBatch, position, objectList);
            }

            return !st.Effect.IsDisposed && st.Effect.View.Draw(spriteBatch, position, objectList);
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int) vertex[0].Position.X;
            int y = list.MousePosition.Y - (int) vertex[0].Position.Y;
            //if (Art.Contains(GameObject.Graphic, x, y))
            if (Texture.Contains(x, y))
                list.Add(GameObject, vertex[0].Position);
        }
    }
}