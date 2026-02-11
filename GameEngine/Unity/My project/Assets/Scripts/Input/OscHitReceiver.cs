using UnityEngine;
using IT4s.Data;
using IT4s.Input;

// extOSC namespaces:
using extOSC;
using extOSC.Core;

namespace IT4s.Input
{
    /// <summary>
    /// Receives OSC hit messages from Bela and appends them to an in-memory HitBuffer.
    /// Expects: /it4/hit <tSamples:int64> <pad:int32> <vel:int32>
    /// </summary>
    public class OscHitReceiver : MonoBehaviour
    {
        [Header("OSC")]
        [SerializeField] private OSCReceiver receiver;
        [SerializeField] private string address = "/it4/hit";

        [Header("Buffer")]
        [SerializeField] private int logEveryNHits = 25;

        private HitBuffer _buffer;
        private IOSCBind _hitBind;   // <-- store bind handle
        public HitBuffer Buffer => _buffer;

        private long _lastSamples;
        private bool _hasLastSamples;

        public bool HasLastSamples => _hasLastSamples;
        public long LastSamples => _lastSamples;


        private void Awake()
        {
            _buffer = new HitBuffer();

            if (receiver == null)
            {
                Debug.LogError("[OscHitReceiver] No OSCReceiver assigned.");
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            _hitBind = receiver.Bind(address, OnHitMessage);
        }

        private void OnDisable()
        {
            if (_hitBind != null)
            {
                receiver.Unbind(_hitBind);
                _hitBind = null;
            }
        }

        private void OnHitMessage(OSCMessage message)
        {
            // Expected args: (long, int, int)
            // extOSC supports Int and Long types depending on what was sent.

            if (message.Values.Count < 3)
                return;

            long tSamples = ReadInt64(message.Values[0]);
            int pad = ReadInt32(message.Values[1]);
            int vel = ReadInt32(message.Values[2]);

            _lastSamples = tSamples;
            _hasLastSamples = true;


            // Clamp velocity into MIDI-ish 0..127 (Bela might send wider if you choose later)
            vel = Mathf.Clamp(vel, 0, 127);

            _buffer.Add(new HitEvent(tSamples, pad, vel));

            if (logEveryNHits > 0 && (_buffer.Count % logEveryNHits) == 0)
            {
                Debug.Log($"[OscHitReceiver] hits={_buffer.Count} last={tSamples} pad={pad} vel={vel}");
            }
        }

        private static int ReadInt32(OSCValue v)
        {
            // extOSC may encode ints as Int or Long depending on sender.
            switch (v.Type)
            {
                case OSCValueType.Int: return v.IntValue;
                case OSCValueType.Long: return (int)v.LongValue;
                case OSCValueType.Float: return Mathf.RoundToInt(v.FloatValue);
                default: return 0;
            }
        }

        private static long ReadInt64(OSCValue v)
        {
            switch (v.Type)
            {
                case OSCValueType.Long: return v.LongValue;
                case OSCValueType.Int: return v.IntValue;
                case OSCValueType.Float: return (long)Mathf.RoundToInt(v.FloatValue);
                default: return 0L;
            }
        }
    }
}
