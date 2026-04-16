using System;
using System.Collections;
using UnityEngine;
using IT4s.Data;

public class IT4ChuckTurnPlayer : MonoBehaviour
{
    [Header("ChucK")]
    [SerializeField] private ChuckMainInstance chuck;
    [SerializeField] private string chuckFile = "Chuck/IT4_TurnPlayer.ck";

    [Header("Playback")]
    [SerializeField] private bool metronomeOnPlayback = true;

    private bool ready;
    private Chuck.IntCallback readyCb;

    public bool IsReady => ready;

    private void Awake()
    {
        if (!chuck) chuck = GetComponent<ChuckMainInstance>();

        if (!chuck)
        {
            Debug.LogError("[IT4] No ChuckMainInstance found on IT4ChuckTurnPlayer.");
            enabled = false;
            return;
        }

        readyCb = chuck.CreateGetIntCallback((long v) =>
        {
            if (v != 0) ready = true;
        });
    }

    private void Start()
    {
        if (!enabled) return;

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

    public void SetMetronomeEnabled(bool enabled)
    {
        metronomeOnPlayback = enabled;
    }

    public virtual bool StopPlayback()
    {
        if (!ready)
        {
            return false;
        }

        if (chuck == null)
        {
            Debug.LogWarning("[IT4] Cannot stop ChucK TurnPlayer because no ChuckMainInstance is assigned.");
            return false;
        }

        chuck.BroadcastEvent("stopTurn");
        Debug.Log("[IT4] Broadcasted 'stopTurn'.");
        return true;
    }

    public void PlayTurn(PatternTurn turn, bool? metronomeOverride = null)
    {
        if (!ready)
        {
            Debug.LogWarning("[IT4] ChucK TurnPlayer not ready yet.");
            return;
        }

        if (turn == null || turn.velocity == null || turn.offsetSamples == null)
        {
            Debug.LogWarning("[IT4] Cannot play null or incomplete PatternTurn.");
            return;
        }

        int stepCount = Math.Min(turn.velocity.Length, turn.offsetSamples.Length);
        if (stepCount <= 0)
        {
            Debug.LogWarning("[IT4] Cannot play PatternTurn with no steps.");
            return;
        }

        if (turn.velocity.Length != turn.offsetSamples.Length)
        {
            Debug.LogWarning(
                $"[IT4] PatternTurn {turn.turnId} has mismatched arrays: " +
                $"velocity={turn.velocity.Length}, offsetSamples={turn.offsetSamples.Length}. " +
                $"Truncating to {stepCount}."
            );
        }

        if (turn.bpm <= 0)
        {
            Debug.LogWarning($"[IT4] Invalid BPM on PatternTurn {turn.turnId}: {turn.bpm}");
            return;
        }

        if (turn.sampleRate <= 0)
        {
            Debug.LogWarning($"[IT4] Invalid sampleRate on PatternTurn {turn.turnId}: {turn.sampleRate}");
            return;
        }

        int safeStepsPerQuarter = Mathf.Max(1, turn.stepsPerQuarter);

        double secPerQuarter = 60.0 / turn.bpm;
        double secPerStep = secPerQuarter / safeStepsPerQuarter;
        double samplesPerStepD = secPerStep * turn.sampleRate;
        int samplesPerStep = Math.Max(1, (int)Math.Round(samplesPerStepD));

        long[] vel = new long[stepCount];
        long[] off = new long[stepCount];

        for (int i = 0; i < stepCount; i++)
        {
            vel[i] = turn.velocity[i];
            off[i] = turn.offsetSamples[i];
        }

        bool useMetronome = metronomeOverride ?? metronomeOnPlayback;

        bool ok = true;
        ok &= chuck.SetInt("stepCount", stepCount);
        ok &= chuck.SetInt("samplesPerStep", samplesPerStep);
        ok &= chuck.SetInt("stepsPerQuarter", safeStepsPerQuarter);
        ok &= chuck.SetInt("metronomeEnabled", useMetronome ? 1 : 0);
        ok &= chuck.SetIntArray_AT("velocity", vel);
        ok &= chuck.SetIntArray_AT("offsetSamples", off);

        if (!ok)
        {
            Debug.LogError("[IT4] Failed to push playback data to ChucK.");
            return;
        }

        chuck.BroadcastEvent("playTurn");
        Debug.Log(
            $"[IT4] Broadcasted 'playTurn' turn={turn.turnId} " +
            $"steps={stepCount} samplesPerStep={samplesPerStep} " +
            $"metronome={(useMetronome ? "on" : "off")}"
        );
    }
}
