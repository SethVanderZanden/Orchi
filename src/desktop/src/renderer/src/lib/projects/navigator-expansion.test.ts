import { describe, expect, it } from 'vitest'

import {
  isParentChatVisibleExpanded,
  isWorkspaceVisibleExpanded,
  resolveIsProjectExpanded,
  resolveVisibleParentChatIds,
  resolveVisibleWorkspaceIds,
  shouldStickyExpandParent
} from './navigator-expansion'

describe('resolveVisibleParentChatIds', () => {
  it('includes sticky-only parents until user collapses them', () => {
    const input = {
      expandedParentChatIds: new Set<string>(),
      collapsedParentChatIds: new Set<string>(),
      stickyExpandedParentIds: new Set(['parent-1']),
      activeParentChatId: null
    }

    expect(resolveVisibleParentChatIds(input)).toEqual(new Set(['parent-1']))
    expect(isParentChatVisibleExpanded('parent-1', input)).toBe(true)
  })

  it('hides sticky-only parents after user collapse', () => {
    const collapsed = {
      expandedParentChatIds: new Set<string>(),
      collapsedParentChatIds: new Set(['parent-1']),
      stickyExpandedParentIds: new Set(['parent-1']),
      activeParentChatId: null
    }

    expect(resolveVisibleParentChatIds(collapsed)).toEqual(new Set())
    expect(isParentChatVisibleExpanded('parent-1', collapsed)).toBe(false)
  })

  it('re-expands after user removes collapse override', () => {
    const expanded = {
      expandedParentChatIds: new Set(['parent-1']),
      collapsedParentChatIds: new Set<string>(),
      stickyExpandedParentIds: new Set(['parent-1']),
      activeParentChatId: null
    }

    expect(resolveVisibleParentChatIds(expanded)).toEqual(new Set(['parent-1']))
  })

  it('keeps active child parent expanded even when user collapsed it', () => {
    const input = {
      expandedParentChatIds: new Set<string>(),
      collapsedParentChatIds: new Set(['parent-1']),
      stickyExpandedParentIds: new Set<string>(),
      activeParentChatId: 'parent-1'
    }

    expect(resolveVisibleParentChatIds(input)).toEqual(new Set(['parent-1']))
    expect(isParentChatVisibleExpanded('parent-1', input)).toBe(true)
  })
})

describe('resolveVisibleWorkspaceIds', () => {
  it('includes active workspace even when user collapsed it', () => {
    const input = {
      expandedWorkspaceIds: new Set<string>(),
      collapsedWorkspaceIds: new Set(['workspace-1']),
      activeWorkspaceId: 'workspace-1'
    }

    expect(resolveVisibleWorkspaceIds(input)).toEqual(new Set(['workspace-1']))
    expect(isWorkspaceVisibleExpanded('workspace-1', input)).toBe(true)
  })

  it('respects user collapse when workspace is not active', () => {
    const input = {
      expandedWorkspaceIds: new Set(['workspace-1']),
      collapsedWorkspaceIds: new Set(['workspace-1']),
      activeWorkspaceId: 'workspace-2'
    }

    expect(resolveVisibleWorkspaceIds(input)).toEqual(new Set(['workspace-2']))
    expect(isWorkspaceVisibleExpanded('workspace-1', input)).toBe(false)
  })
})

describe('resolveIsProjectExpanded', () => {
  const projectGroupIds = ['project-1', 'project-2']

  it('auto-expands the first project when nothing else is expanded', () => {
    const input = {
      projectId: 'project-1',
      expandedProjectIds: new Set<string>(),
      collapsedProjectIds: new Set<string>(),
      activeProjectId: null,
      projectGroupIds
    }

    expect(resolveIsProjectExpanded(input)).toBe(true)
  })

  it('respects user collapse on the first-project fallback', () => {
    const input = {
      projectId: 'project-1',
      expandedProjectIds: new Set<string>(),
      collapsedProjectIds: new Set(['project-1']),
      activeProjectId: null,
      projectGroupIds
    }

    expect(resolveIsProjectExpanded(input)).toBe(false)
  })

  it('keeps active project expanded even when user collapsed it', () => {
    const input = {
      projectId: 'project-2',
      expandedProjectIds: new Set<string>(),
      collapsedProjectIds: new Set(['project-2']),
      activeProjectId: 'project-2',
      projectGroupIds
    }

    expect(resolveIsProjectExpanded(input)).toBe(true)
  })

  it('uses persisted expansion when present', () => {
    const input = {
      projectId: 'project-2',
      expandedProjectIds: new Set(['project-2']),
      collapsedProjectIds: new Set<string>(),
      activeProjectId: null,
      projectGroupIds
    }

    expect(resolveIsProjectExpanded(input)).toBe(true)
    expect(
      resolveIsProjectExpanded({
        ...input,
        projectId: 'project-1'
      })
    ).toBe(false)
  })
})

describe('shouldStickyExpandParent', () => {
  it('blocks sticky auto-expand for user-collapsed parents', () => {
    expect(shouldStickyExpandParent('parent-1', new Set(['parent-1']))).toBe(false)
    expect(shouldStickyExpandParent('parent-1', new Set())).toBe(true)
  })
})
