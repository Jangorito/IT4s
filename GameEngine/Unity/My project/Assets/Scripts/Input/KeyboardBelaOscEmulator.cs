using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using extOSC;

namespace IT4s.Input
{
    /// <summary>
    /// Determines how held keyboard keys should emit synthetic Bela hits.
    /// </summary>
    public enum KeyboardOscRepeatPolicy
    {
        KeyDownOnly = 0,
        RepeatWhileHeld = 1
    }

    /// <summary>
    /// Maps one keyboard key to one Bela-style pad hit definition.
    /// </summary>
    [Serializable]
    public sealed class KeyboardBelaOscMapping
    {
        [Tooltip("Keyboard key that will emit a synthetic Bela hit.")]
        public KeyCode key = KeyCode.None;

        [Tooltip("Pad id sent in the OSC payload.")]
        public int pad;

        [Tooltip("Per-key velocity override. Uses the emulator default when <= 0.")]
        public int velocityOverride = 0;
    }

    /// <summary>
    /// Sends Bela-compatible OSC hit messages from keyboard presses so the existing
    /// Unity input pipeline can be tested without Bela hardware.
    /// </summary>
    [AddComponentMenu("IT4s/Input/Keyboard Bela OSC Emulator")]
    [DisallowMultipleComponent]
    public class KeyboardBelaOscEmulator : MonoBehaviour
    {
        private const string DefaultRemoteHost = "127.0.0.1";
        private const string DefaultOscAddress = "/it4/hit";
        private const int DefaultRemotePort = 9000;
        private const int DefaultSampleRate = 44100;

        [Header("OSC")]
        [Tooltip("Optional transmitter reference. If omitted, one is created or reused on this GameObject.")]
        [SerializeField] private OSCTransmitter transmitter;

        [Tooltip("OSC destination IP. extOSC in this project expects an IP literal rather than a hostname.")]
        [SerializeField] private string remoteHost = DefaultRemoteHost;

        [Min(1)]
        [SerializeField] private int remotePort = DefaultRemotePort;

        [SerializeField] private string oscAddress = DefaultOscAddress;

        [Header("Timing")]
        [Min(1)]
        [SerializeField] private int sampleRate = DefaultSampleRate;

        [Header("Hits")]
        [Range(0, 127)]
        [SerializeField] private int defaultVelocity = 100;

        [SerializeField] private KeyboardOscRepeatPolicy repeatPolicy = KeyboardOscRepeatPolicy.KeyDownOnly;

        [Min(0.01f)]
        [SerializeField] private float heldRepeatIntervalSeconds = 0.10f;

        [SerializeField] private bool verboseLogging = false;

        [Header("Key Mappings")]
        [SerializeField] private List<KeyboardBelaOscMapping> keyMappings = new();

        private readonly List<KeyboardBelaOscMapping> _activeMappings = new();
        private readonly Dictionary<KeyCode, double> _nextRepeatTimes = new();

        private double _startRealtimeSeconds;
        private long _lastTimestampSamples = -1;
        private bool _configurationValid;
        private bool _transmitterWasAutoCreated;

        private void Reset()
        {
            remoteHost = DefaultRemoteHost;
            remotePort = DefaultRemotePort;
            oscAddress = DefaultOscAddress;
            sampleRate = DefaultSampleRate;
            defaultVelocity = 100;
            repeatPolicy = KeyboardOscRepeatPolicy.KeyDownOnly;
            heldRepeatIntervalSeconds = 0.10f;
            verboseLogging = false;

            if (keyMappings == null || keyMappings.Count == 0)
            {
                keyMappings = new List<KeyboardBelaOscMapping>
                {
                    new KeyboardBelaOscMapping { key = KeyCode.A, pad = 0, velocityOverride = 72 },
                    new KeyboardBelaOscMapping { key = KeyCode.S, pad = 1, velocityOverride = 90 },
                    new KeyboardBelaOscMapping { key = KeyCode.D, pad = 2, velocityOverride = 108 },
                    new KeyboardBelaOscMapping { key = KeyCode.F, pad = 3, velocityOverride = 124 }
                };
            }

            EnsureTransmitterReference(logWarnings: false);
            ConfigureAutoCreatedTransmitterDefaults();
            RebuildActiveMappings(logWarnings: false);
        }

        private void Awake()
        {
            if (keyMappings == null)
            {
                keyMappings = new List<KeyboardBelaOscMapping>();
            }
        }

        private void OnEnable()
        {
            ResetSyntheticClock();
            RefreshConfiguration(logWarnings: true);
        }

        private void OnDisable()
        {
            _nextRepeatTimes.Clear();
        }

        private void OnValidate()
        {
            defaultVelocity = Mathf.Clamp(defaultVelocity, 0, 127);
            heldRepeatIntervalSeconds = Mathf.Max(0.01f, heldRepeatIntervalSeconds);

            if (keyMappings == null)
            {
                keyMappings = new List<KeyboardBelaOscMapping>();
            }

            RebuildActiveMappings(logWarnings: false);

            if (Application.isPlaying)
            {
                RefreshConfiguration(logWarnings: true);
            }
        }

