# Per-mode default model settings

## Summary
Add user-configurable default models for each agent mode (Default, Implementation, Orchestration, Review) so new chats and kicked-off child chats start with the right model. `null` continues to mean “Default (CLI)” — Cursor auto/default with no `--model` flag.

## Goal
Users can open Settings, pick a default model per mode (including hidden Implementation), and have that model applied automatically when:
- a chat is created in that mode (today: always `default` on create),
- an implementation child is kicked off from orchestration,
- a review child is kicked off after implementation.

Per-chat overrides via the existing chat model selector remain unchanged.

## Scope
- Persist per-agent, per-mode default model selections in SQLite.
- Expose GET + PATCH API under `/agents/{agentId}/mode-model-defaults`.
- Resolve defaults in `AgentSessionManager.CreateSessionAsync` when no explicit `modelId` is passed.
- Change kick-off handlers to stop inheriting the parent orchestration chat’s model; use the Implementation / Review mode defaults instead.
- Add a Settings card with one selector per mode (4 rows), reusing enabled-model catalog data.
- Integration/unit tests for API resolution and kick-off behavior.

## Out of scope
- Changing a chat’s model when the user switches mode on an existing chat (`UpdateChatMode`).
- Clearing mode defaults when a catalog model is disabled or removed (document behavior; optional follow-up).
- Per-agent support beyond `cursor` in the desktop UI (follow existing `AgentModelsCard` pattern).
- Codex or other agent adapters beyond what the catalog already supports.

## Affected files

### Files to add
- `src/API/Entities/AgentModeModelDefault.cs` — entity keyed by `(AgentId, Mode)` with nullable `ModelId`.
- `src/API/Infrastructure/Agents/Persistence/IAgentModeModelDefaultStore.cs` — list/get/upsert for mode defaults.
- `src/API/Infrastructure/Agents/Persistence/EfAgentModeModelDefaultStore.cs` — EF implementation.
- `src/API/Infrastructure/Agents/Models/IAgentModeModelDefaultService.cs` — validation + resolution API.
- `src/API/Infrastructure/Agents/Models/AgentModeModelDefaultService.cs` — validates mode ids, enabled models; resolves default for a mode.
- `src/API/Features/Agents/ListAgentModeModelDefaults/ListAgentModeModelDefaults.cs` — `GET /agents/{agentId}/mode-model-defaults`.
- `src/API/Features/Agents/UpdateAgentModeModelDefault/UpdateAgentModeModelDefault.cs` — `PATCH /agents/{agentId}/mode-model-defaults/{mode}`.
- `src/API/Migrations/{timestamp}_AddAgentModeModelDefaults.cs` — migration (generated).
- `src/API/Migrations/{timestamp}_AddAgentModeModelDefaults.Designer.cs` — migration designer (generated).
- `tests/Orchi.Api.Tests/Infrastructure/Agents/Models/AgentModeModelDefaultServiceTests.cs` — unit tests for resolution/validation.
- `tests/Orchi.Api.Tests/Integration/AgentModeModelDefaultsEndpointTests.cs` — GET/PATCH integration tests.
- `tests/Orchi.Api.Tests/Integration/CreateChatModeModelDefaultTests.cs` — create chat applies mode default.
- `src/desktop/src/renderer/src/lib/chat/agent-mode-model-defaults-api.ts` — client for list/update endpoints.
- `src/desktop/src/renderer/src/components/settings/agent-mode-model-defaults-card.tsx` — Settings UI card.
- `.orchi/plan-mode-model-defaults.md` — this plan file (delete when done).

