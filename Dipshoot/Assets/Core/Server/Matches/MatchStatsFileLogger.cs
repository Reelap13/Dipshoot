using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.MatchConfig;
using Game.MatchMode;
using Server.Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Server.Match
{
    internal static class MatchStatsFileLogger
    {
        private const string LogPrefix = "[MatchStatsLog]";
        private const string LogsDirectoryName = "Logs";

        public static void Write(MatchController match_controller)
        {
            if (match_controller == null || match_controller.MatchData == null)
                return;

            try
            {
                MatchStatsController stats_controller = FindStatsController(match_controller.SceneManager.Scene);
                if (stats_controller == null)
                {
                    Debug.LogWarning($"{LogPrefix} Match stats controller not found.");
                    return;
                }

                string directory = Path.Combine(Directory.GetCurrentDirectory(), LogsDirectoryName);
                Directory.CreateDirectory(directory);

                string preset_name = GetPresetName(match_controller.MatchData.LobbyData);
                string file_name = $"{BuildSafeTimestamp(DateTime.Now)}_{SanitizeFilePart(preset_name)}.csv";
                string path = Path.Combine(directory, file_name);

                File.WriteAllText(path, BuildLog(stats_controller), Encoding.UTF8);
                Debug.Log($"{LogPrefix} Written: {path}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} Failed: {exception}");
            }
        }

        private static string BuildLog(MatchStatsController stats_controller)
        {
            List<PlayerRoundStats> stats = new();
            foreach (PlayerRoundStats player_stats in stats_controller.PlayerStats)
                stats.Add(player_stats);

            stats.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

            StringBuilder builder = new();
            builder.AppendLine("Nickname;Kills;Deaths;CapturePresenceSeconds");

            for (int i = 0; i < stats.Count; i++)
            {
                PlayerRoundStats row = stats[i];
                string nickname = string.IsNullOrWhiteSpace(row.Nickname)
                    ? $"Player{row.PlayerId}"
                    : row.Nickname;

                builder
                    .Append(EscapeCsv(nickname))
                    .Append(';')
                    .Append(row.Kills)
                    .Append(';')
                    .Append(row.Deaths)
                    .Append(';')
                    .Append(row.CapturePresenceTime.ToString("0.###", CultureInfo.InvariantCulture))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static MatchStatsController FindStatsController(Scene scene)
        {
            if (!scene.IsValid())
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                MatchStatsController stats_controller = roots[i].GetComponentInChildren<MatchStatsController>(true);
                if (stats_controller != null)
                    return stats_controller;
            }

            return null;
        }

        private static string GetPresetName(LobbyData lobby)
        {
            if (lobby == null)
                return "UnknownPreset";

            MatchPreset preset = string.IsNullOrWhiteSpace(lobby.SelectedPresetId)
                ? null
                : MatchPresetRegistry.GetPreset(lobby.SelectedPresetId);

            if (preset != null && !string.IsNullOrWhiteSpace(preset.DisplayName))
                return preset.DisplayName;

            return string.IsNullOrWhiteSpace(lobby.SelectedPresetId)
                ? "UnknownPreset"
                : lobby.SelectedPresetId;
        }

        private static string BuildSafeTimestamp(DateTime time)
        {
            return SanitizeFilePart(time.ToString("dd:HH:mm:ss", CultureInfo.InvariantCulture));
        }

        private static string SanitizeFilePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            char[] invalid_chars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new(value.Trim());

            for (int i = 0; i < builder.Length; i++)
            {
                char symbol = builder[i];
                for (int j = 0; j < invalid_chars.Length; j++)
                {
                    if (symbol == invalid_chars[j])
                    {
                        builder[i] = '-';
                        break;
                    }
                }
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string value)
        {
            value ??= string.Empty;
            value = value.Replace("\r", " ").Replace("\n", " ");

            if (!value.Contains(";") && !value.Contains("\""))
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
