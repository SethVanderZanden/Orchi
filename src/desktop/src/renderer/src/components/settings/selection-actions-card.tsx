import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { MousePointerClick, Pencil, Plus, Trash2 } from 'lucide-react'

import { EmptyState } from '@/components/empty-state'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  containsSelectedTextPlaceholder,
  defaultSelectionTemplate
} from '@/lib/selection-actions/apply-template'
import {
  createSelectionAction,
  deleteSelectionAction,
  listSelectionActions,
  updateSelectionAction
} from '@/lib/selection-actions/api'
import { SELECTED_TEXT_PLACEHOLDER, type SelectionAction } from '@/lib/selection-actions/types'
import { selectionActionKeys } from '@/lib/query-keys'

type DraftForm = {
  label: string
  template: string
}

const emptyDraft = (): DraftForm => ({
  label: '',
  template: defaultSelectionTemplate()
})

export function SelectionActionsCard(): React.JSX.Element {
  const queryClient = useQueryClient()
  const actionsQuery = useQuery({
    queryKey: selectionActionKeys.lists(),
    queryFn: listSelectionActions
  })

  const [draft, setDraft] = useState<DraftForm>(emptyDraft)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const invalidate = (): Promise<void> =>
    queryClient.invalidateQueries({ queryKey: selectionActionKeys.lists() })

  const createMutation = useMutation({
    mutationFn: createSelectionAction,
    onSuccess: async () => {
      setDraft(emptyDraft())
      setFormError(null)
      await invalidate()
    },
    onError: (error: Error) => setFormError(error.message)
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, ...request }: { id: string } & DraftForm): Promise<SelectionAction> =>
      updateSelectionAction(id, request),
    onSuccess: async () => {
      setEditingId(null)
      setDraft(emptyDraft())
      setFormError(null)
      await invalidate()
    },
    onError: (error: Error) => setFormError(error.message)
  })

  const deleteMutation = useMutation({
    mutationFn: deleteSelectionAction,
    onSuccess: async () => {
      await invalidate()
    }
  })

  const actions = actionsQuery.data ?? []
  const isSaving = createMutation.isPending || updateMutation.isPending
  const canSave =
    draft.label.trim().length > 0 &&
    draft.template.trim().length > 0 &&
    containsSelectedTextPlaceholder(draft.template)

  function startEdit(action: SelectionAction): void {
    setEditingId(action.id)
    setDraft({ label: action.label, template: action.template })
    setFormError(null)
  }

  function cancelEdit(): void {
    setEditingId(null)
    setDraft(emptyDraft())
    setFormError(null)
  }

  function handleSubmit(): void {
    if (!canSave) {
      setFormError(`Template must include ${SELECTED_TEXT_PLACEHOLDER}.`)
      return
    }

    if (editingId) {
      updateMutation.mutate({ id: editingId, ...draft })
      return
    }

    createMutation.mutate(draft)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Selection actions</CardTitle>
        <CardDescription>
          Custom right-click actions for highlighted chat text. Use{' '}
          <code className="rounded bg-muted px-1 py-0.5 text-xs">{SELECTED_TEXT_PLACEHOLDER}</code>{' '}
          where the selection should be inserted. Each action opens a split chat and sends the
          prompt.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {actionsQuery.isPending && actions.length === 0 ? (
          <p className="text-sm text-muted-foreground">Loading actions…</p>
        ) : actions.length === 0 ? (
          <EmptyState
            className="py-8"
            title="No custom actions yet"
            description="Built-in Add to chat and Define still appear in the menu. Add one below to get started."
            icon={<MousePointerClick className="size-8" />}
          />
        ) : (
          <ul className="divide-y rounded-lg border">
            {actions.map((action) => (
              <li key={action.id} className="flex items-start justify-between gap-3 px-3 py-2.5">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{action.label}</p>
                  <p className="line-clamp-2 text-xs text-muted-foreground">{action.template}</p>
                </div>
                <div className="flex shrink-0 gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label={`Edit ${action.label}`}
                    onClick={() => startEdit(action)}
                  >
                    <Pencil className="size-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label={`Delete ${action.label}`}
                    disabled={deleteMutation.isPending}
                    onClick={() => deleteMutation.mutate(action.id)}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        )}

        <div className="space-y-3 rounded-lg border p-3">
          <p className="text-sm font-medium">{editingId ? 'Edit action' : 'New action'}</p>
          <div className="space-y-2">
            <Label htmlFor="selection-action-label">Label</Label>
            <Input
              id="selection-action-label"
              value={draft.label}
              onChange={(event) =>
                setDraft((current) => ({ ...current, label: event.target.value }))
              }
              placeholder="e.g. Explain simply"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="selection-action-template">Template</Label>
            <Textarea
              id="selection-action-template"
              value={draft.template}
              onChange={(event) =>
                setDraft((current) => ({ ...current, template: event.target.value }))
              }
              rows={4}
              className="min-h-24"
            />
            <p className="text-xs text-muted-foreground">
              Must include {SELECTED_TEXT_PLACEHOLDER}.
            </p>
          </div>
          {formError ? <p className="text-sm text-destructive">{formError}</p> : null}
          <div className="flex justify-end gap-2">
            {editingId ? (
              <Button type="button" variant="ghost" onClick={cancelEdit}>
                Cancel
              </Button>
            ) : null}
            <Button type="button" disabled={!canSave || isSaving} onClick={handleSubmit}>
              <Plus className="size-4" />
              {editingId ? 'Save' : 'Add action'}
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
