using System.Collections;
using IT4s.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(ChuckMainInstance))]
public class ChuckSnarePlayer : MonoBehaviour
{
    [Header("ChucK file path inside StreamingAssets")]
    [SerializeField] private string chuckFile = "Chuck/IT4Snare.ck";

    private ChuckMainInstance chuck;
    private bool chuckReady;

    private Chuck.IntCallback ckReadyCb;

    private void Awake()
    {
        chuck = GetComponent<ChuckMainInstance>();

        ckReadyCb = chuck.CreateGetIntCallback((long v) =>
        {
            if (v != 0)
            {
                chuckReady = true;
                RuntimeDebugLog.Log("[UNITY] ChucK ckReady=1 (IT4Snare initialized)");
            }
        });
    }

    private void Start()
    {
        // Start ChucK file from StreamingAssets
        chuck.RunFile(chuckFile);

        // Poll ckReady until ChucK flips it
        StartCoroutine(WaitForCkReady());
    }

    private IEnumerator WaitForCkReady()
    {
        while (!chuckReady)
        {
            chuck.GetInt("ckReady", ckReadyCb);
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Call this from your TurnManager / AI playback code:
    public void TriggerSnare()
    {
        if (!chuckReady) return;
        chuck.BroadcastEvent("snareTrig");
        RuntimeDebugLog.Log("[UNITY] Broadcasted snareTrig event to ChucK.");
    }

    // Quick test: press LeftControl to hear snare
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            TriggerSnare();
        }
    }
}
