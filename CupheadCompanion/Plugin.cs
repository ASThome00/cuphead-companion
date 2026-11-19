using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CupheadCompanion
{
    // Vanilla-friendly helper mod: quality-of-life additions in the game's own style.
    // It never changes combat, difficulty, grades or save data. Every feature has its own on/off setting
    // in BepInEx\config\cuphead.companion.cfg.
    [BepInPlugin(Guid, "Cuphead Companion", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "cuphead.companion";
        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> SkipIntro, MapPopup, ChecklistIcons, ProgressPanel, ResultsTopTime, WinLog, FastRetry;

        private void Awake()
        {
            Log = Logger;
            SkipIntro = Config.Bind("Helpers", "SkipStudioIntro", true,
                "Skip the Studio MDHR logo and go straight to the title screen.");
            MapPopup = Config.Bind("Info", "MapLevelPopup", true,
                "Show a stats card (grade, time, difficulty, Ms. Chalice, Cursed Relic) when you walk up to a level on the map.");
            ChecklistIcons = Config.Bind("Info", "ChecklistIcons", true,
                "Show Astral Cookie and relic icons on checklist rows beaten as Ms. Chalice or with the Cursed Relic.");
            ProgressPanel = Config.Bind("Info", "ChecklistProgressPanel", true,
                "Show relic power and filter / recolour unlock progress on a card beside the equip menu's checklist.");
            ResultsTopTime = Config.Bind("Info", "ResultsTopTime", true,
                "On the results screen, show your previous top time under the top grade and say when you beat it.");
            WinLog = Config.Bind("Tracker", "LogWins", false,
                "Off by default. Record every win (level, grade, time, who was playing) as one line of JSON in %LOCALAPPDATA%\\CupheadCompanion\\wins.jsonl, for your own stats tools. Nothing is sent anywhere.");
            FastRetry = Config.Bind("Helpers", "FastRetry", false,
                "Changes pacing, so off by default: press confirm as soon as YOU DIED appears to retry at once, without waiting for the death card.");
            new Harmony(Guid).PatchAll();
            Logger.LogInfo("Cuphead Companion loaded");
        }
    }

    // Shows a stats card whenever the game shows its "ENTER" prompt at a level entrance on the map.
    [HarmonyPatch(typeof(AbstractMapInteractiveEntity))]
    internal static class MapEntityPatches
    {
        private static readonly Dictionary<AbstractMapInteractiveEntity, LevelPopup> Popups =
            new Dictionary<AbstractMapInteractiveEntity, LevelPopup>();

        [HarmonyPostfix, HarmonyPatch("Show")]
        private static void AfterShow(AbstractMapInteractiveEntity __instance, PlayerInput player)
        {
            try
            {
                if (player == null || !Plugin.MapPopup.Value) return;
                // Most entrances are MapLevelLoaders; the bakery (Chef Saltbaker) and the graveyard
                // (secret boss) have their own entrance types.
                Levels level;
                var loader = __instance as MapLevelLoader;
                if (loader != null) level = (Levels)AccessTools.Field(typeof(MapLevelLoader), "level").GetValue(loader);
                else if (__instance is MapBakeryLoader) level = Levels.Saltbaker;
                else if (__instance is MapGraveyardHandler) level = Levels.Graveyard;
                else return;
                if (!LevelInfo.IsTracked(level)) return;

                LevelPopup existing;
                if (Popups.TryGetValue(__instance, out existing) && existing != null) return;
                Popups[__instance] = LevelPopup.Create(level, player.transform, __instance.dialogueOffset);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not show level popup: " + e);
            }
        }

        [HarmonyPostfix, HarmonyPatch("Hide")]
        private static void AfterHide(AbstractMapInteractiveEntity __instance)
        {
            Close(__instance);
        }

        [HarmonyPostfix, HarmonyPatch("Activate", new[] { typeof(MapPlayerController) })]
        private static void AfterActivate(AbstractMapInteractiveEntity __instance)
        {
            Close(__instance);
        }

        private static void Close(AbstractMapInteractiveEntity entity)
        {
            try
            {
                LevelPopup popup;
                if (!Popups.TryGetValue(entity, out popup)) return;
                Popups.Remove(entity);
                if (popup != null) popup.Close();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not close level popup: " + e);
            }
        }
    }
}
