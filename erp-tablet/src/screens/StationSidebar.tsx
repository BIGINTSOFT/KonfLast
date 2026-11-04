import type { SessionStatus, StaffRole, TabletPerformance } from '../api/types';
import type { StationState } from '../station/StationController';
import { BigButton } from '../ui/controls';
import { SidebarOperator, SidebarPerformance, SidebarSignedOut, StationSidebarFrame } from '../ui/StationSidebarFrame';
import { formatClock, formatElapsed } from '../util/time';

interface StationSidebarProps {
  state: StationState;
  tabletCode: string;
  now: Date;
  /** Markaya uzun basılınca sistem ayarları açılır (yönetici işlemi). */
  onOpenSetup: () => void;
  onSignOut: () => void;
  /** Oturumu açık personelin bugünkü performansı; henüz alınmadıysa null. */
  todayPerformance?: TabletPerformance | null;
}

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
export function StationSidebar({ state, tabletCode, now, onOpenSetup, onSignOut, todayPerformance }: StationSidebarProps) {
  const { context, session } = state;
  const machine = context?.machine;
  const loginAt = session ? new Date(session.loginAt) : null;

  return (
    <StationSidebarFrame
      moduleName="Konfeksiyon"
      title={machine ? machine.machineCode : tabletCode}
      subtitle={machine ? context?.operation?.name ?? 'Operasyon tanımsız' : undefined}
      details={[
        ...(context?.line?.name ? [{ label: 'Bant', value: context.line.name }] : []),
        ...(machine?.machineName ? [{ label: 'Makine', value: machine.machineName }] : []),
        { label: 'Tablet', value: tabletCode },
        ...(context?.currentPlan ? [{ label: 'Plan', value: `${context.currentPlan.orderNo} · ${context.currentPlan.modelCode}` }] : []),
      ]}
      // Makine durumu etiketi gösterilmez: arıza üst çubukta ("Arızayı kapat") ve iş alanında nedeniyle görünür.
      onOpenSetup={onOpenSetup}
      // Düğme hep yerinde durur (alanlar kaymaz); oturum yokken ya da süren iş varken pasiftir.
      footer={
        <BigButton
          label="Oturumu kapat"
          tone="ghost"
          size="compact"
          disabled={state.busy || !session || state.job !== null || state.needsEnrollment}
          onPress={onSignOut}
          testID="station-sign-out"
        />
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
          ]}>
          {/* Performans sayfasına geçmeden bir bakışta görülür. */}
          <SidebarPerformance
            efficiencyPercent={todayPerformance?.efficiencyPercent ?? null}
            qualityPercent={todayPerformance?.qualityPercent ?? null}
            produced={todayPerformance?.produced ?? null}
            targetQuantity={todayPerformance?.targetQuantity ?? null}
          />
        </SidebarOperator>
      ) : (
        <SidebarSignedOut hint="Çalışmaya başlamak için personel kartınızı okutun." />
      )}
    </StationSidebarFrame>
  );
}
