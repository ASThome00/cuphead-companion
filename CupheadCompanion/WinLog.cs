using System;
using System.Globalization;
using System.IO;
using System.Text;
using HarmonyLib;

namespace CupheadCompanion
{
    // Observes every level win. It records what the save held before the game updated it (used by the
    // results-screen extras) and appends one line per win to a log the companion tracker reads.
    // It only reads game state; nothing here changes the save.
    [HarmonyPatch(typeof(Level), "zHack_OnWin")]
    internal static class WinCapture
    {
        internal class Record
        {
            public Levels level;
            public bool previouslyWon;
            public LevelScoringData.Grade prevGrade;
            public float prevTime;
            public bool graded;
            public LevelScoringData.Grade grade;
            public float time;
            public Level.Mode difficulty;
            public bool newBestGrade, newBestTime;
        }

        // The most recent win, for the results screen that follows it.
        internal static Record Last;

        private static void Prefix(Level __instance, out Record __state)
        {
            __state = null;
            try
            {
                if (Level.IsTowerOfPower) return;
                PlayerData.PlayerLevelDataObject data = PlayerData.Data.GetLevelData(__instance.CurrentLevel);
                __state = new Record
                {
                    level = __instance.CurrentLevel,
                    previouslyWon = data.completed,
                    prevGrade = data.grade,
                    prevTime = data.bestTime,
                };
            }
            catch (Exception e) { Plugin.Log.LogError("Win capture (before) failed: " + e); }
        }

        private static void Postfix(Level __instance, Record __state)
        {
            if (__state == null) return;
            try
            {
                Record r = __state;
                // Same rule the game uses to decide whether a win gets a grade and time.
                r.graded = (__instance.LevelType == Level.Type.Battle || __instance.LevelType == Level.Type.Platforming)
                    && (!Level.IsDicePalace || Level.IsDicePalaceMain);
                r.difficulty = Level.Difficulty;
                if (r.graded)
                {
                    r.grade = Level.Grade;
                    r.time = Level.ScoringData.time;
                    r.newBestGrade = !r.previouslyWon || r.grade > r.prevGrade;
                    r.newBestTime = r.newBestGrade || (r.grade == r.prevGrade && r.time < r.prevTime);
                }
                Last = r;
                if (Plugin.WinLog.Value) WinLogFile.Append(r);
            }
            catch (Exception e) { Plugin.Log.LogError("Win capture (after) failed: " + e); }
        }
    }

    // One JSON object per line in %LOCALAPPDATA%\CupheadCompanion\wins.jsonl.
    internal static class WinLogFile
    {
        internal static string FilePath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CupheadCompanion");
                return Path.Combine(dir, "wins.jsonl");
            }
        }

        internal static void Append(WinCapture.Record r)
        {
            bool coop = PlayerManager.Multiplayer;
            bool p1Chalice = PlayerManager.playerWasChalice[0];
            bool p2Chalice = coop && PlayerManager.playerWasChalice[1];
            string p1 = p1Chalice ? "Ms. Chalice" : (PlayerManager.player1IsMugman ? "Mugman" : "Cuphead");
            string p2 = !coop ? null : (p2Chalice ? "Ms. Chalice" : (PlayerManager.player1IsMugman ? "Cuphead" : "Mugman"));
            long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            var sb = new StringBuilder("{");
            Num(sb, "ts", ts.ToString(CultureInfo.InvariantCulture));
            Num(sb, "slot", PlayerData.CurrentSaveFileIndex.ToString(CultureInfo.InvariantCulture));
            Num(sb, "levelId", ((int)r.level).ToString(CultureInfo.InvariantCulture));
            Str(sb, "level", r.level.ToString());
            Num(sb, "difficulty", ((int)r.difficulty).ToString(CultureInfo.InvariantCulture));
            Num(sb, "graded", r.graded ? "true" : "false");
            Num(sb, "grade", r.graded ? ((int)r.grade).ToString(CultureInfo.InvariantCulture) : "null");
            Num(sb, "time", r.graded ? r.time.ToString("0.###", CultureInfo.InvariantCulture) : "null");
            Num(sb, "firstClear", r.previouslyWon ? "false" : "true");
            Num(sb, "newBestGrade", r.newBestGrade ? "true" : "false");
            Num(sb, "newBestTime", r.newBestTime ? "true" : "false");
            Num(sb, "coop", coop ? "true" : "false");
            Str(sb, "p1", p1);
            if (p2 != null) Str(sb, "p2", p2);
            sb.Length -= 1; // trailing comma
            sb.Append("}\n");

            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static void Num(StringBuilder sb, string key, string raw)
        {
            sb.Append('"').Append(key).Append("\":").Append(raw).Append(',');
        }

        private static void Str(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(key).Append("\":\"").Append(value.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\",");
        }
    }
}
