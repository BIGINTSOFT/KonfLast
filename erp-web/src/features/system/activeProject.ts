import { useSyncExternalStore } from 'react'
import { activeProjects, useGetSystemModulesQuery } from './systemApi'
import type { ProjectKey } from './systemApi'

const STORAGE_KEY = 'erp.activeProject'
const listeners = new Set<() => void>()

function readStored(): ProjectKey | null {
  try {
    return (localStorage.getItem(STORAGE_KEY) as ProjectKey | null) ?? null
  } catch {
    return null
  }
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

function storeProject(project: ProjectKey) {
  try {
    localStorage.setItem(STORAGE_KEY, project)
  } catch {
    // Tarayıcı depolaması kapalıysa seçim yalnızca bu oturumda geçerli olur.
  }
  listeners.forEach((listener) => listener())
}

/**
 * Üst çubukta seçilen proje (Konfeksiyon, Boyahane, Dokuma). Menüde çekirdek ekranlarla birlikte yalnızca bu
 * projenin ekranları listelenir. Seçim tarayıcıda saklanır; kurulumda olmayan bir proje seçili kalmışsa
 * devredeki ilk proje kullanılır.
 */
export function useActiveProject(): {
  project: ProjectKey | null
  projects: ProjectKey[]
  select: (project: ProjectKey) => void
} {
  const stored = useSyncExternalStore(subscribe, readStored, () => null)
  const { data } = useGetSystemModulesQuery()
  const projects = activeProjects(data)
  const project = stored !== null && projects.includes(stored) ? stored : (projects[0] ?? null)
  return { project, projects, select: storeProject }
}

/** Rol ya da yetki seçili projede görünür mü? Projesiz (moduleKey boş) olanlar ortaktır. */
export const inProject = (moduleKey: string | null, project: ProjectKey | null) => moduleKey === null || moduleKey === project
