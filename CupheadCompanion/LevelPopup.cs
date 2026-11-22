using UnityEngine;

namespace CupheadCompanion
{
    // A stats card that floats above the player on the world map, just above the game's "ENTER" prompt.
    public class LevelPopup : MonoBehaviour
    {
        private const float PromptHalfHeight = 26f, GapAbovePrompt = 10f;
        private const float TweenTime = 0.2f;
        private const float ScreenMargin = 24f, SideGap = 60f;

        private Transform target;
        private Vector2 offset;
        private float scale;
        private bool closing;
        private bool beside;

        public static LevelPopup Create(Levels level, Transform target, Vector2 dialogueOffset)
        {
            return Create(LevelInfo.For(level), target, dialogueOffset, false);
        }

        // `beside` puts the card next to the player instead of above the prompt, for places with no prompt.
        public static LevelPopup Create(LevelInfo info, Transform target, Vector2 dialogueOffset, bool beside)
        {
            Canvas canvas = MapUI.Current.sceneCanvas;
            float titleSize = CardBuilder.PromptFontSize;
            RectTransform card = CardBuilder.Build(canvas.transform, canvas.gameObject.layer, info.Title, info.Rows,
                titleSize, Mathf.Round(titleSize * 0.8f));
            card.gameObject.name = "CompanionLevelPopup";
            card.pivot = new Vector2(0.5f, 0f);

            var popup = card.gameObject.AddComponent<LevelPopup>();
            popup.target = target;
            popup.offset = dialogueOffset;
            popup.beside = beside;
            card.localScale = Vector3.zero;
            popup.UpdatePosition();
            return popup;
        }

        public void Close()
        {
            closing = true;
        }

        private void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            scale = Mathf.MoveTowards(scale, closing ? 0f : 1f, Time.unscaledDeltaTime / TweenTime);
            if (closing && scale <= 0f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, scale);
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            // Sit just above the game's own prompt bubble, which is centred on target + offset.
            float unitsPerPixel = transform.parent.lossyScale.y;
            Vector2 prompt = (Vector2)target.position + offset;
            Vector2 p = prompt;
            p.y += (PromptHalfHeight + GapAbovePrompt) * unitsPerPixel;

            // Keep the whole card on screen. If it would run off the top, sit beside the player instead.
            Canvas canvas = transform.parent.GetComponent<Canvas>();
            Camera cam = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (cam != null && cam.orthographic)
            {
                Vector2 size = ((RectTransform)transform).sizeDelta * unitsPerPixel;
                float margin = ScreenMargin * unitsPerPixel;
                float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
                Vector2 c = cam.transform.position;
                float top = c.y + halfH - margin, bottom = c.y - halfH + margin;
                float left = c.x - halfW + margin, right = c.x + halfW - margin;
                if (beside || p.y + size.y > top)
                {
                    float side = SideGap * unitsPerPixel + size.x / 2f;
                    p.x = prompt.x + side + size.x / 2f <= right ? prompt.x + side : prompt.x - side;
                    p.y = Mathf.Clamp(prompt.y - size.y / 2f, bottom, top - size.y);
                }
                if (right - left > size.x) p.x = Mathf.Clamp(p.x, left + size.x / 2f, right - size.x / 2f);
            }
            transform.position = new Vector3(p.x, p.y, transform.parent.position.z);
        }
    }
}
