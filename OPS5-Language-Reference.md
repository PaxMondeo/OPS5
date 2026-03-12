# OPS5 Language Reference

## 1. Introduction

OPS5 (Official Production System, version 5) is one of the most influential production rule languages, originally developed at Carnegie Mellon University. This engine provides a complete implementation of OPS5, parsing the classic S-expression syntax directly and compiling it into a RETE network for efficient rule matching and execution.

An OPS5 program is contained in a single `.ops5` file that combines class definitions, initial data, and production rules. The engine parses this file, builds a RETE network, loads the initial working memory, and then runs the recognize-act cycle until no rules can fire or a `halt` action is executed.

To run an OPS5 program:

```
dotnet run --project OPS5.Host -- myprogram.ops5
```

Or run `OPS5.Host` without arguments to enter an interactive console (REPL).

---

## 2. Syntax Fundamentals

### 2.1 S-Expression Structure

OPS5 uses an S-expression (parenthesised) syntax. All constructs are enclosed in parentheses. There are no semicolon statement terminators — the structure is defined entirely by matching parentheses.

```ops5
(literalize block name color)
(make block ^name B1 ^color red)
(p my-rule
    (block ^name <b>)
    -->
    (write |Found: | <b>))
```

### 2.2 Comments

A semicolon (`;`) starts a line comment that extends to the end of the line.

```ops5
; This is a full-line comment
(literalize block name color)  ; This is an inline comment
```

### 2.3 Identifiers

Identifiers may contain letters, digits, underscores (`_`), hyphens (`-`), and dots (`.`). Hyphenated identifiers are idiomatic in OPS5:

```ops5
(literalize block name color on-top-of)
(p find-red-block ...)
```

### 2.4 Literal Types

| Type | Syntax | Examples |
|------|--------|---------|
| Integer | Digits, optional leading `-` | `42`, `-5`, `0` |
| Decimal | Digits with `.`, optional leading `-` | `3.14`, `-2.5` |
| String | Pipe-delimited | `|hello world|`, `|Block A|` |
| Identifier | Letters/digits/hyphens/underscores/dots | `block`, `on-top-of`, `B1` |
| Variable | Angle-bracket delimited | `<x>`, `<my-var>`, `<block-name>` |

Pipe-delimited strings (`|text|`) are the OPS5 way to write string literals containing spaces or special characters.

### 2.5 Variables

Variables are denoted by angle brackets: `<name>`. They serve as pattern-matching bindings within rules.

**In conditions (LHS)**, a variable captures the value of an attribute when an object matches:

```ops5
(block ^name <b> ^color <c>)
```

Here `<b>` and `<c>` are bound to the matching object's attribute values.

**In actions (RHS)**, variables are substituted with their bound values:

```ops5
(write |Block | <b> | is | <c>)
```

When the same variable appears in multiple conditions, it creates a **join** — requiring the values to match across different objects.

---

## 3. Top-Level Forms

An OPS5 file contains the following top-level forms, which can appear in any order:

| Form | Purpose |
|------|---------|
| `(literalize ...)` | Define a class and its attributes |
| `(make ...)` | Create an initial object in Working Memory |
| `(p ...)` | Define a production rule |
| `(default ...)` | Set default attribute values for a class |
| `(vector-attribute ...)` | Declare multi-valued (vector) attributes |

---

## 4. Class Definitions (literalize)

The `literalize` form defines a class (object type) and its attributes:

```ops5
(literalize class-name attr1 attr2 attr3)
```

Example from the BlocksWorld program:

```ops5
(literalize block name color on-top-of)
(literalize goal status object)
```

Multiple `literalize` forms define multiple classes. Attribute names are listed without types — types are inferred from values at runtime.

---

## 5. Default Declarations

The `default` form sets default attribute values for a class. When a `make` action creates an object without specifying every attribute, unspecified attributes receive the default values declared here:

```ops5
(default class-name ^attr1 val1 ^attr2 val2)
```

For example:

```ops5
(literalize block name color on-top-of)
(default block ^color unknown ^on-top-of table)
```

Now creating a block with only a name:

```ops5
(make block ^name B1)
```

will produce a block where `color` is `unknown` and `on-top-of` is `table`.

---

## 6. Vector Attributes

The `vector-attribute` form declares that certain attributes of a class can hold multiple values (arrays):

