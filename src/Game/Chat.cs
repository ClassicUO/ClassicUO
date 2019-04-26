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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game
{
    [Flags]
    public enum MessageType : byte
    {
        Regular = 0,
        System = 1,
        Emote = 2,
        Limit3Spell = 3, // Sphere style shards use this to limit to 3 of these message types showing overhead.
        Label = 6,
        Focus = 7,
        Whisper = 8,
        Yell = 9,
        Spell = 10,
        Guild = 13,
        Alliance = 14,
        Command = 15,
        Encoded = 0xC0,
        Party = 0xFF, // This is a CUO assigned type, value is unimportant
    }

    public enum MessageFont : ushort
    {
        INVALID = 0xFFFF,
        Bold = 0,
        Shadow = 1,
        BoldShadow = 2,
        Normal = 3,
        Gothic = 4,
        Italic = 5,
        SmallDark = 6,
        Colorful = 7,
        Rune = 8,
        SmallLight = 9
    }

    public enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }

   

    internal static class Chat
    {      
        public static PromptData PromptData { get; set; }

        public static event EventHandler<UOMessageEventArgs> MessageReceived;

        public static event EventHandler<UOMessageEventArgs> LocalizedMessageReceived;
     
    
        public static void HandleMessage(Entity parent, string text, string name, Hue hue, MessageType type, MessageFont font, bool unicode = false, string lang = null)
        {
			switch (type)
			{
                case MessageType.Spell:
                {
                    hue = Constants.NEUTRAL_LABEL_COLOR; //gray color per default
                    if (!string.IsNullOrEmpty(text) && SpellDefinition.WordToTargettype.TryGetValue(text, out TargetType targetType))
                    {
                        if (targetType == TargetType.Beneficial)
                            hue = Constants.BENEFIC_LABEL_COLOR;
                        else if(targetType == TargetType.Harmful)
                            hue = Constants.HARMFUL_LABEL_COLOR;
                    }
                    goto case MessageType.Label;
                }
                case MessageType.Focus:
			    case MessageType.Whisper:
			    case MessageType.Yell:
				case MessageType.Regular:
			    case MessageType.Label:

                    if (parent == null)
                        break;

                    if (parent is Item it && !it.OnGround)
                    {
                        Gump gump = Engine.UI.GetByLocalSerial<Gump>(it.Container);

                        if (gump is PaperDollGump paperDoll)
                        {

                            var inter = paperDoll
                                             .FindControls<PaperDollInteractable>()
                                             .FirstOrDefault();

                            var f = inter?.FindControls<ItemGump>()
                                         .SingleOrDefault(s => s.Item == it);

                            if (f != null)
                                f.AddLabel(text, hue, (byte)font, unicode);
                            else
                                paperDoll.FindControls<EquipmentSlot>()?
                                         .SelectMany(s => s.Children)
                                         .OfType<ItemGump>()
                                         .SingleOrDefault(s => s.Item == it)?
                                         .AddLabel(text, hue, (byte)font, unicode);
                        }
                        else if (gump is ContainerGump container)
                        {
                            container
                                   .FindControls<ItemGump>()?
                                   .SingleOrDefault(s => s.Item == it)?
                                   .AddLabel(text, hue, (byte)font, unicode); 
                        }
                        else
                        {

                            Entity ent = World.Get(it.RootContainer);

                            if (ent == null || ent.IsDestroyed)
                                break;

                            gump = Engine.UI.GetByLocalSerial<TradingGump>(ent);
                            if (gump != null)
                            {
                                gump.FindControls<DataBox>()?
                                       .SelectMany(s => s.Children)
                                       .OfType<ItemGump>()
                                       .SingleOrDefault(s => s.Item == it)?
                                       .AddLabel(text, hue, (byte)font, unicode);
                            }
                            else
                            {
                                Item item = ent.Items.FirstOrDefault(s => s.Graphic == 0x1E5E);

                                if (item == null)
                                    break;

                                gump = Engine.UI.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == item || s.ID2 == item);

                                if (gump != null)
                                {
                                    gump.FindControls<DataBox>()?
                                        .SelectMany(s => s.Children)
                                        .OfType<ItemGump>()
                                        .SingleOrDefault(s => s.Item == it)?
                                        .AddLabel(text, hue, (byte)font, unicode);
                                }
                                else
                                    Log.Message(LogTypes.Warning, "Missing label handler for this control: 'UNKNOWN'. Report it!!");
                            }
                        }
                        
                    }
                    else
                        parent.AddOverhead(type, text, (byte)font, hue, unicode);
					break;
				case MessageType.Emote:
				    parent?.AddOverhead(type, $"*{text}*", (byte)font, hue, unicode);
					break;

                case MessageType.Command:

				case MessageType.Encoded:
                case MessageType.System:
				case MessageType.Party:
				case MessageType.Guild:
                case MessageType.Alliance:
                    break;
                default:
                    parent?.AddOverhead(type, text, (byte)font, hue, unicode);
                    break;
			}

			MessageReceived.Raise(new UOMessageEventArgs(parent, text, name, hue, type, font, unicode, lang), parent);
		}

		public static void OnLocalizedMessage(Entity entity, UOMessageEventArgs args)
        {
            LocalizedMessageReceived.Raise(args, entity);
        }

	}

	internal class UOMessageEventArgs : EventArgs
    {
        public UOMessageEventArgs(Entity parent, string text, string name, Hue hue, MessageType type, MessageFont font, bool unicode = false, string lang = null)
        {
            Parent = parent;
            Text = text;
            Name = name;
            Hue = hue;
            Type = type;
            Font = font;
            Language = lang;
            AffixType = AffixType.None;
            IsUnicode = unicode;
        }

        public UOMessageEventArgs(Entity parent, string text, Hue hue, MessageType type, MessageFont font, uint cliloc, bool unicode = false, AffixType affixType = AffixType.None, string affix = null)
        {
            Parent = parent;
            Text = text;
            Hue = hue;
            Type = type;
            Font = font;
            Cliloc = cliloc;
            AffixType = affixType;
            Affix = affix;
            IsUnicode = unicode;
        }

        public Entity Parent { get; }

        public string Text { get; }

        public string Name { get; }

        public Hue Hue { get; }

        public MessageType Type { get; }

        public MessageFont Font { get; }

        public string Language { get; }

        public uint Cliloc { get; }

        public AffixType AffixType { get; }

        public string Affix { get; }

        public bool IsUnicode { get; }
    }
}