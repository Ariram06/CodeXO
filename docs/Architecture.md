# Codexo — Architecture

> **Status:** NOT IMPLEMENTED (stub created in M0)

## Overview

```
Chrome Extension (MV3)
├── Content Script   → extracts problem from page
├── Service Worker   → coordinates messaging
└── Side Panel (Angular) → user-facing hint UI
        │
        ▼
ASP.NET Core Web API
├── Codexo.Api            → controllers, validation, middleware
├── Codexo.Core           → entities, hint logic, prompt building
└── Codexo.Infrastructure → EF Core, AI client, auth
        │
        ▼
SQL Server (LocalDB for dev)
```

## Dependency Direction

`Api → Infrastructure → Core`

Core never references EF Core, ASP.NET HTTP, SQL Server, browser APIs, or AI provider implementations.

## Key Design Decisions

*To be documented as modules are built.*
