using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
	internal class CreateCharProfessionGump : Gump
	{
		public CreateCharProfessionGump() : base(0, 0)
		{
			var professions = new List<ProfessionInfo>();

			/* Parse the prof.txt if one is available. */
			// TODO: Move this to FileManager for parsing at load time. Leave it here for now to test.
			var professionFilePath = $@"{FileManager.UoFolderPath}\prof.txt";
			var professionFile = new FileInfo(professionFilePath);

			if (professionFile.Exists)
			{
				var professionParser = new TextFileParser(professionFile, new char[] { '\t', '\r', '\n' }, new char[] { }, new char[] { '"', '"' });
				var tokens = professionParser.ReadTokens();

				var index = 0;

				while (index < tokens.Count)
				{
					var currentToken = tokens[index];

					index++;

					if (String.IsNullOrEmpty(currentToken))
						continue;

					if (!currentToken.StartsWith("begin", StringComparison.OrdinalIgnoreCase))
						continue;

					if (ProfessionInfo.TryReadSection(tokens, ref index, out var info))
						professions.Add(info);
				}
			}

			professions.Add(new ProfessionInfo()
			{
				Name = "Advanced",
				Localization = 1061176,
				Description = 1061226,
				Graphic = 5504,
			});

			X = 100;
			Y = 80;

			/* Build the gump */
			Add(new ResizePic(2600)
			{
				Width = 470, Height = 372,
			});

			Add(new GumpPic(113, -23, 1419, 0));
			Add(new GumpPic(190, -36, 1417, 0));
			Add(new GumpPic(199, -27, 5546, 0));

			var localization = FileManager.Cliloc;

			Add(new Label(localization.Translate(3000326), false, 0, font: 2)
			{
				X = 58, Y = 52,
			});

			for (int i = 0; i < professions.Count; i++)
			{
				var cx = i % 2;
				var cy = i / 2;

				Add(new ProfessionInfoGump(professions[i])
				{
					X = 45 + (cx * 195),
					Y = 88 + (cy * 70),

					Selected = SelectProfession,
				});
			}

			Add(new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586, Y = 445, ButtonAction = ButtonAction.Activate
            });
		}

		public void SelectProfession(ProfessionInfo info)
		{
			var charCreationGump = Engine.UI.GetByLocalSerial<CharCreationGump>();

			if (charCreationGump != null)
				charCreationGump.SetProfession(info);
		}

		public override void OnButtonClick(int buttonID)
		{
			var charCreationGump = Engine.UI.GetByLocalSerial<CharCreationGump>();

			switch ((Buttons)buttonID)
			{
				case Buttons.Prev:
					charCreationGump.StepBack();
					break;
			}

			base.OnButtonClick(buttonID);
		}

		private enum Buttons
		{
			Prev,
		}
	}

	internal class ProfessionInfoGump : Control
	{
		private ProfessionInfo _info;

		public Action<ProfessionInfo> Selected;

		public ProfessionInfoGump(ProfessionInfo info)
		{
			_info = info;

			Add(new ResizePic(3000)
			{
				Width = 175, Height = 34,
			});

			var localization = FileManager.Cliloc;

			Add(new Label(localization.Translate(info.Localization), true, 0x00, font: 1)
			{
				X = 7, Y = 8,
			});

			Add(new GumpPic(121, -12, info.Graphic, 0));

			SetTooltip(localization.Translate(info.Description));
		}

		protected override void OnMouseClick(int x, int y, MouseButton button)
		{
			base.OnMouseClick(x, y, button);

			if (button == MouseButton.Left)
			{
				if (Selected != null)
					Selected(_info);
			}
		}
	}

	internal class ProfessionInfo
	{
		public static bool TryReadSection(List<string> list, ref int index, out ProfessionInfo info)
		{
			info = new ProfessionInfo();

			while (index < list.Count)
			{
				var currentToken = list[index].ToLower();

				if (currentToken.StartsWith("end", StringComparison.OrdinalIgnoreCase))
					break;

				switch (currentToken)
				{
					case "name":
					{
						info.Name = list[++index];
						break;
					}
					case "nameid":
					{
						info.Localization = int.Parse(list[++index]);
						break;
					}
					case "descid":
					{
						info.Description = int.Parse(list[++index]);
						break;
					}
					case "gump":
					{
						info.Graphic = (Graphic)int.Parse(list[++index]);
						break;
					}
					case "skill":
					{
						info.Skills.Add(list[++index], int.Parse(list[++index]));
						break;
					}
					case "stat":
					{
						info.Stats.Add(list[++index], int.Parse(list[++index]));
						break;
					}
				}

				index++;
			}

			return true;
		}

		public string Name { get; set; }
		public int Localization { get; set; }
		public int Description { get; set; }

		public Graphic Graphic { get; set; }

		public Dictionary<string, int> Skills { get; set; } = new Dictionary<string, int>();
		public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();
	}

}
