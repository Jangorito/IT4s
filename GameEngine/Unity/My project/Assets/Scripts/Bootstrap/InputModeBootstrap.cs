using UnityEngine;

namespace IT4s.Input
{
    /// <summary>
    /// Chooses whether Unity should use live Bela OSC input
    /// or the internal fake OSC test rig.
    ///
    /// LiveBelaOsc:
    /// - OscHitReceiver listens for incoming messages from Bela
    /// - FakeBelaOscSender is disabled
    ///
    /// FakeOscRig:
    /// - OscHitReceiver still listens as normal
    /// - FakeBelaOscSender is enabled and sends test hits into the same pipeline
    /// </summary>
    public class InputModeBootstrap : MonoBehaviour
    {
        public enum InputMode
        {
            LiveBelaOsc,
            FakeOscRig
        }

        [Header("Mode")]
        [SerializeField] private InputMode mode = InputMode.LiveBelaOsc;

        [Header("Required")]
        [SerializeField] private OscHitReceiver oscHitReceiver;

        [Header("Optional Test Rig")]
        [SerializeField] private FakeBelaOscSender fakeBelaOscSender;

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
                    Debug.Log("[InputModeBootstrap] Mode = Live Bela OSC");
                    break;

                case InputMode.FakeOscRig:
                    SetFakeRigEnabled(true);
                    Debug.Log("[InputModeBootstrap] Mode = Fake OSC Rig");
                    break;

                default:
                    SetFakeRigEnabled(false);
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
    }
}