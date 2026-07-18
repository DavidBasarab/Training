# Toolchain

## Prettier — Final Formatting Authority
Prettier is the single source of truth for all TypeScript and TSX formatting.

**React app config** (Sites/main/.prettierrc):
- `printWidth: 160`
- `tabWidth: 2` (but `useTabs: true` — tabs, not spaces)
- `trailingComma: es5`
- `singleQuote: false` (double quotes)
- `semi: true`

**Never fight Prettier.** If it reformats something, that is correct. Do not manually reformat to match what you think Prettier will do — just write readable code.

## ESLint — Sites/main
The React app uses ESLint 9 flat config format (eslint.config.js).
- Parser: `@typescript-eslint/parser`
- Plugins: `react`, `react-hooks`, `@typescript-eslint`, `prettier`
- Key rules enforced:
  - `@typescript-eslint/no-unused-vars: error` — unused variables are an error
  - `prettier/prettier: error` — formatting violations fail the lint check
  - `react-hooks/rules-of-hooks` and `react-hooks/exhaustive-deps` — hooks rules enforced

If ESLint flags something, address it.

## TypeScript — Strict Mode
Both projects run TypeScript in strict mode.
- Strict null checks are enabled.
- `noImplicitAny` is enabled — every value must have an inferred or explicit type.
- `experimentalDecorators` and `emitDecoratorMetadata` are enabled in DataMigration for tsyringe.

## Build Tools
- **React app (Sites/main):** Vite 5 with `@vitejs/plugin-react`, `vite-tsconfig-paths`, and `vite-plugin-svgr`. Build output goes to `build/`. Minification is disabled for debugging.
- **DataMigration:** `tsc` compiles to `build/`, then `nexe` packages to a single executable.
- Do not change build configuration unless asked.

## Path Aliases
The React app defines path aliases in `tsconfig.json` for all major source directories (`Actions/`, `Components/`, `Views/`, `Utilities/`, etc.). Always use these aliases rather than relative `../../` imports when crossing directory boundaries.

## Pre-Commit Behavior
TypeScript and React files are not checked by the C# pre-commit hook. ESLint and TypeScript errors are caught during build and CI. Keep the project lint- and type-error-free before merging.
