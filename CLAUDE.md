# OPS5 Rules Engine

## Project Goal

A **pure OPS5 rules engine** suitable for open-source release. Implements the classic OPS5 production system with a RETE network in C#/.NET.

## Solution Structure

- **OPS5.Engine** - Core RETE engine, native OPS5 parser, working memory, conflict resolution
- **OPS5.Host** - Console application entry point (interactive REPL + file loading)
- **OPS5.Tests** - Unit tests (xUnit) for parser, engine components, calculators
- **OPS5.FunctionalTests** - End-to-end integration tests using `.ops5` test programs
- **Shared/AttributeLibrary** - Shared attribute data structures

Target framework: .NET 10. Solution file: `OPS5.slnx`

## Parsing Architecture

The parser reads OPS5 S-expression syntax directly and produces strongly-typed model objects:

```
.ops5 file (S-expressions)
    |
    v
OPS5Lexer (tokenizes S-expression syntax)
    |
    v
OPS5Parser (recursive descent, produces models directly)
    |
    v
ClassFileModel / DataFileModel / RuleFileModel
    |
    v
OPS5FileProcessing (registers classes, builds RETE network, loads data)
    |
    v
Engine (RETE network execution)
```

### Key Files - Parser
- `Parsers/OPS5/OPS5Parser.cs` - Native OPS5 parser (S-expr -> models)
- `Parsers/OPS5/OPS5ParseResult.cs` - Parser result type
- `Parsers/OPS5/OPS5Lexer.cs` - Lexer for OPS5 S-expression syntax
- `Contracts/Parser/IOPS5Parser.cs` - Parser interface

### Key Files - Shared Infrastructure
- `Parsers/Tokenizer/TokenStream.cs` - Token stream navigation
- `Parsers/Tokenizer/LexToken.cs` - Token representation
- `Parsers/Tokenizer/TokenType.cs` - Token type definitions
- `Parsers/Tokenizer/ParseDiagnostic.cs` - Error reporting

### Key Files - Models (parser output, consumed by engine)
- `Models/IOCCFileModel.cs` - ClassFileModel, ClassModel (ClassName, Atoms/attributes, inheritance)
- `Models/IOCDFileModel.cs` - DataFileModel, DataActionModel (Command, Atoms)
- `Models/IOCRFileModel.cs` - RuleFileModel, RuleModel, ConditionModel, ActionModel

### DI Registration
- `DI/InitialiseParserDI.cs` - Parser service registrations
- `DI/InitialiseDI.cs` - Core engine service registrations

### Entry Point
- `FileProcessing/OPS5FileProcessing.cs` - Orchestrates file loading:
  `ProcessFile()` --> `ProcessOPS5File()` --> parse --> process classes/rules/data --> engine

## Testing

- **Unit tests**: `OPS5.Tests/` - test parser, engine components (163 tests)
- **Functional tests**: `OPS5.FunctionalTests/` - load and run full `.ops5` programs (63 tests)
- Functional test programs are `.ops5` files in the `examples/` directory
- Test harness: `OPS5.FunctionalTests/Infrastructure/OPS5TestEngine.cs`

## Build & Test

```bash
dotnet build OPS5.Engine
dotnet test OPS5.Tests
dotnet test OPS5.FunctionalTests
```

## Book: OPS5 Revisited — Second Edition

Markdown source files are in `C:\Development\Book\SecondEdition\` (16 files, 00 through 14 + appendix-a).

To generate the book:

```bash
cd C:\Development\Book\SecondEdition
pandoc -o book.epub 00-front-matter.md 01-ops5.md 02-rete-network.md 03-architecture.md 04-working-memory.md 05-alpha-network.md 06-beta-network.md 07-conflict-resolution.md 08-parsing.md 09-parsing-lhs.md 10-parsing-rhs.md 11-runtime-actions.md 12-console.md 13-building-running.md 14-conclusion.md appendix-a.md
```

Replace `.epub` with `.docx` for Word format.

## OPS5 Language Features Supported

- `(literalize ...)` - Class/WME type declarations
- `(p rule-name ...)` - Production rules with LHS conditions and RHS actions
- `(make ...)` - Create working memory elements
- Negated conditions: `-(condition)`
- Variable binding: `<varname>`
- Predicates: `<`, `>`, `<=`, `>=`, `<>`, `=`
- Conjunctions: `{ pred1 pred2 }`
- Disjunctions: `<< val1 val2 >>`
- Arithmetic: `(compute ...)`
- Actions: make, modify, remove, write, halt, bind, openfile, closefile, accept, acceptline
- Conflict resolution: MEA (Means-Ends Analysis), LEX (Lexicographic)