```ops5
(vector-attribute class-name attr1 attr2)
```

For example:

```ops5
(literalize container name items)
(vector-attribute container items)
```

This declares that the `items` attribute of a `container` can hold more than one value.

---

## 7. Initial Data (make)

The `make` form creates an object in Working Memory with initial attribute values:

```ops5
(make class-name ^attr1 value1 ^attr2 value2)
```

The caret (`^`) prefix marks each attribute name.

Examples with different value types:

```ops5
(make block ^name B1 ^color red ^on-top-of B2)       ; identifier values
(make sensor ^id S1 ^reading 42)                       ; integer value
(make measurement ^id M1 ^value 3.14)                  ; decimal value
(make message ^text |Hello World|)                      ; pipe-delimited string
```

Top-level `make` forms define the initial state of Working Memory before any rules fire. The `make` action can also appear on the RHS of rules to create objects during execution (see Section 8.6).

---

## 8. Production Rules (p)

### 8.1 Rule Structure

A production rule is defined with the `p` form:

```ops5
(p rule-name
    condition1
    condition2
    ...
    -->
    action1
    action2
    ...)
```

The `-->` arrow separates the **Left-Hand Side (LHS)** conditions from the **Right-Hand Side (RHS)** actions. When all conditions are satisfied simultaneously, the rule is eligible to fire and its actions execute.

### 8.2 LHS Conditions

Each condition is a parenthesised pattern that matches against objects of a given class:

```ops5
(class-name ^attr1 test1 ^attr2 test2)
```

The caret (`^`) prefix marks each attribute being tested. After the attribute name, the test can take several forms:

| Pattern | Meaning |
|---------|---------|
| `^attr value` | Exact match |
| `^attr <var>` | Bind to variable |
| `^attr = value` | Explicit equals |
| `^attr <> value` | Not equal |
| `^attr > value` | Greater than |
| `^attr < value` | Less than |
| `^attr >= value` | Greater or equal |
| `^attr <= value` | Less or equal |

Example with multiple tests:

```ops5
(p find-heavy-red-block
    (goal ^status searching)
    (block ^name <b> ^color red ^mass > 75)
    -->
    (write |Found heavy red block: | <b>))
```

### 8.3 Negated Conditions

Prefix a condition with `-` to test that **no** matching object exists:

```ops5
-(block ^on-top-of <b>)
```

Negated conditions are useful for detecting the absence of facts. For example, to fire only when there is no block on top of another block:

```ops5
(p find-clear-block
    (block ^name <b>)
    -(block ^on-top-of <b>)
    -->
    (write |Block | <b> | is clear|))
```

### 8.4 Conjunctions

Conjunctions allow multiple predicates on the same attribute using curly braces:

```ops5
(block ^mass { > 100 < 500 })
```

This means "mass is greater than 100 AND less than 500".

### 8.5 Disjunctions

Disjunctions allow matching any one of several values using double angle brackets:

```ops5
(block ^color << red blue green >>)
```

This means "color is red OR blue OR green".

### 8.6 RHS Actions

The following actions are supported on the Right-Hand Side of a rule:

| Action | OPS5 Syntax | Purpose |
|--------|-------------|---------|
| `make` | `(make class ^attr val)` | Create a new object |
| `modify` | `(modify N ^attr val)` | Update matched object |
| `remove` | `(remove N)` | Delete matched object |
| `write` | `(write \|text\| <var>)` | Output text |
| `halt` | `(halt)` | Stop the engine |
| `compute` | `(compute <r> (op a b))` | Arithmetic |
| `bind` | `(bind <v> value)` | Variable assignment |
| `cbind` | `(cbind <v>)` | Bind time-tag of last created WME |
| `call` | `(call prog arg1 arg2 ...)` | Execute external program |
| `accept` | `(bind <v> (accept))` | Read single token from console |
| `acceptline` | `(bind <v> (acceptline))` | Read full line from console |
| `openfile` | `(openfile name \|path\| in\|out)` | Open a file for reading or writing |
| `closefile` | `(closefile name)` | Close a named file handle |
| `write` (to file) | `(write name \|text\| <v>)` | Write to a named file |
| `accept` (from file) | `(bind <v> (accept name))` | Read token from a named file |
| `tabto` | `(write \|text\| (tabto 20) <v>)` | Pad output with spaces to reach column N |
| `substr` | `(bind <r> (substr <s> 3 5))` | Extract substring (1-based position, length or INF) |
| `genatom` | `(bind <v> (genatom))` | Generate a unique atom identifier |

