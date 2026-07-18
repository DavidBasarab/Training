# DataMigration

## Scope
These rules apply to `Installer/DataMigration` only. They do not apply to `Sites/main` or any other part of the codebase.

---

## Writing a Migration

### File location and naming

Migrations live in `Installer/DataMigration/migrations/`. Name the file:

```
XXXX-what-it-does.ts
```

Where `XXXX` is a zero-padded integer one higher than the last migration in the directory (e.g. `0014-addRoomSlug.ts`). Use kebab-case for the description.

### Structure

A migration exports a single `up` function. There is no class, no constructor, and no DI. The function receives the MongoDB `Db` instance and returns `Promise<void>`:

```typescript
import {Db} from "mongodb";
import {IMigrationScript} from "../src/Types/Migration";

export const up: IMigrationScript["up"] = async (db: Db): Promise<void> => {
	const collection = db.collection("Rooms");

	await collection.updateMany({IsActive: {$exists: false}}, {$set: {IsActive: true}});
};
```

Always type the function as `IMigrationScript["up"]` — this keeps the signature in sync with the interface automatically.

### Registering the migration

After creating the file, add it to `MigrationRegistryAuto.ts`. Add an import at the top and an entry in the `migrationRegistry` array:

```typescript
import * as migration14 from "./migrations/0014-addRoomSlug";

export const migrationRegistry: Migration[] = [
	// ...existing entries...
	{id: 14, name: "0014-addRoomSlug", up: migration14.up},
].sort((a, b) => a.id - b.id);
```

The `id` is the numeric value of the zero-padded prefix. Keep the array sorted — the `.sort()` call at the end is a safety net, not a substitute for inserting in the correct position.

---

## Error Handling

### Let errors throw

Migrations fail by throwing. Do not catch and swallow errors. If a MongoDB operation fails, let the exception propagate:

```typescript
// Correct — let failures surface
export const up: IMigrationScript["up"] = async (db: Db): Promise<void> => {
	const collection = db.collection("ApplianceDatas");
	await collection.updateMany({}, {$set: {IsActive: true}});
};

// Wrong — swallowed error hides failure
export const up: IMigrationScript["up"] = async (db: Db): Promise<void> => {
	try {
		await collection.updateMany({}, {$set: {IsActive: true}});
	} catch {
		// never do this
	}
};
```

The Runner catches thrown errors, logs them, stops execution, and records the failure. A migration is only marked as applied if it completes without throwing.

### When to catch

Only catch when you need to handle a specific recoverable case within a single document — for example, when parsing a JSON string that may be malformed, where skipping the document is intentional:

```typescript
for await (const doc of cursor) {
	let parsed;
	try {
		parsed = JSON.parse(doc.DataJson);
	} catch {
		continue; // skip documents with invalid JSON
	}
	// ... process parsed ...
}
```

Even then: log or count skipped documents so the output shows what happened.

### Never return an error value

DataMigration uses thrown exceptions for failures — not return codes or result objects. The `up` function returns `Promise<void>`. If something is wrong enough to stop the migration, throw.

---

## Dependency Injection (tsyringe)

Migrations themselves do not use DI — they are plain functions. DI is used by the infrastructure services in `src/`.

### Registering a new service

Add registrations to `src/Containers/DependencyInjection.ts`. Use `registerSingleton` for services that should share a single instance, and `register` for everything else:

```typescript
container.registerSingleton<IMyService>("IMyService", MyService);
container.register<IMyOtherService>("IMyOtherService", {useClass: MyOtherService});
```

Always register as an interface (`IMyService`), never as the concrete class.

### Implementing a service

Decorate the class with `@injectable()`. If it must be a singleton, also add `@singleton()`. Inject dependencies by name using `@inject("IKey")` in the constructor:

```typescript
import {injectable, inject} from "tsyringe";

@injectable()
export class MyService implements IMyService {
	constructor(
		@inject("ILogger") private readonly logger: ILogger,
		@inject("IMongo") private readonly mongo: IMongo
	) {}
}
```

Use named string keys (`"ILogger"`, `"IMongo"`) — not type-based resolution. The key must match the registration in `DependencyInjection.ts`.

### Resolving at the entry point

Services are resolved once in `src/index.ts` using `container.resolve`. Do not call `container.resolve` anywhere else:

```typescript
const runner = container.resolve<IRunner>("IRunner");
```

---

## Logging

Inject `ILogger` and use it in place of `console.log` in any service class. Inside migration functions, `console.log` is acceptable since migrations are plain functions with no DI:

```typescript
// In a service — use ILogger
this.logger.log(`Processing ${count} documents`);
this.logger.error("Unexpected failure", error);

// In a migration function — console.log is fine
console.log(`Updated ${updatedCount} documents`);
```

Log progress at meaningful checkpoints — start, completion, and document counts — so operators can verify what ran.
