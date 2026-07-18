# Review brief for plan chat-git-workspace-ui

## Original implementation plan

# Chat Git Workspace UI

## Summary
Add a split-button git actions control to the chat workspace header (alongside Open in editor / Close), defaulting to **Commit and push** with a chevron dropdown for **Commit** and **Create pull request**. Wire confirmation/input dialogs and API calls with loading, disabled, and error states.

## Goal
Users can trigger commit / commit+push / create PR from the active chat header without waiting for AgentFinish scripts. Primary button opens the appropriate dialog and executes the default `commitAndPush` flow; dropdown exposes the other two actions.

## Scope
- New git actions split-button component modeled after `OpenInEditorMenu`.
- Dialogs for commit message entry and PR metadata entry.
- Client API module + types + query keys.
- Header wiring from `ChatWorkspacePanel` through `ChatWorkspaceHeader`.
- Lightweight unit tests for action-label/disabled logic if extracted to pure helpers.

## Out of scope
- Backend endpoint implementation (depends on `chat-git-actions-api`).
- Settings page changes.
- Keyboard shortcuts (unless trivially added — skip for now).
- Running git commands locally in Electron main process (all via API).

## Affected files

### Files to add
- `.orchi/plan-chat-git-workspace-ui.md` — this plan file (created by kickoff orchestration).
- `src/desktop/src/renderer/src/components/layout/chat-git-actions-menu.tsx` — split button + dropdown + dialog orchestration.
- `src/desktop/src/renderer/src/components/layout/git-commit-dialog.tsx` — commit message dialog (used for both Commit and Commit and push).
- `src/desktop/src/renderer/src/components/layout/git-pull-request-dialog.tsx` — PR title/body/target branch dialog.
- `src/desktop/src/renderer/src/lib/git/api.ts` — `runChatGitAction`, `getSuggestedCommitMessage`.
- `src/desktop/src/renderer/src/lib/git/types.ts` — action kinds + response DTO types.
- `src/desktop/src/renderer/src/lib/git/git-action-labels.ts` — pure helpers for labels/disabled reasons (optional but preferred for testability).
- `src/desktop/src/renderer/src/lib/git/git-action-labels.test.ts` — tests for helpers (if extracted).

### Files to modify
- `src/desktop/src/renderer/src/components/layout/chat-workspace-header.tsx` — render `ChatGitActionsMenu` in `endContent` after `OpenInEditorMenu`.
- `src/desktop/src/renderer/src/components/layout/chat-workspace-panel.tsx` — pass `chatId`, `projectId`, `workspacePath`, branch/base branch, and project git settings into header.
- `src/desktop/src/renderer/src/lib/query-keys.ts` — add `gitActionKeys` factory (`suggestedCommitMessage(chatId)`, etc.).

### Files to delete
- None

## Expected changes

### Split-button UX (`chat-git-actions-menu.tsx`)
Mirror `open-in-editor-menu.tsx` styling:
- Primary button label: **Commit and push**
- Adjacent chevron dropdown with:
  - **Commit**
  - **Create pull request**
- Use `inline-flex -space-x-px`, `rounded-r-none` / `rounded-l-none`, `h-8`, outline variant.
- Primary click → open commit dialog in `commitAndPush` mode.
- Dropdown items → open commit dialog in `commit` mode, or PR dialog respectively.
- Show inline error text below control (same pattern as Open in editor) and `toast.success` / `toast.error` from `sonner` on completion.

### Disabled / readiness rules
- Disable entire control when `workspacePath` is empty.
- For **Create pull request** menu item: disable when `projectId` is null; optionally prefetch `getGitHostReadiness({ projectId, workspacePath, provider })` and disable with tooltip/title when status !== `ready` (follow `git-settings-card.tsx` messaging).
- Do not disable during chat send unless it simplifies race avoidance — optional `disabled={isSending}` is acceptable but not required.

### `git-commit-dialog.tsx`
- Props: `open`, `onOpenChange`, `mode: 'commit' | 'commitAndPush'`, `chatId`, `onSuccess`.
- On open: fetch suggested message via React Query (`gitActionKeys.suggestedCommitMessage(chatId)`) calling `GET /chats/{chatId}/git/suggested-commit-message`.
- Textarea prefilled with suggestion; user can edit.
- Checkbox **Generate from diff on submit** (default checked) — when checked, send `generateCommitMessage: true` and omit manual message; when unchecked, require non-empty textarea.
- Primary CTA label: `Commit` or `Commit and push` based on mode.
- Submit mutation calls `runChatGitAction` with appropriate `action`.