**make** — Creates a new object in Working Memory:

```ops5
(make result ^block <b> ^status found)
```

**modify** — Updates attributes of a matched object. The number refers to the condition position (1-based) on the LHS:

```ops5
(modify 1 ^status found ^object <b>)
```

Here `1` refers to the first condition's matched object, `2` would be the second, and so on.

**remove** — Deletes a matched object from Working Memory. Uses the same condition reference:

```ops5
(remove 1)
```

**write** — Outputs text. Pipe-delimited strings, variables, and identifiers can be intermixed:

```ops5
(write |Found block: | <b> | with color | <c> (crlf))
```

The `(crlf)` form outputs a newline. It can also be used standalone:

```ops5
(write (crlf))
```

**halt** — Stops the rule engine:

```ops5
(halt)
```

**cbind** — Binds a variable to the time-tag (unique identifier) of the most recently created working memory element. This is typically used immediately after a `make` action:

```ops5
(make result ^status active)
(cbind <tag>)
```

The variable `<tag>` now holds the time-tag of the newly created `result` object.

**call** — Executes an external program with arguments. The first argument is the program name, and subsequent arguments are passed to it:

```ops5
(call myprogram arg1 <var>)
```

The engine captures stdout and stderr from the called process and reports any non-zero exit codes.

**genatom** — Generates a unique atom identifier. Used inside `bind`:

```ops5
(bind <id> (genatom))
```

This is useful for creating unique identifiers when making new objects dynamically.

**accept** — Reads a single whitespace-delimited token from console input. Used inside `bind`:

```ops5
(bind <answer> (accept))
```

**acceptline** — Reads a full line from console input. Used inside `bind`:

```ops5
(bind <line> (acceptline))
```

### 8.7 Compute (Arithmetic)

The `compute` action evaluates arithmetic expressions. OPS5 uses **prefix notation** (operator before operands):

```ops5
(compute <result> (+ <a> <b>))
```

Supported arithmetic operators:

| Operator | Meaning |
|----------|---------|
| `+` | Addition |
| `-` | Subtraction |
| `*` | Multiplication |
| `/` | Division |
| `//` | Integer division |
| `\` | Integer division (alternative) |

Nested expressions are supported:

```ops5
(compute <volume> (* <length> (* <width> <height>)))
```

### 8.8 Bind (Variable Assignment)

The `bind` action assigns a value to a variable. It can assign a simple value:

```ops5
(bind <status> complete)
```

Or assign the result of an arithmetic expression:

```ops5
(bind <total> (+ <subtotal> <tax>))
```

Or assign the result of a function:

```ops5
(bind <id> (genatom))
(bind <part> (substr <fullname> 1 3))
```

### 8.9 Condition References

The `modify` and `remove` actions refer to matched objects by their **condition position** (1-based index). The first condition on the LHS is `1`, the second is `2`, and so on:

```ops5
(p example
    (goal ^status active)         ; condition 1
    (block ^name <b> ^color red)  ; condition 2
    -->
    (modify 1 ^status done)       ; modifies the goal object
    (remove 2))                   ; removes the block object
