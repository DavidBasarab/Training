# What NOT to Do

These are hard stops. Do not do any of the following under any circumstances.

## Type Safety
- Do NOT use `any`. Use `unknown` and narrow it, or define a proper type.
- Do NOT use `//@ts-ignore` or `//@ts-expect-error` to suppress TypeScript errors — fix the type.
- Do NOT use `as SomeType` to cast away a type error. Fix the underlying type instead. The two accepted exceptions are: Redux initial state placeholders (`current: {} as Room`) where the value is always populated before use, and Jest mock factory functions (`{} as unknown as jest.Mocked<IInterface>`) where the pattern is unavoidable.
- Do NOT use `!` non-null assertions to silence nullable warnings — handle the null case explicitly.

## Code Style
- Do NOT use `var` — use `const` or `let`.
- Do NOT use `.reduce()` for operations that are more clearly expressed with `.map()`, `.filter()`, or `for...of`.
- Do NOT concatenate strings with `+` — always use template literals.
- Do NOT abbreviate names — write them out fully.
- Do NOT write comments explaining what code does — rename or extract until it is obvious.

## React
- Do NOT use class components.
- Do NOT use `makeStyles`, `withStyles`, or inline `style` props — use the `sx` prop.
- Do NOT call hooks conditionally or inside loops — hooks must always be called in the same order.
- Do NOT omit the dependency array from `useEffect` — always provide one.
- Do NOT fetch data directly in components with raw `fetch` or `axios` — use the established API hooks.
- Do NOT use Redux for UI state that is local to a single component — use `useState`.
- Do NOT use Context as a Redux replacement for complex shared state with business logic — use Redux.
- Do NOT use the untyped `useDispatch` or `useSelector` — use `useAppDispatch` and `useAppSelector`.
- Do NOT define components inside other components — always define at module scope.
- Do NOT add boolean props to customize component behavior — create explicit variant components instead.
- Do NOT use `&&` in JSX with a left-hand side that can be `0`, `NaN`, or another falsy non-boolean — use a ternary.
- Do NOT store derived values in state and sync them with `useEffect` — compute them during render.
- Do NOT use `.sort()` — it mutates the array in place. Use `.toSorted()` instead.
- Do NOT import from barrel `index` files when direct imports are available — import from the source file.

## Testing
- Do NOT write new e2e tests — write Cypress component tests.
- Do NOT use JSON fixtures for test data — use `getFake*` functions from `UiDefined/`.
- Do NOT hard-code every field of a test object — spread `getFake*()` and pin only what the test depends on.
- Do NOT select elements in Cypress by class, tag, or text — always use `data-cy` via `cy.getByDataCy()`.

## Generated Code
- Do NOT manually edit any file under `Sites/main/src/generated/`. These files are auto-generated during the build by `InternallyUsedTools/CSharpPocoToTsInterfaces` and any manual changes will be overwritten on the next build.
- If a generated type is missing a field or is incorrect, fix the source C# POCO — not the generated TypeScript.
- To extend a generated type for UI-specific needs, add it to the corresponding file in `UiDefined/` rather than touching the generated file.

## Architecture
- Do NOT use property injection or setter injection in DataMigration — constructor injection via tsyringe only.
- Do NOT use `new` inside a DataMigration class to instantiate a service dependency — inject it via the constructor. This does not apply to value objects like `new Map()`, `new Set()`, or `new Date()`.
- Do NOT add abstractions or patterns that do not already exist in the surrounding codebase.
- Do NOT over-engineer — match the abstraction level of the existing code.

## Formatting
- Do NOT manually fight Prettier formatting — it is the final authority.
- Do NOT suppress ESLint rules without a comment explaining why.
