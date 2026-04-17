using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal sealed class SkeletonDerivedMaps
    {
        public bool[] SourceOccupied { get; private set; }
        public bool[] SourceGap { get; private set; }
        public bool[] SourceAnchors { get; private set; }
        public bool[] ExplicitAnchors { get; private set; }
        public bool[] FallbackAnchors { get; private set; }
        public bool[] EndingRegion { get; private set; }
        public int[] StepsFromEnd { get; private set; }
        public float[] MetricalSalience { get; private set; }
        public bool[] StrongBeats { get; private set; }
        public MetricStrengthLevel[] MetricStrengthLevels { get; private set; }
        public int[] StepToSegment { get; private set; }
        public bool[] AdjacentToSource { get; private set; }
        public bool[] NearSource { get; private set; }
        public bool[] InterstitialSourceGap { get; private set; }
        public int[] DistanceToNearestSource { get; private set; }
        public int[] PreviousSourceDistance { get; private set; }
        public int[] NextSourceDistance { get; private set; }
        public int[] LocalSourceDensity { get; private set; }
        public int[] SegmentSourceCounts { get; private set; }
        public float[] SegmentSourceWeights { get; private set; }

        private SkeletonDerivedMaps(
            bool[] sourceOccupied,
            bool[] sourceGap,
            bool[] sourceAnchors,
            bool[] explicitAnchors,
            bool[] fallbackAnchors,
            bool[] endingRegion,
            int[] stepsFromEnd,
            float[] metricalSalience,
            bool[] strongBeats,
            MetricStrengthLevel[] metricStrengthLevels,
            int[] stepToSegment,
            bool[] adjacentToSource,
            bool[] nearSource,
            bool[] interstitialSourceGap,
            int[] distanceToNearestSource,
            int[] previousSourceDistance,
            int[] nextSourceDistance,
            int[] localSourceDensity,
            int[] segmentSourceCounts,
            float[] segmentSourceWeights)
        {
            SourceOccupied = sourceOccupied;
            SourceGap = sourceGap;
            SourceAnchors = sourceAnchors;
            ExplicitAnchors = explicitAnchors;
            FallbackAnchors = fallbackAnchors;
            EndingRegion = endingRegion;
            StepsFromEnd = stepsFromEnd;
            MetricalSalience = metricalSalience;
            StrongBeats = strongBeats;
            MetricStrengthLevels = metricStrengthLevels;
            StepToSegment = stepToSegment;
            AdjacentToSource = adjacentToSource;
            NearSource = nearSource;
            InterstitialSourceGap = interstitialSourceGap;
            DistanceToNearestSource = distanceToNearestSource;
            PreviousSourceDistance = previousSourceDistance;
            NextSourceDistance = nextSourceDistance;
            LocalSourceDensity = localSourceDensity;
            SegmentSourceCounts = segmentSourceCounts;
            SegmentSourceWeights = segmentSourceWeights;
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
            bool[] sourceOccupied = SkeletonSourceMapBuilder.BuildOccupiedMap(sourceTurn, turnLengthSteps);
            int[] stepToSegment = SkeletonSegmentMapBuilder.BuildStepToSegmentMap(turnLengthSteps);
            SkeletonSourceNeighbourhoodMap sourceNeighbourhood = SkeletonSourceMapBuilder.BuildNeighbourhoodMap(
                sourceOccupied,
                stepToSegment);

            return new SkeletonDerivedMaps(
                sourceOccupied,
                sourceNeighbourhood.SourceGap,
                anchorMap.CombinedAnchors,
                anchorMap.ExplicitAnchors,
                anchorMap.FallbackAnchors,
                endingMap.EndingRegion,
                endingMap.StepsFromEnd,
                metricMap.Salience,
                metricMap.StrongBeats,
                metricMap.StrengthLevels,
                stepToSegment,
                sourceNeighbourhood.AdjacentToSource,
                sourceNeighbourhood.NearSource,
                sourceNeighbourhood.InterstitialSourceGap,
                sourceNeighbourhood.DistanceToNearestSource,
                sourceNeighbourhood.PreviousSourceDistance,
                sourceNeighbourhood.NextSourceDistance,
                sourceNeighbourhood.LocalSourceDensity,
                sourceNeighbourhood.SegmentSourceCounts,
                sourceNeighbourhood.SegmentSourceWeights);
        }
    }
}
