# What NOT to Do

These are hard stops. Do not do any of the following under any circumstances.

## Type System
- Do NOT use nullable annotations (`?`) defensively — only use `?` where a value is genuinely nullable (generic return types, reflection code, extension methods designed to accept null). Never annotate injected dependencies or always-populated values.
- Do NOT use records — use classes only

## Async
- Do NOT use `async void` — always return `Task` or `Task<T>`
- Do NOT use `ConfigureAwait(false)` — we do not use it
- Do NOT block on tasks with `.Result` or `.Wait()`
- Do NOT use `Task` or `Thread` directly for threading — use `IThread`. No `Task.Delay`, no `Thread.Sleep`, no `new Thread(...)`.
- Do NOT call `DateTime.UtcNow` or `DateTime.Now` in production code — inject `IDateTimeUtilities` and call `dateTimeUtilities.UtcNow()`.

## Code Style
- Do NOT use expression-bodied members (`=>` syntax for methods or properties) — this applies to ALL access levels (public, private, protected, internal) and ALL projects including test projects
- Do NOT use query syntax LINQ (`from x in y where...`) — method chaining only
- Do NOT use string concatenation with `+` — use string interpolation. Write `$"Some string with data {theData}"`, never `"Some string with data " + theData`. (No analyzer enforces this; it is caught by code review.)
- Do NOT abbreviate names — write them out fully
- Do NOT write comments explaining what code does — rename until obvious
- Do NOT use `new List<T>()`, `new T[0]`, or `new Dictionary<K, V>()` for empty or inline-populated collections — use collection expressions (`[]`)
- Do NOT construct durations with `TimeSpan.FromMilliseconds(...)`, `TimeSpan.FromSeconds(...)`, `TimeSpan.FromMinutes(...)`, etc. from a constant — use Humanizer fluent extensions (`500.Milliseconds()`, `3.Seconds()`, `10.Minutes()`)

## Architecture
- Do NOT use property injection or setter injection — constructor only
- Do NOT use `new` inside a class to instantiate a dependency
- Do NOT name a file after an interface — always name after the class
- Do NOT add abstractions or patterns that do not exist in the surrounding codebase
- Do NOT introduce over-engineering — match the abstraction level of the existing code
- Do NOT use `ISystemScope` as a service locator to avoid constructor injection — only use it for genuine runtime resolution (worker dispatch, message-processor dispatch, configuration-driven factories)
- Do NOT inline kebab-case error strings at the call site — add them as `const string` on `Fog.Common.Errors` and reference by name
- Do NOT use AutoMapper, `IConfigureMappings`, or `CreateMap` — use `IProjector` and register custom projectors via `ProjectionConfiguration.UseCustomProjection<,>`

## Errors & Logging
- Do NOT throw exceptions for predictable, known failure states — return an enum
- Do NOT swallow exceptions silently
- Do NOT inject `Microsoft.Extensions.Logging.ILogger` — always inject `Serilog.ILogger` (aliased as `using ILogger = Serilog.ILogger;`)
- Do NOT use `QuickLog` or `DebugLogger.Write` as a scratch debugger — if you add a temporary trace, remove it before merging. They are acceptable in permanent code only for genuine boot-time announcements (QuickLog) or persistent live-flow tracing (DebugLogger).

## Testing
- Do NOT use `A<T>.Ignored` in FakeItEasy argument matchers — always use `A<T>._`

## Formatting
- Do NOT manually fight CSharpier formatting — it is the final authority
- Do NOT suppress `dotnet format` / analyzer warnings without a comment explaining why