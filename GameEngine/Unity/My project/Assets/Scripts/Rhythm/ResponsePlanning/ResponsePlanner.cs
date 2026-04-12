using System;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.ResponsePlanning
{
    public sealed class ResponsePlanner : IResponsePlanner
    {
        private const float MaxVelocity = 127f;
        private const float HighSupportThreshold = 0.6f;
        private const float LowSupportThreshold = 0.4f;
        private const float MidLevelThreshold = 0.5f;
        private const float LowDensityThreshold = 0.35f;
        private const float HighDensityThreshold = 0.7f;
        private const float StrongAnchorThreshold = 0.7f;
        private const float StrongEndDensityThreshold = 0.5f;
        private const float StrongEndEnergyThreshold = 0.6f;
        private const float StrongEndAccentThreshold = 0.75f;
        private const float SupportComplementarityDelta = 0.05f;

        private readonly IRandomSource randomSource;

        public ResponsePlanner()
            : this(new SystemRandomSource())
        {
        }

        public ResponsePlanner(int seed)
            : this(new SystemRandomSource(seed))
        {
        }

        public ResponsePlanner(Random random)
            : this(new SystemRandomSource(random))
        {
        }

        public ResponsePlanner(IRandomSource randomSource)
        {
            this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        }

        public ResponsePlan Plan(TurnAnalysisResult analysis)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            ResponseType type = DecideType(analysis);
            ResponsePlan plan = BuildPlan(type, analysis);
            plan = ApplyStochasticVariation(plan);
            return Clamp(plan);
        }

        private ResponseType DecideType(TurnAnalysisResult analysis)
        {
            if (HasStrongEndActivity(analysis))
                return ResponseType.Fill;

            if (HasStrongAnchors(analysis) && HasHighSupport(analysis))
                return ResponseType.Mirror;

            if (HasAnchors(analysis) && HasLowSupport(analysis))
                return IsBelowMidLevel(analysis)
                    ? ResponseType.Intensify
                    : ResponseType.Complement;

            if (HasAnchors(analysis))
                return ResponseType.Complement;

            if (IsLowDensity(analysis) || IsLowEnergy(analysis))
                return ResponseType.Intensify;

            if (IsHighDensity(analysis) && IsHighEnergy(analysis))
                return ResponseType.Simplify;

            if (IsStrongDirectionalSap(analysis))
                return ResponseType.Contrast;

            if (IsRisingSap(analysis))
                return ResponseType.Complement;

            return ResponseType.Mirror;
        }

        private ResponsePlan BuildPlan(ResponseType type, TurnAnalysisResult analysis)
        {
            float density = GetDensityLevel(analysis);
            float energy = GetEnergyLevel(analysis);
            int turnLengthSteps = GetTurnLengthSteps(analysis);

            SamplePlanParameters(
                type,
                out float variationAmount,
                out float syncopationBias,
                out float complementarityBias);

            complementarityBias = ModulateComplementarity(
                complementarityBias,
                GetAnchorSupport(analysis));

            switch (type)
            {
                case ResponseType.Mirror:
                    return new ResponsePlan(
                        type,
                        density,
                        energy,
                        true,
                        true,
                        variationAmount,
                        syncopationBias,
                        complementarityBias,
                        turnLengthSteps);

                case ResponseType.Complement:
                    return new ResponsePlan(
                        type,
                        density * 0.9f,
                        energy * 0.9f,
                        false,
                        false,
                        variationAmount,
                        syncopationBias,
                        complementarityBias,
                        turnLengthSteps);

                case ResponseType.Simplify:
                    return new ResponsePlan(
                        type,
                        density * 0.6f,
                        energy * 0.7f,
                        true,
                        false,
                        variationAmount,
                        syncopationBias,
                        complementarityBias,
                        turnLengthSteps);

                case ResponseType.Intensify:
                    return new ResponsePlan(
                        type,
                        density + 0.2f,
                        energy + 0.15f,
                        false,
                        false,
                        variationAmount,
                        syncopationBias,
                        complementarityBias,
                        turnLengthSteps);

                case ResponseType.Contrast:
                    return new ResponsePlan(
                        type,
                        1f - density,
                        1f - energy,
                        false,
                        false,
                        variationAmount,
                        syncopationBias,
                        complementarityBias,
                        turnLengthSteps);

                case ResponseType.Fill:
                    return new ResponsePlan(
                        type,
                        density + 0.25f,
                        energy + 0.2f,
                        false,
                        true,
                        variationAmount,
                        syncopationBias,
                        complementarityBias,
                        turnLengthSteps);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown response type.");
            }
        }

        private ResponsePlan ApplyStochasticVariation(ResponsePlan plan)
        {
            return plan.With(
                targetDensity: plan.TargetDensity + NextSigned(0.05f),
                targetEnergy: plan.TargetEnergy + NextSigned(0.05f),
                variationAmount: plan.VariationAmount + NextSigned(0.1f),
                syncopationBias: plan.SyncopationBias + NextSigned(0.1f),
                complementarityBias: plan.ComplementarityBias + NextSigned(0.05f));
        }

        private static ResponsePlan Clamp(ResponsePlan plan)
        {
            return plan.With(
                targetDensity: Clamp01(plan.TargetDensity),
                targetEnergy: Clamp01(plan.TargetEnergy),
                variationAmount: Clamp01(plan.VariationAmount),
                syncopationBias: Clamp01(plan.SyncopationBias),
                complementarityBias: Clamp01(plan.ComplementarityBias),
                turnLengthSteps: Math.Max(1, plan.TurnLengthSteps));
        }

        private float NextSigned(float magnitude)
        {
            return (float)((randomSource.NextDouble() * 2d - 1d) * magnitude);
        }

        private float SampleRange(float minimum, float maximum)
        {
            return minimum + (float)(randomSource.NextDouble() * (maximum - minimum));
        }

        private void SamplePlanParameters(
            ResponseType type,
            out float variationAmount,
            out float syncopationBias,
            out float complementarityBias)
        {
            switch (type)
            {
                case ResponseType.Mirror:
                    variationAmount = SampleRange(0.15f, 0.30f);
                    syncopationBias = SampleRange(0.25f, 0.40f);
                    complementarityBias = SampleRange(0.10f, 0.30f);
                    break;

                case ResponseType.Complement:
                    variationAmount = SampleRange(0.30f, 0.50f);
                    syncopationBias = SampleRange(0.40f, 0.65f);
                    complementarityBias = SampleRange(0.70f, 0.90f);
                    break;

                case ResponseType.Simplify:
                    variationAmount = SampleRange(0.10f, 0.25f);
                    syncopationBias = SampleRange(0.10f, 0.30f);
                    complementarityBias = SampleRange(0.20f, 0.40f);
                    break;

                case ResponseType.Intensify:
                    variationAmount = SampleRange(0.35f, 0.60f);
                    syncopationBias = SampleRange(0.50f, 0.75f);
                    complementarityBias = SampleRange(0.50f, 0.70f);
                    break;

                case ResponseType.Contrast:
                    variationAmount = SampleRange(0.50f, 0.80f);
                    syncopationBias = SampleRange(0.60f, 0.85f);
                    complementarityBias = SampleRange(0.40f, 0.60f);
                    break;

                case ResponseType.Fill:
                    variationAmount = SampleRange(0.60f, 0.90f);
                    syncopationBias = SampleRange(0.70f, 0.95f);
                    complementarityBias = SampleRange(0.60f, 0.80f);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown response type.");
            }
        }

        private static bool HasStrongEndActivity(TurnAnalysisResult analysis)
        {
            EndActivityFeatures endActivity = GetEndActivity(analysis);

            if (endActivity.EndDensity < StrongEndDensityThreshold)
                return false;

            float endEnergy = Clamp01(endActivity.EndEnergy / MaxVelocity);
            float endAccent = Clamp01(endActivity.EndAccent / MaxVelocity);

            return endEnergy >= StrongEndEnergyThreshold
                || endAccent >= StrongEndAccentThreshold;
        }

        private static bool HasStrongAnchors(TurnAnalysisResult analysis)
        {
            AnchorFeatures anchors = GetAnchors(analysis);
            return anchors.AnchorCount > 0
                && anchors.StrongestAnchorScore >= StrongAnchorThreshold;
        }

        private static bool HasAnchors(TurnAnalysisResult analysis)
        {
            return GetAnchors(analysis).AnchorCount > 0;
        }

        private static bool HasHighSupport(TurnAnalysisResult analysis)
        {
            return GetAnchorSupport(analysis).AverageSupport > HighSupportThreshold;
        }

        private static bool HasLowSupport(TurnAnalysisResult analysis)
        {
            return GetAnchorSupport(analysis).AverageSupport < LowSupportThreshold;
        }

        private static bool IsBelowMidLevel(TurnAnalysisResult analysis)
        {
            return GetDensityLevel(analysis) < MidLevelThreshold
                || GetEnergyLevel(analysis) < MidLevelThreshold;
        }

        private static bool IsLowDensity(TurnAnalysisResult analysis)
        {
            return GetDensityLevel(analysis) < LowDensityThreshold;
        }

        private static bool IsHighDensity(TurnAnalysisResult analysis)
        {
            return GetDensityLevel(analysis) > HighDensityThreshold;
        }

        private static bool IsLowEnergy(TurnAnalysisResult analysis)
        {
            EnergyFeatures energy = GetEnergy(analysis);
            return energy.IsLowEnergy || GetEnergyLevel(analysis) < LowDensityThreshold;
        }

        private static bool IsHighEnergy(TurnAnalysisResult analysis)
        {
            EnergyFeatures energy = GetEnergy(analysis);
            return energy.IsHighEnergy || GetEnergyLevel(analysis) > HighDensityThreshold;
        }

        private static bool IsStrongDirectionalSap(TurnAnalysisResult analysis)
        {
            SegmentActivityProfileFeatures sap = GetSegmentActivityProfile(analysis);
            TemporalBias densityBias = GetTemporalBias(sap.DensityShape);
            TemporalBias energyBias = GetTemporalBias(sap.EnergyShape);

            return densityBias != TemporalBias.Neutral
                && densityBias == energyBias;
        }

        private static bool IsRisingSap(TurnAnalysisResult analysis)
        {
            SegmentActivityProfileFeatures sap = GetSegmentActivityProfile(analysis);
            return HasLateBias(sap.DensityShape)
                || HasLateBias(sap.EnergyShape);
        }

        private static bool HasLateBias(ActivityShape shape)
        {
            return GetTemporalBias(shape) == TemporalBias.Late;
        }

        // Planner-level interpretation: late-biased shapes imply buildup,
        // while shared early/late bias across density and energy implies a strong direction.
        private static TemporalBias GetTemporalBias(ActivityShape shape)
        {
            switch (shape)
            {
                case ActivityShape.Increasing:
                case ActivityShape.BackLoaded:
                    return TemporalBias.Late;

                case ActivityShape.Decreasing:
                case ActivityShape.FrontLoaded:
                    return TemporalBias.Early;

                default:
                    return TemporalBias.Neutral;
            }
        }

        private static float ModulateComplementarity(
            float complementarity,
            AnchorSupportFeatures anchorSupport)
        {
            if (anchorSupport.AverageSupport < LowSupportThreshold)
                return complementarity + SupportComplementarityDelta;

            if (anchorSupport.AverageSupport > HighSupportThreshold)
                return complementarity - SupportComplementarityDelta;

            return complementarity;
        }

        private static float GetDensityLevel(TurnAnalysisResult analysis)
        {
            return Clamp01(GetDensity(analysis).StepDensity);
        }

        private static float GetEnergyLevel(TurnAnalysisResult analysis)
        {
            return Clamp01(GetEnergy(analysis).MeanVelocity / MaxVelocity);
        }

        private static int GetTurnLengthSteps(TurnAnalysisResult analysis)
        {
            return Math.Max(
                GetDensity(analysis).StepCount,
                GetAnchors(analysis).StepCount);
        }

        private static DensityFeatures GetDensity(TurnAnalysisResult analysis)
        {
            return analysis.Density ?? new DensityFeatures();
        }

        private static EnergyFeatures GetEnergy(TurnAnalysisResult analysis)
        {
            return analysis.Energy ?? new EnergyFeatures();
        }

        private static AnchorFeatures GetAnchors(TurnAnalysisResult analysis)
        {
            return analysis.Anchor ?? new AnchorFeatures();
        }

        private static AnchorSupportFeatures GetAnchorSupport(TurnAnalysisResult analysis)
        {
            return analysis.AnchorSupport ?? new AnchorSupportFeatures();
        }

        private static EndActivityFeatures GetEndActivity(TurnAnalysisResult analysis)
        {
            return analysis.EndActivity ?? new EndActivityFeatures();
        }

        private static SegmentActivityProfileFeatures GetSegmentActivityProfile(TurnAnalysisResult analysis)
        {
            return analysis.SegmentActivityProfile ?? new SegmentActivityProfileFeatures();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }

        private enum TemporalBias
        {
            Neutral,
            Early,
            Late
        }
    }
}
