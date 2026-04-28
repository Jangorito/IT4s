namespace IT4s.Rhythm.ResponsePlanning
{
    internal sealed class PlanningContext
    {
        public PlanningContext(
            float sourceDensity,
            float sourceEnergy,
            int anchorCount,
            bool hasMeaningfulAnchors,
            bool hasStrongEnding,
            bool endingIsOpen,
            bool activityIsBackLoaded,
            bool activityIsFrontLoaded,
            bool activityIsBalanced,
            bool isSparse,
            bool isBusy,
            bool isLowEnergy,
            bool isHighEnergy,
            bool hasConversationalSpace,
            bool isCongested,
            bool isPressurised,
            bool isPredictableProfile,
            int turnLengthSteps)
        {
            SourceDensity = sourceDensity;
            SourceEnergy = sourceEnergy;
            AnchorCount = anchorCount;
            HasMeaningfulAnchors = hasMeaningfulAnchors;
            HasStrongEnding = hasStrongEnding;
            EndingIsOpen = endingIsOpen;
            ActivityIsBackLoaded = activityIsBackLoaded;
            ActivityIsFrontLoaded = activityIsFrontLoaded;
            ActivityIsBalanced = activityIsBalanced;
            IsSparse = isSparse;
            IsBusy = isBusy;
            IsLowEnergy = isLowEnergy;
            IsHighEnergy = isHighEnergy;
            HasConversationalSpace = hasConversationalSpace;
            IsCongested = isCongested;
            IsPressurised = isPressurised;
            IsPredictableProfile = isPredictableProfile;
            TurnLengthSteps = turnLengthSteps;
        }

        public float SourceDensity { get; }
        public float SourceEnergy { get; }
        public int AnchorCount { get; }
        public bool HasMeaningfulAnchors { get; }
        public bool HasStrongEnding { get; }
        public bool EndingIsOpen { get; }
        public bool ActivityIsBackLoaded { get; }
        public bool ActivityIsFrontLoaded { get; }
        public bool ActivityIsBalanced { get; }
        public bool IsSparse { get; }
        public bool IsBalancedDensity => !IsSparse && !IsBusy;
        public bool IsBusy { get; }
        public bool IsLowEnergy { get; }
        public bool IsMediumEnergy => !IsLowEnergy && !IsHighEnergy;
        public bool IsHighEnergy { get; }
        public bool HasConversationalSpace { get; }
        public bool HasMeaningfulGaps => HasConversationalSpace;
        public bool IsCongested { get; }
        public bool IsPressurised { get; }
        public bool IsPredictableProfile { get; }
        public int TurnLengthSteps { get; }
    }
}
