
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { AcademicYear } from './api'

type ResearchNorm = {
  id: number
  lecturerId: number
  academicYearId: number
  requiredHours: number
  basis: string | null
}

type Props = {
  academicYears: AcademicYear[]
  canEdit: boolean
}

export function ResearchNorms({ academicYears, canEdit }: Props) {
  const [norms, setNorms] = useState<ResearchNorm[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [editing, setEditing] = useState<ResearchNorm | null>(null)
  const [showForm, setShowForm] = useState(false)

  useEffect(() => {
    let active = true

    api<ResearchNorm[]>('/research-norms')
      .then(data => {
        if (active) setNorms(data)
      })
      .catch(e => {
        if (active) setError((e as Error).message)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => { active = false }
  }, [])

  async function saveNorm(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()

    if (!canEdit) {
      setError('Bạn không có quyền chỉnh sửa định mức.')
      return
    }

    const form = new FormData(e.currentTarget)
    const requiredHours = Number(form.get('requiredHours'))

    if (!Number.isFinite(requiredHours) || requiredHours < 0) {
      setError('Số giờ định mức phải lớn hơn hoặc bằng 0.')
      return
    }

    setBusy(true)
    setError('')
    setMessage('')

    try {
      const basis = String(form.get('basis') ?? '').trim() || null

      if (editing) {
        const updated = await api<ResearchNorm>(
          `/research-norms/${editing.id}`,
          'PUT',
          {
            requiredHours,
            basis
          }
        )

        setNorms(previous =>
          previous.map(item =>
            item.id === updated.id ? updated : item
          )
        )

        setMessage('Cập nhật định mức thành công.')
      } else {
        const lecturerId = Number(form.get('lecturerId'))
        const academicYearId = Number(form.get('academicYearId'))

        if (
          !Number.isSafeInteger(lecturerId) ||
          lecturerId <= 0 ||
          !Number.isSafeInteger(academicYearId) ||
          academicYearId <= 0
        ) {
          setError('Vui lòng chọn giảng viên và năm học hợp lệ.')
          return
        }

        const created = await api<ResearchNorm>(
          '/research-norms',
          'POST',
          {
            lecturerId,
            academicYearId,
            requiredHours,
            basis
          }
        )

        setNorms(previous => [...previous, created])
        setMessage('Tạo định mức thành công.')
      }

      setEditing(null)
      setShowForm(false)
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  function startEdit(norm: ResearchNorm) {
    if (!canEdit) return

    setEditing(norm)
    setShowForm(true)
    setError('')
    setMessage('')
  }

  return (
    <div className="research-norms">
      <p className="eyebrow">QUẢN LÝ NGHIÊN CỨU</p>

      <h1>Định mức nghiên cứu khoa học</h1>

      <p className="muted">
        {canEdit
          ? 'Quản lý số giờ nghiên cứu khoa học bắt buộc theo giảng viên và năm học.'
          : 'Tra cứu định mức giờ nghiên cứu khoa học theo năm học.'}
      </p>

      {error && <p className="alert" role="alert">{error}</p>}
      {message && <p role="status">{message}</p>}

      {canEdit && (
        <section className="panel">
          <button
            type="button"
            className="primary"
            onClick={() => {
              setEditing(null)
              setShowForm(!showForm)
              setError('')
              setMessage('')
            }}
            disabled={busy}
          >
            {showForm ? 'Đóng biểu mẫu' : '+ Thêm định mức'}
          </button>

          {showForm && (
            <form
              key={editing?.id ?? 'new'}
              onSubmit={saveNorm}
            >
              <h2>
                {editing
                  ? 'Chỉnh sửa định mức'
                  : 'Tạo định mức mới'}
              </h2>

              {!editing && (
                <>
                  <label>
                    Mã giảng viên
                    <input
                      name="lecturerId"
                      type="number"
                      min="1"
                      step="1"
                      required
                    />
                  </label>

                  <label>
                    Năm học
                    <select
                      name="academicYearId"
                      required
                      defaultValue=""
                    >
                      <option value="">Chọn năm học</option>

                      {academicYears.map(year => (
                        <option
                          key={year.id}
                          value={year.id}
                        >
                          {year.code}
                        </option>
                      ))}
                    </select>
                  </label>
                </>
              )}

              <label>
                Số giờ định mức
                <input
                  name="requiredHours"
                  type="number"
                  min="0"
                  step="any"
                  defaultValue={editing?.requiredHours ?? 0}
                  required
                />
              </label>

              <label>
                Căn cứ xác định định mức
                <textarea
                  name="basis"
                  rows={3}
                  defaultValue={editing?.basis ?? ''}
                  placeholder="Nhập căn cứ xác định định mức"
                />
              </label>

              <button
                type="submit"
                className="primary"
                disabled={busy}
              >
                {busy
                  ? 'Đang lưu…'
                  : editing
                    ? 'Lưu chỉnh sửa'
                    : 'Tạo định mức'}
              </button>
            </form>
          )}
        </section>
      )}

      <section className="panel">
        <h2>Danh sách định mức</h2>

        {loading ? (
          <p>Đang tải dữ liệu…</p>
        ) : norms.length === 0 ? (
          <p className="muted">
            Chưa có định mức nghiên cứu khoa học.
          </p>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>Giảng viên</th>
                  <th>Năm học</th>
                  <th>Số giờ định mức</th>
                  <th>Căn cứ</th>
                  {canEdit && <th>Thao tác</th>}
                </tr>
              </thead>

              <tbody>
                {norms.map(norm => (
                  <tr key={norm.id}>
                    <td>{norm.lecturerId}</td>

                    <td>
                      {academicYears.find(
                        y => y.id === norm.academicYearId
                      )?.code ?? norm.academicYearId}
                    </td>

                    <td>{norm.requiredHours}</td>

                    <td>{norm.basis || 'Chưa có'}</td>

                    {canEdit && (
                      <td>
                        <button
                          type="button"
                          onClick={() => startEdit(norm)}
                        >
                          Chỉnh sửa
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}
