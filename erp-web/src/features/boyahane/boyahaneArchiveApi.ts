import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DyeChemicalKind } from './boyahaneLabApi'

export type PrintType = 'PIGMENT' | 'REACTIVE' | 'DISCHARGE' | 'OTHER'
export type RecipeAmountUnit = 'PERCENT' | 'GRAMS_PER_LITER'

export const printTypeLabels: Record<PrintType, string> = {
  PIGMENT: 'Pigment',
  REACTIVE: 'Reaktif',
  DISCHARGE: 'Aşındırma',
  OTHER: 'Diğer',
}

/** Söküm reçetesinde miktarın ölçüsü: mal ağırlığına göre % ya da flotte suyuna göre g/L. */
export const recipeAmountUnitLabels: Record<RecipeAmountUnit, string> = {
  PERCENT: '%',
  GRAMS_PER_LITER: 'g/L',
}

export interface BoyahaneArchiveImage {
  contentType: string
  dataBase64: string
  updatedAt: string
}

export interface BoyahaneArchiveImageUpload {
  contentType: string
  dataBase64: string
}

export interface BoyahaneColorCard {
  id: number
  code: string
  name: string
  customerId: number | null
  customerName: string | null
  customerColorCode: string | null
  pantone: string | null
  labL: number | null
  labA: number | null
  labB: number | null
  notes: string | null
  hasImage: boolean
  recipeCount: number
  isActive: boolean
}

export interface BoyahaneColorCardRequest {
  code: string
  name: string
  customerId: number | null
  customerColorCode: string | null
  pantone: string | null
  labL: number | null
  labA: number | null
  labB: number | null
  notes: string | null
  isActive: boolean
}

export interface BoyahaneColorCardRecipe {
  trialId: number
  requestId: number
  requestNo: string
  trialCode: string
  orderId: number
  orderNo: string
  customerName: string | null
  itemCode: string | null
  itemName: string | null
  decisionOn: string | null
  items: { code: string; name: string; kind: DyeChemicalKind; amount: number }[]
}

export interface BoyahanePattern {
  id: number
  code: string
  name: string
  customerId: number | null
  customerName: string | null
  repeatWidthCm: number | null
  repeatHeightCm: number | null
  printType: PrintType
  screenCount: number | null
  notes: string | null
  hasImage: boolean
  colorwayCount: number
  isActive: boolean
}

export interface BoyahanePatternRequest {
  code: string
  name: string
  customerId: number | null
  repeatWidthCm: number | null
  repeatHeightCm: number | null
  printType: PrintType
  screenCount: number | null
  notes: string | null
  isActive: boolean
}

export interface BoyahanePrintRecipeLine {
  id: number
  dyeChemicalId: number
  code: string
  name: string
  kind: DyeChemicalKind
  gramsPerKilogram: number
}

export interface BoyahaneColorwayColor {
  id: number
  position: number
  colorCardId: number | null
  colorCardCode: string | null
  name: string
  pasteKilogramsPerMeter: number | null
  lines: BoyahanePrintRecipeLine[]
}

export interface BoyahaneColorway {
  id: number
  patternId: number
  code: string
  name: string
  isActive: boolean
  orderLineCount: number
  colors: BoyahaneColorwayColor[]
}

export interface BoyahaneColorwayRequest {
  code: string
  name: string
  isActive: boolean
  colors: {
    position: number
    colorCardId: number | null
    name: string | null
    pasteKilogramsPerMeter: number | null
    lines: { dyeChemicalId: number; gramsPerKilogram: number }[]
  }[]
}

export interface BoyahaneStrippingRecipeLine {
  id: number
  sequence: number
  dyeChemicalId: number
  code: string
  name: string
  amount: number
  unit: RecipeAmountUnit
  temperatureCelsius: number | null
  durationMinutes: number | null
}

