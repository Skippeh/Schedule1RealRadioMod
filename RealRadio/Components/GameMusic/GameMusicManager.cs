using System;
using System.Collections;
using System.Collections.Generic;
using RealRadio.Components.Audio;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace RealRadio.GameMusic;

public class GameMusicManager : Singleton<GameMusicManager>
{
    private Dictionary<MusicTrack, AudioSourceController> activeMusicTracks = [];
    private StreamAudioClient? globalClient;
    private float? currentVolume;

    public override void Awake()
    {
        // Disable ambient music
        foreach (var ambientTrack in FindObjectsOfType<AmbientTrack>())
        {
            ambientTrack.gameObject.SetActive(false);
        }

        MusicTrackPatches.MusicTrackToggled += OnMusicTrackToggled;
        MusicTrackPatches.MusicTrackPlay += OnMusicTrackPlay;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        MusicTrackPatches.MusicTrackToggled -= OnMusicTrackToggled;
        MusicTrackPatches.MusicTrackPlay -= OnMusicTrackPlay;
    }

    private void OnMusicTrackToggled(MusicTrack track, bool enabled)
    {
        bool isActiveTrack = activeMusicTracks.ContainsKey(track);

        if (enabled && !isActiveTrack)
        {
            activeMusicTracks.Add(track, track.Controller);
        }
        else if (!enabled && isActiveTrack)
        {
            if (track.FadeOutTime > 0f)
            {
                StartCoroutine(RemoveTrackAfterDelay(track, track.FadeOutTime));
            }
            else
            {
                RemoveTrack(track);
            }
        }
    }

    private void OnMusicTrackPlay(MusicTrack track)
    {
        activeMusicTracks.TryAdd(track, track.Controller);
    }

    private IEnumerator RemoveTrackAfterDelay(MusicTrack track, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveTrack(track);
    }

    private void RemoveTrack(MusicTrack track)
    {
        activeMusicTracks.Remove(track);
    }

    private void Update()
    {
        if (globalClient != null)
        {
            if (
                !globalClient.enabled ||
                !globalClient.AudioSource.isPlaying ||
                globalClient.Host?.AudioStream is null or { Started: false } ||
                globalClient.AudioSource.spatialBlend > 0f ||
                globalClient.AudioSource.volume <= 0f ||
                globalClient.AudioSource.mute
            )
            {
                globalClient = null;
            }
        }

        if (globalClient == null)
        {
            foreach (var host in AudioStreamManager.Instance.Hosts)
            {
                if (host.NumActiveClients == 0)
                    continue;

                foreach (var client in host.ActiveClients)
                {
                    if (
                        client.AudioSource.volume <= 0f ||
                        client.AudioSource.mute ||
                        client.AudioSource.spatialBlend > 0f
                    )
                    {
                        continue;
                    }

                    globalClient = client;
                    break;
                }
            }
        }
    }

    private void LateUpdate()
    {
        float volumeTarget = globalClient == null ? 1f : 0f;

        if (currentVolume == null)
        {
            currentVolume = volumeTarget;
        }

        if (Mathf.Approximately(currentVolume.Value, volumeTarget) && volumeTarget == 1f)
            return;

        foreach (var (track, controller) in activeMusicTracks)
        {
            // Lerp volume towards volumeTarget if target volume is higher
            if (currentVolume < volumeTarget)
            {
                currentVolume = Mathf.Min(currentVolume.Value + (Time.unscaledDeltaTime / 3f), volumeTarget);
            }
            else if (currentVolume > volumeTarget)
            {
                // Set volume to volumeTarget if target volume is lower
                currentVolume = volumeTarget;
            }

            controller.VolumeMultiplier = currentVolume.Value;
            controller.ApplyVolume();
        }
    }
}
