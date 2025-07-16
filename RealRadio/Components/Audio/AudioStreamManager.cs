using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using RealRadio.Components.Audio.HostControllers;
using RealRadio.Components.Radio;
using RealRadio.Data;
using UnityEngine;

namespace RealRadio.Components.Audio;

public class AudioStreamManager : MonoBehaviour
{
    public IReadOnlyCollection<StreamAudioHost> Hosts => hosts.Values;

    public uint MaxActiveInaudibleHosts = 5;

    public int NumActiveHosts => activeHosts.Count;

    private Dictionary<string, StreamAudioHost> hosts = new();
    private LinkedList<StreamAudioHost> activeHosts = new();
    private StreamAudioHost[]? activeHostsBuffer;

    public static AudioStreamManager Instance => GetOrInitInstance();
    private static AudioStreamManager? cachedInstance;

    private static AudioStreamManager GetOrInitInstance()
    {
        if (!cachedInstance)
            cachedInstance = null;

        cachedInstance ??= FindObjectOfType<AudioStreamManager>() ?? new GameObject("AudioStreamManager").AddComponent<AudioStreamManager>();
        return cachedInstance;
    }

    public StreamAudioHost GetOrCreateHost(RadioStation station)
    {
        if (station?.Id == null)
            throw new ArgumentNullException(nameof(station.Id), "Station or station id is null");

        if (hosts.TryGetValue(station.Id, out var host))
            return host;

        host = CreateHost(station);
        hosts.Add(station.Id, host);
        return host;
    }

    /// <summary>
    /// Removes a host from the manager.
    ///
    /// Note: Don't call this. It's called automatically when an audio host component is removed.
    /// </summary>
    /// <param name="host"></param>
    public void RemoveHost(StreamAudioHost host)
    {
        // optimize this? probably not necessary...
        string? key = hosts.FirstOrDefault(x => x.Value == host).Key;

        if (key != null)
        {
            hosts.Remove(key);
        }

        activeHosts.Remove(host);
    }

    private StreamAudioHost CreateHost(RadioStation station)
    {
        if (station == null)
            throw new ArgumentNullException(nameof(station));

        var go = new GameObject("AudioStreamHost");
        go.SetActive(false);
        go.transform.SetParent(transform);

        var host = go.AddComponent<StreamAudioHost>();

        HostController controller;

        switch (station.Type)
        {
            case RadioType.InternetRadio:
                controller = go.AddComponent<InternetRadioHostController>();
                break;
            case RadioType.YtDlp:
                controller = go.AddComponent<YtDlpHostController>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(station.Type), station.Type, "Unknown radio type");
        }

        controller.Station = station;

        host.OnStreamStarted += () => OnHostStreamToggled(host, true);
        host.OnStreamStopped += () => OnHostStreamToggled(host, false);

        go.SetActive(true);

        return host;
    }

    private void OnHostStreamToggled(StreamAudioHost host, bool started)
    {
        if (started && !activeHosts.Contains(host))
        {
            activeHosts.AddLast(host);
        }
        else if (!started && activeHosts.Contains(host))
        {
            activeHosts.Remove(host);
        }
    }

    private void Awake()
    {
        if (cachedInstance)
        {
            Logger.LogWarning("An instance of AudioStreamManager already exists");
            Destroy(this);
            return;
        }

        RadioStationManager.Instance.StationUpdated += OnRadioStationUpdated;
        RadioStationManager.Instance.StationRemoved += OnRadioStationRemoved;
    }

    private void OnRadioStationUpdated(RadioStation station, RadioStation? oldStation)
    {
        if (oldStation == null)
            return;

        if (!hosts.Remove(oldStation.Id!, out var oldHost))
            return;

        hosts.Add(station.Id!, oldHost);
        oldHost.GetComponent<HostController>().Station = station;
        Logger.LogInfo($"Updated AudioStreamManager hosts dictionary for radio station {station.Id}");
    }

    private void OnRadioStationRemoved(RadioStation station)
    {
        if (hosts.Remove(station.Id!, out var host))
        {
            foreach (var client in host.SpawnedClients.ToList())
            {
                host.DetachClient(client.gameObject);
            }

            if (host)
                Destroy(host.gameObject);

            Logger.LogInfo($"Removed AudioStreamManager host for radio station {station.Id}");
        }
    }

    private void Update()
    {
        int numActiveHosts = NumActiveHosts;

        if (numActiveHosts > MaxActiveInaudibleHosts)
        {
            if (activeHostsBuffer == null || activeHostsBuffer.Length < numActiveHosts)
            {
                activeHostsBuffer = new StreamAudioHost[numActiveHosts];
            }

            Logger.LogInfo($"activeHosts len: {activeHosts.Count}, numActiveHosts: {numActiveHosts}, activeHostsBuffer len: {activeHostsBuffer.Length}");
            activeHosts.CopyTo(activeHostsBuffer, 0);

            foreach (var host in activeHostsBuffer.Take(activeHosts.Count))
            {
                if (host.NumActiveClients == 0)
                {
                    Logger.LogInfo($"Killing quiet audio host before inactive timer expires due to too many active hosts");

                    host.StopAudioStream();
                    --numActiveHosts;

                    if (numActiveHosts <= MaxActiveInaudibleHosts)
                        break;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (cachedInstance == this)
        {
            cachedInstance = null;
        }

        RadioStationManager.Instance.StationUpdated -= OnRadioStationUpdated;

        foreach (var host in hosts.Values)
            Destroy(host.gameObject);
    }
}
