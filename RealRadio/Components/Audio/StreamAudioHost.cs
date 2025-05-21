using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioStreamer;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using RealRadio.Events;
using ScheduleOne.NPCs.CharacterClasses;
using UnityEngine;

namespace RealRadio.Components.Audio;

[RequireComponent(typeof(AudioSource))]
public class StreamAudioHost : MonoBehaviour
{
    public IReadOnlyCollection<StreamAudioClient> ActiveClients => enabledClients;
    public IReadOnlyCollection<StreamAudioClient> SpawnedClients => spawnedClients;

    public float MaxClientInactivityTime = 30f;

    public int NumActiveClients => enabledClients.Count;

    public Action? OnStreamStarted;
    public Action? OnStreamStopped;
    public Action? OnStreamEnded;
    public Action<EventRefData<bool>>? OnStreamStartRequested;

    public AudioStream? AudioStream;

    public float[]? AudioData { get; private set; }
    public int AudioDataLength { get; private set; }

    /// <summary>
    /// True if the audio stream is started and has read to the end.
    /// </summary>
    public bool StreamEnded => streamEnded == true;

    private AudioSource audioSource = null!;
    private Task? startStreamTask;
    private CancellationTokenSource? startStreamCts;
    private HashSet<StreamAudioClient> spawnedClients = [];
    private HashSet<StreamAudioClient> enabledClients = [];
    private int clientIdCounter = 1;
    private float? inactiveTimer;
    private bool startRequested;
    private bool stopRequested;
    private bool readingAudioData;
    private bool waitingForWarmup;
    private bool? streamEnded;

    public StreamAudioClient CreateClient(Transform? parent = null, Vector3? localPosition = null)
    {
        var go = new GameObject("StreamAudioClient");

        if (parent != null)
            go.transform.SetParent(parent ?? transform, false);

        if (localPosition != null)
            go.transform.localPosition = localPosition.Value;

        var client = AddClient(go);

        if (parent != null || localPosition != null)
        {
            client.AudioSource.spatialBlend = 1;
        }

        return client;
    }

    public StreamAudioClient AddClient(GameObject gameObject)
    {
        bool createdAudioSource = false;
        var audioSource = gameObject.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            createdAudioSource = true;
        }

        var client = gameObject.GetComponent<StreamAudioClient>() ?? gameObject.AddComponent<StreamAudioClient>();
        client.Host = this;
        client.Id = clientIdCounter++;

        if (createdAudioSource)
        {
            audioSource.volume = 0.2f;
        }

        spawnedClients.Add(client);

        if (client.isActiveAndEnabled)
        {
            enabledClients.Add(client);

            if (AudioStream?.Started == true && startStreamTask != null)
                client.OnHostStartRequested?.Invoke();
            else if (AudioStream?.Started == true)
                client.OnHostStreamStarted?.Invoke();

            OnNumActiveClientsChanged();
        }

