using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CoolDownBar : Gump
    {
        public const int COOL_DOWN_WIDTH = 180, COOL_DOWN_HEIGHT = 30;
        public static int DEFAULT_X { get { return ProfileManager.CurrentProfile.CoolDownX; } }
        public static int DEFAULT_Y { get { return ProfileManager.CurrentProfile.CoolDownY; } }

        private AlphaBlendControl background, foreground;
        private Label textLabel, cooldownLabel;
        private DateTime expire;
        private TimeSpan duration;

        private GumpPic gumpPic;

        public BuffIconType buffIconType;

        public CoolDownBar(TimeSpan _duration, string _name, ushort _hue, int x, int y, ushort graphic = ushort.MaxValue, BuffIconType type = BuffIconType.Unknown2) : base(0, 0)
        {
            #region VARS
            Width = COOL_DOWN_WIDTH;
            Height = COOL_DOWN_HEIGHT;
            X = x;
            Y = y;
            expire = DateTime.Now + _duration;
            duration = _duration;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            buffIconType = type;
            #endregion

            #region BACK/FORE GROUND
            background = new AlphaBlendControl(0.3f);
            background.Width = COOL_DOWN_WIDTH;
            background.Height = COOL_DOWN_HEIGHT;
            background.Hue = _hue;

            foreground = new AlphaBlendControl(0.8f);
            foreground.Width = COOL_DOWN_WIDTH;
            foreground.Height = COOL_DOWN_HEIGHT;
            foreground.Hue = _hue;
            #endregion

            if (graphic != ushort.MaxValue)
            {
                gumpPic = new GumpPic(0, 2, graphic, 0);
                background.X = gumpPic.Width;
                background.Width = COOL_DOWN_WIDTH - gumpPic.Width;

                foreground.X = gumpPic.Width;
                foreground.Width = COOL_DOWN_WIDTH - gumpPic.Width;
            }

            #region LABELS
            textLabel = new Label(_name, true, _hue, background.Width, style: FontStyle.BlackBorder, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = background.X
            };

            cooldownLabel = new Label("------", true, _hue, background.Width, style: FontStyle.BlackBorder, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = background.X,
                Y = 0
            };
            cooldownLabel.Y = COOL_DOWN_HEIGHT - cooldownLabel.Height - 2;
            cooldownLabel.Text = "";
            #endregion

            #region ADD CONTROLS
            if (graphic != ushort.MaxValue)
                Add(gumpPic);
            Add(background);
            Add(foreground);
            Add(textLabel);
            Add(cooldownLabel);
            #endregion
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            if (DateTime.Now >= expire)
                Dispose();

            TimeSpan remaing = expire - DateTime.Now;

            if (remaing < TimeSpan.FromMinutes(60))
            {
                int offset = 0;
                if (gumpPic != null)
                    offset = gumpPic.Width;
                foreground.Width = (int)((remaing.TotalSeconds / duration.TotalSeconds) * (COOL_DOWN_WIDTH - offset));
                cooldownLabel.Text = ((int)remaing.TotalSeconds).ToString();
            }

            base.Draw(batcher, x, y);

            batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.Black),
                    x, y,
                    COOL_DOWN_WIDTH,
                    COOL_DOWN_HEIGHT,
                    ShaderHueTranslator.GetHueVector(background.Hue, false, 1f)
                );
            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.Black),
                x + 1, y + 1,
                COOL_DOWN_WIDTH - 2,
                COOL_DOWN_HEIGHT - 2,
                ShaderHueTranslator.GetHueVector(background.Hue, false, 1f)
            );

            return true;
        }

        public class CoolDownConditionData
        {
            public ushort hue;
            public string label;
            public string trigger;
            public int cooldown;
            public int message_type;

            private CoolDownConditionData(ushort hue = 42, string label = "Label", string trigger = "Text to trigger", int cooldown = 10, int message_type = (int)MESSAGE_TYPE.ALL)
            {
                this.hue = hue;
                this.label = label;
                this.trigger = trigger;
                this.cooldown = cooldown;
                this.message_type = message_type;
            }

            public static CoolDownConditionData GetConditionData(int key, bool createIfNotExist)
            {
                CoolDownConditionData data = new CoolDownConditionData();
                if(ProfileManager.CurrentProfile.CoolDownConditionCount > key)
                {
                    data.hue = ProfileManager.CurrentProfile.Condition_Hue[key];
                    data.label = ProfileManager.CurrentProfile.Condition_Label[key];
                    data.trigger = ProfileManager.CurrentProfile.Condition_Trigger[key];
                    data.cooldown = ProfileManager.CurrentProfile.Condition_Duration[key];
                    if (ProfileManager.CurrentProfile.Condition_Type.Count > key) //Remove me after a while to prevent index not found
                        data.message_type = ProfileManager.CurrentProfile.Condition_Type[key];
                    else
                    {
                        while (ProfileManager.CurrentProfile.Condition_Type.Count <= key)
                        {
                            ProfileManager.CurrentProfile.Condition_Type.Add((int)MESSAGE_TYPE.ALL);
                        }
                    }
                } else if (createIfNotExist)
                {
                    ProfileManager.CurrentProfile.Condition_Hue.Add(data.hue);
                    ProfileManager.CurrentProfile.Condition_Label.Add(data.label);
                    ProfileManager.CurrentProfile.Condition_Trigger.Add(data.trigger);
                    ProfileManager.CurrentProfile.Condition_Duration.Add(data.cooldown);
                    ProfileManager.CurrentProfile.Condition_Type.Add(data.message_type);
                }
                return data;
            }

            public static void SaveCondition(int key, ushort hue, string label, string trigger, int cooldown, bool createIfNotExist, int message_type)
            {
                if (ProfileManager.CurrentProfile.CoolDownConditionCount > key)
                {
                    ProfileManager.CurrentProfile.Condition_Hue[key] = hue;
                    ProfileManager.CurrentProfile.Condition_Label[key] = label;
                    ProfileManager.CurrentProfile.Condition_Trigger[key] = trigger;
                    ProfileManager.CurrentProfile.Condition_Duration[key] = cooldown;
                    if (ProfileManager.CurrentProfile.Condition_Type.Count > key) //Remove me after a while to prevent index not found
                        ProfileManager.CurrentProfile.Condition_Type[key] = message_type;
                    else
                    {
                        while (ProfileManager.CurrentProfile.Condition_Type.Count <= key)
                        {
                            ProfileManager.CurrentProfile.Condition_Type.Add((int)MESSAGE_TYPE.ALL);
                        }
                    }
                } else if (createIfNotExist)
                    {
                        ProfileManager.CurrentProfile.Condition_Hue.Add(hue);
                        ProfileManager.CurrentProfile.Condition_Label.Add(label);
                        ProfileManager.CurrentProfile.Condition_Trigger.Add(trigger);
                        ProfileManager.CurrentProfile.Condition_Duration.Add(cooldown);
                    }
            }

            public static void RemoveCondition(int key)
            {
                if(ProfileManager.CurrentProfile.CoolDownConditionCount > key)
                {
                    ProfileManager.CurrentProfile.Condition_Hue.RemoveAt(key);
                    ProfileManager.CurrentProfile.Condition_Label.RemoveAt(key);
                    ProfileManager.CurrentProfile.Condition_Trigger.RemoveAt(key);
                    ProfileManager.CurrentProfile.Condition_Duration.RemoveAt(key);
                }
            }

            public enum MESSAGE_TYPE
            {
                ALL,
                SELF,
                OTHER
            }

        }
    }
}
