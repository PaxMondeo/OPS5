using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Parsers.Tokenizer;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Parsers.OPS5
{
    /// <summary>
    /// Transpiles OPS5 syntax into OPS5 engine text-format strings.
    ///
    /// This is a pure text-to-text translator with no dependency on OPS5 engine
    /// internal model types. It reads OPS5 S-expressions and generates
    /// equivalent .iocc, .iocd, and .iocr syntax as text strings.
    /// </summary>
    internal class OPS5Transpiler : IOPS5Transpiler
    {
        private readonly IOPS5Logger _logger;

        public OPS5Transpiler(IOPS5Logger logger)
        {
            _logger = logger;
        }

        public OPS5TranspileResult Transpile(string ops5Text, string fileName)
        {
            var result = new OPS5TranspileResult();
            var classesBuilder = new StringBuilder();
            var dataBuilder = new StringBuilder();
            var rulesBuilder = new StringBuilder();

            var lexer = new OPS5Lexer(ops5Text, fileName);
            var tokens = lexer.Tokenize();
            var stream = new TokenStream(tokens, fileName);

            // Report lexer diagnostics
            foreach (string diag in lexer.Diagnostics)
                result.Diagnostics.Add(diag);

            while (!stream.IsAtEnd)
            {
                try
                {
                    // Check for negation prefix (only valid before production conditions,
                    // but at top level it would be an error)
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
                                    EmitClass(stream, classesBuilder, result);
                                    break;

                                case "make":
                                    stream.Advance();
                                    EmitMake(stream, dataBuilder, result);
                                    break;

                                case "p":
                                    stream.Advance();
                                    EmitProduction(stream, rulesBuilder, result, fileName);
                                    break;

                                default:
                                    result.Diagnostics.Add($"Unknown top-level form '({keyword}' at line {stream.Current.Line} in {fileName}");
                                    SkipToMatchingParen(stream);
                                    break;
                            }
                        }
                        else
                        {
                            result.Diagnostics.Add($"Expected identifier after '(' at line {stream.Current.Line} in {fileName}");
                            SkipToMatchingParen(stream);
                        }
                    }
                    else
                    {
                        // Skip unexpected tokens at top level
                        stream.Advance();
                    }
                }
                catch (ParseException ex)
                {
                    result.Diagnostics.Add(ex.Message);
                    SkipToMatchingParen(stream);
                }
            }

            result.ClassesText = classesBuilder.ToString();
            result.DataText = dataBuilder.ToString();
            result.RulesText = rulesBuilder.ToString();

            return result;
        }

        // ==================== Literalize → Class ====================

        private void EmitClass(TokenStream stream, StringBuilder sb, OPS5TranspileResult result)
        {
            // (literalize class-name attr1 attr2 ...)
            // Already consumed: ( literalize
            // Need: class-name, then identifiers until )

            if (!stream.Check(TokenType.Identifier))
            {
                result.Diagnostics.Add($"Expected class name after 'literalize' at line {stream.Current.Line}");
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
                stream.Advance(); // consume )

            // Emit class syntax: Class ClassName (Attr1, Attr2, ...);
            sb.Append("Class ");
            sb.Append(FormatIdentifier(className));
            sb.Append(" (");
            for (int i = 0; i < attributes.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatIdentifier(attributes[i]));
            }
            sb.AppendLine(");");
        }

        // ==================== Make → Data ====================

        private void EmitMake(TokenStream stream, StringBuilder sb, OPS5TranspileResult result)
        {
            // (make class-name ^attr1 val1 ^attr2 val2 ...)
            // Already consumed: ( make

            if (!stream.Check(TokenType.Identifier))
            {
                result.Diagnostics.Add($"Expected class name after 'make' at line {stream.Current.Line}");
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            var pairs = new List<(string attr, string val)>();
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.Caret))
                {
                    stream.Advance(); // skip ^
                    if (stream.Check(TokenType.Identifier))
                    {
                        string attr = stream.Current.Value;
                        stream.Advance();

                        string val = ConsumeAtomValue(stream);
                        pairs.Add((attr, val));
                    }
                }
                else
                {
                    // Unexpected token — skip
                    stream.Advance();
                }
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            // Emit data syntax: Make ClassName (Attr1 val1, Attr2 val2);
            sb.Append("Make ");
            sb.Append(FormatIdentifier(className));
            sb.Append(" (");
            for (int i = 0; i < pairs.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatIdentifier(pairs[i].attr));
                sb.Append(' ');
                sb.Append(FormatValue(pairs[i].val));
            }
            sb.AppendLine(");");
        }

        // ==================== Production → Rule ====================

        private void EmitProduction(TokenStream stream, StringBuilder sb,
                                     OPS5TranspileResult result, string fileName)
        {
            // (p rule-name LHS --> RHS)
            // Already consumed: ( p

            if (!stream.Check(TokenType.Identifier))
            {
                result.Diagnostics.Add($"Expected rule name after 'p' at line {stream.Current.Line}");
                SkipToMatchingParen(stream);
                return;
            }

            string ruleName = stream.Current.Value;
            stream.Advance();

            sb.Append("Rule ");
            sb.Append(ruleName);
            sb.AppendLine(" (");

            // Parse LHS conditions until -->
            int conditionCount = 0;
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
                    conditionCount++;
                    EmitCondition(stream, sb, negative, result);
                }
                else
                {
                    // Unexpected — skip
                    stream.Advance();
                }
            }

            // Consume -->
            if (stream.Check(TokenType.Arrow))
            {
                stream.Advance();
                sb.AppendLine("    -->");
            }

            // Parse RHS actions until )
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.LeftParen))
                {
                    stream.Advance(); // consume (
                    EmitRHSAction(stream, sb, result, ruleName);
                }
                else
                {
                    stream.Advance();
                }
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance(); // consume closing )

            sb.AppendLine(");");
        }

        // ==================== LHS Condition ====================

        private void EmitCondition(TokenStream stream, StringBuilder sb,
                                    bool negative, OPS5TranspileResult result)
        {
            // Already consumed: ( [and the minus if negative]
            // Format: class-name ^attr1 val1 ^attr2 <var> ...

            if (!stream.Check(TokenType.Identifier))
            {
                result.Diagnostics.Add($"Expected class name in condition at line {stream.Current.Line}");
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            sb.Append("    ");
            if (negative) sb.Append("Not ");
            sb.Append(FormatIdentifier(className));
            sb.Append(" (");

            var tests = new List<string>();

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

                    string attr = stream.Current.Value;
                    stream.Advance();

                    // Check for conjunction: { ... }
                    if (stream.Check(TokenType.LeftBrace))
                    {
                        stream.Advance(); // skip {
                        EmitConjunction(stream, tests, attr);
                    }
                    // Check for disjunction: << ... >>
                    else if (stream.Check(TokenType.DoubleLeftAngle))
                    {
                        stream.Advance(); // skip <<
                        EmitDisjunction(stream, tests, attr);
                    }
                    // Check for predicate operator
                    else if (IsPredicateOperator(stream.Current.Type))
                    {
                        string op = GetOperatorString(stream.Current.Type);
                        stream.Advance();
                        string val = ConsumeAtomValue(stream);
                        tests.Add($"{FormatIdentifier(attr)} {op} {FormatValue(val)}");
                    }
                    // Simple value or variable binding
                    else
                    {
                        string val = ConsumeAtomValue(stream);
                        if (val.StartsWith("<") && val.EndsWith(">"))
                        {
                            // Variable binding — no operator
                            tests.Add($"{FormatIdentifier(attr)} {val}");
                        }
                        else
                        {
                            tests.Add($"{FormatIdentifier(attr)} = {FormatValue(val)}");
                        }
                    }
                }
                else
                {
                    // Unexpected token in condition — skip
                    stream.Advance();
                }
            }

            for (int i = 0; i < tests.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(tests[i]);
            }

            sb.AppendLine(");");

            if (stream.Check(TokenType.RightParen))
                stream.Advance(); // consume )
        }

        private void EmitConjunction(TokenStream stream, List<string> tests, string attr)
        {
            // { predicate1 predicate2 ... }
            // Each predicate is: operator value
            while (!stream.Check(TokenType.RightBrace) && !stream.IsAtEnd)
            {
                if (IsPredicateOperator(stream.Current.Type))
                {
                    string op = GetOperatorString(stream.Current.Type);
                    stream.Advance();
                    string val = ConsumeAtomValue(stream);
                    tests.Add($"{FormatIdentifier(attr)} {op} {FormatValue(val)}");
                }
                else
                {
                    // Could be an = test or variable
                    string val = ConsumeAtomValue(stream);
                    tests.Add($"{FormatIdentifier(attr)} = {FormatValue(val)}");
                }
            }

            if (stream.Check(TokenType.RightBrace))
                stream.Advance(); // skip }
        }

        private void EmitDisjunction(TokenStream stream, List<string> tests, string attr)
        {
            // << val1 val2 val3 >>
            var values = new List<string>();
            while (!stream.Check(TokenType.DoubleRightAngle) && !stream.IsAtEnd)
            {
                values.Add(ConsumeAtomValue(stream));
            }

            if (stream.Check(TokenType.DoubleRightAngle))
                stream.Advance(); // skip >>

            // Emit as disjunction: Attr << val1 val2 val3 >>
            var sb2 = new StringBuilder();
            sb2.Append(FormatIdentifier(attr));
            sb2.Append(" << ");
            sb2.Append(string.Join(" ", values));
            sb2.Append(" >>");
            tests.Add(sb2.ToString());
        }

        // ==================== RHS Actions ====================

        private void EmitRHSAction(TokenStream stream, StringBuilder sb,
                                    OPS5TranspileResult result, string ruleName)
        {
            // Already consumed: (
            // Read the action keyword

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
                    EmitRHSMake(stream, sb);
                    break;

                case "modify":
                    EmitRHSModify(stream, sb);
                    break;

                case "remove":
                    EmitRHSRemove(stream, sb);
                    break;

                case "write":
                    EmitRHSWrite(stream, sb);
                    break;

                case "halt":
                    sb.AppendLine("    Halt;");
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance();
                    break;

                case "compute":
                    EmitRHSCompute(stream, sb, ruleName);
                    break;

                case "bind":
                    EmitRHSBind(stream, sb);
                    break;

                case "crlf":
                    // (crlf) is a standalone newline output — skip it, handled inline by write
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance();
                    break;

                case "call":
                    EmitRHSCall(stream, sb);
                    break;

                case "accept":
                case "acceptline":
                    result.Diagnostics.Add($"OPS5 ({action}) without (bind) at line {stream.Current.Line} — use (bind <var> ({action})) instead");
                    SkipToMatchingParen(stream);
                    break;

                case "openfile":
                    EmitRHSOpenFile(stream, sb);
                    break;

                case "closefile":
                    EmitRHSCloseFile(stream, sb);
                    break;

                case "cbind":
                case "default":
                    result.Diagnostics.Add($"Unsupported OPS5 action '{action}' at line {stream.Current.Line} — skipping");
                    SkipToMatchingParen(stream);
                    break;

                default:
                    result.Diagnostics.Add($"Unknown RHS action '{action}' at line {stream.Current.Line}");
                    SkipToMatchingParen(stream);
                    break;
            }
        }

        private void EmitRHSMake(TokenStream stream, StringBuilder sb)
        {
            // (make class ^attr val ...)
            if (!stream.Check(TokenType.Identifier))
            {
                SkipToMatchingParen(stream);
                return;
            }

            string className = stream.Current.Value;
            stream.Advance();

            var pairs = new List<(string attr, string val)>();
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.Caret))
                {
                    stream.Advance();
                    if (stream.Check(TokenType.Identifier))
                    {
                        string attr = stream.Current.Value;
                        stream.Advance();
                        string val = ConsumeAtomValue(stream);
                        pairs.Add((attr, val));
                    }
                }
                else
                    stream.Advance();
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            sb.Append("    Make ");
            sb.Append(FormatIdentifier(className));
            sb.Append(" (");
            for (int i = 0; i < pairs.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatIdentifier(pairs[i].attr));
                sb.Append(' ');
                sb.Append(FormatValue(pairs[i].val));
            }
            sb.AppendLine(");");
        }

        private void EmitRHSModify(TokenStream stream, StringBuilder sb)
        {
            // (modify N ^attr val ...)
            string condRef = ConsumeAtomValue(stream);

            var pairs = new List<(string attr, string val)>();
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.Caret))
                {
                    stream.Advance();
                    if (stream.Check(TokenType.Identifier))
                    {
                        string attr = stream.Current.Value;
                        stream.Advance();
                        string val = ConsumeAtomValue(stream);
                        pairs.Add((attr, val));
                    }
                }
                else
                    stream.Advance();
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            sb.Append("    Modify ");
            sb.Append(condRef);
            sb.Append(" (");
            for (int i = 0; i < pairs.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatIdentifier(pairs[i].attr));
                sb.Append(' ');
                sb.Append(FormatValue(pairs[i].val));
            }
            sb.AppendLine(");");
        }

        private void EmitRHSRemove(TokenStream stream, StringBuilder sb)
        {
            // (remove N)
            string condRef = ConsumeAtomValue(stream);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            sb.Append("    Remove ");
            sb.Append(condRef);
            sb.AppendLine(";");
        }

        private void EmitRHSWrite(TokenStream stream, StringBuilder sb)
        {
            // (write [logicalname] atom1 atom2 <var> |text| (crlf) ...)
            // If the first token is a bare identifier (not a string literal, variable, or nested form),
            // treat it as a file logical name: Write (...) To logicalname;

            string? logicalName = null;

            // Heuristic: check if the first token looks like a logical file name
            // A logical name is a bare identifier that isn't followed by a closing paren (empty write)
            if (stream.Check(TokenType.Identifier) && !stream.IsAtEnd)
            {
                // Peek ahead: if there's at least one more content token after this identifier,
                // it's likely a logical file name followed by the actual write content
                var nextAfterIdent = stream.Peek(1);
                if (nextAfterIdent.Type != TokenType.RightParen)
                {
                    logicalName = stream.Current.Value;
                    stream.Advance();
                }
            }

            var atoms = new List<string>();

            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.LeftParen))
                {
                    // Nested form like (crlf)
                    stream.Advance(); // skip (
                    if (stream.Check(TokenType.Identifier) && stream.Current.Value.ToLower() == "crlf")
                    {
                        stream.Advance(); // skip crlf
                        // We don't add anything — Write outputs newline by default
                    }
                    else if (stream.Check(TokenType.Identifier) && stream.Current.Value.ToLower() == "tabto")
                    {
                        stream.Advance(); // consume tabto keyword
                        atoms.Add("TabTo");
                        if (!stream.Check(TokenType.RightParen))
                        {
                            atoms.Add(stream.Current.Value); // column number
                            stream.Advance();
                        }
                    }
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip )
                }
                else
                {
                    atoms.Add(ConsumeAtomValue(stream));
                }
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            sb.Append("    Write (");
            for (int i = 0; i < atoms.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(FormatValue(atoms[i]));
            }
            sb.Append(')');

            if (logicalName != null)
            {
                sb.Append(" To ");
                sb.Append(logicalName);
            }

            sb.AppendLine(";");
        }

        private void EmitRHSCompute(TokenStream stream, StringBuilder sb, string ruleName)
        {
            // (compute <result> expression)
            // expression can be: (op <a> <b>) or nested
            // Translate to: Set <result> = Calc(<a> <b> op);

            string resultVar = ConsumeAtomValue(stream);

            // Parse the arithmetic expression
            var calcAtoms = new List<string>();
            CollectComputeExpression(stream, calcAtoms);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            sb.Append("    Set ");
            sb.Append(resultVar);
            sb.Append(" = Calc(");
            sb.Append(string.Join(" ", calcAtoms));
            sb.AppendLine(");");
        }

        private void CollectComputeExpression(TokenStream stream, List<string> atoms)
        {
            // OPS5 compute uses prefix notation: (op arg1 arg2)
            // OPS5 engine Calc uses postfix/infix: arg1 arg2 op
            if (stream.Check(TokenType.LeftParen))
            {
                stream.Advance(); // skip (

                // Read operator
                string op = "";
                if (stream.Check(TokenType.Plus)) { op = "+"; stream.Advance(); }
                else if (stream.Check(TokenType.Minus)) { op = "-"; stream.Advance(); }
                else if (stream.Check(TokenType.Star)) { op = "*"; stream.Advance(); }
                else if (stream.Check(TokenType.Slash)) { op = "/"; stream.Advance(); }
                else if (stream.Check(TokenType.Backslash)) { op = "\\"; stream.Advance(); }
                else { op = ConsumeAtomValue(stream); }

                // Read operands (could be nested)
                CollectComputeExpression(stream, atoms); // arg1
                CollectComputeExpression(stream, atoms); // arg2

                // Add operator AFTER operands (postfix for Calc)
                atoms.Add(op);

                if (stream.Check(TokenType.RightParen))
                    stream.Advance();
            }
            else if (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                // Simple atom
                atoms.Add(ConsumeAtomValue(stream));
            }
        }

        private void EmitRHSBind(TokenStream stream, StringBuilder sb)
        {
            // (bind <var> expression)
            // Translate to: Set <var> = expression;
            string var_ = ConsumeAtomValue(stream);

            // Check if the value is (accept)/(acceptline), a compute expression, or a simple value
            if (stream.Check(TokenType.LeftParen))
            {
                // Peek ahead to see if this is (accept) or (acceptline)
                var nextToken = stream.Peek(1);
                if (nextToken.Type == TokenType.Identifier &&
                    (nextToken.Value.ToLower() == "accept" || nextToken.Value.ToLower() == "acceptline"))
                {
                    stream.Advance(); // skip (
                    string keyword = stream.Current.Value.ToLower();
                    stream.Advance(); // skip accept/acceptline

                    string ops5Action = keyword == "acceptline" ? "AcceptLine" : "Accept";

                    // Check for optional file logical name: (accept logicalname)
                    string? fileLogicalName = null;
                    if (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
                    {
                        fileLogicalName = ConsumeAtomValue(stream);
                    }

                    sb.Append("    ");
                    sb.Append(ops5Action);
                    sb.Append(' ');
                    sb.Append(var_);

                    if (fileLogicalName != null)
                    {
                        sb.Append(" From ");
                        sb.Append(fileLogicalName);
                    }

                    sb.AppendLine(";");

                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (accept)
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (bind)
                }
                else if (nextToken.Type == TokenType.Identifier && nextToken.Value.ToLower() == "substr")
                {
                    stream.Advance(); // skip (
                    stream.Advance(); // skip "substr"

                    // Collect arguments: string, start, length/inf
                    var args = new List<string>();
                    while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
                        args.Add(ConsumeAtomValue(stream));

                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (substr)
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance(); // skip ) closing (bind)

                    sb.Append("    Set ");
                    sb.Append(var_);
                    sb.Append(" = Substr(");
                    for (int i = 0; i < args.Count; i++)
                    {
                        if (i > 0) sb.Append(' ');
                        sb.Append(FormatValue(args[i]));
                    }
                    sb.AppendLine(");");
                }
                else
                {
                    var calcAtoms = new List<string>();
                    CollectComputeExpression(stream, calcAtoms);
                    if (stream.Check(TokenType.RightParen))
                        stream.Advance();

                    sb.Append("    Set ");
                    sb.Append(var_);
                    sb.Append(" = Calc(");
                    sb.Append(string.Join(" ", calcAtoms));
                    sb.AppendLine(");");
                }
            }
            else
            {
                string val = ConsumeAtomValue(stream);
                if (stream.Check(TokenType.RightParen))
                    stream.Advance();

                sb.Append("    Set ");
                sb.Append(var_);
                sb.Append(" = ");
                sb.Append(FormatValue(val));
                sb.AppendLine(";");
            }
        }

        private void EmitRHSCall(TokenStream stream, StringBuilder sb)
        {
            // (call progname arg1 arg2 ...)
            // Translate to: Execute progname arg1 arg2;
            var atoms = new List<string>();

            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                atoms.Add(ConsumeAtomValue(stream));
            }

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            sb.Append("    Execute ");
            sb.Append(string.Join(" ", atoms));
            sb.AppendLine(";");
        }

        private void EmitRHSOpenFile(TokenStream stream, StringBuilder sb)
        {
            // (openfile logicalname "filepath" in|out)
            // Translate to: OpenFile logicalname "filepath" In|Out;
            if (stream.Check(TokenType.RightParen) || stream.IsAtEnd)
            {
                SkipToMatchingParen(stream);
                return;
            }

            string logicalName = ConsumeAtomValue(stream);
            string filePath = ConsumeAtomValue(stream);
            string mode = ConsumeAtomValue(stream).ToLower();

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            string ops5Mode = mode == "in" ? "In" : "Out";

            sb.Append("    OpenFile ");
            sb.Append(logicalName);
            sb.Append(' ');
            sb.Append(FormatValue(filePath));
            sb.Append(' ');
            sb.Append(ops5Mode);
            sb.AppendLine(";");
        }

        private void EmitRHSCloseFile(TokenStream stream, StringBuilder sb)
        {
            // (closefile logicalname)
            // Translate to: CloseFile logicalname;
            if (stream.Check(TokenType.RightParen) || stream.IsAtEnd)
            {
                SkipToMatchingParen(stream);
                return;
            }

            string logicalName = ConsumeAtomValue(stream);

            if (stream.Check(TokenType.RightParen))
                stream.Advance();

            sb.Append("    CloseFile ");
            sb.Append(logicalName);
            sb.AppendLine(";");
        }

        // ==================== Helpers ====================

        /// <summary>
        /// Consume a single atom value from the stream (identifier, variable, number, string).
        /// Returns the raw value string.
        /// </summary>
        private string ConsumeAtomValue(TokenStream stream)
        {
            var token = stream.Current;
            switch (token.Type)
            {
                case TokenType.Variable:
                    stream.Advance();
                    return $"<{token.Value}>";

                case TokenType.StringLiteral:
                    stream.Advance();
                    return $"\"{token.Value}\"";

                case TokenType.IntegerLiteral:
                case TokenType.DecimalLiteral:
                    stream.Advance();
                    return token.Value;

                case TokenType.Identifier:
                    stream.Advance();
                    return token.Value;

                default:
                    stream.Advance();
                    return token.Value;
            }
        }

        /// <summary>Format an identifier for OPS5 engine output (preserve original casing).</summary>
        private static string FormatIdentifier(string name)
        {
            // Convert hyphenated OPS5 names to the same format
            // OPS5 engine accepts hyphens in identifiers, so keep as-is
            return name;
        }

        /// <summary>Format a value for OPS5 engine output.</summary>
        private static string FormatValue(string val)
        {
            // Variables and quoted strings are already formatted by ConsumeAtomValue
            return val;
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

        /// <summary>Skip tokens until we find the matching closing parenthesis.</summary>
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
