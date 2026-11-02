/** Yüklenen fotoğrafın kenar uzunluğu (piksel); tablette büyük gösterim için yeterli, dosya küçük kalır. */
export const PHOTO_SIZE = 480

const JPEG_QUALITY = 0.85

export interface PhotoUpload {
  contentType: string
  dataBase64: string
}

/** Görüntünün ortasından alınacak en büyük kare (vesikalık çerçevesi). */
function centerSquare(width: number, height: number): { x: number; y: number; size: number } {
  const size = Math.min(width, height)
  return { x: Math.round((width - size) / 2), y: Math.round((height - size) / 2), size }
}

/** Fotoğrafın `<img>` içinde gösterilebilir adresi. */
export function photoDataUri(photo: PhotoUpload): string {
  return `data:${photo.contentType};base64,${photo.dataBase64}`
}

/**
 * Seçilen görüntüyü ortadan kare kırpar, {@link PHOTO_SIZE} piksele küçültür ve JPEG'e çevirir.
 * Böylece telefonla çekilmiş büyük fotoğraflar da sunucunun boyut sınırına takılmaz.
 */
export async function preparePhoto(file: File): Promise<PhotoUpload> {
  const bitmap = await createImageBitmap(file)
  try {
    const crop = centerSquare(bitmap.width, bitmap.height)
    const canvas = document.createElement('canvas')
    canvas.width = PHOTO_SIZE
    canvas.height = PHOTO_SIZE
    canvas.getContext('2d')!.drawImage(bitmap, crop.x, crop.y, crop.size, crop.size, 0, 0, PHOTO_SIZE, PHOTO_SIZE)

    const dataUri = canvas.toDataURL('image/jpeg', JPEG_QUALITY)
    return { contentType: 'image/jpeg', dataBase64: dataUri.slice(dataUri.indexOf(',') + 1) }
  } finally {
    bitmap.close()
  }
}
