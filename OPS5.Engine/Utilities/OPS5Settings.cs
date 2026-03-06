using OPS5.Engine.Contracts;
using OPS5.Engine.Enumerations;

namespace OPS5.Engine
{
    internal class OPS5Settings : IOPS5Settings
    {
        public string ProjectName { get; set; } = "";
        public int Steps { get; set; } = 1;
        public bool AutoRun { get; set; } = false;
        public ConflictResolutionStrategy Strategy { get; set; } = ConflictResolutionStrategy.MEA;
    }
}
