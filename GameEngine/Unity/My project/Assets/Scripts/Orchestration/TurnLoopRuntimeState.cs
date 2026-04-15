using IT4s.Data;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Orchestration
{
    /// <summary>
    /// Passive runtime backing store for TurnLoopController.
    /// It only holds controller-owned data and reset helpers; orchestration decisions remain in the controller.
    /// </summary>
    internal sealed class TurnLoopRuntimeState
    {
        public HitEvent LastTriggerHit { get; private set; }
        public bool HasLastTriggerHit { get; private set; }
        public long CaptureStartSamples { get; private set; } = -1;
        public long CaptureEndSamples { get; private set; } = -1;
        public bool CaptureClockWarningIssued { get; set; }

        public TurnWindow LastTurnWindow { get; private set; }
        public bool HasLastTurnWindow { get; private set; }

        public PatternTurn LastCompiledPatternTurn { get; private set; }
        public bool HasLastCompiledPatternTurn { get; private set; }

        public TurnAnalysisResult LastAnalysisResult { get; private set; }
        public bool HasLastAnalysisResult { get; private set; }

        public ResponsePlan CurrentResponsePlan { get; private set; }
        public bool HasCurrentResponsePlan { get; private set; }

        public PatternTurn LastGeneratedAiPatternTurn { get; private set; }
        public bool HasLastGeneratedAiPatternTurn { get; private set; }

        public void RecordCaptureStart(HitEvent triggerHit, long endSamples)
        {
            LastTriggerHit = triggerHit;
            HasLastTriggerHit = true;
            CaptureStartSamples = triggerHit.tSamples;
            CaptureEndSamples = endSamples;
            CaptureClockWarningIssued = false;
        }

        public void ClearCaptureRuntimeState()
        {
            LastTriggerHit = default;
            HasLastTriggerHit = false;
            CaptureStartSamples = -1;
            CaptureEndSamples = -1;
            CaptureClockWarningIssued = false;
        }

        public void SetLastTurnWindow(TurnWindow turnWindow)
        {
            LastTurnWindow = turnWindow;
            HasLastTurnWindow = true;
        }

        public void SetLastCompiledPatternTurn(PatternTurn pattern)
        {
            LastCompiledPatternTurn = pattern;
            HasLastCompiledPatternTurn = pattern != null;
        }

        public void SetLastAnalysisResult(TurnAnalysisResult analysis)
        {
            LastAnalysisResult = analysis;
            HasLastAnalysisResult = analysis != null;
        }

        public void ClearAnalysisResultState()
        {
            LastAnalysisResult = null;
            HasLastAnalysisResult = false;
        }

        public void SetCurrentResponsePlan(ResponsePlan responsePlan)
        {
            CurrentResponsePlan = responsePlan;
            HasCurrentResponsePlan = responsePlan != null;
        }

        public void ClearResponsePlanState()
        {
            CurrentResponsePlan = null;
            HasCurrentResponsePlan = false;
        }

        public void SetLastGeneratedAiPatternTurn(PatternTurn pattern)
        {
            LastGeneratedAiPatternTurn = pattern;
            HasLastGeneratedAiPatternTurn = pattern != null;
        }
    }
}
