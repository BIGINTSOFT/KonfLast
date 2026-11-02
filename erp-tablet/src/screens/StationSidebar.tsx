import type { MachineStatus, SessionStatus, StaffRole } from '../api/types';
import type { StationState } from '../station/StationController';
import { BigButton } from '../ui/controls';
import { SidebarOperator, SidebarSignedOut, StationSidebarFrame } from '../ui/StationSidebarFrame';
import { formatClock, formatElapsed } from '../util/time';

interface StationSidebarProps {
  state: StationState;
  tabletCode: string;
  now: Date;
  /** Markaya uzun basılınca sistem ayarları açılır (yönetici işlemi). */
  onOpenSetup: () => void;
  onSignOut: () => void;
}

const machineStatus: Record<MachineStatus, { label: string; color: string }> = {
  AVAILABLE: { label: 'Müsait', color: '#3ecf8e' },
  RUNNING: { label: 'Çalışıyor', color: '#4fc3d9' },
  IDLE: { label: 'Boşta', color: '#aab6c7' },
  BREAKDOWN: { label: 'Arızalı', color: '#ff8a80' },
  MAINTENANCE: { label: 'Bakımda', color: '#f0bd5b' },
  OFFLINE: { label: 'Çevrimdışı', color: '#aab6c7' },
};

const sessionStatus: Record<SessionStatus, { label: string; color: string }> = {
  ACTIVE: { label: 'Çalışıyor', color: '#3ecf8e' },
  BREAK: { label: 'Molada', color: '#f0bd5b' },
  PAUSED: { label: 'Ara verdi', color: '#f0bd5b' },
  WAITING_FOR_BUNDLE: { label: 'Demet bekliyor', color: '#4fc3d9' },
  MACHINE_BREAKDOWN: { label: 'Makine arızalı', color: '#ff8a80' },
  LOGGED_OUT: { label: 'Oturum kapalı', color: '#aab6c7' },
};

export const staffRoleLabels: Record<StaffRole, string> = {
  OPERATOR: 'Operatör',
  LINE_CHIEF: 'Bant Şefi',
  MAINTENANCE: 'Bakım Operatörü',
};

/** Sol panel: marka, istasyon (makine / operasyon / bant) ve oturumu açık personel. */
export function StationSidebar({ state, tabletCode, now, onOpenSetup, onSignOut }: StationSidebarProps) {
  const { context, session } = state;
  const machine = context?.machine;
  const loginAt = session ? new Date(session.loginAt) : null;

  return (
    <StationSidebarFrame
      moduleName="Konfeksiyon"
      title={machine ? machine.machineCode : tabletCode}
      subtitle={machine ? context?.operation?.name ?? 'Operasyon tanımsız' : undefined}
      meta={[context?.line?.name, machine?.machineName, tabletCode].filter(Boolean).join(' · ')}
      status={machine ? { label: `Makine: ${machineStatus[machine.status].label}`, color: machineStatus[machine.status].color } : undefined}
      statusReason={machine?.status === 'BREAKDOWN' ? machine.statusReason : null}
      onOpenSetup={onOpenSetup}
      footer={
        session && !state.job && !state.needsEnrollment ? (
          <BigButton
            label="Oturumu kapat"
            tone="ghost"
            size="compact"
            disabled={state.busy}
            onPress={onSignOut}
            testID="station-sign-out"
          />
        ) : undefined
      }>
      {session && loginAt ? (
        <SidebarOperator
          name={session.fullName}
          role={staffRoleLabels[session.role] ?? staffRoleLabels.OPERATOR}
          code={session.operatorCode}
          status={sessionStatus[session.status]}
          photoUri={state.operatorPhoto}
          facts={[
            { label: 'Giriş saati', value: formatClock(loginAt), testID: 'operator-login-at' },
            { label: 'Oturum süresi', value: formatElapsed(now.getTime() - loginAt.getTime()), testID: 'operator-elapsed' },
          ]}
        />
      ) : (
        <SidebarSignedOut hint="Çalışmaya başlamak için personel kartınızı okutun." />
      )}
    </StationSidebarFrame>
  );
}
