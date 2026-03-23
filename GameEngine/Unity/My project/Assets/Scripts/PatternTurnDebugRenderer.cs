using IT4s.Data;
using UnityEngine;

namespace IT4s.Debugging
{
    /// <summary>
    /// Lightweight immediate-mode renderer for a PatternTurn.
    /// The renderer uses fixed cell geometry so rhythmic steps remain visually aligned even while
    /// patterns update in real time.
    /// </summary>
    public class PatternTurnDebugRenderer : MonoBehaviour
    {
        private const float Padding = 12f;
        private const float TitleHeight = 22f;
        private const float MetaHeight = 20f;
        private const float RowLabelWidth = 58f;
        private const float RowTextHeight = 18f;
        private const float CellRowHeight = 20f;
        private const float GroupSpacing = 12f;
        private const float LegendLineHeight = 18f;
        private const float HeaderFooterSpacing = 8f;

        [Header("Content")]
        [SerializeField] private string panelTitle = "PATTERN";
        [SerializeField] private bool drawStandalone;
        [SerializeField] private Rect standaloneRect = new Rect(16f, 120f, 960f, 260f);

        [Header("Layout")]
        [SerializeField, Min(1)] private int stepsPerRow = 24;
        [SerializeField] private bool showOffsetMarkers = true;
        [SerializeField] private bool showLegend = true;

        [Header("Colours")]
        [SerializeField] private Color panelBackground = new Color(0.11f, 0.11f, 0.11f, 0.95f);
        [SerializeField] private Color borderColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color strongHitColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color mediumHitColor = new Color(0.72f, 0.82f, 1f, 1f);
        [SerializeField] private Color weakHitColor = new Color(0.45f, 0.60f, 0.85f, 1f);
        [SerializeField] private Color inactiveCellColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        [SerializeField] private Color markerColor = new Color(0.05f, 0.05f, 0.05f, 1f);

        private PatternTurn pattern;
        private GUIStyle titleStyle;
        private GUIStyle metaStyle;
        private GUIStyle rowLabelStyle;
        private GUIStyle cellTextStyle;
        private GUIStyle emptyStateStyle;

        public PatternTurn Pattern => pattern;

        public void SetPattern(PatternTurn newPattern)
        {
            pattern = newPattern;
        }

        public void ClearPattern()
        {
            pattern = null;
        }

        public void SetPanelTitle(string newTitle)
        {
            panelTitle = string.IsNullOrWhiteSpace(newTitle) ? "PATTERN" : newTitle;
        }

        public float GetPreferredHeight(float width)
        {
            int groups = pattern != null && pattern.StepCount > 0
                ? Mathf.CeilToInt(pattern.StepCount / (float)Mathf.Max(1, stepsPerRow))
                : 1;

            float height = Padding * 2f;
            height += TitleHeight;
            height += MetaHeight;
            height += HeaderFooterSpacing;
            height += groups * (RowTextHeight + CellRowHeight + RowTextHeight + GroupSpacing);

            if (showLegend)
            {
                height += HeaderFooterSpacing + (LegendLineHeight * 5f);
            }

            return Mathf.Max(height, 140f);
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

            GUI.Label(new Rect(contentRect.x, y, contentRect.width, TitleHeight), panelTitle, titleStyle);
            y += TitleHeight;

            if (pattern == null || pattern.velocity == null)
            {
                GUI.Label(
                    new Rect(contentRect.x, y, contentRect.width, RowTextHeight * 2f),
                    "No pattern available.",
                    emptyStateStyle);
                return;
            }

            long durationSamples = pattern.endSamples - pattern.startSamples;
            string metadata =
                $"BPM: {pattern.bpm:0.##}  |  Steps/Quarter: {pattern.stepsPerQuarter}  |  " +
                $"Total Steps: {pattern.StepCount}  |  Duration: {durationSamples} samples";
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, MetaHeight), metadata, metaStyle);
            y += MetaHeight + HeaderFooterSpacing;

            int totalRows = Mathf.CeilToInt(pattern.StepCount / (float)Mathf.Max(1, stepsPerRow));
            int columnsPerRow = Mathf.Max(1, stepsPerRow);
            float gridWidth = Mathf.Max(1f, contentRect.width - RowLabelWidth);
            float cellWidth = gridWidth / columnsPerRow;
            int stepDigits = Mathf.Max(2, Mathf.Max(0, pattern.StepCount - 1).ToString().Length);

            for (int rowIndex = 0; rowIndex < totalRows; rowIndex++)
            {
                int startStep = rowIndex * columnsPerRow;
                DrawStepNumberRow(contentRect.x, y, cellWidth, columnsPerRow, startStep, stepDigits);
                y += RowTextHeight;

                DrawActiveRow(contentRect.x, y, cellWidth, columnsPerRow, startStep);
                y += CellRowHeight;

                DrawVelocityRow(contentRect.x, y, cellWidth, columnsPerRow, startStep);
                y += RowTextHeight + GroupSpacing;
            }

            if (!showLegend)
            {
                return;
            }

            GUI.Label(new Rect(contentRect.x, y, contentRect.width, LegendLineHeight), "Legend", rowLabelStyle);
            y += LegendLineHeight;

