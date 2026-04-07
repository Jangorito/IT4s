using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class DensityAnalyser : IAnalyser<DensityFeatures>
    {
        public DensityFeatures Analyze(PatternTurn pattern)
        {
            return new DensityFeatures();
        }
    }
}
