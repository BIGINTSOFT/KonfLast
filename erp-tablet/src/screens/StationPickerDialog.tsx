import { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { isRetryable } from '../api/http';
import { fetchStations, pairStation } from '../device/pairing';
import type { PairingStation, PairResponse } from '../device/pairing';
import { PROJECT_LABELS } from '../device/project';
import type { ProjectKey } from '../device/project';
import { Banner, BigButton } from '../ui/controls';
import { DialogFrame } from '../ui/Dialog';
import { colors, radius, TOUCH_HEIGHT_COMPACT } from '../ui/theme';

interface StationPickerDialogProps {
  origin: string;
  project: ProjectKey;
  password: string;
  deviceIdentifier: string;
  onPaired: (paired: PairResponse) => void;
  onCancel: () => void;
}

const errorText = (error: unknown) =>
  isRetryable(error)
    ? 'Sunucuya ulaşılamıyor. Adresi ve Wi-Fi bağlantısını kontrol edin.'
    : error instanceof Error ? error.message : 'İşlem yapılamadı.';

/** Projenin makineleri arasından bu tabletin bağlanacağı istasyonun seçimi. */
export function StationPickerDialog({ origin, project, password, deviceIdentifier, onPaired, onCancel }: StationPickerDialogProps) {
  const [stations, setStations] = useState<PairingStation[] | null>(null);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  /** Başka cihaza bağlı istasyon seçildi: devralma onayı bekleniyor. */
  const [confirming, setConfirming] = useState<PairingStation | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetchStations(origin, project, password, deviceIdentifier)
      .then(list => !cancelled && setStations(list))
      .catch(failure => !cancelled && setError(errorText(failure)));
    return () => {
      cancelled = true;
    };
  }, [origin, project, password, deviceIdentifier]);

  const visible = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('tr-TR');
    return (stations ?? []).filter(
      station => term === '' || `${station.code} ${station.name} ${station.detail ?? ''}`.toLocaleLowerCase('tr-TR').includes(term));
  }, [stations, search]);

  const pair = async (station: PairingStation, takeOver: boolean) => {
    setBusy(true);
    setError(null);
    try {
      onPaired(await pairStation(origin, project, password, deviceIdentifier, station, takeOver));
    } catch (failure) {
      setError(errorText(failure));
      setConfirming(null);
    } finally {
      setBusy(false);
    }
  };

  if (confirming) {
    return (
      <DialogFrame
        title="Makineyi devral"
        description={`${confirming.code} başka bir cihaza bağlı.`}
        onClose={() => setConfirming(null)}
        testID="pairing-confirm"
        footer={
          <>
            <BigButton label="Vazgeç" tone="neutral" size="compact" onPress={() => setConfirming(null)} disabled={busy} testID="pairing-confirm-cancel" />
            <BigButton label="Devral" tone="danger" size="compact" busy={busy} onPress={() => pair(confirming, true)} testID="pairing-confirm-take-over" />
          </>
        }>
        <Text style={styles.confirmText}>Devralırsanız önceki cihaz bu makinede çalışamaz; yeniden eşleştirilmesi gerekir.</Text>
      </DialogFrame>
    );
  }

  return (
    <DialogFrame
      wide
      title={`${PROJECT_LABELS[project]} makine seçimi`}
      description="Bu tabletin bağlanacağı makineyi seçin."
      onClose={onCancel}
      testID="pairing-dialog"
      footer={<BigButton label="Vazgeç" tone="neutral" size="compact" onPress={onCancel} disabled={busy} testID="pairing-cancel" />}>
      {error && <Banner kind="error" text={error} testID="pairing-error" />}

      {stations === null && !error && <ActivityIndicator size="large" color={colors.primary} style={styles.loading} />}

      {stations !== null && (
        <>
          <TextInput
            testID="pairing-search"
            style={styles.search}
            value={search}
            onChangeText={setSearch}
            autoCorrect={false}
            placeholder="Makine ara"
            placeholderTextColor={colors.muted}
          />

          {stations.length === 0 && <Text style={styles.empty}>Bu projede tanımlı makine yok. Önce web'den makine ekleyin.</Text>}
          {stations.length > 0 && visible.length === 0 && <Text style={styles.empty}>Aramaya uyan makine yok.</Text>}

          <ScrollView style={styles.list} keyboardShouldPersistTaps="handled">
            {visible.map((station, index) => (
              <Pressable
                key={`${station.machineId ?? 'n'}-${station.tabletId ?? 'n'}`}
                testID={`pairing-station-${station.code}`}
                disabled={busy}
                onPress={() => (station.isPaired && !station.isThisDevice ? setConfirming(station) : pair(station, false))}
                style={({ pressed }) => [styles.row, index > 0 && styles.rowDivider, pressed && styles.rowPressed]}>
                <View style={styles.rowText}>
                  <Text style={styles.rowCode} numberOfLines={1}>{station.code}</Text>
                  <Text style={styles.rowName} numberOfLines={1}>
                    {[station.name, station.detail].filter(Boolean).join(' · ')}
                  </Text>
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
            ))}
          </ScrollView>
        </>
      )}
    </DialogFrame>
  );
}

const styles = StyleSheet.create({
  loading: { paddingVertical: 48 },
  confirmText: { color: colors.textSecondary, fontSize: 15, lineHeight: 22 },
  search: {
    minHeight: TOUCH_HEIGHT_COMPACT,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    borderRadius: radius.control,
    color: colors.text,
    fontSize: 15,
    paddingHorizontal: 12,
    marginBottom: 8,
  },
  empty: { color: colors.muted, fontSize: 14, paddingVertical: 16, textAlign: 'center' },
  list: { maxHeight: 320, borderWidth: 1, borderColor: colors.border, borderRadius: radius.control },
  row: { flexDirection: 'row', alignItems: 'center', gap: 12, paddingVertical: 10, paddingHorizontal: 12 },
  rowDivider: { borderTopWidth: 1, borderTopColor: colors.border },
  rowPressed: { backgroundColor: colors.primarySoft },
  rowText: { flex: 1, gap: 1 },
  rowCode: { color: colors.text, fontSize: 15, fontWeight: '700' },
  rowName: { color: colors.muted, fontSize: 13 },
  state: { color: colors.muted, fontSize: 13, fontWeight: '600' },
  stateThis: { color: colors.primary },
  stateTaken: { color: colors.danger },
});
