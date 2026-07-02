import { useState } from 'react'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Layout, LayoutContent, VStack, HStack } from '@astryxdesign/core/Layout'
import { Button } from '@astryxdesign/core/Button'
import { TextInput } from '@astryxdesign/core/TextInput'
import { Text } from '@astryxdesign/core/Text'
import { Section } from '@astryxdesign/core/Section'
import { List, ListItem } from '@astryxdesign/core/List'
import { Toolbar } from '@astryxdesign/core/Toolbar'
import { Icon } from '@astryxdesign/core/Icon'
import { FolderPlusIcon, TrashIcon } from '@heroicons/react/24/outline'

import { useWorkspaces } from '@/providers/workspace-provider'
import { displayWorkspacePath } from '@/lib/workspaces/store'

export const Route = createFileRoute('/_app/settings')({
  component: SettingsPage
})

function SettingsPage(): React.JSX.Element {
  const navigate = useNavigate()
  const {
    workspaces,
    addWorkspace: registerWorkspace,
    removeWorkspace,
    renameWorkspace,
    pickDirectory
  } = useWorkspaces()
  const [manualPath, setManualPath] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState('')
  const [isPicking, setIsPicking] = useState(false)

  async function handlePickDirectory(): Promise<void> {
    setIsPicking(true)
    try {
      const path = await pickDirectory()
      if (path) {
        registerWorkspace(path)
      }
    } finally {
      setIsPicking(false)
    }
  }

  function handleManualAdd(): void {
    const path = displayWorkspacePath(manualPath)
    if (!path) {
      return
    }

    registerWorkspace(path)
    setManualPath('')
  }

  function startEditing(id: string, name: string): void {
    setEditingId(id)
    setEditingName(name)
  }

  function saveEditing(): void {
    if (!editingId) {
      return
    }

    renameWorkspace(editingId, editingName)
    setEditingId(null)
    setEditingName('')
  }

  return (
    <Layout
      height="fill"
      header={
        <Toolbar
          label="Settings"
          size="sm"
          dividers={['bottom']}
          startContent={
            <VStack gap={0}>
              <Text type="label" weight="semibold">
                Settings
              </Text>
              <Text type="supporting" color="secondary">
                Projects and app preferences
              </Text>
            </VStack>
          }
        />
      }
      content={
        <LayoutContent>
          <VStack gap={6} style={{ maxWidth: 640, marginInline: 'auto', width: '100%' }}>
            <Section variant="section" padding={4}>
              <VStack gap={4}>
                <HStack hAlign="between" vAlign="start" gap={4}>
                  <VStack gap={1}>
                    <Text type="label" weight="semibold">
                      Projects
                    </Text>
                    <Text type="supporting" color="secondary">
                      Register project folders once, then create many chats per project from the
                      navigator.
                    </Text>
                  </VStack>
                  <Button
                    label="Add project"
                    variant="secondary"
                    size="sm"
                    icon={<Icon icon={FolderPlusIcon} size="sm" />}
                    onClick={() => void handlePickDirectory()}
                    isDisabled={isPicking}
                  />
                </HStack>

                {workspaces.length === 0 ? (
                  <Text type="supporting" color="secondary">
                    No projects registered yet. Add a folder to organize chats by workspace.
                  </Text>
                ) : (
                  <List density="balanced" hasDividers>
                    {workspaces.map((workspace) => (
                      <ListItem
                        key={workspace.id}
                        label={
                          editingId === workspace.id ? editingName : workspace.name
                        }
                        description={workspace.path}
                        onClick={() => startEditing(workspace.id, workspace.name)}
                        endContent={
                          <Button
                            label={`Remove ${workspace.name}`}
                            variant="ghost"
                            size="sm"
                            icon={<Icon icon={TrashIcon} size="sm" />}
                            isIconOnly
                            onClick={(event) => {
                              event.stopPropagation()
                              removeWorkspace(workspace.id)
                            }}
                          />
                        }
                      />
                    ))}
                  </List>
                )}

                {editingId ? (
                  <TextInput
                    label="Project name"
                    value={editingName}
                    onChange={setEditingName}
                    onBlur={saveEditing}
                    hasAutoFocus
                  />
                ) : null}

                <VStack gap={2}>
                  <TextInput
                    label="Or paste a path"
                    value={manualPath}
                    onChange={setManualPath}
                    placeholder="e.g. E:\\Projects\\Orchi"
                  />
                  <HStack hAlign="end">
                    <Button
                      label="Add"
                      variant="secondary"
                      onClick={handleManualAdd}
                      isDisabled={!manualPath.trim()}
                    />
                  </HStack>
                </VStack>
              </VStack>
            </Section>

            <Section variant="section" padding={4}>
              <VStack gap={3}>
                <Text type="supporting" color="secondary">
                  Jump back to a conversation without losing navigator state.
                </Text>
                <Button label="Open chats" variant="secondary" onClick={() => navigate({ to: '/' })} />
              </VStack>
            </Section>

            <Text type="supporting" color="secondary">
              Removing a project does not delete its chats — they appear under Other until
              re-registered.
            </Text>
          </VStack>
        </LayoutContent>
      }
    />
  )
}
