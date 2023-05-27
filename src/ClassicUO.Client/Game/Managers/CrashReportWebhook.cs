using System;
using System.Net;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Managers
{
    public class CrashReportWebhook : IDisposable
    {
        private readonly WebClient dWebClient;
        private static NameValueCollection discordValues = new NameValueCollection();
        public string WebHook { get; set; }
        public string UserName { get; set; }
        public string ProfilePicture { get; set; }

        public CrashReportWebhook()
        {
            dWebClient = new WebClient();

            ProfilePicture = "https://static.giantbomb.com/uploads/original/4/42381/1196379-gas_mask_respirator.jpg";
            UserName = "Crash Reporter";
            WebHook = "";
        }


        public CrashReportWebhook SendMessage(string msgSend)
        {
            if (String.IsNullOrEmpty(WebHook))
                return null;
            msgSend = System.Net.WebUtility.HtmlEncode(msgSend);
            discordValues.Add("username", UserName);
            discordValues.Add("avatar_url", ProfilePicture);
            discordValues.Add("content", msgSend);
            dWebClient.UploadValues(Obf(WebHook, 0), discordValues);
            return this;
        }

        public void Dispose()
        {
            dWebClient.Dispose();
        }

        public static List<string> Split(string str, int chunkSize)
        {
            List<string> strings = new List<string>();

            int c = 0, amt = str.Length / chunkSize;

            for (int i = 0; i < amt; i++)
            {
                strings.Add(str.Substring(i * chunkSize, chunkSize));
            }

            return strings;
        }

        public static string Obf(string source, Int16 shift)
        {
            var maxChar = Convert.ToInt32(char.MaxValue);
            var minChar = Convert.ToInt32(char.MinValue);

            var buffer = source.ToCharArray();

            for (var i = 0; i < buffer.Length; i++)
            {
                var shifted = Convert.ToInt32(buffer[i]) + shift;

                if (shifted > maxChar)
                {
                    shifted -= maxChar;
                }
                else if (shifted < minChar)
                {
                    shifted += maxChar;
                }

                buffer[i] = Convert.ToChar(shifted);
            }

            return new string(buffer);
        }
    }

}
