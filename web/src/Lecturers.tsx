import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { api, ApiError, roleNames } from './api'
import type { Lookups, Profile } from './api'
import './Lecturers.css'
import { ProfileSection } from './ScientificProfile'
import type { ScientificProfile } from './ScientificProfile'

/* ===== Kiểu dữ liệu khớp với LecturerModels.cs ===== */
export type Lecturer = {
  id: number; accountId: number; code: string; fullName: string; birthDate: string | null
  gender: string | null; email: string | null; phone: string | null
  academicRank: string | null; degree: string | null; position: string | null
  departmentId: number; departmentName: string; facultyId: number; facultyName: string
}
type Paged<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
type LecturerAccountOption = { id: number; username: string; fullName: string; email: string | null; roles: string[] }

const genderNames: Record<string, string> = { NAM: 'Nam', NU: 'Nữ', KHAC: 'Khác' }

/* ===== Quy tắc quyền, phản chiếu LecturerAccess.cs (backend vẫn là nơi quyết định cuối cùng) ===== */
export const isOffice = (user: Profile) =>
  user.roles.some(r => (r.role === 'PHONG_QLKH' || r.role === 'QUAN_TRI') && r.scope === 'TOAN_TRUONG')
export const canBrowseLecturers = (user: Profile) => user.roles.some(r => r.role !== 'GIANG_VIEN')

function permissionsFor(user: Profile, lecturer: Lecturer) {
  const office = isOffice(user)
  const owner = user.id === lecturer.accountId
  return {
    office, owner,
    editPersonal: office || owner,   // họ tên, ngày sinh, giới tính, email, điện thoại
    editManaged: office,             // mã, bộ môn, học hàm, học vị, chức vụ
    editProfile: office || owner,    // lý lịch khoa học
  }
}

const fmtDate = (iso: string | null) => iso ? new Date(iso + (iso.length === 10 ? 'T00:00:00' : '')).toLocaleDateString('vi-VN') : '—'
const show = (v: string | null | undefined) => v?.trim() ? v : '—'

