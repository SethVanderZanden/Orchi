import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useMatch } from '@tanstack/react-router'

import { useDeleteChat } from '@/hooks/use-delete-chat'
import { useKeyboardShortcut } from '@/hooks/use-keyboard-shortcut'
import type { SidebarChatActions } from '@/components/layout/sidebar/sidebar-utils'
import {
  filterProjectGroups,
  findProjectGroupForChat,
  groupChatsByProject,
  ORPHAN_GROUP_ID,
  resolveWorkspaceIdForNewChat,
  type ProjectChatGroup
} from '@/lib/projects/group-chats'
import { useChat } from '@/providers/chat-provider'
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

export function useProjectNavigatorState(navigateToChat: (chatId: string) => void): UseProjectNavigatorStateResult {
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
  const priorChildCountsRef = useRef<Map<string, number>>(new Map())

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

  const ensureWorkspaceExpanded = useCallback((workspaceId: string) => {
    setExpandedWorkspaceIds((current) => {
      if (current.has(workspaceId)) {
        return current
      }
      const next = new Set(current)
      next.add(workspaceId)
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

  const ensureParentExpanded = useCallback((parentChatId: string) => {
    setExpandedParentChatIds((current) => {
      if (current.has(parentChatId)) {
        return current
      }
      const next = new Set(current)
      next.add(parentChatId)
      return next
    })
  }, [])

  useEffect(() => {
    if (!activeChat) {
      return
    }

    const matchingGroup = findProjectGroupForChat(projectGroups, activeChat)
    if (matchingGroup) {
      ensureProjectExpanded(matchingGroup.id)
    }
    if (activeChat.workspaceId) {
      ensureWorkspaceExpanded(activeChat.workspaceId)
    }
    if (activeChat.parentChatId) {
      ensureParentExpanded(activeChat.parentChatId)
    }
  }, [
    activeChat,
    ensureParentExpanded,
    ensureProjectExpanded,
    ensureWorkspaceExpanded,
    projectGroups
  ])

  useEffect(() => {
    for (const group of projectGroups) {
      const nodes = group.isFlat
        ? group.chatNodes
        : group.workspaceGroups.flatMap((workspaceGroup) => workspaceGroup.chatNodes)

      for (const node of nodes) {
        if (node.children.length === 0) {
          continue
        }

        const priorCount = priorChildCountsRef.current.get(node.chat.id) ?? 0
        if (node.children.length > priorCount) {
          ensureParentExpanded(node.chat.id)
        }
        priorChildCountsRef.current.set(node.chat.id, node.children.length)
      }
    }
  }, [ensureParentExpanded, projectGroups])

  useEffect(() => {
    if (projectGroups.length === 0) {
      return
    }

    const hasExpandedProject = projectGroups.some((group) => isProjectExpanded(group.id))
    if (!hasExpandedProject) {
      ensureProjectExpanded(projectGroups[0]!.id)
    }
  }, [ensureProjectExpanded, isProjectExpanded, projectGroups])

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

      setIsCreating(true)
      try {
        await createChat({ workspaceId, projectId: group.id })
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
    expandedWorkspaceIds,
    expandedParentChatIds,
    isProjectExpanded,
    toggleProjectExpanded,
    toggleWorkspaceExpanded,
    toggleParentExpanded,
    createChatInGroup,
    handleAddProject,
    handleRegisterOrphanPath,
    settingsMatch
  }
}
