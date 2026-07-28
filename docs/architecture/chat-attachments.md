# Chat attachments (PDF, Excel, and more)

## Dummy section (start here)

Attaching a file is like handing a coworker a **paper from your stack** before you ask a question.

You do **not** photocopy the whole PDF into the sticky note (the chat message). You put the paper on the desk and write on the sticky note: “see `design.pdf` over there.” The coworker (Cursor/Codex) already knows how to open PDFs and Excel files if they can find them.

```
You pick design.pdf / budget.xlsx
        ↓
Orchi keeps the bytes on disk (.bin → workspace copy)
        ↓
SQLite remembers: name (with .pdf/.xlsx), MIME, size, path
        ↓
Prompt lists the path → agent opens the real file
```

**Aha:** Extension lives on the file name; content is a byte stream on disk — **not** a `byte[]` column in SQLite. That is intentional: large Excel/PDF blobs would bloat the DB, and the agent needs a real workspace path anyway.

| Papers analogy | Orchi |
|----------------|-------|
| Paper on the desk | `{workspace}/.orchi/attachments/{id}/design.pdf` |
| Sticky note metadata | `ChatMessageAttachments` (FileName, ContentType, SizeBytes, …) |
| Staging tray | `%LocalApplicationData%/Orchi/attachment-blobs/staged/…/{id}.bin` |
| Kind of paper (PDF vs sheet) | Derived `AttachmentKind` (`pdf`, `spreadsheet`, …) for UI icons |

Everything below is the same idea with API and desktop details.

---

## Storage model (extension + bytes — without SQLite blobs)

| Concern | Where it lives |
|---------|----------------|
| Extension | End of `FileName` (e.g. `budget.xlsx`) |
| Raw file bytes | Staged `.bin` under the attachment blob root, then mirrored into the workspace with the original name |
| MIME | `ContentType` (inferred from extension when the client sends empty / `application/octet-stream`) |
| Kind for UI | Computed `AttachmentKind` — not a DB column |
| Optional text preview | `ExtractedText` for text-like files only (CSV, markdown, …). PDF/Excel are **not** pre-parsed; the agent reads them with tools |

**Why not `byte[]` in SQLite?** It is possible in EF, but Orchi deliberately keeps binaries on disk: 25 MB × many files would inflate the chat DB, and every agent turn still needs a workspace file path for tools/`--image`.

## Primary file types

Any non-empty file under the size limit can upload. First-class UX (MIME inference + icons) covers:

| Kind | Extensions | Example MIME |
|------|------------|--------------|
| `pdf` | `.pdf` | `application/pdf` |
| `spreadsheet` | `.xlsx`, `.xls`, `.xlsm` | OpenXML / `vnd.ms-excel` |
| `csv` | `.csv` | `text/csv` |
| `image` | `.png`, `.jpg`, … | `image/*` |
| `text` | `.txt`, `.md`, `.json`, … | `text/*` |

## Agent delivery

1. Upload stages bytes + metadata
2. Send links staged ids to the user message and mirrors files into `.orchi/attachments/…`
3. `AttachmentContributor` lists each path (and text preview when extracted)
4. Images also get Cursor/Codex `--image` absolute paths

PDF and Excel rely on **path-in-prompt** — no server-side Excel/PDF text extraction in v1.

## Related code

| Area | Path |
|------|------|
| Service | `src/API/Infrastructure/Agents/Attachments/ChatAttachmentService.cs` |
| Kind / MIME helpers | `AttachmentPaths.cs`, `AttachmentKind.cs` |
| API DTO | `Features/Chats/Shared/ChatAttachmentContracts.cs` (`Kind` on `AttachmentResponse`) |
| Desktop icons / accept | `src/desktop/.../lib/chat/attachment-kind.ts`, `attachment-icons.ts` |
