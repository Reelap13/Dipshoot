using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Core.Logging
{
    public sealed class MainFileLogger : MonoBehaviour
    {
        private static MainFileLogger _instance;

        private readonly object _lock = new();
        private StreamWriter _writer;
        private string _path;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
                return;

            GameObject target = new("MainFileLogger");
            DontDestroyOnLoad(target);
            _instance = target.AddComponent<MainFileLogger>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            OpenWriter();
            Application.logMessageReceivedThreaded += HandleLogMessage;
            Debug.Log($"[MainFileLogger] Writing logs to {_path}");
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= HandleLogMessage;
            CloseWriter();
        }

        private void OnApplicationQuit()
        {
            CloseWriter();
        }

        private void OpenWriter()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Main");
            Directory.CreateDirectory(directory);

            string file_name = DateTime.Now.ToString("'Logs 'dd.MM.yyyy-H-mm-ss'.log'", CultureInfo.InvariantCulture);
            _path = Path.Combine(directory, file_name);
            _writer = new StreamWriter(_path, false, new UTF8Encoding(false)) { AutoFlush = true };
            _writer.WriteLine($"[{DateTime.Now:O}] Log started");
        }

        private void CloseWriter()
        {
            lock (_lock)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            lock (_lock)
            {
                if (_writer == null)
                    return;

                _writer.Write('[');
                _writer.Write(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
                _writer.Write("] [");
                _writer.Write(type);
                _writer.Write("] ");
                _writer.WriteLine(condition);

                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    _writer.WriteLine(stackTrace);
            }
        }
    }
}