### `git-pull-request-dialog.tsx`
- Props: `open`, `onOpenChange`, `chatId`, `projectId`, `workspacePath`, `defaultBaseBranch`, `gitHostProvider`, `headBranch?`, `onSuccess`.
- Fields: Title (default `Orchi: {headBranch}` or similar), Body (default short Orchi message), Target branch (default project `defaultBaseBranch`).
- Show readiness warning banner when host not ready (reuse `getGitHostReadiness`).
- Submit calls `runChatGitAction({ action: 'createPullRequest', ... })`.
- On success with `pullRequestUrl`, toast with message and use `window.open(pullRequestUrl, '_blank')` if available.

### Client API (`lib/git/api.ts`, `types.ts`)
Follow `docs/frontend/api-conventions.md`:
- `getApiBaseUrl()`, `readErrorMessage()`.
- Map JSON to typed responses at bottom of `api.ts`.
- Throw `Error` on failure.

### Header wiring
Extend `ChatWorkspaceHeaderProps` with git props (`chatId`, `projectId`, `defaultBaseBranch`, `gitHostProvider`, `workspaceBranch`).
In `chat-workspace-panel.tsx`, derive from `chat` + matching `projects` entry:
```typescript
const project = projects.find((p) => p.id === chat.projectId)
const workspace = project?.workspaces.find((w) => w.id === chat.workspaceId)
```

### Visual consistency
Follow `.cursor/skills/orchi-ui-theme/SKILL.md`: zinc tokens, `text-sm`, lucide icons (`GitCommit` or `Upload`/`GitBranch` — pick one consistent icon for primary button).

## Tasks
- [ ] Add `lib/git/types.ts` and `lib/git/api.ts`
- [ ] Add `gitActionKeys` to `query-keys.ts`
- [ ] Implement `git-commit-dialog.tsx` and `git-pull-request-dialog.tsx`
- [ ] Implement `chat-git-actions-menu.tsx` split button
- [ ] Wire props through `chat-workspace-panel.tsx` and `chat-workspace-header.tsx`
- [ ] Add helper tests if logic extracted
- [ ] Delete this plan file after implementation is complete and validated

## Implementation notes
- Import via `@/` alias only.
- Keep components in `components/layout/` next to `open-in-editor-menu.tsx`.
- Use existing `Dialog`, `Button`, `DropdownMenu`, `ButtonGroup` primitives.
- Mutations: `@tanstack/react-query` `useMutation` inside menu/dialog components (consistent with settings cards).
- Do not inline query key arrays in components.
- Primary default action is **Commit and push**, not Commit alone — matches user request.

## Dependencies and sequencing
**Depends on `chat-git-actions-api`** — endpoints must exist before UI integration testing.

## Validation
```bash
cd src/desktop
npm run typecheck
npm run lint
npm run test
```
Manual:
1. Open a chat with a git workspace and uncommitted changes.
2. Click **Commit and push** → dialog → confirm → changes committed/pushed (or clear error toast).
3. Dropdown **Commit** → commits without push.
4. Dropdown **Create pull request** → PR dialog → submit → URL toast/open (when `gh`/`az` ready).
5. Control disabled when workspace path missing.

Confirm the plan file has been deleted after successful implementation.

## Handoff notes
- Match Open-in-editor split-button layout exactly for visual consistency.
- API action strings: `commit`, `commitAndPush`, `createPullRequest`.
- If API returns multi-step results, toast the last successful output or first error message.
- Remind implementation agent to delete `.orchi/plan-chat-git-workspace-ui.md` when done; keep the plan file if blocked.

## Implementation chat

Chat ID: `f85d05a2-2184-4e7f-ab45-eccbf66254d8`

## Parent orchestration chat

Chat ID: `129d5578-d3c7-4524-812d-853c9997f507`

## Instructions

Review the implementation against the original plan above using the git diff injected into your prompt context.
Produce one or more actionable review plans for the reviewer.