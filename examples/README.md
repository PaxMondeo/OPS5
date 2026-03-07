# OPS5 Example Programs

These example programs demonstrate the features of the OPS5 language and engine. Each program is self-contained in its own directory.

## Getting Started

Run any example from the repository root:

```bash
dotnet run --project OPS5.Host -- examples/HelloWorld/HelloWorld.ops5
```

## Examples

### Introductory

| Example | Description | Features Demonstrated |
|---------|-------------|----------------------|
| [HelloWorld](HelloWorld/) | Minimal OPS5 program | `literalize`, `make`, `write`, variable binding |
| [Calculator](Calculator/) | Arithmetic operations | `bind`, compute expressions (`+`, `-`, `*`, `/`), `remove` |
| [BlocksWorld](BlocksWorld/) | Classic AI blocks world | `modify`, `halt`, multi-condition matching |

### Classic AI Problems

| Example | Description | Features Demonstrated |
|---------|-------------|----------------------|
| [TowerOfHanoi](TowerOfHanoi/) | Recursive puzzle solving | Arithmetic, conjunction `{ }`, negated conditions, `bind` |
| [Manners](Manners/) | Dinner party seating | Multi-fact joins, max-finding via negation, complex variable binding |

### Language Features

| Example | Description | Features Demonstrated |
|---------|-------------|----------------------|
| [StateMachine](StateMachine/) | State transition pattern | Negated conditions, sequential rule firing |
| [ComparisonTest](ComparisonTest/) | All comparison operators | `=`, `<>`, `<`, `>`, `<=`, `>=` predicates |
| [ConjunctionTest](ConjunctionTest/) | AND predicates | `{ > 10 < 20 }` conjunction on a single attribute |
| [DisjunctionTest](DisjunctionTest/) | OR matching | `<< Red Amber >>` disjunction syntax |
| [StrategyTest](StrategyTest/) | Conflict resolution | LEX and MEA strategies |
| [GenatomTest](GenatomTest/) | Unique symbol generation | `genatom` for generating unique identifiers |
| [SubstrTest](SubstrTest/) | Substring operations | `substr` with literal positions, variables, and `INF` |
| [FileIOTest](FileIOTest/) | File I/O operations | `openfile`, `closefile`, `accept`, writing to files |

## OPS5 Language Quick Reference

```ops5
; Define a working memory element type
(literalize typename attribute1 attribute2)

; Create a working memory element
(make typename ^attribute1 value1 ^attribute2 value2)

; Define a production rule
(p rule-name
    (condition1 ^attr <variable>)        ; positive condition with variable binding
    - (condition2 ^attr value)           ; negated condition
    -->
    (write |output text| <variable>)     ; write to console
    (modify 1 ^attr new-value)           ; modify matched element
    (remove 1)                           ; remove matched element
    (bind <var> (+ <x> <y>))             ; arithmetic binding
    (make typename ^attr value)          ; create new element
    (halt))                              ; stop execution
```
