
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { Lookups } from './api'

type Target = {
  id: number
  facultyId: number
  academicYearId: number
  assignedArticleCount: number
  deadline: string | null
}

type Allocation = {
  id: number
  researchTargetId: number
  facultyId: number
  departmentId: number
  allocatedArticleCount: number
  allocationDate: string
}

type Props = {
  lookups: Lookups | null
  canEdit: boolean
}

export function ResearchTargets({ lookups, canEdit }: Props) {
  const [targets, setTargets] = useState<Target[]>([])
  const [allocations, setAllocations] = useState<Allocation[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [editingTarget, setEditingTarget] = useState<Target | null>(null)
  const [editingAllocation, setEditingAllocation] = useState<Allocation | null>(null)
  const [targetFormOpen, setTargetFormOpen] = useState(false)
  const [allocationFormOpen, setAllocationFormOpen] = useState(false)

  const selectedTarget = targets.find(t => t.id === selectedId)
  const faculties = lookups?.faculties ?? []
  const departments = lookups?.departments ?? []
  const years = lookups?.academicYears ?? []

  const facultyName = (id: number) =>
    faculties.find(f => f.id === id)?.name ?? `Khoa #${id}`

  const departmentName = (id: number) =>
    departments.find(d => d.id === id)?.name ?? `Bộ môn #${id}`

  const yearName = (id: number) =>
    years.find(y => y.id === id)?.code ?? `Năm học #${id}`

  async function loadTargets() {
    setLoading(true)
    setError('')
    try {
      const data = await api<Target[]>('/research-targets')
      setTargets(data)
      setSelectedId(current =>
        current !== null && data.some(t => t.id === current)
          ? current
          : data[0]?.id ?? null
      )
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadTargets()
  }, [])

  useEffect(() => {
    if (selectedId === null) {
      setAllocations([])
      return
    }

    let active = true
    setAllocations([])
    setError('')

    api<Allocation[]>(
      `/research-target-allocations?researchTargetId=${selectedId}`
    )
      .then(data => {
        if (active) setAllocations(data)
      })
      .catch(e => {
        if (active) setError((e as Error).message)
      })

    return () => {
      active = false
    }
  }, [selectedId])

  async function saveTarget(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!canEdit || busy) return

    const form = new FormData(e.currentTarget)
    const count = Number(form.get('assignedArticleCount'))

    if (!Number.isInteger(count) || count < 0) {
      setError('Số bài chỉ tiêu phải là số nguyên không âm.')
      return
    }

    const deadline = String(form.get('deadline') || '') || null

    const body = editingTarget
      ? { assignedArticleCount: count, deadline }
      : {
          facultyId: Number(form.get('facultyId')),
          academicYearId: Number(form.get('academicYearId')),
          assignedArticleCount: count,
          deadline,
        }

    setBusy(true)
    setError('')
    setMessage('')

    try {
      if (editingTarget) {
        await api(`/research-targets/${editingTarget.id}`, 'PUT', body)
      } else {
        await api('/research-targets', 'POST', body)
      }

      setTargetFormOpen(false)
      setEditingTarget(null)
      setMessage('Lưu chỉ tiêu thành công.')
      await loadTargets()
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  async function saveAllocation(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!canEdit || busy || !selectedTarget) return

    const form = new FormData(e.currentTarget)
    const count = Number(form.get('allocatedArticleCount'))
    const allocationDate = String(form.get('allocationDate'))

    if (!Number.isInteger(count) || count < 0) {
      setError('Số bài phân bổ phải là số nguyên không âm.')
      return
    }

    if (!allocationDate) {
      setError('Vui lòng chọn ngày phân bổ.')
      return
    }

    const body = editingAllocation
      ? { allocatedArticleCount: count, allocationDate }
      : {
          researchTargetId: selectedTarget.id,
          facultyId: selectedTarget.facultyId,
          departmentId: Number(form.get('departmentId')),
          allocatedArticleCount: count,
          allocationDate,
        }

    setBusy(true)
    setError('')
    setMessage('')

    try {
      if (editingAllocation) {
        await api(
          `/research-target-allocations/${editingAllocation.id}`,
          'PUT',
          body
        )
      } else {
        await api('/research-target-allocations', 'POST', body)
      }

      const data = await api<Allocation[]>(
        `/research-target-allocations?researchTargetId=${selectedTarget.id}`
      )
      setAllocations(data)
      setAllocationFormOpen(false)
      setEditingAllocation(null)
      setMessage('Lưu phân bổ thành công.')
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  function openNewTarget() {
    setEditingTarget(null)
    setTargetFormOpen(true)
    setAllocationFormOpen(false)
    setError('')
    setMessage('')
  }

  function openEditTarget(target: Target) {
    setEditingTarget(target)
    setTargetFormOpen(true)
    setAllocationFormOpen(false)
    setError('')
    setMessage('')
  }

  function openNewAllocation() {
    setEditingAllocation(null)
    setAllocationFormOpen(true)
    setTargetFormOpen(false)
    setError('')
    setMessage('')
  }

  function openEditAllocation(allocation: Allocation) {
    setEditingAllocation(allocation)
    setAllocationFormOpen(true)
    setTargetFormOpen(false)
    setError('')
    setMessage('')
  }

  const allocatedTotal = allocations.reduce(
    (sum, item) => sum + item.allocatedArticleCount, 0
  )

  const availableDepartments = departments.filter(
    d => d.facultyId === selectedTarget?.facultyId &&
      !allocations.some(a => a.departmentId === d.id)
  )

  return (
    <>
      <p className="eyebrow">QUẢN LÝ NGHIÊN CỨU KHOA HỌC</p>
      <h1>Chỉ tiêu và phân bổ NCKH</h1>
      <p className="muted">
        Quản lý số bài báo khoa học được giao theo khoa, năm học
        và phân bổ chỉ tiêu cho từng bộ môn.
      </p>

      {error && <p className="alert" role="alert">{error}</p>}
      {message && <p role="status">{message}</p>}

      <section className="panel">
        <div className="section-heading">
          <h2>Danh sách chỉ tiêu</h2>
          {canEdit && (
            <button
              type="button"
              className="primary"
              onClick={openNewTarget}
              disabled={!lookups}
            >
              + Thêm chỉ tiêu
            </button>
          )}
        </div>

        {loading ? (
          <p>Đang tải danh sách chỉ tiêu...</p>
        ) : targets.length === 0 ? (
          <p className="muted">Chưa có chỉ tiêu NCKH.</p>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>Khoa</th>
                  <th>Năm học</th>
                  <th>Số bài giao</th>
                  <th>Hạn hoàn thành</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {targets.map(target => (
                  <tr key={target.id}>
                    <td>{facultyName(target.facultyId)}</td>
                    <td>{yearName(target.academicYearId)}</td>
                    <td>{target.assignedArticleCount}</td>
                    <td>{target.deadline || 'Chưa quy định'}</td>
                    <td>
                      <button
                        type="button"
                        onClick={() => {
                          setSelectedId(target.id)
                          setTargetFormOpen(false)
                          setAllocationFormOpen(false)
                        }}
                      >
                        {selectedId === target.id ? 'Đang xem' : 'Xem phân bổ'}
                      </button>
                      {' '}
                      {canEdit && (
                        <button
                          type="button"
                          onClick={() => openEditTarget(target)}
                        >
                          Sửa
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {canEdit && targetFormOpen && (
        <section className="panel">
          <h2>{editingTarget ? 'Sửa chỉ tiêu' : 'Thêm chỉ tiêu'}</h2>
          <form key={editingTarget?.id ?? 'new'} onSubmit={saveTarget}>
            {!editingTarget && (
              <>
                <label>
                  Khoa
                  <select name="facultyId" required>
                    <option value="">-- Chọn khoa --</option>
                    {faculties.map(f => (
                      <option key={f.id} value={f.id}>{f.name}</option>
                    ))}
                  </select>
                </label>
                <label>
                  Năm học
                  <select name="academicYearId" required>
                    <option value="">-- Chọn năm học --</option>
                    {years.map(y => (
                      <option key={y.id} value={y.id}>{y.code}</option>
                    ))}
                  </select>
                </label>
              </>
            )}

            {editingTarget && (
              <p className="muted">
                {facultyName(editingTarget.facultyId)} —{' '}
                {yearName(editingTarget.academicYearId)}
              </p>
            )}

            <label>
              Số bài báo được giao
              <input
                name="assignedArticleCount"
                type="number"
                min="0"
                step="1"
                required
                defaultValue={editingTarget?.assignedArticleCount ?? 0}
              />
            </label>

            <label>
              Hạn hoàn thành
              <input
                name="deadline"
                type="date"
                defaultValue={editingTarget?.deadline ?? ''}
              />
            </label>

            <div className="actions">
              <button className="primary" disabled={busy}>
                {busy ? 'Đang lưu...' : 'Lưu chỉ tiêu'}
              </button>
              <button
                type="button"
                onClick={() => setTargetFormOpen(false)}
                disabled={busy}
              >
                Hủy
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="panel">
        <div className="section-heading">
          <h2>Phân bổ chỉ tiêu theo bộ môn</h2>
          {canEdit && selectedTarget && (
            <button
              type="button"
              className="primary"
              onClick={openNewAllocation}
              disabled={availableDepartments.length === 0}
            >
              + Thêm phân bổ
            </button>
          )}
        </div>

        {!selectedTarget ? (
          <p className="muted">Chọn chỉ tiêu để xem phân bổ.</p>
        ) : (
          <>
            <p className="muted">
              {facultyName(selectedTarget.facultyId)} —{' '}
              {yearName(selectedTarget.academicYearId)}
            </p>
            <p>
              <strong>Chỉ tiêu:</strong>{' '}
              {selectedTarget.assignedArticleCount} bài
              {' · '}
              <strong>Đã phân bổ:</strong> {allocatedTotal} bài
              {' · '}
              <strong>Chênh lệch:</strong>{' '}
              {selectedTarget.assignedArticleCount - allocatedTotal} bài
            </p>

            {allocations.length === 0 ? (
              <p className="muted">Chưa có phân bổ cho chỉ tiêu này.</p>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table>
                  <thead>
                    <tr>
                      <th>Bộ môn</th>
                      <th>Số bài phân bổ</th>
                      <th>Ngày phân bổ</th>
                      <th>Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {allocations.map(item => (
                      <tr key={item.id}>
                        <td>{departmentName(item.departmentId)}</td>
                        <td>{item.allocatedArticleCount}</td>
                        <td>{item.allocationDate}</td>
                        <td>
                          {canEdit ? (
                            <button
                              type="button"
                              onClick={() => openEditAllocation(item)}
                            >
                              Sửa
                            </button>
                          ) : 'Chỉ xem'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </section>

      {canEdit && allocationFormOpen && selectedTarget && (
        <section className="panel">
          <h2>
            {editingAllocation ? 'Sửa phân bổ' : 'Thêm phân bổ'}
          </h2>

          <form
            key={editingAllocation?.id ?? `new-${selectedTarget.id}`}
            onSubmit={saveAllocation}
          >
            {!editingAllocation ? (
              <label>
                Bộ môn
                <select name="departmentId" required>
                  <option value="">-- Chọn bộ môn --</option>
                  {availableDepartments.map(d => (
                    <option key={d.id} value={d.id}>{d.name}</option>
                  ))}
                </select>
              </label>
            ) : (
              <p className="muted">
                {departmentName(editingAllocation.departmentId)}
              </p>
            )}

            <label>
              Số bài phân bổ
              <input
                name="allocatedArticleCount"
                type="number"
                min="0"
                step="1"
                required
                defaultValue={
                  editingAllocation?.allocatedArticleCount ?? 0
                }
              />
            </label>

            <label>
              Ngày phân bổ
              <input
                name="allocationDate"
                type="date"
                required
                defaultValue={
                  editingAllocation?.allocationDate ??
                  new Date().toLocaleDateString('en-CA')
                }
              />
            </label>

            <div className="actions">
              <button className="primary" disabled={busy}>
                {busy ? 'Đang lưu...' : 'Lưu phân bổ'}
              </button>
              <button
                type="button"
                onClick={() => setAllocationFormOpen(false)}
                disabled={busy}
              >
                Hủy
              </button>
            </div>
          </form>
        </section>
      )}
    </>
  )
}
