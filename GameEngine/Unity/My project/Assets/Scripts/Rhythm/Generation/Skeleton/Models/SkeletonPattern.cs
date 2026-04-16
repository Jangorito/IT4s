using System;

namespace IT4s.Rhythm.Generation.Skeleton.Models
{
    [Serializable]
    public sealed class SkeletonPattern
    {
        public int TurnLengthSteps { get; private set; }
        public bool[] ActiveSteps { get; private set; }
        public float[] SelectionScores { get; private set; }
        public SkeletonStepMeta[] StepMeta { get; private set; }
        public int[] SelectedStepIndices { get; private set; }
        public SkeletonPatternSummary Summary { get; private set; }

        public SkeletonPattern(
            int turnLengthSteps,
            bool[] activeSteps,
            float[] selectionScores,
            SkeletonStepMeta[] stepMeta,
            int[] selectedStepIndices,
            SkeletonPatternSummary summary)
        {
            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            if (activeSteps == null)
                throw new ArgumentNullException(nameof(activeSteps));

            if (selectionScores == null)
                throw new ArgumentNullException(nameof(selectionScores));

            if (stepMeta == null)
                throw new ArgumentNullException(nameof(stepMeta));

            if (selectedStepIndices == null)
                throw new ArgumentNullException(nameof(selectedStepIndices));

            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            if (activeSteps.Length != turnLengthSteps)
                throw new ArgumentException("Active step map length must match turn length.", nameof(activeSteps));

            if (selectionScores.Length != turnLengthSteps)
                throw new ArgumentException("Selection score map length must match turn length.", nameof(selectionScores));

            if (stepMeta.Length != turnLengthSteps)
                throw new ArgumentException("Step metadata length must match turn length.", nameof(stepMeta));

            TurnLengthSteps = turnLengthSteps;
            ActiveSteps = activeSteps;
            SelectionScores = selectionScores;
            StepMeta = stepMeta;
            SelectedStepIndices = selectedStepIndices;
            Summary = summary;
        }
    }
}
