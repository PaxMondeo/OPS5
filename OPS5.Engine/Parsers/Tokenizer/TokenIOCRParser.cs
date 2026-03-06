using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// Token-based parser for .iocr rule files.
    /// </summary>
    internal class TokenIOCRParser : TokenParserBase, IIOCRParser
    {
        private readonly IWMClasses _WMClasses;

        public TokenIOCRParser(IOPS5Logger logger, ISourceFiles sourceFiles,
                               IWMClasses WMClasses)
            : base(logger, sourceFiles)
        {
            _WMClasses = WMClasses;
        }

        public IOCRFileModel ParseIOCRFile(string file, string fileName)
        {
            var model = new IOCRFileModel();

            string uFileName = fileName.ToUpper();
            if (!SourceFiles.RuleFiles.ContainsKey(uFileName))
                SourceFiles.RuleFiles.Add(uFileName,
                    new SourceFile(fileName, SourceFiles.ProjectFile.FilePath, "", file, true, true));

            try
            {
                var stream = LexAndSetup(file, fileName);

                while (!stream.IsAtEnd)
                {
                    if (stream.Check(TokenType.KW_Rule))
                    {
                        try
                        {
                            ParseRule(stream, model, fileName);
                        }
                        catch (ParseException ex)
                        {
                            Logger.WriteError(ex.Message, fileName);
                            // Skip to next Rule keyword
                            stream.SkipUntil(TokenType.KW_Rule);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteError(ex.Message, fileName);
                            stream.SkipUntil(TokenType.KW_Rule);
                        }
                    }
                    else
                    {
                        ReportError(stream, "Expected 'Rule' keyword", "rules file");
                        stream.SkipUntil(TokenType.KW_Rule);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex.Message, fileName);
            }

            return model;
        }

        private void ParseRule(TokenStream stream, IOCRFileModel model, string fileName)
        {
            stream.Expect(TokenType.KW_Rule);
            var ruleModel = new RuleModel { FileName = fileName };

            // Rule name
            var nameToken = stream.Advance();
            ruleModel.RuleName = nameToken.Value;

            // Deprecation check: rule-level ALL and ANY modifiers are no longer supported
            if (stream.Check(TokenType.KW_ALL))
            {
                stream.Advance();
                Logger.WriteError(
                    $"The ALL modifier on rule '{ruleModel.RuleName}' is no longer supported. " +
                    "Use 'Strategy MEA-ALL;' in your .ioc file instead.",
                    fileName);
            }
            else if (stream.Check(TokenType.KW_ANY))
            {
                // Check this is rule-level ANY (before the opening paren), not condition-level
                if (stream.Peek(1).Type == TokenType.LeftParen || stream.Peek(1).Type == TokenType.Semicolon)
                {
                    stream.Advance();
                    Logger.WriteError(
                        $"The ANY modifier on rule '{ruleModel.RuleName}' is no longer supported. " +
                        "Use ANY on individual conditions instead.",
                        fileName);
                }
            }

            Logger.WriteInfo($"Parsing Rule {ruleModel.RuleName}", 1);

            stream.Expect(TokenType.LeftParen);

            // === Parse LHS (conditions until -->) ===
            int order = 1;
            while (!stream.Check(TokenType.Arrow) && !stream.IsAtEnd)
            {
                ParseLHSLine(stream, ruleModel, ref order, fileName);
            }

            stream.Expect(TokenType.Arrow);
            Logger.WriteInfo("Completed LHS", 2);

            // === Parse RHS (actions until ); ) ===
            while (!stream.IsAtEnd)
            {
                // Check for rule-closing );
                if (stream.Check(TokenType.RightParen))
                {
                    stream.Advance();
                    stream.Expect(TokenType.Semicolon);
                    break;
                }
                ParseRHSAction(stream, ruleModel);
            }

            model.Rules.Add(ruleModel);
        }

        // ==================== LHS Parsing ====================

        private void ParseLHSLine(TokenStream stream, RuleModel ruleModel,
                                   ref int order, string fileName)
        {
            switch (stream.Current.Type)
            {
                case TokenType.KW_Disabled:
                    stream.Advance();
                    stream.Expect(TokenType.Semicolon);
                    ruleModel.Enabled = false;
                    break;

                case TokenType.KW_Comment:
                    stream.Advance();
                    var commentToken = stream.Advance();
                    ruleModel.Comment += commentToken.Value + "\n";
                    stream.Expect(TokenType.Semicolon);
                    break;

                case TokenType.KW_Set:
                    ParseSetDirective(stream, ruleModel);
                    break;

                case TokenType.KW_Check:
                    ParseCheckDirective(stream, ruleModel);
                    break;

                case TokenType.KW_FindPath:
                    ParseFindPathDirective(stream, ruleModel, ref order);
                    break;

                default:
                    ParseCondition(stream, ruleModel, ref order);
                    break;
            }
        }

        private void ParseCondition(TokenStream stream, RuleModel ruleModel, ref int order)
        {
            bool negative = stream.TryConsume(TokenType.Bang) != null;

            // Class name
            var classToken = stream.Advance();
            string className = classToken.Value;

            // Optional condition alias: ClassName alias ( ... )
            string? alias = null;
            if (!negative && stream.Check(TokenType.Identifier))
            {
                var afterAlias = stream.Peek(1).Type;
                if (afterAlias == TokenType.LeftParen || afterAlias == TokenType.Semicolon)
                {
                    alias = stream.Advance().Value;
                }
            }

            var conditionModel = new ConditionModel(order++, className, negative, "", false);

            // Validate and register alias
            if (alias != null)
            {
                if (ruleModel.ConditionAliases.ContainsKey(alias))
                    Logger.WriteError($"Duplicate condition alias '{alias}' in Rule {ruleModel.RuleName}", "Rule Parser");
                else
                {
                    conditionModel.Alias = alias;
                    ruleModel.ConditionAliases[alias] = conditionModel.Order;
                }
            }

            // CLASS = className test
            conditionModel.Tests.Add(new ConditionTest("CLASS", "=", conditionModel.ClassName));

            // Parentheses are optional around attribute tests
            bool hasParens = stream.TryConsume(TokenType.LeftParen) != null;

            if (hasParens)
            {
                // Parse comma-separated attribute tests inside parens
                while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
                {
                    ParseAttributeTest(stream, conditionModel, ruleModel.RuleName);
                    stream.TryConsume(TokenType.Comma);
                }
                stream.Expect(TokenType.RightParen);
            }
            else
            {
                // No parens: parse attribute tests until semicolon
                while (!stream.Check(TokenType.Semicolon) && !stream.IsAtEnd)
                {
                    ParseAttributeTest(stream, conditionModel, ruleModel.RuleName);
                    stream.TryConsume(TokenType.Comma);
                }
            }

            // Check for trailing ANY
            if (stream.TryConsume(TokenType.KW_ANY) != null)
                conditionModel.IsAny = true;

            stream.Expect(TokenType.Semicolon);

            ruleModel.Conditions.Add(conditionModel);
        }

        private void ParseAttributeTest(TokenStream stream, ConditionModel condition, string ruleName)
        {
            // Read attribute name
            var attrToken = stream.Advance();
            string attr = attrToken.Value.ToUpper();

            // Special DATETIME class handling
            if (condition.ClassName == "DATETIME")
            {
                ParseDateTimeTest(stream, attr, condition, ruleName);
                return;
            }

            // Determine what follows: operator or implicit =
            ParseTestOperator(stream, attr, condition, ruleName);
        }

        private void ParseDateTimeTest(TokenStream stream, string attr,
                                        ConditionModel condition, string ruleName)
        {
            // Read optional operator
            string op = "=";
            if (IsComparisonOperator(stream.Current.Type))
            {
                op = stream.Advance().Value;
            }

            // Read value
            string val = ConsumeValueWithVarQualify(stream, ruleName);

            condition.Tests.Add(new ConditionTest(attr, op, val));
        }

        private void ParseTestOperator(TokenStream stream, string attr,
                                        ConditionModel condition, string ruleName)
        {
            var current = stream.Current;

            // Explicit comparison operator
            if (IsComparisonOperator(current.Type))
            {
                string op = stream.Advance().Value;
                ParseTestValue(stream, attr, op, condition, ruleName);
                return;
            }

            // Special operator keywords
            switch (current.Type)
            {
                case TokenType.KW_IN:
                case TokenType.KW_NotIN:
                    ParseInTest(stream, attr, condition, ruleName);
                    return;

                case TokenType.KW_Matches:
                    stream.Advance();
                    string matchVal = ConsumeValueWithVarQualify(stream, ruleName);
                    condition.Tests.Add(new ConditionTest(attr, "MATCHES", matchVal) { MatchTest = true });
                    return;

                case TokenType.KW_Contains:
                    stream.Advance();
                    string containsVal = ConsumeValueWithVarQualify(stream, ruleName);
                    condition.Tests.Add(new ConditionTest(attr, "CONTAINS", containsVal) { ContainsTest = true });
                    return;

                case TokenType.KW_Length:
                    stream.Advance();
                    string lengthOp = stream.Advance().Value;
                    string lengthVal = ConsumeValueWithVarQualify(stream, ruleName);
                    condition.Tests.Add(new ConditionTest(attr, lengthOp, lengthVal) { VectorLength = true });
                    return;

                case TokenType.KW_Conjunction:
                    stream.Advance();
                    ParseConjunction(stream, attr, condition, ruleName);
                    return;

                case TokenType.LeftBrace:
                    ParseBraceConjunction(stream, attr, condition, ruleName);
                    return;

                case TokenType.KW_Disjunction:
                    stream.Advance();
                    ParseDisjunctionValue(stream, attr, condition, ruleName);
                    return;

                case TokenType.DoubleLeftAngle:
                    ParseAngleDisjunction(stream, attr, condition);
                    return;

                case TokenType.KW_Vector:
                    ParseVectorTest(stream, attr, condition, ruleName);
                    return;

                default:
                    // Implicit = with value
                    string val = ConsumeValueWithVarQualify(stream, ruleName);
                    // Check for CONCAT
                    if (stream.Check(TokenType.KW_Concat))
                    {
                        string concat = val;
                        while (stream.TryConsume(TokenType.KW_Concat) != null)
                        {
                            string addn = ConsumeValueWithVarQualify(stream, ruleName);
                            concat += " " + addn;
                        }
                        condition.Tests.Add(new ConditionTest(attr, "=", concat) { Concatenation = true });
                    }
                    else
                    {
                        condition.Tests.Add(new ConditionTest(attr, "=", val));
                    }
                    return;
            }
        }

        private void ParseTestValue(TokenStream stream, string attr, string op,
                                     ConditionModel condition, string ruleName)
        {
            // Check for disjunction after operator
            if (stream.Check(TokenType.KW_Disjunction))
            {
                stream.Advance();
                ParseDisjunctionValue(stream, attr, condition, ruleName);
                return;
            }

            if (stream.Check(TokenType.DoubleLeftAngle))
            {
                ParseAngleDisjunction(stream, attr, condition);
                return;
            }

            string val = ConsumeValueWithVarQualify(stream, ruleName);

            // Check for CONCAT
            if (stream.Check(TokenType.KW_Concat))
            {
                string concat = val;
                while (stream.TryConsume(TokenType.KW_Concat) != null)
                {
                    string addn = ConsumeValueWithVarQualify(stream, ruleName);
                    concat += " " + addn;
                }
                condition.Tests.Add(new ConditionTest(attr, op, concat) { Concatenation = true });
            }
            else
            {
                condition.Tests.Add(new ConditionTest(attr, op, val));
            }
        }

        private void ParseInTest(TokenStream stream, string attr,
                                  ConditionModel condition, string ruleName)
        {
            string op = stream.Advance().Value.ToUpper(); // IN or !IN

            string val = ConsumeValueWithVarQualify(stream, ruleName);

            var inTest = new ConditionTest(attr, op, val) { InTest = true };

            // Check for => result variable binding
            if (stream.TryConsume(TokenType.FatArrow) != null)
            {
                inTest.InVar = ConsumeValueWithVarQualify(stream, ruleName).ToUpper();
            }

            condition.Tests.Add(inTest);
        }

        private void ParseConjunction(TokenStream stream, string attr,
                                       ConditionModel condition, string ruleName)
        {
            // Conjunction values are in parentheses after the CONJUNCTION keyword
            // or in the next token as a comma-separated string
            string conjContent = ConsumeValueWithVarQualify(stream, ruleName);
            // Parse the content as sub-tests
            var subParts = conjContent.Split(',');
            foreach (var part in subParts)
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    // Each sub-part is an operator+value like "=5" or ">10"
                    ParseSubConditionTest(trimmed, attr, condition);
                }
            }
        }

        private void ParseBraceConjunction(TokenStream stream, string attr,
                                             ConditionModel condition, string ruleName)
        {
            stream.Expect(TokenType.LeftBrace);
            // Parse tests inside braces
            while (!stream.Check(TokenType.RightBrace) && !stream.IsAtEnd)
            {
                ParseTestOperator(stream, attr, condition, ruleName);
                stream.TryConsume(TokenType.Comma);
            }
            stream.Expect(TokenType.RightBrace);
        }

        private void ParseDisjunctionValue(TokenStream stream, string attr,
                                            ConditionModel condition, string ruleName)
        {
            string disjContent = ConsumeValueWithVarQualify(stream, ruleName);
            condition.Tests.Add(new ConditionTest(attr, "=", "<<" + disjContent + ">>"));
        }

        private void ParseAngleDisjunction(TokenStream stream, string attr,
                                            ConditionModel condition)
        {
            stream.Expect(TokenType.DoubleLeftAngle);
            var values = new List<string>();
            while (!stream.Check(TokenType.DoubleRightAngle) && !stream.IsAtEnd)
            {
                values.Add(ConsumeValue(stream));
                stream.TryConsume(TokenType.Comma);
            }
            stream.Expect(TokenType.DoubleRightAngle);
            condition.Tests.Add(new ConditionTest(attr, "=", "<<" + string.Join(" ", values) + ">>"));
        }

        private void ParseVectorTest(TokenStream stream, string attr,
                                      ConditionModel condition, string ruleName)
        {
            stream.Advance(); // consume Vector keyword
            string vectorContent = ConsumeValueWithVarQualify(stream, ruleName);
            int v = 0;
            foreach (var bit in vectorContent.Split(','))
            {
                string val = bit.Trim();
                if (val.StartsWith("<"))
                    val = val.ToUpper();
                var vecTest = new ConditionTest(attr, "=", val);
                vecTest.VectorVar = v++;
                condition.Tests.Add(vecTest);
            }
        }

        private void ParseSubConditionTest(string expr, string attr, ConditionModel condition)
        {
            string op = "=";
            string val = expr;

            // Try to extract operator prefix
            if (expr.StartsWith(">="))
            { op = ">="; val = expr[2..].Trim(); }
            else if (expr.StartsWith("<="))
            { op = "<="; val = expr[2..].Trim(); }
            else if (expr.StartsWith("!="))
            { op = "!="; val = expr[2..].Trim(); }
            else if (expr.StartsWith("<>"))
            { op = "<>"; val = expr[2..].Trim(); }
            else if (expr.StartsWith("="))
            { op = "="; val = expr[1..].Trim(); }
            else if (expr.StartsWith(">"))
            { op = ">"; val = expr[1..].Trim(); }
            else if (expr.StartsWith("<"))
            { op = "<"; val = expr[1..].Trim(); }

            condition.Tests.Add(new ConditionTest(attr, op, val));
        }

        private void ParseSetDirective(TokenStream stream, RuleModel ruleModel)
        {
            stream.Advance(); // consume Set

            // SET <var> = CalcExpr;
            var varToken = stream.Advance();
            string var = QualifyVariable(varToken, ruleModel.RuleName).ToUpper();

            stream.Expect(TokenType.Equals);

            // Collect the rest until semicolon as the calc expression
            string calc = CollectCalcExpression(stream, ruleModel.RuleName);
            stream.Expect(TokenType.Semicolon);

            // Add to the last non-negative condition
            for (int x = 0; x < ruleModel.Conditions.Count; x++)
            {
                if (!ruleModel.Conditions[ruleModel.Conditions.Count - x - 1].Negative)
                {
                    ruleModel.Conditions[ruleModel.Conditions.Count - x - 1].Tests
                        .Add(new ConditionTest(calc, "=", var));
                    break;
                }
            }
        }

        /// <summary>
        /// Collect a calc expression string (e.g. "Calc(&lt;A.RULE&gt; &lt;B.RULE&gt; +)").
        /// Handles Calc, AddYears, etc. by formatting as "FuncName(content)" without
        /// extra spaces around parentheses, matching the old parser's output.
        /// </summary>
        private string CollectCalcExpression(TokenStream stream, string ruleName)
        {
            var parts = new List<string>();

            while (!stream.Check(TokenType.Semicolon) && !stream.IsAtEnd)
            {
                var token = stream.Current;
                if (IsCalcKeyword(token.Type))
                {
                    // Collect FuncName(content) as a single string
                    string funcName = token.Value;
                    stream.Advance();
                    if (stream.Check(TokenType.LeftParen))
                    {
                        stream.Advance(); // consume (
                        var inner = new List<string>();
                        int depth = 1;
                        while (depth > 0 && !stream.IsAtEnd)
                        {
                            if (stream.Check(TokenType.LeftParen))
                            {
                                depth++;
                                inner.Add("(");
                                stream.Advance();
                            }
                            else if (stream.Check(TokenType.RightParen))
                            {
                                depth--;
                                if (depth > 0)
                                {
                                    inner.Add(")");
                                    stream.Advance();
                                }
                                else
                                    stream.Advance(); // consume closing )
                            }
                            else
                            {
                                inner.Add(ConsumeValueForCalc(stream, ruleName));
                            }
                        }
                        parts.Add(funcName + "(" + string.Join(" ", inner) + ")");
                    }
                    else
                    {
                        parts.Add(funcName);
                    }
                }
                else
                {
                    parts.Add(ConsumeValueForCalc(stream, ruleName));
                }
            }

            return string.Join(" ", parts);
        }

        private static bool IsCalcKeyword(TokenType type) =>
            type == TokenType.KW_Calc ||
            type == TokenType.KW_AddYears ||
            type == TokenType.KW_AddMonths ||
            type == TokenType.KW_AddWeeks ||
            type == TokenType.KW_AddDays ||
            type == TokenType.KW_AddHours ||
            type == TokenType.KW_AddMins ||
            type == TokenType.KW_AddSecs;

        private void ParseCheckDirective(TokenStream stream, RuleModel ruleModel)
        {
            stream.Advance(); // consume Check

            var varToken = stream.Advance();
            string var = QualifyVariable(varToken, ruleModel.RuleName).ToUpper();

            // Collect rest as value (may contain Calc expressions)
            string val = CollectCalcExpression(stream, ruleModel.RuleName);
            stream.Expect(TokenType.Semicolon);

            val = QualifyVariablesInString(val, ruleModel.RuleName);

            var test = new ConditionTest(val, "=", var) { CheckTest = true };

            for (int x = 0; x < ruleModel.Conditions.Count; x++)
            {
                if (!ruleModel.Conditions[ruleModel.Conditions.Count - x - 1].Negative)
                {
                    ruleModel.Conditions[ruleModel.Conditions.Count - x - 1].Tests.Add(test);
                    break;
                }
            }
        }

        private void ParseFindPathDirective(TokenStream stream, RuleModel ruleModel, ref int order)
        {
            stream.Advance(); // consume FindPath

            var elements = new List<string>();
            while (!stream.Check(TokenType.Semicolon) && !stream.IsAtEnd)
            {
                // Skip punctuation that doesn't contribute to keyword-value pairs
                if (stream.Check(TokenType.Equals) ||
                    stream.Check(TokenType.Comma) ||
                    stream.Check(TokenType.LeftParen) ||
                    stream.Check(TokenType.RightParen))
                {
                    stream.Advance();
                    continue;
                }
                string val = ConsumeValueForCalc(stream, ruleModel.RuleName);
                elements.Add(val);
            }
            stream.Expect(TokenType.Semicolon);

            var findPath = new FindPathInfo();
            var conditionList = new List<string>();

            if (elements.Count > 0)
                findPath.EdgeClass = elements[0].ToUpper();

            for (int x = 1; x < elements.Count; x += 2)
            {
                if (x + 1 >= elements.Count) break;
                switch (elements[x].ToUpper())
                {
                    case "FROM":
                        findPath.FromAttr = elements[x].ToUpper();
                        findPath.FromVar = elements[x + 1].ToUpper();
                        break;
                    case "TO":
                        findPath.ToAttr = elements[x].ToUpper();
                        findPath.ToVar = elements[x + 1].ToUpper();
                        break;
                    case "DISTANCE":
                        findPath.DistAttr = elements[x].ToUpper();
                        findPath.DistVar = elements[x + 1].ToUpper();
                        break;
                    case "START":
                        findPath.StartVar = elements[x + 1].ToUpper();
                        break;
                    case "END":
                        findPath.EndVar = elements[x + 1].ToUpper();
                        break;
                    case "WHERE":
                        conditionList.AddRange(elements[x + 1].Split().ToList());
                        break;
                }
            }

            // Build condition for FindPath
            var conditionModel = new ConditionModel(order++, findPath.EdgeClass, false, "", true);
            conditionModel.Tests.Add(new ConditionTest("CLASS", "=", findPath.EdgeClass));
            for (int i = 0; i < conditionList.Count; i += 2)
            {
                if (i + 1 < conditionList.Count)
                    conditionModel.Tests.Add(new ConditionTest(conditionList[i], "=", conditionList[i + 1]));
            }
            ruleModel.PathCondition = conditionModel;
            ruleModel.IsFindPath = true;
            ruleModel.FindPathInfo = findPath;
        }

        // ==================== RHS Parsing ====================

        private void ParseRHSAction(TokenStream stream, RuleModel ruleModel)
        {
            try
            {
                var actionToken = stream.Current;

                // EXEC_SQL needs special handling: the parenthesized SQL command must be
                // preserved as a single string rather than broken into individual tokens.
                if (actionToken.Type == TokenType.KW_Exec_SQL)
                {
                    var execSqlAtoms = CollectExecSQLAtoms(stream);
                    if (execSqlAtoms.Count == 0) return;
                    var execSqlModel = new ActionModel("EXEC_SQL", "");
                    execSqlModel.Atoms = execSqlAtoms;
                    ruleModel.Actions.Add(execSqlModel);
                    return;
                }

                var atoms = CollectActionAtoms(stream, ruleModel);
                if (atoms.Count == 0) return;

                string command = atoms[0].ToString()!.ToUpper();
                var actionModel = new ActionModel(command, "");

                switch (command)
                {
                    case "MAKE":
                    case "MATRIX.MAKE":
                        if (atoms.Count > 1)
                            actionModel.ClassName = atoms[1].ToString()!.ToUpper();
                        actionModel.Actions = atoms;
                        ruleModel.Actions.Add(actionModel);
                        break;

                    case "MODIFY":
                        if (atoms.Count > 1)
                        {
                            string modRef = atoms[1].ToString()!;
                            int modCond = ResolveConditionRef(modRef, ruleModel);
                            if (modCond > 0 && modCond <= ruleModel.Conditions.Count)
                            {
                                atoms[1] = modCond.ToString();
                                actionModel.ClassName = ruleModel.Conditions[modCond - 1].ClassName;
                                actionModel.Actions = atoms;
                                ruleModel.Actions.Add(actionModel);
                            }
                            else if (modCond > ruleModel.Conditions.Count)
                                Logger.WriteError($"Condition reference '{modRef}' ({modCond}) out of range in Modify action of Rule {ruleModel.RuleName} (only {ruleModel.Conditions.Count} conditions defined)", "Rule Parser");
                            else
                                Logger.WriteError($"Unknown condition reference '{modRef}' in Modify action of Rule {ruleModel.RuleName}", "Rule Parser");
                        }
                        else
                            Logger.WriteError($"Syntax error in Modify action of Rule {ruleModel.RuleName}", "Rule Parser");
                        break;

                    case "REMOVE":
                        if (atoms.Count > 1)
                        {
                            string remRef = atoms[1].ToString()!;
                            int remCond = ResolveConditionRef(remRef, ruleModel);
                            if (remCond > 0 && remCond <= ruleModel.Conditions.Count)
                            {
                                atoms[1] = remCond.ToString();
                                actionModel.ClassName = ruleModel.Conditions[remCond - 1].ClassName;
                                actionModel.Atoms = FlattenAtomsToStrings(atoms);
                                ruleModel.Actions.Add(actionModel);
                            }
                            else if (remCond > ruleModel.Conditions.Count)
                                Logger.WriteError($"Condition reference '{remRef}' ({remCond}) out of range in Remove action of Rule {ruleModel.RuleName} (only {ruleModel.Conditions.Count} conditions defined)", "Rule Parser");
                            else
                                Logger.WriteError($"Unknown condition reference '{remRef}' in Remove action of Rule {ruleModel.RuleName}", "Rule Parser");
                        }
                        else
                            Logger.WriteError($"Syntax error in Remove action of Rule {ruleModel.RuleName}", "Rule Parser");
                        break;

                    case "REMOVEALL":
                    case "READDOCUMENT":
                    case "WRITEDOCUMENT":
                        if (atoms.Count > 1)
                            actionModel.ClassName = atoms[1].ToString()!.ToUpper();
                        actionModel.Atoms = FlattenAtomsToStrings(atoms);
                        ruleModel.Actions.Add(actionModel);
                        break;

                    case "EXEC_SP":
                    case "EXEC_FUNC":
                        if (atoms.Count >= 3)
                        {
                            string execRef = atoms[2].ToString()!;
                            int execCond = ResolveConditionRef(execRef, ruleModel);
                            if (execCond > 0 && execCond <= ruleModel.Conditions.Count)
                            {
                                atoms[2] = execCond.ToString();
                                actionModel.ClassName = atoms[1].ToString()!;
                                actionModel.Actions = atoms;
                                ruleModel.Actions.Add(actionModel);
                            }
                            else if (execCond > ruleModel.Conditions.Count)
                                Logger.WriteError($"Condition reference '{execRef}' ({execCond}) out of range in {command} of Rule {ruleModel.RuleName} (only {ruleModel.Conditions.Count} conditions defined)", "Rule Parser");
                            else
                                Logger.WriteError($"Unknown condition reference '{execRef}' in {command} of Rule {ruleModel.RuleName}", "Rule Parser");
                        }
                        else
                            Logger.WriteError($"Syntax error in {command} of Rule {ruleModel.RuleName}", "Rule Parser");
                        break;

                    case "READRANGE":
                        {
                            var atomStrings = FlattenAtomsToStrings(atoms);
                            int arrowIdx = atomStrings.IndexOf("=>");
                            actionModel.ClassName = arrowIdx >= 0 && arrowIdx + 1 < atomStrings.Count
                                ? atomStrings[arrowIdx + 1].ToUpper()
                                : (atomStrings.Count > 1 ? atomStrings[1].ToUpper() : "");
                            actionModel.Atoms = atomStrings;
                            ruleModel.Actions.Add(actionModel);
                        }
                        break;

                    case "WRITERANGE":
                        {
                            var atomStrings = FlattenAtomsToStrings(atoms);
                            int arrowIdx = atomStrings.IndexOf("<=");
                            actionModel.ClassName = arrowIdx >= 0 && arrowIdx + 1 < atomStrings.Count
                                ? atomStrings[arrowIdx + 1].ToUpper()
                                : (atomStrings.Count > 1 ? atomStrings[1].ToUpper() : "");
                            actionModel.Atoms = atomStrings;
                            ruleModel.Actions.Add(actionModel);
                        }
                        break;

                    case "MAKEMULTIPLE":
                        if (atoms.Count > 1)
                            actionModel.ClassName = atoms[1].ToString()!.ToUpper();
                        actionModel.Actions = atoms;
                        ruleModel.Actions.Add(actionModel);
                        break;

                    case "SET":
                    case "WRITE":
                    case "HALT":
                    case "WAIT":
                    case "EXEC_SQL":
                    case "READTABLE":
                    case "READTABLECHANGES":
                    case "WRITETABLE":
                    case "WRITECELLVALUE":
                    case "WRITECELLFORMULA":
                    case "WRITECELLFORMULAR1C1":
                    case "COPYCELLVALUE":
                    case "COPYCELLFORMULA":
                    case "WRITETABLECHANGES":
                    case "PREDICT":
                    case "TEST":
                    case "EXECUTE":
                    case "DELFILE":
                    case "INTERFACE":
                    case "MODIFYINTERFACE":
                    case "CONNECTINTERFACE":
                    case "DISCONNECTINTERFACE":
                    case "SEND":
                    case "EVENTLISTENER":
                    case "CONNECTEMAIL":
                    case "SENDEMAIL":
                    case "DISCONNECTEMAIL":
                    case "ADDTIMER":
                    case "REMOVETIMER":
                    case "ACCEPT":
                    case "ACCEPTLINE":
                    case "OPENFILE":
                    case "CLOSEFILE":
                        actionModel.Atoms = FlattenAtomsToStrings(atoms);
                        ruleModel.Actions.Add(actionModel);
                        break;

                    default:
                        Logger.WriteError($"Unknown RHS action '{command}' in Rule {ruleModel.RuleName}", "Rule Parser");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex.Message, "Rule Parser");
            }
        }

        /// <summary>
        /// Collect all tokens for an RHS action statement (until semicolon),
        /// producing a List of objects matching the format the engine expects.
        /// Handles special constructs: Calc, Where, Vector, Split.
        /// </summary>
        private List<object> CollectActionAtoms(TokenStream stream, RuleModel ruleModel)
        {
            var atoms = new List<object>();
            int parenDepth = 0;

            while (!stream.IsAtEnd)
            {
                // Check for statement end: semicolon at depth 0
                if (stream.Check(TokenType.Semicolon) && parenDepth == 0)
                {
                    stream.Advance();
                    break;
                }

                // Also check for rule-closing ); at depth 0
                if (stream.Check(TokenType.RightParen) && parenDepth == 0)
                    break;

                var token = stream.Current;

                switch (token.Type)
                {
                    case TokenType.LeftParen:
                        parenDepth++;
                        stream.Advance();
                        break;

                    case TokenType.RightParen:
                        parenDepth--;
                        stream.Advance();
                        break;

                    case TokenType.Comma:
                        stream.Advance();
                        break;

                    // Special action constructs
                    case TokenType.KW_Calc:
                    case TokenType.KW_AddYears:
                    case TokenType.KW_AddMonths:
                    case TokenType.KW_AddWeeks:
                    case TokenType.KW_AddDays:
                    case TokenType.KW_AddHours:
                    case TokenType.KW_AddMins:
                    case TokenType.KW_AddSecs:
                        string calcType = token.Value.ToUpper();
                        stream.Advance();
                        atoms.Add(calcType);
                        var calcContent = CollectParenContent(stream, ruleModel.RuleName);
                        atoms.Add(calcContent);
                        break;

                    case TokenType.KW_Where:
                        stream.Advance();
                        atoms.Add("WHERE");
                        var whereContent = CollectParenContent(stream, ruleModel.RuleName);
                        atoms.Add(whereContent);
                        break;

                    case TokenType.KW_Vector:
                        stream.Advance();
                        atoms.Add("VECTOR");
                        var vectorContent = CollectParenOrBracketContent(stream, ruleModel.RuleName);
                        atoms.Add(vectorContent);
                        break;

                    case TokenType.KW_VectorAppend:
                        stream.Advance();
                        atoms.Add("VECTOR.APPEND");
                        var appendContent = CollectParenOrBracketContent(stream, ruleModel.RuleName);
                        atoms.Add(appendContent);
                        break;

                    case TokenType.KW_VectorRemove:
                        stream.Advance();
                        atoms.Add("VECTOR.REMOVE");
                        if (stream.Check(TokenType.LeftParen) || stream.Check(TokenType.LeftBracket))
                        {
                            var removeContent = CollectParenOrBracketContent(stream, ruleModel.RuleName);
                            atoms.Add(removeContent);
                        }
                        else
                        {
                            // Single token: VECTOR.REMOVE Dog
                            atoms.Add(new List<string> { ConsumeValueForCalc(stream, ruleModel.RuleName) });
                        }
                        break;

                    case TokenType.KW_Split:
                    {
                        // Disambiguate: genuine SPLIT command vs literal "Split" value.
                        // A SPLIT command requires a source argument (variable, string, or bracketed content).
                        // If followed by a comma, semicolon, or closing paren, it's a literal value
                        // (e.g., Make Result (Test Split, Value <x>) where "Split" is just a string).
                        var nextType = stream.Peek(1).Type;
                        if (nextType == TokenType.Variable || nextType == TokenType.StringLiteral ||
                            nextType == TokenType.LeftParen || nextType == TokenType.LeftBracket)
                        {
                            // Genuine SPLIT command
                            stream.Advance();
                            atoms.Add("SPLIT");
                            if (stream.Check(TokenType.LeftParen) || stream.Check(TokenType.LeftBracket))
                            {
                                var splitContent = CollectParenOrBracketContent(stream, ruleModel.RuleName);
                                atoms.Add(splitContent);
                            }
                            else
                            {
                                // Non-bracketed: SPLIT <text> <delimiter>
                                var splitArgs = new List<string>();
                                splitArgs.Add(ConsumeValueForCalc(stream, ruleModel.RuleName));
                                splitArgs.Add(ConsumeValueForCalc(stream, ruleModel.RuleName));
                                atoms.Add(splitArgs);
                            }
                        }
                        else
                        {
                            // Literal "Split" value — treat as a regular atom
                            atoms.Add(token.Value);
                            stream.Advance();
                        }
                    }
                        break;

                    case TokenType.KW_Substr:
                    {
                        stream.Advance();
                        atoms.Add("SUBSTR");
                        if (stream.Check(TokenType.LeftParen) || stream.Check(TokenType.LeftBracket))
                        {
                            var substrContent = CollectParenOrBracketContent(stream, ruleModel.RuleName);
                            atoms.Add(substrContent);
                        }
                    }
                        break;

                    case TokenType.KW_Range:
                        stream.Advance();
                        atoms.Add("RANGE");
                        // Range content: TYPE from TO to BY step
                        var rangeList = new List<string>();
                        // Consume type (INT, DECIMAL, CHAR)
                        rangeList.Add(stream.Advance().Value.ToUpper());
                        rangeList.Add(ConsumeValueForCalc(stream, ruleModel.RuleName)); // from
                        stream.Advance(); // TO
                        rangeList.Add(ConsumeValueForCalc(stream, ruleModel.RuleName)); // to
                        stream.Advance(); // BY
                        rangeList.Add(ConsumeValueForCalc(stream, ruleModel.RuleName)); // step
                        atoms.Add(rangeList);
                        break;

                    case TokenType.Variable:
                        atoms.Add(QualifyVariable(token, ruleModel.RuleName));
                        stream.Advance();
                        break;

                    case TokenType.FormattedString:
                        atoms.Add("FORMATTED");
                        atoms.Add(QualifyVariablesInString(token.Value, ruleModel.RuleName));
                        stream.Advance();
                        break;

                    case TokenType.KW_OpenFile:
                    case TokenType.KW_CloseFile:
                    case TokenType.KW_TabTo:
                    case TokenType.KW_Out:
                    case TokenType.KW_Append:
                    case TokenType.KW_To:
                    case TokenType.KW_From:
                        atoms.Add(token.Value.ToUpper());
                        stream.Advance();
                        break;

                    case TokenType.FatArrow:
                        atoms.Add("=>");
                        stream.Advance();
                        break;

                    case TokenType.LessOrEqual:
                        atoms.Add("<=");
                        stream.Advance();
                        break;

                    default:
                        atoms.Add(QualifyVariablesInString(token.Value, ruleModel.RuleName));
                        stream.Advance();
                        break;
                }
            }

            return atoms;
        }

        /// <summary>
        /// Collect content inside parentheses as a List for Calc/Where expressions.
        /// </summary>
        private List<string> CollectParenContent(TokenStream stream, string ruleName)
        {
            var content = new List<string>();
            if (stream.TryConsume(TokenType.LeftParen) != null)
            {
                while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
                {
                    content.Add(ConsumeValueForCalc(stream, ruleName));
                }
                stream.TryConsume(TokenType.RightParen);
            }
            return content;
        }

        /// <summary>
        /// Collect content inside parentheses or brackets as a List for Vector/Split.
        /// </summary>
        private List<string> CollectParenOrBracketContent(TokenStream stream, string ruleName)
        {
            var content = new List<string>();
            bool isParen = stream.Check(TokenType.LeftParen);
            bool isBracket = stream.Check(TokenType.LeftBracket);

            if (isParen || isBracket)
            {
                stream.Advance();
                var closeType = isParen ? TokenType.RightParen : TokenType.RightBracket;
                while (!stream.Check(closeType) && !stream.IsAtEnd)
                {
                    if (stream.Check(TokenType.Comma))
                    {
                        stream.Advance();
                        continue;
                    }
                    content.Add(ConsumeValueForCalc(stream, ruleName));
                }
                stream.TryConsume(closeType);
            }
            return content;
        }

        // ==================== Variable Qualification ====================

        /// <summary>
        /// Qualify a variable token with the rule name: &lt;VAR&gt; becomes &lt;VAR.RULENAME&gt;
        /// </summary>
        private string QualifyVariable(LexToken token, string ruleName)
        {
            if (token.Type == TokenType.Variable)
                return "<" + token.Value.ToUpper() + "." + ruleName.ToUpper() + ">";
            return token.Value;
        }

        /// <summary>
        /// Qualify all variables in a string.
        /// </summary>
        private string QualifyVariablesInString(string input, string ruleName)
        {
            string ruleUpper = "." + ruleName.ToUpper();
            string pattern = "(?i)<[A-Z0-9]+?>";
            return Regex.Replace(input, pattern, m =>
            {
                string varName = m.Value[1..^1].ToUpper();
                return "<" + varName + ruleUpper + ">";
            });
        }

        /// <summary>
        /// Consume a value and qualify any variables with rule name.
        /// </summary>
        private string ConsumeValueWithVarQualify(TokenStream stream, string ruleName)
        {
            var token = stream.Current;
            if (token.Type == TokenType.Variable)
            {
                stream.Advance();
                return "<" + token.Value.ToUpper() + "." + ruleName.ToUpper() + ">";
            }
            return ConsumeValue(stream);
        }

        /// <summary>
        /// Consume a value for calc expressions - handles operators as values too.
        /// </summary>
        private string ConsumeValueForCalc(TokenStream stream, string ruleName)
        {
            var token = stream.Current;
            switch (token.Type)
            {
                case TokenType.Variable:
                    stream.Advance();
                    return "<" + token.Value.ToUpper() + "." + ruleName.ToUpper() + ">";
                case TokenType.Plus:
                    stream.Advance();
                    return "+";
                case TokenType.Minus:
                    stream.Advance();
                    return "-";
                case TokenType.Star:
                    stream.Advance();
                    return "*";
                case TokenType.Slash:
                    stream.Advance();
                    return "/";
                case TokenType.Percent:
                    stream.Advance();
                    return "%";
                case TokenType.Comma:
                    stream.Advance();
                    return ",";
                case TokenType.Equals:
                    stream.Advance();
                    return "=";
                case TokenType.FormattedString:
                    stream.Advance();
                    return QualifyVariablesInString(token.Value, ruleName);
                default:
                    return ConsumeValue(stream);
            }
        }

        // ==================== Helpers ====================

        private static bool IsComparisonOperator(TokenType type) =>
            type == TokenType.Equals ||
            type == TokenType.NotEquals ||
            type == TokenType.LessThan ||
            type == TokenType.GreaterThan ||
            type == TokenType.LessOrEqual ||
            type == TokenType.GreaterOrEqual;

        /// <summary>
        /// Resolves a condition reference (numeric index or alias name) to a 1-based condition number.
        /// Returns -1 if the reference is not a valid number and not a known alias.
        /// </summary>
        private static int ResolveConditionRef(string reference, RuleModel ruleModel)
        {
            if (int.TryParse(reference, out int index))
                return index;
            if (ruleModel.ConditionAliases.TryGetValue(reference, out int aliasIndex))
                return aliasIndex;
            return -1;
        }

        /// <summary>
        /// Collect atoms for an EXEC_SQL action, preserving the parenthesized SQL command
        /// as a single string. Produces: ["EXEC_SQL", connectionName, sqlCommand, optionalWAIT].
        /// The generic CollectActionAtoms breaks parenthesized content into individual tokens,
        /// losing commas, dots, and inner parens — which corrupts the SQL statement.
        /// </summary>
        private List<string> CollectExecSQLAtoms(TokenStream stream)
        {
            var atoms = new List<string>();

            // atoms[0]: the EXEC_SQL keyword
            atoms.Add(stream.Advance().Value);

            // atoms[1]: the connection name
            if (stream.IsAtEnd || stream.Check(TokenType.Semicolon))
            {
                Logger.WriteError("Syntax error in Exec_SQL: missing connection name", "Rule Parser");
                return atoms;
            }
            atoms.Add(stream.Advance().Value);

            // atoms[2]: the SQL command enclosed in parentheses — rebuild as a single string
            if (!stream.Check(TokenType.LeftParen))
            {
                Logger.WriteError("Syntax error in Exec_SQL: expected '(' before SQL command", "Rule Parser");
                return atoms;
            }
            stream.Advance(); // consume opening '('

            var sql = new System.Text.StringBuilder();
            int depth = 1;
            bool suppressSpace = false; // suppress leading space on next token (after dot or open-brace)

            while (!stream.IsAtEnd && depth > 0)
            {
                var token = stream.Current;

                if (token.Type == TokenType.LeftParen)
                {
                    depth++;
                    sql.Append('(');
                    stream.Advance();
                    suppressSpace = true; // no space after '('
                }
                else if (token.Type == TokenType.RightParen)
                {
                    depth--;
                    if (depth > 0)
                    {
                        sql.Append(')');
                        stream.Advance();
                    }
                    else
                    {
                        stream.Advance(); // consume closing ')'
                    }
                    suppressSpace = false;
                }
                else if (token.Type == TokenType.Comma)
                {
                    sql.Append(", ");
                    stream.Advance();
                    suppressSpace = true; // space already added after comma
                }
                else if (token.Type == TokenType.Dot)
                {
                    sql.Append('.');
                    stream.Advance();
                    suppressSpace = true; // no space after dot (e.g., dbo.TableName)
                }
                else if (token.Type == TokenType.LeftBrace)
                {
                    // Variable substitution: {varName} — preserve as-is for runtime regex
                    if (sql.Length > 0 && !suppressSpace) sql.Append(' ');
                    sql.Append('{');
                    stream.Advance();
                    suppressSpace = true; // no space inside braces
                }
                else if (token.Type == TokenType.RightBrace)
                {
                    sql.Append('}');
                    stream.Advance();
                    suppressSpace = false;
                }
                else
                {
                    // Regular token (identifier, number, keyword, etc.)
                    if (sql.Length > 0 && !suppressSpace)
                        sql.Append(' ');
                    sql.Append(token.Value);
                    stream.Advance();
                    suppressSpace = false;
                }
            }

            atoms.Add(sql.ToString());

            // atoms[3]: optional WAIT keyword before semicolon
            while (!stream.IsAtEnd && !stream.Check(TokenType.Semicolon))
            {
                if (stream.Check(TokenType.RightParen))
                    break; // rule-closing );
                atoms.Add(stream.Advance().Value);
            }

            // Consume the semicolon
            if (stream.Check(TokenType.Semicolon))
                stream.Advance();

            return atoms;
        }

        /// <summary>
        /// Convert a List of object atoms (which may contain List&lt;string&gt; entries for
        /// Calc/Where/Vector/Split) into a flat List of strings.
        /// For Vector content: comma-joined. For Calc/Where/Split content: space-joined.
        /// </summary>
        private static List<string> FlattenAtomsToStrings(List<object> atoms)
        {
            var result = new List<string>();
            for (int i = 0; i < atoms.Count; i++)
            {
                if (atoms[i] is string s)
                    result.Add(s);
                else if (atoms[i] is List<string> list)
                {
                    string prev = i > 0 ? (atoms[i - 1] as string)?.ToUpper() ?? "" : "";
                    if (prev == "VECTOR" || prev == "VECTOR.APPEND" || prev == "VECTOR.REMOVE")
                        result.Add(string.Join(",", list));
                    else
                        result.Add(string.Join(" ", list));
                }
                else
                    result.Add(atoms[i]?.ToString() ?? "");
            }
            return result;
        }
    }
}
