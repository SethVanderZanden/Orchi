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
import { filterWorkspaceGroups, groupChatsByWorkspace } from '@/lib/workspaces/group-chats'

const navigatorRoot: CSSProperties = {
  width: 480,
  flexShrink: 0,
  height: '100%',
  minHeight: 0,
  borderRight: '1px solid var(--color-border-subtle)'
}

const columnRow: CSSProperties = {
  overflowX: 'auto',
  overflowY: 'hidden',
  flex: 1,
  minHeight: 0
}

const scrollable: CSSProperties = {
  overflowY: 'auto'
}

const fixedColumn: CSSProperties = {
  flexShrink: 0
}

export function WorkspaceNavigator(): React.JSX.Element {
  const navigate = useNavigate()
  const { chats, searchQuery, setSearchQuery, createChat, isLoadingChats, getChat } = useChat()
  const { workspaces, addWorkspace, pickDirectory } = useWorkspaces()
  const [selectedWorkspaceId, setSelectedWorkspaceId] = useState<string | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
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

  const selectedGroup = useMemo(
    () => workspaceGroups.find((group) => group.id === selectedWorkspaceId) ?? null,
    [workspaceGroups, selectedWorkspaceId]
  )

  useEffect(() => {
    if (workspaceGroups.length === 0) {
      setSelectedWorkspaceId(null)
      return
    }

    if (activeChat) {
      const matchingGroup = workspaceGroups.find((group) =>
        group.chats.some((chat) => chat.id === activeChat.id)
      )
      if (matchingGroup) {
        setSelectedWorkspaceId(matchingGroup.id)
        return
      }
    }

    if (!selectedWorkspaceId || !workspaceGroups.some((group) => group.id === selectedWorkspaceId)) {
      setSelectedWorkspaceId(workspaceGroups[0]?.id ?? null)
    }
  }, [workspaceGroups, activeChat, selectedWorkspaceId])

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

  function openNewChatDialog(): void {
    if (!selectedGroup || selectedGroup.isOrphan) {
      return
    }

    setDialogOpen(true)
  }

  function handleRegisterOrphanPath(path: string): void {
    addWorkspace(path)
  }

  return (
    <>
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
          <VStack height="100%" gap={0}>
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
              <HStack height="100%" style={columnRow}>
                <Section
                  width={240}
                  padding={2}
                  variant="transparent"
                  dividers={['end']}
                  style={{ ...scrollable, ...fixedColumn }}
                >
                  <List density="compact" hasDividers={false}>
                    {workspaceGroups.map((group) => (
                      <ListItem
                        key={group.id}
                        label={group.name}
                        description={group.path || 'Unregistered workspace'}
                        startContent={<Icon icon={FolderIcon} size="sm" color="secondary" />}
                        isSelected={group.id === selectedWorkspaceId}
                        onClick={() => setSelectedWorkspaceId(group.id)}
                      />
                    ))}
                  </List>
                </Section>

                <Section width={240} padding={2} variant="transparent" style={{ ...scrollable, ...fixedColumn }}>
                  {selectedGroup ? (
                    <VStack gap={2} height="100%">
                      <HStack hAlign="between" vAlign="center">
                        <Text type="label" weight="semibold">
                          Chats
                        </Text>
                        {selectedGroup.isOrphan ? (
                          selectedGroup.chats.length > 0 ? (
                            <Button
                              label="Add as project"
                              variant="ghost"
                              size="sm"
                              icon={<Icon icon={FolderPlusIcon} size="sm" />}
                              onClick={() => {
                                const path = selectedGroup.chats[0]?.workspacePath
                                if (path) {
                                  handleRegisterOrphanPath(path)
                                }
                              }}
                            />
                          ) : null
                        ) : (
                          <IconButton
                            variant="ghost"
                            size="sm"
                            icon={<Icon icon={ChatBubbleLeftEllipsisIcon} size="sm" />}
                            label={`New chat in ${selectedGroup.name}`}
                            onClick={openNewChatDialog}
                          />
                        )}
                      </HStack>

                      {selectedGroup.chats.length === 0 ? (
                        <Text type="supporting" color="secondary">
                          No chats yet
                        </Text>
                      ) : (
                        <List density="compact" hasDividers={false}>
                          {selectedGroup.chats.map((chat) => (
                            <ListItem
                              key={chat.id}
                              label={chat.title}
                              description={chat.preview}
                              isSelected={chat.id === activeChatId}
                              endContent={
                                <Timestamp value={chat.updatedAt} format="relative" />
                              }
                              onClick={() =>
                                navigate({ to: '/chat/$chatId', params: { chatId: chat.id } })
                              }
                            />
                          ))}
                        </List>
                      )}
                    </VStack>
                  ) : (
                    <Text type="supporting" color="secondary">
                      Select a workspace
                    </Text>
                  )}
                </Section>
              </HStack>
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

      {selectedGroup && !selectedGroup.isOrphan ? (
        <NewChatDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          workspacePath={selectedGroup.path}
          workspaceName={selectedGroup.name}
          onCreateChat={handleCreateChat}
          isSubmitting={isCreating}
        />
      ) : null}
    </>
  )
}
