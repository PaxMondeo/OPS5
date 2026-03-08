using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;
using System.IO;

namespace OPS5.Engine
{
    /// <summary>
    /// Manages named file handles for file I/O during rule execution.
    /// </summary>
    internal class FileHandleManager : IFileHandleManager
    {
        private readonly IOPS5Logger _logger;
        private readonly Dictionary<string, FileHandle> _handles = new Dictionary<string, FileHandle>(StringComparer.OrdinalIgnoreCase);

        public FileHandleManager(IOPS5Logger logger)
        {
            _logger = logger;
        }

        public void OpenFile(string logicalName, string filePath, string mode)
        {
            string key = logicalName.ToUpperInvariant();
            string modeUpper = mode.ToUpperInvariant();

            // Close existing handle if already open
            if (_handles.Remove(key, out var existing))
            {
                existing.Dispose();
                _logger.WriteInfo($"FileHandleManager: Closed existing handle '{logicalName}' before reopening", 2);
            }

            try
            {
                FileHandle handle;
                switch (modeUpper)
                {
                    case "OUT":
                        var writer = new StreamWriter(filePath, append: false);
                        handle = new FileHandle(logicalName, filePath, modeUpper, writer: writer);
                        break;

                    case "APPEND":
                        var appendWriter = new StreamWriter(filePath, append: true);
                        handle = new FileHandle(logicalName, filePath, modeUpper, writer: appendWriter);
                        break;

                    case "IN":
                        var reader = new StreamReader(filePath);
                        handle = new FileHandle(logicalName, filePath, modeUpper, reader: reader);
                        break;

                    default:
                        _logger.WriteError($"OpenFile: Unknown mode '{mode}' for file '{logicalName}'. Use In, Out, or Append.", "FileHandleManager");
                        return;
                }

                _handles[key] = handle;
                _logger.WriteInfo($"FileHandleManager: Opened '{logicalName}' ({filePath}) in {modeUpper} mode", 2);
            }
            catch (Exception ex)
            {
                _logger.WriteError($"OpenFile: Failed to open '{filePath}' as '{logicalName}': {ex.Message}", "FileHandleManager");
            }
        }

        public void CloseFile(string logicalName)
        {
            string key = logicalName.ToUpperInvariant();
            if (_handles.Remove(key, out var handle))
            {
                handle.Dispose();
                _logger.WriteInfo($"FileHandleManager: Closed '{logicalName}'", 2);
            }
            else
            {
                _logger.WriteError($"CloseFile: No open file handle named '{logicalName}'", "FileHandleManager");
            }
        }

        public void CloseAll()
        {
            foreach (var kvp in _handles)
            {
                kvp.Value.Dispose();
            }
            _handles.Clear();
        }

        public StreamWriter? GetWriter(string logicalName)
        {
            string key = logicalName.ToUpperInvariant();
            if (_handles.TryGetValue(key, out var handle))
            {
                if (handle.Writer != null)
                    return handle.Writer;

                _logger.WriteError($"GetWriter: File '{logicalName}' is opened for reading, not writing", "FileHandleManager");
                return null;
            }

            _logger.WriteError($"GetWriter: No open file handle named '{logicalName}'", "FileHandleManager");
            return null;
        }

        public StreamReader? GetReader(string logicalName)
        {
            string key = logicalName.ToUpperInvariant();
            if (_handles.TryGetValue(key, out var handle))
            {
                if (handle.Reader != null)
                    return handle.Reader;

                _logger.WriteError($"GetReader: File '{logicalName}' is opened for writing, not reading", "FileHandleManager");
                return null;
            }

            _logger.WriteError($"GetReader: No open file handle named '{logicalName}'", "FileHandleManager");
            return null;
        }

        public bool IsOpen(string logicalName)
        {
            return _handles.ContainsKey(logicalName.ToUpperInvariant());
        }

        /// <summary>
        /// Internal record holding the file stream and metadata.
        /// </summary>
        private class FileHandle : IDisposable
        {
            public string LogicalName { get; }
            public string FilePath { get; }
            public string Mode { get; }
            public StreamWriter? Writer { get; }
            public StreamReader? Reader { get; }

            public FileHandle(string logicalName, string filePath, string mode,
                              StreamWriter? writer = null, StreamReader? reader = null)
            {
                LogicalName = logicalName;
                FilePath = filePath;
                Mode = mode;
                Writer = writer;
                Reader = reader;
            }

            public void Dispose()
            {
                Writer?.Flush();
                Writer?.Dispose();
                Reader?.Dispose();
            }
        }
    }
}
