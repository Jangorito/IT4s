using IT4s.Data;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public interface IAnalyser<TFeature>
    {
        TFeature Analyze(PatternTurn pattern);
    }
}
