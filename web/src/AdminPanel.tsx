import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { Faculty, Department, AcademicYear } from './api'
type Account = { id: number; username: string; fullName: string; email: string | null; status: string }
type Entity = Faculty | Department | AcademicYear | Account
const tabs = [{ key: 'faculties', label: 'Khoa' }, { key: 'departments', label: 'Bộ môn' }, { key: 'academic-years', label: 'Năm học' }, { key: 'accounts', label: 'Tài khoản' }]
export function AdminPanel({ onChanged }: { onChanged: () => void }) {
  const [tab, setTab] = useState('faculties'); const [items, setItems] = useState<Entity[]>([])
  const [faculties, setFaculties] = useState<Faculty[]>([])
  const [error, setError] = useState(''); const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false); const [edit, setEdit] = useState<Entity | null | undefined>(undefined)
  const [revision, setRevision] = useState(0); const [page, setPage] = useState(1); const [total, setTotal] = useState(0)
  useEffect(() => {
    let active = true; setLoading(true); setError(''); setItems([])
    Promise.all([api<Entity[] | { items: Account[]; total: number }>(`/admin/${tab}${tab === 'accounts' ? `?page=${page}&pageSize=20` : ''}`), api<Faculty[]>('/admin/faculties')])
      .then(([data, fs]) => { if (active) { setItems(Array.isArray(data) ? data : data.items); setTotal(Array.isArray(data) ? data.length : data.total); setFaculties(fs) } })
      .catch(e => { if (active) setError(e.message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [tab, revision, page])
  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const data = Object.fromEntries(new FormData(e.currentTarget))
    const payload = tab === 'departments' ? { ...data, facultyId: Number(data.facultyId) } : tab === 'accounts' ? { ...data, email: data.email || null } : data
    setBusy(true); setError('')
    try { await api(`/admin/${tab}${edit ? `/${edit.id}` : ''}`, edit ? 'PUT' : 'POST', payload); setEdit(undefined); setRevision(x => x + 1); onChanged() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  async function remove(item: Entity) {
    if (!window.confirm('Xóa bản ghi này? Dữ liệu đang được sử dụng sẽ không thể xóa.')) return
    setBusy(true); setError('')
    try { await api(`/admin/${tab}/${item.id}`, 'DELETE'); setRevision(x => x + 1); onChanged() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  const value = (key: string) => edit && key in edit ? String((edit as unknown as Record<string, unknown>)[key] ?? '') : ''
  return <><p className="eyebrow">QUẢN TRỊ</p><h1>Dữ liệu nền</h1><p className="muted">Quản lý đơn vị, năm học và tài khoản sử dụng hệ thống.</p>
    <div className="tabs" role="group" aria-label="Loại dữ liệu">{tabs.map(t => <button disabled={busy} key={t.key} className={t.key === tab ? 'active' : ''} onClick={() => { setTab(t.key); setEdit(undefined); setPage(1) }}>{t.label}</button>)}</div>
    {error && <p className="alert" role="alert">{error}</p>}
    {edit !== undefined && <form key={`${tab}-${edit?.id ?? "new"}`} className="panel" onSubmit={save}><h2>{edit ? 'Cập nhật' : 'Thêm mới'} {tabs.find(t => t.key === tab)?.label.toLowerCase()}</h2><div className="form-grid">
      {tab === 'accounts' ? <>
        {!edit && <label>Tên đăng nhập<input name="username" required maxLength={100} pattern="[a-zA-Z0-9._-]+" /></label>}
        <label>Họ tên<input name="fullName" required maxLength={200} defaultValue={value('fullName')} /></label>
        <label>Email<input type="email" name="email" maxLength={254} defaultValue={value('email')} /></label>
        {!edit ? <label>Mật khẩu ban đầu<input type="password" name="password" required minLength={12} maxLength={128} autoComplete="new-password" /></label> : <label>Trạng thái<select name="status" defaultValue={value('status')}><option value="HOAT_DONG">Hoạt động</option><option value="KHOA">Khóa</option></select></label>}
      </> : <><label>Mã<input name="code" required maxLength={tab === 'academic-years' ? 20 : 30} defaultValue={value('code')} /></label>
        {tab !== 'academic-years' && <label>Tên<input name="name" required maxLength={200} defaultValue={value('name')} /></label>}
        {tab === 'departments' && <label>Khoa<select name="facultyId" required defaultValue={value('facultyId')}><option value="">Chọn khoa</option>{faculties.map(f => <option value={f.id} key={f.id}>{f.name}</option>)}</select></label>}
        {tab === 'academic-years' && <><label>Ngày bắt đầu<input type="date" name="startDate" required defaultValue={value('startDate')} /></label><label>Ngày kết thúc<input type="date" name="endDate" required defaultValue={value('endDate')} /></label></>}
      </>}
    </div><div className="actions"><button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu'}</button><button type="button" disabled={busy} onClick={() => setEdit(undefined)}>Hủy</button></div></form>}
    <section className="panel"><div className="section-top"><h2>{tabs.find(t => t.key === tab)?.label}</h2><button className="primary" disabled={busy || loading} onClick={() => { setEdit(null); setError('') }}>+ Thêm mới</button></div>
      {loading ? <p role="status">Đang tải…</p> : <><div className="table-wrap"><table><thead><tr><th>Mã / tài khoản</th><th>Tên / thời gian</th><th>Thông tin</th><th>Thao tác</th></tr></thead><tbody>
        {items.map(item => <tr key={item.id}><td>{'username' in item ? item.username : item.code}</td><td>{'fullName' in item ? item.fullName : 'name' in item ? item.name : `${item.startDate} → ${item.endDate}`}</td><td>{'status' in item ? item.status === 'HOAT_DONG' ? 'Hoạt động' : 'Khóa' : 'facultyId' in item ? faculties.find(f => f.id === item.facultyId)?.name : '—'}</td><td><div className="actions"><button disabled={busy} onClick={() => { setEdit(item); setError('') }}>Sửa</button><button disabled={busy} className="danger" onClick={() => remove(item)}>Xóa</button></div></td></tr>)}
      </tbody></table></div>{!items.length && <p className="empty">Chưa có dữ liệu.</p>}
      {tab === 'accounts' && <div className="actions"><button disabled={page === 1} onClick={() => setPage(x => x - 1)}>Trước</button><span>Trang {page} · {total} tài khoản</span><button disabled={page * 20 >= total} onClick={() => setPage(x => x + 1)}>Sau</button></div>}
      {error && <button onClick={() => setRevision(x => x + 1)}>Tải lại</button>}
      </>}
    </section></>
}
