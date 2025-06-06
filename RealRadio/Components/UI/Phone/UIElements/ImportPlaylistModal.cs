using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RealRadio.Components.Radio;
using RealRadio.Components.YoutubeDL;
using UnityEngine;
using UnityEngine.UIElements;
using YoutubeDLSharp.Metadata;

namespace RealRadio.Components.UI.Phone.UIElements;

public class ImportPlaylistModal : IDisposable
{
    public List<string> SongUrls => songUrls;
    public UiState State
    {
        get => state;
        private set
        {
            if (state == value)
                return;

            state = value;
            OnStateChanged();
        }
    }

    private readonly MonoBehaviour owner;
    private readonly VisualTreeAsset urlsListItemAsset;
    private TextField urlsField;
    private ListView urlsList;
    private UiState state;
    private readonly List<string> songUrls = [];
    private readonly List<VideoData> songData = [];
    private Coroutine? queryCoroutine;
    private Exception? queryException;
    private CancellationTokenSource? queryCts;
    private const float debounceTimeLimit = 1f;
    private Coroutine? debounceCoroutine;

    public ImportPlaylistModal(MonoBehaviour owner, VisualElement root, VisualTreeAsset urlsListItemAsset)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.urlsListItemAsset = urlsListItemAsset ?? throw new ArgumentNullException(nameof(urlsListItemAsset));

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

            var videoData = songData.Count > index ? songData[index] : null;

            if (videoData != null)
                listItem.HumanReadableText = RadioStationInfoManager.SongInfoFromVideoData(videoData).ToString();
        };

        urlsList.itemsSource = songUrls;
    }

    private void OnUrlsFieldChanged()
    {
        CancelQuery();

        if (urlsField.text == string.Empty)
        {
            State = UiState.FieldEmpty;
            return;
        }

        State = UiState.LoadingPlaylists;
    }

    private void OnStateChanged()
    {
        CancelQuery();

        switch (State)
        {
            case UiState.LoadingPlaylists:
                debounceCoroutine = owner.StartCoroutine(DebounceRoutine());
                break;
        }
    }

    private IEnumerator DebounceRoutine()
    {
        yield return new WaitForSeconds(debounceTimeLimit);
        debounceCoroutine = null;

        // This is a really inefficient way to only query valid urls, but considering
        // the small number of urls that will be processed it's not a big deal
        var playlistUrls = urlsField.text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(url => Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri) ? uri : null)
            .Where(uri => uri is not null)
            .Where(uri => uri!.Scheme is "http" or "https")
            .Select(uri => uri!.ToString())
            .ToArray();

        queryCoroutine = owner.StartCoroutine(QueryPlaylistUrls(playlistUrls));
    }

    private IEnumerator QueryPlaylistUrls(string[] playlistUrls)
    {
        songData.Clear();
        songUrls.Clear();
        urlsList.Rebuild();

        queryCts = new CancellationTokenSource();
        var queryTask = YtDlpManager.Instance.FetchPlaylistMetaData(playlistUrls, queryCts.Token);
        yield return new WaitUntil(() => queryTask.IsCompleted);

        queryCts.Dispose();
        queryCts = null;

        if (queryTask.IsFaulted)
        {
            Plugin.Logger.LogWarning($"Failed to fetch playlist urls: {queryTask.Exception}");
            queryCoroutine = null;
            queryException = queryTask.Exception;
            State = UiState.Invalid;
            yield break;
        }

        songData.Clear();
        songData.AddRange(queryTask.Result);

        songUrls.Clear();
        songUrls.AddRange(queryTask.Result.Select(vd => vd.Url));

        urlsList.Rebuild();

        State = UiState.Valid;
        queryCoroutine = null;
    }

    public bool IsValid() => State == UiState.Valid;

    public void Dispose()
    {
        CancelQuery();
    }

    private void CancelQuery()
    {
        if (queryCoroutine != null)
        {
            owner.StopCoroutine(queryCoroutine);
            queryCoroutine = null;
        }

        if (debounceCoroutine != null)
        {
            owner.StopCoroutine(debounceCoroutine);
            debounceCoroutine = null;
        }

        queryCts?.Cancel();
        queryCts?.Dispose();
        queryCts = null;
    }

    public enum UiState
    {
        FieldEmpty,
        LoadingPlaylists,
        Invalid,
        Valid,
    }
}
