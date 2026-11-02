import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult, User } from '../../types/api'

export type UserListParams = {
  page: number
  pageSize: number
  search?: string
  isActive?: boolean
} & GridParams

export interface CreateUserRequest {
  userName: string
  fullName: string
  email: string | null
  password: string
  roles: string[]
}

export interface UpdateUserRequest {
  id: number
  fullName: string
  email: string | null
  isActive: boolean
  roles: string[]
}

export const usersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getUsers: build.query<PagedResult<User>, UserListParams>({
      query: (params) => ({ url: '/users', params }),
      providesTags: ['User'],
    }),
    createUser: build.mutation<User, CreateUserRequest>({
      query: (body) => ({ url: '/users', method: 'POST', body }),
      invalidatesTags: ['User'],
    }),
    updateUser: build.mutation<User, UpdateUserRequest>({
      query: ({ id, ...body }) => ({ url: `/users/${id}`, method: 'PUT', body }),
      invalidatesTags: ['User'],
    }),
    resetUserPassword: build.mutation<null, { id: number; newPassword: string }>({
      query: ({ id, newPassword }) => ({ url: `/users/${id}/reset-password`, method: 'POST', body: { newPassword } }),
      invalidatesTags: ['User'],
    }),
    deleteUser: build.mutation<null, number>({
      query: (id) => ({ url: `/users/${id}`, method: 'DELETE' }),
      invalidatesTags: ['User'],
    }),
  }),
})

export const {
  useGetUsersQuery,
  useCreateUserMutation,
  useUpdateUserMutation,
  useResetUserPasswordMutation,
  useDeleteUserMutation,
} = usersApi
