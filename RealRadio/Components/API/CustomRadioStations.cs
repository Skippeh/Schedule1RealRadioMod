using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Funly.SkyStudio;
using Newtonsoft.Json;
using RealRadio.Data;
using UnityEngine;
using ApiRadioStation = RealRadio.Components.API.Data.RadioStation;

namespace RealRadio.Components.API;

public class CustomRadioStations
{
    private readonly string rootDirectory;

    public CustomRadioStations(string rootDirectory)
    {
        this.rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));

        if (!Directory.Exists(rootDirectory))
            Directory.CreateDirectory(rootDirectory);
    }

    public async Task<List<RadioStation>> LoadStationsFromDisk()
    {
        var result = new List<RadioStation>();

        string[] files = Directory.GetFiles(rootDirectory, "*.json", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            string iconPath = Path.ChangeExtension(filePath, ".png");
            Plugin.Logger.LogInfo($"Attempting to load radio station from file '{filePath}'...");

            try
            {
                string fileContents = await File.ReadAllTextAsync(filePath);
                result.Add(await LoadRadioStation(fileContents, iconPath));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load custom radio station from file '{filePath}': {ex}");
            }
        }

        return result;
    }

    private async Task<RadioStation> LoadRadioStation(string json, string iconPath)
    {
        var apiStation = JsonConvert.DeserializeObject<ApiRadioStation>(json) ?? throw new ArgumentException("Deserialized JSON is null");

        if (!apiStation.IsValid(out var invalidReasons))
        {
            throw new ArgumentException($"Could not validate radio station:\n- {string.Join("\n- ", invalidReasons)}");
        }

        RadioStation result = ScriptableObject.CreateInstance<RadioStation>();
        result.Id = apiStation.Id;
        result.Name = apiStation.Name;
        result.name = apiStation.Name;
        result.Abbreviation = apiStation.Abbreviation;
        result.Type = apiStation.Type.Value;
        result.Url = apiStation.Url;
        result.Urls = apiStation.Urls;
        result.CanBePlayedByNPCs = apiStation.CanBePlayedByNPCs;
        result.RoundedBackground = apiStation.RoundedBackground;

        Color? backgroundColor = ParseColor(apiStation.BackgroundColor);

        if (backgroundColor.HasValue)
            result.BackgroundColor = backgroundColor.Value;

        Color? textColor = ParseColor(apiStation.TextColor);

        if (textColor.HasValue)
            result.TextColor = textColor.Value;

        Sprite? sprite = await LoadSprite(iconPath);

        if (sprite != null)
            result.Icon = sprite;

        return result;
    }

    private Color? ParseColor(string? color)
    {
        if (string.IsNullOrEmpty(color))
            return null;

        if (!ColorUtility.TryParseHtmlString(color, out var result))
            return null;

        return result;
    }

    private async Task<Sprite?> LoadSprite(string iconPath)
    {
        if (!File.Exists(iconPath))
            return null;

        var bytes = await File.ReadAllBytesAsync(iconPath);
        var texture = new Texture2D(1, 1);
        ImageConversion.LoadImage(texture, bytes);
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, pixelsPerUnit: 100);
        return sprite;
    }
}
