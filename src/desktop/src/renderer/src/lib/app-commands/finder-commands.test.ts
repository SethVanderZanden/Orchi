import { describe, expect, it } from 'vitest'

import {
  filterFinderCommands,
  isFinderCommandMode,
  type AppFinderCommand
} from '@/lib/app-commands/finder-commands'

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
  },
  {
    id: 'close-all-chats',
    label: '> Close All Chats',
    keywords: ['close', 'all', 'tabs'],
    onSelect: () => undefined
  },
  {
    id: 'pin-chat',
    label: '> Pin Chat',
    keywords: ['pin', 'tab'],
    onSelect: () => undefined
  }
]

describe('filterFinderCommands', () => {
  it('returns primary commands when query is empty', () => {
    expect(filterFinderCommands(commands, '')).toEqual(commands.slice(0, 2))
    expect(filterFinderCommands(commands, '   ')).toEqual(commands.slice(0, 2))
  })

  it('hides tab-switch commands until the user searches', () => {
    const withTabs: AppFinderCommand[] = [
      ...commands.slice(0, 2),
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

    expect(filterFinderCommands(withTabs, '')).toEqual(commands.slice(0, 2))
    expect(filterFinderCommands(withTabs, 'tab')).toHaveLength(3)
  })

  it('matches label and keywords case-insensitively', () => {
    expect(filterFinderCommands(commands, 'settings')).toHaveLength(1)
    expect(filterFinderCommands(commands, 'PREFERENCES')).toHaveLength(1)
    expect(filterFinderCommands(commands, 'create')).toHaveLength(1)
    expect(filterFinderCommands(commands, 'missing')).toHaveLength(0)
  })

  it('shows only action commands when the query starts with >', () => {
    expect(filterFinderCommands(commands, '>')).toEqual(commands.slice(2))
    expect(filterFinderCommands(commands, '> pin')).toEqual([commands[3]])
    expect(filterFinderCommands(commands, '> close all')).toEqual([commands[2]])
  })
})

describe('isFinderCommandMode', () => {
  it('detects command mode from the > prefix', () => {
    expect(isFinderCommandMode('>')).toBe(true)
    expect(isFinderCommandMode('> close')).toBe(true)
    expect(isFinderCommandMode(' close')).toBe(false)
    expect(isFinderCommandMode('settings')).toBe(false)
  })
})
