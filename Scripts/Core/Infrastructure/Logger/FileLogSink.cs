using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DRAUM.Core.Infrastructure.Logger
{
    /// <summary>
    /// Синхронный sink, который пишет LogEvent в файлы в текущей папке запуска.
    /// </summary>
    public sealed class FileLogSink : IDisposable
    {
        private readonly object _lock = new object();
        private readonly string _runFolderPath;

        private readonly Dictionary<string, StreamWriter> _writers = new Dictionary<string, StreamWriter>(16);

        public FileLogSink(string runFolderPath)
        {
            _runFolderPath = runFolderPath;
        }

        public void Write(in LogEvent evt, string moduleFileName)
        {
            if (string.IsNullOrEmpty(moduleFileName))
                moduleFileName = "Core.log";

            lock (_lock)
            {
                EnsureWriter(moduleFileName);
                var line = LogFormat.FormatLine(evt);
                _writers[moduleFileName].WriteLine(line);
            }
        }

        public void WriteAll(in LogEvent evt)
        {
            lock (_lock)
            {
                EnsureWriter("All.log");
                var line = LogFormat.FormatLine(evt);
                _writers["All.log"].WriteLine(line);
            }
        }

        private void EnsureWriter(string fileName)
        {
            if (_writers.ContainsKey(fileName))
                return;

            Directory.CreateDirectory(_runFolderPath);
            string path = Path.Combine(_runFolderPath, fileName);

            var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            var sw = new StreamWriter(fs, Encoding.UTF8);
            sw.AutoFlush = true;
            _writers[fileName] = sw;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var kvp in _writers)
                {
                    kvp.Value?.Dispose();
                }
                _writers.Clear();
            }
        }
    }
}

