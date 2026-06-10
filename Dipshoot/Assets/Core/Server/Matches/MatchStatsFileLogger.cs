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
    public sealed class MatchLogContext : MonoBehaviour
    {
        private const string LogsDirectoryName = "Logs";
        private const string MatchLogsDirectoryName = "Matches";
        private static readonly Dictionary<int, MatchLogContext> ContextsByScene = new();
        private static readonly object FileLock = new();

        public string DirectoryPath { get; private set; }
        public MatchController MatchController { get; private set; }

        public static MatchLogContext Get(Scene scene)
        {
            return scene.IsValid() && ContextsByScene.TryGetValue(scene.handle, out MatchLogContext context)
                ? context
                : null;
        }

        public void Initialize(MatchController match_controller)
        {
            MatchController = match_controller;
            DirectoryPath = BuildDirectoryPath(match_controller);
            Directory.CreateDirectory(DirectoryPath);
            ContextsByScene[gameObject.scene.handle] = this;
            Application.logMessageReceivedThreaded += HandleApplicationLog;
            Write("match", $"created matchId={match_controller.MatchData.MatchId} guid={match_controller.MatchData.Guid} preset={GetPresetId(match_controller)} seed={GetSeed(match_controller)} scene={gameObject.scene.name}");
        }

        public void Write(string log_name, string message)
        {
            if (string.IsNullOrWhiteSpace(DirectoryPath))
                return;

            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
            string path = Path.Combine(DirectoryPath, $"{SanitizeFilePart(log_name)}.log");
            lock (FileLock)
                File.AppendAllText(path, line, Encoding.UTF8);
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= HandleApplicationLog;
            if (gameObject.scene.IsValid())
                ContextsByScene.Remove(gameObject.scene.handle);
        }

        private void HandleApplicationLog(string condition, string stack_trace, LogType type)
        {
            if (type != LogType.Warning && type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            Write("errors", $"{type}: {condition}");
            if (!string.IsNullOrWhiteSpace(stack_trace) && type != LogType.Warning)
                Write("errors", stack_trace);
        }

        private static string BuildDirectoryPath(MatchController match_controller)
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), LogsDirectoryName, MatchLogsDirectoryName);
            string preset = GetPresetId(match_controller);
            int seed = GetSeed(match_controller);
            string guid = match_controller.MatchData.Guid.ToString("N")[..8];
            string folder = $"{DateTime.Now:dd.MM.yyyy-HH-mm-ss}_{SanitizeFilePart(preset)}_seed{seed}_{guid}";
            return Path.Combine(root, folder);
        }

        private static string GetPresetId(MatchController match_controller)
        {
            string preset_id = match_controller?.MatchData?.LobbyData?.SelectedPresetId;
            if (!string.IsNullOrWhiteSpace(preset_id))
                return preset_id;

            MatchPreset preset = MatchPresetRegistry.LoadDefault()?.GetDefault();
            return preset == null ? "UnknownPreset" : preset.Id;
        }

        private static int GetSeed(MatchController match_controller)
        {
            return match_controller?.MatchData?.LobbyData?.SelectedSeed ?? 0;
        }

        private static string SanitizeFilePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            char[] invalid_chars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new(value.Trim());
            for (int i = 0; i < builder.Length; i++)
            {
                for (int j = 0; j < invalid_chars.Length; j++)
                {
                    if (builder[i] == invalid_chars[j])
                    {
                        builder[i] = '-';
                        break;
                    }
                }
            }

            return builder.ToString();
        }
    }

    internal static class MatchStatsFileLogger
    {
        private const string LogPrefix = "[MatchStatsLog]";
        private const string LogsDirectoryName = "Logs";
        private const string MatchLogsDirectoryName = "Matches";

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

                MatchLogContext log_context = MatchLogContext.Get(match_controller.SceneManager.Scene);
                string directory = log_context == null
                    ? Path.Combine(Directory.GetCurrentDirectory(), LogsDirectoryName, MatchLogsDirectoryName)
                    : log_context.DirectoryPath;
                Directory.CreateDirectory(directory);

                string file_name = log_context == null
                    ? $"{BuildSafeTimestamp(DateTime.Now)}_{SanitizeFilePart(GetPresetName(match_controller.MatchData.LobbyData))}.csv"
                    : "stats.csv";
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
            builder.AppendLine("Team;Nickname;Kills;Deaths;CapturePresenceSeconds");

            for (int i = 0; i < stats.Count; i++)
            {
                PlayerRoundStats row = stats[i];
                string nickname = string.IsNullOrWhiteSpace(row.Nickname)
                    ? $"Player{row.PlayerId}"
                    : row.Nickname;

                builder
                    .Append(row.TeamId)
                    .Append(';')
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