            DrawLegendEntry(contentRect.x, y, strongHitColor, "strong hit");
            y += LegendLineHeight;
            DrawLegendEntry(contentRect.x, y, mediumHitColor, "medium hit");
            y += LegendLineHeight;
            DrawLegendEntry(contentRect.x, y, weakHitColor, "weak hit");
            y += LegendLineHeight;
            DrawLegendEntry(contentRect.x, y, inactiveCellColor, "no hit");
        }

        private void OnGUI()
        {
            if (!drawStandalone)
            {
                return;
            }

            Draw(standaloneRect);
        }

        private void DrawStepNumberRow(float x, float y, float cellWidth, int columnsPerRow, int startStep, int stepDigits)
        {
            GUI.Label(new Rect(x, y, RowLabelWidth, RowTextHeight), "Step:", rowLabelStyle);

            for (int column = 0; column < columnsPerRow; column++)
            {
                int stepIndex = startStep + column;
                Rect cellRect = GetCellRect(x, y, cellWidth, column, RowTextHeight);
                DrawCellOutline(cellRect, borderColor);

                if (stepIndex >= pattern.StepCount)
                {
                    continue;
                }

                string label = stepIndex.ToString().PadLeft(stepDigits, '0');
                GUI.Label(cellRect, label, cellTextStyle);
            }
        }

        private void DrawActiveRow(float x, float y, float cellWidth, int columnsPerRow, int startStep)
        {
            GUI.Label(new Rect(x, y, RowLabelWidth, RowTextHeight), "Active:", rowLabelStyle);

            float samplesPerStep = GetSamplesPerStep();
            float maxOffsetMagnitude = Mathf.Max(1f, samplesPerStep * 0.5f);

            for (int column = 0; column < columnsPerRow; column++)
            {
                int stepIndex = startStep + column;
                Rect cellRect = GetCellRect(x, y, cellWidth, column, CellRowHeight);

                DrawFilledRect(cellRect, inactiveCellColor);
                DrawCellOutline(cellRect, borderColor);

                if (stepIndex >= pattern.StepCount || stepIndex >= pattern.velocity.Length)
                {
                    continue;
                }

                int velocity = pattern.velocity[stepIndex];
                if (velocity <= 0)
                {
                    continue;
                }

                Rect fillRect = new Rect(cellRect.x + 2f, cellRect.y + 2f, cellRect.width - 4f, cellRect.height - 4f);
                DrawFilledRect(fillRect, GetVelocityColor(velocity));

                if (!showOffsetMarkers || pattern.offsetSamples == null || stepIndex >= pattern.offsetSamples.Length)
                {
                    continue;
                }

                float normalizedOffset = Mathf.Clamp(pattern.offsetSamples[stepIndex] / maxOffsetMagnitude, -1f, 1f);
                float markerX = cellRect.center.x + (normalizedOffset * (cellRect.width * 0.35f));
                Rect markerRect = new Rect(markerX - 1f, cellRect.y + 3f, 2f, cellRect.height - 6f);
                DrawFilledRect(markerRect, markerColor);
            }
        }

        private void DrawVelocityRow(float x, float y, float cellWidth, int columnsPerRow, int startStep)
        {
            GUI.Label(new Rect(x, y, RowLabelWidth, RowTextHeight), "Vel:", rowLabelStyle);

            for (int column = 0; column < columnsPerRow; column++)
            {
                int stepIndex = startStep + column;
                Rect cellRect = GetCellRect(x, y, cellWidth, column, RowTextHeight);
                DrawCellOutline(cellRect, borderColor);

                if (stepIndex >= pattern.StepCount || stepIndex >= pattern.velocity.Length)
                {
                    continue;
                }

                GUI.Label(cellRect, pattern.velocity[stepIndex].ToString(), cellTextStyle);
            }
        }

        private void DrawLegendEntry(float x, float y, Color swatchColor, string label)
        {
            Rect swatchRect = new Rect(x, y + 2f, 16f, 12f);
            DrawFilledRect(swatchRect, swatchColor);
            DrawCellOutline(swatchRect, borderColor);
            GUI.Label(new Rect(x + 24f, y, 180f, LegendLineHeight), label, metaStyle);
        }

        private static Rect GetCellRect(float contentX, float y, float cellWidth, int column, float height)
        {
            return new Rect(
                contentX + RowLabelWidth + (cellWidth * column),
                y,
                cellWidth,
                height);
        }

        private float GetSamplesPerStep()
        {
            if (pattern == null || pattern.bpm <= 0f || pattern.stepsPerQuarter <= 0 || pattern.sampleRate <= 0)
            {
                return 1f;
            }

            double secondsPerQuarter = 60.0 / pattern.bpm;
            double secondsPerStep = secondsPerQuarter / pattern.stepsPerQuarter;
            return Mathf.Max(1f, (float)(secondsPerStep * pattern.sampleRate));
        }

        private Color GetVelocityColor(int velocity)
        {
            if (velocity >= 96)
            {
                return strongHitColor;
            }

            if (velocity >= 64)
            {
                return mediumHitColor;
            }

            return weakHitColor;
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

            emptyStateStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.82f, 0.82f, 1f) }
            };
        }
    }
}
