using IT4s.Data;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    public sealed class SkeletonBuildRequest
    {
        public ResponsePlan Plan { get; set; }
        public PatternTurn SourceTurn { get; set; }
        public TurnAnalysisResult SourceAnalysis { get; set; }
        public int TurnLengthSteps { get; set; }
        public int StepsPerQuarter { get; set; }
        public SkeletonBuilderConfig Config { get; set; }
    }
}
