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
        builder.Replace("<", "&lt;");
        builder.Replace(">", "&gt;");
        builder.Replace("\"", "&quot;");
        return builder.ToString();
    }
}
