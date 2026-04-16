using System.IO;
using UnityEditor;
using UnityEngine;

namespace IT4s.Diagnostics.Temporary
{
    public static class TemporaryTurnAnalysisPlannerBatchMenu
    {
        private const string MenuRoot = "Tools/IT4s/Diagnostics/Turn Analysis Planner Batch/";

        [MenuItem(MenuRoot + "Run Base + Variants", false, 1000)]
        public static void RunBasePlusVariants()
        {
            Run(SyntheticDatasetGenerationMode.BasePlusVariants);
        }

        [MenuItem(MenuRoot + "Run Base Only", false, 1001)]
        public static void RunBaseOnly()
        {
            Run(SyntheticDatasetGenerationMode.BaseOnly);
        }

        private static void Run(SyntheticDatasetGenerationMode generationMode)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDirectory = Path.Combine(projectRoot, "DiagnosticsOutput", "TurnAnalysisResponsePlanner");

            var runner = new TemporaryTurnAnalysisPlannerBatchRunner();
            TemporaryTurnAnalysisPlannerBatchResult result = runner.Run(generationMode, outputDirectory);

            Debug.Log(
                "[TemporaryTurnAnalysisPlannerBatch] " +
                "Wrote " + result.TotalRows + " rows " +
                "(base " + result.BaseCaseCount + ", variants " + result.VariantCaseCount + ", " +
                "variant mode " + (result.VariantModeEnabled ? "enabled" : "disabled") + ") " +
                "to " + result.CsvPath);
        }
    }
}
