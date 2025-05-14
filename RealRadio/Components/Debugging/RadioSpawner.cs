using System;
using System.Collections;
using System.Linq;
using AudioStreamer;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using RealRadio.Components.Audio;
using RealRadio.Components.Radio;
using ScheduleOne.NPCs;
using UnityEngine;

namespace RealRadio.Components.Debugging;

public class RadioSpawner : MonoBehaviour
{
    private StreamAudioHost? audioHost;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Plugin.Logger.LogInfo("Try spawn radio");
            TrySpawnRadio();
        }
    }

    private void TrySpawnRadio()
    {
        var cameraTransform = Camera.main.transform;
        var lookDirection = cameraTransform.forward;
        var ray = new Ray(cameraTransform.position, lookDirection);
        var layerMask = Layers.All;
        layerMask &= ~Layers.Player;

        if (Physics.Raycast(ray, out var hit, 10f, layerMask.ToLayerMask()))
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(hit.transform, worldPositionStays: true);
            go.transform.localScale = Vector3.one * 0.2f;
            go.transform.position = hit.point;
            go.transform.rotation.SetLookRotation(lookDirection);

            var audioHost = GetOrCreateAudioHost();
            var audioClient = audioHost.CreateClient(parent: go.transform);
            audioClient.ConvertToMono = true;

            Plugin.Logger.LogInfo($"Spawned radio at {go.transform.position} (hit {hit.transform.gameObject.name}, layer {hit.collider.gameObject.layer})");
        }
    }

    private StreamAudioHost GetOrCreateAudioHost()
    {
        if (audioHost == null)
        {
            audioHost = AudioStreamManager.Instance.GetOrCreateHost(RadioStationManager.Instance.Stations.First());
        }

        return audioHost;
    }
}
