using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace CupheadCompanion
{
    // The reflecting pool's spirit (Quadratus) already tells you how often you've died. While you stand
    // at the pool, this shows a card with the rest of your record: totals the game keeps but never shows.
    [HarmonyPatch(typeof(MapNPCQuadratus))]
    internal static class PoolRecord
    {
        private static LevelPopup popup;

        // The pool has no button prompt; the game calls Show when a player walks into range.
        [HarmonyPostfix, HarmonyPatch("Show")]
        private static void AfterShow(MapNPCQuadratus __instance, PlayerInput player)
        {
            try
            {
                if (!Plugin.PoolRecordCard.Value || player == null || popup != null) return;
                popup = LevelPopup.Create(BuildRecord(), player.transform, __instance.dialogueOffset, true);
            }
            catch (Exception e) { Plugin.Log.LogError("Pool record card failed: " + e); }
        }

        [HarmonyPostfix, HarmonyPatch("Hide")]
        private static void AfterHide()
        {
            if (popup != null) popup.Close();
            popup = null;
        }

        private static LevelInfo BuildRecord()
        {
            PlayerData data = PlayerData.Data;
            var info = new LevelInfo { Title = "YOUR RECORD" };

            var bosses = new List<Levels>();
            bosses.AddRange(Level.world1BossLevels);
            bosses.AddRange(Level.world2BossLevels);
            bosses.AddRange(Level.world3BossLevels);
            bosses.AddRange(Level.world4BossLevels);
            if (DLCManager.DLCEnabled())
            {
                bosses.AddRange(Level.worldDLCBossLevelsWithSaltbaker);
                bosses.Add(Levels.Graveyard);
            }
            Levels[] all = bosses.ToArray();
            string ofTotal = " OF " + all.Length;

            info.Add("DEATHS", data.DeathCount(PlayerId.Any).ToString());
            info.Add("BEST PARRY STREAK", data.GetNumParriesInRow(PlayerId.Any).ToString());
            info.Add("BOSSES BEATEN", data.CountLevelsCompleted(all) + ofTotal);
            info.Add("A RANKS", data.CountLevelsHaveMinGrade(all, LevelScoringData.Grade.AMinus) + ofTotal);
            info.Add("ON EXPERT", data.CountLevelsHaveMinDifficulty(all, Level.Mode.Hard) + ofTotal);
            // The save-select percentage, split the way the game adds it up.
            info.Add("BASE GAME", Number(data.GetCompletionPercentage()) + " OF 200");
            if (DLCManager.DLCEnabled()) info.Add("DLC", Number(data.GetCompletionPercentageDLC()) + " OF 100");
            info.Add("WISHES LEFT", data.djimmiWishes.ToString());

            // Boss wins per character, if the win log has been recording this save slot.
            Dictionary<string, int> wins = CountWins(PlayerData.CurrentSaveFileIndex, all);
            if (wins != null)
            {
                foreach (string who in new[] { "Cuphead", "Mugman", "Ms. Chalice" })
                {
                    int n;
                    wins.TryGetValue(who, out n);
                    info.Add(who.ToUpper() + " WINS", n.ToString());
                }
            }
            return info;
        }

        private static string Number(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static readonly Regex SlotField = new Regex("\"slot\":(\\d+)");
        private static readonly Regex LevelField = new Regex("\"levelId\":(-?\\d+)");
        private static readonly Regex PlayerField = new Regex("\"p[12]\":\"([^\"]+)\"");

        // Reads the mod's own win log. Returns null when there is nothing logged for this slot.
        private static Dictionary<string, int> CountWins(int slot, Levels[] bosses)
        {
            try
            {
                string path = WinLogFile.FilePath;
                if (!File.Exists(path)) return null;
                Dictionary<string, int> wins = null;
                foreach (string line in File.ReadAllLines(path))
                {
                    Match s = SlotField.Match(line), l = LevelField.Match(line);
                    if (!s.Success || !l.Success) continue;
                    if (int.Parse(s.Groups[1].Value) != slot) continue;
                    if (Array.IndexOf(bosses, (Levels)int.Parse(l.Groups[1].Value)) < 0) continue;
                    if (wins == null) wins = new Dictionary<string, int>();
                    foreach (Match p in PlayerField.Matches(line))
                    {
                        int n;
                        wins.TryGetValue(p.Groups[1].Value, out n);
                        wins[p.Groups[1].Value] = n + 1;
                    }
                }
                return wins;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not read the win log: " + e.Message);
                return null;
            }
        }
    }
}
