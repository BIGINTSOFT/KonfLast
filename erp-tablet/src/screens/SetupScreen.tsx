import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { checkServer } from '../api/client';
import { DEFAULT_API_ORIGIN } from '../api/config';
import { isRetryable } from '../api/http';
import { getDeviceIdentifier, loadEnrolledDevice, normalizeOrigin, saveEnrolledDevice } from '../device/deviceConfig';
import type { EnrolledDevice } from '../device/deviceConfig';
import { fetchStations, pairStation, supportsPairing } from '../device/pairing';
import type { PairingStation } from '../device/pairing';
import { fetchProjects, PROJECT_LABELS, PROJECTS, saveProject } from '../device/project';
import type { ProjectKey, ProjectSetting } from '../device/project';
import type { KeyValueStore, SecretStore } from '../storage/stores';
import { Banner, BigButton } from '../ui/controls';
import { colors, radius, TOUCH_HEIGHT, TOUCH_HEIGHT_COMPACT } from '../ui/theme';

interface SetupScreenProps {
  store: KeyValueStore;
  secretsFor: (project: ProjectKey) => SecretStore;
  /** Girişte doğrulanan parola; sunucu eşleştirmede yeniden doğrular. */
  password: string;
  /** Tabletin açtığı proje arayüzü (daha önce seçildiyse). */
  project?: ProjectSetting | null;
  /** Proje seçimi kaydedildi; projenin eşleştirmesi varsa onunla açılır. */
  onApply: (project: ProjectSetting, device: EnrolledDevice | null) => void;
  /** Mevcut ayarlar korunarak geri dönülebiliyorsa. */
  onCancel?: () => void;
}

type Section = 'server' | 'project' | 'machine';
type Message = { kind: 'error' | 'success' | 'info'; text: string };
/** Sunucu sınaması: bilinmiyor, sınanıyor, bağlı, bağlanamadı. */
type ServerState = 'unknown' | 'checking' | 'online' | 'failed';

const SECTIONS: { key: Section; label: string; icon: (color: string) => ReactNode }[] = [
  { key: 'server', label: 'Sunucu', icon: color => <ServerIcon color={color} /> },
  { key: 'project', label: 'Proje', icon: color => <ProjectIcon color={color} /> },
  { key: 'machine', label: 'Makine', icon: color => <MachineIcon color={color} /> },
];

const UNREACHABLE = 'Sunucuya ulaşılamıyor. Adresi ve Wi-Fi bağlantısını kontrol edin.';
const INVALID_ADDRESS = 'Sunucu adresi geçersiz. Örnek: http://192.168.1.10:5272';

const errorText = (error: unknown) =>
  isRetryable(error) ? UNREACHABLE : error instanceof Error ? error.message : 'İşlem yapılamadı.';

const stationLabel = (device: EnrolledDevice | null | undefined) =>
  device ? [device.config.stationCode ?? device.config.tabletCode, device.config.stationName].filter(Boolean).join(' · ') : null;

/**
 * Tabletin sistem ayarları (yönetici tarafından yapılır). Solda kare menü: Sunucu (adres ve bağlantı sınaması),
 * Proje (açılacak arayüz) ve Makine (seçili projenin makine listesinden eşleştirme). Her projenin eşleştirmesi
 * ayrı saklanır; eşleşmiş projeye geçiş tek dokunuştur.
 */
