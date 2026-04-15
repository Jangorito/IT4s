using IT4s.Data;
using IT4s.Input;
using IT4s.Rhythm;

namespace IT4s.Orchestration
{
    /// <summary>
    /// Plain C# flow for coordinating the concrete human capture window.
    /// The controller decides when this flow may run; the flow handles the start/end checks
    /// against TurnCaptureController, sample-time progression, and capture-window completion.
    /// </summary>
    public sealed class HumanTurnCaptureFlow
    {
        public enum StartStatus
        {
            Started,
            Ignored,
            Error
        }

        public enum TickStatus
        {
            WaitingForClock,
            WaitingForEnd,
            Completed,
            Error
        }

        private readonly TurnCaptureController turnCaptureController;
        private readonly OscHitReceiver hitReceiver;

        public HumanTurnCaptureFlow(TurnCaptureController turnCaptureController, OscHitReceiver hitReceiver)
        {
            this.turnCaptureController = turnCaptureController;
            this.hitReceiver = hitReceiver;
        }

        public StartStatus TryStartCapture(
            HitEvent triggerHit,
            MusicalTimingConfig timing,
            out long turnDurationSamples,
            out long endSamples,
            out string errorReason)
        {
            turnDurationSamples = 0;
            endSamples = -1;
            errorReason = null;

            if (turnCaptureController == null)
            {
                errorReason = "Cannot start human turn because TurnCaptureController is missing.";
                return StartStatus.Error;
            }

            if (hitReceiver == null)
            {
                errorReason = "Cannot start human turn because OscHitReceiver is missing.";
                return StartStatus.Error;
            }

            turnDurationSamples = timing.GetTurnDurationSamples();

            if (triggerHit.tSamples > long.MaxValue - turnDurationSamples)
            {
                errorReason =
                    $"Cannot schedule human turn end because start={triggerHit.tSamples} would overflow with duration={turnDurationSamples}.";
                return StartStatus.Error;
            }

            if (!turnCaptureController.TryBeginCapture(triggerHit.tSamples))
            {
                return StartStatus.Ignored;
            }

            endSamples = triggerHit.tSamples + turnDurationSamples;
            return StartStatus.Started;
        }

        public TickStatus TickCapture(
            long startSamples,
            long endSamples,
            out TurnWindow turnWindow,
            out string errorReason)
        {
            turnWindow = default;
            errorReason = null;

            if (turnCaptureController == null)
            {
                errorReason = "Capture phase failed because TurnCaptureController is missing.";
                return TickStatus.Error;
            }

            if (!turnCaptureController.IsCapturing)
            {
                errorReason = "Capture phase lost sync because TurnCaptureController is no longer capturing.";
                return TickStatus.Error;
            }

            if (startSamples < 0 || endSamples < startSamples)
            {
                errorReason = $"Capture phase has invalid timing bounds start={startSamples}, end={endSamples}.";
                return TickStatus.Error;
            }

            if (!TryGetCurrentSampleTime(out long currentSamples))
            {
                return TickStatus.WaitingForClock;
            }

            if (currentSamples < endSamples)
            {
                return TickStatus.WaitingForEnd;
            }

            if (!turnCaptureController.TryEndCapture(endSamples, out turnWindow))
            {
                errorReason = $"Capture phase failed to end cleanly at sample {endSamples}.";
                return TickStatus.Error;
            }

            return TickStatus.Completed;
        }

        private bool TryGetCurrentSampleTime(out long currentSamples)
        {
            if (hitReceiver == null)
            {
                currentSamples = 0;
                return false;
            }

            return hitReceiver.TryGetCurrentSampleTime(out currentSamples);
        }
    }
}
