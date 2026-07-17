import { useCallback, useMemo, useState } from 'react'
import { useMatch } from '@tanstack/react-router'

import { useDeleteChat } from '@/hooks/use-delete-chat'
import { useKeyboardShortcut } from '@/hooks/use-keyboard-shortcut'
import type { SidebarChatActions } from '@/components/layout/sidebar/sidebar-utils'
import { collectChatTreeNodes, findAncestorIdsInChatTree } from '@/lib/projects/chat-tree'
import {
  filterProjectGroups,
  findProjectGroupForChat,
  groupChatsByProject,
  ORPHAN_GROUP_ID,
  resolveWorkspaceIdForNewChat,
  type ProjectChatGroup
} from '@/lib/projects/group-chats'
import { useChat } from '@/providers/chat-context'
import { useProjects } from '@/providers/project-provider'
import { useProjectLayout } from '@/providers/project-layout-provider'

type UseProjectNavigatorStateResult = {
  searchQuery: string
  setSearchQuery: (query: string) => void
  projects: ReturnType<typeof useProjects>['projects']
  isInitialChatLoad: boolean
  isInitialProjectLoad: boolean
  projectsError: Error | null
  chatsError: Error | null
  refetchProjects: () => void
  refetchChats: () => void
  regularGroups: ProjectChatGroup[]
  orphanGroup: ProjectChatGroup | null
  chatActions: SidebarChatActions
  isCreating: boolean
  isAddingProject: boolean
  expandedWorkspaceIds: ReadonlySet<string>
  expandedParentChatIds: ReadonlySet<string>
  isProjectExpanded: (projectId: string) => boolean
  toggleProjectExpanded: (projectId: string) => void
  toggleWorkspaceExpanded: (workspaceId: string) => void
  toggleParentExpanded: (parentChatId: string) => void
  createChatInGroup: (group: ProjectChatGroup, workspaceSubGroupId?: string) => Promise<void>
  handleAddProject: () => Promise<void>
  handleRegisterOrphanPath: (path: string) => void
  settingsMatch: ReturnType<typeof useMatch>
}

