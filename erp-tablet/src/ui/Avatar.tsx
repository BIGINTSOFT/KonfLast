import { Image, StyleSheet, Text, View } from 'react-native';
import { initialsOf } from '../util/time';
import { colors } from './theme';

interface AvatarProps {
  size: number;
  /** Oturumu açık personelin adı; yoksa boş siluet çizilir. */
  name?: string | null;
  /** Personel fotoğrafı (data URI); yoksa adın baş harfleri gösterilir. */
  photoUri?: string | null;
  /** Durumu belirten çerçeve rengi. */
  ringColor?: string;
}

/** Personel fotoğrafı: fotoğraf → baş harfler → boş siluet sırasıyla, hep aynı yuvarlak çerçevede. */
export function Avatar({ size, name, photoUri, ringColor = colors.headerBorder }: AvatarProps) {
  const ring = Math.max(3, Math.round(size / 30));
  const inner = size - ring * 4;
  const circle = { width: inner, height: inner, borderRadius: inner / 2 };

  return (
    <View
      style={[styles.ring, { width: size, height: size, borderRadius: size / 2, borderWidth: ring, borderColor: ringColor }]}
      accessibilityRole="image"
      accessibilityLabel={name ? `${name} fotoğrafı` : 'Personel fotoğrafı yok'}>
      {photoUri ? (
        <Image source={{ uri: photoUri }} style={circle} resizeMode="cover" testID="avatar-photo" />
      ) : name ? (
        <View style={[styles.initials, circle]}>
          <Text style={[styles.initialsText, { fontSize: inner * 0.38 }]} testID="avatar-initials">
            {initialsOf(name)}
          </Text>
        </View>
      ) : (
        <View style={[styles.silhouette, circle]} testID="avatar-empty">
          <View style={[styles.head, { width: inner * 0.36, height: inner * 0.36, borderRadius: inner * 0.18, top: inner * 0.2 }]} />
          <View style={[styles.shoulders, { width: inner * 0.78, height: inner * 0.78, borderRadius: inner * 0.39, top: inner * 0.62 }]} />
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  ring: { alignItems: 'center', justifyContent: 'center' },
  initials: { alignItems: 'center', justifyContent: 'center', backgroundColor: colors.primary },
  initialsText: { color: '#ffffff', fontWeight: '700', letterSpacing: 1 },
  silhouette: { alignItems: 'center', overflow: 'hidden', backgroundColor: colors.headerSurface },
  head: { position: 'absolute', backgroundColor: colors.headerBorder },
  shoulders: { position: 'absolute', backgroundColor: colors.headerBorder },
});
