import { Component, Fragment, type ErrorInfo, type ReactNode } from 'react'

type Props = { children: ReactNode; fallback?: ReactNode }
type State = { error: Error | null; retryKey: number }

export class ErrorBoundary extends Component<Props, State> {
  state = { error: null as Error | null, retryKey: 0 }

  static getDerivedStateFromError(error: Error) {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Renderer error:', error, info.componentStack)
  }

  render() {
    if (this.state.error) {
      return (
        this.props.fallback ?? (
          <div className="flex h-full items-center justify-center p-8">
            <div className="text-center">
              <h1 className="text-lg font-semibold">Something went wrong</h1>
              <p className="mt-2 text-sm text-muted-foreground">{this.state.error.message}</p>
              <button
                type="button"
                className="mt-4 text-primary underline"
                onClick={() => this.setState({ error: null, retryKey: this.state.retryKey + 1 })}
              >
                Try again
              </button>
            </div>
          </div>
        )
      )
    }
    return <Fragment key={this.state.retryKey}>{this.props.children}</Fragment>
  }
}
