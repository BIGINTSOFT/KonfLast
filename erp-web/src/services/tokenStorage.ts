const KEY = 'erp.refreshToken'
const LAST_USER_KEY = 'erp.lastUserName'

function read(storage: () => Storage, key: string): string | null {
  try {
    return storage().getItem(key)
  } catch {
    return null
  }
}

function write(storage: () => Storage, key: string, value: string | null): void {
  try {
    if (value === null) {
      storage().removeItem(key)
    } else {
      storage().setItem(key, value)
    }
  } catch {
    // Depolama kapalıysa oturum yalnızca bu sekme açıkken sürer.
  }
}

/**
 * Refresh token: "Beni hatırla" işaretliyse localStorage'da (tarayıcı kapansa da oturum sürer), değilse
 * sessionStorage'da (tarayıcı sekmesi kapanınca biter) tutulur. Access token yalnızca bellekte (Redux) yaşar.
 */
export const tokenStorage = {
  get(): string | null {
    return read(() => localStorage, KEY) ?? read(() => sessionStorage, KEY)
  },
  set(token: string, persistent: boolean): void {
    write(() => (persistent ? localStorage : sessionStorage), KEY, token)
    write(() => (persistent ? sessionStorage : localStorage), KEY, null)
  },
  clear(): void {
    write(() => localStorage, KEY, null)
    write(() => sessionStorage, KEY, null)
  },
  /** "Beni hatırla" ile girilen son kullanıcı adı (giriş formuna önceden yazılır). */
  lastUserName(): string | null {
    return read(() => localStorage, LAST_USER_KEY)
  },
  rememberUserName(userName: string | null): void {
    write(() => localStorage, LAST_USER_KEY, userName)
  },
}
