using System.Text;

namespace ClassicUO.Utility
{
    public static class StringHelper
    {
        public static string CapitalizeFirstCharacter(string str)
        {
            if (str == null || str == string.Empty)
                return string.Empty;
            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static string CapitalizeAllWords(string str)
        {
            if (str == null || str == string.Empty)
                return string.Empty;
            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();

            StringBuilder sb = new StringBuilder();
            bool capitalizeNext = true;
            for (int i = 0; i < str.Length; i++)
            {
                if (capitalizeNext)
                    sb.Append(char.ToUpper(str[i]));
                else
                    sb.Append(str[i]);
                capitalizeNext = " .,;!".Contains(str[i]);
            }

            return sb.ToString();
        }
    }
}