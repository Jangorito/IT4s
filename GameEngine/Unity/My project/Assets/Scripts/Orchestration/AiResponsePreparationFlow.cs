using System;
using IT4s.Data;
using IT4s.Rhythm.Generation.Skeleton;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.Transformations;
using IT4s.Rhythm.TurnAnalysis;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Orchestration
{
    /// <summary>
    /// Plain C# flow for the synchronous AI preparation path.
    /// It owns the concrete analyse -> plan -> transform sequence, while the controller remains
    /// responsible for deciding when to run it, storing results, publishing events, and triggering playback.
    /// </summary>
    public sealed class AiResponsePreparationFlow
    {
        private readonly TurnAnalyser turnAnalyser;
        private readonly IResponsePlanner responsePlanner;
        private readonly ISkeletonBuilder skeletonBuilder;
        private readonly SkeletonBuilderConfig skeletonBuilderConfig;
        private readonly FeatureTransformer featureTransformer;

        public AiResponsePreparationFlow(
            TurnAnalyser turnAnalyser,
            IResponsePlanner responsePlanner,
            FeatureTransformer featureTransformer)
            : this(turnAnalyser, responsePlanner, null, null, featureTransformer)
        {
        }

        public AiResponsePreparationFlow(
            TurnAnalyser turnAnalyser,
            IResponsePlanner responsePlanner,
            ISkeletonBuilder skeletonBuilder,
            SkeletonBuilderConfig skeletonBuilderConfig,
            FeatureTransformer featureTransformer)
        {
            this.turnAnalyser = turnAnalyser;
            this.responsePlanner = responsePlanner;
            this.skeletonBuilder = skeletonBuilder;
            this.skeletonBuilderConfig = skeletonBuilderConfig;
            this.featureTransformer = featureTransformer;
        }

        public AiResponsePreparationResult Prepare(
            PatternTurn compiledPattern,
            Action<TurnAnalysisResult> onAnalysed = null,
            Action<ResponsePlan> onPlanned = null,
            Action<PatternTurn> onGenerated = null,
            Action<SkeletonPattern> onSkeletonBuilt = null)
        {
            if (compiledPattern == null)
            {
                throw new InvalidOperationException(
                    "GeneratingAiResponse failed because no compiled human PatternTurn was available.");
            }

            if (turnAnalyser == null)
            {
                throw new InvalidOperationException(
                    "GeneratingAiResponse failed because TurnAnalyser reference is missing.");
            }

            TurnAnalysisResult analysis = ExecuteRequiredStage(
                "TurnAnalyser.Analyze",
                () => turnAnalyser.Analyze(compiledPattern),
                () => $"TurnAnalyser returned null when analysing turn {compiledPattern.turnId}.");

            onAnalysed?.Invoke(analysis);

            if (responsePlanner == null)
            {
                throw new InvalidOperationException(
                    "GeneratingAiResponse failed because IResponsePlanner reference is missing.");
            }

            ResponsePlan responsePlan = ExecuteRequiredStage(
                "IResponsePlanner.Plan",
                () => responsePlanner.Plan(analysis),
                () => $"IResponsePlanner returned null when planning a response for turn {compiledPattern.turnId}.");

            onPlanned?.Invoke(responsePlan);

            SkeletonPattern skeletonPattern = null;
            if (skeletonBuilder != null)
            {
                if (skeletonBuilderConfig == null)
                {
                    throw new InvalidOperationException(
                        "GeneratingAiResponse failed because SkeletonBuilderConfig reference is missing.");
                }

                skeletonPattern = ExecuteRequiredStage(
                    "ISkeletonBuilder.BuildSkeleton",
                    () => skeletonBuilder.BuildSkeleton(CreateSkeletonBuildRequest(compiledPattern, analysis, responsePlan)),
                    () => $"ISkeletonBuilder returned null when building a response skeleton for turn {compiledPattern.turnId}.");

                onSkeletonBuilt?.Invoke(skeletonPattern);
            }

            if (featureTransformer == null)
            {
                throw new InvalidOperationException(
                    "GeneratingAiResponse failed because FeatureTransformer reference is missing.");
            }

            // ResponsePlan is intentionally produced and exposed first, but generation still
            // transforms the compiled human pattern directly. That gap remains explicit for now.
            PatternTurn generatedPattern = ExecuteRequiredStage(
                "FeatureTransformer.Transform",
                () => featureTransformer.Transform(compiledPattern),
                () => $"FeatureTransformer returned null when generating an AI response for turn {compiledPattern.turnId}.");

            onGenerated?.Invoke(generatedPattern);

            return new AiResponsePreparationResult(analysis, responsePlan, generatedPattern, skeletonPattern);
        }

        private SkeletonBuildRequest CreateSkeletonBuildRequest(
            PatternTurn compiledPattern,
            TurnAnalysisResult analysis,
            ResponsePlan responsePlan)
        {
            return new SkeletonBuildRequest
            {
                Plan = responsePlan,
                SourceTurn = compiledPattern,
                SourceAnalysis = analysis,
                TurnLengthSteps = responsePlan.TurnLengthSteps > 0
                    ? responsePlan.TurnLengthSteps
                    : compiledPattern.StepCount,
                StepsPerQuarter = compiledPattern.stepsPerQuarter,
                Config = skeletonBuilderConfig
            };
        }

        private static T ExecuteRequiredStage<T>(
            string operation,
            Func<T> action,
            Func<string> nullResultMessageFactory)
            where T : class
        {
            try
            {
                T result = action();

                if (result == null)
                {
                    throw new MissingStageResultException(nullResultMessageFactory());
                }

                return result;
            }
            catch (MissingStageResultException ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Exception during {operation}: {ex.Message}", ex);
            }
        }

        private sealed class MissingStageResultException : Exception
        {
            public MissingStageResultException(string message)
                : base(message)
            {
            }
        }
    }
}