export function SetupScreen({ store, secretsFor, password, project, onApply, onCancel }: SetupScreenProps) {
  const [section, setSection] = useState<Section>('server');
  const [origin, setOrigin] = useState(project?.origin ?? DEFAULT_API_ORIGIN);
  const [selected, setSelected] = useState<ProjectKey>(project?.project ?? 'Konfeksiyon');
  /** Projelerin bu tabletteki eşleştirmeleri. */
  const [devices, setDevices] = useState<Partial<Record<ProjectKey, EnrolledDevice | null>>>({});
  /** Sunucuda devrede olan projeler; bağlantı sınanınca öğrenilir. */
  const [available, setAvailable] = useState<ProjectKey[] | null>(null);
  const [server, setServer] = useState<ServerState>('unknown');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<Message | null>(null);
  /** Kayıtlı ayar; bu ekranda kaydedildiyse `changed` (çıkışta bu ayarla açılır). */
  const [saved, setSaved] = useState<{ setting: ProjectSetting | null; device: EnrolledDevice | null; changed: boolean }>({
    setting: project ?? null,
    device: null,
    changed: false,
  });
  /** Listeden seçilen, henüz kaydedilmemiş makine (null: mevcut eşleştirme kalır). */
  const [pending, setPending] = useState<PairingStation | null>(null);
  /** Listede bu tabletin şu anki istasyonu (açık oturum bilgisiyle); başka istasyon seçilirse o oturum kapanır. */
  const [currentStation, setCurrentStation] = useState<PairingStation | null>(null);
  /** Kaydedince makine listesi yeniden okunur (eşleştirme durumları değişir). */
  const [listVersion, setListVersion] = useState(0);
  const address = normalizeOrigin(origin);
  const device = devices[selected] ?? null;
  const pairable = supportsPairing(selected);

  useEffect(() => {
    let cancelled = false;
    Promise.all(PROJECTS.map(key => loadEnrolledDevice(store, secretsFor(key), key).catch(() => null))).then(loaded => {
      if (!cancelled) {
        setDevices(Object.fromEntries(PROJECTS.map((key, index) => [key, loaded[index]])));
      }
    });
    return () => {
      cancelled = true;
    };
  }, [store, secretsFor]);

  const testConnection = useCallback(async (target: string | null) => {
    if (!target) {
      setServer('failed');
      setMessage({ kind: 'error', text: INVALID_ADDRESS });
      return;
    }

    setServer('checking');
    setMessage(null);
    const status = await checkServer(target);
    const projects = status === 'online' ? await fetchProjects(target).catch(() => null) : null;

    if (status !== 'online') {
      setServer('failed');
      setMessage({ kind: 'error', text: status === 'database-down' ? 'Sunucu yanıt veriyor ama veritabanına ulaşamıyor.' : UNREACHABLE });
      return;
    }

    setServer('online');
    setAvailable(projects);
    setMessage(
      projects && projects.length === 0
        ? { kind: 'error', text: 'Sunucuya bağlanıldı ama devrede proje yok (lisansı kontrol edin).' }
        : { kind: 'success', text: 'Sunucuya bağlanıldı.' },
    );
  }, []);

  // Ekran açılınca kayıtlı adres bir kez sınanır: devredeki projeler hemen görünür.
  useEffect(() => {
    testConnection(normalizeOrigin(project?.origin ?? DEFAULT_API_ORIGIN));
  }, [testConnection, project?.origin]);

  const go = (next: Section) => {
    setSection(next);
    setMessage(null);
  };

  const selectProject = (key: ProjectKey) => {
    setSelected(key);
    setPending(null);
    go(supportsPairing(key) ? 'machine' : 'project');
  };

  /** Seçimleri kaydeder: seçilen makineye eşleştirir, projeyi tabletin açtığı proje yapar. Ekranda kalınır. */
  const save = async () => {
    if (!address) {
      setMessage({ kind: 'error', text: INVALID_ADDRESS });
      return;
    }
    if (pairable && !pending && !device) {
      setSection('machine');
      setMessage({ kind: 'error', text: `${PROJECT_LABELS[selected]} için önce listeden bir makine seçin.` });
      return;
    }

    setBusy(true);
    setMessage(null);
    try {
      const projects = await fetchProjects(address);
      setAvailable(projects);
      if (!projects.includes(selected)) {
        setMessage({ kind: 'error', text: `${PROJECT_LABELS[selected]} bu sunucuda devrede değil (kurulum ya da lisans).` });
        return;
      }

      let enrolled: EnrolledDevice | null = null;
      if (pairable && pending) {
        // Başka cihaza bağlı makine ya da mevcut makinede süren iş varsa uyarısı listede gösterildi; kaydetmek onaydır.
        const paired = await pairStation(
          address, selected, password, await getDeviceIdentifier(store), pending, pending.isPaired,
          currentStation?.sessionWorkInProgress === true,
        );
        enrolled = await saveEnrolledDevice(
          store,
          secretsFor(selected),
          selected,
          { origin: address, tabletCode: paired.tabletCode, stationCode: paired.stationCode, stationName: paired.stationName },
          paired.deviceKey,
        );
      } else if (pairable) {
        enrolled = await loadEnrolledDevice(store, secretsFor(selected), selected, address);
      }

      const setting: ProjectSetting = { project: selected, origin: address };
      await saveProject(store, setting);
      setSaved({ setting, device: enrolled, changed: true });
      setDevices(current => ({ ...current, [selected]: enrolled }));
      setPending(null);
      setListVersion(value => value + 1);
      setMessage({
        kind: 'success',
        text: `Kaydedildi: ${[PROJECT_LABELS[selected], stationLabel(enrolled)].filter(Boolean).join(' · ')}. Çıkışta bu ekran açılır.`,
      });
    } catch (error) {
      setMessage({ kind: 'error', text: errorText(error) });
    } finally {
      setBusy(false);
    }
  };

  /** Kaydedilmemiş seçimleri geri alır. */
  const revert = () => {
    setOrigin(saved.setting?.origin ?? DEFAULT_API_ORIGIN);
    setSelected(saved.setting?.project ?? 'Konfeksiyon');
    setPending(null);
    setMessage({ kind: 'info', text: 'Kaydedilmemiş değişiklikler geri alındı.' });
  };

  /** Çıkış: bu ekranda kaydedildiyse kaydedilen projenin ekranı, değilse önceki ekran açılır. */
  const exit = () => {
    if (saved.changed && saved.setting) {
      onApply(saved.setting, saved.device);
    } else {
      onCancel?.();
    }
  };

  const dirty = pending !== null || selected !== saved.setting?.project || address !== saved.setting?.origin;
  const station = stationLabel(device);
  const chosen = pending ? [pending.code, pending.name].filter(Boolean).join(' · ') : station;
  const header = {
    server: { caption: 'SUNUCU', title: 'Sunucu ve bağlantı', description: 'Tabletin bağlanacağı ERP sunucusunun adresi.' },
    project: { caption: 'PROJE', title: 'Proje seçimi', description: 'Tabletin açacağı arayüz. Seçince makine listesine geçilir; değişiklik Kaydet ile geçerli olur.' },
    machine: {
      caption: 'MAKİNE',
      title: `${PROJECT_LABELS[selected]} makine eşleştirmesi`,
      description: 'Makineyi seçip Kaydet ile eşleştirin.',
    },
  }[section];

  return (
    <View style={styles.root}>
      <View style={styles.rail}>
        <Text style={styles.railTitle}>AYARLAR</Text>
        {SECTIONS.map(item => {
          const active = item.key === section;
          return (
            <Pressable
              key={item.key}
              testID={`setup-section-${item.key}`}
              accessibilityRole="tab"
              accessibilityState={{ selected: active }}
              onPress={() => go(item.key)}
              style={({ pressed }) => [styles.tile, active && styles.tileActive, pressed && !active && styles.tilePressed]}>
              {active && <View style={styles.tileAccent} />}
              {item.icon(active ? colors.headerText : colors.headerMuted)}
              <Text style={[styles.tileLabel, active && styles.tileLabelActive]}>{item.label}</Text>
            </Pressable>
          );
        })}

        {/* Çıkış: kaydedilen projenin ekranına (kaydedilmediyse önceki ekrana) dönülür. */}
        {(onCancel || saved.changed) && (
          <Pressable
            testID="setup-cancel"
            accessibilityRole="button"
            disabled={busy}
            onPress={exit}
            style={({ pressed }) => [styles.tile, styles.exitTile, pressed && styles.exitPressed, busy && styles.tileDisabled]}>
            <View style={styles.exitIcon}>
              <ExitIcon color={colors.headerText} />
            </View>
            <Text style={[styles.tileLabel, styles.tileLabelActive]}>Çıkış</Text>
          </Pressable>
        )}
      </View>

      <View style={styles.main}>
        <View style={styles.header}>
          <Text style={styles.caption}>{header.caption}</Text>
          <Text style={styles.title}>{header.title}</Text>
          <Text style={styles.description}>{header.description}</Text>
        </View>

        <View style={styles.body}>
          {section === 'server' && (
            <ScrollView contentContainerStyle={styles.pane} keyboardShouldPersistTaps="handled">
              {message && <Banner kind={message.kind} text={message.text} testID="setup-message" />}
              <Text style={styles.label}>Sunucu adresi</Text>
              <View style={styles.originRow}>
                <TextInput
                  testID="setup-origin"
                  style={[styles.input, styles.originInput]}
                  value={origin}
                  onChangeText={value => {
                    setOrigin(value);
                    setServer('unknown');
                  }}
                  autoCapitalize="none"
                  autoCorrect={false}
                  keyboardType="url"
                  placeholder="http://192.168.1.10:5272"
                  placeholderTextColor={colors.muted}
                />
                <BigButton
                  label="Bağlantıyı sına"
                  tone="primaryOutline"
                  onPress={() => testConnection(address)}
                  busy={server === 'checking'}
                  testID="setup-test"
                />
              </View>

              <View style={styles.card}>
                <InfoRow label="Bağlantı">
                  <StateText
                    tone={server === 'online' ? 'success' : server === 'failed' ? 'danger' : 'muted'}
                    text={{ unknown: 'Sınanmadı', checking: 'Sınanıyor…', online: 'Bağlı', failed: 'Bağlanamadı' }[server]}
                  />
                </InfoRow>
                <InfoRow label="Devredeki projeler" divider>
                  <Text style={styles.infoValue}>
                    {available === null ? '—' : available.length === 0 ? 'Yok' : available.map(key => PROJECT_LABELS[key]).join(', ')}
                  </Text>
                </InfoRow>
              </View>

              <View style={styles.paneActions}>
                <BigButton label="Proje seçimine geç" size="compact" onPress={() => go('project')} disabled={server !== 'online'} testID="setup-next-project" />
              </View>
            </ScrollView>
          )}

          {section === 'project' && (
            <ScrollView contentContainerStyle={styles.pane}>
              {message && <Banner kind={message.kind} text={message.text} testID="setup-message" />}
              <View style={styles.list} accessibilityRole="radiogroup">
                {PROJECTS.map((key, index) => {
                  const enabled = available === null || available.includes(key);
                  const active = key === selected;
                  const paired = stationLabel(devices[key]);
                  return (
                    <Pressable
                      key={key}
                      testID={`setup-project-${key}`}
                      accessibilityRole="radio"
                      accessibilityState={{ selected: active, disabled: !enabled }}
                      disabled={!enabled || busy}
                      onPress={() => selectProject(key)}
                      style={({ pressed }) => [
                        styles.row,
                        index > 0 && styles.rowDivider,
                        active && styles.rowActive,
                        pressed && styles.rowPressed,
                        !enabled && styles.rowDisabled,
                      ]}>
                      <View style={[styles.radio, active && styles.radioActive]}>{active && <View style={styles.radioDot} />}</View>
                      <View style={styles.rowText}>
                        <Text style={styles.rowTitle}>
                          {PROJECT_LABELS[key]}
                          {saved.setting?.project === key ? <Text style={styles.rowTag}>  · Kayıtlı</Text> : null}
                        </Text>
                        <Text style={styles.rowDetail} numberOfLines={1}>
                          {!enabled ? 'Bu sunucuda devrede değil' : paired ? `Bağlı makine: ${paired}` : 'Makine eşleştirilmedi'}
                        </Text>
                      </View>
                      {enabled && <Text style={styles.chevron}>›</Text>}
                    </Pressable>
                  );
                })}
              </View>
            </ScrollView>
          )}

          {section === 'machine' && message && <Banner kind={message.kind} text={message.text} testID="setup-message" />}
          {section === 'machine' &&
            (!pairable ? (
              <Text style={styles.empty}>{PROJECT_LABELS[selected]} için makine eşleştirmesi yok.</Text>
            ) : !address ? (
              <Banner kind="error" text={INVALID_ADDRESS} />
            ) : (
              <StationList
                key={`${address}-${selected}-${listVersion}`}
                store={store}
                origin={address}
                project={selected}
                password={password}
                current={station}
                pending={pending}
                disabled={busy}
                onSelect={setPending}
                onCurrent={setCurrentStation}
              />
            ))}
        </View>

        <View style={styles.footer}>
          <View style={styles.summary}>
            <Text style={styles.summaryLabel}>{dirty ? 'SEÇİLİ · KAYDEDİLMEDİ' : 'KAYITLI'}</Text>
            <Text style={styles.summaryValue} numberOfLines={1} testID="setup-station">
              {PROJECT_LABELS[selected]} · {pairable ? chosen ?? 'makine seçilmedi' : 'eşleştirme gerekmez'}
            </Text>
          </View>
          <BigButton label="İptal" tone="neutral" size="compact" onPress={revert} disabled={busy || !dirty} testID="setup-revert" style={styles.footerButton} />
          <BigButton label="Kaydet" size="compact" onPress={save} busy={busy} disabled={!dirty} testID="setup-save" style={styles.footerButton} />
        </View>
      </View>
    </View>
  );
}

