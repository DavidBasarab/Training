# Error Handling & Logging

## Error Handling
- Exceptions are for unplanned, unexpected failures only (hardware failures, network timeouts, corrupted state).
- Never throw an exception for a predictable outcome (validation failure, value out of range, known bad state).
- For known failure modes, return a value — an enum is preferred.
- Let exceptions bubble to the boundary where they can be meaningfully handled.
- Do not catch and swallow exceptions silently. The one exception: if a failure is genuinely non-actionable (e.g. a socket error on disconnect, a reflection comparison on an incompatible type), an empty catch with a `// ignored` comment is acceptable. This must be rare and deliberate — never use it to hide logic errors.
- "Log and rethrow at the boundary" is allowed at endpoint or top-level entry points (e.g. `LoginEndpoint` calling `QuickLog.Exception(e)` and re-throwing). Do not log-and-rethrow at every layer — pick one boundary.

```csharp
// Preferred for known failures:
public enum LoginResult { Success, UnknownUser, InvalidPassword }

public LoginResult TryLogin(LoginRequest request)
{
    if (!userExists)        return LoginResult.UnknownUser;
    if (!passwordMatches)   return LoginResult.InvalidPassword;
    SignIn();
    return LoginResult.Success;
}
```

## Logging — Serilog
- We use Serilog. Inject `Serilog.ILogger` via the constructor for permanent logging. The codebase aliases this as `using ILogger = Serilog.ILogger;` to disambiguate from `Microsoft.Extensions.Logging.ILogger` — never inject the Microsoft `ILogger`.
- Log at the action site, not at the boundary.
- Log thoughtfully — do not add log entries without a clear reason.
- Active log levels: `Debug`, `Information`, `Warning`, `Error`.

### QuickLog and DebugLogger
The codebase has two static helpers in addition to Serilog:
- `QuickLog` (`Fog.Common.Infrastructure.Logging.QuickLog`) — colour-coded console writes for startup banners, configuration warnings, and exceptional events at module-load time. Acceptable in permanent code where a one-off boot-time announcement is genuinely useful (see `BrumeModule` for examples).
- `DebugLogger.Write(...)` — verbose tracing of message flow (e.g. hub send/receive). Acceptable in permanent code where the trace is genuinely useful for diagnosing live systems. Do not use it as a substitute for proper Serilog logging in normal business logic — prefer injected `ILogger` when you have an instance.

Neither should be used as a scratch debugger. If you add a temporary trace while diagnosing, remove it before merging.

## Logging and TDD
- Logging is the one area where strict TDD is not enforced.
- Do not block on log string test coverage — test critical entries, use judgment for the rest.
