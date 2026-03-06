using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using AttributeLibrary;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OPS5.Engine
{
    internal class RHSActionExecutor : IRHSActionExecutor
    {
        private readonly IOPS5Logger _logger;
        private readonly IUtils _parserUtils;
        private readonly ICalculators _calculators;
        private readonly IWorkingMemory _workingMemory;
        private readonly IWMClasses _WMClasses;
        private readonly IConfig _config;
        private readonly IBetaMemory _betaMemory;
        private readonly IFileHandleManager _fileHandleManager;
        private readonly IExecuteBindingRegistry _executeBindingRegistry;

        private char[] _trimChars { get; } = new char[] { ' ', '\t', '\n' };
        private string _writeOut = "";

        public bool HaltRequested { get; private set; }
        public string HaltingRule { get; private set; } = string.Empty;

        public RHSActionExecutor(
            IOPS5Logger logger,
            IUtils parserUtils,
            ICalculators calculators,
            IWorkingMemory workingMemory,
            IWMClasses WMClasses,
            IConfig config,
            IBetaMemory betaMemory,
            IExecuteBindingRegistry executeBindingRegistry,
            IFileHandleManager fileHandleManager)
        {
            _logger = logger;
            _parserUtils = parserUtils;
            _calculators = calculators;
            _workingMemory = workingMemory;
            _WMClasses = WMClasses;
            _config = config;
            _betaMemory = betaMemory;
            _executeBindingRegistry = executeBindingRegistry;
            _fileHandleManager = fileHandleManager;
        }

        public void ResetHalt()
        {
            HaltRequested = false;
            HaltingRule = string.Empty;
        }

        public async Task ExecuteActions(IRule rule, IToken token)
        {
            try
            {
                foreach (IRHSAction action in rule.RHS)
                    await DoActions(action.Action, token, rule);
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Error {ex.Message} encountered whilst processing rule {rule.Name}", "Do Actions");
            }
        }

        private async Task DoActions(List<object> actions, IToken thisToken, IRule prod)
        {
            List<object> actionItems = new List<object>();
            for(int x = 0; x < actions.Count; x++)
            {
                if (actions[x] is string actionX)
                {
                    if (actionX.ToUpper() == "FORMATTED" && actions.Count > x && actions[x + 1] is string actionString)
                    {
                        actionItems.Add(FormatString(actionString, thisToken.Variables));
                        x++;
                    }
                    else
                        actionItems.Add(actionX);
                }
                else
                    actionItems.Add(actions[x]);
            }
            bool doing = true;
                foreach (object action in actionItems)
                {
                    if (action is string actionString && doing)
                    {
                        //action is a string, so therefore execute it
                        switch (actionString.ToUpper())
                        {
                            case "MODIFY":
                                DoModify(actionItems, thisToken, prod);
                                doing = false;
                                break;

                            case "REMOVE":
                                DoRemove(actionItems, thisToken, prod);
                                doing = false;
                                break;

                            case "MAKE":
                                DoMake(actionItems, thisToken, prod);
                                doing = false;
                                break;

                            case "WRITE":
                                DoWriteConsole(actionItems, thisToken, prod);
                                doing = false;
                                break;

                            case "EXECUTE":
                                DoExecute(actionItems, thisToken, prod);
                                doing = false;
                                break;

                            case "SET":
                                DoSet(actionItems, thisToken, prod);
                                doing = false;
                                break;

                            case "OPENFILE":
                                DoOpenFile(actionItems, thisToken, prod);
                                doing = false;
                                break;

                            case "CLOSEFILE":
                                DoCloseFile(actionItems, thisToken, prod);
                                doing = false;
                                break;

                            case "ACCEPT":
                                DoAccept(actionItems, thisToken, prod, singleToken: true);
                                doing = false;
                                break;

                            case "ACCEPTLINE":
                                DoAccept(actionItems, thisToken, prod, singleToken: false);
                                doing = false;
                                break;

                            case "HALT":
                                HaltRequested = true;
                                HaltingRule = prod.Name;
                                break;

                            default:
                                throw new Exception($"Unknown action {action}");
                        }
                    }
                    else if (action is List<object> && doing)
                    {
                        //action is a sub-action
                        await DoActions((List<object>)action, thisToken, prod);
                    }
                    else if (action is List<string> && doing)
                    {
                        //action is a sub-action
                        DoActionsString((List<string>)action, thisToken);
                    }
                    else
                    {
                        if (doing)
                        {
                            throw new Exception($"Unknown action {actions[0]}");
                        }
                    }
                    if (HaltRequested)
                        break;
                }
        }

        private void DoSet(List<object> actions, IToken thisToken, IRule prod)
        {
            string result = "NIL";
            int act = 1;
            if (actions[act++] is string var)
            {
                if (actions[act].ToString() == "=")
                    act++;

                if (actions[act++] is string action)
                {
                    switch (action.ToUpper())
                    {
                        case "CALC":
                        case "COMPUTE":
                            if (actions[act++] is string commands)
                                result = _calculators.Default().DoCalc(commands, thisToken);
                            else
                                _logger.WriteError("No actions found in set command", "Engine");
                            break;

                        case "SUBSTR":
                            if (actions[act++] is string substrCommands)
                            {
                                string[] sp2 = substrCommands.Split(" ");
                                if (sp2.Length >= 2)
                                {
                                    string text = thisToken.TryGetVariableValue(sp2[0]);
                                    string startStr = thisToken.TryGetVariableValue(sp2[1]);
                                    if (int.TryParse(startStr, out int start) && start >= 1)
                                    {
                                        int zeroStart = start - 1; // OPS5 is 1-based
                                        if (sp2.Length >= 3 && sp2[2].ToUpper() == "INF")
                                        {
                                            result = zeroStart < text.Length
                                                ? text.Substring(zeroStart)
                                                : "";
                                        }
                                        else if (sp2.Length >= 3 && int.TryParse(
                                            thisToken.TryGetVariableValue(sp2[2]), out int length))
                                        {
                                            int safeLen = Math.Min(length, text.Length - zeroStart);
                                            result = zeroStart < text.Length && safeLen > 0
                                                ? text.Substring(zeroStart, safeLen)
                                                : "";
                                        }
                                        else
                                            _logger.WriteError($"Invalid Substr length in {prod.Name}", "Engine");
                                    }
                                    else
                                        _logger.WriteError($"Invalid Substr start position in {prod.Name}", "Engine");
                                }
                                else
                                    _logger.WriteError($"Invalid Substr parameters {substrCommands}", "Engine");
                            }
                            else
                                _logger.WriteError("No actions found in set command", "Engine");
                            break;

                        default:
                            //Assume this is a variable assignment or concatenation
                            result = thisToken.TryGetVariableValue(action);
                            for (int x = act; x < actions.Count; x++)
                            {
                                if (actions[x] is string plus)
                                {
                                    if (plus.Trim(_trimChars) == "+")
                                    {
                                        x++;
                                        if (actions.Count >= x)
                                        {
                                            if (actions[x] is string nextString)
                                            {
                                                result += thisToken.TryGetVariableValue(nextString);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.WriteError($"Unknown action in SET statement. Did you mean {result} + {plus} ?", "Engine");
                                    }
                                }
                            }
                            break;
                    }
                    thisToken.UpdateVariable(var, result);
                }
                else
                    _logger.WriteError("Syntax error in Set statement in rule", "DoSet");
            }
            else
                _logger.WriteError("Syntax error in Set statement in rule", "DoSet");
        }

        private void DoAccept(List<object> actions, IToken thisToken, IRule prod, bool singleToken)
        {
            if (actions.Count < 2 || actions[1] is not string varName)
            {
                _logger.WriteError(
                    $"Syntax error in {(singleToken ? "Accept" : "AcceptLine")} action in rule {prod.Name}: variable required",
                    "RHSActionExecutor");
                return;
            }

            int fromIndex = FindKeywordIndex(actions, "FROM");
            if (fromIndex > 0 && fromIndex + 1 < actions.Count)
            {
                // File-sourced read: Accept <var> From <logicalName>;
                string logicalName = actions[fromIndex + 1] is string ln
                    ? thisToken.TryGetVariableValue(ln)
                    : actions[fromIndex + 1].ToString()!;

                var reader = _fileHandleManager.GetReader(logicalName);
                if (reader == null)
                {
                    thisToken.UpdateVariable(varName, "NIL");
                    return;
                }

                string? input;
                if (singleToken)
                {
                    input = ReadTokenFromStream(reader);
                }
                else
                {
                    input = reader.ReadLine();
                }

                thisToken.UpdateVariable(varName, input ?? "NIL");
            }
            else
            {
                // Console read (existing behaviour)
                string? prompt = null;
                if (actions.Count >= 3 && actions[2] is string promptStr)
                    prompt = thisToken.TryGetVariableValue(promptStr);

                string? input = singleToken
                    ? _logger.ReadInput(prompt)
                    : _logger.ReadInputLine(prompt);

                thisToken.UpdateVariable(varName, input ?? "NIL");
            }
        }

        /// <summary>
        /// Reads a single whitespace-delimited token from a StreamReader.
        /// Skips leading whitespace and reads until the next whitespace or EOF.
        /// </summary>
        private static string? ReadTokenFromStream(StreamReader reader)
        {
            // Skip leading whitespace
            int ch;
            while ((ch = reader.Peek()) != -1 && char.IsWhiteSpace((char)ch))
                reader.Read();

            if (reader.Peek() == -1)
                return null;

            var token = new System.Text.StringBuilder();
            while ((ch = reader.Peek()) != -1 && !char.IsWhiteSpace((char)ch))
            {
                token.Append((char)reader.Read());
            }

            return token.Length > 0 ? token.ToString() : null;
        }

        private void DoOpenFile(List<object> actions, IToken thisToken, IRule prod)
        {
            // OpenFile <logicalName> <filePath> <mode>
            // actions[0] = "OPENFILE", [1] = logical name, [2] = file path, [3] = mode (IN/OUT/APPEND)
            if (actions.Count < 4)
            {
                _logger.WriteError(
                    $"Syntax error in OpenFile action in rule {prod.Name}: expected OpenFile <name> <path> <mode>",
                    "RHSActionExecutor");
                return;
            }

            string logicalName = actions[1] is string ln ? thisToken.TryGetVariableValue(ln) : actions[1].ToString()!;
            string filePath = actions[2] is string fp ? thisToken.TryGetVariableValue(fp) : actions[2].ToString()!;
            string mode = actions[3] is string m ? m.ToUpperInvariant() : actions[3].ToString()!.ToUpperInvariant();

            _fileHandleManager.OpenFile(logicalName, filePath, mode);
        }

        private void DoCloseFile(List<object> actions, IToken thisToken, IRule prod)
        {
            // CloseFile <logicalName>
            // actions[0] = "CLOSEFILE", [1] = logical name
            if (actions.Count < 2)
            {
                _logger.WriteError(
                    $"Syntax error in CloseFile action in rule {prod.Name}: expected CloseFile <name>",
                    "RHSActionExecutor");
                return;
            }

            string logicalName = actions[1] is string ln ? thisToken.TryGetVariableValue(ln) : actions[1].ToString()!;
            _fileHandleManager.CloseFile(logicalName);
        }

        /// <summary>
        /// Scans action atoms for a "TO" keyword and returns its index, or -1 if not found.
        /// Used to detect file-targeted Write actions: Write (...) To output;
        /// </summary>
        private static int FindKeywordIndex(List<object> actions, string keyword)
        {
            for (int i = 1; i < actions.Count; i++)
            {
                if (actions[i] is string s && s.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void DoModify(List<object> actions, IToken thisToken, IRule prod)
        {
            // First parameter of modify is the index of the Object in the token
            // SUbsequent pairs are attributes and values
            //until next non-string action
            if (actions[1] is string id)
            {
                if (int.TryParse(id, out int tokenIndex))
                {
                    tokenIndex--;
                    int count = 0;
                    for (int c = 0; c < tokenIndex; c++)
                    {
                        if (prod.Conditions[c].Negative)
                            count++;
                    }
                    tokenIndex -= count;

                    //Find the object to modify
                    //Remove the object from WM, but first make a new one
                    int objectID = thisToken.ObjectIDs[tokenIndex];
                    _logger.WriteInfo($"Modifying Object #{objectID}", 1);
                    IWMElement iObject = _workingMemory.GetWME(objectID);
                    string className = iObject.ClassName;

                    AttributesCollection attributes = iObject.GetUserAttributes(); //returns a copy

                    List<object> makeActions = new List<object>();
                    List<string> modifiedAttributes = new List<string>();
                    makeActions.Add("MAKE");
                    makeActions.Add(className);
                    string lastAttr = "";
                    List<object> localActions = new List<object>(actions);
                    for (int x = 2; x < actions.Count; x++)
                    {
                        if (localActions[x] is string action)
                        {
                            if( attributes.ContainsKey(action.ToUpper()))
                            {
                                modifiedAttributes.Add(action.ToUpper());
                                lastAttr = action;
                            }
                        }

                        makeActions.Add(localActions[x]);
                    }

                    foreach (KeyValuePair<string, string?> attr in attributes)
                    {
                        if (!modifiedAttributes.Contains(attr.Key) && attr.Value is string)
                        {
                            makeActions.Add(attr.Key);
                            makeActions.Add(attr.Value);
                        }
                    }

                    DoMake(makeActions, thisToken, prod);

                    _workingMemory.RemoveObject(objectID, false, true); //Moved to after make so that vectors and matrices aren't destroyed
                                                                        //Also flagging this is a modify to make absolutely sure - needs investigating
                }
            }
        }

        /// <summary>
        /// action[1] of remove is the index of the Object in the token
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="thisToken"></param>
        /// <param name="prod"></param>
        private void DoRemove(List<object> actions, IToken thisToken, IRule prod)
        {
            if (actions[1] is string id)
            {
                if (int.TryParse(id, out int tokenIndex))
                {
                    tokenIndex--;
                    int count = 0;
                    for (int c = 0; c < tokenIndex; c++)
                    {
                        if (prod.Conditions[c].Negative)
                            count++;
                    }
                    tokenIndex -= count;
                    if (tokenIndex > -1 && tokenIndex < thisToken.ObjectIDs.Count)
                    {
                        int objectID = thisToken.ObjectIDs[tokenIndex];
                        if (!_workingMemory.RemoveObject(objectID, true))
                            _logger.WriteError("Failed to remove Object", "DoActions");
                    }
                    else
                        _logger.WriteError($"Attempted to remove object at index {tokenIndex} of token {thisToken.ID} which only contains {thisToken.ObjectIDs.Count} Objects", "DoActions");
                }
            }
        }

        private void DoMake(List<object> actions, IToken thisToken, IRule prod)
        {
            if (_betaMemory.GetBetaNode(thisToken.Owner).IsFindPath)
            {
                // Create a new object for each step in the path
                IFindPathInfo fpi = _betaMemory.GetBetaNode(thisToken.Owner).FindPath;
                int index = 1;
                for (int x = fpi.FirstObject; x < thisToken.ObjectCount(); x++)
                {
                    IWMElement iObject = _workingMemory.GetWME(thisToken.ObjectIDs[x]);
                    var fromVal = iObject.AttributeValue(fpi.FromAttr);
                    var toVal = iObject.AttributeValue(fpi.ToAttr);
                    string distVal = "";
                    if (fpi.DistAttr != "")
                    {
                        var v = iObject.AttributeValue(fpi.DistAttr);
                        if (v is string)
                            distVal = v;
                    }

                    List<object> acts = new List<object>();
                    for (int y = 0; y < actions.Count; y++)
                    {
                        if (actions[y] == null)
                            _logger.WriteError("Error executing Make command for FindPath", "Engine");
                        else
                        {
                            if (actions[y] is string act && fromVal is string && toVal is string)
                            {
                                if (act.ToUpper() == fpi.FromVar)
                                    acts.Add(fromVal);
                                else if (act.ToUpper() == fpi.ToVar)
                                    acts.Add(toVal);
                                else if (act.ToUpper() == fpi.DistVar)
                                    acts.Add(distVal);
                                else
                                    acts.Add(act);
                            }
                        }
                    }
                    Make(acts, thisToken, prod);
                    index++;
                }
            }
            else
                Make(actions, thisToken, prod); //NOT FindPath

        }

        private void Make(List<object> actions, IToken thisToken, IRule prod)
        {
            char[] trimChars = new char[] { ' ', '\t', '\n' };
            List<string> elements = new List<string>();
            if(actions.Count > 0 && actions[1] is string className)
            {
                for (int y = 2; y < actions.Count; y++)
                {
                    if (actions[y] is string action)
                    {
                        string element;
                        if (actions.Count > y + 2 && actions[y + 1] is string acty1 && acty1.Trim(trimChars).ToUpper() == "CONCAT")
                        {
                            //this is a concatenation, so find out how far it goes
                            List<string> toConcat = new List<string>
                        {
                            action
                        };
                            for (int z = y + 1; z < actions.Count; z += 2)
                            {
                                if (actions.Count > z + 1 && actions[z] is string && actions[z + 1] is string nextAction)
                                {
                                    toConcat.Add(nextAction);
                                    y += 2;
                                }
                                else
                                    break;
                            }
                            element = Concatenate(toConcat, thisToken);
                        }
                        else
                            element = action;

                        if (element.StartsWith("<"))
                        {
                            string val = thisToken.TryGetVariableValue(element);
                            elements.Add(val);
                        }
                        else
                        {
                            switch (element.ToUpper())
                            {
                                case "CALC":
                                    List<string> calculation = new List<string>((List<string>)actions[y + 1]);          //Need to do this to copy values, not make reference to actions[y + 1]
                                    for (int z = 0; z < calculation.Count; z++)
                                    {
                                        if (calculation[z].StartsWith("<"))
                                            calculation[z] = thisToken.TryGetVariableValue(calculation[z]);
                                    }

                                    elements.Add(_calculators.Default().Calc(calculation));
                                    y++;
                                    break;

                                case "SUBSTR":
                                    if (y + 1 < actions.Count && actions[y + 1] is List<string> substrArgs)
                                    {
                                        List<string> sArgs = new List<string>(substrArgs);
                                        string subText = sArgs[0].StartsWith("<")
                                            ? thisToken.TryGetVariableValue(sArgs[0]) : CleanLiteral(sArgs[0]);
                                        string subStartStr = sArgs[1].StartsWith("<")
                                            ? thisToken.TryGetVariableValue(sArgs[1]) : sArgs[1];
                                        if (int.TryParse(subStartStr, out int subStart) && subStart >= 1)
                                        {
                                            int zeroStart = subStart - 1;
                                            if (sArgs.Count >= 3 && sArgs[2].ToUpper() == "INF")
                                            {
                                                elements.Add(zeroStart < subText.Length ? subText.Substring(zeroStart) : "");
                                            }
                                            else if (sArgs.Count >= 3)
                                            {
                                                string lenStr = sArgs[2].StartsWith("<")
                                                    ? thisToken.TryGetVariableValue(sArgs[2]) : sArgs[2];
                                                if (int.TryParse(lenStr, out int len))
                                                {
                                                    int safeLen = Math.Min(len, subText.Length - zeroStart);
                                                    elements.Add(zeroStart < subText.Length && safeLen > 0
                                                        ? subText.Substring(zeroStart, safeLen) : "");
                                                }
                                            }
                                        }
                                        y++;
                                    }
                                    break;

                                default:
                                    //Value is a literal
                                    element = CleanLiteral(element);
                                    if (element.StartsWith("{}"))
                                        element = FormatString(element, thisToken.Variables);

                                    elements.Add(element);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        //Checking for a compute statement
                        List<object> sub = (List<object>)actions[y];
                        if (sub[0] is string subVal)
                        {
                            if (subVal.ToUpper() == "COMPUTE")
                            {
                                List<string> calculation = new List<string>();
                                for (int i = 1; i < sub.Count; i++)
                                {
                                    if (sub[i] is string calcVal)
                                    {
                                        if (calcVal.StartsWith("<") && calcVal.EndsWith(">"))
                                            calcVal = thisToken.TryGetVariableValue(calcVal);
                                        calculation.Add(calcVal);
                                    }
                                }

                                elements.Add(_calculators.Default().Calc(calculation));
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                string[] attributes = elements.ToArray();
                _workingMemory.AddObject(className, attributes);
            }
        }

        private string CleanLiteral(string element)
        {
            if (_config.Ops5 && element.StartsWith("|") && element.EndsWith("|"))
                element = element.Replace("|", "");
            if (!_config.Ops5 && element.StartsWith("\"") && element.EndsWith("\""))
                element = element.Replace("\"", "");
            return element;
        }

        private string Concatenate(List<string> atoms, IToken thisToken)
        {
            string result = "";

            foreach (string atom in atoms)
            {
                string thisAtom = atom;
                if (thisAtom.StartsWith("<"))
                    thisAtom = thisToken.TryGetVariableValue(thisAtom);
                thisAtom = CleanLiteral(thisAtom);
                if (thisAtom.StartsWith("{}"))
                    thisAtom = FormatString(thisAtom, thisToken.Variables);
                result += thisAtom;
            }
            return result;
        }

        /// <summary>
        /// Writes output to console
        /// All actions starting from [1] treated as output
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="thisToken"></param>
        /// <param name="prod"></param>
        private void DoWriteConsole(List<object> actions, IToken thisToken, IRule prod)
        {
            int toIndex = FindKeywordIndex(actions, "TO");
            if (toIndex > 0 && toIndex + 1 < actions.Count)
            {
                // File-targeted write: Write (...) To <logicalName>;
                // Build output from atoms before "TO"
                var writeActions = actions.Take(toIndex).ToList();
                DoWrite(writeActions, thisToken, prod);

                string logicalName = actions[toIndex + 1] is string ln
                    ? thisToken.TryGetVariableValue(ln)
                    : actions[toIndex + 1].ToString()!;

                var writer = _fileHandleManager.GetWriter(logicalName);
                if (writer != null)
                {
                    writer.Write(_writeOut);
                    writer.Flush();
                }
                _writeOut = "";
            }
            else
            {
                // Console write (existing behaviour)
                DoWrite(actions, thisToken, prod);
                _logger.WriteOutput(_writeOut);
                _writeOut = "";
            }
        }


        /// <summary>
        /// Constructs output for writing to static variable
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="thisToken"></param>
        /// <param name="prod"></param>
        private void DoWrite(List<object> actions, IToken thisToken, IRule prod)
        {
            int charCount = 0;
            for (int y = 1; y < actions.Count; y++)
            {
                if (actions[y] is string str)
                {
                    if (str == "FORMATTED")
                    {
                        y++;
                        if (actions[y] is string strY)
                        charCount += WriteFormattedAtom(strY, thisToken);
                    }
                    else if (str == "TABTO")
                    {
                        y++;
                        if (y < actions.Count && actions[y] is string colStr)
                        {
                            string resolved = thisToken.TryGetVariableValue(colStr);
                            if (int.TryParse(resolved, out int targetCol))
                            {
                                int currentCol = _writeOut.Length;
                                if (targetCol > currentCol)
                                {
                                    _writeOut += new string(' ', targetCol - currentCol);
                                    charCount += targetCol - currentCol;
                                }
                            }
                        }
                    }
                    else
                        charCount += WriteAtom(str, thisToken);
                }
                else
                {
                    charCount += WriteAction((List<object>)actions[y], thisToken, charCount);
                }
            }
        }

        private void DoExecute(List<object> actions, IToken thisToken, IRule prod)
        {
            string bindingName = "";
            if (actions.Count < 2)
            {
                _logger.WriteError("Invalid EXECUTE statement, incorrect number of arguments", "DoActions");
            }
            else
            {
                if (actions[1] is string act1)
                {
                    bindingName = act1.ToUpper();
                    var binding = _executeBindingRegistry.Get(bindingName);
                    if (binding != null)
                    {
                        if (actions.Count == 2)
                            binding.Execute("");
                        else
                        {
                            string arguments = "";
                            for (int i = 2; i < actions.Count; i++)
                            {
                                if (actions[i] is string arg)
                                {
                                    if (arg.StartsWith("<"))
                                        arg = thisToken.TryGetVariableValue(arg);
                                    arguments += arg + " ";
                                }
                            }
                            binding.Execute(arguments);
                        }
                    }
                    else
                        _logger.WriteError($"ERROR - Execute Binding {bindingName} not found", "DoActions");
                }
                else
                    _logger.WriteError($"ERROR - Execute Binding {bindingName} not found", "DoActions");
            }
        }

        private string FormatString(string inputToken, ConcurrentDictionary<string, string> variables)
        {
            string outputToken = inputToken;
            Match match = Regex.Match(outputToken, @"\{\<[A-Z0-9.]*\>\}");
            while (match.Success)
            {
                string part = match.Value;
                Match varMatch = Regex.Match(part, @"\<.+\>");
                if (varMatch.Success)
                {
                    string varX = varMatch.Value.ToUpper();
                    if (variables.ContainsKey(varX))
                    {
                        varX = variables[varX];

                        Regex regx = new Regex(@"\{[^\}]+\}");
                        Match fmtMatch = Regex.Match(part, @"\:.+\}");
                        if (fmtMatch.Success)
                        {
                            string fmt = fmtMatch.Value;
                            fmt = fmt.Substring(1, fmt.Length - 2);
                            DateTime dt = DateTime.Now;

                            if (double.TryParse(varX, out double tmp))
                            {
                                outputToken = regx.Replace(outputToken, tmp.ToString(fmt), 1);
                            }
                            else if (DateTime.TryParse(varX, out dt))
                            {
                                outputToken = regx.Replace(outputToken, dt.ToString(fmt), 1);
                            }
                        }
                        else
                        {
                            outputToken = regx.Replace(outputToken, varX, 1);
                        }
                    }
                    else
                        _logger.WriteError($"Variable {varX} not found in token", "DoActions");
                }
                else
                    _logger.WriteError($"Could not find variable to be replaced in text {outputToken}", "DoActions");
                match = match.NextMatch();
            }
            return outputToken;
        }

        private void DoActionsString(List<string> actions, IToken thisToken)
        {
            switch (actions[0].ToUpper())
            {
                default:
                    _logger.WriteError($"Unknown action {actions[0]} in Rule", "DoActionsString");
                    break;
            }
        }

        private int WriteAtom(string atom, IToken thisToken)
        {
            int charCount = 0;
            string var = "";
            if (atom.StartsWith("<"))
                var = thisToken.TryGetVariableValue(atom);
            else
                var = atom;

            if (_config.Ops5)
                var = var.Replace("|", "");

            if (var.StartsWith("{}"))
                var = FormatString(var, thisToken.Variables);

            _writeOut += var;
            charCount += var.Length;
            return charCount;
        }

        private int WriteFormattedAtom(string atom, IToken thisToken)
        {
            string line = FormatString(atom, thisToken.Variables);
            _writeOut += line;
            return line.Length; ;
        }


        private int WriteAction(List<object> actions, IToken thisToken, int preCount)
        {
            int charCount = 0;
            for (int x = 0; x < actions.Count; x++)
            {
                if (actions[x] is string act)
                {
                    switch (act.ToUpper())
                    {
                        case "TABTO":
                            if (x + 1 < actions.Count && actions[x + 1] is string tabColStr)
                            {
                                if (int.TryParse(tabColStr, out int targetCol))
                                {
                                    int currentCol = preCount + charCount;
                                    if (targetCol > currentCol)
                                    {
                                        _writeOut += new string(' ', targetCol - currentCol);
                                        charCount += targetCol - currentCol;
                                    }
                                }
                            }
                            x++;
                            break;
                        case "CRLF":
                            _logger.WriteOutput(_writeOut);
                            _writeOut = "";
                            break;
                        default:
                            charCount += WriteAtom(act, thisToken);
                            break;
                    }
                }
                else
                {
                    charCount += WriteAction((List<object>)actions[x], thisToken, preCount + charCount);
                }
            }
            return charCount;
        }
    }
}
