# Event scripting

## Dummy section (start here)

Think of Orchi scripts like **hooks on a workshop door**.

- When a worker (agent) **walks in** (start) or **walks out** (finish), the door can trigger a checklist.
- The checklist can say “run the lint blower” or “file the paperwork with GitHub / Azure.”
- You choose whether that checklist is for **everyone** (global) or **this project only**, and optionally only for certain job roles (modes: implementation, review, …).

```
Agent turn starts → matching scripts → CLI adapter runs → turn ends → matching scripts → orchestration continues
```

**Aha:** git commit/push/PR are not a separate product — they are **steps on those checklists**, using local `git` plus a host adapter (`gh` or `az`).

| Analogy | Orchi |
|---------|--------|
| Door events | `AgentStart` / `AgentFinish` |
| Job role filter | Mode filter (`implementation`, `review`, …) |
| Checklist | `Script` + `ScriptBinding` |
| Checklist steps | `IScriptActionStrategy` (`shell`, `git.commit`, …) |
| City hall (PR) | `IGitHostingFacade` → GitHub / Azure DevOps adapters |

Everything below is the same idea with types and file paths.

---

## Model

- **Events:** `agentStart`, `agentFinish` only. Review/orchestration “finish” = same events with `modeFilter`.
- **Scope:** `ProjectId == null` → global; otherwise project-scoped.
- **Resolution:** project bindings first, then global; ordered by binding `Order`.
- **Steps JSON:** typed kinds — `shell`, `git.commit`, `git.push`, `git.merge`, `git.createPullRequest`, `git.worktree`.

## Key types

| Type | Location |
|------|----------|
| `IScriptStore` / `EfScriptStore` | `Infrastructure/Scripts/` |
| `IScriptEventDispatcher` | `Infrastructure/Scripts/ScriptEventDispatcher.cs` |
| `IScriptActionStrategy` + factory | `Infrastructure/Scripts/Actions/` |
| `IGitWorkspaceService` | `Infrastructure/Git/Workspace/` |
| `IGitHostAdapter` / factory / facade | `Infrastructure/Git/Hosting/` |
| CRUD slices | `Features/Scripts/` |
| Host readiness | `Features/GitHosting/` |

## Lifecycle wiring

`AgentSessionManager.SendMessageAsync`:

1. Status → in progress  
2. Dispatch `AgentStart` scripts (SSE `script` events)  
3. Adapter turn  
4. Dispatch `AgentFinish` scripts  
5. `IAgentTurnCompletionNotifier` (orchestration pipeline)

## Git hosts

Local ops use `git`. PR creation goes through `IGitHostingFacade`:

- `GitHubHostAdapter` → `gh`
- `AzureDevOpsHostAdapter` → `az`

Project setting `GitHostProvider` selects the adapter. Readiness must be `ready` before PR steps run.

## Default orchestration template

`POST /scripts/templates/orchestration-git-defaults` creates two scripts:

1. **AgentStart** — `git.worktree` (any mode). Creates a worktree from the project pattern, registers a workspace, and switches the chat onto it before the CLI runs. Skips if the chat is already on a worktree.
2. **AgentFinish** (implementation) — `git.commit` (generated message) → `git.push` → `git.createPullRequest`

## Worktree branch pattern

Project field `DefaultWorktreeBranchPattern` (default `orchi/{date}-{shortId}`) controls the branch name when:

- An AgentStart `git.worktree` step omits `branch`
- Manual `POST /projects/{id}/worktrees` omits `branchName`
- Plan kickoff provisions a worktree

Tokens: `{date}`, `{time}`, `{shortId}`, `{chatId}`, `{mode}`.

Plan kickoff with `UseWorktreeOnKickoff` provisions `WorkspaceKind.Worktree` under `.orchi/worktrees/{planId}` from `DefaultBaseBranch`.
