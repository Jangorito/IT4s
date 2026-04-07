using System;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class EndActivityAnalyser : IAnalyser<EndActivityFeatures>
    {
        public EndActivityFeatures Analyze(PatternTurn pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            int N = pattern.StepCount;
            int endStart = (int)(N * 0.75f);

            int activeCount = 0;
            float velocitySum = 0f;
            int maxVelocity = 0;

            for (int i = endStart; i < N; i++)
            {
                int v = pattern.velocity[i];

                if (v > 0)
                {
                    activeCount++;
                    velocitySum += v;

                    if (v > maxVelocity)
                        maxVelocity = v;
                }
            }

            int windowSize = N - endStart;

            float endDensity = windowSize > 0
                ? (float)activeCount / windowSize
                : 0f;

            float endEnergy = activeCount > 0
                ? velocitySum / activeCount
                : 0f;

            int endAccent = maxVelocity;

            return new EndActivityFeatures(
                endDensity,
                endEnergy,
                endAccent);
        }
    }
}
