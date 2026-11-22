using System;
using HarmonyLib;

namespace CupheadCompanion
{
    // Diagnostic only: writes a line to BepInEx\LogOutput.log the moment a Run 'n Gun stops being a
    // pacifist run, naming the enemy that died and what kind of hit killed it. Changes nothing in the game.
    [HarmonyPatch(typeof(AbstractPlatformingLevelEnemy), "OnDamageTaken")]
    internal static class PacifistWatch
    {
        private static void Prefix(out bool __state)
        {
            __state = Level.ScoringData != null && Level.ScoringData.pacifistRun;
        }

        private static void Postfix(AbstractPlatformingLevelEnemy __instance, DamageDealer.DamageInfo info, bool __state)
        {
            try
            {
                if (!__state || Level.ScoringData == null || Level.ScoringData.pacifistRun) return;
                Plugin.Log.LogInfo(string.Format(
                    "Pacifist run ended: enemy '{0}' ({1}) was killed by a hit of {2} damage, source {3}, from {4}, {5:0.0}s into the level.",
                    __instance.name, __instance.GetType().Name, info.damage, info.damageSource, info.origin,
                    Level.Current != null ? Level.Current.LevelTime : -1f));
            }
            catch (Exception e) { Plugin.Log.LogError("Pacifist watch failed: " + e); }
        }
    }
}
