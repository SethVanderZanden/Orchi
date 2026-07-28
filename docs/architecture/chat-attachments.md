# Chat attachments

## Dummy section (start here)

Attaching a file to a chat is like handing a coworker a **folder of papers** before you ask a question.

Today Orchi only sends the spoken question (plain text). The coworker (Cursor/Codex CLI) can already open any file on the desk if you tell them the path — they just never get the papers unless you put them there yourself.

So Orchi needs three simple jobs:

1. **Take the papers** (upload PDF / Excel / CSV / images / any file)
2. **Put them on the desk** where the agent can reach them, and remember which chat turn they belong to
3. **Show them in the room** — chips on the message, plus a right-hand drawer that lists every file in the chat

```
You pick files  →  Orchi saves them  →  Message mentions the paths
                         ↓
              Agent opens them with tools
                         ↓
         UI shows chips + optional Files sidebar
```

**Aha:** We do **not** need a special multimodal protocol for v1. Save the file, put its path in the prompt, and the existing agent already reads it — mode (default / orchestration / review) does not matter.

**Orchi translation:**

| Papers analogy | Orchi |
|----------------|-------|
| Folder on the desk | `{workspace}/.orchi/attachments/{chatId}/…` |
| Sticky note "this came with message 3" | `ChatMessageAttachments` table |
| Handing over the question + folder | Prompt composer lists attachment paths |
| Drawer of all papers in the room | Right Files sidebar (toggle when chat has files) |

Everything below is the same idea with schema, API, UI, and phased delivery.

---

## Current state (gap)

| Layer | Today | Needed |
|-------|-------|--------|
| Schema | `ChatMessages.Content` is plain text only | `ChatMessageAttachments` (+ optional chat-level list) |
| API | `POST /chats/{id}/messages` JSON `{ content }` | Upload + attach; detail responses include attachments |
| Storage | Plan/review markdown via `OrchiArtifactFileStore` | Binary-capable attachment store under `.orchi/attachments/` |
| Agent turn | Prompt is text only | Composer injects absolute (or workspace-relative) paths |
| Desktop | Composer is textarea-only | Attach / drag-drop / paste; message chips; Files sidebar |

Mode is irrelevant: attachments are universal for every chat mode and both Cursor and Codex adapters.

## Design principles

1. **Path-in-prompt, not bytes-in-prompt** — Cursor headless docs: include file paths; the agent reads via tools (images included). Same for PDFs, Excel, CSV, and arbitrary binaries the tools can open.
2. **Persist metadata in SQLite; bytes on disk** — do not put file blobs in the DB.
3. **Workspace-local under `.orchi/`** — mirrors plan/review artifacts; agent cwd is already `WorkspacePath`.
4. **Attach on send, list for the whole chat** — files belong to a user message, but the sidebar shows all attachments for the chat.
5. **Keep SSE for the assistant reply** — uploads are multipart HTTP; streaming stays for tokens/tools/`done`.

## Recommended architecture

### Data model

New entity / table `ChatMessageAttachments` (name can be `ChatMessageAttachment` in EF):

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | PK |
| `ChatId` | `Guid` | Indexed; cascade with chat soft-delete policy (see below) |
| `ChatMessageId` | `Guid` | FK → `ChatMessages`, cascade delete with message |
| `FileName` | `string` (max ~260) | Original display name |
| `ContentType` | `string` (max ~128) | MIME, e.g. `application/pdf` |
| `ByteSize` | `long` | For UI + validation |
| `RelativePath` | `string` | Path under workspace, e.g. `.orchi/attachments/{chatId}/{id}-{safeName}` |
| `Kind` | `string` or enum | Derived for UI: `image` \| `pdf` \| `spreadsheet` \| `csv` \| `other` |
| `CreatedAt` | `DateTimeOffset` | |
| `Ordinal` | `int` | Order within the message |

Optional denormalized `HasAttachments` on `Chat` is **not** required for v1 — derive from attachment count when loading detail / when deciding whether to show the Files toggle.

**Soft-delete:** chats are soft-deleted today. Either cascade soft-hide attachments with the chat query filter, or leave rows and rely on chat filter when joining. Prefer loading attachments only through chat-scoped queries.

**EF:** generate migration with `dotnet ef` only (see `.cursor/skills/ef-migrations/SKILL.md`). Extend `AppDbContext`, `EfChatStore` / mapper, and domain `ChatMessage` with an `Attachments` collection.

### Disk layout

```
{workspace}/
  .orchi/
    attachments/
      {chatId}/
        {attachmentId}-{sanitized-original-name}.pdf
        {attachmentId}-{sanitized-original-name}.xlsx
```