```

### 8.10 File I/O (openfile / closefile)

OPS5 supports opening files for reading or writing, writing to files, and reading from files.

**Opening a file:**

```ops5
(openfile myfile |results.txt| out)    ; open for writing
(openfile datafile |input.dat| in)     ; open for reading
```

**Closing a file:**

```ops5
(closefile myfile)
```

**Writing to a file:** When the first argument to `write` is a logical file name (not a string literal or variable), output goes to that file:

```ops5
(write myfile |Result: | <value> (crlf))
```

**Reading from a file:** When `accept` is called with a logical file name inside `bind`, input is read from the file:

```ops5
(bind <line> (accept myfile))
```

---

## 9. Conflict Resolution

When multiple rules are eligible to fire on the same cycle, the engine uses a **conflict resolution strategy** to select which rule fires. Two strategies are supported:

### 9.1 MEA (Means-Ends Analysis)

MEA is the default strategy. It selects the winning rule instantiation by:

1. **First-condition recency** — The instantiation whose first condition matched the most recently modified WME is preferred. This makes the first condition act as a "focus" or "goal" element.
2. **Specificity** — Among ties, the rule with more conditions (more specific) is preferred.
3. **Recency of remaining conditions** — Further ties are broken by the recency of WMEs matched by the remaining conditions.

MEA fires **one** rule per cycle.

### 9.2 LEX (Lexicographic)

LEX selects the winning rule instantiation by:

1. **Overall recency** — Compare the recency of WMEs matched by each instantiation lexicographically (most recent first across all conditions).
2. **Specificity** — Among ties, the rule with more conditions is preferred.

LEX fires **one** rule per cycle.

### 9.3 Setting the Strategy

The conflict resolution strategy can be configured in the engine settings. MEA is used by default.

---

## 10. Limitations

The following features have known limitations:

| Feature | Limitation |
|---------|------------|
| `call` arguments | Arguments containing `/` may not be passed correctly due to lexer tokenisation |
| Advanced math functions | Functions like `SQRT`, `ABS`, `LOG`, `SIN`, etc. are available in the internal RPN calculator but are not exposed through the OPS5 `(compute)` prefix syntax — only the basic arithmetic operators (`+`, `-`, `*`, `/`, `//`) are accessible via `compute` |

---

## 11. Complete Example: BlocksWorld

### 11.1 OPS5 Source

```ops5
; BlocksWorld - Classic OPS5 blocks world example

; === Class definitions ===
(literalize block name color on-top-of)
(literalize goal status object)

; === Initial data ===
(make block ^name B1 ^color red ^on-top-of B2)
(make block ^name B2 ^color blue ^on-top-of table)
(make block ^name B3 ^color green ^on-top-of table)
(make goal ^status find-red ^object none)

; === Production rules ===

; Find a red block and report it
(p find-red-block
    (goal ^status find-red)
    (block ^name <b> ^color red)
    -->
    (write |Found red block: | <b>)
    (modify 1 ^status found ^object <b>))

; After finding, halt
(p done-finding
    (goal ^status found ^object <b>)
    -->
    (write |Done. Red block is: | <b>)
    (halt))
```

### 11.2 Running the Example

Run the example using `OPS5.Host`:

```
dotnet run --project OPS5.Host -- examples/BlocksWorld/BlocksWorld.ops5
```

---

## 12. Quick Reference

| OPS5 Syntax | Purpose |
|-------------|---------|
| `(literalize cls a b)` | Define class with attributes |
| `(default cls ^a v1 ^b v2)` | Set default attribute values |
| `(vector-attribute cls a b)` | Declare vector attributes |
| `(make cls ^a v1 ^b v2)` | Create working memory element |
| `(p name ... --> ...)` | Define production rule |
| `(cls ^a v)` | Condition: exact match |
| `(cls ^a <x>)` | Condition: bind to variable |
| `-(cls ^a v)` | Negated condition |
| `{ > 100 < 500 }` | Conjunction (AND predicates) |
| `<< v1 v2 v3 >>` | Disjunction (OR values) |
| `(make cls ^a v)` (RHS) | Create new object |
| `(modify N ^a v)` | Update matched object N |
| `(remove N)` | Delete matched object N |
| `(write \|text\| <var>)` | Output to console |
| `(write name \|text\| <v>)` | Write to named file |
| `(halt)` | Stop the engine |
| `(compute <r> (+ a b))` | Arithmetic (prefix notation) |
| `(bind <v> val)` | Assign value to variable |
| `(bind <v> (genatom))` | Generate unique atom |
| `(bind <v> (accept))` | Read token from console |
| `(bind <v> (acceptline))` | Read line from console |
| `(bind <v> (accept name))` | Read token from file |
| `(bind <r> (substr <s> 3 5))` | Extract substring |
| `(cbind <v>)` | Bind time-tag of last created WME |
| `(call prog arg1 arg2)` | Execute external program |
| `(openfile name \|path\| in\|out)` | Open file for reading/writing |
| `(closefile name)` | Close file handle |
| `(write \|text\| (tabto 20) <v>)` | Tab to column in output |
| `(crlf)` | Newline in write output |
| `; comment` | Line comment |
| `\|pipe string\|` | String literal with spaces |
| `<variable>` | Variable reference |
