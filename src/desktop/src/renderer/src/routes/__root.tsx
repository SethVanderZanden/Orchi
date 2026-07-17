import { createRootRoute, Outlet } from '@tanstack/react-router'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { TanStackRouterDevtools } from '@tanstack/react-router-devtools'
import { QueryClientProvider } from '@tanstack/react-query'

import { ErrorBoundary } from '@/components/error-boundary'
import { Toaster } from '@/components/ui/sonner'
import { queryClient } from '@/lib/query-client'
import { ThemeProvider } from '@/providers/theme-provider'

export const Route = createRootRoute({
  component: RootComponent
})

function RootComponent(): React.JSX.Element {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <ErrorBoundary>
          <Outlet />
        </ErrorBoundary>
        <Toaster richColors closeButton position="top-right" />
        {import.meta.env.DEV && (
          <>
            <ReactQueryDevtools buttonPosition="bottom-left" />
            <TanStackRouterDevtools position="bottom-right" />
          </>
        )}
      </QueryClientProvider>
    </ThemeProvider>
  )
}
