using System;
using System.Collections.Generic;

namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// Single-pass character-by-character lexer for OPS5 engine language files.
    /// Produces strongly-typed tokens with source position information.
    /// </summary>
    public class OPS5InternalLexer
    {
        private readonly string _source;
        private readonly string _fileName;
        private int _pos;
        private int _line;
        private int _column;
        private readonly List<ParseDiagnostic> _diagnostics = new();
        private TokenType _previousTokenType = TokenType.EOF;

        private static readonly Dictionary<string, TokenType> Keywords =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // File-level
            ["Project"] = TokenType.KW_Project,
            ["Verbosity"] = TokenType.KW_Verbosity,
            ["Load"] = TokenType.KW_Load,
            ["Run"] = TokenType.KW_Run,
            ["SQL"] = TokenType.KW_SQL,
            ["Persistence"] = TokenType.KW_Persistence,
            ["Strategy"] = TokenType.KW_Strategy,

            // Class definition
            ["Class"] = TokenType.KW_Class,
            ["Disabled"] = TokenType.KW_Disabled,
            ["Persistent"] = TokenType.KW_Persistent,
            ["PersistObject"] = TokenType.KW_PersistObject,

            // Rule definition
            ["Rule"] = TokenType.KW_Rule,
            ["Comment"] = TokenType.KW_Comment,
            ["ALL"] = TokenType.KW_ALL,
            ["ANY"] = TokenType.KW_ANY,
            ["Set"] = TokenType.KW_Set,
            ["Check"] = TokenType.KW_Check,
            ["FindPath"] = TokenType.KW_FindPath,

            // RHS actions
            ["Make"] = TokenType.KW_Make,
            ["Modify"] = TokenType.KW_Modify,
            ["Remove"] = TokenType.KW_Remove,
            ["RemoveAll"] = TokenType.KW_RemoveAll,
            ["Write"] = TokenType.KW_Write,
            ["Halt"] = TokenType.KW_Halt,
            ["Wait"] = TokenType.KW_Wait,
            ["MakeMultiple"] = TokenType.KW_MakeMultiple,
            ["Execute"] = TokenType.KW_Execute,
            ["DelFile"] = TokenType.KW_DelFile,
            ["Accept"] = TokenType.KW_Accept,
            ["AcceptLine"] = TokenType.KW_AcceptLine,
            ["OpenFile"] = TokenType.KW_OpenFile,
            ["CloseFile"] = TokenType.KW_CloseFile,
            ["TabTo"] = TokenType.KW_TabTo,
            ["Out"] = TokenType.KW_Out,
            ["Append"] = TokenType.KW_Append,

            // Database actions
            ["ReadTable"] = TokenType.KW_ReadTable,
            ["WriteTable"] = TokenType.KW_WriteTable,
            ["ReadTableChanges"] = TokenType.KW_ReadTableChanges,
            ["WriteTableChanges"] = TokenType.KW_WriteTableChanges,
            ["Exec_SP"] = TokenType.KW_Exec_SP,
            ["Exec_Func"] = TokenType.KW_Exec_Func,
            ["Exec_SQL"] = TokenType.KW_Exec_SQL,

            // Spreadsheet actions
            ["ReadRange"] = TokenType.KW_ReadRange,
            ["WriteRange"] = TokenType.KW_WriteRange,
            ["WriteCellValue"] = TokenType.KW_WriteCellValue,
            ["WriteCellFormula"] = TokenType.KW_WriteCellFormula,
            ["WriteCellFormulaR1C1"] = TokenType.KW_WriteCellFormulaR1C1,
            ["CopyCellValue"] = TokenType.KW_CopyCellValue,
            ["CopyCellFormula"] = TokenType.KW_CopyCellFormula,

            // Document actions
            ["ReadDocument"] = TokenType.KW_ReadDocument,
            ["WriteDocument"] = TokenType.KW_WriteDocument,

            // Prediction/ML
            ["Predict"] = TokenType.KW_Predict,
            ["Test"] = TokenType.KW_Test,

            // Interface/communication
            ["Interface"] = TokenType.KW_Interface,
            ["ModifyInterface"] = TokenType.KW_ModifyInterface,
            ["ConnectInterface"] = TokenType.KW_ConnectInterface,
            ["DisconnectInterface"] = TokenType.KW_DisconnectInterface,
            ["Send"] = TokenType.KW_Send,
            ["EventListener"] = TokenType.KW_EventListener,

            // Email
            ["Email"] = TokenType.KW_Email,
            ["ConnectEmail"] = TokenType.KW_ConnectEmail,
            ["SendEmail"] = TokenType.KW_SendEmail,
            ["DisconnectEmail"] = TokenType.KW_DisconnectEmail,
            ["ApiKey"] = TokenType.KW_ApiKey,
            ["ApiKeyEnv"] = TokenType.KW_ApiKeyEnv,
            ["Domain"] = TokenType.KW_Domain,
            ["Host"] = TokenType.KW_Host,
            ["Username"] = TokenType.KW_Username,
            ["Password"] = TokenType.KW_Password,
            ["PasswordEnv"] = TokenType.KW_PasswordEnv,
            ["SSL"] = TokenType.KW_SSL,
            ["Subject"] = TokenType.KW_Subject,
            ["Body"] = TokenType.KW_Body,
            ["BodyType"] = TokenType.KW_BodyType,
            ["ReplyTo"] = TokenType.KW_ReplyTo,
            ["CC"] = TokenType.KW_CC,
            ["BCC"] = TokenType.KW_BCC,
            ["Template"] = TokenType.KW_Template,
            ["Templates"] = TokenType.KW_Templates,

            // Timer
            ["AddTimer"] = TokenType.KW_AddTimer,
            ["RemoveTimer"] = TokenType.KW_RemoveTimer,
            ["Timezone"] = TokenType.KW_Timezone,
            ["Daily"] = TokenType.KW_Daily,
            ["Weekly"] = TokenType.KW_Weekly,
            ["Monthly"] = TokenType.KW_Monthly,
            ["Yearly"] = TokenType.KW_Yearly,
            ["Hourly"] = TokenType.KW_Hourly,

            // Condition operators
            ["IN"] = TokenType.KW_IN,
            ["Matches"] = TokenType.KW_Matches,
            ["Contains"] = TokenType.KW_Contains,
            ["Length"] = TokenType.KW_Length,
            ["Concat"] = TokenType.KW_Concat,
            ["Substr"] = TokenType.KW_Substr,
            ["NIL"] = TokenType.KW_NIL,

            // Calc
            ["Calc"] = TokenType.KW_Calc,
            ["AddYears"] = TokenType.KW_AddYears,
            ["AddMonths"] = TokenType.KW_AddMonths,
            ["AddWeeks"] = TokenType.KW_AddWeeks,
            ["AddDays"] = TokenType.KW_AddDays,
            ["AddHours"] = TokenType.KW_AddHours,
            ["AddMins"] = TokenType.KW_AddMins,
            ["AddSecs"] = TokenType.KW_AddSecs,

            // Compound constructs
            ["Vector"] = TokenType.KW_Vector,
            ["Where"] = TokenType.KW_Where,
            ["Split"] = TokenType.KW_Split,
            ["Range"] = TokenType.KW_Range,
            ["Conjunction"] = TokenType.KW_Conjunction,
            ["Disjunction"] = TokenType.KW_Disjunction,

            // Binding keywords
            ["Document"] = TokenType.KW_Document,
            ["Database"] = TokenType.KW_Database,
            ["TableDef"] = TokenType.KW_TableDef,
            ["TableDefs"] = TokenType.KW_TableDefs,
            ["StoredProc"] = TokenType.KW_StoredProc,
            ["DBFunction"] = TokenType.KW_DBFunction,
            ["Spreadsheet"] = TokenType.KW_Spreadsheet,
            ["SheetDef"] = TokenType.KW_SheetDef,
            ["RangeDef"] = TokenType.KW_RangeDef,
            ["CSVDef"] = TokenType.KW_CSVDef,
            ["Folder"] = TokenType.KW_Folder,
            ["Executable"] = TokenType.KW_Executable,

            // Data keywords
            ["CSVLoad"] = TokenType.KW_CSVLoad,
            ["XMLLoad"] = TokenType.KW_XMLLoad,

            // Binding attributes
            ["Type"] = TokenType.KW_Type,
            ["Serialisation"] = TokenType.KW_Serialisation,
            ["Serialization"] = TokenType.KW_Serialisation,
            ["Location"] = TokenType.KW_Location,
            ["Delimiter"] = TokenType.KW_Delimiter,
            ["HasHeadings"] = TokenType.KW_HasHeadings,
            ["Parameters"] = TokenType.KW_Parameters,
            ["Model"] = TokenType.KW_Model,
            ["GetModel"] = TokenType.KW_GetModel,
            ["Schema"] = TokenType.KW_Schema,
            ["Table"] = TokenType.KW_Table,
            ["ReadOnly"] = TokenType.KW_ReadOnly,
            ["Matrix"] = TokenType.KW_Matrix,
            ["Auto"] = TokenType.KW_Auto,
            ["Params"] = TokenType.KW_Params,
            ["Results"] = TokenType.KW_Results,
            ["Sheet"] = TokenType.KW_Sheet,
            ["Headings"] = TokenType.KW_Headings,
            ["Rows"] = TokenType.KW_Rows,
            ["Cols"] = TokenType.KW_Cols,
            ["Scan"] = TokenType.KW_Scan,
            ["Recursive"] = TokenType.KW_Recursive,
            ["Interval"] = TokenType.KW_Interval,
            ["Repeat"] = TokenType.KW_Repeat,
            ["Bindings"] = TokenType.KW_Bindings,
            ["Tabbed"] = TokenType.KW_Tabbed,
            ["Header"] = TokenType.KW_Header,

            // FindPath/Range
            ["From"] = TokenType.KW_From,
            ["To"] = TokenType.KW_To,
            ["By"] = TokenType.KW_By,
            ["Start"] = TokenType.KW_Start,
            ["End"] = TokenType.KW_End,
            ["Distance"] = TokenType.KW_Distance,
            ["Int"] = TokenType.KW_Int,
            ["Decimal"] = TokenType.KW_Decimal,
            ["Char"] = TokenType.KW_Char,
            ["Address"] = TokenType.KW_Address,
            ["Identifier"] = TokenType.KW_Identifier,

            // Date/Time type keywords
            ["DATETIME"] = TokenType.KW_DATETIME,
            ["DATE"] = TokenType.KW_DATE,
            ["TIME"] = TokenType.KW_TIME,
            ["DAY"] = TokenType.KW_DAY,
            ["NUMBER"] = TokenType.KW_NUMBER,
            ["TEXT"] = TokenType.KW_TEXT,
            ["GENERAL"] = TokenType.KW_GENERAL,
            ["FORMATTED"] = TokenType.KW_FORMATTED,
        };

        public OPS5InternalLexer(string source, string fileName)
        {
            _source = source ?? "";
            _fileName = fileName ?? "";
            _pos = 0;
            _line = 1;
            _column = 1;
        }

        public IReadOnlyList<ParseDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// Tokenizes the entire source, returning a list of tokens.
        /// Whitespace and line comments (//) are skipped.
        /// File comments (////) are included as FileComment tokens.
        /// The list always ends with an EOF token.
        /// </summary>
        public List<LexToken> Tokenize()
        {
            var tokens = new List<LexToken>();

            while (!IsAtEnd)
            {
                SkipWhitespace();
                if (IsAtEnd)
                    break;

                var token = NextToken();
                if (token.Type != TokenType.EOF)
                {
                    tokens.Add(token);
                    _previousTokenType = token.Type;
                }
            }

            tokens.Add(LexToken.Eof(_line, _column, _pos));
            return tokens;
        }

        private LexToken NextToken()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;
            char c = Peek();

            // String literal
            if (c == '"')
                return ScanStringLiteral();

            // Formatted string: $"..."
            if (c == '$' && Peek(1) == '"')
                return ScanFormattedString();

            // Comments: //// (file comment) or // (line comment) or / (operator)
            if (c == '/')
            {
                if (Peek(1) == '/')
                    return ScanComment();
                Advance();
                return MakeToken(TokenType.Slash, "/", startLine, startCol, startPos);
            }

            // Arrow: -->
            if (c == '-' && Peek(1) == '-' && Peek(2) == '>')
            {
                Advance(); Advance(); Advance();
                return MakeToken(TokenType.Arrow, "-->", startLine, startCol, startPos);
            }

            // Less-than family: <varName>, <<, <=, <>, <
            if (c == '<')
                return ScanLessThanOrVariable();

            // Greater-than family: >>, >=, >
            if (c == '>')
                return ScanGreaterThan();

            // Bang family: !=, !IN, !
            if (c == '!')
                return ScanBang();

            // Minus: negative number or operator
            if (c == '-')
                return ScanMinus();

            // Equals family: =>, =
            if (c == '=')
            {
                Advance();
                if (!IsAtEnd && Peek() == '>')
                {
                    Advance();
                    return MakeToken(TokenType.FatArrow, "=>", startLine, startCol, startPos);
                }
                return MakeToken(TokenType.Equals, "=", startLine, startCol, startPos);
            }

            // Numbers
            if (char.IsDigit(c))
                return ScanNumber(startLine, startCol, startPos);

            if (c == '.' && !IsAtEnd && _pos + 1 < _source.Length && char.IsDigit(Peek(1)))
                return ScanNumber(startLine, startCol, startPos);

            // Identifiers and keywords
            if (IsIdentifierStart(c))
                return ScanIdentifierOrKeyword();

            // Single-character punctuation
            Advance();
            return c switch
            {
                '(' => MakeToken(TokenType.LeftParen, "(", startLine, startCol, startPos),
                ')' => MakeToken(TokenType.RightParen, ")", startLine, startCol, startPos),
                ';' => MakeToken(TokenType.Semicolon, ";", startLine, startCol, startPos),
                ',' => MakeToken(TokenType.Comma, ",", startLine, startCol, startPos),
                ':' => MakeToken(TokenType.Colon, ":", startLine, startCol, startPos),
                '{' => MakeToken(TokenType.LeftBrace, "{", startLine, startCol, startPos),
                '}' => MakeToken(TokenType.RightBrace, "}", startLine, startCol, startPos),
                '[' => MakeToken(TokenType.LeftBracket, "[", startLine, startCol, startPos),
                ']' => MakeToken(TokenType.RightBracket, "]", startLine, startCol, startPos),
                '+' => MakeToken(TokenType.Plus, "+", startLine, startCol, startPos),
                '*' => MakeToken(TokenType.Star, "*", startLine, startCol, startPos),
                '%' => MakeToken(TokenType.Percent, "%", startLine, startCol, startPos),
                '\\' => MakeToken(TokenType.Backslash, "\\", startLine, startCol, startPos),
                '.' => MakeToken(TokenType.Dot, ".", startLine, startCol, startPos),
                _ => ScanError(c, startLine, startCol, startPos)
            };
        }

        private LexToken ScanStringLiteral()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;
            Advance(); // consume opening "

            var content = new System.Text.StringBuilder();
            while (!IsAtEnd && Peek() != '"')
            {
                if (Peek() == '\\' && _pos + 1 < _source.Length && Peek(1) == '"')
                {
                    content.Append('"');
                    Advance(); Advance(); // skip \"
                }
                else
                {
                    if (Peek() == '\n')
                    {
                        _line++;
                        _column = 0;
                    }
                    content.Append(Peek());
                    Advance();
                }
            }

            if (!IsAtEnd)
                Advance(); // consume closing "
            else
                AddDiagnostic(DiagnosticSeverity.Error, "Unterminated string literal",
                    startLine, startCol);

            return MakeToken(TokenType.StringLiteral, content.ToString(),
                startLine, startCol, startPos);
        }

        private LexToken ScanFormattedString()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;
            Advance(); // consume $
            Advance(); // consume opening "

            var content = new System.Text.StringBuilder();
            while (!IsAtEnd && Peek() != '"')
            {
                if (Peek() == '\\' && _pos + 1 < _source.Length && Peek(1) == '"')
                {
                    content.Append('"');
                    Advance(); Advance();
                }
                else
                {
                    if (Peek() == '\n')
                    {
                        _line++;
                        _column = 0;
                    }
                    content.Append(Peek());
                    Advance();
                }
            }

            if (!IsAtEnd)
                Advance(); // consume closing "
            else
                AddDiagnostic(DiagnosticSeverity.Error, "Unterminated formatted string",
                    startLine, startCol);

            return MakeToken(TokenType.FormattedString, content.ToString(),
                startLine, startCol, startPos);
        }

        private LexToken ScanComment()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;

            Advance(); // first /
            Advance(); // second /

            // Check if this is a file comment (////)
            bool isFileComment = !IsAtEnd && Peek() == '/' && _pos + 1 < _source.Length && Peek(1) == '/';
            if (isFileComment)
            {
                Advance(); // third /
                Advance(); // fourth /
            }

            // Read to end of line
            var content = new System.Text.StringBuilder();
            while (!IsAtEnd && Peek() != '\n' && Peek() != '\r')
            {
                content.Append(Peek());
                Advance();
            }

            if (isFileComment)
            {
                return MakeToken(TokenType.FileComment, content.ToString().TrimStart(),
                    startLine, startCol, startPos);
            }

            // Line comment - skip it and return next token
            SkipWhitespace();
            if (IsAtEnd)
                return LexToken.Eof(_line, _column, _pos);
            return NextToken();
        }

        private LexToken ScanLessThanOrVariable()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;
            Advance(); // consume <

            if (IsAtEnd)
                return MakeToken(TokenType.LessThan, "<", startLine, startCol, startPos);

            char next = Peek();

            // << (double left angle)
            if (next == '<')
            {
                Advance();
                return MakeToken(TokenType.DoubleLeftAngle, "<<", startLine, startCol, startPos);
            }

            // <= (less or equal)
            if (next == '=')
            {
                Advance();
                return MakeToken(TokenType.LessOrEqual, "<=", startLine, startCol, startPos);
            }

            // <> (not equals)
            if (next == '>')
            {
                Advance();
                return MakeToken(TokenType.NotEquals, "<>", startLine, startCol, startPos);
            }

            // <varName> (variable)
            if (char.IsLetter(next) || next == '_' || char.IsDigit(next))
            {
                var name = new System.Text.StringBuilder();
                while (!IsAtEnd && Peek() != '>')
                {
                    if (Peek() == '\n' || Peek() == ';')
                    {
                        // Not a variable - this is a less-than operator
                        // Restore position to just after the <
                        _pos = startPos + 1;
                        _line = startLine;
                        _column = startCol + 1;
                        return MakeToken(TokenType.LessThan, "<", startLine, startCol, startPos);
                    }
                    name.Append(Peek());
                    Advance();
                }

                if (!IsAtEnd)
                {
                    Advance(); // consume >
                    return MakeToken(TokenType.Variable, name.ToString(),
                        startLine, startCol, startPos);
                }

                // Unterminated variable - treat as less-than
                _pos = startPos + 1;
                _line = startLine;
                _column = startCol + 1;
                return MakeToken(TokenType.LessThan, "<", startLine, startCol, startPos);
            }

            // Plain less-than
            return MakeToken(TokenType.LessThan, "<", startLine, startCol, startPos);
        }

        private LexToken ScanGreaterThan()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;
            Advance(); // consume >

            if (!IsAtEnd)
            {
                if (Peek() == '>')
                {
                    Advance();
                    return MakeToken(TokenType.DoubleRightAngle, ">>",
                        startLine, startCol, startPos);
                }
                if (Peek() == '=')
                {
                    Advance();
                    return MakeToken(TokenType.GreaterOrEqual, ">=",
                        startLine, startCol, startPos);
                }
            }

            return MakeToken(TokenType.GreaterThan, ">", startLine, startCol, startPos);
        }

        private LexToken ScanBang()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;
            Advance(); // consume !

            if (!IsAtEnd)
            {
                // !=
                if (Peek() == '=')
                {
                    Advance();
                    return MakeToken(TokenType.NotEquals, "!=", startLine, startCol, startPos);
                }

                // !IN (must be followed by word boundary)
                if ((_pos + 1 < _source.Length) &&
                    (Peek() == 'I' || Peek() == 'i') &&
                    (Peek(1) == 'N' || Peek(1) == 'n'))
                {
                    // Check word boundary: !IN must not be followed by a letter
                    if (_pos + 2 >= _source.Length || !IsIdentifierChar(_source[_pos + 2]))
                    {
                        Advance(); Advance();
                        return MakeToken(TokenType.KW_NotIN, "!IN",
                            startLine, startCol, startPos);
                    }
                }
            }

            return MakeToken(TokenType.Bang, "!", startLine, startCol, startPos);
        }

        private LexToken ScanMinus()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;
            Advance(); // consume -

            // Check if this is a negative number (preceded by operator/comma/paren/start)
            if (!IsAtEnd && char.IsDigit(Peek()) && IsNegativeNumberContext())
            {
                return ScanNumber(startLine, startCol, startPos, negative: true);
            }

            return MakeToken(TokenType.Minus, "-", startLine, startCol, startPos);
        }

        private bool IsNegativeNumberContext()
        {
            return _previousTokenType == TokenType.EOF ||
                   _previousTokenType == TokenType.Equals ||
                   _previousTokenType == TokenType.NotEquals ||
                   _previousTokenType == TokenType.LessThan ||
                   _previousTokenType == TokenType.GreaterThan ||
                   _previousTokenType == TokenType.LessOrEqual ||
                   _previousTokenType == TokenType.GreaterOrEqual ||
                   _previousTokenType == TokenType.Comma ||
                   _previousTokenType == TokenType.LeftParen ||
                   _previousTokenType == TokenType.LeftBracket ||
                   _previousTokenType == TokenType.Plus ||
                   _previousTokenType == TokenType.Minus ||
                   _previousTokenType == TokenType.Star ||
                   _previousTokenType == TokenType.Slash ||
                   _previousTokenType == TokenType.Percent;
        }

        private LexToken ScanNumber(int startLine, int startCol, int startPos, bool negative = false)
        {
            var number = new System.Text.StringBuilder();
            if (negative)
                number.Append('-');

            bool hasDecimalPoint = false;

            while (!IsAtEnd && (char.IsDigit(Peek()) || Peek() == '.'))
            {
                if (Peek() == '.')
                {
                    // Check if this is a decimal point or a dot separator (like Vector.Append)
                    if (hasDecimalPoint || (_pos + 1 < _source.Length && !char.IsDigit(Peek(1))))
                        break;
                    hasDecimalPoint = true;
                }
                number.Append(Peek());
                Advance();
            }

            var type = hasDecimalPoint ? TokenType.DecimalLiteral : TokenType.IntegerLiteral;
            return MakeToken(type, number.ToString(), startLine, startCol, startPos);
        }

        private LexToken ScanIdentifierOrKeyword()
        {
            int startPos = _pos;
            int startLine = _line;
            int startCol = _column;

            var ident = new System.Text.StringBuilder();
            while (!IsAtEnd && IsIdentifierChar(Peek()))
            {
                ident.Append(Peek());
                Advance();
            }

            string text = ident.ToString();

            // Check for dot-qualified compound keywords: Vector.Append, Vector.Remove,
            // Matrix.Make, Matrix.AppendX
            if (!IsAtEnd && Peek() == '.')
            {
                string upper = text.ToUpperInvariant();
                if (upper == "VECTOR" || upper == "MATRIX")
                {
                    int savedPos = _pos;
                    int savedLine = _line;
                    int savedCol = _column;
                    Advance(); // consume .

                    var suffix = new System.Text.StringBuilder();
                    while (!IsAtEnd && IsIdentifierChar(Peek()))
                    {
                        suffix.Append(Peek());
                        Advance();
                    }

                    string compound = text + "." + suffix.ToString();
                    string compoundUpper = compound.ToUpperInvariant();

                    switch (compoundUpper)
                    {
                        case "VECTOR.APPEND":
                            return MakeToken(TokenType.KW_VectorAppend, compound,
                                startLine, startCol, startPos);
                        case "VECTOR.REMOVE":
                            return MakeToken(TokenType.KW_VectorRemove, compound,
                                startLine, startCol, startPos);
                        case "MATRIX.MAKE":
                            return MakeToken(TokenType.KW_Matrix_Make, compound,
                                startLine, startCol, startPos);
                        case "MATRIX.APPENDX":
                            return MakeToken(TokenType.KW_Matrix_AppendX, compound,
                                startLine, startCol, startPos);
                        default:
                            // Not a specific keyword, but still a valid compound identifier
                            // (e.g., VECTOR.LENGTH, VECTOR.INDEX, MATRIX.COUNTX, MATRIX.COUNTY, MATRIX.SUMY)
                            return MakeToken(TokenType.Identifier, compound,
                                startLine, startCol, startPos);
                    }
                }
            }

            // Keyword lookup
            if (Keywords.TryGetValue(text, out var keywordType))
                return MakeToken(keywordType, text, startLine, startCol, startPos);

            return MakeToken(TokenType.Identifier, text, startLine, startCol, startPos);
        }

        private LexToken ScanError(char c, int startLine, int startCol, int startPos)
        {
            AddDiagnostic(DiagnosticSeverity.Error,
                $"Unexpected character '{c}'", startLine, startCol);
            return MakeToken(TokenType.Error, c.ToString(), startLine, startCol, startPos);
        }

        // === Utility methods ===

        private char Peek(int ahead = 0)
        {
            int idx = _pos + ahead;
            return idx < _source.Length ? _source[idx] : '\0';
        }

        private char Advance()
        {
            char c = _source[_pos];
            _pos++;
            if (c == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            return c;
        }

        private bool IsAtEnd => _pos >= _source.Length;

        private void SkipWhitespace()
        {
            while (!IsAtEnd)
            {
                char c = Peek();
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }
        }

        private static bool IsIdentifierStart(char c) =>
            char.IsLetter(c) || c == '_';

        private static bool IsIdentifierChar(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '-';

        private LexToken MakeToken(TokenType type, string value,
            int startLine, int startCol, int startPos) =>
            new(type, value, startLine, startCol, startPos, _pos - startPos);

        private void AddDiagnostic(DiagnosticSeverity severity, string message,
            int line, int column)
        {
            _diagnostics.Add(new ParseDiagnostic
            {
                Severity = severity,
                Message = message,
                FileName = _fileName,
                Line = line,
                Column = column
            });
        }
    }
}
