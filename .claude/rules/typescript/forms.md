# Forms

## Scope
These rules apply to all form code in Sites/main. Forms are built on react-hook-form via the `useEasyForm` wrapper hook.

---

## useEasyForm

All forms use the `useEasyForm` hook from `Components/Forms/EasyForm/UseEasyForm.ts`. It wraps react-hook-form's `useForm` and adds translation key generation and shared defaults:

```tsx
const {easyForm, handleSubmit, reset, getValues, setValue, watch} = useEasyForm<Room>(
	"rooms-edit-form",  // translation prefix — all field keys derive from this
	{
		defaultValues: room,
		shouldUnregister: true,
		required: true,   // makes all fields required by default
		width: 400,
	}
);
```

The second argument forwards all standard `UseFormProps` options plus these additions:
- `required` — sets required on all fields; individual fields can override
- `width` — default width applied to all `FormField` components

---

## Declaring Form Fields — `easyForm()`

Call `easyForm(fieldName, options?)` and spread the result into a `FormField` or `FormsSelect`. It automatically generates the translation key as `${translationPrefix}-${fieldName}`:

```tsx
// Translation key becomes "rooms-edit-form-Name" automatically
<FormField {...easyForm("Name", {commonPattern: "alphaNumericHyphenSpace"})} />

// Override required for optional fields
<FormField {...easyForm("Description", {required: false})} />

// Custom validator — return true for valid, false for invalid
<FormField {...easyForm("Slug", {validate: isUniqueSlug})} />
```

---

## Field Components

`FormField` and `FormsSelect` are the standard field components. Both accept the spread from `easyForm()`:

```tsx
// Text, number, and other standard inputs
<FormField {...easyForm("Name")} />

// Select dropdowns
<FormsSelect
	{...easyForm("RoomId")}
	options={rooms}
	defaultValue={room.RoomId}
/>
```

For non-standard inputs (date pickers, custom components), use react-hook-form's `Controller` directly.

---

## Validation

### Common patterns (`commonPattern`)
Pre-defined regexes available via the `commonPattern` option: `alphaNumericHyphenSpace`, `ipAddress`, `ipv6ValidCharacters`, and others. Use these instead of duplicating regexes:

```tsx
<FormField {...easyForm("IpAddress", {commonPattern: "ipAddress"})} />
```

### Custom validators (`validate`)
Pass a function that returns `true` for valid and `false` (or a translation ID string) for invalid:

```tsx
const isUniqueName = (value: string) => {
	const isDuplicate = existingRooms.some((r) => r.Name === value && r.Id !== room.Id);
	return !isDuplicate;
};

<FormField {...easyForm("Name", {validate: isUniqueName})} />
```

### Overriding the error translation key
Pass `errorTranslationId` directly to `FormField` when the validator needs a specific message:

```tsx
<FormField {...easyForm("ZoneId", {validate: isUniqueZoneId})} errorTranslationId={duplicateIdError} />
```

---

## Submission

Pass `handleSubmit(onValid)` to `SidePanel`'s `buttonRowProps.onSave` or call it directly:

```tsx
const handleSave = handleSubmit((data: Room) => {
	save({...room, ...data});
});

return (
	<SidePanel buttonRowProps={{onSave: handleSave, forwardActionDisabled: false}}>
		{/* fields */}
	</SidePanel>
);
```

For multiple forms on one page, call each `handleSubmit` and accumulate failures:

```tsx
const handleSave = async (e: BaseSyntheticEvent) => {
	e.preventDefault();
	let failures = 0;

	await form1.handleSubmit(
		(data) => { merged.Field1 = data.Field1; },
		() => failures++
	)(e);

	await form2.handleSubmit(
		(data) => { merged.Field2 = data.Field2; },
		() => failures++
	)(e);

	if (!failures) save(merged);
};
```

---

## Keeping Forms in Sync

Call `reset(newValues)` whenever the underlying data changes (e.g. when a different item is selected):

```tsx
useEffect(() => {
	reset(selectedRoom);
}, [selectedRoom]);
```

This is the correct use of `useEffect` here — it is synchronising react-hook-form's internal library state with new external data, not deriving component state. It does not violate the "do not sync derived values into state with useEffect" rule, which applies to `useState` values computed from other state or props.

Use `setValue(fieldName, value)` to programmatically update a single field, and `watch(fieldName)` to reactively read a field value in the render.

---

## Nested Fields

Use dot notation for fields inside arrays or nested objects:

```tsx
{presets.map((preset, index) => (
	<FormField
		key={preset.Id}
		{...easyForm(`Presets.${index}.Name` as keyof FormValues)}
	/>
))}
```

---

## What NOT to Do

- Do NOT use react-hook-form's `useForm` directly — always use `useEasyForm` so the translation prefix is consistent.
- Do NOT store form state in component state with `useState` and sync it with `useEffect` — let react-hook-form own the form state.
- Do NOT call `handleSubmit` inside a `useEffect` — call it from an event handler.
