export type Permission = { role: string; scope: string; facultyId: number | null; departmentId: number | null }
export type Profile = { id: number; username: string; fullName: string; email: string | null; permissions: Permission[] }
export type Faculty = { id: number; code: string; name: string }
export type Department = Faculty & { facultyId: number }
export type AcademicYear = { id: number; code: string; startDate: string; endDate: string }
export type Lookups = { faculties: Faculty[]; departments: Department[]; academicYears: AcademicYear[] }
export const roleNames: Record<string, string> = {
  QUAN_TRI: 'Quản trị viên', GIANG_VIEN: 'Giảng viên', TRUONG_BO_MON: 'Trưởng bộ môn',
  TRUONG_KHOA: 'Trưởng khoa', PHONG_QLKH: 'Phòng quản lý khoa học', BAN_GIAM_HIEU: 'Ban giám hiệu',
}
export const scopeNames: Record<string, string> = { CA_NHAN: 'Cá nhân', BO_MON: 'Bộ môn', KHOA: 'Khoa', TOAN_TRUONG: 'Toàn trường' }
export class ApiError extends Error { status: number; constructor(message: string, status: number) { super(message); this.status = status } }
export async function api<T>(path: string, method = 'GET', body?: unknown, notifyUnauthorized = true): Promise<T> {
  let response: Response
  try {
    response = await fetch(`/api${path}`, { method, credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'ResearchHub' },
      body: body === undefined ? undefined : JSON.stringify(body) })
  } catch { throw new ApiError('Không thể kết nối máy chủ. Vui lòng kiểm tra API và thử lại.', 0) }
  if (!response.ok) {
    if (response.status === 401 && notifyUnauthorized && path !== '/auth/login') window.dispatchEvent(new Event('session-expired'))
    const error = await response.json().catch(() => ({}))
    const validation = error.errors ? Object.values(error.errors).flat().join(' ') : ''
    throw new ApiError(validation || error.title || ({ 401: 'Phiên đăng nhập đã hết hạn.', 403: 'Bạn không có quyền thực hiện thao tác này.', 429: 'Bạn thử quá nhiều lần. Vui lòng đợi một phút.' }[response.status] ?? 'Không thể hoàn thành yêu cầu.'), response.status)
  }
  return response.status === 204 ? undefined as T : response.json()
}
