/** API farklı bir origin'de barındırılıyorsa VITE_API_ORIGIN ile verilir; aksi halde aynı origin. */
const API_ORIGIN: string = import.meta.env.VITE_API_ORIGIN ?? ''

export const API_BASE_URL = `${API_ORIGIN}/api/v1`
export const PRODUCTION_HUB_URL = `${API_ORIGIN}/hubs/production`
