# Naming & Structure

## Scope
These rules apply to all TypeScript and TSX files in this codebase — both the React frontend (Sites/main) and the Node.js CLI tools (Installer/DataMigration). Language-specific rules for React components and testing build on top of these.

## Core Philosophy
- Follow the same Clean Code principles as C#: methods do one thing, names reveal intent.
- Match the abstraction level and style of the surrounding code.

## Naming Rules
- Avoid abbreviations. Use full words so readers never have to guess.
- Acceptable abbreviations: widely recognized acronyms (e.g. `HTTP`, `URL`, `ID`) and any abbreviation in the top 3 Google results for that term.
- Names reveal intent. A function name makes it unnecessary to read its body.
- PascalCase: classes, interfaces, types, enums, React components, files containing components
- camelCase: variables, function parameters, local constants, non-component functions
- SCREAMING_SNAKE_CASE: module-level constants that are truly constant values (not config objects)
- Boolean names read as questions or states: `isReady`, `hasOutputs`, `canRestore`, `isDisabled`
- Event handlers are prefixed `handle`: `handleClick`, `handleRoomSelect`
- Data-fetching and computation helpers are prefixed `get`: `getActivityName`, `getRoomStatus`
- Custom hooks always start with `use`: `useRoomEndpoint`, `useAppContext`

## Files & Modules
- One primary export per file. Name the file after that export.
- React component files use PascalCase: `RoomCard.tsx`, `LandingView.tsx`
- Non-component TypeScript files use PascalCase: `Logger.ts`, `Runner.ts`, `SnackbarSlice.ts`
- Test files mirror the source file name with a `.specs.tsx` or `.cspecs.tsx` or `.specs.ts` suffix.
- Use path aliases (configured in tsconfig.json) rather than deep relative imports.

## Types & Interfaces
- Use `type` for unions, intersections, complex compositions, context shapes, and hook return types.
- Use `interface` for object shapes that extend another type, or that are likely to be extended by others.
- React component props always use `type`: `type RoomCardProps = { ... }`. See `react.md` for the full component structure rule.
- For non-props object shapes, both `type` and `interface` are acceptable — be consistent with the surrounding code. Prefer `interface` when the shape extends a base type.
- Props are named `<ComponentName>Props`: `RoomCardProps`, `ActionMenuProps`.
- Interfaces that serve as contracts (e.g. in UiDefined) use the `I` prefix: `IAssetCreated`, `IRole`.
- Nullable reference types (`null` or `undefined` annotations) are allowed only where the value is genuinely optional or nullable. Do not annotate everything as nullable defensively.
- Use `unknown` when a type genuinely cannot be known at the call site, then narrow it before use.
- Prefer explicit return types on public functions and hook return values. Rely on inference for local variables.

## Functions & Methods
- Functions should be as short as possible. ~15 lines is a signal to evaluate extraction.
- No function should require a comment to explain what it does. Rename or extract instead.
- Use arrow functions everywhere — components, hooks, utilities, callbacks, and handlers.
- The only exception is test functions that need access to Cypress alias context via `this` — those must use `function()` declarations, not arrow functions, because arrow functions do not bind `this`.
- Prefer early returns and guard clauses over deep nesting.

## Spacing & Formatting
- Leave a blank line between logical sections within a function.
- Keep related imports grouped (React, third-party, internal) — prettier-plugin-organize-imports handles ordering automatically.

## Directory Structure (Sites/main/src)

Where to put new code in the React frontend:

| Directory | What goes here |
|---|---|
| `Components/` | Reusable UI components shared across views. One folder per component: `ComponentName.tsx`, `ComponentName.cspecs.tsx`, optional type files. |
| `Views/` | Page-level components, organized hierarchically by feature. Tabs and sub-panels nest beneath their parent view folder. |
| `Utilities/` | Shared helper functions and custom hooks not tied to a single feature. |
| `UiDefined/` | UI-specific types that extend or augment generated types, and `getFake*` test data generators. |
| `Languages/` | i18n translation files. `en-US.json` is the master — all new keys go here only. |
| `generated/` | Auto-generated TypeScript from C# POCOs. Never edited manually. |
| `Reducers/` | Root reducer only (`index.ts` with `combineReducers`). Individual slice files live alongside the feature in `Views/`, or at `src/` root for app-wide slices (e.g. `SnackbarSlice.ts`). |
| `Modals/` | Modal dialog components. |
| `Routes/` | Route definitions and navigation. |
| `Theme/` | MUI theme configuration. |
| `Consts/` | Module-level constants and shared regex patterns. |

**Placement rule:** If a component or hook is used by exactly one feature, co-locate it inside that feature's folder in `Views/`. If it is shared across two or more features, move it to `Components/` or `Utilities/`.
