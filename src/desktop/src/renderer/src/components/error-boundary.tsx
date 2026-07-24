import { Component, Fragment, type ErrorInfo, type ReactNode } from 'react'

import { Button } from '@/components/ui/button'
import { logger } from '@/lib/logger'

type Props = { children: ReactNode; fallback?: ReactNode }
type State = { error: Error | null; retryKey: number }

export class ErrorBoundary extends Component<Props, State> {
  state = { error: null as Error | null, retryKey: 0 }

  static getDerivedStateFromError(error: Error): Partial<State> {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    logger.error('Renderer React error boundary', error, info.componentStack)
  }

  render(): ReactNode {
    if (this.state.error) {
      return (
        this.props.fallback ?? (
          <div className="flex h-full min-h-[50vh] items-center justify-center bg-background p-8 text-foreground">
            <div className="max-w-md text-center">
              <h1 className="text-lg font-semibold">Something went wrong</h1>
              <p className="mt-2 text-sm text-muted-foreground">{this.state.error.message}</p>
              <p className="mt-2 text-xs text-muted-foreground">
                Details were written to the app log. Open Settings → Diagnostics if you need the
                file.
              </p>
              <Button
                type="button"
                className="mt-4"
                onClick={() => this.setState({ error: null, retryKey: this.state.retryKey + 1 })}
              >
                Try again
              </Button>
            </div>
          </div>
        )
      )
    }
    return <Fragment key={this.state.retryKey}>{this.props.children}</Fragment>
  }
}