export function useProjectNavigatorState(
  navigateToChat: (chatId: string) => void
): UseProjectNavigatorStateResult {
  const {
    chats,
    searchQuery,
    setSearchQuery,
    createChat,
    isPendingChats,
    isFetchingChats,
    chatsError,
    refetchChats,
    getChat,
    isChatSending,
    getChatSidebarStatus
  } = useChat()
  const { requestDelete, isDeletingChat } = useDeleteChat()
  const { projects, addProject, pickDirectory, isPendingProjects, projectsError, refetchProjects } =
    useProjects()
  const { isProjectExpanded, toggleProjectExpanded, ensureProjectExpanded } = useProjectLayout()

  const [isCreating, setIsCreating] = useState(false)
  const [isAddingProject, setIsAddingProject] = useState(false)
  const [expandedWorkspaceIds, setExpandedWorkspaceIds] = useState<Set<string>>(() => new Set())
  const [expandedParentChatIds, setExpandedParentChatIds] = useState<Set<string>>(() => new Set())
  const [childExpansionSnapshot, setChildExpansionSnapshot] = useState<{
    projectGroups: ProjectChatGroup[]
    priorChildCounts: Map<string, number>
    stickyExpandedParentIds: Set<string>
  }>(() => ({
    projectGroups: [],
    priorChildCounts: new Map<string, number>(),
    stickyExpandedParentIds: new Set<string>()
  }))

  const chatMatch = useMatch({ from: '/_app/chat/$chatId', shouldThrow: false })
  const activeChatId = chatMatch?.params.chatId ?? null
  const activeChat = activeChatId ? getChat(activeChatId) : null
  const settingsMatch = useMatch({ from: '/_app/settings', shouldThrow: false })

  const projectGroups = useMemo(() => {
    const groups = groupChatsByProject(projects, chats)
    return filterProjectGroups(groups, searchQuery)
  }, [projects, chats, searchQuery])

  const orphanGroup = useMemo(
    () => projectGroups.find((group) => group.id === ORPHAN_GROUP_ID) ?? null,
    [projectGroups]
  )
  const regularGroups = useMemo(
    () => projectGroups.filter((group) => !group.isOrphan),
    [projectGroups]
  )

  if (childExpansionSnapshot.projectGroups !== projectGroups) {
    const priorChildCounts = new Map(childExpansionSnapshot.priorChildCounts)
    const stickyExpandedParentIds = new Set(childExpansionSnapshot.stickyExpandedParentIds)

    for (const group of projectGroups) {
      const nodes = group.isFlat
        ? group.chatNodes
        : group.workspaceGroups.flatMap((workspaceGroup) => workspaceGroup.chatNodes)

      for (const node of collectChatTreeNodes(nodes)) {
        const priorCount = priorChildCounts.get(node.chat.id) ?? 0
        priorChildCounts.set(node.chat.id, node.children.length)
        if (node.children.length > priorCount) {
          stickyExpandedParentIds.add(node.chat.id)
        }
      }
    }

    setChildExpansionSnapshot({ projectGroups, priorChildCounts, stickyExpandedParentIds })
  }

  const toggleWorkspaceExpanded = useCallback((workspaceId: string) => {
    setExpandedWorkspaceIds((current) => {
      const next = new Set(current)
      if (next.has(workspaceId)) {
        next.delete(workspaceId)
      } else {
        next.add(workspaceId)
      }
      return next
    })
  }, [])

  const toggleParentExpanded = useCallback((parentChatId: string) => {
    setExpandedParentChatIds((current) => {
      const next = new Set(current)
      if (next.has(parentChatId)) {
        next.delete(parentChatId)
      } else {
        next.add(parentChatId)
      }
      return next
    })
  }, [])

  const activeProjectId = useMemo(() => {
    if (!activeChat) {
      return null
    }
    return findProjectGroupForChat(projectGroups, activeChat)?.id ?? null
  }, [activeChat, projectGroups])

  const visibleExpandedWorkspaceIds = useMemo(() => {
    const merged = new Set(expandedWorkspaceIds)
    if (activeChat?.workspaceId) {
      merged.add(activeChat.workspaceId)
    }
    return merged
  }, [activeChat, expandedWorkspaceIds])

  const visibleExpandedParentChatIds = useMemo(() => {
    const merged = new Set(expandedParentChatIds)
    if (activeChat?.parentChatId) {
      merged.add(activeChat.parentChatId)
    }
    if (activeChat) {
      for (const group of projectGroups) {
        const nodes = group.isFlat
          ? group.chatNodes
          : group.workspaceGroups.flatMap((workspaceGroup) => workspaceGroup.chatNodes)
        const ancestors = findAncestorIdsInChatTree(nodes, activeChat.id)
        if (ancestors) {
          for (const ancestorId of ancestors) {
            merged.add(ancestorId)
          }
          break
        }
      }
    }
    for (const parentChatId of childExpansionSnapshot.stickyExpandedParentIds) {
      merged.add(parentChatId)
    }
    return merged
  }, [
    activeChat,
    childExpansionSnapshot.stickyExpandedParentIds,
    expandedParentChatIds,
    projectGroups
  ])

  const resolveIsProjectExpanded = useCallback(
    (projectId: string) => {
      if (isProjectExpanded(projectId)) {
        return true
      }
      if (activeProjectId === projectId) {
        return true
      }
      const hasAnyExpanded = projectGroups.some(
        (group) => isProjectExpanded(group.id) || group.id === activeProjectId
      )
      if (!hasAnyExpanded && projectGroups.length > 0) {
        return projectGroups[0]!.id === projectId
      }
      return false
    },
    [activeProjectId, isProjectExpanded, projectGroups]
  )

  const defaultProjectGroup = useMemo(() => {
    if (activeChat) {
      const activeGroup = findProjectGroupForChat(projectGroups, activeChat)
      if (activeGroup && !activeGroup.isOrphan) {
        return activeGroup
      }
    }
    return projectGroups.find((group) => !group.isOrphan) ?? null
  }, [activeChat, projectGroups])

  const createChatInGroup = useCallback(
    async (group: ProjectChatGroup, workspaceSubGroupId?: string) => {
      if (group.isOrphan || isCreating) {
        return
      }

      const workspaceId = resolveWorkspaceIdForNewChat(group, workspaceSubGroupId)
      if (!workspaceId) {
        return
      }

      const workspacePath = group.isFlat
        ? group.defaultWorkspacePath
        : (group.workspaceGroups.find((workspace) => workspace.id === workspaceId)?.path ??
          group.defaultWorkspacePath)

      setIsCreating(true)
      try {
        await createChat({ workspaceId, workspacePath, projectId: group.id })
      } finally {
        setIsCreating(false)
      }
    },
    [createChat, isCreating]
  )

  const createDefaultChat = useCallback(() => {
    if (!defaultProjectGroup) {
      return
    }
    void createChatInGroup(defaultProjectGroup)
  }, [createChatInGroup, defaultProjectGroup])

  useKeyboardShortcut('n', createDefaultChat, {
    enabled: Boolean(defaultProjectGroup) && !isCreating
  })

  const chatActions = useMemo<SidebarChatActions>(
    () => ({
      activeChatId,
      onSelectChat: navigateToChat,
      onRequestDelete: requestDelete,
      isDeletingChat,
      getChatSidebarStatus,
      isChatSending
    }),
    [
      activeChatId,
      getChatSidebarStatus,
      isChatSending,
      isDeletingChat,
      navigateToChat,
      requestDelete
    ]
  )

  const handleAddProject = useCallback(async () => {
    setIsAddingProject(true)
    try {
      const path = await pickDirectory()
      if (path) {
        const project = await addProject(path)
        if (project) {
          ensureProjectExpanded(project.id)
        }
      }
    } finally {
      setIsAddingProject(false)
    }
  }, [addProject, ensureProjectExpanded, pickDirectory])

  const handleRegisterOrphanPath = useCallback(
    (path: string) => {
      void addProject(path)
    },
    [addProject]
  )

  return {
    searchQuery,
    setSearchQuery,
    projects,
    isInitialChatLoad: isPendingChats || (isFetchingChats && chats.length === 0),
    isInitialProjectLoad: isPendingProjects && projects.length === 0,
    projectsError,
    chatsError,
    refetchProjects,
    refetchChats,
    regularGroups,
    orphanGroup,
    chatActions,
    isCreating,
    isAddingProject,
    expandedWorkspaceIds: visibleExpandedWorkspaceIds,
    expandedParentChatIds: visibleExpandedParentChatIds,
    isProjectExpanded: resolveIsProjectExpanded,
    toggleProjectExpanded,
    toggleWorkspaceExpanded,
    toggleParentExpanded,
    createChatInGroup,
    handleAddProject,
    handleRegisterOrphanPath,
    settingsMatch
  }
}
