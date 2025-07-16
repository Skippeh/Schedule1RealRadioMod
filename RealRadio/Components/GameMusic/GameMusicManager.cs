using System.Collections;
using System.Collections.Generic;
using RealRadio.Components.Audio;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using UnityEngine;

namespace RealRadio.GameMusic;

public class GameMusicManager : Singleton<GameMusicManager>
{
    private Dictionary<MusicTrack, AudioSourceController> activeMusicTracks = [];
    private HashSet<MusicTrack> soonStopping = [];
    private StreamAudioClient? globalClient;
    private float currentVolume;

    public override void Awake()
    {
        // Disable ambient music
        foreach (var ambientTrack in FindObjectsOfType<AmbientTrack>())
        {
            ambientTrack.gameObject.SetActive(false);
        }

        GameEvents.MusicTrackToggled += OnMusicTrackToggled;
        GameEvents.MusicTrackPlay += OnMusicTrackPlay;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        GameEvents.MusicTrackToggled -= OnMusicTrackToggled;
        GameEvents.MusicTrackPlay -= OnMusicTrackPlay;
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
        soonStopping.Add(track);
        track.volumeMultiplier = currentVolume;
        yield return new WaitForSeconds(delay);
        RemoveTrack(track);
    }

    private void RemoveTrack(MusicTrack track)
    {
        activeMusicTracks.Remove(track);
        soonStopping.Remove(track);
    }

    private void Update()
    {
        if (globalClient != null)
        {
            if (
                IsClientQuietOrSpatial(globalClient)
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
                        IsClientQuietOrSpatial(client)
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

    private static bool IsClientQuietOrSpatial(StreamAudioClient client)
    {
        return !client.enabled ||
            !client.AudioSource.isPlaying ||
            client.Host?.AudioStream is null or { Started: false } ||
            client.AudioSource.spatialBlend > 0f ||
            client.AudioSource.volume <= 0f ||
            client.AudioSource.mute;
    }

    private void LateUpdate()
    {
        float volumeTarget = globalClient == null ? 1f : 0f;

        foreach (var (track, controller) in activeMusicTracks)
        {
            if (soonStopping.Contains(track))
                continue;

            // Lerp volume towards volumeTarget if target volume is higher
            if (currentVolume < volumeTarget)
            {
                currentVolume = Mathf.Min(currentVolume + (Time.unscaledDeltaTime / 3f), volumeTarget);
            }
            else if (currentVolume > volumeTarget)
            {
                // Set volume to volumeTarget if target volume is lower
                currentVolume = volumeTarget;
            }

            controller.VolumeMultiplier = track.VolumeMultiplier * currentVolume;
            controller.ApplyVolume();

            if (track is StartLoopMusicTrack loopTrack && loopTrack.IsPlaying)
            {
                loopTrack.LoopSound.VolumeMultiplier = track.VolumeMultiplier * currentVolume;
                loopTrack.LoopSound.ApplyVolume();
            }
        }
    }
}