### Files to modify
- `src/API/Data/AppDbContext.cs` — register `DbSet<AgentModeModelDefault>`, configure composite PK `(AgentId, Mode)`.
- `src/API/Migrations/AppDbContextModelSnapshot.cs` — updated by migration.
- `src/API/Infrastructure/Agents/AgentsExtensions.cs` — register store + service.
- `src/API/Infrastructure/Agents/AgentSessionManager.cs` — inject service; when `modelId` param is null, resolve via `GetDefaultModelIdAsync(agentId, resolvedMode)` before create.
- `src/API/Features/Chats/KickOffPlan/KickOffPlan.cs` — remove `modelId: parent.ModelId`; pass `modelId: null` so Implementation mode default applies.
- `src/API/Features/Chats/KickOffReview/KickOffReview.cs` — remove `modelId: parent.ModelId`; pass `modelId: null` so Review mode default applies.
- `src/desktop/src/renderer/src/routes/_app/settings.tsx` — render `AgentModeModelDefaultsCard` below `AgentModelsCard`.
- `src/desktop/src/renderer/src/lib/chat/types.ts` — add `AgentModeModelDefault`, list/update response types.

### Files to delete
- None (except this plan file after completion).

## Expected changes

### Data model
```csharp
// AgentModeModelDefault
AgentId (string, PK part)
Mode (string, PK part) — one of AgentModeIds.*
ModelId (string?, nullable) — null = CLI default
UpdatedAt (DateTimeOffset)
```

No seed rows required. Missing row = CLI default for that mode.

### API contract

**GET** `/agents/{agentId}/mode-model-defaults`

Response:
```json
{
  "defaults": [
    { "mode": "default", "label": "Default", "modelId": null },
    { "mode": "implementation", "label": "Implementation", "modelId": "gpt-5.3-codex" },
    { "mode": "orchestration", "label": "Orchestration", "modelId": null },
    { "mode": "review", "label": "Review", "modelId": "claude-4.6-sonnet-medium-thinking" }
  ]
}
```

Always return all four modes from registered `IAgentModeStrategy` instances (include Implementation even though it is hidden from `/agents/modes`). Order: Default, Orchestration, Review, Implementation (or alphabetical by label — pick one and keep consistent).

**PATCH** `/agents/{agentId}/mode-model-defaults/{mode}`

Request:
```json
{ "modelId": "gpt-5.3-codex" }
```
or `{ "modelId": null }` for CLI default.

Validation:
- `mode` must match a registered strategy (`AgentModeIds`).
- Non-null `modelId` must exist and be enabled in the agent model catalog (`IsEnabledModelAsync`).
- Unsupported agent → same validation as other agent endpoints.

### Resolution logic (`AgentSessionManager.CreateSessionAsync`)
1. If caller passes non-null `modelId` → validate enabled, use it (unchanged).
2. If caller passes null → `resolvedModelId = await modeDefaultService.ResolveAsync(agentId, resolvedMode)`.
3. Persist `resolvedModelId` on the chat (may still be null → no `--model` CLI arg, existing `BuildCliArgs` behavior).

### Kick-off behavior change
Today `KickOffPlan` / `KickOffReview` pass `modelId: parent.ModelId`, so implementation/review children inherit the orchestration parent’s per-chat model. Change both to omit explicit model (pass null) so each child uses its mode’s configured default instead.

### Settings UI
New card **“Mode default models”** (or similar):
- Short description: defaults apply to new chats in that mode and to kicked-off implementation/review children; per-chat selector still overrides.
- Four rows, one per mode, each with a dropdown matching `ChatModelSelector` options:
  - First option: **Default (CLI)** → sends `modelId: null`
  - Remaining options: enabled models from `listAgentModels(agentId, false)`
- Save immediately on change (mirror `AgentModelsCard` toggle UX), with inline error text on failure.
- Implementation row label should note it applies to kicked-off plan agents (hidden from chat mode picker).

Reuse `DEFAULT_MODEL_VALUE` pattern from `chat-model-selector.tsx` rather than duplicating magic strings.

## Tasks
- [ ] Add `AgentModeModelDefault` entity, EF config, migration, and store.
- [ ] Implement `AgentModeModelDefaultService` (list all modes with labels, upsert, resolve).
- [ ] Add `ListAgentModeModelDefaults` and `UpdateAgentModeModelDefault` endpoints; register in DI.
- [ ] Wire resolution into `AgentSessionManager.CreateSessionAsync`.
- [ ] Update `KickOffPlan` and `KickOffReview` to use mode defaults instead of parent model inheritance.
- [ ] Add backend unit + integration tests (GET/PATCH, create chat resolution, kick-off child model).
- [ ] Add `agent-mode-model-defaults-api.ts` and types.
- [ ] Build `AgentModeModelDefaultsCard` and add it to Settings.
- [ ] Run `dotnet test` and desktop tests/lint for touched files.
- [ ] Delete this plan file after implementation is complete and validated

