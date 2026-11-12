using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>A scrolling checklist of things to exclude; ticked entries' keys are kept in the given settings list.</summary>
    public class Dialog_ExclusionList : Window
    {
        public readonly struct Entry
        {
            public readonly string Key;
            public readonly string Label;
            public readonly string? Tooltip;
            public readonly Texture2D? Icon;

            public Entry(string key, string label, string? tooltip, Texture2D? icon = null)
            {
                Key = key;
                Label = label;
                Tooltip = tooltip;
                Icon = icon;
            }
        }

        private const float RowHeight = 28f;

        private const float IconSize = 24f;

        private readonly string title;
        private readonly string clearLabel;
        private readonly List<Entry> entries;
        private readonly List<string> excluded;
        private Vector2 scroll;

        public Dialog_ExclusionList(string title, List<Entry> entries, List<string> excluded)
        {
            this.title = title;
            this.entries = entries;
            this.excluded = excluded;
            clearLabel = "JustStart_ExclusionClear".Translate();
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(460f, 640f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), title);
            Text.Font = GameFont.Small;

            Rect clearRect = new Rect(inRect.xMax - 120f, inRect.y + 40f, 120f, 30f);
            if (Widgets.ButtonText(clearRect, clearLabel, active: excluded.Count > 0))
                excluded.Clear();

            Rect outRect = new Rect(inRect.x, clearRect.yMax + 6f, inRect.width, inRect.height - (clearRect.yMax + 6f - inRect.y) - CloseButSize.y - 10f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, entries.Count * RowHeight);
            Widgets.BeginScrollView(outRect, ref scroll, viewRect);
            for (int i = 0; i < entries.Count; i++)
            {
                Rect row = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                // Only draw rows in view.
                if (row.yMax < scroll.y || row.y > scroll.y + outRect.height)
                    continue;
                DrawRow(row, entries[i], i);
            }
            Widgets.EndScrollView();
        }

        private void DrawRow(Rect row, Entry entry, int index)
        {
            if (index % 2 == 1)
                Widgets.DrawLightHighlight(row);
            Widgets.DrawHighlightIfMouseover(row);
            Rect labelRect = row;
            if (entry.Icon != null)
            {
                GUI.DrawTexture(new Rect(row.x + 4f, row.y + (RowHeight - IconSize) / 2f, IconSize, IconSize), entry.Icon);
                labelRect.xMin += IconSize + 8f;
            }
            else
                labelRect.xMin += 4f;

            bool wasExcluded = excluded.Contains(entry.Key);
            bool isExcluded = wasExcluded;
            Widgets.CheckboxLabeled(labelRect, entry.Label, ref isExcluded);
            if (isExcluded != wasExcluded)
            {
                if (isExcluded)
                    excluded.Add(entry.Key);
                else
                    excluded.Remove(entry.Key);
            }
            if (entry.Tooltip != null)
                TooltipHandler.TipRegion(row, entry.Tooltip);
        }
    }
}
