using System;
using System.Collections;
using UnityEngine;
using IT4s.Data;

public class IT4ChuckTurnPlayer : MonoBehaviour
{
    [SerializeField] private ChuckMainInstance chuck;
    [SerializeField] private string chuckFile = "Chuck/IT4_TurnPlayer.ck";

    private bool ready;
    private Chuck.IntCallback readyCb;

    private void Awake()
    {
        if (!chuck) chuck = GetComponent<ChuckMainInstance>();

        readyCb = chuck.CreateGetIntCallback((long v) =>
        {
            if (v != 0) ready = true;
        });
    }

    private void Start()
    {
        chuck.RunFile(chuckFile);
        StartCoroutine(PollReady());
    }

    private IEnumerator PollReady()
    {
        while (!ready)
        {
            chuck.GetInt("ckReady", readyCb);
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("[IT4] ChucK TurnPlayer ready.");
    }

    public void Play(PatternTurn turn)
    {
        if (!ready || turn == null || turn.velocity == null || turn.offsetSamples == null)
            return;

        int stepCount = Math.Min(turn.velocity.Length, turn.offsetSamples.Length);

        // EXACTLY matches PatternCompiler math
        double secPerQuarter = 60.0 / turn.bpm;
        double secPerStep = secPerQuarter / turn.stepsPerQuarter;
        double samplesPerStepD = secPerStep * turn.sampleRate;

        int samplesPerStep = Math.Max(1, (int)Math.Round(samplesPerStepD));

        long[] vel = new long[stepCount];
        long[] off = new long[stepCount];

        for (int i = 0; i < stepCount; i++)
        {
            vel[i] = turn.velocity[i];
            off[i] = turn.offsetSamples[i];
        }

        Debug.Log($"[IT4] Playing turn {turn.turnId} with {stepCount} steps, {samplesPerStep} samples");
        Debug.Log($"[IT4] Calculated velocities: {string.Join(", ", vel)}");
        Debug.Log($"[IT4] Calculated offsets: {string.Join(", ", off)}");
        chuck.SetInt("stepCount", stepCount);
        chuck.SetInt("samplesPerStep", samplesPerStep);
        // chuck.SetIntArray("velocity", vel);
        // chuck.SetIntArray("offsetSamples", off);

        if (chuck.SetIntArray_AT("velocity", vel) !=  true)
        {
            Debug.LogError("[IT4] Failed to set velocity array in ChucK.");
            return;
        }
        if (chuck.SetIntArray_AT("offsetSamples", off) != true)
        {
            Debug.LogError("[IT4] Failed to set offsetSamples array in ChucK.");
            return;
        }

        Debug.Log("[IT4] Setting turn data in ChucK:");
        
        if (chuck.GetInt("stepCount", (v) => Debug.Log("[IT4] stepCount in ck: " + v)) == false)
        {
            Debug.LogError("[IT4] Failed to get stepCount from ChucK for verification.");
            return;
        }
        
        if (chuck.GetInt("samplesPerStep", (v) => Debug.Log("[IT4] samplesPerStep in ck: " + v)) == false)
        {
            Debug.LogError("[IT4] Failed to get samplesPerStep from ChucK for verification.");
            return;
        }
        if (chuck.GetIntArray("velocity", (arr, len) =>
        {
            var n = Math.Min(arr.Length, (int)len);
            Debug.Log("[IT4] velocity in ck: " + string.Join(", ", arr[..n]));
        }) == false)
        {
            Debug.LogError("[IT4] Failed to get velocity array from ChucK for verification.");
            return;
        }
        if (chuck.GetIntArray("offsetSamples", (arr, len) =>
        {
            var n = Math.Min(arr.Length, (int)len);
            Debug.Log("[IT4] offsetSamples in ck: " + string.Join(", ", arr[..n]));
        }) == false)
        {
            Debug.LogError("[IT4] Failed to get offsetSamples array from ChucK for verification.");
            return;
        }

        chuck.BroadcastEvent("playTurn");
        Debug.Log("[IT4] Broadcasted 'playTurn' " + turn.turnId + " with " + stepCount + " steps.");
    }
}