interface StationListProps {
  store: KeyValueStore;
  origin: string;
  project: ProjectKey;
  password: string;
  /** Bu tabletin projedeki mevcut (kayıtlı) makinesi. */
  current: string | null;
  /** Listeden seçilen, kaydedilmemiş makine. */
  pending: PairingStation | null;
  disabled?: boolean;
  /** Dokunulan makine seçilir; eşleştirme Kaydet ile yapılır. Mevcut makineye dokunmak seçimi bırakır. */
  onSelect: (station: PairingStation | null) => void;
  /** Liste okununca bu tabletin şu anki istasyonu (yoksa boş). */
  onCurrent: (station: PairingStation | null) => void;
}

const sameStation = (a: PairingStation, b: PairingStation) => a.machineId === b.machineId && a.tabletId === b.tabletId;

/** Seçili projenin makineleri doğrudan liste olarak. */
function StationList({ store, origin, project, password, current, pending, disabled, onSelect, onCurrent }: StationListProps) {
  const [stations, setStations] = useState<PairingStation[] | null>(null);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setStations(null);
    setError(null);
    getDeviceIdentifier(store)
      .then(identifier => fetchStations(origin, project, password, identifier))
      .then(list => {
        if (!cancelled) {
          setStations(list);
          onCurrent(list.find(station => station.isThisDevice) ?? null);
        }
      })
      .catch(failure => !cancelled && setError(errorText(failure)));
    return () => {
      cancelled = true;
    };
  }, [store, origin, project, password, reload, onCurrent]);

  // Başka istasyona geçilirse tabletin şu anki istasyonunda açık kalan oturum kapatılır.
  const leaving = stations?.find(station => station.isThisDevice);
  const closesSession = pending && leaving?.sessionOperator && !sameStation(pending, leaving) ? leaving : null;

  const visible = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('tr-TR');
    return (stations ?? []).filter(
      station => term === '' || `${station.code} ${station.name} ${station.detail ?? ''}`.toLocaleLowerCase('tr-TR').includes(term));
  }, [stations, search]);

  return (
    <View style={styles.stations}>
      <View style={styles.stationsTop}>
        <View style={styles.currentBox}>
          <Text style={styles.currentLabel}>Mevcut eşleştirme</Text>
          <Text style={current ? styles.currentValue : styles.currentEmpty} numberOfLines={1}>
            {current ?? 'Bu tablet henüz bir makineye bağlı değil'}
          </Text>
        </View>
        <TextInput
          testID="pairing-search"
          style={[styles.input, styles.search]}
          value={search}
          onChangeText={setSearch}
          autoCorrect={false}
          placeholder="Makine ara"
          placeholderTextColor={colors.muted}
        />
        <BigButton label="Yenile" tone="neutral" size="compact" onPress={() => setReload(value => value + 1)} disabled={disabled} testID="pairing-reload" />
      </View>

      {error && <Banner kind="error" text={error} testID="pairing-error" />}

      {pending?.isPaired && (
        <View style={styles.confirm} testID="pairing-take-over-warning">
          <View style={styles.confirmText}>
            <Text style={styles.confirmTitle}>{pending.code} başka bir cihaza bağlı</Text>
            <Text style={styles.confirmDetail}>Kaydederseniz bu tablet makineyi devralır; önceki cihaz bu makinede çalışamaz.</Text>
          </View>
        </View>
      )}

      {closesSession && (
        <View style={styles.confirm} testID="pairing-close-session-warning">
          <View style={styles.confirmText}>
            <Text style={styles.confirmTitle}>
              {closesSession.code} makinesinde {closesSession.sessionWorkInProgress ? 'süren iş var' : 'açık oturum var'}
            </Text>
            <Text style={styles.confirmDetail}>
              {closesSession.sessionWorkInProgress
                ? `${closesSession.sessionOperator} adedi bildirilmemiş bir işle çalışıyor. Kaydederseniz iş bırakılır ve oturum kapatılır; kalan adet sonra sürdürülür.`
                : `${closesSession.sessionOperator} adlı operatörün oturumu kapatılır.`}
            </Text>
          </View>
        </View>
      )}

      {stations === null && !error ? (
        <ActivityIndicator size="large" color={colors.primary} style={styles.loading} />
      ) : stations !== null && stations.length === 0 ? (
        <Text style={styles.empty}>Bu projede tanımlı makine yok. Önce web'den makine ekleyin.</Text>
      ) : stations !== null && visible.length === 0 ? (
        <Text style={styles.empty}>Aramaya uyan makine yok.</Text>
      ) : stations !== null ? (
        <ScrollView style={styles.stationList} contentContainerStyle={styles.list} keyboardShouldPersistTaps="handled">
          {visible.map((station, index) => {
            const chosen = pending ? sameStation(pending, station) : station.isThisDevice;
            return (
              <Pressable
                key={`${station.machineId ?? 'n'}-${station.tabletId ?? 'n'}`}
                testID={`pairing-station-${station.code}`}
                accessibilityRole="radio"
                accessibilityState={{ selected: chosen }}
                disabled={disabled}
                onPress={() => onSelect(station.isThisDevice ? null : station)}
                style={({ pressed }) => [styles.row, index > 0 && styles.rowDivider, chosen && styles.rowActive, pressed && styles.rowPressed]}>
                <View style={[styles.radio, chosen && styles.radioActive]}>{chosen && <View style={styles.radioDot} />}</View>
                <View style={styles.rowText}>
                  <Text style={styles.rowTitle} numberOfLines={1}>{station.code}</Text>
                  <Text style={styles.rowDetail} numberOfLines={1}>{[station.name, station.detail].filter(Boolean).join(' · ')}</Text>
                </View>
                <Text
                  style={[
                    styles.state,
                    station.isThisDevice && styles.stateThis,
                    station.isPaired && !station.isThisDevice && styles.stateTaken,
                  ]}>
                  {station.isThisDevice ? 'Bu tablet' : station.isPaired ? 'Başka cihaza bağlı' : 'Boş'}
                </Text>
              </Pressable>
            );
          })}
        </ScrollView>
      ) : null}
    </View>
  );
}

