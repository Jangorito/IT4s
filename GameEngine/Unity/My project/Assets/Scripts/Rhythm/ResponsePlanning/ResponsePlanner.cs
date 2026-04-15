using System;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.ResponsePlanning
{
    public sealed class ResponsePlanner : IResponsePlanner
    {
        private const float MaxVelocity = 127f;
        private readonly ResponsePlannerConfig config;

        public ResponsePlanner()
            : this(new ResponsePlannerConfig())
        {
        }

        public ResponsePlanner(ResponsePlannerConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ResponsePlan Plan(TurnAnalysisResult analysis)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            PlanningContext context = BuildContext(analysis);
            ResponseType responseType = ChooseResponseType(context);
            return DerivePlan(context, responseType);
        }

        private PlanningContext BuildContext(TurnAnalysisResult analysis)
        {
            DensityFeatures density = GetDensity(analysis);
            EnergyFeatures energy = GetEnergy(analysis);
            AnchorFeatures anchors = GetAnchors(analysis);
            EndActivityFeatures endActivity = GetEndActivity(analysis);
            SegmentActivityProfileFeatures profile = GetSegmentActivityProfile(analysis);

            float sourceDensity = Clamp01(density.StepDensity);
            float sourceEnergy = Clamp01(energy.MeanVelocity / MaxVelocity);
            float endEnergy = Clamp01(endActivity.EndEnergy / MaxVelocity);
            float endAccent = Clamp01(endActivity.EndAccent / MaxVelocity);
            bool hasMeaningfulAnchors =
                anchors.AnchorCount >= config.MeaningfulAnchorCountThreshold &&
                anchors.StrongestAnchorScore >= config.MeaningfulAnchorScoreThreshold;
            bool hasStrongEnding =
                anchors.HasClosingAnchor ||
                (endActivity.EndDensity >= config.StrongEndingDensityThreshold &&
                (endEnergy >= config.StrongEndingEnergyThreshold ||
                endAccent >= config.StrongEndingAccentThreshold));
            bool endingIsOpen =
                !anchors.HasClosingAnchor &&
                endActivity.EndDensity <= config.OpenEndingDensityThreshold &&
                endEnergy <= config.OpenEndingEnergyThreshold &&
                endAccent <= config.OpenEndingAccentThreshold;
            bool activityIsBackLoaded = HasLateBias(profile.DensityShape) || HasLateBias(profile.EnergyShape);
            bool activityIsFrontLoaded = HasEarlyBias(profile.DensityShape) || HasEarlyBias(profile.EnergyShape);
            bool isSparse = sourceDensity < config.SparseDensityThreshold;
            bool isBusy = sourceDensity > config.BusyDensityThreshold;
            bool isLowEnergy = energy.IsLowEnergy || sourceEnergy < config.LowEnergyThreshold;
            bool isHighEnergy = energy.IsHighEnergy || sourceEnergy > config.HighEnergyThreshold;
            bool hasMeaningfulGaps = sourceDensity <= config.GapDensityThreshold;
            bool isCongested = sourceDensity >= config.CongestedDensityThreshold || (isBusy && isHighEnergy);

            return new PlanningContext(
                sourceDensity,
                sourceEnergy,
                anchors.AnchorCount,
                hasMeaningfulAnchors,
                hasStrongEnding,
                endingIsOpen,
                activityIsBackLoaded,
                activityIsFrontLoaded,
                isSparse,
                isBusy,
                isLowEnergy,
                isHighEnergy,
                hasMeaningfulGaps,
                isCongested,
                GetTurnLengthSteps(analysis));
        }

        private ResponseType ChooseResponseType(PlanningContext context)
        {
            float mirrorScore = ScoreMirror(context);
            float complementScore = ScoreComplement(context);
            float simplifyScore = ScoreSimplify(context);
            float intensifyScore = ScoreIntensify(context);
            float contrastScore = ScoreContrast(context);
            float fillScore = ScoreFill(context);
            float bestScore = GetBestScore(mirrorScore, complementScore, simplifyScore, intensifyScore, contrastScore, fillScore);

            bool mirrorTop = IsTopScore(mirrorScore, bestScore);
            bool complementTop = IsTopScore(complementScore, bestScore);
            bool simplifyTop = IsTopScore(simplifyScore, bestScore);
            bool intensifyTop = IsTopScore(intensifyScore, bestScore);
            bool contrastTop = IsTopScore(contrastScore, bestScore);
            bool fillTop = IsTopScore(fillScore, bestScore);

            if (complementTop && mirrorTop && context.HasMeaningfulGaps)
                return ResponseType.Complement;

            if (fillTop && intensifyTop && context.EndingIsOpen)
                return ResponseType.Fill;

            if (simplifyTop && contrastTop && context.IsBusy)
                return ResponseType.Simplify;

            if (mirrorTop && contrastTop && context.HasMeaningfulAnchors)
                return ResponseType.Mirror;

            if (mirrorTop)
                return ResponseType.Mirror;
            if (complementTop)
                return ResponseType.Complement;
            if (simplifyTop)
                return ResponseType.Simplify;
            if (intensifyTop)
                return ResponseType.Intensify;
            if (contrastTop)
                return ResponseType.Contrast;

            return ResponseType.Fill;
        }

        private ResponsePlan DerivePlan(PlanningContext context, ResponseType responseType)
        {
            ResponsePlan plan = new ResponsePlan(
                responseType,
                DeriveTargetDensity(context, responseType),
                DeriveComplementarityBias(context, responseType),
                DerivePreserveAnchors(context, responseType),
                DeriveMirrorEnding(context, responseType),
                context.TurnLengthSteps);

            return Clamp(plan);
        }

        private static ResponsePlan Clamp(ResponsePlan plan)
        {
            return plan.With(
                targetDensity: Clamp01(plan.TargetDensity),
                complementarityBias: Clamp01(plan.ComplementarityBias),
                turnLengthSteps: Math.Max(1, plan.TurnLengthSteps));
        }

        private float DeriveTargetDensity(PlanningContext context, ResponseType responseType)
        {
            float density = context.SourceDensity + GetDensityDelta(responseType) + GetDensityContextModifier(context, responseType);
            return Clamp(density, config.MinTargetDensity, config.MaxTargetDensity);
        }

        private float DeriveComplementarityBias(PlanningContext context, ResponseType responseType)
        {
            float bias = GetBaselineComplementarityBias(responseType);

            if (context.IsCongested || context.HasMeaningfulGaps)
                bias += config.ComplementarityAdjustment;

            if (context.HasMeaningfulAnchors)
                bias -= config.ComplementarityAdjustment;

            if (responseType == ResponseType.Mirror && context.HasStrongEnding)
                bias -= config.ComplementarityAdjustment;

            if (responseType == ResponseType.Fill && context.EndingIsOpen)
                bias += config.ComplementarityAdjustment * 0.5f;

            return Clamp01(bias);
        }

        private static bool DerivePreserveAnchors(PlanningContext context, ResponseType responseType)
        {
            if (!context.HasMeaningfulAnchors)
                return false;

            switch (responseType)
            {
                case ResponseType.Mirror:
                case ResponseType.Complement:
                    return !context.IsCongested || responseType == ResponseType.Mirror;
                case ResponseType.Simplify:
                    return !context.IsCongested && context.HasStrongEnding;
                case ResponseType.Contrast:
                case ResponseType.Intensify:
                    return false;
                case ResponseType.Fill:
                    return context.HasStrongEnding && !context.EndingIsOpen;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private static bool DeriveMirrorEnding(PlanningContext context, ResponseType responseType)
        {
            return context.HasStrongEnding && responseType == ResponseType.Mirror;
        }

        private static float ScoreMirror(PlanningContext context)
        {
            float score = 0f;
            if (context.IsBalancedDensity) score += 2f;
            if (context.IsMediumEnergy) score += 1f;
            if (context.HasMeaningfulAnchors) score += 2f;
            if (context.HasStrongEnding) score += 2f;
            if (context.EndingIsOpen) score -= 1f;
            return score;
        }

        private static float ScoreComplement(PlanningContext context)
        {
            float score = 0f;
            if (context.IsSparse) score += 1f;
            if (context.IsBalancedDensity) score += 1f;
            if (context.IsMediumEnergy) score += 1f;
            if (context.HasMeaningfulAnchors) score += 2f;
            else score -= 0.5f;
            if (context.HasMeaningfulGaps) score += 1f;
            if (!context.HasStrongEnding) score += 1f;
            if (context.ActivityIsBackLoaded || context.ActivityIsFrontLoaded) score += 0.5f;
            return score;
        }

        private static float ScoreSimplify(PlanningContext context)
        {
            float score = 0f;
            if (context.IsBusy) score += 3f;
            if (context.IsHighEnergy) score += 2f;
            if (context.HasMeaningfulAnchors) score += 1f;
            if (context.IsCongested) score += 1f;
            if (context.IsSparse) score -= 2f;
            if (context.IsLowEnergy) score -= 1f;
            return score;
        }

        private static float ScoreIntensify(PlanningContext context)
        {
            float score = 0f;
            if (context.IsSparse) score += 2f;
            if (context.IsLowEnergy) score += 2f;
            if (!context.HasMeaningfulAnchors) score += 1f;
            if (context.EndingIsOpen) score += 1f;
            if (context.IsBusy) score -= 1f;
            return score;
        }

        private static float ScoreContrast(PlanningContext context)
        {
            float score = 0f;
            if (context.IsBusy) score += 2f;
            if (context.IsHighEnergy) score += 1f;
            if (!context.HasMeaningfulAnchors) score += 1f;
            if (context.ActivityIsBackLoaded || context.ActivityIsFrontLoaded) score += 2f;
            if (context.HasMeaningfulAnchors) score -= 1f;
            return score;
        }

        private static float ScoreFill(PlanningContext context)
        {
            float score = 0f;
            if (context.IsSparse) score += 2f;
            if (context.IsLowEnergy) score += 1f;
            if (context.EndingIsOpen) score += 3f;
            if (context.ActivityIsBackLoaded) score += 1f;
            if (context.HasStrongEnding) score -= 1f;
            return score;
        }

        private float GetDensityDelta(ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Mirror: return 0.00f;
                case ResponseType.Complement: return -0.05f;
                case ResponseType.Simplify: return -0.15f;
                case ResponseType.Intensify: return 0.12f;
                case ResponseType.Contrast: return 0.00f;
                case ResponseType.Fill: return 0.15f;
                default: throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private static float GetBaselineComplementarityBias(ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Mirror: return 0.30f;
                case ResponseType.Complement: return 0.75f;
                case ResponseType.Simplify: return 0.40f;
                case ResponseType.Intensify: return 0.55f;
                case ResponseType.Contrast: return 0.65f;
                case ResponseType.Fill: return 0.70f;
                default: throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private float GetDensityContextModifier(PlanningContext context, ResponseType responseType)
        {
            float adjustment = config.DensityContextAdjustment;

            switch (responseType)
            {
                case ResponseType.Mirror:
                    return context.IsHighEnergy && !context.IsBusy ? adjustment * 0.5f : 0f;
                case ResponseType.Complement:
                    return context.IsCongested ? -adjustment * 0.5f : 0f;
                case ResponseType.Simplify:
                    if (context.IsBusy && context.IsHighEnergy) return -adjustment;
                    return context.IsBusy ? -adjustment * 0.5f : 0f;
                case ResponseType.Intensify:
                    if (context.IsSparse && !context.HasStrongEnding) return adjustment * 0.5f;
                    return context.HasStrongEnding ? -adjustment * 0.5f : 0f;
                case ResponseType.Contrast:
                    return context.ActivityIsBackLoaded || context.ActivityIsFrontLoaded ? adjustment * 0.5f : 0f;
                case ResponseType.Fill:
                    if (context.EndingIsOpen || context.ActivityIsBackLoaded) return adjustment;
                    return context.HasStrongEnding ? -adjustment * 0.5f : 0f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private bool IsTopScore(float score, float bestScore)
        {
            return bestScore - score <= config.ScoreTieMargin;
        }

        private static float GetBestScore(
            float mirrorScore,
            float complementScore,
            float simplifyScore,
            float intensifyScore,
            float contrastScore,
            float fillScore)
        {
            return Math.Max(
                Math.Max(Math.Max(mirrorScore, complementScore), Math.Max(simplifyScore, intensifyScore)),
                Math.Max(contrastScore, fillScore));
        }

        private static bool HasLateBias(ActivityShape shape)
        {
            return shape == ActivityShape.Increasing || shape == ActivityShape.BackLoaded;
        }

        private static bool HasEarlyBias(ActivityShape shape)
        {
            return shape == ActivityShape.Decreasing || shape == ActivityShape.FrontLoaded;
        }

        private static int GetTurnLengthSteps(TurnAnalysisResult analysis)
        {
            return Math.Max(GetDensity(analysis).StepCount, GetAnchors(analysis).StepCount);
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
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }
    }
}
