using IT4s.Data;
using IT4s.Orchestration;
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

        [Header("Sources")]
        [SerializeField] private TurnLoopController turnLoopController;

        [Header("Renderers")]
        [SerializeField] private PatternTurnDebugRenderer humanPatternRenderer;
        [SerializeField] private PatternTurnDebugRenderer aiPatternRenderer;

        [Header("Layout")]
        [SerializeField] private bool showDebugUi = true;
        [SerializeField] private float maxPanelWidth = 980f;
        [SerializeField] private Vector2 screenPadding = new Vector2(16f, 16f);

        private TurnWindow lastTurnWindow;
        private bool hasLastTurnWindow;
        private PatternTurn lastHumanPattern;
        private PatternTurn lastAiPattern;
        private TurnPhase currentPhase = TurnPhase.Transition;

        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle placeholderStyle;

        public TurnPhase CurrentPhase => currentPhase;
        public bool HasLastTurnWindow => hasLastTurnWindow;
        public TurnWindow LastTurnWindow => lastTurnWindow;
        public PatternTurn LastHumanPattern => lastHumanPattern;
        public PatternTurn LastAiPattern => lastAiPattern;

        private void Awake()
        {
            AutoResolveReferences();

            humanPatternRenderer?.SetPanelTitle("HUMAN PATTERN");
            aiPatternRenderer?.SetPanelTitle("AI PATTERN");
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

        public void OnHumanTurnCaptured(TurnWindow window)
        {
            lastTurnWindow = window;
            hasLastTurnWindow = true;
        }

        public void OnPatternCompiled(PatternTurn pattern)
        {
            lastHumanPattern = pattern;
            humanPatternRenderer?.SetPattern(pattern);
        }

        public void OnAiPatternGenerated(PatternTurn pattern)
        {
            lastAiPattern = pattern;
            aiPatternRenderer?.SetPattern(pattern);
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

            float humanHeight = humanPatternRenderer != null
                ? humanPatternRenderer.GetPreferredHeight(width)
                : 180f;
            Rect humanRect = new Rect(x, y, width, humanHeight);
            DrawPatternPanel(humanRect, humanPatternRenderer, "HUMAN PATTERN");
            y += humanHeight + SectionSpacing;

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
            turnLoopController.OnAiPatternGenerated -= OnAiPatternGenerated;
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