        private void Update()
        {
            if (!_configurationValid)
            {
                return;
            }

            if (repeatPolicy == KeyboardOscRepeatPolicy.KeyDownOnly)
            {
                ProcessKeyDownMappings();
                return;
            }

            ProcessRepeatMappings();
        }

        /// <summary>
        /// Builds a synthetic Bela-style sample timestamp from real elapsed time.
        /// </summary>
        private long BuildCurrentSampleTimestamp()
        {
            double elapsedSeconds = Math.Max(0.0d, Time.realtimeSinceStartupAsDouble - _startRealtimeSeconds);
            long candidate = (long)Math.Floor(elapsedSeconds * sampleRate);

            if (candidate <= _lastTimestampSamples)
            {
                candidate = _lastTimestampSamples + 1L;
            }

            _lastTimestampSamples = candidate;
            return candidate;
        }

        /// <summary>
        /// Splits a signed 64-bit sample timestamp into two OSC-safe 32-bit integers.
        /// </summary>
        private static void SplitLongToHiLo(long value, out int hi, out int lo)
        {
            hi = unchecked((int)(value >> 32));
            lo = unchecked((int)(value & 0xFFFFFFFFL));
        }

        /// <summary>
        /// Sends one Bela-compatible hit message using the existing Unity OSC contract.
        /// </summary>
        private bool SendHit(int pad, int velocity)
        {
            return SendHit(KeyCode.None, pad, velocity);
        }

        private bool TryGetVelocity(KeyboardBelaOscMapping mapping, out int velocity)
        {
            velocity = mapping != null && mapping.velocityOverride > 0
                ? mapping.velocityOverride
                : defaultVelocity;

            velocity = Mathf.Clamp(velocity, 0, 127);

            if (velocity <= 0)
            {
                Debug.LogWarning(
                    $"[KeyboardBelaOscEmulator] Ignored key {mapping?.key} because its resolved velocity was {velocity}."
                );
                return false;
            }

            return true;
        }

        private bool SendHit(KeyCode key, int pad, int velocity)
        {
            if (!EnsureTransmitterReady(logWarnings: true))
            {
                return false;
            }

            long timestamp = BuildCurrentSampleTimestamp();
            SplitLongToHiLo(timestamp, out int hi, out int lo);

            var message = new OSCMessage(oscAddress);
            message.AddValue(OSCValue.Int(hi));
            message.AddValue(OSCValue.Int(lo));
            message.AddValue(OSCValue.Int(pad));
            message.AddValue(OSCValue.Int(velocity));

            transmitter.Send(message);

            if (verboseLogging)
            {
                Debug.Log(
                    $"[KeyboardBelaOscEmulator] key={key} ts={timestamp} hi={hi} lo={lo} pad={pad} vel={velocity}"
                );
            }

            return true;
        }

        private void ProcessKeyDownMappings()
        {
            for (int i = 0; i < _activeMappings.Count; i++)
            {
                KeyboardBelaOscMapping mapping = _activeMappings[i];
                if (!UnityEngine.Input.GetKeyDown(mapping.key))
                {
                    continue;
                }

                TriggerMapping(mapping);
            }
        }

        private void ProcessRepeatMappings()
        {
            double now = Time.realtimeSinceStartupAsDouble;

            for (int i = 0; i < _activeMappings.Count; i++)
            {
                KeyboardBelaOscMapping mapping = _activeMappings[i];

                if (UnityEngine.Input.GetKeyDown(mapping.key))
                {
                    if (TriggerMapping(mapping))
                    {
                        _nextRepeatTimes[mapping.key] = now + heldRepeatIntervalSeconds;
                    }

                    continue;
                }

                if (!UnityEngine.Input.GetKey(mapping.key))
                {
                    _nextRepeatTimes.Remove(mapping.key);
                    continue;
                }

                if (!_nextRepeatTimes.TryGetValue(mapping.key, out double nextRepeatTime))
                {
                    continue;
                }

                if (now < nextRepeatTime)
                {
                    continue;
                }

                if (TriggerMapping(mapping))
                {
                    _nextRepeatTimes[mapping.key] = now + heldRepeatIntervalSeconds;
                }
            }
        }

        private bool TriggerMapping(KeyboardBelaOscMapping mapping)
        {
            if (!TryGetVelocity(mapping, out int velocity))
            {
                return false;
            }

            return SendHit(mapping.key, mapping.pad, velocity);
        }

        private void ResetSyntheticClock()
        {
            _startRealtimeSeconds = Time.realtimeSinceStartupAsDouble;
            _lastTimestampSamples = -1L;
            _nextRepeatTimes.Clear();
        }

