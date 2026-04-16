using System.Globalization;
using IT4s.Orchestration;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.ResponsePlanning.Models;
using UnityEngine;

namespace IT4s.Debugging
{
    /// <summary>
    /// Lightweight immediate-mode renderer for ResponsePlanner.LastSnapshot.
    /// This is intentionally passive: it only reads the latest snapshot published by the planner.
    /// </summary>
    public class ResponsePlannerDebugRenderer : MonoBehaviour
    {
        private const float Padding = 12f;
        private const float TitleHeight = 22f;
        private const float SelectedHeight = 24f;
        private const float SectionTitleHeight = 18f;
        private const float RowHeight = 19f;
        private const float SectionSpacing = 9f;
        private const float ColumnSpacing = 16f;
        private const float ScoreNameWidth = 150f;
        private const float ScoreGroupMargin = 0.5f;
        private const float MinimumHeight = 520f;

        private static readonly ResponseType[] ResponseTypes =
        {
            ResponseType.Mirror,
            ResponseType.Complement,
            ResponseType.Simplify,
            ResponseType.Intensify,
            ResponseType.Contrast,
            ResponseType.Fill
        };

        [Header("Source")]
        [SerializeField] private TurnLoopController turnLoopController;

        [Header("Content")]
        [SerializeField] private bool drawStandalone;
        [SerializeField] private Rect standaloneRect = new Rect(16f, 400f, 460f, 520f);

        [Header("Colours")]
        [SerializeField] private Color panelBackground = new Color(0.10f, 0.10f, 0.10f, 0.94f);
        [SerializeField] private Color borderColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color selectedRowColor = new Color(0.18f, 0.32f, 0.44f, 0.95f);
        [SerializeField] private Color selectedTextColor = new Color(0.86f, 0.96f, 1f, 1f);
        [SerializeField] private Color mutedTextColor = new Color(0.82f, 0.82f, 0.82f, 1f);

        private GUIStyle titleStyle;
        private GUIStyle selectedStyle;
        private GUIStyle sectionStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle decisionStyle;
        private GUIStyle selectedValueStyle;
        private GUIStyle placeholderStyle;

        public ResponsePlannerDebugSnapshot CurrentSnapshot
        {
            get
            {
                AutoResolveController();
                return turnLoopController != null ? turnLoopController.LastResponsePlannerSnapshot : null;
            }
        }

        public void SetTurnLoopController(TurnLoopController controller)
        {
            turnLoopController = controller;
        }

        public float GetPreferredHeight(float width)
        {
            return MinimumHeight;
        }

        public void Draw(Rect rect)
        {
            EnsureStyles();
            DrawPanel(rect);

            Rect contentRect = new Rect(
                rect.x + Padding,
                rect.y + Padding,
                rect.width - (Padding * 2f),
                rect.height - (Padding * 2f));

            ResponsePlannerDebugSnapshot snapshot = CurrentSnapshot;

            float y = contentRect.y;
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, TitleHeight), "Response Planner", titleStyle);
            y += TitleHeight;

            if (snapshot == null)
            {
                GUI.Label(
                    new Rect(contentRect.x, y + SectionSpacing, contentRect.width, RowHeight * 2f),
                    "No planner data yet",
                    placeholderStyle);
                return;
            }

            GUI.Label(
                new Rect(contentRect.x, y, contentRect.width, SelectedHeight),
                $"Selected: {snapshot.SelectedResponseType}",
                selectedStyle);
            y += SelectedHeight;

            y = DrawDecisionSummary(contentRect, y, snapshot);
            y += SectionSpacing;

            y = DrawSourceSummary(contentRect, y, snapshot);
            y += SectionSpacing;

            y = DrawResponseTypeScores(contentRect, y, snapshot);
            y += SectionSpacing;

