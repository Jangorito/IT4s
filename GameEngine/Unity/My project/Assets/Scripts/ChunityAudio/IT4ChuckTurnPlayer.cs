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

    public void SetMetronomeEnabled(bool enabled)
    {
        metronomeOnPlayback = enabled;
    }

    public void Play(PatternTurn turn)
    {
        if (!ready || turn == null || turn.velocity == null || turn.offsetSamples == null)
            return;

        int stepCount = Math.Min(turn.velocity.Length, turn.offsetSamples.Length);
        int safeStepsPerQuarter = Mathf.Max(1, turn.stepsPerQuarter);

        // EXACTLY matches PatternCompiler math
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

        bool ok = true;
        ok &= chuck.SetInt("stepCount", stepCount);
        ok &= chuck.SetInt("samplesPerStep", samplesPerStep);
        ok &= chuck.SetInt("stepsPerQuarter", safeStepsPerQuarter);
        ok &= chuck.SetInt("metronomeEnabled", metronomeOnPlayback ? 1 : 0);
        ok &= chuck.SetIntArray_AT("velocity", vel);
        ok &= chuck.SetIntArray_AT("offsetSamples", off);

        if (!ok)
        {
            Debug.LogError("[IT4] Failed to push playback data to ChucK.");
            return;
        }

        chuck.BroadcastEvent("playTurn");
        Debug.Log($"[IT4] Broadcasted 'playTurn' turn={turn.turnId} steps={stepCount} metronome={(metronomeOnPlayback ? "on" : "off")}");
    }
}
