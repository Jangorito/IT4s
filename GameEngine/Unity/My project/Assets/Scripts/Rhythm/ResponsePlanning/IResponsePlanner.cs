using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.ResponsePlanning
{
    public interface IResponsePlanner
    {
        ResponsePlan Plan(TurnAnalysisResult analysis);
    }
}
