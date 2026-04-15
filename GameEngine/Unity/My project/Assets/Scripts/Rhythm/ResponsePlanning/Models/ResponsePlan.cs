using System;

namespace IT4s.Rhythm.ResponsePlanning.Models
{
    [Serializable]
    public sealed class ResponsePlan
    {
        public ResponseType ResponseType { get; private set; }
        public float TargetDensity { get; private set; }
        public float ComplementarityBias { get; private set; }
        public bool PreserveAnchors { get; private set; }
        public bool MirrorEnding { get; private set; }
        public int TurnLengthSteps { get; private set; }

        public ResponsePlan()
            : this(ResponseType.Mirror, 0f, 0f, false, false, 0)
        {
        }

        public ResponsePlan(
            ResponseType responseType,
            float targetDensity,
            float complementarityBias,
            bool preserveAnchors,
            bool mirrorEnding,
            int turnLengthSteps)
        {
            ResponseType = responseType;
            TargetDensity = targetDensity;
            ComplementarityBias = complementarityBias;
            PreserveAnchors = preserveAnchors;
            MirrorEnding = mirrorEnding;
            TurnLengthSteps = turnLengthSteps;
        }

        public ResponsePlan With(
            ResponseType? responseType = null,
            float? targetDensity = null,
            float? complementarityBias = null,
            bool? preserveAnchors = null,
            bool? mirrorEnding = null,
            int? turnLengthSteps = null)
        {
            return new ResponsePlan(
                responseType ?? ResponseType,
                targetDensity ?? TargetDensity,
                complementarityBias ?? ComplementarityBias,
                preserveAnchors ?? PreserveAnchors,
                mirrorEnding ?? MirrorEnding,
                turnLengthSteps ?? TurnLengthSteps);
        }
    }
}
