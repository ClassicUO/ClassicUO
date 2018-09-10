using System;
using System.Collections.Generic;
using System.Text;

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
            return _encoding.GetChars(new byte[] { (byte)input })[0];
        }

        private static Encoding GetSystemEncoding()
        {
            Encoding encoding = Encoding.GetEncoding((int)Platforms.Windows.NativeMethods.GetCurrentCodePage());

            return encoding;
        }
    }
}
