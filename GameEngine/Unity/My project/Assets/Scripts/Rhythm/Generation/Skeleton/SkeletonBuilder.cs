using System;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    public sealed class SkeletonBuilder : ISkeletonBuilder
    {
        private const float MetricWeakThreshold = 0.35f;
        private const float SourceRelationScale = 0.25f;
        private const float SourceRelationMaxMagnitude = 0.30f;
        private const float AnchorExplicitScale = 0.42f;
        private const float AnchorFallbackScale = 0.30f;
        private const float AnchorMaxMagnitude = 0.50f;
        private const float EndingScale = 0.16f;
        private const float EndingMaxMagnitude = 0.20f;
        private const float DensityShapingMaxMagnitude = 0.20f;
        private const float PhraseBalanceMaxMagnitude = 0.10f;
        private const float MaximumJitter = 0.01f;

        public SkeletonPattern BuildSkeleton(SkeletonBuildRequest request)
        {
            ValidateRequest(request);

            SkeletonDerivedMaps maps = SkeletonDerivedMaps.Build(
                request.SourceTurn,
                request.SourceAnalysis,
                request.TurnLengthSteps,
                request.StepsPerQuarter);

            var activeSteps = new bool[request.TurnLengthSteps];
            SkeletonStepMeta[] stepMeta = BuildStepMeta(request, maps);
            float[] selectionScores = BuildSelectionScores(stepMeta);
            int[] selectedStepIndices = new int[0];
            SkeletonPatternSummary summary = CreateEmptySummary(request);

            return new SkeletonPattern(
                request.TurnLengthSteps,
                activeSteps,
                selectionScores,
                stepMeta,
                selectedStepIndices,
                summary);
        }

        private static SkeletonStepMeta[] BuildStepMeta(
            SkeletonBuildRequest request,
            SkeletonDerivedMaps maps)
        {
            var stepMeta = new SkeletonStepMeta[request.TurnLengthSteps];

            for (int i = 0; i < stepMeta.Length; i++)
            {
                bool sourceAnchor = maps.SourceAnchors[i];
                bool protectedAnchor = request.Plan.PreserveAnchors && sourceAnchor;
                float metricScore = ScoreMetric(request.Config, maps.MetricalSalience[i], maps.StrongBeats[i]);
                float sourceRelationScore = ScoreSourceRelation(
                    request.Plan,
                    request.Config,
                    maps.SourceOccupied[i],
                    sourceAnchor,
                    maps.MetricalSalience[i],
                    maps.StrongBeats[i]);
                float anchorScore = ScoreAnchor(
                    request.Plan,
                    request.Config,
                    sourceAnchor,
                    maps.ExplicitAnchors[i]);
                float endingScore = ScoreEnding(
                    request.Plan,
                    request.Config,
                    maps.EndingRegion[i],
                    maps.StepsFromEnd[i],
                    request.StepsPerQuarter,
                    maps.SourceOccupied[i],
                    maps.MetricalSalience[i],
                    maps.StrongBeats[i]);
                float phraseBalanceScore = ScorePhraseBalance(
                    request.Config,
                    maps.StepToSegment[i]);
                float densityShapingScore = ScoreDensityShaping(
                    request.Plan,
                    maps.MetricStrengthLevels[i]);
                float spacingPenalty = 0f;
                float jitterOffset = ScoreJitter(
                    request.Plan,
                    request.Config,
                    i,
                    request.TurnLengthSteps);
                float rawScore = SanitizeScore(
                    metricScore +
                    sourceRelationScore +
                    anchorScore +
                    endingScore +
                    phraseBalanceScore +
                    densityShapingScore +
                    jitterOffset);
                float finalScore = SanitizeScore(rawScore - spacingPenalty);

                SkeletonReasonFlags reasonFlags = SkeletonReasonFlags.None;
                if (maps.StrongBeats[i])
                    reasonFlags |= SkeletonReasonFlags.MetricStrong;
                else if (maps.MetricalSalience[i] <= MetricWeakThreshold)
                    reasonFlags |= SkeletonReasonFlags.MetricWeak;

                if (protectedAnchor)
                    reasonFlags |= SkeletonReasonFlags.ProtectedAnchor;

                if (sourceRelationScore > 0f)
                {
                    if (request.Plan.ResponseType == ResponseType.Complement ||
                        request.Plan.ResponseType == ResponseType.Contrast ||
                        request.Plan.ResponseType == ResponseType.Fill)
                    {
                        reasonFlags |= SkeletonReasonFlags.ComplementBoosted;
                    }
                    else if (request.Plan.ResponseType == ResponseType.Mirror)
                    {
                        reasonFlags |= SkeletonReasonFlags.MirrorBoosted;
                    }
                }

                if (anchorScore > 0f)
                    reasonFlags |= SkeletonReasonFlags.AnchorBoosted;

                if (endingScore > 0f)
                    reasonFlags |= SkeletonReasonFlags.EndingBoosted;

                stepMeta[i] = new SkeletonStepMeta
                {
                    StepIndex = i,
                    SegmentIndex = maps.StepToSegment[i],
                    IsStrongBeat = maps.StrongBeats[i],
                    StepsFromEnd = maps.StepsFromEnd[i],
                    MetricScore = metricScore,
                    SourceRelationScore = sourceRelationScore,
                    AnchorScore = anchorScore,
                    EndingScore = endingScore,
                    PhraseBalanceScore = phraseBalanceScore,
                    DensityShapingScore = densityShapingScore,
                    SpacingPenalty = spacingPenalty,
                    JitterOffset = jitterOffset,
                    RawScore = rawScore,
                    FinalScore = finalScore,
                    Selected = false,
                    SourceOccupied = maps.SourceOccupied[i],
                    SourceAnchor = sourceAnchor,
                    IsExplicitAnchor = maps.ExplicitAnchors[i],
                    IsFallbackAnchor = maps.FallbackAnchors[i],
                    InEndingRegion = maps.EndingRegion[i],
                    Protected = protectedAnchor,
                    ReasonFlags = reasonFlags
                };
            }

            return stepMeta;
        }

        private static float[] BuildSelectionScores(SkeletonStepMeta[] stepMeta)
        {
            var selectionScores = new float[stepMeta.Length];
            for (int i = 0; i < stepMeta.Length; i++)
                selectionScores[i] = stepMeta[i].FinalScore;

            return selectionScores;
        }

        private static float ScoreMetric(SkeletonBuilderConfig config, float metricSalience, bool isStrongBeat)
        {
            float metricWeight = Clamp01(config.MetricStrengthWeight);
            float strongBeatWeight = Clamp01(config.StrongBeatPreferenceWeight);
            float weightedSalience = metricSalience * metricWeight;
            float strongBeatBoost = isStrongBeat
                ? Math.Min(1f - weightedSalience, 0.20f * strongBeatWeight)
                : 0f;

            return SanitizeScore(
                Clamp01(weightedSalience + strongBeatBoost));
        }

        private static float ScoreSourceRelation(
            ResponsePlan plan,
            SkeletonBuilderConfig config,
            bool sourceOccupied,
            bool sourceAnchor,
            float metricSalience,
            bool isStrongBeat)
        {
            float complementarity = Clamp01(plan.ComplementarityBias);
            bool sourceRelated = sourceOccupied || sourceAnchor;

            switch (plan.ResponseType)
            {
                case ResponseType.Mirror:
                    return ClampMagnitude(
                        config.MirrorWeight * SourceRelationScale *
                        (sourceOccupied ? 1f : (sourceAnchor ? 0.55f : -0.06f * complementarity)),
                        SourceRelationMaxMagnitude);

                case ResponseType.Complement:
                    return ClampMagnitude(
                        config.ComplementWeight * SourceRelationScale *
                        (sourceOccupied ? (-0.25f - 0.15f * complementarity) : (0.70f + 0.10f * complementarity)),
                        SourceRelationMaxMagnitude);

                case ResponseType.Simplify:
                    return ClampMagnitude(
                        config.MirrorWeight * SourceRelationScale *
                        (sourceRelated ? 0.40f : 0.08f * complementarity),
                        SourceRelationMaxMagnitude);

                case ResponseType.Intensify:
                    return ClampMagnitude(
                        SourceRelationScale *
                        (sourceRelated
                            ? config.MirrorWeight * 0.60f
                            : config.ComplementWeight * (0.22f + 0.18f * complementarity)),
                        SourceRelationMaxMagnitude);

                case ResponseType.Contrast:
                    return ClampMagnitude(
                        config.ComplementWeight * SourceRelationScale *
                        (sourceOccupied ? -0.30f : (0.50f + 0.20f * complementarity)),
                        SourceRelationMaxMagnitude);

                case ResponseType.Fill:
                    float interstitialSupport = isStrongBeat ? 0.18f : 0.50f + (1f - metricSalience) * 0.20f;
                    return ClampMagnitude(
                        config.ComplementWeight * SourceRelationScale *
                        (sourceOccupied ? -0.12f : interstitialSupport + complementarity * 0.15f),
                        SourceRelationMaxMagnitude);

                default:
                    throw new ArgumentOutOfRangeException(nameof(plan.ResponseType), plan.ResponseType, "Unknown response type.");
            }
        }

        private static float ScoreAnchor(
            ResponsePlan plan,
            SkeletonBuilderConfig config,
            bool sourceAnchor,
            bool isExplicitAnchor)
        {
            if (!plan.PreserveAnchors || !sourceAnchor)
                return 0f;

            float anchorScale = isExplicitAnchor ? AnchorExplicitScale : AnchorFallbackScale;

            return SanitizeScore(Math.Min(config.AnchorInfluenceWeight * anchorScale, AnchorMaxMagnitude));
        }

        private static float ScoreEnding(
            ResponsePlan plan,
            SkeletonBuilderConfig config,
            bool inEndingRegion,
            int stepsFromEnd,
            int stepsPerQuarter,
            bool sourceOccupied,
            float metricSalience,
            bool isStrongBeat)
        {
            if (!inEndingRegion)
                return 0f;

            float proximity = GetEndingProximity(stepsFromEnd, stepsPerQuarter);
            float lateWeight = 0.35f + proximity * 0.65f;
            float strongFactor = isStrongBeat ? 1f : (metricSalience >= 0.50f ? 0.45f : 0f);
            float finalAccent = stepsFromEnd == 0 ? 1f : 0f;
            float directionalScore;

            if (plan.MirrorEnding)
            {
                directionalScore = (sourceOccupied ? 0.75f : 0.20f) * lateWeight;
                if (isStrongBeat)
                    directionalScore += 0.20f * lateWeight;

                return SanitizeScore(ClampMagnitude(config.EndingInfluenceWeight * EndingScale * directionalScore, EndingMaxMagnitude));
            }

            switch (plan.ResponseType)
            {
                case ResponseType.Mirror:
                    directionalScore = (sourceOccupied ? 0.55f : 0.15f) * lateWeight;
                    break;

                case ResponseType.Complement:
                    directionalScore = (sourceOccupied ? -0.20f : 0.45f + (1f - metricSalience) * 0.20f) * lateWeight;
                    break;

                case ResponseType.Simplify:
                    directionalScore = (strongFactor - (metricSalience < 0.50f ? 0.35f : 0f)) * lateWeight;
                    break;

                case ResponseType.Intensify:
                    directionalScore = (strongFactor + finalAccent * 0.35f) * lateWeight;
                    break;

                case ResponseType.Contrast:
                    directionalScore = (finalAccent > 0f ? -0.60f : 0.20f + (1f - metricSalience) * 0.25f) * lateWeight;
                    break;

                case ResponseType.Fill:
                    directionalScore = (isStrongBeat ? 0.15f : 0.55f + (1f - metricSalience) * 0.25f) * lateWeight;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(plan.ResponseType), plan.ResponseType, "Unknown response type.");
            }

            return SanitizeScore(ClampMagnitude(config.EndingInfluenceWeight * EndingScale * directionalScore, EndingMaxMagnitude));
        }

        private static float ScorePhraseBalance(SkeletonBuilderConfig config, int segmentIndex)
        {
            if (!config.RebalanceAcrossSegments)
                return 0f;

            switch (segmentIndex)
            {
                case 1:
                case 2:
                    return ClampMagnitude(0.03f, PhraseBalanceMaxMagnitude);
                case 3:
                    return ClampMagnitude(0.015f, PhraseBalanceMaxMagnitude);
                default:
                    return 0f;
            }
        }

        private static float ScoreDensityShaping(ResponsePlan plan, MetricStrengthLevel strengthLevel)
        {
            float targetDensity = Clamp01(plan.TargetDensity);

            if (targetDensity < 0.35f)
            {
                float lowAmount = (0.35f - targetDensity) / 0.35f;
                switch (strengthLevel)
                {
                    case MetricStrengthLevel.Strongest: return ClampMagnitude(0.08f * lowAmount, DensityShapingMaxMagnitude);
                    case MetricStrengthLevel.Strong: return ClampMagnitude(0.03f * lowAmount, DensityShapingMaxMagnitude);
                    case MetricStrengthLevel.Medium: return 0f;
                    case MetricStrengthLevel.Weak: return ClampMagnitude(-0.03f * lowAmount, DensityShapingMaxMagnitude);
                    default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
                }
            }

            if (targetDensity > 0.65f)
            {
                float highAmount = (targetDensity - 0.65f) / 0.35f;
                switch (strengthLevel)
                {
                    case MetricStrengthLevel.Strongest: return 0f;
                    case MetricStrengthLevel.Strong: return ClampMagnitude(0.02f * highAmount, DensityShapingMaxMagnitude);
                    case MetricStrengthLevel.Medium: return ClampMagnitude(0.04f * highAmount, DensityShapingMaxMagnitude);
                    case MetricStrengthLevel.Weak: return ClampMagnitude(0.05f * highAmount, DensityShapingMaxMagnitude);
                    default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
                }
            }

            return 0f;
        }

        private static float ScoreJitter(
            ResponsePlan plan,
            SkeletonBuilderConfig config,
            int stepIndex,
            int turnLengthSteps)
        {
            if (config.SelectionJitter <= 0f)
                return 0f;

            float jitterAmount = Math.Min(config.SelectionJitter, MaximumJitter);
            float unit = DeterministicUnitNoise(stepIndex, turnLengthSteps, plan.ResponseType);
            return (unit * 2f - 1f) * jitterAmount;
        }

        private static float DeterministicUnitNoise(int stepIndex, int turnLengthSteps, ResponseType responseType)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)stepIndex) * 16777619u;
                hash = (hash ^ (uint)turnLengthSteps) * 16777619u;
                hash = (hash ^ (uint)responseType) * 16777619u;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static float GetEndingProximity(int stepsFromEnd, int stepsPerQuarter)
        {
            int endingWindow = Math.Max(1, stepsPerQuarter);
            if (endingWindow == 1)
                return 1f;

            int boundedStepsFromEnd = Math.Max(0, Math.Min(stepsFromEnd, endingWindow - 1));
            return 1f - boundedStepsFromEnd / (float)(endingWindow - 1);
        }

        private static SkeletonPatternSummary CreateEmptySummary(SkeletonBuildRequest request)
        {
            bool densityTargetMet = request.Plan.TargetDensity <= request.Config.DensityTolerance;

            return new SkeletonPatternSummary(
                activeCount: 0,
                achievedDensity: 0f,
                sourceOverlapCount: 0,
                anchorAlignedCount: 0,
                densityTargetMet: densityTargetMet,
                usedStochasticTieBreak: false);
        }

        private static void ValidateRequest(SkeletonBuildRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Plan == null)
                throw new ArgumentException("A response plan is required.", nameof(request));

            if (request.SourceTurn == null)
                throw new ArgumentException("A source turn is required.", nameof(request));

            if (request.SourceAnalysis == null)
                throw new ArgumentException("Source analysis is required.", nameof(request));

            if (request.Config == null)
                throw new ArgumentException("Skeleton builder config is required.", nameof(request));

            if (request.TurnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.TurnLengthSteps), "Turn length steps must be greater than zero.");

            if (request.StepsPerQuarter <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.StepsPerQuarter), "Steps per quarter must be greater than zero.");

            if (request.Plan.TurnLengthSteps > 0 && request.Plan.TurnLengthSteps != request.TurnLengthSteps)
                throw new ArgumentException("Request turn length must match the response plan turn length.", nameof(request));

            ValidateFinite(request.Plan.TargetDensity, nameof(request.Plan.TargetDensity));
            ValidateFinite(request.Plan.ComplementarityBias, nameof(request.Plan.ComplementarityBias));

            if (request.SourceTurn.velocity == null)
                throw new ArgumentException("Source turn velocity data is required.", nameof(request));

            if (request.SourceTurn.stepsPerQuarter > 0 && request.SourceTurn.stepsPerQuarter != request.StepsPerQuarter)
                throw new ArgumentException("Source turn grid does not match request steps-per-quarter.", nameof(request));

            ValidateConfig(request.Config);
        }

        private static void ValidateConfig(SkeletonBuilderConfig config)
        {
            ValidateNonNegativeFinite(config.MetricStrengthWeight, nameof(config.MetricStrengthWeight));
            ValidateNonNegativeFinite(config.MirrorWeight, nameof(config.MirrorWeight));
            ValidateNonNegativeFinite(config.ComplementWeight, nameof(config.ComplementWeight));
            ValidateNonNegativeFinite(config.AnchorInfluenceWeight, nameof(config.AnchorInfluenceWeight));
            ValidateNonNegativeFinite(config.EndingInfluenceWeight, nameof(config.EndingInfluenceWeight));
            ValidateNonNegativeFinite(config.StrongBeatPreferenceWeight, nameof(config.StrongBeatPreferenceWeight));
            ValidateNonNegativeFinite(config.DensityTolerance, nameof(config.DensityTolerance));
            ValidateNonNegativeFinite(config.SelectionJitter, nameof(config.SelectionJitter));

            if (config.MinimumStepSpacing < 0)
                throw new ArgumentOutOfRangeException(nameof(config.MinimumStepSpacing), "Minimum step spacing cannot be negative.");

            if (config.MaximumClusterSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(config.MaximumClusterSize), "Maximum cluster size must be greater than zero.");
        }

        private static void ValidateNonNegativeFinite(float value, string name)
        {
            ValidateFinite(value, name);

            if (value < 0f)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and non-negative.");
        }

        private static void ValidateFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }

        private static float ClampMagnitude(float value, float maxMagnitude)
        {
            if (value > maxMagnitude)
                return maxMagnitude;

            if (value < -maxMagnitude)
                return -maxMagnitude;

            return value;
        }

        private static float SanitizeScore(float score)
        {
            if (float.IsNaN(score) || float.IsInfinity(score))
                throw new InvalidOperationException("Skeleton raw scoring produced a non-finite value.");

            return score;
        }
    }
}
