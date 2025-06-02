using System;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone.UIElements;

public class UrlEditModal
{
    public string Url
    {
        get => urlField.text;
        set
        {
            var newValue = value ?? string.Empty;

            if (urlField.text == newValue)
                return;

            urlField.text = newValue;
        }
    }

    private readonly TextField urlField;

    public UrlEditModal(VisualElement root, string? url)
    {
        urlField = root.Query<TextField>(name: "Url").First() ?? throw new InvalidOperationException("Could not find url TextField ui element");

        Url = url ?? string.Empty;
    }

    public bool IsValid()
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https"))
            return false;

        return true;
    }
}
