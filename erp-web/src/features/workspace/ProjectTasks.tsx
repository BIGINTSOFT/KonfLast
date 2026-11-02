import { ChevronRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { navIcon } from '../../layouts/navPresentation'
import { Permissions } from '../../types/permissions'
import { useGetBoyahaneDashboardQuery } from '../boyahane/boyahaneInsightsApi'
import { useGetDokumaDashboardQuery } from '../dokuma/dokumaDashboardApi'
import { useGetOrmeDashboardQuery } from '../orme/ormeDashboardApi'
import type { ProjectKey } from '../system/systemApi'

export interface TaskRow {
  key: string
  title: string
  description: string
  count: number
  path: string
}

/** Bekleyen işler: tıklayınca işin yapılacağı ekran açılır. */
export function TaskList({ tasks }: { tasks: TaskRow[] }) {
  if (tasks.length === 0) {
    return <p className="muted">Şu an bekleyen iş yok.</p>
  }
  return (
    <div className="task-list">
      {tasks.map((task) => {
        const TaskIcon = navIcon(task.path)
        return (
          <Link key={task.key} to={task.path} className="task">
            <span className="task__icon"><TaskIcon aria-hidden="true" /></span>
            <span className="task__body">
              <strong>{task.title}</strong>
              <span className="muted">{task.description}</span>
            </span>
            <span className="task__count">{task.count}</span>
            <ChevronRight className="task__go" aria-hidden="true" />
          </Link>
        )
      })}
    </div>
  )
}

const pollingInterval = 60_000

function DokumaTasks() {
  const { data, error, isLoading } = useGetDokumaDashboardQuery(undefined, { pollingInterval })
  const canWeave = usePermission(Permissions.DokumaWeavingView)
  const canWarp = usePermission(Permissions.DokumaWarpView)
  const canInspect = usePermission(Permissions.DokumaInspectionView)
  const canPack = usePermission(Permissions.DokumaPackingView)
  const canOrder = usePermission(Permissions.DokumaOrderView)
  const tasks: (TaskRow | false)[] = data ? [
    canWeave && { key: 'stops', title: 'Duran tezgâhlar', description: 'Açık duruş ya da arıza kaydı olan tezgâhlar', count: data.stoppedLooms + data.breakdownLooms, path: '/dokuma/weaving' },
    canOrder && { key: 'late', title: 'Geciken siparişler', description: 'Termini geçmiş, sevki tamamlanmamış siparişler', count: data.lateOrderCount, path: '/dokuma/orders' },
    canInspect && { key: 'inspection', title: 'Kontrol bekleyen toplar', description: 'Ham kontrol sonucu girilmemiş toplar', count: data.pendingInspectionRolls, path: '/dokuma/inspection' },
    canPack && { key: 'packing', title: 'Sevk bekleyen çeki listeleri', description: 'Onaylanmış, henüz sevk edilmemiş çeki listeleri', count: data.packingListsToShip, path: '/dokuma/packing-lists' },
    canWarp && { key: 'beams', title: 'Tezgâha hazır leventler', description: 'Tezgâha bağlanmayı bekleyen leventler', count: data.readyBeams, path: '/dokuma/beams' },
  ] : []
  return <TaskSection error={error} isLoading={isLoading} tasks={tasks} />
}

function OrmeTasks() {
  const { data, error, isLoading } = useGetOrmeDashboardQuery(undefined, { pollingInterval })
  const canKnit = usePermission(Permissions.OrmeKnittingView)
  const canInspect = usePermission(Permissions.OrmeInspectionView)
  const canPack = usePermission(Permissions.OrmePackingView)
  const canOrder = usePermission(Permissions.OrmeOrderView)
  const tasks: (TaskRow | false)[] = data ? [
    canKnit && { key: 'stops', title: 'Duran makineler', description: 'Açık duruş ya da arıza kaydı olan makineler', count: data.stoppedMachines + data.breakdownMachines, path: '/orme/machine-board' },
    canOrder && { key: 'late', title: 'Geciken siparişler', description: 'Termini geçmiş, sevki tamamlanmamış siparişler', count: data.lateOrderCount, path: '/orme/orders' },
    canInspect && { key: 'inspection', title: 'Kontrol bekleyen toplar', description: 'Ham kontrol sonucu girilmemiş toplar', count: data.pendingInspectionRolls, path: '/orme/inspection' },
    canPack && { key: 'packing', title: 'Sevk bekleyen çeki listeleri', description: 'Onaylanmış, henüz sevk edilmemiş çeki listeleri', count: data.packingListsToShip, path: '/orme/packing-lists' },
  ] : []
  return <TaskSection error={error} isLoading={isLoading} tasks={tasks} />
}

function BoyahaneTasks() {
  const { data, error, isLoading } = useGetBoyahaneDashboardQuery(undefined, { pollingInterval })
  const tasks: (TaskRow | false)[] = data ? [
    { key: 'down', title: 'Arızalı makineler', description: 'Arıza kaydı açık olan makineler', count: data.machinesDown, path: '/boyahane/dashboard' },
    { key: 'planned', title: 'Planlanan partiler', description: 'Makineye alınmayı bekleyen partiler', count: data.planned, path: '/boyahane/batches' },
  ] : []
  return <TaskSection error={error} isLoading={isLoading} tasks={tasks} />
}

function TaskSection({ error, isLoading, tasks }: { error: unknown; isLoading: boolean; tasks: (TaskRow | false)[] }) {
  return (
    <>
      <ErrorAlert error={error} />
      {isLoading && <p role="status">Yükleniyor…</p>}
      {!isLoading && !error && <TaskList tasks={tasks.filter((task): task is TaskRow => task !== false && task.count > 0)} />}
    </>
  )
}

/** Dokuma, Örme ve Boyahane'nin ana sayfa işleri; o projenin panosunu görme yetkisi olmayana gösterilmez. */
export function ProjectTasks({ project }: { project: ProjectKey | null }) {
  const canDokuma = usePermission(Permissions.DokumaView)
  const canOrme = usePermission(Permissions.OrmeView)
  const canBoyahane = usePermission(Permissions.BoyahaneBatchView)
  const content = project === 'Dokuma' && canDokuma ? <DokumaTasks />
    : project === 'Orme' && canOrme ? <OrmeTasks />
    : project === 'Boyahane' && canBoyahane ? <BoyahaneTasks />
    : null
  return content && (
    <>
      <h2 className="section-title">Yapılacaklar</h2>
      {content}
    </>
  )
}
