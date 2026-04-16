using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal sealed class SkeletonDerivedMaps
    {
        public bool[] SourceOccupied { get; private set; }
        public bool[] SourceAnchors { get; private set; }
        public bool[] EndingRegion { get; private set; }
        public float[] MetricalSalience { get; private set; }
        public bool[] StrongBeats { get; private set; }
        public int[] StepToSegment { get; private set; }

        private SkeletonDerivedMaps(
            bool[] sourceOccupied,
            bool[] sourceAnchors,
            bool[] endingRegion,
            float[] metricalSalience,
            bool[] strongBeats,
            int[] stepToSegment)
        {
            SourceOccupied = sourceOccupied;
            SourceAnchors = sourceAnchors;
            EndingRegion = endingRegion;
            MetricalSalience = metricalSalience;
            StrongBeats = strongBeats;
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

            return new SkeletonDerivedMaps(
                SkeletonSourceMapBuilder.BuildOccupiedMap(sourceTurn, turnLengthSteps),
                SkeletonSourceMapBuilder.BuildAnchorMap(sourceAnalysis, turnLengthSteps),
                SkeletonEndingRegionHelper.BuildEndingRegionMap(turnLengthSteps, stepsPerQuarter),
                metricMap.Salience,
                metricMap.StrongBeats,
                SkeletonSegmentMapBuilder.BuildStepToSegmentMap(turnLengthSteps));
        }
    }
}
