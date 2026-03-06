namespace OPS5.Engine.Enumerations
{
    /// <summary>
    /// Conflict resolution strategies for the RETE engine's recognize-act cycle.
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        /// <summary>
        /// Means-Ends Analysis — orders by recency of first condition's WME,
        /// then specificity tiebreaker, fires ONE rule per cycle. (Default)
        /// </summary>
        MEA,

        /// <summary>
        /// Lexicographic — orders by most recent WME in each instantiation
        /// (sorted descending), then specificity tiebreaker, fires ONE rule per cycle.
        /// </summary>
        LEX,

        /// <summary>
        /// MEA ordering with all-fire — fires ALL matching instantiations per cycle
        /// rather than selecting a single winner.
        /// </summary>
        MEA_ALL
    }
}
