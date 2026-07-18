# Error Handling

## Scope
These rules apply to all React code in Sites/main. For DataMigration see the async correctness rules in `async.md`.

---

## Core Principle
Errors come from API callbacks — not from thrown exceptions in component code. There are no `try/catch` blocks in components. Every API call handles success and failure through `onResolve` and `onReject`:

```tsx
const {isPending, run: saveRoom} = useRoomEndpoint({
	method: "POST",
	onResolve: () => onClose(),
	onReject: (error) => dispatch(showSnackbarFromError({error, translationId: "rooms-save-network-error"})),
});
```

`onReject` is required for mutations (POST/PUT/DELETE) — omitting it silently ignores failures. GET calls that display errors via the `error` state and `NetworkAlert` do not need `onReject`.

---

## HTTP Error Classification

The API layer (`UseRefreshTokenPromises`) classifies errors automatically — no component code is needed for these:

| Status | Behaviour |
|---|---|
| **401** | Automatically redirects to `/logout`. Components never see this error. |
| **400** | Parses the response body into `error.validationErrors: Record<string, string[]>`. |
| **403** | Passed through. Displayed with `severity="info"` by `NetworkAlert` — it is a permission state, not a system error. |
| **404** | Passed through. `NetworkAlert` renders `<PageNotFoundView />` automatically. |
| **Other** | Passed through as a generic network error. |

---

## Choosing How to Display an Error

There are three error display mechanisms. Use the right one for the context:

### 1. `SiteError` — critical startup failures only
Use when a required app-level data fetch fails on startup. Renders a full-page error that auto-reloads once:

```tsx
if (serverStateError) return <SiteError error={serverStateError} translationId="global-serverState-getError" />;
if (licenseError) return <SiteError error={licenseError} translationId="main-content-licenseError" />;
```

Only used in `MainContent.tsx` for the three startup calls. Do not use `SiteError` for feature-level failures.

### 2. `NetworkAlert` via `SidePanel` or `Confirmation` — inline form/panel errors
When a save or load operation fails inside a panel or dialog, pass the error as a prop. `SidePanel` and `Confirmation` render `NetworkAlert` automatically:

```tsx
const {isPending, error} = useRoomEndpoint({method: "PUT"});

return (
	<SidePanel
		isPending={isPending}
		error={error}
		translations={{
			title: "rooms-edit-title",
			networkAlert: "rooms-edit-save-error",  // fallback translation if no validation errors
		}}
	>
		{/* form content */}
	</SidePanel>
);
```

`NetworkAlert` handles the logic: 404 shows `PageNotFoundView`, 403 shows an info alert, validation errors show per-field messages, and everything else shows the fallback `translationId`.

### 3. `showSnackbarFromError` — background operations and confirmation dialogs
Use for operations that close the UI before the user sees a result (deletions, background tasks, confirmations):

```tsx
onReject: (error) => {
	onClose();
	dispatch(showSnackbarFromError({
		error,
		translationId: "asset-card-set-background-network-error",
	}));
},
```

Use `showSnackbar` directly (without an error object) for non-error user feedback:

```tsx
dispatch(showSnackbar({
	alertProps: {severity: "success", translationId: "room-save-success"},
}));
```

---

## Validation Errors (400 responses)

A 400 response body is automatically parsed into `error.validationErrors`:

```ts
type ValidationErrors = Record<string, string[]>;
// e.g. { "Name": ["api-error-name-required", "api-error-name-too-long"] }
```

Individual error strings that start with `"api-error"` are used as translation IDs directly. Strings that do not start with `"api-error"` fall back to the `translationId` provided to `showSnackbarFromError` or `NetworkAlert`.

To display validation errors inline in a panel, extract them in `onReject` and pass them as part of the error prop:

```tsx
const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});

const {run: save} = useRoomEndpoint({
	method: "POST",
	onResolve: () => { setValidationErrors({}); onClose(); },
	onReject: ({validationErrors}) => setValidationErrors(validationErrors ?? {}),
});

return (
	<SidePanel error={{validationErrors}} isPending={isPending} translations={{networkAlert: "rooms-save-error"}}>
		{/* form content */}
	</SidePanel>
);
```

---

## Error Translation IDs

Every error display needs a fallback `translationId` in case there are no validation errors or the server returns an unexpected format. Follow the naming convention:

```
{feature}-{component}-{operation}-{error|network-error}
```

Examples: `"rooms-edit-save-network-error"`, `"asset-card-set-background-network-error"`, `"displays-properties-error"`

The generic fallback `"network-error"` exists for cases where no specific message is needed, but prefer a specific key so users understand what failed.

---

## What NOT to Do

- Do NOT use `try/catch` in component or hook code — errors flow through `onReject`
- Do NOT leave `onReject` undefined on any API call
- Do NOT render `error.message` directly — always go through the translation system
- Do NOT use `SiteError` for feature-level errors — it is for startup failures only
- Do NOT handle 401 errors in components — the API layer redirects to logout automatically
