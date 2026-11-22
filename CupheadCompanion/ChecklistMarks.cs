using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace CupheadCompanion
{
    // Adds small charm icons to each row of the equip menu's checklist ("task list"):
    // the Astral Cookie if the level was beaten as Ms. Chalice, the relic if beaten with the Cursed Relic.
    // Grade, time and difficulty are already shown by the game itself.
    [HarmonyPatch(typeof(MapEquipUIChecklistItem))]
    internal static class ChecklistMarks
    {
        private const string ContainerName = "CompanionMarks";
        private const string WorthName = "RelicWorth";
        private const float GapAfterName = 5f, GapBetween = 3f, DotsPadding = 5f, SizeFactor = 1.3f;
        // Capital letters reach roughly 3/4 of the way from the baseline to the font's ascender line.
        private const float CapCentre = 0.38f;

        private static Sprite chaliceSprite, relicSprite;
        private static bool warnedMissing;

        [HarmonyPostfix, HarmonyPatch("SetDescription")]
        private static void AfterSetDescription(MapEquipUIChecklistItem __instance, Levels selectedLevel)
        {
            try { Refresh(__instance, selectedLevel); }
            catch (Exception e) { Plugin.Log.LogError("Checklist marks failed: " + e); }
        }

        private static void Refresh(MapEquipUIChecklistItem item, Levels level)
        {
            if (item.descriptionText == null) return;
            Transform existing = item.descriptionText.transform.Find(ContainerName);

            PlayerData.PlayerLevelDataObject data = PlayerData.Data.GetLevelData(level);
            bool chalice = data.played && (data.completedAsChaliceP1 || data.completedAsChaliceP2);
            bool relic = data.played && (data.curseCharmP1 || data.curseCharmP2);
            // Relic points this level would still give, shown as "+3" after the icons.
            float worth = data.played && !relic ? RelicInfo.PointsAvailable(level) : 0f;
            if (!Plugin.ChecklistIcons.Value || item.isDicePalaceMiniBoss || (!chalice && !relic && worth <= 0f))
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            LoadSprites();
            RectTransform container = existing as RectTransform ?? CreateContainer(item);
            container.gameObject.SetActive(true);

            // Measure the rendered name so the icons sit right after it, centred on the capital letters.
            // All coordinates are in the name text's local space (origin at its pivot).
            TMPro.TextMeshProUGUI name = item.descriptionText;
            name.ForceMeshUpdate();
            Rect rect = name.rectTransform.rect;
            float size = name.fontSize * SizeFactor;
            float x = rect.xMin + name.preferredWidth + GapAfterName;
            float y = rect.center.y;
            if (name.textInfo != null && name.textInfo.lineCount > 0)
            {
                TMPro.TMP_LineInfo line = name.textInfo.lineInfo[0];
                x = name.bounds.max.x + GapAfterName;
                y = line.baseline + (line.ascender - line.baseline) * CapCentre;
            }
            x = Place(container.GetChild(0), chaliceSprite, chalice, x, y, size);
            x = Place(container.GetChild(1), relicSprite, relic, x, y, size);
            x = PlaceWorth(container, item, worth, x, y);

            // The game filled the rest of the line with leader dots; trim them so they don't run under the icons.
            Text dots = item.leaderDotText;
            if (dots != null)
            {
                float room = rect.xMax - x - DotsPadding;
                if (room < 0f) dots.text = string.Empty;
                int guard = 1000;
                while (dots.text.Length > 2 && dots.preferredWidth > room && guard-- > 0)
                    dots.text = dots.text.Substring(0, dots.text.Length - 2);
            }
        }

        private static float Place(Transform child, Sprite sprite, bool show, float x, float y, float size)
        {
            var image = child.GetComponent<Image>();
            if (!show || sprite == null) { image.enabled = false; return x; }
            float width = size * sprite.rect.width / sprite.rect.height;
            image.enabled = true;
            image.sprite = sprite;
            var rt = (RectTransform)child;
            rt.sizeDelta = new Vector2(width, size);
            rt.anchoredPosition = new Vector2(x, y);
            return x + width + GapBetween;
        }

        // A small "+3" in the checklist's own grade font, copied from the row's grade text.
        private static float PlaceWorth(RectTransform container, MapEquipUIChecklistItem item, float worth, float x, float y)
        {
            Transform existing = container.Find(WorthName);
            if (worth <= 0f || item.gradeText == null)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return x;
            }
            Text text;
            if (existing != null) text = existing.GetComponent<Text>();
            else
            {
                GameObject copy = UnityEngine.Object.Instantiate(item.gradeText.gameObject, container);
                copy.name = WorthName;
                foreach (LocalizationHelper helper in copy.GetComponentsInChildren<LocalizationHelper>(true))
                    UnityEngine.Object.DestroyImmediate(helper);
                text = copy.GetComponent<Text>();
                text.raycastTarget = false;
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.fontSize = Mathf.Max(8, Mathf.RoundToInt(item.gradeText.fontSize * 0.7f));
                Color c = text.color; c.a = 0.75f; text.color = c;
                RectTransform rt = text.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0f, 0.5f);
                rt.localScale = Vector3.one;
            }
            text.gameObject.SetActive(true);
            text.text = "+" + RelicInfo.Format(worth);
            float width = text.preferredWidth;
            text.rectTransform.sizeDelta = new Vector2(width, text.preferredHeight);
            text.rectTransform.anchoredPosition = new Vector2(x, y);
            return x + width + GapBetween;
        }

        private static RectTransform CreateContainer(MapEquipUIChecklistItem item)
        {
            int layer = item.descriptionText.gameObject.layer;
            var go = new GameObject(ContainerName, typeof(RectTransform));
            go.layer = layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(item.descriptionText.transform, false);
            // A zero-size holder at the name's pivot, so children are positioned in the name's local space.
            Vector2 pivot = item.descriptionText.rectTransform.pivot;
            rt.anchorMin = pivot; rt.anchorMax = pivot; rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            for (int i = 0; i < 2; i++)
            {
                var child = new GameObject(i == 0 ? "Chalice" : "Relic", typeof(RectTransform));
                child.layer = layer;
                var crt = (RectTransform)child.transform;
                crt.SetParent(rt, false);
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0f, 0.5f);
                var image = child.AddComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
            }
            return rt;
        }

        private static void LoadSprites()
        {
            if (chaliceSprite != null && relicSprite != null) return;
            SpriteAtlas atlas = AssetLoader<SpriteAtlas>.GetCachedAsset("Equip_Icons_DLC");
            if (atlas == null) { WarnOnce("icon atlas Equip_Icons_DLC is not loaded"); return; }
            if (chaliceSprite == null) chaliceSprite = Find(atlas, "equip_icon_charm_chalice");
            if (relicSprite == null) relicSprite = Find(atlas, "equip_icon_charm_curse_1", "equip_icon_charm_curse");
            if (chaliceSprite == null || relicSprite == null)
                WarnOnce("missing icon sprites (chalice=" + (chaliceSprite != null) + ", relic=" + (relicSprite != null) + ")");
        }

        private static Sprite Find(SpriteAtlas atlas, params string[] names)
        {
            foreach (string name in names)
            {
                foreach (string suffix in new[] { "", "_0001" })
                {
                    Sprite s = atlas.GetSprite(name + suffix);
                    if (s != null) return s;
                }
            }
            return null;
        }

        private static void WarnOnce(string message)
        {
            if (warnedMissing) return;
            warnedMissing = true;
            Plugin.Log.LogWarning("Checklist marks: " + message);
        }
    }
}
