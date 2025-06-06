using System;
using System.Collections.Generic;
using System.Linq;
using RealRadio.Components.UI.Phone.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ImportPlaylistModal
{
    public List<string> SongUrls => songUrls;

    private readonly MonoBehaviour owner;
    private readonly VisualTreeAsset urlsListItemAsset;
    private TextField urlsField;
    private ListView urlsList;
    private readonly List<string> songUrls;

    public ImportPlaylistModal(MonoBehaviour owner, VisualElement root, VisualTreeAsset urlsListItemAsset)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.urlsListItemAsset = urlsListItemAsset ?? throw new ArgumentNullException(nameof(urlsListItemAsset));
        songUrls = [];

        urlsField = root.Query<TextField>(name: "UrlsField").First() ?? throw new InvalidOperationException("Could not find urls TextField ui element");
        urlsField.RegisterValueChangedCallback((_) => OnUrlsFieldChanged());

        urlsList = root.Query<ListView>(name: "UrlsList").First() ?? throw new InvalidOperationException("Could not find urls List ui element");
        InitUrlsList();
    }

    private void InitUrlsList()
    {
        urlsList.scrollView.mouseWheelScrollSize = RadioAppUi.ScrollSpeed;

        urlsList.makeItem = () => new UrlListItem(urlsListItemAsset).Element;

        urlsList.bindItem = (element, index) =>
        {
            var url = (string)urlsList.itemsSource[index];
            var listItem = (UrlListItem)element.userData;
            listItem.Url = url;
        };

        urlsList.itemsSource = songUrls;
    }

    private void OnUrlsFieldChanged()
    {
    }

    public bool IsValid()
    {
        return false;
    }
}
