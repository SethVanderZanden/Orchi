export function kickOffKey(parentChatId: string, planId: string): string {
  return `${parentChatId}:${planId}`
}

export function isParentKickingOffAnyKeys(
  parentChatId: string,
  kickingOffKeys: Iterable<string>
): boolean {
  const prefix = `${parentChatId}:`
  for (const key of kickingOffKeys) {
    if (key.startsWith(prefix)) {
      return true
    }
  }

  return false
}

export function removeKickoffKeysForParent(
  parentChatId: string,
  kickingOffKeys: Set<string>
): Set<string> {
  const prefix = `${parentChatId}:`
  let changed = false
  const next = new Set<string>()

  for (const key of kickingOffKeys) {
    if (key.startsWith(prefix)) {
      changed = true
    } else {
      next.add(key)
    }
  }

  return changed ? next : kickingOffKeys
}
