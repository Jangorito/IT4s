using System;
using System.Collections.Generic;
using IT4s.Data;

namespace IT4s.Diagnostics.Temporary
{
    internal enum SyntheticDatasetGenerationMode
    {
        BaseOnly,
        BasePlusVariants
    }

    internal enum SyntheticVariantKind
    {
        Base,
        Variant
    }

    internal enum SyntheticStructureType
    {
        Even,
        FrontLoaded,
        BackLoaded,
        Clustered,
        AnchorLed
    }

    internal enum SyntheticEnergyProfile
    {
        Flat,
        Increasing,
        AccentedEnding
    }

    internal sealed class SyntheticTurnCase
    {
        public SyntheticTurnCase(
            string caseId,
            SyntheticStructureType structureType,
            SyntheticEnergyProfile energyProfile,
            int nominalHitCount,
            SyntheticVariantKind variantKind,
            string parentCaseId,
            int? variantSeed)
        {
            CaseId = caseId ?? string.Empty;
            StructureType = structureType;
            EnergyProfile = energyProfile;
            NominalHitCount = nominalHitCount;
            VariantKind = variantKind;
            ParentCaseId = parentCaseId ?? string.Empty;
            VariantSeed = variantSeed;
        }

        public string CaseId { get; private set; }
        public SyntheticStructureType StructureType { get; private set; }
        public SyntheticEnergyProfile EnergyProfile { get; private set; }
        public int NominalHitCount { get; private set; }
        public SyntheticVariantKind VariantKind { get; private set; }
        public string ParentCaseId { get; private set; }
        public int? VariantSeed { get; private set; }
    }

    internal static class SyntheticPatternTurnDataset
    {
        private static readonly int[] HitCountBuckets = { 8, 12, 16, 20, 24, 28, 32, 36, 40, 44 };
        private static readonly SyntheticStructureType[] StructureTypes =
        {
            SyntheticStructureType.Even,
            SyntheticStructureType.FrontLoaded,
            SyntheticStructureType.BackLoaded,
            SyntheticStructureType.Clustered,
            SyntheticStructureType.AnchorLed
        };

        private static readonly SyntheticEnergyProfile[] EnergyProfiles =
        {
            SyntheticEnergyProfile.Flat,
            SyntheticEnergyProfile.Increasing,
            SyntheticEnergyProfile.AccentedEnding
        };

        public static IReadOnlyList<SyntheticTurnCase> Generate(SyntheticDatasetGenerationMode generationMode)
        {
            var cases = new List<SyntheticTurnCase>();

            for (int hitIndex = 0; hitIndex < HitCountBuckets.Length; hitIndex++)
            {
                int hitCount = HitCountBuckets[hitIndex];

                for (int structureIndex = 0; structureIndex < StructureTypes.Length; structureIndex++)
                {
                    SyntheticStructureType structureType = StructureTypes[structureIndex];

                    for (int energyIndex = 0; energyIndex < EnergyProfiles.Length; energyIndex++)
                    {
                        SyntheticEnergyProfile energyProfile = EnergyProfiles[energyIndex];
                        string baseCaseId = BuildCaseId("Base", hitCount, structureType, energyProfile);

                        cases.Add(new SyntheticTurnCase(
                            baseCaseId,
                            structureType,
                            energyProfile,
                            hitCount,
                            SyntheticVariantKind.Base,
                            string.Empty,
                            null));

                        if (generationMode != SyntheticDatasetGenerationMode.BasePlusVariants)
                            continue;

                        int seed = SyntheticSeedUtility.PositiveFromString(baseCaseId);
                        string variantCaseId = BuildCaseId("Variant", hitCount, structureType, energyProfile);

                        cases.Add(new SyntheticTurnCase(
                            variantCaseId,
                            structureType,
                            energyProfile,
                            hitCount,
                            SyntheticVariantKind.Variant,
                            baseCaseId,
                            seed));
                    }
                }
            }

            return Array.AsReadOnly(cases.ToArray());
        }

