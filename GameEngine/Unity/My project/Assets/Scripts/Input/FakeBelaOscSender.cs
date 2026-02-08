using UnityEngine;
using extOSC;

namespace IT4s.Input
{
    /// <summary>
    /// Sends fake Bela-style hit messages into an OSCTarget (typically your local OSCReceiver).
    /// Use this to test the pipeline without hardware.
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
        [SerializeField] private float hitsPerSecond = 4f; // 4 = quarter notes at 60bpm feel
        [SerializeField] private bool sendOnStart = true;

        // We'll emulate Bela sample-time
        [Header("Clock")]
        [SerializeField] private int sampleRate = 48000;
        private long _tSamples;
        private float _accum;

        private void Awake()
        {
            if (transmitter == null)
            {
                Debug.LogError("[FakeBelaOscSender] No OSCTransmitter assigned.");
                enabled = false;
            }
        }

        private void Start()
        {
            _tSamples = 0;
            _accum = 0f;

            if (sendOnStart)
                SendHit(); // one immediate hit to prove wiring

            Debug.Log("[FakeBelaOscSender] Started. Sending fake hits to " + transmitter.RemoteHost + ":" + transmitter.RemotePort);
        }

        private void Update()
        {
            // advance sample clock
            _tSamples += (long)(sampleRate * Time.deltaTime);

            if (hitsPerSecond <= 0f) return;

            _accum += Time.deltaTime;
            float interval = 1f / hitsPerSecond;

            while (_accum >= interval)
            {
                _accum -= interval;
                SendHit();
            }

            // Manual trigger: space bar
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
                SendHit();
        }

        private void SendHit()
        {
            int vel = Random.Range(minVel, maxVel + 1);

            var msg = new OSCMessage(address);

            // IMPORTANT: extOSC value types.
            // Use Long for tSamples (int64), Int for pad/vel (int32).
            msg.AddValue(OSCValue.Long(_tSamples));
            msg.AddValue(OSCValue.Int(pad));
            msg.AddValue(OSCValue.Int(vel));

            transmitter.Send(msg);

            // Optional local log
            // Debug.Log($"[FakeBelaOscSender] sent t={_tSamples} pad={pad} vel={vel}");
        }
    }
}
