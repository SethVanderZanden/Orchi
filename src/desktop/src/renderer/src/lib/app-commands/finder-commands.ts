export type AppFinderCommand = {
  id: string
  label: string
  keywords: string[]
  shortcut?: string
  disabled?: boolean
  onSelect: () => void
}

const TAB_COMMAND_PREFIX = 'tab-'
const BACKGROUND_TAB_COMMANDS = new Set(['next-tab', 'previous-tab'])

export function filterFinderCommands(
  commands: AppFinderCommand[],
  query: string
): AppFinderCommand[] {
  const normalized = query.trim().toLowerCase()
  const matched = normalized
    ? commands.filter((command) => {
        if (command.label.toLowerCase().includes(normalized)) {
          return true
        }

        return command.keywords.some((keyword) => keyword.includes(normalized))
      })
    : commands.filter(
        (command) =>
          !command.id.startsWith(TAB_COMMAND_PREFIX) && !BACKGROUND_TAB_COMMANDS.has(command.id)
      )

  return matched
}
