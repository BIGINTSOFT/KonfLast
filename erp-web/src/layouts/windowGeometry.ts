import type { WindowRect } from './tabs'

export interface Point {
  x: number
  y: number
}

export const MIN_WIDTH = 380
export const MIN_HEIGHT = 240
/** Pencere ekranın dışına sürüklense de başlığının bu kadarı görünür kalır (geri çekilebilsin diye). */
const KEEP_VISIBLE = 140
const BAR_HEIGHT = 30

/** Pencereyi görünümün içinde tutar: başlık çubuğu her zaman erişilebilir kalır. */
export function clampWindow(rect: WindowRect, viewport = { width: window.innerWidth, height: window.innerHeight }): WindowRect {
  const width = Math.max(MIN_WIDTH, Math.min(rect.width, viewport.width))
  const height = Math.max(MIN_HEIGHT, Math.min(rect.height, viewport.height))
  return {
    width,
    height,
    x: Math.max(KEEP_VISIBLE - width, Math.min(rect.x, viewport.width - KEEP_VISIBLE)),
    y: Math.max(0, Math.min(rect.y, viewport.height - BAR_HEIGHT)),
  }
}
