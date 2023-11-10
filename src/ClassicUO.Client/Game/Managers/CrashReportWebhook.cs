using System;
using System.Collections.Specialized;
using System.Net.Http;

namespace ClassicUO.Game.Managers
{
    public class CrashReportWebhook
    {
        private static NameValueCollection discordValues = new NameValueCollection();
        public string WebHook { get; set; } = "";
        public string UserName { get; set; } = "";
        public string ProfilePicture { get; set; } = "https://static.giantbomb.com/uploads/original/4/42381/1196379-gas_mask_respirator.jpg";

        public CrashReportWebhook()
        {
        }

        public CrashReportWebhook SendMessage(string msgSend)
        {
            if (String.IsNullOrEmpty(WebHook))
                return null;

            using (HttpClient httpClient = new HttpClient())
            {
                MultipartFormDataContent form = new MultipartFormDataContent();
                var file_bytes = System.Text.Encoding.Unicode.GetBytes(msgSend);
                form.Add(new ByteArrayContent(file_bytes, 0, file_bytes.Length), "Document", "log.txt");
                httpClient.PostAsync(WebHook, form).Wait();
                httpClient.Dispose();
            }

            return this;
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
