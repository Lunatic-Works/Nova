using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Image))]
    public class MusicFrequencyHistogram : MonoBehaviour
    {
        [Range(6, 10)] public int sampleResolution = 8;
        public int logSegments = 25;
        public float smoothTime = 0.1f;
        public float multiply = 50.0f;

        private AudioSource audioSource;
        private Image image;

        private float[] frequencies;
        private float[] logFrequencies;
        private float[] accLogFrequencies;
        private float[] currentSpeed;
        private Material material;
        private float frequencyBase;

        private static readonly int SegmentCountID = Shader.PropertyToID("_SegmentCount");
        private static readonly int SegmentsID = Shader.PropertyToID("_Segments");
        private static readonly int ScaleID = Shader.PropertyToID("_Scale");

        private void Awake()
        {
            var frequencySampleCount = 1 << sampleResolution;
            frequencies = new float[frequencySampleCount];
            logFrequencies = new float[logSegments];
            accLogFrequencies = new float[logSegments];
            currentSpeed = new float[logSegments];
            frequencyBase = Mathf.Pow(frequencySampleCount, 1.0f / logSegments);
            image = GetComponent<Image>();
            material = image.material;
            audioSource = GetComponentInParent<MusicGalleryPlayer>().audioSource;
        }

        private void OnDisable()
        {
            Array.Clear(accLogFrequencies, 0, accLogFrequencies.Length);
            Array.Clear(currentSpeed, 0, currentSpeed.Length);
            material.SetFloatArray(SegmentsID, accLogFrequencies);
        }

        private float GetInterpolatedFrequency(float index)
        {
            Assert.IsTrue(index >= 0);
            var i = Mathf.FloorToInt(index);
            return i >= frequencies.Length - 1
                ? frequencies[frequencies.Length - 1]
                : Mathf.LerpUnclamped(frequencies[i], frequencies[i + 1], index - i);
        }

        private void UpdateLogFrequencies()
        {
            var rangeA = 0.0f;
            var rangeB = frequencyBase - 1;
            Array.Clear(logFrequencies, 0, logFrequencies.Length);
            for (var i = 0; i < logFrequencies.Length; i++)
            {
                var ia = Mathf.Min(Mathf.FloorToInt(rangeA), frequencies.Length - 1);
                var ib = Mathf.Min(Mathf.FloorToInt(rangeB), frequencies.Length - 1);
                Assert.IsTrue(ia <= ib);
                var va = GetInterpolatedFrequency(rangeA);
                var vb = GetInterpolatedFrequency(rangeB);
                if (ia == ib)
                {
                    logFrequencies[i] = 0.5f * (va + vb) * (rangeB - rangeA);
                }
                else
                {
                    logFrequencies[i] =
                        0.5f * (frequencies[ia + 1] + va) * (ia + 1 - rangeA) +
                        0.5f * (frequencies[ib] + vb) * (rangeB - ib);
                    for (var j = ia + 1; j < ib; j++)
                    {
                        logFrequencies[i] += 0.5f * (frequencies[j] + frequencies[j + 1]);
                    }
                }

                rangeA = rangeB;
                rangeB = (rangeA + 1.0f) * frequencyBase - 1.0f;
            }
        }

        // Return 1/x, and smoothly drop to 0 when x -> 0
        private static float ReciprocalSmoothZero(float x, float threshold)
        {
            if (x > threshold)
            {
                return 1.0f / x;
            }

            return 1.0f / threshold * Mathf.Max(x / threshold * (1.0f + threshold) - threshold, 0.0f);
        }

        private void LateUpdate()
        {
            if (audioSource.isPlaying)
            {
                audioSource.GetSpectrumData(frequencies, 0, FFTWindow.Blackman);
                UpdateLogFrequencies();
            }
            else
            {
                Array.Clear(frequencies, 0, frequencies.Length);
                Array.Clear(logFrequencies, 0, logFrequencies.Length);
            }

            for (var i = 0; i < accLogFrequencies.Length; i++)
            {
                accLogFrequencies[i] = Mathf.SmoothDamp(accLogFrequencies[i], logFrequencies[i], ref currentSpeed[i],
                    smoothTime);
            }

            material.SetInt(SegmentCountID, accLogFrequencies.Length - 1);
            material.SetFloatArray(SegmentsID, accLogFrequencies);
            material.SetFloat(ScaleID, multiply * ReciprocalSmoothZero(audioSource.volume, 0.1f));
        }
    }
}
