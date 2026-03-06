using System.IO;

namespace OPS5.Engine.Contracts
{
    /// <summary>
    /// Manages named file handles for file I/O operations in rules.
    /// Supports opening files for reading (In), writing (Out), or appending (Append).
    /// </summary>
    public interface IFileHandleManager
    {
        /// <summary>
        /// Opens a file with the specified logical name and mode.
        /// </summary>
        /// <param name="logicalName">Logical name used to reference the file in rules</param>
        /// <param name="filePath">Physical file path</param>
        /// <param name="mode">File mode: "IN" (read), "OUT" (write/create), or "APPEND"</param>
        void OpenFile(string logicalName, string filePath, string mode);

        /// <summary>
        /// Closes a file handle by logical name, disposing its stream.
        /// </summary>
        void CloseFile(string logicalName);

        /// <summary>
        /// Closes all open file handles. Called on engine reset.
        /// </summary>
        void CloseAll();

        /// <summary>
        /// Gets the StreamWriter for a file opened in Out or Append mode.
        /// Returns null if the file is not open or was opened for reading.
        /// </summary>
        StreamWriter? GetWriter(string logicalName);

        /// <summary>
        /// Gets the StreamReader for a file opened in In mode.
        /// Returns null if the file is not open or was opened for writing.
        /// </summary>
        StreamReader? GetReader(string logicalName);

        /// <summary>
        /// Returns true if a file handle with the given logical name is currently open.
        /// </summary>
        bool IsOpen(string logicalName);
    }
}