            DrawFinalPlan(contentRect, y, snapshot.FinalPlan);
        }

        private void OnGUI()
        {
            if (!drawStandalone)
            {
                return;
            }

            Draw(standaloneRect);
        }

        private void AutoResolveController()
        {
            if (turnLoopController != null)
            {
                return;
            }

            turnLoopController = GetComponent<TurnLoopController>();

            if (turnLoopController == null)
            {
                turnLoopController = GetComponentInParent<TurnLoopController>();
            }
        }

        private float DrawSourceSummary(Rect contentRect, float y, ResponsePlannerDebugSnapshot snapshot)
        {
            ResponsePlannerDescriptorSummary descriptor = snapshot.SourceDescriptorSummary;
            ResponsePlannerNumericSummary numeric = snapshot.SourceNumericSummary;

            GUI.Label(new Rect(contentRect.x, y, contentRect.width, SectionTitleHeight), "SOURCE SUMMARY", sectionStyle);
            y += SectionTitleHeight;

            string descriptorText = descriptor != null && !string.IsNullOrWhiteSpace(descriptor.Summary)
                ? descriptor.Summary
                : "Neutral";
            DrawField(new Rect(contentRect.x, y, contentRect.width, RowHeight), "Descriptors", descriptorText);
            y += RowHeight;

            float columnWidth = (contentRect.width - ColumnSpacing) * 0.5f;
            float rightX = contentRect.x + columnWidth + ColumnSpacing;

            DrawField(new Rect(contentRect.x, y, columnWidth, RowHeight), "Density", FormatFloat(numeric != null ? numeric.SourceDensity : 0f));
            DrawField(new Rect(rightX, y, columnWidth, RowHeight), "Energy", FormatFloat(numeric != null ? numeric.SourceEnergy : 0f));
            y += RowHeight;

            DrawField(new Rect(contentRect.x, y, columnWidth, RowHeight), "AnchorCount", (numeric != null ? numeric.AnchorCount : 0).ToString(CultureInfo.InvariantCulture));
            DrawField(new Rect(rightX, y, columnWidth, RowHeight), "Profile", GetProfileLabel(descriptor));
            y += RowHeight;

            DrawField(new Rect(contentRect.x, y, columnWidth, RowHeight), "HasMeaningfulAnchors", FormatBool(descriptor != null && descriptor.HasMeaningfulAnchors));
            DrawField(new Rect(rightX, y, columnWidth, RowHeight), "HasStrongEnding", FormatBool(descriptor != null && descriptor.HasStrongEnding));
            y += RowHeight;

            DrawField(new Rect(contentRect.x, y, columnWidth, RowHeight), "EndingIsOpen", FormatBool(descriptor != null && descriptor.EndingIsOpen));
            return y + RowHeight;
        }

        private float DrawResponseTypeScores(Rect contentRect, float y, ResponsePlannerDebugSnapshot snapshot)
        {
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, SectionTitleHeight), "RESPONSE TYPE SCORES", sectionStyle);
            y += SectionTitleHeight;

            float selectedScore = GetScore(snapshot, snapshot.SelectedResponseType);
            y = DrawScoreGroup(contentRect, y, snapshot, selectedScore, "HIGH", true);
            y = DrawScoreGroup(contentRect, y, snapshot, selectedScore, "LOW", false);

            return y;
        }

        private float DrawDecisionSummary(Rect contentRect, float y, ResponsePlannerDebugSnapshot snapshot)
        {
            ResponseType topResponseType = GetTopScoringType(snapshot, out float topScore);
            float selectedScore = GetScore(snapshot, snapshot.SelectedResponseType);

            if (topResponseType != snapshot.SelectedResponseType)
            {
                string reason = InferOverrideReason(snapshot.SourceDescriptorSummary);
                GUI.Label(
                    new Rect(contentRect.x, y, contentRect.width, RowHeight),
                    $"Decision Override: {snapshot.SelectedResponseType} over {topResponseType} ({reason})",
                    decisionStyle);
                y += RowHeight;
            }

            DrawField(
                new Rect(contentRect.x, y, contentRect.width, RowHeight),
                "Top Score",
                $"{topResponseType} ({FormatFloat(topScore)})");
            y += RowHeight;

            DrawField(
                new Rect(contentRect.x, y, contentRect.width, RowHeight),
                "Selected",
                $"{snapshot.SelectedResponseType} ({FormatFloat(selectedScore)})");
            return y + RowHeight;
        }

        private float DrawScoreGroup(
            Rect contentRect,
            float y,
            ResponsePlannerDebugSnapshot snapshot,
            float selectedScore,
            string groupLabel,
            bool drawHighGroup)
        {
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, SectionTitleHeight), groupLabel, sectionStyle);
            y += SectionTitleHeight;

            bool drewAnyRow = false;
            for (int i = 0; i < ResponseTypes.Length; i++)
            {
                ResponseType responseType = ResponseTypes[i];
                float score = GetScore(snapshot, responseType);
                bool isHighScore = score >= selectedScore - ScoreGroupMargin;

                if (isHighScore != drawHighGroup)
                {
                    continue;
                }

                DrawScoreRow(
                    new Rect(contentRect.x, y, contentRect.width, RowHeight),
                    responseType,
                    score,
                    responseType == snapshot.SelectedResponseType);
                y += RowHeight;
                drewAnyRow = true;
            }

            if (!drewAnyRow)
            {
                GUI.Label(new Rect(contentRect.x + 4f, y, contentRect.width - 4f, RowHeight), "(none)", placeholderStyle);
                y += RowHeight;
            }

            return y;
        }

        private void DrawFinalPlan(Rect contentRect, float y, ResponsePlan plan)
        {
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, SectionTitleHeight), "FINAL PLAN", sectionStyle);
            y += SectionTitleHeight;

            if (plan == null)
            {
                GUI.Label(new Rect(contentRect.x, y, contentRect.width, RowHeight), "No final plan available.", placeholderStyle);
                return;
            }

            float columnWidth = (contentRect.width - ColumnSpacing) * 0.5f;
            float rightX = contentRect.x + columnWidth + ColumnSpacing;

            DrawField(new Rect(contentRect.x, y, columnWidth, RowHeight), "TargetDensity", FormatFloat(plan.TargetDensity));
            DrawField(new Rect(rightX, y, columnWidth, RowHeight), "ComplementarityBias", FormatFloat(plan.ComplementarityBias));
            y += RowHeight;

            DrawField(new Rect(contentRect.x, y, columnWidth, RowHeight), "PreserveAnchors", FormatBool(plan.PreserveAnchors));
            DrawField(new Rect(rightX, y, columnWidth, RowHeight), "MirrorEnding", FormatBool(plan.MirrorEnding));
            y += RowHeight;

            DrawField(new Rect(contentRect.x, y, columnWidth, RowHeight), "TurnLengthSteps", plan.TurnLengthSteps.ToString(CultureInfo.InvariantCulture));
        }

        private void DrawField(Rect rect, string label, string value)
        {
            float labelWidth = Mathf.Min(136f, rect.width * 0.68f);
            GUI.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), label, labelStyle);
            GUI.Label(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height), value, valueStyle);
        }

        private void DrawScoreRow(Rect rect, ResponseType responseType, float score, bool selected)
        {
            if (selected)
            {
                DrawFilledRect(new Rect(rect.x, rect.y + 1f, rect.width, rect.height - 2f), selectedRowColor);
            }

            GUIStyle rowValueStyle = selected ? selectedValueStyle : valueStyle;
            string prefix = selected ? ">> " : "   ";
            GUI.Label(
                new Rect(rect.x + 4f, rect.y, ScoreNameWidth, rect.height),
                $"{prefix}{responseType}",
                rowValueStyle);
            GUI.Label(
                new Rect(rect.x + ScoreNameWidth, rect.y, rect.width - ScoreNameWidth - 4f, rect.height),
                FormatFloat(score),
                rowValueStyle);
        }

        private static float GetScore(ResponsePlannerDebugSnapshot snapshot, ResponseType responseType)
        {
            if (snapshot == null || snapshot.PerResponseTypeScores == null)
            {
                return 0f;
            }

            for (int i = 0; i < snapshot.PerResponseTypeScores.Count; i++)
            {
                ResponseTypeScore score = snapshot.PerResponseTypeScores[i];
                if (score != null && score.ResponseType == responseType)
                {
                    return score.Score;
                }
            }

            return 0f;
        }

        private static ResponseType GetTopScoringType(ResponsePlannerDebugSnapshot snapshot, out float topScore)
        {
            ResponseType topResponseType = snapshot != null
                ? snapshot.SelectedResponseType
                : ResponseType.Mirror;
            topScore = float.MinValue;

            for (int i = 0; i < ResponseTypes.Length; i++)
            {
                ResponseType responseType = ResponseTypes[i];
                float score = GetScore(snapshot, responseType);

                if (score > topScore)
                {
                    topResponseType = responseType;
                    topScore = score;
                }
            }

            return topResponseType;
        }

        private static string InferOverrideReason(ResponsePlannerDescriptorSummary descriptor)
        {
            if (descriptor == null)
            {
                return "rule preference";
            }

            if (descriptor.EndingIsOpen || !descriptor.HasStrongEnding)
            {
                return "weak ending";
            }

            if (descriptor.HasMeaningfulAnchors)
            {
                return "anchor preservation";
            }

            if (descriptor.HasConversationalSpace)
            {
                return "gap-play";
            }

            return "rule preference";
        }

        private static string GetProfileLabel(ResponsePlannerDescriptorSummary descriptor)
        {
            if (descriptor == null)
            {
                return "Unknown";
            }

            if (descriptor.ActivityIsFrontLoaded)
            {
                return "Front";
            }

            if (descriptor.ActivityIsBackLoaded)
            {
                return "Back";
            }

            return descriptor.ActivityIsBalanced ? "Balanced" : "Unknown";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatBool(bool value)
        {
            return value ? "Yes" : "No";
        }

        private void DrawPanel(Rect rect)
        {
            DrawFilledRect(rect, panelBackground);
            DrawOutline(rect, borderColor);
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

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            selectedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = selectedTextColor }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = mutedTextColor }
            };

            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };

            decisionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = selectedTextColor }
            };

            selectedValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = selectedTextColor }
            };

            placeholderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = mutedTextColor }
            };
        }
    }
}
