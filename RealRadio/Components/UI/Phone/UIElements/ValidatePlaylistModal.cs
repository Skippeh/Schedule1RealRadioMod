using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RealRadio.Components.YoutubeDL;
using UnityEngine;
using UnityEngine.UIElements;
using YoutubeDLSharp.Metadata;

namespace RealRadio.Components.UI.Phone.UIElements
{
    public class ValidatePlaylistModal : IDisposable
    {
        public event Action<string[]>? OnValidated;

        public ReadOnlyCollection<string> ValidatedUrls { get; private set; }
        public ReadOnlyCollection<string> FailedUrls { get; private set; }

        private readonly MonoBehaviour owner;
        private readonly List<string> urls;
        private readonly List<string> validatedUrls = [];
        private readonly List<string> failedUrls = [];

        private readonly ProgressBar progressBar;
        private readonly Label statusLabel;

        private readonly CancellationTokenSource cts = new();
        private readonly Dictionary<string, Coroutine> coroutines = [];

        private Coroutine? validateRoutine;

        public ValidatePlaylistModal(MonoBehaviour owner, VisualElement root, IEnumerable<string> urls)
        {
            this.owner = owner;
            this.urls = urls.ToList();
            ValidatedUrls = new(validatedUrls);
            FailedUrls = new(failedUrls);

            progressBar = root.Query<ProgressBar>(name: "ProgressBar").First() ?? throw new InvalidOperationException("Could not find progress bar ui element");
            statusLabel = root.Query<Label>(name: "StatusLabel").First() ?? throw new InvalidOperationException("Could not find status label ui element");

            progressBar.lowValue = 0;
            progressBar.highValue = this.urls.Count;

            UpdateProgressBarTitle(0);
            SetStatus(null);

            validateRoutine = owner.StartCoroutine(ValidateRoutine());
        }

        private IEnumerator ValidateRoutine()
        {
            int currentIndex = 0;

            while (true)
            {
                int processingCount = coroutines.Count;

                if (processingCount >= 8)
                    yield return null;

                if (currentIndex >= urls.Count)
                    break;

                var url = urls[currentIndex++];
                var coroutine = owner.StartCoroutine(ValidateUrl(url));
                coroutines.Add(url, coroutine);
            }

            while (coroutines.Count > 0)
                yield return null;

            OnValidated?.Invoke(validatedUrls.ToArray());
        }

        private IEnumerator ValidateUrl(string url)
        {
            var task = YtDlpManager.Instance.FetchMetaData(url);
            yield return new WaitUntil(() => task.IsCompleted);

            try
            {
                if (task.IsFaulted)
                {
                    failedUrls.Add(url);
                    yield break;
                }

                if (task.Result.Availability is not Availability.Public or Availability.Unlisted)
                {
                    Plugin.Logger.LogInfo($"Song '{url}' is not public or unlisted");
                    failedUrls.Add(url);
                    yield break;
                }

                validatedUrls.Add(url);
            }
            finally
            {
                UpdateProgressBarTitle(validatedUrls.Count + failedUrls.Count);
                coroutines.Remove(url);
            }

            yield break;
        }

        public void Dispose()
        {
            owner.StopCoroutine(validateRoutine);

            foreach (var coroutine in coroutines.Values)
                owner.StopCoroutine(coroutine);

            coroutines.Clear();

            try
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignored, it may already be disposed
            }

            cts.Dispose();
        }

        public bool IsValid => ValidatedUrls.Count == urls.Count;

        private void UpdateProgressBarTitle(int validatedUrlsCount)
        {
            progressBar.title = $"Validating song {validatedUrlsCount}/{urls.Count}...";
        }

        private void SetStatus(string? status)
        {
            statusLabel.text = status ?? string.Empty;

            if (string.IsNullOrEmpty(status))
                statusLabel.style.display = DisplayStyle.None;
            else
                statusLabel.style.display = DisplayStyle.Flex;
        }
    }
}
