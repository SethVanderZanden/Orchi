# Review brief for plan chat-git-actions-api

## Original implementation plan

# Chat Git Actions API

## Summary
Add chat-scoped HTTP endpoints so the desktop app can run **Commit**, **Commit and Push**, and **Create Pull Request** manually from the chat workspace header. Reuse the existing script action strategies (`git.commit`, `git.push`, `git.createPullRequest`) and commit-message generator rather than duplicating git logic.

## Goal
Implement `POST /chats/{chatId}/git/actions` and `GET /chats/{chatId}/git/suggested-commit-message` that resolve chat/project/workspace context and execute the requested git workflow, returning structured step results (including PR URL when applicable).

## Scope
- New infrastructure service to build `ScriptActionContext` from a chat session and run one or more git script steps sequentially.
- Two new chat feature endpoints with request/response contracts.
- DI registration and API integration tests for happy-path and validation failures.

## Out of scope
- Desktop UI components or client API wrappers.
- New git CLI/host adapter logic (reuse `IGitWorkspaceService`, `IGitHostingFacade`, existing strategies).
- Automatic script binding changes or AgentStart/AgentFinish behavior.
- EF migrations.

## Affected files

### Files to add
- `.orchi/plan-chat-git-actions-api.md` — this plan file (created by kickoff orchestration).
- `src/API/Infrastructure/Git/ChatGitActionRunner.cs` — orchestrates step execution for manual chat git actions; builds context from session + project/workspace metadata.
- `src/API/Infrastructure/Git/IChatGitActionRunner.cs` — interface for the runner.
- `src/API/Features/Chats/RunGitAction/RunGitAction.cs` — `POST /chats/{chatId}/git/actions` endpoint + handler.
- `src/API/Features/Chats/GetSuggestedCommitMessage/GetSuggestedCommitMessage.cs` — `GET /chats/{chatId}/git/suggested-commit-message` endpoint + handler.
- `tests/Orchi.Api.Tests/Features/Chats/ChatGitActionEndpointTests.cs` — integration tests.

### Files to modify
- `src/API/Features/Chats/Shared/ChatContracts.cs` — add shared request/response records for git action API (`RunGitActionRequest`, `RunGitActionResponse`, `GitActionStepResult`, `SuggestedCommitMessageResponse`, and a `GitActionKind` enum or string constants: `commit`, `commitAndPush`, `createPullRequest`).
- `src/API/Infrastructure/Agents/AgentsExtensions.cs` — register `IChatGitActionRunner` in DI.

### Files to delete
- None

## Expected changes

### `IChatGitActionRunner` / `ChatGitActionRunner`
- Accept `chatId`, action kind, and optional overrides (`CommitMessage`, `GenerateCommitMessage`, `PullRequestTitle`, `PullRequestBody`, `TargetBranch`).
- Load chat session via `AgentSessionManager` (or `IChatStore` + session load pattern used elsewhere — follow `UpdateChatWorkspace` / `AgentSessionManager.GetRequiredSessionAsync`).
- Build context mirroring `AgentSessionManager.BuildScriptDispatchContextAsync`:
  - `WorkspacePath`, `ProjectId`, `WorkspaceId`, `Branch`, `BaseBranch`, `GitHost` snapshot from project.
- Map actions to step sequences:
  - `commit` → `git.commit`
  - `commitAndPush` → `git.commit` then `git.push`
  - `createPullRequest` → optionally `git.commit` + `git.push` first **only if** request includes `commitFirst: true`? **No** — keep actions independent: `createPullRequest` runs only `git.createPullRequest` (matching script step semantics). UI can chain separately if needed later.
- For commit steps:
  - Honor `GenerateCommitMessage` (default `true` when message empty) using `IGitCommitMessageGenerator`.
  - Pass `Message`/`GenerateMessage` into `ScriptStepDto`.
- For PR step:
  - Pass `Title`, `Body`, `TargetBranch` into `ScriptStepDto`; fall back to strategy defaults when omitted.
