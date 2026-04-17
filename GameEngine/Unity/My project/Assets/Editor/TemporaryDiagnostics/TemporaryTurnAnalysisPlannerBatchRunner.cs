using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using IT4s.Data;
using IT4s.Rhythm.Generation.Skeleton;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Diagnostics.Temporary
{
    /// <summary>
    /// Temporary editor-only diagnostic harness for exercising PatternTurn -> analysis -> planner -> skeleton.
    /// It deliberately bypasses scene orchestration, live capture, OSC, playback, and Play Mode.
    /// </summary>
    internal sealed class TemporaryTurnAnalysisPlannerBatchRunner
    {
        private const string DefaultSkeletonBuilderVersion = "SkeletonBuilder.DefaultConfig.v1";
        private const int EvaluatedResponseTypeCount = 6;

        public TemporaryTurnAnalysisPlannerBatchResult Run(
            SyntheticDatasetGenerationMode generationMode,
            string outputDirectory)
        {
            return Run(generationMode, outputDirectory, DefaultSkeletonBuilderVersion);
        }

        public TemporaryTurnAnalysisPlannerBatchResult Run(
            SyntheticDatasetGenerationMode generationMode,
            string outputDirectory,
            string skeletonBuilderVersion)
        {
            if (string.IsNullOrEmpty(outputDirectory))
                throw new ArgumentException("Output directory is required.", nameof(outputDirectory));

            Directory.CreateDirectory(outputDirectory);

            string csvPath = Path.Combine(outputDirectory, GetFileName(generationMode));
            string skeletonCsvPath = Path.Combine(outputDirectory, GetSkeletonCsvFileName(generationMode, skeletonBuilderVersion));
            string skeletonJsonPath = Path.Combine(outputDirectory, GetSkeletonJsonFileName(generationMode, skeletonBuilderVersion));
            string skeletonMarkdownPath = Path.Combine(outputDirectory, GetSkeletonMarkdownFileName(generationMode, skeletonBuilderVersion));
            IReadOnlyList<SyntheticTurnCase> syntheticCases = SyntheticPatternTurnDataset.Generate(generationMode);

            TurnAnalyser analyser = TemporaryPipelineFactory.CreateTurnAnalyser();
            ResponsePlanner planner = TemporaryPipelineFactory.CreateResponsePlanner();
            SkeletonBuilder skeletonBuilder = TemporaryPipelineFactory.CreateSkeletonBuilder();
            SkeletonBuilderConfig skeletonBuilderConfig = TemporaryPipelineFactory.CreateSkeletonBuilderConfig();

            var rows = new List<TemporaryTurnAnalysisPlannerCsvRow>(syntheticCases.Count);
            var skeletonRows = new List<TemporaryTurnAnalysisSkeletonCsvRow>(
                syntheticCases.Count * EvaluatedResponseTypeCount);
            var skeletonCaseReports = new List<TemporarySkeletonCaseReport>(syntheticCases.Count);
            int baseCaseCount = 0;
            int variantCaseCount = 0;

            for (int i = 0; i < syntheticCases.Count; i++)
            {
                SyntheticTurnCase syntheticCase = syntheticCases[i];
                PatternTurn patternTurn = SyntheticPatternTurnFactory.CreatePatternTurn(syntheticCase);
                TurnAnalysisResult analysis = analyser.Analyze(patternTurn);
                ResponsePlan plan = planner.Plan(analysis);

                // Capture immediately so later planner calls cannot overwrite this case's debug state.
                ResponsePlannerDebugSnapshot snapshot = planner.LastSnapshot;

                rows.Add(TemporaryTurnAnalysisPlannerCsvRow.From(
                    generationMode,
                    syntheticCase,
                    patternTurn,
                    analysis,
                    plan,
                    snapshot));

                TemporarySkeletonCaseReport skeletonCaseReport =
                    TemporaryTurnAnalysisSkeletonReportBuilder.BuildCaseReport(
                        syntheticCase,
                        patternTurn,
                        analysis,
                        plan,
                        snapshot,
                        planner,
                        skeletonBuilder,
                        skeletonBuilderConfig,
                        skeletonBuilderVersion);

                skeletonCaseReports.Add(skeletonCaseReport);

                if (skeletonCaseReport.responseEvaluations != null)
                {
                    for (int responseIndex = 0;
                         responseIndex < skeletonCaseReport.responseEvaluations.Length;
                         responseIndex++)
                    {
                        skeletonRows.Add(TemporaryTurnAnalysisSkeletonCsvRow.From(
                            skeletonCaseReport,
                            skeletonCaseReport.responseEvaluations[responseIndex]));
                    }
                }

                if (syntheticCase.VariantKind == SyntheticVariantKind.Base)
                    baseCaseCount++;
                else
                    variantCaseCount++;
            }

            TemporaryTurnAnalysisPlannerCsvWriter.Write(csvPath, rows);
            TemporaryTurnAnalysisSkeletonCsvWriter.Write(skeletonCsvPath, skeletonRows);

            TemporarySkeletonBatchReport skeletonBatchReport =
                TemporaryTurnAnalysisSkeletonReportBuilder.BuildBatchReport(
                    generationMode,
                    skeletonBuilderVersion,
                    skeletonCaseReports);
            TemporaryTurnAnalysisSkeletonJsonWriter.Write(skeletonJsonPath, skeletonBatchReport);
            TemporaryTurnAnalysisSkeletonMarkdownWriter.Write(skeletonMarkdownPath, skeletonBatchReport);

            return new TemporaryTurnAnalysisPlannerBatchResult(
                csvPath,
                skeletonCsvPath,
                skeletonJsonPath,
                skeletonMarkdownPath,
                rows.Count,
                skeletonRows.Count,
                baseCaseCount,
                variantCaseCount,
                generationMode);
        }

        private static string GetFileName(SyntheticDatasetGenerationMode generationMode)
        {
            switch (generationMode)
            {
                case SyntheticDatasetGenerationMode.BaseOnly:
                    return "synthetic_turn_analysis_response_planner_base_only.csv";
                case SyntheticDatasetGenerationMode.BasePlusVariants:
                    return "synthetic_turn_analysis_response_planner_base_plus_variants.csv";
                default:
                    throw new ArgumentOutOfRangeException(nameof(generationMode), generationMode, "Unknown generation mode.");
            }
        }

        private static string GetSkeletonCsvFileName(
            SyntheticDatasetGenerationMode generationMode,
            string skeletonBuilderVersion)
        {
            string suffix = GetBuilderVersionFileSuffix(skeletonBuilderVersion);

            switch (generationMode)
            {
                case SyntheticDatasetGenerationMode.BaseOnly:
                    return "synthetic_turn_analysis_response_planner_skeleton_base_only" + suffix + ".csv";
                case SyntheticDatasetGenerationMode.BasePlusVariants:
                    return "synthetic_turn_analysis_response_planner_skeleton_base_plus_variants" + suffix + ".csv";
                default:
                    throw new ArgumentOutOfRangeException(nameof(generationMode), generationMode, "Unknown generation mode.");
            }
        }

        private static string GetSkeletonJsonFileName(
            SyntheticDatasetGenerationMode generationMode,
            string skeletonBuilderVersion)
        {
            string suffix = GetBuilderVersionFileSuffix(skeletonBuilderVersion);

            switch (generationMode)
            {
                case SyntheticDatasetGenerationMode.BaseOnly:
                    return "synthetic_turn_analysis_response_planner_skeleton_base_only" + suffix + ".json";
                case SyntheticDatasetGenerationMode.BasePlusVariants:
                    return "synthetic_turn_analysis_response_planner_skeleton_base_plus_variants" + suffix + ".json";
                default:
                    throw new ArgumentOutOfRangeException(nameof(generationMode), generationMode, "Unknown generation mode.");
            }
        }

        private static string GetSkeletonMarkdownFileName(
            SyntheticDatasetGenerationMode generationMode,
            string skeletonBuilderVersion)
        {
            string suffix = GetBuilderVersionFileSuffix(skeletonBuilderVersion);

            switch (generationMode)
            {
                case SyntheticDatasetGenerationMode.BaseOnly:
                    return "synthetic_turn_analysis_response_planner_skeleton_base_only" + suffix + ".md";
                case SyntheticDatasetGenerationMode.BasePlusVariants:
                    return "synthetic_turn_analysis_response_planner_skeleton_base_plus_variants" + suffix + ".md";
                default:
                    throw new ArgumentOutOfRangeException(nameof(generationMode), generationMode, "Unknown generation mode.");
            }
        }

        private static string GetBuilderVersionFileSuffix(string skeletonBuilderVersion)
        {
            if (string.IsNullOrEmpty(skeletonBuilderVersion))
                return string.Empty;

            return "_" + SanitizeFileNameToken(skeletonBuilderVersion);
        }

        private static string SanitizeFileNameToken(string value)
        {
            var builder = new StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if ((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9'))
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }
    }

    internal sealed class TemporaryTurnAnalysisPlannerBatchResult
    {
        public TemporaryTurnAnalysisPlannerBatchResult(
            string csvPath,
            string skeletonCsvPath,
            string skeletonJsonPath,
            string skeletonMarkdownPath,
            int totalRows,
            int skeletonEvaluationRows,
            int baseCaseCount,
            int variantCaseCount,
            SyntheticDatasetGenerationMode generationMode)
        {
            CsvPath = csvPath;
            SkeletonCsvPath = skeletonCsvPath;
            SkeletonJsonPath = skeletonJsonPath;
            SkeletonMarkdownPath = skeletonMarkdownPath;
            TotalRows = totalRows;
            SkeletonEvaluationRows = skeletonEvaluationRows;
            BaseCaseCount = baseCaseCount;
            VariantCaseCount = variantCaseCount;
            GenerationMode = generationMode;
        }

        public string CsvPath { get; private set; }
        public string SkeletonCsvPath { get; private set; }
        public string SkeletonJsonPath { get; private set; }
        public string SkeletonMarkdownPath { get; private set; }
        public int TotalRows { get; private set; }
        public int SkeletonEvaluationRows { get; private set; }
        public int BaseCaseCount { get; private set; }
        public int VariantCaseCount { get; private set; }
        public SyntheticDatasetGenerationMode GenerationMode { get; private set; }
        public bool VariantModeEnabled => GenerationMode == SyntheticDatasetGenerationMode.BasePlusVariants;
    }

    internal static class TemporaryPipelineFactory
    {
        private const float DefaultHighEnergyMeanVelocity = 90f;
        private const float DefaultLowEnergyMeanVelocity = 10f;
        private const float DefaultFlatVarianceThreshold = 1f;
        private const float DefaultAccentPeakOverMeanThreshold = 45f;
        private const float DefaultCrescendoMinDelta = 50f;
        private const float DefaultDecrescendoMinDelta = 50f;
        private const float DefaultShapeEpsilon = 0.02f;

        public static TurnAnalyser CreateTurnAnalyser()
        {
            return new TurnAnalyser(
                new DensityAnalyser(),
                new EnergyAnalyser(DefaultEnergyThresholds()),
                new AnchorAnalyser(),
                new EndActivityAnalyser(),
                new SegmentActivityProfileAnalyser(DefaultSegmentActivityProfileThresholds()));
        }

        public static ResponsePlanner CreateResponsePlanner()
        {
            return new ResponsePlanner();
        }

        public static SkeletonBuilder CreateSkeletonBuilder()
        {
            return new SkeletonBuilder();
        }

        public static SkeletonBuilderConfig CreateSkeletonBuilderConfig()
        {
            return new SkeletonBuilderConfig();
        }

        private static EnergyThresholds DefaultEnergyThresholds()
        {
            return new EnergyThresholds(
                DefaultHighEnergyMeanVelocity,
                DefaultLowEnergyMeanVelocity,
                DefaultFlatVarianceThreshold,
                DefaultAccentPeakOverMeanThreshold,
                DefaultCrescendoMinDelta,
                DefaultDecrescendoMinDelta);
        }

        private static SegmentActivityProfileThresholds DefaultSegmentActivityProfileThresholds()
        {
            return new SegmentActivityProfileThresholds(DefaultShapeEpsilon);
        }
    }

    internal sealed class TemporaryTurnAnalysisPlannerCsvRow
    {
        public TemporaryTurnAnalysisPlannerCsvRow(IReadOnlyList<string> values)
        {
            Values = values ?? Array.AsReadOnly(new string[0]);
        }

        public IReadOnlyList<string> Values { get; private set; }

        public static TemporaryTurnAnalysisPlannerCsvRow From(
            SyntheticDatasetGenerationMode generationMode,
            SyntheticTurnCase syntheticCase,
            PatternTurn patternTurn,
            TurnAnalysisResult analysis,
            ResponsePlan plan,
            ResponsePlannerDebugSnapshot snapshot)
        {
            if (syntheticCase == null)
                throw new ArgumentNullException(nameof(syntheticCase));
            if (patternTurn == null)
                throw new ArgumentNullException(nameof(patternTurn));
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            DensityFeatures density = analysis.Density ?? new DensityFeatures();
            EnergyFeatures energy = analysis.Energy ?? new EnergyFeatures();
            AnchorFeatures anchor = analysis.Anchor ?? new AnchorFeatures();
            EndActivityFeatures endActivity = analysis.EndActivity ?? new EndActivityFeatures();
            SegmentActivityProfileFeatures profile =
                analysis.SegmentActivityProfile ?? new SegmentActivityProfileFeatures();

            var values = new[]
            {
                syntheticCase.CaseId,
                syntheticCase.StructureType.ToString(),
                syntheticCase.EnergyProfile.ToString(),
                syntheticCase.VariantKind.ToString(),
                syntheticCase.ParentCaseId,
                generationMode.ToString(),
                syntheticCase.VariantSeed.HasValue
                    ? syntheticCase.VariantSeed.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
                syntheticCase.NominalHitCount.ToString(CultureInfo.InvariantCulture),
                CountActiveSteps(patternTurn).ToString(CultureInfo.InvariantCulture),
                patternTurn.StepCount.ToString(CultureInfo.InvariantCulture),
                Format(density.StepDensity),
                Format(energy.MeanVelocity),
                energy.PeakVelocity.ToString(CultureInfo.InvariantCulture),
                anchor.AnchorCount.ToString(CultureInfo.InvariantCulture),
                anchor.HasOpeningAnchor.ToString(),
                anchor.HasClosingAnchor.ToString(),
                Format(endActivity.EndDensity),
                Format(endActivity.EndEnergy),
                endActivity.EndAccent.ToString(CultureInfo.InvariantCulture),
                profile.DensityShape.ToString(),
                profile.EnergyShape.ToString(),
                plan.ResponseType.ToString(),
                Format(plan.TargetDensity),
                Format(plan.ComplementarityBias),
                plan.PreserveAnchors.ToString(),
                plan.MirrorEnding.ToString(),
                snapshot != null ? snapshot.SelectedResponseType.ToString() : string.Empty,
                snapshot != null && snapshot.SourceDescriptorSummary != null
                    ? snapshot.SourceDescriptorSummary.Summary
                    : string.Empty,
                snapshot != null && snapshot.SourceNumericSummary != null
                    ? Format(snapshot.SourceNumericSummary.SourceDensity)
                    : string.Empty,
                snapshot != null && snapshot.SourceNumericSummary != null
                    ? Format(snapshot.SourceNumericSummary.SourceEnergy)
                    : string.Empty,
                Format(GetScore(snapshot, ResponseType.Mirror)),
                Format(GetScore(snapshot, ResponseType.Complement)),
                Format(GetScore(snapshot, ResponseType.Simplify)),
                Format(GetScore(snapshot, ResponseType.Intensify)),
                Format(GetScore(snapshot, ResponseType.Contrast)),
                Format(GetScore(snapshot, ResponseType.Fill))
            };

            return new TemporaryTurnAnalysisPlannerCsvRow(Array.AsReadOnly(values));
        }

        private static int CountActiveSteps(PatternTurn patternTurn)
        {
            int count = 0;
            int stepCount = patternTurn.StepCount;

            for (int i = 0; i < stepCount; i++)
            {
                if (patternTurn.velocity[i] > 0)
                    count++;
            }

            return count;
        }

        private static float GetScore(ResponsePlannerDebugSnapshot snapshot, ResponseType responseType)
        {
            if (snapshot == null || snapshot.PerResponseTypeScores == null)
                return 0f;

            IReadOnlyList<ResponseTypeScore> scores = snapshot.PerResponseTypeScores;
            for (int i = 0; i < scores.Count; i++)
            {
                ResponseTypeScore score = scores[i];
                if (score != null && score.ResponseType == responseType)
                    return score.Score;
            }

            return 0f;
        }

        private static string Format(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }

    internal static class TemporaryTurnAnalysisPlannerCsvWriter
    {
        private static readonly IReadOnlyList<string> Header = Array.AsReadOnly(new[]
        {
            "CaseId",
            "StructureType",
            "EnergyProfile",
            "VariantKind",
            "ParentCaseId",
            "GenerationMode",
            "VariantSeed",
            "NominalHitCount",
            "ActualHitCount",
            "StepCount",
            "Density",
            "MeanEnergy",
            "PeakEnergy",
            "AnchorCount",
            "HasOpeningAnchor",
            "HasClosingAnchor",
            "EndDensity",
            "EndEnergy",
            "EndAccent",
            "DensityShape",
            "EnergyShape",
            "SelectedResponseType",
            "TargetDensity",
            "ComplementarityBias",
            "PreserveAnchors",
            "MirrorEnding",
            "SnapshotSelectedResponseType",
            "PlannerDescriptorSummary",
            "PlannerSourceDensity",
            "PlannerSourceEnergy",
            "Score_Mirror",
            "Score_Complement",
            "Score_Simplify",
            "Score_Intensify",
            "Score_Contrast",
            "Score_Fill"
        });

        public static void Write(string csvPath, IReadOnlyList<TemporaryTurnAnalysisPlannerCsvRow> rows)
        {
            if (string.IsNullOrEmpty(csvPath))
                throw new ArgumentException("CSV path is required.", nameof(csvPath));
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var builder = new StringBuilder();
            AppendLine(builder, Header);

            for (int i = 0; i < rows.Count; i++)
                AppendLine(builder, rows[i].Values);

            File.WriteAllText(csvPath, builder.ToString(), Encoding.UTF8);
        }

        private static void AppendLine(StringBuilder builder, IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');

                builder.Append(Escape(values[i]));
            }

            builder.AppendLine();
        }

        private static string Escape(string value)
        {
            if (value == null)
                return string.Empty;

            bool mustQuote =
                value.IndexOf(',') >= 0 ||
                value.IndexOf('"') >= 0 ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0;

            if (!mustQuote)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