        private static string BuildCaseId(
            string prefix,
            int hitCount,
            SyntheticStructureType structureType,
            SyntheticEnergyProfile energyProfile)
        {
            return prefix + "_H" + hitCount.ToString("00") + "_" + structureType + "_" + energyProfile;
        }
    }

    internal static class SyntheticPatternTurnFactory
    {
        private const int StepCount = 96;
        private const int StepsPerQuarter = 12;
        private const int SampleRate = 44100;
        private const float Bpm = 120f;
        private const int FinalQuarterStart = 72;

        private static readonly int[] AnchorLedAnchors = { 0, 48, 95 };

        public static PatternTurn CreatePatternTurn(SyntheticTurnCase syntheticCase)
        {
            if (syntheticCase == null)
                throw new ArgumentNullException(nameof(syntheticCase));

            int[] velocity = CreateVelocityGrid(syntheticCase);

            return new PatternTurn
            {
                turnId = SyntheticSeedUtility.PositiveFromString(syntheticCase.CaseId),
                bpm = Bpm,
                stepsPerQuarter = StepsPerQuarter,
                sampleRate = SampleRate,
                startSamples = 0,
                endSamples = CalculateEndSamples(),
                velocity = velocity,
                offsetSamples = new int[StepCount]
            };
        }

        private static int[] CreateVelocityGrid(SyntheticTurnCase syntheticCase)
        {
            List<int> activeSteps = GenerateBaseActiveSteps(
                syntheticCase.StructureType,
                syntheticCase.NominalHitCount);

            if (syntheticCase.VariantKind == SyntheticVariantKind.Variant)
                activeSteps = CreateVariantActiveSteps(activeSteps, syntheticCase);

            var velocity = new int[StepCount];
            AssignVelocities(velocity, activeSteps, syntheticCase);
            return velocity;
        }

        private static List<int> GenerateBaseActiveSteps(
            SyntheticStructureType structureType,
            int hitCount)
        {
            hitCount = Clamp(hitCount, 0, StepCount);

            switch (structureType)
            {
                case SyntheticStructureType.Even:
                    return GenerateEven(hitCount, 0, StepCount - 1);
                case SyntheticStructureType.FrontLoaded:
                    return GenerateFrontLoaded(hitCount);
                case SyntheticStructureType.BackLoaded:
                    return GenerateBackLoaded(hitCount);
                case SyntheticStructureType.Clustered:
                    return GenerateClustered(hitCount);
                case SyntheticStructureType.AnchorLed:
                    return GenerateAnchorLed(hitCount);
                default:
                    throw new ArgumentOutOfRangeException(nameof(structureType), structureType, "Unknown structure type.");
            }
        }

        private static List<int> GenerateEven(int hitCount, int minInclusive, int maxInclusive)
        {
            var active = new HashSet<int>();
            AddEvenlyDistributed(active, hitCount, minInclusive, maxInclusive);
            return ToSortedList(active);
        }

        private static List<int> GenerateFrontLoaded(int hitCount)
        {
            var active = new HashSet<int>();
            int firstHalfCount = Clamp(
                (int)Math.Round(hitCount * 0.68f, MidpointRounding.AwayFromZero),
                1,
                hitCount - 1);

            AddEvenlyDistributed(active, firstHalfCount, 0, 47);
            AddEvenlyDistributed(active, hitCount - firstHalfCount, 48, StepCount - 1);
            return ToSortedList(active);
        }

        private static List<int> GenerateBackLoaded(int hitCount)
        {
            var active = new HashSet<int>();
            int firstHalfCount = Clamp(
                (int)Math.Round(hitCount * 0.32f, MidpointRounding.AwayFromZero),
                1,
                hitCount - 1);

            AddEvenlyDistributed(active, firstHalfCount, 0, 47);
            AddEvenlyDistributed(active, hitCount - firstHalfCount, 48, StepCount - 1);
            return ToSortedList(active);
        }

