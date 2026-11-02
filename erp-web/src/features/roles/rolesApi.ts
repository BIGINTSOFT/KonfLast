import { baseApi } from '../../api/baseApi'
import type { Permission, Role } from '../../types/api'

export const rolesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getRoles: build.query<Role[], void>({
      query: () => '/roles',
      providesTags: ['Role'],
    }),
    getPermissions: build.query<Permission[], void>({
      query: () => '/permissions',
    }),
    updateRolePermissions: build.mutation<Role, { id: number; permissions: string[] }>({
      query: ({ id, permissions }) => ({ url: `/roles/${id}/permissions`, method: 'PUT', body: { permissions } }),
      invalidatesTags: ['Role'],
    }),
  }),
})

export const { useGetRolesQuery, useGetPermissionsQuery, useUpdateRolePermissionsMutation } = rolesApi
