<<<<<<< .mine
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

			/* Build the gump */
			Add(new ResizePic(2600)
			{
				X = 100, Y = 80,
				Width = 470, Height = 372,
			});

			Add(new GumpPic(291, 42, 0x0589, 0));
			Add(new GumpPic(214, 58, 0x058B, 0));
			Add(new GumpPic(300, 51, 0x15A9, 0));

			var localization = FileManager.Cliloc;

			Add(new Label(localization.Translate(3000326), false, 0, font: 2)
			{
				X = 158, Y = 132,
			});

			for (int i = 0; i < professions.Count; i++)
			{
				var cx = i % 2;
				var cy = i / 2;

				Add(new ProfessionInfoGump(professions[i])
				{
					X = 145 + (cx * 195),
					Y = 168 + (cy * 70),

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

			var localization = FileManager.Cliloc;

			var background = new ResizePic(3000)
			{
				Width = 175,
				Height = 34,
			};
			background.SetTooltip(localization.Translate(info.Description));

			Add(background);

			Add(new Label(localization.Translate(info.Localization), true, 0x00, font: 1)
			{
				X = 7, Y = 8,
			});

			Add(new GumpPic(121, -12, info.Graphic, 0));
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



















































































































=======
using ClassicUO.IO;
using ClassicUO.Utility;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharProfessionGump
    {
        private enum PROF_TYPE
        {
            NO_PROF = 0,
            CATEGORY,
            PROFESSION
        }

        private enum PM_CODE
        {
            BEGIN = 1,
            NAME,
            TRUENAME,
            DESC,
            TOPLEVEL,
            GUMP,
            TYPE,
            CHILDREN,
            SKILL,
            STAT,
            STR,
            INT,
            DEX,
            END,
            TRUE,
            CATEGORY,
            NAME_CLILOC_ID,
            DESCRIPTION_CLILOC_ID
        };

        private readonly string[] _Keys = {
            "begin", "name", "truename", "desc", "toplevel", "gump", "type",     "children", "skill",
            "stat",  "str",  "int",      "dex",  "end",      "true", "category", "nameid",   "descid"
        };

        private int GetKeyCode(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return 0;
            }

            key = key.ToLowerInvariant();
            int result = 0;

            for (int i = 0; i < _Keys.Length && result <= 0; i++)
            {
                if (key == _Keys[i])
                {
                    result = i + 1;
                }
            }

            return result;
        }

        private bool ParseFilePart(FileInfo fi)
        {
            List<string> childrens = new List<string>();
            string name = string.Empty;
            string trueName = string.Empty;
            int nameClilocID = 0;
            int descriptionIndex = 0;
            ushort gump = 0;
            bool topLevel = false;
            int[] skillIndex = new int[4] { 0xFF, 0xFF, 0xFF, 0xFF };
            int[] skillValue = new int[4] { 0, 0, 0, 0 };
            int[] stats = new int[3] { 0, 0, 0 };

            bool exit = false;

            TextFileParser file = new TextFileParser(fi, new char[] { '\t', ',' }, new char[] { '#', ';' }, new char[] { '"', '"' });
            while (!file.IsEOF() && !exit)
            {
                List<string> strings = file.ReadTokens();

                if (strings.Count < 1)
                {
                    continue;
                }

                int code = GetKeyCode(strings[0]);

                switch ((PM_CODE)code)
                {
                    case PM_CODE.BEGIN:
                    case PM_CODE.END:
                    {
                        exit = true;
                        break;
                    }
                    case PM_CODE.NAME:
                    {
                        name = strings[1];
                        break;
                    }
                    case PM_CODE.TRUENAME:
                    {
                        trueName = strings[1];
                        break;
                    }
                    case PM_CODE.DESC:
                    {
                        int.TryParse(strings[1], out descriptionIndex);
                        break;
                    }
                    case PM_CODE.TOPLEVEL:
                    {
                        topLevel = (GetKeyCode(strings[1]) == (int)PM_CODE.TRUE);
                        break;
                    }
                    case PM_CODE.GUMP:
                    {
                        ushort.TryParse(strings[1], out gump);

                        /*g_Orion.ExecuteGump(gump);
                        g_Orion.ExecuteGump(gump + 1);*/
                        break;
                    }
                    case PM_CODE.TYPE:
                    {
                        if (GetKeyCode(strings[1]) == (int)PM_CODE.CATEGORY)
                        {
                        }
                        else
                        {
                        }

                        break;
                    }
                    case PM_CODE.CHILDREN:
                    {
                        /*IFOR(j, 1, (int)strings.size())
                        childrens.push_back(strings[j]);*/

                        break;
                    }
                    case PM_CODE.SKILL:
                    {
                        for (int i = 0; i < 4 && i < strings.Count && strings.Count > 2; i++)
                        {
                            for (int j = 0; j < 54; j++)
                            {
                                /*CSkill* skillPtr = g_SkillsManager.Get((uint)j);

                                if (skillPtr != NULL && strings[1] == skillPtr->Name)
                                {
                                    skillIndex[i] = j;
                                    skillValue[i] = atoi(strings[2].c_str());
                                }*/
                            }
                        }

                        break;
                    }
                    case PM_CODE.STAT:
                    {
                        if (strings.Count > 2)
                        {
                            code = GetKeyCode(strings[1]);
                            int.TryParse(strings[2], out int val);

                            if ((PM_CODE)code == PM_CODE.STR)
                            {
                                stats[0] = val;
                            }
                            else if ((PM_CODE)code == PM_CODE.INT)
                            {
                                stats[1] = val;
                            }
                            else if ((PM_CODE)code == PM_CODE.DEX)
                            {
                                stats[2] = val;
                            }
                        }

                        break;
                    }
                    case PM_CODE.NAME_CLILOC_ID:
                    {
                        int.TryParse(strings[1], out nameClilocID);
                        name = FileManager.Cliloc.GetString(nameClilocID).ToUpperInvariant();
                        break;
                    }
                    case PM_CODE.DESCRIPTION_CLILOC_ID:
                    {
                        //descriptionClilocID = atoi(strings[1].c_str());
                        break;
                    }
                    default:
                        break;
                }
            }

            /*CBaseProfession* obj = NULL;

            if (type == PROF_TYPE.CATEGORY)
            {
                CProfessionCategory* temp = new CProfessionCategory();

                IFOR(i, 0, (int)childrens.size())
                    temp->AddChildren(childrens[i]);

                obj = temp;
            }
            else if (type == PROF_TYPE.PROFESSION)
            {
                CProfession* temp = new CProfession();

                temp->Str = stats[0];
                temp->Int = stats[1];
                temp->Dex = stats[2];

                IFOR(i, 0, 4)
                {
                    temp->SetSkillIndex((int)i, (uchar)skillIndex[i]);
                    temp->SetSkillValue((int)i, (uchar)skillValue[i]);
                }

                obj = temp;
            }

            bool result = (type != PROF_TYPE.NO_PROF);

            if (obj != NULL)
            {
                obj->NameClilocID = nameClilocID;
                obj->Name = name;
                obj->TrueName = trueName;
                obj->DescriptionClilocID = descriptionClilocID;
                obj->DescriptionIndex = descriptionIndex;
                obj->TopLevel = topLevel;
                obj->Gump = gump;
                obj->Type = type;

                if (topLevel)
                    m_Items->Add(obj);
                else
                {
                    CBaseProfession* parent = (CBaseProfession*)(m_Items);

                    while (parent != NULL)
                    {
                        result = AddChild(parent, obj);

                        if (result)
                            break;

                        parent = (CBaseProfession*)parent->m_Next;
                    }

                    if (!result)
                        delete obj;
                }
            }*/

            return false;//result;
        }

        private bool Load()
        {
            bool result = false;

            /*CProfessionCategory* head = new CProfessionCategory();
            head->TrueName = "parent";
            head->Name = "Parent";
            head->DescriptionIndex = -2;
            head->Type = PROF_TYPE.CATEGORY;
            head->Gump = 0x15A9;
            head->TopLevel = true;
            Add(head);*/

            FileInfo file = new FileInfo(Path.Combine(FileManager.UoFolderPath, "Prof.txt"));
            //what if file doesn't exist? we skip section completely...directly into advanced selection
            TextFileParser read = new TextFileParser(file, new char[] { ' ', '\t', ',' }, new char[] { '#', ';' }, new char[] { '"', '"' });

            if (!read.IsEOF())
            {
                while (!read.IsEOF())
                {
                    List<string> strings = read.ReadTokens();

                    if (strings.Count > 0)
                    {
                        if (strings[0].ToLowerInvariant() == "begin")
                        {
                            result = ParseFilePart(file);

                            if (!result)
                            {
                                break;
                            }
                        }
                    }
                }


                /*g_Orion.ExecuteGump(0x15A9);
                g_Orion.ExecuteGump(0x15AA);

                CProfession* apc = new CProfession();
                apc->TrueName = "advanced";
                apc->Name = "Advanced";
                apc->Type = PT_PROFESSION;
                apc->Gump = 0x15A9;
                apc->DescriptionIndex = -1;
                apc->SetSkillIndex(0, 0xFF);
                apc->SetSkillIndex(1, 0xFF);
                apc->SetSkillIndex(2, 0xFF);
                apc->SetSkillIndex(3, 0xFF);

                apc->Str = 44;
                apc->Int = 10;
                apc->Dex = 11;

                apc->SetSkillValue(0, 50);
                apc->SetSkillValue(1, 50);
                apc->SetSkillValue(2, 0);
                apc->SetSkillValue(3, 0);

                head->Add(apc);

                LoadProfessionDescription();*/
            }
            /*else
                LOG("Could not find prof.txt in your UO directory. Character creation professions loading failed.\n");*/

            return result;
        }
    }
}
>>>>>>> .theirs
