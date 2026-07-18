# React Patterns

## Scope
These rules apply to all React code in Sites/main. They do not apply to Installer/DataMigration.

## Component Structure
- Use functional components only. No class components.
- Props are destructured in the function signature.
- The props type is defined as `type <Name>Props = { ... }` and placed after the component, before the export.
- Export the component as the default export at the bottom of the file.

```tsx
const RoomCard = ({room, locked = false, sx}: RoomCardProps) => {
	// hooks first
	// derived values
	// handlers
	// render
	return (...);
};

type RoomCardProps = {
	room: Room;
	locked?: boolean;
	sx?: SxProps<Theme>;
};

export default RoomCard;
```

- Hooks are called at the top of the component body, before any derived values or handlers.
- Derived values (computed from props/state) come next.
- Event handlers (prefixed `handle`) come after derived values.
- The return statement is last.

## Custom Hooks
- Custom hooks live in `Utilities/` or alongside the feature they serve.
- Hook files use PascalCase with `Use` prefix: `UseCineNetApi.tsx`, `UseRoomEndpoint.tsx`.
- Hooks return typed values. Prefer returning a named object over a tuple unless there are exactly two values with obvious meaning.
- When two hooks differ only in their response type — not in their behaviour or API path — prefer a single generic hook over two identical copies: use `useApi<T>()` rather than `useRoomsApi()` and `useActivitiesApi()` that do the same thing with different types.
- Separate named hooks are correct when they encode meaningfully different behaviour or target a different API resource. `useRoomEndpoint` and `useDeviceEndpoint` are separate because they call different API base paths — not because they return different types. Do not try to collapse them into a single generic.

## Context
- Context is typed explicitly: `React.createContext<ContextType | null>(null)`
- A custom hook wraps `useContext` and throws a descriptive error if used outside the provider:

```tsx
export const useFeatureFlags = () => {
	const context = useContext(FeatureFlagContext);
	if (!context) throw new Error("useFeatureFlags must be used within FeatureFlagProvider");
	return context;
};
```

- The provider component is named `<Name>Provider` and accepts `{children: React.ReactNode}`.
- Export both the provider and the custom hook. Do not export the raw context.

## Context vs Redux — When to Use Each

Use **React Context** when:
- Passing data to deeply nested components that would otherwise require prop drilling (theme, locale, current user, feature flags).
- The data is relatively static or changes infrequently.
- The consuming subtree is well-defined and bounded.
- You do not need to trace state changes, replay actions, or attach middleware.

Use **Redux** when:
- State is shared across many distant parts of the application.
- You need DevTools support for tracing state updates and debugging.
- You need middleware (e.g., for centralized API logic, side effects).
- You need Redux-adjacent capabilities: persistence with redux-persist, optimistic updates, undo history, or serializable action logs.
- The data drives business logic, not just UI presentation.

**The guiding principle:** If you would only use Redux to avoid passing props, Context is the right tool. If you need centralized state management with traceability and middleware, Redux is the right tool. Do not use Redux as a prop-drilling workaround, and do not use Context as a Redux replacement for complex shared state. React Redux uses Context internally — that is its job, not yours.

## Redux (Redux Toolkit)

### Slice structure
Name the file `FeatureSlice.ts` and co-locate it with the feature in `Views/`. App-wide slices (e.g. `SnackbarSlice.ts`) live at `src/` root. The structure is:

```ts
import {createSlice, PayloadAction} from "@reduxjs/toolkit";
import {useAppSelector} from "configureStore";

const initialState: RoomState = {
	current: {} as Room,
};

const RoomSlice = createSlice({
	name: "room",
	initialState,
	reducers: {
		setCurrentRoom: (state, action: PayloadAction<Room>) => {
			state.current = action.payload;
		},
	},
});

type RoomState = {
	current: Room;
};

// Typed selector hooks — co-locate here, not inline at the call site
export const useCurrentRoom = () => useAppSelector((state) => state.room.current);

export const {setCurrentRoom} = RoomSlice.actions;
export default RoomSlice.reducer;
```

