import { useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { checkServer } from '../api/client';
import { DEFAULT_API_ORIGIN } from '../api/config';
import { isRetryable } from '../api/http';
import { getDeviceIdentifier, loadEnrolledDevice, normalizeOrigin, saveEnrolledDevice } from '../device/deviceConfig';
import type { EnrolledDevice } from '../device/deviceConfig';
import { supportsPairing } from '../device/pairing';
import type { PairResponse } from '../device/pairing';
import { fetchProjects, PROJECT_LABELS, PROJECTS, saveProject } from '../device/project';
import type { ProjectKey, ProjectSetting } from '../device/project';
import type { KeyValueStore, SecretStore } from '../storage/stores';
import { Banner, BigButton } from '../ui/controls';
import { colors, radius, TOUCH_HEIGHT } from '../ui/theme';
import { StationPickerDialog } from './StationPickerDialog';

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

/**
 * Tabletin sistem ayarları (yönetici tarafından yapılır): sunucu adresi, açılacak proje ve o projede tabletin
 * bağlı olduğu makine. Her projenin eşleştirmesi ayrı saklanır; eşleşmiş projeye geçiş tek dokunuştur.
 */
export function SetupScreen({ store, secretsFor, password, project, onApply, onCancel }: SetupScreenProps) {
  const [origin, setOrigin] = useState(project?.origin ?? DEFAULT_API_ORIGIN);
  const [selected, setSelected] = useState<ProjectKey>(project?.project ?? 'Konfeksiyon');
  /** Seçili projenin bu tabletteki eşleştirmesi. */
  const [device, setDevice] = useState<EnrolledDevice | null>(null);
  /** Sunucuda devrede olan projeler; bağlantı sınanınca öğrenilir. */
  const [available, setAvailable] = useState<ProjectKey[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ kind: 'error' | 'success' | 'info'; text: string } | null>(null);
  const [picker, setPicker] = useState<{ origin: string; deviceIdentifier: string } | null>(null);
  const pairable = supportsPairing(selected);

  useEffect(() => {
    let cancelled = false;
    setDevice(null);
    loadEnrolledDevice(store, secretsFor(selected), selected)
      .catch(() => null)
      .then(loaded => {
        if (!cancelled) {
          setDevice(loaded);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [store, secretsFor, selected]);

  const invalidAddress = () =>
    setMessage({ kind: 'error', text: 'Sunucu adresi geçersiz. Örnek: http://192.168.1.10:5272' });
  const unreachable = () =>
    setMessage({ kind: 'error', text: 'Sunucuya ulaşılamıyor. Adresi ve Wi-Fi bağlantısını kontrol edin.' });

  const testConnection = async () => {
    const address = normalizeOrigin(origin);
    if (!address) {
      invalidAddress();
      return;
    }

    setBusy(true);
    const status = await checkServer(address);
    let projects: ProjectKey[] | null = null;
    if (status === 'online') {
      projects = await fetchProjects(address).catch(() => null);
    }
    setBusy(false);

    if (status !== 'online') {
      if (status === 'database-down') {
        setMessage({ kind: 'error', text: 'Sunucu yanıt veriyor ama veritabanına ulaşamıyor.' });
      } else {
        unreachable();
      }
      return;
    }

    setAvailable(projects);
    setMessage(
      projects && projects.length === 0
        ? { kind: 'error', text: 'Sunucuya bağlanıldı ama devrede proje yok (lisansı kontrol edin).' }
        : { kind: 'success', text: `Sunucuya bağlanıldı. Devredeki projeler: ${(projects ?? []).map(key => PROJECT_LABELS[key]).join(', ') || '-'}.` },
    );
  };

  /** Seçili projeyi tabletin açtığı proje yapar. */
  const apply = async (address: string, enrolled: EnrolledDevice | null) => {
    const setting: ProjectSetting = { project: selected, origin: address };
    await saveProject(store, setting);
    onApply(setting, enrolled);
  };

  const failure = (error: unknown) => {
    if (isRetryable(error)) {
      unreachable();
    } else {
      setMessage({ kind: 'error', text: error instanceof Error ? error.message : 'Kaydedilemedi.' });
    }
  };

  const openPicker = async () => {
    const address = normalizeOrigin(origin);
    if (!address) {
      invalidAddress();
      return;
    }

    setMessage(null);
    setPicker({ origin: address, deviceIdentifier: await getDeviceIdentifier(store) });
  };

  const onPaired = async (address: string, paired: PairResponse) => {
    setPicker(null);
    setBusy(true);
    try {
      const enrolled = await saveEnrolledDevice(
        store,
        secretsFor(selected),
        selected,
        { origin: address, tabletCode: paired.tabletCode, stationCode: paired.stationCode, stationName: paired.stationName },
        paired.deviceKey,
      );
      await apply(address, enrolled);
    } catch (error) {
      failure(error);
    } finally {
      setBusy(false);
    }
  };

  const open = async () => {
    const address = normalizeOrigin(origin);
    if (!address) {
      invalidAddress();
      return;
    }

    setBusy(true);
    try {
      const projects = await fetchProjects(address);
      setAvailable(projects);
      if (!projects.includes(selected)) {
        setMessage({ kind: 'error', text: `${PROJECT_LABELS[selected]} bu sunucuda devrede değil (kurulum ya da lisans).` });
        return;
      }

      const enrolled = pairable ? await loadEnrolledDevice(store, secretsFor(selected), selected, address) : null;
      await apply(address, enrolled);
    } catch (error) {
      failure(error);
    } finally {
      setBusy(false);
    }
  };

  const station = device
    ? [device.config.stationCode ?? device.config.tabletCode, device.config.stationName].filter(Boolean).join(' · ')
    : null;

  return (
    <View style={styles.root}>
      <ScrollView style={styles.screen} contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
        <Text style={styles.title}>Sistem ayarları</Text>

        <Text style={styles.label}>Sunucu adresi</Text>
        <View style={styles.originRow}>
          <TextInput
            testID="setup-origin"
            style={[styles.input, styles.originInput]}
            value={origin}
            onChangeText={setOrigin}
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="url"
            placeholder="http://192.168.1.10:5272"
            placeholderTextColor={colors.muted}
          />
          <BigButton label="Bağlantıyı sına" tone="neutral" onPress={testConnection} disabled={busy} testID="setup-test" />
        </View>

        <Text style={styles.label}>Proje</Text>
        <View style={styles.projects} accessibilityRole="radiogroup">
          {PROJECTS.map(key => (
            <BigButton
              key={key}
              label={PROJECT_LABELS[key]}
              size="compact"
              tone={key === selected ? 'primary' : 'neutral'}
              disabled={(available !== null && !available.includes(key)) || busy}
              onPress={() => {
                setSelected(key);
                setMessage(null);
              }}
              testID={`setup-project-${key}`}
              style={styles.project}
            />
          ))}
        </View>

        <Text style={styles.label}>Makine eşleştirmesi</Text>
        <View style={styles.pairing}>
          {pairable ? (
            <>
              <View style={styles.pairingText}>
                <Text style={station ? styles.pairingStation : styles.pairingEmpty} testID="setup-station">
                  {station ?? `Bu tablet ${PROJECT_LABELS[selected]} için henüz bir makineye bağlı değil.`}
                </Text>
              </View>
              <BigButton
                label={station ? 'Makineyi değiştir' : 'Makine seç'}
                tone={station ? 'neutral' : 'primary'}
                size="compact"
                onPress={openPicker}
                disabled={busy}
                testID="setup-pair"
              />
            </>
          ) : (
            <Text style={styles.pairingEmpty}>{PROJECT_LABELS[selected]} için makine eşleştirmesi henüz yok.</Text>
          )}
        </View>

        {message && <Banner kind={message.kind} text={message.text} testID="setup-message" />}

        <View style={styles.actions}>
          <BigButton
            label={`${PROJECT_LABELS[selected]} ekranını aç`}
            onPress={open}
            busy={busy}
            disabled={pairable && !device}
            testID="setup-open"
          />
          {onCancel && <BigButton label="Vazgeç" tone="neutral" onPress={onCancel} disabled={busy} testID="setup-cancel" />}
        </View>
      </ScrollView>

      {picker && (
        <StationPickerDialog
          origin={picker.origin}
          project={selected}
          password={password}
          deviceIdentifier={picker.deviceIdentifier}
          onPaired={paired => onPaired(picker.origin, paired)}
          onCancel={() => setPicker(null)}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  screen: { flex: 1 },
  content: { width: '100%', maxWidth: 640, alignSelf: 'center', padding: 28, gap: 8 },
  title: { color: colors.text, fontSize: 22, fontWeight: '700', marginBottom: 4 },
  label: { color: colors.text, fontSize: 14, fontWeight: '600', marginTop: 10 },
  originRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  originInput: { flex: 1 },
  input: {
    minHeight: TOUCH_HEIGHT,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
    backgroundColor: colors.surface,
    color: colors.text,
    fontSize: 16,
    paddingHorizontal: 14,
  },
  projects: { flexDirection: 'row', gap: 8 },
  project: { flex: 1 },
  pairing: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    minHeight: TOUCH_HEIGHT,
    padding: 12,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
  },
  pairingText: { flex: 1 },
  pairingStation: { color: colors.text, fontSize: 15, fontWeight: '700' },
  pairingEmpty: { flex: 1, color: colors.muted, fontSize: 14 },
  actions: { flexDirection: 'row', gap: 10, marginTop: 16 },
});
