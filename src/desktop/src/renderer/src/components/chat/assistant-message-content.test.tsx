import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { AssistantMessageContent } from './assistant-message-content'

describe('AssistantMessageContent', () => {
  it('renders plain text while streaming', () => {
    render(
      <AssistantMessageContent
        content={'# Heading\n\n**bold**'}
        status="streaming"
        className="prose-base"
      />
    )

    expect(screen.getByText(/# Heading/)).toBeInTheDocument()
    expect(screen.queryByRole('heading', { level: 1 })).not.toBeInTheDocument()
  })

  it('renders markdown when complete', () => {
    render(
      <AssistantMessageContent content={'# Heading'} status="complete" className="prose-base" />
    )

    expect(screen.getByRole('heading', { level: 1, name: 'Heading' })).toBeInTheDocument()
  })
})
