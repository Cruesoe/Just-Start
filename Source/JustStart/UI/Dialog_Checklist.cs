using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace JustStart
{
    /// <summary>A scrolling checklist; ticked entries' keys are kept in the given settings list.</summary>
    public class Dialog_Checklist : Window
    {
        public readonly struct Entry
        {
            public readonly string Key;
            public readonly string Label;
            public readonly string? Tooltip;
            public readonly Texture2D? Icon;
            /// <summary>When set, the entry is greyed out and inert, with this as its tooltip.</summary>
            public readonly string? DisabledReason;

            public Entry(string key, string label, string? tooltip, Texture2D? icon = null, string? disabledReason = null)
            {
                Key = key;
                Label = label;
                Tooltip = tooltip;
                Icon = icon;
                DisabledReason = disabledReason;
            }
        }

        private const float RowHeight = 28f;

        private const float IconSize = 24f;

        private readonly string title;
        private readonly string? description;
        private readonly string clearLabel;
        private readonly List<Entry> entries;
        private readonly List<string> ticked;
        private Vector2 scroll;

        public Dialog_Checklist(string title, string? description, List<Entry> entries, List<string> ticked)
        {
            this.title = title;
            this.description = description;
            this.entries = entries;
            this.ticked = ticked;
            clearLabel = "JustStart_ChecklistClear".Translate();
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
            if (description != null)
            {
                TextAnchor anchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(inRect.x, clearRect.y, clearRect.x - inRect.x - 10f, clearRect.height), description);
                Text.Anchor = anchor;
            }
            if (Widgets.ButtonText(clearRect, clearLabel, active: ticked.Count > 0))
                ticked.Clear();

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

            bool disabled = entry.DisabledReason != null;
            bool wasTicked = ticked.Contains(entry.Key);
            bool isTicked = wasTicked;
            Widgets.CheckboxLabeled(labelRect, entry.Label, ref isTicked, disabled);
            if (isTicked != wasTicked)
            {
                if (isTicked)
                    ticked.Add(entry.Key);
                else
                    ticked.Remove(entry.Key);
            }
            string? tip = entry.DisabledReason ?? entry.Tooltip;
            if (tip != null)
                TooltipHandler.TipRegion(row, tip);
        }
    }
}
