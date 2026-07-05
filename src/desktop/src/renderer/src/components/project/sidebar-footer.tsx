import { FolderPlus, Settings } from 'lucide-react'

import { SidebarNavItem } from '@/components/layout/sidebar/sidebar-nav-item'
import { sidebarIconClass } from '@/components/layout/sidebar/sidebar-utils'

type SidebarFooterProps = {
  isSettingsActive: boolean
  onOpenSettings: () => void
  onAddProject: () => void
  isAddingProject: boolean
}

export function SidebarFooter({
  isSettingsActive,
  onOpenSettings,
  onAddProject,
  isAddingProject
}: SidebarFooterProps): React.JSX.Element {
  return (
    <div className="shrink-0 space-y-0.5 px-3 pb-3 pt-2">
      <SidebarNavItem
        type="button"
        isActive={isSettingsActive}
        aria-current={isSettingsActive ? 'page' : undefined}
        leading={<Settings className={sidebarIconClass(isSettingsActive)} />}
        onClick={onOpenSettings}
      >
        Settings
      </SidebarNavItem>
      <SidebarNavItem
        type="button"
        leading={<FolderPlus className={sidebarIconClass(false)} />}
        onClick={onAddProject}
        disabled={isAddingProject}
      >
        Add project
      </SidebarNavItem>
    </div>
  )
}
