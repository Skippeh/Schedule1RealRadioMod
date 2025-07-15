using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RealRadio;

public static class UITKHelper
{
    [return: NotNullIfNotNull(nameof(text))]
    public static string? EscapeRichText(this string? text)
    {
        if (text == null)
            return null;

        var builder = new StringBuilder(text);

        while (true)
        {
            int indexOfNoParse1 = text.IndexOf("<noparse>", System.StringComparison.OrdinalIgnoreCase);
            int indexOfNoParse2 = text.IndexOf("</noparse>", System.StringComparison.OrdinalIgnoreCase);

            if (indexOfNoParse1 == -1 && indexOfNoParse2 == -1)
                break;

            if (indexOfNoParse1 != -1)
            {
                builder.Remove(indexOfNoParse1, "<noparse>".Length);

                if (indexOfNoParse2 > indexOfNoParse1)
                    indexOfNoParse2 -= "<noparse>".Length;
            }

            if (indexOfNoParse2 != -1)
                builder.Remove(indexOfNoParse2, "</noparse>".Length);

            // This is probably very inefficient but likely doesn't matter in practice
            text = builder.ToString();
        }

        builder.Insert(0, "<noparse>");
        builder.Append("</noparse>");
        return builder.ToString();
    }
}
