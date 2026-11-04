import { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Keyboard, StatusBar, StyleSheet, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { TabletApi } from './src/api/TabletApi';
import { BoyahaneApi } from './src/boyahane/BoyahaneApi';
import { BoyahaneStationScreen } from './src/boyahane/BoyahaneStationScreen';
import { CompletionOutbox } from './src/boyahane/completionOutbox';
import { DokumaApi } from './src/dokuma/DokumaApi';
import { DokumaStationScreen } from './src/dokuma/DokumaStationScreen';
import { RollOutbox } from './src/dokuma/rollOutbox';
import { OrmeApi } from './src/orme/OrmeApi';
import { OrmeStationScreen } from './src/orme/OrmeStationScreen';
import { RollOutbox as OrmeRollOutbox } from './src/orme/rollOutbox';
import { loadEnrolledDevice, migrateLegacyDevice } from './src/device/deviceConfig';
import type { EnrolledDevice } from './src/device/deviceConfig';
import { supportsPairing } from './src/device/pairing';
import { loadProject } from './src/device/project';
import type { ProjectSetting } from './src/device/project';
import { ReportOutbox } from './src/production/outbox';
import { useTabletLive } from './src/live/useTabletLive';
import { KeyboardWedgeReader } from './src/rfid/KeyboardWedgeReader';
import { normalizeUid } from './src/rfid/RfidReader';
import { ModuleScreen } from './src/screens/ModuleScreen';
import { SettingsPasswordDialog } from './src/screens/SettingsPasswordDialog';
import { SetupScreen } from './src/screens/SetupScreen';
import { StationScreen } from './src/screens/StationScreen';
import { StationController } from './src/station/StationController';
import { deviceKeyStoreFor, deviceStore, legacyDeviceKeyStore } from './src/storage/nativeStores';
import { colors } from './src/ui/theme';

type Screen = 'booting' | 'password' | 'setup' | 'station' | 'boyahane' | 'dokuma' | 'orme' | 'module';

/** Boyahane okuyucusu kart numarasının yanında kağıt karttaki parti barkodunu da kabul eder. */
const normalizeBoyahaneCode = (raw: string): string | null => {
  const code = raw.trim().toUpperCase();
  return /^BP-\d{4}-\d{5}$/.test(code) ? code : normalizeUid(raw);
};

/** Eşleştirilmiş tablette seçili projenin ekranı. */
const screenOf = (project: ProjectSetting | null, enrolled: boolean): Screen => {
  if (project && !supportsPairing(project.project)) {
    return 'module';
  }

  if (!enrolled) {
    return 'password';
  }

  return project?.project === 'Boyahane' ? 'boyahane' : project?.project === 'Dokuma' ? 'dokuma' : project?.project === 'Orme' ? 'orme' : 'station';
};

function App() {
  const [device, setDevice] = useState<EnrolledDevice | null>(null);
  // Tabletin açtığı proje arayüzü (sistem ayarlarından seçilir). Seçim yoksa eşleştirilmiş tablet Konfeksiyon
  // istasyonudur (proje seçimi gelmeden önce kurulmuş tabletler).
  const [project, setProject] = useState<ProjectSetting | null>(null);
  const [screen, setScreen] = useState<Screen>('booting');
  /** Sistem ayarlarına girişte doğrulanan parola; yalnızca bellekte durur. */
  const [settingsPassword, setSettingsPassword] = useState('');
  /** Açık ekranın üzerindeki parola penceresi (istasyon arkada çalışmaya devam eder). */
  const [passwordOpen, setPasswordOpen] = useState(false);

  useEffect(() => {
    let cancelled = false;
    const boot = async () => {
      const selected = await loadProject(deviceStore).catch(() => null);
      const key = selected?.project ?? 'Konfeksiyon';
      await migrateLegacyDevice(deviceStore, legacyDeviceKeyStore, deviceKeyStoreFor(key), key).catch(() => {});
      const loaded = await loadEnrolledDevice(deviceStore, deviceKeyStoreFor(key), key, selected?.origin).catch(() => null);
      if (!cancelled) {
        setDevice(loaded);
        setProject(selected);
        setScreen(screenOf(selected, loaded !== null));
      }
    };
    boot();
    return () => {
      cancelled = true;
    };
  }, []);

  // Eşleştirme değişince istasyon baştan kurulur: yeni kimlik, yeni token, temiz durum.
  const station = useMemo(() => {
    if (!device || project?.project === 'Boyahane' || project?.project === 'Dokuma' || project?.project === 'Orme') {
      return null;
    }

    const api = new TabletApi(device.credentials);
    const controller = new StationController(api, new ReportOutbox(deviceStore));
    const reader = new KeyboardWedgeReader(() => controller.notifyInvalidRead());
    return { api, controller, reader };
  }, [device, project?.project]);

  // Makinede web'den yapılan değişiklik (elle bildirim, arıza kapatma...) beklemeden ekrana yansır.
  useTabletLive(station?.api ?? null, () => void station?.controller.tick());

  const boyahane = useMemo(() => {
    if (!device || project?.project !== 'Boyahane') {
      return null;
    }

    return {
      api: new BoyahaneApi(device.credentials),
      outbox: new CompletionOutbox(deviceStore),
      reader: new KeyboardWedgeReader(undefined, normalizeBoyahaneCode),
    };
  }, [device, project?.project]);

  const dokuma = useMemo(() => {
    if (!device || project?.project !== 'Dokuma') {
      return null;
    }

    return {
      api: new DokumaApi(device.credentials),
      outbox: new RollOutbox(deviceStore),
      reader: new KeyboardWedgeReader(),
    };
  }, [device, project?.project]);

  const orme = useMemo(() => {
    if (!device || project?.project !== 'Orme') {
      return null;
    }

    return {
      api: new OrmeApi(device.credentials),
      outbox: new OrmeRollOutbox(deviceStore),
      reader: new KeyboardWedgeReader(),
    };
  }, [device, project?.project]);

  // Ayarlardan çıkınca klavye açık kalmasın: istasyon ekranındaki gizli kart alanı klavye açmaz ama açık olanı kapatmaz.
  useEffect(() => {
    if (screen !== 'password' && screen !== 'setup' && !passwordOpen) {
      Keyboard.dismiss();
    }
  }, [screen, passwordOpen]);

  /** Ayarlardan vazgeçince dönülecek ekran; 'password' ise dönülecek ekran yoktur. */
  const returnScreen = screenOf(project, device !== null);

  return (
    <SafeAreaProvider>
      <StatusBar barStyle="light-content" />
      <SafeAreaView style={styles.screen}>
        {screen === 'booting' && (
          <View style={styles.booting}>
            <ActivityIndicator size="large" color={colors.primary} />
          </View>
        )}

        {screen === 'password' && <View style={styles.blank} />}

        {screen === 'setup' && (
          <SetupScreen
            store={deviceStore}
            secretsFor={deviceKeyStoreFor}
            password={settingsPassword}
            project={project}
            onApply={(selected, enrolled) => {
              setDevice(enrolled);
              setProject(selected);
              setSettingsPassword('');
              setScreen(screenOf(selected, enrolled !== null));
            }}
            onCancel={
              returnScreen === 'password'
                ? undefined
                : () => {
                    setSettingsPassword('');
                    setScreen(returnScreen);
                  }
            }
          />
        )}

        {screen === 'module' && project && (
          <ModuleScreen project={project.project} origin={project.origin} onOpenSetup={() => setPasswordOpen(true)} />
        )}

        {screen === 'boyahane' && device && boyahane && (
          <BoyahaneStationScreen
            api={boyahane.api}
            outbox={boyahane.outbox}
            reader={boyahane.reader}
            tabletCode={device.config.tabletCode}
            onOpenSetup={() => setPasswordOpen(true)}
          />
        )}

        {screen === 'dokuma' && device && dokuma && (
          <DokumaStationScreen
            api={dokuma.api}
            outbox={dokuma.outbox}
            reader={dokuma.reader}
            tabletCode={device.config.tabletCode}
            onOpenSetup={() => setPasswordOpen(true)}
          />
        )}

        {screen === 'orme' && device && orme && (
          <OrmeStationScreen
            api={orme.api}
            outbox={orme.outbox}
            reader={orme.reader}
            tabletCode={device.config.tabletCode}
            onOpenSetup={() => setPasswordOpen(true)}
          />
        )}

        {screen === 'station' && device && station && (
          <StationScreen
            controller={station.controller}
            reader={station.reader}
            tabletCode={device.config.tabletCode}
            onOpenSetup={() => setPasswordOpen(true)}
          />
        )}
        {(screen === 'password' || passwordOpen) && (
          <SettingsPasswordDialog
            onUnlock={password => {
              setSettingsPassword(password);
              setPasswordOpen(false);
              setScreen('setup');
            }}
            onCancel={screen === 'password' ? undefined : () => setPasswordOpen(false)}
          />
        )}
      </SafeAreaView>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.header },
  blank: { flex: 1, backgroundColor: colors.background },
  booting: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.background },
});

export default App;
