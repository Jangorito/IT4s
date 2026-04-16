using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal sealed class SkeletonDerivedMaps
    {
        public bool[] SourceOccupied { get; private set; }
        public bool[] SourceAnchors { get; private set; }
        public bool[] ExplicitAnchors { get; private set; }
        public bool[] FallbackAnchors { get; private set; }
        public bool[] EndingRegion { get; private set; }
        public int[] StepsFromEnd { get; private set; }
        public float[] MetricalSalience { get; private set; }
        public bool[] StrongBeats { get; private set; }
        public MetricStrengthLevel[] MetricStrengthLevels { get; private set; }
        public int[] StepToSegment { get; private set; }

        private SkeletonDerivedMaps(
            bool[] sourceOccupied,
            bool[] sourceAnchors,
            bool[] explicitAnchors,
            bool[] fallbackAnchors,
            bool[] endingRegion,
            int[] stepsFromEnd,
            float[] metricalSalience,
            bool[] strongBeats,
            MetricStrengthLevel[] metricStrengthLevels,
            int[] stepToSegment)
        {
            SourceOccupied = sourceOccupied;
            SourceAnchors = sourceAnchors;
            ExplicitAnchors = explicitAnchors;
            FallbackAnchors = fallbackAnchors;
            EndingRegion = endingRegion;
            StepsFromEnd = stepsFromEnd;
            MetricalSalience = metricalSalience;
            StrongBeats = strongBeats;
            MetricStrengthLevels = metricStrengthLevels;
            StepToSegment = stepToSegment;
        }

        public static SkeletonDerivedMaps Build(
            PatternTurn sourceTurn,
            TurnAnalysisResult sourceAnalysis,
            int turnLengthSteps,
            int stepsPerQuarter)
        {
            SkeletonMetricMap metricMap = SkeletonMetricMapBuilder.Build(
                turnLengthSteps,
                stepsPerQuarter);
            SkeletonSourceAnchorMap anchorMap = SkeletonSourceMapBuilder.BuildAnchorMap(
                sourceAnalysis,
                turnLengthSteps);
            SkeletonEndingMap endingMap = SkeletonEndingRegionHelper.BuildEndingMap(
                turnLengthSteps,
                stepsPerQuarter);

            return new SkeletonDerivedMaps(
                SkeletonSourceMapBuilder.BuildOccupiedMap(sourceTurn, turnLengthSteps),
                anchorMap.CombinedAnchors,
                anchorMap.ExplicitAnchors,
                anchorMap.FallbackAnchors,
                endingMap.EndingRegion,
                endingMap.StepsFromEnd,
                metricMap.Salience,
                metricMap.StrongBeats,
                metricMap.StrengthLevels,
                SkeletonSegmentMapBuilder.BuildStepToSegmentMap(turnLengthSteps));
        }
    }
}
