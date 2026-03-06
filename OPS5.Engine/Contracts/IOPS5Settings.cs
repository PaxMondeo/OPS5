using OPS5.Engine.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IOPS5Settings
    {
        public string ProjectName { get; set; }
        int Steps { get; set; }
        bool AutoRun { get; set; }

        /// <summary>
        /// The conflict resolution strategy used during the recognize-act cycle.
        /// Defaults to MEA (Means-Ends Analysis).
        /// </summary>
        ConflictResolutionStrategy Strategy { get; set; }
    }
}
