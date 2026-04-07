using System;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.TurnAnalysis
{
    public sealed class TurnAnalyser
    {
        private readonly DensityAnalyser densityAnalyser;
        private readonly EnergyAnalyser energyAnalyser;
        private readonly AnchorAnalyser anchorAnalyser;
        private readonly EndActivityAnalyser endActivityAnalyser;
        private readonly SegmentActivityProfileAnalyser segmentActivityProfileAnalyser;

        public TurnAnalyser(
            DensityAnalyser densityAnalyser,
            EnergyAnalyser energyAnalyser,
            AnchorAnalyser anchorAnalyser,
            EndActivityAnalyser endActivityAnalyser,
            SegmentActivityProfileAnalyser segmentActivityProfileAnalyser)
        {
            this.densityAnalyser = densityAnalyser ?? throw new ArgumentNullException(nameof(densityAnalyser));
            this.energyAnalyser = energyAnalyser ?? throw new ArgumentNullException(nameof(energyAnalyser));
            this.anchorAnalyser = anchorAnalyser ?? throw new ArgumentNullException(nameof(anchorAnalyser));
            this.endActivityAnalyser = endActivityAnalyser ?? throw new ArgumentNullException(nameof(endActivityAnalyser));
            this.segmentActivityProfileAnalyser = segmentActivityProfileAnalyser ?? throw new ArgumentNullException(nameof(segmentActivityProfileAnalyser));
        }

        public TurnAnalysisResult Analyze(PatternTurn pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            DensityFeatures density = densityAnalyser.Analyze(pattern);
            EnergyFeatures energy = energyAnalyser.Analyze(pattern);
            AnchorFeatures anchor = anchorAnalyser.Analyze(pattern);
            EndActivityFeatures endActivity = endActivityAnalyser.Analyze(pattern);
            SegmentActivityProfileFeatures segmentActivityProfile = segmentActivityProfileAnalyser.Analyze(density, energy);

            return new TurnAnalysisResult(
                density,
                energy,
                anchor,
                endActivity,
                segmentActivityProfile);
        }
    }
}
