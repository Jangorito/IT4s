using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using IT4s.Data;
using IT4s.Rhythm;
using IT4s.Rhythm.Generation.Skeleton;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.Transformations;
using IT4s.Rhythm.TurnAnalysis;
using IT4s.Rhythm.TurnAnalysis.Models;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace IT4s.Diagnostics.Temporary
{
    /// <summary>
    /// Editor-only timing harness for Chapter 5 timing analysis.
    /// It measures the synchronous post-capture pipeline over the current synthetic dataset.
    /// Live OSC capture and ChucK playback are not driven in batchmode; capture duration is
    /// derived from the musical turn window, and playback trigger time is simulated by preparing
    /// the same payload shape sent to ChucK.
    /// </summary>
    public static class TemporaryTimingAnalysisRunner
    {
        private const string MenuRoot = "Tools/IT4s/Diagnostics/Timing Analysis/";

        [MenuItem(MenuRoot + "Run Base + Variants Timing", false, 1100)]
        public static void RunBasePlusVariantsFromMenu()
        {
            RunBasePlusVariants();
        }

        public static void RunBasePlusVariants()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string repoRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", "..", ".."));
            string outputDirectory = Path.Combine(repoRoot, "Evaluation", "chapter5_timing_outputs");

            TimingAnalysisResult result = Run(
                SyntheticDatasetGenerationMode.BasePlusVariants,
                outputDirectory);

            Debug.Log(
                "[TemporaryTimingAnalysis] " +
                "Wrote " + result.RowCount.ToString(CultureInfo.InvariantCulture) +
                " timing rows to " + result.AnalysisPath +
                " and summary to " + result.SummaryPath + ".");
        }

        private static TimingAnalysisResult Run(
            SyntheticDatasetGenerationMode generationMode,
            string outputDirectory)
        {
            if (string.IsNullOrEmpty(outputDirectory))
                throw new ArgumentException("Output directory is required.", nameof(outputDirectory));

            Directory.CreateDirectory(outputDirectory);

            IReadOnlyList<SyntheticTurnCase> syntheticCases =
                SyntheticPatternTurnDataset.Generate(generationMode);

            var compiler = new PatternCompiler();
            TurnAnalyser analyser = TemporaryPipelineFactory.CreateTurnAnalyser();
            ResponsePlanner planner = TemporaryPipelineFactory.CreateResponsePlanner();
            SkeletonBuilder skeletonBuilder = TemporaryPipelineFactory.CreateSkeletonBuilder();
            SkeletonBuilderConfig skeletonBuilderConfig = TemporaryPipelineFactory.CreateSkeletonBuilderConfig();
            var featureTransformer = new FeatureTransformer();

            var rows = new List<TimingAnalysisRow>(syntheticCases.Count);

            for (int i = 0; i < syntheticCases.Count; i++)
            {
                SyntheticTurnCase syntheticCase = syntheticCases[i];
                PatternTurn referencePattern = SyntheticPatternTurnFactory.CreatePatternTurn(syntheticCase);
                TurnWindow turnWindow = CreateTurnWindow(referencePattern);
                QuantisationSettings quantisation = CreateQuantisationSettings(referencePattern);

                double captureTimeMs = SamplesToMilliseconds(
                    turnWindow.DurationSamples,
                    referencePattern.sampleRate);

                Stopwatch stopwatch = Stopwatch.StartNew();
                PatternTurn compiledPattern = compiler.Compile(turnWindow, quantisation);
                double compileTimeMs = ElapsedAndRestart(stopwatch);

                TurnAnalysisResult analysis = analyser.Analyze(compiledPattern);
                double analysisTimeMs = ElapsedAndRestart(stopwatch);

                ResponsePlan plan = planner.Plan(analysis);
                double planningTimeMs = ElapsedAndRestart(stopwatch);

                SkeletonPattern skeletonPattern = skeletonBuilder.BuildSkeleton(
                    CreateSkeletonBuildRequest(
                        compiledPattern,
                        analysis,
                        plan,
                        skeletonBuilderConfig));
                PatternTurn generatedPattern = featureTransformer.Transform(compiledPattern);
                double generationTimeMs = ElapsedAndRestart(stopwatch);

                SimulatePlaybackTriggerPayload(generatedPattern);
                double playbackTriggerTimeMs = ElapsedAndRestart(stopwatch);
                stopwatch.Stop();

                double postCaptureLatencyMs =
                    compileTimeMs +
                    analysisTimeMs +
                    planningTimeMs +
                    generationTimeMs +
                    playbackTriggerTimeMs;

                rows.Add(new TimingAnalysisRow
                {
                    TurnID = syntheticCase.CaseId,
                    StructureType = syntheticCase.StructureType.ToString(),
                    EnergyProfile = syntheticCase.EnergyProfile.ToString(),
                    VariantKind = syntheticCase.VariantKind.ToString(),
                    HitCount = turnWindow.HitCount,
                    StepCount = compiledPattern.StepCount,
                    ResponseType = plan.ResponseType.ToString(),
                    CaptureTime = captureTimeMs,
                    CompileTime = compileTimeMs,
                    AnalysisTime = analysisTimeMs,
                    PlanningTime = planningTimeMs,
                    GenerationTime = generationTimeMs,
                    PlaybackTriggerTime = playbackTriggerTimeMs,
                    PostCaptureLatency = postCaptureLatencyMs,
                    TotalLatency = captureTimeMs + postCaptureLatencyMs,
                    PlaybackTriggerMode = "SimulatedUnityPayloadPreparation",
                    SkeletonSelectedStepCount =
                        skeletonPattern != null && skeletonPattern.SelectedStepIndices != null
                            ? skeletonPattern.SelectedStepIndices.Length
                            : 0
                });
            }

            string analysisPath = Path.Combine(outputDirectory, "timing_analysis.csv");
            string summaryPath = Path.Combine(outputDirectory, "timing_summary.csv");

            TimingAnalysisCsvWriter.WriteAnalysis(analysisPath, rows);
            TimingAnalysisCsvWriter.WriteSummary(summaryPath, rows);

            return new TimingAnalysisResult(analysisPath, summaryPath, rows.Count);
        }

        private static TurnWindow CreateTurnWindow(PatternTurn pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            var hits = new List<HitEvent>();
            int stepCount = pattern.StepCount;
            double samplesPerStep = stepCount > 0
                ? (pattern.endSamples - pattern.startSamples) / (double)stepCount
                : 0.0;

            for (int i = 0; i < stepCount; i++)
            {
                int velocity = pattern.velocity[i];
                if (velocity <= 0)
                    continue;

                long sampleTime = pattern.startSamples + (long)Math.Round(i * samplesPerStep);
                if (pattern.offsetSamples != null && i < pattern.offsetSamples.Length)
                    sampleTime += pattern.offsetSamples[i];

                hits.Add(new HitEvent(sampleTime, 0, velocity));
            }

            return new TurnWindow(
                pattern.turnId,
                pattern.startSamples,
                pattern.endSamples,
                hits);
        }

        private static QuantisationSettings CreateQuantisationSettings(PatternTurn pattern)
        {
            return new QuantisationSettings
            {
                bpm = pattern.bpm,
                stepsPerQuarter = pattern.stepsPerQuarter,
                sampleRate = pattern.sampleRate
            };
        }

        private static SkeletonBuildRequest CreateSkeletonBuildRequest(
            PatternTurn compiledPattern,
            TurnAnalysisResult analysis,
            ResponsePlan plan,
            SkeletonBuilderConfig config)
        {
            return new SkeletonBuildRequest
            {
                Plan = plan,
                SourceTurn = compiledPattern,
                SourceAnalysis = analysis,
                TurnLengthSteps = plan.TurnLengthSteps > 0
                    ? plan.TurnLengthSteps
                    : compiledPattern.StepCount,
                StepsPerQuarter = compiledPattern.stepsPerQuarter,
                Config = config
            };
        }

        private static void SimulatePlaybackTriggerPayload(PatternTurn turn)
        {
            if (turn == null || turn.velocity == null || turn.offsetSamples == null)
                throw new InvalidOperationException("Cannot simulate playback payload for an incomplete turn.");

            int stepCount = Math.Min(turn.velocity.Length, turn.offsetSamples.Length);
            if (stepCount <= 0)
                throw new InvalidOperationException("Cannot simulate playback payload for an empty turn.");

            int safeStepsPerQuarter = Math.Max(1, turn.stepsPerQuarter);
            double secondsPerQuarter = 60.0 / turn.bpm;
            double secondsPerStep = secondsPerQuarter / safeStepsPerQuarter;
            int samplesPerStep = Math.Max(1, (int)Math.Round(secondsPerStep * turn.sampleRate));

            long[] velocity = new long[stepCount];
            long[] offsets = new long[stepCount];

            for (int i = 0; i < stepCount; i++)
            {
                velocity[i] = turn.velocity[i];
                offsets[i] = turn.offsetSamples[i];
            }

            // Keep the computed values live so this method cannot be optimised into a no-op.
            if (samplesPerStep <= 0 || velocity.Length != offsets.Length)
                throw new InvalidOperationException("Invalid simulated playback payload.");
        }

        private static double SamplesToMilliseconds(long samples, int sampleRate)
        {
            if (sampleRate <= 0)
                return 0.0;

            return samples * 1000.0 / sampleRate;
        }

        private static double ElapsedAndRestart(Stopwatch stopwatch)
        {
            double elapsed = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
            return elapsed;
        }
    }

    internal sealed class TimingAnalysisResult
    {
        public TimingAnalysisResult(string analysisPath, string summaryPath, int rowCount)
        {
            AnalysisPath = analysisPath;
            SummaryPath = summaryPath;
            RowCount = rowCount;
        }

        public string AnalysisPath { get; private set; }
        public string SummaryPath { get; private set; }
        public int RowCount { get; private set; }
    }

    internal sealed class TimingAnalysisRow
    {
        public string TurnID;
        public string StructureType;
        public string EnergyProfile;
        public string VariantKind;
        public int HitCount;
        public int StepCount;
        public string ResponseType;
        public double CaptureTime;
        public double CompileTime;
        public double AnalysisTime;
        public double PlanningTime;
        public double GenerationTime;
        public double PlaybackTriggerTime;
        public double PostCaptureLatency;
        public double TotalLatency;
        public string PlaybackTriggerMode;
        public int SkeletonSelectedStepCount;
    }

    internal static class TimingAnalysisCsvWriter
    {
        private static readonly string[] AnalysisHeader =
        {
            "TurnID",
            "CaptureTime",
            "AnalysisTime",
            "PlanningTime",
            "GenerationTime",
            "PlaybackTriggerTime",
            "TotalLatency",
            "CompileTime",
            "PostCaptureLatency",
            "StructureType",
            "EnergyProfile",
            "VariantKind",
            "ResponseType",
            "HitCount",
            "StepCount",
            "PlaybackTriggerMode",
            "SkeletonSelectedStepCount"
        };

        private static readonly string[] SummaryHeader =
        {
            "Metric",
            "Count",
            "MeanMs",
            "MinMs",
            "MaxMs",
            "StdDevMs"
        };

        public static void WriteAnalysis(string path, IReadOnlyList<TimingAnalysisRow> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", AnalysisHeader));

            for (int i = 0; i < rows.Count; i++)
            {
                TimingAnalysisRow row = rows[i];
                builder.AppendLine(string.Join(",", new[]
                {
                    Csv(row.TurnID),
                    Format(row.CaptureTime),
                    Format(row.AnalysisTime),
                    Format(row.PlanningTime),
                    Format(row.GenerationTime),
                    Format(row.PlaybackTriggerTime),
                    Format(row.TotalLatency),
                    Format(row.CompileTime),
                    Format(row.PostCaptureLatency),
                    Csv(row.StructureType),
                    Csv(row.EnergyProfile),
                    Csv(row.VariantKind),
                    Csv(row.ResponseType),
                    row.HitCount.ToString(CultureInfo.InvariantCulture),
                    row.StepCount.ToString(CultureInfo.InvariantCulture),
                    Csv(row.PlaybackTriggerMode),
                    row.SkeletonSelectedStepCount.ToString(CultureInfo.InvariantCulture)
                }));
            }

            File.WriteAllText(path, builder.ToString());
        }

        public static void WriteSummary(string path, IReadOnlyList<TimingAnalysisRow> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", SummaryHeader));

            AppendSummary(builder, "CaptureTime", rows.Select(row => row.CaptureTime));
            AppendSummary(builder, "CompileTime", rows.Select(row => row.CompileTime));
            AppendSummary(builder, "AnalysisTime", rows.Select(row => row.AnalysisTime));
            AppendSummary(builder, "PlanningTime", rows.Select(row => row.PlanningTime));
            AppendSummary(builder, "GenerationTime", rows.Select(row => row.GenerationTime));
            AppendSummary(builder, "PlaybackTriggerTime", rows.Select(row => row.PlaybackTriggerTime));
            AppendSummary(builder, "PostCaptureLatency", rows.Select(row => row.PostCaptureLatency));
            AppendSummary(builder, "TotalLatency", rows.Select(row => row.TotalLatency));

            File.WriteAllText(path, builder.ToString());
        }

        private static void AppendSummary(
            StringBuilder builder,
            string metric,
            IEnumerable<double> values)
        {
            double[] array = values.ToArray();
            int count = array.Length;
            double mean = count > 0 ? array.Average() : 0.0;
            double min = count > 0 ? array.Min() : 0.0;
            double max = count > 0 ? array.Max() : 0.0;
            double stdDev = 0.0;

            if (count > 1)
            {
                double sumSquares = 0.0;
                for (int i = 0; i < count; i++)
                {
                    double delta = array[i] - mean;
                    sumSquares += delta * delta;
                }

                stdDev = Math.Sqrt(sumSquares / (count - 1));
            }

            builder.AppendLine(string.Join(",", new[]
            {
                Csv(metric),
                count.ToString(CultureInfo.InvariantCulture),
                Format(mean),
                Format(min),
                Format(max),
                Format(stdDev)
            }));
        }

        private static string Format(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
