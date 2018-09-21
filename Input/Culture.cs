using System.Text;
using ClassicUO.Platforms.Windows;

namespace ClassicUO.Input
{
    public static class Culture
    {
        private static Encoding _encoding;

        public static void RemoveEncoder() => _encoding = null;

        public static char TranslateChar(char input)
        {
            if (_encoding == null)
                _encoding = GetSystemEncoding();
            return _encoding.GetChars(new[] {(byte) input})[0];
        }

        private static Encoding GetSystemEncoding()
        {
            Encoding encoding = Encoding.GetEncoding((int) NativeMethods.GetCurrentCodePage());

            return encoding;
        }
    }
}