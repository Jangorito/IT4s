using System;
using System.Collections.Generic;
using IT4s.Rhythm.ResponsePlanning.Models;

namespace IT4s.Rhythm.ResponsePlanning
{
    [Serializable]
    public sealed class ResponsePlannerDebugSnapshot
    {
        public ResponsePlannerDebugSnapshot()
            : this(
                new ResponsePlannerDescriptorSummary(),
                new ResponsePlannerNumericSummary(),
                Array.Empty<ResponseTypeScore>(),
                ResponseType.Mirror,
                new ResponsePlan())
        {
        }

        public ResponsePlannerDebugSnapshot(
            ResponsePlannerDescriptorSummary sourceDescriptorSummary,
            ResponsePlannerNumericSummary sourceNumericSummary,
            IReadOnlyList<ResponseTypeScore> perResponseTypeScores,
            ResponseType selectedResponseType,
            ResponsePlan finalPlan)
        {
            SourceDescriptorSummary = sourceDescriptorSummary ?? new ResponsePlannerDescriptorSummary();
            SourceNumericSummary = sourceNumericSummary ?? new ResponsePlannerNumericSummary();
            PerResponseTypeScores = CopyScores(perResponseTypeScores);
            SelectedResponseType = selectedResponseType;
            FinalPlan = finalPlan ?? new ResponsePlan();
        }

        public ResponsePlannerDescriptorSummary SourceDescriptorSummary { get; private set; }
        public ResponsePlannerNumericSummary SourceNumericSummary { get; private set; }
        public IReadOnlyList<ResponseTypeScore> PerResponseTypeScores { get; private set; }
        public ResponseType SelectedResponseType { get; private set; }
        public ResponsePlan FinalPlan { get; private set; }

        private static IReadOnlyList<ResponseTypeScore> CopyScores(IReadOnlyList<ResponseTypeScore> scores)
        {
            if (scores == null || scores.Count == 0)
                return Array.AsReadOnly(new ResponseTypeScore[0]);

            var copy = new ResponseTypeScore[scores.Count];
            for (int i = 0; i < scores.Count; i++)
            {
                ResponseTypeScore score = scores[i];
                copy[i] = score == null
                    ? new ResponseTypeScore()
                    : new ResponseTypeScore(score.ResponseType, score.Score);
            }

            return Array.AsReadOnly(copy);
        }
    }

    [Serializable]
    public sealed class ResponsePlannerDescriptorSummary
    {
        public ResponsePlannerDescriptorSummary()
            : this(string.Empty, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false)
        {
        }

        public ResponsePlannerDescriptorSummary(
            string summary,
            bool isSparse,
            bool isBalancedDensity,
            bool isBusy,
            bool isLowEnergy,
            bool isMediumEnergy,
            bool isHighEnergy,
            bool hasMeaningfulAnchors,
            bool hasStrongEnding,
            bool endingIsOpen,
            bool activityIsBackLoaded,
            bool activityIsFrontLoaded,
            bool activityIsBalanced,
            bool hasConversationalSpace,
            bool isPredictableProfile,
            bool isCongested)
        {
            Summary = summary ?? string.Empty;
            IsSparse = isSparse;
            IsBalancedDensity = isBalancedDensity;
            IsBusy = isBusy;
            IsLowEnergy = isLowEnergy;
            IsMediumEnergy = isMediumEnergy;
            IsHighEnergy = isHighEnergy;
            HasMeaningfulAnchors = hasMeaningfulAnchors;
            HasStrongEnding = hasStrongEnding;
            EndingIsOpen = endingIsOpen;
            ActivityIsBackLoaded = activityIsBackLoaded;
            ActivityIsFrontLoaded = activityIsFrontLoaded;
            ActivityIsBalanced = activityIsBalanced;
            HasConversationalSpace = hasConversationalSpace;
            IsPredictableProfile = isPredictableProfile;
            IsCongested = isCongested;
        }

        public string Summary { get; private set; }
        public bool IsSparse { get; private set; }
        public bool IsBalancedDensity { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsLowEnergy { get; private set; }
        public bool IsMediumEnergy { get; private set; }
        public bool IsHighEnergy { get; private set; }
        public bool HasMeaningfulAnchors { get; private set; }
        public bool HasStrongEnding { get; private set; }
        public bool EndingIsOpen { get; private set; }
        public bool ActivityIsBackLoaded { get; private set; }
        public bool ActivityIsFrontLoaded { get; private set; }
        public bool ActivityIsBalanced { get; private set; }
        public bool HasConversationalSpace { get; private set; }
        public bool IsPredictableProfile { get; private set; }
        public bool IsCongested { get; private set; }
    }

    [Serializable]
    public sealed class ResponsePlannerNumericSummary
    {
        public ResponsePlannerNumericSummary()
            : this(0f, 0f, 0, 0)
        {
        }

        public ResponsePlannerNumericSummary(
            float sourceDensity,
            float sourceEnergy,
            int anchorCount,
            int turnLengthSteps)
        {
            SourceDensity = sourceDensity;
            SourceEnergy = sourceEnergy;
            AnchorCount = anchorCount;
            TurnLengthSteps = turnLengthSteps;
        }

        public float SourceDensity { get; private set; }
        public float SourceEnergy { get; private set; }
        public int AnchorCount { get; private set; }
        public int TurnLengthSteps { get; private set; }
    }

    [Serializable]
    public sealed class ResponseTypeScore
    {
        public ResponseTypeScore()
            : this(ResponseType.Mirror, 0f)
        {
        }

        public ResponseTypeScore(ResponseType responseType, float score)
        {
            ResponseType = responseType;
            Score = score;
        }

        public ResponseType ResponseType { get; private set; }
        public float Score { get; private set; }
    }
}