        private static List<int> GenerateClustered(int hitCount)
        {
            var active = new HashSet<int>();
            int[] centers = GetClusterCenters(hitCount);
            int[] clusterCounts = DistributeCount(hitCount, centers.Length);
            int radius = GetClusterRadius(centers.Length);

            for (int i = 0; i < centers.Length; i++)
            {
                AddCluster(active, centers[i], clusterCounts[i], radius);
            }

            return ToSortedList(active);
        }

        private static List<int> GenerateAnchorLed(int hitCount)
        {
            var active = new HashSet<int>();

            for (int i = 0; i < AnchorLedAnchors.Length && active.Count < hitCount; i++)
                active.Add(AnchorLedAnchors[i]);

            for (int radius = 1; radius < StepCount && active.Count < hitCount; radius++)
            {
                AddIfAvailable(active, AnchorLedAnchors[0] + radius, hitCount);
                AddIfAvailable(active, AnchorLedAnchors[1] - radius, hitCount);
                AddIfAvailable(active, AnchorLedAnchors[1] + radius, hitCount);
                AddIfAvailable(active, AnchorLedAnchors[2] - radius, hitCount);
            }

            return ToSortedList(active);
        }

        private static void AddEvenlyDistributed(
            HashSet<int> active,
            int count,
            int minInclusive,
            int maxInclusive)
        {
            if (count <= 0)
                return;

            if (count == 1)
            {
                AddUniqueNearest(active, (minInclusive + maxInclusive) / 2, minInclusive, maxInclusive);
                return;
            }

            float range = maxInclusive - minInclusive;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                int preferred = (int)Math.Round(minInclusive + (range * t), MidpointRounding.AwayFromZero);
                AddUniqueNearest(active, preferred, minInclusive, maxInclusive);
            }
        }

        private static void AddCluster(
            HashSet<int> active,
            int center,
            int count,
            int radius)
        {
            int min = Math.Max(0, center - radius);
            int max = Math.Min(StepCount - 1, center + radius);
            int added = 0;

            if (AddUniqueNearest(active, center, min, max))
                added++;

            for (int distance = 1; added < count && distance <= radius; distance++)
            {
                if (AddUniqueNearest(active, center - distance, min, max))
                    added++;

                if (added >= count)
                    break;

                if (AddUniqueNearest(active, center + distance, min, max))
                    added++;
            }
        }

        private static List<int> CreateVariantActiveSteps(
            IReadOnlyList<int> baseActiveSteps,
            SyntheticTurnCase syntheticCase)
        {
            var active = new HashSet<int>();
            var ordered = new List<int>(baseActiveSteps.Count);

            for (int i = 0; i < baseActiveSteps.Count; i++)
            {
                active.Add(baseActiveSteps[i]);
                ordered.Add(baseActiveSteps[i]);
            }

            int seed = syntheticCase.VariantSeed.HasValue
                ? syntheticCase.VariantSeed.Value
                : SyntheticSeedUtility.PositiveFromString(syntheticCase.CaseId);
            var random = new Random(seed);
            int editCount = Math.Max(1, Math.Min(5, Math.Max(1, syntheticCase.NominalHitCount / 8)));

            for (int edit = 0; edit < editCount; edit++)
            {
                TryApplyLocalStepEdit(active, ordered, syntheticCase, random);
            }

            ordered.Sort();
            return ordered;
        }

        private static void TryApplyLocalStepEdit(
            HashSet<int> active,
            List<int> ordered,
            SyntheticTurnCase syntheticCase,
            Random random)
        {
            if (ordered.Count == 0)
                return;

            for (int attempt = 0; attempt < 16; attempt++)
            {
                int index = random.Next(ordered.Count);
                int oldStep = ordered[index];

                if (IsProtectedAnchor(syntheticCase.StructureType, oldStep))
                    continue;

                GetVariantAllowedRange(
                    syntheticCase.StructureType,
                    syntheticCase.NominalHitCount,
                    oldStep,
                    out int min,
                    out int max);

                int candidate = oldStep + PickSmallShift(random);
                if (candidate < min || candidate > max)
                    continue;

                if (active.Contains(candidate))
                    continue;

                active.Remove(oldStep);
                active.Add(candidate);
                ordered[index] = candidate;
                return;
            }
        }

