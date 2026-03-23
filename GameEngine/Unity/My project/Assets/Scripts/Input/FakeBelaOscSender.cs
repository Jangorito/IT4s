using UnityEngine;
using extOSC;

namespace IT4s.Input
{
    /// <summary>
    /// Sends fake Bela-style hit messages into an OSC target
    /// (typically your local OSCReceiver).
    ///
    /// Message format:
    /// /it4/hit <tHigh:int32> <tLow:int32> <pad:int32> <vel:int32>
    ///
    /// This matches the live Bela contract where the 64-bit sample timestamp
    /// is split into two 32-bit integers.
    /// </summary>
    public class FakeBelaOscSender : MonoBehaviour
    {
        [Header("OSC")]
        [SerializeField] private OSCTransmitter transmitter;
        [SerializeField] private string address = "/it4/hit";

        [Header("Fake hit settings")]
        [SerializeField] private int pad = 0;
        [SerializeField] private int minVel = 60;
        [SerializeField] private int maxVel = 110;
        [SerializeField] private float hitsPerSecond = 4f;
        [SerializeField] private bool sendOnStart = true;

        [Header("Clock")]
        [SerializeField] private int sampleRate = 44100;

        private long _tSamples;
        private float _accum;

        private void Awake()
        {
            if (transmitter == null)
            {
                Debug.LogError("[FakeBelaOscSender] No OSCTransmitter assigned.");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            _tSamples = 0;
            _accum = 0f;

            if (sendOnStart)
            {
                SendHit();
            }

            Debug.Log($"[FakeBelaOscSender] Started. Sending fake hits to {transmitter.RemoteHost}:{transmitter.RemotePort}");
        }

        private void Update()
        {
            // Advance fake Bela sample clock
            _tSamples += (long)(sampleRate * Time.deltaTime);

            if (hitsPerSecond > 0f)
            {
                _accum += Time.deltaTime;
                float interval = 1f / hitsPerSecond;

                while (_accum >= interval)
                {
                    _accum -= interval;
                    SendHit();
                }
            }

            // Manual trigger
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) SendHit();
        }

        private void SendHit()
        {
            int vel = Random.Range(minVel, maxVel + 1);
            vel = Mathf.Clamp(vel, 0, 127);

            SplitInt64(_tSamples, out int tHigh, out int tLow);

            var msg = new OSCMessage(address);
            msg.AddValue(OSCValue.Int(tHigh));
            msg.AddValue(OSCValue.Int(tLow));
            msg.AddValue(OSCValue.Int(pad));
            msg.AddValue(OSCValue.Int(vel));

            transmitter.Send(msg);

            // Optional debug
            // Debug.Log($"[FakeBelaOscSender] sent t={_tSamples} hi={tHigh} lo={tLow} pad={pad} vel={vel}");
        }

        private static void SplitInt64(long value, out int high, out int low)
        {
            high = (int)(value >> 32);
            low = (int)(value & 0xFFFFFFFF);
        }
    }
}
