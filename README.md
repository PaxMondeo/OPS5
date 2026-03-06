# OPS5

A forward-chaining production rules engine based on the RETE algorithm (Forgy, 1982), implementing the OPS5 language.

## Features

- Full OPS5 language support (S-expression syntax)
- RETE algorithm for efficient pattern matching
- MEA and LEX conflict resolution strategies
- Interactive console with REPL
- Working memory operations: make, modify, remove
- Condition tests: =, <>, <, >, <=, >=, NOT, AND, OR (disjunction)
- Variable binding across conditions
- I/O: write, crlf, accept, openfile, closefile, tabto
- Arithmetic: compute (RPN calculator with +, -, *, /, //, \\)
- String operations: substr, genatom
- External function calls via (call)

## Requirements

- .NET 10.0 SDK

## Building

```bash
dotnet build OPS5.slnx
```

## Running

### Interactive mode
```bash
dotnet run --project OPS5.Host
```

### Load and run a file
```bash
dotnet run --project OPS5.Host -- path/to/program.ops5
```

### Console commands
Once in the interactive console, type `HELP` for a list of available commands.

## Testing

```bash
dotnet test OPS5.slnx
```

## Project Structure

```
OPS5.Engine/          Core RETE engine and OPS5 parser pipeline
OPS5.Host/            Console application host
OPS5.Tests/           Unit tests
OPS5.FunctionalTests/ End-to-end functional tests
Shared/               Shared attribute utilities
```

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

Based on the RETE algorithm as described in:
- Forgy, C.L. (1982). "Rete: A Fast Algorithm for the Many Pattern/Many Object Pattern Match Problem". *Artificial Intelligence*, 19(1), 17-37.
