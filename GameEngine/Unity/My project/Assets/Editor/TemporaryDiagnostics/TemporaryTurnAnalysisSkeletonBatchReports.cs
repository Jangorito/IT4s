using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using IT4s.Data;
using IT4s.Rhythm.Generation.Skeleton;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;
using IT4s.Rhythm.TurnAnalysis.Utils;
using UnityEngine;

namespace IT4s.Diagnostics.Temporary
{
    [Serializable]
    internal sealed class TemporarySkeletonBatchReport
    {
        public string generationMode;
        public string builderVersion;
        public int sourceCaseCount;
        public int responseEvaluationCount;
        public TemporarySkeletonCaseReport[] cases;
        public TemporarySkeletonAggregateSummary[] aggregates;
    }

    [Serializable]
    internal sealed class TemporarySkeletonCaseReport
    {
        public string sourceId;
        public string sourceLabel;
        public string sourceCategory;
        public string variantKind;
        public string parentCaseId;
        public string plannerSelectedResponseType;
        public string plannerDescriptorSummary;
        public TemporarySourceSummary sourceSummary;
        public TemporaryAnalysisSummary analysisSummary;
        public TemporarySkeletonEvaluationReport[] responseEvaluations;
        public TemporaryResponseTypeComparisonReport responseTypeComparison;
    }

    [Serializable]
    internal sealed class TemporarySourceSummary
    {
        public int totalSteps;
        public int sourceHitCount;
        public float sourceDensity;
        public int[] sourceHitIndices;
        public int[] sourceSegmentCounts;
        public int sourceEndingHitCount;
        public int[] sourceAnchorIndices;
        public int sourceAnchorCount;
    }

    [Serializable]
    internal sealed class TemporaryAnalysisSummary
    {
        public float density;
        public float meanEnergy;
        public int peakEnergy;
        public int anchorCount;
        public bool hasOpeningAnchor;
        public bool hasClosingAnchor;
        public float endDensity;
        public float endEnergy;
        public int endAccent;
        public string densityShape;
        public string energyShape;
    }

    [Serializable]
    internal sealed class TemporarySkeletonEvaluationReport
    {
        public string sourceId;
        public string sourceLabel;
        public string sourceCategory;
        public string responseType;
        public string builderVersion;
        public bool plannerSelected;
        public TemporaryPlanSummary planSummary;
        public TemporarySkeletonOutputSummary skeletonSummary;
        public TemporaryRelationshipMetrics relationshipToSource;
        public TemporaryDistributionMetrics distributionMetrics;
        public TemporarySelectedStepDebug[] selectedStepBreakdown;
    }

    [Serializable]
    internal sealed class TemporaryPlanSummary
    {
        public float targetDensity;
        public int targetCount;
        public bool preserveAnchors;
        public bool mirrorEnding;
        public float complementarityBias;
    }

    [Serializable]
    internal sealed class TemporarySkeletonOutputSummary
    {
        public int[] selectedHitIndices;
        public int selectedHitCount;
        public float selectedDensity;
        public int selectedWeakCount;
        public int selectedMediumCount;
        public int selectedStrongCount;
        public int selectedStrongestCount;
        public int[] selectedSegmentCounts;
        public int selectedEndingHitCount;
        public int selectedAnchorHitCount;
    }

    [Serializable]
    internal sealed class TemporaryRelationshipMetrics
    {
        public int overlapCount;
        public int gapFillCount;
        public float overlapRatio;
        public float gapFillRatio;
        public float anchorPreservationRatio;
        public int endingOverlapCount;
        public int endingGapCount;
    }

    [Serializable]
    internal sealed class TemporaryDistributionMetrics
    {
        public float weakStepRatio;
        public float strongestStepRatio;
        public TemporaryMetricTierDistribution metricTierDistribution;
        public float segmentDifferenceFromSource;
        public float endingBias;
        public float interstitialBias;
    }

    [Serializable]
    internal sealed class TemporaryMetricTierDistribution
    {
        public int weakCount;
        public int mediumCount;
        public int strongCount;
        public int strongestCount;
        public float weakRatio;
        public float mediumRatio;
        public float strongRatio;
        public float strongestRatio;
    }

    [Serializable]
    internal sealed class TemporarySelectedStepDebug
    {
        public int stepIndex;
        public float finalScore;
        public float metricScore;
        public float sourceRelationScore;
        public float anchorScore;
        public float endingScore;
        public float densityScore;
        public float phraseScore;
        public string metricTier;
        public bool sourceOccupied;
        public bool anchor;
        public bool endingRegion;
        public bool forcedByEndingEnforcement;
    }

    [Serializable]
    internal sealed class TemporaryResponseTypeComparisonReport
    {
        public TemporaryResponseTypeComparisonMetric[] responseTypes;
        public TemporaryPairwiseDivergence[] pairwiseDivergence;
        public float averagePairwiseDivergence;
    }

    [Serializable]
    internal sealed class TemporaryResponseTypeComparisonMetric
    {
        public string responseType;
        public int hitCount;
        public float overlapRatio;
        public float gapFillRatio;
        public float weakStepRatio;
        public float strongestStepRatio;
        public float endingOccupancy;
        public int[] segmentCounts;
    }

    [Serializable]
    internal sealed class TemporaryPairwiseDivergence
    {
        public string leftResponseType;
        public string rightResponseType;
        public float jaccardDistance;
    }

    [Serializable]
    internal sealed class TemporarySkeletonAggregateSummary
    {
        public string builderVersion;
        public string responseType;
        public int sourceCaseCount;
        public float averageOverlapRatio;
        public float averageGapFillRatio;
        public float averageWeakStepRatio;
        public float averageStrongestStepRatio;
        public float averageEndingOccupancy;
        public float averageAnchorPreservationRatio;
        public float averagePairwiseDivergence;
    }

    internal static class TemporaryTurnAnalysisSkeletonReportBuilder
    {
        private enum TemporaryMetricTier
        {
            Weak,
            Medium,
            Strong,
            Strongest
        }

        private static readonly ResponseType[] ResponseTypes =
        {
            ResponseType.Mirror,
            ResponseType.Complement,
            ResponseType.Simplify,
            ResponseType.Intensify,
            ResponseType.Contrast,
            ResponseType.Fill
        };