function InfoRow({ label, divider, children }: { label: string; divider?: boolean; children: ReactNode }) {
  return (
    <View style={[styles.infoRow, divider && styles.rowDivider]}>
      <Text style={styles.infoLabel}>{label}</Text>
      <View style={styles.infoValueBox}>{children}</View>
    </View>
  );
}

function StateText({ tone, text }: { tone: 'success' | 'danger' | 'muted'; text: string }) {
  const color = tone === 'success' ? colors.success : tone === 'danger' ? colors.danger : colors.muted;
  return (
    <View style={styles.stateRow}>
      <View style={[styles.stateDot, { backgroundColor: color }]} />
      <Text style={[styles.infoValue, { color }]}>{text}</Text>
    </View>
  );
}

// Menü ikonları çizimle (ikon kütüphanesi yok): sunucu dolabı, proje kutuları, makine ekranı.
function ServerIcon({ color }: { color: string }) {
  return (
    <View style={icon.box}>
      {[0, 1, 2].map(index => (
        <View key={index} style={[icon.rack, { borderColor: color }]}>
          <View style={[icon.rackDot, { backgroundColor: color }]} />
        </View>
      ))}
    </View>
  );
}

function ProjectIcon({ color }: { color: string }) {
  return (
    <View style={[icon.box, icon.grid]}>
      {[0, 1, 2, 3].map(index => (
        <View key={index} style={[icon.cell, { borderColor: color }, index === 0 && { backgroundColor: color }]} />
      ))}
    </View>
  );
}

