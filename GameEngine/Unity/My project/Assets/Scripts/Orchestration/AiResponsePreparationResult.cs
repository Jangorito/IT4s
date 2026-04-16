using IT4s.Data;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Orchestration
{
    /// <summary>
    /// Small data carrier for a fully prepared AI response.
    /// The controller still decides what to store, publish, and play; this simply groups the
    /// concrete outputs of analyse -> plan -> transform once the flow has completed.
    /// </summary>
    public sealed class AiResponsePreparationResult
    {
        public AiResponsePreparationResult(
            TurnAnalysisResult analysis,
            ResponsePlan responsePlan,
            PatternTurn generatedPattern,
            SkeletonPattern skeletonPattern = null)
        {
            Analysis = analysis;
            ResponsePlan = responsePlan;
            GeneratedPattern = generatedPattern;
            SkeletonPattern = skeletonPattern;
        }

        public TurnAnalysisResult Analysis { get; }
        public ResponsePlan ResponsePlan { get; }
        public SkeletonPattern SkeletonPattern { get; }
        public PatternTurn GeneratedPattern { get; }
    }
}
