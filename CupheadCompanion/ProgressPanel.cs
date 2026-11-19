using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CupheadCompanion
{
    // Shows relic and unlock progress as a card beside the equip menu's checklist.
    // Values come from the game's own calculations; nothing is written to the save.
    [HarmonyPatch(typeof(MapEquipUIChecklist), "UpdateList")]
    internal static class ProgressPanel
    {
        private const string PanelName = "CompanionProgress";
        private const float GapFromCard = 18f, ScreenMargin = 40f;
        private const int RelicMaxPoints = 16, StripFilterTarget = 15;

        private static void Postfix(MapEquipUIChecklist __instance)
        {
            try
            {
                Transform old = __instance.transform.Find(PanelName);
                if (!Plugin.ProgressPanel.Value)
                {
                    if (old != null) old.gameObject.SetActive(false);
                    return;
                }
                if (Map.Current == null) return;

                // Measure the checklist card before our own panel is part of it.
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Bounds card = RectTransformUtility.CalculateRelativeRectTransformBounds(__instance.transform);

                float rowFont = 22f;
                var items = Traverse.Create(__instance).Field("checklistItems").GetValue<List<MapEquipUIChecklistItem>>();
                if (items != null && items.Count > 0 && items[0] != null && items[0].descriptionText != null)
                {
                    // Use the row's designed font size. Its current size is no good as a reference: the game
                    // shrinks it to fit long names (and never restores it), which made this card change size
                    // the first time you switched tabs.
                    float designed = Traverse.Create(items[0]).Field("originalFontSize").GetValue<float>();
                    if (designed <= 0f) designed = items[0].descriptionText.fontSize;
                    rowFont = Mathf.Clamp(designed * 0.7f, 12f, 40f);
                }

                RectTransform panel = CardBuilder.Build(__instance.transform, __instance.gameObject.layer,
                    "PROGRESS", Rows(), Mathf.Round(rowFont * 1.2f), Mathf.Round(rowFont));
                panel.gameObject.name = PanelName;

                // Sit to the left of the card, vertically centred, shrunk if the screen edge is too close.
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
                panel.pivot = new Vector2(1f, 0.5f);
                Vector2 pivotOffset = PivotOffset((RectTransform)__instance.transform);
                float rightEdge = card.min.x - GapFromCard;
                panel.anchoredPosition = new Vector2(rightEdge, card.center.y) - pivotOffset;

                float room = rightEdge - ScreenLeft(__instance.transform) - ScreenMargin;
                float width = panel.sizeDelta.x;
                if (room > 40f && width > room) panel.localScale = Vector3.one * (room / width);
            }
            catch (Exception e) { Plugin.Log.LogError("Progress panel failed: " + e); }
        }

        // anchoredPosition is measured from the parent's rect centre (anchors at 0.5), while the bounds
        // above are measured from the parent's pivot. This converts between the two.
        private static Vector2 PivotOffset(RectTransform parent)
        {
            return parent.rect.center;
        }

        // Left edge of the screen in the checklist's local space.
        private static float ScreenLeft(Transform checklist)
        {
            Canvas canvas = checklist.GetComponentInParent<Canvas>();
            if (canvas == null) return float.NegativeInfinity;
            var corners = new Vector3[4];
            ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(corners);
            return checklist.InverseTransformPoint(corners[0]).x;
        }

        private static List<KeyValuePair<string, string>> Rows()
        {
            var rows = new List<KeyValuePair<string, string>>();
            PlayerData data = PlayerData.Data;

            // Relic: the game's own level and point calculation.
            bool owned = data.IsUnlocked(PlayerId.PlayerOne, Charm.charm_curse);
            int level = CharmCurse.CalculateLevel(PlayerId.PlayerOne);
            string relic;
            if (!owned) relic = "NOT BOUGHT";
            else if (level < 0) relic = "BROKEN";
            else if (CharmCurse.IsMaxLevel(PlayerId.PlayerOne)) relic = "DIVINE";
            else relic = "CURSED " + level;
            Add(rows, "RELIC", relic);
            if (owned && level >= 0)
            {
                int points = Mathf.Min(RelicMaxPoints,
                    data.CalculateCurseCharmAccumulatedValue(PlayerId.PlayerOne, CharmCurse.CountableLevels));
                Add(rows, "RELIC POWER", points + " OF " + RelicMaxPoints);
            }

            // 2-Strip filter: A- or better on any 15 base-game bosses or Run 'n Guns.
            int aRanks = 0;
            foreach (Levels[] set in new[] { Level.world1BossLevels, Level.world2BossLevels, Level.world3BossLevels,
                         Level.world4BossLevels, Level.platformingLevels })
                aRanks += data.CountLevelsHaveMinGrade(set, LevelScoringData.Grade.AMinus);
            Add(rows, "2 STRIP FILTER", data.unlocked2Strip ? "UNLOCKED"
                : Mathf.Min(aRanks, StripFilterTarget) + " OF " + StripFilterTarget);

            // Black and white filter: a P grade on all six Run 'n Guns.
            int pRuns = data.CountLevelsHaveMinGrade(Level.platformingLevels, LevelScoringData.Grade.P);
            Add(rows, "B AND W FILTER", data.unlockedBlackAndWhite ? "UNLOCKED"
                : pRuns + " OF " + Level.platformingLevels.Length);

            // Ms. Chalice recolour: every boss on the game's list beaten as Ms. Chalice by player one.
            int asChalice = 0;
            foreach (Levels l in Level.chaliceLevels)
                if (data.GetLevelData(l).completedAsChaliceP1) asChalice++;
            Add(rows, "CHALICE RECOLOR", data.unlockedChaliceRecolor ? "UNLOCKED"
                : asChalice + " OF " + Level.chaliceLevels.Length);
            return rows;
        }

        private static void Add(List<KeyValuePair<string, string>> rows, string label, string value)
        {
            rows.Add(new KeyValuePair<string, string>(label, value));
        }
    }
}
