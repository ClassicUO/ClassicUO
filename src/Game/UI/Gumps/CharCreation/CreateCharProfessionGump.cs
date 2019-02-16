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
