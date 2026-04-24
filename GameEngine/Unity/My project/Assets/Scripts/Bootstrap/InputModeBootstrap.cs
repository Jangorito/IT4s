using IT4s.Diagnostics;
using UnityEngine;

namespace IT4s.Input
{
    /// <summary>
    /// Chooses whether Unity should use live Bela OSC input
    /// or one of the internal test-input rigs.
    ///
    /// LiveBelaOsc:
    /// - OscHitReceiver listens for incoming messages from Bela
    /// - Internal senders are disabled
    ///
    /// FakeOscRig:
    /// - OscHitReceiver still listens as normal
    /// - FakeBelaOscSender is enabled and sends test hits into the same pipeline
    ///
    /// KeyboardOscEmulator:
    /// - OscHitReceiver still listens as normal
    /// - KeyboardBelaOscEmulator is enabled and sends Bela-compatible OSC hits from key presses
    /// </summary>
    public class InputModeBootstrap : MonoBehaviour
    {
        public enum InputMode
        {
            LiveBelaOsc,
            FakeOscRig,
            KeyboardOscEmulator
        }

        [Header("Mode")]
        [SerializeField] private InputMode mode = InputMode.LiveBelaOsc;

        [Header("Required")]
        [SerializeField] private OscHitReceiver oscHitReceiver;

        [Header("Optional Test Rig")]
        [SerializeField] private FakeBelaOscSender fakeBelaOscSender;
        [SerializeField] private KeyboardBelaOscEmulator keyboardBelaOscEmulator;

        public InputMode Mode => mode;

        private void Awake()
        {
            if (oscHitReceiver == null)
            {
                Debug.LogError("[InputModeBootstrap] No OscHitReceiver assigned.");
                enabled = false;
                return;
            }

            // Receiver always be on for both modes
            oscHitReceiver.enabled = true;

            switch (mode)
            {
                case InputMode.LiveBelaOsc:
                    SetFakeRigEnabled(false);
                    SetKeyboardEmulatorEnabled(false);
                    RuntimeDebugLog.Log("[InputModeBootstrap] Mode = Live Bela OSC");
                    break;

                case InputMode.FakeOscRig:
                    SetFakeRigEnabled(true);
                    SetKeyboardEmulatorEnabled(false);
                    RuntimeDebugLog.Log("[InputModeBootstrap] Mode = Fake OSC Rig");
                    break;

                case InputMode.KeyboardOscEmulator:
                    SetFakeRigEnabled(false);
                    SetKeyboardEmulatorEnabled(true);
                    RuntimeDebugLog.Log("[InputModeBootstrap] Mode = Keyboard OSC Emulator");
                    break;

                default:
                    SetFakeRigEnabled(false);
                    SetKeyboardEmulatorEnabled(false);
                    Debug.LogWarning("[InputModeBootstrap] Unknown mode. Defaulting to Live Bela OSC behaviour.");
                    break;
            }
        }

        private void SetFakeRigEnabled(bool enabledState)
        {
            if (fakeBelaOscSender != null)
            {
                fakeBelaOscSender.enabled = enabledState;
            }
            else if (enabledState)
            {
                Debug.LogWarning("[InputModeBootstrap] Fake mode selected, but no FakeBelaOscSender is assigned.");
            }
        }

        private void SetKeyboardEmulatorEnabled(bool enabledState)
        {
            if (keyboardBelaOscEmulator != null)
            {
                keyboardBelaOscEmulator.enabled = enabledState;
            }
            else if (enabledState)
            {
                Debug.LogWarning("[InputModeBootstrap] Keyboard emulator mode selected, but no KeyboardBelaOscEmulator is assigned.");
            }
        }
    }
}
