import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'

import { MarkdownContent } from './markdown-content'

describe('MarkdownContent open-editor links', () => {
  it('renders open-editor blocks as clickable links', async () => {
    const openInEditor = vi.fn().mockResolvedValue({ ok: true })
    window.api = {
      ...window.api,
      openInEditor
    }

    render(
      <MarkdownContent>
        {`See <orchi-open-editor>code /workspace/project -g src/file.ts:42</orchi-open-editor> for details.`}
      </MarkdownContent>
    )

    const link = screen.getByRole('button', { name: 'src/file.ts:42' })
    fireEvent.click(link)

    expect(openInEditor).toHaveBeenCalledWith('/workspace/project', expect.any(String), {
      relativePath: 'src/file.ts',
      line: 42
    })
  })
})
