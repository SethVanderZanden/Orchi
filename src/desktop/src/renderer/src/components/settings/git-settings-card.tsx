import { useMutation, useQuery } from '@tanstack/react-query'
import { GitBranch } from 'lucide-react'
import { toast } from 'sonner'

import { EmptyState } from '@/components/empty-state'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect } from '@/components/ui/native-select'
import { requestOpenBranchReview } from '@/lib/branch-review/events'
import { getDefaultWorkspace } from '@/lib/projects/group-chats'
import { listProjectBranches } from '@/lib/projects/api'
import type { GitHostProvider } from '@/lib/projects/types'
import { getGitHostReadiness, listGitHostProviders } from '@/lib/scripts/api'
import { gitHostingKeys, projectBranchKeys } from '@/lib/query-keys'
import { useProjects } from '@/providers/project-provider'

function readinessVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  if (status === 'ready') {
    return 'default'
  }
  if (status === 'missingCli' || status === 'notAuthenticated') {
    return 'destructive'
  }
  return 'secondary'
}

export function GitSettingsCard(): React.JSX.Element {
  const { projects, updateProjectSettings } = useProjects()

  const providersQuery = useQuery({
    queryKey: gitHostingKeys.providers(),
    queryFn: listGitHostProviders
  })

  const updateMutation = useMutation({
    mutationFn: ({
      projectId,
      ...request
    }: {
      projectId: string
      defaultBaseBranch?: string
      defaultWorktreeBranchPattern?: string
      gitHostProvider?: GitHostProvider
      useWorktreeOnKickoff?: boolean
    }) => updateProjectSettings(projectId, request),
    onError: (error: Error) => toast.error(error.message)
  })

  if (projects.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Git</CardTitle>
          <CardDescription>
            Configure git host CLI readiness and default base branches per project.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <EmptyState
            className="py-6"
            title="No projects"
            description="Add a project first, then choose GitHub or Azure DevOps."
            icon={<GitBranch className="size-8" />}
          />
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Git</CardTitle>
        <CardDescription>
          Pick GitHub (gh) or Azure DevOps (az) per project. Host CLIs must be installed and
          authenticated before pull requests can run.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {providersQuery.data ? (
          <ul className="space-y-1 text-xs text-muted-foreground">
            {providersQuery.data.map((provider) => (
              <li key={provider.providerId}>
                {provider.displayName}: {provider.configureHint}
              </li>
            ))}
          </ul>
        ) : null}

        {projects.map((project) => (
          <ProjectGitSettings
            key={project.id}
            projectId={project.id}
            name={project.name}
            defaultBaseBranch={project.defaultBaseBranch}
            defaultWorktreeBranchPattern={project.defaultWorktreeBranchPattern}
            gitHostProvider={project.gitHostProvider}
            useWorktreeOnKickoff={project.useWorktreeOnKickoff}
            workspacePath={getDefaultWorkspace(project)?.path}
            onChange={(request) => updateMutation.mutate({ projectId: project.id, ...request })}
          />
        ))}
      </CardContent>
    </Card>
  )
}

function ProjectGitSettings({
  projectId,
  name,
  defaultBaseBranch,
  defaultWorktreeBranchPattern,
  gitHostProvider,
  useWorktreeOnKickoff,
  workspacePath,
  onChange
}: {
  projectId: string
  name: string
  defaultBaseBranch: string
  defaultWorktreeBranchPattern: string
  gitHostProvider: GitHostProvider
  useWorktreeOnKickoff: boolean
  workspacePath?: string
  onChange: (request: {
    defaultBaseBranch?: string
    defaultWorktreeBranchPattern?: string
    gitHostProvider?: GitHostProvider
    useWorktreeOnKickoff?: boolean
  }) => void
}): React.JSX.Element {
  const readinessQuery = useQuery({
    queryKey: gitHostingKeys.readiness(projectId),
    queryFn: () =>
      getGitHostReadiness({
        projectId,
        workspacePath,
        provider: gitHostProvider
      })
  })

  const branchesQuery = useQuery({
    queryKey: projectBranchKeys.list(projectId),
    queryFn: () => listProjectBranches(projectId),
    retry: false
  })

  const readiness = readinessQuery.data

  return (
    <div className="space-y-3 rounded-lg border p-3">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-medium">{name}</p>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            className="h-7 gap-1.5 px-2 text-xs font-normal"
            onClick={() => requestOpenBranchReview({ projectId })}
          >
            <GitBranch className="size-3.5" />
            Review branch
          </Button>
          {readiness ? (
            <Badge variant={readinessVariant(readiness.status)}>{readiness.status}</Badge>
          ) : null}
        </div>
      </div>
      {readiness ? <p className="text-xs text-muted-foreground">{readiness.message}</p> : null}

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label htmlFor={`host-${projectId}`}>Host</Label>
          <NativeSelect
            id={`host-${projectId}`}
            value={gitHostProvider}
            onChange={(change) =>
              onChange({ gitHostProvider: change.target.value as GitHostProvider })
            }
          >
            <option value="github">GitHub</option>
            <option value="azureDevOps">Azure DevOps</option>
          </NativeSelect>
        </div>
        <div className="space-y-2">
          <Label htmlFor={`branch-${projectId}`}>Base branch</Label>
          {branchesQuery.data && branchesQuery.data.length > 0 ? (
            <NativeSelect
              id={`branch-${projectId}`}
              value={defaultBaseBranch}
              onChange={(change) => onChange({ defaultBaseBranch: change.target.value })}
            >
              {branchesQuery.data.map((branch) => (
                <option key={branch.name} value={branch.name}>
                  {branch.name}
                  {branch.isCurrent ? ' (current)' : ''}
                </option>
              ))}
            </NativeSelect>
          ) : (
            <Input
              id={`branch-${projectId}`}
              value={defaultBaseBranch}
              onChange={(change) => onChange({ defaultBaseBranch: change.target.value })}
              placeholder="main"
            />
          )}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor={`worktree-pattern-${projectId}`}>Worktree branch pattern</Label>
        <Input
          id={`worktree-pattern-${projectId}`}
          value={defaultWorktreeBranchPattern}
          onChange={(change) => onChange({ defaultWorktreeBranchPattern: change.target.value })}
          placeholder="orchi/{date}-{shortId}"
        />
        <p className="text-xs text-muted-foreground">
          Tokens: {'{date}'}, {'{time}'}, {'{shortId}'}, {'{chatId}'}, {'{mode}'}. Used when a
          worktree is created from chat or an AgentStart script.
        </p>
      </div>

      <div className="flex items-center justify-between gap-3">
        <div className="space-y-0.5">
          <p className="text-sm font-medium">Worktree on plan kickoff</p>
          <p className="text-xs text-muted-foreground">
            Create an isolated worktree workspace for each implementation plan.
          </p>
        </div>
        <Button
          size="sm"
          variant={useWorktreeOnKickoff ? 'default' : 'outline'}
          onClick={() => onChange({ useWorktreeOnKickoff: !useWorktreeOnKickoff })}
        >
          {useWorktreeOnKickoff ? 'On' : 'Off'}
        </Button>
      </div>
    </div>
  )
}