        private static int PickSmallShift(Random random)
        {
            switch (random.Next(4))
            {
                case 0:
                    return -2;
                case 1:
                    return -1;
                case 2:
                    return 1;
                default:
                    return 2;
            }
        }

        private static void GetVariantAllowedRange(
            SyntheticStructureType structureType,
            int hitCount,
            int oldStep,
            out int min,
            out int max)
        {
            min = 0;
            max = StepCount - 1;

            switch (structureType)
            {
                case SyntheticStructureType.FrontLoaded:
                case SyntheticStructureType.BackLoaded:
                    if (oldStep < StepCount / 2)
                    {
                        min = 0;
                        max = 47;
                    }
                    else
                    {
                        min = 48;
                        max = StepCount - 1;
                    }
                    break;

                case SyntheticStructureType.Clustered:
                    int center = GetNearestClusterCenter(hitCount, oldStep);
                    int radius = GetClusterRadius(GetClusterCenters(hitCount).Length);
                    min = Math.Max(0, center - radius);
                    max = Math.Min(StepCount - 1, center + radius);
                    break;

                case SyntheticStructureType.AnchorLed:
                    if (oldStep < 32)
                    {
                        min = 0;
                        max = 31;
                    }
                    else if (oldStep < 64)
                    {
                        min = 32;
                        max = 63;
                    }
                    else
                    {
                        min = 64;
                        max = StepCount - 1;
                    }
                    break;
            }
        }

        private static void AssignVelocities(
            int[] velocity,
            IReadOnlyList<int> activeSteps,
            SyntheticTurnCase syntheticCase)
        {
            var sortedSteps = new List<int>(activeSteps);
            sortedSteps.Sort();

            Random random = null;
            if (syntheticCase.VariantKind == SyntheticVariantKind.Variant)
            {
                int seed = syntheticCase.VariantSeed.HasValue
                    ? syntheticCase.VariantSeed.Value
                    : SyntheticSeedUtility.PositiveFromString(syntheticCase.CaseId);
                random = new Random(seed ^ 0x2A53B91);
            }

            for (int i = 0; i < sortedSteps.Count; i++)
            {
                int step = sortedSteps[i];
                int value = BaseVelocityFor(step, syntheticCase.EnergyProfile);

                if (syntheticCase.StructureType == SyntheticStructureType.AnchorLed &&
                    IsAnchorLedAnchor(step))
                {
                    value += 16;
                }

                if (random != null)
                    value += VelocityJitter(random, syntheticCase.EnergyProfile);

                velocity[step] = ClampVelocity(value, step, syntheticCase.EnergyProfile);
            }
        }

        private static int BaseVelocityFor(int step, SyntheticEnergyProfile energyProfile)
        {
            switch (energyProfile)
            {
                case SyntheticEnergyProfile.Flat:
                    return 84;
                case SyntheticEnergyProfile.Increasing:
                    return 70 + (int)Math.Round(38f * step / (StepCount - 1), MidpointRounding.AwayFromZero);
                case SyntheticEnergyProfile.AccentedEnding:
                    return step >= FinalQuarterStart ? 110 : 84;
                default:
                    throw new ArgumentOutOfRangeException(nameof(energyProfile), energyProfile, "Unknown energy profile.");
            }
        }

        private static int VelocityJitter(Random random, SyntheticEnergyProfile energyProfile)
        {
            switch (energyProfile)
            {
                case SyntheticEnergyProfile.Flat:
                    return random.Next(-4, 5);
                case SyntheticEnergyProfile.Increasing:
                    return random.Next(-3, 4);
                case SyntheticEnergyProfile.AccentedEnding:
                    return random.Next(-4, 5);
                default:
                    throw new ArgumentOutOfRangeException(nameof(energyProfile), energyProfile, "Unknown energy profile.");
            }
        }

