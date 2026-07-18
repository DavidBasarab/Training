# Performance

## Scope
These rules apply to all TypeScript and TSX code. Sections that mention React-specific APIs do not apply to Installer/DataMigration.

---

## Bundle Size

### Avoid Barrel File Imports
Do not import from barrel files (`index.ts` files that re-export many things). Barrel imports force the bundler to load every module in the barrel even if only one is used, adding hundreds of milliseconds to startup.

```ts
// Wrong — loads all 2000+ modules in the MUI barrel
import {Button, TextField} from "@mui/material";

// Correct — loads only the modules needed
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
```

The path aliases in `tsconfig.json` point to specific files, not barrel re-exports — always prefer them.

### Dynamic Imports for Heavy Components
Use `React.lazy()` with `Suspense` to defer loading large components that are not needed on initial render. This reduces the initial bundle and improves time-to-interactive:

```tsx
const MonacoEditor = React.lazy(() => import("Components/MonacoEditor"));

// Wrap in Suspense at the usage site
<Suspense fallback={<Waiter />}>
	<MonacoEditor />
</Suspense>
```

Use this for: rich text editors, charts, 3D viewers, code editors, and any component that pulls a large third-party library.

### Preload on User Intent
For components behind interactions (modals, tabs, drawers), trigger the import preload on `onMouseEnter` or `onFocus` — before the user clicks — so the bundle is ready when they do:

```tsx
const handleMouseEnter = () => {
	import("Components/HeavyModal");  // starts loading, no await needed
};

<button onMouseEnter={handleMouseEnter} onClick={handleOpen}>Open</button>
```

---

## Preventing Unnecessary Re-renders

### Never Define Components Inside Components
Defining a component inside another component creates a new component type on every render. React treats it as a different component each time and remounts it, destroying all state and DOM nodes. This causes inputs to lose focus, animations to restart, and scroll position to reset.

```tsx
// Wrong — new component type created on every render of Parent
const Parent = () => {
	const Child = () => <div>{value}</div>;  // recreated every render
	return <Child />;
};

// Correct — defined at module scope, stable identity
const Child = ({value}: ChildProps) => <div>{value}</div>;

const Parent = () => {
	return <Child value={value} />;
};
```

### Derive State During Rendering — No Effect+State
If a value can be computed from existing props or state, compute it directly during render. Do not store it in a separate state variable and sync it with `useEffect`. The extra state causes an additional render cycle and risks getting out of sync:

```tsx
// Wrong — extra state + effect = two renders, sync risk
const [fullName, setFullName] = useState("");
useEffect(() => {
	setFullName(`${firstName} ${lastName}`);
}, [firstName, lastName]);

// Correct — derived during render, always in sync
const fullName = `${firstName} ${lastName}`;
```

### Lazy State Initialization
When computing an initial state value is expensive, pass a function to `useState` instead of the value. Without the function form, the expensive computation runs on every render even though the result is only used once:

```tsx
// Wrong — parses JSON on every render
const [config, setConfig] = useState(JSON.parse(localStorage.getItem("config") ?? "{}"));

// Correct — runs only once on mount
const [config, setConfig] = useState(() => JSON.parse(localStorage.getItem("config") ?? "{}"));
```

### Functional setState
When a state update depends on the current state value, always use the functional update form. This prevents stale closures and eliminates the need to add state to effect dependency arrays:

```tsx
// Wrong — stale closure risk if called in async context
setCount(count + 1);

// Correct — always reads the current value
setCount((current) => current + 1);
```

### Narrow Effect Dependencies
Subscribe to the specific primitive value an effect needs rather than the whole object. This prevents effects from re-running when unrelated parts of an object change:

```tsx
// Wrong — re-runs whenever any user property changes
useEffect(() => { syncName(user.name); }, [user]);

// Correct — re-runs only when name changes
useEffect(() => { syncName(user.name); }, [user.name]);
```

### Do Not Memoize Cheap Expressions
Do not wrap simple expressions with primitive results in `useMemo`. The hook overhead can exceed the cost of the expression itself:

```tsx
// Wrong — measuring micro-optimizations that don't matter
const isLoading = useMemo(() => user.isLoading || settings.isLoading, [user.isLoading, settings.isLoading]);

// Correct — just compute it
const isLoading = user.isLoading || settings.isLoading;
```

Memoization is appropriate for: expensive computations (sorting/filtering large arrays), object/array references passed as props to memoized children, and callbacks passed to memoized children.

### Memoized Components — Stable Non-Primitive Defaults
When a memoized component has an optional prop with a non-primitive default value, extract that default to a module-level constant. Inline default values create a new object/array reference on every render, breaking the memo comparison:

