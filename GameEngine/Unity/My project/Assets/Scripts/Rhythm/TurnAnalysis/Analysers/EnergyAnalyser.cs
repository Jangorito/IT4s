using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class EnergyAnalyser : IAnalyser<EnergyFeatures>
    {
        public EnergyFeatures Analyze(PatternTurn pattern)
        {
            return new EnergyFeatures();
        }
    }
}
