const MOJIBAKE_REPLACEMENTS: ReadonlyArray<[string, string]> = [
  ['ΓÇö', '—'],
  ['ΓÇô', '–'],
  ['ΓÇ£', '“'],
  ['ΓÇ¥', '”'],
  ['ΓÇÖ', '’'],
  ['ΓÇÿ', '‘'],
  ['ΓÇª', '…'],
  ['ΓåÆ', '→'],
  ['ΓÇó', '•']
]

export function normalizeAgentText(text: string): string {
  let normalized = text

  for (const [from, to] of MOJIBAKE_REPLACEMENTS) {
    normalized = normalized.split(from).join(to)
  }

  return normalized
}
