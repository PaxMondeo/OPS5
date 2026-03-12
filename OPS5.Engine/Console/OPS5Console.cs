using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OPS5.Engine.Commands
{
    internal class OPS5Console : IOPS5Console
    {
        private readonly IOPS5Logger _logger;
        private readonly IUtils _parserUtils;
        private readonly ICalculators _calculators;

        private IFileProcessing _fileProcessing;
        private IWorkingMemory _workingMemory;
        private IAlphaMemory _alphaMemory;
        private IBetaMemory _betaMemory;
        private IRules _rules;
        private IWMClasses _WMClasses;
        private IOPS5Settings _settings;

        private string _lastRuleFired = "";
        private int _ruleSeq = 0;

        public OPS5Console(IOPS5Logger logger,
                            IUtils parserUtils,
                            IFileProcessing fileProcessing,
                            IWorkingMemory workingMemory,
                            IAlphaMemory alphaMemory,
                            IBetaMemory betaMemory,
                            IRules rules,
                            IWMClasses WMClasses,
                            ICalculators calculators,
                            IOPS5Settings settings)
        {
            _parserUtils = parserUtils;
            _fileProcessing = fileProcessing;
            _alphaMemory = alphaMemory;
            _betaMemory = betaMemory;
            _workingMemory = workingMemory;
            _rules = rules;
            _WMClasses = WMClasses;
            _logger = logger;
            _calculators = calculators;
            _settings = settings;
        }

        private void PrintCommands()
        {
            Console.WriteLine("ALPHA\t\t\tLists Alpha Memory.");
            Console.WriteLine("ALPHA n\t\t\tExamines Alpha Node n.");
            Console.WriteLine("BETA\t\t\tLists Beta Memory.");
            Console.WriteLine("BETA n\t\t\tExamines Beta Node n.");
            Console.WriteLine("CALC c c c...\t\tPerforms an RPN calculation.");
            Console.WriteLine("CLEARERRORS\t\tClears any errors from the log.");
            Console.WriteLine("CLASSES\t\t\tLists the Classes.");
            Console.WriteLine("CS\t\t\tLists the current conflict set.");
            Console.WriteLine("EXIT\t\t\tExits OPS5.");
            Console.WriteLine("LAST\t\t\tDescribes the last rule fired.");
            Console.WriteLine("LOAD <filename>\t\tLoads the requested file.");
            Console.WriteLine("MAKE class (attribute1 val1, attribute2 val2, . . .)\n\t\t\tMakes an object.");
            Console.WriteLine("REMOVE n\t\tRemoves the object with ID n.");
            Console.WriteLine("RESET\t\t\tResets OPS5 and restarts.");
            Console.WriteLine("RETE\t\t\tDumps the contents of memory.");
            Console.WriteLine("RULES\t\t\tLists the rules in the conflict set.");
            Console.WriteLine("RULES FULL\t\tLists the rules and their current state.");
            Console.WriteLine("RULES ALL\t\tLists all rules.");
            Console.WriteLine("RUN\t\t\tRuns until all rules are satisfied.");
            Console.WriteLine("STEP n\t\t\tRuns until n rules have fired or all rules are satisfied.");
            Console.WriteLine("VERBOSITY n\t\tSets the level of verbosity (-1 to 2).");
            Console.WriteLine("WM\t\t\tLists the contents of working memory.");
            Console.WriteLine("WM <classname>\t\tLists the objects of the requested class.");
        }


        public async Task<ConsoleResult> RunConsole()
        {
            ConsoleResult result = ConsoleResult.OK;
            try
            {
                Console.Write(">");
                var cmdLine = await Task.Run(() => Console.ReadLine());
                if(cmdLine is string cmd)
                {
                    cmd += " ";
                    List<string> atoms = _parserUtils.ParseCommand(cmd);
                    if (atoms.Count() > 0)
                    {
                        int node;
                        switch (atoms[0].ToUpper())
                        {
                            case "EXIT":
                                result = ConsoleResult.Exit;
                                break;

                            case "RESET":
                                result = ConsoleResult.Reset;
                                break;

                            case "LOAD":
                                result = await Load(atoms);
                                break;

                            case "WM":
                                switch (atoms.Count())
                                {
                                    case 1:
                                        _workingMemory.ListWM();
                                        break;

                                    case 2:
                                        _workingMemory.ListWM(atoms[1]);
                                        break;

                                    default:
                                        _workingMemory.ListWM(atoms);
                                        break;
                                }
                                break;

                            case "RETE":
                                bool full = false;
                                bool all = false;
                                for (int x = 1; x < atoms.Count(); x++)
                                {
                                    if (atoms[x].ToUpper() == "FULL")
                                        full = true;
                                    if (atoms[x].ToUpper() == "ALL")
                                        all = true;
                                }
                                PrintRete(full, all);
                                break;

                            case "RULES":
                                bool full1 = false;
                                bool all1 = false;
                                for (int x = 1; x < atoms.Count(); x++)
                                {
                                    if (atoms[x].ToUpper() == "FULL")
                                        full1 = true;
                                    if (atoms[x].ToUpper() == "ALL")
                                        all1 = true;
                                }
                                _rules.PrintRules(full1, all1);
                                break;

                            case "CS":
                                _rules.PrintConflictSet();
                                break;

                            case "LAST":
                                Console.WriteLine(_lastRuleFired);
                                break;

                            case "CLASSES":
                                _WMClasses.PrintClasses();
                                break;

                            case "RUN":
                                if (_logger.ErrorCount == 0)
                                {
                                    result = ConsoleResult.Run;
                                }
                                else
                                {
                                    Console.WriteLine("\n\nPlease correct all errors before attempting to run\n\n");
                                }
                                break;

                            case "CALC":
                                List<string> calculation = new List<string>();
                                foreach (string obj in atoms)
                                    if(obj.ToUpper() != "CALC")
                                    calculation.Add(obj.ToString());

                                Console.WriteLine(_calculators.Default().Calc(calculation));
                                break;

                            case "ALPHA":
                                node = 0;
                                if (atoms.Count == 1)
                                {
                                    _alphaMemory.PrintAlphaMemory();
                                }
                                else if (atoms.Count == 2 && int.TryParse(atoms[1], out node))
                                {
                                    _alphaMemory.ExamineAlpha(node);
                                }
                                else
                                {
                                    Console.WriteLine("Usage: Alpha n   where n is number of node to examine");
                                }
                                break;

                            case "BETA":
                                node = 0;
                                if (atoms.Count == 1)
                                {
                                    _betaMemory.PrintBetaMemory();
                                }
                                else if (atoms.Count == 2 && int.TryParse(atoms[1], out node))
                                {
                                    _betaMemory.ExamineBeta(node);
                                }
                                else
                                {
                                    Console.WriteLine("Usage: Beta n   where n is number of node to examine");
                                }
                                break;

                            case "STEP":
                                int steps = 0;
                                int.TryParse(atoms[1], out steps);
                                if (_logger.Verbosity > 0)
                                {
                                    Console.WriteLine($"Stepping = {steps} steps");
                                }
                                _settings.Steps = steps;
                                result = ConsoleResult.RunSteps;
                                break;

                            case "VERBOSITY":
                                int v;
                                try
                                {
                                    if (!int.TryParse(atoms[1], out v))
                                        throw new Exception("Invalid verbosity setting, use Verbosity -1, 0, 1 or 2");
                                    if (v < -1 || v > 2)
                                        throw new Exception("Invalid verbosity setting, use Verbosity -1, 0, 1 or 2");
                                    _logger.Verbosity = v;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                                break;

                            case "MAKE":
                                _fileProcessing.Make(atoms);
                                break;

                            case "REMOVE":
                                int o;
                                try
                                {
                                    if (!int.TryParse(atoms[1], out o))
                                        throw new Exception("Invalid object ID, it should be an integer greater than zero");
                                    if (o < 0)
                                        throw new Exception("Invalid object ID, it should be an integer greater than zero");
                                    if (!_workingMemory.RemoveObject(o, true))
                                        _logger.WriteError($"Failed to remove Object {o}", "Console");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                                break;

                            case "HELP":
                                PrintCommands();
                                break;

                            case "CLEARERRORS":
                                _logger.ClearErrors();
                                break;

                            default:
                                if (atoms[0] != "")
                                    Console.WriteLine("Sorry, I have no idea what you mean");
                                break;
                        }
                    }
                    else if (cmd == "? ")
                    {
                        PrintCommands();
                    }
                }
                else
                    Console.Write(">");
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }

            return result;
        }

        private async Task<ConsoleResult> Load(List<string> atoms)
        {
            ConsoleResult result = ConsoleResult.OK;
            if (atoms.Count != 2)
            {
                _logger.WriteError($"Invalid Syntax", "Parser");
                result = ConsoleResult.Errors;
            }
            else
            {
                string fileName = atoms[1];
                if (!await _fileProcessing.ProcessFile(fileName))
                    result = ConsoleResult.Errors;
            }
            return result;
        }

        private void PrintRete(bool full, bool all)
        {
            _alphaMemory.PrintAlphaMemory();
            _betaMemory.PrintBetaMemory();
            _rules.PrintRules(full, all);
        }

        public void WriteDots(string ruleName)
        {
            if (ruleName == _lastRuleFired)
                _ruleSeq++;
            else
            {
                _ruleSeq = 1;
                _lastRuleFired = ruleName;
            }

            string bin = (_ruleSeq % 4) switch
            {
                0 => "|",
                1 => "/",
                2 => "-",
                3 => @"\",
                _ => ""
            };

            Console.Write($"{bin}\r");
        }
    }
}
