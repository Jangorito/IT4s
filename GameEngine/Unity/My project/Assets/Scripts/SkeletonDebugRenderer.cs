using System.Globalization;
using IT4s.Orchestration;
using IT4s.Rhythm.Generation.Skeleton;
using UnityEngine;

namespace IT4s.Debugging
{
    /// <summary>
    /// Passive immediate-mode renderer for the latest generated skeleton snapshot.
    /// </summary>
    public class SkeletonDebugRenderer : MonoBehaviour
    {
        private const float Padding = 12f;
        private const float TitleHeight = 22f;
        private const float MetaLineHeight = 18f;
        private const float RowLabelWidth = 58f;
        private const float RowTextHeight = 18f;
        private const float CellRowHeight = 22f;
        private const float GroupSpacing = 10f;
        private const float LegendLineHeight = 18f;
        private const float HeaderFooterSpacing = 8f;
        private const float MinimumHeight = 170f;

        [Header("Source")]
        [SerializeField] private TurnLoopController turnLoopController;

        [Header("Content")]
        [SerializeField] private bool drawStandalone;
        [SerializeField] private Rect standaloneRect = new Rect(16f, 680f, 960f, 260f);

        [Header("Layout")]
        [SerializeField, Min(1)] private int stepsPerRow = 24;
        [SerializeField] private bool showLegend = true;

        [Header("Colours")]
        [SerializeField] private Color panelBackground = new Color(0.11f, 0.11f, 0.11f, 0.95f);
        [SerializeField] private Color borderColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color activeCellColor = new Color(0.86f, 0.96f, 1f, 1f);
        [SerializeField] private Color inactiveCellColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        [SerializeField] private Color anchorMarkerColor = new Color(0.95f, 0.78f, 0.28f, 1f);
        [SerializeField] private Color endingMarkerColor = new Color(0.30f, 0.70f, 0.85f, 0.95f);
        [SerializeField] private Color strongBeatOutlineColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color weakBeatTextColor = new Color(0.66f, 0.66f, 0.66f, 1f);

        private GUIStyle titleStyle;
        private GUIStyle metaStyle;
        private GUIStyle rowLabelStyle;
        private GUIStyle cellTextStyle;
        private GUIStyle weakCellTextStyle;
        private GUIStyle emptyStateStyle;

        public SkeletonDebugSnapshot CurrentSnapshot
        {
            get
            {
                AutoResolveController();
                return turnLoopController != null ? turnLoopController.LastSkeletonDebugSnapshot : null;
            }
        }

        public void SetTurnLoopController(TurnLoopController controller)
        {
            turnLoopController = controller;
        }

        public float GetPreferredHeight(float width)
        {
            SkeletonDebugSnapshot snapshot = CurrentSnapshot;
            if (snapshot == null)
            {
                return MinimumHeight;
            }

            int totalSteps = snapshot.TurnLengthSteps > 0
                ? snapshot.TurnLengthSteps
                : Mathf.Max(1, stepsPerRow);
            int groups = Mathf.CeilToInt(totalSteps / (float)Mathf.Max(1, stepsPerRow));

            float height = Padding * 2f;
            height += TitleHeight;
            height += MetaLineHeight * (snapshot != null ? 2f : 1f);
            height += HeaderFooterSpacing;
            height += groups * (RowTextHeight + CellRowHeight + GroupSpacing);

            if (showLegend)
            {
                height += HeaderFooterSpacing + (LegendLineHeight * 5f);
            }

            return Mathf.Max(height, MinimumHeight);
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

            float y = contentRect.y;
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, TitleHeight), "Skeleton", titleStyle);
            y += TitleHeight;

            SkeletonDebugSnapshot snapshot = CurrentSnapshot;
            if (snapshot == null)
            {
                GUI.Label(
                    new Rect(contentRect.x, y + HeaderFooterSpacing, contentRect.width, RowTextHeight * 2f),
                    "No skeleton data yet",
                    emptyStateStyle);
                return;
            }

            GUI.Label(
                new Rect(contentRect.x, y, contentRect.width, MetaLineHeight),
                $"Steps: {snapshot.TurnLengthSteps}  |  Selected: {snapshot.SelectedStepCount}  |  Target Density: {FormatFloat(snapshot.TargetDensity)}  |  Achieved: {FormatFloat(snapshot.AchievedDensity)}",
                metaStyle);
            y += MetaLineHeight;

            GUI.Label(
                new Rect(contentRect.x, y, contentRect.width, MetaLineHeight),
                $"Preserve Anchors: {FormatBool(snapshot.PreserveAnchors)}  |  Require Strong Ending: {FormatBool(snapshot.RequireStrongEnding)}  |  Anchor Hits: {snapshot.AnchorAlignedCount}  |  Source Overlap: {snapshot.SourceOverlapCount}",
                metaStyle);
            y += MetaLineHeight + HeaderFooterSpacing;

            int totalRows = Mathf.CeilToInt(snapshot.TurnLengthSteps / (float)Mathf.Max(1, stepsPerRow));
            int columnsPerRow = Mathf.Max(1, stepsPerRow);
            float gridWidth = Mathf.Max(1f, contentRect.width - RowLabelWidth);
            float cellWidth = gridWidth / columnsPerRow;
            int stepDigits = Mathf.Max(2, Mathf.Max(0, snapshot.TurnLengthSteps - 1).ToString().Length);

