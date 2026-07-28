import { useCallback, useReducer } from 'react'

type AttachmentsPanelState = {
  panelOpen: boolean
}

type AttachmentsPanelAction = { type: 'toggle-panel' } | { type: 'close-panel' } | { type: 'open-panel' }

function attachmentsPanelReducer(
  state: AttachmentsPanelState,
  action: AttachmentsPanelAction
): AttachmentsPanelState {
  switch (action.type) {
    case 'toggle-panel':
      return { panelOpen: !state.panelOpen }
    case 'open-panel':
      return { panelOpen: true }
    case 'close-panel':
      return { panelOpen: false }
    default:
      return state
  }
}

export function useAttachmentsPanel(hasAttachments: boolean) {
  const [state, dispatch] = useReducer(attachmentsPanelReducer, { panelOpen: false })

  const togglePanel = useCallback(() => {
    if (!hasAttachments) {
      return
    }

    dispatch({ type: 'toggle-panel' })
  }, [hasAttachments])

  const closePanel = useCallback(() => {
    dispatch({ type: 'close-panel' })
  }, [])

  return {
    attachmentsPanelOpen: state.panelOpen && hasAttachments,
    toggleAttachmentsPanel: togglePanel,
    closeAttachmentsPanel: closePanel
  }
}