        return client;
    }

    public void DestroyClient(StreamAudioClient client)
    {
        if (spawnedClients.Remove(client))
        {
            Destroy(client);
        }
    }

    public void DetachClient(GameObject gameObject)
    {
        var client = gameObject.GetComponent<StreamAudioClient>();

        if (client == null)
            return;

        if (client.Host != this)
            throw new InvalidOperationException("Client is not owned by this host");

        if (spawnedClients.Remove(client))
        {
            enabledClients.Remove(client);

            client.Host = null;
            client.Id = 0;

            OnNumActiveClientsChanged();
        }
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? throw new InvalidOperationException("No AudioSource component found on game object");
        audioSource.mute = true;
    }

    private void Start()
    {
        if (AudioStream?.Started == false && startStreamTask == null)
            OnEnable();
    }

    private void Update()
    {
        CheckStartStreamTask();

        if (inactiveTimer != null)
        {
            inactiveTimer += Time.unscaledDeltaTime;

            if (inactiveTimer >= MaxClientInactivityTime)
            {
                Plugin.Logger.LogInfo("Stopping audio stream host due to inactivity");
                StopAudioStream();
            }
        }

        if (streamEnded == true)
        {
            OnStreamEnded?.Invoke();
            streamEnded = null;
            StopAudioStreamNow();
        }
    }

    private void LateUpdate()
    {
        bool startRequested = this.startRequested;
        bool stopRequested = this.stopRequested;

        if (startRequested && !stopRequested)
        {
            if (startStreamTask == null && AudioStream?.Started is false or null)
            {
                var preventStartRefData = new EventRefData<bool>(false);
                OnStreamStartRequested?.Invoke(preventStartRefData);

                if (preventStartRefData.Value)
                {
                    Plugin.Logger.LogInfo("Prevented audio stream start");
                    this.startRequested = false;
                    return;
                }
            }

            StartAudioStreamNow();
        }

        if (stopRequested && !readingAudioData && !waitingForWarmup)
        {
            StopAudioStreamNow();
        }
    }

    private void OnEnable()
    {
        if (AudioStream == null)
        {
            Plugin.Logger.LogWarning("AudioStream is null");
            return;
        }

        if (!AudioStream.Started && enabledClients.Count > 0)
            StartAudioStream();
    }

    private void OnDisable()
    {
        StopAudioStreamNow();
    }

    private void OnDestroy()
    {
        foreach (var client in spawnedClients)
            Destroy(client.gameObject);

        StopAudioStreamNow();
        AudioStream?.Dispose();
    }

    public void StartAudioStream()
    {
        startRequested = true;
        stopRequested = false;
    }

    public void StopAudioStream()
    {
        stopRequested = true;
        startRequested = false;
    }

    private void StartAudioStreamNow()
    {
        startRequested = false;

        if (startStreamTask != null || AudioStream?.Started == true)
            return; // Already starting

        inactiveTimer = null;
        waitingForWarmup = true;

        foreach (var client in enabledClients)
        {
            Plugin.Logger.LogInfo($"Invoking OnHostStartRequested for client {client.Id}");
            client.OnHostStartRequested?.Invoke();
        }

        startStreamCts = new CancellationTokenSource();
        var token = startStreamCts.Token;
        startStreamTask = Task.Run(() =>
        {
            if (AudioStream == null)
                throw new InvalidOperationException("AudioStream is not set");

            try
            {
                AudioStream.Start();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Failed to start audio stream: {ex}");
                waitingForWarmup = false;
                return;
            }

            if (token.IsCancellationRequested || !AudioStream.Started)
            {
                waitingForWarmup = false;
                return;
            }

            AudioStream.WarmupReader();
            waitingForWarmup = false;
        });
    }

    private void StopAudioStreamNow()
    {
        if (startStreamCts != null)
        {
            Plugin.Logger.LogInfo("Cancelling start stream task");
            startStreamCts.Cancel();

            try
            {
                Plugin.Logger.LogInfo("Waiting for start stream task to complete");
                startStreamTask?.Wait();
            }
            catch (AggregateException)
            {
                // Ignore any exceptions thrown in task
            }

            startStreamTask = null;
            startStreamCts.Dispose();
            startStreamCts = null;
        }

        bool streamStarted = AudioStream?.Started == true;

        if (streamStarted)
            AudioStream!.Stop();

        audioSource.Stop();
        inactiveTimer = null;

        if (isActiveAndEnabled && streamStarted)
            OnStreamStopped?.Invoke();

        stopRequested = false;
        streamEnded = null;
    }

    private void CheckStartStreamTask()
    {
        if (startStreamTask == null)
            return;

        if (!startStreamTask.IsCompleted)
            return;

        if (startStreamTask.Exception != null)
        {
            Plugin.Logger.LogError("Error starting audio stream");
            Plugin.Logger.LogError(startStreamTask.Exception);
        }

        if (!audioSource.isPlaying)
            audioSource.Play();

        startStreamTask = null;
        startStreamCts?.Dispose();
        startStreamCts = null;

        streamEnded = false;
        OnStreamStarted?.Invoke();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        readingAudioData = true;

        if (AudioStream == null || !AudioStream.StreamAvailable || stopRequested || streamEnded == true || inactiveTimer >= MaxClientInactivityTime)
        {
            readingAudioData = false;
            return;
        }

        if (AudioStream.WaveFormat.Channels != channels)
        {
            // bepinex logger doesn't work here (doesn't work on audio thread i guess?), so use unity logger
            UnityEngine.Debug.LogError($"Channels mismatch: audio stream has {AudioStream.WaveFormat.Channels} channels but unity wants {channels}");
            readingAudioData = false;
            return;
        }

        if (AudioData == null || AudioData.Length < data.Length)
        {
            AudioData = new float[data.Length];
        }

        var numFloatsRead = AudioStream.Read(AudioData, 0, data.Length);
        AudioDataLength = numFloatsRead;

        if (streamEnded != null)
            streamEnded = numFloatsRead == 0;

        readingAudioData = false;
    }

    internal void OnClientEnabled(StreamAudioClient client)
    {
        enabledClients.Add(client);
        OnNumActiveClientsChanged();
    }

    internal void OnClientDisabled(StreamAudioClient client)
    {
        enabledClients.Remove(client);
        OnNumActiveClientsChanged();
    }

    private void OnNumActiveClientsChanged()
    {
        Plugin.Logger.LogInfo($"Num active clients: {enabledClients.Count}");

        if (enabledClients.Count > 0)
        {
            inactiveTimer = null;

            if (AudioStream == null || !AudioStream.Started)
            {
                Plugin.Logger.LogInfo("Starting audio stream");
                StartAudioStream();
            }
        }
        else if (enabledClients.Count == 0)
        {
            inactiveTimer = 0;
        }
    }
}
