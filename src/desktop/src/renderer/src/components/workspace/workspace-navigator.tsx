import { useEffect, useMemo, useState, type CSSProperties } from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'
import { HStack, Layout, LayoutContent, VStack } from '@astryxdesign/core/Layout'
import { List, ListItem } from '@astryxdesign/core/List'
import { Section } from '@astryxdesign/core/Section'
import { Text } from '@astryxdesign/core/Text'
import { Icon } from '@astryxdesign/core/Icon'
import { IconButton } from '@astryxdesign/core/IconButton'
import { Button } from '@astryxdesign/core/Button'
import { TextInput } from '@astryxdesign/core/TextInput'
import { Toolbar } from '@astryxdesign/core/Toolbar'
import { Timestamp } from '@astryxdesign/core/Timestamp'
import {
  ChevronDownIcon,
  ChevronRightIcon,
  FolderPlusIcon,
  MagnifyingGlassIcon,
  ChatBubbleLeftEllipsisIcon,
  Cog6ToothIcon
} from '@heroicons/react/24/outline'
import { FolderIcon } from '@heroicons/react/24/solid'

import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'
import { NewChatDialog, type NewChatOptions } from '@/components/chat/new-chat-dialog'
import { useChat } from '@/providers/chat-provider'
import { useWorkspaces } from '@/providers/workspace-provider'
import { useWorkspaceLayout } from '@/providers/workspace-layout-provider'
import {
  filterWorkspaceGroups,
  groupChatsByWorkspace,
  type WorkspaceChatGroup
} from '@/lib/workspaces/group-chats'

const navigatorShell: CSSProperties = {
  width: 240,
  flexShrink: 0,
  height: '100%',
  minHeight: 0,
  borderRight: '1px solid var(--color-border-subtle)'
}

const navigatorRoot: CSSProperties = {
  width: '100%',
  height: '100%',
  minHeight: 0
}

const scrollable: CSSProperties = {
  overflowY: 'auto',
  flex: 1,
  minHeight: 0
}