            for (int rowIndex = 0; rowIndex < totalRows; rowIndex++)
            {
                int startStep = rowIndex * columnsPerRow;
                DrawStepNumberRow(snapshot, contentRect.x, y, cellWidth, columnsPerRow, startStep, stepDigits);
                y += RowTextHeight;

                DrawActiveRow(snapshot, contentRect.x, y, cellWidth, columnsPerRow, startStep);
                y += CellRowHeight + GroupSpacing;
            }

            if (!showLegend)
            {
                return;
            }

            y += HeaderFooterSpacing;
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, LegendLineHeight), "Legend", rowLabelStyle);
            y += LegendLineHeight;

            DrawLegendEntry(contentRect.x, y, activeCellColor, "active skeleton step");
            y += LegendLineHeight;
            DrawLegendEntry(contentRect.x, y, anchorMarkerColor, "anchor marker");
            y += LegendLineHeight;
            DrawLegendEntry(contentRect.x, y, endingMarkerColor, "ending region marker");
            y += LegendLineHeight;
            DrawLegendEntry(contentRect.x, y, inactiveCellColor, "inactive step");
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

        private void DrawStepNumberRow(
            SkeletonDebugSnapshot snapshot,
            float x,
            float y,
            float cellWidth,
            int columnsPerRow,
            int startStep,
            int stepDigits)
        {
            GUI.Label(new Rect(x, y, RowLabelWidth, RowTextHeight), "Step:", rowLabelStyle);

            for (int column = 0; column < columnsPerRow; column++)
            {
                int stepIndex = startStep + column;
                Rect cellRect = GetCellRect(x, y, cellWidth, column, RowTextHeight);
                DrawCellOutline(cellRect, borderColor);

                if (stepIndex >= snapshot.TurnLengthSteps)
                {
                    continue;
                }

                SkeletonStepDebugAnnotation annotation = snapshot.GetAnnotation(stepIndex);
                GUIStyle style = annotation.IsWeakMetrical ? weakCellTextStyle : cellTextStyle;
                GUI.Label(cellRect, stepIndex.ToString().PadLeft(stepDigits, '0'), style);

                if (annotation.IsStrongBeat)
                {
                    DrawCellOutline(cellRect, strongBeatOutlineColor);
                }
            }
        }

        private void DrawActiveRow(
            SkeletonDebugSnapshot snapshot,
            float x,
            float y,
            float cellWidth,
            int columnsPerRow,
            int startStep)
        {
            GUI.Label(new Rect(x, y, RowLabelWidth, RowTextHeight), "Active:", rowLabelStyle);

            for (int column = 0; column < columnsPerRow; column++)
            {
                int stepIndex = startStep + column;
                Rect cellRect = GetCellRect(x, y, cellWidth, column, CellRowHeight);
                DrawFilledRect(cellRect, inactiveCellColor);
                DrawCellOutline(cellRect, borderColor);

                if (stepIndex >= snapshot.TurnLengthSteps)
                {
                    continue;
                }

                SkeletonStepDebugAnnotation annotation = snapshot.GetAnnotation(stepIndex);
                if (snapshot.IsActiveStep(stepIndex))
                {
                    Rect fillRect = new Rect(cellRect.x + 2f, cellRect.y + 2f, cellRect.width - 4f, cellRect.height - 4f);
                    DrawFilledRect(fillRect, activeCellColor);
                }

                if (annotation.InEndingRegion)
                {
                    DrawFilledRect(
                        new Rect(cellRect.x + 2f, cellRect.yMax - 5f, cellRect.width - 4f, 3f),
                        endingMarkerColor);
                }

                if (annotation.IsAnchor)
                {
                    float markerHeight = annotation.IsProtectedAnchor ? 5f : 3f;
                    DrawFilledRect(
                        new Rect(cellRect.x + 2f, cellRect.y + 2f, cellRect.width - 4f, markerHeight),
                        anchorMarkerColor);
                }

                if (annotation.IsStrongBeat)
                {
                    DrawCellOutline(cellRect, strongBeatOutlineColor);
                }
            }
        }

        private void DrawLegendEntry(float x, float y, Color swatchColor, string label)
        {
            Rect swatchRect = new Rect(x, y + 2f, 16f, 12f);
            DrawFilledRect(swatchRect, swatchColor);
            DrawCellOutline(swatchRect, borderColor);
            GUI.Label(new Rect(x + 24f, y, 220f, LegendLineHeight), label, metaStyle);
        }

        private static Rect GetCellRect(float contentX, float y, float cellWidth, int column, float height)
        {
            return new Rect(
                contentX + RowLabelWidth + (cellWidth * column),
                y,
                cellWidth,
                height);
        }

        private void DrawPanel(Rect rect)
        {
            DrawFilledRect(rect, panelBackground);
            DrawCellOutline(rect, borderColor);
        }

        private static void DrawFilledRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawCellOutline(Rect rect, Color color)
        {
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            DrawFilledRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            DrawFilledRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            DrawFilledRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatBool(bool value)
        {
            return value ? "Yes" : "No";
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

            metaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            rowLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            cellTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            weakCellTextStyle = new GUIStyle(cellTextStyle)
            {
                normal = { textColor = weakBeatTextColor }
            };

            emptyStateStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.82f, 0.82f, 1f) }
            };
        }
    }
}