- Reuse the spirit of `OrchiArtifactFileStore`, but add a **binary** store (e.g. `OrchiAttachmentFileStore`) with `WriteAsync(Stream)`, `OpenRead`, `TryDelete`.
- Sanitize file names (strip path segments, control chars); keep extension.
- Document that workspaces should ignore `.orchi/` (repo root already has `/.orchi/`; project templates / README should mention `.orchi/` in workspace gitignores).
- Worktree chats: attachments live on that workspace’s disk (same as plan files).

**Limits (v1 defaults, config via options):**

| Limit | Suggested default |
|-------|-------------------|
| Max file size | 25 MB |
| Max files per message | 10 |
| Max total per chat (soft) | 200 MB or 100 files |
| Allowed types | Any (`*/*`) with primary UX for PDF, Excel (`.xlsx`/`.xls`), CSV, images (`.png`/`.jpg`/`.jpeg`/`.gif`/`.webp`) |

Reject oversize with a clear validation error; do not silently truncate.

### API contracts

Keep send + stream as the primary message path; add a dedicated upload so large files and progress UI stay simple.

#### 1. Upload (staging or immediate)

```
POST /chats/{chatId}/attachments
Content-Type: multipart/form-data
  files: <one or more>
→ 201 AttachmentResponse[]
```

`AttachmentResponse`: `id`, `fileName`, `contentType`, `byteSize`, `kind`, `createdAt`.

**Staging rule for v1:** upload requires an existing chat id. Draft chats on the desktop already promote via `POST /chats` before the first message — upload after promote (same as today’s send flow). Optionally allow upload with a temporary staging folder keyed by draft id later; do not block v1 on that.

Uploaded files without a `ChatMessageId` yet are **pending** rows (nullable `ChatMessageId`) or held only on disk until send. Prefer:

- Write bytes immediately
- Insert row with `ChatMessageId = null` and `ChatId` set
- On send, attach pending ids to the new user message
- Garbage-collect pending rows older than N hours (optional background; or delete on chat delete)

Simpler alternative for v1: **multipart send only** (no pending table) — see Phased delivery. Staging is better UX for large Excel files.

#### 2. Send message (unchanged SSE, richer body)

Keep `POST /chats/{id}/messages` returning SSE. Extend the request:

```csharp
// JSON variant (preferred once files are pre-uploaded)
SendMessageRequest(string Content, Guid[]? AttachmentIds);

// OR multipart variant for one-shot small files
// content + files[]
```

**Recommendation:** JSON `{ content, attachmentIds }` after separate upload. Composer never blocks the SSE parser on multipart boundaries.

Validation: non-empty content **or** at least one attachment; attachment ids must belong to this chat and be unassigned (or already assigned to this new message in the same transaction).

#### 3. List / download

```
GET  /chats/{chatId}/attachments          → AttachmentResponse[] (for sidebar)
GET  /chats/{chatId}/attachments/{id}     → file download (Content-Disposition)
DELETE /chats/{chatId}/attachments/{id}   → optional v1.1
```

`GET /chats/{id}` (`ChatDetailResponse`) should include attachments on each message:

```csharp
ChatMessageResponse(
  Guid Id,
  string Role,
  string Content,
  DateTimeOffset CreatedAt,
  string Status,
  AttachmentResponse[] Attachments);
```

Chat summary can include `int AttachmentCount` later if the Files badge needs it without loading detail.

### Agent / prompt integration

**Mode-agnostic.** Extend `PromptBuildContext` with attachment paths (absolute paths preferred for clarity across worktrees).

Add a small prompt contributor (or extend the message section) that appends something like:

```xml
<attachments>
  <file path="C:/…/.orchi/attachments/{chatId}/….pdf" name="spec.pdf" contentType="application/pdf" />
  …
</attachments>
```

Plus a short rule: “User attached the files below. Read them with your tools before answering when relevant.”

Wire into `AgentPromptComposer.Compose` / `PromptSectionPipeline` so **every** mode strategy inherits it. Do not fork per mode.

**Cursor:** path-in-prompt is enough (official headless guidance). Optional later: pass `--image` for image attachments if we want native image attach without a tool round-trip — not required for v1.

**Codex:** same path references; confirm Codex can read arbitrary workspace paths (it already operates in the workspace).

**Do not** base64-embed files into the prompt.

Stored `ChatMessages.Content` remains the user’s typed text (and maybe a short “Attached: a.pdf, b.csv” suffix only if we want searchable titles — prefer **not** polluting content; search can join attachment file names later).

### Desktop UI

#### Composer

