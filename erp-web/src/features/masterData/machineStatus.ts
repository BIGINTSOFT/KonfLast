export const machineStatusPresentation: Record<string, { icon: string; label: string; tone: string }> = {
  AVAILABLE: { icon: '○', label: 'Müsait', tone: 'neutral' },
  RUNNING: { icon: '▶', label: 'Çalışıyor', tone: 'ok' },
  IDLE: { icon: '‖', label: 'Boşta', tone: 'neutral' },
  BREAKDOWN: { icon: '!', label: 'Arıza', tone: 'danger' },
  MAINTENANCE: { icon: '🛠', label: 'Bakımda', tone: 'warning' },
  OFFLINE: { icon: '✕', label: 'Çevrimdışı', tone: 'neutral' },
}

export function machineStatusLabel(status: string): string {
  return machineStatusPresentation[status]?.label ?? status
}