function MachineIcon({ color }: { color: string }) {
  return (
    <View style={[icon.box, icon.center]}>
      <View style={[icon.screen, { borderColor: color }]}>
        <View style={[icon.screenLine, { backgroundColor: color }]} />
      </View>
      <View style={[icon.stand, { backgroundColor: color }]} />
      <View style={[icon.base, { backgroundColor: color }]} />
    </View>
  );
}

function ExitIcon({ color }: { color: string }) {
  return (
    <View style={[icon.box, icon.exit]}>
      <View style={[icon.door, { borderColor: color }]} />
      <View style={[icon.arrowLine, { backgroundColor: color }]} />
      <View style={[icon.arrowHead, { borderColor: color }]} />
    </View>
  );
}

const icon = StyleSheet.create({
  box: { width: 34, height: 30, justifyContent: 'space-between' },
  rack: { height: 8, borderWidth: 2, borderRadius: 2, justifyContent: 'center', paddingLeft: 3 },
  rackDot: { width: 3, height: 3, borderRadius: 2 },
  grid: { flexDirection: 'row', flexWrap: 'wrap', alignContent: 'space-between' },
  cell: { width: 15, height: 13, borderWidth: 2, borderRadius: 2 },
  center: { alignItems: 'center', justifyContent: 'flex-start' },
  screen: { width: 34, height: 21, borderWidth: 2, borderRadius: 3, justifyContent: 'center', paddingHorizontal: 5 },
  screenLine: { height: 3, width: '60%', borderRadius: 2 },
  stand: { width: 3, height: 5 },
  base: { width: 16, height: 3, borderRadius: 2 },
  // Kapı ve dışarı ok.
  exit: { flexDirection: 'row', alignItems: 'center', justifyContent: 'flex-start' },
  door: { width: 16, height: 28, borderWidth: 2, borderRightWidth: 0, borderTopLeftRadius: 3, borderBottomLeftRadius: 3 },
  arrowLine: { position: 'absolute', left: 9, width: 20, height: 3, borderRadius: 2 },
  arrowHead: { position: 'absolute', right: 4, width: 10, height: 10, borderTopWidth: 3, borderRightWidth: 3, transform: [{ rotate: '45deg' }] },
});