After creating a slice, register it in `Reducers/index.ts` under a key name.

### Rules
- Use `createSlice` from `@reduxjs/toolkit` for all slice definitions.
- Reducer payloads are typed with `PayloadAction<T>`.
- Actions are destructured from `slice.actions` and exported as named exports.
- The reducer is the default export from a slice file.
- Use the typed `useAppDispatch` and `useAppSelector` hooks (defined in `configureStore.ts`) — never the untyped `useDispatch` / `useSelector` directly.
- Co-locate typed selector hooks in the slice file. Do not write inline `useAppSelector` calls scattered through components.

## MUI Styling
- Use the `sx` prop for component-level styles.
- Extract repeated style objects into named constants above the component: `const cardSx = { ... }`.
- Spread multiple style objects in `sx`: `sx={{...cardSx, ...sx, color: isDisabled ? "text.disabled" : undefined}}`.
- Accept `sx?: SxProps<Theme>` in props to allow callers to extend styles.
- Do not use `makeStyles`, `withStyles`, or inline `style` props. Use `sx` only.

## Testing Attributes
- Add `data-cy` to every interactive and meaningful element for Cypress tests: `data-cy="roomCard-{room.Slug}"`.
- Add `data-auto` for automation frameworks using `generateDataAuto(category, name)`.
- Never select elements in tests by class name, element type, or text content — always by `data-cy`.

## Data Fetching

All data fetching goes through the pre-built endpoint hooks in `Utilities/Api/UseCineNetApi.tsx`. Never use raw `fetch` or `axios` in a component.

### Using an existing endpoint hook

Each hook covers a known API resource (`useRoomEndpoint`, `useDeviceEndpoint`, `useCineLinksEndpoint`, etc.). Pass an options object and destructure the result:

```tsx
const {data, isPending, error} = useRoomEndpoint<Room[]>();
```

Available options:

| Option | Type | Purpose |
|---|---|---|
| `method` | `"GET" \| "POST" \| "PUT" \| "DELETE"` | HTTP method. Default: `"GET"`. |
| `apiEndpoint` | `string` | Path suffix appended to the base endpoint (e.g. `"/${room.Id}"`). |
| `onResolve` | `(data: T) => void` | Called on success. |
| `onReject` | `(error: Error) => void` | Called on failure. Required for mutations (POST/PUT/DELETE) — omitting it silently ignores failures. Not needed for GET calls that display errors via the `error` state and `NetworkAlert`. |
| `useDefer` | `boolean` | If true, the request does not fire on mount. Call `run()` explicitly. |

### GET on mount (immediate)

Omit `useDefer` for data that should load when the component mounts:

```tsx
const {data: rooms, isPending, error} = useRoomEndpoint<Room[]>();

if (isPending) return <Waiter />;
if (error) return <NetworkAlert translationId="rooms-load-error" error={error} />;

return <RoomList rooms={rooms} />;
```

### POST / PUT / DELETE (deferred)

Pass `useDefer: true` so the request only fires when `run()` is called. Rename `run` to describe the action:

```tsx
const {run: saveRoom, isPending, error} = useRoomEndpoint<Room>({
	method: "POST",
	onResolve: () => onClose(),
	onReject: (error) => dispatch(showSnackbarFromError({error, translationId: "rooms-save-error"})),
	useDefer: true,
});

return (
	<SidePanel isPending={isPending} error={error} translations={{title: "rooms-edit-title", networkAlert: "rooms-save-error"}}>
		<FormField {...easyForm("Name")} />
	</SidePanel>
);
```

Call `run(payload)` from the submit handler. Pass the request body directly, or pass `{path: "/suffix"}` to append a path segment:

```tsx
const handleSave = (data: Room) => {
	saveRoom(data);                          // POST with body
};

const handleDelete = () => {
	deleteRoom({path: `/${room.Id}`});       // path segment only
};
```

### Chaining two calls

Use `onResolve` on the first hook to trigger the second. Each hook manages its own `isPending` and `error` — aggregate them for the UI:

