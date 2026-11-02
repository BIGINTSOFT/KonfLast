import { StyleSheet, Text, View } from 'react-native';
import { PROJECT_LABELS } from '../device/project';
import type { ProjectKey } from '../device/project';
import { BigButton } from '../ui/controls';
import { colors } from '../ui/theme';

interface ModuleScreenProps {
  project: ProjectKey;
  origin: string;
  onOpenSetup: () => void;
}

/**
 * İstasyon ekranları henüz olmayan bir projenin tablet arayüzü. Proje sunucuda devrededir;
 * istasyon ekranları geliştirildikçe bu ekranın yerini alır.
 */
export function ModuleScreen({ project, origin, onOpenSetup }: ModuleScreenProps) {
  const label = PROJECT_LABELS[project];
  return (
    <View style={styles.screen} testID="module-screen">
      <View style={styles.card}>
        <Text style={styles.eyebrow}>TEKSTİL ERP · {label.toLocaleUpperCase('tr-TR')}</Text>
        <Text style={styles.title}>{label} istasyonu</Text>
        <Text style={styles.text}>
          Bu modülün tablet ekranları geliştiriliyor. Modül sunucuda devrede; ekranlar hazır olduğunda bu tablet
          doğrudan {label} arayüzünü açacak.
        </Text>
        <Text style={styles.meta}>Sunucu: {origin}</Text>
        <View style={styles.actions}>
          <BigButton label="Sistem ayarları" tone="neutral" onPress={onOpenSetup} testID="module-open-setup" />
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background, alignItems: 'center', justifyContent: 'center', padding: 32 },
  card: {
    width: '100%',
    maxWidth: 640,
    gap: 10,
    padding: 28,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
  },
  eyebrow: { color: colors.muted, fontSize: 12, fontWeight: '700', letterSpacing: 1.2 },
  title: { color: colors.text, fontSize: 22, fontWeight: '700' },
  text: { color: colors.textSecondary, fontSize: 15, lineHeight: 22 },
  meta: { color: colors.muted, fontSize: 13 },
  actions: { flexDirection: 'row', marginTop: 8 },
});
