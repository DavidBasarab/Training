# Fog Coding Standards

This file defines the coding standards for the Fog codebase.
All code you generate — in any context — must follow the rules for the relevant language below.
The goal is that AI-generated code is indistinguishable from code written by a senior member of this team.

## ⚠️ READ THIS FIRST — Before Any Work

**Before doing anything else in this repository — reading, planning, or writing code — read [`README.md`](README.md) in full.**
It is the single source of truth for what this product is and how it is built. Do not skip it, even for a "small" change.

The README tells you the things you must know before you touch a line of code:
- **Product:** DocLokr — a secure wire-transfer instruction exchange that removes email from the flow and verifies the receiver's identity biometrically. Understanding the domain (Lawyer, Receiver, Lokr, biometric match, sealed locker) is required to name and place code correctly.
- **Tech stack:** **.NET 10 / C# 14** across every C# project. The user-facing apps (`DocLokrSite`, `AdminSite`) are **Blazor WebAssembly** (not React, not MVC, not server-side Blazor), styled with **Tailwind CSS**. The backend is ASP.NET Core (`Brume` cloud API + `Haze` edge nodes) on the **FatCat** toolkit with **MongoDB** persistence.
- **Architecture:** which project owns which responsibility (`Brume` vs `Haze` vs `DocLokr/Business` vs the Blazor sites vs `Common`), so a new endpoint, projector, view, or service lands in the right project.

If anything below in the coding standards appears to conflict with the README, the README describes *what* the system is; these rules describe *how* to write code for it — follow both. Where the README and the actual code disagree, **the code is authoritative** — trust the code and flag the stale README.

The repo contains several .NET 10 C# projects organised by product area: `Brume`, `Common`, `Common.WebServer`, `Haze`, `DocLokr/Business`, `DocLokr/DocLokrCli`, `DocLokrSite`, `DocLokrSite/AdminSite`, `DocLokrSite/DocLokrSite.Common`, with mirrored test projects (`Tests.Brume`, `Tests.Common`, `Tests.Haze`, `Tests.Business`, `Test.DocLokrSite`). All production namespaces start with `Fog.*`; test namespaces start with `Tests.Fog.*` (or `Tests.DocLokr.*` for the DocLokr subtree).

---

## C# Rules

Apply these rules to all C# code. Do not apply them to React, TypeScript, PowerShell, or any other language.

@.claude/rules/csharp/naming-and-structure.md
@.claude/rules/csharp/types-and-di.md
@.claude/rules/csharp/toolchain.md
@.claude/rules/csharp/async.md
@.claude/rules/csharp/errors-and-logging.md
@.claude/rules/csharp/testing.md
@.claude/rules/csharp/not-allowed.md

## PowerShell Rules

Apply these rules to all PowerShell scripts. Do not apply them to C#, React, TypeScript, or any other language.

@.claude/rules/powershell/powershell.md

## TypeScript & React Rules

Apply these rules to all TypeScript and TSX files — both the React frontend (Sites/main) and the Node.js CLI tools (Installer/DataMigration). Do not apply them to C#, PowerShell, or any other language.

@.claude/rules/typescript/naming-and-structure.md
@.claude/rules/typescript/toolchain.md
@.claude/rules/typescript/async.md
@.claude/rules/typescript/react.md
@.claude/rules/typescript/i18n.md
@.claude/rules/typescript/errors.md
@.claude/rules/typescript/performance.md
@.claude/rules/typescript/forms.md
@.claude/rules/typescript/datamigration.md
@.claude/rules/typescript/testing.md
@.claude/rules/typescript/not-allowed.md