        private static int ClampVelocity(int value, int step, SyntheticEnergyProfile energyProfile)
        {
            switch (energyProfile)
            {
                case SyntheticEnergyProfile.Flat:
                    return Clamp(value, 70, 110);
                case SyntheticEnergyProfile.Increasing:
                    return Clamp(value, 65, 127);
                case SyntheticEnergyProfile.AccentedEnding:
                    if (step >= FinalQuarterStart)
                        return Clamp(value, 98, 127);

                    return Clamp(value, 70, 105);
                default:
                    throw new ArgumentOutOfRangeException(nameof(energyProfile), energyProfile, "Unknown energy profile.");
            }
        }

        private static int[] GetClusterCenters(int hitCount)
        {
            if (hitCount <= 10)
                return new[] { 20, 72 };

            if (hitCount <= 24)
                return new[] { 14, 48, 82 };

            return new[] { 10, 34, 62, 86 };
        }

        private static int GetClusterRadius(int clusterCount)
        {
            switch (clusterCount)
            {
                case 2:
                    return 10;
                case 3:
                    return 8;
                default:
                    return 7;
            }
        }

        private static int GetNearestClusterCenter(int hitCount, int step)
        {
            int[] centers = GetClusterCenters(hitCount);
            int nearest = centers[0];
            int nearestDistance = Math.Abs(step - nearest);

            for (int i = 1; i < centers.Length; i++)
            {
                int distance = Math.Abs(step - centers[i]);
                if (distance < nearestDistance)
                {
                    nearest = centers[i];
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static int[] DistributeCount(int totalCount, int bucketCount)
        {
            var counts = new int[bucketCount];
            int baseCount = totalCount / bucketCount;
            int remainder = totalCount % bucketCount;

            for (int i = 0; i < counts.Length; i++)
                counts[i] = baseCount + (i < remainder ? 1 : 0);

            return counts;
        }

        private static bool AddUniqueNearest(
            HashSet<int> active,
            int preferred,
            int minInclusive,
            int maxInclusive)
        {
            preferred = Clamp(preferred, minInclusive, maxInclusive);

            if (!active.Contains(preferred))
            {
                active.Add(preferred);
                return true;
            }

            int range = maxInclusive - minInclusive;
            for (int distance = 1; distance <= range; distance++)
            {
                int lower = preferred - distance;
                if (lower >= minInclusive && !active.Contains(lower))
                {
                    active.Add(lower);
                    return true;
                }

                int upper = preferred + distance;
                if (upper <= maxInclusive && !active.Contains(upper))
                {
                    active.Add(upper);
                    return true;
                }
            }

            return false;
        }

        private static void AddIfAvailable(HashSet<int> active, int step, int targetCount)
        {
            if (active.Count >= targetCount)
                return;

            if (step < 0 || step >= StepCount)
                return;

            active.Add(step);
        }

        private static List<int> ToSortedList(HashSet<int> active)
        {
            var sorted = new List<int>(active);
            sorted.Sort();
            return sorted;
        }

        private static bool IsProtectedAnchor(SyntheticStructureType structureType, int step)
        {
            return structureType == SyntheticStructureType.AnchorLed && IsAnchorLedAnchor(step);
        }

        private static bool IsAnchorLedAnchor(int step)
        {
            for (int i = 0; i < AnchorLedAnchors.Length; i++)
            {
                if (AnchorLedAnchors[i] == step)
                    return true;
            }

            return false;
        }

        private static long CalculateEndSamples()
        {
            double quarterDurationSeconds = 60.0 / Bpm;
            double stepDurationSeconds = quarterDurationSeconds / StepsPerQuarter;
            return (long)Math.Round(StepCount * stepDurationSeconds * SampleRate, MidpointRounding.AwayFromZero);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }

    internal static class SyntheticSeedUtility
    {
        public static int PositiveFromString(string value)
        {
            unchecked
            {
                const int offsetBasis = (int)2166136261;
                const int fnvPrime = 16777619;
                int hash = offsetBasis;
                string safeValue = value ?? string.Empty;

                for (int i = 0; i < safeValue.Length; i++)
                {
                    hash ^= safeValue[i];
                    hash *= fnvPrime;
                }

                return hash & 0x7fffffff;
            }
        }
    }
}
