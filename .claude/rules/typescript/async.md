# Async / Await

## Correctness Rules
- Async all the way up. If anything in a call chain is async, the whole chain is async.
- Use `async/await`. Avoid raw `.then()/.catch()` chains — the only exception is framework callbacks that structurally require them.
- Always handle errors — do not swallow promise rejections silently.

## Parallel Async Operations
When two or more async operations have no dependency on each other, run them concurrently with `Promise.all()`. Sequential awaits compound round-trip time:

```ts
// Wrong — sequential, each waits for the previous
const user = await getUser(id);
const settings = await getSettings(id);
const permissions = await getPermissions(id);

// Correct — all three start immediately
const [user, settings, permissions] = await Promise.all([
	getUser(id),
	getSettings(id),
	getPermissions(id),
]);
```

## Defer Await Until Needed
Do not await a promise before you know whether you need its value. Start the operation immediately but defer the `await` to the point where the value is actually used:

```ts
// Wrong — always pays the cost, even on early return
const data = await fetchData();
if (!isEnabled) return null;
return process(data);

// Correct — start fetch immediately, only await if needed
const dataPromise = fetchData();
if (!isEnabled) return null;
return process(await dataPromise);
```

## Partial-Dependency Parallelization
When operations have partial dependencies, start independent operations as early as possible. Create the promises first, then await them at the latest point each is needed:

```ts
// Wrong — sequential even though B and C don't depend on A
const a = await fetchA();
const b = await fetchB();
const c = await fetchC();
const d = await fetchD(a);

// Correct — B and C start immediately; only D waits for A
const bPromise = fetchB();
const cPromise = fetchC();
const a = await fetchA();
const [b, c] = await Promise.all([bPromise, cPromise]);
const d = await fetchD(a);
```
