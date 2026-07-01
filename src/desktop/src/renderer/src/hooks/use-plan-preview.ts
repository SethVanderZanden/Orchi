import { useCallback, useEffect, useMemo, useRef, useState, type RefObject } from 'react'
import { useQuery } from '@tanstack/react-query'
import { usePanelRef, type PanelImperativeHandle } from 'react-resizable-panels'

import { getPlan } from '@/lib/chat/api'
import {
  extractPlanTitle,
  getLatestAssistantMessage,
  hasPlanPreviewContent,
  isAssistantMessageStreaming,
  isPlanOrOrchestrateMode,
  isPlanResponseMessage
} from '@/lib/chat/plan-preview'
import type { ChatMessage, ChatThread } from '@/lib/chat/types'
import { planKeys } from '@/lib/query-keys'

export function usePlanPreview(chat: ChatThread) {
  const planPanelRef = usePanelRef()
  const [isOpen, setIsOpen] = useState(false)
  const userClosedRef = useRef(false)

  const attachedPlanId = chat.attachedPlanId ?? null
  const isPlanMode = isPlanOrOrchestrateMode(chat.mode)

  const planQuery = useQuery({
    queryKey: planKeys.detail(attachedPlanId ?? ''),
    queryFn: () => getPlan(attachedPlanId!),
    enabled: Boolean(attachedPlanId)
  })

  const latestAssistant = useMemo(
    () => getLatestAssistantMessage(chat.messages),
    [chat.messages]
  )

  const messagePlanContent = isPlanMode && latestAssistant ? latestAssistant.content : ''
  const isStreaming = isPlanMode && isAssistantMessageStreaming(latestAssistant)
  const hasPlanContent = hasPlanPreviewContent(chat)

  const planContent =
    attachedPlanId && planQuery.data ? planQuery.data.contentMarkdown : messagePlanContent

  const planTitle =
    attachedPlanId && planQuery.data ? planQuery.data.title : extractPlanTitle(messagePlanContent)

  const planId = attachedPlanId ?? planQuery.data?.id ?? null
  const isFromApi = Boolean(attachedPlanId && planQuery.data)

  const openPanel = useCallback(() => {
    userClosedRef.current = false
    setIsOpen(true)
    planPanelRef.current?.expand()
  }, [planPanelRef])

  const closePanel = useCallback(() => {
    userClosedRef.current = true
    setIsOpen(false)
    planPanelRef.current?.collapse()
  }, [planPanelRef])

  const togglePanel = useCallback(() => {
    if (planPanelRef.current?.isCollapsed()) {
      openPanel()
      return
    }

    closePanel()
  }, [closePanel, openPanel, planPanelRef])

  const checkIsPlanResponseMessage = useCallback(
    (message: ChatMessage) => isPlanResponseMessage(chat, message),
    [chat]
  )

  useEffect(() => {
    userClosedRef.current = false
    setIsOpen(false)
  }, [chat.id])

  useEffect(() => {
    if (userClosedRef.current || !hasPlanContent) {
      return
    }

    if (attachedPlanId || isStreaming || planContent.length > 0) {
      openPanel()
    }
  }, [attachedPlanId, hasPlanContent, isStreaming, openPanel, planContent.length])

  return {
    hasPlanContent,
    isOpen,
    planContent,
    planTitle,
    isStreaming,
    planId,
    isFromApi,
    isLoadingPlan: Boolean(attachedPlanId) && planQuery.isLoading,
    planPanelRef: planPanelRef as RefObject<PanelImperativeHandle | null>,
    openPanel,
    closePanel,
    togglePanel,
    isPlanResponseMessage: checkIsPlanResponseMessage
  }
}
