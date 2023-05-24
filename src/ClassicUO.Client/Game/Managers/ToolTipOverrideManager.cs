using ClassicUO.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal class ToolTipOverrideData
    {
        private ToolTipOverrideData(int index, string searchText, string formattedText, int min1, int max1, int min2, int max2)
        {
            Index = index;
            SearchText = searchText;
            FormattedText = formattedText;
            Min1 = min1;
            Max1 = max1;
            Min2 = min2;
            Max2 = max2;
        }

        public int Index { get; }
        public string SearchText { get; set; }
        public string FormattedText { get; set; }
        public int Min1 { get; set; }
        public int Max1 { get; set; }
        public int Min2 { get; set; }
        public int Max2 { get; set; }

        public bool IsNew { get; set; } = false;

        public static ToolTipOverrideData Get(int index)
        {
            bool isNew = false;
            if (ProfileManager.CurrentProfile != null)
            {
                string searchText = "Weapon Damage", formattedText = "DMG /c[orange]{0} /cd- /c[red]";
                int min1 = -1, max1 = 99, min2 = -1, max2 = 99;

                if (ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count > index)
                    searchText = ProfileManager.CurrentProfile.ToolTipOverride_SearchText[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_NewFormat.Count > index)
                    formattedText = ProfileManager.CurrentProfile.ToolTipOverride_NewFormat[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_MinVal1.Count > index)
                    min1 = ProfileManager.CurrentProfile.ToolTipOverride_MinVal1[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_MinVal2.Count > index)
                    min2 = ProfileManager.CurrentProfile.ToolTipOverride_MinVal2[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1.Count > index)
                    max1 = ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1[index];
                else isNew = true;

                if (ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2.Count > index)
                    max2 = ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2[index];
                else isNew = true;

                ToolTipOverrideData data = new ToolTipOverrideData(index, searchText, formattedText, min1, max1, min2, max2);

                if (isNew)
                {
                    data.IsNew = true;
                    data.Save();
                }
                return data;
            }
            return null;
        }

        public void Save()
        {
            if (ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_SearchText[Index] = SearchText;
            else ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Add(SearchText);

            if (ProfileManager.CurrentProfile.ToolTipOverride_NewFormat.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_NewFormat[Index] = FormattedText;
            else ProfileManager.CurrentProfile.ToolTipOverride_NewFormat.Add(FormattedText);

            if (ProfileManager.CurrentProfile.ToolTipOverride_MinVal1.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_MinVal1[Index] = Min1;
            else ProfileManager.CurrentProfile.ToolTipOverride_MinVal1.Add(Min1);

            if (ProfileManager.CurrentProfile.ToolTipOverride_MinVal2.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_MinVal2[Index] = Min2;
            else ProfileManager.CurrentProfile.ToolTipOverride_MinVal2.Add(Min2);

            if (ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1[Index] = Max1;
            else ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1.Add(Max1);

            if (ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2.Count > Index)
                ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2[Index] = Max2;
            else ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2.Add(Max2);
        }

        public void Delete()
        {
            ProfileManager.CurrentProfile.ToolTipOverride_SearchText.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_NewFormat.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_MinVal1.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_MinVal2.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1.RemoveAt(Index);
            ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2.RemoveAt(Index);
        }
    }
}
