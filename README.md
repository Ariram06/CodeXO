# Codexo

A Chrome extension that reads DSA problems and provides progressive, problem-specific hints — never the full solution. The goal is to help users learn to solve the **next** problem on their own.

## Principles

- **Learning over answers** — teach problem-solving, not pattern-matching
- **Minimum effective hint** — give the least information needed to unblock
- **Progressive disclosure** — five hint levels from understanding to deep analysis
- **Problem-specific** — never generic advice when context exists
- **Spoiler-aware** — no algorithm names at early levels, no code at any level
- **Graceful failure** — never a blank screen

## Tech Stack

- **Backend:** ASP.NET Core Web API (.NET 10), EF Core, SQL Server
- **AI:** Provider-independent via `IAiClient` (Microsoft.Extensions.AI)
- **Side Panel:** Angular (standalone components, Signals, Angular Material)
- **Extension:** Chrome Manifest V3, Side Panel API

## Project Structure

```
Codexo/
├── src/
│   ├── Codexo.Api/            — ASP.NET Core Web API
│   ├── Codexo.Core/           — Domain logic, entities, hint engine
│   └── Codexo.Infrastructure/ — EF Core, AI client, auth
├── tests/
│   ├── Codexo.Core.Tests/     — Unit tests
│   └── Codexo.Api.IntegrationTests/ — Integration tests
├── extension/                 — Chrome extension + Angular side panel
└── docs/                      — Project documentation
```

## Getting Started

### Backend
```bash
dotnet build
dotnet test
```

### Extension
```bash
cd extension
npm install
npm run build
```
Then load the `extension/dist/codexo` folder as an unpacked extension in Chrome.

## Status

See [docs/LEARNING-LOG.md](docs/LEARNING-LOG.md) for module-by-module progress.
