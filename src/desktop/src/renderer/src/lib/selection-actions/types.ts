export type SelectionAction = {
  id: string
  label: string
  template: string
  sortOrder: number
  createdAt: string
  updatedAt: string
}

export type CreateSelectionActionRequest = {
  label: string
  template: string
}

export type UpdateSelectionActionRequest = {
  label: string
  template: string
  sortOrder?: number
}

/** Placeholder users put in custom selection-action templates. */
export const SELECTED_TEXT_PLACEHOLDER = '{{selected text}}'

export const DEFINE_SELECTION_TEMPLATE =
  'Please define "{{selected text}}" for me in simple terms with an example use-case.'
