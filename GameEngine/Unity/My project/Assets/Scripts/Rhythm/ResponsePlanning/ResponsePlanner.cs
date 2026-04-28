using System;
using System.Collections.Generic;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.ResponsePlanning
{
    public sealed class ResponsePlanner : IResponsePlanner
    {
        private const float MaxVelocity = 127f;
        private const float ScoreEqualityEpsilon = 0.0001f;
        private readonly ResponsePlannerConfig config;
        private Dictionary<ResponseType, float> lastScores = new Dictionary<ResponseType, float>();

        public ResponsePlanner()
            : this(new ResponsePlannerConfig())
        {
        }

        public ResponsePlanner(ResponsePlannerConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ResponsePlannerDebugSnapshot LastSnapshot { get; private set; }

        public ResponsePlan Plan(TurnAnalysisResult analysis)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            LastSnapshot = null;
            PlanningContext context = BuildContext(analysis, config);
            ResponseType responseType = ChooseResponseType(context, config);
            ResponsePlan plan = DerivePlan(context, responseType, config);
            LastSnapshot = CreateDebugSnapshot(context, responseType, plan, lastScores);
            return plan;
        }

        public ResponsePlan PlanForResponseType(TurnAnalysisResult analysis, ResponseType responseType)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            PlanningContext context = BuildContext(analysis, config);
            return DerivePlan(context, responseType, config);
        }

        private static PlanningContext BuildContext(TurnAnalysisResult analysis, ResponsePlannerConfig config)
        {
            DensityFeatures density = GetDensity(analysis);
            EnergyFeatures energy = GetEnergy(analysis);
            AnchorFeatures anchors = GetAnchors(analysis);
            EndActivityFeatures endActivity = GetEndActivity(analysis);
            SegmentActivityProfileFeatures profile = GetSegmentActivityProfile(analysis);

            float sourceDensity = Clamp01(density.StepDensity);
            float sourceEnergy = NormalizeVelocity(energy.MeanVelocity);
            float endDensity = Clamp01(endActivity.EndDensity);
            float endEnergy = NormalizeVelocity(endActivity.EndEnergy);
            float endAccent = NormalizeVelocity(endActivity.EndAccent);
            bool hasMeaningfulAnchors =
                anchors.AnchorCount >= config.MeaningfulAnchorCountThreshold &&
                anchors.StrongestAnchorScore >= config.MeaningfulAnchorScoreThreshold;
            bool hasStrongEnding =
                anchors.HasClosingAnchor ||
                (endDensity >= config.StrongEndingDensityThreshold &&
                (endEnergy >= config.StrongEndingEnergyThreshold ||
                endAccent >= config.StrongEndingAccentThreshold));
            bool endingIsOpen =
                !anchors.HasClosingAnchor &&
                endDensity <= config.OpenEndingDensityThreshold &&
                endEnergy <= config.OpenEndingEnergyThreshold &&
                endAccent <= config.OpenEndingAccentThreshold;
            bool activityIsBackLoaded = HasLateBias(profile.DensityShape) || HasLateBias(profile.EnergyShape);
            bool activityIsFrontLoaded = HasEarlyBias(profile.DensityShape) || HasEarlyBias(profile.EnergyShape);
            bool activityIsBalanced = IsBalancedProfile(profile);
            bool isSparse = sourceDensity <= config.SparseDensityThreshold;
            bool isBusy = sourceDensity >= config.BusyDensityThreshold;
            bool isLowEnergy = energy.IsLowEnergy || sourceEnergy <= config.LowEnergyThreshold;
            bool isHighEnergy = energy.IsHighEnergy || sourceEnergy >= config.HighEnergyThreshold;
            bool hasConversationalSpace =
                sourceDensity > config.SparseDensityThreshold &&
                sourceDensity <= config.ConversationalSpaceDensityThreshold &&
                !endingIsOpen;
            bool isCongested = sourceDensity >= config.CongestedDensityThreshold || (isBusy && isHighEnergy);
            bool isPressurised =
                isHighEnergy &&
                sourceDensity >= config.PressurisedDensityThreshold;
            bool isPredictableProfile = IsPredictableShape(profile.DensityShape) || IsPredictableShape(profile.EnergyShape);

            return new PlanningContext(
                sourceDensity,
                sourceEnergy,
                anchors.AnchorCount,
                hasMeaningfulAnchors,
                hasStrongEnding,
                endingIsOpen,
                activityIsBackLoaded,
                activityIsFrontLoaded,
                activityIsBalanced,
                isSparse,
                isBusy,
                isLowEnergy,
                isHighEnergy,
                hasConversationalSpace,
                isCongested,
                isPressurised,
                isPredictableProfile,
                GetTurnLengthSteps(analysis));
        }

        private ResponseType ChooseResponseType(PlanningContext context, ResponsePlannerConfig config)
        {
            lastScores = ScoreResponseTypes(context, config);
            float bestScore = GetBestScore(lastScores);
            List<ResponseType> candidates = GetCloseScoreCandidates(lastScores, bestScore, config.ScoreTieMargin);

            if (candidates.Count == 1)
                return candidates[0];

            return ResolveCloseScoreTie(candidates, lastScores, context, config);
        }

        private ResponsePlan DerivePlan(PlanningContext context, ResponseType responseType, ResponsePlannerConfig config)
        {
            ResponsePlan plan = new ResponsePlan(
                responseType,
                DeriveTargetDensity(context, responseType, config),
                DeriveComplementarityBias(context, responseType, config),
                DerivePreserveAnchors(context, responseType),
                DeriveMirrorEnding(context, responseType),
                context.TurnLengthSteps);

            return Clamp(plan);
        }

        private static ResponsePlan Clamp(ResponsePlan plan)
        {
            return plan.With(
                targetDensity: Clamp01(plan.TargetDensity),
                complementarityBias: Clamp01(plan.ComplementarityBias),
                turnLengthSteps: Math.Max(1, plan.TurnLengthSteps));
        }

        private float DeriveTargetDensity(PlanningContext context, ResponseType responseType, ResponsePlannerConfig config)
        {
            float density = context.SourceDensity + GetDensityDelta(responseType);
            density += GetDensityContextModifier(context, responseType, config);
            density = ClampDensityChange(context.SourceDensity, density, config);
            return Clamp(density, config.MinTargetDensity, config.MaxTargetDensity);
        }

        private static float DeriveComplementarityBias(PlanningContext context, ResponseType responseType, ResponsePlannerConfig config)
        {
            float bias = GetBaselineComplementarityBias(responseType);
            float adjustment = config.ComplementarityAdjustment;

            switch (responseType)
            {
                case ResponseType.Mirror:
                    if (context.HasMeaningfulAnchors) bias -= adjustment;
                    if (context.HasStrongEnding) bias -= adjustment * 0.5f;
                    if (context.HasConversationalSpace) bias += adjustment * 0.5f;
                    break;
                case ResponseType.Complement:
                    if (context.HasConversationalSpace) bias += adjustment;
                    if (context.HasMeaningfulAnchors) bias -= adjustment;
                    if (context.HasStrongEnding) bias -= adjustment * 0.5f;
                    break;
                case ResponseType.Simplify:
                    if (context.IsCongested) bias += adjustment;
                    if (context.HasMeaningfulAnchors) bias -= adjustment;
                    break;
                case ResponseType.Intensify:
                    if (context.HasConversationalSpace) bias += adjustment;
                    if (context.EndingIsOpen) bias += adjustment * 0.5f;
                    if (context.HasMeaningfulAnchors) bias -= adjustment;
                    break;
                case ResponseType.Contrast:
                    if (context.IsPredictableProfile) bias += adjustment;
                    if (context.HasMeaningfulAnchors) bias -= adjustment;
                    break;
                case ResponseType.Fill:
                    if (context.HasConversationalSpace) bias += adjustment;
                    if (context.EndingIsOpen) bias += adjustment * 0.5f;
                    if (context.HasMeaningfulAnchors) bias -= adjustment;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }

            return Clamp01(bias);
        }

        private static bool DerivePreserveAnchors(PlanningContext context, ResponseType responseType)
        {
            if (!context.HasMeaningfulAnchors)
                return false;

            switch (responseType)
            {
                case ResponseType.Mirror:
                    return true;
                case ResponseType.Complement:
                    return !context.IsCongested;
                case ResponseType.Simplify:
                    return !context.IsCongested && (context.HasStrongEnding || context.ActivityIsBalanced);
                case ResponseType.Contrast:
                case ResponseType.Intensify:
                case ResponseType.Fill:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private static bool DeriveMirrorEnding(PlanningContext context, ResponseType responseType)
        {
            return responseType == ResponseType.Mirror &&
                context.HasStrongEnding &&
                !context.EndingIsOpen;
        }

        private static Dictionary<ResponseType, float> ScoreResponseTypes(PlanningContext context, ResponsePlannerConfig config)
        {
            return new Dictionary<ResponseType, float>
            {
                [ResponseType.Mirror] = ScoreMirror(context, config),
                [ResponseType.Complement] = ScoreComplement(context, config),
                [ResponseType.Simplify] = ScoreSimplify(context, config),
                [ResponseType.Intensify] = ScoreIntensify(context, config),
                [ResponseType.Contrast] = ScoreContrast(context, config),
                [ResponseType.Fill] = ScoreFill(context, config)
            };
        }

        private static float ScoreMirror(PlanningContext context, ResponsePlannerConfig config)
        {
            float score = 0f;
            if (context.IsBalancedDensity) score += config.DensityPrimaryScore;
            if (context.IsMediumEnergy) score += config.EnergyPrimaryScore;
            if (context.HasMeaningfulAnchors) score += config.AnchorRefinementScore;
            if (context.HasStrongEnding) score += config.EndingRefinementScore;
            if (context.ActivityIsBalanced) score += config.ProfileRefinementScore;
            if (context.EndingIsOpen) score -= config.EndingRefinementScore;
            if (context.IsPredictableProfile) score -= config.ProfileRefinementScore * 0.5f;
            return score;
        }

        private static float ScoreComplement(PlanningContext context, ResponsePlannerConfig config)
        {
            float score = 0f;
            if (context.IsBalancedDensity) score += config.DensityPrimaryScore * 0.75f;
            if (context.IsMediumEnergy) score += config.EnergyPrimaryScore * 0.75f;
            if (context.HasMeaningfulAnchors) score += config.AnchorRefinementScore;
            else score -= config.AnchorRefinementScore * 0.5f;
            if (context.HasConversationalSpace) score += config.ConversationalSpaceScore;
            if (!context.HasStrongEnding) score += config.EndingRefinementScore * 0.5f;
            if (context.ActivityIsBalanced) score += config.ProfileRefinementScore;
            if (context.IsBusy) score -= config.DensityPrimaryScore * 0.5f;
            return score;
        }

        private static float ScoreSimplify(PlanningContext context, ResponsePlannerConfig config)
        {
            float score = 0f;
            if (context.IsBusy) score += config.DensityPrimaryScore;
            if (context.IsHighEnergy) score += config.EnergyPrimaryScore;
            if (context.IsCongested) score += config.CongestionScore;
            if (context.IsPressurised) score += config.PressurisedScore;
            if (context.IsPredictableProfile) score += config.ProfileRefinementScore * 0.5f;
            if (context.IsSparse && !context.IsPressurised) score -= config.DensityPrimaryScore;
            if (context.IsLowEnergy) score -= config.EnergyPrimaryScore * 0.5f;
            return score;
        }

        private static float ScoreIntensify(PlanningContext context, ResponsePlannerConfig config)
        {
            float score = 0f;
            if (context.IsSparse && !context.IsPressurised) score += config.DensityPrimaryScore;
            if (context.IsLowEnergy) score += config.EnergyPrimaryScore;
            else if (context.IsMediumEnergy) score += config.EnergyPrimaryScore * 0.5f;
            if (!context.HasMeaningfulAnchors) score += config.AnchorRefinementScore * 0.5f;
            if (!context.HasStrongEnding) score += config.EndingRefinementScore * 0.5f;
            if (context.IsBusy) score -= config.DensityPrimaryScore * 0.75f;
            if (context.IsCongested) score -= config.CongestionScore;
            if (context.IsPressurised) score -= config.PressurisedScore;
            return score;
        }

        private static float ScoreContrast(PlanningContext context, ResponsePlannerConfig config)
        {
            bool hasProfileReason =
                context.IsPredictableProfile ||
                context.ActivityIsBackLoaded ||
                context.ActivityIsFrontLoaded;

            if (!hasProfileReason)
                return -config.PredictableProfileScore * 0.25f;

            float score = 0f;
            if (context.IsPredictableProfile) score += config.PredictableProfileScore;
            if (context.ActivityIsBackLoaded || context.ActivityIsFrontLoaded) score += config.ProfileRefinementScore;
            if (context.IsHighEnergy) score += config.EnergyPrimaryScore * 0.5f;
            if (context.IsBalancedDensity || context.IsBusy) score += config.DensityPrimaryScore * 0.25f;
            if (!context.HasMeaningfulAnchors) score += config.AnchorRefinementScore * 0.5f;
            if (context.HasMeaningfulAnchors) score -= config.AnchorRefinementScore;
            if (context.ActivityIsBalanced) score -= config.ProfileRefinementScore * 0.5f;
            if (!context.IsPredictableProfile) score -= config.PredictableProfileScore * 0.5f;
            return score;
        }

        private static float ScoreFill(PlanningContext context, ResponsePlannerConfig config)
        {
            float score = 0f;
            if (context.IsSparse) score += config.DensityPrimaryScore;
            if (context.EndingIsOpen) score += config.EndingRefinementScore * 1.5f;
            else if (!context.HasStrongEnding) score += config.EndingRefinementScore * 0.5f;
            if (context.ActivityIsBackLoaded) score += config.ProfileRefinementScore;
            if (context.IsLowEnergy) score += config.EnergyPrimaryScore * 0.5f;
            if (context.HasStrongEnding) score -= config.EndingRefinementScore;
            if (context.IsBusy) score -= config.DensityPrimaryScore * 0.5f;
            return score;
        }

        private static float GetBestScore(IReadOnlyDictionary<ResponseType, float> scores)
        {
            float bestScore = float.MinValue;

            foreach (KeyValuePair<ResponseType, float> entry in scores)
            {
                if (entry.Value > bestScore)
                    bestScore = entry.Value;
            }

            return bestScore;
        }

        private static List<ResponseType> GetCloseScoreCandidates(
            IReadOnlyDictionary<ResponseType, float> scores,
            float bestScore,
            float tieMargin)
        {
            var candidates = new List<ResponseType>(scores.Count);

            foreach (KeyValuePair<ResponseType, float> entry in scores)
            {
                if (bestScore - entry.Value <= tieMargin)
                    candidates.Add(entry.Key);
            }

            return candidates;
        }

        private static ResponseType ResolveCloseScoreTie(
            IReadOnlyList<ResponseType> candidates,
            IReadOnlyDictionary<ResponseType, float> scores,
            PlanningContext context,
            ResponsePlannerConfig config)
        {
            if (ContainsPair(candidates, ResponseType.Complement, ResponseType.Mirror) &&
                IsConversationalGapPlayUseful(context))
            {
                return ResponseType.Complement;
            }

            if (ContainsPair(candidates, ResponseType.Simplify, ResponseType.Contrast) &&
                context.SourceDensity >= config.CongestedDensityThreshold)
            {
                return ResponseType.Simplify;
            }

            if (ContainsPair(candidates, ResponseType.Fill, ResponseType.Intensify) &&
                IsWeakClosurePrimaryIssue(context))
            {
                return ResponseType.Fill;
            }

            if (ContainsCandidate(candidates, ResponseType.Mirror) &&
                HasBalancedAnchoredIdentity(context))
            {
                return ResponseType.Mirror;
            }

            return SelectHighestScoringCandidate(candidates, scores);
        }

        private static ResponseType SelectHighestScoringCandidate(
            IReadOnlyList<ResponseType> candidates,
            IReadOnlyDictionary<ResponseType, float> scores)
        {
            ResponseType bestType = candidates[0];
            float bestScore = scores[bestType];

            for (int i = 1; i < candidates.Count; i++)
            {
                ResponseType candidate = candidates[i];
                float candidateScore = scores[candidate];

                if (candidateScore > bestScore + ScoreEqualityEpsilon)
                {
                    bestType = candidate;
                    bestScore = candidateScore;
                    continue;
                }

                if (Math.Abs(candidateScore - bestScore) <= ScoreEqualityEpsilon &&
                    GetStableTiePreference(candidate) < GetStableTiePreference(bestType))
                {
                    bestType = candidate;
                    bestScore = candidateScore;
                }
            }

            return bestType;
        }

        private static bool ContainsPair(IReadOnlyList<ResponseType> candidates, ResponseType first, ResponseType second)
        {
            return ContainsCandidate(candidates, first) && ContainsCandidate(candidates, second);
        }

        private static bool ContainsCandidate(IReadOnlyList<ResponseType> candidates, ResponseType candidate)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == candidate)
                    return true;
            }

            return false;
        }

        private static bool IsConversationalGapPlayUseful(PlanningContext context)
        {
            return context.HasConversationalSpace &&
                context.HasMeaningfulAnchors &&
                context.IsBalancedDensity &&
                !context.EndingIsOpen;
        }

        private static bool IsWeakClosurePrimaryIssue(PlanningContext context)
        {
            return context.EndingIsOpen ||
                (!context.HasStrongEnding && context.ActivityIsBackLoaded);
        }

        private static bool HasBalancedAnchoredIdentity(PlanningContext context)
        {
            return context.HasMeaningfulAnchors &&
                context.IsBalancedDensity &&
                !context.EndingIsOpen;
        }

        private static int GetStableTiePreference(ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Mirror:
                    return 0;
                case ResponseType.Complement:
                    return 1;
                case ResponseType.Simplify:
                    return 2;
                case ResponseType.Intensify:
                    return 3;
                case ResponseType.Fill:
                    return 4;
                case ResponseType.Contrast:
                    return 5;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private float GetDensityDelta(ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Mirror: return 0.00f;
                case ResponseType.Complement: return -0.05f;
                case ResponseType.Simplify: return -0.15f;
                case ResponseType.Intensify: return 0.12f;
                case ResponseType.Contrast: return 0.00f;
                case ResponseType.Fill: return 0.15f;
                default: throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private static float GetBaselineComplementarityBias(ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Mirror: return 0.30f;
                case ResponseType.Complement: return 0.75f;
                case ResponseType.Simplify: return 0.40f;
                case ResponseType.Intensify: return 0.55f;
                case ResponseType.Contrast: return 0.65f;
                case ResponseType.Fill: return 0.70f;
                default: throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private static float GetDensityContextModifier(
            PlanningContext context,
            ResponseType responseType,
            ResponsePlannerConfig config)
        {
            float adjustment = config.DensityContextAdjustment;

            switch (responseType)
            {
                case ResponseType.Mirror:
                    if (context.IsBusy && context.IsHighEnergy) return -adjustment * 0.25f;
                    if (context.IsSparse && !context.EndingIsOpen) return adjustment * 0.25f;
                    return 0f;
                case ResponseType.Complement:
                    if (context.IsCongested) return -adjustment * 0.5f;
                    if (context.IsSparse && context.HasConversationalSpace) return adjustment * 0.25f;
                    return 0f;
                case ResponseType.Simplify:
                    float simplifyModifier = 0f;
                    if (context.IsBusy) simplifyModifier -= adjustment * 0.5f;
                    if (context.IsHighEnergy) simplifyModifier -= adjustment * 0.25f;
                    if (context.IsCongested) simplifyModifier -= adjustment * 0.25f;
                    if (context.HasMeaningfulAnchors && context.HasStrongEnding) simplifyModifier += adjustment * 0.25f;
                    return simplifyModifier;
                case ResponseType.Intensify:
                    float intensifyModifier = 0f;
                    if (context.SourceDensity <= config.VerySparseDensityThreshold) intensifyModifier += adjustment;
                    else if (context.IsSparse) intensifyModifier += adjustment * 0.5f;
                    if (context.IsLowEnergy) intensifyModifier += adjustment * 0.25f;
                    if (context.HasStrongEnding) intensifyModifier -= adjustment * 0.25f;
                    return intensifyModifier;
                case ResponseType.Contrast:
                    if (context.IsSparse) return adjustment * 0.25f;
                    if (context.IsBusy) return -adjustment * 0.25f;
                    return 0f;
                case ResponseType.Fill:
                    float fillModifier = 0f;
                    if (context.EndingIsOpen) fillModifier += adjustment;
                    else if (!context.HasStrongEnding) fillModifier += adjustment * 0.5f;
                    if (context.ActivityIsBackLoaded) fillModifier += adjustment * 0.5f;
                    return fillModifier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private static float ClampDensityChange(
            float sourceDensity,
            float targetDensity,
            ResponsePlannerConfig config)
        {
            return Clamp(
                targetDensity,
                sourceDensity - config.MaxTargetDensityDelta,
                sourceDensity + config.MaxTargetDensityDelta);
        }

        private static ResponsePlannerDebugSnapshot CreateDebugSnapshot(
            PlanningContext context,
            ResponseType selectedResponseType,
            ResponsePlan finalPlan,
            IReadOnlyDictionary<ResponseType, float> scores)
        {
            return new ResponsePlannerDebugSnapshot(
                CreateDescriptorSummary(context),
                CreateNumericSummary(context),
                CreateScoreSnapshot(scores),
                selectedResponseType,
                finalPlan);
        }

        private static ResponsePlannerDescriptorSummary CreateDescriptorSummary(PlanningContext context)
        {
            return new ResponsePlannerDescriptorSummary(
                BuildDescriptorSummaryText(context),
                context.IsSparse,
                context.IsBalancedDensity,
                context.IsBusy,
                context.IsLowEnergy,
                context.IsMediumEnergy,
                context.IsHighEnergy,
                context.HasMeaningfulAnchors,
                context.HasStrongEnding,
                context.EndingIsOpen,
                context.ActivityIsBackLoaded,
                context.ActivityIsFrontLoaded,
                context.ActivityIsBalanced,
                context.HasConversationalSpace,
                context.IsPredictableProfile,
                context.IsCongested,
                context.IsPressurised);
        }

        private static ResponsePlannerNumericSummary CreateNumericSummary(PlanningContext context)
        {
            return new ResponsePlannerNumericSummary(
                context.SourceDensity,
                context.SourceEnergy,
                context.AnchorCount,
                context.TurnLengthSteps);
        }

        private static IReadOnlyList<ResponseTypeScore> CreateScoreSnapshot(IReadOnlyDictionary<ResponseType, float> scores)
        {
            ResponseType[] responseTypes =
            {
                ResponseType.Mirror,
                ResponseType.Complement,
                ResponseType.Simplify,
                ResponseType.Intensify,
                ResponseType.Contrast,
                ResponseType.Fill
            };

            var snapshot = new ResponseTypeScore[responseTypes.Length];
            for (int i = 0; i < responseTypes.Length; i++)
            {
                ResponseType responseType = responseTypes[i];
                snapshot[i] = new ResponseTypeScore(responseType, GetScore(scores, responseType));
            }

            return Array.AsReadOnly(snapshot);
        }

        private static string BuildDescriptorSummaryText(PlanningContext context)
        {
            var descriptors = new List<string>(8);

            if (context.IsSparse) descriptors.Add("Sparse");
            else if (context.IsBusy) descriptors.Add("Busy");
            else descriptors.Add("BalancedDensity");

            if (context.IsLowEnergy) descriptors.Add("LowEnergy");
            else if (context.IsHighEnergy) descriptors.Add("HighEnergy");
            else descriptors.Add("MediumEnergy");

            if (context.HasMeaningfulAnchors) descriptors.Add("MeaningfulAnchors");
            if (context.HasStrongEnding) descriptors.Add("StrongEnding");
            else if (context.EndingIsOpen) descriptors.Add("OpenEnding");
            if (context.HasConversationalSpace) descriptors.Add("ConversationalSpace");
            if (context.ActivityIsBackLoaded) descriptors.Add("BackLoaded");
            else if (context.ActivityIsFrontLoaded) descriptors.Add("FrontLoaded");
            else if (context.ActivityIsBalanced) descriptors.Add("BalancedProfile");
            if (context.IsPredictableProfile) descriptors.Add("PredictableProfile");
            if (context.IsCongested) descriptors.Add("Congested");
            if (context.IsPressurised) descriptors.Add("Pressurised");

            return descriptors.Count == 0
                ? "Neutral"
                : string.Join(", ", descriptors);
        }

        private static float GetScore(IReadOnlyDictionary<ResponseType, float> scores, ResponseType responseType)
        {
            return scores != null && scores.TryGetValue(responseType, out float score)
                ? score
                : 0f;
        }

        private static bool HasLateBias(ActivityShape shape)
        {
            return shape == ActivityShape.Increasing || shape == ActivityShape.BackLoaded;
        }

        private static bool HasEarlyBias(ActivityShape shape)
        {
            return shape == ActivityShape.Decreasing || shape == ActivityShape.FrontLoaded;
        }

        private static bool IsBalancedProfile(SegmentActivityProfileFeatures profile)
        {
            return IsBalancedShape(profile.DensityShape) && IsBalancedShape(profile.EnergyShape);
        }

        private static bool IsBalancedShape(ActivityShape shape)
        {
            switch (shape)
            {
                case ActivityShape.Flat:
                case ActivityShape.MidPeak:
                case ActivityShape.MidDip:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsPredictableShape(ActivityShape shape)
        {
            switch (shape)
            {
                case ActivityShape.Increasing:
                case ActivityShape.Decreasing:
                case ActivityShape.FrontLoaded:
                case ActivityShape.BackLoaded:
                    return true;
                default:
                    return false;
            }
        }

        private static int GetTurnLengthSteps(TurnAnalysisResult analysis)
        {
            return Math.Max(GetDensity(analysis).StepCount, GetAnchors(analysis).StepCount);
        }

        private static DensityFeatures GetDensity(TurnAnalysisResult analysis)
        {
            return analysis.Density ?? new DensityFeatures();
        }

        private static EnergyFeatures GetEnergy(TurnAnalysisResult analysis)
        {
            return analysis.Energy ?? new EnergyFeatures();
        }

        private static AnchorFeatures GetAnchors(TurnAnalysisResult analysis)
        {
            return analysis.Anchor ?? new AnchorFeatures();
        }

        private static EndActivityFeatures GetEndActivity(TurnAnalysisResult analysis)
        {
            return analysis.EndActivity ?? new EndActivityFeatures();
        }

        private static SegmentActivityProfileFeatures GetSegmentActivityProfile(TurnAnalysisResult analysis)
        {
            return analysis.SegmentActivityProfile ?? new SegmentActivityProfileFeatures();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static float NormalizeVelocity(float velocity)
        {
            return Clamp01(velocity / MaxVelocity);
        }

        private static float NormalizeVelocity(int velocity)
        {
            return Clamp01(velocity / MaxVelocity);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }
    }
}
