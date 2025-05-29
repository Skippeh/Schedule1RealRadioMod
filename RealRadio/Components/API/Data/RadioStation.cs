using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RealRadio.Components.API.Data;

public class RadioStation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Abbreviation { get; set; }
    public RadioType? Type { get; set; }
    public string? Url { get; set; }
    public string[]? Urls { get; set; }
    public bool CanBePlayedByNPCs { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public bool RoundedBackground { get; set; }
    public string? TextColor { get; set; }

    [MemberNotNullWhen(true, nameof(Id), nameof(Name), nameof(Type))]
    public bool IsValid(out List<string> invalidReasons)
    {
        invalidReasons = [];

        if (string.IsNullOrWhiteSpace(Id))
        {
            invalidReasons.Add("Id cannot be null or only whitespace");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            invalidReasons.Add("Name cannot be null or only whitespace");
        }

        if (Type == null || !Enum.IsDefined(typeof(RadioType), Type.Value))
        {
            invalidReasons.Add($"Invalid radio type: {Type} - must be one of: {string.Join(", ", Enum.GetNames(typeof(RadioType)))}");
        }

        if (Type == RadioType.InternetRadio && string.IsNullOrEmpty(Url))
        {
            invalidReasons.Add("Internet radio station must have a 'Url' set");
        }

        if (Type == RadioType.YtDlp)
        {
            if (Urls is not { Length: > 0 })
            {
                invalidReasons.Add("YtDlp radio station must have at least one url in 'Urls'");
            }
            else
            {
                for (int i = 0; i < Urls.Length; i++)
                {
                    string url = Urls[i];

                    if (string.IsNullOrEmpty(url))
                    {
                        invalidReasons.Add($"Urls[{i}] cannot be empty");
                    }
                    else
                    {
                        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https"))
                        {
                            invalidReasons.Add($"Urls[{i}] must be a valid url '{url}' (must start with 'http://' or 'https://')");
                        }
                    }
                }
            }
        }

        if (BackgroundColor != null)
        {
            if (!ColorUtility.TryParseHtmlString(BackgroundColor, out _))
            {
                invalidReasons.Add($"Invalid background color '{BackgroundColor}'");
            }
        }

        if (TextColor != null)
        {
            if (!ColorUtility.TryParseHtmlString(TextColor, out _))
            {
                invalidReasons.Add($"Invalid text color '{TextColor}'");
            }
        }

        return invalidReasons.Count == 0;
    }

    [MemberNotNullWhen(true, nameof(Type), nameof(Url))]
    public bool IsInternetRadio()
    {
        return Type == RadioType.InternetRadio && !string.IsNullOrEmpty(Url);
    }

    [MemberNotNullWhen(true, nameof(Type), nameof(Urls))]
    public bool IsYtDlpRadio()
    {
        return Type == RadioType.YtDlp && Urls is { Length: > 0 };
    }

    public RealRadio.Data.RadioStation ToRuntimeType()
    {
        var result = ScriptableObject.CreateInstance<RealRadio.Data.RadioStation>();

        result.Id = Id;
        result.Name = Name ?? string.Empty;
        result.Abbreviation = Abbreviation ?? string.Empty;
        result.Type = Type.GetValueOrDefault();
        result.Url = Url ?? string.Empty;
        result.Urls = Urls ?? [];
        result.CanBePlayedByNPCs = CanBePlayedByNPCs;
        result.BackgroundColor = ColorUtility.TryParseHtmlString(BackgroundColor, out var backgroundColor) ? backgroundColor : Color.clear;
        result.RoundedBackground = RoundedBackground;
        result.TextColor = ColorUtility.TryParseHtmlString(TextColor, out var textColor) ? textColor : Color.white;

        return result;
    }
}
