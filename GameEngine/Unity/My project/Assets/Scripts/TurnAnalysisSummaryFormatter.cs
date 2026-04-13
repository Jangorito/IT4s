using System.Collections.Generic;
using System.Globalization;
using System.Text;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Debugging
{
    /// <summary>
    /// Converts TurnAnalysisResult data into a compact, scan-friendly debug summary.
    /// The formatter only references fields that exist in the current analysis models.
    /// </summary>
    public static class TurnAnalysisSummaryFormatter
    {
        public static string Format(TurnAnalysisResult analysis)
        {
            if (analysis == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(768);

            AppendDensity(builder, analysis.Density);
            builder.AppendLine();
            AppendEnergy(builder, analysis.Energy);
            builder.AppendLine();
            AppendAnchors(builder, analysis.Anchor);
            builder.AppendLine();
            AppendEndActivity(builder, analysis.EndActivity);
            builder.AppendLine();
            AppendSegmentActivityProfile(builder, analysis);

            return builder.ToString().TrimEnd();
        }

        private static void AppendDensity(StringBuilder builder, DensityFeatures density)
        {
            builder.AppendLine("<b>Density</b>");
            builder.AppendLine($"StepDensity: {FormatFloat(density?.StepDensity ?? 0f)}");
            builder.AppendLine(
                $"ActiveStepCount: {density?.ActiveStepCount ?? 0} / {density?.StepCount ?? 0}");
            builder.AppendLine($"InactiveStepCount: {density?.InactiveStepCount ?? 0}");
            builder.AppendLine($"SegmentDensities: {FormatFloatList(density?.SegmentDensities)}");
        }

        private static void AppendEnergy(StringBuilder builder, EnergyFeatures energy)
        {
            builder.AppendLine("<b>Energy</b>");
            builder.AppendLine($"MeanVelocity: {FormatFloat(energy?.MeanVelocity ?? 0f)}");
            builder.AppendLine($"PeakVelocity: {energy?.PeakVelocity ?? 0}");
            builder.AppendLine($"VelocityVariance: {FormatFloat(energy?.VelocityVariance ?? 0f)}");
            builder.AppendLine($"SegmentMeanVelocities: {FormatFloatList(energy?.SegmentMeanVelocities)}");
            builder.AppendLine($"Flags: {FormatEnergyFlags(energy)}");
        }

        private static void AppendAnchors(StringBuilder builder, AnchorFeatures anchor)
        {
            builder.AppendLine("<b>Anchors</b>");
            builder.AppendLine($"AnchorCount: {anchor?.AnchorCount ?? 0}");
            builder.AppendLine($"AnchorIndices: {FormatIntList(anchor?.AnchorIndices)}");
            builder.AppendLine($"StrongestAnchorIndex: {FormatNullableInt(anchor?.StrongestAnchorIndex)}");
            builder.AppendLine($"StrongestAnchorScore: {FormatFloat(anchor?.StrongestAnchorScore ?? 0f)}");
            builder.AppendLine($"AnchorCountsPerSegment: {FormatIntList(anchor?.AnchorCountsPerSegment)}");
            builder.AppendLine(
                $"Opening/Closing: {FormatBool(anchor?.HasOpeningAnchor ?? false)} / {FormatBool(anchor?.HasClosingAnchor ?? false)}");
        }

        private static void AppendEndActivity(StringBuilder builder, EndActivityFeatures endActivity)
        {
            builder.AppendLine("<b>End Activity</b>");
            builder.AppendLine($"EndDensity: {FormatFloat(endActivity?.EndDensity ?? 0f)}");
            builder.AppendLine($"EndEnergy: {FormatFloat(endActivity?.EndEnergy ?? 0f)}");
            builder.AppendLine($"EndAccent: {endActivity?.EndAccent ?? 0}");
        }

        private static void AppendSegmentActivityProfile(StringBuilder builder, TurnAnalysisResult analysis)
        {
            builder.AppendLine("<b>SAP</b>");
            builder.AppendLine($"DensityShape: {FormatShape(analysis, useDensityShape: true)}");
            builder.AppendLine($"EnergyShape: {FormatShape(analysis, useDensityShape: false)}");
            builder.AppendLine($"SourceDensitySegments: {FormatFloatList(analysis?.Density?.SegmentDensities)}");
            builder.AppendLine($"SourceEnergySegments: {FormatFloatList(analysis?.Energy?.SegmentMeanVelocities)}");
        }

        private static string FormatEnergyFlags(EnergyFeatures energy)
        {
            if (energy == null)
            {
                return "None";
            }

            var flags = new List<string>(6);

            if (energy.IsHighEnergy)
            {
                flags.Add(nameof(energy.IsHighEnergy));
            }

            if (energy.IsLowEnergy)
            {
                flags.Add(nameof(energy.IsLowEnergy));
            }

            if (energy.IsFlatEnergy)
            {
                flags.Add(nameof(energy.IsFlatEnergy));
            }

            if (energy.IsAccented)
            {
                flags.Add(nameof(energy.IsAccented));
            }

            if (energy.IsCrescendo)
            {
                flags.Add(nameof(energy.IsCrescendo));
            }

            if (energy.IsDecrescendo)
            {
                flags.Add(nameof(energy.IsDecrescendo));
            }

            return flags.Count == 0 ? "None" : string.Join(", ", flags);
        }

        private static string FormatFloatList(IReadOnlyList<float> values)
        {
            if (values == null || values.Count == 0)
            {
                return "[]";
            }

            var builder = new StringBuilder();
            builder.Append('[');

            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(FormatFloat(values[i]));
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string FormatIntList(IReadOnlyList<int> values)
        {
            if (values == null || values.Count == 0)
            {
                return "[]";
            }

            var builder = new StringBuilder();
            builder.Append('[');

            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(values[i].ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatNullableInt(int? value)
        {
            return value.HasValue
                ? value.Value.ToString(CultureInfo.InvariantCulture)
                : "None";
        }

        private static string FormatShape(TurnAnalysisResult analysis, bool useDensityShape)
        {
            if (analysis == null || analysis.SegmentActivityProfile == null)
            {
                return ActivityShape.Flat.ToString();
            }

            return useDensityShape
                ? analysis.SegmentActivityProfile.DensityShape.ToString()
                : analysis.SegmentActivityProfile.EnergyShape.ToString();
        }

        private static string FormatBool(bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}
