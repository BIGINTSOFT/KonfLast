/** Sunucu sözleşmeleri (ERP.Api). Durum kodları sunucudan geldiği gibi kullanılır. */

export type MachineStatus = 'AVAILABLE' | 'RUNNING' | 'IDLE' | 'BREAKDOWN' | 'MAINTENANCE' | 'OFFLINE';

export type BundleOperationStatus =
  | 'WAITING'
  | 'READY'
  | 'IN_PROGRESS'
  | 'PAUSED'
  | 'COMPLETED'
  | 'QUALITY_HOLD'
  | 'CANCELLED';

export interface TokenResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
}

/** Tabletin "neredeyim?" bilgisi; eksik halkalar null gelir. */
export interface TabletContext {
  tabletId: number;
  tabletCode: string;
  serverTime: string;
  machine: {
    id: number;
    machineCode: string;
    machineName: string;
    machineTypeName: string;
    status: MachineStatus;
    /** Şu anki durum için kaydedilmiş neden (ör. arıza nedeni). */
    statusReason?: string | null;
  } | null;
  operation: { id: number; operationCode: string; name: string; sam: number } | null;
  line: { id: number; lineCode: string; name: string } | null;
  currentPlan: { orderNo: string; modelCode: string; productName: string; customerName: string; plannedQuantity: number } | null;
  /** "Ara ver" seçenekleri: web'deki Duruş Nedenleri ana verisinden, gösterilecek sırayla. */
  pauseReasons: StopReasonOption[];
  /** "Arıza bildir" seçenekleri: web'deki Duruş Nedenleri ana verisinden, gösterilecek sırayla. */
  breakdownReasons: StopReasonOption[];
  /** Görevlerin ortak yetkileri (web'deki Görev Yetkileri ekranından); kimin kartının isteneceğini belirler. */
  staffRoles?: StaffRolePermissions[];
  /** Oturum açık ve iş yokken operatöre sormadan önce beklenecek süre (dakika); 0 ya da boş: sorulmaz. */
  idlePromptMinutes?: number;
}

export interface StaffRolePermissions {
  role: StaffRole;
  canPause: boolean;
  canReportBreakdown: boolean;
  canResolveBreakdown: boolean;
}

/** Ana veriden gelen duruş nedeni (ara ya da arıza). */
export interface StopReasonOption {
  id: number;
  code: string;
  name: string;
}

/** Operatörün bu makinedeki çalışma oturumu. */
export interface TabletSession {
  /** Operatörün bu makinede zaten açık oturumu vardı. */
  duplicate: boolean;
  sessionId: number;
  operatorId: number;
  operatorCode: string;
  fullName: string;
  loginAt: string;
  /** Operatörün şu anki durumu; ACTIVE dışındakiler bir duraklamadır. */
  status: SessionStatus;
  /** Süren duraklama (varsa). */
  currentPause: { type: PauseType; reasonName: string | null; breakName: string | null; startedAt: string } | null;
  /** Zamanı gelmiş, henüz çıkılmamış planlı mola (varsa). */
  dueBreak: DueBreak | null;
  /** Oturum sahibinin sahadaki görevi. */
  role: StaffRole;
  /** Oturum sahibi kendi yetkisiyle ara verebilir mi (veremiyorsa bant şefinin kartı istenir). */
  canPause: boolean;
  /** Oturum sahibi kendi yetkisiyle arıza bildirebilir mi (bildiremiyorsa yetkili kartı istenir). */
  canReportBreakdown: boolean;
}

export type StaffRole = 'OPERATOR' | 'LINE_CHIEF' | 'MAINTENANCE';

/** Personel fotoğrafı; içerik base64 kodludur. */
export interface OperatorPhoto {
  contentType: string;
  dataBase64: string;
  updatedAt: string;
}

export type SessionStatus = 'ACTIVE' | 'LOGGED_OUT' | 'BREAK' | 'PAUSED' | 'MACHINE_BREAKDOWN' | 'WAITING_FOR_BUNDLE';

/** Tabletten bildirilebilen duraklamalar; makine arızası "Arıza bildir" ile ayrıca bildirilir. */
export type PauseType = 'BREAK' | 'PAUSE' | 'WAITING_FOR_BUNDLE' | 'MACHINE_BREAKDOWN';

