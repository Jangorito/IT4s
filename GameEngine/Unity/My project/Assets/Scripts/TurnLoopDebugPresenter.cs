using System;
using System.Collections;
using System.IO;
using IT4s.Data;
using IT4s.Orchestration;
using IT4s.Rhythm.TurnAnalysis.Models;
using UnityEngine;

namespace IT4s.Debugging
{
    /// <summary>
    /// Connects TurnLoopController observability events to lightweight debug renderers.
    /// This keeps the orchestration layer free of UI logic while still giving the scene a
    /// structured, inspectable view of the rhythmic pipeline.
    /// </summary>
    public class TurnLoopDebugPresenter : MonoBehaviour
    {
        private const float PanelPadding = 16f;
        private const float SectionSpacing = 12f;
        private const float SummaryHeight = 96f;
        private const float SplitPanelSpacing = 16f;
        private const float AnalysisTitleHeight = 22f;
        private const float AnalysisMetaHeight = 18f;
        private const float AnalysisTopSpacing = 8f;
        private const float MinimumAnalysisHeight = 140f;
        private const string AnalysisPanelTitle = "TURN ANALYSIS";

        [Header("Sources")]
        [SerializeField] private TurnLoopController turnLoopController;

        [Header("Renderers")]
        [SerializeField] private PatternTurnDebugRenderer humanPatternRenderer;
        [SerializeField] private PatternTurnDebugRenderer aiPatternRenderer;
        [SerializeField] private ResponsePlannerDebugRenderer responsePlannerRenderer;
        [SerializeField] private SkeletonDebugRenderer skeletonRenderer;

        [Header("Layout")]
        [SerializeField] private bool showDebugUi = true;
        [SerializeField] private bool showResponsePlannerPanel = true;
        [SerializeField] private bool showSkeletonPanel = true;
        [SerializeField] private bool showAiPatternPanel = false;
        [SerializeField] private float maxPanelWidth = 980f;
        [SerializeField] private Vector2 screenPadding = new Vector2(16f, 16f);

        [Header("Export")]
        [SerializeField] private KeyCode exportHumanPanelKey = KeyCode.P;
        [SerializeField, Min(1)] private int exportSuperSize = 4;
        [SerializeField] private string exportFolderName = "DiagnosticsOutput/DebugPanelExports";

        private TurnWindow lastTurnWindow;
        private bool hasLastTurnWindow;
        private PatternTurn lastHumanPattern;
        private PatternTurn lastAiPattern;
        private TurnAnalysisResult lastAnalysisResult;
        private string lastAnalysisSummary = string.Empty;
        private TurnPhase currentPhase = TurnPhase.Transition;
        private bool exportInProgress;

        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle placeholderStyle;
        private GUIStyle analysisTextStyle;

        public TurnPhase CurrentPhase => currentPhase;
        public bool HasLastTurnWindow => hasLastTurnWindow;
        public TurnWindow LastTurnWindow => lastTurnWindow;
        public PatternTurn LastHumanPattern => lastHumanPattern;
        public PatternTurn LastAiPattern => lastAiPattern;
        public TurnAnalysisResult LastAnalysisResult => lastAnalysisResult;

        private void Awake()
        {
            AutoResolveReferences();

            humanPatternRenderer?.SetPanelTitle("HUMAN PATTERN");
            aiPatternRenderer?.SetPanelTitle("AI PATTERN");
            responsePlannerRenderer?.SetTurnLoopController(turnLoopController);
            skeletonRenderer?.SetTurnLoopController(turnLoopController);
        }

        private void OnEnable()
        {
            Subscribe();
            PullExistingControllerState();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (exportInProgress || !UnityEngine.Input.GetKeyDown(exportHumanPanelKey))
            {
                return;
            }

            TryBeginHumanPatternExport();
        }

        public void OnHumanTurnCaptured(TurnWindow window)
        {
            lastTurnWindow = window;
            hasLastTurnWindow = true;
        }

        public void OnPatternCompiled(PatternTurn pattern)
        {
            lastHumanPattern = pattern;
            humanPatternRenderer?.SetPattern(pattern);
            ClearAnalysisPresentation();
        }

        public void OnAiPatternGenerated(PatternTurn pattern)
        {
            lastAiPattern = pattern;
            aiPatternRenderer?.SetPattern(pattern);
        }

