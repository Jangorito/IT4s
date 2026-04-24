using System;
using UnityEngine;
using IT4s.Data;
using IT4s.Diagnostics;

// extOSC namespaces:
using extOSC;
using extOSC.Core;

namespace IT4s.Input
{
    /// <summary>
    /// Receives OSC hit messages from Bela and appends them to an in-memory HitBuffer.
    ///
    /// Expected message format:
    /// /it4/hit <tHigh:int32> <tLow:int32> <pad:int32> <vel:int32>
    ///
    /// Bela splits the 64-bit sample timestamp into high/low 32-bit words.
    /// Unity reconstructs it back into a signed 64-bit sample timestamp.
    /// </summary>
    public class OscHitReceiver : MonoBehaviour
    {
        [Header("OSC")]
        [SerializeField] private OSCReceiver receiver;
        [SerializeField] private string address = "/it4/hit";

        [Header("Buffer")]
        [SerializeField] private bool logEveryHit = false;
        [SerializeField] private int logEveryNHits = 1;

        [Header("Clock")]
        [SerializeField] private int sampleRate = 44100;

        private HitBuffer _buffer;
        private IOSCBind _hitBind;   // <-- store bind handle
        public HitBuffer Buffer => _buffer;

        private long _lastSamples;
        private bool _hasLastSamples;
        private double _lastSampleRealtimeSeconds;

        public bool HasLastSamples => _hasLastSamples;
        public long LastSamples => _lastSamples;
        public event Action<HitEvent> OnHitReceived;

        private void Awake()
        {
            _buffer = new HitBuffer();

            if (receiver == null)
            {
                Debug.LogError("[OscHitReceiver] No OSCReceiver assigned.");
                enabled = false;
                return;
            }

            RuntimeDebugLog.Log($"[OscHitReceiver] Listening on address={address}");
        }

        private void OnEnable()
        {
            if (receiver == null)
            {
                return;
            }

            _hitBind = receiver.Bind(address, OnHitMessage);
        }

        private void OnDisable()
        {
            if (receiver == null)
            {
                return;
            }

            if (_hitBind != null)
            {
                receiver.Unbind(_hitBind);
                _hitBind = null;
            }
        }

        private void OnHitMessage(OSCMessage message)
        {

            // Debug.Log($"[OscHitReceiver] RAW message received with {message.Values.Count} args");
            // Expected args:
            // 0 = tHigh (int32)
            // 1 = tLow  (int32)
            // 2 = pad   (int32)
            // 3 = vel   (int32)

            if (message.Values.Count < 4)
            {
                Debug.LogWarning($"[OscHitReceiver] Ignored malformed OSC hit. Expected 4 args, got {message.Values.Count}.");
                return;
            }

            int tHigh = ReadInt32(message.Values[0]);
            int tLow = ReadInt32(message.Values[1]);
            int pad = ReadInt32(message.Values[2]);
            int vel = ReadInt32(message.Values[3]);

            long tSamples = CombineHighLowToInt64(tHigh, tLow);

            _lastSamples = tSamples;
            _hasLastSamples = true;
            _lastSampleRealtimeSeconds = Time.realtimeSinceStartup;

            vel = Mathf.Clamp(vel, 0, 127);

            var hitEvent = new HitEvent(tSamples, pad, vel);
            _buffer.Add(hitEvent);
            OnHitReceived?.Invoke(hitEvent);

            if (logEveryHit || (logEveryNHits > 0 && (_buffer.Count % logEveryNHits) == 0))
            {
                RuntimeDebugLog.Log(
                    $"[OscHitReceiver] hits={_buffer.Count} last={tSamples} " +
                    $"(hi={tHigh}, lo={tLow}) pad={pad} vel={vel}"
                );
            }
        }

        private static long CombineHighLowToInt64(int high, int low)
        {
            // low must be treated as unsigned bits when recombining
            return ((long)high << 32) | (uint)low;
        }

        private static int ReadInt32(OSCValue v)
        {
            switch (v.Type)
            {
                case OSCValueType.Int:
                    return v.IntValue;

                case OSCValueType.Long:
                    return (int)v.LongValue;

                case OSCValueType.Float:
                    return Mathf.RoundToInt(v.FloatValue);

                default:
                    return 0;
            }
        }

        public bool TryGetCurrentSampleTime(out long currentSamples)
        {
            if (!_hasLastSamples)
            {
                currentSamples = 0;
                return false;
            }

            float elapsedSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - (float)_lastSampleRealtimeSeconds);
            currentSamples = _lastSamples + (long)(elapsedSeconds * sampleRate);
            return true;
        }
    }
}