export interface DueBreak {
  breakScheduleId: number;
  name: string;
  startTime: string;
  endTime: string;
  plannedMinutes: number;
  policy: string;
  /** Sunucu hesaplar: mola şimdi başlatılabilir mi (önce eldeki iş bildirilmeli olabilir). */
  canStartNow: boolean;
  /** Başlatılamıyorsa operatöre gösterilecek açıklama. */
  hint: string | null;
}

export interface BundleRead {
  duplicate: boolean;
  bundleId: number;
  bundleNo: string;
  orderNo: string;
  modelCode: string;
  colorName: string;
  sizeName: string;
  quantity: number;
  status: string;
  /** Bu makinede işlenecek adım (başlatılabilir ilk adım, yoksa makinenin yapabildiği ilk bitmemiş adım); yoksa null. */
  machineOperation: BundleStep | null;
  /** Demetin rotasındaki tüm adımlar, sırasıyla. */
  operations?: BundleStep[];
}

/** Demetin rotasındaki bir adım. */
export interface BundleStep {
  bundleOperationId: number;
  sequenceNo: number;
  operationName: string;
  status: BundleOperationStatus;
  plannedQuantity: number;
  producedQuantity: number;
  rejectedQuantity: number;
  remainingQuantity: number;
  /** Bu makine operasyonu yapabiliyor mu. */
  machineCanDo?: boolean;
  /** Adım şu an bu makinede başlatılabilir mi (sunucu hesaplar). */
  canStart?: boolean;
  /** Bir adedin standart süresi (dakika). */
  sam?: number;
}

/** Bir demet operasyonunun işlem sonrası durumu. */
export interface OperationProgress {
  duplicate: boolean;
  bundleOperationId: number;
  bundleId: number;
  bundleNo: string;
  bundleStatus: string;
  sequenceNo: number;
  operationName: string;
  status: BundleOperationStatus;
  plannedQuantity: number;
  producedQuantity: number;
  rejectedQuantity: number;
  remainingQuantity: number;
  machineCode: string | null;
  operatorId: number | null;
  operatorName: string | null;
  startedAt: string | null;
  completedAt: string | null;
  /** Demette şu an başlatılabilecek operasyonlar. */
  nextOperations: string[];
  /** Bir adedin standart süresi (dakika). */
  sam?: number;
  /** İşlemdeki operasyonda kalan adedin hedef süresi (saniye). */
  runTargetSeconds?: number | null;
  /** Son başlatmadan bu yana geçen net süre (saniye); mola, ara ve arıza düşülmüştür. Sunucu hesaplar. */
  runElapsedSeconds?: number | null;
  /** Yalnızca adet bildirimi yanıtında: bu işin verimi (%). */
  efficiencyPercent?: number | null;
}

export interface MachineStatusInfo {
  machineId: number;
  machineCode: string;
  status: MachineStatus;
  statusReason?: string | null;
}

/** Performans sayfasının dönemi (fabrika saatiyle): bugün ya da bu haftanın pazartesisinden beri. */
export type PerformancePeriod = 'TODAY' | 'WEEK';

/** Aynı hesap kıyas döneminde (ör. dün, son 7 gün, geçen hafta). */
export interface PerformanceComparison {
  label: string;
  efficiencyPercent: number | null;
  qualityPercent: number | null;
}

/** Dönemin bir dilimi (bugün için saat, hafta için gün) ve o dilimde bildirilen adet. */
export interface PerformanceBucket {
  label: string;
  produced: number;
  rejected: number;
  /** Şu an içinde bulunulan dilim. */
  current: boolean;
}

/** Oturumu açık operatörün performansı (sunucunun performans hesabı; web'deki raporla aynı). */
export interface TabletPerformance {
  operatorId: number;
  fullName: string;
  period: PerformancePeriod;
  from: string;
  to: string;
  /** Kazanılan dakika ÷ (net çalışma − demet bekleme); %100'ü aşabilir. */
  efficiencyPercent: number | null;
  /** Sağlam adet ÷ (sağlam + hatalı). */
  qualityPercent: number | null;
  produced: number;
  rejected: number;
  /** %100 verimde aynı sürede üretilmesi gereken adet. */
  targetQuantity: number | null;
  producedPerHour: number | null;
  /** %100 verimde saatte üretilmesi gereken adet (grafikteki hedef çizgisi). */
  targetPerHour: number | null;
  netWorkingMinutes: number;
  waitingMinutes: number;
  /** Üretilen adet × birim süre. */
  earnedMinutes: number;
  /** Net çalışma − demet bekleme (verimin paydası). */
  availableMinutes: number;
  comparisons: PerformanceComparison[];
  buckets: PerformanceBucket[];
}