- Paperclip / Attach control on `chat-composer-toolbar`
- Drag-and-drop onto the composer
- Paste image from clipboard → temp file → upload as image attachment
- Pending file chips above the textarea (name, size, remove)
- `onSend: (content: string, attachmentIds: string[]) => void`
- Upload progress per file; disable send while uploads in flight
- Electron: `showOpenDialog` for multi-file (any type); renderer can also use `<input type="file" multiple>`

#### Message list

- User bubbles show attachment chips under text (icon by `kind`, open/download via API URL or Electron shell)
- Image kind: small thumbnail if cheap; click to open

#### Files sidebar (right)

- Toggle button in chat header chrome (only enabled / visible when `attachments.length > 0`, or always visible but empty-state when none)
- Panel lists all chat attachments: name, kind icon, size, which message / time
- Click → preview (images inline; PDF/others open externally or download)
- Reuse existing `ResizablePanel` patterns (`project-shell-layout`, agents sidebar) for a right panel inside the chat workspace
- Persist open/closed preference optionally (`localStorage`, same style as sidebar width)

#### Streaming flow (desktop)

1. If draft → `POST /chats` (existing promote)
2. Upload pending local files → `attachmentIds`
3. Optimistic user message with attachment metadata in TanStack Query cache
4. `POST /chats/{id}/messages` with `{ content, attachmentIds }` + SSE (existing handlers)
5. On `done`, reload detail (attachments already on message)

Update: `lib/chat/types.ts`, `lib/chat/api.ts`, `use-chat-stream.ts`, `message-updates.ts`, `chat-panel.tsx`, composer, message list.

### Security / trust

- Path traversal: resolve under `{workspace}/.orchi/attachments/{chatId}/` only; reject `..`
- Chat ownership: all attachment routes scoped by `chatId` (local single-user app today, still enforce)
- Do not serve files outside the attachment root
- Virus scanning is out of scope for local desktop v1

## Phased delivery

### Phase 1 — Core (ship this first)

1. EF entity + migration (`ChatMessageAttachments`)
2. `OrchiAttachmentFileStore` (binary write/read)
3. `POST /chats/{id}/attachments` + list + download
4. Extend `SendMessageRequest` with `attachmentIds`; prompt contributor lists paths
5. Desktop: attach button, chips, upload-then-send; message chips
6. Tests: store, upload validation, send with attachments, mapper includes attachments

### Phase 2 — Files sidebar

1. Right Files panel + header toggle
2. Chat-scoped list API wired to sidebar
3. Empty / error states (see helpful empty-state skill when implementing)
4. Open-in-OS / download actions

### Phase 3 — Polish

1. Clipboard paste images
2. Drag-and-drop
3. Image thumbnails
4. Delete attachment
5. Pending-upload GC
6. Optional Cursor `--image` for images
7. Search by attachment file name
8. Size/type quotas in settings

## Explicit non-goals (v1)

- Storing blobs in SQLite
- Mode-specific attachment behaviour
- Cloud object storage
- Editing files in-app (view/open only)
- Parsing Excel/PDF server-side into text before the agent (the agent does that)

## Key files to touch (implementation map)

| Area | Paths |
|------|--------|
| Entity / EF | `Entities/ChatMessageAttachment.cs`, `Data/AppDbContext.cs`, Migrations via `dotnet ef` |
| Store | `Infrastructure/Agents/Attachments/OrchiAttachmentFileStore.cs`, chat persistence mapper |
| API | `Features/Chats/UploadAttachment/`, `ListAttachments/`, `DownloadAttachment/`, `SendMessage/`, `Shared/ChatContracts.cs` |
| Prompt | `Modes/Prompt/*` contributor + `PromptBuildContext` |
| Session | `AgentSessionManager.SendMessageAsync`, domain `ChatMessage` |
| Desktop | `components/chat/*`, `lib/chat/*`, `hooks/chat/use-chat-stream.ts`, chat workspace layout |
| Docs | this file; later notes in `docs/frontend/chat-streaming.md` and `docs/agents/cursor-cli.md` |
| Tests | `tests/Orchi.Api.Tests` integration + unit for store/validation |

## Decision summary

| Question | Decision |
|----------|----------|
| Any file type? | Yes; primary UX for PDF, Excel, CSV, images |
| Mode-specific? | No — universal |
| How does the agent see files? | Paths injected into composed prompt; agent tools read them |
| Where are bytes? | `{workspace}/.orchi/attachments/{chatId}/` |
| Where is metadata? | `ChatMessageAttachments` table |
| Upload vs stream? | Multipart upload (or pre-upload) + existing SSE for the reply |
| Sidebar? | Phase 2 right Files panel, toggled from chat chrome when files exist |
