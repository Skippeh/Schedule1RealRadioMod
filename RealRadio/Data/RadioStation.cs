using System;
using System.Linq;
using FishNet.Serializing;
using HashUtility;
using RealRadio.Components.Radio;
using UnityEngine;

namespace RealRadio.Data;

[Serializable]
[CreateAssetMenu(fileName = "RadioStation", menuName = "RealRadio/ScriptableObjects/RadioStation")]
public class RadioStation : ScriptableObject
{
    /// <summary>
    /// The unique id of this radio station.
    /// </summary>
    public string? Id;

    /// <summary>
    /// The name of this radio station.
    /// </summary>
    public string? Name;

    /// <summary>
    /// The abbreviation to use for this radio station. Used in the radial menu if there is no icon.
    /// </summary>
    public string? Abbreviation;

    /// <summary>
    /// The type of this radio station. Determines which <see cref="Components.Audio.HostControllers.HostController" /> is used to play this radio station.
    /// </summary>
    public RadioType Type;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="RadioType.InternetRadio"/>, this is the url of the internet radio.
    /// </summary>
    public string? Url;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="RadioType.YtDlp"/>, this is the urls of all the video/audio files to play on this station.
    /// The files will be played in a random order.
    ///
    /// The files are downloaded and converted to an audio file using yt-dlp.
    /// </summary>
    public string[]? Urls;

    /// <summary>
    /// Whether or not this radio station can be played by NPCs. This applies for vehicles driven by NPCs and random houses around the map.
    /// </summary>
    public bool CanBePlayedByNPCs = true;

    /// <summary>
    /// The icon to use for this radio station. Used in the radial menu. Should be a square texture, preferably minimum 128x128.
    /// </summary>
    public Sprite? Icon;

    /// <summary>
    /// An optional background color to use for this radio station. The background is used in the radial menu.
    /// </summary>
    public Color BackgroundColor = Color.clear;

    /// <summary>
    /// Whether or not to make the background rounded. Used in the radial menu.
    /// </summary>
    public bool RoundedBackground = true;

    /// <summary>
    /// Text color to use for the abbreviation text. Used in the radial menu.
    /// </summary>
    public Color TextColor = Color.white;

    public override string ToString()
    {
        return $"{Name} ({Type}): {TypeDataToString()}";
    }

    public Components.API.Data.RadioStation ToDataType()
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Abbreviation = Abbreviation,
            Type = Type,
            Url = Url,
            Urls = Urls.ToArray(),
            CanBePlayedByNPCs = CanBePlayedByNPCs,
            TextColor = $"#{ColorUtility.ToHtmlStringRGBA(TextColor)}",
            BackgroundColor = $"#{ColorUtility.ToHtmlStringRGBA(BackgroundColor)}",
            RoundedBackground = RoundedBackground,
        };
    }

    private string TypeDataToString()
    {
        return Type switch
        {
            RadioType.InternetRadio => Url ?? string.Empty,
            RadioType.YtDlp => Urls != null ? $" ({Urls.Length} songs)" : string.Empty,
            _ => throw new NotImplementedException($"Unknown type: {Type}"),
        };
    }
}

public static class RadioStationNetworkExtensions
{
    public static void WriteRadioStation(this Writer writer, RadioStation? value)
    {
        if (value == null)
        {
            writer.WriteByte(1);
            return;
        }

        if (value.Id == null)
            throw new ArgumentNullException(nameof(value.Id), "Station id cannot be null");

        writer.WriteByte(0);
        writer.Write(value.Id.GetStableHashCode());
    }

    public static RadioStation? ReadRadioStation(this Reader reader)
    {
        if (reader.ReadByte() == 1)
            return null;

        uint hashedId = reader.ReadUInt32();

        if (!RadioStationManager.InstanceExists)
            throw new InvalidOperationException("RadioStationManager does not exist at the time of reading a radio station from the network");

        if (!RadioStationManager.Instance.StationsByHashedId.TryGetValue(hashedId, out var station))
            throw new InvalidOperationException($"Could not find radio station with id {hashedId}");

        return station;
    }
}
