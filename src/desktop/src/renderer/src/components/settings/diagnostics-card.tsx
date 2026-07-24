import { useState } from 'react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { logger } from '@/lib/logger'

export function DiagnosticsCard(): React.JSX.Element {
  const [opening, setOpening] = useState(false)

  const openLogFolder = async (): Promise<void> => {
    setOpening(true)
    try {
      const result = await window.api.openLogFolder()
      logger.info('Opened log folder', result)
      toast.success('Log folder opened')
    } catch (error) {
      logger.error('Failed to open log folder', error)
      toast.error('Could not open the log folder')
    } finally {
      setOpening(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Diagnostics</CardTitle>
        <CardDescription>
          App logs help debug blank screens and crashes. Main and renderer messages go to the same
          file.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        <p className="text-sm leading-relaxed text-muted-foreground">
          If the window turns white after sitting idle, Orchi tries to reload automatically. If it
          keeps happening, grab the latest log and share it when reporting the issue.
        </p>
        <Button type="button" variant="secondary" disabled={opening} onClick={() => void openLogFolder()}>
          {opening ? 'Opening…' : 'Open log folder'}
        </Button>
      </CardContent>
    </Card>
  )
}