const TILE = 96;

const styles = StyleSheet.create({
  root: { flex: 1, flexDirection: 'row', backgroundColor: colors.background },
  rail: { width: TILE + 28, backgroundColor: colors.header, alignItems: 'center', paddingVertical: 18, gap: 12 },
  railTitle: { color: colors.headerMuted, fontSize: 11, fontWeight: '700', letterSpacing: 1.2, marginBottom: 2 },
  tile: {
    width: TILE,
    height: TILE,
    borderRadius: radius.card,
    borderWidth: 1,
    borderColor: colors.headerBorder,
    backgroundColor: colors.headerSurface,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
    overflow: 'hidden',
  },
  tileActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  tilePressed: { borderColor: colors.headerMuted },
  tileDisabled: { opacity: 0.5 },
  // Çıkış en altta, kırmızı tonla menüden ayrılır.
  exitTile: {
    marginTop: 'auto',
    height: 44,
    flexDirection: 'row',
    gap: 6,
    backgroundColor: colors.danger,
    borderColor: colors.danger,
  },
  exitIcon: { transform: [{ scale: 0.6 }], marginHorizontal: -6 },
  exitPressed: { opacity: 0.85 },
  tileAccent: { position: 'absolute', left: 0, top: 0, bottom: 0, width: 4, backgroundColor: colors.brandAccent },
  tileLabel: { color: colors.headerMuted, fontSize: 13, fontWeight: '700' },
  tileLabelActive: { color: colors.headerText },
  main: { flex: 1 },
  header: {
    gap: 2,
    paddingVertical: 14,
    paddingHorizontal: 24,
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  caption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.1 },
  title: { color: colors.text, fontSize: 20, fontWeight: '700' },
  description: { color: colors.muted, fontSize: 14 },
  body: { flex: 1, padding: 20, gap: 12 },
  pane: { gap: 12 },
  label: { color: colors.text, fontSize: 14, fontWeight: '600' },
  originRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  originInput: { flex: 1 },
  input: {
    minHeight: TOUCH_HEIGHT,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    borderRadius: radius.control,
    backgroundColor: colors.surface,
    color: colors.text,
    fontSize: 16,
    paddingHorizontal: 14,
  },
  card: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, borderRadius: radius.card },
  infoRow: { flexDirection: 'row', alignItems: 'center', gap: 12, paddingVertical: 12, paddingHorizontal: 16 },
  infoLabel: { width: 170, color: colors.muted, fontSize: 14, fontWeight: '600' },
  infoValueBox: { flex: 1 },
  infoValue: { color: colors.text, fontSize: 15, fontWeight: '700' },
  stateRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  stateDot: { width: 10, height: 10, borderRadius: 5 },
  paneActions: { flexDirection: 'row', justifyContent: 'flex-end' },
  list: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, borderRadius: radius.card, overflow: 'hidden' },
  row: { flexDirection: 'row', alignItems: 'center', gap: 14, minHeight: 60, paddingVertical: 10, paddingHorizontal: 16 },
  rowDivider: { borderTopWidth: 1, borderTopColor: colors.border },
  rowActive: { backgroundColor: colors.primarySoft },
  rowPressed: { backgroundColor: colors.primarySoft },
  rowDisabled: { opacity: 0.45 },
  rowText: { flex: 1, gap: 2 },
  rowTitle: { color: colors.text, fontSize: 16, fontWeight: '700' },
  rowTag: { color: colors.primary, fontSize: 13, fontWeight: '700' },
  rowDetail: { color: colors.muted, fontSize: 13 },
  chevron: { color: colors.muted, fontSize: 26, fontWeight: '300' },
  radio: {
    width: 22,
    height: 22,
    borderRadius: 11,
    borderWidth: 2,
    borderColor: colors.borderStrong,
    alignItems: 'center',
    justifyContent: 'center',
  },
  radioActive: { borderColor: colors.primary },
  radioDot: { width: 10, height: 10, borderRadius: 5, backgroundColor: colors.primary },
  stations: { flex: 1, gap: 12 },
  stationsTop: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  currentBox: {
    flex: 1,
    minHeight: TOUCH_HEIGHT_COMPACT,
    justifyContent: 'center',
    paddingHorizontal: 14,
    paddingVertical: 6,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
  },
  currentLabel: { color: colors.muted, fontSize: 11, fontWeight: '700' },
  currentValue: { color: colors.primary, fontSize: 15, fontWeight: '700' },
  currentEmpty: { color: colors.muted, fontSize: 14 },
  search: { width: 260, minHeight: TOUCH_HEIGHT_COMPACT, fontSize: 15 },
  stationList: { flex: 1 },
  state: { color: colors.success, fontSize: 13, fontWeight: '700' },
  stateThis: { color: colors.primary },
  stateTaken: { color: colors.danger },
  confirm: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    padding: 12,
    backgroundColor: colors.dangerSoft,
    borderWidth: 1,
    borderColor: colors.danger,
    borderRadius: radius.card,
  },
  confirmText: { flex: 1, gap: 2 },
  confirmTitle: { color: colors.danger, fontSize: 15, fontWeight: '700' },
  confirmDetail: { color: colors.textSecondary, fontSize: 13 },
  loading: { paddingVertical: 48 },
  empty: { color: colors.muted, fontSize: 14, paddingVertical: 24, textAlign: 'center' },
  footer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingVertical: 12,
    paddingHorizontal: 20,
    backgroundColor: colors.surface,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  summary: { flex: 1, gap: 1 },
  // Proje adı değişince düğmeler kaymasın.
  footerButton: { minWidth: 130 },
  summaryLabel: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.1 },
  summaryValue: { color: colors.text, fontSize: 15, fontWeight: '700' },
});
