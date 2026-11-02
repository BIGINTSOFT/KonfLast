import { baseApi } from '../../api/baseApi'

export type ApproverType = 'ROLE' | 'USERS'

export interface ApprovalUser {
  id: number
  userName: string
  fullName: string
}

export interface ApprovalFlowStep {
  id: number
  sequence: number
  name: string
  approverType: ApproverType
  roleId: number | null
  roleName: string | null
  /** TRY cinsinden tutar eşiği; boşsa her siparişte uygulanır. */
  minAmount: number | null
  users: ApprovalUser[]
}

export interface ApprovalFlow {
  documentType: 'SALES_ORDER'
  /** Akış hiç kaydedilmemiş; siparişler akışsız onaylanır. */
  isDefined: boolean
  name: string
  isActive: boolean
  steps: ApprovalFlowStep[]
  updatedAt: string | null
  updatedBy: string | null
}

export interface ApprovalFlowOptions {
  roles: { id: number; code: string; name: string }[]
  users: ApprovalUser[]
}

export interface SaveApprovalFlowRequest {
  name: string
  isActive: boolean
  steps: { name: string; approverType: ApproverType; roleId: number | null; userIds: number[] | null; minAmount: number | null }[]
}

export const approvalFlowsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getSalesOrderApprovalFlow: build.query<ApprovalFlow, void>({
      query: () => '/approval-flows/sales-order',
      providesTags: ['ApprovalFlow'],
    }),
    getApprovalFlowOptions: build.query<ApprovalFlowOptions, void>({
      query: () => '/approval-flows/sales-order/options',
      providesTags: ['ApprovalFlow', 'User', 'Role'],
    }),
    saveSalesOrderApprovalFlow: build.mutation<ApprovalFlow, SaveApprovalFlowRequest>({
      query: (body) => ({ url: '/approval-flows/sales-order', method: 'PUT', body }),
      invalidatesTags: ['ApprovalFlow'],
    }),
  }),
})

export const {
  useGetSalesOrderApprovalFlowQuery,
  useGetApprovalFlowOptionsQuery,
  useSaveSalesOrderApprovalFlowMutation,
} = approvalFlowsApi
