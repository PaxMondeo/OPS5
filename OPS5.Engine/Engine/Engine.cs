using OPS5.Engine.Contracts;
using OPS5.Engine.Enumerations;
using AttributeLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OPS5.Engine
{
    internal class Engine : IEngine
    {
        private readonly IOPS5Logger _logger;
        private readonly IRHSActionExecutor _actionExecutor;
        private readonly IFileHandleManager _fileHandleManager;

        private IWorkingMemory _workingMemory;
        private IWMClasses _WMClasses;
        private IAlphaMemory _alphaMemory;
        private IBetaMemory _betaMemory;
        private IRules _rules;
        private ISourceFiles _sourceFiles;
        private IConflictItemFactory _conflictItemFactory;
        private IOPS5Console _console;
        private IOPS5Settings _settings;


        private bool _halt = false;
        private string _haltingRule = string.Empty;
        private bool _running = false;

        public int LastRunRuleFirings { get; private set; }
        public TimeSpan LastRunDuration { get; private set; }


        public Engine(IOPS5Logger logger,
                      IWorkingMemory workingMemory,
                      IWMClasses WMClasses,
                      IAlphaMemory alphaMemory,
                      IBetaMemory betaMemory,
                      IRules rules,
                      ISourceFiles sourceFiles,
                      IConflictItemFactory conflictItemFactory,
                      IOPS5Console console,
                      IOPS5Settings settings,
                      IRHSActionExecutor actionExecutor,
                      IFileHandleManager fileHandleManager)
        {
            _logger = logger;
            _workingMemory = workingMemory;
            _WMClasses = WMClasses;
            _alphaMemory = alphaMemory;
            _betaMemory = betaMemory;
            _rules = rules;
            _sourceFiles = sourceFiles;
            _conflictItemFactory = conflictItemFactory;
            _console = console;
            _settings = settings;
            _actionExecutor = actionExecutor;
            _fileHandleManager = fileHandleManager;

            Reset();
            _logger.WriteInfo($"Started", 0);
        }

        public event EventHandler<bool> RunStarted = default!;
        public event EventHandler<bool> RunComplete = default!;


        public async Task<bool> RunEngine(bool isDaemon)
        {
            bool exit = false;

            if (isDaemon)
                await Task.Delay(1000);
            else
            {
                if (_settings.AutoRun)
                {
                    _settings.AutoRun = false;
                    await Run();
                }
                else
                {
                    ConsoleResult result = await _console.RunConsole();
                    switch (result)
                    {
                        case ConsoleResult.Exit:
                            exit = true;
                            break;

                        case ConsoleResult.Reset:
                            Reset();
                            break;

                        case ConsoleResult.Run:
                            await Run();
                            break;

                        case ConsoleResult.RunSteps:
                            await Run(_settings.Steps);
                            break;

                        case ConsoleResult.Errors:
                            Console.WriteLine("\n\nPlease correct all errors before attempting to run\n\n");
                            break;
                    }
                }
            }
            return exit;
        }


        /// <summary>
        /// Resets the OPS5 Engine, clearing all code and data
        /// </summary>
        private void Reset()
        {
            _settings.ProjectName = "";
            IAlphaNode alphaRoot = _alphaMemory.Reset();
            IBetaNode betaRoot = _betaMemory.Reset();
            _workingMemory.Reset(alphaRoot, betaRoot);
            _rules.Reset();
            _WMClasses.Reset();
            _logger.Verbosity = 0;
            _fileHandleManager.CloseAll();
            _logger.ErrorCount = 0;
            string folder = "";
            //Maintain file path in case loading from REST
            if (_sourceFiles.ProjectFile != null && _sourceFiles.ProjectFile.FilePath != null)
                folder = _sourceFiles.ProjectFile.FilePath;
            _sourceFiles.ProjectFile = new SourceFile("", folder, "", "", false, false);
            _sourceFiles.ClassFiles = new Dictionary<string, SourceFile>(StringComparer.OrdinalIgnoreCase);
            _sourceFiles.RuleFiles = new Dictionary<string, SourceFile>(StringComparer.OrdinalIgnoreCase);
            _sourceFiles.BindingFile = new SourceFile("", "", "", "", false, false);
            _sourceFiles.DataFile = new SourceFile("", "", "", "", false, false);
            _sourceFiles.OPS5File = new SourceFile("", "", "", "", false, false);
            _logger.WriteInfo("Completed reset of OPS5 engine", 0);
        }



        /// <summary>
        /// Runs the OPS5 Engine until it runs out of things to do
        /// </summary>
        public async Task Run()
        {
            _halt = false;
            _haltingRule = string.Empty;
            _actionExecutor.ResetHalt();
            if (_running)
                _logger.WriteInfo("Skipping run as previous run has not completed.", 1);
            else
            {
                await Run(0);
            }
        }

        public void Halt()
        {
            _halt = true;
            _haltingRule = "External Input";
        }

        /// <summary>
        /// Runs the OPS5 Engine for the specified number of cycles
        /// </summary>
        /// <param name="maxSteps"></param>
        public async Task Run(int maxSteps)
        {
            await Run(maxSteps, true);
        }
        private async Task Run(int maxSteps, bool isInternal)
        {
            if(_logger.ErrorCount > 0)
            {
                _logger.WriteInfo($"Please execute ClearErrors to continue", 0);
            }
            else
            {
                _running = true;
                RunStarted?.Invoke(this, true);
                _logger.WriteInfo($"Started run", 0);

                // Reset per-rule fire counts
                foreach (var rule in _rules.GetRules())
                    rule.FireCount = 0;

                var stopwatch = Stopwatch.StartNew();

                int stepCount = 0;
                bool SomethingToDo = true;

                _workingMemory.InjectObjects();

                _halt = false;
                _haltingRule = string.Empty;
                _actionExecutor.ResetHalt();
                while (SomethingToDo && !_halt && !_actionExecutor.HaltRequested && (maxSteps == 0 || stepCount < maxSteps) && _logger.ErrorCount == 0)
                {

                    SomethingToDo = _settings.Strategy switch
                    {
                        ConflictResolutionStrategy.LEX => await ExecuteLEX(),
                        _ => await ExecuteMEA()
                    };

                    if (SomethingToDo)
                    {
                        //Now see if there are any incoming data objects to be injected into working memory
                        _workingMemory.InjectObjects();

                        stepCount++;
                    }
                    else
                        UnfireTokens();
                }

                bool halted = _halt || _actionExecutor.HaltRequested;
                if (halted)
                {
                    string haltRule = _actionExecutor.HaltRequested ? _actionExecutor.HaltingRule : _haltingRule;
                    await Task.Delay(1000); //To make sure this is displayed after the asynchronous messages
                    _logger.WriteInfo($"Halted by Rule {haltRule}", 0);
                    IRule? chosenRule = null;
                    IToken? chosenToken = null;
                    _rules.PrintRules(false, false);
                    List<IConflictItem> conflictSet = BuildConflictSet();
                    Recency(conflictSet);
                    Specificity(conflictSet, ref chosenRule, ref chosenToken);
                    if (chosenRule != null)
                        _logger.WriteInfo($"Next rule scheduled to fire: {chosenRule.Name}", 1);
                }
                else
                    WriteCompleted(stepCount);

                stopwatch.Stop();
                LastRunRuleFirings = stepCount;
                LastRunDuration = stopwatch.Elapsed;

                _running = false;
                RunComplete?.Invoke(this, true);
            }
        }

        private void WriteCompleted(int stepCount)
        {
            if (stepCount == 0)
            {
                _logger.WriteInfo($"Completed run with nothing to do", 0);
            }
            else
                _logger.WriteInfo($"Completed run after firing {stepCount} rules", 1);
        }

        private void LogRuleFiring(IRule rule, IToken token)
        {
            if (_logger.Verbosity > 0)
            {
                string objects = string.Join(" ", token.ObjectIDs.Select(id => $"#{id}"));
                _logger.WriteInfo($"Firing Rule {rule.ID} {rule.Name} with Objects\t{objects}", 1);
            }
        }

        private void UnfireTokens()
        {
            foreach (IRule p in _rules.GetEnabledRulesWithTokens())
            {
                foreach (IToken t in p.PNodeTokens())
                    t.Fired = false;
            }
        }

        #region Conflict Resolution Strategies

        /// <summary>
        /// MEA (Means-Ends Analysis) — orders by recency of first condition's WME (position 0),
        /// then subsequent positions as tiebreakers, then specificity. Fires ONE rule per cycle.
        /// </summary>
        private async Task<bool> ExecuteMEA()
        {
            bool foundSomethingToDo = false;

            try
            {
                List<IConflictItem> conflictSet = BuildConflictSet();
                Recency(conflictSet);

                IRule? chosenRule = null;
                IToken? chosenToken = null;
                if (conflictSet.Count > 0)
                {
                    Specificity(conflictSet, ref chosenRule, ref chosenToken);

                    if (chosenRule != null && chosenToken != null)
                    {
                        foundSomethingToDo = true;
                        await FireSingleRule(chosenRule, chosenToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Error executing MEA strategy: {ex.Message}", "MEA");
            }

            return foundSomethingToDo;
        }

        /// <summary>
        /// LEX (Lexicographic) — sorts each instantiation's recency list descending (most recent WME first),
        /// then compares position-by-position. Most recently touched WME anywhere in the instantiation wins.
        /// Then specificity tiebreaker. Fires ONE rule per cycle.
        /// </summary>
        private async Task<bool> ExecuteLEX()
        {
            bool foundSomethingToDo = false;

            try
            {
                List<IConflictItem> conflictSet = BuildConflictSet();
                LexRecency(conflictSet);

                IRule? chosenRule = null;
                IToken? chosenToken = null;
                if (conflictSet.Count > 0)
                {
                    Specificity(conflictSet, ref chosenRule, ref chosenToken);

                    if (chosenRule != null && chosenToken != null)
                    {
                        foundSomethingToDo = true;
                        await FireSingleRule(chosenRule, chosenToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Error executing LEX strategy: {ex.Message}", "LEX");
            }

            return foundSomethingToDo;
        }

        /// <summary>
        /// Fire a single chosen rule/token pair.
        /// </summary>
        private async Task FireSingleRule(IRule chosenRule, IToken chosenToken)
        {
            LogRuleFiring(chosenRule, chosenToken);
            chosenToken.Fired = true;
            chosenRule.FireCount++;
            await _actionExecutor.ExecuteActions(chosenRule, chosenToken);
            _console.WriteDots(chosenRule.Name);
        }

        #endregion

        #region Conflict Set Building and Ordering

        private List<IConflictItem> BuildConflictSet()
        {
            List<IConflictItem> conflictSet = new List<IConflictItem>();
            foreach (Rule rule in _rules.GetEnabledRulesWithTokens())
            {
                foreach (IToken token in rule.PNodeTokens().Where(t => !t.Fired))
                {
                    IConflictItem item = _conflictItemFactory.NewConflictItem();
                    item.SetProperties(token, rule);
                    conflictSet.Add(item);
                }
            }
            return conflictSet;
        }

        /// <summary>
        /// MEA recency ordering — compares recency values position-by-position (position 0 = first condition).
        /// Reduces conflict set to items with the highest recency at each position.
        /// </summary>
        private void Recency(List<IConflictItem> conflictSet)
        {
            List<IConflictItem> mostRecent = new List<IConflictItem>();
            if (conflictSet.Count > 1)
            {
                int x = 0;
                while (true)
                {
                    int bestRecency = 0;
                    foreach (IConflictItem item in conflictSet)
                    {
                        if (item.TheToken.Recency.Count > x && item.TheToken.Recency[x] > bestRecency)
                        {
                            bestRecency = item.TheToken.Recency[x];
                            mostRecent.Clear();
                            mostRecent.Add(item);
                        }
                        else if (item.TheToken.Recency.Count > x && item.TheToken.Recency[x] == bestRecency)
                        {
                            if (!mostRecent.Contains(item))
                            {
                                mostRecent.Add(item);
                            }
                        }
                    }
                    if (mostRecent.Count == 0)
                        break;

                    conflictSet.Clear();
                    foreach (IConflictItem item in mostRecent)
                        conflictSet.Add(item);
                    mostRecent.Clear();
                    if (conflictSet.Count == 1)
                        break;

                    x++;
                }
            }
        }

        /// <summary>
        /// LEX recency ordering — sorts each token's recency list descending (most recent WME first),
        /// then compares position-by-position like MEA. This means the instantiation with the
        /// most recently touched WME anywhere wins, regardless of condition position.
        /// </summary>
        private void LexRecency(List<IConflictItem> conflictSet)
        {
            if (conflictSet.Count <= 1)
                return;

            // Sort each token's recency list descending (most recent WME first)
            foreach (var item in conflictSet)
            {
                item.TheToken.Recency.Sort((a, b) => b.CompareTo(a));
            }

            // Then apply the same position-by-position comparison as MEA
            List<IConflictItem> mostRecent = new List<IConflictItem>();
            int x = 0;
            while (true)
            {
                int bestRecency = 0;
                foreach (IConflictItem item in conflictSet)
                {
                    if (item.TheToken.Recency.Count > x && item.TheToken.Recency[x] > bestRecency)
                    {
                        bestRecency = item.TheToken.Recency[x];
                        mostRecent.Clear();
                        mostRecent.Add(item);
                    }
                    else if (item.TheToken.Recency.Count > x && item.TheToken.Recency[x] == bestRecency)
                    {
                        if (!mostRecent.Contains(item))
                        {
                            mostRecent.Add(item);
                        }
                    }
                }
                if (mostRecent.Count == 0)
                    break;

                conflictSet.Clear();
                foreach (IConflictItem item in mostRecent)
                    conflictSet.Add(item);
                mostRecent.Clear();
                if (conflictSet.Count == 1)
                    break;

                x++;
            }
        }

        private void Specificity(List<IConflictItem> conflictSet, ref IRule? chosenRule, ref IToken? chosenToken)
        {
            int bestSpecificity = 0;
            foreach (IConflictItem item in conflictSet)
            {
                if (item.TheRule.Specificity > bestSpecificity)
                {
                    bestSpecificity = item.TheRule.Specificity;
                    chosenToken = item.TheToken;
                    chosenRule = item.TheRule;
                }
            }
        }

        #endregion


        /// <summary>
        /// Helper method to split a Rule RHS line into its components
        /// </summary>
        /// <param name="line"></param>
        /// <param name="remainder"></param>
        /// <returns></returns>
        public List<object> SplitRHSLine(string line, out string remainder)
        {
            List<object> atoms = new List<object>();
            if (line != "")
            {
                int x = 0;
                bool doing = true;
                string thisAtom = "";

                while (doing)
                {
                    switch (line[x])
                    {
                        case '(':
                            //Opening bracket, so something new
                            atoms.Add(SplitRHSLine(line.Substring(x + 1), out line));
                            x = -1; //Line returned above as remainder after sub
                            if (line == "")
                            {
                                doing = false;
                            }
                            break;
                        case ')':
                            //Closing bracket, so return what we have so far
                            if (thisAtom != "")
                            {
                                atoms.Add(thisAtom);
                            }
                            line = line.Substring(x + 1);
                            doing = false;
                            break;
                        case '|':
                            if (thisAtom == "")
                            {
                                thisAtom = "|";
                            }
                            else
                            {
                                thisAtom += "|";
                                atoms.Add(thisAtom);
                                thisAtom = "";
                            }
                            break;
                        case ' ':
                        case '\t':
                            if (thisAtom.StartsWith("|") && !(thisAtom.EndsWith("|") && thisAtom.Length > 1))
                            {
                                thisAtom += " ";
                            }
                            else
                            {
                                if (thisAtom != "")
                                {
                                    atoms.Add(thisAtom);
                                    thisAtom = "";
                                }
                            }
                            break;
                        default:
                            thisAtom += line[x];
                            break;
                    }
                    x++;
                }
            }
            remainder = line;
            return atoms;
        }

    }
}
