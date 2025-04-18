namespace ClassicUO.Ecs;

internal static class TextComposer
{
    public static string Compose(string text, char v)
    {
        if (v == '\b')
        {
            if (!string.IsNullOrEmpty(text) && text.Length > 0)
                return text.Remove(text.Length - 1, 1);

            return string.Empty;
        }

        if (v == '\t')
            return string.Empty;

        if (v == '\r')
            v = '\n';

        return text + v;
    }
}