```tsx
const {run: createLink, isPending: linkPending, error: linkError} = useNodeManager({
	method: "POST",
	onResolve: onClose,
	onReject: (error) => dispatch(showSnackbarFromError({error, translationId: "device-link-error"})),
	useDefer: true,
});

const {run: createDevice, isPending: createPending, error: createError} = useDeviceEndpoint<Device>({
	method: "POST",
	onResolve: (device) => createLink({path: `/Link/Device/${device.Id}`}),
	onReject: (error) => dispatch(showSnackbarFromError({error, translationId: "device-create-error"})),
	useDefer: true,
});

const isPending = createPending || linkPending;
const error = createError || linkError;
```

### Adding a new endpoint hook

If no existing hook covers your API resource, add one to `UseCineNetApi.tsx` following the `addToApiEndpoint` pattern already used there. Do not create a new file — all endpoint hooks live in that one file.

### Co-location rule

Co-locate the endpoint hook call with the component that uses it. If the same hook is called by two or more unrelated components, extract it to a custom hook in `Utilities/`.

## Loading States

### Initial data loads — return `<Waiter />`
When a component is waiting for data it needs to render, return `<Waiter />` as an early exit. Never return `null` while loading — always give the user visual feedback. Always check `isPending` first, then `error`, then render with data:

```tsx
if (isPending) return <Waiter />;
if (error) return <NetworkAlert translationId="rooms-load-error" error={error} />;

return <RoomList rooms={data} />;
```

`<Waiter />` renders MUI `<Skeleton>` rows. Use the `rowCount` prop when the default of 4 rows does not fit the context:

```tsx
if (isPending) return <Waiter rowCount={2} />;
```

### Action buttons — pass `isPending` to the panel or dialog
For save/submit operations, pass `isPending` and `error` as props to `SidePanel` or `Confirmation`. They handle disabling buttons and showing a `CircularProgress` spinner automatically — no extra code needed in the component. The form content remains visible while saving:

```tsx
const {run: saveRoom, isPending, error} = useRoomEndpoint<Room>({
	method: "POST",
	onResolve: () => onClose(),
	onReject: (error) => dispatch(showSnackbarFromError({error, translationId: "rooms-save-error"})),
	useDefer: true,
});

return (
	<SidePanel isPending={isPending} error={error} translations={{title: "rooms-edit-title", networkAlert: "rooms-save-error"}}>
		{/* form content — stays visible while saving */}
	</SidePanel>
);
```

### Modal-blocking operations — use `<WaiterModal>`
For operations that must block the entire UI while running, use `<WaiterModal>`. It renders a `LinearProgress` bar inside a dialog. Use this sparingly — `SidePanel` with `isPending` handles most save operations.

## SidePanel vs Confirmation

Choose the right container based on what the user is doing:

| Container | When to use |
|---|---|
| `SidePanel` | Create or edit forms. The user fills fields, then saves. The panel stays open while saving, form content remains visible, only buttons disable. |
| `Confirmation` | Destructive or irreversible actions: delete, reset, bulk operations. A short description of what will happen, then confirm/cancel. On confirm, close immediately and dispatch `showSnackbarFromError` in `onReject`. |
| `WaiterModal` | Operations that must block the entire UI while running. Use sparingly — `SidePanel` with `isPending` handles most save operations. |

```tsx
// SidePanel — for forms
<SidePanel isPending={isPending} error={error} translations={{title: "rooms-edit-title", networkAlert: "rooms-save-error"}}>
	<FormField {...easyForm("Name")} />
</SidePanel>

// Confirmation — for destructive actions
<Confirmation
	isPending={isPending}
	translations={{title: "rooms-delete-title", body: "rooms-delete-body", networkAlert: "rooms-delete-error"}}
	onConfirm={() => deleteRoom(room.Id)}
/>
```

## Permission Checking

Permission checking uses three tools from `Components/Permission/`, depending on the use case:

### Declarative gating — `<Permission>`
Wrap a section of JSX to hide it entirely when the user lacks permission:

```tsx
<Permission permissionsAllowingAccess={["RoomConfig", "SystemAdmin"]}>
	<EditRoomButton />
</Permission>
```