- Before PR: optionally validate host readiness via `IGitHostingFacade.GetReadinessAsync` and return validation error if not `Ready`.
- Validate workspace is inside a git repo via `IGitWorkspaceService.IsGitRepositoryAsync`.
- Execute steps via `IScriptActionStrategyFactory`; stop on first failure; collect per-step `ScriptActionResult`.
- Return aggregated response with `Succeeded`, `Steps[]`, and `PullRequestUrl` when PR step succeeds.

### `GetSuggestedCommitMessage`
- Resolve chat workspace path from session.
- Call `IGitCommitMessageGenerator.GenerateAsync`.
- Return `{ message: string | null }` (null when no changes / generation failed).

### `RunGitAction` endpoint
- Route: `POST /chats/{chatId:guid}/git/actions`
- Body example:
  ```json
  {
    "action": "commitAndPush",
    "commitMessage": null,
    "generateCommitMessage": true,
    "pullRequestTitle": null,
    "pullRequestBody": null,
    "targetBranch": null
  }
  ```
- Response example:
  ```json
  {
    "succeeded": true,
    "steps": [
      { "label": "Committing changes", "output": "Committed with message: ...", "succeeded": true },
      { "label": "Pushing branch", "output": "Push completed.", "succeeded": true }
    ],
    "pullRequestUrl": null
  }
  ```
- Use existing `ICommand`/`IQuery` + `Result`/`ToProblem()` patterns (see `UpdateChatWorkspace`, `GetGitHostReadiness`).
- FluentValidation: require non-empty `chatId`, valid `action` enum/string.

### Tests
- Create temp git repo in test setup (`git init`, add/commit initial file, modify file).
- Create project + chat pointing at repo path (use `ProjectTestHelper`).
- `GET suggested-commit-message` returns non-null after modification.
- `POST commit` succeeds and working tree clean.
- `POST commitAndPush` — if push cannot run in CI, assert commit step succeeds and push failure returns structured error (or skip push assertion when no remote; document in test).
- `POST createPullRequest` without authenticated `gh`/`az` returns validation/readiness failure (not 500).
- Unknown chat → 404.

## Tasks
- [ ] Add `IChatGitActionRunner` + `ChatGitActionRunner` reusing script strategies and commit message generator
- [ ] Add shared git-action contracts to `ChatContracts.cs`
- [ ] Implement `GetSuggestedCommitMessage` endpoint
- [ ] Implement `RunGitAction` endpoint
- [ ] Register runner in `AgentsExtensions.cs`
- [ ] Add `ChatGitActionEndpointTests.cs`
- [ ] Delete this plan file after implementation is complete and validated

## Implementation notes
- Follow vertical-slice layout under `Features/Chats/{ActionName}/`.
- Do **not** invoke `IScriptEventDispatcher` — run individual strategies directly to avoid script-binding side effects.
- `GitCommitScriptActionStrategy` no-ops when working tree clean; surface that as success with informative output rather than error.
- PR creation requires `session.ProjectId` and project `GitHostProvider`; return `400` with clear message if project missing.
- Known repo build caveat: missing `Artifacts/` folder may block compile in some environments — not introduced by this plan.
- Follow never-nesting / guard-clause style for C# handlers.

## Dependencies and sequencing
This plan must complete before `chat-git-workspace-ui`. No other cross-plan coordination.

## Validation
```bash
dotnet build src/API
dotnet test tests/Orchi.Api.Tests --filter "FullyQualifiedName~ChatGitAction"
```
- Confirm endpoints appear in Scalar at `/scalar/v1`.
- Manual smoke (if local git available): POST commitAndPush against a chat with dirty worktree.

Confirm the plan file has been deleted after successful implementation.

## Handoff notes
- Action string values exposed to the desktop client should be exactly: `commit`, `commitAndPush`, `createPullRequest`.
- Suggested commit message endpoint powers dialog prefill in the UI plan.
- Keep response shape stable; UI will toast step outputs and open PR URL externally.
- Remind implementation agent to delete `.orchi/plan-chat-git-actions-api.md` when done; keep the plan file if blocked.

## Implementation chat

Chat ID: `60c5032b-42db-4aa0-9420-8e7607a68087`

## Parent orchestration chat

Chat ID: `129d5578-d3c7-4524-812d-853c9997f507`

## Instructions

Review the implementation against the original plan above using the git diff injected into your prompt context.
Produce one or more actionable review plans for the reviewer.