        public static TemporarySkeletonCaseReport BuildCaseReport(
            SyntheticTurnCase syntheticCase,
            PatternTurn patternTurn,
            TurnAnalysisResult analysis,
            ResponsePlan selectedPlan,
            ResponsePlannerDebugSnapshot plannerSnapshot,
            ResponsePlanner planner,
            SkeletonBuilder skeletonBuilder,
            SkeletonBuilderConfig skeletonBuilderConfig,
            string builderVersion)
        {
            if (syntheticCase == null)
                throw new ArgumentNullException(nameof(syntheticCase));
            if (patternTurn == null)
                throw new ArgumentNullException(nameof(patternTurn));
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));
            if (selectedPlan == null)
                throw new ArgumentNullException(nameof(selectedPlan));
            if (planner == null)
                throw new ArgumentNullException(nameof(planner));
            if (skeletonBuilder == null)
                throw new ArgumentNullException(nameof(skeletonBuilder));
            if (skeletonBuilderConfig == null)
                throw new ArgumentNullException(nameof(skeletonBuilderConfig));

            TemporarySourceSummary sourceSummary = BuildSourceSummary(patternTurn, analysis);
            var evaluations = new TemporarySkeletonEvaluationReport[ResponseTypes.Length];

            for (int i = 0; i < ResponseTypes.Length; i++)
            {
                ResponseType responseType = ResponseTypes[i];
                ResponsePlan plan = responseType == selectedPlan.ResponseType
                    ? selectedPlan
                    : planner.PlanForResponseType(analysis, responseType);

                SkeletonPattern skeleton = skeletonBuilder.BuildSkeleton(new SkeletonBuildRequest
                {
                    Plan = plan,
                    SourceTurn = patternTurn,
                    SourceAnalysis = analysis,
                    TurnLengthSteps = ResolveTurnLengthSteps(plan, patternTurn),
                    StepsPerQuarter = ResolveStepsPerQuarter(patternTurn),
                    Config = skeletonBuilderConfig
                });

                evaluations[i] = BuildEvaluation(
                    syntheticCase,
                    patternTurn,
                    sourceSummary,
                    plan,
                    skeleton,
                    builderVersion,
                    plannerSelected: responseType == selectedPlan.ResponseType);
            }

