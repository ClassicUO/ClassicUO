namespace ClassicUO.Ecs;

internal static class TextComposer
{
    public static string Compose(string text, char v)
    {
        if (v == '\b')
        {
            if (!string.IsNullOrEmpty(text) && text.Length > 0)
                return text.Remove(text.Length - 1, 1);

            return text;
        }

        if (v == '\t')
            return text;

        if (v == '\r')
            v = '\n';

        return text + v;
    }
}