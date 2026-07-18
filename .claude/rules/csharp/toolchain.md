# Toolchain

## CSharpier — Final Formatting Authority
CSharpier owns **all** C# layout — braces, spacing, new lines, wrapping, single-line blocks. It is the single source of truth for formatting, and it is fully opinionated: it has no per-rule formatting switches.
- Configuration: `.csharpierrc` at the repo root (CSharpier's primary config name, searched before `.editorconfig`) — `printWidth: 128`, `useTabs: true`, `indentSize: 4`.
- CSharpier reads a small whitespace set from `.editorconfig` and keeps it in sync with `.csharpierrc`: `indent_style` (→ useTabs), `indent_size` (→ indentSize), `max_line_length` (→ printWidth), `end_of_line`, `insert_final_newline`, `charset`, `trim_trailing_whitespace`, `dotnet_sort_system_directives_first`, `dotnet_separate_import_directive_groups`. It **ignores** every `csharp_*` formatting key.
- Runs automatically on build and on save.
- **Never fight CSharpier.** If it reformats something, that is correct. Do not manually reformat to avoid it.
- Write readable code — CSharpier handles the rest. Do not pre-format to match what you think CSharpier will do.

## dotnet format — Style & Analyzer Enforcement (NOT formatting)
`dotnet format` applies code-**style** and **analyzer** fixes only. Formatting/whitespace is CSharpier's job — never run the whitespace formatter, or it will fight CSharpier. Style/analyzer rules are driven by `.editorconfig`. It enforces:
- Remove redundant code and unnecessary qualifiers
- `var` everywhere (enforced)
- Fields made `readonly` where possible (enforced)
- **Block bodies only** — expression-bodied members (`=>`) are banned
- String interpolation enforced over concatenation

Run it before committing — style and analyzers only, never `whitespace`:
```bash
dotnet format style Fog.sln                  # apply code-style fixes from .editorconfig
dotnet format analyzers Fog.sln              # apply analyzer fixes
dotnet format style Fog.sln --verify-no-changes   # CI / pre-commit gate
```

If `dotnet format` changes something, that change is correct — do not revert it. Do not suppress an analyzer rule without a comment explaining why. Suppression format when genuinely necessary:
```csharp
#pragma warning disable <RuleId> // <reason>
```

## .editorconfig — Style Rules + CSharpier Whitespace Inputs
- `.editorconfig` at the repo root holds two things only: (1) the whitespace keys CSharpier reads (Core EditorConfig Options), and (2) the code-style and naming rules `dotnet format` applies.
- It declares **no** `csharp_*` formatting keys — that would be a second, conflicting formatting spec. CSharpier owns layout.
- Naming conventions are enforced as warnings.
- Namespace must match folder structure — enforced.
- File-scoped namespaces, `var` preference, and the expression-bodied-method ban are all enforced here.
- All files should be green (no unresolved warnings) unless suppressed with reason.

## Expression-Bodied Members — BANNED
This applies to ALL members regardless of access modifier: public, private, protected, internal.
**This ban also applies to test projects** — test methods and constructors must use block bodies too.
Do not write:
```csharp
public string Name => name;                                     // banned
public void Reset() => Execute();                               // banned
private FogData CurrentFog => Get();                            // banned
private HazeServiceModel HazeModel => GetHaze();                // banned
public void WillReturnOk() => result.Should().BeOk();           // banned — even in tests
public MyTests() => sut = new MyClass();                        // banned — even in test constructors
```
Always use block bodies:
```csharp
public string Name { get { return name; } }                     // correct
public void Reset() { Execute(); }                              // correct
private FogData CurrentFog { get { return Get(); } }            // correct
private HazeServiceModel HazeModel { get { return GetHaze(); } }// correct
public void WillReturnOk() { result.Should().BeOk(); }          // correct — test method
public MyTests() { sut = new MyClass(); }                       // correct — test constructor
```
