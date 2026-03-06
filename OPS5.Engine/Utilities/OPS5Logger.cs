using OPS5.Engine.Contracts;
using System;

namespace OPS5.Engine
{
    internal class OPS5Logger : IOPS5Logger
    {

        /// <summary>
        /// The verbosity setting for error reporting, minimum is 0, maximum is 2
        /// </summary>
        public int Verbosity { get; set; }

        /// <summary>
        /// The current number of errors encountered by the Engine
        /// </summary>
        public int ErrorCount { get; set; }

        public OPS5Logger()
        {
            ErrorCount = 0;
        }


        public void SetVerbosity(int v)
        {
            if (v < -1 || v > 2)
                WriteError($"Invalid verbosity setting {v}, must be between -1 and 2", "SetVerbosity");
            else
            {
                Verbosity = v;
                WriteInfo($"Verbosity = {v}", 0);
            }
        }

        public void WriteError(string message, string procedure)
        {
            message = $"{DateTime.Now.ToString("HH:mm:ss.fff")} " + message;
            message = $"{message} ({procedure})";
            Console.WriteLine($"ERROR: {message}");
            ErrorCount++;
        }

        public void WriteWarning(string message, string procedure)
        {
            message = $"{DateTime.Now.ToString("HH:mm:ss.fff")} " + message;
            message = $"{message} ({procedure})";
            Console.WriteLine($"WARNING: {message}");
        }

        public void WriteInfo(string message, int importance)
        {
            message = $"{DateTime.Now.ToString("HH:mm:ss.fff")} " + message;
            if (importance <= Verbosity)
            {
                Console.WriteLine(message);
            }
        }

        public void WriteOutput(string message)
        {
            Console.WriteLine(message);
        }

        public string? ReadInput(string? prompt = null)
        {
            if (prompt != null)
                Console.Write(prompt);

            string? line = Console.ReadLine();
            if (line == null)
                return null;

            string trimmed = line.TrimStart();
            int spaceIndex = trimmed.IndexOfAny(new[] { ' ', '\t' });
            return spaceIndex >= 0 ? trimmed.Substring(0, spaceIndex) : trimmed;
        }

        public string? ReadInputLine(string? prompt = null)
        {
            if (prompt != null)
                Console.Write(prompt);

            return Console.ReadLine();
        }

        public void ClearErrors()
        {
            ErrorCount = 0;
        }

    }

}
