using System;
using System.Globalization;
using System.Reflection;
using HarmonyLib;

namespace CupheadCompanion
{
    // Answers "would beating this level with the relic equipped still power it up, and by how much?"
    // using the game's own relic rules.
    internal static class RelicInfo
    {
        private static readonly MethodInfo CurseCharmValue = AccessTools.Method(typeof(PlayerData), "CurseCharmValue");

        // Points the level would add to player one's relic if beaten with it now; 0 if none.
        // The game only awards them on Regular or Expert.
        internal static float PointsAvailable(Levels level)
        {
            PlayerData data = PlayerData.Data;
            if (!data.IsUnlocked(PlayerId.PlayerOne, Charm.charm_curse)) return 0f;
            if (CharmCurse.CalculateLevel(PlayerId.PlayerOne) < 0 || CharmCurse.IsMaxLevel(PlayerId.PlayerOne)) return 0f;
            if (Array.IndexOf(CharmCurse.CountableLevels, level) < 0) return 0f;
            if (data.GetLevelData(level).curseCharmP1) return 0f;
            if (CurseCharmValue == null) return 0f;
            return (float)CurseCharmValue.Invoke(null, new object[] { level });
        }

        internal static string Format(float points)
        {
            return points.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