        private void RefreshConfiguration(bool logWarnings)
        {
            RebuildActiveMappings(logWarnings);
            _configurationValid = ValidateConfiguration(logWarnings) && EnsureTransmitterReady(logWarnings);
        }

        private bool ValidateConfiguration(bool logWarnings)
        {
            if (sampleRate <= 0)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[KeyboardBelaOscEmulator] sampleRate must be greater than zero.");
                }

                return false;
            }

            if (string.IsNullOrWhiteSpace(oscAddress))
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[KeyboardBelaOscEmulator] OSC address is blank. No hits will be sent.");
                }

                return false;
            }

            if (!IPAddress.TryParse(remoteHost, out _))
            {
                if (logWarnings)
                {
                    Debug.LogWarning(
                        $"[KeyboardBelaOscEmulator] remoteHost '{remoteHost}' is not a valid IP address. " +
                        "Use 127.0.0.1 for local loopback with this extOSC setup."
                    );
                }

                return false;
            }

            if (remotePort <= 0 || remotePort > 65535)
            {
                if (logWarnings)
                {
                    Debug.LogWarning($"[KeyboardBelaOscEmulator] remotePort {remotePort} is outside the valid UDP range.");
                }

                return false;
            }

            if (_activeMappings.Count == 0)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[KeyboardBelaOscEmulator] No valid key mappings are configured, so no hits will be sent.");
                }

                return false;
            }

            return true;
        }

        private void RebuildActiveMappings(bool logWarnings)
        {
            _activeMappings.Clear();

            if (keyMappings == null)
            {
                return;
            }

            var seenKeys = new HashSet<KeyCode>();

            for (int i = 0; i < keyMappings.Count; i++)
            {
                KeyboardBelaOscMapping mapping = keyMappings[i];

                if (mapping == null)
                {
                    continue;
                }

                if (mapping.key == KeyCode.None)
                {
                    if (logWarnings)
                    {
                        Debug.LogWarning($"[KeyboardBelaOscEmulator] Ignored mapping at index {i} because no key was assigned.");
                    }

                    continue;
                }

                if (!seenKeys.Add(mapping.key))
                {
                    if (logWarnings)
                    {
                        Debug.LogWarning(
                            $"[KeyboardBelaOscEmulator] Duplicate mapping for key {mapping.key} detected. " +
                            "Only the first mapping for that key will be used."
                        );
                    }

                    continue;
                }

                _activeMappings.Add(mapping);
            }
        }

        private bool EnsureTransmitterReference(bool logWarnings)
        {
            if (transmitter != null)
            {
                return true;
            }

            transmitter = GetComponent<OSCTransmitter>();

            if (transmitter != null)
            {
                return true;
            }

            transmitter = gameObject.AddComponent<OSCTransmitter>();
            _transmitterWasAutoCreated = transmitter != null;

            if (_transmitterWasAutoCreated)
            {
                ConfigureAutoCreatedTransmitterDefaults();
                return true;
            }

            if (logWarnings)
            {
                Debug.LogWarning("[KeyboardBelaOscEmulator] OSCTransmitter could not be created automatically.");
            }

            return false;
        }

        private void ConfigureAutoCreatedTransmitterDefaults()
        {
            if (transmitter != null && !ReferenceEquals(transmitter, GetComponent<OSCTransmitter>()))
            {
                _transmitterWasAutoCreated = false;
            }

            if (!_transmitterWasAutoCreated || transmitter == null)
            {
                return;
            }

            transmitter.AutoConnect = true;
            transmitter.WorkInEditor = false;
            transmitter.CloseOnPause = false;
            transmitter.LocalHostMode = OSCLocalHostMode.Any;
            transmitter.LocalPortMode = OSCLocalPortMode.Random;
            transmitter.UseBundle = false;
        }

        private bool EnsureTransmitterReady(bool logWarnings)
        {
            if (!EnsureTransmitterReference(logWarnings))
            {
                return false;
            }

            try
            {
                ConfigureAutoCreatedTransmitterDefaults();

                transmitter.RemoteHost = remoteHost;
                transmitter.RemotePort = remotePort;

                if (transmitter.SourceReceiver == null && transmitter.LocalPortMode == OSCLocalPortMode.FromReceiver)
                {
                    transmitter.LocalPortMode = OSCLocalPortMode.Random;
                }

                if (Application.isPlaying && !transmitter.IsStarted)
                {
                    transmitter.Connect();
                }
            }
            catch (Exception exception)
            {
                if (logWarnings)
                {
                    Debug.LogWarning($"[KeyboardBelaOscEmulator] Failed to initialise OSC transmitter: {exception.Message}");
                }

                return false;
            }

            if (!Application.isPlaying)
            {
                return true;
            }

            if (!transmitter.IsStarted)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[KeyboardBelaOscEmulator] OSC transmitter is present but not connected.");
                }

                return false;
            }

            return true;
        }
    }
}
