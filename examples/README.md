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
| [AliasTest](AliasTest/) | Duplicate class conditions | Matching two WMEs of the same class in one rule (auto-aliasing) |
| [CBindTest](CBindTest/) | Time-tag capture | `cbind` to capture the time-tag of a newly created WME |
| [CallTest](CallTest/) | External program invocation | `call` to execute a system command |
| [ComparisonTest](ComparisonTest/) | All comparison operators | `=`, `<>`, `<`, `>`, `<=`, `>=` predicates |
| [ComputeTest](ComputeTest/) | Nested arithmetic | `compute` with nested prefix expressions |
| [ConjunctionTest](ConjunctionTest/) | AND predicates | `{ > 10 < 20 }` conjunction on a single attribute |
| [DefaultTest](DefaultTest/) | Default attribute values | `default` to set fallback values for omitted attributes |
| [DisjunctionTest](DisjunctionTest/) | OR matching | `<< Red Amber >>` disjunction syntax |
| [GenatomTest](GenatomTest/) | Unique symbol generation | `genatom` for generating unique identifiers |
| [StateMachine](StateMachine/) | State transition pattern | Negated conditions, sequential rule firing |
| [StrategyTest](StrategyTest/) | Conflict resolution | LEX and MEA strategies |
| [SubstrTest](SubstrTest/) | Substring operations | `substr` with literal positions, variables, and `INF` |
| [TabToTest](TabToTest/) | Column-aligned output | `tabto` for padded output formatting |
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
