# Test-Driven Development

## TDD Is Non-Negotiable
- All production code is written test-first. No exceptions (other than logging).
- Tests define the contract. Implementation satisfies the tests.
- Tests are not written after the fact — they define behavior before implementation begins.

## One Test, One Assertion
- Each test verifies exactly one thing.
- A failing test must tell you precisely what broke without investigation.
- Test names are sentences describing the expected behavior.

```csharp
[Fact] public void ReturnAllHazes() { ... }
[Fact] public void CreateFogInRepository() { ... }
[Fact] public void ReturnOk() { ... }
```

## Test Stack
- Framework: xUnit
- Faking: FakeItEasy (`A.Fake<T>()`, `A.CallTo()`)
- Assertions: FluentAssertions (`.Should()`, `.Be()`, `.BeEquivalentTo()`, etc.)
- Thread substitute: `FakeThread` (from `FatCat.Toolkit.Threading` — runs `IThread` operations synchronously in tests)
- Test data: `Faker.Create<T>()` (from `FatCat.Fakes`) for generating test objects — do not hard-code values

## Test Class Layout — Abstract Base + Specs Folder
Each class under test has a `<Class>Specs` folder containing:
- An `abstract class <Class>Tests` that holds all shared setup (fakes, the system under test, default fake configurations, helper `Verify*` methods)
- One concrete class per method-under-test (e.g. `GetAllTests`, `SendFileTransferTests`) that derives from the abstract base and contains the `[Fact]` methods for that scenario

Setup uses `protected readonly` fields populated inline with `A.Fake<T>()` or `Faker.Create<T>()`. The system under test is constructed in the protected constructor of the abstract base:

```csharp
namespace Tests.Fog.Brume.WebConnections.HazeManagerSpecs;

public abstract class HazeManagerTests
{
    protected readonly IFatCatCache<HazeConnectionCacheItem> cache = A.Fake<IFatCatCache<HazeConnectionCacheItem>>();
    protected readonly IGenerator generator = A.Fake<IGenerator>();
    protected readonly HazeManager hazeManager;
    protected readonly IFogHubManager hubManager = A.Fake<IFogHubManager>();
    protected readonly List<HazeConnectionCacheItem> items = Faker.Create<List<HazeConnectionCacheItem>>();
    protected readonly IJsonOperations jsonOperations = A.Fake<IJsonOperations>();
    protected readonly FakeThread thread = new();

    protected HazeManagerTests()
    {
        A.CallTo(() => cache.GetAll()).ReturnsLazily(() => items);

        hazeManager = new HazeManager(cache, generator, hubManager, jsonOperations, thread);
    }

    protected void VerifyCacheGetAll()
    {
        A.CallTo(() => cache.GetAll()).MustHaveHappened();
    }
}
```

There is no `BddBase` — the abstract class is plain.

## Global Usings
Each test project has a single `GlobalUsings.cs` file that declares `global using` directives for the test stack. Production projects also use a `GlobalUsings.cs` — see `types-and-di.md`.

```csharp
// GlobalUsings.cs — test project
global using FakeItEasy;
global using FatCat.Fakes;
global using FluentAssertions;
global using Serilog;
global using Xunit;
```

Add project-specific namespaces that appear in nearly every test file in the same project.

## Per-Service Test Base Class
Each test project has a single top-level base class named after the service (`BrumeTests`, `HazeTests`, etc.) that all test classes in the project inherit from indirectly via their feature-level abstract base (`HazeManagerTests : BrumeTests`). The service base:
- Registers any custom `Faker.AddGenerator(...)` calls in a static constructor
- Configures FluentAssertions equivalency tolerances (e.g. `DateTime` BeCloseTo with a 1-second tolerance)

```csharp
public class BrumeTests
{
    static BrumeTests()
    {
        Faker.AddGenerator(typeof(HazeConnectionCacheItem), new HazeConnectionCacheItemGenerator());

        AssertionOptions.AssertEquivalencyUsing(options =>
            options
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds()))
                .WhenTypeIs<DateTime>()
        );
    }
}
```

There is no `BddBase`. The abstract per-class base (e.g. `HazeManagerTests`) holds the system under test and its fakes — see the example earlier in this file.

## MongoFakeRepository — Use the Concrete Fake
For tests against code that depends on `IMongoRepository<T>`, use the concrete `MongoFakeRepository<T>` (from FatCat) directly as a `protected readonly` field. Do NOT fake the interface with `A.Fake<IMongoRepository<T>>()` — the concrete fake provides `Item`, `Items`, `VerifyCreate(expected)`, `VerifyUpdate(expected)`, and matching state semantics that hand-rolled fakes lose.

