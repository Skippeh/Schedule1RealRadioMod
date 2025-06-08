using System;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone.UIElements;

public class UrlListItem
{
    public VisualElement Element { get; private set; }

    public string Url
    {
        get => url;
        set
        {
            if (value == url)
                return;

            url = value;
            OnTextParametersChanged();
        }
    }

    public string? HumanReadableText
    {
        get => humanReadableText;
        set
        {
            if (value == humanReadableText)
                return;

            humanReadableText = value;
            OnTextParametersChanged();
        }
    }

    private Label textLabel;
    private string url = string.Empty;
    private string? humanReadableText;

    public UrlListItem(VisualTreeAsset listItemAsset)
    {
        Element = listItemAsset.Instantiate();
        Element.userData = this;

        textLabel = Element.Query<Label>(name: "Text").First() ?? throw new InvalidOperationException("Could not find name label ui element");

        OnTextParametersChanged();
    }

    private void OnTextParametersChanged()
    {
        textLabel.text = humanReadableText ?? url;
    }
}