export interface BoyahaneStrippingRecipe {
  id: number
  code: string
  name: string
  notes: string | null
  isActive: boolean
  usageCount: number
  lines: BoyahaneStrippingRecipeLine[]
}

export interface BoyahaneStrippingRecipeRequest {
  code: string
  name: string
  notes: string | null
  isActive: boolean
  lines: { dyeChemicalId: number; amount: number; unit: RecipeAmountUnit; temperatureCelsius: number | null; durationMinutes: number | null }[]
}

// Seçim kutuları (LookupSelect) ortak 'MasterData' etiketiyle önbelleğe alınır; kayıt değişince onlar da yenilenir.
const tags = ['BoyahaneLab', 'MasterData'] as const

export const boyahaneArchiveApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneColorCards: build.query<PagedResult<BoyahaneColorCard>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-color-cards', params }),
      providesTags: ['BoyahaneLab'],
    }),
    getBoyahaneColorCard: build.query<BoyahaneColorCard, number>({
      query: (id) => `/boyahane-color-cards/${id}`,
      providesTags: ['BoyahaneLab'],
    }),
    saveBoyahaneColorCard: build.mutation<BoyahaneColorCard, { id?: number; body: BoyahaneColorCardRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-color-cards' : `/boyahane-color-cards/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    deleteBoyahaneColorCard: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-color-cards/${id}`, method: 'DELETE' }),
      invalidatesTags: [...tags],
    }),
    getBoyahaneColorCardImage: build.query<BoyahaneArchiveImage, number>({
      query: (id) => `/boyahane-color-cards/${id}/image`,
      providesTags: ['BoyahaneLab'],
    }),
    saveBoyahaneColorCardImage: build.mutation<null, { id: number } & BoyahaneArchiveImageUpload>({
      query: ({ id, ...body }) => ({ url: `/boyahane-color-cards/${id}/image`, method: 'PUT', body }),
      invalidatesTags: ['BoyahaneLab'],
    }),
    deleteBoyahaneColorCardImage: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-color-cards/${id}/image`, method: 'DELETE' }),
      invalidatesTags: ['BoyahaneLab'],
    }),
    getBoyahaneColorCardRecipes: build.query<BoyahaneColorCardRecipe[], number>({
      query: (id) => `/boyahane-color-cards/${id}/recipes`,
      providesTags: ['BoyahaneLab'],
    }),

    getBoyahanePatterns: build.query<PagedResult<BoyahanePattern>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-patterns', params }),
      providesTags: ['BoyahaneLab'],
    }),
    getBoyahanePattern: build.query<BoyahanePattern, number>({
      query: (id) => `/boyahane-patterns/${id}`,
      providesTags: ['BoyahaneLab'],
    }),
    saveBoyahanePattern: build.mutation<BoyahanePattern, { id?: number; body: BoyahanePatternRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-patterns' : `/boyahane-patterns/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    deleteBoyahanePattern: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-patterns/${id}`, method: 'DELETE' }),
      invalidatesTags: [...tags],
    }),
    getBoyahanePatternImage: build.query<BoyahaneArchiveImage, number>({
      query: (id) => `/boyahane-patterns/${id}/image`,
      providesTags: ['BoyahaneLab'],
    }),
    saveBoyahanePatternImage: build.mutation<null, { id: number } & BoyahaneArchiveImageUpload>({
      query: ({ id, ...body }) => ({ url: `/boyahane-patterns/${id}/image`, method: 'PUT', body }),
      invalidatesTags: ['BoyahaneLab'],
    }),
    deleteBoyahanePatternImage: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-patterns/${id}/image`, method: 'DELETE' }),
      invalidatesTags: ['BoyahaneLab'],
    }),
    getBoyahanePatternColorways: build.query<BoyahaneColorway[], number>({
      query: (id) => `/boyahane-patterns/${id}/colorways`,
      providesTags: ['BoyahaneLab'],
    }),
    saveBoyahanePatternColorway: build.mutation<BoyahaneColorway, { patternId: number; colorwayId?: number; body: BoyahaneColorwayRequest }>({
      query: ({ patternId, colorwayId, body }) => ({
        url: colorwayId === undefined ? `/boyahane-patterns/${patternId}/colorways` : `/boyahane-patterns/${patternId}/colorways/${colorwayId}`,
        method: colorwayId === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: [...tags],
    }),
    deleteBoyahanePatternColorway: build.mutation<null, { patternId: number; colorwayId: number }>({
      query: ({ patternId, colorwayId }) => ({ url: `/boyahane-patterns/${patternId}/colorways/${colorwayId}`, method: 'DELETE' }),
      invalidatesTags: [...tags],
    }),

    getBoyahaneStrippingRecipes: build.query<PagedResult<BoyahaneStrippingRecipe>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-stripping-recipes', params }),
      providesTags: ['BoyahaneLab'],
    }),
    saveBoyahaneStrippingRecipe: build.mutation<BoyahaneStrippingRecipe, { id?: number; body: BoyahaneStrippingRecipeRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-stripping-recipes' : `/boyahane-stripping-recipes/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    deleteBoyahaneStrippingRecipe: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-stripping-recipes/${id}`, method: 'DELETE' }),
      invalidatesTags: [...tags],
    }),
  }),
})

