using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace CupheadCompanion
{
    // Skips the Studio MDHR logo that plays every time the title screen loads.
    // Replaces StartScreen.loop_cr with a version that does the same setup without the logo and its waits.
    [HarmonyPatch(typeof(StartScreen), "loop_cr")]
    internal static class IntroSkip
    {
        private const float FadeTime = 0.3f;

        private static bool Prefix(StartScreen __instance, ref IEnumerator __result)
        {
            if (!Plugin.SkipIntro.Value) return true;
            __result = TitleWithoutLogo(__instance);
            return false;
        }

        private static IEnumerator TitleWithoutLogo(StartScreen screen)
        {
            Traverse t = Traverse.Create(screen);
            var fader = t.Field("fader").GetValue<SpriteRenderer>();
            var splash = t.Field("mdhrSplash").GetValue<Component>();
            var title = t.Field("titleAnimation").GetValue<GameObject>();
            var titleDlc = t.Field("titleAnimationDLC").GetValue<GameObject>();

            // Let the scene finish its own first-frame setup, as the original does before touching audio.
            yield return null;
            yield return null;

            SpriteRenderer splashRenderer = splash != null ? splash.GetComponent<SpriteRenderer>() : null;
            if (splashRenderer != null) splashRenderer.enabled = false;

            // Same music and state setup the game does once the logo has finished.
            AudioManager.SnapshotReset(Scenes.scene_title.ToString(), 0.3f);
            bool forceOriginal = t.Method("forceOriginalTitleScreen").GetValue<bool>();
            if (!CreditsScreen.goodEnding)
            {
                AudioManager.PlayBGM();
            }
            else if (DLCManager.DLCEnabled() && !forceOriginal)
            {
                AudioManager.StartBGMAlternate(0);
                if (title != null) title.SetActive(false);
                if (titleDlc != null) titleDlc.SetActive(true);
            }
            else
            {
                AudioManager.PlayBGMPlaylistManually(true);
            }
            StartScreen.initialLoadData = null;
            CreditsScreen.goodEnding = true;
            SettingsData.Data.hasBootedUpGame = true;

            // Short fade from black so the title doesn't pop in.
            if (fader != null)
            {
                Color c = fader.color;
                for (float time = 0f; time < FadeTime; time += Time.deltaTime)
                {
                    c.a = 1f - time / FadeTime;
                    fader.color = c;
                    yield return null;
                }
                c.a = 0f;
                fader.color = c;
            }

            t.Property("state").SetValue(StartScreen.State.Title);
            var joined = typeof(PlayerManager).GetEvent("OnPlayerJoinedEvent");
            Delegate handler = Delegate.CreateDelegate(joined.EventHandlerType, screen,
                AccessTools.Method(typeof(StartScreen), "onPlayerJoined"));
            joined.AddEventHandler(null, handler);
            PlayerManager.SetPlayerCanJoin(PlayerId.PlayerOne, true, false);
        }
    }
}