`<Permission>` renders `<NoAccess />` by default when access is denied. Pass `replacementComponent` to customize it, or `allowNoPermissions` to grant access when no permissions are required.

### Hook — `usePermission`
Use when you need the boolean in component logic:

```tsx
const canEdit = usePermission({permissionsAllowingAccess: ["RoomConfig"]});

return (
	<Button disabled={!canEdit}>Edit</Button>
);
```

### Imperative — `checkPermissions`
Use outside of a hook context — typically when filtering an array of items:

```tsx
const visibleActions = actions.filter((action) =>
	checkPermissions({permissionsAllowingAccess: action.permissions, allowNoPermissions: true})
);
```

`checkPermissions` is not a hook — it reads from context internally but can be called inside a `.filter()` or `.map()`.

**`PermissionType`** is auto-generated from the C# backend in `generated/DataContracts/ServiceModels/Permission/Permissions/PermissionType.ts`. Never define permissions manually.

## State and Side Effects
- Prefer local component state (`useState`) for UI state that does not need to be shared.
- Use Redux only for state that is genuinely shared across multiple distant components.
- Use `useEffect` sparingly. If you can compute a value from existing state and props, do that instead.
- Every `useEffect` must have a dependency array. A missing dependency array is a bug.
- See `performance.md` for detailed rules on deriving state during render, narrowing effect dependencies, and moving interaction logic into event handlers.

## Feature Flags
- Feature flags live in `FeatureFlagContext.tsx`.
- New flags are added to the `FeatureFlags` type and defaulted in the provider.
- Access flags via `useFeatureFlags()`. Never read them from a global variable or environment variable directly in components.

## Composition Patterns

### Avoid Boolean Prop Proliferation
Do not add boolean props like `isThread`, `isDMThread`, `isEditing` to customize a component's behavior. Each boolean doubles the number of possible states and creates unmaintainable conditional branches. Instead, create explicit variant components that compose the pieces they need:

```tsx
// Wrong — booleans multiply complexity
<Composer isThread={true} isDMThread={false} isEditing={false} />

// Correct — explicit variants, self-documenting
<ThreadComposer />
<DirectMessageComposer />
<EditMessageComposer />
```

### Compound Components
Structure complex components as compound components that share state via context rather than passing it through props. Export the pieces as a composed object so consumers can assemble what they need:

```tsx
// Each sub-component reads shared state from context, not from props
const Composer = {
	Provider: ComposerProvider,
	Input: ComposerInput,
	Submit: ComposerSubmit,
};

// Consumer composes only what it needs
<Composer.Provider>
	<Composer.Input />
	<Composer.Submit />
</Composer.Provider>
```

### Lift State Into Providers
When multiple components (including siblings outside the main visual tree) need to share or modify the same state, move that state into a dedicated provider. Components that need the shared state don't need to be visually nested — they only need to be within the same provider boundary. Before choosing a provider, see **Context vs Redux — When to Use Each** above to decide whether the state belongs in a Context provider or Redux.

### Context Interface: State, Actions, Meta
When a context will have multiple implementations or needs to be testable in isolation, define its interface with three explicit parts:

```tsx
type ComposerContextType = {
	state: {text: string; attachments: Attachment[]};
	actions: {setText: (text: string) => void; send: () => void};
	meta: {isSending: boolean; characterLimit: number};
};
```

This separates read-only state, mutation actions, and derived/metadata values — making the contract clear and the context easy to fake in tests.

### Decouple State Implementation From UI
UI components should consume the context interface only — they must not know whether the state comes from `useState`, Redux, a server sync, or a test double. The provider is the only place that knows how state is managed. This means the same `Composer.Input` component works with any provider that satisfies the interface.

### Children Over Render Props
Prefer `children` for composition when the parent does not need to provide data to the child. Use render props only when the parent must pass data or state down:

```tsx
// Prefer — composing static structure
<Card>
	<CardHeader />
	<CardBody />
</Card>

// Only when parent provides data the child needs
<DataProvider render={(data) => <Chart data={data} />} />
```

