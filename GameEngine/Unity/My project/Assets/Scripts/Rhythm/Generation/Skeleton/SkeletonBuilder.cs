using System;
using System.Collections.Generic;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    public sealed class SkeletonBuilder : ISkeletonBuilder
    {
        private const float MetricWeakThreshold = 0.35f;
        private const float AnchorExplicitScale = 0.42f;
        private const float AnchorFallbackScale = 0.30f;
        private const float AnchorMaxMagnitude = 0.50f;
        private const float EndingScale = 0.16f;
        private const float EndingMaxMagnitude = 0.20f;
        private const float DensityShapingMaxMagnitude = 0.20f;
        private const float PhraseBalanceMaxMagnitude = 0.10f;
        private const float MaximumJitter = 0.01f;
        private const int DefaultSegmentCount = 4;

        private enum AnchorPriority
        {
            NonAnchor = 0,
            Fallback = 1,
            Explicit = 2
        }

        public SkeletonPattern BuildSkeleton(SkeletonBuildRequest request)
        {
            ValidateRequest(request);

            SkeletonDerivedMaps maps = SkeletonDerivedMaps.Build(
                request.SourceTurn,
                request.SourceAnalysis,
                request.TurnLengthSteps,
                request.StepsPerQuarter);
            ResponseBehaviourProfile profile = ResponseBehaviourProfileFactory.Build(
                request.Plan,
                request.Config,
                maps);

            SkeletonStepMeta[] stepMeta = BuildStepMeta(request, maps, profile);
            List<StepScore> scoredSteps = BuildScoredSteps(stepMeta);
            SkeletonContext selectionContext = BuildSelectionContext(request, maps, stepMeta, profile);
            bool[] activeSteps = BuildSkeleton(scoredSteps, selectionContext);
            ApplySelectionToStepMeta(
                stepMeta,
                activeSteps,
                request.Config.MinimumStepSpacing,
                selectionContext.ForcedEndingSteps);
            float[] selectionScores = BuildSelectionScores(stepMeta);
            int[] selectedStepIndices = BuildSelectedStepIndices(activeSteps);
            SkeletonPatternSummary summary = CreateSummary(request, activeSteps, stepMeta);

            return new SkeletonPattern(
                request.TurnLengthSteps,
                activeSteps,
                selectionScores,
                stepMeta,
                selectedStepIndices,
                summary);
        }

        public bool[] BuildSkeleton(List<StepScore> scoredSteps, SkeletonContext context)
        {
            if (scoredSteps == null)
                throw new ArgumentNullException(nameof(scoredSteps));

            ValidateSelectionContext(context);
            context.ClearForcedEndingSteps();

            List<SelectionCandidate> candidates = BuildSelectionCandidates(scoredSteps, context);
            candidates.Sort(CompareCandidates);

            int targetCount = GetTargetCount(context.TargetDensity, context.TotalSteps);
            int segmentCount = GetSegmentCount(candidates);
            var state = new SelectionState(context.TotalSteps, segmentCount);

            RunSelectionPass(
                candidates,
                context,
                state,
                targetCount,
                context.MinSpacingSteps,
                useSoftConstraints: true);

            if (state.SelectedCount < targetCount)
            {
                for (int spacing = Math.Max(0, context.MinSpacingSteps - 1);
                     spacing >= 0 && state.SelectedCount < targetCount;
                     spacing--)
                {
                    RunSelectionPass(
                        candidates,
                        context,
                        state,
                        targetCount,
                        spacing,
                        useSoftConstraints: false);
                }
            }

            EnsureEndingSelection(candidates, context, state, targetCount);
            EnsureBehaviourProfileSelection(candidates, context, state, targetCount);

            return state.ActiveSteps;
        }

        private static SkeletonStepMeta[] BuildStepMeta(
            SkeletonBuildRequest request,
            SkeletonDerivedMaps maps,
            ResponseBehaviourProfile profile)
        {
            var stepMeta = new SkeletonStepMeta[request.TurnLengthSteps];

            for (int i = 0; i < stepMeta.Length; i++)
            {
                bool sourceAnchor = maps.SourceAnchors[i];
                bool protectedAnchor = request.Plan.PreserveAnchors && sourceAnchor;
                float metricScore = ScoreMetric(
                    request.Config,
                    profile,
                    maps.MetricalSalience[i],
                    maps.StrongBeats[i],
                    maps.MetricStrengthLevels[i]);
                float sourceRelationScore = ScoreSourceRelation(
                    request.Config,
                    profile,
                    maps.SourceOccupied[i],
                    sourceAnchor,
                    maps.SourceGap[i],
                    maps.AdjacentToSource[i],
                    maps.NearSource[i],
                    maps.InterstitialSourceGap[i],
                    maps.DistanceToNearestSource[i],
                    maps.LocalSourceDensity[i],
                    maps.SegmentSourceWeights[maps.StepToSegment[i]],
                    maps.MetricalSalience[i],
                    maps.StrongBeats[i]);
                float anchorScore = ScoreAnchor(
                    request.Plan,
                    request.Config,
                    sourceAnchor,
                    maps.ExplicitAnchors[i]);
                float endingScore = ScoreEnding(
                    request.Config,
                    profile,
                    maps.EndingRegion[i],
                    maps.StepsFromEnd[i],
                    request.StepsPerQuarter,
                    maps.SourceOccupied[i],
                    maps.SourceGap[i],
                    maps.MetricalSalience[i],
                    maps.StrongBeats[i],
                    maps.InterstitialSourceGap[i]);
                float phraseBalanceScore = ScorePhraseBalance(
                    request.Config,
                    profile,
                    maps.StepToSegment[i]);
                float densityShapingScore = ScoreDensityShaping(
                    profile,
                    request.Config,
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
                    if (profile.SourceOverlapPolicy == SourceOverlapPolicy.PreferGaps ||
                        profile.SourceOverlapPolicy == SourceOverlapPolicy.Displace ||
                        profile.SourceOverlapPolicy == SourceOverlapPolicy.Interstitial)
                    {
                        reasonFlags |= SkeletonReasonFlags.ComplementBoosted;
                    }
                    else if (profile.SourceOverlapPolicy == SourceOverlapPolicy.Preserve)
                    {
                        reasonFlags |= SkeletonReasonFlags.MirrorBoosted;
                    }
                }

                if (maps.InterstitialSourceGap[i] && sourceRelationScore > 0f)
                    reasonFlags |= SkeletonReasonFlags.InterstitialBoosted;

                if (phraseBalanceScore > 0f)
                    reasonFlags |= SkeletonReasonFlags.SegmentTargetBoosted;

                if (profile.EndingPolicy == EndingPolicy.LateDrive && endingScore > 0f)
                    reasonFlags |= SkeletonReasonFlags.LateDriveBoosted;

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
                    SourceGap = maps.SourceGap[i],
                    SourceAnchor = sourceAnchor,
                    IsExplicitAnchor = maps.ExplicitAnchors[i],
                    IsFallbackAnchor = maps.FallbackAnchors[i],
                    InEndingRegion = maps.EndingRegion[i],
                    AdjacentToSource = maps.AdjacentToSource[i],
                    NearSource = maps.NearSource[i],
                    InterstitialSourceGap = maps.InterstitialSourceGap[i],
                    DistanceToNearestSourceHit = maps.DistanceToNearestSource[i],
                    PreviousSourceHitDistance = maps.PreviousSourceDistance[i],
                    NextSourceHitDistance = maps.NextSourceDistance[i],
                    LocalSourceDensity = maps.LocalSourceDensity[i],
                    MetricStrengthLevel = (int)maps.MetricStrengthLevels[i],
                    SegmentSourceWeight = maps.SegmentSourceWeights[maps.StepToSegment[i]],
                    Protected = protectedAnchor,
                    ReasonFlags = reasonFlags
                };
            }

            return stepMeta;
        }

        private static List<StepScore> BuildScoredSteps(SkeletonStepMeta[] stepMeta)
        {
            var scoredSteps = new List<StepScore>(stepMeta.Length);

            for (int i = 0; i < stepMeta.Length; i++)
            {
                SkeletonStepMeta meta = stepMeta[i];
                scoredSteps.Add(new StepScore
                {
                    StepIndex = meta.StepIndex,
                    Score = meta.FinalScore,
                    Flags = meta.ReasonFlags,
                    MetricalWeight = meta.MetricScore,
                    SourceRelationScore = meta.SourceRelationScore,
                    IsExplicitAnchor = meta.IsExplicitAnchor,
                    IsFallbackAnchor = meta.IsFallbackAnchor,
                    InEndingRegion = meta.InEndingRegion,
                    IsWeakMetrical = HasFlag(meta.ReasonFlags, SkeletonReasonFlags.MetricWeak),
                    SourceOccupied = meta.SourceOccupied,
                    AdjacentToSource = meta.AdjacentToSource,
                    NearSource = meta.NearSource,
                    InterstitialSourceGap = meta.InterstitialSourceGap,
                    LocalSourceDensity = meta.LocalSourceDensity,
                    SegmentTargetWeight = meta.SegmentSourceWeight,
                    SegmentIndex = meta.SegmentIndex
                });
            }

            return scoredSteps;
        }

        private static SkeletonContext BuildSelectionContext(
            SkeletonBuildRequest request,
            SkeletonDerivedMaps maps,
            SkeletonStepMeta[] stepMeta)
        {
            return BuildSelectionContext(request, maps, stepMeta, null);
        }

        private static SkeletonContext BuildSelectionContext(
            SkeletonBuildRequest request,
            SkeletonDerivedMaps maps,
            SkeletonStepMeta[] stepMeta,
            ResponseBehaviourProfile profile)
        {
            var explicitAnchors = new bool[stepMeta.Length];
            var fallbackAnchors = new bool[stepMeta.Length];
            var endingSteps = new bool[stepMeta.Length];
            var weakMetricalSteps = new bool[stepMeta.Length];
            var sourceOccupiedSteps = new bool[stepMeta.Length];
            var adjacentToSourceSteps = new bool[stepMeta.Length];
            var nearSourceSteps = new bool[stepMeta.Length];
            var interstitialSourceGapSteps = new bool[stepMeta.Length];
            var localSourceDensity = new int[stepMeta.Length];
            var metricStrengthLevels = new MetricStrengthLevel[stepMeta.Length];
            var metricalWeights = new float[stepMeta.Length];
            var sourceRelationScores = new float[stepMeta.Length];
            var segmentTargetWeightsByStep = new float[stepMeta.Length];
            var segmentByStep = new int[stepMeta.Length];

            for (int i = 0; i < stepMeta.Length; i++)
            {
                explicitAnchors[i] = stepMeta[i].IsExplicitAnchor;
                fallbackAnchors[i] = stepMeta[i].IsFallbackAnchor;
                endingSteps[i] = stepMeta[i].InEndingRegion;
                weakMetricalSteps[i] = HasFlag(stepMeta[i].ReasonFlags, SkeletonReasonFlags.MetricWeak);
                sourceOccupiedSteps[i] = stepMeta[i].SourceOccupied;
                adjacentToSourceSteps[i] = stepMeta[i].AdjacentToSource;
                nearSourceSteps[i] = stepMeta[i].NearSource;
                interstitialSourceGapSteps[i] = stepMeta[i].InterstitialSourceGap;
                localSourceDensity[i] = stepMeta[i].LocalSourceDensity;
                metricStrengthLevels[i] = maps.MetricStrengthLevels[i];
                metricalWeights[i] = stepMeta[i].MetricScore;
                sourceRelationScores[i] = stepMeta[i].SourceRelationScore;
                segmentTargetWeightsByStep[i] = profile != null
                    ? profile.GetSegmentTargetWeight(stepMeta[i].SegmentIndex)
                    : 1f;
                segmentByStep[i] = stepMeta[i].SegmentIndex;
            }

            return new SkeletonContext
            {
                ResponseType = request.Plan.ResponseType,
                BehaviourProfile = profile,
                TargetDensity = request.Plan.TargetDensity,
                TotalSteps = request.TurnLengthSteps,
                MinSpacingSteps = request.Config.MinimumStepSpacing,
                PreserveAnchors = profile != null ? profile.PreserveAnchors : request.Plan.PreserveAnchors,
                RequireStrongEnding = profile != null ? profile.RequireStrongEnding : request.Plan.MirrorEnding,
                EndingWindowSteps = request.StepsPerQuarter,
                RebalanceAcrossSegments = request.Config.RebalanceAcrossSegments,
                DensityTolerance = request.Config.DensityTolerance,
                ExplicitAnchors = explicitAnchors,
                FallbackAnchors = fallbackAnchors,
                EndingSteps = endingSteps,
                WeakMetricalSteps = weakMetricalSteps,
                SourceOccupiedSteps = sourceOccupiedSteps,
                AdjacentToSourceSteps = adjacentToSourceSteps,
                NearSourceSteps = nearSourceSteps,
                InterstitialSourceGapSteps = interstitialSourceGapSteps,
                LocalSourceDensity = localSourceDensity,
                MetricStrengthLevels = metricStrengthLevels,
                MetricalWeights = metricalWeights,
                SourceRelationScores = sourceRelationScores,
                SegmentTargetWeightsByStep = segmentTargetWeightsByStep,
                SegmentByStep = segmentByStep
            };
        }

        private static List<SelectionCandidate> BuildSelectionCandidates(
            List<StepScore> scoredSteps,
            SkeletonContext context)
        {
            var candidates = new List<SelectionCandidate>(scoredSteps.Count);

            for (int i = 0; i < scoredSteps.Count; i++)
            {
                StepScore stepScore = scoredSteps[i];
                if (stepScore == null)
                    throw new ArgumentException("Scored step list cannot contain null entries.", nameof(scoredSteps));

                int stepIndex = stepScore.StepIndex;
                if (stepIndex < 0 || stepIndex >= context.TotalSteps)
                    throw new ArgumentOutOfRangeException(nameof(scoredSteps), "Scored step index is outside the skeleton length.");

                ValidateFinite(stepScore.Score, nameof(stepScore.Score));

                float metricalWeight = ResolveMetricalWeight(stepScore, context, stepIndex);
                float sourceRelationScore = ResolveSourceRelationScore(stepScore, context, stepIndex);
                int segmentIndex = ResolveSegment(stepScore, context, stepIndex);

                candidates.Add(new SelectionCandidate(
                    stepIndex,
                    stepScore.Score,
                    metricalWeight,
                    sourceRelationScore,
                    ResolveAnchorPriority(stepScore, context, stepIndex),
                    ResolveEndingRegion(stepScore, context, stepIndex),
                    ResolveWeakMetrical(stepScore, context, stepIndex, metricalWeight),
                    segmentIndex,
                    ResolveSourceOccupied(stepScore, context, stepIndex),
                    ResolveAdjacentToSource(stepScore, context, stepIndex),
                    ResolveNearSource(stepScore, context, stepIndex),
                    ResolveInterstitialSourceGap(stepScore, context, stepIndex),
                    ResolveLocalSourceDensity(stepScore, context, stepIndex),
                    ResolveMetricStrengthLevel(context, stepIndex, metricalWeight),
                    ResolveSegmentTargetWeight(stepScore, context, stepIndex, segmentIndex)));
            }

            return candidates;
        }

        private static void RunSelectionPass(
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount,
            int minSpacing,
            bool useSoftConstraints)
        {
            if (context.PreserveAnchors)
            {
                RunSelectionGroup(
                    candidates,
                    context,
                    state,
                    targetCount,
                    minSpacing,
                    useSoftConstraints,
                    AnchorPriority.Explicit);

                RunSelectionGroup(
                    candidates,
                    context,
                    state,
                    targetCount,
                    minSpacing,
                    useSoftConstraints,
                    AnchorPriority.Fallback);

                RunSelectionGroup(
                    candidates,
                    context,
                    state,
                    targetCount,
                    minSpacing,
                    useSoftConstraints,
                    AnchorPriority.NonAnchor);

                return;
            }

            RunSelectionGroup(
                candidates,
                context,
                state,
                targetCount,
                minSpacing,
                useSoftConstraints,
                null);
        }

        private static void RunSelectionGroup(
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount,
            int minSpacing,
            bool useSoftConstraints,
            AnchorPriority? requiredAnchorPriority)
        {
            for (int i = 0; i < candidates.Count && state.SelectedCount < targetCount; i++)
            {
                SelectionCandidate candidate = candidates[i];

                if (requiredAnchorPriority.HasValue &&
                    candidate.AnchorPriority != requiredAnchorPriority.Value)
                {
                    continue;
                }

                if (!IsCandidateAllowed(
                    candidate,
                    candidates,
                    context,
                    state,
                    targetCount,
                    minSpacing,
                    useSoftConstraints))
                {
                    continue;
                }

                SelectCandidate(candidate, state);
            }
        }

        private static bool IsCandidateAllowed(
            SelectionCandidate candidate,
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount,
            int minSpacing,
            bool useSoftConstraints)
        {
            if (state.ActiveSteps[candidate.StepIndex])
                return false;

            if (ViolatesSpacing(candidate, state, minSpacing))
                return false;

            if (ShouldReserveEndingSlot(candidate, candidates, context, state, targetCount, minSpacing))
                return false;

            if (useSoftConstraints &&
                ShouldDeferWeakCandidate(candidate, candidates, context, state, targetCount, minSpacing))
            {
                return false;
            }

            if (useSoftConstraints &&
                ShouldDeferForBehaviourPolicy(candidate, candidates, context, state, targetCount, minSpacing))
            {
                return false;
            }

            if (useSoftConstraints &&
                ShouldDeferForSegmentBalance(candidate, candidates, context, state, targetCount, minSpacing))
            {
                return false;
            }

            return true;
        }

        private static bool ShouldReserveEndingSlot(
            SelectionCandidate candidate,
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount,
            int minSpacing)
        {
            if (!context.RequireStrongEnding || state.HasEnding || candidate.InEndingRegion)
                return false;

            int slotsRemaining = targetCount - state.SelectedCount;
            if (slotsRemaining > 1)
                return false;

            return ExistsSelectableEndingCandidate(candidates, state, minSpacing);
        }

        private static bool ShouldDeferWeakCandidate(
            SelectionCandidate candidate,
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount,
            int minSpacing)
        {
            if (!candidate.IsWeakMetrical)
                return false;

            if (context.PreserveAnchors && candidate.AnchorPriority != AnchorPriority.NonAnchor)
                return false;

            int slotsRemaining = targetCount - state.SelectedCount;
            if (slotsRemaining <= 0)
                return false;

            if (context.RequireStrongEnding &&
                !state.HasEnding &&
                candidate.InEndingRegion &&
                slotsRemaining == 1)
            {
                return false;
            }

            ResponseBehaviourProfile profile = context.BehaviourProfile;
            if (profile == null)
                profile = ResponseBehaviourProfile.Neutral(context.ResponseType, context.TargetDensity, state.SegmentCounts.Length);

            switch (profile.WeakStepPolicy)
            {
                case WeakStepPolicy.Suppress:
                    return true;

                case WeakStepPolicy.Defer:
                    if (profile.ResponseType == ResponseType.Intensify)
                        return state.SelectedCount < targetCount * profile.WeakStepSelectionPortion;

                    return candidate.SourceRelationScore < 0.12f;

                case WeakStepPolicy.Allow:
                    return false;

                case WeakStepPolicy.PreferInterstitial:
                    return !candidate.InterstitialSourceGap && !candidate.AdjacentToSource;

                default:
                    return true;
            }
        }

        private static bool ShouldDeferForBehaviourPolicy(
            SelectionCandidate candidate,
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount,
            int minSpacing)
        {
            ResponseBehaviourProfile profile = context.BehaviourProfile;
            if (profile == null)
                return false;

            if (profile.LimitDirectOverlap &&
                candidate.SourceOccupied &&
                candidate.AnchorPriority == AnchorPriority.NonAnchor &&
                CountSelectedSourceOverlaps(candidates, state) >= GetAllowedDirectOverlapCount(profile, targetCount) &&
                ExistsSelectableNonOverlapCandidate(candidates, state, minSpacing))
            {
                return true;
            }

            if (profile.SourceOverlapPolicy == SourceOverlapPolicy.Preserve &&
                !candidate.SourceOccupied &&
                !candidate.NearSource &&
                candidate.AnchorPriority == AnchorPriority.NonAnchor &&
                ExistsSelectableSourceRelatedCandidate(candidates, state, minSpacing))
            {
                return true;
            }

            if (profile.SourceOverlapPolicy == SourceOverlapPolicy.ReduceToAnchors &&
                candidate.MetricStrengthLevel == MetricStrengthLevel.Weak &&
                candidate.AnchorPriority == AnchorPriority.NonAnchor)
            {
                return true;
            }

            if (profile.GuaranteeInterstitial &&
                !candidate.InterstitialSourceGap &&
                !HasSelectedInterstitial(candidates, state) &&
                targetCount - state.SelectedCount <= 1 &&
                ExistsSelectableInterstitialCandidate(candidates, state, minSpacing))
            {
                return true;
            }

            if (profile.GuaranteeExpansion &&
                !IsExpansionCandidate(candidate) &&
                !HasSelectedExpansion(candidates, state) &&
                targetCount - state.SelectedCount <= 1 &&
                ExistsSelectableExpansionCandidate(candidates, state, minSpacing))
            {
                return true;
            }

            if (profile.GuaranteeLateDrive &&
                !candidate.InEndingRegion &&
                !state.HasEnding &&
                targetCount - state.SelectedCount <= 1 &&
                ExistsSelectableEndingCandidate(candidates, state, minSpacing))
            {
                return true;
            }

            return false;
        }

        private static bool ShouldDeferForSegmentBalance(
            SelectionCandidate candidate,
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount,
            int minSpacing)
        {
            if (!context.RebalanceAcrossSegments || state.SegmentCounts.Length <= 1)
                return false;

            int segmentIndex = candidate.SegmentIndex;
            if (segmentIndex < 0 || segmentIndex >= state.SegmentCounts.Length)
                return false;

            int softLimit = GetSegmentSoftLimit(targetCount, state.SegmentCounts.Length);
            if (candidate.SegmentTargetWeight > 1.08f)
                softLimit++;
            else if (candidate.SegmentTargetWeight < 0.92f)
                softLimit = Math.Max(1, softLimit - 1);

            if (state.SegmentCounts[segmentIndex] < softLimit)
                return false;

            int slotsRemaining = targetCount - state.SelectedCount;
            if (CountSelectableCandidates(candidates, state, minSpacing) <= slotsRemaining)
                return false;

            return ExistsSelectableUnderfilledSegmentCandidate(
                candidates,
                state,
                minSpacing,
                segmentIndex,
                softLimit);
        }

        private static int CountSelectedSourceOverlaps(
            List<SelectionCandidate> candidates,
            SelectionState state)
        {
            int count = 0;
            for (int i = 0; i < state.SelectedSteps.Count; i++)
            {
                SelectionCandidate candidate = FindCandidateByStep(candidates, state.SelectedSteps[i]);
                if (candidate != null && candidate.SourceOccupied)
                    count++;
            }

            return count;
        }

        private static int GetAllowedDirectOverlapCount(
            ResponseBehaviourProfile profile,
            int targetCount)
        {
            float limit = profile.DirectOverlapSoftLimit;
            int allowed = (int)Math.Floor(targetCount * limit);
            return Math.Max(0, allowed);
        }

        private static bool ExistsSelectableNonOverlapCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                if (!candidate.SourceOccupied &&
                    CandidatePassesBaseConstraints(candidate, state, minSpacing))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ExistsSelectableSourceRelatedCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                if ((candidate.SourceOccupied || candidate.NearSource) &&
                    CandidatePassesBaseConstraints(candidate, state, minSpacing))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSelectedInterstitial(
            List<SelectionCandidate> candidates,
            SelectionState state)
        {
            for (int i = 0; i < state.SelectedSteps.Count; i++)
            {
                SelectionCandidate candidate = FindCandidateByStep(candidates, state.SelectedSteps[i]);
                if (candidate != null && candidate.InterstitialSourceGap)
                    return true;
            }

            return false;
        }

        private static bool ExistsSelectableInterstitialCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                if (candidate.InterstitialSourceGap &&
                    CandidatePassesBaseConstraints(candidate, state, minSpacing))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSelectedExpansion(
            List<SelectionCandidate> candidates,
            SelectionState state)
        {
            for (int i = 0; i < state.SelectedSteps.Count; i++)
            {
                SelectionCandidate candidate = FindCandidateByStep(candidates, state.SelectedSteps[i]);
                if (candidate != null && IsExpansionCandidate(candidate))
                    return true;
            }

            return false;
        }

        private static bool ExistsSelectableExpansionCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                if (IsExpansionCandidate(candidate) &&
                    CandidatePassesBaseConstraints(candidate, state, minSpacing))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsExpansionCandidate(SelectionCandidate candidate)
        {
            return !candidate.SourceOccupied &&
                   candidate.NearSource &&
                   candidate.MetricStrengthLevel != MetricStrengthLevel.Weak;
        }

        private static void EnsureEndingSelection(
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount)
        {
            if (!context.RequireStrongEnding || state.HasEnding)
                return;

            SelectionCandidate endingCandidate = FindFirstSelectableEndingCandidate(
                candidates,
                state,
                minSpacing: 0);

            if (endingCandidate == null)
                return;

            if (state.SelectedCount < targetCount)
            {
                SelectCandidate(endingCandidate, state);
                context.RecordForcedEndingStep(endingCandidate.StepIndex);
                return;
            }

            int replacementIndex = FindReplacementIndexForEnding(candidates, state);
            if (replacementIndex < 0)
                return;

            RemoveSelectedAt(candidates, state, replacementIndex);
            SelectCandidate(endingCandidate, state);
            context.RecordForcedEndingStep(endingCandidate.StepIndex);
        }

        private static void EnsureBehaviourProfileSelection(
            List<SelectionCandidate> candidates,
            SkeletonContext context,
            SelectionState state,
            int targetCount)
        {
            ResponseBehaviourProfile profile = context.BehaviourProfile;
            if (profile == null || targetCount <= 0)
                return;

            if (profile.GuaranteeInterstitial && !HasSelectedInterstitial(candidates, state))
            {
                TryEnsureCandidate(
                    candidates,
                    state,
                    targetCount,
                    Math.Max(0, context.MinSpacingSteps - 1),
                    IsInterstitialCandidate);
            }

            if (profile.GuaranteeExpansion && !HasSelectedExpansion(candidates, state))
            {
                TryEnsureCandidate(
                    candidates,
                    state,
                    targetCount,
                    Math.Max(0, context.MinSpacingSteps - 1),
                    IsExpansionCandidate);
            }

            if (profile.GuaranteeLateDrive && !state.HasEnding)
            {
                TryEnsureCandidate(
                    candidates,
                    state,
                    targetCount,
                    Math.Max(0, context.MinSpacingSteps - 1),
                    IsLateDriveCandidate);
            }
        }

        private static bool TryEnsureCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int targetCount,
            int minSpacing,
            Predicate<SelectionCandidate> predicate)
        {
            SelectionCandidate candidate = FindBestSelectableCandidate(candidates, state, minSpacing, predicate);
            if (candidate != null && state.SelectedCount < targetCount)
            {
                SelectCandidate(candidate, state);
                return true;
            }

            if (candidate != null)
            {
                int replacementIndex = FindReplacementIndexForBehaviourGuarantee(candidates, state, predicate);
                if (replacementIndex < 0)
                    return false;

                SelectionCandidate removedCandidate = FindCandidateByStep(candidates, state.SelectedSteps[replacementIndex]);
                RemoveSelectedAt(candidates, state, replacementIndex);
                candidate = FindBestSelectableCandidate(candidates, state, minSpacing, predicate);
                if (candidate != null)
                {
                    SelectCandidate(candidate, state);
                    return true;
                }

                if (removedCandidate != null)
                    SelectCandidate(removedCandidate, state);

                return false;
            }

            int relaxedReplacementIndex = FindReplacementIndexForBehaviourGuarantee(candidates, state, predicate);
            if (relaxedReplacementIndex < 0)
                return false;

            SelectionCandidate relaxedRemovedCandidate = FindCandidateByStep(candidates, state.SelectedSteps[relaxedReplacementIndex]);
            RemoveSelectedAt(candidates, state, relaxedReplacementIndex);
            candidate = FindBestSelectableCandidate(candidates, state, 0, predicate);
            if (candidate == null)
            {
                if (relaxedRemovedCandidate != null)
                    SelectCandidate(relaxedRemovedCandidate, state);

                return false;
            }

            SelectCandidate(candidate, state);
            return true;
        }

        private static SelectionCandidate FindBestSelectableCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing,
            Predicate<SelectionCandidate> predicate)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                if (predicate(candidate) &&
                    CandidatePassesBaseConstraints(candidate, state, minSpacing))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static int FindReplacementIndexForBehaviourGuarantee(
            List<SelectionCandidate> candidates,
            SelectionState state,
            Predicate<SelectionCandidate> protectedPredicate)
        {
            int bestIndex = -1;
            SelectionCandidate bestCandidate = null;

            for (int i = 0; i < state.SelectedSteps.Count; i++)
            {
                SelectionCandidate candidate = FindCandidateByStep(candidates, state.SelectedSteps[i]);
                if (candidate == null ||
                    protectedPredicate(candidate) ||
                    candidate.AnchorPriority != AnchorPriority.NonAnchor)
                {
                    continue;
                }

                if (bestCandidate == null || IsBetterReplacementRemoval(candidate, bestCandidate))
                {
                    bestCandidate = candidate;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static bool IsInterstitialCandidate(SelectionCandidate candidate)
        {
            return candidate.InterstitialSourceGap;
        }

        private static bool IsLateDriveCandidate(SelectionCandidate candidate)
        {
            return candidate.InEndingRegion &&
                   (candidate.MetricStrengthLevel == MetricStrengthLevel.Strong ||
                    candidate.MetricStrengthLevel == MetricStrengthLevel.Medium ||
                    candidate.MetricStrengthLevel == MetricStrengthLevel.Strongest);
        }

        private static int FindReplacementIndexForEnding(
            List<SelectionCandidate> candidates,
            SelectionState state)
        {
            int bestIndex = -1;
            SelectionCandidate bestCandidate = null;

            for (int i = 0; i < state.SelectedSteps.Count; i++)
            {
                SelectionCandidate candidate = FindCandidateByStep(candidates, state.SelectedSteps[i]);
                if (candidate == null || candidate.InEndingRegion)
                    continue;

                if (bestCandidate == null || IsBetterReplacementRemoval(candidate, bestCandidate))
                {
                    bestCandidate = candidate;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static bool IsBetterReplacementRemoval(
            SelectionCandidate candidate,
            SelectionCandidate currentBest)
        {
            if (candidate.AnchorPriority != currentBest.AnchorPriority)
                return candidate.AnchorPriority < currentBest.AnchorPriority;

            if (candidate.Score != currentBest.Score)
                return candidate.Score < currentBest.Score;

            if (candidate.MetricalWeight != currentBest.MetricalWeight)
                return candidate.MetricalWeight < currentBest.MetricalWeight;

            return candidate.StepIndex > currentBest.StepIndex;
        }

        private static void SelectCandidate(SelectionCandidate candidate, SelectionState state)
        {
            state.ActiveSteps[candidate.StepIndex] = true;
            state.SelectedSteps.Add(candidate.StepIndex);

            if (candidate.SegmentIndex >= 0 && candidate.SegmentIndex < state.SegmentCounts.Length)
                state.SegmentCounts[candidate.SegmentIndex]++;

            if (candidate.InEndingRegion)
                state.HasEnding = true;
        }

        private static void RemoveSelectedAt(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int selectedListIndex)
        {
            int stepIndex = state.SelectedSteps[selectedListIndex];
            SelectionCandidate candidate = FindCandidateByStep(candidates, stepIndex);

            state.ActiveSteps[stepIndex] = false;
            state.SelectedSteps.RemoveAt(selectedListIndex);

            if (candidate != null &&
                candidate.SegmentIndex >= 0 &&
                candidate.SegmentIndex < state.SegmentCounts.Length)
            {
                state.SegmentCounts[candidate.SegmentIndex]--;
            }

            state.HasEnding = HasSelectedEnding(candidates, state);
        }

        private static bool HasSelectedEnding(
            List<SelectionCandidate> candidates,
            SelectionState state)
        {
            for (int i = 0; i < state.SelectedSteps.Count; i++)
            {
                SelectionCandidate candidate = FindCandidateByStep(candidates, state.SelectedSteps[i]);
                if (candidate != null && candidate.InEndingRegion)
                    return true;
            }

            return false;
        }

        private static bool ExistsSelectableEndingCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            return FindFirstSelectableEndingCandidate(candidates, state, minSpacing) != null;
        }

        private static SelectionCandidate FindFirstSelectableEndingCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                if (candidate.InEndingRegion && CandidatePassesBaseConstraints(candidate, state, minSpacing))
                    return candidate;
            }

            return null;
        }

        private static int CountSelectableNonWeakCandidates(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            int count = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                if (!candidate.IsWeakMetrical &&
                    CandidatePassesBaseConstraints(candidate, state, minSpacing))
                {
                    count++;
                }
            }

            return count;
        }

        private static float GetBestSelectableNonWeakScore(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                if (!candidate.IsWeakMetrical &&
                    CandidatePassesBaseConstraints(candidate, state, minSpacing))
                {
                    return candidate.Score;
                }
            }

            return float.NegativeInfinity;
        }

        private static int CountSelectableCandidates(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing)
        {
            int count = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (CandidatePassesBaseConstraints(candidates[i], state, minSpacing))
                    count++;
            }

            return count;
        }

        private static bool ExistsSelectableUnderfilledSegmentCandidate(
            List<SelectionCandidate> candidates,
            SelectionState state,
            int minSpacing,
            int overfilledSegment,
            int softLimit)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SelectionCandidate candidate = candidates[i];
                int segmentIndex = candidate.SegmentIndex;

                if (segmentIndex < 0 ||
                    segmentIndex >= state.SegmentCounts.Length ||
                    segmentIndex == overfilledSegment ||
                    state.SegmentCounts[segmentIndex] >= softLimit)
                {
                    continue;
                }

                if (CandidatePassesBaseConstraints(candidate, state, minSpacing))
                    return true;
            }

            return false;
        }

        private static bool CandidatePassesBaseConstraints(
            SelectionCandidate candidate,
            SelectionState state,
            int minSpacing)
        {
            return !state.ActiveSteps[candidate.StepIndex] &&
                   !ViolatesSpacing(candidate, state, minSpacing);
        }

        private static bool ViolatesSpacing(
            SelectionCandidate candidate,
            SelectionState state,
            int minSpacing)
        {
            if (minSpacing <= 0)
                return false;

            for (int i = 0; i < state.SelectedSteps.Count; i++)
            {
                if (Math.Abs(candidate.StepIndex - state.SelectedSteps[i]) < minSpacing)
                    return true;
            }

            return false;
        }

        private static SelectionCandidate FindCandidateByStep(
            List<SelectionCandidate> candidates,
            int stepIndex)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].StepIndex == stepIndex)
                    return candidates[i];
            }

            return null;
        }

        private static int CompareCandidates(SelectionCandidate left, SelectionCandidate right)
        {
            int scoreComparison = right.Score.CompareTo(left.Score);
            if (scoreComparison != 0)
                return scoreComparison;

            int metricComparison = right.MetricalWeight.CompareTo(left.MetricalWeight);
            if (metricComparison != 0)
                return metricComparison;

            return left.StepIndex.CompareTo(right.StepIndex);
        }

        private static float ResolveMetricalWeight(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.MetricalWeights, stepIndex))
            {
                ValidateFinite(context.MetricalWeights[stepIndex], nameof(context.MetricalWeights));
                return context.MetricalWeights[stepIndex];
            }

            if (stepScore.MetricalWeight != 0f)
            {
                ValidateFinite(stepScore.MetricalWeight, nameof(stepScore.MetricalWeight));
                return stepScore.MetricalWeight;
            }

            if (HasFlag(stepScore.Flags, SkeletonReasonFlags.MetricStrong))
                return 1f;

            if (HasFlag(stepScore.Flags, SkeletonReasonFlags.MetricWeak))
                return 0.20f;

            return 0f;
        }

        private static float ResolveSourceRelationScore(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.SourceRelationScores, stepIndex))
            {
                ValidateFinite(context.SourceRelationScores[stepIndex], nameof(context.SourceRelationScores));
                return context.SourceRelationScores[stepIndex];
            }

            ValidateFinite(stepScore.SourceRelationScore, nameof(stepScore.SourceRelationScore));
            return stepScore.SourceRelationScore;
        }

        private static AnchorPriority ResolveAnchorPriority(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (IsTrue(context.ExplicitAnchors, stepIndex) || stepScore.IsExplicitAnchor)
                return AnchorPriority.Explicit;

            if (IsTrue(context.FallbackAnchors, stepIndex) || stepScore.IsFallbackAnchor)
                return AnchorPriority.Fallback;

            if (context.PreserveAnchors &&
                (HasFlag(stepScore.Flags, SkeletonReasonFlags.ProtectedAnchor) ||
                 HasFlag(stepScore.Flags, SkeletonReasonFlags.AnchorBoosted)))
            {
                return AnchorPriority.Fallback;
            }

            return AnchorPriority.NonAnchor;
        }

        private static bool ResolveEndingRegion(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.EndingSteps, stepIndex))
                return context.EndingSteps[stepIndex];

            if (stepScore.InEndingRegion)
                return true;

            if (!context.RequireStrongEnding)
                return false;

            int endingWindow = context.EndingWindowSteps > 0
                ? Math.Min(context.TotalSteps, context.EndingWindowSteps)
                : Math.Max(1, context.TotalSteps / DefaultSegmentCount);

            return stepIndex >= context.TotalSteps - endingWindow;
        }

        private static bool ResolveWeakMetrical(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex,
            float metricalWeight)
        {
            if (HasValue(context.WeakMetricalSteps, stepIndex))
                return context.WeakMetricalSteps[stepIndex];

            if (stepScore.IsWeakMetrical || HasFlag(stepScore.Flags, SkeletonReasonFlags.MetricWeak))
                return true;

            return metricalWeight > 0f && metricalWeight <= MetricWeakThreshold;
        }

        private static int ResolveSegment(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.SegmentByStep, stepIndex))
                return context.SegmentByStep[stepIndex];

            if (stepScore.SegmentIndex >= 0)
                return stepScore.SegmentIndex;

            return Math.Min(
                DefaultSegmentCount - 1,
                stepIndex * DefaultSegmentCount / context.TotalSteps);
        }

        private static bool ResolveSourceOccupied(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.SourceOccupiedSteps, stepIndex))
                return context.SourceOccupiedSteps[stepIndex];

            return stepScore.SourceOccupied;
        }

        private static bool ResolveAdjacentToSource(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.AdjacentToSourceSteps, stepIndex))
                return context.AdjacentToSourceSteps[stepIndex];

            return stepScore.AdjacentToSource;
        }

        private static bool ResolveNearSource(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.NearSourceSteps, stepIndex))
                return context.NearSourceSteps[stepIndex];

            return stepScore.NearSource;
        }

        private static bool ResolveInterstitialSourceGap(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.InterstitialSourceGapSteps, stepIndex))
                return context.InterstitialSourceGapSteps[stepIndex];

            return stepScore.InterstitialSourceGap;
        }

        private static int ResolveLocalSourceDensity(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex)
        {
            if (HasValue(context.LocalSourceDensity, stepIndex))
                return context.LocalSourceDensity[stepIndex];

            return Math.Max(0, stepScore.LocalSourceDensity);
        }

        private static MetricStrengthLevel ResolveMetricStrengthLevel(
            SkeletonContext context,
            int stepIndex,
            float metricalWeight)
        {
            if (context.MetricStrengthLevels != null &&
                stepIndex >= 0 &&
                stepIndex < context.MetricStrengthLevels.Length)
            {
                return context.MetricStrengthLevels[stepIndex];
            }

            if (metricalWeight >= 0.95f)
                return MetricStrengthLevel.Strongest;

            if (metricalWeight >= 0.65f)
                return MetricStrengthLevel.Strong;

            if (metricalWeight >= 0.40f)
                return MetricStrengthLevel.Medium;

            return MetricStrengthLevel.Weak;
        }

        private static float ResolveSegmentTargetWeight(
            StepScore stepScore,
            SkeletonContext context,
            int stepIndex,
            int segmentIndex)
        {
            if (HasValue(context.SegmentTargetWeightsByStep, stepIndex))
            {
                ValidateFinite(context.SegmentTargetWeightsByStep[stepIndex], nameof(context.SegmentTargetWeightsByStep));
                return context.SegmentTargetWeightsByStep[stepIndex];
            }

            if (context.BehaviourProfile != null)
                return context.BehaviourProfile.GetSegmentTargetWeight(segmentIndex);

            ValidateFinite(stepScore.SegmentTargetWeight, nameof(stepScore.SegmentTargetWeight));
            return stepScore.SegmentTargetWeight > 0f ? stepScore.SegmentTargetWeight : 1f;
        }

        private static int GetTargetCount(float targetDensity, int totalSteps)
        {
            int targetCount = (int)Math.Round(
                targetDensity * totalSteps,
                MidpointRounding.AwayFromZero);

            if (targetCount < 1)
                return 1;

            if (targetCount > totalSteps)
                return totalSteps;

            return targetCount;
        }

        private static int GetSegmentCount(List<SelectionCandidate> candidates)
        {
            int maxSegment = -1;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].SegmentIndex > maxSegment)
                    maxSegment = candidates[i].SegmentIndex;
            }

            return Math.Max(1, maxSegment + 1);
        }

        private static int GetSegmentSoftLimit(int targetCount, int segmentCount)
        {
            int limit = (targetCount + segmentCount - 1) / segmentCount;
            if (targetCount > segmentCount)
                limit++;

            return Math.Max(1, limit);
        }

        private static void ApplySelectionToStepMeta(
            SkeletonStepMeta[] stepMeta,
            bool[] activeSteps,
            int minSpacing,
            IReadOnlyList<int> forcedEndingSteps)
        {
            for (int i = 0; i < stepMeta.Length; i++)
            {
                stepMeta[i].Selected = activeSteps[i];

                if (activeSteps[i] && Contains(forcedEndingSteps, i))
                    stepMeta[i].ReasonFlags |= SkeletonReasonFlags.ForcedEnding;

                if (!activeSteps[i] && IsSuppressedBySelectedSpacing(i, activeSteps, minSpacing))
                    stepMeta[i].ReasonFlags |= SkeletonReasonFlags.SpacingSuppressed;
            }
        }

        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            if (values == null)
                return false;

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == value)
                    return true;
            }

            return false;
        }

        private static bool IsSuppressedBySelectedSpacing(
            int stepIndex,
            bool[] activeSteps,
            int minSpacing)
        {
            if (minSpacing <= 0)
                return false;

            for (int i = 0; i < activeSteps.Length; i++)
            {
                if (!activeSteps[i])
                    continue;

                if (Math.Abs(stepIndex - i) < minSpacing)
                    return true;
            }

            return false;
        }

        private static int[] BuildSelectedStepIndices(bool[] activeSteps)
        {
            int activeCount = 0;
            for (int i = 0; i < activeSteps.Length; i++)
            {
                if (activeSteps[i])
                    activeCount++;
            }

            var selectedStepIndices = new int[activeCount];
            int selectedIndex = 0;

            for (int i = 0; i < activeSteps.Length; i++)
            {
                if (activeSteps[i])
                    selectedStepIndices[selectedIndex++] = i;
            }

            return selectedStepIndices;
        }

        private static float[] BuildSelectionScores(SkeletonStepMeta[] stepMeta)
        {
            var selectionScores = new float[stepMeta.Length];
            for (int i = 0; i < stepMeta.Length; i++)
                selectionScores[i] = stepMeta[i].FinalScore;

            return selectionScores;
        }

        private static float ScoreMetric(
            SkeletonBuilderConfig config,
            ResponseBehaviourProfile profile,
            float metricSalience,
            bool isStrongBeat,
            MetricStrengthLevel strengthLevel)
        {
            float metricWeight = Clamp01(config.MetricStrengthWeight);
            float strongBeatWeight = Clamp01(config.StrongBeatPreferenceWeight);
            float weightedSalience = metricSalience * metricWeight;
            float strongBeatBoost = isStrongBeat
                ? Math.Min(1f - weightedSalience, 0.20f * strongBeatWeight)
                : 0f;
            float profileBias = profile != null ? profile.GetMetricClassBias(strengthLevel) : 0f;

            return SanitizeScore(
                Clamp01(weightedSalience + strongBeatBoost + profileBias));
        }

        private static float ScoreSourceRelation(
            SkeletonBuilderConfig config,
            ResponseBehaviourProfile profile,
            bool sourceOccupied,
            bool sourceAnchor,
            bool sourceGap,
            bool adjacentToSource,
            bool nearSource,
            bool interstitialSourceGap,
            int distanceToNearestSource,
            int localSourceDensity,
            float segmentSourceWeight,
            float metricSalience,
            bool isStrongBeat)
        {
            if (profile == null)
                profile = ResponseBehaviourProfile.Neutral(ResponseType.Mirror, 0.5f, DefaultSegmentCount);

            float complementarity = Clamp01(profile.ComplementarityBias);
            bool sourceRelated = sourceOccupied || sourceAnchor;
            float mirrorScale = config.MirrorWeight;
            float complementScale = config.ComplementWeight * (0.85f + complementarity * 0.30f);
            float relation = 0f;

            if (sourceOccupied)
                relation += profile.SourceHitWeight * mirrorScale;

            if (sourceAnchor && !sourceOccupied)
                relation += profile.SourceHitWeight * 0.65f * mirrorScale;

            if (sourceGap)
                relation += profile.SourceGapWeight * complementScale;

            if (adjacentToSource)
                relation += profile.AdjacentSourceWeight * complementScale;

            if (nearSource && !adjacentToSource)
                relation += profile.NearSourceWeight * complementScale;

            if (!sourceRelated && distanceToNearestSource > 2)
                relation += profile.FarSourceWeight * complementScale;

            if (interstitialSourceGap)
                relation += profile.InterstitialWeight * complementScale;

            relation += localSourceDensity > 1
                ? profile.DenseSourceRegionWeight * Math.Min(1f, localSourceDensity / 3f)
                : profile.SparseSourceRegionWeight * (1f - Math.Min(1f, segmentSourceWeight * 2f));

            if (profile.SourceOverlapPolicy == SourceOverlapPolicy.ReduceToAnchors &&
                sourceOccupied &&
                !sourceAnchor &&
                metricSalience < 0.75f)
            {
                relation -= 0.16f;
            }

            if (profile.SourceOverlapPolicy == SourceOverlapPolicy.Displace &&
                adjacentToSource &&
                !isStrongBeat)
            {
                relation += 0.08f;
            }

            if (profile.SourceOverlapPolicy == SourceOverlapPolicy.ExpandAroundSource &&
                nearSource &&
                metricSalience >= 0.50f)
            {
                relation += 0.10f;
            }

            if (profile.SourceOverlapPolicy == SourceOverlapPolicy.Interstitial &&
                interstitialSourceGap &&
                !isStrongBeat)
            {
                relation += 0.12f + (1f - metricSalience) * 0.10f;
            }

            float rawScore = relation * GetSourceRelationResponseTypeMultiplier(profile.ResponseType);
            float scaled = rawScore * config.SourceRelationScale;

            return SanitizeScore(ClampMagnitude(scaled, config.SourceRelationMaxMagnitude));
        }

        private static float GetSourceRelationResponseTypeMultiplier(ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Mirror:
                    return 1.2f;

                case ResponseType.Complement:
                    return 1.25f;

                case ResponseType.Fill:
                case ResponseType.Contrast:
                    return 1.3f;

                case ResponseType.Simplify:
                    return 0.85f;

                case ResponseType.Intensify:
                    return 1.1f;

                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
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
            SkeletonBuilderConfig config,
            ResponseBehaviourProfile profile,
            bool inEndingRegion,
            int stepsFromEnd,
            int stepsPerQuarter,
            bool sourceOccupied,
            bool sourceGap,
            float metricSalience,
            bool isStrongBeat,
            bool interstitialSourceGap)
        {
            if (!inEndingRegion)
                return 0f;

            if (profile == null)
                profile = ResponseBehaviourProfile.Neutral(ResponseType.Mirror, 0.5f, DefaultSegmentCount);

            float proximity = GetEndingProximity(stepsFromEnd, stepsPerQuarter);
            float lateWeight = 0.35f + proximity * 0.65f;
            float strongFactor = isStrongBeat ? 1f : (metricSalience >= 0.50f ? 0.45f : 0f);
            float finalAccent = stepsFromEnd == 0 ? 1f : 0f;
            float directionalScore;

            if (profile.RequireStrongEnding || profile.EndingPolicy == EndingPolicy.MirrorSource)
            {
                directionalScore = (sourceOccupied ? 0.75f : 0.20f) * lateWeight;
                if (isStrongBeat)
                    directionalScore += 0.20f * lateWeight;

                return SanitizeScore(ClampMagnitude(config.EndingInfluenceWeight * EndingScale * directionalScore, EndingMaxMagnitude));
            }

            switch (profile.EndingPolicy)
            {
                case EndingPolicy.StructuralOnly:
                    directionalScore = (strongFactor - (metricSalience < 0.50f ? 0.35f : 0f)) * lateWeight;
                    break;

                case EndingPolicy.FillGap:
                    directionalScore = (sourceGap ? 0.48f : -0.20f) * lateWeight;
                    if (interstitialSourceGap)
                        directionalScore += 0.16f * lateWeight;
                    break;

                case EndingPolicy.AvoidAccent:
                    directionalScore = (finalAccent > 0f ? -0.60f : 0.20f + (1f - metricSalience) * 0.25f) * lateWeight;
                    break;

                case EndingPolicy.LateDrive:
                    directionalScore = (strongFactor + finalAccent * 0.35f) * lateWeight;
                    if (!sourceOccupied && metricSalience >= 0.50f)
                        directionalScore += 0.18f * lateWeight;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(profile.EndingPolicy), profile.EndingPolicy, "Unknown ending policy.");
            }

            return SanitizeScore(ClampMagnitude(config.EndingInfluenceWeight * EndingScale * directionalScore, EndingMaxMagnitude));
        }

        private static float ScorePhraseBalance(
            SkeletonBuilderConfig config,
            ResponseBehaviourProfile profile,
            int segmentIndex)
        {
            if (!config.RebalanceAcrossSegments)
                return 0f;

            float legacyBalance = segmentIndex == 1 || segmentIndex == 2
                ? 0.015f
                : (segmentIndex == 3 ? 0.008f : 0f);

            if (profile == null)
                return ClampMagnitude(legacyBalance, PhraseBalanceMaxMagnitude);

            float targetWeight = profile.GetSegmentTargetWeight(segmentIndex);
            float profileScore = (targetWeight - 1f) * profile.SegmentInfluence;
            return SanitizeScore(ClampMagnitude(legacyBalance + profileScore, PhraseBalanceMaxMagnitude));
        }

        private static float ScoreDensityShaping(
            ResponseBehaviourProfile profile,
            SkeletonBuilderConfig config,
            MetricStrengthLevel strengthLevel)
        {
            if (profile == null)
                profile = ResponseBehaviourProfile.Neutral(ResponseType.Mirror, 0.5f, DefaultSegmentCount);

            float targetDensity = Clamp01(profile.TargetDensity);
            float score = 0f;

            if (targetDensity < 0.35f)
            {
                float lowAmount = (0.35f - targetDensity) / 0.35f;
                switch (strengthLevel)
                {
                    case MetricStrengthLevel.Strongest:
                        score = 0.08f * lowAmount;
                        break;

                    case MetricStrengthLevel.Strong:
                        score = 0.03f * lowAmount;
                        break;

                    case MetricStrengthLevel.Medium:
                        score = 0f;
                        break;

                    case MetricStrengthLevel.Weak:
                        score = -0.03f * lowAmount;
                        break;

                    default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
                }
            }
            else if (targetDensity > 0.65f)
            {
                float highAmount = (targetDensity - 0.65f) / 0.35f;
                switch (strengthLevel)
                {
                    case MetricStrengthLevel.Strongest:
                        score = 0f;
                        break;

                    case MetricStrengthLevel.Strong:
                        score = 0.02f * highAmount;
                        break;

                    case MetricStrengthLevel.Medium:
                        score = config.HighDensityMediumBoost * highAmount;
                        break;

                    case MetricStrengthLevel.Weak:
                        score = config.HighDensityWeakBoost * highAmount;
                        break;

                    default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
                }
            }

            switch (profile.MetricTolerancePolicy)
            {
                case MetricTolerancePolicy.OffbeatFriendly:
                    if (strengthLevel == MetricStrengthLevel.Weak ||
                        strengthLevel == MetricStrengthLevel.Medium)
                    {
                        score += 0.06f;
                    }

                    break;

                case MetricTolerancePolicy.Broad:
                    if (strengthLevel != MetricStrengthLevel.Strongest)
                        score += 0.025f;
                    break;

                case MetricTolerancePolicy.StrongAndMedium:
                    if (strengthLevel == MetricStrengthLevel.Medium)
                        score += 0.015f;
                    break;

                case MetricTolerancePolicy.SpineOnly:
                    if (strengthLevel != MetricStrengthLevel.Strongest)
                        score -= strengthLevel == MetricStrengthLevel.Strong ? 0.04f : 0.08f;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(profile.MetricTolerancePolicy), profile.MetricTolerancePolicy, "Unknown metric tolerance policy.");
            }

            if (profile.ResponseType == ResponseType.Intensify &&
                targetDensity > 0.45f &&
                strengthLevel == MetricStrengthLevel.Medium)
            {
                score += 0.035f;
            }

            return SanitizeScore(ClampMagnitude(score, DensityShapingMaxMagnitude));
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

        private static SkeletonPatternSummary CreateSummary(
            SkeletonBuildRequest request,
            bool[] activeSteps,
            SkeletonStepMeta[] stepMeta)
        {
            int activeCount = 0;
            int sourceOverlapCount = 0;
            int anchorAlignedCount = 0;

            for (int i = 0; i < activeSteps.Length; i++)
            {
                if (!activeSteps[i])
                    continue;

                activeCount++;

                if (stepMeta[i].SourceOccupied)
                    sourceOverlapCount++;

                if (stepMeta[i].SourceAnchor)
                    anchorAlignedCount++;
            }

            float achievedDensity = activeCount / (float)request.TurnLengthSteps;
            bool densityTargetMet = Math.Abs(achievedDensity - Clamp01(request.Plan.TargetDensity)) <=
                                    request.Config.DensityTolerance;

            return new SkeletonPatternSummary(
                activeCount: activeCount,
                achievedDensity: achievedDensity,
                sourceOverlapCount: sourceOverlapCount,
                anchorAlignedCount: anchorAlignedCount,
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
            ValidateNonNegativeFinite(config.SourceRelationScale, nameof(config.SourceRelationScale));
            ValidateNonNegativeFinite(config.SourceRelationMaxMagnitude, nameof(config.SourceRelationMaxMagnitude));
            ValidateNonNegativeFinite(config.HighDensityWeakBoost, nameof(config.HighDensityWeakBoost));
            ValidateNonNegativeFinite(config.HighDensityMediumBoost, nameof(config.HighDensityMediumBoost));
            ValidateNonNegativeFinite(config.DensityTolerance, nameof(config.DensityTolerance));
            ValidateNonNegativeFinite(config.SelectionJitter, nameof(config.SelectionJitter));

            if (config.MinimumStepSpacing < 0)
                throw new ArgumentOutOfRangeException(nameof(config.MinimumStepSpacing), "Minimum step spacing cannot be negative.");

            if (config.MaximumClusterSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(config.MaximumClusterSize), "Maximum cluster size must be greater than zero.");
        }

        private static void ValidateSelectionContext(SkeletonContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.TotalSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(context.TotalSteps), "Total steps must be greater than zero.");

            if (context.MinSpacingSteps < 0)
                throw new ArgumentOutOfRangeException(nameof(context.MinSpacingSteps), "Minimum spacing cannot be negative.");

            ValidateFinite(context.TargetDensity, nameof(context.TargetDensity));
            ValidateNonNegativeFinite(context.DensityTolerance, nameof(context.DensityTolerance));
        }

        private static bool HasValue(bool[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length;
        }

        private static bool HasValue(float[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length;
        }

        private static bool HasValue(int[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length;
        }

        private static bool IsTrue(bool[] values, int index)
        {
            return HasValue(values, index) && values[index];
        }

        private static bool HasFlag(SkeletonReasonFlags actual, SkeletonReasonFlags expected)
        {
            return (actual & expected) == expected;
        }

        private sealed class SelectionCandidate
        {
            public SelectionCandidate(
                int stepIndex,
                float score,
                float metricalWeight,
                float sourceRelationScore,
                AnchorPriority anchorPriority,
                bool inEndingRegion,
                bool isWeakMetrical,
                int segmentIndex,
                bool sourceOccupied,
                bool adjacentToSource,
                bool nearSource,
                bool interstitialSourceGap,
                int localSourceDensity,
                MetricStrengthLevel metricStrengthLevel,
                float segmentTargetWeight)
            {
                StepIndex = stepIndex;
                Score = score;
                MetricalWeight = metricalWeight;
                SourceRelationScore = sourceRelationScore;
                AnchorPriority = anchorPriority;
                InEndingRegion = inEndingRegion;
                IsWeakMetrical = isWeakMetrical;
                SegmentIndex = segmentIndex;
                SourceOccupied = sourceOccupied;
                AdjacentToSource = adjacentToSource;
                NearSource = nearSource;
                InterstitialSourceGap = interstitialSourceGap;
                LocalSourceDensity = localSourceDensity;
                MetricStrengthLevel = metricStrengthLevel;
                SegmentTargetWeight = segmentTargetWeight;
            }

            public int StepIndex { get; private set; }
            public float Score { get; private set; }
            public float MetricalWeight { get; private set; }
            public float SourceRelationScore { get; private set; }
            public AnchorPriority AnchorPriority { get; private set; }
            public bool InEndingRegion { get; private set; }
            public bool IsWeakMetrical { get; private set; }
            public int SegmentIndex { get; private set; }
            public bool SourceOccupied { get; private set; }
            public bool AdjacentToSource { get; private set; }
            public bool NearSource { get; private set; }
            public bool InterstitialSourceGap { get; private set; }
            public int LocalSourceDensity { get; private set; }
            public MetricStrengthLevel MetricStrengthLevel { get; private set; }
            public float SegmentTargetWeight { get; private set; }
        }

        private sealed class SelectionState
        {
            public SelectionState(int totalSteps, int segmentCount)
            {
                ActiveSteps = new bool[totalSteps];
                SelectedSteps = new List<int>();
                SegmentCounts = new int[Math.Max(1, segmentCount)];
                HasEnding = false;
            }

            public bool[] ActiveSteps { get; private set; }
            public List<int> SelectedSteps { get; private set; }
            public int[] SegmentCounts { get; private set; }
            public bool HasEnding { get; set; }

            public int SelectedCount
            {
                get { return SelectedSteps.Count; }
            }
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
