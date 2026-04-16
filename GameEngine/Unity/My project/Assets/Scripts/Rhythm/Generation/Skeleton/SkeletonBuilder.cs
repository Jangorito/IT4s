using System;
using IT4s.Rhythm.Generation.Skeleton.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    public sealed class SkeletonBuilder : ISkeletonBuilder
    {
        private const float MetricWeakThreshold = 0.35f;

        public SkeletonPattern BuildSkeleton(SkeletonBuildRequest request)
        {
            ValidateRequest(request);

            SkeletonDerivedMaps maps = SkeletonDerivedMaps.Build(
                request.SourceTurn,
                request.SourceAnalysis,
                request.TurnLengthSteps,
                request.StepsPerQuarter);

            var activeSteps = new bool[request.TurnLengthSteps];
            var selectionScores = new float[request.TurnLengthSteps];
            SkeletonStepMeta[] stepMeta = BuildStepMeta(request, maps);
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

                SkeletonReasonFlags reasonFlags = SkeletonReasonFlags.None;
                if (maps.StrongBeats[i])
                    reasonFlags |= SkeletonReasonFlags.MetricStrong;
                else if (maps.MetricalSalience[i] <= MetricWeakThreshold)
                    reasonFlags |= SkeletonReasonFlags.MetricWeak;

                if (protectedAnchor)
                    reasonFlags |= SkeletonReasonFlags.ProtectedAnchor;

                stepMeta[i] = new SkeletonStepMeta
                {
                    StepIndex = i,
                    SegmentIndex = maps.StepToSegment[i],
                    MetricScore = maps.MetricalSalience[i],
                    SourceRelationScore = 0f,
                    AnchorScore = 0f,
                    EndingScore = 0f,
                    SpacingPenalty = 0f,
                    JitterOffset = 0f,
                    FinalScore = 0f,
                    Selected = false,
                    SourceOccupied = maps.SourceOccupied[i],
                    SourceAnchor = sourceAnchor,
                    InEndingRegion = maps.EndingRegion[i],
                    Protected = protectedAnchor,
                    ReasonFlags = reasonFlags
                };
            }

            return stepMeta;
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
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and non-negative.");
        }
    }
}