        public void OnHumanTurnAnalysed(TurnAnalysisResult analysis)
        {
            lastAnalysisResult = analysis;
            lastAnalysisSummary = TurnAnalysisSummaryFormatter.Format(analysis);
        }

        public void OnPhaseChanged(TurnPhase phase)
        {
            currentPhase = phase;
        }

        private void OnGUI()
        {
            if (!showDebugUi)
            {
                return;
            }

            EnsureStyles();

            float width = Mathf.Min(maxPanelWidth, Screen.width - (screenPadding.x * 2f));
            float x = screenPadding.x;
            float y = screenPadding.y;

            Rect summaryRect = new Rect(x, y, width, SummaryHeight);
            DrawSummary(summaryRect);
            y += SummaryHeight + SectionSpacing;

            float panelWidth = (width - SplitPanelSpacing) * 0.5f;
            float humanHeight = humanPatternRenderer != null
                ? humanPatternRenderer.GetPreferredHeight(panelWidth)
                : 180f;
            float analysisHeight = GetAnalysisPreferredHeight(panelWidth);
            float comparisonHeight = Mathf.Max(humanHeight, analysisHeight);

            Rect humanRect = new Rect(x, y, panelWidth, comparisonHeight);
            Rect analysisRect = new Rect(x + panelWidth + SplitPanelSpacing, y, panelWidth, comparisonHeight);
            DrawPatternPanel(humanRect, humanPatternRenderer, "HUMAN PATTERN");
            DrawAnalysisPanel(analysisRect);
            y += comparisonHeight + SectionSpacing;

            bool drawPlannerPanel = showResponsePlannerPanel && responsePlannerRenderer != null;
            bool drawSkeletonPanel = showSkeletonPanel && skeletonRenderer != null;

            if (drawPlannerPanel && drawSkeletonPanel)
            {
                float lowerPanelWidth = (width - SplitPanelSpacing) * 0.5f;
                float plannerHeight = responsePlannerRenderer.GetPreferredHeight(lowerPanelWidth);
                float skeletonHeight = skeletonRenderer.GetPreferredHeight(lowerPanelWidth);
                float lowerRowHeight = Mathf.Max(plannerHeight, skeletonHeight);

                Rect plannerRect = new Rect(x, y, lowerPanelWidth, lowerRowHeight);
                Rect skeletonRect = new Rect(x + lowerPanelWidth + SplitPanelSpacing, y, lowerPanelWidth, lowerRowHeight);
                responsePlannerRenderer.Draw(plannerRect);
                skeletonRenderer.Draw(skeletonRect);
                y += lowerRowHeight + SectionSpacing;
            }
            else if (drawPlannerPanel)
            {
                float plannerHeight = responsePlannerRenderer.GetPreferredHeight(width);
                Rect plannerRect = new Rect(x, y, width, plannerHeight);
                responsePlannerRenderer.Draw(plannerRect);
                y += plannerHeight + SectionSpacing;
            }
            else if (drawSkeletonPanel)
            {
                float skeletonHeight = skeletonRenderer.GetPreferredHeight(width);
                Rect skeletonRect = new Rect(x, y, width, skeletonHeight);
                skeletonRenderer.Draw(skeletonRect);
                y += skeletonHeight + SectionSpacing;
            }

            if (!showAiPatternPanel)
            {
                return;
            }

            float aiHeight = aiPatternRenderer != null
                ? aiPatternRenderer.GetPreferredHeight(width)
                : 180f;
            Rect aiRect = new Rect(x, y, width, aiHeight);
            DrawPatternPanel(aiRect, aiPatternRenderer, "AI PATTERN");
        }

        private void DrawSummary(Rect rect)
        {
            DrawFilledRect(rect, new Color(0.10f, 0.10f, 0.10f, 0.94f));
            DrawOutline(rect, new Color(0.75f, 0.75f, 0.75f, 1f));

            Rect contentRect = new Rect(
                rect.x + PanelPadding,
                rect.y + PanelPadding,
                rect.width - (PanelPadding * 2f),
                rect.height - (PanelPadding * 2f));

            GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 22f), "TURN LOOP DEBUG", headingStyle);

