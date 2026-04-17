using System;
using IT4s.Rhythm.ResponsePlanning.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal enum SourceOverlapPolicy
    {
        Preserve,
        ReduceToAnchors,
        PreferGaps,
        Displace,
        Interstitial,
        ExpandAroundSource
    }

    internal enum MetricTolerancePolicy
    {
        SpineOnly,
        StrongAndMedium,
        Broad,
        OffbeatFriendly
    }

    internal enum WeakStepPolicy
    {
        Suppress,
        Defer,
        Allow,
        PreferInterstitial
    }

    internal enum EndingPolicy
    {
        MirrorSource,
        StructuralOnly,
        FillGap,
        AvoidAccent,
        LateDrive
    }

    internal enum SegmentTargetPolicy
    {
        FollowSource,
        StructuralSpine,
        SparseSegments,
        InvertSource,
        Interstitial,
        ActiveLateExpansion
    }

    internal sealed class ResponseBehaviourProfile
    {
        public ResponseType ResponseType { get; set; }
        public SourceOverlapPolicy SourceOverlapPolicy { get; set; }
        public MetricTolerancePolicy MetricTolerancePolicy { get; set; }
        public WeakStepPolicy WeakStepPolicy { get; set; }
        public EndingPolicy EndingPolicy { get; set; }
        public SegmentTargetPolicy SegmentTargetPolicy { get; set; }

        public bool PreserveAnchors { get; set; }
        public bool RequireStrongEnding { get; set; }
        public bool LimitDirectOverlap { get; set; }
        public bool GuaranteeInterstitial { get; set; }
        public bool GuaranteeExpansion { get; set; }
        public bool GuaranteeLateDrive { get; set; }

        public float TargetDensity { get; set; }
        public float ComplementarityBias { get; set; }
        public float SourceHitWeight { get; set; }
        public float SourceGapWeight { get; set; }
        public float AdjacentSourceWeight { get; set; }
        public float NearSourceWeight { get; set; }
        public float FarSourceWeight { get; set; }
        public float InterstitialWeight { get; set; }
        public float DenseSourceRegionWeight { get; set; }
        public float SparseSourceRegionWeight { get; set; }
        public float SegmentInfluence { get; set; }
        public float DirectOverlapSoftLimit { get; set; }
        public float WeakStepSelectionPortion { get; set; }
        public float[] SegmentTargetWeights { get; set; }

        public static ResponseBehaviourProfile Neutral(
            ResponseType responseType,
            float targetDensity,
            int segmentCount)
        {
            return new ResponseBehaviourProfile
            {
                ResponseType = responseType,
                SourceOverlapPolicy = SourceOverlapPolicy.Preserve,
                MetricTolerancePolicy = MetricTolerancePolicy.Broad,
                WeakStepPolicy = WeakStepPolicy.Defer,
                EndingPolicy = EndingPolicy.StructuralOnly,
                SegmentTargetPolicy = SegmentTargetPolicy.FollowSource,
                TargetDensity = targetDensity,
                DirectOverlapSoftLimit = 1f,
                WeakStepSelectionPortion = 0.65f,
                SegmentTargetWeights = UniformSegmentWeights(segmentCount)
            };
        }

        public float GetSegmentTargetWeight(int segmentIndex)
        {
            if (SegmentTargetWeights == null ||
                segmentIndex < 0 ||
                segmentIndex >= SegmentTargetWeights.Length)
            {
                return 1f;
            }

            return SegmentTargetWeights[segmentIndex];
        }

        public float GetMetricClassBias(MetricStrengthLevel strengthLevel)
        {
            switch (MetricTolerancePolicy)
            {
                case MetricTolerancePolicy.SpineOnly:
                    switch (strengthLevel)
                    {
                        case MetricStrengthLevel.Strongest: return 0.10f;
                        case MetricStrengthLevel.Strong: return 0.02f;
                        case MetricStrengthLevel.Medium: return -0.07f;
                        case MetricStrengthLevel.Weak: return -0.16f;
                        default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
                    }

                case MetricTolerancePolicy.StrongAndMedium:
                    switch (strengthLevel)
                    {
                        case MetricStrengthLevel.Strongest: return 0.06f;
                        case MetricStrengthLevel.Strong: return 0.04f;
                        case MetricStrengthLevel.Medium: return 0.02f;
                        case MetricStrengthLevel.Weak: return -0.08f;
                        default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
                    }

                case MetricTolerancePolicy.Broad:
                    switch (strengthLevel)
                    {
                        case MetricStrengthLevel.Strongest: return 0.03f;
                        case MetricStrengthLevel.Strong: return 0.03f;
                        case MetricStrengthLevel.Medium: return 0.02f;
                        case MetricStrengthLevel.Weak: return -0.01f;
                        default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
                    }

                case MetricTolerancePolicy.OffbeatFriendly:
                    switch (strengthLevel)
                    {
                        case MetricStrengthLevel.Strongest: return -0.01f;
                        case MetricStrengthLevel.Strong: return 0.02f;
                        case MetricStrengthLevel.Medium: return 0.06f;
                        case MetricStrengthLevel.Weak: return 0.05f;
                        default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(MetricTolerancePolicy), MetricTolerancePolicy, "Unknown metric tolerance policy.");
            }
        }

        private static float[] UniformSegmentWeights(int segmentCount)
        {
            int count = Math.Max(1, segmentCount);
            var weights = new float[count];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1f;

            return weights;
        }
    }

    internal static class ResponseBehaviourProfileFactory
    {
        public static ResponseBehaviourProfile Build(
            ResponsePlan plan,
            SkeletonBuilderConfig config,
            SkeletonDerivedMaps maps)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (maps == null)
                throw new ArgumentNullException(nameof(maps));

            int segmentCount = maps.SegmentSourceWeights != null && maps.SegmentSourceWeights.Length > 0
                ? maps.SegmentSourceWeights.Length
                : 1;
            ResponseBehaviourProfile profile = ResponseBehaviourProfile.Neutral(
                plan.ResponseType,
                Clamp01(plan.TargetDensity),
                segmentCount);

            profile.PreserveAnchors = plan.PreserveAnchors;
            profile.RequireStrongEnding = plan.MirrorEnding;
            profile.ComplementarityBias = Clamp01(plan.ComplementarityBias);

            switch (plan.ResponseType)
            {
                case ResponseType.Mirror:
                    ConfigureMirror(profile, plan);
                    break;

                case ResponseType.Simplify:
                    ConfigureSimplify(profile, plan);
                    break;

                case ResponseType.Complement:
                    ConfigureComplement(profile, plan);
                    break;

                case ResponseType.Contrast:
                    ConfigureContrast(profile, plan);
                    break;

                case ResponseType.Fill:
                    ConfigureFill(profile, plan);
                    break;

                case ResponseType.Intensify:
                    ConfigureIntensify(profile, plan);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(plan.ResponseType), plan.ResponseType, "Unknown response type.");
            }

            profile.SegmentTargetWeights = BuildSegmentTargetWeights(profile.SegmentTargetPolicy, maps);
            return profile;
        }

        private static void ConfigureMirror(ResponseBehaviourProfile profile, ResponsePlan plan)
        {
            profile.SourceOverlapPolicy = SourceOverlapPolicy.Preserve;
            profile.MetricTolerancePolicy = MetricTolerancePolicy.StrongAndMedium;
            profile.WeakStepPolicy = WeakStepPolicy.Defer;
            profile.EndingPolicy = plan.MirrorEnding ? EndingPolicy.MirrorSource : EndingPolicy.StructuralOnly;
            profile.SegmentTargetPolicy = SegmentTargetPolicy.FollowSource;
            profile.SourceHitWeight = 1.10f;
            profile.SourceGapWeight = -0.10f;
            profile.AdjacentSourceWeight = 0.10f;
            profile.NearSourceWeight = 0.02f;
            profile.FarSourceWeight = -0.08f;
            profile.InterstitialWeight = -0.04f;
            profile.DenseSourceRegionWeight = 0.16f;
            profile.SparseSourceRegionWeight = -0.05f;
            profile.SegmentInfluence = 0.08f;
            profile.DirectOverlapSoftLimit = 1f;
            profile.WeakStepSelectionPortion = 0.70f;
        }

        private static void ConfigureSimplify(ResponseBehaviourProfile profile, ResponsePlan plan)
        {
            profile.SourceOverlapPolicy = SourceOverlapPolicy.ReduceToAnchors;
            profile.MetricTolerancePolicy = MetricTolerancePolicy.SpineOnly;
            profile.WeakStepPolicy = WeakStepPolicy.Suppress;
            profile.EndingPolicy = plan.MirrorEnding ? EndingPolicy.MirrorSource : EndingPolicy.StructuralOnly;
            profile.SegmentTargetPolicy = SegmentTargetPolicy.StructuralSpine;
            profile.SourceHitWeight = 0.30f;
            profile.SourceGapWeight = -0.10f;
            profile.AdjacentSourceWeight = -0.04f;
            profile.NearSourceWeight = -0.06f;
            profile.FarSourceWeight = -0.04f;
            profile.InterstitialWeight = -0.16f;
            profile.DenseSourceRegionWeight = 0.08f;
            profile.SparseSourceRegionWeight = -0.08f;
            profile.SegmentInfluence = 0.10f;
            profile.DirectOverlapSoftLimit = 0.75f;
            profile.WeakStepSelectionPortion = 0.95f;
        }

        private static void ConfigureComplement(ResponseBehaviourProfile profile, ResponsePlan plan)
        {
            profile.SourceOverlapPolicy = SourceOverlapPolicy.PreferGaps;
            profile.MetricTolerancePolicy = MetricTolerancePolicy.StrongAndMedium;
            profile.WeakStepPolicy = WeakStepPolicy.Defer;
            profile.EndingPolicy = EndingPolicy.FillGap;
            profile.SegmentTargetPolicy = SegmentTargetPolicy.SparseSegments;
            profile.SourceHitWeight = -0.45f;
            profile.SourceGapWeight = 0.52f;
            profile.AdjacentSourceWeight = 0.06f;
            profile.NearSourceWeight = 0.02f;
            profile.FarSourceWeight = 0.18f;
            profile.InterstitialWeight = 0.08f;
            profile.DenseSourceRegionWeight = -0.10f;
            profile.SparseSourceRegionWeight = 0.18f;
            profile.SegmentInfluence = 0.12f;
            profile.DirectOverlapSoftLimit = 0.20f;
            profile.LimitDirectOverlap = true;
            profile.WeakStepSelectionPortion = 0.60f;
        }

        private static void ConfigureContrast(ResponseBehaviourProfile profile, ResponsePlan plan)
        {
            profile.SourceOverlapPolicy = SourceOverlapPolicy.Displace;
            profile.MetricTolerancePolicy = MetricTolerancePolicy.Broad;
            profile.WeakStepPolicy = WeakStepPolicy.Allow;
            profile.EndingPolicy = EndingPolicy.AvoidAccent;
            profile.SegmentTargetPolicy = SegmentTargetPolicy.InvertSource;
            profile.SourceHitWeight = -0.55f;
            profile.SourceGapWeight = 0.18f;
            profile.AdjacentSourceWeight = 0.42f;
            profile.NearSourceWeight = 0.24f;
            profile.FarSourceWeight = -0.02f;
            profile.InterstitialWeight = 0.12f;
            profile.DenseSourceRegionWeight = -0.12f;
            profile.SparseSourceRegionWeight = 0.08f;
            profile.SegmentInfluence = 0.16f;
            profile.DirectOverlapSoftLimit = 0.15f;
            profile.LimitDirectOverlap = true;
            profile.WeakStepSelectionPortion = 0.25f;
        }

        private static void ConfigureFill(ResponseBehaviourProfile profile, ResponsePlan plan)
        {
            profile.SourceOverlapPolicy = SourceOverlapPolicy.Interstitial;
            profile.MetricTolerancePolicy = MetricTolerancePolicy.OffbeatFriendly;
            profile.WeakStepPolicy = WeakStepPolicy.PreferInterstitial;
            profile.EndingPolicy = EndingPolicy.FillGap;
            profile.SegmentTargetPolicy = SegmentTargetPolicy.Interstitial;
            profile.SourceHitWeight = -0.22f;
            profile.SourceGapWeight = 0.34f;
            profile.AdjacentSourceWeight = 0.20f;
            profile.NearSourceWeight = 0.16f;
            profile.FarSourceWeight = -0.10f;
            profile.InterstitialWeight = 0.48f;
            profile.DenseSourceRegionWeight = 0.06f;
            profile.SparseSourceRegionWeight = 0.06f;
            profile.SegmentInfluence = 0.10f;
            profile.DirectOverlapSoftLimit = 0.35f;
            profile.LimitDirectOverlap = true;
            profile.GuaranteeInterstitial = true;
            profile.WeakStepSelectionPortion = 0f;
        }

        private static void ConfigureIntensify(ResponseBehaviourProfile profile, ResponsePlan plan)
        {
            profile.SourceOverlapPolicy = SourceOverlapPolicy.ExpandAroundSource;
            profile.MetricTolerancePolicy = MetricTolerancePolicy.Broad;
            profile.WeakStepPolicy = WeakStepPolicy.Defer;
            profile.EndingPolicy = EndingPolicy.LateDrive;
            profile.SegmentTargetPolicy = SegmentTargetPolicy.ActiveLateExpansion;
            profile.SourceHitWeight = 0.45f;
            profile.SourceGapWeight = 0.10f;
            profile.AdjacentSourceWeight = 0.34f;
            profile.NearSourceWeight = 0.22f;
            profile.FarSourceWeight = -0.08f;
            profile.InterstitialWeight = 0.12f;
            profile.DenseSourceRegionWeight = 0.22f;
            profile.SparseSourceRegionWeight = -0.04f;
            profile.SegmentInfluence = 0.14f;
            profile.DirectOverlapSoftLimit = 0.80f;
            profile.GuaranteeExpansion = true;
            profile.GuaranteeLateDrive = true;
            profile.WeakStepSelectionPortion = 0.45f;
        }

        private static float[] BuildSegmentTargetWeights(
            SegmentTargetPolicy policy,
            SkeletonDerivedMaps maps)
        {
            float[] sourceWeights = maps.SegmentSourceWeights;
            int segmentCount = sourceWeights != null && sourceWeights.Length > 0 ? sourceWeights.Length : 1;
            var weights = new float[segmentCount];
            float maxSourceWeight = GetMax(sourceWeights);

            for (int i = 0; i < segmentCount; i++)
            {
                float normalizedSource = maxSourceWeight > 0f ? sourceWeights[i] / maxSourceWeight : 0f;
                float inverseSource = 1f - normalizedSource;

                switch (policy)
                {
                    case SegmentTargetPolicy.FollowSource:
                        weights[i] = maxSourceWeight > 0f ? 0.75f + normalizedSource * 0.50f : 1f;
                        break;

                    case SegmentTargetPolicy.StructuralSpine:
                        weights[i] = maxSourceWeight > 0f
                            ? 0.65f + normalizedSource * 0.50f
                            : (i == 0 || i == segmentCount - 1 ? 1.08f : 0.92f);
                        break;

                    case SegmentTargetPolicy.SparseSegments:
                        weights[i] = maxSourceWeight > 0f ? 0.80f + inverseSource * 0.45f : 1f;
                        break;

                    case SegmentTargetPolicy.InvertSource:
                        weights[i] = maxSourceWeight > 0f ? 0.72f + inverseSource * 0.58f : (i % 2 == 0 ? 0.92f : 1.08f);
                        break;

                    case SegmentTargetPolicy.Interstitial:
                        weights[i] = maxSourceWeight > 0f
                            ? 0.92f + Math.Min(normalizedSource, inverseSource) * 0.40f
                            : 1f;
                        break;

                    case SegmentTargetPolicy.ActiveLateExpansion:
                        float lateBoost = segmentCount > 1 && i >= segmentCount - 2 ? 0.18f : 0f;
                        weights[i] = (maxSourceWeight > 0f ? 0.80f + normalizedSource * 0.40f : 1f) + lateBoost;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown segment target policy.");
                }
            }

            return weights;
        }

        private static float GetMax(float[] values)
        {
            if (values == null || values.Length == 0)
                return 0f;

            float max = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > max)
                    max = values[i];
            }

            return max;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }
    }
}
