using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Models;
using OPS5.Engine.Parsers.Tokenizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OPS5.Engine.Parsers.OPS5
{
    /// <summary>
    /// Native OPS5 parser that reads OPS5 S-expression syntax directly
    /// and produces the model objects consumed by the engine.
    /// </summary>
    internal class OPS5Parser : IOPS5Parser
    {
        private readonly IOPS5Logger _logger;

        public OPS5Parser(IOPS5Logger logger)
        {
            _logger = logger;
        }

        public OPS5ParseResult Parse(string ops5Text, string fileName)
        {
            var result = new OPS5ParseResult();

            var lexer = new OPS5Lexer(ops5Text, fileName);
            var tokens = lexer.Tokenize();
            var stream = new TokenStream(tokens, fileName);

            foreach (string diag in lexer.Diagnostics)
                _logger.WriteError(diag, fileName);

            while (!stream.IsAtEnd)
            {
                try
                {
                    if (stream.Check(TokenType.LeftParen))
                    {
                        stream.Advance(); // consume (

                        if (stream.Check(TokenType.Identifier))
                        {
                            string keyword = stream.Current.Value.ToLower();
                            switch (keyword)
                            {
                                case "literalize":
                                    stream.Advance();
                                    ParseLiteralize(stream, result, fileName);
                                    break;

                                case "make":
                                    stream.Advance();
                                    ParseTopLevelMake(stream, result, fileName);
                                    break;

                                case "p":
                                    stream.Advance();
                                    ParseProduction(stream, result, fileName);
                                    break;

                                case "default":
                                    stream.Advance();
                                    ParseDefault(stream, result, fileName);
                                    break;

                                case "vector-attribute":
                                    stream.Advance();
                                    ParseVectorAttribute(stream, result, fileName);
                                    break;

                                default:
                                    _logger.WriteError($"Unknown top-level form '({keyword}' at line {stream.Current.Line} in {fileName}", fileName);
                                    SkipToMatchingParen(stream);
                                    break;
                            }
                        }
                        else
                        {
                            _logger.WriteError($"Expected identifier after '(' at line {stream.Current.Line} in {fileName}", fileName);
                            SkipToMatchingParen(stream);
                        }
                    }
                    else
                    {
                        stream.Advance();
                    }
                }
                catch (ParseException ex)
                {
                    _logger.WriteError(ex.Message, fileName);
                    SkipToMatchingParen(stream);
                }
                catch (Exception ex)
                {
                    _logger.WriteError(ex.Message, fileName);
                    SkipToMatchingParen(stream);
                }
            }

            return result;
        }

        // ==================== Literalize ====================

        private void ParseLiteralize(TokenStream stream, OPS5ParseResult result, string fileName)
        {
            // (literalize class-name attr1 attr2 ...)
            // Already consumed: ( literalize

            if (!stream.Check(TokenType.Identifier))
            {
                _logger.WriteError($"Expected class name after 'literalize' at line {stream.Current.Line}", fileName);
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            var attributes = new List<string>();
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                attributes.Add(stream.Current.Value);
                stream.Advance();
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            var classModel = new ClassModel("")
            {
                ClassName = className,
                Atoms = attributes
            };

            try
            {
                classModel.ValidateAtoms();
                result.Classes.Classes.Add(classModel);
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Invalid literalize in {fileName}: {ex.Message}", fileName);
            }
        }

        // ==================== Default ====================

        private void ParseDefault(TokenStream stream, OPS5ParseResult result, string fileName)
        {
            // (default class-name ^attr1 val1 ^attr2 val2 ...)
            // Already consumed: ( default

            if (!stream.Check(TokenType.Identifier))
            {
                _logger.WriteError($"Expected class name after 'default' at line {stream.Current.Line}", fileName);
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.Caret))
                {
                    stream.Advance(); // skip ^
                    if (stream.Check(TokenType.Identifier))
                    {
                        string attr = stream.Current.Value;
                        stream.Advance();
                        string val = ConsumeAtomValueRaw(stream);
                        defaults[attr] = val;
                    }
                }
                else
                {
                    stream.Advance();
                }
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            result.Defaults.Add(new DefaultModel { ClassName = className, Defaults = defaults });
        }

        // ==================== Vector-Attribute ====================

        private void ParseVectorAttribute(TokenStream stream, OPS5ParseResult result, string fileName)
        {
            // (vector-attribute class-name attr1 attr2 ...)
            // Already consumed: ( vector-attribute

            if (!stream.Check(TokenType.Identifier))
            {
                _logger.WriteError($"Expected class name after 'vector-attribute' at line {stream.Current.Line}", fileName);
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            var attributes = new List<string>();
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                attributes.Add(stream.Current.Value);
                stream.Advance();
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            result.VectorAttributes.Add(new VectorAttributeModel { ClassName = className, Attributes = attributes });
        }

        // ==================== Top-Level Make ====================

        private void ParseTopLevelMake(TokenStream stream, OPS5ParseResult result, string fileName)
        {
            // (make class-name ^attr1 val1 ^attr2 val2 ...)
            // Already consumed: ( make

            if (!stream.Check(TokenType.Identifier))
            {
                _logger.WriteError($"Expected class name after 'make' at line {stream.Current.Line}", fileName);
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            var atoms = new List<string> { "MAKE", className };
            CollectMakeAttrValuePairs(stream, atoms);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            var action = new DataActionModel("MAKE", fileName) { Atoms = atoms };
            result.Data.Actions.Add(action);
        }

        // ==================== Production ====================

        private void ParseProduction(TokenStream stream, OPS5ParseResult result, string fileName)
        {
            // (p rule-name LHS --> RHS)
            // Already consumed: ( p

            if (!stream.Check(TokenType.Identifier))
            {
                _logger.WriteError($"Expected rule name after 'p' at line {stream.Current.Line}", fileName);
                SkipToMatchingParen(stream);
                return;
            }

            string ruleName = stream.Current.Value;
            stream.Advance();

            var ruleModel = new RuleModel
            {
                RuleName = ruleName,
                FileName = fileName
            };

            _logger.WriteInfo($"Parsing Rule {ruleName}", 1);

            // Parse LHS conditions until -->
            int order = 1;
            var classNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            while (!stream.Check(TokenType.Arrow) && !stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                bool negative = false;
                if (stream.Check(TokenType.Minus))
                {
                    negative = true;
                    stream.Advance();
                }

                if (stream.Check(TokenType.LeftParen))
                {
                    stream.Advance(); // consume (
                    ParseCondition(stream, ruleModel, ref order, negative, classNameCounts);
                }
                else
                {
                    stream.Advance();
                }
            }

            // Consume -->
            if (stream.Check(TokenType.Arrow))
            {
                stream.Advance();
                _logger.WriteInfo("Completed LHS", 2);
            }

            // Parse RHS actions until )
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.LeftParen))
                {
                    stream.Advance(); // consume (
                    ParseRHSAction(stream, ruleModel);
                }
                else
                {
                    stream.Advance();
                }
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            result.Rules.Rules.Add(ruleModel);
        }

        // ==================== LHS Condition ====================

        private void ParseCondition(TokenStream stream, RuleModel ruleModel, ref int order,
                                     bool negative, Dictionary<string, int> classNameCounts)
        {
            // Already consumed: ( [and minus if negative]
            // Format: class-name ^attr1 val1 ^attr2 <var> ...

            if (!stream.Check(TokenType.Identifier))
            {
                _logger.WriteError($"Expected class name in condition at line {stream.Current.Line}", "Parser");
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            // Track class name occurrences for alias generation
            if (!classNameCounts.ContainsKey(className))
                classNameCounts[className] = 0;
            classNameCounts[className]++;
            int occurrence = classNameCounts[className];

            var conditionModel = new ConditionModel(order++, className, negative, "");

            // Generate alias for 2nd+ occurrence of a positive condition
            if (!negative && occurrence > 1)
            {
                string alias = $"{className}_{occurrence}";
                conditionModel.Alias = alias;
                if (!ruleModel.ConditionAliases.ContainsKey(alias))
                    ruleModel.ConditionAliases[alias] = conditionModel.Order;
            }

            // CLASS = className test (always first)
            conditionModel.Tests.Add(new ConditionTest("CLASS", "=", conditionModel.ClassName));

            // Parse ^attr value pairs
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.Caret))
                {
                    stream.Advance(); // skip ^

                    if (!stream.Check(TokenType.Identifier))
                    {
                        stream.Advance();
                        continue;
                    }

                    string attr = stream.Current.Value.ToUpper();
                    stream.Advance();

                    // Check for conjunction: { ... }
                    if (stream.Check(TokenType.LeftBrace))
                    {
                        stream.Advance(); // skip {
                        ParseOPS5Conjunction(stream, conditionModel, attr, ruleModel.RuleName);
                    }
                    // Check for disjunction: << ... >>
                    else if (stream.Check(TokenType.DoubleLeftAngle))
                    {
                        stream.Advance(); // skip <<
                        ParseOPS5Disjunction(stream, conditionModel, attr);
                    }
                    // Check for predicate operator
                    else if (IsPredicateOperator(stream.Current.Type))
                    {
                        string op = GetOperatorString(stream.Current.Type);
                        stream.Advance();
                        string val = ConsumeAtomValue(stream, ruleModel.RuleName);
                        conditionModel.Tests.Add(new ConditionTest(attr, op, val));
                    }
                    // Simple value or variable binding
                    else
                    {
                        string val = ConsumeAtomValue(stream, ruleModel.RuleName);
                        if (val.StartsWith("<") && val.EndsWith(">"))
                        {
                            // Variable binding — use = operator (engine handles binding vs test)
                            conditionModel.Tests.Add(new ConditionTest(attr, "=", val));
                        }
                        else
                        {
                            conditionModel.Tests.Add(new ConditionTest(attr, "=", val));
                        }
                    }
                }
                else
                {
                    stream.Advance();
                }
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            ruleModel.Conditions.Add(conditionModel);
        }

        private void ParseOPS5Conjunction(TokenStream stream, ConditionModel condition,
                                           string attr, string ruleName)
        {
            // { predicate1 predicate2 ... }
            while (!stream.Check(TokenType.RightBrace) && !stream.IsAtEnd)
            {
                if (IsPredicateOperator(stream.Current.Type))
                {
                    string op = GetOperatorString(stream.Current.Type);
                    stream.Advance();
                    string val = ConsumeAtomValue(stream, ruleName);
                    condition.Tests.Add(new ConditionTest(attr, op, val));
                }
                else
                {
                    string val = ConsumeAtomValue(stream, ruleName);
                    condition.Tests.Add(new ConditionTest(attr, "=", val));
                }
            }

            if (stream.Check(TokenType.RightBrace))
                stream.Advance();
        }

        private void ParseOPS5Disjunction(TokenStream stream, ConditionModel condition, string attr)
        {
            // << val1 val2 val3 >>
            var values = new List<string>();
            while (!stream.Check(TokenType.DoubleRightAngle) && !stream.IsAtEnd)
            {
                values.Add(ConsumeAtomValueRaw(stream));
            }

            if (stream.Check(TokenType.DoubleRightAngle))
                stream.Advance();

            condition.Tests.Add(new ConditionTest(attr, "=",
                "<<" + string.Join(" ", values) + ">>"));
        }

        // ==================== RHS Actions ====================

        private void ParseRHSAction(TokenStream stream, RuleModel ruleModel)
        {
            // Already consumed: (
            if (!stream.Check(TokenType.Identifier))
            {
                SkipToMatchingParen(stream);
                return;
            }

            string action = stream.Current.Value.ToLower();
            stream.Advance();

            switch (action)
            {
                case "make":
                    ParseRHSMake(stream, ruleModel);
                    break;

                case "modify":
                    ParseRHSModify(stream, ruleModel);
                    break;

                case "remove":
                    ParseRHSRemove(stream, ruleModel);
                    break;

                case "write":
                    ParseRHSWrite(stream, ruleModel);
                    break;

                case "halt":
                    ParseRHSHalt(stream, ruleModel);
                    break;

                case "compute":
                    ParseRHSCompute(stream, ruleModel);
                    break;

                case "bind":
                    ParseRHSBind(stream, ruleModel);
                    break;

                case "call":
                    ParseRHSCall(stream, ruleModel);
                    break;

                case "crlf":
                    // Standalone (crlf) — skip
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance();
                    break;

                case "accept":
                case "acceptline":
                    _logger.WriteError(
                        $"OPS5 ({action}) without (bind) at line {stream.Current.Line} — use (bind <var> ({action})) instead",
                        ruleModel.FileName);
                    SkipToMatchingParen(stream);
                    break;

                case "openfile":
                    ParseRHSOpenFile(stream, ruleModel);
                    break;

                case "closefile":
                    ParseRHSCloseFile(stream, ruleModel);
                    break;

                case "cbind":
                    ParseRHSCBind(stream, ruleModel);
                    break;

                default:
                    _logger.WriteError($"Unknown RHS action '{action}' at line {stream.Current.Line}", ruleModel.FileName);
                    SkipToMatchingParen(stream);
                    break;
            }
        }

        private void ParseRHSMake(TokenStream stream, RuleModel ruleModel)
        {
            // (make class ^attr val ...)
            if (!stream.Check(TokenType.Identifier))
            {
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            var atoms = new List<object> { "MAKE", className };
            CollectMakeAttrValuePairsAsObjects(stream, atoms, ruleModel.RuleName);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            var actionModel = new ActionModel("MAKE", "")
            {
                ClassName = className.ToUpper(),
                Actions = atoms
            };
            ruleModel.Actions.Add(actionModel);
        }

        private void ParseRHSModify(TokenStream stream, RuleModel ruleModel)
        {
            // (modify N ^attr val ...)
            string condRef = ConsumeAtomValueRaw(stream);

            int condNum = ResolveConditionRef(condRef, ruleModel);

            var atoms = new List<object> { "MODIFY", condNum.ToString() };
            CollectMakeAttrValuePairsAsObjects(stream, atoms, ruleModel.RuleName);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            if (condNum > 0 && condNum <= ruleModel.Conditions.Count)
            {
                var actionModel = new ActionModel("MODIFY", "")
                {
                    ClassName = ruleModel.Conditions[condNum - 1].ClassName,
                    Actions = atoms
                };
                ruleModel.Actions.Add(actionModel);
            }
            else if (condNum > ruleModel.Conditions.Count)
                _logger.WriteError($"Condition reference '{condRef}' ({condNum}) out of range in Modify action of Rule {ruleModel.RuleName} (only {ruleModel.Conditions.Count} conditions defined)", "Rule Parser");
            else
                _logger.WriteError($"Unknown condition reference '{condRef}' in Modify action of Rule {ruleModel.RuleName}", "Rule Parser");
        }

        private void ParseRHSRemove(TokenStream stream, RuleModel ruleModel)
        {
            // (remove N)
            string condRef = ConsumeAtomValueRaw(stream);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            int condNum = ResolveConditionRef(condRef, ruleModel);

            if (condNum > 0 && condNum <= ruleModel.Conditions.Count)
            {
                var actionModel = new ActionModel("REMOVE", "")
                {
                    ClassName = ruleModel.Conditions[condNum - 1].ClassName,
                    Atoms = new List<string> { "REMOVE", condNum.ToString() }
                };
                ruleModel.Actions.Add(actionModel);
            }
            else if (condNum > ruleModel.Conditions.Count)
                _logger.WriteError($"Condition reference '{condRef}' ({condNum}) out of range in Remove action of Rule {ruleModel.RuleName} (only {ruleModel.Conditions.Count} conditions defined)", "Rule Parser");
            else
                _logger.WriteError($"Unknown condition reference '{condRef}' in Remove action of Rule {ruleModel.RuleName}", "Rule Parser");
        }

        private void ParseRHSWrite(TokenStream stream, RuleModel ruleModel)
        {
            // (write [logicalname] atom1 atom2 <var> |text| (crlf) ...)
            string? logicalName = null;

            // Heuristic: check if first token is a logical file name
            if (stream.Check(TokenType.Identifier) && !stream.IsAtEnd)
            {
                var nextAfterIdent = stream.Peek(1);
                if (nextAfterIdent.Type != TokenType.RightParen)
                {
                    logicalName = stream.Current.Value;
                    stream.Advance();
                }
            }

            var atoms = new List<string> { "WRITE" };

            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.LeftParen))
                {
                    stream.Advance(); // skip (
                    if (stream.Check(TokenType.Identifier) && stream.Current.Value.ToLower() == "crlf")
                    {
                        stream.Advance(); // skip crlf
                    }
                    else if (stream.Check(TokenType.Identifier) && stream.Current.Value.ToLower() == "tabto")
                    {
                        stream.Advance(); // consume tabto keyword
                        atoms.Add("TABTO");
                        if (!stream.Check(TokenType.RightParen))
                        {
                            atoms.Add(stream.Current.Value);
                            stream.Advance();
                        }
                    }
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance();
                }
                else
                {
                    atoms.Add(ConsumeAtomValue(stream, ruleModel.RuleName));
                }
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            if (logicalName != null)
            {
                atoms.Add("TO");
                atoms.Add(logicalName);
            }

            var actionModel = new ActionModel("WRITE", "") { Atoms = atoms };
            ruleModel.Actions.Add(actionModel);
        }

        private void ParseRHSHalt(TokenStream stream, RuleModel ruleModel)
        {
            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            var actionModel = new ActionModel("HALT", "") { Atoms = new List<string> { "HALT" } };
            ruleModel.Actions.Add(actionModel);
        }

        private void ParseRHSCompute(TokenStream stream, RuleModel ruleModel)
        {
            // (compute <result> expression)
            // expression is prefix notation: (op arg1 arg2) or nested
            // Engine expects atoms: ["SET", "<var>", "=", "CALC", "op arg1 arg2"]

            string resultVar = ConsumeAtomValue(stream, ruleModel.RuleName);

            var calcAtoms = new List<string>();
            CollectComputeExpression(stream, calcAtoms, ruleModel.RuleName);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            var actionAtoms = new List<string> { "SET", resultVar, "=", "CALC", string.Join(" ", calcAtoms) };
            var actionModel = new ActionModel("SET", "") { Atoms = actionAtoms };
            ruleModel.Actions.Add(actionModel);
        }

        private void CollectComputeExpression(TokenStream stream, List<string> atoms, string ruleName)
        {
            // OPS5 uses prefix notation: (op arg1 arg2)
            // Engine Calc also uses prefix: op arg1 arg2
            if (stream.Check(TokenType.LeftParen))
            {
                stream.Advance(); // skip (

                // Read operator
                string op = "";
                if (stream.Check(TokenType.Plus)) { op = "+"; stream.Advance(); }
                else if (stream.Check(TokenType.Minus)) { op = "-"; stream.Advance(); }
                else if (stream.Check(TokenType.Star)) { op = "*"; stream.Advance(); }
                else if (stream.Check(TokenType.Slash)) { op = stream.Current.Value; stream.Advance(); }
                else if (stream.Check(TokenType.Backslash)) { op = "\\"; stream.Advance(); }
                else { op = ConsumeAtomValue(stream, ruleName); }

                // Add operator BEFORE operands (prefix notation)
                atoms.Add(op);

                // Read operands (could be nested)
                CollectComputeExpression(stream, atoms, ruleName); // arg1
                CollectComputeExpression(stream, atoms, ruleName); // arg2

                if (stream.Check(TokenType.RightParen))
                    stream.Advance();
            }
            else if (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                atoms.Add(ConsumeAtomValue(stream, ruleName));
            }
        }

        private void ParseRHSBind(TokenStream stream, RuleModel ruleModel)
        {
            // (bind <var> expression)
            string var_ = ConsumeAtomValue(stream, ruleModel.RuleName);

            if (stream.Check(TokenType.LeftParen))
            {
                var nextToken = stream.Peek(1);
                if (nextToken.Type == TokenType.Identifier &&
                    (nextToken.Value.ToLower() == "accept" || nextToken.Value.ToLower() == "acceptline"))
                {
                    stream.Advance(); // skip (
                    string keyword = stream.Current.Value.ToLower();
                    stream.Advance(); // skip accept/acceptline

                    string ops5Action = keyword == "acceptline" ? "ACCEPTLINE" : "ACCEPT";

                    string? fileLogicalName = null;
                    if (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
                        fileLogicalName = ConsumeAtomValueRaw(stream);

                    var atoms = new List<string> { ops5Action, var_ };
                    if (fileLogicalName != null)
                    {
                        atoms.Add("FROM");
                        atoms.Add(fileLogicalName);
                    }

                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (accept)
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (bind)

                    var actionModel = new ActionModel(ops5Action, "") { Atoms = atoms };
                    ruleModel.Actions.Add(actionModel);
                }
                else if (nextToken.Type == TokenType.Identifier && nextToken.Value.ToLower() == "genatom")
                {
                    stream.Advance(); // skip (
                    stream.Advance(); // skip genatom

                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (genatom)
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (bind)

                    var atoms = new List<string> { "SET", var_, "=", "Genatom" };
                    var actionModel = new ActionModel("SET", "") { Atoms = atoms };
                    ruleModel.Actions.Add(actionModel);
                }
                else if (nextToken.Type == TokenType.Identifier && nextToken.Value.ToLower() == "substr")
                {
                    stream.Advance(); // skip (
                    stream.Advance(); // skip substr

                    var args = new List<string>();
                    while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
                        args.Add(ConsumeAtomValue(stream, ruleModel.RuleName));

                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (substr)
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (bind)

                    var atoms = new List<string> { "SET", var_, "=", "SUBSTR", string.Join(" ", args) };
                    var actionModel = new ActionModel("SET", "") { Atoms = atoms };
                    ruleModel.Actions.Add(actionModel);
                }
                else
                {
                    // Compute expression
                    var calcAtoms = new List<string>();
                    CollectComputeExpression(stream, calcAtoms, ruleModel.RuleName);
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance();

                    var atoms = new List<string> { "SET", var_, "=", "CALC", string.Join(" ", calcAtoms) };
                    var actionModel = new ActionModel("SET", "") { Atoms = atoms };
                    ruleModel.Actions.Add(actionModel);
                }
            }
            else
            {
                string val = ConsumeAtomValue(stream, ruleModel.RuleName);
                if (stream.Check(TokenType.RightParen))
                    stream.Advance();

                var atoms = new List<string> { "SET", var_, "=", val };
                var actionModel = new ActionModel("SET", "") { Atoms = atoms };
                ruleModel.Actions.Add(actionModel);
            }
        }

        private void ParseRHSCall(TokenStream stream, RuleModel ruleModel)
        {
            // (call progname arg1 arg2 ...)
            var atoms = new List<string> { "CALL" };

            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
                atoms.Add(ConsumeAtomValue(stream, ruleModel.RuleName));

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            var actionModel = new ActionModel("CALL", "") { Atoms = atoms };
            ruleModel.Actions.Add(actionModel);
        }

        private void ParseRHSOpenFile(TokenStream stream, RuleModel ruleModel)
        {
            // (openfile logicalname "filepath" in|out)
            if (stream.Check(TokenType.RightParen) || stream.IsAtEnd)
            {
                SkipToMatchingParen(stream);
                return;
            }

            string logicalName = ConsumeAtomValueRaw(stream);
            string filePath = ConsumeAtomValue(stream, ruleModel.RuleName);
            string mode = ConsumeAtomValueRaw(stream).ToLower();

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            string ops5Mode = mode == "in" ? "In" : "Out";

            var atoms = new List<string> { "OPENFILE", logicalName, filePath, ops5Mode };
            var actionModel = new ActionModel("OPENFILE", "") { Atoms = atoms };
            ruleModel.Actions.Add(actionModel);
        }

        private void ParseRHSCloseFile(TokenStream stream, RuleModel ruleModel)
        {
            // (closefile logicalname)
            if (stream.Check(TokenType.RightParen) || stream.IsAtEnd)
            {
                SkipToMatchingParen(stream);
                return;
            }

            string logicalName = ConsumeAtomValueRaw(stream);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            var atoms = new List<string> { "CLOSEFILE", logicalName };
            var actionModel = new ActionModel("CLOSEFILE", "") { Atoms = atoms };
            ruleModel.Actions.Add(actionModel);
        }

        private void ParseRHSCBind(TokenStream stream, RuleModel ruleModel)
        {
            // (cbind <var>)
            string var_ = ConsumeAtomValue(stream, ruleModel.RuleName);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            var actionModel = new ActionModel("CBIND", "")
            {
                Atoms = new List<string> { "CBIND", var_ }
            };
            ruleModel.Actions.Add(actionModel);
        }

        // ==================== Helpers ====================

        /// <summary>
        /// Consume a single atom value from the stream with variable qualification.
        /// String literals are returned without surrounding quotes (matching engine expectations).
        /// </summary>
        private string ConsumeAtomValue(TokenStream stream, string ruleName)
        {
            var token = stream.Current;
            switch (token.Type)
            {
                case TokenType.Variable:
                    stream.Advance();
                    return "<" + token.Value.ToUpper() + "." + ruleName.ToUpper() + ">";

                case TokenType.StringLiteral:
                case TokenType.IntegerLiteral:
                case TokenType.DecimalLiteral:
                case TokenType.Identifier:
                    stream.Advance();
                    return token.Value;

                default:
                    stream.Advance();
                    return token.Value;
            }
        }

        /// <summary>
        /// Consume a single atom value without variable qualification.
        /// </summary>
        private string ConsumeAtomValueRaw(TokenStream stream)
        {
            var token = stream.Current;
            switch (token.Type)
            {
                case TokenType.Variable:
                    stream.Advance();
                    return $"<{token.Value}>";

                case TokenType.StringLiteral:
                case TokenType.IntegerLiteral:
                case TokenType.DecimalLiteral:
                case TokenType.Identifier:
                    stream.Advance();
                    return token.Value;

                default:
                    stream.Advance();
                    return token.Value;
            }
        }

        /// <summary>
        /// Collect ^attr value pairs for Make actions into an atoms list.
        /// </summary>
        private void CollectMakeAttrValuePairs(TokenStream stream, List<string> atoms)
        {
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.Caret))
                {
                    stream.Advance(); // skip ^
                    if (stream.Check(TokenType.Identifier))
                    {
                        string attr = stream.Current.Value;
                        stream.Advance();
                        string val = ConsumeAtomValueRaw(stream);
                        atoms.Add(attr);
                        atoms.Add(val);
                    }
                }
                else
                {
                    stream.Advance();
                }
            }
        }

        /// <summary>
        /// Collect ^attr value pairs for RHS Make/Modify actions into an object atoms list.
        /// </summary>
        private void CollectMakeAttrValuePairsAsObjects(TokenStream stream, List<object> atoms, string ruleName)
        {
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.Caret))
                {
                    stream.Advance(); // skip ^
                    if (stream.Check(TokenType.Identifier))
                    {
                        string attr = stream.Current.Value;
                        stream.Advance();
                        string val = ConsumeAtomValue(stream, ruleName);
                        atoms.Add(attr);
                        atoms.Add(val);
                    }
                }
                else
                {
                    stream.Advance();
                }
            }
        }

        private static bool IsPredicateOperator(TokenType type)
            => type == TokenType.LessThan ||
               type == TokenType.GreaterThan ||
               type == TokenType.LessOrEqual ||
               type == TokenType.GreaterOrEqual ||
               type == TokenType.NotEquals ||
               type == TokenType.Equals;

        private static string GetOperatorString(TokenType type) => type switch
        {
            TokenType.LessThan => "<",
            TokenType.GreaterThan => ">",
            TokenType.LessOrEqual => "<=",
            TokenType.GreaterOrEqual => ">=",
            TokenType.NotEquals => "<>",
            TokenType.Equals => "=",
            _ => "="
        };

        private static int ResolveConditionRef(string reference, RuleModel ruleModel)
        {
            if (int.TryParse(reference, out int index))
                return index;
            if (ruleModel.ConditionAliases.TryGetValue(reference, out int aliasIndex))
                return aliasIndex;
            return -1;
        }

        private static void SkipToMatchingParen(TokenStream stream)
        {
            int depth = 1;
            while (!stream.IsAtEnd && depth > 0)
            {
                if (stream.Check(TokenType.LeftParen))
                    depth++;
                else if (stream.Check(TokenType.RightParen))
                    depth--;
                stream.Advance();
            }
        }
    }
}