/* ===== Danh sách giảng viên (chỉ hiện các giảng viên trong phạm vi của người dùng) ===== */
export function LecturerDirectory({ user, lookups }: { user: Profile; lookups: Lookups | null }) {
  const [selected, setSelected] = useState<number | null>(null)
  const [keyword, setKeyword] = useState(''); const [applied, setApplied] = useState('')
  const [facultyId, setFacultyId] = useState(''); const [departmentId, setDepartmentId] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<Paged<Lecturer> | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState(''); const [loading, setLoading] = useState(true); const [retry, setRetry] = useState(0)
  const departments = useMemo(
    () => (lookups?.departments ?? []).filter(d => !facultyId || d.facultyId === Number(facultyId)), [lookups, facultyId])

  useEffect(() => {
    if (selected !== null) return
    let active = true; setLoading(true); setError('')
    const q = new URLSearchParams({ page: String(page), pageSize: '15' })
    if (applied) q.set('keyword', applied)
    if (facultyId) q.set('facultyId', facultyId)
    if (departmentId) q.set('departmentId', departmentId)
    api<Paged<Lecturer>>(`/lecturers?${q}`).then(d => { if (active) setData(d) })
      .catch(e => { if (active) setError((e as Error).message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [selected, page, applied, facultyId, departmentId, retry])

  if (selected !== null) return <LecturerDetail user={user} lecturerId={selected} lookups={lookups} onBack={() => setSelected(null)} />
  const submit = (e: FormEvent) => { e.preventDefault(); setPage(1); setApplied(keyword.trim()) }
  return <>
    <p className="eyebrow">HỒ SƠ KHOA HỌC</p>
    <div className="section-top"><h1>Hồ sơ giảng viên</h1>{isOffice(user) && <button className="primary" disabled={!lookups} onClick={() => setCreating(value => !value)}>{creating ? 'Đóng biểu mẫu' : '+ Thêm hồ sơ giảng viên'}</button>}</div>
    <p className="muted">Danh sách giảng viên trong phạm vi bạn được xem. Chọn một giảng viên để xem hồ sơ và lý lịch khoa học.</p>
    {creating && <CreateLecturerForm lookups={lookups} onCancel={() => setCreating(false)} onCreated={lecturer => { setCreating(false); setSelected(lecturer.id) }} />}
    <form className="panel lec-filter" onSubmit={submit}>
      <label>Tìm kiếm<input value={keyword} onChange={e => setKeyword(e.target.value)} maxLength={100} placeholder="Họ tên hoặc mã giảng viên" /></label>
      <label>Khoa<select value={facultyId} onChange={e => { setFacultyId(e.target.value); setDepartmentId(''); setPage(1) }}>
        <option value="">Tất cả khoa</option>{lookups?.faculties.map(f => <option key={f.id} value={f.id}>{f.name}</option>)}</select></label>
      <label>Bộ môn<select value={departmentId} onChange={e => { setDepartmentId(e.target.value); setPage(1) }}>
        <option value="">Tất cả bộ môn</option>{departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}</select></label>
      <button className="primary" disabled={loading}>Tìm</button>
    </form>
    {error && <p className="alert" role="alert">{error} <button onClick={() => setRetry(x => x + 1)}>Thử lại</button></p>}
    <section className="panel">
      {loading && !data ? <p role="status">Đang tải…</p> : <>
        <div className="table-wrap"><table><thead><tr><th>Mã</th><th>Họ tên</th><th>Học hàm / học vị</th><th>Bộ môn</th><th>Khoa</th><th></th></tr></thead><tbody>
          {data?.items.map(l => <tr key={l.id}>
            <td>{l.code}</td><td><strong>{l.fullName}</strong>{l.accountId === user.id && <span className="lec-badge">Của bạn</span>}</td>
            <td>{[l.academicRank, l.degree].filter(Boolean).join(' · ') || '—'}</td><td>{l.departmentName}</td><td>{l.facultyName}</td>
            <td><button onClick={() => setSelected(l.id)}>Xem hồ sơ</button></td></tr>)}
        </tbody></table></div>
        {!loading && !data?.items.length && <p className="empty">Không có giảng viên phù hợp.</p>}
        {data && data.totalCount > 0 && <div className="actions"><button disabled={page <= 1 || loading} onClick={() => setPage(x => x - 1)}>Trước</button>
          <span>Trang {data.page}/{Math.max(data.totalPages, 1)} · {data.totalCount} giảng viên</span>
          <button disabled={page >= data.totalPages || loading} onClick={() => setPage(x => x + 1)}>Sau</button></div>}
      </>}
    </section>
  </>
}

function CreateLecturerForm({ lookups, onCreated, onCancel }: {
  lookups: Lookups | null; onCreated: (lecturer: Lecturer) => void; onCancel: () => void
}) {
  const [accounts, setAccounts] = useState<LecturerAccountOption[]>([])
  const [accountId, setAccountId] = useState('')
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    setLoading(true); setError('')
    api<LecturerAccountOption[]>('/lecturers/account-options')
      .then(items => { if (active) setAccounts(items) })
      .catch(e => { if (active) setError((e as Error).message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  const selectedAccount = accounts.find(account => account.id === Number(accountId))

  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const form = new FormData(e.currentTarget)
    setBusy(true); setError('')
    try {
      const lecturer = await api<Lecturer>('/lecturers', 'POST', {
        accountId: Number(form.get('accountId')),
        departmentId: Number(form.get('departmentId')),
        code: String(form.get('code') ?? '').trim(),
        fullName: String(form.get('fullName') ?? '').trim(),
        birthDate: form.get('birthDate') || null,
        gender: form.get('gender') || null,
        email: String(form.get('email') ?? '').trim() || null,
        phone: String(form.get('phone') ?? '').trim() || null,
        academicRank: String(form.get('academicRank') ?? '').trim() || null,
        degree: String(form.get('degree') ?? '').trim() || null,
        position: String(form.get('position') ?? '').trim() || null,
      })
      onCreated(lecturer)
    } catch (e) { setError((e as Error).message) }
    finally { setBusy(false) }
  }

  return <form className="panel lec-create-form" onSubmit={submit}>
    <div className="section-top"><div><h2>Thêm hồ sơ và liên kết tài khoản</h2><p className="muted lec-sub">Chỉ các tài khoản đang hoạt động, có vai trò Giảng viên/Trưởng bộ môn/Trưởng khoa và chưa liên kết mới xuất hiện.</p></div><button type="button" onClick={onCancel} disabled={busy}>Hủy</button></div>
    {error && <p className="alert" role="alert">{error}</p>}
    {loading ? <p role="status">Đang tải tài khoản có thể liên kết…</p> : accounts.length === 0 ? <p className="empty">Không có tài khoản phù hợp. Hãy tạo tài khoản và phân quyền trước trong Quản trị dữ liệu nền.</p> : <>
      <label>Tài khoản liên kết *<select name="accountId" required value={accountId} onChange={e => setAccountId(e.target.value)}><option value="">— Chọn tài khoản chưa liên kết —</option>{accounts.map(account => <option value={account.id} key={account.id}>{account.fullName} (@{account.username}) · {account.roles.map(role => roleNames[role] ?? role).join(', ')}</option>)}</select></label>
      <div className="form-grid">
        <label>Mã giảng viên *<input name="code" required maxLength={30} placeholder="Ví dụ: CNTT009" /></label>
        <label>Bộ môn *<select name="departmentId" required defaultValue=""><option value="">— Chọn bộ môn —</option>{lookups?.departments.map(department => <option value={department.id} key={department.id}>{department.name} · {lookups.faculties.find(faculty => faculty.id === department.facultyId)?.name}</option>)}</select></label>
        <label>Họ tên *<input key={`name-${accountId}`} name="fullName" required maxLength={200} defaultValue={selectedAccount?.fullName ?? ''} /></label>
        <label>Email<input key={`email-${accountId}`} type="email" name="email" maxLength={254} defaultValue={selectedAccount?.email ?? ''} /></label>
        <label>Ngày sinh<input type="date" name="birthDate" min="1900-01-01" max={new Date().toISOString().slice(0, 10)} /></label>
        <label>Giới tính<select name="gender" defaultValue=""><option value="">— Chưa chọn —</option><option value="NAM">Nam</option><option value="NU">Nữ</option><option value="KHAC">Khác</option></select></label>
        <label>Điện thoại<input name="phone" maxLength={30} pattern="\+?[0-9 .()\-]{8,30}" title="Số điện thoại 8–30 ký tự, gồm chữ số và + . ( ) -" /></label>
        <label>Học hàm<input name="academicRank" maxLength={100} placeholder="Ví dụ: PGS" /></label>
        <label>Học vị<input name="degree" maxLength={100} placeholder="Ví dụ: Tiến sĩ" /></label>
        <label>Chức vụ<input name="position" maxLength={100} /></label>
      </div>
      <div className="actions"><button className="primary" disabled={busy || !accountId}>{busy ? 'Đang tạo…' : 'Tạo và liên kết hồ sơ'}</button><button type="button" onClick={onCancel} disabled={busy}>Hủy</button></div>
    </>}
  </form>
}

/* ===== Hồ sơ của tôi ===== */
export function MyLecturerProfile({ user, lookups }: { user: Profile; lookups: Lookups | null }) {
  return <LecturerDetail user={user} lookups={lookups} />
}

/* ===== Chi tiết: thông tin hồ sơ + lý lịch khoa học ===== */
function LecturerDetail({ user, lookups, lecturerId, onBack }: {
  user: Profile; lookups: Lookups | null; lecturerId?: number; onBack?: () => void
}) {
  const [lecturer, setLecturer] = useState<Lecturer | null>(null)
  const [profile, setProfile] = useState<ScientificProfile | null>(null)
  const [error, setError] = useState(''); const [status, setStatus] = useState<number | null>(null)
  const [loading, setLoading] = useState(true); const [retry, setRetry] = useState(0)

  useEffect(() => {
    let active = true; setLoading(true); setError(''); setStatus(null)
    ;(async () => {
      const l = await api<Lecturer>(lecturerId === undefined ? '/lecturers/me' : `/lecturers/${lecturerId}`)
      const p = await api<ScientificProfile>(`/lecturers/${l.id}/scientific-profile`)
      if (active) { setLecturer(l); setProfile(p) }
    })().catch(e => { if (active) { setError((e as Error).message); setStatus(e instanceof ApiError ? e.status : null) } })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [lecturerId, retry])

  const back = onBack && <button className="lec-back" onClick={onBack}>← Quay lại danh sách</button>
  if (loading) return <p role="status">Đang tải hồ sơ…</p>
  if (!lecturer || !profile) return <>{back}<p className="eyebrow">HỒ SƠ KHOA HỌC</p><h1>{lecturerId === undefined ? 'Hồ sơ của tôi' : 'Hồ sơ giảng viên'}</h1>
    <p className="alert" role="alert">{status === 404 && lecturerId === undefined
      ? 'Tài khoản của bạn chưa được liên kết với hồ sơ giảng viên. Vui lòng liên hệ Phòng quản lý khoa học.' : error}</p>
    {status !== 404 && <button onClick={() => setRetry(x => x + 1)}>Thử lại</button>}</>

  const perm = permissionsFor(user, lecturer)
  const level = perm.office ? 'office' : perm.owner ? 'owner' : 'view'
  const notice = {
    office: 'Bạn có quyền Phòng QLKH / Quản trị: được sửa toàn bộ hồ sơ và lý lịch khoa học của giảng viên này.',
    owner: 'Đây là hồ sơ của bạn. Bạn được sửa thông tin cá nhân và lý lịch khoa học. Mã, bộ môn, học hàm, học vị và chức vụ do Phòng QLKH quản lý.',
    view: 'Bạn chỉ có quyền xem hồ sơ này. Việc chỉnh sửa thuộc về giảng viên và Phòng QLKH.',
  }[level]
  return <>
    {back}
    <p className="eyebrow">HỒ SƠ KHOA HỌC</p>
    <h1>{lecturerId === undefined ? 'Hồ sơ của tôi' : lecturer.fullName}</h1>
    <p className="muted">{lecturer.code} · {lecturer.departmentName} · {lecturer.facultyName}</p>
    <p className={`lec-notice ${level}`} role="note"><strong>{{ office: 'Toàn quyền chỉnh sửa', owner: 'Chính chủ', view: 'Chỉ xem' }[level]}</strong>{notice}</p>
    <PersonalSection key={`p${lecturer.id}`} lecturer={lecturer} perm={perm} lookups={lookups} onSaved={setLecturer} />
    <ProfileSection key={`s${lecturer.id}`} lecturer={lecturer} profile={profile} canEdit={perm.editProfile} onSaved={setProfile} />
  </>
}

type Perm = ReturnType<typeof permissionsFor>

/* --- Thông tin hồ sơ --- */
function PersonalSection({ lecturer, perm, lookups, onSaved }: {
  lecturer: Lecturer; perm: Perm; lookups: Lookups | null; onSaved: (l: Lecturer) => void
}) {
  const [editing, setEditing] = useState(false); const [busy, setBusy] = useState(false)
  const [error, setError] = useState(''); const [saved, setSaved] = useState(false)
  const departments = lookups?.departments ?? []

  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const f = new FormData(e.currentTarget)
    const text = (k: string) => String(f.get(k) ?? '').trim() || null
    const personal = { fullName: String(f.get('fullName') ?? '').trim(), birthDate: text('birthDate'), gender: text('gender'), email: text('email'), phone: text('phone') }
    setBusy(true); setError(''); setSaved(false)
    try {
      // Phòng QLKH/Quản trị sửa đủ trường qua PUT /{id}; giảng viên chính chủ chỉ gửi trường cá nhân qua PUT /me.
      const updated = perm.office
        ? await api<Lecturer>(`/lecturers/${lecturer.id}`, 'PUT', { ...personal, departmentId: Number(f.get('departmentId')), code: String(f.get('code') ?? '').trim(),
          academicRank: text('academicRank'), degree: text('degree'), position: text('position') })
        : await api<Lecturer>('/lecturers/me', 'PUT', personal)
      onSaved(updated); setEditing(false); setSaved(true)
    } catch (err) { setError((err as Error).message) } finally { setBusy(false) }
  }

  const rows: [string, string][] = [
    ['Họ tên', lecturer.fullName], ['Mã giảng viên', lecturer.code], ['Ngày sinh', fmtDate(lecturer.birthDate)],
    ['Giới tính', lecturer.gender ? genderNames[lecturer.gender] ?? lecturer.gender : '—'], ['Email', show(lecturer.email)], ['Điện thoại', show(lecturer.phone)],
    ['Học hàm', show(lecturer.academicRank)], ['Học vị', show(lecturer.degree)], ['Chức vụ', show(lecturer.position)],
    ['Bộ môn', lecturer.departmentName], ['Khoa', lecturer.facultyName],
  ]
  return <section className="panel">
    <div className="section-top"><h2>Thông tin hồ sơ</h2>
      {perm.editPersonal && !editing && <button className="primary" onClick={() => { setEditing(true); setSaved(false); setError('') }}>Chỉnh sửa</button>}</div>
    {saved && <p className="lec-ok" role="status">Đã lưu thông tin hồ sơ.</p>}
    {!editing ? <dl className="lec-dl">{rows.map(([k, v]) => <div key={k}><dt>{k}</dt><dd>{v}</dd></div>)}</dl> :
      <form onSubmit={save}>
        {error && <p className="alert" role="alert">{error}</p>}
        <div className="form-grid">
          <label>Họ tên *<input name="fullName" required maxLength={200} defaultValue={lecturer.fullName} /></label>
          <label>Mã giảng viên {!perm.editManaged && <Lock />}<input name="code" required maxLength={30} defaultValue={lecturer.code} disabled={!perm.editManaged} /></label>
          <label>Ngày sinh<input type="date" name="birthDate" min="1900-01-01" max={new Date().toISOString().slice(0, 10)} defaultValue={lecturer.birthDate ?? ''} /></label>
          <label>Giới tính<select name="gender" defaultValue={lecturer.gender ?? ''}><option value="">— Chưa chọn —</option>
            {Object.entries(genderNames).map(([v, n]) => <option key={v} value={v}>{n}</option>)}</select></label>
          <label>Email<input type="email" name="email" maxLength={254} defaultValue={lecturer.email ?? ''} /></label>
          <label>Điện thoại<input name="phone" maxLength={30} pattern="\+?[0-9 .()\-]{8,30}" title="Số điện thoại 8–30 ký tự, gồm chữ số và + . ( ) -" defaultValue={lecturer.phone ?? ''} /></label>
          <label>Học hàm {!perm.editManaged && <Lock />}<input name="academicRank" maxLength={100} defaultValue={lecturer.academicRank ?? ''} disabled={!perm.editManaged} /></label>
          <label>Học vị {!perm.editManaged && <Lock />}<input name="degree" maxLength={100} defaultValue={lecturer.degree ?? ''} disabled={!perm.editManaged} /></label>
          <label>Chức vụ {!perm.editManaged && <Lock />}<input name="position" maxLength={100} defaultValue={lecturer.position ?? ''} disabled={!perm.editManaged} /></label>
          <label>Bộ môn {!perm.editManaged && <Lock />}
            {perm.editManaged ? <select name="departmentId" required defaultValue={lecturer.departmentId}>
              {!departments.some(d => d.id === lecturer.departmentId) && <option value={lecturer.departmentId}>{lecturer.departmentName}</option>}
              {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}</select>
              : <input value={`${lecturer.departmentName} — ${lecturer.facultyName}`} disabled readOnly />}</label>
        </div>
        {!perm.editManaged && <p className="muted">Các trường có biểu tượng khóa do Phòng QLKH quản lý. Liên hệ Phòng QLKH nếu cần điều chỉnh.</p>}
        <div className="actions"><button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu thay đổi'}</button>
          <button type="button" disabled={busy} onClick={() => { setEditing(false); setError('') }}>Hủy</button></div>
      </form>}
  </section>
}

const Lock = () => <span className="lec-lock" title="Chỉ Phòng QLKH được sửa" aria-label="Bị khóa, chỉ Phòng QLKH được sửa">🔒</span>