export const {
  useGetBoyahaneColorCardsQuery,
  useGetBoyahaneColorCardQuery,
  useSaveBoyahaneColorCardMutation,
  useDeleteBoyahaneColorCardMutation,
  useGetBoyahaneColorCardImageQuery,
  useSaveBoyahaneColorCardImageMutation,
  useDeleteBoyahaneColorCardImageMutation,
  useGetBoyahaneColorCardRecipesQuery,
  useGetBoyahanePatternsQuery,
  useGetBoyahanePatternQuery,
  useSaveBoyahanePatternMutation,
  useDeleteBoyahanePatternMutation,
  useGetBoyahanePatternImageQuery,
  useSaveBoyahanePatternImageMutation,
  useDeleteBoyahanePatternImageMutation,
  useGetBoyahanePatternColorwaysQuery,
  useSaveBoyahanePatternColorwayMutation,
  useDeleteBoyahanePatternColorwayMutation,
  useGetBoyahaneStrippingRecipesQuery,
  useSaveBoyahaneStrippingRecipeMutation,
  useDeleteBoyahaneStrippingRecipeMutation,
} = boyahaneArchiveApi

/** Görüntünün uzun kenarı en fazla bu kadar piksel olur; numune ve desen için yeterli, dosya sunucu sınırının (2 MB) altında kalır. */
const IMAGE_MAX_EDGE = 1600

const JPEG_QUALITY = 0.85

/** Görüntünün `<img>` içinde gösterilebilir adresi. */
export function archiveImageUri(image: BoyahaneArchiveImageUpload): string {
  return `data:${image.contentType};base64,${image.dataBase64}`
}

/** Seçilen görüntüyü oranını koruyarak küçültür ve JPEG'e çevirir; renk numunesi ve desen görüntüsü için. */
export async function prepareArchiveImage(file: File): Promise<BoyahaneArchiveImageUpload> {
  const bitmap = await createImageBitmap(file)
  try {
    const scale = Math.min(1, IMAGE_MAX_EDGE / Math.max(bitmap.width, bitmap.height))
    const canvas = document.createElement('canvas')
    canvas.width = Math.max(1, Math.round(bitmap.width * scale))
    canvas.height = Math.max(1, Math.round(bitmap.height * scale))
    canvas.getContext('2d')!.drawImage(bitmap, 0, 0, canvas.width, canvas.height)

    const dataUri = canvas.toDataURL('image/jpeg', JPEG_QUALITY)
    return { contentType: 'image/jpeg', dataBase64: dataUri.slice(dataUri.indexOf(',') + 1) }
  } finally {
    bitmap.close()
  }
}