```csharp
protected readonly MongoFakeRepository<UserData> mongo = new();

// in a test
mongo.VerifyCreate(expectedUser);
mongo.Item.Should().BeEquivalentTo(expectedUser);
```

## Endpoint Test Assertions
Endpoint tests verify both the HTTP shape and the result body:
- Shape: `endpoint.Should().BePost(nameof(CreateUserEndpoint.CreateUser), "api/User")` — `BePost`, `BeGet`, `BePut`, `BeDelete` come from `FatCat.Toolkit.WebServer.Testing` and assert both the HTTP verb attribute and the route template.
- Result: `result.Should().BeOk().Be(expectedServiceModel)` — `BeOk()` narrows a `WebResult` to a 200, then `.Be(...)` asserts the body. Other helpers include `.BeBadRequest(Errors.X)`, `.BeNotFound()`.

## Test Method Naming — Verb-First
`[Fact]` methods are named as bare verb phrases describing the observable behaviour, with no `Should`, no underscores, no Given/When/Then:

```csharp
[Fact] public void BeAPost() { ... }
[Fact] public void ReturnUserServiceModel() { ... }
[Fact] public void CreateTheUserDataInMongo() { ... }
[Fact] public void GetUtcNow() { ... }
```

## Expression-Bodied Members in Tests — BANNED
The expression-bodied member ban applies to test code too. All test methods and constructors must use block bodies:

```csharp
// Wrong
[Fact]
public void ReturnOk() => result.Should().BeOk();

public MyTests() => sut = new MySut(fake);

// Correct
[Fact]
public void ReturnOk()
{
    result.Should().BeOk();
}

public MyTests()
{
    sut = new MySut(fake);
}
```

## Test Setup
- Place common setup in the test class constructor: create fakes, configure default return values, initialize the system under test.
- Keep constructor setup minimal and deterministic. Extract to helper methods if setup becomes large.

## FakeItEasy Patterns
- Use `A<T>._` for argument matchers. Never use `A<T>.Ignored` — they are equivalent, and `A<T>._` is the canonical form in this codebase.
- Use `Returns(...)` for static, unchanging responses.
- Use `ReturnsLazily(...)` when the return value needs to vary between tests:

```csharp
// In constructor:
private SomeType currentResult;
A.CallTo(() => repo.Get(...)).ReturnsLazily(() => currentResult);

// In each test — just set the field:
currentResult = new SomeType { ... };
```

- This avoids reconfiguring fakes per test and keeps each test focused on its scenario.
- Document any non-trivial fake behavior so future maintainers understand the intent.

## Test Project Conventions
- The abstract base class name = source class name + `Tests`, placed in a `<Class>Specs` folder. Concrete derived classes are named after the method under test (e.g. `GetAllTests`, `CreateFogTests`).
- Test namespace mirrors source namespace with `Tests.` prepended.
- Example: `Fog.Brume.WebConnections.HazeManager` →
  `Tests.Fog.Brume.WebConnections.HazeManagerSpecs.HazeManagerTests` (abstract base) plus `Tests.Fog.Brume.WebConnections.HazeManagerSpecs.GetAllTests` (concrete `[Fact]` class).
- The DocLokr subtree follows the same rule with the `Tests.DocLokr.*` prefix.
- There is always a direct 1-to-1 correspondence between a class under test and its `<Class>Specs` folder.

## Testing and IThread
- In tests, inject `FakeThread` instead of a real `IThread` implementation.
- This runs async/threaded operations synchronously, giving deterministic test results.
- You do not need to test that an action runs in a new thread — test the action itself.
- For testing sleep/delay behavior, use `IThread` and `FakeThread` directly.

## Low-Level API Implementations — No Unit Tests Required
- Classes that talk directly to a low-level external system do not require unit tests.
- Examples: Win32 P/Invoke wrappers, direct MongoDB driver calls, raw OS or hardware APIs.
- These classes exist to satisfy an interface boundary — the interface is tested via fakes everywhere it is consumed.
- Mark the class with `[ExcludeFromCodeCoverage]` and a `Justification` that explains why.

```csharp
[ExcludeFromCodeCoverage(Justification = "Direct wrapper over the MongoDB driver — no business logic, tested via IMongoRepository fakes in consuming classes.")]
public class MongoRepository(IMongoClient client) : IMongoRepository
{
    // ...
}
```

- The justification must be specific: name the low-level API being wrapped and confirm there is no testable business logic in the class.
- Do not apply this exemption to classes that contain any branching logic or orchestration — extract that logic into a separately tested class first.
