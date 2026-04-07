using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class EndActivityAnalyser : IAnalyser<EndActivityFeatures>
    {
        public EndActivityFeatures Analyze(PatternTurn pattern)
        {
            return new EndActivityFeatures();
        }
    }
}