export function WorkspaceNavigator(): React.JSX.Element {
  const navigate = useNavigate()
  const { chats, searchQuery, setSearchQuery, createChat, isLoadingChats, getChat } = useChat()
  const { workspaces, addWorkspace, pickDirectory } = useWorkspaces()
  const { isProjectExpanded, toggleProjectExpanded, ensureProjectExpanded } = useWorkspaceLayout()
  const [newChatGroup, setNewChatGroup] = useState<WorkspaceChatGroup | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [isAddingProject, setIsAddingProject] = useState(false)

  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })
  const activeChatId = chatMatch?.params.chatId ?? null
  const activeChat = activeChatId ? getChat(activeChatId) : undefined

  const settingsMatch = useMatch({
    from: '/_app/settings',
    shouldThrow: false
  })

  const workspaceGroups = useMemo(() => {
    const groups = groupChatsByWorkspace(workspaces, chats)
    return filterWorkspaceGroups(groups, searchQuery)
  }, [workspaces, chats, searchQuery])

  useEffect(() => {
    if (!activeChat) {
      return
    }

    const matchingGroup = workspaceGroups.find((group) =>
      group.chats.some((chat) => chat.id === activeChat.id)
    )
    if (matchingGroup) {
      ensureProjectExpanded(matchingGroup.id)
    }
  }, [activeChat, ensureProjectExpanded, workspaceGroups])

  useEffect(() => {
    if (workspaceGroups.length === 0) {
      return
    }

    const hasExpandedProject = workspaceGroups.some((group) => isProjectExpanded(group.id))
    if (!hasExpandedProject) {
      ensureProjectExpanded(workspaceGroups[0].id)
    }
  }, [ensureProjectExpanded, isProjectExpanded, workspaceGroups])

  async function handleCreateChat(options: NewChatOptions): Promise<void> {
    setIsCreating(true)
    try {
      await createChat(options)
    } finally {
      setIsCreating(false)
    }
  }

  async function handleAddProject(): Promise<void> {
    setIsAddingProject(true)
    try {
      const path = await pickDirectory()
      if (path) {
        addWorkspace(path)
      }
    } finally {
      setIsAddingProject(false)
    }
  }

  function handleRegisterOrphanPath(path: string): void {
    addWorkspace(path)
  }

  return (
    <>
      <div style={navigatorShell}>
        <Layout
          height="fill"
          style={navigatorRoot}
          header={
            <Toolbar
              label="Orchi navigation"
              size="sm"
              dividers={['bottom']}
              startContent={
                <HStack gap={2} vAlign="center">
                  <OrchiAiIcon className="size-5" />
                  <VStack gap={0}>
                    <Text type="label" weight="semibold">
                      Orchi
                    </Text>
                    <Text type="supporting" color="secondary">
                      AI orchestrator
                    </Text>
                  </VStack>
                </HStack>
              }
              endContent={
                <HStack gap={1}>
                  <IconButton
                    variant="ghost"
                    size="sm"
                    icon={<Icon icon={FolderPlusIcon} size="sm" />}
                    label="Add project"
                    onClick={() => void handleAddProject()}
                    isDisabled={isAddingProject}
                  />
                  <IconButton
                    variant="ghost"
                    size="sm"
                    icon={<Icon icon={Cog6ToothIcon} size="sm" />}
                    label="Settings"
                    onClick={() => navigate({ to: '/settings' })}
                    aria-current={settingsMatch ? 'page' : undefined}
                  />
                </HStack>
              }
            />
          }
          content={
            <LayoutContent padding={0} isScrollable={false}>
              <VStack height="100%" gap={0} className="min-h-0">
                <Section padding={2} variant="transparent" dividers={['bottom']}>
                  <TextInput
                    label="Search chats"
                    isLabelHidden
                    value={searchQuery}
                    onChange={setSearchQuery}
                    placeholder="Search chats"
                    size="sm"
                    startIcon={<Icon icon={MagnifyingGlassIcon} size="sm" />}
                  />
                </Section>

                {isLoadingChats ? (
                  <Section padding={3} variant="transparent">
                    <Text type="supporting" color="secondary">
                      Loading chats…
                    </Text>
                  </Section>
                ) : workspaces.length === 0 ? (
                  <Section padding={3} variant="transparent">
                    <VStack gap={3}>
                      <Text type="supporting" color="secondary">
                        Add a project folder to start chatting with agents in that workspace.
                      </Text>
                      <Button
                        label="Add project"
                        variant="secondary"
                        size="sm"
                        icon={<Icon icon={FolderPlusIcon} size="sm" />}
                        onClick={() => void handleAddProject()}
                        isDisabled={isAddingProject}
                      />
                    </VStack>
                  </Section>
                ) : (
                  <Section padding={1} variant="transparent" style={scrollable}>
                    <List density="compact" hasDividers={false}>
                      {workspaceGroups.map((group) => {
                        const expanded = isProjectExpanded(group.id)

                        return (
                          <VStack key={group.id} gap={0} className="min-w-0">
                            <ListItem
                              label={
                                <Text type="supporting" color="secondary" className="truncate">
                                  {group.name}
                                </Text>
                              }
                              startContent={
                                <HStack gap={1} vAlign="center">
                                  <Icon
                                    icon={expanded ? ChevronDownIcon : ChevronRightIcon}
                                    size="sm"
                                    color="secondary"
                                  />
                                  <Icon icon={FolderIcon} size="sm" color="secondary" />
                                </HStack>
                              }
                              endContent={
                                group.isOrphan ? (
                                  group.chats.length > 0 ? (
                                    <div
                                      onClick={(event) => event.stopPropagation()}
                                      onKeyDown={(event) => event.stopPropagation()}
                                    >
                                      <Button
                                        label="Add as project"
                                        variant="ghost"
                                        size="sm"
                                        icon={<Icon icon={FolderPlusIcon} size="sm" />}
                                        onClick={() => {
                                          const path = group.chats[0]?.workspacePath
                                          if (path) {
                                            handleRegisterOrphanPath(path)
                                          }
                                        }}
                                      />
                                    </div>
                                  ) : null
                                ) : (
                                  <div
                                    onClick={(event) => event.stopPropagation()}
                                    onKeyDown={(event) => event.stopPropagation()}
                                  >
                                    <IconButton
                                      variant="ghost"
                                      size="sm"
                                      icon={<Icon icon={ChatBubbleLeftEllipsisIcon} size="sm" />}
                                      label={`New chat in ${group.name}`}
                                      onClick={() => setNewChatGroup(group)}
                                    />
                                  </div>
                                )
                              }
                              onClick={() => toggleProjectExpanded(group.id)}
                            />

                            {expanded ? (
                              <VStack gap={0} className="min-w-0 pl-5">
                                {group.chats.length === 0 ? (
                                  <Text type="supporting" color="secondary" className="px-2 py-1 text-xs">
                                    No chats yet
                                  </Text>
                                ) : (
                                  group.chats.map((chat) => (
                                    <ListItem
                                      key={chat.id}
                                      label={
                                        <Text type="label" className="truncate text-sm">
                                          {chat.title}
                                        </Text>
                                      }
                                      isSelected={chat.id === activeChatId}
                                      endContent={
                                        <Timestamp value={chat.updatedAt} format="relative" />
                                      }
                                      onClick={() =>
                                        navigate({
                                          to: '/chat/$chatId',
                                          params: { chatId: chat.id }
                                        })
                                      }
                                    />
                                  ))
                                )}
                              </VStack>
                            ) : null}
                          </VStack>
                        )
                      })}
                    </List>
                  </Section>
                )}

                {workspaces.length > 0 ? (
                  <Section padding={2} variant="transparent" dividers={['top']}>
                    <Button
                      label="Add project"
                      variant="secondary"
                      size="sm"
                      icon={<Icon icon={FolderPlusIcon} size="sm" />}
                      onClick={() => void handleAddProject()}
                      isDisabled={isAddingProject}
                    />
                  </Section>
                ) : null}
              </VStack>
            </LayoutContent>
          }
        />
      </div>

      {newChatGroup && !newChatGroup.isOrphan ? (
        <NewChatDialog
          open={Boolean(newChatGroup)}
          onOpenChange={(open) => {
            if (!open) {
              setNewChatGroup(null)
            }
          }}
          workspacePath={newChatGroup.path}
          workspaceName={newChatGroup.name}
          onCreateChat={handleCreateChat}
          isSubmitting={isCreating}
        />
      ) : null}
    </>
  )
}
