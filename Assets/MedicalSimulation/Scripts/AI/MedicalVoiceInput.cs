using System;
using System.Collections;
using UnityEngine;

namespace MedicalSimulation.AI
{
    /// <summary>
    /// Manages microphone capture and Voice Activity Detection (VAD) for dictating
    /// questions to the AI assistant in desktop and VR headsets.
    /// Analyzes RMS audio amplitude in real-time to detect speech start and silence timeouts.
    /// </summary>
    public class MedicalVoiceInput : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private int maxRecordingDurationSec = 15;
        [SerializeField] private float silenceThreshold = 0.02f;
        [SerializeField] private float silenceTimeoutSeconds = 1.4f;

        [Header("State")]
        [SerializeField] private bool isRecording = false;
        [SerializeField] private float currentAudioLevel = 0f;

        public bool IsRecording => isRecording;
        public float CurrentAudioLevel => currentAudioLevel;

        public event Action<bool> OnRecordingStateChanged;
        public event Action<float> OnAudioLevelChanged;
        public event Action<AudioClip, float> OnSpeechRecorded;

        private AudioClip _recordedClip;
        private string _activeMicDevice;
        private float _silenceTimer = 0f;
        private bool _speechDetected = false;
        private Coroutine _recordingLoopCoroutine;

        public bool HasMicrophone => Microphone.devices != null && Microphone.devices.Length > 0;

        public void ToggleRecording()
        {
            if (isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        public void StartRecording()
        {
            if (isRecording) return;

            if (!HasMicrophone)
            {
                Debug.LogWarning("[MedicalVoiceInput] No microphone device detected on this system. Simulating speech input.");
                StartCoroutine(SimulateVoiceInputRoutine());
                return;
            }

            _activeMicDevice = Microphone.devices[0];
            _recordedClip = Microphone.Start(_activeMicDevice, false, maxRecordingDurationSec, sampleRate);
            isRecording = true;
            _speechDetected = false;
            _silenceTimer = 0f;

            OnRecordingStateChanged?.Invoke(true);

            if (_recordingLoopCoroutine != null) StopCoroutine(_recordingLoopCoroutine);
            _recordingLoopCoroutine = StartCoroutine(ProcessMicrophoneRoutine());
        }

        public void StopRecording()
        {
            if (!isRecording) return;

            int lastPos = 0;
            if (HasMicrophone && !string.IsNullOrEmpty(_activeMicDevice))
            {
                lastPos = Microphone.GetPosition(_activeMicDevice);
                Microphone.End(_activeMicDevice);
            }

            isRecording = false;
            if (_recordingLoopCoroutine != null)
            {
                StopCoroutine(_recordingLoopCoroutine);
                _recordingLoopCoroutine = null;
            }

            OnRecordingStateChanged?.Invoke(false);
            OnAudioLevelChanged?.Invoke(0f);

            float recordedDuration = (float)lastPos / sampleRate;
            if (recordedDuration > 0.3f && _recordedClip != null)
            {
                OnSpeechRecorded?.Invoke(_recordedClip, recordedDuration);
            }
        }

        private IEnumerator ProcessMicrophoneRoutine()
        {
            float[] sampleBuffer = new float[256];

            while (isRecording)
            {
                if (Microphone.IsRecording(_activeMicDevice))
                {
                    int micPos = Microphone.GetPosition(_activeMicDevice);
                    if (micPos > 256)
                    {
                        _recordedClip.GetData(sampleBuffer, micPos - 256);

                        // Calculate RMS (Root Mean Square) volume level
                        float sum = 0f;
                        for (int i = 0; i < sampleBuffer.Length; i++)
                        {
                            sum += sampleBuffer[i] * sampleBuffer[i];
                        }
                        float rms = Mathf.Sqrt(sum / sampleBuffer.Length);
                        currentAudioLevel = Mathf.Clamp01(rms * 10f); // amplified for UI meter
                        OnAudioLevelChanged?.Invoke(currentAudioLevel);

                        // VAD (Voice Activity Detection)
                        if (rms > silenceThreshold)
                        {
                            _speechDetected = true;
                            _silenceTimer = 0f;
                        }
                        else if (_speechDetected)
                        {
                            _silenceTimer += Time.deltaTime;
                            if (_silenceTimer >= silenceTimeoutSeconds)
                            {
                                // User finished speaking; automatically stop
                                StopRecording();
                                yield break;
                            }
                        }
                    }
                }
                else
                {
                    // Clip reached maximum recording duration
                    StopRecording();
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator SimulateVoiceInputRoutine()
        {
            isRecording = true;
            OnRecordingStateChanged?.Invoke(true);

            // Simulate sound wave animation
            float elapsed = 0f;
            while (elapsed < 2.0f)
            {
                elapsed += Time.deltaTime;
                float simulatedLevel = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.7f + UnityEngine.Random.Range(0f, 0.2f);
                OnAudioLevelChanged?.Invoke(simulatedLevel);
                yield return null;
            }

            isRecording = false;
            OnRecordingStateChanged?.Invoke(false);
            OnAudioLevelChanged?.Invoke(0f);

            // Dispatch simulated clip
            OnSpeechRecorded?.Invoke(null, 2.0f);
        }
    }
}