            return new TemporarySkeletonCaseReport
            {
                sourceId = syntheticCase.CaseId,
                sourceLabel = BuildSourceLabel(syntheticCase),
                sourceCategory = BuildSourceCategory(syntheticCase),
                variantKind = syntheticCase.VariantKind.ToString(),
                parentCaseId = syntheticCase.ParentCaseId,
                plannerSelectedResponseType = selectedPlan.ResponseType.ToString(),
                plannerDescriptorSummary = plannerSnapshot != null && plannerSnapshot.SourceDescriptorSummary != null
                    ? plannerSnapshot.SourceDescriptorSummary.Summary
                    : string.Empty,
                sourceSummary = sourceSummary,
                analysisSummary = BuildAnalysisSummary(analysis),
                responseEvaluations = evaluations,
                responseTypeComparison = BuildComparison(evaluations)
            };
        }

        public static TemporarySkeletonBatchReport BuildBatchReport(
            SyntheticDatasetGenerationMode generationMode,
            string builderVersion,
            IReadOnlyList<TemporarySkeletonCaseReport> caseReports)
        {
            if (caseReports == null)
                throw new ArgumentNullException(nameof(caseReports));

            return new TemporarySkeletonBatchReport
            {
                generationMode = generationMode.ToString(),
                builderVersion = builderVersion ?? string.Empty,
                sourceCaseCount = caseReports.Count,
                responseEvaluationCount = CountEvaluations(caseReports),
                cases = CopyCases(caseReports),
                aggregates = BuildAggregates(caseReports, builderVersion)
            };
        }

        private static TemporarySkeletonEvaluationReport BuildEvaluation(
            SyntheticTurnCase syntheticCase,
            PatternTurn patternTurn,
            TemporarySourceSummary sourceSummary,
            ResponsePlan plan,
            SkeletonPattern skeleton,
            string builderVersion,
            bool plannerSelected)
        {
            TemporarySkeletonOutputSummary skeletonSummary =
                BuildSkeletonSummary(skeleton, patternTurn);
            TemporaryRelationshipMetrics relationship =
                BuildRelationshipMetrics(skeleton, sourceSummary, skeletonSummary);
            TemporaryDistributionMetrics distribution =
                BuildDistributionMetrics(skeleton, patternTurn, sourceSummary, skeletonSummary, relationship);

            return new TemporarySkeletonEvaluationReport
            {
                sourceId = syntheticCase.CaseId,
                sourceLabel = BuildSourceLabel(syntheticCase),
                sourceCategory = BuildSourceCategory(syntheticCase),
                responseType = plan.ResponseType.ToString(),
                builderVersion = builderVersion ?? string.Empty,
                plannerSelected = plannerSelected,
                planSummary = BuildPlanSummary(plan, ResolveTurnLengthSteps(plan, patternTurn)),
                skeletonSummary = skeletonSummary,
                relationshipToSource = relationship,
                distributionMetrics = distribution,
                selectedStepBreakdown = BuildSelectedStepDebug(skeleton, ResolveStepsPerQuarter(patternTurn))
            };
        }

        private static TemporarySourceSummary BuildSourceSummary(
            PatternTurn patternTurn,
            TurnAnalysisResult analysis)
        {
            int totalSteps = patternTurn.StepCount;
            int stepsPerQuarter = ResolveStepsPerQuarter(patternTurn);
            int[] sourceHitIndices = BuildActiveSourceIndices(patternTurn);
            int[] sourceSegmentCounts = CountSegments(sourceHitIndices, totalSteps);
            int sourceEndingHitCount = CountEndingHits(sourceHitIndices, totalSteps, stepsPerQuarter);
            int[] sourceAnchorIndices = Copy(analysis.Anchor != null ? analysis.Anchor.AnchorIndices : null);

            return new TemporarySourceSummary
            {
                totalSteps = totalSteps,
                sourceHitCount = sourceHitIndices.Length,
                sourceDensity = totalSteps > 0 ? sourceHitIndices.Length / (float)totalSteps : 0f,
                sourceHitIndices = sourceHitIndices,
                sourceSegmentCounts = sourceSegmentCounts,
                sourceEndingHitCount = sourceEndingHitCount,
                sourceAnchorIndices = sourceAnchorIndices,
                sourceAnchorCount = analysis.Anchor != null ? analysis.Anchor.AnchorCount : 0
            };
        }

        private static TemporaryAnalysisSummary BuildAnalysisSummary(TurnAnalysisResult analysis)
        {
            DensityFeatures density = analysis.Density ?? new DensityFeatures();
            EnergyFeatures energy = analysis.Energy ?? new EnergyFeatures();
            AnchorFeatures anchor = analysis.Anchor ?? new AnchorFeatures();
            EndActivityFeatures endActivity = analysis.EndActivity ?? new EndActivityFeatures();
            SegmentActivityProfileFeatures profile =
                analysis.SegmentActivityProfile ?? new SegmentActivityProfileFeatures();

            return new TemporaryAnalysisSummary
            {
                density = density.StepDensity,
                meanEnergy = energy.MeanVelocity,
                peakEnergy = energy.PeakVelocity,
                anchorCount = anchor.AnchorCount,
                hasOpeningAnchor = anchor.HasOpeningAnchor,
                hasClosingAnchor = anchor.HasClosingAnchor,
                endDensity = endActivity.EndDensity,
                endEnergy = endActivity.EndEnergy,
                endAccent = endActivity.EndAccent,
                densityShape = profile.DensityShape.ToString(),
                energyShape = profile.EnergyShape.ToString()
            };
        }

        private static TemporaryPlanSummary BuildPlanSummary(ResponsePlan plan, int totalSteps)
        {
            return new TemporaryPlanSummary
            {
                targetDensity = plan.TargetDensity,
                targetCount = GetTargetCount(plan.TargetDensity, totalSteps),
                preserveAnchors = plan.PreserveAnchors,
                mirrorEnding = plan.MirrorEnding,
                complementarityBias = plan.ComplementarityBias
            };
        }

        private static TemporarySkeletonOutputSummary BuildSkeletonSummary(
            SkeletonPattern skeleton,
            PatternTurn patternTurn)
        {
            int totalSteps = skeleton.TurnLengthSteps;
            int stepsPerQuarter = ResolveStepsPerQuarter(patternTurn);
            int[] selectedHitIndices = Copy(skeleton.SelectedStepIndices);
            int[] selectedSegmentCounts = CountSegments(selectedHitIndices, totalSteps);
            int weakCount = 0;
            int mediumCount = 0;
            int strongCount = 0;
            int strongestCount = 0;
            int endingCount = 0;
            int anchorCount = 0;

            for (int i = 0; i < selectedHitIndices.Length; i++)
            {
                int stepIndex = selectedHitIndices[i];
                SkeletonStepMeta meta = GetMeta(skeleton, stepIndex);
                TemporaryMetricTier tier = GetMetricTier(stepIndex, stepsPerQuarter);

                switch (tier)
                {
                    case TemporaryMetricTier.Weak:
                        weakCount++;
                        break;
                    case TemporaryMetricTier.Medium:
                        mediumCount++;
                        break;
                    case TemporaryMetricTier.Strong:
                        strongCount++;
                        break;
                    case TemporaryMetricTier.Strongest:
                        strongestCount++;
                        break;
                }

                if (meta != null ? meta.InEndingRegion : IsInEndingRegion(stepIndex, totalSteps, stepsPerQuarter))
                    endingCount++;

                if (meta != null && meta.SourceAnchor)
                    anchorCount++;
            }

            int selectedHitCount = selectedHitIndices.Length;
            float selectedDensity = totalSteps > 0
                ? selectedHitCount / (float)totalSteps
                : 0f;

            if (skeleton.Summary != null)
            {
                selectedHitCount = skeleton.Summary.ActiveCount;
                selectedDensity = skeleton.Summary.AchievedDensity;
            }

            return new TemporarySkeletonOutputSummary
            {
                selectedHitIndices = selectedHitIndices,
                selectedHitCount = selectedHitCount,
                selectedDensity = selectedDensity,
                selectedWeakCount = weakCount,
                selectedMediumCount = mediumCount,
                selectedStrongCount = strongCount,
                selectedStrongestCount = strongestCount,
                selectedSegmentCounts = selectedSegmentCounts,
                selectedEndingHitCount = endingCount,
                selectedAnchorHitCount = anchorCount
            };
        }

        private static TemporaryRelationshipMetrics BuildRelationshipMetrics(
            SkeletonPattern skeleton,
            TemporarySourceSummary sourceSummary,
            TemporarySkeletonOutputSummary skeletonSummary)
        {
            int overlapCount = 0;
            int gapFillCount = 0;
            int endingOverlapCount = 0;
            int endingGapCount = 0;

            for (int i = 0; i < skeletonSummary.selectedHitIndices.Length; i++)
            {
                int stepIndex = skeletonSummary.selectedHitIndices[i];
                SkeletonStepMeta meta = GetMeta(skeleton, stepIndex);
                bool sourceOccupied = meta != null && meta.SourceOccupied;
                bool inEndingRegion = meta != null && meta.InEndingRegion;

                if (sourceOccupied)
                {
                    overlapCount++;
                    if (inEndingRegion)
                        endingOverlapCount++;
                }
                else
                {
                    gapFillCount++;
                    if (inEndingRegion)
                        endingGapCount++;
                }
            }

            int selectedHitCount = Math.Max(1, skeletonSummary.selectedHitCount);

            return new TemporaryRelationshipMetrics
            {
                overlapCount = overlapCount,
                gapFillCount = gapFillCount,
                overlapRatio = overlapCount / (float)selectedHitCount,
                gapFillRatio = gapFillCount / (float)selectedHitCount,
                anchorPreservationRatio = sourceSummary.sourceAnchorCount > 0
                    ? skeletonSummary.selectedAnchorHitCount / (float)sourceSummary.sourceAnchorCount
                    : 0f,
                endingOverlapCount = endingOverlapCount,
                endingGapCount = endingGapCount
            };
        }

        private static TemporaryDistributionMetrics BuildDistributionMetrics(
            SkeletonPattern skeleton,
            PatternTurn patternTurn,
            TemporarySourceSummary sourceSummary,
            TemporarySkeletonOutputSummary skeletonSummary,
            TemporaryRelationshipMetrics relationship)
        {
            int selectedHitCount = Math.Max(1, skeletonSummary.selectedHitCount);
            TemporaryMetricTierDistribution tierDistribution = BuildMetricTierDistribution(skeletonSummary);
            float sourceEndingRatio = sourceSummary.sourceHitCount > 0
                ? sourceSummary.sourceEndingHitCount / (float)sourceSummary.sourceHitCount
                : 0f;
            float selectedEndingRatio = skeletonSummary.selectedEndingHitCount / (float)selectedHitCount;

            return new TemporaryDistributionMetrics
            {
                weakStepRatio = tierDistribution.weakRatio,
                strongestStepRatio = tierDistribution.strongestRatio,
                metricTierDistribution = tierDistribution,
                segmentDifferenceFromSource = CalculateSegmentDifference(
                    sourceSummary.sourceSegmentCounts,
                    sourceSummary.sourceHitCount,
                    skeletonSummary.selectedSegmentCounts,
                    skeletonSummary.selectedHitCount),
                endingBias = selectedEndingRatio - sourceEndingRatio,
                interstitialBias = CalculateInterstitialBias(
                    skeleton,
                    skeletonSummary.selectedHitIndices,
                    ResolveStepsPerQuarter(patternTurn),
                    selectedHitCount)
            };
        }

        private static TemporaryMetricTierDistribution BuildMetricTierDistribution(
            TemporarySkeletonOutputSummary skeletonSummary)
        {
            int selectedHitCount = Math.Max(1, skeletonSummary.selectedHitCount);

            return new TemporaryMetricTierDistribution
            {
                weakCount = skeletonSummary.selectedWeakCount,
                mediumCount = skeletonSummary.selectedMediumCount,
                strongCount = skeletonSummary.selectedStrongCount,
                strongestCount = skeletonSummary.selectedStrongestCount,
                weakRatio = skeletonSummary.selectedWeakCount / (float)selectedHitCount,
                mediumRatio = skeletonSummary.selectedMediumCount / (float)selectedHitCount,
                strongRatio = skeletonSummary.selectedStrongCount / (float)selectedHitCount,
                strongestRatio = skeletonSummary.selectedStrongestCount / (float)selectedHitCount
            };
        }

        private static TemporarySelectedStepDebug[] BuildSelectedStepDebug(
            SkeletonPattern skeleton,
            int stepsPerQuarter)
        {
            int[] selectedHitIndices = Copy(skeleton.SelectedStepIndices);
            var breakdown = new TemporarySelectedStepDebug[selectedHitIndices.Length];

            for (int i = 0; i < selectedHitIndices.Length; i++)
            {
                int stepIndex = selectedHitIndices[i];
                SkeletonStepMeta meta = GetMeta(skeleton, stepIndex) ?? new SkeletonStepMeta { StepIndex = stepIndex };

                breakdown[i] = new TemporarySelectedStepDebug
                {
                    stepIndex = stepIndex,
                    finalScore = meta.FinalScore,
                    metricScore = meta.MetricScore,
                    sourceRelationScore = meta.SourceRelationScore,
                    anchorScore = meta.AnchorScore,
                    endingScore = meta.EndingScore,
                    densityScore = meta.DensityShapingScore,
                    phraseScore = meta.PhraseBalanceScore,
                    metricTier = GetMetricTier(stepIndex, stepsPerQuarter).ToString(),
                    sourceOccupied = meta.SourceOccupied,
                    anchor = meta.SourceAnchor,
                    endingRegion = meta.InEndingRegion,
                    forcedByEndingEnforcement = HasFlag(meta.ReasonFlags, SkeletonReasonFlags.ForcedEnding)
                };
            }

            return breakdown;
        }

        private static TemporaryResponseTypeComparisonReport BuildComparison(
            TemporarySkeletonEvaluationReport[] evaluations)
        {
            var metrics = new TemporaryResponseTypeComparisonMetric[evaluations.Length];
            for (int i = 0; i < evaluations.Length; i++)
            {
                TemporarySkeletonEvaluationReport evaluation = evaluations[i];
                int selectedHitCount = Math.Max(1, evaluation.skeletonSummary.selectedHitCount);

                metrics[i] = new TemporaryResponseTypeComparisonMetric
                {
                    responseType = evaluation.responseType,
                    hitCount = evaluation.skeletonSummary.selectedHitCount,
                    overlapRatio = evaluation.relationshipToSource.overlapRatio,
                    gapFillRatio = evaluation.relationshipToSource.gapFillRatio,
                    weakStepRatio = evaluation.distributionMetrics.weakStepRatio,
                    strongestStepRatio = evaluation.distributionMetrics.strongestStepRatio,
                    endingOccupancy = evaluation.skeletonSummary.selectedEndingHitCount / (float)selectedHitCount,
                    segmentCounts = Copy(evaluation.skeletonSummary.selectedSegmentCounts)
                };
            }

            var divergences = new List<TemporaryPairwiseDivergence>();
            float totalDistance = 0f;

            for (int left = 0; left < evaluations.Length; left++)
            {
                for (int right = left + 1; right < evaluations.Length; right++)
                {
                    float distance = CalculateJaccardDistance(
                        evaluations[left].skeletonSummary.selectedHitIndices,
                        evaluations[right].skeletonSummary.selectedHitIndices);
                    totalDistance += distance;

                    divergences.Add(new TemporaryPairwiseDivergence
                    {
                        leftResponseType = evaluations[left].responseType,
                        rightResponseType = evaluations[right].responseType,
                        jaccardDistance = distance
                    });
                }
            }

            return new TemporaryResponseTypeComparisonReport
            {
                responseTypes = metrics,
                pairwiseDivergence = divergences.ToArray(),
                averagePairwiseDivergence = divergences.Count > 0
                    ? totalDistance / divergences.Count
                    : 0f
            };
        }

        private static TemporarySkeletonAggregateSummary[] BuildAggregates(
            IReadOnlyList<TemporarySkeletonCaseReport> caseReports,
            string builderVersion)
        {
            var aggregates = new TemporarySkeletonAggregateSummary[ResponseTypes.Length];

            for (int responseIndex = 0; responseIndex < ResponseTypes.Length; responseIndex++)
            {
                ResponseType responseType = ResponseTypes[responseIndex];
                string responseTypeName = responseType.ToString();
                int count = 0;
                float overlap = 0f;
                float gapFill = 0f;
                float weak = 0f;
                float strongest = 0f;
                float ending = 0f;
                float anchors = 0f;
                float divergence = 0f;
                int divergenceCount = 0;

                for (int caseIndex = 0; caseIndex < caseReports.Count; caseIndex++)
                {
                    TemporarySkeletonCaseReport caseReport = caseReports[caseIndex];
                    TemporarySkeletonEvaluationReport evaluation = FindEvaluation(caseReport, responseTypeName);
                    if (evaluation == null)
                        continue;

                    int selectedHitCount = Math.Max(1, evaluation.skeletonSummary.selectedHitCount);
                    count++;
                    overlap += evaluation.relationshipToSource.overlapRatio;
                    gapFill += evaluation.relationshipToSource.gapFillRatio;
                    weak += evaluation.distributionMetrics.weakStepRatio;
                    strongest += evaluation.distributionMetrics.strongestStepRatio;
                    ending += evaluation.skeletonSummary.selectedEndingHitCount / (float)selectedHitCount;
                    anchors += evaluation.relationshipToSource.anchorPreservationRatio;

                    TemporaryPairwiseDivergence[] pairs =
                        caseReport.responseTypeComparison != null
                            ? caseReport.responseTypeComparison.pairwiseDivergence
                            : null;
                    if (pairs == null)
                        continue;

                    for (int pairIndex = 0; pairIndex < pairs.Length; pairIndex++)
                    {
                        TemporaryPairwiseDivergence pair = pairs[pairIndex];
                        if (pair.leftResponseType == responseTypeName || pair.rightResponseType == responseTypeName)
                        {
                            divergence += pair.jaccardDistance;
                            divergenceCount++;
                        }
                    }
                }

                aggregates[responseIndex] = new TemporarySkeletonAggregateSummary
                {
                    builderVersion = builderVersion ?? string.Empty,
                    responseType = responseTypeName,
                    sourceCaseCount = count,
                    averageOverlapRatio = Average(overlap, count),
                    averageGapFillRatio = Average(gapFill, count),
                    averageWeakStepRatio = Average(weak, count),
                    averageStrongestStepRatio = Average(strongest, count),
                    averageEndingOccupancy = Average(ending, count),
                    averageAnchorPreservationRatio = Average(anchors, count),
                    averagePairwiseDivergence = Average(divergence, divergenceCount)
                };
            }

            return aggregates;
        }

        private static TemporarySkeletonEvaluationReport FindEvaluation(
            TemporarySkeletonCaseReport caseReport,
            string responseTypeName)
        {
            if (caseReport == null || caseReport.responseEvaluations == null)
                return null;

            for (int i = 0; i < caseReport.responseEvaluations.Length; i++)
            {
                TemporarySkeletonEvaluationReport evaluation = caseReport.responseEvaluations[i];
                if (evaluation != null && evaluation.responseType == responseTypeName)
                    return evaluation;
            }

            return null;
        }

        private static int CountEvaluations(IReadOnlyList<TemporarySkeletonCaseReport> caseReports)
        {
            int count = 0;
            for (int i = 0; i < caseReports.Count; i++)
            {
                if (caseReports[i] != null && caseReports[i].responseEvaluations != null)
                    count += caseReports[i].responseEvaluations.Length;
            }

            return count;
        }

        private static TemporarySkeletonCaseReport[] CopyCases(
            IReadOnlyList<TemporarySkeletonCaseReport> caseReports)
        {
            var copy = new TemporarySkeletonCaseReport[caseReports.Count];
            for (int i = 0; i < caseReports.Count; i++)
                copy[i] = caseReports[i];

            return copy;
        }

        private static string BuildSourceLabel(SyntheticTurnCase syntheticCase)
        {
            return syntheticCase.StructureType + " " +
                   syntheticCase.EnergyProfile + " H" +
                   syntheticCase.NominalHitCount.ToString("00", CultureInfo.InvariantCulture) + " " +
                   syntheticCase.VariantKind;
        }

        private static string BuildSourceCategory(SyntheticTurnCase syntheticCase)
        {
            return syntheticCase.StructureType + "/" + syntheticCase.EnergyProfile;
        }

        private static int[] BuildActiveSourceIndices(PatternTurn patternTurn)
        {
            var indices = new List<int>();
            if (patternTurn.velocity == null)
                return indices.ToArray();

            for (int i = 0; i < patternTurn.velocity.Length; i++)
            {
                if (patternTurn.velocity[i] > 0)
                    indices.Add(i);
            }

            return indices.ToArray();
        }

        private static int[] CountSegments(int[] indices, int totalSteps)
        {
            var counts = new int[SegmentHelper.SegmentCount];
            if (indices == null || totalSteps <= 0)
                return counts;

            for (int i = 0; i < indices.Length; i++)
            {
                int segmentIndex = GetSegmentIndex(indices[i], totalSteps);
                if (segmentIndex >= 0 && segmentIndex < counts.Length)
                    counts[segmentIndex]++;
            }

            return counts;
        }

        private static int GetSegmentIndex(int stepIndex, int totalSteps)
        {
            for (int segmentIndex = 0; segmentIndex < SegmentHelper.SegmentCount; segmentIndex++)
            {
                SegmentHelper.GetSegmentBounds(
                    totalSteps,
                    segmentIndex,
                    out int startInclusive,
                    out int endExclusive);

                if (stepIndex >= startInclusive && stepIndex < endExclusive)
                    return segmentIndex;
            }

            return SegmentHelper.SegmentCount - 1;
        }

        private static int CountEndingHits(int[] indices, int totalSteps, int stepsPerQuarter)
        {
            int count = 0;
            if (indices == null)
                return count;

            for (int i = 0; i < indices.Length; i++)
            {
                if (IsInEndingRegion(indices[i], totalSteps, stepsPerQuarter))
                    count++;
            }

            return count;
        }

        private static bool IsInEndingRegion(int stepIndex, int totalSteps, int stepsPerQuarter)
        {
            if (totalSteps <= 0)
                return false;

            int windowLength = Math.Min(totalSteps, Math.Max(1, stepsPerQuarter));
            return stepIndex >= totalSteps - windowLength && stepIndex < totalSteps;
        }

        private static int ResolveTurnLengthSteps(ResponsePlan plan, PatternTurn patternTurn)
        {
            if (plan != null && plan.TurnLengthSteps > 0)
                return plan.TurnLengthSteps;

            return patternTurn != null && patternTurn.StepCount > 0
                ? patternTurn.StepCount
                : 1;
        }

        private static int ResolveStepsPerQuarter(PatternTurn patternTurn)
        {
            return patternTurn != null && patternTurn.stepsPerQuarter > 0
                ? patternTurn.stepsPerQuarter
                : 12;
        }

        private static int GetTargetCount(float targetDensity, int totalSteps)
        {
            int targetCount = (int)Math.Round(
                Clamp01(targetDensity) * totalSteps,
                MidpointRounding.AwayFromZero);

            if (targetCount < 1)
                return 1;

            if (targetCount > totalSteps)
                return totalSteps;

            return targetCount;
        }

        private static TemporaryMetricTier GetMetricTier(int stepIndex, int stepsPerQuarter)
        {
            if (stepsPerQuarter <= 0)
                stepsPerQuarter = 12;

            if (stepIndex % stepsPerQuarter == 0)
                return TemporaryMetricTier.Strongest;

            if (IsSubdivision(stepIndex, stepsPerQuarter, 2))
                return TemporaryMetricTier.Strong;

            if (IsSubdivision(stepIndex, stepsPerQuarter, 4))
                return TemporaryMetricTier.Medium;

            return TemporaryMetricTier.Weak;
        }

        private static bool IsSubdivision(int stepIndex, int stepsPerQuarter, int divisor)
        {
            if (stepsPerQuarter % divisor != 0)
                return false;

            int subdivision = stepsPerQuarter / divisor;
            return subdivision > 0 && stepIndex % subdivision == 0;
        }

        private static float CalculateSegmentDifference(
            int[] sourceSegmentCounts,
            int sourceHitCount,
            int[] selectedSegmentCounts,
            int selectedHitCount)
        {
            float difference = 0f;
            int count = Math.Max(
                sourceSegmentCounts != null ? sourceSegmentCounts.Length : 0,
                selectedSegmentCounts != null ? selectedSegmentCounts.Length : 0);

            for (int i = 0; i < count; i++)
            {
                float sourceRatio = sourceHitCount > 0
                    ? GetValue(sourceSegmentCounts, i) / (float)sourceHitCount
                    : 0f;
                float selectedRatio = selectedHitCount > 0
                    ? GetValue(selectedSegmentCounts, i) / (float)selectedHitCount
                    : 0f;

                difference += Math.Abs(selectedRatio - sourceRatio);
            }

            return difference;
        }

        private static float CalculateInterstitialBias(
            SkeletonPattern skeleton,
            int[] selectedHitIndices,
            int stepsPerQuarter,
            int selectedHitCount)
        {
            if (selectedHitIndices == null || selectedHitIndices.Length == 0)
                return 0f;

            int interstitialCount = 0;
            for (int i = 0; i < selectedHitIndices.Length; i++)
            {
                int stepIndex = selectedHitIndices[i];
                SkeletonStepMeta meta = GetMeta(skeleton, stepIndex);
                TemporaryMetricTier tier = GetMetricTier(stepIndex, stepsPerQuarter);

                if (meta != null &&
                    !meta.SourceOccupied &&
                    (tier == TemporaryMetricTier.Weak || tier == TemporaryMetricTier.Medium))
                {
                    interstitialCount++;
                }
            }

            return interstitialCount / (float)Math.Max(1, selectedHitCount);
        }

        private static float CalculateJaccardDistance(int[] left, int[] right)
        {
            var union = new HashSet<int>();
            var intersection = new HashSet<int>();

            if (left != null)
            {
                for (int i = 0; i < left.Length; i++)
                    union.Add(left[i]);
            }

            if (right != null)
            {
                for (int i = 0; i < right.Length; i++)
                {
                    if (union.Contains(right[i]))
                        intersection.Add(right[i]);

                    union.Add(right[i]);
                }
            }

            if (union.Count == 0)
                return 0f;

            return 1f - intersection.Count / (float)union.Count;
        }

        private static SkeletonStepMeta GetMeta(SkeletonPattern skeleton, int stepIndex)
        {
            if (skeleton == null ||
                skeleton.StepMeta == null ||
                stepIndex < 0 ||
                stepIndex >= skeleton.StepMeta.Length)
            {
                return null;
            }

            return skeleton.StepMeta[stepIndex];
        }

        private static bool HasFlag(SkeletonReasonFlags actual, SkeletonReasonFlags expected)
        {
            return (actual & expected) == expected;
        }

        private static int GetValue(int[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length
                ? values[index]
                : 0;
        }

        private static int[] Copy(IReadOnlyList<int> values)
        {
            if (values == null || values.Count == 0)
                return new int[0];

            var copy = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];

            return copy;
        }

        private static int[] Copy(int[] values)
        {
            if (values == null || values.Length == 0)
                return new int[0];

            var copy = new int[values.Length];
            Array.Copy(values, copy, values.Length);
            return copy;
        }

        private static float Average(float sum, int count)
        {
            return count > 0 ? sum / count : 0f;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }
    }

    internal sealed class TemporaryTurnAnalysisSkeletonCsvRow
    {
        public TemporaryTurnAnalysisSkeletonCsvRow(IReadOnlyList<string> values)
        {
            Values = values ?? Array.AsReadOnly(new string[0]);
        }

        public IReadOnlyList<string> Values { get; private set; }

        public static TemporaryTurnAnalysisSkeletonCsvRow From(
            TemporarySkeletonCaseReport caseReport,
            TemporarySkeletonEvaluationReport evaluation)
        {
            TemporarySourceSummary source = caseReport.sourceSummary;
            TemporaryPlanSummary plan = evaluation.planSummary;
            TemporarySkeletonOutputSummary skeleton = evaluation.skeletonSummary;
            TemporaryRelationshipMetrics relationship = evaluation.relationshipToSource;
            TemporaryDistributionMetrics distribution = evaluation.distributionMetrics;

            var values = new[]
            {
                evaluation.sourceId,
                evaluation.sourceLabel,
                evaluation.sourceCategory,
                evaluation.responseType,
                evaluation.builderVersion,
                evaluation.plannerSelected.ToString(),
                source.totalSteps.ToString(CultureInfo.InvariantCulture),
                source.sourceHitCount.ToString(CultureInfo.InvariantCulture),
                Format(source.sourceDensity),
                Join(source.sourceHitIndices),
                Join(source.sourceSegmentCounts),
                source.sourceEndingHitCount.ToString(CultureInfo.InvariantCulture),
                Join(source.sourceAnchorIndices),
                source.sourceAnchorCount.ToString(CultureInfo.InvariantCulture),
                Format(plan.targetDensity),
                plan.targetCount.ToString(CultureInfo.InvariantCulture),
                plan.preserveAnchors.ToString(),
                plan.mirrorEnding.ToString(),
                Format(plan.complementarityBias),
                Join(skeleton.selectedHitIndices),
                skeleton.selectedHitCount.ToString(CultureInfo.InvariantCulture),
                Format(skeleton.selectedDensity),
                skeleton.selectedWeakCount.ToString(CultureInfo.InvariantCulture),
                skeleton.selectedMediumCount.ToString(CultureInfo.InvariantCulture),
                skeleton.selectedStrongCount.ToString(CultureInfo.InvariantCulture),
                skeleton.selectedStrongestCount.ToString(CultureInfo.InvariantCulture),
                Join(skeleton.selectedSegmentCounts),
                skeleton.selectedEndingHitCount.ToString(CultureInfo.InvariantCulture),
                skeleton.selectedAnchorHitCount.ToString(CultureInfo.InvariantCulture),
                relationship.overlapCount.ToString(CultureInfo.InvariantCulture),
                relationship.gapFillCount.ToString(CultureInfo.InvariantCulture),
                Format(relationship.overlapRatio),
                Format(relationship.gapFillRatio),
                Format(relationship.anchorPreservationRatio),
                relationship.endingOverlapCount.ToString(CultureInfo.InvariantCulture),
                relationship.endingGapCount.ToString(CultureInfo.InvariantCulture),
                Format(distribution.weakStepRatio),
                Format(distribution.strongestStepRatio),
                FormatMetricDistribution(distribution.metricTierDistribution),
                Format(distribution.segmentDifferenceFromSource),
                Format(distribution.endingBias),
                Format(distribution.interstitialBias),
                FormatSelectedStepDebug(evaluation.selectedStepBreakdown)
            };

            return new TemporaryTurnAnalysisSkeletonCsvRow(Array.AsReadOnly(values));
        }

        private static string FormatSelectedStepDebug(TemporarySelectedStepDebug[] breakdown)
        {
            if (breakdown == null || breakdown.Length == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (int i = 0; i < breakdown.Length; i++)
            {
                if (i > 0)
                    builder.Append(" | ");

                TemporarySelectedStepDebug step = breakdown[i];
                builder
                    .Append(step.stepIndex.ToString(CultureInfo.InvariantCulture))
                    .Append(":")
                    .Append(step.metricTier)
                    .Append(":score=")
                    .Append(Format(step.finalScore))
                    .Append(":src=")
                    .Append(step.sourceOccupied ? "1" : "0")
                    .Append(":anchor=")
                    .Append(step.anchor ? "1" : "0")
                    .Append(":end=")
                    .Append(step.endingRegion ? "1" : "0")
                    .Append(":forcedEnd=")
                    .Append(step.forcedByEndingEnforcement ? "1" : "0");
            }

            return builder.ToString();
        }

        private static string FormatMetricDistribution(TemporaryMetricTierDistribution distribution)
        {
            if (distribution == null)
                return string.Empty;

            return "weak:" + distribution.weakCount.ToString(CultureInfo.InvariantCulture) +
                   "|medium:" + distribution.mediumCount.ToString(CultureInfo.InvariantCulture) +
                   "|strong:" + distribution.strongCount.ToString(CultureInfo.InvariantCulture) +
                   "|strongest:" + distribution.strongestCount.ToString(CultureInfo.InvariantCulture);
        }

        private static string Join(int[] values)
        {
            if (values == null || values.Length == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    builder.Append('|');

                builder.Append(values[i].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static string Format(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }

    internal static class TemporaryTurnAnalysisSkeletonCsvWriter
    {
        private static readonly IReadOnlyList<string> Header = Array.AsReadOnly(new[]
        {
            "SourceId",
            "SourceLabel",
            "SourceCategory",
            "ResponseType",
            "BuilderVersion",
            "PlannerSelected",
            "TotalSteps",
            "SourceHitCount",
            "SourceDensity",
            "SourceHitIndices",
            "SourceSegmentCounts",
            "SourceEndingHitCount",
            "SourceAnchorIndices",
            "SourceAnchorCount",
            "TargetDensity",
            "TargetCount",
            "PreserveAnchors",
            "MirrorEnding",
            "ComplementarityBias",
            "SelectedHitIndices",
            "SelectedHitCount",
            "SelectedDensity",
            "SelectedWeakCount",
            "SelectedMediumCount",
            "SelectedStrongCount",
            "SelectedStrongestCount",
            "SelectedSegmentCounts",
            "SelectedEndingHitCount",
            "SelectedAnchorHitCount",
            "OverlapCount",
            "GapFillCount",
            "OverlapRatio",
            "GapFillRatio",
            "AnchorPreservationRatio",
            "EndingOverlapCount",
            "EndingGapCount",
            "WeakStepRatio",
            "StrongestStepRatio",
            "MetricTierDistribution",
            "SegmentDifferenceFromSource",
            "EndingBias",
            "InterstitialBias",
            "SelectedStepDebug"
        });

        public static void Write(
            string csvPath,
            IReadOnlyList<TemporaryTurnAnalysisSkeletonCsvRow> rows)
        {
            if (string.IsNullOrEmpty(csvPath))
                throw new ArgumentException("CSV path is required.", nameof(csvPath));
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var builder = new StringBuilder();
            AppendLine(builder, Header);

            for (int i = 0; i < rows.Count; i++)
                AppendLine(builder, rows[i].Values);

            File.WriteAllText(csvPath, builder.ToString(), Encoding.UTF8);
        }

        private static void AppendLine(StringBuilder builder, IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');

                builder.Append(Escape(values[i]));
            }

            builder.AppendLine();
        }

        private static string Escape(string value)
        {
            if (value == null)
                return string.Empty;

            bool mustQuote =
                value.IndexOf(',') >= 0 ||
                value.IndexOf('"') >= 0 ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0;

            if (!mustQuote)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }

    internal static class TemporaryTurnAnalysisSkeletonJsonWriter
    {
        public static void Write(string jsonPath, TemporarySkeletonBatchReport report)
        {
            if (string.IsNullOrEmpty(jsonPath))
                throw new ArgumentException("JSON path is required.", nameof(jsonPath));
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true), Encoding.UTF8);
        }
    }

    internal static class TemporaryTurnAnalysisSkeletonMarkdownWriter
    {
        public static void Write(string markdownPath, TemporarySkeletonBatchReport report)
        {
            if (string.IsNullOrEmpty(markdownPath))
                throw new ArgumentException("Markdown path is required.", nameof(markdownPath));
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            var builder = new StringBuilder();
            builder.AppendLine("# Turn Analysis -> Response Planner -> Skeleton Builder Batch");
            builder.AppendLine();
            builder.AppendLine("- Generation mode: " + report.generationMode);
            builder.AppendLine("- Builder version: " + report.builderVersion);
            builder.AppendLine("- Source cases: " + report.sourceCaseCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("- Response evaluations: " + report.responseEvaluationCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();
            builder.AppendLine("Overlap and gap-fill ratios are measured as shares of selected skeleton hits. Ending occupancy is selected ending hits divided by selected hits. Segment difference is the L1 distance between source and selected segment hit distributions.");
            builder.AppendLine();
            AppendAggregates(builder, report.aggregates);
            builder.AppendLine();
            AppendPerSourceComparison(builder, report.cases);

            File.WriteAllText(markdownPath, builder.ToString(), Encoding.UTF8);
        }

        private static void AppendAggregates(
            StringBuilder builder,
            TemporarySkeletonAggregateSummary[] aggregates)
        {
            builder.AppendLine("## Aggregate by Response Type");
            builder.AppendLine();
            builder.AppendLine("| Response | Cases | Overlap | Gap fill | Weak | Strongest | Ending | Anchor preservation | Avg divergence |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");

            if (aggregates == null)
                return;

            for (int i = 0; i < aggregates.Length; i++)
            {
                TemporarySkeletonAggregateSummary aggregate = aggregates[i];
                builder.Append("| ")
                    .Append(aggregate.responseType)
                    .Append(" | ")
                    .Append(aggregate.sourceCaseCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" | ")
                    .Append(Format(aggregate.averageOverlapRatio))
                    .Append(" | ")
                    .Append(Format(aggregate.averageGapFillRatio))
                    .Append(" | ")
                    .Append(Format(aggregate.averageWeakStepRatio))
                    .Append(" | ")
                    .Append(Format(aggregate.averageStrongestStepRatio))
                    .Append(" | ")
                    .Append(Format(aggregate.averageEndingOccupancy))
                    .Append(" | ")
                    .Append(Format(aggregate.averageAnchorPreservationRatio))
                    .Append(" | ")
                    .Append(Format(aggregate.averagePairwiseDivergence))
                    .AppendLine(" |");
            }
        }

        private static void AppendPerSourceComparison(
            StringBuilder builder,
            TemporarySkeletonCaseReport[] cases)
        {
            builder.AppendLine("## Per-Source Response-Type Comparison");
            builder.AppendLine();
            builder.AppendLine("| Source | Planner | Avg divergence | Closest pair | Hit counts | Overlap | Gap fill | Weak | Strongest | Ending | Segments |");
            builder.AppendLine("|---|---|---:|---|---|---|---|---|---|---|---|");

            if (cases == null)
                return;

            for (int i = 0; i < cases.Length; i++)
            {
                TemporarySkeletonCaseReport caseReport = cases[i];
                TemporaryPairwiseDivergence closestPair = FindClosestPair(caseReport.responseTypeComparison);

                builder.Append("| ")
                    .Append(EscapeTable(caseReport.sourceId))
                    .Append(" | ")
                    .Append(caseReport.plannerSelectedResponseType)
                    .Append(" | ")
                    .Append(Format(caseReport.responseTypeComparison != null
                        ? caseReport.responseTypeComparison.averagePairwiseDivergence
                        : 0f))
                    .Append(" | ")
                    .Append(FormatPair(closestPair))
                    .Append(" | ")
                    .Append(FormatComparisonMetric(caseReport.responseTypeComparison, MetricKind.HitCount))
                    .Append(" | ")
                    .Append(FormatComparisonMetric(caseReport.responseTypeComparison, MetricKind.Overlap))
                    .Append(" | ")
                    .Append(FormatComparisonMetric(caseReport.responseTypeComparison, MetricKind.GapFill))
                    .Append(" | ")
                    .Append(FormatComparisonMetric(caseReport.responseTypeComparison, MetricKind.Weak))
                    .Append(" | ")
                    .Append(FormatComparisonMetric(caseReport.responseTypeComparison, MetricKind.Strongest))
                    .Append(" | ")
                    .Append(FormatComparisonMetric(caseReport.responseTypeComparison, MetricKind.Ending))
                    .Append(" | ")
                    .Append(FormatComparisonMetric(caseReport.responseTypeComparison, MetricKind.Segments))
                    .AppendLine(" |");
            }
        }

        private enum MetricKind
        {
            HitCount,
            Overlap,
            GapFill,
            Weak,
            Strongest,
            Ending,
            Segments
        }

        private static string FormatComparisonMetric(
            TemporaryResponseTypeComparisonReport comparison,
            MetricKind metricKind)
        {
            if (comparison == null || comparison.responseTypes == null)
                return string.Empty;

            var builder = new StringBuilder();
            for (int i = 0; i < comparison.responseTypes.Length; i++)
            {
                if (i > 0)
                    builder.Append("<br>");

                TemporaryResponseTypeComparisonMetric metric = comparison.responseTypes[i];
                builder
                    .Append(ShortResponseType(metric.responseType))
                    .Append(":");

                switch (metricKind)
                {
                    case MetricKind.HitCount:
                        builder.Append(metric.hitCount.ToString(CultureInfo.InvariantCulture));
                        break;
                    case MetricKind.Overlap:
                        builder.Append(Format(metric.overlapRatio));
                        break;
                    case MetricKind.GapFill:
                        builder.Append(Format(metric.gapFillRatio));
                        break;
                    case MetricKind.Weak:
                        builder.Append(Format(metric.weakStepRatio));
                        break;
                    case MetricKind.Strongest:
                        builder.Append(Format(metric.strongestStepRatio));
                        break;
                    case MetricKind.Ending:
                        builder.Append(Format(metric.endingOccupancy));
                        break;
                    case MetricKind.Segments:
                        builder.Append(Join(metric.segmentCounts));
                        break;
                }
            }

            return builder.ToString();
        }

        private static TemporaryPairwiseDivergence FindClosestPair(
            TemporaryResponseTypeComparisonReport comparison)
        {
            if (comparison == null ||
                comparison.pairwiseDivergence == null ||
                comparison.pairwiseDivergence.Length == 0)
            {
                return null;
            }

            TemporaryPairwiseDivergence closest = comparison.pairwiseDivergence[0];
            for (int i = 1; i < comparison.pairwiseDivergence.Length; i++)
            {
                if (comparison.pairwiseDivergence[i].jaccardDistance < closest.jaccardDistance)
                    closest = comparison.pairwiseDivergence[i];
            }

            return closest;
        }

        private static string FormatPair(TemporaryPairwiseDivergence pair)
        {
            if (pair == null)
                return string.Empty;

            return ShortResponseType(pair.leftResponseType) + "/" +
                   ShortResponseType(pair.rightResponseType) + " " +
                   Format(pair.jaccardDistance);
        }

        private static string ShortResponseType(string responseType)
        {
            if (string.IsNullOrEmpty(responseType))
                return string.Empty;

            return responseType.Length <= 3
                ? responseType
                : responseType.Substring(0, 3);
        }

        private static string Join(int[] values)
        {
            if (values == null || values.Length == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    builder.Append('/');

                builder.Append(values[i].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static string EscapeTable(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("|", "\\|");
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
