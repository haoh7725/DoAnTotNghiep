import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { api, ApiError } from './api'
import type { Lookups, Profile } from './api'
import './Lecturers.css'

/* ===== Kiểu dữ liệu khớp với LecturerModels.cs ===== */
export type Lecturer = {
  id: number; accountId: number; code: string; fullName: string; birthDate: string | null
  gender: string | null; email: string | null; phone: string | null
  academicRank: string | null; degree: string | null; position: string | null
  departmentId: number; departmentName: string; facultyId: number; facultyName: string
}
type ScientificProfile = {
  lecturerId: number; expertise: string | null; researchFields: string | null
  researchDirections: string | null; activitySummary: string | null; updatedAt: string | null
}
type Paged<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }

const genderNames: Record<string, string> = { NAM: 'Nam', NU: 'Nữ', KHAC: 'Khác' }
const profileFields = [
  { key: 'expertise', label: 'Chuyên môn', hint: 'Ngành, chuyên ngành được đào tạo và đang giảng dạy.' },
  { key: 'researchFields', label: 'Lĩnh vực nghiên cứu', hint: 'Các lĩnh vực khoa học chính.' },
  { key: 'researchDirections', label: 'Hướng nghiên cứu', hint: 'Các hướng đề tài đang và sẽ theo đuổi.' },
  { key: 'activitySummary', label: 'Tóm tắt hoạt động khoa học', hint: 'Đề tài, bài báo, sách, giải thưởng tiêu biểu.' },
] as const
type ProfileKey = (typeof profileFields)[number]['key']
const MAX_TEXT = 4000

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
const fmtDateTime = (iso: string | null) => iso ? new Date(iso).toLocaleString('vi-VN') : null
const show = (v: string | null | undefined) => v?.trim() ? v : '—'

/* ===== Danh sách giảng viên (chỉ hiện các giảng viên trong phạm vi của người dùng) ===== */
export function LecturerDirectory({ user, lookups }: { user: Profile; lookups: Lookups | null }) {
  const [selected, setSelected] = useState<number | null>(null)
  const [keyword, setKeyword] = useState(''); const [applied, setApplied] = useState('')
  const [facultyId, setFacultyId] = useState(''); const [departmentId, setDepartmentId] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<Paged<Lecturer> | null>(null)
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
    <p className="eyebrow">HỒ SƠ KHOA HỌC</p><h1>Hồ sơ giảng viên</h1>
    <p className="muted">Danh sách giảng viên trong phạm vi bạn được xem. Chọn một giảng viên để xem hồ sơ và lý lịch khoa học.</p>
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

/* --- Lý lịch khoa học --- */
function ProfileSection({ lecturer, profile, canEdit, onSaved }: {
  lecturer: Lecturer; profile: ScientificProfile; canEdit: boolean; onSaved: (p: ScientificProfile) => void
}) {
  const [editing, setEditing] = useState(false); const [busy, setBusy] = useState(false)
  const [error, setError] = useState(''); const [saved, setSaved] = useState(false)
  const [draft, setDraft] = useState<Record<ProfileKey, string>>(() => toDraft(profile))
  const empty = !profile.updatedAt
  const updated = fmtDateTime(profile.updatedAt)

  async function save(e: FormEvent) {
    e.preventDefault(); setBusy(true); setError(''); setSaved(false)
    try {
      const body = Object.fromEntries(profileFields.map(f => [f.key, draft[f.key].trim() || null]))
      onSaved(await api<ScientificProfile>(`/lecturers/${lecturer.id}/scientific-profile`, 'PUT', body))
      setEditing(false); setSaved(true)
    } catch (err) { setError((err as Error).message) } finally { setBusy(false) }
  }

  return <section className="panel">
    <div className="section-top"><div><h2>Lý lịch khoa học</h2>
      <p className="muted lec-sub">{updated ? `Cập nhật lần cuối: ${updated}` : 'Chưa lập lý lịch khoa học.'}</p></div>
      {canEdit && !editing && <button className="primary" onClick={() => { setDraft(toDraft(profile)); setEditing(true); setSaved(false); setError('') }}>{empty ? 'Lập lý lịch' : 'Chỉnh sửa'}</button>}</div>
    {saved && <p className="lec-ok" role="status">Đã lưu lý lịch khoa học.</p>}
    {!editing ? <div className="lec-blocks">{profileFields.map(f => <article key={f.key}><h3>{f.label}</h3>
      {profile[f.key]?.trim() ? <p className="lec-text">{profile[f.key]}</p> : <p className="muted">Chưa có thông tin.</p>}</article>)}</div> :
      <form onSubmit={save}>
        {error && <p className="alert" role="alert">{error}</p>}
        {profileFields.map(f => <label key={f.key}>{f.label}
          <span className="lec-hint">{f.hint}</span>
          <textarea rows={4} maxLength={MAX_TEXT} value={draft[f.key]} onChange={e => setDraft(d => ({ ...d, [f.key]: e.target.value }))} />
          <span className="lec-count">{draft[f.key].length}/{MAX_TEXT}</span></label>)}
        <div className="actions"><button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu lý lịch'}</button>
          <button type="button" disabled={busy} onClick={() => { setEditing(false); setError('') }}>Hủy</button></div>
      </form>}
  </section>
}

const toDraft = (p: ScientificProfile): Record<ProfileKey, string> => ({
  expertise: p.expertise ?? '', researchFields: p.researchFields ?? '',
  researchDirections: p.researchDirections ?? '', activitySummary: p.activitySummary ?? '',
})
