
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { Lookups } from './api'
import { Toast } from './Toast'

type Target = {
  id: number
  facultyId: number
  academicYearId: number
  assignedArticleCount: number
  deadline: string | null
  journalConferenceId: number | null
  status: string
}
type JournalConference = { id: number; name: string; type: string }

type Allocation = {
  id: number
  researchTargetId: number
  facultyId: number
  departmentId: number
  allocatedArticleCount: number
  allocationDate: string
}
type LecturerOption = { id: number; code: string; fullName: string }
type LecturerAssignment = { id: number; departmentAllocationId: number; lecturerId: number; lecturerCode: string; lecturerName: string; allocatedArticleCount: number; allocationDate: string }
type TargetProgress = { assignedArticleCount: number; approvedArticles: number; completionPercent: number; isCompleted: boolean; units: { departmentId: number; departmentName: string; allocatedArticles: number; approvedArticles: number; completionPercent: number; isCompleted: boolean }[] }

type Props = {
  lookups: Lookups | null
  canEdit: boolean
  canEditAllocations: boolean
  roles: string[]
}

export function ResearchTargets({ lookups, canEdit, canEditAllocations, roles }: Props) {
  const [targets, setTargets] = useState<Target[]>([])
  const [allocations, setAllocations] = useState<Allocation[]>([])
  const [catalog, setCatalog] = useState<JournalConference[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [editingTarget, setEditingTarget] = useState<Target | null>(null)
  const [editingAllocation, setEditingAllocation] = useState<Allocation | null>(null)
  const [targetFormOpen, setTargetFormOpen] = useState(false)
  const [targetYearId, setTargetYearId] = useState<number | null>(null)
  const [allocationFormOpen, setAllocationFormOpen] = useState(false)
  const [selectedAllocation, setSelectedAllocation] = useState<Allocation | null>(null)
  const [lecturerOptions, setLecturerOptions] = useState<LecturerOption[]>([])
  const [lecturerAssignments, setLecturerAssignments] = useState<LecturerAssignment[]>([])
  const [progress, setProgress] = useState<TargetProgress | null>(null)
  const [targetPanel, setTargetPanel] = useState<'overview' | 'allocations' | 'lecturers'>('overview')

  const selectedTarget = targets.find(t => t.id === selectedId)
  const canReceiveAndDistribute = roles.some(x => x === 'TRUONG_KHOA' || x === 'QUAN_TRI')
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
    api<JournalConference[]>('/journal-conferences').then(setCatalog).catch(() => setCatalog([]))
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

  useEffect(() => {
    if (selectedId === null) { setProgress(null); return }
    let active = true
    api<TargetProgress>(`/research-progress/targets/${selectedId}`)
      .then(data => { if (active) setProgress(data) })
      .catch(() => { if (active) setProgress(null) })
    return () => { active = false }
  }, [selectedId, allocations])

  async function saveTarget(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!canEdit || busy) return

    const form = new FormData(e.currentTarget)
    const count = Number(form.get('assignedArticleCount'))

    if (!Number.isInteger(count) || count <= 0) {
      setError('Số bài chỉ tiêu phải là số nguyên lớn hơn 0.')
      return
    }

    const deadline = String(form.get('deadline') || '') || null
    const academicYearId = editingTarget?.academicYearId ?? Number(form.get('academicYearId'))
    const academicYear = years.find(y => y.id === academicYearId)
    if (deadline && academicYear && (deadline < academicYear.startDate || deadline > academicYear.endDate)) {
      setError(`Hạn hoàn thành phải nằm trong năm học ${academicYear.code} (${academicYear.startDate} đến ${academicYear.endDate}).`)
      return
    }
    const today = new Date().toLocaleDateString('en-CA')
    if (deadline && deadline < today) {
      setError('Hạn hoàn thành không được nằm trong quá khứ.')
      return
    }

    const body = editingTarget
      ? { assignedArticleCount: count, deadline, journalConferenceId: Number(form.get('journalConferenceId')) || null }
      : {
        facultyId: Number(form.get('facultyId')),
        academicYearId,
        assignedArticleCount: count,
        deadline,
        journalConferenceId: Number(form.get('journalConferenceId')) || null,
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
    if (!canEditAllocations || busy || !selectedTarget) return

    const form = new FormData(e.currentTarget)
    const count = Number(form.get('allocatedArticleCount'))

    if (!Number.isInteger(count) || count < 0) {
      setError('Số bài phân bổ phải là số nguyên không âm.')
      return
    }

    const body = editingAllocation
      ? { allocatedArticleCount: count }
      : {
          researchTargetId: selectedTarget.id,
          facultyId: selectedTarget.facultyId,
          departmentId: Number(form.get('departmentId')),
          allocatedArticleCount: count,
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

  async function openLecturerAssignments(allocation: Allocation) {
    setSelectedAllocation(allocation); setTargetPanel('lecturers'); setError(''); setMessage('')
    try {
      const [items, options] = await Promise.all([
        api<LecturerAssignment[]>(`/lecturer-target-allocations?departmentAllocationId=${allocation.id}`),
        api<LecturerOption[]>(`/lecturer-target-allocations/options?departmentAllocationId=${allocation.id}`)
      ])
      setLecturerAssignments(items); setLecturerOptions(options)
    } catch (e) { setError((e as Error).message) }
  }

  async function saveLecturerAssignment(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); if (!selectedAllocation || busy) return
    const element = e.currentTarget
    const form = new FormData(element)
    setBusy(true); setError(''); setMessage('')
    try {
      await api('/lecturer-target-allocations', 'POST', {
        departmentAllocationId: selectedAllocation.id,
        lecturerId: Number(form.get('lecturerId')),
        allocatedArticleCount: Number(form.get('allocatedArticleCount'))
      })
      await openLecturerAssignments(selectedAllocation)
      setMessage('Đã phân công chỉ tiêu cho giảng viên.')
      element.reset()
    } catch (e) { setError((e as Error).message) }
    finally { setBusy(false) }
  }

  async function deleteLecturerAssignment(id: number) {
    if (!selectedAllocation || busy) return
    setBusy(true); setError('')
    try { await api(`/lecturer-target-allocations/${id}`, 'DELETE'); await openLecturerAssignments(selectedAllocation) }
    catch (e) { setError((e as Error).message) }
    finally { setBusy(false) }
  }

  async function transition(action: 'accept' | 'publish' | 'lock' | 'cancel') {
    if (!selectedTarget || busy) return
    setBusy(true); setError(''); setMessage('')
    try {
      await api(`/research-targets/${selectedTarget.id}/${action}`, 'POST')
      setMessage('Cập nhật trạng thái chỉ tiêu thành công.')
      await loadTargets()
    } catch (e) { setError((e as Error).message) }
    finally { setBusy(false) }
  }

  function openNewTarget() {
    setEditingTarget(null)
    setTargetYearId(null)
    setTargetFormOpen(true)
    setAllocationFormOpen(false)
    setError('')
    setMessage('')
  }

  function openEditTarget(target: Target) {
    setEditingTarget(target)
    setTargetYearId(target.academicYearId)
    setTargetFormOpen(true)
    setAllocationFormOpen(false)
    setError('')
    setMessage('')
  }

  function openNewAllocation() {
    setEditingAllocation(null)
    setAllocationFormOpen(true)
    setTargetFormOpen(false)
    setTargetPanel('allocations')
    setError('')
    setMessage('')
  }

  function openEditAllocation(allocation: Allocation) {
    setEditingAllocation(allocation)
    setAllocationFormOpen(true)
    setTargetFormOpen(false)
    setTargetPanel('allocations')
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
  const isAllocationLocked = selectedTarget?.status === 'DA_KHOA'
  const canAssignLecturers = roles.some(x => ['TRUONG_BO_MON', 'QUAN_TRI'].includes(x))
  const canViewLecturerAssignments = roles.some(x => ['TRUONG_BO_MON', 'PHONG_QLKH', 'QUAN_TRI'].includes(x))

  return (
    <>
      <p className="eyebrow">QUẢN LÝ NGHIÊN CỨU KHOA HỌC</p>
      <h1>Chỉ tiêu và phân bổ NCKH</h1>
      <p className="muted">
        Quản lý số bài báo khoa học được giao theo khoa, năm học
        và phân bổ chỉ tiêu cho từng bộ môn.
      </p>

      <Toast message={error} kind="error" onClose={() => setError('')} />
      <Toast message={message} kind="success" onClose={() => setMessage('')} />

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
                  <th>Trạng thái</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {targets.map(target => (
                  <tr key={target.id}>
                    <td>{facultyName(target.facultyId)}</td>
                    <td>{yearName(target.academicYearId)}</td>
                    <td>{target.assignedArticleCount}{target.journalConferenceId ? ` · ${catalog.find(x => x.id === target.journalConferenceId)?.name ?? 'Danh mục đã xóa'}` : ''}</td>
                    <td>{target.deadline || 'Chưa quy định'}</td>
                    <td>{({ CHO_TIEP_NHAN: 'Chờ tiếp nhận', DA_TIEP_NHAN: 'Đã tiếp nhận', CONG_BO: 'Đã công bố', DA_KHOA: 'Đã khóa', DA_HUY: 'Đã hủy' } as Record<string,string>)[target.status] ?? target.status}</td>
                    <td>
                      <button
                        type="button"
                        onClick={() => {
                          setSelectedId(target.id)
                          setTargetPanel('overview')
                          setSelectedAllocation(null)
                          setTargetFormOpen(false)
                          setAllocationFormOpen(false)
                        }}
                      >
                        {selectedId === target.id ? 'Đang xem' : 'Mở chi tiết'}
                      </button>
                      {' '}
                      {canEdit && target.status === 'CHO_TIEP_NHAN' && (
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
                  <select name="academicYearId" required onChange={e => setTargetYearId(Number(e.target.value) || null)}>
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
              Tạp chí/hội nghị liên kết (không bắt buộc)
              <select name="journalConferenceId" defaultValue={editingTarget?.journalConferenceId ?? ''}>
                <option value="">-- Chỉ tiêu chung --</option>
                {catalog.map(x => <option key={x.id} value={x.id}>{x.type === 'TAP_CHI' ? 'Tạp chí' : 'Hội nghị'} — {x.name}</option>)}
              </select>
            </label>

            <label>
              Số bài báo được giao
              <input
                name="assignedArticleCount"
                type="number"
                min="1"
                step="1"
                required
                defaultValue={editingTarget?.assignedArticleCount ?? 1}
              />
            </label>

            <label>
              Hạn hoàn thành
              <input
                name="deadline"
                type="date"
                min={(() => {
                  const start = years.find(y => y.id === targetYearId)?.startDate
                  const today = new Date().toLocaleDateString('en-CA')
                  return start && start > today ? start : today
                })()}
                max={years.find(y => y.id === targetYearId)?.endDate}
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
          <h2>Thực hiện chỉ tiêu</h2>
          {targetPanel === 'allocations' && canEditAllocations && selectedTarget?.status === 'CONG_BO' && (
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
            <div className="tabs target-workflow-tabs" aria-label="Các bước thực hiện chỉ tiêu">
              <button className={targetPanel === 'overview' ? 'active' : ''} onClick={() => setTargetPanel('overview')}>1. Tổng quan</button>
              <button className={targetPanel === 'allocations' ? 'active' : ''} onClick={() => setTargetPanel('allocations')}>2. Phân bổ bộ môn</button>
              {isAllocationLocked && canViewLecturerAssignments && <button className={targetPanel === 'lecturers' ? 'active' : ''} onClick={() => setTargetPanel('lecturers')}>3. Phân công giảng viên</button>}
            </div>
            {targetPanel === 'overview' && <>
            <p className="muted">Thực hiện lần lượt: tiếp nhận chỉ tiêu → phân bổ đủ xuống bộ môn → khóa phân bổ. Sau khi khóa, Trưởng bộ môn mới giao cho giảng viên.</p>
            {progress && <section className="panel" style={{ marginTop: 16 }}>
              <h3>Dashboard tiến độ chỉ tiêu</h3>
              <p><strong>Toàn khoa:</strong> {progress.approvedArticles}/{progress.assignedArticleCount} bài · {progress.completionPercent}% {progress.isCompleted ? '· Đã hoàn thành' : ''}</p>
              <div style={{ height: 12, background: '#e5e7eb', borderRadius: 99, overflow: 'hidden', marginBottom: 14 }}><div style={{ height: '100%', width: `${Math.min(progress.completionPercent, 100)}%`, background: progress.isCompleted ? '#15803d' : '#0f766e' }} /></div>
              <div style={{ overflowX: 'auto' }}><table><thead><tr><th>Bộ môn</th><th>Đã duyệt / chỉ tiêu</th><th>Tỷ lệ</th><th>Biểu đồ</th></tr></thead><tbody>{progress.units.map(unit => <tr key={unit.departmentId}><td>{unit.departmentName}</td><td>{unit.approvedArticles}/{unit.allocatedArticles}</td><td>{unit.completionPercent}% {unit.isCompleted ? '✓' : ''}</td><td style={{ minWidth: 150 }}><div style={{ height: 10, background: '#e5e7eb', borderRadius: 99, overflow: 'hidden' }}><div style={{ height: '100%', width: `${Math.min(unit.completionPercent, 100)}%`, background: unit.isCompleted ? '#15803d' : '#0f766e' }} /></div></td></tr>)}</tbody></table></div>
            </section>}
            {(canEdit || canReceiveAndDistribute) && <div className="actions">
              {selectedTarget.status === 'CHO_TIEP_NHAN' && canReceiveAndDistribute && <button className="primary" onClick={() => void transition('accept')} disabled={busy}>Tiếp nhận chỉ tiêu</button>}
              {selectedTarget.status === 'DA_TIEP_NHAN' && canReceiveAndDistribute && <button className="primary" onClick={() => void transition('publish')} disabled={busy}>Công bố để phân bổ</button>}
              {selectedTarget.status === 'CONG_BO' && canReceiveAndDistribute && <button onClick={() => void transition('lock')} disabled={busy}>Khóa phân bổ</button>}
              {!allocations.length && selectedTarget.status !== 'DA_KHOA' && selectedTarget.status !== 'DA_HUY' && (canEdit || canReceiveAndDistribute) && <button className="danger" onClick={() => void transition('cancel')} disabled={busy}>Hủy chỉ tiêu</button>}
            </div>}
            </>}

            {targetPanel === 'allocations' && <>
            <p className="muted">Bước 2: chia tổng chỉ tiêu của khoa cho các bộ môn. Tổng số bài đã phân bổ không được vượt chỉ tiêu của khoa.</p>
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
                      <th>Phân công giảng viên</th>
                      <th>Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {allocations.map(item => (
                      <tr key={item.id}>
                        <td>{departmentName(item.departmentId)}</td>
                        <td>{item.allocatedArticleCount}</td>
                        <td>{item.allocationDate}</td>
                        <td>{isAllocationLocked && canViewLecturerAssignments
                          ? <button type="button" onClick={() => void openLecturerAssignments(item)}>Xem / phân công</button>
                          : 'Chờ Trưởng khoa khóa phân bổ'}</td>
                        <td>
                          {canEditAllocations && selectedTarget.status === 'CONG_BO' ? (
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
            </>}
            {targetPanel === 'lecturers' && isAllocationLocked && <p className="muted">Bước 3: Trưởng bộ môn chọn bộ môn của mình, sau đó phân công chỉ tiêu cho từng giảng viên.</p>}
          </>
        )}
      </section>

      {targetPanel === 'lecturers' && selectedAllocation && <section className="panel">
        <div className="section-heading"><h2>Phân công cho giảng viên — {departmentName(selectedAllocation.departmentId)}</h2>
          <button onClick={() => { setSelectedAllocation(null); setTargetPanel('allocations') }}>Quay lại phân bổ bộ môn</button></div>
        <p><strong>Chỉ tiêu bộ môn:</strong> {selectedAllocation.allocatedArticleCount} bài · <strong>Đã giao cá nhân:</strong> {lecturerAssignments.reduce((s, x) => s + x.allocatedArticleCount, 0)} bài</p>
        {lecturerAssignments.length > 0 && <div style={{ overflowX: 'auto' }}><table><thead><tr><th>Giảng viên</th><th>Số bài</th><th>Ngày giao</th><th></th></tr></thead><tbody>
          {lecturerAssignments.map(x => <tr key={x.id}><td>{x.lecturerCode} — {x.lecturerName}</td><td>{x.allocatedArticleCount}</td><td>{x.allocationDate}</td><td><button className="danger" disabled={busy} onClick={() => void deleteLecturerAssignment(x.id)}>Hủy</button></td></tr>)}
        </tbody></table></div>}
        {isAllocationLocked && canAssignLecturers && <form onSubmit={saveLecturerAssignment}>
          <label>Giảng viên<select name="lecturerId" required defaultValue=""><option value="">-- Chọn giảng viên --</option>
            {lecturerOptions.filter(x => !lecturerAssignments.some(a => a.lecturerId === x.id)).map(x => <option key={x.id} value={x.id}>{x.code} — {x.fullName}</option>)}
          </select></label>
          <label>Số bài giao<input name="allocatedArticleCount" type="number" min="1" step="1" required /></label>
          <button className="primary" disabled={busy}>Giao chỉ tiêu</button>
        </form>}
      </section>}

      {canEditAllocations && allocationFormOpen && selectedTarget?.status === 'CONG_BO' && (
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
                min="1"
                step="1"
                required
                defaultValue={
                  editingAllocation?.allocatedArticleCount ?? 1
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
