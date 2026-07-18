# Testing

## React App — Cypress Component Tests (Sites/main)

### Framework
- All new tests are **component tests** using Cypress.
- Component test files are named `<Component>.cspecs.tsx`.
- Do NOT write new e2e (end-to-end) tests. E2e tests are legacy: they are slow, depend on live infrastructure, and rely on stale fixture data. If you encounter an e2e test while working in a section of code, convert it to a component test where possible.

### Component Under Test Pattern
Wrap the component under test in a typed `CUT` component that wires up all required providers:

```tsx
type CUTProps = {
	store: MockStore;
	userPermissions: PermissionType[];
};

const CUT = ({store, userPermissions}: CUTProps) => (
	<ComponentWrapper userPermissions={userPermissions} store={store}>
		<MyComponent />
	</ComponentWrapper>
);
```

This isolates provider boilerplate from the test logic itself.

### Test Structure
- Use `describe` blocks to group related tests. Nest to organize by scenario: `describe("on initial load should", ...)`.
- Use `beforeEach` to mount the component and alias the default props.
- Reference aliased props via `function()` callbacks and `this` — not arrow functions — when you need the alias:

```typescript
beforeEach(() => {
	const defaultProps: CUTProps = { ... };
	cy.wrap(defaultProps).as("defaultProps");
	cy.mount(<CUT {...defaultProps} />);
});

it("show the menu", function () {
	cy.getByDataCy("myMenu").should("exist");
});
```

### Selectors
- Always select elements by `data-cy` attribute using the custom `cy.getByDataCy()` command.
- Never select by class name, tag name, or text content.

### Assertions
- Prefer Cypress chainable assertions: `.should("exist")`, `.should("contain", "text")`, `.should("have.length", 3)`.
- Use `cy.then()` for sequenced assertions that depend on prior commands.

### Network Stubbing — cy.intercept()

Do not mock endpoint hooks. Intercept HTTP requests at the network level with `cy.intercept()`. The endpoint hooks make real fetch calls — Cypress intercepts them before they reach the network.

**Stubbing a GET response:**
```tsx
cy.intercept("GET", "**/Manager/Room", {body: [getFakeRoom(), getFakeRoom()]});
cy.mount(<CUT {...defaultProps} />);
```

Use `getFake*` functions to generate response bodies — not fixture files.

**Stubbing a mutation and validating the request body:**
```tsx
cy.intercept("POST", "**/Manager/Room", {statusCode: 204}).as("saveRoom");

cy.getByDataCy("buttonRowSave").click();

cy.wait("@saveRoom").its("request.body").should("deep.equal", {Name: "Boardroom A"});
```

Use `.as("alias")` to name the intercept, `cy.wait("@alias")` to synchronize, then `.its("request.body")` to assert what was sent.

**Testing an error path:**
```tsx
cy.intercept("POST", "**/Manager/Room", {forceNetworkError: true}).as("saveError");

cy.getByDataCy("buttonRowSave").click();

cy.getByDataCy("networkAlert").should("exist");
```

**URL patterns:** Use `**` to match any path prefix. Include enough of the path to be specific: `"**/Manager/Room/${room.Id}"` not just `"**/${room.Id}"`.

**Callback props:** Use `cy.stub()` for React function props that the component calls directly — these are separate from HTTP calls:
```tsx
const defaultProps: CUTProps = {
	onClose: cy.stub().as("onClose"),
};

cy.get("@onClose").should("have.been.called");
```

### Redux in Tests
- Use `redux-mock-store` with `configureMockStore()` to create a typed mock store.
- Pass the store through the `CUT` wrapper.

### Error Boundary Testing
- To test that a hook throws when used outside a provider, wrap the component in an `ErrorBoundary` and capture the thrown error:

```tsx
let caughtError: Error | null = null;
cy.mount(
	<ErrorBoundary onError={(e: Error) => { caughtError = e; }}>
		<ComponentThatThrows />
	</ErrorBoundary>
);
cy.then(() => {
	expect(caughtError?.message).to.include("must be used within provider");
});
```

### Test Data — getFake* Functions, Not Fixtures

**Do not use JSON fixtures.** Fixtures are static files that go stale as the backend evolves. They are legacy.

**Use `getFake*` functions** defined in the `UiDefined/` directory. These functions are the canonical source of test data:
- They are named after the type they produce: `getFakeRoom()`, `getFakeActivity()`, `getFakeUser()`.
- They are defined in `.ts` files inside `UiDefined/` alongside the UI-extended interface that wraps the generated backend TypeScript.
- The generated TypeScript interfaces (produced on build from C# POCOs) keep the UI models in sync with the backend automatically. The `UiDefined/` interfaces extend these generated types for UI-specific needs.
- `getFake*` functions fill all fields with realistic random values that represent a valid system state.

**`getFake*` functions are shared tools — improve them freely.** If a function you need does not exist, add it. If an existing function produces poor test data or is missing fields, make it better. Everyone on the team benefits when these functions improve. Treat them as first-class code, not throwaway helpers. When writing a new `getFake*` function, fill every field with a value that represents a plausible, valid system state — not empty strings or zero values unless those are genuinely valid.

**Pinning test data with spread:** Use object spread to override only the fields your test depends on, leaving all other fields as random values from `getFake*`:

```tsx
// Correct — spread getFake* and pin only the fields the test cares about
const room = {...getFakeRoom(), isLocked: true, slug: "boardroom-a"};

// Wrong — hard-code every field like a fixture
const room: Room = { id: "123", name: "Room A", isLocked: false, slug: "roomA", ... };
```

**Critical:** Always pin every field that your assertion depends on. Random data for unrelated fields is fine and intentional — but if your test logic reads a specific field, pin it explicitly. Failing to do this causes flaky tests that fail intermittently as random values occasionally produce unexpected results.

---

## Node.js CLI — Jest (Installer/DataMigration)

### Framework
- Tests use Jest with ts-jest.
- Test files are named `<Class>.specs.ts`.
- Test files live alongside the source file they test.

### Test Structure
- One `describe` block per class, named after the class.
- Nest `describe` blocks by behavioral area: `describe("console override behavior", ...)`.
- Test names are complete sentences that describe the expected behavior: `"should call fileSystem.appendFileSync when console.log is used"`.

### Mocking with jest.Mocked
- Use `jest.Mocked<IInterface>` for all dependency mocks.
- Create mocks with factory functions to keep them readable and reusable:

```typescript
const createMockFileSystem = (): jest.Mocked<IFileSystem> =>
	({
		existsSync: jest.fn().mockReturnValue(false),
		mkdirSync: jest.fn(),
		appendFileSync: jest.fn(),
	} as unknown as jest.Mocked<IFileSystem>);
```

### tsyringe Container
- In `beforeEach`: call `container.clearInstances()` and `container.reset()`, then register mocks and resolve the class under test.
- In `afterEach`: restore any global state (e.g., if you replaced `console.log`).
- Never share container state between tests.

### Assertions
- Use Jest's `expect` with precise matchers: `toHaveBeenCalledWith`, `toHaveBeenCalledTimes`, `stringContaining`, `objectContaining`.
- One primary assertion per test. Secondary assertions are allowed only to verify related invariants.

### What Not to Test
- Do not test logging output unless it is the primary behavior under test.
- Do not test framework behavior (tsyringe wiring, Jest setup). Test the business logic only.

---

## TDD Is Non-Negotiable
- All production TypeScript code is written test-first. Tests define the contract; implementation satisfies the tests.
- This applies to both the React app (Cypress component tests) and DataMigration (Jest tests).
- Exception: logging calls do not require test coverage.
