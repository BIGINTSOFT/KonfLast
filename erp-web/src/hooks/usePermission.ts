import { useAppSelector } from '../store/hooks'

export function usePermission(permission: string): boolean {
  return useAppSelector((state) => state.auth.user?.permissions.includes(permission) ?? false)
}
