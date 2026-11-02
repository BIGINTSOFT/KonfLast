import { request } from '../api/http';
import type { KeyValueStore } from '../storage/stores';

/** Tekstil ERP'nin sektör modülleri: tablet hangisinin arayüzünü açacağını sistem ayarlarından seçer. */
export type ProjectKey = 'Konfeksiyon' | 'Boyahane' | 'Dokuma' | 'Orme';

export const PROJECTS: ProjectKey[] = ['Konfeksiyon', 'Boyahane', 'Dokuma', 'Orme'];

/** Modülün ekranda görünen adı; anahtar kodda ASCII kalır. */
export const PROJECT_LABELS: Record<ProjectKey, string> = { Konfeksiyon: 'Konfeksiyon', Boyahane: 'Boyahane', Dokuma: 'Dokuma', Orme: 'Örme' };

const PROJECT_KEY = 'app.project';

/** Tabletin açtığı arayüz ve o arayüzün bağlandığı sunucu. */
export interface ProjectSetting {
  project: ProjectKey;
  origin: string;
}

/**
 * Sunucuda devrede olan modüller (kurulu ve lisanslı). Uç oturum gerektirmez; tablet eşleştirilmeden önce de
 * okunabilir. Bilinmeyen anahtarlar atlanır.
 */
export async function fetchProjects(origin: string): Promise<ProjectKey[]> {
  const data = await request<{ modules: { key: string }[] }>(origin, '/system/modules');
  const keys = new Set(data.modules.map(m => m.key));
  return PROJECTS.filter(key => keys.has(key));
}

export async function loadProject(store: KeyValueStore): Promise<ProjectSetting | null> {
  const raw = await store.get(PROJECT_KEY);
  if (!raw) {
    return null;
  }

  try {
    const setting = JSON.parse(raw) as ProjectSetting;
    return PROJECTS.includes(setting.project) ? setting : null;
  } catch {
    return null;
  }
}

export async function saveProject(store: KeyValueStore, setting: ProjectSetting): Promise<void> {
  await store.set(PROJECT_KEY, JSON.stringify(setting));
}
