using System;
using System.Collections.Generic;

namespace CupheadCompanion
{
    // Turns the game's own record for a level into the rows shown in the popup. Read-only.
    public class LevelInfo
    {
        public string Title;
        public readonly List<KeyValuePair<string, string>> Rows = new List<KeyValuePair<string, string>>();

        private static readonly string[] Grades =
        {
            "D-", "D", "D+", "C-", "C", "C+", "B-", "B", "B+", "A-", "A", "A+", "S", "P"
        };

        // Map entrances that are not graded fights, so a stats popup would be noise.
        private static readonly Levels[] Skipped =
        {
            Levels.Mausoleum, Levels.ShmupTutorial, Levels.ChaliceTutorial,
            Levels.Tutorial, Levels.House, Levels.DiceGate, Levels.Kitchen
        };

        // The King of Games' champions, in the order of Level.kingOfGamesLevels.
        private static readonly string[] ChampionNames = { "THE PAWNS", "THE KNIGHT", "THE BISHOP", "THE ROOK", "THE QUEEN" };

        // The ladder to the King of Games' castle leads to five fights, so its card summarises all of them.
        private static LevelInfo ForKingOfGames()
        {
            var info = new LevelInfo { Title = "KING OF GAMES" };
            for (int i = 0; i < Level.kingOfGamesLevels.Length; i++)
            {
                PlayerData.PlayerLevelDataObject data = PlayerData.Data.GetLevelData(Level.kingOfGamesLevels[i]);
                string name = i < ChampionNames.Length ? ChampionNames[i] : Level.kingOfGamesLevels[i].ToString().ToUpper();
                info.Add(name, data.completed ? Grades[(int)data.grade] + "   " + FormatTime(data.bestTime) : "NOT BEATEN");
            }
            info.Add("GAUNTLET", PlayerData.Data.GetLevelData(Levels.ChessCastle).completed ? "BEATEN" : "NOT BEATEN");
            return info;
        }

        public static bool IsTracked(Levels level)
        {
            return Array.IndexOf(Skipped, level) < 0;
        }

        private static bool CountsForRelic(Levels level)
        {
            return Array.IndexOf(Level.world1BossLevels, level) >= 0
                || Array.IndexOf(Level.world2BossLevels, level) >= 0
                || Array.IndexOf(Level.world3BossLevels, level) >= 0
                || Array.IndexOf(Level.world4BossLevels, level) >= 0
                || Array.IndexOf(Level.worldDLCBossLevelsWithSaltbaker, level) >= 0;
        }

        public static LevelInfo For(Levels level)
        {
            if (level == Levels.ChessCastle) return ForKingOfGames();
            var info = new LevelInfo();
            info.Title = level.ToString();
            try
            {
                string name = Localization.Translate(level.ToString()).text;
                if (!string.IsNullOrEmpty(name)) info.Title = name.Replace("\\n", " ");
            }
            catch (Exception) { /* keep the fallback name */ }
            info.Title = info.Title.ToUpper();

            PlayerData.PlayerLevelDataObject data = PlayerData.Data.GetLevelData(level);
            bool platforming = Array.IndexOf(Level.platformingLevels, level) >= 0;

            if (!data.completed)
            {
                info.Add("STATUS", data.played ? "NOT BEATEN YET" : "NOT ATTEMPTED");
            }
            else
            {
                info.Add("BEST GRADE", Grades[(int)data.grade]);
                info.Add("BEST TIME", FormatTime(data.bestTime));
                if (!platforming) info.Add("DIFFICULTY", DifficultyName(data.difficultyBeaten));
            }
            info.Add("MS. CHALICE", data.completedAsChaliceP1 || data.completedAsChaliceP2 ? "YES" : "NO");
            if (CountsForRelic(level)) info.Add("CURSED RELIC", data.curseCharmP1 || data.curseCharmP2 ? "YES" : "NO");
            return info;
        }

        private void Add(string label, string value)
        {
            Rows.Add(new KeyValuePair<string, string>(label, value));
        }

        private static string DifficultyName(Level.Mode mode)
        {
            switch (mode)
            {
                case Level.Mode.Easy: return "SIMPLE";
                case Level.Mode.Hard: return "EXPERT";
                default: return "REGULAR";
            }
        }

        internal static string FormatTime(float seconds)
        {
            if (seconds >= float.MaxValue) return "-";
            int total = (int)seconds;
            return string.Format("{0}:{1:00}", total / 60, total % 60);
        }
    }
}
