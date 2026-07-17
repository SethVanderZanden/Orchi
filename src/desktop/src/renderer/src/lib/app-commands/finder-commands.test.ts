import { describe, expect, it } from 'vitest'

import { filterFinderCommands, type AppFinderCommand } from '@/lib/app-commands/finder-commands'

const commands: AppFinderCommand[] = [
  {
    id: 'new-chat',
    label: 'New chat',
    keywords: ['create', 'tab'],
    shortcut: 'Ctrl+N',
    onSelect: () => undefined
  },
  {
    id: 'settings',
    label: 'Settings',
    keywords: ['preferences', 'config'],
    shortcut: 'Ctrl+,',
    onSelect: () => undefined
  }
]

describe('filterFinderCommands', () => {
  it('returns primary commands when query is empty', () => {
    expect(filterFinderCommands(commands, '')).toEqual(commands)
    expect(filterFinderCommands(commands, '   ')).toEqual(commands)
  })

  it('hides tab-switch commands until the user searches', () => {
    const withTabs: AppFinderCommand[] = [
      ...commands,
      {
        id: 'next-tab',
        label: 'Next tab',
        keywords: ['tab'],
        shortcut: 'Ctrl+Tab',
        onSelect: () => undefined
      },
      {
        id: 'tab-1',
        label: 'Go to tab 1',
        keywords: ['tab', '1'],
        shortcut: 'Ctrl+1',
        onSelect: () => undefined
      }
    ]

    expect(filterFinderCommands(withTabs, '')).toEqual(commands)
    expect(filterFinderCommands(withTabs, 'tab')).toHaveLength(3)
  })

  it('matches label and keywords case-insensitively', () => {
    expect(filterFinderCommands(commands, 'settings')).toHaveLength(1)
    expect(filterFinderCommands(commands, 'PREFERENCES')).toHaveLength(1)
    expect(filterFinderCommands(commands, 'create')).toHaveLength(1)
    expect(filterFinderCommands(commands, 'missing')).toHaveLength(0)
  })
})
