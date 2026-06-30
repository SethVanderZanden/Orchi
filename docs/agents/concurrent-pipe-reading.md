# Concurrent stdout/stderr reading

## Dummy section (start here)

Orchi talks to the Cursor CLI as **another program's process** — outside our full control. That program has **two output hoses**: **stdout** (the NDJSON chat stream) and **stderr** (warnings and errors). Each hose has a small nozzle; only so much can sit in the pipe before it backs up. If a hose overflows, the CLI **blocks** and may stop sending on **either** hose. You wait forever.

**Rule: open both nozzles so both can drain at the same time.**

```
  Cursor CLI (feeds both hoses whenever it wants)
        |
        +-- stdout hose ---> Orchi reads line-by-line (what we care about)
        |
        +-- stderr hose ---> Orchi starts draining immediately (background)
```

**What goes wrong if you don't:**

| Mistake | Hose picture |
|---------|----------------|
| Await stderr first | Stand at the stderr nozzle until it's completely empty; stdout overflows while you wait |
| Read stdout only, stderr at the very end | Work only at stdout; stderr backs up and jams the whole system |
| Start stderr, then read stdout | Both nozzles open; focus on stdout, stderr drains in parallel |

You **start** stderr (`Task` + `ReadToEndAsync`) without awaiting yet — that's opening the second nozzle, not ignoring it. You **loop on stdout** because that's the live event stream we need for the UI. When stdout is done, you **await** the stderr task to collect the full log.

**The aha:** The CLI feeds both hoses the whole time; we must drain both at once, even though we only *use* stdout during the turn.

**Orchi translation:**

| Hose setup | Code |
|------------|------|
| Open the stderr nozzle immediately | `Task<string> stderrTask = process.StandardError.ReadToEndAsync(...)` |
| Work at the stdout nozzle | `while` loop with `ReadLineAsync` on stdout |
| Pick up the stderr bucket at the end | `string stderr = await stderrTask` after the loop |

Used in: `CursorAgentAdapter.ReadEventsAsync` — see method XML doc for the link back here.

Everything below is the same idea with pipe buffers and deadlock detail.

---

## Why not await stderr at the top?

If you wrote this at the start of the method:

```csharp
string stderr = await process.StandardError.ReadToEndAsync(...);
```

you would **block until stderr closes**. Stderr often stays open until the process exits. Meanwhile stdout (your event stream) would pile up unread — same deadlock risk, just on the other pipe.

If you only started reading stderr **after** the stdout loop, stderr could fill its OS pipe buffer (~4 KB on many systems) while you were still reading stdout. The child process would block on `write()` to stderr, stop making progress, and you could deadlock waiting on stdout.

## The pattern

```csharp
// 1. Start stderr consumption (do not await yet)
Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

// 2. Consume stdout in the foreground
while ((line = await process.StandardOutput.ReadLineAsync(cancellationToken)) is not null)
{
    // yield parsed events
}

// 3. Stdout is done — collect stderr for logging
string stderr = await stderrTask;
```

| Step | What happens |
|------|----------------|
| Assign to `Task` | `ReadToEndAsync` runs on a thread-pool thread; stderr pipe is drained in the background |
| Stdout loop | Primary stream; NDJSON events yielded to the caller |
| `await stderrTask` | Process is usually finished; full stderr text available for warning logs |

## When this applies

Any time you redirect **both** `StandardOutput` and `StandardError` on a `Process` and read one stream synchronously in a loop. Same pattern applies to other agent adapters that spawn CLI processes.

## Further reading

- [Cursor CLI integration](cursor-cli.md) — spawn, NDJSON, error handling
- [Agent adapters overview](README.md)
