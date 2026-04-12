using System;

namespace IT4s.Rhythm.ResponsePlanning.Models
{
    [Serializable]
    public sealed class ResponsePlan
    {
        public ResponseType Type { get; private set; }
        public float TargetDensity { get; private set; }
        public float TargetEnergy { get; private set; }
        public bool PreserveAnchors { get; private set; }
        public bool MirrorEnding { get; private set; }
        public float VariationAmount { get; private set; }
        public float SyncopationBias { get; private set; }
        public float ComplementarityBias { get; private set; }
        public int TurnLengthSteps { get; private set; }

        public ResponsePlan()
            : this(ResponseType.Mirror, 0f, 0f, false, false, 0f, 0f, 0f, 0)
        {
        }

        public ResponsePlan(
            ResponseType type,
            float targetDensity,
            float targetEnergy,
            bool preserveAnchors,
            bool mirrorEnding,
            float variationAmount,
            float syncopationBias,
            float complementarityBias,
            int turnLengthSteps)
        {
            Type = type;
            TargetDensity = targetDensity;
            TargetEnergy = targetEnergy;
            PreserveAnchors = preserveAnchors;
            MirrorEnding = mirrorEnding;
            VariationAmount = variationAmount;
            SyncopationBias = syncopationBias;
            ComplementarityBias = complementarityBias;
            TurnLengthSteps = turnLengthSteps;
        }

        public ResponsePlan With(
            ResponseType? type = null,
            float? targetDensity = null,
            float? targetEnergy = null,
            bool? preserveAnchors = null,
            bool? mirrorEnding = null,
            float? variationAmount = null,
            float? syncopationBias = null,
            float? complementarityBias = null,
            int? turnLengthSteps = null)
        {
            return new ResponsePlan(
                type ?? Type,
                targetDensity ?? TargetDensity,
                targetEnergy ?? TargetEnergy,
                preserveAnchors ?? PreserveAnchors,
                mirrorEnding ?? MirrorEnding,
                variationAmount ?? VariationAmount,
                syncopationBias ?? SyncopationBias,
                complementarityBias ?? ComplementarityBias,
                turnLengthSteps ?? TurnLengthSteps);
        }
    }
}
