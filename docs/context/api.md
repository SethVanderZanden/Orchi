# SharedContext API

## Dummy section (start here)

These endpoints are the **front desk** for workspace knowledge — check index status, trigger a re-scan, or search the catalog.

---

## Status

**done** — see [PROGRESS.md](PROGRESS.md)

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/workspaces/context?path=` | Workspace index overview |
| `POST` | `/workspaces/index` | Trigger re-index (`{ workspacePath, fullRebuild? }`) |
| `GET` | `/workspaces/search?path=&q=&topK=` | Keyword search over indexed content |

## Implementation

- [`Features/Context/GetWorkspaceContext/`](../../src/API/Features/Context/GetWorkspaceContext/)
- [`Features/Context/IndexWorkspace/`](../../src/API/Features/Context/IndexWorkspace/)
- [`Features/Context/SearchWorkspace/`](../../src/API/Features/Context/SearchWorkspace/)

Desktop UI for context search is deferred (Phase 4).
