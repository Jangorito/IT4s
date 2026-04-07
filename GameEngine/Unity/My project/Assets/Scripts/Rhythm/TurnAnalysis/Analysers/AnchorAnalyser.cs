using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class AnchorAnalyser : IAnalyser<AnchorFeatures>
    {
        public AnchorFeatures Analyze(PatternTurn pattern)
        {
            return new AnchorFeatures();
        }
    }
}