## Implementation notes

### Existing patterns to follow
- Agent model catalog: `src/API/Features/Agents/ListAgentModels/`, `UpdateAgentModel/`, `EfAgentModelStore.cs`.
- Settings UI: `src/desktop/src/renderer/src/components/settings/agent-models-card.tsx`.
- Chat model dropdown: `src/desktop/src/renderer/src/components/chat/chat-model-selector.tsx`.
- Mode ids: `src/API/Infrastructure/Agents/Modes/AgentModeIds.cs`.
- Mode labels: `IAgentModeStrategy.DisplayLabel` (Implementation label already exists).

### Service design
```csharp
Task<IReadOnlyList<AgentModeModelDefaultDto>> ListAsync(string agentId, CT);
Task<Result<AgentModeModelDefaultDto>> UpdateAsync(string agentId, string mode, string? modelId, CT);
Task<string?> ResolveAsync(string agentId, string mode, CT); // null = CLI default
```

`ListAsync` joins strategies with stored rows (left join — absent row → `modelId: null`).

Optional: add cache key `agent-mode-model-defaults:{agentId}` in `OrchiCacheKeys.cs` and invalidate on PATCH. Not required for v1 if reads are cheap.

### Edge cases
- User sets Implementation default to Codex, Orchestration default to CLI auto — kick-off creates child with Codex even if parent orchestration chat uses a different per-chat model.
- New chat created via `POST /chats` with `mode: "default"` and no model → uses Default mode setting.
- Explicit `modelId` on `CreateSessionAsync` (if any internal callers besides kick-off) still wins.
- Disabled model in a saved default: PATCH already prevents setting disabled models; if a model is later disabled, resolution should still validate at create time via `IsEnabledModelAsync` — return validation error or fall back to null. **Prefer validation error** (`Model.Unsupported`) so the user fixes settings; do not silently fall back.

### Frontend query keys
- `['agent-mode-model-defaults', agentId]` — invalidate after PATCH and when agent model catalog changes (optional cross-invalidate).

## Dependencies and sequencing
No cross-plan coordination required — this is a single plan.

Run the EF migration before integration tests. Frontend work can proceed once PATCH/GET endpoints exist.

## Validation
1. **Migration**: `dotnet ef database update` (or test factory auto-migrate) succeeds.
2. **API tests**:
   - GET returns 4 modes with null defaults initially.
   - PATCH with enabled model persists; PATCH with disabled/unknown model fails.
   - Create chat with Default mode setting = `claude-*` → response `modelId` matches.
   - KickOffPlan child gets Implementation mode default, not parent model.
   - KickOffReview child gets Review mode default.
3. **Manual desktop**:
   - Settings → set Orchestration = CLI default, Implementation = Codex (or any enabled slug).
   - Create orchestration chat → model selector shows CLI default unless overridden.
   - Kick off plan → implementation child uses Codex.
   - Per-chat model override on parent does not affect kicked-off child.
4. **Build**: `dotnet test tests/Orchi.Api.Tests` and desktop unit tests for any new TS helpers.

Confirm `.orchi/plan-mode-model-defaults.md` is deleted after successful validation.

## Handoff notes
- Agent model catalog work is already in the branch (`AgentModel` entity, settings card, `UpdateChatModel`, chat model selector). This plan builds on that — do not reimplement catalog sync.
- `Chat.ModelId` null = CLI default at runtime (`BuildCliArgs` skips `--model`). Mode defaults stored as null use the same semantics.
- Implementation mode is intentionally excluded from `/agents/modes` and the chat mode selector, but **must appear** in the new settings card.
- Do not change `UpdateChatMode` to auto-switch models unless product asks — that would surprise users who tuned a chat manually.
- Delete `.orchi/plan-mode-model-defaults.md` when done; keep it if blocked on unresolved decisions.