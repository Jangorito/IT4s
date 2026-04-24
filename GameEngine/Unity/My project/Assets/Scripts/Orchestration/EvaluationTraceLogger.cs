using System;
using System.Globalization;
using IT4s.Data;
using IT4s.Rhythm.ResponsePlanning.Models;
using UnityEngine;

namespace IT4s.Orchestration
{
    /// <summary>
    /// Minimal opt-in trace writer for dissertation-ready interaction-loop summaries.
    /// It observes TurnLoopController lifecycle events and emits only abstract phase blocks.
    /// </summary>
    public sealed class EvaluationTraceLogger : IDisposable
    {
        private const string Arrow = "\u2193";

        private readonly TurnLoopController controller;
        private readonly Action<string> output;

        private TurnWindow capturedTurnWindow;
        private bool hasCapturedTurnWindow;
        private PatternTurn compiledHumanPattern;
        private ResponsePlan responsePlan;
        private bool disposed;

        public EvaluationTraceLogger(TurnLoopController controller)
            : this(controller, message => Debug.Log(message))
        {
        }

        internal EvaluationTraceLogger(TurnLoopController controller, Action<string> output)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            this.output = output ?? throw new ArgumentNullException(nameof(output));

            this.controller.OnHumanTurnCaptured += HandleHumanTurnCaptured;
            this.controller.OnHumanPatternCompiled += HandleHumanPatternCompiled;
            this.controller.OnResponsePlanned += HandleResponsePlanned;
            this.controller.OnPhaseChanged += HandlePhaseChanged;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            controller.OnHumanTurnCaptured -= HandleHumanTurnCaptured;
            controller.OnHumanPatternCompiled -= HandleHumanPatternCompiled;
            controller.OnResponsePlanned -= HandleResponsePlanned;
            controller.OnPhaseChanged -= HandlePhaseChanged;
            disposed = true;
        }

        private void HandleHumanTurnCaptured(TurnWindow turnWindow)
        {
            ClearCycleCache();
            capturedTurnWindow = turnWindow;
            hasCapturedTurnWindow = true;
        }

        private void HandleHumanPatternCompiled(PatternTurn pattern)
        {
            compiledHumanPattern = pattern;
        }

        private void HandleResponsePlanned(IT4s.Rhythm.TurnAnalysis.Models.TurnAnalysisResult analysis, ResponsePlan plan)
        {
            responsePlan = plan;
        }

        private void HandlePhaseChanged(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.WaitingForHuman:
                    EmitWaitingForHumanSummary();
                    ClearCycleCache();
                    break;

                case TurnPhase.CompilingHumanTurn:
                    EmitCapturedHumanSummary();
                    break;

                case TurnPhase.GeneratingAiResponse:
                    EmitCompiledHumanTurnSummary();
                    break;

                case TurnPhase.PlayingAiResponse:
                    EmitGeneratedAiResponseSummary();
                    EmitPlaybackTriggeredSummary();
                    break;
            }
        }

        private void EmitWaitingForHumanSummary()
        {
            EmitBlock("WaitingForHuman", "ready for human input");
        }

        private void EmitCapturedHumanSummary()
        {
            if (!hasCapturedTurnWindow)
            {
                return;
            }

            EmitBlock(
                "CapturingHuman",
                $"{capturedTurnWindow.HitCount} hits captured, duration = {capturedTurnWindow.DurationSamples} samples");
        }

        private void EmitCompiledHumanTurnSummary()
        {
            if (compiledHumanPattern == null)
            {
                return;
            }

            EmitBlock(
                "CompilingHumanTurn",
                $"{compiledHumanPattern.StepCount} steps, bpm={FormatBpm(compiledHumanPattern.bpm)}");
        }

        private void EmitGeneratedAiResponseSummary()
        {
            if (responsePlan == null)
            {
                return;
            }

            EmitBlock(
                "GeneratingAIResponse",
                $"ResponseType={responsePlan.ResponseType}, targetDensity={FormatDensity(responsePlan.TargetDensity)}");
        }

        private void EmitPlaybackTriggeredSummary()
        {
            EmitBlock("PlayingAIResponse", "playback triggered successfully");
        }

        private void EmitBlock(string phaseName, string summary)
        {
            output($"{phaseName}\n    {Arrow} ({summary})");
        }

        private void ClearCycleCache()
        {
            capturedTurnWindow = default;
            hasCapturedTurnWindow = false;
            compiledHumanPattern = null;
            responsePlan = null;
        }

        private static string FormatBpm(float bpm)
        {
            return bpm.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatDensity(float density)
        {
            return density.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
