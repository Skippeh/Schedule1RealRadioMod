using System;
using ScheduleOne.Audio;
using UnityEngine;

namespace RealRadio.Components.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class StreamAudioClient : MonoBehaviour
    {
        public bool ConvertToMono { get; set; }
        public float LeftChannelVolume { get; set; } = 1f;
        public float RightChannelVolume { get; set; } = 1f;

        public StreamAudioHost? Host
        {
            get => host;
            set
            {
                if (host != null)
                {
                    host.OnStreamStarted -= OnHostStreamStarted;
                    host.OnStreamStopped -= OnHostStreamStopped;
                }

                host = value;

                if (host != null)
                {
                    host.OnStreamStarted += OnHostStreamStarted;
                    host.OnStreamStopped += OnHostStreamStopped;
                }
            }
        }

        public int Id { get; set; }

        public AudioSource AudioSource => audioSource;

        public Action? OnHostStreamStarted;
        public Action? OnHostStreamStopped;
        public Action? OnHostStartRequested;

        private StreamAudioHost? host;
        private bool initialized;
        private AudioSource audioSource = null!;
        private bool hostEnabled;
        private AudioSourceController? audioSourceController;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>() ?? throw new InvalidOperationException("No AudioSource component found on game object");

            int numChannels = 1;
            audioSource.clip = AudioClip.Create("Dummy", 2048, numChannels, AudioSettings.GetSampleRate(), false);
            audioSource.loop = true;
            float[] audioData = new float[2048 * numChannels];
            Array.Fill(audioData, 1);
            audioSource.clip.SetData(audioData, 0);

            audioSourceController = GetComponent<AudioSourceController>();

            // Set some sane defaults on the audio source
            /*audioSource.volume = 0.1f;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CreateLogarithmicRolloffCurve(maxDistance: 30f));
            audioSource.maxDistance = 30f;
            audioSource.dopplerLevel = 0.25f;*/
        }

        private AnimationCurve CreateLogarithmicRolloffCurve(float maxDistance)
        {
            var keyFrames = new Keyframe[(int)Math.Floor(maxDistance) * 2 + 1];

            float distancePerKeyframe = maxDistance / (keyFrames.Length - 1);

            for (int i = 0; i < keyFrames.Length; ++i)
            {
                float distance = i * distancePerKeyframe;
                //float value = 1f / distance;
                float value = 1f - Mathf.Log10(distance) / Mathf.Log10(maxDistance);
                value = Math.Clamp(value, 0f, 1f); // save my ears from potential ruin
                keyFrames[i] = new Keyframe(distance, value);
            }

            return new AnimationCurve(keyFrames);
        }

        private void Start()
        {
            if (!initialized)
            {
                initialized = true;
                OnEnable();
            }
        }

        private void OnEnable()
        {
            if (!initialized || host == null)
                return;

            Host?.OnClientEnabled(this);
            audioSource.Play();

            if (audioSourceController != null)
                audioSourceController.isPlayingCached = true;
        }

        private void OnDisable()
        {
            if (!initialized)
                return;

            audioSource.Stop();
            Host?.OnClientDisabled(this);
        }

        private void OnDestroy()
        {
            Host?.DestroyClient(this);
        }

        private void Update()
        {
            hostEnabled = host?.enabled ?? false;

            if (hostEnabled && !audioSource.isPlaying)
            {
                audioSource.Play();

                if (audioSourceController != null)
                    audioSourceController.isPlayingCached = true;
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (host == null || host.AudioData == null || !hostEnabled || host.AudioStream?.StreamAvailable != true)
            {
                Array.Fill(data, 0);
                return;
            }

            if (!ConvertToMono)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    data[i] *= host.AudioData[i];

                    if (i % 2 == 0 && !Mathf.Approximately(LeftChannelVolume, 1f))
                    {
                        data[i] *= LeftChannelVolume;
                    }
                    else if (i % 2 == 1 && !Mathf.Approximately(RightChannelVolume, 1f))
                    {
                        data[i] *= RightChannelVolume;
                    }
                }
            }
            else
            {
                for (int i = 0; i < data.Length / channels; ++i)
                {
                    float average = 0;

                    for (int j = 0; j < channels; ++j)
                    {
                        float channelValue = host.AudioData[i * channels + j];

                        if (j % 2 == 0 && !Mathf.Approximately(LeftChannelVolume, 1f))
                        {
                            channelValue *= LeftChannelVolume;
                        }
                        else if (j % 2 == 1 && !Mathf.Approximately(RightChannelVolume, 1f))
                        {
                            channelValue *= RightChannelVolume;
                        }

                        channelValue /= channels;
                        average += channelValue;
                    }

                    for (int j = 0; j < channels; ++j)
                    {
                        data[i * channels + j] *= average;
                    }
                }
            }
        }
    }
}
