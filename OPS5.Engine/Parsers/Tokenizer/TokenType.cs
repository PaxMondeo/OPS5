namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// All token types recognized by the OPS5 internal lexer.
    /// </summary>
    public enum TokenType
    {
        // === Literals ===
        StringLiteral,         // "quoted string" (Value = content without quotes)
        FormattedString,       // $"formatted string" (Value = content without $" and ")
        IntegerLiteral,        // 42, -5
        DecimalLiteral,        // 3.14, -2.5
        Identifier,            // unquoted names: class names, attribute names, values

        // === Variables ===
        Variable,              // <varName> (Value = "varName" without angle brackets)

        // === Operators ===
        Equals,                // =
        NotEquals,             // != or <>
        LessThan,              // <
        GreaterThan,           // >
        LessOrEqual,           // <=
        GreaterOrEqual,        // >=
        Arrow,                 // -->
        FatArrow,              // =>
        Bang,                  // !
        Plus,                  // +
        Minus,                 // -
        Star,                  // *
        Slash,                 // /
        Percent,               // %
        Backslash,             // \

        // === Punctuation ===
        LeftParen,             // (
        RightParen,            // )
        Semicolon,             // ;
        Comma,                 // ,
        Colon,                 // :
        Dot,                   // .
        LeftBrace,             // {
        RightBrace,            // }
        DoubleLeftAngle,       // <<
        DoubleRightAngle,      // >>
        LeftBracket,           // [
        RightBracket,          // ]

        // === Keywords - File-level ===
        KW_Project,
        KW_Verbosity,
        KW_Load,
        KW_Run,
        KW_SQL,
        KW_Persistence,
        KW_Strategy,

        // === Keywords - Class definition ===
        KW_Class,
        KW_Disabled,
        KW_Persistent,
        KW_PersistObject,

        // === Keywords - Rule definition ===
        KW_Rule,
        KW_Comment,
        KW_ALL,
        KW_ANY,
        KW_Set,
        KW_Check,
        KW_FindPath,

        // === Keywords - RHS actions ===
        KW_Make,
        KW_Modify,
        KW_Remove,
        KW_RemoveAll,
        KW_Write,
        KW_Halt,
        KW_Wait,
        KW_MakeMultiple,
        KW_Execute,
        KW_DelFile,
        KW_Accept,
        KW_AcceptLine,
        KW_OpenFile,
        KW_CloseFile,
        KW_TabTo,
        KW_Out,
        KW_Append,

        // === Keywords - Database actions ===
        KW_ReadTable,
        KW_WriteTable,
        KW_ReadTableChanges,
        KW_WriteTableChanges,
        KW_Exec_SP,
        KW_Exec_Func,
        KW_Exec_SQL,

        // === Keywords - Spreadsheet actions ===
        KW_ReadRange,
        KW_WriteRange,
        KW_WriteCellValue,
        KW_WriteCellFormula,
        KW_WriteCellFormulaR1C1,
        KW_CopyCellValue,
        KW_CopyCellFormula,

        // === Keywords - Document actions ===
        KW_ReadDocument,
        KW_WriteDocument,

        // === Keywords - Prediction/ML ===
        KW_Predict,
        KW_Test,

        // === Keywords - Interface/communication ===
        KW_Interface,
        KW_ModifyInterface,
        KW_ConnectInterface,
        KW_DisconnectInterface,
        KW_Send,
        KW_EventListener,

        // === Keywords - Email ===
        KW_Email,
        KW_ConnectEmail,
        KW_SendEmail,
        KW_DisconnectEmail,
        KW_Template,
        KW_Templates,

        // === Keywords - Condition operators ===
        KW_IN,
        KW_NotIN,              // !IN
        KW_Matches,
        KW_Contains,
        KW_Length,
        KW_Concat,
        KW_Substr,
        KW_NIL,

        // === Keywords - Calc ===
        KW_Calc,
        KW_AddYears,
        KW_AddMonths,
        KW_AddWeeks,
        KW_AddDays,
        KW_AddHours,
        KW_AddMins,
        KW_AddSecs,

        // === Keywords - Compound constructs ===
        KW_Vector,
        KW_VectorAppend,       // Vector.Append
        KW_VectorRemove,       // Vector.Remove
        KW_Where,
        KW_Split,
        KW_Range,
        KW_Conjunction,
        KW_Disjunction,
        KW_Matrix_Make,        // Matrix.Make
        KW_Matrix_AppendX,     // Matrix.AppendX

        // === Keywords - Binding ===
        KW_Document,
        KW_Database,
        KW_TableDef,
        KW_TableDefs,
        KW_StoredProc,
        KW_DBFunction,
        KW_Spreadsheet,
        KW_SheetDef,
        KW_RangeDef,
        KW_CSVDef,
        KW_Folder,
        KW_Executable,

        // === Keywords - Data ===
        KW_CSVLoad,
        KW_XMLLoad,

        // === Keywords - Binding attributes ===
        KW_Type,
        KW_Serialisation,
        KW_Location,
        KW_Delimiter,
        KW_HasHeadings,
        KW_Parameters,
        KW_Model,
        KW_GetModel,
        KW_Schema,
        KW_Table,
        KW_ReadOnly,
        KW_Matrix,
        KW_Auto,
        KW_Params,
        KW_Results,
        KW_Sheet,
        KW_Headings,
        KW_Rows,
        KW_Cols,
        KW_Scan,
        KW_Recursive,
        KW_Interval,
        KW_Repeat,
        KW_Bindings,
        KW_Tabbed,
        KW_Header,
        KW_ApiKey,
        KW_ApiKeyEnv,
        KW_Domain,
        KW_Host,
        KW_Username,
        KW_Password,
        KW_PasswordEnv,
        KW_SSL,
        KW_Subject,
        KW_Body,
        KW_BodyType,
        KW_ReplyTo,
        KW_CC,
        KW_BCC,

        // === Keywords - FindPath/Range ===
        KW_From,
        KW_To,
        KW_By,
        KW_Start,
        KW_End,
        KW_Distance,
        KW_Int,
        KW_Decimal,
        KW_Char,
        KW_Address,
        KW_Identifier,

        // === Keywords - Timer ===
        KW_AddTimer,
        KW_RemoveTimer,
        KW_Timezone,
        KW_Daily,
        KW_Weekly,
        KW_Monthly,
        KW_Yearly,
        KW_Hourly,

        // === Keywords - Date/Time ===
        KW_DATETIME,
        KW_DATE,
        KW_TIME,
        KW_DAY,
        KW_NUMBER,
        KW_TEXT,
        KW_GENERAL,
        KW_FORMATTED,

        // === OPS5 ===
        Caret,                 // ^ (attribute prefix in OPS5)

        // === Special ===
        FileComment,           // //// line comments (preserved for source files)
        EOF,                   // End of input
        Error                  // Unrecognized character
    }
}
