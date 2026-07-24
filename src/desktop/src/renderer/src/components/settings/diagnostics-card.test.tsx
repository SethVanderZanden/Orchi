import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { toast } from 'sonner'

import { DiagnosticsCard } from '@/components/settings/diagnostics-card'

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn()
  }
}))

describe('DiagnosticsCard', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('opens the log folder via the desktop API', async () => {
    render(<DiagnosticsCard />)

    fireEvent.click(screen.getByRole('button', { name: 'Open log folder' }))

    await waitFor(() => {
      expect(window.api.openLogFolder).toHaveBeenCalledTimes(1)
      expect(toast.success).toHaveBeenCalledWith('Log folder opened')
    })
  })

  it('shows an error toast when opening fails', async () => {
    vi.mocked(window.api.openLogFolder).mockRejectedValueOnce(new Error('no folder'))

    render(<DiagnosticsCard />)

    fireEvent.click(screen.getByRole('button', { name: 'Open log folder' }))

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith('Could not open the log folder')
    })
  })
})