            float y = contentRect.y + 24f;
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, 18f), $"Phase: {currentPhase}", bodyStyle);
            y += 20f;

            if (hasLastTurnWindow)
            {
                GUI.Label(
                    new Rect(contentRect.x, y, contentRect.width, 18f),
                    $"Last TurnWindow: id={lastTurnWindow.turnId} | hits={lastTurnWindow.HitCount} | duration={lastTurnWindow.DurationSamples} samples",
                    bodyStyle);
            }
            else
            {
                GUI.Label(new Rect(contentRect.x, y, contentRect.width, 18f), "Last TurnWindow: none captured yet", bodyStyle);
            }

            y += 20f;

            if (turnLoopController != null && turnLoopController.CurrentMusicalTiming.IsValid(out _))
            {
                GUI.Label(
                    new Rect(contentRect.x, y, contentRect.width, 18f),
                    $"Timing: {turnLoopController.CurrentMusicalTiming}",
                    bodyStyle);
            }
        }

        private void DrawAnalysisPanel(Rect rect)
        {
            DrawFilledRect(rect, new Color(0.10f, 0.10f, 0.10f, 0.94f));
            DrawOutline(rect, new Color(0.75f, 0.75f, 0.75f, 1f));

            Rect contentRect = new Rect(
                rect.x + PanelPadding,
                rect.y + PanelPadding,
                rect.width - (PanelPadding * 2f),
                rect.height - (PanelPadding * 2f));

            float y = contentRect.y;
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, AnalysisTitleHeight), AnalysisPanelTitle, headingStyle);
            y += AnalysisTitleHeight;

            string metaText = lastHumanPattern != null
                ? $"Turn: {lastHumanPattern.turnId}  |  Steps: {lastHumanPattern.StepCount}  |  BPM: {lastHumanPattern.bpm:0.##}"
                : "Waiting for a compiled human PatternTurn.";
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, AnalysisMetaHeight), metaText, bodyStyle);
            y += AnalysisMetaHeight + AnalysisTopSpacing;

            Rect textRect = new Rect(
                contentRect.x,
                y,
                contentRect.width,
                Mathf.Max(1f, contentRect.yMax - y));

            if (lastAnalysisResult == null)
            {
                GUI.Label(
                    textRect,
                    "No TurnAnalysisResult available yet.\nCompile a turn to inspect the analyser output.",
                    placeholderStyle);
                return;
            }

            GUI.Label(textRect, lastAnalysisSummary, analysisTextStyle);
        }

        private void DrawPatternPanel(Rect rect, PatternTurnDebugRenderer renderer, string label)
        {
            if (renderer == null)
            {
                DrawFilledRect(rect, new Color(0.10f, 0.10f, 0.10f, 0.94f));
                DrawOutline(rect, new Color(0.75f, 0.75f, 0.75f, 1f));
                GUI.Label(new Rect(rect.x + PanelPadding, rect.y + PanelPadding, rect.width - (PanelPadding * 2f), 22f), label, headingStyle);
                GUI.Label(
                    new Rect(rect.x + PanelPadding, rect.y + PanelPadding + 28f, rect.width - (PanelPadding * 2f), 36f),
                    "Assign a PatternTurnDebugRenderer to this panel.",
                    placeholderStyle);
                return;
            }

            renderer.Draw(rect);
        }

        private void TryBeginHumanPatternExport()
        {
            PatternTurn pattern = humanPatternRenderer != null ? humanPatternRenderer.Pattern : lastHumanPattern;
            if (pattern == null)
            {
                Debug.LogWarning("TurnLoopDebugPresenter could not export the human pattern panel because no compiled pattern is available yet.");
                return;
            }

            if (!showDebugUi)
            {
                Debug.LogWarning("TurnLoopDebugPresenter could not export the human pattern panel because the debug UI is hidden.");
                return;
            }

            if (!TryGetHumanPatternPanelRect(out _))
            {
                Debug.LogWarning("TurnLoopDebugPresenter could not export the human pattern panel because its on-screen rect could not be resolved.");
                return;
            }

            StartCoroutine(CaptureHumanPatternPanelPng(pattern));
        }

        private IEnumerator CaptureHumanPatternPanelPng(PatternTurn pattern)
        {
            exportInProgress = true;
            yield return new WaitForEndOfFrame();

            Texture2D screenshot = null;
            Texture2D cropped = null;

            try
            {
                if (!TryGetHumanPatternPanelRect(out Rect panelRect))
                {
                    Debug.LogWarning("TurnLoopDebugPresenter could not export the human pattern panel because its layout changed before capture completed.");
                    yield break;
                }

                int superSize = Mathf.Max(1, exportSuperSize);
                screenshot = ScreenCapture.CaptureScreenshotAsTexture(superSize);
                if (screenshot == null)
                {
                    Debug.LogWarning("TurnLoopDebugPresenter failed to capture the screen texture for human panel export.");
                    yield break;
                }

                // IMGUI rects use a top-left origin, but Texture2D pixel reads start at bottom-left.
                if (!TryGetCropBounds(panelRect, superSize, screenshot.width, screenshot.height, out int cropX, out int cropY, out int cropWidth, out int cropHeight))
                {
                    Debug.LogWarning("TurnLoopDebugPresenter failed to crop the captured screen to the human pattern panel bounds.");
                    yield break;
                }

                cropped = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false);
                cropped.SetPixels(screenshot.GetPixels(cropX, cropY, cropWidth, cropHeight));
                cropped.Apply();

                string exportDirectory = GetHumanPanelExportDirectory();
                Directory.CreateDirectory(exportDirectory);

                string filePath = Path.Combine(exportDirectory, BuildHumanPanelExportFileName(pattern, superSize));
                File.WriteAllBytes(filePath, cropped.EncodeToPNG());
                Debug.Log($"Human pattern panel PNG exported to: {filePath}");
            }
            finally
            {
                if (cropped != null)
                {
                    Destroy(cropped);
                }

                if (screenshot != null)
                {
                    Destroy(screenshot);
                }

                exportInProgress = false;
            }
        }

        private void AutoResolveReferences()
        {
            if (turnLoopController == null)
            {
                turnLoopController = GetComponent<TurnLoopController>();
            }

            PatternTurnDebugRenderer[] renderers = GetComponentsInChildren<PatternTurnDebugRenderer>(true);
            if (humanPatternRenderer == null && renderers.Length > 0)
            {
                humanPatternRenderer = renderers[0];
            }

            if (aiPatternRenderer == null && renderers.Length > 1)
            {
                aiPatternRenderer = renderers[1];
            }

            if (responsePlannerRenderer == null)
            {
                responsePlannerRenderer = GetComponentInChildren<ResponsePlannerDebugRenderer>(true);
            }

            if (skeletonRenderer == null)
            {
                skeletonRenderer = GetComponentInChildren<SkeletonDebugRenderer>(true);
            }

            if (skeletonRenderer == null)
            {
                skeletonRenderer = gameObject.AddComponent<SkeletonDebugRenderer>();
            }

            responsePlannerRenderer?.SetTurnLoopController(turnLoopController);
            skeletonRenderer?.SetTurnLoopController(turnLoopController);
        }

        private void PullExistingControllerState()
        {
            if (turnLoopController == null)
            {
                return;
            }

            currentPhase = turnLoopController.CurrentPhase;

            if (turnLoopController.HasLastTurnWindow)
            {
                OnHumanTurnCaptured(turnLoopController.LastTurnWindow);
            }

            if (turnLoopController.HasLastCompiledPatternTurn)
            {
                OnPatternCompiled(turnLoopController.LastCompiledPatternTurn);
            }

            if (turnLoopController.HasLastGeneratedAiPatternTurn)
            {
                OnAiPatternGenerated(turnLoopController.LastGeneratedAiPatternTurn);
            }

            if (turnLoopController.HasLastAnalysisResult)
            {
                OnHumanTurnAnalysed(turnLoopController.LastAnalysisResult);
            }
        }

        private void Subscribe()
        {
            if (turnLoopController == null)
            {
                return;
            }

            turnLoopController.OnPhaseChanged += OnPhaseChanged;
            turnLoopController.OnHumanTurnCaptured += OnHumanTurnCaptured;
            turnLoopController.OnHumanPatternCompiled += OnPatternCompiled;
            turnLoopController.OnHumanTurnAnalysed += OnHumanTurnAnalysed;
            turnLoopController.OnAiPatternGenerated += OnAiPatternGenerated;
        }

        private void Unsubscribe()
        {
            if (turnLoopController == null)
            {
                return;
            }

            turnLoopController.OnPhaseChanged -= OnPhaseChanged;
            turnLoopController.OnHumanTurnCaptured -= OnHumanTurnCaptured;
            turnLoopController.OnHumanPatternCompiled -= OnPatternCompiled;
            turnLoopController.OnHumanTurnAnalysed -= OnHumanTurnAnalysed;
            turnLoopController.OnAiPatternGenerated -= OnAiPatternGenerated;
        }

        private float GetAnalysisPreferredHeight(float width)
        {
            EnsureStyles();

            float contentWidth = Mathf.Max(1f, width - (PanelPadding * 2f));
            float height = (PanelPadding * 2f) + AnalysisTitleHeight + AnalysisMetaHeight + AnalysisTopSpacing;

            if (lastAnalysisResult == null)
            {
                return Mathf.Max(height + 48f, MinimumAnalysisHeight);
            }

            height += analysisTextStyle.CalcHeight(new GUIContent(lastAnalysisSummary), contentWidth);
            return Mathf.Max(height, MinimumAnalysisHeight);
        }

        private bool TryGetHumanPatternPanelRect(out Rect rect)
        {
            rect = default;
            if (!showDebugUi)
            {
                return false;
            }

            float width = Mathf.Min(maxPanelWidth, Screen.width - (screenPadding.x * 2f));
            if (width <= 0f)
            {
                return false;
            }

            float x = screenPadding.x;
            float y = screenPadding.y + SummaryHeight + SectionSpacing;
            float panelWidth = (width - SplitPanelSpacing) * 0.5f;
            float humanHeight = humanPatternRenderer != null
                ? humanPatternRenderer.GetPreferredHeight(panelWidth)
                : 180f;
            float analysisHeight = GetAnalysisPreferredHeight(panelWidth);
            float comparisonHeight = Mathf.Max(humanHeight, analysisHeight);

            rect = new Rect(x, y, panelWidth, comparisonHeight);
            return rect.width > 0f && rect.height > 0f;
        }

        private string GetHumanPanelExportDirectory()
        {
            string relativeDirectory = string.IsNullOrWhiteSpace(exportFolderName)
                ? Path.Combine("DiagnosticsOutput", "DebugPanelExports")
                : exportFolderName;
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeDirectory));
        }

        private static string BuildHumanPanelExportFileName(PatternTurn pattern, int superSize)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string turnFragment = pattern != null ? $"turn_{pattern.turnId}_" : string.Empty;
            return $"human_pattern_{turnFragment}{timestamp}_x{superSize}.png";
        }

        private static bool TryGetCropBounds(
            Rect panelRect,
            int superSize,
            int screenshotWidth,
            int screenshotHeight,
            out int cropX,
            out int cropY,
            out int cropWidth,
            out int cropHeight)
        {
            int scaledXMin = Mathf.Clamp(Mathf.RoundToInt(panelRect.xMin * superSize), 0, screenshotWidth);
            int scaledXMax = Mathf.Clamp(Mathf.RoundToInt(panelRect.xMax * superSize), 0, screenshotWidth);
            int scaledYTop = Mathf.Clamp(Mathf.RoundToInt(panelRect.yMin * superSize), 0, screenshotHeight);
            int scaledYBottom = Mathf.Clamp(Mathf.RoundToInt(panelRect.yMax * superSize), 0, screenshotHeight);

            cropX = scaledXMin;
            cropY = screenshotHeight - scaledYBottom;
            cropWidth = scaledXMax - scaledXMin;
            cropHeight = scaledYBottom - scaledYTop;

            return cropWidth > 0 && cropHeight > 0;
        }

        private void ClearAnalysisPresentation()
        {
            lastAnalysisResult = null;
            lastAnalysisSummary = string.Empty;
        }

        private void EnsureStyles()
        {
            if (headingStyle != null)
            {
                return;
            }

            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            placeholderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.82f, 0.82f, 1f) }
            };

            analysisTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }

        private static void DrawFilledRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            DrawFilledRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            DrawFilledRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            DrawFilledRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }
    }
}
