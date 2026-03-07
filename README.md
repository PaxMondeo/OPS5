# OPS5

A forward-chaining production rules engine implementing the OPS5 language with the RETE algorithm (Forgy, 1982) in C#/.NET.

OPS5 was the language behind R1/XCON, the expert system that Digital Equipment Corporation used to configure VAX computer systems -- one of the great success stories of applied artificial intelligence. This engine is a modern, clean-room implementation with dependency injection, async support, and a full test suite.

## Companion Book

This engine is the companion source code for **OPS5 Revisited -- Second Edition**, which walks through building a complete OPS5 rules engine step by step, from the RETE algorithm through parsing to a working system.

## Features

- Full OPS5 language support (S-expression syntax)
- RETE algorithm for efficient pattern matching
- MEA and LEX conflict resolution strategies
- Interactive console with REPL
- Working memory operations: `make`, `modify`, `remove`
- Condition predicates: `=`, `<>`, `<`, `>`, `<=`, `>=`
- Conjunctions: `{ pred1 pred2 }` and disjunctions: `<< val1 val2 >>`
- Negated conditions: `-(condition)`
- Variable binding across conditions
- Arithmetic: `compute` / `bind` with `+`, `-`, `*`, `/`, `//`, `\`
- String operations: `substr`, `genatom`
- I/O: `write`, `crlf`, `accept`, `acceptline`, `openfile`, `closefile`, `tabto`
- External function calls via `(call)`

## Requirements

- [.NET 10.0 SDK](https://dot.net)

## Quick Start

```bash
# Clone the repository
git clone https://github.com/PaxMondeo/OPS5.git
cd OPS5

# Build
dotnet build OPS5.slnx

# Run an example
dotnet run --project OPS5.Host -- examples/HelloWorld/HelloWorld.ops5

# Start the interactive REPL
dotnet run --project OPS5.Host
```

## Examples

The [`examples/`](examples/) directory contains 13 sample OPS5 programs covering introductory programs, classic AI problems, and language feature demonstrations:

| Example | Description |
|---------|-------------|
| [HelloWorld](examples/HelloWorld/) | Minimal program -- variable binding and output |
| [BlocksWorld](examples/BlocksWorld/) | Classic AI blocks world |
| [TowerOfHanoi](examples/TowerOfHanoi/) | Recursive puzzle solving with negated conditions |
| [Calculator](examples/Calculator/) | Arithmetic with bind and compute |
| [Manners](examples/Manners/) | Dinner party seating -- complex constraint satisfaction |
| [StateMachine](examples/StateMachine/) | State transition pattern |

See [`examples/README.md`](examples/README.md) for the full list and a language quick reference.

## Project Structure

```
OPS5.Engine/          Core RETE engine, parser, working memory, conflict resolution
OPS5.Host/            Console application (interactive REPL + file loading)
OPS5.Tests/           Unit tests (xUnit)
OPS5.FunctionalTests/ End-to-end integration tests
Shared/               Shared attribute utilities
examples/             Sample OPS5 programs
```

## Testing

```bash
# Run all tests
dotnet test OPS5.slnx

# Unit tests only
dotnet test OPS5.Tests

# Functional (integration) tests only
dotnet test OPS5.FunctionalTests
```

## Console Commands

Once in the interactive console, type `HELP` for available commands including:
- Loading and running OPS5 files
- Inspecting working memory
- Stepping through rule execution
- Switching conflict resolution strategies

## License

This project is licensed under the GNU General Public License v3.0 -- see the [LICENSE](LICENSE) file for details.

## Acknowledgements

Based on the RETE algorithm as described in:
- Forgy, C.L. (1982). "Rete: A Fast Algorithm for the Many Pattern/Many Object Pattern Match Problem". *Artificial Intelligence*, 19(1), 17-37.

---

*Developed by [PaxMondeo](https://paxmondeo.com)*
