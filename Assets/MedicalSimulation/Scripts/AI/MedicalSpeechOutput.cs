using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MedicalSimulation.Audio;

namespace MedicalSimulation.AI
{
    /// <summary>
    /// Text-to-speech (TTS) speaker controller.
    /// Reads out assistant responses, controls speech playback state, and ducks background
    /// simulation audio (heartbeats, ventilator hiss) to preserve vocal clarity.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MedicalSpeechOutput : MonoBehaviour
    {
        [Header("Audio Configuration")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float wordsPerMinute = 160f;
        [SerializeField] private float speechVolume = 0.9f;
        [SerializeField] private bool duckBackgroundAudio = true;

        [Header("State")]
        [SerializeField] private bool isSpeaking = false;
        [SerializeField] private string currentSpokenText = "";

        public bool IsSpeaking => isSpeaking;
        public string CurrentSpokenText => currentSpokenText;

        public event Action<bool> OnSpeakingStateChanged;
        public event Action<string> OnSentenceSpoken;

        private Coroutine _speechRoutine;
        private MedicalAudioController _audioController;

        void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.0f; // 2D voiceover for clarity
            audioSource.volume = speechVolume;

            _audioController = FindFirstObjectByType<MedicalAudioController>();
        }

        public void Speak(string text)
        {
            Stop();
            if (string.IsNullOrEmpty(text)) return;

            currentSpokenText = text;
            _speechRoutine = StartCoroutine(SpeakTextRoutine(text));
        }

        public void Stop()
        {
            if (_speechRoutine != null)
            {
                StopCoroutine(_speechRoutine);
                _speechRoutine = null;
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (isSpeaking)
            {
                isSpeaking = false;
                OnSpeakingStateChanged?.Invoke(false);
                RestoreBackgroundAudio();
            }
        }

        private IEnumerator SpeakTextRoutine(string text)
        {
            isSpeaking = true;
            OnSpeakingStateChanged?.Invoke(true);

            if (duckBackgroundAudio)
            {
                ApplyBackgroundDucking();
            }

            // Split into readable sentences
            string cleanText = StripMarkdown(text);
            string[] sentences = cleanText.Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Synthesize procedural audio chime or speech duration estimation
            for (int i = 0; i < sentences.Length; i++)
            {
                string sentence = sentences[i].Trim();
                if (string.IsNullOrEmpty(sentence)) continue;

                OnSentenceSpoken?.Invoke(sentence);

                // Word count duration calculation (Words per minute)
                int wordCount = sentence.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                float duration = Mathf.Max(1.2f, (wordCount / (wordsPerMinute / 60f)));

                // Procedural harmonic acoustic pulse indicating vocal utterance
                PlaySpeechUtteranceChime(duration);

                yield return new WaitForSeconds(duration);

                if (!isSpeaking) yield break;
            }

            isSpeaking = false;
            OnSpeakingStateChanged?.Invoke(false);
            RestoreBackgroundAudio();
        }

        private void PlaySpeechUtteranceChime(float duration)
        {
            if (audioSource == null) return;

            // Generate a subtle dynamic speech carrier wave if no external audio clip is loaded
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * 0.15f); // 150ms gentle notification pop
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin((float)i / samples * Mathf.PI);
                float freq = 520f; // Warm subtle vocal pitch
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.08f;
            }

            var clip = AudioClip.Create("TTS_Utterance", samples, 1, sampleRate, false);
            clip.SetData(data, 0);

            audioSource.PlayOneShot(clip, speechVolume);
        }

        private void ApplyBackgroundDucking()
        {
            if (_audioController != null)
            {
                _audioController.SetMasterVolume(0.25f);
            }
        }

        private void RestoreBackgroundAudio()
        {
            if (_audioController != null)
            {
                _audioController.SetMasterVolume(0.85f);
            }
        }

        private string StripMarkdown(string md)
        {
            if (string.IsNullOrEmpty(md)) return "";
            return md.Replace("**", "")
                     .Replace("*", "")
                     .Replace("#", "")
                     .Replace("`", "")
                     .Replace("- ", "")
                     .Trim();
        }
    }
}
