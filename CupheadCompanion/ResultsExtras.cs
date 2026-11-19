using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CupheadCompanion
{
    // The results screen already shows your previous top grade. This adds the matching "top time" line
    // under it (the time the save held before this run) and says when this run beat it.
    [HarmonyPatch(typeof(WinScreenGradeDisplay), "Start")]
    internal static class ResultsExtras
    {
        private static void Postfix(WinScreenGradeDisplay __instance)
        {
            try
            {
                if (!Plugin.ResultsTopTime.Value) return;
                WinCapture.Record last = WinCapture.Last;
                if (last == null || !last.graded || !last.previouslyWon || last.level != Level.PreviousLevel) return;
                if (last.prevTime >= float.MaxValue) return;

                Traverse t = Traverse.Create(__instance);
                var label = t.Field("topGradeLabel").GetValue<TextMeshProUGUI>();
                var value = t.Field("topGradeValue").GetValue<Text>();
                if (label == null || value == null) return;

                // One text line below the game's own "top grade" line.
                float lineHeight = Mathf.Max(label.rectTransform.rect.height, label.fontSize * 1.1f);
                TextMeshProUGUI timeLabel = Clone(label, lineHeight);
                Text timeValue = Clone(value, lineHeight);
                timeLabel.text = "TOP TIME:";
                timeValue.text = " " + LevelInfo.FormatTime(last.prevTime) + (last.newBestTime ? " - BEATEN!" : string.Empty);
                timeValue.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            catch (Exception e) { Plugin.Log.LogError("Results extras failed: " + e); }
        }

        private static T Clone<T>(T original, float lineHeight) where T : Component
        {
            GameObject copy = UnityEngine.Object.Instantiate(original.gameObject, original.transform.parent);
            copy.name = original.gameObject.name + "_CompanionTopTime";
            // Stop the game's translation helper from writing the original label text back.
            foreach (LocalizationHelper helper in copy.GetComponentsInChildren<LocalizationHelper>(true))
                UnityEngine.Object.DestroyImmediate(helper);
            var rt = (RectTransform)copy.transform;
            rt.anchoredPosition = ((RectTransform)original.transform).anchoredPosition - new Vector2(0f, lineHeight);
            return copy.GetComponent<T>();
        }
    }

    // Optional and off by default, because it changes the game's pacing: pressing confirm as soon as
    // "YOU DIED" appears retries immediately instead of waiting for the death card to finish sliding in.
    [HarmonyPatch(typeof(LevelGameOverGUI), "Update")]
    internal static class FastRetry
    {
        private static void Prefix(LevelGameOverGUI __instance)
        {
            try
            {
                if (!Plugin.FastRetry.Value || Level.IsTowerOfPower) return;
                Traverse t = Traverse.Create(__instance);
                Traverse state = t.Field("state");
                if (Convert.ToInt32(state.GetValue()) != 0) return; // 0 = card still animating in
                var input = t.Field("input").GetValue<CupheadInput.AnyPlayerInput>();
                if (input == null || !input.GetButtonDown(CupheadButton.Accept)) return;

                // Same path as choosing "Retry" on the card: reset audio, then reload the level.
                t.Field("selection").SetValue(0);
                state.SetValue(Enum.ToObject(state.GetValueType(), 2)); // 2 = exiting
                AudioManager.Play("level_menu_select");
                t.Method("Select").GetValue();
            }
            catch (Exception e) { Plugin.Log.LogError("Fast retry failed: " + e); }
        }
    }
}
