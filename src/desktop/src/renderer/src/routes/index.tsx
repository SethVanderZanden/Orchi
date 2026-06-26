import { createFileRoute } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '@/components/ui/table'
import { fetchWeatherForecast } from '@/lib/api'
import { weatherKeys } from '@/lib/query-keys'

export const Route = createFileRoute('/')({
  component: IndexPage
})

function IndexPage(): React.JSX.Element {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: weatherKeys.forecast(),
    queryFn: fetchWeatherForecast
  })

  const errorMessage =
    error instanceof Error
      ? `${error.message}. Is the API running on http://localhost:5265?`
      : 'Failed to load weather data. Is the API running on http://localhost:5265?'

  return (
    <div className="mx-auto w-full max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Orchi</h1>
        <p className="text-muted-foreground text-sm">Weather forecast from the API</p>
      </div>

      {isLoading && <p className="text-muted-foreground text-sm">Loading forecast...</p>}

      {isError && (
        <p className="text-destructive text-sm" role="alert">
          {errorMessage}
        </p>
      )}

      {data && (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Date</TableHead>
                <TableHead>Temp (°C)</TableHead>
                <TableHead>Temp (°F)</TableHead>
                <TableHead>Summary</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.map((forecast) => (
                <TableRow key={forecast.date}>
                  <TableCell>{forecast.date}</TableCell>
                  <TableCell>{forecast.temperatureC}</TableCell>
                  <TableCell>{forecast.temperatureF}</TableCell>
                  <TableCell>{forecast.summary ?? '—'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}
