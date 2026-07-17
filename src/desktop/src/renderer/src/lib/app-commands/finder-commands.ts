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
export const FINDER_COMMAND_PREFIX = '>'

function isActionCommand(command: AppFinderCommand): boolean {
  return command.label.startsWith(FINDER_COMMAND_PREFIX)
}

function matchesCommandQuery(command: AppFinderCommand, query: string): boolean {
  const label = command.label.slice(FINDER_COMMAND_PREFIX.length).trim().toLowerCase()

  if (label.includes(query)) {
    return true
  }

  return command.keywords.some((keyword) => keyword.includes(query))
}

function filterActionCommands(commands: AppFinderCommand[], query: string): AppFinderCommand[] {
  const actionCommands = commands.filter(isActionCommand)
  const commandQuery = query.slice(FINDER_COMMAND_PREFIX.length).trim().toLowerCase()

  if (!commandQuery) {
    return actionCommands
  }

  return actionCommands.filter((command) => matchesCommandQuery(command, commandQuery))
}

export function filterFinderCommands(
  commands: AppFinderCommand[],
  query: string
): AppFinderCommand[] {
  const trimmed = query.trim()

  if (trimmed.startsWith(FINDER_COMMAND_PREFIX)) {
    return filterActionCommands(commands, trimmed)
  }

  const normalized = trimmed.toLowerCase()
  const standardCommands = commands.filter((command) => !isActionCommand(command))
  const matched = normalized
    ? standardCommands.filter((command) => {
        if (command.label.toLowerCase().includes(normalized)) {
          return true
        }

        return command.keywords.some((keyword) => keyword.includes(normalized))
      })
    : standardCommands.filter(
        (command) =>
          !command.id.startsWith(TAB_COMMAND_PREFIX) && !BACKGROUND_TAB_COMMANDS.has(command.id)
      )

  return matched
}

export function isFinderCommandMode(query: string): boolean {
  return query.trim().startsWith(FINDER_COMMAND_PREFIX)
}
