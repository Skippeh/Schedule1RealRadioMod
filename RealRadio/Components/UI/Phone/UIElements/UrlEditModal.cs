using System;
using System.Collections;
using RealRadio.Components.YoutubeDL;
using UnityEngine;
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
            OnUrlChanged();
        }
    }

    public UrlState State
    {
        get => state;
        private set
        {
            // There is intentionally no check for equality here.
            // This is to allow the state being updated to the same one several times so that the debounce works properly.

            state = value;
            OnStateChanged();
        }
    }

    private readonly TextField urlField;
    private readonly Label statusLabel;
    private readonly VisualElement metadataContainer;
    private readonly Label uploaderLabel;
    private readonly Label titleLabel;
    private readonly MonoBehaviour owner;
    private UrlState state;

    private const float debounceTimeLimit = 1f;
    private Coroutine? debounceCoroutine;
    private Coroutine? fetchMetadataCoroutine;
    private Exception? fetchException;

    public UrlEditModal(MonoBehaviour owner, VisualElement root, string? url)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

        urlField = root.Query<TextField>(name: "Url").First() ?? throw new InvalidOperationException("Could not find url TextField ui element");
        statusLabel = root.Query<Label>(name: "StatusLabel").First() ?? throw new InvalidOperationException("Could not find status Label ui element");
        metadataContainer = root.Query(name: "MetadataContainer").First() ?? throw new InvalidOperationException("Could not find metadata container ui element");
        uploaderLabel = metadataContainer.Query<Label>(name: "UploaderLabel").First() ?? throw new InvalidOperationException("Could not find uploader Label ui element");
        titleLabel = metadataContainer.Query<Label>(name: "TitleLabel").First() ?? throw new InvalidOperationException("Could not find title Label ui element");

        urlField.RegisterValueChangedCallback((_) => OnUrlChanged());

        if (string.IsNullOrEmpty(url))
        {
            Url = string.Empty;
            OnUrlChanged();
        }
        else
        {
            Url = url;
        }
    }

    public bool IsValid()
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https"))
            return false;

        return true;
    }

    private void OnUrlChanged()
    {
        State = Url switch
        {
            "" => UrlState.UrlEmpty,
            _ => IsValid() ? UrlState.ValidAndMetaDataLoading : UrlState.Invalid,
        };
    }

    private void OnStateChanged()
    {
        Plugin.Logger.LogInfo($"Url state changed to {State}");

        if (debounceCoroutine != null)
        {
            owner.StopCoroutine(debounceCoroutine);
            debounceCoroutine = null;
        }

        if (fetchMetadataCoroutine != null)
        {
            owner.StopCoroutine(fetchMetadataCoroutine);
            fetchMetadataCoroutine = null;
        }

        if (State == UrlState.ValidAndMetaDataLoading)
            debounceCoroutine = owner.StartCoroutine(DebounceRoutine());

        if (State == UrlState.ValidAndMetaDataLoaded)
        {
            metadataContainer.style.display = DisplayStyle.Flex;
            statusLabel.style.display = DisplayStyle.None;
        }
        else
        {
            metadataContainer.style.display = DisplayStyle.None;
            statusLabel.style.display = DisplayStyle.Flex;
        }

        statusLabel.text = State switch
        {
            UrlState.UrlEmpty => "Type a URL to fetch metadata",
            UrlState.Invalid => "Invalid URL",
            UrlState.ValidAndMetaDataLoading => "Loading metadata...",
            UrlState.ValidAndMetaDataFailed => $"Error: {fetchException?.Message ?? "Unknown error"}",
            _ => string.Empty,
        };
    }

    private IEnumerator DebounceRoutine()
    {
        yield return new WaitForSeconds(debounceTimeLimit);
        debounceCoroutine = null;
        OnDebounceFinished();
    }

    private void OnDebounceFinished()
    {
        fetchMetadataCoroutine = owner.StartCoroutine(FetchMetadataRoutine());
    }

    private IEnumerator FetchMetadataRoutine()
    {
        var fetchTask = YtDlpManager.Instance.FetchMetaData(Url);
        yield return new WaitUntil(() => fetchTask.IsCompleted);

        if (fetchTask.IsFaulted)
        {
            Plugin.Logger.LogWarning($"Failed to fetch metadata for '{Url}':\n{fetchTask.Exception}");
            fetchException = fetchTask.Exception;
            State = UrlState.ValidAndMetaDataFailed;
            yield break;
        }

        var metaData = fetchTask.Result;

        if (metaData.IsLive == true)
        {
            fetchException = new ArgumentException("Live streams are not supported");
            State = UrlState.ValidAndMetaDataFailed;
            yield break;
        }

        uploaderLabel.text = metaData.Uploader;
        titleLabel.text = metaData.Title;
        State = UrlState.ValidAndMetaDataLoaded;
    }

    public enum UrlState
    {
        ValidAndMetaDataLoaded,
        ValidAndMetaDataFailed,
        ValidAndMetaDataLoading,
        Invalid,
        UrlEmpty,
    }
}
