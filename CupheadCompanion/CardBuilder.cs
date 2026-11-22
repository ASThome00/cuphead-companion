using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CupheadCompanion
{
    // Builds a titled two-column card in the style of the game's own "ENTER" prompt bubble on the map
    // (same background sprite, font and colors). Used by the map popup and the progress panel.
    internal static class CardBuilder
    {
        private const float PadX = 16f, PadY = 12f, RowGap = 4f, ColumnGap = 28f, TitleGap = 8f;
        private static bool loggedTemplate;

        // Font size of the prompt bubble's text, the natural size for a card on the map canvas.
        public static float PromptFontSize
        {
            get { return PromptText().fontSize; }
        }

        private static TMP_FontAsset fullFont;
        private static bool fullFontTried;

        // The prompt bubble's own font is a cut-down English set whose "+" is the wrong glyph, which broke
        // grades like "C+". The game also ships the same typeface with its full character set; prefer that.
        private static TMP_FontAsset FullFont()
        {
            if (fullFont != null) return fullFont;
            if (fullFontTried) return null;
            fullFontTried = true;
            try
            {
                fullFont = FontLoader.GetTMPFont(FontLoader.TMPFontType.CupheadVogue_ExtraBold_merged__SDF);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("Full Vogue font not available, using the prompt font: " + e.Message);
            }
            Plugin.Log.LogDebug("Card font: " + (fullFont != null ? fullFont.name : "prompt font (fallback)"));
            return fullFont;
        }

        // Card titles are the game's own translated level names. In Korean, Japanese and Chinese those need
        // the font the game uses for its map prompt in that language; the Latin typeface has no such glyphs.
        // Row labels stay in English in every language.
        private static TMP_FontAsset NonLatinTitleFont()
        {
            Localization.Languages lang = Localization.language;
            if (lang != Localization.Languages.Korean && lang != Localization.Languages.Japanese
                && lang != Localization.Languages.SimplifiedChinese) return null;
            try { return Localization.Instance.fonts[(int)lang][28].fontAsset; }
            catch (System.Exception) { return null; }
        }

        private static TextMeshProUGUI PromptText()
        {
            MapUIInteractionDialogue prefab = Map.Current.MapResources.mapUIInteractionDialogue;
            return (TextMeshProUGUI)AccessTools.Field(typeof(AbstractUIInteractionDialogue), "tmpText").GetValue(prefab);
        }

        // Creates the card under `parent`, sized to fit its content. The caller positions and scales it.
        public static RectTransform Build(Transform parent, int layer, string titleText,
            IList<KeyValuePair<string, string>> rows, float titleSize, float rowSize)
        {
            MapUIInteractionDialogue prefab = Map.Current.MapResources.mapUIInteractionDialogue;
            TextMeshProUGUI tmpl = PromptText();
            var back = (RectTransform)AccessTools.Field(typeof(AbstractUIInteractionDialogue), "back").GetValue(prefab);
            Image backImage = back != null ? back.GetComponentInChildren<Image>(true) : null;

            if (!loggedTemplate)
            {
                loggedTemplate = true;
                Plugin.Log.LogDebug(string.Format(
                    "Prompt template: font={0} size={1} color={2} style={3}; back sprite={4} type={5} color={6}",
                    tmpl.font != null ? tmpl.font.name : "null", tmpl.fontSize, tmpl.color, tmpl.fontStyle,
                    backImage != null && backImage.sprite != null ? backImage.sprite.name : "null",
                    backImage != null ? backImage.type.ToString() : "-", backImage != null ? backImage.color.ToString() : "-"));
            }

            var root = new GameObject("CompanionCard", typeof(RectTransform));
            root.layer = layer;
            var rootRt = (RectTransform)root.transform;
            rootRt.SetParent(parent, false);
            rootRt.localRotation = Quaternion.identity;

            // Background, copied from the prompt bubble so it matches the game's art.
            RectTransform bg = NewChild("Back", rootRt, layer);
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
            var image = bg.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            if (backImage != null)
            {
                image.sprite = backImage.sprite; image.type = backImage.type; image.color = backImage.color;
                image.material = backImage.material; image.fillCenter = backImage.fillCenter;
            }
            else
            {
                image.color = Color.white;
                bg.gameObject.AddComponent<Outline>().effectColor = Color.black;
            }

            TextMeshProUGUI title = NewText("Title", rootRt, layer, tmpl, titleText, titleSize, TextAlignmentOptions.Center);
            TMP_FontAsset scriptFont = NonLatinTitleFont();
            if (scriptFont != null) title.font = scriptFont;
            var labels = new List<TextMeshProUGUI>();
            var values = new List<TextMeshProUGUI>();
            float labelWidth = 0f, valueWidth = 0f;
            foreach (KeyValuePair<string, string> row in rows)
            {
                TextMeshProUGUI l = NewText("Label", rootRt, layer, tmpl, row.Key, rowSize, TextAlignmentOptions.Left);
                TextMeshProUGUI v = NewText("Value", rootRt, layer, tmpl, row.Value, rowSize, TextAlignmentOptions.Right);
                labels.Add(l); values.Add(v);
                labelWidth = Mathf.Max(labelWidth, l.preferredWidth);
                valueWidth = Mathf.Max(valueWidth, v.preferredWidth);
            }

            float titleHeight = title.preferredHeight;
            float rowHeight = labels.Count > 0 ? labels[0].preferredHeight : 0f;
            float innerWidth = Mathf.Max(title.preferredWidth, labelWidth + ColumnGap + valueWidth);
            float width = innerWidth + PadX * 2f;
            float height = PadY * 2f + titleHeight + TitleGap + labels.Count * rowHeight + Mathf.Max(0, labels.Count - 1) * RowGap;
            rootRt.sizeDelta = new Vector2(width, height);

            Place(title.rectTransform, PadX, PadY, innerWidth, titleHeight);
            float y = PadY + titleHeight + TitleGap;
            for (int i = 0; i < labels.Count; i++)
            {
                Place(labels[i].rectTransform, PadX, y, innerWidth, rowHeight);
                Place(values[i].rectTransform, PadX, y, innerWidth, rowHeight);
                y += rowHeight + RowGap;
            }
            foreach (TextMeshProUGUI v in values) DrawPlusSigns(v, layer);
            return rootRt;
        }

        // In this typeface the "+" character is drawn as an ampersand-like ornament, so "C+" reads as "C&".
        // Hide that glyph and draw a plain plus sign (two bars) in the space it occupied.
        private static void DrawPlusSigns(TextMeshProUGUI text, int layer)
        {
            if (text.text.IndexOf('+') < 0) return;
            text.text = text.text.Replace("+", "<color=#00000000>+</color>");
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            Vector2 pivot = text.rectTransform.pivot;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (ch.character != '+') continue;
                // Glyph box in the text's local space (origin at its pivot).
                Vector2 centre = (ch.bottomLeft + ch.topRight) / 2f;
                float arm = Mathf.Min(ch.topRight.x - ch.bottomLeft.x, ch.topRight.y - ch.bottomLeft.y) * 0.72f;
                float thickness = arm * 0.3f;
                Bar(text.rectTransform, layer, pivot, centre, new Vector2(arm, thickness), text.color);
                Bar(text.rectTransform, layer, pivot, centre, new Vector2(thickness, arm), text.color);
            }
        }

        private static void Bar(RectTransform parent, int layer, Vector2 parentPivot, Vector2 centre, Vector2 size, Color color)
        {
            RectTransform rt = NewChild("PlusBar", parent, layer);
            rt.anchorMin = rt.anchorMax = parentPivot; // measure from the parent's pivot, like the glyph box
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = centre;
            rt.sizeDelta = size;
            var image = rt.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
        }

        private static RectTransform NewChild(string name, RectTransform parent, int layer)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static TextMeshProUGUI NewText(string name, RectTransform parent, int layer, TextMeshProUGUI tmpl,
            string text, float size, TextAlignmentOptions align)
        {
            RectTransform rt = NewChild(name, parent, layer);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset full = FullFont();
            if (full != null)
            {
                t.font = full; // uses that font's own default material
            }
            else
            {
                t.font = tmpl.font;
                t.fontSharedMaterial = tmpl.fontSharedMaterial;
            }
            t.fontStyle = tmpl.fontStyle;
            t.color = tmpl.color;
            t.fontSize = size;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            t.alignment = align;
            t.text = text;
            return t;
        }

        // Positions a child by its distance from the card's top-left corner.
        private static void Place(RectTransform rt, float x, float top, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -top);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