```tsx
// Wrong — new array reference breaks memo on every render
const Chart = memo(({data = []}: ChartProps) => { ... });

// Correct — stable reference, memo comparison works
const EMPTY_DATA: DataPoint[] = [];
const Chart = memo(({data = EMPTY_DATA}: ChartProps) => { ... });
```

### useRef for Transient Values
Use `useRef` for values that change frequently but do not drive the rendered output. Storing them in state causes unnecessary re-renders:

```tsx
// Wrong — re-renders on every mouse move
const [mouseX, setMouseX] = useState(0);

// Correct — tracked without triggering re-renders
const mouseXRef = useRef(0);
const handleMouseMove = (e: MouseEvent) => { mouseXRef.current = e.clientX; };
```

### Put Interaction Logic in Event Handlers
If a side effect is triggered by a specific user action, run it in that event handler — not in a `useEffect` watching state. Modeling actions as state + effect causes the effect to re-run on any change to its dependencies, not just the action:

```tsx
// Wrong — effect re-runs whenever isSubmitted changes
const [isSubmitted, setIsSubmitted] = useState(false);
useEffect(() => { if (isSubmitted) submitForm(); }, [isSubmitted]);

// Correct — runs exactly when the user clicks
const handleSubmit = () => { submitForm(); };
```

### Hoist Static JSX
Extract JSX elements that never change to module-level constants. React recreates JSX objects on every render; hoisted elements reuse the same reference:

```tsx
// Wrong — new object created on every render
const Layout = () => <div><Header /></div>;

// Correct — created once
const header = <Header />;
const Layout = () => <div>{header}</div>;
```

### useTransition for Non-Urgent Updates
Use `useTransition` instead of a manual `isLoading` state when an update can be deferred without blocking the UI. React will keep the current UI interactive during the transition:

```tsx
const [isPending, startTransition] = useTransition();

const handleTabChange = (tab: string) => {
	startTransition(() => {
		setActiveTab(tab);
	});
};
```

### Conditional Rendering — Ternary Over &&
Use explicit ternaries instead of `&&` when the left-hand side can be `0`, `NaN`, or any other falsy non-boolean value. `&&` will render those falsy values as visible text nodes:

```tsx
// Wrong — renders "0" when count is 0
{count && <Badge count={count} />}

// Correct — explicit and safe
{count > 0 ? <Badge count={count} /> : null}
```

---

## JavaScript Performance

### Use Set/Map for Repeated Membership Checks
Array `.includes()` and `.find()` are O(n) per call. When checking membership multiple times, convert to a `Set` or `Map` first for O(1) lookups:

```ts
// Wrong — O(n) per check
const isSelected = selectedIds.includes(id);

// Correct — O(1) per check
const selectedSet = new Set(selectedIds);
const isSelected = selectedSet.has(id);
```

### Build Index Maps for Repeated Lookups
When joining two collections by a key, build a `Map` from the lookup collection first. Repeated `.find()` calls are O(n×m); a Map reduces it to O(n+m):

```ts
// Wrong — O(n) for every item in orders
const enriched = orders.map((order) => ({
	...order,
	user: users.find((u) => u.id === order.userId),
}));

// Correct — build index once, O(1) per lookup
const userById = new Map(users.map((u) => [u.id, u]));
const enriched = orders.map((order) => ({...order, user: userById.get(order.userId)}));
```

### Use flatMap to Map and Filter in a Single Pass
Chaining `.map().filter()` creates an intermediate array and iterates twice. Use `.flatMap()` to transform and filter in one pass:

```ts
// Wrong — two iterations, intermediate array
const names = items.map((item) => item.name).filter(Boolean);

// Correct — one pass
const names = items.flatMap((item) => (item.name ? [item.name] : []));
```

### Use toSorted() for Immutable Sorting
`.sort()` mutates the array in place, which causes subtle bugs when sorting React state or shared arrays. Use `.toSorted()` to return a new sorted array without mutation:

```ts
// Wrong — mutates the original array
const sorted = items.sort((a, b) => a.name.localeCompare(b.name));

// Correct — returns new array, original unchanged
const sorted = items.toSorted((a, b) => a.name.localeCompare(b.name));
```

Also prefer `.toReversed()`, `.toSpliced()`, and `.with()` for the same reason.

### Hoist RegExp Creation
Do not create `RegExp` objects inside render functions or loops. Hoist them to module scope so they are created once:

```ts
// Wrong — new RegExp on every render
const isValid = /^[a-z0-9]+$/i.test(input);

// Correct — created once at module scope
const ALPHANUMERIC = /^[a-z0-9]+$/i;
const isValid = ALPHANUMERIC.test(input);
```

Note: do not hoist RegExp with the `g` flag to module scope if it is used concurrently — the `lastIndex` state is mutable and shared.
